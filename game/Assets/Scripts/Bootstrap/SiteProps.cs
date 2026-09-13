#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cipher.Game
{
    /// <summary>
    /// The built things that make a position a PLACE rather than an arena: gate pillars, a
    /// guardhouse, a boom barrier -- and, since the owner played The Gate and found it was a brown
    /// field with a road down it, the community itself. Houses, the clubhouse, the rec centre, the
    /// fenced pool.
    ///
    /// These are made out of boxes, on purpose. The free kits have vegetation, vehicles and street
    /// furniture but no buildings, and the alternative to boxes is either a purchase or nothing.
    /// Under the ink shader a box is not a placeholder: flat banded colour and a hard contour line
    /// is what the whole art direction is, and a guardhouse that is six boxes with a roof overhang
    /// and a window band reads as a guardhouse from forty metres. The thing that would look cheap
    /// is a photoreal model next to them, which is the direction we are not going.
    ///
    /// What makes a box stop reading as a crate, in rough order of how much it buys:
    ///   - a PITCHED roof that overhangs the walls, so the silhouette has a point in it;
    ///   - a dark window band at eye height, and a door you can see is a door;
    ///   - a porch or a canopy, because a flat facade has no depth and a porch is two boxes;
    ///   - a chimney, an air handler, a stoop: one asymmetric thing so it is not a solid of
    ///     revolution;
    ///   - colour VARIATION between neighbours, taken from the prop's own cell, so a street of
    ///     houses is a street rather than a barracks.
    ///
    /// WHAT IS SOLID LIVES IN <see cref="PropCatalog"/>, not here. This file is only the boxes.
    /// The class comment on that file explains why authored props now claim grid cells and why
    /// doing so does not break the build preview's covenant.
    /// </summary>
    public sealed class SiteProps
    {
        private readonly Transform _root;
        private readonly Func<Color, Material> _material;

        private static readonly Color Brick = new Color(0.40f, 0.25f, 0.21f);
        private static readonly Color Concrete = new Color(0.55f, 0.54f, 0.51f);
        private static readonly Color Siding = new Color(0.62f, 0.60f, 0.54f);
        private static readonly Color Roof = new Color(0.24f, 0.24f, 0.26f);
        private static readonly Color Glass = new Color(0.16f, 0.21f, 0.24f);
        private static readonly Color HazardRed = new Color(0.62f, 0.14f, 0.12f);
        private static readonly Color HazardWhite = new Color(0.86f, 0.85f, 0.80f);

        // The rest of the neighbourhood's palette. Kept small and deliberately desaturated: the
        // lighting is an overcast Virginia winter and a saturated house punches a hole in it.
        private static readonly Color Timber = new Color(0.34f, 0.26f, 0.19f);
        private static readonly Color PaleTrim = new Color(0.78f, 0.76f, 0.70f);
        private static readonly Color Water = new Color(0.16f, 0.34f, 0.40f);
        private static readonly Color Deck = new Color(0.66f, 0.64f, 0.59f);
        private static readonly Color Steel = new Color(0.38f, 0.40f, 0.42f);
        private static readonly Color Rust = new Color(0.45f, 0.29f, 0.18f);
        private static readonly Color Brush = new Color(0.33f, 0.28f, 0.20f);

        /// <summary>
        /// Siding colours for houses, chosen from the prop's cell. Five is enough that a row of
        /// six houses does not repeat next to itself and few enough that the street still looks
        /// like one development, which is exactly what a gated community IS.
        /// </summary>
        private static readonly Color[] HouseSiding =
        {
            new Color(0.66f, 0.63f, 0.56f),   // cream clapboard
            new Color(0.45f, 0.47f, 0.44f),   // sage
            new Color(0.55f, 0.42f, 0.34f),   // clay
            new Color(0.40f, 0.44f, 0.49f),   // colonial blue-grey
            new Color(0.72f, 0.70f, 0.66f),   // white
        };

        private static readonly Color[] HouseRoof =
        {
            new Color(0.22f, 0.22f, 0.24f),
            new Color(0.29f, 0.25f, 0.23f),
            new Color(0.25f, 0.27f, 0.27f),
        };

        /// <summary>
        /// Kinds whose contact shadow is a lie rather than a help.
        ///
        /// <see cref="BlobShadows.RegisterProp(GameObject, float, float)"/> clamps the blob to 2.2
        /// metres, which is right for a bush and absurd under a sixteen-metre clubhouse: a small
        /// round smudge in the middle of a long building reads as a hovering disc. These props are
        /// GameObjects, the ink shader has a real ShadowCaster pass, and the overcast key light
        /// grounds them properly on its own.
        /// </summary>
        private static readonly HashSet<string> NoBlobShadow = new HashSet<string>(StringComparer.Ordinal)
        {
            "Clubhouse", "CommunityCentre", "PoolDeck", "House", "Bleachers",
        };

        public SiteProps(Transform root, Func<Color, Material> material)
        {
            _root = root;
            _material = material;
        }

        public int Placed { get; private set; }

        /// <summary>Builds one prop by name. Unknown kinds are the caller's problem, not ours.</summary>
        public void Build(string kind, float x, float z, float yaw)
        {
            var pivot = new GameObject($"Prop_{kind}").transform;
            pivot.SetParent(_root, false);
            pivot.position = new Vector3(x, 0f, z);
            pivot.rotation = Quaternion.Euler(0f, yaw, 0f);

            // One deterministic number per placed prop, so "which siding colour" and "which way is
            // the chimney" are stable across runs and a screenshot is reproducible. Taken from the
            // CELL, not from a counter, so inserting a prop earlier in the list does not repaint
            // the whole street.
            int seed = Hash(Mathf.FloorToInt(x), Mathf.FloorToInt(z));

            // A BOUGHT MODEL WHEN THERE IS ONE, our own boxes when there is not.
            //
            // This is the only line of the prop system that changes for bought art, and that is the
            // point: PropCatalog owns a prop's FOOTPRINT in grid cells and this class owns its
            // shape. Swapping the shape cannot touch the cells written into the one GridMap before
            // the first flow field, so hard rule 4 -- the preview can never lie -- holds by
            // construction rather than by care.
            if (TryModel(kind, pivot, seed))
            {
                // No VertexAo bake and no box-derived shadow key: both were written for geometry we
                // generated and whose box layout we knew. A bought model brings its own baked
                // lighting in its atlas.
                if (!NoBlobShadow.Contains(kind)) BlobShadows.RegisterProp(pivot.gameObject);
                Placed++;
                return;
            }

            switch (kind)
            {
                case "Pillar": Pillar(pivot); break;
                case "Guardhouse": Guardhouse(pivot); break;
                case "BoomBarrier": BoomBarrier(pivot); break;
                case "JerseyBarrier": JerseyBarrier(pivot); break;
                case "House": House(pivot, seed); break;
                case "Clubhouse": Clubhouse(pivot); break;
                case "CommunityCentre": CommunityCentre(pivot); break;
                case "PoolHouse": PoolHouse(pivot); break;
                case "PoolDeck": PoolDeck(pivot); break;
                case "Bleachers": Bleachers(pivot); break;
                case "BrushPile": BrushPile(pivot, seed); break;
                case "PalletStack": PalletStack(pivot, seed); break;
                case "Dumpster": Dumpster(pivot); break;
                case "StreetLamp": StreetLamp(pivot); break;
                case "PicnicTable": PicnicTable(pivot); break;
                case "Mailbox": Mailbox(pivot); break;
                default:
                    Discard(pivot.gameObject);
                    return;
            }

            // Corner and base darkening, baked into the colour channel now that the boxes exist and
            // their positions relative to each other are known. Cached by kind, so twelve jersey
            // barriers cost one bake.
            //
            // Houses are baked per VARIANT, not per kind: the bake caches the vertex colours it
            // computed for the first object it saw under a key, and every house sharing one key
            // would inherit the first house's occlusion regardless of which way its porch faces.
            VertexAo.Bake(pivot.gameObject, kind == "House" ? HouseAoKey(seed) : kind);

            // And a contact shadow, so the thing sits ON the ground instead of near it.
            if (!NoBlobShadow.Contains(kind)) BlobShadows.RegisterProp(pivot.gameObject);

            Placed++;
        }

        /// <summary>
        /// Instantiates a bought prop for this kind, when one has been built.
        ///
        /// Variants are chosen with the SAME per-cell seed that already decides siding colour and
        /// chimney side, so a street is mixed rather than a row of one repeated house, and the mix
        /// is stable: a screenshot taken twice is the same screenshot, and inserting a prop earlier
        /// in the scenario does not repaint the whole neighbourhood.
        ///
        /// Placed at NATIVE SCALE. Nothing here resizes the model to fit the grid -- the footprint
        /// in PropCatalog is authored to fit the model instead. See the House entry there.
        /// </summary>
        private bool TryModel(string kind, Transform pivot, int seed)
        {
            if (!_modelCache.TryGetValue(kind, out var variants))
            {
                var found = new List<GameObject>();
                for (int v = 0; v < 8; v++)
                {
                    var prefab = Resources.Load<GameObject>($"Props/{kind}_{v:00}");
                    if (prefab == null) break;
                    found.Add(prefab);
                }
                variants = found.Count > 0 ? found.ToArray() : System.Array.Empty<GameObject>();
                _modelCache[kind] = variants;

                Debug.Log(variants.Length > 0
                    ? $"[Props] {kind}: {variants.Length} bought variants"
                    : $"[Props] {kind}: no bought model, drawing boxes");
            }

            if (variants.Length == 0) return false;

            var go = UnityEngine.Object.Instantiate(variants[(seed >> 3) % variants.Length], pivot);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            return true;
        }

        /// <summary>Loaded once per kind per process: Resources.Load is not free at 57 props.</summary>
        private readonly Dictionary<string, GameObject[]> _modelCache = new Dictionary<string, GameObject[]>();

        /// <summary>
        /// The AO cache key for a house: its GEOMETRY variant, not its colour.
        ///
        /// The bake caches vertex colours under a key and reuses them for the next object with the
        /// same key and the same mesh count. Two houses differ in storey count (which changes the
        /// box count, so the cache notices) and in which end the chimney is on (which does NOT),
        /// so keying on the colour index would hand a left-chimney house the occlusion baked for a
        /// right-chimney one. Four keys, one per shape.
        /// </summary>
        private static string HouseAoKey(int seed) =>
            $"House{(((seed >> 6) & 3) != 0 ? 1 : 0)}{(seed >> 8) & 1}";

        /// <summary>A brick gate pillar with a cap. The thing a community gate is actually made of.</summary>
        private void Pillar(Transform parent)
        {
            Box(parent, "Shaft", new Vector3(0f, 1.55f, 0f), new Vector3(0.95f, 3.1f, 0.95f), Brick);
            Box(parent, "Cap", new Vector3(0f, 3.22f, 0f), new Vector3(1.25f, 0.24f, 1.25f), Concrete);
            // A lamp on top, unlit. Half this game is daylight and a dark bulb reads as "no power".
            Box(parent, "Lamp", new Vector3(0f, 3.55f, 0f), new Vector3(0.42f, 0.44f, 0.42f), Glass);
        }

        /// <summary>
        /// The gatehouse. Roof overhangs the walls, which is most of what makes a box read as a
        /// building rather than a crate, and a dark band at eye height reads as windows.
        /// </summary>
        private void Guardhouse(Transform parent)
        {
            Box(parent, "Walls", new Vector3(0f, 1.3f, 0f), new Vector3(3.2f, 2.6f, 2.8f), Siding);
            Box(parent, "Windows", new Vector3(0f, 1.85f, 0f), new Vector3(3.26f, 0.75f, 2.86f), Glass);
            Box(parent, "Roof", new Vector3(0f, 2.72f, 0f), new Vector3(3.9f, 0.26f, 3.5f), Roof);
            Box(parent, "Step", new Vector3(0f, 0.09f, 1.7f), new Vector3(1.4f, 0.18f, 0.7f), Concrete);
        }

        /// <summary>
        /// A boom barrier, raised. Striped by alternating short segments rather than by a texture,
        /// which costs nothing under flat shading and reads instantly.
        ///
        /// It is UP, and up is the whole point: somebody lifted it to let the last cars through and
        /// nobody ever put it down.
        /// </summary>
        private void BoomBarrier(Transform parent)
        {
            Box(parent, "Post", new Vector3(0f, 0.55f, 0f), new Vector3(0.34f, 1.1f, 0.34f), Concrete);

            var arm = new GameObject("Arm").transform;
            arm.SetParent(parent, false);
            arm.localPosition = new Vector3(0f, 1.05f, 0f);
            arm.localRotation = Quaternion.Euler(0f, 0f, 58f);   // raised

            const int segments = 6;
            const float segment = 1.05f;
            for (int i = 0; i < segments; i++)
            {
                Box(arm, $"Seg{i}",
                    new Vector3(segment * (i + 0.5f), 0f, 0f),
                    new Vector3(segment, 0.16f, 0.16f),
                    (i % 2 == 0) ? HazardWhite : HazardRed);
            }
        }

        /// <summary>A concrete barrier. Cheap, and it makes a road look closed rather than empty.</summary>
        private void JerseyBarrier(Transform parent)
        {
            Box(parent, "Base", new Vector3(0f, 0.17f, 0f), new Vector3(2.4f, 0.34f, 0.72f), Concrete);
            Box(parent, "Top", new Vector3(0f, 0.56f, 0f), new Vector3(2.4f, 0.46f, 0.38f), Concrete);
        }

        // ------------------------------------------------------------------------------------
        // The community.
        //
        // Every one of these is authored with its long axis along local X and its front toward
        // local +Z, so a scenario author aims a building the same way they aim a boom barrier:
        // yaw 0 faces +Y in the sim, which is +Z in Unity. PropCatalog's footprints follow the
        // same convention, so the cells and the boxes turn together.
        // ------------------------------------------------------------------------------------

        /// <summary>
        /// A house. Six cells by five, gable roof, porch on the front, chimney on one end.
        ///
        /// The porch and the chimney are what stop a row of these reading as storage units: the
        /// porch gives the facade depth and the chimney breaks the symmetry, and neither costs more
        /// than three boxes.
        /// </summary>
        private void House(Transform parent, int seed)
        {
            Color siding = HouseSiding[seed % HouseSiding.Length];
            Color roof = HouseRoof[(seed >> 3) % HouseRoof.Length];
            bool twoStorey = ((seed >> 6) & 3) != 0;          // most of them are
            float h = twoStorey ? 4.2f : 3.0f;
            float depth = 4.6f;

            Box(parent, "Walls", new Vector3(0f, h * 0.5f, 0f), new Vector3(5.6f, h, depth), siding);

            // Window bands: one per storey, proud of the siding so the ink line catches them.
            Box(parent, "Windows0", new Vector3(0f, 1.55f, 0f), new Vector3(5.66f, 0.85f, depth + 0.06f), Glass);
            if (twoStorey)
                Box(parent, "Windows1", new Vector3(0f, 3.25f, 0f), new Vector3(5.66f, 0.8f, depth + 0.06f), Glass);

            Gable(parent, roof, halfSpan: depth * 0.5f, eaveY: h, length: 6.4f, pitch: 34f, overhang: 0.55f);

            // Porch: a slab, a roof and two posts. Toward +Z, which is the front.
            float front = depth * 0.5f;
            Box(parent, "PorchDeck", new Vector3(0f, 0.11f, front + 0.75f), new Vector3(4.2f, 0.22f, 1.5f), Timber);
            Box(parent, "PorchRoof", new Vector3(0f, 2.55f, front + 0.8f), new Vector3(4.4f, 0.18f, 1.8f), roof);
            Box(parent, "PorchPostL", new Vector3(-1.9f, 1.35f, front + 1.4f), new Vector3(0.16f, 2.4f, 0.16f), PaleTrim);
            Box(parent, "PorchPostR", new Vector3(1.9f, 1.35f, front + 1.4f), new Vector3(0.16f, 2.4f, 0.16f), PaleTrim);
            Box(parent, "Door", new Vector3(0f, 1.05f, front + 0.04f), new Vector3(0.95f, 2.1f, 0.12f), Timber);

            // A driveway out the front, off-centre from the door. Half of what makes a row of
            // boxes read as a street rather than as storage units is that each one has a strip of
            // concrete pointing at the road. Given real thickness and a top at the surface lift,
            // because a zero-height slab on the ground plane z-fights from the overhead camera.
            Box(parent, "Drive", new Vector3(0.9f, -0.13f, front + 3.4f), new Vector3(2.9f, 0.3f, 5.4f), Concrete);

            // Chimney on whichever end the seed picks, running up past the ridge.
            float side = ((seed >> 8) & 1) == 0 ? -2.4f : 2.4f;
            Box(parent, "Chimney", new Vector3(side, h * 0.5f + 1.55f, -0.9f),
                new Vector3(0.8f, h + 3.1f, 0.8f), Brick);
        }

        /// <summary>
        /// The clubhouse: the social centre of a place like this, and on this map the biggest single
        /// obstruction the crowd has to walk around.
        ///
        /// Long and low with a deep colonnaded porch facing the road, a continuous window band and a
        /// flagpole. It is the one building the player will use as a landmark, so it gets the
        /// silhouette budget.
        /// </summary>
        private void Clubhouse(Transform parent)
        {
            const float w = 15.4f, d = 9.0f, h = 4.0f;
            Box(parent, "Walls", new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, d), Brick);
            // A pale band along the base: brick to the sill, painted below the eaves. Two colours
            // on one volume is the cheapest way to say "this was built, not extruded".
            Box(parent, "Fascia", new Vector3(0f, h - 0.45f, 0f), new Vector3(w + 0.08f, 0.9f, d + 0.08f), PaleTrim);
            Box(parent, "Windows", new Vector3(0f, 2.15f, 0f), new Vector3(w + 0.1f, 1.5f, d + 0.1f), Glass);

            Gable(parent, Roof, halfSpan: d * 0.5f, eaveY: h, length: w + 1.4f, pitch: 26f, overhang: 0.9f);

            // Porch across the front, on five posts.
            float front = d * 0.5f;
            Box(parent, "PorchDeck", new Vector3(0f, 0.13f, front + 1.5f), new Vector3(w - 1.0f, 0.26f, 3.0f), Concrete);
            Box(parent, "PorchRoof", new Vector3(0f, 3.5f, front + 1.6f), new Vector3(w - 0.6f, 0.22f, 3.4f), Roof);
            for (int i = 0; i < 5; i++)
            {
                float px = -5.6f + i * 2.8f;
                Box(parent, $"Post{i}", new Vector3(px, 1.8f, front + 2.8f), new Vector3(0.24f, 3.4f, 0.24f), PaleTrim);
            }
            Box(parent, "Doors", new Vector3(0f, 1.2f, front + 0.05f), new Vector3(2.4f, 2.4f, 0.14f), Glass);
            Box(parent, "Step", new Vector3(0f, 0.06f, front + 3.3f), new Vector3(4.0f, 0.12f, 1.0f), Concrete);

            // Flagpole, bare. Nobody has taken the flag down and nobody has put one up.
            Box(parent, "Flagpole", new Vector3(-8.6f, 3.6f, front + 2.2f), new Vector3(0.13f, 7.2f, 0.13f), PaleTrim);

            // Two air handlers on the roof ridge line, offset, because a perfectly clean roof is
            // the tell that nobody dressed it.
            Box(parent, "HVAC0", new Vector3(3.2f, h + 0.5f, -2.2f), new Vector3(1.6f, 1.0f, 1.4f), Steel);
            Box(parent, "HVAC1", new Vector3(-1.4f, h + 0.4f, -2.6f), new Vector3(1.2f, 0.8f, 1.2f), Steel);
        }

        /// <summary>
        /// The rec centre: a gym hall with a lower entrance wing. An L, not a cuboid.
        ///
        /// Flat roof with a parapet and a clerestory band near the top, which is what a municipal
        /// sports hall looks like everywhere in the world and reads instantly as "not a house".
        /// </summary>
        private void CommunityCentre(Transform parent)
        {
            const float w = 13.6f, d = 7.6f, h = 6.4f;
            Box(parent, "Hall", new Vector3(0f, h * 0.5f, -1.0f), new Vector3(w, h, d), Concrete);
            Box(parent, "Clerestory", new Vector3(0f, h - 1.1f, -1.0f), new Vector3(w + 0.08f, 1.1f, d + 0.08f), Glass);
            // Parapet: the roof slab is wider than the walls, and the lip around it is FOUR BARS,
            // not a slab. A flat-roofed building capped with a solid parapet photographs from the
            // tactical camera as a blank concrete rectangle -- the whole roof is hidden under its
            // own lip. With a frame you see the dark roof, the plant on it, and the pale edge.
            Box(parent, "RoofSlab", new Vector3(0f, h + 0.12f, -1.0f), new Vector3(w + 0.5f, 0.24f, d + 0.5f), Roof);
            float px = (w + 0.5f) * 0.5f, pz = (d + 0.5f) * 0.5f;
            Box(parent, "ParapetN", new Vector3(0f, h + 0.42f, -1.0f + pz), new Vector3(w + 0.5f, 0.36f, 0.35f), Concrete);
            Box(parent, "ParapetS", new Vector3(0f, h + 0.42f, -1.0f - pz), new Vector3(w + 0.5f, 0.36f, 0.35f), Concrete);
            Box(parent, "ParapetE", new Vector3(px, h + 0.42f, -1.0f), new Vector3(0.35f, 0.36f, d + 0.5f), Concrete);
            Box(parent, "ParapetW", new Vector3(-px, h + 0.42f, -1.0f), new Vector3(0.35f, 0.36f, d + 0.5f), Concrete);

            // Plant on the roof, off-centre. A sports hall is all roof from above and an empty one
            // is the flattest object on the map.
            Box(parent, "RoofUnit0", new Vector3(-3.4f, h + 0.75f, -2.2f), new Vector3(2.6f, 1.3f, 2.0f), Steel);
            Box(parent, "RoofUnit1", new Vector3(1.8f, h + 0.62f, 0.6f), new Vector3(1.8f, 1.0f, 1.6f), Steel);
            Box(parent, "RoofVent", new Vector3(4.6f, h + 0.52f, -3.0f), new Vector3(0.9f, 0.8f, 0.9f), Rust);
            // Pilasters: vertical ribs down the long wall. Three boxes that turn a blank concrete
            // slab into a facade.
            for (int i = 0; i < 5; i++)
                Box(parent, $"Rib{i}", new Vector3(-5.6f + i * 2.8f, h * 0.5f, -1.0f - d * 0.5f - 0.12f),
                    new Vector3(0.5f, h, 0.3f), PaleTrim);

            // Entrance wing, lower, pushed out toward the front.
            Box(parent, "Wing", new Vector3(0f, 1.7f, 4.6f), new Vector3(7.8f, 3.4f, 4.0f), Brick);
            Box(parent, "WingRoof", new Vector3(0f, 3.5f, 4.6f), new Vector3(8.4f, 0.24f, 4.6f), Roof);
            Box(parent, "WingGlass", new Vector3(0f, 1.85f, 4.6f), new Vector3(7.86f, 1.6f, 4.06f), Glass);
            Box(parent, "Canopy", new Vector3(0f, 3.0f, 7.2f), new Vector3(5.0f, 0.16f, 2.0f), Steel);
            Box(parent, "CanopyPostL", new Vector3(-2.1f, 1.5f, 7.9f), new Vector3(0.14f, 3.0f, 0.14f), Steel);
            Box(parent, "CanopyPostR", new Vector3(2.1f, 1.5f, 7.9f), new Vector3(0.14f, 3.0f, 0.14f), Steel);
        }

        /// <summary>Pool changing rooms. Low, flat-roofed, a door at each end.</summary>
        private void PoolHouse(Transform parent)
        {
            Box(parent, "Walls", new Vector3(0f, 1.45f, 0f), new Vector3(6.6f, 2.9f, 4.4f), PaleTrim);
            Box(parent, "Band", new Vector3(0f, 2.3f, 0f), new Vector3(6.66f, 0.5f, 4.46f), Glass);
            Box(parent, "Roof", new Vector3(0f, 3.03f, 0f), new Vector3(7.4f, 0.26f, 5.2f), Roof);
            Box(parent, "DoorL", new Vector3(-1.8f, 1.05f, 2.25f), new Vector3(0.9f, 2.1f, 0.14f), Timber);
            Box(parent, "DoorR", new Vector3(1.8f, 1.05f, 2.25f), new Vector3(0.9f, 2.1f, 0.14f), Timber);
            Box(parent, "Step", new Vector3(0f, 0.07f, 2.9f), new Vector3(6.0f, 0.14f, 1.2f), Concrete);
        }

        /// <summary>
        /// The pool itself: concrete deck, coping, water, and the furniture nobody put away.
        ///
        /// The water is a shade of the sky rather than swimming-pool cyan, because under an
        /// overcast key a bright blue rectangle is the one thing on the map that will look like a
        /// UI element that fell out of the HUD.
        /// </summary>
        private void PoolDeck(Transform parent)
        {
            // Deck: sits a hair above the ground plane, as all flat surfaces here must.
            Box(parent, "Deck", new Vector3(0f, 0.03f, 0f), new Vector3(13.6f, 0.06f, 8.6f), Deck);

            // The water, then a coping lip AS A FRAME around it.
            //
            // The first version made the coping a single slab a little larger than the water and a
            // little taller, which is what a coping is -- and which therefore covered the water
            // completely. The pool photographed as a pale rectangle on a pale deck. A lip is four
            // bars with a hole in the middle; anything that surrounds something has to be built
            // that way or it is a lid.
            Box(parent, "Water", new Vector3(0f, 0.05f, 0f), new Vector3(9.6f, 0.10f, 5.6f), Water);
            Box(parent, "CopingN", new Vector3(0f, 0.09f, 3.0f), new Vector3(10.4f, 0.18f, 0.4f), PaleTrim);
            Box(parent, "CopingS", new Vector3(0f, 0.09f, -3.0f), new Vector3(10.4f, 0.18f, 0.4f), PaleTrim);
            Box(parent, "CopingE", new Vector3(5.0f, 0.09f, 0f), new Vector3(0.4f, 0.18f, 6.4f), PaleTrim);
            Box(parent, "CopingW", new Vector3(-5.0f, 0.09f, 0f), new Vector3(0.4f, 0.18f, 6.4f), PaleTrim);

            // Loungers, shoved around. Three boxes each; the value is entirely in them not being
            // in a row.
            Lounger(parent, "Lounger0", new Vector3(-5.6f, 0f, 2.6f), 8f);
            Lounger(parent, "Lounger1", new Vector3(-5.4f, 0f, 0.4f), -14f);
            Lounger(parent, "Lounger2", new Vector3(5.5f, 0f, -2.2f), 172f);

            // Lifeguard chair, which is the tallest thing inside the fence and therefore the thing
            // that says "pool" from across the map.
            var chair = new GameObject("LifeguardChair").transform;
            chair.SetParent(parent, false);
            chair.localPosition = new Vector3(6.0f, 0f, 1.6f);
            Box(chair, "LegL", new Vector3(-0.5f, 1.0f, 0f), new Vector3(0.12f, 2.0f, 0.12f), PaleTrim);
            Box(chair, "LegR", new Vector3(0.5f, 1.0f, 0f), new Vector3(0.12f, 2.0f, 0.12f), PaleTrim);
            Box(chair, "Seat", new Vector3(0f, 2.05f, 0f), new Vector3(1.3f, 0.14f, 0.9f), PaleTrim);
            Box(chair, "Back", new Vector3(0f, 2.55f, -0.42f), new Vector3(1.3f, 1.0f, 0.12f), PaleTrim);
        }

        private void Lounger(Transform parent, string name, Vector3 at, float yaw)
        {
            var g = new GameObject(name).transform;
            g.SetParent(parent, false);
            g.localPosition = at;
            g.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Box(g, "Frame", new Vector3(0f, 0.32f, 0f), new Vector3(0.7f, 0.1f, 1.9f), PaleTrim);
            Box(g, "Back", new Vector3(0f, 0.55f, -0.75f), new Vector3(0.7f, 0.56f, 0.1f), PaleTrim,
                Quaternion.Euler(28f, 0f, 0f));
            Box(g, "LegF", new Vector3(0f, 0.14f, 0.7f), new Vector3(0.6f, 0.28f, 0.08f), Steel);
            Box(g, "LegB", new Vector3(0f, 0.14f, -0.6f), new Vector3(0.6f, 0.28f, 0.08f), Steel);
        }

        /// <summary>Four rows of bleachers beside the pool. Stepped, so the silhouette is a wedge.</summary>
        private void Bleachers(Transform parent)
        {
            for (int i = 0; i < 4; i++)
            {
                float y = 0.32f + i * 0.42f;
                float z = -1.0f + i * 0.62f;
                Box(parent, $"Riser{i}", new Vector3(0f, y * 0.5f, z), new Vector3(7.6f, y, 0.6f), Concrete);
                Box(parent, $"Plank{i}", new Vector3(0f, y + 0.05f, z), new Vector3(7.7f, 0.1f, 0.66f), Timber);
            }
            Box(parent, "RailL", new Vector3(-3.85f, 1.3f, 0.0f), new Vector3(0.1f, 1.1f, 3.0f), Steel);
            Box(parent, "RailR", new Vector3(3.85f, 1.3f, 0.0f), new Vector3(0.1f, 1.1f, 3.0f), Steel);
        }

        // ------------------------------------------------------------------------------------
        // Improvised blockade.
        //
        // Owner, on the pre-placed log walls: "The stacks of logs look very unrealistic if we have
        // a different model that's like stacks of branches or makeshift event blockading". These
        // are the makeshift end of that: things the residents dragged across a road in an
        // afternoon, none of which is a neat stack of anything.
        // ------------------------------------------------------------------------------------

        /// <summary>
        /// A tangle of cut brush and branches. Every limb is a separate thin bar at its own angle,
        /// pulled from the prop's hash, because the failure mode of the thing this replaces is that
        /// it is TIDY, and nothing anybody dragged across a road in a hurry is tidy.
        /// </summary>
        private void BrushPile(Transform parent, int seed)
        {
            const int limbs = 16;
            for (int i = 0; i < limbs; i++)
            {
                int h = Hash(seed, i);
                float len = 1.5f + ((h >> 2) & 7) * 0.22f;
                float thick = 0.10f + ((h >> 5) & 3) * 0.035f;
                float lift = 0.12f + ((h >> 7) & 7) * 0.16f;
                float yaw = ((h >> 10) & 127) * 2.83f;
                float pitch = (((h >> 17) & 31) - 15.5f) * 1.9f;
                float ox = (((h >> 22) & 15) - 7.5f) * 0.16f;
                float oz = (((h >> 12) & 15) - 7.5f) * 0.12f;

                Box(parent, $"Limb{i}", new Vector3(ox, lift, oz),
                    new Vector3(len, thick, thick), Brush,
                    Quaternion.Euler(0f, yaw, pitch));
            }
            // A couple of whole saplings laid on top, longer than everything else, which is what
            // gives the pile a scale.
            Box(parent, "Sapling0", new Vector3(0.2f, 0.95f, -0.1f), new Vector3(3.4f, 0.16f, 0.16f), Timber,
                Quaternion.Euler(0f, 14f, 6f));
            Box(parent, "Sapling1", new Vector3(-0.3f, 1.05f, 0.3f), new Vector3(3.0f, 0.14f, 0.14f), Timber,
                Quaternion.Euler(0f, -22f, -9f));
        }

        /// <summary>
        /// Shipping pallets off the back of somebody's truck, stacked and leaning. Slatted, because
        /// a pallet that is a solid block is a crate and the gaps are the whole read.
        /// </summary>
        private void PalletStack(Transform parent, int seed)
        {
            int count = 4 + (seed & 3);
            for (int i = 0; i < count; i++)
            {
                int h = Hash(seed, i * 7);
                var stack = new GameObject($"Pallet{i}").transform;
                stack.SetParent(parent, false);
                stack.localPosition = new Vector3(
                    (((h >> 3) & 7) - 3.5f) * 0.045f, 0.09f + i * 0.16f,
                    (((h >> 6) & 7) - 3.5f) * 0.045f);
                stack.localRotation = Quaternion.Euler(0f, ((h >> 9) & 15) * 1.6f - 12f, 0f);

                Box(stack, "Deck", Vector3.zero, new Vector3(1.2f, 0.05f, 1.0f), Timber);
                for (int s = 0; s < 3; s++)
                    Box(stack, $"Bearer{s}", new Vector3(-0.5f + s * 0.5f, -0.06f, 0f),
                        new Vector3(0.12f, 0.09f, 1.0f), Timber);
            }
            // One leaning against the stack, which stops it reading as a single extruded object.
            Box(parent, "Leaner", new Vector3(0.85f, 0.55f, 0.2f), new Vector3(1.2f, 0.06f, 1.0f), Timber,
                Quaternion.Euler(0f, 22f, 68f));
        }

        /// <summary>A wheeled skip, shoved sideways across a gap. Lid half up.</summary>
        private void Dumpster(Transform parent)
        {
            Box(parent, "Body", new Vector3(0f, 0.62f, 0f), new Vector3(2.6f, 1.24f, 1.5f), Rust);
            Box(parent, "Rim", new Vector3(0f, 1.26f, 0f), new Vector3(2.7f, 0.12f, 1.6f), Steel);
            Box(parent, "Lid", new Vector3(0f, 1.55f, -0.65f), new Vector3(2.6f, 0.1f, 1.5f), Steel,
                Quaternion.Euler(-58f, 0f, 0f));
            Box(parent, "WheelL", new Vector3(-1.05f, 0.13f, 0.58f), new Vector3(0.22f, 0.26f, 0.26f), Steel);
            Box(parent, "WheelR", new Vector3(1.05f, 0.13f, 0.58f), new Vector3(0.22f, 0.26f, 0.26f), Steel);
            // Spill: two bags on the ground beside it.
            Box(parent, "Bag0", new Vector3(1.6f, 0.22f, 0.3f), new Vector3(0.7f, 0.44f, 0.6f), Steel,
                Quaternion.Euler(0f, 20f, 0f));
            Box(parent, "Bag1", new Vector3(1.35f, 0.18f, -0.5f), new Vector3(0.6f, 0.36f, 0.5f), Steel);
        }

        /// <summary>
        /// A street lamp. No footprint, no collision -- it is the one prop here that is purely a
        /// mark on the map, and it exists because a road with nothing along it is a runway.
        /// </summary>
        private void StreetLamp(Transform parent)
        {
            Box(parent, "Base", new Vector3(0f, 0.18f, 0f), new Vector3(0.42f, 0.36f, 0.42f), Concrete);
            Box(parent, "Post", new Vector3(0f, 2.7f, 0f), new Vector3(0.16f, 5.4f, 0.16f), Steel);
            Box(parent, "Arm", new Vector3(0.62f, 5.3f, 0f), new Vector3(1.3f, 0.12f, 0.12f), Steel);
            Box(parent, "Head", new Vector3(1.2f, 5.16f, 0f), new Vector3(0.7f, 0.22f, 0.34f), Glass);
        }

        /// <summary>A picnic table on the common ground. Purely a mark; nothing walks round it.</summary>
        private void PicnicTable(Transform parent)
        {
            Box(parent, "Top", new Vector3(0f, 0.74f, 0f), new Vector3(2.0f, 0.08f, 0.9f), Timber);
            Box(parent, "BenchL", new Vector3(0f, 0.44f, -0.72f), new Vector3(2.0f, 0.07f, 0.32f), Timber);
            Box(parent, "BenchR", new Vector3(0f, 0.44f, 0.72f), new Vector3(2.0f, 0.07f, 0.32f), Timber);
            Box(parent, "LegL", new Vector3(-0.8f, 0.37f, 0f), new Vector3(0.1f, 0.74f, 1.7f), Timber,
                Quaternion.Euler(0f, 0f, 0f));
            Box(parent, "LegR", new Vector3(0.8f, 0.37f, 0f), new Vector3(0.1f, 0.74f, 1.7f), Timber);
        }

        /// <summary>A mailbox on a post at the end of a drive. The smallest "somebody lived here".</summary>
        private void Mailbox(Transform parent)
        {
            Box(parent, "Post", new Vector3(0f, 0.55f, 0f), new Vector3(0.1f, 1.1f, 0.1f), Timber);
            Box(parent, "Box", new Vector3(0f, 1.18f, 0.1f), new Vector3(0.24f, 0.24f, 0.46f), Steel);
            Box(parent, "Flag", new Vector3(0.15f, 1.3f, -0.05f), new Vector3(0.05f, 0.28f, 0.12f), HazardRed);
        }

        /// <summary>
        /// A pitched roof: two slabs meeting at a ridge, overhanging the eaves.
        ///
        /// This is the single highest-value shape in the file. A flat-topped box is a container; the
        /// same box with a ridge on it is a building, at any distance and from any angle, and the
        /// ink outline traces the pitch which is exactly where the comic look wants a line.
        /// </summary>
        private void Gable(Transform parent, Color colour, float halfSpan, float eaveY,
                           float length, float pitch, float overhang)
        {
            float rad = pitch * Mathf.Deg2Rad;
            float slope = halfSpan / Mathf.Cos(rad) + overhang;   // slab length down the slope
            float rise = halfSpan * Mathf.Tan(rad);

            // Centre of each slab: half way down the slope from the ridge, pushed out by the
            // overhang so the eave hangs proud of the wall rather than sitting flush on it.
            float cz = Mathf.Cos(rad) * slope * 0.5f;
            float cy = eaveY + rise - Mathf.Sin(rad) * slope * 0.5f;

            Box(parent, "RoofFront", new Vector3(0f, cy, cz), new Vector3(length, 0.22f, slope), colour,
                Quaternion.Euler(pitch, 0f, 0f));
            Box(parent, "RoofBack", new Vector3(0f, cy, -cz), new Vector3(length, 0.22f, slope), colour,
                Quaternion.Euler(-pitch, 0f, 0f));
            // The gable ends, filling the triangle the two slabs leave open. A wedge is not worth a
            // custom mesh; a thin slab standing on the ridge line closes it from every angle that
            // matters.
            Box(parent, "GableEndL", new Vector3(-length * 0.5f + 0.35f, eaveY + rise * 0.5f, 0f),
                new Vector3(0.25f, rise, halfSpan * 1.2f), colour);
            Box(parent, "GableEndR", new Vector3(length * 0.5f - 0.35f, eaveY + rise * 0.5f, 0f),
                new Vector3(0.25f, rise, halfSpan * 1.2f), colour);
        }

        private void Box(Transform parent, string name, Vector3 localPosition, Vector3 size, Color colour)
            => Box(parent, name, localPosition, size, colour, Quaternion.identity);

        private void Box(Transform parent, string name, Vector3 localPosition, Vector3 size, Color colour,
                         Quaternion localRotation)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            // No colliders anywhere in here. These are scenery; the grid owns what is solid.
            Discard(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = localRotation;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = _material(colour);
        }

        /// <summary>
        /// Throws an object away, in play mode or in the editor.
        ///
        /// <c>Object.Destroy</c> is deferred to the end of the frame and logs an error outside play
        /// mode, so a plain Destroy here makes the whole class untestable in EditMode -- and the
        /// one test worth having is "every kind in the catalogue actually builds geometry", which
        /// has to construct one of each.
        /// </summary>
        private static void Discard(UnityEngine.Object? o)
        {
            if (o == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(o);
            else UnityEngine.Object.DestroyImmediate(o);
        }

        /// <summary>Cheap deterministic hash, so a prop dresses itself the same way every run.</summary>
        private static int Hash(int a, int b)
        {
            unchecked
            {
                int h = (a * 73856093) ^ (b * 19349663);
                h ^= h >> 13;
                h *= 1274126177;
                h ^= h >> 16;
                return h & 0x7FFFFFFF;
            }
        }
    }
}
