const data = require("./genres-data.js");
const J = (v) => JSON.stringify(v);
const bpm = (list) => list.map((s) => s.match(/(\d+)-(\d+)/).slice(1, 3).map(Number));
const fams = (f, frame) => {
  const out = {};
  for (const [k, v] of Object.entries(f)) out[k.replace(/\s+/g, "_")] = { frame, sources: v.src, character: v.ch };
  return out;
};
const [A, B, C] = data;
const norm = (o) => Object.fromEntries(Object.entries(o).map(([k, v]) => [k.replace(/\s+/g, "_"), v]));
const lanesOf = (id, label, d, extra) => d.lanes
  ? d.lanes.map((l) => ({ ...l, families: norm(l.families), weight: l.weight || 1, bass: l.bass || [] }))
  : [{ id, label, weight: 1, anchors: d.anchors, families: extra.families, bass: [] }];
const pack = (id, label, blurb, d, extra) => `  ${id}: {
    id: "${id}",
    show: false,
    label: "${label}",
    blurb: "${blurb}",
    grammar: {
      TEMPO: ${J(bpm(d.bpm))},
      MAJOR_SHARE: ${extra.major},
      GUEST_SHARE: ${extra.guest == null ? 0.3 : extra.guest},
      GUEST_FRAME: "${extra.guestFrame || extra.mix.split(",")[0].replace(/ mix$/, "")}",
      MIX_TAG: "${extra.mix}",${extra.vocal ? "\n      VOCAL: true," : ""}${extra.model ? `\n      MODEL: "${extra.model}",` : ""}
      DRUMS_TAG: "${extra.drumsTag}",
      MODES: ${J(extra.modes)},
      SLUR_SHARE: ${extra.slur},
      LOW_SHARE: ${extra.low},
      SLIDERS: ${J(extra.sliders)},
      EXCLUDE: [...new Set(BASE_EXCLUDE.concat(${J(d.exclude.split(", "))}))],
      LANES: ${J(lanesOf(id, label, d, extra))},${d.allow ? `
      ALLOW: /${d.allow}/i,` : ""}
      FAMILIES: ${J(fams(d.families, extra.frame))},
      BASS: { source: ${J(d.bass)}, tone: ${J(d.btone)}, pattern: Base.BASS.pattern, human: Base.BASS.human },
      DRUMS: { kick: ${J(d.kick)}, snare: ${J(d.snare)}, hats: ${J(d.hats)} },
      COLOR: ${J(d.color)},
      GROOVE: { sync: ${J(d.sync)}, human: ${J(d.human)}, triplet: ${extra.triplet} },
      LEAD: { ...Base.LEAD, motif: ${J(d.motif)}, articulation: ${J(d.artic)}${extra.register ? `, register: ${extra.register}` : ""} },${extra.twitchFromKick ? "\n      TWITCH_FROM_KICK: true," : ""}
      RHYTHM: { ...Base.RHYTHM, sync: ${extra.sync || J(d.sync)}, twitch: ${extra.twitch} },${extra.noCounter ? "\n      NO_COUNTER: true," : ""}${extra.twitchLabel ? `\n      TWITCH_LABEL: "${extra.twitchLabel}",` : ""}${extra.twitchPool ? `\n      TWITCH: ${extra.twitchPool},` : ""}
      SWITCH: ${extra.switch},
      STRUCTURE: ${extra.structure},
    },
  },`;


// Spin-offs delivered as standalone files with lanes and an allow list.
const FILE_PACKS = [
  { file: "./genre-prof.js", label: "Prof", blurb: "Two moods in one button: a crawl through a low tide (drunk piano, dark organ, muddy sliding bass, slurred triplets) or good time boy (stomping claps, huge saw bass, siren hooks, horn stabs).",
    extra: {
      major: 0.25, mix: "loud bar-jukebox mix, bass huge, drums cracking", drumsTag: "bar-fight stomp drums and a huge bass", slur: 0.6, low: 0.6, guest: 0.3, guestFrame: "over heavy Midwest hip hop drums and a huge bass",
      modes: [["switch", 35], ["loop", 25], ["straight", 40]],
      sliders: { switch: { weirdness: [45, 60], styleInfluence: [65, 78] }, loop: { weirdness: [45, 55], styleInfluence: [70, 78] }, straight: { weirdness: [45, 58], styleInfluence: [65, 78] } },
      triplet: "Base.GROOVE.triplet",
      twitch: "Base.RHYTHM.twitch",
      switch: `{
        axes: [
          { tag: "beat switch into a crawl", cue: "the drums go half-time, the tempo halves into a crawl, long 808 tails, the lead slow and slurred", after: "half-time crawl, the lead slurred, bass dragging" },
          { tag: "beat switch into a stomp", cue: "the drums double into a stomp, gang claps and boot stomps on 2 and 4, the bass turns huge and distorted", after: "double-time stomp, claps on 2 and 4, bass huge" },
          { tag: "beat switch, key falls", cue: "key falls a minor third, the 808 tails longer, the lead pitched down and slurred", after: "in the lower key, the lead pitched down" },
        ],
        land: Base.SWITCH.land,
      }`,
      structure: function (p) {
        if (p.mode === "loop") return null;
        const H = p.lead.handle, c = p.counter, sw = p.sw, low = p.lane === "lowtide";
        const full = low ? "the bass dragging, 808 tails ringing" : "stomping claps, the bass huge";
        const lines = [
          `[Intro | ${H} alone, ${low ? "dark and slurred" : "dry and loud"}, 4 bars]`,
          `[Verse | ${p.drums.kick.replace(/^an? /, "")}, 808 ${p.bass.pattern}, ${H} ${low ? "low and behind the beat" : "low, stabbing with the kick"}, ${p.drums.sync}]`,
          `[Hook | full beat, ${H} up front, ${full}, ${c.handle} ${c.role}]`,
          `[Verse 2 | ${H} thinned to one chord per bar, ${p.drums.feel}]`,
          `[Hook | full beat, ${H} up front, ${full}, ${p.lead.motion}]`,
        ];
        if (p.mode === "switch") lines.push(`[Break | drums cut, ${H} and 808 only, ${sw.land}]`, `[Beat Switch | ${sw.cue}]`, `[Hook | ${sw.after}, ${H} up front]`, `[Outro | ${sw.after}, ${H} alone, ends on one sour held chord]`);
        else lines.push(`[Outro | drums cut, ${H} alone, ends on one ${low ? "sour held chord" : "hard stab"}]`);
        lines.push("[End]");
        return j(lines);
      },
    } },
  { file: "./genre-bare.js", label: "Bare minimum", blurb: "One toy voice, one dumb-simple riff, kick and snare. A single kick stumbles early before the snare and the snare cracks it back onto the beat.",
    extra: {
      major: 0.35, mix: "bare, dry, loud, nothing extra", drumsTag: "bare kick and snare with one kick trick", slur: 0.15, low: 0, guest: 0.25, guestFrame: "over bare kick and snare",
      modes: [["loop", 50], ["straight", 50]],
      sliders: { switch: { weirdness: [50, 65], styleInfluence: [70, 80] }, loop: { weirdness: [50, 60], styleInfluence: [72, 80] }, straight: { weirdness: [50, 65], styleInfluence: [70, 80] } },
      triplet: '["only the kick stutter leans off the grid", "the stutter kick a hair early, the snare dead on", "one late double kick, then straight"]',
      twitch: '["one stuttered double kick before the snare", "a single off-beat kick stutter", "a kick that trips into the snare", "off-beat double kick, snare cracks it back", "one kick stutter per bar", "a stutter kick a 16th before the snare"]',
      sync: '["kick slightly off, snare dead on", "bare drums, one kick trick", "kick and snare only, locked", "the snare pulls the kick back on beat", "one double kick, everything else plain"]',
      noCounter: true,
      twitchLabel: "kick trick",
      twitchFromKick: true,
      register: '["mid register, narrow range", "mid register, never above the snare\'s crack", "mid register, plain, one octave only", "mid register, thin, the sub far below"]',
      twitchPool: '{ sound: ["the stutter kick"], rhythm: ["doubling once a 16th before the snare", "tripping in just before the 2", "landing a hair early before the 4", "hitting twice, quick, right before the snare", "skipping the one and stuttering into the snare"], where: ["once per bar", "every other bar", "only in the hook", "on every snare", "once per bar, never in the same spot"] }',
      switch: "Base.SWITCH",
      structure: function (p) {
        if (p.mode === "loop") return null;
        const H = p.lead.handle;
        return j([
          `[Intro | ${H} alone, dry, 4 bars]`,
          `[Verse | kick and snare, ${p.drums.kick.replace(/^an? /, "")}, sub on the root, ${H} plain]`,
          `[Hook | same bare drums, ${H} a little louder, ${p.lead.motion}]`,
          `[Verse | same bare drums, ${H} leaves one note out, ${p.drums.feel}]`,
          `[Hook | same bare drums, ${H} a little louder]`,
          `[Outro | drums cut, ${H} alone, ends dry on beat 1]`,
          "[End]",
        ]);
      },
    } },
  { file: "./genre-pulse.js", label: "Bass is the drums", blurb: "Take the kit out and let the 808 do its job. Drumless loop, Running 808 (Detroit stop-start), or Trapdoor: a one-bar hole where everything cuts but the 808, then slams back on the one.",
    extra: {
      major: 0.15, mix: "loud dry mix, 808 up front, open mids", drumsTag: "the 808 as the pulse, a slapping snare, kick sparse", slur: 0.4, low: 0.85, guest: 0.15, guestFrame: "over an 808 pulse with the kit mostly out",
      modes: [["straight", 45], ["switch", 35], ["loop", 20]],
      sliders: { switch: { weirdness: [45, 58], styleInfluence: [72, 82] }, loop: { weirdness: [45, 55], styleInfluence: [75, 82] }, straight: { weirdness: [45, 58], styleInfluence: [72, 82] } },
      triplet: '["the 808 slurs into a triplet on beat 3, then runs straight", "the stabs stumble three quick hits on beat 3", "beats 1 and 2 straight, beat 3 slurred into three", "a drunk triplet 808 bounce on the third beat"]',
      twitch: '["sour stabs off the grid", "stabs landing late behind the snare", "stop-start stabs", "a one-bar hole then a slam on the one", "the 808 sits out beat 1 every other bar", "a pulse that cuts out mid-bar", "a stick click on 4, nothing else"]',
      sync: '["808 running the melody, kick sparse", "snare slapping dead on 2 and 4, stabs off the grid", "kick only on the one, the 808 carries the pulse", "the 808 anticipating the downbeat by a 16th", "the beat stops dead for a bar, then slams back on the one", "the 808 as the only pulse, a click on 4"]',
      twitchLabel: "808 twitch",
      twitchPool: '{ sound: ["the 808"], rhythm: ["doubling once a 16th before the snare", "stuttering twice into the next note", "cutting out for half a beat", "sliding late into the downbeat", "retriggering on the and of 4"], where: ["once per bar", "every other bar", "only in the hook", "on the last bar of each 8", "whenever the melody changes note"] }',
      model: "v6",
      switch: "Base.SWITCH",
      structure: function (p) {
        const H = p.lead.handle, c = p.counter, sw = p.sw, lane = p.lane;
        const running808 = /808|bass|sub/.test(p.lead.handle);
        const hole = "the beat stops dead for one bar, 808 line alone, slams back on the one";
        const pulse = lane === "drumless"
          ? "the 808 the only pulse, a stick click on 4"
          : `808 running the melody in 8ths, ${p.drums.kick.replace(/^an? /, "")}, ${p.drums.snare.replace(/^an? /, "")}`;
        const leadDoes = lane === "drumless" ? `${H} looping low, open mids` : running808 ? `${H} running 8ths, ${c.handle} stabbing off the grid` : `${H} stabbing off the grid`;
        const counter = p.noCounter || running808 ? "" : `, ${c.handle} ${c.role}`;
        if (p.mode === "loop") {
          return j([
            `[Intro | ${H} alone, dry, 4 bars]`,
            `[Loop | same two-bar loop, ${pulse}, ${leadDoes}, everything else identical]`,
            `[Loop | same loop, identical, ${p.drums.sync}]`,
            `[Loop | same loop, identical, ${hole}]`,
            `[Loop | same loop, ${p.lead.motion}, the pulse identical]`,
            `[Loop | same loop, identical, ${p.drums.feel}]`,
            `[Outro | the 808 line alone, stops dead on beat 1]`,
            "[End]",
          ]);
        }
        const hookHole = lane === "trapdoor" ? `, ${hole}` : "";
        const lines = [
          `[Intro | ${H} alone${lane === "drumless" ? ", the seam audible" : " over the running 808, dry"}, 4 bars]`,
          `[Verse | ${pulse}, ${leadDoes}, ${p.drums.sync}]`,
          `[Hook | same pulse, the 808 louder, ${H} up front${hookHole}${counter}]`,
          `[Verse 2 | same pulse, ${H} leaves half its notes out, ${p.drums.feel}]`,
          `[Hook | same pulse, ${H} up front, ${hole}]`,
        ];
        if (p.mode === "switch") lines.push(
          `[Break | everything cuts, the 808 line alone for one bar, ${sw.land}]`,
          `[Beat Switch | ${sw.cue}]`,
          `[Hook | ${sw.after}, ${H} up front, ${p.lead.motion}${hookHole}]`,
          `[Outro | the 808 line alone, stops dead on beat 1]`,
        );
        else lines.push(`[Outro | the 808 line alone, stops dead on beat 1]`);
        lines.push("[End]");
        return j(lines);
      },
    } },
  { file: "./genre-radio.js", label: "Radio hit", blurb: "A vocal record on radio's clock: the hook inside twenty seconds, a quotable first line, a pre-hook lift, the biggest hook last. Melodic rap or late-night R&B, lyrics written fresh each roll.",
    extra: {
      major: 0.55, mix: "glossy radio-ready mix, voice up front, wide and clean", drumsTag: "glossy trap-soul radio drums", slur: 0.15, low: 0.3, guest: 0.1, guestFrame: "over a glossy radio beat",
      modes: [["straight", 75], ["switch", 25]],
      sliders: { switch: { weirdness: [30, 45], styleInfluence: [65, 80] }, loop: { weirdness: [30, 45], styleInfluence: [65, 80] }, straight: { weirdness: [30, 45], styleInfluence: [65, 80] } },
      triplet: '["a soft triplet lean on the last beat of each bar", "the snap a hair late, hats straight", "a triplet flow pocket under the verse"]',
      twitch: '["light hat stutters into the hook", "one hat stutter before each hook", "a clipped hat flick on the and of 4", "a stuttered clap before the snare"]',
      sync: '["kick on 1 and the and of 2, snap on 3", "a half-time snap under 16th hats", "kicks pushed off the beat, snap dead on", "the 808 anticipates the chord change by a 16th"]',
      model: "v6",
      vocal: true,
      switch: `{
        axes: [
          { tag: "beat switch to half-time for the last hook", cue: "half-time drums, the 808 longer, the keys darker, the voice lower", after: "half-time, 808 longer" },
          { tag: "beat switch, key falls on the bridge", cue: "key falls a minor third, drums thinned, the voice alone with the keys", after: "in the lower key, drums thinned" },
        ],
        land: Base.SWITCH.land,
      }`,
      structure: function (p) { return null; },
    } },
  { file: "./genre-newwave.js", label: "New wave", blurb: "What the under-25 crowd streams in 2026. Rage (clipped 808s, detuned saw riffs, drums cutting dead), Jerk (pitched-up sliced loops, stuttering snares rushing ahead, elastic bass), Pain (dark low piano or picked guitar, long 808 slides, triplet hats).",
    extra: {
      major: 0.15, mix: "crisp punchy radio mix, 808 loud", drumsTag: "modern trap drums, hard and clipped", slur: 0.35, low: 0.8, guest: 0.15, guestFrame: "over hard modern trap drums",
      modes: [["straight", 45], ["loop", 30], ["switch", 25]],
      sliders: { switch: { weirdness: [40, 55], styleInfluence: [70, 82] }, loop: { weirdness: [40, 50], styleInfluence: [72, 82] }, straight: { weirdness: [40, 55], styleInfluence: [70, 82] } },
      triplet: "Base.GROOVE.triplet",
      twitch: "Base.RHYTHM.twitch",
      switch: "Base.SWITCH",
      structure: function (p) { return null; },
    } },
  { file: "./genre-space.js", label: "Spaceship", blurb: "The Houston psychedelic melodic-trap lane: sliding distorted 808s, detuned synth leads soaked in a long dark hall, pitched-up guitar loops with the seam audible, analog mono leads with slow portamento, half-time snares, sparse open hats.",
    extra: {
      major: 0.1, mix: "crisp punchy radio mix, 808 loud, wide and dark", drumsTag: "half-time trap drums with sparse open hats", slur: 0.35, low: 0.75, guest: 0.15, guestFrame: "over half-time trap drums and sliding 808s",
      modes: [["switch", 40], ["loop", 30], ["straight", 30]],
      sliders: { switch: { weirdness: [40, 55], styleInfluence: [70, 82] }, loop: { weirdness: [40, 50], styleInfluence: [72, 82] }, straight: { weirdness: [40, 55], styleInfluence: [70, 82] } },
      triplet: "Base.GROOVE.triplet",
      twitch: "Base.RHYTHM.twitch",
      switch: "Base.SWITCH",
      structure: function (p) { return null; },
    } },
];
const extraPacks = () => FILE_PACKS.filter((f) => { try { require(f.file); return true; } catch { return false; } })
  .map((f) => { const d = require(f.file); return pack(d.id, f.label, f.blurb, d, { ...f.extra, families: Object.values(d.lanes ? {} : {}), frame: f.extra.frame || "" }); })
  .join("\n");

const out = `/* ONE OF ONE — genre packs. Each pack overrides whole sections of the base
 * grammar (src/one/grammar.js). The base is the syncopated experimental
 * hip hop engine; the spin-offs carry their own instruments, drums, groove,
 * structure, sliders and exclusions. Generated from the producer advisory
 * (scratchpad/genres-data.js) and edited by hand. */
"use strict";

const OneGenres = (() => {
  const Base = typeof OneGrammar !== "undefined" ? OneGrammar : require("./grammar.js");
  const BASE_EXCLUDE = Base.EXCLUDE;
  const P = typeof OnePersonality !== "undefined" ? OnePersonality : require("./personality.js");
  const j = (lines) => lines.join("\\n\\n");

  return {
${require("./gen-one.js")}
  hiphop: {
    id: "hiphop",
    show: false,
    label: "Syncopated hip hop",
    blurb: "Twitchy, minimal, one-of-one instrumentals. Beat switch, hypnotic loop, or one locked pocket.",
    grammar: {},
  },
${pack("pop", "R&B radio pop", "Glossy radio pop R&B with a hole in the mids for a guest verse. Verse, pre-hook, hook.", A, {
  major: 0.6, mix: "glossy radio-ready mix, wide and clean", drumsTag: "glossy pop drums, soft snap, 3-3-2 kick", modes: [["straight", 100]], slur: 0, low: 0.4, guest: 0.2, guestFrame: "over a glossy pop R&B beat",
  sliders: { straight: { weirdness: [30, 45], styleInfluence: [70, 85] }, switch: { weirdness: [30, 45], styleInfluence: [70, 85] }, loop: { weirdness: [30, 45], styleInfluence: [70, 85] } },
  families: { glossy_keys: 3, tropical_plucks: 3, "trap-soul_guitar": 3 }, frame: "over a glossy pop R&B beat",
  triplet: '["a soft triplet lean on the last beat of each bar", "the snap a hair late, hats straight", "a half-time snap under straight 16th hats"]',
  twitch: '["light hat stutters into the hook", "a subtle percussion twitch under the verse", "clipped hat flicks on the and of 4", "one hat stutter before each hook"]',
  switch: "Base.SWITCH",
  structure: `(p) => {
        const H = p.lead.handle, c = p.counter;
        return j([
          \`[Intro | \${H} alone, filtered dark, 4 bars]\`,
          \`[Verse | sparse, \${p.drums.snare.replace(/^an? /, "")}, sub, \${H} low, \${p.drums.sync}]\`,
          \`[Pre-Hook | hats enter, bass opens up, \${H} climbing, \${c.handle} \${c.role}]\`,
          \`[Hook | full beat, \${H} up front, wide and glossy, \${p.lead.motion}]\`,
          \`[Verse 2 | \${H} thinned, 808 sliding, \${p.drums.feel}]\`,
          \`[Hook | full beat, \${H} up front, wide and glossy]\`,
          \`[Feature Verse | drums and 808 only, \${H} one note per bar, open mids, room for a guest verse]\`,
          \`[Hook | full beat, last pass, brightest]\`,
          \`[Outro | \${H} alone, ends dry]\`,
          "[End]",
        ]);
      }`,
})}
${pack("soul", "Soul / emo hip hop", "Sample-born keys or sad guitar over a loose played-in pocket, a buzzing 808, one hook. Chicago-soul and emo flavors.", B, {
  major: 0.4, mix: "warm punchy mix, drums up front", drumsTag: "loose live-feeling soul drums and a buzzing 808", modes: [["switch", 60], ["loop", 20], ["straight", 20]], slur: 0.4, low: 0.5, guest: 0.3, guestFrame: "over a loose played-in hip hop pocket",
  sliders: { switch: { weirdness: [45, 60], styleInfluence: [65, 75] }, loop: { weirdness: [45, 55], styleInfluence: [70, 75] }, straight: { weirdness: [45, 60], styleInfluence: [65, 75] } },
  families: { soul_keys: 3, sad_guitar: 3, soul_sample: 2 }, frame: "over a loose played-in hip hop pocket",
  triplet: 'Base.GROOVE.triplet',
  twitch: '["hats that stutter then fall back to 8ths", "a nervous hat flick before the snare", "twitchy percussion low in the mix", "stuttering snare retriggers on bar 4"]',
  switch: `{
        axes: [
          { tag: "beat switch, drums go live", cue: "the drums go live-sounding and loose, the key falls a minor third, the 808 buzzing louder", after: "in the lower key, drums loose and wide" },
          { tag: "beat switch to half-time", cue: "half-time drums, lazy snare on 3, long buzzing 808 slides, the sample pitched down", after: "half-time, lazy snare, sample pitched down" },
          { tag: "second half double-time", cue: "double-time hats, kick pattern doubles, the sample sliced to one hit per beat", after: "double-time, sample sliced per beat" },
        ],
        land: Base.SWITCH.land,
      }`,
  structure: `(p) => {
        if (p.mode !== "switch") return null;
        const H = p.lead.handle, c = p.counter, sw = p.sw;
        return j([
          \`[Intro | \${H} alone, grainy, 4 bars]\`,
          \`[Verse | \${p.drums.kick.replace(/^an? /, "")}, \${p.drums.hats.replace(/^an? /, "")}, 808 \${p.bass.pattern}, \${H} low, \${p.drums.sync}]\`,
          \`[Hook | full beat, \${H} up front, bright, \${c.handle} \${c.role}]\`,
          \`[Verse 2 | \${H} thinned, 808 sliding, \${p.drums.feel}]\`,
          \`[Break | drums cut, \${H} and 808 only, \${sw.land}]\`,
          \`[Beat Switch | \${sw.cue}]\`,
          \`[Hook | \${sw.after}, \${H} \${p.lead.motion}]\`,
          \`[Outro | \${sw.after}, \${H} alone, ends on one held chord]\`,
          "[End]",
        ]);
      }`,
})}
${pack("boombap", "Boom bap soul mashup", "A sped-up soul record sliced into stabs on a cracking 12-bit snare and a boom kick, with a modern 808 under it all.", C, {
  major: 0.3, mix: "knocking drums, sample bright, 808 heavy, clean mix", drumsTag: "knocking boom bap drums, 12-bit snare, modern 808", modes: [["switch", 55], ["loop", 25], ["straight", 20]], slur: 0.5, low: 0.5, guest: 0.3, guestFrame: "over knocking boom bap drums and a modern 808",
  sliders: { switch: { weirdness: [40, 55], styleInfluence: [70, 80] }, loop: { weirdness: [40, 50], styleInfluence: [75, 80] }, straight: { weirdness: [40, 55], styleInfluence: [70, 80] } },
  families: { soul_sample_flip: 5, soul_keys: 2, sampled_horns: 2 }, frame: "over knocking boom bap drums and a modern 808",
  triplet: 'Base.GROOVE.triplet',
  twitch: 'Base.RHYTHM.twitch',
  switch: `{
        axes: [
          { tag: "beat switch, new drums", cue: "new drums, harder and louder, the sample pitched up a half step, 808 heavier", after: "new drums, sample a half step up" },
          { tag: "beat switch to half-time", cue: "half-time drums, the sample stretched, long 808 slides", after: "half-time, sample stretched" },
          { tag: "beat flips at the midpoint", cue: "the sample re-sliced to one hit per beat, kick pattern flips to the ands", after: "re-sliced sample, kicks on the ands" },
        ],
        land: Base.SWITCH.land,
      }`,
  structure: `(p) => {
        if (p.mode !== "switch") return null;
        const H = p.lead.handle, c = p.counter, sw = p.sw;
        return j([
          \`[Intro | \${H} alone, filtered, 4 bars]\`,
          \`[Verse | \${p.drums.kick.replace(/^an? /, "")}, \${p.drums.snare.replace(/^an? /, "")}, 808 \${p.bass.pattern}, \${H} low, \${p.drums.sync}]\`,
          \`[Hook | full beat, \${H} up front, the 808 loud, \${c.handle} \${c.role}]\`,
          \`[Verse 2 | \${H} re-sliced, hats doubled, \${p.drums.feel}]\`,
          \`[Break | drums cut, \${H} and 808 only, \${sw.land}]\`,
          \`[Beat Switch | \${sw.cue}]\`,
          \`[Hook | \${sw.after}, \${H} \${p.lead.motion}]\`,
          \`[Outro | \${sw.after}, \${H} alone, ends on a hard cut]\`,
          "[End]",
        ]);
      }`,
})}
${extraPacks()}
  };
})();

if (typeof module !== "undefined") module.exports = OneGenres;
`;
require("fs").writeFileSync(require("path").join(__dirname, "../../src/one/genres.js"), out);
console.log("written", out.length);
