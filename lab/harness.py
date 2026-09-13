"""Benchmark harness: runs strategies over the corpus, verifies losslessness,
and appends results to the knowledge base.

Usage:
    python -m lab.harness --tag gen0 [--only zstd-19,brotli-11] [--agent NAME]

Each run writes knowledge/results/<tag>.jsonl (one record per strategy x file),
so parallel agents can benchmark concurrently without write races as long as
they use distinct tags.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import time
import traceback

from . import corpus
from .strategies import load_all

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RESULTS_DIR = os.path.join(REPO, "knowledge", "results")


def run_one(strategy, name: str, data: bytes) -> dict:
    rec = {
        "strategy": strategy.name,
        "file": name,
        "orig_bytes": len(data),
        "ok": False,
    }
    try:
        t0 = time.perf_counter()
        blob = strategy.compress(data)
        t1 = time.perf_counter()
        out = strategy.decompress(blob)
        t2 = time.perf_counter()
        if out != data:
            rec["error"] = "roundtrip mismatch (LOSSY OR BROKEN — disqualified)"
            return rec
        rec.update(
            ok=True,
            comp_bytes=len(blob),
            ratio=round(len(data) / len(blob), 4),
            saved_pct=round(100 * (1 - len(blob) / len(data)), 2),
            comp_mbps=round(len(data) / (t1 - t0) / 1e6, 2),
            decomp_mbps=round(len(data) / (t2 - t1) / 1e6, 2),
        )
    except Exception:
        rec["error"] = traceback.format_exc(limit=3).strip().splitlines()[-1]
    return rec


def run(tag: str, only: list[str] | None = None, agent: str = "") -> list[dict]:
    corpus_dir = corpus.build()
    strategies = load_all()
    if only:
        missing = set(only) - set(strategies)
        if missing:
            raise SystemExit(f"unknown strategies: {sorted(missing)}; known: {sorted(strategies)}")
        strategies = {k: v for k, v in strategies.items() if k in only}

    os.makedirs(RESULTS_DIR, exist_ok=True)
    out_path = os.path.join(RESULTS_DIR, f"{tag}.jsonl")
    records = []
    with open(out_path, "a") as out:
        for fname in sorted(corpus.FILES):
            with open(os.path.join(corpus_dir, fname), "rb") as f:
                data = f.read()
            digest = hashlib.sha256(data).hexdigest()[:12]
            for sname in sorted(strategies):
                rec = run_one(strategies[sname], fname, data)
                rec.update(tag=tag, agent=agent, corpus_sha=digest, ts=round(time.time(), 1))
                records.append(rec)
                out.write(json.dumps(rec) + "\n")
                status = f"{rec['ratio']:.3f}x" if rec["ok"] else f"FAIL: {rec.get('error')}"
                print(f"  {sname:28s} {fname:12s} {status}")
    return records


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--tag", required=True, help="results file tag, e.g. gen1-media")
    ap.add_argument("--only", help="comma-separated strategy names to run")
    ap.add_argument("--agent", default="", help="name of the agent submitting this run")
    args = ap.parse_args()
    run(args.tag, args.only.split(",") if args.only else None, args.agent)


if __name__ == "__main__":
    main()
