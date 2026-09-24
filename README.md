# Cipher

A hip hop and R&B banger prompt engine for Suno v6 (v6, v6-wild and v6-mini). Open `dist/cipher.html` in a browser; it has no dependencies.

Pick a **lane** (Dark Trap, Rage / Glitch, Drill, Melodic Trap, Hypnotic Minimal, Grimy Boom Bap, Dark Alt R&B, Trap Soul, Y2K Stutter R&B, Pluggnb, Slow-Jam R&B) and an **edge** (Syncopated, Twitch or Experimental). You get:

- **Likeness** (optional): the sound of a popular artist (Kendrick, JID, Drake, T.I., Travis Scott, Future, 21 Savage, Kevin Gates, Lil Baby, Playboi Carti, Young Thug, Lil Uzi Vert, Kanye, Pop Smoke; The Weeknd, Bryson Tiller, SZA, Brent Faiyaz, Summer Walker, PARTYNEXTDOOR) as delivery, flow signature and production tells. The name is only on the chip; a lint blocks any artist name from reaching the prompt.
- **Style field**: 8–12 front-loaded descriptors with the key, BPM, syncopation and an infectious repeating motif.
- **Exclude styles**: the locked house bans (`jazz, funk, edm, pop, drum fills, tom fills, rimshot, cowbell, dj scratch`), `humming` (pre-selected for v6), and any extras you add.
- **Lyrics field**: section tags with flow and groove directions, or boxes to write your own bars. A repeated hook reprints the same words.
- **Harmony sheet**: key, progression, cadence and chords for each section.
- **Hook blueprint**: repetition map, rhythm cell, syllable budget and a melody contour in real pitches.
- **Settings** for each edge and model: Weirdness and Style Influence ranges, Variety Off, and when to use Max Mode.

The same settings and roll number always give the same package. The research behind the engine is in [`docs/research.md`](docs/research.md).

## Develop

```sh
npm test        # every lane × edge × structure × seed: bans, limits, spelling, hook count
npm run build   # inlines src/likeness.js + src/engine.js + src/app.js into dist/cipher.html
```
