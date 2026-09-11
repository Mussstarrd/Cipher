#nullable enable
using System.Collections.Generic;
using System.IO;
using Cipher.Game.Scenarios;
using Cipher.Sim.Agents;
using Cipher.Sim.Core;
using Cipher.Sim.Grid;
using NUnit.Framework;
using UnityEngine;

namespace Cipher.Game.Tests
{
    /// <summary>
    /// The cells a piece of built scenery claims.
    ///
    /// This matters more than a footprint table usually would, because these cells are the terrain
    /// the crowd paths around: a wrong footprint is not a cosmetic bug, it is a level whose funnel
    /// is somewhere other than where the buildings are.
    /// </summary>
    public sealed class PropCatalogTests
    {
        private static List<(int X, int Y)> Cells(string kind, float x, float y, float yaw = 0f)
        {
            var list = new List<(int, int)>();
            foreach (var c in PropCatalog.Footprint(kind, x, y, yaw)) list.Add(c);
            return list;
        }

        [Test]
        public void TheGateFurnitureKeepsTheFootprintsItAlwaysHad()
        {
            // These four moved here out of a switch in the bootstrap. If they changed, every
            // mission already shipped changed with them, silently.
            Assert.That(Cells("Guardhouse", 35f, 60f), Has.Count.EqualTo(9));
            Assert.That(Cells("Guardhouse", 35f, 60f), Contains.Item((34, 59)));
            Assert.That(Cells("Guardhouse", 35f, 60f), Contains.Item((36, 61)));
            Assert.That(Cells("Pillar", 29f, 40.8f), Is.EqualTo(new[] { (29, 40) }));
            Assert.That(Cells("JerseyBarrier", 36f, 46f), Has.Count.EqualTo(1));
            Assert.That(Cells("BoomBarrier", 30.8f, 42.8f), Is.EqualTo(new[] { (30, 42) }));
        }

        [Test]
        public void ABuildingClaimsItsWholeFloorPlan()
        {
            var house = Cells("House", 50f, 30f);
            Assert.That(house, Has.Count.EqualTo(30), "a house is six cells by five");
            Assert.That(house, Contains.Item((47, 28)));
            Assert.That(house, Contains.Item((52, 32)));
            Assert.That(house.Contains((46, 28)), Is.False);
            Assert.That(house.Contains((53, 32)), Is.False);
        }

        [Test]
        public void AQuarterTurnSwapsTheAxes()
        {
            var flat = Cells("House", 50f, 30f);
            var turned = Cells("House", 50f, 30f, 90f);

            Assert.That(turned, Has.Count.EqualTo(flat.Count));
            // Six along x and five along y becomes five along x and six along y.
            Assert.That(Span(turned), Is.EqualTo((5, 6)));
            Assert.That(Span(flat), Is.EqualTo((6, 5)));
            Assert.That(turned, Contains.Item((48, 28)));
            Assert.That(turned, Contains.Item((52, 33)));
            Assert.That(turned.Contains((47, 28)), Is.False);
        }

        [Test]
        public void AHalfTurnKeepsTheFloorPlanAndShiftsAnEvenOneByACell()
        {
            // Worth pinning down because it is the kind of thing an author trips over once and
            // then distrusts forever. A footprint with an EVEN number of cells across cannot be
            // centred on a cell: it occupies one more cell on the low side than the high side, and
            // turning it through 180 degrees swaps which side that is. The shape and its size are
            // unchanged; it sits one cell over. Odd footprints are unaffected.
            var flat = Cells("Clubhouse", 80f, 66f);
            var about = Cells("Clubhouse", 80f, 66f, 180f);

            Assert.That(Span(about), Is.EqualTo(Span(flat)));
            Assert.That(about, Has.Count.EqualTo(flat.Count));
            Assert.That(Origin(about), Is.EqualTo((Origin(flat).X + 1, Origin(flat).Y + 1)));

            // Odd: a guardhouse is three by three and does not move.
            Assert.That(Cells("Guardhouse", 35f, 60f, 180f),
                        Is.EquivalentTo(Cells("Guardhouse", 35f, 60f)));
        }

        /// <summary>Width and height, in cells, of a footprint's bounding box.</summary>
        private static (int W, int H) Span(List<(int X, int Y)> cells)
        {
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach (var (x, y) in cells)
            {
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
            return (maxX - minX + 1, maxY - minY + 1);
        }

        /// <summary>Low corner of a footprint's bounding box.</summary>
        private static (int X, int Y) Origin(List<(int X, int Y)> cells)
        {
            int minX = int.MaxValue, minY = int.MaxValue;
            foreach (var (x, y) in cells)
            {
                if (x < minX) minX = x;
                if (y < minY) minY = y;
            }
            return (minX, minY);
        }

        [Test]
        public void YawIsSnappedToTheNearestQuarterTurn()
        {
            Assert.That(PropCatalog.QuarterTurns(0f), Is.EqualTo(0));
            Assert.That(PropCatalog.QuarterTurns(37f), Is.EqualTo(0));
            Assert.That(PropCatalog.QuarterTurns(80f), Is.EqualTo(1));
            Assert.That(PropCatalog.QuarterTurns(200f), Is.EqualTo(2));
            Assert.That(PropCatalog.QuarterTurns(-90f), Is.EqualTo(3));
            Assert.That(PropCatalog.QuarterTurns(720f), Is.EqualTo(0));
        }

        [Test]
        public void AnLShapedBuildingClaimsBothWings()
        {
            var rec = Cells("CommunityCentre", 78f, 32f);
            Assert.That(rec, Contains.Item((84, 30)), "the hall reaches the barricade column");
            Assert.That(rec, Contains.Item((78, 37)), "and the entrance wing sticks out in front");
            Assert.That(rec.Contains((84, 37)), Is.False,
                        "the wing is narrower than the hall; that gap IS the south pinch");
        }

        [Test]
        public void DecorationClaimsNothing()
        {
            // A lamp post that stopped a crowd would be a lie the pathing tells the player.
            Assert.That(Cells("StreetLamp", 34f, 44f), Is.Empty);
            Assert.That(Cells("PicnicTable", 44f, 26f), Is.Empty);
            Assert.That(Cells("Mailbox", 36f, 53.5f), Is.Empty);
            Assert.That(PropCatalog.Knows("StreetLamp"), Is.True,
                        "no footprint is not the same as not being a prop");
        }

        [Test]
        public void AnUnknownKindIsKnownToBeUnknown()
        {
            Assert.That(PropCatalog.Knows("Lighthouse"), Is.False);
            Assert.That(Cells("Lighthouse", 10f, 10f), Is.Empty);
        }

        [Test]
        public void EveryCatalogueKindActuallyBuildsSomething()
        {
            // The catalogue is what the scenario reader validates against, so a kind listed here
            // and missing from SiteProps' switch is a prop that loads clean and never appears --
            // the exact failure the strict reader exists to prevent.
            var root = new GameObject("props").transform;
            try
            {
                foreach (var kind in PropCatalog.Kinds)
                {
                    var props = new SiteProps(root, _ => new Material(Shader.Find("Unlit/Color")));
                    props.Build(kind, 10f, 10f, 0f);
                    Assert.That(props.Placed, Is.EqualTo(1), $"SiteProps cannot build '{kind}'");
                }
            }
            finally
            {
                Object.DestroyImmediate(root.gameObject);
            }
        }
    }

    /// <summary>
    /// The Gate, as a piece of level design rather than as a file that parses.
    ///
    /// The owner played it and said the map had no reason to exist: "besides need to have either
    /// houses or community Center / gym or pool whatever we got to fill that empty space so that
    /// traffic can naturally go to the Center Road area and give me a reason to blockade that
    /// space". Terrain that funnels is a PROPERTY OF THE MAP, and a property is testable. These
    /// tests fail if somebody moves a building and quietly reopens the field or seals the level.
    /// </summary>
    public sealed class GateLevelShapeTests
    {
        private const int BarricadeColumn = 84;

        private static ScenarioDef Gate()
        {
            string path = Path.Combine("Assets", "Resources", "Scenarios", "act1-01-the-gate.json");
            return ScenarioReader.Read(File.ReadAllText(path));
        }

        /// <summary>
        /// The map as the bootstrap builds it: authored walls, then every prop's footprint.
        ///
        /// KeepClear is deliberately NOT applied. The game frees the road corridor and the hero's
        /// own cells afterwards, so this map is a superset of what is solid at runtime: anything
        /// reachable here is reachable in the game, and a pinch measured here is no wider there.
        /// </summary>
        private static GridMap Build(ScenarioDef def)
        {
            var map = new GridMap(def.Map.Width, def.Map.Height);
            foreach (var w in def.Map.Walls)
                for (int x = w.X; x < w.X + w.Width; x++)
                    for (int y = w.Y; y < w.Y + w.Height; y++)
                        map.SetWall(x, y, w.Kind, GridMap.DefaultWallHp);

            foreach (var p in def.Props)
                foreach (var (x, y) in PropCatalog.Footprint(p.Kind, p.X, p.Y, p.Yaw))
                {
                    if (!map.InBounds(x, y)) continue;
                    if (map.KindAt(x, y) != WallKind.None) continue;
                    map.SetWall(x, y, WallKind.Rock, GridMap.DefaultWallHp);
                }

            return map;
        }

        [Test]
        public void EveryGateCanStillReachTheTruck()
        {
            // The one thing a dressed map may never do. A spawn with no route is a whole gate's
            // worth of crowd standing still for the entire mission.
            var def = Gate();
            var map = Build(def);
            var field = new FlowField(map);
            field.Compute(def.Vault.X, def.Vault.Y);

            foreach (var gate in def.SpawnCells)
            {
                Assert.That(map.IsBlocked(gate.X, gate.Y), Is.False,
                            $"gate '{gate.Gate}' spawns inside something solid");
                Assert.That(field.HasPath(gate.X, gate.Y), Is.True,
                            $"gate '{gate.Gate}' at ({gate.X},{gate.Y}) cannot reach the truck");
            }
        }

        [Test]
        public void TheHeroDoesNotSpawnInsideABuilding()
        {
            var def = Gate();
            var map = Build(def);
            Assert.That(map.IsBlocked(def.HeroSpawn.X, def.HeroSpawn.Y), Is.False);
        }

        [Test]
        public void NoPropIsPlacedOnTopOfAnother()
        {
            // Two buildings sharing cells look like one building with a wall through it, and the
            // overlap is invisible in the footprint because the second one silently loses.
            var def = Gate();
            var taken = new Dictionary<(int, int), string>();
            foreach (var p in def.Props)
                foreach (var cell in PropCatalog.Footprint(p.Kind, p.X, p.Y, p.Yaw))
                {
                    Assert.That(taken.TryGetValue(cell, out string? already), Is.False,
                                $"{p.Kind} at ({p.X},{p.Y}) overlaps {already} at {cell}");
                    taken[cell] = $"{p.Kind}@({p.X},{p.Y})";
                }
        }

        [Test]
        public void NoPropSitsOnAnAuthoredWall()
        {
            var def = Gate();
            var walls = new GridMap(def.Map.Width, def.Map.Height);
            foreach (var w in def.Map.Walls)
                for (int x = w.X; x < w.X + w.Width; x++)
                    for (int y = w.Y; y < w.Y + w.Height; y++)
                        walls.SetWall(x, y, w.Kind, GridMap.DefaultWallHp);

            foreach (var p in def.Props)
                foreach (var (x, y) in PropCatalog.Footprint(p.Kind, p.X, p.Y, p.Yaw))
                {
                    if (!walls.InBounds(x, y)) continue;
                    Assert.That(walls.KindAt(x, y), Is.EqualTo(WallKind.None),
                                $"{p.Kind} at ({p.X},{p.Y}) stands on the fence at ({x},{y})");
                }
        }

        [Test]
        public void TheRoadIsBlockedAndTheOnlyWayPastItIsTwoPinches()
        {
            // This is the level's whole design in one test. Across the middle of the map the
            // column the barricade stands in is solid -- rec centre, barricade, clubhouse -- except
            // for two short gaps. Those two gaps are the reason the player has anything to do.
            var def = Gate();
            var map = Build(def);

            Assert.That(map.IsBlocked(BarricadeColumn, 48), Is.True, "the road itself must be shut");

            var south = OpenRunThrough(map, 37);
            var north = OpenRunThrough(map, 57);

            Assert.That(south.Width, Is.InRange(3, 6),
                        $"the south pinch beside the rec centre is {south.Width} cells " +
                        $"(y {south.From}..{south.To}); under three cannot be fought in, over six " +
                        "is not a pinch");
            Assert.That(north.Width, Is.InRange(3, 6),
                        $"the north pinch beside the clubhouse is {north.Width} cells " +
                        $"(y {north.From}..{north.To})");

            // And nothing else is open between the rec centre and the back of the clubhouse, or
            // the pinches are not pinches -- they are two of several ways through.
            int open = 0;
            for (int y = 27; y <= 72; y++) if (!map.IsBlocked(BarricadeColumn, y)) open++;
            Assert.That(open, Is.EqualTo(south.Width + north.Width),
                        "the barricade column has a third hole in it");
        }

        /// <summary>The contiguous run of open cells in the barricade column containing y.</summary>
        private static (int From, int To, int Width) OpenRunThrough(GridMap map, int y)
        {
            Assert.That(map.IsBlocked(BarricadeColumn, y), Is.False,
                        $"({BarricadeColumn},{y}) was expected to be a way through and is solid");
            int from = y, to = y;
            while (from > 0 && !map.IsBlocked(BarricadeColumn, from - 1)) from--;
            while (to < map.Height - 1 && !map.IsBlocked(BarricadeColumn, to + 1)) to++;
            return (from, to, to - from + 1);
        }

        [Test]
        public void TheCentreRoadIsTheShortRouteAndTheFlanksAreTheLongOne()
        {
            // Why the crowd prefers the road: measured, not asserted by eye. The cost of walking
            // from the county road gate to the truck is compared with the cost from a point out in
            // the southern field at the same distance east.
            var def = Gate();
            var map = Build(def);
            var field = new FlowField(map);
            field.Compute(def.Vault.X, def.Vault.Y);

            float onTheRoad = field.IntegrationCostAt(40, 48);
            float outInTheField = field.IntegrationCostAt(40, 14);

            Assert.That(field.HasPath(40, 14), Is.True, "the field must not be a dead end");
            Assert.That(onTheRoad, Is.LessThan(outInTheField),
                        "the road has to be the cheapest way east or the buildings are decoration");
        }

        [Test]
        public void TheFlankGatesAreAtStructuresRatherThanInEmptyGround()
        {
            // The owner: "I want them to come like for real from the side and maybe they can just
            // climb over the fence of whatever structure is there". A flank that spawns in a bare
            // field is the thing he was complaining about, so the test is that each flank gate has
            // something built within a few cells of it.
            var def = Gate();
            foreach (var gate in def.SpawnCells)
            {
                if (!gate.Flank) continue;

                bool nearSomething = false;
                foreach (var p in def.Props)
                {
                    foreach (var (x, y) in PropCatalog.Footprint(p.Kind, p.X, p.Y, p.Yaw))
                        if (Mathf.Abs(x - gate.X) <= 8 && Mathf.Abs(y - gate.Y) <= 8) { nearSomething = true; break; }
                    if (nearSomething) break;
                }

                foreach (var w in def.Map.Walls)
                {
                    if (nearSomething) break;
                    if (gate.X >= w.X - 8 && gate.X < w.X + w.Width + 8 &&
                        gate.Y >= w.Y - 8 && gate.Y < w.Y + w.Height + 8) nearSomething = true;
                }

                Assert.That(nearSomething, Is.True,
                            $"flank gate '{gate.Gate}' at ({gate.X},{gate.Y}) arrives out of thin air");
            }
        }

        [Test]
        public void TheBuildValidatorSeesTheBuildingsAndStillRefusesToSealTheMap()
        {
            // Hard rule 4, on the map the buildings changed. There is one GridMap: a clubhouse
            // writes Rock into it during the scene build, so the preview's scratch copy inherits
            // the architecture and cannot disagree with the live sim about where the gaps are.
            //
            // Proved by closing the south pinch and checking the validator reports it: a wall the
            // player can place there is now load-bearing terrain, which is exactly the decision
            // the level is asking them to make.
            var def = Gate();
            var map = Build(def);
            var field = new FlowField(map);
            field.Compute(def.Vault.X, def.Vault.Y);
            var world = new AgentWorld(map, field, new SimConfig(), initialCapacity: 8);
            var validator = new BuildValidator(map);
            var spawns = new List<(int X, int Y)>();
            foreach (var g in def.SpawnCells) spawns.Add((g.X, g.Y));

            var southPinch = new List<(int X, int Y)>();
            var run = OpenRunThrough(map, 37);
            for (int y = run.From; y <= run.To; y++) southPinch.Add((BarricadeColumn, y));

            // Closing one pinch is legal: the other one, and the long way round, are still open.
            Assert.That(validator.Validate(world, southPinch, WallKind.Barricade, 20,
                                           def.Vault.X, def.Vault.Y, spawns),
                        Is.EqualTo(PlacementResult.Ok),
                        "closing one pinch must stay legal, or the level has only one answer");

            BuildValidator.Commit(map, southPinch, WallKind.Barricade, 20);

            // Closing the other one too, plus the open ground past the clubhouse, DOES seal the
            // pool gate in behind the architecture, and the validator has to say so.
            var rest = new List<(int X, int Y)>();
            for (int y = 0; y < map.Height; y++)
                if (!map.IsBlocked(BarricadeColumn, y)) rest.Add((BarricadeColumn, y));

            Assert.That(validator.Validate(world, rest, WallKind.Barricade, 20,
                                           def.Vault.X, def.Vault.Y, spawns),
                        Is.EqualTo(PlacementResult.SealsSpawn),
                        "walling the whole column must be reported as sealing, not quietly allowed");
        }

        [Test]
        public void ThePlaceHasBuildingsInIt()
        {
            // A blunt guard against the thing that was actually wrong: an empty map either side of
            // the road. Counted as claimed floor area rather than as prop count, so twenty street
            // lamps cannot satisfy it.
            var def = Gate();
            int claimed = 0;
            foreach (var p in def.Props)
                foreach (var _ in PropCatalog.Footprint(p.Kind, p.X, p.Y, p.Yaw)) claimed++;

            Assert.That(claimed, Is.GreaterThan(600),
                        "The Gate should be a community, not a field with a road down it");
        }
    }
}
