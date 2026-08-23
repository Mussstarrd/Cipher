/**
 * Token accounting. "Are we wasting tokens" deserves a number, not a feeling —
 * every model call records its usage here, the heartbeat reports it, and the
 * daily deep scan can watch the trend.
 */
import fs from "node:fs";
import path from "node:path";
import { DATA, todayET } from "./memory.js";

const FILE = path.join(DATA, "usage.json");

export function record(kind, usage) {
  if (!usage) return;
  let all = {};
  try { all = JSON.parse(fs.readFileSync(FILE, "utf8")); } catch { /* first run */ }
  const day = (all[todayET()] ||= {});
  const k = (day[kind] ||= { calls: 0, in: 0, out: 0, cacheRead: 0, cacheWrite: 0 });
  k.calls += 1;
  k.in += usage.input_tokens || 0;
  k.out += usage.output_tokens || 0;
  k.cacheRead += usage.cache_read_input_tokens || 0;
  k.cacheWrite += usage.cache_creation_input_tokens || 0;
  // Keep a month; the ledger is a gauge, not an archive.
  const days = Object.keys(all).sort();
  while (days.length > 31) delete all[days.shift()];
  fs.mkdirSync(DATA, { recursive: true });
  fs.writeFileSync(FILE, JSON.stringify(all, null, 1));
}
