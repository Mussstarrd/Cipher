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
    'the snare a hair late and hit harder for it, its ring left to decay',
    'hats dead straight on the grid, the kick leaning late behind them, the snare a hair early so the pocket feels pulled two ways',
    'played by hand against the bass, the second hit always a hair late',
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
    { tag: 'second half on a new drum pattern', cue: 'a second drum pattern: the snare moves from 2 and 4 to 3 alone and the kick thins to one per bar', after: 'a new drum pattern' },
    { tag: 'second half, kick an 8th late', cue: 'the kick pattern sits one 8th later against everything else, the snare where it was', after: 'kick an 8th late' },
    { tag: 'second half, new kit, same pattern', cue: 'keeps the pattern and swaps the kit: a deeper, longer kick and a rim click where the snare was', after: 'new kit, same pattern' },
    { tag: 'claps take over in the second half', cue: 'trades the snare for a stack of handclaps and adds a kick on the and of 2', after: 'claps take over' },
    { tag: 'hats twice as fast in the second half', cue: 'doubles the hat rate while kick and snare hold their pattern', after: 'hats twice as fast' },
    { tag: 'second half a minor third lower', cue: 'the whole beat sits a minor third lower, lead, bass and drums pitched together', after: 'a minor third lower' },
    { tag: 'final third a whole step up', cue: 'sits a whole step higher, every sound moved together, the tempo unchanged', after: 'final third a whole step up' },
    { tag: 'riff and bass trade registers', cue: 'the riff and the bass trade registers, the riff down low and the bass line up top', after: 'riff and bass trade registers' },
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
    'the old beat stops on beat 4 and the new beat is simply there on beat 1, nothing between',
    'the new section starts on beat 2, not 1',
    'the new beat already playing when the section begins, as if it had been there all along',
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
  twitch: ['twitchy percussion', 'stuttering 32nd perc hits', 'nervous clicking percussion', 'retriggered perc bursts', 'clipped hat bursts', 'jittery metallic ticks', 'stop-start hat stabs', 'flamming perc hits', 'broken percussion loop', 'twitching snare retriggers', 'nervous perc ticks between kicks', 'shaky hat retriggers under the loop', 'twitchy off-grid clicks', 'nervous stop-start ticks', 'jittery 32nd knock bursts', 'twitching kick retriggers'],
  slur: ['drunk 808 bounce', 'slurred triplet bounce', 'a stumble on beat 3', '808 slurring into beat 3', 'three-note stumble on beat 3', 'triplet stumble on beat 3', 'drunken bounce pocket', 'hats dragging on beat 3', 'lazy triplet 808 slide', 'stumbling triplet kicks', 'the pocket slips drunk on beat 3', 'beat 3 trips, beat 4 straightens'],
  low: ['low register lead', 'deep low-mid lead', 'lead under the snare', 'lead an octave above the 808', 'low dark lead melody', 'low-mid lead, top cut off', 'lead parked in the low mids', 'deep baritone melody', 'lead below the hats', 'lead tucked above the 808'],
  loop: ['one hypnotic loop', 'minimal one-loop beat', 'hypnotic repeating loop', 'single loop, never changes', 'one-bar loop, locked', 'minimal, one riff only', 'same two bars all song', 'two-bar loop, hypnotic', 'stripped minimal loop'],
  straight: ['steady pocket start to finish', 'one pocket, locked in', 'the beat never lets up', 'same drums every section, hard cuts only'],
};

/** Always one of these in the drums line. The positive form of "no fills, no transitions". */
/** Short head-tag forms of the anti-transition rule; one always sits in the front tags. */
const ANTI_TAGS = ['hard cuts only', 'every section starts cold on beat 1', 'sections cut dead, nothing between', 'the beat stays frozen', 'same drums every section'];

const ANTI_TRANSITION = [
  'the same loop the whole way',
  'drums cut dead, then simply there again on the one',
  'every section identical except one sound',
  'the beat stays frozen from start to finish',
  'hard cuts only, straight in on beat 1',
  'sections change on a hard cut, nothing in between',
  'the loop runs flat through every section',
  'one sound in, one sound out, drums identical',
  'drums stop dead on beat 4, back on beat 1',
  'the hook is the verse with one more sound',
  'every section starts cold on beat 1, nothing leads into it',
  'the drums never announce a change, they just change',
];

/**
 * Drum cadences: where the kick and snare land, said in beats, so the bar is never the genre default. The text goes
 * in the drums line; the tag takes the syncopation tag's seat in the head.
 */
const CADENCE = [
  'kick on 1 and the and of 2, the snare on 3 only|snare on 3 only',
  'three kicks per bar spaced long, long, short, the snare on 2 and 4|long-long-short kick',
  'one kick per bar on 1, the snare on 2 and 4, the bar otherwise open|one kick per bar',
  'kick, snare, kick, kick, one hit per beat, the snare on 2 only|snare on 2 only',
  'a two-bar pattern, the first bar busy with kicks, the second bar a single kick and air|busy bar, empty bar',
  'kick on 1 and 3, the snare on the off-beats between them|off-beat snare',
  'rim clicks in a five-hit figure across two bars, three slow then two quick, the kick on 1 only|five-click rim figure',
  'a soft kick on every beat, the snare on 2 and 4 hit twice as hard|four soft kicks, loud snare',
  'the kick copies the rhythm of the lead note for note, the snare keeping time by itself|kick shadows the lead',
  'the snare on 2 and again a 16th after 3, the kick on 1 and the and of 3|uneven double snare',
  'kick on 1, the and of 1 and the and of 3, the snare on 2 and 4|lopsided three-kick bar',
  'a marching snare figure of five strokes per bar over one deep kick on 1|marching snare over one kick',
  'the snare once per bar on 3, two kicks inside the first beat and none after|two kicks then one snare',
  'a seven-hit kick figure that takes two bars to come around, the snare fixed on 2 and 4|seven-hit kick figure',
  'kick on 1 and the and of 2, a clap on 2 and 4, a low tom on the and of 3|low tom on the and of 3',
  'hand drums keeping time with three slaps per bar, the kick on 1, the snare on 3|three hand slaps per bar',
  'three kicks in a row on 1, 2 and 3, then the snare by itself|three kicks then a snare',
  'the kick between the beats all bar, the snare square on 2 and 4|kick between the beats',
  'the snare on the and of 2 and on 4, the kick on 1 and 3|snare on the and of 2',
  'a three-beat kick figure repeating over four-beat bars so it lands somewhere new each bar, the snare fixed on 3|three-beat kick cycle',
  'six even kicks across the bar against four even hat strokes|six kicks against four hats',
  'kicks only in the first half of the bar, snares only in the second half|kicks first half, snares second',
  'the kick twice on 1 like a heartbeat, the snare a lone hit on 3|heartbeat kick, lone snare',
  'a kick on 1 and a quieter kick a 16th after 2, the snare on 4 only|snare on 4 only',
  'kick on 1 and a 16th after 2, the snare on 3, a quiet second snare a 16th after 3|ghost snare after 3',
  'kick on 1, the and of 1 and the and of 2, a clap on 2 and 4|three kicks crowding the front',
  'kick on the and of 1 and on 3, the snare on 2 and 4, beat 1 left to the bass alone|bass alone on the one',
  'kick on 1 and 2, then on the 16th before 3, the and of 3 and a 16th after 4, a clap on 2 and 4|five kicks, the later three uneven',
  'kick on every beat, rim hits on the 16th before 2 and on the and of 2 only|rims crowding beat 2',
  'a two-bar pattern, the snare on 3 in the first bar and on 4 in the second, the kick on 1 and the and of 2 both times|snare on 3, then on 4',
  'kick on 1 and the and of 3, the snare on 2 only, a rim click on 4|snare on 2, rim on 4',
  'five kicks spaced three 16ths apart starting on 1, the snare on 2 and 4|kick every three 16ths',
  'kick on 1, the 16th before 2 and the and of 2, one snare on 3, the second half of the bar left to the bass|front-loaded kicks, lone snare',
  'kick on 1 and the and of 2, the snare a 16th ahead of 2 and square on 4|early snare ahead of 2',
  'kick on 1 and the and of 3, claps on 2 and a 16th after 4|second clap a 16th late',
  'the snare on 3, kicks on 1, the and of 3 and on 4, beat 2 left empty|empty beat 2',
  'kick on 2 and the and of 3, the snare on 4 only, beat 1 carried by the bass|kick waits for 2',
  'a rim click on 2 and 4, the snare once on the and of 3, the kick on 1 and a 16th after 2|snare on the and of 3',
  'kick on 1 and the and of 2, two snare hits a 16th apart with the second on 3, the first one soft|flammed snare on 3',
  'a rim click on 1, the 16th before 2, the and of 2, the and of 3 and on 4, the kick only on 1 and 3|clave rim over a plain kick',
  'the snare on 2 and 4, kicks on 1, the and of 2 and the 16th before 3|two kicks chasing the snare',
  'kick on 1, the and of 2 and on 3, a clap on 2 and 4 with a finger snap on top of it|snap-and-clap bounce',
  'the snare on 3 only, kicks on 1 and the 16th before 3 so the second kick sits tight against the snare|kick tight against the snare',
  'a two-bar pattern, the first bar kick on 1 and snare on 3, the second bar the same with one more kick on the and of 2|one extra kick in the second bar',
  'a two-bar pattern, kicks on 1 and the and of 2 in the first bar, on the and of 1 and on 3 in the second, the snare on 2 and 4 in both|kick swaps sides',
  'a two-bar pattern, the snare on 2 and 4 in the first bar and only on 3 in the second, the kick on 1 in both|backbeat bar, then a lone snare on 3',
  'kick on 1 and on 4, the snare on the and of 2 only|lone snare on the and of 2',
  'kick on 1 and the 16th before 4, the snare on 2 and the and of 3|snare on 2 and the and of 3',
  'kick on 1 and the and of 2, a loud snare on 3, two quiet snare taps on the 16ths either side of 2|quiet taps around 2',
  'a low tom on 1 and the and of 1, a clap on 2 and 4, the kick only on 3|tom, tom, clap',
  'a clap on every beat, kicks on 1, the and of 2 and the 16th before 4|clap on all four',
  'kick a 16th after 1 and on 3, the snare on 2 and 4, beat 1 marked by a rim click|kick a 16th late off the one',
  'three evenly spaced snares across the bar against a kick on every beat|three snares over four kicks',
  'a two-bar pattern, kicks on 1 and 3 in both bars, the snare on 4 in the first bar and on 2 in the second|one snare per bar, trading places',
  'kick on 1, on 2 and the and of 2, the snare on 3, the fourth beat left bare|bare fourth beat',
  'kicks on 2 and 4, the snare on the and of 1 and the and of 3|kick and snare trade places',
  'a two-bar pattern, kicks on 1 and the and of 2 throughout, the first bar a snare on 2 and 4, the second bar a rim on 2 and the snare on the and of 3|second bar trips on the and of 3',
  'one long kick on 1, a clap on 2, a snare on 3, a clap on 4|clap, snare, clap',
];

/** Non-quantization, as a head tag. */
const LOOSE = ['played by hand, off the grid', 'unquantized drums', 'loose human timing', 'hand-played, a little uneven', 'late snare, early hats', 'wobbly human pocket', 'drums tapped out by hand', 'timing that leans and recovers', 'off-grid, unsnapped drums', 'drummer a little ahead, bass a little behind'];

/**
 * Injections: the one unplanned event of a track (a glitchy snare slap, a key change, a two-bar beat swap). It happens
 * once, with nothing leading up to it, and is described in the one lyrics section where it happens. A switch roll
 * needs none: its beat switch is the injection.
 */
const INJECTIONS = [
  'one snare hit bit-crushed and slapped back three times in a row, each echo shorter|the glitched snare slap',
  'the key lifts a half step and stays there, the drums unchanged|the half-step lift',
  'the whole beat sinks a whole step for one bar, tight, then is simply where it was|the one-bar key sink',
  'the kick pattern swaps to a new one for two bars, then the first pattern is simply there again|the two-bar swap',
  'every sound mutes for one beat, mid-bar, the bass tail gated|the one-beat mute',
  'the lead jams on one note for a beat like a frozen machine|the jammed note',
  'the snare is replaced by a struck metal hit for one bar|the metal bar',
  'the 808 whips up an octave and down again inside one beat|the octave whip',
  'the hats vanish for two bars and the kick doubles|the hatless bars',
  'the lead plays its phrase in mirror order, final note first, for one pass|the mirrored phrase',
  'the snare flams into five quick ghost hits and one loud one, by hand|the hand flam',
  'the whole mix collapses to mono in a phone-speaker band for one bar|the flattened bar',
  'the kick lands a full beat early, once, and the bar carries on around it|the early kick',
  'the feel halves for two bars, the snare on 3 only, the hats gone|the two-bar half-time',
  'the lead and the bass swap registers for one bar, the bass up high and the lead underneath|the register swap',
  'a single bar where the drums play the pattern of a different song, dry, then the loop again|the borrowed bar',
  'the snare slap pitched up a fifth and clipped for one hit only|the pitched slap',
];
const INJECTIONS_RESEARCHED = [
  'the snare is a hard digital slap, clipped and too loud, on its usual beat|the clipped snare slap',
  'the snare lands a 16th early and twice, a flam that sounds like a sampler misfire|the misfired snare',
  'two snares a 16th apart where one belongs, the second harder|the double slap',
  'the snare is answered by its own copy a 32nd later and an octave down|the shadowed snare',
  'a burst of bit-grain noise the length of a 16th stands in for the snare|the noise snare',
  'the whole mix is muted for one beat, the hole exactly where the snare would be|the muted beat',
  'everything but the bass is muted for a single bar|the bar of bass alone',
  'the lead holds a single flat note for a full bar while the drums carry on under it|the frozen bar of lead',
  'the lead plays its riff a tritone away from home for one pass|the pass a tritone off',
  'the riff is an octave higher for one pass|the pass an octave up',
  'for one bar the kick takes the snare\'s places and the snare takes the kick\'s|the swapped bar',
  'one bar plays through a telephone-thin filter, lows and highs removed, the pattern unchanged|the bar on a tiny speaker',
  'the mix folds to mono and bone dry for one bar|the narrow bar',
  'one bar plays at half speed with the pitch an octave down|the slowed bar',
  'the 808 holds one note for two whole bars while the drums play on|the long held 808',
];
INJECTIONS.push(...INJECTIONS_RESEARCHED);
const INJECTION_ROLES = ['once, in the second verse', 'once, in the last hook', 'once only, in the first verse', 'once in the whole song, in the second verse', 'once in the whole song, near the end', 'once, about halfway through the song'];

/**
 * Tight prompts (research/07): a short Style field of a dozen or so decisions, with the variety put where Suno
 * listens: a scene (era, region, subgenre) first, a tempo feel, a key colour instead of a tonic, one lateness phrase.
 */
const KEY_COLOUR = {
  minor: ['dark minor', 'cold minor key', 'minor, heavy and sour', 'low dark minor'],
  phrygian: ['dark minor, flat-second menace', 'Phrygian menace', 'minor with a flat second, tense'],
  dorian: ['restless minor with a raised sixth', 'Dorian minor, gritty'],
  major: ['major key, proud', 'major key, heavy', 'Mixolydian strut'],
};
const TEMPO_FEEL = {
  slow: ['slow heavy head-nod', 'slow and wide', 'crawling tempo'],
  mid: ['unhurried head-nod', 'mid-tempo, sitting back', 'steady walking pace'],
  up: ['pushing forward', 'quick and tense', 'uptempo, urgent'],
};
/** Human timing as one constant on one element (never a band word). */
const LATE = [
  'the snare always a hair late', 'the clap a touch behind the kick each time', 'hats played unevenly by hand, the same unevenness each bar',
  'the kick a hair early and the snare a hair late', 'the snare dragged late, the hats rushing ahead', 'the second kick of each bar always late',
  'the snare a touch early, the bass a touch late', 'the snare a few milliseconds late each time, the hats dead on the grid', 'the hats ahead of the beat, the kick behind it',
];
/** Where chart hip hop puts its energy, said as a physical result (research/08, section 1). One per tight roll. */
const SPECTRUM = [
  'a tuned 808 sitting very low and clean, one long round note under everything',
  'the sub felt in the chest more than heard, pure and steady',
  'a little grit on top of the 808 so every bass note reads on a phone speaker',
  'a short knocking kick with a hard tick on the front, sitting just above the 808',
  'kick and 808 taking turns, the kick\'s knock first and the 808\'s weight right behind it',
  'a dry cracking snare, a wooden thump under a sharp snap',
  'thin crisp hats tucked under the snare, airy on top',
  'the lead low but biting, nasal and buzzing, a hard biting edge on a low note',
  'the lead thin at the bottom, as if from a small speaker, clear of the bass',
  'a clear gap between the bass and the lead, the middle of the mix left open',
  'the lead forward and dead centre, the loudest thing after the snare',
  'bass and kick in the centre, mono, only the hats and one small sound out wide',
  'dry and close, short rooms, every tail kept short',
  'the whole beat reads on a small speaker, every part with an edge you can follow',
  'loud and dense, every hit the same strength, the snare on top',
];
const STEADY = ['same drums all song', 'one drum pattern, start to finish', 'steady pocket', 'the drums a two-bar loop'];

/** Title words. Titles are built from the roll's own sounds (lead handle, hint, key, motion) plus these, and the page never repeats one. */
const TITLE = {
  a: ['Glass', 'Rubber', 'Copper', 'Static', 'Velvet', 'Crooked', 'Paper', 'Neon', 'Iron', 'Loose', 'Hollow', 'Quiet', 'Gold', 'Bent', 'Midnight', 'Chrome', 'Backseat', 'Sunday', 'Low', 'Sour', 'Late', 'Drunk', 'Cold', 'Warm', 'Dead', 'Wet', 'Dry', 'Borrowed', 'Broken', 'Second', 'Last', 'Half', 'Slow', 'Heavy', 'Thin', 'Rusted', 'Burnt', 'Pale', 'Loud', 'Small', 'Blue', 'Grey', 'Velour', 'Tin', 'Brass', 'Nylon', 'Felt', 'Cheap', 'Stolen', 'Borrowed', 'Idle', 'Crooked', 'Patient', 'Nervous', 'Sideways', 'Upstairs', 'Lefthand', 'Evening', 'Early', 'Overdue'],
  b: ['Jaw', 'Halo', 'Receipt', 'Pulse', 'Lantern', 'Alibi', 'Engine', 'Teeth', 'Mirror', 'Ledger', 'Fuse', 'Orbit', 'Hinge', 'Tab', 'Signal', 'Crown', 'Ceiling', 'Pocket', 'Window', 'Porch', 'Motor', 'Hallway', 'Parade', 'Kitchen', 'Deposit', 'Weather', 'Elbow', 'Static', 'Ladder', 'Thermostat', 'Curfew', 'Sermon', 'Mileage', 'Stairwell', 'Sidewalk', 'Lamplight', 'Dial', 'Voltage', 'Tollbooth', 'Hangover', 'Invoice', 'Basement', 'Driveway', 'Weekday', 'Pavement', 'Rearview', 'Pillow', 'Ashtray', 'Doorframe', 'Umbrella', 'Napkin', 'Satellite', 'Interstate', 'Lullaby', 'Verdict', 'Gravel', 'Postcard', 'Freezer', 'Stopwatch', 'Oxygen'],
  shapes: ['AB', 'AB', 'AB', 'HB', 'AH', 'HH', 'ABB', 'KB'],
};

/** Shared instrument families any lane may borrow as a guest lead (experimental palette). */
const FAMILIES = {
  chart: {
    frame: 'one nameable sound, short and percussive, played as the hook over hard drums',
    sources: [
      'a rubber sole squeaking on a polished floor, two short squeaks tuned a fourth apart, close and dry|the sneaker squeak',
      'a windshield wiper dragging across dry glass, sampled and tuned, a two-note creak that falls a half step|the wiper creak',
      'a bed spring creak sampled and tuned to one pitch, four quick creaks per bar|the spring creak',
      'a drinking straw dragged through a plastic cup lid, the squawk tuned and cut to two notes|the straw squawk',
      'a tennis ball can popped open, the pop tuned and played as a three-note riff, dry|the can pop',
      'a metal ruler flicked on a desk edge so each note slides upward as it shortens, three flicks per bar|the ruler flick',
      'a shop counter bell tapped twice and choked by a palm, pitched down a fifth|the counter bell',
      'a stapler punched in time, a low tuned thud layered under each punch so it carries a note|the stapler punch',
      'a row of clay flower pots struck with a wooden spoon, each pot one note, a dry four-note riff|the clay pots',
      'an empty gas cylinder struck with a rubber mallet, a dull ringing note with a short metal tail, tuned to the root|the tank ring',
      'a brake drum hit with a hammer, a short dead clang, three hits tuned to a minor triad|the brake drum',
      'tuned rimshots on a tight snare shell, each one pitched, a five-note riff played where keys would be|the pitched rimshots',
      'a hollow log-drum note with a knocking front, played as the riff an octave above the bass|the log knock',
      'a length of plastic tube slapped on its open top, a deep round thwop, four tubes for four notes|the tube slap',
      'an old mobile phone ringtone of five square-wave notes, tinny and dry, played an octave down|the old ringtone',
      'a checkout scanner beep pitched into a three-note riff, each beep clipped square|the scanner beep',
      'a heart monitor beep pitched down to a soft square note, one per beat, one note a minor third up on beat 3|the monitor beep',
      'a short pitched zap that falls a fifth in an instant, used as a note, three zaps per bar|the zap',
      'a bit-reduced sine tone with coarse steps of grain, played as a slow two-note seesaw|the stepped tone',
      'a battery-starved toy keyboard whose notes sag flat as they sustain, one finger, three notes|the sagging toy keys',
      'a delivery truck warning beeper, one flat note pulsing on the off-beats, heard from across a street|the truck beeper',
      'a camera flash charging whine held as one thin note that bends a half step sharp|the flash whine',
      'an electric screwdriver squeezed in short bursts, the motor note tuned to the root and the fifth|the motor bursts',
      'an electric razor pressed against a tabletop, a low rasping note gated into short stabs|the razor rasp',
      'a subway door chime, two falling notes, sampled on a phone and pitched down a fourth|the door chime',
      'a turn-signal relay click with a tuned low thump under it, tick-tock on two pitches|the turn signal',
      'a pneumatic nail gun, each shot a hard tick with a pitched tail of air, tuned to three notes|the nail gun',
      'a rubbery square-wave bass synth riff played an octave too high and too fast, dry and mono, five notes then a gap|the hyper bass riff',
      'a hard-clipped saw synth playing one three-note hook, spread wide to both sides, the centre left for the kick|the wide saw hook',
      'a sine-wave lead gliding slowly between two notes, a little distortion so it buzzes on small speakers|the gliding sine',
      'a square-wave synth pluck with the filter almost shut, each note a soft knock with a short bright tick on top|the muffled pluck',
      'a two-oscillator synth stab tuned slightly apart so the pitches beat against each other, one stab on the and of 2|the beating stab',
      'a filter pinged by a click so it rings one short note, tuned for each note of a four-note riff|the filter chirp',
      'a noise burst through a tuned comb filter so it carries a pitch, short and breathy, five hits per bar|the tuned noise hit',
      'a synth lead that glides up a whole octave on the first note of the riff and stays there|the octave glide',
      'a sped-up stab of massed strings and brass, two notes, the low end filtered off so it is thin and urgent|the thin stab',
      'a brass-band horn hit sampled from a parade, one fat chord pitched down a third and cut to an eighth|the parade hit',
      'a trumpet phrase with the low end filtered away so it sounds like a tin speaker, four notes, mono|the tin-speaker trumpet',
      'a double-reed horn sample, nasal and piercing, pitched down a fifth so it growls, three notes|the nasal reed',
      'one piano note stretched to ten times its length until it turns into a grainy held tone|the stretched grain tone',
      'a guitar feedback squeal caught in a sampler and played from pads, the same squeal at four pitches|the sampled feedback',
      'a plastic toy horn, one braying flat note bent a half step by the lips, short blasts|the toy horn',
      'a duck call squeezed into three pitches, nasal and rude, played low|the duck call',
      'a kazoo through a small amp, doubled an octave down, a buzzing four-note riff|the amp kazoo',
      'a length of plastic drainpipe blown like a horn, a rough low honk, two honks per bar|the drainpipe honk',
      'a conch shell blown as one long hollow note that cracks upward as the breath runs out|the shell horn',
    ],
    character: ['a hard biting edge on a low note', 'thin at the bottom, as if from a small speaker', 'dry and close, every tail kept short', 'hard-clipped so it reads on a phone speaker', 'gated so each note ends in an instant', 'tuned by ear, a few cents off', 'dead centre and loud', 'resampled until the grain shows'],
  },
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
const GUEST_FAMILIES = [['chart', 8], ['found', 2], ['synth', 2], ['guitar', 1], ['organ', 1], ['wind', 1]];

/**
 * Attitude: the posture of the record. strut = head up, absolute confidence; cold = menace; low = reflective;
 * wild = unhinged. Each adds a head tag, steers timing and motif words, overlays drums, and (for strut) strips
 * every sad, dragging or mournful phrase from the pools.
 */
const ATTITUDE = {
  strut: {
    tags: ['head up, chest out', 'absolute confidence, every hit lands square', 'strutting, unbothered', 'cocky bounce, shoulders back', 'proud and loud, the snare like a slap', 'walks in like it owns the room', 'grinning, sure of itself', 'big, upright, head-nod strut'],
    human: ['every hit lands exactly where it means to, dead center on the one', 'played like someone who knows it is good', 'pushed a hair ahead of the beat, impatient, sure', 'the accent on the one, hard, every time', 'loud and even, certain from the first hit', 'each note placed like a step, heavy and certain', 'the second note louder than the first, on purpose'],
    motif: ['a two-note strut, root and fifth, landing square on the one', 'a four-note riff that climbs and stays up there', 'a major-pentatonic riff that grins', 'a brass-style stab figure on 1 and the and of 2, proud', 'a three-note hook that lands on the root every time, hard', 'one big note on the one, held, then two short ones answering it', 'a riff that walks up the scale and plants its feet on the fifth', 'a two-chord stomp, both chords major, the second one louder'],
    drums: {
      kick: ['a kick that stomps on the one, hard and square, the room shaking', 'a big kick on 1 and 3, dead on, nothing fancy, heavy', 'a kick like a boot on a wooden floor, on the one every bar', 'two kicks on the one, stacked, proud'],
      snare: ['a snare like a slap, dead on 2 and 4, loud', 'a cracking snare with a clap stacked on it, square on the beat, cocky', 'a fat snare hit hard and even, the ring left in, up front', 'a snare that cracks on 2 and 4 like it means it'],
      hats: ['hats in straight 8ths, tight, marching', 'a tambourine on every 8th, loud and even', 'hats that open on the and of 4, bragging', 'tight hats in 16ths with the accent on every downbeat'],
    },
    mix: 'loud and sure, the snare up front, wide',
    bass: ['a bass that walks in like it owns the place, two notes per bar, dead on', 'an 808 that lands on the one every bar, long and proud', 'a bass playing root and fifth, square, heavy'],
    sad: /\b(mournful|sad|sadness|funeral|crying|cries|lament\w*|murky|tired|drag\w*|sag\w*|wheez\w*|broken|dead-tuned|weep\w*|lonely|lazy|drunk\w*|stumbl\w*|slur\w*|trips?|hesitat\w*|sleepy|woozy|seasick|wobbl\w*|sour|wrong|dying|exhaust\w*|limp|behind the beat|a hair late|late and heavy|pulled late|leaning late|landing late|played late|a hair behind|dragging)\b/i,
  },
  cold: {
    tags: ['cold, menacing, unhurried', 'dead-eyed, heavy, patient', 'threatening, the drums like footsteps', 'cold and still, the 808 doing the talking'],
    human: ['every hit placed and left there, unhurried', 'patient, nothing rushed, nothing late', 'played still, the accents flat and even'],
    motif: [],
    drums: {},
    mix: null,
    bass: [],
    sad: /\b(grin\w*|cheeky|playful|sunny|giddy|bright|happy)\b/i,
  },
  low: {
    tags: ['low, reflective, head down', 'quiet and sure, nothing to prove', 'heavy-lidded, slow, inward'],
    human: [],
    motif: [],
    drums: {},
    mix: null,
    bass: [],
    sad: null,
  },
  wild: {
    tags: ['unhinged, manic, off the leash', 'twitchy and fearless', 'wrong on purpose, loud about it'],
    human: ['played too fast and too hard, then suddenly not', 'every hit a different force, wild'],
    motif: [],
    drums: {},
    mix: null,
    bass: [],
    sad: null,
  },
};
const ATTITUDE_WEIGHTS = [['strut', 40], ['cold', 25], ['low', 20], ['wild', 15]];

const MIX_TAG = 'crisp punchy radio mix';

module.exports = { LEAD, BASS, TWITCH, GROOVE, SWITCH, COLOR, COUNTER_ROLE, RHYTHM, ANTI_TRANSITION, ANTI_TAGS, TITLE, FAMILIES, GUEST_FAMILIES, MIX_TAG, ATTITUDE, ATTITUDE_WEIGHTS, CADENCE, LOOSE, INJECTIONS, INJECTION_ROLES, KEY_COLOUR, TEMPO_FEEL, LATE, STEADY, SPECTRUM };
