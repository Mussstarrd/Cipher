# REV1 Trainer — Prototype 1

A single-file Web MIDI proof-of-fun: connects to a Pioneer DDJ-REV1 in the
browser, shows a live MIDI monitor, calibrates the pads and crossfader, and
runs a Guitar-Hero-style "cut drill" with real timing scores (PERFECT ±60ms /
GOOD ±140ms), combo, and a grade.

## Run it

1. Plug in the DDJ-REV1 **before** opening the page. Close Serato/rekordbox —
   only one app can hold the MIDI device at a time.
2. Open `index.html` in **Chrome or Edge** (Web MIDI is not in Safari/Firefox).
   Double-clicking the file works; if MIDI permission misbehaves on `file://`,
   serve it instead: `python3 -m http.server` then visit
   `http://localhost:8000/prototypes/rev1-trainer/`.
3. Click **Connect MIDI** and allow the permission prompt. The pill in the
   header should show the DDJ-REV1.
4. Touch anything on the controller — events appear in the MIDI monitor.
5. Run **Calibrate controller** (3 steps: left pad, right pad, crossfader).
   The mapping is saved in localStorage.
6. Pick a drill and hit **Start drill**.

No controller handy? Keyboard fallback: `Z` = left pad, `X` = cut left,
`N` = cut right, `M` = right pad.

## Prototype 1 additions

- **Song ingestion & analysis** — load any local audio file; on-device DSP
  extracts BPM (autocorrelation of spectral flux, verified to ~0.1% on a
  synthetic test), beat grid, kick onsets (low band) and snare onsets
  (mid band). Waveform strip visualizes all of it. Nothing is uploaded.
- **Song drills** — "Ride the beat", "Cut the snares", "Chop the kicks",
  and a combo mode: notes are generated from the track's actual drum
  structure while the song plays, with an ergonomic minimum spacing so
  charts stay humanly playable.
- **Surface Learn** — touch any control on the REV1, name it, and build a
  full exportable JSON map of the control surface (shown by name in the
  MIDI monitor once learned). Export the JSON and feed it back to
  development — it becomes the app's built-in REV1 map.

Still to come (P2+): jog/scratch gesture recognition, EQ/FX-lever drills,
phrase detection, drill cards with star ratings, two-deck audio engine,
performance mode. See `docs/rev1-curriculum.md` for the skill taxonomy and
chart-generation rules.
