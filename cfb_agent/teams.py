"""Canonical team registry, keyed by team id.

The load-bearing fact: **CFBD team ids and ESPN team ids are the same
namespace.** Verified for 2026 — all 138 FBS teams and 667 teams overall share
an id across both providers, and CFBD's `/games` and `/lines` rows are keyed by
the ESPN event id. So ESPN games, ESPN odds, ESPN FPI and CFBD lines all join on
integer ids with no name matching whatsoever.

Two feeds still arrive name-keyed and must be resolved:

  * CFBD `/ratings/sp` and `/talent` — carry CFBD's own `school` string.
  * The Odds API — carries free-text display names ("Alabama Crimson Tide").

For those, resolution is *exact after canonicalization* (case, accents,
punctuation and "&"/"and" folded) against an explicit lookup built from the
provider registries, plus a hand-written ALIASES table for the irregulars.

There is deliberately **no fuzzy fallback**. Team-name matching is the single
biggest source of silent, catastrophic error in a betting model: a fuzzy match
of "Miami" onto Miami (OH) does not fail loudly, it just prices a game against
the wrong team and produces a large, confident, wrong edge. An unrecognized
name raises `UnmappedTeam` and the caller drops the row and reports it.
"""

import json
import re
import unicodedata
from typing import Iterable, Optional

from . import config, http

CFBD_TEAMS = "https://api.collegefootballdata.com/teams"
ESPN_TEAMS = "https://site.api.espn.com/apis/site/v2/sports/football/college-football/teams"

# Ranked best-to-worst; used to break lookup collisions deterministically.
_CLASS_RANK = {"fbs": 0, "fcs": 1, "ii": 2, "iii": 3, None: 4}

# Canonicalized name -> team id, for names no provider registry generates.
# Every entry here is a deliberate, checked decision; keep the reason visible.
ALIASES: dict[str, int] = {
    "albany": 399,                          # Odds API's bare "Albany" = UAlbany
    "citadel bulldogs": 2643,               # registry has "The Citadel"
    "citadel": 2643,
    "southeastern louisiana lions": 2545,   # registry alt is "Southeast Louisiana"
    "southeastern louisiana": 2545,
    "nicholls state colonels": 2447,        # registry dropped the "State"
    "nicholls state": 2447,
    "houston baptist huskies": 2277,        # renamed Houston Christian in 2022
    "houston baptist": 2277,
    "ut rio grande valley vaqueros": 292,
    "louisiana monroe": 2433,               # UL Monroe / Louisiana-Monroe
    "la monroe": 2433,
    "louisiana lafayette": 309,             # UL Lafayette, a different school
    "miami fl": 2390,                       # the Hurricanes
    "miami florida": 2390,
    "miami oh": 193,                        # the RedHawks
    "miami redhawks": 193,                  # never let this fall to Miami (FL)
    "miami ohio": 193,
    "southern california": 30,              # USC
    "mississippi": 145,                     # Ole Miss (NOT Mississippi State)
    "san jose state": 23,                   # accent-folded, kept explicit
}

# Collisions we resolve by hand rather than by classification rank.
FORCED: dict[str, int] = {
    "charlotte": 2429,   # FBS 49ers, not the NAIA "Charlotte Saints"
    "troy": 2653,        # FBS Trojans, not "Troy Vikings"
}


class UnmappedTeam(KeyError):
    """A team name could not be resolved to an id. Never guess — surface it."""


def canon(name: Optional[str]) -> str:
    """Fold a team name to its canonical comparison key."""
    s = unicodedata.normalize("NFKD", name or "")
    s = "".join(c for c in s if not unicodedata.combining(c))
    s = s.lower().replace("&", " and ").replace("'", "")
    s = re.sub(r"[^a-z0-9]+", " ", s)
    return re.sub(r"\s+", " ", s).strip()


class Registry:
    """team id -> identity, plus an exact name -> id lookup."""

    def __init__(self, teams: dict[int, dict], lookup: dict[str, int]):
        self.teams = teams
        self.lookup = lookup

    # --- construction -------------------------------------------------------
    @classmethod
    def build(cls, season: int) -> "Registry":
        """Fetch both provider registries and fuse them into one id-keyed map."""
        teams: dict[int, dict] = {}

        cfbd_rows = []
        if config.CFBD_API_KEY:
            cfbd_rows = http.get_json(
                CFBD_TEAMS,
                params={"year": season},
                headers={"Authorization": f"Bearer {config.CFBD_API_KEY}"},
                ttl=30 * 24 * 3600,
            )
        for t in cfbd_rows:
            if t.get("id") is None:
                continue
            teams[int(t["id"])] = {
                "id": int(t["id"]),
                "school": t.get("school"),
                "mascot": t.get("mascot"),
                "abbreviation": t.get("abbreviation"),
                "conference": t.get("conference"),
                "classification": t.get("classification"),
                "alt_names": [a for a in (t.get("alternateNames") or []) if a],
                "espn_name": None,
                "espn_location": None,
                "espn_short": None,
            }

        for t in _espn_teams():
            try:
                tid = int(t["id"])
            except (KeyError, TypeError, ValueError):
                continue
            rec = teams.setdefault(tid, {
                "id": tid, "school": t.get("location"), "mascot": t.get("name"),
                "abbreviation": t.get("abbreviation"), "conference": None,
                "classification": None, "alt_names": [],
            })
            rec["espn_name"] = t.get("displayName")
            rec["espn_location"] = t.get("location")
            rec["espn_short"] = t.get("shortDisplayName")

        return cls(teams, _build_lookup(teams))

    @classmethod
    def load(cls, season: int, refresh: bool = False) -> "Registry":
        """Load the cached registry snapshot, rebuilding it if asked/missing."""
        config.ensure_dirs()
        if config.REGISTRY_PATH.exists() and not refresh:
            blob = json.loads(config.REGISTRY_PATH.read_text(encoding="utf-8"))
            if blob.get("season") == season:
                teams = {int(k): v for k, v in blob["teams"].items()}
                return cls(teams, _build_lookup(teams))
        reg = cls.build(season)
        reg.save(season)
        return reg

    def save(self, season: int) -> None:
        config.ensure_dirs()
        config.REGISTRY_PATH.write_text(
            json.dumps({"season": season, "teams": self.teams}, indent=1, sort_keys=True),
            encoding="utf-8",
        )

    # --- resolution ---------------------------------------------------------
    def resolve(self, name: str) -> int:
        """Name -> team id. Raises UnmappedTeam rather than guessing."""
        key = canon(name)
        tid = self.lookup.get(key)
        if tid is None:
            raise UnmappedTeam(f"no team id for {name!r} (canonical {key!r})")
        return tid

    def try_resolve(self, name: str) -> Optional[int]:
        try:
            return self.resolve(name)
        except UnmappedTeam:
            return None

    def resolve_all(self, names: Iterable[str]) -> tuple[dict[str, int], list[str]]:
        """Bulk resolve. Returns (name -> id, sorted list of unmapped names)."""
        out: dict[str, int] = {}
        missing: set[str] = set()
        for n in names:
            tid = self.try_resolve(n)
            if tid is None:
                missing.add(n)
            else:
                out[n] = tid
        return out, sorted(missing)

    # --- display ------------------------------------------------------------
    def name(self, tid: int) -> str:
        t = self.teams.get(int(tid))
        if not t:
            return f"team:{tid}"
        return t.get("school") or t.get("espn_name") or f"team:{tid}"

    def display(self, tid: int) -> str:
        t = self.teams.get(int(tid)) or {}
        return t.get("espn_name") or self.name(tid)

    def classification(self, tid: int) -> Optional[str]:
        return (self.teams.get(int(tid)) or {}).get("classification")

    def is_fbs(self, tid: int) -> bool:
        return self.classification(tid) == "fbs"


def _espn_teams() -> list[dict]:
    data = http.get_json(ESPN_TEAMS, params={"limit": 1000}, ttl=30 * 24 * 3600)
    out = []
    for sport in data.get("sports", []):
        for league in sport.get("leagues", []):
            for entry in league.get("teams", []):
                if entry.get("team"):
                    out.append(entry["team"])
    return out


def _build_lookup(teams: dict[int, dict]) -> dict[str, int]:
    """Exact-match lookup. Ambiguous keys are dropped, never guessed at."""
    lut: dict[str, int] = {}
    ambiguous: set[str] = set()

    def add(raw: Optional[str], tid: int) -> None:
        key = canon(raw)
        if not key or key in ambiguous:
            return
        prev = lut.get(key)
        if prev is None or prev == tid:
            lut[key] = tid
            return
        # Collision: prefer the higher classification, else refuse the key.
        rank_prev = _CLASS_RANK.get((teams.get(prev) or {}).get("classification"), 4)
        rank_new = _CLASS_RANK.get((teams.get(tid) or {}).get("classification"), 4)
        if rank_new < rank_prev:
            lut[key] = tid
        elif rank_new == rank_prev:
            del lut[key]
            ambiguous.add(key)

    for tid, t in teams.items():
        mascot = t.get("mascot") or ""
        bases = [t.get("school"), t.get("espn_name"), t.get("espn_location"),
                 t.get("espn_short"), *t.get("alt_names", [])]
        for b in bases:
            add(b, tid)
            # Providers routinely spell a team as "<some name> <mascot>" (the
            # Odds API does this exclusively). Generating the combos keeps
            # matching exact instead of pushing us toward fuzzy suffix-stripping.
            if b and mascot:
                add(f"{b} {mascot}", tid)

    for key, tid in FORCED.items():
        lut[canon(key)] = tid
        ambiguous.discard(canon(key))
    for key, tid in ALIASES.items():
        lut[canon(key)] = tid
    return lut
