/* ANOMALY engine — instrumental prompt generator for Suno v6, tuned to
 * today's hip hop and R&B radio. Pure functions over the vocabulary in
 * data.js. Same seed + same options = same package. */
"use strict";

const AnomalyEngine = (() => {
  const D = typeof AnomalyData !== "undefined" ? AnomalyData : require("./data.js");

  // ---------------------------------------------------------------- random
  function rng(seed) {
    let t = (seed >>> 0) || 0x9e3779b9;
    return () => {
      t = (t + 0x6d2b79f5) | 0;
      let r = Math.imul(t ^ (t >>> 15), 1 | t);
      r = (r + Math.imul(r ^ (r >>> 7), 61 | r)) ^ r;
      return ((r ^ (r >>> 14)) >>> 0) / 4294967296;
    };
  }
  const pick = (arr, r) => arr[Math.floor(r() * arr.length)];
  function shuffle(arr, r) {
    const out = [...arr];
    for (let i = out.length - 1; i > 0; i--) {
      const j = Math.floor(r() * (i + 1));
      [out[i], out[j]] = [out[j], out[i]];
    }
    return out;
  }
  // Pick n distinct items.
  const take = (arr, n, r) => shuffle(arr, r).slice(0, n);

  // ---------------------------------------------------------------- theory
  const SHARP = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];
  const FLAT = ["C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B"];
  const PC = { C: 0, "C#": 1, Db: 1, D: 2, "D#": 3, Eb: 3, E: 4, F: 5, "F#": 6, Gb: 6, G: 7, "G#": 8, Ab: 8, A: 9, "A#": 10, Bb: 10, B: 11 };
  const SHARP_KEYS = { minor: new Set(["E", "B", "F#", "C#", "G#", "D#", "A#"]), major: new Set(["G", "D", "A", "E", "B", "F#"]) };
  const noteName = (pc, tonic, tonality) => (SHARP_KEYS[tonality].has(tonic) ? SHARP : FLAT)[((pc % 12) + 12) % 12];
  const spell = (tonic, tonality, [offset, quality]) => noteName(PC[tonic] + offset, tonic, tonality) + quality;

  function chooseHarmony(world, r, avoidKeys = []) {
    const prog = pick(D.PROGRESSIONS.filter((p) => world.tonalities.includes(p.tonality)), r);
    const pool = prog.tonality === "minor" ? D.MINOR_KEYS : D.MAJOR_KEYS;
    let tonic = pick(pool, r);
    // Keep the key moving across rolls: retry a few times if it repeats.
    for (let i = 0; i < 8 && avoidKeys.includes(`${tonic} ${prog.tonality}`); i++) tonic = pick(pool, r);
    const chords = prog.chords.map((c) => spell(tonic, prog.tonality, c));
    return { prog, tonic, tonality: prog.tonality, key: `${tonic} ${prog.tonality}`, chords };
  }

  // ---------------------------------------------------------------- copy
  // Adjective + instrument, never the same adjective twice in a package.
  function makeVoice(pool, adjectives, r, used) {
    let adj = pick(adjectives, r);
    for (let i = 0; i < 6 && used.has(adj); i++) adj = pick(adjectives, r);
    used.add(adj);
    return `${adj} ${pick(pool, r)}`;
  }

  // Keyboard budget: at most one keys instrument, and piano only sometimes.
  const isPiano = (s) => /piano/i.test(s);

  function buildStyle(ctx) {
    const { world, harmony, bpm, r, used } = ctx;
    // Higher priority number = dropped first when the field runs long.
    const parts = [];
    const add = (text, pri) => parts.push({ text, pri });

    // The opener is the identity. It goes first because Suno weights the front.
    add(pick(world.openers, r), 0);
    add(world.genre, 0);
    add(`${pick(D.SWING, r)} drums, ${pick(D.SYNC, r)}`, 1);
    add(pick(world.lowEnd, r), 2);

    let leadPool = r() < 0.7 ? world.leads : D.GLOBAL_LEADS;
    if (r() > D.PIANO_SHARE) leadPool = leadPool.filter((l) => !isPiano(l));
    const lead = makeVoice(leadPool, D.ADJECTIVES, r, used);
    ctx.timbre = pick(D.TIMBRE, r);
    ctx.playing = pick(D.PLAYING, r);
    ctx.space = pick(D.SPACE, r);
    add(`${lead} playing ${harmony.prog.color} in ${harmony.key}, ${ctx.timbre}, ${ctx.playing}, looping obsessively`, 0);
    ctx.motif = `the ${lead.split(",")[0].split(" ").slice(1, 5).join(" ")}`;
    ctx.clash = r() < 0.7 ? pick(world.clash, r) : pick(D.CLASH, r);
    add(`${ctx.clash} cutting in every 4 bars, ${ctx.space}`, 0);

    add(`technical flex: ${ctx.flex}`, 1);
    add(`signature: ${ctx.signature}`, 0);
    add(ctx.glitch, 2);
    add(`beat switch at the midpoint: ${ctx.beatSwitch}`, 0);
    add(pick(world.drums, r), 3);
    add(pick(D.MIX, r), 3);
    add(`${pick(D.ADJECTIVES.filter((x) => !used.has(x)), r)} and ${pick(D.ADJECTIVES.filter((x) => !used.has(x)), r)}`, 4);
    const tail = `${bpm} BPM ${world.feel}, hard stop ending`;

    // Trim one line at a time, least important first, latest first within a tier.
    const budget = D.STYLE_LIMIT - tail.length - 2;
    const len = (list) => list.map((p) => p.text).join(", ").length;
    const kept = [...parts];
    for (let pri = 4; pri >= 1 && len(kept) > budget; pri--) {
      for (let i = kept.length - 1; i >= 0 && len(kept) > budget; i--) if (kept[i].pri === pri) kept.splice(i, 1);
    }
    return `${kept.map((p) => p.text).join(", ")}, ${tail}`;
  }

  // Lyrics field as a proper instrumental arrangement. Every tag says what
  // the beat does; nothing for a voice to sing.
  function buildArrangement(ctx) {
    const { harmony, structure } = ctx;
    const motif = ctx.motif || "the lead riff";
    const chords = harmony.chords.join(" – ");
    const S = [];
    const push = (name, desc) => S.push({ tag: `[${name}: ${desc}]`, name });
    const sig = ctx.signature;

    for (const step of structure.steps) {
      switch (step) {
        case "intro":
          push("Intro", `${motif} alone, 2 bars, ${chords}, drums and 808 fall in on bar 3`);
          break;
        case "hook":
          push("Hook", `full beat, ${motif} loops obsessively, ${ctx.playing}, ${ctx.clash} answers every 4 bars, 8 bars`);
          break;
        case "hookSig":
          push("Hook", `full beat, ${motif} loops obsessively, ${ctx.clash} answers, ${sig}, ${ctx.move}, 8 bars`);
          break;
        case "verse":
          push("Verse", `drums and 808 only, ${motif} low and sparse, open space, 16 bars`);
          break;
        case "verse2":
          push("Verse", `drums and 808, ${ctx.glitch}, ${motif} every 4th bar, 16 bars`);
          break;
        case "verseShort":
          push("Verse", `drums and 808 only, ${motif} sparse, 8 bars`);
          break;
        case "pre":
          push("Pre-Hook", `808 out, ${motif} holds the tension chord, ${ctx.flex}, 4 bars`);
          break;
        case "switch":
          push("Beat Switch", `${ctx.beatSwitch}, ${ctx.glitch}, ${chords} darker, 8 bars`);
          break;
        case "switchHook":
          push("Hook", `on the switched beat, ${motif} twice as heavy, ${ctx.clash} doubled, ${sig}, ${ctx.move}, 8 bars`);
          break;
        case "break":
          push("Break", `808 alone for 2 bars, then a half-beat of total silence`);
          break;
        case "bridge":
          push("Bridge", `half-time drums, ${motif} reharmonized, ${sig}, 8 bars`);
          break;
        case "outro":
          push("Outro", `${motif} for 2 bars over the 808, then hard stop`);
          break;
        default:
          break;
      }
    }
    S.push({ tag: "[End]", name: "End" });
    return S;
  }

  // ---------------------------------------------------------------- lint
  function lint(text) {
    const found = [];
    for (const rule of D.LINT) {
      const m = text.match(rule.re);
      if (m) found.push({ level: rule.level, message: `"${m[0]}" — ${rule.msg}` });
    }
    return found;
  }

  // ---------------------------------------------------------------- main
  function generate(input = {}) {
    const seed = input.seed >>> 0;
    const r = rng(seed);
    const worlds = input.family ? D.WORLDS.filter((w) => w.family === input.family) : D.WORLDS;
    const world = D.WORLDS.find((w) => w.id === input.world) || pick(worlds, r);
    const harmony = chooseHarmony(world, r, input.avoidKeys || []);
    const [lo, hi] = world.bpm;
    const bpm = input.bpm || lo + Math.round((r() * (hi - lo)) / 2) * 2;
    const structure = D.STRUCTURES.find((s) => s.id === input.structure) || pick(D.STRUCTURES, r);
    const used = new Set();
    const ctx = {
      world, harmony, bpm, r, used, structure,
      glitch: pick(D.GLITCH, r),
      signature: pick(D.SIGNATURES, r),
      move: pick(world.moves, r),
      flex: pick(D.TECH_FLEX, r),
      beatSwitch: pick(D.BEAT_SWITCH, r),
    };
    const styleText = buildStyle(ctx);
    const sections = buildArrangement(ctx);
    const lyricsText = sections.map((s) => s.tag).join("\n\n");
    const excludeText = D.EXCLUDE.join(", ");
    const settings = input.wild ? D.SETTINGS.wild : D.SETTINGS.radio;
    const warnings = [...lint(styleText).map((w) => ({ ...w, message: `style: ${w.message}` })), ...lint(lyricsText).map((w) => ({ ...w, message: `arrangement: ${w.message}` }))];
    if (lyricsText.length > D.LYRICS_LIMIT) warnings.push({ level: "warn", message: "arrangement exceeds the lyrics limit" });

    return {
      styleText,
      lyricsText,
      excludeText,
      sections,
      settings,
      harmony: { key: harmony.key, progression: harmony.prog.name, roman: harmony.prog.roman, chords: harmony.chords, cadence: harmony.prog.cadence },
      signature: ctx.signature,
      move: ctx.move,
      clash: ctx.clash,
      timbre: ctx.timbre,
      playing: ctx.playing,
      space: ctx.space,
      flex: ctx.flex,
      beatSwitch: ctx.beatSwitch,
      glitch: ctx.glitch,
      warnings,
      meta: {
        seed, world: world.id, worldLabel: world.label, spirit: world.spirit, family: world.family, bpm,
        structure: structure.id, structureLabel: structure.label, length: structure.length,
        styleChars: styleText.length, descriptors: styleText.split(", ").length,
      },
    };
  }

  return { generate, lint, D };
})();

if (typeof module !== "undefined") module.exports = AnomalyEngine;
