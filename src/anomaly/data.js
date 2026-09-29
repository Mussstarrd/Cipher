/* ANOMALY vocabulary — built from docs/producer-brief.md (executive-producer
 * sonic brief for 2025–2026 hip hop and R&B radio instrumentals). Every phrase
 * is written to be pasted verbatim into a Suno v6 style field. */
"use strict";

const AnomalyData = (() => {
  const STYLE_LIMIT = 1000;
  const LYRICS_LIMIT = 5000;
  // Share of rolls allowed to reach for a piano lead at all.
  const PIANO_SHARE = 0.12;

  const MINOR_KEYS = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];
  const MAJOR_KEYS = ["C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B"];

  // [semitones above tonic, chord suffix]
  const PROGRESSIONS = [
    { id: "pendulum", name: "Two-chord pendulum", tonality: "minor", roman: "i – ♭VI", chords: [[0, "m"], [8, ""]], color: "a brooding two-chord minor loop", cadence: "Open loop; it never resolves, so the ear keeps leaning in." },
    { id: "phrygian", name: "Phrygian hover", tonality: "minor", roman: "i – ♭II", chords: [[0, "m"], [1, ""]], color: "a half-step Phrygian loop", cadence: "♭II falls a half step into the tonic: drill and hard-trap dread." },
    { id: "sting", name: "Harmonic-minor sting", tonality: "minor", roman: "i – ♭VI – V", chords: [[0, "m"], [8, ""], [7, ""]], color: "a harmonic-minor loop with a tense major V", cadence: "Half cadence on V; the leading tone snaps the loop back to i every time." },
    { id: "lament", name: "Descending lament", tonality: "minor", roman: "i – ♭VII – ♭VI – V", chords: [[0, "m"], [10, ""], [8, ""], [7, ""]], color: "a falling four-chord minor bassline", cadence: "Andalusian descent; the bass walks down and parks on V." },
    { id: "climb", name: "Aeolian climb", tonality: "minor", roman: "i – ♭VI – ♭III – ♭VII", chords: [[0, "m"], [8, ""], [3, ""], [10, ""]], color: "a cinematic four-chord minor loop", cadence: "♭VII climbs a whole step home; sturdy and dark." },
    { id: "coldFifth", name: "Cold fifth", tonality: "minor", roman: "i – v – iv – ♭VII", chords: [[0, "m"], [7, "m"], [5, "m"], [10, ""]], color: "a cold all-minor loop", cadence: "All minor chords; no leading tone, no relief." },
    { id: "sway", name: "Minor plagal sway", tonality: "minor", roman: "i – iv", chords: [[0, "m"], [5, "m"]], color: "a hypnotic minor i–iv sway", cadence: "iv to i: the soft release, soulful without getting sweet." },
    { id: "drone", name: "Pedal drone", tonality: "minor", roman: "i (pedal)", chords: [[0, "m"]], color: "a one-chord minor drone", cadence: "No cadence; all motion lives in the 808 and the hats." },
    { id: "dorian", name: "Dorian lift", tonality: "minor", roman: "i7 – IV7", chords: [[0, "m7"], [5, "7"]], color: "a Dorian two-chord loop with a bright IV", cadence: "The major IV inside a minor key: warm, sly, Afro and R&B friendly." },
    { id: "ninthSway", name: "Minor-ninth sway", tonality: "minor", roman: "i9 – iv9", chords: [[0, "m9"], [5, "m9"]], color: "a lush minor-ninth two-chord sway", cadence: "Ninth voicings turn a plain sway into late-night R&B." },
    { id: "lateNight", name: "Late-night climb", tonality: "minor", roman: "i9 – ♭VImaj7 – ♭IIImaj7 – ♭VII", chords: [[0, "m9"], [8, "maj7"], [3, "maj7"], [10, ""]], color: "lush minor-ninth chords climbing to the subtonic", cadence: "Rich extensions, dark resolution." },
    { id: "resolve", name: "Aeolian resolve", tonality: "minor", roman: "♭VImaj7 – ♭VII – i9", chords: [[8, "maj7"], [10, ""], [0, "m9"]], color: "chords climbing up to land on the minor tonic", cadence: "Lands on i like a verdict." },
    { id: "backdoor", name: "Backdoor home", tonality: "major", roman: "iv7 – ♭VII9 – Imaj7", chords: [[5, "m7"], [10, "9"], [0, "maj7"]], color: "warm borrowed-chord R&B changes", cadence: "Backdoor cadence: the gospel road home, warm not bright." },
    { id: "float", name: "Plagal float", tonality: "major", roman: "IVmaj7 – Imaj7", chords: [[5, "maj7"], [0, "maj7"]], color: "a floating major-seventh sway", cadence: "IV to I: weightless, never pushes." },
    { id: "bittersweet", name: "Bittersweet iv", tonality: "major", roman: "Imaj7 – iii7 – IVmaj7 – iv6", chords: [[0, "maj7"], [4, "m7"], [5, "maj7"], [5, "m6"]], color: "bittersweet major-seventh chords with a borrowed minor iv", cadence: "The borrowed iv is the sigh at the end of the loop." },
    { id: "susCycle", name: "Suspended cycle", tonality: "major", roman: "vi9 – IVmaj7 – Imaj7 – Vsus4", chords: [[9, "m9"], [5, "maj7"], [0, "maj7"], [7, "sus4"]], color: "silky suspended major-seventh changes", cadence: "Hangs on the sus chord so the return feels like relief." },
  ];

  // Producer blueprints are the worlds now (see blueprints.js).
  const BLUEPRINTS = typeof AnomalyBlueprints !== "undefined" ? AnomalyBlueprints : require("./blueprints.js");
  const WORLDS = BLUEPRINTS;
  const ARTIST_NAMES = [...new Set(BLUEPRINTS.flatMap((b) => b.aliases))].filter((n) => n.replace(/[^a-z0-9]/gi, "").length >= 3);

  // Groove, in the words producers use. Meter play is part of it: Suno reacts
  // to "6/8" and "waltz" far more than to milliseconds.
  const SWING = [
    "drunk off-kilter swing", "lurching humanized MPC swing", "lopsided shuffle that drags then rushes",
    "waltz-feel 3/4 swing layered over the 4/4 kick", "6/8 triplet stumble", "broken stop-start pocket",
    "lazy behind-the-beat swing", "loose hand-played timing that never sits on the grid",
    "12/8 shuffle with a limp", "sloppy-tight swing with rushed hats and a late snare",
    "stumbling half-time swing", "off-grid drums that fall on and off the beat",
  ];
  const SYNC = [
    "kick skipping on and off the beat", "808 hitting on the offbeats", "hats in a 3-3-2 pattern",
    "snare pushed to the and-of-3", "clap displaced to the and-of-2 every fourth bar",
    "bass resting on 1 and entering on the and-of-1", "kick in duple against 808 in triplets",
    "melody accents only on the offbeat 16ths", "ghost snare on the and-of-4", "beat 4 left empty every bar",
    "open hat on the 16th before every snare", "808 slides that start on the and-of-4",
  ];
  // Glitch as rhythm, said plainly.
  const GLITCH = [
    "808 stutters on the last beat of every 8 bars", "half-beat of dead silence before every fourth snare",
    "snare retriggers in 16ths across beat 4 every 4 bars", "bit-crushed stab repeated three times then cut",
    "hi-hat stalls on one repeated 16th for half a bar", "kick doubles into a flam on beat 1 of the hook",
    "melody restarts from its first note on the and-of-4", "whole beat mutes for one 8th before every downbeat",
    "808 jumps an octave for one 16th and drops back", "clap decelerates in a stutter over the last beat",
    "hat burst replaces the snare once every 8 bars", "loop skips beat 4 and jumps to the next bar",
  ];
  // One unexpected element per roll. This is where the mixtures come from.
  const CLASH = [
    "punchy horn-section stabs", "low brass stabs", "a muted-trumpet-style synth stab", "a tuba-like bass blurt",
    "a flamenco guitar run", "a harpsichord riff", "a church organ swell", "a steel-pan pluck",
    "pizzicato strings", "a bowed double bass", "a tabla roll", "a mellotron flute",
    "a distorted electric guitar stab", "a detuned toy organ", "a marching-band snare rudiment", "an accordion stab",
    "a sitar riff", "a koto pluck", "a bagpipe-like drone", "an orchestral timpani hit",
    "a slap-back dub echo on the snare", "a reversed cymbal swell", "a pitched-down cello stab", "a cheap 80s synth-brass hit",
  ];
  // Lead voices: low and mid register, physical and textured. Never a bright pluck.
  const GLOBAL_LEADS = [
    "low-register horn-section riff", "growling baritone synth-brass riff", "dark muted-guitar riff", "low cello line",
    "gritty organ riff", "warm mid-register nylon-guitar riff", "detuned mid-register analog lead", "low tremolo-guitar riff",
    "hollow woodwind-style synth riff", "dusty sampled string riff", "mid-register harp figure", "bowed string riff",
    "distorted low guitar riff", "low pitched-down bell riff", "mid-register flute-style synth riff", "warm electric-keys riff",
    "low fuzz-bass riff", "grainy tape-worn lead riff", "muted felt piano riff",
  ];
  const MIX = [
    "loud clean radio master with a heavy mono sub", "dry punchy drums, wide melody, mono 808",
    "polished mid-forward mix, dark top end", "tight low end, 808 that reads on a phone",
    "glued and competitive, never pumping", "crisp hats, snappy snare, sub felt not heard",
  ];

  const ADJECTIVES = [
    "chrome-cold", "velvet-scorched", "pewter-lit", "glacial-tender", "furnace-pressed", "satin-sly", "ink-warm", "bruised-silk",
    "amber-heavy", "midnight-plush", "lacquer-slow", "sun-lacquered", "copper-swaying", "dusk-sweet", "gloss-drunk", "chrome-tender",
    "rollerskate-slick", "asphalt-hot", "lowrider-slow", "swagger-glossy", "wraith-cold", "iron-lit", "prowling-elegant", "serrated-bright",
    "streetlight-orange", "jittery-brash", "sneering-loose", "breathless-metallic", "strobe-stung", "sweat-bright", "powder-soft", "dawn-hazed",
    "pastel-numb", "neon-warm", "candlelit-cocky", "sedated-luxe", "marble-quiet", "tar-thick", "frost-bitten-smooth", "oil-slick-dark",
    "smoke-grained", "nickel-dry", "rain-slicked", "sodium-lit", "basement-damp", "humid-plush", "cathedral-cold", "concrete-warm",
    "mirror-flat", "sandpaper-soft", "molten-lazy", "bone-dry", "bronze-dull", "granite-heavy", "syrup-slow", "static-prickled",
    "glass-thin", "leather-worn", "ember-dim", "blood-warm",
  ];

  const INSTRUMENTAL_LOCK = [
    "purely instrumental, melody-led",
    "instrumental only, the riff is the hook",
    "wordless instrumental beat, riff-led",
  ];

  const BEAT_SWITCH = [
    "tempo halves, the same melody continues over a slow trap-soul drum pattern",
    "tempo doubles into a Jersey club kick pattern with the same 808",
    "the key drops a whole step and the pad is replaced by a string ensemble",
    "trap hats swap for a drill hat pattern and the 808 starts sliding",
    "the 808 changes from a long distorted tone to a short round sub",
    "melody flips from a bright pluck to a dark low guitar, drums stay",
    "all drums drop for 4 bars, then a Detroit-style 808 bass melody takes over",
    "the same chords re-voiced on chorused electric keys at half tempo",
    "the texture flips from dry and cramped to wide and reverb-soaked",
    "drums cut to a bare kick and clap, and the lead is replaced by a synth-brass stab",
    "two beats of silence then a faster, harder trap pattern in a new minor key",
    "the 808 becomes the melody, playing pitched 8th-note runs while the pad drops out",
  ];
  const SIGNATURES = [
    "a reversed snare that swallows beat 4 every 8th bar",
    "one detuned synth-bell note that rings on the and-of-4 of every fourth bar, always the same pitch",
    "the 808 glides up a full octave for exactly one beat at the end of every 8-bar phrase",
    "a half-beat of total silence right before every hook downbeat",
    "a bit-crushed guitar pluck that hiccups twice on beat 2 every second bar",
    "the hi-hats collapse into a single repeated 16th for the last beat of every 4 bars",
    "a low string stab that lands a 16th early on the 1 of every 8th bar",
    "the kick disappears for one whole bar every 16 bars while the 808 keeps going",
    "a pitched-down snare that answers the normal snare on the and-of-3 every fourth bar",
    "a synth flute note that bends a quarter-tone flat on the last note of every loop",
    "the entire mix drops to mono for the first beat of every hook",
    "an 808 that retriggers in a fast triplet on the and-of-2 every 8 bars",
    "a single muted guitar harmonic that pings on beat 3 of every second bar",
    "the pad cuts hard to silence for the final 8th of every 4 bars",
    "a clap doubled with a reversed version, so each backbeat has a small inhale before it",
    "a synth-brass stab that plays only once every 16 bars, always a tritone off the key",
    "the melody loop is one 16th shorter than 4 bars, so it slowly rotates against the drums",
    "a sub-bass drop of one octave on the final beat of every phrase, felt in the chest",
    "a bit-crushed 808 hit replaces the kick on beat 1 of every 8th bar",
    "a bell-like synth note struck on the and-of-1 of the first bar only, then again at every beat switch",
  ];
  // The technical flex: polymeter, odd bars, tuplets and micro-timing that
  // only a producer who counts would put in a radio record.
  const TECH_FLEX = [
    "hi-hats phrased in 7 against the 4/4 kick so the accents rotate every 7 beats",
    "one bar of 7/8 inserted before every hook, the loop resolves a beat early",
    "808 phrased in 5-beat groups over the 4/4 drums, realigning every 20 beats",
    "quintuplet hi-hat rolls instead of triplets on every fourth bar",
    "the snare alternates between dead-on and 1/32 late in a strict A-B pattern",
    "melody in 3/4 over drums in 4/4, meeting on the downbeat every 12 beats",
    "kick pattern built on a 3-3-3-3-2-2 subdivision across 2 bars",
    "the 808 slides exactly a perfect fifth on the and-of-3 of every 4th bar",
    "a 9-beat melody phrase that drifts one beat later through every 8-bar cycle",
    "hi-hat rolls that accelerate from 16ths to 32nds to 64ths across a single beat",
    "clap pattern in 6/8 feel laid over the straight 4/4 kick",
    "every 8th bar is a bar of 5/4, the extra beat is 808 alone",
    "septuplet 808 stutter on the last beat of every 16-bar section",
    "the melody's rhythm is the drum pattern played backwards",
  ];
  const STRUCTURES = [
    { id: "radio-melodic", label: "Radio melodic", length: "~2:45", steps: ["intro", "verse", "hookSig", "verse2", "hook", "switch", "switchHook", "outro"] },
    { id: "drill-radio", label: "Drill radio", length: "~2:30", steps: ["intro", "verse", "hook", "verse2", "break", "switch", "switchHook", "outro"] },
    { id: "rnb-slow", label: "R&B slow", length: "~3:10", steps: ["intro", "verse", "pre", "hookSig", "verse2", "hook", "switch", "bridge", "hookSig", "outro"] },
    { id: "bounce", label: "Bounce", length: "~2:20", steps: ["intro", "verse", "hook", "verse2", "switch", "switchHook", "outro"] },
    { id: "hybrid", label: "Club hybrid", length: "~2:35", steps: ["intro", "verse", "hookSig", "verseShort", "switch", "switchHook", "outro"] },
    { id: "short-loop", label: "Short loop", length: "~1:30", steps: ["intro", "hook", "verseShort", "hookSig", "switch", "hookSig", "outro"] },
  ];

  const EXCLUDE = [
    "vocals", "singing", "humming", "spoken word", "rap vocals", "wordless vocals", "choir", "vocal samples",
    "DJ scratching", "drum fills", "EDM", "big room", "lo-fi hip hop", "jazz", "funk", "dubstep",
  ];

  const SETTINGS = {
    radio: {
      model: "Suno v6", weirdness: "40–55%", styleInfluence: "75–85%", variety: "Off (0)", maxMode: "On",
      note: "Radio finish, but not safe: under 35% Weirdness v6 averages everything into a stock beat, so stay in the 40s. Style Influence high enough to enforce the swing, the register and the clash element. Variety Off so these phrases are not rewritten. Turn Suno's Instrumental toggle on.",
    },
    wild: {
      model: "Suno v6-wild first, then re-run the keeper on v6", weirdness: "55–70%", styleInfluence: "65–75%", variety: "Off (0)", maxMode: "On for the v6 re-run",
      note: "Wild invents the rhythm edits and the clashes; take the keeper's seed to v6 with Max Mode for the radio finish. Turn Suno's Instrumental toggle on.",
    },
  };

  const LINT = [
    { re: /\b(vocal|vocals|vocalist|rap\b|rapper|sing|sings|singing|singer|sung|chant\w*|choir|hum|humming|hummed|lyric\w*|ad-?lib\w*)\b/i, level: "block", msg: "vocal vocabulary invites a voice" },
    { re: /\b(transition\w*|sweeps?|risers?|phaser|dj\b|scratch\w*|fills?|cowbell|rimshot|airhorn)\b/i, level: "block", msg: "effect vocabulary turns into DJ tricks and fills" },
    { re: /\b(marimba|kalimba|music box|glockenspiel|celesta|xylophone|toy piano|dulcimer)\b/i, level: "block", msg: "stock mallet preset" },
    { re: /\b(edm|dubstep|jazz\w*|funk\w*|disco|house\b)\b/i, level: "block", msg: "off-format genre word" },
    { re: /\bno\s+\w+/i, level: "warn", msg: "\"no X\" inside a prompt plants X" },
    ...ARTIST_NAMES.map((n) => ({ re: new RegExp(`(^|[^a-z0-9])${n.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")}($|[^a-z0-9])`, "i"), level: "block", msg: `artist name "${n}"` })),
  ];

  return {
    STYLE_LIMIT, LYRICS_LIMIT, PIANO_SHARE, MINOR_KEYS, MAJOR_KEYS, PROGRESSIONS, WORLDS, BLUEPRINTS, GLOBAL_LEADS,
    ADJECTIVES, INSTRUMENTAL_LOCK, SWING, SYNC, CLASH, GLITCH, BEAT_SWITCH, SIGNATURES, TECH_FLEX, MIX, STRUCTURES,
    SYNCOPATION: SYNC, HUMAN_FEEL: SWING,
    EXCLUDE, SETTINGS, LINT,
  };
})();

if (typeof module !== "undefined") module.exports = AnomalyData;
