"use strict";
const test = require("node:test");
const assert = require("node:assert");
const E = require("../src/manhole/engine.js");
const D = E.D;

function* rolls() {
  for (const dna of D.DNA) {
    for (const structure of D.STRUCTURES) {
      for (let i = 0; i < 8; i++) yield { dna: dna.id, structure: structure.id, seed: i * 104729 + 7 };
    }
  }
  for (let i = 0; i < 300; i++) yield { seed: i * 7919 + 3 };
}

test("same seed, same package", () => {
  assert.deepStrictEqual(E.generate({ seed: 42 }), E.generate({ seed: 42 }));
});

test("every package is clean and inside Suno's limits", () => {
  for (const input of rolls()) {
    const out = E.generate(input);
    assert.deepStrictEqual(out.warnings, [], `${JSON.stringify(input)}\n${out.warnings.map((w) => w.message).join("\n")}\n${out.styleText}\n${out.lyricsText}`);
    assert.ok(out.styleText.length <= 1000, out.styleText);
    assert.ok(out.lyricsText.length <= 5000);
  }
});

test("every roll carries the whole brief", () => {
  for (const input of rolls()) {
    const out = E.generate(input);
    const s = out.styleText;
    const dna = D.DNA.find((d) => d.id === out.meta.dna);
    assert.ok(dna.openers.some((o) => s.startsWith(o)), `opener not first: ${s}`);
    assert.ok(s.includes(`crossed with ${out.parts.trait}`), s);
    for (const pool of [D.WALTZ, D.ON_OFF, D.MIX]) assert.ok(pool.some((p) => s.includes(p)), s);
    const hit = `${s}\n${out.lyricsText}`.match(D.SOUNDTRACK);
    assert.ok(!hit, `soundtrack word: ${hit && hit[0]}`);
    assert.ok(s.includes(`house rule: ${out.parts.rule}`), s);
    assert.ok(s.includes(`beat switch at the midpoint: ${out.parts.beatSwitch}`), s);
    assert.match(s, / looping/);
    assert.match(s, /\d+ BPM .*hard stop ending$/);
    assert.notStrictEqual(out.meta.dna, out.meta.second);
    assert.ok(out.sections.some((x) => x.name === "Beat Switch"));
    assert.ok(out.sections.filter((x) => x.name === "Hook").length >= 2);
    assert.strictEqual(out.sections.at(-1).tag, "[End]");
  }
});

test("the style field stays short", () => {
  let total = 0;
  for (let i = 0; i < 500; i++) total += E.generate({ seed: i * 48271 }).styleText.length;
  assert.ok(total / 500 < 800, `average ${Math.round(total / 500)} chars`);
});

test("fingerprints almost never collide", () => {
  const seen = new Set();
  const n = 2000;
  for (let i = 0; i < n; i++) seen.add(E.generate({ seed: i * 2654435761 }).fingerprint);
  assert.ok(seen.size / n > 0.99, `${n - seen.size} collisions in ${n}`);
});

test("chords are spelled with one accidental family", () => {
  for (const input of rolls()) {
    const { chords, key } = E.generate(input).harmony;
    const roots = [key.split(" ")[0], ...chords].join(" ");
    assert.ok(!(/#/.test(roots) && /[A-G]b/.test(roots)), `${key}: ${chords.join(" ")}`);
  }
});

test("the lint catches a reference name and a song title", () => {
  assert.ok(E.lint("beat like Big Sean").some((w) => w.level === "block"));
  assert.ok(E.lint("a milli type loop").some((w) => w.level === "block"));
  assert.ok(E.lint("professional crisp drums").every((w) => w.level !== "block"));
});
