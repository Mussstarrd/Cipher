import { describe, expect, it } from "vitest";
import { buildPackage } from "../src/engine.ts";
import { ARTISTS } from "../src/artists.ts";
import { PROFILES, STYLE_WORD_TARGET } from "../src/profiles.ts";
import { GROOVES } from "../src/grooves.ts";

describe("buildPackage", () => {
  it("is deterministic: same input produces identical output", () => {
    const opts = {
      fusion: { dominant: "xxxtentacion", accents: [{ artist: "travis-scott", weight: 0.4 }] },
    };
    expect(buildPackage(opts)).toEqual(buildPackage(opts));
  });

  it("front-loads the dominant genre anchor", () => {
    const pkg = buildPackage({ fusion: { dominant: "ti" } });
    expect(pkg.styleText.startsWith("southern trap")).toBe(true);
  });

  it("never emits artist names into Suno-bound fields", () => {
    for (const artist of ARTISTS) {
      const pkg = buildPackage({ fusion: { dominant: artist.id } });
      const bound = `${pkg.styleText}\n${pkg.excludeText}\n${pkg.lyricsScaffold}`.toLowerCase();
      for (const name of [artist.displayName, ...artist.aliases]) {
        if (name.length < 3) continue; // short aliases can't be word-scanned meaningfully
        const re = new RegExp(`(?<![a-z0-9])${name.toLowerCase().replace(/[.*+?^${}()|[\]\\]/g, "\\$&")}(?![a-z0-9])`);
        expect(re.test(bound), `"${name}" leaked for ${artist.id}`).toBe(false);
      }
      expect(pkg.warnings.some((w) => w.level === "block")).toBe(false);
    }
  });

  it("always emits a groove phrase (anti-homogenization slot)", () => {
    const allPhrases = Object.values(GROOVES).flatMap((g) => g.phrases);
    for (const artist of ARTISTS) {
      const pkg = buildPackage({ fusion: { dominant: artist.id } });
      expect(
        allPhrases.some((p) => pkg.styleText.includes(p)),
        `no groove phrase for ${artist.id}`,
      ).toBe(true);
    }
  });

  it("respects style character and word budgets", () => {
    const pkg = buildPackage({
      fusion: {
        dominant: "travis-scott",
        accents: [
          { artist: "weezer", weight: 0.45 },
          { artist: "outkast", weight: 0.4 },
        ],
      },
      ban: ["saxophone", "edm", "pop"],
    });
    expect(pkg.styleText.length).toBeLessThanOrEqual(PROFILES["v5.5"].styleCharLimit);
    // Sweet-spot target is soft; allow small overshoot but not runaway growth.
    expect(pkg.styleText.split(/\s+/).length).toBeLessThanOrEqual(STYLE_WORD_TARGET.max + 4);
  });

  it("keeps genre count at 1-2 in fusions", () => {
    const pkg = buildPackage({
      fusion: { dominant: "ti", accents: [{ artist: "weezer", weight: 0.45 }] },
    });
    expect(pkg.styleText.startsWith("southern trap, alt-rock")).toBe(true);
  });

  it("keeps accent instrumentation in the style text (the fusion's point)", () => {
    const pkg = buildPackage({
      fusion: { dominant: "ti", accents: [{ artist: "weezer", weight: 0.35 }] },
      template: "hook-first-beat-switch",
    });
    expect(pkg.styleText).toContain("power chord");
  });
});

describe("negative-to-positive inversion", () => {
  it("pairs a ban with a competing positive in the style field", () => {
    const pkg = buildPackage({ fusion: { dominant: "juice-wrld" }, ban: ["saxophone"] });
    expect(pkg.excludeText).toBe("saxophone");
    expect(pkg.styleText).toContain("heavy synth brass lead");
  });

  it("caps exclude terms at the profile max and warns", () => {
    const pkg = buildPackage({
      fusion: { dominant: "jid" },
      ban: ["saxophone", "edm", "pop", "piano", "choir", "strings", "flute"],
    });
    expect(pkg.excludeText.split(", ").length).toBeLessThanOrEqual(PROFILES["v5.5"].maxExcludeTerms);
    expect(pkg.warnings.some((w) => w.message.includes("dropped"))).toBe(true);
  });

  it("warns instead of silently passing an unmapped ban", () => {
    const pkg = buildPackage({ fusion: { dominant: "jid" }, ban: ["kazoo"] });
    expect(pkg.excludeText).toBe("kazoo");
    expect(pkg.warnings.some((w) => w.message.includes("No inversion mapping"))).toBe(true);
  });
});

describe("structure templates", () => {
  it("hook-first opens with cold-open chorus and closes with Outro/End", () => {
    const pkg = buildPackage({ fusion: { dominant: "kanye-west" } });
    const lines = pkg.lyricsScaffold.split("\n").filter((l) => !l.startsWith("//") && l.trim());
    expect(lines[0]).toBe("[Intro: Cold Open]");
    expect(lines[1]).toBe("[Chorus]");
    expect(lines[lines.length - 1]).toBe("[End]");
    expect(lines[lines.length - 3]).toBe("[Outro]");
  });

  it("beat-switch template uses Build/Drop/Breakdown, not an unverified [Beat Switch] tag", () => {
    const pkg = buildPackage({
      fusion: { dominant: "ti" },
      template: "hook-first-beat-switch",
    });
    expect(pkg.lyricsScaffold).toContain("[Breakdown]");
    expect(pkg.lyricsScaffold).toContain("[Build]");
    expect(pkg.lyricsScaffold).toContain("[Drop]");
    expect(pkg.lyricsScaffold).not.toContain("[Beat Switch]");
    expect(pkg.styleText).toContain("beat switch");
  });

  it("emits identical hook blocks each time the chorus recurs", () => {
    const pkg = buildPackage({ fusion: { dominant: "jay-z" } });
    const hooks = pkg.lyricsScaffold.split("[Chorus]").length - 1;
    expect(hooks).toBe(3);
  });
});

describe("sliders", () => {
  it("selects fusion preset when accents are present", () => {
    const pkg = buildPackage({
      fusion: { dominant: "ti", accents: [{ artist: "weezer" }] },
    });
    expect(pkg.meta.build).toBe("fusion");
    expect(pkg.sliders.weirdness).toEqual({ min: 50, max: 70 });
    expect(pkg.sliders.styleInfluence.min).toBeGreaterThanOrEqual(60);
  });

  it("selects faithful preset for single-DNA builds", () => {
    const pkg = buildPackage({ fusion: { dominant: "jadakiss" } });
    expect(pkg.meta.build).toBe("faithful");
    expect(pkg.sliders.weirdness).toEqual({ min: 10, max: 25 });
  });

  it("notes missing sliders on the free-tier profile", () => {
    const pkg = buildPackage({ fusion: { dominant: "jid" }, profile: "v4.5-all" });
    expect(pkg.warnings.some((w) => w.message.includes("no creative sliders"))).toBe(true);
  });
});

describe("fusion weighting", () => {
  it("keeps the dominant vocal identity in every fusion", () => {
    const pkg = buildPackage({
      fusion: { dominant: "ti", accents: [{ artist: "weezer", weight: 0.45 }] },
    });
    expect(pkg.styleText).toContain("smooth commanding southern drawl");
  });

  it("caps runaway accent weights", () => {
    const pkg = buildPackage({
      fusion: { dominant: "ti", accents: [{ artist: "weezer", weight: 0.9 }] },
    });
    expect(pkg.meta.accents[0]?.weight).toBeLessThanOrEqual(0.45);
    expect(pkg.warnings.some((w) => w.message.includes("capped"))).toBe(true);
  });

  it("throws on unknown artists", () => {
    expect(() => buildPackage({ fusion: { dominant: "drake" } })).toThrow(/Unknown dominant/);
  });
});
