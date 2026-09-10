#nullable enable
using System;

namespace Cipher.Game.Audio
{
    /// <summary>
    /// Tiny procedural DSP toolkit: every sound in the graybox is synthesized from these at
    /// startup (no asset files, no licences, deterministic). Pure C# so it is unit-testable;
    /// the Unity side only wraps the float[] in an AudioClip.
    /// </summary>
    public static class Waveforms
    {
        public const int SampleRate = 22050;

        /// <summary>Deterministic noise source (xorshift32).</summary>
        public struct Noise
        {
            private uint _s;
            public Noise(uint seed) { _s = seed == 0 ? 0x9E3779B9u : seed; }
            public float Next()
            {
                _s ^= _s << 13; _s ^= _s >> 17; _s ^= _s << 5;
                return (_s & 0xFFFFFF) / 8388608f - 1f; // [-1, 1)
            }
        }

        public static float[] Silence(float seconds) => new float[Math.Max(1, (int)(seconds * SampleRate))];

        /// <summary>White noise shaped by an exponential decay: the basis of every impact.</summary>
        public static float[] NoiseBurst(float seconds, float decayPerSecond, uint seed)
        {
            var buf = Silence(seconds);
            var n = new Noise(seed);
            for (int i = 0; i < buf.Length; i++)
                buf[i] = n.Next() * MathF.Exp(-decayPerSecond * i / SampleRate);
            return buf;
        }

        /// <summary>Sine with a frequency sweep (linear in log-frequency) and exponential decay.</summary>
        public static float[] Sweep(float seconds, float fromHz, float toHz, float decayPerSecond, float amplitude = 1f)
        {
            var buf = Silence(seconds);
            double phase = 0;
            float logFrom = MathF.Log(Math.Max(1f, fromHz)), logTo = MathF.Log(Math.Max(1f, toHz));
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / buf.Length;
                float hz = MathF.Exp(logFrom + (logTo - logFrom) * t);
                phase += 2 * Math.PI * hz / SampleRate;
                buf[i] = amplitude * (float)Math.Sin(phase) * MathF.Exp(-decayPerSecond * i / SampleRate);
            }
            return buf;
        }

        /// <summary>Constant tone with a linear attack and exponential release. Harmonics add a bit of bite.</summary>
        public static float[] Tone(float seconds, float hz, float attackSeconds, float releasePerSecond, float amplitude = 1f, int harmonics = 1)
        {
            var buf = Silence(seconds);
            int attack = Math.Max(1, (int)(attackSeconds * SampleRate));
            for (int i = 0; i < buf.Length; i++)
            {
                double t = (double)i / SampleRate;
                float v = 0f;
                for (int h = 1; h <= harmonics; h++)
                    v += (float)Math.Sin(2 * Math.PI * hz * h * t) / h;
                float env = Math.Min(1f, (float)i / attack) * MathF.Exp(-releasePerSecond * Math.Max(0, i - attack) / SampleRate);
                buf[i] = amplitude * v * env;
            }
            return buf;
        }

        /// <summary>Square-ish beep (odd harmonics) for UI and alerts.</summary>
        public static float[] Beep(float seconds, float hz, float amplitude = 0.6f)
        {
            var buf = Silence(seconds);
            int fade = Math.Max(1, (int)(0.004f * SampleRate));
            for (int i = 0; i < buf.Length; i++)
            {
                double t = (double)i / SampleRate;
                float v = 0f;
                for (int h = 1; h <= 7; h += 2) v += (float)Math.Sin(2 * Math.PI * hz * h * t) / h;
                float env = Math.Min(1f, Math.Min(i, buf.Length - 1 - i) / (float)fade);
                buf[i] = amplitude * v * 0.8f * env;
            }
            return buf;
        }

        /// <summary>One-pole low-pass, in place. Cutoff in Hz.</summary>
        public static float[] LowPass(float[] buf, float cutoffHz)
        {
            float rc = 1f / (2f * MathF.PI * Math.Max(1f, cutoffHz));
            float dt = 1f / SampleRate;
            float a = dt / (rc + dt);
            float y = 0f;
            for (int i = 0; i < buf.Length; i++)
            {
                y += a * (buf[i] - y);
                buf[i] = y;
            }
            return buf;
        }

        /// <summary>One-pole high-pass, in place.</summary>
        public static float[] HighPass(float[] buf, float cutoffHz)
        {
            float rc = 1f / (2f * MathF.PI * Math.Max(1f, cutoffHz));
            float dt = 1f / SampleRate;
            float a = rc / (rc + dt);
            float prevX = 0f, prevY = 0f;
            for (int i = 0; i < buf.Length; i++)
            {
                float x = buf[i];
                float y = a * (prevY + x - prevX);
                prevX = x; prevY = y;
                buf[i] = y;
            }
            return buf;
        }

        /// <summary>Sums layers sample-wise (shorter layers are zero-padded), optionally offset in time.</summary>
        public static float[] Mix(params (float[] Samples, float Gain, float OffsetSeconds)[] layers)
        {
            int length = 0;
            foreach (var l in layers) length = Math.Max(length, (int)(l.OffsetSeconds * SampleRate) + l.Samples.Length);
            var buf = new float[Math.Max(1, length)];
            foreach (var l in layers)
            {
                int off = (int)(l.OffsetSeconds * SampleRate);
                for (int i = 0; i < l.Samples.Length; i++) buf[off + i] += l.Samples[i] * l.Gain;
            }
            return buf;
        }

        public static float[] Concat(params float[][] parts)
        {
            int n = 0;
            foreach (var p in parts) n += p.Length;
            var buf = new float[Math.Max(1, n)];
            int at = 0;
            foreach (var p in parts) { Array.Copy(p, 0, buf, at, p.Length); at += p.Length; }
            return buf;
        }

        /// <summary>Soft-clips and scales so the peak sits at <paramref name="peak"/>. Never produces NaN.</summary>
        public static float[] Normalize(float[] buf, float peak = 0.9f)
        {
            float max = 1e-6f;
            for (int i = 0; i < buf.Length; i++)
            {
                float v = buf[i];
                if (float.IsNaN(v) || float.IsInfinity(v)) buf[i] = v = 0f;
                max = Math.Max(max, Math.Abs(v));
            }
            float g = peak / max;
            for (int i = 0; i < buf.Length; i++) buf[i] = MathF.Tanh(buf[i] * g * 1.3f); // soft clip into (-1, 1)
            // Second pass keeps the promise exactly.
            max = 1e-6f;
            for (int i = 0; i < buf.Length; i++) max = Math.Max(max, Math.Abs(buf[i]));
            g = peak / max;
            for (int i = 0; i < buf.Length; i++) buf[i] *= g;
            return buf;
        }

        /// <summary>Fades the tail so loops and one-shots never click.</summary>
        public static float[] FadeOut(float[] buf, float seconds)
        {
            int n = Math.Min(buf.Length, Math.Max(1, (int)(seconds * SampleRate)));
            for (int i = 0; i < n; i++) buf[buf.Length - 1 - i] *= (float)i / n;
            return buf;
        }

        /// <summary>Cross-fades the last <paramref name="seconds"/> into the start so the buffer loops seamlessly.</summary>
        public static float[] MakeLoop(float[] buf, float seconds)
        {
            int n = Math.Min(buf.Length / 2, Math.Max(1, (int)(seconds * SampleRate)));
            var outBuf = new float[buf.Length - n];
            Array.Copy(buf, 0, outBuf, 0, outBuf.Length);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                outBuf[i] = buf[i] * t + buf[outBuf.Length + i] * (1f - t);
            }
            return outBuf;
        }

        public static float Peak(float[] buf)
        {
            float max = 0f;
            foreach (var v in buf) max = Math.Max(max, Math.Abs(v));
            return max;
        }
    }
}
