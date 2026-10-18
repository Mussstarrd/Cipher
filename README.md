# Cipher

Suno v6 prompt engines, each a single self-contained page in `dist/`. **ONE OF ONE is the current one**; the other three are kept for reference.

## ONE OF ONE (`dist/one.html`)

One button. It fills Suno's create screen in Suno's order: **Lyrics → Styles → Exclude styles → Sliders**, each with a Copy button, plus a pinned paste bar that copies the next field in that order.

**The one button** (`src/one/genres.js`, pack `one`, built by `tools/one-packs/gen-one.js`) fuses two things:

1. **Eleven hit archetypes** from a decade of radio (`tools/one-packs/genre-hits.js`, reference records never enter a prompt): Squeak and space (The Box / Savage), Country-trap loop (Old Town Road / A Bar Song), Drawbar triumph (Glory / Industry Baby), Soul slice (Mmhmm / Texas), Flute and 808 menace (Mask Off / Suge), Pain piano (Outside Today / Lonely Child), Cold southern trap (Back in Blood / FNF), West Coast bounce (Not Like Us / Ballin'), Guitar emo (Lucid Dreams / SAD!), Pop-trap earworm (God's Plan / fukumean), Trunk 808 switch (Sicko Mode / Like That). Each lane carries its own anchors, instrument families, tempo, drums, 808 patterns, switch axes, key colour and mix tag. Two more lanes: **Slow and sad** (58–72 BPM, long single notes, the motif grows) and **Weird** (about one roll in eight: v6-wild, Weirdness 70–85, warped and reversed samples, sliced soul keys over slammed drums).
2. **The personality layer** (`src/one/personality.js`): a signature harmonica / erhu / fuzz lap steel / melodica family parked in the low mids that takes the lead about a fifth of the time and answers as the counter otherwise; the beat-3 drunken triplet slur in hip hop words (60% of rolls); twelve syncopation and twelve twitch tags; anti-transition phrases in the drums line and 27 banned transition terms in the Exclude box; twelve lyrics-field layouts that decide the structure (two-bar loop that never changes, one sound in one sound out, hook first, verse bare hook full, slow motif, half-time last third, A then B, call and response, kick and lead only, long fade, lead out for 8 bars, drums cut then beat B). About a third of rolls are minimal.

Every section change is a hard cut: the words for fills, rolls, risers, lead-ins and transitions never appear, and the Exclude box bans them. Model v6 (v6-wild for the weird lane), Variety Off, Max Mode On, Weirdness 40–55, Style Influence 75–85.

The older packs (Syncopated hip hop, R&B radio pop, Soul / emo, Boom bap soul mashup, Bare minimum, Bass is the drums, Radio hit, Spaceship, New wave, Prof) are still generated with `show: false`: reachable by id in the engine and the tests, hidden from the page.

Each roll picks a **lane** that sets the opening tags and the instrument families: South Side brass (trumpet sections, French horns with trombones, sliding tuba, bass trombone, sousaphone; a tuba or sousaphone bass about half the time), Electronic mutant (rubber-band mono synth, woozy seasick synth, car-alarm lead, pager lead, underwater synth...), Crate flip (trap flute, erhu, harmonica, bamboo flute, all framed as a pitched, flipped sample over trap drums and 808s so they never turn into film score), Drunk syrup (drunken detuned organ, wheezing broken organ, sticky out-of-tune piano) and Minimal left-field. Each family has its own processing words, so a tuba gets "blown so hard the brass rasps" and a synth gets "glide time drifting slower and faster by hand". Every roll also has a **counter** instrument from a second family and a hand-played **percussion color** (trap maracas, military snare rudiments, tambourine, woodblock, finger snaps...).

**Genre-drift guard.** Wide palettes invite the wrong genre, so a `genre drift` rule blocks the magnets: sax, clarinet, flugelhorn, Harmon mute, rips and fall-offs, swing (jazz); Rhodes, Wurlitzer, electric piano, smooth, mellow, lush, lounge (elevator music); Hammond, Leslie, wail, twang, spring reverb, pentatonic, swamp (blues); acid, squelch, supersaw, laser, chiptune, ping-pong, ramps, builds (EDM and techno). The Exclude field adds `jazz, smooth jazz, blues, big band, EDM, techno, house, elevator music, lounge, new age, marching band`.

Every sound is built from slots in a sound-design grammar (`src/one/grammar.js`), never picked whole from a short list. A one-phrase label like "dark synth chord loop" points Suno at the average of thousands of tracks, which is the preset. Each lead gets seven slots:

| slot | example |
| --- | --- |
| source | a Wurlitzer with a broken tremolo |
| character | saturated with a slight pitch warble |
| register | low-mid, darker than the drums |
| motif | a three-note cell: root, flat second, root |
| articulation | off-beat stabs on the and of 2 and 4 |
| human feel | the second note always a hair late |
| placement | bone-dry, center, tucked right above the 808 |

The 808 gets its own human-feel slot ("slides played by hand, each one a different length"). Drums, twitch percussion (sound + rhythm + where), groove and the beat switch (when + what + how it lands) are slot-built too. The lead's evolution, the bass pattern and the switch go in the lyrics-field section tags, where v6 reads structure.

- **Clash rules** keep combinations sane. A dry lead never gets chorus. An 808 melody forces a sine sub. A long motif is never played as "one held note per bar". A sparse switch never lands on "new drums".
- **Guard** (`src/one/guard.js`): every roll is checked against all the bans learned from listening tests, plus the v6 research's trigger words (drop, riser, tape, chopped, dusty, anthem, strings, pads, glitch...). Brass, tuba, organ, erhu, harmonica, flute and military snare are allowed here by request; "cinematic", "film score" and "orchestral strings" stay excluded.
- **Settings**: Variety Off (any other setting rewrites the style field), Weirdness 55–65, Style Influence 65–75, Max Mode on, v6 or v6-wild.
- **Memory**: the page remembers every roll on your device and never repeats one.

### On Android (PWA + split-screen)

`dist/one.html` is an installable PWA (`one.webmanifest`, `one-sw.js`, icons are built next to it). Serve `dist/` over HTTPS (the included GitHub Pages workflow does this on push to `main`), open it in Chrome on the phone, **Add to Home screen**, then open it in split-screen beside the Suno app. The pinned bar at the bottom copies the next Suno field with one tap (Lyrics, Styles, Exclude styles, then shows the slider values), and **Copy all as one block** / **Share** hand the whole prompt to any app. Full research, decision and the native fallback (a floating-bubble + accessibility-service filler in `android/`) are in [`docs/android-plan.md`](docs/android-plan.md).

## MANHOLE (`dist/manhole.html`)

One-of-a-kind minimalist instrumentals. Every roll crosses two reference DNAs (horror minimal / Big Sean "Blessings", one-loop hypnosis / Lil Wayne "A Milli", devil-on-the-shoulder bounce / JID "McAfee", cathedral thunder / JID "Glory", loyalty heavy / JID "Bruddanem", porch-light soul / J. Cole, bar-fight blues stomp / Prof, crisp quirk bounce / Connor Price, Detroit stop-start / Big Sean) and adds a triplet bounce over straight trap drums, an on-and-off-the-beat pocket, one house rule and a beat switch, in plain modern hip hop production language. The page remembers every fingerprint (DNA pair, rule, switch, lead, key) it has rolled on your device and never repeats one. Reference names appear in the UI only; a lint and tests keep them out of the prompt.

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

## Vocabulary rule (all engines)

Only modern hip hop and R&B production words. Field tests showed that world and folk instruments (erhu, koto, sitar, banjo, tuba, whistle, pizzicato, cello, harp, pipe organ), film words (cinematic, orchestral, eerie, haunting) and lo-fi sound design (cassette, vinyl crackle, sewer and basement rooms, found-object percussion) turn Suno's output into film-score background music. A `SOUNDTRACK` lint blocks all of them, and the Exclude field carries `cinematic, orchestral, film score, world music, ambient, lo-fi`.

The same goes for meter words: "waltz", "lilt", "limping", and any time signature like 3/4, 6/8 or 12/8 make Suno play oom-pah carnival music. The triplet feel is written as hip hop language instead ("triplet-bounce hats grouping in threes over a straight kick", "dotted-8th accents in the riff"), and a lint blocks the meter words.

## How the instrumental style field is built (ANOMALY, MANHOLE)

Listening tests showed Suno reads the style field mostly as tags and weights the front. Bar-by-bar instructions ("a half-beat of silence before every hook downbeat") buried mid-field were mostly ignored, and bare instrument names ("dark keys", "warm bassline") rendered as stock presets. So the field is now:

1. Genre tags, then three rhythm tags (one glitch, one hi-hat, one syncopation) and "hard beat switch", all in the first ~200 characters.
2. The identity sentence (blueprint opener or DNA opener).
3. Sounds from the shared bank `src/shared/vibe.js`, each one vivid phrase: bass ("gooey slime-thick 808 slides"), lead ("buzzsaw distorted 808 melody looping a brooding two-chord minor loop in C minor"), side percussion ("chopped stuttering clap edits").
4. Mix line and BPM. About 600–850 characters.

The bar-by-bar detail (signature, technical flex, glitch moves, house rule, beat-switch content) moves into the lyrics-field arrangement tags, where Suno uses structure.

## Develop

```sh
npm test        # every lane × edge × structure × seed: bans, limits, spelling, hook count
npm run build   # builds dist/one.html, dist/cipher.html, dist/anomaly.html and dist/manhole.html from src/
```
