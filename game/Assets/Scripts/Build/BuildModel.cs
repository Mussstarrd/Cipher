#nullable enable
using System;
using System.Collections.Generic;
using Cipher.Game.Match;
using Cipher.Sim.Agents;
using Cipher.Sim.Core;
using Cipher.Sim.Emplacements;
using Cipher.Sim.Grid;

namespace Cipher.Game.Build
{
    public enum BuildItem { Barricade = 0, Turret = 1, RepairDrone = 2 }

    /// <summary>
    /// Build mode as pure state: a grid cursor, an item, and the place/sell rules. Every
    /// placement question goes through the sim's BuildValidator, so the overlay the player
    /// sees is the same algorithm that will route the horde ("preview never lies").
    /// Rendering and input live in the game layer.
    /// </summary>
    public sealed class BuildModel
    {
        public static readonly BuildItem[] Items = { BuildItem.Barricade, BuildItem.Turret, BuildItem.RepairDrone };

        private readonly GridMap _map;
        private readonly AgentWorld _world;
        private readonly TurretSystem _turrets;
        private readonly MatchState _match;
        private readonly EconomyConfig _eco;
        private readonly IReadOnlyList<(int X, int Y)> _spawns;
        private readonly int _goalX, _goalY;
        private readonly (int X, int Y)[] _oneCell = new (int, int)[1];

        public BuildValidator Validator { get; }
        public int CursorX { get; private set; }
        public int CursorY { get; private set; }
        public BuildItem Item { get; private set; } = BuildItem.Barricade;
        public PlacementResult LastResult { get; private set; } = PlacementResult.Ok;
        public string Message { get; private set; } = "";
        public int Placed { get; private set; }
        public int Sold { get; private set; }
        public int Upgraded { get; private set; }
        /// <summary>Live repair drones (consumables). Finished drones are pruned on Tick.</summary>
        public List<RepairDrone> Drones { get; } = new List<RepairDrone>(4);

        public BuildModel(GridMap map, AgentWorld world, TurretSystem turrets, MatchState match, EconomyConfig eco,
                          IReadOnlyList<(int X, int Y)> spawns, int goalX, int goalY, int cursorX, int cursorY)
        {
            _map = map; _world = world; _turrets = turrets; _match = match; _eco = eco;
            _spawns = spawns; _goalX = goalX; _goalY = goalY;
            Validator = new BuildValidator(map);
            SetCursor(cursorX, cursorY);
        }

        public int ItemCost => Item switch { BuildItem.Turret => _eco.TurretCost, BuildItem.RepairDrone => _eco.DroneCost, _ => _eco.BarricadeCost };
        public string ItemName => Item switch { BuildItem.Turret => "Sentry .50", BuildItem.RepairDrone => "Repair Drone", _ => "Barricade" };
        public static string NameOf(BuildItem item) => item switch { BuildItem.Turret => "Sentry .50", BuildItem.RepairDrone => "Repair Drone", _ => "Barricade" };
        public int CostOf(BuildItem item) => item switch { BuildItem.Turret => _eco.TurretCost, BuildItem.RepairDrone => _eco.DroneCost, _ => _eco.BarricadeCost };
        public bool CanAfford => _match.Bank.CanAfford(ItemCost);

        /// <summary>Turret under the cursor, or -1.</summary>
        public int HoveredTurret => _turrets.IndexAt(CursorX, CursorY);

        /// <summary>Upgrade offer for the hovered turret: "Twin .50 $120", "MAX", or "" when not on a turret.</summary>
        public string UpgradeOffer
        {
            get
            {
                int i = HoveredTurret;
                if (i < 0) return "";
                var next = _turrets.NextTier(i);
                return next == null ? "MAX tier" : $"{next.Name} ${next.Cost}";
            }
        }

        /// <summary>Cells this far from the cursor are close enough for a drone to snap onto a breach.</summary>
        public const int DroneSnapRadius = 3;

        /// <summary>The breach a drone would be dropped on, or (-1,-1). Snaps so the player never has to hit one cell exactly.</summary>
        public (int X, int Y) DroneTarget { get; private set; } = (-1, -1);

        private bool IsBreached(int x, int y)
            => _map.KindAt(x, y) != WallKind.None && _map.StageAt(x, y) != BreachStage.Intact;

        private bool HasDroneAt(int x, int y)
        {
            foreach (var d in Drones) if (d.X == x && d.Y == y) return true;
            return false;
        }

        /// <summary>Nearest un-droned breach within <see cref="DroneSnapRadius"/>; ties resolve deterministically.</summary>
        private (int X, int Y) FindNearestBreach()
        {
            (int X, int Y) best = (-1, -1);
            int bestDistSq = int.MaxValue;
            for (int dy = -DroneSnapRadius; dy <= DroneSnapRadius; dy++)
            {
                for (int dx = -DroneSnapRadius; dx <= DroneSnapRadius; dx++)
                {
                    int x = CursorX + dx, y = CursorY + dy;
                    if (!_map.InBounds(x, y) || !IsBreached(x, y) || HasDroneAt(x, y)) continue;
                    int distSq = dx * dx + dy * dy;
                    if (distSq < bestDistSq) { bestDistSq = distSq; best = (x, y); }
                }
            }
            return best;
        }

        public void SetCursor(int x, int y)
        {
            CursorX = Math.Clamp(x, 0, _map.Width - 1);
            CursorY = Math.Clamp(y, 0, _map.Height - 1);
        }

        public void MoveCursor(int dx, int dy) => SetCursor(CursorX + dx, CursorY + dy);

        public void CycleItem(int direction)
        {
            int n = Items.Length;
            int i = Array.IndexOf(Items, Item);
            Item = Items[((i + direction) % n + n) % n];
        }

        /// <summary>
        /// Re-validates the cursor cell for the current item and refreshes the preview field.
        /// Call after any cursor move, item change, or world change.
        /// </summary>
        public PlacementResult Refresh()
        {
            if (Item == BuildItem.RepairDrone)
            {
                // Drones go on breached cells, which are never "buildable"; the preview shows live routing.
                Validator.Validate(_world, Array.Empty<(int X, int Y)>(), WallKind.Barricade, GridMap.DefaultWallHp, _goalX, _goalY, _spawns);
                DroneTarget = FindNearestBreach();
                LastResult = DroneTarget.X >= 0 ? PlacementResult.Ok : PlacementResult.NotBuildable;
                Message = LastResult == PlacementResult.Ok
                    ? (CanAfford ? $"drop drone on the breach at {DroneTarget.X},{DroneTarget.Y}" : $"need ${ItemCost}")
                    : "no breach within reach — move the cursor next to an orange wall";
                return LastResult;
            }
            DroneTarget = (-1, -1);

            _oneCell[0] = (CursorX, CursorY);
            WallKind kind = Item == BuildItem.Turret ? WallKind.Structure : WallKind.Barricade;
            ushort hp = Item == BuildItem.Turret ? _turrets.Config.MaxHp : GridMap.DefaultWallHp;
            LastResult = Validator.Validate(_world, _oneCell, kind, hp, _goalX, _goalY, _spawns);
            if (LastResult != PlacementResult.Ok && LastResult != PlacementResult.SealsSpawn)
            {
                // Nothing can be placed here: make the preview show the live routing instead of a stale what-if.
                Validator.Validate(_world, Array.Empty<(int X, int Y)>(), kind, hp, _goalX, _goalY, _spawns);
            }
            string upgrade = UpgradeOffer;
            Message = LastResult switch
            {
                PlacementResult.Ok => CanAfford ? "" : $"need ${ItemCost}",
                PlacementResult.SealsSpawn => "SEALED — they will chew through here",
                PlacementResult.Occupied => "runners in the way",
                PlacementResult.NotBuildable => upgrade.Length > 0 ? $"turret: Y upgrade ({upgrade})  X sell" : "",
                _ => "",
            };
            return LastResult;
        }

        /// <summary>Upgrades the turret under the cursor if there is a next tier and the bank can pay.</summary>
        public bool TryUpgrade()
        {
            int i = HoveredTurret;
            if (i < 0) return false;
            var next = _turrets.NextTier(i);
            if (next == null) { Message = "MAX tier"; return false; }
            if (!_match.Bank.TrySpend(next.Cost)) { Message = $"need ${next.Cost}"; return false; }
            _turrets.Upgrade(_map, i);
            Upgraded++;
            Message = $"upgraded: {next.Name}";
            Refresh();
            return true;
        }

        /// <summary>Advances repair drones; prunes finished ones. Returns stages repaired this call.</summary>
        public int TickDrones(Vec2 heroPosition, float dt)
        {
            int repaired = 0;
            for (int i = Drones.Count - 1; i >= 0; i--)
            {
                repaired += Drones[i].Tick(_world, heroPosition, dt);
                if (Drones[i].Done) Drones.RemoveAt(i);
            }
            return repaired;
        }

        /// <summary>Places the current item at the cursor if legal and affordable. Full seals are allowed.</summary>
        public bool TryPlace()
        {
            Refresh();
            if (LastResult != PlacementResult.Ok && LastResult != PlacementResult.SealsSpawn) return false;
            if (!_match.Bank.TrySpend(ItemCost)) { Message = $"need ${ItemCost}"; return false; }

            switch (Item)
            {
                case BuildItem.Turret: _turrets.Place(_map, CursorX, CursorY); break;
                case BuildItem.RepairDrone: Drones.Add(new RepairDrone(DroneTarget.X, DroneTarget.Y)); break;
                default: _map.SetWall(CursorX, CursorY, WallKind.Barricade, GridMap.DefaultWallHp); break;
            }
            Placed++;
            Refresh();
            return true;
        }

        /// <summary>Sells whatever player-built thing is under the cursor. Refund = cost x phase multiplier x remaining health.</summary>
        public bool TrySell()
        {
            int refund;
            WallKind kind = _map.KindAt(CursorX, CursorY);
            if (kind == WallKind.Structure)
            {
                int i = _turrets.IndexAt(CursorX, CursorY);
                if (i < 0) return false;
                int invested = _eco.TurretCost + _turrets.Turrets[i].Invested;
                float hpFraction = _turrets.Remove(_map, i);
                refund = (int)MathF.Round(invested * _match.RefundMultiplier * hpFraction);
            }
            else if (kind == WallKind.Barricade)
            {
                float hpFraction = (float)_map.HpAt(CursorX, CursorY) / GridMap.DefaultWallHp;
                _map.Clear(CursorX, CursorY);
                refund = (int)MathF.Round(_eco.BarricadeCost * _match.RefundMultiplier * hpFraction);
            }
            else
            {
                return false;
            }

            _match.Bank.Earn(refund);
            Sold++;
            Message = $"sold +${refund}";
            Refresh();
            return true;
        }

        /// <summary>
        /// Follows the preview field from a spawn cell toward the goal, appending cell centres to
        /// <paramref name="route"/>. Returns true if the route reaches the goal. Call after Refresh.
        /// </summary>
        public bool TraceRoute((int X, int Y) spawn, List<Vec2> route, int maxSteps = 600)
        {
            route.Clear();
            var field = Validator.PreviewField;
            if (!field.HasPath(spawn.X, spawn.Y)) { route.Add(GridMap.CellCenter(spawn.X, spawn.Y)); return false; }

            Vec2 p = GridMap.CellCenter(spawn.X, spawn.Y);
            int cx = spawn.X, cy = spawn.Y;
            route.Add(p);
            for (int step = 0; step < maxSteps; step++)
            {
                if (cx == _goalX && cy == _goalY) return true;
                Vec2 d = field.DirectionAt(cx, cy);
                if (d.LengthSquared < 1e-6f) return false;
                p += d;
                (cx, cy) = _map.WorldToCell(p);
                route.Add(p);
            }
            return false;
        }
    }
}
