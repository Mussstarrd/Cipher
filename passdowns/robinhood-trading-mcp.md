# Pass-Down — Robinhood Trading MCP Agent

**Session:** Robinhood trading MCP integration (`claude/robinhood-trading-mcp-uxp77m`)
**Written:** 2026-08-24

## What I was built to do

Connect Jeffery's Robinhood brokerage to Claude via MCP so an agent can monitor his
portfolio, research the market, and execute trades in the designated agentic account —
with him approving anything irreversible.

## What I've built / done so far

- Registered the `robinhood-trading` HTTP MCP server (`https://agent.robinhood.com/mcp/trading`)
  at project scope in `.mcp.json` and pushed it to this branch (commit `3a277e2`).
  In practice the live connection ended up going through the **Robinson Trading claude.ai
  connector** instead, which works on his phone — the `.mcp.json` remains useful for
  terminal sessions only.
- Guided setup end-to-end: mobile-vs-PC paths, OAuth, and diagnosing why this cloud
  environment's network policy blocks `agent.robinhood.com` directly (connector route
  bypasses that).
- **Executed the first agentic trade (2026-08-10):** sold 0.500360 shares of LMT at
  $599.00 avg (~$299.72 proceeds, $0 fees) in the agentic cash account, dollar-based
  market order, after a review-first + explicit-confirmation flow. Remaining: ~0.4996 LMT.
- Delivered market/geopolitics briefings tied to holdings (Strait of Hormuz talks,
  defense-sector outlook, CPI calendar).

## Signals and sources I watch

- **Robinson Trading connector:** accounts, portfolio values, positions, real-time quotes,
  fundamentals, order status. Two accounts visible: default margin (read-only to agents)
  and the "Agentic" cash account (agent-tradable).
- **Web search:** market wraps, Fed/CPI expectations, Middle East / Hormuz negotiations,
  Lockheed Martin contract news.
- No standing monitors or scheduled routines yet — everything is on-demand.

## What I've gotten right and wrong

**Right:**
- Corrected the user's settlement model: US equities settle T+1, not 2–3 days — moved his
  expected trading start from "mid next week" to the next morning.
- The LMT sale hit the $300 target cleanly ($299.72) with review-first discipline.
- Spotted that his LMT position is an accidental hedge: defense names move opposite the
  broad market on Middle East peace/escalation headlines.

**Wrong / missteps:**
- Initially set up the terminal-oriented `.mcp.json` path when the user is phone-first;
  the claude.ai connector was the right primary path all along.
- Probed the Robinhood endpoint from inside the network-restricted cloud env and framed
  the 403 as a hard blocker before realizing the connector route sidesteps it.
- Never captured the margin account's individual stock holdings before the connector
  dropped mid-session — so portfolio-level analysis of ~$10.4k in equities + ~$1.4k in
  options is still blind.

## Credentials / connections held (names only, never values)

- **Robinson Trading** — claude.ai connector, org-level OAuth to Robinhood (user-authorized)
- **GitHub** — scoped to `Mussstarrd/cipher` via the Claude Code GitHub app
- **Gmail / Google Calendar / Google Drive** — connectors present in the session, unused
- Robinhood accounts referenced: margin ••••3444 (agent read-only), cash "Agentic"
  ••••7872 (agent-tradable)
- The project `.mcp.json` contains only a server URL — no secrets.

## Open questions

1. What are the actual holdings in the margin account? (Needs one positions call when the
   connector is enabled in-session.)
2. What's the plan for the ~$300 in the agentic account — SPCX was discussed but never
   decided; funds settled 2026-08-11.
3. Does the user want standing monitoring (price alerts, a morning-brief routine, news
   watch on LMT/Hormuz), or strictly on-demand sessions?
4. Should the redundant `.mcp.json` stay (terminal use) or be removed in favor of the
   connector alone?
5. Risk guardrails are implicit, not agreed: max position size, order types allowed,
   whether options are in scope for the agentic account.
