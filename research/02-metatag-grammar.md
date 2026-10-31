# Suno Metatag Grammar for Radio-Ready Hip Hop Instrumentals

Research memo for **suno-forge**. Compiled 2026-10-31. Scope: Suno v4.5+ / v5 / v5.5 and the current v6 family. All steering is text-only (Style field, Exclude Styles, Lyrics field, sliders); no audio uploads.

## 0. How to read this document

**Evidence labels**

| Label | Meaning |
|---|---|
| **VERIFIED** | Stated in official Suno material (help.suno.com, suno.com/blog, Suno team posts) as quoted or paraphrased by a source we could read. We could not load help.suno.com or suno.com directly from this session, so "VERIFIED" here means an official page was quoted verbatim by at least one secondary source that cites the URL; the URL is listed in Section 9 for the caller to confirm. |
| **COMMUNITY-TESTED** | The same behavior is reported by three or more independent community sources (guides, GitHub field guides, Reddit threads quoted in those guides), or one source reports a sample size. |
| **ANECDOTAL** | One or two sources, no sample size, or sources disagree. |

**Reliability ratings in the cheat sheet** (Section 8): *Reliable* = followed most of the time per multiple sources; *Mixed* = works often but sources disagree or report frequent misses; *Unreliable* = widely reported as ignored, sung, or inconsistent.

**Research limits.** This session's network allow-list blocked direct fetches of help.suno.com, suno.com, reddit.com, sunoaiwiki.com, jackrighteous.com, hookgenius.app, songsmith.studio, blakecrosley.com, heho.ai, medium.com and huggingface.co. GitHub (github.com and raw.githubusercontent.com) was reachable. Everything else came through search-engine summaries of those pages. Treat any claim attributed to a blocked host as "as summarized", and spot-check before building product copy on it.

**Model landscape as of 2026-10-31 (important).** Per the regiellis `suno-v6-controls.md` reference (rechecked against help.suno.com on 2026-09-24): Suno released **v6 on 2026-09-09 and retired v5.5, v5, v4.5 and v4 for new generations**; old songs remain playable, coverable and remasterable. Current models are `v6` (flagship, Pro/Premier), `v6-wild` (exploratory, Pro/Premier) and `v6-mini` (all plans). The bitwize `best-practices.md` cites The Verge (Sep 2026) and the official v6 FAQ for the same. **VERIFIED (via two independent secondary sources citing official pages).** Most of the community tag lore below was developed on v4.5-v5.5; the places where v6 is known to differ are called out explicitly.

---

## 1. Structural section tags in the Lyrics field

### 1.1 Grammar basics

- **Square brackets, own line.** A tag is text in `[ ]` on its own line, placed immediately before the material it governs. Every guide we read agrees; the Suno Wiki FAQ (older, unofficial) defines metatags as "keywords you insert within brackets [ ] in your lyrics." **COMMUNITY-TESTED.**
- **Case-insensitive.** `[Verse]`, `[VERSE]`, `[verse]` behave the same per blakecrosley, sunometatagcreator, learnstemlab and stokemctoke. No official statement. **COMMUNITY-TESTED (no official confirmation).**
- **Numbering.** `[Verse 1]`, `[Verse 2]` is recommended by nearly every source "for clarity"; `[Verse]` alone also works. Numbering is a convention, not a documented rule. **COMMUNITY-TESTED.**
- **Tag every section.** bitwize `100-expert-tips` #23: "Untagged blocks get treated as verses." bitwize `structure-tags.md`: structure tags "are reliable when every section carries one." **COMMUNITY-TESTED.**
- **Combining / modifying tags.** Four forms are in circulation:
  1. Adjective prefix: `[Short Instrumental Intro]`, `[Soft Verse]`, `[Building Chorus]` (bitwize, suno-field-guide: "modifiers attach as adjectives"). Most widely reported to work. **COMMUNITY-TESTED.**
  2. Dash suffix: `[Verse 1 - quiet intimate]`, `[Intro - Spoken]` (bitwize). Works as a short cue; the specific form `[Chorus - Instrumental]` is **not** tested by any source we found. **ANECDOTAL.**
  3. Colon parameters: `[Verse 1: whispered vocals, acoustic guitar only]` (sunometatagcreator; bitwize calls inline descriptors "an escalation technique, not a default"). **ANECDOTAL.**
  4. Pipe stacking: `[Chorus | anthemic | stacked harmonies]` (suno-field-guide; Fyzel wiki `[chorus | style: phonk hook, instruments: 808 bass]`). suno-field-guide claims stacked tags "override global style prompt for that specific section" and caps it at ~7 elements (Grenar trick #11). A bitwize launch-day v6 test reports `[Bridge | Female - Whispered ...]` cues were read. **ANECDOTAL-to-COMMUNITY (two guides plus one v6 test).**
  Two guides warn that mixing conventions (`[Chorus]`, `(Chorus)`, `CHORUS`) in one lyric sheet confuses the model. **ANECDOTAL.**
- **Keep cues short.** bitwize: "Keep the cue short - a word or two. Long cue lists cause noise." Several guides advise shortening `[emotional bridge with strings entering softly]` to `[Bridge]` and moving the description to the Style field. **COMMUNITY-TESTED.**
- **Blank lines.** suno-field-guide: Suno "reads blank lines as a pause or instrumental break and will hollow out the section." One v5.5 fix for verse-ending drift was literally "remove empty lines." Useful for instrumentals (a blank line after a tag = play, don't sing). **ANECDOTAL (one detailed source, consistent with the punctuation trick in 2.3).**
- **Tags are hints, not commands.** Every serious source says this. jackrighteous: there is no "official v6 meta-tag grammar that turns bracketed cues into deterministic commands." **COMMUNITY-TESTED.**

### 1.2 Which section tags are recognized

Consolidated from bitwize `structure-tags.md` (has an explicit reliability tier list), suno-field-guide, Humanitariansai cheat sheet, songsmith/usesuno cheat sheets, sunoai-music.ai, hookgenius and musci.io summaries:

| Tag | Status | Notes |
|---|---|---|
| `[Intro]` | Mixed | bitwize: "notoriously unreliable"; prefer `[Short Instrumental Intro]`. Long intros bury the hook; v5 "insists on weird intro vocals"; v5.5 had a stuttering-intro bug fixed by `[Short Instrumental Intro]` then `[Hook]`. |
| `[Short Instrumental Intro]` | Reliable | bitwize and suno-field-guide both recommend it as the replacement for `[Intro]`. |
| `[Verse]` / `[Verse 1]` | Reliable | bitwize top tier. |
| `[Pre-Chorus]` | Reliable | bitwize top tier. |
| `[Chorus]` | Reliable | "followed most reliably" (sunoai-music.ai); suno-field-guide notes the "system often overrides modifiers" attached to it. |
| `[Hook]` | Reliable | Hip hop synonym for chorus; bitwize lists `[Hook]`, `[Catchy Hook]`. |
| `[Post-Chorus]` | Mixed | bitwize "less reliable, test first." |
| `[Bridge]` | Mixed | bitwize "moderately reliable"; suno-field-guide "use once"; `[Instrumental Bridge]` "most effective" variant. |
| `[Break]` | Mixed | bitwize moderate; suno-field-guide: about one measure; bitwize `instrumental-tags.md`: "lead goes silent while accompaniment plays." Good for stop-start. |
| `[Breakdown]` | Mixed | bitwize `instrumental-tags.md`: "stripped-down section." Few reliability reports. |
| `[Drop]` / `[Bass Drop]` | Mixed | Cheat sheets pair it with EDM/hip hop; bitwize example `[Build-up]` then `[Bass Drop]`. Works best when the Style field is a drop-having genre. No reliability data. |
| `[Build]` / `[Build-up]` | Mixed | bitwize tip #75 says `[Build]` "still steers dynamics" in non-song pieces; Fyzel wiki lists `[build]` as added in v5.0. |
| `[Interlude]` / `[Melodic Interlude]` | Mixed-Reliable | suno-field-guide ranks `[Melodic Interlude]` among the most reliable instrumental tags; bitwize prefers the descriptive form over bare `[Interlude]`. |
| `[Instrumental]` | Reliable (as "no vocals here") | Fyzel wiki: "v4.5+ stricter adherence", "strictly honored"; james-palm claims ~9/10 vocal-free first takes on v5 vs ~4/10 for "no vocals" in Style (single creator). suno-field-guide caveat: it "may add a measure or two but won't carry a real solo section." |
| `[Instrumental Break]` | Reliable | bitwize tip #34: "`[Instrumental Break]` beats `[Solo]`," since `[Solo]` "often produces noodling." |
| `[Guitar Solo]` / `[Piano Solo]` / `[Sax Solo]` | Mixed | Soft hints; most reliable when the solo instrument is also named in Style and the section has no lyrics. One guide: genre words inside solo tags are "mostly ignored." |
| `[Outro]` | Mixed | bitwize moderate; suno-field-guide "use exactly once"; Fyzel: place before `[end]`. A bare `[Outro]` "leaves the shape open" (regiellis). |
| `[End]` | Reliable (vocal songs), Mixed (instrumentals) | bitwize top tier; suno-field-guide: "community testing puts it around 85-90% effective"; Fyzel: "Unreliable alone; combine with outro description. May not stop instrumentals." |
| `[Fade Out]` | Reliable | bitwize top tier; sunoaiwiki (2024) says "mixed results, may take a few attempts." |
| `[Fade In]` | Unreliable | bitwize "test first." |
| `[Big Finish]`, `[Fade to End]`, `[Coda]` | Anecdotal | Listed by bitwize/Fyzel; no reliability data. |
| `[Refrain]`, `[Vamp]`, `[Post-Chorus]` | Anecdotal | Appear in cheat sheets only. |
| Invented tags (`[My Special Section]`) | Unreliable | musci.io and songtoolbox: unknown tags are misread or sung. |

**Model differences.** v4.5 made `[instrumental]` stricter (Fyzel). v5 "insists on weird intro vocals" (suno-field-guide). v5.5 had reported mid-track degradation, verse-ending drift and a stuttering intro (suno-field-guide, citing the r/SunoAI v5.5 master thread). v6: bitwize reports a launch-day test where per-section pipe cues were read; Suno's redesigned lyrics editor offers a "Melodic instructions" option that inserts one bracketed production note per section, e.g. `[half-time drums]` (bitwize `structure-tags.md`). That last item is the closest thing to an official endorsement of production-cue brackets we found. **ANECDOTAL pending direct confirmation.**

### 1.3 Section tags when the Instrumental toggle is on

What the sources say, in order of weight:

1. **Official:** The toggle makes "a strictly voiceless track" (regiellis, paraphrasing the Custom Mode help page); Suno's own v5 tips (r/SunoAI u/Suno_helper post, cited by suno-field-guide) note that Cover/Inspo/Persona reduce length, but say nothing about tags under the toggle. **No official statement on whether Lyrics-field tags are honored in instrumental mode.**
2. **Community split.** sunoai-music.ai: "The Instrumental switch ignores the lyrics and the voice words entirely." blakecrosley and others: "Even in instrumental mode, add `[Instrumental]` or `[Instrumental Break]` metatags in the Lyrics field to reinforce the intent and control arrangement structure." bitwize `instrumental-tags.md` shows instrumental prompts using `[Intro]`, `[Build-up]`, `[Drop]`, `[Breakdown]`, `[Outro]` but "does not explicitly confirm that Suno follows these structure tags in instrumental mode." **ANECDOTAL / CONFLICTING.**
3. **Practical consensus for structured instrumentals** (songtoolbox, hookgenius instrumental guide, james-palm, bitwize): either (a) toggle **on** and accept a loosely structured result, or (b) toggle **off**, Lyrics field holds **only** bracket tags (and optionally punctuation lines, see 2.3), Style field says "instrumental", and Exclude Styles lists vocal terms. Option (b) is the one guides recommend when section order matters (intro, hook, verse, hook, break, hook, outro). **COMMUNITY-TESTED as a workflow; the exact degree of structure adherence is ANECDOTAL.**
4. **Caveat:** suno-field-guide: "Suno doesn't reliably invent instrumental filler to stretch runtime"; `[Instrumental]` "won't carry a real solo section." Expect short instrumental sections to come out shorter than written. Also Fyzel: `[end]` "may not stop instrumentals."

**Recommendation for suno-forge:** default to option (b) with a fallback button for option (a). Whether the UI greys out the Lyrics field when the toggle is on could not be confirmed from this session (regiellis only says "the Instrumental toggle coexists with Styles and the rest of the Advanced controls").

---

## 2. Performance and production cues

### 2.1 Which bracketed cues actually influence output

No source has run a controlled test of individual cues. The table below merges the reliability notes that exist (bitwize, suno-field-guide, Fyzel wiki, Humanitariansai list, cheat-sheet summaries) with the general rule that **cues naming a concrete, audible musical event in a familiar genre vocabulary work; cues naming a mix-engineering operation mostly do not.**

| Cue | Rating | Basis |
|---|---|---|
| `[Instrumental Break]`, `[Drum Break]`, `[Percussion Break]` | Reliable | bitwize: `[Percussion Break]` = "drums only"; tips-and-tricks uses `[Instrumental]` + "[drum break]" in Replace Section. |
| `[drums only]` / `[drums drop out]` | Mixed | Not found as a named tag; achieved via `[Break]`/`[Breakdown]` plus Style wording ("drums drop out before the hook"). ANECDOTAL. |
| `[Bass Drop]`, `[Drop]` | Mixed | Cheat sheets; bitwize `[Build-up]` -> `[Bass Drop]`. |
| `[808 slide]` | Unreliable as a tag | Not in any tag list; "808 slides" in Style field is standard trap vocabulary (hookgenius/undetectr prompts). Put in Style. |
| `[Vocal Chop]` | Mixed | Appears in sunoai-music/hookgenius lists; risk of triggering real vocals in an instrumental. Prefer "chopped vocal sample" in Style with vocal terms in Exclude. ANECDOTAL. |
| `[Filter Sweep]`, `[Tape Stop]`, `[Riser]`, `[Sidechain]`, `[Bitcrusher]` | Unreliable | Listed in Humanitariansai "Sound Effects and Processing" with no reliability claim; Fyzel marks `[mix]`, `[master]`, `[filter]`, `[panning]`, `[volume]` as "Ineffective." Effects/processing cues are the weakest class. |
| `[Half-time]`, `[Double-time]` | Mixed | bitwize mentions "double-time hats" as a Style-field chorus lift; Suno's own v6 "Melodic instructions" example is `[half-time drums]`. tagasong suggests `[Rhythm: Half-time]` on its own line. Rhythm-feel cues are in the "plausible" band. |
| `[Stripped]`, `[Stripped Back]`, `[Sparse]` | Mixed | Humanitariansai lists `[Stripped Back]`; dynamic-contrast cues generally reported to work when paired with a section tag (`[Breakdown - stripped, kick and 808 only]`). ANECDOTAL. |
| `[Full Band]` | Mixed | regiellis recommends Style-field "full-band energy throughout" instead. |
| `[Strings Swell]`, `[Crescendo]`, `[Building Intensity]` | Mixed | suno-field-guide lists "crescendo" among unreliable words; bitwize `[Build]` works for dynamics. Use `[Build]` not `[Crescendo]`. |
| `[Beat Switch]` | Mixed | Cheat sheets pair with `[Drop]` for hip hop transitions; no reliability data. Reinforce with Style ("beat switch in the second half: tempo drops, new drum pattern"). |
| `[Key Change]` | Unreliable | "Suno won't reliably obey a key-change tag" (tagasong); Fyzel `[key: E minor]` "affects overall tonality" only. |
| `[Tempo Change]` | Unreliable | Fyzel `[tempo]`: "No exact BPM; use descriptive phrases." |
| `[Silence]` | Mixed | bitwize tip #75 says `[Silence]` steers dynamics in ambient pieces; no hip hop reports. |
| `[Guitar Solo]`, `[Piano Solo]`, `[Sax Solo]` | Mixed | See 1.2. Name the instrument also in Style. |
| `[Scratching]`, `[Record Scratch]`, `[Vinyl Crackle]` | Mixed (Style) / Unreliable (tag) | "vinyl crackle", "dusty samples", "DJ scratches" are standard boom-bap Style vocabulary (hookgenius, james-palm, songsmith drum sheet) and are reported to work there. As Lyrics-field tags there is no evidence. |

Fyzel wiki's "Deprecated/ineffective" list is the only explicit negative list we found: `[sing-style]`, `[autotune]` (deprecated), `[loop]` (removed), `[mix]`, `[master]`, `[filter]`, `[panning]`, `[volume]` (ineffective), unlabeled `[theme]` (ignored). **ANECDOTAL (single curated source, dated 2025-12-02).**

### 2.2 Describing a section without words

Example target: `[Verse 1: muted plucks, kick and snare only, 8 bars]`.

- **Descriptive text inside brackets is treated as instruction, not lyrics**, provided it is inside the brackets and on its own line. All guides agree. **COMMUNITY-TESTED.**
- **Whether the description is honored** is a different question. bitwize: inline descriptors are "an escalation technique"; keep to "a word or two"; "mood, energy, and instrumentation belong in Style Box prose rather than bracket tags." Several guides say long descriptive tags are more likely to be partially ignored. **COMMUNITY-TESTED (direction), ANECDOTAL (degree).**
- **Bar counts.** bitwize: append a number, e.g. `[VERSE 1 8]`; "Results are approximate - Suno treats them as guidance, not strict limits." hookgenius: "(8-bar instrumental)" is read as a duration hint. No source reports exact bar counts being honored. **COMMUNITY-TESTED that it is only approximate.**
- **Recommended pattern** (synthesis of bitwize, regiellis, suno-field-guide): short tag + optional 2-4 word cue in the Lyrics field; the full instrument/arrangement description in the Style field (regiellis "Directed Brief": labeled lines Genre / Drums / Bass / Other / Arrangement / Production / Ending). Example:
  ```
  [Short Instrumental Intro]
  [Verse 1 - sparse, kick and snare only]
  [Hook - full beat, 808 enters]
  [Verse 2]
  [Break - drums drop out]
  [Hook]
  [Instrumental Outro]
  [End]
  ```
  with Style: "... arrangement: intro on muted Rhodes plucks, verse 1 kick and snare only, 808 enters on the hook, drums drop out for a 2-bar break before the last hook, short instrumental outro, hard stop."

### 2.3 Parentheses vs brackets; stopping Suno from singing tags

- **Parentheses are sung.** "(text) renders as backing vocals" - suno-field-guide rates this "Very High" reliability; bitwize tip #28 "Use parentheses for backing vocals"; sunoai-music.ai "parentheses are always sung, usually as a backing vocal." **COMMUNITY-TESTED.** For instrumentals this means: **never use parentheses in the Lyrics field.** (A minority of guides use parenthetical production notes like "(8 bars guitar solo)"; this is contradicted by the majority and should be avoided.)
- **Why bracketed text gets sung** (hookgenius, tagasong, paddystudio, sunomarket, songtoolbox): (1) tag not on its own line; (2) unrecognized / invented tag word; (3) long production notes like `[make this louder]`; (4) stray text under a tag outside brackets. Fixes: own line, standard tag names, short cues, nothing else in the box. **COMMUNITY-TESTED.**
- **Non-singable punctuation** (bitwize `instrumental-tags.md`; suno-field-guide "interlude pacing"): lines like `. .! .. .! !! ... ! ! !` between section tags "sometimes forces instrumental solos"; onomatopoeia "may be sung." **ANECDOTAL (two sources).** Use sparingly; the Exclude field is the stronger lever.
- **Instrumentals with words in Style:** "Suno will sing anything that looks singable." Avoid lyric-like phrases in Style ("say my name", "we rise") and avoid any vocal nouns ("vocal chops", "choir pad") unless the vocal terms are also in Exclude Styles. **COMMUNITY-TESTED.**

---

## 3. Ending control and length

### 3.1 Ending

- **Official position:** Suno documents no ending tag. jackrighteous: "Suno's documentation doesn't define [End], [Soft End], or [Dramatic End] as hard commands." The official remedies are editing tools: **Extend** (help.suno.com/en/articles/2409601, "make songs longer or just give it a new ending"), **Replace Section**, **Crop** (auto-applies a fade when cropping the end) and **Fade Out** in the Song Editor. **VERIFIED (Extend article quoted by search; Crop/Fade per jackrighteous summarizing the editor).**
- **`[End]` on its own line as the last line** is the single most consistent community recommendation (bitwize top tier; suno-field-guide "85-90% effective", fails "when the section right before it runs long or leaves a structure tag unclosed"). **COMMUNITY-TESTED.** Instrumental caveat (Fyzel): "May not stop instrumentals."
- **`[Outro]` + `[End]` or `[Outro]` + `[Fade Out]`** (bitwize tip #35): "Without this, Suno may ramble or cut off mid-phrase." **COMMUNITY-TESTED.**
- **Describe the ending in words** (jackrighteous, lyromusic): `[Outro]` plus "instruments fade out slowly" / "ends on one held chord"; "naming which instrument drops out first helps more than a generic 'fade out'." Put this in Style for instrumentals. **ANECDOTAL (two guides).**
- **Disposable landing section** (regiellis): place `[Instrumental Outro]` after the final hook, then `[End]`, so an early cut eats filler, not the hook; pair with Style text "complete the arrangement before a short instrumental outro and a hard stop." `[Hard Stop]` is community usage, not Suno syntax. **ANECDOTAL.**
- **"..." / ellipsis tricks.** The only related evidence is the punctuation-line trick (2.3) and Fyzel's note that `[end]` is "unreliable alone; combine with outro description." No source documents a trailing "..." as an ending signal. **UNVERIFIED.**
- **Reverb tail cut off:** suno-field-guide: add 1-2 s of silence or a fade in post. **ANECDOTAL.**
- **v6 note:** regiellis: "`[End]` and `[Fade Out]` request an ending, but exact timing needs auditioning or editing." Duration control (below) is now the primary length lever.

### 3.2 Hitting 2:30-3:30

- **Length is driven by structure, not by writing "3 minutes" in the prompt** (hookgenius, songsmith length formula). Lyric volume per section sets section length; instrumental sections add time without words. **COMMUNITY-TESTED.**
- **For instrumentals** the levers are: number of section tags, bar-count hints (approximate), and, on v5.5/v6, the **Duration** control. suno-field-guide warns `[Instrumental]` sections come out short, so overshoot section count slightly (7-9 tags for a 3-minute beat).
- **Duration control (v5.5 and v6):** note.com (v5.5) reports a slider from 0:10 to 6:00 in 5-second steps; bitwize v6 addendum "Advanced Mode offers Duration from Auto to a custom 10 s - 6:00"; regiellis: "a 0:10 to 6:00 Custom setting is a reported observation, not a verified limit"; all v6 variants "generate up to 8 minutes in one pass" (official v6 FAQ as cited). bitwize: on **Duration Auto, v6 outputs "often run slower, sparser and longer than the prompt implies"**, so set an explicit duration (e.g. 3:00) and state tempo and density. **COMMUNITY-TESTED for existence of the control; exact range ANECDOTAL.**
- **Max Mode (v6):** official FAQ (as cited by bitwize/regiellis) recommends it for songs over ~2 minutes and for consistency across a full track; costs 2x credits. Relevant for a 3-minute instrumental that must not degrade after 2:30 (bitwize reports mixes "go muffled after about 2:30 in standard mode"). **VERIFIED (existence/purpose) / ANECDOTAL (muffling).**
- **Extend:** keeps audio before the chosen point and generates after it; each extension ~1 minute and yields 2 clips; "Keep total extensions to 2-3 max per song to avoid quality degradation" (bitwize tips). Extend "from points of building momentum, not after natural endings" (suno-field-guide). For an instrumental tail, leave the extension Lyrics empty or Instrumental on, add Style note "slow instrumental fade" (hookgenius). Then **Get Whole Song**. v5.5 Extend was reported broken in the community master thread; v6 status unknown. **VERIFIED (mechanics) / COMMUNITY-TESTED (practice).**
- **Adherence decay:** suno-field-guide: the first 30-60 s track the prompt most closely. Front-load the hook (bitwize tip #24: chorus in the first 45 s).

---

## 4. Groove and anti-grid tricks

No source has measured swing in Suno output; everything here is practitioner lore. Grouped by lever.

### 4.1 Style-field vocabulary that is repeatedly reported to loosen the grid

**Core phrases (3+ sources):**
- "live drums" / "live drum kit" / "live-sounding drums" (songsmith, hookgenius hip-hop guide, HF knowledge base, bitwize "Live drums with room ambience")
- "behind the beat" / "slightly behind the beat on the snare" (songsmith humanization + drum cheat sheet, undetectr rock guide)
- "pocket groove" / "loose pocket" / "human pocket feel" (songsmith, undetectr)
- "swung hi-hats" / "swing" / "subtle swing" (songsmith drum sheet, openmusicprompt boom bap, travisnicholson lo-fi)
- "ghost notes on snare" (songsmith boom-bap line; sunodownloader breakbeat guide; regiellis drum examples)
- "brushed snare" / "brushed hi-hats" / "brushed kit" (songsmith, bitwize sound blueprint)
- "unquantized groove" / "played live not quantized" (travisnicholson, songsmith)
- "dusty drum break", "sampled drums", "chopped breaks", "raw sampled funk feel" (openmusicprompt, bitwize, sunodownloader)
- "boom bap" + "90s hip-hop" + "dusty samples" + "vinyl crackle" (hookgenius: "boom bap requires vintage production tags or Suno will modernize it")
- "J Dilla inspired" / "Dilla-style" (hookgenius instrumental prompt "j dilla inspired, boom bap, soul samples, punchy drums, 88 BPM"; openmusicprompt). Named-producer references are not blocked the way artist-vocal names are, but Suno rewrites some named artists automatically (bitwize tip #13). **ANECDOTAL.**

**Second-tier phrases (1-2 sources):**
- "subtle timing jitter", "human micro-rubato", "natural timing drift", "one-take performance", "recorded in one take with the band in the room together" (songsmith, suno-field-guide, undetectr)
- "linear drumming" instead of "complex drums" (songsmith: Suno "overcorrects to chaos" on "complex")
- "neo-soul drummer", "drums played softer on verse hi-hats with accents on the 2 and 4" (songsmith)
- "shuffle" (needed or blues/funk "come out rock-straight"), "triplet feel", "half-time feel" (hookgenius, tagasong)
- "MPC swing 58%" - **no source reports this working; percentages are DAW vocabulary with no evidence Suno maps them.** UNVERIFIED; the words "MPC", "SP-404", "sampler" as texture descriptors are common in prompts but untested.

**Modifiers that help:** "slight", "subtle" keep the imperfection from being overdone (songsmith). **Words reported to backfire:** "complex drums" (chaos), "aggressive"/"hard" (pulls to trap), "realistic", "crescendo", "ethereal", "lush", "shimmering" (suno-field-guide unreliable-word list).

### 4.2 Lyrics-field tags that help groove and dynamics

- `[Percussion Break]` / `[Drum Break]` (drums only, good for exposing a break-beat feel)
- `[Break]` (one measure of lead silence) and `[Breakdown]` (stripped) for stop-start arrangements
- `[Build]` -> `[Drop]` / `[Hook]` for a mute-then-slam entrance
- `[Verse - sparse, kick and snare only]` -> `[Hook - full beat]` for contrast (bitwize: "Contrast is what sells it")
- Suno v6 "Melodic instructions" style notes such as `[half-time drums]` (bitwize)
- Blank line or punctuation line after a tag to force instrumental play (2.3)
All **ANECDOTAL-to-COMMUNITY**; the stop-start pattern via `[Break]`/`[Breakdown]` is the best supported.

### 4.3 Slider settings for more human drums

- **Official:** Weirdness "Safe to Chaos, 50% is normal"; Style Influence "Loose to Strong"; "Tweak one slider at a time; don't jump above 50% immediately"; double-click resets (help.suno.com/en/articles/6141377, as quoted). **VERIFIED.**
- **Community hip hop ranges:** bitwize table: Hip-Hop/Rap Weirdness 20-40, Style Influence 50-70. bitwize tip #55: 70/20 (Style Influence/Weirdness) "is a common commercial setting"; tip #56: Weirdness above 60 causes "tempo shifts, odd instrumentation, and structure drift." suno-field-guide: Style Influence 70-90 for vague tags, 40-60 for very specific tags. **COMMUNITY-TESTED (direction), ANECDOTAL (numbers).**
- **No source gives a slider value that specifically humanizes drums.** The consistent view is that groove comes from vocabulary; sliders control adherence/chaos. A moderate Weirdness (35-50) is sometimes suggested to avoid the "most probable next event" (= quantized default) while keeping Style Influence high enough to hold the drum description. **ANECDOTAL.**
- **v6 Variety:** rewrites the Style prompt above Off; Suno FAQ: "if you'd like to retain full control of your style tags, reduce the Variety slider to 0." For engineered drum descriptions set Variety Off. **VERIFIED (FAQ quote via two sources).**

### 4.4 Post-generation
Stem-split and transient-shape kick/snare subtly (suno-field-guide); high-pass everything except kick/808 (bitwize #82). Outside suno-forge's text-only scope but worth a tooltip.

---

## 5. Negative prompting

- **Exclude Styles field.** Official: Custom Mode -> Advanced Options -> "Exclude" where you "enter instruments or other elements you don't want in the track" (help.suno.com/en/articles/3161921, as quoted). Added September 2024 (gcui-art/suno-api issue #158). **VERIFIED.**
- **It is a bias, not a ban.** regiellis: "Exclude is a request, not a guaranteed ban." bitwize: "exclusions shift the odds and aren't a hard filter." **COMMUNITY-TESTED.**
- **Write bare nouns, not negations:** "write `drums`, not `no drums`" (bitwize); suno-field-guide: inline fallback should be "no X", never "don't". **COMMUNITY-TESTED.**
- **Keep it short:** 2-4 items (bitwize, suno-field-guide "2-3 maximum", hookgenius "2-3"); "over-specifying dilutes the effect." A ~1,000-char limit is measured, not published (regiellis). **COMMUNITY-TESTED.**
- **Negatives inside the Style field:**
  - v4.5-v5.5: reported "unreliable" and "less consistently respected" than the Exclude field (acetaggen, hookgenius, songsmith). One outlier (roo.beehiiv) claims "no X" in Style works better on v5.5. **CONFLICTING.**
  - **v6: ignored.** bitwize cites a launch-week test (The Verge, Sep 2026) in which "no drums" and "monotone vocals" typed into the prompt were ignored; bitwize 100-tips: "Inline negatives in the Style Box are ignored on v6." **COMMUNITY-TESTED (two sources, one press test).**
  - **Does "no vocals" backfire?** The specific claim that the word "vocals" makes Suno add vocals is widespread in forum lore but **no source we could read demonstrates it**. What is documented: a lone "no vocals" in Style is "often not enough", producing "ooh" sustains or choir pads (hookgenius). The safe design is to not rely on Style-field negatives at all: say "instrumental" positively, keep vocal nouns out of Style, and list them in Exclude. **ANECDOTAL (backfire) / COMMUNITY-TESTED (insufficient alone).**
- **Suggested Exclude list for hip hop instrumentals** (union of hookgenius, suno-field-guide intro-hum list, bitwize group-vocal list; trim to 3-4 per generation):
  - Core: `vocals, singing, humming, choir`
  - Intro-hum variant (v5-era): `wordless vocals, spoken intro, ad-libs`
  - Genre guard: `EDM drops, pop, acoustic guitar` (only if drift is observed)
  - Avoid listing things that contradict Style (e.g. excluding "808" while Style asks for 808s).

---

## 6. Style field writing conventions

- **Field name and size.** Suno help now calls it the "Style" field (undetectr). Measured limits: ~1,000 chars for Style and Exclude on v4.5+ (was ~120-200 on v3-v4), ~5,000 for Lyrics; "Suno has never published a limit for any field" (regiellis). **COMMUNITY-TESTED.**
- **Comma tags vs prose.** Sources split. Majority (bitwize templates, hookgenius, musci) use comma-separated descriptors; bitwize tip #20: commas separate descriptors, periods separate blocks; semicolons/slashes/quotes "parse inconsistently." suno-field-guide: "use periods, not commas, for critical requirements" and hover the style panel to see what Suno underlines as one unit. regiellis: "Style holds prose, not markup." bitwize `instrumental-tags.md`: "Narrative, producer-style descriptions are recommended over flat tag lists." Official v5 tip (Suno team Reddit post, via suno-field-guide): "Begin with 1-2 genre tags"; "Prompts that worked in earlier models may not translate." **Synthesis:** short comma phrases grouped into 3-4 sentence-like blocks separated by periods; 1-2 genres first. **COMMUNITY-TESTED (direction).**
- **Ideal length.** "~10 descriptors works well" and "synonym piles dilute" (bitwize); 5-8 tags with high Style Influence (jackrighteous); 8-15 for detailed control, ~20 max (suno-field-guide); regiellis treats ~200 chars as a compact default. v5 "performs worse with dense prompts" (dev.to, moelueker). **COMMUNITY-TESTED: 100-400 chars, 8-15 descriptors, no repeats.**
- **Ordering.** "Suno weights early words more heavily" (bitwize #11; HF knowledge base; suno-field-guide "first genre has most influence"). Recommended order for instrumentals: genre/subgenre + era -> drums/groove -> bass -> lead instruments/samples -> mood -> arrangement notes -> mix/master terms -> BPM/key at the end (bitwize puts BPM last; suno-field-guide puts BPM/key first - both say be consistent across a project). **COMMUNITY-TESTED.**
- **BPM.** Fyzel: `[bpm]` "suggestive, not precise"; heho.ai measured a DAW tempo "nowhere near the prompt" and quotes a Reddit commenter who had "not seen any evidence that Suno can apply BPM instructions"; bitwize #15 says "128 BPM is unambiguous" and better than "fast"; mixmasterai claims it is followed. **CONFLICTING -> treat BPM as a strong style hint that biases tempo range, not a setting. Always pair with a feel word ("88 BPM, laid-back head-nod tempo"). Never put BPM in Lyrics (it gets sung).**
- **Key.** heho.ai: key "even less reliable" than BPM; Fyzel `[key: E minor]` "affects overall tonality." **ANECDOTAL -> key words bias mood ("minor", "dark") more than pitch.**
- **Era terms.** "90s hip-hop", "golden era", "early-2000s New York" reliably pull vintage production; hookgenius: without vintage tags "Suno will modernize it." Emotion words outperform technical ones ("desperate" beat "minor key with reverb" in a 200+ track Reddit test cited by suno-field-guide). **COMMUNITY-TESTED.**
- **Mix/master terms.** "punchy drums", "wide stereo", "crisp transients", "warm", "tape saturation", "analog glue compression" are widely used and reported to help **when attached to an element** ("punchy kick", "wide hook") rather than as bare compliments (undetectr V6 analysis). "radio-ready", "masterpiece", "high quality" measured as changing nothing on an earlier model (lyromusic); undetectr notes Suno's own prompt enhancer already adds mix words. regiellis fault-to-clause map: muddy -> "open defined low-mids, clear high-end air, crisp transients." **COMMUNITY-TESTED (element-attached terms) / ANECDOTAL (bare "radio-ready").** Output is pre-mastering (~ -14 LUFS target is a post step).
- **v6 specifics.** Variety Off for an engineered Style field (official FAQ quote); Max Mode for >2:00; "re-prompt, don't rerun" old v5 prompts (bitwize); v6-wild for exploration then Cover on v6.

---

## 7. Known failure modes for instrumentals and fixes

| Failure | Likely cause | Fix (source) | Evidence |
|---|---|---|---|
| Unwanted vocals / humming / "ooh" pads | Vocal nouns in Style; empty intro invites "improvised vocal noise"; v5 "intro hums" | Instrumental toggle **and** `[Instrumental]`/`[Short Instrumental Intro]` first line; "instrumental" early in Style (suno.ing: at the end it still leaked); Exclude `vocals, singing, humming, choir`; v5.5: `[Instrumental]` as the *final* tag also reported (roo.beehiiv, conflicting); regenerate (same prompt often cleans up) | COMMUNITY-TESTED |
| Abrupt genre switch / pop gravity mid-song | Low Style Influence, high Weirdness, Variety on (v6), dense contradictory Style | Raise Style Influence to 70-90, Weirdness <= 40, Variety Off, "reassert influences mid-prompt", 2-3 genres max, "house sound" suffix (bitwize); v5.5 degradation workaround: generate on v5 then Cover on v5.5 (now moot; use Max Mode on v6) | COMMUNITY-TESTED |
| Overly long intro / hook arrives late | `[Intro]` unreliable; Suno defaults to long instrumental lead-in | `[Short Instrumental Intro]`, hook within 45 s, declare intro instrumentation in Style, `[START_ON: TRUE]` (suno-field-guide, opinion) | COMMUNITY-TESTED (tag) / ANECDOTAL (START_ON) |
| No clear hook / drop | Flat dynamics, no contrast tags | `[Build]` -> `[Hook]`/`[Drop]`; `[Verse - sparse]` vs `[Hook - full]`; Style "drums drop out before each hook, 808 enters on the hook" | ANECDOTAL-COMMUNITY |
| Muddy low end | Kick vs 808 competing; Suno stems have low-end phase issues (roexaudio) | Style: "tight short 808, clean sub, punchy kick, open low-mids, crisp transients"; avoid "boomy sustained sub"; post: high-pass non-bass, mono sub, cut 200-350 Hz 2-4 dB (suno-field-guide) | ANECDOTAL (prompt) / COMMUNITY-TESTED (post) |
| Stops too early (cuts mid-phrase) | Too many sections for the length; `[End]` after a long section | Fewer sections, hook earlier, disposable `[Instrumental Outro]` before `[End]`, set Duration explicitly (v6), Extend from a momentum point | COMMUNITY-TESTED |
| Runs too long / won't end | No ending tag; Duration Auto on v6 runs long | `[Outro]` + `[End]` or `[Fade Out]` last line; describe ending in Style ("ends on one held chord"); Crop + Fade in editor | COMMUNITY-TESTED |
| 4-bar loop with no development | Loop-implying genre words ("lo-fi beat", "ambient"); no section tags; bar-count hints too literal | Explicit structure tags with contrast; Style "progressive development, builds in intensity, new element every 8 bars"; add a bridge/breakdown with a different texture; Replace Section for one bad stretch (hookgenius, jackrighteous, mixmasterai) | COMMUNITY-TESTED |
| Mix goes dull after ~2:30 | v6 standard mode | Max Mode (bitwize) | ANECDOTAL |
| Bracket text sung | Tag on same line as text, unknown tag, long note | Own line, standard names, 1-4 word cues, nothing unbracketed in the box | COMMUNITY-TESTED |

---

## 8. Cheat sheet: tags and cues with reliability rating

| Tag / cue (Lyrics field unless noted) | Rating | Source basis |
|---|---|---|
| `[Verse]`, `[Verse 1]` | Reliable | bitwize tier 1; all guides |
| `[Pre-Chorus]` | Reliable | bitwize tier 1 |
| `[Chorus]` / `[Hook]` | Reliable | bitwize; sunoai-music "most reliably followed" |
| `[End]` (last line) | Reliable (vocal) / Mixed (instrumental) | suno-field-guide 85-90%; bitwize; Fyzel caveat |
| `[Fade Out]` | Reliable | bitwize tier 1; sunoaiwiki "mixed, retry" |
| `[Instrumental]` | Reliable for "no vocals here" | Fyzel v4.5+ strict; james-palm 9/10 (single creator) |
| `[Instrumental Break]` | Reliable | bitwize #34 |
| `[Short Instrumental Intro]` | Reliable | bitwize; suno-field-guide v5.5 fix |
| `[Percussion Break]` / `[Drum Break]` | Reliable-Mixed | bitwize instrumental-tags |
| `[Melodic Interlude]`, `[Instrumental Bridge]` | Mixed-Reliable | suno-field-guide "most reliable instrumental tags" |
| `[Bridge]` | Mixed | bitwize moderate; use once |
| `[Break]` | Mixed | bitwize; ~1 measure |
| `[Breakdown]` | Mixed | bitwize |
| `[Build]` / `[Build-up]` | Mixed | bitwize #75; Fyzel v5 addition |
| `[Drop]` / `[Bass Drop]` | Mixed | cheat sheets; works best in drop genres |
| `[Beat Switch]` | Mixed | cheat sheets only; reinforce in Style |
| `[Outro]` | Mixed | bitwize moderate; "leaves shape open" alone |
| `[Intro]` | Mixed-Unreliable | bitwize "notoriously unreliable" |
| `[Post-Chorus]`, `[Fade In]` | Unreliable | bitwize "test first" |
| `[Guitar/Piano/Sax Solo]` | Mixed | soft hint; also name in Style |
| `[Solo]` | Unreliable | bitwize: "noodling" |
| `[Half-time]` / `[half-time drums]` | Mixed | Suno v6 "Melodic instructions" example; tagasong |
| `[Double-time]` | Mixed | bitwize Style usage only |
| `[Stripped]`, `[Sparse]` | Mixed | Humanitariansai list; works best as section modifier |
| `[Strings Swell]`, `[Crescendo]` | Mixed-Unreliable | "crescendo" on unreliable-word list |
| `[Key Change]` | Unreliable | tagasong; Fyzel |
| `[Tempo Change]` | Unreliable | Fyzel |
| `[Silence]` | Mixed | bitwize #75 (ambient only) |
| `[Filter Sweep]`, `[Tape Stop]`, `[Riser]`, `[Sidechain]` | Unreliable | Humanitariansai list only; Fyzel marks mix ops ineffective |
| `[808 slide]`, `[Vocal Chop]` | Unreliable as tags | not in tag lists; use Style field |
| `[Scratching]`, `[Record Scratch]`, `[Vinyl Crackle]` | Unreliable as tags / Reliable as Style words | boom-bap prompt guides |
| `[START_ON: TRUE]` | Unreliable | suno-field-guide opinion only |
| `[Hard Stop]` | Unreliable | regiellis: community usage, not syntax |
| `(parentheses)` | Reliable - but SUNG as backing vocals | suno-field-guide "Very High"; bitwize #28 |
| Blank line / punctuation line after tag | Mixed | suno-field-guide; bitwize instrumental-tags |
| Bar counts `[Verse 1 8]`, "(8 bars)" | Mixed (approximate only) | bitwize; hookgenius |
| `[mix]`, `[master]`, `[filter]`, `[panning]`, `[volume]`, `[loop]`, `[autotune]` | Unreliable | Fyzel deprecated/ineffective list |
| Style field: "live drums", "behind the beat", "swung hats", "ghost notes", "brushed snare", "boom bap", "dusty drum break" | Reliable-Mixed (3+ sources) | songsmith, hookgenius, bitwize, openmusicprompt |
| Style field: "unquantized", "loose pocket", "timing jitter", "neo-soul drummer", "shuffle" | Mixed (1-2 sources) | songsmith, travisnicholson, undetectr |
| Style field: "MPC swing 58%" | Unverified | no source |
| Style field: "92 BPM" | Mixed (bias, not setting) | heho.ai measured miss; bitwize/mixmasterai say use it |
| Style field: "F# minor" | Unreliable for pitch; Mixed for mood | heho.ai; Fyzel |
| Style field: "no vocals" | Unreliable alone; ignored on v6 | hookgenius; bitwize/The Verge |
| Exclude Styles: `vocals, singing, humming, choir` | Reliable (bias) | official field; all guides |

---

## 9. Sources

Access date for all: 2026-10-31. "(summary)" = content obtained through search-engine summaries because the host was blocked in this session; "(fetched)" = read directly.

**Official Suno (quoted by secondary sources; URLs for confirmation)**
- How do I exclude elements of a song? - https://help.suno.com/en/articles/3161921 (summary; feature dates to Sep 2024)
- How to Use: Creative Sliders - https://help.suno.com/en/articles/6141377 (summary; ~515 days old per search snippet, so ~mid-2025)
- How do I make my song longer? (Extend) - https://help.suno.com/en/articles/2409601 (summary)
- Custom Mode guide - https://help.suno.com/en/articles/3197377 (cited by regiellis, checked 2026-09-13)
- What's New in v5.5 - https://help.suno.com/en/articles/11362305 ; Voices 11362369 ; Custom Models 11362497 (cited by suno-field-guide)
- v6 FAQ - https://help.suno.com/en/articles/13924481 ; Current Models: v6 - 13924737 ; What's new in v6 - 13924801 ; How do I change models - 13924993 (cited by bitwize and regiellis, rechecked 2026-09-24)
- Introducing v6 - https://suno.com/blog/introducing-v6 and https://suno.com/release-notes/introducing-v6 (v6 released 2026-09-09)
- Suno v5.5 announcement - https://suno.com/blog/v5-5 (2026-03-26)
- Suno team, "Tips & Tricks for Using the New Model" (v5), u/Suno_helper - https://www.reddit.com/r/SunoAI/comments/1nudoue/ (cited by suno-field-guide; not readable here)
- The Verge on v6 launch (inline negatives ignored) - https://www.theverge.com/ai-artificial-intelligence/991977/suno-releases-its-first-ai-music-model-made-with-record-industry-help (Sep 2026, cited by bitwize)

**Community references read directly (GitHub)**
- mttkllr, Suno Field Guide - https://github.com/mttkllr/suno-field-guide (fetched; undated, latest item v5.5 2026-03-26; labels claims Official/Field-tested/Community/Opinion; cites r/SunoAI threads 1qk766q, 1nudoue, 1rdu5e5, 1nop8q6, 1in79lh, 1qrf21y, 1of8wt9, 1tooamm, 1sfqdlj, 1u1vxz8, 1u7asio)
- bitwize-music-studio, claude-ai-music-skills /reference/suno/ : structure-tags.md, instrumental-tags.md, best-practices.md, tips-and-tricks.md, creative-sliders.md, models.md, 100-expert-tips.md - https://github.com/bitwize-music-studio/claude-ai-music-skills/tree/main/reference/suno (fetched; v6-era, undated)
- Fyzel, claude-skills wiki "suno-songwriter" - https://github.com/Fyzel/claude-skills/wiki/suno-songwriter (fetched; tag reference dated 2025-12-02, page edited 2026-05-24)
- regiellis, suno-songwriter-agent-skill: README, references/suno-v6-controls.md, references/v6-production-direction.md - https://github.com/regiellis/suno-songwriter-agent-skill (fetched; official pages rechecked 2026-09-24)
- Humanitariansai, Lyrical-Literacy Suno cheat sheet - https://github.com/Humanitariansai/Lyrical-Literacy/blob/main/Suno/suno-cheatsheet.md (fetched; undated, no reliability claims)
- gcui-art/suno-api issue #158 (Exclude field added Sep 2024) - https://github.com/gcui-art/suno-api/issues/158

**Community guides (summary only)**
- blakecrosley.com, Suno Guide: Tags, Meta Tags & Prompts (V5.5) - https://blakecrosley.com/guides/suno
- jackrighteous.com: [End] prompt guide - https://jackrighteous.com/en-us/blogs/guides-using-suno-ai-music-creation/suno-ai-end-prompt-guide ; Fix songs that repeat or won't end - https://jackrighteous.com/blogs/guides-using-suno-ai-music-creation/fix-suno-song-repeats-wont-end ; Crop and fade - https://jackrighteous.com/en-us/blogs/guides-using-suno-ai-music-creation/crop-fade-out-suno-song ; Slider recipes - https://jackrighteous.com/en-us/blogs/guides-using-suno-ai-music-creation/creative-control-sliders-suno-v5 ; Duration slider - https://jackrighteous.com/en-us/blogs/guides-using-suno-ai-music-creation/suno-duration-slider-song-length-guide
- hookgenius.app: negative prompting - https://hookgenius.app/learn/suno-negative-prompting/ ; instrumental music - https://hookgenius.app/learn/suno-instrumental-music/ ; instrumental prompts - https://hookgenius.app/learn/suno-instrumental-prompts/ ; hip-hop prompts - https://hookgenius.app/learn/suno-hip-hop-prompts/ ; lyrics formatting - https://hookgenius.app/learn/suno-lyrics-formatting/ ; fix repetition - https://hookgenius.app/learn/fix-suno-repetition/ ; fix too short - https://hookgenius.app/learn/fix-suno-too-short/ ; character limits - https://hookgenius.app/learn/suno-character-limits/ (pages marked updated May 2026)
- songsmith.studio: humanization controls - https://songsmith.studio/blog/suno-humanization-controls ; drum pattern cheat sheet - https://songsmith.studio/blog/suno-drum-pattern-cheat-sheet ; negative prompts - https://songsmith.studio/blog/suno-negative-prompts-guide ; song length formula - https://songsmith.studio/blog/suno-song-length-formula ; structure tags - https://songsmith.studio/blog/suno-structure-tags-cheat-sheet
- james-palm.medium.com: instrumental tag - https://james-palm.medium.com/suno-ai-instrumental-tag-stop-vocals-eb4488b082ad ; metatags guide - https://james-palm.medium.com/stop-wasting-your-credits-the-ultimate-guide-to-suno-ai-metatags-verse-chorus-and-drop-57e209a0e5d8 ; hip-hop prompts - https://james-palm.medium.com/the-complete-list-of-suno-ai-prompts-for-hip-hop-and-rap-trap-boom-bap-drill-3af109dafd21
- heho.ai, Does Suno follow the BPM you ask for? - https://heho.ai/blog/does-suno-follow-bpm
- sunoaiwiki.com, How to End a Song Naturally (2024-05-09) - https://sunoaiwiki.com/tips/2024-05-09-how-to-end-a-song-naturally/
- Suno Wiki FAQ (Scribd mirror, older) - https://www.scribd.com/document/859627879/suno-wiki
- sunoai-music.ai tags list - https://sunoai-music.ai/blog/suno-tags ; musci.io tags list - https://musci.io/blog/suno-tags ; sunometatagcreator.com guide - https://sunometatagcreator.com/metatags-guide ; learnstemlab - https://learnstemlab.com/suno-ai-song-control-metatags-guide ; stokemctoke - https://stokemctoke.com/the-complete-suno-ai-meta-tags-guide/ ; songtoolbox - https://songtoolbox.tools/blog/suno-prompt-guide ; tagasong brackets - https://tagasong.com/suno-lyrics-tags-square-brackets-prompts/ ; tagasong tempo - https://tagasong.com/ai-music-tempo/ ; paddystudio - https://paddystudio.com/blog/suno-song-structure-and-vocal-tags/ ; sunomarket - https://sunomarket.com/blog/suno-lyric-metatags-that-work ; usesuno.online structure tags - https://usesuno.online/tools/structure-tags
- acetaggen advanced parameters - https://acetaggen.com/blog/weirdness-exclude-styles-reference-suno-advanced-parameters ; roo.beehiiv v5.5 prompting - https://roo.beehiiv.com/p/suno-prompting ; apipass v5/v5.5 hacks - https://apipass.dev/blogs/12-suno-v5-v5-5-hacks ; suno.ing instrumental guide - https://suno.ing/blog/suno-instrumental-music-guide/ ; suno.hk instrumental guide - https://suno.hk/blog/suno-ai-instrumental-backing-music-guide/ ; mixmasterai too-repetitive - https://www.mixmasterai.co/suno-prompts/fix/too-repetitive ; lyromusic quality words - https://lyromusic.com/prompts/better-sound-quality ; undetectr V6 mastering prompts - https://undetectr.com/blog/suno-v6-mastering-prompts ; undetectr custom mode - https://undetectr.com/blog/suno-ai-custom-mode-guide ; openmusicprompt boom bap - https://openmusicprompt.com/suno/hip-hop/boom-bap ; sunodownloader breakbeat - https://sunodownloader.ai/blog/ai-breakbeat-generator ; travisnicholson lo-fi prompts - https://travisnicholson.medium.com/50-suno-ai-prompts-for-lo-fi-jazz-and-chill-beats-766cfe8995a7 ; note.com v5.5 duration control - https://note.com/dreammii/n/n6e7cf9fc2ace ; dev.to v5 vs v4.5 - https://dev.to/shaizadok/suno-v5-vs-v45-the-complete-comparison-2c49 ; moelueker v4.5 vs v5 - https://moelueker.com/blog/suno-v5-vs-v4-5-what-actually-changed-and-when-to-use-each ; roexaudio stems - https://www.roexaudio.com/blog/suno-stems-sound-bad-here-is-why-and-how-to-fix-them ; develephant song tags (v3-era) - https://develephant.github.io/suno-songtags/song-tags/end ; AnimalMonk HF knowledge base - https://huggingface.co/spaces/AnimalMonk/Suno-Prompt-Generator/blob/main/knowledge_base.py

---

## 10. Could not verify (follow-ups for a session with wider network access)

1. The text of any help.suno.com article, including whether Suno officially documents *any* metatag; and whether the Instrumental toggle hides/disables the Lyrics field in the current v6 UI.
2. Whether structure tags are honored at all when the Instrumental toggle is on (sources conflict; no test).
3. `[Chorus - Instrumental]` hyphen form specifically; `[START_ON: TRUE]`; `[Hard Stop]`; trailing "..." as an ending signal.
4. Any measured swing/humanization effect of Style vocabulary; "MPC swing 58%" style percentages.
5. Whether "no vocals" in the Style field actively *adds* vocals (backfire) versus merely failing to remove them.
6. BPM/key adherence on v6 (only one DAW-measured report, model unspecified).
7. Exact Duration control range on v6 (0:10-6:00 reported; 8:00 max per official FAQ as cited).
8. The r/SunoAI threads cited by the field guides (1nudoue official v5 tips, 1u1vxz8 intro-hum exclusion list, 1sfqdlj v5.5 master thread) could not be opened.
