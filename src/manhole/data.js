/* MANHOLE vocabulary. Garage-sewer minimalism with a waltz cadence riding
 * trap drums. Reference names live in `refs` and `aliases` for the UI and
 * the lint only; nothing named ever reaches the prompt. Every phrase is
 * instrumental: no voices, no FX words, no fills. */
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
  const DNA = [
    {
      id: "horror-minimal", label: "Horror minimal", refs: "Big Sean · Blessings",
      aliases: ["big sean", "sean don", "blessings", "vinylz", "allen ritter"],
      bpm: [128, 148], feel: "slow menacing half-time", tonalities: ["minor"],
      openers: [
        "a menacing horror-film minimal beat: one eerie low organ riff, a huge sub that waits a whole bar to drop, and dead silence before every hook",
        "spacious sinister minimalism: a dark pad holding two notes, sparse hard drums, and stop-start gaps that leave room for the punchline",
      ],
      trait: "stop-start gaps that leave a full beat of silence for the punchline",
      drums: ["sparse hard drums, kick on 1 only, snare cracking on 3", "half-time drums that stop dead for a beat every 2 bars"],
      low: ["huge sub that waits a whole bar to drop", "808 that rings long and dark under the riff"],
      leads: ["eerie low organ riff", "dark pad riff, two notes", "low detuned bell riff"],
    },
    {
      id: "one-loop", label: "One-loop hypnosis", refs: "Lil Wayne · A Milli",
      aliases: ["lil wayne", "weezy", "wayne", "a milli", "bangladesh"],
      bpm: [140, 154], feel: "bare hypnotic bounce", tonalities: ["minor"],
      openers: [
        "a brutally minimal hypnotic beat: one stubborn two-note loop repeating forever, a trunk-slapping 808, a snap and a kick, and nothing else",
        "a one-idea beat: the same two-note figure for the entire track over bare hard drums and a booming 808 that rattles the windows",
      ],
      trait: "one stubborn two-note loop that never changes",
      drums: ["bare kick and snap only, nothing else in the kit", "hard kick on the downbeats, a dry snap on 2 and 4, hats almost absent"],
      low: ["trunk-slapping 808 that rattles the windows", "booming 808 hitting with every kick"],
      leads: ["stubborn two-note synth-brass loop", "two-note pitched-down guitar loop", "two-note low organ stab loop"],
    },
    {
      id: "devil-bounce", label: "Devil-on-the-shoulder bounce", refs: "JID · McAfee",
      aliases: ["jid", "j.i.d", "j.i.d.", "mcafee", "boi-1da", "boi1da", "cubeatz", "dreamville"],
      bpm: [150, 166], feel: "dark rubbery uptempo bounce", tonalities: ["minor"],
      openers: [
        "a bouncy dark uptempo beat like a devil whispering over your shoulder: rubbery bass, dry snappy drums, staccato stabs that jump in and out of the pocket",
        "dark twitchy uptempo bounce made for staccato rapid-fire flows: elastic bass, off-kilter hats, a sinister plucked riff, sudden drop-outs",
      ],
      trait: "staccato stabs that jump in and out of the pocket",
      drums: ["dry snappy drums with hats that skip and double", "tight dry kit, snare landing a hair early, kick stumbling"],
      low: ["rubbery elastic bass bending between notes", "bouncing 808 sliding into every root"],
      leads: ["sinister plucked riff", "staccato low synth-brass stab riff", "dark muted guitar riff"],
    },
    {
      id: "cathedral", label: "Cathedral thunder", refs: "JID · Glory",
      aliases: ["jid", "lex luger", "luger", "beatnick dee"],
      bpm: [138, 150], feel: "grand booming half-time", tonalities: ["minor"],
      openers: [
        "a triumphant pipe-organ and string swell over hard booming Atlanta drums, grand and wounded at once, with the low end shaking the pews",
        "heavy cathedral trap: a pipe-organ chord loop, big string stabs, rattling hats and an 808 that rumbles like thunder in a stone hall",
      ],
      trait: "a grand pipe-organ swell under the hook",
      drums: ["rattling hat rolls, a booming clap-snare, kick that hits like a door", "huge half-time drums with machine-gun hats"],
      low: ["808 rumbling like thunder in a stone hall", "long distorted 808 tail under the organ"],
      leads: ["pipe-organ chord riff", "big string-stab riff", "low brass and organ riff"],
    },
    {
      id: "loyalty-heavy", label: "Loyalty heavy", refs: "JID · Bruddanem",
      aliases: ["jid", "bruddanem", "lil durk", "durk", "dj khalil", "forever story"],
      bpm: [130, 150], feel: "heavy wounded half-time", tonalities: ["minor"],
      openers: [
        "a hard heavy loyalty beat: detuned out-of-tune piano chords, ghostly high-key noodling drifting over the top, a dark synth pad mirroring the chords, drums that hit like a vow",
        "concentrated, heavy and wounded: a detuned piano chord loop, a ghostly high-register keys figure, a synth pad doubling the chords, booming drums under all of it",
      ],
      trait: "detuned piano chords with ghostly high-key noodling over the top",
      drums: ["heavy booming drums, snare cracking on 3, hats rolling", "hard half-time drums that hit like a vow"],
      low: ["deep 808 holding each chord root", "heavy 808 swelling under the piano"],
      leads: ["detuned piano chord loop", "ghostly high-register keys figure", "dark synth pad mirroring the chords"],
    },
    {
      id: "porch-soul", label: "Porch-light soul", refs: "J. Cole",
      aliases: ["j. cole", "j cole", "jcole", "cole", "j coke"],
      bpm: [84, 96], feel: "lazy head-nod swing", tonalities: ["minor", "major"],
      openers: [
        "a warm dusty soul loop of sampled strings and electric guitar, head-nod drums played loose like a live drummer, a round bass walking underneath, humble and heavy",
        "a porch-light beat at midnight: crackly sampled soul chords, a lazy boom-bap swing, a deep round bass, and the space to tell a story",
      ],
      trait: "a lazy head-nod boom-bap swing",
      drums: ["loose boom-bap drums, snare dragging behind, kick with a soft thud", "dusty swung drums like a live drummer leaning back"],
      low: ["round warm bass walking under the loop", "deep upright-style bass following the chords"],
      leads: ["dusty sampled soul-string loop", "warm sampled electric-guitar loop", "crackly sampled soul-chord loop"],
    },
    {
      id: "bar-fight", label: "Bar-fight blues stomp", refs: "Prof",
      aliases: ["prof", "gampo", "stophouse"],
      bpm: [86, 104], feel: "lurching stomp", tonalities: ["minor"],
      openers: [
        "a rowdy Minneapolis bar-fight stomp: a greasy distorted blues guitar lick, a stomping kick and clap, a growling synth bass, sweaty and unhinged",
        "a drunk midwest stomp: dirty slide-guitar riff through a busted amp, hard slapping drums, a whiskey-soaked low organ, a beat that lurches like it has had too many",
      ],
      trait: "a greasy distorted blues guitar lick",
      drums: ["stomping kick and big clap, hats loose and drunk", "hard slapping drums that lurch on the and-of-4"],
      low: ["growling synth bass under the stomp", "dirty overdriven bass doubling the kick"],
      leads: ["greasy distorted blues guitar lick", "slide-guitar riff through a busted amp", "whiskey-soaked low organ riff"],
    },
    {
      id: "crisp-quirk", label: "Crisp quirk bounce", refs: "Connor Price",
      aliases: ["connor price", "connor"],
      bpm: [96, 124], feel: "punchy playful bounce", tonalities: ["minor", "major"],
      openers: [
        "a crisp punchy playful beat built around one quirky instrument riff, snap-heavy clean drums, a bouncy 808, and big empty gaps that make every hit pop",
        "clean witty bounce: a goofy plucked riff on an unexpected instrument, tight crisp drums, a confident 808, the hook instrument front and center",
      ],
      trait: "one quirky instrument riff front and center over crisp snap-heavy drums",
      drums: ["crisp snap-heavy drums, tight and dry", "punchy clean kick and snap with big gaps between hits"],
      low: ["bouncy confident 808", "short punchy 808 locked to the kick"],
      leads: ["staccato tuba riff", "plucked pizzicato violin riff", "whistled melody riff", "plucked banjo riff"],
    },
    {
      id: "stop-start", label: "Detroit stop-start", refs: "Big Sean",
      aliases: ["big sean", "sean don", "detroit 2"],
      bpm: [130, 150], feel: "cold stop-start half-time", tonalities: ["minor"],
      openers: [
        "a cold Detroit stop-start beat: the drums slam to a halt every two bars for a punchline pause then crash back in, dark detuned bells, a deep sub, confident swagger",
        "stop-and-go Detroit trap: one dark riff, a sub that drops out with the drums, hard silence, then everything slams back on the one",
      ],
      trait: "the drums slam to a halt every two bars and crash back in",
      drums: ["hard drums that stop dead every 2 bars and slam back on the one", "crisp half-time drums with a full-bar halt before each hook"],
      low: ["deep sub that drops out with the drums and slams back", "808 punching on the downbeat after every stop"],
      leads: ["dark detuned bell riff", "cold low synth riff", "low muted-guitar riff"],
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
  const ROOMS = [
    "recorded in a concrete storm drain with a long metallic echo",
    "sewer-tunnel reverb dripping off every hit",
    "parking-garage acoustics with a slap off the far wall",
    "a damp basement room, low ceiling, sweating walls",
    "under a highway overpass with traffic rumbling far off",
    "an empty boiler room with the pipes ringing",
    "a cinderblock garage with the door half open",
    "an abandoned subway platform, cold and cavernous",
  ];
  // Found-object percussion: the garage-sewer signature.
  const FOUND = [
    "a dripping pipe as an extra hi-hat",
    "a slammed car door layered under the snare",
    "a manhole cover clank on the and-of-4",
    "a shaken spray can as the shaker",
    "a basketball bounce ghosting the kick",
    "a lighter flick on the offbeats",
    "a chain dragged across concrete before every hook",
    "a garage door rattle on the last beat of every 8 bars",
    "keys jangling as the tambourine",
    "a steel pipe struck like a bell on beat 4",
    "a dumpster lid slam as the downbeat accent",
    "knuckles on a car hood as ghost snares",
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
    "the riff is replaced by the found-object percussion alone for 4 bars",
    "the key drops a whole step and the drums go bone-dry",
    "the 808 becomes the melody and the riff disappears",
    "the drums double time while the riff stays half time",
    "the whole beat moves into the room's echo, far away, then slams back close",
  ];

  // Lead character: what it is, how it sounds, how it is played.
  const LEADS = [
    "low synth-brass riff", "fuzz-bass riff", "muted electric-guitar riff", "low cello riff",
    "detuned organ riff", "sampled string riff", "slide-guitar riff", "pitched-down bell riff",
    "plucked upright-bass riff", "harmonica-like reed riff", "low woodwind riff", "bowed saw drone riff",
    "bit-crushed synth riff", "tremolo-guitar riff", "detuned analog synth riff", "muted horn-section riff",
  ];
  const ADJECTIVES = [
    "rusted", "sodium-lit", "rain-slicked", "basement-damp", "concrete-cold", "oil-slick", "nicotine-yellow", "tar-thick",
    "chrome-chipped", "cinderblock", "streetlight-orange", "sweat-soaked", "smoke-grained", "gutter-dark", "iron-cold", "humid",
    "half-broken", "warped", "cracked-vinyl", "burnt-out", "graffiti-bright", "storm-drain", "subway-cold", "dented",
  ];
  const GRIT = [
    "run through a worn cassette so it wobbles and hisses", "bit-crushed like a 12-bit sampler", "saturated until the edges fur",
    "sampled off dusty vinyl with crackle in the gaps", "detuned a few cents so it beats against itself",
    "through a cheap amp in a small room", "pitched down a step so it drags", "thin and crackly like a transistor radio",
    "warbling with tape flutter", "overdriven until the attack clips", "dull and warm like an old sample",
  ];
  const PLAYING = [
    "played a hair behind the beat", "played by hand with uneven touch", "rushing ahead on every repeat",
    "slightly out of tune on the top note", "with fret noise left in", "sliding into pitch late",
    "with the last note held too long", "hesitating before every downbeat", "loose and drunk, never quantized",
  ];
  const MINIMAL = [
    "minimal: kick, snare, 808, one riff, one found sound, nothing else",
    "skeletal arrangement with wide empty space between hits",
    "three elements at a time at most, every hit counts",
    "bare-bones minimalism, silence used as an instrument",
  ];
  const MIX = [
    "crisp hard-hitting drums and a clean booming 808 up front, grime on everything else",
    "drums sharp enough to cut, instruments dirty and worn",
    "loud radio master, crisp punch on top, sewer grime underneath",
    "clean heavy low end, crunchy dusty mids, snappy top",
  ];

  const INSTRUMENTAL = "instrumental only";

  const STRUCTURES = [
    { id: "radio", label: "Radio", length: "~2:30", steps: ["intro", "hook", "verseOn", "hook", "verseOff", "switch", "switchHook", "outro"] },
    { id: "short", label: "Short loop", length: "~1:30", steps: ["intro", "hook", "verseOn", "hook", "switch", "switchHook", "outro"] },
    { id: "story", label: "Story", length: "~2:45", steps: ["intro", "verseOn", "hook", "verseOff", "break", "switch", "switchHook", "outro"] },
    { id: "stopstart", label: "Stop-start", length: "~2:15", steps: ["intro", "hook", "verseStop", "hook", "switch", "switchHook", "outro"] },
  ];

  const EXCLUDE = [
    "vocals", "singing", "humming", "choir", "vocal chops", "vocal samples", "spoken word",
    "DJ scratching", "drum fills", "risers", "EDM", "stock synth presets", "lo-fi hip hop", "jazz",
  ];

  const SETTINGS = {
    radio: {
      model: "Suno v6", weirdness: "50–60%", styleInfluence: "75–85%", variety: "Off (0)", maxMode: "On",
      note: "Weirdness in the 50s so the waltz cadence and the house rule actually happen; Style Influence high so the room, the grit and the bans hold. Variety Off so these phrases are not rewritten. Turn Suno's Instrumental toggle on.",
    },
    wild: {
      model: "Suno v6-wild, keeper re-run on v6", weirdness: "60–75%", styleInfluence: "65–75%", variety: "Off (0)", maxMode: "On for the v6 re-run",
      note: "Wild leans into the found sounds and the rule; take the keeper's seed to v6 with Max Mode for a crisp finish. Turn Suno's Instrumental toggle on.",
    },
  };

  const NAMES = [...new Set(DNA.flatMap((d) => d.aliases))].filter((n) => n.replace(/[^a-z0-9]/gi, "").length >= 3);
  const esc = (s) => s.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
  const LINT = [
    { re: /\b(vocal|vocals|vocalist|rap\b|rapper|sing|sings|singing|singer|sung|chant\w*|choir|hum|humming|lyric\w*|ad-?lib\w*)\b/i, level: "block", msg: "vocal vocabulary invites a voice" },
    { re: /\b(transition\w*|sweeps?|risers?|phaser|dj\b|scratch\w*|fills?|cowbell|rimshot|airhorn)\b/i, level: "block", msg: "effect vocabulary turns into DJ tricks and fills" },
    { re: /\b(marimba|kalimba|music box|glockenspiel|celesta|xylophone)\b/i, level: "block", msg: "stock mallet preset" },
    { re: /\b(edm|dubstep|jazz\w*|funk\w*|disco)\b/i, level: "block", msg: "off-format genre word" },
    { re: /\bno\s+\w+/i, level: "warn", msg: "\"no X\" inside a prompt plants X" },
    ...NAMES.map((n) => ({ re: new RegExp(`(^|[^a-z0-9])${esc(n)}($|[^a-z0-9])`, "i"), level: "block", msg: `reference name "${n}"` })),
  ];

  return {
    STYLE_LIMIT, LYRICS_LIMIT, MINOR_KEYS, MAJOR_KEYS, PROGRESSIONS, DNA, WALTZ, ON_OFF, ROOMS, FOUND, RULES, SWITCH,
    LEADS, ADJECTIVES, GRIT, PLAYING, MINIMAL, MIX, INSTRUMENTAL, STRUCTURES, EXCLUDE, SETTINGS, NAMES, LINT,
  };
})();

if (typeof module !== "undefined") module.exports = ManholeData;
