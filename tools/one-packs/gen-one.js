/* The ONE button: the hit anatomist's eleven archetype lanes (genre-hits.js)
 * fused with the artist's personality layer (personality.js). Emits the
 * `one` pack source for src/one/genres.js. */
"use strict";
const d = require("./genre-hits.js");
const P = require("../../src/one/personality.js");
const J = (v) => JSON.stringify(v);
const norm = (o) => Object.fromEntries(Object.entries(o).map(([k, v]) => [k.replace(/\s+/g, "_"), v]));
const sum = (o) => Object.values(o).reduce((a, b) => a + b, 0);

// How often the harmonica / erhu signature takes the lead in a hit lane.
const SIGNATURE_SHARE = 0.22;
const WEIGHTS = { cold: 3, pain: 3, soulslice: 3, earworm: 2, trunk: 2, squeak: 2, flute: 2, coast: 2, emo: 2, countryloop: 1, triumph: 1 };
const SLOW_OK = new Set(["pain", "emo", "slow"]);
const layoutIds = (laneId) => P.layouts.map((l) => l.id).filter((id) => id !== "slow_motif" || SLOW_OK.has(laneId));

// An anchor or mix tag that names the lane's own sound would promise an instrument the roll
// does not carry when the signature family or a guest takes the lead. Each lane gets a
// sound-neutral anchor and a neutral mix tag; the engine uses them whenever the lead leaves
// the lane's main family.
const NEUTRAL_ANCHORS = {
  squeak: "minimal trap, one found-sound hook in the mids, long 808, open space",
  countryloop: "country trap, one plucked loop pitched down, hard 808, half-time",
  triumph: "triumphant southern hip hop, one swelling lead, hard 808, cracking snare",
  soulslice: "soul-sample hip hop, one sliced loop, knocking 808, boom bap pocket",
  flute: "dark trap, one low breathy lead over a sliding 808, half-time",
  pain: "pain trap, one low sad lead, triplet hats, long 808 slides",
  cold: "cold southern trap, one dead-dry low lead, 808 doing the melody",
  coast: "West Coast bounce hip hop, one two-note plink lead, hard claps, synth bass",
  emo: "sad melodic trap, one low picked lead, soft 808, half-time",
  earworm: "pop trap, one three-note hook, hook first, steady beat",
  trunk: "trunk-rattling trap, one hard-clipped low lead, huge 808, beat switch",
};
// Short mix tags (the style field is tight) that name no instrument.
const MIX = {
  squeak: "clean radio mix, huge 808", countryloop: "loud radio mix, 808 huge", triumph: "big loud radio mix, 808 in front",
  soulslice: "crisp loud mix, 808 knocking", flute: "dark crisp mix, 808 huge", pain: "warm loud mix, 808 round and huge",
  cold: "cold dry mix, 808 hard and short", coast: "loud club-ready radio mix, claps huge", emo: "clean radio mix, 808 round",
  earworm: "crisp punchy radio mix, 808 huge", trunk: "loud dark mix, 808 the biggest thing",
};
const laneOf = (l) => {
  const families = norm(l.families);
  const main = Object.entries(families).sort((a, b) => b[1] - a[1])[0][0];
  const tot = sum(families);
  families.signature = +((tot * SIGNATURE_SHARE) / (1 - SIGNATURE_SHARE)).toFixed(2);
  return {
    id: l.id, label: l.label, weight: WEIGHTS[l.id] || 1, anchors: l.anchors, families, tempo: l.tempo, modes: l.modes,
    majorShare: l.majorShare, mixTag: MIX[l.id] || l.mixTag, mainFamily: main, neutralAnchor: NEUTRAL_ANCHORS[l.id], drums: l.drums, bassPattern: l.bassPattern, switchAxes: l.switchAxes,
    bass: [], layouts: layoutIds(l.id), ...(l.id === "triumph" ? { counterFamily: "brass_stack" } : {}),
  };
};
const by = (id) => d.lanes.find((l) => l.id === id);
const pain = by("pain"), trunk = by("trunk");

const ballad = {
  id: "slow", label: "Slow and sad", weight: 2.5,
  anchors: [
    "slow sad hip hop, low piano, long 808, half-time",
    "slow heavy trap, long notes under the snare, sad and low",
    "sad hip hop, quiet piano and 808, slow",
    "slow pain trap, long held notes, heavy drums, low",
  ],
  families: { slow_lead: 5, signature: 3, pain_piano: 2 },
  tempo: [[58, 72]], modes: pain.modes, majorShare: 0.35,
  mixTag: "quiet wide mix, 808 soft and long",
  tags: P.ballad.tags, drums: pain.drums, bassPattern: pain.bassPattern, switchAxes: pain.switchAxes, bass: [],
  layouts: ["slow_motif", "long_fade", "verse_bare_hook_full", "call_response", "lead_out_middle"],
  sliders: { weirdness: [38, 50], styleInfluence: [75, 85] },
};

const weird = {
  id: "weird", label: "Weird, v6-wild", weight: 2.5,
  anchors: P.weird.anchors,
  families: { warped: 4, signature: 2, found: 3, synth: 2, organ: 1, soul_slices: 2 },
  tempo: [[66, 84]], modes: trunk.modes, majorShare: 0.1,
  mixTag: P.weird.mixTag, tags: P.weird.tags.concat(P.weird.dreamville),
  drums: trunk.drums, bassPattern: trunk.bassPattern, switchAxes: trunk.switchAxes, bass: [],
  layouts: layoutIds("weird"), model: P.weird.model, sliders: P.weird.sliders,
};

const lanes = d.lanes.map(laneOf).concat([ballad, weird]);
const bpm = (list) => list.map((s) => s.match(/(\d+)-(\d+)/).slice(1, 3).map(Number));
const fams = (f) => {
  const out = {};
  for (const [k, v] of Object.entries(f)) out[k.replace(/\s+/g, "_")] = { frame: "over hard radio trap drums and a huge 808", sources: v.src, character: v.ch };
  out.slow_lead = { frame: "slow, over heavy half-time drums and a long 808", sources: P.ballad.sources, character: f["pain piano"].ch };
  out.warped = { frame: "over slowed, slammed experimental drums and a blown-out 808", sources: P.weird.sources, character: f["trunk stabs"].ch };
  return out;
};
const exclude = [...new Set(d.exclude.split(", ").concat(P.antiTransition.excludeTerms))];

module.exports = `  one: {
    id: "one",
    label: "ONE OF ONE",
    blurb: "One button. The sound that ran radio for the last decade, eleven archetypes from squeak-and-space to trunk 808 switches, with the harmonica and erhu slur worked in. Some rolls are two sounds and a kick, some are slow and sad, one in eight goes weird on v6-wild.",
    grammar: {
      TEMPO: ${J(bpm(d.bpm))},
      MAJOR_SHARE: 0.3,
      GUEST_SHARE: 0.15,
      GUEST_FRAME: "over hard radio trap drums and a huge 808",
      MIX_TAG: "clean loud radio mix, huge 808, the lead dead center",
      MODEL: "v6",
      COMPACT: true,
      DRUMS_TAG: "hard modern trap drums and a huge 808",
      MODES: [["straight", 45], ["loop", 30], ["switch", 25]],
      SLUR_SHARE: 0.6,
      LOW_SHARE: 0.8,
      SLIDERS: { switch: { weirdness: [40, 55], styleInfluence: [75, 85] }, loop: { weirdness: [40, 52], styleInfluence: [76, 85] }, straight: { weirdness: [40, 55], styleInfluence: [75, 85] } },
      EXCLUDE: [...new Set(BASE_EXCLUDE.concat(${J(exclude)}))],
      LANES: ${J(lanes)},
      ALLOW: /${d.allow}/i,
      FAMILIES: { ...${J(fams(d.families))}, signature: P.signature, found: Base.FAMILIES.found, synth: Base.FAMILIES.synth, organ: Base.FAMILIES.organ },
      BASS: { source: ${J(d.bass)}.concat(Base.BASS.source.filter((x) => /808|sub/.test(x) && !/tuba|sousaphone|horn/.test(x))), tone: ${J(d.btone)}, pattern: Base.BASS.pattern, human: Base.BASS.human },
      DRUMS: { kick: ${J(d.kick)}, snare: ${J(d.snare)}, hats: ${J(d.hats)} },
      COLOR: ${J(d.color)},
      GROOVE: { sync: ${J(d.sync)}, human: ${J(d.human.map((h) => h.replace(/^the piano/, "the lead")))}, triplet: Base.GROOVE.triplet },
      LEAD: { ...Base.LEAD, motif: ${J(d.motif)}, articulation: ${J(d.artic)} },
      COUNTER_ROLE: Base.COUNTER_ROLE.concat(P.signatureCounterRoles),
      RHYTHM: { ...Base.RHYTHM, sync: P.syncopation, twitch: P.twitch, slur: P.slurCadence },
      ANTI: P.antiTransition.phrases,
      LAYOUTS: P.layouts,
      LAYOUT_TAGS: { bare_loop: P.minimal, kick_and_lead: P.minimal, one_in_one_out: P.minimal },
      SWITCH: { axes: Base.SWITCH.axes, land: ${J(["on a hard cut", "straight in on beat 1", "beat B slams in on the one", "one beat of dead silence, then beat B", "the 808 tail the only thing left, then beat B on the one", "cut dead, beat B on the next downbeat"])} },
      STRUCTURE: null,
    },
  },`;
