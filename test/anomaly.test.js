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

test("every roll has syncopation, human feel, a glitch, a signature, a flex, a beat switch, a mix line and a BPM", () => {
  for (const input of rolls()) {
    const out = E.generate(input);
    const s = out.styleText;
    assert.ok(D.SYNCOPATION.some((p) => s.includes(p)), s);
    assert.ok(D.HUMAN_FEEL.some((p) => s.includes(p)), s);
    assert.ok(D.GLITCH.some((p) => s.includes(p)), s);
    assert.ok(s.includes(`signature: ${out.signature}`), s);
    assert.ok(s.includes(`technical flex: ${out.flex}`), s);
    assert.ok(s.includes(`beat switch at the midpoint: ${out.beatSwitch}`), s);
    assert.ok(D.MIX.some((p) => s.includes(p)), s);
    assert.match(s, /\d+ BPM/);
    assert.ok(out.sections.some((x) => x.name === "Beat Switch"), out.lyricsText);
    assert.ok(out.sections.filter((x) => x.name === "Hook").length >= 2, out.lyricsText);
    assert.strictEqual(out.sections.at(-1).tag, "[End]");
    assert.match(out.lyricsText, /hard stop/);
  }
});

test("piano is rare and keys never double up", () => {
  let piano = 0, n = 0;
  for (const seed of Array.from({ length: 300 }, (_, i) => i * 31 + 5)) {
    const s = E.generate({ seed }).styleText;
    n++;
    if (/piano/i.test(s)) piano++;
    const voices = (s.match(/, ([^,]+) playing /) || ["", ""])[1] + " " + (s.match(/, ([^,]+) answering in the gaps/) || ["", ""])[1];
    const keysCount = (voices.match(/\b(piano|organ|rhodes|wurlitzer|clav\w*|keys)\b/gi) || []).length;
    assert.ok(keysCount <= 1, s);
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
  assert.ok(leads.size >= 12, `${leads.size} leads`);
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
