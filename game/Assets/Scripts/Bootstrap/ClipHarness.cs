#nullable enable
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Cipher.Game
{
    /// <summary>
    /// Records a run of frames to disk so a clip can be cut out of real play.
    ///
    /// This is <see cref="ScreenshotHarness"/> with the shutter held open, and it exists for the
    /// same reason: an external screen grab cannot see the player's GPU surface, so the game has to
    /// film itself. It is also BETTER than a screen recording, and the whole reason why is one line
    /// -- <c>Time.captureDeltaTime</c>. With that set, Unity stops advancing time by how long the
    /// last frame took and advances it by exactly 1/fps instead. Spending 80ms encoding a frame
    /// therefore does NOT become an 80ms hitch in the footage: the game runs in slow motion on the
    /// wall clock and in perfect real time in the file.
    ///
    /// The consequence worth knowing before you wait on it: frames are not dropped, they are waited
    /// for. A 10-second shot at 60fps is 600 frames and takes as long as it takes.
    ///
    /// Each run films ONE shot. Cutting four short shots and concatenating them beats one long take
    /// because every beat wants its own camera and its own staging, and because a failed shot then
    /// costs one shot rather than the whole reel.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ClipHarness : MonoBehaviour
    {
        private const string DirArg = "-exodus-clip";
        private const string ShotArg = "-exodus-clip-shot";
        private const string FpsArg = "-exodus-clip-fps";
        private const string SecondsArg = "-exodus-clip-seconds";
        private const string QualityArg = "-exodus-clip-quality";
        private const string RealtimeArg = "-exodus-clip-realtime";

        /// <summary>
        /// The beat being filmed. Each one stages the field differently, because the four things
        /// worth showing have incompatible needs: the area tower wants the crowd ON it, the strike
        /// wants the crowd spread out under it, and the Collector wants room to walk into frame.
        /// </summary>
        private enum Shot
        {
            /// <summary>The wave closing down the lane. The establishing shot.</summary>
            Mob = 0,

            /// <summary>The Brush Hog -- the FireMode.Area tower -- cutting into a crowd that reached it.</summary>
            Tower = 1,

            /// <summary>The EMP: called, inbound, then walking its bombs down the line.</summary>
            Strike = 2,

            /// <summary>A Collector arriving and soaking fire that would retire anything else.</summary>
            Boss = 3,
        }

        private string _dir = null!;
        private Shot _shot;
        private int _fps = 60;
        private float _seconds = 8f;
        private int _quality = 95;
        private bool _realtime;

        private int _frame;
        private int _totalFrames;
        private float _t;
        private bool _waveStarted;
        private bool _turretsPlaced;
        private bool _strikeCalled;
        private bool _bossSpawned;
        private bool _aimed;
        private bool _done;

        /// <summary>Adds the recorder to the scene when the command line asks for it.</summary>
        public static void InstallIfRequested(GameObject host)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));
            var args = Environment.GetCommandLineArgs();
            string? dir = ReadString(args, DirArg);
            if (string.IsNullOrWhiteSpace(dir)) return;

            var h = host.AddComponent<ClipHarness>();
            h._dir = Path.GetFullPath(dir);
            h._shot = ParseShot(ReadString(args, ShotArg));
            h._fps = Mathf.Clamp((int)ReadFloat(args, FpsArg, 60f), 12, 120);
            h._seconds = Mathf.Clamp(ReadFloat(args, SecondsArg, 8f), 0.5f, 60f);
            h._quality = Mathf.Clamp((int)ReadFloat(args, QualityArg, 95f), 50, 100);
            h._realtime = HasFlag(args, RealtimeArg);
            Debug.Log($"[Clip] armed: shot {h._shot}, {h._seconds:F1}s at {h._fps}fps -> {h._dir}");
        }

        /// <summary>Unknown names fall back to the establishing shot rather than filming nothing.</summary>
        private static Shot ParseShot(string? name) => (name ?? string.Empty).ToLowerInvariant() switch
        {
            "tower" => Shot.Tower,
            "strike" => Shot.Strike,
            "boss" => Shot.Boss,
            _ => Shot.Mob,
        };

        private void Start()
        {
            Directory.CreateDirectory(_dir);
            _totalFrames = Mathf.CeilToInt(_seconds * _fps);

            // The line the whole harness is for. See the class comment.
            //
            // `-exodus-clip-realtime` opts out of it, which costs smooth footage and is here purely
            // as a diagnostic: anything that renders in a screenshot but not in a clip is either
            // this line or nothing, and guessing about it cost more than the switch does.
            if (!_realtime) Time.captureDeltaTime = 1f / _fps;

            StartCoroutine(Record());
        }

        private void OnDestroy()
        {
            // Leaving this set would freeze the timestep for anything else in the process.
            Time.captureDeltaTime = 0f;
        }

        /// <summary>
        /// Stages the beat. Ordering against the recorder matters and is not accidental: Update runs
        /// early in the frame, the camera and the sim then advance, the frame renders, and only then
        /// does the coroutine's WaitForEndOfFrame fire and grab it. So a beat called here is always
        /// visible in the frame that follows it, never half-applied inside the one being filmed.
        /// </summary>
        private void Update()
        {
            if (_done) return;
            _t += Time.unscaledDeltaTime;

            var b = GetComponent<FloodBootstrap>();
            if (b == null) return;

            // A trailer does not show the hero dying in the first four seconds. A CAPTURE AID: like
            // the screenshot harness's immortal flag, nothing outside this class reads it.
            b.KeepHeroUpForCapture();

            // Aimed once, and on the first frame, so the rig is already pointing the right way when
            // frame zero is grabbed -- and once only, so the player's own look input is not fought
            // over every frame.
            //
            // YAW IS -90 AND NOT 180. The rig's own default is -90 -- "forward = (sin, 0, cos), -90
            // looks down -X, toward the flood" -- and every capture hook in the bootstrap aims down
            // that same axis (the strike is called at Position + (-14, 0)). The first take of this
            // was shot at 180 and the result was two seconds of very nice houses with the entire
            // wave behind the camera.
            if (!_aimed)
            {
                const float DownTheLane = -90f;

                // Pitch is the only thing worth varying. The strike wants sky in frame because the
                // bombs come out of it; the Collector wants a low angle because the whole point of
                // it is that it is three times the size of a person.
                float pitch = _shot switch
                {
                    Shot.Boss => 14f,
                    Shot.Strike => 30f,
                    _ => 20f,
                };
                b.AimCameraForCapture(DownTheLane, pitch);
                b.EnterCinematicForCapture();
                _aimed = true;
            }

            // Turrets BEFORE the wave, always: they have to be standing when the crowd arrives, and
            // the Brush Hog's reach is 2.4m, so the shot is only a shot if the mob gets all the way
            // onto it.
            if (_shot == Shot.Tower && !_turretsPlaced && _t > 0.15f)
            {
                b.PlaceTurretsForCapture();
                _turretsPlaced = true;
            }

            if (!_waveStarted && _t > 0.3f)
            {
                b.StartWaveForCapture();

                // The wave alone is not enough to film. Its bodies enter at the map edge and need
                // twenty-odd seconds to walk into shot, so an eight-second take of the real wave is
                // eight seconds of empty road -- which is exactly what the first take produced.
                // The mob is seeded up the lane instead and the wave arrives behind it.
                b.SpawnMobForCapture();

                // Real skins rather than instanced capsules out to the far edge of frame: a trailer
                // is the one capture where the LOD ladder is a liability rather than a budget.
                b.SetCrowdRangeForCapture(110f);
                _waveStarted = true;
            }

            // Late enough that the wave is on the field and spread out under it, early enough that
            // the bombs land and their smoke is still thick while the camera is still rolling.
            if (_shot == Shot.Strike && !_strikeCalled && _t > _seconds * 0.45f)
            {
                b.CallStrikeForCapture();
                _strikeCalled = true;
            }

            // Early: a Collector is deliberately slow, which is the exact property that makes it
            // awkward to film. It needs most of the shot to cross the frame.
            if (_shot == Shot.Boss && !_bossSpawned && _t > 0.8f)
            {
                b.SpawnCollectorForCapture();
                _bossSpawned = true;
            }
        }

        /// <summary>
        /// Grabs the composited frame -- game, health bars, alerts and all -- at the end of every
        /// frame until the shot is in the can.
        ///
        /// CaptureScreenshotAsTexture rather than CaptureScreenshot because the latter is
        /// asynchronous and lands "at the end of a later frame", which is fine for one photograph
        /// and useless for a sequence that has to stay in order.
        /// </summary>
        private IEnumerator Record()
        {
            var endOfFrame = new WaitForEndOfFrame();

            while (_frame < _totalFrames)
            {
                yield return endOfFrame;

                Texture2D? tex = null;
                try
                {
                    tex = ScreenCapture.CaptureScreenshotAsTexture();
                    byte[] jpg = tex.EncodeToJPG(_quality);
                    File.WriteAllBytes(Path.Combine(_dir, $"f{_frame:D5}.jpg"), jpg);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Clip] frame {_frame} failed: {e.Message}");
                }
                finally
                {
                    if (tex != null) Destroy(tex);
                }

                _frame++;
                if (_frame % 60 == 0) Debug.Log($"[Clip] {_frame}/{_totalFrames}");
            }

            _done = true;
            Debug.Log($"[Clip] wrote {_frame} frames to {_dir}");
            Time.captureDeltaTime = 0f;
            Application.Quit(0);
        }

        private static bool HasFlag(string[] args, string flag)
        {
            if (args == null) return false;
            foreach (var a in args)
                if (string.Equals(a, flag, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static string? ReadString(string[] args, string flag)
        {
            if (args == null) return null;
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            return null;
        }

        /// <summary>
        /// InvariantCulture on purpose, like the scenario reader: a comma-decimal locale must not
        /// read 22.5 as 225.
        /// </summary>
        private static float ReadFloat(string[] args, string flag, float fallback)
        {
            if (args == null) return fallback;
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (!string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase)) continue;
                if (float.TryParse(args[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
                    return v;
            }
            return fallback;
        }
    }
}
