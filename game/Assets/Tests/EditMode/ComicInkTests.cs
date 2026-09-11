#nullable enable
using Cipher.Game.UI.Comic;
using NUnit.Framework;

namespace Cipher.Game.Tests
{
    /// <summary>
    /// What can be checked about the drawing kit without a GPU: that the generated textures build,
    /// that the cache survives being torn down and rebuilt, and that ComicRect's containment and
    /// overlap rules — which every layout assertion in the other three files leans on — are right.
    ///
    /// The pixels themselves are NOT verified here and cannot be. CLAUDE.md: "Verify rendering,
    /// never infer it" — that means a screenshot of ComicPageDemo.DrawAll, not a unit test.
    /// </summary>
    public sealed class ComicInkTests
    {
        [TearDown]
        public void Cleanup() => ComicInk.Release();

        [Test]
        public void TexturesBuildAndTheCacheHealsAfterATeardown()
        {
            // Twice on purpose: the second Warm proves the lazy accessors rebuild rather than handing
            // back a destroyed texture, which is the domain-reload case in a form a test can reach.
            Assert.DoesNotThrow(ComicInk.Warm);
            Assert.DoesNotThrow(ComicInk.Release);
            Assert.DoesNotThrow(ComicInk.Warm);
            Assert.DoesNotThrow(ComicInk.Release);
            Assert.DoesNotThrow(ComicInk.Release, "releasing twice must be harmless");
        }

        [Test]
        public void ThePaletteIsTheOneInTheBrief()
        {
            // Off-white paper, near-black ink, and exactly one accent. If someone adds a second
            // accent colour this file is where the argument should happen.
            Assert.AreEqual(0xF2 / 255f, ComicInk.Paper.r, 0.01f);
            Assert.Less(ComicInk.Ink.r, 0.12f);
            Assert.Greater(ComicInk.Ink.r, 0f, "ink is never pure black next to warm paper");
            Assert.AreEqual(1f, ComicInk.Amber.r, 0.01f);
            Assert.AreEqual(0x1F / 255f, ComicInk.Amber.b, 0.02f);
            Assert.AreEqual(3f, ComicInk.BorderWidth);
        }

        [Test]
        public void TouchingEdgesDoNotCountAsOverlapping()
        {
            // Panels on a page sit gutter to gutter; if this were not true every "must not overlap"
            // assertion in the layout tests would be unusable.
            var a = new ComicRect(0f, 0f, 10f, 10f);
            var b = new ComicRect(10f, 0f, 10f, 10f);
            Assert.IsFalse(a.Overlaps(b));
            Assert.IsTrue(a.Overlaps(new ComicRect(9.9f, 0f, 10f, 10f)));
        }

        [Test]
        public void AnEmptyRectOverlapsNothing()
        {
            var a = new ComicRect(0f, 0f, 0f, 10f);
            Assert.IsTrue(a.IsEmpty);
            Assert.IsFalse(a.Overlaps(new ComicRect(-5f, -5f, 20f, 20f)));
        }

        [Test]
        public void InsetShrinksAndOffsetMoves()
        {
            var r = new ComicRect(10f, 20f, 100f, 50f);
            var inset = r.Inset(5f);
            Assert.AreEqual(15f, inset.X);
            Assert.AreEqual(90f, inset.W);
            Assert.IsTrue(inset.IsInside(r));

            // A negative inset grows the rect, which is how the panel shadow and the cursor bracket
            // are built; it must NOT then claim to be inside.
            Assert.IsFalse(r.Inset(-5f).IsInside(r));
            Assert.AreEqual(12f, r.Offset(2f, 3f).X);
            Assert.AreEqual(23f, r.Offset(2f, 3f).Y);
        }

        [Test]
        public void ANegativeSizeCollapsesRatherThanInverting()
        {
            // Layout maths divides by counts and subtracts padding; a too-small panel must produce an
            // empty rect that draws nothing, never an inside-out one that draws everywhere.
            var r = new ComicRect(10f, 10f, -40f, -40f);
            Assert.AreEqual(0f, r.W);
            Assert.AreEqual(0f, r.H);
            Assert.IsTrue(r.IsEmpty);
        }
    }
}
