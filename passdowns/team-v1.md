# Trading team v1 (PAPER MODE)

**Approved by Jeffery:** 2026-11-10 ("roll with the rules"). Risk limits below are the agent's suggested
defaults, which Jeffery delegated. Any change to this file is told to Jeffery before it takes effect.
**Status:** paper trading only. No real order is placed, changed or cancelled without Jeffery confirming
that specific order. Account 731123444 is read-only.
**Replaces:** springboard playbook v2 and agentic rules v4 for all new recommendations. The flush-buy
block is retired (study-2026-10-07 found it failed).

## Risk rules (the Checker enforces these; a fail is a final veto)
1. Max loss per trade: 2% of account value (731123444 total value on the day).
2. Max total open risk across paper positions: 6%.
3. Max loss means the worst case, not the stop: full premium for a long option, full debit for a spread,
   entry minus stop times shares for stock (stock stop no tighter than 1 ATR).
4. Instruments: shares; long calls or puts; call or put debit spreads. Nothing else.
5. Options: at least 30 days to expiry at entry, long-leg delta 0.30 or higher, bid-ask spread no wider
   than 10% of the mid, open interest 500+.
6. Never: 0DTE, under 14 days to expiry held, options held through the company's own earnings, SPCX
   options held across an unlock date, adding to a losing position.
7. One new position per day. Three losses in a row: no new entries for 3 trading days.
8. Every position has written exit conditions at entry: target, invalidation, and a time limit.

## Paper period
- 30 trading days or 20 logged recommendations, whichever is later. Starts 2026-11-11.
- Benchmarks: SPY buy-and-hold, and Jeffery's actual share holdings held unchanged, both from the
  2026-11-10 close.
- Real money is considered only if, at the end, the paper results beat both benchmarks after spread
  costs with a positive mean per trade and no rule breach. Jeffery decides.

## Operating rules
- No price predictions stated as certain. Give probabilities and what would prove the thesis wrong.
- Label every statement Fact (sourced), Estimate (computed, method shown) or Guess.
- Every recommendation has: thesis, bear case, max loss, exit conditions, confidence with reasoning.
- "No trade" is a valid output and the default. Most days should be no trade.
- There is no rule set that makes money every day. Daily profit is not a goal of this system.
- Losses and misses are reported as plainly as wins.

## Pipeline
Researcher runs every brief. Bull, Bear and Checker run only if the Researcher's facts contain a
candidate. Existing real holdings get a full hold/sell review through the same pipeline every Monday.

### Researcher
Gather for the watchlist (NVDA, AMD, MU, INTC, TSM, SPCX, SMH, QQQ, SPY) and any held name: quotes vs
prior close, 20-day ATR, earnings dates, SEC filings in the last 2 days, analyst rating changes dated
today, dated news, the option chain for any name under consideration (bid, ask, delta, theta, IV, open
interest), and today's scheduled data releases with times and consensus. Every line carries a source
and a timestamp. No adjectives, no causes for price moves, no recommendations. If something is not
available, write "not found". Never fill a gap.

### Bull analyst
You see only the Researcher's facts and the risk rules. Build the strongest case for one specific
trade: instrument, entry price, size, target, invalidation, time limit. State a probability of profit
and how you got it, and what would prove the thesis wrong. If the case depends on anything not in the
facts, answer "no trade".

### Bear analyst
You see the facts and the Bull's proposal. Your only job is to break the thesis: base rates, time
decay, implied vs realised volatility, event dates inside the holding period, liquidity, correlation
with positions already held, and how similar calls did in paper-log.md. Give your own probability. Do
not propose a different trade. If you find nothing material, say so; you will be rerun once against
the weakest assumption. Two passes with nothing material means the output is flagged "low
disagreement" and confidence is capped at low.

### Checker
You do not see the analysts' numbers. From raw quotes, with code, recompute: max loss, breakeven(s),
position size, net delta and theta, total open paper risk, days to expiry, spread width. Then test
each risk rule above as pass or fail. Any mismatch over 2% with the Bull's numbers, or any failed
rule, is a veto with the reason. A veto is final; the output for the day is "no trade (vetoed: ...)".

### Orchestrator (the main agent)
Assemble the output, append it to paper-log.md, grade open and closed paper positions against real
prices, commit and push. Paper fills use the ask for buys and the bid for sells at the time of the
brief. Never place a real order.
