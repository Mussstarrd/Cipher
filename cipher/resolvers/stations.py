"""Station registry for Kalshi daily high-temperature markets.

This file is the single most dangerous piece of the weather strategy, so it is
kept separate and loud about it.

A Kalshi high-temperature market does not settle on "the temperature in New
York". It settles on a **named NWS product for a named station** -- typically
the Daily Climate Report (CLI) for that station's climatological day. Central
Park (KNYC) and LaGuardia (KLGA) routinely differ by 2-4 degrees F, which is one
to two whole brackets. Trading the right city off the wrong station is how you
are exactly right about the weather and still lose.

Nothing here is verified against live market rules, because the environment this
was written in cannot reach Kalshi. Every entry therefore ships with
``verified=False``, and ``WeatherResolver`` refuses to emit deterministic
signals for an unverified station. Read the rulebook for the specific series,
confirm the station identifier and the settlement product, then flip the flag.
That check is not bureaucracy -- it is the entire difference between this being
a source-of-truth strategy and being a guess with extra steps.
"""

from __future__ import annotations

from dataclasses import dataclass


@dataclass(frozen=True)
class Station:
    """One NWS observing station backing one Kalshi series."""

    station_id: str  # NWS/ICAO identifier, e.g. "KNYC"
    timezone: str  # IANA zone; the climatological day is local, not UTC
    label: str
    # Local hour when this station typically reaches its daily max. Used only to
    # decide how much upside is left in the day, never to predict the max.
    typical_peak_hour: int = 16
    # Flip to True only after reading the series rulebook and confirming both
    # the station and the settlement product.
    verified: bool = False


# Candidate mappings for Kalshi's daily-high series. Series tickers and stations
# are best-effort and MUST be confirmed against each market's rules before use.
STATIONS: dict[str, Station] = {
    "KXHIGHNY": Station("KNYC", "America/New_York", "New York (Central Park)"),
    "KXHIGHCHI": Station("KMDW", "America/Chicago", "Chicago (Midway)"),
    "KXHIGHPHIL": Station("KPHL", "America/New_York", "Philadelphia"),
    "KXHIGHMIA": Station("KMIA", "America/New_York", "Miami"),
    "KXHIGHAUS": Station("KAUS", "America/Chicago", "Austin"),
    "KXHIGHDEN": Station("KDEN", "America/Denver", "Denver", typical_peak_hour=15),
    "KXHIGHLAX": Station("KLAX", "America/Los_Angeles", "Los Angeles", typical_peak_hour=14),
}


def station_for(ticker: str) -> Station | None:
    """Look up the station backing a market ticker, or None if unmapped."""
    return STATIONS.get(ticker.split("-", 1)[0].upper())


def verify(series: str, station_id: str, *, typical_peak_hour: int | None = None) -> Station:
    """Mark a series' station as confirmed against the published rules.

    Call this from your own configuration once you have read the rulebook. It
    requires you to restate the station id, so a mismatch surfaces here rather
    than in a losing trade.
    """
    existing = STATIONS.get(series.upper())
    if existing is None:
        raise KeyError(f"unknown series {series!r}; add it to STATIONS first")
    if existing.station_id != station_id.upper():
        raise ValueError(
            f"{series}: registry says {existing.station_id}, "
            f"you passed {station_id.upper()} -- resolve this before trading"
        )
    confirmed = Station(
        station_id=existing.station_id,
        timezone=existing.timezone,
        label=existing.label,
        typical_peak_hour=(
            typical_peak_hour if typical_peak_hour is not None else existing.typical_peak_hour
        ),
        verified=True,
    )
    STATIONS[series.upper()] = confirmed
    return confirmed
