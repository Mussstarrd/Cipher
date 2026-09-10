#nullable enable
using System;
using Cipher.Sim.Core;

namespace Cipher.Sim.Grid
{
    /// <summary>What occupies a cell. None is open floor.</summary>
    public enum WallKind : byte
    {
        None = 0,
        /// <summary>Indestructible map boundary / bedrock. Never breachable, never buildable.</summary>
        Rock = 1,
        /// <summary>Map wall: breachable by saboteurs, not sellable.</summary>
        Wall = 2,
        /// <summary>Player barricade: breachable, sellable, repairable.</summary>
        Barricade = 3,
        /// <summary>Player emplacement (turret). Blocks pathing like a barricade.</summary>
        Structure = 4,
    }

    /// <summary>Breach progression for a wall cell. Cracked/Broken are passable but gated; Collapsed is plain floor.</summary>
    public enum BreachStage : byte
    {
        Intact = 0,
        /// <summary>Hole admits ~1 agent/s.</summary>
        Cracked = 1,
        /// <summary>Hole admits ~3 agents/s.</summary>
        Broken = 2,
        /// <summary>Wall is gone; ordinary open cell.</summary>
        Collapsed = 3,
    }

    /// <summary>
    /// The tactical grid: traversal costs, walls with hit points, and breach state.
    /// World units: one cell = 1.0 x 1.0, cell (x, y) has its center at (x + 0.5, y + 0.5).
    /// Structure-of-arrays; every mutation bumps <see cref="Version"/> so flow fields know
    /// they are stale (docs/design/sim-dynamic-walls-plan.md §1).
    /// </summary>
    public sealed class GridMap
    {
        public const byte MinCost = 1;
        public const ushort DefaultWallHp = 200;

        /// <summary>Traversal cost while a hole is at each stage (Intact is blocked, Collapsed is MinCost).</summary>
        public const byte CrackedCost = 12;
        public const byte BrokenCost = 4;

        /// <summary>Gate admission rates (agents per second) per breach stage.</summary>
        public const float CrackedRatePerSecond = 1f;
        public const float BrokenRatePerSecond = 3f;
        private const int TokensPerAgent = 1000; // milli-tokens; integer so throttling is bit-exact

        public int Width { get; }
        public int Height { get; }

        private readonly byte[] _cost;      // >= MinCost for passable cells
        private readonly byte[] _kind;      // WallKind
        private readonly ushort[] _hp;      // 0 when kind == None
        private readonly byte[] _stage;     // BreachStage
        private readonly int[] _gateTokens; // only meaningful while stage is Cracked/Broken

        /// <summary>Bumped on every pathing-relevant mutation so dependents (flow fields) can detect staleness.</summary>
        public int Version { get; private set; }

        /// <summary>Number of cells currently in a gated stage; the hot loop skips gate logic when zero.</summary>
        public int GateCount { get; private set; }

        public GridMap(int width, int height)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), "Grid dimensions must be positive.");

            Width = width;
            Height = height;
            int n = width * height;
            _cost = new byte[n];
            _kind = new byte[n];
            _hp = new ushort[n];
            _stage = new byte[n];
            _gateTokens = new int[n];
            for (int i = 0; i < n; i++) _cost[i] = MinCost;
        }

        /// <summary>Linear index for a cell. Out-of-range coordinates throw — silent linearization would alias to a different valid cell and corrupt it.</summary>
        public int CellIndex(int x, int y)
        {
            if (!InBounds(x, y))
                throw new ArgumentOutOfRangeException(nameof(x), $"Cell ({x},{y}) is outside the {Width}x{Height} grid.");
            return y * Width + x;
        }

        public bool InBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

        // ---------------------------------------------------------------- queries

        /// <summary>Impassable: any wall kind that has not been breached at all.</summary>
        public bool IsBlocked(int x, int y)
        {
            int i = CellIndex(x, y);
            return _kind[i] != (byte)WallKind.None && _stage[i] == (byte)BreachStage.Intact;
        }

        /// <summary>Traversal cost used by the flow field; already reflects breach stage.</summary>
        public byte CostAt(int x, int y) => _cost[CellIndex(x, y)];

        public WallKind KindAt(int x, int y) => (WallKind)_kind[CellIndex(x, y)];
        public ushort HpAt(int x, int y) => _hp[CellIndex(x, y)];
        public BreachStage StageAt(int x, int y) => (BreachStage)_stage[CellIndex(x, y)];

        /// <summary>True for a Cracked/Broken hole: passable, but admission is throttled.</summary>
        public bool IsGate(int x, int y)
        {
            int i = CellIndex(x, y);
            return _stage[i] == (byte)BreachStage.Cracked || _stage[i] == (byte)BreachStage.Broken;
        }

        public bool IsBuildable(int x, int y)
        {
            int i = CellIndex(x, y);
            return _kind[i] == (byte)WallKind.None;
        }

        // ---------------------------------------------------------------- legacy shape API

        /// <summary>Legacy toggle: blocked = an intact map Wall; unblocked = open floor. Prefer the typed setters.</summary>
        public void SetBlocked(int x, int y, bool blocked)
        {
            if (blocked) SetWall(x, y, WallKind.Wall, DefaultWallHp);
            else Clear(x, y);
        }

        public void SetCost(int x, int y, byte cost)
        {
            if (cost < MinCost)
                throw new ArgumentOutOfRangeException(nameof(cost), $"Cost must be >= {MinCost}; use SetBlocked for impassable cells.");
            int i = CellIndex(x, y);
            if (_cost[i] == cost) return;
            _cost[i] = cost;
            Version++;
        }

        // ---------------------------------------------------------------- mutation

        /// <summary>Places an intact wall of the given kind (map authoring and player building).</summary>
        public void SetWall(int x, int y, WallKind kind, ushort hp)
        {
            if (kind == WallKind.None) { Clear(x, y); return; }
            int i = CellIndex(x, y);
            if (_kind[i] == (byte)kind && _stage[i] == (byte)BreachStage.Intact && _hp[i] == hp) return;
            LeaveGateIfAny(i);
            _kind[i] = (byte)kind;
            _hp[i] = hp;
            _stage[i] = (byte)BreachStage.Intact;
            _cost[i] = MinCost;
            Version++;
        }

        /// <summary>Removes whatever is in the cell (sell, collapse cleanup). Cost resets to MinCost.</summary>
        public void Clear(int x, int y)
        {
            int i = CellIndex(x, y);
            if (_kind[i] == (byte)WallKind.None && _stage[i] == (byte)BreachStage.Intact && _cost[i] == MinCost) return;
            LeaveGateIfAny(i);
            _kind[i] = (byte)WallKind.None;
            _hp[i] = 0;
            _stage[i] = (byte)BreachStage.Intact;
            _cost[i] = MinCost;
            Version++;
        }

        /// <summary>
        /// Applies damage to a wall. Returns true when the wall's hit points reach zero (the caller
        /// decides whether that means a breach stage or a collapse). Rock ignores damage.
        /// </summary>
        public bool Damage(int x, int y, int amount)
        {
            int i = CellIndex(x, y);
            if (_kind[i] == (byte)WallKind.None || _kind[i] == (byte)WallKind.Rock || amount <= 0) return false;
            if (_hp[i] == 0) return true;
            _hp[i] = (ushort)Math.Max(0, _hp[i] - amount);
            return _hp[i] == 0;
        }

        /// <summary>
        /// Advances a breachable wall one stage: Intact → Cracked → Broken → Collapsed.
        /// Returns the new stage. Rock and open cells are unaffected.
        /// </summary>
        public BreachStage Breach(int x, int y)
        {
            int i = CellIndex(x, y);
            if (_kind[i] == (byte)WallKind.None || _kind[i] == (byte)WallKind.Rock) return (BreachStage)_stage[i];
            if (_stage[i] == (byte)BreachStage.Collapsed) return BreachStage.Collapsed;

            var next = (BreachStage)(_stage[i] + 1);
            ApplyStage(i, next);
            Version++;
            return next;
        }

        /// <summary>Restores a breached wall to Intact at full hit points. No-op on open cells.</summary>
        public void Repair(int x, int y, ushort hp = DefaultWallHp)
        {
            int i = CellIndex(x, y);
            if (_kind[i] == (byte)WallKind.None) return;
            if (_stage[i] == (byte)BreachStage.Intact && _hp[i] == hp) return;
            ApplyStage(i, BreachStage.Intact);
            _hp[i] = hp;
            Version++;
        }

        /// <summary>Repairs one stage toward Intact (Collapsed → Broken → Cracked → Intact). Returns the new stage.</summary>
        public BreachStage RepairStage(int x, int y, ushort hp = DefaultWallHp)
        {
            int i = CellIndex(x, y);
            if (_kind[i] == (byte)WallKind.None || _stage[i] == (byte)BreachStage.Intact) return (BreachStage)_stage[i];
            var next = (BreachStage)(_stage[i] - 1);
            ApplyStage(i, next);
            if (next == BreachStage.Intact) _hp[i] = hp;
            Version++;
            return next;
        }

        private void ApplyStage(int i, BreachStage stage)
        {
            LeaveGateIfAny(i);
            _stage[i] = (byte)stage;
            switch (stage)
            {
                case BreachStage.Intact:
                    _cost[i] = MinCost;
                    break;
                case BreachStage.Cracked:
                    _cost[i] = CrackedCost;
                    _gateTokens[i] = 0;
                    GateCount++;
                    break;
                case BreachStage.Broken:
                    _cost[i] = BrokenCost;
                    _gateTokens[i] = 0;
                    GateCount++;
                    break;
                case BreachStage.Collapsed:
                    _cost[i] = MinCost;
                    break;
            }
        }

        private void LeaveGateIfAny(int i)
        {
            if (_stage[i] == (byte)BreachStage.Cracked || _stage[i] == (byte)BreachStage.Broken)
            {
                GateCount--;
                _gateTokens[i] = 0;
            }
        }

        // ---------------------------------------------------------------- gates

        /// <summary>Refills every gate's token bucket for one tick. Integer maths: bit-exact across runs.</summary>
        public void RefillGates(float dt)
        {
            if (GateCount == 0 || dt <= 0f) return;
            int crackedAdd = (int)(CrackedRatePerSecond * TokensPerAgent * dt);
            int brokenAdd = (int)(BrokenRatePerSecond * TokensPerAgent * dt);
            for (int i = 0; i < _stage.Length; i++)
            {
                if (_stage[i] == (byte)BreachStage.Cracked)
                    _gateTokens[i] = Math.Min(TokensPerAgent, _gateTokens[i] + crackedAdd);
                else if (_stage[i] == (byte)BreachStage.Broken)
                    _gateTokens[i] = Math.Min(TokensPerAgent, _gateTokens[i] + brokenAdd);
            }
        }

        /// <summary>
        /// An agent entering a gate cell from a non-gate cell must spend one agent's worth of tokens.
        /// Returns false (and spends nothing) when the gate has not accumulated enough yet.
        /// </summary>
        public bool TryEnterGate(int x, int y)
        {
            int i = CellIndex(x, y);
            if (_stage[i] != (byte)BreachStage.Cracked && _stage[i] != (byte)BreachStage.Broken) return true;
            if (_gateTokens[i] < TokensPerAgent) return false;
            _gateTokens[i] -= TokensPerAgent;
            return true;
        }

        // ---------------------------------------------------------------- preview / hashing

        /// <summary>Copies the whole grid into a same-sized scratch map (build-mode what-if preview).</summary>
        public void CopyTo(GridMap scratch)
        {
            if (scratch.Width != Width || scratch.Height != Height)
                throw new ArgumentException("Scratch map must have identical dimensions.", nameof(scratch));
            Array.Copy(_cost, scratch._cost, _cost.Length);
            Array.Copy(_kind, scratch._kind, _kind.Length);
            Array.Copy(_hp, scratch._hp, _hp.Length);
            Array.Copy(_stage, scratch._stage, _stage.Length);
            Array.Copy(_gateTokens, scratch._gateTokens, _gateTokens.Length);
            scratch.GateCount = GateCount;
            scratch.Version = Version;
        }

        /// <summary>FNV-1a over wall kind, hp, stage, cost and gate tokens. Mixed into AgentWorld.StateHash.</summary>
        public ulong StateHash()
        {
            const ulong prime = 1099511628211UL;
            ulong h = 14695981039346656037UL;
            unchecked
            {
                for (int i = 0; i < _kind.Length; i++)
                {
                    h = (h ^ _kind[i]) * prime;
                    h = (h ^ _hp[i]) * prime;
                    h = (h ^ _stage[i]) * prime;
                    h = (h ^ _cost[i]) * prime;
                    h = (h ^ (uint)_gateTokens[i]) * prime;
                }
            }
            return h;
        }

        public static Vec2 CellCenter(int x, int y) => new Vec2(x + 0.5f, y + 0.5f);

        public (int X, int Y) WorldToCell(Vec2 position)
        {
            int x = Math.Clamp((int)MathF.Floor(position.X), 0, Width - 1);
            int y = Math.Clamp((int)MathF.Floor(position.Y), 0, Height - 1);
            return (x, y);
        }
    }
}
