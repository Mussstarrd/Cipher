"use strict";
const test = require("node:test");
const assert = require("node:assert");
const E = require("../src/one/engine.js");
const G = require("../src/one/grammar.js");
const Guard = require("../src/one/guard.js");

const seeds = Array.from({ length: 3000 }, (_, i) => i * 7919 + 3);

test("same seed, same package", () => {
  assert.deepStrictEqual(E.generate({ seed: 42 }), E.generate({ seed: 42 }));
});

test("every grammar and genre entry passes the guard", () => {
  const walk = (o, path) => {
    if (typeof o === "string") assert.deepStrictEqual(Guard.check(o), [], `${path}: ${o}`);
    else if (o && typeof o === "object") for (const k of Object.keys(o)) if (k !== "EXCLUDE" && typeof o[k] !== "function") walk(o[k], `${path}.${k}`);
  };
  walk(G, "G");
  walk(require("../src/one/genres.js"), "genres");
});

test("every N bars phrases are capped: one per roll, none in loop mode", () => {
  for (const g of E.genres()) for (const seed of seeds.slice(0, 1000)) {
    const { style, lyrics, meta } = E.generate({ seed, genre: g.id });
    const hits = `${style}\n${lyrics}`.match(new RegExp(E.EVERY.source, "gi")) || [];
    const limit = meta.mode === "loop" ? 0 : 1;
    // The same phrase may echo once in a lyrics tag; count distinct phrases.
    assert.ok(new Set(hits.map((h) => h.toLowerCase())).size <= limit, `${g.id} ${meta.mode}: ${hits.join(" | ")}\n${style}\n${lyrics}`);
  }
});

test("switch mode repeats its axis words after the switch, and never says returns", () => {
  for (const g of E.genres()) for (const seed of seeds.slice(0, 1000)) {
    const { lyrics, style, parts, meta } = E.generate({ seed, genre: g.id });
    assert.ok(!/returns/.test(lyrics), lyrics);
    if (meta.mode !== "switch") continue;
    assert.ok(style.includes(parts.switch.tag), style);
    const after = lyrics.split("[Beat Switch")[1];
    assert.ok(after && after.includes(parts.switch.after), lyrics);
  }
});

test("pop always leaves a feature-verse slot; every genre stays instrumental", () => {
  for (const seed of seeds.slice(0, 300)) {
    const pop = E.generate({ seed, genre: "pop" });
    assert.match(pop.lyrics, /\[Feature Verse \| /);
    for (const g of E.genres()) {
      const o = E.generate({ seed, genre: g.id });
      assert.ok(o.style.endsWith("instrumental.") || o.style.includes("instrumental."), o.style);
      assert.ok(o.exclude.startsWith("vocals"));
    }
  }
});

test("every roll is clean and inside Suno's limits", () => {
  for (const g of E.genres()) for (const seed of seeds) {
    const out = E.generate({ seed, genre: g.id });
    assert.deepStrictEqual(out.warnings, [], `${seed}\n${out.style}\n${out.lyrics}`);
    assert.ok(out.style.length <= 1000, out.style);
    assert.ok(out.lyrics.length <= 5000);
    assert.ok(out.exclude.length <= 1000);
  }
});

test("the lead gets every slot, up front", () => {
  for (const seed of seeds) {
    const { style, lyrics, parts } = E.generate({ seed });
    const L = parts.lead;
    for (const slot of ["source", "character", "register", "motif", "articulation", "human"]) {
      assert.ok(style.includes(L[slot]), `${slot} missing: ${style}`);
    }
    // Placement always reaches Suno: in the style field or the intro tag.
    assert.ok(style.includes(L.placement) || lyrics.includes(L.placement.split(",")[0]), style);
    assert.ok(style.indexOf("lead:") < style.indexOf("bass:") && style.indexOf("bass:") < style.indexOf("drums:"), style);
    const head = style.slice(0, 260);
    for (const slot of ["sync", "twitch", "mode", "slur", "low"]) if (parts.rhythm[slot]) assert.ok(head.includes(parts.rhythm[slot]), `${slot} not in the front tags: ${style}`);
    assert.ok(head.includes(L.handle), `lead handle not in the front tags: ${style}`);
  }
});

test("the lyrics field is section tags only: a real beat switch, or one hypnotic loop", () => {
  let loops = 0;
  for (const seed of seeds) {
    const { lyrics, meta } = E.generate({ seed });
    for (const line of lyrics.split("\n").filter(Boolean)) assert.match(line, /^\[[^\]]+\]$/, line);
    if (meta.mode === "loop") {
      loops++;
      assert.ok(!/Beat Switch|Verse|Hook/.test(lyrics), lyrics);
      assert.ok((lyrics.match(/\[Loop \| /g) || []).length >= 4, lyrics);
    } else if (meta.mode === "switch") assert.match(lyrics, /\[Beat Switch \| /);
    else assert.ok(!/Beat Switch/.test(lyrics));
    assert.ok(lyrics.trimEnd().endsWith("[End]"));
  }
  assert.ok(loops > seeds.length * 0.28 && loops < seeds.length * 0.42, `${loops} loops`);
});

test("vocals are always excluded; sliders follow the v6 research", () => {
  const out = E.generate({ seed: 7 });
  assert.ok(out.exclude.startsWith("vocals, singing, rap, choir"));
  assert.ok(out.exclude.length <= 1000);
  for (const g of ["jazz", "blues", "EDM", "techno", "elevator music"]) assert.ok(out.exclude.includes(g), g);
  for (const seed of seeds.slice(0, 300)) {
    const { sliders, settings, meta } = E.generate({ seed });
    assert.ok(sliders.weirdness >= 45 && sliders.weirdness <= 65, `${meta.mode} ${sliders.weirdness}`);
    assert.ok(sliders.styleInfluence >= 65 && sliders.styleInfluence <= 75);
    assert.strictEqual(settings.variety, "Off");
    if (meta.mode !== "switch") assert.strictEqual(settings.model, "v6");
  }
});

test("rolls are one of one", () => {
  const fps = new Set(seeds.map((seed) => E.generate({ seed }).fingerprint));
  assert.ok(fps.size === seeds.length, `${fps.size} unique of ${seeds.length}`);
});

test("clash rules hold", () => {
  for (const seed of seeds) {
    const { parts } = E.generate({ seed });
    const { lead, bass, switch: sw } = parts;
    if (lead.dry) assert.ok(!/chorus|detuned/.test(lead.character), lead.character);
    if (lead.is808) assert.match(bass.source, /sine sub|sub that only/);
    if (/five|six|seven|run/.test(lead.motif)) assert.ok(!/one held|one anticipated/.test(lead.articulation));
    if (/go dry and the lead goes huge/.test(sw.what)) assert.ok(lead.dry);
  }
});

test("every roll has a counter instrument and twitch percussion; most keep the percussion color", () => {
  let colors = 0;
  for (const seed of seeds) {
    const { style, lyrics, parts, meta } = E.generate({ seed });
    assert.notStrictEqual(parts.counter.family, parts.lead.family);
    // The counter reaches Suno in the style field, or in the hook tag when the field runs long.
    if (meta.mode !== "loop") assert.ok(style.includes(`counter: ${parts.counter.source}`) || lyrics.includes(parts.counter.handle), style);
    if (style.includes(parts.drums.color)) colors++;
    assert.ok(lyrics.includes(`${parts.drums.twitch.sound} ${parts.drums.twitch.rhythm}`), lyrics);
  }
  assert.ok(colors > seeds.length * 0.8, `${colors} of ${seeds.length} keep the percussion color`);
});

test("South Side brass carries low brass most of the time, never trumpets", () => {
  let n = 0, brass = 0;
  for (const seed of seeds) {
    const { meta, parts, style } = E.generate({ seed });
    assert.ok(!/trumpet/.test(style), style);
    if (meta.lane !== "South Side brass") continue;
    n++;
    if (parts.lead.family === "brass" || parts.counter.family === "brass") brass++;
  }
  assert.ok(brass > n * 0.55, `${brass} of ${n}`);
});

test("never two tubas, never a low lead over a horn bass", () => {
  for (const seed of seeds) {
    const { lead, bass } = E.generate({ seed }).parts;
    if (/tuba|sousaphone/.test(lead.source)) assert.ok(!/tuba|sousaphone/.test(bass.source), `${lead.source} / ${bass.source}`);
    if (lead.low) assert.ok(!/tuba|sousaphone/.test(bass.source), `${lead.register} / ${bass.source}`);
  }
});
