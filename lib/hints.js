'use strict';
/**
 * Hints: one extra, strange ingredient per roll that keeps a record from sounding like the
 * preset. Four families, each a physical description with a handle, never a label:
 *
 *   fuzzlament   a mournful fuzz-and-wah guitar lament with tape echo trailing behind it
 *   machineglitch an industrial, bit-reduced, skipping-disc machine texture
 *   staxtwitch   nervous horn jabs, guitar chucks and tambourine bursts recorded hot to tape
 *   hits         a uniquely described piano or string HIT STRUCTURE (a rhythmic cell, how it
 *                is struck and damped)
 *
 * A hint takes the counter's seat about half the time (lane.hintShare). It gets a short tag in
 * the head ("the crying fuzz guitar, faint"), a `hint:` line in the Style field after the drums
 * line, and its handle reaches the Lyrics tags through {HINT}. The records these were learned
 * from are named only in research/06-addictive-sound-theory.md.
 *
 * HOOK devices (from earworm research: short contour, one unusual interval, repetition with one
 * change, an ending left unresolved, a gap right before the hook) go into the lead line.
 */

const HINT_FAMILIES = {
  fuzzlament: {
    frame: 'a slow guitar lament over the beat',
    sources: [
      'a fuzz guitar wailing one long bent note, tape echo trailing behind it out of time|the crying fuzz guitar',
      'an electric guitar through a fuzz pedal and a half-open wah, one slow bend per bar, feedback swelling into the next|the wah lament',
      'a reversed fuzz-guitar swell that blooms into each downbeat and dies before the snare|the reversed fuzz swell',
      'a lone fuzzed guitar line played slow and mournful, the echo repeats louder than the notes|the mourning guitar',
      'a guitar bend held until it breaks into feedback, then let fall, tape echo smearing the fall|the feedback fall',
      'a fuzz guitar playing two notes a minor third apart, each held for a bar and bent flat at the end|the two-note lament',
      'a phased fuzz guitar hanging one note over the whole hook, the phase moving slower than the bar|the hanging fuzz note',
    ],
    character: [
      'tape echo set long, the repeats decaying out of time',
      'the fuzz cranked until the note blooms into feedback',
      'bent flat and held until the sustain breaks up',
      'played slow over the beat like a funeral, never on the grid',
      'the wah barely moving, half open',
      'mixed under the drums, the echo louder than the note',
      'one take, the string squeaks and amp buzz left in',
      'pitched down a whole step so it sounds heavy and sad',
    ],
    roles: [
      'a faint hint under the hook only',
      'one long note before the last hook, then gone',
      'crying in the gaps the lead leaves',
      'low in the mix through the verse, up front for the last eight bars',
      'answering the lead once per phrase with a single bend',
    ],
    tagWord: 'faint',
  },
  machineglitch: {
    frame: 'an industrial machine texture cut into the beat',
    sources: [
      'a distorted guitar chord sliced into a skipping-disc stutter, retriggering off the grid|the skipping chord',
      'a machine-shop clank sampled and tuned to the key, hard-clipped on every hit|the tuned clank',
      'a bit-reduced industrial hit that repeats like a damaged disc, three skips then silence|the skipping-disc hit',
      'a detuned guitar riff crushed to a grinding digital edge, slowed to half speed|the grinding riff',
      'a short noise burst gated into a mechanical rhythm, cold and dry|the gated noise',
      'a pitch-dropped sample stab mangled through a ring modulator, metallic and wrong|the ring-mod stab',
      'a drum loop resampled through a broken converter, the bits splintering on the snare|the splintered loop',
      'a hydraulic hiss and a steel door slam sampled and placed as percussion, tuned down|the steel door',
    ],
    character: [
      'bit-reduced until the edges splinter',
      'retriggered off the grid like a skipping disc',
      'ring-modulated to a cold metallic edge',
      'hard-clipped on every hit, the tail cut dead',
      'the pitch falling on each repeat like a dying motor',
      'gated in uneven bursts, never twice the same',
      'resampled at a low rate so the top end turns to grit',
      'detuned a quarter tone against the key',
    ],
    roles: [
      'a glitch in the gaps between the lead\'s phrases',
      'twitching in and out under the hook, never on the one',
      'one appearance before the switch, then gone',
      'quietly through the verse, slamming in on the last hook',
      'answering the snare with one skip per bar',
    ],
    tagWord: 'glitching',
  },
  staxtwitch: {
    frame: 'recorded hot to tape over the beat',
    sources: [
      'a trumpet and baritone horn stack jabbing three quick stabs then silence, recorded hot|the horn jabs',
      'a clean electric guitar chucking short chords on the and of 2 and 4, dry and close|the guitar chucks',
      'a tambourine shaken in nervous bursts between the snares, a hair ahead|the nervous tambourine',
      'a drawbar organ pushing one chord in short urgent stabs, the percussion click on|the organ stabs',
      'a baritone horn honking a two-note reply on the turnaround|the bari honk',
      'a horn section swelling in one breath then cut off dead on beat 3|the horn swell',
      'a trumpet playing one urgent stabbed note, repeated three times, each a hair earlier|the urgent trumpet',
      'a clean guitar lick of four notes sliding up to the root, played once per 8 bars|the guitar lick',
    ],
    character: [
      'recorded hot to tape, the horns rasping',
      'a little out of tune between players, so it sounds real',
      'played a hair ahead, urgent',
      'dry and close, the room tail cut off',
      'pushed until the tape saturates on the loud hits',
      'mono, up the middle, loud',
      'the breath and valve clicks left in',
    ],
    roles: [
      'a twitch of it answering the snare in the hook',
      'three stabs at the end of each phrase, then nothing',
      'urgent through the verse, cut dead for the hook',
      'one stab on beat 3 every other pass, the rest silent',
      'jabbing in the gaps the lead leaves',
    ],
    tagWord: 'twitching',
  },
  hits: {
    frame: 'struck and damped by hand over the beat',
    sources: [
      'a grand piano struck in a 3+2 pattern, five hits per bar, the last damped by hand|the piano hits',
      'an upright piano hit on beat 1 and the and of 3, the sustain pedal stamped down after each hit|the stamped piano',
      'a string section stab slammed on beat 1, then three quieter stabs falling by half steps|the falling string stabs',
      'a piano octave hit on the e of 1, cut dead, answered by one low note on 4|the octave hit',
      'a prepared piano cluster struck twice then muted with the palm, the strings buzzing|the palm-muted cluster',
      'a string section hit chopped to a 16th and struck in a 3+3+2 pattern, dry|the chopped string hits',
      'a piano chord slammed with both hands then one bass note a 16th late, four times then nothing|the hand-slammed chord',
      'a low piano note struck hard on 2 and 4 with the snare, the hammer thud louder than the pitch|the piano thud',
      'a string stab pitched down an octave and struck in a 2+3 pattern, the tail cut on the snare|the low string stab',
      'a single piano hit, high and sour, placed on the and of 4 every bar, damped before the one|the sour hit',
    ],
    character: [
      'every hit a different force, by hand',
      'damped by hand before the next hit',
      'recorded close, the hammer thud louder than the note',
      'the sustain pedal creaking under the hits',
      'pitched down a whole step so it sounds heavy',
      'the last hit of each bar left to ring',
      'hard-clipped so each hit cracks',
      'sampled off a record, the hiss left in',
    ],
    roles: [
      'the hook\'s backbone, every bar, identical',
      'in the hook only, out for the verse',
      'answering the lead with one hit per phrase',
      'twice per bar through the verse, four times in the hook',
      'alone with the drums for the last eight bars',
    ],
    tagWord: 'hand-struck',
  },
};

const HINT_WEIGHTS = [['hits', 3], ['staxtwitch', 2], ['fuzzlament', 2], ['machineglitch', 2]];

/** Hook devices from earworm research; one goes into the lead line. */
const HOOK_DEVICES = [
  'the hook is three notes and a rest, and the rest is the hook',
  'the hook repeats four times, the fourth time with one note changed',
  'the phrase stops one note short of the root, every time',
  'a full beat of silence right before the hook, then the hook',
  'one unusual leap in the middle of a plain contour, the leap never resolved',
  'the hook answers itself an octave down on the repeat',
  'the last note of the hook lands a 16th early every time',
  'the hook starts on the and of 4, a beat before you expect it',
  'the same two bars, the second time one note sour',
  'the hook is two notes, the second held until the snare cuts it',
  'the melody circles the fifth and never lands on the root until the last bar',
  'the hook is the lead\'s phrase with the middle note missing',
];

module.exports = { HINT_FAMILIES, HINT_WEIGHTS, HOOK_DEVICES };
