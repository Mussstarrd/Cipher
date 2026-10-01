# Springboard playbook review: improvements (Oct 1, 2026)

Constructive review by a process-coach agent with read-only access to account ••••3444.

**Data basis.** Daily bars Jun 1–Sep 29 for NVDA, AMD, INTC, MU, AVGO, SPCX plus SMH/QQQ (84 sessions); 10-min bars Sep 29–30; live scan run; scanner filter specs; Robinhood earnings calendar Oct 1–Nov 30. Approximations: "down 3%" is measured close-to-close; "bounce captured" uses the stock's highest print in the next 1–3 sessions; a **+2.5% stock move ≈ +30% on a 0.40-delta, 2–3 week call**, ignoring vega/theta. 69 events is a sanity check, not a backtest.

## (a) Five highest-value changes, ranked

**1. Replace "down 3%" with a sector-relative, ATR-scaled trigger.**
Current ATR20 as % of price: NVDA 2.5, AVGO 2.9, AMD 4.0, MU 4.2, SPCX 4.2, INTC 5.3. A fixed −3% is 1.2 ATR for NVDA but 0.56 ATR for INTC (noise).
New rule, all four: (i) stock ≤ −max(3%, 1.0×ATR20%) → NVDA −3, AMD −4, MU −4.2, INTC −5.3, SPCX −4.2; (ii) SMH ≤ −1.25%; (iii) excess vs SMH (stock% − SMH%) ≥ −3 pts; (iv) rel vol ≥ 1.0.

| Trigger (pooled, 6 names) | n | next-day gap | max high ≤3d | 3-day close | next-day worst | ≥+2.5% high within 3d |
|---|---|---|---|---|---|---|
| Current: close ≤ −3% | 69 | +0.5% | +6.7% | +0.5% | −2.6% | 75% |
| …of which stock >3 pts worse than SMH | 26 | +0.3% | +7.8% | −0.5% | −3.1% | 85% |
| Proposed (i–iv) | 16 | +1.0% | +7.0% | +3.3% | −1.7% | 62% |
| Proposed + closed in top half of day range | 5 | +1.3% | +10.2% | +7.3% | −1.1% | 100% |
| Proposed + closed in bottom half | 11 | +0.9% | +5.6% | +1.4% | −1.9% | 45% |

The "worse than sector" bucket is the trap: biggest bounces, but worst 3-day close and worst next-day drawdown; that is where a −35% stop fires before the bounce. AVGO: 6 fires, 50% hit, −1.7% 3-day. Drop AVGO from the long list. SPCX never fired the proposed trigger (its flushes are idiosyncratic/lock-up driven); trade it only on the lock-up-day-2 setup below.

**2. Add an intraday structure gate ("the reclaim") before buying.** Enter only after price prints a 10-min close above the 9:30–10:00 opening-range high **or** above session VWAP, between 10:00 and 11:00. Daily proxy above (close in top half) is the whole edge: 100% vs 45%. For the Sep 29 3:58 pm buy: NVDA 10-min bars show no reclaim (227.9→227.3 on 3.4M shares); it worked because of the gap, not structure. Late-day entries are allowed only when a Tier-1 catalyst is pre-market tomorrow.

**3. Exit: stop selling the opening print; sell the first failure.** Sep 30 NVDA 10-min bars: open 229.27, OR high 231.43, the 10:00 bar pushed to 232.37 and *closed* 230.38 (failed breakout); that was the sell, ~+1.3% above the open; 3:50 pm it dumped to 228.29 on 6.3M shares. Rule:
- At fill: +30% GTC limit (always, driving or not).
- Gap-up morning: do not sell 9:30–9:50 unless price breaks the first 5-min low. Sell on the first 10-min close below the prior 10-min low after 9:50, or at 10:30 at the latest.
- Anytime the contract shows ≥ +20%: cancel the +30% limit, replace with a stop-limit at +10% trigger / +5% limit (Robinhood permits one open sell). That caps the AMD-style giveback at ~$100 instead of $400 and keeps the upside open.
- Two-contract positions (anything under $5): sell one at +30%, trail the second with the +10% stop.
- Day-trade days: flat at 3:30 unless Tier 1 tomorrow and green.

**4. Catalyst tiers: only Tier 1 justifies an overnight hold.**
- Tier 1 (full $1,000, hold into the event, sell within 60 min of the reaction): direct-supply-chain peer earnings (MU/TSM/ASML/LRCX/SNDK for semis; MSFT/GOOGL/META/AMZN capex prints for NVDA/AMD/AVGO), FOMC decision, CPI, jobs. Own earnings = never hold.
- Tier 2 (60% size = $600, day trade only, flat 3:30): product/keynote events (GTC, Apple, Tesla), PPI/PCE/ISM, Fed chair, lock-up expirations. Lock-up day itself is a no-buy; the setup is **day 2 after** a lock-up flush with rel vol ≥ 1.2.
- Tier 3 (does not qualify): regional Fed speakers, unrelated earnings, conferences, "AI news".

**5. Sizing ladder.** Cap = min($1,000, 22% of cash) today. Step up to 25% of cash (recomputed each Sunday) after 10 completed trades with win rate ≥ 55% **and** profit factor ≥ 1.3; ceiling $2,500 until the account passes $20k. Step down: two consecutive −35% stops → cap $500 for the next 5 trades; two stops in a day → done for the day; three losers in a week → no trades until the Sunday review. One position at a time stays.

## (b) Q4 2026 catalyst calendar (watchlist-relevant)

| Date | Event | Tier | Source |
|---|---|---|---|
| Oct 2 | Jobs 8:30; TSLA Q3 deliveries | 1 / 2 | BLS; TSLA date unverified |
| Oct 9 (Fri) | SPCX lock-up 328.4M | 2 (neg) | Brief; staggered schedule confirmed by press |
| Oct 10 | TSLA robotaxi event | 2 | **Unverified** (single low-quality source) |
| Oct 13 | JPM, GS am | 3 | RH calendar, verified |
| Oct 14 | CPI 8:30; ASML am | 1 | BLS; RH verified |
| Oct 15 | TSM; PPI 8:30 | 1 / 2 | RH verified; fedratecalc |
| Oct 20 | NFLX pm; TXN pm | 3 / 2 | RH; TXN tentative |
| Oct 21 | LRCX pm (verified); TSLA pm (tentative) | 1 / 2 | RH |
| Oct 22 | INTC pm | 1 | RH tentative; never hold INTC through it |
| Oct 24 (Sat) | SPCX lock-up 328.4M → trades Mon Oct 26 | 2 (neg) | Brief; weekday effect is a note |
| Oct 27 | Apple event (Macs); NXPI pm | 2 / 3 | TechCrunch, **year unverified** |
| Oct 28 | FOMC 2:00 pm; MSFT/GOOGL/META pm; KLAC pm | 1 | Fed calendar; RH tentative |
| Oct 29 | PCE 8:30; AAPL, AMZN, SNDK (verified), WDC pm | 2 / 1 | BEA/fedratecalc; RH |
| Nov 1 | DST ends; shift routine crons +1h UTC | — | Playbook |
| Nov 3 | AMD pm | 1 (peer) | RH tentative |
| Nov 4 | QCOM, ARM pm | 2 | RH verified |
| Nov 6 | Jobs | 1 | BLS |
| Nov 9 | CRWV pm | 2 | RH tentative |
| Nov 10 | CPI | 1 | BLS |
| Nov 12 | AMAT pm | 2 | RH verified |
| Nov 13 | PPI | 2 | fedratecalc |
| Nov 17 | NVDA pm (verified); SPCX pm (tentative) → post-Q3 ~1.3B lock-up follows | 1 | RH; lock-up date unverified |
| Nov 24 | DELL pm, ADI am | 2 | RH tentative |
| Nov 25 | PCE | 2 | BEA |
| Dec 1 | NVDA GTC DC keynote 2–4 pm ET (event Nov 30–Dec 3); MRVL pm | 2 | nvidia.com; RH tentative |
| Dec 4 | Jobs | 1 | BLS |
| Dec 8 | SPCX full 180-day lock-up | 2 (neg) | Brief + press |
| Dec 9 | FOMC 2:00 pm | 1 | Fed calendar |
| Dec 10 | CPI | 1 | BLS |
| Dec 15 | PPI | 2 | fedratecalc |
| Dec 16 | MU FQ1 | 1 | **Unverified** (forecast) |

No AMD Q4 event found (Advancing AI was July 22–23). OpenAI DevDay was Sep 29.

## (c) 2-minute "company or macro?" checklist (run every candidate)

1. `get_equity_quotes` [name, SMH, QQQ, 3 peers]. Excess = name% − SMH%. ≥ −1.5 → sector; −1.5 to −3 → gray (steps 3–6 must all be clean); < −3 → company, **stop**.
2. Peers: ≥ 2 of 3 peers down ≥ 1.5% → sector confirmed.
3. Rel vol from the scan column: 1.0–2.5 = flush; > 2.5 with excess < −1.5 = news.
4. `get_sec_filing_index`, last 2 days: any 8-K (2.02 results, 5.02 officer change, 8.01) or large Form 4 → company. Server down → mark unverified, 50% size.
5. `get_equity_analyst_ratings`: rating or target change dated today → company.
6. Earnings column / `get_earnings_calendar`: own report within 2 sessions → disqualify.
7. Only now one WebSearch, `"<TICKER> shares" <today's date>`, prefer reuters.com, marketwatch.com, barrons.com, investors.com. Undated result = ignore. Tiebreaker, never the primary test.

## (d) Scan filter changes (exact enums; PERCENTAGE values are decimals)

Keep: `FILTER_TYPE_INSTRUMENT_TYPE = STOCK`; `FILTER_TYPE_MARKET_CAP >= 20000000000`; `FILTER_TYPE_AVERAGE_OPTIONS_VOLUME >= 20000` (1d, length 30).
Change: `FILTER_TYPE_PERCENT_CHANGE_FROM_CLOSE BETWEEN [-0.08, -0.025]` interval 1d plot Close (below −8% is company news; −2.5% catches NVDA-sized flushes).
Add: `FILTER_TYPE_SECTOR ANY_OF ["Technology","Communication Services"]`; `FILTER_TYPE_RELATIVE_VOLUME >= 0.9` interval 1d length 10 (at 3:15 pm day volume is ~85% complete, so 0.9 ≈ 1.0 full-day); `FILTER_TYPE_RSI BETWEEN [25, 45]` interval 1d length 14 (RSI < 25 is a trend, not a flush).
Add columns, not filters: `FILTER_TYPE_AVERAGE_TRUE_RANGE` 1d length 20; `FILTER_TYPE_GAP` 1d (gap-down > 2% = overnight headline); Historical volatility beside the IV column (IV/HV > 1.3 = pumped). No IV-percentile or sector-relative filter exists; the SMH comparison stays in the routine.

## (e) Automation (all read-only; none ever places, cancels or modifies an order)

1. **10:15 am "did the flush happen"**: quotes for watchlist + SMH/QQQ, excess vs SMH, drop ÷ ATR20, 5-min VWAP/opening-range reclaim check, runs checklist (c), prices the 0.35–0.45-delta contract 2–3 weeks out and prints the limit. One line on no-candidate days.
2. **Alert prices in the 8:45 brief**: prints prior close × (1 − max(3%, ATR20)) for NVDA, AMD, MU, INTC so the user sets Robinhood price alerts; reads `get_alert_log` to report what fired overnight.
3. **3:25 pm flat-by-3:30 check**: `get_option_positions`; for anything opened today: red or no Tier 1 tomorrow → "SELL, bid X, suggested limit mid−0.05"; green with Tier 1 tomorrow → "HOLD, replace limit with +10% stop-limit". Flags ≥ +20% positions for the stop swap.
4. **Sunday 6 pm review**: pulls week's fills, grades each against the entry and exit rules (names the rule broken), computes win rate, avg win/loss, profit factor, spread paid vs mid, applies the sizing ladder, outputs next week's cap, builds the 14-day catalyst list with tiers.
5. **8 pm T−1 calendar check**: tomorrow's Tier 1/2 events, no-trade windows (8:30–8:45, FOMC after 1:30), names reporting within 2 days (excluded), lock-up weekday adjustments.

Sources: BLS schedule (bls.gov/schedule/2026), FOMC dates (fedratecalc.com/fomc-meeting-schedule), PPI/PCE dates (fedratecalc.com/us-economic-calendar), NVIDIA GTC DC (nvidia.com/en-us/gtc-dc/attend), AMD Advancing AI (ir.amd.com), Apple Oct 27 invite (techcrunch.com, verify year), SPCX lock-up structure (daytradingtoolkit.com), MU Dec 16 forecast (wallstreethorizon.com).
