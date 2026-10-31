#!/usr/bin/env node
'use strict';
/** Minimal test suite (no dependencies): engine invariants, QA rules, CLI round-trip. */
const assert = require('assert');
const { execFileSync } = require('child_process');
const fs = require('fs');
const os = require('os');
const path = require('path');
const { planSections, ARCS, barSeconds } = require('../lib/arrange');
const { generate, LANES, SPEC } = require('../lib/generate');
const { checkBlock, parseMarkdownBlock, findBannedNames, findAudioRefs } = require('../lib/qa');
const { renderBlock } = require('../lib/render');

let n = 0;
function test(name, fn) { n++; try { fn(); console.log(`ok   ${name}`); } catch (e) { console.log(`FAIL ${name}\n     ${e.message}`); process.exitCode = 1; } }

test('spec is current (v6 family only)', () => {
  assert.deepStrictEqual(SPEC.models.list, ['v6', 'v6-wild', 'v6-mini']);
  assert.strictEqual(SPEC.fields.style.limit, 1000);
  assert.strictEqual(SPEC.fields.lyrics.limit, 5000);
  assert.strictEqual(SPEC.sliders.audioInfluence.used, false);
});

test('every arc at every lane tempo lands in the radio window', () => {
  for (const arc of Object.keys(ARCS)) for (const lane of Object.values(LANES)) for (const bpm of [lane.bpm.min, lane.bpm.default, lane.bpm.max]) {
    const p = planSections(arc, bpm, { halfTime: lane.bpm.halfTime && bpm >= 120 });
    assert(p.totalSeconds >= 150 && p.totalSeconds <= 210, `${arc}@${bpm}: ${p.totalSeconds}s`);
    assert(p.introBars <= 8, `${arc}@${bpm}: intro ${p.introBars}`);
    assert(p.hookAtSeconds <= 45, `${arc}@${bpm}: hook at ${p.hookAtSeconds}`);
    assert(p.sections.some((s) => ['break', 'bridge', 'drop', 'switch', 'pre'].includes(s.type)));
  }
});

test('half-time lanes get 16-written-bar hooks', () => {
  const p = planSections('flat-bang', 140, { halfTime: true });
  assert(p.sections.filter((s) => s.type === 'hook').every((s) => s.bars === 16));
  assert.strictEqual(barSeconds(120), 2);
});

test('all lanes x seeds x arcs pass QA with no warnings', () => {
  for (const lane of Object.keys(LANES)) for (const seed of [1, 2, 3]) for (const arc of Object.keys(ARCS)) {
    const b = generate({ lane, seed, arc });
    const r = checkBlock(b);
    assert(r.ok, `${lane}/${seed}/${arc}: ${r.errors.join(' | ')}`);
    assert.strictEqual(r.warnings.length, 0, `${lane}/${seed}/${arc}: ${r.warnings.join(' | ')}`);
    assert(b.style.length <= SPEC.fields.style.target, `style too long: ${b.style.length}`);
    assert(/instrumental/i.test(b.style.split(',').slice(0, 3).join(',')), 'instrumental not in first three tags');
    assert(b.lyrics.startsWith('[Instrumental]') && b.lyrics.trim().endsWith('[End]'));
    assert(!/\(/.test(b.lyrics), 'parentheses in lyrics');
    assert.strictEqual(b.toggles.variety, 'Off');
    assert.strictEqual(b.toggles.maxMode, true);
  }
});

test('generation is deterministic for a seed', () => {
  const a = generate({ lane: 'jid-dark', seed: 9 }), b = generate({ lane: 'jid-dark', seed: 9 });
  assert.strictEqual(a.style, b.style); assert.strictEqual(a.lyrics, b.lyrics);
  assert.notStrictEqual(generate({ lane: 'jid-dark', seed: 10 }).style, a.style);
});

test('blend builds a custom hybrid', () => {
  const b = generate({ lane: 'hybrid', blend: ['phonk-experimental', 'paak-pocket', 'connor-price'], seed: 1 });
  assert(/cowbell/i.test(b.style) && /(Rhodes|bass guitar)/i.test(b.style));
  assert(checkBlock(b).ok);
});

test('QA catches artist names, inline negatives, audio refs, sung lines, retired models', () => {
  const b = generate({ lane: 'connor-price', seed: 1 });
  const bad = JSON.parse(JSON.stringify(b));
  bad.style = 'Connor Price type beat, no vocals, ' + bad.style;
  bad.lyrics = bad.lyrics.replace('[End]', 'la la la\n(oohs)\n[End]');
  bad.exclude = 'no vocals, drums';
  bad.model.pick = 'v5';
  bad.postGen.push('Upload a reference clip and raise Audio Influence to 60.');
  const r = checkBlock(bad);
  const txt = r.errors.join('\n');
  for (const needle of ['artist name', 'Inline negative', 'non-bracketed', 'parentheses', 'bare nouns', 'not in the verified list', 'Audio Influence']) assert(txt.includes(needle), `missing error for ${needle}:\n${txt}`);
  assert(findBannedNames('a dilla swing feel').length > 0);
  assert.strictEqual(findAudioRefs('Audio Influence is not used here.', true).length, 0);
  assert(findAudioRefs('Try Audio Influence at 40%.', true).length > 0);
});

test('QA catches structure violations', () => {
  const b = generate({ lane: 'paak-pocket', seed: 1 });
  const bad = JSON.parse(JSON.stringify(b));
  bad.lyrics = '[Instrumental]\n[Short Instrumental Intro - 24 bars: pads]\n[Hook 1 - 8 bars: drums]\n[Verse 1 - 16 bars: drums]\n[Outro - 4 bars: fade]';
  const r = checkBlock(bad);
  const txt = r.errors.join('\n');
  assert(txt.includes('Intro is 24 bars'));
  assert(txt.includes('arrives at'));
  assert(txt.includes('No contrast section'));
  assert(txt.includes('outside 150-210'));
});

test('markdown round-trip: rendered block parses back and passes QA', () => {
  const b = generate({ lane: 'phonk-experimental', seed: 5 });
  const parsed = parseMarkdownBlock(renderBlock(b));
  assert.strictEqual(parsed.style, b.style);
  assert.strictEqual(parsed.lyrics, b.lyrics);
  assert.strictEqual(parsed.exclude, b.exclude);
  assert.strictEqual(parsed.model.pick, 'v6');
  assert.strictEqual(parsed.sliders.weirdness, b.sliders.weirdness);
  const r = checkBlock(parsed);
  assert(r.ok, r.errors.join(' | '));
});

test('CLI: flags, json, batch, qa subcommand', () => {
  const root = path.join(__dirname, '..');
  const out = execFileSync('node', ['forge.js', '--lane', 'jid-dark', '--bpm', '150', '--seed', '3', '--json'], { cwd: root, encoding: 'utf8' });
  const j = JSON.parse(out);
  assert.strictEqual(j.meta.bpm, 150); assert(j.qa.ok);
  const batch = JSON.parse(execFileSync('node', ['forge.js', '--lane', 'connor-price', '--batch', '4', '--format', 'json'], { cwd: root, encoding: 'utf8' }));
  assert.strictEqual(batch.length, 4); assert(batch.every((r) => r.qa.ok));
  assert(new Set(batch.map((r) => r.style)).size === 4, 'batch rows should differ');
  const tmp = path.join(os.tmpdir(), `forge-test-${Date.now()}.md`);
  fs.writeFileSync(tmp, renderBlock(generate({ lane: 'hybrid', seed: 1 })));
  const qa = execFileSync('node', ['forge.js', 'qa', tmp], { cwd: root, encoding: 'utf8' });
  assert(qa.includes('PASS'));
  fs.unlinkSync(tmp);
});

console.log(`\n${n} tests run${process.exitCode ? ', with failures' : ', all passed'}`);
