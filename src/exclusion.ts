import type { EngineWarning, ModelProfile } from "./types.ts";
import { blockedNameTokens } from "./artists.ts";

/**
 * Two-tier kill list + negative-to-positive inversion.
 *
 * Research grounding:
 * - §5: the exclude field is soft guidance that leaks; Style beats Exclude, so
 *   every exclusion must be paired with a dominant competing positive that
 *   fills the vacated musical role ("acoustic only" beats "no electric").
 * - §5: >5 exclude terms degrades output; never emit "-" prefixes.
 * - §3.2/§5: never write "no X" phrasing inside the style field (elephant
 *   effect is documented for style-field negations).
 * - §3.4: artist names hard-block or get silently stripped → hard-ban tier.
 */

/** Inversion map: banned element → competing positive for the style field. */
const INVERSIONS: Record<string, string> = {
  saxophone: "heavy synth brass lead",
  sax: "heavy synth brass lead",
  "acoustic guitar": "crunchy distorted electric guitar",
  piano: "dark analog synth chords",
  "edm drop": "hard trap beat drop",
  edm: "heavy 808-driven trap production",
  "pop sheen": "gritty lo-fi mix",
  pop: "hard-hitting rap production",
  "female vocals": "commanding male rap vocal",
  "male vocals": "commanding female rap vocal",
  autotune: "raw untreated vocal delivery",
  "auto-tune": "raw untreated vocal delivery",
  country: "urban trap production",
  "live crowd": "tight studio mix",
  choir: "solo lead vocal focus",
  strings: "synth pad atmosphere",
  flute: "dark synth lead melody",
  whistle: "synth lead melody",
};

/**
 * Hard-ban tier: verified-problem content that must never reach a Suno field.
 * Currently: every artist name/alias in the DNA library (research §3.4).
 */
export function hardBanTokens(): string[] {
  return blockedNameTokens();
}

/**
 * Watch-list tier: patterns that don't block, but warn the user instead of
 * being silently stripped (research §3.2, §4.1).
 */
const WATCH_PATTERNS: { pattern: RegExp; message: string }[] = [
  {
    pattern: /\bno\s+\w+/i,
    message:
      '"no X" phrasing detected in style text — negations inside the style field backfire (elephant effect, research §5). Use a ban/inversion instead.',
  },
  {
    pattern: /[\[\]]/,
    message:
      "Square brackets detected in style text — brackets belong only in the lyrics box; in the style field they can cause glitches (research §3.2).",
  },
  {
    pattern: /\b(beautiful|epic|amazing|awesome|incredible)\b/i,
    message:
      "Vague affective word detected — widely reported as a no-op in style prompts (research §3.2). Replace with concrete sonic language.",
  },
  {
    pattern: /^-|,\s*-/,
    message:
      'Leading "-" detected — minus prefixes are UI display output, not input syntax (research §5).',
  },
];

export interface ExclusionResult {
  /** Comma-joined exclude-field text (may be empty). */
  excludeText: string;
  /** Competing positives to inject into the style field's instrumentation slot. */
  positiveInjections: string[];
  warnings: EngineWarning[];
}

export function buildExclusions(bans: string[], profile: ModelProfile): ExclusionResult {
  const warnings: EngineWarning[] = [];
  const terms: string[] = [];
  const positives: string[] = [];

  for (const raw of bans) {
    const ban = raw.trim().toLowerCase();
    if (!ban) continue;
    if (terms.length >= profile.maxExcludeTerms) {
      warnings.push({
        level: "warn",
        message: `Ban "${ban}" dropped: more than ${profile.maxExcludeTerms} exclude terms degrades output (research §5). Prioritize your bans.`,
      });
      continue;
    }
    terms.push(ban);
    const positive = INVERSIONS[ban];
    if (positive) {
      positives.push(positive);
    } else {
      warnings.push({
        level: "info",
        message: `No inversion mapping for "${ban}" — exclude alone is leaky (research §5). Add a competing positive to the style yourself, or extend the inversion map.`,
      });
    }
  }

  let excludeText = terms.join(", ");
  if (excludeText.length > profile.excludeCharLimit) {
    excludeText = excludeText.slice(0, profile.excludeCharLimit);
    warnings.push({ level: "warn", message: "Exclude text hit the character limit and was trimmed." });
  }

  return { excludeText, positiveInjections: positives, warnings };
}

/** Scan any Suno-bound text for hard-banned tokens and watch-list patterns. */
export function scanText(text: string, context: "style" | "lyrics"): EngineWarning[] {
  const warnings: EngineWarning[] = [];
  const lower = text.toLowerCase();

  for (const token of hardBanTokens()) {
    const t = token.toLowerCase();
    // Only match as a standalone word to avoid false hits ("x", "tip" etc.).
    if (t.length < 3) continue;
    const escaped = t.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
    // No lookbehind: unsupported on iOS Safari < 16.4, and this code ships to browsers.
    const re = new RegExp(`(^|[^a-z0-9])${escaped}($|[^a-z0-9])`, "i");
    if (re.test(lower)) {
      warnings.push({
        level: "block",
        message: `Artist name "${token}" found in ${context} text — Suno blocks or strips artist names (research §3.4). Remove it.`,
      });
    }
  }

  if (context === "style") {
    for (const { pattern, message } of WATCH_PATTERNS) {
      if (pattern.test(text)) warnings.push({ level: "warn", message });
    }
  }

  return warnings;
}
