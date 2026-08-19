# REV1 Trainer — Control Surface, Skill Curriculum & Drill Design

Working doc: how the DDJ-REV1 is physically operated at each skill level, and
how song analysis maps onto drill/chart generation. This is the knowledge base
the chart generator draws from — the goal is that generated drills read like
*DJ vocabulary*, not random button prompts.

## 1. Control surface inventory (DDJ-REV1)

2-channel, battle-style (mirrors PLX turntable + DJM-S mixer layout).

| Zone | Controls | Notes for drills |
|---|---|---|
| Decks (L/R) | 6" capacitive jog wheels | Scratch input; touch top = stop platter, rim = pitch bend |
| Above decks | 60 mm **horizontal** tempo sliders (±8/16/50%) | Battle placement; beatmatch drills |
| Deck transport | PLAY/PAUSE, CUE per deck | Foundation of every drill |
| Mixer center | 16 RGB performance pads (8 per deck, in mixer section) | Modes: Hot Cue, Auto Loop, Tracking Scratch, Sampler |
| Mixer | 2 channel faders, crossfader (sharp curve for cuts) | Cut/transform drills |
| Per channel | Trim, 3-band EQ (Hi/Mid/Low), CFX filter knob | Blend drills |
| FX | **Lever FX paddles** (pull = momentary burst, push up = latch) | Effect throws on phrase boundaries |
| Browse | Rotary selector, load buttons | Prep flow |

The exact MIDI codes for all of these are captured per-unit by the prototype's
**Surface Learn** mode (touch a control, name it, export JSON) rather than
hardcoded — this makes the app resilient to firmware differences and portable
to the FLX4/DDJ-400 family later.

## 2. How a fluid human operates it, by level

### Beginner (weeks 1–4) — "keep time, don't crash"
- Cue point set/recall; starting a track **on beat** from CUE
- Channel-fader swap between two playing tracks
- Basic crossfader cut (sharp curve): closed → open on the one
- Counting bars/phrases (4 beats × 8 = 32-beat phrase)
- Bass-swap blend: Low EQ down on outgoing, up on incoming at phrase boundary
- Motion profile: one hand at a time, eyes on hands.

### Intermediate (months 2–6) — "two hands, two layers"
- Manual beatmatch with tempo slider + jog nudge (no sync crutch)
- Baby scratch, then chirp (fader closes as platter pushes forward)
- Pad drumming: finger-drum hot cues in rhythm (cue juggling seeds)
- Auto-loop capture (4/8 beat) to extend an outro under an incoming track
- Lever FX bursts (pull) timed to the last beat of a phrase
- Filter sweeps into transitions
- Motion profile: hands split — left on platter, right on fader; peripheral vision.

### Advanced (6 months+) — "the controller is an instrument"
- Transform/stab combos: rhythmic crossfader chops against a held platter
- Chirp-flare progressions; scribble; drags timed to half/quarter notes
- Cue juggling: two copies of a break, rebuilding the drum pattern live by
  alternating hot cues across decks
- Tracking Scratch pads (REV1's assisted-scratch mode) as a stepping stone
- Sampler layering: horns/airhorns/vocal stabs dropped on phrase heads while
  both decks run
- Latch FX + hands-free: push lever up, both hands to pads
- Full routine: 4-layer session — deck A groove, deck B scratch source,
  sampler accents, loop safety-net.

## 3. Song analysis → chart generation rules

The analyzer extracts: **BPM**, **beat grid** (phase-aligned), **kick onsets**
(low-band spectral flux < ~150 Hz), **snare onsets** (mid-band flux ~1.5–5 kHz),
and later: phrase boundaries (32-beat energy shifts), sections (energy tiers).

Mapping vocabulary (the "playbook"):

| Musical event | Beginner chart | Intermediate | Advanced |
|---|---|---|---|
| Downbeat (the one) | pad hit | cue-in from CUE | juggle restart |
| Kick | pad chop | fader stab | transform run |
| Snare (2 & 4) | crossfader cut | chirp scratch | flare combo |
| Phrase boundary (32 beats) | bass swap EQ | FX lever burst | sampler drop + FX latch |
| Section change | fader swap | filter sweep transition | full transition routine |
| Break/outro | auto-loop capture | loop + incoming deck | cue juggle over loop |

Density rules: beginner ≤ 1 event/beat, one limb; intermediate ≤ 2 events/beat,
two limbs never on the same hand simultaneously; advanced allows 16th-note
subdivisions and cross-hand patterns. A chart is *ergonomically valid* only if
consecutive events respect hand-travel time (pads↔fader ≈ 120 ms min, jog↔pads
≈ 250 ms min) — this is what makes drills feel fluid instead of random.

## 4. Madden-style mini-drill progression (product shape)

Each **drill card** = one skill, 60–90 seconds, on the user's own music:
objective, controls used, demo overlay, 3-star thresholds (accuracy %, max
combo, no-miss). Drills chain into **skill trees** (Cuts → Transforms →
Flares). Completing a tree unlocks its **Routine**: a 16-bar chart that strings
the tree's moves into one performance. End state: **Performance Mode** — the
app analyzes two of your tracks + sampler bank and generates a full 4-layer
routine (both decks, sampler, loops) with live scoring, replayable and
shareable.

## 5. Prototype roadmap

- **P0 (done, field-tested):** Web MIDI in-browser, monitor, calibration,
  metronome cut-drill with ±60/±140 ms scoring.
- **P1 (this build):** Surface Learn (label + export full MIDI map); song
  ingestion; offline BPM/beat-grid/kick/snare analysis with waveform view;
  drills generated from the song's actual structure.
- **P2:** jog/scratch gesture recognition (baby, chirp), EQ/FX lever drills,
  phrase detection, drill cards + stars, first skill tree.
- **P3:** two-deck audio engine (time-stretch, per-channel routing through the
  REV1 sound card), sampler bank, Performance Mode routine generator.
