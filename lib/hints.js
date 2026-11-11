'use strict';
/**
 * Hints: one extra, strange ingredient per roll that keeps a sample from sounding like the
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
 * line, and its handle reaches the Lyrics tags through {HINT}. The samples these were learned
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
      
      
      'a lone fuzzed guitar line played slow and mournful, the echo repeats louder than the notes|the mourning guitar',
      'a guitar bend held until it tips into feedback, then let fall, tape echo smearing the fall|the feedback fall',
      'a fuzz guitar playing two notes a minor third apart, each held for a bar and bent flat at the end|the two-note lament',
      'a phased fuzz guitar hanging one note over the whole hook, the phase moving slower than the bar|the hanging fuzz note',
      'a fuzz guitar with tape echo regenerating so each phrase overlaps the last, the echo darker than the dry note|the regenerating echo guitar',
      'a wah rocked slowly across one held fuzz note so the tone opens and closes once per bar|the breathing wah note',
    ],
    character: [
      'tape echo set long, the repeats decaying out of time',
      'the fuzz cranked until the note blooms into feedback',
      'bent flat and held until the sustain crumbles',
      'played slow over the beat like a funeral, never on the grid',
      'the wah barely moving, half open',
      'mixed under the drums, the echo louder than the note',
      'one take, the string squeaks and amp buzz left in',
      'pitched down a whole step so it sounds heavy and sad',
      'the fuzz set thick enough that two notes together turn to grit, so the line is single notes only',
      'leaning on the flat seventh and the fourth, slow and mournful',
    ],
    roles: [
      'a faint hint under the hook only',
      
      'crying in the gaps the lead leaves',
      'low in the mix through the verse, up front for the last eight bars',
      'answering the lead once per phrase with a single bend',
    ],
    tagWord: 'faint',
  },
  machineglitch: {
    frame: 'an industrial machine texture cut into the beat',
    sources: [
      
      'a machine-shop clank sampled and tuned to the key, hard-clipped on every hit|the tuned clank',
      
      'a detuned guitar riff crushed to a grinding digital edge, slowed to half speed|the grinding riff',
      'a short noise burst gated into a mechanical rhythm, cold and dry|the gated noise',
      
      'a drum loop resampled through a broken converter, the bits splintering on the snare|the splintered loop',
      'a hydraulic hiss and a steel door shut hard, sampled and placed as percussion, tuned down|the steel door',
      'metal-on-metal hits and machine noise used as percussion, each one tuned to the key|the tuned metal hits',
      
      'gated noise bursts that open with the snare and shut with it, the length of a sixteenth|the snare-gated noise',
      
      'tape-warped keys, the pitch sagging a quarter tone at the end of each chord|the warped keys',
    ],
    character: [
      'bit-reduced until the edges splinter',
      
      'ring-modulated to a cold metallic edge',
      'hard-clipped on every hit, the tail gated short',
      'the pitch falling on each repeat like a dying motor',
      'gated in uneven bursts, never twice the same',
      'resampled at a low rate so the top end turns to grit',
      'detuned a quarter tone against the key',
    ],
    roles: [
      'a glitch in the gaps between the lead\'s phrases',
      'twitching in and out under the hook, never on the one',
      
      'quietly through the verse, arriving on the last hook unannounced',
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
      
      
      'a trumpet playing one urgent stabbed note, repeated three times, each a hair earlier|the urgent trumpet',
      'a clean guitar lick of four notes sliding up to the root, played once per 8 bars|the guitar lick',
      'a horn stack of trumpet over baritone, two-note stabs that answer the lead a beat after it stops|the answering horns',
      'the horns stabbing three times fast then holding once, the figure identical every pass|the three-stab horns',
      'a spinet drawbar organ holding one chord under everything, a second chord only at the turn of the two bars|the spinet organ',
      'a bright single-coil guitar doubling the bass line note for note, then chopping one muted chord on beats 2 and 4|the doubling guitar',
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
      
      'a prepared piano cluster struck twice then muted with the palm, the strings buzzing|the palm-muted cluster',
      'a string section hit chopped to a 16th and struck in a 3+3+2 pattern, dry|the chopped string hits',
      'a piano chord slammed with both hands then one bass note a 16th late, four times then nothing|the hand-slammed chord',
      'a low piano note struck hard on 2 and 4 with the snare, the hammer thud louder than the pitch|the piano thud',
      'a string stab pitched down an octave and struck in a 2+3 pattern, the tail cut on the snare|the low string stab',
      
      'a grand piano struck in short damped hits, the sustain pedal off, every note cut by the hand|the damped piano hits',
      'piano hits on a 3+3+2 cell across one bar of sixteenths, the same cell every bar, the last hit a sixteenth late|the 3+3+2 piano',
      'piano hits damped by the palm on the strings inside the lid, more thud than tone|the lid-damped piano',
      
      'chord stabs played from drum pads like a drum part, each stab the length of a sixteenth, three per bar|the pad stabs',
      'a prepared piano with felt on the hammers, two notes stabbed in 3+2 across the first half of the bar|the felt piano stabs',
      'a sampled whole-band chord hit, one hit, pitched to the key and placed on beat 1 only|the sampled band hit',
    ],
    character: [
      'every hit a different force, by hand',
      'damped by hand',
      'recorded close, the hammer thud louder than the note',
      'the sustain pedal creaking under the hits',
      'pitched down a whole step so it sounds heavy',
      'the last hit of each bar left to ring',
      'hard-clipped so each hit cracks',
      'sampled off a sample, the hiss left in',
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
  foundsound: {
    frame: 'a real object recorded close and placed as an instrument',
    sources: [
      'a wooden chair creaking in rhythm, recorded close, tuned down|the creaking chair',
      'coins tipped onto a wooden table, each one a different pitch, cut to the grid|the tipped coins',
      'a dripping tap recorded in a tiled room, the drips tuned to the key|the tuned drip',
      'a bicycle wheel ticking as it spins down, slowing across the bar|the bicycle wheel',
      'a zippo lighter clicking open and shut as percussion, dry|the lighter click',
      'a glass bottle blown across the top, one breathy low note|the bottle note',
      'a typewriter key clack sampled and tuned, four notes|the typewriter',
      'keys jangling in a pocket, filtered dark, in rhythm|the pocket keys',
      'a basketball bounced on a gym floor, pitched down, the room left in|the basketball bounce',
      'a car door shutting, sampled, tuned to the key, placed on beat 3|the car door',
    ],
    character: ['recorded close, the room left in', 'tuned by ear, a few cents off', 'gated so each hit is short', 'pitched down until it sounds heavy', 'hard-clipped on every hit', 'dry, up the middle, quiet'],
    roles: ['once in the whole song, in the second verse', 'barely there, under the hats, the whole way', 'twice only', 'only in the last four bars', 'one hit per phrase, answering the lead'],
    tagWord: 'hidden',
    sprinkle: true,
  },
  lowbrass: {
    frame: 'one low horn note',
    sources: [
      'a tuba playing one low note per bar, breathy and close|the tuba note',
      'a bass trombone holding a single flat note under the lead|the trombone drone',
      'a euphonium doubling the bass an octave up, soft and round|the euphonium',
      
      
      
      'a baritone horn holding one note under the hook, a little flat|the baritone note',
      'a tuba and an 808 playing the same note, the tuba a hair late|the tuba double',
    ],
    character: ['blown so hard it rasps', 'a little out of tune, so it sounds real', 'recorded close, the valve clicks left in', 'pitched down off an old sample', 'doubled by the 808 on the root'],
    roles: ['once per phrase, answering the lead', 'under the hook only', 'once in the whole song, in the last verse', 'twice only', ],
    tagWord: 'low',
    sprinkle: true,
  },
};

const HINT_WEIGHTS = [['hits', 3], ['staxtwitch', 2], ['fuzzlament', 1], ['machineglitch', 2], ['foundsound', 3], ['lowbrass', 2]];
/** The sprinkled families: a mystery ingredient that appears once or twice and sends the track over the edge. */
const SPRINKLE_WEIGHTS = [['foundsound', 4], ['lowbrass', 2], ['staxtwitch', 1], ['hits', 1]];

/** Hook devices from earworm research; one goes into the lead line. */
/**
 * Hook devices: the thing the lead does that makes it stick (research/06). Each is a full description for the lead line
 * and a short tag for the head, where Suno weights it. None names a hook or a verse, so every device works in loop layouts.
 */
const HOOK_DEVICES = [
  'three notes and a rest, the rest being the part that sticks|three notes and a rest',
  'the riff repeats four times, the fourth time with one note changed|fourth repeat changes one note',
  'the phrase stops one note short of the root, every time|riff stops short of the root',
  'a full beat of silence right before the riff lands, then the riff|a beat of silence before the riff',
  'a plain rise and fall with one wide leap in the middle, left hanging|one wide leap mid-riff',
  'the riff answers itself an octave down on the repeat|riff answered an octave down',
  'the last note of the riff lands a 16th early every time|last note lands early',
  'the riff starts a beat early, ahead of where the ear expects it|riff starts a beat early',
  'the same two bars twice, the second time with one note sour|second pass one sour note',
  'the riff is two notes, the second held until the snare cuts it|two notes, cut by the snare',
  'the melody circles the fifth and lands on the root only at the end|riff circling the fifth',
  'the riff with its middle note missing, the gap louder than the notes|middle note missing',
  'four notes up then down, the third a tritone leap, identical each time|tritone leap on the third note',
  'one pitch on every 8th for two bars, the final note a half step down|one note hammered, then a half step down',
  'the lead moves in 16ths over half-time drums, quick riff, heavy pocket|quick riff over a heavy pocket',
  'five notes rising and falling, the second one a 16th behind each time|second note a 16th behind',
  'the motif plays four times, the fourth with one note a half step lower|fourth pass a half step lower',
  'the riff copies the kick rhythm note for note, so it hits like a drum|riff locked to the kick',
  'the lead plays and stops, the bass answers in the gap, trading all song|lead and bass trading',
  'a three-16th figure looping across the bar line, the accent always moving|three-16th figure crossing the bar line',
  'the riff once loud, once at half volume, an echo played by hand|loud riff, quiet echo',
  'each phrase ends on a note bent up a half step at the last moment|last note bent up',
  'the first note struck three times fast, then the phrase played plain|first note struck three times',
  'a long note for two bars, then four quick ones bunched together|long note, then four quick ones',
  'each note just ahead of the beat except the last, dead on the one|ahead of the beat, last note on the one',
  'the top note hit once per phrase, the loudest thing in the bar|one loud top note',
  'three steps down, then an octave jump back up on the and of 3|octave jump on the and of 3',
  'a note twice, a rest, then twice a half step up, simple as a doorbell|doorbell riff, two notes twice',
  'the riff silent on 2 and 4, loud everywhere the snare is not|riff dodging the snare',
  'three short notes answered by one long one, the answer a little flat|three short notes, one long reply',
  'the phrase begins and ends mid-bar, never starting on the one|riff never starts on the one',
  'repeated so exactly it turns into rhythm, then one note goes missing|exact repeats, one note missing',
  'two notes a half step apart, rocking, the rhythm doing all the work|half-step rocking riff',
  'the riff staccato three times and legato once, the long one last|short, short, short, long',
  'the riff leans hard on the flat seventh and leaves it hanging|leaning on the flat seventh',
  'the riff sits in the gaps of the kick, melody and drums interlocked|riff in the gaps of the kick',
  'a quick grace note flicked off the last pitch, like a finger slipping|grace-note flick ending',
  'four notes over a three-beat cycle, starting somewhere new each time|four-note riff over a three-beat cycle',
];

module.exports = { HINT_FAMILIES, HINT_WEIGHTS, SPRINKLE_WEIGHTS, HOOK_DEVICES };
