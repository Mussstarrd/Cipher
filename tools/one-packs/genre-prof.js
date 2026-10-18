// Prof button: two moods in one pack. "lowtide" = the slow heavy late-night
// crawl (Bar Breaker 88, Ain't We Rich 73, A Crawl Through a Low Tide); "goodtime"
// = the rowdy bar-fight party record (Gampo ~95, Action 127, Apeshit 139, Kia
// Boy, Dynamite). Every phrase was run through OneGuard.check() (see
// test-prof.js). No artist or song names appear outside `names`.
module.exports = {
  id: "prof",
  title: "Prof button: low tide crawl / good time boy",
  identity: "Minneapolis bar-room hip hop with two faces. Low tide: a crawl at 68-92, a drunk out-of-tune piano or a dark wheezing organ over a muddy sliding 808 whose tail drags into the next bar, the pocket slurring into triplets on beat 3, sour flat thirds, everything a hair behind the beat like the last hour of a bar. Good time boy: a stomp at 94-142, kick on every beat, gang claps and boot stomps, a huge distorted saw bass, a high chirping sine hook or an air-raid siren synth on top, hot horn stabs with the snare, cartoon bravado, bar-fight energy. Both faces share a melodic lead that takes the hook slot, heavy low end, and a human played-in feel.",
  anchors: [
    "Minneapolis bar-fight hip hop, heavy bass",
    "Minnesota party hip hop, stomping drums, piercing sine hook",
    "Midwest bar-room hip hop, drunk piano, long 808 tails",
    "Minneapolis late-night crawl hip hop, muddy sliding bass",
    "rowdy Midwest hip hop, horn stabs, huge saw bass",
    "Twin Cities hip hop, dark organ keys, slurred triplet bounce"
  ],
  bpm: ["68-80 low tide crawl", "84-96 bar-room stomp", "126-142 double-time bar-fight"],
  families: {
    drunk_piano: {
      src: [
        "a bar-room piano with sticky keys, played heavy and a little behind|the drunk piano",
        "a dark low piano, two-handed minor chords struck hard and left to ring|the low piano",
        "a detuned out-of-tune piano, the pitch sagging on every held chord|the sagging piano",
        "a felt-dampened piano close-miked, every hammer thud audible|the felt piano",
        "a piano sample pitched down a fourth until it drags, grainy|the dragged piano",
        "a clanging piano hammering sour flat thirds with the sustain pedal down|the sour piano",
        "a toy piano with plastic clanky hammers, doubled an octave down by a real piano|the toy piano",
        "a reversed piano chord resampled into a one-shot that swells into each downbeat|the reversed piano"
      ],
      ch: [
        "slurring in and out of tune like it had a few drinks",
        "keys clacking and the pedal thumping under the notes",
        "low-passed so it sounds like it is coming through the wall of the bar next door",
        "pushed through a cranked tube preamp until the low notes snarl",
        "the pitch sagging on every held chord",
        "one take, the wrong notes left in",
        "the top end rolled off until only the body speaks"
      ]
    },
    dark_organ: {
      src: [
        "a cheap combo organ with a wavering vibrato, dark and low|the combo organ",
        "a wheezing reed organ running out of air on the long chords|the reed organ",
        "a drawbar organ with all the bright bars pushed in, only the low ones out|the dark drawbar organ",
        "a drunken detuned organ, the pitch sagging and swaying|the drunken organ",
        "a tonewheel organ with the percussion click on, stabbing short dark chords|the tonewheel organ",
        "a broken organ with one dead key, buzzing on the held notes|the broken organ",
        "an organ sample pitched down an octave so it drones like a foghorn|the foghorn organ",
        "a toy keyboard organ patch run through a guitar amp until it barks|the amp organ"
      ],
      ch: [
        "the pitch sagging on every held chord",
        "overdriven until the low notes snarl",
        "wheezing and losing air at the end of each phrase",
        "low-passed so it sits behind a wall",
        "the vibrato speeding up as the chord dies",
        "keys clacking audibly under the notes",
        "sample-rate reduced to a grainy edge"
      ]
    },
    siren_synth: {
      src: [
        "a high chirping sine hook, two notes that pierce through everything|the chirping sine hook",
        "a shrill tea-kettle sine lead that pierces like a referee's signal|the tea-kettle lead",
        "an air-raid siren synth that climbs a fifth and holds|the air-raid siren",
        "a fat synth brass stab patch, three short hits per bar|the synth horn stabs",
        "a thick nasal saw lead honking the riff like a cartoon car horn|the honking saw lead",
        "a rubber-band mono synth that boings on every glide|the rubber-band synth",
        "a squealing pitch-bent mono lead like a car alarm|the car-alarm lead",
        "a cheap trumpet-style synth preset playing a two-note taunt|the taunt lead"
      ],
      ch: [
        "hard-clipped on every transient",
        "doubled an octave up so it cuts through a loud bar",
        "detuned a few cents against itself",
        "gated tight so every note cuts off clean",
        "bitcrushed just enough to grit the attacks",
        "pitched up a step for the last hook",
        "panned hard left and right, a copy on each side"
      ]
    },
    stomp_brass: {
      src: [
        "a stacked trombone and trumpet section, blown hot, stabbing with the snare|the horn section",
        "a sliding tuba line doubling the bass|the tuba",
        "a bass trombone sliding down into each note|the bass trombone",
        "a muted electric guitar through a fuzz pedal, chugging the root|the fuzz guitar",
        "a dark baritone guitar riff, picked low and close|the baritone guitar",
        "a trumpet section pitched down a whole step so it sounds enormous|the pitched-down trumpets",
        "a distorted bass guitar played up high as the lead|the distorted bass guitar",
        "a lone cracked trumpet, raspy, two notes then silence|the lone trumpet"
      ],
      ch: [
        "blown so hard the brass rasps and buzzes",
        "fat and compressed into one wall of sound",
        "distorted like it is coming out of a car trunk",
        "a little out of tune between players, so it sounds real",
        "tight and dry, the room tail cut off",
        "overdriven through a small amp",
        "doubled by a distorted 808 underneath"
      ]
    }
  },
  bass: [
    "a sliding 808 with a long tail that drags into the next bar",
    "a huge saw-wave synth bass, distorted and wide",
    "a mono synth bass that slurs between notes like it is wading through mud",
    "an 808 with a hard knock on top, tuned to the piano",
    "a distorted 808 and a saw sub layered, the saw on top",
    "a pitched-down kick used as bass, long and muddy"
  ],
  btone: [
    "glide between every note, slow and seasick",
    "long tails that ring under the snare and into the next bar",
    "hard-clipped grit so it shows up on a bar jukebox",
    "tube saturation so it shows up on phone speakers",
    "a slow pitch fall at the end of every note",
    "a click on the attack, the body round and heavy"
  ],
  kick: [
    "a deep thud kick with a long muddy tail",
    "a stomping kick on every beat, hard-clipped",
    "a dry knocking kick that lands slightly behind",
    "a layered kick, sub plus a boot stomp on a wooden floor",
    "a kick that only hits twice per bar, heavy",
    "a hard punchy kick with a click on top, pushed ahead of the grid"
  ],
  snare: [
    "a gang clap stacked with a snare, a hair late",
    "a loose hand clap stacked three times, slightly out of sync",
    "a fat sampled snare with the wires rattling",
    "a half-time snare on 3 only, cracking",
    "a snare and boot stomp hit together on 2 and 4",
    "a crisp dry snare with a short room"
  ],
  hats: [
    "hats slurring into threes on beat 3, then straightening",
    "sparse 8th hats with one open hat per bar",
    "trap hats rolling in 16ths then stopping dead",
    "hand-played hats, every hit a different velocity",
    "a shaker in place of hats, dragging",
    "tight closed hats in uneven 16ths, pushed ahead"
  ],
  color: [
    "a tambourine smacked on 2 and 4, never quite even",
    "boot stomps on a wooden floor under the kick, a little late",
    "a dry woodblock knocking the off-beats",
    "finger snaps landing a hair late",
    "glass bottles knocked with a stick, tuned to the key",
    "a second clap layer slapped by hand, slightly early"
  ],
  sync: [
    "the 808 slurs into a triplet on beat 3",
    "kick on every beat, the clap stomping on 2 and 4",
    "a half-time snare under lazy 8th hats",
    "the kick avoids beat 1 every other bar",
    "the 808 anticipates the downbeat by a 16th",
    "a full stop on beat 4 every 8 bars"
  ],
  human: [
    "a little drunk on the timing but always landing the one",
    "the whole loop sitting back on the beat",
    "the clap a hair late, every time",
    "piano and bass drifting apart, then finding each other",
    "played in one take, the small mistakes left in",
    "hats rushing slightly ahead, kick dragging behind"
  ],
  structure: [
    "[Intro | the HANDLE alone, dark, 4 bars]",
    "[Verse | heavy kick, sliding 808, the HANDLE low and behind the beat]",
    "[Hook | full beat, the HANDLE up front, stomping claps, the bass huge]",
    "[Verse 2 | the HANDLE thinned to one chord per bar, 808 tails longer]",
    "[Hook | full beat, the HANDLE up front, hit harder]",
    "[Break | drums cut, the HANDLE and 808 only, slurring]",
    "[Beat Switch | halfway through, the drums go half-time, the tempo halves into a crawl, long 808 tails, the HANDLE slow and slurred]",
    "[Hook | half-time, the HANDLE slurred, bass dragging]",
    "[Outro | half-time, the HANDLE alone, ends on one sour held chord]",
    "[End]"
  ],
  motif: [
    "a four-note minor descent, last note bent flat and sour",
    "a two-note taunt a minor third apart, repeated until it is a dare",
    "a three-note cell: root, flat third, root, dragged behind the beat",
    "a six-note phrase circling the fifth, never resolving",
    "a stubborn root note leaning on the flat sixth",
    "a five-note figure that climbs a fourth and falls an octave onto the one"
  ],
  artic: [
    "stabs that only answer the snare",
    "one chord per bar, struck heavy, left to ring",
    "two long notes then a triplet stab",
    "clipped 8th stabs on every beat, stomping with the kick",
    "legato runs that slur into each other, a hair late",
    "a three-note pickup that lands on beat 1 and stops"
  ],
  sliders: "Weirdness 45-60, Style Influence 65-78, model v6; low tide loops want Max Mode On (long tails drift), good time boy rolls want Variety Off and the lower Weirdness end.",
  exclude: "blues, blues rock, country, jazz, funk, EDM drop, whistling, circus, carnival, polka, lo-fi, vocals, singing, crowd noise, DJ scratching, sea shanty",
  ban: "circus, carnival, clown\\w*, honky-tonk, honky tonk, saloon, polka, oompah, pirate\\w*, sea shanty, shanty, nautical, sailor\\w*, surf, country, blues\\w*, swamp\\w*, whistl\\w*, airhorn, air horn, kazoo, novelty, comedy, comedic, goofy, bar band, pub rock",
  names: "prof, gampo, king gampo, pookie baby, stophouse, rhymesayers, dillon parker, mike campbell, jacob anderson, jake anderson, ghosttown, frankie teardrop, kyle c-e, brian eisner, nox, dj fundo, fundo, rahzwell, beatchefs, mux mool, willie wonka, insightful, 84 caprice, aesop rock, curtiss king, big chocolate, dj two stacks, chickenbone, markmadethis, noearth, cory grindberg, jordy bangs, charley cooks, chris karns, dj vajra, willie clayton, dj absolute, atmosphere, brother ali, powderhorn suites, powderhorn, kaiser von powderhorn, recession music, project gampo, fifth element, vanguard pow, liability, good time boy, a crawl through a low tide, bar breaker, time bomb, apeshit, bow-legged, bow legged, send me up, borderline, say no, ahoy, scoop, animal patrol, squad goals, flower boy, kia boy, jewelry duty, reaching for fire, garden of eden, big dog, brrrr, pack a lunch, devils gate, subpar, soupy, tough boy, creek boy, high priced shoes, waka flocka, e-40, t-pain, 2 chainz, that mexican ot, sauce walka, pete & bas, jonray, redman, method man, kevin gates, cozz, mac irv, cashinova, muja messiah, taylor j. Never add bare ant, horse, animal, dope, dynamite, imposter, fighter or action: they match ordinary words.",
  allow: "",
  allowWhy: "Nothing allowed. The three banned words the moods tempt (blues, swamp, whistle) all have substitutes that passed: bluesy -> sour flat thirds, bent flat, bar-room piano, drunk piano; swampy -> muddy, wading through mud, long tail that drags, low tide crawl; whistle -> high chirping sine hook, tea-kettle sine lead, referee's signal, air-raid siren synth. The guard learned those three from real takes (blues pulled 12-bar jams, whistle pulled film-score flutes), so the pack keeps them banned and even adds circus, carnival, pirate and honky-tonk to its own ban list, since critics describe this catalog in those words and Suno would take them literally.",
  lanes: [
    {
      id: "lowtide", label: "Low tide crawl",
      anchors: [
        "Minneapolis late-night crawl hip hop, muddy sliding bass",
        "Midwest bar-room hip hop, drunk piano, long 808 tails",
        "slow heavy Minnesota hip hop, dark organ keys, slurred bounce",
        "low-tide hip hop crawl, sour bent minor hook, half-time drums"
      ],
      families: { drunk_piano: 4, dark_organ: 3, stomp_brass: 1 },
      tempo: [[68, 80], [82, 92]],
      modes: [["switch", 30], ["loop", 40], ["straight", 30]]
    },
    {
      id: "goodtime", label: "Good time boy",
      anchors: [
        "Minneapolis bar-fight hip hop, heavy bass",
        "rowdy Midwest party hip hop, stomping drums, piercing sine hook",
        "trap-rock hip hop, huge saw bass, gang-stomp claps",
        "Twin Cities party hip hop, horn stabs, siren synth hook"
      ],
      families: { siren_synth: 4, stomp_brass: 3, drunk_piano: 1 },
      tempo: [[94, 106], [126, 142]],
      modes: [["switch", 35], ["loop", 15], ["straight", 50]]
    }
  ]
};
