# ScratchTrainer — Phase 0 Verification Spikes

Blocking spikes per the build brief. **No product code until these report.**
The AI side cannot touch the hardware, so the split is: `harness.html`
instruments and auto-analyzes; the human runs the protocols on the laptop
with the DDJ-REV1 and pastes back the generated reports.

## Run order (per brief): B → A → C → D

Open `harness.html` in Chrome on the laptop. Each tab walks its protocol and
generates the markdown report to paste into the matching `spikes/*.md` file.

- **Spike B — Serato coexistence (run FIRST).** Open Serato DJ Lite, confirm
  it's controlling the deck, then open the harness and hit Connect. The tab
  reports whether Chrome receives events while Serato holds the device.
  If Chrome gets nothing, run the optional native probe
  (`node native-midi-probe.mjs` after `npm i easymidi`, or
  `python3 native_midi_probe.py` after `pip install mido python-rtmidi`)
  with Serato still open — that isolates OS-block vs Chrome-block.
  These probes are Phase 0 diagnostics run manually; nothing native ships.
- **Spike A — jog encoding.** Guided captures: slow forward, slow reverse,
  1-second aggressive stroke, touch-only, and exactly 5 slow revolutions.
  The tab computes value distributions, MSB/LSB pairing evidence, touch
  CC/note, ticks/revolution under both encoding hypotheses, and update rate.
- **Spike C — Web Audio scrub.** AudioWorklet scrub of a synthesized chirp
  (no copyrighted audio in repo — an original commissioned scratch sample
  pack is a known Phase 1 dependency). Jog deltas drive playback position;
  the tab reports baseLatency/outputLatency/worklet round-trip, and asks for
  the human verdict: does pitch track the hand mid-stroke, honestly?
- **Spike D — timestamp precision.** Measures `performance.now()` coarsening
  in this Chrome, and inter-event jitter during a steady jog spin.

Findings go in: `serato_coexistence.md`, `jog_encoding.md`,
`webaudio_scrub.md`, `timestamp_precision.md`. Where a result contradicts
the build brief, the result wins and the report must say so explicitly.

**Then stop. Phase 1 branch (scoring overlay vs self-contained vs re-plan)
is decided from these reports, not before.**
