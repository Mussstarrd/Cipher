"""Minimal Kalshi trade-api v2 client (stdlib only).

Read-only market data on Kalshi is public -- ``/events``, ``/markets`` and
``/markets/{ticker}/orderbook`` need no credentials, which means the whole
scanner can run in observe-only mode with nothing at risk. Private endpoints
(balance, orders, positions) require an RSA key pair and a signed header; that
path is implemented but stays dormant unless a key is configured.

No third-party dependencies on the read path on purpose: the thing that watches
markets should not be able to break because a transitive dep did.
"""

from __future__ import annotations

import base64
import json
import time
import urllib.error
import urllib.parse
import urllib.request
from dataclasses import dataclass
from datetime import datetime, timezone

from .model import Event, Market, Quote

PROD_BASE = "https://api.elections.kalshi.com/trade-api/v2"
DEMO_BASE = "https://demo-api.kalshi.co/trade-api/v2"

USER_AGENT = "cipher-scanner/0.1 (+observe-only)"


class KalshiError(RuntimeError):
    """Any non-2xx response or transport failure, with the body attached."""

    def __init__(self, message: str, status: int | None = None, body: str = ""):
        super().__init__(message)
        self.status = status
        self.body = body


@dataclass
class Credentials:
    """API key id plus a PEM-encoded RSA private key.

    Only needed for trading and account endpoints. Signing uses RSA-PSS over
    ``timestamp_ms + METHOD + path``.
    """

    key_id: str
    private_key_pem: str

    def sign(self, method: str, path: str) -> dict[str, str]:
        try:
            from cryptography.hazmat.primitives import hashes, serialization
            from cryptography.hazmat.primitives.asymmetric import padding
        except ImportError as exc:  # pragma: no cover - only hit when trading
            raise KalshiError(
                "signing requires the 'cryptography' package; "
                "install it only if you intend to place orders"
            ) from exc

        timestamp_ms = str(int(time.time() * 1000))
        message = f"{timestamp_ms}{method.upper()}{path}".encode()
        key = serialization.load_pem_private_key(self.private_key_pem.encode(), password=None)
        signature = key.sign(
            message,
            padding.PSS(mgf=padding.MGF1(hashes.SHA256()), salt_length=hashes.SHA256.digest_size),
            hashes.SHA256(),
        )
        return {
            "KALSHI-ACCESS-KEY": self.key_id,
            "KALSHI-ACCESS-SIGNATURE": base64.b64encode(signature).decode(),
            "KALSHI-ACCESS-TIMESTAMP": timestamp_ms,
        }


class KalshiClient:
    """Thin REST wrapper. Every method returns normalised model objects."""

    def __init__(
        self,
        base_url: str = PROD_BASE,
        credentials: Credentials | None = None,
        timeout: float = 10.0,
        max_retries: int = 3,
    ):
        self.base_url = base_url.rstrip("/")
        self.credentials = credentials
        self.timeout = timeout
        self.max_retries = max_retries

    # ---- transport ----------------------------------------------------

    def _get(self, path: str, params: dict | None = None) -> dict:
        query = f"?{urllib.parse.urlencode(params)}" if params else ""
        # The signature covers the path *including* the /trade-api/v2 prefix.
        signed_path = urllib.parse.urlsplit(self.base_url).path + path
        url = f"{self.base_url}{path}{query}"

        headers = {"Accept": "application/json", "User-Agent": USER_AGENT}
        if self.credentials:
            headers.update(self.credentials.sign("GET", signed_path))

        backoff = 1.0
        last: Exception | None = None
        for attempt in range(self.max_retries):
            try:
                request = urllib.request.Request(url, headers=headers)
                with urllib.request.urlopen(request, timeout=self.timeout) as response:
                    return json.loads(response.read().decode())
            except urllib.error.HTTPError as exc:
                body = exc.read().decode(errors="replace")
                # 429 and 5xx are worth another go; 4xx is our own fault.
                if exc.code != 429 and exc.code < 500:
                    raise KalshiError(f"GET {path} -> {exc.code}", exc.code, body) from exc
                last = KalshiError(f"GET {path} -> {exc.code}", exc.code, body)
            except (urllib.error.URLError, TimeoutError) as exc:
                last = KalshiError(f"GET {path} failed: {exc}")
            if attempt < self.max_retries - 1:
                time.sleep(backoff)
                backoff *= 2
        raise last or KalshiError(f"GET {path} failed")

    # ---- public market data -------------------------------------------

    def exchange_status(self) -> dict:
        return self._get("/exchange/status")

    def markets(
        self,
        *,
        status: str = "open",
        event_ticker: str | None = None,
        series_ticker: str | None = None,
        limit: int = 200,
        max_pages: int = 25,
    ) -> list[Market]:
        """Page through /markets, returning normalised markets."""
        params: dict = {"status": status, "limit": min(limit, 1000)}
        if event_ticker:
            params["event_ticker"] = event_ticker
        if series_ticker:
            params["series_ticker"] = series_ticker

        out: list[Market] = []
        cursor: str | None = None
        for _ in range(max_pages):
            if cursor:
                params["cursor"] = cursor
            payload = self._get("/markets", params)
            out.extend(parse_market(raw) for raw in payload.get("markets", []))
            cursor = payload.get("cursor") or None
            if not cursor:
                break
        return out

    def events(
        self,
        *,
        status: str = "open",
        with_nested_markets: bool = True,
        limit: int = 200,
        max_pages: int = 25,
    ) -> list[Event]:
        params: dict = {
            "status": status,
            "with_nested_markets": str(with_nested_markets).lower(),
            "limit": min(limit, 200),
        }
        out: list[Event] = []
        cursor: str | None = None
        for _ in range(max_pages):
            if cursor:
                params["cursor"] = cursor
            payload = self._get("/events", params)
            out.extend(parse_event(raw) for raw in payload.get("events", []))
            cursor = payload.get("cursor") or None
            if not cursor:
                break
        return out

    def orderbook(self, ticker: str, depth: int = 10) -> dict:
        return self._get(f"/markets/{ticker}/orderbook", {"depth": depth})


# ---- parsing ----------------------------------------------------------


def _parse_time(value: str | None) -> datetime:
    if not value:
        return datetime.max.replace(tzinfo=timezone.utc)
    return datetime.fromisoformat(value.replace("Z", "+00:00"))


def _int_or_none(value) -> int | None:
    if value is None:
        return None
    try:
        parsed = int(value)
    except (TypeError, ValueError):
        return None
    # Kalshi reports 0 for "no quote on this side"; a genuine 0c resting bid is
    # indistinguishable and worthless either way, so treat it as absent.
    return parsed or None


def parse_market(raw: dict) -> Market:
    """Normalise one /markets entry.

    Kalshi returns ``yes_bid``/``yes_ask``/``no_bid``/``no_ask`` in cents. It
    does *not* always return sizes on this endpoint -- depth comes from the
    orderbook call -- so sizes default to 0 and scanners must treat 0 as unknown
    rather than as "no liquidity".
    """
    quote = Quote(
        yes_bid=_int_or_none(raw.get("yes_bid")),
        yes_ask=_int_or_none(raw.get("yes_ask")),
        no_bid=_int_or_none(raw.get("no_bid")),
        no_ask=_int_or_none(raw.get("no_ask")),
        # Sizes are absent from /markets and present once an orderbook has been
        # merged in; 0 therefore means "unknown", not "empty".
        yes_bid_size=int(raw.get("yes_bid_size") or 0),
        yes_ask_size=int(raw.get("yes_ask_size") or 0),
        no_bid_size=int(raw.get("no_bid_size") or 0),
        no_ask_size=int(raw.get("no_ask_size") or 0),
    )
    return Market(
        ticker=raw["ticker"],
        event_ticker=raw.get("event_ticker", ""),
        title=raw.get("title") or raw.get("yes_sub_title") or raw["ticker"],
        close_time=_parse_time(raw.get("close_time")),
        quote=quote,
        status=raw.get("status", "unknown"),
        volume=int(raw.get("volume") or 0),
        open_interest=int(raw.get("open_interest") or 0),
        strike_type=raw.get("strike_type"),
        floor_strike=raw.get("floor_strike"),
        cap_strike=raw.get("cap_strike"),
    )


def parse_event(raw: dict) -> Event:
    return Event(
        event_ticker=raw["event_ticker"],
        title=raw.get("title", raw["event_ticker"]),
        markets=tuple(parse_market(m) for m in raw.get("markets", [])),
        mutually_exclusive=bool(raw.get("mutually_exclusive")),
        category=raw.get("category"),
    )
