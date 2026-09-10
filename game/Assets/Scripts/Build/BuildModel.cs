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
    public enum BuildItem { Barricade = 0, Turret = 1 }

    /// <summary>
    /// Build mode as pure state: a grid cursor, an item, and the place/sell rules. Every
    /// placement question goes through the sim's BuildValidator, so the overlay the player
    /// sees is the same algorithm that will route the horde ("preview never lies").
    /// Rendering and input live in the game layer.
    /// </summary>
    public sealed class BuildModel
    {
        public static readonly BuildItem[] Items = { BuildItem.Barricade, BuildItem.Turret };

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

        public BuildModel(GridMap map, AgentWorld world, TurretSystem turrets, MatchState match, EconomyConfig eco,
                          IReadOnlyList<(int X, int Y)> spawns, int goalX, int goalY, int cursorX, int cursorY)
        {
            _map = map; _world = world; _turrets = turrets; _match = match; _eco = eco;
            _spawns = spawns; _goalX = goalX; _goalY = goalY;
            Validator = new BuildValidator(map);
            SetCursor(cursorX, cursorY);
        }

        public int ItemCost => Item == BuildItem.Turret ? _eco.TurretCost : _eco.BarricadeCost;
        public string ItemName => Item == BuildItem.Turret ? "Sentry .50" : "Barricade";
        public bool CanAfford => _match.Bank.CanAfford(ItemCost);

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
            _oneCell[0] = (CursorX, CursorY);
            WallKind kind = Item == BuildItem.Turret ? WallKind.Structure : WallKind.Barricade;
            ushort hp = Item == BuildItem.Turret ? _turrets.Config.MaxHp : GridMap.DefaultWallHp;
            LastResult = Validator.Validate(_world, _oneCell, kind, hp, _goalX, _goalY, _spawns);
            if (LastResult != PlacementResult.Ok && LastResult != PlacementResult.SealsSpawn)
            {
                // Nothing can be placed here: make the preview show the live routing instead of a stale what-if.
                Validator.Validate(_world, Array.Empty<(int X, int Y)>(), kind, hp, _goalX, _goalY, _spawns);
            }
            Message = LastResult switch
            {
                PlacementResult.Ok => CanAfford ? "" : $"need ${ItemCost}",
                PlacementResult.SealsSpawn => "SEALED — they will chew through here",
                PlacementResult.Occupied => "runners in the way",
                PlacementResult.NotBuildable => "",
                _ => "",
            };
            return LastResult;
        }

        /// <summary>Places the current item at the cursor if legal and affordable. Full seals are allowed.</summary>
        public bool TryPlace()
        {
            Refresh();
            if (LastResult != PlacementResult.Ok && LastResult != PlacementResult.SealsSpawn) return false;
            if (!_match.Bank.TrySpend(ItemCost)) { Message = $"need ${ItemCost}"; return false; }

            if (Item == BuildItem.Turret) _turrets.Place(_map, CursorX, CursorY);
            else _map.SetWall(CursorX, CursorY, WallKind.Barricade, GridMap.DefaultWallHp);
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
                float hpFraction = _turrets.Remove(_map, i);
                refund = (int)MathF.Round(_eco.TurretCost * _match.RefundMultiplier * hpFraction);
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
