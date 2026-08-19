# ScratchTrainer — Phase 1, Milestone 1

Scoring overlay for scratch practice on the Pioneer DDJ-REV1 (Branch 1
architecture per Phase 0: Serato DJ Lite owns the audio; this app reads the
controller's MIDI in parallel and scores your hands).

## Run
1. Open Serato DJ Lite, load a scratch sample on the left deck.
2. Open `index.html` in Chrome (double-click is fine — no worklet needed).
3. Connect MIDI → allow.
4. Run **Calibrate timing** once (10 clicks, tap a pad or spacebar from
   click 3). Non-optional: training is gated until an offset is saved.
5. Train → Baby Scratch.

Keyboard dev fallback: hold ArrowUp/ArrowDown = fwd/back stroke, Space = pad.

## What's inside (M1)
- **Stroke engine** built from Spike A measurements: center-64 tick
  integration (velocity from message rate), touch-gate origin reset,
  3-tick debounce, stall splitting, per-stroke extent/velocity.
- **Latency calibration**: tap-to-metronome median offset, stored locally,
  applied to all timing judgments.
- **Chirp drill**: same groove as the baby scratch with the fader riding
  along — both hands out on the beat (open + push), both back on the "and"
  (close + pull). One chirp per beat.
- **Baby Scratch drill**: trajectory ribbons (direction + timing + arc
  extent), ±60/±140 ms windows on stroke start, quarter-turn arc credit
  (90–140° full), short/long arc hints, combo, 3-star grading.
- **Purple-dot arc coach** on the jog graphic — visual guide only, never a
  scoring input (scoring uses relative displacement from touch-on).
- Hardened map in `rev1-map.json` (spike-verified entries flagged).

## Backing beat
Drills run over a real groove, not a bare click. Pick **boom bap**, **four on
the floor**, plain **metronome click**, or **silent** — or load your own beat
file, which is analysed on-device for tempo (±0.2% on a synthetic test) and
looped 4 bars from a detected downbeat. The beat keeps running between drills;
starting a drill drops its chart onto the next bar downbeat so the drill rides
the groove instead of restarting it.

## Milestone 2 (next)
Chirp drill (fader gates join the stroke) → Transform → crossfader cut-in &
curve lesson zero → 15 s 9:16 clip export (tab + system audio capture) →
right-deck verification, remaining map hardening.

Known dependency: original commissioned scratch sample pack (canonical
battle sentences are copyrighted; placeholder guidance until cut).
