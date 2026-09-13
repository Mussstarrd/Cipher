# gen1 — text/code specialist (agent: text)

Strategies: `gen1.text-token` (word-tokenizing container), `gen1.text-lzmax`
(tuned raw LZMA2 baseline). Results tag: `gen1-text`. All runs lossless on all
7 corpus files.

## Headline results (gen1.text-token vs best gen0 baseline)

| file       | best gen0            | gen1.text-token      | change |
|------------|----------------------|----------------------|--------|
| text.txt   | bz2-9: 65615 B, 7.99x | **57954 B, 9.05x**  | −11.7% size |
| code.py    | bz2-9: 2157 B, 182.3x | **963 B, 408.3x**   | −55.4% size |
| series.csv | lzma-9e: 65112 B, 6.04x | **49753 B, 7.90x** (bonus, not my focus) | −23.6% size |
| logs.jsonl | bz2-9: 78824 B, 6.65x | 78825 B, 6.65x (fallback mode = bz2) | tie |
| audio.pcm / image.rgb | brotli-11 | same (fallback mode = brotli) | tie |
| random.bin | 1.000x | 1.000x (raw-store mode, +1 B) | tie |

## What worked

1. **Word-level tokenization is the single biggest lever on text-ish files.**
   Partition bytes into maximal runs of letters `[A-Za-z]+` (word dict),
   digits `[0-9]+` (a NUM symbol + side stream), and everything else (sep
   dict). text.txt becomes 150 057 tokens over a 104-symbol alphabet
   (101 words incl. capitalized variants + 2 separators + NUM);
   code.py becomes 88 156 tokens over 68 symbols.

2. **The best back-end for the token-id stream depends on the content, so the
   container tries all and keeps the smallest:**
   - text.txt: adaptive **order-1 arithmetic coding** wins — 57 483 B payload
     (+457 B dict). The corpus words are ~i.i.d. uniform over 51 words
     (≈5.67 bits/word Shannon floor ≈ 56.7 KB), and order-1 conditioning on
     the previous separator captures capitalize-after-period for free. We are
     within ~2% of the source entropy — text.txt is nearly closed.
     For comparison: lzma over the same ids = 64 678 B, bz2 over ids = 60 331 B.
   - code.py: **bz2 over the id bytes** wins — 585 B for 88 KB of ids. BWT+RLE
     eats the ~730 nearly-identical per-function chunks. Context modeling
     lost here: order-1 = 15 481 B, order-2 = 2 342 B, order-3 = 1 199 B
     (estimates) — long-range chunk repetition beats short Markov context.

3. **Routing the digit side stream into per-context streams (context = id of
   the preceding token) cut code.py from 1219 B to 963 B** and flipped
   series.csv from fallback to a 7.90x win. The routing is a pure function of
   the decoded id stream, so it costs zero metadata. Per stream we pick
   literal-LZMA vs zigzag-delta-varint: the `process_batch_{i}` counter
   stream (deltas ≡ 1) and constant streams (`3`, `1`) collapse to a few
   dozen bytes; in series.csv the timestamp column (delta ≡ 10) vanishes and
   the fractional-digit columns separate cleanly from integer parts.

4. **Self-describing container + min() over candidates + compress-time
   roundtrip self-verification** makes losslessness on binary/random inputs
   trivial: mode byte ∈ {raw, bz2, raw-LZMA2, tokenized, brotli}. The
   strategy is never worse than any gen0 codec by more than 1 byte on any
   file. Recommended pattern for every future strategy.

## What didn't work (don't repeat)

- **LZMA filter tuning is a dead end on this corpus**: best raw LZMA2 9e
  (pb=0, lc=0) = 90 631 B on text.txt (vs 91 020 stock) and 2 507 B on
  code.py — never within reach of bz2, let alone tokenization. Registered as
  `gen1.text-lzmax` for reference; ≤0.5% over gen0 lzma-9e.
- **Higher-order context models on text** hurt: order-2 = 61.3 KB,
  order-3 = 73.2 KB vs 57.5 KB for order-1 (words are i.i.d.; extra context
  only dilutes counts and slows adaptation).
- **Tokenize-then-LZ on text** (lzma on ids, 64.7 KB) barely beats plain bz2
  (65.6 KB): an i.i.d. symbol stream needs an entropy coder, not a matcher.
- **A single shared number stream** left ~250 B on the table on code.py
  (mixed counter + constants defeat delta coding until split by context).
- **Tokenized modes on logs.jsonl lose** (best 85 KB vs bz2 78.8 KB): the
  digit side stream dominates (49.4 KB — hex request_ids split into many
  short digit/letter runs, float latencies). Ids alone cost only ~46.7 KB
  under order-1. The transform is right, the number/hex handling is not.

## Suggestions for gen2

- **logs.jsonl is the biggest open gap.** Field-aware splitting (per-JSON-key
  value streams) + storing 32-hex request_ids as 16 raw bytes (they are
  random, so they cost 128 bits regardless — but as text they fragment into
  many short digit/letter tokens that pollute both dictionaries and defeat
  the coder) + delta timestamps should land well under 70 KB.
  Reuse `_encode_numbers`/context-routing from `gen1_text.py`.
- **text.txt is ~2% from its entropy floor** given the i.i.d. word model;
  remaining ideas are microscopic (explicit sentence-length model, case-flag
  instead of doubled dictionary ≈ a few hundred bytes). Deprioritize.
- **code.py** still has ~300 B of container/dict overhead vs the ~585 B id
  payload; a combined dict+ids+numbers joint compression or template/diff
  coding might reach ~700 B. Low absolute value — deprioritize.
- **series.csv**: a numeric specialist doing columnar transpose + per-column
  delta should beat my incidental 7.90x; the fractional-digit streams are
  the remaining mass (~45 KB of true noise entropy — check against
  lognormal/gauss entropy before chasing it).
- Speed note: the pure-Python arithmetic coder does ~2.5 s compress /
  ~1.5 s decompress on 512 KB (all candidates + self-verify ≈ 4 s total).
  Fine for the lab; do not port to production without a C coder.

## Numbers cited from

`knowledge/results/gen0.jsonl`, `knowledge/results/gen1-text.jsonl` (final
run at the bottom of the file; earlier rows in the tag are intermediate
iterations of the same strategies).
