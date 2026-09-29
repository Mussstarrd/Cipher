# Cipher

Two Suno v6 prompt engines, each a single self-contained page in `dist/`.

## ANOMALY (`dist/anomaly.html`)

An instrumental prompt generator aimed at today's hip hop and R&B radio, built from an executive-producer sonic brief ([`docs/producer-brief.md`](docs/producer-brief.md)). One button. Each roll picks a **producer blueprint** (`src/anomaly/blueprints.js`): a record identity with its own opener sentence (the first thing Suno reads), drums, 808, leads, clash elements and trademark moves. The reference producer is shown as "in the spirit of" and never enters the prompt. Each roll gives you:

- **Style field**: genre, an instrumental lock, a syncopation line, a non-quantized feel line, the world's drums and 808, an adjective-tagged lead (from 12 sound worlds and a shared pool; piano in under 12% of rolls) playing a named progression in a real key, a counter-voice, a twitchy glitch written as rhythm, a **signature anomaly**, a **technical flex** (polymeter, odd bars, tuplets, micro-timing), a beat switch, texture and mood, a mix/master line, BPM and a hard stop.
- **Lyrics field**: a proper instrumental arrangement in section tags (intro, verses, hooks, pre-hook, break, beat switch, bridge, outro) with bar counts, where the signature hits, and where the switch lands. Six structures from 1:30 to 3:10.
- **Exclude styles**: 16 terms, vocals first.
- **Suno settings**: model, Instrumental on, Weirdness, Style Influence, Variety, Max Mode, for Radio and Experimental modes.
- Harmony sheet with the cadence explained.

Blueprints: Broward emo lo-fi, Texas trunk menace, King of the South orchestral, Cash Money bounce, Toronto nocturnal, East Atlanta elastic, Houston psychedelic, Chicago icy drill, Compton theatrical, Memphis crunk, Baton Rouge pain, Detroit / Flint, NY sample drill, West Coast bounce, Cinematic trap, Dark synth R&B, Trap-soul, Afro R&B, Y2K R&B.

## CIPHER (`dist/cipher.html`)

A one-button hip hop, pop rap and R&B banger randomizer for Suno v6 (v6, v6-wild and v6-mini) with vocals. Open `dist/cipher.html` in a browser; it has no dependencies.

Press **Roll**. Each roll picks a lane (Dark Trap, Rage, Drill, Melodic Trap, Hypnotic Minimal, Grimy Boom Bap, Pop Rap, Phonk, Acid Soul Rap, Dark Alt R&B, Trap Soul, Y2K R&B, Pluggnb, Slow-Jam R&B), often a likeness and a blend, then a key (never one of the last four), a progression, a structure (a 90-second short loop about a third of the time) and a hook. You can narrow to hip hop or R&B, flip Instrumental, and pick the Suno model. You get:

- **Likeness** (rolled about half the time): the sound of a popular artist (Kendrick, JID, Drake, T.I., Travis Scott, Future, 21 Savage, Kevin Gates, Lil Baby, Playboi Carti, Young Thug, Lil Uzi Vert, Kanye, Pop Smoke, Connor Price, Chance; The Weeknd, Bryson Tiller, SZA, Brent Faiyaz, Summer Walker, PARTYNEXTDOOR) as delivery, flow signature and production tells. A lint blocks any artist name from reaching the prompt.
- **Style field**: 12–13 descriptors. Every roll carries one line each for non-quantized feel, syncopation, experimental sound design, minimalism and club-anthem sex appeal, plus the 808, a lead from a 30-instrument modern pool, the key and progression, and a loud radio-quality master with a hard stop.
- **Exclude styles**: the locked house bans (`dj effects, scratching, transition sweeps, risers, drum fills, tom fills, cowbell, rimshot, background vocals, vocal chops, chanting, jazz, funk, edm, bubblegum pop`). No lane, likeness or pool uses glitch, stutter, chop, mute, transition or mallet-instrument words; a lint blocks them.
- **Lyrics field**: section tags with flow and groove directions, or a structured instrumental arrangement.
- **Harmony sheet**: key, progression, cadence and chords for each section.
- **Hook blueprint**: repetition map, rhythm cell, syllable budget and a melody contour in real pitches.
- **Settings** for each edge and model: Weirdness and Style Influence ranges, Variety Off, and when to use Max Mode.

The same settings and roll number always give the same package. The research behind the engine is in [`docs/research.md`](docs/research.md).

## Develop

```sh
npm test        # every lane × edge × structure × seed: bans, limits, spelling, hook count
npm run build   # builds dist/cipher.html and dist/anomaly.html from src/
```
