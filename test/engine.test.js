"use strict";
const test = require("node:test");
const assert = require("node:assert");
const E = require("../src/engine.js");

const EDGES = Object.keys(E.EDGES);
const TEMPLATES = Object.keys(E.TEMPLATES);

function* everyCombo() {
  for (const lane of E.LANES) {
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
    assert.ok(out.meta.descriptors <= 12 && out.meta.descriptors >= 8, `${out.meta.descriptors}: ${out.styleText}`);
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
