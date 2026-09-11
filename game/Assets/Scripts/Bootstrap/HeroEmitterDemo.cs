#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cipher.Game
{
    /// <summary>
    /// Puts all four rungs of the gun ladder on real ground, in a hand, firing, so they can be
    /// PHOTOGRAPHED side by side. Run the player with
    ///     ProjectExodus.exe -exodus-emitter-demo -exodus-screenshot art/review/emitters.png
    ///                       -exodus-screenshot-delay 6
    /// and add <c>-exodus-emitter-hands</c> for the silhouettes alone (no pulses, weapons turned
    /// side-on), or <c>-exodus-emitter-age 0..1</c> to choose the instant of the shot.
    ///
    /// WHY THE AGE FLAG IS THE IMPORTANT ONE. A pulse lives <see cref="SignalFx.BeamLife"/> =
    /// 0.22 seconds. Photographing four weapons at the same phase by choosing a shutter delay is
    /// guesswork, and "how do these four differ" is a question about ONE phase seen four times --
    /// four shots caught at four different ages tell you nothing. <see cref="SignalFx.MarkBeam"/>
    /// makes the phase a parameter and this component restates it every frame.
    ///
    /// It follows the two rules TurretPropsDemo and SignalFxDemo follow. It installs itself from a
    /// [RuntimeInitializeOnLoadMethod], so reviewing the weapon never means editing the composition
    /// root; and it stages into the REAL level under the real overcast lighting rather than
    /// building a studio, because a prop reviewed on a black background is a prop reviewed against
    /// a background the game does not have.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HeroEmitterDemo : MonoBehaviour
    {
        private const string Flag = "-exodus-emitter-demo";
        private const string HandsArg = "-exodus-emitter-hands";
        private const string AgeArg = "-exodus-emitter-age";

        /// <summary>
        /// Where in the pulse's life the held frame sits. A third of the way through is where the
        /// packet is longest and the tail has not yet caught the head up, which is the frame that
        /// shows the most of what makes one rung different from another.
        /// </summary>
        private const float DefaultAge = 0.34f;

        /// <summary>How far off the camera's axis the shots are fired, in degrees.</summary>
        private const float ShotAngle = 48f;

        /// <summary>How long each staged shot is, in metres.</summary>
        private const float ShotLength = 11f;

        /// <summary>How far apart the firing stations stand along the camera's axis.</summary>
        private const float Depth = 4.2f;

        /// <summary>Where the weapon rides on a hero-scaled capsule, in metres.</summary>
        private const float WeaponHeight = 1.06f;

        private static readonly string[] Names = { "FIELD JAMMER", "DECRYPTOR", "WORM LANCE", "CASCADE EMITTER" };

        private bool _handsOnly;
        private float _age = DefaultAge;
        private Camera? _camera;
        private bool _pinned;
        private float _elapsed;
        private SignalFx? _fx;
        private bool _ownsDraw;

        private Vector3 _forward, _right, _anchor, _shotDir;
        private readonly List<HeroEmitter> _built = new List<HeroEmitter>(4);
        private readonly List<Transform> _stands = new List<Transform>(4);
        private readonly Dictionary<Color, Material> _palette = new Dictionary<Color, Material>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallIfRequested()
        {
            var args = Environment.GetCommandLineArgs();
            if (args == null) return;
            foreach (var a in args)
            {
                if (!string.Equals(a, Flag, StringComparison.OrdinalIgnoreCase)) continue;
                var host = new GameObject("HeroEmitterDemo");
                DontDestroyOnLoad(host);
                var demo = host.AddComponent<HeroEmitterDemo>();
                demo._handsOnly = HasFlag(args, HandsArg);
                demo._age = ReadFloat(args, AgeArg, DefaultAge);
                Debug.Log($"[HeroEmitterDemo] staging the gun ladder, age {demo._age:0.00}" +
                          (demo._handsOnly ? ", hands only" : string.Empty));
                return;
            }
        }

        private static bool HasFlag(string[] args, string flag)
        {
            foreach (var a in args)
                if (string.Equals(a, flag, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static float ReadFloat(string[] args, string flag, float fallback)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (!string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase)) continue;
                if (float.TryParse(args[i + 1], System.Globalization.NumberStyles.Float,
                                   System.Globalization.CultureInfo.InvariantCulture, out float v))
                    return v;
            }
            return fallback;
        }

        private void Update()
        {
            if (_camera == null || !_camera.isActiveAndEnabled) _camera = FindLiveCamera();
            if (_camera == null) return;

            _elapsed += Time.deltaTime;

            if (!_pinned)
            {
                // Give the bootstrap a moment to put its camera behind the hero. A stage pinned on
                // frame zero lands wherever the scene template happened to start.
                if (_elapsed < 0.6f) return;

                _forward = _camera.transform.forward;
                _forward.y = 0f;
                if (_forward.sqrMagnitude < 1e-4f) _forward = Vector3.forward;
                _forward.Normalize();
                _right = Vector3.Cross(Vector3.up, _forward);
                // THE TWO COMPOSITIONS WANT DIFFERENT GROUND. The hardware shot wants the
                // middle of the open road, close in. The firing line wants to stand off to
                // one side, because its packets run obliquely for eleven metres and an anchor
                // straight ahead fires two of the four into somebody's house -- which is the
                // one thing that can make this photograph useless. Both were learned by
                // taking the picture.
                _anchor = _handsOnly
                    ? _camera.transform.position + _forward * 9f
                    : _camera.transform.position + _forward * 13f + _right * 8f;
                _anchor.y = 0f;

                // The shots run obliquely ACROSS the frame rather than away down it. A packet
                // photographed end-on is a bright dot: the whole point of this effect is a train
                // of fronts spaced along an axis, and an axis pointing at the lens has no length.
                _shotDir = Quaternion.AngleAxis(-ShotAngle, Vector3.up) * _forward;

                Build();
                TakeOverTheCamera();
                _pinned = true;
            }

            // The rig is restated every frame. The bootstrap is off, so nothing else is writing
            // to the camera -- but a demo that sets its shot once and hopes is a demo that
            // photographs whatever the last thing to touch the transform wanted.
            AimCamera();

            // Borrow the game's renderer if there is one; drive our own otherwise. Ticking a
            // renderer the game is already ticking would age every effect at double speed --
            // and once the bootstrap is disabled, NOBODY is ticking it but us.
            if (_fx == null)
            {
                var existing = SignalFx.Current;
                _fx = existing ?? new SignalFx();
                _ownsDraw = true;
            }

            if (!_handsOnly)
            {
                for (int i = 0; i < _built.Count; i++)
                {
                    var muzzle = _built[i].Muzzle;
                    if (muzzle == null) continue;
                    Vector3 from = muzzle.position;
                    Vector3 to = from + _shotDir * ShotLength + Vector3.up * 0.05f;
                    _fx.MarkBeam(from, to, SignalBeam.Emitter, landed: true, tier: i, age01: _age);
                }
            }

            if (_ownsDraw) _fx.Draw(Time.deltaTime);
        }

        /// <summary>
        /// Switches the running match OFF.
        ///
        /// NOT tidiness. The bootstrap rewrites the camera every frame from the hero's position,
        /// so a demo that wants a chosen angle is fighting it; and it draws the whole HUD from
        /// OnGUI, so the first capture of this stage came back with the mission brief, the health
        /// bar and the objectives panel printed across the weapons being reviewed. Disabling the
        /// component stops both, and freezing the world mid-frame is exactly what a review stage
        /// wants anyway.
        /// </summary>
        private void TakeOverTheCamera()
        {
            var boot = FindFirstObjectByType<FloodBootstrap>();
            if (boot != null)
            {
                boot.enabled = false;
                Debug.Log("[HeroEmitterDemo] match paused for the stage");
            }
        }

        private void AimCamera()
        {
            if (_camera == null) return;

            // Two compositions, because the two halves of the owner's note are two different
            // photographs. The hardware wants to be close enough to read the parts; the shot
            // wants to be far enough back that ten metres of packet fits in the frame.
            Vector3 target, eye;
            if (_handsOnly)
            {
                target = _anchor + Vector3.up * (WeaponHeight + 0.04f);
                eye = target - _forward * 5.6f + Vector3.up * 0.68f;
            }
            else
            {
                var centre = _anchor + _forward * (Depth * 1.5f) + Vector3.up * WeaponHeight;
                target = centre + _shotDir * 3.6f;
                eye = centre - _forward * 12f + Vector3.up * 8.5f;
            }

            _camera.transform.position = eye;
            _camera.transform.rotation = Quaternion.LookRotation((target - eye).normalized, Vector3.up);
        }

        private void Build()
        {
            for (int i = 0; i < Names.Length; i++)
            {
                // In the hands-only shot the four stand shoulder to shoulder; firing, they are
                // staggered in depth as well as across, so four parallel shots fired obliquely do
                // not fly through the next man along.
                var at = _handsOnly
                    ? _anchor + _right * ((i - 1.5f) * 2.6f)
                    : _anchor + _right * ((i - 1.5f) * 4.6f) + _forward * (i * Depth);

                // A stand-in for the body, at the hero capsule's own scale. The weapon is judged
                // AT that size, held at that height, or it is not being judged at all -- an
                // emitter photographed alone on the grass is a model review, not a weapon review.
                // Deliberately a drab olive and NOT the hero capsule's yellow: a stand-in painted
                // the weapon's own amber is the one colour that makes the emitter unreadable.
                var stand = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                stand.name = "Stand" + i;
                DestroyNow(stand.GetComponent<Collider>());
                stand.transform.position = at + Vector3.up * 0.9f;
                stand.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);

                // Turned FULL PROFILE for the hardware shot, and turned that way round on
                // purpose: the emitter hangs off the body's right, so this is the one heading
                // that puts the weapon between the camera and the capsule instead of behind it.
                // The first capture of this stage was four capsules with the weapons hidden.
                var facing = _handsOnly
                    ? Quaternion.AngleAxis(90f, Vector3.up) * _forward
                    : _shotDir;
                stand.transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
                stand.GetComponent<Renderer>().sharedMaterial =
                    PropMaterial(new Color(0.30f, 0.30f, 0.27f));
                _stands.Add(stand.transform);

                var emitter = HeroEmitter.Build(stand.transform, PropMaterial);
                emitter.SetTier(i);
                _built.Add(emitter);
            }
        }

        /// <summary>
        /// Names under the weapons. A four-up of unlabelled hardware makes the reviewer count
        /// left to right and hope, and the whole question being asked of this image is "which of
        /// these is which".
        /// </summary>
        private void OnGUI()
        {
            if (!_pinned || _camera == null) return;
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fontStyle = FontStyle.Bold,
            };
            style.normal.textColor = Color.white;

            for (int i = 0; i < _stands.Count && i < Names.Length; i++)
            {
                Vector3 sp = _camera.WorldToScreenPoint(_stands[i].position + Vector3.down * 1.1f);
                if (sp.z <= 0f) continue;
                var r = new Rect(sp.x - 130f, Screen.height - sp.y - 12f, 260f, 24f);
                GUI.Label(new Rect(r.x + 1f, r.y + 1f, r.width, r.height), Names[i], Shadow(style));
                GUI.Label(r, Names[i], style);
            }
        }

        private static GUIStyle Shadow(GUIStyle from)
        {
            var s = new GUIStyle(from);
            s.normal.textColor = new Color(0f, 0f, 0f, 0.75f);
            return s;
        }

        /// <summary>
        /// The same cached-by-colour prop material the bootstrap hands its props, rebuilt here
        /// because the bootstrap's is private and this component must not need wiring into it.
        /// </summary>
        private Material PropMaterial(Color colour)
        {
            if (_palette.TryGetValue(colour, out var cached)) return cached;

            var shader = Shader.Find("Exodus/InstancedLit");
            var made = new Material(shader != null ? shader : Shader.Find("Sprites/Default"));
            made.color = colour;
            if (made.HasProperty("_BaseColor")) made.SetColor("_BaseColor", colour);
            if (made.HasProperty("_OutlineWidth")) made.SetFloat("_OutlineWidth", FloodBootstrap.InkProp);
            _palette[colour] = made;
            return made;
        }

        private static Camera? FindLiveCamera()
        {
            var cams = Camera.allCameras;
            foreach (var c in cams)
                if (c != null && c.isActiveAndEnabled && c.targetTexture == null) return c;
            return Camera.main;
        }

        private static void DestroyNow(UnityEngine.Object? victim)
        {
            if (victim == null) return;
            if (Application.isPlaying) Destroy(victim);
            else DestroyImmediate(victim);
        }

        private void OnDestroy()
        {
            foreach (var pair in _palette)
                if (pair.Value != null) Destroy(pair.Value);
            _palette.Clear();
        }
    }
}
