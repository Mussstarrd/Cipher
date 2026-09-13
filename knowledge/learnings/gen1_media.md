# Gen 1 — MEDIA specialist findings (audio.pcm, image.rgb)

Strategies: `gen1.media-lpc`, `gen1.media-pngf`, `gen1.media-auto`
(module `lab/strategies/gen1_media.py`, results tag `gen1-media`).
All use a self-describing container (mode byte 0=zstd-19 fallback,
1=audio LPC, 2=image row-filter); compression tries every applicable mode
and keeps the smallest blob, so nothing regresses on non-media files and
every file stays lossless (harness-verified on all 7 corpus files).

## Headline results vs best gen0 baseline

| file      | best gen0            | gen1.media-auto | improvement |
|-----------|----------------------|-----------------|-------------|
| audio.pcm | brotli-11: 1.1836x (442,950 B) | **1.5329x** (342,032 B) | −22.8% bytes, +29.5% ratio |
| image.rgb | brotli-11: 1.3724x (382,021 B) | **1.8515x** (283,170 B) | −25.9% bytes, +34.9% ratio |

All other corpus files fall through to the mode-0 fallback and match
zstd-19 (e.g. code.py 102.3x, logs 5.506x, random.bin 1.000x).

## Audio (raw s16le mono PCM): what worked

Winner: per-block quantized least-squares LPC, FLAC-style.
- Block = 4096 samples; per block try orders {1, 2, 4, 8, 16}, keep the one
  with minimum sum|residual|. Coeffs are float LS solutions quantized to
  int16 at qshift=12; prediction is pure-integer `(A·q)>>12` so decode is
  exactly reproducible.
- Residuals zig-zag mapped to uint16 and split into byte planes (all low
  bytes, then all high bytes); entropy stage picks best of
  zstd-19/lzma-6/brotli-11 (brotli-11 wins).

Measured ladder on audio.pcm (transform + best entropy stage):
- fixed per-block predictor orders 0–3 (FLAC "fixed"), zigzag planes: 1.431x
- global 2nd-order delta, zigzag planes: 1.384x
- LS-LPC order 4: 1.486x; order 8: 1.524x; **order 16: 1.530x**; order 32: 1.527x (worse — coeff overhead + noise amplification)
- qshift 12 vs 13 vs 14: identical to 4 decimal places — coefficient precision is NOT the bottleneck.

## Audio: what didn't work, and why

- **Second/third differences lose to first difference.** Per-block
  order-pick among fixed predictors chose order 1 in 63/64 blocks. The
  corpus signal is tones + white noise (sigma ≈ 262 LSB); k-th differencing
  multiplies white-noise variance by C(2k,k) (x2 for d1, x6 for d2), which
  swamps the tiny gain on the low-frequency tonal part. Any next-gen idea
  based on "more differencing" is a dead end for noisy signals.
- **Byte-interleaved residuals lose to byte planes**: 1.348x vs 1.409x
  (lzma). Keep low/high bytes separated.
- **Rice coding is not worth building here**: per-block optimal-k Rice on
  the fixed-predictor residuals gave 369,161 B — brotli-11 on zigzag planes
  already hit 366,366 B, i.e. within 0.2% of the residuals' empirical
  per-sample entropy (366,942 B). Generic codecs on planes ARE the entropy
  coder; only better *prediction* moves the needle.
- **Ceiling is close.** Residual mean|.| is ~252 vs the generator's noise
  floor sigma ≈ 262 (0.008 * 32767); white noise is incompressible beyond
  its ~10.1 bits/sample entropy, putting the absolute ceiling near ~1.59x.
  We are at 1.533x; at most ~3–4% remains for smarter audio modeling.

## Image (raw RGB, width 512, 3 B/px): what worked

Winner: PNG-style per-row filtering.
- Row stride 1536 B; per row pick none/sub/up/average/Paeth by minimum sum
  of absolute signed (mod-256) residuals; partial trailing row stored raw.
  The chooser picked average for 340/341 rows (sub for the first).
- Stream = filter ids + filtered rows; entropy stage best-of-3 (brotli-11
  wins: 283,160 B; zstd-19: 287,931 B; lzma-6: 290,708 B).

## Image: what didn't work, and why

- **Per-channel separation adds nothing**: chan-separated planes after
  filtering gave 286,984 B (lzma) vs 286,996 — noise dominates and RGB
  channels are independent in this corpus; skip the complexity.
- **Per-channel independent filtering** (filter each plane with its own
  left/up): 1.8268x, no better than filtering interleaved rows with
  bpp-offset neighbors.
- **We are at the entropy wall**: filtered-byte empirical entropy is 4.295
  bits/B => bound 1.8626x; we achieve 1.8515x (99.4% of the bound). The
  +-6-uniform pixel noise (avg-filter residual entropy ~4.3 bits) is the
  floor. Better filters can't help; only a smarter entropy coder modeling
  the residual distribution could claw back the last ~0.6%.

## Suggestions for gen 2

1. Don't chase audio prediction past order ~16 or fancier entropy coders;
   the remaining headroom on these two files is ~3–4% (audio) and ~0.6%
   (image). Marginal-cost territory.
2. The `_pick_smallest` container pattern (try transforms + fallback, keep
   min) is cheap insurance and made `media-auto` win on both files with
   zero regressions elsewhere — reuse it for combined/ensemble strategies.
3. Surprise: the image row filter also compresses *audio* to 1.188x
   (beats brotli-11's 1.1836 baseline) because the 1536-B "up" filter is a
   768-sample delta. Cross-domain transforms are worth auto-trying on all
   numeric content.
4. For structured-text agents: series.csv is numeric-in-ASCII; a
   parse-to-binary-columns + delta + plane-split pipeline (same toolbox as
   audio) should beat the 6.04x lzma baseline substantially.
5. If the corpus ever gains stereo audio or 16-bit images: add channel
   decorrelation (mid/side) and 2-D prediction (up+left LPC) — the current
   module's container has spare mode bytes for it.
