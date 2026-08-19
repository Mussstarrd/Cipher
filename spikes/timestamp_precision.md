# Spike D — Timestamp precision (RESULTS — owner-run, 2026-08-19)

## Environment
Chrome 151, Windows x64, DDJ-REV1 left jog, steady spin.

## Measured
performance.now() granularity ≈ 0.1 ms (1995 distinct steps in 200 ms)
inter-message intervals over 2193 CC msgs (5 s spin ≈ 439 msgs/s):
mean 2.28 ms · sd 1.04 ms · p5 0.90 · p50 2.00 · p95 4.00 ms
timestamps quantized to 0.1 ms (2 distinct sub-ms fractional values)

## Conclusion vs ±60 ms windows
PASS with enormous margin. Worst-case observed spread (p95 4 ms) is 15x
inside the PERFECT window; sd 1.04 ms is an upper bound that still includes
real hand-speed variation. The ±60/±140 ms windows are limited by human
skill, not by the input stack. Tighter windows (±20-30 ms) for advanced
flare drills are technically supportable if pedagogy ever wants them.
Side confirmation: 439 msgs/s sustained matches Spike A's rate finding.
