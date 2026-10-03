/* MANHOLE vocabulary. Crisp minimalist hip hop with a waltz cadence riding
 * trap drums. Reference names live in `refs` and `aliases` for the UI and
 * the lint only; nothing named ever reaches the prompt. Every phrase is
 * instrumental and modern: no voices, no FX words, no film-score words. */
"use strict";

const ManholeData = (() => {
  const STYLE_LIMIT = 1000;
  const LYRICS_LIMIT = 5000;

  const MINOR_KEYS = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];
  const MAJOR_KEYS = ["C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B"];

  // [semitones above tonic, chord suffix]
  const PROGRESSIONS = [
    { id: "pendulum", tonality: "minor", roman: "i – ♭VI", chords: [[0, "m"], [8, ""]], color: "a two-chord minor see-saw", cadence: "Never resolves; the ear keeps leaning in." },
    { id: "phrygian", tonality: "minor", roman: "i – ♭II", chords: [[0, "m"], [1, ""]], color: "a half-step Phrygian creep", cadence: "♭II drops a half step into the tonic: pure menace." },
    { id: "drone", tonality: "minor", roman: "i (pedal)", chords: [[0, "m"]], color: "a one-chord minor drone", cadence: "No cadence at all; the rhythm does the moving." },
    { id: "sting", tonality: "minor", roman: "i – ♭VI – V", chords: [[0, "m"], [8, ""], [7, ""]], color: "a harmonic-minor loop with a tense major V", cadence: "The leading tone snaps the loop back to i every time." },
    { id: "lament", tonality: "minor", roman: "i – ♭VII – ♭VI – V", chords: [[0, "m"], [10, ""], [8, ""], [7, ""]], color: "a falling four-chord minor bassline", cadence: "The bass walks down and parks on V." },
    { id: "sway", tonality: "minor", roman: "i – iv", chords: [[0, "m"], [5, "m"]], color: "a slow minor i–iv sway", cadence: "iv to i: soulful release without sweetness." },
    { id: "blues", tonality: "minor", roman: "i7 – iv7 – v7", chords: [[0, "m7"], [5, "m7"], [7, "m7"]], color: "a greasy minor-blues turnaround", cadence: "Minor v7 back to i7: a barroom shrug, never a bow." },
    { id: "dorian", tonality: "minor", roman: "i7 – IV7", chords: [[0, "m7"], [5, "7"]], color: "a Dorian two-chord vamp", cadence: "The major IV inside a minor key: warm and sly." },
    { id: "soul", tonality: "major", roman: "IVmaj7 – iii7 – vi9", chords: [[5, "maj7"], [4, "m7"], [9, "m9"]], color: "dusty soul chords sliding down to the relative minor", cadence: "Lands on vi: the bittersweet story-time resolution." },
    { id: "backdoor", tonality: "major", roman: "iv7 – ♭VII9 – Imaj7", chords: [[5, "m7"], [10, "9"], [0, "maj7"]], color: "warm borrowed-chord changes", cadence: "The backdoor home: warm, never bright." },
    { id: "bounce", tonality: "major", roman: "I – ♭VII", chords: [[0, ""], [10, ""]], color: "a cocky two-chord major bounce", cadence: "♭VII to I: grinning, unbothered." },
  ];

  // ------------------------------------------------------------ reference DNA
  // Each roll takes one DNA as the lead identity and borrows one trait from a
  // second. `refs` is what the UI shows; `aliases` is what the lint blocks.
  // Vocabulary rule: modern hip hop production words only. No film-score,
  // world, folk or lo-fi sound-design words; those turn Suno into a soundtrack.
  const DNA = [
    {
      id: "dark-minimal", label: "Dark minimal", refs: "Big Sean · Blessings",
      aliases: ["big sean", "sean don", "blessings", "vinylz", "allen ritter"],
      bpm: [128, 148], feel: "slow menacing half-time", tonalities: ["minor"],
      openers: [
        "dark minimal trap: one menacing synth riff, a huge 808 that waits a whole bar to drop, hard sparse drums and dead silence before every hook",
        "spacious menacing trap: a dark synth pad holding two notes, hard sparse drums, and stop-start gaps that leave room for the punchline",
      ],
      trait: "stop-start gaps that leave a full beat of silence for the punchline",
      low: ["huge 808 that waits a whole bar to drop", "808 that rings long and dark under the riff"],
      leads: ["menacing synth riff", "dark synth-pad riff, two notes", "dark detuned synth-bell riff"],
    },
    {
      id: "one-loop", label: "One-loop hypnosis", refs: "Lil Wayne · A Milli",
      aliases: ["lil wayne", "weezy", "wayne", "a milli", "bangladesh"],
      bpm: [140, 154], feel: "bare hypnotic bounce", tonalities: ["minor"],
      openers: [
        "brutally minimal hypnotic trap: one stubborn two-note loop repeating forever, a trunk-slapping 808, a snap and a kick, nothing else",
        "a one-idea beat: the same two-note figure for the entire track over bare hard drums and a booming 808 that rattles the windows",
      ],
      trait: "one stubborn two-note loop that never changes",
      low: ["trunk-slapping 808 that rattles the windows", "booming 808 hitting with every kick"],
      leads: ["two-note synth-brass loop", "two-note distorted synth loop", "two-note electric-guitar loop"],
    },
    {
      id: "devil-bounce", label: "Devil-on-the-shoulder bounce", refs: "JID · McAfee",
      aliases: ["jid", "j.i.d", "j.i.d.", "mcafee", "boi-1da", "boi1da", "cubeatz", "dreamville"],
      bpm: [150, 166], feel: "dark rubbery uptempo bounce", tonalities: ["minor"],
      openers: [
        "a bouncy dark uptempo beat like a devil whispering over your shoulder: rubbery bass, dry snappy drums, staccato stabs that jump in and out of the pocket",
        "dark twitchy uptempo bounce made for rapid-fire staccato flows: elastic bass, off-kilter hats, a sinister plucked synth riff, sudden drop-outs",
      ],
      trait: "staccato stabs that jump in and out of the pocket",
      low: ["rubbery elastic bass bending between notes", "bouncing 808 sliding into every root"],
      leads: ["sinister plucked synth riff", "staccato synth-brass stab riff", "dark muted electric-guitar riff"],
    },
    {
      id: "triumph", label: "Triumphant thunder", refs: "JID · Glory",
      aliases: ["jid", "lex luger", "luger", "beatnick dee"],
      bpm: [138, 150], feel: "huge booming half-time", tonalities: ["minor"],
      openers: [
        "triumphant hard trap: big brass stabs and a dark synth chord loop over booming Atlanta drums, rattling hats and an 808 that rumbles like thunder",
        "huge victorious trap beat: a dark synth chord loop, punchy brass hits, machine-gun hats and a long distorted 808, grand and wounded at once",
      ],
      trait: "big brass stabs on the hook",
      low: ["808 rumbling like thunder", "long distorted 808 tail"],
      leads: ["dark synth chord loop", "big brass-stab riff", "punchy string-stab riff"],
    },
    {
      id: "loyalty-heavy", label: "Loyalty heavy", refs: "JID · Bruddanem",
      aliases: ["jid", "bruddanem", "lil durk", "durk", "dj khalil", "forever story"],
      bpm: [130, 150], feel: "heavy wounded half-time", tonalities: ["minor"],
      openers: [
        "a hard heavy loyalty beat: detuned piano chords, airy high keys drifting over the top, a dark synth pad doubling the chords, drums that hit like a vow",
        "heavy and wounded trap: a detuned piano chord loop, a soft high keys figure, a synth pad doubling the chords, booming drums under all of it",
      ],
      trait: "detuned piano chords with soft high keys drifting over the top",
      low: ["deep 808 holding each chord root", "heavy 808 swelling under the chords"],
      leads: ["detuned piano chord loop", "soft high keys figure", "dark synth pad doubling the chords"],
    },
    {
      id: "soul-loop", label: "Soul loop", refs: "J. Cole",
      aliases: ["j. cole", "j cole", "jcole", "cole", "j coke"],
      bpm: [84, 96], feel: "lazy head-nod swing", tonalities: ["minor", "major"],
      openers: [
        "a warm soul-sample loop of strings and electric guitar, head-nod boom-bap drums played loose like a live drummer, a round bass underneath, humble and heavy",
        "a late-night soul-sample beat: warm sampled chords, a lazy boom-bap swing, a deep round bass, and the space to tell a story",
      ],
      trait: "a lazy head-nod boom-bap swing",
      low: ["round warm bass walking under the loop", "deep round bass following the chords"],
      leads: ["sampled soul-string loop", "warm sampled electric-guitar loop", "warm sampled soul-chord loop"],
    },
    {
      id: "bar-fight", label: "Bar-fight stomp", refs: "Prof",
      aliases: ["prof", "gampo", "stophouse"],
      bpm: [86, 104], feel: "rowdy lurching stomp", tonalities: ["minor"],
      openers: [
        "a rowdy Minneapolis stomp: a distorted electric-guitar lick, a stomping kick and huge clap, a growling synth bass, sweaty and unhinged",
        "a rowdy midwest party beat: a gritty overdriven guitar riff, hard slapping drums, a growling bass, and a groove that lurches like it has had too many",
      ],
      trait: "a distorted electric-guitar lick",
      low: ["growling synth bass under the stomp", "overdriven bass doubling the kick"],
      leads: ["distorted electric-guitar lick", "gritty overdriven guitar riff", "growling synth-bass riff"],
    },
    {
      id: "crisp-bounce", label: "Crisp bounce", refs: "Connor Price",
      aliases: ["connor price", "connor"],
      bpm: [96, 124], feel: "punchy playful bounce", tonalities: ["minor", "major"],
      openers: [
        "a crisp punchy playful beat built around one catchy riff, snap-heavy clean drums, a bouncy 808, and big empty gaps that make every hit pop",
        "clean witty bounce: a catchy plucked synth riff front and center, tight crisp drums, a confident 808, nothing wasted",
      ],
      trait: "one catchy riff front and center over crisp snap-heavy drums",
      low: ["bouncy confident 808", "short punchy 808 locked to the kick"],
      leads: ["bouncy plucked synth riff", "staccato brass-stab riff", "rubbery bass-synth riff", "punchy synth-bell riff"],
    },
    {
      id: "stop-start", label: "Detroit stop-start", refs: "Big Sean",
      aliases: ["big sean", "sean don", "detroit 2"],
      bpm: [130, 150], feel: "cold stop-start half-time", tonalities: ["minor"],
      openers: [
        "cold Detroit stop-start trap: the drums slam to a halt every two bars for a punchline pause then crash back in, dark detuned bells, a deep 808, confident swagger",
        "stop-and-go Detroit trap: one dark riff, an 808 that drops out with the drums, hard silence, then everything slams back on the one",
      ],
      trait: "the drums slam to a halt every two bars and crash back in",
      low: ["deep 808 that drops out with the drums and slams back", "808 punching on the downbeat after every stop"],
      leads: ["dark detuned synth-bell riff", "cold analog synth riff", "muted electric-guitar riff"],
    },
  ];

  // ------------------------------------------------------------ cadence
  const WALTZ = [
    "waltz-time 3/4 accents riding over the 4/4 kick",
    "the melody counts in threes while the drums count in fours",
    "a 6/8 lilt in the hats over a straight kick",
    "a limping waltz swing that lands back on the one every three bars",
    "dotted-quarter accents so every third beat feels like the downbeat",
    "12/8 shuffle on the hats, straight 808",
    "a lopsided three-step bounce across a four-beat bar",
    "a slow waltz figure in the riff over half-time trap drums",
  ];
  const ON_OFF = [
    "two bars locked on the beat, two bars sliding off it",
    "kick dead on the grid, everything else drifting off it",
    "the snare lands on the beat in the hook and a 16th late in the verse",
    "the riff starts on the beat then slips behind it a little more each bar",
    "on the beat, off the beat, on again, the pocket breathing",
    "the 808 hits the offbeats while the kick holds the downbeats",
    "every fourth bar the whole groove shifts one 8th late, then snaps back",
    "drums unquantized and drunk, landing on the beat only when it counts",
  ];
  // The rule of the track: one constraint that makes the roll its own thing.
  const RULES = [
    "the kick never lands on beat 1",
    "only three sounds play at once, ever",
    "the riff is five beats long, so it rotates against the bar",
    "the snare only hits on beat 4",
    "every fourth bar is one beat short",
    "the hook is the 808 alone with everything else gone",
    "the melody uses only two notes",
    "the hats play only triplets, never straight 8ths",
    "the lead is reversed for the whole track",
    "the loop is 7 bars long, not 8",
    "every 8th bar is total silence",
    "the 808 never plays the root note",
    "the drums are pitched down a full octave",
    "the clap lands a beat early every second bar",
    "the riff loses one note every hook",
    "the drums play in 3 for one bar in every 4",
    "nothing plays on beat 3",
    "the tempo halves for exactly 2 bars before each hook",
    "the riff and the 808 never play at the same time",
    "the beat starts on the and-of-4 and never resets",
  ];
  const SWITCH = [
    "tempo halves and the riff drops an octave",
    "the drums flip to a waltz pattern and the 808 starts sliding",
    "everything drops for 2 bars, then a harder faster beat in a new minor key",
    "the riff disappears and the 808 plays the melody",
    "the key drops a whole step and the drums go bone-dry",
    "the drums double time while the riff stays half time",
    "the hats switch to triplets and the snare moves to beat 4",
    "the kick pattern flips to the offbeats for the rest of the track",
  ];

  // Lead character: modern hip hop instruments only, one quality word, one
  // human-playing phrase. Quality words never degrade the fidelity.
  const LEADS = [
    "dark synth-bell riff", "detuned analog synth riff", "distorted 808 melody", "muted electric-guitar riff",
    "brass-stab riff", "plucked synth riff", "electric-keys riff", "dark synth-pad riff", "string-stab riff",
    "distorted electric-guitar riff", "synth-brass riff", "detuned piano riff",
  ];
  const QUALITY = [
    "warm analog saturation", "slightly detuned", "dark and filtered", "dry and punchy", "wide and glossy", "gritty but clean",
  ];
  const PLAYING = [
    "played a hair behind the beat", "played by hand with uneven touch", "rushing slightly ahead on every repeat",
    "sliding into pitch late", "loose and human, never quantized",
  ];
  const MIX = [
    "crisp punchy drums, hard-hitting 808, polished modern hip hop mix, loud and clean",
    "radio-ready mix, crisp drums up front, deep clean 808, warm midrange",
    "crisp, loud and wide with a mono sub, every hit sharp",
    "polished major-label hip hop master, crisp top, heavy clean low end",
  ];

  const MINIMAL = [
    "minimal: kick, snare, 808 and one riff",
    "sparse arrangement, wide empty space between hits",
    "three sounds at a time at most, every hit counts",
  ];

  const INSTRUMENTAL = "instrumental only";

  const STRUCTURES = [
    { id: "radio", label: "Radio", length: "~2:30", steps: ["intro", "hook", "verseOn", "hook", "verseOff", "switch", "switchHook", "outro"] },
    { id: "short", label: "Short loop", length: "~1:30", steps: ["intro", "hook", "verseOn", "hook", "switch", "switchHook", "outro"] },
    { id: "story", label: "Story", length: "~2:45", steps: ["intro", "verseOn", "hook", "verseOff", "break", "switch", "switchHook", "outro"] },
    { id: "stopstart", label: "Stop-start", length: "~2:15", steps: ["intro", "hook", "verseStop", "hook", "switch", "switchHook", "outro"] },
  ];

  const EXCLUDE = [
    "vocals", "singing", "humming", "choir", "cinematic", "orchestral", "film score", "world music",
    "ambient", "lo-fi", "DJ scratching", "drum fills", "risers", "EDM",
  ];

  const SETTINGS = {
    radio: {
      model: "Suno v6", weirdness: "40–50%", styleInfluence: "80–90%", variety: "Off (0)", maxMode: "On",
      note: "Weirdness in the 40s: enough for the waltz cadence and the house rule, not enough to wander into soundtrack territory. Style Influence high so it stays a crisp hip hop record. Variety Off so these phrases are not rewritten. Turn Suno's Instrumental toggle on.",
    },
    wild: {
      model: "Suno v6-wild, keeper re-run on v6", weirdness: "60–75%", styleInfluence: "65–75%", variety: "Off (0)", maxMode: "On for the v6 re-run",
      note: "Wild leans harder into the house rule and the switch; take the keeper's seed to v6 with Max Mode for a crisp finish. Turn Suno's Instrumental toggle on.",
    },
  };

  // Words that pull Suno toward film score, world music or lo-fi sound design.
  const SOUNDTRACK = /\b(erhu|koto|sitar|tabla|bagpipe\w*|accordion|harpsichord|flamenco|steel[- ]?(pan|drum)|banjo|tuba\w*|whistl\w*|pan flute|ocarina|duduk|cello\w*|viola|violin|pizzicato|harps?|bowed|harmonica\w*|timpani|cathedral|church|pipe[- ]organ|organs?|orchestral|cinematic|film|horror|haunt\w*|eerie|ghostly|spaghetti|mellotron|flutes?|woodwind|reed|cassette|vinyl|crackl\w*|transistor|sewer|storm drain|basement|boiler|tunnel|subway|overpass|dripping|manhole|dumpster|nylon|slide[- ]guitar|upright|double bass|fanfare|marching|military)\b/i;
  const NAMES = [...new Set(DNA.flatMap((d) => d.aliases))].filter((n) => n.replace(/[^a-z0-9]/gi, "").length >= 3);
  const esc = (s) => s.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
  const LINT = [
    { re: /\b(vocal|vocals|vocalist|rap\b|rapper|sing|sings|singing|singer|sung|chant\w*|choir|hum|humming|lyric\w*|ad-?lib\w*)\b/i, level: "block", msg: "vocal vocabulary invites a voice" },
    { re: /\b(transition\w*|sweeps?|risers?|phaser|dj\b|scratch\w*|fills?|cowbell|rimshot|airhorn)\b/i, level: "block", msg: "effect vocabulary turns into DJ tricks and fills" },
    { re: /\b(marimba|kalimba|music box|glockenspiel|celesta|xylophone)\b/i, level: "block", msg: "stock mallet preset" },
    { re: /\b(edm|dubstep|jazz\w*|funk\w*|disco)\b/i, level: "block", msg: "off-format genre word" },
    { re: /\bno\s+\w+/i, level: "warn", msg: "\"no X\" inside a prompt plants X" },
    { re: SOUNDTRACK, level: "block", msg: "soundtrack, world or lo-fi word turns the beat into film music" },
    ...NAMES.map((n) => ({ re: new RegExp(`(^|[^a-z0-9])${esc(n)}($|[^a-z0-9])`, "i"), level: "block", msg: `reference name "${n}"` })),
  ];

  return {
    STYLE_LIMIT, LYRICS_LIMIT, MINOR_KEYS, MAJOR_KEYS, PROGRESSIONS, DNA, WALTZ, ON_OFF, RULES, SWITCH,
    LEADS, QUALITY, PLAYING, MINIMAL, MIX, INSTRUMENTAL, SOUNDTRACK, STRUCTURES, EXCLUDE, SETTINGS, NAMES, LINT,
  };
})();

if (typeof module !== "undefined") module.exports = ManholeData;
