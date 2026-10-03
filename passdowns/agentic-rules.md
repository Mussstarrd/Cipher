# Agentic account rules (account 406267872, cash, ••••7872)

Approved by Jeffery 2026-10-03. Funding clears Mon 2026-10-05, $2,000-$2,500.
The agent places orders ONLY in this account and ONLY inside these rules. The main account
(731123444) is never traded.

## Sizing (read equity at each check)
- Starting equity under $2,250: max $800 per position.
- Starting equity $2,250 or more: max $1,100 per position.
- Always keep at least $400 uninvested. Max 2 positions at once.
- Whole shares only, so a stop order can be placed. Shares = floor(cap / ask). If one share costs
  more than the cap, skip that name.
- Buy only with settled cash: use get_portfolio buying_power (cash account excludes unsettled).
  Never buy with unsettled proceeds and sell before they settle (good-faith violation).

## Universe
NVDA, AMD, MU, INTC, SMH, QQQ. Skip any name with its own earnings within 2 sessions
(INTC Oct 22, AMD Nov 3, NVDA Nov 17). Nothing else, ever.

## Entry A: overnight hold (3:45 pm check)
- Candidate: a core name red on the day, OR down 0.3%+ in the last 30 minutes on volume above the
  afternoon's median 5-minute bar. Preference order: NVDA, AMD, SMH, QQQ, MU, INTC.
- Max one new overnight position per day.
- Buy with a marketable limit at the ask, whole shares, then immediately place a stop_market
  sell GTC at entry x 0.97 for the same quantity.

## Entry B: flush buy (10:25 am check)
- Name down at least max(3%, 20-day ATR%): NVDA 3, AMD 4, MU 4.2, INTC 5.3, SMH 2.5, QQQ 2.
- SMH down 1.25%+ (for chip names) and the name no more than 3 points worse than SMH.
- No 8-K/large Form 4 in 2 days (get_sec_filing_index), no rating/target change today
  (get_equity_analyst_ratings).
- Last 10-minute close above the 9:30-10:00 opening-range high or above VWAP.
- Buy as in Entry A, stop at entry x 0.97 GTC. Hold up to 3 sessions.

## Exits
- Overnight holds (Entry A): 9:50 check. Gapped up: sell if the last 10-minute close is below the
  prior 10-minute low, or if up 1.5%+ from entry. Otherwise 10:25 check sells regardless.
  Flat or red open: sell at 10:25 regardless.
- Flush holds (Entry B): sell at +3% from entry at any check, or at the 10:25 check on the third
  session after entry.
- To sell: cancel the open stop order first, then sell with a marketable limit at the bid.
- Stops are GTC and stay on overnight.

## Loss limits
- Daily: realized + open loss of 2% of starting equity ($40 at $2,000, $50 at $2,500): no new
  entries for the rest of the day.
- Weekly: 5% ($100 / $125): no new entries until Jeffery reviews on Sunday.

## Reporting
- Every check that acts posts one short message: what, how many shares, price, stop, why.
- Every fill is appended to `passdowns/agentic-log.md` (date, symbol, side, qty, price, P&L).
- Checks that do nothing post one line.

## Schedule (UTC cron, EDT; shift +1h after Nov 1 DST)
- 9:50 am ET `50 13 * * 1-5` exits
- 10:25 am ET `25 14 * * 1-5` exits + Entry B
- 3:45 pm ET `45 19 * * 1-5` Entry A + daily summary
