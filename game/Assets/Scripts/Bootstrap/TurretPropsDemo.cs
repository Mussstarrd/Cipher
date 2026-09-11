#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cipher.Game
{
    /// <summary>
    /// Lines both emplacement families up on real ground so they can be PHOTOGRAPHED. Run the
    /// player with
    ///     ProjectExodus.exe -exodus-turret-demo -exodus-screenshot art/review/turrets.png
    ///                       -exodus-screenshot-delay 6
    ///
    /// Add <c>-exodus-turret-demo-damage</c> for the same eight emplacements at three states of
    /// repair instead of three tiers.
    ///
    /// WHY THIS EXISTS RATHER THAN -exodus-screenshot-emplacements. That flag puts turrets on the
    /// board mid-match, which is the right shot for judging whether they read in a firefight and
    /// the wrong one for judging the OBJECTS: they land where the build rules allow, at whatever
    /// tier the player could afford, at full health, with bodies in front of them. Tier and damage
    /// are two of the three things the silhouette has to carry, and neither had ever been on film.
    ///
    /// It follows SignalFxDemo's two rules. It installs itself from a
    /// [RuntimeInitializeOnLoadMethod] so reviewing an emplacement never means editing the
    /// composition root, and it stages into the REAL level under the real overcast lighting rather
    /// than building a studio -- a prop reviewed on a black background is a prop reviewed against a
    /// background the game does not have, and this whole art direction is the lighting doing half
    /// the work.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TurretPropsDemo : MonoBehaviour
    {
        private const string Flag = "-exodus-turret-demo";
        private const string DamageArg = "-exodus-turret-demo-damage";

        /// <summary>Tiers shown across the row, when showing tiers.</summary>
        private static readonly int[] Tiers = { 0, 1, 2, 3 };

        /// <summary>States of repair shown across the row, when showing damage.</summary>
        private static readonly float[] Health = { 1.0f, 0.65f, 0.40f, 0.10f };

        private bool _damage;
        private Camera? _camera;
        private bool _pinned;
        private float _elapsed;
        private readonly List<TurretProps> _built = new List<TurretProps>(8);
        private readonly Dictionary<Color, Material> _palette = new Dictionary<Color, Material>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallIfRequested()
        {
            var args = Environment.GetCommandLineArgs();
            if (args == null) return;
            foreach (var a in args)
            {
                if (!string.Equals(a, Flag, StringComparison.OrdinalIgnoreCase)) continue;
                var host = new GameObject("TurretPropsDemo");
                DontDestroyOnLoad(host);
                var demo = host.AddComponent<TurretPropsDemo>();
                demo._damage = HasFlag(args, DamageArg);
                Debug.Log($"[TurretPropsDemo] staging emplacements by {(demo._damage ? "damage" : "tier")}");
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

        private void Update()
        {
            // The contact shadows are a PER-FRAME queue, not a registration -- an emplacement can
            // be sold or abandoned, so nothing under a turret is permanent. Restate them every
            // frame or the demo's emplacements float exactly the way a live one would not.
            if (_pinned)
            {
                for (int i = 0; i < _built.Count; i++)
                    GroundMarkRenderer.Active?.Queue(_built[i].Root.transform.position, _built[i].GroundRadius);
                return;
            }

            _elapsed += Time.deltaTime;

            if (_camera == null || !_camera.isActiveAndEnabled) _camera = FindLiveCamera();
            if (_camera == null) return;

            // Give the bootstrap a moment to put its camera behind the hero. A stage pinned on
            // frame zero lands wherever the scene template happened to start, which is usually
            // inside the treeline.
            if (_elapsed < 0.6f) return;

            var forward = _camera.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 1e-4f) forward = Vector3.forward;
            forward.Normalize();
            var right = Vector3.Cross(Vector3.up, forward);
            var anchor = _camera.transform.position + forward * 15f;
            anchor.y = 0f;

            // Two ranks, and the back one is OFFSET BY HALF A SPACING. Squared up behind each
            // other the sentries were hidden by the Brush Hogs -- and the sentry is the family
            // whose whole read is a tall silhouette, so the one shot taken of it showed none of
            // it. Staggering is the difference between a line-up and a photograph.
            for (int i = 0; i < Tiers.Length; i++)
            {
                Place(area: false, index: i, offset: 0.5f, anchor: anchor + forward * 5.2f,
                      right: right, forward: forward);
                Place(area: true, index: i, offset: 0f, anchor: anchor, right: right, forward: forward);
            }

            _pinned = true;
        }

        private void Place(bool area, int index, float offset, Vector3 anchor, Vector3 right, Vector3 forward)
        {
            var props = TurretProps.Build(area, PropMaterial);
            int tier = _damage ? 1 : Tiers[index];
            float hp = _damage ? Health[index] : 1f;

            props.SetTier(tier);
            props.SetIntegrity(hp);

            // Faced slightly apart rather than all square to the camera. A rank of identical
            // objects at identical angles photographs as a product line-up; a couple of degrees of
            // difference is what makes them read as things somebody dragged into position.
            float spread = 3.4f;
            var at = anchor + right * ((index + offset - (Tiers.Length - 1) * 0.5f) * spread);
            props.Root.transform.position = at;
            props.Root.transform.rotation = Quaternion.Euler(0f, 180f + (index - 1) * 9f, 0f);

            // The bootstrap scales a live emplacement by tier, and the demo has to do the same or
            // it is photographing an object the game never draws.
            props.Root.transform.localScale = Vector3.one * (1f + 0.10f * tier);

            // The head aims at the camera's side of the field, because a dish pointing away is the
            // one angle at which a dish does not read as a dish.
            if (props.Head != null)
                props.Head.rotation = Quaternion.LookRotation(-forward + right * (index - 1.5f) * 0.22f, Vector3.up);

            _built.Add(props);
        }

        /// <summary>
        /// The same cached-by-colour prop material the bootstrap hands TurretProps, rebuilt here
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

        private void OnDestroy()
        {
            foreach (var pair in _palette)
                if (pair.Value != null) Destroy(pair.Value);
            _palette.Clear();
        }
    }
}
