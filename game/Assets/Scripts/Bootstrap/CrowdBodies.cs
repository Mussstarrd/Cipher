#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Cipher.Game
{
    /// <summary>
    /// Which body a slot of each class is built from, and the small amount of dressing that turns a
    /// stock civilian into a named archetype.
    ///
    /// This exists so the bootstrap's pool loop stays a loop. Everything class-specific -- picking
    /// a model, choosing which machine chassis, putting a man in hi-vis -- is here, and the
    /// bootstrap asks for a slot and gets one.
    /// </summary>
    public static class CrowdBodies
    {
        /// <summary>
        /// The Sapper's model. ADR-003: "The Sapper is a person with a toolbox who used to be a
        /// contractor", and the free pack happens to ship exactly that man.
        ///
        /// HE IS TAKEN OUT OF THE ORDINARY CROWD (<see cref="SplitPool"/>). A silhouette only means
        /// something if it is not also worn by forty people who are not breachers -- the whole
        /// point is that when a player sees this shape, he knows what it is here to do.
        /// </summary>
        public const string SapperModelName = "Worker_Civilian";

        /// <summary>
        /// Hi-vis orange. Lifted from the capsule the Sapper used to be, (1, 0.45, 0.05), because
        /// a player has spent five waves learning that orange means "that one is going through
        /// your wall". The shape is new; the colour must not be.
        /// </summary>
        public static readonly Color HiVis = new Color(1f, 0.45f, 0.05f);

        /// <summary>How far a Sapper's clothing is pulled toward hi-vis. Not all the way.</summary>
        public const float HiVisStrength = 0.88f;

        /// <summary>
        /// Splits the loaded civilian prefabs into the ordinary crowd and the Sapper's model,
        /// leaving the hero's out of both.
        /// </summary>
        public static void SplitPool(IReadOnlyList<GameObject> prefabs, string heroModelName,
                                     out List<GameObject> signed, out GameObject? sapper)
        {
            signed = new List<GameObject>();
            sapper = null;
            for (int i = 0; i < prefabs.Count; i++)
            {
                var p = prefabs[i];
                if (p == null || p.name == heroModelName) continue;
                if (p.name == SapperModelName) { sapper = p; continue; }
                signed.Add(p);
            }
            // If the worker model ever goes missing, the crowd is better off with a plain civilian
            // in hi-vis than with no breacher body at all -- the colour still carries it.
            if (sapper == null && signed.Count > 0) sapper = signed[0];
            if (signed.Count == 0 && sapper != null) signed.Add(sapper);
        }

        /// <summary>The model a slot of this class is built from.</summary>
        public static GameObject? TemplateFor(BodyClass bodyClass, int index,
                                              IReadOnlyList<GameObject> signed, GameObject? sapper)
        {
            if (bodyClass == BodyClass.Sapper) return sapper;
            return signed.Count > 0 ? signed[index % signed.Count] : null;
        }

        /// <summary>
        /// Which chassis a machine slot gets. The three service kinds are dealt round-robin so a
        /// crowd is never all delivery walkers, and every Spitter is a sprayer.
        /// </summary>
        public static MachineKind KindFor(BodyClass bodyClass, int index)
        {
            if (bodyClass == BodyClass.Spitter) return MachineKind.Sprayer;
            return (MachineKind)(index % 3);
        }

        /// <summary>
        /// Builds a machine slot: the slot the crowd drives, with a machine under it and a gait to
        /// run it. Same three-level shape as a civilian for the same reason -- the thing that is
        /// animated and the thing that is positioned must not be the same transform.
        /// </summary>
        public static Transform BuildMachine(BodyClass bodyClass, int index, Transform parent,
                                             MachineFactory factory)
        {
            var slot = new GameObject(bodyClass == BodyClass.Spitter ? "Spitter" : "Humanoid");
            slot.transform.SetParent(parent, false);

            var machine = factory.Create(KindFor(bodyClass, index), slot.transform);
            machine.transform.localPosition = Vector3.zero;
            machine.AddComponent<MachineGait>();

            slot.SetActive(false);
            return slot.transform;
        }

        /// <summary>
        /// Puts a civilian in hi-vis.
        ///
        /// A PULL TOWARD ORANGE RATHER THAN A REPAINT. Flat orange would throw away the model's own
        /// light and shade and leave a traffic cone; <see cref="Tint"/> keeps the dark boots and the
        /// hair and reads, correctly, as a man in full hi-vis coveralls at ten metres and as an
        /// orange man at sixty. The bootstrap has already minted a fresh Material for every submesh
        /// of every slot, so this mutates nothing shared.
        /// </summary>
        public static void DressSapper(Transform slot)
        {
            if (slot == null) return;
            var renderers = slot.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r == null) continue;
                var mats = r.sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                {
                    var mat = mats[m];
                    if (mat == null) continue;
                    var tinted = Tint(mat.color);
                    mat.color = tinted;
                    if (mat.HasProperty(BaseColorId)) mat.SetColor(BaseColorId, tinted);
                }
            }
        }

        /// <summary>
        /// The hi-vis pull, as arithmetic, so it can be tested without a renderer.
        ///
        /// Weighted by how DARK the source is: boots, a belt and hair stay dark, because a man
        /// who is uniformly orange from sole to scalp reads as a bug rather than as workwear.
        /// </summary>
        public static Color Tint(Color source)
        {
            // The weighting SATURATES EARLY on purpose. A gentle ramp spread the pull across the
            // whole model and produced a faintly warm man rather than a man in hi-vis -- the first
            // render put four Sappers in a live wave and not one of them read as orange, which is
            // the entire job. Anything above about a third luminance is clothing and goes fully
            // over; only genuinely dark things -- boots, a belt, hair -- stay where they are.
            float luminance = source.r * 0.299f + source.g * 0.587f + source.b * 0.114f;
            float strength = HiVisStrength * Mathf.Clamp01(luminance * 3f);
            return Color.Lerp(source, HiVis, strength);
        }

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    }
}
