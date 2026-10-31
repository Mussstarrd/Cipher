'use strict';
const path = require('path');
const fs = require('fs');
const { planSections, renderLyrics, fmtTime, RADIO, ARCS } = require('./arrange');

const SPEC = JSON.parse(fs.readFileSync(path.join(__dirname, 'spec.json'), 'utf8'));
const LANES = JSON.parse(fs.readFileSync(path.join(__dirname, 'lanes.json'), 'utf8')).lanes;

/** Small seeded PRNG (mulberry32) so examples are reproducible. */
function rng(seed) {
  let a = (typeof seed === 'string' ? hashStr(seed) : seed) >>> 0;
  return function () {
    a |= 0; a = (a + 0x6D2B79F5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}
function hashStr(s) { let h = 2166136261; for (const c of s) { h ^= c.charCodeAt(0); h = Math.imul(h, 16777619); } return h >>> 0; }

/**
 * Pick `n` items from an ordered pool, biased toward the front (priority).
 * The first item is always included when n >= 1 and `keepFirst` is true.
 */
function pick(pool, n, rand, keepFirst = true) {
  if (!pool || !pool.length) return [];
  const out = [];
  const rest = [...pool];
  if (keepFirst) out.push(rest.shift());
  while (out.length < n && rest.length) {
    // triangular bias toward index 0
    const i = Math.floor(Math.min(rand(), rand()) * rest.length);
    out.push(rest.splice(i, 1)[0]);
  }
  return out;
}

function laneOrThrow(name) {
  const lane = LANES[name];
  if (!lane) throw new Error(`Unknown lane "${name}". Available: ${Object.keys(LANES).join(', ')}`);
  return lane;
}

/** Build a blended lane: drums from a, bass+harmony from b, texture+mix from c. */
function blendLanes(names) {
  const [a, b, c] = names.map(laneOrThrow);
  const third = c || a;
  const merge = (k) => [...new Set([...(a.pools[k] || []), ...(b.pools[k] || []), ...(third.pools[k] || [])])];
  return {
    label: `Custom blend: ${a.label} drums + ${b.label} pocket + ${third.label} texture`,
    description: `Custom blend. Drums from ${names[0]}, bass and harmony from ${names[1]}, texture and mix from ${names[2] || names[0]}.`,
    bpm: a.bpm,
    keys: b.keys,
    defaultArc: 'beat-switch',
    model: LANES.hybrid.model,
    sliders: LANES.hybrid.sliders,
    pools: {
      genre: [`${a.pools.genre[0].replace(' instrumental', '')} meets ${b.pools.genre[0].replace(' instrumental', '')} instrumental`, ...merge('genre')],
      drums: a.pools.drums, bass: b.pools.bass, harmony: b.pools.harmony,
      texture: third.pools.texture, mood: merge('mood'), mix: third.pools.mix, wild: merge('wild'),
    },
    safer: [a.pools.genre[0], a.pools.drums[0], a.pools.drums[1], b.pools.bass[0], b.pools.harmony[0], third.pools.texture[0], merge('mood')[0], third.pools.mix[0]],
    exclude: [...new Set([...a.exclude, ...b.exclude, ...third.exclude])].slice(0, 8),
    excludeVocalStyle: a.excludeVocalStyle || b.excludeVocalStyle,
    cues: Object.fromEntries(Object.keys(LANES.hybrid.cues).map((k) => [k, [...new Set([...(a.cues[k] || []), ...(b.cues[k] || []), ...(third.cues[k] || [])])]])),
    listenFor: [...a.listenFor.slice(0, 1), ...b.listenFor.slice(0, 1), ...third.listenFor.slice(0, 1)],
  };
}

function clampLen(parts, limit) {
  // Drop from the END (least important) until under limit.
  const out = [...parts];
  while (out.join(', ').length > limit && out.length > 1) out.pop();
  return out;
}

/**
 * Compose a Style field. Order = importance: genre -> user mood -> drums/groove ->
 * tempo/key -> bass -> harmony/instruments -> texture -> mood -> mix/master.
 */
function buildStyle(lane, input, rand, variant = 'main') {
  const p = lane.pools;
  const limit = SPEC.fields.style.limit;
  const target = input.styleTarget || SPEC.fields.style.target;
  const counts = {
    // Community guidance: comma tags, roughly 12-18 descriptors, genre/era first, production last.
    main:   { genre: 2, drums: 3, bass: 1, harmony: 3, texture: 1, mood: 2, mix: 2, wild: 0 },
    safer:  { genre: 1, drums: 3, bass: 1, harmony: 2, texture: 1, mood: 1, mix: 2, wild: 0 },
    weirder:{ genre: 2, drums: 3, bass: 1, harmony: 3, texture: 2, mood: 2, mix: 2, wild: 2 },
  }[variant];

  const parts = [];
  if (variant === 'safer' && lane.safer) {
    parts.push(...lane.safer.slice(0, 2));
  } else {
    parts.push(...pick(p.genre, counts.genre, rand));
  }
  parts.push('instrumental'); // community: keep "instrumental" inside the first three tags
  if (input.mood) parts.push(...splitList(input.mood));
  parts.push(...(variant === 'safer' && lane.safer ? lane.safer.slice(2, 4) : pick(p.drums, counts.drums, rand)));
  parts.push(`${input.bpm} BPM`);
  if (input.key) parts.push(input.key);
  if (lane.bpm.halfTime && input.bpm >= 120) parts.push('half-time feel');
  parts.push(...(variant === 'safer' && lane.safer ? [lane.safer[4]] : pick(p.bass, counts.bass, rand)));
  parts.push(...(variant === 'safer' && lane.safer ? lane.safer.slice(5, 6).concat(pick(p.harmony, 1, rand, false)) : pick(p.harmony, counts.harmony, rand)));
  if (input.constraints) parts.push(...splitList(input.constraints));
  if (input.arc === 'beat-switch') parts.push('beat switch halfway');
  if (input.arc === 'stop-start') parts.push('stop-start arrangement');
  parts.push(...pick(p.texture, counts.texture, rand, variant !== 'safer'));
  if (counts.wild) parts.push(...pick(p.wild, counts.wild, rand, false));
  parts.push(...pick(p.mood, counts.mood, rand, false));
  parts.push(...pick(p.mix, counts.mix, rand));

  // de-dupe (case-insensitive) preserving order; also drop a later descriptor whose
  // leading noun (e.g. "Rhodes", "cowbell") already leads an earlier one, and drop the
  // standalone "instrumental" when a genre tag already carries the word.
  const seen = new Set();
  const leads = new Set();
  let out = parts.filter((x, i) => {
    const k = x.toLowerCase().trim();
    if (!k || seen.has(k)) return false;
    if (k === 'instrumental' && parts.slice(0, i).some((g) => /instrumental/i.test(g))) return false;
    const lead = k.split(/[\s,]+/)[0];
    if (lead.length > 4 && !/^\d/.test(lead) && leads.has(lead) && !/^(live|dark|warm|clean|bright|tight|loose|heavy|dusty|crisp|swung|distorted|punchy|lazy|slow|hard|sparse)$/.test(lead)) return false;
    seen.add(k); leads.add(lead);
    return true;
  });
  out = clampLen(out, Math.min(limit, target));
  return out.join(', ');
}

function splitList(s) { return String(s).split(/[,;]+/).map((x) => x.trim()).filter(Boolean); }

function buildExclude(lane, input) {
  const preset = input.excludePreset || 'default';
  const core = SPEC.baseExclude.core[lane.excludeVocalStyle === 'lead' ? 'lead' : 'full'];
  let parts;
  if (preset === 'short') parts = [...core];
  else if (preset === 'full') parts = [...SPEC.baseExclude.presets.full.filter((t) => lane.excludeVocalStyle !== 'lead' || !/^(vocals|vocal chops|rap)$/.test(t)), ...(lane.excludeVocalStyle === 'lead' ? ['lead vocals', 'rapping'] : []), ...(lane.exclude || [])];
  else parts = [...core, ...(lane.exclude || []).slice(0, 4)];
  parts.push(...splitList(input.exclude || ''));
  const seen = new Set();
  const out = parts.filter((x) => { const k = x.toLowerCase(); if (seen.has(k)) return false; seen.add(k); return true; });
  return clampLen(out, SPEC.fields.exclude.limit).join(', ');
}

function cueFor(lane, rand, input) {
  const used = new Set();
  return (s) => {
    const key = s.variant && lane.cues[`${s.type}_${s.variant}`] ? `${s.type}_${s.variant}` : s.type;
    const pool = lane.cues[key] || lane.cues[s.type] || ['groove continues'];
    // first cue is the anchor; vary the rest, avoid repeating cue sets across same-type sections
    const n = s.type === 'intro' || s.type === 'outro' ? 3 : 4;
    let cues = pick(pool, n, rand);
    const sig = cues.join('|');
    if (used.has(sig)) cues = pick(pool, n, rand);
    used.add(sig);
    // Anti-grid cue for drum-bearing sections in swung lanes
    if (['hook', 'verse', 'drop'].includes(s.type) && input.antiGrid !== false) {
      const anti = ANTI_GRID[lane.bpm.halfTime ? 'half' : 'straight'];
      const extra = anti[Math.floor(rand() * anti.length)];
      if (!cues.some((c) => c.includes('swung') || c.includes('behind') || c.includes('ghost') || c.includes('off-grid'))) cues.push(extra);
    }
    // Drop cues that reference a bar number beyond this section's length.
    cues = cues.filter((c) => { const m = c.match(/bar (\d+)/); return !m || Number(m[1]) <= s.bars; });
    return cues;
  };
}

const ANTI_GRID = {
  straight: ['swung 16ths', 'kick pushed slightly late', 'ghost notes on the snare', 'loose human timing', 'drummer leans behind the beat'],
  half: ['hi-hats swung not straight', 'kick lands slightly behind the grid', 'off-grid snare placement', 'loose drum machine swing', 'rolls land lazily'],
};

function sliders(lane, input, variant) {
  let w = input.weirdness ?? lane.sliders.weirdness;
  let si = input.styleInfluence ?? lane.sliders.styleInfluence;
  if (variant === 'safer') { w = Math.max(0, w - 20); si = Math.min(100, si + 10); }
  if (variant === 'weirder') { w = Math.min(80, w + 15); si = Math.max(0, si - 10); }
  return { weirdness: w, styleInfluence: si };
}

/**
 * Generate a full prompt block object. input: { lane, bpm, key, mood, arc, constraints,
 * exclude, weirdness, styleInfluence, seed, blend, title, durationTarget }
 */
function generate(input = {}) {
  const laneName = input.lane || 'hybrid';
  const lane = input.blend ? blendLanes(input.blend) : laneOrThrow(laneName);
  const rand = rng(input.seed ?? `${laneName}-${input.bpm || ''}-${input.mood || ''}`);
  const bpm = Number(input.bpm) || lane.bpm.default;
  if (bpm < 60 || bpm > 200) throw new Error('BPM must be between 60 and 200');
  const key = input.key || lane.keys[Math.floor(rand() * Math.min(3, lane.keys.length))];
  const arc = input.arc || lane.defaultArc;
  if (!ARCS[arc]) throw new Error(`Unknown energy arc "${arc}". Available: ${Object.keys(ARCS).join(', ')}`);
  const halfTime = lane.bpm.halfTime && bpm >= 120;
  const plan = planSections(arc, bpm, { halfTime, targetSeconds: input.durationTarget });
  const full = { ...input, bpm, key, arc, lane: laneName };

  const style = buildStyle(lane, full, rng(`${rand()}-main`), 'main');
  const safer = buildStyle(lane, full, rng(`${rand()}-safer`), 'safer');
  const weirder = buildStyle(lane, full, rng(`${rand()}-weird`), 'weirder');
  const exclude = buildExclude(lane, full);
  const lyrics = renderLyrics(plan, cueFor(lane, rng(`${rand()}-cues`), full));
  const sl = sliders(lane, full, 'main');

  const modelPick = SPEC.models.default;
  const title = input.title || defaultTitle(laneName, key, bpm);

  return {
    meta: { lane: laneName, laneLabel: lane.label, bpm, key, arc, arcLabel: ARCS[arc].label, halfTime, seed: input.seed ?? null, generatedWithSpecVerifiedOn: SPEC.verifiedOn },
    model: {
      pick: modelPick,
      rationale: `${lane.model.rationale} ${SPEC.models.notes[modelPick]}`,
      wild: `${lane.model.wild} (Variant: ${SPEC.models.wild}; it defaults Variety to Off. ${SPEC.models.notes[SPEC.models.wild]})`,
      mini: `${SPEC.models.mini}: ${SPEC.models.notes[SPEC.models.mini]}`,
      alternatives: SPEC.models.list.filter((m) => m !== modelPick),
    },
    toggles: {
      customMode: true,
      modeName: SPEC.mode.name,
      instrumental: true,
      persona: false,
      personaNote: `${SPEC.personas.uiName}: ${SPEC.personas.note}`,
      maxMode: plan.totalSeconds > 120,
      maxModeNote: SPEC.maxMode.recommended + ` (${SPEC.maxMode.credits}).`,
      variety: 'Off',
      varietyNote: SPEC.sliders.variety.why,
      duration: `Custom ${durationSetting(plan.totalSeconds)} (planned ${plan.totalFormatted} = ${plan.sections.reduce((a, s) => a + s.bars, 0)} bars at ${bpm} BPM, plus a ${SPEC.duration.bufferSeconds} s buffer so [End] lands before the hard cut; Crop any tail). Do not use Auto: v6 runs long on Auto.`,
      title,
    },
    style,
    lyrics,
    exclude,
    sliders: {
      weirdness: sl.weirdness, weirdnessWhy: lane.sliders.weirdnessWhy,
      styleInfluence: sl.styleInfluence, styleInfluenceWhy: lane.sliders.styleInfluenceWhy,
      audioInfluence: 'Not used. No audio is uploaded in this workflow, so the slider does not even appear; leave it alone.',
      variety: `Off (Exact style). ${SPEC.sliders.variety.why}`,
    },
    variations: {
      safer: { style: safer, sliders: sliders(lane, full, 'safer') },
      weirder: { style: weirder, sliders: sliders(lane, full, 'weirder') },
    },
    postGen: postGenNotes(lane, plan, full),
    plan: {
      totalSeconds: Math.round(plan.totalSeconds), totalFormatted: plan.totalFormatted,
      introBars: plan.introBars, hookAt: plan.hookAtFormatted,
      sections: plan.sections.map((s) => ({ type: s.type, variant: s.variant || null, bars: s.bars, start: fmtTime(s.startSec), energy: s.energy })),
    },
    limits: { style: SPEC.fields.style.limit, lyrics: SPEC.fields.lyrics.limit, exclude: SPEC.fields.exclude.limit },
  };
}

function durationSetting(totalSeconds) {
  const t = Math.ceil((totalSeconds + SPEC.duration.bufferSeconds) / 5) * 5;
  return fmtTime(Math.min(t, 360));
}

function defaultTitle(lane, key, bpm) {
  const names = { 'connor-price': 'Bounce House', 'paak-pocket': 'Backseat Pocket', 'jid-dark': 'Low Ceiling', 'phonk-experimental': 'Cassette Cowbell', 'hybrid': 'Hazy Bounce' };
  return `${names[lane] || 'Untitled Instrumental'} (${key}, ${bpm} BPM)`;
}

function postGenNotes(lane, plan, input) {
  const f = SPEC.features;
  const notes = [];
  notes.push(`Structure check: intro should hand off to the first hook/drop around ${plan.hookAtFormatted}; total near ${plan.totalFormatted}. If Suno ignores a section, use Replace Section on that time range with a shorter, blunter cue (e.g. "[Break - 4 bars: drums only]").`);
  notes.push(...lane.listenFor);
  notes.push('Vocal leak: any hum, ad-lib or sung syllable means a re-roll, not a fix. Confirm the Instrumental toggle, keep the Exclude list intact, and do not add negatives to the Style field.');
  notes.push(`Ending: the [End] tag should produce a stop near ${plan.totalFormatted}. If Suno keeps playing, Crop to the last downbeat. If it stops early, Extend from the last good section and paste only the remaining section tags into the Extend lyrics box.`);
  notes.push('When to Extend vs re-roll: Extend when the first 60-90 s are keepers and only the back half drifts. Re-roll (and pick the safer or weirder variation) when the drums or genre are wrong from the start.');
  notes.push(`Remaster: ${f.remaster} Compare loudness against a current radio instrumental before export.`);
  notes.push(`Stems: ${f.stems} Export stems from the final take for a DAW polish (re-balance the 808, add a limiter).`);
  notes.push(`Prompt ignored? First check Variety is Off (v6 and v6-mini default it to Normal, which rewrites the Style text), then check Max Mode is on for a full-length beat. ${SPEC.fields.exclude.tip}`);
  notes.push(`If the mix goes muffled or thin after ~2:30, that is the reported v6 standard-mode drift: re-run with Max Mode on, or Extend from the last clean bar.`);
  notes.push('A/B: generate the main Style and one variation back to back with identical Lyrics/Exclude and sliders changed only as listed, so the comparison isolates the Style wording.');
  return notes;
}

module.exports = { generate, LANES, SPEC, ARCS, RADIO, blendLanes, splitList };
