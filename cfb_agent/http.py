"""Tiny cached HTTP client (stdlib only, so the repo has zero hard deps).

Every GET is cached to disk keyed by URL+params so re-running the card
doesn't hammer free APIs, and so a week's inputs are reproducible.

Metered endpoints (The Odds API) additionally get an immutable, timestamped
archive of the raw response body under data/cache/raw/ — a metered request is
paid for once and should never need to be spent twice to answer "what did the
book actually say at 5pm?".
"""

import hashlib
import json
import time
import urllib.error
import urllib.parse
import urllib.request
from datetime import datetime, timezone
from typing import Any, Optional

from . import config

# ESPN's public JSON hosts return 403 to requests without an Accept header.
DEFAULT_HEADERS = {
    "User-Agent": "cipher-cfb/0.1",
    "Accept": "application/json",
}


class FetchError(RuntimeError):
    """A source could not be reached. Callers decide whether it's fatal."""


def _cache_path(url: str, params: Optional[dict]) -> "Any":
    key = url + "?" + urllib.parse.urlencode(sorted((params or {}).items()))
    digest = hashlib.sha256(key.encode()).hexdigest()[:24]
    return config.CACHE_DIR / f"{digest}.json"


def get_json(
    url: str,
    params: Optional[dict] = None,
    headers: Optional[dict] = None,
    ttl: int = config.CACHE_TTL,
    archive_as: Optional[str] = None,
    retries: int = 2,
) -> Any:
    """GET a JSON endpoint with disk caching. Raises FetchError on failure.

    `archive_as` names a metered response worth keeping forever; the body is
    written to data/cache/raw/<name>_<timestamp>.json in addition to the
    TTL cache.
    """
    config.ensure_dirs()
    path = _cache_path(url, params)
    if path.exists() and (time.time() - path.stat().st_mtime) < ttl:
        with open(path, encoding="utf-8") as f:
            return json.load(f)

    full = url + ("?" + urllib.parse.urlencode(params) if params else "")
    req = urllib.request.Request(full, headers={**DEFAULT_HEADERS, **(headers or {})})

    last_err: Optional[Exception] = None
    for attempt in range(retries + 1):
        try:
            with urllib.request.urlopen(req, timeout=30) as resp:
                body = resp.read().decode()
                data = json.loads(body)
            break
        except (urllib.error.URLError, urllib.error.HTTPError, TimeoutError, OSError,
                json.JSONDecodeError) as e:
            last_err = e
            if attempt < retries:
                time.sleep(1.5 * (attempt + 1))
    else:
        # Stale cache beats no data.
        if path.exists():
            with open(path, encoding="utf-8") as f:
                return json.load(f)
        raise FetchError(f"GET {url} failed: {last_err}") from last_err

    with open(path, "w", encoding="utf-8") as f:
        json.dump(data, f)
    if archive_as:
        stamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
        raw_path = config.RAW_DIR / f"{archive_as}_{stamp}.json"
        raw_path.write_text(body, encoding="utf-8")
    return data


def cached_only(url: str, params: Optional[dict] = None) -> Optional[Any]:
    """Return a cached body regardless of age, or None. Spends no request."""
    path = _cache_path(url, params)
    if not path.exists():
        return None
    with open(path, encoding="utf-8") as f:
        return json.load(f)
