# REV1 Trainer — Prototype 0

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

## What this proves / doesn't

Proves: the REV1 is readable from a browser with millisecond timestamps,
crossfader cuts and pad hits can be detected and scored, and the rhythm-game
loop is buildable with zero native code.

Doesn't include (yet): audio playback of real tracks, jog-wheel/scratch
scoring, song ingestion & beatgrid analysis. Those are the next spikes.
