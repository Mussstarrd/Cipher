#nullable enable
using System;
using Cipher.Game;
using Cipher.Game.Audio;
using NUnit.Framework;
using UnityEngine;

namespace Cipher.Game.Tests
{
    /// <summary>
    /// The ambient mix: three beds, one of which moves.
    ///
    /// Audio cannot be screenshotted and a bed that is 40% too loud sounds like "the game is a bit
    /// noisy" rather than like a bug, so the levels are asserted rather than listened for. What is
    /// actually tested here is the headroom rule and the shape of the crowd curve; the fact that
    /// an AudioSource gets the number is not interesting.
    /// </summary>
    public sealed class AmbienceMixTests
    {
        private static WeatherProfile Dusk => Weather.Profile(Sky.DuskClear);
        private static WeatherProfile Rain => Weather.Profile(Sky.DuskRain);

        [Test]
        public void Everything_starts_silent()
        {
            var mix = new AmbienceMix();
            Assert.AreEqual(0f, mix.Weather);
            Assert.AreEqual(0f, mix.Place);
            Assert.AreEqual(0f, mix.Crowd);
        }

        [Test]
        public void Beds_fade_towards_the_condition_rather_than_cutting()
        {
            var mix = new AmbienceMix();
            mix.SetCondition(Dusk);
            mix.Step(1f / 60f, 0f);

            Assert.Greater(mix.Weather, 0f, "the bed never started");
            Assert.Less(mix.Weather, Dusk.WeatherBedVolume, "the bed snapped instead of fading");
        }

        [Test]
        public void Beds_arrive_at_the_condition_eventually()
        {
            var mix = new AmbienceMix();
            mix.SetCondition(Dusk);
            for (int i = 0; i < 60 * 30; i++) mix.Step(1f / 60f, 0f);

            Assert.AreEqual(Dusk.WeatherBedVolume, mix.Weather, 1e-3f);
            Assert.AreEqual(Dusk.PlaceBedVolume, mix.Place, 1e-3f);
        }

        [Test]
        public void Snap_puts_the_beds_at_their_level_immediately()
        {
            var mix = new AmbienceMix();
            mix.SetCondition(Rain);
            mix.Snap();
            Assert.AreEqual(Rain.WeatherBedVolume, mix.Weather, 1e-5f);
            Assert.AreEqual(Rain.PlaceBedVolume, mix.Place, 1e-5f);
        }

        [Test]
        public void A_handful_of_stragglers_is_inaudible()
        {
            // The murmur has to mean "a lot of them, over there". A linear curve makes it mean
            // "someone is alive somewhere", which is true for most of every mission.
            Assert.Less(AmbienceMix.CrowdTarget(0.05f), 0.01f, "five percent pressure is audible");
            Assert.AreEqual(0f, AmbienceMix.CrowdTarget(0f), 1e-6f);
        }

        [Test]
        public void The_crowd_bed_rises_with_pressure_and_stops_at_its_ceiling()
        {
            float previous = -1f;
            for (float p = 0f; p <= 1.0001f; p += 0.1f)
            {
                float v = AmbienceMix.CrowdTarget(p);
                Assert.GreaterOrEqual(v, previous, $"the murmur went down at pressure {p}");
                Assert.LessOrEqual(v, AmbienceMix.CrowdCeiling + 1e-5f, $"over the ceiling at {p}");
                previous = v;
            }
            Assert.AreEqual(AmbienceMix.CrowdCeiling, AmbienceMix.CrowdTarget(1f), 1e-5f);
        }

        [Test]
        public void Pressure_outside_zero_to_one_is_clamped_rather_than_extrapolated()
        {
            Assert.AreEqual(0f, AmbienceMix.CrowdTarget(-5f), 1e-6f);
            Assert.AreEqual(AmbienceMix.CrowdCeiling, AmbienceMix.CrowdTarget(9f), 1e-5f);
        }

        [Test]
        public void The_three_beds_together_never_break_the_headroom_rule()
        {
            // The rule the synthesised bank follows is a per-sound peak; this is the same idea for
            // the layer that plays continuously. If the beds can sum past the ceiling, the gunfire
            // has nowhere to sit and the owner will (correctly) call the game muddy.
            foreach (Sky sky in Enum.GetValues(typeof(Sky)))
            {
                var mix = new AmbienceMix();
                mix.SetCondition(Weather.Profile(sky));
                mix.Snap();
                for (int i = 0; i < 600; i++)
                {
                    mix.Step(1f / 60f, 1f);      // worst case: a full field, forever
                    Assert.LessOrEqual(mix.Total, AmbienceMix.Ceiling + 1e-4f,
                        $"{sky} beds sum to {mix.Total}");
                }
            }
        }

        [Test]
        public void When_the_field_is_full_the_static_beds_duck_and_the_murmur_does_not()
        {
            // The murmur is the layer carrying information, so it wins the headroom fight.
            var mix = new AmbienceMix();
            mix.SetCondition(Rain);
            mix.Snap();
            for (int i = 0; i < 600; i++) mix.Step(1f / 60f, 1f);

            Assert.AreEqual(AmbienceMix.CrowdCeiling, mix.Crowd, 1e-3f, "the murmur was ducked");
            Assert.Less(mix.Weather, Rain.WeatherBedVolume, "the rain bed did not make room");
        }

        [Test]
        public void Master_scales_everything_and_mutes_at_zero()
        {
            var mix = new AmbienceMix { Master = 0.5f };
            mix.SetCondition(Dusk);
            mix.Snap();
            mix.Step(1f / 60f, 1f);

            Assert.AreEqual(mix.Weather * 0.5f, mix.WeatherVolume, 1e-6f);
            Assert.AreEqual(mix.Place * 0.5f, mix.PlaceVolume, 1e-6f);
            Assert.AreEqual(mix.Crowd * 0.5f, mix.CrowdVolume, 1e-6f);

            mix.Master = 0f;
            Assert.AreEqual(0f, mix.WeatherVolume);
            Assert.AreEqual(0f, mix.CrowdVolume);
        }

        [Test]
        public void A_negative_frame_does_not_move_the_mix_backwards()
        {
            var mix = new AmbienceMix();
            mix.SetCondition(Dusk);
            mix.Snap();
            float before = mix.Weather;
            mix.Step(-1f, 0f);
            Assert.AreEqual(before, mix.Weather, 1e-6f);
        }
    }

    /// <summary>
    /// Distant thunder's timer. "Randomly every so often" is exactly the kind of thing that ships
    /// firing twice in a frame or never firing at all, and neither is visible in a screenshot.
    /// </summary>
    public sealed class ThunderClockTests
    {
        [Test]
        public void A_clear_sky_never_thunders()
        {
            var clock = new ThunderClock(seed: 1);        // starts disarmed
            for (int i = 0; i < 100000; i++) Assert.IsFalse(clock.Tick(1f / 60f));
        }

        [Test]
        public void A_storm_thunders_repeatedly()
        {
            var clock = new ThunderClock(seed: 1) { Enabled = true };
            int strikes = 0;
            for (int i = 0; i < 60 * 600; i++) if (clock.Tick(1f / 60f)) strikes++;
            Assert.Greater(strikes, 10, "ten minutes of storm produced almost no thunder");
            Assert.Less(strikes, 60, "the thunder is a machine gun");
        }

        [Test]
        public void Thunder_fires_at_most_once_per_call_even_on_a_huge_frame()
        {
            var clock = new ThunderClock(seed: 2) { Enabled = true };
            Assert.IsTrue(clock.Tick(600f), "a ten-minute frame produced no thunder at all");
            // The gap was re-rolled, so the very next normal frame must be quiet.
            Assert.IsFalse(clock.Tick(1f / 60f), "thunder fired twice in a row");
        }

        [Test]
        public void Arming_a_storm_re_rolls_rather_than_firing_instantly()
        {
            // Otherwise a position that spent four minutes dry opens its storm with an immediate
            // clap, from a countdown that had been running the whole time.
            var clock = new ThunderClock(seed: 3);
            for (int i = 0; i < 60 * 300; i++) clock.Tick(1f / 60f);   // disarmed: nothing moves
            clock.Enabled = true;
            Assert.Greater(clock.NextIn, 1f, "the storm opened on an instant strike");
        }

        [Test]
        public void The_same_seed_gives_the_same_storm()
        {
            var a = new ThunderClock(seed: 42) { Enabled = true };
            var b = new ThunderClock(seed: 42) { Enabled = true };
            for (int i = 0; i < 60 * 300; i++)
                Assert.AreEqual(a.Tick(1f / 60f), b.Tick(1f / 60f), $"diverged at frame {i}");
        }

        [Test]
        public void Disabling_and_re_enabling_is_idempotent_when_nothing_changed()
        {
            var clock = new ThunderClock(seed: 5) { Enabled = true };
            float gap = clock.NextIn;
            clock.Enabled = true;                     // already on: must not re-roll
            Assert.AreEqual(gap, clock.NextIn, 1e-6f);
        }
    }

    /// <summary>
    /// Footstep cadence. The owner would hear a wrong one immediately, which is exactly why it
    /// needs a test: "I'll notice if it's wrong" is not a way to keep it right through six more
    /// changes to the hero's movement.
    /// </summary>
    public sealed class FootstepCadenceTests
    {
        private const float Frame = 1f / 60f;

        private static int RunFor(FootstepCadence cadence, float seconds, float speed)
        {
            int steps = 0;
            int frames = Mathf.RoundToInt(seconds / Frame);
            for (int i = 0; i < frames; i++) if (cadence.Tick(Frame, speed) >= 0) steps++;
            return steps;
        }

        [Test]
        public void Standing_still_never_steps()
        {
            var cadence = new FootstepCadence(6);
            Assert.AreEqual(0, RunFor(cadence, 30f, 0f));
        }

        [Test]
        public void Shuffling_against_a_wall_does_not_machine_gun()
        {
            // This is the failure mode a time-based cadence ships with: a hero pressed into
            // geometry, barely moving, firing a footstep every frame.
            var cadence = new FootstepCadence(6);
            Assert.AreEqual(0, RunFor(cadence, 30f, 0.2f), "a crawl produced footsteps");
        }

        [Test]
        public void Walking_produces_a_plausible_cadence()
        {
            var cadence = new FootstepCadence(6);
            int steps = RunFor(cadence, 10f, 4f);          // 40 m at a jog
            int expected = Mathf.RoundToInt(40f / FootstepCadence.Stride);
            Assert.AreEqual(expected, steps, 2, "cadence does not match the ground covered");
        }

        [Test]
        public void Running_twice_as_fast_steps_about_twice_as_often()
        {
            int slow = RunFor(new FootstepCadence(6), 10f, 3f);
            int fast = RunFor(new FootstepCadence(6), 10f, 6f);
            Assert.AreEqual(2f, (float)fast / Mathf.Max(1, slow), 0.25f);
        }

        [Test]
        public void Steps_are_never_closer_together_than_the_floor()
        {
            // Even at an impossible speed. The guard exists for teleports and long frames.
            var cadence = new FootstepCadence(6);
            float since = 99f;
            for (int i = 0; i < 6000; i++)
            {
                since += Frame;
                if (cadence.Tick(Frame, 200f) >= 0)
                {
                    Assert.GreaterOrEqual(since, FootstepCadence.MinGap - 1e-4f, "two steps too close");
                    since = 0f;
                }
            }
        }

        [Test]
        public void Feet_alternate()
        {
            var cadence = new FootstepCadence(6);
            bool? last = null;
            for (int i = 0; i < 2000; i++)
            {
                if (cadence.Tick(Frame, 5f) < 0) continue;
                if (last.HasValue) Assert.AreNotEqual(last.Value, cadence.RightFoot, "the same foot twice");
                last = cadence.RightFoot;
            }
            Assert.IsTrue(last.HasValue, "no steps at all");
        }

        [Test]
        public void The_same_recording_never_plays_twice_in_a_row()
        {
            var cadence = new FootstepCadence(6);
            int previous = -1;
            for (int i = 0; i < 2000; i++)
            {
                int clip = cadence.Tick(Frame, 5f);
                if (clip < 0) continue;
                Assert.AreNotEqual(previous, clip, "the same footstep recording repeated");
                previous = clip;
            }
        }

        [Test]
        public void Six_recordings_do_not_settle_into_an_A_B_A_B_loop()
        {
            // One-back avoidance degenerates into an audible two-clip alternation surprisingly
            // often. Two-back is the cheapest rule that sounds like a person walking.
            var cadence = new FootstepCadence(6);
            int a = -1, b = -1;
            for (int i = 0; i < 4000; i++)
            {
                int clip = cadence.Tick(Frame, 5f);
                if (clip < 0) continue;
                Assert.IsFalse(clip == b && b >= 0, "clip repeated two steps later");
                b = a; a = clip;
            }
        }

        [Test]
        public void Every_recording_gets_used()
        {
            var cadence = new FootstepCadence(6);
            var seen = new bool[6];
            for (int i = 0; i < 4000; i++)
            {
                int clip = cadence.Tick(Frame, 5f);
                if (clip >= 0) seen[clip] = true;
            }
            for (int i = 0; i < seen.Length; i++) Assert.IsTrue(seen[i], $"recording {i} never played");
        }

        [Test]
        public void A_single_recording_is_allowed_and_does_not_hang()
        {
            var cadence = new FootstepCadence(1);
            Assert.Greater(RunFor(cadence, 10f, 5f), 10);
        }

        [Test]
        public void Two_recordings_relax_to_one_back_rather_than_spinning()
        {
            // The two-back rule is unsatisfiable with two clips; it must degrade, not loop.
            var cadence = new FootstepCadence(2);
            int steps = RunFor(cadence, 10f, 5f);
            Assert.Greater(steps, 10, "two clips produced no cadence");
        }

        [Test]
        public void Pitch_stays_in_a_believable_range()
        {
            var cadence = new FootstepCadence(6);
            for (int i = 0; i < 500; i++)
            {
                float p = cadence.Pitch;
                Assert.Greater(p, 0.9f);
                Assert.Less(p, 1.1f);
            }
        }

        [Test]
        public void Stopping_briefly_does_not_reset_the_whole_walk()
        {
            // Turning on the spot should not cost the player a step; standing about should.
            var cadence = new FootstepCadence(6);
            cadence.Tick(Frame, 5f);
            for (int i = 0; i < 30; i++) cadence.Tick(Frame, 5f);   // build the accumulator
            int before = cadence.Steps;
            for (int i = 0; i < 6; i++) cadence.Tick(Frame, 0f);    // a tenth of a second still
            for (int i = 0; i < 20; i++) cadence.Tick(Frame, 5f);
            Assert.Greater(cadence.Steps, before, "a brief pause killed the cadence");
        }

        [Test]
        public void A_zero_length_frame_does_nothing()
        {
            var cadence = new FootstepCadence(6);
            Assert.AreEqual(-1, cadence.Tick(0f, 10f));
            Assert.AreEqual(0, cadence.Steps);
        }
    }
}
