'use strict';
/** Keys and chord loops. Minor-heavy hip hop canon; the neo-soul lane raises the major/Dorian share. */

const SHARP = ['C', 'C#', 'D', 'D#', 'E', 'F', 'F#', 'G', 'G#', 'A', 'A#', 'B'];
const FLAT = ['C', 'Db', 'D', 'Eb', 'E', 'F', 'Gb', 'G', 'Ab', 'A', 'Bb', 'B'];
const PC = { C: 0, 'C#': 1, Db: 1, D: 2, 'D#': 3, Eb: 3, E: 4, F: 5, 'F#': 6, Gb: 6, G: 7, 'G#': 8, Ab: 8, A: 9, 'A#': 10, Bb: 10, B: 11 };
const SHARP_KEYS = { minor: new Set(['E', 'B', 'F#', 'C#', 'G#', 'D#', 'A#']), major: new Set(['G', 'D', 'A', 'E', 'B', 'F#']) };

/** [semitones above tonic, suffix]. Short loops; the riff does the talking. */
const LOOPS = [
  { tonality: 'minor', roman: 'i - bVI', chords: [[0, 'm'], [8, '']], weight: 3 },
  { tonality: 'minor', roman: 'i - bII', chords: [[0, 'm'], [1, '']], weight: 2 },
  { tonality: 'minor', roman: 'i (pedal)', chords: [[0, 'm']], weight: 2 },
  { tonality: 'minor', roman: 'i - bVI - V', chords: [[0, 'm'], [8, ''], [7, '']], weight: 2 },
  { tonality: 'minor', roman: 'i - bVII - bVI - V', chords: [[0, 'm'], [10, ''], [8, ''], [7, '']], weight: 1 },
  { tonality: 'minor', roman: 'i - iv', chords: [[0, 'm'], [5, 'm']], weight: 2 },
  { tonality: 'minor', roman: 'i7 - IV7 (Dorian)', chords: [[0, 'm7'], [5, '7']], weight: 2, dorian: true },
  { tonality: 'minor', roman: 'i9 - bVImaj7', chords: [[0, 'm9'], [8, 'maj7']], weight: 2, soul: true },
  { tonality: 'minor', roman: 'i - v - bVI', chords: [[0, 'm'], [7, 'm'], [8, '']], weight: 1 },
  { tonality: 'minor', roman: 'i11 - iv9 (Dorian vamp)', chords: [[0, 'm11'], [5, 'm9']], weight: 1, dorian: true, soul: true },
  { tonality: 'major', roman: 'IVmaj7 - iii7 - vi9', chords: [[5, 'maj7'], [4, 'm7'], [9, 'm9']], weight: 2, soul: true },
  { tonality: 'major', roman: 'I - bVII', chords: [[0, ''], [10, '']], weight: 1 },
  { tonality: 'major', roman: 'I - V - vi - IV', chords: [[0, ''], [7, ''], [9, 'm'], [5, '']], weight: 1 },
  { tonality: 'major', roman: 'Imaj7 - ii7', chords: [[0, 'maj7'], [2, 'm7']], weight: 1, soul: true },
  { tonality: 'major', roman: 'I - bVII - IV (Mixolydian strut)', chords: [[0, ''], [10, ''], [5, '']], weight: 2, strut: true },
  { tonality: 'major', roman: 'I7 - IV7 (dominant stomp)', chords: [[0, '7'], [5, '7']], weight: 2, strut: true },
  { tonality: 'major', roman: 'I - IV (two-chord stomp)', chords: [[0, ''], [5, '']], weight: 2, strut: true },
  { tonality: 'minor', roman: 'i - bII - bvii (Phrygian)', chords: [[0, 'm'], [1, ''], [10, 'm']], weight: 2 },
  { tonality: 'minor', roman: 'i - IV (Dorian two-chord)', chords: [[0, 'm'], [5, '']], weight: 2, dorian: true },
  { tonality: 'major', roman: 'I - II (Lydian)', chords: [[0, ''], [2, '']], weight: 1, lydian: true },
  { tonality: 'minor', roman: 'i - bIII - bVII (minor strut)', chords: [[0, 'm'], [3, ''], [10, '']], weight: 2, strut: true },
];

/** The scale named in the head: the mode the loop implies, so the written key is not always "minor". */
function scaleName(loop, r) {
  if (loop.tonality === 'minor') {
    if (/bII\b/.test(loop.roman)) return 'Phrygian';
    if (loop.dorian) return 'Dorian';
    if (/- V\b/.test(loop.roman)) return 'harmonic minor';
    const x = r();
    return x < 0.2 ? 'Phrygian' : x < 0.35 ? 'Dorian' : 'minor';
  }
  if (loop.lydian) return 'Lydian';
  if (/bVII|7 /.test(loop.roman)) return 'Mixolydian';
  return r() < 0.2 ? 'Lydian' : 'major';
}

const name = (pc, tonic, tonality) => (SHARP_KEYS[tonality].has(tonic) ? SHARP : FLAT)[((pc % 12) + 12) % 12];

/**
 * Pick a key and loop. majorShare: chance of a major loop. soulShare: chance of preferring
 * extended (7th/9th/Dorian) loops. avoidKeys: recent keys to steer away from.
 */
function harmony(r, pick, { avoidKeys = [], majorShare = 0.15, soulShare = 0, forceKey = null, strut = false } = {}) {
  const wantMajor = r() < majorShare;
  const wantSoul = r() < soulShare;
  let pool = LOOPS.filter((l) => l.tonality === (wantMajor ? 'major' : 'minor'));
  if (strut && pool.some((l) => l.strut)) pool = pool.filter((l) => l.strut || (wantMajor && !l.soul));
  else if (wantSoul && pool.some((l) => l.soul || l.dorian)) pool = pool.filter((l) => l.soul || l.dorian);
  const expanded = pool.flatMap((l) => Array(l.weight).fill(l));
  const loop = pick(expanded, r);
  const keys = loop.tonality === 'minor' ? SHARP.slice() : FLAT.slice();
  let tonic = pick(keys, r);
  if (forceKey) {
    const m = String(forceKey).trim().match(/^([A-Ga-g][#b]?)\s*(harmonic minor|minor|major|min|maj|m|dorian|phrygian|lydian|mixolydian)?/i);
    if (m) {
      tonic = m[1][0].toUpperCase() + (m[1][1] || '');
      const t = (m[2] || 'minor').toLowerCase();
      const tonality = /maj|lydian|mixolydian/.test(t) ? 'major' : 'minor';
      const named = { dorian: 'Dorian', phrygian: 'Phrygian', lydian: 'Lydian', mixolydian: 'Mixolydian', 'harmonic minor': 'harmonic minor' }[t] || tonality;
      const pool2 = LOOPS.filter((l) => l.tonality === tonality && (t !== 'dorian' || l.dorian));
      const loop2 = pick((pool2.length ? pool2 : LOOPS.filter((l) => l.tonality === tonality)).flatMap((l) => Array(l.weight).fill(l)), r);
      if (!(tonic in PC)) tonic = 'F';
      const chords2 = loop2.chords.map(([o, q]) => name(PC[tonic] + o, tonic, tonality) + q);
      return { key: `${tonic} ${named}`, roman: loop2.roman, chords: chords2, dorian: !!loop2.dorian };
    }
  }
  const recent = new Set(avoidKeys.map((k) => PC[String(k).split(' ')[0]]));
  for (let i = 0; i < 12 && recent.has(PC[tonic]); i++) tonic = pick(keys, r);
  const chords = loop.chords.map(([o, q]) => name(PC[tonic] + o, tonic, loop.tonality) + q);
  return { key: `${tonic} ${scaleName(loop, r)}`, roman: loop.roman, chords, dorian: !!loop.dorian };
}

module.exports = { harmony, LOOPS };
