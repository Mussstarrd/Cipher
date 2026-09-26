"use strict";
const test = require("node:test");
const assert = require("node:assert");
const E = require("../src/engine.js");

const EDGES = Object.keys(E.EDGES);
const TEMPLATES = Object.keys(E.TEMPLATES);

function* everyCombo() {
  for (const lane of E.LANES) {
    yield* laneCombos(lane);
  }
}
function* laneCombos(lane) {
  {
    for (const edge of EDGES) {
      for (const template of TEMPLATES) {
        for (let seed = 1; seed <= 12; seed++) {
          const blend = E.LANES[(seed * 7) % E.LANES.length].id;
          yield {
            lane: lane.id, edge, template, seed,
            blend: seed % 3 === 0 ? blend : undefined,
            chordsInTags: seed % 2 === 0, postHook: seed % 4 === 0,
            vocal: ["auto", "male", "female", "duet"][seed % 4],
            plan: seed % 5 === 0 ? "loop" : undefined,
          };
        }
      }
    }
  }
}

test("same inputs give the same package", () => {
  const input = { lane: "drill", edge: "twitch", seed: 42 };
  assert.deepStrictEqual(E.generate(input), E.generate(input));
});

test("no banned vocabulary ever reaches the style text or section tags", () => {
  for (const input of everyCombo()) {
    const out = E.generate(input);
    const hits = [...E.lint(out.styleText, "style"), ...E.lint(out.lyricsTagsOnly, "tags")];
    assert.deepStrictEqual(hits, [], `${JSON.stringify(input)}\n${out.styleText}\n${out.lyricsTagsOnly}`);
  }
});

test("style stays inside limits and the descriptor sweet spot", () => {
  for (const input of everyCombo()) {
    const out = E.generate(input);
    assert.ok(out.styleText.length <= 1000, out.styleText);
    assert.ok(out.meta.descriptors <= 14 && out.meta.descriptors >= 8, `${out.meta.descriptors}: ${out.styleText}`);
    assert.match(out.styleText, /\d+ BPM/);
  }
});

test("every hard ban is always excluded, extras appended once", () => {
  const out = E.generate({ lane: "pluggnb", seed: 3, extraBans: ["airhorn", "Jazz", "airhorn"] });
  for (const ban of E.HARD_BANS) assert.ok(out.excludeText.split(", ").includes(ban), ban);
  assert.strictEqual(out.excludeText.split(", ").filter((b) => b === "airhorn").length, 1);
  assert.strictEqual(out.excludeText.split(", ").filter((b) => b === "jazz").length, 1);
});

test("chords are spelled with one accidental family per key", () => {
  for (const input of everyCombo()) {
    const { chords, key } = E.generate(input).harmony;
    const roots = [key.split(" ")[0], ...chords].join(" ");
    assert.ok(!(/#/.test(roots) && /[A-G]b/.test(roots)), `${key}: ${chords.join(" ")}`);
  }
});

test("known keys spell correctly", () => {
  const out = E.generate({ lane: "dark-trap", progression: "climb", key: "F# minor", seed: 1 });
  assert.deepStrictEqual(out.harmony.chords, ["F#m", "D", "A", "E"]);
  const rnb = E.generate({ lane: "slow-jam", progression: "backdoor", key: "Eb major", seed: 1 });
  assert.deepStrictEqual(rnb.harmony.chords, ["Abm7", "Db9", "Ebmaj7"]);
  const phr = E.generate({ lane: "drill", progression: "phrygian", key: "A# minor", seed: 1 });
  assert.deepStrictEqual(phr.harmony.chords, ["A#m", "B"]);
});

test("the hook repeats at least three times and the song ends on [End]", () => {
  for (const input of everyCombo()) {
    const out = E.generate(input);
    const hooks = out.sections.filter((s) => s.slot === "hook").length;
    assert.ok(hooks >= 3, `${hooks} hooks in ${input.template}`);
    assert.strictEqual(out.sections.at(-1).tag, "[End]");
  }
});

test("written bars replace every repeat of their slot", () => {
  const out = E.generate({ lane: "dark-trap", seed: 9, template: "hook-first" });
  const text = E.renderSections(out.sections, { hook: "run it up\nrun it up" });
  assert.strictEqual(text.split("run it up\nrun it up").length - 1, 3);
});

test("targets the Suno v6 family and tunes sliders per model", () => {
  assert.deepStrictEqual(Object.keys(E.PROFILES), ["v6", "v6-wild", "v6-mini"]);
  const base = { lane: "rage", edge: "experimental", seed: 5 };
  const v6 = E.generate(base);
  const wild = E.generate({ ...base, profile: "v6-wild" });
  assert.strictEqual(v6.meta.profile, "v6");
  assert.deepStrictEqual(wild.sliders.weirdness, v6.sliders.weirdness.map((v) => v - 15));
  assert.ok(wild.sliders.styleInfluence.every((v) => v <= 100));
  assert.strictEqual(v6.sliders.variety, "Off");
  assert.match(v6.styleText, /hard-stops after the last hook/);
});

test("instrumental mode has no vocal words anywhere and excludes vocals", () => {
  const vocalWords = /\b(vocal|vocalist|vocals|rap\b|rapper|sing|sung|singing|chant|ad-lib|lyric|verse\s+flow|falsetto|harmonies)/i;
  for (const input of everyCombo()) {
    const out = E.generate({ ...input, instrumental: true });
    assert.ok(!vocalWords.test(out.styleText), out.styleText);
    assert.ok(!vocalWords.test(out.lyricsTagsOnly), out.lyricsTagsOnly);
    assert.match(out.styleText, /purely instrumental beat/);
    for (const b of ["vocals", "rap", "singing"]) assert.ok(out.excludeText.split(", ").includes(b));
    assert.ok(out.sections.filter((s) => s.name === "Hook").length >= 3);
    assert.deepStrictEqual([...E.lint(out.styleText, "style"), ...E.lint(out.lyricsTagsOnly, "tags")], []);
  }
});

test("every likeness works on every lane and never leaks a name", () => {
  const names = E.LIKENESS.flatMap((l) => [l.name, ...l.aliases]).map((n) => n.toLowerCase());
  for (const like of E.LIKENESS) {
    assert.ok(E.LANES.some((l) => l.id === like.homeLane), `${like.id} home lane`);
    for (const lane of E.LANES) {
      for (const edge of Object.keys(E.EDGES)) {
        for (let seed = 1; seed <= 4; seed++) {
          for (const instrumental of [false, true]) {
            const out = E.generate({ lane: lane.id, likeness: like.id, edge, seed, instrumental, postHook: seed === 2 });
            const text = `${out.styleText}\n${out.lyricsTagsOnly}\n${JSON.stringify(out.blueprint)}`.toLowerCase();
            for (const n of names) {
              if (n.replace(/[^a-z0-9]/g, "").length < 3) continue;
              assert.ok(!new RegExp(`(^|[^a-z0-9])${n.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")}($|[^a-z0-9])`).test(text), `${like.id}: "${n}" leaked\n${text}`);
            }
            assert.deepStrictEqual([...E.lint(out.styleText, "style"), ...E.lint(out.lyricsTagsOnly, "tags")], [], out.styleText);
            assert.ok(out.meta.descriptors <= 14, `${out.meta.descriptors}: ${out.styleText}`);
            assert.ok(out.styleText.length <= 1000);
            assert.strictEqual(out.meta.likeness, like.id);
            if (!instrumental) assert.ok(out.styleText.includes(like.signature), out.styleText);
          }
        }
      }
    }
  }
});

test("the name lint catches an artist name in free text", () => {
  assert.ok(E.lint("dark trap like Drake", "style").some((w) => w.level === "block"));
  assert.ok(E.lint("dark trap, deep 808s", "style").every((w) => w.level !== "block"));
});

test("every roll carries a phonk, a syncopation and an experimental adjective", () => {
  const has = (text, pool) => pool.some((p) => text.includes(p));
  for (const input of everyCombo()) {
    const out = E.generate(input);
    assert.ok(has(out.styleText, E.FLAVORS.phonk), out.styleText);
    assert.ok(has(out.styleText, E.FLAVORS.syncopation), out.styleText);
    assert.ok(has(out.styleText, E.FLAVORS.experimental), out.styleText);
  }
});

test("lead instruments vary across rolls", () => {
  const leads = new Set();
  for (let seed = 1; seed <= 60; seed++) {
    const out = E.generate({ lane: "dark-trap", seed });
    leads.add(out.styleText.match(/, ([^,]+) playing /)[1]);
  }
  assert.ok(leads.size >= 20, `${leads.size} distinct leads`);
});
