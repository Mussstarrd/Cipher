"""Knowledge base: aggregates all benchmark runs and renders the leaderboard.

The knowledge base is the loop's memory. It consists of:
  knowledge/results/*.jsonl   raw benchmark records from every generation
  knowledge/learnings/*.md    prose findings written by agents after each run
  knowledge/LEADERBOARD.md    generated summary (this module renders it)

Usage:
    python -m lab.knowledge            # print + write leaderboard
    python -m lab.knowledge --best     # best strategy per file only
"""

from __future__ import annotations

import argparse
import glob
import json
import os
from collections import defaultdict

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RESULTS_DIR = os.path.join(REPO, "knowledge", "results")
LEADERBOARD = os.path.join(REPO, "knowledge", "LEADERBOARD.md")


def load_records() -> list[dict]:
    records = []
    for path in sorted(glob.glob(os.path.join(RESULTS_DIR, "*.jsonl"))):
        with open(path) as f:
            for line in f:
                if line.strip():
                    records.append(json.loads(line))
    # Keep only the latest record per (strategy, file).
    latest: dict[tuple, dict] = {}
    for rec in records:
        latest[(rec["strategy"], rec["file"])] = rec
    return list(latest.values())


def render(best_only: bool = False) -> str:
    records = [r for r in load_records() if r.get("ok")]
    if not records:
        return "No successful results yet.\n"

    by_file: dict[str, list[dict]] = defaultdict(list)
    for r in records:
        by_file[r["file"]].append(r)

    lines = ["# Leaderboard", ""]

    # Overall: weighted total compressed size across the whole corpus.
    totals: dict[str, dict] = defaultdict(lambda: {"orig": 0, "comp": 0, "files": 0})
    n_files = len(by_file)
    for r in records:
        t = totals[r["strategy"]]
        t["orig"] += r["orig_bytes"]
        t["comp"] += r["comp_bytes"]
        t["files"] += 1
    complete = {s: t for s, t in totals.items() if t["files"] == n_files}
    if complete:
        lines += ["## Overall (total corpus, complete strategies only)", "",
                  "| rank | strategy | total ratio | saved |", "|---|---|---|---|"]
        ranked = sorted(complete.items(), key=lambda kv: kv[1]["comp"])
        for i, (s, t) in enumerate(ranked, 1):
            lines.append(
                f"| {i} | {s} | {t['orig']/t['comp']:.3f}x | {100*(1-t['comp']/t['orig']):.2f}% |"
            )
        lines.append("")

    lines += ["## Per file", ""]
    for fname in sorted(by_file):
        rows = sorted(by_file[fname], key=lambda r: r["comp_bytes"])
        if best_only:
            rows = rows[:3]
        lines += [f"### {fname}", "",
                  "| strategy | ratio | saved | comp MB/s | decomp MB/s | tag |",
                  "|---|---|---|---|---|---|"]
        for r in rows:
            lines.append(
                f"| {r['strategy']} | {r['ratio']:.3f}x | {r['saved_pct']:.2f}% "
                f"| {r['comp_mbps']:.1f} | {r['decomp_mbps']:.1f} | {r.get('tag','')} |"
            )
        lines.append("")
    return "\n".join(lines) + "\n"


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--best", action="store_true", help="top 3 per file only")
    args = ap.parse_args()
    text = render(best_only=args.best)
    os.makedirs(os.path.dirname(LEADERBOARD), exist_ok=True)
    with open(LEADERBOARD, "w") as f:
        f.write(text)
    print(text)


if __name__ == "__main__":
    main()
