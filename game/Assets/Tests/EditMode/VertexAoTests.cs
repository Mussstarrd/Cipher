#nullable enable
using Cipher.Game;
using NUnit.Framework;
using UnityEngine;

namespace Cipher.Game.Tests
{
    /// <summary>
    /// The vertex-AO darkening curve: the only part of the bake with any judgement in it, and the
    /// part that decides whether a stack of boxes reads as a made thing or as a stack of boxes.
    ///
    /// Bake() itself is geometry plumbing -- transform, measure, clone, write -- and needs a scene
    /// to say anything. This does not.
    /// </summary>
    public sealed class VertexAoTests
    {

        [Test]
        public void Occlusion_never_darkens_past_the_floor_and_never_brightens()
        {
            for (float h = 0f; h < 4f; h += 0.05f)
            {
                for (float gap = 0f; gap < 1f; gap += 0.05f)
                {
                    float ao = VertexAo.Occlusion(h, gap);
                    Assert.GreaterOrEqual(ao, VertexAo.Floor - 1e-4f, $"too dark at h={h} gap={gap}");
                    Assert.LessOrEqual(ao, 1f + 1e-4f, $"brighter than white at h={h} gap={gap}");
                }
            }
        }

        [Test]
        public void Occlusion_darkens_at_the_base_and_leaves_the_top_alone()
        {
            float bottom = VertexAo.Occlusion(0f, float.PositiveInfinity);
            float top = VertexAo.Occlusion(3f, float.PositiveInfinity);

            Assert.AreEqual(VertexAo.Floor, bottom, 1e-4f, "the ground contact is as dark as it goes");
            Assert.AreEqual(1f, top, 1e-4f, "a roof is not shaded by being a roof");
            Assert.Less(bottom, top);
        }

        [Test]
        public void Occlusion_darkens_where_another_box_is_close()
        {
            // Same height, well clear of the ground: the only difference is the neighbour.
            float open = VertexAo.Occlusion(3f, float.PositiveInfinity);
            float crevice = VertexAo.Occlusion(3f, 0f);
            Assert.Less(crevice, open, "a corner has to be darker than an open face");
            Assert.AreEqual(VertexAo.Floor, crevice, 1e-4f);
        }

        [Test]
        public void Occlusion_ignores_a_neighbour_that_is_far_enough_away()
        {
            float ao = VertexAo.Occlusion(3f, VertexAo.CreviceRadius * 2f);
            Assert.AreEqual(1f, ao, 1e-4f);
        }

        [Test]
        public void Occlusion_is_quantised_rather_than_smooth()
        {
            // The direction is flat banded shading, so the curve must produce a small set of
            // values, not a continuum. Anything else is a soft gradient wearing a ladder's name.
            var seen = new System.Collections.Generic.HashSet<int>();
            for (float h = 0f; h < VertexAo.GroundFalloff; h += 0.005f)
                seen.Add(Mathf.RoundToInt(VertexAo.Occlusion(h, float.PositiveInfinity) * 1000f));

            Assert.LessOrEqual(seen.Count, VertexAo.Steps + 1,
                               $"expected at most {VertexAo.Steps + 1} flat tones, got {seen.Count}");
        }

        [Test]
        public void Occlusion_takes_the_stronger_of_the_two_causes_not_their_sum()
        {
            float both = VertexAo.Occlusion(0f, 0f);
            float groundOnly = VertexAo.Occlusion(0f, float.PositiveInfinity);
            Assert.AreEqual(groundOnly, both, 1e-4f,
                            "a corner at ground level is a corner, not twice as dark as either");
        }
    }
}
