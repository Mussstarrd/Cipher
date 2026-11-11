# Suno metatag cheat sheet (Lyrics field, instrumental use)

Built from `research/02-metatag-grammar.md` (community-tested on v4.5 through v6; Suno documents almost none of this). Ratings: **Reliable** (3+ independent sources, consistent), **Mixed** (works often, not always), **Unreliable** (mostly ignored). Everything in the Lyrics field is a hint, not a command; the Style field and sliders decide most of the sound.

## Grammar (as suno-forge writes it)

- One tag per line, square brackets: `[Hook | the whole beat from the first second, the tine piano up front]`. The tag name, a pipe, then plain directions. Suno reads the words inside the bracket as arrangement and performance cues (render-tested; see `research/handoff/`).
- **Never use parentheses.** Parenthesized text is reliably sung as backing voices.
- No bare text lines. Anything outside brackets may be sung.
- Refer to sounds by the same **handle** the Style field used ("the sliced soul keys", "the cassette loop"), so the tags and the Style field describe one record.
- Bar counts are approximate hints only; the generator writes them on intros and bare verses and sets the Duration control from its own estimate.
- Start with `[Instrumental]` and end with `[End]`; one `[End]` only.
- Never write fill, roll, riser, sweep, build, drop, transition, "comes in", "enters", "returns", "every 4 bars" inside a tag. Say the steady state: "same drums", "identical". Never describe the drums stopping, cutting or coming back: Suno stages a lead-in at every boundary it is told about.
- Mix-operation words work as descriptions inside a tag but not as standalone tags (`[Tape Stop]` alone is unreliable, and "tape stop" is banned here anyway as a transition word).
- Loop mode: every `[Loop | ...]` tag says identical. Switch mode: one axis, its words in the head tag, the `[Beat Switch | ...]` cue and every section after it.

## Section tags

| Tag | Rating | Notes |
|---|---|---|
| `[Instrumental]` | Reliable | Leading line; marks the track as no-vocals |
| `[Intro \| the lead alone, 4 bars]` | Reliable with directions | A bare `[Intro]` is "notoriously unreliable"; `[Intro \| ...]` with the lead handle and a bar count behaves. (`[Short Instrumental Intro]` also works.) |
| `[Loop \| same loop, identical, ...]` | Reliable | The hypnotic minimal layouts; say identical every time |
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
[Verse | 8 bars, live kick and bass only, beats 1 and 2 straight, beat 3 slurred into three]
[Hook | the whole beat from the first second, the tine piano up front]
[Verse | same drums, the muted trumpet in place of the tine piano, a dry wooden click in uneven pairs]
[Hook | identical to the first hook]
```

Describe what the melodic sounds do and one timing-feel cue. Contrast comes from the lead (half as many notes, an octave lower, resting while the bass carries it, trading with the counter), never from the drums leaving and coming back.

## Ending and length control

- Last line: `[End]`, straight after the final section. This toolkit writes no intro, break or outro: each is a boundary Suno decorates.
- Set Duration (Custom) to planned runtime + ~10 s; Suno hard-cuts there if `[End]` is ignored. Never Auto on v6 (runs long).
- Too short: Extend from the last good section and paste only the remaining tags.
- Too long: Crop to the last downbeat.

## Groove and anti-grid tricks

No source has measured swing in Suno output; this is practitioner consensus (3+ sources unless noted). Groove comes from **vocabulary**, not sliders.

**Style field phrases that loosen the grid:** live drums / live drum kit; behind the beat / slightly late snare; pocket groove / loose pocket; swung hi-hats / subtle swing; ghost notes on the snare; brushed snare; unquantized groove / played live not quantized; dusty sampled drum break; boom bap + 90s hip hop + vinyl crackle (vintage tags stop Suno modernizing boom bap). Second tier (1-2 sources): subtle timing jitter, human micro-rubato, one-take feel, neo-soul drummer, shuffle, triplet feel, half-time feel, linear drumming.

**Modifiers:** "slight" and "subtle" keep imperfection from tipping into chaos.

**Words reported to backfire:** "complex drums" (chaos), "aggressive" / "hard" (pulls to generic trap), "realistic", "crescendo", "ethereal", "lush", "shimmering". **"MPC swing 58%"**-style percentages: no evidence Suno maps them. **Producer names** ("Dilla-style"): banned here regardless; Suno rewrites some names.

**Lyrics field moves (general Suno, not used by this toolkit):** `[Drum Break]`, `[Break]`/`[Breakdown]`, `[Build]` -> `[Drop]`. Each one is a boundary, and in listening tests every boundary came back with a reversed kick or a lead-in, so the layouts here use none of them.

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
