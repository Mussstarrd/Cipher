# Suno metatag cheat sheet (Lyrics field, instrumental use)

Built from `research/02-metatag-grammar.md` (community-tested on v4.5 through v6; Suno documents almost none of this). Ratings: **Reliable** (3+ independent sources, consistent), **Mixed** (works often, not always), **Unreliable** (mostly ignored). Everything in the Lyrics field is a hint, not a command; the Style field and sliders decide most of the sound.

## Grammar

- One tag per line, square brackets, case-insensitive. `[Hook 2 - 16 bars: distorted 808 slams in, cowbell rides the off-beats]` works: the tag name first, then a dash or colon, then plain descriptive cues.
- **Never use parentheses.** Parenthesized text is reliably sung as backing vocals.
- No bare text lines. Anything outside brackets may be sung.
- Bar counts are approximate hints only; Suno does not count bars. We keep them because they time the plan and they do bias section length.
- Start the field with `[Instrumental]` (reliable "no vocals here" marker on v4.5+) and end with `[End]`.
- Mix-operation words work as **descriptions inside a section tag** ("tape stop into the hook") but not as standalone tags (`[Tape Stop]` alone is unreliable).

## Section tags

| Tag | Rating | Notes |
|---|---|---|
| `[Instrumental]` | Reliable | Leading line; marks the track as no-vocals |
| `[Short Instrumental Intro]` | Reliable | Use instead of bare `[Intro]`, which is "notoriously unreliable" |
| `[Verse]`, `[Verse 1]` | Reliable | 16 written bars standard |
| `[Pre-Chorus]` | Reliable | Riser / thinning section before a hook |
| `[Hook]` / `[Chorus]` | Reliable | The most consistently followed tag |
| `[Instrumental Break]` | Reliable | Beats `[Solo]` |
| `[Drum Break]` / `[Percussion Break]` | Reliable-Mixed | Drums only; exposes the groove |
| `[Instrumental Bridge]`, `[Melodic Interlude]` | Mixed-Reliable | Better than bare `[Bridge]` |
| `[Bridge]` | Mixed | Use once |
| `[Break]` | Mixed | Often about one measure of drop-out |
| `[Breakdown]`, `[Build]` / `[Build-up]` | Mixed | Build -> Drop works best in drop-centric genres |
| `[Drop]` / `[Bass Drop]` | Mixed | Reinforce with "hard drop" cue text |
| `[Beat Switch]` | Mixed | Reinforce in the Style field ("beat switch halfway") |
| `[Outro]` | Mixed | Always follow with `[End]` |
| `[End]` | Reliable (vocal songs) / Mixed (instrumentals) | ~85-90% community figure; set Duration so a hard cut backs it up |
| `[Fade Out]` | Reliable | Fades have nearly vanished from hits; prefer a cold stop |
| `[Intro]`, `[Post-Chorus]`, `[Fade In]` | Unreliable | Avoid |
| `[Solo]` | Unreliable | "Noodling"; name the instrument in Style instead |
| `[Half-time]`, `[Double-time]` | Mixed | Better as cue text inside a section |
| `[Key Change]`, `[Tempo Change]`, `[Silence]`, `[Hard Stop]`, `[START_ON: TRUE]` | Unreliable | Describe the effect inside a section instead ("everything stops on the downbeat") |
| `[Filter Sweep]`, `[Tape Stop]`, `[Riser]`, `[Sidechain]`, `[808 slide]`, `[Vocal Chop]`, `[Scratching]`, `[mix]`, `[master]` | Unreliable as tags | Fine as words inside a section cue or in the Style field |

## Writing a section without words

```
[Verse 1 - 16 bars: drums simplify to kick and snare, Rhodes voicings open up, bass plays sparse, ghost notes stay]
[Drum Break - 4 bars: drums only, loose fills, kick pushes behind the beat]
[Hook 3 - 8 bars: biggest groove, horn section joins, fills every 4 bars]
```

Describe what plays, what drops out, and one timing-feel cue. Contrast sells the arrangement: a sparse verse into a full hook is the best-supported trick for dynamics.

## Ending and length control

- Last two lines: `[Outro - 4 bars: ...]` then `[End]`.
- Set Duration (Custom) to planned runtime + ~10 s; Suno hard-cuts there if `[End]` is ignored. Never Auto on v6 (runs long).
- Too short: Extend from the last good section and paste only the remaining tags.
- Too long: Crop to the last downbeat.

## Groove and anti-grid tricks

No source has measured swing in Suno output; this is practitioner consensus (3+ sources unless noted). Groove comes from **vocabulary**, not sliders.

**Style field phrases that loosen the grid:** live drums / live drum kit; behind the beat / slightly late snare; pocket groove / loose pocket; swung hi-hats / subtle swing; ghost notes on the snare; brushed snare; unquantized groove / played live not quantized; dusty sampled drum break; boom bap + 90s hip hop + vinyl crackle (vintage tags stop Suno modernizing boom bap). Second tier (1-2 sources): subtle timing jitter, human micro-rubato, one-take feel, neo-soul drummer, shuffle, triplet feel, half-time feel, linear drumming.

**Modifiers:** "slight" and "subtle" keep imperfection from tipping into chaos.

**Words reported to backfire:** "complex drums" (chaos), "aggressive" / "hard" (pulls to generic trap), "realistic", "crescendo", "ethereal", "lush", "shimmering". **"MPC swing 58%"**-style percentages: no evidence Suno maps them. **Producer names** ("Dilla-style"): banned here regardless; Suno rewrites some names.

**Lyrics field moves:** `[Drum Break]` to expose the groove; `[Break]`/`[Breakdown]` for stop-start; `[Build]` -> `[Drop]` for a mute-then-slam; sparse-verse / full-hook contrast; "full stop on the last beat of every 4th bar" cue text for mutes.

**Sliders:** hip hop Weirdness 20-40 (commercial) to 55 (experimental); above 60 tempo and structure drift. Style Influence 50-70 for engineered prompts. Variety **Off** on v6. No slider humanizes drums by itself.

## Known instrumental failure modes

| Symptom | Likely cause | Fix |
|---|---|---|
| Humming / "ooh" pad / ad-libs | Vocal-implying Style words; Variety above Off; long empty intro | Strip the words, Variety Off, Exclude `vocals, humming, ad-libs, vocal chops`, Replace Section on the leak |
| Prompt ignored entirely | Variety at Normal rewrote the Style text | Variety Off, re-roll |
| Genre switch mid-song | Too many genre tags, Weirdness > 60 | One or two genre tags up front, Weirdness down 10-15, Max Mode on |
| Long intro, late hook | Bare `[Intro]`, no bar hint | `[Short Instrumental Intro - 4 bars: ...]`, hook tag second |
| No hook or drop | Flat cue text | "full drums in", "hard drop", "biggest hook" cue language; stop-start or flat-bang arc |
| Muddy low end | Competing bass descriptors | One bass descriptor; mix terms "clean low end", "controlled sub" |
| Stops early / runs on | `[End]` ignored; Duration Auto | Duration Custom = plan + 10 s; Crop or Extend |
| 4-bar loop, no development | No contrast sections | Add a break, bridge or beat switch; vary cue text per section |
| Muffled after ~2:30 | v6 standard-mode drift | Max Mode on |
