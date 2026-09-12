#nullable enable
using Cipher.Game.Audio;
using UnityEngine;

namespace Cipher.Game
{
    /// <summary>
    /// Everything about the sky and the air, behind one object.
    ///
    /// WHY A FACADE. The weather is four systems that have to agree -- the lighting, the fog and
    /// sky, the rain, and the audio beds -- and the one thing that must never happen is three of
    /// them changing at a position boundary and the fourth not. Making the bootstrap call four
    /// objects in the right order at three different places is how that bug gets written. So it
    /// calls one object at three places, and the ordering lives here where it can be read.
    ///
    /// The composition root owns exactly three lines of this:
    ///
    ///     _atmos = Atmosphere.Create(transform, _camera, _scenario.DirectorSeed);   // build
    ///     _atmos.Reseed(_scenario.DirectorSeed, _camera);                           // new position
    ///     _atmos.Update(Time.unscaledDeltaTime, _camera, _hordeIntensity, heroPos); // per frame
    /// </summary>
    public sealed class Atmosphere
    {
        private readonly Ambience _ambience;
        private readonly RainFall _rain;
        private Light? _sun;

        private Vector3 _lastHeroPos;
        private bool _haveHero;
        private float _heroSpeed;

        /// <summary>The condition in force. Logged at every position change; read by the HUD.</summary>
        public Sky Sky { get; private set; }

        /// <summary>The full profile, so callers can ask what the weather implies.</summary>
        public WeatherProfile Profile { get; private set; } = Weather.Profile(Sky.DuskClear);

        /// <summary>The directional light, for anything that needs to aim shadows at it.</summary>
        public Light? Sun => _sun;

        public Ambience Audio => _ambience;

        /// <summary>Smoothed hero ground speed in m/s, derived here so the caller need not.</summary>
        public float HeroSpeed => _heroSpeed;

        private Atmosphere(Transform parent, int audioSeed)
        {
            _ambience = new Ambience(parent, audioSeed);
            _rain = new RainFall(audioSeed);
        }

        /// <summary>Builds the atmosphere and applies the seeded condition immediately.</summary>
        public static Atmosphere Create(Transform parent, Camera? camera, ulong seed)
        {
            var atmos = new Atmosphere(parent, unchecked((int)seed));
            atmos.Set(Weather.Resolve(seed), camera, snapAudio: true);
            return atmos;
        }

        /// <summary>
        /// Re-rolls for a new position. Call this from wherever the scenario changes, alongside
        /// the other things that have to be re-pointed when the map does.
        /// </summary>
        public void Reseed(ulong seed, Camera? camera)
        {
            // Not snapped: a position change is a continuous moment for the player (they drove
            // here), so the beds crossfade rather than cut.
            Set(Weather.Resolve(seed), camera, snapAudio: false);
        }

        /// <summary>Forces a named condition. For the screenshot harness and for debugging.</summary>
        public void Force(Sky sky, Camera? camera) => Set(sky, camera, snapAudio: true);

        private void Set(Sky sky, Camera? camera, bool snapAudio)
        {
            Sky = sky;
            Profile = Weather.Profile(sky);

            // Order matters only in that the sun must exist before anything reads it; the rest
            // are independent, and they are here together so they cannot drift apart.
            _sun = Weather.Apply(Profile, camera, _sun);
            _rain.SetWeather(Profile);
            _ambience.SetWeather(Profile, snapAudio);

            Debug.Log($"[Weather] {Profile.Name} (fog {Profile.FogDensity:0.0000}, rain {Profile.Rain:0.0})");
        }

        /// <summary>
        /// One call a frame, from the composition root's update.
        /// </summary>
        /// <param name="unscaledDt">
        /// MUST be unscaled. Adrenaline focus slows the world; slowing the rain and the wind with
        /// it turns a tactical pause into an underwater one.
        /// </param>
        /// <param name="crowdPressure">0..1, the same figure the synthesised horde bed is given.</param>
        /// <param name="heroPos">Hero position in world space; the footstep speed is derived from it.</param>
        public void Update(float unscaledDt, Camera? camera, float crowdPressure, Vector3 heroPos)
        {
            if (unscaledDt > 0f)
            {
                if (_haveHero)
                {
                    var delta = heroPos - _lastHeroPos;
                    delta.y = 0f;
                    float instant = delta.magnitude / unscaledDt;

                    // A position change teleports the hero across the map in one frame. Without
                    // this the cadence would fire a burst of steps on arrival; the threshold is
                    // far above any speed the hero can reach under their own power.
                    if (instant > 40f) instant = 0f;

                    // Smoothed, because a per-frame delta is noisy enough to flicker across the
                    // walk/stand threshold and stutter the footsteps.
                    _heroSpeed = Mathf.Lerp(_heroSpeed, instant, 1f - Mathf.Exp(-12f * unscaledDt));
                }
                _lastHeroPos = heroPos;
                _haveHero = true;
            }

            _rain.Update(unscaledDt, camera);
            _ambience.Update(unscaledDt, crowdPressure, _heroSpeed);
        }

        /// <summary>Mutes the whole organic layer, for the pause menu's master control.</summary>
        public void SetMuted(bool muted) => _ambience.Muted = muted;

        /// <summary>Master level for the organic layer, matched to the SoundBank's.</summary>
        public void SetVolume(float volume) => _ambience.MasterVolume = Mathf.Clamp01(volume);
    }
}
