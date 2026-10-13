/* ONE OF ONE — one engine. Builds every sound from grammar slots, resolves
 * clashes, and lays the result out in Suno's own field order.
 *
 * Style field order (Suno weights the front and reads it as tags):
 *   head: anchor, BPM, key, SYNC, TWITCH, SLUR, SWITCH or LOOP, LOW LEAD,
 *         lead handle, mix tag, instrumental            (~220 chars, never cut)
 *   prose: lead (every slot), bass, drums, counter       (trimmed from the end)
 *
 * Modes: switch (one axis, its words repeated in the head, the cue and every
 * later section), loop (identical [Loop] cues, nothing arranged), straight.
 * "Every N bars" phrases are capped at one per roll and zero in loop mode,
 * because they teach Suno to change something every four bars.
 *
 * A genre pack (src/one/genres.js) overrides whole sections of the base
 * grammar and may supply its own STRUCTURE(p, r, h) for the lyrics field. */
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
  // Phrases that teach Suno to change something every four bars.
  const EVERY = /every (\d+\w*|other|third) (bar|pass|4|8)|\bbar \d+( and bar \d+)?|every \d+ bars|partway/i;

  const grammarFor = (genreId) => {
    const pack = Genres[genreId] || Genres.hiphop;
    return { ...Base, ...pack.grammar, id: pack.id, label: pack.label };
  };

  // One "every N bars" phrase per roll, none in loop mode.
  function makeAllow(mode) {
    let left = mode === "loop" ? 0 : 1;
    const fn = (phrase) => {
      if (EVERY.test(phrase)) left--;
    };
    fn.peek = () => left > 0;
    return fn;
  }
  const pickBudget = (arr, r, allow, ok = () => true) => {
    const chosen = pickWhere(arr, r, (x) => ok(x) && (!EVERY.test(x) || allow.peek()));
    allow(chosen);
    return chosen;
  };

  // Guest leads: a spin-off genre borrows the experimental hip hop palette
  // (found sounds, mutant synths, fuzz guitar, drunk organ, flipped wind, low
  // brass) for the lead some of the time. The counter then stays in-genre so
  // the record keeps its identity.
  const GUEST_FAMILIES = [["found", 3], ["synth", 3], ["guitar", 2], ["organ", 2], ["wind", 1], ["brass", 1]];

  function buildLead(r, G, lane, allow) {
    const L = G.LEAD;
    const guest = (G.GUEST_SHARE || 0) > 0 && r() < G.GUEST_SHARE;
    const family = guest ? weighted(GUEST_FAMILIES, r) : weighted(Object.entries(lane.families), r);
    const F = guest ? { ...Base.FAMILIES[family], frame: G.GUEST_FRAME || Base.FAMILIES[family].frame } : G.FAMILIES[family];
    const [source, handle] = split(pick(F.sources, r));
    const lowFirst = r() < G.LOW_SHARE;
    const register = pickWhere(L.register, r, (x) => (lowFirst ? !/^mid register/.test(x) : true));
    const low = !/^mid register|low-mid, stepping/.test(register);
    const motif = pickBudget(L.motif, r, allow);
    const long = /five|six|seven|run/.test(motif);
    const short = /two-note|one repeated|stubborn root/.test(motif);
    const articulation = pickBudget(L.articulation, r, allow, (a) =>
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
    const motion = pickBudget(L.motion, r, allow, (m) =>
      !(dry && /delay feedback|detune widens/.test(m)) &&
      !(legato && /shorter/.test(m)) &&
      !(/bitcrush|grittier/.test(m) && /low-passed/.test(character)));
    return {
      family, guest, handle, frame: F.frame, source, character, register, articulation, placement, motion, motif,
      human: pick(L.human, r),
      low, dry,
      triplet: /triplet|three-against/.test(articulation),
      is808: /^an 808/.test(source),
    };
  }

  function buildBass(r, G, lead, lane, allow) {
    const B = G.BASS;
    const fits = (s) => !(lead.low && /fingered|saw basses|tuba|sousaphone/.test(s)) && !(/tuba|sousaphone/.test(s) && /tuba|sousaphone/.test(lead.source));
    const laneBass = lane.bass || [];
    const laneFits = laneBass.filter(fits);
    // A lane's own basses (the South Side tuba) come up about half the time.
    const pool = laneFits.length && r() < 0.5 ? laneFits : B.source;
    const source = lead.is808
      ? pick(["a clean sine sub", "a sub that only sustains on the root"], r)
      : pickWhere(pool, r, fits);
    const pattern = pickBudget(B.pattern, r, allow, (p) =>
      !(lead.low && /motif|moving when/.test(p)) &&
      !(lead.triplet && /triplet/.test(p)) &&
      !(lead.is808 && !/one note|silent|three notes|root/.test(p)));
    return { source, tone: pick(B.tone, r), pattern, human: pick(B.human, r) };
  }

  function buildCounter(r, G, lead, lane, allow, mode) {
    const forced = lane.counterFamily && lead.family !== lane.counterFamily && r() < 0.5;
    // A guest lead gets an in-genre counter, so the genre's own sound is always in the record.
    const others = forced ? [[lane.counterFamily, 1]] : Object.entries(lane.families).filter(([f]) => lead.guest || f !== lead.family);
    const family = others.length ? weighted(others, r) : pick(Object.keys(G.FAMILIES).filter((f) => f !== lead.family), r);
    const F = G.FAMILIES[family];
    const [source, handle] = split(pickWhere(F.sources, r, (x) => !/808/.test(x)));
    const role = pickBudget(G.COUNTER_ROLE, r, allow, (x) => !(mode !== "switch" && /after the switch/.test(x)));
    return { family, source, handle, character: pick(F.character, r), role };
  }

  function buildDrums(r, G, lead, allow, mode) {
    const D = G.DRUMS;
    const hats = pickBudget(D.hats, r, allow, (h) => !(lead.triplet && /threes/.test(h)));
    const twitch = {
      sound: pick(G.TWITCH.sound, r),
      rhythm: pickBudget(G.TWITCH.rhythm, r, allow, (x) => !(lead.triplet && /threes|triple/.test(x))),
      where: pickBudget(G.TWITCH.where, r, allow, (w) => !(mode !== "switch" && /after the switch/.test(w))),
    };
    // One stumbling element only: when the hats roll in threes the slur goes elsewhere.
    const slur = pickWhere(G.GROOVE.triplet, r, (t) => !(/threes/.test(hats) && /hats/.test(t)));
    return {
      color: pickBudget(G.COLOR, r, allow),
      kick: pickBudget(D.kick, r, allow),
      snare: pickBudget(D.snare, r, allow),
      hats,
      twitch,
      sync: pickBudget(G.GROOVE.sync, r, allow),
      feel: pick(G.GROOVE.human, r),
      slur,
    };
  }

  function buildSwitch(r, G, lead, bass, drums) {
    const S = G.SWITCH;
    const axis = pickWhere(S.axes, r, (a) =>
      !(/half-time/.test(a.tag) && /half-time/.test(drums.snare)) &&
      !(/strips/.test(a.cue) && /after the switch/.test(drums.twitch.where)));
    const sparse = /strips/.test(axis.cue);
    const land = pickWhere(S.land, r, (l) =>
      !(sparse && /drums join|everything slams/.test(l)) &&
      !(/half-time/.test(axis.tag) && /two bars later/.test(l)));
    return { ...axis, land };
  }

  function buildRhythm(r, G, mode, lead, sw, allow) {
    const R = G.RHYTHM;
    const modeTag = mode === "switch" ? sw.tag : mode === "loop" ? pick(R.loop, r) : pick(R.straight, r);
    return {
      sync: pickBudget(R.sync, r, allow),
      twitch: pickBudget(R.twitch, r, allow),
      slur: r() < G.SLUR_SHARE ? pick(R.slur, r) : null,
      mode: modeTag,
      low: lead.low ? pick(R.low, r) : null,
    };
  }

  function style(p, G) {
    const { lead, bass, drums, rhythm } = p;
    const head = [
      `${p.anchor}, ${p.bpm} BPM, ${p.key}`,
      rhythm.sync, rhythm.twitch, rhythm.slur, p.mode === "loop" ? "minimal" : null, rhythm.mode, rhythm.low,
      lead.handle, G.MIX_TAG, "instrumental",
    ].filter(Boolean).join(", ") + ".";
    const leadLine = `lead: ${lead.source}, ${lead.character}, ${lead.register}, playing ${lead.motif} as ${lead.articulation}, ${lead.human}, ${lead.placement}.`;
    const leadNoPlace = `lead: ${lead.source}, ${lead.character}, ${lead.register}, playing ${lead.motif} as ${lead.articulation}, ${lead.human}.`;
    const leadShort = `lead: ${lead.source}, ${lead.character}, ${lead.register}, playing ${lead.motif} as ${lead.articulation}.`;
    const bassLine = `bass: ${bass.source}, ${bass.tone}, ${bass.human}.`;
    const bassShort = `bass: ${bass.source}, ${bass.human}.`;
    const slur = rhythm.slur ? `, ${drums.slur}` : "";
    const drumLine = `drums: ${drums.kick}, ${drums.snare}, ${drums.hats}, ${drums.color}${slur}.`;
    const drumShort = `drums: ${drums.kick}, ${drums.snare}, ${drums.hats}${slur}.`;
    const noCounter = p.mode === "loop" || G.NO_COUNTER;
    const counterLine = noCounter ? null : `counter: ${p.counter.source}, ${p.counter.character}, ${p.counter.role}.`;
    const counterShort = noCounter ? null : `counter: ${p.counter.source}, ${p.counter.role}.`;
    // The head and the lead always stay. Cut order: counter, bass tone,
    // percussion color, lead placement, lead human.
    const tries = [
      [head, leadLine, bassLine, drumLine, counterLine],
      [head, leadLine, bassLine, drumLine, counterShort],
      [head, leadLine, bassLine, drumLine],
      [head, leadLine, bassShort, drumLine],
      [head, leadLine, bassShort, drumShort],
      [head, leadNoPlace, bassShort, drumShort],
      [head, leadShort, bassShort, drumShort],
      [head, leadShort, bassShort, `drums: ${drums.kick}, ${drums.snare}, ${drums.hats}.`],
      [head, leadShort, bassShort],
    ];
    let s;
    for (const t of tries) if ((s = t.filter(Boolean).join(" ")).length <= 1000) break;
    return s;
  }

  function lyrics(p, G, r) {
    const h = { bare, int, pick };
    if (G.STRUCTURE) {
      const custom = G.STRUCTURE(p, r, h);
      if (custom) return custom;
    }
    const { lead, drums, sw, bass, counter, rhythm } = p;
    const L = lead.handle;
    const bassWord = /808/.test(bass.source) ? "808" : "bass";
    const twitchCue = G.TWITCH_FROM_KICK
      ? `${G.TWITCH_LABEL || "kick trick"}: ${bare(drums.kick)}, ${drums.twitch.where}`
      : `${G.TWITCH_LABEL || "twitch percussion"}: ${drums.twitch.sound} ${drums.twitch.rhythm}, ${drums.twitch.where}`;
    const slurCue = rhythm.slur ? drums.slur : null;
    const counterLate = /after the switch/.test(counter.role);

    if (p.mode === "loop") {
      const loop = `${L} playing ${bare(lead.articulation)}, ${bare(drums.kick)}, ${bare(drums.hats)}, ${bassWord} ${bass.pattern}`;
      return [
        `[Intro | the loop alone, ${bassWord} and kick only, 2 bars]`,
        `[Loop | same two-bar loop, ${loop}, everything else identical]`,
        `[Loop | same loop, identical, ${twitchCue}${slurCue ? `, ${slurCue}` : ""}]`,
        "[Loop | same loop, identical, hats cut for two bars then back]",
        `[Loop | same loop, ${lead.motion}, drums identical]`,
        `[Loop | same loop, identical, ${drums.feel}]`,
        "[Outro | the loop runs until it stops dead on beat 1]",
        "[End]",
      ].join("\n\n");
    }

    const verse = `[Verse | full drums, ${rhythm.sync}, ${bassWord} ${bass.pattern}${slurCue ? `, ${slurCue}` : ""}, ${twitchCue}]`;
    if (p.mode === "straight") {
      const hook = `[Hook | full beat, ${L} up front, ${lead.motion}, ${counter.handle} ${counter.role}]`;
      return [
        `[Intro | ${L} alone, ${lead.placement.split(",")[0]}]`,
        verse,
        hook,
        `[Verse | same drums, ${L} thinner, ${drums.feel}]`,
        hook,
        `[Outro | drums cut, ${L} alone, ends dry on beat 1]`,
        "[End]",
      ].join("\n\n");
    }

    // Switch mode: one axis, repeated in every section after the switch.
    return [
      `[Intro | ${L} alone, ${lead.placement.split(",")[0]}]`,
      verse,
      `[Hook | full beat, ${L} up front${counterLate ? "" : `, ${counter.handle} ${counter.role}`}]`,
      `[Break | drums stop, ${sw.land}]`,
      `[Beat Switch | ${sw.cue}]`,
      `[Verse | ${sw.after}, ${L} stays low, ${counterLate ? `${counter.handle} enters, ` : ""}${/after the switch/.test(drums.twitch.where) ? "twitch percussion enters" : "twitch percussion in the gaps"}]`,
      `[Hook | ${sw.after}, heavier, ${L} up front, ${lead.motion}]`,
      `[Outro | ${sw.after}, drums cut, ends dry on beat 1]`,
      "[End]",
    ].join("\n\n");
  }

  function generate({ seed = 1, genre = "hiphop", mode, avoidKeys = [] } = {}) {
    const G = grammarFor(genre);
    const r = mulberry32(seed >>> 0);
    const harmony = T.harmony(r, pick, avoidKeys, G.MAJOR_SHARE);
    const lane = weighted(G.LANES.map((l) => [l, l.weight]), r);
    // A lane may carry its own tempo and mode mix (the Prof moods do).
    const [lo, hi] = pick(lane.tempo || G.TEMPO, r);
    const chosenMode = mode || weighted(lane.modes || G.MODES, r);
    const allow = makeAllow(chosenMode);
    const lead = buildLead(r, G, lane, allow);
    const counter = buildCounter(r, G, lead, lane, allow, chosenMode);
    const bass = buildBass(r, G, lead, lane, allow);
    const drums = buildDrums(r, G, lead, allow, chosenMode);
    const sw = buildSwitch(r, G, lead, bass, drums);
    const rhythm = buildRhythm(r, G, chosenMode, lead, sw, allow);
    const p = {
      anchor: pick(lane.anchors, r),
      bpm: int(lo, hi, r),
      key: `in ${harmony.key}`,
      mode: chosenMode,
      lane: lane.id,
      lead, counter, bass, drums, sw, rhythm,
    };
    const styleText = style(p, G);
    const lyricsText = lyrics(p, G, r);
    const warnings = Guard.check(`${styleText}\n${lyricsText}`, G.ALLOW).map((h) => ({ ...h, message: `${h.rule}: "${h.word}" ${h.why}` }));
    const S = G.SLIDERS[chosenMode] || G.SLIDERS.switch;
    const weirdness = int(S.weirdness[0], S.weirdness[1], r);
    const styleInfluence = int(S.styleInfluence[0], S.styleInfluence[1], r);
    const model = chosenMode === "switch" && r() < 0.5 ? "v6-wild" : "v6";
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
      fingerprint: [G.id, chosenMode === "loop" ? "loop" : counter.source, lead.source, lead.character, lead.motif, lead.articulation, lead.human, chosenMode === "switch" ? sw.tag : chosenMode, harmony.key].join("|"),
      parts: { lead, counter, bass, drums, switch: sw, rhythm },
      warnings,
    };
  }

  const genres = () => Object.values(Genres).map((g) => ({ id: g.id, label: g.label, blurb: g.blurb }));

  return { generate, genres, EVERY };
})();

if (typeof module !== "undefined") module.exports = OneEngine;
