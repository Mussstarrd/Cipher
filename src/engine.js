/* CIPHER engine — hip hop & R&B banger builder for Suno v6.
 * Pure functions, no DOM. Same inputs + same seed = same package.
 * Research behind every table lives in docs/research.md. */
"use strict";

const CipherEngine = (() => {
  // ---------------------------------------------------------------- profiles
  // Suno v6 family (9 Sep 2026). Every earlier model is retired for new songs.
  // Limits are unchanged from v5: style 1,000, lyrics 5,000 (research §2).
  const PROFILES = {
    v6: {
      id: "v6", label: "v6", styleCharLimit: 1000, lyricsCharLimit: 5000,
      weirdnessShift: 0, styleShift: 0,
      note: "v6 is the precise flagship: it follows a detailed style prompt closely, so the bans hold best here.",
    },
    "v6-wild": {
      id: "v6-wild", label: "v6-wild", styleCharLimit: 1000, lyricsCharLimit: 5000,
      weirdnessShift: -15, styleShift: 5,
      note: "v6-wild already pushes away from the prompt, so Weirdness comes down 15 and Style Influence goes up 5 to keep the lane and the bans.",
    },
    "v6-mini": {
      id: "v6-mini", label: "v6-mini (all plans)", styleCharLimit: 1000, lyricsCharLimit: 5000,
      weirdnessShift: 0, styleShift: 0,
      note: "v6-mini is the fast, cheap tier: use it to test a package, then render the keeper on v6 with Max Mode.",
    },
  };
  const DEFAULT_PROFILE = "v6";
  const clamp = (v) => Math.max(0, Math.min(100, v));

  // Style-field sweet spot: 6–12 distinct descriptors lands best (research §2).
  const DESCRIPTOR_TARGET = { min: 8, max: 12 };

  // ------------------------------------------------------------ hard bans
  // Always sent to Exclude Styles. Order = most likely to leak in first.
  const HARD_BANS = [
    "jazz",
    "funk",
    "edm",
    "pop",
    "drum fills",
    "tom fills",
    "rimshot",
    "cowbell",
    "dj scratch",
  ];
  const OPTIONAL_BANS = ["humming", "airhorn", "dj tag", "risers", "disco", "live band", "saxophone", "brass", "choir"];

  // Words that must never appear in the style text or section tags: naming a
  // banned thing, even to negate it, pulls it into the mix (research §3).
  const LINT_RULES = [
    { re: /\b(fill|fills|filler|fillers)\b/i, level: "block", msg: "mentions fills — naming them invites them" },
    { re: /\btoms?\b/i, level: "block", msg: "mentions toms" },
    { re: /\brim ?shots?\b/i, level: "block", msg: "mentions rimshot" },
    { re: /\bcowbells?\b/i, level: "block", msg: "mentions cowbell" },
    { re: /\bjazz\w*/i, level: "block", msg: "jazz vocabulary" },
    { re: /\bfunk\w*/i, level: "block", msg: "funk vocabulary" },
    { re: /\bedm\b|\bfour-on-the-floor\b|\bsupersaw\b|\bbuild-?ups?\b|\brisers?\b/i, level: "block", msg: "EDM vocabulary" },
    { re: /\bdrops?\b/i, level: "block", msg: "\"drop\" reads as an EDM drop" },
    { re: /\bpop\b|\bpoppy\b/i, level: "block", msg: "pop vocabulary" },
    { re: /\bdj\b|\bscratch\w*|\bairhorns?\b|\bdisco\b/i, level: "block", msg: "DJ effect vocabulary" },
    { re: /\b(sax\w*|trumpets?|horns?|rhodes|clavinet|talkbox|wah|slap bass)\b/i, level: "warn", msg: "instrument that drags toward jazz/funk" },
    { re: /\bno\s+\w+/i, level: "warn", msg: "\"no X\" inside the style text backfires — use Exclude Styles", styleOnly: true },
    { re: /[\[\]]/, level: "warn", msg: "brackets belong in the lyrics box only", styleOnly: true },
  ];

  // ---------------------------------------------------------------- theory
  const SHARP_NAMES = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];
  const FLAT_NAMES = ["C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B"];
  const PC = {
    C: 0, "C#": 1, Db: 1, D: 2, "D#": 3, Eb: 3, E: 4, F: 5, "F#": 6, Gb: 6,
    G: 7, "G#": 8, Ab: 8, A: 9, "A#": 10, Bb: 10, B: 11,
  };
  // Spell each key the way a producer reads it on a DAW key picker.
  const SHARP_KEYS = {
    minor: new Set(["E", "B", "F#", "C#", "G#", "D#", "A#"]),
    major: new Set(["G", "D", "A", "E", "B", "F#"]),
  };

  function noteName(pc, tonic, tonality) {
    const names = SHARP_KEYS[tonality].has(tonic) ? SHARP_NAMES : FLAT_NAMES;
    return names[((pc % 12) + 12) % 12];
  }

  const QUALITY_SUFFIX = {
    "": "", m: "m", m7: "m7", m9: "m9", maj7: "maj7", "7": "7", "9": "9",
    "6": "6", m6: "m6", sus4: "sus4", add9: "add9",
  };

  // Progressions: [semitones above tonic, chord quality]. Every entry is a loop
  // lifted from the hip hop / R&B canon — no ii–V jazz motion, no I–V–vi–IV.
  const PROGRESSIONS = {
    pendulum: {
      name: "Two-chord pendulum", tonality: "minor", roman: "i – ♭VI",
      chords: [[0, "m"], [8, ""]], cadence: "open", color: "a brooding two-chord minor loop",
    },
    phrygian: {
      name: "Phrygian hover", tonality: "minor", roman: "i – ♭II",
      chords: [[0, "m"], [1, ""]], cadence: "phrygian", color: "a dark half-step Phrygian loop",
    },
    sting: {
      name: "Harmonic-minor sting", tonality: "minor", roman: "i – ♭VI – V",
      chords: [[0, "m"], [8, ""], [7, ""]], cadence: "half", color: "a harmonic-minor loop with a tense major V",
    },
    lament: {
      name: "Descending lament", tonality: "minor", roman: "i – ♭VII – ♭VI – V",
      chords: [[0, "m"], [10, ""], [8, ""], [7, ""]], cadence: "andalusian", color: "a falling minor bassline loop",
    },
    climb: {
      name: "Aeolian climb", tonality: "minor", roman: "i – ♭VI – ♭III – ♭VII",
      chords: [[0, "m"], [8, ""], [3, ""], [10, ""]], cadence: "subtonic", color: "a cinematic four-chord minor loop",
    },
    sway: {
      name: "Minor plagal sway", tonality: "minor", roman: "i – iv",
      chords: [[0, "m"], [5, "m"]], cadence: "plagal", color: "a hypnotic minor i–iv sway",
    },
    coldFifth: {
      name: "Cold fifth", tonality: "minor", roman: "i – v – iv – ♭VII",
      chords: [[0, "m"], [7, "m"], [5, "m"], [10, ""]], cadence: "subtonic", color: "a cold all-minor loop",
    },
    drone: {
      name: "Pedal drone", tonality: "minor", roman: "i (pedal)",
      chords: [[0, "m"]], cadence: "drone", color: "a one-chord minor drone riff",
    },
    drillClimb: {
      name: "Drill climb", tonality: "minor", roman: "i – ♭VI – ♭III – V",
      chords: [[0, "m"], [8, ""], [3, ""], [7, ""]], cadence: "half", color: "an ominous minor loop that hangs on a tense V",
    },
    lateNight: {
      name: "Late-night climb", tonality: "minor", roman: "i9 – ♭VImaj7 – ♭IIImaj7 – ♭VII",
      chords: [[0, "m9"], [8, "maj7"], [3, "maj7"], [10, ""]], cadence: "subtonic", color: "lush minor-ninth chords",
    },
    ninthSway: {
      name: "Minor-ninth sway", tonality: "minor", roman: "i9 – iv9",
      chords: [[0, "m9"], [5, "m9"]], cadence: "plagal", color: "a lush minor-ninth two-chord sway",
    },
    resolve: {
      name: "Aeolian resolve", tonality: "minor", roman: "♭VImaj7 – ♭VII – i9",
      chords: [[8, "maj7"], [10, ""], [0, "m9"]], cadence: "aeolian", color: "chords climbing up to land on the minor tonic",
    },
    backdoor: {
      name: "Backdoor home", tonality: "major", roman: "iv7 – ♭VII9 – Imaj7",
      chords: [[5, "m7"], [10, "9"], [0, "maj7"]], cadence: "backdoor", color: "warm borrowed-chord R&B changes",
    },
    slowJam: {
      name: "Slow-jam cycle", tonality: "major", roman: "vi9 – IVmaj7 – Imaj7 – Vsus4",
      chords: [[9, "m9"], [5, "maj7"], [0, "maj7"], [7, "sus4"]], cadence: "suspended", color: "silky major-seventh chords",
    },
    float: {
      name: "Plagal float", tonality: "major", roman: "IVmaj7 – Imaj7",
      chords: [[5, "maj7"], [0, "maj7"]], cadence: "plagalMajor", color: "a floating major-seventh sway",
    },
    pluggDescent: {
      name: "Plugg descent", tonality: "major", roman: "IVmaj7 – iii7 – ii7 – Imaj7",
      chords: [[5, "maj7"], [4, "m7"], [2, "m7"], [0, "maj7"]], cadence: "descending", color: "dreamy descending major-seventh chords",
    },
    bittersweet: {
      name: "Bittersweet iv", tonality: "major", roman: "Imaj7 – iii7 – IVmaj7 – iv6",
      chords: [[0, "maj7"], [4, "m7"], [5, "maj7"], [5, "m6"]], cadence: "minorPlagal", color: "bittersweet major-seventh chords with a borrowed minor iv",
    },
  };

  // What each cadence does to the listener — shown on the harmony sheet.
  const CADENCES = {
    open: "Open loop — never resolves, so the ear keeps waiting and the loop keeps pulling you back. Hypnotic repetition is the point.",
    phrygian: "Phrygian cadence ♭II → i — the half-step fall into the tonic is pure dread. Drill and dark trap live here.",
    half: "Half cadence on V — the harmonic-minor leading tone builds tension every loop and snaps back to i. Endless forward pull.",
    andalusian: "Andalusian descent — the bass walks down step by step and parks on V, then the loop restarts. Feels inevitable.",
    aeolian: "Aeolian cadence ♭VI → ♭VII → i — resolves with no leading tone. Final but dark; the hook lands like a verdict.",
    subtonic: "Subtonic cadence ♭VII → i — the loop climbs a whole step back into the tonic. Sturdy, dark, no bright leading tone.",
    plagalMajor: "Plagal motion IV → I — the loop floats home without ever pushing. Dreamy and weightless.",
    plagal: "Plagal motion iv → i — the soft 'amen' release. Soulful without getting sweet.",
    drone: "No cadence — a pedal tone. All movement lives in the 808 slides and hi-hat syncopation.",
    backdoor: "Backdoor cadence iv → ♭VII → I — the R&B/gospel road home. Resolves warm instead of bright.",
    suspended: "Suspended V (sus4) — the loop hangs unresolved on the sus chord, which makes the hook's return to vi feel like relief.",
    descending: "Stepwise descent into I — floaty, sad-happy, and it loops without a seam.",
    minorPlagal: "Borrowed minor iv → I — the bittersweet sigh at the end of the loop. Instant late-night melancholy.",
  };

  // ---------------------------------------------------------------- lanes
  // Vocabulary is filtered for the house bans: nothing that reads as jazz,
  // funk, EDM, pop, DJ effects or percussion fills.
  const LANES = [
    {
      id: "dark-trap", label: "Dark Trap", family: "hiphop",
      genres: ["dark trap", "Atlanta trap"], bpm: [136, 150], feel: "half-time bounce",
      keys: { minor: ["C#", "F#", "G", "A", "F", "D#", "B"], major: [] },
      progressions: ["pendulum", "phrygian", "sting", "lament", "coldFifth"],
      moods: ["menacing", "cold-blooded confidence", "midnight paranoia", "villain energy"],
      drums: [
        "rattling triplet hi-hat rolls",
        "syncopated kicks dancing around the 808",
        "hi-hats flipping between eighth-note and sixteenth-triplet rolls",
        "hard clap on the half-time backbeat",
      ],
      bass: ["distorted gliding 808s", "long 808 slides landing on the downbeat"],
      leads: ["haunting piano", "eerie bells", "dark plucked strings", "a detuned synth"],
      motifs: ["bell", "piano", "string"],
      vocals: ["aggressive melodic rap", "gritty chant-style delivery", "auto-tuned menacing rap"],
      textures: ["pitch-black low end", "crisp modern master", "wide cinematic mix"],
      flows: ["triplet", "stopstart", "staccato"],
      hooks: ["chant", "callresp", "stair"],
      adlibs: ["yeah", "go", "huh"],
    },
    {
      id: "rage", label: "Rage / Glitch", family: "hiphop",
      genres: ["rage trap", "experimental glitch rap"], bpm: [150, 170], feel: "frantic bounce",
      keys: { minor: ["F", "F#", "G", "A", "C#"], major: [] },
      progressions: ["drone", "pendulum", "phrygian"],
      moods: ["chaotic adrenaline", "euphoric menace", "mosh-pit energy"],
      drums: [
        "blown-out stuttering hi-hats",
        "off-grid glitch-chopped drums",
        "distorted kicks firing in syncopated bursts",
      ],
      bass: ["clipping overdriven 808s", "808s that bend and snap back"],
      leads: ["a buzzing detuned rage synth", "bit-crushed synth stabs", "a pitched vocal-chop riff"],
      motifs: ["synth", "vocal chop"],
      vocals: ["hyper ad-lib-heavy delivery", "pitched-up melodic yelps", "nasal chant flow"],
      textures: ["crushed distorted master", "lo-fi digital grit"],
      flows: ["staccato", "offbeat", "stopstart"],
      hooks: ["stutter", "chant", "callresp"],
      adlibs: ["what", "ay", "yeah"],
    },
    {
      id: "drill", label: "Drill", family: "hiphop",
      genres: ["drill", "dark drill rap"], bpm: [140, 146], feel: "sliding half-time",
      keys: { minor: ["C#", "F#", "G", "A#", "D#", "F"], major: [] },
      progressions: ["sting", "phrygian", "drillClimb", "lament"],
      moods: ["cold menace", "tense and ominous", "street-war tension"],
      drums: [
        "skippy syncopated drill hi-hats",
        "off-beat snares landing late in the bar",
        "sparse kicks syncopated against the 808",
      ],
      bass: ["sliding glide 808s bending between notes"],
      leads: ["ominous string stabs", "a cold minor piano", "a dark vocal pad"],
      motifs: ["string", "piano"],
      vocals: ["cold low-register drill flow", "aggressive punchy delivery"],
      textures: ["dry gritty mix", "cold dark space"],
      flows: ["offbeat", "stopstart", "triplet"],
      hooks: ["chant", "callresp", "stutter"],
      adlibs: ["grr", "yeah", "gang"],
    },
    {
      id: "melodic-trap", label: "Melodic Trap", family: "hiphop",
      genres: ["melodic trap", "emotional melodic rap"], bpm: [140, 160], feel: "half-time bounce",
      keys: { minor: ["C#", "F#", "A", "B", "E", "D#"], major: [] },
      progressions: ["climb", "pendulum", "lament", "sway"],
      moods: ["heartbroken", "bittersweet late-night", "melancholic euphoria"],
      drums: [
        "bouncy syncopated hi-hats with rapid triplet rolls",
        "punchy clap on the half-time backbeat",
        "kicks skipping ahead of the beat",
      ],
      bass: ["warm booming 808s", "808 glides under every chord change"],
      leads: ["a clean electric guitar", "a glassy bell arpeggio", "an airy detuned synth"],
      motifs: ["guitar", "bell"],
      vocals: ["auto-tuned melodic sing-rap", "aching sustained hook notes"],
      textures: ["spacious reverb-washed mix", "polished modern trap sheen"],
      flows: ["melodic", "triplet", "offbeat"],
      hooks: ["stair", "mantra", "chant"],
      adlibs: ["oh", "yeah", "woah"],
    },
    {
      id: "hypnotic", label: "Hypnotic Minimal", family: "hiphop",
      genres: ["psychedelic trap", "hypnotic minimal rap"], bpm: [128, 150], feel: "cavernous half-time",
      keys: { minor: ["F", "G", "A", "C", "D"], major: [] },
      progressions: ["drone", "sway", "phrygian", "pendulum"],
      moods: ["narcotic haze", "dark euphoria", "nocturnal hypnosis"],
      drums: [
        "sparse syncopated hi-hats with sudden triplet bursts",
        "kicks placed off the grid",
        "echoing claps that land a sixteenth late",
      ],
      bass: ["cavernous sub 808s", "808s that swell and swallow the room"],
      leads: ["a hazy reversed synth wash", "a filtered vocal-chop motif", "sparse icy keys"],
      motifs: ["vocal chop", "synth"],
      vocals: ["layered auto-tuned vocals with ad-lib echoes", "hypnotic repeated phrases"],
      textures: ["psychedelic haze", "cavernous reverb tails", "pitch-black low end"],
      flows: ["melodic", "stopstart", "offbeat"],
      hooks: ["chant", "callresp", "stutter"],
      adlibs: ["yeah", "straight up", "alright"],
    },
    {
      id: "grimy-boom", label: "Grimy Boom Bap", family: "hiphop",
      genres: ["grimy boom bap", "hardcore East Coast hip hop"], bpm: [86, 96], feel: "head-nod pocket",
      keys: { minor: ["A", "D", "E", "G", "C", "F"], major: [] },
      progressions: ["pendulum", "sway", "lament", "coldFifth"],
      moods: ["gritty", "cold street wisdom", "menacing calm"],
      drums: [
        "hard dusty kick and cracking snare",
        "syncopated kick pattern sliding off the grid",
        "tight swung hi-hats",
      ],
      bass: ["deep round sub bass shadowing the kick"],
      leads: ["a dark minor piano", "a chopped string sample", "an eerie soul vocal chop"],
      motifs: ["piano", "string", "vocal chop"],
      vocals: ["raspy punchline-heavy rap", "precise commanding delivery"],
      textures: ["dusty tape grit", "concrete-hard drums"],
      flows: ["staccato", "offbeat", "double"],
      hooks: ["chant", "callresp", "stair"],
      adlibs: ["uh", "yeah", "come on"],
    },
    {
      id: "dark-rnb", label: "Dark Alt R&B", family: "rnb",
      genres: ["dark alternative R&B", "moody trap soul"], bpm: [62, 76], feel: "slow sensual sway",
      keys: { minor: ["D#", "A#", "F", "C", "G#", "F#"], major: [] },
      progressions: ["lateNight", "ninthSway", "resolve"],
      moods: ["seductive", "toxic late-night tension", "wounded desire"],
      drums: [
        "syncopated trap hi-hats at half speed",
        "off-beat finger snaps",
        "deep kicks hitting on the syncopated pickup",
      ],
      bass: ["heavy sub 808 swells"],
      leads: ["dark filtered electric piano", "reverse-swelling vocal pads", "a lush detuned synth"],
      motifs: ["vocal chop", "synth"],
      vocals: ["breathy falsetto R&B vocals", "silky layered harmonies", "melismatic runs on the hook"],
      textures: ["velvet-dark reverb-heavy mix", "underwater low-pass warmth"],
      flows: ["melodic", "offbeat"],
      hooks: ["mantra", "stair", "callresp"],
      adlibs: ["ooh", "yeah", "baby"],
    },
    {
      id: "trap-soul", label: "Trap Soul", family: "rnb",
      genres: ["trap soul", "moody R&B"], bpm: [64, 78], feel: "laid-back half-time",
      keys: { minor: ["C", "F", "A#", "D#"], major: ["Eb", "Ab", "Db", "Bb"] },
      progressions: ["ninthSway", "lateNight", "backdoor", "bittersweet"],
      moods: ["intimate", "confessional", "smooth late-night heartbreak"],
      drums: [
        "rolling syncopated hi-hats",
        "sparse knocking kicks",
        "finger snaps sitting just behind the beat",
      ],
      bass: ["gliding 808 bass"],
      leads: ["a clean chorused guitar arpeggio", "warm electric piano", "an ambient pad"],
      motifs: ["guitar", "piano"],
      vocals: ["smooth sung-rap hybrid", "conversational melodic R&B vocals"],
      textures: ["warm intimate mix", "late-night studio haze"],
      flows: ["melodic", "offbeat", "stopstart"],
      hooks: ["mantra", "callresp", "stair"],
      adlibs: ["yeah", "ooh", "oh no"],
    },
    {
      id: "y2k-rnb", label: "Y2K Stutter R&B", family: "rnb",
      genres: ["2000s R&B", "futuristic syncopated R&B"], bpm: [92, 106], feel: "stutter-step groove",
      keys: { minor: ["F", "G", "A#", "C", "D"], major: [] },
      progressions: ["sway", "phrygian", "ninthSway", "sting"],
      moods: ["futuristic", "flirtatious confidence", "icy seduction"],
      drums: [
        "stuttering syncopated drum programming with skittering hi-hats",
        "beatbox-style vocal percussion",
        "off-beat kicks and chopped snare stutters",
      ],
      bass: ["deep sliding sub bass"],
      leads: ["an eerie exotic string riff", "chopped pitched vocal hiccups", "staccato synth plucks"],
      motifs: ["string", "vocal chop", "synth"],
      vocals: ["layered R&B vocals with stacked harmonies", "crisp staccato sung phrasing"],
      textures: ["glossy 2000s R&B polish", "tight dry low end"],
      flows: ["staccato", "offbeat"],
      hooks: ["stutter", "callresp", "mantra"],
      adlibs: ["uh", "yeah", "ooh"],
    },
    {
      id: "pluggnb", label: "Pluggnb", family: "rnb",
      genres: ["pluggnb", "melodic plugg"], bpm: [140, 160], feel: "floaty bounce",
      keys: { minor: [], major: ["C", "D", "Eb", "F", "G", "Ab"] },
      progressions: ["pluggDescent", "bittersweet", "float"],
      moods: ["dreamy", "lovesick and floaty", "sweet euphoria"],
      drums: [
        "bouncy syncopated plugg hi-hats",
        "soft thumping kicks off the beat",
        "crisp snaps",
      ],
      bass: ["smooth sine 808s"],
      leads: ["a dreamy bell synth", "glassy synth chords", "soft vocal-chop pads"],
      motifs: ["bell", "synth"],
      vocals: ["auto-tuned melodic R&B vocals", "airy sing-rap with soft ad-libs"],
      textures: ["soft glossy mix", "hazy stereo shimmer"],
      flows: ["melodic", "triplet"],
      hooks: ["mantra", "stair", "callresp"],
      adlibs: ["yeah", "ooh", "baby"],
    },
    {
      id: "slow-jam", label: "Slow-Jam R&B", family: "rnb",
      genres: ["contemporary R&B", "sensual slow jam"], bpm: [60, 72], feel: "slow deep pocket",
      keys: { minor: ["F", "C", "A#"], major: ["Db", "Eb", "Ab", "Gb", "Bb"] },
      progressions: ["backdoor", "slowJam", "float", "ninthSway"],
      moods: ["sensual", "tender longing", "candlelit intimacy"],
      drums: [
        "crisp syncopated half-time hi-hats",
        "deep round kicks on the pickup",
        "soft snaps landing just behind the beat",
      ],
      bass: ["warm deep 808 bass"],
      leads: ["silky electric piano", "lush vocal-harmony pads", "soft plucked guitar"],
      motifs: ["piano", "guitar"],
      vocals: ["soulful R&B vocals with melismatic runs", "breathy intimate verses opening into belted hooks"],
      textures: ["warm polished mix", "close-mic intimacy"],
      flows: ["melodic", "offbeat"],
      hooks: ["mantra", "stair", "callresp"],
      adlibs: ["ooh", "yeah", "mm"],
    },
  ];
  const LANE_BY_ID = new Map(LANES.map((l) => [l.id, l]));

  // ---------------------------------------------------------------- edge
  // How far past the grid the groove goes. Syncopation is always on.
  const EDGES = {
    syncopated: {
      label: "Syncopated",
      phrases: [
        "heavily syncopated groove with accents off the beat",
        "off-beat kick placement pushing against the snare",
        "syncopated bounce that never lands where you expect",
      ],
      extra: [],
      sliders: { weirdness: [35, 45], styleInfluence: [75, 90] },
      note: "Syncopated: moderate Weirdness keeps the bounce inventive; high Style Influence holds the lane and keeps the banned genres out.",
    },
    twitch: {
      label: "Twitch",
      phrases: [
        "twitchy stutter-edited vocal chops",
        "glitchy micro-edits and stuttered hi-hat bursts",
        "808 retriggers twitching on the off-beats",
      ],
      extra: ["sudden half-bar beat mutes for impact", "heavily syncopated groove with accents off the beat"],
      sliders: { weirdness: [45, 60], styleInfluence: [70, 85] },
      note: "Twitch: push Weirdness into the mid range for the glitch edits, but keep Style Influence at 70+ — below that the stutters start dragging the track toward EDM.",
    },
    experimental: {
      label: "Experimental",
      phrases: [
        "left-field experimental sound design",
        "unpredictable rhythmic displacement",
        "warped pitch-shifted textures",
      ],
      extra: ["twitchy stutter-edited vocal chops", "glitchy micro-edits and stuttered hi-hat bursts"],
      sliders: { weirdness: [60, 75], styleInfluence: [65, 80] },
      note: "Experimental: high Weirdness for left-field choices, Style Influence still 65+ so the genre anchor survives. On v6-wild, the model supplies the chaos, so the Weirdness range drops. Generate in batches and keep the strangest take that still bangs.",
    },
  };

  // Competing positives for the "no fillers / no fills" rule.
  const LOCKED_GROOVE = [
    "locked unbroken drum loop",
    "hard cuts between sections",
    "808 and hi-hats carry every transition",
    "tight minimal arrangement where every sound hits",
  ];

  // ---------------------------------------------------------------- flows
  const FLOWS = {
    triplet: { tag: "triplet flow", guide: "3 syllables per beat; let the accents roll across the bar line" },
    offbeat: { tag: "off-beat flow", guide: "start lines on the 'and' of 1; land rhymes just before the snare" },
    stopstart: { tag: "stop-start flow", guide: "fire for 2 beats, rest half a bar; the silence is the syncopation" },
    staccato: { tag: "staccato flow", guide: "clipped one-word punches, hard consonants, no run-on lines" },
    double: { tag: "double-time bursts", guide: "sprint 2 bars double-time, then drop back into the pocket" },
    melodic: { tag: "melodic sing-rap", guide: "sing the last 3 syllables of every bar and stay inside 5 notes" },
  };

  // ---------------------------------------------------------------- hooks
  const HOOKS = {
    chant: {
      name: "Chant loop", map: "A · A · A · B", syllables: "4–6 syllables per line",
      rule: "Line A is the title. Say it three times, word for word; line B is the only new information.",
      contour: "narrow",
    },
    callresp: {
      name: "Call & response", map: "A (x) · B (x) · A (x) · C (x)", syllables: "5–8 syllables + a 1–2 word answer",
      rule: "Every lead line gets the same ad-lib answer in parentheses. The answer never changes; that's what people shout back.",
      contour: "narrow",
    },
    stair: {
      name: "Stair-step", map: "A · A′ · A″ · B", syllables: "6–8 syllables per line",
      rule: "Keep the rhythm identical for three lines; only the last word changes, and each one lands a step higher.",
      contour: "rising",
    },
    mantra: {
      name: "Two-line mantra", map: "A · B · A · B", syllables: "6–9 syllables per line",
      rule: "Line B rhymes back into the title. The second pass is word-for-word identical to the first.",
      contour: "arch",
    },
    stutter: {
      name: "Stutter hook", map: "A · A · B · A", syllables: "3–5 syllables per line",
      rule: "Stutter the first syllable of the title (ta-ta-title). The twitch is the hook; write it with hyphens so Suno chops it.",
      contour: "narrow",
    },
  };

  const RHYTHM_CELLS = {
    hiphop: [
      "da-da-DUM · da-da-DUM · (rest) da-DUM",
      "DUM (rest) da-da DUM · DUM (rest) da-da DUM",
      "da-DUM-da da-DUM-da · da-DUM (rest)",
      "(rest) da-DUM · (rest) da-DUM · da-da-da-DUM",
    ],
    rnb: [
      "da-da DUUUM · da-da-da DUM (hold)",
      "(rest) da-DUM da-da · DUUUM",
      "da-DUM · da-DUM · da-da-da-DUUUM",
    ],
  };

  const HARMONIC_PLANS = {
    loop: { label: "One loop", note: "One progression under every section. The hook hits harder through added layers and vocal stacks, not new chords. This is the trap/drill default: repetition is the hook." },
    lift: { label: "Hook lift", note: "Verses ride a stripped vamp; the pre-chorus hangs on the tension chord; the hook brings the full progression and its cadence. The R&B default." },
  };

  // ---------------------------------------------------------------- random
  function rng(seed) {
    let t = (seed >>> 0) || 0x9e3779b9;
    return () => {
      t = (t + 0x6d2b79f5) | 0;
      let r = Math.imul(t ^ (t >>> 15), 1 | t);
      r = (r + Math.imul(r ^ (r >>> 7), 61 | r)) ^ r;
      return ((r ^ (r >>> 14)) >>> 0) / 4294967296;
    };
  }
  const pick = (arr, r) => arr[Math.floor(r() * arr.length)];
  function shuffle(arr, r) {
    const out = [...arr];
    for (let i = out.length - 1; i > 0; i--) {
      const j = Math.floor(r() * (i + 1));
      [out[i], out[j]] = [out[j], out[i]];
    }
    return out;
  }

  // ---------------------------------------------------------------- harmony
  function spellChord(tonic, tonality, [offset, quality]) {
    return noteName(PC[tonic] + offset, tonic, tonality) + QUALITY_SUFFIX[quality];
  }

  function chooseHarmony(lane, opts, r) {
    const progId = opts.progression && PROGRESSIONS[opts.progression] ? opts.progression : pick(lane.progressions, r);
    const prog = PROGRESSIONS[progId];
    let pool = lane.keys[prog.tonality];
    if (!pool.length) pool = prog.tonality === "minor" ? ["A", "F#", "C#"] : ["C", "Eb", "Ab"];
    let tonic = pick(pool, r);
    if (opts.key) {
      const [kTonic, kTon] = opts.key.split(" ");
      if (kTon === prog.tonality && PC[kTonic] !== undefined) tonic = kTonic;
    }
    const chords = prog.chords.map((c) => spellChord(tonic, prog.tonality, c));
    const keyName = `${tonic} ${prog.tonality}`;
    const scaleFifth = noteName(PC[tonic] + 7, tonic, prog.tonality);
    const scaleThird = noteName(PC[tonic] + (prog.tonality === "minor" ? 3 : 4), tonic, prog.tonality);
    return { id: progId, prog, tonic, tonality: prog.tonality, keyName, chords, fifth: scaleFifth, third: scaleThird };
  }

  // Map the progression onto each section per the harmonic plan.
  function sectionHarmony(h, plan) {
    const full = h.chords;
    const n = full.length;
    const tonicIdx = Math.max(0, h.prog.chords.findIndex(([o]) => o === 0));
    const tonicChord = full[tonicIdx];
    // The chord that leads back into the tonic carries the loop's tension.
    const tension = full[(tonicIdx - 1 + n) % n];
    const deceptive = spellChord(h.tonic, h.tonality, h.tonality === "minor" ? [8, "maj7"] : [9, "m7"]);
    if (plan === "loop") {
      return { intro: full, verse: full, pre: full, hook: full, bridge: [deceptive, tension], outro: full };
    }
    const vamp = n > 2 ? [tonicChord, full[(tonicIdx + 1) % n]] : [tonicChord];
    return { intro: [tonicChord], verse: vamp, pre: [tension], hook: full, bridge: [deceptive, tension], outro: [tonicChord] };
  }

  // ---------------------------------------------------------------- style
  function buildStyle(ctx) {
    const { lane, blend, edge, harmony, vocalMode, bpm, r } = ctx;
    const parts = [];
    const add = (text, pri) => text && parts.push({ text, pri });

    add(blend ? `${lane.genres[0]} fused with ${blend.genres[0]}` : lane.genres[0], 0);
    const moods = shuffle(lane.moods, r);
    add(moods[0], 1);

    if (ctx.instrumental) {
      add("purely instrumental beat, lead melody carries the hook", 0);
    } else {
      const vocal = pick(lane.vocals, r);
      const gender = {
        male: "male vocalist",
        female: "female vocalist",
        duet: lane.family === "rnb" ? "female lead with a male rap verse" : "male rap verses with a female sung hook",
      }[vocalMode];
      add(vocal, 0);
      add(gender, 0);
    }

    const drums = shuffle(lane.drums, r);
    add(drums[0], 0);
    const edgeDef = EDGES[edge];
    add(pick(edgeDef.phrases, r), 0);
    add(pick(lane.bass, r), 1);

    const lead = pick(blend && r() < 0.5 ? blend.leads : lane.leads, r);
    add(`${lead} playing ${harmony.prog.color} in ${harmony.keyName}`, 0);
    const motif = pick(lane.motifs, r);
    add(`infectious ${r() < 0.5 ? "two" : "one"}-bar ${motif} motif ${ctx.instrumental ? "carrying the hook as the lead line" : "repeating every hook"}`, 1);

    add(drums[1], 2);
    add(pick(LOCKED_GROOVE, r), 1);
    // v6 runs long when the ending is unspecified: state both edges positively.
    add(`opens straight on the ${motif} motif and hard-stops after the last hook`, 1);
    if (edgeDef.extra.length) add(pick(edgeDef.extra, r), 2);
    if (blend) add(pick(blend.drums, r), 3);
    add(moods[1], 4);
    add(pick(lane.textures, r), 3);
    add(`${bpm} BPM ${lane.feel}`, 0);

    // Dedupe, then trim lowest priority until inside the descriptor target.
    const seen = new Set();
    let kept = parts.filter((p) => {
      const k = p.text.toLowerCase();
      if (seen.has(k)) return false;
      seen.add(k);
      return true;
    });
    for (let pri = 4; pri >= 2 && kept.length > DESCRIPTOR_TARGET.max; pri--) {
      for (let i = kept.length - 1; i >= 0 && kept.length > DESCRIPTOR_TARGET.max; i--) {
        if (kept[i].pri === pri) kept.splice(i, 1);
      }
    }
    return kept.map((p) => p.text).join(", ");
  }

  // ---------------------------------------------------------------- structure
  const TEMPLATES = {
    "hook-first": { label: "Hook-first", length: "~2:30" },
    "pre-hook": { label: "Pre-hook", length: "~3:00" },
    "beat-switch": { label: "Beat switch", length: "~3:00" },
    short: { label: "Short", length: "~2:00" },
  };

  function buildSections(ctx) {
    const { lane, edge, template, harmony, plan, chordsInTags, postHook, r, hook } = ctx;
    const sh = sectionHarmony(harmony, plan);
    const flows = shuffle(lane.flows, r);
    const flowA = FLOWS[flows[0]];
    const flowB = FLOWS[flows[1] || flows[0]];
    const flowC = FLOWS[flows[2] || flows[0]];
    const twitch = edge !== "syncopated";
    const adlib = pick(lane.adlibs, r);
    const hookWord = lane.family === "rnb" ? "Chorus" : "Hook";
    const chordText = (list) => (chordsInTags ? `, ${list.join(" – ")}` : "");

    const S = [];
    const push = (tag, desc, slot, extra = {}) => S.push({ tag: `[${tag}${desc ? `: ${desc}` : ""}]`, name: tag, slot, ...extra });

    const intro = twitch
      ? `stuttered chop of the hook motif, 2 bars${chordText(sh.intro)}`
      : `hook motif alone, 2 bars${chordText(sh.intro)}`;
    const hookDesc = () =>
      `${hook.contour === "narrow" ? "chant-ready" : "sticky melody"}, identical every time, doubled vocals, "${adlib}" ad-lib answers${chordText(sh.hook)}`;
    const verseDesc = (flow, n) => {
      const bits = [flow.tag];
      if (twitch && n > 1) bits.push("stutter-chopped ad-libs between bars");
      else bits.push(n === 1 ? "beat locked in" : "denser rhymes");
      return bits.join(", ") + chordText(sh.verse);
    };
    const hookSec = () => {
      push(hookWord, hookDesc(), "hook", { guide: `${hook.name}: ${hook.map}`, adlib, harmony: sh.hook });
      if (postHook) push("Post-Hook", `2-word chant x4, beat stays full${chordText(sh.hook)}`, "posthook", { guide: "2-word chant, repeated 4 times", harmony: sh.hook });
    };
    const verse = (n, flow) =>
      push(`Verse ${n}`, verseDesc(flow, n), `verse${n}`, { guide: `12–16 bars · ${flow.guide}`, harmony: sh.verse });
    const pre = () =>
      push("Pre-Chorus", `808 cuts out, tension hangs unresolved${chordsInTags ? ` on ${sh.pre[0]}` : ""}`, "pre", {
        guide: "2–4 lines, shorter words each line, end on an unresolved note",
        harmony: sh.pre,
      });

    push("Intro", intro, null, { harmony: sh.intro });

    if (template === "pre-hook") {
      verse(1, flowA); pre(); hookSec();
      verse(2, flowB); pre(); hookSec();
      push("Bridge", `half-time, stripped to chords and voice, lands on a deceptive chord${chordText(sh.bridge)}`, "bridge", {
        guide: "2–4 lines, new angle on the title; the deceptive chord makes the last hook hit harder",
        harmony: sh.bridge,
      });
      hookSec();
    } else if (template === "short") {
      hookSec(); verse(1, flowA); hookSec();
      push("Break", "beat cuts to 808 and voice for 2 bars", "break", { guide: "1–2 lines, the hardest bar in the song", harmony: sh.verse });
      hookSec();
    } else {
      hookSec(); verse(1, flowA); hookSec(); verse(2, flowB); hookSec();
      if (template === "beat-switch") {
        push("Beat Switch", `new drum pattern, darker 808, same tempo${chordsInTags ? `, ${harmony.chords[0]} pedal` : ""}`, null, { harmony: [harmony.chords[0]] });
        verse(3, flowC);
        hookSec();
      }
    }
    push("Outro", `hook motif for 2 bars, then hard stop${chordText(sh.outro)}`, "outro", { guide: "1–2 lines: the title one last time", harmony: sh.outro });
    S.push({ tag: "[End]", name: "End", slot: null });
    return S;
  }

  // Instrumental arrangement: the same skeleton, but every tag says what the
  // beat does. Vocal words never appear, so Suno has nothing to sing.
  function buildInstrumentalSections(ctx) {
    const { lane, edge, template, harmony, plan, chordsInTags, postHook, r } = ctx;
    const sh = sectionHarmony(harmony, plan);
    const twitch = edge !== "syncopated";
    const motif = pick(lane.motifs, r);
    const lead = pick(lane.leads, r);
    const drums = shuffle(lane.drums, r);
    const chordText = (list) => (chordsInTags ? `, ${list.join(" – ")}` : "");
    const S = [];
    const push = (tag, desc, extra = {}) => S.push({ tag: `[${tag}: ${desc}]`, name: tag, slot: null, ...extra });

    const hookSec = () => {
      push("Hook", `full beat, ${motif} motif doubled an octave up, ${lane.bass[0]}, identical every time${chordText(sh.hook)}`, { harmony: sh.hook });
      if (postHook) push("Post-Hook", `motif alone over drums, 4 bars${chordText(sh.hook)}`, { harmony: sh.hook });
    };
    const verse = (n) => {
      const bits = [`stripped to ${drums[n % drums.length]}`, n === 1 ? `${lead} sparse` : twitch ? `${lead} stutter-chopped` : `${lead} denser variation`];
      if (twitch && n > 1) bits.push("half-bar beat mutes");
      push(`Verse ${n}`, `${bits.join(", ")}, 16 bars${chordText(sh.verse)}`, { harmony: sh.verse });
    };
    const pre = () => push("Pre-Chorus", `808 cuts out, ${lead} holds the tension chord, hi-hats tighten${chordsInTags ? ` on ${sh.pre[0]}` : ""}`, { harmony: sh.pre });

    push("Intro", twitch ? `stuttered chop of the ${motif} motif, 2 bars, drums enter on bar 3${chordText(sh.intro)}` : `${motif} motif alone, 2 bars, drums enter on bar 3${chordText(sh.intro)}`, { harmony: sh.intro });
    if (template === "pre-hook") {
      verse(1); pre(); hookSec(); verse(2); pre(); hookSec();
      push("Bridge", `half-time, chords and 808 only, lands on a deceptive chord${chordText(sh.bridge)}`, { harmony: sh.bridge });
      hookSec();
    } else if (template === "short") {
      hookSec(); verse(1); hookSec();
      push("Break", "808 and hi-hats only for 2 bars", { harmony: sh.verse });
      hookSec();
    } else {
      hookSec(); verse(1); hookSec(); verse(2); hookSec();
      if (template === "beat-switch") {
        push("Beat Switch", `new drum pattern, darker 808, same tempo${chordsInTags ? `, ${harmony.chords[0]} pedal` : ""}`, { harmony: [harmony.chords[0]] });
        verse(3);
        hookSec();
      }
    }
    push("Outro", `${motif} motif for 2 bars, then hard stop${chordText(sh.outro)}`, { harmony: sh.outro });
    S.push({ tag: "[End]", name: "End", slot: null });
    return S;
  }

  function renderSections(sections, lyrics = {}) {
    // A repeated slot (the hook) reprints the same words: identical repetition
    // is what makes a hook stick.
    return sections
      .map((s) => {
        const body = s.slot && lyrics[s.slot] ? lyrics[s.slot].trim() : "";
        return body ? `${s.tag}\n${body}` : s.tag;
      })
      .join("\n\n");
  }

  // ---------------------------------------------------------------- hook blueprint
  function buildBlueprint(ctx) {
    const { lane, hook, harmony, r, edge } = ctx;
    const cell = pick(RHYTHM_CELLS[lane.family], r);
    const contour = {
      narrow: `Two or three pitches. Park the title on ${harmony.tonic} (the tonic) and let the last word fall there every time.`,
      rising: `Each repeat climbs a step: start on ${harmony.tonic}, then ${harmony.third}, then peak on ${harmony.fifth} before line B falls home.`,
      arch: `Rise to ${harmony.fifth} (the 5th) on the title word, then walk back down stepwise to ${harmony.tonic}.`,
    }[hook.contour];
    return {
      name: hook.name,
      map: hook.map,
      syllables: hook.syllables,
      rule: hook.rule,
      rhythm: cell,
      contour,
      rules: [
        "The hook arrives within the first 15 seconds and appears at least 3 times.",
        "Title in the first and last line of the hook.",
        "Longer notes and small steps are easier to sing back; save leaps for the title word only.",
        edge === "syncopated"
          ? "Start the hook on an off-beat (the 'and' of 1) so it snags against the kick."
          : "Chop the title into stuttered syllables once per hook (ti-ti-title); the edit becomes the earworm.",
        "Every verse bar should earn its place: an image, a punchline or a flow switch. No throwaway bars.",
      ],
    };
  }

  // ---------------------------------------------------------------- lint
  function lint(text, where) {
    const found = [];
    for (const rule of LINT_RULES) {
      if (rule.styleOnly && where !== "style") continue;
      const m = text.match(rule.re);
      if (m) found.push({ level: rule.level, message: `${where}: "${m[0]}" — ${rule.msg}.` });
    }
    return found;
  }

  // Instrumental packages must not say "vocal" anywhere, or Suno may add some.
  function devocalize(text) {
    return text
      .replace(/vocal-harmony pads/gi, "synth pads")
      .replace(/vocal[- ]chop/gi, "synth chop")
      .replace(/vocal pads?/gi, (m) => m.replace(/vocal/i, "synth"))
      .replace(/chopped pitched vocal hiccups/gi, "chopped pitched synth hiccups")
      .replace(/beatbox-style vocal percussion/gi, "clicky glitch percussion");
  }

  function buildExclude(extraBans) {
    const list = [...HARD_BANS];
    for (const b of extraBans || []) {
      const t = String(b).trim().toLowerCase();
      if (t && !list.includes(t)) list.push(t);
    }
    return list.join(", ");
  }

  // ---------------------------------------------------------------- main
  function generate(input = {}) {
    const profile = PROFILES[input.profile] || PROFILES[DEFAULT_PROFILE];
    const lane = LANE_BY_ID.get(input.lane) || LANES[0];
    const blend = input.blend && input.blend !== lane.id ? LANE_BY_ID.get(input.blend) : undefined;
    const edge = EDGES[input.edge] ? input.edge : "twitch";
    const seed = input.seed >>> 0;
    const r = rng(seed);
    const warnings = [];

    const harmony = chooseHarmony(lane, input, r);
    if (input.key && input.key.split(" ")[1] !== harmony.tonality) {
      warnings.push({
        level: "info",
        message: `${harmony.prog.name} is a ${harmony.tonality}-key progression, so the ${input.key} request was swapped for ${harmony.keyName}.`,
      });
    }
    const plan = HARMONIC_PLANS[input.plan] ? input.plan : lane.family === "rnb" ? "lift" : "loop";
    const template = TEMPLATES[input.template] ? input.template : lane.family === "rnb" ? "pre-hook" : "hook-first";

    let bpm = input.bpm;
    const [lo, hi] = lane.bpm;
    if (!bpm) bpm = lo + Math.round((r() * (hi - lo)) / 2) * 2;
    else if (bpm < lo || bpm > hi) {
      warnings.push({ level: "warn", message: `${bpm} BPM is outside ${lane.label}'s ${lo}–${hi} range; using it anyway.` });
    }

    let hookPool = [...lane.hooks];
    if (edge !== "syncopated" && !hookPool.includes("stutter")) hookPool.push("stutter");
    const hookId = input.hook && HOOKS[input.hook] ? input.hook : pick(hookPool, r);
    const hook = HOOKS[hookId];

    const vocalMode = ["male", "female", "duet"].includes(input.vocal) ? input.vocal : "auto";
    const ctx = {
      lane, blend, edge, harmony, plan, template, bpm, r, hook, vocalMode,
      chordsInTags: !!input.chordsInTags, postHook: !!input.postHook, instrumental: !!input.instrumental,
    };

    let styleText = buildStyle(ctx);
    if (ctx.instrumental) styleText = devocalize(styleText);
    if (styleText.length > profile.styleCharLimit) {
      styleText = styleText.slice(0, profile.styleCharLimit).replace(/,\s*[^,]*$/, "");
      warnings.push({ level: "warn", message: `Style text trimmed to the ${profile.styleCharLimit}-character limit.` });
    }
    const sections = ctx.instrumental ? buildInstrumentalSections(ctx) : buildSections(ctx);
    if (ctx.instrumental) for (const s of sections) s.tag = devocalize(s.tag);
    const tagsOnly = renderSections(sections);
    const excludeText = buildExclude(ctx.instrumental ? ["vocals", "rap", "singing", ...(input.extraBans || [])] : input.extraBans);
    const blueprint = buildBlueprint(ctx);

    warnings.push(...lint(styleText, "style"), ...lint(tagsOnly, "tags"));
    if (tagsOnly.length > profile.lyricsCharLimit) {
      warnings.push({ level: "warn", message: `Lyrics exceed ${profile.lyricsCharLimit} characters.` });
    }

    const sh = sectionHarmony(harmony, plan);
    return {
      styleText,
      excludeText,
      lyricsTagsOnly: tagsOnly,
      sections,
      harmony: {
        key: harmony.keyName,
        progression: harmony.prog.name,
        roman: harmony.prog.roman,
        chords: harmony.chords,
        cadence: CADENCES[harmony.prog.cadence],
        plan: HARMONIC_PLANS[plan],
        bySection: [
          ["Intro", sh.intro],
          ["Verse", sh.verse],
          ...(template === "pre-hook" ? [["Pre-Chorus", sh.pre]] : []),
          [lane.family === "rnb" ? "Chorus" : "Hook", sh.hook],
          ...(template === "pre-hook" ? [["Bridge", sh.bridge]] : []),
          ["Outro", sh.outro],
        ],
      },
      blueprint,
      sliders: {
        weirdness: EDGES[edge].sliders.weirdness.map((v) => clamp(v + profile.weirdnessShift)),
        styleInfluence: EDGES[edge].sliders.styleInfluence.map((v) => clamp(v + profile.styleShift)),
        // Above Off, Variety rewrites the style prompt and can undo the bans.
        variety: "Off",
        maxMode: profile.id === "v6-mini" ? "Not needed for test drafts" : "On for the keeper (2× credits; holds the whole song consistent)",
      },
      sliderNote: `${EDGES[edge].note} ${profile.note}`,
      warnings,
      meta: {
        profile: profile.id, lane: lane.id, laneLabel: lane.label, blend: blend && blend.id,
        edge, bpm, seed, template, templateLength: TEMPLATES[template].length, plan, hook: hookId,
        instrumental: ctx.instrumental,
        styleChars: styleText.length, styleLimit: profile.styleCharLimit,
        descriptors: styleText.split(", ").length,
      },
    };
  }

  function keyOptions() {
    const minor = [...new Set(LANES.flatMap((l) => l.keys.minor))].map((k) => `${k} minor`);
    const major = [...new Set(LANES.flatMap((l) => l.keys.major))].map((k) => `${k} major`);
    return [...minor.sort(), ...major.sort()];
  }

  return {
    PROFILES, LANES, EDGES, HOOKS, PROGRESSIONS, CADENCES, TEMPLATES, HARMONIC_PLANS,
    HARD_BANS, OPTIONAL_BANS, generate, renderSections, lint, keyOptions,
  };
})();

if (typeof module !== "undefined") module.exports = CipherEngine;
