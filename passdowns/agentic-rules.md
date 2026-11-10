# Agentic account rules v4 (account 406267872, cash, ••••7872)

Jeffery handed the whole account to the agent on 2026-10-08 ("You manage it ... make that money grow as quickly and
efficiently as possible. Keep me updated on major changes and I'll pull in the reins if I need to").
v4 written 2026-10-08 from `study-2026-10-07-agentic-rules.md` and `study-2026-10-08-growth.md`.
The agent places orders ONLY in this account. The main account (731123444) is never traded. No options in this account.

## Changes from v3 (tell Jeffery about any future change of this size before it takes effect)
- One rule only: gap-up momentum. The overnight hold (Rule O) is retired: it tied up cash across settlement and
  lowered final equity in every sizing test.
- Size grows with equity instead of a fixed $800 cap.
- Wider universe in two tiers. SOXL (3x) was tested and rejected (−69% drawdown, volatility decay).

## Universe
- Tier A: NVDA, AMD, MU, INTC, SMH.
- Tier B (half size, only on days with no Tier A signal): AVGO, MRVL, PLTR, TSLA, META, AMZN, MSFT, GOOGL, AAPL, QQQ.
- Skip any name reporting earnings today or the prior evening (get_earnings_calendar). Skip any name whose own
  company news explains the gap if it is an acquisition/halt-type event.

## Signal (9:45 am check)
- 9:30 open at least +2.0% above prior close, AND the 9:45 price above the 9:30 open.
- Several qualify: Tier A first; within a tier, take the largest gap. One position per day.

## Size
- Base fraction of equity: 50% for the first 10 trades; 75% for trades 11–20 if the live mean P&L per trade is
  positive; then 100% for SMH and 85% for a single stock. Tier B is half the base fraction.
- Size = min(fraction × equity, settled buying_power − $25). Skip if under $300.
- Hybrid fill: floor(size / ask) whole shares as a limit at the ask, the remainder as a fractional market order.
- Buy only with settled cash; never sell a position bought with unsettled funds before those funds settle.

## Exits
- Disaster stop: stop_market GTC at entry × 0.95 on the whole shares, placed right after the fill.
- 12:30 pm check: if the position is below entry × 0.97, sell it all.
- 3:45 pm check: sell whatever is open, win or lose. Cancel the stop first; whole shares limit at bid, fractional
  piece market.
- Nothing is held overnight.

## Risk limits
- Daily: a loss over 3% of the day's starting equity: no new entries that day.
- Weekly: −6% from the week's starting equity: no entries until Monday.
- Losing streak: 4 losses in a row: halve the fraction for the next 5 trades. 6 in a row: stop and report.
- Drawdown: −10% from the equity high: fraction drops to 50%. −15% from the high: stop trading and check in
  with Jeffery.
- After 20 trades, if the live mean P&L per trade is ≤ 0: stop and report.
- Track the equity high, trade count, streak and live mean in agentic-log.md.

## Reporting
- Every check that trades: one push notification plus a short message (symbol, gap %, shares, price, stop, P&L).
- Checks that do nothing: one line.
- Every fill appended to `passdowns/agentic-log.md`. Sunday: weekly P&L review.

## Schedule (CRON_TZ=America/New_York, Mon–Fri)
- 9:31 am: sell any leftover position (should be none under v4; kept as a safety net).
- 9:45 am: signal and entry.
- 12:30 pm: −3% check.
- 3:45 pm: exit everything, daily summary.

## Honest expectations
- The backtest (35 qualifying days, Tier A without earnings days) shows +1.4% per signal day, 71% wins, worst trade
  −3%, on a six-month semis bull run, with more than 100 variants tested. Expect live results between flat and half
  the backtest. It trades on roughly 25–45% of days; most days it does nothing.
