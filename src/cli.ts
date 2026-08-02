import { parseArgs } from "node:util";
import { buildPackage } from "./engine.ts";
import { ARTISTS } from "./artists.ts";
import type { BuildType, ProfileId, TemplateId } from "./types.ts";

const HELP = `CIPHER — Suno prompt engine

Usage:
  npm run cipher -- --dominant <artist> [options]
  npm run cipher -- --list-artists

Options:
  --dominant <id>        Dominant artist DNA (id, name, or alias). Required.
  --accent <id[:w]>      Accent DNA with optional weight 0-1 (default 0.3). Repeatable.
  --build <type>         faithful | balanced | fusion (default: fusion if accents, else faithful)
  --template <id>        hook-first | hook-first-beat-switch (default: hook-first)
  --profile <id>         v4.5-all | v5 | v5.5 (default: v5.5)
  --ban <element>        Element to exclude; triggers negative-to-positive inversion. Repeatable.
  --bpm <n>              BPM override.
  --seed <n>             Variation seed: same seed = same package, new seed = same vibe, fresh wording.
  --mode <id>            Pin the dominant's DNA mode (see --list-artists); omit to roll one.
  --instrumental         Beat-only: no vocal language, anti-vocal excludes, instrumental tag structure.
  --verses <2|3>         Verse count (default 2; 3 lengthens the track).
  --tags-only            Print the bracket-tags-only lyrics (structure control, no bars).
  --json                 Emit the package as JSON.

Examples:
  npm run cipher -- --dominant xxxtentacion --accent travis-scott:0.4
  npm run cipher -- --dominant ti --accent weezer:0.35 --template hook-first-beat-switch
  npm run cipher -- --dominant juice-wrld --ban saxophone --ban edm
`;

function main(): void {
  const { values } = parseArgs({
    options: {
      dominant: { type: "string" },
      accent: { type: "string", multiple: true },
      build: { type: "string" },
      template: { type: "string" },
      profile: { type: "string" },
      ban: { type: "string", multiple: true },
      bpm: { type: "string" },
      seed: { type: "string" },
      mode: { type: "string" },
      instrumental: { type: "boolean" },
      verses: { type: "string" },
      "tags-only": { type: "boolean" },
      json: { type: "boolean" },
      "list-artists": { type: "boolean" },
      help: { type: "boolean" },
    },
  });

  if (values.help || (!values.dominant && !values["list-artists"])) {
    console.log(HELP);
    return;
  }

  if (values["list-artists"]) {
    for (const a of ARTISTS) {
      const modes = a.modes?.length ? `  [modes: ${a.modes.map((m) => m.id).join(", ")}]` : "";
      console.log(`${a.id.padEnd(14)} ${a.displayName.padEnd(16)} ${a.genres.join(" / ")}${modes}`);
    }
    return;
  }

  const accents = (values.accent ?? []).map((spec) => {
    const [artist, w] = spec.split(":");
    return { artist: artist!, weight: w ? Number(w) : undefined };
  });

  const pkg = buildPackage({
    fusion: { dominant: values.dominant!, accents },
    build: values.build as BuildType | undefined,
    template: values.template as TemplateId | undefined,
    profile: values.profile as ProfileId | undefined,
    ban: values.ban,
    bpm: values.bpm ? Number(values.bpm) : undefined,
    seed: values.seed ? Number(values.seed) : undefined,
    mode: values.mode,
    instrumental: values.instrumental,
    verses: values.verses === "3" ? 3 : 2,
  });

  if (values.json) {
    console.log(JSON.stringify(pkg, null, 2));
    return;
  }

  const { sliders, meta } = pkg;
  const line = "─".repeat(64);
  console.log(line);
  console.log(
    `CIPHER package  ·  ${meta.dominant}${meta.mode ? ` (${meta.mode})` : ""}${meta.accents.length ? " + " + meta.accents.map((a) => `${a.artist}(${a.weight})`).join(" + ") : ""}  ·  ${meta.build} / ${meta.template}  ·  ${meta.profile}  ·  ${meta.bpm} BPM`,
  );
  console.log(line);
  console.log("\n▌STYLE FIELD — paste into “Styles”\n");
  console.log(pkg.styleText);
  console.log("\n▌EXCLUDE FIELD — paste into “Exclude Styles”\n");
  console.log(pkg.excludeText || "(leave empty)");
  if (pkg.meta.instrumental) {
    console.log(
      "\n▌LYRICS (instrumental structure) — paste as-is, and ALSO flip Suno's Instrumental toggle\n",
    );
    console.log(pkg.lyricsTagsOnly);
  } else if (values["tags-only"]) {
    console.log("\n▌LYRICS (tags only) — paste as-is; Suno fills in around the structure\n");
    console.log(pkg.lyricsTagsOnly);
  } else {
    console.log("\n▌LYRICS — fill every «placeholder», delete // lines, then paste\n");
    console.log(pkg.lyricsScaffold);
  }
  console.log(`\n▌SLIDERS (Safe↔Chaos / Loose↔Strong scales are unnumbered — eyeball the range)\n`);
  console.log(`  Weirdness:        ~${sliders.weirdness.min}–${sliders.weirdness.max}%`);
  console.log(`  Style Influence:  ~${sliders.styleInfluence.min}–${sliders.styleInfluence.max}%`);
  console.log(`  ${sliders.note}`);
  if (pkg.warnings.length) {
    console.log(`\n▌NOTES\n`);
    for (const w of pkg.warnings) console.log(`  [${w.level}] ${w.message}`);
  }
  console.log();
}

main();
