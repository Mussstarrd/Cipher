/* ONE OF ONE — radio hit lyric writer. Hip hop and R&B radio, 2024-2026.
 *
 * Writes a full vocal song: a quotable hook (title drop on line 1, title
 * callback on line 4, one rhyme family per hook, 4-9 syllables a line),
 * verses from couplet templates with rhyme families (8-12 syllables a line,
 * one internal rhyme per four bars, one turn line, a hook callback in verse
 * 2), a rising pre-hook, a sparse bridge, an outro, and section tags that
 * carry performance direction, at most three cues each:
 *   [Hook | half-sung, doubled, full beat]
 * Ad-libs are 1-3 words in parentheses on their own line. A voice is in by
 * bar 4, the first full hook by bar 20, three or four hook statements, and
 * the whole thing runs about 2:45. Deterministic from the seeded `r`.
 *
 * Every line here is original. No names of people, artists, songs, brands or
 * labels. Light profanity only. Words the guard bans in instrumental prompts
 * (house, voice, drop, chill, no X, ...) never appear, except the voice words
 * the engine unlocks for this genre (hook, harmonies, ad-lib, sung, rapped).
 * Chant, choir, crowd, gospel and anthem stay out: they summon group voices. */
"use strict";

const OneLyrics = (() => {
  // ---------------------------------------------------------------- rhymes
  // Families of line endings. Each entry is "ending:types" where the types
  // say which template slot it can close:
  //   n  a noun that follows "the" / "my"       ("still won't touch the phone")
  //   a  a state that follows "I'm" / "it's"    ("they say I changed, I'm grown")
  //   x  an adverbial tail after any verb phrase ("did the passport thing on my own")
  //   c  a tail that only follows a comma        ("and if they ask, just begun")
  //   p  a present-tense predicate after "I/we" ("I don't fold, I pull up")
  // A template whose ending follows a comma takes e: "xc" (x or c).
  const RAW_RHYMES = {
    own: ["phone:n", "throne:n", "zone:n", "chrome:n", "ringtone:n", "cologne:n", "alone:a x", "known:a", "grown:a", "unknown:a", "on my own:x", "on the phone:x", "in the zone:x", "to the bone:c"],
    ight: ["night:n", "light:n", "flight:n", "spotlight:n", "right:a c", "tight:a", "bright:a", "polite:a", "all night:x", "on sight:x", "in the light:x", "tonight:x", "outta sight:a c"],
    ay: ["day:n", "way:n", "payday:n", "replay:n", "play:p", "stay:p", "pray:p", "sway:p", "okay:a", "away:x", "all day:x", "my way:x", "today:x", "on display:c", "in the way:c"],
    eal: ["deal:n", "wheel:n", "meal:n", "feel:p", "kneel:p", "steal:p", "real:a", "ideal:a", "surreal:a", "for real:x", "how I feel:c", "behind the wheel:x", "the real deal:c"],
    ack: ["back:n", "track:n", "stack:n", "pack:n", "plaque:n", "way back:c", "on track:a c", "in the back:x", "in all black:x", "bounce back:p", "fall back:p", "off track:a c", "like that:x"],
    ee: ["key:n", "degree:n", "guarantee:n", "trophy:n", "VIP:n a c", "free:a", "carefree:a", "agree:p", "see:p", "for free:x", "with me:x", "at three:x", "finally:c", "on TV:x"],
    own2: ["crown:n", "town:n", "sound:n", "downtown:n x", "right now:x", "somehow:c", "around:x", "uptown:x", "all around:x", "underground:x", "slow it down:p", "turn it down:p", "hold it down:p", "down:a c"],
    ine: ["line:n", "sign:n", "nine:n", "wine:n", "shine:n p", "fine:a", "mine:a", "divine:a", "online:a x", "in line:x", "by design:c", "at nine:x", "every time:x", "one of a kind:a c"],
    old: ["gold:n a", "cold:a", "bold:a", "old:a", "sold:a", "controlled:a", "untold:a", "hold:p", "fold:p", "on hold:x", "in the cold:x", "all gold:a c", "like I'm told:c", "out cold:a c"],
    eat: ["heat:n", "seat:n", "beat:n", "street:n", "receipt:n", "front seat:n c", "sweet:a", "complete:a", "discreet:a", "on repeat:x", "in the street:x", "can't be beat:c", "off my feet:c", "compete:p", "delete:p"],
    ace: ["face:n", "place:n", "case:n", "space:n", "suitcase:n", "chase:n p", "race:n", "in my place:x", "first place:c", "to my face:c", "second place:c", "in your face:c", "erase:p", "replace:p"],
    ice: ["ice:n", "price:n", "dice:n", "device:n", "paradise:n", "sacrifice:n p", "twice:x", "nice:a", "precise:a", "on ice:x", "roll the dice:p", "at any price:c", "think twice:p"],
    ame: ["name:n", "game:n", "flame:n", "frame:n", "fame:n", "lane:n", "chain:n", "plane:n", "rain:n", "the same:a", "insane:a", "to blame:a", "in my lane:x", "on the plane:x", "in the rain:x"],
    oo: ["crew:n", "view:n", "clue:n", "debut:n", "breakthrough:n", "blue:a", "true:a", "new:a", "brand new:a", "too:c", "at two:x", "with you:x", "for you:x", "on cue:x", "pull through:p"],
    up: ["cup:n", "luck:n", "up:a c", "lit up:a", "stuck:a", "what's up:c", "on the come up:x", "out of luck:a c", "pull up:p", "run it up:p", "wake up:p", "level up:p", "add it up:p", "interrupt:p"],
    ood: ["neighborhood:n", "childhood:n", "good:a", "too good:a", "understood:a", "misunderstood:a", "all good:a", "like I should:c", "for good:x", "if I could:c", "knock on wood:c", "where I stood:c"],
    ing: ["ring:n", "thing:n", "king:n", "bling:n", "diamond ring:n", "everything:c", "anything:c", "the whole thing:c", "one more thing:c", "the real thing:c", "bring:p"],
    ar: ["car:n", "bar:n", "star:n", "superstar:n", "sports car:n", "far:a c", "so far:c", "by far:c", "in the car:x", "at the bar:x", "who you are:c", "where you are:c", "from afar:x", "raise the bar:p"],
    ed: ["bed:n", "head:n", "thread:n", "red:a", "fed:a", "set:a", "well-fed:a", "ahead:a x", "instead:c", "in my head:x", "out of bed:x", "yet:c", "said:p", "forget:p", "regret:p"],
    un: ["sun:n", "run:p", "done:a", "undone:a", "number one:a c", "second to none:a c", "won:p", "outrun:p", "on the run:x", "just begun:c", "in the sun:x", "just for fun:x", "by one:x"],
    out: ["doubt:n", "route:n", "out:a c", "sold out:a c", "all out:a c", "blacked out:a", "without:c", "inside out:c", "what it's about:c", "about:c", "figure it out:p", "work it out:p", "shout:p"],
    ore: ["door:n", "floor:n", "score:n", "store:n", "encore:n", "hardcore:a", "more:c", "before:c", "for sure:c", "on the floor:x", "out the door:x", "by four:x", "ignore:p", "explore:p", "keep score:p"],
    money: ["money:n", "honey:n", "something:n c", "nothing:n c", "stunning:a", "running:a", "coming:a", "stunting:a", "hungry:a", "lucky:a", "funny:a", "a hundred:c"],
  };
  const RHYMES = Object.entries(RAW_RHYMES).map(([id, raw]) => ({
    id,
    entries: raw.map((s) => {
      const [text, types] = s.split(":");
      return { text, types: types.split(" ") };
    }),
  }));
  const strip = (line) => line.replace(/\([^)]*\)/g, "").trim();
  const lastWord = (line) => strip(line).replace(/[^A-Za-z0-9' -]/g, "").trim().split(/\s+/).pop().toLowerCase();
  const FAMILY_OF = {};
  for (const f of RHYMES) for (const e of f.entries) (FAMILY_OF[lastWord(e.text)] = FAMILY_OF[lastWord(e.text)] || new Set()).add(f.id);
  const rhymes = (a, b) => {
    const A = FAMILY_OF[lastWord(a)], B = FAMILY_OF[lastWord(b)];
    return !!(A && B && [...A].some((id) => B.has(id)));
  };

  // Syllables, the heuristic Suno phrasing cares about: vowel groups, a
  // silent e dropped, digits one each ("3am" is two, "2:47" is three).
  function syllables(text) {
    let n = 0;
    for (const raw of strip(text).toLowerCase().split(/[\s-]+/)) {
      const digits = (raw.match(/\d/g) || []).length;
      const w = raw.replace(/[^a-z']/g, "");
      n += digits;
      if (!w) continue;
      if (w.length <= 2) { n += 1; continue; }
      let v = (w.replace(/'/g, "").match(/[aeiouy]+/g) || []).length;
      if (/[^aeiou]e$/.test(w) && !/le$/.test(w) && v > 1) v--;
      if (/[^aeiouy]ed$/.test(w) && !/[td]ed$/.test(w) && v > 1) v--;
      n += Math.max(1, v);
    }
    return n;
  }

  // -------------------------------------------------------------- personas
  // `tag` is the voice phrase for the style field, in the grammar the v6
  // research settled on: "<gender> vocals, <delivery>, <flow>, ad-libs".
  const PERSONAS = {
    rap: [
      { id: "rasp", gender: "male", voice: "male rapper, raspy, laid-back behind the beat", tag: "male vocals, laid-back rapped delivery, behind-the-beat flow, ad-libs", adlibs: ["yeah", "uh", "skrrt"], palette: ["laid-back behind the beat", "half-sung", "doubled"] },
      { id: "bounce", gender: "female", voice: "female rapper, playful, bouncy, light auto-tune", tag: "female vocals, confident rapped delivery, playful flow, ad-libs", adlibs: ["okay", "ha", "period"], palette: ["pushed ahead of the beat", "shouted", "stacked"] },
      { id: "punch", gender: "male", voice: "male rapper, aggressive, punchy, ahead of the beat", tag: "male vocals, aggressive rapped delivery, punchy flow, ad-libs", adlibs: ["what", "huh", "let's go"], palette: ["pushed ahead of the beat", "start-stop flow", "belted"] },
      { id: "sharp", gender: "female", voice: "female rapper, sharp consonants, start-stop flow", tag: "female vocals, aggressive rapped delivery, sharp articulated flow, ad-libs", adlibs: ["uh-huh", "what", "ayy"], palette: ["start-stop flow", "shouted", "dry and close"] },
      { id: "whisper", gender: "male", voice: "male rapper, whispered, close mic, low register", tag: "male vocals, whispered rapped delivery, close-mic low-register flow, sparse ad-libs", adlibs: ["shh", "yeah"], palette: ["whispered", "laid-back behind the beat", "dry and close"] },
      { id: "triplet", gender: "male", voice: "male rapper, triplet flow, high energy", tag: "male vocals, triplet rapped delivery, bouncy high-energy flow, hype ad-libs", adlibs: ["skrrt", "ooh", "go"], palette: ["triplet flow", "shouted", "ad-libs echoing the last word"] },
      { id: "sungrap", gender: "male", voice: "male rapper, sung rap, heavy auto-tune, airy falsetto hook", tag: "auto-tuned male vocals, heavy pitch-corrected melodic rap", adlibs: ["mm", "ooh"], palette: ["half-sung", "falsetto tag", "stacked"] },
      { id: "deadpan", gender: "female", voice: "female rapper, deadpan, dry, slightly behind the beat", tag: "female vocals, deadpan rapped delivery, behind-the-beat flow, sparse ad-libs", adlibs: ["mhm", "yeah", "okay"], palette: ["laid-back behind the beat", "conversational", "single take"] },
      { id: "baritone", gender: "male", voice: "male rapper, deep baritone, slow drawl", tag: "male vocals, deep baritone rapped delivery, slow-drawl flow, ad-libs", adlibs: ["yeah", "uh", "woo"], palette: ["laid-back behind the beat", "doubled", "ad-libs echoing the last word"] },
    ],
    rnb: [
      { id: "breath", gender: "female", voice: "female singer, breathy, intimate, close mic", tag: "female R&B vocals, breathy intimate verses, belted hook, melismatic runs", adlibs: ["mm", "oh", "yeah"], palette: ["whispered", "half-sung", "stacked harmonies"] },
      { id: "falsetto", gender: "male", voice: "male singer, falsetto, airy, late-night", tag: "male R&B vocals, airy falsetto on the hook, breathy verses, stacked harmonies", adlibs: ["ooh", "mm"], palette: ["falsetto tag", "doubled", "sparse"] },
      { id: "belt", gender: "female", voice: "female singer, belted, powerful, runs on the last line", tag: "female R&B vocals, belted hook, big melismatic runs, stacked harmonies", adlibs: ["oh", "hey", "yeah"], palette: ["belted", "octave up on the last line", "stacked"] },
      { id: "moody", gender: "male", voice: "male singer, auto-tuned, half-sung half-rapped, moody", tag: "auto-tuned male R&B vocals, half-sung half-rapped verses, moody falsetto hook", adlibs: ["yeah", "uh", "mm"], palette: ["half-sung", "laid-back behind the beat", "doubled"] },
      { id: "hush", gender: "female", voice: "female singer, hushed, whispery, minimal", tag: "female R&B vocals, hushed whispered verses, soft doubled hook, slow runs", adlibs: ["mm", "hm"], palette: ["whispered", "sparse", "dry and close"] },
      { id: "tenor", gender: "male", voice: "male singer, warm tenor, conversational, rap cadence", tag: "male R&B vocals, warm conversational verses, sung-rap cadence, stacked harmonies", adlibs: ["yeah", "okay", "mm"], palette: ["conversational", "half-sung", "harmonies on the last word"] },
      { id: "sweet", gender: "female", voice: "female singer, sweet tone, light auto-tune, bouncy", tag: "female R&B vocals, sweet light auto-tuned verses, bouncy hook, stacked ad-libs", adlibs: ["ayy", "oh", "mm-hm"], palette: ["half-sung", "stacked", "ad-libs echoing the last word"] },
      { id: "plead", gender: "male", voice: "male singer, raspy, pleading, belted bridge", tag: "male R&B vocals, raspy pleading verses, belted bridge, falsetto tag", adlibs: ["oh", "yeah", "woah"], palette: ["belted", "falsetto tag", "octave up on the last line"] },
      { id: "alto", gender: "female", voice: "female singer, low alto, cool, laid-back", tag: "female R&B vocals, low alto laid-back verses, doubled hook, breathy harmonies", adlibs: ["mm", "right", "yeah"], palette: ["laid-back behind the beat", "doubled", "sparse"] },
    ],
  };

  // ---------------------------------------------------------------- themes
  // obj: bare singular nouns (templates add the/my). place: with its own
  // preposition. img: a noun phrase. scene: a short present-tense clause.
  // verbs: [base, -ing]. flex / vuln: short clause fragments that take a tail.
  const THEMES = [
    {
      id: "comeup", label: "the come-up", lanes: ["rap"],
      obj: ["passport", "spare key", "duffel", "wristwatch", "hoodie", "parking ticket", "bus pass", "lock screen"],
      place: ["on the rooftop", "in the parking lot", "in the back booth", "in the stairwell", "at the gas station", "in the lobby"],
      img: ["rain on the windshield", "forty in the glovebox", "two phones on the charger", "hood up in the lobby", "receipts in the hoodie"],
      scene: ["the lights go off", "the card gets approved", "the driver asks my name", "my mom stops asking", "the wire clears"],
      verbs: [["ask", "asking"], ["fold", "folding"], ["wait", "waiting"], ["beg", "begging"], ["look back", "looking back"], ["sleep", "sleeping"]],
      flex: ["same hoodie, new tax bracket", "took the bus, left in a two-door", "mom's rent paid off one verse", "broke at nine, booked by eleven", "they laughed, now they hold the door", "ran it up off a cracked screen", "first flight, I kept the ticket"],
      vuln: ["I still count the change twice", "I don't sleep, I set alarms", "I flinch when the card gets swiped", "my mom still thinks I'm lying", "had the plan before the bus fare", "I still sleep with the lights on", "I got the keys, still knock first"],
    },
    {
      id: "latetext", label: "late-night text", lanes: ["rap", "rnb"],
      obj: ["read receipt", "lock screen", "voicemail", "screenshot", "group chat", "second phone", "charger", "typing bubble"],
      place: ["in the parking lot", "in the back seat", "on your front step", "in the hotel lobby", "on the top floor"],
      img: ["three dots at 2am", "your name on the lock screen", "one bar of signal", "the glow on the ceiling", "my thumb on send"],
      scene: ["the dots show up", "you leave me on read", "the screen lights up", "my phone buzzes twice", "you say you're almost here"],
      verbs: [["text", "texting"], ["wait", "waiting"], ["call", "calling"], ["scroll", "scrolling"], ["pretend", "pretending"], ["reply", "replying"]],
      flex: ["I don't double text, I show up", "you typed a minute, I read it in one", "phone on silent, you still got through", "two words from you, I'm outside", "you say goodnight, I say what time", "you said wyd, I said outside", "left you on read, you read it twice"],
      vuln: ["typed it four times, sent nothing", "read it twice and left it there", "I still save the screenshots", "I keep the ringer on for you", "I hear the buzz when it's quiet", "I check the dots like a pulse", "sent a dot, deleted the dot"],
    },
    {
      id: "exwatching", label: "the ex who's watching", lanes: ["rap", "rnb"],
      obj: ["story", "profile", "mutual friend", "old hoodie", "old number", "new ride", "block list", "view count"],
      place: ["at the club", "on the rooftop", "at the same bar", "on your timeline", "in the front row"],
      img: ["your name in my views", "a like from the past", "my pic on your timeline", "the same two mutuals", "your new one in my old hoodie"],
      scene: ["you watch the story twice", "your friend says hey", "you drive by slow", "your name pops up", "the views come in"],
      verbs: [["look", "looking"], ["post", "posting"], ["scroll", "scrolling"], ["glow", "glowing"], ["win", "winning"], ["text", "texting"]],
      flex: ["you watch my story, I don't watch back", "blocked you Tuesday, you found Friday", "you kept the hoodie, I kept the glow", "you said I'd fall off, I fell up", "I'm doing fine, go ask your friends", "new car, same spot, you know the plate", "you stayed up for my story"],
      vuln: ["I check who watched, then hate it", "I still know your car by the sound", "I posted that one for you", "I kept one picture, won't say which", "blocked you twice, unblocked you once", "I post at midnight so you see it", "your name's still saved, won't say as what"],
    },
    {
      id: "loyalty", label: "loyalty", lanes: ["rap", "rnb"],
      obj: ["day one", "spare key", "group chat", "same number", "back seat", "old phone", "front door", "first car"],
      place: ["in the back booth", "on the same block", "at the long table", "on the porch", "in my mom's kitchen"],
      img: ["same five in the group chat", "the key I never changed", "two chairs, same table", "my day ones in the front", "the same number since fourteen"],
      scene: ["the money gets funny", "the room goes quiet", "the lights come on", "it gets real late", "they stop calling"],
      verbs: [["switch", "switching"], ["fold", "folding"], ["ask", "asking"], ["count", "counting"], ["trust", "trusting"], ["leave", "leaving"]],
      flex: ["same five since fourteen, we don't recruit", "ain't call at four, don't call at noon", "I don't add seats, I add plates", "my day ones got keys, not invites", "the chat got five, the list got five", "same porch, same five, bigger plates", "my circle's small, the table's long"],
      vuln: ["I count who stayed on one hand", "I pay the table so nobody asks", "I still owe the ones who drove me", "I say I'm fine so they don't worry", "rather be broke with the same five", "I miss calls from everyone but five", "they knew me broke, that's the whole list"],
    },
    {
      id: "outside", label: "outside tonight", lanes: ["rap", "rnb"],
      obj: ["two-door", "rooftop", "guest list", "last bottle", "back door", "tinted window", "after-party", "driver"],
      place: ["on the rooftop", "in the parking lot", "on the back patio", "on the top floor", "at the side door"],
      img: ["headlights in the rearview", "forty deep at the side door", "phone at one percent", "shoes still clean at four", "sparklers on the last bottle"],
      scene: ["the lights come on", "the bottle comes out", "the driver pulls up", "the speaker cuts out", "my name's at the door"],
      verbs: [["leave", "leaving"], ["sleep", "sleeping"], ["wait", "waiting"], ["spin", "spinning"], ["dance", "dancing"], ["pull up", "pulling up"]],
      flex: ["I'm outside, bring a jacket", "pulled up at two, leaving whenever", "list said four, we came as nine", "I don't dance, I supervise", "we don't take pics, we take the table", "the valet knows the car, not me", "tints up, speakers down, we here"],
      vuln: ["I'm outside so I don't sit with it", "the louder it gets, the less I think", "I'm the last one here most nights", "I text the chat so I'm not alone", "I'm outside but I'm not all here", "I'm loud so the quiet can't find me", "first one in, last one honest"],
    },
    {
      id: "newmoney", label: "new money problems", lanes: ["rap"],
      obj: ["wire", "accountant", "deposit slip", "second account", "tax guy", "new address", "safe", "black card"],
      place: ["at the bank", "in the lobby", "in the showroom", "in the corner office", "at the valet"],
      img: ["a wire with too many zeros", "my accountant on read", "two keys to a place I never sleep", "a safe with nothing in it yet", "a receipt longer than the menu"],
      scene: ["the wire clears", "the accountant calls", "the valet asks", "the decimal moves", "the card gets declined"],
      verbs: [["spend", "spending"], ["count", "counting"], ["save", "saving"], ["lend", "lending"], ["stress", "stressing"], ["flex", "flexing"]],
      flex: ["new problems, same solution, more", "my accountant texts like an ex", "two cars and one parking spot", "the tax man knows my name now", "bought next door for the quiet", "I tip the valet what I made at nine", "two accountants, both on speed dial"],
      vuln: ["I still check the price first", "new money, same sleep schedule", "I pay people to say I'm fine", "got the bag, lost the appetite", "the more it clears, the less I feel", "the zeros grew, the circle didn't", "I still feel broke on the first"],
    },
    {
      id: "longdistance", label: "long-distance", lanes: ["rnb", "rap"],
      obj: ["time zone", "boarding pass", "window seat", "hotel key", "layover", "red-eye", "voicemail", "countdown"],
      place: ["at the terminal", "at gate twenty-two", "in the hotel lobby", "in the window seat", "at baggage claim"],
      img: ["two clocks on one lock screen", "your words at 3am my time", "a boarding pass in my back pocket", "the same moon, different hour", "your hoodie in my carry-on"],
      scene: ["the wheels touch down", "the signal cuts", "you fall asleep first", "the countdown hits zero", "the layover runs long"],
      verbs: [["land", "landing"], ["call", "calling"], ["count", "counting"], ["wait", "waiting"], ["fly", "flying"], ["miss", "missing"]],
      flex: ["fly out at six, yours by nine", "two time zones, one ringtone", "the red-eye knows my name", "I don't miss flights, I miss you", "booked the window to watch for you", "your sunrise is my encore", "I packed one bag and your hoodie"],
      vuln: ["I count the hours wrong on purpose", "your goodnight is my good morning", "I sleep with the ringer up", "boarding pass in the mirror", "your voicemail got me through", "I check your weather before mine", "three hours ahead, still waiting up"],
    },
    {
      id: "city3am", label: "the city at 3am", lanes: ["rap", "rnb"],
      obj: ["night bus", "corner store", "last train", "rooftop", "streetlight", "taxi light", "diner", "all-night spot"],
      place: ["on the rooftop", "at the corner store", "in the diner", "on the bridge", "on the last train"],
      img: ["streetlights on wet pavement", "the diner lit like a stage", "one taxi light for miles", "steam from the grate", "headlights in the fog"],
      scene: ["the last train leaves", "the sirens pass", "the diner closes", "the rain starts", "the lights go green"],
      verbs: [["walk", "walking"], ["ride", "riding"], ["wait", "waiting"], ["think", "thinking"], ["leave", "leaving"], ["wander", "wandering"]],
      flex: ["city sleeps, I run the night shift", "3am, I own every green light", "the diner knows my order, not my name", "I walk the city like a landlord", "streetlights clock in when I do", "the taxi light only blinks for me", "the diner holds my seat till four"],
      vuln: ["I ride the last train so I'm not home", "the city's loud enough to think in", "I know this corner more than my place", "I walk until the thoughts get tired", "3am is the only honest hour", "the quiet hours know my walk", "I'm up with the streetlights and the bakers"],
    },
    {
      id: "selfmade", label: "self-made", lanes: ["rap"],
      obj: ["blueprint", "first check", "old laptop", "contract", "business card", "garage", "spreadsheet", "first key"],
      place: ["in the garage", "at the kitchen table", "in the corner office", "in the first office", "in the back room"],
      img: ["a spreadsheet lit at midnight", "the first check in a frame", "one laptop, three jobs", "a plan on a napkin", "sawdust on the business card"],
      scene: ["the first check clears", "the plan works", "the lights stay on", "the landlord smiles", "the rent comes early"],
      verbs: [["ask", "asking"], ["wait", "waiting"], ["quit", "quitting"], ["fold", "folding"], ["stack", "stacking"], ["plan", "planning"]],
      flex: ["handed nothing but the bill", "wrote the plan on a napkin in ink", "I was the intern and the owner", "first office had a garage door", "they said get a job, I made nine", "I signed my own permission slip", "my job title is my own name"],
      vuln: ["I work like the lights might go off", "I don't celebrate, I restart", "made it alone, that's the part that hurts", "I say I'm fine, the spreadsheet disagrees", "never learned to rest, learned to invoice", "made the room, still knock first", "the office is quiet when I win"],
    },
    {
      id: "mainchar", label: "main character", lanes: ["rap", "rnb"],
      obj: ["slow walk", "playlist", "sunglasses", "long coat", "entrance", "mirror", "window seat", "theme song"],
      place: ["at the crosswalk", "at the airport", "in the front row", "in the grocery aisle", "on the escalator"],
      img: ["a slow walk through the crosswalk", "sunglasses at the register", "the wind doing its job", "my playlist in the airport", "a coat that enters first"],
      scene: ["the song starts", "the doors open", "the wind picks up", "the lights hit", "the camera finds me"],
      verbs: [["rush", "rushing"], ["blink", "blinking"], ["turn", "turning"], ["pose", "posing"], ["walk", "walking"], ["wave", "waving"]],
      flex: ["I walk slow so the story catches up", "every room's a scene, I'm the lead", "the wind's on payroll when I leave", "I don't enter, I arrive", "my playlist knows before I do", "slow motion is my only speed", "the lights find me first"],
      vuln: ["I play the lead so I don't feel small", "camera's off, I'm still posing", "I walk slow so nobody sees me rush", "I'm the main one, still check my teeth", "theme song's loud so the quiet's not", "the story's mine, the ending isn't", "I pose so the room won't ask"],
    },
    {
      id: "lastcall", label: "last call", lanes: ["rnb", "rap"],
      obj: ["last bottle", "tab", "closing light", "coat check", "last song", "bartender", "back booth", "cab"],
      place: ["in the back booth", "on the patio", "in the parking lot", "on the side street", "at the coat check"],
      img: ["the lights up too bright", "two glasses, one left", "a tab longer than the night", "a cab with the door open", "your number on a napkin"],
      scene: ["the lights come up", "they call last call", "the tab comes", "the cab pulls up", "the music cuts"],
      verbs: [["stay", "staying"], ["leave", "leaving"], ["wait", "waiting"], ["close", "closing"], ["dance", "dancing"], ["lie", "lying"]],
      flex: ["last call, I'm first out the door", "the tab's on me, the night's on you", "lights up, still looking good", "I close the place like I own it", "the last song's mine, I asked", "the bartender knows the next round's mine", "closing time, I'm still the show"],
      vuln: ["I stay till last call so I don't go home", "lights come up, so does the truth", "one more and I'll say it", "I tip too much so somebody remembers", "last call's the only time I'm honest", "I count the glasses, not the hours", "the lights are honest, so am I"],
    },
    {
      id: "betteroff", label: "better off", lanes: ["rnb", "rap"],
      obj: ["old number", "spare key", "mutual friend", "apartment", "playlist", "pillow", "plant", "chair"],
      place: ["in the new place", "on the old street", "at the corner store", "in the kitchen", "on the balcony"],
      img: ["your plant alive without you", "a bed made all the way", "the playlist with your songs gone", "sunlight on your side", "the chair where you used to sulk"],
      scene: ["the lease ends", "the plant grows", "your friend asks about me", "the apartment goes quiet", "the song comes on"],
      verbs: [["call", "calling"], ["look back", "looking back"], ["beg", "begging"], ["sleep", "sleeping"], ["water", "watering"], ["change", "changing"]],
      flex: ["the plant's alive, so am I", "I sleep in the middle now", "same rent, the air is cheaper", "kept the apartment and the glow", "your friend asked, I said look", "your side's a bookshelf now", "I changed the locks and the playlist"],
      vuln: ["still check the time you'd call", "I water your plant, that's all I'll admit", "the quiet was loud for a month", "I'm good now, that took a minute", "I don't miss you, I miss the routine", "I kept the mug, not the number", "I'm fine, that's not the same as good"],
    },
  ];
  const NUM = ["two", "four", "seven", "nine", "twelve", "twenty", "forty", "ninety-nine", "two hundred", "a thousand"];
  const TIME = ["3am", "4am", "2:47", "half past two", "midnight", "a quarter to four", "Sunday", "last Tuesday", "noon", "6am"];
  const SOUND = ["ring ring", "beep beep", "tick tock", "knock knock", "vroom vroom", "cha-ching", "click clack", "ding ding"];
  const QUESTION = ["what's the move?", "what it is?", "who's that?", "where we at?", "what's the word?", "you know what?", "say what?", "what they want?"];

  // --------------------------------------------------------------- templates
  // {E} is the rhyme ending; `e` says which type it takes. `lanes` limits a
  // template to one lane. Hook lines land at 4-9 syllables.
  const HOOK_T = [
    // objects and placements
    { t: "{num} missed calls, won't touch the {E}", e: "n" },
    { t: "{time}, back on the {E}", e: "n" },
    { t: "put my {obj} by the {E}", e: "n" },
    { t: "they want the {obj}, I want the {E}", e: "n" },
    { t: "don't need a map, I need the {E}", e: "n" },
    { t: "left the {obj}, kept the {E}", e: "n" },
    { t: "one hand on the {obj}, one on the {E}", e: "n" },
    { t: "new {obj}, same {E}", e: "n" },
    { t: "{img}, hand on the {E}", e: "n" },
    { t: "{obj} up, pass me the {E}", e: "n" },
    { t: "I'm the {obj} and the {E}", e: "n" },
    { t: "{num} on the dash, {num2} on the {E}", e: "n" },
    { t: "we don't do {objs}, we do the {E}", e: "n" },
    { t: "{sound}, that's the {E}", e: "n" },
    { t: "bet you won't say that to the {E}", e: "n" },
    { t: "one for the {obj}, two for the {E}", e: "n" },
    { t: "two in the morning, one in the {E}", e: "n" },
    { t: "keep your {obj}, I keep the {E}", e: "n" },
    { t: "I'll be the {obj} and the {E}", e: "n", lanes: ["rnb"] },
    { t: "one more {obj}, one more {E}", e: "n" },
    // states
    { t: "{num} in the {obj}, still {E}", e: "a" },
    { t: "{time} text said you're still {E}", e: "a" },
    { t: "they say I changed, I say I'm {E}", e: "a" },
    { t: "{obj} on the floor and I'm {E}", e: "a" },
    { t: "the {obj} said I'm {E}, so I'm {E}", e: "a" },
    { t: "{img}, everything {E}", e: "a" },
    { t: "{num} {objs}? that's {E}", e: "a" },
    { t: "showed up {time}, still {E}", e: "a" },
    { t: "{obj} {place}, keep it {E}", e: "a" },
    { t: "I'm {num} deep and I'm {E}", e: "a" },
    { t: "{scene}, now I'm {E}", e: "a" },
    { t: "{num} down, {num2} to go, I'm {E}", e: "a" },
    { t: "come over, it's {time}, I'm {E}", e: "a", lanes: ["rnb"] },
    { t: "say less, I'm already {E}", e: "a" },
    { t: "{time} and I'm still {E}", e: "a", lanes: ["rnb"] },
    { t: "I'm not calling, I'm {E}", e: "a", lanes: ["rnb"] },
    // tails
    { t: "did the {obj} thing {E}", e: "x" },
    { t: "{time} and we out, {E}", e: "xc" },
    { t: "found the {obj} {E}", e: "x" },
    { t: "{obj} off, I'm {ving} {E}", e: "x" },
    { t: "we been {ving} {E}", e: "x" },
    { t: "{place} with the {obj}, {E}", e: "xc" },
    { t: "{num} deep, pulled up {E}", e: "x" },
    { t: "{obj} {place}, {E}", e: "xc" },
    { t: "{ving} with the {obj} {E}", e: "x" },
    { t: "meet me {place}, {E}", e: "xc", lanes: ["rnb"] },
    { t: "{num} on the {obj}, {E}", e: "xc" },
    { t: "I keep the {obj} {E}", e: "x" },
    // predicates
    { t: "I don't {vb}, I {E}", e: "p" },
    { t: "{num} {objs} later, still I {E}", e: "p" },
    { t: "they talk all week, we {E}", e: "p" },
    { t: "{time} hits and we {E}", e: "p" },
    { t: "{obj} {place}? we {E}", e: "p" },
    { t: "every {time} I {E}", e: "p" },
    { t: "when {scene}, I {E}", e: "p" },
    { t: "I won't call, I'll just {E}", e: "p" },
    { t: "she said {vb}, so I {E}", e: "p" },
    { t: "{num} missed calls, I still {E}", e: "p" },
    { t: "dare me to {vb}, I {E}", e: "p" },
    { t: "you don't {vb}, you {E}", e: "p", lanes: ["rnb"] },
  ];

  const VERSE_T = [
    // detail
    { k: "detail", t: "{num} on the dash, {num2} on the {E}", e: "n" },
    { k: "detail", t: "woke up {time}, hand still on the {E}", e: "n" },
    { k: "detail", t: "{img}, I'm looking at the {E}", e: "n" },
    { k: "detail", t: "{obj} {place}, that's the {E}", e: "n" },
    { k: "detail", t: "{img}, {E}", e: "xc" },
    { k: "detail", t: "{time}, {place}, {E}", e: "xc" },
    { k: "detail", t: "{obj} in my pocket, {E}", e: "xc" },
    { k: "detail", t: "{scene}, I'm {ving} {E}", e: "x" },
    { k: "detail", t: "{obj} {place}, that's {E}", e: "a" },
    { k: "detail", t: "{scene}, now it's {E}", e: "a" },
    { k: "detail", t: "{scene}, I {E}", e: "p" },
    { k: "detail", t: "{time}, I {vb}, then I {E}", e: "p" },
    { k: "detail", t: "{num} {objs} and one {obj2}, {E}", e: "xc" },
    { k: "detail", t: "{time} and your name's on the {E}", e: "n", lanes: ["rnb"] },
    { k: "detail", t: "I'm {ving} {place}, {E}", e: "xc", lanes: ["rnb"] },
    { k: "detail", t: "you {vb}, I {E}", e: "p", lanes: ["rnb"] },
    // flex
    { k: "flex", t: "{flex}, {E}", e: "xc" },
    { k: "flex", t: "{flex}, I'm {E}", e: "a" },
    { k: "flex", t: "{flex}, that's the {E}", e: "n" },
    { k: "flex", t: "{flex}, we {E}", e: "p" },
    { k: "flex", t: "we don't {vb}, we {E}", e: "p" },
    // turn
    { k: "turn", t: "{vuln}, {E}", e: "xc" },
    { k: "turn", t: "{vuln}, I'm {E}", e: "a" },
    { k: "turn", t: "{vuln}, so I {E}", e: "p" },
    { k: "turn", t: "{vuln}, that's the {E}", e: "n" },
    // internal rhyme: {E2} is a second ending from the same family
    { k: "internal", t: "{num} {objs}, got the {E2} and the {E}", e: "n", e2: "n" },
    { k: "internal", t: "I been {E2}, I been {E}", e: "x", e2: "x" },
    { k: "internal", t: "I'm so {E2} it's {E}", e: "a", e2: "a" },
    { k: "internal", t: "I {E2}, then I {E}", e: "p", e2: "p" },
    { k: "internal", t: "they {E2}, we {E}", e: "p", e2: "p" },
    { k: "internal", t: "{obj} {E2}, {obj2} {E}", e: "x", e2: "x" },
    { k: "internal", t: "I'm {E2} when you're {E}", e: "a", e2: "a", lanes: ["rnb"] },
    { k: "internal", t: "you {E2}, I {E}", e: "p", e2: "p", lanes: ["rnb"] },
    // callback to the hook's object (verse 2 only)
    { k: "callback", t: "still got the {hobj} on me, {E}", e: "xc" },
    { k: "callback", t: "back to the {hobj}, back to the {E}", e: "n" },
    { k: "callback", t: "the {hobj} thing? I'm {E}", e: "a" },
    { k: "callback", t: "{hobj} on my mind, so I {E}", e: "p" },
  ];

  const PRE_T = [
    { t: "and if they ask, {E}", e: "xc" },
    { t: "{scene}, {E}", e: "xc" },
    { t: "say it with me, {E}", e: "xc" },
    { t: "{num} more and I'm {E}", e: "a" },
    { t: "one more time for the {E}", e: "n" },
    { t: "louder for the {E}", e: "n" },
    { t: "here it comes, {E}", e: "xc" },
    { t: "and I {E}", e: "p" },
    { t: "all the way {E}", e: "a" },
    { t: "so we {E}", e: "p" },
    { t: "hands up if you're {E}", e: "a" },
    { t: "{time}, {E}", e: "xc" },
  ];

  const BRIDGE_T = [
    { t: "{vuln}, {E}", e: "xc" },
    { t: "{vuln}, I'm {E}", e: "a" },
    { t: "if you {vb}, I {E}", e: "p" },
    { t: "{img}, and that's the {E}", e: "n" },
    { t: "it's quiet now, {E}", e: "xc" },
    { t: "I say I'm good, I mean I'm {E}", e: "a" },
    { t: "{time}, I'm {ving} {E}", e: "x" },
    { t: "{scene}, and I {E}", e: "p" },
  ];

  const INTRO_SPOKEN = ["yeah", "okay, okay", "run it", "one take", "say less", "it's {time}, who's up?", "lights off, mic on", "{title}, listen", "{title}", "mm, yeah", "ayy, ayy", "let's get it"];

  // ------------------------------------------------------ performance banks
  // No chant, choir, crowd, gospel or anthem anywhere: they summon group voices.
  const DELIVERY = {
    rap: ["laid-back behind the beat", "pushed ahead of the beat", "triplet flow", "start-stop flow", "whispered", "half-sung", "shouted", "conversational", "double-time burst"],
    rnb: ["half-sung", "whispered", "belted", "conversational", "laid-back behind the beat", "sung with a rap cadence", "breathy", "runs on the last word"],
  };
  const DYNAMIC = ["sparse", "rising", "biggest", "quiet", "wide", "tight"];
  const TEXTURE = ["stacked", "doubled", "octave up on the last line", "falsetto tag", "ad-libs echoing the last word", "dry and close", "harmonies on the last word", "single take", "stacked harmonies"];
  const BEAT = ["full beat", "808 only", "drums cut", "hats only", "half-time drums", "808 only then full beat", "drums cut on the last line"];

  // ---------------------------------------------------------------- helpers
  const cap = (s) => s.charAt(0).toUpperCase() + s.slice(1);
  const plural = (n) => (/(s|x|ch|sh)$/.test(n) ? `${n}es` : /[^aeiou]y$/.test(n) ? `${n.slice(0, -1)}ies` : `${n}s`);
  const choose = (arr, r, not) => {
    const pool = not ? arr.filter((x) => !not.includes(x)) : arr;
    const p = pool.length ? pool : arr;
    return p[Math.floor(r() * p.length)];
  };
  const forLane = (list, lane) => list.filter((t) => !t.lanes || t.lanes.includes(lane));
  const endingsOf = (fam, type) => fam.entries.filter((e) => type === "xc" ? e.types.includes("x") || e.types.includes("c") : e.types.includes(type)).map((e) => e.text);
  const distinct = (list) => new Set(list.map(lastWord)).size;
  const canEnd = (fam, t) => endingsOf(fam, t.e).length > 0 && (!t.e2 || distinct(endingsOf(fam, t.e2)) > 1);
  const HOOK_SYL = [4, 9], VERSE_SYL = [7, 12], SOFT_SYL = [4, 12];
  const fits = (line, [lo, hi]) => { const s = syllables(line); return s >= lo && s <= hi; };

  // Fill one template. `ctx` carries the theme, the persona and the slots
  // that must stay fixed across a hook (the title object, the hook ending).
  // A fragment (flex, vuln, image, scene) is not reused in one song until
  // its bank runs out; the line that lands commits the fragments it used.
  function fill(tpl, fam, ctx, r, fixed = {}) {
    const T = ctx.theme;
    const used = ctx.used || (ctx.used = {});
    const taken = {};
    const fresh = (key, bank) => {
      const count = used[key] || {};
      const low = Math.min(...bank.map((x) => count[x] || 0));
      let pool = bank.filter((x) => (count[x] || 0) === low);
      if (pool.length < 2) pool = bank.filter((x) => (count[x] || 0) <= low + 1);
      const v = choose(pool, r);
      taken[key] = v;
      return v;
    };
    const obj = fixed.obj || choose(T.obj, r);
    const obj2 = choose(T.obj, r, [obj]);
    const num = fixed.num || choose(NUM, r);
    const num2 = choose(NUM, r, [num]);
    const verb = choose(T.verbs, r);
    const E = fixed.E || choose(endingsOf(fam, tpl.e), r);
    // An internal rhyme ends on a different word than the line's ending.
    const E2 = tpl.e2 ? choose(endingsOf(fam, tpl.e2).filter((x) => lastWord(x) !== lastWord(E)), r) : "";
    const slots = {
      obj, objs: plural(obj), obj2, num, num2, E, E2,
      time: choose(TIME, r), place: choose(T.place, r), sound: choose(SOUND, r),
      vb: verb[0], ving: verb[1], hobj: ctx.hookObj || obj, title: ctx.title || "",
    };
    const line = tpl.t.replace(/\{(\w+)\}/g, (_, k) => {
      if (k === "img" || k === "scene" || k === "flex" || k === "vuln") return fresh(k, T[k]);
      return slots[k] ?? "";
    });
    const commit = () => { for (const k in taken) { used[k] = used[k] || {}; used[k][taken[k]] = (used[k][taken[k]] || 0) + 1; } };
    return { line: cap(line), obj, E, tpl, commit };
  }

  // Fill until the line fits the syllable window; keep the closest miss.
  function fillFit(tpls, fam, ctx, r, range, fixed, avoid = []) {
    let best = null, bestMiss = Infinity;
    for (let i = 0; i < 24; i++) {
      const tpl = tpls[Math.floor(r() * tpls.length)];
      const out = fill(tpl, fam, ctx, r, fixed);
      if (avoid.includes(out.line)) continue;
      const s = syllables(out.line);
      const miss = s < range[0] ? range[0] - s : s > range[1] ? s - range[1] : 0;
      if (miss === 0) { out.commit(); return out; }
      if (miss < bestMiss) { best = out; bestMiss = miss; }
    }
    best = best || fill(tpls[0], fam, ctx, r, fixed);
    best.commit();
    return best;
  }

  const adlibLine = (persona, r) => `(${choose(persona.adlibs, r)})`;

  // Title: the last 2-4 words of the hook's first line, never starting on a
  // throwaway word.
  const STOP = new Set(["the", "a", "an", "my", "your", "and", "it's", "still", "so", "to", "on", "in", "of", "that's", "then", "be", "just", "that", "me", "deep", "with"]);
  function titleOf(line) {
    const clause = strip(line).split(",").pop().replace(/[.?!]/g, "").trim().split(/\s+/);
    if (clause.length >= 2 && clause.length <= 4) return clause.map(cap).join(" ");
    const w = strip(line).replace(/[,.?!]/g, "").split(/\s+/);
    for (const n of [2, 3, 4]) {
      if (w.length < n + 1) continue;
      const tail = w.slice(-n);
      if (!STOP.has(tail[0].toLowerCase())) return tail.map(cap).join(" ");
    }
    return w.slice(-2).map(cap).join(" ");
  }

  // -------------------------------------------------------------- the hook
  // Title line, one or two more lines in the same family, title line again.
  function writeHook(ctx, r) {
    const pool = forLane(HOOK_T, ctx.lane);
    const fam = choose(RHYMES.filter((f) => pool.filter((t) => canEnd(f, t)).length >= 6), r);
    const usable = pool.filter((t) => canEnd(fam, t));
    const used = [];
    const make = (fixed, avoid) => {
      const out = fillFit(usable.filter((t) => !used.includes(t)), fam, ctx, r, HOOK_SYL, fixed, avoid);
      used.push(out.tpl);
      return out;
    };
    const one = make();
    const title = titleOf(one.line);
    // The object verse 2 calls back is one that is actually in the title line.
    const inLine = ctx.theme.obj.filter((o) => one.line.toLowerCase().includes(o));
    ctx.hookObj = inLine.includes(one.obj) ? one.obj : inLine[0] || one.obj;
    ctx.title = title;
    const lines = [one.line];
    // Line 2: the same line with one word swapped, some rolls.
    let two = null;
    if (r() < 0.25 && /\{obj\}/.test(one.tpl.t) && ctx.theme.obj.length > 1) {
      const swapped = fill(one.tpl, fam, ctx, r, { obj: choose(ctx.theme.obj, r, [one.obj]), E: one.E });
      if (swapped.line !== one.line && fits(swapped.line, HOOK_SYL)) two = swapped;
    }
    if (!two) two = make(undefined, lines);
    lines.push(two.line);
    // Line 3: a question answered by the title, some rolls; else a third line.
    const q = `${cap(choose(QUESTION, r))} ${title}`;
    if (r() < 0.3 && fits(q, HOOK_SYL) && !lines.includes(q)) lines.push(q);
    else lines.push(make(undefined, lines).line);
    lines.push(one.line);
    return { lines, title, family: fam.id, obj: one.obj, E: one.E };
  }

  // ------------------------------------------------------------- the verse
  // `bars` lines in couplets that share a family. One internal rhyme per
  // four bars, one turn line, a callback to the hook's object when asked,
  // a hook-shaped one-liner at the top of verse 1.
  function writeVerse(ctx, r, bars, { callback = false, opener = null, oneliner = false, lane = ctx.lane } = {}) {
    const pool = forLane(VERSE_T, lane);
    const by = (k) => pool.filter((t) => t.k === k);
    const couplets = Math.max(1, Math.round(bars / 2));
    // Internal rhymes sit on even couplets (one per four bars); the turn and
    // the hook callback take odd ones so nothing pushes the internal out.
    const odd = Array.from({ length: couplets }, (_, i) => i).filter((i) => i % 2 === 1);
    const turnAt = choose(odd, r);
    const callAt = callback ? choose(odd, r, [turnAt]) : -1;
    const lines = [];
    let firstTpl = null;
    let prev = null;
    // A line retries across its own template first, then its siblings of
    // the same kind, so a long flex fragment can fall back to a shorter one.
    const emit = (tpl, fam) => {
      // An internal rhyme or a hook callback only retries within its own kind.
      const own = tpl.k === "internal" || tpl.k === "callback";
      const siblings = (own ? by(tpl.k) : tpl.k ? [...by(tpl.k), ...by("detail")] : forLane(HOOK_T, lane)).filter((t) => canEnd(fam, t));
      const out = fillFit([tpl, tpl, tpl, ...siblings], fam, ctx, r, VERSE_SYL, undefined, [prev]);
      prev = out.line;
      return out.line;
    };
    for (let c = 0; c < couplets; c++) {
      const kinds = [];
      if (c === turnAt) kinds.push("turn");
      if (c === callAt) kinds.push("callback");
      if (c % 2 === 0) kinds.push("internal");
      while (kinds.length < 2) kinds.push(r() < 0.35 ? "flex" : "detail");
      let a = choose(by(kinds[0]), r), b = choose(by(kinds[1]), r, [a]);
      // The quotable one-liner lands in bars 1-4, on the couplet's second
      // line so the internal rhyme on the first line stays.
      if (c === 0 && oneliner && r() < 0.6) b = choose(forLane(HOOK_T, lane), r);
      if (c === 0 && opener) for (let i = 0; i < 6 && a === opener; i++) a = choose(by(kinds[0]), r);
      if (c === 0) firstTpl = a;
      const candidates = RHYMES.filter((f) => canEnd(f, a) && canEnd(f, b));
      const fam = candidates.length ? choose(candidates, r) : choose(RHYMES.filter((f) => canEnd(f, b)), r);
      if (!candidates.length) a = b, b = choose(by("detail").filter((t) => canEnd(fam, t)), r, [a]);
      lines.push(emit(a, fam), emit(b, fam));
    }
    return { lines, opener: firstTpl };
  }

  function writePre(ctx, r, famId) {
    const fam = RHYMES.find((f) => f.id === famId);
    const n = 2 + Math.floor(r() * 3);
    const pool = PRE_T.filter((t) => canEnd(fam, t));
    const lines = [];
    const used = [];
    for (let i = 0; i < n; i++) {
      const tpls = pool.filter((t) => !used.includes(t));
      const out = fillFit(tpls.length ? tpls : pool, fam, ctx, r, SOFT_SYL, undefined, lines);
      used.push(out.tpl);
      lines.push(out.line);
    }
    return lines;
  }

  function writeBridge(ctx, r) {
    const lines = [];
    for (let c = 0; c < 2; c++) {
      const a = choose(BRIDGE_T, r), b = choose(BRIDGE_T, r, [a]);
      const fam = choose(RHYMES.filter((f) => canEnd(f, a) && canEnd(f, b)), r);
      const l1 = fillFit([a], fam, ctx, r, SOFT_SYL, undefined, lines).line;
      const l2 = fillFit([b], fam, ctx, r, SOFT_SYL, undefined, [...lines, l1]).line;
      lines.push(l1, l2);
    }
    return lines;
  }

  // Ad-libs: 1-3 words in parentheses on their own line, after lines 4, 8,
  // 12 of a verse (never more than one per two lines), after line 2 or 4 of
  // a hook.
  function withAdlibs(lines, persona, r, every, p) {
    const out = [];
    lines.forEach((l, i) => {
      out.push(l);
      if ((i + 1) % every === 0 && r() < p) out.push(adlibLine(persona, r));
    });
    return out;
  }

  // ---------------------------------------------------------------- tags
  // At most three cues per tag; prose under a tag gets sung.
  const tag = (name, cues) => `[${name} | ${cues.filter(Boolean).slice(0, 3).join(", ")}]`;
  const section = (name, cues, lines) => [tag(name, cues), ...lines].join("\n");

  // --------------------------------------------------------------- write
  function write({ r, pick, lane, theme, persona, key, bpm } = {}) {
    if (typeof r !== "function") throw new Error("OneLyrics.write needs a seeded r()");
    const _pick = pick || ((arr) => arr[Math.floor(r() * arr.length)]);
    lane = lane === "rnb" || lane === "rap" ? lane : _pick(["rap", "rnb"], r);
    const T = typeof theme === "string" ? THEMES.find((t) => t.id === theme) : theme;
    const themeObj = T || _pick(THEMES.filter((t) => t.lanes.includes(lane)), r);
    const personaObj = (typeof persona === "string" ? PERSONAS[lane].find((p) => p.id === persona) : persona) || _pick(PERSONAS[lane], r);
    const ctx = { lane, theme: themeObj, persona: personaObj };

    const hook = writeHook(ctx, r);
    const structure = _pick(["intro-hook", "hook-first-switch", "tease"], r);
    const feature = lane === "rnb" && r() < 0.45;
    const featurePersona = feature ? _pick(PERSONAS.rap.filter((p) => p.gender !== personaObj.gender), r) : null;
    const rapped = lane === "rap" ? "rapped" : null;

    // One energy per song in the style field; contrast lives in the tags.
    const hookStyle = choose(personaObj.palette, r);
    const hookTexture = choose(TEXTURE, r, [hookStyle]);
    const hookCues = (variant) => {
      if (variant === "biggest") return ["biggest", choose(["belted", "octave up on the last line", "stacked harmonies"], r, [hookTexture]), hookTexture];
      if (variant === "cold") return [hookStyle, /dry/.test(hookStyle) ? "quiet" : "dry", "808 only then full beat"];
      if (variant === "tease") return ["two lines", "whispered", "808 only"];
      return [hookStyle, hookTexture, "full beat"];
    };
    const verseCues = (extraFirst) => [rapped || choose(personaObj.palette, r, [hookStyle]), choose(lane === "rap" ? personaObj.palette : ["quiet", "sparse", "conversational", "breathy"], r, [hookStyle]), extraFirst || choose(BEAT, r, ["drums cut"])];

    // Verses: 16 / 12 bars rapped, 8 / 4 lines sung; verse 2 shorter or a feature.
    const long = lane === "rap" ? 16 : 8, short = lane === "rap" ? (structure === "intro-hook" ? 12 : 8) : 4;
    const v1Bars = structure === "tease" ? short : long;
    const v2Bars = structure === "tease" ? long : short;
    const v1 = writeVerse(ctx, r, v1Bars, { oneliner: true });
    const v2 = feature
      ? writeVerse(ctx, r, 8, { callback: true, opener: v1.opener, lane: "rap" })
      : writeVerse(ctx, r, v2Bars, { callback: true, opener: v1.opener });
    const pre = writePre(ctx, r, hook.family);
    const bridge = writeBridge(ctx, r);

    const pAd = lane === "rap" ? 0.75 : 0.5;
    const hookBlock = (variant) => section("Hook", hookCues(variant), variant === "tease" ? hook.lines.slice(0, 2) : withAdlibs(hook.lines, personaObj, r, 2, variant === "biggest" ? 0.8 : 0.45));
    const verseBlock = (name, v, cues) => section(name, cues, withAdlibs(v.lines, v === v2 && feature ? featurePersona : personaObj, r, 4, pAd));
    const introSpoken = () => {
      const n = 1 + Math.floor(r() * 2);
      const out = [];
      for (let i = 0; i < n; i++) {
        const l = cap(choose(INTRO_SPOKEN, r).replace("{time}", choose(TIME, r)).replace("{title}", hook.title));
        if (!out.includes(l)) out.push(l);
      }
      return out;
    };
    // A voice by bar 2: spoken, or the title line whispered, or an ad-lib.
    const intro = () => (r() < 0.5
      ? section("Intro", ["spoken", "dry", `${2 + 2 * Math.floor(r() * 2)} bars`], introSpoken())
      : section("Intro", [choose(["title line whispered", "hook melody hummed", "ad-libs only"], r), "808 only", "2 bars"], [r() < 0.4 ? adlibLine(personaObj, r) : hook.lines[0]]));

    const v2Block = feature
      ? section("Verse 2", ["feature", `${featurePersona.gender} rapper`, choose(["call and response", "rapped", "pushed ahead of the beat", "laid-back behind the beat"], r)], withAdlibs(v2.lines, featurePersona, r, 4, 0.75))
      : verseBlock("Verse 2", v2, verseCues());
    const preBlock = section("Pre-Hook", ["rising", choose(["doubled", "stacked", "whispered then belted", "half-sung"], r), "drums cut on the last line"], pre);
    const bridgeBlock = section("Bridge", ["sparse", choose(["whispered", "half-sung", "falsetto", "breathy", "spoken"], r), "drums cut"], bridge);
    const switchBlock = tag("Beat Switch", [choose(["half-time", "double-time", "new 808", "808 only"], r), choose(["4 bars", "2 bars"], r), "melody stays"]);
    const outroBlock = section("Outro", [choose(["whispered", "half-sung", "spoken"], r), "drums cut", r() < 0.5 ? "fade" : "hard stop"], [hook.lines[0], hook.lines[0], adlibLine(personaObj, r)]);

    let parts;
    if (structure === "intro-hook") {
      // Intro 4 / Hook / V1 16 / Pre / Hook / V2 12 / Bridge / Hook / Outro: three hooks, ~2:50.
      parts = [intro(), hookBlock(), verseBlock("Verse 1", v1, verseCues("808 only then full beat")), preBlock, hookBlock(), v2Block, bridgeBlock, hookBlock("biggest"), outroBlock];
    } else if (structure === "hook-first-switch") {
      // Hook cold / V1 16 / Pre / Hook / V2 8 / Switch / Hook / Outro: three hooks, ~2:40.
      parts = [hookBlock("cold"), verseBlock("Verse 1", v1, verseCues()), preBlock, hookBlock(), v2Block, switchBlock, hookBlock("biggest"), outroBlock];
    } else {
      // Tease / V1 8 / Pre / Hook / V2 16 / Pre / Hook / Bridge / Hook / Outro: four statements, ~2:50.
      parts = [hookBlock("tease"), verseBlock("Verse 1", v1, verseCues("808 only")), preBlock, hookBlock(), v2Block, preBlock, hookBlock(), bridgeBlock, hookBlock("biggest"), outroBlock];
    }
    parts.push("[End]");

    return {
      lyrics: parts.join("\n\n"),
      hook: { lines: hook.lines, title: hook.title, family: hook.family },
      title: hook.title,
      persona: { ...personaObj, feature: featurePersona },
      theme: { id: themeObj.id, label: themeObj.label },
      lane, structure,
      style: personaObj.tag,
      key, bpm,
    };
  }

  const EXCLUDE = ["choir", "crowd vocals", "gang vocals", "vocal chops", "gospel"];
  const ALLOW = /\b(vocal|vocals|vocalist|rap|rapper|rapped|sing|sings|singing|singer|sung|hum|humming|hummed|lyric\w*|ad-?lib\w*|falsetto|harmonies|doubles|belt\w*|whisper\w*|spoken)\b/i;

  return { write, rhymes, lastWord, syllables, RHYMES, PERSONAS, THEMES, HOOK_T, VERSE_T, DELIVERY, TEXTURE, BEAT, EXCLUDE, ALLOW };
})();

if (typeof module !== "undefined") module.exports = OneLyrics;
