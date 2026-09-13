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

        // THE CROWD AND THE PLACE. Owner: "no birds chirping or forest sounds or feet shuffling
        // or people grunting or breathing heavily as they run faster or smacking attacking sounds
        // or impact sounds". None of these existed. See CrowdFoley for how they are placed.
        Step, Breath, Grunt, Bite, TruckHit, DeathGrunt, Bird, Cricket,
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
                // A GUNSHOT IS A TRANSIENT, A BODY AND A PLACE. This used to be 70ms of noise over
                // a 60ms sweep and nothing else, and the owner's description of it was exact: "my
                // laser sounds like it's just clicking". It was not a mix problem. A sound that
                // stops dead 70ms after it starts IS a click, however well built the first 70ms
                // are -- there was no body under the crack and no room around it.
                //
                // Four layers, and the last two are what the old one was missing entirely:
                //   CRACK   the supersonic snap. Short, bright, unchanged in character.
                //   BODY    the low end of the discharge, with real weight and a slower decay.
                //   SLAP    the crack again 55ms later, quieter and dulled -- the report coming
                //           back off the houses across the street. This single layer is most of
                //           the difference between "in a place" and "in a vacuum".
                //   TAIL    low-passed noise decaying over a third of a second: the street itself
                //           ringing. Quiet enough to feel rather than hear.
                case Sfx.Shot:
                    return Normalize(Mix(
                        (HighPass(NoiseBurst(0.05f, 95f, 11), 1100f), 1.0f, 0f),
                        (Sweep(0.16f, 220f, 52f, 17f), 0.95f, 0f),
                        (LowPass(HighPass(NoiseBurst(0.05f, 80f, 17), 700f), 3200f), 0.34f, 0.055f),
                        (LowPass(NoiseBurst(0.34f, 12f, 29), 1400f), 0.20f, 0.02f)), 0.8f);

                // Impact on a body: a slap with something behind it, not a tick. Still short --
                // this plays many times a second in a firefight and cannot be allowed to smear.
                case Sfx.Hit:
                    return Normalize(Mix(
                        (FadeOut(Sweep(0.035f, 1700f, 820f, 80f), 0.01f), 1f, 0f),
                        (LowPass(NoiseBurst(0.07f, 46f, 71), 900f), 0.55f, 0.004f)), 0.42f);

                // A body going down. Given a tail so it lands with some finality rather than
                // stopping the instant it starts -- this is the sound that has to feel like a
                // consequence, because it is the one the player is working for.
                case Sfx.Kill:
                    return Normalize(Mix(
                        (LowPass(NoiseBurst(0.14f, 34f, 23), 1200f), 1f, 0f),
                        (Sweep(0.20f, 240f, 62f, 19f), 0.95f, 0f),
                        (LowPass(NoiseBurst(0.30f, 11f, 31), 700f), 0.28f, 0.03f)), 0.65f);

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

                // The same architecture as Sfx.Shot but DELIBERATELY TIGHTER. A turret fires far
                // more often than the player does, and half a dozen of them firing a sound with a
                // third of a second of tail turns the whole position to mush. Short slap, short
                // tail, and a quieter mix: it should read as a place full of guns, not as one gun
                // played six times.
                case Sfx.TurretShot:
                    return Normalize(Mix(
                        (HighPass(NoiseBurst(0.04f, 125f, 53), 1600f), 1f, 0f),
                        (Sweep(0.09f, 330f, 105f, 30f), 0.55f, 0f),
                        (LowPass(NoiseBurst(0.14f, 26f, 59), 1100f), 0.20f, 0.035f)), 0.44f);

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

                // ---- the crowd -----------------------------------------------------------
                // A footfall on gravel: a crunch with a small thump under it. Short, because it
                // plays a great many times and the tail of the last one must not still be going
                // when the next lands.
                case Sfx.Step:
                    return Normalize(Mix(
                        (HighPass(NoiseBurst(0.055f, 70f, 211), 1300f), 1f, 0f),
                        (LowPass(NoiseBurst(0.045f, 90f, 223), 420f), 0.7f, 0f)), 0.5f);

                // An exhale: breath is noise shaped by the mouth, so it is band-passed noise with
                // a slow decay and nothing tonal in it at all.
                case Sfx.Breath:
                    return Normalize(FadeOut(LowPass(HighPass(NoiseBurst(0.30f, 8f, 307), 280f), 2100f), 0.08f), 0.45f);

                // Effort. A VOICE is a pitch with harmonics and breath over it, and a grunt is that
                // pitch falling: the sweep is the vocal cords, the tone adds the harmonics, the
                // filtered noise is the throat.
                case Sfx.Grunt:
                    return Normalize(Mix(
                        (Sweep(0.18f, 165f, 92f, 11f), 1f, 0f),
                        (Tone(0.15f, 130f, 0.01f, 14f, 0.55f, 4), 0.8f, 0.01f),
                        (LowPass(NoiseBurst(0.16f, 18f, 401), 1500f), 0.45f, 0f)), 0.6f);

                // Teeth and hands landing on someone: a slap transient over a dull body.
                case Sfx.Bite:
                    return Normalize(Mix(
                        (HighPass(NoiseBurst(0.02f, 160f, 503), 2400f), 0.6f, 0f),
                        (Sweep(0.05f, 900f, 290f, 60f), 1f, 0.004f),
                        (LowPass(NoiseBurst(0.10f, 38f, 509), 950f), 0.9f, 0.006f)), 0.7f);

                // Bodies hitting a vehicle: a low thud with the panel ringing after it. This is
                // the sound of the objective taking damage, and it had NO sound at all.
                case Sfx.TruckHit:
                    return Normalize(Mix(
                        (Sweep(0.22f, 180f, 55f, 14f), 1f, 0f),
                        (Tone(0.34f, 220f, 0.002f, 9f, 0.5f, 5), 0.5f, 0.01f),
                        (LowPass(NoiseBurst(0.18f, 22f, 601), 900f), 0.8f, 0f)), 0.85f);

                // The last sound a body makes. Longer and lower than the grunt, falling all the way.
                case Sfx.DeathGrunt:
                    return Normalize(Mix(
                        (Sweep(0.32f, 170f, 68f, 8f), 1f, 0f),
                        (Tone(0.28f, 112f, 0.01f, 8f, 0.5f, 5), 0.7f, 0.02f),
                        (LowPass(NoiseBurst(0.30f, 10f, 701), 1400f), 0.4f, 0f)), 0.6f);

                // ---- the place -----------------------------------------------------------
                // Three notes, up-down-up, high and thin. Pitch jitter at play time makes it a
                // different bird each time.
                case Sfx.Bird:
                    return Normalize(HighPass(Concat(
                        Sweep(0.06f, 2800f, 3600f, 25f, 0.8f), Silence(0.05f),
                        Sweep(0.05f, 3400f, 2600f, 30f, 0.7f), Silence(0.08f),
                        Sweep(0.07f, 3000f, 4200f, 25f, 0.6f)), 1500f), 0.35f);

                // Stridulation: a train of tiny clicks at a fixed rate. Eight of them is one chirp.
                case Sfx.Cricket:
                {
                    var parts = new float[16][];
                    for (int i = 0; i < 8; i++)
                    {
                        parts[i * 2] = Tone(0.018f, 4300f, 0.001f, 220f, 0.7f);
                        parts[i * 2 + 1] = Silence(0.022f);
                    }
                    return Normalize(HighPass(Concat(parts), 2500f), 0.25f);
                }

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
