#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Cipher.Game.Audio
{
    /// <summary>
    /// The organic half of the game's audio: weather, place, distant crowd, footsteps.
    ///
    /// THIS DOES NOT REPLACE THE SYNTHESIS AND MUST NOT. <see cref="SoundBank"/> generates every
    /// weapon, EMP, breach and menu sound at startup, which is right for them: they are synthetic
    /// objects by nature and being generated is what lets them answer to tier and distance. What
    /// synthesis is hopeless at is rain on leaves, a boot in gravel and two hundred people
    /// murmuring a street away, and that is exactly this file. The two live side by side.
    ///
    /// Every clip here is CC0 or CC-BY, fetched by tools/audio/fetch-sounds.py and credited in
    /// docs/CREDITS-AUDIO.md by that script. Nothing in this directory may be added by hand.
    /// </summary>
    public sealed class Ambience
    {
        private const string Root = "Audio/";

        private readonly AmbienceMix _mix = new AmbienceMix();
        private readonly ThunderClock _thunder;
        private readonly FootstepCadence _steps;

        private readonly AudioSource _weatherBed;
        private readonly AudioSource _placeBed;
        private readonly AudioSource _crowdBed;
        private readonly AudioSource[] _oneShots;
        private int _oneShotNext;

        private readonly AudioClip[] _thunderClips;
        private readonly AudioClip[] _stepClips;
        private readonly AudioClip[] _rustleClips;

        private readonly Dictionary<string, AudioClip?> _beds = new Dictionary<string, AudioClip?>();

        private string _weatherSlot = "";
        private string _placeSlot = "";
        private int _stepsSinceRustle;

        public float MasterVolume { get => _mix.Master; set => _mix.Master = value; }
        public bool Muted { get; set; }

        /// <summary>The mixing desk, for tests and for the debug overlay.</summary>
        public AmbienceMix Mix => _mix;

        public Ambience(Transform parent, int seed)
        {
            _thunder = new ThunderClock(seed);
            _thunderClips = Load("weather/thunder-distant");
            _stepClips = Load("foley/step-gravel");
            _rustleClips = Load("foley/cloth-rustle");
            _steps = new FootstepCadence(Mathf.Max(1, _stepClips.Length), seed);

            var root = new GameObject("Ambience");
            root.transform.SetParent(parent, false);

            _weatherBed = MakeLoop(root, "bed-weather");
            _placeBed = MakeLoop(root, "bed-place");
            _crowdBed = MakeLoop(root, "bed-crowd");

            // Four is enough: footsteps are rate-limited to ~6/s and thunder is minutes apart, so
            // the only way to need a fifth is a bug.
            _oneShots = new AudioSource[4];
            for (int i = 0; i < _oneShots.Length; i++) _oneShots[i] = MakeOneShot(root, "oneshot");

            var murmur = Pick("crowd/crowd-murmur-distant");
            if (murmur != null) { _crowdBed.clip = murmur; _crowdBed.Play(); }
        }

        private static AudioSource MakeLoop(GameObject root, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = true;
            src.spatialBlend = 0f;      // beds are the room, not a point in it
            src.volume = 0f;            // everything fades up; nothing starts audible
            src.dopplerLevel = 0f;
            return src;
        }

        private static AudioSource MakeOneShot(GameObject root, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            // The player's own boots and the sky overhead are both 2D. Panning your own
            // footsteps is a well-known way to make a third-person camera feel wrong.
            src.spatialBlend = 0f;
            src.dopplerLevel = 0f;
            return src;
        }

        /// <summary>All clips in a slot folder, or an empty array when the folder is missing.</summary>
        private static AudioClip[] Load(string slot)
        {
            var clips = Resources.LoadAll<AudioClip>(Root + slot);
            if (clips == null || clips.Length == 0)
            {
                Debug.LogWarning($"[Ambience] no clips under Resources/{Root}{slot}; that layer will be silent.");
                return System.Array.Empty<AudioClip>();
            }
            return clips;
        }

        /// <summary>The bed clip for a slot, loaded once and remembered.</summary>
        private AudioClip? Pick(string slot)
        {
            if (_beds.TryGetValue(slot, out var cached)) return cached;
            var clips = Load(slot);
            var chosen = clips.Length > 0 ? clips[0] : null;
            _beds[slot] = chosen;
            return chosen;
        }

        /// <summary>
        /// Points the beds at a new condition. Swapping the CLIP is a cut, so it only happens when
        /// the slot actually changed; the LEVEL always crossfades.
        /// </summary>
        /// <param name="snap">
        /// True on the first frame of a position: fading up from silence over two seconds just
        /// sounds like the audio was late to start.
        /// </param>
        public void SetWeather(WeatherProfile profile, bool snap = false)
        {
            _mix.SetCondition(profile);
            _thunder.Enabled = profile.Thunder;

            SwapBed(_weatherBed, ref _weatherSlot, profile.WeatherBed);
            SwapBed(_placeBed, ref _placeSlot, profile.PlaceBed);

            if (snap) _mix.Snap();
        }

        private void SwapBed(AudioSource src, ref string current, string wanted)
        {
            if (current == wanted && src.clip != null) return;
            var clip = Pick(wanted);
            current = wanted;
            if (clip == null) { src.Stop(); src.clip = null; return; }
            src.clip = clip;
            // Start somewhere random in the file. Two positions in a row under the same wind bed
            // otherwise open on the identical gust, which is the tell that it is a loop.
            src.time = Random.Range(0f, Mathf.Max(0.01f, clip.length - 0.5f));
            src.Play();
        }

        /// <summary>
        /// One call a frame.
        /// </summary>
        /// <param name="dt">UNSCALED seconds. Adrenaline focus slows the world; the rain is not in it.</param>
        /// <param name="crowdPressure">0..1 horde on the field — the same number the horde bed uses.</param>
        /// <param name="heroSpeed">Ground speed in m/s, for the footstep cadence.</param>
        public void Update(float dt, float crowdPressure, float heroSpeed)
        {
            if (Muted)
            {
                _weatherBed.volume = _placeBed.volume = _crowdBed.volume = 0f;
                return;
            }

            _mix.Step(dt, crowdPressure);
            _weatherBed.volume = _mix.WeatherVolume;
            _placeBed.volume = _mix.PlaceVolume;
            _crowdBed.volume = _mix.CrowdVolume;

            // The murmur pitches down a touch as it swells. A big crowd is lower than a small one
            // and it costs a float to say so.
            _crowdBed.pitch = 1.0f - 0.08f * Mathf.Clamp01(crowdPressure);

            if (_thunder.Tick(dt)) PlayOneShot(_thunderClips, 0.38f, 0.08f);

            int step = _steps.Tick(dt, heroSpeed);
            if (step >= 0 && _stepClips.Length > 0)
            {
                // Quiet. Footsteps are for presence, not for information; a player who notices
                // them is a player whose gunfight has gone quiet.
                PlayClip(_stepClips[step % _stepClips.Length], 0.30f, _steps.Pitch);

                // Gear rustle every few steps rather than every one. Cloth on every footfall is
                // the sound of a costume, not a man carrying a rifle.
                if (++_stepsSinceRustle >= 3)
                {
                    _stepsSinceRustle = 0;
                    PlayOneShot(_rustleClips, 0.16f, 0.10f);
                }
            }
        }

        private void PlayOneShot(AudioClip[] bank, float volume, float jitter)
        {
            if (bank.Length == 0) return;
            var clip = bank[Random.Range(0, bank.Length)];
            PlayClip(clip, volume, 1f + Random.Range(-jitter, jitter));
        }

        private void PlayClip(AudioClip clip, float volume, float pitch)
        {
            var src = _oneShots[_oneShotNext++ % _oneShots.Length];
            src.pitch = pitch;
            src.PlayOneShot(clip, volume * _mix.Master);
        }
    }
}
