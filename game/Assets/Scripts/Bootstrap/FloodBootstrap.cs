using Cipher.Sim.Agents;
using Cipher.Sim.Core;
using Cipher.Sim.Grid;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cipher.Game
{
    /// <summary>
    /// Milestone 1 "The Flood": the entire graybox scene is built procedurally at
    /// startup — camera, light, arena, sim — so ANY empty scene runs it and no binary
    /// scene assets need authoring. Sim runs at a fixed 30Hz tick (deterministic core),
    /// rendering interpolates per frame via instanced draws.
    ///
    /// Input: left stick / mouse moves the strike cursor, A (button south) / left click
    /// calls in an airstrike. This is a placeholder loop to validate feel + density.
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

        private GridMap _map = null!;
        private FlowField _field = null!;
        private AgentWorld _world = null!;

        private Mesh _agentMesh = null!;
        private Material _agentMaterial = null!;
        private Mesh _wallMesh = null!;
        private Material _wallMaterial = null!;
        private Matrix4x4[] _instanceBuffer = null!;
        private Matrix4x4[] _wallMatrices = null!;

        private Transform _cursor = null!;
        private Vector2 _cursorPos;
        private float _strikeCooldown;
        private float _tickAccumulator;
        private int _targetDensity;
        private float _smoothedFps = 60f;

        private void Awake()
        {
            _targetDensity = Application.isMobilePlatform ? 400 : 1000;

            BuildSim();
            BuildSceneObjects();
        }

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
            // Camera: high tilted view covering the arena.
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.transform.position = new Vector3(GridW / 2f, 52f, -8f);
            cam.transform.rotation = Quaternion.Euler(62f, 0f, 0f);
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.07f);
            cam.clearFlags = CameraClearFlags.SolidColor;

            var lightGo = new GameObject("Sun");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
            light.intensity = 1.1f;

            // Ground plane (sim XY maps to world XZ).
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = new Vector3(GridW / 2f, 0f, GridH / 2f);
            ground.transform.localScale = new Vector3(GridW / 10f, 1f, GridH / 10f);
            ground.GetComponent<Renderer>().material = MakeMaterial(new Color(0.16f, 0.16f, 0.18f), instanced: false);

            // Meshes for instanced drawing, harvested from temp primitives.
            _agentMesh = HarvestMesh(PrimitiveType.Capsule);
            _wallMesh = HarvestMesh(PrimitiveType.Cube);
            _agentMaterial = MakeMaterial(new Color(0.75f, 0.15f, 0.12f), instanced: true);
            _wallMaterial = MakeMaterial(new Color(0.35f, 0.33f, 0.30f), instanced: true);
            _instanceBuffer = new Matrix4x4[MaxInstancesPerDraw];

            BakeWallMatrices();

            // Strike cursor.
            var cursorGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cursorGo.name = "StrikeCursor";
            Destroy(cursorGo.GetComponent<Collider>());
            cursorGo.transform.localScale = Vector3.one * 1.5f;
            cursorGo.GetComponent<Renderer>().material = MakeMaterial(new Color(1f, 0.65f, 0.1f), instanced: false);
            _cursor = cursorGo.transform;
            _cursorPos = new Vector2(GridW / 2f, GridH / 2f);
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
            var material = new Material(shader) { color = color, enableInstancing = instanced };
            return material;
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
            {
                for (int x = 0; x < GridW; x++)
                {
                    if (!_map.IsBlocked(x, y)) continue;
                    _wallMatrices[i++] = Matrix4x4.TRS(
                        new Vector3(x + 0.5f, 1f, y + 0.5f),
                        Quaternion.identity,
                        new Vector3(1f, 2f, 1f));
                }
            }
        }

        private void Update()
        {
            _smoothedFps = Mathf.Lerp(_smoothedFps, 1f / Mathf.Max(Time.unscaledDeltaTime, 1e-5f), 0.05f);

            ReadInput();

            // Fixed-tick sim, decoupled from render rate.
            _tickAccumulator += Time.deltaTime;
            int safety = 0;
            while (_tickAccumulator >= TickDt && safety++ < 8)
            {
                _tickAccumulator -= TickDt;
                SpawnTrickle();
                _world.Step(TickDt);
            }

            DrawInstanced(_wallMesh, _wallMaterial, _wallMatrices, _wallMatrices.Length);
            DrawAgents();
        }

        private void ReadInput()
        {
            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                Vector2 stick = gamepad.leftStick.ReadValue();
                _cursorPos += stick * (25f * Time.deltaTime);
            }
            else if (Mouse.current != null)
            {
                var cam = Camera.main;
                if (cam != null)
                {
                    Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
                    if (Mathf.Abs(ray.direction.y) > 1e-4f)
                    {
                        float t = -ray.origin.y / ray.direction.y;
                        Vector3 hit = ray.origin + ray.direction * t;
                        _cursorPos = new Vector2(hit.x, hit.z);
                    }
                }
            }

            _cursorPos.x = Mathf.Clamp(_cursorPos.x, 0f, GridW);
            _cursorPos.y = Mathf.Clamp(_cursorPos.y, 0f, GridH);
            _cursor.position = new Vector3(_cursorPos.x, 0.75f, _cursorPos.y);

            _strikeCooldown -= Time.deltaTime;
            bool firePressed = (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame)
                            || (gamepad == null && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
            if (firePressed && _strikeCooldown <= 0f)
            {
                _strikeCooldown = 0.35f;
                _world.ApplyRadialDamage(new Vec2(_cursorPos.x, _cursorPos.y), radius: 3.5f, damage: 50f);
            }
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

        private void DrawAgents()
        {
            int inBuffer = 0;
            for (int id = 0; id < _world.Count; id++)
            {
                if (!_world.IsAlive(id)) continue;
                Vec2 p = _world.PositionOf(id);
                _instanceBuffer[inBuffer++] = Matrix4x4.TRS(
                    new Vector3(p.X, 0.6f, p.Y),
                    Quaternion.identity,
                    new Vector3(0.45f, 0.6f, 0.45f));

                if (inBuffer == MaxInstancesPerDraw)
                {
                    DrawInstanced(_agentMesh, _agentMaterial, _instanceBuffer, inBuffer);
                    inBuffer = 0;
                }
            }

            if (inBuffer > 0)
                DrawInstanced(_agentMesh, _agentMaterial, _instanceBuffer, inBuffer);
        }

        private static void DrawInstanced(Mesh mesh, Material material, Matrix4x4[] matrices, int count)
        {
            if (count == 0) return;
            Graphics.DrawMeshInstanced(mesh, 0, material, matrices, count);
        }

        private void OnGUI()
        {
            GUI.Label(new Rect(12, 8, 640, 28),
                $"CIPHER flood graybox — fps {_smoothedFps:F0} | alive {_world.AliveCount} | breached {_world.ReachedCount} | kills {_world.TotalKills}");
            GUI.Label(new Rect(12, 32, 640, 28),
                Gamepad.current != null
                    ? "Gamepad: left stick = cursor, A = airstrike"
                    : "No gamepad — mouse: move = cursor, left click = airstrike");
        }
    }
}
