# Gen 3 — AUDIO specialist findings (the last open question)

Strategies: `gen3.audio-sin`, `gen3.audio-auto`
(module `lab/strategies/gen3_audio.py`, results tag `gen3-audio`).
Scope per gen2's convergence analysis: audio.pcm only — ~3-4% measured
headroom, reachable only through better *prediction*. Both strategies are
harness-verified lossless on all 7 corpus files; mode-3 compression also
self-verifies its roundtrip inside compress() and withdraws on mismatch.

## Headline result

| file      | gen1.media-lpc (prev best) | gen3.audio-sin | improvement |
|-----------|----------------------------|----------------|-------------|
| audio.pcm | 1.5329x (342,032 B)        | **1.5819x (331,426 B)** | −10,606 B (−3.10% bytes, +3.2% ratio) |

`gen3.audio-auto` (sinusoid mode + gen1 audio/image modes + zstd fallback
behind one container) posts the same 1.5819x on audio and matches
gen1.media-auto everywhere else (image.rgb 1.851x, code.py 102.3x, ...).

**The prediction ceiling is REACHED, not just approached.** Residual std
after the new predictor is 262.47 vs the generator's noise sigma = 262.14
(0.008*32767); mean|r| = 209.3 = sigma*sqrt(2/pi) exactly; residual
autocorrelation |r| < 0.004 at lags 1-10 (white). Blob composition:
330,442 B arithmetic-coded residual (empirical residual entropy = 10.077
bits/sample = 330,203 B floor, so the entropy stage is within 0.07%),
960 B tone parameters, 24 B headers. Nothing measurable remains.

## What worked: model the generator, not the samples

The corpus audio *is* two sinusoids (amps 0.45/0.25) whose frequencies
re-drift every 4410 samples, over white Gaussian noise. LPC approximates
sinusoid removal with a short all-pole filter and pays a noise-amplification
tax (gen1 residual std 316 = 1.21x the noise floor). Direct per-block
sinusoid estimation removes the tones exactly:

1. Blocks aligned to the drift grid: [k*4410, (k+1)*4410) — inside each
   block the two frequencies are constant by construction.
2. Frequency estimation per block: peaks of a Hann-windowed 8x-zero-padded
   FFT (second peak after nulling +-24 padded bins around the first), then
   coordinate-descent refinement of (w1, w2) minimizing the *joint* 4-column
   least-squares residual power. Two alternation rounds, 16 halvings from
   span 2pi/N. At this SNR the CRLB gives freq std ~2e-7 rad/sample =>
   end-of-block phase error ~1e-3 rad => amplitude-domain error ~13 LSB,
   negligible vs sigma=262. Measured per-block residual std: min 257.6,
   mean 262.4, max 268.5 — i.e. at the floor in every block.
3. Quantization for integer-exact reconstruction: freq as u32 fraction of
   2pi (error ~1.5e-9 rad, nil), the 4 cos/sin amplitudes as i16 (error
   <=0.5 LSB, nil). 16 B/block x 60 blocks = 960 B — 4.4x cheaper than
   gen1's order-16 LPC coefficients while predicting strictly better.
4. Prediction = round(sum a_k cos + b_k sin) computed by one shared
   routine on both sides; residual defined against that reconstruction.

## Entropy stage had to be revisited (one-line caveat to gen1's finding)

Gen1's "generic codecs on byte planes are within 0.2% of residual entropy"
was true *of gen1's residual*. The whiter gen3 residual defeats brotli's
context modeling: planes left 333,242 B vs a 330,203 B empirical-entropy
floor (0.92% gap). Fix: gen2's parametric-AC insight — a *static
discretized-Gaussian arithmetic coder* (gen1_text's `_AEnc/_ADec`, table =
integer-normalized N(0, sigma) pmf over [-R, R], sigma and R = max|res| in
the 9-byte header, no escapes needed) codes the residual at 330,442 B,
0.07% above the floor, in ~1.3 s. Lesson: "entropy stage is done" is a
statement about a *specific* residual distribution; re-check it whenever
the predictor changes the residual's character.

## What did NOT work (measured negatives — do not repeat)

- **Aligning gen1-style LPC blocks to the 4410 drift grid**: 1.5303x vs
  1.5297x at block 4096 — worth +0.04%, i.e. boundary mis-fit was never
  the problem; the order-16 noise-amplification tax was. Order 32 on
  aligned blocks: 1.5279x (coeff overhead again, confirming gen1).
- **Sign-sign/NLMS adaptive prediction** (the Shorten/ALAC-style lever):
  order 16 mu=0.2 -> 1.5254x, order 32 -> 1.5270x. Residual std 338-340,
  *worse* than block LPC. Gradient tracking pays a permanent misadjustment
  tax proportional to mu, and cutting mu makes re-convergence at each
  4410-sample drift point too slow. Block-fit beats online adaptation when
  the nonstationarity is piecewise-constant.
- **Long-term (pitch) prediction cascade** on the LPC residual (per-block
  best lag 20-600, quantized gain): 1.5286x — the LPC residual's leftover
  tonal energy is too spread in phase for a 1-tap lag predictor.
- **Naive per-tone FFT-peak fitting without joint refinement** gave
  residual std 350 (worse than LPC!) — with two tones present, refining
  each frequency against a residual that still contains the other tone's
  mismatch mis-converges. Joint LS inside the frequency search is what
  gets to the floor.

## Convergence verdict

audio.pcm now stands at 331,426 B against a measured floor of ~331.2 KB
(residual entropy 330,203 B + 960 B irreducible model parameters + headers):
remaining slack **< 0.1%**. With gen2's table, every corpus file is now
within ~1% (text's 1.5% is proven unreachable adaptively) of its measured
floor: the lab has converged on every file class. Recommendation: **declare
convergence and stop**; the corpus generators are exhausted (further gains
would require either new corpus content or memorizing the seed, which is
not compression). If the corpus ever gains real music/speech: keep the
per-block joint sinusoid estimator (it is FLAC-beating on tonal content)
but expect to need >2 tones, tracked across blocks, plus a noise-shaping
LPC on what remains.
