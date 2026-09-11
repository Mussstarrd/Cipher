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

        private string? _path;
        private float _delay = 8f;
        private float _elapsed;
        private bool _captured;
        private float _shotAt;

        /// <summary>Adds the harness to the scene when the command line asks for it.</summary>
        public static void InstallIfRequested(GameObject host)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));
            var (path, delay) = Parse(Environment.GetCommandLineArgs());
            if (path == null) return;

            var harness = host.AddComponent<ScreenshotHarness>();
            harness._path = path;
            harness._delay = delay;
            Debug.Log($"[Screenshot] armed: {path} after {delay:F1}s");
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
