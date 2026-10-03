/* MANHOLE engine — one-of-a-kind minimalist instrumental prompts for Suno v6.
 * Every roll is a cross of two reference DNAs plus a triplet pocket, an
 * on/off-the-beat pocket, one house rule and a beat switch, written in plain
 * modern hip hop production language. The fingerprint names that combination so the page can refuse
 * to repeat it. */
"use strict";

const ManholeEngine = (() => {
  const D = typeof ManholeData !== "undefined" ? ManholeData : require("./data.js");
  const V = typeof SharedVibe !== "undefined" ? SharedVibe : require("../shared/vibe.js");

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
    // Rhythm identity first, as short tags: Suno weights the front and reads
    // the field as tags. Bar-by-bar detail lives in the arrangement.
    add("experimental minimalist hip hop", 0);
    add(c.glitchTag, 0);
    add(c.hatTag, 0);
    add(c.syncTag, 0);
    add("hard beat switch", 0);
    add(c.opener, 1);
    add(`crossed with ${c.trait}`, 2);
    add(D.INSTRUMENTAL, 0);
    add(c.triplet, 1);
    // Every sound is one vivid phrase, never a bare instrument name.
    add(c.bass, 0);
    add(`${c.lead} looping ${c.harmony.prog.color} in ${c.harmony.key}`, 0);
    add(c.perc, 0);
    add(c.mix, 1);
    const tail = `${c.bpm} BPM ${c.dna.feel}`;

    const budget = Math.min(D.STYLE_LIMIT, 850) - tail.length - 2;
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
      if (step === "intro") push("Intro", `${m} alone, 2 bars, ${chords}, drums slam in on bar 3`);
      else if (step === "hook") push("Hook", `full beat, twitchy stuttering hats, ${c.perc}, ${m} looping, ${c.triplet}, 8 bars`);
      else if (step === "verseOn") push("Verse", `stripped drums and 808 locked on the grid, ${m} sparse, house rule: ${c.rule}, 16 bars`);
      else if (step === "verseOff") push("Verse", `glitchy chopped drums sliding off the beat, ${c.onOff}, ${m} every 4th bar, 16 bars`);
      else if (step === "verseStop") push("Verse", `drums stop dead every 2 bars and slam back on the one, syncopated kicks, ${m} in the gaps, 16 bars`);
      else if (step === "break") push("Break", `drums stutter and cut out, 808 alone for 2 bars, then a half-beat of silence`);
      else if (step === "switch") push("Beat Switch", `${c.beatSwitch}, glitchy percussion, 8 bars`);
      else if (step === "switchHook") push("Hook", `on the switched beat, heavier, twitchy hats, ${c.onOff}, ${m}, house rule: ${c.rule}, 8 bars`);
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

    // Lead, bass and percussion come from the shared vivid-sound bank.
    const leadName = pick(V.LEADS, r);
    const c = {
      dna, second, harmony, bpm, structure, leadName,
      opener: pick(dna.openers, r),
      trait: second.trait,
      lead: leadName,
      bass: pick(V.BASS, r),
      glitchTag: pick(V.GLITCH_TAGS, r),
      hatTag: pick(V.HAT_TAGS, r),
      syncTag: pick(V.SYNC_TAGS, r),
      triplet: pick(D.TRIPLET, r),
      onOff: pick(D.ON_OFF, r),
      rule: pick(D.RULES, r),
      beatSwitch: pick(D.SWITCH, r),
      mix: pick(D.MIX, r),
    };
    {
      // Percussion must not echo the glitch tag's first word.
      const head = c.glitchTag.split(" ")[0];
      const pool = V.PERC.filter((p) => !p.startsWith(head));
      c.perc = pick(pool.length ? pool : V.PERC, r);
    }
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
    const fingerprint = [dna.id, second.id, D.RULES.indexOf(c.rule), D.SWITCH.indexOf(c.beatSwitch), leadName, harmony.key].join("|");

    return {
      styleText, lyricsText, sections, fingerprint,
      excludeText: D.EXCLUDE.join(", "),
      settings: input.wild ? D.SETTINGS.wild : D.SETTINGS.radio,
      harmony: { key: harmony.key, roman: harmony.prog.roman, chords: harmony.chords, cadence: harmony.prog.cadence },
      parts: {
        rule: c.rule, triplet: c.triplet, onOff: c.onOff, beatSwitch: c.beatSwitch,
        lead: c.lead, bass: c.bass, perc: c.perc, trait: c.trait,
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
