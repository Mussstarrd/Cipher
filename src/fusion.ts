import type { ArtistDNA, EngineWarning, StyleSlots } from "./types.ts";
import { groovePhrase } from "./grooves.ts";

/**
 * Dominant + accent fusion.
 *
 * The dominant DNA owns the identity slots: genre anchor, groove, vocal
 * delivery, BPM. Accents contribute a weight-capped number of
 * instrumentation/texture/mood descriptors so a blend (e.g. Weezer texture
 * over T.I. bounce) colors the track without collapsing the rap identity.
 * Research grounding: §3.2 (1–2 genres, 2–4 instruments, one strong identity
 * beats many weak signals) and §3.3 (mashups are officially supported but the
 * style vector is lossy — the loudest tags win, so the dominant must stay
 * loudest by coming first).
 */
export interface WeightedAccent {
  dna: ArtistDNA;
  weight: number;
}

export const DEFAULT_ACCENT_WEIGHT = 0.3;
export const MAX_ACCENT_WEIGHT = 0.45;

/** How many descriptors an accent may contribute at a given weight. */
function accentQuota(weight: number): { instrumentation: number; texture: number; vocal: number } {
  if (weight >= 0.4) return { instrumentation: 2, texture: 1, vocal: 1 };
  if (weight >= 0.25) return { instrumentation: 2, texture: 1, vocal: 0 };
  return { instrumentation: 1, texture: 0, vocal: 0 };
}

export function fuse(
  dominant: ArtistDNA,
  accents: WeightedAccent[],
  opts: { bpm?: number; grooveVariant?: number } = {},
): {
  slots: StyleSlots;
  warnings: EngineWarning[];
  bpm: number;
  /** Accents with clamped weights actually applied. */
  applied: { artist: string; weight: number }[];
} {
  const warnings: EngineWarning[] = [];

  const clamped = accents.map((a) => {
    if (a.weight > MAX_ACCENT_WEIGHT) {
      warnings.push({
        level: "info",
        message: `Accent "${a.dna.id}" weight ${a.weight} capped at ${MAX_ACCENT_WEIGHT} to protect the dominant rap identity.`,
      });
      return { ...a, weight: MAX_ACCENT_WEIGHT };
    }
    return a;
  });

  // Genre: dominant's primary anchor always leads (research §3.1 front-loading).
  // One accent genre may ride along only for a strong accent, keeping total ≤ 2.
  const genre = [dominant.genres[0]!];
  const strongAccent = clamped.find((a) => a.weight >= 0.4);
  if (strongAccent) {
    genre.push(strongAccent.dna.genres[0]!);
  } else if (dominant.genres[1]) {
    genre.push(dominant.genres[1]);
  }

  // Groove: dominant's, always emitted (first-class slot).
  const groove = [groovePhrase(dominant.groove, opts.grooveVariant ?? 0)];
  if (dominant.grooveExtras?.[0]) groove.push(dominant.grooveExtras[0]);

  // Mood: dominant leads; strongest accent may add one.
  const mood = [...dominant.mood.slice(0, 2)];
  const topAccent = clamped.slice().sort((a, b) => b.weight - a.weight)[0];
  if (topAccent && topAccent.weight >= 0.25 && topAccent.dna.mood[0]) {
    mood.push(topAccent.dna.mood[0]);
  }

  // Instrumentation: dominant first, then accent contributions by quota,
  // capped at 4 total (research §3.2: 2–4 instruments).
  const instrumentation = [...dominant.instrumentation.slice(0, 2)];
  for (const a of clamped) {
    const quota = accentQuota(a.weight);
    for (const item of a.dna.instrumentation.slice(0, quota.instrumentation)) {
      if (instrumentation.length >= 4) break;
      instrumentation.push(item);
    }
  }
  if (instrumentation.length < 4 && dominant.instrumentation[2]) {
    instrumentation.push(dominant.instrumentation[2]);
  }

  // Vocal: dominant's delivery is untouchable; a strong accent may add one
  // harmony/texture descriptor after it.
  const vocal = [...dominant.vocal.slice(0, 2)];
  for (const a of clamped) {
    const quota = accentQuota(a.weight);
    if (quota.vocal > 0 && a.dna.vocal[0]) vocal.push(a.dna.vocal[0]);
  }

  // Texture: dominant first, accents per quota, cap 3.
  const texture = [...dominant.texture.slice(0, 1)];
  for (const a of clamped) {
    const quota = accentQuota(a.weight);
    for (const item of a.dna.texture.slice(0, quota.texture)) {
      if (texture.length >= 3) break;
      texture.push(item);
    }
  }

  // BPM: explicit request wins; otherwise midpoint of the dominant's range.
  const [lo, hi] = dominant.bpm;
  let bpm = opts.bpm ?? Math.round((lo + hi) / 2 / 5) * 5;
  if (opts.bpm && (opts.bpm < lo || opts.bpm > hi)) {
    warnings.push({
      level: "warn",
      message: `Requested ${opts.bpm} BPM is outside the dominant DNA's ${lo}–${hi} range; using it anyway.`,
    });
    bpm = opts.bpm;
  }

  return {
    slots: { genre, mood, groove, instrumentation, vocal, texture, bpm },
    warnings,
    bpm,
    applied: clamped.map((a) => ({ artist: a.dna.id, weight: a.weight })),
  };
}
