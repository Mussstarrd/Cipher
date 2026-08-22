/**
 * The thinking. Four scheduled wakes with four different jobs, plus answering
 * whatever the family asks in between.
 *
 * The 22:00 wake is the one that matters: it is the only place memory is
 * rewritten, and it asks "what did I get wrong" rather than "what happened".
 * Recording makes a system fatter. Reviewing its own errors makes it sharper.
 */
import Anthropic from "@anthropic-ai/sdk";
import { loadBrief, readDaily, todayET, LAYERS } from "./memory.js";

const client = new Anthropic();
const MODEL = "claude-opus-5";

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

async function ask(system, user, maxTokens = 4000) {
  const r = await client.messages.create({
    model: MODEL,
    max_tokens: maxTokens,
    thinking: { type: "adaptive" },
    cache_control: { type: "ephemeral" },   // the memory prefix is identical every wake
    system,
    messages: [{ role: "user", content: user }],
  });
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
  const system = `You are Hearth, this household's assistant.\n\n${context()}\n\n${VOICE}`;
  const user = `It is ${slot} on ${todayET()}. Produce the ${slot} check-in.

${SLOT_JOB[slot]}

Today's log so far:
${readDaily() || "(nothing logged yet today)"}
${extra ? `\nAlso relevant right now:\n${extra}` : ""}

Write only the check-in itself. No preamble, no sign-off.`;
  return ask(system, user);
}

/** Answer anyone in the family, from memory. */
export async function answer(who, question, recent = []) {
  const system = `You are Hearth, this household's assistant.\n\n${context()}\n\n${VOICE}`;
  const thread = recent.map((m) => `${m.who}: ${m.text}`).join("\n");
  const user = `${who} just asked, in the family channel:

${question}

Recent channel messages for context:
${thread || "(none)"}

Answer ${who} directly. Check memory before answering — most questions have
already been answered once, and re-asking is the fastest way to get abandoned.
If you do not know, say so and say you will find out.`;
  return ask(system, user, 2000);
}

/**
 * The 22:00 self-review. Reads the whole day back, works out what it got wrong,
 * and rewrites the long-lived memory layers. This is the only writer of memory.
 */
export async function review() {
  const system = `You are Hearth, reviewing your own day. Nobody reads this — it
is you talking to your future self. Be honest rather than flattering.\n\n${context()}\n\n${VOICE}`;

  const user = `Today is ${todayET()}. Here is everything that happened:

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
- corrections.md — anything a human explicitly said you got wrong. Highest
  authority in the system. Never soften these.
- misses.md — where you were wrong, and the adjustment it implies.
- reference.md — only if a genuinely new detail surfaced.

Return ONLY a JSON object, no prose around it:
{"summary": "<3-5 lines for the daily log>",
 "files": {"facts.md": "<complete new contents>", ...}}

Include a file ONLY if it actually changed. Each value must be the COMPLETE new
file, not a diff. If nothing changed, return {"summary": "...", "files": {}}.`;

  const raw = await ask(system, user, 16000);
  const start = raw.indexOf("{");
  const end = raw.lastIndexOf("}");
  if (start < 0 || end < start) return { summary: raw, files: {} };
  try {
    const out = JSON.parse(raw.slice(start, end + 1));
    const files = Object.fromEntries(
      Object.entries(out.files || {}).filter(([f]) => LAYERS.includes(f)),
    );
    return { summary: String(out.summary || "").trim(), files };
  } catch {
    // Never let a parse failure silently discard a day. Keep the prose.
    return { summary: raw, files: {} };
  }
}
