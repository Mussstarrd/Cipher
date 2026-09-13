#nullable enable
using Cipher.Game.Match;
using UnityEngine;

namespace Cipher.Game
{
    /// <summary>
    /// Plays a position start to finish with nobody at the controller.
    ///
    ///     ProjectExodus.exe -exodus-scenario act1-06-crowbar -exodus-autoplay 600
    ///
    /// WHY THIS EXISTS. Every headless run before it watched wave one and nothing else. With no
    /// player, nothing dies; wave one never clears; wave two never starts. So the second half of
    /// every mission -- and two whole archetypes -- had never executed outside a human session:
    /// a Spitter needs a turret to hunt, and a Sapper needs a player-built wall to be worth
    /// spawning. The perf numbers in CLAUDE.md were measured against an idle first wave and the
    /// balance numbers were arithmetic.
    ///
    /// It builds through <see cref="FloodBootstrap.AutoBuildForCapture"/>, which drives the real
    /// BuildModel, so cost, legality and the seal rules all apply. This is a MEASURING instrument,
    /// not an opponent: it does not shoot, dodge, repair or retreat, so its results are a FLOOR --
    /// what the position does to a defence that was built once and then abandoned. A mission that
    /// holds here is not proven easy; a mission that collapses here is worth a second look.
    /// </summary>
    public sealed class AutoPlayHarness : MonoBehaviour
    {
        private const string Arg = "-exodus-autoplay";
        private const string CashArg = "-exodus-autoplay-cash";
        private const string TurretArg = "-exodus-autoplay-turrets";
        private const float ReportEvery = 30f;

        /// <summary>Turrets and barricades bought at each setup phase, cash permitting.</summary>
        private const int DefaultTurretsPerSetup = 3;
        // Enough to actually SPAN four streets (13 cells a side, four headings), not a token
        // sprinkle. The bank is the real limiter -- on the opening $400 this still buys about five
        // -- but when there IS money the run must be allowed to close a route, because a barricade
        // that leaves a gap is one the Sapper's cost model correctly ignores. Eight was the number
        // that produced twenty-four barricades across three waves and not one sapper interested in
        // any of them.
        private const int BarricadesPerSetup = 52;

        private FloodBootstrap _game = null!;
        private float _budgetSeconds;
        private float _elapsed;
        private float _sinceReport;
        private int _lastWaveBuiltFor = -1;
        private int _grantPerSetup;
        private int _turretsPerSetup = DefaultTurretsPerSetup;
        private bool _done;

        public static void InstallIfRequested(GameObject host)
        {
            string[] argv = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < argv.Length - 1; i++)
            {
                if (argv[i] != Arg) continue;
                if (!float.TryParse(argv[i + 1], out float seconds) || seconds <= 0f) continue;

                var h = host.AddComponent<AutoPlayHarness>();
                h._game = host.GetComponent<FloodBootstrap>();
                h._budgetSeconds = seconds;
                h._grantPerSetup = ReadCashGrant(argv);
                h._turretsPerSetup = ReadIntArg(argv, TurretArg, DefaultTurretsPerSetup);
                Debug.Log($"[Auto] armed: up to {seconds:F0}s, building {h._turretsPerSetup} turrets " +
                          $"and {BarricadesPerSetup} barricades at each setup" +
                          (h._grantPerSetup > 0 ? $" | PROBE: ${h._grantPerSetup} granted per setup, "
                                                + "these results measure the mechanic, not the balance" : ""));
                return;
            }
        }

        /// <summary>
        /// Optional cash per setup. A PROBE: the opening $400 cannot buy a barricade run that
        /// actually closes a street, so without this no bot ever builds a wall a Sapper would want,
        /// and mission 6's premise stays untested. Results with it on say the mechanic works; they
        /// say nothing about whether the position is fair.
        /// </summary>
        private static int ReadCashGrant(string[] argv) => ReadIntArg(argv, CashArg, 0);

        private static int ReadIntArg(string[] argv, string name, int fallback)
        {
            for (int i = 0; i < argv.Length - 1; i++)
                if (argv[i] == name && int.TryParse(argv[i + 1], out int v) && v > 0)
                    return v;
            return fallback;
        }

        private void Update()
        {
            if (_done || _game == null) return;

            _elapsed += Time.deltaTime;
            _sinceReport += Time.deltaTime;

            // Build once per setup phase, then send the wave. Building EVERY frame would spend the
            // wave-clear bonus the instant it landed and quietly turn this into a different test.
            if (_game.PhaseForCapture == MatchPhase.Setup)
            {
                if (_lastWaveBuiltFor != _game.WaveNumberForCapture)
                {
                    _lastWaveBuiltFor = _game.WaveNumberForCapture;
                    if (_grantPerSetup > 0) _game.GrantCashForCapture(_grantPerSetup);
                    int built = _game.AutoBuildForCapture(_turretsPerSetup, BarricadesPerSetup);
                    Debug.Log($"[Auto] wave {_game.WaveNumberForCapture} setup: built {built}, " +
                              $"${_game.CashForCapture} left, {_game.TurretCountForCapture} turrets, " +
                              $"{_game.BreachableWallsForCapture} breachable walls");
                }
                _game.StartWaveForCapture();
            }

            if (_sinceReport >= ReportEvery) { _sinceReport = 0f; Report("tick"); }

            bool over = _game.PhaseForCapture is MatchPhase.Won or MatchPhase.Extracted
                     || _game.VaultHpForCapture <= 0;
            if (over || _elapsed >= _budgetSeconds)
            {
                Report(over ? "over" : "budget spent");
                _done = true;
                _game.LogCrowdCensusForCapture();
                Application.Quit(0);
                enabled = false;
            }
        }

        private void Report(string why)
        {
            var (spotted, planted, opened, collapsed) = _game.BreachTallyForCapture;
            Debug.Log($"[Auto] {why} t={_elapsed:F0}s | wave {_game.WaveNumberForCapture}/" +
                      $"{_game.WaveCountForCapture} {_game.PhaseForCapture} | truck " +
                      $"{_game.VaultHpForCapture}/{_game.VaultMaxHpForCapture} | kills {_game.KillsForCapture} " +
                      $"| turrets {_game.TurretCountForCapture} | walls {_game.BreachableWallsForCapture} " +
                      $"| sappers seen {spotted} planted {planted} opened {opened} collapsed {collapsed}");
        }
    }
}
