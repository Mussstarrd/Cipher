/* Shared sound bank for the instrumental engines (ANOMALY, MANHOLE).
 *
 * Two findings from listening tests shape this file:
 * 1. Suno reads the style field mostly as tags. Short rhythm tags at the very
 *    front ("twitchy stuttering hi-hats", "glitchy percussion") steer it;
 *    bar-by-bar instructions buried later in the field mostly do not.
 * 2. A bare instrument name ("dark keys", "warm bassline") renders as a stock
 *    preset. Every sound here is one vivid phrase that carries its own vibe.
 *
 * Every phrase must pass both engines' lints: no voices, no FX words, no
 * film-score/world/lo-fi words, no meter words. */
"use strict";

const SharedVibe = {
  // Front-loaded rhythm tags. Each roll takes one from each group.
  GLITCH_TAGS: [
    "glitchy percussion",
    "glitchy chopped drums",
    "bitcrushed glitch percussion",
    "clicky glitch percussion",
    "stuttering glitch drums",
  ],
  HAT_TAGS: [
    "twitchy stuttering hi-hats",
    "rapid-fire hat rolls that freeze mid-bar",
    "skittering triplet hi-hats",
    "nervous ratcheting hi-hats",
    "jittery 32nd-note hat bursts",
  ],
  SYNC_TAGS: [
    "heavy syncopation",
    "off-grid stumbling kicks",
    "stop-start rhythm",
    "kicks dodging the downbeat",
    "lopsided syncopated groove",
  ],
  // Low end, one vivid phrase each.
  BASS: [
    "gooey slime-thick 808 slides",
    "rubber-band bouncing sub bass",
    "trunk-rattling knocking 808 glides",
    "growling overdriven 808 grunts",
    "tight pitched 808 runs",
    "elastic gliding sub drops",
    "fat wobbling detuned sub",
    "punch-drunk stumbling bassline",
    "syrup-heavy sliding 808",
    "buzzing fuzz-bass stabs",
    "snapping rubbery synth bass",
    "chest-caving clipped 808 hits",
  ],
  // Lead and melody sounds, one vivid phrase each. Piano appears once.
  LEADS: [
    "gooey detuned saw lead",
    "glassy icepick bell synth",
    "woozy pitch-wobbling keys loop",
    "crunchy bitcrushed brass stab",
    "razor-thin plucked synth line",
    "smeared chorus-drenched guitar loop",
    "sticky rubber-band synth pluck",
    "seasick vibrato synth line",
    "neon-sizzled square-wave lead",
    "syrupy slowed-down electric keys",
    "chrome-plated synth-brass stabs",
    "warped tape-stretched keys loop",
    "buzzsaw distorted 808 melody",
    "sour detuned bell loop",
    "slinky muted guitar riff",
    "fizzy overdriven square lead",
    "honey-thick melting electric keys",
    "spiky staccato synth stabs",
    "icy pitched-down piano stabs",
    "wobbly chorus-soaked synth pad",
    "bent-note gliding portamento lead",
    "metallic ringing bell stabs",
  ],
  // Glitchy side percussion, one vivid phrase each.
  PERC: [
    "clicky glitch percussion ticks",
    "bitcrushed snare stutters",
    "metallic clanking off-beat percussion",
    "rubbery knocking side percussion",
    "tight crunchy finger snaps",
    "chopped stuttering clap edits",
    "skittering off-grid shaker",
    "dry popping wood-block ticks",
    "crunchy reversed snare swells",
    "squeaky elastic percussion hits",
  ],
};

if (typeof module !== "undefined") module.exports = SharedVibe;
