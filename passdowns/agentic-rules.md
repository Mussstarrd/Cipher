# Agentic account rules v3 (account 406267872, cash, ••••7872)

v1 approved by Jeffery 2026-10-03. v3 written 2026-10-07 at Jeffery's request ("figure out the recipe and
start actively trading"), from `study-2026-10-07-agentic-rules.md` (122 sessions, after 0.10% round-trip cost).
The agent places orders ONLY in this account and ONLY inside these rules. The main account (731123444) is never
traded.

## What changed from v2
- Added Rule G (gap-up momentum). It was the only rule with a clear edge in the study.
- Entry A is now Rule O: the overnight hold runs every day in SMH and is sold at the 9:30 open, not at 9:50/10:25.
- Dropped Entry B (flush buy). The study showed −0.41% per signal day; NVDA lost on 5 of 6 trades.

## Sizing
- Max $800 per position, max 2 positions. Keep at least $400 of total cash uninvested.
- Buy only with settled cash: use get_portfolio buying_power (cash accounts exclude unsettled funds). Never buy with
  unsettled proceeds and then sell before they settle (good-faith violation). If buying_power is under $300, skip
  the entry.
- Hybrid sizing: buy floor(size / ask) whole shares with a limit at the ask, then the rest of the size as a
  fractional market order (dollar_amount), regular hours. If one share costs more than the size, buy it all as
  fractional.
- Stops cover the whole shares only (Robinhood does not allow stops on fractional shares).

## Rule G: gap-up momentum (9:45 am check), the main rule
- Candidates: NVDA, AMD, MU, INTC, SMH. Skip any name with its own earnings within 2 sessions (INTC Oct 22,
  AMD Nov 3, NVDA Nov 17).
- Signal: the 9:30 open is at least +2.0% above the prior close, AND the 9:45 price is above the 9:30 open.
- Several qualify: take the largest gap. One Rule G position per day.
- Size: min($800, buying_power − $100).
- Stop: stop_market GTC at entry × 0.96 for the whole shares. This is a disaster stop only; a −1.5% stop made the
  backtest worse.
- Exit: the 3:45 pm check sells it no matter what. Cancel the stop first, whole shares limit at bid, fractional
  piece market.
- Study: 80 trades / 36 days, 64% wins, +1.13% per trade, portfolio +1.02% per signal day, t = 3.1, worst trade
  −3.0%. Held up in both halves of the window and with the top 3 trades removed. Fires on about 30% of days.

## Rule O: overnight hold (3:45 pm buy, 9:31 am sell), the secondary rule
- Every trading day at 3:45, buy SMH. Size: min($800, buying_power − $100). Skip if buying_power is under $300
  (this happens on some Rule G days because of settlement).
- Stop: stop_market GTC at entry × 0.97 for the whole shares. It only triggers in regular hours, so it is a backstop
  in case the 9:31 sell fails.
- Exit: the 9:31 am check sells it at the open, win or lose. Cancel the stop, whole shares limit at bid, fractional
  piece market.
- Study: semis overnight +0.33% per night, t = 1.5, max drawdown −12%. This is mostly market exposure in a semis
  bull run. The edge faded from +0.47% (Apr–Jul) to +0.13% (Aug–Oct). Kill it if it loses money over 15 trades.
- None of the filters (red day, late dump, skip late ramp) helped, so there are none.

## Loss limits
- Daily: realized + open loss of $50 (2% of $2,500): no new entries for the rest of the day.
- Weekly: $125 (5%): no new entries until Jeffery reviews on Sunday.
- Rule G five losses in a row, or Rule O down over its first 15 trades: stop that rule and report.

## Reporting
- Every check posts one short message and a one-line push notification: what, shares, price, stop, why.
- Every fill is appended to `passdowns/agentic-log.md` (date, time, symbol, side, qty, price, rule, P&L).
- Checks that do nothing post one line.

## Schedule (CRON_TZ=America/New_York, Mon–Fri)
- 9:31 am: Rule O exit (sell the overnight SMH).
- 9:45 am: Rule G entry.
- 3:45 pm: Rule G exit, then Rule O entry, then the daily summary.
