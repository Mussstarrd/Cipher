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

  // ------------------------------------------------------------ sound worlds
  const WORLDS = [
    {
      id: "melodic-trap-radio", label: "Melodic Trap", family: "hiphop", genre: "melodic trap, hip hop",
      bpm: [130, 150], feel: "heavy sliding half-time", tonalities: ["minor"],
      drums: [
        "sparse trap kick placed on the and-of-3, layered clap-snare on 3 with a 30 ms late lean",
        "triplet hi-hat rolls in 32nd bursts every fourth bar, open hat on the offbeat",
        "half-time drum grid with a lazy human pocket, hats slightly rushed against a late snare",
      ],
      lowEnd: [
        "long tuned 808 that answers the kick instead of doubling it, saturated for phone speakers",
        "808 slides up a fifth at the end of every 4-bar phrase then decays into sub",
      ],
      leads: ["detuned analog saw lead with slow vibrato", "plucked nylon-string guitar loop, palm-muted", "airy synth flute lead with breath noise", "glassy FM electric-keys pad, chorused"],
      motifs: ["the saw-lead motif", "the palm-muted guitar motif", "the synth-flute motif"],
      texture: ["wide stereo pad with mono sub underneath, dry drums in the center", "faint vinyl crackle bed under the melody, gated off whenever the 808 hits"],
      moods: ["glacial-tender", "pewter-lit", "sedated-luxe"],
    },
    {
      id: "hard-trap", label: "Hard Trap", family: "hiphop", genre: "hard trap, aggressive hip hop",
      bpm: [140, 160], feel: "abrasive pressurized bounce", tonalities: ["minor"],
      drums: [
        "punchy clipped kick on 1 and the and-of-2, tight bone-dry snare on 3",
        "machine-gun hi-hat rolls in 64ths at the end of every 2 bars, open hat on 4-and",
        "kick pattern that drops out for a half beat before every snare, creating a sucking bounce",
      ],
      lowEnd: [
        "distorted 808 with a clipped attack and a long mono tail, tuned to the root and fifth",
        "808 retriggers in 16th-note stutters on the last beat of every 8th bar",
      ],
      leads: ["bit-crushed square-wave synth lead, hard-panned octave doubling", "overdriven synth-brass stab, dark and short", "screaming detuned supersaw lead with pitch bends through tape saturation", "cold digital pluck arpeggio in 8ths"],
      motifs: ["the square-wave motif", "the synth-brass stab motif", "the supersaw motif"],
      texture: ["dry in-your-face mix, close and airless, everything center except the lead", "a layer of white-noise hiss that opens only when the hats roll"],
      moods: ["serrated-bright", "furnace-pressed", "chrome-hostile"],
    },
    {
      id: "ny-drill", label: "NY Drill", family: "hiphop", genre: "New York drill, drill beat",
      bpm: [138, 146], feel: "sliding menacing skip", tonalities: ["minor"],
      drums: [
        "drill kick pattern with the kick on the and-of-1 and 3-and, snare on 3, rolling 16th hats",
        "hi-hats with triplet skips that alternate every bar, one open hat on the last 8th",
        "clap layered under the snare, both sitting 10 ms late for a dragging pocket",
      ],
      lowEnd: [
        "sliding 808 that glides between the root, minor third and fifth in 8th-note figures",
        "808 with a short fade-in on every slide so the bass swells into each note",
      ],
      leads: ["haunting sustained string ensemble on a 2-bar descending line", "muted electric guitar single-note melody with slapback delay", "dark bell-like synth tone, slightly detuned, two notes per bar", "eerie reed-organ pad holding a low drone"],
      motifs: ["the string-ensemble motif", "the muted-guitar motif", "the detuned-bell motif"],
      texture: ["cold wide reverb on the strings only, drums and bass bone-dry", "low-passed room noise that swells under the drum dropouts"],
      moods: ["wraith-cold", "iron-lit", "prowling-elegant"],
    },
    {
      id: "sexy-drill", label: "Sexy Drill", family: "hiphop", genre: "sexy drill, melodic drill",
      bpm: [138, 146], feel: "flirty syncopated bounce", tonalities: ["minor"],
      drums: [
        "drill kick grid softened with a clap on 3 and a Jersey-style triple kick on the last beat of every 2 bars",
        "shuffled hi-hats with swing, occasional 32nd skip, open hat on the and-of-4",
        "snare replaced by a snappy layered clap with a short plate tail",
      ],
      lowEnd: [
        "smooth sliding 808 with a rounder attack, glides between root and fourth every 2 bars",
        "808 that drops out on beat 4 of every fourth bar and returns with a slide",
      ],
      leads: ["warm chorused electric-keys chords in a Y2K R&B voicing", "plucked clean electric guitar riff with a light tremolo", "breathy synth flute countermelody", "silky string pad with a slow filter opening"],
      motifs: ["the chorused-keys motif", "the clean-guitar motif", "the synth-flute motif"],
      texture: ["glossy wide keys, tight center drums, sub in mono", "soft vinyl warmth on the keys, none on the drums"],
      moods: ["satin-sly", "neon-warm", "candlelit-cocky"],
    },
    {
      id: "detroit-flint", label: "Detroit / Flint", family: "hiphop", genre: "Detroit bounce, Flint bounce",
      bpm: [90, 105], feel: "stumbling off-grid straight time", tonalities: ["minor"],
      drums: [
        "straight-time kick that lands on 1 and the and-of-3, dry cracking snare on 2 and 4, thin and papery",
        "hi-hats in loose 8ths that lurch behind the grid, an occasional 16th stumble",
        "drums-only for the first 2 bars before the bass enters, deliberately non-quantized",
      ],
      lowEnd: [
        "melodic pitched 808 bassline playing 8th-note runs up and down a minor scale, the bass is the hook",
        "bouncing 808 melody that jumps an octave on every fourth bar and slides back down",
      ],
      leads: ["cheap-sounding bright synth pluck on a nervous 1-bar loop", "tense two-note string stab on beats 1 and 3", "grainy organ drone one octave under the bass melody", "sharp plucked harp figure that answers the bass every 2 bars"],
      motifs: ["the nervous synth-pluck motif", "the two-note string stab", "the 808 bass melody"],
      texture: ["cramped dry mono-leaning mix with a loud bass and tucked melody", "a faint tape-hiss layer that cuts to silence for half a beat before each bass run"],
      moods: ["jittery-brash", "streetlight-orange", "sneering-loose"],
    },
    {
      id: "jersey-club-rap", label: "Jersey Club Rap", family: "hiphop", genre: "Jersey club beat, club bounce",
      bpm: [130, 140], feel: "breathless kinetic bounce", tonalities: ["minor", "major"],
      drums: [
        "Jersey club kick pattern: kick on 1, 2-and, 3, 4-and with a triple-kick flourish every second bar",
        "the signature five-kick rhythmic squeak pattern replacing the snare on every fourth bar",
        "clap on 2 and 4 with rolling 16th hats and a breathy open hat on the and-of-3",
      ],
      lowEnd: [
        "short punchy 808 tuned to the kick pattern, hitting only on the kick accents",
        "808 that stutters in a 1/8-note repeat on beat 4 of every fourth bar",
      ],
      leads: ["bright plucked synth lead with a 2-bar staccato hook", "retriggered electric-keys chord stab hitting on the kick pattern", "airy string pad with a fast filter wobble", "dark synth-brass hit on the 1 of every 4 bars"],
      motifs: ["the staccato pluck motif", "the keys-stab motif", "the synth-brass hit"],
      texture: ["dry club drums in the center, wide pluck lead, sub in mono", "short room reverb on the clap only, everything else dry"],
      moods: ["breathless-metallic", "strobe-stung", "sweat-bright"],
    },
    {
      id: "plugg", label: "Plugg / PluggnB", family: "hiphop", genre: "plugg, pluggnb",
      bpm: [140, 160], feel: "airy sparse float", tonalities: ["minor", "major"],
      drums: [
        "sparse plugg drums: soft rounded kick, a thin snap snare on 3, hats in relaxed 8ths with rare triplet skips",
        "drums held back for the first 4 bars, then a single kick-and-snap pattern with wide gaps",
        "a loose swung hat pattern that occasionally drops for a whole beat",
      ],
      lowEnd: [
        "smooth sine-like 808 with a long tail, one note per chord, clean and undistorted",
        "808 that hums under the chords and slides up gently into the hook",
      ],
      leads: ["twinkling detuned synth bells, sparse, panned wide", "soft mellow electric-keys chords with a wide chorus, voicings full of ninths", "dreamy synth flute melody with long portamento", "warm airy pad that swells underneath"],
      motifs: ["the detuned-bell motif", "the mellow-keys motif", "the synth-flute motif"],
      texture: ["dreamy wide reverb on the keys and bells with the drums dry and small", "wide stereo shimmer that collapses to mono on every downbeat"],
      moods: ["powder-soft", "dawn-hazed", "pastel-numb"],
    },
    {
      id: "dark-rnb", label: "Dark R&B", family: "rnb", genre: "dark R&B, alternative R&B",
      bpm: [60, 75], feel: "slow submerged pocket", tonalities: ["minor"],
      drums: [
        "slow R&B drums with a soft thudding kick, a layered snap-clap on 3, hats in swung 16ths with occasional trap rolls",
        "kick that hits only on 1 and the and-of-2, leaving beat 4 empty",
        "a drum pattern that drops entirely for the last 2 bars of every 8",
      ],
      lowEnd: [
        "short round sub 808, one note per chord change, sitting under the pad",
        "808 that dips a whole step on the fourth chord, felt more than heard",
      ],
      leads: ["detuned analog pad with slow chorus, minor ninth chords", "clean electric guitar with tremolo and reverb plucking a slow 2-bar phrase", "low breathy synth flute countermelody", "warm tremolo electric keys, two notes at a time"],
      motifs: ["the tremolo-guitar motif", "the synth-flute motif", "the detuned-pad motif"],
      texture: ["deep dark reverb on the pad and guitar, dry tight drums, sub dead-center", "a low-passed hum under everything that opens up in the hook"],
      moods: ["velvet-scorched", "ink-warm", "bruised-silk"],
    },
    {
      id: "trap-soul", label: "Trap-Soul", family: "rnb", genre: "trap soul, R&B trap",
      bpm: [60, 72], feel: "smooth weighty half-time", tonalities: ["minor", "major"],
      drums: [
        "trap-soul drums: soft kick on 1 and the and-of-3, snappy snare on 3, double-time hats with lazy triplet rolls",
        "hats that swing late and drop out for beat 4 every second bar",
        "layered clap-snare with a short dark plate, hats slightly rushed for a nervous pocket",
      ],
      lowEnd: [
        "warm long 808 tuned to the chord roots, gliding down an octave at phrase ends",
        "808 that hits an 8th before the downbeat, pushing the whole loop forward",
      ],
      leads: ["lush chorused electric-keys chords, minor seventh and ninth voicings", "slow plucked electric guitar melody with light tremolo", "silky synth string pad", "soft glassy synth bell motif, two notes"],
      motifs: ["the chorused-keys motif", "the plucked-guitar motif", "the two-note bell motif"],
      texture: ["wide keys, deep sub, tight center drums, a warm tape haze over everything", "reverb tails that get cut short before each snare"],
      moods: ["amber-heavy", "midnight-plush", "lacquer-slow"],
    },
    {
      id: "afro-rnb", label: "Afro R&B", family: "rnb", genre: "Afro R&B, afrobeats R&B",
      bpm: [95, 110], feel: "rolling swung warmth", tonalities: ["minor", "major"],
      drums: [
        "afrobeats drum groove: soft kick on 1 and the and-of-2, snappy snare on 3, shaker in swung 16ths",
        "syncopated wood-tick percussion on the offbeats, hats in a rolling swing",
        "a kick pattern that pushes the and-of-4 into the next bar",
      ],
      lowEnd: [
        "log-drum style bass hits on the syncopated kick accents, pitched to the chords",
        "warm 808 that slides between root and sixth in a bouncing 2-bar phrase",
      ],
      leads: ["clean nylon-string guitar plucks on a bright 2-bar riff", "warm chorused electric keys with major-seventh voicings and a swung comping rhythm", "soft synth flute lead with slides and breath", "wide airy pad with gentle filter movement"],
      motifs: ["the nylon-guitar riff", "the comping-keys motif", "the synth-flute motif"],
      texture: ["wide guitar and keys, shaker panned left, drums dry and warm, sub in mono", "short spring reverb on the guitar only, keeping the drums dry"],
      moods: ["sun-lacquered", "copper-swaying", "dusk-sweet"],
    },
    {
      id: "y2k-rnb", label: "Y2K R&B", family: "rnb", genre: "Y2K R&B, 2000s R&B",
      bpm: [88, 100], feel: "swung glossy bounce", tonalities: ["minor", "major"],
      drums: [
        "early-2000s R&B drums: snappy layered snare on 2 and 4, swung 16th hats, a kick pattern that skips the 3",
        "crisp drum machine hats with a shaker layer, ghost snares on the and-of-4",
        "a tight dry kick that doubles on the and-of-1 every second bar",
      ],
      lowEnd: [
        "punchy short synth bass playing a syncopated 2-bar figure, tucked under the kick",
        "warm sub 808 following the bass figure one octave down, only on the chord roots",
      ],
      leads: ["glossy chorused electric-keys chords with suspended fourths", "clean electric guitar with a slow filter movement, single-note riff", "plucked harp glissando answering every 4 bars", "shimmering string pad with an octave lift in the hook"],
      motifs: ["the suspended-keys motif", "the filtered-guitar riff", "the harp answer"],
      texture: ["wide glossy keys, dry tight drums, bass in the center, a light polish on top", "a subtle chorus wash over the keys that widens in the hook and narrows in the verse"],
      moods: ["gloss-drunk", "chrome-tender", "rollerskate-slick"],
    },
    {
      id: "west-coast", label: "West Coast Bounce", family: "hiphop", genre: "West Coast hip hop, hyphy bounce",
      bpm: [96, 104], feel: "slapping cocky bounce", tonalities: ["minor"],
      drums: [
        "west-coast bounce drums: punchy kick on 1 and the and-of-2, big clap on 2 and 4, hats in loose 8ths",
        "a dry snare with a clap doubled slightly late, swung hats with an open hat on the and-of-3",
        "a kick that hits an extra 16th before beat 3, giving the bounce",
      ],
      lowEnd: [
        "thick synth bass doubling a slapping 808, playing a 2-bar syncopated figure",
        "808 that drops out on beat 3 of every fourth bar and slams back on 4",
      ],
      leads: ["high sine-wave synth lead with pitch bends on a 2-bar whistling hook", "sharp synth-brass stab on the offbeats, dry and short", "dark synth string stab on the 1 of every bar", "buzzy square-wave pluck counterline"],
      motifs: ["the whistling sine motif", "the synth-brass stab", "the square-wave counterline"],
      texture: ["dry loud center-heavy mix with the lead panned slightly right", "tight room on the clap only, bass and kick bone-dry"],
      moods: ["asphalt-hot", "lowrider-slow", "swagger-glossy"],
    },
  ];

  // Leads any world can borrow, so the melody voice keeps changing.
  const GLOBAL_LEADS = [
    "detuned analog saw lead with slow vibrato", "plucked nylon-string guitar loop", "airy synth flute lead with breath noise",
    "bit-crushed square-wave synth lead", "overdriven synth-brass stab", "cold digital pluck arpeggio in 8ths",
    "haunting sustained string ensemble", "muted electric guitar single-note melody with slapback delay", "dark detuned bell-like synth tone",
    "plucked clean electric guitar riff with light tremolo", "breathy synth flute countermelody", "sharp plucked harp figure",
    "grainy organ drone", "bright plucked synth lead in staccato 8ths", "screaming detuned supersaw lead with pitch bends",
    "high sine-wave synth lead with pitch bends", "clean electric guitar with tremolo and reverb", "warm chorused electric keys with ninth voicings",
    "twinkling detuned synth bells", "buzzy square-wave pluck", "low bowed cello-like synth", "glassy FM keys pad",
    "resampled reverse pad melody", "tape-warped analog pad lead", "hard-plucked FM bass-synth melody", "muted felt piano, two notes at a time",
  ];
  const COUNTER_VOICES = [
    "string pad", "synth flute", "plucked harp", "nylon-string guitar", "chorused electric keys", "reed-organ drone",
    "airy pad", "synth-brass stab", "bell-like synth", "tremolo guitar", "square-wave pluck", "sine-wave lead",
    "bowed string line", "bit-crushed pluck", "detuned pad", "low organ", "muted guitar", "harp glissando",
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
    "purely instrumental beat with the lead melody sitting where a voice would",
    "instrumental only, every hook carried by the lead melody and the 808",
    "wordless instrumental production, melody-led, built for radio",
  ];

  const SYNCOPATION = [
    "kick on the and-of-3 instead of 3, snare on 3 dead on the grid",
    "808 pattern that anticipates the downbeat by an 8th note",
    "clap displaced to the and-of-2 every fourth bar",
    "hats in a 3-3-2 pattern across each bar",
    "melody accents landing on the offbeat 16ths, never on the beat",
    "bass that rests on 1 and enters on the and-of-1",
    "cross-rhythm between the 808 and kick, kick in duple, 808 in triplets",
    "a syncopated pluck figure that pushes into the next bar",
    "snare on 3 with a ghost snare on the and-of-4",
    "kick pattern that leaves beat 4 completely empty every bar",
    "open hat on the 16th before every snare",
    "808 slides that start on the and-of-4 and resolve on the following 1",
  ];
  const HUMAN_FEEL = [
    "non-quantized drums with a late snare and slightly rushed hats",
    "hats played with loose human timing, drifting off the grid a little each bar",
    "the 808 lands 20 ms behind the kick for a dragging pocket",
    "swing at about 60 percent on the hats, kick straight",
    "snare drifting late by a few milliseconds more each bar, then resetting",
    "a lazy behind-the-beat pocket where the drums lean back against the melody",
    "drums that sound hand-played on pads, uneven velocities, every hat a little different",
    "a slightly stumbling kick pattern that never fully locks to the grid",
    "loose off-grid bass runs that rush ahead of the drums",
    "hat velocities that breathe, soft on the offbeats, hard on the rolls",
    "a melody loop that is a hair out of time with the drums, deliberately unaligned",
    "human shuffle where the second 8th of each beat is pushed late",
  ];
  // Glitch as rhythm: stutters, silences, retriggers. Never as an effect.
  const GLITCH = [
    "the 808 stutters in a 32nd-note repeat on the last beat of every 8th bar",
    "a half-beat drop of total silence before every fourth snare",
    "retriggered snare in 16ths across beat 4 every 4 bars",
    "bit-crushed synth stab that repeats three times in a fast triplet then cuts",
    "the hi-hat pattern stalls and repeats one 16th for half a bar",
    "a stuttered kick that doubles into a 64th-note flam on beat 1 of the hook",
    "the melody retriggers from its first note on the and-of-4 every second bar",
    "a full-band mute for one 8th note right before the downbeat of every 4 bars",
    "808 pitch jumps up an octave for a single 16th and drops back",
    "the clap repeats in a decelerating stutter over the last beat of the phrase",
    "a bit-crushed hat burst that replaces the snare once every 8 bars",
    "the loop skips a beat, jumping from beat 3 straight to the next bar's 1",
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
  const MIX = [
    "radio-ready master, loud and clean, around -9 LUFS integrated, true peak -1 dB",
    "tight controlled low end, 808 saturated so it reads on phone speakers, sub in mono below 120 Hz",
    "dry punchy center-focused drums with wide pads and a mono 808",
    "clean headroom, the 808 kept just under clipping, kick and 808 sidechained so they never smear",
    "polished modern hip hop mix, mid-forward, slightly dark top end, smooth highs",
    "the lead melody sits in the midrange where a voice would, with nothing competing there",
    "stereo width on pads and plucks, everything below 100 Hz dead-center",
    "clear separation between kick and 808, kick clicks on top and the 808 owns the sub",
    "glued, professionally mastered, competitive loudness without pumping",
    "hi-hats crisp and forward but not brittle, snare snappy with a short dark tail",
    "wide but mono-compatible, phase-clean low end",
    "instrumental mix with the melody balanced as the lead, drums loud, bass felt not heard",
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
      model: "Suno v6", weirdness: "15–25%", styleInfluence: "70–80%", variety: "Off (0)", maxMode: "On",
      note: "Safe radio instrumental. v6 follows a dense style field most faithfully; low Weirdness keeps the drums and 808 behaving like a record; Variety Off so these exact phrases are not rewritten; Max Mode holds the arrangement together through the beat switch. Turn Suno's Instrumental toggle on.",
    },
    wild: {
      model: "Suno v6-wild first, then re-run the keeper on v6", weirdness: "35–50%", styleInfluence: "60–70%", variety: "Off (0), or one notch if takes get samey", maxMode: "On for the v6 re-run",
      note: "Experimental-but-radio. Wild invents textures and rhythm edits the flagship will not; keep the keeper's seed and re-run it on v6 with Max Mode for the clean radio finish. Turn Suno's Instrumental toggle on.",
    },
  };

  const LINT = [
    { re: /\b(vocal|vocals|vocalist|rap\b|rapper|sing\w*|sung|chant\w*|choir|hum|humming|hummed|lyric\w*|ad-?lib\w*)\b/i, level: "block", msg: "vocal vocabulary invites a voice" },
    { re: /\b(transition\w*|sweeps?|risers?|phaser|dj\b|scratch\w*|fills?|cowbell|rimshot|airhorn)\b/i, level: "block", msg: "effect vocabulary turns into DJ tricks and fills" },
    { re: /\b(marimba|kalimba|music box|glockenspiel|celesta|xylophone|toy piano|dulcimer)\b/i, level: "block", msg: "stock mallet preset" },
    { re: /\b(edm|dubstep|jazz\w*|funk\w*|disco|house\b)\b/i, level: "block", msg: "off-format genre word" },
    { re: /\bno\s+\w+/i, level: "warn", msg: "\"no X\" inside a prompt plants X" },
  ];

  return {
    STYLE_LIMIT, LYRICS_LIMIT, PIANO_SHARE, MINOR_KEYS, MAJOR_KEYS, PROGRESSIONS, WORLDS, GLOBAL_LEADS, COUNTER_VOICES,
    ADJECTIVES, INSTRUMENTAL_LOCK, SYNCOPATION, HUMAN_FEEL, GLITCH, BEAT_SWITCH, SIGNATURES, TECH_FLEX, MIX, STRUCTURES,
    EXCLUDE, SETTINGS, LINT,
  };
})();

if (typeof module !== "undefined") module.exports = AnomalyData;
