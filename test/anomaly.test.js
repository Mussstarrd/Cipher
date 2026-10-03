"use strict";
const test = require("node:test");
const assert = require("node:assert");
const E = require("../src/anomaly/engine.js");
const D = E.D;

const SEEDS = Array.from({ length: 40 }, (_, i) => i * 7919 + 1);
function* rolls() {
  for (const world of D.WORLDS) {
    for (const structure of D.STRUCTURES) {
      for (const seed of SEEDS.slice(0, 6)) yield { world: world.id, structure: structure.id, seed };
    }
  }
  for (const seed of SEEDS) yield { seed };
}

test("same seed, same package", () => {
  assert.deepStrictEqual(E.generate({ seed: 99 }), E.generate({ seed: 99 }));
});

test("every package is clean: no vocals, no FX words, no artist names, inside limits", () => {
  const vocal = /\b(vocal|vocals|vocalist|rap\b|rapper|sing|sings|singing|singer|sung|chant\w*|choir|hum|humming|lyric\w*|ad-?lib\w*|falsetto|harmonies)\b/i;
  for (const input of rolls()) {
    const out = E.generate(input);
    const all = `${out.styleText}\n${out.lyricsText}`;
    assert.deepStrictEqual(out.warnings, [], `${JSON.stringify(input)}\n${out.warnings.map((w) => w.message).join("\n")}\n${all}`);
    assert.ok(!vocal.test(all.replace(/purely instrumental|no-vocal|vocal-free|instrumental/gi, "")), `vocal word in ${JSON.stringify(input)}\n${all}`);
    assert.ok(out.styleText.length <= 1000, out.styleText);
    assert.ok(out.lyricsText.length <= 5000);
    
  }
});

test("every roll leads with rhythm tags and vivid sounds", () => {
  const V = require("../src/shared/vibe.js");
  for (const input of rolls()) {
    const out = E.generate(input);
    const s = out.styleText;
    const world = D.WORLDS.find((w) => w.id === out.meta.world);
    assert.ok(s.startsWith(`${world.genre}, experimental hip hop, `), `genre tags not first: ${s}`);
    const head = s.slice(0, 220);
    for (const pool of [V.GLITCH_TAGS, V.HAT_TAGS, V.SYNC_TAGS]) assert.ok(pool.some((p) => head.includes(p)), `rhythm tag missing from the front: ${s}`);
    assert.ok(head.includes("hard beat switch"), s);
    assert.ok(s.includes(out.bass) && s.includes(out.lead) && s.includes(out.perc), s);
    assert.match(s, /\d+ BPM/);
    assert.ok(out.sections.some((x) => x.name === "Beat Switch"), out.lyricsText);
    assert.ok(out.sections.filter((x) => x.name === "Hook").length >= 2, out.lyricsText);
    assert.strictEqual(out.sections.at(-1).tag, "[End]");
    assert.match(out.lyricsText, /hard stop/);
    for (const part of [out.glitch, out.flex, out.signature]) assert.ok(out.lyricsText.includes(part), `missing from the arrangement: ${part}`);
    assert.match(out.lyricsText, /twitchy stuttering hats/);
  }
});

test("piano is rare", () => {
  let piano = 0, n = 0;
  for (const seed of Array.from({ length: 300 }, (_, i) => i * 31 + 5)) {
    const s = E.generate({ seed }).styleText;
    n++;
    if (/piano/i.test(s)) piano++;
    assert.ok(s.length <= 1000);
  }
  assert.ok(piano / n < 0.2, `piano in ${piano}/${n} rolls`);
});

test("adjectives and leads vary; keys spread", () => {
  const leads = new Set(), keys = new Set(), adjs = new Set();
  for (let seed = 1; seed <= 120; seed++) {
    const out = E.generate({ seed, world: D.WORLDS[0].id });
    leads.add(out.lead);
    adjs.add(out.bass);
    keys.add(out.harmony.key);
  }
  assert.ok(leads.size >= 10, `${leads.size} leads`);
  assert.ok(adjs.size >= 10, `${adjs.size} basses`);
  assert.ok(keys.size >= 8, `${keys.size} keys`);
});

test("chords are spelled with one accidental family", () => {
  for (const input of rolls()) {
    const { chords, key } = E.generate(input).harmony;
    const roots = [key.split(" ")[0], ...chords].join(" ");
    assert.ok(!(/#/.test(roots) && /[A-G]b/.test(roots)), `${key}: ${chords.join(" ")}`);
  }
});

test("exclude list leads with vocals and effects", () => {
  assert.ok(D.EXCLUDE.length >= 10 && D.EXCLUDE.length <= 16);
  assert.match(D.EXCLUDE.slice(0, 3).join(","), /vocal/);
});

test("every blueprint opens differently and no artist name leaks", () => {
  const openers = new Set();
  for (const w of D.WORLDS) for (const o of w.openers) openers.add(o.slice(0, 40));
  assert.strictEqual(openers.size, D.WORLDS.reduce((n, w) => n + w.openers.length, 0));
  const names = D.WORLDS.flatMap((w) => w.aliases).filter((n) => n.replace(/[^a-z0-9]/gi, "").length >= 3);
  for (const w of D.WORLDS) {
    for (let seed = 1; seed <= 10; seed++) {
      const out = E.generate({ world: w.id, seed });
      const text = `${out.styleText}\n${out.lyricsText}`.toLowerCase();
      for (const n of names) assert.ok(!new RegExp(`(^|[^a-z0-9])${n.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")}($|[^a-z0-9])`).test(text), `${n} leaked in ${w.id}`);
    }
  }
});
