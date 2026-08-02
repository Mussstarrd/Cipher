import { describe, expect, it } from "vitest";
import { buildPackage } from "../src/engine.ts";
import { ARTISTS } from "../src/artists.ts";
import { PROFILES, STYLE_WORD_TARGET } from "../src/profiles.ts";
import { GROOVES } from "../src/grooves.ts";

describe("buildPackage", () => {
  it("is deterministic: same input and seed produce identical output", () => {
    const opts = {
      fusion: { dominant: "xxxtentacion", accents: [{ artist: "travis-scott", weight: 0.4 }] },
      seed: 42,
    };
    expect(buildPackage(opts)).toEqual(buildPackage(opts));
  });

  it("ten seeds on the same fusion give at least nine distinct prompts", () => {
    const texts = new Set<string>();
    for (let seed = 1; seed <= 10; seed++) {
      texts.add(
        buildPackage({
          fusion: { dominant: "jid", accents: [{ artist: "jay-z", weight: 0.3 }] },
          seed,
        }).styleText,
      );
    }
    expect(texts.size).toBeGreaterThanOrEqual(9);
  });

  it("a different seed rerolls wording but keeps the identity anchors", () => {
    const base = { fusion: { dominant: "ti" }, mode: "trap-anthem" };
    const a = buildPackage({ ...base, seed: 1 });
    const b = buildPackage({ ...base, seed: 2 });
    expect(a.styleText).not.toBe(b.styleText);
    for (const pkg of [a, b]) {
      expect(pkg.styleText.startsWith("southern trap")).toBe(true);
      expect(pkg.styleText).toContain("live orchestral brass stabs over 808 knock"); // signature
      expect(pkg.styleText).toContain("smooth commanding southern drawl"); // vocal signature
    }
  });

  it("modes: pinning selects the era, rolling varies it, vocal identity persists", () => {
    const gritty = buildPackage({ fusion: { dominant: "jay-z" }, mode: "gritty-nyc" });
    const intro = buildPackage({ fusion: { dominant: "jay-z" }, mode: "introspective" });
    const bounce = buildPackage({ fusion: { dominant: "jay-z" }, mode: "bounce" });
    expect(gritty.styleText.startsWith("gritty New York rap")).toBe(true);
    expect(intro.styleText.startsWith("reflective East Coast rap")).toBe(true);
    expect(bounce.styleText.startsWith("2000s East Coast club rap")).toBe(true);
    for (const pkg of [gritty, intro, bounce]) {
      expect(pkg.styleText).toContain("laid-back commanding rap flow");
    }
    // Rolling (no pin) reaches more than one mode across seeds.
    const modes = new Set<string>();
    for (let seed = 1; seed <= 12; seed++) {
      modes.add(buildPackage({ fusion: { dominant: "jay-z" }, seed }).meta.mode ?? "");
    }
    expect(modes.size).toBeGreaterThan(1);
  });

  it("front-loads the dominant genre anchor", () => {
    const pkg = buildPackage({ fusion: { dominant: "ti" }, mode: "trap-anthem" });
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
      mode: "trap-anthem",
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
    expect(pkg.excludeText.startsWith("saxophone")).toBe(true);
    expect(pkg.styleText).toContain("heavy synth brass lead");
  });

  it("emits the dominant's lane guards by default, user bans first", () => {
    const pkg = buildPackage({ fusion: { dominant: "ti" }, ban: ["saxophone"] });
    expect(pkg.excludeText).toBe("saxophone, edm drop, country");
  });

  it("omits lane guards when disabled", () => {
    const pkg = buildPackage({ fusion: { dominant: "ti" }, laneGuards: false });
    expect(pkg.excludeText).toBe("");
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
    expect(pkg.excludeText.startsWith("kazoo")).toBe(true);
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

describe("production polish", () => {
  it("appends specific engineering polish phrases, skipping overlaps", async () => {
    const { POLISH_POOL } = await import("../src/polish.ts");
    const pkg = buildPackage({ fusion: { dominant: "jay-z" }, seed: 1 });
    const present = POLISH_POOL.filter((p) => pkg.styleText.includes(p));
    expect(present.length).toBeGreaterThanOrEqual(1);
    // Never the debunked generic praise words (research §11).
    expect(pkg.styleText).not.toMatch(/studio quality|high quality/i);
  });

  it("filters polish that repeats wording already in the DNA", async () => {
    const { pickPolish } = await import("../src/polish.ts");
    const slots = {
      genre: ["trap"],
      mood: [],
      groove: [],
      instrumentation: ["crisp trap hi-hat rolls", "punchy snappy drums"],
      vocal: [],
      texture: ["warm analog warmth"],
    };
    for (let seed = 0; seed < 20; seed++) {
      const picks = pickPolish(slots, seed);
      expect(picks).not.toContain("crisp highs");
      expect(picks).not.toContain("punchy low end");
      expect(picks).not.toContain("analog warmth");
    }
  });
});

describe("instrumental (beat-only) builds", () => {
  it("emits zero performed-vocal language across every DNA and seed sample", async () => {
    const { allowedInInstrumental } = await import("../src/fusion.ts");
    for (const artist of ARTISTS) {
      for (const seed of [0, 7, 99]) {
        const pkg = buildPackage({ fusion: { dominant: artist.id }, instrumental: true, seed });
        for (const part of pkg.styleText.split(", ")) {
          expect(allowedInInstrumental(part), `"${part}" (${artist.id}, seed ${seed})`).toBe(true);
        }
        for (const v of artist.vocal) {
          expect(pkg.styleText).not.toContain(v);
        }
      }
    }
  });

  it("keeps vocal-sample descriptors — the subtle soul-sample exception", async () => {
    const { allowedInInstrumental } = await import("../src/fusion.ts");
    expect(allowedInInstrumental("pitched-down chopped vocal hook")).toBe(true);
    expect(allowedInInstrumental("soulful vocal sample chops")).toBe(true);
    expect(allowedInInstrumental("gospel choir swells")).toBe(false);
    expect(allowedInInstrumental("airy backing-vocal harmonies")).toBe(false);
    expect(allowedInInstrumental("vocal-forward mix")).toBe(false);
  });

  it("spends the exclude budget on vocal suppression", () => {
    const pkg = buildPackage({ fusion: { dominant: "ti" }, instrumental: true });
    expect(pkg.excludeText.startsWith("vocals, singing, lyrics, humming")).toBe(true);
    expect(pkg.excludeText).not.toContain("edm drop"); // lane guards yield their slots
  });

  it("emits an instrumental tag structure with no song-section tags", () => {
    const pkg = buildPackage({
      fusion: { dominant: "travis-scott" },
      instrumental: true,
      template: "hook-first-beat-switch",
    });
    expect(pkg.lyricsTagsOnly.startsWith("[Instrumental]")).toBe(true);
    expect(pkg.lyricsTagsOnly).not.toContain("[Verse");
    expect(pkg.lyricsTagsOnly).not.toContain("[Chorus]");
    expect(pkg.lyricsTagsOnly).toContain("[Drop]");
    expect(pkg.lyricsTagsOnly.endsWith("[End]")).toBe(true);
  });
});

describe("tags-only lyrics", () => {
  it("emits bracket tags only, in order, with Drop glued", () => {
    const pkg = buildPackage({
      fusion: { dominant: "ugk" },
      template: "hook-first-beat-switch",
    });
    const lines = pkg.lyricsTagsOnly.split("\n").filter((l) => l.trim());
    expect(lines.every((l) => /^\[.*\]$/.test(l))).toBe(true);
    expect(pkg.lyricsTagsOnly).toContain("[Drop]\n[Verse 2]");
    expect(lines[0]).toBe("[Intro: Cold Open]");
    expect(lines[lines.length - 1]).toBe("[End]");
  });

  it("three-verse option adds [Verse 3] and another hook", () => {
    const pkg = buildPackage({ fusion: { dominant: "ugk" }, verses: 3 });
    expect(pkg.lyricsTagsOnly).toContain("[Verse 3]");
    expect(pkg.lyricsTagsOnly.split("[Chorus]").length - 1).toBe(4);
  });
});

describe("phase 3 tuning", () => {
  it("never puts funk in a genre slot (genre tokens are takeover attractors)", () => {
    for (const artist of ARTISTS) {
      const genrePools = [artist.genres, ...(artist.modes ?? []).map((m) => m.genres ?? [])];
      for (const g of genrePools.flat()) {
        expect(/funk/i.test(g), `"${g}" in ${artist.id} genres`).toBe(false);
      }
    }
    // Funk-colored DNAs keep an explicit rap identity in the vocal signature
    // and guard against funk-band takeover in the exclude field.
    for (const id of ["ugk", "outkast"]) {
      const pkg = buildPackage({ fusion: { dominant: id } });
      expect(pkg.styleText).toMatch(/rap/i);
      expect(pkg.excludeText).toContain("disco");
    }
  });

  it("ugk resolves by alias and keeps country out of its lane guards", () => {
    const pkg = buildPackage({ fusion: { dominant: "pimp c" }, mode: "classic-slab" });
    expect(pkg.meta.dominant).toBe("ugk");
    expect(pkg.excludeText).not.toContain("country");
    expect(pkg.styleText.startsWith("Texas southern rap")).toBe(true);
  });

  it("jay-z guards against the jazzy drift it was producing", () => {
    const pkg = buildPackage({ fusion: { dominant: "jay-z" } });
    expect(pkg.styleText).not.toContain("boom bap");
    expect(pkg.excludeText).toContain("smooth jazz");
    expect(pkg.excludeText).toContain("lo-fi chillhop");
  });

  it("new DNAs resolve by alias and anchor rap-first", () => {
    const cases: [string, string][] = [
      ["gates", "Baton Rouge street rap"],
      ["chance", "psychedelic Chicago rap"],
      ["drizzy", "moody Toronto rap"],
      ["gampo", "rowdy midwest party rap"],
    ];
    for (const [alias, genre] of cases) {
      const pkg = buildPackage({ fusion: { dominant: alias }, mode: "pain" });
      expect(pkg.styleText.startsWith(genre), `${alias} → ${genre}`).toBe(true);
    }
    expect(() => buildPackage({ fusion: { dominant: "bobby ray" } })).toThrow(/Unknown/);
  });

  it("drake DNA carries the muffled filtered-beat identity", () => {
    const pkg = buildPackage({ fusion: { dominant: "drake" } });
    expect(pkg.styleText).toContain("muffled low-pass filtered beat");
    expect(pkg.meta.bpm).toBeGreaterThanOrEqual(70);
    expect(pkg.meta.bpm).toBeLessThanOrEqual(100);
  });

  it("anti-cartoon lane guards hold for the crossover-risk DNAs", () => {
    expect(buildPackage({ fusion: { dominant: "chance" } }).excludeText).toContain("smooth jazz");
    expect(buildPackage({ fusion: { dominant: "kanye-west" } }).excludeText).toContain(
      "high-pitched cartoon vocals",
    );
  });

  it("kanye no longer leads with chipmunk-pitched samples", () => {
    const pkg = buildPackage({ fusion: { dominant: "kanye-west" } });
    expect(pkg.styleText).not.toContain("chipmunk");
    expect(pkg.excludeText).toContain("high-pitched cartoon vocals");
  });
});

describe("lyrics assembly", () => {
  it("fills written slots and repeats the hook identically", async () => {
    const { assembleLyrics } = await import("../src/structure.ts");
    const pkg = buildPackage({ fusion: { dominant: "ti" } });
    const text = assembleLyrics(pkg.lyricsSections, {
      hook: "King talk, we don't fold (fold)",
      verse1: "Line one\nLine two",
    });
    expect(text.split("King talk, we don't fold (fold)").length - 1).toBe(3);
    expect(text).toContain("[Verse 1]\nLine one\nLine two");
    // Unwritten slots keep their « » placeholders so gaps are visible.
    expect(text).toContain("«6–8 bars — escalate");
    expect(text.endsWith("[End]")).toBe(true);
  });

  it("glues [Drop] to the switched verse in the beat-switch template", async () => {
    const { assembleLyrics } = await import("../src/structure.ts");
    const pkg = buildPackage({ fusion: { dominant: "ti" }, template: "hook-first-beat-switch" });
    const text = assembleLyrics(pkg.lyricsSections, { verse2: "Switch flow here" });
    expect(text).toContain("[Drop]\n[Verse 2]\nSwitch flow here");
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
    expect(() => buildPackage({ fusion: { dominant: "nas" } })).toThrow(/Unknown dominant/);
  });
});
