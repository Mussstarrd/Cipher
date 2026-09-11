#nullable enable
using Cipher.Game;
using NUnit.Framework;

namespace Cipher.Game.Tests
{
    /// <summary>Command-line parsing for the visual smoke test harness.</summary>
    public sealed class ScreenshotHarnessTests
    {
        [Test]
        public void NoArgsMeansDisarmed()
        {
            var (path, _) = ScreenshotHarness.Parse(new[] { "ProjectExodus.exe" });
            Assert.IsNull(path);
        }

        [Test]
        public void NullArgsAreSafe()
        {
            var (path, _) = ScreenshotHarness.Parse(null!);
            Assert.IsNull(path);
        }

        [Test]
        public void PathIsPickedUp()
        {
            var (path, delay) = ScreenshotHarness.Parse(
                new[] { "ProjectExodus.exe", "-exodus-screenshot", "C:/shots/a.png" });
            Assert.AreEqual("C:/shots/a.png", path);
            Assert.AreEqual(8f, delay, 0.001f, "default delay");
        }

        [Test]
        public void DelayOverrideIsParsedInvariantly()
        {
            var (_, delay) = ScreenshotHarness.Parse(
                new[] { "x", "-exodus-screenshot", "a.png", "-exodus-screenshot-delay", "2.5" });
            Assert.AreEqual(2.5f, delay, 0.001f);
        }

        [Test]
        public void AFlagWithNoValueDoesNotArmIt()
        {
            var (path, _) = ScreenshotHarness.Parse(new[] { "x", "-exodus-screenshot" });
            Assert.IsNull(path);
        }

        [Test]
        public void BlankPathDoesNotArmIt()
        {
            var (path, _) = ScreenshotHarness.Parse(new[] { "x", "-exodus-screenshot", "   " });
            Assert.IsNull(path);
        }

        [Test]
        public void OverheadIsOffByDefault()
        {
            Assert.IsFalse(ScreenshotHarness.WantsOverhead(new[] { "x", "-exodus-screenshot", "a.png" }));
            Assert.IsFalse(ScreenshotHarness.WantsOverhead(null!));
        }

        [Test]
        public void OverheadFlagIsDetected()
        {
            Assert.IsTrue(ScreenshotHarness.WantsOverhead(
                new[] { "x", "-exodus-screenshot", "a.png", "-exodus-screenshot-overhead" }));
        }

        [Test]
        public void ANegativeDelayIsIgnored()
        {
            var (_, delay) = ScreenshotHarness.Parse(
                new[] { "x", "-exodus-screenshot", "a.png", "-exodus-screenshot-delay", "-3" });
            Assert.AreEqual(8f, delay, 0.001f);
        }
    }
}
