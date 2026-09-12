#nullable enable
using System;
using System.Collections.Generic;
using Cipher.Game;
using NUnit.Framework;
using UnityEngine;

namespace Cipher.Game.Tests
{
    /// <summary>
    /// The weather's decisions, with no scene in them.
    ///
    /// Weather.Apply writes into RenderSettings and is a sequence of assignments with no judgement
    /// in it. Everything that could be subtly wrong -- a seed that always picks the same sky, a
    /// profile whose fog and horizon disagree, a rain field that leaks drops out of its box -- is
    /// in the pure half and is here.
    /// </summary>
    public sealed class WeatherTests
    {
        private static readonly Sky[] All = (Sky[])Enum.GetValues(typeof(Sky));

        // ------------------------------------------------------------------ picking

        [Test]
        public void Pick_is_deterministic_for_a_seed()
        {
            for (ulong seed = 0; seed < 50; seed++)
                Assert.AreEqual(Weather.Pick(seed), Weather.Pick(seed), $"seed {seed} moved");
        }

        [Test]
        public void Pick_returns_every_condition_across_the_seed_space()
        {
            var seen = new HashSet<Sky>();
            for (ulong seed = 0; seed < 500; seed++) seen.Add(Weather.Pick(seed));
            CollectionAssert.AreEquivalent(All, seen, "some condition is unreachable");
        }

        [Test]
        public void Pick_makes_dusk_the_most_common_sky()
        {
            var counts = new Dictionary<Sky, int>();
            foreach (var s in All) counts[s] = 0;
            for (ulong seed = 0; seed < 20000; seed++) counts[Weather.Pick(seed)]++;

            foreach (var s in All)
            {
                if (s == Sky.DuskClear) continue;
                Assert.Less(counts[s], counts[Sky.DuskClear], $"{s} is at least as common as dusk");
            }
        }

        [Test]
        public void Pick_scrambles_seeds_that_differ_only_in_their_low_bits()
        {
            // The real scenario seeds are authored dates: 20260910, 20260911, 20260912. A weak
            // mix would put the whole campaign under one sky. This is the case that motivated
            // running the seed through SplitMix64 rather than taking it modulo five.
            var seen = new HashSet<Sky>();
            for (ulong i = 0; i < 12; i++) seen.Add(Weather.Pick(20260910UL + i));
            Assert.GreaterOrEqual(seen.Count, 3,
                "twelve consecutive authored seeds produced fewer than three different skies");
        }

        // ------------------------------------------------------------------ override

        [Test]
        public void Command_line_override_names_a_condition()
        {
            Assert.AreEqual(Sky.Fog, Weather.FromCommandLine(new[] { "game.exe", "-exodus-weather", "Fog" }));
            Assert.AreEqual(Sky.DuskRain, Weather.FromCommandLine(new[] { "-exodus-weather", "duskrain" }));
        }

        [Test]
        public void Command_line_override_is_absent_when_it_is_absent()
        {
            Assert.IsNull(Weather.FromCommandLine(null));
            Assert.IsNull(Weather.FromCommandLine(Array.Empty<string>()));
            Assert.IsNull(Weather.FromCommandLine(new[] { "game.exe", "-screen-width", "1600" }));
        }

        [Test]
        public void Command_line_override_with_no_value_does_not_read_past_the_end()
        {
            Assert.IsNull(Weather.FromCommandLine(new[] { "game.exe", "-exodus-weather" }));
        }

        [Test]
        public void Command_line_override_falls_back_when_the_name_is_nonsense()
        {
            // Warns and returns null, so a typo at the command line gives you the seeded sky
            // rather than a hard failure in the middle of a screenshot run.
            UnityEngine.TestTools.LogAssert.Expect(
                LogType.Warning, new System.Text.RegularExpressions.Regex("not a condition"));
            Assert.IsNull(Weather.FromCommandLine(new[] { "-exodus-weather", "sleet" }));
        }

        // ------------------------------------------------------------------ profiles

        [Test]
        public void Every_condition_has_a_profile_that_names_itself()
        {
            foreach (var sky in All)
            {
                var p = Weather.Profile(sky);
                Assert.AreEqual(sky, p.Sky, "profile reports the wrong condition");
                Assert.IsNotEmpty(p.Name, $"{sky} has no display name");
            }
        }

        [Test]
        public void Every_condition_names_both_audio_beds()
        {
            foreach (var sky in All)
            {
                var p = Weather.Profile(sky);
                Assert.IsNotEmpty(p.WeatherBed, $"{sky} has no weather bed");
                Assert.IsNotEmpty(p.PlaceBed, $"{sky} has no place bed");
                // A slot is "group/name" and is used verbatim as a Resources path.
                StringAssert.Contains("/", p.WeatherBed);
                StringAssert.Contains("/", p.PlaceBed);
            }
        }

        [Test]
        public void Every_condition_is_lit_fogged_and_visible()
        {
            foreach (var sky in All)
            {
                var p = Weather.Profile(sky);
                Assert.Greater(p.SunIntensity, 0f, $"{sky} has no key light at all");
                Assert.Greater(p.FogDensity, 0f, $"{sky} has no haze, so the map edge is visible");
                // Exp-squared at 0.05 leaves under 10% visibility at 30 m, which is closer than
                // the chase camera itself. Anything thicker is a bug, not a weather.
                Assert.Less(p.FogDensity, 0.05f, $"{sky} is too thick to play");
                Assert.GreaterOrEqual(p.ShadowStrength, 0f);
                Assert.LessOrEqual(p.ShadowStrength, 1f);
            }
        }

        [Test]
        public void Fog_colour_matches_the_sky_horizon_it_dissolves_into()
        {
            // The failure this catches is the one that reads as "the lighting is broken": geometry
            // fading to grey against a sunset-orange horizon band, with a visible seam at the
            // treeline. They do not have to be equal, but they cannot be opposite.
            foreach (var sky in All)
            {
                var p = Weather.Profile(sky);
                float d = Mathf.Abs(p.FogColor.r - p.SkyHorizon.r)
                        + Mathf.Abs(p.FogColor.g - p.SkyHorizon.g)
                        + Mathf.Abs(p.FogColor.b - p.SkyHorizon.b);
                Assert.Less(d, 0.85f, $"{sky}: fog {p.FogColor} fights its horizon {p.SkyHorizon}");
            }
        }

        [Test]
        public void Only_the_raining_condition_rains_and_it_is_the_one_with_thunder()
        {
            foreach (var sky in All)
            {
                var p = Weather.Profile(sky);
                bool wet = sky == Sky.DuskRain;
                Assert.AreEqual(wet, p.Rain > 0f, $"{sky} rain flag disagrees with the condition");
                if (p.Thunder) Assert.Greater(p.Rain, 0f, $"{sky} has dry thunder");
                StringAssert.Contains(wet ? "rain" : "wind", p.WeatherBed,
                    $"{sky} bed does not match whether it is raining");
            }
        }

        [Test]
        public void Dusk_clear_still_carries_the_shipped_look_exactly()
        {
            // These numbers came from ApplyOvercastWinter and the art was tuned against them. If
            // this test fails, the game's default look has changed and that is a regression
            // wearing a weather system as a disguise.
            var p = Weather.Profile(Sky.DuskClear);
            Assert.AreEqual(new Vector3(13f, -58f, 0f), p.SunEuler);
            Assert.AreEqual(1.30f, p.SunIntensity, 1e-4f);
            Assert.AreEqual(0.80f, p.ShadowStrength, 1e-4f);
            Assert.AreEqual(0.0135f, p.FogDensity, 1e-6f);
            AssertColor(new Color(1f, 0.80f, 0.57f), p.SunColor, "sun");
            AssertColor(new Color(0.62f, 0.52f, 0.47f), p.FogColor, "fog");
            AssertColor(new Color(0.30f, 0.36f, 0.50f), p.AmbientSky, "ambient sky");
            AssertColor(new Color(0.38f, 0.36f, 0.40f), p.AmbientEquator, "ambient equator");
            AssertColor(new Color(0.24f, 0.18f, 0.14f), p.AmbientGround, "ambient ground");
        }

        [Test]
        public void Sunrise_and_dusk_put_the_sun_on_opposite_sides_of_the_map()
        {
            // Both are low raking light; what separates them at a glance is which way the shadows
            // point. A sunrise that lights from the west is just a dusk with warmer clouds.
            var dawn = Weather.Profile(Sky.Sunrise);
            var dusk = Weather.Profile(Sky.DuskClear);
            Assert.Less(dusk.SunEuler.y, 0f, "dusk should rake from the west");
            Assert.Greater(dawn.SunEuler.y, 0f, "sunrise should rake from the east");
            Assert.Less(dawn.SunEuler.x, 30f, "sunrise sun is not low");
            Assert.Less(dusk.SunEuler.x, 30f, "dusk sun is not low");
        }

        [Test]
        public void Noon_is_the_only_condition_with_a_high_sun()
        {
            foreach (var sky in All)
            {
                var p = Weather.Profile(sky);
                if (sky == Sky.OvercastNoon) Assert.Greater(p.SunEuler.x, 45f, "noon sun is not high");
                else Assert.Less(p.SunEuler.x, 45f, $"{sky} has a midday sun");
            }
        }

        [Test]
        public void Rain_and_fog_shorten_the_sightline_more_than_the_clear_skies_do()
        {
            float clear = Weather.Profile(Sky.DuskClear).FogDensity;
            Assert.Greater(Weather.Profile(Sky.DuskRain).FogDensity, clear, "rain does not close in");
            Assert.Greater(Weather.Profile(Sky.Fog).FogDensity, Weather.Profile(Sky.DuskRain).FogDensity,
                "fog is not the thickest condition");
            Assert.Less(Weather.Profile(Sky.OvercastNoon).FogDensity, clear, "noon is hazier than dusk");
        }

        [Test]
        public void Rain_drains_the_warmth_out_of_the_dusk_it_is_built_on()
        {
            // Same sun angle, much less sun. The trap is shipping "dusk plus rain streaks", which
            // keeps the golden key light and reads as a sunny shower.
            var dry = Weather.Profile(Sky.DuskClear);
            var wet = Weather.Profile(Sky.DuskRain);
            Assert.AreEqual(dry.SunEuler, wet.SunEuler, "the rain moved the sun");
            Assert.Less(wet.SunIntensity, dry.SunIntensity * 0.7f, "rain did not kill the key light");

            float dryWarmth = dry.SunColor.r - dry.SunColor.b;
            float wetWarmth = wet.SunColor.r - wet.SunColor.b;
            Assert.Less(wetWarmth, dryWarmth, "the wet sun is as warm as the dry one");
        }

        private static void AssertColor(Color expected, Color actual, string what)
        {
            Assert.AreEqual(expected.r, actual.r, 1e-4f, what + ".r");
            Assert.AreEqual(expected.g, actual.g, 1e-4f, what + ".g");
            Assert.AreEqual(expected.b, actual.b, 1e-4f, what + ".b");
        }
    }

    /// <summary>
    /// The rain field: a fixed array of drops that must stay inside its box forever.
    /// </summary>
    public sealed class RainFieldTests
    {
        [Test]
        public void Drops_start_inside_the_box()
        {
            var field = new RainField(200, radius: 10f, height: 20f, seed: 7);
            AssertAllInside(field);
        }

        [Test]
        public void Field_is_deterministic_for_a_seed()
        {
            var a = new RainField(64, 10f, 20f, seed: 99);
            var b = new RainField(64, 10f, 20f, seed: 99);
            for (int i = 0; i < a.Count; i++) Assert.AreEqual(a.Positions[i], b.Positions[i], $"drop {i}");
        }

        [Test]
        public void Different_seeds_give_different_rain()
        {
            var a = new RainField(64, 10f, 20f, seed: 1);
            var b = new RainField(64, 10f, 20f, seed: 2);
            bool any = false;
            for (int i = 0; i < a.Count; i++) any |= a.Positions[i] != b.Positions[i];
            Assert.IsTrue(any, "two seeds produced identical rain");
        }

        [Test]
        public void Drops_stay_inside_the_box_over_a_long_storm()
        {
            var field = new RainField(300, radius: 10f, height: 20f, seed: 3);
            for (int step = 0; step < 2000; step++) field.Step(1f / 60f, 22f, new Vector2(-4f, 1.5f));
            AssertAllInside(field);
        }

        [Test]
        public void One_enormous_frame_does_not_launch_drops_out_of_the_box()
        {
            // A level load or a breakpoint produces a frame worth several seconds. A single
            // "if below floor, add height" would leave the drop far under the world, invisible
            // and never coming back.
            var field = new RainField(64, radius: 10f, height: 20f, seed: 11);
            field.Step(5f, 30f, new Vector2(-9f, 9f));
            AssertAllInside(field);
        }

        [Test]
        public void A_zero_or_negative_step_changes_nothing()
        {
            var field = new RainField(32, 10f, 20f, seed: 5);
            var before = (Vector3[])field.Positions.Clone();
            field.Step(0f, 20f, Vector2.zero);
            field.Step(-1f, 20f, Vector2.zero);
            for (int i = 0; i < before.Length; i++) Assert.AreEqual(before[i], field.Positions[i]);
        }

        [Test]
        public void Drops_fall_downward()
        {
            var field = new RainField(16, 10f, 100f, seed: 13);
            var before = (Vector3[])field.Positions.Clone();
            field.Step(0.1f, 10f, Vector2.zero);   // exactly one metre

            for (int i = 0; i < before.Length; i++)
            {
                float dropped = before[i].y - field.Positions[i].y;
                // A drop that reached the floor this step comes back in at the top, which is
                // still falling. Asserting a plain decrease would flake on whichever drop
                // happened to start near the bottom.
                if (dropped < 0f) dropped += field.Height;
                Assert.AreEqual(1f, dropped, 1e-3f, $"drop {i} did not fall one metre");
            }
        }

        [Test]
        public void Streak_lengths_vary_but_stay_sane()
        {
            var field = new RainField(256, 10f, 20f, seed: 21);
            float min = float.MaxValue, max = float.MinValue;
            foreach (float l in field.Lengths) { min = Mathf.Min(min, l); max = Mathf.Max(max, l); }
            Assert.Greater(max - min, 0.2f, "every streak is the same length");
            Assert.Greater(min, 0.1f, "a streak is degenerate");
            Assert.Less(max, 3f, "a streak is absurdly long");
        }

        [Test]
        public void A_bad_shape_is_refused_rather_than_drawn()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RainField(-1, 10f, 20f, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RainField(10, 0f, 20f, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RainField(10, 10f, 0f, 1));
        }

        private static void AssertAllInside(RainField field)
        {
            float halfH = field.Height * 0.5f;
            for (int i = 0; i < field.Count; i++)
            {
                var p = field.Positions[i];
                Assert.GreaterOrEqual(p.x, -field.Radius - 1e-3f, $"drop {i} left the box in x");
                Assert.LessOrEqual(p.x, field.Radius + 1e-3f, $"drop {i} left the box in x");
                Assert.GreaterOrEqual(p.z, -field.Radius - 1e-3f, $"drop {i} left the box in z");
                Assert.LessOrEqual(p.z, field.Radius + 1e-3f, $"drop {i} left the box in z");
                Assert.GreaterOrEqual(p.y, -halfH - 1e-3f, $"drop {i} fell out of the bottom");
                Assert.LessOrEqual(p.y, halfH + 1e-3f, $"drop {i} rose out of the top");
            }
        }
    }
}
