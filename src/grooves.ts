import type { GrooveId, GrooveVocabulary } from "./types.ts";

/**
 * Three distinct rhythmic vocabularies, always one emitted per build, so
 * consecutive prompts don't homogenize into the same feel. Phrasing uses
 * community-verified language (research §3.3: "half-time feel" appears in the
 * documented trap formula; drum/808 character must be named explicitly).
 */
export const GROOVES: Record<GrooveId, GrooveVocabulary> = {
  "behind-beat-pocket": {
    id: "behind-beat-pocket",
    label: "Behind-the-beat pocket",
    phrases: [
      "laid-back half-time feel",
      "drums sitting behind the beat",
      "deep relaxed pocket groove",
    ],
  },
  "displaced-anti-grid": {
    id: "displaced-anti-grid",
    label: "Displaced anti-grid",
    phrases: [
      "off-kilter syncopated drum pattern",
      "displaced snare hits off the grid",
      "stuttering unpredictable hi-hat placement",
    ],
  },
  "forward-aggressive": {
    id: "forward-aggressive",
    label: "Forward-leaning aggressive",
    phrases: [
      "driving forward-leaning rhythm",
      "relentless aggressive drum attack",
      "urgent double-time hi-hat push",
    ],
  },
};

/**
 * Deterministic pick: variant index rotates phrase choice so repeated builds
 * with different seeds vary wording without randomness.
 */
export function groovePhrase(id: GrooveId, variant = 0): string {
  const vocab = GROOVES[id];
  const phrase = vocab.phrases[variant % vocab.phrases.length];
  return phrase ?? vocab.phrases[0]!;
}
