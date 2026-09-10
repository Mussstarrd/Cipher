#nullable enable
using Cipher.Game.UI;
using NUnit.Framework;

namespace Cipher.Game.Tests
{
    public sealed class PauseMenuModelTests
    {
        [Test]
        public void StartsClosedAndIgnoresInputWhileClosed()
        {
            var m = new PauseMenuModel();
            Assert.IsFalse(m.IsOpen);
            m.MoveDown();
            Assert.AreEqual(0, m.SelectedIndex);
            Assert.AreEqual(PauseMenuAction.None, m.Confirm());
            Assert.AreEqual(PauseMenuAction.None, m.Cancel());
        }

        [Test]
        public void ToggleOpensOnResumeEveryTime()
        {
            var m = new PauseMenuModel();
            m.Toggle();
            m.MoveDown();
            Assert.AreEqual("Quit", m.SelectedLabel);
            m.Toggle(); // close
            m.Toggle(); // reopen
            Assert.IsTrue(m.IsOpen);
            Assert.AreEqual("Resume", m.SelectedLabel, "reopening must never land on Quit");
        }

        [Test]
        public void NavigationWrapsBothWays()
        {
            var m = new PauseMenuModel();
            m.Open();
            m.MoveUp();
            Assert.AreEqual(PauseMenuModel.Items.Length - 1, m.SelectedIndex);
            m.MoveDown();
            Assert.AreEqual(0, m.SelectedIndex);
        }

        [Test]
        public void ConfirmOnResumeClosesAndReportsResume()
        {
            var m = new PauseMenuModel();
            m.Open();
            Assert.AreEqual(PauseMenuAction.Resume, m.Confirm());
            Assert.IsFalse(m.IsOpen);
        }

        [Test]
        public void ConfirmOnQuitReportsQuitAndStaysOpen()
        {
            var m = new PauseMenuModel();
            m.Open();
            m.MoveDown();
            Assert.AreEqual(PauseMenuAction.Quit, m.Confirm());
            Assert.IsTrue(m.IsOpen, "menu stays up while the app tears down");
        }

        [Test]
        public void CancelAlwaysResumesRegardlessOfSelection()
        {
            var m = new PauseMenuModel();
            m.Open();
            m.MoveDown();
            Assert.AreEqual(PauseMenuAction.Resume, m.Cancel());
            Assert.IsFalse(m.IsOpen);
        }

        [Test]
        public void SelectOutOfRangeIsIgnored()
        {
            var m = new PauseMenuModel();
            m.Open();
            Assert.AreEqual(PauseMenuAction.None, m.Select(-1));
            Assert.AreEqual(PauseMenuAction.None, m.Select(99));
            Assert.IsTrue(m.IsOpen);
        }
    }
}
