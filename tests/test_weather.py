import unittest
from datetime import datetime, timedelta, timezone
from zoneinfo import ZoneInfo

from cipher.model import Market, Quote
from cipher.resolvers import stations
from cipher.resolvers.weather import (
    BOUNDARY_MARGIN_F,
    Bracket,
    Observation,
    WeatherResolver,
    bracket_for,
    climatological_day_start,
    hours_past_peak,
    observed_max,
    parse_observations,
    probability_of_bracket,
    probability_rises_by,
    to_fahrenheit,
)
from cipher.resolvers.stations import Station

NY = ZoneInfo("America/New_York")


def at(local_hour, minute=0, day=15, month=8):
    """A UTC instant corresponding to a New York wall-clock time."""
    return datetime(2025, month, day, local_hour, minute, tzinfo=NY).astimezone(timezone.utc)


def market(ticker="KXHIGHNY-25AUG15-B72", strike_type="between", floor=72, cap=73) -> Market:
    return Market(
        ticker=ticker, event_ticker="KXHIGHNY-25AUG15", title="NYC high",
        close_time=at(23, 59), quote=Quote(yes_bid=50, yes_ask=52, no_bid=48, no_ask=50),
        strike_type=strike_type, floor_strike=floor, cap_strike=cap,
    )


class StationIsolation:
    """Restore the station registry after each test.

    ``stations.verify`` deliberately mutates module-level state -- it is
    configuration, set once at startup -- so anything that calls it has to put
    the registry back or it leaks into unrelated tests.
    """

    def setUp(self):
        super().setUp()
        self._saved_stations = dict(stations.STATIONS)

    def tearDown(self):
        stations.STATIONS.clear()
        stations.STATIONS.update(self._saved_stations)
        super().tearDown()


class TestUnits(unittest.TestCase):
    def test_celsius_to_fahrenheit(self):
        self.assertAlmostEqual(to_fahrenheit(0), 32.0)
        self.assertAlmostEqual(to_fahrenheit(22.2), 71.96, places=2)

    def test_parse_skips_missing_temperatures_and_sorts(self):
        payload = {
            "features": [
                {"properties": {"timestamp": "2025-08-15T18:00:00Z",
                                "temperature": {"value": 24.0, "qualityControl": "V"}}},
                {"properties": {"timestamp": "2025-08-15T17:00:00Z",
                                "temperature": {"value": None}}},
                {"properties": {"timestamp": "2025-08-15T16:00:00Z",
                                "temperature": {"value": 22.0, "qualityControl": "V"}}},
            ]
        }
        parsed = parse_observations(payload)
        self.assertEqual(len(parsed), 2)
        self.assertEqual([o.observed_at.hour for o in parsed], [16, 18])

    def test_observed_max_ignores_failed_quality_control(self):
        good = Observation(at(14), 80.0, quality="V")
        bad = Observation(at(15), 120.0, quality="X")
        self.assertEqual(observed_max([good, bad]), good)

    def test_observed_max_of_nothing_is_none(self):
        self.assertIsNone(observed_max([]))


class TestClimatologicalDay(unittest.TestCase):
    def test_day_starts_at_local_midnight_not_utc(self):
        station = Station("KNYC", "America/New_York", "NY")
        start = climatological_day_start(station, at(14))
        self.assertEqual(start.astimezone(NY).hour, 0)

    def test_different_zones_give_different_boundaries(self):
        ny = climatological_day_start(Station("KNYC", "America/New_York", "NY"), at(14))
        la = climatological_day_start(Station("KLAX", "America/Los_Angeles", "LA"), at(14))
        self.assertNotEqual(ny, la)

    def test_hours_past_peak_signs(self):
        station = Station("KNYC", "America/New_York", "NY", typical_peak_hour=16)
        self.assertLess(hours_past_peak(station, at(10)), 0)
        self.assertAlmostEqual(hours_past_peak(station, at(16)), 0.0, places=6)
        self.assertAlmostEqual(hours_past_peak(station, at(19, 30)), 3.5, places=6)


class TestRiseModel(unittest.TestCase):
    def test_zero_rise_is_certain(self):
        self.assertEqual(probability_rises_by(0, past_peak=2.0), 1.0)

    def test_larger_rises_are_less_likely(self):
        probabilities = [probability_rises_by(d, 1.0) for d in (1, 2, 4, 8)]
        self.assertEqual(probabilities, sorted(probabilities, reverse=True))

    def test_later_in_the_day_means_less_upside(self):
        probabilities = [probability_rises_by(2, past_peak=h) for h in (-2, 0, 2, 5, 8)]
        self.assertEqual(probabilities, sorted(probabilities, reverse=True))

    def test_evening_upside_is_negligible(self):
        self.assertLess(probability_rises_by(2, past_peak=7.0), 0.01)

    def test_morning_upside_is_substantial(self):
        self.assertGreater(probability_rises_by(2, past_peak=-4.0), 0.5)


class TestBracketParsing(unittest.TestCase):
    def test_between(self):
        self.assertEqual(bracket_for(market()), Bracket(72.0, 73.0))

    def test_greater_excludes_the_strike(self):
        b = bracket_for(market(strike_type="greater", floor=90, cap=None))
        self.assertEqual(b, Bracket(91.0, None))

    def test_less_excludes_the_strike(self):
        b = bracket_for(market(strike_type="less", floor=None, cap=60))
        self.assertEqual(b, Bracket(None, 59.0))

    def test_unstructured_market_is_unparseable(self):
        self.assertIsNone(bracket_for(market(strike_type="custom", floor=None, cap=None)))

    def test_contains(self):
        self.assertTrue(Bracket(72.0, 73.0).contains(72))
        self.assertTrue(Bracket(72.0, 73.0).contains(73))
        self.assertFalse(Bracket(72.0, 73.0).contains(74))
        self.assertTrue(Bracket(None, 59.0).contains(-10))
        self.assertTrue(Bracket(91.0, None).contains(200))


class TestBracketProbability(unittest.TestCase):
    """The asymmetry tests. These are the ones that protect real money."""

    def test_already_above_the_ceiling_is_deterministically_no(self):
        p, deterministic, _ = probability_of_bracket(Bracket(72.0, 73.0), 78.0, past_peak=2.0)
        self.assertEqual(p, 0.0)
        self.assertTrue(deterministic, "a daily max cannot come back down")

    def test_already_above_the_ceiling_holds_even_before_peak(self):
        """Monotonicity does not depend on the time of day."""
        p, deterministic, _ = probability_of_bracket(Bracket(72.0, 73.0), 78.0, past_peak=-5.0)
        self.assertEqual(p, 0.0)
        self.assertTrue(deterministic)

    def test_already_past_an_open_topped_floor_is_deterministically_yes(self):
        p, deterministic, _ = probability_of_bracket(Bracket(91.0, None), 95.0, past_peak=1.0)
        self.assertEqual(p, 1.0)
        self.assertTrue(deterministic)

    def test_still_below_the_floor_is_never_deterministic(self):
        """The obs feed can understate the true max, so 'not yet' proves nothing."""
        _, deterministic, _ = probability_of_bracket(Bracket(91.0, None), 70.0, past_peak=6.0)
        self.assertFalse(deterministic)

    def test_inside_the_bracket_is_probabilistic_and_firms_up_late(self):
        early, det_early, _ = probability_of_bracket(Bracket(72.0, 73.0), 72.0, past_peak=0.0)
        late, det_late, _ = probability_of_bracket(Bracket(72.0, 73.0), 72.0, past_peak=6.0)
        self.assertFalse(det_early)
        self.assertFalse(det_late)
        self.assertLess(early, late)
        self.assertGreater(late, 0.95)

    def test_more_headroom_means_safer_inside_the_bracket(self):
        tight, _, _ = probability_of_bracket(Bracket(70.0, 79.0), 79.0, past_peak=1.0)
        roomy, _, _ = probability_of_bracket(Bracket(70.0, 79.0), 70.0, past_peak=1.0)
        self.assertGreater(roomy, tight)

    def test_below_a_closed_bracket_must_reach_without_overshooting(self):
        p, _, _ = probability_of_bracket(Bracket(72.0, 73.0), 68.0, past_peak=1.0)
        self.assertGreaterEqual(p, 0.0)
        self.assertLess(p, probability_rises_by(4.0, 1.0))

    def test_probabilities_stay_in_range(self):
        for current in (60.0, 72.0, 73.0, 90.0):
            for past in (-3.0, 0.0, 3.0, 9.0):
                p, _, _ = probability_of_bracket(Bracket(72.0, 73.0), current, past)
                self.assertGreaterEqual(p, 0.0)
                self.assertLessEqual(p, 1.0)


class TestResolver(StationIsolation, unittest.TestCase):
    def _resolver(self, temperature_f, obs_time=None, **kwargs):
        obs_time = obs_time or at(15)

        def fetcher(station_id, *, since, **_):
            celsius = (temperature_f - 32) * 5 / 9
            return [Observation(obs_time, to_fahrenheit(celsius))]

        return WeatherResolver(fetcher=fetcher, **kwargs)

    def test_handles_only_mapped_and_parseable_markets(self):
        resolver = self._resolver(78.0)
        self.assertTrue(resolver.handles(market()))
        self.assertFalse(resolver.handles(market(ticker="KXBTCD-1")))
        self.assertFalse(
            resolver.handles(market(strike_type="custom", floor=None, cap=None))
        )

    def test_silent_before_the_afternoon_peak(self):
        """No observational edge exists at 10am; the day has not happened yet."""
        resolver = self._resolver(78.0, obs_time=at(10))
        self.assertIsNone(resolver.estimate(market(), now=at(10)))

    def test_fires_after_peak_with_a_settled_reading(self):
        resolver = self._resolver(78.0)
        estimate = resolver.estimate(market(), now=at(19))
        self.assertIsNotNone(estimate)
        self.assertEqual(estimate.probability, 0.0)

    def test_unverified_station_never_claims_determinism(self):
        resolver = self._resolver(78.0)
        estimate = resolver.estimate(market(), now=at(19))
        self.assertFalse(estimate.deterministic)
        self.assertFalse(estimate.detail["station_verified"])

    def test_verified_station_may_claim_determinism(self):
        stations.verify("KXHIGHNY", "KNYC")
        resolver = self._resolver(78.0)
        estimate = resolver.estimate(market(), now=at(19))
        self.assertTrue(estimate.deterministic)
        self.assertGreater(estimate.confidence, 0.9)

    def test_readings_on_a_rounding_boundary_are_refused(self):
        """71.6F could print as 71 or 72; that flips the bracket."""
        resolver = self._resolver(73.5)
        self.assertIsNone(resolver.estimate(market(), now=at(19)))

    def test_readings_clear_of_a_boundary_are_accepted(self):
        resolver = self._resolver(73.5 + BOUNDARY_MARGIN_F + 0.2)
        self.assertIsNotNone(resolver.estimate(market(), now=at(19)))

    def test_staleness_is_measured_from_the_observation(self):
        """An hourly feed fetched now can still be 50 minutes old."""
        resolver = self._resolver(78.0, obs_time=at(15))
        estimate = resolver.estimate(market(), now=at(19))
        self.assertGreater(estimate.staleness_seconds(now=at(19)), 3 * 3600)

    def test_no_observations_yields_no_estimate(self):
        resolver = WeatherResolver(fetcher=lambda station_id, *, since, **_: [])
        self.assertIsNone(resolver.estimate(market(), now=at(19)))

    def test_observations_before_the_local_day_are_dropped(self):
        yesterday = at(14, day=14)

        def fetcher(station_id, *, since, **_):
            return [Observation(yesterday, 99.0), Observation(at(15), 70.0)]

        resolver = WeatherResolver(fetcher=fetcher)
        estimate = resolver.estimate(market(), now=at(19))
        self.assertEqual(estimate.detail["observed_max_rounded_f"], 70)


if __name__ == "__main__":
    unittest.main()


class TestRoundingConsensus(StationIsolation, unittest.TestCase):
    """The API reports Celsius; the climate report publishes whole Fahrenheit."""

    def test_a_clean_reading_has_one_plausible_value(self):
        from cipher.resolvers.weather import plausible_rounded_values

        self.assertEqual(plausible_rounded_values(78.08), [78])

    def test_a_reading_near_a_half_degree_has_two(self):
        from cipher.resolvers.weather import plausible_rounded_values

        self.assertEqual(plausible_rounded_values(78.5), [78, 79])

    def test_ambiguity_is_tolerated_when_the_answer_does_not_change(self):
        """78 or 79 both sit above a 75F ceiling, so the ambiguity is irrelevant."""
        resolver_market = market(floor=74, cap=75)

        def fetcher(station_id, *, since, **_):
            return [Observation(at(15), 78.5)]

        resolver = WeatherResolver(fetcher=fetcher)
        estimate = resolver.estimate(resolver_market, now=at(19))
        self.assertIsNotNone(estimate, "both roundings rule the bracket out")
        self.assertEqual(estimate.probability, 0.0)

    def test_ambiguity_is_refused_when_it_straddles_the_bracket(self):
        """78 loses this bracket and 79 wins it, so there is nothing to trade."""
        straddled = market(floor=79, cap=80)

        def fetcher(station_id, *, since, **_):
            return [Observation(at(15), 78.5)]

        resolver = WeatherResolver(fetcher=fetcher)
        self.assertIsNone(resolver.estimate(straddled, now=at(19)))

    def test_determinism_requires_unanimous_candidates(self):
        stations.verify("KXHIGHNY", "KNYC")

        def fetcher(station_id, *, since, **_):
            return [Observation(at(15), 78.5)]

        resolver = WeatherResolver(fetcher=fetcher)
        estimate = resolver.estimate(market(floor=74, cap=75), now=at(19))
        self.assertTrue(estimate.deterministic)


class TestWeatherEndToEnd(StationIsolation, unittest.TestCase):
    """The fixture day, run the way the CLI runs it."""

    def _run(self):
        import json
        from pathlib import Path

        from cipher.cli import _weather_signals
        from cipher.client import parse_market
        from cipher.resolvers.weather import parse_observations

        path = Path(__file__).parent / "fixtures" / "weather_day.json"
        payload = json.loads(path.read_text())
        now = datetime.fromisoformat(payload["now"].replace("Z", "+00:00"))
        observations = parse_observations(payload["observations"])
        markets = [parse_market(m) for m in payload["markets"]]
        resolver = WeatherResolver(fetcher=lambda s, *, since, **k: observations)
        return _weather_signals(markets, resolver, now=now)

    def test_unverified_station_suppresses_everything(self):
        signals, log = self._run()
        self.assertEqual(signals, [])
        self.assertTrue(any("UNVERIFIED" in note for _, note in log))

    def test_verified_station_finds_the_ruled_out_brackets(self):
        stations.verify("KXHIGHNY", "KNYC")
        signals, _ = self._run()
        self.assertTrue(signals)
        # Observed max is 78F, so 74-75 and 76-77 are both already impossible.
        ruled_out = {s.ticker for s in signals}
        self.assertIn("KXHIGHNY-25AUG15-B74", ruled_out)
        self.assertIn("KXHIGHNY-25AUG15-B76", ruled_out)
        self.assertTrue(all(s.side.value == "no" for s in signals))

    def test_it_never_recommends_the_bracket_containing_the_observed_max(self):
        """78-79 contains the observed 78F; selling it would be backwards."""
        stations.verify("KXHIGHNY", "KNYC")
        signals, _ = self._run()
        for s in signals:
            if s.ticker == "KXHIGHNY-25AUG15-B78":
                self.assertEqual(s.side.value, "yes")

    def test_every_signal_sizes_into_a_twenty_dollar_stake(self):
        from cipher.ticket import size_to_stake

        stations.verify("KXHIGHNY", "KNYC")
        signals, _ = self._run()
        for s in signals:
            ticket = size_to_stake(s, 2000)
            self.assertIsNotNone(ticket)
            self.assertLessEqual(ticket.outlay_cents, 2000)


class TestUserAgent(unittest.TestCase):
    """The NWS asks callers to identify themselves and may block those who don't."""

    def setUp(self):
        import os

        self._saved = os.environ.get("CIPHER_CONTACT")

    def tearDown(self):
        import os

        if self._saved is None:
            os.environ.pop("CIPHER_CONTACT", None)
        else:
            os.environ["CIPHER_CONTACT"] = self._saved

    def test_missing_contact_fails_loudly_rather_than_sending_a_placeholder(self):
        import os

        from cipher.resolvers.weather import WeatherError, user_agent

        os.environ.pop("CIPHER_CONTACT", None)
        with self.assertRaises(WeatherError):
            user_agent()

    def test_contact_is_included_when_set(self):
        import os

        from cipher.resolvers.weather import user_agent

        os.environ["CIPHER_CONTACT"] = "someone@example.com"
        self.assertIn("someone@example.com", user_agent())

    def test_blank_contact_is_treated_as_missing(self):
        import os

        from cipher.resolvers.weather import WeatherError, user_agent

        os.environ["CIPHER_CONTACT"] = "   "
        with self.assertRaises(WeatherError):
            user_agent()
