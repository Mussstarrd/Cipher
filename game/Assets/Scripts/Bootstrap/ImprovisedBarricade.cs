#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Cipher.Game
{
    /// <summary>
    /// What a map wall looks like: brush, branches and whatever else got dragged across a road in
    /// an afternoon.
    ///
    /// REPLACES A NEAT STACK OF LOGS, and the owner is the one who called it: "The stacks of logs
    /// look very unrealistic if we have a different model that's like stacks of branches or
    /// makeshift event blockading or something". He is right, and the diagnosis is worth writing
    /// down because it generalises -- the old bake was four courses of near-identical cylinders all
    /// lying along the run, nested like masonry. That is a WOODPILE. A woodpile is a thing somebody
    /// built carefully over a weekend with a chainsaw and a truck, and every cell of it looked
    /// identical to the one beside it, so a sixteen-metre run read as extruded fencing.
    ///
    /// A barricade people threw together in a hurry has the opposite properties, and they are what
    /// this bakes:
    ///   - NO PREVAILING DIRECTION. Limbs cross the run as often as they lie along it.
    ///   - NO LEVEL COURSES. Heights are sampled, not stacked, so the top edge is ragged.
    ///   - A SIZE RANGE, not a size. Two or three heavy trunks carry the pile and a dozen thin
    ///     branches fill it, which is what makes the silhouette read as brush rather than as timber.
    ///   - CELL-TO-CELL VARIATION. Every number comes from a hash of the cell, so neighbours differ
    ///     and the same wall still looks the same on every run, which a screenshot depends on.
    ///
    /// Cylinders only, into the same instanced buffer the log pile used, because that buffer is
    /// drawn with the cylinder mesh and one shared material and this has no business adding either.
    /// </summary>
    public static class ImprovisedBarricade
    {
        /// <summary>Heavy trunks: the two or three things actually holding the pile up.</summary>
        private const int Trunks = 3;

        /// <summary>Thin stuff piled over and through them.</summary>
        private const int Branches = 11;

        /// <summary>
        /// Bakes one cell of barricade.
        ///
        /// Signature matches the log pile it replaces so the call site is a one-word change.
        /// <paramref name="run"/> is which way the wall travels; it is used as a WEAK bias -- limbs
        /// lean toward it rather than lying on it -- because a barricade that ignores the run
        /// entirely stops reading as a line and one that obeys it reads as a fence.
        /// </summary>
        public static void Bake(Vector3 centre, Quaternion run, int x, int y, List<Matrix4x4> into)
        {
            // Unity's cylinder is two units tall on Y, so a limb is built along Y and tipped over.
            // Scale y is HALF the limb's length.
            for (int i = 0; i < Trunks; i++)
            {
                int h = Hash(x, y, i);
                float length = 1.15f + ((h >> 2) & 7) * 0.055f;
                float radius = 0.15f + ((h >> 5) & 3) * 0.018f;
                float lift = 0.16f + ((h >> 7) & 3) * 0.13f;
                // Trunks lean within about 40 degrees of the run: enough that the wall still reads
                // as a line from the air, nowhere near enough to look laid.
                float skew = (((h >> 9) & 31) - 15.5f) * 2.6f;
                float roll = (((h >> 14) & 15) - 7.5f) * 1.6f;

                Add(into, centre, run, lift,
                    offset: new Vector3((((h >> 18) & 7) - 3.5f) * 0.06f, 0f, (((h >> 21) & 7) - 3.5f) * 0.10f),
                    yaw: skew, pitch: 90f + roll, length: length, radius: radius);
            }

            for (int i = 0; i < Branches; i++)
            {
                int h = Hash(x, y, 64 + i);
                float length = 0.55f + ((h >> 2) & 15) * 0.055f;
                float radius = 0.035f + ((h >> 6) & 3) * 0.016f;
                // Height is SAMPLED over the pile's whole depth rather than stepped, which is what
                // makes the top of a run ragged instead of flat.
                float lift = 0.12f + ((h >> 8) & 15) * 0.115f;
                float yaw = ((h >> 12) & 127) * 2.84f;
                // Mostly lying over, some leaning up out of the pile: the few sticking out are what
                // stop the silhouette being a loaf.
                float pitch = ((h >> 19) & 7) == 0
                    ? 30f + ((h >> 22) & 15) * 2.4f
                    : 72f + ((h >> 22) & 31) * 1.2f;

                Add(into, centre, run, lift,
                    offset: new Vector3((((h >> 27) & 7) - 3.5f) * 0.085f, 0f, (((h >> 24) & 7) - 3.5f) * 0.085f),
                    yaw: yaw, pitch: pitch, length: length, radius: radius);
            }
        }

        private static void Add(List<Matrix4x4> into, Vector3 centre, Quaternion run, float lift,
                                Vector3 offset, float yaw, float pitch, float length, float radius)
        {
            var rotation = run * Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(pitch, 0f, 0f);
            into.Add(Matrix4x4.TRS(
                centre + run * offset + new Vector3(0f, lift, 0f),
                rotation,
                new Vector3(radius * 2f, length * 0.5f, radius * 2f)));
        }

        /// <summary>Cheap deterministic hash so a wall dresses itself the same way every run.</summary>
        private static int Hash(int x, int y, int k)
        {
            unchecked
            {
                int h = (x * 73856093) ^ (y * 19349663) ^ (k * 83492791);
                h ^= h >> 13;
                h *= 1274126177;
                h ^= h >> 16;
                return h & 0x7FFFFFFF;
            }
        }
    }
}
