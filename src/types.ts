/**
 * Core data models for the CIPHER engine.
 *
 * Every constant that encodes a Suno behavior traces to docs/suno-research.md
 * (cited as "research §N" in comments). Do not add Suno facts here without a
 * research-doc entry.
 */

/** Ordered style slots. Emission order = influence order (research §3.1: front-load genre). */
export interface StyleSlots {
  /** 1–2 genre anchors, always emitted first (research §3.2: limit to 1–2 genres). */
  genre: string[];
  mood: string[];
  /** Always emitted — rhythmic vocabulary prevents prompt homogenization. */
  groove: string[];
  /** 2–4 instruments (research §3.2); 808/hi-hat character named explicitly (§3.3). */
  instrumentation: string[];
  /** Vocal delivery / flow descriptors. */
  vocal: string[];
  /** Era / production texture. */
  texture: string[];
  /** Optional BPM anchor, emitted last with production info (research §3.3). */
  bpm?: number;
}

export type ProfileId = "v4.5-all" | "v5" | "v5.5";

export interface ModelProfile {
  id: ProfileId;
  /** research §2 — community/API-reseller consensus, not official. */
  styleCharLimit: number;
  lyricsCharLimit: number;
  excludeCharLimit: number;
  /** research §5 — >5 exclusions degrades output. */
  maxExcludeTerms: number;
  hasSliders: boolean;
  hasDurationSlider: boolean;
}

export type GrooveId =
  | "behind-beat-pocket"
  | "displaced-anti-grid"
  | "forward-aggressive";

export interface GrooveVocabulary {
  id: GrooveId;
  label: string;
  /** Community phrasing Suno responds to (research §3.3, e.g. "half-time feel"). */
  phrases: string[];
}

export interface ArtistDNA {
  id: string;
  /** Human label only — never emitted into Suno-bound fields (research §3.4). */
  displayName: string;
  /** Alternate spellings, used for input matching and output leak-scanning. */
  aliases: string[];
  genres: string[];
  mood: string[];
  groove: GrooveId;
  /** Extra rhythm phrases beyond the base groove vocabulary. */
  grooveExtras?: string[];
  instrumentation: string[];
  vocal: string[];
  texture: string[];
  bpm: [number, number];
  /** Generic ad-lib words for parenthetical placement (research §4.5). */
  adlibs?: string[];
  /** Flow/rhyme guidance surfaced in the lyric scaffold (research §4.3: bar cadence beats tags). */
  lyricNotes?: string[];
}

/** Build types map to slider presets (research §6). */
export type BuildType = "faithful" | "balanced" | "fusion";

export interface AccentSpec {
  artist: string;
  /** 0–1; default 0.3. Capped so accents never displace the dominant rap identity. */
  weight?: number;
}

export interface FusionSpec {
  dominant: string;
  accents?: AccentSpec[];
}

export interface SliderRange {
  min: number;
  max: number;
}

export interface SliderRecommendation {
  weirdness: SliderRange;
  styleInfluence: SliderRange;
  note: string;
}

export type TemplateId = "hook-first" | "hook-first-beat-switch";

export type WarningLevel = "info" | "warn" | "block";

export interface EngineWarning {
  level: WarningLevel;
  message: string;
}

export interface BuildOptions {
  fusion: FusionSpec;
  build?: BuildType;
  template?: TemplateId;
  profile?: ProfileId;
  /** Elements the user wants banned; each triggers negative-to-positive inversion (research §5). */
  ban?: string[];
  bpm?: number;
}

export interface SunoPackage {
  styleText: string;
  excludeText: string;
  lyricsScaffold: string;
  sliders: SliderRecommendation;
  warnings: EngineWarning[];
  meta: {
    profile: ProfileId;
    template: TemplateId;
    build: BuildType;
    dominant: string;
    accents: { artist: string; weight: number }[];
    bpm: number;
  };
}
