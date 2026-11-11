---
name: suno-critique
description: Diagnose and rewrite a Suno prompt that produced a bad instrumental. Use when the user pastes a Styles / Lyrics / Exclude prompt (or a suno-forge block) plus what went wrong (voices or humming, fills or risers at section changes, carnival or jazz feel, rigid grid, wrong genre, EDM drop, trailer music, no hook, muddy, stopped early). Outputs a diagnosis table and a corrected copy-ready block.
---

# suno-critique

Input: the fields that were used (Styles, Lyrics, Exclude, sliders, model, toggles if known) plus the complaint. Output: a short diagnosis, then a full corrected block from the generator.

## Procedure

1. **Run the guard and QA on what they pasted.** Save the fields as JSON (`style`, `lyrics`, `exclude`, `sliders.weirdness`, `sliders.styleInfluence`, `toggles.instrumental`, `toggles.customMode`, `toggles.variety`, `model.pick`, `meta.bpm`, `meta.lane` if known) and run `node forge.js qa /tmp/critique.json`. Most "Suno ignored me" cases are a rule violation the guard names: a word that summons voices, a swing/meter word, a fill or transition word, a negation, an artist name, Variety left on Normal, a bare `[Intro]`.
2. **Map the symptom** (the listening rules in `reference/grammar-and-guard.md` and `reference/metatag-cheatsheet.md`):

   | Symptom | First suspects | Fix |
   |---|---|---|
   | Humming, "ooh" pads, chants, a sung hook | a voice-class word (soulful, choir, chant, gospel, anthem, crowd, call and response, vocal chops); Variety above Off; a long empty intro | strip the word (the guard names it); Variety Off; re-roll with a new seed; Replace Section on the leak |
   | Fills, risers, a reversed kick or lead-in at every section change or every fourth bar, DJ scratches | any word that names a boundary ("hard cuts", "stops dead", "straight in", "drums out", "back"), places a hit against the end of a bar ("before the downbeat", "last beat", "into the next bar"), repeats on a cycle ("every 4 bars"), or says reverse, record, retrigger or stutter; an intro, break or outro tag | delete the phrase; one steady-drums phrase in the drums line ("every bar of drums identical"); drums from the first second; a rolled cadence; Exclude carries the nouns |
   | Carnival, oom-pah, jazz-club feel | waltz, lilt, shuffle, swing, swung, a time signature | say the slur physically ("the 808 stumbles into three on beat 3"); the generator never uses those words |
   | Rigid, quantized grid | no human-timing clauses; a label instead of a physical description; Weirdness under 40 | re-roll (every lead carries a "played by hand" clause); Weirdness 45-55; v6-wild for the weird lane |
   | Generic trap / stock preset | the drums line got cut, or the lead was a one-phrase label | shorten the counter, keep `drums:` and `lead:` lines; physical descriptions with handles |
   | EDM drop, trailer music, elevator jazz | drop, riser, sidechain, cinematic, epic, orchestral, strings, pads, smooth, Rhodes, sax, mellow | remove; the lane's deny list and Exclude handle it |
   | Beat "returns" after the switch | the switch had no single axis or its words were not repeated | one axis; its words in the head tag, the cue and every later section |
   | Long intro, late hook | bare `[Intro]`; a 16-bar verse before the first hook | no intro tag at all; hook_first or space_verses layout |
   | Stopped early / ran long | `[End]` ignored; Duration Auto | Duration Custom at estimate + buffer; Crop or Extend |
   | Muffled after ~2:30 | v6 standard mode | Max Mode ON |

3. **Decide: regenerate.** Almost always regenerate with the same seed plus targeted flags (`--mood`, `--constraints`, `--exclude`, `--mode`, `--layout`, `--weirdness`, `--styleInfluence`, `--wild`) or a new seed when the sounds themselves were wrong. If the lane was wrong, say so and use the right lane.
4. **Respond** with: (a) a diagnosis table (symptom -> cause -> change), quoting the offending words exactly, (b) the corrected block verbatim from `node forge.js ... --qa`, (c) one sentence on what to listen for. Never recommend uploads, Audio Influence, Cover or a reference clip.

## Rules

- Negatives never move into Styles as a fix; the Exclude box is the only place a ban works.
- Change the fewest things that explain the symptom so the next A/B is informative.
- If the complaint is about Suno behaviour the field spec cannot explain, suggest the `suno-researcher` agent before guessing.
