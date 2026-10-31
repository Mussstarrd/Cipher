# Pass-down: how to build a Suno v6 hip hop instrumental prompt generator that actually works

This is everything we learned building ONE OF ONE over several weeks and a few hundred rendered tracks. It is written for a team starting from zero. Read it before writing any code. The full source is in the companion file `handoff-generator-source.txt`.

## 1. What you are really building

A prompt generator is not a random phrase picker. It is a **grammar of physical sound descriptions** plus a **set of rules about what Suno does with words**. Nine tenths of the quality comes from the vocabulary and the rules, one tenth from the code. The code's job is to make every roll unique, keep it inside Suno's limits, and guarantee the rules can never be broken by chance.

The target: radio-quality, infectious hip hop instrumentals with one person's fingerprint on them, where every roll is one of one (never repeats), and nothing in the prompt invites the things that ruin a track (vocals, DJ transitions, fourth-bar fills, genre drift into jazz, EDM, film score, lo-fi).

## 2. Suno v6 facts you must design around

These were confirmed by rendering, not guessed.

- **Fields and limits.** Lyrics field 5,000 characters. Styles field 1,000 characters. Exclude Styles field (under Advanced Options). Two sliders: Weirdness and Style Influence. Model picker: v6, v6-wild. Variety toggle. Max Mode toggle. An Instrumental toggle.
- **The Styles field is front-weighted and read as tags.** The first 200 to 260 characters decide the genre, tempo, key and feel. Put the identity there as short comma tags. Long prose later in the field is read loosely; bar-level instructions in the Styles field are ignored.
- **The Lyrics field is read as directions when it contains section tags.** `[Hook | full beat from beat 1, the soul stabs up front]` works. The model follows the words inside the bracket as performance and arrangement cues. This is where structure lives, never in the Styles field.
- **Words summon things.** `chant`, `choir`, `crowd`, `gospel`, `anthem` summon group voices even on an instrumental. `drop`, `riser`, `sweep`, `build`, `sidechain` pull EDM. `cinematic`, `epic`, `orchestral`, `strings`, `pads` pull film score. `swing`, `shuffle`, `waltz`, `lilt`, any time signature (`3/4`, `6/8`) produce carnival or jazz feels, not a hip hop triplet slur. `lo-fi`, `vinyl crackle`, `dusty` pull lo-fi. `smooth`, `mellow`, `Rhodes`, `sax` pull elevator jazz. `Hammond`, `wail`, `twang` pull blues. The fix is never a longer prompt. It is a guard that forbids the word and a vocabulary that says the same thing another way.
- **Negations do not work.** "no reverb" reads as "reverb". "no fills" reads as "fills". Say the positive instead ("dry", "hard cuts only") and put the ban in the Exclude box.
- **Artist names do nothing useful** and can get a prompt rejected. Never put a name in a prompt. Keep a list of names in the guard so none slips in from a research note.
- **Variety Off, Max Mode On.** Variety On blurs the specifics you wrote. Max Mode keeps the silence silent on minimal tracks.
- **v6 for radio, v6-wild for the weird lane.** v6-wild at Weirdness 70 to 85 is where the strange, great accidents happen. For everything else Weirdness 40 to 55 and Style Influence 75 to 85 landed closest to the written prompt.
- **Suno collapses similar descriptions into the same sound.** Two rolls that both say "a plucked synth lead" come back sounding identical even if the rest differs. Uniqueness must be in the physical description of the sound, not in the arrangement words.

## 3. The style field, line by line

Four sentences, always in this order, and the total is checked against 1,000 characters with a cut order that never removes the head or the lead.

1. **Head tags**, comma separated, no sentence: `anchor (subgenre and the two or three sounds that define it), BPM, key, instrumental, syncopation tag, twitch tag, beat-3 slur tag, "minimal" when the layout is minimal, mode tag (beat switch axis, or "same two bars all song", or "the beat never lets up"), low-lead tag, lead handle plus a short timbre clause, mix tag`. Everything Suno must not miss goes here. Keep each tag under eight words.
2. **lead:** one physical sentence: the sound and how it was made, its processing, its register, the figure it plays (only if the sound itself does not already state one), the articulation, the human-timing clause, the stereo placement.
3. **bass:** the source (which 808 or sub, physically), its tone, its human-timing clause.
4. **drums:** kick, snare, hats, one percussion colour, and one anti-transition phrase ("drums cut dead, then slam back on the one", "the beat stays frozen from start to finish").
5. **counter:** a second instrument and its role, when there is room. When there is not, the counter is named in the lyrics tags by its handle and Suno still adds it.

Cut order when over 1,000: counter detail, then bass tone, then percussion colour, then lead placement, then lead human clause, then the lead's register (the head's low tag already says it), then the lead's timbre clause in the head. Measure over thousands of seeds that nothing ever exceeds 1,000 and that the drums line survives in most rolls. We lost the drums line in early builds and tracks came back as generic trap.

## 4. Every sound is a physical description with a handle

The single most important rule. Never a label. Not "piano", but "a grand piano recorded close, the hammer thud louder than the note, playing two chords per bar, the sustain pedal down so the chords smear into each other". Not "flute", but "a low flute sample pitched down an octave, dark and woody, playing a four-note minor riff over the 808". Each source line carries a short **handle** after a pipe (`...|the close piano`) so the lyrics tags can refer to the same sound by name.

Each instrument family has eight sources and seven processing characters. A roll picks one of each, so a family alone gives 56 combinations before motif, articulation, register and placement multiply it. Families we found worth having for this music: found sounds and toys (door hinges, car alarms, rubber squeaks, smoke alarms, tuned knocks), mutant and plucked synths, guitar (picked, muted, bridge pickup, lap steel through fuzz), drunk and drawbar organs, low brass and trumpet stacks, flipped wind (flute, erhu, harmonica, melodica), bounce plucks, sliced soul records, low and cold pianos. Every lead register is low or low-mid: high leads come back shrill and radio never uses them. Say "pitched down into the low mids" or "parked under the snare" in the source itself.

Timbre and feel words that reliably humanise a sound: "played by hand", "the second chord always a hair late", "a few cents off, never corrected", "breath noise and reed buzz left in", "the pick click left in", "one take, the poked wrong notes left in", "resampled until the grain shows", "pitched down until it sounds heavy". Quantised, robotic results came from prompts that had none of these.

## 5. Rhythm vocabulary: the owner's fingerprint

The fingerprint is syncopation, a drunken triplet slur on beat 3, twitchy percussion, and non-quantised feel, with a slurred harmonica or erhu somewhere in the low mids. Each of these is a list of short head tags written in hip hop language and tested in Suno:

- Syncopation: "kick a 16th ahead of one", "808 on the and of 4", "kick skipping beat 1", "double kick before each snare", "snare early, 808 late".
- Beat-3 slur (the "waltz feel" without ever saying waltz): "808 stumbles into three on beat 3", "beat 3 trips, beat 4 straightens", "drunk triplet skip on beat 3", "the pocket slips drunk on beat 3", "third beat slurs, downbeat snaps back". Any phrase with waltz, lilt, shuffle, swing or a time signature came back as carnival music.
- Twitch: "nervous perc ticks between kicks", "jittery 32nd knock bursts", "twitchy knock flams before the snare", "shaky hat retriggers under the loop".
- Human feel: "unquantized, the snare a hair late", "the 808 slides played by hand, each one a different length", "clap layers a few milliseconds apart".

The slur tag appears on about 60% of rolls, and the signature family (harmonica, erhu, two-string fiddle, fuzz lap steel, melodica, all described physically and pitched into the low mids) takes the lead about a fifth of the time and answers as the counter otherwise. A fingerprint that appears on every roll stops being a fingerprint and becomes a rut; the owner complained the moment harmonica and flute were on everything.

## 6. No transitions, no fillers, ever

The owner's hardest rule, and the one most generators break by accident. Every section change in Suno defaults to a fill, a riser or a fourth-bar roll unless you prevent it three ways at once:

1. **Vocabulary.** The words fill, roll, riser, sweep, build, transition, lead-in, pickup, "comes in", "enters", "every 4 bars", "every other bar", "bar 4", "4th bar", "cuts out every 8th bar" never appear anywhere in the grammar. We audited 2,000 rolls with a regex and reworded every hit. Even "a sparse kick that skips bar 4" had to go.
2. **Positive instruction.** The drums line always carries one anti-transition phrase: "hard cuts only, straight in on beat 1", "the same loop the whole way", "every section identical except one sound", "sections change on a hard cut, nothing in between". Beat switches land "on a hard cut, beat B slams in on the one", never "after one bar of silence" or "the 808 comes in first".
3. **The Exclude box.** `drum fills, tom fills, snare rolls, hat rolls, drum rolls, transitions, transition fills, section transitions, risers, sweeps, filter sweeps, white noise sweeps, build-ups, breakdown FX, impacts, crash cymbals, reverse cymbals, DJ drops, DJ scratching, tape stop, phaser, cowbell`.

A related cap: any phrase of the form "every N bars" is budgeted to at most one per roll and zero in loop mode. A motif that changes every 4 bars plus hats that cut every 8 plus a counter every 4 produced exactly the four-bar-filler feel the owner hates.

## 7. Structure lives in the lyrics field, and it must be diverse

Each roll picks a **layout** before it picks sounds, and the layout decides the mode. Twelve layouts, weighted so about a third of rolls are minimal:

- Two-bar loop, nothing changes, 2:00 (loop)
- Stripped to kick and lead, 3 minutes (loop)
- One sound added, one sound removed (loop)
- Hook first (straight)
- Verse bare, hook full (straight)
- Slow and sad: long intro on the lead, the motif grows (straight, the ballad shape)
- Call and response, two-bar phrases (straight)
- Lead out for 8 bars, slams back (straight)
- Long outro fade (straight)
- Half-time drop-down for the last third (switch)
- A then B, two beats one song (switch)
- Drums cut, lead alone, then beat B slams in (switch)

Each layout is a template of section tags with placeholders: `{LEAD}` `{COUNTER}` `{BASS}` `{KICK}` `{HATS}` `{SNARE}` `{SYNC}` `{SLUR}` `{FEEL}` `{MOTION}` `{CUE}` `{AFTER}` `{LAND}`. Each placeholder has a length limit so a tag stays under about 100 characters, and a fallback when the value is null. A placeholder whose value does not fit is dropped whole, never cut mid-word: a cut-off phrase ("a gap on beat 1 so the hook hits h") is worse than none.

Switch mode rule: the beat switch has one axis (half-time, double-time, key falls a minor third, the floor goes out, strip to 808 and snare), and the axis words appear in the head tag, in the `[Beat Switch | ...]` cue and in every section tag after it, so nothing ever "returns" to the first beat. Loop mode rule: every `[Loop | same loop, identical, ...]` tag says identical, and the counter is dropped.

Intros vary between 2, 4 and 8 bars. The lyrics always open with `[Instrumental]` and close with `[End]`.

## 8. The archetype lanes

Random vocabulary makes random music. Radio quality came from **lanes**: each lane is one archetype of what has actually run radio for a decade, with its own anchors, instrument families and weights, tempo ranges, drums (kick, snare, hats lists), 808 patterns, switch axes, key colour (share of major keys) and mix tag. Ours, with the records they were reverse-engineered from (never named in prompts): squeak and space (The Box, Savage), country-trap loop (Old Town Road, A Bar Song), drawbar triumph (Glory, Industry Baby), soul slice over a modern 808 (Mmhmm, Texas), flute and 808 menace (Mask Off, Suge), pain piano (Outside Today, Lonely Child), cold southern trap (Back in Blood, FNF), West Coast bounce (Not Like Us, Ballin'), guitar emo (Lucid Dreams, SAD!), pop-trap earworm (God's Plan, fukumean), trunk 808 switch (Sicko Mode, Like That), plus a slow-and-sad lane and a weird lane on v6-wild at about one roll in eight.

Two lessons from lanes. First, a lane's anchor must not promise a sound the roll does not carry: when the signature family or a guest sound takes the lead, the anchor and mix tag switch to a sound-neutral version ("pain trap, one low sad lead, triplet hats, long 808 slides"). Suno invents whatever the header names. Second, every lane draws its kicks, snares and hats from its own list plus the shared palette, so the same "deep thud kick with the low end cut" does not open every track. Measure it: we track distinct phrases and the top phrase's share over 1,500 rolls (kick 36 distinct, top 7%; snare 46, top 6%; hats 32, top 7%).

## 9. The guard

A regex guard runs over every finished prompt and over every string in the grammar in the test suite. Rules: film score, world and folk words; time signatures and swing words; voice words (on instrumental packs); DJ and transition FX; off-format genres; EDM, voice and lo-fi trigger words; genre-drift magnets (sax, Rhodes, Hammond, supersaw, chiptune and the rest); negations ("no X"); and a list of a few hundred artist and producer names. Each pack can unlock specific words it needs ("country" for the country-trap lane, "banjo" described physically over trap drums). A single test walks the whole grammar and fails on any hit, so the vocabulary cannot drift as it grows. Keep the names list clean: ours once contained common words ("ray", "scoop", "call back") from a careless import and blocked ordinary phrases for days.

## 10. Uniqueness

- Seeded random (mulberry32). Same seed, same package, so a roll can be reproduced and tested.
- A fingerprint per roll (lane, lead source, character, motif, articulation, human clause, mode, key). The page remembers the last 5,000 fingerprints in local storage and re-rolls on a repeat.
- The page also remembers the lead family, bass source, kick, snare and hats of the last three rolls and re-rolls when any would repeat, because Suno makes near-identical tracks from near-identical phrases.
- Keys are picked to avoid the last four keys. The major-key share is decided once per song, per lane.

## 11. The page

One button. Suno's order: Lyrics, then Styles, then Exclude, then the sliders and settings, each with its own Copy button and a character counter. A pinned bar copies the next field in order so the user can split-screen beside Suno and paste, come back, tap, paste. Copy-all as one block, Web Share on Android, installable as a PWA with an offline copy. No settings, no menus. The owner asked for "aesthetically pleasing and easy for an end user" and that meant removing options, not adding them.

## 12. Vocal packs, if you go there

Same engine, with `VOCAL: true`: the vocal exclusions are dropped from the Exclude box, the guard unlocks voice words for that pack only, Instrumental is Off, and a lyric writer produces the whole lyrics field with performance cues in every tag (`[Hook | biggest, belted, stacked]`). Chant, choir, crowd and gospel stay banned even then: they summon group voices. For spoken delivery, "spoken not sung, no melody, no harmonies, a hair behind the beat, line ends slurred, breaths left in" in both the Styles vocals sentence and the section tags, ellipses in the lyric lines at the pauses, and never a double-time cue, which snaps the flow to a 16th grid and sounds robotic.

## 13. How we tested

67 automated tests over thousands of seeds: every grammar string passes the guard; every roll is under the field limits; the every-N-bars cap holds; switch rolls repeat their axis words after the switch and never say "returns"; every roll has a counter and twitch percussion; the lead's slots all reach the field; the slur, low-lead and minimal tags land in the front 260 characters; lane shares and layout shares match their weights; fingerprints never repeat across 3,000 seeds. On top of that, a **critic pass**: generate 24 rolls, hand them to a separate reviewer with the brief, and fix what it finds (phantom instruments in headers, stacked "playing" clauses, fragments from mid-word clipping, periodic fillers). Then render in Suno and listen. The owner's ear found things no test did: "all horns sound the same", "high leads are frequent", "after the Amen it goes quantised".

## 14. Mistakes we made so you do not

- One-phrase labels for sounds. Everything came back generic.
- Letting the drums line get cut for length. Tracks became stock trap.
- "Hats rolling into the hook" and "the 808 comes in first". Instant fills.
- Describing the beat-3 feel with waltz, swing or 3/4. Carnival music.
- A fingerprint sound on every roll. The owner tired of it in a day.
- Anchors that named instruments the roll did not carry. Suno added them.
- Clipping placeholders mid-word. Nonsense inside section tags.
- Common words in the names list. The guard blocked half the vocabulary.
- Double-time cues on spoken vocals. Robotic.
- Genre buttons. The owner wanted one button that does the right thing, with the diversity inside the roll, not in a menu.

## 15. Build order that worked

1. Guard and names list, with a test that walks every string.
2. Base grammar: families with physical sources and handles, rhythm tags, drums, bass, the exclude list.
3. Engine: seeded roll, head tags, four lines, cut order, fingerprint.
4. Lyrics layouts with placeholders, limits and fallbacks.
5. Lanes from real records, with sound-neutral anchors.
6. The personality layer: signature family, slur, syncopation, twitch, anti-transition phrases.
7. Variety: pooled drums and bass, page memory, measured phrase shares.
8. Page in Suno's order with copy buttons and the paste bar.
9. Audit scripts: filler regex over 2,000 rolls, distinct-phrase counts, length histograms.
10. Critic pass, then listen, then fix, then repeat.
