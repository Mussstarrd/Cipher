# Cipher

A hip hop and R&B banger prompt engine for Suno v5.5. Open `dist/cipher.html` in a browser; it has no dependencies.

Pick a **lane** (Dark Trap, Rage / Glitch, Drill, Melodic Trap, Hypnotic Minimal, Grimy Boom Bap, Dark Alt R&B, Trap Soul, Y2K Stutter R&B, Pluggnb, Slow-Jam R&B) and an **edge** (Syncopated, Twitch or Experimental). You get:

- **Style field**: 8–12 front-loaded descriptors with the key, BPM, syncopation and an infectious repeating motif.
- **Exclude styles**: the locked house bans (`jazz, funk, edm, pop, drum fills, tom fills, rimshot, cowbell, dj scratch`) plus any extras you add.
- **Lyrics field**: section tags with flow and groove directions, or boxes to write your own bars. A repeated hook reprints the same words.
- **Harmony sheet**: key, progression, cadence and chords for each section.
- **Hook blueprint**: repetition map, rhythm cell, syllable budget and a melody contour in real pitches.
- **Slider ranges** for each edge.

The same settings and roll number always give the same package. The research behind the engine is in [`docs/research.md`](docs/research.md).

## Develop

```sh
npm test        # every lane × edge × structure × seed: bans, limits, spelling, hook count
npm run build   # inlines src/engine.js + src/app.js into dist/cipher.html
```
