#!/usr/bin/env node
'use strict';
/**
 * Audit the engine over many seeds: filler-word scan, phrase shares (kick/snare/hats/lead family),
 * style-length histogram, drums-line survival, mode and layout shares, fingerprint uniqueness.
 *   node scripts/audit.js [rolls=2000] [lane=any]
 */
const { generate, LANES } = require('../lib/engine');
const Guard = require('../lib/guard');

const N = Number(process.argv[2]) || 2000;
const laneArg = process.argv[3] || 'any';
const lanes = laneArg === 'all' ? Object.keys(LANES) : [laneArg];

for (const lane of lanes) {
  const counts = { kick: {}, snare: {}, hats: {}, family: {}, layout: {}, mode: {}, key: {} };
  const lens = []; let drums = 0, fillers = 0, guardHits = 0; const fps = new Set();
  const FILLER = /\b(fills?|rolls?|risers?|sweeps?|transition\w*|build-?ups?|drops?|comes in|enters?|bar 4|4th bar|every other bar)\b/i;
  for (let i = 0; i < N; i++) {
    const b = generate({ lane, seed: i * 7919 + 3 });
    lens.push(b.style.length);
    if (/\bdrums:/.test(b.style)) drums++;
    if (FILLER.test(`${b.style}\n${b.lyrics}`)) fillers++;
    if (b.warnings.length) guardHits++;
    fps.add(b.fingerprint);
    const d = b.parts.drums;
    for (const [k, v] of [['kick', d.kick], ['snare', d.snare], ['hats', d.hats], ['family', b.parts.lead.family], ['layout', b.meta.layout], ['mode', b.meta.mode], ['key', b.meta.key]]) counts[k][v] = (counts[k][v] || 0) + 1;
  }
  const top = (o) => Object.entries(o).sort((a, b) => b[1] - a[1]);
  const pct = (x) => `${(100 * x / N).toFixed(1)}%`;
  console.log(`\n== ${lane} (${N} rolls) ==`);
  console.log(`style length: min ${Math.min(...lens)} avg ${Math.round(lens.reduce((a, b) => a + b) / N)} max ${Math.max(...lens)} | drums line kept ${pct(drums)} | filler words ${fillers} | guard warnings ${guardHits} | unique fingerprints ${fps.size}/${N}`);
  const hist = {}; for (const l of lens) { const b = Math.floor(l / 100) * 100; hist[b] = (hist[b] || 0) + 1; }
  console.log('length histogram: ' + Object.entries(hist).sort((a, b) => a[0] - b[0]).map(([b, c]) => `${b}s:${c}`).join('  '));
  for (const k of ['kick', 'snare', 'hats']) { const t = top(counts[k]); console.log(`${k}: ${t.length} distinct, top ${pct(t[0][1])} "${t[0][0].slice(0, 60)}"`); }
  console.log('lead families: ' + top(counts.family).map(([k, v]) => `${k} ${pct(v)}`).join(', '));
  console.log('modes: ' + top(counts.mode).map(([k, v]) => `${k} ${pct(v)}`).join(', '));
  console.log('layouts: ' + top(counts.layout).map(([k, v]) => `${k} ${pct(v)}`).join(', '));
  console.log('keys: ' + top(counts.key).slice(0, 8).map(([k, v]) => `${k} ${pct(v)}`).join(', '));
}
console.log(`\nguard rules: ${Guard.RULES.map((r) => r.name).join(' | ')}`);
