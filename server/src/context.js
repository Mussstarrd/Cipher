/**
 * Everything true right now that a check-in needs but memory cannot hold:
 * the calendar, the mail since last time, and — just as important — every way
 * in which Hearth's own inputs are currently broken.
 *
 * Separated from the server so `npm run preflight -- --brief` renders exactly
 * what the family would receive. A preview built from different inputs than the
 * real thing is worse than no preview: it is a rehearsal of a different play.
 */
import { upcoming, asLines, calendarReady } from "./calendar.js";
import { backupReady } from "./backup.js";
import { forecast, asWeatherLines } from "./weather.js";
import { summary as portfolioSummary, loadBook } from "./markets.js";
import fs from "node:fs";
import path from "node:path";
import { TOPICS } from "./memory.js";

/**
 * @param slot  which check-in
 * @param s     persisted state
 * @param late  minutes past the slot, 0 if on time
 * @returns {{extra: string, calTotal: number|null, calError: string|null}}
 */
export async function wakeContext(slot, s, late = 0) {
  const since = s.reports?.[0]?.at || new Date(Date.now() - 864e5).toISOString();
  const fresh = (s.mail || []).filter((m) => m.at > since);
  const cal = await upcoming(8);
  const wx = await forecast();
  const book = loadBook();
  const hasPaper = Object.keys(book.positions).length > 0 || book.watch.length > 0;
  const paper = hasPaper ? await portfolioSummary() : "";

  const extra = [
    calendarReady()
      ? (cal.events.length
          ? `Calendar, next 8 days:\n${asLines(cal.events)}`
          : cal.total > 0
            // Genuinely a quiet week.
            ? "Calendar reachable, nothing scheduled in the next 8 days."
            // Reachable but completely empty at any date. Saying "nothing on
            // this week" here would contradict rhythms.md, which is where this
            // family's schedule actually lives — and one confidently wrong
            // "you're free" costs more trust than a whole quiet day.
            : "The connected calendar is reachable but EMPTY — it holds no events at any date, so this family does not keep its schedule there. Do not treat this as a free week, and do not say the calendar shows anything. Work from rhythms.md and open-loops.md.")
      : "",

    cal.error
      ? `WARNING: a calendar failed to load (${cal.error}). Say so; do not present this as an empty week.`
      : "",

    wx.days
      ? `Weather, Lake of the Woods:\n${asWeatherLines(wx.days)}\nMention weather ONLY when it changes a plan — practice under a thunderstorm forecast, a pool party against rain, a hard freeze. A pleasant day is not a line in a check-in.`
      : (wx.error ? `WARNING: weather could not be fetched (${wx.error}).` : ""),

    paper
      ? `${paper}\nThe paper portfolio is pretend money for learning. Everything about it is MONEY and belongs in the adults part of the check-in, never the family part. Mention it at 07:00 and 17:00 only, one or two lines, and only if something moved or a watch-listed ticker did something notable.`
      : "",

    // Dinner is the 17:00 check-in's job, and the plan lives in the meals
    // topic. Loaded only for this slot — the working set stays flat.
    (slot === "17:00" && fs.existsSync(path.join(TOPICS, "meals.md")))
      ? `----- memory/topics/meals.md -----\n${fs.readFileSync(path.join(TOPICS, "meals.md"), "utf8").slice(0, 4000)}\nTonight's dinner comes from the plan above, adjusted by anything in today's log (a swap said in the channel beats the plan). Soccer nights end 18:30 — call out when the plan fights the clock.`
      : "",

    fresh.length
      ? `Mail since the last check-in:\n${fresh.map((m) =>
          `- ${m.from} | ${m.subject}\n  ${m.text.slice(0, 700)}`).join("\n")}`
      : "No new mail since the last check-in.",

    s.mailError
      ? `WARNING: the inbox could not be read (${s.mailError}). Say so in the check-in; do not present this as a quiet day.`
      : "",

    s.backupError
      ? `WARNING: memory has not been backed up since ${s.backupErrorSince || "the last success"} (${s.backupError}). Raise this once, plainly — everything learned is currently on one disk.`
      : "",

    // The OFF case used to be the only silent one, which made it the most
    // dangerous: with no remote configured backup() returned instantly and
    // nobody was ever told. Say it — but at the close-out only, once a day,
    // and right after the pass that rewrote memory.
    (!backupReady() && slot === "22:00")
      ? "WARNING: there is no off-machine backup of memory. Everything you have learned about this family exists on one disk. Say this once, in one line, without alarm — it needs a private repo and BACKUP_GIT_REMOTE set."
      : "",

    late
      ? `You are ${late} minutes late for this check-in — the machine was restarting or busy at ${slot}. Open by saying it is late, in one short clause. Do not write as though it were still ${slot}.`
      : "",
  ].filter(Boolean).join("\n\n");

  return { extra, calTotal: cal.error ? null : cal.total, calError: cal.error };
}
