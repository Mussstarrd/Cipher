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

/* ---------- topics: knowledge filed by subject ---------- */
// The daily log is a stream; a stream is where knowledge goes to be forgotten.
// Topics are where it goes to be FOUND: one file per subject, curated by the
// 22:00 review, so "everything about soccer" is one open, not an archaeology
// dig through a season of daily logs.
export const TOPICS = path.join(MEM, "topics");
const TOPIC_NAME = /^[a-z0-9][a-z0-9-]{0,48}\.md$/;

export function listTopics() {
  if (!fs.existsSync(TOPICS)) return [];
  return fs.readdirSync(TOPICS).filter((f) => TOPIC_NAME.test(f)).sort().map((f) => {
    const text = read(path.join(TOPICS, f));
    // First line is the title; an "aliases:" line teaches retrieval its other names.
    const title = (text.match(/^#\s*(.+)$/m) || [, f.replace(/\.md$/, "")])[1];
    const aliases = (text.match(/^aliases:\s*(.+)$/mi) || [, ""])[1]
      .split(",").map((a) => a.trim().toLowerCase()).filter(Boolean);
    return { file: f, title, aliases, text };
  });
}

export function writeTopic(file, content) {
  if (!TOPIC_NAME.test(file)) throw new Error(`refusing topic name ${file}`);
  fs.mkdirSync(TOPICS, { recursive: true });
  fs.writeFileSync(path.join(TOPICS, file), content.endsWith("\n") ? content : content + "\n");
}

/** Topics whose name or aliases appear in the text — cheap, no model call. */
export function matchTopics(text, max = 3) {
  const hay = String(text).toLowerCase();
  return listTopics().filter((t) => {
    const names = [t.file.replace(/\.md$/, "").replace(/-/g, " "), t.title.toLowerCase(), ...t.aliases];
    return names.some((n) => n && hay.includes(n));
  }).slice(0, max);
}

/* ---------- search: retrieval without a model call ---------- */
/** Case-insensitive search across every memory file, snippets with sources. */
export function searchMemory(q, cap = 30) {
  const needle = String(q || "").trim().toLowerCase();
  if (needle.length < 2) return [];
  const words = needle.split(/\s+/).filter((w) => w.length >= 2);
  const files = [
    ...LAYERS.map((f) => ({ label: f, p: path.join(MEM, f) })),
    ...(fs.existsSync(TOPICS) ? fs.readdirSync(TOPICS).filter((f) => f.endsWith(".md"))
        .map((f) => ({ label: `topics/${f}`, p: path.join(TOPICS, f) })) : []),
    ...(fs.existsSync(path.join(MEM, "daily")) ? fs.readdirSync(path.join(MEM, "daily"))
        .filter((f) => f.endsWith(".md")).sort().reverse().slice(0, 30)
        .map((f) => ({ label: `daily/${f}`, p: path.join(MEM, "daily", f) })) : []),
    { label: "portfolio.md", p: path.join(MEM, "portfolio.md") },
  ];
  const out = [];
  for (const { label, p } of files) {
    if (out.length >= cap) break;
    const lines = read(p).split("\n");
    for (let i = 0; i < lines.length && out.length < cap; i++) {
      const low = lines[i].toLowerCase();
      if (words.every((w) => low.includes(w))) {
        out.push({ file: label, line: i + 1, text: lines[i].trim().slice(0, 240) });
      }
    }
  }
  return out;
}

/* ---- channel state: messages, reports, push subscriptions ---- */
const STATE = path.join(DATA, "state.json");
const EMPTY = { messages: [], reports: [], subs: [], lastRun: {}, mail: [], lastUid: 0 };

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
