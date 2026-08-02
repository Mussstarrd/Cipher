import type { BuildOptions, EngineWarning, SunoPackage } from "./types.ts";
import { DEFAULT_PROFILE, PROFILES } from "./profiles.ts";
import { resolveArtist } from "./artists.ts";
import { DEFAULT_ACCENT_WEIGHT, fuse, type WeightedAccent } from "./fusion.ts";
import { buildExclusions, scanText } from "./exclusion.ts";
import { assembleStyle } from "./style.ts";
import { assembleLyrics, buildScaffold } from "./structure.ts";
import { SLIDER_PRESETS } from "./sliders.ts";
import { pickPolish } from "./polish.ts";

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

  const seed = options.seed ?? 0;
  const instrumental = options.instrumental ?? false;
  const fused = fuse(dominant, accents, {
    bpm: options.bpm,
    seed,
    mode: options.mode,
    instrumental,
  });
  warnings.push(...fused.warnings);

  const guards = options.laneGuards === false ? [] : (dominant.laneGuards ?? []);
  const exclusions = buildExclusions(options.ban ?? [], profile, guards, { instrumental });
  warnings.push(...exclusions.warnings);

  const scaffold = buildScaffold(
    template,
    dominant,
    profile,
    fused.bpm,
    options.verses ?? 2,
    instrumental,
  );
  warnings.push(...scaffold.warnings);

  if (instrumental) {
    warnings.push({
      level: "info",
      message:
        'Beat-only build: ALSO switch on Suno\'s own "Instrumental" toggle — style/exclude/tags alone leak vocals (research §5 triple-layering).',
    });
  }

  // Template style addenda (e.g. the beat-switch description, research §4.2)
  // ride with the inversion positives: both are load-bearing and must survive
  // sweet-spot trimming.
  const injections = [
    ...exclusions.positiveInjections,
    ...(scaffold.styleAddendum ? [scaffold.styleAddendum] : []),
  ];

  const style = assembleStyle(
    fused.slots,
    profile,
    injections,
    pickPolish(fused.slots, seed, 2, { instrumental }),
  );
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
    lyricsSections: scaffold.sections,
    lyricsTagsOnly: assembleLyrics(scaffold.sections, {}, { tagsOnly: true }),
    sliders: SLIDER_PRESETS[build],
    warnings,
    meta: {
      profile: profile.id,
      template,
      build,
      dominant: dominant.id,
      accents: fused.applied,
      bpm: fused.bpm,
      seed,
      verses: options.verses ?? 2,
      mode: fused.modeLabel,
      instrumental,
    },
  };
}
