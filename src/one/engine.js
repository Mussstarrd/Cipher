/* ONE OF ONE — one engine. Builds every sound from grammar slots, resolves
 * clashes, and lays the result out in Suno's own field order. */
"use strict";

const OneEngine = (() => {
  const G = typeof OneGrammar !== "undefined" ? OneGrammar : require("./grammar.js");
  const T = typeof OneTheory !== "undefined" ? OneTheory : require("./theory.js");
  const Guard = typeof OneGuard !== "undefined" ? OneGuard : require("./guard.js");

  function mulberry32(a) {
    return () => {
      a |= 0;
      a = (a + 0x6d2b79f5) | 0;
      let t = Math.imul(a ^ (a >>> 15), 1 | a);
      t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
      return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
    };
  }
  const pick = (arr, r) => arr[Math.floor(r() * arr.length)];
  // Pick from the entries that pass every rule; fall back to the whole list.
  const pickWhere = (arr, r, ok) => {
    const pool = arr.filter(ok);
    return pick(pool.length ? pool : arr, r);
  };
  const int = (lo, hi, r) => lo + Math.floor(r() * (hi - lo + 1));

  const TEMPO = [
    [72, 82],
    [86, 98],
    [132, 148],
    [150, 162],
  ];

  const EXCLUDE = [
    "vocals", "singing", "rap", "choir", "humming", "background vocals", "vocal chops", "chanting",
    "DJ scratching", "riser", "drum fills",
    "jazz", "smooth jazz", "blues", "big band", "EDM", "techno", "house", "elevator music", "lounge",
    "new age", "film score", "cinematic", "orchestral strings", "marching band", "lo-fi",
  ].join(", ");

  const weighted = (entries, r) => {
    const total = entries.reduce((n, [, w]) => n + w, 0);
    let x = r() * total;
    for (const [v, w] of entries) if ((x -= w) < 0) return v;
    return entries[entries.length - 1][0];
  };
  // "a drunken Hammond organ ...|the drunken organ" -> [full, handle]
  const split = (entry) => entry.split("|");

  function buildLead(r, lane) {
    const L = G.LEAD;
    const family = weighted(Object.entries(lane.families), r);
    const F = G.FAMILIES[family];
    const [source, handle] = split(pick(F.sources, r));
    const register = pickWhere(L.register, r, () => true);
    const low = /low register|doubling the 808|under the snare/.test(register);
    const motif = pick(L.motif, r);
    const long = /five|six|seven|run/.test(motif);
    const short = /two-note|one repeated|stubborn root/.test(motif);
    const articulation = pickWhere(L.articulation, r, (a) =>
      !(long && /one held|one anticipated|a push note|two stabs|three-note pickup/.test(a)) &&
      !(short && /three-note pickup/.test(a)));
    const legato = /legato|long tones|held note/.test(articulation);
    const placement = pick(L.placement, r);
    const dry = /dry|narrow|mono/.test(placement);
    const character = pickWhere(F.character, r, (c) =>
      !(dry && /chorus|detuned/.test(c)) &&
      !(legato && /gated/.test(c)) &&
      !(low && /band-passed/.test(c)) &&
      !(/detuned/.test(source) && /detuned|chorus|ring-mod/.test(c)));
    const motion = pickWhere(L.motion, r, (m) =>
      !(dry && /delay feedback|detune widens/.test(m)) &&
      !(legato && /shorter/.test(m)) &&
      !(/bitcrush/.test(m) && /low-passed/.test(character)));
    return {
      family, handle, frame: F.frame, source, character, register, articulation, placement, motion,
      motif,
      human: pick(L.human, r),
      low, dry,
      triplet: /triplet|three-against/.test(articulation),
      is808: /^an 808/.test(source),
    };
  }

  function buildBass(r, lead, lane) {
    const B = G.BASS;
    // A lane's own basses (the South Side tuba) come up about half the time.
    const fits = (s) => !(lead.low && /fingered|saw basses|tuba|sousaphone/.test(s)) && !(/tuba|sousaphone/.test(s) && /tuba|sousaphone/.test(lead.source));
    const laneFits = lane.bass.filter(fits);
    const pool = laneFits.length && r() < 0.5 ? laneFits : B.source;
    const source = lead.is808
      ? pick(["a clean sine sub", "a sub that only sustains on the root"], r)
      : pickWhere(pool, r, fits);
    const pattern = pickWhere(B.pattern, r, (p) =>
      !(lead.low && /motif|moving when/.test(p)) &&
      !(lead.triplet && /triplet/.test(p)) &&
      !(lead.is808 && !/one note|silent|three notes|root/.test(p)));
    return { source, tone: pick(B.tone, r), pattern, human: pick(B.human, r) };
  }

  function buildCounter(r, lead, lane) {
    // South Side keeps brass in the record: a non-brass lead gets a brass counter.
    const others = lane.id === "southside" && lead.family !== "brass"
      ? [["brass", 1]]
      : Object.entries(lane.families).filter(([f]) => f !== lead.family);
    const family = others.length ? weighted(others, r) : pick(Object.keys(G.FAMILIES).filter((f) => f !== lead.family), r);
    const F = G.FAMILIES[family];
    const [source, handle] = split(pickWhere(F.sources, r, (x) => !/808/.test(x)));
    return { family, source, handle, character: pick(F.character, r), role: pick(G.COUNTER_ROLE, r) };
  }

  function buildDrums(r, lead) {
    const D = G.DRUMS;
    const hats = pickWhere(D.hats, r, (h) => !(lead.triplet && /threes/.test(h)));
    const twitch = {
      sound: pick(G.TWITCH.sound, r),
      rhythm: pickWhere(G.TWITCH.rhythm, r, (x) => !(lead.triplet && /threes|triple/.test(x))),
      where: pick(G.TWITCH.where, r),
    };
    const triplet = lead.triplet
      ? "a heavy bounce, triplets felt but not played"
      : pickWhere(G.GROOVE.triplet, r, (t) => !(/threes/.test(hats) && /hats/.test(t)));
    return {
      color: pick(G.COLOR, r),
      kick: pick(D.kick, r),
      snare: pick(D.snare, r),
      hats,
      twitch,
      sync: pick(G.GROOVE.sync, r),
      feel: pick(G.GROOVE.human, r),
      triplet,
    };
  }

  function buildSwitch(r, lead, bass, drums) {
    const S = G.SWITCH;
    const when = pick(S.when, r);
    const what = pickWhere(S.what, r, (w) =>
      !(/only the twitch/.test(w) && /after the switch/.test(drums.twitch.where)) &&
      !(/go dry and the lead goes huge/.test(w) && !lead.dry) &&
      !(/swaps from 808/.test(w) && !/808/.test(bass.source)) &&
      !(/rolling in threes/.test(w) && /threes/.test(drums.hats)) &&
      !(/half-time/.test(w) && /half-time/.test(drums.snare)));
    const sparse = /only the twitch|strips to 808/.test(what);
    const land = pickWhere(S.land, r, (l) =>
      !(/new key/.test(l) && !/key/.test(what)) &&
      !(sparse && /new drums|drums join|everything slams/.test(l)) &&
      !(/silence/.test(l) && /silence|full-stop/.test(when)));
    return { when, what, land };
  }

  function style(p) {
    const { lead, bass, drums } = p;
    const head = `${p.anchor}, ${p.bpm} BPM, ${p.key}`;
    const leadLine = `lead: ${lead.source}, ${lead.frame}, ${lead.character}, ${lead.register}, playing ${lead.motif} as ${lead.articulation}, ${lead.human}, ${lead.placement}`;
    const counterLine = `counter: ${p.counter.source}, ${p.counter.character}, ${p.counter.role}`;
    const bassLine = `bass: ${bass.source}, ${bass.tone}, ${bass.human}`;
    const drumLine = `drums: ${drums.kick}, ${drums.snare}, ${drums.hats}, ${drums.color}`;
    const twitchLine = `twitch percussion: ${drums.twitch.sound} ${drums.twitch.rhythm}`;
    const grooveLine = `${drums.sync}, ${drums.feel}`;
    const tail = `${G.MIX}, instrumental`;
    // The lead gets every slot, up front; the extras go first when the field runs long.
    // The groove line also lives in the lyrics tags, so it is the first thing cut.
    const tries = [
      [head, leadLine, counterLine, bassLine, drumLine, twitchLine, grooveLine, tail],
      [head, leadLine, counterLine, bassLine, drumLine, twitchLine, tail],
      [head, leadLine, counterLine, bassLine, drumLine, tail], // twitch then rides in the lyrics tags
      [head, leadLine, `counter: ${p.counter.source}, ${p.counter.role}`, bassLine, drumLine, tail],
      [head, leadLine, `counter: ${p.counter.source}, ${p.counter.role}`, `bass: ${bass.source}, ${bass.human}`, drumLine, tail],
    ];
    let s;
    for (const t of tries) if ((s = t.join(". ") + ".").length <= 1000) break;
    return s;
  }

  function lyrics(p, r) {
    const { lead, drums, sw } = p;
    const h = lead.handle;
    const c = p.counter;
    const late = /after the switch/.test(drums.twitch.where);
    const twitch = p.styleHasTwitch
      ? `twitch percussion ${drums.twitch.where}`
      : `twitch percussion: ${drums.twitch.sound} ${drums.twitch.rhythm}, ${drums.twitch.where}`;
    const sections = [
      `[Intro | ${h} alone, ${lead.placement.split(",")[0]}]`,
      `[Verse | ${drums.kick.replace(/^an? /, "")}, ${drums.hats.replace(/^an? /, "")}, ${/808/.test(p.bass.source) ? "808" : "bass"} ${p.bass.pattern}, ${drums.sync}, ${late ? "drums only" : twitch}]`,
      `[Hook | full beat, ${h} up front, ${lead.motion}${/after the switch/.test(c.role) ? "" : `, ${c.handle} ${c.role}`}]`,
    ];
    const shape = int(0, 2, r);
    if (shape >= 1) sections.push(`[Verse 2 | ${drums.triplet}, ${h} thinner]`);
    if (shape === 2) sections.push(`[Break | drums cut, ${h} and 808 only]`);
    sections.push(`[Beat Switch | ${sw.when}, ${sw.what}, ${sw.land}]`);
    const after = [];
    if (/after the switch/.test(c.role)) after.push(`${c.handle} enters`);
    if (late) after.push(p.styleHasTwitch ? "twitch percussion enters" : `twitch percussion enters: ${drums.twitch.sound} ${drums.twitch.rhythm}`);
    if (!after.length) after.push(drums.sync);
    sections.push(`[Hook | after the switch, ${h} returns, ${after.join(", ")}]`);
    sections.push(`[Outro | ${h} alone, ends dry on beat 1]`);
    sections.push("[End]");
    return sections.join("\n\n");
  }

  function generate({ seed = 1, avoidKeys = [] } = {}) {
    const r = mulberry32(seed >>> 0);
    const harmony = T.harmony(r, pick, avoidKeys);
    const [lo, hi] = pick(TEMPO, r);
    const lane = weighted(G.LANES.map((l) => [l, l.weight]), r);
    const lead = buildLead(r, lane);
    const counter = buildCounter(r, lead, lane);
    const bass = buildBass(r, lead, lane);
    const drums = buildDrums(r, lead);
    const sw = buildSwitch(r, lead, bass, drums);
    const p = {
      anchor: pick(lane.anchors, r),
      counter,
      bpm: int(lo, hi, r),
      key: `in ${harmony.key}`,
      lead, bass, drums, sw,
    };
    const styleText = style(p);
    p.styleHasTwitch = styleText.includes("twitch percussion:");
    const lyricsText = lyrics(p, r);
    const warnings = Guard.check(`${styleText}\n${lyricsText}`).map((h) => ({ ...h, message: `${h.rule}: "${h.word}" ${h.why}` }));
    const weirdness = int(55, 65, r);
    const styleInfluence = int(65, 75, r);
    const model = r() < 0.5 ? "v6-wild" : "v6";
    return {
      style: styleText,
      lyrics: lyricsText,
      exclude: EXCLUDE,
      sliders: { weirdness, styleInfluence },
      settings: { model, variety: "Off", maxMode: "On" },
      meta: { lane: lane.label, seed: seed >>> 0, title: `${pick(G.TITLE.a, r)} ${pick(G.TITLE.b, r)}`, bpm: p.bpm, key: harmony.key },
      fingerprint: [counter.source, lead.source, lead.character, lead.motif, lead.articulation, lead.human, sw.what, harmony.key].join("|"),
      parts: { lead, counter, bass, drums, switch: sw },
      warnings,
    };
  }

  return { generate, EXCLUDE };
})();

if (typeof module !== "undefined") module.exports = OneEngine;
