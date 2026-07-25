import type {
  ArtistDNA,
  EngineWarning,
  ModelProfile,
  ScaffoldSection,
  TemplateId,
} from "./types.ts";

/**
 * Hit-structure templates → lyrics structure + text scaffold.
 *
 * Research grounding:
 * - §4.1: reliable-tier tags only ([Intro]/[Verse]/[Chorus]/[Outro]/[End];
 *   [Chorus] over [Hook]); ordering in the lyrics box is respected; a chorus
 *   recurs only if written again with identical lyrics.
 * - §4.6: hook-first = [Chorus] first; [Intro: Cold Open] suppresses the
 *   instrumental lead-in; always end [Outro] → [End]; ~2:30–2:50 ≈ 2 verses
 *   (6–8 lines) + 2–3 choruses (4 lines), no bridge, moderate-to-fast BPM.
 * - §4.2: beat switches via [Build]/[Drop]/[Breakdown] (no verified
 *   [Beat Switch] tag) plus a style-field note.
 * - §4.5: parenthetical ad-libs ≤3 words; §4.7 sparing caps/`!`.
 *
 * The structured `sections` form drives UIs that let the user write bars in
 * place; the text scaffold is the CLI rendering, with « » placeholders that
 * MUST be replaced before pasting.
 */
export interface ScaffoldResult {
  lyricsScaffold: string;
  sections: ScaffoldSection[];
  warnings: EngineWarning[];
  /** Extra phrase for the style field when the template needs one. */
  styleAddendum?: string;
}

const HOOK_GUIDE =
  "4 short hook lines — simple phrasing, heavy repetition, identical every time it appears";

function sectionsFor(template: TemplateId, adlib: string): ScaffoldSection[] {
  const hook: ScaffoldSection = { tag: "[Chorus]", slot: "hook", guide: HOOK_GUIDE, adlib };
  const hookRepeat: ScaffoldSection = { tag: "[Chorus]", slot: "hook", guide: HOOK_GUIDE, adlib };

  if (template === "hook-first") {
    return [
      { tag: "[Intro: Cold Open]" },
      hook,
      {
        tag: "[Verse 1]",
        slot: "verse1",
        guide: "6–8 bars — open with your hardest image; land punchlines on beat 4",
        adlib,
      },
      hookRepeat,
      {
        tag: "[Verse 2]",
        slot: "verse2",
        guide: "6–8 bars — escalate: denser rhymes or a flow switch in the last 4",
        adlib: "echo last words",
      },
      { ...hookRepeat },
      { tag: "[Outro]", slot: "outro", guide: "2 lines — strip back to one repeated phrase, let it decay" },
      { tag: "[End]" },
    ];
  }

  return [
    { tag: "[Intro: Cold Open]" },
    hook,
    { tag: "[Verse 1]", slot: "verse1", guide: "6–8 bars in the primary groove", adlib },
    hookRepeat,
    {
      tag: "[Breakdown]",
      slot: "breakdown",
      guide: "1–2 sparse lines — half the energy, space before the switch",
    },
    { tag: "[Build]", slot: "build", guide: "2 lines rising tension — shorter words, tighter rhythm" },
    { tag: "[Drop]" },
    {
      tag: "[Verse 2]",
      slot: "verse2",
      guide: "6–8 bars on the switched beat — new flow, same identity",
      adlib: "echo",
    },
    { ...hookRepeat },
    { tag: "[Outro]", slot: "outro", guide: "2 lines — one repeated phrase over the decaying beat" },
    { tag: "[End]" },
  ];
}

/** Render one section's placeholder body line (« » must be replaced before pasting). */
export function sectionPlaceholder(s: ScaffoldSection): string | undefined {
  if (!s.guide) return undefined;
  return `«${s.guide}»${s.adlib ? ` (${s.adlib})` : ""}`;
}

/**
 * Assemble final lyrics text from sections + user-written slot texts.
 * Slots without text fall back to their « » placeholder. Consecutive [Drop] +
 * section tags stay adjacent; sections are separated by blank lines.
 */
export function assembleLyrics(
  sections: ScaffoldSection[],
  slotTexts: Partial<Record<NonNullable<ScaffoldSection["slot"]>, string>> = {},
): string {
  const blocks: string[] = [];
  let pendingTag: string | undefined;
  for (const s of sections) {
    const body = (s.slot && slotTexts[s.slot]?.trim()) || sectionPlaceholder(s);
    const tag = pendingTag ? `${pendingTag}\n${s.tag}` : s.tag;
    pendingTag = undefined;
    if (!body) {
      // Bare tags with no body: [Drop] glues to the next section; terminal tags
      // ([End]) and standalone intros stand alone.
      if (s.tag === "[Drop]") {
        pendingTag = tag;
        continue;
      }
      blocks.push(tag);
      continue;
    }
    blocks.push(`${tag}\n${body}`);
  }
  if (pendingTag) blocks.push(pendingTag);
  return blocks.join("\n\n").replace(/\n\n\[End\]$/, "\n[End]");
}

export function buildScaffold(
  template: TemplateId,
  dominant: ArtistDNA,
  profile: ModelProfile,
  bpm: number,
): ScaffoldResult {
  const warnings: EngineWarning[] = [];
  const adlib = dominant.adlibs?.[0] ?? "yeah";
  const flowNote = dominant.lyricNotes?.[0] ?? "keep bar cadence consistent with the groove";
  const sections = sectionsFor(template, adlib);

  const header = [
    `// CIPHER scaffold — ${template} @ ${bpm} BPM (delete these // lines before pasting)`,
    `// Flow guide: ${flowNote}`,
    "// Ad-libs: parentheses = background vocals; keep them 1–3 words (research §4.5).",
    "// Emphasis: ALL CAPS + ! = shouted attack, and it bleeds into following lines — use on 1–3 words max (research §4.7).",
    "// Every «placeholder» must be replaced, or Suno sings it.",
    "",
  ].join("\n");

  const scaffold = header + assembleLyrics(sections);

  if (scaffold.length > profile.lyricsCharLimit) {
    warnings.push({
      level: "warn",
      message: `Scaffold exceeds the ${profile.lyricsCharLimit}-char lyrics limit — shorten before pasting.`,
    });
  }

  return {
    lyricsScaffold: scaffold,
    sections,
    warnings,
    styleAddendum:
      template === "hook-first-beat-switch"
        ? "dramatic mid-song beat switch from minimal to explosive"
        : undefined,
  };
}
