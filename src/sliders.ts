import type { BuildType, SliderRecommendation } from "./types.ts";

/**
 * Slider presets per build type — research §6 (community recipes; the UI is
 * unnumbered, so ranges are conventions to eyeball on the Safe↔Chaos /
 * Loose↔Strong scales). Fusion pairs raised weirdness with mid-high style
 * influence — never low — so experiments stay inside the genre lane.
 */
export const SLIDER_PRESETS: Record<BuildType, SliderRecommendation> = {
  faithful: {
    weirdness: { min: 10, max: 25 },
    styleInfluence: { min: 80, max: 100 },
    note: "Genre-faithful: low weirdness, strict style adherence (research §6).",
  },
  balanced: {
    weirdness: { min: 30, max: 50 },
    styleInfluence: { min: 60, max: 80 },
    note: "Creative but listenable: the widely cited 40–60% weirdness sweet spot, style strongly guiding (research §6).",
  },
  fusion: {
    weirdness: { min: 50, max: 70 },
    styleInfluence: { min: 60, max: 80 },
    note: "Experimental fusion: high weirdness + mid-high style influence keeps the blend inside the lane; never drop style influence low here (research §6).",
  },
};
