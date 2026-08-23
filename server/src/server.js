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
import { execSync } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { wake, answer, review, readPaper } from "./brain.js";
import { notify, notifyVerbose, pushReady } from "./push.js";
import { fetchNew, send as sendMail, mailReady, credentialWarning } from "./mail.js";
import { calendarReady, feeds, addFeed, removeFeed } from "./calendar.js";
import { backup, backupReady, backupDir } from "./backup.js";
import {
  loadState, saveState, appendDaily, writeLayer, writeTopic, searchMemory,
  todayET, loadBrief,
} from "./memory.js";
import { SLOTS, dueSlot, GRACE_MIN, MAX_TRIES } from "./schedule.js";
import { wakeContext } from "./context.js";
import * as loops from "./loops.js";
import { paperTrade } from "./markets.js";

const here = path.dirname(fileURLToPath(import.meta.url));
const PUB = path.resolve(here, "..", "public");
// Photographs stay on this disk and out of git. They are of children's paperwork.
const PHOTOS = path.resolve(here, "..", "data", "photos");
const PORT = Number(process.env.PORT || 8787);
const URL_BASE = process.env.HEARTH_URL || `http://localhost:${PORT}`;
const PASS = process.env.HEARTH_PASSPHRASE || "";
// A second passphrase is the whole boundary between the rooms. Without real
// accounts, a name picked from a list is a label, not a wall — a nine-year-old
// can tap "Jeffery". A separate secret the children do not have is a wall.
//
// It can come from .env, or from a file an adult sets through the app — because
// "open an SSH session from your phone and edit .env" turned out to be the
// single step this household could not do. .env wins if both exist.
const ADULT_FILE = path.resolve(here, "..", "data", "adult-pass");
const adultPass = () => process.env.HEARTH_ADULT_PASSPHRASE ||
  (fs.existsSync(ADULT_FILE) ? fs.readFileSync(ADULT_FILE, "utf8").trim() : "");
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

/** The slot whose moment we are in or most recently passed. */
function currentSlot() {
  const { hm } = nowET();
  let pick = SLOTS[SLOTS.length - 1];        // before 07:00, the night's close-out
  for (const sl of SLOTS) if (hm >= sl) pick = sl;
  return pick;
}

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
      url: URL_BASE, tag: `hearth-checkin-${slot}`,
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
    // The adults half was being written to the daily log and then nowhere —
    // the room filter was ready for adults reports that were never stored, so
    // the portfolio brief and every adults-only line simply vanished from the
    // app. Found by the first deep scan.
    if (out.adults) {
      after.reports.unshift({
        slot, at: new Date().toISOString(), day: todayET(),
        text: out.adults, room: "adults",
      });
    }
    after.reports = after.reports.slice(0, 60);
    if (!forced) {
      after.lastRun[slot] = todayET();
      if (after.wakeTries) delete after.wakeTries[slot];
    }
    saveState(after);

    // The close-out is followed by the only pass that rewrites memory.
    if (slot === "22:00") {
      const { summary, files, topics } = await review();
      for (const [f, content] of Object.entries(files)) writeLayer(f, content);
      for (const [f, content] of Object.entries(topics || {})) {
        try { writeTopic(f, String(content)); }
        catch (e) { appendDaily(`- review proposed a topic I refused: ${f} (${e.message})`); }
      }
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

/** Fire reminders whose moment has arrived. Never twice for the same loop —
 *  state remembers what it has already said, and unticking a loop clears it. */
async function remindTick(day, hm) {
  const now = `${day} ${hm}`;
  let due;
  try { due = loops.dueNow(now); } catch { return; }
  if (!due.length) return;
  const s = loadState();
  s.reminded = s.reminded || {};
  for (const l of due) {
    if (s.reminded[l.id]) continue;
    s.reminded[l.id] = now;
    // A reminder "for" someone goes only to that person's phones; anything
    // else goes to everyone — the loop list is shared, so this leaks nothing.
    let targets = l.for ? s.subs.filter((x) => x.who === l.for) : s.subs;
    // Subscriptions from before name-tagging carry no owner. A reminder that
    // fires to zero phones is a reminder that silently failed — fall back to
    // everyone (the To do list is shared; this leaks nothing) rather than
    // to nobody.
    if (l.for && !targets.length) targets = s.subs;
    appendDaily(`- reminder fired (${l.due}${l.for ? `, for ${l.for}` : ""}): ${l.title}`);
    if (targets.length) {
      try {
        await notify(targets, { title: "Hearth reminder", body: l.title, url: URL_BASE, tag: `hearth-remind-${l.id}` });
      } catch { /* logged above; the To do tab still shows it */ }
    }
  }
  // Unticked-then-reticked loops aside, drop bookkeeping for loops that no
  // longer exist so state does not grow forever.
  const live = new Set(loops.list().map((x) => x.id));
  for (const id of Object.keys(s.reminded)) if (!live.has(id)) delete s.reminded[id];
  saveState(s);
}

function tick() {
  const { day, hm } = nowET();
  remindTick(day, hm).catch(() => {});
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

const body = (req, limit = 1e6) => new Promise((ok, no) => {
  let d = ""; req.on("data", (c) => { d += c; if (d.length > limit) req.destroy(); });
  req.on("end", () => { try { ok(d ? JSON.parse(d) : {}); } catch (e) { no(e); } });
});

const authed = (b) => !PASS || b.pass === PASS;

// Which room a post lands in. "me" is anyone's to use; "adults" needs the
// second secret; everything else is family.
const roomFor = (b, who) => {
  if (b.room === "me") return { room: "me", owner: who };
  if (b.room === "adults" && adultPass() && b.apass === adultPass()) return { room: "adults" };
  return { room: "family" };
};

// Funnel publishes this to the open internet — that is the point, so the family
// needs nothing installed. It also means the passphrase is the only door. Reads
// must be gated too: a check-in naming the children, the schools and the day's
// movements is not less sensitive than the ability to post.
const given = (u, req) => u.searchParams.get("pass") || req.headers["x-hearth-pass"] || "";
const adultGiven = (u, req) => u.searchParams.get("apass") || req.headers["x-hearth-apass"] || "";
const authedRead = (u, req) => !PASS || given(u, req) === PASS || adultGiven(u, req) === adultPass() && !!adultPass();
// Adults-only content is served only to a request carrying the second secret.
const isAdult = (u, req) => Boolean(adultPass()) && adultGiven(u, req) === adultPass();

// Who a message is from is decided here, from the device it came from — never
// from what the client says. A name chosen from a list is a costume: anyone can
// wear anyone's. Binding it to the device is what makes "he said, she said"
// answerable, and unclaiming a device needs the adult passphrase.
function nameFor(state, device) {
  return (state.devices || {})[String(device || "")] || null;
}

/* ---------- the build stamp ---------- */
// "Is the push live?" should be a glance at the page, not a question to a
// terminal. `running` is the commit this process booted on; `repo` is what the
// last pull brought in — when they differ, an update has landed and a restart
// is pending (or was rightly skipped for a no-code commit).
const gitInfo = () => {
  try {
    const [rev, ts] = execSync("git log -1 --format='%h %ct'", { cwd: ROOT })
      .toString().trim().replace(/'/g, "").split(" ");
    return { rev, at: new Date(Number(ts) * 1000).toISOString() };
  } catch { return { rev: "unknown", at: null }; }
};
import { ROOT } from "./memory.js";
const BOOT = { ...gitInfo(), started: new Date().toISOString() };
let repoCache = { at: 0, info: BOOT };
const repoNow = () => {
  if (Date.now() - repoCache.at > 60e3) repoCache = { at: Date.now(), info: gitInfo() };
  return repoCache.info;
};

// When the updater will next look. The timer drifts on purpose (15min after
// the last run, plus jitter), so the only honest source is systemd itself.
let nextCache = { at: 0, when: null };
const nextUpdateCheck = () => {
  if (Date.now() - nextCache.at > 60e3) {
    let when = null;
    try {
      const raw = execSync("systemctl show hearth-update.timer -p NextElapseUSecRealtime --value",
        { timeout: 2000 }).toString().trim();
      const d = new Date(raw.replace(/^\w+ /, ""));
      if (raw && !Number.isNaN(d.getTime())) when = d.toISOString();
    } catch { /* not on systemd (dev box) — the strip just omits it */ }
    nextCache = { at: Date.now(), when };
  }
  return nextCache.when;
};

const server = http.createServer(async (req, res) => {
  const u = new URL(req.url, URL_BASE);

  try {
    if (req.method === "GET" && u.pathname === "/api/state") {
      if (!authedRead(u, req)) return send(res, 401, { error: "passphrase required" });
      const s = loadState();
      const { brief } = loadBrief();
      const adult = isAdult(u, req);
      const me = nameFor(s, u.searchParams.get("device") || req.headers["x-hearth-device"] || "");
      // Filtered server-side, never client-side: a hidden div is not a boundary.
      // A private message belongs to exactly one person; not even an adult sees
      // someone else's scratchpad.
      const visible = (x) => x.room === "me"
        ? Boolean(me) && x.owner === me
        : adult || (x.room || "family") === "family";
      return send(res, 200, {
        adult,
        adultRoomExists: Boolean(adultPass()),
        reports: s.reports.filter((r) => !r.internal).filter(visible).slice(0, 12),
        messages: s.messages.filter(visible).slice(-80),
        // Only the roster table's rows — not every bold word in the brief.
        household: [...brief.matchAll(/^\|\s*\*\*([\w'-]+)\*\*\s*\|/gm)].map((m) => m[1]),
        slots: SLOTS, now: nowET(), push: pushReady(),
        nextSlot: currentSlot(),
        me,
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
        // Open loops are the list the family actually acts on, so they are a
        // first-class view rather than something only the check-ins mention.
        loops: loops.list(),
        // Labels only. An ICS secret address is a password and never leaves here.
        calendars: feeds(),
        build: { running: BOOT.rev, builtAt: BOOT.at, since: BOOT.started, repo: repoNow().rev,
                 nextCheck: nextUpdateCheck() },
        needsPass: Boolean(PASS),
      });
    }

    if (req.method === "POST" && u.pathname === "/api/message") {
      const b = await body(req);
      if (!authed(b)) return send(res, 401, { error: "wrong passphrase" });
      const s0 = loadState();
      const who = nameFor(s0, b.device);
      if (!who) return send(res, 409, { error: "this device has not said who it belongs to" });
      const text = String(b.text || "").trim().slice(0, 4000);
      if (!text) return send(res, 400, { error: "empty" });
      // Posting to the adults room requires the adult secret, not a claim.
      const { room, owner } = roomFor(b, who);

      const s = loadState();
      s.messages.push({ id: `m${Date.now()}`, who, text, room, ...(owner ? { owner } : {}), at: new Date().toISOString() });
      saveState(s);
      // A scratchpad line is marked so the 22:00 review knows it must never
      // surface anywhere that person would not be alone.
      appendDaily(room === "me" ? `- [private ${who}] ${text}` : `- ${who}: ${text}`);

      // Answer in the background; the poster should not wait on a model call.
      (async () => {
        try {
          const recent = loadState().messages
            .filter((m) => (m.room || "family") === room && (room !== "me" || m.owner === who))
            .slice(-8);
          const r = await answer(who, text, recent, room);
          // Anything the family asked Hearth to remember becomes a tracked
          // loop, not a sentence that scrolls away. Never from a scratchpad —
          // the To do list is shared and nothing leaves a private room.
          const day = todayET();
          const opened = [];
          for (const l of r.loops || []) {
            // due/for were never passed on this path — the coffee reminder's
            // real killer: the model could send a perfect time and storage
            // dropped it. The tail below speaks from what was STORED, never
            // from the model's prose, which had promised a 06:38 push over a
            // loop with no time at all.
            const a = loops.add({ section: l.section, title: l.title, detail: l.detail, day, due: l.due, for: l.for });
            if (a.ok && !a.duplicate) opened.push(
              a.due ? `${a.title} — push at ${a.due}${l.for ? ` for ${l.for}` : ""}` :
              a.dueDropped ? `${a.title} — the time did not parse, NO push is set` : a.title);
          }
          // Paper trades: adults room only (brain drops the field elsewhere).
          // Executed here so the ledger and the refusal lines are the server's,
          // not the model's word for what happened.
          const executed = [];
          for (const t of r.trades || []) {
            const out = await paperTrade({ ...t, who });
            executed.push(out.line);
            appendDaily(`- [adults] paper trade by ${who}: ${out.line}`);
          }
          const reply = r.text
            + (executed.length ? `\n\n${executed.join("\n")}` : "")
            + (opened.length ? `\n\nOn the To do list now: ${opened.join("; ")}.` : "");
          const st = loadState();
          st.messages.push({ id: `h${Date.now()}`, who: "Hearth", text: reply, room, ...(owner ? { owner } : {}), at: new Date().toISOString() });

          // The two doors out of a scratchpad, and the only two. A relay is the
          // person's own words posted under their own name because they asked.
          // An alert goes to the parents alone — never the family room — and
          // Hearth has already told the person it is doing it.
          if (room === "me" && r.relay) {
            st.messages.push({ id: `m${Date.now() + 1}`, who, text: r.relay, room: "family", at: new Date().toISOString() });
            appendDaily(`- ${who} (posted from their scratchpad): ${r.relay}`);
          }
          if (room === "me" && r.alert) {
            st.messages.push({
              id: `h${Date.now() + 2}`, who: "Hearth",
              text: `A safety note from ${who}'s private chat — shared with you two only, and ${who} knows I am sharing it:\n\n${r.alert}`,
              room: "adults", at: new Date().toISOString(),
            });
            appendDaily(`- [safety] Alerted Jeffery and Suzan about ${who}'s private chat. ${who} was told.`);
            // Push only to devices claimed by a parent. A phone that has not
            // said who it belongs to gets nothing — a safety note on a child's
            // lock screen would be the exact leak the rooms exist to prevent.
            const parents = st.subs.filter((x) => x.who === "Jeffery" || x.who === "Suzan");
            if (parents.length) {
              try { await notify(parents, { title: "Hearth", body: `A safety note about ${who} is in the Adults thread.`, url: URL_BASE, tag: "hearth-adults" }); }
              catch { /* the message is still in the thread */ }
            }
          }
          saveState(st);
          appendDaily(room === "me" ? `- [private ${who}] Hearth: ${r.text}` : `- Hearth: ${reply}`);
        } catch (e) {
          const st = loadState();
          st.messages.push({
            id: `h${Date.now()}`, who: "Hearth", failed: true,
            text: `I could not answer that: ${e?.message || e}`, room, ...(owner ? { owner } : {}), at: new Date().toISOString(),
          });
          saveState(st);
        }
      })();
      return send(res, 200, { ok: true });
    }

    // A photograph of a piece of school paper. This is the input that closes the
    // household's real failure: a form comes home in a bag and surfaces the
    // morning it is due. Aiden holds it up to a phone and it stops being lost.
    if (req.method === "POST" && u.pathname === "/api/photo") {
      const b = await body(req, 12e6);
      if (!authed(b)) return send(res, 401, { error: "wrong passphrase" });
      const s0 = loadState();
      const who = nameFor(s0, b.device);
      if (!who) return send(res, 409, { error: "this device has not said who it belongs to" });
      const m = String(b.data || "").match(/^data:(image\/(?:jpeg|png|webp|gif));base64,([\s\S]+)$/);
      if (!m) return send(res, 400, { error: "expected a jpeg, png, webp or gif" });
      const [, mediaType, b64] = m;
      const bytes = Buffer.from(b64, "base64");
      if (bytes.length > 8e6) return send(res, 413, { error: "that photo is too big" });

      const id = `p${Date.now()}`;
      fs.mkdirSync(PHOTOS, { recursive: true });
      fs.writeFileSync(path.join(PHOTOS, `${id}.${mediaType.split("/")[1]}`), bytes);

      const note = String(b.note || "").trim().slice(0, 500);
      const { room, owner } = roomFor(b, who);
      const s = loadState();
      s.messages.push({ id: `m${Date.now()}`, who, text: note || "Sent a photo.", photo: id,
                        room, ...(owner ? { owner } : {}), at: new Date().toISOString() });
      saveState(s);
      appendDaily(room === "me"
        ? `- [private ${who}] photographed a document${note ? `: ${note}` : ""} (${id})`
        : `- ${who} photographed a document${note ? `: ${note}` : ""} (${id})`);

      // Read it in the background. Nobody holds a phone up waiting for a model.
      (async () => {
        try {
          const r = await readPaper(who, [{ media_type: mediaType, data: b64 }], note);
          // A photograph can be about money or about a child's behaviour, so the
          // reading decides the room — but it can only ever go MORE private than
          // where it was posted, never less. Posted in a scratchpad, it stays
          // in that scratchpad.
          const out = room === "me" ? "me" : room === "adults" ? "adults" : r.room;
          const day = todayET();
          const opened = [];
          for (const l of r.loops || []) {
            const a = loops.add({ section: l.section, title: l.title, detail: l.detail, day, due: l.due, for: l.for });
            if (a.ok && !a.duplicate) opened.push(a.due ? `${a.title} — push at ${a.due}` : a.title);
          }
          const tail = opened.length
            ? `\n\nOpen loop${opened.length > 1 ? "s" : ""} added: ${opened.join("; ")}.`
            : "";
          const st = loadState();
          st.messages.push({ id: `h${Date.now()}`, who: "Hearth", text: r.text + tail,
                             room: out, ...(owner ? { owner } : {}), at: new Date().toISOString() });
          saveState(st);
          appendDaily(room === "me"
            ? `- [private ${who}] Hearth read ${id}: ${r.text}${tail}`
            : `- Hearth read ${id}: ${r.text}${tail}`);
        } catch (e) {
          const st = loadState();
          st.messages.push({ id: `h${Date.now()}`, who: "Hearth", failed: true,
            text: `I could not read that photo: ${e?.message || e}. The picture is saved — try again, or just tell me what it says.`,
            room, ...(owner ? { owner } : {}), at: new Date().toISOString() });
          saveState(st);
        }
      })();
      return send(res, 200, { ok: true, id });
    }

    // The photo back again, so the channel shows what was sent. Gated like
    // everything else: the passphrase is the only door.
    if (req.method === "GET" && u.pathname === "/api/photo") {
      if (!authedRead(u, req)) return send(res, 401, { error: "passphrase required" });
      const id = String(u.searchParams.get("id") || "");
      if (!/^p\d+$/.test(id)) return send(res, 400, { error: "bad id" });
      const hit = (fs.existsSync(PHOTOS) ? fs.readdirSync(PHOTOS) : [])
        .find((f) => f.startsWith(`${id}.`));
      if (!hit) return send(res, 404, { error: "no such photo" });
      const ext = path.extname(hit).slice(1);
      res.writeHead(200, { "content-type": `image/${ext === "jpg" ? "jpeg" : ext}`,
                           "cache-control": "private, max-age=86400" });
      return res.end(fs.readFileSync(path.join(PHOTOS, hit)));
    }

    // Ticking a loop off. Attribution comes from the device, same as a message —
    // "who said it was done" is exactly the kind of thing that gets disputed.
    if (req.method === "POST" && u.pathname === "/api/loop") {
      const b = await body(req);
      if (!authed(b)) return send(res, 401, { error: "wrong passphrase" });
      const who = nameFor(loadState(), b.device);
      if (!who) return send(res, 409, { error: "this device has not said who it belongs to" });
      const r = loops.setDone(String(b.id || ""), Boolean(b.done), who, todayET());
      if (!r.ok) return send(res, 400, r);
      if (!r.unchanged) {
        appendDaily(`- ${who} marked "${r.title}" ${b.done ? "done" : "not done after all"}`);
        if (!b.done) {
          // Brought back to life: its reminder deserves to fire again.
          const st = loadState();
          if (st.reminded && st.reminded[String(b.id)]) { delete st.reminded[String(b.id)]; saveState(st); }
        }
      }
      return send(res, 200, r);
    }

    // Setting the adults passphrase without opening a terminal. First set needs
    // only the family passphrase; changing it needs the current one. If .env
    // carries a value, that wins and this endpoint refuses — one source of truth.
    if (req.method === "POST" && u.pathname === "/api/adult-pass") {
      const b = await body(req);
      if (!authed(b)) return send(res, 401, { error: "wrong passphrase" });
      const who = nameFor(loadState(), b.device);
      if (!who) return send(res, 409, { error: "this device has not said who it belongs to" });
      if (process.env.HEARTH_ADULT_PASSPHRASE)
        return send(res, 400, { error: "the adults passphrase is set in .env; change it there" });
      const cur = adultPass();
      if (cur && b.apass !== cur)
        return send(res, 403, { error: "already set; changing it needs the current adults passphrase" });
      const v = String(b.value || "").trim();
      if (v.length < 4) return send(res, 400, { error: "too short" });
      if (v === PASS) return send(res, 400, { error: "it must be different from the family passphrase — the same word is one room, not two" });
      fs.mkdirSync(path.dirname(ADULT_FILE), { recursive: true });
      fs.writeFileSync(ADULT_FILE, v + "\n", { mode: 0o600 });
      appendDaily(`- ${who} ${cur ? "changed" : "set"} the adults passphrase`);
      return send(res, 200, { ok: true });
    }

    // Connecting a calendar without opening a terminal.
    if (req.method === "POST" && u.pathname === "/api/calendar") {
      const b = await body(req);
      if (!adultPass() || b.apass !== adultPass()) return send(res, 403, { error: "adults only" });
      const who = nameFor(loadState(), b.device) || "an adult";
      if (b.remove) {
        const r = removeFeed(String(b.remove));
        appendDaily(`- ${who} disconnected a calendar feed`);
        return send(res, 200, r);
      }
      const r = addFeed(b.url, who, b.label || "");
      if (!r.ok) return send(res, 400, r);
      if (!r.duplicate) appendDaily(`- ${who} connected a calendar feed (${b.label || "unlabelled"})`);
      return send(res, 200, r);
    }

    // Retrieval without a model call: grep the memory, show the source. The
    // answer to "what did the coach's email say" should not cost a wake.
    if (req.method === "GET" && u.pathname === "/api/search") {
      if (!authedRead(u, req)) return send(res, 401, { error: "passphrase required" });
      const hits = searchMemory(u.searchParams.get("q") || "");
      // The adults boundary holds here too: money and adults-room material live
      // in files a child's passphrase can still search. Filter by source.
      const adult = isAdult(u, req);
      const filtered = adult ? hits : hits.filter((h) =>
        h.file !== "portfolio.md" && !/\[adults\]|\[private /.test(h.text));
      return send(res, 200, { hits: filtered.slice(0, 20) });
    }

    if (req.method === "POST" && u.pathname === "/api/claim") {
      const b = await body(req);
      if (!authed(b)) return send(res, 401, { error: "wrong passphrase" });
      const device = String(b.device || "").slice(0, 64);
      const who = String(b.who || "").slice(0, 40);
      if (!device || !who) return send(res, 400, { error: "device and who required" });
      const s = loadState();
      s.devices = s.devices || {};
      const existing = s.devices[device];
      // Reassigning a device is how attribution gets laundered. Adults only.
      if (existing && existing !== who && !(adultPass() && b.apass === adultPass())) {
        return send(res, 403, { error: `this device is already ${existing}; an adult must change it` });
      }
      s.devices[device] = who;
      saveState(s);
      appendDaily(`- device claimed as ${who}`);
      return send(res, 200, { ok: true, who });
    }

    if (req.method === "POST" && u.pathname === "/api/clear-channel") {
      const b = await body(req);
      if (!adultPass() || b.apass !== adultPass()) return send(res, 403, { error: "adults only" });
      const s = loadState();
      const n = s.messages.length;
      s.messages = [];
      saveState(s);
      // Memory is untouched on purpose: the thread is scratch, memory is the product.
      appendDaily(`- channel cleared by an adult (${n} messages). Memory untouched.`);
      return send(res, 200, { ok: true, cleared: n });
    }

    // The only real test of push is a push. The chip, once subscribed, sends
    // one through the full pipeline to THIS phone and reports what the push
    // service actually said — delivery stops being a matter of faith.
    if (req.method === "POST" && u.pathname === "/api/test-push") {
      const b = await body(req);
      if (!authed(b)) return send(res, 401, { error: "wrong passphrase" });
      if (!pushReady()) return send(res, 400, { error: "push is not configured on the server" });
      const s = loadState();
      const mine = s.subs.filter((x) => x.endpoint === String(b.endpoint || ""));
      if (!mine.length) return send(res, 404, { error: "this phone has no registered subscription here — tap Notify me first" });
      const results = await notifyVerbose(mine, {
        title: "Hearth test", body: "Push delivery works on this phone.", url: URL_BASE,
        tag: "hearth-test",
      });
      appendDaily(`- push test by ${nameFor(s, b.device) || "someone"}: ${results.map((r) => r.ok ? `accepted by ${r.host}` : `FAILED ${r.status || r.error}`).join("; ")}`);
      return send(res, 200, { results });
    }

    if (req.method === "POST" && u.pathname === "/api/subscribe") {
      const b = await body(req);
      if (!authed(b)) return send(res, 401, { error: "wrong passphrase" });
      const s = loadState();
      // Remember whose phone this subscription is, so anything sensitive can be
      // pushed to the parents' devices and only theirs. Re-subscribing updates
      // the name — claims can change.
      const subWho = nameFor(s, b.device) || null;
      const hit = s.subs.find((x) => x.endpoint === b.sub?.endpoint);
      if (hit) hit.who = subWho;
      else if (b.sub) s.subs.push({ ...b.sub, who: subWho });
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
      // One button. Which check-in it is depends on the time of day, which the
      // server already knows — asking a person to choose between four is asking
      // them to understand the schedule to use the app.
      const slot = SLOTS.includes(b.slot) ? b.slot : currentSlot();
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
      // Icons may cache for a day; everything that carries behaviour must
      // revalidate every load, or a fix exists on the server and nobody has it.
      const cache = ext === ".png" || ext === ".ico" ? "public, max-age=86400" : "no-cache";
      res.writeHead(200, { "content-type": type, "cache-control": cache });
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
  console.log(`[hearth] adults room ${adultPass() ? "ON" : "OFF — an adult sets it in the app, or HEARTH_ADULT_PASSPHRASE in .env"}`);
  console.log(`[hearth] push ${pushReady() ? "ready" : "OFF — run: npm run keys"}`);
  console.log(backupReady()
    ? `[hearth] backup ON — pushing to a remote after every wake`
    : `[hearth] backup LOCAL ONLY — history kept in ${backupDir()}, but memory still lives on one disk. Set BACKUP_GIT_REMOTE: see docs/backup.md`);
  console.log(`[hearth] calendar ${calendarReady() ? "ON" : "OFF — set CALENDAR_ICS_URLS"}`);
  const cw = credentialWarning();
  if (cw) console.error(`[hearth] !! ${cw}`);
  console.log(`[hearth] mail ${mailReady() ? `ON as ${process.env.GMAIL_USER}` : "OFF — set GMAIL_USER + GMAIL_APP_PASSWORD"}`);
});
