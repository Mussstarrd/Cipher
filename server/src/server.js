/**
 * Hearth — a hibernating household assistant.
 *
 * It sleeps. Four times a day it wakes, reads its own memory, works out what
 * the family needs to know, writes what it learned back to disk, and pushes a
 * notification. In between it answers whatever anyone asks.
 *
 * No cron, no external scheduler, no platform. It owns its own clock.
 */
import http from "node:http";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { wake, answer, review } from "./brain.js";
import { notify, pushReady } from "./push.js";
import { fetchNew, send as sendMail, mailReady, credentialWarning } from "./mail.js";
import { calendarReady } from "./calendar.js";
import { backup, backupReady, backupDir } from "./backup.js";
import {
  loadState, saveState, appendDaily, writeLayer, todayET, loadBrief,
} from "./memory.js";
import { SLOTS, dueSlot, GRACE_MIN, MAX_TRIES } from "./schedule.js";
import { wakeContext } from "./context.js";

const here = path.dirname(fileURLToPath(import.meta.url));
const PUB = path.resolve(here, "..", "public");
const PORT = Number(process.env.PORT || 8787);
const URL_BASE = process.env.HEARTH_URL || `http://localhost:${PORT}`;
const PASS = process.env.HEARTH_PASSPHRASE || "";
// A second passphrase is the whole boundary between the rooms. Without real
// accounts, a name picked from a list is a label, not a wall — a nine-year-old
// can tap "Jeffery". A separate secret the children do not have is a wall.
const ADULT_PASS = process.env.HEARTH_ADULT_PASSPHRASE || "";
const TZ = "America/New_York";

/* ---------- the clock ---------- */
// Read wall-clock time in Eastern directly, so DST is handled by the calendar
// rather than by remembering to edit a cron expression twice a year.
function nowET() {
  const p = Object.fromEntries(
    new Intl.DateTimeFormat("en-CA", {
      timeZone: TZ, hourCycle: "h23",
      year: "numeric", month: "2-digit", day: "2-digit",
      hour: "2-digit", minute: "2-digit",
    }).formatToParts(new Date()).map((x) => [x.type, x.value]),
  );
  return { day: `${p.year}-${p.month}-${p.day}`, hm: `${p.hour}:${p.minute}` };
}


/* ---------- the inbox ---------- */
// Ingestion is separate from thinking on purpose: pulling mail is free and can
// run often, while reasoning about it is expensive and happens on the wakes.
let ingesting = false;
async function ingest() {
  if (!mailReady() || ingesting) return;
  ingesting = true;
  try {
    const s = loadState();
    const { messages, lastUid, error } = await fetchNew(s.lastUid);
    if (error) {
      console.error(`[hearth] mail: ${error}`);
      // A failing inbox must never look like a quiet one.
      const st = loadState();
      st.mailError = error;
      saveState(st);
      return;
    }
    if (!messages.length) {
      const st = loadState();
      st.lastUid = lastUid; st.mailError = null;
      saveState(st);
      return;
    }
    const st = loadState();
    st.mail = [...st.mail, ...messages].slice(-120);
    st.lastUid = lastUid;
    st.mailError = null;
    saveState(st);
    appendDaily(messages.map((m) =>
      `- mail from ${m.from} — ${m.subject}`).join("\n"));
    console.log(`[hearth] ingested ${messages.length} message(s)`);
  } finally {
    ingesting = false;
  }
}
setInterval(ingest, 10 * 60_000);
setTimeout(ingest, 4000);

let running = false;

// Last known total size of the calendar feeds, so /api/state can distinguish
// "quiet week" from "empty calendar" without refetching on every poll. null
// until the first wake has looked — which is "unknown", not "fine".
let calTotal = null;

async function runSlot(slot, { forced = false, late = 0 } = {}) {
  if (running) return null;
  running = true;
  const s = loadState();
  try {
    // Count the attempt before doing any work. A wake that takes the process
    // down with it must still count as a try, or a reliably-failing slot turns
    // into a restart loop that burns a model call on every tick.
    if (!forced) {
      const d = todayET();
      const prev = s.wakeTries?.[slot];
      s.wakeTries = {
        ...(s.wakeTries || {}),
        [slot]: { day: d, n: (prev?.day === d ? prev.n : 0) + 1, at: new Date().toISOString() },
      };
      saveState(s);
    }
    const { extra, calTotal: total } = await wakeContext(slot, s, late);
    if (total !== null) calTotal = total;

    const out = await wake(slot, extra);
    const text = out.family;

    appendDaily(`## ${slot} check-in\n\n${text}` + (out.adults ? `\n\n### adults only\n\n${out.adults}` : ""));

    const subs = await notify(loadState().subs, {
      title: `Hearth — ${slot}${late ? " (late)" : ""}`,
      body: text.split("\n").filter(Boolean)[0]?.slice(0, 140) || "Your check-in is ready.",
      url: URL_BASE,
    });

    // Reload before writing. The wake is tens of seconds of model call, and
    // ingest() or a backgrounded answer() will have saved their own work in the
    // meantime — writing back the copy loaded before the call silently deleted
    // whatever arrived during it: a family message, or a morning's mail.
    const after = loadState();
    after.subs = subs;
    after.reports.unshift({
      slot, at: new Date().toISOString(), day: todayET(), text,
      ...(late ? { late } : {}),
    });
    after.reports = after.reports.slice(0, 60);
    if (!forced) {
      after.lastRun[slot] = todayET();
      if (after.wakeTries) delete after.wakeTries[slot];
    }
    saveState(after);

    // The close-out is followed by the only pass that rewrites memory.
    if (slot === "22:00") {
      const { summary, files } = await review();
      for (const [f, content] of Object.entries(files)) writeLayer(f, content);
      appendDaily(`## Evening review\n\n${summary}`);
      const st = loadState();
      st.reports.unshift({
        slot: "review", at: new Date().toISOString(), day: todayET(),
        text: summary, internal: true,
      });
      saveState(st);
      console.log(`[hearth] review rewrote: ${Object.keys(files).join(", ") || "nothing"}`);
    }
    const backupErr = await backup(slot);
    if (backupErr) {
      console.error(`[hearth] BACKUP FAILED: ${backupErr}`);
      const st = loadState();
      st.backupError = backupErr;
      st.backupErrorSince ||= new Date().toISOString();
      saveState(st);
    } else {
      const st = loadState();
      st.backupError = null; st.backupErrorSince = null;
      st.backupAt = new Date().toISOString();
      st.backupOffMachine = backupReady();
      saveState(st);
    }

    console.log(`[hearth] ${slot} done`);
    return text;
  } catch (e) {
    // A failed wake is never silent. It goes in the log and on the wall.
    const msg = `Wake ${slot} FAILED: ${e?.message || e}`;
    console.error(`[hearth] ${msg}`);
    appendDaily(`## ${slot} — FAILED\n\n${msg}`);
    const st = loadState();
    st.reports.unshift({
      slot, at: new Date().toISOString(), day: todayET(),
      text: `I could not produce this check-in.\n\n${msg}\n\nThis is my failure, not a quiet day.`,
      failed: true, room: "family",
    });
    saveState(st);
    return null;
  } finally {
    running = false;
  }
}

function tick() {
  const { day, hm } = nowET();
  const d = dueSlot(hm, loadState(), day);
  if (!d) return;
  console.log(d.late === 0
    ? `[hearth] waking for ${d.slot}`
    : `[hearth] waking for ${d.slot}, ${d.late} min late — the exact minute was missed`);
  runSlot(d.slot, { late: d.late });
}
setInterval(tick, 30_000);
tick();

/* ---------- the door ---------- */
const send = (res, code, body, type = "application/json") => {
  res.writeHead(code, { "content-type": type, "cache-control": "no-store" });
  res.end(typeof body === "string" ? body : JSON.stringify(body));
};

const body = (req) => new Promise((ok, no) => {
  let d = ""; req.on("data", (c) => { d += c; if (d.length > 1e6) req.destroy(); });
  req.on("end", () => { try { ok(d ? JSON.parse(d) : {}); } catch (e) { no(e); } });
});

const authed = (b) => !PASS || b.pass === PASS;

// Funnel publishes this to the open internet — that is the point, so the family
// needs nothing installed. It also means the passphrase is the only door. Reads
// must be gated too: a check-in naming the children, the schools and the day's
// movements is not less sensitive than the ability to post.
const given = (u, req) => u.searchParams.get("pass") || req.headers["x-hearth-pass"] || "";
const adultGiven = (u, req) => u.searchParams.get("apass") || req.headers["x-hearth-apass"] || "";
const authedRead = (u, req) => !PASS || given(u, req) === PASS || adultGiven(u, req) === ADULT_PASS && !!ADULT_PASS;
// Adults-only content is served only to a request carrying the second secret.
const isAdult = (u, req) => Boolean(ADULT_PASS) && adultGiven(u, req) === ADULT_PASS;

const server = http.createServer(async (req, res) => {
  const u = new URL(req.url, URL_BASE);

  try {
    if (req.method === "GET" && u.pathname === "/api/state") {
      if (!authedRead(u, req)) return send(res, 401, { error: "passphrase required" });
      const s = loadState();
      const { brief } = loadBrief();
      const adult = isAdult(u, req);
      // Filtered server-side, never client-side: a hidden div is not a boundary.
      const visible = (x) => adult || (x.room || "family") === "family";
      return send(res, 200, {
        adult,
        adultRoomExists: Boolean(ADULT_PASS),
        reports: s.reports.filter((r) => !r.internal).filter(visible).slice(0, 12),
        messages: s.messages.filter(visible).slice(-80),
        // Only the roster table's rows — not every bold word in the brief.
        household: [...brief.matchAll(/^\|\s*\*\*([\w'-]+)\*\*\s*\|/gm)].map((m) => m[1]),
        slots: SLOTS, now: nowET(), push: pushReady(),
        vapid: process.env.VAPID_PUBLIC || null,
        calendar: {
          ready: calendarReady(),
          // null = not looked yet. Do not collapse that into false.
          empty: calTotal === null ? null : calTotal === 0,
        },
        // `ready` has always meant "there is a copy off this machine". Keep that
        // meaning — the wall must not go green because a local commit worked.
        backup: {
          ready: backupReady(), at: s.backupAt || null, error: s.backupError || null,
          local: backupDir(), offMachine: Boolean(s.backupOffMachine),
        },
        mail: { ready: mailReady(), count: s.mail?.length || 0, error: s.mailError || null,
                recent: (s.mail || []).slice(-15).map(({ from, subject, at }) => ({ from, subject, at })) },
        needsPass: Boolean(PASS),
      });
    }

    if (req.method === "POST" && u.pathname === "/api/message") {
      const b = await body(req);
      if (!authed(b)) return send(res, 401, { error: "wrong passphrase" });
      const who = String(b.who || "someone").slice(0, 40);
      const text = String(b.text || "").trim().slice(0, 4000);
      if (!text) return send(res, 400, { error: "empty" });
      // Posting to the adults room requires the adult secret, not a claim.
      const room = b.room === "adults" && ADULT_PASS && b.apass === ADULT_PASS ? "adults" : "family";

      const s = loadState();
      s.messages.push({ id: `m${Date.now()}`, who, text, room, at: new Date().toISOString() });
      saveState(s);
      appendDaily(`- ${who}: ${text}`);

      // Answer in the background; the poster should not wait on a model call.
      (async () => {
        try {
          const recent = loadState().messages.filter((m) => (m.room || "family") === room).slice(-8);
          const reply = await answer(who, text, recent, room);
          const st = loadState();
          st.messages.push({ id: `h${Date.now()}`, who: "Hearth", text: reply, room, at: new Date().toISOString() });
          saveState(st);
          appendDaily(`- Hearth: ${reply}`);
        } catch (e) {
          const st = loadState();
          st.messages.push({
            id: `h${Date.now()}`, who: "Hearth", failed: true,
            text: `I could not answer that: ${e?.message || e}`, room, at: new Date().toISOString(),
          });
          saveState(st);
        }
      })();
      return send(res, 200, { ok: true });
    }

    if (req.method === "POST" && u.pathname === "/api/subscribe") {
      const b = await body(req);
      if (!authed(b)) return send(res, 401, { error: "wrong passphrase" });
      const s = loadState();
      if (!s.subs.some((x) => x.endpoint === b.sub?.endpoint)) s.subs.push(b.sub);
      saveState(s);
      return send(res, 200, { ok: true });
    }


    if (req.method === "POST" && u.pathname === "/api/send") {
      const b = await body(req);
      if (!authed(b)) return send(res, 401, { error: "wrong passphrase" });
      if (!mailReady()) return send(res, 400, { error: "mail not configured" });
      // Deliberately only reachable from a human pressing a button. Hearth
      // drafts; a person sends. Nothing here is callable by the model.
      try {
        await sendMail({ to: b.to, subject: b.subject, body: b.body });
        appendDaily(`- ${b.who || "someone"} sent mail to ${b.to} — ${b.subject}`);
        return send(res, 200, { ok: true });
      } catch (e) {
        return send(res, 500, { error: String(e?.message || e) });
      }
    }

    if (req.method === "POST" && u.pathname === "/api/ingest") {
      const b = await body(req);
      if (!authed(b)) return send(res, 401, { error: "wrong passphrase" });
      await ingest();
      return send(res, 200, { ok: true, mail: loadState().mail.length });
    }

    if (req.method === "POST" && u.pathname === "/api/wake") {
      const b = await body(req);
      if (!authed(b)) return send(res, 401, { error: "wrong passphrase" });
      const slot = SLOTS.includes(b.slot) ? b.slot : "07:00";
      const text = await runSlot(slot, { forced: true });
      return send(res, 200, { ok: Boolean(text), text });
    }

    // static
    const file = u.pathname === "/" ? "index.html" : u.pathname.slice(1);
    const p = path.join(PUB, path.normalize(file).replace(/^(\.\.[/\\])+/, ""));
    if (fs.existsSync(p) && fs.statSync(p).isFile()) {
      const ext = path.extname(p);
      // A wrong content-type on an icon is silent: the browser simply never
      // offers to install the app, with no error anywhere.
      const type = { ".html": "text/html", ".js": "text/javascript",
        ".json": "application/json", ".css": "text/css", ".png": "image/png",
        ".svg": "image/svg+xml", ".ico": "image/x-icon",
        ".webmanifest": "application/manifest+json" }[ext] || "application/octet-stream";
      res.writeHead(200, { "content-type": type });
      return res.end(fs.readFileSync(p));
    }
    return send(res, 404, { error: "not found" });
  } catch (e) {
    return send(res, 500, { error: String(e?.message || e) });
  }
});

server.listen(PORT, () => {
  console.log(`[hearth] awake on ${URL_BASE}`);
  console.log(`[hearth] slots ${SLOTS.join(" ")} ${TZ} (grace ${GRACE_MIN}m, ${MAX_TRIES} tries)`);
  console.log(`[hearth] adults room ${ADULT_PASS ? "ON" : "OFF — set HEARTH_ADULT_PASSPHRASE"}`);
  console.log(`[hearth] push ${pushReady() ? "ready" : "OFF — run: npm run keys"}`);
  console.log(backupReady()
    ? `[hearth] backup ON — pushing to a remote after every wake`
    : `[hearth] backup LOCAL ONLY — history kept in ${backupDir()}, but memory still lives on one disk. Set BACKUP_GIT_REMOTE: see docs/backup.md`);
  console.log(`[hearth] calendar ${calendarReady() ? "ON" : "OFF — set CALENDAR_ICS_URLS"}`);
  const cw = credentialWarning();
  if (cw) console.error(`[hearth] !! ${cw}`);
  console.log(`[hearth] mail ${mailReady() ? `ON as ${process.env.GMAIL_USER}` : "OFF — set GMAIL_USER + GMAIL_APP_PASSWORD"}`);
});
