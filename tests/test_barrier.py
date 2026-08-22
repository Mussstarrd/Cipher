import math
import unittest

from cipher.resolvers.barrier import (
    BarrierInputs,
    implied_move_to_flip,
    realised_volatility,
    terminal_probability,
    touch_probability,
)

MINUTES_PER_YEAR = 365 * 24 * 60


def inputs(spot=100.0, strike=100.0, seconds=300.0, vol=0.5, floor=0.0):
    return BarrierInputs(
        spot=spot, strike=strike, seconds_to_expiry=seconds,
        annual_volatility=vol, vol_floor=floor,
    )


class TestTerminalProbability(unittest.TestCase):
    def test_at_the_money_is_a_coin_flip(self):
        self.assertAlmostEqual(terminal_probability(inputs()), 0.5, places=2)

    def test_decreasing_in_strike(self):
        probabilities = [terminal_probability(inputs(strike=k)) for k in (95, 98, 100, 102, 105)]
        self.assertEqual(probabilities, sorted(probabilities, reverse=True))

    def test_converges_to_certainty_as_time_runs_out(self):
        far = terminal_probability(inputs(spot=101, seconds=3600))
        near = terminal_probability(inputs(spot=101, seconds=10))
        self.assertGreater(near, far)
        self.assertGreater(near, 0.99)

    def test_expired_is_degenerate(self):
        self.assertEqual(terminal_probability(inputs(spot=101, seconds=0)), 1.0)
        self.assertEqual(terminal_probability(inputs(spot=99, seconds=0)), 0.0)

    def test_higher_vol_pulls_toward_a_coin_flip(self):
        calm = terminal_probability(inputs(spot=102, vol=0.2))
        wild = terminal_probability(inputs(spot=102, vol=2.0))
        self.assertGreater(calm, wild)
        self.assertGreater(wild, 0.5)

    def test_vol_floor_prevents_manufactured_certainty(self):
        """A quiet window must not convince the model that nothing can happen."""
        unfloored = terminal_probability(
            BarrierInputs(spot=101, strike=100, seconds_to_expiry=600,
                          annual_volatility=0.001, vol_floor=0.0)
        )
        floored = terminal_probability(
            BarrierInputs(spot=101, strike=100, seconds_to_expiry=600,
                          annual_volatility=0.001, vol_floor=0.60)
        )
        self.assertGreater(unfloored, 0.999)
        self.assertLess(floored, unfloored)

    def test_rejects_nonpositive_prices(self):
        with self.assertRaises(ValueError):
            terminal_probability(inputs(spot=0))


class TestTouchProbability(unittest.TestCase):
    def test_touch_is_roughly_double_terminal_out_of_the_money(self):
        """The reflection principle. Confusing the two rules is a costly bug."""
        args = inputs(spot=99, strike=100, seconds=600)
        self.assertAlmostEqual(
            touch_probability(args), 2 * terminal_probability(args), delta=0.05
        )

    def test_already_through_the_barrier_is_certain(self):
        self.assertEqual(touch_probability(inputs(spot=101, strike=100)), 1.0)

    def test_never_exceeds_one(self):
        self.assertLessEqual(touch_probability(inputs(spot=99.999, strike=100)), 1.0)


class TestRealisedVolatility(unittest.TestCase):
    def test_flat_series_has_zero_volatility(self):
        self.assertAlmostEqual(realised_volatility([100.0] * 10, MINUTES_PER_YEAR), 0.0)

    def test_scales_with_dispersion(self):
        calm = realised_volatility([100, 100.01, 99.99, 100.02, 100.0], MINUTES_PER_YEAR)
        wild = realised_volatility([100, 101, 99, 102, 100], MINUTES_PER_YEAR)
        self.assertGreater(wild, calm)

    def test_annualisation_matches_the_sampling_rate(self):
        prices = [100, 100.5, 100.2, 100.8, 100.4]
        per_minute = realised_volatility(prices, MINUTES_PER_YEAR)
        per_hour = realised_volatility(prices, 365 * 24)
        self.assertAlmostEqual(per_minute / per_hour, math.sqrt(60), places=6)

    def test_rejects_degenerate_input(self):
        with self.assertRaises(ValueError):
            realised_volatility([100, 101], MINUTES_PER_YEAR)
        with self.assertRaises(ValueError):
            realised_volatility([100, 0, 101], MINUTES_PER_YEAR)


class TestImpliedMove(unittest.TestCase):
    def test_reports_distance_as_a_fraction_of_spot(self):
        self.assertAlmostEqual(implied_move_to_flip(inputs(spot=100, strike=101)), 0.01)


if __name__ == "__main__":
    unittest.main()
