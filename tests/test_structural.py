import json
import unittest
from datetime import timedelta
from pathlib import Path

from cipher.client import parse_event
from cipher.model import Event, Market, Quote, utcnow
from cipher.scanners import structural
from cipher.signal import Kind

FIXTURE = Path(__file__).parent / "fixtures" / "events.json"


def load_events():
    raw = json.loads(FIXTURE.read_text())
    return {e["event_ticker"]: parse_event(e) for e in raw["events"]}


def market(ticker="T-1", event="E-1", **quote_kwargs) -> Market:
    return Market(
        ticker=ticker,
        event_ticker=event,
        title=ticker,
        close_time=utcnow() + timedelta(hours=1),
        quote=Quote(**quote_kwargs),
    )


class TestYesNoCross(unittest.TestCase):
    def test_fires_on_a_gap_that_clears_fees(self):
        m = market(yes_ask=8, no_ask=88, yes_ask_size=250, no_ask_size=250)
        signals = structural.scan_yes_no_cross(m)
        self.assertEqual(len(signals), 2, "both legs must be emitted together")
        self.assertEqual({s.side.value for s in signals}, {"yes", "no"})
        self.assertTrue(all(s.kind is Kind.ARBITRAGE for s in signals))
        self.assertGreater(signals[0].locked_profit_cents, 0)

    def test_legs_sum_to_the_basket_profit(self):
        m = market(yes_ask=8, no_ask=88, yes_ask_size=250, no_ask_size=250)
        signals = structural.scan_yes_no_cross(m)
        total = sum(s.expected_value_cents for s in signals)
        self.assertAlmostEqual(total, signals[0].locked_profit_cents, places=6)

    def test_silent_when_the_gap_does_not_clear_fees(self):
        """A 3c gap at mid prices is eaten by the fee, which peaks at 50c."""
        m = market(yes_ask=45, no_ask=52, yes_ask_size=250, no_ask_size=250)
        self.assertEqual(structural.scan_yes_no_cross(m), [])

    def test_silent_on_a_normal_book(self):
        m = market(yes_ask=61, no_ask=42, yes_ask_size=500, no_ask_size=500)
        self.assertEqual(structural.scan_yes_no_cross(m), [])

    def test_silent_when_a_side_is_unquoted(self):
        self.assertEqual(structural.scan_yes_no_cross(market(yes_ask=8)), [])

    def test_size_is_the_thinnest_leg(self):
        m = market(yes_ask=8, no_ask=88, yes_ask_size=300, no_ask_size=90)
        signals = structural.scan_yes_no_cross(m)
        self.assertTrue(all(s.contracts == 90 for s in signals))


class TestPartitionSum(unittest.TestCase):
    def setUp(self):
        self.events = load_events()

    def test_fires_on_brackets_summing_below_100(self):
        signals = structural.scan_partition_sum(self.events["DEMO-BRACKETS"])
        self.assertEqual(len(signals), 3, "one leg per bracket")
        self.assertTrue(all(s.side.value == "yes" for s in signals))
        self.assertAlmostEqual(
            sum(s.expected_value_cents for s in signals),
            signals[0].locked_profit_cents,
            places=6,
        )

    def test_silent_on_a_fairly_priced_partition(self):
        self.assertEqual(structural.scan_partition_sum(self.events["DEMO-CLEAN"]), [])

    def test_requires_the_exclusivity_flag(self):
        """Summing brackets that are not exhaustive is meaningless."""
        exclusive = self.events["DEMO-BRACKETS"]
        overlapping = Event(
            event_ticker=exclusive.event_ticker,
            title=exclusive.title,
            markets=exclusive.markets,
            mutually_exclusive=False,
        )
        self.assertEqual(structural.scan_partition_sum(overlapping), [])


class TestLadderMonotonicity(unittest.TestCase):
    def setUp(self):
        self.events = load_events()

    def test_fires_when_a_higher_strike_is_cheaper_to_buy_than_a_lower_is_to_sell(self):
        signals = structural.scan_ladder_monotonicity(self.events["DEMO-LADDER"])
        self.assertEqual(len(signals), 2)
        buy = next(s for s in signals if s.extra["leg"] == "buy")
        sell = next(s for s in signals if s.extra["leg"] == "sell")
        self.assertEqual(buy.ticker, "DEMO-LADDER-T110", "buy the higher strike")
        self.assertEqual(sell.ticker, "DEMO-LADDER-T100", "sell the lower strike")

    def test_silent_on_a_correctly_ordered_ladder(self):
        ordered = Event(
            event_ticker="E",
            title="ordered",
            mutually_exclusive=False,
            markets=(
                Market(
                    ticker="E-T100", event_ticker="E", title="above 100",
                    close_time=utcnow() + timedelta(hours=1),
                    quote=Quote(yes_bid=62, yes_ask=65, yes_bid_size=200, yes_ask_size=200),
                    strike_type="greater", floor_strike=100,
                ),
                Market(
                    ticker="E-T110", event_ticker="E", title="above 110",
                    close_time=utcnow() + timedelta(hours=1),
                    quote=Quote(yes_bid=68, yes_ask=71, yes_bid_size=200, yes_ask_size=200),
                    strike_type="greater", floor_strike=110,
                ),
            ),
        )
        # Higher strike is dearer than the lower one's bid: no violation to trade.
        self.assertEqual(structural.scan_ladder_monotonicity(ordered), [])

    def test_ignores_non_ladder_markets(self):
        self.assertEqual(structural.scan_ladder_monotonicity(self.events["DEMO-CLEAN"]), [])


class TestScanEvent(unittest.TestCase):
    def test_control_event_is_quiet(self):
        """The most important test here: no false positives on a sane book."""
        self.assertEqual(structural.scan_event(load_events()["DEMO-CLEAN"]), [])

    def test_every_fixture_family_is_detected(self):
        events = load_events()
        for ticker in ("DEMO-YESNO", "DEMO-BRACKETS", "DEMO-LADDER"):
            self.assertTrue(structural.scan_event(events[ticker]), ticker)


if __name__ == "__main__":
    unittest.main()
