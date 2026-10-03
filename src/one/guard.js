/* ONE OF ONE — guard rails learned from listening tests. Every word here
 * sent a real Suno take somewhere the artist did not want it to go. */
"use strict";

const OneGuard = (() => {
  const RULES = [
    {
      name: "film score / world / folk / lo-fi",
      why: "turned beats into film-score background music or low-fidelity sound design",
      re: /\b(erhu|koto|sitar|tabla|bagpipe\w*|accordion|harpsichord|flamenco|steel[- ]?(pan|drum)|banjo|tuba\w*|whistl\w*|pan flute|ocarina|duduk|cello\w*|viola|violin|pizzicato|harps?|bowed|harmonica\w*|timpani|cathedral|church|pipe[- ]organ|organs?|orchestral|cinematic|film|horror|haunt\w*|eerie|ghostly|spaghetti|western|mellotron|flutes?|woodwind|reed|cassette|vinyl|crackl\w*|transistor|sewer|storm drain|basement|boiler|tunnel|subway|overpass|dripping|manhole|dumpster|nylon|slide[- ]guitar|upright|double bass|fanfare|marching|military|marimba|kalimba|music box|glockenspiel|celesta|xylophone)\b/i,
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
  ];
  const esc = (s) => s.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
  const NAME_RE = new RegExp(`(^|[^a-z0-9])(${NAMES.map(esc).join("|")})($|[^a-z0-9])`, "i");

  function check(text) {
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
