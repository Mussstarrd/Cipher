#nullable enable
using Cipher.Game.UI.Comic;
using NUnit.Framework;

namespace Cipher.Game.Tests
{
    /// <summary>
    /// The end-of-position screen's copy.
    ///
    /// The owner, 2026-09-11: *"after a certain amount of time it just kicked me out to that fell
    /// back screen so I'm not sure what that really meant either."* Every test here exists because
    /// of that sentence. If one starts failing, the screen has gone back to being five numbers with
    /// no explanation of what happened to him.
    /// </summary>
    public sealed class FellBackReportTests
    {
        private static FellBackReport.Facts Typical(string next = "The Service Road")
            => new FellBackReport.Facts(wavesHeld: 5, chipsBroken: 431, onTheTruck: 3, leftBehind: 4,
                                        prepSeconds: 134f, materials: 161, nextName: next, pad: true);

        [Test]
        public void ItLeadsWithWhatHappened_NotWithANumber()
        {
            string body = FellBackReport.Compose(Typical());
            string first = body.Split('\n')[0];

            // ADR-005: "Extracted is neither a win nor a loss ... leaving on schedule is the correct
            // play, not a failure." That is the single most important thing this screen says and it
            // was not on it at all.
            StringAssert.Contains("Not a defeat", first);
            StringAssert.DoesNotContain("5 waves", first, "the lead is a sentence, not a statistic");
        }

        [Test]
        public void EveryLedgerRowIsLabelled()
        {
            string body = FellBackReport.Compose(Typical()).ToUpperInvariant();
            StringAssert.Contains("HELD", body);
            StringAssert.Contains("ON THE TRUCK", body);
            StringAssert.Contains("LEFT BEHIND", body);
            StringAssert.Contains("TIME UNSPENT", body);
        }

        [Test]
        public void ItNamesWhatWasTakenAndWhatWasAbandoned()
        {
            string body = FellBackReport.Compose(Typical());
            StringAssert.Contains("3 emplacements", body);
            StringAssert.Contains("4 emplacements", body);
            StringAssert.Contains("bolted down for good", body);
        }

        [Test]
        public void AClearGravelIsSaidInWords()
        {
            var facts = new FellBackReport.Facts(3, 90, 5, 0, 40f, 48, "The Pump House", true);
            StringAssert.Contains("nothing", FellBackReport.Compose(facts).ToLowerInvariant());
        }

        [Test]
        public void UnspentTimeIsShownAsAClockAndAsMoney()
        {
            // ADR-005 §4: the cycle is one budget spent in three places, and this row is the only
            // time the player is ever told the third place paid out.
            string body = FellBackReport.Compose(Typical());
            StringAssert.Contains("2:14", body);
            StringAssert.Contains("$161", body);
        }

        [Test]
        public void ItSaysWhereYouAreGoing()
        {
            StringAssert.Contains("The Service Road", FellBackReport.Compose(Typical()));
        }

        [Test]
        public void WithNothingBehindYouItSaysSoRatherThanNamingNowhere()
        {
            var facts = Typical(next: "");
            string body = FellBackReport.Compose(facts);

            Assert.IsFalse(FellBackReport.HasNext(facts));
            StringAssert.Contains("no line behind this one", body);
            StringAssert.Contains("nowhere else to fall back to", body);
        }

        [Test]
        public void ThePromptNamesTheDeviceInThePlayersHands()
        {
            StringAssert.Contains("A: move out", FellBackReport.Compose(Typical()));

            var keys = new FellBackReport.Facts(5, 431, 3, 4, 134f, 161, "The Service Road", pad: false);
            StringAssert.Contains("Enter: move out", FellBackReport.Compose(keys));
        }

        [Test]
        public void SingularsReadLikeEnglish()
        {
            var one = new FellBackReport.Facts(1, 12, 1, 1, 5f, 6, "Somewhere", true);
            string body = FellBackReport.Compose(one);
            StringAssert.Contains("1 wave", body);
            StringAssert.DoesNotContain("1 waves", body);
            StringAssert.DoesNotContain("1 emplacements", body);
        }

        [Test]
        public void TheClockIsMinutesAndSeconds()
        {
            Assert.AreEqual("0:00", FellBackReport.Clock(0f));
            Assert.AreEqual("0:09", FellBackReport.Clock(9f));
            Assert.AreEqual("1:00", FellBackReport.Clock(60f));
            Assert.AreEqual("2:14", FellBackReport.Clock(134f));
            Assert.AreEqual("0:00", FellBackReport.Clock(-5f), "a negative clock is zero, not a minus sign");
        }
    }
}
