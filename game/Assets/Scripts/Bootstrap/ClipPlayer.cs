#nullable enable
using UnityEngine;

namespace Cipher.Game
{
    /// <summary>
    /// Plays one animation clip by sampling it directly, with no Animator and no avatar.
    ///
    /// Why not an AnimatorController: these characters import as a Generic rig, and generic
    /// retargeting binds through an avatar. With the avatar missing the character silently stays in
    /// its bind pose; with an avatar copied from the shared clip library it ALSO silently stays in
    /// its bind pose. Nothing errors either way, which is the worst kind of failure: it looks exactly
    /// like an import that simply was not told to move.
    ///
    /// AnimationClip.SampleAnimation binds by transform PATH instead, needs no avatar, and was
    /// already proven to work on these models by the crowd baker. So use the thing that works.
    ///
    /// Cost: this is per-instance CPU sampling, fine for the dozens a demo level needs and wrong for
    /// a thousand. The thousand-agent path is the vertex-animation-texture bake (ADR-007); this keeps
    /// real people on screen until that lands.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ClipPlayer : MonoBehaviour
    {
        [SerializeField] private AnimationClip? clip;

        /// <summary>Seconds offset into the loop, so a crowd does not march in lockstep.</summary>
        [SerializeField] private float phase;

        /// <summary>Playback rate. Slight per-agent variation stops the crowd looking mechanical.</summary>
        [SerializeField] private float speed = 1f;

        private float _time;

        public AnimationClip? Clip
        {
            get => clip;
            set { clip = value; _time = 0f; }
        }

        public void Configure(AnimationClip? source, float phaseSeconds, float rate)
        {
            clip = source;
            phase = phaseSeconds;
            speed = Mathf.Max(0.05f, rate);
            _time = phase;
        }

        private void Start()
        {
            if (clip == null) return;
            _time = phase;
            clip.SampleAnimation(gameObject, _time);
        }

        private void Update()
        {
            if (clip == null || clip.length <= 0f) return;

            _time += Time.deltaTime * speed;
            if (_time >= clip.length) _time %= clip.length;

            clip.SampleAnimation(gameObject, _time);
        }
    }
}
