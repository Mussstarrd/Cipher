"""NWS daily-high resolver -- the first real source-of-truth resolver.

Why weather is the right place to start: the settlement input is public, free,
machine-readable, updated hourly, and -- crucially -- **monotone**. A day's
maximum temperature can only ever go up. That one property turns a forecasting
problem into an observation problem for part of every day.

The asymmetry that makes this work
----------------------------------
The hourly observation feed is not guaranteed to contain the exact value the
Daily Climate Report will publish; the station's own instrumentation can catch a
spike between hourly obs. So the reported daily max satisfies::

    reported_max >= max(observed hourly readings)

That inequality only points one way, and it decides which claims are safe:

* "observed max is already above this bracket's ceiling" -> the bracket **cannot**
  win. Reported max is at least the observed max, so it is also above the
  ceiling. This is genuinely deterministic, and it is where the money is.
* "observed max is still below this bracket's floor" -> tells you much less. The
  real max may already be higher than anything you have seen. Model territory,
  not certainty.

The resolver reflects that split: only the first case is ever marked
``deterministic``.

Timing
------
Before the afternoon peak, the day has most of its heating left and this
resolver is close to useless -- it will report low confidence and the
disagreement scanner will discard it. The edge is in the hours *after* peak
heating, when the max is effectively locked but the book has not fully
converged.

Two failure modes handled explicitly
------------------------------------
1. **Unit rounding.** The API reports Celsius; the climate report publishes whole
   degrees Fahrenheit, and a flip across a half-degree changes which bracket
   wins. Rather than refusing every reading near a boundary, the resolver
   enumerates the whole-degree values the report could plausibly publish
   (``plausible_rounded_values``) and takes the signal only when all of them
   imply the same outcome -- so an ambiguity that does not change the answer
   costs nothing, and one that does is dropped.
2. **Wrong station.** See ``stations.py``. Unverified stations never produce
   deterministic signals.
"""

from __future__ import annotations

import json
import math
import os
import urllib.error
import urllib.parse
import urllib.request
from dataclasses import dataclass
from datetime import datetime, timedelta, timezone
from zoneinfo import ZoneInfo

from ..model import Market, utcnow
from .base import Estimate
from .stations import Station, station_for

NWS_API = "https://api.weather.gov"

# Assumed uncertainty, in degrees F, between the value derived from the API and
# the whole-degree value the climate report publishes. ASOS instruments report
# in whole degrees F natively and the API round-trips through Celsius, so the
# realistic disagreement is small -- but it is not zero, and a flip across a
# half-degree changes which bracket wins. Raise it to trade more conservatively;
# every increase costs signals, since a wider band straddles more brackets.
BOUNDARY_MARGIN_F = 0.35


class WeatherError(RuntimeError):
    pass


def user_agent() -> str:
    """User-Agent for NWS requests, including a contact address.

    The NWS API asks callers to identify themselves with a contact address and
    reserves the right to block traffic that does not. Read at call time rather
    than at import so setting the variable does not require a reimport.
    """
    contact = os.environ.get("CIPHER_CONTACT", "").strip()
    if not contact:
        raise WeatherError(
            "set CIPHER_CONTACT to an email address or URL before calling the NWS API -- "
            "they ask callers to identify themselves and may block anonymous traffic"
        )
    return f"cipher-scanner/0.1 ({contact})"


@dataclass(frozen=True)
class Observation:
    observed_at: datetime
    temperature_f: float
    quality: str = "V"

    @property
    def usable(self) -> bool:
        # NWS quality flags: V/C are validated/coarse-checked; others are suspect.
        return self.quality in ("V", "C")


def to_fahrenheit(celsius: float) -> float:
    return celsius * 9.0 / 5.0 + 32.0


def fetch_observations(
    station_id: str,
    *,
    since: datetime,
    timeout: float = 10.0,
    agent: str | None = None,
) -> list[Observation]:
    """Pull validated temperature observations for a station since ``since``."""
    agent = agent or user_agent()
    query = urllib.parse.urlencode(
        {"start": since.astimezone(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"), "limit": 200}
    )
    url = f"{NWS_API}/stations/{station_id}/observations?{query}"
    request = urllib.request.Request(
        url, headers={"Accept": "application/geo+json", "User-Agent": agent}
    )
    try:
        with urllib.request.urlopen(request, timeout=timeout) as response:
            payload = json.loads(response.read().decode())
    except (urllib.error.URLError, TimeoutError, json.JSONDecodeError) as exc:
        raise WeatherError(f"NWS observations for {station_id} failed: {exc}") from exc
    return parse_observations(payload)


def parse_observations(payload: dict) -> list[Observation]:
    """Normalise a GeoJSON observations payload, oldest first."""
    out: list[Observation] = []
    for feature in payload.get("features", []):
        properties = feature.get("properties", {})
        temperature = (properties.get("temperature") or {}).get("value")
        stamp = properties.get("timestamp")
        if temperature is None or not stamp:
            continue
        out.append(
            Observation(
                observed_at=datetime.fromisoformat(stamp.replace("Z", "+00:00")),
                temperature_f=to_fahrenheit(float(temperature)),
                quality=(properties.get("temperature") or {}).get("qualityControl", "V"),
            )
        )
    return sorted(out, key=lambda o: o.observed_at)


def climatological_day_start(station: Station, now: datetime | None = None) -> datetime:
    """UTC instant of local midnight for the station's current climatological day."""
    zone = ZoneInfo(station.timezone)
    local_now = (now or utcnow()).astimezone(zone)
    local_midnight = local_now.replace(hour=0, minute=0, second=0, microsecond=0)
    return local_midnight.astimezone(timezone.utc)


def hours_past_peak(station: Station, now: datetime | None = None) -> float:
    """Hours since the station's typical daily maximum. Negative means before."""
    local_now = (now or utcnow()).astimezone(ZoneInfo(station.timezone))
    return (local_now.hour + local_now.minute / 60.0) - station.typical_peak_hour


def observed_max(observations: list[Observation]) -> Observation | None:
    """Warmest usable reading, or None."""
    usable = [o for o in observations if o.usable]
    return max(usable, key=lambda o: o.temperature_f) if usable else None


# ---- how much upside is left in the day --------------------------------


def probability_rises_by(degrees_f: float, past_peak: float) -> float:
    """P(the day's max ends at least ``degrees_f`` above what has been observed).

    A deliberately simple, legible prior rather than a fitted model: the honest
    state of things is that nobody has station-level empirical distributions
    here yet, and a transparent heuristic that can be checked by eye beats a
    fitted curve nobody can audit. Replace it with per-station empirics once the
    journal has a season of data -- that replacement is the single highest-value
    improvement available to this resolver.

    Shape: the chance of any further rise falls off through and after the
    afternoon peak, and the size of a rise, given one happens, shrinks with it.
    """
    if degrees_f <= 0:
        return 1.0

    # Probability the max increases at all from here. Logistic through the peak:
    # ~0.95 well before, ~0.5 at peak, small and shrinking after.
    p_any = 1.0 / (1.0 + math.exp(1.15 * (past_peak + 0.4)))
    # Past sunset the day is done; taper hard rather than leaving a fat tail.
    if past_peak > 5.0:
        p_any *= math.exp(-(past_peak - 5.0))

    # Given a rise happens, its magnitude decays; the decay is faster later.
    scale = max(0.45, 2.6 * math.exp(-0.42 * max(past_peak, 0.0)))
    return p_any * math.exp(-(degrees_f - 1.0) / scale)


# ---- bracket arithmetic ------------------------------------------------


@dataclass(frozen=True)
class Bracket:
    """The YES condition of a high-temperature market, in whole degrees F."""

    floor_f: float | None  # inclusive lower bound; None = unbounded below
    cap_f: float | None  # inclusive upper bound; None = unbounded above

    def contains(self, value: float) -> bool:
        if self.floor_f is not None and value < self.floor_f:
            return False
        if self.cap_f is not None and value > self.cap_f:
            return False
        return True

    def nearest_boundary_distance(self, value: float) -> float:
        """Distance to the nearest strike edge, in degrees."""
        bounds = [b for b in (self.floor_f, self.cap_f) if b is not None]
        if not bounds:
            return math.inf
        # Boundaries sit between whole degrees: "72 to 73" excludes 73.5 upward.
        return min(abs(value - (b + 0.5)) for b in bounds)


def bracket_for(market: Market) -> Bracket | None:
    """Read the YES condition off the market's structured strike fields."""
    kind = (market.strike_type or "").lower()
    if kind in ("between", "range"):
        if market.floor_strike is None or market.cap_strike is None:
            return None
        return Bracket(float(market.floor_strike), float(market.cap_strike))
    if kind in ("greater", "greater_or_equal"):
        if market.floor_strike is None:
            return None
        # "above 90" resolves YES at 91+ in whole-degree terms.
        floor = float(market.floor_strike) + (0.0 if kind.endswith("equal") else 1.0)
        return Bracket(floor, None)
    if kind in ("less", "less_or_equal"):
        if market.cap_strike is None:
            return None
        cap = float(market.cap_strike) - (0.0 if kind.endswith("equal") else 1.0)
        return Bracket(None, cap)
    return None


def plausible_rounded_values(raw_f: float, margin_f: float = BOUNDARY_MARGIN_F) -> list[int]:
    """Whole-degree values the climate report might plausibly publish.

    The API gives Celsius; the report publishes whole Fahrenheit. Conversion and
    the station's own rounding can disagree by a fraction of a degree, so a raw
    78.08F might be published as 78 -- or, if the instrument read a touch
    higher, as 79. Enumerating the candidates is better than refusing outright:
    if every candidate implies the same answer, the ambiguity does not matter
    and the signal is perfectly safe to take.
    """
    low, high = round(raw_f - margin_f), round(raw_f + margin_f)
    return list(range(min(low, high), max(low, high) + 1))


def probability_of_bracket(
    bracket: Bracket, current_max_f: float, past_peak: float
) -> tuple[float, bool, str]:
    """P(YES), whether it is deterministic, and a one-line rationale.

    Only the "already ruled out" case is deterministic -- see the module
    docstring on why the inequality points one way.
    """
    rounded = round(current_max_f)

    # Case 1: the observed max already exceeds the ceiling. Cannot come back
    # down, and the reported max is >= the observed max. Genuinely settled.
    if bracket.cap_f is not None and rounded > bracket.cap_f:
        return (
            0.0,
            True,
            f"observed max {rounded}F already above the {bracket.cap_f:.0f}F ceiling",
        )

    # Case 2: the observed max already clears the floor of an open-topped
    # bracket. Same inequality, same certainty, opposite direction.
    if bracket.floor_f is not None and bracket.cap_f is None and rounded >= bracket.floor_f:
        return (
            1.0,
            True,
            f"observed max {rounded}F already at or above the {bracket.floor_f:.0f}F floor",
        )

    # Case 3: sitting inside the bracket. YES unless the day still has enough
    # left in it to climb out of the top.
    if bracket.contains(rounded):
        if bracket.cap_f is None:
            return 1.0, True, f"observed max {rounded}F inside an open-topped bracket"
        headroom = bracket.cap_f - rounded
        p_escape = probability_rises_by(headroom + 1.0, past_peak)
        return (
            1.0 - p_escape,
            False,
            f"observed max {rounded}F is inside the bracket with {headroom:.0f}F of headroom",
        )

    # Case 4: still below the bracket. Needs further warming, and the observation
    # feed may already be understating the true max. Weakest case by far.
    assert bracket.floor_f is not None
    needed = bracket.floor_f - rounded
    p_reaches = probability_rises_by(needed, past_peak)
    if bracket.cap_f is None:
        return p_reaches, False, f"needs {needed:.0f}F more to reach the floor"
    p_overshoots = probability_rises_by(bracket.cap_f - rounded + 1.0, past_peak)
    return (
        max(0.0, p_reaches - p_overshoots),
        False,
        f"needs {needed:.0f}F more, and must stop below {bracket.cap_f:.0f}F",
    )


# ---- the resolver ------------------------------------------------------


class WeatherResolver:
    """Resolver for Kalshi daily high-temperature series."""

    name = "nws-daily-high"

    def __init__(
        self,
        *,
        fetcher=fetch_observations,
        require_verified_station: bool = True,
        min_hours_past_peak: float = 0.5,
        max_rounding_disagreement: float = 0.05,
    ):
        self._fetch = fetcher
        self.require_verified_station = require_verified_station
        # How far the plausible roundings may disagree before the reading is
        # treated as sitting on a boundary.
        self.max_rounding_disagreement = max_rounding_disagreement
        # Before the peak the day has most of its heating left; there is no
        # observational edge to have.
        self.min_hours_past_peak = min_hours_past_peak
        self._cache: dict[tuple[str, str], list[Observation]] = {}

    def handles(self, market: Market) -> bool:
        return station_for(market.ticker) is not None and bracket_for(market) is not None

    def estimate(self, market: Market, now: datetime | None = None) -> Estimate | None:
        now = now or utcnow()
        station = station_for(market.ticker)
        bracket = bracket_for(market)
        if station is None or bracket is None:
            return None

        past_peak = hours_past_peak(station, now)
        if past_peak < self.min_hours_past_peak:
            return None

        observations = self._observations(station, now)
        peak = observed_max(observations)
        if peak is None:
            return None

        # Evaluate every whole-degree value the report might plausibly publish.
        # If they disagree about the outcome, the reading is sitting on a
        # rounding boundary and there is nothing tradeable here. If they agree,
        # the ambiguity is irrelevant and we take the most conservative reading.
        candidates = [
            probability_of_bracket(bracket, float(value), past_peak)
            for value in plausible_rounded_values(peak.temperature_f)
        ]
        probabilities = [c[0] for c in candidates]
        if max(probabilities) - min(probabilities) > self.max_rounding_disagreement:
            return None

        # Most conservative candidate: the one furthest from a confident call.
        probability, deterministic, rationale = min(
            candidates, key=lambda c: abs(c[0] - 0.5)
        )
        # Determinism only survives if every candidate agrees on it.
        deterministic = deterministic and all(c[1] for c in candidates)

        if deterministic and self.require_verified_station and not station.verified:
            # Downgrade rather than discard: the reading is probably right, but
            # an unconfirmed station mapping does not earn a certainty claim.
            deterministic = False

        return Estimate(
            probability=probability,
            confidence=self._confidence(deterministic, past_peak, station),
            source=f"{self.name}:{station.station_id}",
            # Staleness is measured from the observation, not from the fetch.
            # An hourly feed retrieved a second ago can still be 50 minutes old.
            observed_at=peak.observed_at,
            rationale=f"{station.label}: {rationale}",
            deterministic=deterministic,
            detail={
                "station": station.station_id,
                "station_verified": station.verified,
                "observed_max_f": round(peak.temperature_f, 1),
                "observed_max_rounded_f": round(peak.temperature_f),
                "observed_at": peak.observed_at.isoformat(),
                "hours_past_peak": round(past_peak, 2),
                "observations_used": len(observations),
            },
        )

    def _confidence(self, deterministic: bool, past_peak: float, station: Station) -> float:
        if deterministic:
            return 0.99
        # Confidence in the *model* grows as the day runs out of heating.
        confidence = 1.0 - math.exp(-0.5 * max(past_peak, 0.0))
        if not station.verified:
            confidence *= 0.8
        return max(0.0, min(0.95, confidence))

    def _observations(self, station: Station, now: datetime) -> list[Observation]:
        day_start = climatological_day_start(station, now)
        key = (station.station_id, day_start.isoformat())
        cached = self._cache.get(key)
        # Re-fetch when the newest reading is over 10 minutes old.
        if cached and cached[-1].observed_at > now - timedelta(minutes=10):
            return cached
        fetched = self._fetch(station.station_id, since=day_start)
        within_day = [o for o in fetched if o.observed_at >= day_start]
        self._cache[key] = within_day
        return within_day
