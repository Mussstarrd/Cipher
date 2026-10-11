/* ONE OF ONE — one engine. Builds every sound from grammar slots, resolves
 * clashes, and lays the result out in Suno's own field order.
 *
 * Style field order (Suno weights the front and reads it as tags):
 *   1. anchor, BPM, key
 *   2. rhythm tags: register, syncopation, twitch, triplet slur, loop or switch, human feel
 *   3. lead (every slot), bass, drums, counter, twitch detail
 *   4. mix, instrumental
 * When the field runs long the tail is trimmed from the middle out; the
 * front tags are never cut.
 *
 * A genre pack (src/one/genres.js) overrides whole sections of the base
 * grammar: FAMILIES, LANES, BASS, DRUMS, TWITCH, GROOVE, SWITCH, COLOR,
 * RHYTHM, TEMPO, MIX, EXCLUDE, SLIDERS, LOOP_SHARE, and a STRUCTURE hook. */
"use strict";

const OneEngine = (() => {
  const Base = typeof OneGrammar !== "undefined" ? OneGrammar : require("./grammar.js");
  const Genres = typeof OneGenres !== "undefined" ? OneGenres : require("./genres.js");
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
  const weighted = (entries, r) => {
    const total = entries.reduce((n, [, w]) => n + w, 0);
    let x = r() * total;
    for (const [v, w] of entries) if ((x -= w) < 0) return v;
    return entries[entries.length - 1][0];
  };
  // "a drunken detuned organ ...|the drunken organ" -> [full, handle]
  const split = (entry) => entry.split("|");
  const bare = (s) => s.replace(/^an? /, "");

  const grammarFor = (genreId) => {
    const pack = Genres[genreId] || Genres.hiphop;
    return { ...Base, ...pack.grammar, id: pack.id, label: pack.label };
  };

  function buildLead(r, G, lane) {
    const L = G.LEAD;
    const family = weighted(Object.entries(lane.families), r);
    const F = G.FAMILIES[family];
    const [source, handle] = split(pick(F.sources, r));
    const register = pick(L.register, r);
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
      !(/bitcrush|grittier/.test(m) && /low-passed/.test(character)));
    return {
      family, handle, frame: F.frame, source, character, register, articulation, placement, motion, motif,
      human: pick(L.human, r),
      low, dry,
      triplet: /triplet|three-against/.test(articulation),
      is808: /^an 808/.test(source),
    };
  }

  function buildBass(r, G, lead, lane) {
    const B = G.BASS;
    const fits = (s) => !(lead.low && /fingered|saw basses|tuba|sousaphone/.test(s)) && !(/tuba|sousaphone/.test(s) && /tuba|sousaphone/.test(lead.source));
    const laneBass = lane.bass || [];
    const laneFits = laneBass.filter(fits);
    // A lane's own basses (the South Side tuba) come up about half the time.
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

  function buildCounter(r, G, lead, lane) {
    const must = lane.counterFamily && lead.family !== lane.counterFamily ? [[lane.counterFamily, 1]] : null;
    const others = must || Object.entries(lane.families).filter(([f]) => f !== lead.family);
    const family = others.length ? weighted(others, r) : pick(Object.keys(G.FAMILIES).filter((f) => f !== lead.family), r);
    const F = G.FAMILIES[family];
    const [source, handle] = split(pickWhere(F.sources, r, (x) => !/808/.test(x)));
    return { family, source, handle, character: pick(F.character, r), role: pick(G.COUNTER_ROLE, r) };
  }

  function buildDrums(r, G, lead) {
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

  function buildSwitch(r, G, lead, bass, drums) {
    const S = G.SWITCH;
    const when = pick(S.when, r);
    const what = pickWhere(S.what, r, (w) =>
      !(/only the twitch/.test(w) && /after the switch/.test(drums.twitch.where)) &&
      !(/go dry and the lead goes huge/.test(w) && !lead.dry) &&
      !(/swaps? (from|to)/.test(w) && !/808/.test(bass.source)) &&
      !(/rolling in threes/.test(w) && /threes/.test(drums.hats)) &&
      !(/half-time/.test(w) && /half-time/.test(drums.snare)));
    const sparse = /only the twitch|strips to 808/.test(what);
    const land = pickWhere(S.land, r, (l) =>
      !(/new key/.test(l) && !/key/.test(what)) &&
      !(sparse && /new drums|drums join|everything slams/.test(l)) &&
      !(/silence/.test(l) && /silence|full-stop/.test(when)));
    return { when, what, land };
  }

  function buildRhythm(r, G, mode) {
    const R = G.RHYTHM;
    return {
      register: pick(R.register, r),
      sync: pick(R.sync, r),
      twitch: pick(R.twitch, r),
      cadence: pick(R.cadence, r),
      mode: mode === "loop" ? pick(R.loopTag, r) : pick(R.switchTag, r),
      human: pick(R.human, r),
    };
  }

  function style(p, G) {
    const { lead, bass, drums, rhythm } = p;
    const head = [
      `${p.anchor}, ${p.bpm} BPM, ${p.key}`,
      rhythm.register, rhythm.sync, rhythm.twitch, rhythm.cadence, rhythm.mode, rhythm.human,
    ].join(", ");
    const leadLine = `lead: ${lead.source}, ${lead.frame}, ${lead.character}, ${lead.register}, playing ${lead.motif} as ${lead.articulation}, ${lead.human}, ${lead.placement}`;
    const leadShort = `lead: ${lead.source}, ${lead.character}, ${lead.register}, playing ${lead.motif} as ${lead.articulation}, ${lead.human}`;
    const counterLine = `counter: ${p.counter.source}, ${p.counter.character}, ${p.counter.role}`;
    const counterShort = `counter: ${p.counter.source}, ${p.counter.role}`;
    const bassLine = `bass: ${bass.source}, ${bass.tone}, ${bass.human}`;
    const bassShort = `bass: ${bass.source}, ${bass.human}`;
    const drumLine = `drums: ${drums.kick}, ${drums.snare}, ${drums.hats}, ${drums.color}`;
    const drumShort = `drums: ${drums.kick}, ${drums.snare}, ${drums.hats}`;
    const twitchLine = `twitch percussion: ${drums.twitch.sound} ${drums.twitch.rhythm}`;
    const tail = `${G.MIX}, instrumental`;
    // The front tags and the lead always stay. Everything else gives way in this
    // order: the lead's frame (the anchor already says it), the twitch detail
    // (it lives in the lyrics tags too), then the counter's processing, the bass
    // tone, the percussion color, and last the lead's placement.
    const leadNoFrame = `lead: ${lead.source}, ${lead.character}, ${lead.register}, playing ${lead.motif} as ${lead.articulation}, ${lead.human}, ${lead.placement}`;
    const tries = [
      [head, leadLine, bassLine, drumLine, counterLine, twitchLine, tail],
      [head, leadNoFrame, bassLine, drumLine, counterLine, twitchLine, tail],
      [head, leadNoFrame, bassLine, drumLine, counterLine, tail],
      [head, leadNoFrame, bassLine, drumLine, counterShort, tail],
      [head, leadNoFrame, bassShort, drumLine, counterShort, tail],
      [head, leadNoFrame, bassShort, drumShort, counterShort, tail],
      [head, leadShort, bassShort, drumShort, counterShort, tail],
      [head, leadShort, bassShort, drumShort, tail],
    ];
    let s;
    for (const t of tries) if ((s = t.join(". ") + ".").length <= 1000) break;
    return s;
  }

  function lyrics(p, G, r) {
    if (G.STRUCTURE) return G.STRUCTURE(p, r, { bare, int, pick });
    const { lead, drums, sw, bass } = p;
    const h = lead.handle;
    const c = p.counter;
    const bassWord = /808/.test(bass.source) ? "808" : "bass";
    const late = /after the switch/.test(drums.twitch.where);
    const twitch = p.styleHasTwitch
      ? `twitch percussion ${drums.twitch.where}`
      : `twitch percussion: ${drums.twitch.sound} ${drums.twitch.rhythm}, ${drums.twitch.where}`;
    const counterIn = /after the switch/.test(c.role) ? "" : `, ${c.handle} ${c.role}`;

    if (p.mode === "loop") {
      // One loop the whole way. Sections are the same loop with one thing added or taken away.
      const loop = `${h} playing ${bare(lead.articulation)}, ${bare(drums.kick)}, ${bare(drums.hats)}, ${bassWord} ${bass.pattern}`;
      const sections = [
        `[Intro | ${h} alone, ${lead.placement.split(",")[0]}, 2 bars]`,
        `[Loop | ${loop}, ${drums.sync}]`,
        `[Loop | same loop, ${/after the switch/.test(drums.twitch.where) ? `twitch percussion enters: ${drums.twitch.sound} ${drums.twitch.rhythm}` : twitch}${counterIn}]`,
        `[Loop | same loop, drums cut out for 2 bars, then slam back, ${drums.triplet}]`,
        `[Loop | same loop, ${lead.motion}]`,
        `[Loop | same loop, ${drums.feel}, 808 only for the last bar]`,
        `[Outro | ${h} alone, cuts dead on beat 1]`,
        "[End]",
      ];
      return sections.join("\n\n");
    }

    const sections = [
      `[Intro | ${h} alone, ${lead.placement.split(",")[0]}]`,
      `[Verse | ${bare(drums.kick)}, ${bare(drums.hats)}, ${bassWord} ${bass.pattern}, ${drums.sync}, ${late ? "drums only" : twitch}]`,
      `[Hook | full beat, ${h} up front, ${lead.motion}${counterIn}]`,
    ];
    const shape = int(0, 2, r);
    if (shape >= 1) sections.push(`[Verse 2 | ${drums.triplet}, ${h} thinner]`);
    if (shape === 2) sections.push(`[Break | drums cut, ${h} and ${bassWord} only]`);
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

  function generate({ seed = 1, genre = "hiphop", mode, avoidKeys = [] } = {}) {
    const G = grammarFor(genre);
    const r = mulberry32(seed >>> 0);
    const harmony = T.harmony(r, pick, avoidKeys, G.MAJOR_SHARE);
    const [lo, hi] = pick(G.TEMPO, r);
    const lane = weighted(G.LANES.map((l) => [l, l.weight]), r);
    const chosenMode = mode || (r() < G.LOOP_SHARE ? "loop" : "switch");
    const lead = buildLead(r, G, lane);
    const counter = buildCounter(r, G, lead, lane);
    const bass = buildBass(r, G, lead, lane);
    const drums = buildDrums(r, G, lead);
    const sw = buildSwitch(r, G, lead, bass, drums);
    const rhythm = buildRhythm(r, G, chosenMode);
    const p = {
      anchor: pick(lane.anchors, r),
      bpm: int(lo, hi, r),
      key: `in ${harmony.key}`,
      mode: chosenMode,
      lead, counter, bass, drums, sw, rhythm,
    };
    const styleText = style(p, G);
    p.styleHasTwitch = styleText.includes("twitch percussion:");
    const lyricsText = lyrics(p, G, r);
    const warnings = Guard.check(`${styleText}\n${lyricsText}`).map((h) => ({ ...h, message: `${h.rule}: "${h.word}" ${h.why}` }));
    const weirdness = int(G.SLIDERS.weirdness[0], G.SLIDERS.weirdness[1], r);
    const styleInfluence = int(G.SLIDERS.styleInfluence[0], G.SLIDERS.styleInfluence[1], r);
    const model = r() < 0.5 ? "v6-wild" : "v6";
    return {
      style: styleText,
      lyrics: lyricsText,
      exclude: G.EXCLUDE.join(", "),
      sliders: { weirdness, styleInfluence },
      settings: { model, variety: "Off", maxMode: "On" },
      meta: {
        genre: G.id, genreLabel: G.label, lane: lane.label, mode: chosenMode, seed: seed >>> 0,
        title: `${pick(G.TITLE.a, r)} ${pick(G.TITLE.b, r)}`, bpm: p.bpm, key: harmony.key,
      },
      fingerprint: [G.id, counter.source, lead.source, lead.character, lead.motif, lead.articulation, lead.human, chosenMode === "loop" ? "loop" : sw.what, harmony.key].join("|"),
      parts: { lead, counter, bass, drums, switch: sw, rhythm },
      warnings,
    };
  }

  const genres = () => Object.values(Genres).map((g) => ({ id: g.id, label: g.label, blurb: g.blurb }));

  return { generate, genres };
})();

if (typeof module !== "undefined") module.exports = OneEngine;
