# Spike C — Web Audio scrub viability (RESULTS — owner-run, 2026-08-19)

## Environment
Chrome 151, Windows x64, DDJ-REV1 left jog, worklet engine over localhost.

## Measured
engine: worklet
AudioContext sampleRate: 48000 Hz
baseLatency: 10.0 ms · outputLatency: 40.0 ms · block: 2.67 ms
main-thread → worklet round-trip: mean 0.07 ms over 200 msgs
estimated motion→sound ≈ 42.7 ms (excl. USB/MIDI input path)

## Human verdict (jog)
NOT USABLE. Pitch did not audibly track the hand; output was a "scratchy
gurgle." Perceived lag grew as sensitivity (chase distance) increased;
lower sensitivity sounded tighter but still not musical.

## Interpretation
- The 40 ms Windows/Chrome output latency is structural: even a perfect
  scrub algorithm sits ~4x above the <10 ms hand-to-ear target scratching
  needs. The naive position-follower in the rig adds zipper artifacts on
  top, but the latency floor alone disqualifies the path.
- Consistent with the brief's warning: browser scrub cannot honestly teach
  the auditory-motor loop.

## Conclusion
- [ ] usable for learning
- [ ] usable only for slow strokes
- [x] NOT usable — Branch 2 is dead as a scratch-learning path.
      Irrelevant to product viability: Spike B passed, so Branch 1
      (Serato = audio engine, we = scoring overlay) is the architecture.
      No further scrub engineering warranted.
