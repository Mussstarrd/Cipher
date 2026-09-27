# Cipher

A one-button hip hop, pop rap and R&B banger randomizer for Suno v6 (v6, v6-wild and v6-mini). Open `dist/cipher.html` in a browser; it has no dependencies.

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
npm run build   # inlines src/likeness.js + src/engine.js + src/app.js into dist/cipher.html
```
