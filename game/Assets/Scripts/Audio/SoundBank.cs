#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Cipher.Game.Audio
{
    /// <summary>
    /// Unity side of the procedural audio: wraps the recipes in AudioClips and plays them
    /// through small pools of 2D and 3D sources, with pitch variation and per-sound rate
    /// limits so 12 shots a second and 30 turret rounds do not turn into white noise.
    /// </summary>
    public sealed class SoundBank
    {
        private const int Pool2D = 10;
        private const int Pool3D = 12;

        private readonly Dictionary<Sfx, AudioClip> _clips = new Dictionary<Sfx, AudioClip>();
        private readonly Dictionary<Sfx, float> _lastPlayed = new Dictionary<Sfx, float>();
        private readonly AudioSource[] _flat;
        private readonly AudioSource[] _spatial;
        private readonly AudioSource _wind;
        private readonly AudioSource _horde;
        private int _flatNext, _spatialNext;
        private float _hordeTarget;

        public float MasterVolume { get; set; } = 1f;
        public bool Muted { get; set; }

        public SoundBank(Transform parent)
        {
            foreach (var kv in SoundRecipes.BuildAll())
                _clips[kv.Key] = ToClip(kv.Key.ToString(), kv.Value);

            var root = new GameObject("SoundBank");
            root.transform.SetParent(parent, false);

            _flat = new AudioSource[Pool2D];
            for (int i = 0; i < Pool2D; i++) _flat[i] = MakeSource(root, "sfx2d", spatial: false);
            _spatial = new AudioSource[Pool3D];
            for (int i = 0; i < Pool3D; i++) _spatial[i] = MakeSource(root, "sfx3d", spatial: true);

            _wind = MakeSource(root, "wind", spatial: false);
            _wind.clip = ToClip("WindLoop", SoundRecipes.WindLoop());
            _wind.loop = true;
            _wind.volume = 0.22f;
            _wind.Play();

            _horde = MakeSource(root, "horde", spatial: false);
            _horde.clip = ToClip("HordeLoop", SoundRecipes.HordeLoop());
            _horde.loop = true;
            _horde.volume = 0f;
            _horde.Play();
        }

        private static AudioSource MakeSource(GameObject root, string name, bool spatial)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = spatial ? 1f : 0f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 6f;
            src.maxDistance = 45f;
            src.dopplerLevel = 0f;
            return src;
        }

        private static AudioClip ToClip(string name, float[] samples)
        {
            var clip = AudioClip.Create(name, samples.Length, 1, Waveforms.SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>Plays a 2D sound. <paramref name="minInterval"/> drops repeats closer than that.</summary>
        public void Play(Sfx sfx, float volume = 1f, float pitchJitter = 0.06f, float minInterval = 0f)
        {
            if (Muted || !Ready(sfx, minInterval)) return;
            var src = _flat[_flatNext++ % Pool2D];
            src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            src.PlayOneShot(_clips[sfx], volume * MasterVolume);
        }

        /// <summary>Plays a positioned sound in world space.</summary>
        public void PlayAt(Sfx sfx, Vector3 position, float volume = 1f, float pitchJitter = 0.06f, float minInterval = 0f)
        {
            if (Muted || !Ready(sfx, minInterval)) return;
            var src = _spatial[_spatialNext++ % Pool3D];
            src.transform.position = position;
            src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            src.PlayOneShot(_clips[sfx], volume * MasterVolume);
        }

        private bool Ready(Sfx sfx, float minInterval)
        {
            if (minInterval <= 0f) return true;
            float now = Time.unscaledTime;
            if (_lastPlayed.TryGetValue(sfx, out float last) && now - last < minInterval) return false;
            _lastPlayed[sfx] = now;
            return true;
        }

        /// <summary>0..1 how loud the horde bed should be; smoothed so it swells rather than snaps.</summary>
        public void SetHordeIntensity(float intensity)
        {
            _hordeTarget = Mathf.Clamp01(intensity);
        }

        public void Update(float dt)
        {
            if (Muted) { _horde.volume = 0f; _wind.volume = 0f; return; }
            float target = _hordeTarget * 0.7f * MasterVolume;
            _horde.volume = Mathf.Lerp(_horde.volume, target, 1f - Mathf.Exp(-2.5f * dt));
            _horde.pitch = 0.85f + 0.3f * _hordeTarget;
            _wind.volume = Muted ? 0f : 0.22f * MasterVolume;
        }
    }
}
