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
  };
})();

if (typeof module !== "undefined") module.exports = OneGenres;
