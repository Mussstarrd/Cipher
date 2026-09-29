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
      drums: ["sparse kick on the and-of-3, late layered clap", "triplet hat rolls every fourth bar, open hat offbeat", "half-time grid with rushed hats and a late snare"],
      lowEnd: ["long tuned 808 answering the kick, not doubling it", "808 sliding up a fifth at every phrase end"],
      leads: ["warm low nylon-guitar riff", "low detuned analog lead with slow vibrato", "dark mid-register brass-style riff", "low woodwind-style synth riff"],
      motifs: ["the saw-lead motif", "the palm-muted guitar motif", "the synth-flute motif"],
      texture: ["wide pad, dry center drums, mono sub", "faint vinyl crackle under the melody"],
      moods: ["glacial-tender", "pewter-lit", "sedated-luxe"],
    },
    {
      id: "hard-trap", label: "Hard Trap", family: "hiphop", genre: "hard trap, aggressive hip hop",
      bpm: [140, 160], feel: "abrasive pressurized bounce", tonalities: ["minor"],
      drums: ["clipped kick on 1 and the and-of-2, bone-dry snare on 3", "machine-gun hat rolls every 2 bars", "kick drops out for a half beat before every snare"],
      lowEnd: ["distorted 808 with a clipped attack and a long mono tail", "808 stuttering in 16ths on the last beat of every 8th bar"],
      leads: ["growling low synth-brass riff", "overdriven low brass stab riff", "screaming mid-register detuned lead through tape", "distorted low guitar riff"],
      motifs: ["the square-wave motif", "the synth-brass stab motif", "the supersaw motif"],
      texture: ["dry in-your-face mix, close and airless", "white-noise hiss that opens only on the hat rolls"],
      moods: ["serrated-bright", "furnace-pressed", "chrome-hostile"],
    },
    {
      id: "ny-drill", label: "NY Drill", family: "hiphop", genre: "New York drill, drill beat",
      bpm: [138, 146], feel: "sliding menacing skip", tonalities: ["minor"],
      drums: ["drill kick on the and-of-1 and 3-and, snare on 3, rolling hats", "triplet hat skips alternating every bar", "clap under the snare, both dragging late"],
      lowEnd: ["sliding 808 gliding root, minor third, fifth in 8ths", "808 swelling into every slide"],
      leads: ["haunting low string-ensemble line", "muted low electric-guitar riff with slapback", "low pitched-down bell riff", "eerie reed-organ drone riff"],
      motifs: ["the string-ensemble motif", "the muted-guitar motif", "the detuned-bell motif"],
      texture: ["cold wide reverb on strings only, drums bone-dry", "low-passed room noise swelling under drum dropouts"],
      moods: ["wraith-cold", "iron-lit", "prowling-elegant"],
    },
    {
      id: "sexy-drill", label: "Sexy Drill", family: "hiphop", genre: "sexy drill, melodic drill",
      bpm: [138, 146], feel: "flirty syncopated bounce", tonalities: ["minor"],
      drums: ["drill grid softened with a clap on 3 and a triple kick every 2 bars", "shuffled swung hats with 32nd skips", "snappy layered clap with a short plate tail instead of a snare"],
      lowEnd: ["smooth sliding 808 gliding root to fourth every 2 bars", "808 dropping out on beat 4 of every fourth bar, back with a slide"],
      leads: ["warm chorused electric-keys riff in a Y2K voicing", "mid-register clean tremolo-guitar riff", "breathy low flute-style synth riff", "silky mid-register string riff"],
      motifs: ["the chorused-keys motif", "the clean-guitar motif", "the synth-flute motif"],
      texture: ["glossy wide keys, tight center drums, mono sub", "soft vinyl warmth on the keys only"],
      moods: ["satin-sly", "neon-warm", "candlelit-cocky"],
    },
    {
      id: "detroit-flint", label: "Detroit / Flint", family: "hiphop", genre: "Detroit bounce, Flint bounce",
      bpm: [90, 105], feel: "stumbling off-grid straight time", tonalities: ["minor"],
      drums: ["straight-time kick on 1 and the and-of-3, dry cracking snare on 2 and 4", "loose 8th hats lurching behind the grid", "drums alone for 2 bars before the bass, deliberately unquantized"],
      lowEnd: ["melodic pitched 808 bassline running up and down the minor scale, the bass is the hook", "bouncing 808 melody jumping an octave every fourth bar"],
      leads: ["cheap nervous mid-register synth riff", "tense two-note low string stab", "grainy organ drone riff under the bass", "sharp mid-register harp figure answering the bass"],
      motifs: ["the nervous synth-pluck motif", "the two-note string stab", "the 808 bass melody"],
      texture: ["cramped dry mono-leaning mix, loud bass, tucked melody", "tape hiss cutting to silence before each bass run"],
      moods: ["jittery-brash", "streetlight-orange", "sneering-loose"],
    },
    {
      id: "jersey-club-rap", label: "Jersey Club Rap", family: "hiphop", genre: "Jersey club beat, club bounce",
      bpm: [130, 140], feel: "breathless kinetic bounce", tonalities: ["minor", "major"],
      drums: ["Jersey club kick on 1, 2-and, 3, 4-and with a triple-kick every second bar", "five-kick squeak pattern replacing the snare every fourth bar", "clap on 2 and 4, rolling hats, breathy open hat"],
      lowEnd: ["short punchy 808 hitting only on the kick accents", "808 stuttering in 8ths on beat 4 of every fourth bar"],
      leads: ["staccato mid-register horn-stab riff", "retriggered electric-keys stab riff on the kick pattern", "dark low synth-brass hit riff", "mid-register string-stab riff with a fast filter wobble"],
      motifs: ["the staccato pluck motif", "the keys-stab motif", "the synth-brass hit"],
      texture: ["dry club drums center, wide melody, mono sub", "short room on the clap only"],
      moods: ["breathless-metallic", "strobe-stung", "sweat-bright"],
    },
    {
      id: "plugg", label: "Plugg / PluggnB", family: "hiphop", genre: "plugg, pluggnb",
      bpm: [140, 160], feel: "airy sparse float", tonalities: ["minor", "major"],
      drums: ["soft rounded kick, thin snap on 3, relaxed 8th hats", "drums held back 4 bars then a bare kick-and-snap", "loose swung hats that drop for a whole beat"],
      lowEnd: ["smooth sine 808 with a long tail, one note per chord", "808 droning under the chords, sliding up into the hook"],
      leads: ["mellow low electric-keys riff full of ninths", "dreamy mid-register flute-style synth riff with portamento", "low pitched-down bell riff", "warm low pad riff"],
      motifs: ["the detuned-bell motif", "the mellow-keys motif", "the synth-flute motif"],
      texture: ["dreamy wide reverb on keys, drums dry and small", "stereo shimmer collapsing to mono on every downbeat"],
      moods: ["powder-soft", "dawn-hazed", "pastel-numb"],
    },
    {
      id: "dark-rnb", label: "Dark R&B", family: "rnb", genre: "dark R&B, alternative R&B",
      bpm: [60, 75], feel: "slow submerged pocket", tonalities: ["minor"],
      drums: ["soft thudding kick, layered snap-clap on 3, swung 16th hats", "kick only on 1 and the and-of-2, beat 4 empty", "drums dropping out for the last 2 bars of every 8"],
      lowEnd: ["short round sub 808, one note per chord", "808 dipping a whole step on the fourth chord"],
      leads: ["detuned low analog pad riff in minor ninths", "slow low tremolo-guitar riff", "low breathy flute-style synth riff", "warm low tremolo electric-keys riff"],
      motifs: ["the tremolo-guitar motif", "the synth-flute motif", "the detuned-pad motif"],
      texture: ["deep dark reverb on pad and guitar, dry tight drums", "low-passed drone under everything opening on the hook"],
      moods: ["velvet-scorched", "ink-warm", "bruised-silk"],
    },
    {
      id: "trap-soul", label: "Trap-Soul", family: "rnb", genre: "trap soul, R&B trap",
      bpm: [60, 72], feel: "smooth weighty half-time", tonalities: ["minor", "major"],
      drums: ["soft kick on 1 and the and-of-3, snappy snare on 3, double-time hats with lazy rolls", "hats swinging late and dropping for beat 4 every second bar", "layered clap-snare with a dark plate, rushed hats"],
      lowEnd: ["warm long 808 on the chord roots, gliding down an octave at phrase ends", "808 hitting an 8th before the downbeat, pushing the loop forward"],
      leads: ["lush low chorused electric-keys riff", "slow mid-register plucked-guitar riff", "silky low string riff", "low pitched-down bell riff, two notes"],
      motifs: ["the chorused-keys motif", "the plucked-guitar motif", "the two-note bell motif"],
      texture: ["wide keys, deep sub, tight drums, warm tape haze", "reverb tails cut short before each snare"],
      moods: ["amber-heavy", "midnight-plush", "lacquer-slow"],
    },
    {
      id: "afro-rnb", label: "Afro R&B", family: "rnb", genre: "Afro R&B, afrobeats R&B",
      bpm: [95, 110], feel: "rolling swung warmth", tonalities: ["minor", "major"],
      drums: ["soft kick on 1 and the and-of-2, snappy snare on 3, shaker in swung 16ths", "wood-tick percussion on the offbeats, rolling swung hats", "kick pushing the and-of-4 into the next bar"],
      lowEnd: ["log-drum bass hits on the kick accents, pitched to the chords", "warm 808 sliding root to sixth in a bouncing 2-bar phrase"],
      leads: ["sunny mid-register nylon-guitar riff", "warm chorused electric-keys comping riff", "low flute-style synth riff with slides", "low airy pad riff"],
      motifs: ["the nylon-guitar riff", "the comping-keys motif", "the synth-flute motif"],
      texture: ["wide guitar and keys, shaker left, drums dry and warm", "spring reverb on the guitar only"],
      moods: ["sun-lacquered", "copper-swaying", "dusk-sweet"],
    },
    {
      id: "y2k-rnb", label: "Y2K R&B", family: "rnb", genre: "Y2K R&B, 2000s R&B",
      bpm: [88, 100], feel: "swung glossy bounce", tonalities: ["minor", "major"],
      drums: ["snappy layered snare on 2 and 4, swung 16th hats, kick skipping the 3", "crisp drum-machine hats with a shaker, ghost snares on the and-of-4", "tight dry kick doubling on the and-of-1 every second bar"],
      lowEnd: ["punchy short synth bass on a syncopated 2-bar figure", "warm sub 808 following the bass an octave down"],
      leads: ["glossy chorused electric-keys riff with suspended fourths", "mid-register filtered clean-guitar riff", "mid-register harp glissando riff", "shimmering mid-register string riff"],
      motifs: ["the suspended-keys motif", "the filtered-guitar riff", "the harp answer"],
      texture: ["wide glossy keys, dry tight drums, bass center", "chorus wash widening in the hook"],
      moods: ["gloss-drunk", "chrome-tender", "rollerskate-slick"],
    },
    {
      id: "west-coast", label: "West Coast Bounce", family: "hiphop", genre: "West Coast hip hop, hyphy bounce",
      bpm: [96, 104], feel: "slapping cocky bounce", tonalities: ["minor"],
      drums: ["punchy kick on 1 and the and-of-2, big clap on 2 and 4, loose 8th hats", "dry snare with a late doubled clap, swung hats", "kick hitting an extra 16th before beat 3"],
      lowEnd: ["thick synth bass doubling a slapping 808 on a 2-bar syncopated figure", "808 dropping on beat 3 of every fourth bar and slamming back on 4"],
      leads: ["mid-register whistling sine riff with pitch bends", "sharp low synth-brass stab riff", "dark low string-stab riff", "buzzy mid-register square-wave riff"],
      motifs: ["the whistling sine motif", "the synth-brass stab", "the square-wave counterline"],
      texture: ["dry loud center-heavy mix, lead slightly right", "tight room on the clap only"],
      moods: ["asphalt-hot", "lowrider-slow", "swagger-glossy"],
    },
  ];


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
    "low fuzz-bass riff", "grainy tape-worn lead riff", "low choir-like pad riff", "muted felt piano riff",
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
  ];

  return {
    STYLE_LIMIT, LYRICS_LIMIT, PIANO_SHARE, MINOR_KEYS, MAJOR_KEYS, PROGRESSIONS, WORLDS, GLOBAL_LEADS,
    ADJECTIVES, INSTRUMENTAL_LOCK, SWING, SYNC, CLASH, GLITCH, BEAT_SWITCH, SIGNATURES, TECH_FLEX, MIX, STRUCTURES,
    SYNCOPATION: SYNC, HUMAN_FEEL: SWING,
    EXCLUDE, SETTINGS, LINT,
  };
})();

if (typeof module !== "undefined") module.exports = AnomalyData;
