# Cipher

A one-button hip hop, pop rap and R&B banger randomizer for Suno v6 (v6, v6-wild and v6-mini). Open `dist/cipher.html` in a browser; it has no dependencies.

Press **Roll**. Each roll picks a lane (Dark Trap, Rage / Glitch, Drill, Melodic Trap, Hypnotic Minimal, Grimy Boom Bap, Pop Rap, Phonk, Acid Soul Rap, Dark Alt R&B, Trap Soul, Y2K Stutter R&B, Pluggnb, Slow-Jam R&B), an edge (Syncopated, Twitch or Experimental), often a likeness and a blend, then a key, progression, structure and hook. You can narrow to hip hop or R&B, flip Instrumental, and pick the Suno model. You get:

- **Likeness** (rolled about half the time): the sound of a popular artist (Kendrick, JID, Drake, T.I., Travis Scott, Future, 21 Savage, Kevin Gates, Lil Baby, Playboi Carti, Young Thug, Lil Uzi Vert, Kanye, Pop Smoke, Connor Price, Chance; The Weeknd, Bryson Tiller, SZA, Brent Faiyaz, Summer Walker, PARTYNEXTDOOR) as delivery, flow signature and production tells. A lint blocks any artist name from reaching the prompt.
- **Style field**: 8–14 front-loaded descriptors with the key, BPM, a lead instrument drawn from a 40-instrument pool, an infectious repeating motif, and one phonk, one syncopation and one experimental adjective on every roll.
- **Exclude styles**: the locked house bans (`jazz, funk, edm, bubblegum pop, drum fills, tom fills, rimshot, cowbell, dj scratch, background vocals, vocal chops, vocal samples, chanting, gang vocals`). Only one lead voice is ever asked for; no lane, likeness or instrument pool names a vocal sample, chop, chant or choir, and a lint blocks that vocabulary.
- **Lyrics field**: section tags with flow and groove directions, or a structured instrumental arrangement.
- **Harmony sheet**: key, progression, cadence and chords for each section.
- **Hook blueprint**: repetition map, rhythm cell, syllable budget and a melody contour in real pitches.
- **Settings** for each edge and model: Weirdness and Style Influence ranges, Variety Off, and when to use Max Mode.

The same settings and roll number always give the same package. The research behind the engine is in [`docs/research.md`](docs/research.md).

## Develop

```sh
npm test        # every lane × edge × structure × seed: bans, limits, spelling, hook count
npm run build   # inlines src/likeness.js + src/engine.js + src/app.js into dist/cipher.html
```
