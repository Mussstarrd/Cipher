#!/usr/bin/env node
'use strict';
/**
 * Self-audit: what actually reaches Suno. Survival rate of every slot after the 1000-char cut,
 * top-phrase share per slot, head length, anchor/lead contradictions, context errors (hook/verse
 * words in loop layouts, hats in bare mode), repeated cues inside one lyric.
 *   node scripts/selfaudit.js [rolls=2000]
 */
const { generate, PROMISES, LAYOUTS } = require('../lib/engine');
const STRIPPED = new Set(LAYOUTS.filter((l) => l.bare || l.minimal >= 0.9).map((l) => l.id));
const N = Number(process.argv[2]) || 2000;
const QUIET = process.argv.includes('--json');
const surv = {}, share = {}, headLens = [];
let contradictions = 0, loopCtx = 0, bareHats = 0, repeatedCue = 0, loops = 0, bares = 0, attTagMissing = 0;
const bump = (o, k) => { o[k] = (o[k] || 0) + 1; };
const slot = (name, present) => { surv[name] = surv[name] || { n: 0, kept: 0 }; surv[name].n++; if (present) surv[name].kept++; };
const any = {};
const anywhere = (name, present) => { any[name] = any[name] || { n: 0, kept: 0 }; any[name].n++; if (present) any[name].kept++; };
const sh = (name, v) => { if (!v) return; share[name] = share[name] || {}; bump(share[name], v); };
for (let i = 0; i < N; i++) {
  const b = generate({ lane: 'any', seed: i * 7919 + 11 });
  const P = b.parts, s = b.style, head = s.split('. lead:')[0];
  headLens.push(head.length);
  slot('lead.hook device (tag in head)', head.includes(P.lead.hookTag)); slot('lead.hook device (full line)', s.includes(P.lead.hook)); slot('lead.human', s.includes(P.lead.human)); slot('lead.articulation', s.includes(P.lead.articulation));
  slot('lead.motif', s.includes(P.lead.motif) || /riff|loop|hook|stab|figure|melody|-note|-chord|two notes|three notes|four notes|two chords/.test(P.lead.source));
  slot('lead.register', s.includes(P.lead.register)); slot('lead.placement', s.includes(P.lead.placement)); slot('lead.character', s.includes(P.lead.character));
  slot('bass.tone', s.includes(P.bass.tone)); slot('bass.pattern', s.includes(P.bass.pattern)); slot('bass.human', s.includes(P.bass.human));
  slot('drums.hats', s.includes(P.drums.hats)); slot('drums.color', s.includes(P.drums.color)); slot('drums.twitch clause', s.includes(P.drums.twitch.sound));
  if (P.rhythm.slur) slot('drums.slur phrase', s.includes(P.drums.slur));
  if (P.hint) { slot('hint.source', s.includes(P.hint.source)); slot('hint.character', s.includes(P.hint.character)); slot('hint.role', s.includes(P.hint.role)); }
  if (P.sprinkle) { slot('sprinkle head tag', head.includes(`${P.sprinkle.handle} once only`)); slot('sprinkle described in lyrics', b.lyrics.includes(P.sprinkle.source)); }
  if (P.counter && !P.bare) slot('counter (any)', s.includes('counter:'));
  slot('attitude tag', s.includes(P.attitudeTag)); if (!s.includes(P.attitudeTag)) attTagMissing++;
  sh('sync tag', P.rhythm.sync); sh('twitch tag', P.rhythm.twitch); sh('motif', P.lead.motif); sh('articulation', P.lead.articulation); sh('hook device', P.lead.hook);
  sh('lead human', P.lead.human); sh('anti phrase', P.anti); sh('attitude tag', P.attitudeTag); sh('anchor', b.meta.lane + ': ' + head.split(',')[0]); sh('lead source', P.lead.source); sh('register', P.lead.register); sh('placement', P.lead.placement);
  sh('twitch sound', P.drums.twitch.sound); sh('twitch rhythm', P.drums.twitch.rhythm); sh('feel', P.drums.feel); sh('motion', P.lead.motion); sh('bass source', b.meta.lane + ': ' + P.bass.source);
  // what the anchor words in the opening promise must be written in the prose lines or the lyrics
  const anchorWords = b.meta.headStyle === 'anchor' ? head.split(/,? ?\d+ BPM/)[0] : (head.split(/ in [A-G][#b]? \w+, /)[1] || '').split(',')[0];
  const prose = s.slice(head.length) + '\n' + b.lyrics;
  for (const [word, , proof] of PROMISES) { const m = word.exec(anchorWords); if (m && !proof.test(prose)) { contradictions++; if (contradictions <= 6 && !QUIET) console.log(`  contradiction: opening says "${m[0]}" | ${s.slice(0, 160)}`); break; } }
  if (b.meta.mode === 'loop') { loops++; if (/\b(hook|verse)\b/i.test(s + b.lyrics)) loopCtx++; }
  if (P.bare) { bares++; if (s.includes(P.drums.hats)) bareHats++; }
  // a feel, motion, twitch or anti-transition cue said twice inside one lyric (layout words and the switch axis repeat on purpose)
  const once = [P.drums.feel, P.lead.motion, P.anti, P.rhythm.slur ? P.drums.slur : null, P.rhythm.sync, P.drums.twitch.sound].filter(Boolean);
  if (once.some((c) => b.lyrics.split(c).length > 2)) repeatedCue++;
  const both = s + '\n' + b.lyrics;
  anywhere('hook device', both.includes(P.lead.hook)); anywhere('motif', both.includes(P.lead.motif) || /riff|loop|hook|stab|figure|melody|-note|-chord|two notes|three notes|four notes|two chords/.test(P.lead.source));
  if (!P.bare && !STRIPPED.has(b.meta.layout)) anywhere('hats phrase', both.includes(P.drums.hats)); anywhere('bass pattern', both.includes(P.bass.pattern));
  if (P.rhythm.slur) anywhere('slur phrase', both.includes(P.drums.slur)); if (P.hint) anywhere('hint role', both.includes(P.hint.role));
}
const pct = (a, b) => `${(100 * a / b).toFixed(0)}%`;
const rate = (k) => (surv[k] ? surv[k].kept / surv[k].n : 1);
const result = { rolls: N, survival: Object.fromEntries(Object.keys(surv).map((k) => [k, rate(k)])), anywhere: Object.fromEntries(Object.entries(any).map(([k, v]) => [k, v.kept / v.n])), headMedian: [...headLens].sort((a, b) => a - b)[N >> 1], contradictions: contradictions / N, loopContext: loopCtx, bareHats, repeatedCue: repeatedCue / N, attitudeTagCut: attTagMissing, variety: Object.fromEntries(Object.entries(share).map(([k, o]) => [k, Object.keys(o).length])) };
if (QUIET) { console.log(JSON.stringify(result)); process.exit(0); }
console.log(`\n== survival after the 1000-char cut (${N} rolls) ==`);
for (const [k, v] of Object.entries(surv).sort((a, b) => a[1].kept / a[1].n - b[1].kept / b[1].n)) console.log(`${pct(v.kept, v.n).padStart(5)}  ${k}`);
console.log(`\n== reaches Suno in the Style field or the Lyrics field ==`);
for (const [k, v] of Object.entries(any)) console.log(`${pct(v.kept, v.n).padStart(5)}  ${k}`);
console.log(`\n== pool variety: distinct / top share ==`);
for (const [k, o] of Object.entries(share)) { const t = Object.entries(o).sort((a, b) => b[1] - a[1]); console.log(`${String(t.length).padStart(4)} distinct, top ${pct(t[0][1], N).padStart(4)}  ${k}: "${t[0][0].slice(0, 60)}"`); }
headLens.sort((a, b) => a - b);
console.log(`\nhead length: median ${headLens[N >> 1]}, p90 ${headLens[Math.floor(N * 0.9)]}, max ${headLens[N - 1]}`);
console.log(`anchor promises a sound the roll lacks: ${contradictions} (${pct(contradictions, N)})`);
console.log(`hook/verse words inside loop layouts: ${loopCtx}/${loops}`);
console.log(`hats described in bare mode: ${bareHats}/${bares}`);
console.log(`a cue repeated inside one lyric: ${repeatedCue} (${pct(repeatedCue, N)})`);
console.log(`attitude tag cut from the head: ${attTagMissing} (${pct(attTagMissing, N)})`);
