import unittest

from cipher.signal import Kind, Side, Signal
from cipher.ticket import size_to_stake


def signal(price=95, probability=0.99, contracts=1000) -> Signal:
    return Signal(
        ticker="KXHIGHNY-25AUG15-B72", side=Side.NO, kind=Kind.DETERMINISTIC,
        scanner="test", price_cents=price, probability=probability,
        contracts=contracts, rationale="fixture", invalidated_if="fixture",
    )


class TestSizing(unittest.TestCase):
    def test_outlay_never_exceeds_the_stake(self):
        for stake in (500, 1000, 2000, 3333, 10000):
            for price in (5, 37, 90, 95, 99):
                ticket = size_to_stake(signal(price=price), stake)
                if ticket is not None:
                    self.assertLessEqual(ticket.outlay_cents, stake, f"{stake}@{price}")

    def test_fee_is_included_in_the_budget(self):
        """stake // price alone overshoots once the fee lands."""
        ticket = size_to_stake(signal(price=95), 2000)
        self.assertLess(ticket.contracts, 2000 // 95 + 1)
        self.assertEqual(
            ticket.outlay_cents, ticket.contracts * 95 + ticket.fee_cents
        )

    def test_capped_by_available_depth(self):
        ticket = size_to_stake(signal(price=10, contracts=7), 10000)
        self.assertEqual(ticket.contracts, 7)

    def test_stake_too_small_for_one_contract(self):
        self.assertIsNone(size_to_stake(signal(price=95), 50))

    def test_zero_stake(self):
        self.assertIsNone(size_to_stake(signal(), 0))


class TestTicketArithmetic(unittest.TestCase):
    def test_win_and_loss_sides_are_consistent(self):
        ticket = size_to_stake(signal(price=95), 2000)
        self.assertEqual(
            ticket.profit_if_right_cents + ticket.outlay_cents, ticket.contracts * 100
        )
        self.assertEqual(ticket.loss_if_wrong_cents, ticket.outlay_cents)

    def test_expensive_contracts_have_brutal_loss_ratios(self):
        cheap = size_to_stake(signal(price=50), 2000)
        dear = size_to_stake(signal(price=95), 2000)
        self.assertLess(cheap.losses_erased_by_one_loss, 2)
        self.assertGreater(dear.losses_erased_by_one_loss, 15)

    def test_breakeven_exceeds_the_price(self):
        ticket = size_to_stake(signal(price=95), 2000)
        self.assertGreater(ticket.breakeven_probability, 0.95)

    def test_expected_value_turns_negative_below_breakeven(self):
        good = size_to_stake(signal(price=95, probability=0.99), 2000)
        bad = size_to_stake(signal(price=95, probability=0.90), 2000)
        self.assertGreater(good.expected_value_cents, 0)
        self.assertLess(bad.expected_value_cents, 0)

    def test_render_states_both_sides(self):
        text = size_to_stake(signal(price=95), 2000).render()
        self.assertIn("if right", text)
        self.assertIn("if wrong", text)
        self.assertIn("erases", text)


if __name__ == "__main__":
    unittest.main()
