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
    un: ["run:p", "done:a", "undone:a", "number one:a c", "second to none:a c", "won:p", "outrun:p", "on the run:x", "just begun:c", "in the sun:x", "just for fun:x", "by one:x"],
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
  // ------------------------------------------------------------ the bank
  // Authored lines, per theme. hooks: { a: title line, b: rhyme line, alt:
  // three third lines (a question answered by the title, a one-word swap of
  // a, another rhyme), title, obj (the anchor verse 2 calls back), fam }.
  // rap: couplets { k: flex|detail|callback|turn, a, b, fam, i: internal
  // rhyme in a }. rnb: four-line stanzas. pre: 2-3 lines. bridge: 4 lines.
  // Each line sits inside the syllable windows (hook 4-9, rap <= 12, rnb
  // 7-11) and the end words of a couplet rhyme in the family named by fam.
  const BANK = {
    comeup: {
      hooks: [
        { a: "Rent was late, now I'm early", b: "Same corner, different jewelry", alt: ["Who's that? Now I'm Early", "Check was late, now I'm early", "Mom asleep, I'm up early"], title: "Now I'm Early", obj: "rent", fam: "early" },
        { a: "I ate off a napkin plan", b: "Signed it in pen, that's the whole brand", alt: ["What's the move? Napkin Plan", "Slept on a napkin plan", "Bus stop to a napkin plan"], title: "Napkin Plan", obj: "napkin", fam: "an" },
        { a: "Old job calling, I don't call back", b: "Took the bus there, left in all black", alt: ["Say what? I Don't Call Back", "Old boss calling, I don't call back", "Forty missed calls, I don't call back"], title: "I Don't Call Back", obj: "old job", fam: "ack" },
        { a: "Broke at nine, booked by eleven", b: "Mom's rent paid off one verse, heaven", alt: ["Where we at? Booked By Eleven", "Broke in June, booked by eleven", "Slept at nine, booked by eleven"], title: "Booked By Eleven", obj: "eleven", fam: "even" },
        { a: "Card got approved and I flinched", b: "Two zeros more and I still winced", alt: ["What's the word? I Flinched", "Wire got approved and I flinched", "Lobby said my name and I flinched"], title: "I Flinched", obj: "card", fam: "inch" },
        { a: "They held the door, I held the pen", b: "Laughed in the lobby back when", alt: ["Say less, I held the pen", "They held the phone, I held the pen", "What they want? I Held The Pen"], title: "I Held The Pen", obj: "pen", fam: "en" },
        { a: "Two phones, one charger, zero sleep", b: "Count it twice, then I count the week", alt: ["Where we at? Zero Sleep", "Two jobs, one charger, zero sleep", "Alarm at four, zero sleep"], title: "Zero Sleep", obj: "charger", fam: "ee" },
        { a: "Bus stop to valet", b: "Same shoes, I just walk straight", alt: ["Say what? Bus Stop To Valet", "Back seat to valet", "Who's that? Bus Stop To Valet"], title: "Bus Stop To Valet", obj: "valet", fam: "ay" },
        { a: "Hood up, name on the list", b: "Last year they said I don't exist", alt: ["What they want? Name On The List", "Hood up, mom on the list", "Hood up, whole block on the list"], title: "Name On The List", obj: "hood", fam: "ist" },
        { a: "I keep receipts in my hoodie", b: "Ask the lobby who's the rookie", alt: ["Who's that? In My Hoodie", "I keep the plan in my hoodie", "Mom's rent in my hoodie"], title: "In My Hoodie", obj: "hoodie", fam: "oodie" },
        { a: "Spare key to a place I pay for", b: "First time nobody at the door", alt: ["Say less, a place I pay for", "Spare key to a car I pay for", "What's the move? A Place I Pay For"], title: "Place I Pay For", obj: "spare key", fam: "ore" },
        { a: "Nine to five, now it's five to nine", b: "Rent paid twice, that's a good sign", alt: ["Say what? Five To Nine", "Nine to five, now I sign", "Mom asleep, I'm up five to nine"], title: "Five To Nine", obj: "five", fam: "ine" },
      ],
      rap: [
        { k: "flex", i: true, a: "Same hoodie, new tax bracket, same bus route", b: "Mom asks if I ate, I say sold out", fam: "out" },
        { k: "detail", a: "Forty in the glovebox, gas light on", b: "Rent's due Friday, Friday's gone", fam: "on" },
        { k: "flex", a: "They laughed in the lobby, now they hold the door", b: "I don't say thank you, I say what for", fam: "ore" },
        { k: "detail", i: true, a: "Two phones, one charger, both of them mom", b: "One says eat, one says stay calm", fam: "om" },
        { k: "turn", a: "I got the keys and I still knock first", b: "Scared the good part is the part that hurts", fam: "urst" },
        { k: "callback", a: "Wrote the plan on a napkin, signed it in pen", b: "Lobby knows my name, go ask them when", fam: "en" },
        { k: "flex", i: true, a: "Took the bus here, leaving in a two-door", b: "Same stop, same corner, different score", fam: "ore" },
        { k: "detail", a: "Alarm at four, bus pass in the hoodie", b: "Valet asks my name now, I say rookie", fam: "oodie" },
        { k: "flex", a: "Spare key to a spot I pay for", b: "First time it's quiet when I close the door", fam: "ore" },
        { k: "callback", i: true, a: "Nine to five flipped, now it's five to nine", b: "Receipts in the hoodie, every line", fam: "ine" },
      ],
      rnb: [
        { l: ["You knew me when the bus ran late", "Held my hand at the turnstile gate", "Now the car's warm, you still wait", "Same front seat, different plate"], fam: "ate" },
        { l: ["I sent half the check back home", "Kept the other half for the phone", "So I could call you from the road alone", "Say I made it in a low tone"], fam: "own" },
        { l: ["I count the zeros twice at night", "Then I count your texts in the light", "You were there when the gas light hit", "Now we park wherever we fit"], fam: "ight" },
        { l: ["Mama said be home by ten", "I'm home, just a different den", "You still call me by the old name", "I let you, that's the whole game"], fam: "en" },
        { l: ["I bought the couch you slept on first", "You said it's ugly, that's the worst", "Kept it anyway for the memory", "On the hungry days, you fed me"], fam: "urst" },
        { l: ["First flight, I gave you the window", "You pressed your face to the glass slow", "You rode the bus with me, so", "Now you ride front, let 'em know"], fam: "o" },
      ],
      pre: [
        { l: ["And if they ask where I was", "Say the bus, say because"], fam: "uz" },
        { l: ["Count it twice, say it slow", "Rent paid, let 'em know"], fam: "o" },
        { l: ["Mom, pick up, it's me", "Yeah, it cleared, it's free"], fam: "ee" },
        { l: ["Hood up, hand on the door", "Said I'd be back for more"], fam: "ore" },
      ],
      bridge: [
        { l: ["I still sleep with the lights on", "Scared the good part's already gone", "Count my keys like they're borrowed", "Pay the rent twice for tomorrow"], fam: "on" },
        { l: ["Mom still asks if I'm eating", "I say yes and I keep breathing", "She don't know about the two phones", "She just knows that I came home"], fam: "eating" },
        { l: ["I got the keys and I still knock", "Scared they'll say it's not my block", "Then the lobby says my name slow", "And I walk in like I know"], fam: "ock" },
        { l: ["Everything I own fits in a hoodie", "Everything I am they called rookie", "Kept the receipts for the day they ask", "Kept the plan in the same bag"], fam: "oodie" },
      ],
    },
    latetext: {
      hooks: [
        { a: "Three dots at 2am", b: "Say it or don't, I know who I am", alt: ["What's the move? Three Dots At 2am", "Three dots at 3am", "Who's that? Three Dots At 2am"], title: "Three Dots At 2am", obj: "dots", fam: "am" },
        { a: "Left me on read, I'm outside", b: "Phone face down, I don't hide", alt: ["Say what? I'm Outside", "Left me on seen, I'm outside", "Two words from you, I'm outside"], title: "I'm Outside", obj: "read", fam: "ide" },
        { a: "Don't text me, pull up", b: "Porch light on, I'm up", alt: ["What's the word? Pull Up", "Don't call me, pull up", "Who's that? Pull Up"], title: "Pull Up", obj: "text", fam: "up" },
        { a: "Drafted it four times, sent nothing", b: "You said wyd, I said something", alt: ["Say what? Sent Nothing", "Drafted all night, sent nothing", "Drafted a page, sent nothing"], title: "Sent Nothing", obj: "draft", fam: "money" },
        { a: "One bar of signal, still you", b: "2am and the glow's blue", alt: ["Who's that? Still You", "Two percent battery, still you", "Where we at? Still You"], title: "Still You", obj: "signal", fam: "oo" },
        { a: "You say goodnight, I say what time", b: "Ringer up like it's a crime", alt: ["Say what? I Say What Time", "You say you're tired, I say what time", "You say goodnight, I say what line"], title: "I Say What Time", obj: "goodnight", fam: "ine" },
        { a: "Read receipt at 2:47", b: "Left it there, that's my confession", alt: ["Who's that? At 2:47", "Screenshot at 2:47", "Say what? At 2:47"], title: "At 2:47", obj: "receipt", fam: "even" },
        { a: "Phone on silent, you got through", b: "Porch light on, I know what to do", alt: ["Say less, you got through", "Phone on silent, she got through", "Phone on do-not-disturb, you got through"], title: "You Got Through", obj: "phone", fam: "oo" },
        { a: "I don't double text, I show up", b: "Two knocks and a coffee cup", alt: ["What's the move? I Show Up", "I don't double text, I pull up", "Say what? I Show Up"], title: "I Show Up", obj: "double text", fam: "up" },
        { a: "Your name lit up the ceiling", b: "Lock screen on, I'm still feeling", alt: ["Who's that? Lit Up The Ceiling", "Your text lit up the ceiling", "Your call lit up the ceiling"], title: "Lit Up The Ceiling", obj: "ceiling", fam: "eeling" },
        { a: "Lock screen says last seen: me", b: "Online at three, so are we", alt: ["Say what? Last Seen Me", "Who's that? Last Seen Me", "Lock screen says last seen: we"], title: "Last Seen Me", obj: "lock screen", fam: "ee" },
        { a: "Text it now or text it never", b: "Phone at two, won't last forever", alt: ["What's the word? Text It Never", "Text it now or say it never", "Call me now or call me never"], title: "Text It Never", obj: "text", fam: "ever" },
      ],
      rap: [
        { k: "flex", i: true, a: "I don't double text, I double park", b: "Porch light on, you know the mark", fam: "ark" },
        { k: "detail", a: "Three dots, then nothing, then the dots again", b: "I been in the lot since I don't know when", fam: "en" },
        { k: "flex", a: "Phone on silent and you still got through", b: "Two words from you and I'm at your door, boo", fam: "oo" },
        { k: "detail", i: true, a: "Lock screen lit, the ceiling turned blue", b: "2am and the room smells like you", fam: "oo" },
        { k: "turn", a: "I typed it four times and I sent none", b: "Deleted a page before the sun", fam: "un" },
        { k: "callback", a: "You said goodnight, I said what time", b: "Ringer up, that's a crime", fam: "ine" },
        { k: "flex", a: "Left me on read, so I read the room", b: "Then I pulled up, that's a plot twist, boom", fam: "oom" },
        { k: "detail", a: "One bar of signal in the back seat", b: "Your name on the screen, on repeat", fam: "eat" },
        { k: "flex", i: true, a: "I don't wait up, I stay up", b: "Coffee cold, I don't say what", fam: "up" },
        { k: "callback", a: "Three dots at two, that's the whole plan", b: "You typed, I read, I know who I am", fam: "am" },
      ],
      rnb: [
        { l: ["You typed for a minute, I read it in one", "Said nothing back and the night's not done", "Phone on the pillow, face up", "Waiting on the three dots, don't give up"], fam: "un" },
        { l: ["It's 2am, your name's on the glass", "I tell myself to let it pass", "Then the dots come up and I'm alive", "Two words from you and I'll drive"], fam: "ass" },
        { l: ["Say it slow, I'll read it twice", "Say it wrong, I'll still be nice", "Say nothing and I'll pull up anyway", "Porch light on, I know the way"], fam: "ice" },
        { l: ["One bar of signal in the back seat", "I hear the buzz and I feel it in my feet", "Your goodnight came at a quarter to four", "Mine says I'm already at your door"], fam: "eat" },
        { l: ["I keep the ringer on for you", "Even when I say I'm through", "Screenshot of the last thing you said", "I read it in bed before bed"], fam: "oo" },
        { l: ["Phone on silent, still you got through", "Lock screen lit the ceiling blue", "Don't double text, just come outside", "I left the door unlocked, don't hide"], fam: "oo" },
      ],
      pre: [
        { l: ["Say it now, say it twice", "Then pull up, that's my advice"], fam: "ice" },
        { l: ["Phone face up, ringer high", "And if it's you, I'm outside"], fam: "ide" },
        { l: ["Three dots, I'm counting", "Two words and I'm bouncing"], fam: "ounting" },
        { l: ["Left on read, that's fine", "I know the way, I know the time"], fam: "ine" },
      ],
      bridge: [
        { l: ["I typed it four times and sent nothing", "Then I said hey like it's something", "You said hey back like it's small", "And that's the whole call"], fam: "money" },
        { l: ["It's quiet and the screen's still warm", "I keep the ringer on like a storm", "Not because you'll call tonight", "Just in case I'm right"], fam: "orm" },
        { l: ["I don't miss you, I miss the dots", "The little pause before the thought", "The way you said goodnight at two", "Then said one more thing, then you"], fam: "ots" },
        { l: ["I'd rather be left on read", "Than left out of your head", "So text me nothing, I'll take that", "I'll read nothing right back"], fam: "ed" },
      ],
    },
    exwatching: {
      hooks: [
        { a: "You watch my story, I don't watch back", b: "Blocked you on Tuesday, you came back", alt: ["Who's that? I Don't Watch Back", "You watch my story, I don't post back", "Say what? I Don't Watch Back"], title: "I Don't Watch Back", obj: "story", fam: "ack" },
        { a: "Your name in my views again", b: "Same time every night, it's ten", alt: ["What's the word? My Views Again", "Your friend in my views again", "Your new one in my views again"], title: "My Views Again", obj: "views", fam: "en" },
        { a: "You kept the hoodie, I kept the glow", b: "Ask your friends, they know", alt: ["Say less, I kept the glow", "You kept the ring, I kept the glow", "Who's that? I Kept The Glow"], title: "I Kept The Glow", obj: "hoodie", fam: "o" },
        { a: "Blocked you twice, still you found me", b: "Drove past slow, that's how you crowned me", alt: ["Who's that? Still You Found Me", "Blocked you Tuesday, still you found me", "Changed my number, still you found me"], title: "Still You Found Me", obj: "block", fam: "ound me" },
        { a: "I'm doing fine, go ask your friends", b: "They watched the story till the end", alt: ["Say what? Go Ask Your Friends", "I'm doing great, go ask your friends", "I'm outside, go ask your friends"], title: "Go Ask Your Friends", obj: "friends", fam: "en" },
        { a: "You said I'd fall off, I fell up", b: "New car, same spot, pull up", alt: ["What's the move? I Fell Up", "You said I'd fall off, I went up", "Who's that? I Fell Up"], title: "I Fell Up", obj: "fall", fam: "up" },
        { a: "Same two mutuals, same bar", b: "You saw the pic, you know the car", alt: ["Say what? Same Bar", "Same two mutuals, same car", "Who's that? Same Bar"], title: "Same Bar", obj: "mutuals", fam: "ar" },
        { a: "New ride, same spot, you know the plate", b: "Drive by slow, you're never late", alt: ["Who's that? You Know The Plate", "New ride, old spot, you know the plate", "Say less, you know the plate"], title: "You Know The Plate", obj: "plate", fam: "ate" },
        { a: "I post at midnight so you see it", b: "You watch twice, I don't need it", alt: ["What's the word? So You See It", "I post at noon so you see it", "I pulled up loud so you see it"], title: "So You See It", obj: "post", fam: "ee it" },
        { a: "You in my old hoodie, that's cute", b: "I'm in a new city, that's the truth", alt: ["Say what? That's Cute", "Your new one in my hoodie, that's cute", "Who's that? That's Cute"], title: "That's Cute", obj: "hoodie", fam: "oot" },
        { a: "I kept one picture, won't say which", b: "You kept my number, didn't switch", alt: ["Who's that? Won't Say Which", "I kept one hoodie, won't say which", "Kept one voicemail, won't say which"], title: "Won't Say Which", obj: "picture", fam: "itch" },
        { a: "I know your car by the sound", b: "Still don't turn around", alt: ["Say what? By The Sound", "I know your walk by the sound", "Who's that? By The Sound"], title: "By The Sound", obj: "car", fam: "own2" },
      ],
      rap: [
        { k: "flex", i: true, a: "You watch my story twice, I watch the clock", b: "New car, same spot, same block", fam: "ock" },
        { k: "detail", a: "Your name in my views at 11:11", b: "Same time every night, that's your heaven", fam: "even" },
        { k: "flex", a: "You kept the hoodie, I kept the glow", b: "Your friends still like my posts, so", fam: "o" },
        { k: "detail", i: true, a: "Blocked you on a Tuesday, unblocked by Friday", b: "Then I drove past your street the long way", fam: "ay" },
        { k: "turn", a: "I check who watched, then I hate that I checked", b: "Scroll to your name, then I hit back and forget", fam: "et" },
        { k: "callback", a: "I post at midnight so you see it", b: "Two views in, I don't need it", fam: "ee it" },
        { k: "flex", a: "I'm doing fine, go ask your friends", b: "They watched the whole story, they know how it ends", fam: "en" },
        { k: "detail", a: "Same two mutuals at the same bar", b: "You saw the pic, you clocked the car", fam: "ar" },
        { k: "flex", i: true, a: "You said I'd fall off, I fell up, way up", b: "New ride, same spot, you pulled up", fam: "up" },
        { k: "callback", a: "Still know your car by the sound", b: "Still don't turn around", fam: "own2" },
      ],
      rnb: [
        { l: ["You watch every story, I know your name", "Little grey circle, it's always the same", "I don't post for you, I swear", "Okay, that one, but that's rare"], fam: "ame" },
        { l: ["Your new one wears my old hoodie", "It looks better on her, truly", "I'm not mad, I'm just awake", "Three in the morning, give or take"], fam: "oodie" },
        { l: ["I kept one picture, I won't say which", "You kept my number, you didn't switch", "We don't talk now, we just watch", "Same two bars, the same old notch"], fam: "itch" },
        { l: ["Blocked you twice and found you once", "Scrolling past like it's by chance", "You drove by slow on Sunday", "I pretended it was any day"], fam: "once" },
        { l: ["I'm doing fine, I'm really fine", "Your friend asked and I said fine", "Then I checked who watched at three", "And your name was waiting there for me"], fam: "ine" },
        { l: ["I know your car by the sound", "I know your laugh across the town", "I don't turn around, I don't", "You want me to turn, I won't"], fam: "own2" },
      ],
      pre: [
        { l: ["And if you're watching, hey", "I'm doing fine, okay"], fam: "ay" },
        { l: ["Views in, I don't check", "Okay, I checked, what the heck"], fam: "eck" },
        { l: ["Same bar, same two friends", "Watch how this one ends"], fam: "en" },
        { l: ["New ride, you know the plate", "Drive by, don't be late"], fam: "ate" },
      ],
      bridge: [
        { l: ["I check who watched, then I hate that I checked", "Then I check again, what did I expect", "Three in the morning, grey circle, your name", "I'm doing fine, we're not the same"], fam: "et" },
        { l: ["You kept the hoodie, keep it", "I kept the glow, I don't need it", "We don't talk, we just watch", "I'm fine, you're fine, that's a lot"], fam: "ee it" },
        { l: ["I posted that one for you", "Just so you'd see I pulled through", "Not to hurt, just to show", "I'm good, and I know you know"], fam: "oo" },
        { l: ["I still know your car by the sound", "Still don't turn my head around", "I kept one picture, won't say which", "Some lights I won't switch"], fam: "own2" },
      ],
    },
    loyalty: {
      hooks: [
        { a: "Same five since fourteen", b: "Same chat, same team", alt: ["Who's that? Same Five Since Fourteen", "Say what? Same Five Since Fourteen", "Same block, same five, same team"], title: "Same Five Since Fourteen", obj: "fourteen", fam: "een" },
        { a: "I don't add seats, I add plates", b: "Day ones eat first, that's the rate", alt: ["What's the move? I Add Plates", "I don't add chairs, I add plates", "Say less, I add plates"], title: "I Add Plates", obj: "plates", fam: "ate" },
        { a: "Ain't call at four, don't call at noon", b: "The ones who drove me get the room", alt: ["Say what? Don't Call At Noon", "Ain't call at three, don't call at noon", "Who's that? Don't Call At Noon"], title: "Don't Call At Noon", obj: "noon", fam: "oon" },
        { a: "Day ones got keys, not invites", b: "Spare key, porch light", alt: ["Who's that? Keys Not Invites", "Day ones got keys, not flights", "Say less, keys not invites"], title: "Keys Not Invites", obj: "keys", fam: "ight" },
        { a: "Same number since the flip phone", b: "Still pick up, that's the tone", alt: ["What's the word? Since The Flip Phone", "Same group chat since the flip phone", "Same five since the flip phone"], title: "Since The Flip Phone", obj: "number", fam: "own" },
        { a: "Rather be broke with the same five", b: "Five seats, one drive", alt: ["Say what? With The Same Five", "Rather be late with the same five", "Who's that? With The Same Five"], title: "With The Same Five", obj: "five", fam: "ive" },
        { a: "Count who stayed on one hand", b: "Still got fingers left, understand", alt: ["What's the move? On One Hand", "Count who called on one hand", "Count who drove on one hand"], title: "On One Hand", obj: "hand", fam: "and" },
        { a: "Day one in the front seat", b: "Everyone else, back seat", alt: ["Who's that? In The Front Seat", "Mom in the front seat", "Say less, in the front seat"], title: "In The Front Seat", obj: "front seat", fam: "eat" },
        { a: "The chat got five, the list got five", b: "Same porch, same drive", alt: ["Say what? The List Got Five", "The chat got five, the car got five", "Who's that? The List Got Five"], title: "The List Got Five", obj: "list", fam: "ive" },
        { a: "They knew me broke, that's the whole list", b: "Everybody else, dismissed", alt: ["What's the word? That's The Whole List", "They fed me broke, that's the whole list", "Who's that? That's The Whole List"], title: "That's The Whole List", obj: "list", fam: "ist" },
        { a: "Pay the table so nobody asks", b: "Same five, same laughs", alt: ["Say less, so nobody asks", "I pay the tab so nobody asks", "Who's that? So Nobody Asks"], title: "So Nobody Asks", obj: "table", fam: "asks" },
        { a: "Spare key, same porch, same crew", b: "Twenty years and nothing new", alt: ["Who's that? Same Crew", "Spare key, same block, same crew", "Say what? Same Crew"], title: "Same Crew", obj: "porch", fam: "oo" },
      ],
      rap: [
        { k: "flex", i: true, a: "Same five since fourteen, same chat, same seats", b: "Nobody new eats where my day ones eat", fam: "eat" },
        { k: "detail", a: "Spare key on the porch under the same mat", b: "Twenty years, nobody moved that", fam: "at" },
        { k: "flex", a: "I don't add seats, I add plates", b: "Same table, same faces, bigger weights", fam: "ate" },
        { k: "detail", i: true, a: "Ain't call at four? Don't call at noon", b: "Only the ones who drove me get the room", fam: "oon" },
        { k: "turn", a: "I pay the table so nobody has to ask", b: "Then I lie that I'm fine behind the glass", fam: "ask" },
        { k: "callback", a: "Count who stayed on one hand, got fingers left", b: "That's the whole list, that's the whole set", fam: "et" },
        { k: "flex", a: "My day ones got keys, not invites", b: "Front seat, porch light, every night", fam: "ight" },
        { k: "detail", a: "Same number since the flip phone", b: "Still pick up, same tone", fam: "own" },
        { k: "flex", i: true, a: "Rather be broke with the same five", b: "Than rich in a room where nobody's alive", fam: "ive" },
        { k: "callback", a: "They knew me broke, that's the whole list", b: "Everybody after that, dismissed", fam: "ist" },
      ],
      rnb: [
        { l: ["You knew my name before the name", "You knew the car before the chain", "Same five seats and the same old song", "You been riding front seat all along"], fam: "ame" },
        { l: ["I don't add seats, I add plates", "You've been here since the first dates", "Same porch light and the same spare key", "You never had to ask for me"], fam: "ate" },
        { l: ["You stayed when the money got strange", "You stayed when the number changed", "Same five, same chat, the same tone", "I never once felt alone"], fam: "ange" },
        { l: ["I count who stayed on one hand", "You're the thumb, you understand", "Everybody else can wait", "You get the front seat, you get the plate"], fam: "and" },
        { l: ["I pay the table so nobody asks", "I hold it down behind the glass", "But you see through it every time", "You say sit down, you say you're fine"], fam: "ass" },
        { l: ["Same number since the flip phone", "Same voicemail, same ringtone", "I'd pick up at four, at noon", "I'd pick up calling from the moon"], fam: "own" },
      ],
      pre: [
        { l: ["Same five, say it loud", "Same five, I'm proud"], fam: "oud" },
        { l: ["Spare key, porch light", "Day ones, every night"], fam: "ight" },
        { l: ["Count 'em on one hand", "Still here, understand"], fam: "and" },
        { l: ["If you drove me, come eat", "Front seat, front seat"], fam: "eat" },
      ],
      bridge: [
        { l: ["I pay the table so nobody asks", "I lie that I'm fine behind the glass", "But the same five see right through", "They say sit down, we got you"], fam: "ass" },
        { l: ["Count who stayed on one hand", "Five fingers, that's the whole band", "Everybody else is weather", "The same five is forever"], fam: "and" },
        { l: ["I still owe the ones who drove me", "Gas money, couches, the whole story", "I'll never pay that back right", "So I keep the porch light on at night"], fam: "ory" },
        { l: ["Rather be broke with the same five", "Than rich with nobody to drive", "Same number since the flip phone", "If you call at four, I'm home"], fam: "ive" },
      ],
    },
    outside: {
      hooks: [
        { a: "I'm outside, bring a jacket", b: "It's a long one, I planned it", alt: ["Who's that? Bring A Jacket", "I'm outside, grab a jacket", "Say what? Bring A Jacket"], title: "Bring A Jacket", obj: "jacket", fam: "acket" },
        { a: "Pulled up at two, leaving whenever", b: "Shoes still clean, that's clever", alt: ["What's the move? Leaving Whenever", "Pulled up at ten, leaving whenever", "Pulled up alone, leaving whenever"], title: "Leaving Whenever", obj: "whenever", fam: "ever" },
        { a: "List said four, we came as nine", b: "Side door, front of the line", alt: ["Say what? We Came As Nine", "List said two, we came as nine", "List said four, we came at nine"], title: "We Came As Nine", obj: "list", fam: "ine" },
        { a: "I don't dance, I supervise", b: "Back wall, tints, that's the prize", alt: ["Who's that? I Supervise", "I don't dance, I advise", "Say less, I supervise"], title: "I Supervise", obj: "dance", fam: "ize" },
        { a: "Phone at one percent, still here", b: "Last call came and went, still here", alt: ["What's the word? Still Here", "Phone at two percent, still here", "Phone dead, still here"], title: "Still Here", obj: "phone", fam: "ere" },
        { a: "We don't take pics, we take the table", b: "Sparklers out, that's the label", alt: ["Say what? We Take The Table", "Skip the pics, we take the table", "Who's that? We Take The Table"], title: "We Take The Table", obj: "table", fam: "able" },
        { a: "Tints up, speakers down, we here", b: "Pull around the back, it's clear", alt: ["Who's that? We Here", "Tints up, windows down, we here", "Say less, we here"], title: "We Here", obj: "tints", fam: "ere" },
        { a: "Forty deep at the side door", b: "Guy with the list said what for", alt: ["What's the move? At The Side Door", "Forty deep at the back door", "Twenty deep at the side door"], title: "At The Side Door", obj: "side door", fam: "ore" },
        { a: "The valet knows the car, not me", b: "That's how I like it, VIP", alt: ["Say what? The Car Not Me", "The valet knows the plate, not me", "Who's that? The Car Not Me"], title: "The Car Not Me", obj: "valet", fam: "ee" },
        { a: "Driver pulled up, I'm not done", b: "Lights came on, that's step one", alt: ["Who's that? I'm Not Done", "Driver called twice, I'm not done", "Say less, I'm not done"], title: "I'm Not Done", obj: "driver", fam: "un" },
        { a: "Shoes still clean at four", b: "That's the point of the back door", alt: ["What's the word? Still Clean At Four", "Hair still right at four", "Who's that? Still Clean At Four"], title: "Still Clean At Four", obj: "shoes", fam: "ore" },
        { a: "Last bottle got sparklers on it", b: "My name on the list, my name on it", alt: ["Say what? Sparklers On It", "Last bottle got my name on it", "Who's that? Sparklers On It"], title: "Sparklers On It", obj: "bottle", fam: "on it" },
      ],
      rap: [
        { k: "flex", i: true, a: "I'm outside, bring a jacket, it's a long one", b: "Pulled up at two, the night's a young one", fam: "un" },
        { k: "detail", a: "Tints up, windows down, forty deep", b: "Shoes still clean, phone on three", fam: "ee" },
        { k: "flex", a: "I don't dance, I supervise the whole floor", b: "Back wall, back booth, back door", fam: "ore" },
        { k: "detail", i: true, a: "List said four, we came as nine, side door", b: "Guy with the clipboard said what for", fam: "ore" },
        { k: "turn", a: "I'm outside so I don't sit with it", b: "The louder it gets, the less I'm in it", fam: "it" },
        { k: "callback", a: "We don't take pics, we take the table", b: "Sparklers on the bottle, that's the label", fam: "able" },
        { k: "flex", a: "The valet knows the car, not my face", b: "That's the point, that's the whole place", fam: "ace" },
        { k: "detail", a: "Phone at one percent, lights came on", b: "Last call came, I'm still not gone", fam: "on" },
        { k: "flex", i: true, a: "Driver called twice, I'm not done, son", b: "Leaving whenever, that's step one", fam: "un" },
        { k: "callback", a: "Shoes still clean at four, that's the back door", b: "Forty deep and we want more", fam: "ore" },
      ],
      rnb: [
        { l: ["Bring a jacket, it's a long one", "Pull up at two, we're not done", "Tints up, the whole car's warm", "Your hand on my knee, that's the norm"], fam: "un" },
        { l: ["The lights came on and I stayed", "You said leave and I said wait", "Driver's out front, let him wait", "One more song and one more take"], fam: "ay" },
        { l: ["Phone at one percent, still here", "Lights too bright and you're too near", "Say the word and we go, you know", "Or we stay till the lights go low"], fam: "ere" },
        { l: ["I don't dance, I watch you dance", "Back wall, then I take my chance", "Then you pull me in by the sleeve", "And I forget I planned to leave"], fam: "ance" },
        { l: ["We don't take pics, we take the table", "You take the booth, I take the label", "Sparklers out, your face lit gold", "That's the only night I want to hold"], fam: "able" },
        { l: ["My shoes are still clean at four", "Back seat, side street, the back door", "You said one more and I said okay", "That's how we end up here every day"], fam: "ore" },
      ],
      pre: [
        { l: ["Lights up, say when", "We going out again"], fam: "en" },
        { l: ["Jacket on, let's go", "Side door, say hello"], fam: "o" },
        { l: ["Tints up, phone low", "Pull around, let 'em know"], fam: "o" },
        { l: ["Last call, not done", "Driver's here, step one"], fam: "un" },
      ],
      bridge: [
        { l: ["I'm outside so I don't sit with it", "The louder it gets, the less I'm in it", "I'm the last one here most nights", "I'm not here, I'm just the lights"], fam: "it" },
        { l: ["I text the chat so I'm not alone", "Then I'm in a room full and on my phone", "Everybody's here, nobody's here", "I'll leave whenever, that's clear"], fam: "own" },
        { l: ["Phone at one percent, I'm fine", "Lights came on, I'm fine", "Driver's out front, I'm fine", "Say it enough and it's mine"], fam: "ine" },
        { l: ["Shoes still clean, that's the plan", "Don't ask how I am, I can't", "Last bottle, last song, last call", "And then I'm nobody at all"], fam: "an" },
      ],
    },
    newmoney: {
      hooks: [
        { a: "My accountant texts like an ex", b: "Wyd, where's the checks", alt: ["Who's that? Texts Like An Ex", "My tax guy texts like an ex", "Say what? Texts Like An Ex"], title: "Texts Like An Ex", obj: "accountant", fam: "ex" },
        { a: "Two cars, one parking spot", b: "New problems, that's a lot", alt: ["What's the move? One Parking Spot", "Two cars, one driveway, one spot", "Who's that? One Parking Spot"], title: "One Parking Spot", obj: "car", fam: "ot" },
        { a: "The wire cleared, I felt nothing", b: "Zeros moved, that's something", alt: ["Say what? I Felt Nothing", "The wire cleared, I said nothing", "The check cleared, I felt nothing"], title: "I Felt Nothing", obj: "wire", fam: "money" },
        { a: "Bought the spot next door for the quiet", b: "Still can't sleep, don't deny it", alt: ["Who's that? For The Quiet", "Bought the lot next door for the quiet", "Say less, for the quiet"], title: "For The Quiet", obj: "spot", fam: "iet" },
        { a: "I pay people to say I'm fine", b: "Three on payroll, one on the line", alt: ["What's the word? To Say I'm Fine", "I pay people to say it's fine", "Who's that? To Say I'm Fine"], title: "To Say I'm Fine", obj: "people", fam: "ine" },
        { a: "Tax man knows my first name", b: "Mom still thinks it's a game", alt: ["Say what? Knows My First Name", "Tax man knows my middle name", "Valet knows my first name"], title: "Knows My First Name", obj: "tax man", fam: "ame" },
        { a: "Still check the price, then say yes", b: "Old habits in a new address", alt: ["Who's that? Then Say Yes", "Still check the tag, then say yes", "Say less, then say yes"], title: "Then Say Yes", obj: "price", fam: "ess" },
        { a: "Got the bag, lost the appetite", b: "Menu's long, I'm not hungry tonight", alt: ["What's the move? Lost The Appetite", "Got the wire, lost the appetite", "Who's that? Lost The Appetite"], title: "Lost The Appetite", obj: "bag", fam: "ight" },
        { a: "Safe in the wall, nothing in it yet", b: "That's the plan, not a threat", alt: ["Say what? Nothing In It Yet", "Safe in the floor, nothing in it yet", "Who's that? Nothing In It Yet"], title: "Nothing In It Yet", obj: "safe", fam: "et" },
        { a: "New money, same sleep schedule", b: "Zeros up, still can't settle", alt: ["Who's that? Same Sleep Schedule", "New money, same bus schedule", "New address, same sleep schedule"], title: "Same Sleep Schedule", obj: "schedule", fam: "edule" },
        { a: "The decimal moved, I didn't", b: "Same hoodie, same kitchen", alt: ["Say what? I Didn't", "The comma moved, I didn't", "Who's that? I Didn't"], title: "I Didn't", obj: "decimal", fam: "idn't" },
        { a: "Receipt longer than the menu", b: "Then I tipped the whole venue", alt: ["What's the word? Longer Than The Menu", "Receipt taller than the menu", "Who's that? Longer Than The Menu"], title: "Longer Than The Menu", obj: "receipt", fam: "enu" },
      ],
      rap: [
        { k: "flex", i: true, a: "My accountant texts me like an ex", b: "Wyd, where's the checks, what's next", fam: "ex" },
        { k: "detail", a: "Wire cleared at nine, I felt nothing at ten", b: "Checked it twice, then I checked it again", fam: "en" },
        { k: "flex", a: "Tax man knows my first name now", b: "Mom still asks what I do, I say wow", fam: "ow" },
        { k: "detail", i: true, a: "Two cars, one spot, that's the whole fight", b: "Valet parked the second one out of sight", fam: "ight" },
        { k: "turn", a: "The more it clears, the less I feel it", b: "Got the bag and I can't feel it", fam: "eel it" },
        { k: "callback", a: "Bought the spot next door for the quiet", b: "Still up at four, don't deny it", fam: "iet" },
        { k: "flex", a: "Receipt longer than the menu, tipped the room", b: "Called my mom from the lobby, she said who", fam: "oom" },
        { k: "detail", a: "Safe in the wall with nothing in it yet", b: "That's the plan, not a threat", fam: "et" },
        { k: "flex", i: true, a: "Still check the price, still say yes", b: "Old habits, new address", fam: "ess" },
        { k: "callback", a: "Got the bag, lost the appetite", b: "Menu's long, nothing's right tonight", fam: "ight" },
      ],
      rnb: [
        { l: ["The wire cleared and I called you first", "Not the bank, not the accountant, you first", "You said eat something, I said I can't", "You said come home then, and I ran"], fam: "irst" },
        { l: ["Two cars and one parking spot", "You still take the bus, that's a lot", "You say it's fine, you like the walk", "I say get in, we don't have to talk"], fam: "ot" },
        { l: ["New money and the same old sleep", "4am, counting what I keep", "You roll over, say put it down", "I put it down, I'm all yours now"], fam: "eep" },
        { l: ["Bought the spot next door for the quiet", "Turns out that you were the quiet", "Now I pay for both, I don't mind", "Two doors, one key, that's by design"], fam: "iet" },
        { l: ["I pay people to say I'm fine", "You say nothing, just hold the line", "The decimal moved and I didn't", "You moved closer, I admit it"], fam: "ine" },
        { l: ["Receipt longer than the menu", "I tip the whole room so they'll let you", "Sit by me when the lights are up", "Lost the appetite, kept the cup"], fam: "enu" },
      ],
      pre: [
        { l: ["Zeros up, say when", "Count it, count again"], fam: "en" },
        { l: ["Wire cleared, I'm fine", "Say it one more time"], fam: "ine" },
        { l: ["Two cars, one spot", "That's a lot, that's a lot"], fam: "ot" },
        { l: ["Safe in the wall, nothing yet", "Patience, that's the bet"], fam: "et" },
      ],
      bridge: [
        { l: ["The more it clears, the less I feel", "Paid the whole table, still can't eat the meal", "Got the bag and lost the appetite", "Lights off, still up tonight"], fam: "eal" },
        { l: ["I pay people to say I'm fine", "Then I check the price one more time", "Mom still thinks it's a phase", "Maybe she's right, maybe I'm in a daze"], fam: "ine" },
        { l: ["Safe in the wall, nothing in it", "That's the plan, give it a minute", "New money, same old sleep", "Some things you can't buy, some you keep"], fam: "innit" },
        { l: ["Two cars, one spot, that's the fight", "Nobody to drive the second one tonight", "Receipt longer than the menu", "Still I'm the only one in the venue"], fam: "ight" },
      ],
    },
    longdistance: {
      hooks: [
        { a: "Two time zones, one ringtone", b: "Still pick up, I'm not alone", alt: ["Who's that? One Ringtone", "Two time zones, same ringtone", "Say what? One Ringtone"], title: "One Ringtone", obj: "ringtone", fam: "own" },
        { a: "Fly out at six, yours by nine", b: "Boarding pass in my back pocket, fine", alt: ["What's the move? Yours By Nine", "Fly out at two, yours by nine", "Land at six, yours by nine"], title: "Yours By Nine", obj: "six", fam: "ine" },
        { a: "I don't miss flights, I miss you", b: "Gate twenty-two, I'll be through", alt: ["Say what? I Miss You", "I don't miss calls, I miss you", "Who's that? I Miss You"], title: "I Miss You", obj: "flights", fam: "oo" },
        { a: "Your goodnight is my good morning", b: "Three hours ahead, I'm yawning", alt: ["Who's that? My Good Morning", "Your goodnight, my good morning", "Your text is my good morning"], title: "My Good Morning", obj: "goodnight", fam: "orning" },
        { a: "Booked the window to watch for you", b: "Wheels down and the sky's blue", alt: ["Say what? To Watch For You", "Booked the aisle to watch for you", "Who's that? To Watch For You"], title: "To Watch For You", obj: "window", fam: "oo" },
        { a: "The red-eye knows my name", b: "Same seat, same lane", alt: ["What's the word? Knows My Name", "The layover knows my name", "The gate knows my name"], title: "Knows My Name", obj: "red-eye", fam: "ame" },
        { a: "Two clocks on one lock screen", b: "Yours says sleep, mine says seen", alt: ["Who's that? One Lock Screen", "Two clocks, one lock screen", "Say less, one lock screen"], title: "One Lock Screen", obj: "lock screen", fam: "een" },
        { a: "I count the hours wrong on purpose", b: "So it's closer, so it's worth it", alt: ["Say what? Wrong On Purpose", "I count the days wrong on purpose", "Who's that? Wrong On Purpose"], title: "Wrong On Purpose", obj: "hours", fam: "urpose" },
        { a: "Signal cut, I'm still talking", b: "Hotel hallway, still walking", alt: ["Who's that? I'm Still Talking", "Signal gone, I'm still talking", "Say less, I'm still talking"], title: "I'm Still Talking", obj: "signal", fam: "alking" },
        { a: "Hotel key and your hoodie", b: "That's the whole carry-on, truly", alt: ["What's the move? And Your Hoodie", "Boarding pass and your hoodie", "Who's that? And Your Hoodie"], title: "And Your Hoodie", obj: "hoodie", fam: "oodie" },
        { a: "Countdown says four days", b: "I'll be early anyways", alt: ["Say what? Four Days", "Countdown says two days", "Who's that? Four Days"], title: "Four Days", obj: "countdown", fam: "ay" },
        { a: "Wheels touch down, I'm calling", b: "Skip the bag, I'm walking", alt: ["Who's that? I'm Calling", "Doors open up, I'm calling", "Say less, I'm calling"], title: "I'm Calling", obj: "wheels", fam: "alling" },
      ],
      rap: [
        { k: "flex", i: true, a: "Two time zones, one ringtone, same tone", b: "Your goodnight hits when I'm driving home", fam: "own" },
        { k: "detail", a: "Booked the window seat to watch for you", b: "Wheels down at nine, sky still blue", fam: "oo" },
        { k: "flex", a: "I don't miss flights, I miss you, that's it", b: "Gate twenty-two, I'm first to sit", fam: "it" },
        { k: "detail", i: true, a: "Two clocks on the lock screen, yours says sleep", b: "Mine says seen, mine says keep", fam: "eep" },
        { k: "turn", a: "I count the hours wrong on purpose", b: "So the distance feels like it's worth it", fam: "urpose" },
        { k: "callback", a: "The red-eye knows my name and my seat", b: "Same row, same hoodie, on repeat", fam: "eat" },
        { k: "flex", a: "Signal cut and I'm still talking", b: "Hotel hallway, three laps, still walking", fam: "alking" },
        { k: "detail", a: "Countdown says four days, I say two", b: "Changed the flight, that's what I do", fam: "oo" },
        { k: "flex", i: true, a: "Your goodnight is my good morning, yawning", b: "Same moon, two windows, still calling", fam: "alling" },
        { k: "callback", a: "Wheels touch down and I'm calling", b: "Skip the bags, I'm walking", fam: "alling" },
      ],
      rnb: [
        { l: ["Your goodnight comes at my noon", "I say go to sleep, you say soon", "Two clocks on the one lock screen", "Yours says love you, mine says seen"], fam: "oon" },
        { l: ["I booked the window to watch for you", "Wheels down and the sky's still blue", "I don't miss flights, I miss your face", "Gate twenty-two, I know the place"], fam: "oo" },
        { l: ["Signal cut and I kept talking", "Hotel hallway, I kept walking", "Said the whole thing to the dial tone", "Then I said it again alone"], fam: "alking" },
        { l: ["I count the hours wrong on purpose", "Four becomes two, that's the service", "Countdown's a liar and so am I", "Said I'm fine, then I booked the flight"], fam: "urpose" },
        { l: ["Hotel key and your old hoodie", "That's the whole bag, truly", "Your voicemail got me through the layover", "Played it twice, then played it over"], fam: "oodie" },
        { l: ["The red-eye knows my name by now", "Same row, same seat, the same vow", "Wheels touch down and I'm calling", "Skip the bags, I'm already walking"], fam: "ow" },
      ],
      pre: [
        { l: ["Wheels down, I'm here", "Say it in my ear"], fam: "ere" },
        { l: ["Two clocks, one phone", "Pick up, you're not alone"], fam: "own" },
        { l: ["Four days, say two", "I'm already through"], fam: "oo" },
        { l: ["Gate twenty-two, say when", "I'll be there by ten"], fam: "en" },
      ],
      bridge: [
        { l: ["I count the hours wrong on purpose", "Four becomes two, that's the service", "I know you know I'm lying", "Still you say thanks for trying"], fam: "urpose" },
        { l: ["Your goodnight is my good morning", "I'm up when you're falling", "Three hours ahead, three hours behind", "Somewhere in the middle, you're mine"], fam: "orning" },
        { l: ["Signal cut and I kept on talking", "Said the whole thing to nothing", "Then I said it to the hotel wall", "Then I booked the flight after all"], fam: "alking" },
        { l: ["I keep the boarding pass in the mirror", "So the distance looks nearer", "Hotel key and your old hoodie", "Home is wherever you are, truly"], fam: "irror" },
      ],
    },
    city3am: {
      hooks: [
        { a: "3am, I own every green light", b: "City's asleep, I'm up, that's right", alt: ["Who's that? Every Green Light", "3am, I own every streetlight", "Say what? Every Green Light"], title: "Every Green Light", obj: "green light", fam: "ight" },
        { a: "Diner knows my order, not my name", b: "Same booth, same rain", alt: ["What's the word? Not My Name", "The driver knows my block, not my name", "Who's that? Not My Name"], title: "Not My Name", obj: "diner", fam: "ame" },
        { a: "Last train left, I'm walking", b: "Streetlights on, I'm talking", alt: ["Say what? I'm Walking", "Last bus left, I'm walking", "Who's that? I'm Walking"], title: "I'm Walking", obj: "train", fam: "alking" },
        { a: "One taxi light for miles", b: "Blinks for me, that's my style", alt: ["Who's that? For Miles", "One streetlight for miles", "Say less, for miles"], title: "For Miles", obj: "taxi light", fam: "iles" },
        { a: "I run the night shift", b: "Streetlights clock in, that's the gift", alt: ["What's the move? The Night Shift", "I run the late shift", "Who's that? The Night Shift"], title: "The Night Shift", obj: "night shift", fam: "ift" },
        { a: "Steam off the grate, hood up", b: "Corner store, one cup", alt: ["Say what? Hood Up", "Steam off the grate, lights up", "Who's that? Hood Up"], title: "Hood Up", obj: "grate", fam: "up" },
        { a: "Wet pavement, same route", b: "Sirens pass, I walk it out", alt: ["Who's that? Same Route", "Wet pavement, new route", "Say less, same route"], title: "Same Route", obj: "pavement", fam: "out" },
        { a: "The corner store's my living room", b: "3am, that's noon", alt: ["What's the word? My Living Room", "The diner's my living room", "Who's that? My Living Room"], title: "My Living Room", obj: "corner store", fam: "oom" },
        { a: "Night bus, back seat, window", b: "City lights go slow", alt: ["Say what? Back Seat Window", "Night bus, back row, window", "Who's that? Back Seat Window"], title: "Back Seat Window", obj: "night bus", fam: "o" },
        { a: "I walk the city like a landlord", b: "Every block, I'm on board", alt: ["Who's that? Like A Landlord", "I walk the block like a landlord", "Say what? Like A Landlord"], title: "Like A Landlord", obj: "landlord", fam: "ord" },
        { a: "3am's the only honest hour", b: "Streetlight hum, that's my power", alt: ["Say what? The Only Honest Hour", "4am's the only honest hour", "Who's that? The Only Honest Hour"], title: "The Only Honest Hour", obj: "hour", fam: "our" },
        { a: "Rooftop, one bar of signal", b: "City lights, that's the ritual", alt: ["Who's that? One Bar Of Signal", "Rooftop, two bars of signal", "Say less, one bar of signal"], title: "One Bar Of Signal", obj: "rooftop", fam: "ignal" },
      ],
      rap: [
        { k: "flex", i: true, a: "3am, I own every light on the block", b: "City's asleep, I'm the one who talks", fam: "ock" },
        { k: "detail", a: "Steam off the grate, hood up, one cup", b: "Corner store guy says what's up", fam: "up" },
        { k: "flex", a: "The diner knows my order, not my name", b: "Same booth, same window, same rain", fam: "ame" },
        { k: "detail", i: true, a: "Last train left at one, so I walk", b: "Wet pavement, sirens, I don't talk", fam: "alk" },
        { k: "turn", a: "I ride the last train so I'm not home yet", b: "The city's loud enough to forget", fam: "et" },
        { k: "callback", a: "One taxi light for miles, it blinks for me", b: "Night bus, back seat, that's VIP", fam: "ee" },
        { k: "flex", a: "I run the night shift, streetlights clock in", b: "Corner store's open, I walk in", fam: "in" },
        { k: "detail", a: "Rooftop, one bar of signal, phone at three", b: "City lights look like a map of me", fam: "ee" },
        { k: "flex", i: true, a: "Walk the city like a landlord, every floor", b: "Every door's my door", fam: "ore" },
        { k: "callback", a: "3am's the only honest hour", b: "Streetlight hum, that's the power", fam: "our" },
      ],
      rnb: [
        { l: ["3am and the city's ours", "Every green light counts the hours", "Night bus, back seat, your head on me", "Window fog, that's all I see"], fam: "ours" },
        { l: ["The diner knows our booth by now", "Two coffees, one bill, somehow", "You fall asleep against the glass", "And I let the last train pass"], fam: "ow" },
        { l: ["Steam off the grate, your hood up", "Corner store, one coffee cup", "We walk slow because we can", "3am, and that's the whole plan"], fam: "up" },
        { l: ["Rooftop, one bar of signal", "Don't need it, this is the ritual", "City lights and your face lit", "I'd stay up here, I'd never quit"], fam: "ignal" },
        { l: ["The last train left and I'm glad", "Walking home with you's not bad", "Wet pavement, your hand in mine", "Sirens pass us and we're fine"], fam: "ad" },
        { l: ["3am's the only honest hour", "Say it now, say that it's ours", "I walk this city like a landlord", "But you're the only open door"], fam: "our" },
      ],
      pre: [
        { l: ["Lights green, say when", "3am again"], fam: "en" },
        { l: ["Hood up, one cup", "Night shift, what's up"], fam: "up" },
        { l: ["Last train gone, I'm fine", "Walk with me, take your time"], fam: "ine" },
        { l: ["Rooftop, one bar", "Say it, here we are"], fam: "ar" },
      ],
      bridge: [
        { l: ["I ride the last train so I'm not home yet", "The city's loud enough to forget", "Then it's quiet at the end of the line", "And I'm the only one, and that's fine"], fam: "et" },
        { l: ["3am's the only honest hour", "Daylight, I'm somebody's power", "Night time, I'm just a walk", "Streetlight, pavement, small talk"], fam: "our" },
        { l: ["I know this corner more than my place", "Know the diner more than my face", "I walk until the thoughts get tired", "Then I walk until they're wired"], fam: "ace" },
        { l: ["Night bus, back seat, window", "Everybody's somewhere I don't go", "One taxi light for miles", "I wave, it blinks, we're friends for a while"], fam: "o" },
      ],
    },
    selfmade: {
      hooks: [
        { a: "Wrote the plan on a napkin in ink", b: "Signed it before I could think", alt: ["Who's that? On A Napkin In Ink", "Drew the plan on a napkin in ink", "Say what? On A Napkin In Ink"], title: "Napkin In Ink", obj: "napkin", fam: "ink" },
        { a: "I was the intern and the owner", b: "Same desk, same corner", alt: ["What's the word? And The Owner", "I was the driver and the owner", "Who's that? And The Owner"], title: "And The Owner", obj: "intern", fam: "owner" },
        { a: "They said get a job, I made nine", b: "Payroll's long, so's the line", alt: ["Say what? I Made Nine", "They said get a job, I made mine", "Who's that? I Made Nine"], title: "I Made Nine", obj: "job", fam: "ine" },
        { a: "First office had a garage door", b: "First check framed on the floor", alt: ["Who's that? A Garage Door", "First office was a garage door", "Say less, a garage door"], title: "A Garage Door", obj: "garage", fam: "ore" },
        { a: "I signed my own permission slip", b: "Then I took my own trip", alt: ["What's the move? Permission Slip", "I wrote my own permission slip", "Who's that? Permission Slip"], title: "Permission Slip", obj: "slip", fam: "ip" },
        { a: "Job title is my own name", b: "Business card, same", alt: ["Say what? My Own Name", "Job title is my last name", "Who's that? My Own Name"], title: "My Own Name", obj: "job title", fam: "ame" },
        { a: "Spreadsheet lit at midnight", b: "Lights stay on, that's right", alt: ["Who's that? Lit At Midnight", "Laptop lit at midnight", "Say less, lit at midnight"], title: "Lit At Midnight", obj: "spreadsheet", fam: "ight" },
        { a: "One laptop, three jobs", b: "Kitchen table, bad odds", alt: ["What's the word? Three Jobs", "One laptop, four jobs", "Who's that? Three Jobs"], title: "Three Jobs", obj: "laptop", fam: "obs" },
        { a: "Rent came early, I don't celebrate", b: "I restart, that's the trait", alt: ["Say what? I Don't Celebrate", "Check came early, I don't celebrate", "Who's that? I Don't Celebrate"], title: "I Don't Celebrate", obj: "rent", fam: "ate" },
        { a: "Handed nothing but the bill", b: "Paid it anyway, still", alt: ["Who's that? Nothing But The Bill", "Handed nothing but the drill", "Say less, nothing but the bill"], title: "Nothing But The Bill", obj: "bill", fam: "ill" },
        { a: "I made the room, still knock first", b: "Scared of the good part, that's the worst", alt: ["What's the move? Still Knock First", "I own the room, still knock first", "Who's that? Still Knock First"], title: "Still Knock First", obj: "room", fam: "urst" },
        { a: "Never learned to rest, learned to invoice", b: "Every yes was my choice", alt: ["Say what? Learned To Invoice", "Never learned to sleep, learned to invoice", "Who's that? Learned To Invoice"], title: "Learned To Invoice", obj: "invoice", fam: "oice" },
      ],
      rap: [
        { k: "flex", i: true, a: "Wrote the plan on a napkin, signed it in ink", b: "Garage door office, kitchen sink", fam: "ink" },
        { k: "detail", a: "One laptop, three jobs, one chair", b: "Spreadsheet lit at midnight, I'm there", fam: "air" },
        { k: "flex", a: "They said get a job, so I made nine", b: "Payroll's long, so is the line", fam: "ine" },
        { k: "detail", i: true, a: "First check framed, first key on the ring", b: "Rent came early, I didn't say a thing", fam: "ing" },
        { k: "turn", a: "I made the room and I still knock first", b: "Scared the good part's the part that'll hurt", fam: "urst" },
        { k: "callback", a: "I was the intern and the owner, same desk", b: "Business card says my name, nothing less", fam: "ess" },
        { k: "flex", a: "I signed my own permission slip", b: "Took my own trip, that's the whole script", fam: "ip" },
        { k: "detail", a: "Landlord smiled when the rent came in early", b: "Lights stayed on, I'm still here early", fam: "early" },
        { k: "flex", i: true, a: "Never learned to rest, learned to invoice", b: "Every yes I said was my choice", fam: "oice" },
        { k: "callback", a: "Handed nothing but the bill, paid it still", b: "Job title's my name, that's the deal", fam: "ill" },
      ],
      rnb: [
        { l: ["You saw the napkin before the plan", "You said do it, so I ran", "Garage door, one laptop, three jobs", "You made coffee, held the odds"], fam: "an" },
        { l: ["I made the room and I still knock", "You walk in like you own the block", "Say my name like it's on the card", "It is now, because I worked hard"], fam: "ock" },
        { l: ["Spreadsheet lit at midnight, you're asleep", "I count what we have, what we keep", "Rent came early, I didn't say", "You already knew anyway"], fam: "eep" },
        { l: ["Never learned to rest, you taught me", "Shut the laptop, you caught me", "Said the invoice can wait till ten", "Said the lights stay on, and then"], fam: "ee" },
        { l: ["They said get a job, you said get two", "So I made nine and gave one to you", "Job title is my own name", "And the card says yours is the same"], fam: "oo" },
        { l: ["First check framed above the sink", "You said that's cute, I said don't blink", "Second check framed next to it", "Third one paid for all of it"], fam: "ink" },
      ],
      pre: [
        { l: ["Napkin, pen, say when", "Signed it, signed again"], fam: "en" },
        { l: ["Three jobs, one chair", "Lights on, I'm there"], fam: "air" },
        { l: ["Garage door, say it loud", "Mom came through, she's proud"], fam: "oud" },
        { l: ["Still knock first, still here", "Say it in my ear"], fam: "ere" },
      ],
      bridge: [
        { l: ["I made the room and I still knock first", "Scared the good part is the part that hurts", "I say I'm fine, the spreadsheet disagrees", "Lights on at midnight, nobody sees"], fam: "urst" },
        { l: ["I don't celebrate, I restart", "Rent came early, that's the hard part", "Never learned to rest, learned to invoice", "Some days I can't hear my own choice"], fam: "art" },
        { l: ["Handed nothing but the bill", "Paid it, paid it, paid it still", "Now the lobby says my name", "And I still check if it's the same"], fam: "ill" },
        { l: ["One laptop, three jobs, one chair", "Nobody saw, nobody was there", "Now they say it looked easy", "Napkin plan, kitchen table, believe me"], fam: "air" },
      ],
    },
    mainchar: {
      hooks: [
        { a: "I don't enter, I arrive", b: "Slow walk, that's the drive", alt: ["Who's that? I Arrive", "I don't walk in, I arrive", "Say what? I Arrive"], title: "I Arrive", obj: "arrive", fam: "ive" },
        { a: "Walk slow so the story catches up", b: "Sunglasses on, cup", alt: ["What's the move? The Story Catches Up", "Walk slow so the camera catches up", "Who's that? The Story Catches Up"], title: "The Story Catches Up", obj: "story", fam: "up" },
        { a: "The wind's on payroll when I leave", b: "Long coat, long sleeve", alt: ["Say what? When I Leave", "The lights on payroll when I leave", "Who's that? When I Leave"], title: "When I Leave", obj: "wind", fam: "eave" },
        { a: "Every room's a scene, I'm the lead", b: "Theme song on, that's all I need", alt: ["Who's that? I'm The Lead", "Every aisle's a scene, I'm the lead", "Say less, I'm the lead"], title: "I'm The Lead", obj: "scene", fam: "eed" },
        { a: "Sunglasses at the register", b: "Slow walk, that's the signature", alt: ["What's the word? At The Register", "Long coat at the register", "Who's that? At The Register"], title: "At The Register", obj: "register", fam: "ister" },
        { a: "My playlist knows before I do", b: "Doors open on cue", alt: ["Say what? Before I Do", "My playlist knows what I'll do", "Who's that? Before I Do"], title: "Before I Do", obj: "playlist", fam: "oo" },
        { a: "Slow motion is my only speed", b: "Crosswalk, long coat, take the lead", alt: ["Who's that? My Only Speed", "Slow motion is my top speed", "Say less, my only speed"], title: "My Only Speed", obj: "slow motion", fam: "eed" },
        { a: "The camera finds me first", b: "Front row, worst to first", alt: ["What's the move? Finds Me First", "The light finds me first", "Who's that? Finds Me First"], title: "Finds Me First", obj: "camera", fam: "urst" },
        { a: "Window seat, main character", b: "Airport, I'm the narrator", alt: ["Say what? Main Character", "Front row, main character", "Who's that? Main Character"], title: "Main Character", obj: "window seat", fam: "ator" },
        { a: "Long coat enters first", b: "Then me, then the verse", alt: ["Who's that? Enters First", "Sunglasses enter first", "Say less, enters first"], title: "Enters First", obj: "coat", fam: "urst" },
        { a: "I pose so the room won't ask", b: "Mirror check, that's the task", alt: ["What's the word? So The Room Won't Ask", "I smile so the room won't ask", "Who's that? So The Room Won't Ask"], title: "Room Won't Ask", obj: "pose", fam: "ask" },
        { a: "Theme song's loud so the quiet's not", b: "Grocery aisle, that's my spot", alt: ["Say what? So The Quiet's Not", "Playlist's loud so the quiet's not", "Who's that? So The Quiet's Not"], title: "So The Quiet's Not", obj: "theme song", fam: "ot" },
      ],
      rap: [
        { k: "flex", i: true, a: "I don't enter, I arrive, slow walk, long coat", b: "Every room's a scene, I'm the quote", fam: "ote" },
        { k: "detail", a: "Sunglasses at the register, four items", b: "Theme song on, I'm not hiding", fam: "iding" },
        { k: "flex", a: "I walk slow so the story catches up", b: "Crosswalk, every car waits, what's up", fam: "up" },
        { k: "detail", i: true, a: "Window seat, airport, playlist on", b: "Doors open on cue, I'm gone", fam: "on" },
        { k: "turn", a: "I play the lead so I don't feel small", b: "Camera's off and I'm still posing in the hall", fam: "all" },
        { k: "callback", a: "The wind's on payroll when I leave the car", b: "Long coat, slow walk, there you are", fam: "ar" },
        { k: "flex", a: "My playlist knows the moment before I do", b: "Lights hit on cue, right on cue", fam: "oo" },
        { k: "detail", a: "Mirror check in the grocery aisle", b: "Main character, that's my style", fam: "ile" },
        { k: "flex", i: true, a: "Slow motion is my only speed, read it", b: "Every room's a scene and I lead it", fam: "eed it" },
        { k: "callback", a: "The camera finds me first, that's the plot", b: "Theme song's loud so the quiet's not", fam: "ot" },
      ],
      rnb: [
        { l: ["You walk in and the room turns slow", "Long coat, sunglasses, that's the show", "I'm the lead but you're the scene", "Every light finds you, you know what I mean"], fam: "o" },
        { l: ["My playlist knows before I do", "Every song's about you", "Crosswalk, wind machine, slow walk", "You look back and I forget to talk"], fam: "oo" },
        { l: ["I pose so the room won't ask", "You see through it, you see past", "Camera's off and I'm still here", "Theme song off and you're still near"], fam: "ask" },
        { l: ["Window seat, you take the aisle", "Airport lights, main character style", "Doors open and you go first", "I don't mind, I've already rehearsed"], fam: "ile" },
        { l: ["Sunglasses on at the register", "Four items, one of them's for her", "I walk slow so you catch up", "Grocery aisle, that's enough"], fam: "er" },
        { l: ["Every room's a scene, you're the lead", "I'm the one who wrote it, I don't need", "The camera, the light, the slow motion", "Just you in the kitchen, the whole ocean"], fam: "eed" },
      ],
      pre: [
        { l: ["Slow walk, say when", "Lights on, again"], fam: "en" },
        { l: ["Playlist, doors, cue", "Here I come, here's you"], fam: "oo" },
        { l: ["Long coat, head high", "Camera on, say hi"], fam: "igh" },
        { l: ["Main character, say it", "Front row, I made it"], fam: "ay it" },
      ],
      bridge: [
        { l: ["I play the lead so I don't feel small", "Camera's off, I'm still posing in the hall", "The story's mine, the ending isn't", "I'm the main one and I'm still listening"], fam: "all" },
        { l: ["Theme song's loud so the quiet's not", "Grocery aisle, that's my spot", "Sunglasses so the room won't ask", "Four items and a face like a mask"], fam: "ot" },
        { l: ["I walk slow so nobody sees me rush", "Long coat, slow walk, hush", "I'm the main one, still check my teeth", "Still check if the lead's underneath"], fam: "ush" },
        { l: ["The wind's on payroll when I leave", "Nobody's paying when I grieve", "Every room's a scene, I'm the lead", "Camera off, I'm just me, indeed"], fam: "eave" },
      ],
    },
    lastcall: {
      hooks: [
        { a: "Last call, I'm first out the door", b: "Tab's on me, the night's on you, for sure", alt: ["Who's that? First Out The Door", "Last call, I'm last out the door", "Say what? First Out The Door"], title: "First Out The Door", obj: "last call", fam: "ore" },
        { a: "Lights up, still looking good", b: "Back booth, understood", alt: ["What's the move? Still Looking Good", "Lights up, you still looking good", "Who's that? Still Looking Good"], title: "Still Looking Good", obj: "lights", fam: "ood" },
        { a: "I close the place like I own it", b: "Coat check, name on it", alt: ["Say what? Like I Own It", "I close the bar like I own it", "Who's that? Like I Own It"], title: "Like I Own It", obj: "place", fam: "on it" },
        { a: "The tab's on me, the night's on you", b: "Two glasses, one left, that's true", alt: ["Who's that? The Night's On You", "The tab's on me, the cab's on you", "Say less, the night's on you"], title: "The Night's On You", obj: "tab", fam: "oo" },
        { a: "One more and I'll say it", b: "Last call, I'll weigh it", alt: ["What's the word? I'll Say It", "One more song and I'll say it", "Who's that? I'll Say It"], title: "I'll Say It", obj: "one more", fam: "ay it" },
        { a: "Your number on a napkin, folded", b: "Cab outside, I'm still holding", alt: ["Say what? On A Napkin Folded", "Your name on a napkin, folded", "Who's that? On A Napkin Folded"], title: "On A Napkin Folded", obj: "napkin", fam: "olded" },
        { a: "They called last call, I called you", b: "Lights up, we're the last two", alt: ["Who's that? I Called You", "They called last call, I texted you", "Say less, I called you"], title: "I Called You", obj: "last call", fam: "oo" },
        { a: "Last song's mine, I already asked", b: "Bartender nodded, that's the task", alt: ["What's the move? I Already Asked", "Last dance is mine, I already asked", "Who's that? I Already Asked"], title: "I Already Asked", obj: "last song", fam: "ask" },
        { a: "Tip big so the staff remembers", b: "Back booth, same old members", alt: ["Say what? Staff Remembers", "Stay late so the staff remembers", "Who's that? Staff Remembers"], title: "Staff Remembers", obj: "tip", fam: "embers" },
        { a: "Lights come up, so does the truth", b: "Coat check, back booth", alt: ["Who's that? So Does The Truth", "Lights come up and so does the truth", "Say less, so does the truth"], title: "So Does The Truth", obj: "lights", fam: "ooth" },
        { a: "Cab's outside with the door open", b: "Say it now, I'm hoping", alt: ["What's the word? With The Door Open", "Car's outside with the door open", "Who's that? With The Door Open"], title: "With The Door Open", obj: "cab", fam: "open" },
        { a: "Last call's the only time I'm honest", b: "Two in the booth, that's a promise", alt: ["Say what? The Only Time I'm Honest", "Lights up's the only time I'm honest", "Who's that? The Only Time I'm Honest"], title: "Time I'm Honest", obj: "last call", fam: "onest" },
      ],
      rap: [
        { k: "flex", i: true, a: "Last call, I'm first out the door, tab's paid", b: "Night's on you, that's the trade", fam: "ade" },
        { k: "detail", a: "Lights up too bright, two glasses, one left", b: "Coat check girl says you're the last one left", fam: "eft" },
        { k: "flex", a: "I close the place like I own it, I don't", b: "Bartender says go home, I won't", fam: "ont" },
        { k: "detail", i: true, a: "Last song's mine, I already asked at ten", b: "Played it twice, then asked again", fam: "en" },
        { k: "turn", a: "I stay till last call so I don't go home", b: "Lights up, the truth's up, I'm alone", fam: "own" },
        { k: "callback", a: "The tab's on me, the night's on you", b: "Cab's outside, door open, two", fam: "oo" },
        { k: "flex", a: "I tip too much so somebody remembers", b: "Back booth, same crew, same members", fam: "embers" },
        { k: "detail", a: "Lights come up, so does the truth", b: "Napkin, number, back booth", fam: "ooth" },
        { k: "flex", i: true, a: "They called last call, I called you, one ring", b: "Then the bartender cut the whole thing", fam: "ing" },
        { k: "callback", a: "One more and I'll say it, I'm honest", b: "Last call's a promise", fam: "onest" },
      ],
      rnb: [
        { l: ["Last call and the lights come up", "You look better in the bad light, what's up", "Two glasses, one left, one for me", "Say it now, it's half past three"], fam: "up" },
        { l: ["The tab's on me, the night's on you", "Cab's outside, I'll count to two", "Say the thing you said you'd say", "Or I'll say it anyway"], fam: "oo" },
        { l: ["I stay till last call so I don't go home", "Then you walk me out and I'm not alone", "Your number on a napkin, folded twice", "I read it under the streetlight twice"], fam: "own" },
        { l: ["Lights up and so does the truth", "You in the coat check, me in the booth", "One more and I'll say it slow", "Bartender's counting, I know"], fam: "ooth" },
        { l: ["I tip too much so they remember us", "Back booth, same song, the same fuss", "Last song's mine, I already asked", "You pulled me up before it passed"], fam: "us" },
        { l: ["Last call's the only time I'm honest", "Lights too bright, that's the promise", "I'll say it once, then I'll leave", "Or you'll say stay, and I'll believe"], fam: "onest" },
      ],
      pre: [
        { l: ["Lights up, say when", "One more, then"], fam: "en" },
        { l: ["Tab's on me, cab's on you", "Say it, count to two"], fam: "oo" },
        { l: ["Last call, last song", "Say it before it's gone"], fam: "ong" },
        { l: ["Napkin, number, fold", "Say it before it's old"], fam: "old" },
      ],
      bridge: [
        { l: ["I stay till last call so I don't go home", "Lights up, the truth's up, I'm alone", "One more and I'll say what I came to say", "Then I tip too much and walk away"], fam: "own" },
        { l: ["Last call's the only time I'm honest", "The rest of the day I'm a promise", "Lights up and I look like me", "Lights down and I'm whoever you see"], fam: "onest" },
        { l: ["Your number on a napkin, folded twice", "I read it under the streetlight, nice", "Cab's outside with the door open", "I'm not getting in, I'm hoping"], fam: "ice" },
        { l: ["I count the glasses, not the hours", "Two in the booth, the night's ours", "Lights come up and so does the truth", "I say it to the coat check, not you"], fam: "ours" },
      ],
    },
    betteroff: {
      hooks: [
        { a: "Your plant's alive, so am I", b: "Watered it once, didn't cry", alt: ["Who's that? So Am I", "Your cat's alive, so am I", "Say what? So Am I"], title: "So Am I", obj: "plant", fam: "igh" },
        { a: "I sleep in the middle now", b: "Both pillows, somehow", alt: ["What's the move? In The Middle Now", "I sleep on your side now", "Who's that? In The Middle Now"], title: "In The Middle Now", obj: "middle", fam: "ow" },
        { a: "Same rent, the air is cheaper", b: "Changed the locks, I'm the keeper", alt: ["Say what? The Air Is Cheaper", "Same bed, the air is cheaper", "Who's that? The Air Is Cheaper"], title: "The Air Is Cheaper", obj: "rent", fam: "eeper" },
        { a: "Your side's a bookshelf now", b: "Three books and a plant, somehow", alt: ["Who's that? A Bookshelf Now", "Your side's a window now", "Say less, a bookshelf now"], title: "A Bookshelf Now", obj: "bookshelf", fam: "ow" },
        { a: "Your friend asked, I said look", b: "Then I went back to my book", alt: ["What's the word? I Said Look", "Your mom asked, I said look", "Who's that? I Said Look"], title: "I Said Look", obj: "friend", fam: "ook" },
        { a: "I kept the mug, not the number", b: "Coffee's hot, I don't wonder", alt: ["Say what? Not The Number", "I kept the plant, not the number", "Who's that? Not The Number"], title: "Not The Number", obj: "mug", fam: "umber" },
        { a: "Changed the locks and the playlist", b: "Six songs gone, that's the whole gist", alt: ["Who's that? And The Playlist", "Changed the sheets and the playlist", "Say less, and the playlist"], title: "And The Playlist", obj: "playlist", fam: "ist" },
        { a: "I don't miss you, I miss the routine", b: "Sunday coffee, same scene", alt: ["What's the move? I Miss The Routine", "I don't miss it, I miss the routine", "Who's that? I Miss The Routine"], title: "I Miss The Routine", obj: "routine", fam: "een" },
        { a: "I'm good now, that took a minute", b: "Lease is mine, my name's in it", alt: ["Say what? That Took A Minute", "I'm fine now, that took a minute", "Who's that? That Took A Minute"], title: "That Took A Minute", obj: "minute", fam: "innit" },
        { a: "The quiet was loud for a month", b: "Now it's just Sunday brunch", alt: ["Who's that? Loud For A Month", "The silence was loud for a month", "Say less, loud for a month"], title: "Loud For A Month", obj: "quiet", fam: "unch" },
        { a: "Bed made all the way", b: "Both sides, every day", alt: ["What's the word? All The Way", "Bed made, my way", "Who's that? All The Way"], title: "All The Way", obj: "bed", fam: "ay" },
        { a: "Still check the time you'd call", b: "Then I don't, that's all", alt: ["Say what? The Time You'd Call", "Still know the time you'd call", "Who's that? The Time You'd Call"], title: "The Time You'd Call", obj: "time", fam: "all" },
      ],
      rap: [
        { k: "flex", i: true, a: "Your plant's alive, so am I, we're fine", b: "Sleep in the middle now, both sides mine", fam: "ine" },
        { k: "detail", a: "Same rent, the air is cheaper, windows wide", b: "Your side's a bookshelf now, three books, pride", fam: "ide" },
        { k: "flex", a: "Your friend asked how I'm doing, I said look", b: "Then I went back to my book", fam: "ook" },
        { k: "detail", i: true, a: "Kept the mug, not the number, coffee's hot", b: "Sunday, same routine, same spot", fam: "ot" },
        { k: "turn", a: "I don't miss you, I miss the routine", b: "The quiet was loud, now it's clean", fam: "een" },
        { k: "callback", a: "Good now, but that took a minute", b: "Lease is mine, nobody else in it", fam: "innit" },
        { k: "flex", a: "Changed the locks and the playlist, six songs", b: "Deleted the ones that sang along", fam: "ong" },
        { k: "detail", a: "Bed made all the way, both sides", b: "Pillow on your side, nobody hides", fam: "ide" },
        { k: "flex", i: true, a: "Still check the time you'd call, then I don't", b: "Said I'd text back, I won't", fam: "ont" },
        { k: "callback", a: "Your plant's alive and so am I", b: "Watered it once, didn't cry", fam: "igh" },
      ],
      rnb: [
        { l: ["Your plant's alive, so am I", "Watered it Sunday, didn't cry", "Your side's a bookshelf now", "I read in the middle, somehow"], fam: "igh" },
        { l: ["I kept the mug, not the number", "Coffee at eight, that's the wonder", "Same rent and the air is cheaper", "Changed the locks, now I'm the keeper"], fam: "umber" },
        { l: ["Your friend asked me how I'm doing", "I said look, then kept moving", "I'm good now, that took a minute", "The lease is mine, my name's in it"], fam: "ooing" },
        { l: ["I don't miss you, I miss the routine", "Sunday coffee, the same scene", "I made it mine, slowly, slowly", "Nobody knows, only me, only"], fam: "een" },
        { l: ["The quiet was loud for a month", "Then it was just quiet, just lunch", "Bed made all the way, both sides", "Nothing to say, nobody hides"], fam: "unch" },
        { l: ["I still check the time you'd call", "And then I don't, and that's all", "Changed the playlist, kept one song", "The one that was never about you"], fam: "all" },
      ],
      pre: [
        { l: ["Plant's alive, say it", "So am I, play it"], fam: "ay it" },
        { l: ["Locks changed, I'm fine", "Both sides mine"], fam: "ine" },
        { l: ["Same rent, more air", "Your side, nobody there"], fam: "air" },
        { l: ["Your friend asked, look", "I'm good, closed the book"], fam: "ook" },
      ],
      bridge: [
        { l: ["I don't miss you, I miss the routine", "Sunday coffee, the same scene", "I kept the mug, not the number", "Some days I still wonder"], fam: "een" },
        { l: ["The quiet was loud for a month", "Then it was quiet, then it was lunch", "I'm good now, that took a minute", "Some nights I'm still in it"], fam: "unch" },
        { l: ["Still check the time you'd call", "Then I don't, and that's all", "I water your plant, that's all I'll admit", "It's alive, so am I, that's it"], fam: "all" },
        { l: ["Bed made all the way", "Both sides mine, every day", "Your side's a bookshelf now", "I read it to the end somehow"], fam: "ay" },
      ],
    },
  };

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
    const used = ctx.frag || (ctx.frag = {});
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
    return { lines, title, family: fam.id, obj: ctx.hookObj, E: one.E };
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
    const fam = RHYMES.find((f) => f.id === famId) || choose(RHYMES, r);
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

  // ------------------------------------------------------- curated writers
  // The authored bank first; the template engine only for the hook-object
  // callback couplet and for the 15% of hooks that roll the template path.
  const bankOf = (ctx) => BANK[ctx.theme.id];
  const take = (ctx, key, list, r, prefer) => {
    const used = ctx.used[key] || (ctx.used[key] = new Set());
    let pool = list.filter((x) => !used.has(x));
    if (prefer) { const p = pool.filter(prefer); if (p.length) pool = p; }
    if (!pool.length) return null;
    const x = choose(pool, r);
    used.add(x);
    return x;
  };

  // Curated hook: title line / rhyme line / a third line (a question the
  // title answers, or a one-word swap of the title line) / title line.
  function curatedHook(ctx, r) {
    const h = take(ctx, "hooks", bankOf(ctx).hooks, r);
    ctx.hookObj = h.obj;
    ctx.title = h.title;
    return { lines: [h.a, h.b, choose(h.alt, r), h.a], title: h.title, family: h.fam, obj: h.obj, curated: true };
  }

  // The hook-object callback couplet: authored lines with one slot, the
  // anchor word from the title line, so every verse points back at the hook.
  const CALLBACK_C = [
    { a: "Back to the {hobj}, back to the start", b: "Said it once, now it's the best part" },
    { a: "Still on that {hobj}, still the same", b: "Say it twice, you know the name" },
    { a: "{hobj} on my mind, say it slow", b: "Said it in the hook, now you know" },
    { a: "Still on that {hobj} thing, you heard", b: "Hook said it first, that's my word" },
    { a: "Back to the {hobj}, that's the one", b: "Said it in the hook, I'm not done" },
    { a: "{hobj} again, you know the line", b: "Hook said it first, every time" },
    { a: "Still on that {hobj}, that's the move", b: "Hook said it first, nothing to prove" },
    { a: "Back to the {hobj}, run it back", b: "Hook said it first, that's a fact" },
  ];
  function callbackCouplet(ctx, r) {
    const c = take(ctx, "callbacks", CALLBACK_C, r) || choose(CALLBACK_C, r);
    const hobj = ctx.hookObj || choose(ctx.theme.obj, r);
    return { k: "callback", a: cap(c.a.replace("{hobj}", hobj)), b: c.b, template: true };
  }

  // A rap verse from authored couplets: open on a flex or detail, the
  // quotable one-liner (an unused hook couplet) in bars 3-4, the turn around
  // bars 9-12, close on a callback. Couplets never repeat in a song.
  function curatedRapVerse(ctx, r, bars, { callback = false, oneliner = false, lane = "rap" } = {}) {
    const bank = bankOf(ctx);
    const n = Math.max(2, Math.round(bars / 2));
    const want = n - (callback ? 1 : 0) - (oneliner ? 1 : 0);
    const used = ctx.used.rap || (ctx.used.rap = new Set());
    const fresh = () => bank.rap.filter((c) => !used.has(c));
    const chosen = [];
    const grab = (prefer) => { const c = take(ctx, "rap", bank.rap, r, prefer); if (c) chosen.push(c); return c; };
    // One internal-rhyme couplet per verse, never more than two, so the
    // other verse still finds one.
    grab((c) => c.i && c.k !== "turn");
    if (want >= 3 && fresh().some((c) => c.k === "turn")) grab((c) => c.k === "turn");
    while (chosen.length < want && fresh().length) {
      const internals = chosen.filter((c) => c.i).length;
      grab((c) => c.k !== "turn" && (internals < 2 || !c.i));
    }
    // Fill from templates only if the bank ran dry.
    while (chosen.length < want) chosen.push(callbackCouplet(ctx, r));
    // Order: opener, one-liner, body, turn near 60%, callback last.
    const turn = chosen.find((c) => c.k === "turn");
    const close = callback ? null : chosen.find((c) => c.k === "callback");
    const body = chosen.filter((c) => c !== turn && c !== close);
    const openIdx = body.findIndex((c) => c.k === "flex" || c.k === "detail");
    if (openIdx > 0) body.unshift(body.splice(openIdx, 1)[0]);
    const seq = [body[0]];
    if (oneliner) {
      // Not a hook whose opening words a chosen couplet already uses.
      const head = (x) => x.toLowerCase().replace(/[^a-z0-9' ]/g, "").split(/\s+/).slice(0, 3).join(" ");
      const heads = new Set(chosen.map((c) => head(c.a)));
      const h = take(ctx, "hooks", bank.hooks, r, (x) => !heads.has(head(x.a)));
      seq.push(h ? { k: "oneliner", a: h.a, b: h.b } : callbackCouplet(ctx, r));
    }
    seq.push(...body.slice(1));
    if (turn) seq.splice(Math.min(seq.length, Math.max(1, Math.round(n * 0.6) - 1)), 0, turn);
    if (close) seq.push(close);
    if (callback) seq.push(callbackCouplet(ctx, r));
    const lines = [];
    for (const c of seq) lines.push(c.a, c.b);
    return { lines, couplets: seq, internal: seq.some((c) => c.i) };
  }

  // An R&B verse: authored stanzas, plus the hook callback couplet in verse 2.
  function curatedRnbVerse(ctx, r, stanzas, { callback = false } = {}) {
    const bank = bankOf(ctx);
    const lines = [];
    for (let i = 0; i < stanzas; i++) {
      const s = take(ctx, "rnb", bank.rnb, r);
      if (s) lines.push(...s.l);
    }
    if (callback) { const c = callbackCouplet(ctx, r); lines.push(c.a, c.b); }
    return { lines };
  }

  const curatedPre = (ctx, r, famId) => {
    const p = take(ctx, "pre", bankOf(ctx).pre, r, (x) => x.fam === famId);
    return p ? p.l : null;
  };
  const curatedBridge = (ctx, r) => { const b = take(ctx, "bridge", bankOf(ctx).bridge, r); return b ? b.l : null; };

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
    // Every theme has an authored bank, so both lanes draw from all twelve.
    const themeObj = T || _pick(THEMES, r);
    const personaObj = (typeof persona === "string" ? PERSONAS[lane].find((p) => p.id === persona) : persona) || _pick(PERSONAS[lane], r);
    const ctx = { lane, theme: themeObj, persona: personaObj, used: {} };
    const bank = BANK[themeObj.id];

    // Curated hooks nine rolls in ten; the template engine for the rest.
    const hook = bank ? curatedHook(ctx, r) : writeHook(ctx, r); // authored hooks only; the template writer is the fallback
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
    // Rap: authored couplets with the one-liner in verse 1 and the hook
    // callback closing verse 2. R&B: two authored stanzas, then one stanza
    // plus the callback couplet (the feature verse keeps the rap builder).
    let v1, v2;
    if (!bank) {
      v1 = writeVerse(ctx, r, v1Bars, { oneliner: true });
      v2 = feature ? writeVerse(ctx, r, 8, { callback: true, opener: v1.opener, lane: "rap" }) : writeVerse(ctx, r, v2Bars, { callback: true, opener: v1.opener });
    } else if (lane === "rap") {
      v1 = curatedRapVerse(ctx, r, v1Bars, { oneliner: true, callback: true });
      v2 = curatedRapVerse(ctx, r, v2Bars, { oneliner: true, callback: true });
    } else {
      v1 = curatedRnbVerse(ctx, r, structure === "tease" ? 1 : 2);
      v2 = feature ? curatedRapVerse(ctx, r, 8, { callback: true }) : curatedRnbVerse(ctx, r, structure === "tease" ? 2 : 1, { callback: true });
    }
    const pre = (bank && curatedPre(ctx, r, hook.family)) || writePre(ctx, r, hook.family);
    const bridge = (bank && curatedBridge(ctx, r)) || writeBridge(ctx, r);

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
    const intro = () => {
      if (r() < 0.5) return section("Intro", ["spoken", "dry", `${2 + 2 * Math.floor(r() * 2)} bars`], introSpoken());
      const cue = choose(["title line whispered", "hook melody hummed", "ad-libs only"], r);
      const adlib = r() < 0.4;
      return section("Intro", [adlib ? "ad-libs only" : cue === "ad-libs only" ? "title line whispered" : cue, "808 only", "2 bars"], [adlib ? adlibLine(personaObj, r) : hook.lines[0]]);
    };

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
      hook: { lines: hook.lines, title: hook.title, family: hook.family, obj: hook.obj, curated: !!hook.curated },
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

  return { write, rhymes, lastWord, syllables, BANK, RHYMES, PERSONAS, THEMES, HOOK_T, VERSE_T, DELIVERY, TEXTURE, BEAT, EXCLUDE, ALLOW };
})();

if (typeof module !== "undefined") module.exports = OneLyrics;
