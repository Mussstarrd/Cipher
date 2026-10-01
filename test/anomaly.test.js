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

test("every roll keeps its identity and the extras survive most of the time", () => {
  let n = 0;
  const miss = { swing: 0, sync: 0, glitch: 0, move: 0, flex: 0, mix: 0 };
  for (const input of rolls()) {
    const out = E.generate(input);
    const s = out.styleText;
    n++;
    const world = D.WORLDS.find((w) => w.id === out.meta.world);
    assert.ok(world.openers.some((o) => s.startsWith(o)), `opener not first: ${s}`);
    assert.ok(s.includes(`signature: ${out.signature}`), s);
    assert.ok(s.includes(`beat switch at the midpoint: ${out.beatSwitch}`), s);
    assert.ok(D.CLASH.includes(out.clash) || world.clash.includes(out.clash), s);
    assert.ok(s.includes(`${out.clash} cutting in every 4 bars`), s);
    assert.ok(D.TIMBRE.some((p) => s.includes(p)) && D.PLAYING.some((p) => s.includes(p)) && D.SPACE.some((p) => s.includes(p)), `lead character missing: ${s}`);
    assert.match(s, /looping obsessively/);
    assert.ok(!/\b(bright|twinkling|high-pitched|sine lead|synth lead)\b/i.test(s.match(/, ([^,]+) playing /)[1].split(" ").slice(1).join(" ")), `high lead: ${s}`);
    assert.match(s, /\d+ BPM/);
    if (!D.SWING.some((p) => s.includes(p))) miss.swing++;
    if (!D.SYNC.some((p) => s.includes(p))) miss.sync++;
    if (!D.GLITCH.some((p) => s.includes(p))) miss.glitch++;
    if (!s.includes(`trademark move: ${out.move}`)) miss.move++;
    if (!s.includes(`technical flex: ${out.flex}`)) miss.flex++;
    if (!D.MIX.some((p) => s.includes(p))) miss.mix++;
    assert.ok(out.sections.some((x) => x.name === "Beat Switch"), out.lyricsText);
    assert.ok(out.sections.filter((x) => x.name === "Hook").length >= 2, out.lyricsText);
    assert.strictEqual(out.sections.at(-1).tag, "[End]");
    assert.match(out.lyricsText, /hard stop/);
  }
  for (const k of ["swing", "sync", "flex"]) assert.ok(miss[k] / n < 0.1, `${k} missing in ${miss[k]}/${n}`);
  // The glitch and the trademark move also live in the arrangement tags, so the style field may drop them when long.
  assert.ok(miss.glitch / n < 0.6, `glitch missing in ${miss.glitch}/${n}`);
  assert.ok(miss.move / n < 0.8, `move missing in ${miss.move}/${n}`);
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
    leads.add(out.styleText.match(/, ([^,]+) playing /)[1].split(" ").slice(1).join(" "));
    adjs.add(out.styleText.match(/, ([^,]+) playing /)[1].split(" ")[0]);
    keys.add(out.harmony.key);
  }
  assert.ok(leads.size >= 10, `${leads.size} leads`);
  assert.ok(adjs.size >= 25, `${adjs.size} adjectives`);
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
