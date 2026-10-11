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

test("every grammar entry passes the guard", () => {
  const walk = (o, path) => {
    if (typeof o === "string") assert.deepStrictEqual(Guard.check(o), [], `${path}: ${o}`);
    else if (o && typeof o === "object") for (const k of Object.keys(o)) if (k !== "EXCLUDE") walk(o[k], `${path}.${k}`);
  };
  walk(G, "G");
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
    for (const slot of ["register", "sync", "twitch", "cadence", "mode"]) assert.ok(head.includes(parts.rhythm[slot]), `${slot} not in the front tags: ${style}`);
  }
});

test("the lyrics field is section tags only: a real beat switch, or one hypnotic loop", () => {
  let loops = 0;
  for (const seed of seeds) {
    const { lyrics, meta } = E.generate({ seed });
    for (const line of lyrics.split("\n").filter(Boolean)) assert.match(line, /^\[[^\]]+\]$/, line);
    if (meta.mode === "loop") {
      loops++;
      assert.ok(!/Beat Switch/.test(lyrics));
      assert.ok((lyrics.match(/\[Loop \| /g) || []).length >= 4, lyrics);
    } else assert.match(lyrics, /\[Beat Switch \| /);
    assert.ok(lyrics.trimEnd().endsWith("[End]"));
  }
  assert.ok(loops > seeds.length * 0.3 && loops < seeds.length * 0.5, `${loops} loops`);
});

test("vocals are always excluded; sliders follow the v6 research", () => {
  const out = E.generate({ seed: 7 });
  assert.ok(out.exclude.startsWith("vocals, singing, rap, choir"));
  assert.ok(out.exclude.length <= 1000);
  for (const g of ["jazz", "blues", "EDM", "techno", "elevator music"]) assert.ok(out.exclude.includes(g), g);
  for (const seed of seeds.slice(0, 300)) {
    const { sliders, settings } = E.generate({ seed });
    assert.ok(sliders.weirdness >= 55 && sliders.weirdness <= 65);
    assert.ok(sliders.styleInfluence >= 65 && sliders.styleInfluence <= 75);
    assert.strictEqual(settings.variety, "Off");
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
    const { style, lyrics, parts } = E.generate({ seed });
    assert.notStrictEqual(parts.counter.family, parts.lead.family);
    assert.ok(style.includes(`counter: ${parts.counter.source}`), style);
    if (style.includes(parts.drums.color)) colors++;
    assert.ok(`${style}\n${lyrics}`.includes(parts.drums.twitch.sound));
  }
  assert.ok(colors > seeds.length * 0.8, `${colors} of ${seeds.length} keep the percussion color`);
});

test("South Side brass always carries brass", () => {
  for (const seed of seeds) {
    const { meta, parts } = E.generate({ seed });
    if (meta.lane === "South Side brass") assert.ok(parts.lead.family === "brass" || parts.counter.family === "brass");
  }
});

test("never two tubas, never a low lead over a horn bass", () => {
  for (const seed of seeds) {
    const { lead, bass } = E.generate({ seed }).parts;
    if (/tuba|sousaphone/.test(lead.source)) assert.ok(!/tuba|sousaphone/.test(bass.source), `${lead.source} / ${bass.source}`);
    if (lead.low) assert.ok(!/tuba|sousaphone/.test(bass.source), `${lead.register} / ${bass.source}`);
  }
});
