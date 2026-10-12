/* ONE OF ONE — genre packs. Each pack overrides whole sections of the base
 * grammar (src/one/grammar.js). The base is the syncopated experimental
 * hip hop engine; the spin-offs carry their own instruments, drums, groove,
 * structure, sliders and exclusions. Generated from the producer advisory
 * (scratchpad/genres-data.js) and edited by hand. */
"use strict";

const OneGenres = (() => {
  const Base = typeof OneGrammar !== "undefined" ? OneGrammar : require("./grammar.js");
  const BASE_EXCLUDE = Base.EXCLUDE;
  const j = (lines) => lines.join("\n\n");

  return {
  hiphop: {
    id: "hiphop",
    label: "Syncopated hip hop",
    blurb: "Twitchy, minimal, one-of-one instrumentals. Beat switch, hypnotic loop, or one locked pocket.",
    grammar: {},
  },
  pop: {
    id: "pop",
    label: "R&B radio pop",
    blurb: "Glossy radio pop R&B with a hole in the mids for a guest verse. Verse, pre-hook, hook.",
    grammar: {
      TEMPO: [[88,100],[118,128],[136,150]],
      MAJOR_SHARE: 0.6,
      MIX_TAG: "glossy radio-ready mix, wide and clean",
      MODES: [["straight",100]],
      SLUR_SHARE: 0,
      LOW_SHARE: 0.4,
      SLIDERS: {"straight":{"weirdness":[30,45],"styleInfluence":[70,85]},"switch":{"weirdness":[30,45],"styleInfluence":[70,85]},"loop":{"weirdness":[30,45],"styleInfluence":[70,85]}},
      EXCLUDE: BASE_EXCLUDE.concat(["tropical house","reggaeton","dancehall","moombahton","EDM drop","future bass","steel drums","marimba","saxophone","piano ballad","acoustic ballad","dream pop","synth-pop"]),
      LANES: [{"id":"pop","label":"R&B radio pop","weight":1,"anchors":["radio pop R&B, trap-soul","glossy pop R&B, tropical-tinged","trap-soul R&B, radio pop","tropical pop R&B, modern and glossy","pop R&B, late-night trap-soul","radio-ready R&B pop, sparse and wide"],"families":{"glossy_keys":3,"tropical_plucks":3,"trap-soul_guitar":3},"bass":[]}],
      FAMILIES: {"glossy_keys":{"frame":"over a glossy pop R&B beat","sources":["a soft felt-dampened piano, close-miked, every hammer audible|the felt piano","a glassy digital keyboard with a slow tremolo|the glassy keys","a tine keyboard with a soft bark on the attack, lightly chorused|the tine keys","a breathy sustained synth bed, filtered dark, under everything|the synth bed","a pitched-up piano sample, sliced to one chord per beat|the sliced piano","a hollow sine lead that opens like a vowel on each note|the vowel lead","a detuned pluck synth with a quick pitch dip on each note|the dip pluck"],"character":["glossed with a wide short plate","chorused just enough to shimmer","soft-clipped so the attacks pop on earbuds","low-passed under the hook, opened up in the verse","pitched down a whole step, thick and slow","ducking a hair under every kick","compressed flat and glued to the drums"]},"tropical_plucks":{"frame":"over a glossy pop R&B beat","sources":["a rubbery pluck synth with a fast decay, bright and bouncing|the rubber pluck","a mallet-struck wooden-bar patch, hollow, the mallet knock left in|the wooden mallets","a pitched-down flute sample playing one short phrase|the pitched flute","a tongue drum sample, soft and round, tuned to the key|the tongue drum","a wet pluck that echoes in dotted 8ths|the dotted pluck","a saw pluck with a fast filter close on every note|the saw pluck","a hollow log-drum patch, each hit tuned to the chord|the log drum"],"character":["dry and bright, up front","echoed in dotted 8ths, the echo lower than the note","high-passed thin like a phone","stereo-widened above 2k only","velocity so uneven it sounds hand-tapped","a short pitch dip on the attack of each note","ducked hard under the kick so it pumps"]},"trap-soul_guitar":{"frame":"over a glossy pop R&B beat","sources":["a clean electric guitar, single-note line with a light chorus|the clean guitar","a muted electric guitar plucked in short pairs|the muted guitar","a steel-string acoustic guitar, close-miked, fingerpicked|the acoustic guitar","a reversed guitar chord resampled as a one-shot|the reversed guitar","a pitch-shifted guitar loop sped up a few percent|the sped guitar loop","a clean guitar harmonic looped as a pulse|the guitar harmonic"],"character":["lightly chorused, dry and close","through a clean amp with the room cut","pick noise and string squeaks left in","a slow tremolo on the long notes","doubled and panned a little either side","soft-clipped so it sits flat behind the keys"]}},
      BASS: { source: ["a clean 808 with a soft round attack","a sine sub that only moves on the chord change","a plucked synth bass, short and rubbery","a long 808 with a slow glide into each chord root","a fingered bass guitar sample, round and short","an 808 layered with a soft electric bass pluck"], tone: ["a clean round body with the top rolled off","soft saturation just for phone speakers","a tight decay that leaves a gap before each kick","glide only on the chord change","a slow low-pass opening on the hook","a short click on the attack, otherwise pure"], pattern: Base.BASS.pattern, human: Base.BASS.human },
      DRUMS: { kick: ["a soft round pop kick with a tight thud","a short kick with the sub cut so the bass carries","a dry knocking kick doubled a 16th before beat 3","a deep soft kick on 1 and the and of 2","a 3-3-2 kick pattern, bouncing","a tight clicky kick tucked under the sub"], snare: ["a soft layered snap and clap, wide and short","a side-stick click on 2 and 4","a snappy pitched-up clap with a short bright plate","a finger snap on the backbeat, dry and close","a clap stacked with a thin snare, a few milliseconds apart","a soft snare with its tail gated short"], hats: ["closed hats in light 8ths with a shaker on the 16ths","tight trap hats rolling only into the hook","a shaker loop with the accents drifting","soft closed hats, barely there in the verse","ticking hats with one open hat on the and of 4","bright hats pitched up and panned right"] },
      COLOR: ["a wooden tongue-drum knock on the off-beats","congas played softly, a little behind","a shaker shaken by hand, uneven","a tambourine on 2 and 4, hit lightly","a dry wooden click answering the snap","a finger snap stacked on the clap, late"],
      GROOVE: { sync: ["kick on 1 and the and of 2, snap on 3","a half-time snap under 16th hats","the 3-3-2 pattern in the kick, straight hats on top","the bass anticipates the chord change by a 16th","a push into beat 3 every other bar","the kick left out of bar 4 so the snap hangs alone"], human: ["the snap a hair late, every time","hats pushed a touch ahead, kick dead on","a loose pocket with the shaker wandering","clap layers a few milliseconds apart","the pluck played in by hand, velocity never even","everything laid back except the hats"], triplet: ["a soft triplet roll into the hook only", "the snap a hair late, hats straight", "a half-time snap under straight 16th hats"] },
      LEAD: { ...Base.LEAD, motif: ["a four-note major-key figure that leans on the sixth","a two-chord loop: minor, then its relative major","a three-note rising figure that resolves up, then repeats a step lower","a five-note figure built on the major sixth and the second","a four-chord cycle, one minor and three major, never starting on the root","a two-note call that falls a major third, answered a bar later"], articulation: ["dotted-8th plucks that echo across the bar","one chord per bar, struck soft, held under the snap","off-beat stabs on the and of every beat, muted short","a four-chord cycle, each chord struck once on the and of 1","long sustained chords with only the top note moving","quick 16th double hits leading into each chord"] },
      RHYTHM: { ...Base.RHYTHM, sync: ["kick on 1 and the and of 2, snap on 3","a half-time snap under 16th hats","the 3-3-2 pattern in the kick, straight hats on top","the bass anticipates the chord change by a 16th","a push into beat 3 every other bar","the kick left out of bar 4 so the snap hangs alone"], twitch: ["light hat stutters into the hook", "a subtle percussion twitch under the verse", "clipped hat flicks on the and of 4", "one hat stutter before each hook"] },
      SWITCH: Base.SWITCH,
      STRUCTURE: (p) => {
        const H = p.lead.handle, c = p.counter;
        return j([
          `[Intro | ${H} alone, filtered dark, 4 bars]`,
          `[Verse | sparse, ${p.drums.snare.replace(/^an? /, "")}, sub, ${H} low, ${p.drums.sync}]`,
          `[Pre-Hook | hats enter, bass opens up, ${H} climbing, ${c.handle} ${c.role}]`,
          `[Hook | full beat, ${H} up front, wide and glossy, ${p.lead.motion}]`,
          `[Verse 2 | ${H} thinned, 808 sliding, ${p.drums.feel}]`,
          `[Hook | full beat, ${H} up front, wide and glossy]`,
          `[Feature Verse | drums and 808 only, ${H} one note per bar, open mids, room for a guest verse]`,
          `[Hook | full beat, last pass, brightest]`,
          `[Outro | ${H} alone, ends dry]`,
          "[End]",
        ]);
      },
    },
  },
  soul: {
    id: "soul",
    label: "Soul / emo hip hop",
    blurb: "Sample-born keys or sad guitar over a loose played-in pocket, a buzzing 808, one hook. Chicago-soul and emo flavors.",
    grammar: {
      TEMPO: [[66,78],[84,96],[100,112]],
      MAJOR_SHARE: 0.4,
      MIX_TAG: "warm punchy mix, drums up front",
      MODES: [["switch",60],["loop",20],["straight",20]],
      SLUR_SHARE: 0.4,
      LOW_SHARE: 0.5,
      SLIDERS: {"switch":{"weirdness":[45,60],"styleInfluence":[65,75]},"loop":{"weirdness":[45,55],"styleInfluence":[70,75]},"straight":{"weirdness":[45,60],"styleInfluence":[65,75]}},
      EXCLUDE: BASE_EXCLUDE.concat(["gospel choir","church organ","worship music","jazz","lo-fi hiss","vinyl crackle","acoustic ballad","singer-songwriter","indie folk","pop punk","emo rock"]),
      LANES: [{"id":"soul","label":"Soul / emo hip hop","weight":1,"anchors":["soul-sample hip hop, bright keys","emo hip hop, sad guitar, distorted 808","soul hip hop, Chicago, live-sounding drums","sad hip hop, grainy, buzzing 808","soul-sample hip hop, sunny and sped up","emo trap, bedroom-recorded, raw"],"families":{"soul_keys":3,"sad_guitar":3,"soul_sample":2},"bass":[]}],
      FAMILIES: {"soul_keys":{"frame":"over a loose played-in hip hop pocket","sources":["a drawbar organ through a slow rotary speaker, Sunday-morning glow|the drawbar organ","a tonewheel organ with the percussion click on, stabbing short chords|the tonewheel organ","a bright grand piano played heavy, extended chords, pedal down|the bright piano","a tine keyboard with a soft bark, slightly overdriven, chorused|the tine keys","a sampled soul-record keyboard, pitched up a half step, looped at one bar|the pitched keys loop","a toy-bright keyboard patch with the tremolo on|the tremolo keys","a pump-style reed organ, breathy, holding chords|the reed organ"],"character":["bright and slightly overdriven, keys clacking","the rotary speaker speeding up into the hook","pitched up a half step off an old record","glued flat to the drums with slow compression","extended seventh chords voiced high and open","low-passed in the verse, wide open in the hook","the pitch sagging on every held chord"]},"sad_guitar":{"frame":"over a loose played-in hip hop pocket","sources":["a sad clean electric guitar, single notes with a long reverb tail|the sad guitar","a steel-string acoustic guitar, fingerpicked, close and dry|the fingerpicked guitar","a muffled guitar loop, high-passed, the loop seam audible|the guitar loop","a detuned acoustic guitar sample, pitched down, grainy|the grainy guitar","a clean guitar arpeggio through a slow chorus and a long plate|the arpeggio guitar","a reversed guitar swell resampled, dark and sad|the reversed guitar","a distorted guitar chord hit once per bar, muffled under a blanket|the muffled chord"],"character":["grainy, sample-rate reduced, the top end gone","bedroom-recorded through a cheap interface, room noise and all","low-passed like it is playing in the next room","band-passed thin like a phone speaker","the loop seam and click left in on purpose","a long dark plate and nothing else","repitched a step down so it drags"]},"soul_sample":{"frame":"over a loose played-in hip hop pocket","sources":["a soul-record sample pitched up a step and sped up, bright and sunny|the sunny sample","a soul-record keyboard loop, sliced at one bar, the seam audible|the sliced loop","a pitched-up soul sample with a soaring top line, cut to four hits|the four-hit sample","a soul-record horn stab, pitched up, cut short|the horn stab","a soul sample time-stretched so the grain smears|the stretched sample","a low-passed soul-record bass note and chord, looped as one hit|the low loop"],"character":["pitched up and sped up like an old record","cut into one-bar pieces and rearranged","high-passed above the 808 so the low end stays clean","sample-rate reduced to a grainy edge","pumping under the kick on every hit","the loop seam clicking on every bar line"]}},
      BASS: { source: ["a distorted 808 clipped until it buzzes","a raw 808 with the sub pulled down and the mids pushed up","a long sliding 808 with the tail ringing into the next bar","a round fingered bass guitar sample, short and plucky","a sine sub under a soul-record bass loop","a saturated 808 played an octave higher than usual"], tone: ["overdriven so the distortion carries on a phone","a clean round body that follows the keys' root","a long tail that bleeds under the snare","glide into the downbeat only","a slow pitch fall at the end of every note","bitcrushed on the attack, clean underneath"], pattern: Base.BASS.pattern, human: Base.BASS.human },
      DRUMS: { kick: ["a live-sounding kick with the beater click and some room","a muffled sampled kick, soft and round","a hard-clipped trap kick stacked on an 808","a dead thud kick with the low end left to the 808","a punchy sampled kick a touch early","a boxy drum-kit kick, close-miked"], snare: ["a live-sounding snare with a wide short room","a fat sampled snare with the wires buzzing","a clap stacked with a snare, the clap a hair late","a thin crispy trap snare pitched up","a loose hand clap stacked three times, slightly out of sync","a side-stick on 2 with a full snare on 4"], hats: ["hand-played hats, every hit a different velocity","loose open hats sizzling on the and of each beat","closed hats in 8ths, barely quantized","trap hats rolling in 16ths then stopping dead","a ride cymbal ticked lightly through the verse","ticking hats low in the mix, pushed slightly ahead"] },
      COLOR: ["a tambourine shaken loosely on every 8th","finger snaps landing a hair late","a shaker played live, the accents wandering","a dry woodblock knocking the off-beats","soft congas played by hand under the hook","a clap on 2 and 4 that doubles on the last bar"],
      GROOVE: { sync: ["kick on 1 and the and of 3, snare dead on 2 and 4","the kick skipping beat 3 every other bar","a lazy half-time snare under 8th hats","the sample pushed a 16th ahead of the kick","kicks doubled before the snare on bar 4","the 808 landing after the kick, never with it"], human: ["a loose pocket, played in, never snapped","the snare dragging behind, the hats pushing ahead","drums played by a person, every bar a little different","the whole kit sitting back on the beat","kick drifting behind the grid","one take, the small mistakes left in"], triplet: Base.GROOVE.triplet },
      LEAD: { ...Base.LEAD, motif: ["a four-chord soul loop, major seventh to minor, repeating every two bars","a two-bar keyboard phrase that lands on a suspended chord","a sad three-note guitar figure falling a minor third","a four-note guitar descent ending a half step below the root","a bright six-note piano run that climbs and stops on the sixth","a sample phrase whose last note is cut off every other bar"], articulation: ["one chord per bar, struck on the and of 1 and held","fingerpicked 8ths with the thumb dragging","short stabs on 2 and 4 with the snare","one slice per beat, the loop restarting every bar","a single strummed chord that rings for two bars","legato runs that slur into each other"] },
      RHYTHM: { ...Base.RHYTHM, sync: ["kick on 1 and the and of 3, snare dead on 2 and 4","the kick skipping beat 3 every other bar","a lazy half-time snare under 8th hats","the sample pushed a 16th ahead of the kick","kicks doubled before the snare on bar 4","the 808 landing after the kick, never with it"], twitch: ["hats that stutter then fall back to 8ths", "a nervous hat flick before the snare", "twitchy percussion low in the mix", "stuttering snare retriggers on bar 4"] },
      SWITCH: {
        axes: [
          { tag: "beat switch, drums go live", cue: "the drums go live-sounding and loose, the key falls a minor third, the 808 buzzing louder", after: "in the lower key, drums loose and wide" },
          { tag: "beat switch to half-time", cue: "half-time drums, lazy snare on 3, long buzzing 808 slides, the sample pitched down", after: "half-time, lazy snare, sample pitched down" },
          { tag: "second half double-time", cue: "double-time hats, kick pattern doubles, the sample sliced to one hit per beat", after: "double-time, sample sliced per beat" },
        ],
        land: Base.SWITCH.land,
      },
      STRUCTURE: (p) => {
        if (p.mode !== "switch") return null;
        const H = p.lead.handle, c = p.counter, sw = p.sw;
        return j([
          `[Intro | ${H} alone, grainy, 4 bars]`,
          `[Verse | ${p.drums.kick.replace(/^an? /, "")}, ${p.drums.hats.replace(/^an? /, "")}, 808 ${p.bass.pattern}, ${H} low, ${p.drums.sync}]`,
          `[Hook | full beat, ${H} up front, bright, ${c.handle} ${c.role}]`,
          `[Verse 2 | ${H} thinned, 808 sliding, ${p.drums.feel}]`,
          `[Break | drums cut, ${H} and 808 only, ${sw.land}]`,
          `[Beat Switch | ${sw.cue}]`,
          `[Hook | ${sw.after}, ${H} ${p.lead.motion}]`,
          `[Outro | ${sw.after}, ${H} alone, ends on one held chord]`,
          "[End]",
        ]);
      },
    },
  },
  boombap: {
    id: "boombap",
    label: "Boom bap soul mashup",
    blurb: "A sped-up soul record sliced into stabs on a cracking 12-bit snare and a boom kick, with a modern 808 under it all.",
    grammar: {
      TEMPO: [[84,94],[96,104],[140,152]],
      MAJOR_SHARE: 0.3,
      MIX_TAG: "knocking drums, sample bright, 808 heavy, clean mix",
      MODES: [["switch",55],["loop",25],["straight",20]],
      SLUR_SHARE: 0.5,
      LOW_SHARE: 0.5,
      SLIDERS: {"switch":{"weirdness":[40,55],"styleInfluence":[70,80]},"loop":{"weirdness":[40,50],"styleInfluence":[75,80]},"straight":{"weirdness":[40,55],"styleInfluence":[70,80]}},
      EXCLUDE: BASE_EXCLUDE.concat(["jazz","funk","disco","DJ scratching","turntablism","vinyl crackle","drum fills","lounge","G-funk","talkbox","trip hop"]),
      LANES: [{"id":"boombap","label":"Boom bap soul mashup","weight":1,"anchors":["Texas hip hop, soul-sample flip, hard 808s","boom bap soul flip, modern 808s","southern boom bap, flipped soul sample","Dallas hip hop, sped-up soul sample, knocking drums","soul-sample boom bap, heavy 808","hard boom bap, pitched-up soul flip"],"families":{"soul_sample_flip":5,"soul_keys":2,"sampled_horns":2},"bass":[]}],
      FAMILIES: {"soul_sample_flip":{"frame":"over knocking boom bap drums and a modern 808","sources":["a late-70s soul-record sample pitched up a step and sped up, the whole band in one loop|the sped soul loop","a soul-record piano and bass loop, sliced at every downbeat|the sliced soul piano","a soul sample with a soaring top line, cut to four stabs|the four-stab sample","a soul-record guitar lick looped at two bars, pitched up|the soul guitar lick","a 12-bit sampled soul chord, one hit per bar, the tail cut|the 12-bit chord","a soul-record intro sliced into eight pieces and re-ordered|the re-ordered sample","a filtered soul loop with the bass cut so the 808 owns the low end|the filtered soul loop"],"character":["pitched up and sped up like an old record","12-bit grain on every slice","high-passed hard so the 808 carries the sub","the loop seam audible on every bar line","re-pitched a half step per section","cut into one-bar pieces and rearranged","the sample ducking under every kick"]},"soul_keys":{"frame":"over knocking boom bap drums and a modern 808","sources":["a bright grand piano, two-handed chords struck hard|the hard piano","a tine keyboard with a soft bark, slightly overdriven|the tine keys","a drawbar organ through a slow rotary speaker|the drawbar organ","a pitched-up piano sample looped at one bar, the seam audible|the piano loop","a dark low piano note, struck hard and left to ring|the low piano","a sampled keyboard stab, pitched up, cut short|the keyboard stab"],"character":["pushed through a cranked tube preamp","12-bit sample grain on the attacks","low-passed so it sits behind the snare","pitched up a half step off an old record","compressed flat so every chord is the same size","keys clacking audibly under the notes"]},"sampled_horns":{"frame":"over knocking boom bap drums and a modern 808","sources":["a soul-record horn section stab, pitched up, cut short|the horn stab","a lone trumpet with a cracked, raspy tone|the lone trumpet","a trombone section playing short punchy swells|the trombones","a sampled horn line with the room cut, three notes then silence|the horn line","a low brass stab doubled by the 808|the low brass stab","a trumpet section pitched down a whole step, heavy|the trumpets"],"character":["blown so hard the brass rasps and buzzes","pitched down off an old record so it sounds heavy","tight and dry, the room tail cut off","12-bit sampled, the tail gated","distorted like it is coming out of a car trunk","a little out of tune between players, so it sounds real"]}},
      BASS: { source: ["a long-decay 808 tuned to the sample's root","a distorted 808 with a hard knock on top","a sine sub under the soul-record bass line","an 808 sliding up a fifth into each chord","a pitched-down kick used as bass","a sampled electric bass loop, pitched up with the sample, plus a sine sub"], tone: ["tube saturation so it shows up on phone speakers","a hard knock that outlasts the kick","a slow glide only on the chord change","long tails that ring into the snare","the sub following the sample's bass note exactly","the top end rolled off so the sample stays bright above it"], pattern: Base.BASS.pattern, human: Base.BASS.human },
      DRUMS: { kick: ["a hard sampled boom kick, 12-bit, dry","a knocking kick layered with a soft 808 thump","a kick doubled on the and of 3","a dry kick that lands a 16th before the snare on bar 4","a hard-clipped kick with a wooden knock on top","a tight 12-bit kick dead center"], snare: ["a cracking 12-bit snare, compressed hard","a fat sampled snare with a clap under it","a snare smacked on 2 and 4, a hair late","a snare with the wires rattling, dry","a layered clap and snare, the clap pitched up","a dry snare with a wooden crack, tail cut"], hats: ["crunchy 12-bit hats in uneven 8ths","hats dragging late like a finger on a drum machine","closed hats in 8ths with one open hat per bar","trap hats in 16ths under a boom bap kick","hats that cut out every 4th bar","tight hats pitched down a step, crunchy"] },
      COLOR: ["a tambourine smacked on 2 and 4, never quite even","a dry woodblock knocking the off-beats","finger snaps on 2 and 4 stacked under the snare","a shaker played live, the accents wandering","congas from the sample left in, low in the mix","a clap on the and of 4 only"],
      GROOVE: { sync: ["kick on 1 and the and of 2, snare dead on 2 and 4","the kick anticipating beat 3 by a 16th","a 2-bar kick pattern that never lands on 3","kick on the ands, snare on the beat","the 808 hitting with the kick, then sliding alone on beat 4","a full stop on beat 4 every 8 bars"], human: ["hats dragging late, kick dead on","the whole loop sitting back on the beat","drums played by hand, each bar a little different","the snare a hair late, the kick a hair early","sample slices landing late, drums on time","a drunk pocket that always finds the one"], triplet: Base.GROOVE.triplet },
      LEAD: { ...Base.LEAD, motif: ["a two-bar sample phrase that ends on a major seventh","a four-stab sample hook that climbs a fourth","a sample slice re-pitched up a step on the second pass","a sampled horn figure, three notes then silence","a piano loop whose last chord is cut off early every bar","a bass note and chord hit together on 1, then silence until the and of 3"], articulation: ["one slice per beat, the loop restarting on every downbeat","a chord hit on 1, muted on 2, hit again on the and of 3","two long slices then four quick ones","the sample cut before each snare so the crack is bare","the whole loop played straight for 4 bars, then diced","stabs that only answer the snare"] },
      RHYTHM: { ...Base.RHYTHM, sync: ["kick on 1 and the and of 2, snare dead on 2 and 4","the kick anticipating beat 3 by a 16th","a 2-bar kick pattern that never lands on 3","kick on the ands, snare on the beat","the 808 hitting with the kick, then sliding alone on beat 4","a full stop on beat 4 every 8 bars"], twitch: Base.RHYTHM.twitch },
      SWITCH: {
        axes: [
          { tag: "beat switch, new drums", cue: "new drums, harder and louder, the sample pitched up a half step, 808 heavier", after: "new drums, sample a half step up" },
          { tag: "beat switch to half-time", cue: "half-time drums, the sample stretched, long 808 slides", after: "half-time, sample stretched" },
          { tag: "beat flips at the midpoint", cue: "the sample re-sliced to one hit per beat, kick pattern flips to the ands", after: "re-sliced sample, kicks on the ands" },
        ],
        land: Base.SWITCH.land,
      },
      STRUCTURE: (p) => {
        if (p.mode !== "switch") return null;
        const H = p.lead.handle, c = p.counter, sw = p.sw;
        return j([
          `[Intro | ${H} alone, filtered, 4 bars]`,
          `[Verse | ${p.drums.kick.replace(/^an? /, "")}, ${p.drums.snare.replace(/^an? /, "")}, 808 ${p.bass.pattern}, ${H} low, ${p.drums.sync}]`,
          `[Hook | full beat, ${H} up front, the 808 loud, ${c.handle} ${c.role}]`,
          `[Verse 2 | ${H} re-sliced, hats doubled, ${p.drums.feel}]`,
          `[Break | drums cut, ${H} and 808 only, ${sw.land}]`,
          `[Beat Switch | ${sw.cue}]`,
          `[Hook | ${sw.after}, ${H} ${p.lead.motion}]`,
          `[Outro | ${sw.after}, ${H} alone, ends on a hard cut]`,
          "[End]",
        ]);
      },
    },
  },
  country: {
    id: "country",
    label: "Country x hip hop",
    blurb: "Nashville acoustic instruments dropped on a Dallas 808. Stomp-and-clap radio, or lonely trap-country. Guest-verse slot.",
    grammar: {
      TEMPO: [[70,86],[96,110],[122,150]],
      MAJOR_SHARE: 0.55,
      MIX_TAG: "radio country trap mix, 808 heavy",
      MODES: [["straight",45],["switch",40],["loop",15]],
      SLUR_SHARE: 0.4,
      LOW_SHARE: 0.5,
      SLIDERS: {"switch":{"weirdness":[35,50],"styleInfluence":[70,80]},"loop":{"weirdness":[35,45],"styleInfluence":[75,80]},"straight":{"weirdness":[35,50],"styleInfluence":[70,80]}},
      EXCLUDE: BASE_EXCLUDE.concat(["bluegrass","honky-tonk","square dance","line dance","hoedown","barn dance","rodeo","yodeling","americana","folk","campfire","acoustic ballad","singer-songwriter","arena rock","stadium rock","pop punk","southern rock guitar solo","orchestral strings","fiddle solo","EDM drop"]),
      LANES: [{"id":"stomp","label":"Stomp and clap radio","anchors":["country trap, stomp and clap, radio twang","stomp-and-clap country trap, dobro, hard 808","radio country trap, acoustic riff, trap hats, big hook","trap country, boot stomps and hand claps, 808"],"families":{"twang_pickers":4,"stomp_grit":3,"lonely_steel":1},"tempo":[[122,150]],"weight":1,"bass":[]},{"id":"lonely","label":"Lonely trap-country","anchors":["lonely trap country, pedal steel, buzzing 808","sad country trap, half-time, pedal steel over 808s","country hip hop, slow and lonesome, slide guitar, long 808","dark trap country, low piano, harmonica, snare on 3"],"families":{"lonely_steel":4,"twang_pickers":2,"stomp_grit":1},"tempo":[[70,86]],"weight":1,"bass":[]}],
      ALLOW: /country|banjo|slide[- ]guitar|twang\w*/i,
      FAMILIES: {"twang_pickers":{"frame":"","sources":["a steel-string acoustic guitar strummed hard with a thin pick, every string scrape left in|the acoustic strum","a resonator dobro picked with metal fingerpicks, bright and barking|the dobro","a clawhammer banjo thumped and brushed, the head slapping on every downstroke|the banjo","a mandolin tremolo-picked on one string, nervous and bright|the mandolin","a flat-picked acoustic guitar riff doubled an octave down on a baritone acoustic|the flat-picked riff","a fiddle sawing short double-stops, rosin grit on every bow stroke|the fiddle","an electric baritone guitar picked low and dry, the pick clicking against the string|the baritone guitar","a bridge-pickup clean electric guitar, popping and snapping on every note, compressed hard|the snapping electric"],"character":["picked so hard the fingerpicks click on the metal","close-miked with the pick noise and string scrape left in","doubled and panned hard left and right, the two takes slightly out of time","run through a cranked small tube amp until it spits","high-passed so the 808 owns everything under 100 Hz","compressed flat so every note slaps the same","capo'd high so it rings thin and bright above the 808"]},"lonely_steel":{"frame":"","sources":["a pedal steel guitar sliding slow into each chord, the volume pedal swelling in late|the pedal steel","a lap steel played with a glass bar, the bar rattling on the string|the lap steel","a slide guitar on a resonator body, the slide buzzing against the frets|the slide guitar","a dark grand piano, one low chord struck hard and left to ring under the 808|the low piano","a lonesome harmonica breathed in long bent notes, the breath audible|the harmonica","a reversed pedal steel swell resampled into a one-shot|the reversed steel","a baritone electric guitar with a heavy tremolo, picked slow and low|the tremolo baritone","a fiddle holding one long low note, the bow pressure wavering|the low fiddle"],"character":["sliding into pitch late and landing a hair under","a volume pedal swelling each note in after the kick","a long dark plate on the steel and nothing else","pitched down a whole step so it drags behind the beat","doubled by a distorted 808 on the root","run through a slow tremolo so it shivers","the bar noise and string squeak left in"]},"stomp_grit":{"frame":"","sources":["a sampled acoustic guitar loop pitched up a step and sliced to one hit per beat|the sliced acoustic loop","a banjo roll sampled, pitched down an octave, dark and thumping|the pitched banjo","a dobro lick time-stretched until the grain smears|the stretched dobro","a piano played in hard two-handed stabs, pedal down, over the 808|the stab piano","a resampled fiddle run cut to four stabs|the four-stab fiddle","a palm-muted acoustic guitar chugging 8ths, more percussion than melody|the muted chug","a harmonica sample pitched down two octaves and distorted into a bass horn|the pitched harmonica","a mandolin strummed on the off-beats, the pick smacking the body|the mandolin strum"],"character":["resampled until the grain shows","gated so each hit stops dead before the snare","hard-clipped on every hit","high-passed hard so the 808 carries the sub","the loop seam audible on every bar line","pitched down until it sounds heavy","tuned by ear, a few cents off"]}},
      BASS: { source: ["a long-decay 808 tuned under a thumped acoustic bass, the finger slap audible","a distorted 808 that slides a fourth into each chord root like a steel player","a sine sub under a picked electric bass, flatwound and thuddy, every note dead by the next kick","a short knocking 808 under a muted bass guitar plucked with a pick","a pitched-down kick stacked on a round fingered bass","a buzzing 808 clipped until it rattles, holding the root for a whole bar"], tone: ["tube saturation so the 808 shows up on a gas-station phone speaker","a clean round body with the acoustic bass click on top","glide only into the chord change, like a steel bar sliding","a tight decay that dies before the next stomp","long tails that ring into the half-time snare","the upper harmonics brought up with distortion so the acoustic strum has to fight for room"], pattern: Base.BASS.pattern, human: Base.BASS.human },
      DRUMS: { kick: ["a trap kick stacked on a boot stomp on a wooden floor","a hard-clipped kick with a wood-floor stomp on top","a deep soft kick with the sub left to the 808","a kick that lands on 1 and the and of 3 like a boot heel","a dry knocking kick doubled a 16th before beat 3","a boxy drum-kit kick close-miked, the beater click loud"], snare: ["a hand clap stacked with a boot stomp on 2 and 4, never quite even","a half-time snare on 3 only, a dry crack with a wooden ring","a cracking trap snare layered with a room of people clapping, a few milliseconds apart","a side-stick click in the verse, full snare in the hook","a snare with the wires rattling, hit on 3 under rolling hats","a clap doubled by a thigh slap, close and dry"], hats: ["trap hats rolling in 16ths under an acoustic strum","a shaker in place of hats, shaken by hand","tight closed hats in uneven 16ths, pitched down a step","hats in 8ths with one open hat on the and of 4, rolling into the hook","a tambourine ticking the 8ths in place of hats","half-open hats on the off-beats, crunchy"] },
      COLOR: ["boot stomps on a wooden porch, four people, never quite together","a room of hand claps on 2 and 4, drifting a few milliseconds apart","a tambourine smacked against a thigh on 2 and 4","a washboard scraped with thimbles on the off-beats","a pair of spoons clacked against a knee under the hats","a thigh slap and a finger snap trading the off-beats"],
      GROOVE: { sync: ["kick on 1 and the and of 3, stomp-clap dead on 2 and 4","a half-time snare on 3 under 16th trap hats","the acoustic strum pushed a 16th ahead of the kick","the 808 anticipating the chord change by a 16th","the kick left out of bar 4 so the clap hangs alone","a full stop on beat 4 every 8 bars, the steel ringing through"], human: ["the claps a few milliseconds apart, like a room of people","the steel landing a hair under pitch, then settling","the strum rushing ahead, the kick dead on","stomps played by boots, every one a slightly different weight","a loose pocket, played in, never snapped","one take with the string squeaks and a missed pick left in"], triplet: Base.GROOVE.triplet },
      LEAD: { ...Base.LEAD, motif: ["a four-note major-key riff that leans on the flat seventh","a two-chord loop: the one and the four, the top note never moving","a three-note pickup that lands on beat 1 and stops","a six-note descent down the major scale, the last note bent flat","a two-note call that falls a fourth, answered a bar later a step lower","a five-note figure circling the fifth with one hammered-on second"], articulation: ["flat-picked 8ths with a hard accent on the and of 2","one strummed chord on 1, muted dead on 2, hit again on the and of 3","long steel notes that start a 16th late and swell in","double-stop stabs on 2 and 4 with the clap","a single strummed chord that rings for two bars under the hook","hammer-ons and pull-offs slurring into each other, one hard pick per bar"] },
      RHYTHM: { ...Base.RHYTHM, sync: ["kick on 1 and the and of 3, stomp-clap dead on 2 and 4","a half-time snare on 3 under 16th trap hats","the acoustic strum pushed a 16th ahead of the kick","the 808 anticipating the chord change by a 16th","the kick left out of bar 4 so the clap hangs alone","a full stop on beat 4 every 8 bars, the steel ringing through"], twitch: ["hat rolls pitched down a step into the hook", "a stuttered clap before the snare", "shaker flicks on the and of 4", "one hat stutter into each hook", "clipped hat bursts under the stomps"] },
      SWITCH: {
        axes: [
          { tag: "beat switch to half-time", cue: "half-time drums, tempo halves, the lead pitched down a whole step, long buzzing 808 slides", after: "half-time, the lead pitched down, long 808 slides" },
          { tag: "beat switch, key falls", cue: "key falls a minor third, the 808 doubles the lead, same stomps and claps", after: "in the lower key, 808 doubling the lead" },
          { tag: "beat switch, new drums", cue: "new drums, trap hats doubled, a harder clap stack, the 808 goes rolling", after: "new drums, rolling 808" },
        ],
        land: Base.SWITCH.land,
      },
      STRUCTURE: function (p) {
        if (p.mode === "loop") return null;
        const H = p.lead.handle, c = p.counter, sw = p.sw;
        const big = p.lane === "stomp" ? "stomps and claps wide" : "808 wide, drums heavy";
        const lines = [
          `[Intro | ${H} alone, close and dry, 4 bars]`,
          `[Verse | ${p.drums.kick.replace(/^an? /, "")}, ${p.drums.snare.replace(/^an? /, "")}, 808 ${p.bass.pattern}, ${H} low, ${p.drums.sync}]`,
          `[Pre-Hook | hats roll in, claps enter on 2 and 4, ${H} climbing, ${c.handle} ${c.role}]`,
          `[Hook | full beat, ${big}, ${H} up front, 808 loud, ${p.lead.motion}]`,
          `[Verse 2 | ${H} thinned, 808 sliding, ${p.drums.feel}]`,
          `[Hook | full beat, ${big}, ${H} up front]`,
          `[Feature Verse | drums and 808 only, ${H} one note per bar, open mids, room for a guest verse]`,
        ];
        if (p.mode === "switch") lines.push(`[Beat Switch | ${sw.cue}]`, `[Hook | ${sw.after}, ${big}, last pass, biggest]`, `[Outro | ${sw.after}, ${H} alone, ends on one held chord]`);
        else lines.push(`[Hook | full beat, last pass, biggest, ${H} up front]`, `[Outro | ${H} alone, ends on one held chord]`);
        lines.push("[End]");
        return j(lines);
      },
    },
  },
  prof: {
    id: "prof",
    label: "Prof",
    blurb: "Two moods in one button: a crawl through a low tide (drunk piano, dark organ, muddy sliding bass, slurred triplets) or good time boy (stomping claps, huge saw bass, siren hooks, horn stabs).",
    grammar: {
      TEMPO: [[68,80],[84,96],[126,142]],
      MAJOR_SHARE: 0.25,
      MIX_TAG: "loud bar-jukebox mix, bass huge, drums cracking",
      MODES: [["switch",35],["loop",25],["straight",40]],
      SLUR_SHARE: 0.6,
      LOW_SHARE: 0.6,
      SLIDERS: {"switch":{"weirdness":[45,60],"styleInfluence":[65,78]},"loop":{"weirdness":[45,55],"styleInfluence":[70,78]},"straight":{"weirdness":[45,58],"styleInfluence":[65,78]}},
      EXCLUDE: BASE_EXCLUDE.concat(["blues","blues rock","country","jazz","funk","EDM drop","whistling","circus","carnival","polka","lo-fi","vocals","singing","crowd noise","DJ scratching","sea shanty"]),
      LANES: [{"id":"lowtide","label":"Low tide crawl","anchors":["Minneapolis late-night crawl hip hop, muddy sliding bass","Midwest bar-room hip hop, drunk piano, long 808 tails","slow heavy Minnesota hip hop, dark organ keys, slurred bounce","low-tide hip hop crawl, sour bent minor hook, half-time drums"],"families":{"drunk_piano":4,"dark_organ":3,"stomp_brass":1},"tempo":[[68,80],[82,92]],"modes":[["switch",30],["loop",40],["straight",30]],"weight":1,"bass":[]},{"id":"goodtime","label":"Good time boy","anchors":["Minneapolis bar-fight hip hop, heavy bass","rowdy Midwest party hip hop, stomping drums, piercing sine hook","trap-rock hip hop, huge saw bass, gang-stomp claps","Twin Cities party hip hop, horn stabs, siren synth hook"],"families":{"siren_synth":4,"stomp_brass":3,"drunk_piano":1},"tempo":[[94,106],[126,142]],"modes":[["switch",35],["loop",15],["straight",50]],"weight":1,"bass":[]}],
      FAMILIES: {"drunk_piano":{"frame":"","sources":["a bar-room piano with sticky keys, played heavy and a little behind|the drunk piano","a dark low piano, two-handed minor chords struck hard and left to ring|the low piano","a detuned out-of-tune piano, the pitch sagging on every held chord|the sagging piano","a felt-dampened piano close-miked, every hammer thud audible|the felt piano","a piano sample pitched down a fourth until it drags, grainy|the dragged piano","a clanging piano hammering sour flat thirds with the sustain pedal down|the sour piano","a toy piano with plastic clanky hammers, doubled an octave down by a real piano|the toy piano","a reversed piano chord resampled into a one-shot that swells into each downbeat|the reversed piano"],"character":["slurring in and out of tune like it had a few drinks","keys clacking and the pedal thumping under the notes","low-passed so it sounds like it is coming through the wall of the bar next door","pushed through a cranked tube preamp until the low notes snarl","the pitch sagging on every held chord","one take, the wrong notes left in","the top end rolled off until only the body speaks"]},"dark_organ":{"frame":"","sources":["a cheap combo organ with a wavering vibrato, dark and low|the combo organ","a wheezing reed organ running out of air on the long chords|the reed organ","a drawbar organ with all the bright bars pushed in, only the low ones out|the dark drawbar organ","a drunken detuned organ, the pitch sagging and swaying|the drunken organ","a tonewheel organ with the percussion click on, stabbing short dark chords|the tonewheel organ","a broken organ with one dead key, buzzing on the held notes|the broken organ","an organ sample pitched down an octave so it drones like a foghorn|the foghorn organ","a toy keyboard organ patch run through a guitar amp until it barks|the amp organ"],"character":["the pitch sagging on every held chord","overdriven until the low notes snarl","wheezing and losing air at the end of each phrase","low-passed so it sits behind a wall","the vibrato speeding up as the chord dies","keys clacking audibly under the notes","sample-rate reduced to a grainy edge"]},"siren_synth":{"frame":"","sources":["a high chirping sine hook, two notes that pierce through everything|the chirping sine hook","a shrill tea-kettle sine lead that pierces like a referee's signal|the tea-kettle lead","an air-raid siren synth that climbs a fifth and holds|the air-raid siren","a fat synth brass stab patch, three short hits per bar|the synth horn stabs","a thick nasal saw lead honking the riff like a cartoon car horn|the honking saw lead","a rubber-band mono synth that boings on every glide|the rubber-band synth","a squealing pitch-bent mono lead like a car alarm|the car-alarm lead","a cheap trumpet-style synth preset playing a two-note taunt|the taunt lead"],"character":["hard-clipped on every transient","doubled an octave up so it cuts through a loud bar","detuned a few cents against itself","gated tight so every note cuts off clean","bitcrushed just enough to grit the attacks","pitched up a step for the last hook","panned hard left and right, a copy on each side"]},"stomp_brass":{"frame":"","sources":["a stacked trombone and trumpet section, blown hot, stabbing with the snare|the horn section","a sliding tuba line doubling the bass|the tuba","a bass trombone sliding down into each note|the bass trombone","a muted electric guitar through a fuzz pedal, chugging the root|the fuzz guitar","a dark baritone guitar riff, picked low and close|the baritone guitar","a trumpet section pitched down a whole step so it sounds enormous|the pitched-down trumpets","a distorted bass guitar played up high as the lead|the distorted bass guitar","a lone cracked trumpet, raspy, two notes then silence|the lone trumpet"],"character":["blown so hard the brass rasps and buzzes","fat and compressed into one wall of sound","distorted like it is coming out of a car trunk","a little out of tune between players, so it sounds real","tight and dry, the room tail cut off","overdriven through a small amp","doubled by a distorted 808 underneath"]}},
      BASS: { source: ["a sliding 808 with a long tail that drags into the next bar","a huge saw-wave synth bass, distorted and wide","a mono synth bass that slurs between notes like it is wading through mud","an 808 with a hard knock on top, tuned to the piano","a distorted 808 and a saw sub layered, the saw on top","a pitched-down kick used as bass, long and muddy"], tone: ["glide between every note, slow and seasick","long tails that ring under the snare and into the next bar","hard-clipped grit so it shows up on a bar jukebox","tube saturation so it shows up on phone speakers","a slow pitch fall at the end of every note","a click on the attack, the body round and heavy"], pattern: Base.BASS.pattern, human: Base.BASS.human },
      DRUMS: { kick: ["a deep thud kick with a long muddy tail","a stomping kick on every beat, hard-clipped","a dry knocking kick that lands slightly behind","a layered kick, sub plus a boot stomp on a wooden floor","a kick that only hits twice per bar, heavy","a hard punchy kick with a click on top, pushed ahead of the grid"], snare: ["a gang clap stacked with a snare, a hair late","a loose hand clap stacked three times, slightly out of sync","a fat sampled snare with the wires rattling","a half-time snare on 3 only, cracking","a snare and boot stomp hit together on 2 and 4","a crisp dry snare with a short room"], hats: ["hats slurring into threes on beat 3, then straightening","sparse 8th hats with one open hat per bar","trap hats rolling in 16ths then stopping dead","hand-played hats, every hit a different velocity","a shaker in place of hats, dragging","tight closed hats in uneven 16ths, pushed ahead"] },
      COLOR: ["a tambourine smacked on 2 and 4, never quite even","boot stomps on a wooden floor under the kick, a little late","a dry woodblock knocking the off-beats","finger snaps landing a hair late","glass bottles knocked with a stick, tuned to the key","a second clap layer slapped by hand, slightly early"],
      GROOVE: { sync: ["the 808 slurs into a triplet on beat 3","kick on every beat, the clap stomping on 2 and 4","a half-time snare under lazy 8th hats","the kick avoids beat 1 every other bar","the 808 anticipates the downbeat by a 16th","a full stop on beat 4 every 8 bars"], human: ["a little drunk on the timing but always landing the one","the whole loop sitting back on the beat","the clap a hair late, every time","piano and bass drifting apart, then finding each other","played in one take, the small mistakes left in","hats rushing slightly ahead, kick dragging behind"], triplet: Base.GROOVE.triplet },
      LEAD: { ...Base.LEAD, motif: ["a four-note minor descent, last note bent flat and sour","a two-note taunt a minor third apart, repeated until it is a dare","a three-note cell: root, flat third, root, dragged behind the beat","a six-note phrase circling the fifth, never resolving","a stubborn root note leaning on the flat sixth","a five-note figure that climbs a fourth and falls an octave onto the one"], articulation: ["stabs that only answer the snare","one chord per bar, struck heavy, left to ring","two long notes then a triplet stab","clipped 8th stabs on every beat, stomping with the kick","legato runs that slur into each other, a hair late","a three-note pickup that lands on beat 1 and stops"] },
      RHYTHM: { ...Base.RHYTHM, sync: ["the 808 slurs into a triplet on beat 3","kick on every beat, the clap stomping on 2 and 4","a half-time snare under lazy 8th hats","the kick avoids beat 1 every other bar","the 808 anticipates the downbeat by a 16th","a full stop on beat 4 every 8 bars"], twitch: Base.RHYTHM.twitch },
      SWITCH: {
        axes: [
          { tag: "beat switch into a crawl", cue: "the drums go half-time, the tempo halves into a crawl, long 808 tails, the lead slow and slurred", after: "half-time crawl, the lead slurred, bass dragging" },
          { tag: "beat switch into a stomp", cue: "the drums double into a stomp, gang claps and boot stomps on 2 and 4, the bass turns huge and distorted", after: "double-time stomp, claps on 2 and 4, bass huge" },
          { tag: "beat switch, key falls", cue: "key falls a minor third, the 808 tails longer, the lead pitched down and slurred", after: "in the lower key, the lead pitched down" },
        ],
        land: Base.SWITCH.land,
      },
      STRUCTURE: function (p) {
        if (p.mode === "loop") return null;
        const H = p.lead.handle, c = p.counter, sw = p.sw, low = p.lane === "lowtide";
        const full = low ? "the bass dragging, 808 tails ringing" : "stomping claps, the bass huge";
        const lines = [
          `[Intro | ${H} alone, ${low ? "dark and slurred" : "dry and loud"}, 4 bars]`,
          `[Verse | ${p.drums.kick.replace(/^an? /, "")}, 808 ${p.bass.pattern}, ${H} ${low ? "low and behind the beat" : "low, stabbing with the kick"}, ${p.drums.sync}]`,
          `[Hook | full beat, ${H} up front, ${full}, ${c.handle} ${c.role}]`,
          `[Verse 2 | ${H} thinned to one chord per bar, ${p.drums.feel}]`,
          `[Hook | full beat, ${H} up front, ${full}, ${p.lead.motion}]`,
        ];
        if (p.mode === "switch") lines.push(`[Break | drums cut, ${H} and 808 only, ${sw.land}]`, `[Beat Switch | ${sw.cue}]`, `[Hook | ${sw.after}, ${H} up front]`, `[Outro | ${sw.after}, ${H} alone, ends on one sour held chord]`);
        else lines.push(`[Outro | drums cut, ${H} alone, ends on one ${low ? "sour held chord" : "hard stab"}]`);
        lines.push("[End]");
        return j(lines);
      },
    },
  },
  };
})();

if (typeof module !== "undefined") module.exports = OneGenres;
