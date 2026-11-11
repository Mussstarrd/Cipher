# The grammar and the guard

How suno-forge writes a prompt, and why. Nine tenths of the quality is vocabulary and rules; one tenth is code. The rules come from a few hundred rendered Suno tracks (`research/handoff/handoff-generator-team.md`, this repo's earlier generator) plus the platform research in `research/`.

## 1. Every sound is a physical description with a handle

Never a label. Not "piano" but "a grand piano through a distortion pedal, one chord per bar". A label points Suno at the average of thousands of tracks: the preset. Physical detail pushes it off the average. Each source line carries a short **handle** after a pipe (`...|the distorted piano`) so the Lyrics tags can refer to the same sound by name.

Every lead is built from slots (`lib/grammar.js`): **source** (from a lane family), **character** (processing, from the same family so a tuba never gets "ring-modulated"), **register** (low or low-mid: high leads come back shrill), **motif**, **articulation**, **human** (the anti-quantize clause: "played by hand, never snapped to the grid"), **motion** (how it changes across the song), **placement**. Bass: source, tone, pattern, human. Drums: kick, snare, hats, one percussion colour, a syncopation phrase, a human-timing phrase, the beat-3 slur, and always one anti-transition phrase. A counter instrument from another family with a sparse role.

Suno collapses similar descriptions into the same sound, so uniqueness lives in the physical description, not the arrangement words. A roll's **fingerprint** (lane, lead source, character, motif, articulation, human clause, mode, key, counter) never repeats across thousands of seeds, and the page refuses to re-use the last three rolls' lead family, bass, kick, snare or hats.

## 2. The Style field, line by line

**Head tags** (comma list, never cut): the opening (lane anchor, or the lead over the drums, or the drums first; it only promises sounds the roll carries), `<BPM> BPM` (+ `half-time` when written 120+), `in <key>`, `instrumental`, the attitude tag, mood words, the lead handle, the hook tag (the short form of the hook device), one syncopation tag, one twitch tag, the beat-3 slur tag, `minimal` on minimal layouts, the switch axis or loop tag, the anti-transition tag, the hint tag, the low-lead tag, must-haves, the sprinkle tag, the mix tag. Suno reads the first 200-260 characters as tags and decides genre, tempo, key and feel from them.

Then the sentences: **lead:** source, processing, the hook device, the human-timing clause, then motif, articulation and placement while there is room. **bass:** source, pattern, tone, human. **drums:** kick, snare, hats, colour, twitch, slur, anti-transition phrase. **hint:** source, role, character. **counter:** only when there is no hint, never in loop mode.

Every optional part has a rank (`styleField` in `lib/engine.js`). Over 1000 characters the lowest rank goes first, and a part with a short form shrinks before it goes: counter, lead placement, hint detail, bass human and tone, articulation, the mix tag (to its first clause), the slur phrase, hats (to a short form), bass pattern, the anchor (to its first clause), motif, the twitch clause (to its short form), hats, the hook device in full (its tag stays in the head), human timing, twitch, the hint. A restore pass then puts back whatever fits. Never cut: the head tags, the lead source and processing, the bass source, kick, snare and the anti-transition phrase. Whatever the Style field could not hold in full (hook device, motif, hats, human timing, bass pattern) is written into the Lyrics tags, one detail per section, so nothing rolled is lost. `node scripts/selfaudit.js` measures all of it. Losing the drums line is how tracks come back as generic trap.

## 3. The Lyrics field is directions

`[Hook | the whole beat from the first second, the sliced soul keys up front]` is followed as arrangement and performance cues. Structure lives here, never in the Style field (bar-level instructions in Styles are ignored). Each roll picks a **layout** before it picks sounds (`lib/layouts.js`): four sparse loop layouts (the same two bars all song; about four rolls in ten), five straight layouts (hook first, two leads trading, verses mostly air, a motif that grows, the drums alone first) and two switch layouts (two drum patterns in one song). Placeholders (`{LEAD}`, `{KICK}`, `{SPACE}`, `{CUE}`, ...) are filled with the roll's handles and phrases; a value that does not fit its limit is dropped whole, never cut mid-word. A feel or motion cue is never said twice in one lyric.

Rules: `[Instrumental]` first, `[End]` last, no parentheses (sung as backing voices), no bare lines. There is no intro, break or outro: the drums play from the first second and the song ends on `[End]`. The first full beat lands by 0:45 and every layout runs 2:30-3:30 at its lane tempos (estimated from nominal bars; the Duration control is set to the estimate plus a buffer). Tags stay short: every extra instruction on a section line is something Suno stages at that boundary.

**Switch mode** has one axis (half-time, double-time, key falls, new drums, strip to 808 and snare, flip to off-beats). Its words appear in the head tag, in the `[Beat Switch | ...]` cue and in every section after it, so nothing ever goes back to pattern A. **Loop mode**: every `[Loop | ...]` tag says identical and the counter is dropped.

## 4. No transitions, no boundaries, ever

Suno decorates every boundary it is told about. Listening tests (2026-11) came back with a reversed kick leading into every fourth bar and every "drop", and with DJ scratches, from prompts that said "hard cuts only, straight in on beat 1", "drums cut dead", "a 32nd burst at the end of each bar", "a slide up into each new bar" and "a soul record". Describing a boundary in order to forbid a transition still names the boundary. So:

1. **Vocabulary.** The guard bans, over every string: fill, roll, riser, sweep, build, transition, lead-in, pickup, "comes in", "enters"; any section starting, stopping, cutting or returning ("hard cuts", "stops dead", "straight in", "drums out", "back"); any hit placed against the end of a bar ("before the downbeat", "last beat", "into the next bar", "beat 4", "the and of 4", turnaround, anticipate); any periodic event ("every 4 bars", "one bar in eight", "every other pass"); and reverse, record, retrigger, stutter, lurch and skipping words.
2. **Steady state.** The drums line always carries a steady-drums phrase: "every bar of drums identical", "one drum pattern from the first second to the final one". The drum pattern itself is rolled from `CADENCE` (where the kick and snare land, one fixed bar or two) so the bar is not the genre default.
3. **Layouts.** The drums play from the first second and never leave. Verses and hooks differ only in what the melodic sounds do.
4. **The Exclude box.** `drum fills, snare rolls, risers, sweeps, transitions, reverse kick, lead-in fills, beat drops, turntable scratches, stutter edits` in every lane's list.

## 4b. Space and density

A long prompt reads as a busy record. Sparse rolls carry lead, bass, kick, snare and one `SPACE` phrase ("half of every bar left empty", "the gaps as loud as the hits") in a Style of about 650-720 characters, with a space tag in the head and no hats, twitch, hint or counter. Mid rolls carry the full kit; full rolls add a hint or a sprinkle.

## 5. Rhythm vocabulary: the fingerprint

Syncopation, a drunken triplet slur on beat 3, twitchy percussion, and non-quantised feel, in hip hop language. The beat-3 slur is never "waltz", "lilt", "shuffle", "swing" or a time signature: those come back as carnival or jazz-club music. Say it physically: "the 808 stumbles into three on beat 3", "the pocket slips drunk on beat 3". Humanising words that work: "played by hand", "the second note always a hair late", "a few cents off, never corrected", "breath noise left in", "one take, the mistakes left in", "resampled until the grain shows". Modifiers "slight" and "subtle" keep imperfection from tipping into chaos. The slur tag appears on about 60% of rolls; a fingerprint on every roll becomes a rut.

## 5b. Hints and hook devices: the anti-generic layer

Random vocabulary makes random music; a lane makes radio music; a **hint** makes one of one. About a third of rolls (the full-density ones) carry one strange ingredient (`lib/hints.js`), named in the head ("a faint hint of the crying fuzz guitar"), described in a `hint:` line after the drums, and placed in one Lyrics tag by its handle. Six families, each physical: **fuzz lament**, **machine glitch**, **nervous horn twitch**, **hit structures** (a piano or string hit described as a rhythmic cell and a way of striking and damping it), **found sound** and **low brass**. The tape-lurch, reverse-bloom, radio-static and dropped-beat families were removed in 2026-11: they are transition effects by construction and came back as DJ effects and beat drops.

About three leads in ten come from a guest family outside the lane (`FAMILIES` in `lib/grammar.js`): found objects, synths, organs, guitars, winds, **world** (koto, oud, sitar, banjo, dulcimer, steel pan, accordion, pipe drone) and **machine** (printer, modem, sewing motor, sonar, typewriter, engine).

Each lane weights the families (the live lane leans horn twitch, the dark and phonk lanes lean glitch and lament, every lane likes hit structures). A hint never doubles the lead's family. The safer variation drops the hint; `--hint none` or `--hint hits` forces it.

Every lead line also carries one **hook device** from earworm research (Jakubowski 2017: faster tempo, a common contour plus one unusual interval; Margulis: repetition with small variation is rated more enjoyable; Huron and Salimpoor: the payoff is felt in the anticipation gap; Lamere: a quarter of streams are skipped inside five seconds, so the lead sounds in the first two bars): "the hook repeats four times, the fourth time with one note changed", "the phrase stops one note short of the root, every time", "a full beat of silence right before the hook, then the hook". Groove, with numbers: medium syncopation beats both none and maximum (Witek 2014, Sioros 2014), so one displaced hit per bar in the kick or 808 while the snare and hats stay put; a laid-back snare lands about 17 ms late at 96 BPM and is played harder (Danielsen 2015), hence "a hair late and hit harder for it"; exaggerated timing rates lower than tight timing (Senn 2016), hence "slight" and "subtle" and never the number in the prompt. Sources and the numbers: `research/06-addictive-sound-theory.md`.

## 6. The guard (`lib/guard.js`)

Word classes that sent a rendered take somewhere the prompt did not intend. A regex guard runs over every finished prompt and, in the test suite, over every string in the grammar, lanes and layouts.

| Rule | Examples | Why |
|---|---|---|
| voice | vocal, rap, sing, chant, choir, hum, gospel, crowd, anthem, call-and-response, scat, talk box, soulful, oohs | summon voices into an instrumental |
| meter and swing | waltz, lilt, shuffle, swing, swung, 3/4, 6/8, carnival | oom-pah carnival or jazz-club feels |
| transitions and fillers | transition, sweep, riser, fill, roll, build, drop, impact, crash, reverse cymbal, tape stop, pickup, comes in, enters, bar 4, every other bar, crescendo | fills and FX every few bars |
| EDM and film-score magnets | EDM, dubstep, house, sidechain, supersaw, festival, cinematic, epic, orchestral, trailer, ethereal, lush, strings-family instrument names, music box, whistle | drops or trailer music |
| elevator and lounge magnets | sax, Rhodes, Wurlitzer, Hammond, smooth, lounge, mellow, jazz, chill, lo-fi study | smooth jazz and elevator music |
| negation | "no X", "without X" | plants X |
| vague stock words | realistic, complex drums, professional, catchy, vibes | the preset |
| names | artist, producer, label and record names; "type beat", "in the style of", comparisons to a capitalised name | Suno rewrites the whole field |

Lanes **allow** specific words they need (phonk: cowbell, cassette, lo-fi, Memphis, chop; neo-soul: funk, soul; dark hybrid: strings) and **deny** extra words that pull them off course (bright pop: lo-fi, dusty, distorted 808; live pocket: 808, drum machine, quantized; phonk: drift, gym, Brazilian, cowbell melody). Base-grammar phrases that trip a lane's deny rule are simply not offered to that lane. User mood and must-have words go through the same guard; anything that trips it is dropped and reported in the block.

Keep the names list clean: common words in it block ordinary phrases.

## 7. Sliders and models

v6: Weirdness 40-55 and Style Influence 75-85 landed closest to the written prompt in the first listening tests; the lanes now run 48-65 (raised 2026-11-11 for less conventional takes). Above Weirdness 60 on v6, tempo and structure drift. v6-wild at Weirdness 65-80 and Style Influence 55-70 is where the strange, great accidents happen; the phonk and dark-hybrid lanes suggest it most often, and the weirder variation always targets it. Variety Off (it rewrites the Style text), Max Mode On (it also keeps silences silent on minimal rolls).

## 8. Measuring, not guessing

`npm test` walks every grammar string through the guard, rolls thousands of seeds inside the limits, checks the head carries the identity in the front 300 characters, that the drums line and the anti-transition phrase survive, that every lyrics line is a bracket tag, that "every N bars" stays capped, that switch rolls repeat their axis, that fingerprints never repeat, and that no single kick, snare or hats phrase takes more than a third of a lane's rolls. `node scripts/audit.js` prints phrase shares and length histograms. Then render in Suno and listen: the ear finds what tests cannot ("all horns sound the same", "high leads are frequent", "after the switch it goes quantised"); turn each finding into a guard rule, a clash rule or a vocabulary change, and add a test.
