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
  const VOICE = /\b(vocal\w*|vocalist|sing\w*|sung|hum|humming|hummed|lyric\w*|ad-?lib\w*|falsetto|harmonies|rap|rapper|rapped|rapping|voices?|doubles|spoken|whisper\w*|belt\w*|auto-?tune\w*|melismatic|hook)\b/i;
  for (const pack of Object.values(require("../src/one/genres.js"))) {
    const allow = pack.grammar.VOCAL
      ? new RegExp([pack.grammar.ALLOW && pack.grammar.ALLOW.source, VOICE.source].filter(Boolean).join("|"), "i")
      : pack.grammar.ALLOW;
    const walkAllow = (o, path) => {
      if (typeof o === "string") assert.deepStrictEqual(Guard.check(o, allow), [], `${path}: ${o}`);
      else if (o && typeof o === "object" && !(o instanceof RegExp)) for (const k of Object.keys(o)) if (k !== "EXCLUDE" && k !== "label" && typeof o[k] !== "function") walkAllow(o[k], `${path}.${k}`);
    };
    walkAllow(pack.grammar, `genres.${pack.id}`);
  }
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
    const { lyrics, style, parts, meta, settings } = E.generate({ seed, genre: g.id });
    assert.ok(!/returns/.test(lyrics), lyrics);
    if (meta.mode !== "switch") continue;
    if (settings.instrumental === "Off") {
      // A vocal song owns its own structure; the head tag must still match it.
      assert.ok(style.includes(parts.switch.tag) && /\[Beat Switch/.test(lyrics), style);
      continue;
    }
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
      if (o.settings.instrumental === "Off") {
        assert.ok(!/instrumental/.test(o.style), o.style);
        const items = o.exclude.split(", ");
        for (const w of ["vocals", "singing", "rap", "humming", "background vocals", "chanting"]) assert.ok(!items.includes(w), o.exclude);
        for (const w of ["choir", "crowd vocals", "gospel"]) assert.ok(items.includes(w), o.exclude);
        continue;
      }
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
    for (const slot of ["source", "character", "register", "motif", "articulation"]) {
      assert.ok(style.includes(L[slot]), `${slot} missing: ${style}`);
    }
    // The human-feel clause is the last thing trimmed; it may only go when the field is full.
    assert.ok(style.includes(L.human) || style.length >= 960, `human missing with room to spare: ${style}`);
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

test("Prof holds two moods with their own tempo and words", () => {
  const seen = { lowtide: 0, goodtime: 0 };
  for (const seed of seeds.slice(0, 600)) {
    const { meta, parts, style } = E.generate({ seed, genre: "prof" });
    const lane = meta.lane === "Low tide crawl" ? "lowtide" : "goodtime";
    seen[lane]++;
    if (lane === "lowtide") assert.ok(meta.bpm <= 92, `${meta.bpm}`);
    else assert.ok(meta.bpm >= 94, `${meta.bpm}`);
    assert.ok(!/whistle|circus|pirate|novelty/i.test(style), style);
    assert.ok(parts.lead.family !== "siren_synth" || lane === "goodtime");
  }
  assert.ok(seen.lowtide > 150 && seen.goodtime > 150, JSON.stringify(seen));
});

test("spin-offs borrow experimental leads about a third of the time, with an in-genre counter", () => {
  const own = require("../src/one/genres.js");
  for (const g of E.genres()) {
    if (g.id === "hiphop" || g.id === "cross" || own[g.id].grammar.VOCAL) continue;
    let guests = 0, n = 0;
    for (const seed of seeds.slice(0, 500)) {
      const { parts } = E.generate({ seed, genre: g.id });
      n++;
      if (parts.lead.guest) {
        guests++;
        assert.ok(own[g.id].grammar.FAMILIES[parts.counter.family], `${g.id}: guest lead needs an in-genre counter, got ${parts.counter.family}`);
      }
    }
    assert.ok(guests > n * 0.1 && guests < n * 0.45, `${g.id}: ${guests} of ${n} guest leads`);
  }
});

test("Bare minimum: one voice, a kick trick, nothing else", () => {
  for (const seed of seeds.slice(0, 500)) {
    const { style, lyrics, meta } = E.generate({ seed, genre: "bare" });
    assert.ok(!/counter:/.test(style), style);
    assert.ok(/stutter|double kick|trips|double hit/.test(style), style);
    assert.ok(meta.mode !== "switch");
    assert.ok(/kick trick:|stutter|double kick|trips|double hit/.test(lyrics), lyrics);
  }
});


test("the bounce lane rolls bright: its own instruments, claps, major keys often", () => {
  let n = 0, major = 0, fam = 0;
  for (const seed of seeds) {
    const { meta, parts, style } = E.generate({ seed });
    if (meta.lane !== "Bounce radio") continue;
    n++;
    if (/major/.test(meta.key)) major++;
    if (parts.lead.family === "bounce") fam++;
    assert.ok(/clap/.test(parts.drums.snare), parts.drums.snare);
    assert.ok(style.includes("loud radio-ready club mix"), style);
  }
  assert.ok(n > seeds.length * 0.2, `${n} bounce rolls`);
  assert.ok(major > n * 0.3, `${major} major of ${n}`);
  assert.ok(fam > n * 0.45, `${fam} bounce-family leads of ${n}`);
});
