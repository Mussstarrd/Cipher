import unittest

from cipher.fees import (
    DEFAULT_SCHEDULE,
    FeeSchedule,
    breakeven_probability,
    expected_value_cents,
    kelly_fraction,
    schedule_for,
)


class TestFeeShape(unittest.TestCase):
    def test_fee_peaks_at_the_middle(self):
        """The fee curve is c*n*P*(1-P): most expensive on coin flips."""
        big = 1000
        fees = {p: DEFAULT_SCHEDULE.taker_fee_cents(p, big) for p in (5, 25, 50, 75, 95)}
        self.assertEqual(max(fees, key=fees.get), 50)
        self.assertAlmostEqual(fees[25], fees[75], delta=1)
        self.assertAlmostEqual(fees[5], fees[95], delta=1)

    def test_fee_rounds_up_to_a_whole_cent(self):
        # 0.07 * 1 * 0.95 * 0.05 = $0.0033, which Kalshi bills as a full cent.
        self.assertEqual(DEFAULT_SCHEDULE.taker_fee_cents(95, 1), 1)
        # The same order 500x is billed proportionally, not 500 cents:
        # 0.07 * 500 * 0.95 * 0.05 = $1.6625 -> 167c, i.e. 0.334c/contract.
        self.assertEqual(DEFAULT_SCHEDULE.taker_fee_cents(95, 500), 167)

    def test_maker_is_free_by_default(self):
        self.assertEqual(DEFAULT_SCHEDULE.maker_fee_cents(50, 100), 0)
        paid = FeeSchedule(maker_coefficient=0.0025)
        self.assertGreater(paid.maker_fee_cents(50, 1000), 0)

    def test_invalid_price_rejected(self):
        with self.assertRaises(ValueError):
            DEFAULT_SCHEDULE.taker_fee_cents(101)
        with self.assertRaises(ValueError):
            DEFAULT_SCHEDULE.taker_fee_cents(-1)


class TestBreakeven(unittest.TestCase):
    def test_breakeven_exceeds_price(self):
        for price in (10, 50, 90, 97):
            self.assertGreater(breakeven_probability(price, 100), price / 100)

    def test_larger_orders_have_lower_breakevens(self):
        """Per-order cent rounding punishes small orders."""
        self.assertGreater(breakeven_probability(97, 1), breakeven_probability(97, 500))

    def test_single_contract_at_99c_can_never_pay(self):
        self.assertGreaterEqual(breakeven_probability(99, 1), 1.0)


class TestExpectedValue(unittest.TestCase):
    def test_fair_price_is_negative_after_fees(self):
        """Buying at exactly your own probability loses the fee. Always."""
        self.assertLess(expected_value_cents(0.60, 60, 100), 0)

    def test_edge_must_exceed_fees_to_be_positive(self):
        # Breakeven at 95c on 500 contracts is ~95.33%.
        self.assertLess(expected_value_cents(0.952, 95, 500), 0)
        self.assertGreater(expected_value_cents(0.955, 95, 500), 0)

    def test_scales_with_contracts(self):
        one = expected_value_cents(0.90, 80, 1)
        many = expected_value_cents(0.90, 80, 100)
        self.assertGreater(many, one * 50)


class TestKelly(unittest.TestCase):
    def test_no_edge_means_no_bet(self):
        self.assertEqual(kelly_fraction(0.50, 50, 100), 0.0)
        self.assertEqual(kelly_fraction(0.30, 60, 100), 0.0)

    def test_bounded_to_unit_interval(self):
        for probability in (0.0, 0.5, 0.999, 1.0):
            self.assertGreaterEqual(kelly_fraction(probability, 50, 100), 0.0)
            self.assertLessEqual(kelly_fraction(probability, 50, 100), 1.0)

    def test_grows_with_edge(self):
        small = kelly_fraction(0.60, 55, 100)
        large = kelly_fraction(0.90, 55, 100)
        self.assertLess(small, large)


class TestScheduleLookup(unittest.TestCase):
    def test_unknown_series_gets_the_default(self):
        self.assertIs(schedule_for("KXBTCD-25AUG2217-T113999.99"), DEFAULT_SCHEDULE)


if __name__ == "__main__":
    unittest.main()
