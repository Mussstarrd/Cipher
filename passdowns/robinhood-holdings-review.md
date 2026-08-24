# Pass-Down — Robinhood Holdings Review Desk

**Session span:** Aug 10–21, 2026 (document written Aug 24, 2026)
**Agent:** single Claude Code session acting as market analyst / portfolio watchdog for Jeffery's Robinhood accounts. No sub-agents were used; all "check-ins" were scheduled wake-ups of this same session.

---

## 1. What this agent was built to do

- Review and grade the user's holdings across two Robinhood brokerage accounts (a margin account, read-only to the agent; a small "Agentic" cash account where agent-placed trades are permitted but were never used).
- Sweep news on a cadence around the positions: US macro (CPI, PPI, FOMC minutes, Jackson Hole), the Iran–Strait of Hormuz war (oil, shipping, escalation language), Asia sessions (KOSPI as overnight semiconductor signal), and company-specific catalysts.
- Build probability cards before binary events (earnings, data prints, lockup expirations) and translate option marks into honest odds using market-implied chance-of-profit, implied vs. historical moves, and IV-crush scenario tables.
- Act as a critical sparring partner per the "institutional investment committee" profile the user adopted mid-session (accuracy over confidence, flag uncertainty, challenge emotion-driven reasoning, prefer "no trade" to low-conviction ideas).
- Schedule its own wake-ups around known event times (data prints, earnings, expiries) and report verdicts promptly.

Execution reality: every order this session was placed by the user in their own app. The agent's authority in the trade-enabled cash account was never exercised.

## 2. What it has built / done so far

- **Portfolio baseline and grades (Aug 10):** overall C+; stock sleeve B− (main risk: SPCX ≈ half the stock sleeve, semiconductors most of the rest); options book D+ (out-of-the-money-heavy, several near-total losses at intake).
- **An earnings-trade screen** (adapted from a prompt the user supplied, with amendments): post-earnings reaction history vs. implied move, breakeven clear-count, open-interest/spread liquidity gate, overnight IV-crush tables, "no trade" as default verdict, 3–5% position sizing cap. Run fully on CSCO-style setups; AMAT and NVDA screens were promised and remain owed.
- **Standing exit rules (pre-committed with the user):**
  - SPCX Dec $300 calls: sell trigger at $4.90 (original cost).
  - INTC Aug-26 $97 calls: thesis confirmed at $95 reclaim / bail below $90.
  - Expiring paper: sell into first-hour strength on expiry morning, never hold through a same-day Fed event hoping.
  - Proposed but not adopted: mandatory overnight cooling period between banking a win and redeploying the proceeds.
- **Scheduled check-in workflow:** five one-shot wake-ups around CPI, PPI, Home Depot earnings, FOMC minutes, and Walmart earnings / SPCX unlock. All fired; two arrived late due to infrastructure disconnects — verify data timestamps before trusting any "premarket" claim from a late-firing timer.

## 3. Signals and sources watched

- **Brokerage data (Robinson Trading MCP):** real-time equity and option quotes (greeks, implied volatility, market-implied chance of profit, open interest, spreads), positions, order history, portfolio values, earnings calendar/results, per-symbol news.
- **Web headline sweeps:** macro prints and Fed communications; Iran–Hormuz war developments (oil prices, strait shipping counts, escalation/de-escalation rhetoric); KOSPI/Samsung/SK Hynix sessions as an overnight read-through to MU/NVDA; company catalysts (SPCX lockup tranches, Starship Flight 14 timing, Nvidia partnership/stake disclosures; INTC $20B equity raise and Apple foundry deal; MU memory-squeeze; AMZN analyst notes and AWS metrics).
- **Market microstructure heuristics:** premarket treated in phases (4–7am dead zone, 8–9am desks, 9:30 auction, ~10am verdict); premarket Level 2 depth treated as near-zero-signal; index futures vs. single-name divergence as a head-fake tell.

## 4. Track record — right and wrong, with examples

**Got right:**
- **CSCO earnings skip (Aug 12):** showed the Friday $124 call would lose ~50% even if the stock rose ~1.5% — CSCO rose ~1.5% after hours and the math played out exactly.
- **Hold-through-print warnings:** the "beats get sold" pattern (COHR, CSCO −8.4% despite beat-and-raise, AMAT) was called before it completed; the user's WMT $115 call held through the print lost ~$270 of $314 on a guidance cut.
- **Premarket-is-thin-air:** flagged the Fri Aug 14 and Mon Aug 17 green premarkets as unreliable; both evaporated at the open.
- **SPCX unlock supply risk:** warned repeatedly about the Aug 20 tranche (319M shares); the stock fell 4% through unlock day.
- **FOMC minutes hawkish risk (Aug 19):** flagged as the day's main hazard; minutes printed hawkish and yields rose.

**Got wrong / missed:**
- **SMCI (Aug 10–11):** framed the earnings call as a fair coin at market odds (~27%); it roughly doubled — the user's instinct beat the caution.
- **AMZN bounce timing (Aug 18):** handicapped a Tuesday relief bounce that arrived Wednesday instead; the user's Wednesday-expiry stack died waiting.
- **"AMZN shrugged off WMT" (Aug 20, 7am):** true premarket, false by the close (−2.2%).
- **SpaceX Flight-14 date headline** called "the most probable positive headline in 24–48h" (Aug 15); it did not land in that window.
- **Option-mark estimates from stock price alone** occasionally missed IV moves (e.g., SPCX $300 calls rising on a red stock day). Never quote marks without pulling them.
- **Process:** one scheduled check-in delivered ~a day late after server disconnects; the promised AMAT screen was never delivered before its event.

## 5. Credentials / connections held (names only — no values, no tokens)

- **Robinson Trading MCP server:** read access to both brokerage accounts (margin account ending 3444 — read-only; cash "Agentic" account ending 7872 — agent-trading enabled, never used). Quotes, options, orders, portfolio, earnings, news endpoints.
- **Claude Code Remote MCP server:** self-scheduling via one-shot triggers (`send_later`); no triggers currently pending except the routine that produced this document.
- **WebSearch** via the session's managed proxy.
- **Connected but never used:** Gmail, Google Calendar, Google Drive, GitHub MCP (repo `mussstarrd/cipher` in scope).
- **User identity:** email address on file, used for identification only.

## 6. Open questions for the next shift

1. **AMZN Aug-24 $270 calls ×2** — expire Monday Aug 24 (imminent as of this writing); salvage order was still unplaced Friday morning. Highest-urgency open item; likely resolved worthless if untouched.
2. **SPCX Aug-21 $140 call** — expired Friday Aug 21; final disposition unconfirmed (sell-the-spike rule was standing).
3. **12× SPCX Sep-25 $200 calls** (bought into the unlock flush at $0.47) — strike is ~46% OTM; no exit rule adopted. Proposed: sell half on any double. Catalyst watch: Starship Flight 14 (late August), further lockup tranches.
4. **3× INTC Aug-26 $97 calls** — $95/$90 rule agreed but enforcement untested; they expire the same day NVDA reports (Aug 26).
5. **NVDA earnings Wed Aug 26 + Jackson Hole Aug 27–29** — the owed full screen; user floated a buy-calls / sell-before-print structure that was conditionally endorsed with a hard exit date.
6. **SPCX share concentration** (~30 shares, roughly a third of the book) into future unlock tranches — trim debate open since Day 1, never resolved.
7. **CPNG patient-buy thesis** — $299.72 settled and idle in the Agentic account; entry never executed.
8. **Cooling-period rule** — proposed, acknowledged, not adopted. Highest-value behavioral change available: exits became consistently disciplined; impulse entries (e.g., INTC calls bought two hours after a no-go verdict) are where money leaks.
9. **WMT positions** — the Sep-4 $115 call scraps and 3× Aug-28 $0.18 dip-buy calls: no exit plan on record.
