#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cipher.Game
{
    /// <summary>
    /// Owns the blob shadows and the decals and draws them once a frame.
    ///
    /// It installs ITSELF, after the scene loads, rather than waiting to be constructed by the
    /// bootstrap. That is deliberate and it is about failure modes: props register their contact
    /// shadows as they are built, from three different files, and if the one call that draws them
    /// were missing the result would be no error, no warning and no shadows -- the exact silent
    /// class of bug this project has been bitten by repeatedly (the stripped instanced draws, the
    /// unbound animation, the blinded turrets). Self-installing means the registration and the draw
    /// cannot get out of step.
    ///
    /// What it CANNOT do by itself is the crowd, because the agents live in the sim and this has no
    /// business reaching into it. The integrator supplies them with one line, from DrawWorld:
    ///
    ///     GroundMarkRenderer.Active?.DrawAgents(positions, 0.42f);
    ///
    /// where <c>positions</c> is the list DrawAgents already builds to place the capsules. Anything
    /// that wants to mark the ground calls <c>Decals.Current?.Add(...)</c> from wherever it is.
    ///
    /// Cost at ~900 props and a crowd: two instanced draw calls for the shadows and up to three for
    /// the decals. No per-object components, no allocation per frame.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GroundMarkRenderer : MonoBehaviour
    {
        /// <summary>Stages a spread of decals so a capture can show them before anything wires them up.</summary>
        public const string DemoArg = "-exodus-groundmarks-demo";

        /// <summary>Turns the whole feature off, for a before/after capture.</summary>
        public const string OffArg = "-exodus-no-groundmarks";

        public static GroundMarkRenderer? Active { get; private set; }

        private BlobShadows? _blobs;
        private Decals? _decals;
        private bool _demo;
        private bool _demoDone;
        private float _elapsed;

        public BlobShadows? Blobs => _blobs;
        public Decals? Marks => _decals;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Active != null) return;
            var args = Environment.GetCommandLineArgs();
            if (HasFlag(args, OffArg))
            {
                Debug.Log("[GroundMarks] disabled by command line");
                return;
            }

            var host = new GameObject("GroundMarks");
            DontDestroyOnLoad(host);
            var renderer = host.AddComponent<GroundMarkRenderer>();
            renderer._demo = HasFlag(args, DemoArg);
        }

        private void Awake()
        {
            Active = this;
            _blobs = new BlobShadows();
            _decals = new Decals();
        }

        /// <summary>
        /// Draws a contact shadow under each of the given world positions. Called by the integrator
        /// with whatever moved this frame; y is ignored, the mark goes on the ground.
        /// </summary>
        public void DrawAgents(IReadOnlyList<Vector3> positions, float radius)
        {
            _blobs?.Draw(positions, radius);
        }

        /// <summary>Queues one odd-sized shadow -- an emplacement, the hero -- for this frame.</summary>
        public void Queue(Vector3 position, float radius)
        {
            _blobs?.Queue(position, radius);
        }

        /// <summary>
        /// Wipes everything that belongs to the position being left. Call alongside whatever
        /// destroys the props, or the next position is printed on top of the last one's shadows.
        /// </summary>
        public void ResetForNewPosition()
        {
            BlobShadows.ClearProps();
            VertexAo.ClearCache();
            _decals?.Clear();
        }

        // LateUpdate, not Update: the integrator's DrawWorld runs in Update, so queued shadows for
        // things that moved this frame are already in when this flushes them.
        private void LateUpdate()
        {
            _elapsed += Time.unscaledDeltaTime;
            if (_demo && !_demoDone && _elapsed > 1.5f) { StageDemoMarks(); _demoDone = true; }

            _blobs?.DrawProps();
            _blobs?.FlushQueued();
            _decals?.Draw();
        }

        /// <summary>
        /// Stamps a spread of marks around the middle of the map so a harness capture shows what
        /// decals look like before any gameplay code has been wired to leave them. Staging only --
        /// exactly what -exodus-screenshot-emplacements does for the turrets.
        /// </summary>
        private void StageDemoMarks()
        {
            if (_decals == null) return;
            var centre = _blobsCentreGuess();
            var rng = new System.Random(20260911);

            for (int i = 0; i < 9; i++)
            {
                float a = (float)(rng.NextDouble() * Math.PI * 2.0);
                float r = 3f + (float)rng.NextDouble() * 13f;
                var p = centre + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                _decals.Add(DecalKind.Scorch, p, 1.8f + (float)rng.NextDouble() * 1.6f,
                            (float)rng.NextDouble() * 360f);
            }
            for (int i = 0; i < 22; i++)
            {
                float a = (float)(rng.NextDouble() * Math.PI * 2.0);
                float r = 2f + (float)rng.NextDouble() * 16f;
                var p = centre + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                _decals.Add(DecalKind.Stain, p, 0.5f + (float)rng.NextDouble() * 0.5f,
                            (float)rng.NextDouble() * 360f);
            }
            for (int i = 0; i < 7; i++)
            {
                float a = (float)(rng.NextDouble() * Math.PI * 2.0);
                float r = 2f + (float)rng.NextDouble() * 14f;
                var p = centre + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                _decals.Add(DecalKind.Drag, p, 1.5f + (float)rng.NextDouble(),
                            (float)rng.NextDouble() * 360f);
            }
            Debug.Log($"[GroundMarks] staged {_decals.Count} demo decals around {centre}");
        }

        /// <summary>
        /// Where to put the demo marks. The camera is the only thing here that knows where the
        /// action is without reaching into the match, so aim at the ground in front of it.
        /// </summary>
        private Vector3 _blobsCentreGuess()
        {
            // NOT Camera.main. Unity's scene template ships its own MainCamera and the bootstrap
            // disables foreign cameras rather than deleting them, so the tagged one is not reliably
            // the one being rendered through (CLAUDE.md). Take whichever camera is actually on.
            Camera? cam = null;
            foreach (var c in Camera.allCameras)
                if (c != null && c.isActiveAndEnabled && (cam == null || c.depth > cam.depth)) cam = c;
            if (cam == null) return Vector3.zero;
            var p = cam.transform.position + cam.transform.forward * 18f;
            return new Vector3(p.x, 0f, p.z);
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(Active, this)) Active = null;
            _blobs?.Dispose();
            _decals?.Dispose();
        }

        private static bool HasFlag(string[] args, string flag)
        {
            if (args == null) return false;
            foreach (var a in args)
                if (string.Equals(a, flag, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}
