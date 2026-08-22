# Can a scanner pick short-term Kalshi winners?

Short answer: **yes for a narrow class of markets, and no for the version most
people picture.** The version that works is a latency-and-arithmetic machine.
The version that does not work is an LLM reading headlines and forming opinions
about what will happen. Those two get conflated constantly, so this document
separates them and explains what the code in this repo does about it.

## The fee arithmetic decides the strategy before you write any code

Kalshi's taker fee is `ceil_to_cent(0.07 x contracts x P x (1-P))`. Two
consequences fall straight out of that shape, and they constrain everything
downstream:

| Price | Fee per contract | True probability needed to break even |
|------:|-----------------:|--------------------------------------:|
| 50c   | 1.750c           | 51.75%                                |
| 75c   | 1.314c           | 76.31%                                |
| 90c   | 0.630c           | 90.63%                                |
| 95c   | 0.334c           | 95.33%                                |
| 97c   | 0.204c           | 97.20%                                |
| 99c   | 0.070c           | 99.07%                                |

*(500-contract orders; run `python -m cipher fees` for the full table.)*

**First consequence: the fee is worst exactly where forecasting is hardest.** A
coin-flip market costs 1.75c to enter. To make money on 50/50 contracts you need
a persistent 3.5-point probability edge over the market — on questions whose
whole appeal is that they are genuinely uncertain. That is not a scanner, that
is a claim to be better than everyone else at predicting politics.

**Second consequence: the fee nearly vanishes at the extremes**, which is where
the strategy has to live. But the extremes come with a brutal ratio: buying at
97c risks 97 to make 3. One loss erases 33 wins. So the bar is not "usually
right" — it is "right ~98% of the time, verified". No sentiment model clears
that bar. Only knowing the answer does.

That is the whole thesis: **the money is in contracts that are nearly settled,
and the only reliable way to be right 98% of the time about a nearly-settled
contract is to already be reading the thing that settles it.**

## Four tiers, ranked by how much they depend on being clever

The code encodes this ranking in `signal.Kind`; `signal.rank` sorts by it, so a
locked arbitrage always outranks a bigger "edge" from a weaker source.

### Tier 1 — Arbitrage. No forecast at all. *(built)*

Prices on the same exchange contradicting each other. Nothing is predicted, so
the hit rate is a property of your execution, not your judgement:

- **YES ask + NO ask < 100c.** Own one of each, collect exactly 100c.
- **Exhaustive brackets summing to less than 100c.** Buy the whole ladder.
- **Monotonicity violations.** `P(X > 110)` cannot exceed `P(X > 100)`. When the
  higher strike is buyable below the lower strike's bid, the spread is a
  riskless credit.

These are rarer and thinner than a newcomer hopes, and thin books mean the real
constraint is *partial fills* — half an arbitrage is a naked directional bet.
That is why every signal here is sized to the thinnest leg and the legs are
emitted together. Realistic expectation: small, infrequent, real.

### Tier 2 — Deterministic resolution. The actual opportunity. *(scaffolded)*

For markets settling on a published, machine-readable number, the outcome is
often knowable before the book reprices:

- **Weather.** "High temperature in NYC today" settles on a specific NWS
  product. By mid-afternoon the day's high is frequently locked in, but the
  market can still be pricing 88c.
- **Economic releases.** CPI, NFP, Fed decisions. The number is published at a
  known instant; anyone parsing it faster than the median trader has seconds of
  edge on a contract about to go to 100.
- **Crypto and index levels.** Settle on a named index at a named timestamp.
- **Sports, box office, award shows.** Settle on a specific feed.

The rule that makes this work, and the one people get wrong: **read the exact
source the exchange settles on, not a proxy for it.** A market on the NWS daily
climate summary for KNYC is not a market about the weather — it is a market
about what that specific product says. Trading it off a different forecast
provider is how you lose on a technicality while being right about reality.

This is a plumbing race, not an intelligence contest. You win it with a fast,
correct parser and lose it with a stale one. `resolvers/base.py` therefore makes
every estimate carry an `observed_at`, and `scanners/disagreement.py` throws
away anything older than 20 seconds by default. Near expiry a stale source
doesn't give you a slightly worse signal — it points the wrong way.

### Tier 3 — Model vs. book on a continuous underlying. *(built)*

The hourly crypto and index markets are closed-form: given live spot and a
volatility estimate, "above $113,000 at 5pm" is a barrier calculation
(`resolvers/barrier.py`). Near expiry the probability surface is very steep, so
a book that is a minute stale on spot can be tens of cents wrong.

Two ways to lose money here, both handled in code and both worth stating plainly:

1. **Confusing "above X at expiry" with "ever touches X".** The touch
   probability is roughly double the terminal one. Misreading the settlement
   rule is a systematic, one-directional error — it will not average out.
2. **Realised volatility understating risk before a scheduled event.** The
   estimator sees a calm ten minutes and concludes nothing can happen in the
   next ten, right as a CPI print lands. Hence `vol_floor`.

### Tier 4 — Headlines and patterns. The weakest tier. *(deliberately not built)*

This is the part of the original idea that does not survive contact with the fee
table. Reasons, in order of how much they cost you:

- **You are not first.** Kalshi's flagship markets are watched by people with
  direct feeds and standing orders. By the time a headline is in an RSS feed it
  is in the price. Sentiment on public news is the definition of already-priced.
- **Sentiment is not probability.** "Markets rattled by Fed comments" tells you
  nothing calibrated about whether a specific contract settles YES. Turning
  vibes into a number that clears a 97% breakeven is not a solved problem.
- **Headlines are adversarial.** Sources get promoted, walked back, and outright
  faked, especially around events with money on them. A pipeline that trades on
  text is a pipeline someone can feed.
- **Backtests here lie more than usual.** Pattern-mining thousands of resolved
  markets for "setups" finds patterns in noise, and the survivors look
  spectacular in-sample. Prediction-market histories are short, thin, and
  regime-dependent.

**What headlines are genuinely good for: routing, not deciding.** A news monitor
that says *"something just happened that touches these 6 tickers — go read the
authoritative source now"* is valuable, because it tells the Tier 2 machinery
where to point. It should never size a position by itself. If this gets built,
it emits `Kind.HEURISTIC` and is journalled without ever being traded, until its
own numbers earn otherwise.

## How you find out whether any of it works

The failure mode here is not losing money — it is being unable to tell whether
you have an edge. A scanner that emits confident probabilities is easy to build
and impossible to evaluate by eye, so `journal.py` exists from day one:

- Every signal is written down **before** the outcome is known, traded or not.
- Two Brier scores are reported: the scanner's, and **the market price at signal
  time**. Beating a coin flip is not the bar. Beating the book is the bar, and
  most ideas do not.
- A calibration table asks the question that decides everything: of the signals
  where the scanner said 95%, did 95% happen? Overconfidence at the high end is
  the failure that matters, because that is precisely where the money goes.

The gate before real money: a few hundred settled signals, a Brier score that
beats the market benchmark, and a calibration curve that does not sag at the top
end. There is deliberately no order-placing code in this build. Execution is the
easy part and writing it first is how people end up trading an unmeasured model.

## Honest expectations

- Tier 1 is real but small — a few cents per contract on thin books, gated by
  fill quality rather than by discovery.
- Tier 2 is the one worth the effort, and it is an infrastructure investment: a
  colocated-ish process, per-source parsers, sub-second polling, careful reading
  of settlement rules. Weeks of work per source family, not an afternoon.
- Tier 3 is real at expiry and competitive; your data path speed is the edge.
- Tier 4 is a router, not a strategy.
- Everything here is capacity-constrained. Kalshi books are thin; a strategy
  that works at $500 a trade may not exist at $10,000 a trade.

None of this is investment advice, and the parts that can lose money are the
parts not built yet.
