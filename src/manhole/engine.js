/* MANHOLE engine — one-of-a-kind minimalist instrumental prompts for Suno v6.
 * Every roll is a cross of two reference DNAs plus a waltz cadence, an
 * on/off-the-beat pocket, a garage-sewer room, a found-object sound and one
 * house rule. The fingerprint names that combination so the page can refuse
 * to repeat it. */
"use strict";

const ManholeEngine = (() => {
  const D = typeof ManholeData !== "undefined" ? ManholeData : require("./data.js");

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

  // ---------------------------------------------------------------- harmony
  const SHARP = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];
  const FLAT = ["C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B"];
  const PC = { C: 0, "C#": 1, Db: 1, D: 2, "D#": 3, Eb: 3, E: 4, F: 5, "F#": 6, Gb: 6, G: 7, "G#": 8, Ab: 8, A: 9, "A#": 10, Bb: 10, B: 11 };
  const SHARP_KEYS = { minor: new Set(["E", "B", "F#", "C#", "G#", "D#", "A#"]), major: new Set(["G", "D", "A", "E", "B", "F#"]) };
  const noteName = (pc, tonic, tonality) => (SHARP_KEYS[tonality].has(tonic) ? SHARP : FLAT)[((pc % 12) + 12) % 12];

  function chooseHarmony(dna, r) {
    const prog = pick(D.PROGRESSIONS.filter((p) => dna.tonalities.includes(p.tonality)), r);
    const tonic = pick(prog.tonality === "minor" ? D.MINOR_KEYS : D.MAJOR_KEYS, r);
    const chords = prog.chords.map(([o, q]) => noteName(PC[tonic] + o, tonic, prog.tonality) + q);
    return { prog, tonic, key: `${tonic} ${prog.tonality}`, chords };
  }

  // ---------------------------------------------------------------- style
  function buildStyle(c) {
    const parts = [];
    const add = (text, pri) => parts.push({ text, pri });
    add(c.opener, 0);
    add(`crossed with ${c.trait}`, 0);
    add(D.INSTRUMENTAL, 0);
    add(c.waltz, 0);
    add(c.onOff, 0);
    add(c.room, 0);
    add(`${c.lead} playing ${c.harmony.prog.color} in ${c.harmony.key}, ${c.grit}, ${c.playing}, looping obsessively`, 0);
    add(`house rule: ${c.rule}`, 0);
    add(`beat switch at the midpoint: ${c.beatSwitch}`, 0);
    add(c.found, 1);
    add(c.low, 2);
    add(c.mix, 2);
    add(c.minimal, 3);
    const tail = `${c.bpm} BPM ${c.dna.feel}, hard stop ending`;

    const budget = D.STYLE_LIMIT - tail.length - 2;
    const len = (list) => list.map((p) => p.text).join(", ").length;
    const kept = [...parts];
    for (let pri = 3; pri >= 1 && len(kept) > budget; pri--) {
      for (let i = kept.length - 1; i >= 0 && len(kept) > budget; i--) if (kept[i].pri === pri) kept.splice(i, 1);
    }
    return `${kept.map((p) => p.text).join(", ")}, ${tail}`;
  }

  // ---------------------------------------------------------------- arrangement
  function buildArrangement(c) {
    const m = `the ${c.leadName}`;
    const chords = c.harmony.chords.join(" – ");
    const S = [];
    const push = (name, desc) => S.push({ tag: `[${name}: ${desc}]`, name });
    for (const step of c.structure.steps) {
      if (step === "intro") push("Intro", `${m} alone in the room, 2 bars, ${chords}, ${c.found}`);
      else if (step === "hook") push("Hook", `full beat on the beat, ${m} looping obsessively, ${c.waltz}, 8 bars`);
      else if (step === "verseOn") push("Verse", `drums and 808 locked on the grid, ${m} sparse, house rule: ${c.rule}, 16 bars`);
      else if (step === "verseOff") push("Verse", `drums slide off the beat, ${c.onOff}, ${m} answering every 4th bar, 16 bars`);
      else if (step === "verseStop") push("Verse", `drums stop dead every 2 bars and slam back on the one, ${m} in the gaps, 16 bars`);
      else if (step === "break") push("Break", `808 alone with ${c.found}, 2 bars, then a half-beat of silence`);
      else if (step === "switch") push("Beat Switch", `${c.beatSwitch}, ${chords} darker, 8 bars`);
      else if (step === "switchHook") push("Hook", `on the switched beat, ${m} twice as heavy, ${c.waltz}, house rule: ${c.rule}, 8 bars`);
      else if (step === "outro") push("Outro", `${m} for 2 bars over the 808, then hard stop`);
    }
    S.push({ tag: "[End]", name: "End" });
    return S;
  }

  function lint(text) {
    const found = [];
    for (const rule of D.LINT) {
      const m = text.match(rule.re);
      if (m) found.push({ level: rule.level, message: `"${m[0].trim()}" — ${rule.msg}` });
    }
    return found;
  }

  // ---------------------------------------------------------------- main
  function generate(input = {}) {
    const seed = input.seed >>> 0;
    const r = rng(seed);
    const dna = D.DNA.find((d) => d.id === input.dna) || pick(D.DNA, r);
    let second = pick(D.DNA.filter((d) => d.id !== dna.id && d.refs !== dna.refs), r);
    const harmony = chooseHarmony(dna, r);
    const [lo, hi] = dna.bpm;
    const bpm = lo + Math.round((r() * (hi - lo)) / 2) * 2;
    const structure = D.STRUCTURES.find((s) => s.id === input.structure) || pick(D.STRUCTURES, r);

    // Lead: the DNA's own riff half the time, the shared pool otherwise.
    const leadName = r() < 0.5 ? pick(dna.leads, r) : pick(D.LEADS, r);
    const c = {
      dna, second, harmony, bpm, structure, leadName,
      opener: pick(dna.openers, r),
      trait: second.trait,
      lead: `${pick(D.ADJECTIVES, r)} ${leadName}`,
      grit: pick(D.GRIT, r),
      playing: pick(D.PLAYING, r),
      waltz: pick(D.WALTZ, r),
      onOff: pick(D.ON_OFF, r),
      room: pick(D.ROOMS, r),
      found: pick(D.FOUND, r),
      rule: pick(D.RULES, r),
      beatSwitch: pick(D.SWITCH, r),
      low: pick(dna.low, r),
      mix: pick(D.MIX, r),
      minimal: pick(D.MINIMAL, r),
    };
    const styleText = buildStyle(c);
    const sections = buildArrangement(c);
    const lyricsText = sections.map((s) => s.tag).join("\n\n");
    const warnings = [
      ...lint(styleText).map((w) => ({ ...w, message: `style: ${w.message}` })),
      ...lint(lyricsText).map((w) => ({ ...w, message: `arrangement: ${w.message}` })),
    ];
    if (styleText.length > D.STYLE_LIMIT) warnings.push({ level: "warn", message: "style exceeds 1,000 characters" });
    if (lyricsText.length > D.LYRICS_LIMIT) warnings.push({ level: "warn", message: "arrangement exceeds 5,000 characters" });

    // The parts that make a roll audibly itself. Two rolls with the same
    // fingerprint would sound like cousins, so the page never repeats one.
    const fingerprint = [dna.id, second.id, D.RULES.indexOf(c.rule), D.FOUND.indexOf(c.found), leadName, harmony.key].join("|");

    return {
      styleText, lyricsText, sections, fingerprint,
      excludeText: D.EXCLUDE.join(", "),
      settings: input.wild ? D.SETTINGS.wild : D.SETTINGS.radio,
      harmony: { key: harmony.key, roman: harmony.prog.roman, chords: harmony.chords, cadence: harmony.prog.cadence },
      parts: {
        rule: c.rule, found: c.found, room: c.room, waltz: c.waltz, onOff: c.onOff, beatSwitch: c.beatSwitch,
        lead: `${c.lead}, ${c.grit}, ${c.playing}`, trait: c.trait,
      },
      warnings,
      meta: {
        seed, bpm, dna: dna.id, dnaLabel: dna.label, dnaRefs: dna.refs, second: second.id, secondLabel: second.label, secondRefs: second.refs,
        structure: structure.id, structureLabel: structure.label, length: structure.length, styleChars: styleText.length,
      },
    };
  }

  return { generate, lint, D };
})();

if (typeof module !== "undefined") module.exports = ManholeEngine;
