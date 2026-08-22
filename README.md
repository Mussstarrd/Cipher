# Cipher

A fee-aware, observe-first scanner for [Kalshi](https://kalshi.com) markets.

The premise, argued in full in [docs/DESIGN.md](docs/DESIGN.md): short-term
edge on Kalshi lives in **arbitrage and in reading the exact source a contract
settles on** — not in a model forming opinions from headlines. Kalshi's fee
curve makes near-certain contracts cheap to trade and coin flips expensive, so
the strategy has to be "already know the answer", which in turn makes this a
plumbing problem rather than a forecasting one.

There is **no order-placing code in this build**, on purpose. The journal has to
show a calibrated edge over the market price before execution is worth writing.

## Quick start

No dependencies beyond the standard library.

```bash
python -m cipher fees      # fee-adjusted breakeven table — read this first
python -m cipher demo      # run every scanner over bundled fixtures, offline
python -m cipher scan      # live structural scan (needs network access to Kalshi)
python -m cipher calibrate # score whatever the journal has settled
python -m unittest discover -s tests -t .
```

## What is here

| Module | Role |
|---|---|
| `cipher/fees.py` | Kalshi fee model, breakevens, Kelly. Everything downstream is fee-aware. |
| `cipher/client.py` | Stdlib-only trade-api v2 client. Public market data needs no credentials. |
| `cipher/model.py` | Normalised `Market` / `Event` / `Quote`, so scanners test against fixtures. |
| `cipher/signal.py` | The one object scanners emit, ranked by how much it depends on being clever. |
| `cipher/scanners/structural.py` | Arbitrage: YES/NO crosses, bracket sums, ladder monotonicity. No forecasting. |
| `cipher/scanners/disagreement.py` | Source-vs-book comparison, with staleness and confidence guard rails. |
| `cipher/resolvers/base.py` | The resolver protocol — one per resolution-source family. |
| `cipher/resolvers/barrier.py` | Closed-form pricing for crypto/index level markets. |
| `cipher/journal.py` | Append-only signal log, Brier scoring, calibration table. |

## What is deliberately not here

- **Order placement.** Signing is implemented in `client.Credentials`; nothing
  calls it. Measure first.
- **A headline sentiment model.** [DESIGN.md](docs/DESIGN.md) explains why it
  fails the fee math. When a news monitor gets built it will be a *router* that
  points the resolvers at a source, emitting `Kind.HEURISTIC`, journalled and
  never traded until its own numbers earn otherwise.
- **Live resolvers.** `resolvers/` has the protocol and the barrier maths. The
  weather / CPI / crypto-index feed parsers are the next real work, and they are
  the part that actually takes weeks.

## Next steps

1. Point `scan` at live data and see how often Tier 1 genuinely fires, and at
   what depth. This calibrates whether the arbitrage tier is worth automating.
2. Build one Tier 2 resolver end to end — weather is the best first target: a
   single well-documented NWS product, slow enough to be forgiving, frequent
   enough to accumulate a sample.
3. Run observe-only for a few hundred settled signals. Check the Brier score
   against the market benchmark and the top of the calibration curve.
4. Only then consider execution.

## Caveats

Kalshi is a real-money, CFTC-regulated exchange; automated access is subject to
their API terms and rate limits. Fee parameters here are defaults — verify them
against the live fee schedule before trading size. Books are thin, so anything
that works small may not exist large. Not investment advice.
