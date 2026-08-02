import type { ModelProfile, ProfileId } from "./types.ts";

/**
 * Field budgets per model profile — research §2 (community/API-reseller
 * consensus; Suno publishes no official numbers, and truncation past the cap
 * is silent, so the engine enforces budgets itself).
 *
 * Tier mapping (research §1): free tier = v4.5-all; Pro/Premier = v5/v5.5.
 * Sliders are Pro/Premier (research §6), so the free profile marks them absent.
 */
export const PROFILES: Record<ProfileId, ModelProfile> = {
  "v4.5-all": {
    id: "v4.5-all",
    styleCharLimit: 1000,
    lyricsCharLimit: 5000,
    excludeCharLimit: 1000,
    maxExcludeTerms: 5,
    hasSliders: false,
    hasDurationSlider: false,
  },
  v5: {
    id: "v5",
    styleCharLimit: 1000,
    lyricsCharLimit: 5000,
    excludeCharLimit: 1000,
    maxExcludeTerms: 5,
    hasSliders: true,
    hasDurationSlider: false,
  },
  "v5.5": {
    id: "v5.5",
    styleCharLimit: 1000,
    lyricsCharLimit: 5000,
    excludeCharLimit: 1000,
    maxExcludeTerms: 5,
    hasSliders: true,
    hasDurationSlider: true,
  },
};

export const DEFAULT_PROFILE: ProfileId = "v5.5";

/**
 * Style-field word budget target — research §3.2: guides put the sweet spot
 * around 15–30 words (some allow up to ~100) against the 1,000-char cap. The
 * engine trims toward `max`, allowing fusion builds a little more room since
 * blends carry more load-bearing descriptors than single-DNA builds.
 */
export const STYLE_WORD_TARGET = { min: 15, max: 44 };
