import type { StyleSlots } from "./types.ts";
import { rng, shuffled } from "./random.ts";

/**
 * Production-polish descriptor pool — research §11.
 *
 * Community consensus (2026): generic praise ("studio quality", "high
 * quality") is low-value on v5+, but SPECIFIC engineering language still
 * steers the mix. This pool holds only phrases with multi-source backing.
 * Also per §11: never stack compression language (Suno's output is already
 * heavily compressed), so the pool stays clear of compression terms.
 */
export const POLISH_POOL = [
  "punchy low end",
  "wide stereo image",
  "crisp highs",
  "vocal-forward mix",
  "natural dynamics",
  "analog warmth",
] as const;

const stop = new Set(["mix", "end", "image"]);

function words(s: string): string[] {
  return s.toLowerCase().split(/[^a-z]+/).filter((w) => w.length > 3 && !stop.has(w));
}

/**
 * Pick up to `n` seeded polish phrases that don't overlap wording already in
 * the style slots (e.g. skip "crisp highs" when the DNA already says "crisp
 * trap hi-hat rolls" — redundant tags dilute the vector, research §3.2).
 */
export function pickPolish(slots: StyleSlots, seed: number, n = 2): string[] {
  const existing = new Set(
    [
      ...slots.genre,
      ...slots.mood,
      ...slots.groove,
      ...slots.instrumentation,
      ...slots.vocal,
      ...slots.texture,
    ].flatMap(words),
  );
  const candidates = POLISH_POOL.filter((p) => words(p).every((w) => !existing.has(w)));
  return shuffled(candidates, rng(seed ^ 0x5f3759df)).slice(0, n);
}
