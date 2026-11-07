#!/usr/bin/env node
'use strict';
/**
 * suno-forge CLI: copy-ready Suno prompt blocks for radio-ready hip hop instrumentals.
 *
 *   node forge.js                                   interactive (lane / BPM / key / mood / mode)
 *   node forge.js --lane phonk-experimental --bpm 140 --mood "dark" --mode loop --seed 7 --qa
 *   node forge.js --lane any                        one button: weighted random lane
 *   node forge.js --config my.json                  JSON config (same keys as flags)
 *   node forge.js --lane jid-dark --json            machine-readable block
 *   node forge.js --lane paak-pocket --batch 5 [--format table|json|blocks]
 *   node forge.js qa examples/*.md                  validate rendered blocks
 *   node forge.js lanes | layouts | spec            list lanes, lyrics layouts, or the verified spec
 */
const fs = require('fs');
const path = require('path');
const readline = require('readline');
const { generate, LANES, LAYOUTS, SPEC } = require('./lib/engine');
const { renderBlock, renderBatchTable } = require('./lib/render');
const { checkBlock, parseMarkdownBlock } = require('./lib/qa');

const FLAGS = {
  lane: 'Lane: any | ' + Object.keys(LANES).join(' | '),
  bpm: 'Tempo in BPM (lane range if omitted)',
  key: 'Key, e.g. "F minor", "D dorian", "Eb major" (lane colour if omitted)',
  mood: 'Mood words, comma-separated (guarded; anything that trips the guard is dropped and reported)',
  mode: 'Lyrics mode: loop | straight | switch (layout decides if omitted)',
  layout: 'Force a lyrics layout: ' + LAYOUTS.map((l) => l.id).join(' | '),
  constraints: 'Must-have descriptors, comma-separated (head tags; guarded)',
  exclude: 'Extra Exclude Styles terms, comma-separated',
  weirdness: 'Override Weirdness % (0-100)',
  styleInfluence: 'Override Style Influence % (0-100)',
  sprinkle: 'Force the mystery ingredient on (1) or off (0); random on about a third of rolls if omitted',
  headStyle: 'How the Styles field opens: anchor | leadfirst | drumsfirst (random if omitted)',
  minimal: 'Bare mode: the lead, one kick, one bass, silence; forces a minimal layout',
  hint: 'Force the extra ingredient: fuzzlament | machineglitch | staxtwitch | hits | none (random about half the time if omitted)',
  wild: 'Pick v6-wild as the model for this block',
  seed: 'Seed for reproducible output (number or string; random if omitted)',
  title: 'Song title',
  config: 'Path to a JSON config with any of the above keys',
  batch: 'Generate N varied rolls for A/B testing',
  format: 'Batch output: table (default) | json | blocks',
  out: 'Write output to this file instead of stdout',
  json: 'Print the block as JSON',
  qa: 'Append the QA report to the block',
  help: 'Show help',
};

function parseArgs(argv) {
  const args = { _: [] };
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i];
    if (a.startsWith('--')) { const k = a.slice(2); const next = argv[i + 1]; if (next === undefined || next.startsWith('--')) args[k] = true; else { args[k] = next; i++; } }
    else args._.push(a);
  }
  return args;
}

function help() {
  console.log('suno-forge: Suno prompt generator for radio-ready hip hop instrumentals\n');
  console.log('Usage: node forge.js [command] [--flag value ...]\n\nCommands: (none)=generate | qa <files...> | lanes | layouts | spec\n\nFlags:');
  for (const [k, v] of Object.entries(FLAGS)) console.log(`  --${k.padEnd(16)} ${v}`);
  console.log('\nExamples:\n  node forge.js\n  node forge.js --lane any --seed 42 --qa\n  node forge.js --lane connor-price --bpm 140 --mood "sunny" --mode straight\n  node forge.js --config examples/config.hybrid.json --out out.md\n  node forge.js --lane phonk-experimental --batch 6 --format table\n  node forge.js qa examples/*.md');
}

let pipedAnswers = null;
async function ask(rl, q, def) {
  const prompt = `${q}${def !== undefined ? ` [${def}]` : ''}: `;
  if (pipedAnswers) { const a = pipedAnswers.length ? pipedAnswers.shift().trim() : ''; process.stderr.write(prompt + a + '\n'); return a || (def !== undefined ? String(def) : ''); }
  return new Promise((res) => rl.question(prompt, (a) => res(a.trim() || (def !== undefined ? String(def) : ''))));
}

async function interactive() {
  let rl = null;
  if (process.stdin.isTTY) rl = readline.createInterface({ input: process.stdin, output: process.stderr });
  else pipedAnswers = fs.readFileSync(0, 'utf8').split('\n');
  const say = (m) => process.stderr.write(m + '\n');
  say('suno-forge interactive. Press Enter to accept defaults.\n');
  const laneNames = ['any', ...Object.keys(LANES)];
  laneNames.forEach((l, i) => say(`  ${i + 1}. ${l.padEnd(20)} ${l === 'any' ? 'weighted random lane (one button)' : LANES[l].label}`));
  let lane = await ask(rl, '\nLane (number or name)', 'any');
  if (/^\d+$/.test(lane)) lane = laneNames[Number(lane) - 1] || 'any';
  const L = LANES[lane];
  const bpm = await ask(rl, L ? `BPM (${L.tempo.map((t) => t.join('-')).filter((v, i, a) => a.indexOf(v) === i).join(' or ')}; blank = lane pick)` : 'BPM (blank = lane pick)', '');
  const key = await ask(rl, 'Key (blank = lane colour)', '');
  const mood = await ask(rl, 'Mood words, comma-separated (blank = none)', '');
  const mode = await ask(rl, 'Mode: loop | straight | switch (blank = layout decides)', '');
  if (rl) rl.close();
  return { lane, bpm: bpm ? Number(bpm) : undefined, key: key || undefined, mood: mood || undefined, mode: mode || undefined };
}

function fromArgs(args) {
  const input = {};
  if (args.config) Object.assign(input, JSON.parse(fs.readFileSync(path.resolve(args.config), 'utf8')));
  for (const k of ['lane', 'bpm', 'key', 'mood', 'mode', 'layout', 'hint', 'headStyle', 'constraints', 'exclude', 'weirdness', 'styleInfluence', 'seed', 'title']) if (args[k] !== undefined && args[k] !== true) input[k] = args[k];
  if (args.wild) input.wild = true;
  if (args.minimal) input.minimal = true;
  if (args.sprinkle !== undefined) input.sprinkle = String(args.sprinkle) === '1' || args.sprinkle === true;
  for (const k of ['bpm', 'weirdness', 'styleInfluence']) if (input[k] !== undefined && input[k] !== '') input[k] = Number(input[k]);
  if (input.seed !== undefined && /^\d+$/.test(String(input.seed))) input.seed = Number(input.seed);
  return input;
}

function emit(text, args) {
  if (args.out) { fs.writeFileSync(path.resolve(args.out), text.endsWith('\n') ? text : text + '\n'); console.error(`Wrote ${args.out}`); }
  else process.stdout.write(text.endsWith('\n') ? text : text + '\n');
}

function batch(input, n, args) {
  const blocks = [];
  const seen = new Set();
  for (let i = 0; i < n; i++) {
    let blk;
    for (let tries = 0; tries < 20; tries++) {
      const seed = input.seed !== undefined ? `${input.seed}-${i}-${tries}` : Math.floor(Math.random() * 4294967295);
      blk = generate({ ...input, seed });
      if (!seen.has(blk.fingerprint)) break;
    }
    seen.add(blk.fingerprint);
    blocks.push(blk);
  }
  const fmt = args.format || 'table';
  if (fmt === 'json') return JSON.stringify(blocks.map((b) => ({ seed: b.meta.seed, lane: b.meta.lane, bpm: b.meta.bpm, key: b.meta.key, layout: b.meta.layout, mode: b.meta.mode, model: b.model.pick, maxMode: 'On', variety: 'Off', instrumental: 'On', title: b.meta.title, style: b.style, lyrics: b.lyrics, exclude: b.exclude, weirdness: b.sliders.weirdness, styleInfluence: b.sliders.styleInfluence, qa: checkBlock(b) })), null, 2);
  if (fmt === 'blocks') return blocks.map(renderBlock).join('\n\n---\n\n');
  return `# suno-forge batch: ${blocks[0].meta.laneLabel} (${n} rolls)\n\nModel ${blocks[0].model.pick}, Custom mode, Instrumental ON, Max Mode ON, Variety Off for every row. Each row is one of one (distinct fingerprint).\n\n` + renderBatchTable(blocks);
}

function qaFiles(files) {
  let failed = 0;
  for (const f of files) {
    const raw = fs.readFileSync(f, 'utf8');
    const block = f.endsWith('.json') ? JSON.parse(raw) : parseMarkdownBlock(raw);
    const r = checkBlock(block);
    console.log(`${r.ok ? 'PASS' : 'FAIL'}  ${f}  (style ${r.stats.styleChars}, lyrics ${r.stats.lyricsChars}, exclude ${r.stats.excludeChars} chars, ${r.stats.sections} sections)`);
    for (const e of r.errors) console.log(`   ERROR    ${e}`);
    for (const w of r.warnings) console.log(`   warning  ${w}`);
    if (!r.ok) failed++;
  }
  process.exitCode = failed ? 1 : 0;
  console.log(`\n${files.length - failed}/${files.length} passed`);
}

async function main() {
  const args = parseArgs(process.argv.slice(2));
  const cmd = args._[0];
  if (args.help || cmd === 'help') return help();
  if (cmd === 'lanes') { console.log(`any                  weighted random lane (one button)\n`); for (const [k, v] of Object.entries(LANES)) console.log(`${k.padEnd(20)} ${v.tempo.map((t) => t.join('-')).filter((x, i, a) => a.indexOf(x) === i).join(' or ')} BPM  modes ${v.modes.map(([m, w]) => `${m} ${w}%`).join(', ')}\n${' '.repeat(20)} ${v.blurb}\n`); return; }
  if (cmd === 'layouts') { for (const l of LAYOUTS) console.log(`${l.id.padEnd(22)} ${l.mode.padEnd(8)} ${l.label}`); return; }
  if (cmd === 'spec') { console.log(JSON.stringify({ verifiedOn: SPEC.verifiedOn, models: SPEC.models, fields: SPEC.fields, sliders: SPEC.sliders, maxMode: SPEC.maxMode, duration: SPEC.duration, instrumental: SPEC.instrumental }, null, 2)); return; }
  if (cmd === 'qa') { if (args._.length < 2) throw new Error('qa needs at least one file'); return qaFiles(args._.slice(1)); }

  let input = fromArgs(args);
  if (!Object.keys(input).length) input = await interactive();
  if (args.batch) return emit(batch(input, Number(args.batch), args), args);

  const block = generate(input);
  const report = checkBlock(block);
  if (args.json) return emit(JSON.stringify({ ...block, qa: report }, null, 2), args);
  let text = renderBlock(block);
  if (block.warnings.length) text += `\n\n## GENERATOR NOTES\n` + block.warnings.map((w) => `- ${w}`).join('\n');
  if (args.qa || !report.ok) text += `\n\n## QA ${report.ok ? 'PASS' : 'FAIL'}\n` + [...report.errors.map((e) => `- ERROR: ${e}`), ...report.warnings.map((w) => `- warning: ${w}`)].join('\n');
  emit(text, args);
  if (!report.ok) process.exitCode = 2;
}

main().catch((e) => { console.error(`forge: ${e.message}`); process.exit(1); });
