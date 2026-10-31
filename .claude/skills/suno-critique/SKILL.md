---
name: suno-critique
description: Diagnose and rewrite a Suno prompt that produced a bad instrumental. Use when the user pastes a Style / Lyrics / Exclude prompt (or a full suno-forge block) plus a description of what went wrong (vocals leaked, drums too rigid, wrong genre, no hook, too long, muddy, ignored structure). Outputs a diagnosis table and a corrected copy-ready block.
---

# suno-critique

Input: the prompt that was used (Style field, Lyrics field, Exclude field, sliders, model, toggles if known) and the user's description of the bad result. Output: a diagnosis, then a full corrected block in the `suno-prompt` format.

## Procedure

1. **Run QA on what they pasted.** Save the pasted fields into a temporary block file (or JSON with `style`, `lyrics`, `exclude`, `sliders`, `toggles`, `model`, `meta.bpm`) and run:
   ```bash
   node forge.js qa /tmp/critique.json
   ```
   Report every ERROR and warning; many "Suno ignored me" cases are a rule violation (Variety left on Normal, a negative in Style, a vocal-implying word, an artist name that got silently rewritten, bare `[Intro]`).
2. **Match the symptom to a known failure mode** using the table in `reference/metatag-cheatsheet.md` ("Known instrumental failure modes") and the lane's `listenFor` notes in `lib/lanes.json`. Common mappings:

   | Symptom | First suspects | Fix |
   |---|---|---|
   | Humming, "ooh" pads, ad-libs, sung syllables | vocal-implying Style words (soulful, choir, chant, gospel, vocal chops, R&B); Variety above Off; long empty intro | strip words; Variety Off; Exclude `vocals, humming, ad-libs, vocal chops`; `[Instrumental]` first line; Replace Section on the leak |
   | Prompt ignored / generic result | Variety Normal rewrote Style; artist name triggered the rewrite; Style Influence too low | Variety Off; remove names; Style Influence +10-15 |
   | Rigid, quantized "grid" drums | no groove vocabulary; "aggressive"/"hard"/"complex drums" present | add 2-3 of: live drums, behind the beat, swung hi-hats, ghost notes, unquantized groove, loose pocket; Weirdness 40-55; remove destabilisers |
   | Drifted to drift phonk / EDM / trailer | genre words too broad; too many genre tags | lead with one precise genre tag; Exclude `drift phonk, cowbell lead melody, EDM build, cinematic trailer` |
   | Genre switch mid-song | Weirdness > 60; conflicting genre tags; no Max Mode | Weirdness -15; two genre tags max; Max Mode ON |
   | Long intro, late hook | bare `[Intro]`, no bar hints, slow-burn arc | `[Short Instrumental Intro - 4 bars: ...]`; flat-bang or stop-start arc |
   | No hook / no drop / flat energy | identical cue text in every section; no contrast section | "full drums in" / "hard drop" / "biggest hook" cues; add Break or Drum Break; stop-start arc |
   | Muddy low end | two bass descriptors; "distorted" + "sub" fighting | one bass descriptor; mix words "clean low end, controlled sub" |
   | Stopped early / ran long | `[End]` ignored; Duration Auto | Duration Custom = plan + 10 s; Crop or Extend |
   | Muffled after ~2:30 | v6 standard mode | Max Mode ON |
   | 4-bar loop, no development | no section contrast | vary cues per section; add Instrumental Bridge or Beat Switch |

3. **Decide: edit or regenerate.** If the lane, BPM and arc were right, regenerate with the same seed plus targeted flags (`--constraints`, `--exclude`, `--weirdness`, `--styleInfluence`, `--arc`). If the lane was wrong, say so and regenerate on the right lane.
4. **Rewrite.** Produce the corrected block with `node forge.js ... --qa`. If you hand-edit, re-run `node forge.js qa <file>`.
5. **Respond** with: (a) a short diagnosis table (symptom -> cause -> change made), (b) the full corrected block verbatim, (c) one sentence on what to listen for on the next generation. Never recommend uploading audio, Audio Influence, Cover, or a reference clip as a fix.

## Rules

- Negatives never move into the Style field as a fix; they go to Exclude as bare nouns.
- Do not add more than four new descriptors in one rewrite; change the fewest things that explain the symptom so the next A/B is informative.
- Keep lane vocabulary from `reference/vocabulary-bank.md`; no artist names, no "type beat".
- If the user's complaint is about Suno behaviour that `reference/suno-field-spec.md` cannot explain, suggest running the `suno-researcher` agent to re-verify the spec before guessing.
