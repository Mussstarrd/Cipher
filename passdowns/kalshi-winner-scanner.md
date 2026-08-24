# Pass-down: Kalshi winner scanner

**Agent:** Claude Code session working `Mussstarrd/Cipher`, branch `claude/kalshi-winner-scanner-j572xn`
**Written:** 2026-08-24
**Head at handoff:** `a50b112`
**Status:** code complete for two tiers, **never run against live data**

> The single most important line in this document: **nothing here has ever
> contacted Kalshi or the NWS.** Both hosts are blocked by this environment's
> egress policy. Every result described below comes from fixtures and unit
> tests. No trade has been placed, no signal has settled, and the strategy's
> edge is entirely unmeasured.

---

## 1. What it was built to do

Answer a question — "could a scanner read headlines or spot patterns and pick
very short-term Kalshi winners with decent certainty?" — and then build whatever
part of the answer survived scrutiny.

The conclusion, argued in `docs/DESIGN.md`: **yes for a narrow class of markets,
no for the headline-reading version.** Kalshi's taker fee is
`0.07 × contracts × P × (1-P)`, which peaks at 1.75¢/contract on coin flips and
falls to 0.33¢ at 95¢. So the fee is worst exactly where forecasting is hardest,
and the only economically sensible contracts are near-certainties — where the
payoff ratio is brutal (buying at 97¢ risks 97 to make 3, so one loss erases 33
wins) and the bar is therefore "right ~98% of the time, verified" rather than
"usually right."

Sentiment on public headlines cannot clear that bar: by the time news is in a
feed it is in the price, sentiment is not calibrated probability, and text
inputs are adversarial. What *can* clear it is already reading the source the
contract settles on. So the mandate became: **build the plumbing, not the
oracle.**

## 2. What it has built

Four commits, 137 tests, no third-party dependencies (stdlib only, deliberately
— the process that watches markets should not break because a transitive dep
did). There is **no order-placing code**, on purpose.

| Module | Role |
|---|---|
| `cipher/fees.py` | Fee model, order-size-aware breakevens, Kelly |
| `cipher/client.py` | Kalshi trade-api v2 client; public market data needs no credentials |
| `cipher/model.py` | Normalised `Market`/`Event`/`Quote` so scanners test offline |
| `cipher/signal.py` | The one object scanners emit, ranked by trustworthiness |
| `cipher/scanners/structural.py` | Arbitrage: YES/NO crosses, bracket sums, ladder monotonicity |
| `cipher/scanners/disagreement.py` | Source-vs-book, with staleness and confidence guards |
| `cipher/resolvers/weather.py` | NWS daily-high resolver — the one real source reader |
| `cipher/resolvers/stations.py` | Series→station registry, unverified by default |
| `cipher/resolvers/barrier.py` | Closed-form pricing for crypto/index level markets |
| `cipher/ticket.py` | Signal → priced order, stating the loss side beside the win side |
| `cipher/power.py` | Sample size + pre-committed decision rule for proving an edge |
| `cipher/journal.py` | Append-only signal log, Brier scoring, calibration table |

Test distribution: weather 47, disagreement 16, barrier 15, fees 14,
structural 14, journal 12, ticket 10, power 9.

**The core insight worth carrying forward** (weather): a day's maximum
temperature never falls, so the hourly NWS feed is a *lower bound* on what the
daily climate report publishes — `reported_max >= max(observed)`. That
inequality points one way only, which decides which claims are safe. "Observed
max is already above this bracket's ceiling" is deterministic; the bracket
cannot win. The mirror claim ("still below the floor, so it won't get there") is
**not** safe, because the feed can miss a spike the station caught. The resolver
marks only the first as deterministic.

## 3. Signals and sources it watches

Two, both **currently unreachable**:

- `api.elections.kalshi.com/trade-api/v2` — `/events`, `/markets`,
  `/markets/{ticker}/orderbook`. Public, no credentials needed for reads.
- `api.weather.gov` — `/stations/{id}/observations`. Requires a contact address
  in the User-Agent.

Both return `403` at the egress gateway (`connect_rejected … policy denial`).
That policy is enforced upstream of the container and cannot be changed from
inside the session; it is environment configuration the owner must edit.

Also relevant: **this proxy does not support WebSocket upgrades at all.** Even
if the hosts were allowlisted, a session here gets REST polling only. Fine for
hourly weather; fatal for the crypto/barrier tier, which is a latency race.

## 4. What it got right and wrong

### Right

- **Leading with the fee curve.** Deriving the strategy from the fee arithmetic
  before writing scanner code is what ruled out the headline-sentiment approach
  early instead of after building it.
- **The monotonicity asymmetry.** Recognising the inequality only points one way
  is what makes the weather claim defensible rather than a dressed-up guess.
- **Declining to build the requested thing.** The original ask was headline
  reading. Building the arbitrage/source-truth tiers instead, and writing down
  why, was the right call — and is documented rather than silently substituted.
- **Journal and power calculation before execution.** `python -m cipher power`
  turns "when will we know" into an exact binomial decision rule instead of a
  feeling.

### Wrong — concrete, in the order they were found

1. **Miscalculated my own test expectations.** Asserted
   `taker_fee_cents(95, 500) == 34`; the answer is 167
   (0.07 × 500 × 0.95 × 0.05 = $1.6625). Also asserted EV was negative at
   p=0.955 on a 95¢ contract when breakeven is 0.9533, so it is positive. Both
   were my arithmetic, not the code's — but I initially read the failures as
   possible code bugs.
2. **Arbitrage legs ranked negatively.** Each leg of a locked arb, priced alone,
   buys YES at exactly its implied probability and pays a fee — so it looks like
   a losing trade and sorted *below* weak heuristic signals. Fixed by attaching
   `locked_profit_cents` and dividing across `legs`.
3. **An unrealistic fixture.** My first YES/NO-cross fixture used a 3¢ gap at
   mid prices (45/52) and correctly did **not** fire, because fees peak at 50¢
   and ate it. The code was right; the fixture was naive.
4. **Staleness rejected every weather estimate.** The disagreement scanner's 20s
   default is right for crypto and impossible for an hourly feed — it silently
   returned zero signals. Fixed with per-source presets, plus a carve-out:
   deterministic claims get a much longer allowance, because age degrades a
   guess but not a monotone fact.
5. **`edge_cents` amortised fees over one contract**, so per-order cent rounding
   made a 97¢ contract look like it cost a full cent in fees. This suppressed
   genuinely good signals.
6. **The rounding guard was too blunt.** It refused any reading near a bracket
   boundary — including cases where *both* plausible roundings implied the same
   answer, which is no ambiguity at all. Replaced with enumerate-and-check-
   consensus.
7. **Test pollution.** `stations.verify` mutates a module-level registry; one
   test class did not restore it, and a "no signals expected" assertion started
   failing because an unrelated test had verified the station. Fixed with an
   isolation mixin.
8. **A User-Agent placeholder that never resolved.** The NWS agent string named
   `CIPHER_CONTACT` and then never read it, so a live run would have identified
   itself as `contact: set CIPHER_CONTACT` — passing tests, then getting
   throttled in production. Now fails loudly at call time.

Pattern worth noting for whoever picks this up: **most of these were integration
failures that unit tests passed straight through.** Items 4, 5, 6 and 8 each
produced a silent "no signals" or a silently wrong number rather than an error.

## 5. Credentials and connections held

*Named only. No values, tokens, or key material appear here or in the repo.*

- **Kalshi trading credentials: NONE.** `client.Credentials` implements RSA-PSS
  request signing and is fully unused — nothing constructs it, nothing calls it.
  No API key is configured anywhere in this session.
- **`CIPHER_CONTACT`** — environment variable for the NWS contact address.
  Not set.
- **Git push access** to `Mussstarrd/Cipher`, via the session's git proxy,
  scoped to branch `claude/kalshi-winner-scanner-j572xn`.
- **MCP servers attached to this session** (named for completeness; none were
  used by this project): `github`, `Gmail`, `Google_Calendar`, `Google_Drive`,
  `Robinson_Trading`, `Claude_Code_Remote`.

Worth flagging explicitly for the next shift: **`Robinson_Trading` is a live
brokerage connection available in this session.** This project never touched it,
placed no orders through it, and has no reason to — but anyone inheriting the
session should know it is there before running anything broad.

`.gitignore` excludes `*.pem`, `.env`, and `data/`, so key material and journal
contents cannot be committed by accident.

## 6. Open questions

**Blocking, must be resolved by a human before any real money moves:**

1. **Station mappings are unverified.** All seven entries in `stations.py` ship
   `verified=False` and are best-effort. Central Park vs. LaGuardia is 2–4°F —
   one to two whole brackets. Someone must read each series' rulebook and
   confirm both the station identifier and the settlement product.
2. **Settlement product unconfirmed.** The resolver assumes the NWS Daily
   Climate Report (CLI). If a series settles on something else, the monotonicity
   argument may not hold in the same form.
3. **Series tickers unverified.** `KXHIGHNY`, `KXHIGHCHI` etc. are guesses.
4. **Fee parameters unverified.** Coefficient 0.07 and maker-fee 0 are defaults;
   they should be checked against the live schedule.

**Substantive, affects whether the edge is real:**

5. **`probability_rises_by` is a hand-built prior, not a fitted model.** It is
   transparent and auditable, which is why it was chosen — but replacing it with
   per-station empirics from a season of observations is the highest-value
   improvement available.
6. **`BOUNDARY_MARGIN_F = 0.35` has no empirical grounding.** It was reasoned
   from "ASOS reports whole °F natively and the API round-trips through
   Celsius." Should be measured against actual published climate reports.
7. **Are Kalshi's weather brackets genuinely exhaustive?** `scan_partition_sum`
   depends on the exchange's `mutually_exclusive` flag meaning what it appears
   to mean.
8. **Capacity is unknown.** Books are thin; a strategy that works at $20 may not
   exist at $2,000.
9. **Does the edge exist at all?** Zero signals have been journalled, zero have
   settled. `python -m cipher power --model 0.995 --market 0.92` says 36 settled
   signals at a 92¢ book price to distinguish edge from luck at 80% power —
   157 at a 97¢ price. Nothing is known until then.

**Operational:**

10. **Where should this actually run?** This container is ephemeral and the
    scanner needs to run nightly for weeks. Allowlisting the hosts here buys
    exploration; the real deployment belongs on a machine the owner controls.

---

*Not investment advice. The parts of this system that can lose money are the
parts that have not been built.*
