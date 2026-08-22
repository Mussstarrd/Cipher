/**
 * Calendars, without OAuth.
 *
 * Every Google Calendar has a private "Secret address in iCal format" under
 * Settings → Integrate calendar. It is a plain URL that returns ICS over HTTPS
 * with no authentication at all — so Hearth can read the family calendar with
 * nothing to authorise, nothing to verify and nothing that expires.
 *
 * Read-only, deliberately. Hearth proposes events and a human adds them; it
 * never puts something on the family calendar on its own.
 *
 * Treat those URLs like passwords: anyone holding one can read the calendar.
 */
import ical from "node-ical";

const URLS = (process.env.CALENDAR_ICS_URLS || "")
  .split(",").map((s) => s.trim()).filter(Boolean);

export const calendarReady = () => URLS.length > 0;

const name = (u) => {
  try { return decodeURIComponent(new URL(u).pathname.split("/").filter(Boolean).slice(-2, -1)[0] || "calendar"); }
  catch { return "calendar"; }
};

/** Events between now and `days` ahead, expanding anything recurring. */
export async function upcoming(days = 8) {
  if (!calendarReady()) return { events: [], error: null };
  const from = new Date();
  const to = new Date(Date.now() + days * 864e5);
  const out = [];
  const problems = [];

  await Promise.all(URLS.map(async (url) => {
    const cal = name(url);
    try {
      const data = await ical.async.fromURL(url);
      for (const ev of Object.values(data)) {
        if (ev.type !== "VEVENT") continue;
        const push = (start) => {
          if (start < from || start > to) return;
          const ms = ev.end && ev.start ? ev.end - ev.start : 0;
          out.push({
            calendar: cal,
            summary: ev.summary || "(untitled)",
            location: ev.location || "",
            start: start.toISOString(),
            end: new Date(+start + ms).toISOString(),
            allDay: ev.datetype === "date",
          });
        };
        if (ev.rrule) {
          // Recurring: expand only the window, and honour cancelled instances.
          for (const d of ev.rrule.between(from, to, true)) {
            const key = d.toISOString().slice(0, 10);
            if (ev.exdate && Object.keys(ev.exdate).some((k) => k.startsWith(key))) continue;
            push(d);
          }
        } else if (ev.start) {
          push(new Date(ev.start));
        }
      }
    } catch (e) {
      problems.push(`${cal}: ${e?.message || e}`);
    }
  }));

  out.sort((a, b) => a.start.localeCompare(b.start));
  // A calendar that failed to load must never look like an empty week.
  return { events: out, error: problems.length ? problems.join("; ") : null };
}

/** Compact lines for a wake prompt. */
export function asLines(events, tz = "America/New_York") {
  const fmt = (iso, allDay) =>
    new Date(iso).toLocaleString("en-GB", {
      timeZone: tz, weekday: "short", day: "numeric", month: "short",
      ...(allDay ? {} : { hour: "2-digit", minute: "2-digit" }),
    });
  return events.map((e) =>
    `- ${fmt(e.start, e.allDay)} — ${e.summary}${e.location ? ` (${e.location})` : ""} [${e.calendar}]`,
  ).join("\n");
}
