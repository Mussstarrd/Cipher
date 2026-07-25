import type { BuildOptions, EngineWarning, SunoPackage } from "./types.ts";
import { DEFAULT_PROFILE, PROFILES } from "./profiles.ts";
import { resolveArtist } from "./artists.ts";
import { DEFAULT_ACCENT_WEIGHT, fuse, type WeightedAccent } from "./fusion.ts";
import { buildExclusions, scanText } from "./exclusion.ts";
import { assembleStyle } from "./style.ts";
import { buildScaffold } from "./structure.ts";
import { SLIDER_PRESETS } from "./sliders.ts";

/** Build a ready-to-paste Suno package from a fusion spec. Deterministic. */
export function buildPackage(options: BuildOptions): SunoPackage {
  const profile = PROFILES[options.profile ?? DEFAULT_PROFILE];
  const warnings: EngineWarning[] = [];

  const dominant = resolveArtist(options.fusion.dominant);
  if (!dominant) {
    throw new Error(
      `Unknown dominant artist "${options.fusion.dominant}". Use listArtists() for available ids.`,
    );
  }

  const accents: WeightedAccent[] = [];
  for (const spec of options.fusion.accents ?? []) {
    const dna = resolveArtist(spec.artist);
    if (!dna) throw new Error(`Unknown accent artist "${spec.artist}".`);
    if (dna.id === dominant.id) {
      warnings.push({ level: "warn", message: `Accent "${spec.artist}" is the dominant — ignored.` });
      continue;
    }
    accents.push({ dna, weight: spec.weight ?? DEFAULT_ACCENT_WEIGHT });
  }

  // Build type: explicit, else inferred — accents present ⇒ fusion, else faithful.
  const build = options.build ?? (accents.length > 0 ? "fusion" : "faithful");
  const template = options.template ?? "hook-first";

  const fused = fuse(dominant, accents, { bpm: options.bpm });
  warnings.push(...fused.warnings);

  const exclusions = buildExclusions(options.ban ?? [], profile);
  warnings.push(...exclusions.warnings);

  const scaffold = buildScaffold(template, dominant, profile, fused.bpm);
  warnings.push(...scaffold.warnings);

  // Template style addenda (e.g. the beat-switch description, research §4.2)
  // ride with the inversion positives: both are load-bearing and must survive
  // sweet-spot trimming.
  const injections = [
    ...exclusions.positiveInjections,
    ...(scaffold.styleAddendum ? [scaffold.styleAddendum] : []),
  ];

  const style = assembleStyle(fused.slots, profile, injections);
  warnings.push(...style.warnings);
  warnings.push(...scanText(scaffold.lyricsScaffold, "lyrics"));

  if (!profile.hasSliders) {
    warnings.push({
      level: "info",
      message: `Profile ${profile.id} (free tier) has no creative sliders (research §6) — slider recommendation applies only if you upgrade.`,
    });
  }

  return {
    styleText: style.styleText,
    excludeText: exclusions.excludeText,
    lyricsScaffold: scaffold.lyricsScaffold,
    sliders: SLIDER_PRESETS[build],
    warnings,
    meta: {
      profile: profile.id,
      template,
      build,
      dominant: dominant.id,
      accents: fused.applied,
      bpm: fused.bpm,
    },
  };
}
