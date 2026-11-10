# Springboard ("pogo stick") Playbook, version 2

**Owner:** Jeffery (margin account ••••3444, agent read-only; all orders placed by hand in the app)
**Written:** 2026-09-30. **Revised:** 2026-10-01 after two independent reviews (see
`review-2026-10-01-risk.md` and `review-2026-10-01-improvements.md`).
**Status:** proposed v2, pending Jeffery's sign-off. v1 rules that the data killed are listed at the end.

## What the data actually says (Sept 2026, 68 closed lots, 108 orders)

- Win rate 57%. Average win +$156 (+71%). Average loss −$136 (−48%). Profit factor 1.54 on lots
  bought and sold in September (+$2,141). Robinhood's realized September options figure is −$396
  once August lots sold in September and expirations are counted.
- The money came from the fat right tail: SPCX early-September lots +$3,300, INTC +$945. Capping
  every lot at +30%/−35% would have turned +$2,141 into +$261 before spread.
- The only measurable edge is overnight into the open: after a −3% day the next open gapped up 62% of
  the time; NVDA's whole September gain was in gaps (+5.8%) with intraday negative (−0.7%).
- Intraday +30% was hit 0 of 42 NVDA/AMD sessions. A same-day round trip is noise plus spread.
- Zero stops ever filled. Losers were closed by hand at a median −48%.
- The sizing rule was broken on 9 of 13 buying days. The Sept 8–10 far-OTM NVDA cluster lost $3,029,
  27% of the account.
- Premium turnover was $46k on an $11k account. Spread drag alone is $900–1,800 a month.
- The whole sample is a semis melt-up (AMD +29%, INTC +29.5% in September). Nothing here says the
  dip-buy works with the 10-year at 5.29% and semis not leading.

## The trade

Buy a near-money call on a quality AI name after a sector flush, hold overnight, sell into the next
morning's gap or the first failure after it. The day-trade freedom (no PDT rule since June 4, 2026)
is for cutting losers the same day and selling the morning after without counting, not for forcing
same-day round trips.

## Entry: all six or no trade

1. **Drop is real for the name.** Stock down at least max(3%, one 20-day ATR): NVDA −3%, AMD −4%,
   MU −4.2%, INTC −5.3%. Only these four plus SPCX on the lock-up rule below. AVGO is off the list.
2. **Sector, not company.** SMH down at least 1.25% and the name no more than 3 points worse than
   SMH. Two of three peers down at least 1.5%. Run the two-minute checklist (below). Any 8-K,
   downgrade, target cut, or own earnings inside two sessions disqualifies.
3. **Volume confirms.** Relative volume at least 1.0 (0.9 on the 3:15 scan).
4. **Structure confirms.** A 10-minute close back above the opening-range high or above session VWAP
   between 10:00 and 11:00. If buying late in the day instead, only when a Tier 1 catalyst lands
   pre-market tomorrow.
5. **Contract.** Strike no more than 4% out of the money, hard limit. Expiry 2–3 weeks. Delta
   0.35–0.45. Check IV against the 30-day historical vol column: IV/HV above 1.3 is pumped, skip.
6. **Size.** Max $1,000 or 22% of cash, whichever is smaller. One position. No exceptions, no
   "confidence" multiplier. If the contract is over $10, buy one. If under $5, buy two.

## Exit

- **Stop at fill, every time.** −35% stop-limit, limit $0.25 under the stop, entered before the buy
  confirmation screen is closed. No stop means no trade. Driving is not an exemption; if you can't
  enter the stop, you don't buy.
- **No +30% cap.** The profit is in the tail. Instead, ratchet: at +20%, cancel the stop and replace
  it with a stop-limit at +10%. At +40%, move it to +25%. At +60%, move it to +40%. Robinhood allows
  one open sell per contract, so there is never a limit and a stop at the same time.
- **Two contracts:** sell one into the morning gap, ratchet the other.
- **Gap-up morning:** don't sell the opening print. Sell on the first 10-minute close below the prior
  10-minute low after 9:50, or by 10:30 at the latest.
- **No gap:** if the stock opens flat or red the morning after, sell by 10:30 regardless. The edge was
  the gap. Without it there is no trade.
- **Time stop:** never hold a position past the second morning. Median hold in September was 98 hours.
  That is where theta ate the losers.

## Catalyst tiers (replaces "catalyst inside 48 hours")

- **Tier 1, full size, the overnight hold is the plan:** direct peer earnings (MU, TSM, ASML, LRCX,
  SNDK for semis; MSFT, GOOGL, META, AMZN capex prints for NVDA/AMD), FOMC decision, CPI, jobs. Own
  earnings: never hold through.
- **Tier 2, 60% size ($600), sell by 10:30 next morning no matter what:** product events (GTC, Apple,
  Tesla), PPI, PCE, ISM, Fed chair, lock-up expirations. The lock-up day itself is a no-buy; the
  SPCX setup is day two after a lock-up flush with relative volume at least 1.2.
- **Tier 3, does not qualify:** regional Fed speakers, unrelated earnings, conferences, "AI news."

## Sizing ladder

- Today: min($1,000, 22% of cash).
- Step up to 25% of cash, recomputed each Sunday, after 10 completed trades with win rate at least
  55% and profit factor at least 1.3. Ceiling $2,500 until the account passes $20k.
- Step down: two consecutive stops → cap $500 for the next five trades. Two stops in one day → done
  for the day. Three losers in a week → no trades until the Sunday review.

## Never

- Buy more than 4% out of the money. The six-strike NVDA cluster was the whole September loss.
- Buy green in a red tape (the AAPL lesson, −$248).
- Buy the day of the catalyst, or hold through own earnings.
- Hold SPCX calls across a lock-up date (Oct 9, Oct 24, post-Q3 ~Nov 17, Dec 8).
- Enter without the stop. Zero stops have ever filled. That is the number to change first.
- Report the highlight reel. The Sunday review pulls realized P&L from Robinhood, not from memory.

## Two-minute "company or macro" checklist

1. Quotes for the name, SMH, QQQ, three peers. Excess = name% − SMH%. At least −1.5 → sector.
   −1.5 to −3 → gray, steps 3–6 must all be clean. Below −3 → company, stop.
2. Peers: at least two of three down 1.5%+ → sector confirmed.
3. Relative volume: 1.0–2.5 = flush. Above 2.5 with excess below −1.5 = news.
4. SEC filing index, last two days: any 8-K or large Form 4 → company.
5. Analyst ratings: any rating or target change dated today → company.
6. Earnings calendar: own report within two sessions → disqualify.
7. Only then one web search, "<TICKER> shares <date>". Undated results ignored. Tiebreaker only.

## Concentration

Shares as of Sept 30: SPCX $3,909 (35% of account), NVDA $1,689, AMZN $667. SPCX printed −16.4%
(Jun 22) and −13.6% (Aug 5) with no lock-up. 328.4M shares unlock Oct 9. Flagged, Jeffery's call.

## Automation

- Saved Robinhood scan "Springboard: red for no reason", scan_id
  `5c78ad55-36f1-44a0-8a7a-a3835f368021`. Proposed filter changes are in the improvements review.
- Routine `trig_01Y15gpaTUqqzFtH3TQFKTMw`, 8:45 am ET pre-market brief (cron `45 12 * * 1-5` UTC).
- Routine `trig_01JwKPMpdXsp2wPbUCRojNWu`, 3:15 pm ET scan (cron `15 19 * * 1-5` UTC).
- Proposed, not yet built: 10:15 am flush check, 3:25 pm position check, Sunday 6 pm review,
  8 pm next-day calendar.
- Shift crons one hour after the Nov 1 DST change.
- Core watchlist: NVDA, AMD, MU, INTC, SPCX (lock-up rule only), with AVGO, MRVL, SNDK, AAPL, META,
  GOOGL, AMZN, MSFT, TSLA, PLTR, CRWV, QQQ, SPY, SMH as context.

## v1 rules the data killed

| v1 rule | Why it's gone |
|---|---|
| Sell at +30% | Capping September lots at +30% cut profit from $2,141 to $261 |
| −35% stop "if you can watch it" | Zero stops ever filled; median loser −48% |
| Day-trade test, flat by 3:30 | +30% intraday hit 0 of 42 sessions; the edge is the overnight gap |
| Entry window 10:15–11:00 only | AMD's edge was 9:30–10:30; keep the structure gate, not the clock |
| "Catalyst inside 48 hours" | Every day qualified |
| "Contract down 30% on a 3% move = IV not pumped" | That's just delta math; auto-passes NVDA, auto-fails AMD |
| Flat −3% trigger for every name | 1.2 ATR for NVDA, 0.56 ATR for INTC |
| "If driving, skip the stop" | Authorizes uncapped loss at the exact moment you can't act |
