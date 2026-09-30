# Springboard ("pogo stick") Playbook

**Owner:** Jeffery (margin account ••••3444, agent read-only; all orders placed by hand in the app)
**Written:** 2026-09-30
**Status:** live, with a two-week day-trade test starting 2026-10-01

## The idea

Buy near-money calls on quality AI names when they are red for a sector or macro reason,
not a company reason, with a catalyst inside 48 hours. Sell into the first green morning
for a 30-40% scoop. Do not hold for the thesis; hold for the bounce.

## Entry: all five or no trade

1. Stock down 3% or more on the day for a sector/macro reason. Any company-specific
   headline (downgrade, lawsuit, guidance, insider sale, product news) disqualifies.
   Relative volume under 0.7 disqualifies (drift, not a flush).
2. A catalyst inside 48 hours: earnings from a peer, CPI/PPI/PCE/jobs/FOMC, a launch,
   a product event, a known unlock date.
3. Strike within 4% of the price, expiry 2-3 weeks out, delta 0.35-0.45.
4. Implied vol not pumped. Rule of thumb: the contract is down 30% or more on a 3% stock
   move, so IV is not being bid.
5. Position is 25% of cash or less. One position at a time.

## Exit

- +30%: sell. Do not wait for +40%.
- Gap up: sell in the first 30 minutes.
- Green at 3:30 pm the day before a catalyst: sell.
- Stop: -35%, entered as a stop-limit at fill, limit $0.25-0.50 under the stop.
  Stops do not fill after hours or in the opening print.
- Robinhood rejects two open sell orders on one contract: cancel the stop before placing
  the profit sell.

## Never

- Buy something green in a red tape.
- Buy the day of the catalyst.
- Hold through earnings unless it is house money.
- Go below 25% cash.
- Buy far out-of-the-money lottery tickets before an event (the Oct 2 NVDA $280C lesson).

## Day-trade test (Oct 1 to Oct 14, 2026)

FINRA eliminated the pattern day trader rule effective 2026-06-04 (SEC approval
2026-04-14). Robinhood removed day-trade counting and day-trade calls the same day. The
only remaining floor is $2,000 equity when using leverage. There is no limit on round
trips in the ••••3444 account.

| Rule | Value |
|---|---|
| Max size per trade | $1,000 (about 25% of cash) |
| Positions at once | one |
| Trades per day | two round trips max |
| Entry window | 9:45 to 11:00 am ET only |
| Take profit | +30% limit entered at fill |
| Stop | -35% stop-limit entered at fill |
| Hard exit | flat by 3:30 pm unless the playbook says hold (catalyst tomorrow, position green) |
| Setup | same five entry conditions |
| No-trade | FOMC days after 1:30 pm; first 15 minutes after an 8:30 data print; any day driving without eyes on it |

If driving: enter the +30% limit and skip the stop. Grade the test after two weeks on
win rate, average win, average loss, and spread paid.

## Track record (Sept 2026)

Winners, all bought after a washout and sold into the first green:

| Trade | Result |
|---|---|
| INTC $117C | +$945 |
| AMD $630C (Sept 29) | +$270 |
| NVDA $230C (Sept 30) | +$125 |
| AMD $630C (Sept 30) | +$166 |

Losers, all bought before an event or at a local high:

| Trade | Result |
|---|---|
| NVDA Oct 9 $280C (far OTM, bought Oct 2 lesson) | about -$1,500 |
| SPCX Nov calls | loss |
| AAPL calls | -$280 |

Observed pattern over 10 sessions (Sept 15-28): SPCX gains came overnight (+3.3% summed
gaps) with negative intraday (-4.7%); INTC gaps dominated; NVDA/AMD mixed. Overnight holds
into a catalyst are where the money was. Intraday fades are where it was given back.

## Known calendar

- SPCX lock-up ladder: Oct 9 (328.4M shares), Oct 24 (328.4M), post-Q3 earnings (about
  1.3B), Dec 8 (about 800M). Musk's 6.4B locked to 2027-06-12.
- Routine cron times are UTC and need shifting one hour after the November DST change.

## Automation

- Saved Robinhood scan "Springboard: red for no reason", scan_id
  `5c78ad55-36f1-44a0-8a7a-a3835f368021`: stocks, market cap >= $20B, down >= 3% on the
  day, 30-day average options volume >= 20,000.
- Routine `trig_01Y15gpaTUqqzFtH3TQFKTMw` "Pogo stick 8:45 am pre-market check",
  cron `45 12 * * 1-5` UTC. Pre-market table, positions to sell in the first 30 minutes
  with limits, headlines, catalysts, day-trade candidate.
- Routine `trig_01JwKPMpdXsp2wPbUCRojNWu` "Pogo stick 3:15 pm scan",
  cron `15 19 * * 1-5` UTC. Runs the scan plus the core watchlist, disqualifies on
  company news and low relative volume, confirms the 48-hour catalyst, prices the
  contract, flags 3:30 sells including day-trade positions.
- Core watchlist: NVDA, AMD, MU, INTC, AVGO, MRVL, SNDK, SPCX, AAPL, META, GOOGL, AMZN,
  MSFT, TSLA, PLTR, CRWV, QQQ, SPY.
