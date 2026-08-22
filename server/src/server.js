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
import {
  loadState, saveState, appendDaily, writeLayer, todayET, loadBrief,
} from "./memory.js";

const here = path.dirname(fileURLToPath(import.meta.url));
const PUB = path.resolve(here, "..", "public");
const PORT = Number(process.env.PORT || 8787);
const URL_BASE = process.env.HEARTH_URL || `http://localhost:${PORT}`;
const PASS = process.env.HEARTH_PASSPHRASE || "";
const TZ = "America/New_York";
const SLOTS = ["07:00", "12:00", "17:00", "22:00"];

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

let running = false;

async function runSlot(slot, { forced = false } = {}) {
  if (running) return null;
  running = true;
  const s = loadState();
  try {
    const text = await wake(slot);
    s.reports.unshift({ slot, at: new Date().toISOString(), day: todayET(), text });
    s.reports = s.reports.slice(0, 60);
    if (!forced) s.lastRun[slot] = todayET();

    appendDaily(`## ${slot} check-in\n\n${text}`);

    s.subs = await notify(s.subs, {
      title: `Hearth — ${slot}`,
      body: text.split("\n").filter(Boolean)[0]?.slice(0, 140) || "Your check-in is ready.",
      url: URL_BASE,
    });
    saveState(s);

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
      failed: true,
    });
    saveState(st);
    return null;
  } finally {
    running = false;
  }
}

function tick() {
  const { day, hm } = nowET();
  const s = loadState();
  for (const slot of SLOTS) {
    if (hm === slot && s.lastRun[slot] !== day) {
      console.log(`[hearth] waking for ${slot}`);
      runSlot(slot);
    }
  }
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

const server = http.createServer(async (req, res) => {
  const u = new URL(req.url, URL_BASE);

  try {
    if (req.method === "GET" && u.pathname === "/api/state") {
      const s = loadState();
      const { brief } = loadBrief();
      return send(res, 200, {
        reports: s.reports.filter((r) => !r.internal).slice(0, 12),
        messages: s.messages.slice(-80),
        // Only the roster table's rows — not every bold word in the brief.
        household: [...brief.matchAll(/^\|\s*\*\*([\w'-]+)\*\*\s*\|/gm)].map((m) => m[1]),
        slots: SLOTS, now: nowET(), push: pushReady(),
        vapid: process.env.VAPID_PUBLIC || null,
        needsPass: Boolean(PASS),
      });
    }

    if (req.method === "POST" && u.pathname === "/api/message") {
      const b = await body(req);
      if (!authed(b)) return send(res, 401, { error: "wrong passphrase" });
      const who = String(b.who || "someone").slice(0, 40);
      const text = String(b.text || "").trim().slice(0, 4000);
      if (!text) return send(res, 400, { error: "empty" });

      const s = loadState();
      s.messages.push({ id: `m${Date.now()}`, who, text, at: new Date().toISOString() });
      saveState(s);
      appendDaily(`- ${who}: ${text}`);

      // Answer in the background; the poster should not wait on a model call.
      (async () => {
        try {
          const recent = loadState().messages.slice(-8);
          const reply = await answer(who, text, recent);
          const st = loadState();
          st.messages.push({ id: `h${Date.now()}`, who: "Hearth", text: reply, at: new Date().toISOString() });
          saveState(st);
          appendDaily(`- Hearth: ${reply}`);
        } catch (e) {
          const st = loadState();
          st.messages.push({
            id: `h${Date.now()}`, who: "Hearth", failed: true,
            text: `I could not answer that: ${e?.message || e}`, at: new Date().toISOString(),
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
      const type = { ".html": "text/html", ".js": "text/javascript",
        ".json": "application/json", ".css": "text/css" }[ext] || "application/octet-stream";
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
  console.log(`[hearth] slots ${SLOTS.join(" ")} ${TZ}`);
  console.log(`[hearth] push ${pushReady() ? "ready" : "OFF — run: npm run keys"}`);
});
