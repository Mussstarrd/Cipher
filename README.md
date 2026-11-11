# suno-forge

One button that writes a **Suno prompt for a radio-ready hip hop instrumental**, in Suno's own field order, with a Copy button on each field. Every roll is one of one. Every sound is described physically, never as a label. A guard blocks the words that summon voices, fills, EDM drops, film score and elevator jazz. No audio uploads, no Audio Influence, no artist names.

Verified against Suno as of **2026-10-31** (v6 / v6-wild / v6-mini; every earlier model was retired 2026-09-09). Rules come from a few hundred rendered tracks (`research/handoff/`) plus the sourced platform research in `research/`.

## Use it

**The page.** Open `app/index.html` (double-click; no server, no install), or the published copy. Pick a lane or leave it on **any lane**, press **Generate**. Then the bar at the bottom: tap **COPY LYRICS**, switch to Suno, paste into Lyrics, come back, tap **COPY STYLES**, paste, tap **COPY EXCLUDE STYLES**, paste under More Options, then set the two sliders and the settings it shows. **Roll again** for a new one; the page remembers 5,000 rolls so it never repeats one and never re-uses the last three rolls' lead family or drum phrases.

In Suno: Custom (Advanced) mode, model **v6** (or v6-wild when the block says so), Instrumental **ON**, Max Mode **ON**, **Variety Off** (anything else rewrites your Styles text), no Voice, Duration **Custom** at the value shown. Audio Influence never appears because nothing is uploaded.

**In Claude Code:** `/suno-prompt phonk-experimental, 140, dark, loop` returns the eight-section block; `/suno-critique` diagnoses a bad render and rewrites; `/suno-batch jid-dark 6` makes an A/B matrix.

**Terminal** (Node 18+, no dependencies):
```bash
node forge.js                                   # interactive
node forge.js --lane any --seed 42 --qa         # one roll with the QA report
node forge.js --lane paak-pocket --bpm 94 --key "D dorian" --mood "sun-soaked" --mode straight
node forge.js --lane phonk-experimental --batch 6 --format table
node forge.js qa examples/*.md                  # validate any block (markdown or JSON)
node forge.js lanes | layouts | spec
```

## Lanes

| Lane | Sound (records never named in prompts) | Written BPM |
|---|---|---|
| `connor-price` | Bouncy playful trap-pop: bright plucks and bells over tight drums and big claps, stop-start cuts, crisp clean radio mix | 138-148 half-time, or 96-106 |
| `paak-pocket` | Live neo-soul funk hip hop: a kit played behind the beat, tine piano, bass guitar, horn stabs, warm analog mix | 86-106 |
| `jid-dark` | Dense dark boom-bap/trap hybrid: sliced soul keys and pitched-down strings over knocking drums, twitching hats, a beat switch | 140-152 half-time, or 86-94 |
| `phonk-experimental` | Memphis-rooted phonk: slowed and sliced samples, cassette grit, 808 cowbell knocks, blown-out 808, a slow dragging pocket. Not drift phonk | 132-146 half-time, or 66-74 |
| `hybrid` | Cassette cowbell drums under a live tine-and-bass pocket with bright plucks on top | 136-148 half-time, or 88-98 |

## What makes a roll one of one

About half of rolls carry a **hint**: one strange ingredient in the counter's seat, described physically and named in the head tags and one Lyrics tag. A fuzz-and-wah guitar lament with tape echo trailing behind it; a machine glitch (a chord sliced into a skipping-disc stutter, a tuned machine-shop clank, ring-modulated stabs); a nervous horn twitch (three horn jabs then silence, guitar chucks, a tambourine in bursts, recorded hot to tape); or a piano or string **hit structure** ("struck in a 3+2 pattern, five hits per bar, the last damped by hand"). Every roll also carries one **hook device** from earworm research (repeat four times with one note changed; stop one note short of the root; a full beat of silence right before the riff): its short tag sits in the head next to the lead handle and its full form in the lead line or the first full-beat Lyrics tag. Pick a hint on the page under More options, or `--hint fuzzlament|machineglitch|staxtwitch|hits|none`.

## How a roll is built

1. **Lane, key, tempo, layout.** Keys avoid the last four; the live lane leans Dorian and major-7 loops, the dark lanes stay minor. The lyrics layout (three loop, five straight, three switch) is picked first and decides the mode.
2. **Sounds from slots.** Lead = source (a lane family, e.g. "an electric piano with metal tines hit hard, the tremolo wobbling slow|the tine piano") + processing + register (low or low-mid) + motif + articulation + human-timing clause + motion + placement. Bass, drums (kick, snare, hats, a percussion colour, syncopation, feel, the beat-3 slur, an anti-transition phrase), twitch percussion, a counter instrument, and a beat-switch axis.
3. **Style field.** Head tags first: the opening, BPM, key and scale, instrumental, attitude, the lead handle, the hook tag, the drum cadence tag, a looseness tag, twitch and slur tags, the steady-drums tag, the hint tag, the mix tag. Then `lead:`, `bass:`, `drums:`, `hint:` sentences. Each roll has a density: **sparse** rolls (about 4 in 10) are lead, bass, kick, snare and air in about 650-720 characters; **mid** rolls carry the full kit; **full** rolls add one strange ingredient. Every optional part has a rank, so what makes a take one of one survives the cut.
4. **Lyrics field.** Short section tags filled with the same handles: `[Hook | the whole beat from the first second, the tine piano up front]`, `[Verse | same drums, the tine piano half as many notes]`. The drums play one pattern from the first second; no tag describes the drums leaving, stopping or returning, because Suno decorates every boundary it is told about. `[Instrumental]` first, `[End]` last, no parentheses, first full beat by 0:45, 2:30-3:30.
5. **Guard and QA.** The guard rejects voice, swing/meter, transition/filler, lead-in and boundary words, DJ and tape effects, EDM/film, elevator, negation, stock words and names; each lane allows the few words it needs and denies the ones that pull it off course. `node forge.js qa` checks limits, guard, audio references, instrumental settings, structure, the "every N bars" cap, the switch axis, sliders and models.

Full rules: `reference/grammar-and-guard.md`. Vocabulary: `reference/vocabulary-bank.md`. Suno facts: `reference/suno-field-spec.md`.

## Repository map

```
app/index.html             the page (built from app/src by scripts/build-app.js)
forge.js                   CLI
lib/guard.js               word-class rules + names list + "every N bars" cap
lib/grammar.js             shared slots and guest families
lib/lanes.js               the five lanes (families with handles, drums, bass, tempo, sliders, exclude, allow/deny)
lib/layouts.js             lyrics layouts with placeholders and runtime estimate
lib/theory.js              keys and chord loops
lib/engine.js              the seeded roll, Style cut order, lyrics fill, variations, fingerprint
lib/qa.js                  QA checker + markdown parser
lib/render.js, lib/arrange.js, lib/spec.json
reference/                 grammar-and-guard, suno-field-spec, metatag-cheatsheet, vocabulary-bank, tempo-key-tables, exclusion-presets
research/                  sourced research + research/handoff (the previous generator's pass-down and source)
examples/                  one QA-passed block per lane plus the hybrid
.claude/skills, .claude/agents
scripts/                   build-examples.js, build-app.js, audit.js, test.js
```

`npm test` walks every grammar string through the guard and rolls thousands of seeds; `node scripts/audit.js 2000 all` prints phrase shares and length histograms; `node scripts/build-examples.js` and `node scripts/build-app.js` rebuild outputs after any change under `lib/`.

## Known uncertainties

Suno publishes no character-limit table (limits are community-measured); the detailed mode is called Custom on help pages and Advanced in recent UI reports; whether section tags are honoured with the Instrumental toggle on rests on hands-on reports. The listening rules in the guard are from this repo's earlier generator, confirmed by rendering, not by Suno documentation. Details and sources: `research/01-suno-platform.md`, `research/handoff/handoff-generator-team.md`.
