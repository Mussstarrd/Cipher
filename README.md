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
python -m cipher demo      # structural scanners over fixtures, offline
python -m cipher weather --fixture tests/fixtures/weather_day.json \
        --verify KXHIGHNY:KNYC          # the weather strategy, offline, with order tickets
python -m cipher power     # how many settled trades before edge beats luck
python -m cipher scan      # live structural scan (needs network access to Kalshi)
python -m cipher calibrate # score whatever the journal has settled
python -m unittest discover -s tests -t .
```

To actually trade it, follow [docs/PLAYBOOK.md](docs/PLAYBOOK.md). Live runs need
outbound access to `api.elections.kalshi.com` and `api.weather.gov`, and
`CIPHER_CONTACT` set to an email or URL — the NWS asks callers to identify
themselves and the code refuses to send an anonymous request.

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
| `cipher/resolvers/weather.py` | NWS daily-high resolver. The first real source-of-truth reader. |
| `cipher/resolvers/stations.py` | Series → station registry. Unverified by default, on purpose. |
| `cipher/ticket.py` | Signal → a priced order, stating the loss side as loudly as the win side. |
| `cipher/power.py` | Sample size and a pre-committed decision rule for proving an edge. |
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

## The weather strategy in one paragraph

A day's maximum temperature can only go up. The NWS hourly observation feed is a
*lower bound* on what the daily climate report will publish, and the exchange
settles on that report. So once the observed max is above a bracket's ceiling,
that bracket cannot win — no forecasting required, and the inequality holds even
though the feed may understate the true max. That asymmetry is the whole edge,
and it is why `weather.py` marks only that one direction as deterministic. The
mirror claim ("the max is still below this bracket, so it will not get there")
is *not* safe, and is treated as a model guess.

## Next steps

1. Verify station mappings against the live rulebooks and flip them in
   `stations.py`. Nothing else matters until this is right.
2. Run `weather` observe-only through a few evenings and check that resolutions
   match what the resolver implied.
3. Replace `probability_rises_by` with per-station empirics from a season of
   observations. It is currently a transparent prior, and it is the highest-value
   thing to improve.
4. Point `scan` at live data to see how often the arbitrage tier genuinely fires.
5. Only after `calibrate` beats the market Brier score, consider execution.

## Caveats

Kalshi is a real-money, CFTC-regulated exchange; automated access is subject to
their API terms and rate limits. Fee parameters here are defaults — verify them
against the live fee schedule before trading size. Books are thin, so anything
that works small may not exist large. Not investment advice.
