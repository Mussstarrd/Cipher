"""Append-only signal journal and calibration scoring.

The reason this module exists on day one rather than day ninety: a scanner that
emits confident probabilities is trivial to build and impossible to evaluate by
eye. Until every signal is written down *before* the outcome is known and scored
afterwards, "decent level of certainty" is a feeling, not a measurement.

Two numbers decide whether any of this is real:

* **Brier score** -- mean squared error of the probabilities. Lower is better;
  0.25 is what you get by saying 50% to everything. A scanner whose Brier score
  does not beat the *market price at signal time* has no edge, however good its
  hit rate looks.
* **Calibration table** -- of the signals where the scanner said 95%, did 95%
  happen? Systematic overconfidence at the high end is the failure mode that
  matters here, because that is precisely where the strategy puts its money.

Storage is JSONL, one record per line, opened in append mode. Dull on purpose:
the journal must survive the process crashing mid-scan.
"""

from __future__ import annotations

import json
import os
from collections import defaultdict
from dataclasses import dataclass
from pathlib import Path

from .model import utcnow
from .signal import Signal

DEFAULT_PATH = Path(os.environ.get("CIPHER_JOURNAL", "data/journal.jsonl"))


class Journal:
    """Append-only record of signals and their eventual outcomes."""

    def __init__(self, path: Path | str = DEFAULT_PATH):
        self.path = Path(path)
        self.path.parent.mkdir(parents=True, exist_ok=True)

    def record(self, signal: Signal, *, traded: bool = False) -> str:
        """Write a signal. Returns its id so an outcome can be attached later."""
        row = {
            "type": "signal",
            "logged_at": utcnow().isoformat(),
            "traded": traded,
            **signal.to_dict(),
        }
        self._append(row)
        return signal.signal_id

    def record_outcome(
        self,
        signal_id: str,
        *,
        settled_yes: bool,
        fill_price_cents: int | None = None,
        note: str = "",
    ) -> None:
        """Attach a settlement result to a previously recorded signal."""
        self._append(
            {
                "type": "outcome",
                "logged_at": utcnow().isoformat(),
                "signal_id": signal_id,
                "settled_yes": settled_yes,
                "fill_price_cents": fill_price_cents,
                "note": note,
            }
        )

    def _append(self, row: dict) -> None:
        with self.path.open("a", encoding="utf-8") as handle:
            handle.write(json.dumps(row, default=str) + "\n")

    def read(self) -> list[dict]:
        if not self.path.exists():
            return []
        rows = []
        with self.path.open(encoding="utf-8") as handle:
            for line in handle:
                line = line.strip()
                if line:
                    rows.append(json.loads(line))
        return rows


@dataclass
class Scored:
    """One signal joined to its settled outcome."""

    signal_id: str
    scanner: str
    side: str
    probability: float
    price_cents: int
    contracts: int
    traded: bool
    won: bool

    @property
    def market_probability(self) -> float:
        """What the book implied at signal time -- the benchmark to beat."""
        return self.price_cents / 100


def join(rows: list[dict]) -> list[Scored]:
    """Pair signals with outcomes. Signals without an outcome are dropped."""
    signals = {r["signal_id"]: r for r in rows if r.get("type") == "signal"}
    outcomes = {r["signal_id"]: r for r in rows if r.get("type") == "outcome"}

    scored: list[Scored] = []
    for signal_id, outcome in outcomes.items():
        signal = signals.get(signal_id)
        if signal is None:
            continue
        settled_yes = bool(outcome["settled_yes"])
        won = settled_yes if signal["side"] == "yes" else not settled_yes
        scored.append(
            Scored(
                signal_id=signal_id,
                scanner=signal["scanner"],
                side=signal["side"],
                probability=float(signal["probability"]),
                price_cents=int(signal["price_cents"]),
                contracts=int(signal["contracts"]),
                traded=bool(signal.get("traded")),
                won=won,
            )
        )
    return scored


def brier(scored: list[Scored], *, use_market: bool = False) -> float | None:
    """Mean squared error of the stated probabilities. None if no data."""
    if not scored:
        return None
    total = 0.0
    for s in scored:
        p = s.market_probability if use_market else s.probability
        total += (p - (1.0 if s.won else 0.0)) ** 2
    return total / len(scored)


def calibration_table(scored: list[Scored], buckets: int = 10) -> list[dict]:
    """Predicted vs realised frequency, bucketed by stated probability."""
    grouped: dict[int, list[Scored]] = defaultdict(list)
    for s in scored:
        index = min(buckets - 1, int(s.probability * buckets))
        grouped[index].append(s)

    table = []
    for index in sorted(grouped):
        rows = grouped[index]
        table.append(
            {
                "bucket": f"{index / buckets:.0%}-{(index + 1) / buckets:.0%}",
                "n": len(rows),
                "predicted": sum(r.probability for r in rows) / len(rows),
                "realised": sum(1 for r in rows if r.won) / len(rows),
            }
        )
    return table


def realised_pnl_cents(scored: list[Scored], *, traded_only: bool = True) -> int:
    """Settlement P&L in cents, fees included, for signals marked traded."""
    from .fees import schedule_for

    total = 0
    for s in scored:
        if traded_only and not s.traded:
            continue
        # Ticker is not carried on Scored; the default schedule is right for
        # every series that has not been overridden.
        schedule = schedule_for(s.scanner)
        payout = 100 if s.won else 0
        total += (payout - s.price_cents) * s.contracts
        total -= schedule.round_trip_cost_cents(s.price_cents, s.contracts)
    return total


def summarise(rows: list[dict]) -> dict:
    """Everything needed to answer 'is this scanner actually working?'"""
    scored = join(rows)
    by_scanner: dict[str, list[Scored]] = defaultdict(list)
    for s in scored:
        by_scanner[s.scanner].append(s)

    return {
        "signals": sum(1 for r in rows if r.get("type") == "signal"),
        "settled": len(scored),
        "brier_model": brier(scored),
        "brier_market": brier(scored, use_market=True),
        "hit_rate": (sum(1 for s in scored if s.won) / len(scored)) if scored else None,
        "pnl_cents": realised_pnl_cents(scored),
        "calibration": calibration_table(scored),
        "by_scanner": {
            name: {
                "n": len(rows_),
                "brier_model": brier(rows_),
                "brier_market": brier(rows_, use_market=True),
                "hit_rate": sum(1 for r in rows_ if r.won) / len(rows_),
            }
            for name, rows_ in sorted(by_scanner.items())
        },
    }
