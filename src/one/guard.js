/* ONE OF ONE — guard rails learned from listening tests. Every word here
 * sent a real Suno take somewhere the artist did not want it to go. */
"use strict";

const OneGuard = (() => {
  const RULES = [
    {
      name: "film score / world / folk / lo-fi",
      why: "turned beats into film-score background music or low-fidelity sound design",
      re: /\b(koto|sitar|tabla|bagpipe\w*|accordion|harpsichord|flamenco|steel[- ]?(pan|drum)|banjo|whistl\w*|pan flute|ocarina|duduk|cello\w*|viola|violin|pizzicato|harps?|bowed|timpani|cathedral|church|pipe[- ]organ|orchestral(?! brass)|cinematic|film|horror|haunt\w*|eerie|ghostly|spaghetti|western|mellotron|woodwind|cassette|vinyl|crackl\w*|transistor|sewer|storm drain|basement|boiler|tunnel|subway|overpass|dripping|manhole|dumpster|nylon|slide[- ]guitar|upright|double bass|fanfare|marimba|kalimba|music box|glockenspiel|celesta|xylophone)\b/i,
    },
    {
      name: "meter",
      why: "made Suno play oom-pah carnival music",
      re: /\b(waltz\w*|lilt\w*|limp\w*|oompah|oom-pah|carnival|circus|polka|calliope)\b|\b(3|5|6|7|9|12)\/(4|8)\b/i,
    },
    {
      name: "voice",
      why: "invites gibberish background voices into an instrumental",
      re: /\b(vocal|vocals|vocalist|rap\b|rapper|sing|sings|singing|singer|sung|chant\w*|choir|hum|humming|hummed|lyric\w*|ad-?lib\w*|falsetto|harmonies)\b/i,
    },
    {
      name: "DJ / transition effects",
      why: "produced phaser sweeps, risers and scratches every few bars",
      re: /\b(transition\w*|sweeps?|risers?|phaser|dj\b|scratch\w*|fills?|cowbell|rim ?shots?|airhorn)\b/i,
    },
    {
      name: "off-format genre",
      why: "pulls the beat out of hip hop",
      re: /\b(edm|dubstep|jazz\w*|funk\w*|disco|house\b|polka|country|bluegrass)\b/i,
    },
    {
      name: "EDM / voice / lo-fi trigger",
      why: "research: these words pull in EDM drops, crowd voices or lo-fi hiss",
      re: /\b(drops?|build-?ups?|sidechain\w*|wobble|growl|reese|tape|tape-\w+|chop\w*|stutter edit|beat repeat|dusty|lo-?fi|chill|retro|vintage|soulful|ambient|atmospheric|ethereal|epic|anthem\w*|festival|gospel|arena|crowd|singalong|memphis|phonk|voices?|bells?|strings|string section|pads?|glitch)\b/i,
    },
    {
      name: "genre drift",
      why: "pulls Suno toward jazz, blues, EDM, techno or elevator music",
      re: /\b(sax\w*|clarinet|flugelhorn|harmon mute|rips?|fall-offs?|rhodes|wurlitzer|electric piano|hammond|leslie|saloon|ragtime|dixieland|big band|marching band|brass band|swing\w*|swung|bebop|blues\w*|wail\w*|twang\w*|spring reverb|pentatonic|phrygian|flat-five|ninths?|bossa|latin|reggae|ska|dub|guiro|frame drum|acid|squelch\w*|supersaw|laser|chiptune|8-bit|game-console|theremin|ping-pong|ramp\w*|builds?|building|techno|trance|rave|electro|synthwave|retrowave|smooth|lounge|elevator|new age|meditat\w*|relax\w*|mellow|lush|ghost notes|breakbeat|groovy|swamp\w*|easy listening)\b/i,
    },
    {
      name: "genre drift (spin-offs)",
      why: "pulls the spin-off genres toward tropical house, worship music, indie rock or old-school pastiche",
      re: /\b(tropical house|moombahton|reggaeton|afrobeats?|dancehall|dembow|ballad|dream pop|synth-?pop|bedroom pop|future bass|steel drums?|marimba|ukulele|summer|beach|island|worship|praise|hymn|sermon|preach\w*|testimony|amen|spiritual|clap-along|indie|singer-songwriter|shoegaze|grunge|pop punk|screamo|soundcloud|nostalgic|wholesome|uplifting|turntabl\w*|boogie|g-funk|talk ?box|wah(-wah)?|clavinet|slap bass|walking bass|golden era|90s|nineties|old[- ]school|east coast|trip hop|beat tape|jazz-hop|gospel|church)\b/i,
    },
    {
      name: "negation",
      why: "\"no X\" plants X",
      re: /\bno\s+\w+/i,
    },
  ];

  // Reference names live in the UI only. Suno strips or rewrites them, and
  // a wrong name in a prompt can drag in the wrong artist's sound.
  const NAMES = [
    "jid", "j.i.d", "big sean", "sean don", "lil wayne", "weezy", "wayne", "j. cole", "j cole", "jcole", "cole",
    "prof", "gampo", "connor price", "drake", "drizzy", "kendrick", "travis scott", "future", "metro boomin",
    "a milli", "blessings", "mcafee", "bruddanem", "glory", "lil durk", "durk", "bangladesh", "vinylz",
    "boi-1da", "lex luger", "dj khalil", "timbaland", "pi'erre bourne", "kenny beats", "tay keith", "mannie fresh",
    "three 6 mafia", "dj paul", "cardo", "dreamville",
    "bieber", "justin bieber", "xxxtentacion", "xxx", "jahseh", "chance the rapper", "chancelor", "cocoa butter kisses",
    "acid rap", "bigxthaplug", "big x tha plug", "big x", "poo bear", "skrillex", "benny blanco", "mmhmm", "the largest",
    "t.i.", "bigx", "nico segal", "donnie trumpet", "social experiment",
    "bloodpop", "finneas", "andrew watt", "jon bellion", "the kid laroi", "daniel caesar", "giveon", "quavo", "peaches",
    "xxx tentacion", "tentacion", "onfroy", "potsu", "shiloh dynasty", "john cunningham", "nick mira", "taz taylor",
    "internet money", "ski mask", "juice wrld", "lil peep", "jocelyn flores", "chano", "lil chano", "peter cottontale",
    "cottontale", "nate fox", "cam o'bi", "vic mensa", "twista", "coloring book", "noname", "smino",
    "tha plug", "bandplay", "tony coles", "finesse2tymes", "the whispers", "and the beat goes on", "9th wonder",
    "pete rock", "dj premier", "alchemist", "madlib", "dilla", "j dilla", "just blaze", "kanye",
  ];
  const esc = (s) => s.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
  const NAME_RE = new RegExp(`(^|[^a-z0-9])(${NAMES.map(esc).join("|")})($|[^a-z0-9])`, "i");

  // `allow` is a genre's own unlock: a RegExp of banned words that genre needs
  // (country needs "twang", "banjo", "pedal steel"). They are blanked before
  // the rules run, so everything else in the text is still checked.
  function check(text, allow) {
    if (allow) text = text.replace(new RegExp(allow.source, "gi"), " ");
    const hits = [];
    for (const r of RULES) {
      const m = text.match(r.re);
      if (m) hits.push({ rule: r.name, word: m[0].trim(), why: r.why });
    }
    const n = text.match(NAME_RE);
    if (n) hits.push({ rule: "reference name", word: n[2], why: "names never go in the prompt" });
    return hits;
  }

  return { RULES, NAMES, check };
})();

if (typeof module !== "undefined") module.exports = OneGuard;
