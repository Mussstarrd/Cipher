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
import fs from "node:fs";
import path from "node:path";
import { DATA } from "./memory.js";

// Feeds added from the app live here rather than in .env, so nobody has to open
// an SSH session on a phone to connect a calendar. Deliberately NOT state.json:
// an ICS secret address is a password, and state.json is what gets backed up.
const FEEDS = path.join(DATA, "calendars.json");

const fromEnv = () => (process.env.CALENDAR_ICS_URLS || "")
  .split(",").map((s) => s.trim()).filter(Boolean);

const fromDisk = () => {
  try { return JSON.parse(fs.readFileSync(FEEDS, "utf8")).map((x) => x.url); }
  catch { return []; }
};

const urls = () => [...new Set([...fromEnv(), ...fromDisk()])];

export const calendarReady = () => urls().length > 0;

/** What the app may see: a label and who added it. Never the URL itself. */
export function feeds() {
  let saved = [];
  try { saved = JSON.parse(fs.readFileSync(FEEDS, "utf8")); } catch { /* none yet */ }
  return [
    ...fromEnv().map((u) => ({ label: name(u), by: "the .env file", removable: false })),
    ...saved.map((x) => ({ label: x.label || name(x.url), by: x.by || "someone", at: x.at, removable: true, id: x.id })),
  ];
}

export function addFeed(url, by, label = "") {
  const u = String(url || "").trim();
  if (!/^https?:\/\/\S+$/.test(u)) return { ok: false, error: "that is not a URL" };
  if (!/\.ics(\?|$)/i.test(u) && !/ical/i.test(u)) {
    return { ok: false, error: "that does not look like an iCal address — it should end in .ics" };
  }
  if (urls().includes(u)) return { ok: true, duplicate: true };
  let saved = [];
  try { saved = JSON.parse(fs.readFileSync(FEEDS, "utf8")); } catch { /* none yet */ }
  saved.push({ id: `c${Date.now()}`, url: u, label: label.trim() || name(u), by, at: new Date().toISOString() });
  fs.mkdirSync(DATA, { recursive: true });
  fs.writeFileSync(FEEDS, JSON.stringify(saved, null, 2));
  return { ok: true };
}

export function removeFeed(id) {
  let saved = [];
  try { saved = JSON.parse(fs.readFileSync(FEEDS, "utf8")); } catch { return { ok: false, error: "none saved" }; }
  const left = saved.filter((x) => x.id !== id);
  fs.writeFileSync(FEEDS, JSON.stringify(left, null, 2));
  return { ok: true, removed: saved.length - left.length };
}

const name = (u) => {
  try { return decodeURIComponent(new URL(u).pathname.split("/").filter(Boolean).slice(-2, -1)[0] || "calendar"); }
  catch { return "calendar"; }
};

/** Events between now and `days` ahead, expanding anything recurring. */
export async function upcoming(days = 8) {
  if (!calendarReady()) return { events: [], total: 0, error: null };
  const from = new Date();
  const to = new Date(Date.now() + days * 864e5);
  const out = [];
  const problems = [];
  // How many events the feeds hold in total, at any date. An empty window and
  // an empty *calendar* mean completely different things, and telling them
  // apart is the difference between "a quiet week" and "this family does not
  // keep its schedule here". Only one of those is safe to say out loud.
  let total = 0;

  await Promise.all(urls().map(async (url) => {
    const cal = name(url);
    try {
      const data = await ical.async.fromURL(url);
      for (const ev of Object.values(data)) {
        if (ev.type !== "VEVENT") continue;
        total += 1;
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
  return { events: out, total, error: problems.length ? problems.join("; ") : null };
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
