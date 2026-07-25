import type { ArtistDNA } from "./types.ts";

/**
 * Artist-DNA descriptor library — the core of CIPHER.
 *
 * Suno blocks artist names (research §3.4: moderation category; hard block or
 * silent substitution), so each entry translates an artist reference into the
 * documented substitute: subgenre + era/region + production descriptors.
 * Descriptor vocabulary is grounded in research §3.3 (trap formula, phonk
 * vocabulary, emo-rap vocabulary, BPM anchors) wherever a documented phrase
 * exists; the rest is producer-domain language to be tuned in Phase 3 against
 * real audio.
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
    mood: ["raw aggressive-vulnerable energy", "brooding melodic pain"],
    groove: "forward-aggressive",
    instrumentation: [
      "distorted 808s",
      "lo-fi crunch",
      "sparse detuned guitar loop",
      "tape-saturated drums",
    ],
    vocal: ["pained melodic rap", "raw screamed vocal doubles"],
    texture: ["bedroom lo-fi grit", "blown-out mix"],
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
    mood: ["heartbroken", "melancholic euphoria"],
    groove: "behind-beat-pocket",
    instrumentation: [
      "clean electric guitar riff loop",
      "heavy 808s",
      "airy reverb-soaked pads",
    ],
    vocal: ["auto-tuned melodic vocals", "effortless freestyle sing-rap cadence"],
    texture: ["polished modern trap production"],
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
    mood: ["playful menace", "hungry underdog energy"],
    groove: "displaced-anti-grid",
    instrumentation: ["punchy snappy drums", "warm rubbery bassline", "jazzy chopped samples"],
    vocal: [
      "agile technical rap flow",
      "elastic pitch-bending delivery",
      "dense multisyllabic rhyme chains",
    ],
    texture: ["crisp modern southern mix"],
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
    mood: ["reflective", "warm sincerity"],
    groove: "behind-beat-pocket",
    instrumentation: ["soulful sample chops", "dusty live drums", "mellow electric keys"],
    vocal: ["conversational storytelling flow", "understated double-time runs"],
    texture: ["warm analog warmth", "90s-leaning boom bap polish"],
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
    genres: ["soul-sample hip hop", "maximalist rap"],
    mood: ["triumphant", "grandiose"],
    groove: "behind-beat-pocket",
    instrumentation: [
      "pitched-up chipmunk soul samples",
      "gospel choir stabs",
      "punchy chopped drums",
    ],
    vocal: ["confident chant-like rap delivery", "sung-rap hooks"],
    texture: ["orchestral maximalist production", "wide cinematic mix"],
    laneGuards: ["edm drop", "country"],
    bpm: [85, 105],
    adlibs: ["uh", "hah", "yeah"],
    lyricNotes: ["big declarative hook lines built for crowd shout-back"],
  },
  {
    id: "jay-z",
    displayName: "Jay-Z",
    aliases: ["jayz", "hov", "hova"],
    genres: ["east coast hip hop", "boom bap"],
    mood: ["effortless confidence", "executive cool"],
    groove: "behind-beat-pocket",
    instrumentation: ["chopped soul samples", "hard knocking boom bap drums", "deep round bass"],
    vocal: ["laid-back conversational flow", "precise internal rhyme placement"],
    texture: ["classic New York polish"],
    laneGuards: ["edm drop", "country"],
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
    mood: ["gritty", "menacing street authority"],
    groove: "behind-beat-pocket",
    instrumentation: ["dark minor piano loop", "hard-hitting boom bap drums", "eerie string stabs"],
    vocal: ["raspy gravelly punchline delivery", "measured menacing pace"],
    texture: ["grimy 2000s New York grit"],
    laneGuards: ["edm drop", "country"],
    bpm: [88, 98],
    adlibs: ["ha", "uh-huh"],
    lyricNotes: ["one hard punchline per two bars, delivered dry with a pause after"],
  },
  {
    id: "outkast",
    displayName: "OutKast",
    aliases: ["andre 3000", "big boi"],
    genres: ["southern hip hop", "psychedelic funk rap"],
    mood: ["playful cosmic energy", "eccentric soul"],
    groove: "displaced-anti-grid",
    grooveExtras: ["bouncing southern swing"],
    instrumentation: ["funky live bass grooves", "warm analog synth leads", "live horn section"],
    vocal: ["double-time southern bounce flow", "eccentric melodic hooks"],
    texture: ["psychedelic funk warmth", "vintage analog character"],
    laneGuards: ["edm drop", "country"],
    bpm: [95, 160],
    lyricNotes: ["alternate double-time bounce verses with sung playful hooks"],
  },
  {
    id: "travis-scott",
    displayName: "Travis Scott",
    aliases: ["travis", "la flame", "cactus jack"],
    genres: ["psychedelic trap", "rage trap"],
    mood: ["dark euphoria", "hypnotic nocturnal energy"],
    groove: "behind-beat-pocket",
    grooveExtras: ["cavernous half-time feel"],
    instrumentation: ["rage synths", "booming 808 glides", "cavernous reverb tails"],
    vocal: ["layered autotuned harmonies", "ad-lib heavy delivery", "pitched vocal chops"],
    texture: ["psychedelic haze", "arena-scale modern mix"],
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
    mood: ["unshakable swagger", "confident bounce"],
    groove: "behind-beat-pocket",
    grooveExtras: ["rubber-band southern bounce"],
    instrumentation: ["bouncy synth brass stabs", "crisp trap hi-hat rolls", "808 knock"],
    vocal: ["smooth commanding southern drawl", "effortless swagger delivery"],
    texture: ["clean 2000s Atlanta trap sheen"],
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
    mood: ["earnest", "bittersweet sunshine"],
    groove: "forward-aggressive",
    grooveExtras: ["driving straight-eighths rock rhythm"],
    instrumentation: [
      "crunchy power chord guitar wall",
      "fuzzy melodic guitar leads",
      "live rock drums",
    ],
    vocal: ["earnest clean male vocals", "big singalong harmony stacks"],
    texture: ["90s alt-rock warmth", "garage-polished crunch"],
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
