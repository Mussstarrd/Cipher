#!/usr/bin/env node
'use strict';
/** suno-forge test suite (no dependencies). Walks the grammar through the guard and rolls thousands of seeds. */
const assert = require('assert');
const { execFileSync } = require('child_process');
const fs = require('fs');
const os = require('os');
const path = require('path');
const Guard = require('../lib/guard');
const G = require('../lib/grammar');
const { LANES } = require('../lib/lanes');
const { LAYOUTS, estimate } = require('../lib/layouts');
const HINTS = require('../lib/hints');
const { generate, SPEC } = require('../lib/engine');
const { checkBlock, parseMarkdownBlock } = require('../lib/qa');
const { renderBlock } = require('../lib/render');

const SEEDS = Array.from({ length: 1500 }, (_, i) => i * 7919 + 3);
const LANE_IDS = Object.keys(LANES);
let n = 0;
function test(name, fn) { n++; try { fn(); console.log(`ok   ${name}`); } catch (e) { console.log(`FAIL ${name}\n     ${e.message.split('\n').slice(0, 6).join('\n     ')}`); process.exitCode = 1; } }

test('spec is current (v6 family only, audio influence unused)', () => {
  assert.deepStrictEqual(SPEC.models.list, ['v6', 'v6-wild', 'v6-mini']);
  assert.strictEqual(SPEC.fields.style.limit, 1000);
  assert.strictEqual(SPEC.sliders.audioInfluence.used, false);
});

test('every grammar, lane and layout string passes the guard', () => {
  const walk = (o, p, allow, deny) => {
    if (typeof o === 'string') { const h = Guard.check(o, allow, deny); assert.deepStrictEqual(h, [], `${p}: "${o}" -> ${h.map((x) => `${x.rule}:${x.word}`).join('; ')}`); }
    else if (o && typeof o === 'object' && !(o instanceof RegExp)) for (const k of Object.keys(o)) if (!['exclude', 'label', 'blurb', 'listenFor', 'id', 'deny', 'allow'].includes(k) && typeof o[k] !== 'function') walk(o[k], `${p}.${k}`, allow, deny);
  };
  walk(G, 'grammar', null, null);
  for (const [id, l] of Object.entries(LANES)) walk({ anchors: l.anchors, neutralAnchor: l.neutralAnchor, FAMILIES: l.FAMILIES, bass: l.bass, bassPattern: l.bassPattern, drums: l.drums, color: l.color, mixTag: l.mixTag, neutralMixTag: l.neutralMixTag }, `lanes.${id}`, l.allow, l.deny);
  walk(LAYOUTS.map((l) => l.sections), 'layouts', null, null);
  walk(HINTS, 'hints', null, null);
});

test('the guard catches the known drift words and names', () => {
  for (const bad of ['swung hi-hats', 'a waltz feel', 'hat rolls into the hook', 'the 808 comes in first', 'a riser into the drop', 'no vocals', 'cinematic strings', 'smooth Rhodes', 'a gospel choir', 'Connor Price type beat', 'like Metro Boomin', 'in the style of Drake'])
    assert.ok(Guard.check(bad).length > 0, `guard missed: ${bad}`);
  assert.deepStrictEqual(Guard.check('a dry knocking kick, hard cuts only, straight in on beat 1'), []);
});

test('same seed, same block; different seeds differ', () => {
  const a = generate({ lane: 'jid-dark', seed: 9 }), b = generate({ lane: 'jid-dark', seed: 9 });
  assert.strictEqual(a.style, b.style); assert.strictEqual(a.lyrics, b.lyrics);
  assert.notStrictEqual(generate({ lane: 'jid-dark', seed: 10 }).style, a.style);
});

test('every roll is guard-clean, inside the limits, and passes QA', () => {
  for (const lane of ['any', ...LANE_IDS]) for (const seed of SEEDS.slice(0, 600)) {
    const b = generate({ lane, seed });
    assert.deepStrictEqual(b.warnings, [], `${lane}/${seed}: ${b.warnings.join(' | ')}\n${b.style}`);
    assert.ok(b.style.length <= 1000 && b.lyrics.length <= 5000 && b.exclude.length <= 1000, `${lane}/${seed} over limit`);
    const r = checkBlock(b);
    assert.ok(r.ok, `${lane}/${seed}: ${r.errors.join(' | ')}\n${b.style}\n${b.lyrics}`);
  }
});

test('the head carries the identity in the front 300 chars and the drums line always survives', () => {
  for (const lane of LANE_IDS) for (const seed of SEEDS.slice(0, 400)) {
    const b = generate({ lane, seed });
    const head = b.style.slice(0, 300);
    const R = b.parts.rhythm;
    for (const slot of ['sync', 'twitch', 'mode', 'slur', 'low']) if (R[slot]) assert.ok(head.includes(R[slot]), `${lane}/${seed}: ${slot} not in head: ${b.style}`);
    assert.ok(head.includes(b.parts.lead.handle), `${lane}/${seed}: handle not in head`);
    assert.ok(/\binstrumental\b/.test(head) && /\d+ BPM/.test(head));
    assert.ok(b.style.indexOf('lead:') < b.style.indexOf('bass:') && b.style.indexOf('bass:') < b.style.indexOf('drums:'), b.style);
    assert.ok(b.style.includes(`drums: ${b.parts.drums.kick}`), `${lane}/${seed}: drums line lost`);
    assert.ok(b.style.includes(b.parts.anti), `${lane}/${seed}: anti-transition phrase lost`);
  }
});

test('lyrics are bracket-only, [Instrumental] first, one [End] last, handles reach the tags, no returns', () => {
  for (const lane of LANE_IDS) for (const seed of SEEDS.slice(0, 400)) {
    const b = generate({ lane, seed });
    const lines = b.lyrics.split('\n').filter(Boolean);
    for (const line of lines) assert.match(line, /^\[[^\]]+\]$/, line);
    assert.strictEqual(lines[0], '[Instrumental]');
    assert.strictEqual(lines[lines.length - 1], '[End]');
    assert.strictEqual((b.lyrics.match(/\[End\]/g) || []).length, 1);
    assert.ok(!/\breturns?\b/.test(b.lyrics));
    assert.ok(!/[()]/.test(b.lyrics));
    assert.ok(b.lyrics.includes(b.parts.lead.handle), `${lane}/${seed}: lead handle missing from lyrics`);
    assert.ok(!/\|\s*and\b|,\s*and bass only/.test(b.lyrics), `${lane}/${seed}: dropped placeholder left a dangling "and": ${b.lyrics}`);
    if (b.meta.mode === 'switch') { assert.match(b.lyrics, /\[Beat Switch \| /); const after = b.lyrics.split('[Beat Switch')[1]; assert.ok(after.includes(b.parts.switch.after) && b.style.includes(b.parts.switch.tag)); }
    if (b.meta.mode === 'loop') assert.ok(!/\[(Hook|Verse)/.test(b.lyrics) && (b.lyrics.match(/\[Loop \| /g) || []).length >= 4);
  }
});

test('"every N bars" is capped: one per roll, none in loop mode', () => {
  for (const lane of LANE_IDS) for (const seed of SEEDS.slice(0, 500)) {
    const b = generate({ lane, seed });
    const hits = new Set((`${b.style}\n${b.lyrics}`.match(new RegExp(Guard.EVERY.source, 'gi')) || []).map((h) => h.toLowerCase()));
    assert.ok(hits.size <= (b.meta.mode === 'loop' ? 0 : 1), `${lane}/${seed} ${b.meta.mode}: ${[...hits].join(' | ')}`);
  }
});

test('radio window: first full beat by 0:45 and every layout lands 2:30-3:35 at lane tempos', () => {
  for (const L of LAYOUTS) for (const [bpm, half] of [[86, false], [96, false], [106, false], [132, true], [140, true], [152, true]]) {
    const e = estimate(L, bpm, half, 4);
    assert.ok(e.hookAtSeconds <= 45, `${L.id}@${bpm}: hook at ${e.hookAtSeconds}`);
    assert.ok(e.totalSeconds >= 150 && e.totalSeconds <= 215, `${L.id}@${bpm}: ${Math.round(e.totalSeconds)}s`);
  }
});

test('rolls are one of one and lanes keep their identity', () => {
  const fps = new Set(SEEDS.map((seed) => generate({ lane: 'any', seed }).fingerprint));
  assert.ok(fps.size > SEEDS.length * 0.995, `${fps.size} unique of ${SEEDS.length}`);
  let phonkCowbell = 0, paakBassGuitar = 0, connorMajor = 0, connorN = 0, jidSwitch = 0, jidN = 0;
  for (const seed of SEEDS.slice(0, 500)) {
    const ph = generate({ lane: 'phonk-experimental', seed }); if (/cowbell/.test(ph.style)) phonkCowbell++;
    const pk = generate({ lane: 'paak-pocket', seed }); if (/bass guitar/.test(pk.style)) paakBassGuitar++; assert.ok(!/\b808\b/.test(pk.style), pk.style);
    const cp = generate({ lane: 'connor-price', seed }); connorN++; if (/major/.test(cp.meta.key)) connorMajor++;
    const jd = generate({ lane: 'jid-dark', seed }); jidN++; if (jd.meta.mode === 'switch') jidSwitch++;
  }
  assert.ok(phonkCowbell > 350, `phonk cowbell ${phonkCowbell}/500`);
  assert.ok(paakBassGuitar > 400, `paak bass guitar ${paakBassGuitar}/500`);
  assert.ok(connorMajor > connorN * 0.3, `connor major ${connorMajor}/${connorN}`);
  assert.ok(jidSwitch > jidN * 0.35, `jid switch ${jidSwitch}/${jidN}`);
});

test('titles come from the roll and rarely repeat; salting yields a new one', () => {
  const titles = SEEDS.map((seed) => generate({ lane: 'any', seed }).meta.title);
  const distinct = new Set(titles).size;
  assert.ok(distinct > titles.length * 0.9, `${distinct} distinct titles of ${titles.length}`);
  for (const t of titles.slice(0, 200)) { assert.ok(t.length <= 80 && /^[A-Z]/.test(t) && !/\b(\w+) \1\b/i.test(t), t); assert.deepStrictEqual(Guard.check(t), [], t); }
  const b = generate({ lane: 'jid-dark', seed: 11 });
  const { makeTitle } = require('../lib/engine');
  assert.notStrictEqual(makeTitle(b.meta.seed, b.parts, 1), b.meta.title);
});

test('phrase variety: no single kick, snare or hats phrase dominates a lane', () => {
  for (const lane of LANE_IDS) {
    const counts = { kick: {}, snare: {}, hats: {} };
    for (const seed of SEEDS.slice(0, 400)) { const d = generate({ lane, seed }).parts.drums; for (const k of Object.keys(counts)) counts[k][d[k]] = (counts[k][d[k]] || 0) + 1; }
    for (const k of Object.keys(counts)) { const top = Math.max(...Object.values(counts[k])); assert.ok(top < 400 * 0.35, `${lane} ${k}: top phrase ${top}/400`); }
  }
});

test('hints: present on roughly half of rolls, every family appears, handle reaches head, style and lyrics; hook device in the lead line', () => {
  const fams = {}; let withHint = 0, N = 0;
  for (const lane of LANE_IDS) for (const seed of SEEDS.slice(0, 300)) {
    const b = generate({ lane, seed }); N++;
    assert.ok(b.style.includes(b.parts.lead.hook) || b.style.length >= 900, `${lane}/${seed}: hook device missing with room to spare`);
    if (!b.parts.hint) continue;
    withHint++; fams[b.parts.hint.family] = (fams[b.parts.hint.family] || 0) + 1;
    assert.ok(b.style.slice(0, 380).includes(b.parts.hint.handle), `${lane}/${seed}: hint handle not in head: ${b.style}`);
    assert.ok(b.style.includes('hint:'), `${lane}/${seed}: hint line lost`);
    assert.ok(b.lyrics.includes(b.parts.hint.handle), `${lane}/${seed}: hint handle not in lyrics:\n${b.lyrics}`);
    assert.ok(checkBlock(b).ok);
  }
  assert.ok(withHint > N * 0.45 && withHint < N * 0.7, `${withHint}/${N} rolls carry a hint`);
  for (const f of Object.keys(HINTS.HINT_FAMILIES)) assert.ok(fams[f] > 20, `${f}: ${fams[f] || 0}`);
  const forced = generate({ lane: 'paak-pocket', seed: 3, hint: 'machineglitch' });
  assert.strictEqual(forced.parts.hint.family, 'machineglitch');
  assert.strictEqual(generate({ lane: 'paak-pocket', seed: 3, hint: 'none' }).parts.hint, null);
});

test('user mood and must-have words are guarded', () => {
  const b = generate({ lane: 'paak-pocket', seed: 1, mood: 'sun-soaked, swung, late-night', constraints: 'a choir pad, horn stabs up front' });
  assert.ok(b.style.includes('sun-soaked') && b.style.includes('late-night') && b.style.includes('horn stabs up front'));
  assert.ok(!/\bswung\b|choir/.test(b.style));
  assert.strictEqual(b.warnings.filter((w) => /dropped/.test(w)).length, 2, b.warnings.join(' | '));
  assert.ok(checkBlock(b).ok);
});

test('QA catches names, swing words, fills, negatives, parentheses, retired models and audio refs', () => {
  const b = generate({ lane: 'connor-price', seed: 1 });
  const bad = JSON.parse(JSON.stringify(b));
  bad.style = 'Connor Price type beat, swung hats, hat rolls into the hook, no vocals, ' + bad.style;
  bad.lyrics = bad.lyrics.replace('[End]', 'la la la\n(oohs)\n[End]');
  bad.exclude = 'no vocals, drums';
  bad.model.pick = 'v5';
  bad.postGen.push('Upload a reference clip and raise Audio Influence to 60.');
  const r = checkBlock(bad);
  const txt = r.errors.join('\n');
  for (const needle of ['names', 'meter and swing', 'transition and filler', 'negation', 'non-bracketed', 'parentheses', 'bare nouns', 'not in the verified list', 'Audio Influence']) assert.ok(txt.includes(needle), `missing error for ${needle}:\n${txt}`);
});

test('markdown round-trip: rendered block parses back and passes QA', () => {
  for (const lane of LANE_IDS) {
    const b = generate({ lane, seed: 5 });
    const parsed = parseMarkdownBlock(renderBlock(b));
    assert.strictEqual(parsed.style, b.style); assert.strictEqual(parsed.lyrics, b.lyrics); assert.strictEqual(parsed.exclude, b.exclude);
    assert.strictEqual(parsed.model.pick, b.model.pick); assert.strictEqual(parsed.meta.lane, lane);
    const r = checkBlock(parsed);
    assert.ok(r.ok, `${lane}: ${r.errors.join(' | ')}`);
  }
  const md = renderBlock(generate({ lane: 'connor-price', seed: 1 })).replace('# suno-forge prompt block: ', '# suno-forge prompt block: Connor Price lane: ');
  assert.ok(!checkBlock(parseMarkdownBlock(md)).ok);
});

test('CLI: flags, any lane, json, batch, qa subcommand, piped interactive', () => {
  const root = path.join(__dirname, '..');
  const j = JSON.parse(execFileSync('node', ['forge.js', '--lane', 'jid-dark', '--bpm', '150', '--seed', '3', '--json'], { cwd: root, encoding: 'utf8' }));
  assert.strictEqual(j.meta.bpm, 150); assert.ok(j.qa.ok);
  const any = JSON.parse(execFileSync('node', ['forge.js', '--lane', 'any', '--seed', '77', '--json'], { cwd: root, encoding: 'utf8' }));
  assert.ok(LANE_IDS.includes(any.meta.lane));
  const batch = JSON.parse(execFileSync('node', ['forge.js', '--lane', 'connor-price', '--batch', '4', '--seed', '1', '--format', 'json'], { cwd: root, encoding: 'utf8' }));
  assert.strictEqual(batch.length, 4); assert.ok(batch.every((r) => r.qa.ok)); assert.strictEqual(new Set(batch.map((r) => r.style)).size, 4);
  const tmp = path.join(os.tmpdir(), `forge-test-${Date.now()}.md`);
  fs.writeFileSync(tmp, renderBlock(generate({ lane: 'hybrid', seed: 1 })));
  assert.ok(execFileSync('node', ['forge.js', 'qa', tmp], { cwd: root, encoding: 'utf8' }).includes('PASS'));
  fs.unlinkSync(tmp);
  const piped = execFileSync('node', ['forge.js'], { cwd: root, encoding: 'utf8', input: '5\n138\n\ndark\nloop\n' });
  assert.ok(/Lane `phonk-experimental` \| 138 BPM/.test(piped) && /\[Loop \|/.test(piped), piped.slice(0, 300));
});

console.log(`\n${n} tests run${process.exitCode ? ', with failures' : ', all passed'}`);
