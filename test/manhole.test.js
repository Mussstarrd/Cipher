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

test("every roll leads with rhythm tags and vivid sounds", () => {
  const V = require("../src/shared/vibe.js");
  for (const input of rolls()) {
    const out = E.generate(input);
    const s = out.styleText;
    assert.ok(s.startsWith("experimental minimalist hip hop, "), s);
    const head = s.slice(0, 200);
    for (const pool of [V.GLITCH_TAGS, V.HAT_TAGS, V.SYNC_TAGS]) assert.ok(pool.some((p) => head.includes(p)), `rhythm tag missing from the front: ${s}`);
    assert.ok(head.includes("hard beat switch"), s);
    assert.ok(s.includes(out.parts.bass) && s.includes(out.parts.lead) && s.includes(out.parts.perc), s);
    const hit = `${s}\n${out.lyricsText}`.match(D.SOUNDTRACK);
    assert.ok(!hit, `soundtrack word: ${hit && hit[0]}`);
    assert.match(s, /\d+ BPM/);
    assert.notStrictEqual(out.meta.dna, out.meta.second);
    for (const part of [out.parts.rule, out.parts.beatSwitch, out.parts.triplet]) assert.ok(out.lyricsText.includes(part), `missing from the arrangement: ${part}`);
    assert.match(out.lyricsText, /twitchy/);
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
