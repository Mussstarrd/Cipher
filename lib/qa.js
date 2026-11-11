'use strict';
/**
 * QA checker for suno-forge blocks (v2). Enforces:
 *  - verified character limits (Style, Lyrics, Exclude, title)
 *  - the guard over every copy field (voice, meter/swing, transitions/fills, EDM/film magnets,
 *    elevator magnets, negation, stock words, artist names) with the lane's allow/deny rules
 *  - no audio-reference / Audio Influence usage anywhere (prose may only say it is not used)
 *  - truly instrumental: toggle on, bracket-only Lyrics, no parentheses, [Instrumental] first,
 *    [End] last, Exclude covers vocals and humming as bare nouns
 *  - radio structure: first full beat (Hook or Loop) by 0:45, estimated runtime inside the window
 *    (warning only; the Duration control governs), [End] present, no "returns"
 *  - "every N bars" capped at one per roll (zero in loop mode); switch rolls repeat the axis words
 *  - sliders in range, current models only, Variety Off, Max Mode on
 */
const fs = require('fs');
const path = require('path');
const Guard = require('./guard');
const { LANES } = require('./lanes');
const { barSeconds } = require('./arrange');

const SPEC = JSON.parse(fs.readFileSync(path.join(__dirname, 'spec.json'), 'utf8'));
const RADIO = { minSeconds: 140, maxSeconds: 230, hookByMax: 45, maxIntroBars: 8 }; // runtime bounds are lenient here: a parsed block only has nominal bars, and the Duration control governs
const RETIRED = /\b(v3\.5|v4\.5\+|v4\.5-all|v4\.5|v4|v5\.5|v5)\b/i;

function findAudioRefs(text, prose = false) {
  const hits = [];
  const lower = (text || '').toLowerCase();
  for (const t of SPEC.audioRefTerms) {
    if (!lower.includes(t)) continue;
    if (!prose) { hits.push(t); continue; }
    for (const sen of text.split(/(?<=[.!?])\s+|\n/)) {
      const l = sen.toLowerCase();
      if (l.includes(t) && !/\b(not used|not applicable|leave|off|do not|don't|never|no audio|without|does not appear|is not)\b/.test(l)) hits.push(`${t}: "${sen.trim().slice(0, 80)}"`);
    }
  }
  return hits;
}

/** Parse the Lyrics field: sections [{tag, body, bars|null}] and any bare (unbracketed) lines. */
function parseLyrics(lyrics) {
  const sections = [], bareLines = [];
  for (const line of (lyrics || '').split('\n').map((l) => l.trim()).filter(Boolean)) {
    const m = line.match(/^\[([^\]]+)\]$/);
    if (!m) { bareLines.push(line); continue; }
    const [tag, ...rest] = m[1].split('|');
    const body = rest.join('|').trim();
    const bm = body.match(/(\d+)\s*bars?/);
    sections.push({ raw: line, tag: tag.trim(), type: tag.trim().toLowerCase(), body, bars: bm ? Number(bm[1]) : null });
  }
  return { sections, bareLines };
}

/** Nominal bars for a parsed section (mirrors lib/layouts.js). */
function nominalBars(sec, halfTime) {
  if (sec.bars) return sec.bars;
  const base = { intro: 4, verse: 16, hook: 8, loop: 16, break: 8, 'beat switch': 2, outro: 4, instrumental: 0, end: 0 }[sec.type];
  if (base == null) return 8;
  if (halfTime && sec.type === 'hook') return base * 2;
  if (halfTime && sec.type === 'loop') return Math.round(base * 1.5);
  return base;
}

function checkBlock(block) {
  const errors = [], warnings = [];
  const style = block.style || '', lyrics = block.lyrics || '', exclude = block.exclude || '';
  const meta = block.meta || {};
  const lane = LANES[meta.lane] || null;
  const bpm = meta.bpm;
  const halfTime = meta.halfTime != null ? !!meta.halfTime : (lane ? bpm >= lane.halfTimeAbove : bpm >= 120);

  // Limits
  if (style.length > SPEC.fields.style.limit) errors.push(`Style field is ${style.length} chars; limit ${SPEC.fields.style.limit}.`);
  if (lyrics.length > SPEC.fields.lyrics.limit) errors.push(`Lyrics field is ${lyrics.length} chars; limit ${SPEC.fields.lyrics.limit}.`);
  if (exclude.length > SPEC.fields.exclude.limit) errors.push(`Exclude field is ${exclude.length} chars; limit ${SPEC.fields.exclude.limit}.`);
  if (style.length < 200) errors.push('Style field is too short to carry head tags plus lead, bass and drums lines.');
  const title = block.toggles && block.toggles.title;
  if (title && title.length > SPEC.fields.title.limit) errors.push(`Title is ${title.length} chars; limit ${SPEC.fields.title.limit}.`);
  for (const v of ['safer', 'weirder']) {
    const vs = block.variations && block.variations[v] && block.variations[v].style;
    if (vs && vs.length > SPEC.fields.style.limit) errors.push(`Variation "${v}" Style is ${vs.length} chars; limit ${SPEC.fields.style.limit}.`);
  }

  // Guard over copy fields
  const copyFields = { style, lyrics, 'safer style': block.variations?.safer?.style, 'weirder style': block.variations?.weirder?.style, title };
  for (const [name, text] of Object.entries(copyFields)) {
    if (!text) continue;
    for (const h of Guard.check(text, lane ? lane.allow : null, lane ? lane.deny : null)) errors.push(`${name}: ${h.rule} ("${h.word}") ${h.why}.`);
  }
  // Names and comparisons in prose/header (lane ids are internal labels, allowed)
  const prose = [meta.laneLabel, block.model?.rationale, block.model?.wild, block.model?.mini, ...(block.postGen || []), block.sliders?.weirdnessWhy, block.sliders?.styleInfluenceWhy, block.toggles?.personaNote].filter(Boolean).join('\n').replace(/\b(connor-price|paak-pocket|jid-dark|phonk-experimental|hybrid)\b/gi, 'lane');
  const nameHit = prose.match(Guard.NAMES_RE);
  if (nameHit) errors.push(`Artist, record or label name in the block's prose/header: "${nameHit[2]}".`);
  if (RETIRED.test(prose) && !/retired|previous generation/i.test(prose)) errors.push('A retired Suno model is mentioned in the prose as if current (only v6, v6-wild, v6-mini exist).');

  // Audio references
  const copyAudio = findAudioRefs(Object.values(copyFields).filter(Boolean).join('\n'), false);
  if (copyAudio.length) errors.push(`Audio-reference term in copy fields: ${copyAudio.join(', ')}.`);
  const proseAudio = findAudioRefs([prose, block.sliders?.audioInfluence || ''].join('\n'), true);
  if (proseAudio.length) errors.push(`Prose suggests using audio references/Audio Influence: ${proseAudio.join(' | ')}.`);

  // Instrumental
  if (!block.toggles || block.toggles.instrumental !== true) errors.push('Instrumental toggle must be ON.');
  if (!block.toggles || block.toggles.customMode !== true) errors.push('Custom Mode must be ON.');
  if (block.toggles && block.toggles.variety && !/^off/i.test(String(block.toggles.variety))) errors.push('Variety must be Off (any other setting rewrites the Style text).');
  if (block.toggles && block.toggles.maxMode === false) warnings.push('Max Mode is off; v6 mixes can go muffled after ~2:30 without it.');
  const { sections, bareLines } = parseLyrics(lyrics);
  if (bareLines.length) errors.push(`Lyrics field has ${bareLines.length} non-bracketed line(s) that Suno may sing: "${bareLines[0].slice(0, 60)}".`);
  if (/[()]/.test(lyrics)) errors.push('Lyrics field contains parentheses; Suno sings parenthesized text as backing voices.');
  if (!sections.length || sections[0].type !== 'instrumental') errors.push('Lyrics must start with [Instrumental].');
  if (!sections.length || sections[sections.length - 1].type !== 'end') errors.push('Lyrics must end with [End].');
  if ((lyrics.match(/\[End\]/g) || []).length > 1) errors.push('Lyrics contain more than one [End].');
  if (!/\binstrumental\b/i.test(style)) errors.push('Style field must say "instrumental" in the head tags.');
  const exLower = exclude.toLowerCase();
  if (!/\bvocals\b/.test(exLower)) errors.push('Exclude field must contain "vocals" or "lead vocals".');
  if (!exLower.includes('humming')) errors.push('Exclude field must list "humming" (the most common instrumental leak).');
  if (/(^|,)\s*no\s+\w/.test(exLower)) errors.push('Exclude terms must be bare nouns ("vocals"), never negated ("no vocals").');
  if (!/\b(fills|transitions|risers)\b/.test(exLower)) warnings.push('Exclude field does not list fills/transitions/risers; section changes may grow fills.');

  // Structure
  const body = sections.filter((s) => s.type !== 'instrumental' && s.type !== 'end');
  if (body.length) {
    const first = body[0];
    if (first.type === 'intro' && first.bars && first.bars > RADIO.maxIntroBars) errors.push(`Intro is ${first.bars} bars; must be <= ${RADIO.maxIntroBars}.`);
    const hookIdx = body.findIndex((s) => /^(hook|loop)$/.test(s.type));
    if (hookIdx < 0) errors.push('No [Hook] or [Loop] section (no full beat).');
    else if (bpm) {
      const before = body.slice(0, hookIdx).reduce((a, s) => a + nominalBars(s, halfTime), 0) * barSeconds(bpm);
      if (before > RADIO.hookByMax) errors.push(`First full beat arrives at ${Math.round(before)} s; must be <= ${RADIO.hookByMax} s.`);
      const total = body.reduce((a, s) => a + nominalBars(s, halfTime), 0) * barSeconds(bpm);
      if (total < RADIO.minSeconds || total > RADIO.maxSeconds) warnings.push(`Estimated runtime ${Math.round(total)} s is outside ${RADIO.minSeconds}-${RADIO.maxSeconds} s; the Duration control still governs.`);
    }
    if (/\breturns?\b/i.test(lyrics)) errors.push('Lyrics say "returns"; a beat switch must never go back to beat A.');
    const isLoop = body.every((s) => /^(intro|loop|outro)$/.test(s.type));
    const everyHits = new Set((`${style}\n${lyrics}`.match(new RegExp(Guard.EVERY.source, 'gi')) || []).map((h) => h.toLowerCase()));
    if (everyHits.size > (isLoop ? 0 : 1)) errors.push(`"Every N bars" phrases teach Suno to change something every few bars; found ${everyHits.size} (limit ${isLoop ? 0 : 1}): ${[...everyHits].join(' | ')}.`);
    if (/\[Beat Switch/i.test(lyrics)) {
      const after = lyrics.split(/\[Beat Switch[^\]]*\]/i)[1] || '';
      if (!/\[(Hook|Verse|Loop)/.test(after)) errors.push('A [Beat Switch] must be followed by at least one section carrying the new beat.');
      if (block.parts && block.parts.switch && !style.includes(block.parts.switch.tag)) errors.push('Switch roll: the head tag must carry the switch axis words.');
      if (block.parts && block.parts.switch && !after.includes(block.parts.switch.after)) errors.push('Switch roll: every section after the switch must repeat the axis words.');
    }
    if (!/\bdrums:/.test(style)) errors.push('Style field lost its drums line; the beat will come back generic.');
    if (!/\blead:/.test(style)) errors.push('Style field lost its lead line.');
    if (!/\bbass:/.test(style)) warnings.push('Style field has no bass line.');
    const head = style.slice(0, 300);
    if (!/\d{2,3} BPM/.test(head)) errors.push('BPM must be in the head tags (first 300 chars).');
    if (!/\bin [A-G][#b]? (harmonic minor|minor|major|Dorian|Phrygian|Lydian|Mixolydian)\b/.test(head)) warnings.push('Key is not in the head tags.');
  }

  // Sliders and model
  const sl = block.sliders || {};
  for (const k of ['weirdness', 'styleInfluence']) {
    const v = sl[k], r = SPEC.sliders[k];
    if (typeof v !== 'number' || Number.isNaN(v) || v < r.min || v > r.max) errors.push(`${k} must be a number in ${r.min}-${r.max}.`);
  }
  if (typeof sl.weirdness === 'number' && sl.weirdness > 65 && block.model && block.model.pick === 'v6') warnings.push('Weirdness above 65 on v6 drifts tempo and structure; use v6-wild for that range.');
  if (block.model && !SPEC.models.list.includes(block.model.pick)) errors.push(`Model "${block.model.pick}" is not in the verified list: ${SPEC.models.list.join(', ')}.`);
  for (const k of ['model', 'toggles', 'style', 'lyrics', 'exclude', 'sliders', 'variations', 'postGen']) if (!block[k]) errors.push(`Block is missing section "${k}".`);

  return { ok: errors.length === 0, errors, warnings, stats: { styleChars: style.length, lyricsChars: lyrics.length, excludeChars: exclude.length, sections: body.length } };
}

/** Parse a rendered markdown block (render.js format) back into a block object. */
function parseMarkdownBlock(md) {
  const chunks = md.split(/^(?=#{2,3}\s)/m);
  const grab = (label) => { const re = new RegExp(`^#{2,3}\\s*\\d*\\.?\\s*${label}`, 'i'); const hit = chunks.find((c) => re.test(c)); return hit ? hit.replace(/^[^\n]*\n/, '') : ''; };
  const code = (s) => { const m = s.match(/```[a-z]*\n([\s\S]*?)```/); return m ? m[1].trim() : s.trim(); };
  const modelSec = grab('MODEL'), toggles = grab('MODE\\/TOGGLES'), slSec = grab('SLIDERS'), varSec = grab('VARIATIONS'), postSec = grab('POST-GEN NOTES');
  const num = (re, s) => { const m = s.match(re); return m ? Number(m[1]) : NaN; };
  const h1 = (md.match(/^#\s+suno-forge prompt block:\s*(.+)$/m) || [])[1] || '';
  const laneLine = (md.match(/^Lane\s+`([^`]+)`.*$/m) || []);
  const bpmM = md.match(/(\d{2,3})\s*BPM/i);
  const codeBlocks = [...varSec.matchAll(/```[a-z]*\n([\s\S]*?)```/g)].map((m) => m[1].trim());
  const titleM = toggles.match(/Song Title:\*{0,2}\s*(.+)/);
  return {
    meta: { lane: laneLine[1], bpm: bpmM ? Number(bpmM[1]) : undefined, halfTime: /half-time/i.test(laneLine[0] || ''), laneLabel: [h1, laneLine[0]].filter(Boolean).join(' | ') },
    model: { pick: ((modelSec.match(/\*\*Pick:\*\*\s*`?([^`\n]+?)`?\s*(?:\n|$)/) || [])[1] || '').trim() || undefined, rationale: modelSec },
    toggles: { customMode: /Custom Mode:\*{0,2}\s*ON/i.test(toggles), instrumental: /Instrumental:\*{0,2}\s*ON/i.test(toggles), maxMode: /Max Mode:\*{0,2}\s*ON/i.test(toggles), variety: (toggles.match(/Variety:\*{0,2}\s*(\w+)/i) || [])[1], personaNote: toggles, title: titleM ? titleM[1].trim() : undefined },
    style: code(grab('STYLE FIELD')), lyrics: code(grab('LYRICS FIELD')), exclude: code(grab('EXCLUDE STYLES FIELD')),
    sliders: { weirdness: num(/Weirdness:\*{0,2}\s*(\d+)/i, slSec), styleInfluence: num(/Style Influence:\*{0,2}\s*(\d+)/i, slSec), audioInfluence: slSec, weirdnessWhy: slSec },
    variations: { safer: { style: codeBlocks[0] || '' }, weirder: { style: codeBlocks[1] || '' } },
    postGen: postSec ? [postSec] : [],
  };
}

module.exports = { checkBlock, parseMarkdownBlock, findAudioRefs, parseLyrics, RADIO };
