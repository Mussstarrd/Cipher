# gen1 — STRUCTURED-DATA agent learnings

Strategies: `gen1.struct-logs`, `gen1.struct-csv` (module
`lab/strategies/gen1_structured.py`, results tag `gen1-structured`).
Both are lossless on all 7 corpus files (harness verified); non-matching
content falls back to plain zstd-19 behind a self-describing mode byte.

## Headline results

| file        | best gen0 baseline      | gen1 strategy       | bytes            | ratio            | vs baseline |
|-------------|-------------------------|---------------------|------------------|------------------|-------------|
| logs.jsonl  | bz2-9: 78,824 (6.651x)  | gen1.struct-logs    | 62,847           | **8.342x**       | 20.3% smaller |
| series.csv  | lzma-9e: 65,112 (6.039x)| gen1.struct-csv     | 34,688           | **11.336x**      | 46.7% smaller |

All other corpus files hit the fallback path and match zstd-19 (e.g.
random.bin 1.000x, text.txt 5.788x) — the mode byte costs ~4 bytes.

## What worked (with numbers)

1. **Columnar transposition + per-stream codec choice.** Split each line
   into per-field streams; compress each stream independently with the best
   of {raw, zstd-19, lzma-6, brotli-11} (1-byte codec tag per stream).
   Brotli-11 won almost every text/varint stream; raw won the random-hex
   stream and tiny dictionaries.
2. **Hex request_id → raw 16 bytes.** 2,844 ids: 91,008 B of hex text
   → 45,504 B raw (stored uncompressed; it is cryptographically random and
   codec-incompressible). This alone is why the whole-file LZ codecs stall:
   the ids are ~17% of the file but ~72% of gen1's compressed output —
   they were polluting the LZ window for everything else in gen0.
3. **Timestamps as integer deltas.** "%.3f" seconds parsed to integer
   milliseconds, delta + zigzag + varint: 2,845 timestamps → 2,245 B packed
   (~6.3 bits/line; the exponential inter-arrival entropy is ~5.8 bits, so
   close to the floor). In series.csv the timestamp column has constant
   delta 10 and collapses to 25 B total for 11,919 rows.
4. **Enums as index bytes.** level/service/status each become a
   first-appearance dictionary + one index byte per row; brotli entropy-codes
   the indices to ~1.9–2.7 bits/row (661/960/671 B per stream), near the
   distribution entropy.
5. **msg word tokenization** (biggest single iteration win on logs:
   70,182 → 62,847 B). msgs are words from a small vocabulary; as running
   text brotli needed 15,570 B, as vocabulary indices (dict 157 B + counts
   15 B + indices 8,062 B) it takes 8,234 B — 47% less.
6. **Fixed-point decimal columns for CSV.** Parse "%.3f"/"%.4f" text to
   scaled integers (decimals detected per column from the first row),
   zigzag-delta varints per column. The three sensor columns pack to
   11,847 / 9,584 / 13,136 B ≈ 7.9/6.4/8.8 bits per value — essentially
   the Gaussian step entropy. Text re-rendering is exact because the scale
   is fixed per column and every row is re-rendered and byte-compared at
   compress time.

## Correctness technique that made this safe

Per-line verification with an exceptions stream: at compress time every
parsed line is re-rendered and byte-compared; any mismatch (the truncated
final line both files have, header row, weird formatting) is stored
verbatim, indexed by line number, with a 1-bit-per-line row map (row map
compresses to 18 B). If >10% of lines fail, the whole file falls back to
zstd-19. This means the fast structured path never has to be perfect for
every conceivable input — only for the lines it actually claims.

## What didn't work / dead ends

- **Compressing the request_id stream** (zstd/lzma/brotli all ≥ raw+4 B):
  random bytes are random; the win is only in de-hexing (2x) and isolation.
- **zstd-19/22 as the stream codec**: brotli-11 beat zstd-19 on every
  varint/index stream by 10–25% at these small stream sizes. lzma-6 never
  won a stream on these files.
- **Further squeezing ts/latency/enum streams** is near-pointless: each is
  already within ~10% of its source entropy (measured above). Remaining
  slack on logs.jsonl is ≤ ~2 KB outside the ids.

## Suggestions for gen2

- logs.jsonl is now ids-bound: 45,508 / 62,847 B is the incompressible
  request_id floor. Theoretical remaining gain is small (~2–3 KB); better
  ROI elsewhere.
- series.csv could gain a little from modeling (e.g. second-order deltas
  or a bit-plane split of the varints), but columns are already at ~100–110%
  of Gaussian entropy; expect ≤ 5%.
- The per-stream container + adaptive codec helpers in
  `gen1_structured.py` (`_pack_stream`, `_container`, varint/zigzag) are
  reusable for other line-structured content (code.py imports/indentation,
  text sentence structure).
- A general JSONL transform (arbitrary keys → per-key streams, type-sniffed
  fixed-point numbers) would generalize strategy 1 beyond this exact log
  schema; the exceptions-stream pattern makes that safe to attempt.
