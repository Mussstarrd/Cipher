#nullable enable
using System;
using UnityEngine;

namespace Cipher.Game
{
    /// <summary>
    /// The built things that make a position a PLACE rather than an arena: gate pillars, a
    /// guardhouse, a boom barrier.
    ///
    /// These are made out of boxes, on purpose. The free kits have vegetation, vehicles and street
    /// furniture but no buildings, and the alternative to boxes is either a purchase or nothing.
    /// Under the ink shader a box is not a placeholder: flat banded colour and a hard contour line
    /// is what the whole art direction is, and a guardhouse that is six boxes with a roof overhang
    /// and a window band reads as a guardhouse from forty metres. The thing that would look cheap
    /// is a photoreal model next to them, which is the direction we are not going.
    ///
    /// They are DECORATION. Nothing here touches the grid, blocks pathing or takes damage: the
    /// walls do that, and a prop that silently changed a route would break the preview covenant.
    /// Placement keeps them clear of the gaps the horde walks through.
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

            switch (kind)
            {
                case "Pillar": Pillar(pivot); break;
                case "Guardhouse": Guardhouse(pivot); break;
                case "BoomBarrier": BoomBarrier(pivot); break;
                case "JerseyBarrier": JerseyBarrier(pivot); break;
                default:
                    UnityEngine.Object.Destroy(pivot.gameObject);
                    return;
            }

            Placed++;
        }

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

        private void Box(Transform parent, string name, Vector3 localPosition, Vector3 size, Color colour)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            // No colliders anywhere in here. These are scenery; the grid owns what is solid.
            UnityEngine.Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = _material(colour);
        }
    }
}
