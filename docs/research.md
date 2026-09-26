# CIPHER research notes

This is what the engine in `src/engine.js` is built on. Section numbers match the comments in the code.

## 1. House rules (the brief)

Every package has to be a hip hop or R&B banger: syncopated, able to go experimental or twitchy, and infectious. Nothing below may leak in:

| Banned | How the engine keeps it out |
|---|---|
| Fills, filler, tom fills | Excluded, and the style always carries a competing positive: "locked unbroken drum loop", "hard cuts between sections", "808 and hi-hats carry every transition". Section tags use hard cuts, mutes and 808-only breaks for transitions, never a fill. Verse guides call for no throwaway bars. |
| Rimshot, cowbell | Excluded. No lane lists either one; percussion is limited to kicks, claps, snaps, snares and hi-hats. |
| DJ effects | `dj scratch` is always excluded; `airhorn` and `dj tag` are one tap away. No scratches, rewinds, tape stops or DJ drops appear in the vocabulary. |
| Jazz, funk | Excluded. The lanes avoid the instruments that drag a track that way (sax, trumpet, horns, Rhodes, clavinet, talkbox, wah, slap bass), and the progressions skip ii–V motion. The linter flags them if they ever show up. |
| EDM | Excluded. No "drop", "build-up", "riser", "supersaw" or four-on-the-floor. The old `[Build]` / `[Drop]` tags are replaced with `[Beat Switch]`. |
| Pop | Excluded. No I–V–vi–IV progression, and no "pop" or "catchy pop" descriptors. "Infectious" and chant language carry the hook instead. |

A test (`test/engine.test.js`) generates every lane × edge × structure × 12 seeds and fails if any banned word reaches the style text or the section tags.

## 2. Suno v6 facts the engine relies on

- The v6 family shipped on 9 September 2026, and every earlier model was retired for new songs the same day.
  - **v6** (Pro/Premier) is the precise flagship. The engine uses it by default.
  - **v6-wild** (Pro/Premier) is less predictable and pushes further from the prompt.
  - **v6-mini** (all plans) is faster and cheaper, so the engine treats it as a test tier.
- The style box (1,000 characters), Exclude Styles (1,000) and lyrics box (5,000) are unchanged, and so is the tag syntax. Songs can run up to 8 minutes.
- **Variety** (new slider: Off, Normal, High, Extra, Max) rewrites the style prompt on every generation when it is above Off. It defaults to Normal on v6 and v6-mini and to Off on v6-wild. The engine always recommends **Off**, because a rewritten prompt can bring back what the bans keep out.
- **Max Mode** (new toggle, 2× credits) spends more compute on keeping the whole song consistent. Suno recommends it for longer songs and anything you'll publish, so the engine recommends it for the keeper on v6 and v6-wild.
- v6 tends to run long outros and hum through intros when the edges aren't specified. The fix is to state the start and end in positive terms in both the style field and a tag. The engine adds "opens straight on the … motif and hard-stops after the last hook" to the style and `[Outro: hook motif for 2 bars, then hard stop]` to the lyrics. `humming` is pre-selected in the Exclude field.
- Give the style prompt a clear hierarchy: one lead genre, one rhythmic identity and a few defining sounds. A blend stays a single phrase ("X fused with Y") under one lead genre.
- Artist names are still rewritten on v6.

Carried over from v5/v5.5 and still valid:
- Style field: about 6–12 distinct descriptors land best. Under 5 is too vague; past 15–18 the prompt just repeats itself. The engine targets 8–12 and drops the lowest-priority descriptors first.
- Suno gives more weight to terms near the start of the style field, so genre, vocal and core groove come first and texture comes last.
- v5.5 follows vivid, written-out description better than v5 ("slightly detuned vintage keys, played a hair behind the beat"). The engine writes phrases like "a detuned synth playing a cold all-minor loop in G minor" instead of bare tags.
- Keep tempo and key out of brackets in the style field. Brackets belong in the lyrics box.
- Inline section descriptors work: `[Verse 1: triplet flow, beat locked in]`. Clear cues at section edges make clean cuts instead of blends.
- For rap, two verses plus hooks is the sweet spot. Four or five verses wander.

## 3. Negative prompting

- "no X" inside the style field is unreliable, because naming the thing plants it. The official control is Exclude Styles (Custom Mode → Advanced Options).
- Exclusion is probabilistic. It lowers the odds and won't beat a style prompt that strongly implies the excluded thing. So every ban is paired with a positive that competes with it.
- Guides suggest 2–4 exclusions for the strongest effect. The brief requires nine, so the list is ordered by risk: genre bans first (jazz, funk, edm, pop), since a genre drift ruins a take, then the specific percussion and DJ elements. The style prompt carries most of the weight.

## 4. Sliders

- Weirdness raises unexpected choices. Style Influence controls how strictly Suno follows the style text.
- When the genre drifts, change Style Influence before rewriting the prompt. When a take sounds generic, raise Weirdness 5–10 points and change nothing else.
- The engine never recommends Style Influence below 65. The bans rely on the style text being followed.

| Edge | Weirdness | Style influence |
|---|---|---|
| Syncopated | 35–45 | 75–90 |
| Twitch | 45–60 | 70–85 |
| Experimental | 60–75 | 65–80 |

On v6-wild, Weirdness drops 15 points and Style Influence rises 5, since the model already supplies the unpredictability. Variety stays Off on every model.

## 5. Harmony: keys, loops and cadences

- Trap and drill are built mostly on minor keys: natural minor (Aeolian), harmonic minor for the tense major V, and Phrygian for the half-step ♭II dread. R&B adds 7ths and 9ths and borrowed chords.
- The engine's progressions and the cadence each one ends on:

| Progression | Roman | Cadence | Feel |
|---|---|---|---|
| Two-chord pendulum | i – ♭VI | open loop | hypnotic, never resolves |
| Phrygian hover | i – ♭II | Phrygian | drill and dark-trap dread |
| Harmonic-minor sting | i – ♭VI – V | half cadence | tension snaps back every bar |
| Descending lament | i – ♭VII – ♭VI – V | Andalusian | inevitable falling bassline ("Mask Off", "SICKO MODE") |
| Aeolian climb | i – ♭VI – ♭III – ♭VII | subtonic | cinematic |
| Cold fifth | i – v – iv – ♭VII | subtonic | all-minor chill ("Lemonade") |
| Minor plagal sway | i – iv | plagal | soulful sway |
| Pedal drone | i | none | rage: all motion is in the 808 |
| Drill climb | i – ♭VI – ♭III – V | half cadence | ominous |
| Late-night climb | i9 – ♭VImaj7 – ♭IIImaj7 – ♭VII | subtonic | dark R&B |
| Minor-ninth sway | i9 – iv9 | plagal | trap soul |
| Aeolian resolve | ♭VImaj7 – ♭VII – i9 | Aeolian | the hook lands like a verdict |
| Backdoor home | iv7 – ♭VII9 – Imaj7 | backdoor | the R&B/gospel road home |
| Slow-jam cycle | vi9 – IVmaj7 – Imaj7 – Vsus4 | suspended | silky |
| Plagal float | IVmaj7 – Imaj7 | plagal (major) | pluggnb weightlessness |
| Plugg descent | IVmaj7 – iii7 – ii7 – Imaj7 | stepwise descent | dreamy |
| Bittersweet iv | Imaj7 – iii7 – IVmaj7 – iv6 | borrowed minor plagal | late-night sigh |

- Harmonic plans:
  - **One loop** (hip hop default). The same progression runs under every section, and the hook lifts through layers and vocal stacks. Repetition is the hook.
  - **Hook lift** (R&B default). Verses ride a two-chord vamp built on the tonic. The pre-chorus holds the chord that leads back into the tonic, the unresolved tension. The hook brings the full loop and its cadence. The bridge uses a deceptive chord (♭VI in minor, vi in major) so the last hook hits harder.
- Keys are spelled the way DAW key pickers show them, with one accidental family per key. The tests check this.

## 6. What makes a hook infectious

- Earworms follow a common melodic contour, use longer notes and smaller intervals, and are easy to sing back. Listeners remember the shape more than the exact pitches.
- The strongest hooks repeat with a small variation: the cell comes back, but the last note lands higher or the rhythm clips.
- Repeated exposure builds the earworm. That's why the hook shows up at least three times, arrives within about 15 seconds, and is marked "identical every time".
- In hip hop, an instrumental riff often carries the hook as much as the words do. The engine always adds a one- or two-bar motif "repeating every hook".
- Shorter songs with tight hooks win, so templates run from about 2:00 to 3:00.

Hook formulas in the blueprint: Chant loop (A A A B), Call & response, Stair-step (A A′ A″ B, repetition with variation), Two-line mantra (A B A B), and Stutter hook (the twitch edit is the hook). Melody guidance names real pitches from the chosen key: tonic, third and fifth.

## 7. Artist likeness without artist names

Suno strips or rewrites artist names in prompts, on v6 as before. A likeness in CIPHER is therefore a bundle of describable traits, never a name:

- **vocals**: two or three vocal-timbre descriptions (register, texture, auto-tune, breath).
- **signature**: one line about the flow or phrasing habit, always front-loaded in the style field.
- **tells**: two or three production choices the artist's records share; one is picked per roll.
- **flows, hooks, ad-libs, tempo**: the section tags and hook blueprint draw from the profile instead of the lane, and the BPM is narrowed to where the artist's records sit inside the chosen lane.
- **home lane and template**: picking a likeness jumps to its natural lane and, for some, its natural structure (for example a beat switch).

Profiles avoid the jazz, funk and live-band sides of an artist's catalog so the house bans still hold. A test generates every likeness on every lane, edge and seed, and fails if any name or alias reaches the style, tags or blueprint.

## 8. Randomizer, flavors and the lead pool

- The page is a single Roll button. Each roll picks lane, edge, likeness (55%), blend (30%), structure, hook, post-hook, vocal mode and a seed; the engine renders that recipe deterministically, so a roll number reproduces a keeper.
- **Pop rap** is a lane now, so `pop` left the Exclude list; `bubblegum pop` stays out. Pop Rap uses bright plucked synths, marimba, music box and snap-heavy drums with clean rapid-fire rap and sung hooks.
- **Phonk** is a lane built without cowbell (banned): pitched-down chopped vocal samples, blown-out 808s, crunchy off-grid drum-machine hits and cassette haze carry the Memphis identity.
- **Acid Soul Rap** covers the psychedelic Chicago sound: warped soul-sample keys, gospel organ, juke-style syncopation, elastic yelped rap-sing.
- **Flavors**: every style field carries one phonk adjective, one syncopation adjective and one experimental adjective, drawn from three pools, so Suno is told on every roll to go for grit, off-grid rhythm and left-field edits.
- **Lead pool**: 55% of rolls take the lead instrument from a shared pool of 40 (kalimba, celesta, harp, cello, koto, erhu, duduk, harpsichord, glass harmonica, theremin, steel drum, mbira, and so on) instead of the lane's own list. The instruments were chosen to avoid the jazz/funk lean of horns and Rhodes.

## 9. One lead voice, nothing vocal behind it

Field reports showed gibberish vocal layers (Memphis-style chopped chants, mumbled backing voices) on nearly every take. Three things were inviting them:

1. Instrument lists that literally asked for them: "pitched-down chopped vocal samples", "vocal-chop riff", "vocal pads", "opera vocal sample", "choir-like synth", "beatbox-style vocal percussion", plus likeness tells like "pitched-up soul vocal chop loop".
2. "Chant" language everywhere: "chant-style delivery", "chant-ready" hooks, "2-word chant x4" post-hooks, and a "Chant loop" hook formula. Suno reads "chant" as a group.
3. Call-and-response hooks with "(ad-lib) answers", which Suno fills with a second voice.

The fix, enforced by test 15:

- Every vocal-flavored instrument became a synth, string or sampled-keys equivalent. Phonk keeps its identity through 808 distortion, lo-fi drum-machine crunch and cassette haze instead of chopped vocal samples; "Memphis" and "screwed" left the vocabulary.
- "Chant" is gone from lanes, likenesses, hook formulas and tags. The hook formula is now "Title loop", and the post-hook is a "2-word title repeat".
- Call & response became "Self-echo": the lead repeats its own last words. Hook tags say "single lead voice doubled", never "ad-lib answers".
- The style field always carries "one clean lead vocal over a fully instrumental backing" (with male/female/duet folded in).
- The Exclude field always carries `background vocals, vocal chops, vocal samples, chanting, gang vocals`.
- A lint blocks vocal-chop, vocal-sample, chant, choir, beatbox, gang/crowd/background/backing vocal, shout-back and call-and-response wording anywhere in a package.

## 10. Syncopation and twitch vocabulary

- Syncopation: off-beat kick placement, triplet hi-hat rolls switching between eighths and sixteenth-triplets, claps a sixteenth late, and flows that start on the "and" of 1.
- Twitch: stutter-edited vocal chops, glitch micro-edits, 808 retriggers on the off-beats, and half-bar beat mutes. A mute is a gap, not a fill.
- Flows in section tags: triplet, off-beat, stop-start, staccato, double-time bursts, melodic sing-rap. Verse 2 always switches flow to escalate.

## Sources

- [Suno v6 FAQ](https://help.suno.com/en/articles/13924481) and [What's new in v6](https://help.suno.com/en/articles/13924801)
- [Suno release notes](https://suno.com/release-notes)
- [Update for Suno v6: Variety, Max Mode, retired models (bitwize-music-studio #562)](https://github.com/bitwize-music-studio/claude-ai-music-skills/issues/562)
- [Suno v6 Guide: What Actually Changed](https://hookgenius.app/learn/suno-v6-guide/)
- [Suno V6 Keeps Changing Your Prompt? Variety Explained](https://undetectr.com/blog/suno-v6-changing-your-prompt)
- [Suno V6 Intro and Ending Prompts: Humming and Long Outros](https://undetectr.com/blog/suno-v6-intro-outro-prompts)
- [Suno V6 Guide (Moe Lueker)](https://moelueker.com/blog/suno-v6-guide)
- [Suno v6 review (eesel AI)](https://www.eesel.ai/blog/suno-v6-review)

- [Suno AI Prompt Guide 2026: What Changed Since v5.5](https://aiunfiltered.beehiiv.com/p/suno-ai-prompt-guide-2026)
- [Suno 5.5 Prompt Guide: The Technical Reference](https://roo.beehiiv.com/p/suno-5-5-prompt-guide-the-technical-reference-most-guides-skip)
- [Latest Suno Prompting Tips (June 2026)](https://hookgenius.app/learn/suno-prompting-june-2026/)
- [Suno Guide: Tags, Meta Tags & Prompts (V5.5)](https://blakecrosley.com/guides/suno)
- [Suno Exclude Styles & Negative Prompts: 2026 Guide](https://jackrighteous.com/en-us/blogs/guides-using-suno-ai-music-creation/negative-prompting-suno-v5-guide)
- [Suno Exclude Styles: Negative Prompts + 50 Examples](https://hookgenius.app/learn/suno-negative-prompting/)
- [Suno v5.5 Slider Settings](https://jackrighteous.com/en-us/blogs/guides-using-suno-ai-music-creation/creative-control-sliders-suno-v5)
- [Suno Hip-Hop Prompts: Beats, Rap Flows & R&B Hooks](https://jackrighteous.com/en-us/blogs/guides-using-suno-ai-music-creation/top-music-genres-2025-hip-hop-rnb-suno)
- [Trap Chord Progressions (Unison)](https://unison.audio/trap-chord-progressions/)
- [Trap Chord Progressions: A Complete Guide](https://gbswing.com/trap-chord-progressions-your-complete-guide/)
- [5 common hip hop chord progressions (Native Instruments)](https://blog.native-instruments.com/hip-hop-chord-progressions/)
- [The effect of repeated exposure on the development of an earworm (PMC)](https://www.ncbi.nlm.nih.gov/pmc/articles/PMC10585939/)
- [How To Write A Catchy Song: Anatomy Of An Earworm](https://www.udiscovermusic.com/in-depth-features/how-to-write-an-earworm/)
- [Creating Melodic Hooks That Stick](https://www.pointblankmusicschool.com/blog/creating-melodic-hooks-that-stick/)
