#!/usr/bin/env node
'use strict';
/**
 * suno-forge CLI: generate copy-ready Suno prompt blocks for radio-ready hip hop instrumentals.
 *
 *   node forge.js                               interactive (asks lane / BPM / key / mood / arc)
 *   node forge.js --lane phonk-experimental --bpm 140 --mood "haunted, hazy" --arc stop-start
 *   node forge.js --config my.json              JSON config (same keys as flags)
 *   node forge.js --lane jid-dark --json        machine-readable block
 *   node forge.js --lane paak-pocket --batch 5 [--format table|json|blocks]
 *   node forge.js --blend phonk-experimental,paak-pocket,connor-price
 *   node forge.js qa examples/*.md              validate rendered blocks
 *   node forge.js lanes | arcs | spec           list lanes, energy arcs, or the verified spec
 */
const fs = require('fs');
const path = require('path');
const readline = require('readline');
const { generate, LANES, ARCS, SPEC } = require('./lib/generate');
const { renderBlock, renderBatchTable } = require('./lib/render');
const { checkBlock, parseMarkdownBlock } = require('./lib/qa');

const FLAGS = {
  lane: 'Lane: ' + Object.keys(LANES).join(' | '),
  bpm: 'Tempo in BPM (lane default if omitted)',
  key: 'Key, e.g. "F minor" (lane default if omitted)',
  mood: 'Mood words, comma-separated (placed right after the genre in the Style field)',
  arc: 'Energy arc: ' + Object.keys(ARCS).join(' | '),
  constraints: 'Extra descriptors, comma-separated (instruments, textures, must-haves)',
  exclude: 'Extra Exclude Styles terms, comma-separated',
  weirdness: 'Override Weirdness % (0-100)',
  styleInfluence: 'Override Style Influence % (0-100)',
  seed: 'Seed for reproducible output (number or string)',
  title: 'Song title',
  blend: 'Custom hybrid: three lanes "drums,harmony,texture" (drums from 1st, bass+harmony from 2nd, texture+mix from 3rd)',
  config: 'Path to a JSON config with any of the above keys',
  batch: 'Generate N varied prompts for A/B testing',
  format: 'Batch output: table (default) | json | blocks',
  varyArc: 'In batch mode, cycle energy arcs across rows (default keeps one arc so Lyrics stay identical)',
  out: 'Write output to this file instead of stdout',
  json: 'Print the block as JSON',
  qa: 'Run the QA checker on the generated block and print the report',
  help: 'Show help',
};

function parseArgs(argv) {
  const args = { _: [] };
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i];
    if (a.startsWith('--')) {
      const k = a.slice(2);
      const next = argv[i + 1];
      if (next === undefined || next.startsWith('--')) args[k] = true;
      else { args[k] = next; i++; }
    } else args._.push(a);
  }
  return args;
}

function help() {
  console.log('suno-forge: Suno prompt generator for radio-ready hip hop instrumentals\n');
  console.log('Usage: node forge.js [command] [--flag value ...]\n\nCommands: (none)=generate | qa <files...> | lanes | arcs | spec\n\nFlags:');
  for (const [k, v] of Object.entries(FLAGS)) console.log(`  --${k.padEnd(16)} ${v}`);
  console.log('\nExamples:\n  node forge.js\n  node forge.js --lane connor-price --bpm 150 --mood "playful, sunny" --arc stop-start\n  node forge.js --config examples/config.hybrid.json --out out.md\n  node forge.js --lane phonk-experimental --batch 6 --format table\n  node forge.js qa examples/*.md');
}

let pipedAnswers = null;
async function ask(rl, q, def) {
  const prompt = `${q}${def !== undefined ? ` [${def}]` : ''}: `;
  if (pipedAnswers) { // non-TTY: answers were read from stdin up front; missing answers take the default
    const a = pipedAnswers.length ? pipedAnswers.shift().trim() : '';
    process.stderr.write(prompt + a + '\n');
    return a || (def !== undefined ? String(def) : '');
  }
  return new Promise((res) => rl.question(prompt, (a) => res(a.trim() || (def !== undefined ? String(def) : ''))));
}

async function interactive() {
  let rl = null;
  if (process.stdin.isTTY) rl = readline.createInterface({ input: process.stdin, output: process.stderr });
  else pipedAnswers = fs.readFileSync(0, 'utf8').split('\n');
  const say = (m) => process.stderr.write(m + '\n');
  say('suno-forge interactive. Press Enter to accept defaults.\n');
  const laneNames = Object.keys(LANES);
  laneNames.forEach((l, i) => say(`  ${i + 1}. ${l.padEnd(20)} ${LANES[l].label}`));
  let lane = await ask(rl, '\nLane (number or name)', 'hybrid');
  if (/^\d+$/.test(lane)) lane = laneNames[Number(lane) - 1] || 'hybrid';
  const L = LANES[lane] || LANES.hybrid;
  const bpm = await ask(rl, `BPM (${L.bpm.min}-${L.bpm.max}; ${L.bpm.note})`, L.bpm.default);
  const key = await ask(rl, `Key (${L.keys.slice(0, 3).join(' / ')})`, L.keys[0]);
  const mood = await ask(rl, 'Mood words, comma-separated', '');
  say('\nEnergy arcs:');
  for (const [k, v] of Object.entries(ARCS)) say(`  ${k.padEnd(14)} ${v.summary}`);
  const arc = await ask(rl, '\nEnergy arc', L.defaultArc);
  const constraints = await ask(rl, 'Extra constraints / must-have descriptors, comma-separated', '');
  if (rl) rl.close();
  return { lane, bpm: Number(bpm), key, mood, arc, constraints };
}

function fromArgs(args) {
  const input = {};
  if (args.config) {
    const cfg = JSON.parse(fs.readFileSync(path.resolve(args.config), 'utf8'));
    Object.assign(input, cfg);
  }
  for (const k of ['lane', 'bpm', 'key', 'mood', 'arc', 'constraints', 'exclude', 'weirdness', 'styleInfluence', 'seed', 'title', 'blend']) {
    if (args[k] !== undefined && args[k] !== true) input[k] = args[k];
  }
  for (const k of ['bpm', 'weirdness', 'styleInfluence']) if (input[k] !== undefined) input[k] = Number(input[k]);
  if (input.seed !== undefined && /^\d+$/.test(String(input.seed))) input.seed = Number(input.seed);
  if (typeof input.blend === 'string') input.blend = input.blend.split(',').map((s) => s.trim()).filter(Boolean);
  if (input.blend && input.blend.length < 2) throw new Error('--blend needs at least two lanes, e.g. --blend phonk-experimental,paak-pocket');
  if (input.blend && !input.lane) input.lane = 'hybrid';
  return input;
}

function emit(text, args) {
  if (args.out) { fs.writeFileSync(path.resolve(args.out), text.endsWith('\n') ? text : text + '\n'); console.error(`Wrote ${args.out}`); }
  else process.stdout.write(text.endsWith('\n') ? text : text + '\n');
}

function batch(input, n, args) {
  const arcs = Object.keys(ARCS);
  const lane = LANES[input.lane];
  const blocks = [];
  for (let i = 0; i < n; i++) {
    const seed = input.seed !== undefined ? `${input.seed}-${i}` : i + 1;
    const spread = lane ? Math.round((lane.bpm.max - lane.bpm.min) / 2) : 6;
    const bpm = input.bpm || (lane ? lane.bpm.default + ((i % 3) - 1) * Math.max(2, Math.round(spread / 2)) : undefined);
    const w = (lane ? lane.sliders.weirdness : 50) + [0, -10, 10, -20, 20][i % 5];
    // Keep the arc (and so the Lyrics field) constant unless --varyArc is set, so the A/B isolates Style wording.
    const arc = input.arc || (args.varyArc ? arcs[i % arcs.length] : (lane ? lane.defaultArc : 'beat-switch'));
    const blk = generate({ ...input, seed, bpm, arc, weirdness: input.weirdness ?? Math.max(0, Math.min(100, w)) });
    blocks.push(blk);
  }
  // Shared Lyrics/Exclude for a clean A/B unless arcs differ.
  const fmt = args.format || 'table';
  if (fmt === 'json') return JSON.stringify(blocks.map((b) => ({ seed: b.meta.seed, bpm: b.meta.bpm, key: b.meta.key, arc: b.meta.arc, model: b.model.pick, maxMode: b.toggles.maxMode, variety: 'Off', style: b.style, lyrics: b.lyrics, exclude: b.exclude, weirdness: b.sliders.weirdness, styleInfluence: b.sliders.styleInfluence, qa: checkBlock(b) })), null, 2);
  if (fmt === 'blocks') return blocks.map(renderBlock).join('\n\n---\n\n');
  return `# suno-forge batch: ${blocks[0].meta.laneLabel} (${n} variants)\n\nModel ${blocks[0].model.pick}, Custom mode, Instrumental ON, Max Mode ON, Variety Off for every row.\n\n` + renderBatchTable(blocks);
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
  if (cmd === 'lanes') { for (const [k, v] of Object.entries(LANES)) console.log(`${k.padEnd(20)} ${v.bpm.min}-${v.bpm.max} BPM${v.bpm.halfTime ? ' (half-time)' : ''}  default arc ${v.defaultArc}\n${' '.repeat(20)} ${v.description}\n`); return; }
  if (cmd === 'arcs') { for (const [k, v] of Object.entries(ARCS)) console.log(`${k.padEnd(14)} ${v.summary}`); return; }
  if (cmd === 'spec') { console.log(JSON.stringify({ verifiedOn: SPEC.verifiedOn, models: SPEC.models, fields: SPEC.fields, sliders: SPEC.sliders, maxMode: SPEC.maxMode, duration: SPEC.duration, instrumental: SPEC.instrumental }, null, 2)); return; }
  if (cmd === 'qa') { if (args._.length < 2) throw new Error('qa needs at least one file'); return qaFiles(args._.slice(1)); }

  let input = fromArgs(args);
  const hasInput = Object.keys(input).length > 0;
  if (!hasInput) input = await interactive(); // prompts go to stderr so stdout stays a clean copy block

  if (args.batch) return emit(batch(input, Number(args.batch), args), args);

  const block = generate(input);
  const report = checkBlock(block);
  if (args.json) return emit(JSON.stringify({ ...block, qa: report }, null, 2), args);
  let text = renderBlock(block);
  if (args.qa || !report.ok) {
    text += `\n\n## QA ${report.ok ? 'PASS' : 'FAIL'}\n` + [...report.errors.map((e) => `- ERROR: ${e}`), ...report.warnings.map((w) => `- warning: ${w}`)].join('\n');
  }
  emit(text, args);
  if (!report.ok) process.exitCode = 2;
}

main().catch((e) => { console.error(`forge: ${e.message}`); process.exit(1); });
