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
      name: "genre drift (country)",
      why: "pulls country trap toward bluegrass, honky-tonk, novelty or bro-country cliches",
      re: /\b(chicken pickin\w*|nashville sound|southern rock|boot-scoot\w*|square dance|bro-country|honky-tonk|line dance|honky tonk|rockabilly|barn dance|honkytonk|americana|bluegrass|dirt road|hillbilly|moonshine|red dirt|mariachi|two-step|tailgate|campfire|jamboree|yodel\w*|hayride|tex-mex|yee haw|yee-haw|folk\w*|cowgirl|stadium|whiskey|hoedown|zydeco|cowboy|yeehaw|outlaw|rodeo|truck|cajun|beer)\b/i,
    },
    {
      name: "genre drift (Prof)",
      why: "critics describe this catalog as circus, pirate or novelty music, and Suno plays those words literally",
      re: /\b(clown\w*|saloon|pirate\w*|sea shanty|shanty|nautical|sailor\w*|surf|kazoo|novelty|comedy|comedic|goofy|bar band|pub rock)\b/i,
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
    "ragal ironbull", "valhalla calling", "wardruna", "heilung", "danheim", "einar selvik", "amon amarth",
    "jadakiss", "the lox", "styles p", "sheek louch", "d-block", "ruff ryders", "swizz beatz", "jermaine cole", "forest hills drive", "the never story", "dicaprio 2", "earthgang", "spillage village",
    "42 dugg", "antt beatz", "baby tron", "babyface ray", "babyfxce", "babytron", "bfb da packman", "big sean. never add bare helluva", "danny brown", "detroit lions", "dugg-less words like dug", "eminem", "enrgy beats", "grizzly", "helluva beats", "icewear vezzo", "jay bezzy", "kash doll", "krispylife kidd", "louie ray", "or coochie: they match ordinary words or slang.", "packman", "payroll giovanni", "ray", "rio da yung og", "rmc mike", "sada baby", "scam likely", "shittyboyz", "tee grizzley", "yn jay",
    "2 chainz", "84 caprice", "a crawl through a low tide", "aesop rock", "ahoy", "animal patrol", "apeshit", "atmosphere", "bar breaker", "beatchefs", "big chocolate", "borderline", "bow legged", "bow-legged", "brian eisner", "brother ali", "cashinova", "charley cooks", "chickenbone", "chris karns", "cory grindberg", "cozz", "creek boy", "curtiss king", "devils gate", "dillon parker", "dj absolute", "dj fundo", "dj two stacks", "dj vajra", "e-40", "fifth element", "fighter or action: they match ordinary words.", "flower boy", "frankie teardrop", "fundo", "garden of eden", "ghosttown", "good time boy", "high priced shoes", "jacob anderson", "jake anderson", "jewelry duty", "jonray", "jordy bangs", "kaiser von powderhorn", "kevin gates", "kia boy", "king gampo", "kyle c-e", "liability", "mac irv", "markmadethis", "method man", "mike campbell", "muja messiah", "mux mool", "noearth", "pack a lunch", "pete & bas", "pookie baby", "powderhorn", "powderhorn suites", "project gampo", "rahzwell", "reaching for fire", "recession music", "redman", "rhymesayers", "sauce walka", "say no", "scoop", "send me up", "squad goals", "stophouse", "t-pain", "taylor j. never add bare ant", "that mexican ot", "time bomb", "tough boy", "vanguard pow", "waka flocka", "willie clayton", "willie wonka",
    "a bar song", "amen (already in the guard) or roll: they match ordinary words.", "austin nivarel", "austin post", "bailey zimmerman", "beautifully broken", "big x", "big x tha plug", "breland", "charley cooks", "charlie handsome", "collins chibueze", "crowell", "danny majic", "f-1 trillion", "gabe simon", "good news", "high road", "i had some help", "jason deford", "jelly roll", "jessie murph", "koe wetzel", "louis bell", "morgan wallen", "need a favor", "nevin sastry", "nivarel", "post malone", "sastry", "save me", "sean cook", "shaboozey", "tony coles", "wallen", "where i've been. never add bare liar", "whitsitt chapel", "zach crowell",
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
