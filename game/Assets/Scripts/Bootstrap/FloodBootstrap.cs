#nullable enable
using System.Collections.Generic;
using Cipher.Game.Audio;
using Cipher.Game.Build;
using Cipher.Game.Hero;
using Cipher.Game.Match;
using Cipher.Game.UI;
using Cipher.Sim.Agents;
using Cipher.Sim.Core;
using Cipher.Sim.Emplacements;
using Cipher.Sim.Grid;
using UnityEngine;
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
        private Matrix4x4[] _instanceBuffer = null!;
        private Matrix4x4[] _wallMatrices = System.Array.Empty<Matrix4x4>();
        private Matrix4x4[] _barricadeMatrices = System.Array.Empty<Matrix4x4>();
        private Matrix4x4[] _breachMatrices = System.Array.Empty<Matrix4x4>();
        private int _bakedMapVersion = -1;
        private readonly List<GameObject> _turretGos = new List<GameObject>(32);
        private Transform _vaultT = null!;
        private Transform _crateT = null!;
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
            _turrets = new TurretSystem(new TurretConfig());
            _match = new MatchState(WaveTable.Default, _eco);
            _build = new BuildModel(_map, _world, _turrets, _match, _eco, SpawnCells, GoalX, GoalY, GridW - 12, GridH / 2);
            _world.Structures = _turrets.AsStructureQuery();
            ulong seed = (ulong)System.DateTime.UtcNow.Ticks;
            _director = new SpawnDirector(new DirectorConfig(), seed);
            _pickups = new PickupSystem(_map, seed ^ 0xC1FE, minX: 34, maxX: GridW - 4);
            _matchSeconds = 0f;
            _alerts.Clear();
            _lastPhase = MatchPhase.Setup;
            _wasDown = false;
            _strikeWasInbound = false;
            GunTiers.Apply(_heroCfg, 0);

            _hero = new HeroModel(_heroCfg, HeroSpawn);
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

            var lightGo = new GameObject("Sun");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
            light.intensity = 1.1f;
            RenderSettings.ambientLight = new Color(0.35f, 0.35f, 0.4f);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = new Vector3(GridW / 2f, 0f, GridH / 2f);
            ground.transform.localScale = new Vector3(GridW / 10f, 1f, GridH / 10f);
            ground.GetComponent<Renderer>().material = MakeMaterial(new Color(0.16f, 0.16f, 0.18f), instanced: false);

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
            _sapperMaterial = MakeMaterial(new Color(1f, 0.45f, 0.05f), instanced: true);
            _spitterMaterial = MakeMaterial(new Color(0.35f, 0.9f, 0.25f), instanced: true);
            _droneMaterial = MakeMaterial(new Color(0.4f, 0.95f, 1f), instanced: true);
            _sapperTargetMaterial = MakeMaterial(new Color(1f, 0.3f, 0.05f), instanced: true);
            _instanceBuffer = new Matrix4x4[MaxInstancesPerDraw];

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
            marker.GetComponent<Renderer>().material = MakeMaterial(new Color(1f, 0.55f, 0.1f), instanced: false);
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
            vault.GetComponent<Renderer>().material = MakeMaterial(new Color(0.2f, 0.9f, 0.5f), instanced: false);
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

        private static Material MakeMaterial(Color color, bool instanced)
        {
            var shader = Shader.Find("Standard");
            return new Material(shader) { color = color, enableInstancing = instanced };
        }

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
                _turretGos[i].transform.position = new Vector3(t.X + 0.5f, 0.7f, t.Y + 0.5f);
                _turretGos[i].transform.localScale = new Vector3(0.9f + 0.15f * t.Tier, 0.4f + 0.6f * hp, 0.9f + 0.15f * t.Tier);
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
                    case MatchPhase.Wave: _sfx.Play(Sfx.WaveHorn, 0.9f, 0.02f); break;
                    case MatchPhase.Setup: _sfx.Play(Sfx.WaveClear, 0.9f, 0.01f); break;
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
                        if (a == Archetype.Runner) _world.Spawn(pos, health: 10f);
                        else _world.SpawnArchetype(pos, a);
                    }
                }
            }

            _world.Step(TickDt);

            _shotScratch.Clear();
            _turrets.Step(_world, TickDt, _shotScratch);
            foreach (var s in _shotScratch)
            {
                _tracers.Add(new Tracer { A = ToWorld(s.From, 1.1f), B = ToWorld(s.To, 0.6f), Ttl = TracerLife * 0.7f, Turret = true });
                _sfx.PlayAt(Sfx.TurretShot, ToWorld(s.From, 1f), 0.6f, 0.1f, minInterval: 0.045f);
            }

            if (_hero.ApplyContact(_world, TickDt) > 0f && _hurtCooldown <= 0f)
            {
                _sfx.Play(Sfx.Hurt, 0.8f, 0.1f);
                _hurtCooldown = 0.35f;
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
            _match.ReportKills(kills);
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
            bool toggle = (pad != null && pad.leftShoulder.wasPressedThisFrame) || (kb != null && kb.tabKey.wasPressedThisFrame);
            if (toggle) { SetBuildMode(!_buildMode); _sfx.Play(_buildMode ? Sfx.MenuOpen : Sfx.MenuConfirm, 0.7f, 0f); }

            bool startWave = (pad != null && pad.selectButton.wasPressedThisFrame) || (kb != null && kb.enterKey.wasPressedThisFrame);
            if (startWave && _match.Phase == MatchPhase.Setup) _match.StartWaveNow();
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
                    _cursorRepeatTimer = _cursorHeldTime > 0.6f ? 1f / 16f : 1f / 8f;
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

        private void UpdateHeroVisual()
        {
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

            _cursorT.position = new Vector3(_build.CursorX + 0.5f, 0.06f, _build.CursorY + 0.5f);
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
            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;
        }

        private void Apply(PauseMenuAction action)
        {
            switch (action)
            {
                case PauseMenuAction.Resume:
                    Time.timeScale = 1f;
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

        private void OnGUI()
        {
            bool pad = Gamepad.current != null;
            _subStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter };

            GUI.Label(new Rect(12, 8, 1000, 28),
                $"fps {_smoothedFps:F0} | alive {_world.AliveCount} | breached {_world.ReachedCount} | kills {_world.TotalKills} (you {_hero.Kills}, turrets {TurretKills()})");

            string phase = _match.Phase switch
            {
                MatchPhase.Setup => $"SETUP {_match.SetupTimeLeft:F0}s  ({(pad ? "View" : "Enter")}: start wave now)",
                MatchPhase.Wave => $"WAVE  {_match.SpawnedThisWave}/{_match.CurrentWave.Count} spawned",
                MatchPhase.Won => "TURF HELD",
                _ => "TURF LOST",
            };
            GUI.Label(new Rect(12, 32, 1000, 28),
                $"${_match.Bank.Cash}   Wave {_match.WaveNumber}/{_match.WaveCount}   {phase}   Vault {_match.VaultHp}/{_match.VaultMaxHp}");

            DrawBar(new Rect(12, 60, 260, 18), _hero.HealthFraction, new Color(0.2f, 0.85f, 0.3f), new Color(0.6f, 0.1f, 0.1f), $"HP {_hero.Health:F0}");
            DrawBar(new Rect(12, 84, 260, 14), _hero.AirstrikeReadyFraction, new Color(1f, 0.6f, 0.15f), new Color(0.3f, 0.2f, 0.1f),
                _hero.StrikeInbound ? "STRIKE INBOUND" : _hero.AirstrikeReady ? "AIRSTRIKE READY: look, press Y" : "airstrike recharging");

            if (_buildMode)
            {
                var items = new System.Text.StringBuilder("[BUILD]  ");
                foreach (var it in BuildModel.Items)
                {
                    bool sel = it == _build.Item;
                    items.Append(sel ? "[ " : "  ").Append(BuildModel.NameOf(it)).Append(" $").Append(_build.CostOf(it)).Append(sel ? " ]" : "  ");
                }
                GUI.Label(new Rect(12, 108, 1200, 28), items.ToString());
                GUI.Label(new Rect(12, 130, 1200, 28),
                    pad ? "LS move  A place (hold to paint)  X sell  Y upgrade turret  RB / d-pad L-R switch item  B/LB done"
                        : "arrows move  Space place  X sell  U upgrade turret  Q switch item  Tab done");
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
                    GUI.Label(new Rect(0, 60 + i * 28, Screen.width, 28), _alerts[i].Text, alertStyle);
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

            if (_pauseMenu.IsOpen) DrawPauseMenu();
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
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = prev;
            GUI.Label(new Rect(0, Screen.height * 0.38f, Screen.width, 60), title, _centerStyle);
            GUI.Label(new Rect(0, Screen.height * 0.38f + 64, Screen.width, 70), sub, _subStyle);
        }

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

            float w = Screen.width, h = Screen.height;
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
