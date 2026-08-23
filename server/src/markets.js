/**
 * A paper portfolio. Fake money, real prices, real consequences on paper —
 * Jeffery's way of building knowledge before any of it is real.
 *
 * The rule from CLAUDE.md does not bend: Hearth NEVER places a real trade, and
 * nothing in this file can reach a brokerage — quotes come from a public CSV
 * endpoint and the "portfolio" is a JSON file on this disk. The ledger is
 * mirrored to memory/portfolio.md so the family can read every move and the
 * 22:00 review can learn from the reasoning.
 *
 * Money is adults-room material, always.
 */
import fs from "node:fs";
import path from "node:path";
import { DATA, MEM, todayET } from "./memory.js";

const FILE = path.join(DATA, "portfolio.json");
const MIRROR = path.join(MEM, "portfolio.md");
const START_CASH = 100_000;

const fresh = () => ({
  started: todayET(), cash: START_CASH, startCash: START_CASH,
  positions: {},           // SYM -> {shares, cost}  (cost = total dollars in)
  watch: [],               // symbols to quote at wakes without holding them
  log: [],                 // every trade, forever
});

export function loadBook() {
  try { return { ...fresh(), ...JSON.parse(fs.readFileSync(FILE, "utf8")) }; }
  catch { return fresh(); }
}
function saveBook(b) {
  fs.mkdirSync(DATA, { recursive: true });
  const tmp = FILE + ".tmp";
  fs.writeFileSync(tmp, JSON.stringify(b, null, 2));
  fs.renameSync(tmp, FILE);
  mirror(b);
}

/* ---------- quotes ---------- */
const SYM = /^[A-Z][A-Z0-9.]{0,9}$/;

let qcache = { at: 0, key: "", quotes: null, error: null };

/** Last close for each symbol, via stooq's free CSV. 15 min cache. */
export async function quotes(symbols) {
  const syms = [...new Set(symbols.map((s) => String(s).toUpperCase()))].filter((s) => SYM.test(s));
  if (!syms.length) return { quotes: {}, error: null };
  const key = syms.join(",");
  if (qcache.key === key && Date.now() - qcache.at < 15 * 60e3 && qcache.quotes) return qcache;
  try {
    const url = `https://stooq.com/q/l/?s=${syms.map((s) => s.toLowerCase() + ".us").join(",")}&f=sd2t2ohlcv&h&e=csv`;
    const res = await fetch(url, { signal: AbortSignal.timeout(10_000) });
    if (!res.ok) throw new Error(`stooq ${res.status}`);
    const out = parseStooq(await res.text());
    qcache = { at: Date.now(), key, quotes: out, error: null };
  } catch (e) {
    qcache = { at: Date.now(), key, quotes: null, error: String(e?.message || e) };
  }
  return qcache;
}

/** Exported bare for tests — parsing is where silent lies come from. */
export function parseStooq(csv) {
  const out = {};
  const rows = csv.trim().split("\n");
  for (const row of rows.slice(1)) {
    const c = row.split(",");
    // Symbol,Date,Time,Open,High,Low,Close,Volume — N/D means unknown ticker.
    const sym = (c[0] || "").replace(/\.us$/i, "").toUpperCase();
    const close = parseFloat(c[6]);
    if (sym && Number.isFinite(close)) out[sym] = { price: close, asOf: `${c[1]} ${c[2]}` };
  }
  return out;
}

/* ---------- trading ---------- */
const usd = (n) => `$${n.toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;

/**
 * Execute one paper action. Returns a one-line human outcome either way.
 * op: buy | sell | watch | unwatch. Prices are last close — good enough for
 * learning, and honest about it in the ledger.
 */
export async function paperTrade({ op, symbol, qty, reason = "", who = "someone" }) {
  const sym = String(symbol || "").toUpperCase();
  if (!SYM.test(sym)) return { ok: false, line: `"${symbol}" is not a ticker I recognise the shape of.` };
  const b = loadBook();

  if (op === "watch") {
    if (!b.watch.includes(sym)) { b.watch.push(sym); saveBook(b); }
    return { ok: true, line: `Watching ${sym}. It will show up with quotes at check-ins.` };
  }
  if (op === "unwatch") {
    b.watch = b.watch.filter((s) => s !== sym);
    saveBook(b);
    return { ok: true, line: `Stopped watching ${sym}.` };
  }

  const n = Math.floor(Number(qty));
  if (!Number.isFinite(n) || n <= 0) return { ok: false, line: `How many shares of ${sym}? I need a whole number.` };
  const q = await quotes([sym]);
  const px = q.quotes?.[sym]?.price;
  if (!px) return { ok: false, line: `No price for ${sym}${q.error ? ` (${q.error})` : " — is the ticker right?"}. Nothing was done.` };

  if (op === "buy") {
    const cost = px * n;
    if (cost > b.cash) return { ok: false, line: `${n} ${sym} at ${usd(px)} is ${usd(cost)} — the paper account only has ${usd(b.cash)}. Nothing was done.` };
    b.cash -= cost;
    const p = b.positions[sym] || { shares: 0, cost: 0 };
    b.positions[sym] = { shares: p.shares + n, cost: p.cost + cost };
    if (!b.watch.includes(sym)) b.watch.push(sym);
    b.log.push({ at: new Date().toISOString(), who, op, sym, n, px, reason });
    saveBook(b);
    return { ok: true, line: `PAPER BUY: ${n} ${sym} at ${usd(px)} (last close) = ${usd(cost)}. Cash left: ${usd(b.cash)}.` };
  }

  if (op === "sell") {
    const p = b.positions[sym];
    if (!p || p.shares < n) return { ok: false, line: `The paper account holds ${p?.shares || 0} ${sym} — cannot sell ${n}. Nothing was done.` };
    const proceeds = px * n;
    const costOut = (p.cost / p.shares) * n;
    b.cash += proceeds;
    p.shares -= n; p.cost -= costOut;
    if (p.shares === 0) delete b.positions[sym];
    b.log.push({ at: new Date().toISOString(), who, op, sym, n, px, reason });
    saveBook(b);
    const pl = proceeds - costOut;
    return { ok: true, line: `PAPER SELL: ${n} ${sym} at ${usd(px)} = ${usd(proceeds)} (${pl >= 0 ? "+" : ""}${usd(pl)} vs cost). Cash: ${usd(b.cash)}.` };
  }

  return { ok: false, line: `Unknown action "${op}".` };
}

/** The adults-room briefing line: holdings, live worth, watchlist. */
export async function summary() {
  const b = loadBook();
  const syms = [...new Set([...Object.keys(b.positions), ...b.watch])];
  if (!syms.length) return "Paper portfolio: untouched — $100,000.00 of pretend cash waiting. Say \"paper buy 10 AAPL\" (or watch a ticker) in the Adults thread to start.";
  const q = await quotes(syms);
  const lines = [];
  let worth = b.cash;
  for (const [sym, p] of Object.entries(b.positions)) {
    const px = q.quotes?.[sym]?.price;
    const val = px ? px * p.shares : null;
    if (val) worth += val;
    const pl = val ? val - p.cost : null;
    lines.push(`  ${sym}: ${p.shares} sh, in ${usd(p.cost)}${val ? `, now ${usd(val)} (${pl >= 0 ? "+" : ""}${usd(pl)})` : ", no quote"}`);
  }
  const watchOnly = b.watch.filter((s) => !b.positions[s]);
  for (const sym of watchOnly) {
    const px = q.quotes?.[sym]?.price;
    lines.push(`  ${sym} (watch): ${px ? usd(px) : "no quote"}${q.quotes?.[sym]?.asOf ? ` as of ${q.quotes[sym].asOf}` : ""}`);
  }
  const total = Object.keys(b.positions).length
    ? `Paper portfolio worth ${usd(worth)} (${worth >= b.startCash ? "+" : ""}${usd(worth - b.startCash)} all-time), cash ${usd(b.cash)}:`
    : `Paper account: ${usd(b.cash)} cash, watching only:`;
  return [total, ...lines, q.error ? `  (quotes failed: ${q.error})` : ""].filter(Boolean).join("\n");
}

/** Human-readable ledger in memory/, so every move is diffable and reviewable. */
function mirror(b) {
  const pos = Object.entries(b.positions).map(([s, p]) =>
    `| ${s} | ${p.shares} | ${usd(p.cost / p.shares)} | ${usd(p.cost)} |`).join("\n");
  const log = b.log.slice(-100).map((t) =>
    `- [${t.at.slice(0, 16)}Z] ${t.who}: ${t.op.toUpperCase()} ${t.n} ${t.sym} @ ${usd(t.px)}${t.reason ? ` — ${t.reason}` : ""}`).join("\n");
  fs.writeFileSync(MIRROR, `# Paper portfolio

Simulated only. Started ${b.started} with ${usd(b.startCash)} of pretend money.
Hearth never places a real trade — this ledger exists to learn on.

Cash: ${usd(b.cash)}
Watching: ${b.watch.join(", ") || "nothing"}

## Positions

| symbol | shares | avg cost | total in |
| --- | --- | --- | --- |
${pos || "| — | | | |"}

## Trades (last 100)

${log || "None yet."}
`);
}
