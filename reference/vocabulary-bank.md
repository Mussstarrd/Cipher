# Descriptor vocabulary bank

Words Suno responds to, per lane, grouped by the order they go into the Style field (genre -> drums/groove -> bass -> harmony/instruments -> texture/FX -> mood -> mix/master). The lane names are internal; **the artist names behind them never appear in prompts**. Priority-ordered pools the generator actually samples from live in `lib/lanes.json`; this page is the wider bank to draw from when hand-editing or critiquing. Full sourcing: `research/03-artist-vocabulary.md`. Items marked * were reported effective by Suno community prompt guides (no controlled A/B tests exist).

## Writing rules that apply to every lane

- Comma-separated tags, roughly 12-18 descriptors, ~350-500 characters. Over-stuffed prompts "fight each other into mud".
- "instrumental" inside the first three tags. Genre/era first, mood second, drums and 2-3 named instruments third, production last.
- Numeric BPM plus a feel word ("140 BPM, half-time feel"). Key names and modes ("C minor", "Dorian") are recognised as mood bias more than pitch.
- Describe the resulting texture, not the process ("tape-saturated drum bus" works; "4:1 compression" does not).
- Pair every exclusion with a positive replacement so the empty role is filled (exclude "trap hi-hats", add "shuffled live hi-hats").
- Never use vocal-implying words in Style: soulful, gospel, anthemic, chant, choir, vocal chops (except the phonk lane's "sample texture" phrasing), R&B, talkbox, ad-lib, singalong, call-and-response, pop song, ballad.
- Destabilising words to avoid: complex drums, aggressive, hard (pulls to generic trap), realistic, crescendo, ethereal, lush, shimmering.

## connor-price lane: bouncy playful trap-pop

| Group | Descriptors |
|---|---|
| Genre | trap-pop instrumental, pop-rap beat, bouncy hip hop, feel-good trap, playful pop-trap, upbeat hip hop instrumental, light boom-bap (storytelling variant) |
| Drums & groove | tight syncopated trap drums, crisp snappy snare, punchy short kick, finger snaps and claps, crisp hi-hat rolls with triplet stutters, bouncy off-beat kick pattern, stop-start drum mutes, drums drop out before the hook, swung 16th hi-hats, half-time trap feel 140 BPM*, bouncy 105 BPM groove (straight variant) |
| Bass | clean 808 bass with glides, rounded sub bass, staccato synth bass bounce, tight controlled low end |
| Harmony & instruments | bright synth plucks, music-box bells, marimba and kalimba plucks, glockenspiel hook, pizzicato string plucks, whistle-register synth lead, simple catchy hook melody, repeating nursery-rhyme melody, major 7th chord stabs, two-chord loop, staccato chord stabs |
| Texture & FX | reverse-cymbal pickups, riser into the drop, tape stop before the hook, record-scratch fills, ping-pong delay on the pluck, white-noise sweeps, cartoonish percussion one-shots, pitch-bend plucks |
| Mood | playful, cheeky, confident, sunny, energetic but light, tongue-in-cheek swagger, mischievous |
| Mix & master | crisp clean modern mix, bright top end, tight punchy low end, wide stereo plucks with centred drums, polished radio pop-rap master, good instrument separation*, clean production* |
| Wild pool | glitchy micro-edits, toy piano, 8-bit chiptune blips, reversed pluck swells, polyrhythmic percussion layer, beatbox-style percussion hits |
| Avoid | dark trap, drill, EDM build, festival drop, dubstep, boom bap as lead tag, vocal chops, chipmunk vocals, lo-fi, dusty, vinyl crackle, cinematic, orchestral, distorted 808, pop song, R&B ballad |

## paak-pocket lane: live swung neo-soul funk hip hop

| Group | Descriptors |
|---|---|
| Genre | live neo-soul hip hop instrumental, funk-soul hip hop, live-band hip hop, boom-bap soul, soul-sampling boom bap, lo-fi soul beat (chop variant), West Coast funk (polished variant), quiet-storm soul instrumental, breakbeat funk |
| Drums & groove | live drums with a laid-back swung pocket*, late behind-the-beat snare*, ghost-note hi-hats*, ghost-note snare, unquantized human timing, drunk swing, brushed drums*, dampened muffled snare, loose funk kick, shuffled hi-hats, tambourine on swung 8ths, shaker groove, funk break drums, rimshot verse groove, cross-stick rimshots, open hi-hat barks, 92 BPM head-nod groove |
| Bass | fingerstyle electric bass guitar, warm electric bass*, syncopated funk bass line, round P-bass tone, bass guitar slides and ghost notes, muted palm bass, Minimoog synth bass |
| Harmony & instruments | Rhodes electric piano with tremolo*, Wurlitzer, clavinet with wah, Hammond organ swells, minor 9th and major 7th voicings, Dorian vamp, muted funk guitar chucks, horn section stabs, soft string pads, flute counter-melody, vintage analog synth lead, 70s portamento synth lead, vibraphone |
| Texture & FX | vinyl crackle*, tape hiss, warm tape saturation, cassette wobble, chopped soul sample loop, dusty sample texture, spring reverb guitar, congas and bongos, room mic ambience |
| Mood | laid-back, sun-soaked, warm and nostalgic, smooth with grit, celebratory funk, late-night mellow, groovy, head-nod |
| Mix & master | warm analog mix, vintage warmth*, punchy but soft-top drums, tape-saturated drum bus, natural dynamics, glued, clean low end, wide stereo Rhodes, vintage console warmth |
| Wild pool | odd-meter turnaround, polyrhythmic percussion, psychedelic tape flutter, detuned Rhodes, half-speed breakdown, live room noise |
| Avoid | trap hi-hats, hi-hat rolls, 808, EDM, house, four-on-the-floor, quantized, drum machine, cinematic strings, orchestral, smooth jazz, elevator, lounge, gospel choir, choir, background vocals, humming, soulful (vocal trigger), lo-fi study beats, chillhop, modern clean digital |

## jid-dark lane: dense dark boom-bap/trap hybrid

| Group | Descriptors |
|---|---|
| Genre | dark boom-bap trap hybrid instrumental, soul-sample trap, East Coast meets Southern trap, menacing Atlanta trap instrumental, menacing underground hip hop, dusty soul-sample hip hop, experimental hip hop beat (keep "cinematic" for the strings only, never as a genre tag) |
| Drums & groove | heavy boom-bap snare over trap hats, busy hi-hat work with rapid triplet rolls, rolling hi-hat triplets*, stuttering off-grid hat fills, odd fills and dropped beats, drum switch-ups every eight bars, beat switch halfway through, stop-start drums leaving only the sample, punchy dry kick, hard knocking kick and snare, snare rushes, crunchy sampled drum breaks, half-time pocket, 150 BPM half-time |
| Bass | distorted 808 bass, deep sliding 808 bass*, saturated sub bass, dark heavy low end, 808 slides into the downbeat |
| Harmony & instruments | chopped pitched-down soul sample, pitched sample chops as melody, cinematic string ostinato, dark string swells, dark synth pad (never "choir"), mellotron pad, eerie high synth lead, eerie music-box melody, detuned bells, dark muted piano, detuned piano chops, brass stabs, ominous brass hits, organ sample, reversed sample swells |
| Texture & FX | vinyl crackle, dusty collage-like sampling, tape-stop into the switch, filtered intro that opens up, sample stutter edits, sub drop, DJ rewind, siren-like synth, record scratch stabs, glitch stutters |
| Mood | dark, menacing, gritty, paranoid, intense, ominous, restless, tense, relentless |
| Mix & master | dense mid-heavy saturated mix, lo-fi sample against hi-fi drums, saturated drums, loud dense master, snare cuts through the mix, moderate width with centred drums, wide stereo pads with centred drums, controlled sub, dense but clear |
| Wild pool | polymetric hi-hat pattern, beat switch into double-time, tape-stop chop edits, dissonant string clusters, half-speed breakdown, free-time drum fill section |
| Avoid | cinematic trailer, epic orchestral, film score, choir, gospel choir, vocal chops, drill, UK drill, lo-fi chill, chillhop, boom bap + jazzy alone, EDM, dubstep, bass drop, clean polished pop mix, horrorcore (invites screams) |

## phonk-experimental lane: Memphis-rooted original phonk (not drift phonk)

| Group | Descriptors |
|---|---|
| Genre | Memphis phonk*, experimental phonk instrumental, Memphis underground rap instrumental*, classic phonk, chopped and screwed*, lo-fi Memphis rap beat, SoundCloud-era phonk, jazz-sample phonk, dark lo-fi trap |
| Drums & groove | 808 drum machine kick and snare, boomy long-decay 808 kick, dry 808 snare and clap, 808 cowbell accents riding the off-beats (rhythmic, never melodic), sparse syncopated cowbell pattern, Memphis cowbell pattern, slow swung half-time groove, off-grid sample-loop swing, tape-worn drum loops*, hard drums in lo-fi space*, shuffled closed hats, lazy swung hi-hats, minimal drum programming, crunchy lo-fi snare, 140 BPM half-time slow and heavy |
| Bass | distorted long-decay 808 bass*, distorted 808 sub-bass*, pitched 808 bassline, blown-out sub bass, warm low-passed sub, 808 slides, overdriven bass hits |
| Harmony & instruments | slowed jazz piano sample loop, pitched-down soul sample loop, slowed funk sample, chopped and screwed sample textures used as instruments, pitched-down chopped sample texture with no intelligible words (pair with Exclude lead vocals), eerie minor-key synth pad, horror-movie keys, dark Rhodes loop, dusty vinyl samples*, Memphis samples*, 808 clave bell tone, dark natural-minor or Phrygian riff, two-chord dark loop, dark bell melody |
| Texture & FX | tape hiss, vinyl crackle, cassette wow and flutter, bitcrushed 12-bit sampler grit, low-pass filtered haze, heavy reverb and tape delay on sample stabs, VHS noise floor, record slowdown and tape stop, reversed reverb tails, pitched-down sample stabs as percussion, sparse intro* |
| Mood | dark, eerie, hazy, underground*, gritty*, menacing but slow, woozy, nocturnal, murky, haunted, hypnotic |
| Mix & master | lo-fi*, intentionally murky low-passed mix, hissy wide noise floor, mono-centred drums with hazy wide reverb, tape-saturated master, controlled clean sub under lo-fi texture (the radio-ready twist), polished grit, loud and crunchy master |
| Wild pool | tape-speed wobble, polyrhythmic cowbell, broken-beat drum programming, detuned sample pitch drift, half-speed collapse section, free-time intro |
| Avoid | drift phonk, drift, gym phonk, aggressive phonk, cowbell melody, cowbell lead, Brazilian phonk, Brazilian funk, baile funk, EDM build, drop, sidechain pump, pumping compressed, hard-hitting fast 160 BPM, high energy, driving, house phonk, epic, cinematic, dubstep, rap vocals, spoken samples |

## hybrid lane: phonk drums + neo-soul pocket + trap-pop brightness

Compose from the three lanes above: drums and texture from phonk-experimental (cowbell accents, distorted long-decay 808, tape hiss, off-grid swing), bass and harmony from paak-pocket (fingerstyle bass guitar under the 808, Rhodes with tremolo, minor 9th voicings, clavinet), top layer and mix from connor-price (bright synth plucks, music-box bells, clean punchy master with bright top end). Genre tag: "phonk neo-soul hybrid instrumental" or "lo-fi Memphis drums with live funk pocket". Mood words that bridge: playful but hazy, sunny and dark at once, groovy, nocturnal, head-nod. Avoid: drift phonk, Brazilian funk, EDM build, cowbell lead melody, quantized rigid drums, smooth jazz, cinematic trailer, dubstep.

## Cross-lane radio-ready mix and master words

Use one or two at the end of the Style field: punchy drums, glued cohesive mix, wide stereo image*, clean low end, tight controlled sub, bright clear top end*, well-defined bass*, good instrument separation*, clean production*, loud modern master, polished professional sound*, radio-ready, balanced full-range, crisp transients. Choose one temperature per lane: warm (paak-pocket, phonk-experimental), bright (connor-price), dark/dense (jid-dark), warm-low/bright-top (hybrid).

## Universal avoid list for instrumentals (Exclude field material, never Style)

vocals, lyrics, singing, singer, rap, rapper, spoken word, spoken intro, humming, wordless vocals, ooh-ahh vocals, choir, gospel choir, backing vocals, crowd vocals, chant, call-and-response, singalong hook, vocal hook, ad-libs, vocal chops, cinematic trailer, epic orchestral, film score, EDM, EDM build, festival drop, dubstep, bass drop, piano ballad, acoustic ballad, lullaby, lounge, elevator music, smooth jazz, meditation, ambient drone. "Group energy" words (chant, singalong, call-and-response) are specifically reported to summon voices even in instrumental mode.
