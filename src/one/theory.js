/* ONE OF ONE — keys and chord loops. Minor-heavy, hip hop canon only. */
"use strict";

const OneTheory = (() => {
  const SHARP = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];
  const FLAT = ["C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B"];
  const PC = { C: 0, "C#": 1, Db: 1, D: 2, "D#": 3, Eb: 3, E: 4, F: 5, "F#": 6, Gb: 6, G: 7, "G#": 8, Ab: 8, A: 9, "A#": 10, Bb: 10, B: 11 };
  const SHARP_KEYS = { minor: new Set(["E", "B", "F#", "C#", "G#", "D#", "A#"]), major: new Set(["G", "D", "A", "E", "B", "F#"]) };
  const MINOR_KEYS = SHARP.slice();
  const MAJOR_KEYS = FLAT.slice();

  // [semitones above tonic, suffix]. Short loops; the riff does the talking.
  const LOOPS = [
    { tonality: "minor", roman: "i – ♭VI", chords: [[0, "m"], [8, ""]] },
    { tonality: "minor", roman: "i – ♭II", chords: [[0, "m"], [1, ""]] },
    { tonality: "minor", roman: "i (pedal)", chords: [[0, "m"]] },
    { tonality: "minor", roman: "i – ♭VI – V", chords: [[0, "m"], [8, ""], [7, ""]] },
    { tonality: "minor", roman: "i – ♭VII – ♭VI – V", chords: [[0, "m"], [10, ""], [8, ""], [7, ""]] },
    { tonality: "minor", roman: "i – iv", chords: [[0, "m"], [5, "m"]] },
    { tonality: "minor", roman: "i7 – IV7", chords: [[0, "m7"], [5, "7"]] },
    { tonality: "minor", roman: "i9 – ♭VImaj7", chords: [[0, "m9"], [8, "maj7"]] },
    { tonality: "minor", roman: "i – v – ♭VI", chords: [[0, "m"], [7, "m"], [8, ""]] },
    { tonality: "major", roman: "IVmaj7 – iii7 – vi9", chords: [[5, "maj7"], [4, "m7"], [9, "m9"]] },
    { tonality: "major", roman: "I – ♭VII", chords: [[0, ""], [10, ""]] },
  ];

  const name = (pc, tonic, tonality) => (SHARP_KEYS[tonality].has(tonic) ? SHARP : FLAT)[((pc % 12) + 12) % 12];

  function harmony(r, pick, avoidKeys = []) {
    // Minor about 85% of the time.
    const pool = LOOPS.filter((l) => (r() < 0.85 ? l.tonality === "minor" : l.tonality === "major"));
    const loop = pick(pool.length ? pool : LOOPS, r);
    const keys = loop.tonality === "minor" ? MINOR_KEYS : MAJOR_KEYS;
    let tonic = pick(keys, r);
    for (let i = 0; i < 8 && avoidKeys.includes(`${tonic} ${loop.tonality}`); i++) tonic = pick(keys, r);
    const chords = loop.chords.map(([o, q]) => name(PC[tonic] + o, tonic, loop.tonality) + q);
    return { key: `${tonic} ${loop.tonality}`, roman: loop.roman, chords };
  }

  return { harmony };
})();

if (typeof module !== "undefined") module.exports = OneTheory;
