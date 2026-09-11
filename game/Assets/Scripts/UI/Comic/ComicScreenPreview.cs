#nullable enable
using System.Collections.Generic;
using Cipher.Game.Hero;
using Cipher.Game.Progression;
using UnityEngine;

namespace Cipher.Game.UI.Comic
{
    /// <summary>
    /// Stages one of the three real screen controllers, on real models, so it can be photographed
    /// from a player build.
    ///
    /// CLAUDE.md: "Verify rendering, never infer it." <see cref="ComicPageDemo"/> photographs the
    /// TOOLKIT with hand-written sample data; this photographs the SCREENS — the same adapters,
    /// input and copy the match uses, fed by an Inventory, a SkillState and a TruckLoad built the
    /// way the game builds them. A picture of the demo proves the ink works. Only a picture of this
    /// proves the wiring does.
    ///
    /// It is also hand-drivable: the screens read the devices exactly as they will in a match, so a
    /// build with -exodus-comic kit is a controller test the owner can do in ten seconds.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ComicScreenPreview : MonoBehaviour
    {
        public enum Screen { Kit, Skills, Truck, Demo }

        /// <summary>Which page to stage.</summary>
        public Screen Page = Screen.Kit;

        /// <summary>
        /// Print controller prompts rather than keyboard ones. Defaults to true because the pad is
        /// the design centre on both platforms (ADR-002) and a capture machine never has one plugged
        /// in — which is exactly how a keyboard-only prompt ships unnoticed.
        /// </summary>
        public bool PadPrompts = true;

        /// <summary>
        /// Seconds on the pack-up clock when the truck page opens. Set by
        /// <c>-exodus-comic-clock</c>, because the page's Hurry and Final states are the ones
        /// worth reviewing and waiting a real minute for them is not a review loop.
        /// </summary>
        public float TruckSecondsLeft = 74f;

        private KitScreen? _kit;
        private SkillsScreen? _skills;
        private TruckScreen? _truck;

        private bool _built;

        /// <summary>
        /// Builds the models on FIRST USE, not in Awake.
        ///
        /// `AddComponent` runs Awake synchronously, so the harness sets `Page`, `PadPrompts` and
        /// `TruckSecondsLeft` on the line AFTER the component has already woken up. Page and
        /// PadPrompts survived that because they are read every frame; the clock did not, and
        /// -exodus-comic-clock silently photographed the default. Anything this component reads once
        /// has to be read here rather than in Awake.
        /// </summary>
        private void EnsureBuilt()
        {
            if (_built) return;
            _built = true;

            var loadout = BuildLoadout();
            _kit = new KitScreen(loadout);
            _skills = new SkillsScreen(loadout);
            _truck = new TruckScreen(new PreviewTruckHost(TruckSecondsLeft));

            _kit.Show();
            _skills.Show();

            var truckLoad = new TruckLoad();
            var recoverable = BuildRecoverable(loadout);
            // Two aboard and five on the gravel: the argument of the screen is what is NOT in the bed.
            truckLoad.TryLoad(recoverable[0]);
            truckLoad.TryLoad(recoverable[2]);
            _truck.Open(truckLoad, recoverable);
        }

        /// <summary>
        /// A mid-campaign loadout: some gear worn, some in the pack, a few levels banked and two
        /// nodes bought. Deliberately awkward — an empty charm slot, a downgrade in the pack and a
        /// column of the tree the player cannot reach yet. A demo where everything is tidy proves
        /// nothing about a page that has to hold a mess.
        /// </summary>
        private static Loadout BuildLoadout()
        {
            var loadout = new Loadout(new HeroConfig());

            var roller = new ItemRoller(42);
            for (int i = 0; i < 10; i++)
            {
                var item = roller.TryDrop(i % 3 == 0 ? DropSource.SapperKill : DropSource.SpitterKill, 2, 3);
                if (item != null) loadout.Pickup(item);
            }

            loadout.Skills.AddXp(LevelCurve.TotalXpFor(6));
            loadout.SpendSkillPoint("t-marks");
            loadout.SpendSkillPoint("t-marks");
            loadout.SpendSkillPoint("d-lanes");
            loadout.Recompute();
            return loadout;
        }

        private static List<IHaulable> BuildRecoverable(Loadout loadout)
        {
            var list = new List<IHaulable>
            {
                new SalvagedEmplacement("Sentry T2", new Haulage(420f, 2.4f), 260),
                new SalvagedEmplacement("Grinder T1", new Haulage(510f, 3.1f), 300),
                new SalvagedEmplacement("Sentry T1", new Haulage(180f, 1.2f), 120),
                new SalvagedEmplacement("Repair Drone", new Haulage(60f, 0.5f), 80),
                new SalvagedEmplacement("Fence Panels", new Haulage(240f, 2.2f), 60),
                new SalvagedEmplacement("Generator", new Haulage(700f, 1.6f), 340),
            };
            // EVERY pack item, not three of them. This is the list the real extraction builds,
            // and the whole point of the gear decision is that the page must not drown in it:
            // a preview that quietly trimmed the pack to three could never have shown the fault.
            for (int i = 0; i < loadout.Inventory.Pack.Count; i++)
                list.Add(new HauledItem(loadout.Inventory.Pack[i]));
            return list;
        }

        private void Update()
        {
            EnsureBuilt();
            switch (Page)
            {
                case Screen.Kit: _kit?.HandleInput(); break;
                case Screen.Skills: _skills?.HandleInput(); break;
                case Screen.Truck: _truck?.HandleInput(); break;
            }
        }

        private void OnGUI()
        {
            EnsureBuilt();

            // Negative depth puts this in front of the bootstrap's own HUD. IMGUI draws high depth
            // first, and the order between two components is otherwise arbitrary.
            GUI.depth = -100;

            // The same virtual space FloodBootstrap.BeginScaledUi builds. Duplicated on purpose:
            // this component has to stand alone in a build where the bootstrap may not be drawing.
            float scale = Mathf.Clamp(UnityEngine.Screen.height / 800f, 1f, 3f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            float uiW = UnityEngine.Screen.width / scale;
            float uiH = UnityEngine.Screen.height / scale;

            switch (Page)
            {
                case Screen.Kit: _kit?.Draw(uiW, uiH, PadPrompts); break;
                case Screen.Skills: _skills?.Draw(uiW, uiH, PadPrompts); break;
                case Screen.Truck: _truck?.Draw(uiW, uiH, PadPrompts); break;
                default: ComicPageDemo.DrawAll(uiW, uiH); break;
            }
        }

        /// <summary>A host with a clock that runs and a window that never refuses. Preview only.</summary>
        private sealed class PreviewTruckHost : ITruckHost
        {
            private readonly float _opened = Time.unscaledTime;
            private readonly float _window;

            public PreviewTruckHost(float window) => _window = Mathf.Max(0f, window);

            public float SecondsLeft => Mathf.Max(0f, _window - (Time.unscaledTime - _opened));
            public bool TryUnbolt(int recoveredValue) => true;
            public bool PullOut() => false;
            public void Notice(string text) => Debug.Log($"[ComicPreview] {text}");
            public void Tick() { }
            public void Accept() { }
            public void Refuse() { }
        }
    }
}
