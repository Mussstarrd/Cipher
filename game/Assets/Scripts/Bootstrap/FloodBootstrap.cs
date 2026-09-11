#nullable enable
using System.Collections.Generic;
using Cipher.Game.Audio;
using Cipher.Game.Build;
using Cipher.Game.Hero;
using Cipher.Game.Match;
using Cipher.Game.Progression;
using Cipher.Game.UI;
using Cipher.Sim.Agents;
using Cipher.Sim.Core;
using Cipher.Sim.Emplacements;
using Cipher.Sim.Grid;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;

namespace Cipher.Game
{
    /// <summary>
    /// Graybox composition root for Milestone 2 "The Maze". The entire scene is built
    /// procedurally at startup so ANY empty scene runs it. Sim runs at a fixed 30 Hz tick
    /// (deterministic core); hero, turrets, waves and cash all reach the swarm only through
    /// AgentWorld / GridMap, and the build preview runs the live FlowField on a scratch map.
    ///
    /// Xbox: LS move, RS look, RT fire, Y airstrike where you look, LB build mode (toggle),
    /// View start the wave early, Menu pause, A restart when down / after the match.
    /// Build mode: LS/d-pad move the cursor, A place (hold to paint), X sell, Y upgrade a turret,
    /// RB / d-pad left-right next item, B or LB done. Sappers breach walls, Spitters hunt turrets,
    /// repair drones close holes while you stand near, gun crates upgrade your LMG. Keyboard: WASD, mouse, LMB, RMB/Q, Tab build, Enter start wave,
    /// arrows cursor, Space place, X sell, Q next item, Esc pause.
    /// </summary>
    public static class FloodEntryPoint
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Object.FindFirstObjectByType<FloodBootstrap>() != null) return;
            var go = new GameObject("FloodBootstrap");
            go.AddComponent<FloodBootstrap>();
            Object.DontDestroyOnLoad(go);
        }
    }

    public sealed class FloodBootstrap : MonoBehaviour
    {
        private const float TickRate = 30f;
        private const float TickDt = 1f / TickRate;
        private const int GridW = 64;
        private const int GridH = 48;
        private const int GoalX = GridW - 2, GoalY = GridH / 2;
        private const int MaxInstancesPerDraw = 1023; // Graphics.DrawMeshInstanced hard limit

        // ---- sim / match ----
        private GridMap _map = null!;
        private FlowField _field = null!;
        private AgentWorld _world = null!;
        private TurretSystem _turrets = null!;
        private MatchState _match = null!;
        private readonly EconomyConfig _eco = new EconomyConfig();
        private BuildModel _build = null!;
        private SpawnDirector _director = null!;
        private PickupSystem _pickups = null!;
        private float _matchSeconds;
        private readonly List<SimEvent> _eventScratch = new List<SimEvent>(32);
        private readonly List<(string Text, float Ttl)> _alerts = new List<(string, float)>(8);
        private static readonly (int X, int Y)[] SpawnCells = { (1, 4), (1, 14), (1, 24), (1, 34), (1, 44) };
        private int _spawnCursor;
        private float _tickAccumulator;
        private int _lastReached, _lastKills;
        private readonly List<TurretShot> _shotScratch = new List<TurretShot>(64);

        // ---- hero ----
        private readonly HeroConfig _heroCfg = new HeroConfig();

        // Progression. The loadout is the ONLY thing that turns gear, cards and the tree into
        // numbers the hero feels; everything else reads Effective and stays ignorant.
        private Loadout _loadout = null!;
        private ItemRoller _loot = null!;
        private int _lastLevel = 1;
        private int _lastSapperAlive;
        private int _lastSpitterAlive;
        private string _lootNotice = "";
        private float _lootNoticeTimer;
        private bool _showInventory;
        private bool _showSkills;
        private TruckLoad? _truck;
        private List<IHaulable> _recoverable = new List<IHaulable>();
        private int _truckCursor;
        private int _skillCursor;
        private IReadOnlyList<Improvisation> _cardOffer = System.Array.Empty<Improvisation>();
        private int _cardCursor;
        private HeroModel _hero = null!;
        private static readonly Vec2 HeroSpawn = new Vec2(GridW - 6f, GridH / 2f);
        private Transform _heroT = null!;
        private Transform _barrelT = null!;
        private Transform _markerT = null!;
        private readonly List<StrikeImpact> _impactScratch = new List<StrikeImpact>(8);

        private struct Tracer { public Vector3 A, B; public float Ttl; public bool Turret; }
        private struct Blast { public Vector3 Center; public float Radius; public float Ttl; }
        private readonly List<Tracer> _tracers = new List<Tracer>(128);
        private readonly List<Blast> _blasts = new List<Blast>(8);
        private const float TracerLife = 0.06f;
        private const float BlastLife = 0.45f;

        // ---- build mode ----
        private bool _buildMode;
        private Transform _cursorT = null!;
        private Material _cursorMaterial = null!;
        private float _cursorRepeatTimer, _cursorHeldTime;
        // Held-to-scroll ramp: a nudge steps one cell, holding accelerates so crossing the arena is quick.
        private static readonly (float AfterSeconds, float CellsPerSecond)[] CursorRamp =
        {
            (0f, 7f), (0.30f, 15f), (0.85f, 28f), (1.8f, 46f),
        };
        private (int X, int Y) _lastPaintCell = (-1, -1);
        private readonly List<Vec2> _routeScratch = new List<Vec2>(600);
        private readonly List<Matrix4x4> _routeOk = new List<Matrix4x4>(2048);
        private readonly List<Matrix4x4> _routeBad = new List<Matrix4x4>(2048);

        // ---- rendering ----
        private Mesh _agentMesh = null!;
        private Material _agentMaterial = null!;
        private Mesh _cubeMesh = null!;
        private Mesh _discMesh = null!;
        private Material _wallMaterial = null!;
        private Material _barricadeMaterial = null!;
        private Material _breachMaterial = null!;
        private Material _tracerMaterial = null!;
        private Material _turretTracerMaterial = null!;
        private Material _blastMaterial = null!;
        private Material _routeOkMaterial = null!;
        private Material _routeBadMaterial = null!;
        private Material _turretMaterial = null!;
        private Material _grinderMaterial = null!;
        private Matrix4x4[] _instanceBuffer = null!;
        private Matrix4x4[] _wallMatrices = System.Array.Empty<Matrix4x4>();
        private Matrix4x4[] _barricadeMatrices = System.Array.Empty<Matrix4x4>();
        private Matrix4x4[] _breachMatrices = System.Array.Empty<Matrix4x4>();
        private int _bakedMapVersion = -1;
        private readonly List<GameObject> _turretGos = new List<GameObject>(32);
        private Transform _vaultT = null!;
        private Transform _crateT = null!;
        private Texture2D _minimap = null!;
        private Color32[] _minimapPixels = null!;
        private int[] _minimapAgents = null!;
        private float _minimapTimer;
        private bool _showMinimap = true;
        private Material _sapperMaterial = null!;
        private Material _spitterMaterial = null!;
        private Material _droneMaterial = null!;
        private Material _sapperTargetMaterial = null!;
        private readonly List<Matrix4x4> _sapperMatrices = new List<Matrix4x4>(16);
        private readonly List<Matrix4x4> _spitterMatrices = new List<Matrix4x4>(16);
        private readonly List<Matrix4x4> _fxMatrices = new List<Matrix4x4>(32);

        // ---- camera ----
        private enum CameraMode { Chase, Tactical }
        private Camera _camera = null!;
        private CameraMode _camMode = CameraMode.Chase;
        private float _camYaw = -90f;  // degrees; forward = (sin, 0, cos): -90 looks down -X, toward the flood
        private float _camPitch = 22f; // degrees above horizontal; RS-Y tilts it
        private const float ChaseDistance = 9f, ChaseLookHeight = 1.2f;
        private const float PitchMin = -10f, PitchMax = 55f;
        private const float StickYawSpeed = 170f, StickPitchSpeed = 90f, MouseYawPerPixel = 0.15f, MousePitchPerPixel = 0.1f;

        // ---- audio ----
        private SoundBank _sfx = null!;
        private MatchPhase _lastPhase = MatchPhase.Setup;
        private bool _wasDown;
        private bool _strikeWasInbound;
        private float _hurtCooldown;
        private readonly AdrenalineFocus _focus = new AdrenalineFocus();
        private readonly BuildWheel _wheel = new BuildWheel();
        private bool _wheelHeld;
        private string _declineNotice = "";
        private float _declineNoticeTimer;
        private float _hordePollTimer;
        private float _hordeIntensity;

        // ---- ui ----
        private float _smoothedFps = 60f;
        private readonly PauseMenuModel _pauseMenu = new PauseMenuModel();
        private GUIStyle? _menuTitleStyle;
        private GUIStyle? _menuItemStyle;
        private GUIStyle? _centerStyle;
        private GUIStyle? _subStyle;

        private void Awake()
        {
            BuildSceneObjects();
            NewMatch();
            ScreenshotHarness.InstallIfRequested(gameObject);
        }

        // ------------------------------------------------------------------ setup

        private void NewMatch()
        {
            _map = new GridMap(GridW, GridH);
            // Serpentine graybox arena: three map walls forcing an S-route left to right.
            for (int y = 0; y < GridH - 10; y++) _map.SetWall(16, y, WallKind.Wall, GridMap.DefaultWallHp);
            for (int y = 10; y < GridH; y++) _map.SetWall(32, y, WallKind.Wall, GridMap.DefaultWallHp);
            for (int y = 0; y < GridH - 10; y++) _map.SetWall(48, y, WallKind.Wall, GridMap.DefaultWallHp);

            _field = new FlowField(_map);
            _field.Compute(GoalX, GoalY);
            _world = new AgentWorld(_map, _field, new SimConfig(), initialCapacity: 4096);
            _turrets = new TurretSystem();
            // The game opts in to the untimed opening: dig in for as long as you like, and the
            // first wave comes when you press start.
            _match = new MatchState(WaveTable.Default, _eco, openingIsUntimed: true);
            _focus.Reset();
            _build = new BuildModel(_map, _world, _turrets, _match, _eco, SpawnCells, GoalX, GoalY, GridW - 12, GridH / 2);
            _world.Structures = _turrets.AsStructureQuery();
            ulong seed = (ulong)System.DateTime.UtcNow.Ticks;
            _director = new SpawnDirector(new DirectorConfig(), seed);
            _pickups = new PickupSystem(_map, seed ^ 0xC1FE, minX: 34, maxX: GridW - 4);

            // Carry the permanent tree across restarts; gear and cards are per-position.
            var skills = _loadout?.Skills ?? new SkillState();
            _loadout = new Loadout(_heroCfg, new Inventory(), new ImprovisationDeck(seed ^ 0xA11CE), skills);
            _loot = new ItemRoller(seed ^ 0x9E3779B9UL);
            _lastLevel = _loadout.Skills.Level;
            _cardOffer = System.Array.Empty<Improvisation>();
            _matchSeconds = 0f;
            _alerts.Clear();
            _lastPhase = MatchPhase.Setup;
            _wasDown = false;
            _strikeWasInbound = false;
            GunTiers.Apply(_heroCfg, 0);


            _hero = new HeroModel(_loadout.Effective, HeroSpawn);
            ApplyLoadoutToWorld();
            if (_crowd == null) BuildCivilianPool();
            if (_heroBody == null) BuildHeroBody();
            if (!_environmentDressed) { DressEnvironment(); _environmentDressed = true; }
            _hero.Aim(new Vec2(-1f, 0f));
            _camYaw = -90f;
            _camPitch = 22f;
            _buildMode = false;
            _camMode = CameraMode.Chase;
            _tickAccumulator = 0f;
            _lastReached = 0;
            _lastKills = 0;
            _spawnCursor = 0;
            _tracers.Clear();
            _blasts.Clear();
            _bakedMapVersion = -1;
            SyncTurretObjects();
        }

        private void BuildSceneObjects()
        {
            foreach (var existing in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                existing.gameObject.SetActive(false);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            _camera = camGo.AddComponent<Camera>();
            _camera.backgroundColor = new Color(0.05f, 0.05f, 0.07f);
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.nearClipPlane = 0.2f;
            _camera.farClipPlane = 300f;
            camGo.AddComponent<AudioListener>();
            _sfx = new SoundBank(transform) { MasterVolume = 0.8f };

            ApplyOvercastWinter();
            ApplyColourGrade();

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = new Vector3(GridW / 2f, 0f, GridH / 2f);
            ground.transform.localScale = new Vector3(GridW / 10f, 1f, GridH / 10f);
            ground.GetComponent<Renderer>().material = MakeMaterial(GroundColour, instanced: false, ink: InkNone);

            _agentMesh = HarvestMesh(PrimitiveType.Capsule);
            _cubeMesh = HarvestMesh(PrimitiveType.Cube);
            _discMesh = HarvestMesh(PrimitiveType.Cylinder);
            _agentMaterial = MakeMaterial(new Color(0.75f, 0.15f, 0.12f), instanced: true);
            _wallMaterial = MakeMaterial(new Color(0.35f, 0.33f, 0.30f), instanced: true);
            _barricadeMaterial = MakeMaterial(new Color(0.55f, 0.45f, 0.25f), instanced: true);
            _breachMaterial = MakeMaterial(new Color(0.9f, 0.35f, 0.1f), instanced: true);
            _tracerMaterial = MakeMaterial(new Color(1f, 0.95f, 0.5f), instanced: false);
            _turretTracerMaterial = MakeMaterial(new Color(0.6f, 0.9f, 1f), instanced: false);
            _blastMaterial = MakeMaterial(new Color(1f, 0.5f, 0.1f), instanced: false);
            _routeOkMaterial = MakeMaterial(new Color(0.3f, 0.9f, 0.45f), instanced: true);
            _routeBadMaterial = MakeMaterial(new Color(1f, 0.2f, 0.15f), instanced: true);
            _turretMaterial = MakeMaterial(new Color(0.25f, 0.55f, 0.85f), instanced: false);
            _grinderMaterial = MakeMaterial(new Color(0.85f, 0.35f, 0.75f), instanced: false);
            _sapperMaterial = MakeMaterial(new Color(1f, 0.45f, 0.05f), instanced: true);
            _spitterMaterial = MakeMaterial(new Color(0.35f, 0.9f, 0.25f), instanced: true);
            _droneMaterial = MakeMaterial(new Color(0.4f, 0.95f, 1f), instanced: true);
            _sapperTargetMaterial = MakeMaterial(new Color(1f, 0.3f, 0.05f), instanced: true);
            _instanceBuffer = new Matrix4x4[MaxInstancesPerDraw];

            // Minimap: one pixel per grid cell, repainted a few times a second (cheap at any density).
            _minimap = new Texture2D(GridW, GridH, TextureFormat.RGBA32, mipChain: false) { filterMode = FilterMode.Point };
            _minimapPixels = new Color32[GridW * GridH];
            _minimapAgents = new int[GridW * GridH];

            var heroGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            heroGo.name = "Hero";
            Destroy(heroGo.GetComponent<Collider>());
            heroGo.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
            heroGo.GetComponent<Renderer>().material = MakeMaterial(new Color(1f, 0.84f, 0.2f), instanced: false);
            _heroT = heroGo.transform;

            var barrel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrel.name = "Barrel";
            Destroy(barrel.GetComponent<Collider>());
            barrel.transform.SetParent(_heroT, worldPositionStays: false);
            barrel.transform.localPosition = new Vector3(0f, 0.25f, 0.9f);
            barrel.transform.localScale = new Vector3(0.18f, 0.18f, 1.1f);
            barrel.GetComponent<Renderer>().material = MakeMaterial(new Color(0.12f, 0.12f, 0.12f), instanced: false);
            _barrelT = barrel.transform;

            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "StrikeMarker";
            Destroy(marker.GetComponent<Collider>());
            // Dark wet asphalt with a warm cast rather than traffic-cone orange. It still reads as
            // "they come from here" without being the brightest thing on the map.
            marker.GetComponent<Renderer>().material =
                MakeMaterial(new Color(0.24f, 0.19f, 0.16f), instanced: false, ink: InkNone);
            _markerT = marker.transform;

            var cursor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cursor.name = "BuildCursor";
            Destroy(cursor.GetComponent<Collider>());
            _cursorMaterial = MakeMaterial(Color.green, instanced: false);
            cursor.GetComponent<Renderer>().material = _cursorMaterial;
            cursor.transform.localScale = new Vector3(1.05f, 0.08f, 1.05f);
            _cursorT = cursor.transform;
            cursor.SetActive(false);

            var vault = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vault.name = "Vault";
            Destroy(vault.GetComponent<Collider>());
            vault.transform.position = new Vector3(GoalX + 0.5f, 0.6f, GoalY + 0.5f);
            vault.transform.localScale = new Vector3(1.6f, 1.2f, 1.6f);
            // Weathered olive, like something worth defending rather than a debug cube.
            vault.GetComponent<Renderer>().material =
                MakeMaterial(new Color(0.28f, 0.34f, 0.24f), instanced: false, ink: InkProp);
            _vaultT = vault.transform;

            var crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = "GunCrate";
            Destroy(crate.GetComponent<Collider>());
            crate.transform.localScale = new Vector3(0.9f, 0.6f, 0.9f);
            crate.GetComponent<Renderer>().material = MakeMaterial(new Color(1f, 0.9f, 0.2f), instanced: false);
            _crateT = crate.transform;
            crate.SetActive(false);
        }

        private static Mesh HarvestMesh(PrimitiveType type)
        {
            var temp = GameObject.CreatePrimitive(type);
            var mesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Destroy(temp);
            return mesh;
        }

        // Overcast Virginia winter (ADR-004): flat grey sky, low sun, cold haze, brown ground.
        // Half the game happens in daylight and this is what that daylight looks like.
        private static readonly Color SkyGrey = new Color(0.62f, 0.65f, 0.69f);
        /// <summary>Wet brown leaf litter over dead grass. Winter here is not grey, it is brown.</summary>
        private static readonly Color GroundColour = new Color(0.30f, 0.26f, 0.20f);
        private static readonly Color HazeGrey = new Color(0.58f, 0.61f, 0.65f);

        private void ApplyOvercastWinter()
        {
            var lightGo = new GameObject("Sun");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            // Low winter sun, raking from the south-west, so everything casts a long shadow.
            light.transform.rotation = Quaternion.Euler(26f, -42f, 0f);
            light.color = new Color(1f, 0.96f, 0.88f);
            light.intensity = 1.45f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.72f;
            _sun = light;

            // Trilight ambient reads as an overcast dome: bright sky, dull brown bounce off leaf litter.
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = SkyGrey;
            RenderSettings.ambientEquatorColor = new Color(0.44f, 0.45f, 0.46f);
            RenderSettings.ambientGroundColor = new Color(0.26f, 0.22f, 0.18f);

            // Haze is what sells distance and hides the edge of the play area.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = HazeGrey;
            RenderSettings.fogDensity = 0.011f;

            if (_camera != null)
            {
                _camera.clearFlags = CameraClearFlags.SolidColor;
                _camera.backgroundColor = HazeGrey;
            }
        }

        private Light? _sun;

        private static Shader? _litShader;

        /// <summary>
        /// Our own instanced URP shader, with fallbacks so an un-migrated project still renders.
        /// Standard is last because under URP it renders magenta; it only helps a Built-in fallback.
        /// </summary>
        private static Shader LitShader =>
            _litShader ??= Shader.Find("Exodus/InstancedLit")
                        ?? Shader.Find("Universal Render Pipeline/Lit")
                        ?? Shader.Find("Standard");

        /// <summary>
        /// Comic-book ink width per surface. Characters and props get a drawn line; the ground
        /// gets none, because an inverted-hull outline on a giant flat plane just makes a border
        /// round the whole world.
        /// </summary>
        public const float InkCharacter = 0.05f;
        public const float InkProp = 0.035f;
        public const float InkNone = 0f;
        /// <summary>Fine line for detailed character meshes, which need far less than a cube.</summary>
        public const float InkFigure = 0.012f;

        private static Material MakeMaterial(Color color, bool instanced, float ink = InkProp)
        {
            var mat = new Material(LitShader) { enableInstancing = instanced };
            mat.color = color;
            if (mat.HasProperty(BaseColorId)) mat.SetColor(BaseColorId, color);
            if (mat.HasProperty(OutlineWidthId)) mat.SetFloat(OutlineWidthId, ink);
            return mat;
        }

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");
        private static readonly int SmoothOutlineId = Shader.PropertyToID("_SmoothOutline");

        private void BakeWallsIfChanged()
        {
            if (_bakedMapVersion == _map.Version) return;
            _bakedMapVersion = _map.Version;
            var walls = new List<Matrix4x4>(256);
            var barricades = new List<Matrix4x4>(256);
            var breaches = new List<Matrix4x4>(16);
            for (int y = 0; y < GridH; y++)
            {
                for (int x = 0; x < GridW; x++)
                {
                    WallKind kind = _map.KindAt(x, y);
                    if (kind == WallKind.None || kind == WallKind.Structure) continue;
                    BreachStage stage = _map.StageAt(x, y);
                    if (stage == BreachStage.Collapsed) continue;
                    float h = stage == BreachStage.Intact ? 2f : stage == BreachStage.Cracked ? 1.2f : 0.5f;
                    var m = Matrix4x4.TRS(new Vector3(x + 0.5f, h * 0.5f, y + 0.5f), Quaternion.identity, new Vector3(1f, h, 1f));
                    if (stage != BreachStage.Intact) breaches.Add(m);
                    else if (kind == WallKind.Barricade) barricades.Add(m);
                    else walls.Add(m);
                }
            }
            _wallMatrices = walls.ToArray();
            _barricadeMatrices = barricades.ToArray();
            _breachMatrices = breaches.ToArray();
        }

        private void SyncTurretObjects()
        {
            var list = _turrets.Turrets;
            while (_turretGos.Count > list.Count)
            {
                Destroy(_turretGos[_turretGos.Count - 1]);
                _turretGos.RemoveAt(_turretGos.Count - 1);
            }
            while (_turretGos.Count < list.Count)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = "Turret";
                go.name = "Turret";
                Destroy(go.GetComponent<Collider>());
                go.GetComponent<Renderer>().material = _turretMaterial;
                var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(head.GetComponent<Collider>());
                head.transform.SetParent(go.transform, false);
                head.transform.localPosition = new Vector3(0f, 1.1f, 0f);
                head.transform.localScale = new Vector3(0.6f, 0.45f, 1.2f);
                head.GetComponent<Renderer>().material = _turretMaterial;
                _turretGos.Add(go);
            }
            for (int i = 0; i < list.Count; i++)
            {
                var t = list[i];
                float hp = (float)t.Hp / t.MaxHp;
                bool area = _turrets.FamilyOfTurret(i).Mode == FireMode.Area;
                var go = _turretGos[i];
                go.transform.position = new Vector3(t.X + 0.5f, 0.7f, t.Y + 0.5f);
                go.transform.localScale = new Vector3(0.9f + 0.15f * t.Tier, 0.4f + 0.6f * hp, 0.9f + 0.15f * t.Tier);
                // Grinders spin and wear a different colour so the two families read apart at a glance.
                go.GetComponent<Renderer>().material = area ? _grinderMaterial : _turretMaterial;
                var head = go.transform.GetChild(0);
                head.GetComponent<Renderer>().material = area ? _grinderMaterial : _turretMaterial;
                head.localScale = area ? new Vector3(1.5f, 0.25f, 0.35f) : new Vector3(0.6f, 0.45f, 1.2f);
                head.localRotation = area ? Quaternion.Euler(0f, Time.time * 520f, 0f) : Quaternion.identity;
            }
        }

        // ------------------------------------------------------------------ frame

        private void Update()
        {
            _smoothedFps = Mathf.Lerp(_smoothedFps, 1f / Mathf.Max(Time.unscaledDeltaTime, 1e-5f), 0.05f);

            ReadPauseInput();
            if (_pauseMenu.IsOpen)
            {
                DrawWorld();
                return;
            }

            // Focus runs on unscaled time so four seconds of slow lasts four seconds, not eleven.
            _focus.Tick(Time.unscaledDeltaTime);
            Time.timeScale = _focus.TimeScale;

            float dt = Time.deltaTime;

            if (_match.IsOver)
            {
                if (RestartPressed()) NewMatch();
            }
            else if (_hero.IsDown)
            {
                if (RestartPressed()) { _hero.Respawn(HeroSpawn); _hero.Aim(new Vec2(-1f, 0f)); }
            }
            else
            {
                ReadModeToggles();
                if (_buildMode) ReadBuildInput(dt); else ReadHeroInput(dt);
            }

            // Fixed-tick sim, decoupled from render rate.
            _tickAccumulator += dt;
            int safety = 0;
            // At most 4 catch-up ticks: a slow frame must never buy itself more sim work than it can pay for.
            while (_tickAccumulator >= TickDt && safety++ < 4)
            {
                _tickAccumulator -= TickDt;
                FixedTick();
            }
            _tickAccumulator = Mathf.Min(_tickAccumulator, TickDt);

            UpdateEffects(dt);
            UpdateMinimap(dt);
            UpdateAudio(dt);
            UpdateHeroVisual();
            UpdateBuildVisual();
            UpdateCamera(dt);
            DrawWorld();
        }

        private void UpdateAudio(float dt)
        {
            _hurtCooldown -= dt;

            if (_match.Phase != _lastPhase)
            {
                switch (_match.Phase)
                {
                    case MatchPhase.Wave:
                        _sfx.Play(Sfx.WaveHorn, 0.9f, 0.02f);
                        _hero.RearmSecondWind();   // once per wave, not once per mission
                        break;
                    case MatchPhase.Setup:
                        _sfx.Play(Sfx.WaveClear, 0.9f, 0.01f);
                        OnWaveCleared();
                        break;
                    case MatchPhase.Extraction:
                        _sfx.Play(Sfx.WaveClear, 1f, 0f);
                        AwardXp(LevelCurve.XpForExtraction(_match.WavesCleared));
                        OpenTruck();
                        break;
                    case MatchPhase.Extracted: _sfx.Play(Sfx.Win, 0.85f, 0f); break;
                    case MatchPhase.Won: _sfx.Play(Sfx.Win, 1f, 0f); break;
                    case MatchPhase.Lost: _sfx.Play(Sfx.Lose, 1f, 0f); break;
                }
                _lastPhase = _match.Phase;
            }

            if (_hero.IsDown && !_wasDown) _sfx.Play(Sfx.Down, 1f, 0.02f);
            _wasDown = _hero.IsDown;

            if (_hero.StrikeInbound && !_strikeWasInbound) _sfx.Play(Sfx.StrikeWhistle, 0.8f, 0.03f);
            _strikeWasInbound = _hero.StrikeInbound;

            // Horde bed: how much of the flood is close to the hero. The wide query touches many
            // hash cells, so it runs 4x a second, not every frame.
            _hordePollTimer -= dt;
            if (_hordePollTimer <= 0f)
            {
                _hordePollTimer = 0.25f;
                int near = _world.CountWithin(_hero.Position, 14f);
                _hordeIntensity = Mathf.Clamp01(near / 60f) * 0.75f + Mathf.Clamp01(_world.AliveCount / 1000f) * 0.25f;
            }
            _sfx.SetHordeIntensity(_hordeIntensity);
            _sfx.Update(dt);
        }

        private void FixedTick()
        {
            if (!_match.IsOver)
            {
                _matchSeconds += TickDt;
                int breached = _world.ReachedCount - _lastReached;
                int toSpawn = _match.Tick(TickDt, _world.AliveCount, breached);
                _lastReached = _world.ReachedCount;
                if (toSpawn > 0)
                {
                    bool sealedIn = false;
                    foreach (var sc in SpawnCells) if (!_field.HasPath(sc.X, sc.Y)) { sealedIn = true; break; }
                    for (int i = 0; i < toSpawn; i++)
                    {
                        var (sx, sy) = SpawnCells[_spawnCursor % SpawnCells.Length];
                        _spawnCursor++;
                        var pos = new Vec2(sx + 0.5f + (_spawnCursor % 3) * 0.3f, sy + 0.5f + (_spawnCursor % 5) * 0.2f);
                        var view = new DirectorView(_matchSeconds, _world.ActiveSapperCount, _world.ActiveBreachCount,
                                                    _world.CountAlive(Archetype.Spitter), sealedIn, _turrets.Turrets.Count);
                        Archetype a = _director.Decide(view);
                        if (a == Archetype.Runner)
                        {
                            // Not every body runs the same errand (owner, 2026-09-11). The split is
                            // decided HERE, by the seeded director, because the sim carries no RNG.
                            _world.Spawn(pos, health: 10f, _director.DecideIntent(view));
                        }
                        else _world.SpawnArchetype(pos, a);
                    }
                }
            }

            _world.Step(TickDt);

            // Promote the nearest agents to real bodies. Everything else stays a capsule.
            if (_crowd != null && _camera != null)
                _crowd.Sync(_world, _camera.transform.position, TickDt);
            UpdateHeroBody();

            _shotScratch.Clear();
            _turrets.Step(_world, TickDt, _shotScratch);
            foreach (var s in _shotScratch)
            {
                if (s.Area)
                {
                    // A grinder sweeps rather than fires: show the bite radius, not a tracer.
                    var t = _turrets.Turrets.Count > s.TurretIndex ? _turrets.Turrets[s.TurretIndex] : null;
                    if (t != null)
                        _blasts.Add(new Blast { Center = new Vector3(t.X + 0.5f, 0.05f, t.Y + 0.5f), Radius = t.Range, Ttl = BlastLife * 0.28f });
                    _sfx.PlayAt(Sfx.TurretShot, ToWorld(s.From, 1f), 0.4f, 0.2f, minInterval: 0.11f);
                    continue;
                }
                _tracers.Add(new Tracer { A = ToWorld(s.From, 1.1f), B = ToWorld(s.To, 0.6f), Ttl = TracerLife * 0.7f, Turret = true });
                _sfx.PlayAt(Sfx.TurretShot, ToWorld(s.From, 1f), 0.6f, 0.1f, minInterval: 0.045f);
            }

            if (_hero.ApplyContact(_world, TickDt) > 0f && _hurtCooldown <= 0f)
            {
                _sfx.Play(Sfx.Hurt, 0.8f, 0.1f);
                _hurtCooldown = 0.35f;
            }
            if (_hero.ConsumedSecondWindThisTick)
            {
                Alert("SECOND WIND — that should have killed you", 4f);
                _sfx.Play(Sfx.Win, 0.8f, 0.05f);
            }
            HandleSimEvents();
            _build.TickDrones(_hero.Position, TickDt);
            if (_pickups.Tick(_matchSeconds, TickDt, _hero.Position, _heroCfg))
            {
                Alert($"GUN UPGRADE: {_pickups.GunName}", 3f);
                _sfx.Play(Sfx.Pickup, 1f, 0f);
            }

            // Kills from any source pay out (hero, turrets, airstrike).
            int kills = (int)(_world.TotalKills - _lastKills);
            _lastKills = (int)_world.TotalKills;
            // The two thinking archetypes always drop and are worth far more experience, which is
            // what makes hunting them the right play. The sim does not raise a "died" event per
            // archetype, so infer it from the living count falling. Approximate but stable.
            int sappersNow = _world.CountAlive(Archetype.Sapper);
            int spittersNow = _world.CountAlive(Archetype.Spitter);
            int sapperDeaths = Mathf.Max(0, _lastSapperAlive - sappersNow);
            int spitterDeaths = Mathf.Max(0, _lastSpitterAlive - spittersNow);
            _lastSapperAlive = sappersNow;
            _lastSpitterAlive = spittersNow;

            for (int i = 0; i < sapperDeaths; i++)
            {
                AwardXp(LevelCurve.XpForKill(true, false));
                var d = _loot.TryDrop(DropSource.SapperKill, 1, _match.WaveIndex);
                if (d != null) TakeLoot(d);
            }
            for (int i = 0; i < spitterDeaths; i++)
            {
                AwardXp(LevelCurve.XpForKill(false, true));
                var d = _loot.TryDrop(DropSource.SpitterKill, 1, _match.WaveIndex);
                if (d != null) TakeLoot(d);
            }

            if (kills > 0)
            {
                // Scrounger affixes and supply cards multiply the take.
                _match.Bank.Earn(_loadout.CashForKills(kills, _eco.CashPerKill));
                AwardXp(kills * LevelCurve.XpForKill(false, false));

                // Ordinary bodies almost never drop. The rate lives in the roller.
                for (int k = 0; k < kills; k++)
                {
                    var drop = _loot.TryDrop(DropSource.RunnerKill, 1, _match.WaveIndex);
                    if (drop != null) TakeLoot(drop);
                }
            }
        }

        /// <summary>
        /// Pushes the loadout everywhere it has to land. The hero reads a config; the turret system
        /// reads multipliers. Scaling turrets at FIRE time rather than at placement means a Doctrine
        /// card improves the guns already on the board, which is what a player expects.
        /// </summary>
        private void ApplyLoadoutToWorld()
        {
            _hero.Retune(_loadout.Effective);
            _turrets.DamageMultiplier = _loadout.TurretDamageMultiplier;
            _turrets.RangeMultiplier = _loadout.TurretRangeMultiplier;
            _build.RepairSpeedMultiplier = _loadout.RepairSpeedMultiplier;
        }

        /// <summary>Experience in, level-ups and skill points out, with a notice for the player.</summary>
        private void AwardXp(int amount)
        {
            if (amount <= 0) return;
            int gained = _loadout.Skills.AddXp(amount);
            if (gained <= 0) return;

            _lastLevel = _loadout.Skills.Level;
            Alert($"LEVEL {_lastLevel}  ({_loadout.Skills.UnspentPoints} skill points)", 4f);
            _sfx.Play(Sfx.Pickup, 1f, 0f);
        }

        /// <summary>
        /// Picks a drop up without stopping play. The inventory decides on its own whether it is an
        /// upgrade, worth carrying, or junk that turns straight into Scrip.
        /// </summary>
        private void TakeLoot(ItemInstance item)
        {
            var result = _loadout.Pickup(item);
            _lootNoticeTimer = 3.5f;
            _lootNotice = result.Outcome switch
            {
                PickupOutcome.EquippedEmptySlot => $"EQUIPPED  {item}",
                PickupOutcome.Upgraded => $"UPGRADE  {item}",
                PickupOutcome.Stowed => $"stowed  {item}",
                PickupOutcome.AutoScrapped => $"scrapped for {result.ScripGained} scrip  {item.Name}",
                _ => $"pack full, left it  {item.Name}",
            };
            if (result.Outcome is PickupOutcome.EquippedEmptySlot or PickupOutcome.Upgraded)
            {
                ApplyLoadoutToWorld();
                _sfx.Play(Sfx.Pickup, 0.9f, 0.02f);
            }
        }

        /// <summary>
        /// A wave is down. Pay experience, drop one guaranteed piece, and deal the pick-one-of-three.
        /// The card lands exactly when the player is deciding whether to take another wave, which is
        /// the decision the whole scan cycle is built around.
        /// </summary>
        private void OnWaveCleared()
        {
            AwardXp(LevelCurve.XpForWaveCleared(_match.WaveNumber));

            var drop = _loot.TryDrop(DropSource.WaveClear, 1, _match.WaveIndex);
            if (drop != null) TakeLoot(drop);

            _cardOffer = _loadout.Deck.Deal();
            _cardCursor = 0;
        }

        /// <summary>Reads the card pick. Blocks other input while the offer is up.</summary>
        private void ReadCardInput()
        {
            if (_cardOffer.Count == 0) return;
            var pad = Gamepad.current;
            var kb = Keyboard.current;

            if ((pad != null && (pad.dpad.left.wasPressedThisFrame || pad.leftStick.left.wasPressedThisFrame))
                || (kb != null && kb.leftArrowKey.wasPressedThisFrame))
            { _cardCursor = (_cardCursor - 1 + _cardOffer.Count) % _cardOffer.Count; _sfx.Play(Sfx.MenuTick, 0.6f, 0f); }

            if ((pad != null && (pad.dpad.right.wasPressedThisFrame || pad.leftStick.right.wasPressedThisFrame))
                || (kb != null && kb.rightArrowKey.wasPressedThisFrame))
            { _cardCursor = (_cardCursor + 1) % _cardOffer.Count; _sfx.Play(Sfx.MenuTick, 0.6f, 0f); }

            for (int i = 0; i < _cardOffer.Count && kb != null; i++)
            {
                var key = i == 0 ? kb.digit1Key : i == 1 ? kb.digit2Key : kb.digit3Key;
                if (key.wasPressedThisFrame) { _cardCursor = i; TakeCard(); return; }
            }

            bool confirm = (pad != null && pad.buttonSouth.wasPressedThisFrame)
                           || (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame));
            if (confirm) TakeCard();
        }

        private void TakeCard()
        {
            var card = _loadout.TakeImprovisation(_cardCursor);
            _cardOffer = System.Array.Empty<Improvisation>();
            if (card == null) return;

            ApplyLoadoutToWorld();
            Alert($"{card.Name} — {card.Text}", 4f);
            _sfx.Play(Sfx.MenuConfirm, 1f, 0f);
        }

        private void HandleSimEvents()
        {
            _eventScratch.Clear();
            _world.DrainEvents(_eventScratch);
            bool turretsChanged = false;
            foreach (var e in _eventScratch)
            {
                switch (e.Kind)
                {
                    case SimEventKind.SapperTargeted:
                        Alert("SAPPER SPOTTED — it is heading for your wall", 4f);
                        _sfx.Play(Sfx.SapperSpotted, 0.9f, 0f, minInterval: 2f);
                        break;
                    case SimEventKind.BreachPlanting:
                        Alert($"BREACH IN {e.F:F0}s — kill the Sapper or bring a drone", 4f);
                        _sfx.PlayAt(Sfx.BreachPlanting, CellWorld(e.B, 1f), 1f, 0f, minInterval: 1f);
                        break;
                    case SimEventKind.BreachStage:
                        if (e.A >= 0) { Alert("WALL BREACHED — they are coming through", 4f); _sfx.PlayAt(Sfx.BreachOpened, CellWorld(e.B, 1f), 1f, 0.03f); }
                        else if ((int)e.F > (int)BreachStage.Cracked) { Alert("BREACH WIDENING", 3f); _sfx.PlayAt(Sfx.BreachOpened, CellWorld(e.B, 1f), 0.7f, 0.08f); }
                        break;
                    case SimEventKind.BreachCollapsed:
                        Alert("WALL COLLAPSED", 4f);
                        _sfx.PlayAt(Sfx.WallCollapsed, CellWorld(e.B, 1f), 1f, 0.02f);
                        break;
                    case SimEventKind.BreachRepaired:
                        Alert("wall repaired", 2f);
                        _sfx.PlayAt(Sfx.Repaired, CellWorld(e.B, 1f), 0.9f, 0.02f);
                        break;
                    case SimEventKind.SpitterEngaged:
                        Alert("SPITTER — it is going for a turret", 3f);
                        _sfx.Play(Sfx.SpitterSeen, 0.8f, 0.05f, minInterval: 1.5f);
                        break;
                    case SimEventKind.StructureMauled:
                        if (e.B >= 0 && e.B < _turrets.Turrets.Count)
                        {
                            var mt = _turrets.Turrets[e.B];
                            _sfx.PlayAt(Sfx.Hit, new Vector3(mt.X + 0.5f, 1f, mt.Y + 0.5f),
                                        0.6f, 0.2f, minInterval: 0.25f);
                            if (_turrets.Damage(_map, e.B, (int)e.F))
                            {
                                Alert("TURRET TORN DOWN", 4f);
                                _sfx.PlayAt(Sfx.TurretDestroyed, new Vector3(mt.X + 0.5f, 1f, mt.Y + 0.5f), 1f, 0.02f);
                                turretsChanged = true;
                            }
                        }
                        break;
                    case SimEventKind.StructureHit:
                        if (e.B >= 0 && e.B < _turrets.Turrets.Count)
                        {
                            var t = _turrets.Turrets[e.B];
                            _blasts.Add(new Blast { Center = new Vector3(t.X + 0.5f, 0.05f, t.Y + 0.5f), Radius = 0.7f, Ttl = BlastLife * 0.5f });
                            _sfx.PlayAt(Sfx.Hit, new Vector3(t.X + 0.5f, 1f, t.Y + 0.5f), 0.7f, 0.15f, minInterval: 0.2f);
                            if (_turrets.Damage(_map, e.B, (int)e.F)) { Alert("TURRET DESTROYED", 4f); _sfx.PlayAt(Sfx.TurretDestroyed, new Vector3(t.X + 0.5f, 1f, t.Y + 0.5f), 1f, 0.02f); turretsChanged = true; }
                        }
                        break;
                }
            }
            if (turretsChanged) SyncTurretObjects();
        }

        private static Vector3 CellWorld(int cell, float height) => new Vector3(cell % GridW + 0.5f, height, cell / GridW + 0.5f);

        private void Alert(string text, float seconds)
        {
            for (int i = 0; i < _alerts.Count; i++)
                if (_alerts[i].Text == text) { _alerts[i] = (text, seconds); return; }
            _alerts.Add((text, seconds));
            if (_alerts.Count > 4) _alerts.RemoveAt(0);
        }

        // ------------------------------------------------------------------ input

        private void ReadModeToggles()
        {
            var pad = Gamepad.current;
            var kb = Keyboard.current;
            if (_declineNoticeTimer > 0f) _declineNoticeTimer = Mathf.Max(0f, _declineNoticeTimer - Time.deltaTime);
            if (_lootNoticeTimer > 0f) _lootNoticeTimer = Mathf.Max(0f, _lootNoticeTimer - Time.deltaTime);

            // An offer on the table owns the input until it is answered, and so does the truck.
            if (_cardOffer.Count > 0) { ReadCardInput(); return; }
            if (_match.Phase == MatchPhase.Extraction) { ReadTruckInput(kb, pad); return; }

            if ((pad != null && pad.buttonNorth.wasPressedThisFrame && !_buildMode)
                || (kb != null && kb.iKey.wasPressedThisFrame))
                _showInventory = !_showInventory;

            if (kb != null && kb.kKey.wasPressedThisFrame) { _showSkills = !_showSkills; _showInventory = false; }
            if (_showSkills) { ReadSkillInput(kb, pad); return; }
            // Tab is still a plain toggle for keyboard players who just want in and out.
            bool toggle = kb != null && kb.tabKey.wasPressedThisFrame;
            if (toggle)
            {
                SetBuildMode(!_buildMode);
                if (!_buildMode) _focus.Release();
                _sfx.Play(_buildMode ? Sfx.MenuOpen : Sfx.MenuConfirm, 0.7f, 0f);
            }

            // LB (or held Shift on the keyboard) opens the radial. Holding it is the whole interaction:
            // the wheel appears, adrenaline focus slows the world, and letting go commits the pick.
            bool wheelDown = (pad != null && pad.leftShoulder.isPressed)
                             || (kb != null && kb.leftShiftKey.isPressed && !_match.IsOver);
            UpdateBuildWheel(wheelDown, pad, kb);

            if ((pad != null && pad.rightStickButton.wasPressedThisFrame) || (kb != null && kb.nKey.wasPressedThisFrame))
                _showMinimap = !_showMinimap;

            bool startWave = (pad != null && pad.selectButton.wasPressedThisFrame) || (kb != null && kb.enterKey.wasPressedThisFrame);
            if (startWave && _match.Phase == MatchPhase.Setup) _match.StartWaveNow();

            // Scan cycle (ADR-005): call this your last wave here, then pull out when packed.
            bool lastCall = (pad != null && pad.dpad.down.wasPressedThisFrame) || (kb != null && kb.lKey.wasPressedThisFrame);
            if (lastCall)
            {
                if (_match.Phase == MatchPhase.Extraction)
                {
                    if (_match.PullOutNow()) _sfx.Play(Sfx.MenuConfirm, 0.9f, 0f);
                }
                else
                {
                    var result = _match.DeclareLastWave();
                    _sfx.Play(result == DeclareResult.Ok ? Sfx.WaveHorn : Sfx.MenuTick, 0.8f, 0f);
                    _declineNotice = result switch
                    {
                        DeclareResult.Ok => "LAST WAVE CALLED — hold it, then pack up",
                        DeclareResult.TooEarly =>
                            $"too early — hold {_match.Cycle.MinWavesBeforeExtract} waves before you can call it",
                        DeclareResult.NowhereToGo => "there is nowhere to fall back to",
                        DeclareResult.AlreadyDeclared => "already called",
                        _ => "",
                    };
                    _declineNoticeTimer = 3f;
                }
            }
        }

        /// <summary>Deals an upgrade offer and opens the kit panel, for a capture. Harness only.</summary>
        public void ShowProgressionForCapture()
        {
            _loadout.Skills.AddXp(LevelCurve.TotalXpFor(4));
            _lastLevel = _loadout.Skills.Level;

            var roller = new ItemRoller(42);
            for (int i = 0; i < 6; i++)
            {
                var item = roller.TryDrop(DropSource.SapperKill, 2, 3);
                if (item != null) _loadout.Pickup(item);
            }
            ApplyLoadoutToWorld();

            _cardOffer = _loadout.Deck.Deal();
            _cardCursor = 1;
            _showInventory = true;
        }

        /// <summary>
        /// Lines the imported civilian models up in front of the camera so we can see what the free
        /// CC0 art actually looks like under our comic shader. Harness only.
        /// </summary>
        public void ShowCharacterLineupForCapture()
        {
            // Prefer the built civilian prefabs (Animator + looping walk) over the raw models.
            var prefabs = Resources.LoadAll<GameObject>("Civilians");
            bool animated = prefabs != null && prefabs.Length > 0;
            if (!animated) prefabs = Resources.LoadAll<GameObject>("Characters");
            if (prefabs == null || prefabs.Length == 0)
            {
                Debug.LogWarning("[Lineup] no character models under Resources/Characters");
                return;
            }

            // Clear the graybox out of frame so the shot is about the models.
            foreach (var n in new[] { "Ground", "Vault", "SpawnPad", "Hero" })
            {
                var go0 = GameObject.Find(n);
                if (go0 != null) go0.SetActive(false);
            }

            var root = new GameObject("CharacterLineup");

            // A crowd, not a police lineup: several rows, staggered, all walking at the camera.
            int perRow = animated ? 9 : prefabs.Length;
            int rows = animated ? 5 : 1;
            int count = perRow * rows;
            var rng = new System.Random(7);

            for (int i = 0; i < count; i++)
            {
                var template = prefabs[i % prefabs.Length];

                // The walk clip animates the character's OWN root transform, so anything written to
                // it gets overwritten the moment the animator evaluates: position snapped to the
                // origin and scale to the clip's. Give each one a plain wrapper to live in and put
                // the placement on that, where no animation can reach it.
                int row = i / perRow, col = i % perRow;
                float jitterX = (float)(rng.NextDouble() - 0.5) * 0.7f;
                float jitterZ = (float)(rng.NextDouble() - 0.5) * 0.5f;

                var slot = new GameObject("Slot_" + i);
                slot.transform.SetParent(root.transform, false);
                slot.transform.localPosition = new Vector3(
                    (col - (perRow - 1) * 0.5f) * 1.15f + jitterX,
                    0f,
                    row * 1.6f + jitterZ);
                slot.transform.localRotation =
                    Quaternion.Euler(0f, 180f + (float)(rng.NextDouble() - 0.5) * 18f, 0f);

                // A pivot sits between the slot and the character so placement and animation never
                // fight. It used to carry a 90-degree correction, needed only while the characters
                // were stuck in their bind pose: once the clip actually drives them it supplies the
                // right orientation itself, and the correction tipped everyone onto their backs.
                var pivot = new GameObject("Pivot");
                pivot.transform.SetParent(slot.transform, false);
                pivot.transform.localRotation = Quaternion.identity;

                var go = Instantiate(template, pivot.transform);
                go.name = template.name + "_" + i;
                go.transform.localPosition = Vector3.zero;

                // Desynchronise the walk cycle or the crowd marches in lockstep, which is the
                // single most obvious tell that a crowd is fake.
                // Drive the walk with the legacy Animation component. See LegacyClipMaker for the
                // three approaches that failed silently before this one.
                if (_walkClip == null)
                {
                    _walkClip = Resources.Load<AnimationClip>("Civilians/WalkLegacy");
                    Debug.Log($"[Anim] legacy walk = {(_walkClip != null ? _walkClip.name : "NONE")} " +
                              $"legacy={(_walkClip != null && _walkClip.legacy)}");
                }

                // DESTROY the Animator rather than disable it. The legacy Animation component is
                // suppressed while an Animator exists on the same GameObject, disabled or not, so a
                // merely-disabled Animator leaves the character silently stuck in its bind pose.
                foreach (var a0 in go.GetComponentsInChildren<Animator>()) Destroy(a0);

                if (_walkClip != null && _walkClip.legacy)
                {
                    var legacy = go.AddComponent<Animation>();
                    legacy.AddClip(_walkClip, "walk");
                    legacy.wrapMode = WrapMode.Loop;
                    legacy.playAutomatically = false;
                    legacy.Play("walk");

                    // Desynchronise, or the crowd marches in lockstep, which is the single most
                    // obvious tell that a crowd is fake.
                    var state = legacy["walk"];
                    if (i == 0)
                        Debug.Log($"[Anim] legacy state={(state != null ? "ok" : "NULL")} " +
                                  $"isPlaying={legacy.isPlaying} clipCount={legacy.GetClipCount()}");
                    if (state != null)
                    {
                        state.time = (float)rng.NextDouble() * _walkClip.length;
                        state.speed = 0.9f + (float)rng.NextDouble() * 0.25f;
                    }
                }

                // Size them a few frames later, once the walk is actually posing the mesh. Measuring
                // now would measure the bind pose, which is not the shape that ends up on screen.
                go.AddComponent<FitToHeight>().Configure(slot.transform, 1.8f);

                // Give them the game's own look rather than whatever the FBX shipped with.
                foreach (var r in go.GetComponentsInChildren<Renderer>())
                {
                    var mats = r.sharedMaterials;
                    var swapped = new Material[mats.Length];
                    for (int m = 0; m < mats.Length; m++)
                    {
                        var src = mats[m];
                        var mat = new Material(LitShader) { enableInstancing = false };
                        Color tint = src != null && src.HasProperty(BaseColorId)
                            ? src.GetColor(BaseColorId)
                            : (src != null ? src.color : Color.grey);
                        mat.color = tint;
                        if (mat.HasProperty(BaseColorId)) mat.SetColor(BaseColorId, tint);
                        // Characters carry smoothed normals in their tangents; tell the shader to
                        // extrude along those, and keep the line fine. A thick hull on a detailed
                        // mesh reads as a smudge, not as ink.
                        if (mat.HasProperty(OutlineWidthId)) mat.SetFloat(OutlineWidthId, InkFigure);
                        if (mat.HasProperty(SmoothOutlineId))
                        {
                            mat.SetFloat(SmoothOutlineId, 1f);
                            mat.EnableKeyword("_SMOOTH_OUTLINE");
                        }
                        if (src != null && src.mainTexture != null) mat.mainTexture = src.mainTexture;
                        swapped[m] = mat;
                    }
                    r.sharedMaterials = swapped;
                }
            }

            // Fixed framing, deliberately. Four separate attempts to compute this from renderer
            // bounds put the camera inside somebody or aimed it at empty sky, because bounds on
            // freshly spawned animated characters are not trustworthy on the frame you spawn them.
            // The crowd's layout is known, so the camera can be too.
            const float CrowdZ = 20f;
            root.transform.position = new Vector3(GridW * 0.5f, 0f, CrowdZ);

            if (_camera != null)
            {
                _camera.transform.position = new Vector3(GridW * 0.5f, 1.6f, CrowdZ - 9f);
                _camera.transform.rotation = Quaternion.Euler(1.5f, 0f, 0f);
                _camera.nearClipPlane = 0.1f;
            }

            _lineupMode = true;
            _hudHidden = true;
            Debug.Log($"[Lineup] placed {prefabs.Length} characters");
        }

        private bool _hudHidden;
        private AnimationClip? _walkClip;
        private CivilianCrowd? _crowd;
        private Transform? _heroBody;
        private bool _environmentDressed;
        /// <summary>Model used for the player. Excluded from the civilian pool.</summary>
        private const string HeroModelName = "Adventurer_Civilian";
        /// <summary>Real bodies for the nearest agents. The rest stay instanced capsules.</summary>
        private const int CivilianPoolSize = 110;
        private bool _lineupMode;

        /// <summary>
        /// Builds one walking civilian and returns the SLOT that positions it.
        ///
        /// The three-level structure matters and is not decoration. The slot carries position,
        /// heading and the height correction. The pivot exists so any future orientation fix has
        /// somewhere to live. The character itself is driven by the animation, which overwrites its
        /// own transform every frame; anything written there is lost.
        /// </summary>
        private Transform BuildCivilian(GameObject template, Transform parent, System.Random rng)
        {
            var slot = new GameObject("Civilian");
            slot.transform.SetParent(parent, false);

            var pivot = new GameObject("Pivot");
            pivot.transform.SetParent(slot.transform, false);
            pivot.transform.localRotation = Quaternion.identity;

            var go = Instantiate(template, pivot.transform);
            go.transform.localPosition = Vector3.zero;

            // Give them the game's own look rather than whatever the FBX shipped with.
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                var swapped = new Material[mats.Length];
                for (int m = 0; m < mats.Length; m++)
                {
                    var src = mats[m];
                    var mat = new Material(LitShader) { enableInstancing = false };
                    Color tint = src != null && src.HasProperty(BaseColorId)
                        ? src.GetColor(BaseColorId)
                        : (src != null ? src.color : Color.grey);
                    mat.color = tint;
                    if (mat.HasProperty(BaseColorId)) mat.SetColor(BaseColorId, tint);
                    if (mat.HasProperty(OutlineWidthId)) mat.SetFloat(OutlineWidthId, InkFigure);
                    if (mat.HasProperty(SmoothOutlineId))
                    {
                        mat.SetFloat(SmoothOutlineId, 1f);
                        mat.EnableKeyword("_SMOOTH_OUTLINE");
                    }
                    if (src != null && src.mainTexture != null) mat.mainTexture = src.mainTexture;
                    swapped[m] = mat;
                }
                r.sharedMaterials = swapped;
            }

            // An Animator suppresses the legacy Animation component even when disabled, so it has
            // to go rather than just be switched off.
            foreach (var a0 in go.GetComponentsInChildren<Animator>()) Destroy(a0);

            if (_walkClip != null && _walkClip.legacy)
            {
                var legacy = go.AddComponent<Animation>();
                legacy.AddClip(_walkClip, "walk");
                legacy.wrapMode = WrapMode.Loop;
                legacy.playAutomatically = false;
                legacy.Play("walk");

                // Desynchronise, or the crowd marches in lockstep, which is the single most obvious
                // tell that a crowd is fake.
                var state = legacy["walk"];
                if (state != null)
                {
                    state.time = (float)rng.NextDouble() * _walkClip.length;
                    state.speed = 0.9f + (float)rng.NextDouble() * 0.25f;
                }
            }

            // Size a few frames later, once the walk is posing the mesh: the clip animates scale on
            // the armature, so a standing measurement is not the shape that ends up on screen.
            go.AddComponent<FitToHeight>().Configure(slot.transform, 1.8f);
            return slot.transform;
        }

        /// <summary>Loads the legacy walk clip once. Null when the art has not been imported.</summary>
        private void EnsureWalkClip()
        {
            if (_walkClip != null) return;
            _walkClip = Resources.Load<AnimationClip>("Civilians/WalkLegacy");
        }

        /// <summary>
        /// Builds the pool of real bodies used for the nearest slice of the swarm. Capacity is
        /// deliberately modest: these are skinned characters, and the point is that the part of the
        /// crowd a player can actually read looks like people.
        /// </summary>
        private void BuildCivilianPool()
        {
            var prefabs = Resources.LoadAll<GameObject>("Civilians");
            if (prefabs == null || prefabs.Length == 0) return;

            EnsureWalkClip();

            var root = new GameObject("CivilianCrowd").transform;
            _crowd = new CivilianCrowd(root, CivilianPoolSize);

            // The player's model must not also be walking in the horde: seeing yourself in the
            // crowd you are shooting at reads as a bug, not as variety.
            var pool = new List<GameObject>();
            foreach (var prefab in prefabs)
                if (prefab != null && prefab.name != HeroModelName) pool.Add(prefab);
            if (pool.Count == 0) pool.AddRange(prefabs);

            var rng = new System.Random(20260911);
            for (int i = 0; i < CivilianPoolSize; i++)
                _crowd.AddSlot(BuildCivilian(pool[i % pool.Count], root, rng));

            Debug.Log($"[Crowd] pool of {_crowd.SlotCount} civilians from {prefabs.Length} models");
        }

        /// <summary>
        /// Gives the player a body. Same construction as a civilian, but kept out of the crowd pool
        /// and driven by the hero's own position and facing rather than by an agent id.
        /// </summary>
        private void BuildHeroBody()
        {
            var prefab = Resources.Load<GameObject>("Civilians/" + HeroModelName);
            if (prefab == null)
            {
                Debug.LogWarning($"[Hero] {HeroModelName} not found; the player stays a capsule");
                return;
            }

            EnsureWalkClip();
            var root = new GameObject("HeroBody").transform;
            _heroBody = BuildCivilian(prefab, root, new System.Random(1));
            Debug.Log("[Hero] body built");
        }

        /// <summary>Keeps the hero's body on the hero. Facing comes from the aim, not from movement.</summary>
        private void UpdateHeroBody()
        {
            if (_heroBody == null) return;

            bool show = !_hero.IsDown && !_match.IsOver;
            if (_heroBody.gameObject.activeSelf != show) _heroBody.gameObject.SetActive(show);
            if (!show) return;

            _heroBody.position = new Vector3(_hero.Position.X, 0f, _hero.Position.Y);

            var facing = new Vector3(_hero.Facing.X, 0f, _hero.Facing.Y);
            if (facing.sqrMagnitude > 1e-6f)
                _heroBody.rotation = Quaternion.LookRotation(facing.normalized, Vector3.up);
        }

        /// <summary>
        /// Dresses the map with trees, bushes and abandoned cars. Runs once; the dressing is static
        /// and deterministic, so the same map looks the same every run.
        /// </summary>
        private void DressEnvironment()
        {
            var all = Resources.LoadAll<GameObject>("Environment");
            if (all == null || all.Length == 0) return;

            var trees = new List<GameObject>();
            var bushes = new List<GameObject>();
            var cars = new List<GameObject>();
            GameObject? roadTile = null;
            foreach (var go in all)
            {
                if (go == null) continue;
                string n = go.name;
                if (n == "Street_Straight") roadTile = go;
                else if (n.StartsWith("Street") || n.StartsWith("Sign")) continue;
                else if (n.StartsWith("Bush")) bushes.Add(go);
                else if (n.Contains("Tree")) trees.Add(go);
                else cars.Add(go);
            }

            var root = new GameObject("Environment").transform;
            var dresser = new EnvironmentDresser(_map, root, ReskinForComic);

            // Keep the spawn lane, the objective and the hero's ground clear, or the level dresses
            // itself shut and the horde has nowhere to walk.
            // Road first so props never land on top of it. Six cells wide: the corridor KeepClear
            // holds open is seven, so the verge stays walkable on both sides.
            if (roadTile != null)
                dresser.LayRoad(roadTile, GridH / 2, 6, (x, y) => _map.KindAt(x, y) != WallKind.None);
            else
                Debug.LogWarning("[Env] no Street_Straight in Resources/Environment; no road laid");

            dresser.Dress(trees, bushes, cars, KeepClear);

            Debug.Log($"[Env] placed {dresser.Placed} props " +
                      $"({trees.Count} tree models, {bushes.Count} bush, {cars.Count} vehicle)");
        }

        /// <summary>Cells that must stay empty no matter what the dresser wants.</summary>
        private bool KeepClear(int x, int y)
        {
            // The corridor the horde walks, plus a margin at each end.
            if (Mathf.Abs(y - GridH / 2) <= 3) return true;
            if (x <= 4 || x >= GridW - 5) return true;

            var heroCell = _map.WorldToCell(_hero != null ? _hero.Position : HeroSpawn);
            if (Mathf.Abs(x - heroCell.Item1) <= 3 && Mathf.Abs(y - heroCell.Item2) <= 3) return true;

            return false;
        }

        /// <summary>Re-materialises an imported prop into the game's comic look.</summary>
        private Material ReskinForComic(Material? source)
        {
            var mat = new Material(LitShader) { enableInstancing = false };
            Color tint = source != null && source.HasProperty(BaseColorId)
                ? source.GetColor(BaseColorId)
                : (source != null ? source.color : Color.grey);
            mat.color = tint;
            if (mat.HasProperty(BaseColorId)) mat.SetColor(BaseColorId, tint);
            if (mat.HasProperty(OutlineWidthId)) mat.SetFloat(OutlineWidthId, InkProp);
            if (source != null && source.mainTexture != null) mat.mainTexture = source.mainTexture;
            return mat;
        }

        /// <summary>
        /// Adds the colour grade. Three effects, chosen because each one does something the comic
        /// look specifically needs and nothing else:
        ///
        ///   Tonemapping keeps the flat banded colour from clipping where fire and muzzle flash
        ///   blow past white, which is the one place a cel shader looks broken.
        ///   Bloom gives those same highlights somewhere to go.
        ///   Vignette pulls the eye off the edge of a play area that has no horizon.
        ///
        /// Built in code rather than as an asset so it survives a clean checkout and CI can rebuild
        /// it, same reasoning as the pipeline assets in UrpSetup.
        /// </summary>
        private void ApplyColourGrade()
        {
            var go = new GameObject("Grade");
            var volume = go.AddComponent<UnityEngine.Rendering.Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;

            var profile = ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();
            volume.sharedProfile = profile;

            if (profile.Add<UnityEngine.Rendering.Universal.Tonemapping>() is { } tonemap)
            {
                tonemap.active = true;
                tonemap.mode.overrideState = true;
                tonemap.mode.value = UnityEngine.Rendering.Universal.TonemappingMode.Neutral;
            }

            if (profile.Add<UnityEngine.Rendering.Universal.Bloom>() is { } bloom)
            {
                bloom.active = true;
                bloom.threshold.overrideState = true;
                bloom.threshold.value = 1.05f;      // only genuine highlights, not the whole sky
                bloom.intensity.overrideState = true;
                bloom.intensity.value = 0.55f;
                bloom.scatter.overrideState = true;
                bloom.scatter.value = 0.62f;
            }

            if (profile.Add<UnityEngine.Rendering.Universal.Vignette>() is { } vignette)
            {
                vignette.active = true;
                vignette.intensity.overrideState = true;
                vignette.intensity.value = 0.26f;
                vignette.smoothness.overrideState = true;
                vignette.smoothness.value = 0.5f;
            }

            if (_camera != null)
            {
                var data = _camera.GetUniversalAdditionalCameraData();
                if (data != null) data.renderPostProcessing = true;
            }

            Debug.Log("[Grade] tonemap, bloom and vignette applied");
        }

        /// <summary>Starts the first wave, so a smoke capture can actually see combat. Harness only.</summary>
        public void StartWaveForCapture() => _match.StartWaveNow();

        /// <summary>Opens the skill screen with points banked, for a capture. Harness only.</summary>
        public void ShowSkillsForCapture()
        {
            _loadout.Skills.AddXp(LevelCurve.TotalXpFor(9));
            _loadout.SpendSkillPoint("t-marks");
            _loadout.SpendSkillPoint("t-marks");
            _loadout.SpendSkillPoint("d-lanes");
            ApplyLoadoutToWorld();
            _skillCursor = 1;
            _showSkills = true;
        }

        /// <summary>Opens the radial for a capture, aimed at one option. Screenshot harness only.</summary>
        public void OpenBuildWheelForCapture(int option)
        {
            if (!_buildMode) SetBuildMode(true);
            // Deliberately NOT setting _wheelHeld: the input driver treats that as "button was
            // down last frame" and would close the wheel the instant it sees it released.
            _wheel.Open(_build.Options.Count, option);
            _focus.TryEngage();
            float angle = BuildWheel.AngleForIndex(Mathf.Clamp(option, 0, Mathf.Max(0, _build.Options.Count - 1)),
                                                  _build.Options.Count);
            _wheel.Aim(Mathf.Sin(angle), Mathf.Cos(angle));
        }

        /// <summary>Tactical camera, for the screenshot harness. Shows the field and the crowd.</summary>
        public void EnterTacticalViewForCapture()
        {
            SetBuildMode(true);
            // The tactical camera frames the build cursor, which SetBuildMode parks on the hero.
            // For a capture we want the whole field, so centre it on the map instead.
            _build.SetCursor(GridW / 2, GridH / 2);
            _build.Refresh();
        }

        /// <summary>
        /// Drives the radial build menu. Holding the button opens build mode, spends a focus charge
        /// and shows the wheel; releasing commits whatever is highlighted.
        /// </summary>
        private void UpdateBuildWheel(bool held, Gamepad? pad, Keyboard? kb)
        {
            if (held && !_wheelHeld)
            {
                _wheelHeld = true;
                if (!_buildMode) SetBuildMode(true);
                _wheel.Open(_build.Options.Count, _build.Selected);
                bool slowed = _focus.TryEngage();
                _sfx.Play(Sfx.MenuOpen, 0.8f, 0f);
                if (!slowed)
                {
                    _declineNotice = "focus depleted - wheel is open, time is not slowed";
                    _declineNoticeTimer = 2f;
                }
                return;
            }

            if (held)
            {
                // Right stick aims; the mouse aims from screen centre for keyboard players.
                float ax = 0f, ay = 0f;
                if (pad != null)
                {
                    var stick = pad.rightStick.ReadValue();
                    ax = stick.x; ay = stick.y;
                }
                if (Mathf.Abs(ax) + Mathf.Abs(ay) < 0.2f && Mouse.current != null)
                {
                    Vector2 m = Mouse.current.position.ReadValue();
                    ax = (m.x - Screen.width * 0.5f) / Mathf.Max(1f, Screen.height * 0.35f);
                    ay = (m.y - Screen.height * 0.5f) / Mathf.Max(1f, Screen.height * 0.35f);
                }

                int before = _wheel.Selected;
                _wheel.Aim(ax, ay);

                // Number keys remain the fast path for anyone who already knows the order.
                if (kb != null)
                {
                    if (kb.digit1Key.wasPressedThisFrame) _wheel.Step(-_wheel.Selected + 0);
                    if (kb.leftArrowKey.wasPressedThisFrame) _wheel.Step(-1);
                    if (kb.rightArrowKey.wasPressedThisFrame) _wheel.Step(1);
                }

                if (_wheel.Selected != before && _wheel.Selected >= 0)
                    _sfx.Play(Sfx.MenuTick, 0.5f, 0.02f);
                return;
            }

            if (_wheelHeld)
            {
                _wheelHeld = false;
                int chosen = _wheel.Close();
                if (chosen >= 0 && chosen < _build.Options.Count)
                {
                    _build.SelectOption(chosen);
                    _build.Refresh();
                    _sfx.Play(Sfx.MenuConfirm, 0.8f, 0f);
                }
                _focus.Release();
            }
        }

        private void SetBuildMode(bool on)
        {
            _buildMode = on;
            _camMode = on ? CameraMode.Tactical : CameraMode.Chase;
            if (on)
            {
                var (hx, hy) = _map.WorldToCell(_hero.Position);
                _build.SetCursor(hx, hy);
                _build.Refresh();
                _lastPaintCell = (-1, -1);
                _cursorRepeatTimer = 0f;
                _cursorHeldTime = 0f;
            }
        }

        private void ReadBuildInput(float dt)
        {
            var pad = Gamepad.current;
            var kb = Keyboard.current;

            if (pad != null && pad.buttonEast.wasPressedThisFrame) { SetBuildMode(false); _sfx.Play(Sfx.MenuConfirm, 0.7f, 0f); return; }

            // Stepped cursor: first step immediately, then 8 cells/s, 16 cells/s after 0.6 s held.
            Vector2 dir = Vector2.zero;
            if (pad != null)
            {
                Vector2 ls = pad.leftStick.ReadValue();
                if (ls.sqrMagnitude > 0.25f) dir = ls;
                if (pad.dpad.up.isPressed) dir.y = 1f;
                if (pad.dpad.down.isPressed) dir.y = -1f;
            }
            if (kb != null)
            {
                if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) dir.x = -1f;
                if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) dir.x = 1f;
                if (kb.upArrowKey.isPressed || kb.wKey.isPressed) dir.y = 1f;
                if (kb.downArrowKey.isPressed || kb.sKey.isPressed) dir.y = -1f;
            }

            int dx = Mathf.Abs(dir.x) > 0.5f ? (int)Mathf.Sign(dir.x) : 0;
            int dy = Mathf.Abs(dir.y) > 0.5f ? (int)Mathf.Sign(dir.y) : 0;
            bool moved = false;
            if (dx != 0 || dy != 0)
            {
                _cursorRepeatTimer -= dt;
                if (_cursorHeldTime == 0f || _cursorRepeatTimer <= 0f)
                {
                    _build.MoveCursor(dx, dy);
                    moved = true;
                    _sfx.Play(Sfx.CursorTick, 0.5f, 0.15f, minInterval: 0.04f);
                    float rate = CursorRamp[0].CellsPerSecond;
                    foreach (var step in CursorRamp) if (_cursorHeldTime >= step.AfterSeconds) rate = step.CellsPerSecond;
                    _cursorRepeatTimer = 1f / rate;
                }
                _cursorHeldTime += dt;
            }
            else
            {
                _cursorHeldTime = 0f;
                _cursorRepeatTimer = 0f;
            }

            bool cycle = (pad != null && (pad.rightShoulder.wasPressedThisFrame || pad.dpad.right.wasPressedThisFrame)) || (kb != null && kb.qKey.wasPressedThisFrame);
            bool cycleBack = pad != null && pad.dpad.left.wasPressedThisFrame;
            if (cycle) { _build.CycleItem(1); moved = true; _sfx.Play(Sfx.MenuTick, 0.7f, 0f); }
            else if (cycleBack) { _build.CycleItem(-1); moved = true; _sfx.Play(Sfx.MenuTick, 0.7f, 0f); }
            bool upgrade = (pad != null && pad.buttonNorth.wasPressedThisFrame) || (kb != null && kb.uKey.wasPressedThisFrame);
            if (upgrade)
            {
                if (_build.TryUpgrade()) { SyncTurretObjects(); _sfx.Play(Sfx.Upgrade, 0.9f, 0f); }
                else if (_build.HoveredTurret >= 0) _sfx.Play(Sfx.Refuse, 0.6f, 0f);
            }

            bool placePressed = (pad != null && pad.buttonSouth.wasPressedThisFrame) || (kb != null && kb.spaceKey.wasPressedThisFrame);
            bool placeHeld = (pad != null && pad.buttonSouth.isPressed) || (kb != null && kb.spaceKey.isPressed);
            bool sell = (pad != null && pad.buttonWest.wasPressedThisFrame) || (kb != null && kb.xKey.wasPressedThisFrame);

            if (moved) _build.Refresh();

            var cell = (_build.CursorX, _build.CursorY);
            if (placePressed || (placeHeld && moved && cell != _lastPaintCell))
            {
                if (_build.TryPlace()) { _lastPaintCell = cell; SyncTurretObjects(); _sfx.Play(Sfx.Place, 0.8f, 0.1f, minInterval: 0.03f); }
                else if (placePressed) _sfx.Play(Sfx.Refuse, 0.6f, 0f, minInterval: 0.2f);
            }
            if (!placeHeld) _lastPaintCell = (-1, -1);

            if (sell)
            {
                if (_build.TrySell()) { SyncTurretObjects(); _sfx.Play(Sfx.Sell, 0.8f, 0.03f); }
                else _sfx.Play(Sfx.Refuse, 0.5f, 0f, minInterval: 0.2f);
            }

            // Keep the preview honest against a moving swarm even when the cursor is still.
            if (!moved && Time.frameCount % 6 == 0) _build.Refresh();

            _hero.Tick(dt);
        }

        private void ReadHeroInput(float dt)
        {
            var pad = Gamepad.current;
            var kb = Keyboard.current;
            var mouse = Mouse.current;

            Vector2 move = Vector2.zero;
            Vector2 look = Vector2.zero;
            if (pad != null)
            {
                move = pad.leftStick.ReadValue();
                look = pad.rightStick.ReadValue();
            }
            if (kb != null)
            {
                if (kb.wKey.isPressed) move.y += 1f;
                if (kb.sKey.isPressed) move.y -= 1f;
                if (kb.dKey.isPressed) move.x += 1f;
                if (kb.aKey.isPressed) move.x -= 1f;
            }
            if (move.sqrMagnitude > 1f) move.Normalize();

            float yawDelta = look.x * StickYawSpeed * dt;
            float pitchDelta = -look.y * StickPitchSpeed * dt;
            if (mouse != null)
            {
                Vector2 md = mouse.delta.ReadValue();
                yawDelta += md.x * MouseYawPerPixel;
                pitchDelta -= md.y * MousePitchPerPixel;
            }
            _camYaw += yawDelta;
            _camPitch = Mathf.Clamp(_camPitch + pitchDelta, PitchMin, PitchMax);

            Vector3 camFwd = CameraForward();
            Vector3 camRight = new Vector3(camFwd.z, 0f, -camFwd.x);
            _hero.Aim(new Vec2(camFwd.x, camFwd.z));
            Vector3 worldMove = camFwd * move.y + camRight * move.x;
            _hero.Move(_map, new Vec2(worldMove.x, worldMove.z), dt);

            _hero.Tick(dt);
            _hero.AimStrike(StrikeAimPoint());
            _impactScratch.Clear();
            _hero.TickStrike(_world, dt, _impactScratch);
            foreach (var imp in _impactScratch)
            {
                _blasts.Add(new Blast { Center = ToWorld(imp.Center, 0.05f), Radius = imp.Radius, Ttl = BlastLife });
                _sfx.PlayAt(Sfx.Bomb, ToWorld(imp.Center, 0.5f), 1f, 0.12f);
            }

            bool fire = (pad != null && pad.rightTrigger.isPressed) || (mouse != null && mouse.leftButton.isPressed);
            if (fire && _hero.TryFire(_world, Random.Range(-2.5f, 2.5f), out ShotResult shot))
            {
                _tracers.Add(new Tracer
                {
                    A = ToWorld(shot.Origin, 0.75f),
                    B = ToWorld(shot.End, shot.Hit ? 0.6f : 0.75f),
                    Ttl = TracerLife,
                });
                _sfx.Play(Sfx.Shot, 0.75f, 0.08f);
                if (shot.Killed) _sfx.PlayAt(Sfx.Kill, ToWorld(shot.End, 0.6f), 0.8f, 0.12f);
                else if (shot.Hit) _sfx.Play(Sfx.Hit, 0.5f, 0.15f, minInterval: 0.05f);
                else if (shot.HitWall)
                {
                    _sfx.PlayAt(Sfx.Place, ToWorld(shot.End, 0.8f), 0.35f, 0.2f, minInterval: 0.09f);
                    if (_map.KindAt(shot.WallX, shot.WallY) == WallKind.Barricade)
                        Alert("careful — that is your own wall", 1.2f);
                }
            }

            bool strike = (pad != null && pad.buttonNorth.wasPressedThisFrame)
                       || (mouse != null && mouse.rightButton.wasPressedThisFrame)
                       || (kb != null && kb.qKey.wasPressedThisFrame);
            if (strike)
            {
                if (_hero.TryAirstrike()) _sfx.Play(Sfx.StrikeCall, 0.9f, 0f);
                else _sfx.Play(Sfx.Refuse, 0.5f, 0f, minInterval: 0.3f);
            }
        }

        /// <summary>Where the camera looks at the ground; falls back to max range along the look axis when looking at the sky.</summary>
        private Vec2 StrikeAimPoint()
        {
            Vector3 origin = _camera.transform.position;
            Vector3 dir = _camera.transform.forward;
            if (dir.y < -1e-3f)
            {
                float t = -origin.y / dir.y;
                Vector3 hit = origin + dir * t;
                return new Vec2(hit.x, hit.z);
            }
            Vector3 flat = new Vector3(dir.x, 0f, dir.z);
            if (flat.sqrMagnitude < 1e-6f) flat = CameraForward();
            flat.Normalize();
            return _hero.Position + new Vec2(flat.x, flat.z) * 100f; // clamped by AimStrike
        }

        private bool RestartPressed()
        {
            var pad = Gamepad.current;
            var kb = Keyboard.current;
            return (pad != null && pad.buttonSouth.wasPressedThisFrame)
                || (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame));
        }

        // ------------------------------------------------------------------ visuals

        private static Vector3 ToWorld(Vec2 p, float height) => new Vector3(p.X, height, p.Y);

        private Vector3 CameraForward()
        {
            float r = _camYaw * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(r), 0f, Mathf.Cos(r));
        }

        private void UpdateEffects(float dt)
        {
            for (int i = _alerts.Count - 1; i >= 0; i--)
            {
                var a = _alerts[i];
                a.Ttl -= dt;
                if (a.Ttl <= 0f) _alerts.RemoveAt(i); else _alerts[i] = a;
            }
            for (int i = _tracers.Count - 1; i >= 0; i--)
            {
                var t = _tracers[i];
                t.Ttl -= dt;
                if (t.Ttl <= 0f) _tracers.RemoveAt(i); else _tracers[i] = t;
            }
            for (int i = _blasts.Count - 1; i >= 0; i--)
            {
                var b = _blasts[i];
                b.Ttl -= dt;
                if (b.Ttl <= 0f) _blasts.RemoveAt(i); else _blasts[i] = b;
            }
        }

        private static readonly Color32 MapGround = new Color32(24, 24, 30, 210);
        private static readonly Color32 MapWall = new Color32(120, 116, 105, 255);
        private static readonly Color32 MapBarricade = new Color32(170, 140, 80, 255);
        private static readonly Color32 MapBreach = new Color32(255, 110, 20, 255);
        private static readonly Color32 MapTurret = new Color32(70, 150, 230, 255);
        private static readonly Color32 MapVault = new Color32(50, 230, 130, 255);
        private static readonly Color32 MapHero = new Color32(255, 215, 60, 255);
        private static readonly Color32 MapSapper = new Color32(255, 150, 30, 255);
        private static readonly Color32 MapSpitter = new Color32(110, 240, 80, 255);

        /// <summary>Repaints the minimap 5x a second: terrain, structures, threat density, you.</summary>
        private void UpdateMinimap(float dt)
        {
            _minimapTimer -= dt;
            if (_minimapTimer > 0f) return;
            _minimapTimer = 0.2f;

            System.Array.Clear(_minimapAgents, 0, _minimapAgents.Length);
            for (int id = 0; id < _world.Count; id++)
            {
                if (!_world.IsAlive(id)) continue;
                Vec2 p = _world.PositionOf(id);
                var (ax, ay) = _map.WorldToCell(p);
                int idx = ay * GridW + ax;
                // Rare archetypes are flagged with big negative-free sentinels so they always win the pixel.
                switch (_world.ArchetypeOf(id))
                {
                    case Archetype.Sapper: _minimapAgents[idx] = 100000; break;
                    case Archetype.Spitter: if (_minimapAgents[idx] < 100000) _minimapAgents[idx] = 50000; break;
                    default: if (_minimapAgents[idx] < 50000) _minimapAgents[idx]++; break;
                }
            }

            for (int y = 0; y < GridH; y++)
            {
                for (int x = 0; x < GridW; x++)
                {
                    int i = y * GridW + x;
                    Color32 c = MapGround;

                    WallKind kind = _map.KindAt(x, y);
                    BreachStage stage = _map.StageAt(x, y);
                    if (kind == WallKind.Structure) c = MapTurret;
                    else if (kind != WallKind.None && stage != BreachStage.Collapsed)
                        c = stage != BreachStage.Intact ? MapBreach : kind == WallKind.Barricade ? MapBarricade : MapWall;

                    int agents = _minimapAgents[i];
                    if (agents >= 100000) c = MapSapper;
                    else if (agents >= 50000) c = MapSpitter;
                    else if (agents > 0)
                    {
                        byte heat = (byte)Mathf.Clamp(90 + agents * 45, 90, 255);
                        c = new Color32(heat, (byte)Mathf.Max(20, 70 - agents * 12), 40, 255);
                    }

                    if (x == GoalX && y == GoalY) c = MapVault;
                    // Unity textures are bottom-up; the sim's +Y is "north", so flip the row.
                    _minimapPixels[(GridH - 1 - y) * GridW + x] = c;
                }
            }

            var (hx, hy) = _map.WorldToCell(_hero.Position);
            _minimapPixels[(GridH - 1 - hy) * GridW + hx] = MapHero;
            if (_buildMode)
                _minimapPixels[(GridH - 1 - _build.CursorY) * GridW + _build.CursorX] = new Color32(255, 255, 255, 255);

            _minimap.SetPixels32(_minimapPixels);
            _minimap.Apply(false);
        }

        private void UpdateHeroVisual()
        {
            // The capsule is the fallback. Once a real body exists it takes over, but the barrel and
            // the airstrike marker still hang off the capsule transform, so it is hidden rather than
            // removed.
            var heroRenderer = _heroT.GetComponent<Renderer>();
            if (heroRenderer != null && heroRenderer.enabled == (_heroBody != null))
                heroRenderer.enabled = _heroBody == null;

            _heroT.position = ToWorld(_hero.Position, 0.9f);
            var facing = new Vector3(_hero.Facing.X, 0f, _hero.Facing.Y);
            if (facing.sqrMagnitude > 1e-6f) _heroT.rotation = Quaternion.LookRotation(facing, Vector3.up);
            _barrelT.gameObject.SetActive(!_hero.IsDown && !_buildMode);

            bool showMarker = !_hero.IsDown && !_buildMode && !_match.IsOver;
            _markerT.gameObject.SetActive(showMarker);
            if (showMarker)
            {
                Vec2 m = _hero.StrikeTarget;
                Vec2 ax = _hero.StrikeAxis;
                float scale = _hero.StrikeInbound ? 1f + 0.15f * Mathf.Sin(Time.time * 40f)
                            : _hero.AirstrikeReady ? 1f : 0.3f + 0.7f * _hero.AirstrikeReadyFraction;
                _markerT.position = ToWorld(m, 0.03f);
                _markerT.rotation = Quaternion.LookRotation(new Vector3(ax.X, 0f, ax.Y), Vector3.up);
                _markerT.localScale = new Vector3(_hero.StrikeLineWidth * scale, 0.02f, _hero.StrikeLineLength * scale);
            }

            float vaultHp = (float)_match.VaultHp / _match.VaultMaxHp;
            _vaultT.localScale = new Vector3(1.6f, 0.3f + 1.2f * vaultHp, 1.6f);

            _crateT.gameObject.SetActive(_pickups.CrateActive);
            if (_pickups.CrateActive)
            {
                _crateT.position = ToWorld(_pickups.CratePosition, 0.45f + 0.15f * Mathf.Sin(Time.time * 4f));
                _crateT.rotation = Quaternion.Euler(0f, Time.time * 90f, 0f);
            }
        }

        private void UpdateBuildVisual()
        {
            _cursorT.gameObject.SetActive(_buildMode);
            _routeOk.Clear();
            _routeBad.Clear();
            if (!_buildMode) return;

            bool snapped = _build.Item == BuildItem.RepairDrone && _build.DroneTarget.X >= 0;
            _cursorT.position = snapped
                ? new Vector3(_build.DroneTarget.X + 0.5f, 0.4f, _build.DroneTarget.Y + 0.5f)
                : new Vector3(_build.CursorX + 0.5f, 0.06f, _build.CursorY + 0.5f);
            Color c = _build.LastResult switch
            {
                PlacementResult.Ok => _build.CanAfford ? new Color(0.3f, 1f, 0.4f) : new Color(0.6f, 0.6f, 0.6f),
                PlacementResult.SealsSpawn => new Color(1f, 0.6f, 0.1f),
                _ => new Color(1f, 0.2f, 0.2f),
            };
            _cursorMaterial.color = c;

            // Route preview: one dotted line per spawn, following the what-if field.
            var dot = new Vector3(0.28f, 0.06f, 0.28f);
            foreach (var spawn in SpawnCells)
            {
                bool reaches = _build.TraceRoute(spawn, _routeScratch);
                var list = reaches ? _routeOk : _routeBad;
                for (int i = 0; i < _routeScratch.Count; i += 2)
                {
                    Vec2 p = _routeScratch[i];
                    list.Add(Matrix4x4.TRS(new Vector3(p.X, 0.1f, p.Y), Quaternion.identity, dot));
                }
                if (!reaches)
                {
                    // Dead end: a fat red marker at the last point so the player sees where they stall.
                    Vec2 e = _routeScratch[_routeScratch.Count - 1];
                    list.Add(Matrix4x4.TRS(new Vector3(e.X, 0.3f, e.Y), Quaternion.identity, new Vector3(0.9f, 0.5f, 0.9f)));
                }
            }
        }

        private void UpdateCamera(float dt)
        {
            if (_lineupMode) return;   // the lineup capture owns the camera

            Vector3 targetPos;
            Quaternion targetRot;

            if (_camMode == CameraMode.Chase)
            {
                Vector3 heroPos = ToWorld(_hero.Position, 0f);
                Vector3 fwd = CameraForward();
                float pr = _camPitch * Mathf.Deg2Rad;
                Vector3 pivot = heroPos + Vector3.up * ChaseLookHeight;
                Vector3 offset = -fwd * (ChaseDistance * Mathf.Cos(pr)) + Vector3.up * (ChaseDistance * Mathf.Sin(pr));
                targetPos = pivot + offset;
                if (targetPos.y < 0.6f) targetPos.y = 0.6f;
                targetRot = Quaternion.LookRotation(pivot - targetPos, Vector3.up);
            }
            else
            {
                Vector3 focus = _buildMode ? new Vector3(_build.CursorX + 0.5f, 0f, _build.CursorY + 0.5f) : ToWorld(_hero.Position, 0f);
                targetPos = focus + new Vector3(0f, 34f, -14f);
                targetRot = Quaternion.Euler(68f, 0f, 0f);
            }

            float k = 1f - Mathf.Exp(-12f * dt);
            _camera.transform.position = Vector3.Lerp(_camera.transform.position, targetPos, k);
            _camera.transform.rotation = Quaternion.Slerp(_camera.transform.rotation, targetRot, k);
        }

        private void DrawWorld()
        {
            if (_lineupMode) return;   // lineup capture: nothing but the models
            BakeWallsIfChanged();
            DrawInstancedList(_cubeMesh, _wallMaterial, _wallMatrices, _wallMatrices.Length);
            DrawInstancedList(_cubeMesh, _barricadeMaterial, _barricadeMatrices, _barricadeMatrices.Length);
            DrawInstancedList(_cubeMesh, _breachMaterial, _breachMatrices, _breachMatrices.Length);
            DrawAgents();

            if (_buildMode)
            {
                DrawInstancedBatched(_cubeMesh, _routeOkMaterial, _routeOk);
                DrawInstancedBatched(_cubeMesh, _routeBadMaterial, _routeBad);
            }

            foreach (var t in _tracers)
            {
                Vector3 d = t.B - t.A;
                float len = d.magnitude;
                if (len < 1e-3f) continue;
                var m = Matrix4x4.TRS(t.A + d * 0.5f, Quaternion.LookRotation(d / len, Vector3.up), new Vector3(0.07f, 0.07f, len));
                Graphics.DrawMesh(_cubeMesh, m, t.Turret ? _turretTracerMaterial : _tracerMaterial, 0);
            }
            foreach (var b in _blasts)
            {
                float life = b.Ttl / BlastLife;
                float r = b.Radius * (1.15f - 0.15f * life);
                float h = 0.05f + 2.5f * (1f - life);
                var m = Matrix4x4.TRS(b.Center + Vector3.up * (h * 0.5f), Quaternion.identity, new Vector3(r * 2f, h * 0.5f, r * 2f));
                Graphics.DrawMesh(_discMesh, m, _blastMaterial, 0);
            }
        }

        private void DrawAgents()
        {
            _sapperMatrices.Clear();
            _spitterMatrices.Clear();
            _fxMatrices.Clear();
            int inBuffer = 0;
            for (int id = 0; id < _world.Count; id++)
            {
                if (!_world.IsAlive(id)) continue;
                Vec2 p = _world.PositionOf(id);
                switch (_world.ArchetypeOf(id))
                {
                    case Archetype.Sapper:
                        _sapperMatrices.Add(Matrix4x4.TRS(new Vector3(p.X, 0.9f, p.Y), Quaternion.identity, new Vector3(0.6f, 0.95f, 0.6f)));
                        int cell = _world.SapperTargetCell(id);
                        if (cell >= 0)
                        {
                            float pulse = 1.1f + 0.2f * Mathf.Sin(Time.time * 8f);
                            _fxMatrices.Add(Matrix4x4.TRS(new Vector3(cell % GridW + 0.5f, 2.6f, cell / GridW + 0.5f), Quaternion.identity, new Vector3(pulse, 0.3f, pulse)));
                        }
                        continue;
                    case Archetype.Spitter:
                        _spitterMatrices.Add(Matrix4x4.TRS(new Vector3(p.X, 0.7f, p.Y), Quaternion.identity, new Vector3(0.7f, 0.5f, 0.7f)));
                        continue;
                }
                // Anyone wearing a real body this frame must not also be drawn as a capsule.
                if (_crowd != null && _crowd.Promoted.Contains(id)) continue;
                _instanceBuffer[inBuffer++] = Matrix4x4.TRS(new Vector3(p.X, 0.6f, p.Y), Quaternion.identity, new Vector3(0.45f, 0.6f, 0.45f));
                if (inBuffer == MaxInstancesPerDraw)
                {
                    Graphics.DrawMeshInstanced(_agentMesh, 0, _agentMaterial, _instanceBuffer, inBuffer);
                    inBuffer = 0;
                }
            }
            if (inBuffer > 0) Graphics.DrawMeshInstanced(_agentMesh, 0, _agentMaterial, _instanceBuffer, inBuffer);
            DrawInstancedBatched(_agentMesh, _sapperMaterial, _sapperMatrices);
            DrawInstancedBatched(_agentMesh, _spitterMaterial, _spitterMatrices);
            DrawInstancedBatched(_cubeMesh, _sapperTargetMaterial, _fxMatrices);

            // Repair drones hover over their cell; dim when the hero is too far to power them.
            _fxMatrices.Clear();
            foreach (var d in _build.Drones)
            {
                float bob = 1.6f + 0.2f * Mathf.Sin(Time.time * 6f + d.X);
                float size = d.HeroInRange ? 0.55f : 0.35f;
                _fxMatrices.Add(Matrix4x4.TRS(new Vector3(d.X + 0.5f, bob, d.Y + 0.5f), Quaternion.Euler(0f, Time.time * 180f, 0f), new Vector3(size, 0.2f, size)));
            }
            DrawInstancedBatched(_cubeMesh, _droneMaterial, _fxMatrices);
        }

        private void DrawInstancedList(Mesh mesh, Material material, Matrix4x4[] matrices, int count)
        {
            for (int start = 0; start < count; start += MaxInstancesPerDraw)
            {
                int n = Mathf.Min(MaxInstancesPerDraw, count - start);
                System.Array.Copy(matrices, start, _instanceBuffer, 0, n);
                Graphics.DrawMeshInstanced(mesh, 0, material, _instanceBuffer, n);
            }
        }

        private void DrawInstancedBatched(Mesh mesh, Material material, List<Matrix4x4> matrices)
        {
            for (int start = 0; start < matrices.Count; start += MaxInstancesPerDraw)
            {
                int n = Mathf.Min(MaxInstancesPerDraw, matrices.Count - start);
                matrices.CopyTo(start, _instanceBuffer, 0, n);
                Graphics.DrawMeshInstanced(mesh, 0, material, _instanceBuffer, n);
            }
        }

        // ------------------------------------------------------------------ pause

        private void ReadPauseInput()
        {
            var gamepad = Gamepad.current;
            var keyboard = Keyboard.current;

            bool toggle = (gamepad != null && gamepad.startButton.wasPressedThisFrame)
                       || (keyboard != null && keyboard.escapeKey.wasPressedThisFrame);

            if (!_pauseMenu.IsOpen)
            {
                if (toggle) { SetPaused(true); _sfx.Play(Sfx.MenuOpen, 0.7f, 0f); }
                return;
            }

            bool cancel = toggle || (gamepad != null && gamepad.buttonEast.wasPressedThisFrame);
            if (cancel) { Apply(_pauseMenu.Cancel()); _sfx.Play(Sfx.MenuConfirm, 0.7f, 0f); return; }

            bool toggleMap = (gamepad != null && gamepad.rightStickButton.wasPressedThisFrame) || (keyboard != null && keyboard.nKey.wasPressedThisFrame);
            if (toggleMap) _showMinimap = !_showMinimap;

            bool mute = (gamepad != null && gamepad.buttonNorth.wasPressedThisFrame) || (keyboard != null && keyboard.mKey.wasPressedThisFrame);
            if (mute) { _sfx.Muted = !_sfx.Muted; if (!_sfx.Muted) _sfx.Play(Sfx.MenuConfirm, 0.7f, 0f); }

            bool up = (gamepad != null && (gamepad.dpad.up.wasPressedThisFrame || gamepad.leftStick.up.wasPressedThisFrame))
                   || (keyboard != null && (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame));
            bool down = (gamepad != null && (gamepad.dpad.down.wasPressedThisFrame || gamepad.leftStick.down.wasPressedThisFrame))
                     || (keyboard != null && (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame));
            if (up) { _pauseMenu.MoveUp(); _sfx.Play(Sfx.MenuTick, 0.7f, 0f); }
            if (down) { _pauseMenu.MoveDown(); _sfx.Play(Sfx.MenuTick, 0.7f, 0f); }

            bool confirm = (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame)
                        || (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame));
            if (confirm) { _sfx.Play(Sfx.MenuConfirm, 0.7f, 0f); Apply(_pauseMenu.Confirm()); }
        }

        private void SetPaused(bool paused)
        {
            if (paused) _pauseMenu.Open(); else _pauseMenu.Close();
            Time.timeScale = paused ? 0f : _focus.TimeScale;
            AudioListener.pause = paused;
        }

        private void Apply(PauseMenuAction action)
        {
            switch (action)
            {
                case PauseMenuAction.Resume:
                    Time.timeScale = _focus.TimeScale;
                    AudioListener.pause = false;
                    break;
                case PauseMenuAction.Quit:
                    Time.timeScale = 1f;
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.ExitPlaymode();
#else
                    Application.Quit();
#endif
                    break;
            }
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }

        // ------------------------------------------------------------------ hud

        /// <summary>
        /// Virtual UI size. The HUD is authored against an 800-tall screen and scaled up, so a
        /// phone at 1080p landscape gets readable text instead of ant-sized labels. Everything in
        /// OnGUI must use _uiW/_uiH, never Screen.width/height, or it lands outside the scaled space.
        /// </summary>
        private float _uiW, _uiH;

        private void BeginScaledUi()
        {
            float scale = Mathf.Clamp(Screen.height / 800f, 1f, 3f);
            if (Application.isMobilePlatform) scale *= 1.5f;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            _uiW = Screen.width / scale;
            _uiH = Screen.height / scale;
        }

        private void OnGUI()
        {
            if (_hudHidden) return;
            OnGuiInner();
        }

        private void OnGuiInner()
        {
            BeginScaledUi();
            bool pad = Gamepad.current != null;
            _subStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter };

            GUI.Label(new Rect(12, 8, 1000, 28),
                $"fps {_smoothedFps:F0} | alive {_world.AliveCount} | breached {_world.ReachedCount} | kills {_world.TotalKills} (you {_hero.Kills}, turrets {TurretKills()})");

            string phase = _match.Phase switch
            {
                MatchPhase.Setup => _match.AwaitingStart
                    ? $"SET UP. Take your time.  ({(pad ? "View" : "Enter")}: start the first wave)"
                    : $"SETUP {_match.SetupTimeLeft:F0}s  ({(pad ? "View" : "Enter")}: start wave now)",
                MatchPhase.Wave => $"WAVE  {_match.SpawnedThisWave}/{_match.CurrentWave.Count} spawned",
                MatchPhase.Extraction =>
                    $"PACK UP {_match.ExtractTimeLeft:F0}s  (L: pull out now)  salvaged {_match.SalvagedCount}",
                MatchPhase.Extracted => "EXTRACTED",
                MatchPhase.Won => "TURF HELD",
                _ => "TURF LOST",
            };
            GUI.Label(new Rect(12, 32, 1000, 28),
                $"${_match.Bank.Cash}   Wave {_match.WaveNumber}/{_match.WaveCount}   {phase}   Vault {_match.VaultHp}/{_match.VaultMaxHp}");

            if (_match.CanDeclareLastWave)
            {
                GUI.Label(new Rect(12, 104, 1000, 24),
                    $"{(pad ? "D-pad down" : "L")}: call this your last wave here  ({_match.PrepSecondsRemaining:F0}s prep left for the next line)");
            }
            GUI.Label(new Rect(12, 152, 900, 24),
                $"{_focus.StatusLine()}    LV {_loadout.Skills.Level}  " +
                $"xp {_loadout.Skills.XpIntoLevel}/{Mathf.Max(1, _loadout.Skills.XpNeededForNext)}  " +
                $"scrip {_loadout.Inventory.Scrip}" +
                (_loadout.Skills.UnspentPoints > 0 ? $"   [{_loadout.Skills.UnspentPoints} skill points - K]" : "")
                + "    I: kit");

            if (_lootNoticeTimer > 0f && _lootNotice.Length > 0)
                GUI.Label(new Rect(12, 176, 1200, 24), _lootNotice);

            if (_declineNoticeTimer > 0f && _declineNotice.Length > 0)
            {
                GUI.Label(new Rect(12, 128, 1000, 24), _declineNotice);
            }

            DrawBar(new Rect(12, 60, 260, 18), _hero.HealthFraction, new Color(0.2f, 0.85f, 0.3f), new Color(0.6f, 0.1f, 0.1f), $"HP {_hero.Health:F0}");
            DrawBar(new Rect(12, 84, 260, 14), _hero.AirstrikeReadyFraction, new Color(1f, 0.6f, 0.15f), new Color(0.3f, 0.2f, 0.1f),
                _hero.StrikeInbound ? "STRIKE INBOUND" : _hero.AirstrikeReady ? "AIRSTRIKE READY: look, press Y" : "airstrike recharging");

            if (_buildMode)
            {
                var items = new System.Text.StringBuilder("[BUILD]  ");
                for (int i = 0; i < _build.Options.Count; i++)
                {
                    var opt = _build.Options[i];
                    bool sel = i == _build.Selected;
                    items.Append(sel ? "[ " : "  ").Append(opt.Name).Append(" $").Append(opt.Cost).Append(sel ? " ]" : "  ");
                }
                GUI.Label(new Rect(12, 108, 1200, 28), items.ToString());
                GUI.Label(new Rect(12, 130, 1200, 28),
                    pad ? "LS move  A place (hold to paint)  X sell  Y upgrade turret  hold LB for the wheel  Tab done"
                        : "arrows move  Space place  X sell  U upgrade turret  hold Shift for the wheel  Tab done");
                if (_build.Message.Length > 0)
                {
                    var style = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
                    style.normal.textColor = _build.LastResult == PlacementResult.SealsSpawn ? new Color(1f, 0.6f, 0.1f) : Color.white;
                    GUI.Label(new Rect(12, 152, 1200, 28), _build.Message, style);
                }
            }
            else
            {
                GUI.Label(new Rect(12, 108, 1200, 28),
                    (pad ? "LS move  RS look  RT fire  Y airstrike  LB build  View start wave  Menu pause"
                         : "WASD move  mouse look  LMB fire  RMB/Q airstrike  Tab build  Enter start wave  Esc pause")
                    + $"     gun: {_pickups.GunName}");
            }

            // Alerts: telegraphs for Sappers, breaches, Spitters, upgrades.
            if (_alerts.Count > 0)
            {
                var alertStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                alertStyle.normal.textColor = new Color(1f, 0.55f, 0.1f);
                for (int i = 0; i < _alerts.Count; i++)
                    GUI.Label(new Rect(0, 60 + i * 28, _uiW, 28), _alerts[i].Text, alertStyle);
            }

            if (_match.IsOver && !_pauseMenu.IsOpen)
            {
                bool won = _match.Phase == MatchPhase.Won;
                Overlay(won ? new Color(0f, 0.3f, 0.1f, 0.55f) : new Color(0.4f, 0f, 0f, 0.55f),
                        won ? "TURF HELD" : "TURF LOST",
                        $"{_match.WavesCleared}/{_match.WaveCount} waves   ${_match.Bank.TotalEarned} earned   {_world.TotalKills} kills\n{(pad ? "A" : "Enter")}: run it back");
            }
            else if (_hero.IsDown && !_pauseMenu.IsOpen)
            {
                Overlay(new Color(0.4f, 0f, 0f, 0.55f), "DOWN", pad ? "A: get back up" : "Enter: get back up");
            }

            if (_showMinimap && !_pauseMenu.IsOpen) DrawMinimap();

            // The wheel paints over the HUD but under the pause menu.
            // An offer owns the screen while it is up, so the kit panel stands down.
            if (_showInventory && !_showSkills && _cardOffer.Count == 0 && !_pauseMenu.IsOpen) DrawInventory();
            if (_match.Phase == MatchPhase.Extraction && !_pauseMenu.IsOpen) DrawTruck();
            if (_showSkills && !_pauseMenu.IsOpen) DrawSkillTree();
            if (_cardOffer.Count > 0 && !_pauseMenu.IsOpen) DrawCardOffer();
            if (_wheel.IsOpen && !_pauseMenu.IsOpen && !_match.IsOver) DrawBuildWheel();

            if (_pauseMenu.IsOpen) DrawPauseMenu();
        }

        private void DrawMinimap()
        {
            const float pad = 12f;
            float w = Mathf.Min(300f, _uiW * 0.22f);
            float h = w * GridH / GridW;
            var rect = new Rect(_uiW - w - pad, _uiH - h - pad, w, h);

            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(rect.x - 3f, rect.y - 3f, rect.width + 6f, rect.height + 6f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.DrawTexture(rect, _minimap);
            GUI.color = prev;

            GUI.Label(new Rect(rect.x, rect.y - 22f, rect.width, 20f),
                Gamepad.current != null ? "map (RS click: hide)" : "map (M: hide)");
        }

        private int TurretKills()
        {
            int k = 0;
            foreach (var t in _turrets.Turrets) k += t.Kills;
            return k;
        }

        private void Overlay(Color tint, string title, string sub)
        {
            _centerStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 40, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            var prev = GUI.color;
            GUI.color = tint;
            GUI.DrawTexture(new Rect(0, 0, _uiW, _uiH), Texture2D.whiteTexture);
            GUI.color = prev;
            GUI.Label(new Rect(0, _uiH * 0.38f, _uiW, 60), title, _centerStyle);
            GUI.Label(new Rect(0, _uiH * 0.38f + 64, _uiW, 70), sub, _subStyle);
        }

        /// <summary>
        /// The pack-up window opened. Build the list of what is standing and what it costs to haul.
        /// Weight and volume are separate limits on purpose: a sentry is heavy and compact, a
        /// barricade panel is light and enormous, so four light guns against one heavy one is a real
        /// choice rather than a number to maximise.
        /// </summary>
        private void OpenTruck()
        {
            _truck = new TruckLoad();
            _truckCursor = 0;
            _recoverable = new List<IHaulable>();

            for (int i = 0; i < _turrets.Turrets.Count; i++)
            {
                var t = _turrets.Turrets[i];
                if (!t.Alive) continue;
                string name = _turrets.Families[t.Family].Name;
                // Heavier guns are heavier to carry. Rough, tunable, and legible on the card.
                float weight = 60f + t.DamagePerShot * 2.4f;
                float volume = 0.55f + t.Range * 0.06f;
                _recoverable.Add(new SalvagedEmplacement(name + " T" + (t.Tier + 1),
                                                         new Haulage(weight, volume), t.Invested));
            }

            // Gear you are wearing rides with you regardless; only the pack competes for space.
            for (int i = 0; i < _loadout.Inventory.Pack.Count; i++)
                _recoverable.Add(new HauledItem(_loadout.Inventory.Pack[i]));
        }

        private void ReadTruckInput(Keyboard? kb, Gamepad? pad)
        {
            if (_truck == null || _recoverable.Count == 0) return;

            bool up = (kb != null && kb.upArrowKey.wasPressedThisFrame)
                      || (pad != null && pad.dpad.up.wasPressedThisFrame);
            bool down = (kb != null && kb.downArrowKey.wasPressedThisFrame)
                        || (pad != null && pad.dpad.down.wasPressedThisFrame);
            if (up) { _truckCursor = (_truckCursor - 1 + _recoverable.Count) % _recoverable.Count; _sfx.Play(Sfx.MenuTick, 0.6f, 0f); }
            if (down) { _truckCursor = (_truckCursor + 1) % _recoverable.Count; _sfx.Play(Sfx.MenuTick, 0.6f, 0f); }

            bool toggle = (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                          || (pad != null && pad.buttonSouth.wasPressedThisFrame);
            if (toggle)
            {
                var thing = _recoverable[_truckCursor];
                if (_truck.IsLoaded(thing))
                {
                    _truck.Unload(thing);
                    _sfx.Play(Sfx.MenuTick, 0.7f, 0f);
                }
                else if (!_truck.Fits(thing))
                {
                    // Check the bed BEFORE spending window time or paying out. Charging first meant
                    // a piece that did not fit still paid its full value and still cost four seconds,
                    // and since it never entered the truck the same piece could be sold again every
                    // four seconds until the window closed. Free money, found in review.
                    _declineNoticeTimer = 2f;
                    _declineNotice = _truck.Weight + thing.Haulage.Weight > _truck.MaxWeight
                        ? "too heavy for the bed"
                        : "no room left";
                    _sfx.Play(Sfx.MenuTick, 0.8f, 0f);
                }
                else
                {
                    // It fits. Now unbolting can charge the window and pay out.
                    var salvage = _match.TrySalvage(thing.RecoveredValue);
                    if (salvage != SalvageResult.Ok)
                    {
                        _declineNotice = "no time left to unbolt it";
                        _sfx.Play(Sfx.MenuTick, 0.8f, 0f);
                    }
                    else
                    {
                        var outcome = _truck.TryLoad(thing);
                        _declineNotice = outcome == LoadOutcome.Loaded
                            ? "loaded " + thing.HaulName
                            : "could not load it";
                        _sfx.Play(outcome == LoadOutcome.Loaded ? Sfx.MenuConfirm : Sfx.MenuTick, 0.8f, 0f);
                    }
                    _declineNoticeTimer = 2f;
                }
            }

            // Fill the bed by value density. A convenience, not a strategy: it optimises value per
            // unit carried, which is not always what the player wants at the next position.
            if ((kb != null && kb.fKey.wasPressedThisFrame)
                || (pad != null && pad.buttonWest.wasPressedThisFrame))
            {
                int before = _truck.Loaded.Count;
                var affordable = new List<IHaulable>();
                for (int i = 0; i < _recoverable.Count; i++)
                    if (!_truck.IsLoaded(_recoverable[i])) affordable.Add(_recoverable[i]);

                foreach (var thing in affordable)
                {
                    if (!_truck.Fits(thing)) continue;
                    if (_match.TrySalvage(thing.RecoveredValue) != SalvageResult.Ok) break;
                    _truck.TryLoad(thing);
                }

                _declineNotice = $"auto-loaded {_truck.Loaded.Count - before}";
                _declineNoticeTimer = 2f;
                _sfx.Play(Sfx.MenuConfirm, 0.8f, 0f);
            }

            if (kb != null && kb.lKey.wasPressedThisFrame) _match.PullOutNow();
        }

        private void DrawTruck()
        {
            if (_truck == null) return;
            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.74f);
            GUI.DrawTexture(new Rect(0f, 0f, _uiW, _uiH), Texture2D.whiteTexture);
            GUI.color = prev;

            _invStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };

            GUI.Label(new Rect(_uiW * 0.5f - 460f, 54f, 920f, 30f),
                      "LOAD THE TRUCK - " + _match.ExtractTimeLeft.ToString("F0") + "s before the next scan",
                      _subStyle);
            GUI.Label(new Rect(_uiW * 0.5f - 460f, 86f, 920f, 26f),
                      _truck.Weight.ToString("F0") + "/" + _truck.MaxWeight.ToString("F0") + " kg      "
                      + _truck.Volume.ToString("F1") + "/" + _truck.MaxVolume.ToString("F1") + " m3      "
                      + "value " + _truck.TotalValue, _subStyle);

            float y = 130f;
            for (int i = 0; i < _recoverable.Count && y < _uiH - 120f; i++)
            {
                var thing = _recoverable[i];
                bool on = _truck.IsLoaded(thing);
                bool sel = i == _truckCursor;
                GUI.color = sel ? new Color(1f, 0.86f, 0.38f, 1f)
                          : on ? new Color(0.6f, 0.95f, 0.65f, 0.95f)
                          : _truck.Fits(thing) ? new Color(0.86f, 0.9f, 0.94f, 0.92f)
                          : new Color(0.6f, 0.5f, 0.5f, 0.85f);
                GUI.Label(new Rect(_uiW * 0.5f - 460f, y, 920f, 24f),
                          (sel ? "> " : "  ") + (on ? "[X] " : "[ ] ")
                          + thing.HaulName.PadRight(26) + "  " + thing.Haulage
                          + "   worth " + thing.RecoveredValue, _invStyle);
                y += 24f;
            }
            GUI.color = prev;

            GUI.Label(new Rect(_uiW * 0.5f - 460f, y + 18f, 920f, 26f),
                      "up/down to move, A or Enter to load or unload, F auto-load, L to pull out now", _subStyle);
        }

        /// <summary>
        /// Spending permanent points. Deliberately a flat list rather than a drawn graph: the
        /// prerequisites already impose the shape, and a list navigates on a gamepad without any of
        /// the tree-cursor UX that is still unproven here.
        /// </summary>
        private void ReadSkillInput(Keyboard? kb, Gamepad? pad)
        {
            var all = SkillCatalogue.All;
            if (all.Count == 0) return;

            bool up = (kb != null && kb.upArrowKey.wasPressedThisFrame)
                      || (pad != null && pad.dpad.up.wasPressedThisFrame);
            bool down = (kb != null && kb.downArrowKey.wasPressedThisFrame)
                        || (pad != null && pad.dpad.down.wasPressedThisFrame);
            if (up) { _skillCursor = (_skillCursor - 1 + all.Count) % all.Count; _sfx.Play(Sfx.MenuTick, 0.6f, 0f); }
            if (down) { _skillCursor = (_skillCursor + 1) % all.Count; _sfx.Play(Sfx.MenuTick, 0.6f, 0f); }

            bool buy = (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                       || (pad != null && pad.buttonSouth.wasPressedThisFrame);
            if (!buy) return;

            var node = all[_skillCursor];
            var result = _loadout.SpendSkillPoint(node.Id);
            if (result == SpendResult.Ok)
            {
                ApplyLoadoutToWorld();
                _sfx.Play(Sfx.MenuConfirm, 0.9f, 0f);
                Alert($"{node.Name} — {node.Text}", 3f);
            }
            else
            {
                _sfx.Play(Sfx.MenuTick, 0.7f, 0f);
                _declineNotice = result switch
                {
                    SpendResult.NotEnoughPoints => "not enough skill points",
                    SpendResult.PrerequisiteMissing => $"needs {SkillCatalogue.Find(node.Requires!)?.Name}",
                    SpendResult.AtMaxRank => "already at max rank",
                    _ => "cannot buy that",
                };
                _declineNoticeTimer = 2.5f;
            }
        }

        private void DrawSkillTree()
        {
            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.8f);
            GUI.DrawTexture(new Rect(0f, 0f, _uiW, _uiH), Texture2D.whiteTexture);
            GUI.color = prev;

            _invStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };

            GUI.Label(new Rect(_uiW * 0.5f - 460f, 54f, 920f, 30f),
                      $"SKILLS     level {_loadout.Skills.Level}     {_loadout.Skills.UnspentPoints} points to spend     (K to close)",
                      _subStyle);

            var all = SkillCatalogue.All;
            float y = 100f;
            for (int i = 0; i < all.Count; i++)
            {
                var node = all[i];
                int rank = _loadout.Skills.RankOf(node.Id);
                bool sel = i == _skillCursor;
                bool affordable = _loadout.Skills.CanSpend(node.Id) == SpendResult.Ok;

                GUI.color = sel ? new Color(1f, 0.86f, 0.38f, 1f)
                          : affordable ? new Color(0.86f, 0.9f, 0.94f, 0.95f)
                          : new Color(0.55f, 0.56f, 0.6f, 0.85f);

                string lockNote = node.Requires != null && !_loadout.Skills.IsBought(node.Requires)
                    ? $"  (needs {SkillCatalogue.Find(node.Requires)?.Name})" : "";
                GUI.Label(new Rect(_uiW * 0.5f - 460f, y, 920f, 24f),
                          $"{(sel ? ">" : " ")} [{node.Path,-8}] {node.Name,-20} {rank}/{node.MaxRank}   {node.Cost}pt   {node.Text}{lockNote}",
                          _invStyle);
                y += 24f;
            }
            GUI.color = prev;

            GUI.Label(new Rect(_uiW * 0.5f - 460f, y + 16f, 920f, 26f),
                      "up/down to move, A or Enter to buy", _subStyle);
        }

        /// <summary>
        /// The pick-one-of-three. Deliberately a row of three cards and one button: a Bloons-style
        /// tree needs full gamepad tree navigation, and this lands the reward at the exact moment
        /// the player is weighing another wave.
        /// </summary>
        private void DrawCardOffer()
        {
            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.62f);
            GUI.DrawTexture(new Rect(0f, 0f, _uiW, _uiH), Texture2D.whiteTexture);
            GUI.color = prev;

            _cardStyle ??= new GUIStyle(GUI.skin.box)
            {
                fontSize = 19, alignment = TextAnchor.UpperCenter, wordWrap = true, padding = new RectOffset(14, 14, 16, 14),
            };

            GUI.Label(new Rect(_uiW * 0.5f - 400f, _uiH * 0.22f, 800f, 34f),
                      "WAVE DOWN — take one", _subStyle);

            const float w = 300f, h = 190f, gap = 28f;
            float total = _cardOffer.Count * w + (_cardOffer.Count - 1) * gap;
            float x0 = _uiW * 0.5f - total * 0.5f;
            float y = _uiH * 0.5f - h * 0.5f;

            for (int i = 0; i < _cardOffer.Count; i++)
            {
                var card = _cardOffer[i];
                bool sel = i == _cardCursor;
                GUI.color = sel ? new Color(1f, 0.86f, 0.38f, 0.98f) : new Color(0.82f, 0.85f, 0.9f, 0.9f);
                GUI.Box(new Rect(x0 + i * (w + gap), y, w, h),
                        $"{i + 1}. {card.Name}{System.Environment.NewLine}{System.Environment.NewLine}{card.Text}" +
                        $"{System.Environment.NewLine}{System.Environment.NewLine}[{card.Path}]",
                        _cardStyle);
            }
            GUI.color = prev;

            var dom = _loadout.Deck.DominantPath();
            string commit = dom.HasValue
                ? $"you are leaning {dom.Value} ({_loadout.Deck.CountFor(dom.Value)})"
                : "no path yet";
            GUI.Label(new Rect(_uiW * 0.5f - 400f, y + h + 22f, 800f, 28f),
                      $"{commit}   —   arrows / d-pad to choose, A or Enter to take, 1-3 direct", _subStyle);
        }

        /// <summary>What is worn and what is in the pack. Read-only for now; the sort happens between missions.</summary>
        private void DrawInventory()
        {
            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.72f);
            GUI.DrawTexture(new Rect(_uiW - 560f, 60f, 548f, _uiH - 120f), Texture2D.whiteTexture);
            GUI.color = prev;

            _invStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true, richText = false };

            float y = 74f;
            GUI.Label(new Rect(_uiW - 546f, y, 520f, 26f),
                      $"KIT     scrip {_loadout.Inventory.Scrip}     (I to close)", _invStyle);
            y += 30f;

            foreach (Slot slot in System.Enum.GetValues(typeof(Slot)))
            {
                var item = _loadout.Inventory.Equipped(slot);
                GUI.Label(new Rect(_uiW - 546f, y, 520f, 24f),
                          item == null ? $"{slot,-10} —" : $"{slot,-10} {item}", _invStyle);
                y += 24f;
            }

            y += 12f;
            GUI.Label(new Rect(_uiW - 546f, y, 520f, 24f),
                      $"PACK  {_loadout.Inventory.Pack.Count}/{_loadout.Inventory.PackCapacity}", _invStyle);
            y += 26f;
            for (int i = 0; i < _loadout.Inventory.Pack.Count && y < _uiH - 90f; i++)
            {
                var it = _loadout.Inventory.Pack[i];
                float delta = _loadout.Inventory.UpgradeDelta(it);
                GUI.Label(new Rect(_uiW - 546f, y, 520f, 24f),
                          $"{it}   {(delta >= 0f ? "+" : "")}{delta:F1}", _invStyle);
                y += 24f;
            }
        }

        private GUIStyle? _cardStyle;
        private GUIStyle? _invStyle;

        /// <summary>
        /// The radial build menu. Replaces the row of corner text the owner rightly called a legend
        /// rather than a menu. Drawn in the scaled UI space so it is the same size on every DPI.
        /// </summary>
        private void DrawBuildWheel()
        {
            int count = _build.Options.Count;
            if (count == 0) return;

            float cx = _uiW * 0.5f;
            float cy = _uiH * 0.5f;
            float radius = Mathf.Min(_uiW, _uiH) * 0.26f;
            const float itemW = 250f, itemH = 60f;

            var prev = GUI.color;

            // Dim the world so the wheel is unmistakably modal.
            GUI.color = new Color(0f, 0f, 0f, 0.45f);
            GUI.DrawTexture(new Rect(0f, 0f, _uiW, _uiH), Texture2D.whiteTexture);
            GUI.color = prev;

            _wheelStyle ??= new GUIStyle(GUI.skin.box)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
            };

            for (int i = 0; i < count; i++)
            {
                float angle = BuildWheel.AngleForIndex(i, count);
                float x = cx + Mathf.Sin(angle) * radius - itemW * 0.5f;
                float y = cy - Mathf.Cos(angle) * radius - itemH * 0.5f;

                var option = _build.Options[i];
                bool affordable = _match.Bank.CanAfford(option.Cost);
                bool selected = i == _wheel.Selected;

                GUI.color = selected
                    ? (affordable ? new Color(1f, 0.85f, 0.35f, 0.98f) : new Color(1f, 0.45f, 0.35f, 0.98f))
                    : (affordable ? new Color(0.85f, 0.88f, 0.92f, 0.85f) : new Color(0.55f, 0.55f, 0.58f, 0.8f));

                GUI.Box(new Rect(x, y, itemW, itemH),
                        option.Name + System.Environment.NewLine + "$" + option.Cost, _wheelStyle);
            }
            GUI.color = prev;

            string centre = (_wheel.Selected >= 0 && _wheel.Selected < count)
                ? _build.Options[_wheel.Selected].Name
                : "choose";
            GUI.Label(new Rect(cx - 200f, cy - 18f, 400f, 32f), centre, _subStyle);
            GUI.Label(new Rect(cx - 200f, cy + 14f, 400f, 28f),
                      _focus.IsActive ? "FOCUS" : "release to commit", _subStyle);
        }

        private GUIStyle? _wheelStyle;

        private static void DrawBar(Rect r, float fraction, Color fill, Color back, string label)
        {
            var prev = GUI.color;
            GUI.color = back;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = fill;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width * Mathf.Clamp01(fraction), r.height), Texture2D.whiteTexture);
            GUI.color = prev;
            GUI.Label(new Rect(r.x + 6, r.y - 2, r.width, r.height + 4), label);
        }

        private void DrawPauseMenu()
        {
            _menuTitleStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 36, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _menuItemStyle ??= new GUIStyle(GUI.skin.button) { fontSize = 26, alignment = TextAnchor.MiddleCenter };

            float w = _uiW, h = _uiH;
            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
            GUI.color = prev;

            const float itemW = 320f, itemH = 56f, gap = 14f;
            float top = h * 0.5f - (PauseMenuModel.Items.Length * (itemH + gap)) * 0.5f;

            GUI.Label(new Rect(0, top - 90f, w, 60f), "PAUSED", _menuTitleStyle);

            for (int i = 0; i < PauseMenuModel.Items.Length; i++)
            {
                bool selected = i == _pauseMenu.SelectedIndex;
                var rect = new Rect(w * 0.5f - itemW * 0.5f, top + i * (itemH + gap), itemW, itemH);
                GUI.backgroundColor = selected ? new Color(1f, 0.65f, 0.1f) : Color.white;
                string label = selected ? $"> {PauseMenuModel.Items[i]} <" : PauseMenuModel.Items[i];
                if (GUI.Button(rect, label, _menuItemStyle))
                    Apply(_pauseMenu.Select(i));
            }
            GUI.backgroundColor = Color.white;

            GUI.Label(new Rect(0, top + PauseMenuModel.Items.Length * (itemH + gap) + 10f, w, 30f),
                (Gamepad.current != null ? "stick / d-pad: choose   A: confirm   B or Menu: back   Y: sound "
                                         : "arrows: choose   Enter: confirm   Esc: back   M: sound ") + (_sfx.Muted ? "OFF" : "on"),
                new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
        }
    }
}
