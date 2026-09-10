#nullable enable
using System;
using Cipher.Game.Audio;
using NUnit.Framework;

namespace Cipher.Game.Tests
{
    public sealed class SoundRecipeTests
    {
        [Test]
        public void EveryRecipe_Builds_Normalized_Finite_AndShort()
        {
            var bank = SoundRecipes.BuildAll();
            Assert.AreEqual(Enum.GetValues(typeof(Sfx)).Length, bank.Count);
            long totalSamples = 0;
            foreach (var kv in bank)
            {
                float[] s = kv.Value;
                Assert.Greater(s.Length, 100, $"{kv.Key} is too short to hear");
                Assert.Less(s.Length, Waveforms.SampleRate * 3, $"{kv.Key} is longer than 3 s");
                float peak = Waveforms.Peak(s);
                Assert.LessOrEqual(peak, 0.96f, $"{kv.Key} peaks too hot");
                Assert.GreaterOrEqual(peak, 0.15f, $"{kv.Key} is nearly silent");
                foreach (float v in s) Assert.IsFalse(float.IsNaN(v) || float.IsInfinity(v), $"{kv.Key} has a NaN/Inf sample");
                totalSamples += s.Length;
            }
            Assert.Less(totalSamples * 4, 4_000_000, "whole bank should stay under 4 MB of float samples");
        }

        [Test]
        public void Loops_AreSeamless_AndDeterministic()
        {
            float[] wind = SoundRecipes.WindLoop();
            float[] horde = SoundRecipes.HordeLoop();
            Assert.Less(Math.Abs(wind[0] - wind[wind.Length - 1]), 0.15f, "wind loop must not click at the seam");
            Assert.Less(Math.Abs(horde[0] - horde[horde.Length - 1]), 0.15f, "horde loop must not click at the seam");
            Assert.LessOrEqual(Waveforms.Peak(wind), 0.55f);

            float[] again = SoundRecipes.WindLoop();
            for (int i = 0; i < wind.Length; i += 997) Assert.AreEqual(wind[i], again[i], "recipes are deterministic");
        }

        [Test]
        public void Filters_ShapeSpectrum_AsExpected()
        {
            // A low-pass at 100 Hz must gut a 4 kHz tone; a high-pass at 4 kHz must gut a 100 Hz tone.
            float[] hi = Waveforms.Tone(0.2f, 4000f, 0.001f, 0f);
            float[] lo = Waveforms.Tone(0.2f, 100f, 0.001f, 0f);
            Assert.Less(Waveforms.Peak(Waveforms.LowPass((float[])hi.Clone(), 100f)), 0.06f);
            Assert.Less(Waveforms.Peak(Waveforms.HighPass((float[])lo.Clone(), 4000f)), 0.06f);
            Assert.Greater(Waveforms.Peak(Waveforms.LowPass((float[])lo.Clone(), 4000f)), 0.9f);
        }

        [Test]
        public void Normalize_HandlesGarbage_AndHitsThePeak()
        {
            var buf = new[] { 0f, float.NaN, 5f, -50f, float.PositiveInfinity, 0.1f };
            Waveforms.Normalize(buf, 0.8f);
            foreach (float v in buf) Assert.IsFalse(float.IsNaN(v) || float.IsInfinity(v));
            Assert.AreEqual(0.8f, Waveforms.Peak(buf), 1e-4f);
        }
    }
}
