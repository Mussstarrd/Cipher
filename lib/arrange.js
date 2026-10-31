'use strict';
/**
 * Arrangement engine: turns an energy arc + BPM into a timed, radio-structured
 * section map, then renders it as a Suno Lyrics field for an INSTRUMENTAL
 * (bracketed section tags with production cues, zero sung words).
 *
 * All timing assumes 4/4. For half-time lanes (trap, trap-pop, phonk) the BPM
 * is the written tempo (e.g. 140), so one bar = 240 / bpm seconds.
 */

const RADIO = {
  minSeconds: 150, // 2:30
  maxSeconds: 210, // 3:30
  targetSeconds: 180,
  maxIntroBars: 8,
  hookByMin: 30,
  hookByMax: 45,
};

/** Energy arc templates. bars are nominal; the planner scales to runtime. */
const ARCS = {
  'flat-bang': {
    label: 'Flat-bang (hook first)',
    summary: 'Hook lands immediately after a short intro; energy stays high with a break for contrast and a doubled hook at the end. Best for radio and playlists.',
    sections: [
      { type: 'intro', bars: 4, energy: 4 },
      { type: 'hook', bars: 8, energy: 9 },
      { type: 'verse', bars: 16, energy: 6 },
      { type: 'hook', bars: 8, energy: 9 },
      { type: 'verse', bars: 16, energy: 7 },
      { type: 'break', bars: 8, energy: 3 },
      { type: 'hook', bars: 8, energy: 10 },
      { type: 'hook', bars: 8, energy: 10, variant: 'final' },
      { type: 'outro', bars: 4, energy: 2 },
    ],
  },
  'build-release': {
    label: 'Build-release',
    summary: 'Classic verse-to-hook lift with pre-hook risers; hook by ~0:40. Best when the hook needs tension before it hits.',
    sections: [
      { type: 'intro', bars: 8, energy: 3 },
      { type: 'verse', bars: 12, energy: 5 },
      { type: 'pre', bars: 4, energy: 7 },
      { type: 'hook', bars: 8, energy: 9 },
      { type: 'verse', bars: 16, energy: 6 },
      { type: 'pre', bars: 4, energy: 8 },
      { type: 'hook', bars: 8, energy: 10 },
      { type: 'bridge', bars: 8, energy: 4 },
      { type: 'hook', bars: 8, energy: 10, variant: 'final' },
      { type: 'outro', bars: 4, energy: 2 },
    ],
  },
  'slow-burn': {
    label: 'Slow burn',
    summary: 'Sparse start, each hook adds layers; the last hook is the biggest. Best for mood-first and sync-leaning instrumentals.',
    sections: [
      { type: 'intro', bars: 8, energy: 2 },
      { type: 'verse', bars: 16, energy: 4, variant: 'sparse' },
      { type: 'hook', bars: 8, energy: 6, variant: 'medium' },
      { type: 'verse', bars: 16, energy: 6 },
      { type: 'hook', bars: 8, energy: 8 },
      { type: 'break', bars: 4, energy: 3 },
      { type: 'hook', bars: 8, energy: 10, variant: 'final' },
      { type: 'outro', bars: 8, energy: 2 },
    ],
  },
  'stop-start': {
    label: 'Stop-start',
    summary: 'Hook first, then mutes, drum-only breaks and a drop; maximum arrangement contrast. Best for bouncy trap-pop and phonk.',
    sections: [
      { type: 'intro', bars: 4, energy: 5 },
      { type: 'hook', bars: 8, energy: 9 },
      { type: 'verse', bars: 16, energy: 6, variant: 'mutes' },
      { type: 'hook', bars: 8, energy: 9 },
      { type: 'break', bars: 4, energy: 3, variant: 'drums-only' },
      { type: 'verse', bars: 16, energy: 7, variant: 'mutes' },
      { type: 'drop', bars: 8, energy: 10 },
      { type: 'hook', bars: 8, energy: 10, variant: 'final' },
      { type: 'outro', bars: 2, energy: 1, variant: 'hard-stop' },
    ],
  },
  'beat-switch': {
    label: 'Beat switch (two-part)',
    summary: 'Part A establishes the hook; a 4-bar switch flips drums and key feel into Part B. Best for dark boom-bap/trap hybrids and hybrids across lanes.',
    sections: [
      { type: 'intro', bars: 4, energy: 4 },
      { type: 'hook', bars: 8, energy: 8 },
      { type: 'verse', bars: 16, energy: 6 },
      { type: 'hook', bars: 8, energy: 9 },
      { type: 'switch', bars: 4, energy: 3 },
      { type: 'verse', bars: 16, energy: 7, variant: 'part-b' },
      { type: 'hook', bars: 8, energy: 10, variant: 'part-b' },
      { type: 'hook', bars: 8, energy: 10, variant: 'final' },
      { type: 'outro', bars: 4, energy: 2 },
    ],
  },
};

const SECTION_TAG = {
  intro: 'Short Instrumental Intro',
  hook: 'Hook',
  verse: 'Verse',
  pre: 'Pre-Chorus',
  bridge: 'Instrumental Bridge',
  break: 'Break',
  drop: 'Drop',
  switch: 'Beat Switch',
  outro: 'Outro',
};

function barSeconds(bpm) {
  return 240 / bpm;
}

function fmtTime(sec) {
  const s = Math.round(sec);
  const m = Math.floor(s / 60);
  return `${m}:${String(s % 60).padStart(2, '0')}`;
}

/** Deep-copy an arc's sections. */
function cloneSections(arc) {
  return ARCS[arc].sections.map((s) => ({ ...s }));
}

/**
 * Scale a section list to the radio runtime window at the given BPM.
 * Strategy: fix intro to <= 8 bars and hook arrival <= 45 s; then add or trim
 * verse/hook material until total lands in [min, max].
 */
function planSections(arcName, bpm, opts = {}) {
  if (!ARCS[arcName]) throw new Error(`Unknown energy arc: ${arcName}`);
  const bs = barSeconds(bpm);
  let sections = cloneSections(arcName);
  const target = opts.targetSeconds || RADIO.targetSeconds;
  const halfTime = opts.halfTime === undefined ? bpm >= 120 : !!opts.halfTime;

  // 0. Half-time lanes (trap, trap-pop, phonk at 130-160 written BPM): the
  //    felt tempo is half, so hooks/drops/pre-hooks/switches/outros double in
  //    written bars. Verses stay 16 written bars (industry standard) and the
  //    intro stays capped at 8 written bars.
  if (halfTime) {
    const mult = { hook: 2, drop: 2, pre: 2, switch: 2, outro: 2 };
    for (const s of sections) if (mult[s.type]) s.bars *= mult[s.type];
  }

  // 1. Intro: cap at 8 bars and make sure the first hook/drop starts by 45 s.
  const intro = sections[0];
  intro.bars = Math.min(intro.bars, RADIO.maxIntroBars);
  const firstHookIdx = sections.findIndex((s) => s.type === 'hook' || s.type === 'drop');
  const barsBeforeHook = () => sections.slice(0, firstHookIdx).reduce((a, s) => a + s.bars, 0);
  let guard = 0;
  while (barsBeforeHook() * bs > RADIO.hookByMax && guard++ < 8) {
    // shrink the longest pre-hook section (never below 4 bars).
    const pre = sections.slice(0, firstHookIdx);
    const longest = pre.filter((s) => s.bars > 4).sort((a, b) => b.bars - a.bars)[0];
    if (!longest) break;
    longest.bars -= 4;
  }

  // 2. Total runtime fit.
  const total = () => sections.reduce((a, s) => a + s.bars, 0) * bs;
  const outroIdx = () => sections.length - 1;
  guard = 0;
  while (total() < RADIO.minSeconds && guard++ < 12) {
    // Add material: restore shortened verses to 16, then add a hook repeat
    // before the outro, then grow verses to 20 as a last resort.
    const shortVerse = sections.filter((s) => s.type === 'verse' && s.bars < 16).sort((a, b) => a.bars - b.bars)[0];
    if (shortVerse) { shortVerse.bars += 4; continue; }
    const finals = sections.filter((s) => s.type === 'hook' && s.variant === 'final');
    if (finals.length < 2) {
      sections.splice(outroIdx(), 0, { type: 'hook', bars: halfTime ? 16 : 8, energy: 10, variant: 'final' });
      continue;
    }
    const verse = sections.filter((s) => s.type === 'verse' && s.bars < 20).sort((a, b) => a.bars - b.bars)[0];
    if (verse) { verse.bars += 4; continue; }
    break;
  }
  guard = 0;
  while (total() > RADIO.maxSeconds && guard++ < 12) {
    // Trim: duplicate final hook first, then verses down to 12, then outro, then break/bridge.
    const finals = sections.filter((s) => s.type === 'hook' && s.variant === 'final');
    if (finals.length > 1) { sections.splice(sections.indexOf(finals[finals.length - 1]), 1); continue; }
    const verse = sections.filter((s) => s.type === 'verse' && s.bars > 12).sort((a, b) => b.bars - a.bars)[0];
    if (verse) { verse.bars -= 4; continue; }
    const outro = sections[outroIdx()];
    if (outro.bars > 2) { outro.bars -= 2; continue; }
    const mid = sections.filter((s) => ['break', 'bridge'].includes(s.type) && s.bars > 4)[0];
    if (mid) { mid.bars -= 4; continue; }
    break;
  }
  // 3. Nudge toward the target with the last verse (+/- 4 bars) if that keeps us in-window.
  const lastVerse = [...sections].reverse().find((s) => s.type === 'verse');
  if (lastVerse) {
    const t = total();
    if (t < target - 8 * bs && t + 4 * bs <= RADIO.maxSeconds && lastVerse.bars < 20) lastVerse.bars += 4;
    else if (t > target + 8 * bs && lastVerse.bars > 12 && t - 4 * bs >= RADIO.minSeconds) lastVerse.bars -= 4;
  }

  // 4. Timestamps and numbering.
  let cursor = 0;
  const counters = {};
  for (const s of sections) {
    counters[s.type] = (counters[s.type] || 0) + 1;
    s.index = counters[s.type];
    s.startSec = cursor;
    s.seconds = s.bars * bs;
    cursor += s.seconds;
    s.endSec = cursor;
  }
  const hookAt = sections.find((s) => s.type === 'hook' || s.type === 'drop').startSec;
  return {
    arc: arcName,
    bpm,
    barSeconds: bs,
    sections,
    totalSeconds: cursor,
    totalFormatted: fmtTime(cursor),
    introBars: sections[0].bars,
    hookAtSeconds: hookAt,
    hookAtFormatted: fmtTime(hookAt),
    halfTime,
  };
}

/**
 * Render the Lyrics field for an instrumental. `cues` is a function
 * (section) => string[] of production cues; `lane` supplies defaults.
 */
function renderLyrics(plan, cueFn, opts = {}) {
  const lines = ['[Instrumental]'];
  const multiOccurring = new Set(
    Object.entries(plan.sections.reduce((m, s) => { m[s.type] = (m[s.type] || 0) + 1; return m; }, {}))
      .filter(([, n]) => n > 1).map(([t]) => t)
  );
  for (const s of plan.sections) {
    let name = SECTION_TAG[s.type];
    if (s.type === 'break' && s.variant === 'drums-only') name = 'Drum Break';
    if (multiOccurring.has(s.type) && s.type !== 'outro' && s.type !== 'intro') name += ` ${s.index}`;
    const cues = (cueFn(s) || []).filter(Boolean);
    const barNote = `${s.bars} bars`;
    lines.push(`[${name} - ${barNote}: ${cues.join(', ')}]`);
    if (opts.timestamps) lines[lines.length - 1] += `  <!-- ${fmtTime(s.startSec)} -->`;
  }
  lines.push('[End]');
  return lines.join('\n');
}

function barsToSecondsTable(bpms = [85, 92, 100, 130, 140, 150, 160], bars = [4, 8, 16]) {
  return bpms.map((bpm) => ({ bpm, ...Object.fromEntries(bars.map((b) => [`${b} bars`, fmtTime(b * barSeconds(bpm))])) }));
}

module.exports = { RADIO, ARCS, SECTION_TAG, barSeconds, fmtTime, planSections, renderLyrics, barsToSecondsTable };
