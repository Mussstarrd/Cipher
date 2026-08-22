# Putting $20 on it

This is the operational guide for the weather resolver. Read
[DESIGN.md](DESIGN.md) first for why this particular market family and not
another.

**What $20 is actually for.** It is not a test of the strategy — one trade
cannot be. At a 92c book price, a single trade wins 92% of the time *even if the
model is worthless*, so a win tells you almost nothing. What $20 buys is a test
of the **plumbing**: that the station mapping is right, that the settlement
source says what the code thinks it says, that the fill happens at the price the
ticket assumed, and that the market resolves the way the observation implied.
Those are the things that silently break, and they break identically at $20 and
at $2,000. Find out cheaply.

## Step 0 — verify the station. Non-negotiable.

The code ships with every station **unverified**, and refuses to make a
confident claim until you fix that. This is not ceremony. Central Park and
LaGuardia routinely differ by 2–4°F, which is one to two whole brackets — being
exactly right about the weather and wrong about the station is a losing trade.

1. Open the Kalshi market you intend to trade and read its rules.
2. Find the exact station identifier and the exact settlement product (it is
   usually the NWS Daily Climate Report, "CLI", for a named station).
3. Confirm it:

```bash
python -m cipher weather --series KXHIGHNY --verify KXHIGHNY:KNYC
```

If the registry disagrees with what you read, it errors instead of guessing.
Believe the rulebook, not `stations.py`.

## Step 0.5 — identify yourself to the NWS

The NWS API asks callers to supply a contact address and may block traffic that
does not. The code refuses to send an anonymous request rather than getting your
IP quietly throttled mid-run:

```bash
export CIPHER_CONTACT="you@example.com"
```

## Step 1 — run it in the late afternoon or evening, local time

```bash
python -m cipher weather --series KXHIGHNY --verify KXHIGHNY:KNYC \
    --stake 20 --journal data/journal.jsonl
```

The resolver stays silent before the afternoon peak, on purpose — the day has
most of its heating left and there is no observational edge to have. The window
that matters is roughly 5pm to 11pm local: the max is effectively locked, the
market has not fully converged, and the contract settles that night.

It prints one line per market explaining itself, then an order ticket for
anything worth trading. If it says "no signals", the per-market log tells you
which of the legitimate reasons applied — too early, no observations, sitting on
a rounding boundary, or the book already agrees.

## Step 2 — read the ticket honestly

```
ORDER TICKET
  market      : KXHIGHNY-25AUG15-B76
  side        : buy NO
  limit price : 92c  (do not chase above this)
  quantity    : 21 contracts

  outlay      : $19.43  ($19.32 + $0.11 fee)
  if right    : +$1.57
  if wrong    : -$19.43
  one loss erases 12 wins
```

That last line is the whole strategy in one number. You are risking $19.43 to
make $1.57. That is fine **only** if the claim is genuinely near-certain, which
is why the resolver only marks a claim deterministic in the one direction where
the inequality holds: the observed maximum is already above the bracket's
ceiling, and a day's maximum cannot fall.

## Step 3 — place it by hand

There is no execution code, deliberately. At $20 the automation is not worth its
own risk of bugs, and placing it manually forces you to look at the market once
before committing.

1. Open the market in the Kalshi app or site.
2. Place a **limit** order at the ticket's price. Do not chase — if it has moved
   against you, the edge the scanner measured no longer exists.
3. If it only partially fills, that is fine here (unlike the arbitrage scanners,
   this is a single-leg position).

## Step 4 — record it

The journal is what makes any of this measurable. Mark the trade as actually
taken, and record the settlement once the climate report is out:

```python
from cipher.journal import Journal
journal = Journal("data/journal.jsonl")
journal.record_outcome("<signal_id>", settled_yes=False, fill_price_cents=92)
```

`signal_id` is printed in the journal file for each recorded signal. Then:

```bash
python -m cipher calibrate
```

## When you will know

**That one trade: the same night.** These markets settle on the NWS daily
climate report for the station, published after local midnight. So you know
within hours — but what you learn is whether the *pipeline* worked, not whether
the edge is real.

**Whether the edge is real: dozens of trades.** Run the numbers for your own
prices:

```bash
python -m cipher power --model 0.995 --market 0.92
```

For the trade above, that returns:

```
  run 36 settled signals at ~92% book price
  conclude the edge is real only if losses <= 0

  if the model is right (99.5%): expect 0.2 losses
  if the book is right  (92.0%): expect 2.9 losses

  chance of fooling yourself (book right, rule passes): 5.0%
  chance of detecting a real edge: 83.5%
```

**Commit to that rule before you start.** Thirty-six settled signals, and if you
take more than zero losses the model is not what it claims. Deciding the cutoff
afterwards is how everyone talks themselves into a strategy that does not work.

Note how much harsher it gets on more expensive contracts — at a 97c book price
the same model needs **157** settled signals. That is the real cost of trading
near-certainties: the closer the book is to right, the longer it takes to prove
you are righter.

At a handful of qualifying markets per day across the seven mapped cities,
36 signals is one to three weeks of evenings. That is the honest timeline.

## What would make me stop

- Any loss on a signal marked `deterministic`. That is not variance — it means
  the station mapping, the settlement product, or the rounding assumption is
  wrong, and the whole premise needs re-checking before another trade.
- `python -m cipher calibrate` showing a model Brier score that does not beat
  the market Brier score after 30+ settled signals.
- Fills consistently worse than the ticket price, which means the edge was
  already gone by the time the scanner saw it.

## Standing caveats

Kalshi is a real-money exchange and this is real money. The rise model in
`probability_rises_by` is a transparent prior, not a fitted curve — it is the
first thing that should be replaced with per-station empirics once the journal
has a season of data. Fee parameters are defaults; check them against the live
schedule. None of this is investment advice.
