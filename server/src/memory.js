/**
 * Memory lives in the repo's own markdown files — the same ones a human can
 * read, diff and correct. Nothing here is a database. That is deliberate: the
 * family must be able to read every word held about them.
 */
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const here = path.dirname(fileURLToPath(import.meta.url));
export const ROOT = path.resolve(here, "..", "..");
export const MEM = path.join(ROOT, "memory");
export const DATA = path.resolve(here, "..", "data");

/** Long-lived layers, loaded on every wake. Daily logs are NOT read here — they
 *  are written freely and only re-read by the 22:00 review, which is what keeps
 *  the working set flat no matter how many years accumulate. */
export const LAYERS = [
  "facts.md",
  "rhythms.md",
  "open-loops.md",
  "corrections.md",
  "misses.md",
  "reference.md",
];

const read = (p) => (fs.existsSync(p) ? fs.readFileSync(p, "utf8") : "");

export function loadBrief() {
  const brief = read(path.join(ROOT, "CLAUDE.md"));
  const layers = Object.fromEntries(
    LAYERS.map((f) => [f, read(path.join(MEM, f))]),
  );
  return { brief, layers };
}

export const todayET = (tz = "America/New_York") => {
  const p = Object.fromEntries(
    new Intl.DateTimeFormat("en-CA", {
      timeZone: tz, year: "numeric", month: "2-digit", day: "2-digit",
    }).formatToParts(new Date()).map((x) => [x.type, x.value]),
  );
  return `${p.year}-${p.month}-${p.day}`;
};

export function appendDaily(text, day = todayET()) {
  fs.mkdirSync(path.join(MEM, "daily"), { recursive: true });
  const p = path.join(MEM, "daily", `${day}.md`);
  if (!fs.existsSync(p)) fs.writeFileSync(p, `# ${day}\n`);
  fs.appendFileSync(p, `\n${text}\n`);
}

export const readDaily = (day = todayET()) =>
  read(path.join(MEM, "daily", `${day}.md`));

/** Rewrite a long-lived layer. Only the 22:00 review calls this. */
export function writeLayer(file, content) {
  if (!LAYERS.includes(file)) throw new Error(`refusing to write ${file}`);
  fs.writeFileSync(path.join(MEM, file), content.endsWith("\n") ? content : content + "\n");
}

/* ---- channel state: messages, reports, push subscriptions ---- */
const STATE = path.join(DATA, "state.json");
const EMPTY = { messages: [], reports: [], subs: [], lastRun: {} };

export function loadState() {
  fs.mkdirSync(DATA, { recursive: true });
  if (!fs.existsSync(STATE)) return structuredClone(EMPTY);
  try { return { ...structuredClone(EMPTY), ...JSON.parse(fs.readFileSync(STATE, "utf8")) }; }
  catch { return structuredClone(EMPTY); }
}

export function saveState(s) {
  fs.mkdirSync(DATA, { recursive: true });
  const tmp = STATE + ".tmp";
  fs.writeFileSync(tmp, JSON.stringify(s, null, 1));
  fs.renameSync(tmp, STATE);   // atomic — a crash mid-write never truncates memory
}
