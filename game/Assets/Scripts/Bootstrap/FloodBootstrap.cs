#nullable enable
using System.Collections.Generic;
using Cipher.Sim.Agents;
using Cipher.Sim.Core;
using Cipher.Sim.Grid;
using Cipher.Game.Hero;
using Cipher.Game.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cipher.Game
{
    /// <summary>
    /// Graybox composition root. The entire scene is built procedurally at startup —
    /// camera, light, arena, sim, hero — so ANY empty scene runs it and no binary scene
    /// assets need authoring. Sim runs at a fixed 30Hz tick (deterministic core); the
    /// hero moves per frame but every effect on the swarm goes through AgentWorld.
    ///
    /// Controls (Xbox): left stick move, right stick orbit + tilt camera (chase) / aim (tactical),
    /// RT fire, Y airstrike on the line marker where you are looking (6-24 cells), hold LB for
    /// the tactical overhead camera (look only — ADR-002: combat is chase-only), Menu pause,
    /// A restart when down. Keyboard/mouse: WASD, mouse look, LMB fire, RMB or Q airstrike,
    /// hold Tab tactical, Esc pause, Enter restart.
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
        private const int MaxInstancesPerDraw = 1023; // Graphics.DrawMeshInstanced hard limit

        // ---- sim ----
        private GridMap _map = null!;
        private FlowField _field = null!;
        private AgentWorld _world = null!;
        private float _tickAccumulator;
        private int _targetDensity;

        // ---- hero ----
        private readonly HeroConfig _heroCfg = new HeroConfig();
        private HeroModel _hero = null!;
        private static readonly Vec2 HeroSpawn = new Vec2(GridW - 6f, GridH / 2f);
        private Transform _heroT = null!;
        private Transform _barrelT = null!;
        private Transform _markerT = null!;
        private readonly List<StrikeImpact> _impactScratch = new List<StrikeImpact>(8);

        private struct Tracer { public Vector3 A, B; public float Ttl; }
        private struct Blast { public Vector3 Center; public float Radius; public float Ttl; }
        private readonly List<Tracer> _tracers = new List<Tracer>(64);
        private readonly List<Blast> _blasts = new List<Blast>(8);
        private const float TracerLife = 0.06f;
        private const float BlastLife = 0.45f;

        // ---- rendering ----
        private Mesh _agentMesh = null!;
        private Material _agentMaterial = null!;
        private Mesh _wallMesh = null!;
        private Material _wallMaterial = null!;
        private Mesh _cubeMesh = null!;
        private Mesh _discMesh = null!;
        private Material _tracerMaterial = null!;
        private Material _blastMaterial = null!;
        private Matrix4x4[] _instanceBuffer = null!;
        private Matrix4x4[] _wallMatrices = null!;

        // ---- camera ----
        private enum CameraMode { Chase, Tactical }
        private Camera _camera = null!;
        private CameraMode _camMode = CameraMode.Chase;
        private float _camYaw = -90f;  // degrees; forward = (sin, 0, cos): -90 looks down -X, toward the flood
        private float _camPitch = 22f; // degrees above horizontal; RS-Y tilts it (memo: -10..+55)
        private const float ChaseDistance = 9f, ChaseLookHeight = 1.2f;
        private const float PitchMin = -10f, PitchMax = 55f;
        private const float StickYawSpeed = 170f, StickPitchSpeed = 90f, MouseYawPerPixel = 0.15f, MousePitchPerPixel = 0.1f;

        // ---- ui ----
        private float _smoothedFps = 60f;
        private readonly PauseMenuModel _pauseMenu = new PauseMenuModel();
        private GUIStyle? _menuTitleStyle;
        private GUIStyle? _menuItemStyle;
        private GUIStyle? _centerStyle;

        private void Awake()
        {
            _targetDensity = Application.isMobilePlatform ? 400 : 1000;
            BuildSim();
            BuildSceneObjects();
            _hero = new HeroModel(_heroCfg, HeroSpawn);
            _hero.Aim(new Vec2(-1f, 0f));
        }

        // ------------------------------------------------------------------ setup

        private void BuildSim()
        {
            _map = new GridMap(GridW, GridH);

            // Serpentine graybox arena: three walls forcing an S-route left → right.
            for (int y = 0; y < GridH - 10; y++) _map.SetBlocked(16, y, true);
            for (int y = 10; y < GridH; y++) _map.SetBlocked(32, y, true);
            for (int y = 0; y < GridH - 10; y++) _map.SetBlocked(48, y, true);

            _field = new FlowField(_map);
            _field.Compute(GridW - 2, GridH / 2);

            _world = new AgentWorld(_map, _field, new SimConfig(), initialCapacity: 4096);
        }

        private void BuildSceneObjects()
        {
            // The default scene template ships its own MainCamera; ours must be the only
            // active one or Camera-based input mapping goes through the wrong view.
            foreach (var existing in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                existing.gameObject.SetActive(false);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            _camera = camGo.AddComponent<Camera>();
            _camera.backgroundColor = new Color(0.05f, 0.05f, 0.07f);
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.nearClipPlane = 0.2f;
            _camera.farClipPlane = 300f;

            var lightGo = new GameObject("Sun");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
            light.intensity = 1.1f;
            RenderSettings.ambientLight = new Color(0.35f, 0.35f, 0.4f);

            // Ground plane (sim XY maps to world XZ).
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = new Vector3(GridW / 2f, 0f, GridH / 2f);
            ground.transform.localScale = new Vector3(GridW / 10f, 1f, GridH / 10f);
            ground.GetComponent<Renderer>().material = MakeMaterial(new Color(0.16f, 0.16f, 0.18f), instanced: false);

            // Meshes for instanced / immediate drawing, harvested from temp primitives.
            _agentMesh = HarvestMesh(PrimitiveType.Capsule);
            _wallMesh = HarvestMesh(PrimitiveType.Cube);
            _cubeMesh = _wallMesh;
            _discMesh = HarvestMesh(PrimitiveType.Cylinder);
            _agentMaterial = MakeMaterial(new Color(0.75f, 0.15f, 0.12f), instanced: true);
            _wallMaterial = MakeMaterial(new Color(0.35f, 0.33f, 0.30f), instanced: true);
            _tracerMaterial = MakeMaterial(new Color(1f, 0.95f, 0.5f), instanced: false);
            _blastMaterial = MakeMaterial(new Color(1f, 0.5f, 0.1f), instanced: false);
            _instanceBuffer = new Matrix4x4[MaxInstancesPerDraw];

            BakeWallMatrices();

            // Hero: gold capsule with a barrel cube so facing reads at a glance.
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

            // Airstrike marker: flat orange bar showing the bomb line where Y will land.
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "StrikeMarker";
            Destroy(marker.GetComponent<Collider>());
            marker.GetComponent<Renderer>().material = MakeMaterial(new Color(1f, 0.55f, 0.1f), instanced: false);
            _markerT = marker.transform;
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

        private void BakeWallMatrices()
        {
            int count = 0;
            for (int y = 0; y < GridH; y++)
                for (int x = 0; x < GridW; x++)
                    if (_map.IsBlocked(x, y)) count++;

            _wallMatrices = new Matrix4x4[count];
            int i = 0;
            for (int y = 0; y < GridH; y++)
                for (int x = 0; x < GridW; x++)
                    if (_map.IsBlocked(x, y))
                        _wallMatrices[i++] = Matrix4x4.TRS(new Vector3(x + 0.5f, 1f, y + 0.5f), Quaternion.identity, new Vector3(1f, 2f, 1f));
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

            if (_hero.IsDown)
            {
                if (RestartPressed()) Restart();
            }
            else
            {
                ReadHeroInput(dt);
            }

            // Fixed-tick sim, decoupled from render rate.
            _tickAccumulator += dt;
            int safety = 0;
            while (_tickAccumulator >= TickDt && safety++ < 8)
            {
                _tickAccumulator -= TickDt;
                SpawnTrickle();
                _world.Step(TickDt);
                _hero.ApplyContact(_world, TickDt);
            }
            // Drop unpayable tick debt after a hitch so the loop never death-spirals.
            _tickAccumulator = Mathf.Min(_tickAccumulator, TickDt);

            UpdateEffects(dt);
            UpdateHeroVisual();
            UpdateCamera(dt);
            DrawWorld();
        }

        private void ReadHeroInput(float dt)
        {
            var pad = Gamepad.current;
            var kb = Keyboard.current;
            var mouse = Mouse.current;

            // Camera mode: hold LB / Tab for the tactical overhead view.
            bool tactical = (pad != null && pad.leftShoulder.isPressed) || (kb != null && kb.tabKey.isPressed);
            _camMode = tactical ? CameraMode.Tactical : CameraMode.Chase;

            // Raw stick / key vectors in "screen" space: x right, y up.
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

            Vector3 camFwd = CameraForward();
            Vector3 camRight = new Vector3(camFwd.z, 0f, -camFwd.x);

            if (_camMode == CameraMode.Chase)
            {
                // Right stick / mouse orbits and tilts; hero faces where the camera looks; movement is camera-relative.
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
                camFwd = CameraForward();
                camRight = new Vector3(camFwd.z, 0f, -camFwd.x);

                _hero.Aim(new Vec2(camFwd.x, camFwd.z));
                Vector3 worldMove = camFwd * move.y + camRight * move.x;
                _hero.Move(_map, new Vec2(worldMove.x, worldMove.z), dt);
            }
            else
            {
                // Twin-stick: world-relative movement, right stick aims; mouse aims at the ground point.
                _hero.Move(_map, new Vec2(move.x, move.y), dt);
                if (look.sqrMagnitude > 0.04f)
                {
                    _hero.Aim(new Vec2(look.x, look.y));
                }
                else if (pad == null && mouse != null)
                {
                    Ray ray = _camera.ScreenPointToRay(mouse.position.ReadValue());
                    if (Mathf.Abs(ray.direction.y) > 1e-4f)
                    {
                        float t = -ray.origin.y / ray.direction.y;
                        Vector3 hit = ray.origin + ray.direction * t;
                        _hero.Aim(new Vec2(hit.x - _hero.Position.X, hit.z - _hero.Position.Y));
                    }
                }
                else if (move.sqrMagnitude > 0.04f)
                {
                    _hero.Aim(new Vec2(move.x, move.y));
                }
            }

            _hero.Tick(dt);
            _hero.AimStrike(StrikeAimPoint());
            _impactScratch.Clear();
            _hero.TickStrike(_world, dt, _impactScratch);
            foreach (var imp in _impactScratch)
                _blasts.Add(new Blast { Center = ToWorld(imp.Center, 0.05f), Radius = imp.Radius, Ttl = BlastLife });

            // ADR-002: combat happens in chase. Tactical is for looking (and, next milestone, building).
            bool canFight = _camMode == CameraMode.Chase;

            bool fire = canFight && ((pad != null && pad.rightTrigger.isPressed) || (mouse != null && mouse.leftButton.isPressed));
            if (fire && _hero.TryFire(_world, Random.Range(-2.5f, 2.5f), out ShotResult shot))
            {
                _tracers.Add(new Tracer
                {
                    A = ToWorld(shot.Origin, 0.75f),
                    B = ToWorld(shot.End, shot.Hit ? 0.6f : 0.75f),
                    Ttl = TracerLife,
                });
            }

            bool strike = canFight && ((pad != null && pad.buttonNorth.wasPressedThisFrame)
                       || (mouse != null && mouse.rightButton.wasPressedThisFrame)
                       || (kb != null && kb.qKey.wasPressedThisFrame));
            if (strike) _hero.TryAirstrike();
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

        private void Restart()
        {
            _world = new AgentWorld(_map, _field, new SimConfig(), initialCapacity: 4096);
            _hero.Respawn(HeroSpawn);
            _hero.Aim(new Vec2(-1f, 0f));
            _camYaw = -90f;
            _camPitch = 22f;
            _tracers.Clear();
            _blasts.Clear();
            _tickAccumulator = 0f;
        }

        private void SpawnTrickle()
        {
            // Keep the flood flooding: top up toward target density from the left edge.
            int deficit = _targetDensity - _world.AliveCount;
            int burst = Mathf.Min(deficit, 12);
            int baseCount = _world.Count;
            for (int i = 0; i < burst; i++)
            {
                float y = 2f + ((baseCount + i) * 7 % (GridH - 4));
                _world.Spawn(new Vec2(1.2f, y + 0.3f), health: 10f);
            }
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
            _heroT.gameObject.SetActive(true);
            _barrelT.gameObject.SetActive(!_hero.IsDown);

            bool showMarker = !_hero.IsDown && _camMode == CameraMode.Chase;
            _markerT.gameObject.SetActive(showMarker);
            if (showMarker)
            {
                Vec2 m = _hero.StrikeTarget;
                Vec2 ax = _hero.StrikeAxis;
                // Full-size bar when ready; shrinks while charging; pulses while bombs are inbound.
                float scale = _hero.StrikeInbound ? 1f + 0.15f * Mathf.Sin(Time.time * 40f)
                            : _hero.AirstrikeReady ? 1f : 0.3f + 0.7f * _hero.AirstrikeReadyFraction;
                _markerT.position = ToWorld(m, 0.03f);
                _markerT.rotation = Quaternion.LookRotation(new Vector3(ax.X, 0f, ax.Y), Vector3.up);
                _markerT.localScale = new Vector3(_hero.StrikeLineWidth * scale, 0.02f, _hero.StrikeLineLength * scale);
            }
        }

        private void UpdateCamera(float dt)
        {
            Vector3 heroPos = ToWorld(_hero.Position, 0f);
            Vector3 targetPos;
            Quaternion targetRot;

            if (_camMode == CameraMode.Chase)
            {
                Vector3 fwd = CameraForward();
                float pr = _camPitch * Mathf.Deg2Rad;
                Vector3 pivot = heroPos + Vector3.up * ChaseLookHeight;
                Vector3 offset = -fwd * (ChaseDistance * Mathf.Cos(pr)) + Vector3.up * (ChaseDistance * Mathf.Sin(pr));
                targetPos = pivot + offset;
                if (targetPos.y < 0.6f) targetPos.y = 0.6f; // never dip under the ground plane
                targetRot = Quaternion.LookRotation(pivot - targetPos, Vector3.up);
            }
            else
            {
                targetPos = heroPos + new Vector3(0f, 40f, -16f);
                targetRot = Quaternion.Euler(68f, 0f, 0f);
            }

            float k = 1f - Mathf.Exp(-12f * dt);
            _camera.transform.position = Vector3.Lerp(_camera.transform.position, targetPos, k);
            _camera.transform.rotation = Quaternion.Slerp(_camera.transform.rotation, targetRot, k);
        }

        private void DrawWorld()
        {
            DrawInstanced(_wallMesh, _wallMaterial, _wallMatrices, _wallMatrices.Length);
            DrawAgents();

            foreach (var t in _tracers)
            {
                Vector3 d = t.B - t.A;
                float len = d.magnitude;
                if (len < 1e-3f) continue;
                var m = Matrix4x4.TRS(t.A + d * 0.5f, Quaternion.LookRotation(d / len, Vector3.up), new Vector3(0.07f, 0.07f, len));
                Graphics.DrawMesh(_cubeMesh, m, _tracerMaterial, 0);
            }
            foreach (var b in _blasts)
            {
                float life = b.Ttl / BlastLife;               // 1 → 0
                float r = b.Radius * (1.15f - 0.15f * life);  // slight bloom outward
                float h = 0.05f + 2.5f * (1f - life);          // column rises then vanishes
                var m = Matrix4x4.TRS(b.Center + Vector3.up * (h * 0.5f), Quaternion.identity, new Vector3(r * 2f, h * 0.5f, r * 2f));
                Graphics.DrawMesh(_discMesh, m, _blastMaterial, 0);
            }
        }

        private void DrawAgents()
        {
            int inBuffer = 0;
            for (int id = 0; id < _world.Count; id++)
            {
                if (!_world.IsAlive(id)) continue;
                Vec2 p = _world.PositionOf(id);
                _instanceBuffer[inBuffer++] = Matrix4x4.TRS(new Vector3(p.X, 0.6f, p.Y), Quaternion.identity, new Vector3(0.45f, 0.6f, 0.45f));

                if (inBuffer == MaxInstancesPerDraw)
                {
                    DrawInstanced(_agentMesh, _agentMaterial, _instanceBuffer, inBuffer);
                    inBuffer = 0;
                }
            }
            if (inBuffer > 0) DrawInstanced(_agentMesh, _agentMaterial, _instanceBuffer, inBuffer);
        }

        private static void DrawInstanced(Mesh mesh, Material material, Matrix4x4[] matrices, int count)
        {
            if (count == 0) return;
            Graphics.DrawMeshInstanced(mesh, 0, material, matrices, count);
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
                if (toggle) SetPaused(true);
                return;
            }

            bool cancel = toggle || (gamepad != null && gamepad.buttonEast.wasPressedThisFrame);
            if (cancel) { Apply(_pauseMenu.Cancel()); return; }

            bool up = (gamepad != null && (gamepad.dpad.up.wasPressedThisFrame || gamepad.leftStick.up.wasPressedThisFrame))
                   || (keyboard != null && (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame));
            bool down = (gamepad != null && (gamepad.dpad.down.wasPressedThisFrame || gamepad.leftStick.down.wasPressedThisFrame))
                     || (keyboard != null && (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame));
            if (up) _pauseMenu.MoveUp();
            if (down) _pauseMenu.MoveDown();

            bool confirm = (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame)
                        || (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame));
            if (confirm) Apply(_pauseMenu.Confirm());
        }

        private void SetPaused(bool paused)
        {
            if (paused) _pauseMenu.Open(); else _pauseMenu.Close();
            Time.timeScale = paused ? 0f : 1f;
        }

        private void Apply(PauseMenuAction action)
        {
            switch (action)
            {
                case PauseMenuAction.Resume:
                    Time.timeScale = 1f;
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

            GUI.Label(new Rect(12, 8, 900, 28),
                $"CIPHER graybox — fps {_smoothedFps:F0} | alive {_world.AliveCount} | breached {_world.ReachedCount} | swarm kills {_world.TotalKills} | your kills {_hero.Kills}");
            GUI.Label(new Rect(12, 32, 900, 28),
                pad ? "LS move  RS look  RT fire  Y airstrike where you look  hold LB tactical cam  Menu pause"
                    : "WASD move  mouse look  LMB fire  RMB/Q airstrike where you look  hold Tab tactical cam  Esc pause");

            // Health bar.
            DrawBar(new Rect(12, 60, 260, 18), _hero.HealthFraction, new Color(0.2f, 0.85f, 0.3f), new Color(0.6f, 0.1f, 0.1f), $"HP {_hero.Health:F0}");
            // Airstrike cooldown bar.
            DrawBar(new Rect(12, 84, 260, 14), _hero.AirstrikeReadyFraction, new Color(1f, 0.6f, 0.15f), new Color(0.3f, 0.2f, 0.1f),
                _hero.StrikeInbound ? "STRIKE INBOUND" : _hero.AirstrikeReady ? "AIRSTRIKE READY — look, press Y" : "airstrike recharging");

            GUI.Label(new Rect(12, 104, 500, 24), _camMode == CameraMode.Chase ? "cam: chase (combat)" : "cam: tactical — look only, weapons hold (build mode lives here next)");

            if (_hero.IsDown && !_pauseMenu.IsOpen)
            {
                _centerStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 40, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                var prev = GUI.color;
                GUI.color = new Color(0.4f, 0f, 0f, 0.55f);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = prev;
                GUI.Label(new Rect(0, Screen.height * 0.4f, Screen.width, 60), "DOWN", _centerStyle);
                GUI.Label(new Rect(0, Screen.height * 0.4f + 60, Screen.width, 40),
                    pad ? "A: run it back" : "Enter: run it back",
                    new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter });
            }

            if (_pauseMenu.IsOpen) DrawPauseMenu();
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
                Gamepad.current != null ? "stick / d-pad: choose   A: confirm   B or Menu: back"
                                        : "arrows: choose   Enter: confirm   Esc: back",
                new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
        }
    }
}
