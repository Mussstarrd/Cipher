import tempfile
import unittest
from pathlib import Path

from cipher.journal import Journal, brier, calibration_table, join, summarise
from cipher.signal import Kind, Side, Signal


def signal(ticker="KXTEST-1", price=90, probability=0.95, scanner="test") -> Signal:
    return Signal(
        ticker=ticker, side=Side.YES, kind=Kind.MODEL, scanner=scanner,
        price_cents=price, probability=probability, contracts=100, rationale="fixture",
    )


class TestJournalRoundTrip(unittest.TestCase):
    def setUp(self):
        self.dir = tempfile.TemporaryDirectory()
        self.journal = Journal(Path(self.dir.name) / "journal.jsonl")

    def tearDown(self):
        self.dir.cleanup()

    def test_signal_then_outcome_joins(self):
        signal_id = self.journal.record(signal(), traded=True)
        self.journal.record_outcome(signal_id, settled_yes=True)
        scored = join(self.journal.read())
        self.assertEqual(len(scored), 1)
        self.assertTrue(scored[0].won)
        self.assertTrue(scored[0].traded)

    def test_no_side_wins_when_the_market_settles_no(self):
        s = signal()
        no_side = Signal(
            ticker=s.ticker, side=Side.NO, kind=s.kind, scanner=s.scanner,
            price_cents=s.price_cents, probability=s.probability,
            contracts=s.contracts, rationale=s.rationale,
        )
        signal_id = self.journal.record(no_side)
        self.journal.record_outcome(signal_id, settled_yes=False)
        self.assertTrue(join(self.journal.read())[0].won)

    def test_unsettled_signals_are_not_scored(self):
        self.journal.record(signal())
        self.assertEqual(join(self.journal.read()), [])
        self.assertEqual(summarise(self.journal.read())["settled"], 0)

    def test_missing_file_reads_as_empty(self):
        self.assertEqual(Journal(Path(self.dir.name) / "absent.jsonl").read(), [])

    def test_appends_rather_than_truncates(self):
        for i in range(3):
            self.journal.record(signal(ticker=f"KXTEST-{i}"))
        self.assertEqual(len(self.journal.read()), 3)


class TestScoring(unittest.TestCase):
    def setUp(self):
        self.dir = tempfile.TemporaryDirectory()
        self.journal = Journal(Path(self.dir.name) / "j.jsonl")

    def tearDown(self):
        self.dir.cleanup()

    def _log(self, probability, price, settled_yes, ticker):
        signal_id = self.journal.record(
            signal(ticker=ticker, price=price, probability=probability), traded=True
        )
        self.journal.record_outcome(signal_id, settled_yes=settled_yes)

    def test_perfect_forecaster_scores_zero(self):
        self._log(1.0, 50, True, "A")
        self._log(0.0, 50, False, "B")
        self.assertAlmostEqual(brier(join(self.journal.read())), 0.0)

    def test_coin_flipper_scores_a_quarter(self):
        self._log(0.5, 50, True, "A")
        self._log(0.5, 50, False, "B")
        self.assertAlmostEqual(brier(join(self.journal.read())), 0.25)

    def test_market_benchmark_uses_the_price_at_signal_time(self):
        """The bar is not 'better than a coin' -- it is 'better than the book'."""
        self._log(0.95, 60, True, "A")
        scored = join(self.journal.read())
        self.assertAlmostEqual(brier(scored), (0.95 - 1) ** 2)
        self.assertAlmostEqual(brier(scored, use_market=True), (0.60 - 1) ** 2)

    def test_calibration_buckets_predicted_against_realised(self):
        for i in range(10):
            self._log(0.95, 90, i < 5, f"T{i}")
        table = calibration_table(join(self.journal.read()))
        row = next(r for r in table if r["n"] == 10)
        self.assertAlmostEqual(row["predicted"], 0.95)
        self.assertAlmostEqual(row["realised"], 0.5, msg="badly overconfident")

    def test_pnl_counts_fees_and_only_traded_signals(self):
        self._log(0.95, 90, True, "A")  # +10c x100, minus fee
        summary = summarise(self.journal.read())
        self.assertLess(summary["pnl_cents"], 1000)
        self.assertGreater(summary["pnl_cents"], 900)

    def test_brier_of_nothing_is_none(self):
        self.assertIsNone(brier([]))

    def test_summary_breaks_down_by_scanner(self):
        a = self.journal.record(signal(scanner="alpha"), traded=True)
        b = self.journal.record(signal(ticker="KXTEST-2", scanner="beta"), traded=True)
        self.journal.record_outcome(a, settled_yes=True)
        self.journal.record_outcome(b, settled_yes=False)
        by_scanner = summarise(self.journal.read())["by_scanner"]
        self.assertEqual(set(by_scanner), {"alpha", "beta"})
        self.assertEqual(by_scanner["alpha"]["hit_rate"], 1.0)
        self.assertEqual(by_scanner["beta"]["hit_rate"], 0.0)


if __name__ == "__main__":
    unittest.main()
