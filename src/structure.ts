import type { ArtistDNA, EngineWarning, ModelProfile, TemplateId } from "./types.ts";

/**
 * Hit-structure templates → lyrics-box scaffold.
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
 * Placeholders use « » markers — everything inside them MUST be replaced with
 * real lyrics before pasting, or Suno will sing the placeholder.
 */
export interface ScaffoldResult {
  lyricsScaffold: string;
  warnings: EngineWarning[];
  /** Extra phrase for the style field when the template needs one. */
  styleAddendum?: string;
}

function hookBlock(adlib: string): string {
  return [
    "[Chorus]",
    `«4 short hook lines — simple phrasing, heavy repetition, identical every time it appears» (${adlib})`,
  ].join("\n");
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
  const hook = hookBlock(adlib);

  const header = [
    `// CIPHER scaffold — ${template} @ ${bpm} BPM (delete these // lines before pasting)`,
    `// Flow guide: ${flowNote}`,
    "// Ad-libs: parentheses = background vocals; keep them 1–3 words (research §4.5).",
    "// Emphasis: ALL CAPS + ! = shouted attack, and it bleeds into following lines — use on 1–3 words max (research §4.7).",
    "// Every «placeholder» must be replaced, or Suno sings it.",
    "",
  ].join("\n");

  let body: string;
  if (template === "hook-first") {
    body = [
      "[Intro: Cold Open]",
      hook,
      "",
      "[Verse 1]",
      `«6–8 bars — open with your hardest image; land punchlines on beat 4» (${adlib})`,
      "",
      hook,
      "",
      "[Verse 2]",
      `«6–8 bars — escalate: denser rhymes or a flow switch in the last 4» («echo last words»)`,
      "",
      hook,
      "",
      "[Outro]",
      "«2 lines — strip back to one repeated phrase, let it decay»",
      "[End]",
    ].join("\n");
  } else {
    body = [
      "[Intro: Cold Open]",
      hook,
      "",
      "[Verse 1]",
      `«6–8 bars in the primary groove» (${adlib})`,
      "",
      hook,
      "",
      "[Breakdown]",
      "«1–2 sparse lines — half the energy, space before the switch»",
      "",
      "[Build]",
      "«2 lines rising tension — shorter words, tighter rhythm»",
      "",
      "[Drop]",
      "[Verse 2]",
      "«6–8 bars on the switched beat — new flow, same identity» («echo»)",
      "",
      hook,
      "",
      "[Outro]",
      "«2 lines — one repeated phrase over the decaying beat»",
      "[End]",
    ].join("\n");
  }

  const scaffold = header + body;

  if (scaffold.length > profile.lyricsCharLimit) {
    warnings.push({
      level: "warn",
      message: `Scaffold exceeds the ${profile.lyricsCharLimit}-char lyrics limit — shorten before pasting.`,
    });
  }

  return {
    lyricsScaffold: scaffold,
    warnings,
    styleAddendum:
      template === "hook-first-beat-switch"
        ? "dramatic mid-song beat switch from minimal to explosive"
        : undefined,
  };
}
