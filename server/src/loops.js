/**
 * Open loops, as things you can tick off.
 *
 * memory/open-loops.md is prose on purpose — a human has to be able to read it
 * and a model has to be able to rewrite it at 22:00. So this does not impose a
 * data format on the file. It finds the top-level "- [opened DATE] **Title**"
 * bullets, treats everything under one until the next as its body, and can move
 * a whole block between the section it lives in and a "## Done" section at the
 * bottom.
 *
 * Ticking is reversible. The section a loop came from is written into the done
 * marker so unticking puts it back where it was rather than somewhere plausible.
 */
import crypto from "node:crypto";
import fs from "node:fs";
import path from "node:path";
import { MEM } from "./memory.js";

const FILE = path.join(MEM, "open-loops.md");
const OPENED = /^- \[opened (\d{4}-\d{2}-\d{2})\]\s*(.*)$/;
const DONE = /^- \[done (\d{4}-\d{2}-\d{2}) by ([^\]·]+?)(?: · was: ([^·\]]+))?(?: · opened (\d{4}-\d{2}-\d{2}))?\]\s*(.*)$/;
const HEAD = /^##\s+(.+?)\s*$/;

const read = () => (fs.existsSync(FILE) ? fs.readFileSync(FILE, "utf8") : "");

/** Stable across rewrites of the body, because it hashes the title only. */
const idOf = (title) =>
  crypto.createHash("sha1").update(title.replace(/\W+/g, " ").trim().toLowerCase()).digest("hex").slice(0, 10);

/** Strip markdown emphasis and trailing punctuation for a one-line label. */
function label(rest) {
  const m = rest.match(/\*\*(.+?)\*\*/);
  const t = (m ? m[1] : rest).replace(/[*_`]/g, "").trim();
  return t.replace(/\s*[.,;:]\s*$/, "");
}

/** Split the file into blocks, remembering which "## section" each sits under. */
function parse() {
  const lines = read().split("\n");
  const blocks = [];
  let section = "", cur = null;
  const close = () => { if (cur) { blocks.push(cur); cur = null; } };

  for (const line of lines) {
    const h = line.match(HEAD);
    if (h) { close(); section = h[1]; continue; }
    const o = line.match(OPENED), d = line.match(DONE);
    if (o || d) {
      close();
      const rest = o ? o[2] : d[5];
      cur = {
        done: Boolean(d),
        // The date it was opened must survive a tick, or unticking silently
        // rewrites history and every "how long has this been sitting" is wrong.
        opened: o ? o[1] : (d[4] || d[1]),
        by: d ? d[2].trim() : null,
        from: d ? (d[3] || "").trim() : section,
        section,
        title: label(rest),
        lines: [line],
      };
      continue;
    }
    if (cur) cur.lines.push(line);
  }
  close();
  return blocks.map((b) => ({ ...b, id: idOf(b.title) }));
}

/** What the app shows. Bodies are trimmed of blank tails, not of content. */
export function list() {
  return parse().map((b) => ({
    id: b.id,
    title: b.title,
    section: b.done ? b.from || "Done" : b.section,
    done: b.done,
    by: b.by,
    opened: b.opened,
    detail: b.lines.slice(1).join("\n").replace(/\s+$/, "").replace(/^\s*\n/, ""),
  }));
}

/**
 * Tick or untick. Rewrites the file by hand rather than regenerating it, so
 * every word a human or the 22:00 review wrote survives untouched.
 */
export function setDone(id, done, who, day) {
  const src = read();
  if (!src) return { ok: false, error: "no open-loops.md" };
  const blocks = parse();
  const b = blocks.find((x) => x.id === id);
  if (!b) return { ok: false, error: "no such loop" };
  if (b.done === done) return { ok: true, title: b.title, unchanged: true };

  const body = b.lines.slice(1);
  const rest = (b.lines[0].match(b.done ? DONE : OPENED) || [])[b.done ? 5 : 2] || "";
  const head = done
    ? `- [done ${day} by ${who} · was: ${b.section} · opened ${b.opened}] ${rest}`
    : `- [opened ${b.opened}] ${rest}`;

  // Remove the block wherever it is.
  const lines = src.split("\n");
  const at = lines.findIndex((l) => l === b.lines[0]);
  if (at < 0) return { ok: false, error: "could not locate the loop in the file" };
  lines.splice(at, b.lines.length);

  const put = [head, ...body];
  if (done) {
    let h = lines.findIndex((l) => (l.match(HEAD) || [])[1] === "Done");
    if (h < 0) {
      while (lines.length && lines[lines.length - 1].trim() === "") lines.pop();
      lines.push("", "## Done", "", "Ticked off in the app. The 22:00 review clears these out.", "");
      h = lines.length - 4;
    }
    // Insert at the top of Done, so the most recent win is the one you see.
    let i = h + 1;
    while (i < lines.length && lines[i].trim() === "") i++;
    while (i < lines.length && !lines[i].startsWith("- [")) i++;
    lines.splice(i, 0, ...put, "");
  } else {
    // Put it back under the heading it came from; if that heading is gone, the
    // top of the file is honest about not knowing where it belonged.
    let h = lines.findIndex((l) => (l.match(HEAD) || [])[1] === (b.from || ""));
    if (h < 0) { h = lines.findIndex((l) => HEAD.test(l)); if (h < 0) h = lines.length - 1; }
    let i = h + 1;
    while (i < lines.length && lines[i].trim() === "") i++;
    lines.splice(i, 0, ...put, "");
  }

  fs.writeFileSync(FILE, lines.join("\n").replace(/\n{3,}/g, "\n\n"));
  return { ok: true, title: b.title };
}

/** Open a new loop under a named section, creating the section if it is new. */
export function add({ section = "This week", title, detail = "", day }) {
  if (!title) return { ok: false, error: "title required" };
  const src = read() || "# Open loops\n";
  const lines = src.split("\n");
  const clean = title.replace(/\*/g, "").replace(/\s+/g, " ").trim();

  // Never open the same loop twice. Two identical reminders is how a list stops
  // being read.
  if (parse().some((b) => b.id === idOf(label(clean)))) {
    return { ok: true, duplicate: true, title: clean };
  }

  const body = detail
    ? detail.split("\n").map((l) => "  " + l.trim()).filter((l) => l.trim())
    : [];
  const block = [`- [opened ${day}] **${clean}**`, ...body, ""];

  let h = lines.findIndex((l) => (l.match(HEAD) || [])[1] === section);
  if (h < 0) {
    // Put a new section above "## Done" if there is one, so finished work stays
    // at the bottom where it belongs.
    const d = lines.findIndex((l) => (l.match(HEAD) || [])[1] === "Done");
    const at = d < 0 ? lines.length : d;
    lines.splice(at, 0, `## ${section}`, "");
    h = at;
  }
  let i = h + 1;
  while (i < lines.length && lines[i].trim() === "") i++;
  lines.splice(i, 0, ...block);

  fs.writeFileSync(FILE, lines.join("\n").replace(/\n{3,}/g, "\n\n"));
  return { ok: true, title: clean };
}
