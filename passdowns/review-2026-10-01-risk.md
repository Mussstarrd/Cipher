# Springboard strategy: risk review (data pulled Oct 1, 2026, 00:25 ET)

Adversarial review by a quant-risk agent with read-only access to account ••••3444.

**Data read:** all 108 filled option orders Sept 2–30 (one page, no cursor), per-trade realized P&L (3-month span), Robinhood's Sept realized P&L bucket, daily bars Jun 1–Sep 30 for NVDA/AMD/INTC/MU/SPCX/AAPL/QQQ, and 30-minute bars for NVDA/AMD for all 21 September sessions.

## (a) Five most dangerous flaws, ranked by expected 30-day dollar damage

**1. The sizing rule is fiction. Expected damage ≈ -$1,500.**
"≤25% of cash, one position" was breached on 9 of 13 buying days. Sept 8–10 alone: $8,443 of premium bought on an $11k account, including 17 lots of NVDA calls struck 10–18% OTM (245/255/260/430 strikes with NVDA at ~$220). Result: **-$3,029, 27% of the account, in one idea.** The brief records this as "15x $280C, -$1,500." Wrong twice: the $280C cost $225 and is still open; the real loss was $3,029 across six strikes. A rule broken 70% of the time with a demonstrated 27% drawdown is the single biggest line item.

**2. Turnover. Expected damage ≈ -$900 to -$1,800.**
111 contracts bought ($21,165), 118 sold ($25,116): **$46k of premium turnover in one month on $11k.** Regulatory fees are trivial (~$6). The spread is not: these are mostly $1–3 contracts with $0.05–0.10 markets, 2–4% per side. That is $900–1,800/month of drag before any edge. Robinhood's own realized figure for September options is **-$396**, not the +$1,571 of highlighted winners.

**3. The exit rules contradict the track record. Either way loses ≈ -$1,000.**
Zero of 68 exits were stop orders. Losers were closed at a **median -48%; 19 of 29 were worse than -35%.** Winners ran to a **median +61%, mean +71%; 26 of 39 exceeded +30%.** Apply the playbook's own +30%/-35% caps to every September lot and **+$2,141 becomes +$261** before spread. The strategy made money only because the rules were ignored on the upside. Following them produces a 57%-win-rate, +30/-35 coin flip that nets negative after spread. Not following them produces -48% median losers. There is no version where this rule set is both followed and profitable on this data.

**4. The day-trade test cannot reach its own target. Expected damage ≈ -$500.**
21 September sessions, 10:30 entry → 3:30 exit, modeled as a 17-day 2%-OTM call (NVDA 42% IV, AMD 70% IV, theta included):

| | NVDA | AMD |
|---|---|---|
| Stock 10:30→15:30, mean / stdev | -0.12% / 0.80% | +0.22% / 1.23% |
| Modeled call return, mean / median | -2.4% / -1.5% | +1.5% / +2.5% |
| Days call hit +30% | **0 of 21** | **0 of 21** |
| Days call hit -35% | 0 of 21 | 0 of 21 |
| Stock move needed for +30% in-day | +1.9% | +3.1% |
| Avg call move per session | 10% | 9% |

The entry window is also the wrong window: AMD's edge was 9:30→10:30 (+0.61%/day average) and 15:30→close (+0.24%); the test excludes both. What is left is ±10% noise, ~1%/session theta, 1–2% spread. Twenty trades at $1,000 is -$500 expected, ±$450.

**5. SPCX concentration into the Oct 9 lock-up. Expected ≈ -$200, tail -$1,000+.**
Shares at Sept 30 close: SPCX 25.9 × $150.94 = **$3,909 (35% of account)**, NVDA 7.4 × $228.29 = $1,689, AMZN 2.68 × $249.04 = $667. SPCX printed -16.4% (Jun 22) and -13.6% (Aug 5) this summer with no lock-up. 328.4M shares unlock Oct 9.

## (b) The data vs. the story

**Round trips (68 closed lots, FIFO by contract):**

| Metric | Playbook narrative | Actual |
|---|---|---|
| Trades | "8" | 68 lots, 108 orders |
| Win rate | implied ~60% | 57% (39/68) |
| Avg win / loss | n/a | +$156 / -$136 (+71% / -48%) |
| Median hold | "next green morning" | **98 hours** (25 lots held >250h) |
| Stops filled | -35% stop-limit at fill | **0** |
| Sept options realized (Robinhood) | +$1,571 winners listed | **-$396** (includes Aug lots sold in Sept: AMZN calls -$1,279, SPCX $200C -$792; expirations -$489) |
| Source of profit | INTC, AMD, NVDA | SPCX Sept 2–16 lots **+$3,300**, INTC +$945; everything else net negative |

The NVDA $230C "bought 3:58 on a -6% flush": NVDA stock fell 0.3% in that half hour and -1.6% on the day. The -6% was the option. That trade failed rule 1 and won anyway, which is how bad rules get reinforced.

**-3% day study, 94 events, six names, Jun 1–Sep 30 (next-day return of the stock):**

| | n | Mean +1d | P(up) | Mean gap |
|---|---|---|---|---|
| NVDA | 9 | +1.59% | **89%** | +0.86% |
| AMD | 22 | -0.03% | 55% | +0.45% |
| INTC | 20 | +0.39% | 45% | +1.26% |
| MU | 20 | +0.79% | 55% | +1.11% |
| SPCX | 20 | +0.07% | 50% | -0.17% |
| Pooled | 94 | +0.43% | 55% | +0.66% |

Unconditional next-day mean for these names was +0.2% with P(up) 48–54%. The "red for no reason" edge is **~+0.2% excess, 55% vs 50%**, swamped by 3.5–4%/day theta. NVDA's 9-for-9 is the memory driving the thesis; the other five names do not reproduce it.

**Modeled 0.4-delta call after a -3% day** (IV = 1.1× realized, held constant; crude): hold 1 day median -0.1%, P(+30%) 21%, P(-35%) 18%. Over 3 days using highs/lows: target-only 34, stop-only 29, **both touched 26** (order unknown), neither 5. Break-even for +30/-35 is 53.8% before spread, ~57% after. The data gives 50–54%.

**Serial flushes** (where the stop-limit does not fill): AMD Jun 4 -3.6% then -10.9% next day (gap -4.5%; call ≈ -80%); AMD Jul 24→28 three straight -3%+ days, -17.7% in 3 days; SPCX Jun 17→18, -19% in 2 days; INTC Jul 1, -13% in 3 days. About four per quarter in this basket.

**Regime:** the entire sample is a semis melt-up. Sept alone: AMD +29%, INTC +29.5%, MU +11%, NVDA +3% while QQQ +3%. Every dip-buy observation comes from a tape where the sector rose 11–30% a month. There is zero evidence here about dip-buying with the 10Y at 5.29% and semis not leading.

## (c) Stop immediately

1. **Stop buying anything struck more than 4% OTM.** The six-strike NVDA cluster is the account's whole September loss.
2. **Stop the day-trade test as designed.** The +30% target is unreachable intraday (0/42 sessions).
3. **Stop "if driving, skip the stop."** This rule authorizes uncapped downside at the exact moment you cannot act. Zero stops have ever filled. Either the stop goes in at fill or the trade is not placed.
4. **Stop treating the IV rule as an IV rule.** "Contract down ≥30% on a 3% move" is just delta/premium: a 17-day NVDA 0.4-delta call drops ~41% on -3% with IV flat; AMD's drops ~26%. It auto-passes NVDA and auto-fails AMD regardless of IV. Use IV percentile from the scan columns.
5. **Stop counting the catalyst rule.** Oct 1 has claims, ISM, six Fed speakers and NKE. Every day qualifies.
6. **Stop reporting a highlight reel.** Pull `get_realized_pnl` monthly; September was -$396.

## (d) What holds up

- **Selling into the open.** 35 of 68 exits were 9:30–10:00 ET. After a -3% day the next open gapped up 62% of the time (+0.66% mean); NVDA's September gain was +5.8% in gaps and -0.7% intraday. Overnight-into-open is the only real edge in this dataset. The day-trade test throws it away.
- **The uncapped version made money.** 57% wins, profit factor 1.54, +$2,141 on matched lots, in a bull tape. The profit is the fat right tail (SPCX +288%, INTC +185%). If you keep a cap, it has to be far above +30%, or you have no strategy.
- **The AAPL lesson** (buy green in a red tape, -$248) was learned and not repeated.
- **The lotto diagnosis** is correct; only the size of the damage was understated by half.
- **Flat by 3:30 costs little**: 15:30→close averaged +0.01% (NVDA) and +0.24% (AMD).

**Approximations stated:** option returns are Black-Scholes with constant IV (realized ×1.1 for the dip study; 42%/70% for intraday), no skew, no bid/ask; daily bars cannot order the high and low within a day; the 30-minute NVDA series has a few thin bars that were used as-is.
