/**
 * Does Hearth actually work? — `npm run preflight`
 *
 * The brief's fourth rule is that a silent failure is the enemy, and that a
 * thin brief and a broken run look identical from outside. That ambiguity is
 * what this command exists to remove. It exercises every subsystem a check-in
 * depends on and prints a verdict, so "it's running" and "it works" stop being
 * the same sentence.
 *
 * It writes NOTHING — not memory, not state, not the daily log. Safe to run at
 * any hour, including while the service is up.
 *
 * `npm run preflight -- --brief` additionally renders a real check-in to the
 * terminal, without saving it anywhere. That is the only way to see what the
 * family would actually receive without putting a fabricated one in memory.
 */
import { upcoming, calendarReady } from "./calendar.js";
import { fetchNew, mailReady, credentialWarning } from "./mail.js";
import { pushReady } from "./push.js";
import { backupReady, backupDir } from "./backup.js";
import { loadState, loadBrief, todayET, LAYERS } from "./memory.js";
import { dueSlot, SLOTS, GRACE_MIN } from "./schedule.js";
import fs from "node:fs";
import path from "node:path";

const WANT_BRIEF = process.argv.includes("--brief");
let modelOk = false;
const problems = [];
const warnings = [];

const ok   = (l, d) => console.log(`  ok    ${l.padEnd(10)} ${d}`);
const warn = (l, d) => { warnings.push(`${l}: ${d}`); console.log(`  WARN  ${l.padEnd(10)} ${d}`); };
const bad  = (l, d) => { problems.push(`${l}: ${d}`); console.log(`  FAIL  ${l.padEnd(10)} ${d}`); };

console.log(`\nHearth preflight — ${new Date().toISOString()}\n`);

/* ---- memory: the part that cannot be rebuilt ---- */
const { brief, layers } = loadBrief();
if (!brief.trim()) bad("brief", "CLAUDE.md is empty or unreadable — Hearth has no instructions");
else ok("brief", `CLAUDE.md, ${brief.length} chars`);

const empty = LAYERS.filter((f) => !(layers[f] || "").trim());
if (empty.length === LAYERS.length) bad("memory", "every layer is empty — Hearth knows nothing about this family");
else if (empty.length) warn("memory", `empty: ${empty.join(", ")}`);
else ok("memory", `${LAYERS.length} layers, ${LAYERS.reduce((n, f) => n + layers[f].length, 0)} chars`);

/* ---- the model: without this every check-in is a FAILED report ---- */
try {
  const { default: Anthropic } = await import("@anthropic-ai/sdk");
  const r = await new Anthropic().messages.create({
    model: "claude-opus-5", max_tokens: 8,
    messages: [{ role: "user", content: "Reply with the single word: ok" }],
  });
  ok("model", `claude-opus-5 answered (${r.usage?.input_tokens ?? "?"} in)`);
  modelOk = true;
} catch (e) {
  // This exact failure produced today's "Wake 07:00 FAILED" report: the key was
  // simply absent from the service environment, and nothing checked until the
  // family was already owed a brief.
  bad("model", `${e?.status || ""} ${e?.message}`.trim().slice(0, 160));
}

/* ---- inputs ---- */
if (!calendarReady()) warn("calendar", "no CALENDAR_ICS_URLS — standalone mode, nothing is wrong");
else {
  const c = await upcoming(8);
  if (c.error) bad("calendar", c.error);
  else if (c.total === 0) {
    // Reachable and empty is worse than unreachable: it looks like a working
    // input right up until Hearth tells someone their week is free.
    warn("calendar", "reachable but EMPTY — no events at any date, so the family's schedule is not in it. Hearth is working from rhythms.md instead");
  } else ok("calendar", `${c.events.length} event(s) in the next 8 days, ${c.total} in the feed`);
}

const cw = credentialWarning();
if (cw) bad("mail", cw);
else if (!mailReady()) warn("mail", "no GMAIL_USER/GMAIL_APP_PASSWORD — standalone mode, nothing is wrong");
else {
  // Read from UID 0 without persisting: proves the mailbox opens and fetches,
  // and cannot move the service's own lastUid, because it never saves.
  const m = await fetchNew(0, 5);
  if (m.error) bad("mail", m.error);
  else ok("mail", `INBOX readable, ${m.messages.length} message(s) after junk filtering`);
}

/* ---- outputs ---- */
const st = loadState();
if (!pushReady()) bad("push", "VAPID keys missing — no phone can be notified");
else if (!st.subs?.length) warn("push", "ready, but nobody is subscribed — no phone will be notified");
else ok("push", `${st.subs.length} device(s) subscribed`);

// Every notification carries this as its tap target. If it is unset it falls
// back to localhost, and the notification opens nothing on a phone — a failure
// that is invisible from the server, because sending still succeeds.
const urlBase = process.env.HEARTH_URL || "";
if (!urlBase) bad("url", "HEARTH_URL is not set — every push notification would open http://localhost:8787, which is nothing on a phone");
else if (/localhost|127\.0\.0\.1/.test(urlBase)) bad("url", `HEARTH_URL is ${urlBase} — a phone cannot open that`);
else ok("url", urlBase);

if (!backupReady()) {
  warn("backup", `no off-machine copy — local history only, in ${backupDir()}. See docs/backup.md`);
} else if (st.backupError) {
  bad("backup", `remote set, but the last push FAILED: ${st.backupError}`);
} else {
  ok("backup", `remote configured, last success ${st.backupAt || "unknown"}`);
}

/* ---- the clock ---- */
const day = todayET();
const nowHm = new Date().toLocaleTimeString("en-GB",
  { timeZone: "America/New_York", hour: "2-digit", minute: "2-digit", hourCycle: "h23" });
const done = SLOTS.filter((s) => st.lastRun?.[s] === day);
const due = dueSlot(nowHm, st, day);
console.log(`  --    slots      ${nowHm} ET | done today: ${done.length ? done.join(", ") : "none"}`
  + ` | grace ${GRACE_MIN}m | due now: ${due ? `${due.slot} (${due.late}m late)` : "nothing"}`);

const failedToday = (st.reports || []).filter((r) => r.day === day && r.failed);
if (failedToday.length) {
  bad("wakes", `${failedToday.length} check-in(s) FAILED today: ${failedToday.map((r) => r.slot).join(", ")}`);
}

/* ---- the .env itself ---- */
const envPath = path.resolve(import.meta.dirname, "..", ".env");
if (fs.existsSync(envPath)) {
  // Duplicate keys are silently "last one wins", so a stale value can sit above
  // a good one and never be noticed until you go looking for why a fix did not
  // take. Name the keys; never print a value.
  const lines = fs.readFileSync(envPath, "utf8").split("\n");
  const keys = lines.map((l) => l.match(/^([A-Z][A-Z0-9_]*)=/)?.[1]).filter(Boolean);
  const dupes = [...new Set(keys.filter((k, i) => keys.indexOf(k) !== i))];
  if (dupes.length) warn("env", `defined more than once, last one wins: ${dupes.join(", ")}`);

  // A line with no `=` is skipped in silence by every .env parser there is.
  // HEARTH_URL sat like this for a day — the value was right there in the file,
  // correct, and completely ignored. Report the line number, never the content.
  const malformed = lines
    .map((l, i) => [i + 1, l.trim()])
    .filter(([, l]) => l && !l.startsWith("#") && !l.includes("="))
    .map(([n]) => n);
  if (malformed.length) {
    bad("env", `line(s) ${malformed.join(", ")} have no "=" and are being silently ignored`);
  }
}

/* ---- optional: what the family would actually get ---- */
if (WANT_BRIEF && modelOk) {
  const { wake } = await import("./brain.js");
  const { wakeContext } = await import("./context.js");
  const asked = process.argv[process.argv.indexOf("--brief") + 1];
  const use = SLOTS.includes(asked) ? asked : "07:00";
  // Same context builder the real wake uses, so this is a preview and not an
  // approximation. It still saves nothing.
  const { extra } = await wakeContext(use, st, 0);
  console.log(`\n--- ${use} check-in as it would be sent (NOT saved) ---\n`);
  console.log(await wake(use, extra));
  console.log(`\n--- end ---`);
}

console.log();
if (problems.length) {
  console.log(`RESULT: ${problems.length} FAILING — Hearth cannot do its job.`);
  for (const p of problems) console.log(`  - ${p}`);
  process.exit(1);
}
console.log(warnings.length
  ? `RESULT: working, with ${warnings.length} thing(s) worth knowing.`
  : "RESULT: everything Hearth needs is working.");
for (const w of warnings) console.log(`  - ${w}`);
