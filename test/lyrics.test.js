"use strict";
const test = require("node:test");
const assert = require("node:assert");
const L = require("../src/one/lyrics.js");
const Guard = require("../src/one/guard.js");

// The engine's RNG, copied so the test rolls what the engine rolls.
function mulberry32(a) {
  return () => {
    a |= 0;
    a = (a + 0x6d2b79f5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}
const pick = (arr, r) => arr[Math.floor(r() * arr.length)];
const SEEDS = Array.from({ length: 500 }, (_, i) => i + 1);
const roll = (seed) => L.write({ r: mulberry32(seed), pick, lane: seed % 2 ? "rap" : "rnb" });
const SONGS = SEEDS.map((seed) => ({ seed, out: roll(seed) }));

// Rhyme families, read back from the module's bank by last word.
const FAM = {};
for (const f of L.RHYMES) for (const e of f.entries) {
  const w = L.lastWord(e.text);
  (FAM[w] = FAM[w] || new Set()).add(f.id);
}
const share = (a, b) => {
  const A = FAM[L.lastWord(a)], B = FAM[L.lastWord(b)];
  return !!(A && B && [...A].some((id) => B.has(id)));
};

const ALLOW = /\b(vocal\w*|sing\w*|sung|chant\w*|hum\w*|lyric\w*|ad-?lib\w*|falsetto|harmonies|rap\w*|choir)\b/i;
const PROFANITY = /\b(fuck\w*|f\*+k\w*|shit\w*|bitch\w*|nigg\w*|pussy|dick|cunt|whore|slut|hoes?|cock)\b/i;
const GROUP_VOICE = /\b(chant\w*|choir|crowd|gospel|anthem\w*)\b/i;
const TAG = /^\[[^\]]+\]$/;
const LYRIC = /^[A-Za-z0-9'"(),.?!:&\- ]+$/;
const ADLIB = /^\([a-z' ,-]{1,24}\)$/i;

const lines = (text) => text.split("\n").filter((l) => l.trim());
// Sections as [{ name, cues, lines }].
function sections(text) {
  const out = [];
  for (const l of lines(text)) {
    if (TAG.test(l)) {
      const m = l.match(/^\[([^|\]]+)(?:\|(.*))?\]$/);
      out.push({ name: m[1].trim(), cues: m[2] ? m[2].split(",").map((c) => c.trim()).filter(Boolean) : [], lines: [] });
    } else out[out.length - 1].lines.push(l);
  }
  return out;
}

test("same seed, same song", () => {
  assert.deepStrictEqual(roll(99), roll(99));
});

test("every hook rhymes, drops the title on line 1 and calls it back on line 4", () => {
  for (const { seed, out } of SONGS) {
    const h = out.hook.lines;
    assert.strictEqual(h.length, 4, `seed ${seed}: ${h.join(" / ")}`);
    assert.ok(share(h[0], h[1]), `seed ${seed}: lines 1-2 do not rhyme: ${h[0]} / ${h[1]}`);
    assert.ok(share(h[2], h[3]), `seed ${seed}: lines 3-4 do not rhyme: ${h[2]} / ${h[3]}`);
    assert.strictEqual(h[3], h[0], `seed ${seed}: line 4 is not the title line`);
    const t = out.title.toLowerCase();
    assert.ok(h[0].toLowerCase().replace(/[,.?!]/g, "").includes(t), `seed ${seed}: title "${out.title}" not in "${h[0]}"`);
    const n = t.split(" ").length;
    assert.ok(n >= 2 && n <= 4, `seed ${seed}: title "${out.title}" is ${n} words`);
    for (const l of h) {
      const s = L.syllables(l);
      assert.ok(s >= 4 && s <= 9, `seed ${seed}: hook line is ${s} syllables: ${l}`);
    }
  }
});

test("no banned names or guard words, no profanity, no group-voice words, under 5000 chars", () => {
  for (const { seed, out } of SONGS) {
    const all = `${out.style}\n${out.lyrics}`;
    const hits = Guard.check(all, ALLOW);
    assert.deepStrictEqual(hits, [], `seed ${seed}: ${hits.map((h) => `${h.rule}: ${h.word}`).join("; ")}`);
    assert.ok(!PROFANITY.test(all), `seed ${seed}: profanity in ${all.match(PROFANITY)}`);
    assert.ok(!GROUP_VOICE.test(all), `seed ${seed}: group-voice word ${all.match(GROUP_VOICE)}`);
    assert.ok(out.lyrics.length < 5000, `seed ${seed}: ${out.lyrics.length} chars`);
    for (const name of Guard.NAMES) {
      const re = new RegExp(`(^|[^a-z0-9])${name.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")}($|[^a-z0-9])`, "i");
      assert.ok(!re.test(all), `seed ${seed}: name "${name}" in the song`);
    }
  }
});

test("every line is a section tag, an ad-lib or a lyric line; tags carry 1-3 cues; ad-libs sit on their own line", () => {
  for (const { seed, out } of SONGS) {
    for (const l of lines(out.lyrics)) {
      assert.ok(TAG.test(l) || LYRIC.test(l), `seed ${seed}: odd line: ${l}`);
      if (!TAG.test(l) && l.includes("(")) assert.ok(ADLIB.test(l), `seed ${seed}: ad-lib not on its own line: ${l}`);
    }
    const secs = sections(out.lyrics);
    assert.strictEqual(secs[secs.length - 1].name, "End");
    for (const s of secs) {
      if (s.name === "End") continue;
      assert.ok(s.cues.length >= 1 && s.cues.length <= 3, `seed ${seed}: ${s.cues.length} cues in [${s.name}]`);
      if (/^Verse/.test(s.name)) {
        const lyr = s.lines.filter((l) => !ADLIB.test(l));
        const ads = s.lines.filter((l) => ADLIB.test(l));
        assert.ok(ads.length * 2 <= lyr.length, `seed ${seed}: too many ad-libs in [${s.name}]`);
        for (const l of lyr) assert.ok(L.syllables(l) <= 12, `seed ${seed}: verse line over 12 syllables: ${l}`);
      }
    }
  }
});

test("radio shape: a vocal line in the first section, 3-4 hook statements, verse 2 calls back the hook, verses differ", () => {
  for (const { seed, out } of SONGS) {
    const secs = sections(out.lyrics);
    assert.ok(secs[0].lines.length >= 1, `seed ${seed}: first section has nothing to sing`);
    const hooks = secs.filter((s) => s.name === "Hook");
    assert.ok(hooks.length >= 3 && hooks.length <= 4, `seed ${seed}: ${hooks.length} hooks`);
    const full = hooks.filter((s) => s.lines.filter((l) => !ADLIB.test(l)).length === 4);
    assert.ok(full.length >= 3, `seed ${seed}: only ${full.length} full hooks`);
    const idx = secs.findIndex((s) => s.name === "Hook" && s.lines.filter((l) => !ADLIB.test(l)).length === 4);
    const before = secs.slice(0, idx).reduce((n, s) => n + s.lines.filter((l) => !ADLIB.test(l)).length, 0);
    // A tease (2) + a short verse (8) + a pre-hook (4) at most: the first full hook by bar 20.
    assert.ok(before <= 14, `seed ${seed}: ${before} lines before the first full hook`);
    const v1 = secs.find((s) => s.name === "Verse 1"), v2 = secs.find((s) => s.name === "Verse 2");
    assert.ok(v1 && v2, `seed ${seed}: missing a verse`);
    assert.notStrictEqual(v1.lines[0], v2.lines[0], `seed ${seed}: both verses open the same way`);
    const hookObj = out.hook.lines[0];
    const callback = L.THEMES.find((t) => t.id === out.theme.id).obj.filter((o) => hookObj.toLowerCase().includes(o));
    assert.ok(callback.some((o) => v2.lines.join("\n").toLowerCase().includes(o)) || callback.length === 0, `seed ${seed}: verse 2 never mentions the hook's object (${callback})`);
    assert.ok(secs.some((s) => s.name === "Pre-Hook"), `seed ${seed}: missing the pre-hook`);
    assert.ok(secs.some((s) => s.name === "Bridge" || s.name === "Beat Switch"), `seed ${seed}: missing the bridge or switch`);
    const outro = secs.find((s) => s.name === "Outro");
    assert.deepStrictEqual(outro.lines.slice(0, 2), [out.hook.lines[0], out.hook.lines[0]], `seed ${seed}: outro is not the title line twice`);
    if (out.lane === "rap") for (const v of [v1, v2]) assert.ok(v.cues.includes("rapped"), `seed ${seed}: rap verse without the rapped cue`);
  }
});

test("couplets rhyme and every four bars carry an internal rhyme", () => {
  const famsIn = (line) => {
    const hits = {};
    for (const w of line.replace(/\([^)]*\)/g, "").toLowerCase().replace(/[^a-z0-9' ]/g, " ").split(/\s+/)) {
      for (const id of FAM[w] || []) (hits[id] = hits[id] || new Set()).add(w);
    }
    return Object.values(hits).some((set) => set.size >= 2);
  };
  for (const { seed, out } of SONGS) {
    for (const s of sections(out.lyrics).filter((x) => /^Verse/.test(x.name))) {
      const lyr = s.lines.filter((l) => !ADLIB.test(l));
      for (let i = 0; i + 1 < lyr.length; i += 2) assert.ok(share(lyr[i], lyr[i + 1]), `seed ${seed}: couplet does not rhyme: ${lyr[i]} / ${lyr[i + 1]}`);
      for (let i = 0; i < lyr.length; i += 4) {
        const block = lyr.slice(i, i + 4);
        assert.ok(block.some(famsIn), `seed ${seed}: four bars without an internal rhyme:\n${block.join("\n")}`);
      }
    }
  }
});

test("no two identical consecutive lines outside the outro", () => {
  for (const { seed, out } of SONGS) {
    for (const s of sections(out.lyrics)) {
      if (s.name === "Outro") continue;
      for (let i = 1; i < s.lines.length; i++) assert.notStrictEqual(s.lines[i], s.lines[i - 1], `seed ${seed}: repeated line in [${s.name}]: ${s.lines[i]}`);
    }
  }
});

test("hooks are distinct across seeds: at least 3 distinct, 95%+ unique", () => {
  const set = new Set(SONGS.map(({ out }) => out.hook.lines.join("|")));
  assert.ok(set.size >= 3);
  assert.ok(set.size / SONGS.length >= 0.95, `${set.size} unique hooks of ${SONGS.length}`);
  const titles = new Set(SONGS.map(({ out }) => out.title));
  assert.ok(titles.size / SONGS.length >= 0.8, `${titles.size} unique titles`);
});

test("banks meet the brief: 2 lanes x 8+ personas, 10+ themes, 40+ hook templates, 20+ rhyme families of 8-15", () => {
  for (const lane of ["rap", "rnb"]) {
    assert.ok(L.PERSONAS[lane].length >= 8);
    assert.ok(L.PERSONAS[lane].some((p) => p.gender === "female") && L.PERSONAS[lane].some((p) => p.gender === "male"));
    for (const p of L.PERSONAS[lane]) {
      assert.ok(p.tag && p.adlibs.length && p.palette.length, p.id);
      assert.deepStrictEqual(Guard.check(p.tag, L.ALLOW), [], p.tag);
    }
  }
  assert.ok(L.THEMES.length >= 10);
  for (const t of L.THEMES) for (const k of ["obj", "place", "img", "scene", "verbs", "flex", "vuln"]) assert.ok(t[k].length >= 5, `${t.id}.${k}`);
  assert.ok(L.HOOK_T.length >= 40);
  assert.ok(L.RHYMES.length >= 20);
  for (const f of L.RHYMES) assert.ok(f.entries.length >= 8 && f.entries.length <= 15, `${f.id}: ${f.entries.length}`);
});

test("lanes, themes and personas can be forced, and a feature verse shows up in the rnb lane", () => {
  const out = L.write({ r: mulberry32(5), pick, lane: "rnb", theme: "latetext", persona: "falsetto" });
  assert.strictEqual(out.lane, "rnb");
  assert.strictEqual(out.theme.id, "latetext");
  assert.strictEqual(out.persona.id, "falsetto");
  const features = SONGS.filter(({ out }) => out.lane === "rnb" && out.persona.feature);
  assert.ok(features.length >= 30, `${features.length} feature verses`);
  for (const { seed, out } of features) {
    const v2 = sections(out.lyrics).find((s) => s.name === "Verse 2");
    assert.ok(v2.cues.includes("feature") && /rapper/.test(v2.cues[1]), `seed ${seed}: feature tag ${v2.cues}`);
    assert.notStrictEqual(out.persona.feature.gender, out.persona.gender);
  }
  assert.ok(SONGS.every(({ out }) => out.lane === "rap" || sections(out.lyrics).find((s) => s.name === "Verse 2").cues[0] !== "rapped"));
});
