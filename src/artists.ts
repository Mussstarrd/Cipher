import type { ArtistDNA } from "./types.ts";

/**
 * Artist-DNA descriptor library — the core of CIPHER.
 *
 * Suno blocks artist names (research §3.4: moderation category; hard block or
 * silent substitution), so each entry translates an artist reference into the
 * documented substitute: subgenre + era/region + production descriptors.
 * Descriptor vocabulary is grounded in research §3.3 (trap formula, phonk
 * vocabulary, emo-rap vocabulary, BPM anchors) wherever a documented phrase
 * exists; the rest is producer-domain language tuned in Phase 3 against real
 * audio.
 *
 * Slot arrays are POOLS: the engine picks a seeded subset per build, so the
 * same DNA selection produces a different prompt with the same vibe on each
 * reroll. Index 0 of `instrumentation` and `vocal` is the always-emitted
 * signature.
 *
 * Phase 3 tuning log:
 * - jay-z: "boom bap" + "chopped soul samples" leads pulled Suno into generic
 *   jazzy lo-fi territory (owner feedback) → rebuilt around hard 2000s NYC
 *   language, smooth-jazz/chillhop lane guards.
 * - kanye-west: "pitched-up chipmunk soul samples" as lead descriptor read as
 *   cartoonish (owner feedback) → orchestral/gospel-led pool, cartoon-vocal
 *   lane guard.
 * - ugk: added; lane guards deliberately do NOT exclude country — the
 *   country-fried funk color is the identity.
 * - funk-attractor rule (owner concern): "funk" never appears in genre slots
 *   anywhere in the library — genre tokens are front-loaded and can flip a
 *   track into an actual funk band. Funk color is instrument/texture-level
 *   only, vocal signatures say "rap" explicitly, and ugk/outkast guard
 *   "disco" in the exclude field.
 *
 * displayName/aliases are for input matching and leak-scanning ONLY — they
 * must never reach a Suno-bound field.
 */
export const ARTISTS: ArtistDNA[] = [
  {
    id: "xxxtentacion",
    displayName: "XXXTentacion",
    aliases: ["xxx", "x", "jahseh"],
    genres: ["emo rap", "distorted lo-fi trap"],
    mood: [
      "raw aggressive-vulnerable energy",
      "brooding melodic pain",
      "explosive anguish",
      "numb late-night despair",
    ],
    groove: "forward-aggressive",
    instrumentation: [
      "distorted 808s",
      "lo-fi crunch",
      "sparse detuned guitar loop",
      "tape-saturated drums",
      "grainy vinyl hiss",
      "sub-heavy knock",
    ],
    vocal: [
      "pained melodic rap",
      "raw screamed vocal doubles",
      "whisper-to-scream dynamics",
      "unpolished emotional delivery",
    ],
    texture: ["bedroom lo-fi grit", "blown-out mix", "cassette-degraded warmth"],
    laneGuards: ["edm drop", "country"],
    bpm: [130, 160],
    adlibs: ["yeah", "what", "ay"],
    lyricNotes: [
      "short punchy bars, heavy repetition, sudden dynamic swings between whispered and shouted lines",
    ],
  },
  {
    id: "juice-wrld",
    displayName: "Juice WRLD",
    aliases: ["juice", "juicewrld", "999"],
    genres: ["melodic emo rap", "melodic trap"],
    mood: ["heartbroken", "melancholic euphoria", "bittersweet nostalgia", "lonely arena glow"],
    groove: "behind-beat-pocket",
    instrumentation: [
      "clean electric guitar riff loop",
      "heavy 808s",
      "airy reverb-soaked pads",
      "plucky emo arpeggios",
      "glassy bell melodies",
      "rolling hi-hats",
    ],
    vocal: [
      "auto-tuned melodic vocals",
      "effortless freestyle sing-rap cadence",
      "aching sustained hook notes",
    ],
    texture: [
      "polished modern trap production",
      "radio-ready emo trap sheen",
      "spacious stadium reverb",
    ],
    laneGuards: ["edm drop", "country"],
    bpm: [140, 165],
    adlibs: ["oh", "yeah yeah", "woah"],
    lyricNotes: [
      "long melodic phrases that land slightly behind the beat, confessional heartbreak imagery",
    ],
  },
  {
    id: "jid",
    displayName: "JID",
    aliases: ["j.i.d", "j.i.d."],
    genres: ["southern lyrical hip hop"],
    mood: ["playful menace", "hungry underdog energy", "wired focus", "sly humor"],
    groove: "displaced-anti-grid",
    instrumentation: [
      "punchy snappy drums",
      "warm rubbery bassline",
      "jazzy chopped samples",
      "dusty horn stabs",
      "syncopated percussion layers",
      "moody minor keys",
    ],
    vocal: [
      "agile technical rap flow",
      "elastic pitch-bending delivery",
      "dense multisyllabic rhyme chains",
      "sudden falsetto breaks",
    ],
    texture: ["crisp modern southern mix", "tight dry drum room", "warm analog undertone"],
    laneGuards: ["edm drop", "country"],
    bpm: [88, 150],
    adlibs: ["woo", "ay", "uh"],
    lyricNotes: [
      "pack 3+ syllables per beat in bursts, then drop to sparse pockets; internal rhyme on nearly every bar",
    ],
  },
  {
    id: "j-cole",
    displayName: "J. Cole",
    aliases: ["jcole", "cole", "dreamville"],
    genres: ["conscious hip hop", "soulful boom bap"],
    mood: ["reflective", "warm sincerity", "quiet determination", "hometown nostalgia"],
    groove: "behind-beat-pocket",
    instrumentation: [
      "soulful sample chops",
      "dusty live drums",
      "mellow electric keys",
      "muted trumpet lines",
      "warm upright bass",
      "soft vinyl crackle",
    ],
    vocal: [
      "conversational storytelling flow",
      "understated double-time runs",
      "weary earnest delivery",
    ],
    texture: [
      "warm analog warmth",
      "90s-leaning boom bap polish",
      "intimate late-night studio feel",
    ],
    laneGuards: ["edm drop", "country"],
    bpm: [85, 100],
    lyricNotes: [
      "narrative verses with a clear arc; save the double-time run for the final 4 bars of a verse",
    ],
  },
  {
    id: "kanye-west",
    displayName: "Kanye West",
    aliases: ["kanye", "ye", "yeezy"],
    genres: ["soul-sample hip hop", "orchestral rap"],
    mood: ["triumphant", "hungry underdog fire", "grandiose", "defiant celebration"],
    groove: "behind-beat-pocket",
    instrumentation: [
      "dramatic orchestral stabs",
      "soulful vocal sample chops",
      "gospel choir swells",
      "marching-band brass",
      "hard compressed drum breaks",
      "warm Rhodes keys",
    ],
    vocal: ["confident chant-like rap delivery", "hyped shout-back hooks", "half-sung bravado"],
    texture: ["widescreen cinematic mix", "polished grit", "stadium-scale low end"],
    laneGuards: ["high-pitched cartoon vocals", "edm drop"],
    bpm: [85, 105],
    adlibs: ["uh", "hah", "yeah"],
    lyricNotes: ["big declarative hook lines built for crowd shout-back"],
  },
  {
    id: "jay-z",
    displayName: "Jay-Z",
    aliases: ["jayz", "hov", "hova"],
    genres: ["gritty New York rap", "2000s East Coast hip hop"],
    mood: [
      "effortless kingpin confidence",
      "executive cool",
      "victory-lap swagger",
      "cold ambition",
    ],
    groove: "behind-beat-pocket",
    instrumentation: [
      "hard-knocking chopped soul beat",
      "triumphant horn stabs",
      "dramatic string flourishes",
      "heavy swung drum breaks",
      "deep subway-rumble bass",
      "gritty city-noise texture",
    ],
    vocal: [
      "laid-back commanding rap flow",
      "precise conversational delivery",
      "smirking punchline emphasis",
    ],
    texture: ["big-budget 2000s New York polish", "street-luxury sheen", "concrete-hard drum sound"],
    laneGuards: ["smooth jazz", "lo-fi chillhop", "edm drop"],
    bpm: [85, 100],
    lyricNotes: [
      "conversational cadence that never sounds rushed; punchlines land on beat 4",
    ],
  },
  {
    id: "jadakiss",
    displayName: "Jadakiss",
    aliases: ["jada", "kiss"],
    genres: ["hardcore east coast rap"],
    mood: ["gritty", "menacing street authority", "cold-blooded calm", "war-ready tension"],
    groove: "behind-beat-pocket",
    instrumentation: [
      "dark minor piano loop",
      "hard-hitting boom bap drums",
      "eerie string stabs",
      "ominous bell tolls",
      "grimy bass stabs",
      "scratched vocal cuts",
    ],
    vocal: [
      "raspy gravelly punchline delivery",
      "measured menacing pace",
      "smoky growled emphasis",
    ],
    texture: ["grimy 2000s New York grit", "concrete-hard drum sound", "dark alley atmosphere"],
    laneGuards: ["smooth jazz", "edm drop"],
    bpm: [88, 98],
    adlibs: ["ha", "uh-huh"],
    lyricNotes: ["one hard punchline per two bars, delivered dry with a pause after"],
  },
  {
    id: "outkast",
    displayName: "OutKast",
    aliases: ["andre 3000", "big boi"],
    // Same funk-attractor rule as ugk: no "funk" in genre slots; the funk
    // color comes from instrumentation/texture.
    genres: ["southern hip hop", "eccentric Dirty South rap"],
    mood: [
      "playful cosmic energy",
      "eccentric soul",
      "funky optimism",
      "space-age southern pride",
    ],
    groove: "displaced-anti-grid",
    grooveExtras: ["bouncing southern swing"],
    instrumentation: [
      "funky live bass grooves",
      "warm analog synth leads",
      "live horn section",
      "clavinet funk riffs",
      "psychedelic guitar textures",
      "talkbox accents",
    ],
    vocal: [
      "double-time southern rap bounce",
      "eccentric melodic hooks",
      "smooth crooned interludes",
    ],
    texture: ["psychedelic funk warmth", "vintage analog character", "technicolor stereo spread"],
    laneGuards: ["edm drop", "disco"],
    bpm: [95, 160],
    lyricNotes: ["alternate double-time bounce verses with sung playful hooks"],
  },
  {
    id: "ugk",
    displayName: "UGK",
    aliases: ["bun b", "pimp c", "underground kingz", "underground kings"],
    // "funk" stays OUT of the genre slots (front-loaded genre tokens are the
    // strongest attractors — a funk genre anchor can flip the whole track into
    // a 70s funk band). Funk color lives at instrument level only.
    genres: ["Texas southern rap", "90s Houston player rap"],
    mood: [
      "slab-cruising player cool",
      "sweltering Gulf Coast soul",
      "smoked-out confidence",
      "trunk-rattling menace",
    ],
    groove: "behind-beat-pocket",
    grooveExtras: ["slow-rolling candy-paint bounce", "chopped-and-screwed drag"],
    instrumentation: [
      "greasy funk bassline",
      "wah-wah guitar licks",
      "church organ swells",
      "live-feel drums with 808 knock",
      "pitched-down chopped vocal hook",
      "sunburnt soul horns",
    ],
    vocal: [
      "heavyweight Texas rap drawl",
      "silky player rap delivery trading with a gruff commanding bark",
      "double-time country-boy rap flow",
    ],
    texture: [
      "sweltering analog southern soul",
      "candy-paint trunk rattle",
      "country-fried soul warmth",
      "90s Houston warmth",
    ],
    // No "country" guard: the country-fried color IS this DNA's identity.
    // "disco" guards against the funk-instrumentation pulling a funk-band takeover.
    laneGuards: ["edm drop", "disco"],
    bpm: [80, 105],
    adlibs: ["yeah", "uh", "already"],
    lyricNotes: ["slow confident drawl in the pocket; let bars breathe over the funk"],
  },
  {
    id: "travis-scott",
    displayName: "Travis Scott",
    aliases: ["travis", "la flame", "cactus jack"],
    genres: ["psychedelic trap", "rage trap"],
    mood: [
      "dark euphoria",
      "hypnotic nocturnal energy",
      "narcotic haze",
      "festival-night adrenaline",
    ],
    groove: "behind-beat-pocket",
    grooveExtras: ["cavernous half-time feel"],
    instrumentation: [
      "rage synths",
      "booming 808 glides",
      "cavernous reverb tails",
      "detuned synth wails",
      "sparse icy keys",
      "filtered vocal chops",
    ],
    vocal: [
      "layered autotuned harmonies",
      "ad-lib heavy delivery",
      "pitched vocal chops",
      "hypnotic repeated phrases",
    ],
    texture: ["psychedelic haze", "arena-scale modern mix", "pitch-black low end"],
    laneGuards: ["edm drop", "country"],
    bpm: [130, 155],
    adlibs: ["yeah", "straight up", "alright"],
    lyricNotes: [
      "short hypnotic phrases with heavy ad-lib echoes in parentheses after nearly every line",
    ],
  },
  {
    id: "ti",
    displayName: "T.I.",
    aliases: ["t.i.", "tip", "king of the south"],
    genres: ["southern trap", "Atlanta rap"],
    mood: [
      "unshakable swagger",
      "confident bounce",
      "king-of-the-city poise",
      "celebratory menace",
    ],
    groove: "behind-beat-pocket",
    grooveExtras: ["rubber-band southern bounce"],
    instrumentation: [
      "bouncy synth brass stabs",
      "crisp trap hi-hat rolls",
      "808 knock",
      "plucked synth melodies",
      "marching snare rolls",
      "whistling synth leads",
    ],
    vocal: [
      "smooth commanding southern drawl",
      "effortless swagger delivery",
      "crisp double-time bursts",
    ],
    texture: ["clean 2000s Atlanta trap sheen", "big glossy radio mix", "trunk-knock low end"],
    laneGuards: ["edm drop", "country"],
    bpm: [120, 150],
    adlibs: ["ay", "okay", "yeah"],
    lyricNotes: ["even-keeled confident cadence; never strained, always in control"],
  },
  {
    id: "weezer",
    displayName: "Weezer",
    aliases: ["rivers cuomo"],
    genres: ["alt-rock", "power pop"],
    mood: [
      "earnest",
      "bittersweet sunshine",
      "geeky heart-on-sleeve joy",
      "garage-band abandon",
    ],
    groove: "forward-aggressive",
    grooveExtras: ["driving straight-eighths rock rhythm"],
    instrumentation: [
      "crunchy power chord guitar wall",
      "fuzzy melodic guitar leads",
      "live rock drums",
      "chugging palm-muted riffs",
      "bright chorus-pedal shimmer",
      "melodic bass runs",
    ],
    vocal: ["earnest clean male vocals", "big singalong harmony stacks", "deadpan verse delivery"],
    texture: ["90s alt-rock warmth", "garage-polished crunch", "sun-faded tape warmth"],
    laneGuards: ["edm drop", "country"],
    bpm: [100, 140],
    lyricNotes: ["simple heartfelt hook lines built for group singalong"],
  },
];

const byId = new Map<string, ArtistDNA>();
const byAlias = new Map<string, ArtistDNA>();
for (const a of ARTISTS) {
  byId.set(a.id, a);
  byAlias.set(normalize(a.displayName), a);
  for (const alias of a.aliases) byAlias.set(normalize(alias), a);
}

function normalize(s: string): string {
  return s.toLowerCase().replace(/[^a-z0-9]/g, "");
}

/** Resolve an artist by id, display name, or alias. */
export function resolveArtist(ref: string): ArtistDNA | undefined {
  return byId.get(ref) ?? byAlias.get(normalize(ref));
}

/** Every name/alias string that must never appear in Suno-bound output. */
export function blockedNameTokens(): string[] {
  const tokens: string[] = [];
  for (const a of ARTISTS) {
    tokens.push(a.displayName, ...a.aliases);
  }
  return tokens;
}
