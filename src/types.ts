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

/**
 * A mode is an era/side of an artist's catalog (e.g. gritty NYC vs
 * introspective vs bounce). Fields present override the base DNA's slots for
 * that roll; absent fields fall through to the base. A mode with no overrides
 * represents the base sound. Vocal identity intentionally stays in the base —
 * modes change the beat and palette, not who's rapping.
 */
export interface DnaMode {
  id: string;
  label: string;
  genres?: string[];
  mood?: string[];
  grooveExtras?: string[];
  instrumentation?: string[];
  texture?: string[];
  bpm?: [number, number];
}

/**
 * Slot arrays are DESCRIPTOR POOLS, not fixed emissions: the engine picks a
 * seeded subset each build so repeated builds vary wording while keeping the
 * vibe. Convention: index 0 of `instrumentation` and `vocal` is the signature
 * descriptor and is always emitted.
 */
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
  /**
   * Default exclude-field terms guarding this DNA's genre lane against known
   * drift attractors (research §5: community anti-drift practice, e.g. "no EDM
   * elements"). Emitted unless the build disables lane guards; user bans take
   * the exclude slots first.
   */
  laneGuards?: string[];
  /** Era/mode variants; when present the engine rolls (or pins) one per build. */
  modes?: DnaMode[];
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
  /** Default true: emit the dominant DNA's lane-guard exclusions (anti-drift, research §5). */
  laneGuards?: boolean;
  bpm?: number;
  /**
   * Variation seed. Same seed + same inputs = identical package; a different
   * seed picks a different combination from the same DNA descriptor pools —
   * same vibe, fresh prompt. Default 0.
   */
  seed?: number;
  /** Verse count for the structure template (default 2; 3 lengthens the track). */
  verses?: 2 | 3;
  /** Pin the dominant's DNA mode by id; omit to let the seed roll one. */
  mode?: string;
}

/** One entry of the lyrics structure; `slot` marks user-writable sections. */
export interface ScaffoldSection {
  /** Bracket tag emitted verbatim, e.g. "[Chorus]". */
  tag: string;
  /** Writable slot id; repeated slots (the hook) share one text. */
  slot?: "hook" | "verse1" | "verse2" | "verse3" | "breakdown" | "build" | "outro";
  /** Placeholder guidance shown when the slot is unwritten. */
  guide?: string;
  /** Suggested parenthetical ad-lib (research §4.5). */
  adlib?: string;
}

export interface SunoPackage {
  styleText: string;
  excludeText: string;
  lyricsScaffold: string;
  /** Structured form of the scaffold, for UIs that let the user write bars in place. */
  lyricsSections: ScaffoldSection[];
  /**
   * Bracket tags only, no lyric content — paste-ready structure control for
   * when Suno should handle the vocals/instrumental itself (research §4.1:
   * ordering is respected; §4.2: beat switches via Build/Drop/Breakdown).
   */
  lyricsTagsOnly: string;
  sliders: SliderRecommendation;
  warnings: EngineWarning[];
  meta: {
    profile: ProfileId;
    template: TemplateId;
    build: BuildType;
    dominant: string;
    accents: { artist: string; weight: number }[];
    bpm: number;
    seed: number;
    verses: 2 | 3;
    /** Label of the dominant's rolled/pinned mode, when the DNA has modes. */
    mode?: string;
  };
}
