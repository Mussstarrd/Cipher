#nullable enable
using Cipher.Game;
using NUnit.Framework;
using UnityEngine;

namespace Cipher.Game.Tests
{
    /// <summary>
    /// The decal system's bookkeeping, which is the half with an off-by-one in it.
    ///
    /// Nothing here touches the GPU. Decals.Draw is a loop with no judgement in it; everything
    /// that could be subtly wrong -- an eviction that loses the newest mark, a young mark dropped
    /// behind an expired one, a fade that never reaches zero -- is in DecalRing and is here.
    /// </summary>
    public sealed class DecalRingTests
    {
        // ------------------------------------------------------------------ decal ring

        [Test]
        public void Ring_starts_empty()
        {
            var ring = new DecalRing(8);
            Assert.AreEqual(0, ring.Count);
            Assert.AreEqual(8, ring.Capacity);
        }

        [Test]
        public void Ring_keeps_marks_oldest_first()
        {
            var ring = new DecalRing(8);
            ring.Add(DecalKind.Scorch, new Vector3(1f, 0f, 0f), 1f, 0f, 10f);
            ring.Add(DecalKind.Stain, new Vector3(2f, 0f, 0f), 1f, 0f, 10f);

            Assert.AreEqual(2, ring.Count);
            Assert.AreEqual(1f, ring[0].Position.x, 1e-4f);
            Assert.AreEqual(2f, ring[1].Position.x, 1e-4f);
            Assert.AreEqual(DecalKind.Stain, ring[1].Kind);
        }

        [Test]
        public void Ring_refuses_marks_with_no_size_or_no_life()
        {
            var ring = new DecalRing(4);
            ring.Add(DecalKind.Scorch, Vector3.zero, 0f, 0f, 10f);
            ring.Add(DecalKind.Scorch, Vector3.zero, 1f, 0f, 0f);
            Assert.AreEqual(0, ring.Count);
        }

        [Test]
        public void Ring_overwrites_the_oldest_when_full()
        {
            var ring = new DecalRing(4);
            for (int i = 0; i < 4; i++)
                ring.Add(DecalKind.Scorch, new Vector3(i, 0f, 0f), 1f, 0f, 10f);

            ring.Add(DecalKind.Scorch, new Vector3(99f, 0f, 0f), 1f, 0f, 10f);

            Assert.AreEqual(4, ring.Count, "a full ring stays full");
            Assert.AreEqual(1f, ring[0].Position.x, 1e-4f, "mark 0 was the one evicted");
            Assert.AreEqual(99f, ring[3].Position.x, 1e-4f, "the newest mark must always survive");
        }

        [Test]
        public void Ring_survives_many_wraps()
        {
            var ring = new DecalRing(4);
            for (int i = 0; i < 50; i++)
                ring.Add(DecalKind.Drag, new Vector3(i, 0f, 0f), 1f, 0f, 10f);

            Assert.AreEqual(4, ring.Count);
            Assert.AreEqual(46f, ring[0].Position.x, 1e-4f);
            Assert.AreEqual(49f, ring[3].Position.x, 1e-4f);
        }

        [Test]
        public void Ring_retires_marks_that_have_run_out()
        {
            var ring = new DecalRing(8);
            ring.Add(DecalKind.Scorch, Vector3.zero, 1f, 0f, 1f);
            ring.Add(DecalKind.Scorch, Vector3.one, 1f, 0f, 10f);

            ring.Age(2f);

            Assert.AreEqual(1, ring.Count);
            Assert.AreEqual(1f, ring[0].Position.x, 1e-4f, "the survivor is the long-lived one");
        }

        [Test]
        public void Ring_ages_every_live_mark()
        {
            var ring = new DecalRing(8);
            ring.Add(DecalKind.Stain, Vector3.zero, 1f, 0f, 10f);
            ring.Age(1f);
            ring.Age(1f);
            Assert.AreEqual(2f, ring[0].Age, 1e-4f);
        }

        [Test]
        public void Ring_keeps_a_young_mark_stuck_behind_an_expired_one()
        {
            // Lifetimes differ per kind, so the tail is not always the first to expire. The young
            // mark must still be there -- invisible or not -- rather than dropped with the old one.
            var ring = new DecalRing(8);
            ring.Add(DecalKind.Stain, Vector3.zero, 1f, 0f, 100f);   // long-lived, at the tail
            ring.Add(DecalKind.Drag, Vector3.one, 1f, 0f, 1f);       // short-lived, in front

            ring.Age(2f);

            Assert.AreEqual(2, ring.Count, "only the TAIL retires; the ring stays contiguous");
            Assert.AreEqual(0f, DecalRing.Fade(ring[1].Age, ring[1].Life), 1e-4f,
                            "the expired one is drawn at zero alpha, i.e. not at all");
        }

        [Test]
        public void Ring_clears()
        {
            var ring = new DecalRing(4);
            ring.Add(DecalKind.Scorch, Vector3.zero, 1f, 0f, 10f);
            ring.Clear();
            Assert.AreEqual(0, ring.Count);
        }

        // ------------------------------------------------------------------ fade

        [Test]
        public void Fade_holds_full_then_falls_to_nothing()
        {
            Assert.AreEqual(1f, DecalRing.Fade(0f, 10f), 1e-4f);
            Assert.AreEqual(1f, DecalRing.Fade(5f, 10f), 1e-4f, "a mark is full-strength most of its life");
            Assert.AreEqual(0f, DecalRing.Fade(10f, 10f), 1e-4f);
            Assert.AreEqual(0f, DecalRing.Fade(99f, 10f), 1e-4f, "past its life it stays gone");
        }

        [Test]
        public void Fade_is_monotonic()
        {
            float previous = 2f;
            for (float t = 0f; t <= 10f; t += 0.25f)
            {
                float f = DecalRing.Fade(t, 10f);
                Assert.LessOrEqual(f, previous + 1e-5f, $"fade went back up at {t}");
                previous = f;
            }
        }

        [Test]
        public void Fade_of_a_zero_life_mark_is_zero_rather_than_a_divide_by_zero()
        {
            Assert.AreEqual(0f, DecalRing.Fade(1f, 0f), 1e-4f);
        }
    }
}
