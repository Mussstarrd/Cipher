#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cipher.Game
{
    /// <summary>
    /// Measures frame cost during a real wave and prints percentiles, then quits.
    ///
    /// This exists because the project has an explicit performance floor -- ADR-001 puts Android
    /// first at 300-500 agents -- and no way at all to observe it. Frame rate was HUD text, which
    /// means the only record of it was whatever happened to be legible in a screenshot somebody took
    /// for another reason. Two evenings of adding real character meshes, real buildings and an
    /// Animator per body went by without a single number attached to any of it.
    ///
    /// PERCENTILES, NOT AN AVERAGE. A mean frame time hides exactly the thing that ruins a game:
    /// forty good frames and one 60ms frame average out fine and read as a stutter. The 95th and
    /// 99th are where a hitch lives, so those are what get printed.
    ///
    /// It deliberately does NOT set Time.captureDeltaTime. ClipHarness does, to make smooth footage
    /// out of a slow render -- which is the exact opposite of what a measurement wants.
    ///
    ///   ProjectExodus.exe -exodus-perf 12 [-exodus-scenario &lt;id&gt;]
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PerfHarness : MonoBehaviour
    {
        private const string SecondsArg = "-exodus-perf";

        /// <summary>Seconds to discard before sampling: shader warm-up and the first-wave spawn burst.</summary>
        private const float WarmupSeconds = 4f;

        /// <summary>
        /// FRAMES to discard as well, and the reason is a bug this harness had on its first run.
        ///
        /// A time-only warm-up is satisfied by one frame that is longer than the warm-up. Startup
        /// took 4.9 seconds in a single frame, `_elapsed` went from 0.02 to 4.9 inside it, the gate
        /// opened, and that load frame was recorded as the worst gameplay frame in the run. It
        /// reported a 4.8-second stall in a game whose real p99 is 7ms, and the field was EMPTY at
        /// the time -- `alive 0` in the log was the tell that no crowd could possibly be to blame.
        ///
        /// Counting frames as well makes a long frame unable to satisfy its own warm-up.
        /// </summary>
        private const int WarmupFrames = 120;

        private int _warmFrames;

        private float _seconds = 12f;
        private float _elapsed;
        private bool _waveStarted;
        private readonly List<float> _frames = new List<float>(4096);

        public static void InstallIfRequested(GameObject host)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));

            var args = Environment.GetCommandLineArgs();
            float seconds = -1f;
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], SecondsArg, StringComparison.OrdinalIgnoreCase)
                    && float.TryParse(args[i + 1], System.Globalization.NumberStyles.Float,
                                      System.Globalization.CultureInfo.InvariantCulture, out float v))
                    seconds = v;

            if (seconds <= 0f) return;

            var h = host.AddComponent<PerfHarness>();
            h._seconds = Mathf.Clamp(seconds, 2f, 120f);
            Debug.Log($"[Perf] armed: {h._seconds:F0}s of sampling after {WarmupSeconds:F0}s warm-up");
        }

        private void Update()
        {
            var b = GetComponent<FloodBootstrap>();
            if (b == null) return;

            _elapsed += Time.unscaledDeltaTime;

            // Measure a FIGHT. An empty setup phase renders almost nothing and would report a number
            // that no player will ever see.
            if (!_waveStarted && _elapsed > 0.5f)
            {
                b.StartWaveForCapture();
                b.KeepHeroUpForCapture();
                _waveStarted = true;
            }
            if (_waveStarted) b.KeepHeroUpForCapture();

            _warmFrames++;
            if (_elapsed < WarmupSeconds || _warmFrames < WarmupFrames) return;

            float ms = Time.unscaledDeltaTime * 1000f;
            _frames.Add(ms);

            // A hitch is worth a line of its own with a TIMESTAMP on it. A percentile tells you a
            // stall exists; only the clock tells you what the game was doing when it happened.
            if (ms > 100f)
                Debug.Log($"[Perf] HITCH {ms:F0}ms at t={_elapsed:F1}s " +
                          $"(alive {GetComponent<FloodBootstrap>()?.AliveForPerf ?? -1})");

            if (_elapsed < WarmupSeconds + _seconds) return;

            Report();
            Application.Quit(0);
            enabled = false;
        }

        private void Report()
        {
            if (_frames.Count == 0) { Debug.Log("[Perf] no frames sampled"); return; }

            _frames.Sort();
            float Pick(float q) => _frames[Mathf.Clamp((int)(q * (_frames.Count - 1)), 0, _frames.Count - 1)];

            float total = 0f;
            foreach (float f in _frames) total += f;
            float mean = total / _frames.Count;

            var census = GetComponent<FloodBootstrap>();
            census?.LogCrowdCensusForCapture();

            Debug.Log($"[Perf] frames {_frames.Count} | mean {mean:F1}ms ({1000f / mean:F0} fps) | " +
                      $"p50 {Pick(0.50f):F1} | p95 {Pick(0.95f):F1} | p99 {Pick(0.99f):F1} | " +
                      $"worst {_frames[_frames.Count - 1]:F1}ms");
        }
    }
}
