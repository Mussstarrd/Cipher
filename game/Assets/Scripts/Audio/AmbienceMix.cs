#nullable enable
using UnityEngine;

namespace Cipher.Game.Audio
{
    /// <summary>
    /// The mixing desk for the three looping beds, with no AudioSource in it.
    ///
    /// Three layers, and the split is deliberate:
    ///
    ///   WEATHER  rain or wind. Owned by the condition, constant for the whole position.
    ///   PLACE    which woodland this hour of day sounds like. Also owned by the condition.
    ///   CROWD    distant murmur, and the only one that moves. It rises with how many of them are
    ///            on the field, which makes it free tension: the player hears the wave getting
    ///            bigger before they can see it, and we already know the number.
    ///
    /// EVERYTHING HERE IS QUIET ON PURPOSE. A bed that can be pointed at is a bed that is too
    /// loud. The ceiling below keeps the three of them together under the same headroom rule the
    /// synthesised bank follows, so the gunfire always has somewhere to sit.
    /// </summary>
    public sealed class AmbienceMix
    {
        /// <summary>
        /// The most the three beds may sum to. The synthesis rule is a per-sound peak of 0.95;
        /// this is the same idea applied to the layer that is playing ALL the time, and it is far
        /// lower because a constant bed at 0.9 is a bed you have to shout over.
        /// </summary>
        public const float Ceiling = 0.60f;

        /// <summary>How loud the crowd may ever get. Distant means distant.</summary>
        public const float CrowdCeiling = 0.30f;

        // Weather and place change once per position, so they can take their time. The crowd is
        // a gameplay signal and has to keep up with a wave arriving without snapping.
        private const float BedRate = 0.55f;
        private const float CrowdRate = 1.4f;

        private float _weatherTarget, _placeTarget;

        public float Weather { get; private set; }
        public float Place { get; private set; }
        public float Crowd { get; private set; }

        /// <summary>Scales everything, for the master volume and for mute.</summary>
        public float Master { get; set; } = 1f;

        /// <summary>Sum of the three beds before <see cref="Master"/>. Never above <see cref="Ceiling"/>.</summary>
        public float Total => Weather + Place + Crowd;

        /// <summary>Points the two condition-owned beds at their new levels. They fade, not cut.</summary>
        public void SetCondition(WeatherProfile profile)
        {
            _weatherTarget = Mathf.Max(0f, profile.WeatherBedVolume);
            _placeTarget = Mathf.Max(0f, profile.PlaceBedVolume);
        }

        /// <summary>
        /// Snaps the beds to their targets instead of fading. For the first frame of a position,
        /// where a fade-up from silence just sounds like the audio was late.
        /// </summary>
        public void Snap()
        {
            Weather = _weatherTarget;
            Place = _placeTarget;
        }

        /// <summary>
        /// Advances the crossfades.
        /// </summary>
        /// <param name="dt">Seconds. Unscaled: adrenaline focus slows the world, not the weather.</param>
        /// <param name="crowdPressure">
        /// 0..1, how much horde is on the field. The caller already computes this for the
        /// synthesised horde bed; the same number drives this one.
        /// </param>
        public void Step(float dt, float crowdPressure)
        {
            if (dt < 0f) dt = 0f;

            Weather = Approach(Weather, _weatherTarget, BedRate, dt);
            Place = Approach(Place, _placeTarget, BedRate, dt);
            Crowd = Approach(Crowd, CrowdTarget(crowdPressure), CrowdRate, dt);

            // Duck the two static beds rather than the crowd when the field is busy. The murmur
            // is the one carrying information, so it wins; ducking it to protect the wind would
            // be the mix fighting the design.
            float total = Total;
            if (total > Ceiling && total > 1e-5f)
            {
                float room = Mathf.Max(0f, Ceiling - Crowd);
                float statics = Weather + Place;
                if (statics > 1e-5f)
                {
                    float k = Mathf.Clamp01(room / statics);
                    Weather *= k;
                    Place *= k;
                }
            }
        }

        /// <summary>
        /// How loud the murmur should be for a given pressure.
        ///
        /// Smoothstepped rather than linear so that three stragglers left over from the last wave
        /// are genuinely inaudible. The bed is supposed to mean "a lot of them, over there", and
        /// a linear curve makes it mean "someone is alive somewhere", which is always true.
        /// </summary>
        public static float CrowdTarget(float pressure)
        {
            float p = Mathf.Clamp01(pressure);
            return CrowdCeiling * (p * p * (3f - 2f * p));
        }

        /// <summary>Frame-rate independent exponential approach. Never overshoots.</summary>
        private static float Approach(float current, float target, float rate, float dt)
        {
            return Mathf.Lerp(current, target, 1f - Mathf.Exp(-rate * dt));
        }

        // ---- final levels, what the AudioSources are actually set to --------------------

        public float WeatherVolume => Weather * Master;
        public float PlaceVolume => Place * Master;
        public float CrowdVolume => Crowd * Master;
    }

    /// <summary>
    /// When the next distant thunder fires.
    ///
    /// Its own class because "randomly every so often" is exactly the kind of thing that ships
    /// firing twice in one frame or never firing at all, and neither is visible in a screenshot.
    /// Seeded: the same storm every time the position loads.
    /// </summary>
    public sealed class ThunderClock
    {
        private readonly float _min, _max;
        private uint _state;
        private float _next;
        private bool _armed;

        public ThunderClock(int seed, float minGap = 14f, float maxGap = 42f)
        {
            _min = minGap;
            _max = maxGap;
            _state = (uint)seed | 1u;
            _next = Roll();
        }

        /// <summary>Seconds until the next strike. Exposed for the tests.</summary>
        public float NextIn => _next;

        /// <summary>Whether thunder is possible at all; a clear sky never rolls.</summary>
        public bool Enabled
        {
            get => _armed;
            set
            {
                if (value == _armed) return;
                _armed = value;
                // Re-roll on arming so a storm does not open with an instant clap left over from
                // a previous position's countdown.
                if (value) _next = Roll();
            }
        }

        /// <summary>True on exactly the frame a strike should play. At most one per call.</summary>
        public bool Tick(float dt)
        {
            if (!_armed || dt <= 0f) return false;
            _next -= dt;
            if (_next > 0f) return false;
            _next = Roll();
            return true;
        }

        private float Roll()
        {
            _state ^= _state << 13; _state ^= _state >> 17; _state ^= _state << 5;
            float u = (_state & 0xFFFFFF) * (1f / 16777216f);
            return Mathf.Lerp(_min, _max, u);
        }
    }
}
