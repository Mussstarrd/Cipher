import unittest

from cipher.power import _binomial_cdf, decision_rule, trades_needed


class TestBinomial(unittest.TestCase):
    def test_cdf_edges(self):
        self.assertEqual(_binomial_cdf(-1, 10, 0.5), 0.0)
        self.assertEqual(_binomial_cdf(10, 10, 0.5), 1.0)
        self.assertAlmostEqual(_binomial_cdf(0, 10, 0.5), 0.5**10)

    def test_cdf_is_monotone(self):
        values = [_binomial_cdf(k, 20, 0.3) for k in range(21)]
        self.assertEqual(values, sorted(values))


class TestDecisionRule(unittest.TestCase):
    def test_false_positive_rate_respects_alpha(self):
        for trades in (20, 50, 100, 200):
            plan = decision_rule(trades, 0.995, 0.92, alpha=0.05)
            self.assertLessEqual(plan.false_positive_rate, 0.05)

    def test_more_trades_give_more_power(self):
        small = decision_rule(30, 0.99, 0.92)
        large = decision_rule(150, 0.99, 0.92)
        self.assertGreater(large.power, small.power)

    def test_tiny_samples_cannot_conclude_anything(self):
        """With 2 trades there is no loss count that rejects the null at 5%."""
        self.assertEqual(decision_rule(2, 0.995, 0.92).max_losses, -1)

    def test_rejects_a_model_that_does_not_beat_the_book(self):
        with self.assertRaises(ValueError):
            decision_rule(50, 0.90, 0.92)


class TestTradesNeeded(unittest.TestCase):
    def test_returns_a_workable_plan(self):
        plan = trades_needed(0.995, 0.92)
        self.assertIsNotNone(plan)
        self.assertGreater(plan.trades, 10)
        self.assertGreaterEqual(plan.power, 0.8)

    def test_a_smaller_edge_needs_more_trades(self):
        wide = trades_needed(0.995, 0.92)
        narrow = trades_needed(0.99, 0.97)
        self.assertGreater(narrow.trades, wide.trades)

    def test_expected_losses_are_reported_both_ways(self):
        plan = trades_needed(0.995, 0.92)
        self.assertLess(plan.expected_losses_if_model_right, plan.expected_losses_if_market_right)


if __name__ == "__main__":
    unittest.main()
