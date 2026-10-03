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
    else for (const k of Object.keys(o)) walk(o[k], `${path}.${k}`);
  };
  walk(G, "G");
});

test("every roll is clean and inside Suno's limits", () => {
  for (const seed of seeds) {
    const out = E.generate({ seed });
    assert.deepStrictEqual(out.warnings, [], `${seed}\n${out.style}\n${out.lyrics}`);
    assert.ok(out.style.length <= 1000, out.style);
    assert.ok(out.lyrics.length <= 5000);
    assert.ok(out.exclude.length <= 1000);
  }
});

test("the lead gets every slot, up front", () => {
  for (const seed of seeds) {
    const { style, parts } = E.generate({ seed });
    const L = parts.lead;
    for (const slot of ["source", "character", "register", "motif", "articulation", "human", "placement"]) {
      assert.ok(style.includes(L[slot]), `${slot} missing: ${style}`);
    }
    assert.ok(style.indexOf("lead:") < style.indexOf("bass:") && style.indexOf("bass:") < style.indexOf("drums:"), style);
  }
});

test("the lyrics field is section tags only, with a real beat switch", () => {
  for (const seed of seeds) {
    const { lyrics } = E.generate({ seed });
    for (const line of lyrics.split("\n").filter(Boolean)) assert.match(line, /^\[[^\]]+\]$/, line);
    assert.match(lyrics, /\[Beat Switch \| /);
    assert.ok(lyrics.trimEnd().endsWith("[End]"));
  }
});

test("vocals are always excluded; sliders follow the v6 research", () => {
  const out = E.generate({ seed: 7 });
  assert.ok(out.exclude.startsWith("vocals, singing, rap, choir"));
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
