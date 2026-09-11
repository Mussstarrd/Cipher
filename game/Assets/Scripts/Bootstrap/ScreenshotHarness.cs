#nullable enable
using System;
using System.IO;
using UnityEngine;

namespace Cipher.Game
{
    /// <summary>
    /// Visual smoke test. Run the player with
    ///     ProjectExodus.exe -exodus-screenshot &lt;path.png&gt; [-exodus-screenshot-delay 8]
    /// and it plays for a few seconds, writes a screenshot and quits.
    ///
    /// This exists because the first CI build shipped an arena that rendered nothing but ground
    /// and a cursor, and nobody found out until the owner ran it. Compiling is not rendering.
    /// An external screen grab cannot see the player's GPU surface, so the game has to do it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScreenshotHarness : MonoBehaviour
    {
        private const string PathArg = "-exodus-screenshot";
        private const string DelayArg = "-exodus-screenshot-delay";
        private const string OverheadArg = "-exodus-screenshot-overhead";
        private const string WheelArg = "-exodus-screenshot-wheel";
        private const string ProgressArg = "-exodus-screenshot-progression";
        private const string SkillsArg = "-exodus-screenshot-skills";
        private const string StartArg = "-exodus-screenshot-start";
        private const string LineupArg = "-exodus-screenshot-lineup";
        private const string FallBackArg = "-exodus-screenshot-fallback";
        private const string EmplacementsArg = "-exodus-screenshot-emplacements";
        private const string StrikeArg = "-exodus-screenshot-strike";
        private const string CollectorArg = "-exodus-screenshot-collector";
        private const string ComicArg = "-exodus-comic";
        private const string ComicKeyboardArg = "-exodus-comic-keyboard";

        /// <summary>
        /// Seconds left on the truck page's pack-up clock when the preview opens.
        ///
        /// The page changes at thirty seconds and again at ten, and those are the states the
        /// owner actually complained about ("after a certain amount of time it just kicked me
        /// out"). Without this the only way to photograph the warning is to hold the shutter for
        /// a minute, so the state that needed reviewing was the one that never got reviewed.
        /// </summary>
        private const string ComicClockArg = "-exodus-comic-clock";
        private const string YawArg = "-exodus-screenshot-yaw";
        private const string PitchArg = "-exodus-screenshot-pitch";

        private string? _path;
        private float _delay = 8f;
        private bool _overhead;
        private bool _wheel;
        private bool _progression;
        private bool _skills;
        private bool _start;
        private bool _lineup;
        private bool _fallBack;
        private bool _emplacements;
        private bool _strike;
        private bool _strikeCalled;
        private bool _collector;
        private bool _collectorSpawned;
        private float _yaw = float.NaN;
        private float _pitch = 22f;
        private bool _aimed;
        private bool _emplacementsSet;
        private bool _fellBack;
        private bool _lineupSet;
        private bool _started;
        private bool _viewSet;
        private bool _wheelSet;
        private bool _progressionSet;
        private bool _skillsSet;
        private float _elapsed;
        private bool _captured;
        private float _shotAt;

        /// <summary>Adds the harness to the scene when the command line asks for it.</summary>
        public static void InstallIfRequested(GameObject host)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));
            var args = Environment.GetCommandLineArgs();
            var (path, delay) = Parse(args);
            if (path == null) return;

            var harness = host.AddComponent<ScreenshotHarness>();
            harness._path = path;
            harness._delay = delay;
            harness._overhead = WantsOverhead(args);
            harness._wheel = WantsWheel(args);
            harness._progression = HasFlag(args, ProgressArg);
            harness._skills = HasFlag(args, SkillsArg);
            harness._start = HasFlag(args, StartArg);
            harness._lineup = HasFlag(args, LineupArg);
            harness._fallBack = HasFlag(args, FallBackArg);
            harness._emplacements = HasFlag(args, EmplacementsArg);
            harness._strike = HasFlag(args, StrikeArg);
            harness._collector = HasFlag(args, CollectorArg);
            harness._yaw = ReadFloat(args, YawArg, float.NaN);
            harness._pitch = ReadFloat(args, PitchArg, 22f);
            InstallComicPreview(host, args);
            Debug.Log($"[Screenshot] armed: {path} after {delay:F1}s");
        }

        /// <summary>True when the capture should use the tactical camera, to show the whole field.</summary>
        public static bool WantsOverhead(string[] args)
        {
            if (args == null) return false;
            foreach (var a in args)
                if (string.Equals(a, OverheadArg, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>
        /// Stages one of the rebuilt comic screens over the top of the game, when asked.
        ///
        /// It is a separate component rather than another bootstrap hook because these screens hold
        /// their own models: a capture of the kit page has to show a kit, and mid-mission the real
        /// one is empty. Added in Awake order before the first OnGUI, so the first frame is already
        /// the page.
        /// </summary>
        private static void InstallComicPreview(GameObject host, string[] args)
        {
            string? which = ReadString(args, ComicArg);
            if (which == null) return;

            var page = which.ToLowerInvariant() switch
            {
                "kit" => UI.Comic.ComicScreenPreview.Screen.Kit,
                "skills" => UI.Comic.ComicScreenPreview.Screen.Skills,
                "truck" => UI.Comic.ComicScreenPreview.Screen.Truck,
                "demo" => UI.Comic.ComicScreenPreview.Screen.Demo,
                _ => UI.Comic.ComicScreenPreview.Screen.Kit,
            };

            var preview = host.AddComponent<UI.Comic.ComicScreenPreview>();
            preview.Page = page;
            preview.PadPrompts = !HasFlag(args, ComicKeyboardArg);
            preview.TruckSecondsLeft = ReadFloat(args, ComicClockArg, preview.TruckSecondsLeft);
            Debug.Log($"[Screenshot] comic preview: {page}, pad prompts {preview.PadPrompts}, truck clock {preview.TruckSecondsLeft:F0}s");
        }

        /// <summary>The string after a flag, or null when the flag is absent.</summary>
        private static string? ReadString(string[] args, string flag)
        {
            if (args == null) return null;
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            return null;
        }

        /// <summary>True when the capture should show the radial build menu open.</summary>
        public static bool WantsWheel(string[] args) => HasFlag(args, WheelArg);

        /// <summary>
        /// A numeric argument, or <paramref name="fallback"/> when it is absent or unreadable.
        /// InvariantCulture on purpose, like the scenario reader: a comma-decimal locale must not
        /// read 22.5 as 225.
        /// </summary>
        private static float ReadFloat(string[] args, string flag, float fallback)
        {
            if (args == null) return fallback;
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (!string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase)) continue;
                if (float.TryParse(args[i + 1], System.Globalization.NumberStyles.Float,
                                   System.Globalization.CultureInfo.InvariantCulture, out float v))
                    return v;
            }
            return fallback;
        }

        private static bool HasFlag(string[] args, string flag)
        {
            if (args == null) return false;
            foreach (var a in args)
                if (string.Equals(a, flag, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>Pulled out so it can be unit tested without a player.</summary>
        public static (string? path, float delay) Parse(string[] args)
        {
            string? path = null;
            float delay = 8f;
            if (args == null) return (null, delay);

            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], PathArg, StringComparison.OrdinalIgnoreCase)
                    && i + 1 < args.Length)
                {
                    path = args[i + 1];
                }
                else if (string.Equals(args[i], DelayArg, StringComparison.OrdinalIgnoreCase)
                         && i + 1 < args.Length
                         && float.TryParse(args[i + 1], System.Globalization.NumberStyles.Float,
                                           System.Globalization.CultureInfo.InvariantCulture, out float parsed)
                         && parsed >= 0f)
                {
                    delay = parsed;
                }
            }
            return (string.IsNullOrWhiteSpace(path) ? null : path, delay);
        }

        private void Update()
        {
            if (_path == null) return;
            _elapsed += Time.unscaledDeltaTime;

            // Switch the view a moment before the shot so the camera has settled.
            if (_overhead && !_viewSet && _elapsed > Mathf.Max(0f, _delay - 1.5f))
            {
                var bootstrap = GetComponent<FloodBootstrap>();
                if (bootstrap != null) bootstrap.EnterTacticalViewForCapture();
                _viewSet = true;
            }

            if (_lineup && !_lineupSet && _elapsed > 0.5f)
            {
                var bl = GetComponent<FloodBootstrap>();
                if (bl != null) bl.ShowCharacterLineupForCapture();
                _lineupSet = true;
            }

            // Start the wave early so there is a fight to photograph by the time we capture.
            if (_start && !_started && _elapsed > 0.5f)
            {
                var bstart = GetComponent<FloodBootstrap>();
                if (bstart != null) bstart.StartWaveForCapture();
                _started = true;
            }

            // Fall back to the next position, so the capture shows the mission AFTER this one.
            // This is the only way to exercise the map-resize path without playing two missions.
            if (_fallBack && !_fellBack && _elapsed > Mathf.Max(0f, _delay - 2f))
            {
                var bf = GetComponent<FloodBootstrap>();
                if (bf != null) bf.FallBackForCapture();
                _fellBack = true;
            }

            // Late enough that the wave is on the field, early enough that the strike is still in
            // the air when the shutter opens.
            // Four seconds ahead of the shutter: the strike has an inbound delay and then walks its
            // bombs down the line, so calling it one second early photographed an empty sky.
            if (_emplacements && !_emplacementsSet && _elapsed > Mathf.Max(0f, _delay - 4f))
            {
                var be = GetComponent<FloodBootstrap>();
                if (be != null) be.ShowEmplacementsForCapture();
                _emplacementsSet = true;
            }

            // The bomb itself, not its aftermath. The strike has an inbound flight and then walks
            // its bombs down the line, so the shutter wants to open about a second and a half after
            // the call -- long enough for the first bomb to land, short enough that the smoke from
            // it has not yet thinned out. Photographing an effect is the only way to know it is
            // there; this one lived 0.45s and was never once caught on film.
            // Aimed late, so the bootstrap's own startup cannot overwrite it, and once, so the
            // player's look input is not fought over every frame.
            if (!float.IsNaN(_yaw) && !_aimed && _elapsed > Mathf.Max(0f, _delay - 0.4f))
            {
                var ba = GetComponent<FloodBootstrap>();
                if (ba != null) ba.AimCameraForCapture(_yaw, _pitch);
                _aimed = true;
            }

            // Early enough that it has walked into frame by the time the shutter opens: a
            // Collector is deliberately slow, which is exactly the property that makes it awkward
            // to photograph.
            if (_collector && !_collectorSpawned && _elapsed > Mathf.Max(0f, _delay - 6f))
            {
                var bc = GetComponent<FloodBootstrap>();
                if (bc != null) bc.SpawnCollectorForCapture();
                _collectorSpawned = true;
            }

            if (_strike && !_strikeCalled && _elapsed > Mathf.Max(0f, _delay - 1.5f))
            {
                var bx = GetComponent<FloodBootstrap>();
                if (bx != null) bx.CallStrikeForCapture();
                _strikeCalled = true;
            }

            if (_skills && !_skillsSet && _elapsed > Mathf.Max(0f, _delay - 1f))
            {
                var bs = GetComponent<FloodBootstrap>();
                if (bs != null) bs.ShowSkillsForCapture();
                _skillsSet = true;
            }

            if (_progression && !_progressionSet && _elapsed > Mathf.Max(0f, _delay - 1f))
            {
                var bp = GetComponent<FloodBootstrap>();
                if (bp != null) bp.ShowProgressionForCapture();
                _progressionSet = true;
            }

            if (_wheel && !_wheelSet && _elapsed > Mathf.Max(0f, _delay - 1f))
            {
                var bootstrap = GetComponent<FloodBootstrap>();
                if (bootstrap != null) bootstrap.OpenBuildWheelForCapture(1);
                _wheelSet = true;
            }

            if (!_captured)
            {
                if (_elapsed < _delay) return;
                try
                {
                    string full = Path.GetFullPath(_path);
                    string? dir = Path.GetDirectoryName(full);
                    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                    ScreenCapture.CaptureScreenshot(full);
                    Debug.Log($"[Screenshot] captured to {full}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Screenshot] failed: {e.Message}");
                }
                _captured = true;
                _shotAt = _elapsed;
                return;
            }

            // CaptureScreenshot is asynchronous: it lands at the end of a later frame, so give it
            // a moment before quitting or the file is empty or missing.
            if (_elapsed - _shotAt > 2f) Application.Quit(0);
        }
    }
}
