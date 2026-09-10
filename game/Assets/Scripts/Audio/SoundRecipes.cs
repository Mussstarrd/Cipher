#nullable enable
using System.Collections.Generic;
using static Cipher.Game.Audio.Waveforms;

namespace Cipher.Game.Audio
{
    public enum Sfx
    {
        Shot, Hit, Kill,
        StrikeCall, StrikeWhistle, Bomb,
        TurretShot,
        Place, Sell, Upgrade, Refuse, CursorTick,
        SapperSpotted, BreachPlanting, BreachOpened, WallCollapsed, Repaired, SpitterSeen, TurretDestroyed,
        Pickup, WaveHorn, WaveClear, Win, Lose,
        Hurt, Down,
        MenuOpen, MenuTick, MenuConfirm,
    }

    /// <summary>
    /// The graybox sound bank as pure sample buffers. Each recipe is a few DSP layers; the
    /// whole bank is well under a megabyte and builds in milliseconds.
    /// </summary>
    public static class SoundRecipes
    {
        public static Dictionary<Sfx, float[]> BuildAll()
        {
            var bank = new Dictionary<Sfx, float[]>();
            foreach (Sfx sfx in System.Enum.GetValues(typeof(Sfx))) bank[sfx] = Build(sfx);
            return bank;
        }

        public static float[] Build(Sfx sfx)
        {
            switch (sfx)
            {
                case Sfx.Shot:
                    // Crack (high-passed noise) over a short low thump.
                    return Normalize(Mix(
                        (HighPass(NoiseBurst(0.07f, 70f, 11), 900f), 1f, 0f),
                        (Sweep(0.06f, 180f, 60f, 40f), 0.8f, 0f)), 0.75f);
                case Sfx.Hit:
                    return Normalize(FadeOut(Sweep(0.03f, 1800f, 900f, 90f), 0.01f), 0.35f);
                case Sfx.Kill:
                    return Normalize(Mix(
                        (LowPass(NoiseBurst(0.12f, 40f, 23), 1200f), 1f, 0f),
                        (Sweep(0.12f, 240f, 70f, 30f), 0.9f, 0f)), 0.6f);

                case Sfx.StrikeCall:
                    return Normalize(Concat(Beep(0.07f, 880f), Silence(0.05f), Beep(0.07f, 880f), Silence(0.05f), Beep(0.14f, 1320f)), 0.5f);
                case Sfx.StrikeWhistle:
                    // Inbound: a falling whistle over 1.2 s.
                    return Normalize(FadeOut(Sweep(1.2f, 2400f, 500f, 0.6f, 0.7f), 0.1f), 0.45f);
                case Sfx.Bomb:
                    return Normalize(Mix(
                        (LowPass(NoiseBurst(0.7f, 9f, 37), 900f), 1f, 0f),
                        (Sweep(0.5f, 110f, 35f, 7f), 1.2f, 0f),
                        (HighPass(NoiseBurst(0.12f, 60f, 41), 1500f), 0.5f, 0f)), 0.95f);

                case Sfx.TurretShot:
                    return Normalize(Mix(
                        (HighPass(NoiseBurst(0.045f, 110f, 53), 1400f), 1f, 0f),
                        (Sweep(0.04f, 320f, 120f, 60f), 0.5f, 0f)), 0.4f);

                case Sfx.Place:
                    return Normalize(Mix(
                        (LowPass(NoiseBurst(0.09f, 50f, 61), 2500f), 0.7f, 0f),
                        (Tone(0.12f, 220f, 0.005f, 30f), 1f, 0f)), 0.55f);
                case Sfx.Sell:
                    return Normalize(Concat(Tone(0.08f, 440f, 0.005f, 40f), Tone(0.1f, 660f, 0.005f, 30f)), 0.5f);
                case Sfx.Upgrade:
                    return Normalize(Concat(Tone(0.08f, 523f, 0.005f, 30f, 1f, 2), Tone(0.08f, 659f, 0.005f, 30f, 1f, 2), Tone(0.16f, 784f, 0.005f, 20f, 1f, 2)), 0.55f);
                case Sfx.Refuse:
                    return Normalize(Tone(0.18f, 110f, 0.005f, 15f, 1f, 5), 0.45f);
                case Sfx.CursorTick:
                    return Normalize(FadeOut(Sweep(0.018f, 1400f, 1000f, 120f), 0.006f), 0.18f);

                case Sfx.SapperSpotted:
                    // Two-tone siren, twice.
                    return Normalize(Concat(Beep(0.16f, 660f), Beep(0.16f, 520f), Silence(0.06f), Beep(0.16f, 660f), Beep(0.16f, 520f)), 0.5f);
                case Sfx.BreachPlanting:
                    // Ticking countdown: four ticks, rising.
                    return Normalize(Concat(Beep(0.05f, 700f), Silence(0.15f), Beep(0.05f, 800f), Silence(0.15f), Beep(0.05f, 900f), Silence(0.15f), Beep(0.09f, 1100f)), 0.45f);
                case Sfx.BreachOpened:
                    return Normalize(Mix(
                        (LowPass(NoiseBurst(0.45f, 12f, 71), 1500f), 1f, 0f),
                        (Sweep(0.4f, 160f, 45f, 9f), 1f, 0f),
                        (HighPass(NoiseBurst(0.08f, 80f, 73), 2500f), 0.6f, 0.02f)), 0.85f);
                case Sfx.WallCollapsed:
                    return Normalize(Mix(
                        (LowPass(NoiseBurst(1.1f, 5f, 79), 700f), 1f, 0f),
                        (Sweep(0.9f, 90f, 30f, 4f), 1.3f, 0f),
                        (LowPass(NoiseBurst(0.3f, 20f, 83), 1200f), 0.7f, 0.25f)), 0.95f);
                case Sfx.Repaired:
                    return Normalize(Concat(Tone(0.07f, 660f, 0.005f, 40f), Tone(0.14f, 880f, 0.005f, 25f)), 0.45f);
                case Sfx.SpitterSeen:
                    return Normalize(Mix(
                        (Sweep(0.22f, 300f, 900f, 8f), 1f, 0f),
                        (LowPass(NoiseBurst(0.2f, 25f, 89), 1800f), 0.5f, 0f)), 0.45f);
                case Sfx.TurretDestroyed:
                    return Normalize(Mix(
                        (LowPass(NoiseBurst(0.5f, 10f, 97), 1100f), 1f, 0f),
                        (Sweep(0.4f, 200f, 50f, 8f), 0.9f, 0f),
                        (Concat(Beep(0.06f, 500f), Beep(0.06f, 400f), Beep(0.1f, 300f)), 0.5f, 0.05f)), 0.8f);

                case Sfx.Pickup:
                    return Normalize(Concat(Tone(0.07f, 659f, 0.004f, 30f, 1f, 3), Tone(0.07f, 880f, 0.004f, 30f, 1f, 3), Tone(0.07f, 1109f, 0.004f, 30f, 1f, 3), Tone(0.22f, 1319f, 0.004f, 14f, 1f, 3)), 0.55f);
                case Sfx.WaveHorn:
                    return Normalize(Mix(
                        (Tone(0.9f, 98f, 0.05f, 3f, 1f, 6), 1f, 0f),
                        (Tone(0.9f, 147f, 0.08f, 3f, 0.7f, 6), 1f, 0f)), 0.7f);
                case Sfx.WaveClear:
                    return Normalize(Concat(Tone(0.12f, 392f, 0.005f, 20f, 1f, 3), Tone(0.12f, 523f, 0.005f, 20f, 1f, 3), Tone(0.3f, 659f, 0.005f, 8f, 1f, 3)), 0.6f);
                case Sfx.Win:
                    return Normalize(Concat(
                        Tone(0.14f, 392f, 0.005f, 15f, 1f, 3), Tone(0.14f, 523f, 0.005f, 15f, 1f, 3), Tone(0.14f, 659f, 0.005f, 15f, 1f, 3),
                        Tone(0.14f, 784f, 0.005f, 15f, 1f, 3), Mix((Tone(0.9f, 1047f, 0.01f, 4f, 1f, 3), 1f, 0f), (Tone(0.9f, 784f, 0.01f, 4f, 0.7f, 3), 1f, 0f))), 0.7f);
                case Sfx.Lose:
                    return Normalize(Concat(
                        Tone(0.25f, 330f, 0.01f, 8f, 1f, 4), Tone(0.25f, 294f, 0.01f, 8f, 1f, 4), Tone(0.25f, 262f, 0.01f, 8f, 1f, 4),
                        Mix((Tone(1.2f, 131f, 0.02f, 2.5f, 1f, 5), 1f, 0f), (Tone(1.2f, 139f, 0.02f, 2.5f, 0.6f, 5), 1f, 0f))), 0.7f);

                case Sfx.Hurt:
                    return Normalize(Mix(
                        (LowPass(NoiseBurst(0.12f, 35f, 101), 800f), 1f, 0f),
                        (Sweep(0.15f, 140f, 60f, 20f), 1f, 0f)), 0.55f);
                case Sfx.Down:
                    return Normalize(Mix(
                        (LowPass(NoiseBurst(0.8f, 6f, 103), 500f), 1f, 0f),
                        (Sweep(0.9f, 120f, 30f, 4f), 1.3f, 0f)), 0.85f);

                case Sfx.MenuOpen:
                    return Normalize(Concat(Tone(0.06f, 440f, 0.004f, 40f), Tone(0.1f, 330f, 0.004f, 30f)), 0.4f);
                case Sfx.MenuTick:
                    return Normalize(FadeOut(Sweep(0.025f, 1200f, 900f, 100f), 0.008f), 0.25f);
                case Sfx.MenuConfirm:
                    return Normalize(Concat(Tone(0.06f, 523f, 0.004f, 40f), Tone(0.12f, 784f, 0.004f, 25f)), 0.45f);
            }
            return Silence(0.01f);
        }

        /// <summary>Looping ambience: wind (filtered noise) with a low drone. 4 s seamless loop.</summary>
        public static float[] WindLoop()
        {
            var wind = LowPass(NoiseBurst(4.4f, 0f, 211), 380f);
            // Slow amplitude wobble so it breathes.
            for (int i = 0; i < wind.Length; i++)
                wind[i] *= 0.6f + 0.4f * (float)System.Math.Sin(2 * System.Math.PI * 0.23 * i / SampleRate);
            var drone = Tone(4.4f, 55f, 0.5f, 0f, 0.5f, 2);
            return Normalize(MakeLoop(Mix((wind, 1f, 0f), (drone, 0.35f, 0f)), 0.4f), 0.5f);
        }

        /// <summary>Looping horde rumble: many footfalls as band-limited noise with a growl. 3 s seamless loop.</summary>
        public static float[] HordeLoop()
        {
            var rumble = LowPass(HighPass(NoiseBurst(3.3f, 0f, 313), 60f), 220f);
            var growl = Tone(3.3f, 41f, 0.3f, 0f, 0.6f, 4);
            for (int i = 0; i < rumble.Length; i++)
                rumble[i] *= 0.7f + 0.3f * (float)System.Math.Sin(2 * System.Math.PI * 1.7 * i / SampleRate);
            return Normalize(MakeLoop(Mix((rumble, 1f, 0f), (growl, 0.4f, 0f)), 0.3f), 0.6f);
        }
    }
}
