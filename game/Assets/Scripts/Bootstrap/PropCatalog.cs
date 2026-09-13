#nullable enable
using System;
using System.Collections.Generic;

namespace Cipher.Game
{
    /// <summary>
    /// What a piece of built scenery OCCUPIES, as opposed to what it looks like.
    ///
    /// <see cref="SiteProps"/> owns the boxes. This owns the cells, and the two are deliberately
    /// separate files because they are separate facts: a street lamp is two metres of geometry and
    /// zero cells, a swimming pool is almost no geometry and a great many cells.
    ///
    /// WHY THIS EXISTS AT ALL. The Gate shipped as a brown field with a road down the middle, and
    /// the owner's verdict was that the walls the player places read as arbitrary because nothing
    /// in the terrain prefers one route to another. A tower-defence map works when the ground does
    /// most of the funnelling and the player's barricades finish the job, and the ground can only
    /// funnel if the buildings on it are solid. So authored props claim grid cells.
    ///
    /// THE SEAM, and why it does not break the preview covenant (hard rule 4). There is exactly one
    /// <c>GridMap</c>. A building writes <c>WallKind.Rock</c> into it during the scene build,
    /// BEFORE the first flow field is computed and long before the player can open build mode, so
    /// the pathing preview, the live sim and <c>BuildValidator</c> all read the same map and cannot
    /// disagree. Nothing here is touched again while a match runs: no prop appears, moves or is
    /// destroyed mid-match, which is what would actually make a preview lie.
    ///
    /// Rock rather than Wall on purpose. A clubhouse is not something a sapper opens a hole in, it
    /// stops a bullet, and <c>GridMap.Damage</c> already refuses Rock, so this cannot quietly hand
    /// the horde a demolition route through the architecture.
    ///
    /// Footprints are authored as RECTS, plural, so a shape can be hollow: the pool's fence is
    /// solid and the water is solid but the deck you walk on between them is not.
    /// </summary>
    public static class PropCatalog
    {
        /// <summary>A solid rectangle in cells, relative to the prop's own cell, before yaw.</summary>
        public readonly struct Rect
        {
            public readonly int MinX, MinY, Width, Height;

            public Rect(int minX, int minY, int width, int height)
            {
                MinX = minX; MinY = minY; Width = width; Height = height;
            }
        }

        private static readonly Rect[] None = Array.Empty<Rect>();

        private static Rect[] Centred(int w, int h) =>
            new[] { new Rect(-(w / 2), -(h / 2), w, h) };

        /// <summary>
        /// Every kind the game can build, and the cells it claims.
        ///
        /// The first four are the original gate furniture and their footprints reproduce exactly
        /// what the bootstrap used to hard-code (guardhouse 3x3, the rest a single cell), so moving
        /// the data here changed no existing mission.
        /// </summary>
        private static readonly Dictionary<string, Rect[]> Footprints =
            new Dictionary<string, Rect[]>(StringComparer.Ordinal)
            {
                // --- gate furniture (was FloodBootstrap.MarkPropFootprint) ---
                ["Pillar"] = Centred(1, 1),
                ["Guardhouse"] = Centred(3, 3),
                ["BoomBarrier"] = Centred(1, 1),
                ["JerseyBarrier"] = Centred(1, 1),

                // --- the community ---
                // A house is the unit the whole neighbourhood is measured in. Wide enough to break
                // a sightline, and a row of them still leaves gardens between -- and the gardens
                // are the routes.
                //
                // SIX BY NINE BECAUSE THAT IS WHAT THE MODEL MEASURES. It was 6x5 while a house was
                // a box we drew ourselves and could size to the grid. The bought house is 6.4m x
                // 8.9m, and the choice was to grow the footprint or shrink the model. Shrinking it
                // would also shrink its doors, to about four feet -- shorter than the people walking
                // past them, which is the kind of wrongness nobody can name and everybody sees.
                //
                // The footprint follows the art here, and not the other way round, because the
                // footprint is the thing we can author freely and the door height is not.
                ["House"] = Centred(6, 9),
                // The clubhouse is the biggest single obstruction on the map and is placed so that
                // the gap between it and the blocked road is a pinch the player can hold.
                //
                // STAYS 16x12 EVEN THOUGH THE MODEL IS 15.5 x 10.9m, and that is the opposite call
                // to the one the House got. Worth the words, because the rule is not "the footprint
                // always follows the art".
                //
                // The House GREW to fit its model, because a model larger than its footprint sticks
                // out of the solid ground into somewhere people can walk, and doors you can walk
                // through are worse than a spare cell. Here the model is SMALLER than the footprint,
                // so the error is half a metre of solid ground with no wall drawn on it -- invisible
                // beside a sixteen-metre building.
                //
                // And shrinking it is not free: taking the clubhouse to 16x11 widened the gap beside
                // it and opened A THIRD HOLE in the barricade column, which
                // TheRoadIsBlockedAndTheOnlyWayPastItIsTwoPinches caught immediately. That gap is
                // the thing this whole position is built around. The art bends to the level here,
                // because the level is load-bearing and half a metre is not.
                ["Clubhouse"] = Centred(16, 12),
                // Rec centre / gym: a tall hall with a lower entrance wing off the front. The wing
                // is a second rect rather than a bigger box, because an L reads as a building and a
                // cuboid reads as a crate.
                ["CommunityCentre"] = new[] { new Rect(-7, -5, 14, 8), new Rect(-4, 3, 8, 4) },
                ["PoolHouse"] = Centred(7, 5),
                // The pool: the WATER is solid, the deck around it is not. You walk the deck; you
                // do not walk across the deep end.
                ["PoolDeck"] = Centred(10, 6),
                ["Bleachers"] = Centred(8, 3),

                // --- improvised blockade, the owner's "makeshift event blockading" ---
                ["BrushPile"] = Centred(3, 2),
                ["PalletStack"] = Centred(2, 2),
                ["Dumpster"] = Centred(3, 2),

                // Decoration proper: a lamp post does not stop anybody and should not be allowed to
                // pretend it does. An empty footprint is a supported answer, not a missing one.
                // A LANDMARK, AND DELIBERATELY NOT AN OBSTRUCTION (design pillar P6).
                //
                // Eight metres tall against houses of six to eight, so it clears the roofline and
                // can be seen from most of the position -- which is the entire job. A landmark only
                // works if it contrasts with what is immediately around it, and a tower over a
                // street of bungalows does.
                //
                // Its footprint is None on purpose. The Gate's geometry is pinned by tests that
                // count the holes in the barricade column, and a landmark has no business perturbing
                // the ground a position is balanced on. It is legs on a pad; you can walk under it.
                ["WaterTower"] = None,

                ["StreetLamp"] = None,
                ["PicnicTable"] = None,
                ["Mailbox"] = None,
            };

        /// <summary>Every kind the catalogue knows, for the scenario reader to validate against.</summary>
        public static IReadOnlyCollection<string> Kinds => Footprints.Keys;

        /// <summary>True if <paramref name="kind"/> is buildable.</summary>
        public static bool Knows(string kind) => Footprints.ContainsKey(kind);

        /// <summary>
        /// The cells a prop of this kind claims when placed at <paramref name="x"/>,
        /// <paramref name="y"/> facing <paramref name="yaw"/>.
        ///
        /// Yaw is snapped to the nearest quarter turn. Buildings are authored on the grid and a
        /// footprint at 37 degrees is either a lie about what is solid or a staircase of cells that
        /// looks like a bug; the geometry is free to sit at whatever angle the author wrote, and a
        /// few degrees of skew between a wall and its cells is invisible.
        /// </summary>
        public static IEnumerable<(int X, int Y)> Footprint(string kind, float x, float y, float yaw)
        {
            if (!Footprints.TryGetValue(kind, out var rects) || rects.Length == 0)
                yield break;

            int cx = (int)Math.Floor(x);
            int cy = (int)Math.Floor(y);
            int quarter = QuarterTurns(yaw);

            foreach (var r in rects)
            {
                for (int dx = 0; dx < r.Width; dx++)
                {
                    for (int dy = 0; dy < r.Height; dy++)
                    {
                        var (rx, ry) = Turn(r.MinX + dx, r.MinY + dy, quarter);
                        yield return (cx + rx, cy + ry);
                    }
                }
            }
        }

        /// <summary>Yaw in degrees to a quarter-turn count in 0..3, for any sign or magnitude.</summary>
        public static int QuarterTurns(float yaw)
        {
            int q = (int)Math.Round(yaw / 90.0);
            q %= 4;
            return q < 0 ? q + 4 : q;
        }

        /// <summary>Rotates a cell offset by whole quarter turns, clockwise as yaw increases.</summary>
        public static (int X, int Y) Turn(int dx, int dy, int quarter) => quarter switch
        {
            1 => (dy, -dx),
            2 => (-dx, -dy),
            3 => (-dy, dx),
            _ => (dx, dy),
        };
    }
}
