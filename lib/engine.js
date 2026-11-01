'use strict';
/**
 * suno-forge engine v2. Builds every sound from grammar slots (physical description + handle),
 * resolves clashes, lays the Style field out front-weighted (head tags never cut, drums line
 * never lost), fills a lyrics layout with the same handles, and returns the full block object
 * the renderer, QA checker, CLI and page all consume.
 *
 * Style field order (Suno weights the front and reads it as tags):
 *   head: anchor, BPM (+ half-time), key, instrumental, user mood, SYNC, TWITCH, SLUR,
 *         minimal, mode tag (switch axis / loop / straight), must-haves, LOW LEAD,
 *         lead handle + timbre, mix tag                                  (~260 chars, never cut)
 *   prose: lead (every slot), bass, drums (always carries an anti-transition phrase), counter
 *          (trimmed from the end in a fixed cut order; the drums line survives)
 */
const fs = require('fs');
const path = require('path');
const G = require('./grammar');
const { LANES, LANE_WEIGHTS } = require('./lanes');
const { LAYOUTS, render: renderLayout, estimate, sectionBars } = require('./layouts');
const T = require('./theory');
const Guard = require('./guard');
const { HINT_FAMILIES, HINT_WEIGHTS, HOOK_DEVICES } = require('./hints');
const { fmtTime } = require('./arrange');

const SPEC = JSON.parse(fs.readFileSync(path.join(__dirname, 'spec.json'), 'utf8'));
const RADIO = { minSeconds: 150, maxSeconds: 210, hookByMax: 45, maxIntroBars: 8 };

function mulberry32(a) {
  return () => {
    a |= 0; a = (a + 0x6d2b79f5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}
function hashStr(s) { let h = 2166136261; for (const c of String(s)) { h ^= c.charCodeAt(0); h = Math.imul(h, 16777619); } return h >>> 0; }
const pick = (arr, r) => arr[Math.floor(r() * arr.length)];
const pickWhere = (arr, r, ok) => { const pool = arr.filter(ok); return pick(pool.length ? pool : arr, r); };
/** Base pools are shared by every lane; a lane's DENY rule removes the phrases it must not say ("808" in the live-pocket lane). */
let laneOk = () => true;
const lanePool = (arr) => { const pool = arr.filter((x) => laneOk(typeof x === 'string' ? x : `${x.tag} ${x.cue} ${x.after}`)); return pool.length ? pool : arr; };
const int = (lo, hi, r) => lo + Math.floor(r() * (hi - lo + 1));
const weighted = (entries, r) => { const total = entries.reduce((n, [, w]) => n + w, 0); let x = r() * total; for (const [v, w] of entries) if ((x -= w) < 0) return v; return entries[entries.length - 1][0]; };
const split = (entry) => entry.split('|');
const bare = (s) => s.replace(/^an? /, '');
/** Short drum word for a lyrics tag: the adjectives up to the first drum noun ("hand-played hats", "big layered clap"). */
const DRUM_NOUN = /\b(kick|snare|clap|snap|hats?|shaker|ride|cowbell|rimshot|cross-stick)\b/i;
function drumWord(source) {
  const first = bare(source).split(',')[0];
  const words = first.split(/\s+/);
  const idx = words.findIndex((w) => DRUM_NOUN.test(w));
  const cut = idx >= 0 ? words.slice(Math.max(0, idx - 3), idx + 1) : words.slice(0, 3);
  return cut.join(' ').replace(/^(and|of|with|the|a|an)\s+/i, '');
}
const EVERY = Guard.EVERY;

/** One "every N bars" phrase per roll, none in loop mode. */
function makeAllow(mode) {
  let left = mode === 'loop' ? 0 : 1;
  const fn = (phrase) => { if (EVERY.test(phrase)) left--; };
  fn.peek = () => left > 0;
  return fn;
}
const pickBudget = (arr, r, allow, ok = () => true) => {
  const chosen = pickWhere(arr, r, (x) => ok(x) && (!EVERY.test(x) || allow.peek()));
  allow(chosen);
  return chosen;
};

function buildLead(r, lane, allow) {
  const L = G.LEAD;
  const guest = lane.guestShare > 0 && r() < lane.guestShare;
  const family = guest ? weighted(G.GUEST_FAMILIES, r) : weighted(Object.entries(lane.families).filter(([f]) => lane.FAMILIES[f] || G.FAMILIES[f]), r);
  const F = lane.FAMILIES[family] || G.FAMILIES[family];
  const [source, handle] = split(pick(lanePool(F.sources), r));
  const lowFirst = r() < lane.lowShare;
  const register = pickWhere(lanePool(L.register), r, (x) => (lowFirst ? !/^mid register/.test(x) : true));
  const low = !/^mid register|low-mid, stepping/.test(register);
  const motif = pickBudget(lanePool(L.motif), r, allow, (m) => !(lane.majorShare < 0.2 && /major hook|grins/.test(m)));
  const long = /five|six|seven|run/.test(motif);
  const short = /two-note|one repeated|stubborn root/.test(motif);
  const articulation = pickBudget(lanePool(L.articulation), r, allow, (a) => !(long && /one held|one anticipated|a push note|two stabs/.test(a)) && !(short && /legato runs/.test(a)));
  const legato = /legato|long tones|held note/.test(articulation);
  const placement = pick(lanePool(L.placement), r);
  const dry = /dry|narrow|mono/.test(placement);
  const character = pickWhere(lanePool(F.character), r, (c) => !(dry && /slapback|wide/.test(c)) && !(legato && /gated/.test(c)) && !(/detuned/.test(source) && /detuned|ring-mod/.test(c)));
  const motion = pickBudget(lanePool(L.motion), r, allow, (m) => !(dry && /detune widens/.test(m)) && !(legato && /shorter/.test(m)));
  const hook = pickBudget(lanePool(HOOK_DEVICES), r, allow);
  return { family, guest, handle, frame: F.frame, source, character, register, articulation, placement, motion, motif, hook, human: pick(lanePool(L.human), r), low, dry, triplet: /triplet|three-against/.test(articulation), is808: /^an 808/.test(source) };
}

function buildCounter(r, lane, lead, allow, mode) {
  const own = Object.entries(lane.families).filter(([f]) => (lead.guest || f !== lead.family) && (lane.FAMILIES[f] || G.FAMILIES[f]));
  const family = own.length ? weighted(own, r) : pick(Object.keys(G.FAMILIES).filter((f) => f !== lead.family), r);
  const F = lane.FAMILIES[family] || G.FAMILIES[family];
  const [source, handle] = split(pickWhere(lanePool(F.sources), r, (x) => !/808/.test(x) && x !== lead.source + '|' + lead.handle));
  const role = pickBudget(lanePool(G.COUNTER_ROLE), r, allow, (x) => !(mode !== 'switch' && /after the switch/.test(x)));
  return { family, source, handle, character: pick(lanePool(F.character), r), role };
}

/** One strange ingredient per roll, about half the time: a fuzz lament, a machine glitch, a nervous horn twitch, or a piano/string hit structure. */
function buildHint(r, lane, lead, allow, mode, forced) {
  if (forced === 'none') return null;
  if (!forced && !(r() < (lane.hintShare == null ? 0.55 : lane.hintShare))) return null;
  const weights = (lane.hintWeights || HINT_WEIGHTS).filter(([f]) => HINT_FAMILIES[f] && (!forced || f === forced) && !(f === 'fuzzlament' && lead.family === 'guitar') && !(f === 'hits' && /piano/.test(lead.source)));
  const family = weights.length ? weighted(weights, r) : (forced && HINT_FAMILIES[forced] ? forced : 'hits');
  const F = HINT_FAMILIES[family];
  const [source, handle] = split(pick(lanePool(F.sources), r));
  const role = pickBudget(lanePool(F.roles), r, allow, (x) => !(mode !== 'switch' && /switch/.test(x)));
  return { family, source, handle, character: pick(lanePool(F.character), r), role, tagWord: F.tagWord };
}

function buildBass(r, lane, lead, allow) {
  const source = lead.is808 ? pick(['a clean sine sub', 'a sub that only sustains on the root'], r) : pick(lane.bass, r);
  const pattern = pickBudget(lanePool(lane.bassPattern || G.BASS.pattern), r, allow, (p) => !(lead.low && /motif|moving when/.test(p)) && !(lead.triplet && /triplet/.test(p)));
  const tone = pickWhere(lanePool(G.BASS.tone), r, (t) => !(/bass guitar/.test(source) && /glide|gated|low-pass|pitch fall/.test(t)));
  return { source, tone, pattern, human: pick(lanePool(G.BASS.human), r) };
}

function buildDrums(r, lane, lead, allow, mode) {
  const D = lane.drums;
  const hats = pickBudget(D.hats, r, allow, (h) => !(lead.triplet && /triplets|threes/.test(h)));
  const twitch = {
    sound: pick(lanePool(G.TWITCH.sound), r),
    rhythm: pickBudget(lanePool(G.TWITCH.rhythm), r, allow, (x) => !(lead.triplet && /threes|triple|triplets/.test(x))),
    where: pickBudget(lanePool(G.TWITCH.where), r, allow, (w) => !(mode !== 'switch' && /after the switch/.test(w))),
  };
  const slur = pickWhere(lanePool(G.GROOVE.triplet), r, (t) => !(/triplets|threes/.test(hats) && /hats/.test(t)));
  return {
    color: pickBudget(lanePool(lane.color || G.COLOR), r, allow),
    kick: pickBudget(D.kick, r, allow), snare: pickBudget(D.snare, r, allow), hats, twitch,
    sync: pickBudget(lanePool(G.GROOVE.sync), r, allow), feel: pick(lanePool(G.GROOVE.human), r), slur,
  };
}

function buildSwitch(r, lane, drums) {
  const axis = pickWhere(lanePool(lane.switchAxes || G.SWITCH.axes), r, (a) => !(/half-time/.test(a.tag) && /half-time/.test(drums.snare)) && !(/strips/.test(a.cue) && /after the switch/.test(drums.twitch.where)));
  const sparse = /strips/.test(axis.cue);
  const land = pickWhere(lanePool(G.SWITCH.land), r, (l) => !(sparse && /everything slams/.test(l)));
  return { ...axis, land };
}

function buildRhythm(r, lane, mode, lead, sw) {
  const R = G.RHYTHM;
  return {
    sync: pick(lanePool(R.sync), r), twitch: pick(lanePool(R.twitch), r),
    slur: r() < lane.slurShare ? pick(lanePool(R.slur), r) : null,
    mode: mode === 'switch' ? sw.tag : mode === 'loop' ? pick(lanePool(R.loop), r) : pick(lanePool(R.straight), r),
    low: lead.low ? pick(lanePool(R.low), r) : null,
  };
}

/** The first clause of the processing phrase, short enough for the head. */
const timbre = (character) => {
  const first = character.split(',')[0].trim();
  if (first.split(/\s+/).length > 6) return '';
  const words = first.split(/\s+/).slice(0, 6);
  while (words.length > 2 && /^(the|a|an|on|of|with|and|in|to|at|by|so|like|its|for|into|over|under|every|each|until|then|all|that|this|more|too|very|never)$/i.test(words[words.length - 1])) words.pop();
  return words.join(' ');
};

function styleField(p, lane, opts = {}) {
  const { lead, bass, drums, rhythm } = p;
  const anchor = opts.anchor || p.anchor;
  const H = p.hint && !opts.plain && !p.bare ? p.hint : null;
  const timbreClause = timbre(lead.character);
  const headOf = (withTimbre) => [
    `${anchor}, ${p.bpm} BPM${p.halfTime ? ', half-time' : ''}, in ${p.key}`, 'instrumental',
    ...(p.moods || []),
    opts.plain ? null : rhythm.sync, opts.plain ? null : rhythm.twitch, opts.plain ? null : rhythm.slur,
    p.bare ? 'bare minimum, nothing else' : p.layout && p.layout.minimal >= 0.7 ? 'minimal' : null, rhythm.mode, p.antiTag, ...(p.extraTags || []), opts.wildTag || null, rhythm.low,
    withTimbre ? `${lead.handle} ${timbreClause}`.trim() : lead.handle, H ? `a ${H.tagWord} hint of ${H.handle}` : null, opts.mixTag || p.mixTag,
  ].filter(Boolean).join(', ') + '.';
  const figured = /\b(\w+-note|\w+-chord|riff|loop|hook|stab|figure|melody|two notes|three notes|four notes|two chords)\b/.test(lead.source);
  const C = p.counter && p.mode !== 'loop' && !p.bare ? p.counter : null;
  const laneColor = !!lane.color; // a lane with its own percussion colour (the phonk cowbell) keeps it late in the cut order

  // Optional parts with a drop rank (lower drops first). Required parts have rank Infinity.
  const items = [
    { g: 'lead', k: 'source', t: lead.source, r: Infinity },
    { g: 'lead', k: 'character', t: lead.character, r: Infinity },
    { g: 'lead', k: 'register', t: lead.register, r: rhythm.low ? 11 : 17 },
    { g: 'lead', k: 'motif', t: lead.motif, r: 19, skip: figured },
    { g: 'lead', k: 'articulation', t: lead.articulation, r: 15 },
    { g: 'lead', k: 'hook', t: lead.hook, r: 13 },
    { g: 'lead', k: 'human', t: lead.human, r: 9 },
    { g: 'lead', k: 'placement', t: lead.placement, r: 5 },
    { g: 'bass', k: 'source', t: bass.source, r: Infinity },
    { g: 'bass', k: 'tone', t: bass.tone, r: 4 },
    { g: 'bass', k: 'pattern', t: bass.pattern, r: 10 },
    { g: 'bass', k: 'human', t: bass.human, r: 2 },
    { g: 'drums', k: 'kick', t: drums.kick, r: Infinity },
    { g: 'drums', k: 'snare', t: drums.snare, r: Infinity },
    { g: 'drums', k: 'hats', t: drums.hats, r: Infinity },
    { g: 'drums', k: 'color', t: drums.color, r: laneColor ? 18 : 8, skip: p.bare },
    { g: 'drums', k: 'twitch', t: `${drums.twitch.sound} ${drums.twitch.rhythm}, ${drums.twitch.where}`, r: 12.5 },
    { g: 'drums', k: 'slur', t: drums.slur, r: 14, skip: !rhythm.slur },
    { g: 'drums', k: 'anti', t: p.anti, r: Infinity },
    ...(p.bare ? [{ g: 'drums', k: 'bare', t: 'nothing else in the drums, silence between the hits', r: Infinity }] : []),
    ...(H ? [
      { g: 'hint', k: 'source', t: H.source, r: Infinity },
      { g: 'hint', k: 'character', t: H.character, r: 7 },
      { g: 'hint', k: 'role', t: H.role, r: 16 },
    ] : []),
    ...(C ? [
      { g: 'counter', k: 'source', t: C.source, r: 6 },
      { g: 'counter', k: 'character', t: C.character, r: 1 },
      { g: 'counter', k: 'role', t: C.role, r: 3 },
    ] : []),
  ].filter((x) => !x.skip && x.t);

  const render = (withTimbre) => {
    const lines = [headOf(withTimbre)];
    for (const g of ['lead', 'bass', 'drums', 'hint', 'counter']) {
      const parts = items.filter((x) => x.g === g && !x.dropped);
      if (!parts.length || (g === 'counter' && !parts.some((x) => x.k === 'source'))) continue;
      const texts = [];
      for (const x of parts) {
        if (x.k === 'motif') continue;
        if (x.k === 'articulation') {
          const motif = parts.find((y) => y.k === 'motif');
          texts.push(figured || !motif ? x.t : `playing ${motif.t} as ${x.t}`);
          continue;
        }
        texts.push(x.t);
      }
      const motifOnly = parts.find((y) => y.k === 'motif') && !parts.find((y) => y.k === 'articulation');
      if (motifOnly) texts.splice(2, 0, `playing ${parts.find((y) => y.k === 'motif').t}`);
      lines.push(`${g}: ${texts.join(', ')}.`);
    }
    return lines.join(' ');
  };
  const limit = SPEC.fields.style.limit;
  let withTimbre = true;
  let text = render(withTimbre);
  let guard = 0;
  while (text.length > limit && guard++ < 40) {
    const next = items.filter((x) => !x.dropped && x.r !== Infinity).sort((a, b) => a.r - b.r)[0];
    if (next && next.r <= 12) { next.dropped = true; }
    else if (withTimbre) { withTimbre = false; }
    else if (next) { next.dropped = true; }
    else break;
    // a counter that lost its source loses its other parts too
    if (next && next.g === 'counter' && next.k === 'source') items.filter((x) => x.g === 'counter').forEach((x) => { x.dropped = true; });
    text = render(withTimbre);
  }
  // Restore pass: a dropped item that fits again (highest rank first) comes back, so the field is as full as the limit allows.
  for (const x of items.filter((y) => y.dropped && !(y.g === 'counter' && items.some((c) => c.g === 'counter' && c.k === 'source' && c.dropped))).sort((a, b) => b.r - a.r)) {
    x.dropped = false;
    const t = render(withTimbre);
    if (t.length <= limit) text = t; else x.dropped = true;
  }
  return { text: text.length > limit ? text.slice(0, limit) : text, drumsKept: text.includes('drums:'), hintKept: !H || text.includes('hint:') };
}

function lyricsField(p) {
  const { lead, drums, sw, bass, counter, rhythm, layout } = p;
  const vals = {
    LEAD: lead.handle, COUNTER: counter ? counter.handle : null, BASS: /808|sub/.test(bass.source) && !/bass guitar/.test(bass.source) ? '808' : 'bass',
    KICK: drumWord(drums.kick), HATS: drumWord(drums.hats), SNARE: drumWord(drums.snare),
    SYNC: rhythm.sync, SLUR: rhythm.slur ? drums.slur : null, FEEL: drums.feel, MOTION: lead.motion,
    CUE: sw.cue, AFTER: sw.after, LAND: sw.land, TWITCH: `${drums.twitch.sound} ${drums.twitch.rhythm}, ${drums.twitch.where}`, ANTI: p.anti,
    HINTCUE: p.hint ? `${p.hint.handle} ${p.hint.role}` : null,
  };
  let out = renderLayout(layout, vals, { introBars: p.introBars });
  if (p.mode === 'loop') out = out.replace(/\[Outro \| ([^\]]*)\]/, (m, body) => `[Outro | ${body}, about ${fmtTime(p.plan.totalSeconds)}]`);
  return out;
}

/** A title built from the roll's own sounds, salted so the page can ask for another one when it has seen it. */
const STOP = new Set(['the', 'a', 'an', 'of', 'and', 'with', 'on', 'in']);
const cap = (w) => w.charAt(0).toUpperCase() + w.slice(1);
function makeTitle(seed, parts, salt = 0) {
  const r = mulberry32((seed ^ 0x9e3779b9) + salt * 7919 >>> 0);
  // A handle becomes its last two words, capitalised: "the murky keys" -> "Murky Keys", "the boing lead" -> "Boing Lead".
  const handleTitle = (t) => (t || '').replace(/[^a-z0-9\- ]/gi, ' ').split(/\s+/).filter((w) => w && !STOP.has(w.toLowerCase())).slice(-2).map(cap).join(' ');
  const handleWords = [parts.lead.handle, parts.hint ? parts.hint.handle : null, parts.counter ? parts.counter.handle : null].filter(Boolean).map(handleTitle).filter(Boolean);
  const tonic = parts.harmony.key.split(' ')[0];
  const keyWord = /[#b]$/.test(tonic) ? cap(tonic.replace('#', ' Sharp').replace(/b$/, ' Flat')) : null;
  const H = () => handleWords.length ? pick(handleWords, r) : pick(G.TITLE.b, r);
  const A = () => pick(G.TITLE.a, r);
  const B = () => pick(G.TITLE.b, r);
  let shape = pick(G.TITLE.shapes, r);
  if (shape === 'KB' && !keyWord) shape = 'HB';
  const out = { AB: () => `${A()} ${B()}`, HB: () => `${H()} ${B()}`, AH: () => `${A()} ${H()}`, HH: () => { const a = H(), b = H(); return a === b ? `${a} ${B()}` : `${a} ${b}`; }, ABB: () => `${A()} ${B()} ${B()}`, KB: () => `${keyWord} ${B()}` }[shape]();
  return out.replace(/\b(\w+) \1\b/i, '$1').replace(/\s+/g, ' ').trim().slice(0, SPEC.fields.title.limit);
}

const WILD_TAGS = ['drums slowed and slammed', 'bit-mangled lead', '808 blown out and clipped', 'pitch-warped lead', 'drums pitched down a step', 'sliced sample stabs off the grid', 'hats crushed to grit', 'the loop resampled until it smears'];

function splitList(s) { return String(s || '').split(/[,;]+/).map((x) => x.trim()).filter(Boolean); }

/**
 * Generate a block. input: { lane:'any'|id, seed, bpm, key, mood, mode, layout, constraints, exclude,
 * weirdness, styleInfluence, title, wild, avoidKeys }
 */
function generate(input = {}) {
  const seedIn = input.seed === undefined || input.seed === null || input.seed === '' ? Math.floor(Math.random() * 4294967295) : input.seed;
  const seed = typeof seedIn === 'number' ? seedIn >>> 0 : hashStr(seedIn);
  const r = mulberry32(seed);
  const laneId = !input.lane || input.lane === 'any' ? weighted(LANE_WEIGHTS, r) : input.lane;
  const lane = LANES[laneId];
  if (!lane) throw new Error(`Unknown lane "${input.lane}". Available: any, ${Object.keys(LANES).join(', ')}`);
  laneOk = (str) => Guard.check(str, lane.allow, lane.deny).length === 0;
  const warnings = [];

  const harmony = T.harmony(r, pick, { avoidKeys: input.avoidKeys || [], majorShare: lane.majorShare, soulShare: lane.soulShare, forceKey: input.key || null });
  const [lo, hi] = pick(lane.tempo, r);
  const bpm = input.bpm ? Number(input.bpm) : int(lo, hi, r);
  if (!(bpm >= 60 && bpm <= 200)) throw new Error('BPM must be between 60 and 200');
  const halfTime = bpm >= lane.halfTimeAbove;

  let allowedLayouts = LAYOUTS.filter((l) => lane.layoutWeights[l.id]);
  if (input.minimal) { const bareOnes = allowedLayouts.filter((l) => l.minimal >= 0.9); if (bareOnes.length) allowedLayouts = bareOnes; }
  if (input.layout) { const L = LAYOUTS.find((l) => l.id === input.layout); if (!L) throw new Error(`Unknown layout "${input.layout}". Available: ${LAYOUTS.map((l) => l.id).join(', ')}`); allowedLayouts = [L]; }
  else if (input.mode) { const byMode = allowedLayouts.filter((l) => l.mode === input.mode); if (!byMode.length) throw new Error(`No layout in this lane for mode "${input.mode}" (loop | straight | switch)`); allowedLayouts = byMode; }
  const layout = weighted(allowedLayouts.map((l) => [l, lane.layoutWeights[l.id] || l.weight || 1]), r);
  const mode = layout.mode;
  const allow = makeAllow(mode);

  const lead = buildLead(r, lane, allow);
  const counter = mode === 'loop' ? null : buildCounter(r, lane, lead, allow, mode);
  const bare = !!(layout.bare || input.minimal);
  const hint = bare ? null : buildHint(r, lane, lead, allow, mode, input.hint);
  const antiTag = pick(lanePool(G.ANTI_TAGS), r);
  const bass = buildBass(r, lane, lead, allow);
  const drums = buildDrums(r, lane, lead, allow, mode);
  const sw = buildSwitch(r, lane, drums);
  const rhythm = buildRhythm(r, lane, mode, lead, sw);
  const anti = pick(lanePool(G.ANTI_TRANSITION), r);
  const inLane = lead.family === lane.mainFamily || lane.families[lead.family];
  const anchor = inLane && !lead.guest ? pick(lane.anchors, r) : lane.neutralAnchor;
  const mixTag = inLane && !lead.guest ? lane.mixTag : lane.neutralMixTag;

  // User words go through the guard; anything that trips it is dropped and reported.
  const guardWords = (list, label) => list.filter((w) => { const h = Guard.check(w, lane.allow, lane.deny); if (h.length) { warnings.push(`${label} "${w}" dropped: ${h[0].rule} ("${h[0].word}") ${h[0].why}`); return false; } return true; });
  const moods = guardWords(splitList(input.mood), 'mood word');
  const extraTags = guardWords(splitList(input.constraints), 'must-have');

  // Intro length and the radio window: the first full beat lands by 0:45.
  let introBars = pick([2, 4, 4, 8], r);
  let est = estimate(layout, bpm, halfTime, introBars);
  if (est.hookAtSeconds > RADIO.hookByMax && introBars > 2) { introBars = 2; est = estimate(layout, bpm, halfTime, introBars); }
  const plan = { totalSeconds: est.totalSeconds, hookAtSeconds: est.hookAtSeconds };

  const p = { anchor, bpm, halfTime, key: harmony.key, mode, layout, moods, extraTags, mixTag, anti, antiTag, bare, introBars, plan, lead, counter, hint, bass, drums, sw, rhythm };
  const main = styleField(p, lane);
  const style = main.text;
  const lyrics = lyricsField(p);
  if (!main.drumsKept) warnings.push('the drums line did not fit the Style field; the beat may come back generic');
  if (hint && !style.includes('hint:')) warnings.push('the hint line did not fit the Style field');

  const exclude = [...new Set([...lane.exclude, ...splitList(input.exclude)])].join(', ');
  const S = lane.sliders;
  const weirdness = input.weirdness != null ? Number(input.weirdness) : int(S.weirdness[0], S.weirdness[1], r);
  const styleInfluence = input.styleInfluence != null ? Number(input.styleInfluence) : int(S.styleInfluence[0], S.styleInfluence[1], r);
  const wildS = lane.wildSliders || { weirdness: [65, 80], styleInfluence: [55, 70] };
  const wildSuggested = !!input.wild || r() < lane.wildShare;
  const modelPick = input.wild ? SPEC.models.wild : SPEC.models.default;
  const title = input.title || makeTitle(seed, { lead, hint, counter, harmony }, input.titleSalt || 0);

  const safer = styleField({ ...p, moods, extraTags }, lane, { anchor: lane.neutralAnchor, plain: true, mixTag: lane.neutralMixTag }).text;
  const weirder = styleField(p, lane, { wildTag: pick(lanePool(WILD_TAGS), r) }).text;

  for (const h of Guard.check(`${style}\n${lyrics}\n${safer}\n${weirder}\n${title}`, lane.allow, lane.deny)) warnings.push(`${h.rule}: "${h.word}" ${h.why}`);

  const durationSec = Math.min(360, Math.max(RADIO.minSeconds, Math.ceil((plan.totalSeconds + SPEC.duration.bufferSeconds) / 5) * 5));
  const totalFormatted = fmtTime(plan.totalSeconds);
  const sections = [];
  { // section table for the renderer
    const bs = 240 / bpm; let t = 0;
    layout.sections.forEach((s, i) => {
      if (/^\[End\]/.test(s)) return;
      const bars = sectionBars(layout, i, halfTime, introBars);
      sections.push({ tag: s.match(/^\[([^|\]]+)/)[1].trim(), bars, start: fmtTime(t) }); t += bars * bs;
    });
  }

  return {
    meta: {
      lane: laneId, laneLabel: lane.label, bpm, halfTime, key: harmony.key, chords: harmony.chords.join(' - '), roman: harmony.roman,
      mode, layout: layout.id, layoutLabel: layout.label, bare, seed, title, wildSuggested, hint: hint ? { family: hint.family, handle: hint.handle } : null, generatedWithSpecVerifiedOn: SPEC.verifiedOn,
    },
    model: {
      pick: modelPick,
      rationale: `${modelPick === 'v6' ? 'v6 follows the written prompt most closely (Weirdness 40-55, Style Influence 75-85 landed nearest the text in listening tests).' : 'v6-wild drifts from the prompt by design; use it when v6 comes back too tidy.'} ${SPEC.models.notes[modelPick]}`,
      wild: `${wildSuggested ? 'This roll is a good candidate for v6-wild: ' : 'Try v6-wild when v6 comes back too tidy or too quantized: '}run the same fields on ${SPEC.models.wild} at Weirdness ${wildS.weirdness[0]}-${wildS.weirdness[1]} and Style Influence ${wildS.styleInfluence[0]}-${wildS.styleInfluence[1]}; that is where the strange, great accidents happen. ${SPEC.models.notes['v6-wild']}`,
      mini: `${SPEC.models.mini}: ${SPEC.models.notes['v6-mini']}`,
    },
    toggles: {
      customMode: true, modeName: SPEC.mode.name, instrumental: true, persona: false,
      personaNote: `${SPEC.personas.uiName}: ${SPEC.personas.note}`,
      maxMode: true, maxModeNote: `${SPEC.maxMode.recommended} (${SPEC.maxMode.credits}). Max Mode also keeps silences silent on minimal rolls.`,
      variety: 'Off', varietyNote: SPEC.sliders.variety.why,
      duration: `Custom ${fmtTime(durationSec)} (estimated ${totalFormatted} from the layout at ${bpm} BPM, plus a buffer rounded up to the next 5 s; [End] stops it early, Crop any tail). Never Auto: v6 runs long on Auto.`,
      title,
    },
    style, lyrics, exclude,
    sliders: {
      weirdness, weirdnessWhy: `${lane.label}: the lane's tested range is ${S.weirdness[0]}-${S.weirdness[1]}; above 60 Suno starts drifting tempo and structure.`,
      styleInfluence, styleInfluenceWhy: `High so the physical descriptions are followed; ${S.styleInfluence[0]}-${S.styleInfluence[1]} landed closest to the written prompt.`,
      audioInfluence: 'Not used. No audio is uploaded in this workflow, so the slider does not appear; leave it alone.',
      variety: `Off (Exact style). ${SPEC.sliders.variety.why}`,
    },
    variations: {
      safer: { style: safer, sliders: { weirdness: Math.max(0, weirdness - 10), styleInfluence: Math.min(100, styleInfluence + 5) }, model: SPEC.models.default, note: 'Sound-neutral anchor, no twitch or slur tags, no hint: the plainest reading of the same sounds.' },
      weirder: { style: weirder, sliders: { weirdness: int(wildS.weirdness[0], wildS.weirdness[1], r), styleInfluence: int(wildS.styleInfluence[0], wildS.styleInfluence[1], r) }, model: SPEC.models.wild, note: 'One wild tag added; run it on v6-wild.' },
    },
    postGen: postGen(lane, p, totalFormatted),
    plan: { totalSeconds: Math.round(plan.totalSeconds), totalFormatted, hookAt: fmtTime(plan.hookAtSeconds), introBars, sections, durationSetting: fmtTime(durationSec) },
    limits: { style: SPEC.fields.style.limit, lyrics: SPEC.fields.lyrics.limit, exclude: SPEC.fields.exclude.limit },
    parts: { lead, counter, hint, bass, drums, switch: sw, rhythm, anti, antiTag, bare, harmony },
    fingerprint: [laneId, lead.source, lead.character, lead.motif, lead.articulation, lead.hook, lead.human, mode === 'switch' ? sw.tag : mode, harmony.key, counter ? counter.source : 'loop', hint ? hint.source : 'nohint'].join('|'),
    warnings,
  };
}

function postGen(lane, p, totalFormatted) {
  const f = SPEC.features;
  return [
    ...(p.bare ? ['Bare mode: the lead, one kick, one bass and silence; no counter, no hint, no percussion colour. If Suno adds anything, raise Style Influence five points and keep "bare minimum, nothing else" in the head.'] : []),
    `Structure: the first full beat lands at about ${fmtTime(p.plan.hookAtSeconds)}; the layout is "${p.layout.label}" and runs about ${totalFormatted}. ${p.mode === 'switch' ? `The beat switch is one axis ("${p.sw.tag}"); its words repeat in the head tag and in every section after the switch so Suno cannot undo it.` : p.mode === 'loop' ? 'Loop mode: every [Loop] tag says identical on purpose; if Suno adds changes anyway, raise Style Influence five points.' : 'Straight mode: the same drums in every section, hard cuts only.'}`,
    ...(p.hint ? [`The hint (${p.hint.handle}) is one ingredient, ${p.hint.role}. If it takes over the record, run the safer variation (no hint) or re-roll; if it is inaudible, raise Style Influence five points. The hook device in the lead line ("${p.lead.hook}") is what makes the lead stick; listen for it on the second pass.`] : [`No hint on this roll; the hook device in the lead line ("${p.lead.hook}") carries the stickiness. Listen for it on the second pass.`]),
    ...lane.listenFor,
    'Fills, risers or a roll at a section change mean a word slipped through: run the QA checker on the block, and keep the anti-transition phrase in the drums line (it is the positive form of "no fills").',
    'Any hum, chant or sung syllable means a re-roll, not a fix: the Exclude list is already complete, so change the seed rather than adding words.',
    `Ending: [End] should stop it near ${totalFormatted}. If it runs on, Crop to the last downbeat. If it stops early, Extend from the last good section and paste only the remaining section tags.`,
    'Extend when the first 60-90 s are keepers and only the back half drifts. Re-roll (or run the weirder variation on v6-wild) when the drums or the lead are wrong from the start.',
    `Remaster: ${f.remaster}`,
    `Stems: ${f.stems}`,
    'Prompt ignored? Check Variety is Off (v6 defaults it to Normal, which rewrites the Style text) and Max Mode is on. Then check Style Influence is 70 or higher.',
  ];
}

module.exports = { generate, makeTitle, LANES, LANE_WEIGHTS, LAYOUTS, SPEC, RADIO, splitList, styleField };
