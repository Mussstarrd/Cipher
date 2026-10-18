/* ONE BUTTON — the artist's personality layer, the lyrics-field layouts and
 * the seldom "weird" mode. Pure data for the engine, plus one render helper.
 *
 * Rules every phrase obeys (tested by personality-test.js):
 *   - passes src/one/guard.js (so never: waltz, lilt, shuffle, swing, bowed,
 *     folk, harp, phonk, memphis, cowbell, fills, transitions, "no X", names)
 *   - no arrangement verbs: enters, comes in, builds, drops, transition, returns
 *   - the beat-3 cadence is a drunken triplet slur in hip hop language
 *   - "every N bars" appears only where the engine's EVERY cap can see it
 *
 * Layout placeholders (all optional; see `fallbacks` for the null case):
 *   {LEAD}    lead handle ("the slurred harmonica")
 *   {COUNTER} counter handle
 *   {BASS}    "808" or "bass"
 *   {KICK} {HATS} {SNARE}   bare drum sources (engine's bare(), short form)
 *   {SYNC}    a syncopation head tag       {SLUR} a slurCadence phrase
 *   {FEEL}    GROOVE.human phrase          {MOTION} LEAD.motion phrase
 *   {CUE}     switch cue   {AFTER} switch after-words   {LAND} switch landing
 */
"use strict";

const OnePersonality = {
  // ---------------------------------------------------------------- 1. signature
  // A grammar-shaped FAMILY. Every source states its register.
  signature: {
    frame: "sampled, slurred and pitched down over hard trap drums and 808s",
    sources: [
      "a harmonica sample slurred and bent between notes, pitched down into the low mids|the slurred harmonica",
      "a breathy harmonica pitched down an octave, parked under the snare|the low harmonica",
      "a harmonica run through a cranked guitar amp until it barks, kept in the low mids|the amp harmonica",
      "a harmonica resampled down an octave until the grain shows, sitting under the snare|the resampled harmonica",
      "a harmonica time-stretched so every bend smears, low mids, under the snare|the smeared harmonica",
      "a harmonica and erhu stacked an octave apart, both sliding, the low copy louder|the harmonica erhu stack",
      "an erhu sample sliding between every note, pitched down into the mids|the erhu",
      "an erhu bending into each note late from below, pitched down under the snare|the low erhu",
      "an erhu resampled and distorted, the slides smeared, parked in the low mids|the distorted erhu",
      "a sliding two-string fiddle sample pitched down into the low mids, slurred like the erhu|the two-string fiddle",
      "a lap steel through a fuzz pedal sliding between two notes, pitched down under the snare|the fuzz lap steel",
      "a melodica played breathy and slurred, pitched down into the low mids|the melodica",
    ],
    character: [
      "slurring between notes, never landing clean",
      "bent flat into every note by hand",
      "breath noise and reed buzz left in",
      "pitched down until it sounds heavy and sad",
      "run through a cranked amp, barking on the loud notes",
      "resampled until the grain shows",
      "each note sliding in late from below",
      "doubled by a distorted 808 on the root",
    ],
  },
  signatureCounterRoles: [
    "one slurred reply at the end of each phrase",
    "a bent note answering the snare",
    "one slide down under the hook only",
    "a breathy held note in the gaps the lead leaves",
    "two slurred notes on the turnaround, then gone",
    "doubling the last note of each lead phrase, bent flat",
  ],

  // ---------------------------------------------------------------- 2. slur cadence
  // The beat-3 drunken triplet slur. Head tags (<= 7 words) and lyric cues.
  slurCadence: [
    "808 stumbles into three on beat 3",
    "beat 3 slurs, the one lands hard",
    "drunk triplet skip on beat 3",
    "kick slurs three quick on beat 3",
    "beat 3 trips, beat 4 straightens",
    "lazy three-note drag on beat 3",
    "808 tumbles on 3, sober on 1",
    "a slurred triplet lean on beat 3",
    "kick drags into threes on beat 3",
    "third beat slurs, downbeat snaps back",
    "808 trips three steps into beat 4",
    "drunk bounce on 3, straight hats",
    "one slurred triplet per bar, beat 3",
    "the pocket slips drunk on beat 3",
  ],

  // ---------------------------------------------------------------- 3. syncopation, twitch
  syncopation: [
    "kick a 16th ahead of one",
    "808 on the and of 4",
    "kick skipping beat 1",
    "off-grid kick, snare dead center",
    "double kick before each snare",
    "pushed kicks, snare on the grid",
    "kick on the e of 2",
    "snare early, 808 late",
    "kicks stacked off the downbeat",
    "808 skipping the one, snare locked",
    "kick chasing the lead's rhythm",
  ],
  twitch: [
    "nervous perc ticks between kicks",
    "twitching hat clicks, off the grid",
    "jittery 32nd knock bursts",
    "clipped perc blips under the hats",
    "skittering metallic ticks",
    "stop-start perc clicks",
    "retriggered hat ticks in the gaps",
    "twitchy knock flams before the snare",
    "tiny perc blips answering the snare",
    "nervous bitcrushed tick pattern",
    "jittery clicks, never twice the same",
    "shaky hat retriggers under the loop",
  ],

  // ---------------------------------------------------------------- 4. anti-transition
  antiTransition: {
    phrases: [
      "the same loop the whole way",
      "drums cut dead, then slam back on the one",
      "every section identical except one sound",
      "the beat stays frozen from start to finish",
      "hard cuts only, straight in on beat 1",
      "sections change on a hard cut, nothing in between",
      "the loop runs flat through every section",
      "one sound in, one sound out, drums identical",
      "drums stop dead on beat 4, back on beat 1",
      "the hook is the verse with one more sound",
    ],
    // For the Exclude box only. These are the banned words on purpose and are
    // skipped by the guard test (they never go in the style or lyrics field).
    excludeTerms: [
      "drum fills", "tom fills", "snare rolls", "hat rolls", "drum rolls",
      "transitions", "transition fills", "section transitions",
      "risers", "sweeps", "filter sweeps", "white noise sweeps", "build-ups",
      "breakdown FX", "impacts", "crash cymbals", "reverse cymbals",
      "DJ drops", "DJ scratching", "tape stop", "phaser",
      "cowbell", "vocal chops", "vocals", "choir", "humming", "chanting",
    ],
  },

  // ---------------------------------------------------------------- 5. layouts
  // When a placeholder's value is null the engine substitutes the fallback,
  // then collapses ", ," left by an empty one.
  fallbacks: { SLUR: "{FEEL}", COUNTER: "the 808", MOTION: "the lead a hair louder", LAND: "on a hard cut", SYNC: "{FEEL}" },
  // Max rendered length of each placeholder so every line stays under 100
  // chars (the engine picks with pickWhere(x => x.length <= limit)).
  limits: { LEAD: 25, COUNTER: 25, KICK: 22, HATS: 23, SNARE: 20, SYNC: 34, SLUR: 37, FEEL: 44, MOTION: 45, CUE: 79, AFTER: 39, LAND: 55 },
  layouts: [
    {
      id: "bare_loop", label: "Two-bar loop, nothing changes, 2:00", mode: "loop", weight: 3, minimal: 1.0,
      sections: [
        "[Intro | {LEAD} alone, 2 bars]",
        "[Loop | same two bars, {LEAD} over {KICK} and {BASS} only]",
        "[Loop | same loop, identical, {SLUR}]",
        "[Loop | same loop, identical, nothing changes]",
        "[Loop | same loop, identical, {FEEL}]",
        "[Outro | the loop stops dead on beat 1, 2:00]",
        "[End]",
      ],
    },
    {
      id: "one_in_one_out", label: "One sound added, one sound removed", mode: "loop", weight: 2, minimal: 0.8,
      sections: [
        "[Intro | {KICK} and {BASS} only, 4 bars]",
        "[Loop | same loop, {LEAD} added, everything else identical]",
        "[Loop | same loop, {HATS} added, identical]",
        "[Loop | same loop, hats removed, {LEAD} and {BASS} identical]",
        "[Loop | same loop, {LEAD} removed, drums and {BASS} alone]",
        "[Loop | same loop, {LEAD} back, identical]",
        "[Outro | {KICK} and {BASS} only, stops dead on beat 1]",
        "[End]",
      ],
    },
    {
      id: "hook_first", label: "Hook first", mode: "straight", weight: 2, minimal: 0.3,
      sections: [
        "[Hook | full beat from beat 1, {LEAD} up front]",
        "[Verse | same drums, {SYNC}, {SLUR}]",
        "[Hook | identical to the first hook, {LEAD} up front]",
        "[Verse | same drums, {COUNTER} alone, {LEAD} out]",
        "[Hook | identical, {MOTION}]",
        "[Outro | drums cut dead, {LEAD} alone, ends on beat 1]",
        "[End]",
      ],
    },
    {
      id: "verse_bare_hook_full", label: "Verse bare, hook full", mode: "straight", weight: 2, minimal: 0.5,
      sections: [
        "[Intro | {LEAD} alone, 4 bars]",
        "[Verse | {KICK} and {BASS} only, {SLUR}]",
        "[Hook | full drums, {HATS}, {LEAD} up front]",
        "[Verse | {KICK} and {BASS} only again, hats out, {LEAD} thinner]",
        "[Hook | full drums, identical, {MOTION}]",
        "[Outro | {KICK} and {BASS} only, {LEAD} alone on the last bar]",
        "[End]",
      ],
    },
    {
      id: "slow_motif", label: "Slow and sad: long intro on the lead, the motif grows", mode: "straight", weight: 2, minimal: 0.6,
      sections: [
        "[Intro | {LEAD} alone, slow long notes, 8 bars]",
        "[Verse | {BASS} one note per bar, {SNARE} on 3 only, {LEAD} three notes]",
        "[Hook | same density, the motif grows to five notes, {LEAD} still low]",
        "[Verse | same density, identical drums, the motif grows one note more]",
        "[Hook | same density, full motif, {MOTION}]",
        "[Outro | drums cut dead, {LEAD} alone on the full motif, 8 bars]",
        "[End]",
      ],
    },
    {
      id: "halftime_last_third", label: "Half-time drop-down for the last third", mode: "switch", weight: 2, minimal: 0.4,
      sections: [
        "[Intro | {LEAD} alone, 4 bars]",
        "[Verse | full drums, {SYNC}, {SLUR}]",
        "[Hook | full beat, {LEAD} up front, {COUNTER} answering]",
        "[Verse | {LEAD} thinner, {FEEL}]",
        "[Break | drums cut dead, {LAND}]",
        "[Beat Switch | {CUE}]",
        "[Hook | {AFTER}, {MOTION}]",
        "[Outro | {AFTER}, drums cut dead, ends on beat 1]",
        "[End]",
      ],
    },
    {
      id: "a_then_b", label: "A then B, two beats one song", mode: "switch", weight: 2, minimal: 0.3,
      sections: [
        "[Intro | {LEAD} alone, 2 bars]",
        "[Hook | beat A, {SYNC}, {LEAD} up front]",
        "[Verse | beat A, same drums, {SLUR}]",
        "[Beat Switch | {CUE}]",
        "[Hook | beat B, {AFTER}, {LEAD} up front]",
        "[Verse | beat B, {AFTER}, {COUNTER} answering]",
        "[Outro | beat B, {AFTER}, stops dead on beat 1]",
        "[End]",
      ],
    },
    {
      id: "call_response", label: "Call and response, two-bar phrases", mode: "straight", weight: 2, minimal: 0.5,
      sections: [
        "[Intro | {LEAD} two bars, {COUNTER} two bars, drums out]",
        "[Verse | full drums, {LEAD} two bars, then {COUNTER} two bars]",
        "[Hook | full beat, {SLUR}, {LEAD} and {COUNTER} two bars each]",
        "[Verse | same drums, {FEEL}, {LEAD} and {COUNTER} two bars each]",
        "[Hook | identical, {LEAD} two bars, then {COUNTER} two bars]",
        "[Outro | drums cut dead, {LEAD} once, {COUNTER} once]",
        "[End]",
      ],
    },
    {
      id: "kick_and_lead", label: "Stripped to kick and lead, 3 minutes", mode: "loop", weight: 2, minimal: 0.95,
      sections: [
        "[Intro | {LEAD} alone, 2 bars]",
        "[Loop | {KICK} and {LEAD} only, same two-bar loop, 3 minutes]",
        "[Loop | same loop, identical, {SLUR}]",
        "[Loop | same loop, identical, {FEEL}]",
        "[Loop | same loop, {MOTION}, kick identical]",
        "[Outro | {KICK} alone, stops dead on beat 1]",
        "[End]",
      ],
    },
    {
      id: "long_fade", label: "Long outro fade", mode: "straight", weight: 1, minimal: 0.5,
      sections: [
        "[Intro | {LEAD} alone, 4 bars]",
        "[Verse | full drums, {SYNC}, {SLUR}]",
        "[Hook | full beat, {LEAD} up front, {COUNTER} answering]",
        "[Verse | same drums, identical, {FEEL}]",
        "[Hook | identical, {MOTION}]",
        "[Outro | same loop fading out slowly over 32 bars, nothing else changes]",
        "[End]",
      ],
    },
    {
      id: "lead_out_middle", label: "Lead out for 8 bars, slams back", mode: "straight", weight: 1, minimal: 0.6,
      sections: [
        "[Intro | full drums from beat 1, {KICK}, {HATS}]",
        "[Verse | full drums, {LEAD} low, {SLUR}]",
        "[Hook | full beat, {LEAD} up front]",
        "[Break | {LEAD} out, drums and {BASS} alone, 8 bars]",
        "[Hook | {LEAD} slams back on the one, identical to the first hook]",
        "[Outro | drums cut dead, {LEAD} alone, ends on beat 1]",
        "[End]",
      ],
    },
    {
      id: "switch_strip", label: "Drums cut, lead alone, then beat B slams in", mode: "switch", weight: 1, minimal: 0.5,
      sections: [
        "[Intro | {KICK} and {BASS} only, 4 bars]",
        "[Verse | full drums, {SYNC}, {LEAD} low]",
        "[Hook | full beat, {LEAD} up front, {SLUR}]",
        "[Break | {LEAD} alone, {LAND}]",
        "[Beat Switch | {CUE}]",
        "[Verse | {AFTER}, {COUNTER} answering]",
        "[Hook | {AFTER}, {MOTION}]",
        "[Outro | {AFTER}, stops dead on beat 1]",
        "[End]",
      ],
    },
  ],

  // ---------------------------------------------------------------- 6. weird
  weird: {
    share: 0.12,
    model: "v6-wild",
    sliders: { weirdness: [70, 85], styleInfluence: [55, 70] },
    mixTag: "raw clipped mix, loud and dark",
    anchors: [
      "experimental hip hop, dark slowed drums, distorted 808, left-field",
      "left-field trap, slowed and slammed drums, blown-out 808",
      "experimental southern hip hop, drums pitched down, dark",
      "dark experimental trap, slowed, distorted 808, off-grid",
      "left-field hip hop, sliced samples, slammed drums",
      "underground experimental trap, drums slowed and crushed",
    ],
    tags: [
      "bit-mangled lead",
      "drums slowed and slammed",
      "reversed sample stabs",
      "808 blown out and clipped",
      "pitch-warped lead",
      "drums pitched down a step",
      "sliced sample stabs off the grid",
      "hats crushed to grit",
    ],
    families: { found: 4, synth: 3, organ: 2 },
    sources: [
      "a pitch-warped sample stab sliced into one-beat pieces and reshuffled, low mids|the warped stab",
      "a reversed harmonica note resampled into the lead, swelling backward into each beat, low|the reversed harmonica",
      "a bit-mangled lead run through a broken sample-rate reducer, grinding in the low mids|the mangled lead",
      "a soul-record keys sample sliced into stabs, pitched down and dark|the sliced soul keys",
      "a dial-up modem screech tuned down into a melody, thick and low|the modem lead",
      "a slowed-down radio jingle sliced into stabs, pitched down into the mids|the sliced jingle",
    ],
    dreamville: [
      "sliced soul keys over off-grid drums",
      "a pitched-down soul loop under hard drums",
      "warm piano stabs under slammed drums",
      "a soul guitar lick sliced into one bar, dark",
      "muted keys loop, drums slowed and dry",
      "low soul keys stab sliced off the one",
    ],
  },

  // ---------------------------------------------------------------- 7. slow register, minimal register
  ballad: {
    tags: [
      "slow, long notes, sad and low",
      "slow sad hip hop, piano and 808",
      "long held notes under the snare",
      "slow heavy drums, sad low lead",
      "quiet piano, hard snare, slow",
      "sad low harmonica, long notes",
      "slow and heavy, bare and low",
      "whole notes over slow drums",
    ],
    sources: [
      "a dark low piano playing long single notes, each one left to ring, under the snare|the low piano",
      "a clean electric guitar playing long bent notes, low and sad|the sad guitar",
      "a harmonica pitched down, breathy whole notes slurred into each other, under the snare|the slow harmonica",
      "an erhu sliding slowly into each long note, pitched down into the low mids|the slow erhu",
      "a detuned piano playing two chords per bar, slow and heavy, low|the slow piano",
      "a baritone guitar picked slow, one note per bar, left to ring, low|the slow baritone guitar",
    ],
  },
  minimal: [
    "two sounds only",
    "kick, 808 and one lead",
    "bare bones, nothing else",
    "stripped to the bone",
    "one loop, three sounds",
    "empty space between every hit",
    "so sparse the room shows",
    "bare drums, one-note lead",
  ],
};

// The short drum word for {KICK} {HATS} {SNARE}: the words up to and including
// the drum noun. "a syncopated double kick before the snare" -> "syncopated double kick".
OnePersonality.drumWord = function drumWord(source) {
  const m = source.replace(/^an? /, "").match(/^(.*?\b(kick|hats?|snare|clap|shaker|snap)s?)\b/i);
  return m ? m[1] : source.replace(/^an? /, "").split(",")[0];
};

// Render a layout with a values map. Null values take `fallbacks`; a
// placeholder with nothing left is dropped and the comma list is tidied.
OnePersonality.render = function render(layout, values) {
  const F = OnePersonality.fallbacks;
  const resolve = (key, depth = 0) => {
    const v = values[key];
    if (v) return v;
    const fb = F[key];
    if (!fb || depth > 3) return "";
    const m = fb.match(/^\{(\w+)\}$/);
    return m ? resolve(m[1], depth + 1) : fb;
  };
  return layout.sections
    .map((s) => s.replace(/\{(\w+)\}/g, (_, k) => resolve(k)).replace(/,\s*(?=,|\])/g, "").replace(/\|\s*,/, "|").replace(/\bidentical, identical\b/g, "identical"))
    .join("\n\n");
};

if (typeof module !== "undefined") module.exports = OnePersonality;
