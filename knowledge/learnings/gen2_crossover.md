# gen2 — CROSSOVER agent learnings

Strategies: `gen2.x-struct`, `gen2.x-auto` (module
`lab/strategies/gen2_crossover.py`, results tag `gen2-crossover`).
Both lossless on all 7 corpus files (harness verified; compress() roundtrip
self-verifies every candidate and falls back to zstd-19 on any mismatch).
The module *imports* gen1 helpers (structured transforms, text arithmetic
coder, media transforms) — no gen1 code duplicated or edited.

## Headline results

| file       | gen1 best                  | gen2 crossover        | change |
|------------|----------------------------|-----------------------|--------|
| logs.jsonl | gen1.struct-logs 8.342x (62,847 B) | **gen2.x-struct 8.377x (62,588 B)** | −259 B (−0.41%) |
| series.csv | gen1.struct-csv 11.336x (34,688 B) | **gen2.x-struct 11.549x (34,048 B)** | −640 B (−1.85%) |
| text.txt   | gen1.text-token 9.047x     | 9.046x (wrapped, +1 B) | none — bigram attack failed, see below |
| overall    | gen1.media-auto 2.740x     | **gen2.x-auto 3.016x** | ensemble of per-file winners, +10% total |

`gen2.x-auto` = min over {struct repack, gen1.text-token container,
gen1.media transforms, zstd-19} + 1 mode byte: it matches the per-file best
of the whole lab on every file (ties are 1 byte behind standalone gen1
strategies because of the mode byte).

## What transferred from gen1 (crossover wins, with numbers)

1. **Parametric static AC over the structured agent's varint streams**
   (codec 6). This is the media agent's "model the residual distribution"
   insight applied to gen1_structured's field streams, executed with
   gen1_text's arithmetic-coder primitives. Fit Gaussian / exponential /
   lognormal, quantize the 1–2 parameters into the header (~12 B), code
   against the model pmf. Where it beat gen1's per-stream brotli-11:
   - csv sensor deltas: col_a 11,847→11,552 B, col_c 13,136→13,061 B
     (col_b 9,584 stayed brotli — model est 9,505 didn't materialize after
     escape/header costs)
   - logs ts inter-arrival deltas (exponential fit): 2,245→2,089 B
   - logs latency centi-ms (lognormal fit): 4,340→4,319 B
   A 2-parameter model beats every count-based approach on smooth
   unimodal distributions because it pays no adaptation and no histogram.
2. **Escape symbol is mandatory for delta streams**: gen1's delta streams
   carry the *absolute first value* as their first varint (e.g.
   1,726,200,000). Without outlier escapes the model range explodes
   (col_b span 101,374 → codec inapplicable; ts span 1.7e12) and Gaussian
   fits are wrecked. One reserved escape symbol + verbatim side stream
   fixed it; trim-candidate ranges {0,1,2,4,n/500} let the coder pay ~6 B
   per outlier instead of widening the alphabet.
3. **Reduced-alphabet adaptive order-0 AC** (codec 4) wins the small-alphabet
   index streams where brotli's block overhead dominates: level/service/
   status enum rows 661/960/671→646/937/657 B, row maps 18→10 B,
   msg_counts 15→7 B. Small (~70 B total) but free.
4. **The `_pick_smallest` ensemble pattern** (media agent's suggestion #2)
   scales to whole strategies: wrapping other agents' self-describing
   containers behind one mode byte produced the new overall leaderboard
   winner with zero new compression code.

## What did NOT transfer (negative results — do not repeat)

- **"Adaptive entropy coding beats static codecs on i.i.d. token streams"
  does not generalize to gen1's structured streams.** Measured with the
  exact gen1 adaptation schedule (INC=32, CAP=2^16), coder cost vs gen1
  packed size: byte-order-1 AC lost *everywhere* (ts 2,754 vs 2,245;
  lats 5,727 vs 4,340; msg_indices 9,435 vs 8,062; csv col_a 14,034 vs
  11,847). Byte-order-0 lost on all varint streams too (lats 5,107,
  col_a 12,715). Brotli-11's context modeling is simply better than
  order-0/1 on multi-byte varints; only *value-level* models beat it.
  The order-1 empirical entropies that suggested headroom (e.g. col_a
  "H1=9,310 B") were count-dilution artifacts — 256 contexts over 14 KB
  overfit; the same illusion showed the random request_ids at "H1=39,097 B"
  (truth: 45,504 B, incompressible).
- **Word-bigram context on text.txt loses.** The i.i.d. floor claim from
  gen1 stands. Measured adaptive-coder costs on the 150,057-token id
  stream (alpha 104): gen1's ctx=prev-token = 57,483 B; ctx=last-word =
  83,917 B (loses the capitalize-after-period signal AND dilutes counts);
  ctx=(prev-token, last-word) = 61,305 B. Empirical conditional entropy
  confirms why: H(word)=6.076 bits vs H(word|prev word)=5.985 — only
  0.09 bit/word (~856 B) exists even in principle, and adaptation over
  104x104 contexts costs ~4x that. text.txt is closed at ~9.05x.
- **AR/LPC prediction on series.csv is dead** (the media crossover that
  did NOT work): delta autocorrelation is ≈0 at lags 1–5 (|r|≤0.02),
  AR(2) residual std = delta std to 3 digits, cross-column delta
  correlations ≤0.009. The columns are pure random walks; gen1's
  first-difference is already the optimal predictor. Only the entropy
  stage had headroom (item 1 above).
- **Positional / order-1 context inside msg word indices loses**: per-slot
  entropy is flat (~5.6 bits at each of the 4 positions), order-0
  adaptive body = 8,030 B vs brotli 8,062 — but dictionary+header
  overhead (~55 B) eats the 32 B gain. Left as brotli.

## Where each file class now stands (floors, measured)

| file       | now at   | measured floor           | slack |
|------------|----------|--------------------------|-------|
| logs.jsonl | 62,588 B | ~62,44x B (rids 45,504 raw = 72.7% of blob; lats ≥4,237 digit floor; msg ≥8,030; ts ≈2,073) | ≤ ~150 B (0.25%) |
| series.csv | 34,048 B | ~33,8xx B (Gaussian delta entropy per column) | ≤ ~250 B (0.7%) |
| text.txt   | 57,954 B | 56,985 B i.i.d. word entropy; conditioning provably adds ≤856 B | ~1.5%, unreachable adaptively |
| code.py    | 964 B    | ~700 B (gen1 text est.) | ~250 B, negligible absolute |
| audio.pcm  | 342,033 B| ~330 KB (noise floor, gen1 media est.) | ~3–4% |
| image.rgb  | 283,171 B| 281,5xx B (filtered-byte entropy) | ~0.6% |
| random.bin | 262,145 B| 262,144 B | 0 |

## Recommendation to the orchestrator: STOP (with one optional exception)

Gen2's whole-lab gains: logs +0.41%, csv +1.85%, everything else 0. Both
wins came from entropy-stage polish against floors we can now measure
directly; every remaining per-file slack except audio is under 1%, which
is the protocol's stopping criterion. The only class with >1% measured
headroom is audio.pcm (~3–4%, needs genuinely better *prediction* — e.g.
long-window spectral modeling — not better entropy coding; gen1 media
already showed Rice/planes are at the residual entropy). If a gen3 is
spawned at all, scope it to that single question; otherwise declare
convergence: `gen2.x-auto` at 3.016x total is within ~1% of the sum of
per-file floors for this corpus.
