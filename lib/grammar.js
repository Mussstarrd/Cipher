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
    
    
    'legato runs that slur into each other',
    'ghosted notes with one hard accent per bar',
    'stabs that only answer the snare',
    'three quick notes landing on beat 1, then silence',
    'quarter-note triplets over straight drums',
    
    'long tones that start a 16th late',
    'two stabs, then two bars of silence',
    'staggered 8ths with every third note muted',
    
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
    'a seven-note run hopping over every other scale step',
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
    
    'one take with the small mistakes left in',
    'notes overlapping a little, like a player who holds too long',
    'the accent moving to a new note each pass',
    'pressure rising and fading unevenly under the fingers',
    'timing pulled behind the snare',
    'loose and slightly ahead of the kick',
    'a little drunk on the timing but always landing the one',
    'released early on the off-beats, so the gaps breathe',
    'bent into pitch by hand, never landing in tune twice',
    'played like someone tapping it out on a car dashboard',
    'two takes layered a few milliseconds apart',
    'the hard notes hit hard and the soft ones barely touched',
    'played with one finger, stiff and stubborn',
    'the first note of each phrase a touch early, like someone eager',
    'a tiny pause before the last note, every time',
    'pressed harder as the phrase goes on',
    'played a little faster than it should be, then caught',
    'each repeat played again by hand, so two passes are never alike',
  ],
  motion: [
    'it gets a little brighter each time the hook comes back',
    'it bends down a half step at the end of every phrase',
    
    'the detune widens as the loop goes on',
    'the notes get shorter as the song goes on',
    'a second copy an octave down, louder in the hook',
    
    'it gets grittier each pass',
    'one note changes each pass, so it never repeats exactly',
    'it thins to half its notes in the verse, whole in the hook',
    'it drifts up a few cents over 16 bars, then snaps back',
    'hit softer, then suddenly hard on the last pass',
    'it stays frozen and identical while the drums move',
    'each pass one note is muted, a different one each time',
    'it loses its first note halfway through the song',
    'the echo on its last note gets longer each section',
    'it moves an octave down for one section, then back',
    'it plays twice as dry in the second half of the song',
    'its last note is held a beat longer each time around',
    'it starts dark and muffled and clears a little each section',
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
    
    
    'silent for 2 bars, then hits',
    'three notes then nothing',
    'root and flat second only',
    
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
    'a short noise burst', 'a pitched-up knock blip', 'a dry wooden click', 'a shaker ruffle',
    'a gated snare tail cut short', 'a tiny pitched perc ping', 'a rimshot tick', 'a tongue-click sample',
    'a coin spun on a table and cut short', 'a lighter flick', 'a snapped rubber band', 'a pen tapped on a desk', 'a typewriter key strike',
    'a camera shutter click', 'a tight closed-hat chick', 'a bottle-cap rattle', 'a knuckle knock on a door', 'a dry stick click',
  ],
  rhythm: [
    'in 5-hit bursts that skip the grid', 'repeating every 3 16ths against the bar',
    'in uneven pairs, long-short', 'in a quick double hit right before the snare', 
    'in single hits that land in random 16th slots', 'flamming against the hats',
    'in a stop-start pattern, 3 hits then 2 rests', 'in a burst that slows down as it decays',
    'in a quick three-hit flurry', 'in two fast hits and one late one', 'doubling up for one beat then vanishing', 'in one hit, a gap, then four fast ones',
    'ratcheting faster across one beat', 'in a rapid burst cut off mid-beat', 'in a lopsided long-short-short pattern', 'echoing itself twice, each echo quieter',
    'in pairs that land just off the 16th grid', 'in a 16th run that skips its last hit',
  ],
  where: [
    'between the kick and snare', 'only in the gaps the lead leaves',
    'answering the snare', 'quietly through the verse', 'only after the switch',
    'on beat 3, never the same way twice', 'around the 808 slides', 'under the hats, barely there',
    'in the hole after the snare', 'only when the lead rests', 'tucked behind the snare', 'in the silence after the kick', 'halfway between beats 2 and 3',
  ],
};

const GROOVE = {
  sync: [
    'kicks pushed off the beat, snare dead on', 'everything leans on the and', 'the kick avoids beat 1 through the verse',
    'half-time snare under double-time hats',
    'a broken kick pattern that never repeats twice',
    'kick syncopation that tracks the lead\'s rhythm', 'a gap on beat 1 so the downbeat hits harder', 'off-beat everything except the snare',
    
    'two kicks jammed into the last 16ths before the snare', 'the kick answers the snare instead of leading it',
    'the 808 hits alone where the kick should be', 'the kick pattern is three hits long and restarts wherever it lands', 
    'the kick doubles only when the lead rests', 'the snare stays put while the kick lands somewhere new each bar',
  ],
  human: [
    'unquantized, snare a hair late', 'hats rushing slightly ahead', 'kick drifting behind the grid', '16ths pushed slightly late for a lazy bounce',
    'a loose pocket, played in, never snapped', 'clap layers a few milliseconds apart', 'quiet uneven extra snare taps',
    'percussion played live over the loop', 'the whole loop sitting back on the beat', 'tight kick, loose everything else', 'a slight push into each snare',
    'the hats dragged a few milliseconds behind the kick', 'the snare landing late and heavy like a tired drummer',
    'the snare a hair late and hit harder for it, its ring left to decay',
    'hats dead straight on the grid, the kick leaning late behind them, the snare a hair early so the pocket feels pulled two ways',
    'played by hand against the bass, the second hit always a hair late',
    'the kick dead on the grid and everything else a hair loose around it',
    'each hat hit a different loudness, like a hand that tires',
    'the clap a few milliseconds ahead of the snare under it',
    'ghost notes on the snare so quiet they are felt more than heard',
    'the kick played by hand, the second of each pair softer',
    'hats that speed up a hair into the snare and settle back',
    'one snare hit per phrase noticeably harder than the rest',
    'the kick and the bass struck as one sound, the snare free of both',
  ],
  // The beat-3 drunken triplet slur: the fingerprint, in hip hop language. Never "waltz", never "3/4".
  triplet: [
    'the 808 slurs into a triplet on beat 3', 'a three-note stumble every third beat', 'the kick trips over itself on beat 3',
    'the hats slur into threes on beat 3, then straighten', 'a drunk triplet 808 bounce on the third beat',
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
    { tag: 'beat flips at the midpoint', cue: 'the kick pattern flips to the off-beats, the lead an octave lower', after: 'off-beat kicks, lower lead' },
  ],
  land: [
    
    
    
    
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
  
  'doubling the last note of each lead phrase',
  'only after the switch, never before',
  'a three-note reply in the gaps the lead leaves',
  
];

/** Front tags. Suno weights the first ~260 characters and reads them as tags. */
const RHYTHM = {
  sync: ['off-grid kick pattern', 'kicks pushed off the beat', 'kick missing from the downbeat', 'broken kick pattern', 'snare dead on, kick early', 'hats in uneven 16ths', 'syncopated double kicks', 'off-beat 808 hits', 'kick landing on the and', 'snare early, 808 late', 'kick late into beat 3', 'double kick before the snare', 'kick on the and of 2', '808 landing between the kicks', 'snare pushed a 16th early', 'kick missing from beat 3', 'stop-start kick pattern', 'kick answering the snare', 'kick and 808 never together', 'lopsided kick pattern', 'downbeat left empty', 'kick pattern three hits long'],
  twitch: ['twitchy percussion', 'nervous clicking percussion', 'clipped hat bursts', 'jittery metallic ticks', 'stop-start hat stabs', 'flamming perc hits', 'broken percussion loop', 'nervous perc ticks between kicks', 'twitchy off-grid clicks', 'nervous stop-start ticks', 'jittery 32nd knock bursts', 'ticking perc in odd bursts', 'perc hiccups between snares', 'skittering rim clicks', 'perc flams off the grid', 'jumpy knock bursts', 'restless ticking percussion'],
  slur: ['drunk 808 bounce', 'slurred triplet bounce', 'a stumble on beat 3', '808 slurring into beat 3', 'three-note stumble on beat 3', 'triplet stumble on beat 3', 'drunken bounce pocket', 'hats dragging on beat 3', 'lazy triplet 808 slide', 'stumbling triplet kicks', 'the pocket slips drunk on beat 3', ],
  low: ['low register lead', 'deep low-mid lead', 'lead under the snare', 'lead an octave above the 808', 'low dark lead melody', 'low-mid lead, top cut off', 'lead parked in the low mids', 'deep baritone melody', 'lead below the hats', 'lead tucked above the 808'],
  loop: ['one hypnotic loop', 'minimal one-loop beat', 'hypnotic repeating loop', 'single loop, never changes', 'one-bar loop, locked', 'minimal, one riff only', 'same two bars all song', 'two-bar loop, hypnotic', 'stripped minimal loop'],
  straight: ['steady pocket start to finish', 'one pocket, locked in', 'the beat never lets up', ],
};

/** Always one of these in the drums line. The positive form of "no fills, no transitions". */
/** Short head-tag forms of the anti-transition rule; one always sits in the front tags. */
const ANTI_TAGS = ['one drum pattern all song', 'every bar of drums identical', 'drums on a frozen loop', 'the beat stays frozen', 'steady unchanging drums', 'same drums start to finish', 'drums playing from the first second', 'the drum loop never varies'];

const ANTI_TRANSITION = [
  'one drum pattern from the first second to the final one',
  'every bar of drums identical',
  'the drums a two-bar loop that never varies',
  'the same kick and snare pattern all song',
  'the beat stays frozen from start to finish',
  'the drums a machine loop, bar one the same as bar one hundred',
  'the drum loop runs flat all the way through',
  'kick and snare locked in one pattern for the whole song',
  'the drums play the same two bars and only those',
  'steady drums, the same hits in the same places every bar',
  'the drums already playing at the first second and unchanged at the end',
  'one loop of drums on repeat, the melody doing all the changing',
];

/** One small, safe change per section. The lyrics renderer uses these when a feel or motion cue would otherwise repeat. */
const SECTION_CUES = [
  'the lead a hair louder',
  'the kick hit a touch harder',
  'the hats a little quieter',
  'the snare left to ring a little longer',
  'the bass holding its notes longer',
  'the lead played an octave lower',
  'the lead doubled an octave up, quiet',
  'the lead more clipped',
  'one note of the lead left unplayed',
  'the snare drier and closer',
  'everything a touch quieter except the kick',
  'the bass up front, the lead tucked behind it',
  'the lead answered by a quiet echo of itself',
];

/** The steady phrase for a roll with two drum patterns. */
const ANTI_SWITCH = ['two drum patterns, each one fixed for its half of the song', 'pattern A unchanged for the first half, pattern B unchanged for the second', 'each half of the song one frozen drum loop', 'the drums hold one pattern, then hold another, and that is all they do'];

/** Empty space, said as space. Sparse rolls carry one of these in the drums line and the first lyrics tag. */
const SPACE = [
  'half of every bar left empty', 'long rests between the hits', 'the gaps as loud as the hits', 'two beats of open air in every bar',
  'each hit left to ring into quiet', 'more quiet than sound', 'three sounds and the air between them', 'the bar mostly empty, each hit a small event',
  'the room heard between the hits', 'so sparse the bass tail is heard dying away', 'sparse enough to count every hit', 'wide open, unhurried, mostly air',
];
const SPACE_TAGS = ['sparse, mostly empty space', 'wide open, lots of air', 'minimal, long rests', 'skeletal, room between every hit', 'bare, three sounds and silence', 'spacious and dry', 'empty space between the hits', 'quiet between every hit'];

/**
 * Drum cadences: where the kick and snare land, said in beats. Rolled so the bar is not the genre default
 * (snare on 2 and 4, a busy kick). The text goes in the drums line, the tag replaces the syncopation tag in the head.
 * Each is one fixed pattern for every bar: nothing here sits against the end of a bar.
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
];

/** Non-quantization, as a head tag. */
const LOOSE = ['played by hand, off the grid', 'unquantized drums', 'loose human timing', 'hand-played, a little uneven', 'late snare, early hats', 'wobbly human pocket', 'drums tapped out by hand', 'timing that leans and recovers', 'off-grid, unsnapped drums', 'drummer a little ahead, bass a little behind'];

/** Title words. Titles are built from the roll's own sounds (lead handle, hint, key, motion) plus these, and the page never repeats one. */
const TITLE = {
  a: ['Glass', 'Rubber', 'Copper', 'Static', 'Velvet', 'Crooked', 'Paper', 'Neon', 'Iron', 'Loose', 'Hollow', 'Quiet', 'Gold', 'Bent', 'Midnight', 'Chrome', 'Backseat', 'Sunday', 'Low', 'Sour', 'Late', 'Drunk', 'Cold', 'Warm', 'Dead', 'Wet', 'Dry', 'Borrowed', 'Broken', 'Second', 'Last', 'Half', 'Slow', 'Heavy', 'Thin', 'Rusted', 'Burnt', 'Pale', 'Loud', 'Small', 'Blue', 'Grey', 'Velour', 'Tin', 'Brass', 'Nylon', 'Felt', 'Cheap', 'Stolen', 'Borrowed', 'Idle', 'Crooked', 'Patient', 'Nervous', 'Sideways', 'Upstairs', 'Lefthand', 'Evening', 'Early', 'Overdue'],
  b: ['Jaw', 'Halo', 'Receipt', 'Pulse', 'Lantern', 'Alibi', 'Engine', 'Teeth', 'Mirror', 'Ledger', 'Fuse', 'Orbit', 'Hinge', 'Tab', 'Signal', 'Crown', 'Ceiling', 'Pocket', 'Window', 'Porch', 'Motor', 'Hallway', 'Parade', 'Kitchen', 'Deposit', 'Weather', 'Elbow', 'Static', 'Ladder', 'Thermostat', 'Curfew', 'Sermon', 'Mileage', 'Stairwell', 'Sidewalk', 'Lamplight', 'Dial', 'Voltage', 'Tollbooth', 'Hangover', 'Invoice', 'Basement', 'Driveway', 'Weekday', 'Pavement', 'Rearview', 'Pillow', 'Ashtray', 'Doorframe', 'Umbrella', 'Napkin', 'Satellite', 'Interstate', 'Lullaby', 'Verdict', 'Gravel', 'Postcard', 'Freezer', 'Stopwatch', 'Oxygen'],
  shapes: ['AB', 'AB', 'AB', 'HB', 'AH', 'HH', 'ABB', 'KB'],
};

/** Shared instrument families any lane may borrow as a guest lead (experimental palette). */
const FAMILIES = {
  world: {
    frame: 'sampled and tuned over hard drums',
    sources: [
      'a koto plucked hard with the string buzz left in, pitched down into the mids|the koto',
      'an oud picking a low run, the pick click as loud as the note|the oud',
      'a sitar sample with its sympathetic buzz, one bent note per bar|the sitar buzz',
      'a banjo plucked dead and dry, three notes, tuned down a fourth|the low banjo',
      'a hammered dulcimer sample struck with sticks, metallic and short|the dulcimer',
      'a one-wire gourd bow twanged with a stick, the pitch bent with a stone|the wire twang',
      'a steel pan sample pitched down an octave until it clanks|the low steel pan',
      'an accordion squeezed in short pushes, two chords, reedy|the accordion',
      'a bagpipe drone sample pitched down, one flat note held under everything|the pipe drone',
      'a gourd lute thumbed low, dry plucks that ring short|the gourd lute',
      'a shamisen snapped with a heavy pick, each note with a slap|the shamisen',
      'a zither strummed once per bar, the strings left to buzz|the zither strum',
    ],
    character: ['tuned by ear, a few cents off', 'pitched down until the wood is heard', 'close-miked, the pick and the fingers audible', 'looped a little unevenly, the seam left in', 'dry, the room left behind', 'doubled an octave down', 'played stiffly, like a first take'],
  },
  machine: {
    frame: 'sampled and tuned over hard drums',
    sources: [
      'a dot-matrix printer sampled and pitched into a melody, each note a buzz|the printer buzz',
      'a dial-up modem tone resampled into three notes|the modem tone',
      'a sewing machine motor pitched to the key, starting and settling on each note|the sewing motor',
      'a sonar ping tuned down, one ping per bar with its long tail|the sonar ping',
      'a basketball bounced in an empty hall, tuned to the root, the sneaker squeaks left in|the ball bounce',
      'a spray-can rattle and hiss shaped into a three-note figure|the spray can',
      'a typewriter bell and carriage sampled as two notes, high and low|the typewriter bell',
      'an engine idle pitched into a low growling note that steps down|the engine note',
      'a microwave beep tuned down a fifth, three beeps|the microwave beeps',
      'a pool-ball click sampled and pitched, hard and dry|the pool-ball clicks',
      'a zipper pulled fast, pitched into two notes|the zipper',
      'a wrench struck on a steel pipe, ringing a single note|the pipe ring',
    ],
    character: ['resampled until the grain shows', 'tuned by ear, a few cents off', 'gated so each note is short', 'pitched down until it sounds heavy', 'hard-clipped on every hit', 'looped by hand, slightly uneven', 'left raw, the room it was taped in still on it'],
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
    character: ['resampled until the grain shows', 'tuned by ear, a few cents off', 'gated so each note is short', 'pitched down until it sounds heavy', 'hard-clipped on every hit', 'doubled by a distorted 808 on the root'],
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
      'a pitched-up soul guitar sample one bar long|the soul guitar sample',
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
const GUEST_FAMILIES = [['found', 2], ['synth', 2], ['guitar', 1], ['organ', 1], ['wind', 2], ['world', 4], ['machine', 3]];

/**
 * Attitude: the posture of the sample. strut = head up, absolute confidence; cold = menace; low = reflective;
 * wild = unhinged. Each adds a head tag, steers timing and motif words, overlays drums, and (for strut) strips
 * every sad, dragging or mournful phrase from the pools.
 */
const ATTITUDE = {
  strut: {
    tags: ['head up, chest out', 'absolute confidence, every hit lands square', 'strutting, unbothered', 'cocky bounce, shoulders back', 'proud and loud, the snare like a slap', 'walks in like it owns the room', 'grinning, sure of itself', 'big, upright, head-nod strut', 'chin up, slow walk, sure of every step', 'winner\'s bounce, loud and easy', 'takes its time because it can', 'flexing, loose, in charge', 'victory lap bounce', 'swagger in the low end, a grin on top'],
    human: ['every hit lands exactly where it means to, dead center on the one', 'played like someone who knows it is good', 'pushed a hair ahead of the beat, impatient, sure', 'the accent on the one, hard, every time', 'loud and even, certain from the first hit', 'each note placed like a step, heavy and certain', 'the second note louder than the first, on purpose'],
    motif: ['a two-note strut, root and fifth, landing square on the one', 'a four-note riff that climbs and stays up there', 'a major-pentatonic riff that grins', 'a brass-style stab figure on 1 and the and of 2, proud', 'a three-note hook that lands on the root every time, hard', 'one big note on the one, held, then two short ones answering it', 'a riff that walks up the scale and plants its feet on the fifth', 'a two-chord stomp, both chords major, the second one louder'],
    drums: {
      kick: ['a kick that stomps on the one, hard and square, the room shaking', 'a big kick on 1 and 3, dead on, nothing fancy, heavy', 'a kick like a boot on a wooden floor, on the one every bar', 'two kicks on the one, stacked, proud'],
      snare: ['a snare like a slap, dead on 2 and 4, loud', 'a cracking snare with a clap stacked on it, square on the beat, cocky', 'a fat snare hit hard and even, the ring left in, up front', 'a snare that cracks on 2 and 4 like it means it'],
      hats: ['hats in straight 8ths, tight, marching', 'a tambourine on every 8th, loud and even', 'tight hats in 16ths with the accent on every downbeat'],
    },
    mix: 'loud and sure, the snare up front, wide',
    bass: ['a bass that walks in like it owns the place, two notes per bar, dead on', 'an 808 that lands on the one every bar, long and proud', 'a bass playing root and fifth, square, heavy'],
    sad: /\b(mournful|sad|sadness|funeral|crying|cries|lament\w*|murky|tired|drag\w*|sag\w*|wheez\w*|broken|dead-tuned|weep\w*|lonely|lazy|drunk\w*|stumbl\w*|slur\w*|trips?|hesitat\w*|sleepy|woozy|seasick|wobbl\w*|sour|wrong|dying|exhaust\w*|limp|behind the beat|a hair late|late and heavy|pulled late|leaning late|landing late|played late|a hair behind|behind the kick|dragging)\b/i,
  },
  cold: {
    tags: ['cold, menacing, unhurried', 'dead-eyed, heavy, patient', 'threatening, the drums like footsteps', 'cold and still, the 808 doing the talking', 'ice calm, the drums doing the threatening', 'slow, heavy, eyes forward', 'quiet menace, nothing wasted', 'stone-faced and loud'],
    human: ['every hit placed and left there, unhurried', 'patient, nothing rushed, nothing late', 'played still, the accents flat and even'],
    motif: [],
    drums: {},
    mix: null,
    bass: [],
    sad: /\b(grin\w*|cheeky|playful|sunny|giddy|bright|happy)\b/i,
  },
  low: {
    tags: ['low, reflective, head down', 'quiet and sure, nothing to prove', 'heavy-lidded, slow, inward', 'late-night, private, steady', 'thinking it over, head nodding', 'calm and close, a little bruised', 'eyes down, steady hands'],
    human: [],
    motif: [],
    drums: {},
    mix: null,
    bass: [],
    sad: null,
  },
  wild: {
    tags: ['unhinged, manic, off the leash', 'twitchy and fearless', 'wrong on purpose, loud about it', 'feral, grinning, about to tip over', 'restless, jumpy, daring', 'loud, crooked, having fun', 'reckless and precise at once'],
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

module.exports = { LEAD, BASS, TWITCH, GROOVE, SWITCH, COLOR, COUNTER_ROLE, RHYTHM, ANTI_TRANSITION, ANTI_TAGS, TITLE, SECTION_CUES, ANTI_SWITCH, SPACE, SPACE_TAGS, CADENCE, LOOSE, FAMILIES, GUEST_FAMILIES, MIX_TAG, ATTITUDE, ATTITUDE_WEIGHTS };
