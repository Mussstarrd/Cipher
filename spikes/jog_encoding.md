# Spike A — Jog wheel encoding (RESULTS — owner-run on hardware, 2026-08-19)

## Environment
Chrome 151, Windows x64, DDJ-REV1, left jog. Full raw captures in owner's
harness session; analysis ran over all messages (raw appendix truncated at
400 lines/capture by design).

## Answers to the brief's five questions

1. **Encoding: center-64 offset, confirmed.** Rotation = ch1 (0-indexed ch0),
   CC 34 (0x22). Forward → 65 (0x41), reverse → 63 (0x3F); occasional 66/62
   at peak speed. slowFwd: 748/751 msgs exactly 65. NOT two's complement.
2. **Single 7-bit CC.** No cc+32 companion → no MSB/LSB pair.
3. **Touch: note 54 (0x36) on ch1.** 0x90 vel 0x7F on touch-on, vel 0x00 on
   release. Clean gate; present at the head/tail of every capture.
4. **Ticks/revolution: 722 measured over 5 revs → effectively 720 (2 ticks
   per degree).** Quarter turn ≈ 180 ticks.
5. **Update rate: 340 msgs/s average, 660 msgs/s peak (best 100 ms window)
   under an aggressive stroke.**

## Design consequences (spike wins over prior assumptions)

- **Velocity is encoded in message RATE, not value magnitude.** Values stay
  within ±1–2 of center even at full speed; the wheel emits ~1 tick per
  message and speed = message frequency. Velocity-envelope scoring IS viable
  (~10 ms resolution at peak rate) but must be implemented as a
  tick-rate integrator over time bins, not a value decoder.
- Stroke model per brief: touch-on (n54 ON) resets virtual origin; integrate
  signed ticks for position; bin ticks per 10–20 ms for velocity; direction
  change = sign flip of binned sum with hysteresis.
- **Debounce required:** touch-only capture leaked 6 stray rotation ticks
  (finger wobble). Require ≥~3 net ticks per bin before counting motion.
- Timestamps in the raw log are quantized to 0.1 ms (all end .00/.10) —
  600× finer than the ±60 ms window. Spike D formalizes this.
- Right jog expected at ch2 CC34 / note 54 ch2 (b1 dominance in Spike B's
  wiggle capture is consistent); verify during map hardening.

## Verdict
Full trajectory-lane design (direction + extent + velocity envelope) is GO.
No degradation to extent-only scoring needed.
