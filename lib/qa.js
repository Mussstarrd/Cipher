'use strict';
/**
 * QA checker for suno-forge blocks. Enforces:
 *  - verified character limits (Style, Lyrics, Exclude)
 *  - no real artist names or "type beat"/"in the style of" phrasing anywhere in copy fields
 *  - no audio-reference / Audio Influence usage (copy fields: zero tolerance; prose: only "not used" statements allowed)
 *  - truly instrumental (toggle on, Lyrics are bracket tags only, Exclude covers vocals)
 *  - no inline negatives in the Style field
 *  - radio structure (intro <= 8 bars, hook/drop by 0:45, runtime 2:30-3:30, contrast section, [End])
 *  - sliders in range, model in the verified list
 */
const fs = require('fs');
const path = require('path');
const { barSeconds, RADIO } = require('./arrange');

const SPEC = JSON.parse(fs.readFileSync(path.join(__dirname, 'spec.json'), 'utf8'));

function esc(s) { return s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'); }

const LANE_IDS = /\b(connor-price|paak-pocket|jid-dark|phonk-experimental|hybrid)\b/gi;
function findBannedNames(text, { allowLaneIds = false } = {}) {
  const hits = [];
  if (allowLaneIds) text = text.replace(LANE_IDS, 'lane');
  for (const name of SPEC.bannedNames) {
    const re = new RegExp(`(^|[^A-Za-z0-9])${esc(name)}(?=$|[^A-Za-z0-9])`, 'i');
    if (re.test(text)) hits.push(name);
  }
  for (const p of SPEC.bannedPhrases) if (text.toLowerCase().includes(p)) hits.push(`phrase: "${p}"`);
  return hits;
}

function findAudioRefs(text, prose = false) {
  const hits = [];
  const lower = text.toLowerCase();
  for (const t of SPEC.audioRefTerms) {
    if (!lower.includes(t)) continue;
    if (!prose) { hits.push(t); continue; }
    // In prose, allow only sentences that say it is NOT used.
    const sentences = text.split(/(?<=[.!?])\s+|\n/);
    for (const sen of sentences) {
      const l = sen.toLowerCase();
      if (!l.includes(t)) continue;
      if (!/\b(not used|not applicable|leave|off|do not|don't|never|no audio|without)\b/.test(l)) hits.push(`${t}: "${sen.trim().slice(0, 80)}"`);
    }
  }
  return hits;
}

/** Parse the Lyrics field into sections: [{tag, bars}] and detect non-tag lines. */
function parseLyrics(lyrics) {
  const lines = lyrics.split('\n').map((l) => l.trim()).filter(Boolean);
  const sections = [];
  const bareLines = [];
  for (const line of lines) {
    const m = line.match(/^\[([^\]]+)\]$/);
    if (!m) { bareLines.push(line); continue; }
    const inner = m[1];
    const name = inner.split(/\s*[-:]\s*/)[0].trim();
    const bm = inner.match(/(\d+)\s*bars?/i);
    sections.push({ raw: line, name, type: name.toLowerCase().replace(/\s*\d+$/, ''), bars: bm ? Number(bm[1]) : null });
  }
  return { sections, bareLines };
}

/**
 * Validate a block object (as produced by generate()). Returns {ok, errors, warnings, stats}.
 */
function checkBlock(block) {
  const errors = [], warnings = [];
  const style = block.style || '';
  const lyrics = block.lyrics || '';
  const exclude = block.exclude || '';
  const bpm = block.meta && block.meta.bpm;

  // Limits
  if (style.length > SPEC.fields.style.limit) errors.push(`Style field is ${style.length} chars; limit ${SPEC.fields.style.limit}.`);
  if (style.length > SPEC.fields.style.target) warnings.push(`Style field is ${style.length} chars; soft target ${SPEC.fields.style.target} (long prompts dilute early-token weighting).`);
  if (lyrics.length > SPEC.fields.lyrics.limit) errors.push(`Lyrics field is ${lyrics.length} chars; limit ${SPEC.fields.lyrics.limit}.`);
  if (exclude.length > SPEC.fields.exclude.limit) errors.push(`Exclude field is ${exclude.length} chars; limit ${SPEC.fields.exclude.limit}.`);
  if (style.length < 40) errors.push('Style field is too short to steer the model.');
  for (const v of ['safer', 'weirder']) {
    const vs = block.variations && block.variations[v] && block.variations[v].style;
    if (vs && vs.length > SPEC.fields.style.limit) errors.push(`Variation "${v}" Style is ${vs.length} chars; limit ${SPEC.fields.style.limit}.`);
  }

  // Banned names / phrases in copy fields and in prose
  const copyText = [style, lyrics, exclude, block.variations?.safer?.style, block.variations?.weirder?.style, block.toggles?.title].filter(Boolean).join('\n');
  const nameHits = findBannedNames(copyText);
  if (nameHits.length) errors.push(`Real artist name or style-of phrasing in copy fields: ${nameHits.join('; ')}.`);
  const proseText = [block.meta?.laneLabel, block.model?.rationale, block.model?.wild, block.model?.mini, ...(block.postGen || []), block.sliders?.weirdnessWhy, block.sliders?.styleInfluenceWhy, block.toggles?.personaNote].filter(Boolean).join('\n');
  const proseNames = findBannedNames(proseText, { allowLaneIds: true });
  if (proseNames.length) errors.push(`Artist name or style-of phrasing in the block's prose/header (artist names belong only in README and research): ${proseNames.join('; ')}.`);

  // Audio reference ban
  const audioCopy = findAudioRefs(copyText, false);
  if (audioCopy.length) errors.push(`Audio-reference term in copy fields: ${audioCopy.join(', ')}.`);
  const audioProse = findAudioRefs([proseText, block.sliders?.audioInfluence || '', block.toggles?.personaNote || ''].join('\n'), true);
  if (audioProse.length) errors.push(`Prose suggests using audio references/Audio Influence: ${audioProse.join(' | ')}.`);

  // Instrumental
  if (!block.toggles || block.toggles.instrumental !== true) errors.push('Instrumental toggle must be ON.');
  if (!block.toggles || block.toggles.customMode !== true) errors.push('Custom Mode must be ON (Style/Lyrics/Exclude fields need it).');
  const parsed = parseLyrics(lyrics);
  const bareLines = parsed.bareLines;
  const sections = parsed.sections.filter((s) => s.type !== 'instrumental');
  if (/\([^)]*\)/.test(lyrics)) errors.push('Lyrics field contains parentheses; Suno sings parenthesized text as backing vocals. Use brackets only.');
  if (bareLines.length) errors.push(`Lyrics field has ${bareLines.length} non-bracketed line(s) that Suno may sing: "${bareLines[0].slice(0, 60)}".`);
  const exLower = exclude.toLowerCase();
  if (!/\bvocals\b/.test(exLower)) errors.push('Exclude field must contain "vocals" or "lead vocals".');
  if (!['humming', 'singing', 'ad-libs', 'lyrics'].some((w) => exLower.includes(w))) errors.push('Exclude field must also cover wordless leaks (humming / singing / ad-libs / lyrics).');
  if (!exLower.includes('humming')) warnings.push('Exclude field does not list "humming"; hums are the most common instrumental leak.');
  if (/(^|,)\s*no\s+\w/.test(exLower)) errors.push('Exclude terms must be bare nouns ("vocals"), never negated ("no vocals").');
  if (!/\binstrumental\b/i.test(style)) warnings.push('Style field does not include the word "instrumental"; the toggle usually suffices but the word helps.');

  // Vocal-implying words in copy fields (they summon hums even in instrumental mode)
  const vocalHits = [];
  for (const term of SPEC.vocalImplying || []) {
    const re = new RegExp(`(^|[^a-z0-9])${esc(term.toLowerCase())}(?=$|[^a-z0-9])`, 'i');
    if (re.test(copyText.replace(/^.*$/gm, (line) => line))) vocalHits.push(term);
  }
  // Exclude field is allowed to name vocal terms (that is its job).
  const vocalHitsOutsideExclude = vocalHits.filter((t) => new RegExp(`(^|[^a-z0-9])${esc(t.toLowerCase())}(?=$|[^a-z0-9])`, 'i').test([style, lyrics, block.variations?.safer?.style, block.variations?.weirder?.style, block.toggles?.title].filter(Boolean).join('\n')));
  if (vocalHitsOutsideExclude.length) errors.push(`Vocal-implying term in Style/Lyrics (summons hums in instrumental mode): ${vocalHitsOutsideExclude.join(', ')}.`);

  // Negatives inline in Style
  const neg = style.match(/\b(no|without|avoid|not|never|free of)\s+[a-z]/i);
  if (neg && !/vocal-free/i.test(neg[0])) errors.push(`Inline negative in Style field ("${neg[0]}..."). Negatives belong in Exclude Styles.`);
  if (/\bvocal(s)?\b/i.test(style) && !/vocal-free/i.test(style)) warnings.push('Style field mentions "vocal"; Suno often pattern-matches this and adds vocals. Prefer "pitched-down sample texture".');

  // Radio structure
  if (!sections.length) errors.push('Lyrics field has no section tags.');
  else {
    const first = sections[0];
    if (!/intro/.test(first.type)) warnings.push(`First section is [${first.name}], not an intro tag.`);
    if (first.bars && first.bars > RADIO.maxIntroBars) errors.push(`Intro is ${first.bars} bars; must be <= ${RADIO.maxIntroBars}.`);
    const hookIdx = sections.findIndex((s) => /^(hook|chorus|drop)/.test(s.type));
    if (hookIdx < 0) errors.push('No [Hook]/[Chorus]/[Drop] section.');
    else if (bpm) {
      const before = sections.slice(0, hookIdx).reduce((a, s) => a + (s.bars || 0), 0) * barSeconds(bpm);
      if (before > RADIO.hookByMax) errors.push(`First hook/drop arrives at ${Math.round(before)} s; must be <= ${RADIO.hookByMax} s.`);
    }
    if (bpm && sections.every((s) => s.bars !== null || s.type === 'end')) {
      const total = sections.reduce((a, s) => a + (s.bars || 0), 0) * barSeconds(bpm);
      if (total < RADIO.minSeconds || total > RADIO.maxSeconds) errors.push(`Planned runtime ${Math.round(total)} s is outside ${RADIO.minSeconds}-${RADIO.maxSeconds} s.`);
    }
    const hasContrast = sections.some((s) => /(break|bridge|drop|beat switch|breakdown|interlude|pre-chorus|build)/.test(s.type));
    if (!hasContrast) errors.push('No contrast section ([Break]/[Bridge]/[Drop]/[Beat Switch]).');
    const last = sections[sections.length - 1];
    if (!/^(end|outro|fade out)/.test(last.type)) errors.push('Lyrics must finish with [Outro] and/or [End].');
    if (!sections.some((s) => s.type === 'end')) warnings.push('No [End] tag; Suno may run on past the outro.');
    const hooks = sections.filter((s) => /^(hook|chorus)/.test(s.type)).length;
    if (hooks < 2) warnings.push('Fewer than two hook sections; radio structure usually repeats the hook.');
  }

  // Sliders and model
  const sl = block.sliders || {};
  for (const k of ['weirdness', 'styleInfluence']) {
    const v = sl[k];
    const r = SPEC.sliders[k];
    if (typeof v !== 'number' || v < r.min || v > r.max) errors.push(`${k} must be a number in ${r.min}-${r.max}.`);
  }
  if (block.model && !SPEC.models.list.includes(block.model.pick)) errors.push(`Model "${block.model.pick}" is not in the verified list: ${SPEC.models.list.join(', ')}.`);

  // Required sections of the block
  for (const k of ['model', 'toggles', 'style', 'lyrics', 'exclude', 'sliders', 'variations', 'postGen']) if (!block[k]) errors.push(`Block is missing section "${k}".`);

  return {
    ok: errors.length === 0,
    errors, warnings,
    stats: { styleChars: style.length, lyricsChars: lyrics.length, excludeChars: exclude.length, sections: sections.length },
  };
}

/**
 * Parse a rendered markdown block (as written by render.js / examples) back into a block object.
 */
function parseMarkdownBlock(md) {
  // Split on level-2/3 headings and return the body of the first section whose heading matches the label.
  const chunks = md.split(/^(?=#{2,3}\s)/m);
  const grab = (label) => {
    const re = new RegExp(`^#{2,3}\\s*\\d*\\.?\\s*${label}`, 'i');
    const hit = chunks.find((c) => re.test(c));
    return hit ? hit.replace(/^[^\n]*\n/, '') : '';
  };
  const code = (s) => { const m = s.match(/```[a-z]*\n([\s\S]*?)```/); return m ? m[1].trim() : s.trim(); };
  const modelSec = grab('MODEL');
  const toggles = grab('MODE\\/TOGGLES');
  const styleSec = grab('STYLE FIELD');
  const lyricsSec = grab('LYRICS FIELD');
  const exSec = grab('EXCLUDE STYLES FIELD');
  const slSec = grab('SLIDERS');
  const varSec = grab('VARIATIONS');
  const postSec = grab('POST-GEN NOTES');
  const num = (re, s) => { const m = s.match(re); return m ? Number(m[1]) : NaN; };
  const bpmM = md.match(/(\d{2,3})\s*BPM/i);
  const h1 = (md.match(/^#\s+suno-forge prompt block:\s*(.+)$/m) || [])[1] || '';
  const laneLine = (md.match(/^Lane\s+`[^`]+`.*$/m) || [])[0] || '';
  const codeBlocks = [...varSec.matchAll(/```[a-z]*\n([\s\S]*?)```/g)].map((m) => m[1].trim());
  const modelPick = (modelSec.match(/\*\*Pick:\*\*\s*`?([^`\n]+?)`?\s*(?:\n|$)/) || [])[1];
  return {
    meta: { bpm: bpmM ? Number(bpmM[1]) : undefined, laneLabel: [h1, laneLine].filter(Boolean).join(' | ') },
    model: { pick: modelPick ? modelPick.trim() : undefined, rationale: modelSec },
    toggles: { customMode: /Custom Mode:\*{0,2}\s*ON/i.test(toggles), instrumental: /Instrumental:\*{0,2}\s*ON/i.test(toggles), personaNote: toggles },
    style: code(styleSec),
    lyrics: code(lyricsSec),
    exclude: code(exSec),
    sliders: { weirdness: num(/Weirdness:\*{0,2}\s*(\d+)/i, slSec), styleInfluence: num(/Style Influence:\*{0,2}\s*(\d+)/i, slSec), audioInfluence: slSec, weirdnessWhy: slSec },
    variations: { safer: { style: codeBlocks[0] || '' }, weirder: { style: codeBlocks[1] || '' } },
    postGen: postSec ? [postSec] : [],
  };
}

module.exports = { checkBlock, parseMarkdownBlock, findBannedNames, findAudioRefs, parseLyrics };
