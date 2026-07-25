import type { EngineWarning, ModelProfile, StyleSlots } from "./types.ts";
import { STYLE_WORD_TARGET } from "./profiles.ts";
import { scanText } from "./exclusion.ts";

/**
 * Style-field assembly.
 *
 * Emission order is the influence order (research §3.1: front-load; genre →
 * mood → groove/drums → instrumentation → vocal → production/BPM ordering per
 * §3.2/§3.3). Output is the 2026 de-facto standard: comma-joined descriptor
 * phrases, no brackets, no artist names, BPM last.
 *
 * When the text overshoots the ~15–34 word sweet spot, descriptors are dropped
 * lowest-priority-first — never the genre anchor, groove core, dominant vocal
 * identity, inversion positives, or BPM, which are load-bearing.
 */
interface Prioritized {
  text: string;
  /** 0 = never drop; higher = dropped sooner. */
  priority: number;
}

function prioritize(
  slots: StyleSlots,
  positiveInjections: string[],
  polish: string[],
): Prioritized[] {
  const items: Prioritized[] = [];
  const push = (arr: string[], base: number, extra: number) =>
    arr.forEach((text, i) => items.push({ text, priority: i === 0 ? base : extra }));

  push(slots.genre, 0, 0); // 1–2 genres, both anchors
  push(slots.mood, 1, 4);
  push(slots.groove, 0, 4);
  // All instrumentation is protected: accent contributions here are the whole
  // point of a fusion — dropping them collapses the blend into the dominant.
  slots.instrumentation.forEach((text) => items.push({ text, priority: 1 }));
  // Inversion positives must survive — they crowd out banned elements (research §5).
  positiveInjections.forEach((text) => items.push({ text, priority: 1 }));
  // vocal[0] is the dominant's delivery identity — untouchable.
  slots.vocal.forEach((text, i) => items.push({ text, priority: i === 0 ? 0 : i === 1 ? 2 : 3 }));
  push(slots.texture, 3, 5);
  // Production polish: specific engineering language, low-value words already
  // filtered upstream (research §11). Kept over texture extras, under identity.
  polish.forEach((text) => items.push({ text, priority: 2 }));
  if (slots.bpm) items.push({ text: `${slots.bpm} BPM`, priority: 0 });
  return items;
}

export function assembleStyle(
  slots: StyleSlots,
  profile: ModelProfile,
  positiveInjections: string[] = [],
  polish: string[] = [],
): { styleText: string; warnings: EngineWarning[] } {
  const warnings: EngineWarning[] = [];

  // Dedup while preserving first (highest-influence) position.
  const seen = new Set<string>();
  let items = prioritize(slots, positiveInjections, polish).filter((p) => {
    const key = p.text.toLowerCase();
    if (!p.text || seen.has(key)) return false;
    seen.add(key);
    return true;
  });

  const wordCount = (list: Prioritized[]) =>
    list.map((p) => p.text).join(", ").split(/\s+/).length;

  const originalWords = wordCount(items);
  if (originalWords > STYLE_WORD_TARGET.max) {
    // Drop lowest-priority items first, later-positioned first within a tier.
    for (let tier = 5; tier >= 2 && wordCount(items) > STYLE_WORD_TARGET.max; tier--) {
      for (let i = items.length - 1; i >= 0 && wordCount(items) > STYLE_WORD_TARGET.max; i--) {
        if (items[i]!.priority === tier) items = items.filter((_, j) => j !== i);
      }
    }
    warnings.push({
      level: "info",
      message: `Style text trimmed from ${originalWords} words toward the ~${STYLE_WORD_TARGET.min}–${STYLE_WORD_TARGET.max} word sweet spot (research §3.2); lowest-priority descriptors dropped first.`,
    });
  }

  let styleText = items.map((p) => p.text).join(", ");

  if (styleText.length > profile.styleCharLimit) {
    styleText = styleText.slice(0, profile.styleCharLimit).replace(/,\s*[^,]*$/, "");
    warnings.push({
      level: "warn",
      message: `Style text exceeded the ${profile.styleCharLimit}-char limit and was trimmed — Suno truncates silently past the cap (research §2).`,
    });
  }

  warnings.push(...scanText(styleText, "style"));
  return { styleText, warnings };
}
