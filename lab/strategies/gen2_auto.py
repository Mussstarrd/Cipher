"""Generation 2 — INTEGRATION: the "just works" router strategy.

gen2.auto turns the gen1 research wins into a single strategy that is safe to
point at arbitrary bytes.  Per input it runs the compressors of the best known
gen1 pipelines plus a plain zstd-19 fallback, keeps the smallest blob, and
prefixes a one-byte mode tag so decompression can dispatch back to the right
pipeline:

  mode 0  zstd-19 of the raw input        (universal fallback)
  mode 1  gen1.media-auto container       (audio LPC / PNG-filter / zstd)
  mode 2  gen1.struct-logs container      (columnar JSONL logs)
  mode 3  gen1.struct-csv container       (columnar numeric CSV)
  mode 4  gen1.text-token container       (word tokenizer + best-of backends)

By construction the result is min(candidates) + 1 byte, so gen2.auto matches
every per-file leaderboard best to within one byte.  The gen1 strategies are
imported and called through the shared registry — their modules are never
copied or edited (ORCHESTRATOR.md rule 2).

Correctness: each gen1 candidate is itself a self-describing lossless
container (all were verified lossless on all 7 corpus files by the gen1
harness runs).  Belt-and-braces, compress() additionally verifies the winning
candidate's roundtrip before committing to it and falls back to the zstd mode
on any mismatch or exception — the fancy path can therefore never break
losslessness, only lose a size race.  The extra decompress roughly doubles
worst-case compression time; decompression speed is untouched.

Known cost (measured, see knowledge/learnings/gen2_integration.md): running
four content-aware compressors + fallback on every input makes compression
the sum of all candidates' times (~0.05-0.2 MB/s on this corpus).  That is
the price of "best of everything, no configuration"; pass an explicit
strategy to lab/cipher.py when speed matters.
"""

from __future__ import annotations

import zstandard

from . import FuncStrategy, register

# Importing the gen1 modules both registers them (idempotent under the
# package's import cache) and lets us reference their strategy objects even
# when gen2_auto is imported before load_all() has run.
from . import gen1_media, gen1_structured, gen1_text  # noqa: F401  (side effects)
from . import _REGISTRY

_ZC = zstandard.ZstdCompressor(level=19)
_ZD = zstandard.ZstdDecompressor()

# mode byte -> registered strategy name
_MODES = {
    1: "gen1.media-auto",
    2: "gen1.struct-logs",
    3: "gen1.struct-csv",
    4: "gen1.text-token",
}


def _strategy(name: str):
    return _REGISTRY[name]


def _compress(data: bytes) -> bytes:
    best = b"\x00" + _ZC.compress(data)
    for mode, name in _MODES.items():
        try:
            blob = _strategy(name).compress(data)
        except Exception:
            continue
        if len(blob) + 1 < len(best):
            # verify before trusting a smaller fancy-path result
            try:
                if _strategy(name).decompress(blob) != data:
                    continue
            except Exception:
                continue
            best = bytes([mode]) + blob
    return best


def _decompress(blob: bytes) -> bytes:
    mode = blob[0]
    rest = bytes(blob[1:])
    if mode == 0:
        return _ZD.decompress(rest)
    name = _MODES.get(mode)
    if name is None:
        raise ValueError(f"unknown gen2.auto mode byte {mode}")
    return _strategy(name).decompress(rest)


register(FuncStrategy("gen2.auto", _compress, _decompress))
