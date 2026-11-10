'use strict';
/**
 * suno-forge guard. Every rule here is a word class that sent a rendered Suno take somewhere
 * the prompt did not intend (listening tests over a few hundred tracks; see
 * reference/grammar-and-guard.md). The guard runs over every finished prompt and, in the test
 * suite, over every string in the grammar, so the vocabulary cannot drift as it grows.
 *
 * Universal rules apply to every lane. A lane may ALLOW specific words it needs (phonk needs
 * "cowbell" and "cassette"; the neo-soul lane needs "funk") and DENY extra words that pull it
 * off course (bright pop must never say "lo-fi"). Exclude-field terms are never guarded: they
 * are the banned words on purpose.
 */

const RULES = [
  {
    name: 'voice',
    why: 'invites gibberish voices, hums and group chants into an instrumental',
    re: /\b(vocal|vocals|vocalist|rap|rapper|rapping|sing|sings|singing|singer|sung|chant\w*|choir|hum|humming|hummed|lyric\w*|ad-?lib\w*|falsetto|harmonies|gospel|crowd|anthem\w*|singalong|sing-along|call[- ]and[- ]response|scat|talk ?box|soulful|oohs?|ahhs?|voices?)\b/i,
  },
  {
    name: 'meter and swing words',
    why: 'made Suno play carnival or jazz-club feels instead of a hip hop pocket; say the slur physically ("the 808 stumbles into three on beat 3")',
    re: /\b(waltz\w*|lilt\w*|limp\w*|oompah|oom-pah|carnival|circus|polka|calliope|swing\w*|swung|shuffl\w*|boom-?chick)\b|\b(3|5|6|7|9|12)\/(4|8)\b/i,
  },
  {
    name: 'transition and filler words',
    why: 'every section change grows a fill, a riser or a fourth-bar roll; say the positive ("hard cuts only, straight in on beat 1")',
    re: /\b(transition\w*|sweeps?|sweeping|risers?|phaser|dj|scratch\w*|fills?|filler|rolls?|rolling|airhorn|build-?ups?|builds?|building|drops?|dropping|impacts?|crash(es)?|reverse cymbals?|tape stop|stutter edit|lead-in|pickup|comes in|coming in|enters?|entering|every other bar|bar 4|4th bar|fourth bar|8th bar|eighth bar|crescendo)\b/i,
  },
  {
    name: 'EDM and film-score magnets',
    why: 'pull the beat toward EDM drops or trailer music',
    re: /\b(edm|dubstep|house|techno|trance|rave|electro|supersaw|sidechain\w*|wobble|reese|festival|arena|big room|cinematic|epic|orchestral|orchestra|film score|trailer|ethereal|lush|shimmer\w*|ambient|atmospheric|timpani|cathedral|church|pipe organ|harps?|harpsichord|violin|viola|cello\w*|pizzicato|bowed|woodwind|fanfare|music box|glockenspiel|celesta|xylophone|marimba|kalimba|ocarina|pan flute|whistl\w*)\b/i,
  },
  {
    name: 'elevator and lounge magnets',
    why: 'pull Suno toward smooth jazz, lounge and elevator music',
    re: /\b(sax\w*|clarinet|flugelhorn|harmon mute|rhodes|wurlitzer|hammond|leslie|smooth|lounge|elevator|new age|meditat\w*|relax\w*|mellow|easy listening|bossa|bebop|big band|ragtime|dixieland|jazzy|jazz|chill|chillhop|study beats?)\b/i,
  },
  {
    name: 'negation',
    why: '"no X" plants X; the Exclude box is the only place a ban works',
    re: /\b(no|without)\s+[a-z]/i,
  },
  {
    name: 'vague stock words',
    why: 'point Suno at the average of thousands of tracks: the preset',
    re: /\b(realistic|complex drums|high-quality|professional|awesome|amazing|beautiful|catchy|vibey|vibes?)\b/i,
  },
];

/** Artist, producer, label and record names. Suno strips or rewrites them and you lose the field. */
const NAMES = [
  'connor price', 'anderson .paak', 'anderson paak', '.paak', 'paak', 'jid', 'j.i.d', 'three 6 mafia', 'three six mafia', 'dj smokey',
  'dj paul', 'juicy j', 'tommy wright', 'dj spanish fly', 'soudiere', 'dj yung vamp', 'kaytranada', 'knxwledge', 'nxworries', 'j dilla',
  'dilla', 'madlib', 'metro boomin', 'kenny beats', 'chasethemoney', 'dreamville', 'pharrell', 'timbaland', 'kanye', 'drake', 'drizzy',
  'kendrick', 'travis scott', 'tyler the creator', 'tyler, the creator', 'thundercat', 'dr. dre', 'dr dre', 'flying lotus', 'mac miller',
  'earl sweatshirt', 'mf doom', 'freddie gibbs', 'denzel curry', 'playboi carti', 'carti', "pi'erre bourne", 'pierre bourne', 'zaytoven',
  'lex luger', 'mike will', 'dj screw', 'silk sonic', 'bruno mars', 'free nationals', 'hi-tek', '9th wonder', 'the alchemist', 'pete rock',
  'dj premier', 'rza', 'mndsgn', 'robert glasper', 'tom misch', 'kreepa', 'dvrst', 'ghostface playa', 'kordhell', 'smokepurpp', 'pink guy',
  'lil ugly mane', 'spaceghostpurrp', 'playa fly', 'gangsta pat', 'lord infamous', 'koopsta knicca', 'eminem', 'jay-z', 'jay z', 'lil wayne',
  'weezy', 'young thug', '21 savage', 'tay keith', 'boi-1da', 'vinylz', 'mannie fresh', 'cardo', 'j cole', 'jcole', 'big sean', 'juice wrld',
  'xxxtentacion', 'nick mira', 'internet money', 'chance the rapper', 'lil durk', 'lil peep', 'ski mask', 'kodak black', 'don toliver',
  'mike dean', 'wondagurl', 'yeat', 'ken carson', 'nba youngboy', 'opium records', 'bigxthaplug', 'finesse2tymes', 'cash cobain',
  'daniel caesar', 'giveon', 'quavo', 'the kid laroi', 'finneas', 'benny blanco', 'skrillex', 'jon bellion', 'smino', 'noname',
  'lil baby', 'gunna', 'wheezy', 'southside', 'tm88', 'murda beatz', 'hit-boy', 'boi1da', 'dj mustard', 'mustard', 'kaytra', 'glorilla',
  'spin the globe', 'malibu', 'oxnard', 'ventura', 'the forever story', 'dicaprio 2', 'god does like ugly', 'mask off', 'old town road',
  'sicko mode', "god's plan", 'not like us', 'lucid dreams', 'type beat', 'in the style of', 'inspired by',
];

const esc = (s) => s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
const NAMES_RE = new RegExp(`(^|[^a-z0-9])(${NAMES.map(esc).join('|')})(?=$|[^a-z0-9])`, 'i');

/**
 * Check a string. Returns [] when clean, else [{rule, word, why}]. `allow` is a RegExp of words
 * the lane may use despite the universal rules; `deny` is an extra lane rule {name, why, re}.
 */
function check(text, allow, deny) {
  const hits = [];
  if (!text) return hits;
  const scrubbed = allow ? text.replace(new RegExp(allow.source, 'gi'), ' ') : text;
  for (const rule of RULES) {
    const m = scrubbed.match(rule.re);
    if (m) hits.push({ rule: rule.name, word: m[0], why: rule.why });
  }
  if (deny) {
    const m = scrubbed.match(deny.re);
    if (m) hits.push({ rule: deny.name, word: m[0], why: deny.why });
  }
  const n = text.match(NAMES_RE);
  if (n) hits.push({ rule: 'names', word: n[2], why: 'Suno rewrites prompts that name artists, records or labels, and you lose the whole field' });
  const cmp = text.match(/\b(sounds? like|style of|like)\s+[A-Z][a-z]+(\s+[A-Z][a-z.]+)?/);
  if (cmp && !/^(sounds? like|like)\s+(Suno|BPM)/.test(cmp[0])) hits.push({ rule: 'names', word: cmp[0], why: 'a comparison to a named artist or record; Suno rewrites it' });
  return hits;
}

/** Phrases that teach Suno to change something every four bars. Capped at one per roll, none in loop mode. */
const EVERY = /every (\d+\w*|other|third|second) (bar|bars|pass|4|8)|\bbar \d+( and bar \d+)?|partway/i;

module.exports = { RULES, NAMES, NAMES_RE, check, EVERY };
