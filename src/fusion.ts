import type { ArtistDNA, DnaMode, EngineWarning, StyleSlots } from "./types.ts";
import { GROOVES } from "./grooves.ts";
import { rng, shuffled } from "./random.ts";

/**
 * Dominant + accent fusion with seeded pool-picking.
 *
 * The dominant DNA owns the identity slots: genre anchor, groove, vocal
 * delivery, BPM. Accents contribute a weight-capped number of
 * instrumentation/texture/mood descriptors so a blend (e.g. Weezer texture
 * over T.I. bounce) colors the track without collapsing the rap identity.
 *
 * Slot arrays are pools: each build draws a seeded subset (signatures at
 * index 0 of instrumentation/vocal always included), so the same DNA choices
 * regenerate a different prompt with the same vibe under a new seed, and an
 * identical one under the same seed.
 *
 * Research grounding: §3.2 (1–2 genres, 2–4 instruments, one strong identity
 * beats many weak signals) and §3.3 (mashups are officially supported but the
 * style vector is lossy — the loudest tags win, so the dominant stays first).
 */
export interface WeightedAccent {
  dna: ArtistDNA;
  weight: number;
}

export const DEFAULT_ACCENT_WEIGHT = 0.3;
export const MAX_ACCENT_WEIGHT = 0.45;

/** Signature-first pick: index 0 always, plus n seeded picks from the rest. */
function pickWithSignature(pool: readonly string[], n: number, rnd: () => number): string[] {
  if (pool.length === 0) return [];
  return [pool[0]!, ...shuffled(pool.slice(1), rnd).slice(0, n)];
}

const SAMPLE_HINT = /(chop|sample|pitch|screw|filter)/i;
const VOCAL_HINT = /(vocal|choir|sung|sing|croon|harmon|chant|hum\b|humming|acapella|a cappella)/i;

/**
 * Instrumental-build filter: any performed-vocal language is out; vocal
 * SAMPLES (chopped/pitched/screwed/filtered) are instruments and stay —
 * that's the "subtle soul sample" exception.
 */
export function allowedInInstrumental(text: string): boolean {
  if (!VOCAL_HINT.test(text)) return true;
  return SAMPLE_HINT.test(text) && /vocal/i.test(text);
}

/**
 * Roll (or pin) one of the DNA's modes and merge its overrides over the base.
 * DNAs without modes pass through unchanged.
 */
function applyMode(
  dna: ArtistDNA,
  rnd: () => number,
  forced?: string,
): { dna: ArtistDNA; mode?: DnaMode } {
  if (!dna.modes?.length) return { dna };
  const mode =
    (forced ? dna.modes.find((m) => m.id === forced) : undefined) ??
    shuffled(dna.modes, rnd)[0]!;
  const merged: ArtistDNA = {
    ...dna,
    ...(mode.genres && { genres: mode.genres }),
    ...(mode.mood && { mood: mode.mood }),
    ...(mode.grooveExtras && { grooveExtras: mode.grooveExtras }),
    ...(mode.instrumentation && { instrumentation: mode.instrumentation }),
    ...(mode.texture && { texture: mode.texture }),
    ...(mode.bpm && { bpm: mode.bpm }),
  };
  return { dna: merged, mode };
}

/** How many descriptors an accent may contribute at a given weight. */
function accentQuota(weight: number): { instrumentation: number; texture: number; vocal: number } {
  if (weight >= 0.4) return { instrumentation: 2, texture: 1, vocal: 1 };
  if (weight >= 0.25) return { instrumentation: 2, texture: 1, vocal: 0 };
  return { instrumentation: 1, texture: 0, vocal: 0 };
}

export function fuse(
  dominant: ArtistDNA,
  accents: WeightedAccent[],
  opts: { bpm?: number; seed?: number; mode?: string; instrumental?: boolean } = {},
): {
  slots: StyleSlots;
  warnings: EngineWarning[];
  bpm: number;
  /** Accents with clamped weights actually applied. */
  applied: { artist: string; weight: number }[];
  /** Label of the dominant's rolled/pinned mode, when it has modes. */
  modeLabel?: string;
} {
  const warnings: EngineWarning[] = [];
  const rnd = rng(opts.seed ?? 0);

  const domMode = applyMode(dominant, rnd, opts.mode);
  dominant = domMode.dna;

  // Beat-only builds: strip performed-vocal language from every pool before
  // picking; vocal-sample descriptors survive as instruments.
  const filterPool = (pool: string[]): string[] =>
    opts.instrumental ? pool.filter(allowedInInstrumental) : pool;

  const clamped = accents.map((raw) => {
    const a = { ...raw, dna: applyMode(raw.dna, rnd).dna };
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

  // Groove: dominant's, always emitted (first-class slot); seeded phrase pick.
  const grooveVocab = GROOVES[dominant.groove];
  const groove = [shuffled(grooveVocab.phrases, rnd)[0]!];
  if (dominant.grooveExtras?.length) {
    groove.push(shuffled(dominant.grooveExtras, rnd)[0]!);
  }

  // Mood: two seeded picks from the dominant pool; strongest accent may add one.
  const mood = shuffled(dominant.mood, rnd).slice(0, 2);
  const topAccent = clamped.slice().sort((a, b) => b.weight - a.weight)[0];
  if (topAccent && topAccent.weight >= 0.25 && topAccent.dna.mood.length) {
    mood.push(shuffled(topAccent.dna.mood, rnd)[0]!);
  }

  // Instrumentation: dominant signature + seeded picks (2 when solo, 1 when
  // accents need room), then accent contributions (their signature first) by
  // quota, capped at 4 total (research §3.2).
  const domInstrPool = filterPool(dominant.instrumentation);
  const instrumentation = pickWithSignature(domInstrPool, clamped.length ? 1 : 2, rnd);
  for (const a of clamped) {
    const quota = accentQuota(a.weight);
    for (const item of pickWithSignature(
      filterPool(a.dna.instrumentation),
      quota.instrumentation - 1,
      rnd,
    )) {
      if (instrumentation.length >= 4) break;
      instrumentation.push(item);
    }
  }
  if (instrumentation.length < 4) {
    const extra = shuffled(domInstrPool.slice(1), rnd).find((i) => !instrumentation.includes(i));
    if (extra) instrumentation.push(extra);
  }

  // Vocal: dominant's signature delivery is untouchable + 1 seeded; a strong
  // accent may add its signature as a harmony/texture hint after.
  // Instrumental builds emit NO vocal slot at all.
  const vocal = opts.instrumental ? [] : pickWithSignature(dominant.vocal, 1, rnd);
  if (!opts.instrumental) {
    for (const a of clamped) {
      const quota = accentQuota(a.weight);
      if (quota.vocal > 0 && a.dna.vocal[0]) vocal.push(a.dna.vocal[0]);
    }
  }

  // Texture: seeded dominant picks (2 when solo, 1 with accents), accents per
  // quota, cap 3.
  const texture = shuffled(filterPool(dominant.texture), rnd).slice(0, clamped.length ? 1 : 2);
  for (const a of clamped) {
    const quota = accentQuota(a.weight);
    for (const item of shuffled(filterPool(a.dna.texture), rnd).slice(0, quota.texture)) {
      if (texture.length >= 3) break;
      texture.push(item);
    }
  }

  // BPM: explicit request wins; otherwise a seeded point in the dominant's
  // range, snapped to 5.
  const [lo, hi] = dominant.bpm;
  let bpm = opts.bpm ?? Math.round((lo + rnd() * (hi - lo)) / 5) * 5;
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
    modeLabel: domMode.mode?.label,
  };
}
