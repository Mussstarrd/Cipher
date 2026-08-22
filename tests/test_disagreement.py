import unittest
from datetime import timedelta

from cipher.model import Market, Quote, utcnow
from cipher.resolvers.base import Estimate
from cipher.scanners.disagreement import DisagreementConfig, scan
from cipher.signal import Kind


def market(yes_ask=50, no_ask=52) -> Market:
    return Market(
        ticker="KXTEST-1", event_ticker="KXTEST", title="test",
        close_time=utcnow() + timedelta(minutes=5),
        quote=Quote(yes_bid=yes_ask - 2, yes_ask=yes_ask, no_bid=no_ask - 2, no_ask=no_ask),
    )


def estimate(probability=0.9, confidence=1.0, age_seconds=0.0, deterministic=False) -> Estimate:
    return Estimate(
        probability=probability,
        confidence=confidence,
        source="test-source",
        observed_at=utcnow() - timedelta(seconds=age_seconds),
        rationale="fixture",
        deterministic=deterministic,
    )


class TestDisagreement(unittest.TestCase):
    def test_fires_when_the_source_disagrees_with_the_book(self):
        signals = scan(market(yes_ask=50), estimate(probability=0.90))
        self.assertEqual(len(signals), 1)
        self.assertEqual(signals[0].side.value, "yes")
        self.assertGreater(signals[0].edge_cents, 0)

    def test_takes_the_no_side_when_the_source_says_no(self):
        signals = scan(market(yes_ask=60, no_ask=42), estimate(probability=0.10))
        self.assertEqual(len(signals), 1)
        self.assertEqual(signals[0].side.value, "no")

    def test_silent_when_the_book_already_agrees(self):
        self.assertEqual(scan(market(yes_ask=90), estimate(probability=0.90)), [])

    def test_stale_observations_are_discarded(self):
        """Near expiry a stale source points the wrong way, not merely a worse way."""
        config = DisagreementConfig(max_staleness_seconds=20)
        fresh = scan(market(yes_ask=50), estimate(probability=0.90, age_seconds=1), config)
        stale = scan(market(yes_ask=50), estimate(probability=0.90, age_seconds=120), config)
        self.assertEqual(len(fresh), 1)
        self.assertEqual(stale, [])

    def test_low_confidence_estimates_are_dropped(self):
        config = DisagreementConfig(min_confidence=0.6)
        self.assertEqual(scan(market(), estimate(probability=0.9, confidence=0.3), config), [])

    def test_confidence_haircut_shrinks_toward_the_book(self):
        confident = scan(market(yes_ask=50), estimate(probability=0.95, confidence=1.0))
        unsure = scan(market(yes_ask=50), estimate(probability=0.95, confidence=0.65))
        self.assertLess(unsure[0].probability, confident[0].probability)
        self.assertGreater(unsure[0].probability, 0.50)

    def test_probability_never_reaches_certainty(self):
        signals = scan(market(yes_ask=50), estimate(probability=1.0))
        self.assertLess(signals[0].probability, 1.0)
        self.assertLess(signals[0].kelly, 1.0)

    def test_crossed_book_emits_nothing(self):
        """Both sides firing means the data is wrong, not that both are winners."""
        crossed = Market(
            ticker="KXTEST-1", event_ticker="KXTEST", title="test",
            close_time=utcnow() + timedelta(minutes=5),
            quote=Quote(yes_ask=20, no_ask=20),
        )
        self.assertEqual(scan(crossed, estimate(probability=0.5, confidence=1.0)), [])

    def test_edge_floor_is_respected(self):
        strict = DisagreementConfig(min_edge_cents=20)
        self.assertEqual(scan(market(yes_ask=50), estimate(probability=0.56), strict), [])

    def test_lottery_ticket_prices_are_skipped(self):
        config = DisagreementConfig(min_price_cents=5)
        self.assertEqual(scan(market(yes_ask=2), estimate(probability=0.9), config), [])

    def test_deterministic_sources_are_marked_more_trustworthy(self):
        modelled = scan(market(yes_ask=50), estimate(probability=0.9))
        known = scan(market(yes_ask=50), estimate(probability=0.9, deterministic=True))
        self.assertIs(modelled[0].kind, Kind.MODEL)
        self.assertIs(known[0].kind, Kind.DETERMINISTIC)


if __name__ == "__main__":
    unittest.main()
