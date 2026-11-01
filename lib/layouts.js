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

const FALLBACKS = { SLUR: '{FEEL}', COUNTER: 'the {BASS}', MOTION: 'the lead a hair louder', LAND: 'on a hard cut', SYNC: '{FEEL}', TWITCH: 'twitch percussion in the gaps', HINTCUE: null };
const LIMITS = { LEAD: 30, COUNTER: 30, KICK: 26, HATS: 28, SNARE: 26, SYNC: 48, SLUR: 60, FEEL: 56, MOTION: 60, CUE: 90, AFTER: 40, LAND: 70, TWITCH: 90, ANTI: 56, HINTCUE: 90 };

/** Nominal bars by tag, used for the runtime estimate. Half-time lanes double hooks/loops. */
const BARS = { Intro: 4, Verse: 16, Hook: 8, Loop: 16, Break: 8, 'Beat Switch': 2, Outro: 8 };

const LAYOUTS = [
  {
    id: 'bare_loop', label: 'Two-bar loop, nothing changes', mode: 'loop', weight: 2, minimal: 1.0,
    sections: [
      '[Intro | {LEAD} alone, {INTRO} bars]',
      '[Loop | same two bars, {LEAD} over {KICK} and {BASS} only, {ANTI}]',
      '[Loop | same loop, identical, {SLUR}]',
      '[Loop | same loop, identical, {TWITCH}]',
      '[Loop | same loop, identical, {FEEL}, {HINTCUE}]',
      '[Outro | the loop stops dead on beat 1]',
      '[End]',
    ],
    bars: [4, 16, 16, 16, 16, 2],
  },
  {
    id: 'one_in_one_out', label: 'One sound added, one sound removed', mode: 'loop', weight: 2, minimal: 0.8,
    sections: [
      '[Intro | {KICK} and {BASS} only, {INTRO} bars]',
      '[Loop | same loop, {LEAD} added, everything else identical, {ANTI}]',
      '[Loop | same loop, {HATS} added, identical, {TWITCH}]',
      '[Loop | same loop, hats out, {LEAD} and {BASS} identical, {SLUR}]',
      '[Loop | same loop, {LEAD} out, drums and {BASS} alone, {FEEL}]',
      '[Loop | same loop, {LEAD} back, identical, {MOTION}, {HINTCUE}]',
      '[Outro | {KICK} and {BASS} only, stops dead on beat 1]',
      '[End]',
    ],
    bars: [4, 16, 16, 8, 8, 16, 4],
  },
  {
    id: 'kick_and_lead', label: 'Stripped to kick and lead', mode: 'loop', weight: 1, minimal: 0.95,
    sections: [
      '[Intro | {LEAD} alone, {INTRO} bars]',
      '[Loop | {KICK} and {LEAD} only, same two-bar loop, {ANTI}]',
      '[Loop | same loop, identical, {SLUR}]',
      '[Loop | same loop, identical, {TWITCH}]',
      '[Loop | same loop, {MOTION}, kick identical, {HINTCUE}]',
      '[Outro | {KICK} alone, stops dead on beat 1]',
      '[End]',
    ],
    bars: [4, 16, 16, 16, 16, 2],
  },
  {
    id: 'hook_first', label: 'Hook first', mode: 'straight', weight: 3, minimal: 0.3,
    sections: [
      '[Hook | full beat from beat 1, {LEAD} up front, {ANTI}]',
      '[Verse | same drums, {SYNC}, {SLUR}, {TWITCH}]',
      '[Hook | identical to the first hook, {LEAD} up front, {HINTCUE}]',
      '[Verse | same drums, {COUNTER} alone, {LEAD} out, {FEEL}]',
      '[Hook | identical, {MOTION}]',
      '[Hook | identical again, {LEAD} up front, drums identical]',
      '[Outro | drums cut dead, {LEAD} alone, ends on beat 1]',
      '[End]',
    ],
    bars: [8, 16, 8, 16, 8, 8, 4],
  },
  {
    id: 'verse_bare_hook_full', label: 'Verse bare, hook full', mode: 'straight', weight: 3, minimal: 0.5,
    sections: [
      '[Intro | {LEAD} alone, {INTRO} bars]',
      '[Verse | 8 bars, {KICK} and {BASS} only, {SLUR}]',
      '[Hook | full drums, {HATS}, {LEAD} up front, {ANTI}]',
      '[Verse | {KICK} and {BASS} only again, hats out, {LEAD} thinner, {TWITCH}]',
      '[Hook | full drums, identical, {MOTION}, {HINTCUE}]',
      '[Verse | {KICK} and {BASS} only, {LEAD} out, {COUNTER} alone]',
      '[Hook | full drums, identical, {LEAD} up front]',
      '[Outro | {KICK} and {BASS} only, {LEAD} alone on the last bar]',
      '[End]',
    ],
    bars: [4, 8, 8, 16, 8, 16, 8, 4],
  },
  {
    id: 'slow_motif', label: 'Slow burn: long intro on the lead, the motif grows', mode: 'straight', weight: 1, minimal: 0.6,
    sections: [
      '[Intro | {LEAD} alone, slow long notes, {INTRO} bars]',
      '[Verse | 8 bars, {BASS} one note per bar, {SNARE} on 3 only, {LEAD} three notes]',
      '[Hook | same density, the motif grows to five notes, {LEAD} still low, {ANTI}]',
      '[Verse | same density, identical drums, the motif grows one note more, {TWITCH}]',
      '[Hook | same density, full motif, {MOTION}, {HINTCUE}]',
      '[Hook | identical, full motif, {COUNTER} answering, drums identical]',
      '[Outro | drums cut dead, {LEAD} alone on the full motif]',
      '[End]',
    ],
    bars: [8, 8, 8, 16, 8, 16, 8],
  },
  {
    id: 'halftime_last_third', label: 'Half-time for the last third', mode: 'switch', weight: 2, minimal: 0.4,
    sections: [
      '[Intro | {LEAD} alone, {INTRO} bars]',
      '[Verse | 8 bars, full drums, {SYNC}, {SLUR}]',
      '[Hook | full beat, {LEAD} up front, {COUNTER} answering, {ANTI}]',
      '[Verse | {LEAD} thinner, {FEEL}, {TWITCH}, {HINTCUE}]',
      '[Break | drums cut dead, {LAND}]',
      '[Beat Switch | {CUE}]',
      '[Hook | {AFTER}, {MOTION}]',
      '[Hook | {AFTER}, identical, {COUNTER} answering]',
      '[Outro | {AFTER}, drums cut dead, ends on beat 1]',
      '[End]',
    ],
    bars: [4, 8, 8, 16, 4, 2, 16, 8, 4],
  },
  {
    id: 'a_then_b', label: 'A then B, two beats one song', mode: 'switch', weight: 3, minimal: 0.3,
    sections: [
      '[Intro | {LEAD} alone, {INTRO} bars]',
      '[Hook | beat A, {SYNC}, {LEAD} up front, {ANTI}]',
      '[Verse | beat A, same drums, {SLUR}, {TWITCH}]',
      '[Hook | beat A, identical, {COUNTER} answering]',
      '[Beat Switch | {CUE}]',
      '[Hook | beat B, {AFTER}, {LEAD} up front]',
      '[Verse | beat B, {AFTER}, {COUNTER} answering, {FEEL}]',
      '[Hook | beat B, {AFTER}, {MOTION}, {HINTCUE}]',
      '[Outro | beat B, {AFTER}, stops dead on beat 1]',
      '[End]',
    ],
    bars: [4, 8, 16, 8, 2, 8, 16, 8, 2],
  },
  {
    id: 'call_response', label: 'Call and reply, two-bar phrases', mode: 'straight', weight: 2, minimal: 0.5,
    sections: [
      '[Intro | {LEAD} two bars, {COUNTER} two bars, drums out]',
      '[Verse | 8 bars, full drums, {LEAD} two bars, then {COUNTER} two bars, {SLUR}]',
      '[Hook | full beat, {LEAD} and {COUNTER} two bars each, {ANTI}]',
      '[Verse | same drums, {FEEL}, {LEAD} and {COUNTER} two bars each, {TWITCH}]',
      '[Hook | identical, {LEAD} two bars, then {COUNTER} two bars, {MOTION}, {HINTCUE}]',
      '[Verse | same drums, identical, {COUNTER} alone for two bars, then {LEAD} alone for two]',
      '[Hook | identical, {LEAD} and {COUNTER} two bars each, drums identical]',
      '[Outro | drums cut dead, {LEAD} once, {COUNTER} once]',
      '[End]',
    ],
    bars: [4, 8, 8, 16, 8, 16, 8, 4],
  },
  {
    id: 'lead_out_middle', label: 'Lead out for 8 bars, slams back', mode: 'straight', weight: 2, minimal: 0.6,
    sections: [
      '[Intro | full drums from beat 1, {KICK}, {HATS}, {INTRO} bars]',
      '[Verse | 8 bars, full drums, {LEAD} low, {SLUR}]',
      '[Hook | full beat, {LEAD} up front, {ANTI}]',
      '[Break | {LEAD} out, drums and {BASS} alone, 8 bars, {TWITCH}]',
      '[Hook | {LEAD} slams back on the one, identical to the first hook]',
      '[Verse | same drums, {LEAD} thinner, {FEEL}]',
      '[Hook | identical, {MOTION}, {HINTCUE}]',
      '[Hook | identical again, {LEAD} up front, drums identical]',
      '[Outro | drums cut dead, {LEAD} alone, ends on beat 1]',
      '[End]',
    ],
    bars: [4, 8, 8, 8, 8, 16, 8, 8, 4],
  },
  {
    id: 'switch_strip', label: 'Drums cut, lead alone, then beat B slams in', mode: 'switch', weight: 1, minimal: 0.5,
    sections: [
      '[Intro | {KICK} and {BASS} only, {INTRO} bars]',
      '[Verse | 8 bars, full drums, {SYNC}, {LEAD} low]',
      '[Hook | full beat, {LEAD} up front, {SLUR}, {ANTI}]',
      '[Verse | same drums, {LEAD} thinner, {FEEL}]',
      '[Break | {LEAD} alone, {LAND}]',
      '[Beat Switch | {CUE}]',
      '[Verse | {AFTER}, {COUNTER} answering, {TWITCH}]',
      '[Hook | {AFTER}, {MOTION}, {HINTCUE}]',
      '[Hook | {AFTER}, identical, {LEAD} up front]',
      '[Outro | {AFTER}, stops dead on beat 1]',
      '[End]',
    ],
    bars: [4, 8, 8, 16, 4, 2, 16, 8, 8, 2],
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
function render(layout, vals, { introBars = 4 } = {}) {
  const shaped = {};
  for (const k of Object.keys(vals)) shaped[k] = clip(k, vals[k]);
  shaped.INTRO = String(introBars);
  const resolve = (k, depth = 0) => {
    if (shaped[k] != null && shaped[k] !== '') return shaped[k];
    const fb = FALLBACKS[k];
    if (fb === undefined || fb === null || depth > 3) return null;
    return fb.replace(/\{(\w+)\}/g, (m, j) => resolve(j, depth + 1) || '');
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
