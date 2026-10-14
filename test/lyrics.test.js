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
const roll = (seed, lane) => L.write({ r: mulberry32(seed), pick, lane: lane || (seed % 2 ? "rap" : "rnb") });
const RADIO = SEEDS.map((seed) => ({ seed, out: roll(seed) }));
const GRITTY = L.LANES.flatMap((lane) => Array.from({ length: 150 }, (_, i) => ({ seed: i + 1, lane, out: roll(i + 1, lane) })));
const SONGS = [...RADIO, ...GRITTY];
const gritty = (out) => L.LANES.includes(out.lane);

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

// Every authored couplet (a rhymed pair of lines) as "a\nb", plus a set of
// every authored line.
const AUTHORED_PAIRS = new Set();
const AUTHORED_LINES = new Set();
const HOOKS = [];
for (const [theme, b] of Object.entries(L.BANK)) {
  for (const h of b.hooks) { HOOKS.push({ theme, ...h }); AUTHORED_PAIRS.add(`${h.a}\n${h.b}`); [h.a, h.b, ...h.alt].forEach((x) => AUTHORED_LINES.add(x)); }
  for (const c of b.rap) { AUTHORED_PAIRS.add(`${c.a}\n${c.b}`); AUTHORED_LINES.add(c.a); AUTHORED_LINES.add(c.b); }
  for (const s of b.rnb) { AUTHORED_PAIRS.add(`${s.l[0]}\n${s.l[1]}`); AUTHORED_PAIRS.add(`${s.l[2]}\n${s.l[3]}`); s.l.forEach((x) => AUTHORED_LINES.add(x)); }
  for (const s of [...b.pre, ...b.bridge]) s.l.forEach((x) => AUTHORED_LINES.add(x));
}
const I_LINES = new Set();
for (const b of Object.values(L.BANK)) for (const c of b.rap) if (c.i) I_LINES.add(c.a);
const STORY = [];
for (const [lane, themes] of Object.entries(L.LANE_BANK)) for (const [theme, units] of Object.entries(themes)) for (const u of units) {
  if (lane === "story") {
    STORY.push({ theme, ...u });
    for (const v of [u.v1, u.v2, u.bridge, u.hook]) for (let i = 0; i + 1 < v.length; i += 2) AUTHORED_PAIRS.add(`${v[i]}\n${v[i + 1]}`);
    [...u.v1, ...u.v2, ...u.bridge, ...u.hook].forEach((x) => AUTHORED_LINES.add(x));
  } else {
    AUTHORED_PAIRS.add(`${u.a}\n${u.b}`);
    AUTHORED_LINES.add(u.a); AUTHORED_LINES.add(u.b);
    if (u.i) I_LINES.add(u.a);
  }
}
const coupletOk = (a, b) => share(P(a), P(b)) || AUTHORED_PAIRS.has(`${P(a)}\n${P(b)}`) || /^(Said it|Say it|Hook said)/.test(P(b));

const ALLOW = /\b(vocal\w*|sing\w*|sung|chant\w*|hum\w*|lyric\w*|ad-?lib\w*|falsetto|harmonies|rap\w*|choir)\b/i;
// Explicit radio records: ordinary cursing is allowed. The hard lines Suno
// blocks stay out: slurs, graphic sex, and the f-word stays under five a song.
const PROFANITY = /\b(nigg\w*|fag\w*|dyke\w*|trann\w*|retard\w*|chink\w*|spic\w*|kike\w*|wetback\w*|cunt|pussy|dick|cock|cum|blowjob|titt\w*|whore\w*|slut\w*|hoes?)\b/i;
const CURSE = /\b(shit\w*|damn|hell|ass|bitch\w*|fuck\w*)\b/i;
const EXPLICIT_THEMES = ["trap", "nightmoves", "gone"];
const MARKER = /\b(shit\w*|damn|hell|ass|bitch\w*|fuck\w*|plug|re-up|pack|bricks?|birds|scale|stove|trap|bando|lean|pills?|smoke|fog|faded|numb|flipped|cooking|whip|tints?|come through|unlocked|say my name|wyd|bag|say less|late text|pull up at two|your place|hoodie's mine|fix the sink|back room|count it|took (one|two)|still gone|gone all day|pour|two cups|nothing but you|rubber bands|tip|like the ride|mama think)\b/i;
const METER = /\b(waltz\w*|lilt\w*|shuffl\w*|swing\w*|swung)\b/i;
const P = L.plain;
const GROUP_VOICE = /\b(chant\w*|choir|crowd|gospel|anthem\w*)\b/i;
const BRANDS = /\b(nike|jordans?|gucci|prada|rolex|amex|venmo|cashapp|uber|lyft|tesla|benz|bentley|iphone|android|instagram|tiktok|snapchat|twitter|facebook|spotify|netflix|hennessy|patron|casamigos|louis|chanel|dior|balenciaga|off-white|supreme|starbucks|mcdonald'?s|wendy'?s)\b/i;
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
const clean = (s) => s.toLowerCase().replace(/[,.?!:]/g, "");

test("same seed, same song", () => {
  assert.deepStrictEqual(roll(99), roll(99));
});

test("every hook rhymes, drops the title on line 1 and calls it back on line 4", () => {
  for (const { seed, out } of SONGS) {
    const h = out.hook.lines;
    assert.strictEqual(h.length, 4, `seed ${seed}: ${h.join(" / ")}`);
    if (out.lane === "grit") {
      // A refrain: two lines said twice.
      assert.ok(h[2] === h[0] && h[3] === h[1], `seed ${seed}: grit refrain is not [a, b, a, b]`);
      const bank = HOOKS.find((x) => x.a === h[0]);
      assert.ok(bank && P(h[1]) === bank.b, `seed ${seed}: grit refrain not in the bank: ${h.join(" / ")}`);
      assert.strictEqual(out.title, bank.title);
    } else if (out.lane === "story") {
      const scene = STORY.find((x) => x.hook[0] === h[0]);
      assert.ok(scene, `seed ${seed}: story hook not in the bank: ${h[0]}`);
      assert.deepStrictEqual(h.map(P), scene.hook, `seed ${seed}: story hook lines differ from the scene`);
      assert.strictEqual(out.title, scene.title);
    } else {
    assert.strictEqual(h[3], h[0], `seed ${seed}: line 4 is not the title line`);
    if (out.hook.curated) {
      const bank = HOOKS.find((x) => x.a === h[0]);
      assert.ok(bank, `seed ${seed}: curated hook not in the bank: ${h[0]}`);
      assert.strictEqual(P(h[1]), bank.b, `seed ${seed}: line 2 is not the bank's rhyme line: ${h[1]}`);
      assert.ok(bank.alt.includes(P(h[2])), `seed ${seed}: line 3 is not one of the bank's third lines: ${h[2]}`);
      assert.strictEqual(out.title, bank.title);
      assert.strictEqual(out.hook.obj, bank.obj);
    } else {
      assert.ok(share(P(h[0]), P(h[1])), `seed ${seed}: lines 1-2 do not rhyme: ${h[0]} / ${h[1]}`);
      assert.ok(share(P(h[2]), P(h[3])), `seed ${seed}: lines 3-4 do not rhyme: ${h[2]} / ${h[3]}`);
    }
    }
    assert.ok(clean(h[0]).includes(clean(out.title)), `seed ${seed}: title "${out.title}" not in "${h[0]}"`);
    const n = out.title.split(" ").length;
    assert.ok(n >= 2 && n <= 4, `seed ${seed}: title "${out.title}" is ${n} words`);
    if (!gritty(out)) h.forEach((l, i) => {
      const s = L.syllables(l);
      assert.ok(s >= 3 && s <= (i === 2 ? 10 : 9), `seed ${seed}: hook line is ${s} syllables: ${l}`);
    });
  }
});

test("at least 90% of seeds use a curated hook, and the bank gets wide use", () => {
  const curated = SONGS.filter(({ out }) => out.hook.curated).length;
  assert.ok(curated / SONGS.length >= 0.9, `${curated} curated of ${SONGS.length}`);
  const distinct = new Set(RADIO.map(({ out }) => out.hook.lines.join("|")));
  assert.ok(distinct.size >= 200, `${distinct.size} distinct hooks`);
  const titles = new Set(RADIO.map(({ out }) => out.title));
  assert.ok(titles.size >= 120, `${titles.size} distinct titles`);
});

test("no banned names or guard words, no slurs or graphic sex, under five f-words, no brands, no group-voice or meter words, under 5000 chars", () => {
  for (const { seed, out } of SONGS) {
    const all = `${out.style}\n${out.lyrics}`;
    const hits = Guard.check(all, ALLOW);
    assert.deepStrictEqual(hits, [], `seed ${seed}: ${hits.map((h) => `${h.rule}: ${h.word}`).join("; ")}`);
    assert.ok(!PROFANITY.test(all), `seed ${seed}: slur or graphic word ${all.match(PROFANITY)}`);
    assert.ok((all.match(/\bfuck\w*/gi) || []).length <= 4, `seed ${seed}: too many f-words`);
    assert.ok(!METER.test(all), `seed ${seed}: meter word ${all.match(METER)}`);
    assert.ok(!GROUP_VOICE.test(all), `seed ${seed}: group-voice word ${all.match(GROUP_VOICE)}`);
    assert.ok(!BRANDS.test(all), `seed ${seed}: brand ${all.match(BRANDS)}`);
    assert.ok(out.lyrics.length < 5000, `seed ${seed}: ${out.lyrics.length} chars`);
    for (const name of Guard.NAMES) {
      const re = new RegExp(`(^|[^a-z0-9])${name.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")}($|[^a-z0-9])`, "i");
      assert.ok(!re.test(all), `seed ${seed}: name "${name}" in the song`);
    }
  }
});

test("every line is a section tag, an ad-lib or a lyric line; tags carry 1-3 cues; ad-libs sit on their own line; verse lines fit", () => {
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
        const limit = out.lane === "technical" ? 16 : out.lane === "grit" ? 14 : 12;
        for (const l of lyr) assert.ok(L.syllables(l) <= limit, `seed ${seed}: verse line over ${limit} syllables: ${l}`);
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
    // A tease (2) + a short verse (8) + a pre-hook (3) at most: the first full hook by bar 20.
    if (!gritty(out)) assert.ok(before <= 14, `seed ${seed}: ${before} lines before the first full hook`);
    const v1 = secs.find((s) => s.name === "Verse 1"), v2 = secs.find((s) => s.name === "Verse 2");
    assert.ok(v1 && v2, `seed ${seed}: missing a verse`);
    assert.notStrictEqual(v1.lines[0], v2.lines[0], `seed ${seed}: both verses open the same way`);
    assert.ok(out.hook.obj && v2.lines.map(P).join("\n").toLowerCase().includes(out.hook.obj.toLowerCase()), `seed ${seed}: verse 2 never mentions the hook's anchor "${out.hook.obj}"`);
    if (!gritty(out)) assert.ok(secs.some((s) => s.name === "Pre-Hook"), `seed ${seed}: missing the pre-hook`);
    if (!gritty(out)) assert.ok(secs.some((s) => s.name === "Bridge" || s.name === "Beat Switch"), `seed ${seed}: missing the bridge or switch`);
    const outro = secs.find((s) => s.name === "Outro");
    assert.deepStrictEqual(outro.lines.slice(0, 2), [out.hook.lines[0], out.hook.lines[0]], `seed ${seed}: outro is not the title line twice`);
    if (out.lane === "rap" || gritty(out)) for (const v of [v1, v2]) assert.ok(v.cues.includes("rapped"), `seed ${seed}: rap verse without the rapped cue`);
    if (gritty(out)) continue;
    // Rap verses: 16 / 12 / 8 bars; sung verses: one or two stanzas, plus the callback couplet.
    for (const v of [v1, v2]) {
      const n = v.lines.filter((l) => !ADLIB.test(l)).length;
      assert.ok([4, 6, 8, 10, 12, 16].includes(n), `seed ${seed}: [${v.name}] has ${n} lines`);
    }
  }
});

test("couplets rhyme, no couplet repeats in a song, and every rap verse carries an internal rhyme", () => {
  const famsIn = (line) => {
    const hits = {};
    for (const w of line.replace(/\([^)]*\)/g, "").toLowerCase().replace(/[^a-z0-9' ]/g, " ").split(/\s+/)) {
      for (const id of FAM[w] || []) (hits[id] = hits[id] || new Set()).add(w);
    }
    return Object.values(hits).some((set) => set.size >= 2);
  };
  for (const { seed, out } of SONGS) {
    const seen = new Set();
    for (const s of sections(out.lyrics).filter((x) => /^Verse/.test(x.name))) {
      const lyr = s.lines.filter((l) => !ADLIB.test(l));
      for (let i = 0; i + 1 < lyr.length; i += 2) {
        assert.ok(coupletOk(lyr[i], lyr[i + 1]), `seed ${seed}: couplet does not rhyme: ${lyr[i]} / ${lyr[i + 1]}`);
        const key = `${P(lyr[i])}\n${P(lyr[i + 1])}`;
        assert.ok(!seen.has(key), `seed ${seed}: couplet repeated: ${lyr[i]}`);
        seen.add(key);
      }
      const rapped = s.cues.includes("rapped") || s.cues.includes("feature");
      if (rapped && out.lane !== "story") assert.ok(lyr.some((l) => I_LINES.has(P(l)) || famsIn(P(l))), `seed ${seed}: [${s.name}] has no internal rhyme:\n${lyr.join("\n")}`);
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

test("the authored bank: sizes, syllables, rhymes, guard words, names, brands, no duplicate couplets", () => {
  const themes = Object.keys(L.BANK);
  assert.ok(themes.length >= 15, `${themes.length} themes`);
  for (const t of EXPLICIT_THEMES) assert.ok(L.BANK[t], `missing the ${t} theme`);
  let units = 0, explicit = 0;
  for (const t of themes) for (const k of ["hooks", "rap", "rnb", "pre", "bridge"]) for (const u of L.BANK[t][k]) {
    units++;
    const text = k === "hooks" ? [u.a, u.b, ...u.alt].join(" ") : k === "rap" ? `${u.a} ${u.b}` : u.l.join(" ");
    if (u.x) { explicit++; assert.ok(MARKER.test(text), `${t}.${k}: flagged explicit without a curse, innuendo or dealing word: ${text}`); }
    if (k !== "rap") assert.ok(!/\bfuck/i.test(text), `${t}.${k}: the f-word only goes in rap couplets: ${text}`);
  }
  assert.ok(explicit / units >= 1 / 3, `${explicit} of ${units} units carry the explicit register`);
  const pairs = new Set();
  const lineOk = (where, line, lo, hi) => {
    const s = L.syllables(line);
    assert.ok(s >= lo && s <= hi, `${where}: ${s} syllables: ${line}`);
    assert.deepStrictEqual(Guard.check(line, ALLOW), [], `${where}: guard word in: ${line}`);
    assert.ok(!GROUP_VOICE.test(line) && !PROFANITY.test(line) && !BRANDS.test(line), `${where}: banned word in: ${line}`);
    assert.match(line, LYRIC, `${where}: odd character in: ${line}`);
    for (const name of Guard.NAMES) {
      const re = new RegExp(`(^|[^a-z0-9])${name.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")}($|[^a-z0-9])`, "i");
      assert.ok(!re.test(line), `${where}: name "${name}" in: ${line}`);
    }
  };
  const pairOk = (where, a, b, fam) => {
    assert.ok(fam || share(a, b), `${where}: untagged couplet does not rhyme: ${a} / ${b}`);
    const key = `${a}\n${b}`;
    assert.ok(!pairs.has(key), `${where}: duplicate couplet: ${a}`);
    pairs.add(key);
  };
  for (const t of themes) {
    const b = L.BANK[t];
    assert.ok(b.hooks.length >= 12, `${t}: ${b.hooks.length} hooks`);
    assert.ok(b.rap.length >= 10, `${t}: ${b.rap.length} rap couplets`);
    assert.ok(b.rnb.length >= 6, `${t}: ${b.rnb.length} stanzas`);
    assert.ok(b.pre.length >= 4 && b.bridge.length >= 4, `${t}: pre/bridge`);
    assert.ok(b.rap.some((c) => c.k === "turn") && b.rap.filter((c) => c.i).length >= 3, `${t}: needs a turn and internal rhymes`);
    b.hooks.forEach((h, i) => {
      lineOk(`${t}.hook${i}.a`, h.a, 4, 9);
      lineOk(`${t}.hook${i}.b`, h.b, 4, 9);
      assert.strictEqual(h.alt.length, 3, `${t}.hook${i}: three third lines`);
      h.alt.forEach((x, j) => lineOk(`${t}.hook${i}.alt${j}`, x, 4, 10));
      const n = h.title.split(" ").length;
      assert.ok(n >= 2 && n <= 4, `${t}.hook${i}: title "${h.title}"`);
      assert.ok(clean(h.a).includes(clean(h.title)), `${t}.hook${i}: title not in the title line`);
      assert.ok(h.a.toLowerCase().includes(h.obj), `${t}.hook${i}: anchor "${h.obj}" not in the title line`);
      assert.ok(h.fam, `${t}.hook${i}: fam`);
      pairOk(`${t}.hook${i}`, h.a, h.b, h.fam);
    });
    b.rap.forEach((c, i) => { lineOk(`${t}.rap${i}.a`, c.a, 5, 12); lineOk(`${t}.rap${i}.b`, c.b, 5, 12); assert.ok(["flex", "detail", "callback", "turn"].includes(c.k)); pairOk(`${t}.rap${i}`, c.a, c.b, c.fam); });
    b.rnb.forEach((s, i) => { assert.strictEqual(s.l.length, 4); s.l.forEach((x, j) => lineOk(`${t}.rnb${i}.${j}`, x, 7, 11)); pairOk(`${t}.rnb${i}.1`, s.l[0], s.l[1], s.fam); pairOk(`${t}.rnb${i}.2`, s.l[2], s.l[3], s.fam); });
    b.pre.forEach((s, i) => { assert.ok(s.l.length >= 2 && s.l.length <= 3); s.l.forEach((x, j) => lineOk(`${t}.pre${i}.${j}`, x, 3, 12)); assert.ok(s.fam); });
    b.bridge.forEach((s, i) => { assert.strictEqual(s.l.length, 4); s.l.forEach((x, j) => lineOk(`${t}.bridge${i}.${j}`, x, 4, 12)); assert.ok(s.fam); });
  }
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

test("explicit register: at least 30% of songs carry a curse or an explicit theme", () => {
  const n = SONGS.filter(({ out }) => CURSE.test(out.lyrics) || EXPLICIT_THEMES.includes(out.theme.id)).length;
  assert.ok(n / SONGS.length >= 0.3, `${n} of ${SONGS.length}`);
});

test("human feel: one feel clause on the style, a timing cue on 70%+ of tags that never repeats back to back, 4-8 spelling devices a song", () => {
  assert.ok(L.FEEL.length >= 12 && L.TIMING_ALL.length >= 14);
  for (const f of [...L.FEEL.map((x) => x.t), ...L.TIMING_ALL]) {
    assert.ok(!METER.test(f), `meter word in: ${f}`);
    assert.deepStrictEqual(Guard.check(f, L.ALLOW), [], `guard word in: ${f}`);
  }
  for (const { seed, out } of SONGS) {
    const clauses = L.FEEL.filter((f) => out.style.endsWith(`, ${f.t}`));
    assert.strictEqual(clauses.length, 1, `seed ${seed}: style needs exactly one feel clause: ${out.style}`);
    assert.strictEqual(out.style, `${out.persona.tag}, ${clauses[0].t}`);
    const secs = sections(out.lyrics).filter((s) => s.name !== "End");
    const cues = secs.map((s) => s.cues.find((c) => L.TIMING_ALL.includes(c)) || null);
    const withCue = cues.filter(Boolean).length;
    assert.ok(withCue / secs.length >= 0.7, `seed ${seed}: ${withCue} of ${secs.length} tags carry a timing cue`);
    for (let i = 1; i < cues.length; i++) if (cues[i] && cues[i - 1]) assert.notStrictEqual(cues[i], cues[i - 1], `seed ${seed}: [${secs[i].name}] repeats the timing cue "${cues[i]}"`);
    for (const s of secs) assert.ok(s.cues.length >= 1 && s.cues.length <= 3, `seed ${seed}: [${s.name}] has ${s.cues.length} cues`);
    // Devices: count distinct lyric lines that carry one; ad-libs and tags never do.
    const lyr = [...new Set(lines(out.lyrics).filter((l) => !TAG.test(l)))];
    let touches = 0;
    for (const l of lyr) {
      if (ADLIB.test(l)) { assert.ok(!/\.\.\.| - |([aeiou])\1\1/i.test(l), `seed ${seed}: device inside an ad-lib: ${l}`); continue; }
      const stretched = /([aeiou])\1\1/i.test(l), dots = /^\.\.\./.test(l), breath = / - /.test(l), stutter = /^([A-Za-z]{1,2})-\1/i.test(l);
      assert.ok((l.match(/([aeiou])\1\1/gi) || []).length <= 1, `seed ${seed}: two stretched words on one line: ${l}`);
      touches += stretched + dots + breath + stutter;
    }
    assert.ok(touches >= 4 && touches <= 8, `seed ${seed}: ${touches} devices`);
    assert.strictEqual(touches, out.touches, `seed ${seed}: the writer counted ${out.touches}`);
    const h = out.hook.lines;
    assert.ok(!/([aeiou])\1\1/i.test(h[0]), `seed ${seed}: the title line is never stretched`);
    assert.ok(h.filter((l) => /([aeiou])\1\1/i.test(l)).length <= 2);
  }
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
  assert.ok(RADIO.every(({ out }) => out.lane === "rap" || sections(out.lyrics).find((s) => s.name === "Verse 2").cues[0] !== "rapped"));
});

test("gritty lanes: personas, bars-first shape, punchline rhymes, dense runs, story turns", () => {
  assert.deepStrictEqual(L.LANES, ["grit", "technical", "story"]);
  for (const lane of L.LANES) {
    assert.ok(L.LANE_PERSONAS[lane].length >= 3 && L.LANE_PERSONAS[lane].some((p) => p.gender === "female"), lane);
    for (const p of L.LANE_PERSONAS[lane]) assert.deepStrictEqual(Guard.check(p.tag, L.ALLOW), [], p.tag);
    assert.ok(Object.keys(L.LANE_BANK[lane]).length >= 3, `${lane}: themes`);
  }
  const endsWith = (line, end) => P(line).toLowerCase().replace(/[^a-z0-9' ]/g, "").endsWith(end.toLowerCase().replace(/[^a-z0-9' ]/g, ""));
  for (const [theme, units] of Object.entries(L.LANE_BANK.grit)) {
    assert.ok(units.length >= 12, `grit.${theme}`);
    for (const c of units) {
      assert.ok(c.rhyme && c.rhyme.length === 2 && endsWith(c.a, c.rhyme[0]) && endsWith(c.b, c.rhyme[1]), `grit.${theme}: rhyme ends not at the line ends: ${c.a}`);
      assert.ok(L.syllables(c.rhyme[0]) >= 2 && L.syllables(c.rhyme[1]) >= 2, `grit.${theme}: not a multi-syllable rhyme: ${c.rhyme}`);
      assert.ok(c.fam, `grit.${theme}: fam`);
    }
    assert.ok(units.filter((c) => c.i).length >= 9, `grit.${theme}: internal rhyme on most couplets`);
  }
  for (const [theme, units] of Object.entries(L.LANE_BANK.technical)) {
    assert.ok(units.length >= 12, `technical.${theme}`);
    for (const c of units) {
      assert.ok(c.inner && c.inner.length >= 2, `technical.${theme}: fewer than two internal rhymes: ${c.a}`);
      const text = `${c.a} ${c.b}`.toLowerCase();
      for (const pair of c.inner) { assert.ok(pair.length >= 2); for (const w of pair) assert.ok(text.includes(w.toLowerCase()), `technical.${theme}: "${w}" not in the couplet`); }
      assert.ok(L.syllables(c.a) <= (c.run && c.run.includes("a") ? 16 : 12) && L.syllables(c.b) <= (c.run && c.run.includes("b") ? 16 : 12), `technical.${theme}: line too long: ${c.a} / ${c.b}`);
    }
  }
  for (const sc of STORY) {
    assert.ok(sc.v1.length === 16 && sc.v2.length === 20 && sc.bridge.length === 4 && sc.hook.length === 4, `story.${sc.theme}: sizes`);
    for (const l of [...sc.v1, ...sc.v2, ...sc.bridge]) assert.ok(L.syllables(l) <= 12, `story.${sc.theme}: over 12: ${l}`);
    assert.ok(Number.isInteger(sc.turn) && sc.turn >= 4 && sc.turn < 20, `story.${sc.theme}: turn`);
    assert.ok(clean(sc.hook[0]).includes(clean(sc.title)));
  }
  for (const { seed, lane, out } of GRITTY) {
    assert.ok(out.lane === lane && L.LANE_PERSONAS[lane].some((p) => p.tag === out.persona.tag), `${lane} ${seed}: persona`);
    const secs = sections(out.lyrics);
    const names = secs.map((s) => s.name);
    const third = lane === "story" ? "Bridge" : "Verse 3";
    assert.deepStrictEqual(names, ["Intro", "Verse 1", "Hook", "Verse 2", "Hook", third, "Hook", "Outro", "End"], `${lane} ${seed}: ${names}`);
    const count = (name) => secs.find((s) => s.name === name).lines.filter((l) => !ADLIB.test(l)).length;
    assert.strictEqual(count("Verse 1"), 16, `${lane} ${seed}: verse 1`);
    assert.strictEqual(count("Verse 2"), lane === "story" ? 20 : 16, `${lane} ${seed}: verse 2`);
    if (lane !== "story") assert.ok([8, 12].includes(count("Verse 3")), `${lane} ${seed}: verse 3`);
    if (lane === "technical") for (const v of secs.filter((s) => /^Verse/.test(s.name))) assert.ok(v.cues.some((c) => L.FLOW.includes(c)), `${lane} ${seed}: flow switch cue missing on [${v.name}]`);
    if (lane === "grit") {
      const hook = secs.find((s) => s.name === "Hook");
      assert.ok(hook.lines.filter((l) => ADLIB.test(l)).length >= 1, `${lane} ${seed}: refrain needs its signature ad-lib`);
      assert.ok(hook.cues.includes("rapped refrain"));
    }
    if (lane === "story") {
      const scene = STORY.find((x) => x.title === out.title);
      const v2 = secs.find((s) => s.name === "Verse 2").lines.filter((l) => !ADLIB.test(l)).map(P);
      assert.ok(v2.includes(scene.v2[scene.turn]), `${lane} ${seed}: the turn line is missing from verse 2`);
      const ads = secs.filter((s) => /^Verse/.test(s.name)).flatMap((s) => s.lines.filter((l) => ADLIB.test(l))).length;
      assert.ok(ads <= 4, `${lane} ${seed}: too many ad-libs for a story`);
    }
  }
});
