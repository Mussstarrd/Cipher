'use strict';
/**
 * Base sound-design grammar shared by every lane. Every sound is built from slots (what it is,
 * how it is processed, where it sits, how a person plays it), never picked whole from a short
 * list. A one-phrase label ("dark synth chord loop") points Suno at the average of thousands of
 * tracks, the preset; specific physical detail pushes it off the average.
 *
 * Every string here passes lib/guard.js (a test walks the whole grammar). Lanes add their own
 * instrument families, drums and anchors in lib/lanes.js.
 */

const LEAD = {
  register: [
    'a deep lead parked an octave above the 808',
    'the melody lives under the snare, in the low mids',
    'a low melody, darker than the kick',
    'the lead sits below the hats and above the bass',
    'the top end cut off the lead until only the body speaks',
    'a lead that stays in the bottom two octaves of the keyboard',
    'a chest-level melody, felt as much as heard',
    'the melody tuned down an octave, thick and dark',
    'low register lead, its brightest note still under the snare',
    'the lead doubled an octave down, the low copy louder',
    'mid register, narrow range',
    'mid register, never above the snare\'s crack',
    'low-mid, stepping into the mids only on the last note',
  ],
  articulation: [
    'clipped staccato 16ths that skip the downbeat',
    'two long notes then a triplet stab',
    'off-beat stabs on the and of 2 and 4',
    'one held note per bar, cut before the snare',
    'three-against-two stabs that fight the hats',
    'dotted-8th repeats drifting across the bar line',
    '16th stutters that cut out in the verse',
    'a push note one 16th before each downbeat',
    'legato runs that slur into each other',
    'ghosted notes with one hard accent per bar',
    'stabs that only answer the snare',
    'three quick notes landing on beat 1, then silence',
    'quarter-note triplets over straight drums',
    '32nd-note retriggers on the last beat of each bar',
    'long tones that start a 16th late',
    'two stabs, then two bars of silence',
    'staggered 8ths with every third note muted',
    'one anticipated stab on the last 16th of the bar',
  ],
  motif: [
    'a five-note figure that climbs a minor third and falls an octave',
    'a two-note motif bouncing a tritone',
    'a three-note cell: root, flat second, root',
    'a four-note minor descent, last note bent flat',
    'one repeated note with a single octave leap',
    'a six-note phrase circling the fifth, never resolving',
    'a nervous half-step shake turned into a riff',
    'a three-note dark minor hook with one sour note in the middle',
    'a phrase that rises a fourth, then a reply that falls a fifth',
    'a seven-note run skipping every other scale step',
    'a stubborn root note leaning on the flat sixth',
    'a four-note minor figure ending on the flat second',
    'an octave-jumping two-note riff',
    'a four-note riff that starts one 16th later each bar',
    'a dark minor stab that slides down a whole step',
    'a three-note creeping climb, half step by half step, back to the root',
    'a two-chord loop, the second chord a hair late',
    'a four-note major hook that lands on the sixth and grins',
  ],
  // How a person plays it. The anti-quantize slot.
  human: [
    'played by hand, never snapped to the grid',
    'every note a slightly different velocity, never even twice',
    'the second note always a hair late',
    'rushed into the bar, then laid back',
    'one take with the small mistakes left in',
    'notes overlapping a little, like a player who holds too long',
    'the accent moving to a new note each pass',
    'pressure swelling and fading unevenly under the fingers',
    'timing pulled behind the snare',
    'loose and slightly ahead of the kick',
    'a little drunk on the timing but always landing the one',
    'released early on the off-beats, so the gaps breathe',
    'bent into pitch by hand, never landing in tune twice',
    'played like someone tapping it out on a car dashboard',
  ],
  motion: [
    'it gets a little brighter each time the hook comes back',
    'it bends down a half step at the end of every phrase',
    'one echo of the last note hangs over into the next bar',
    'the detune widens as the loop goes on',
    'the notes get shorter as the song goes on',
    'a second copy an octave down, louder in the hook',
    'the last note of each phrase plays reversed',
    'it gets grittier each pass',
    'one note changes each pass, so it never repeats exactly',
    'it thins to half its notes in the verse, whole in the hook',
    'it drifts up a few cents over 16 bars, then snaps back',
    'hit softer, then suddenly hard on the last pass',
    'it stays frozen and identical while the drums move',
  ],
  placement: [
    'bone-dry, center, tucked right above the 808',
    'panned left with a short slapback on the right',
    'mono center, out of the kick\'s way',
    'wide stereo with the lows summed to mono',
    'dry up front with a small dark plate behind it',
    'tucked under the bass, never fighting it',
    'set back in a short room, drums in front',
    'a short echo on the right side only, low in the mix',
    'dry on the left, delayed on the right',
    'one copy dead center, a quiet copy far right',
    'mid-scooped so the 808 owns the low end',
    'narrow and loud, mono-compatible',
    'dry and close, delay only on the last note',
    'sitting behind the snare, louder than the hats',
  ],
};

const BASS = {
  tone: [
    'tube saturation so it shows up on phone speakers',
    'a clean round body',
    'hard-clipped grit',
    'glide between every note',
    'glide only into the downbeat',
    'a tight decay that dies before the next kick',
    'long tails that ring into the snare',
    'a click on the attack',
    'a low-pass that closes on each note',
    'a slight pitch fall at the end of every note',
    'upper harmonics brought up with distortion',
    'a short gated tail',
  ],
  pattern: [
    'following the kick exactly',
    'playing off the kick, landing in its gaps',
    'one note per bar',
    'syncopated 16ths with long rests',
    'one octave slide into the hook',
    'a slide up a fifth into each new bar',
    'silent for 2 bars, then hits',
    'three notes then nothing',
    'root and flat second only',
    '32nd stutters on the last beat',
    'holding under the lead and moving when it rests',
    'playing the lead\'s motif two octaves down',
    'a triplet bounce against straight hats',
    'landing a 16th late on purpose',
  ],
  human: [
    'slides played by hand, each one a different length',
    'note lengths changing every bar like a player feeling it out',
    'landing a hair behind the kick',
    'the last note of each bar played late on purpose',
    'slides that land slightly flat, then settle',
    'pushed ahead of the beat on the last note of the phrase',
    'hit harder on the second pass, softer on the first',
    'held one beat too long now and then, like a human',
  ],
};

/** Twitch percussion: a sound, a rhythm, a place in time. The nervous-energy layer. */
const TWITCH = {
  sound: [
    'a tight metallic tick', 'a clipped hat burst', 'a dry knock tuned to the key', 'a bitcrushed clap fragment',
    'stuttered kick retriggers', 'a short noise burst', 'a pitched-up knock blip', 'a dry wooden click', 'a shaker ruffle',
    'a gated snare tail cut short', 'a tiny pitched perc ping', 'a reversed hat one-shot', 'a rimshot tick', 'a tongue-click sample',
  ],
  rhythm: [
    'in 5-hit bursts that skip the grid', 'stuttering 32nds for half a beat', 'repeating every 3 16ths against the bar',
    'in uneven pairs, long-short', 'in a quick double hit right before the snare', 'in a broken 7-hit pattern that loops every 2 bars',
    'in single hits that land in random 16th slots', 'retriggering at triple speed for one beat', 'flamming against the hats',
    'stop-start, 3 on, 2 off', 'triplets that stop dead', 'in a burst that slows down as it decays',
  ],
  where: [
    'on the last beat of every 2nd bar', 'between the kick and snare', 'right before each downbeat', 'only in the gaps the lead leaves',
    'answering the snare', 'on the last beat of each hook only', 'quietly through the verse', 'only after the switch',
    'on beat 3, never the same way twice', 'around the 808 slides', 'under the hats, barely there',
  ],
};

const GROOVE = {
  sync: [
    'kicks pushed off the beat, snare dead on', 'everything leans on the and', 'the kick avoids beat 1 through the verse',
    'accents on the last 16th of each beat', 'the 808 and kick anticipate the downbeat', 'half-time snare under double-time hats',
    'the snare a hair early on the last beat of the hook', 'a broken kick pattern that never repeats twice',
    'kick syncopation that tracks the lead\'s rhythm', 'a gap on beat 1 so the downbeat hits harder', 'off-beat everything except the snare',
    'a full stop on beat 4 every 8 bars',
  ],
  human: [
    'unquantized, snare a hair late', 'hats rushing slightly ahead', 'kick drifting behind the grid', '16ths pushed slightly late for a lazy bounce',
    'a loose pocket, played in, never snapped', 'clap layers a few milliseconds apart', 'quiet uneven extra snare taps',
    'percussion played live over the loop', 'the whole loop sitting back on the beat', 'tight kick, loose everything else', 'a slight push into each snare',
    'the hats dragged a few milliseconds behind the kick', 'the snare landing late and heavy like a tired drummer',
  ],
  // The beat-3 drunken triplet slur: the fingerprint, in hip hop language. Never "waltz", never "3/4".
  triplet: [
    'the 808 slurs into a triplet on beat 3', 'a three-note stumble every third beat', 'the kick trips over itself on beat 3',
    'beat 3 drags, beat 4 catches up', 'the hats slur into threes on beat 3, then straighten', 'a drunk triplet 808 bounce on the third beat',
    'the snare lands, then the 808 stumbles three quick notes', 'beats 1 and 2 straight, beat 3 slurred into three',
    'the loop stumbles on 3 and lands the one hard', 'a triplet hiccup on beat 3 every bar', 'the 808 skids into beat 3 like a missed step',
    'the pocket leans drunk on beat 3, sober on the one',
  ],
};

/** The beat switch is one axis. Its words go in the head tag, the [Beat Switch] cue and every section after it. */
const SWITCH = {
  axes: [
    { tag: 'beat switch to half-time', cue: 'half-time drums, tempo halves, slow heavy kick, long 808 slides', after: 'half-time, slow heavy kick' },
    { tag: 'second half double-time', cue: 'double-time drums, hats double, kick pattern doubles', after: 'double-time, hats doubled' },
    { tag: 'beat switch, key falls', cue: 'key falls a minor third, the lead pitched down, same drums', after: 'in the lower key, the lead pitched down' },
    { tag: 'beat switch, new drums', cue: 'new kick pattern, the 808 running straight 16ths, harder snare', after: 'new drums, running 808' },
    { tag: 'hard beat switch halfway', cue: 'everything strips to 808 and one snare, the lead pitched down an octave', after: 'stripped to 808 and snare' },
    { tag: 'beat flips at the midpoint', cue: 'the kick pattern flips to the off-beats, the lead stutters and slices itself up', after: 'off-beat kicks, sliced lead' },
  ],
  land: [
    'on a hard cut, straight in',
    'slowing down into it like the power got cut',
    'the drums cut, the lead rings alone, then everything slams back',
    'the new section starts on beat 2, not 1',
    'everything stops dead on beat 4, the new beat slams in on the one',
  ],
};

/** One extra percussion color per roll, played by a person. */
const COLOR = [
  'maracas shaken by hand under the hats, a little behind the beat',
  'a military snare playing tight rudiments under the hats',
  'a tambourine smacked on 2 and 4, never quite even',
  'finger snaps landing a hair late',
  'a dry woodblock knocking the off-beats',
  'a shaker played live, the accents wandering',
  'a second clap layer slapped by hand, slightly early',
  'congas played with the palms, two hits before each snare',
  'a bottle tapped with a stick on the and of 3',
];

/** The counter part: a second instrument from another family, used sparingly. */
const COUNTER_ROLE = [
  'answering the lead at the end of each phrase',
  'one long held note under the hook only',
  'two notes on the turnaround, then gone',
  'doubling the last note of each lead phrase',
  'only after the switch, never before',
  'a three-note reply in the gaps the lead leaves',
  'a single low note on beat 3, every other pass silent',
];

/** Front tags. Suno weights the first ~260 characters and reads them as tags. */
const RHYTHM = {
  sync: ['off-grid kick pattern', 'kicks pushed off the beat', 'kick skipping the downbeat', 'broken kick pattern', 'snare dead on, kick early', '808 anticipating the one', 'hats in uneven 16ths', 'syncopated double kicks', 'off-beat 808 hits', 'kick landing on the and', 'kick a 16th ahead of one', 'snare early, 808 late'],
  twitch: ['twitchy percussion', 'stuttering 32nd perc hits', 'nervous clicking percussion', 'retriggered perc bursts', 'clipped hat bursts', 'jittery metallic ticks', 'stop-start hat stabs', 'flamming perc hits', 'broken percussion loop', 'twitching snare retriggers', 'nervous perc ticks between kicks', 'shaky hat retriggers under the loop'],
  slur: ['drunk 808 bounce', 'slurred triplet bounce', 'a stumble on beat 3', '808 slurring into beat 3', 'three-note stumble on beat 3', 'triplet stumble on beat 3', 'drunken bounce pocket', 'hats dragging on beat 3', 'lazy triplet 808 slide', 'stumbling triplet kicks', 'the pocket slips drunk on beat 3', 'beat 3 trips, beat 4 straightens'],
  low: ['low register lead', 'deep low-mid lead', 'lead under the snare', 'lead an octave above the 808', 'low dark lead melody', 'low-mid lead, top cut off', 'lead parked in the low mids', 'deep baritone melody', 'lead below the hats', 'lead tucked above the 808'],
  loop: ['one hypnotic loop', 'minimal one-loop beat', 'hypnotic repeating loop', 'single loop, never changes', 'one-bar loop, locked', 'minimal, one riff only', 'same two bars all song', 'two-bar loop, hypnotic', 'stripped minimal loop'],
  straight: ['steady pocket start to finish', 'one pocket, locked in', 'the beat never lets up', 'same drums every section, hard cuts only'],
};

/** Always one of these in the drums line. The positive form of "no fills, no transitions". */
const ANTI_TRANSITION = [
  'the same loop the whole way',
  'drums cut dead, then slam back on the one',
  'every section identical except one sound',
  'the beat stays frozen from start to finish',
  'hard cuts only, straight in on beat 1',
  'sections change on a hard cut, nothing in between',
  'the loop runs flat through every section',
  'one sound in, one sound out, drums identical',
  'drums stop dead on beat 4, back on beat 1',
  'the hook is the verse with one more sound',
];

const TITLE = {
  a: ['Glass', 'Rubber', 'Copper', 'Static', 'Velvet', 'Crooked', 'Paper', 'Neon', 'Iron', 'Loose', 'Hollow', 'Quiet', 'Gold', 'Bent', 'Midnight', 'Chrome', 'Cassette', 'Backseat', 'Sunday', 'Low'],
  b: ['Jaw', 'Halo', 'Receipt', 'Pulse', 'Lantern', 'Alibi', 'Engine', 'Teeth', 'Mirror', 'Ledger', 'Fuse', 'Orbit', 'Hinge', 'Tab', 'Signal', 'Crown', 'Ceiling', 'Pocket', 'Window', 'Porch'],
};

/** Shared instrument families any lane may borrow as a guest lead (experimental palette). */
const FAMILIES = {
  found: {
    frame: 'sampled and tuned over hard drums',
    sources: [
      'a sampled car horn tuned to the key, honking the riff in short blasts|the car horn',
      'a landline dial tone resampled and pitched into the melody|the dial tone',
      'a tuned kick drum pitched into a melody, booming each note|the tuned kick lead',
      'a pitched tom line played as the melody, dead and dry|the tom melody',
      'a rubber band plucked over a tin can, pitched and looped|the rubber band pluck',
      'a door buzzer pitched down and played as notes|the door buzzer',
      'a smoke alarm chirp tuned down two octaves, thick and dark|the smoke alarm',
      'a creaking door hinge sampled and pitched into a melody|the door hinge',
      'a row of glass bottles knocked with a stick, tuned to the key|the bottle knocks',
    ],
    character: ['resampled until the grain shows', 'tuned by ear, a few cents off', 'gated so each note stops dead', 'pitched down until it sounds heavy', 'hard-clipped on every hit', 'doubled by a distorted 808 on the root'],
  },
  synth: {
    frame: 'over hard drums and 808s',
    sources: [
      'a rubber-band mono synth that boings on every glide|the rubber-band synth',
      'a woozy, seasick detuned synth that slides into every note|the woozy synth',
      'a syrupy slowed-down synth lead with a pitch sag|the syrupy synth',
      'a grinding, bit-mangled digital lead|the grinding lead',
      'a thick, chewy analog brass patch|the analog brass patch',
      'a thin nagging square lead like a pager going off|the pager lead',
      'a cold detuned triangle lead, slightly out of tune|the cold triangle lead',
      'a warbling detuned synth like a phone ringing underwater|the underwater synth',
      'a distorted 808 played up an octave as the lead|the distorted 808 lead',
    ],
    character: ['bitcrushed just enough to grit the attacks', 'hard-clipped on every transient', 'faintly ring-modulated with a metallic edge', 'resampled down an octave so the grain shows', 'glide time drifting slower and faster by hand', 'gated tight so every note cuts off clean', 'soft-clipped and glued with slow compression', 'detuned a few cents against itself'],
  },
  organ: {
    frame: 'over hard drums and 808s',
    sources: [
      'a drunken detuned organ, the pitch sagging and swaying|the drunken organ',
      'a wheezing broken organ running out of air|the broken organ',
      'a cheap combo organ with a wavering vibrato|the combo organ',
      'a detuned out-of-tune piano with sticky keys, played heavy|the sticky piano',
      'a toy piano with plastic, clanky hammers|the toy piano',
      'a dark low piano note, struck hard and left to ring|the low piano',
      'a cheap 80s home keyboard on its piano preset, thin and plastic|the home keyboard',
      'a prepared piano with paper on the hammers, dull and clacking|the prepared piano',
      'a harmonium wheezing a two-chord loop, the bellows audible|the harmonium',
    ],
    character: ['slurring in and out of tune like it had a few drinks', 'keys clacking audibly under the notes', 'pushed through a cranked tube preamp', 'with pedal creaks and key noise left in', 'the pitch sagging on every held chord', 'low-passed so it sounds like it is behind a wall', 'overdriven until the low notes snarl', 'sample-rate reduced to a grainy edge'],
  },
  guitar: {
    frame: 'over hard drums and 808s',
    sources: [
      'a muted electric guitar through a fuzz pedal|the fuzz guitar',
      'a pitched-up soul guitar sample cut to one bar|the soul guitar sample',
      'a dark baritone guitar, picked low and close|the baritone guitar',
      'a distorted bass guitar played up high as the lead|the distorted bass guitar',
      'a pitch-bent electric guitar harmonic, sampled and looped|the guitar harmonic',
      'a detuned acoustic guitar sample, pitched down and dark|the acoustic guitar sample',
    ],
    character: ['palm-muted and gated tight', 'overdriven through a small amp', 'string squeaks and pick noise left in', 'bent slightly flat on the long notes', 'doubled and panned hard left and right', 'band-passed thin like a phone speaker', 'parallel-distorted with the clean signal on top'],
  },
  wind: {
    frame: 'sampled, pitched and flipped over hard drums',
    sources: [
      'an erhu sample bending and sliding between notes, pitched down into the mids|the erhu',
      'a harmonica sample slurred and bent between notes, pitched down into the low mids|the slurred harmonica',
      'a bamboo flute sample, overblown so it squeaks, pitched down|the bamboo flute',
      'a low flute sample pitched down an octave, dark and woody|the low flute',
      'a melodica played breathy and slurred, pitched down into the low mids|the melodica',
    ],
    character: ['breath and finger noise left in', 'bent hard between notes', 'doubled by a distorted 808 on the root', 'run through a guitar amp until it barks', 'time-stretched so the grain smears', 'cut into one-bar pieces and rearranged', 'a slight pitch warble, never steady'],
  },
};

/** Guest-lead share: a lane borrows the experimental palette for the lead this often; the counter stays in-lane. */
const GUEST_FAMILIES = [['found', 2], ['synth', 3], ['guitar', 2], ['organ', 2], ['wind', 2]];

const MIX_TAG = 'crisp punchy radio mix';

module.exports = { LEAD, BASS, TWITCH, GROOVE, SWITCH, COLOR, COUNTER_ROLE, RHYTHM, ANTI_TRANSITION, TITLE, FAMILIES, GUEST_FAMILIES, MIX_TAG };
