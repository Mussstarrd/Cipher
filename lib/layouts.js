'use strict';
/**
 * Lyrics-field layouts. Each roll picks a layout before it picks sounds; the layout decides the
 * mode (loop / straight / switch). Sections are templates with placeholders:
 *   {LEAD} {COUNTER} lead and counter handles      {BASS} "808" or "bass"
 *   {KICK} {HATS} {SNARE} bare drum sources         {SYNC} a syncopation tag
 *   {SLUR} the beat-3 slur phrase                   {FEEL} a human-timing phrase
 *   {MOTION} how the lead changes                   {TWITCH} twitch percussion
 *   {CUE} {AFTER} {LAND} beat-switch cue, after-words and landing
 *   {INTRO} intro bar count                          {ANTI} anti-transition phrase
 * A placeholder whose value does not fit its limit is dropped whole (never cut mid-word) and the
 * fallback speaks instead. `bars` are nominal section lengths used only to estimate runtime and
 * check the radio window (first full beat by 0:45, total 2:30-3:30); they are not rendered.
 *
 * Suno reads "[Tag | words]" as directions; words inside the bracket are followed as arrangement
 * and performance cues. No parentheses, no bare lines, [Instrumental] first, [End] last.
 */

const FALLBACKS = { SPACE: null, SLUR: '{FEEL}', COUNTER: 'the {BASS}', MOTION: 'the lead a hair louder', LAND: 'on a hard cut', SYNC: '{FEEL}', TWITCH: 'twitch percussion in the gaps', HINTCUE: null };
const LIMITS = { SPACE: 70, LEAD: 30, COUNTER: 30, KICK: 26, HATS: 28, SNARE: 26, SYNC: 48, SLUR: 60, FEEL: 56, MOTION: 60, CUE: 90, AFTER: 40, LAND: 70, TWITCH: 90, ANTI: 56, HINTCUE: 90 };

/** Nominal bars by tag, used for the runtime estimate. Half-time lanes double hooks/loops. */
const BARS = { Intro: 4, Verse: 16, Hook: 8, Loop: 16, Break: 8, 'Beat Switch': 2, Outro: 8 };

const LAYOUTS = [
  {
    id: 'bare_minimum', label: 'Bare minimum: the lead, a kick, a snare, a bass, air', mode: 'loop', weight: 2, minimal: 1.0, bare: true,
    sections: [
      '[Loop | {LEAD}, {KICK}, {SNARE} and {BASS}, nothing else, {SPACE}]',
      '[Loop | identical]',
      '[Loop | identical, {MOTION}]',
      '[Loop | identical]',
      '[Loop | identical]',
      '[End]',
    ],
    bars: [16, 16, 16, 16, 8],
  },
  {
    id: 'bare_loop', label: 'One two-bar loop, the same all song', mode: 'loop', weight: 3, minimal: 1.0,
    sections: [
      '[Loop | {LEAD} over {KICK}, {SNARE} and {BASS}, the same two bars all song, {SPACE}]',
      '[Loop | identical, {FEEL}]',
      '[Loop | identical]',
      '[Loop | identical, {MOTION}]',
      '[Loop | identical]',
      '[End]',
    ],
    bars: [16, 16, 16, 16, 8],
  },
  {
    id: 'kick_and_lead', label: 'Kick, snare and lead, the bass arriving later', mode: 'loop', weight: 2, minimal: 0.95,
    sections: [
      '[Loop | {KICK}, {SNARE} and {LEAD}, the same two bars all song, {SPACE}]',
      '[Loop | identical, {BASS} under it]',
      '[Loop | identical, {MOTION}]',
      '[Loop | identical, {FEEL}]',
      '[Loop | identical]',
      '[End]',
    ],
    bars: [16, 16, 16, 16, 8],
  },
  {
    id: 'one_voice_changes', label: 'Same drums all song, one melodic voice changes', mode: 'loop', weight: 3, minimal: 0.8,
    sections: [
      '[Loop | {KICK}, {SNARE} and {BASS} in one pattern all song, {LEAD} on top, {SPACE}]',
      '[Loop | identical drums, {LEAD} an octave lower]',
      '[Loop | identical drums, {LEAD} resting, {BASS} carrying it, {FEEL}]',
      '[Loop | identical drums, {LEAD} as at the start, {MOTION}]',
      '[Loop | identical]',
      '[End]',
    ],
    bars: [16, 16, 16, 16, 8],
  },
  {
    id: 'hook_first', label: 'Hook first, same drums throughout', mode: 'straight', weight: 3, minimal: 0.3,
    sections: [
      '[Hook | the whole beat from the first second, {LEAD} up front, {SYNC}]',
      '[Verse | same drums, {LEAD} half as many notes, {FEEL}]',
      '[Hook | identical to the first hook, {HINTCUE}]',
      '[Verse | same drums, {COUNTER} in place of {LEAD}, {TWITCH}]',
      '[Hook | identical, {MOTION}]',
      '[Hook | identical]',
      '[End]',
    ],
    bars: [8, 16, 8, 16, 8, 16],
  },
  {
    id: 'lead_swap', label: 'Two leads trading two bars each', mode: 'straight', weight: 2, minimal: 0.5,
    sections: [
      '[Hook | the whole beat from the first second, {LEAD} two bars then {COUNTER} two bars, trading]',
      '[Verse | same drums, {LEAD} only, {FEEL}]',
      '[Hook | identical to the first hook, {HINTCUE}]',
      '[Verse | same drums, {COUNTER} only, {TWITCH}]',
      '[Hook | identical, {MOTION}]',
      '[End]',
    ],
    bars: [16, 16, 8, 16, 16],
  },
  {
    id: 'space_verses', label: 'Verses mostly air, hooks every bar, same drums', mode: 'straight', weight: 3, minimal: 0.6,
    sections: [
      '[Verse | 8 bars, {KICK}, {SNARE} and {BASS} from the first second, {LEAD} one phrase then two bars of rest, {SPACE}]',
      '[Hook | same drums, {LEAD} every bar, {SYNC}]',
      '[Verse | same drums, {LEAD} one phrase then two bars of rest, {FEEL}]',
      '[Hook | identical to the first hook, {HINTCUE}]',
      '[Verse | same drums, {COUNTER} one phrase then two bars of rest]',
      '[Hook | identical, {MOTION}]',
      '[End]',
    ],
    bars: [8, 8, 16, 8, 16, 16],
  },
  {
    id: 'slow_motif', label: 'The motif grows a note at a time over the same drums', mode: 'straight', weight: 2, minimal: 0.6,
    sections: [
      '[Verse | 8 bars, {KICK}, {SNARE} and {BASS} from the first second, {LEAD} three notes only, {SPACE}]',
      '[Hook | same drums, the motif five notes long, {LEAD} still low]',
      '[Verse | same drums, the motif one note longer, {FEEL}]',
      '[Hook | same drums, the whole motif, {MOTION}, {HINTCUE}]',
      '[Verse | 8 bars, same drums, {LEAD} three notes only, {TWITCH}]',
      '[Hook | identical, {COUNTER} answering]',
      '[End]',
    ],
    bars: [8, 8, 16, 16, 8, 16],
  },
  {
    id: 'drums_alone_first', label: 'The drums alone first, the lead laid over them', mode: 'straight', weight: 2, minimal: 0.7,
    sections: [
      '[Verse | 8 bars, {KICK}, {SNARE} and {HATS} alone from the first second, {SYNC}]',
      '[Hook | same drums, {LEAD} on top, everything else as it was]',
      '[Verse | same drums, {LEAD} half as many notes, {FEEL}]',
      '[Hook | identical to the first hook, {HINTCUE}]',
      '[Verse | 8 bars, same drums, {COUNTER} in place of {LEAD}, {TWITCH}]',
      '[Hook | identical, {MOTION}]',
      '[End]',
    ],
    bars: [8, 16, 16, 8, 8, 16],
  },
  {
    id: 'two_patterns', label: 'Two drum patterns, one song', mode: 'switch', weight: 1, minimal: 0.3,
    sections: [
      '[Hook | pattern A from the first second, {SYNC}, {LEAD} up front]',
      '[Verse | pattern A, same drums, {FEEL}]',
      '[Hook | pattern A, identical, {COUNTER} answering]',
      '[Beat Switch | {CUE}]',
      '[Hook | pattern B, {AFTER}, {LEAD} up front]',
      '[Verse | pattern B, {AFTER}, {COUNTER} answering, {TWITCH}]',
      '[Hook | pattern B, {AFTER}, {MOTION}, {HINTCUE}]',
      '[End]',
    ],
    bars: [8, 16, 8, 2, 8, 16, 16],
  },
  {
    id: 'halftime_last_third', label: 'A second pattern for the last third', mode: 'switch', weight: 1, minimal: 0.4,
    sections: [
      '[Hook | the whole beat from the first second, {LEAD} up front, {SYNC}]',
      '[Verse | same drums, {LEAD} half as many notes, {FEEL}]',
      '[Hook | identical to the first hook, {COUNTER} answering]',
      '[Verse | same drums, {TWITCH}, {HINTCUE}]',
      '[Beat Switch | {CUE}]',
      '[Hook | {AFTER}, {MOTION}]',
      '[Hook | {AFTER}, identical, {COUNTER} answering]',
      '[End]',
    ],
    bars: [8, 16, 8, 16, 2, 8, 16],
  },
];

/** Keep as many whole clauses as fit the limit; never cut mid-word. AFTER stays intact (its words must match the head tag). */
function clip(key, value) {
  const lim = LIMITS[key];
  if (!value || !lim || value.length <= lim || key === 'AFTER') return value;
  const parts = value.split(', ');
  let out = parts[0];
  for (const q of parts.slice(1)) {
    if (`${out}, ${q}`.length > lim) break;
    out = `${out}, ${q}`;
  }
  return out.length > lim ? null : out;
}

/** Fill a layout's sections. `vals` maps placeholder -> string|null. */
/** Cues that describe a feel or a change. One of these never appears twice in a lyric: the second time, a small section cue (PASS) speaks instead. */
const ONCE = new Set(['SYNC', 'SLUR', 'FEEL', 'MOTION', 'TWITCH', 'ANTI']);

function render(layout, vals, { introBars = 4 } = {}) {
  const shaped = {};
  for (const k of Object.keys(vals)) if (k !== 'PASS') shaped[k] = clip(k, vals[k]);
  shaped.INTRO = String(introBars);
  const pass = [...(vals.PASS || [])];
  const said = new Set();
  const fresh = (v) => { if (!v || said.has(v)) return null; said.add(v); return v; };
  const resolve = (k, depth = 0) => {
    const once = ONCE.has(k);
    if (shaped[k] != null && shaped[k] !== '') { if (!once) return shaped[k]; const v = fresh(shaped[k]); if (v) return v; }
    const fb = FALLBACKS[k];
    if (fb != null && depth <= 3) {
      const out = fb.replace(/\{(\w+)\}/g, (m, j) => resolve(j, depth + 1) || '').trim();
      if (out && !once) return out;
      if (out && fresh(out)) return out;
    }
    if (!once) return null;
    while (pass.length) { const v = fresh(pass.shift()); if (v) return v; }
    return null;
  };
  const lines = layout.sections.map((t) => {
    let line = t.replace(/\{(\w+)\}/g, (m, k) => resolve(k) || '');
    line = line.replace(/,\s*,/g, ',').replace(/\|\s*,/g, '| ').replace(/,\s*\]/g, ']').replace(/\s{2,}/g, ' ').replace(/\|\s*\]/g, '| ]');
    return line;
  });
  return `[Instrumental]\n\n${lines.join('\n\n')}`;
}

/** Nominal bars for section i of a layout at a tempo feel. Half-time (written >= 120 BPM): hooks double, loops x1.5; the bare 8-bar verses stay 8 written bars. */
function sectionBars(layout, i, halfTime, introBars = 4) {
  const s = layout.sections[i];
  let bars = layout.bars[i] != null ? layout.bars[i] : (BARS[s.match(/^\[([^|\]]+)/)[1].trim()] || 8);
  if (/^\[Intro/.test(s)) bars = introBars;
  if (halfTime && /^\[Hook/.test(s)) bars *= 2;
  if (halfTime && /^\[Loop/.test(s)) bars = Math.round(bars * 1.5);
  return bars;
}

/** Estimate the runtime and first-full-beat time of a layout at a tempo. */
function estimate(layout, bpm, halfTime, introBars = 4) {
  const bs = 240 / bpm;
  let t = 0, hookAt = null;
  layout.sections.forEach((s, i) => {
    if (/^\[End\]/.test(s)) return;
    if (hookAt === null && /^\[(Hook|Loop)/.test(s)) hookAt = t;
    t += sectionBars(layout, i, halfTime, introBars) * bs;
  });
  return { totalSeconds: t, hookAtSeconds: hookAt == null ? 0 : hookAt };
}

module.exports = { LAYOUTS, FALLBACKS, LIMITS, BARS, render, clip, estimate, sectionBars };
