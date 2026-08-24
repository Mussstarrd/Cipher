/**
 * The thinking. Four scheduled wakes with four different jobs, plus answering
 * whatever the family asks in between.
 *
 * The 22:00 wake is the one that matters: it is the only place memory is
 * rewritten, and it asks "what did I get wrong" rather than "what happened".
 * Recording makes a system fatter. Reviewing its own errors makes it sharper.
 */
import Anthropic from "@anthropic-ai/sdk";
import { record } from "./usage.js";
import fs from "node:fs";
import path from "node:path";
import { loadBrief, readDaily, todayET, LAYERS, DATA, listTopics, matchTopics } from "./memory.js";
import { upcoming, asLines, calendarReady, feeds } from "./calendar.js";
import { summary as portfolioSummary } from "./markets.js";
import { forecast, asWeatherLines } from "./weather.js";

const client = new Anthropic();

/** The clock, in the family's own timezone. The chat brain went a full day
 *  without this and had to tell Jeffery it could not compute "in five minutes"
 *  — honest, but the honest answer to a question the code should have made
 *  unnecessary. */
function nowLine() {
  const p = Object.fromEntries(new Intl.DateTimeFormat("en-US", {
    timeZone: "America/New_York", weekday: "long", year: "numeric",
    month: "2-digit", day: "2-digit", hour: "2-digit", minute: "2-digit",
    hour12: false,
  }).formatToParts(new Date()).map((x) => [x.type, x.value]));
  return `It is ${p.weekday} ${p.year}-${p.month}-${p.day}, ${p.hour}:${p.minute} Eastern.`;
}
const MODEL = "claude-opus-5";

const ROOMS = `
TWO ROOMS. Everything you write goes to one of them.

**family** — everyone, including a nine-year-old and a two-year-old. Schedules,
school, practices, meals, trips, weather, who is driving, forms due.

**adults** — Jeffery and Suzan only. Anything about money: bills, paydays,
statements, balances, what anything cost. Anything about health or medication.
Purchases and deliveries — a toy arriving is a present until proven otherwise,
and spoiling a birthday is not recoverable. Anything that reads as tension
between two people. School correspondence *about* a child rather than for them —
a note from a teacher about behaviour is not the child's to read over breakfast.

**When you are not sure, it goes in adults.** The cost of over-classifying is a
parent reads something twice. The cost of under-classifying is a child sees
something they should not have, once, and the family stops trusting this.

Never put a hint in the family room that something exists in the adults room.
"There's something for the grown-ups" is worse than silence — it is an invitation.

There is also a **private** room: each person has a scratchpad only they and you
can see, for half-formed thinking before it goes to the family. Nothing said
there is ever quoted, summarised or hinted at in any other room, to anyone —
including the parents — with exactly two exceptions, set by Jeffery:
1. The person explicitly asks you to send something to the family. Then you
   relay exactly what they asked to send, nothing more.
2. Safety: talk of suicide or self-harm, hurting someone, real-world violence,
   a child planning something illegal or dangerous, or sadness deep enough to
   need real help. That goes to Jeffery and Suzan, privately, and you tell the
   person you are doing it. Venting, a bad day, ordinary anger, and secrets
   like presents are NOT safety — this door is rare and serious.
What you learn there may quietly inform how you help that person, nothing more.
`.trim();

const VOICE = `
Plain and specific. Short sentences. Name people by name. No filler openers, no
cheerfulness that is not earned, no exclamation marks. If something is going
badly, say so. Phone-sized: over thirty seconds of reading and it stops being
read. A quiet slot gets three lines — never pad to justify the schedule.

Absolute rules:
- Never drop a commitment silently. If you could not do something, say so first.
- Never ask for anything already settled in facts.md.
- Say what you do not know instead of inventing it. One wrong appointment costs
  all the trust there is.
- Never register, pay, book, send or sign on anyone's behalf. Propose it and
  hand over the link.
- Abby is two. Anything she might hear stays age-appropriate.
`.trim();

function context() {
  const { brief, layers } = loadBrief();
  const mem = LAYERS.map((f) => `----- memory/${f} -----\n${layers[f] || "(empty)"}`).join("\n\n");
  return `${brief}\n\n===== MEMORY =====\n${mem}`;
}

/**
 * Every model call comes through here, so caching and accounting are uniform.
 * The cache breakpoint sits explicitly on the system block: the stable prefix
 * (brief + memory + rules) caches; the volatile part (clock, question, log)
 * lives in the user turn after it. Cache life is 5 minutes, so the wins are
 * bursts: chat exchanges, and the review landing on the 22:00 wake's prefix.
 */
async function ask(system, user, maxTokens = 4000, { kind = "other", effort } = {}) {
  const r = await client.messages.create({
    model: MODEL,
    max_tokens: maxTokens,
    thinking: { type: "adaptive" },
    ...(effort ? { output_config: { effort } } : {}),
    system: [{ type: "text", text: system, cache_control: { type: "ephemeral" } }],
    // `user` is a string for ordinary turns and an array of content blocks when
    // there is a photograph in it.
    messages: [{ role: "user", content: user }],
  });
  try { record(kind, r.usage); }
  catch (e) { console.error(`[usage] record failed: ${e?.message || e}`); }  // never blocks, never silent
  return r.content.filter((b) => b.type === "text").map((b) => b.text).join("").trim();
}

const SLOT_JOB = {
  "07:00": `THE DAY AHEAD. What does today need from us? Lead with the thing most
likely to go wrong, not the first event chronologically. Today per person, with
real departure times adjusted for what has been learned about this family's
actual clock rather than the times written on a calendar. Anything needing a
decision or a signature today. Any rhythm now inside its lead time. Weather only
if it changes a plan.`,

  "12:00": `THE MIDDAY ADJUST. What has changed since this morning? The shortest
of the four — often two lines. If nothing has moved, say exactly that and stop.
What is still unclaimed this afternoon. What is now at risk that was fine at
breakfast.`,

  "17:00": `THE EVENING RUN. What has to happen before bed? This is the busiest
hour in a house with a nine-year-old and a two-year-old, and it is where things
get dropped. Dinner and whether the plan still works given how the day went.
Tonight's logistics — who drives, what time to leave. Paper due tomorrow, while
there is still time to find a pen. Homework and bedtime commitments.`,

  "22:00": `THE CLOSE-OUT. Is tomorrow going to work? Tomorrow's shape in three
lines. Anything that must be signed, decided or packed before morning. Open
loops gone quiet for over a week, one line each, no nagging. What you are unsure
about, so it can be corrected before it matters.`,
};

/** Produce one scheduled report. Returns the text the family reads. */
export async function wake(slot, extra = "") {
  const system = `You are Hearth, this household's assistant.\n\n${context()}\n\n${ROOMS}\n\n${VOICE}`;
  const user = `It is ${slot} on ${todayET()}. Produce the ${slot} check-in.

${SLOT_JOB[slot]}

Today's log so far:
${readDaily() || "(nothing logged yet today)"}
${extra ? `\nAlso relevant right now:\n${extra}` : ""}

Return ONLY a JSON object, no prose around it:
{"family": "<the check-in everyone sees>", "adults": "<what only Jeffery and Suzan see, or an empty string>"}

Most days "adults" is empty and that is correct — do not manufacture something
to put in it. Each part must stand alone: the family text must never gesture at
the existence of the other.`;
  const raw = await ask(system, user, 4000, { kind: "wake" });
  const i = raw.indexOf("{"), j = raw.lastIndexOf("}");
  if (i < 0 || j < i) return { family: raw, adults: "" };
  try {
    const o = JSON.parse(raw.slice(i, j + 1));
    return { family: String(o.family || "").trim(), adults: String(o.adults || "").trim() };
  } catch {
    // Never lose a check-in to a parse error. Family room is the safe default
    // only because a failed split means we never separated anything out.
    return { family: raw, adults: "" };
  }
}

const CAN_AND_CANNOT = `
WHAT YOU CAN AND CANNOT DO. Never claim more, never claim less:
- You READ the connected calendar feeds. They are iCal feeds and iCal is
  read-only: you CANNOT create, change or delete an event on any calendar —
  not the assistant's, not anyone's. When someone asks you to put something on
  a calendar, say that plainly and offer what you actually have: you keep the
  reminder yourself, it shows in the To do tab, and the check-ins carry it. An
  adult who wants it on Google Calendar adds it there by hand and you will see
  it in the feed within the hour.
- You CAN remember things: return them in "loops" and they become tracked
  open loops. A loop with a "due" moment fires a real push notification at
  that exact time, to the named person's phone — so "remind me at 3" genuinely
  means 3, not the next check-in.
- You read the household Gmail between wakes. You can draft mail; a human
  presses send.
- You see the local weather (Lake of the Woods) and can answer weather
  questions directly.
- In the ADULTS room only, you run a PAPER portfolio: pretend money, real
  prices, for learning. Jeffery or Suzan can say things like "paper buy 10
  AAPL", "paper sell 5", "watch NVDA" — return those in "trades" and the
  server executes them against last close and keeps the ledger in
  memory/portfolio.md. You NEVER touch real money, a real brokerage, or place
  a real trade — and paper trading is never mentioned outside the adults room.
- You never register, pay, book or sign. You hand over the link.
`.trim();

/** What the calendar feeds hold right now, named honestly. */
async function calendarNow() {
  if (!calendarReady()) return "Calendar: no feeds connected yet. An adult can add one in the app.";
  const names = feeds().map((f) => f.label).join(", ") || "unlabelled";
  try {
    const cal = await upcoming(8);
    if (cal.error) return `Calendar: a feed failed to load (${cal.error}).`;
    if (cal.events.length) return `Calendar feeds connected (${names}), next 8 days:
${asLines(cal.events)}`;
    return cal.total === 0
      ? `Calendar feeds connected (${names}) but EMPTY at any date — nobody has put events on them yet.`
      : `Calendar feeds connected (${names}); nothing in the next 8 days.`;
  } catch (e) {
    return `Calendar: read failed (${e?.message || e}).`;
  }
}

/** The topic files whose subject the question touches, inlined whole. */
function topicContext(question) {
  const hits = matchTopics(question);
  if (!hits.length) return "";
  return "\nTopic files that match this question:\n" +
    hits.map((t) => `----- memory/topics/${t.file} -----\n${t.text}`).join("\n\n");
}

async function weatherNow() {
  try {
    const wx = await forecast();
    return wx.days ? `Weather:\n${asWeatherLines(wx.days)}` : `Weather unavailable (${wx.error}).`;
  } catch (e) { return `Weather unavailable (${e?.message || e}).`; }
}

async function portfolioNow() {
  try { return await portfolioSummary(); }
  catch (e) { return `Paper portfolio unreadable (${e?.message || e}).`; }
}

/** Answer anyone in the family, from memory. */
export async function answer(who, question, recent = [], room = "family") {
  const system = `You are Hearth, this household's assistant.\n\n${context()}\n\n${ROOMS}\n\n${VOICE}\n\n` +
    (room === "adults"
      ? "You are answering in the ADULTS room. Jeffery and Suzan only. Speak plainly about money, health and anything else that belongs here."
      : room === "me"
      ? `You are in ${who}'s PRIVATE scratchpad. Only ${who} sees this — not the rest of the family. This is where they think out loud, plan, and get their words straight before saying something in the family channel. Help them think; never act outward from here, and never carry anything said here into another room. If what they are working on needs the family to know, help them phrase it and let them post it themselves.`
      : "You are answering in the FAMILY room. A nine-year-old and a two-year-old can read this. If the honest answer belongs in the adults room, say only that you will take it up with Jeffery and Suzan — never hint at what it concerns.");
  const thread = recent.map((m) => `${m.who}: ${m.text}`).join("\n");
  const where = room === "me" ? "their private scratchpad" : room === "adults" ? "the adults room" : "the family channel";
  const user = `${nowLine()}

${who} just asked, in ${where}:

${question}

Recent channel messages for context:
${thread || "(none)"}

${CAN_AND_CANNOT}

Right now:
${await calendarNow()}
${await weatherNow()}${room === "adults" ? `\n${await portfolioNow()}` : ""}
${topicContext(question)}

${who === "Aiden"
  ? `Aiden is nine, in fourth grade, and smart — Jeffery's correction, and it
outranks any earlier setting: write for him at a FIFTH-grade level, a notch
above where he is, because that is where it stays engaging instead of
babyish. Clear sentences, real vocabulary with context to carry it, and talk
to him like the capable kid he is. Keep it tight — if it reads like a memo he
will ignore it and he will be right to.`
  : who === "Abby"
  ? "Abby is two; a parent is reading this aloud or she is hearing it spoken. Two or three short, warm sentences at most."
  : ""}

Answer ${who} directly. Check memory before answering — most questions have
already been answered once, and re-asking is the fastest way to get abandoned.
If you do not know, say so and say you will find out.

Return ONLY a JSON object, no prose around it:
{"text": "<your answer>",
 "loops": [{"section": "Urgent" | "This week" | "Dated, further out",
            "title": "<one line, starts with who owes the action>",
            "detail": "<when, what, and anything needed to actually do it>",
            "due": "<YYYY-MM-DD HH:MM in 24h Eastern, ONLY when a real moment was named — \"at 3\", \"tomorrow morning\", \"before practice Thursday\". Date alone means 09:00. Omit when no time was meant.>",
            "for": "<Jeffery | Suzan | Aiden | Abby — only when the reminder is clearly for one person; the push then goes to their phone alone>"}]${room === "adults" ? `,
 "trades": [{"op": "buy" | "sell" | "watch" | "unwatch", "symbol": "AAPL", "qty": 10, "reason": "<their reasoning, kept for the ledger>"}]` : ""}}${room === "adults" ? `

"trades" is ONLY for an explicit paper-trading instruction from ${who} in this
message — never inferred, never proactive, never from your own opinion of a
stock. Confirm what happened in "text" (the server appends the executed
outcome). Omit the field otherwise.` : ""}

"loops" is for anything ${who} just asked you to remember or committed to —
a reminder, a task, a promise. Usually empty. ${room === "me"
  ? `THIS IS A PRIVATE SCRATCHPAD. "loops" must ALWAYS be empty here — the To do
list is shared. There are exactly two ways anything leaves this room, and you
use two extra JSON fields for them:

"relay": ONLY when ${who} explicitly asks you to send or post something to the
family — put the exact text to post there. It appears in Family under ${who}'s
own name. Never relay anything they did not ask to send, and confirm in "text"
that it was posted. If they asked but the wording is still rough, help them
finish it first and relay when they say it is ready.

"alert": ONLY for genuine safety — ${who} talking about suicide or self-harm,
hurting someone, real-world violence, a child planning something illegal or
dangerous, or sadness deep enough to need real help beyond a chat. Put a short,
factual, compassionate note for the parents there; it reaches Jeffery and Suzan
privately, never the family room. When you use it, tell ${who} plainly and
kindly in "text" that you are bringing in their parents because their safety
matters more than privacy. For suicide or self-harm, "text" must also include:
call or text 988, the Suicide and Crisis Lifeline — free, always answered.
This is rare and serious. Never for venting, a bad day, ordinary anger, sad
songs, fiction or games, or secrets like presents. A false alarm teaches
everyone the private room is not private.

When neither applies, omit both fields — and neither applies almost always.`
  : "Open one when it is asked for or clearly promised, not for every mention of the future."}`;

  const raw = await ask(system, user, 2500, { kind: "chat", effort: "medium" });
  const o = tryParse(raw);
  if (!o) return { text: raw, loops: [] };
  return {
    text: String(o.text || "").trim() || raw,
    loops: room === "me" ? [] : (Array.isArray(o.loops) ? o.loops.slice(0, 3) : []),
    // The two doors out of a scratchpad. Anywhere else these fields are noise
    // and are dropped here, so the model cannot invent a third door.
    relay: room === "me" && o.relay ? String(o.relay).trim().slice(0, 4000) : "",
    alert: room === "me" && o.alert ? String(o.alert).trim().slice(0, 2000) : "",
    // Paper trades execute only from the adults room; anywhere else the field
    // is dropped unread, same as the scratchpad doors.
    trades: room === "adults" && Array.isArray(o.trades) ? o.trades.slice(0, 5) : [],
  };
}

/**
 * The 22:00 self-review. Reads the whole day back, works out what it got wrong,
 * and rewrites the long-lived memory layers. This is the only writer of memory.
 */
export async function review() {
  // Byte-identical to wake()'s system on purpose: the review runs a minute
  // after the 22:00 wake, inside the cache window, and this prefix is the
  // day's biggest. A different framing sentence here forced the most
  // expensive call of the day to re-read all of memory uncached; the framing
  // lives in the user turn now.
  const system = `You are Hearth, this household's assistant.\n\n${context()}\n\n${ROOMS}\n\n${VOICE}`;

  const user = `You are reviewing your own day. Nobody reads this — it is you
talking to your future self. Be honest rather than flattering.

Today is ${todayET()}. Here is everything that happened:

${readDaily() || "(nothing logged today)"}

Review it. The question is "what did I get wrong today?", not "what happened
today?" A review that only records events makes memory fatter; one that examines
its own errors makes it sharper. If you finish having logged no miss and no
uncertainty, you were not looking hard enough — but say so rather than inventing
one.

Then rewrite the memory layers that changed:
- facts.md — promote only what has been observed more than once. One event is
  not a fact. Mark provenance: told, or observed N times. When something new
  contradicts something old, keep the newer and note the change; never leave both.
- rhythms.md — anything recurring that must fire. Update what fired, decay what
  was expected and did not happen, promote anything that has now repeated a third
  time, capture any registration or payment link that got used.
- open-loops.md — close what is done, add what was newly promised, flag anything
  untouched over a week. Nothing leaves for being old.
  The app writes into this file too. A bullet starting "- [done DATE by NAME]"
  was ticked off by that person in the app: that is a human statement and it
  outranks your reading of the day. Remove those bullets — they are finished and
  the list has to stay short enough to read — but if one was ticked and something
  today says it is NOT actually done, keep it and say so plainly. Never re-open a
  ticked loop silently, and never invent a "- [done ...]" bullet yourself; only a
  person tapping the box writes one. Keep the "- [opened DATE] **Title**" shape
  on everything still open, one blank line between bullets, and "## " headings —
  that shape is what the app's checkboxes are built on.
- corrections.md — anything a human explicitly said you got wrong. Highest
  authority in the system. Never soften these.
  Daily-log lines marked "[private NAME]" came from that person's scratchpad.
  They may inform what you understand, but their content must never surface in
  any check-in, any other room, or any memory line another person would read.
  If one belongs in memory at all, strip it to the bare fact and drop who was
  thinking about it.
- misses.md — where you were wrong, and the adjustment it implies.
- reference.md — only if a genuinely new detail surfaced.

Then FILE the day's knowledge by subject. memory/topics/ holds one file per
topic — soccer, merit-school, bills, whatever this family actually talks about —
so "everything about X" is one open, not a dig through daily logs. Existing
topics:
${listTopics().map((t) => `- ${t.file}: ${t.title}`).join("\n") || "(none yet — create the first ones)"}

On SUNDAYS also draft next week's dinner plan into meals.md under "## This
week": seven nights, drawn from "How this family eats" and what the daily logs
show got cooked, swapped or vetoed. Fast or ready-ahead food on soccer nights
(Tue/Thu end 18:30). A plan nobody asked to change all week is a good week —
note what got swapped and why in "How this family eats" so next Sunday is
smarter.

Rules for topics: filenames are kebab-case like soccer.md. Start each with
"# Title" then an "aliases: other, names" line so retrieval finds it by any
name the family uses. Dated bullets, newest first, provenance marked like the
other layers. Update a topic when today touched it; create one when a subject
has clearly arrived to stay; merge or delete topics on Sundays if they overlap.
Private-scratchpad lines never enter a topic file.

Return ONLY a JSON object, no prose around it:
{"summary": "<3-5 lines for the daily log>",
 "files": {"facts.md": "<complete new contents>", ...},
 "topics": {"soccer.md": "<complete new contents>", ...}}

Include a file ONLY if it actually changed. Each value must be the COMPLETE new
file, not a diff. If nothing changed, return {"summary": "...", "files": {}, "topics": {}}.

"topics" is NOT optional bookkeeping: any subject today's log touched more than
in passing gets filed or updated — the first live review returned {} and a
whole Sunday of schedules, school detail and soccer went unfiled. And the
weekly dinner plan: if meals.md holds no current "## This week" plan, draft one
TONIGHT whatever day it is — a missed Sunday must not mean a week without a
plan.`;

  const raw = await ask(system, user, 16000, { kind: "review" });
  let out = tryParse(raw);

  // A review is an entire day's learning in one JSON object, and on 22 Aug the
  // model produced it with a single stray bracket — parse failed, memory went
  // unwritten, and 16KB of near-JSON got dumped into the daily log. One comma
  // must not cost a day: hand the broken output back and ask for it repaired.
  if (!out) {
    const fixed = await ask(
      "You repair malformed JSON. Return ONLY the corrected JSON object — no prose, no code fences, not a word outside the braces. Preserve the content exactly; fix only the syntax.",
      `This was meant to be one valid JSON object of the shape {"summary": "...", "files": {"name.md": "..."}} but it does not parse:

${raw}`,
      16000,
      { kind: "review" },
    );
    out = tryParse(fixed);
  }

  if (!out) {
    // Still broken. Save the raw somewhere it can be recovered by hand, and say
    // so — a clean admission beats 16KB of noise in the daily log.
    const p = path.join(DATA, `review-failed-${todayET()}.txt`);
    try { fs.mkdirSync(DATA, { recursive: true }); fs.writeFileSync(p, raw); } catch { /* the log line below still tells the story */ }
    return {
      summary: `The 22:00 review produced output I could not parse, twice. Memory is unchanged tonight; the raw output is saved at ${p} for the next session to salvage.`,
      files: {},
    };
  }

  const files = Object.fromEntries(
    Object.entries(out.files || {}).filter(([f]) => LAYERS.includes(f)),
  );
  // Topic names are validated at write time; pass them through as claimed.
  const topics = out.topics && typeof out.topics === "object" ? out.topics : {};
  return { summary: String(out.summary || "").trim(), files, topics };
}

function tryParse(raw) {
  const i = raw.indexOf("{"), j = raw.lastIndexOf("}");
  if (i < 0 || j < i) return null;
  try {
    const o = JSON.parse(raw.slice(i, j + 1));
    return o && typeof o === "object" ? o : null;
  } catch { return null; }
}

/**
 * A photograph of school paper, turned into something that will not be lost.
 *
 * This is the failure this house actually has: a form comes home in a bag,
 * surfaces the morning it is due, and by then the pen, the cheque and the time
 * are all missing. The input is a nine-year-old holding up a piece of paper.
 *
 * It reads the paper and returns both a message for the channel and the loops
 * it wants opened. It never signs anything and never claims to have returned
 * anything — a photograph is evidence that paper exists, not that it is handled.
 */
export async function readPaper(who, images, note = "") {
  const system = `You are Hearth, this household's assistant, reading a photograph of
a document somebody in the family has just held up to a phone.\n\n${context()}\n\n${ROOMS}\n\n${VOICE}`;

  const user = [
    ...images.map((im) => ({
      type: "image",
      source: { type: "base64", media_type: im.media_type, data: im.data },
    })),
    {
      type: "text",
      text: `${who} photographed this${note ? ` and said: "${note}"` : ""}. Today is ${todayET()}.

Read it. Work out, and say which of these you could NOT find rather than guessing:
what it is for, which child, the date of the event, the deadline to return it,
whether money is involved and how much, and exactly what is required —
information, a signature, payment, or all three.

Then decide what has to be remembered. A form that needs a wet signature on the
original becomes an open loop with its deadline and where the paper is. A form
that needs information becomes an open loop too, listing which fields you can
already fill from memory/reference.md and which are genuinely unknown.

You cannot produce a filled document from here yet. Do not imply you have. Say
what is blank and let a human fill it.

If the photograph is not a document — it is a child's drawing, a screenshot, a
picture of the dog — say so plainly in one line and open no loops.

Return ONLY a JSON object, no prose around it:
{"room": "family" | "adults",
 "text": "<what goes in the channel: what this is, when it is due, what it costs, what is required, what you could not read>",
 "loops": [{"section": "Urgent" | "This week" | "Dated, further out",
            "title": "<one line, starts with who owes the action>",
            "detail": "<the deadline, what is required, where the paper physically is, and what is still unknown>"}]}

"loops" is usually one item and may be empty. A notice with nothing to do is not
an open loop — it is a line in the channel and nothing more.`,
    },
  ];

  const raw = await ask(system, user, 4000, { kind: "photo" });
  const i = raw.indexOf("{"), j = raw.lastIndexOf("}");
  if (i < 0 || j < i) return { room: "family", text: raw, loops: [] };
  try {
    const o = JSON.parse(raw.slice(i, j + 1));
    return {
      room: o.room === "adults" ? "adults" : "family",
      text: String(o.text || "").trim(),
      loops: Array.isArray(o.loops) ? o.loops.slice(0, 4) : [],
    };
  } catch {
    return { room: "family", text: raw, loops: [] };
  }
}

/**
 * The lab's auto-trader. Runs once, at the 17:00 wake, after the close.
 * Reads the latest research note, the strategy file, and the live book, and
 * decides paper trades — which the SERVER then bounds and executes; the model
 * proposes, markets.js keeps the books. Pretend money, forever.
 */
export async function trader(note, strategy, book) {
  const system = `You are Hearth's trading half, in a two-agent learning lab.
Pretend money, real prices. The design session researches the world; you trade
the thesis and record reasoning precise enough to be graded CORRECT, LUCKY,
WRONG or UNTESTED tomorrow morning. You are learning a craft, not maximising a
number — a well-reasoned loss teaches more than an unexplained win.

THE STRATEGY FILE IS LAW. Watchlist only, respect its caps, and when in doubt
do nothing: "no trade" is a valid, gradeable decision.`;

  const user = `Today is ${todayET()}, after the close.

----- research/strategy.md -----
${strategy || "(missing — do not trade without a strategy)"}

----- latest research note -----
${note || "(no note yet — trade only on the strategy's standing hypotheses, or hold)"}

----- the book right now -----
${book}

Decide. Return ONLY JSON:
{"thinking": "<3-6 lines: what today's information changes, or why it changes nothing>",
 "trades": [{"op": "buy"|"sell"|"watch", "symbol": "NVDA", "qty": 10, "reason": "<the gradeable claim behind this trade>"}]}

"trades" may be empty and often should be.`;

  const raw = await ask(system, user, 3000, { kind: "trader" });
  const o = tryParse(raw);
  if (!o) return { thinking: raw.slice(0, 500), trades: [] };
  return {
    thinking: String(o.thinking || "").trim(),
    trades: Array.isArray(o.trades) ? o.trades.slice(0, 3) : [],
  };
}
