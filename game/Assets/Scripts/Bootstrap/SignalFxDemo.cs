#nullable enable
using System;
using UnityEngine;

namespace Cipher.Game
{
    /// <summary>
    /// Stages all four signal effects on a patch of ground in front of the camera, so they can be
    /// PHOTOGRAPHED. Run the player with
    ///     ProjectExodus.exe -exodus-signalfx-demo -exodus-screenshot art/review/fx.png
    ///                       -exodus-screenshot-delay 7.1 [-exodus-signalfx-age 0.12]
    ///
    /// <c>-exodus-signalfx-age</c> FREEZES the EMP at a given fraction of its life instead of
    /// letting it cycle, and it is the flag that matters. A burst lives 1.15 seconds and its
    /// electrical half is over inside the first half of that, so picking a shutter delay means
    /// guessing at a phase; three rounds of tuning here went into stages that were never actually
    /// in frame, and the burst looked like glassware because only its aftermath had been seen.
    ///
    /// This exists because of the rule that cost this project the most time: verify rendering,
    /// never infer it. Compiling is not rendering, a green test suite is not rendering, and an
    /// effect that lives for a fifth of a second cannot be reviewed by playing the game and
    /// squinting. The EMP's predecessor was on screen for 0.45 seconds and was never once caught
    /// on film, which is how it shipped wrong.
    ///
    /// Two things it deliberately does NOT do. It does not add itself to the bootstrap -- it
    /// installs from a [RuntimeInitializeOnLoadMethod] so that reviewing an effect never requires
    /// editing the composition root, which is the file most likely to be in someone else's hands.
    /// And it does not build a scene of its own: it stages into the REAL level, under the real
    /// overcast lighting and against the real brown ground, because an effect reviewed on a black
    /// background is an effect reviewed against a background the game does not have.
    ///
    /// The stage is pinned to where the camera was on the first frame rather than followed, so a
    /// capture taken at eight seconds frames the same ground as one taken at five.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SignalFxDemo : MonoBehaviour
    {
        private const string Flag = "-exodus-signalfx-demo";
        private const string AgeArg = "-exodus-signalfx-age";
        private const string TellArg = "-exodus-signalfx-tell";

        /// <summary>
        /// Seconds between demo bursts. The capture's age within a burst is therefore
        /// <c>(delay - FirstBurst) mod EmpPeriod</c>, which is how a specific stage of the EMP gets
        /// photographed on purpose instead of by luck.
        /// </summary>
        public const float EmpPeriod = 2f;
        public const float FirstBurst = 0.6f;

        /// <summary>
        /// Beams are fired far faster than a weapon would, so that several are always alive at
        /// different ages at once. One frame then shows the head in flight, the dash train behind
        /// it and a terminal burst landing -- the whole life of the effect in a single still.
        /// </summary>
        private const float BeamPeriod = 0.055f;

        /// <summary>The five held decay stages, so the failing tell reads as a ramp, not a state.</summary>
        private static readonly float[] FailStages = { 1.0f, 0.72f, 0.48f, 0.24f, 0.06f };

        private SignalFx? _fx;
        private Camera? _camera;
        private bool _pinned;
        private Vector3 _anchor, _forward, _right;
        private float _elapsed, _empNext = FirstBurst, _beamNext;

        /// <summary>Frozen age, or negative to let the burst run on its own clock.</summary>
        private float _heldAge = -1f;

        /// <summary>
        /// Close-up mode: the failing bodies only, five metres away and nothing else on the
        /// stage. The tell is the subtlest of the four effects -- an implant the size of a
        /// thumbnail and a ring that tightens -- and at the twenty metres the wide shot uses it
        /// is a handful of pixels, which is not enough to judge it by.
        /// </summary>
        private bool _tellOnly;
        private readonly Transform?[] _dummies = new Transform?[FailStages.Length];
        private Material? _dummyMaterial;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallIfRequested()
        {
            var args = Environment.GetCommandLineArgs();
            if (args == null) return;
            foreach (var a in args)
            {
                if (!string.Equals(a, Flag, StringComparison.OrdinalIgnoreCase)) continue;
                var host = new GameObject("SignalFxDemo");
                DontDestroyOnLoad(host);
                var demo = host.AddComponent<SignalFxDemo>();
                demo._heldAge = HeldAge(args);
                demo._tellOnly = HasFlag(args, TellArg);
                Debug.Log("[SignalFxDemo] staging EMP, emitter lance, failing implants and jammer field");
                return;
            }
        }

        private static bool HasFlag(string[] args, string flag)
        {
            if (args == null) return false;
            foreach (var a in args)
                if (string.Equals(a, flag, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>The frozen EMP age from the command line, or -1 when it was not asked for.</summary>
        public static float HeldAge(string[] args)
        {
            if (args == null) return -1f;
            for (int i = 0; i + 1 < args.Length; i++)
            {
                if (!string.Equals(args[i], AgeArg, StringComparison.OrdinalIgnoreCase)) continue;
                if (float.TryParse(args[i + 1], System.Globalization.NumberStyles.Float,
                                   System.Globalization.CultureInfo.InvariantCulture, out float a)
                    && a >= 0f && a <= 1f)
                {
                    return a;
                }
            }
            return -1f;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;

            if (_camera == null || !_camera.isActiveAndEnabled) _camera = FindLiveCamera();
            if (_camera == null) return;

            // Give the bootstrap a moment to place its camera behind the hero before pinning; a
            // stage pinned on frame zero lands wherever the scene template happened to start.
            if (!_pinned)
            {
                if (_elapsed < 0.5f) return;
                _forward = _camera.transform.forward;
                _forward.y = 0f;
                if (_forward.sqrMagnitude < 1e-4f) _forward = Vector3.forward;
                _forward.Normalize();
                _right = Vector3.Cross(Vector3.up, _forward);
                _anchor = _camera.transform.position + _forward * (_tellOnly ? 9f : 20f);
                _anchor.y = 0f;
                _pinned = true;
                BuildDummies();
            }

            // Borrow the game's renderer if the bootstrap has been wired up, build one otherwise.
            // Which of the two happened decides who ticks it: driving Draw on a renderer the game
            // is already ticking would age every effect at twice speed, and an EMP that lives half
            // as long as the code says is exactly the kind of thing a screenshot cannot reveal.
            if (_fx == null)
            {
                var existing = SignalFx.Current;
                _ownsDraw = existing == null;
                _fx = existing ?? new SignalFx();
            }

            // Laid out as the WEAPON'S OWN STORY, left to right, rather than as four unrelated
            // props: a pulse going off on the far left, lances crossing the middle and landing on
            // the row of bodies, those bodies stepping through the whole decay, and an
            // emplacement's field holding ground on the right. One still then answers the only
            // question worth asking of this work -- does it read as attacking the chip?
            var empAt = _anchor - _right * 9f + _forward * 1f + Vector3.up * 0.3f;
            if (_tellOnly)
            {
                // nothing but the bodies
            }
            else if (_heldAge >= 0f)
            {
                _fx.MarkEmp(empAt, 3.5f, _heldAge, seed: 4242);
            }
            else if (_elapsed >= _empNext)
            {
                _empNext += EmpPeriod;
                _fx.AddEmp(empAt, 3.5f);
            }

            if (!_tellOnly && _elapsed >= _beamNext)
            {
                _beamNext = _elapsed + BeamPeriod;
                // Both lances terminate ON a dummy's head, so the terminal burst and the failing
                // tell are photographed in the same place, which is where they happen in play.
                var hitA = HeadOf(0);
                _fx.AddBeam(_anchor - _right * 16f + _forward * 6f + Vector3.up * 1.4f, hitA, SignalBeam.Emitter, landed: true);

                var hitB = HeadOf(4);
                _fx.AddBeam(_anchor + _right * 15f + Vector3.up * 1.2f, hitB, SignalBeam.Turret, landed: true);
            }

            // Held decay stages. Each dummy keeps its own fail01 forever, so the still shows the
            // whole two and a half seconds of the tell laid out left to right.
            for (int i = 0; i < FailStages.Length; i++)
            {
                var t = _dummies[i];
                if (t == null) continue;
                _fx.MarkFailing(HeadOf(i), _forward, FailStages[i], 1000 + i * 7);
            }

            if (!_tellOnly) _fx.MarkJammer(_anchor + _right * 13f + _forward * 2f, 6.5f, 1f);

            if (_ownsDraw) _fx.Draw(Time.deltaTime);
        }

        private bool _ownsDraw;

        /// <summary>Head height of one dummy, which is where both the lances and the tell land.</summary>
        private Vector3 HeadOf(int index)
        {
            var t = _dummies[index];
            return t == null ? _anchor : t.position + Vector3.up * 0.92f;
        }

        private static Camera? FindLiveCamera()
        {
            var cams = Camera.allCameras;
            foreach (var c in cams)
                if (c != null && c.isActiveAndEnabled && c.targetTexture == null) return c;
            return Camera.main;
        }

        /// <summary>
        /// Five capsules standing in for bodies. CAPSULES ON PURPOSE: most of the crowd at any
        /// distance is an instanced capsule, so if the tell does not read on one of these it does
        /// not read for the majority of the enemies the player will ever shoot.
        /// </summary>
        private void BuildDummies()
        {
            var shader = Shader.Find("Exodus/InstancedLit");
            if (shader != null)
            {
                _dummyMaterial = new Material(shader);
                _dummyMaterial.color = new Color(0.42f, 0.40f, 0.38f);
                if (_dummyMaterial.HasProperty("_BaseColor"))
                    _dummyMaterial.SetColor("_BaseColor", new Color(0.42f, 0.40f, 0.38f));
                if (_dummyMaterial.HasProperty("_OutlineWidth"))
                    _dummyMaterial.SetFloat("_OutlineWidth", FloodBootstrap.InkCharacter);
            }

            for (int i = 0; i < FailStages.Length; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.name = $"FailDummy{i}";
                Destroy(go.GetComponent<Collider>());
                go.transform.SetParent(transform, false);
                float spread = _tellOnly ? 1.5f : 3.2f;
                float back = _tellOnly ? 3f : 7f;
                go.transform.position = _anchor + _right * ((i - 2) * spread) - _forward * back + Vector3.up * 0.9f;
                go.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
                if (_dummyMaterial != null) go.GetComponent<Renderer>().sharedMaterial = _dummyMaterial;
                _dummies[i] = go.transform;
            }
        }

        private void OnDestroy()
        {
            if (_dummyMaterial != null) Destroy(_dummyMaterial);
        }
    }
}
