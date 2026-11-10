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
const { HINT_FAMILIES, HINT_WEIGHTS, SPRINKLE_WEIGHTS, HOOK_DEVICES } = require('./hints');
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
let attitudeOk = () => true; // strut strips sad, dragging, mournful phrases from every pool
let contextOk = () => true; // a loop layout has no hook and no verse, so no pool phrase may name one
const poolText = (x) => (typeof x === 'string' ? x : `${x.tag} ${x.cue} ${x.after}`);
const lanePool = (arr) => {
  const a = arr.filter((x) => laneOk(poolText(x)));
  const b = a.filter((x) => attitudeOk(poolText(x)));
  const c = b.filter((x) => contextOk(poolText(x)));
  return c.length ? c : b.length ? b : a.length ? a : arr;
};
const int = (lo, hi, r) => lo + Math.floor(r() * (hi - lo + 1));
const weighted = (entries, r) => { const total = entries.reduce((n, [, w]) => n + w, 0); let x = r() * total; for (const [v, w] of entries) if ((x -= w) < 0) return v; return entries[entries.length - 1][0]; };
const split = (entry) => entry.split('|');
const bare = (s) => s.replace(/^an? /, '');
/** Short drum word for a lyrics tag: the adjectives up to the first drum noun ("hand-played hats", "big layered clap"). */
const DRUM_NOUN = /\b(kicks?|snares?|claps?|snaps?|hats?|shaker|ride|cowbell|rimshot|cross-stick|tambourine)\b/i;
function drumWord(source) {
  const first = bare(source).split(',')[0];
  const words = first.split(/\s+/);
  const idx = words.findIndex((w) => DRUM_NOUN.test(w));
  const cut = idx >= 0 ? words.slice(Math.max(0, idx - 3), idx + 1) : words.slice(0, 3);
  return cut.join(' ').replace(/^(and|of|with|the|a|an)\s+/i, '').replace(/\bhat$/, 'hats');
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

let currentAttitude = 'low';
function buildLead(r, lane, allow) {
  const L = G.LEAD;
  const guest = lane.guestShare > 0 && r() < lane.guestShare;
  const family = guest ? weighted(G.GUEST_FAMILIES, r) : weighted(Object.entries(lane.families).filter(([f]) => lane.FAMILIES[f] || G.FAMILIES[f]), r);
  const F = lane.FAMILIES[family] || G.FAMILIES[family];
  const [source, handle] = split(pick(lanePool(F.sources), r));
  const lowFirst = r() < lane.lowShare;
  const register = pickWhere(lanePool(L.register), r, (x) => (lowFirst ? !/^mid register/.test(x) : true));
  const low = !/^mid register|low-mid, stepping/.test(register);
  const A = G.ATTITUDE[currentAttitude] || {};
  const motifPool = A.motif && A.motif.length && r() < 0.6 ? lanePool(A.motif) : lanePool(L.motif);
  const motif = pickBudget(motifPool, r, allow, (m) => !(lane.majorShare < 0.2 && currentAttitude !== 'strut' && /major hook|grins/.test(m)));
  const long = /five|six|seven|run/.test(motif);
  const short = /two-note|one repeated|stubborn root/.test(motif);
  const articulation = pickBudget(lanePool(L.articulation), r, allow, (a) => !(long && /one held|one anticipated|a push note|two stabs/.test(a)) && !(short && /legato runs/.test(a)));
  const legato = /legato|long tones|held note/.test(articulation);
  const placement = pick(lanePool(L.placement), r);
  const dry = /dry|narrow|mono/.test(placement);
  const character = pickWhere(lanePool(F.character), r, (c) => !(dry && /slapback|wide/.test(c)) && !(legato && /gated/.test(c)) && !(/detuned/.test(source) && /detuned|ring-mod/.test(c)));
  const motion = pickBudget(lanePool(L.motion), r, allow, (m) => !(dry && /detune widens/.test(m)) && !(legato && /shorter/.test(m)));
  const [hook, hookTag] = split(pickBudget(lanePool(HOOK_DEVICES), r, allow));
  const human = A.human && A.human.length && r() < 0.6 ? pick(lanePool(A.human), r) : pick(lanePool(L.human), r);
  return { family, guest, handle, frame: F.frame, source, character, register, articulation, placement, motion, motif, hook, hookTag, human, low, dry, triplet: /triplet|three-against/.test(articulation), is808: /^an 808/.test(source) };
}

function buildCounter(r, lane, lead, allow, mode) {
  const own = Object.entries(lane.families).filter(([f]) => (lead.guest || f !== lead.family) && (lane.FAMILIES[f] || G.FAMILIES[f]));
  const family = own.length ? weighted(own, r) : pick(Object.keys(G.FAMILIES).filter((f) => f !== lead.family), r);
  const F = lane.FAMILIES[family] || G.FAMILIES[family];
  const [source, handle] = split(pickWhere(lanePool(F.sources), r, (x) => !/808/.test(x) && x !== lead.source + '|' + lead.handle));
  const role = pickBudget(lanePool(G.COUNTER_ROLE), r, allow, (x) => !(mode !== 'switch' && /after the switch/.test(x)));
  return { family, source, handle, character: pick(lanePool(F.character), r), role };
}

/** Where an ingredient sits when the layout is one loop (no hook, no verse to name). */
const LOOP_ROLES = ['once, near the end, then gone', 'twice in the whole song, far apart', 'only in the last section', 'once, about halfway through, then gone', 'in the gaps the lead leaves', 'barely there, the whole way'];

/** One strange ingredient per roll, about half the time: a fuzz lament, a machine glitch, a nervous horn twitch, or a piano/string hit structure. */
function buildHint(r, lane, lead, allow, mode, forced) {
  if (forced === 'none') return null;
  if (!forced && !(r() < (lane.hintShare == null ? 0.55 : lane.hintShare))) return null;
  let weights = (lane.hintWeights || HINT_WEIGHTS).filter(([f]) => HINT_FAMILIES[f] && (!forced || f === forced) && !(f === 'fuzzlament' && lead.family === 'guitar') && !(f === 'hits' && /piano/.test(lead.source)));
  if (currentAttitude === 'strut' && !forced) weights = weights.filter(([f]) => f !== 'fuzzlament').map(([f, w]) => [f, /staxtwitch|lowbrass|hits/.test(f) ? w * 2 : w]);
  const family = weights.length ? weighted(weights, r) : (forced && HINT_FAMILIES[forced] ? forced : 'hits');
  const F = HINT_FAMILIES[family];
  const [source, handle] = split(pick(lanePool(F.sources), r));
  let role = pickBudget(lanePool(F.roles), r, allow, (x) => !(mode !== 'switch' && /switch/.test(x)));
  if (!contextOk(role)) role = pick(LOOP_ROLES, r);
  return { family, source, handle, character: pick(lanePool(F.character), r), role, tagWord: F.tagWord };
}

/** The mystery ingredient: a second, sprinkled hint that appears once or twice and sends the track over the edge. */
function buildSprinkle(r, lane, hint, allow, mode, share = 0.35) {
  if (!(r() < share)) return null;
  const weights = SPRINKLE_WEIGHTS.filter(([f]) => !hint || f !== hint.family);
  const family = weighted(weights, r);
  const F = HINT_FAMILIES[family];
  const [source, handle] = split(pick(lanePool(F.sources), r));
  let role = pickBudget(lanePool(F.roles), r, allow, (x) => !(mode !== 'switch' && /switch/.test(x)));
  if (!contextOk(role)) role = pick(LOOP_ROLES, r);
  return { family, source, handle, role, tagWord: F.tagWord };
}

function buildBass(r, lane, lead, allow) {
  const A = G.ATTITUDE[currentAttitude] || {};
  const source = lead.is808 ? pick(['a clean sine sub', 'a sub that only sustains on the root'], r) : (A.bass && A.bass.length && r() < 0.4 && !/paak|hybrid/.test(lane.id) ? pick(lanePool(A.bass), r) : pick(lanePool(lane.bass), r));
  const pattern = pickBudget(lanePool(lane.bassPattern || G.BASS.pattern), r, allow, (p) => !(lead.low && /motif|moving when/.test(p)) && !(lead.triplet && /triplet/.test(p)));
  const tone = pickWhere(lanePool(G.BASS.tone), r, (t) => !(/bass guitar/.test(source) && /glide|gated|low-pass|pitch fall/.test(t)));
  return { source, tone, pattern, human: pick(lanePool(G.BASS.human), r) };
}

function buildDrums(r, lane, lead, allow, mode) {
  const A = G.ATTITUDE[currentAttitude] || {};
  const O = A.drums || {};
  const D = { kick: O.kick && r() < 0.5 ? lanePool(O.kick) : lanePool(lane.drums.kick), snare: O.snare && r() < 0.5 ? lanePool(O.snare) : lanePool(lane.drums.snare), hats: O.hats && r() < 0.35 ? lanePool(O.hats) : lanePool(lane.drums.hats) };
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

/**
 * What an opening promises, the roll must carry in a slot that is never cut (lead source, bass source,
 * kick, snare, plus hats and the lane's percussion colour outside bare mode). [anchor words, scope, proof].
 */
const PROMISES = [
  [/\b(sped-up|chipmunk|pitched-up|chirp\w*)\b/i, 'lead', /sped up|pitched up|chirp|squeak|chipmunk/i],
  [/\b(horns?|brass)\b/i, 'lead', /horn|trumpet|trombone|brass/i],
  [/\bpiano\b/i, 'lead', /piano/i],
  [/\borgan\b/i, 'lead', /organ/i],
  [/\bbells?\b/i, 'lead', /bell/i],
  [/\bstrings?\b/i, 'lead', /string/i],
  [/\bkeys\b/i, 'lead', /keys|piano|organ/i],
  [/(?<!bass )\bguitar\b/i, 'lead', /guitar/i],
  [/\bbass guitar\b/i, 'bass', /bass guitar/i],
  [/\brubber-bass\b/i, 'bass', /rubber/i],
  [/\bplucks?\b/i, 'lead', /pluck|knock|mallet|stab|harmonic|click|keys/i],
  [/\bknock melody\b/i, 'lead', /knock|mallet|wood/i],
  [/\bwood\b/i, 'all', /wood/i],
  [/\b(record|chops?|sample\w*)\b/i, 'lead', /record|sample|soul|loop/i],
  [/\bcowbell\b/i, 'drums', /cowbell/i],
  [/\bsnaps?\b/i, 'drums', /snap/i],
  [/\bclap\b/i, 'drums', /clap/i],
  [/\bhats\b/i, 'drums', /hat/i],
  [/\b808\b/i, 'low', /808|sub/i],
];
function anchorFits(text, p, lane) {
  const drums = [p.drums.kick, p.drums.snare, p.bare ? '' : p.drums.hats, lane.color && !p.bare ? p.drums.color : ''].join(' ');
  const scope = { lead: p.lead.source, bass: p.bass.source, drums, low: `${p.bass.source} ${drums}`, all: `${p.lead.source} ${p.bass.source} ${drums}` };
  return PROMISES.every(([word, where, proof]) => !word.test(text) || proof.test(scope[where]));
}

/**
 * The Style field. Head tags first (Suno weights the front and reads it as tags): the opening, tempo and key,
 * "instrumental", the attitude, the lead handle and its hook tag, then rhythm tags, the hint, the mode and the mix.
 * Then prose lines in the order lead, bass, drums, hint, once, counter. Every optional part has a rank; when the
 * field is over the limit the lowest rank goes first (a part with a short form shrinks before it goes), and a
 * restore pass puts back whatever still fits. Ranks are set so the things that make a take one of one (hook device,
 * processing, human timing, twitch, the hint) outlive the things Suno supplies by itself (hat detail, bass tone).
 */
function styleField(p, lane, opts = {}) {
  const { lead, bass, drums, rhythm } = p;
  const anchor = opts.anchor || p.anchor;
  const plain = !!opts.plain;
  const H = p.hint && !plain && !p.bare ? p.hint : null;
  const SP = p.sprinkle && !plain && !p.bare ? p.sprinkle : null;
  let hintOn = !!H, sprinkleOn = !!SP, shortOpening = false, shortMix = false;
  const firstClause = (t) => t.split(',')[0].trim();
  const tempo = `${p.bpm} BPM${p.halfTime ? ', half-time' : ''}, in ${p.key}`;
  const anchored = plain || p.headStyle === 'anchor';
  const opening = () => anchored ? `${shortOpening ? firstClause(anchor) : anchor}, ${tempo}`
    : p.headStyle === 'leadfirst' ? `${firstClause(lead.source)} over ${drumWord(drums.kick)} and ${drumWord(drums.snare)}, ${tempo}, ${firstClause(anchor)}`
    : `${[drumWord(drums.kick), drumWord(drums.snare), p.bare || (p.layout && p.layout.minimal >= 0.9) ? null : drumWord(drums.hats)].filter(Boolean).join(', ')}, ${tempo}, ${firstClause(anchor)}`;
  const strut = p.attitude === 'strut' && !plain;
  const mix = opts.mixTag || (strut ? G.ATTITUDE.strut.mix : p.mixTag);
  const slurTag = plain ? null : rhythm.slur;
  const lowTag = rhythm.low;
  const head = () => [
    opening(), 'instrumental', p.attitudeTag, ...(p.moods || []),
    lead.handle, plain ? null : lead.hookTag,
    plain ? null : rhythm.sync, plain ? null : rhythm.twitch, slurTag,
    p.bare ? 'bare minimum, nothing else' : p.layout && p.layout.minimal >= 0.7 && !/minimal/.test(rhythm.mode || '') ? 'minimal' : null,
    rhythm.mode, p.antiTag,
    hintOn ? `a ${H.tagWord} hint of ${H.handle}` : null, lowTag,
    ...(p.extraTags || []), opts.wildTag || null,
    sprinkleOn ? `${SP.handle} once only` : null,
    shortMix ? firstClause(mix) : mix,
  ].filter(Boolean).join(', ') + '.';
  const figured = /\b(\w+-note|\w+-chord|riff|loop|hook|stab|figure|melody|two notes|three notes|four notes|two chords)\b/.test(lead.source);
  // The counter only speaks when there is no hint: two extra instruments never both fit, and the hint is the stranger one.
  const C = p.counter && p.mode !== 'loop' && !p.bare && !H ? p.counter : null;
  const laneColor = !!lane.color; // a lane with its own percussion colour (the cowbell) keeps it
  const tw = drums.twitch;
  const noHats = p.bare || (p.layout && p.layout.minimal >= 0.9);

  // r = rank (lower goes first, Infinity never goes). alt = short form, taken at rank ar before the part goes at r.
  const items = [
    { g: 'lead', k: 'source', t: lead.source, r: Infinity },
    { g: 'lead', k: 'character', t: lead.character, r: 28 },
    { g: 'lead', k: 'hook', t: lead.hook, r: 21 },
    { g: 'lead', k: 'human', t: lead.human, r: 23 },
    { g: 'lead', k: 'motif', t: `playing ${lead.motif}`, r: 17, skip: figured },
    { g: 'lead', k: 'articulation', t: lead.articulation, r: 9 },
    { g: 'lead', k: 'placement', t: lead.placement, r: 2 },
    { g: 'bass', k: 'source', t: bass.source, r: Infinity },
    { g: 'bass', k: 'pattern', t: bass.pattern, r: 16 },
    { g: 'bass', k: 'tone', t: bass.tone, r: 8 },
    { g: 'bass', k: 'human', t: bass.human, r: 7 },
    { g: 'drums', k: 'kick', t: drums.kick, r: Infinity },
    { g: 'drums', k: 'snare', t: drums.snare, r: Infinity },
    { g: 'drums', k: 'hats', t: drums.hats, alt: /\s/.test(drumWord(drums.hats)) ? drumWord(drums.hats) : null, ar: 15, r: 20, skip: noHats },
    { g: 'drums', k: 'color', t: drums.color, alt: laneColor && drums.color.includes(',') ? firstClause(drums.color) : null, ar: 19, r: laneColor ? 27 : 10, skip: p.bare },
    { g: 'drums', k: 'twitch', t: `${tw.sound} ${tw.rhythm}, ${tw.where}`, alt: `${tw.sound} ${tw.rhythm}`, ar: 18, r: 24 },
    { g: 'drums', k: 'slur', t: drums.slur, r: 14, skip: !rhythm.slur },
    { g: 'drums', k: 'anti', t: p.anti, r: Infinity },
    ...(p.bare ? [{ g: 'drums', k: 'bare', t: 'nothing else in the drums, silence between the hits', r: Infinity }] : []),
    ...(H ? [
      { g: 'hint', k: 'source', t: H.source, r: 25 },
      { g: 'hint', k: 'role', t: H.role, r: 6 },
      { g: 'hint', k: 'character', t: H.character, r: 5 },
    ] : []),
    // head parts that can give way: the anchor shrinks to its first clause; the sprinkle is only a head tag here, its
    // description lives in the one lyrics section where it plays
    ...(anchored && anchor.includes(',') ? [{ g: 'head', k: 'opening', t: anchor, alt: firstClause(anchor), ar: 16.5, r: Infinity }] : []),
    ...(mix.includes(',') ? [{ g: 'head', k: 'mix', t: mix, alt: firstClause(mix), ar: 12, r: Infinity }] : []),
    ...(SP ? [{ g: 'head', k: 'sprinkle', t: SP.handle, r: 25.5 }] : []),
    ...(C ? [
      { g: 'counter', k: 'source', t: C.source, r: 4 },
      { g: 'counter', k: 'role', t: C.role, r: 3 },
      { g: 'counter', k: 'character', t: C.character, r: 1 },
    ] : []),
  ].filter((x) => !x.skip && x.t).map((x) => ({ ...x, level: 0 })); // level 0 full, 1 short form, 2 gone

  const sourceGone = (g) => items.some((x) => x.g === g && x.k === 'source' && x.level === 2);
  const render = () => {
    const lines = [head()];
    for (const g of ['lead', 'bass', 'drums', 'hint', 'counter']) {
      const parts = items.filter((x) => x.g === g && x.level < 2);
      if (!parts.length || sourceGone(g)) continue;
      lines.push(`${g}: ${parts.map((x) => (x.level === 1 ? x.alt : x.t)).join(', ')}.`);
    }
    return lines.join(' ');
  };
  const headItem = (k) => items.find((x) => x.g === 'head' && x.k === k);
  const sync = () => { hintOn = !!H && !sourceGone('hint'); sprinkleOn = !!SP && headItem('sprinkle').level < 2; shortOpening = !!headItem('opening') && headItem('opening').level > 0; shortMix = !!headItem('mix') && headItem('mix').level > 0; };
  const cost = (x) => (x.level === 0 && x.alt ? x.ar : x.r);
  const limit = SPEC.fields.style.limit;
  let text = render();
  let guard = 0;
  while (text.length > limit && guard++ < 64) {
    const next = items.filter((x) => x.level < 2 && cost(x) !== Infinity && !sourceGone(x.g)).sort((a, b) => cost(a) - cost(b))[0];
    if (!next) break;
    next.level = next.level === 0 && next.alt ? 1 : 2;
    sync();
    text = render();
  }
  // Restore pass: whatever fits again comes back, highest rank first, so the field is as full as the limit allows.
  for (const x of items.filter((y) => y.level > 0).sort((a, b) => b.r - a.r)) {
    if (x.k !== 'source' && sourceGone(x.g)) continue;
    const was = x.level;
    for (const level of was === 2 && x.alt ? [0, 1] : [0]) {
      x.level = level; sync();
      const t = render();
      if (t.length <= limit) { text = t; break; }
      x.level = was; sync();
    }
  }
  sync();
  if (text.length > limit) { const cut = text.lastIndexOf(', ', limit - 1); text = (cut > 0 ? text.slice(0, cut) : text.slice(0, limit - 1)).replace(/[,\s]+$/, '') + '.'; }
  // What the field could not hold in full is handed to the lyrics, which have room: nothing rolled is lost.
  const full = (g, k) => { const x = items.find((y) => y.g === g && y.k === k); return !x || x.level === 0; };
  const displaced = { human: !full('lead', 'human'), hook: !full('lead', 'hook'), motif: !figured && !full('lead', 'motif'), hats: !noHats && !full('drums', 'hats'), pattern: !full('bass', 'pattern') };
  return { text, displaced, drumsKept: text.includes('drums:'), hintKept: !H || hintOn, sprinkleKept: !SP || sprinkleOn };
}

function lyricsField(p) {
  const { lead, drums, sw, bass, counter, rhythm, layout } = p;
  const vals = {
    LEAD: lead.handle, COUNTER: counter ? counter.handle : null, BASS: /808|sub/.test(bass.source) && !/bass guitar/.test(bass.source) ? '808' : 'bass',
    KICK: drumWord(drums.kick), HATS: drumWord(drums.hats), SNARE: drumWord(drums.snare),
    SYNC: rhythm.sync, SLUR: rhythm.slur ? drums.slur : null, FEEL: drums.feel, MOTION: lead.motion,
    CUE: sw.cue, AFTER: sw.after, LAND: sw.land, TWITCH: `${drums.twitch.sound} ${drums.twitch.rhythm}, ${drums.twitch.where}`, ANTI: p.anti,
    HINTCUE: p.hint ? `${p.hint.handle} ${p.hint.role}` : null,
    PASS: p.passCues || [],
  };
  let out = renderLayout(layout, vals, { introBars: p.introBars });
  if (p.mode === 'loop') out = out.replace(/\[Outro \| ([^\]]*)\]/, (m, body) => `[Outro | ${body}, about ${fmtTime(p.plan.totalSeconds)}]`);
  // The Style field carries the identity; the lyrics carry the detail where it happens. The hook device is named where
  // the first full beat lands, and whatever the Style field could not hold in full (hats, bass pattern, motif) is
  // written into the first section that plays it.
  const D = p.displaced || {};
  const lines = out.split('\n');
  const add = (i, text) => { if (i >= 0 && text) lines[i] = lines[i].replace(/\]$/, `, ${text}]`); };
  const beat = lines.findIndex((l) => /^\[(Hook|Loop) \|/.test(l));
  add(beat, D.hook ? lead.hook : lead.hookTag);
  // one detail per section, starting after the first full beat; a section that strips the beat down takes none
  const open = lines.map((l, i) => (i > beat && /^\[(Hook|Loop|Verse) \|/.test(l) && !/\b(only|alone|out)\b/.test(l) ? i : -1)).filter((i) => i >= 0);
  const slots = open.length ? open : [beat];
  let n = 0;
  const next = () => slots[n++ % slots.length];
  if (D.motif) { const i = lines.findIndex((l) => /^\[Intro \|/.test(l) && l.includes(lead.handle)); if (i >= 0) add(i, `playing ${lead.motif}`); else add(next(), `${lead.handle} playing ${lead.motif}`); }
  if (D.hats) add(next(), drums.hats);
  if (D.human) add(next(), `${lead.handle}, ${lead.human}`);
  if (D.pattern) add(next(), `the ${vals.BASS}, ${bass.pattern}`);
  out = lines.join('\n');
  // The mystery ingredient is described only here, in the one section where it plays, so it stays a sprinkle.
  if (p.sprinkle) out = placeSprinkle(out, `${p.sprinkle.handle} ${p.sprinkle.role}, ${p.sprinkle.source}`, p.sprinkle.role);
  return out;
}

/** Append the sprinkled ingredient to the section its role names (second verse, last hook, intro...), never more than one section. */
function placeSprinkle(lyrics, text, role) {
  const lines = lyrics.split('\n');
  const idx = (re, which) => { const found = lines.map((l, i) => (re.test(l) ? i : -1)).filter((i) => i >= 0); return found.length ? (which === 'last' ? found[found.length - 1] : which === 'second' && found[1] != null ? found[1] : found[0]) : -1; };
  let i = -1;
  if (/intro/.test(role)) i = idx(/^\[Intro/, 'first');
  else if (/second verse/.test(role)) i = idx(/^\[Verse/, 'second');
  else if (/last verse/.test(role)) i = idx(/^\[Verse/, 'last');
  else if (/last hook|before the last hook|last four bars|end of the song|last thing/.test(role)) i = idx(/^\[(Hook|Loop)/, 'last');
  else if (/verse/.test(role)) i = idx(/^\[Verse/, 'first');
  else if (/halfway|far apart/.test(role)) { const all = lines.map((l, j) => (/^\[(Hook|Loop|Verse)/.test(l) ? j : -1)).filter((j) => j >= 0); i = all.length ? all[Math.floor(all.length / 2)] : -1; }
  else i = idx(/^\[(Hook|Loop)/, 'last');
  if (i < 0) i = idx(/^\[(Hook|Loop|Verse)/, 'last');
  if (i < 0) return lyrics;
  lines[i] = lines[i].replace(/\]$/, `, ${text}]`);
  return lines.join('\n');
}

/** A title built from the roll's own sounds, salted so the page can ask for another one when it has seen it. */
const STOP = new Set(['the', 'a', 'an', 'of', 'and', 'with', 'on', 'in']);
const cap = (w) => w.charAt(0).toUpperCase() + w.slice(1);
function makeTitle(seed, parts, salt = 0) {
  const r = mulberry32((seed ^ 0x9e3779b9) + salt * 7919 >>> 0);
  // A handle becomes its last two words, capitalised: "the murky keys" -> "Murky Keys", "the boing lead" -> "Boing Lead".
  const handleTitle = (t) => (t || '').replace(/[^a-z0-9\- ]/gi, ' ').split(/\s+/).filter((w) => w && !STOP.has(w.toLowerCase()) && !/^\d+$/.test(w)).slice(-2).map(cap).join(' ');
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
  contextOk = () => true;
  const warnings = [];

  const attitude = input.attitude && G.ATTITUDE[input.attitude] ? input.attitude : weighted(lane.attitudes || G.ATTITUDE_WEIGHTS, r);
  currentAttitude = attitude;
  const sadRe = G.ATTITUDE[attitude].sad;
  const strut = attitude === 'strut';
  attitudeOk = sadRe ? (str) => !sadRe.test(str) && !(strut && /\blate(,|$)/.test(str)) : () => true;

  const harmony = T.harmony(r, pick, { avoidKeys: input.avoidKeys || [], majorShare: strut ? Math.max(lane.majorShare, 0.5) : lane.majorShare, soulShare: strut ? 0.15 : lane.soulShare, forceKey: input.key || null, strut });
  const [lo, hi] = pick(lane.tempo, r);
  const bpm = input.bpm ? Number(input.bpm) : int(lo, hi, r);
  if (!(bpm >= 60 && bpm <= 200)) throw new Error('BPM must be between 60 and 200');
  const halfTime = bpm >= lane.halfTimeAbove;

  let allowedLayouts = LAYOUTS.filter((l) => lane.layoutWeights[l.id]);
  if (input.minimal) { const bareOnes = allowedLayouts.filter((l) => l.minimal >= 0.9); if (bareOnes.length) allowedLayouts = bareOnes; }
  if (input.layout) { const L = LAYOUTS.find((l) => l.id === input.layout); if (!L) throw new Error(`Unknown layout "${input.layout}". Available: ${LAYOUTS.map((l) => l.id).join(', ')}`); allowedLayouts = [L]; }
  else if (input.mode) { const byMode = allowedLayouts.filter((l) => l.mode === input.mode); if (!byMode.length) throw new Error(`No layout in this lane for mode "${input.mode}" (loop | straight | switch)`); allowedLayouts = byMode; }
  // A layout whose first full beat cannot land by 0:45 at this tempo (even with a 2-bar intro) is not offered.
  const fitting = allowedLayouts.filter((l) => estimate(l, bpm, halfTime, 2).hookAtSeconds <= RADIO.hookByMax);
  if (fitting.length) allowedLayouts = fitting;
  const layout = weighted(allowedLayouts.map((l) => [l, lane.layoutWeights[l.id] || l.weight || 1]), r);
  const mode = layout.mode;
  const allow = makeAllow(mode);
  if (mode === 'loop') contextOk = (str) => !/\b(hooks?|verses?)\b/i.test(str);

  const lead = buildLead(r, lane, allow);
  const counter = mode === 'loop' ? null : buildCounter(r, lane, lead, allow, mode);
  const bare = !!(layout.bare || input.minimal);
  let hint = bare ? null : buildHint(r, lane, lead, allow, mode, input.hint);
  let sprinkle = bare ? null : buildSprinkle(r, lane, hint, allow, mode, input.sprinkle === false ? 0 : input.sprinkle === true ? 1 : 0.35);
  let headStyle = input.headStyle || weighted([['anchor', 50], ['leadfirst', 30], ['drumsfirst', 20]], r);
  const bass = buildBass(r, lane, lead, allow);
  const drums = buildDrums(r, lane, lead, allow, mode);
  const sw = buildSwitch(r, lane, drums);
  const rhythm = buildRhythm(r, lane, mode, lead, sw);
  if (strut) rhythm.slur = null; // the drunk slur is the opposite of a strut; the slur pool is all lazy / stumble words
  const anti = pick(lanePool(G.ANTI_TRANSITION), r);
  const inLane = lead.family === lane.mainFamily || lane.families[lead.family];
  // A drums-first opening needs drum words that say something; three bare nouns fall back to the anchor.
  if (headStyle === 'drumsfirst' && !input.headStyle && [drums.kick, drums.snare, drums.hats].filter((d) => /\s/.test(drumWord(d))).length < 2) headStyle = 'anchor';
  // The opening only promises sounds the roll carries in a slot that is never cut; otherwise the neutral anchor speaks.
  const used = (a) => (headStyle === 'anchor' ? a : a.split(',')[0]);
  const fits = (pool) => lanePool(pool).filter((a) => anchorFits(used(a), { lead, bass, drums, bare }, lane));
  const strutPool = strut && lane.strutAnchors ? fits(lane.strutAnchors) : [];
  const anchorPool = fits(lane.anchors);
  const anchor = strutPool.length && r() < 0.6 ? pick(strutPool, r) : anchorPool.length ? pick(anchorPool, r) : lane.neutralAnchor;
  const attitudeTag = pick(lanePool(G.ATTITUDE[attitude].tags), r);
  // no head tag repeats another: the anti-transition tag must not already be inside the mode tag
  const antiTagPool = lanePool(G.ANTI_TAGS).filter((t) => !rhythm.mode.includes(t));
  const antiTagPick = antiTagPool.length ? pick(antiTagPool, r) : null;
  const mixTag = lane.neutralMixTag;
  // One small, safe change per section for the lyrics tags, used when a cue would otherwise repeat.
  const passCues = [...lanePool(G.SECTION_CUES).filter((c) => !(bare && /hats/.test(c)))].map((c) => [r(), c]).sort((a, b) => a[0] - b[0]).map(([, c]) => c).slice(0, 6);

  // User words go through the guard; anything that trips it is dropped and reported.
  const guardWords = (list, label) => list.filter((w) => { const h = Guard.check(w, lane.allow, lane.deny); if (h.length) { warnings.push(`${label} "${w}" dropped: ${h[0].rule} ("${h[0].word}") ${h[0].why}`); return false; } return true; });
  const moods = guardWords(splitList(input.mood), 'mood word');
  const extraTags = guardWords(splitList(input.constraints), 'must-have');

  // Intro length and the radio window: the first full beat lands by 0:45.
  let introBars = pick([2, 4, 4, 8], r);
  let est = estimate(layout, bpm, halfTime, introBars);
  if (est.hookAtSeconds > RADIO.hookByMax && introBars > 2) { introBars = 2; est = estimate(layout, bpm, halfTime, introBars); }
  const plan = { totalSeconds: est.totalSeconds, hookAtSeconds: est.hookAtSeconds };

  const antiTag = mode === 'loop' ? null : antiTagPick; // the loop tag already says it
  if (mode === 'straight') rhythm.mode = null; // the anti-transition tag already says it
  const p = { anchor, bpm, halfTime, key: harmony.key, mode, layout, moods, extraTags, mixTag, anti, antiTag, bare, headStyle, attitude, attitudeTag, introBars, plan, passCues, lead, counter, hint, sprinkle, bass, drums, sw, rhythm };
  const main = styleField(p, lane);
  // An ingredient that did not fit the Style field is not named in the lyrics either.
  if (!main.hintKept) { hint = null; p.hint = null; }
  if (!main.sprinkleKept) { sprinkle = null; p.sprinkle = null; }
  p.displaced = main.displaced;
  const style = main.text;
  const lyrics = lyricsField(p);
  if (!main.drumsKept) warnings.push('the drums line did not fit the Style field; the beat may come back generic');

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
      mode, layout: layout.id, layoutLabel: layout.label, bare, headStyle, attitude, seed, title, wildSuggested, hint: hint ? { family: hint.family, handle: hint.handle } : null, sprinkle: sprinkle ? { family: sprinkle.family, handle: sprinkle.handle, role: sprinkle.role } : null, generatedWithSpecVerifiedOn: SPEC.verifiedOn,
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
    parts: { lead, counter, hint, sprinkle, bass, drums, switch: sw, rhythm, anti, antiTag, bare, headStyle, attitude, attitudeTag, harmony },
    fingerprint: [laneId, lead.source, lead.character, lead.motif, lead.articulation, lead.hook, lead.human, mode === 'switch' ? sw.tag : mode, harmony.key, counter ? counter.source : 'loop', hint ? hint.source : 'nohint', sprinkle ? sprinkle.source : 'nosprinkle', headStyle, attitude].join('|'),
    warnings,
  };
}

function postGen(lane, p, totalFormatted) {
  const f = SPEC.features;
  return [
    ...(p.bare ? ['Bare mode: the lead, one kick, one bass and silence; no counter, no hint, no percussion colour. If Suno adds anything, raise Style Influence five points and keep "bare minimum, nothing else" in the head.'] : []),
    `Structure: the first full beat lands at about ${fmtTime(p.plan.hookAtSeconds)}; the layout is "${p.layout.label}" and runs about ${totalFormatted}. ${p.mode === 'switch' ? `The beat switch is one axis ("${p.sw.tag}"); its words repeat in the head tag and in every section after the switch so Suno cannot undo it.` : p.mode === 'loop' ? 'Loop mode: every [Loop] tag says identical on purpose; if Suno adds changes anyway, raise Style Influence five points.' : 'Straight mode: the same drums in every section, hard cuts only.'}`,
    `Attitude: ${p.attitude} ("${p.attitudeTag}"). ${p.attitude === 'strut' ? 'Every sad, dragging or mournful phrase was stripped from the pools for this roll, the keys lean major or Mixolydian, the kick stomps on the one and the timing is placed, not late. If it still comes back heavy-lidded, re-roll with --attitude strut and a major key.' : p.attitude === 'cold' ? 'Menace without sadness: patient, even, nothing rushed.' : p.attitude === 'low' ? 'Reflective and inward; the slur and the late snare are allowed here.' : 'Off the leash; run it on v6-wild.'}`,
    ...(p.sprinkle ? [`The mystery ingredient (${p.sprinkle.handle}) appears ${p.sprinkle.role}: it is meant to be barely there and to send the track over the edge once. If Suno makes it constant, lower Style Influence five points; if it is missing, that is acceptable on a first take.`] : []),
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

module.exports = { generate, makeTitle, LANES, LANE_WEIGHTS, LAYOUTS, SPEC, RADIO, splitList, styleField, PROMISES };
