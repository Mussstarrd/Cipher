#nullable enable
using System.Collections.Generic;
using Cipher.Game.Hero;
using Cipher.Game.Progression;
using NUnit.Framework;

namespace Cipher.Game.Tests
{
    /// <summary>Gear, upgrades, and the numbers they produce.</summary>
    public sealed class ProgressionTests
    {
        private static ItemInstance Item(Slot slot, Rarity rarity, int ilvl, params Affix[] affixes)
            => new ItemInstance(slot, rarity, ilvl, affixes, "test");

        // ---------------------------------------------------------------- stats

        [Test]
        public void PercentIsAdditiveAcrossSourcesThenAppliedOnce()
        {
            var b = new StatBlock();
            b.AddPercent(StatKind.GunDamage, 0.10f);
            b.AddPercent(StatKind.GunDamage, 0.20f);
            // 6 * 1.30, not 6 * 1.1 * 1.2 — additive keeps eight items readable.
            Assert.AreEqual(7.8f, b.Apply(StatKind.GunDamage, 6f), 0.0001f);
        }

        [Test]
        public void FlatAppliesBeforePercent()
        {
            var b = new StatBlock();
            b.AddFlat(StatKind.MaxHealth, 50f);
            b.AddPercent(StatKind.MaxHealth, 0.10f);
            Assert.AreEqual(165f, b.Apply(StatKind.MaxHealth, 100f), 0.0001f);
        }

        [Test]
        public void ReductionIsFlooredSoStackingCannotTrivialiseACooldown()
        {
            var b = new StatBlock();
            b.AddPercent(StatKind.AirstrikeCooldown, 0.95f);
            float cd = b.ApplyReduction(StatKind.AirstrikeCooldown, 8f);
            Assert.AreEqual(8f * 0.35f, cd, 0.0001f, "floored at 35% of base");
        }

        [Test]
        public void AddingBlocksCombinesBothChannels()
        {
            var a = new StatBlock().AddFlat(StatKind.Armour, 2f).AddPercent(StatKind.MoveSpeed, 0.1f);
            var c = new StatBlock().AddFlat(StatKind.Armour, 3f).AddPercent(StatKind.MoveSpeed, 0.2f);
            a.Add(c);
            Assert.AreEqual(5f, a.Flat(StatKind.Armour), 0.0001f);
            Assert.AreEqual(0.3f, a.Percent(StatKind.MoveSpeed), 0.0001f);
        }

        // ---------------------------------------------------------------- item maths

        [Test]
        public void BudgetMatchesTheDesignMemoWorkedExample()
        {
            // A Hardened vest at ilvl 12: 0.80 * (4 + 19.2) * 1.90 = 35.26
            float budget = ItemRules.Budget(Slot.Vest, Rarity.Hardened, 12);
            Assert.AreEqual(35.26f, budget, 0.05f);
        }

        [Test]
        public void AffixCountFollowsRarity()
        {
            Assert.AreEqual(0, ItemRules.AffixCount(Rarity.Scavenged));
            Assert.AreEqual(1, ItemRules.AffixCount(Rarity.Serviceable));
            Assert.AreEqual(2, ItemRules.AffixCount(Rarity.Issued));
            Assert.AreEqual(3, ItemRules.AffixCount(Rarity.Hardened));
            Assert.AreEqual(4, ItemRules.AffixCount(Rarity.Legacy));
        }

        [Test]
        public void ItemLevelUsesTierAndWave()
        {
            Assert.AreEqual(7, ItemRules.ItemLevel(1, 3), "tier 1 wave 3");
        }

        [Test]
        public void ARolledItemSpendsRoughlyItsBudget()
        {
            var roller = new ItemRoller(12345);
            for (int ilvl = 4; ilvl <= 28; ilvl += 6)
            {
                foreach (var rarity in new[] { Rarity.Serviceable, Rarity.Issued, Rarity.Hardened })
                {
                    var item = roller.Roll(Slot.Vest, rarity, ilvl);
                    float budget = ItemRules.Budget(Slot.Vest, rarity, ilvl);
                    // Rounding to whole numbers plus jitter, so allow generous slack at low budgets.
                    Assert.That(item.PowerScore, Is.EqualTo(budget).Within(budget * 0.45f + 4f),
                        $"{rarity} i{ilvl}: score {item.PowerScore} vs budget {budget}");
                }
            }
        }

        [Test]
        public void RollingIsDeterministicForASeed()
        {
            var a = new ItemRoller(99).Roll(Slot.Weapon, Rarity.Hardened, 10);
            var b = new ItemRoller(99).Roll(Slot.Weapon, Rarity.Hardened, 10);
            Assert.AreEqual(a.PowerScore, b.PowerScore, 0.0001f);
            Assert.AreEqual(a.Affixes.Count, b.Affixes.Count);
        }

        [Test]
        public void AnItemNeverRollsTheSameAffixTwice()
        {
            var roller = new ItemRoller(7);
            for (int i = 0; i < 60; i++)
            {
                var item = roller.Roll(Slot.Vest, Rarity.Legacy, 20);
                var seen = new HashSet<AffixKind>();
                foreach (var a in item.Affixes)
                    Assert.IsTrue(seen.Add(a.Kind), $"duplicate affix {a.Kind}");
            }
        }

        [Test]
        public void AffixesOnlyRollWhereTheyBelong()
        {
            var roller = new ItemRoller(4242);
            for (int i = 0; i < 60; i++)
            {
                var helm = roller.Roll(Slot.Helm, Rarity.Hardened, 16);
                foreach (var a in helm.Affixes)
                    Assert.IsTrue(ItemRules.CanRoll(Slot.Helm, a.Kind), $"{a.Kind} on a helm");
            }
        }

        [Test]
        public void SecondWindIsReservedForTheTopRarity()
        {
            var roller = new ItemRoller(31337);
            for (int i = 0; i < 120; i++)
            {
                var vest = roller.Roll(Slot.Vest, Rarity.Hardened, 20);
                Assert.IsFalse(vest.Has(AffixKind.SecondWind));
            }
        }

        [Test]
        public void NoAffixEverRollsAtZero()
        {
            var roller = new ItemRoller(5);
            for (int i = 0; i < 80; i++)
            {
                var item = roller.Roll(Slot.CharmA, Rarity.Legacy, 1);
                foreach (var a in item.Affixes) Assert.GreaterOrEqual(a.Magnitude, 1f);
            }
        }

        [Test]
        public void RarityFloorsAreRespected()
        {
            var roller = new ItemRoller(808);
            for (int i = 0; i < 80; i++)
                Assert.GreaterOrEqual((int)roller.RollRarity(Rarity.Issued), (int)Rarity.Issued);
        }

        [Test]
        public void TheThinkingArchetypesAlwaysDrop()
        {
            var roller = new ItemRoller(2024);
            for (int i = 0; i < 25; i++)
            {
                Assert.IsNotNull(roller.TryDrop(DropSource.SapperKill, 1, 2));
                Assert.IsNotNull(roller.TryDrop(DropSource.SpitterKill, 1, 2));
            }
        }

        [Test]
        public void RunnersAlmostNeverDrop()
        {
            var roller = new ItemRoller(77);
            int drops = 0;
            for (int i = 0; i < 4000; i++)
                if (roller.TryDrop(DropSource.RunnerKill, 1, 1) != null) drops++;
            Assert.That(drops, Is.InRange(1, 40), $"0.25% of 4000 is ~10, got {drops}");
        }

        [Test]
        public void ScripScalesWithLevelAndRarity()
        {
            Assert.Less(ItemRules.ScripValue(Rarity.Scavenged, 5),
                        ItemRules.ScripValue(Rarity.Legacy, 20));
        }

        // ---------------------------------------------------------------- inventory

        [Test]
        public void AnEmptySlotTakesTheItemStraightAway()
        {
            var inv = new Inventory();
            var r = inv.Pickup(Item(Slot.Vest, Rarity.Issued, 8, new Affix(AffixKind.Plated, 3f)));
            Assert.AreEqual(PickupOutcome.EquippedEmptySlot, r.Outcome);
            Assert.IsNotNull(inv.Equipped(Slot.Vest));
        }

        [Test]
        public void ABetterItemSwapsInAndTheOldOneGoesToThePack()
        {
            var inv = new Inventory();
            inv.Pickup(Item(Slot.Vest, Rarity.Serviceable, 5, new Affix(AffixKind.Plated, 2f)));
            var r = inv.Pickup(Item(Slot.Vest, Rarity.Hardened, 12, new Affix(AffixKind.Plated, 8f)));

            Assert.AreEqual(PickupOutcome.Upgraded, r.Outcome);
            Assert.AreEqual(8f, inv.Equipped(Slot.Vest)!.Affixes[0].Magnitude);
            Assert.AreEqual(1, inv.Pack.Count);
        }

        [Test]
        public void ClearJunkIsScrappedOnThespotRatherThanAskingThePlayer()
        {
            var inv = new Inventory();
            inv.Pickup(Item(Slot.Vest, Rarity.Hardened, 20, new Affix(AffixKind.Plated, 10f)));
            int before = inv.Scrip;

            var r = inv.Pickup(Item(Slot.Vest, Rarity.Scavenged, 2, new Affix(AffixKind.Plated, 1f)));

            Assert.AreEqual(PickupOutcome.AutoScrapped, r.Outcome);
            Assert.Greater(inv.Scrip, before);
            Assert.AreEqual(0, inv.Pack.Count, "junk never clutters the pack");
        }

        [Test]
        public void TheAmbiguousMiddleIsStowedForLaterJudgement()
        {
            var inv = new Inventory();
            inv.Pickup(Item(Slot.Vest, Rarity.Hardened, 10, new Affix(AffixKind.Plated, 10f)));
            var r = inv.Pickup(Item(Slot.Vest, Rarity.Issued, 10, new Affix(AffixKind.Plated, 8f)));
            Assert.AreEqual(PickupOutcome.Stowed, r.Outcome);
            Assert.AreEqual(1, inv.Pack.Count);
        }

        [Test]
        public void AFullPackStillAllowsAnUpgrade()
        {
            var inv = new Inventory(packCapacity: 1);
            inv.Pickup(Item(Slot.Vest, Rarity.Issued, 10, new Affix(AffixKind.Plated, 5f)));
            inv.Pickup(Item(Slot.Helm, Rarity.Issued, 10, new Affix(AffixKind.Plated, 4f)));
            inv.Pickup(Item(Slot.Helm, Rarity.Issued, 10, new Affix(AffixKind.Plated, 4.5f))); // fills pack

            int scripBefore = inv.Scrip;
            var r = inv.Pickup(Item(Slot.Vest, Rarity.Legacy, 20, new Affix(AffixKind.Plated, 20f)));

            Assert.AreEqual(PickupOutcome.Upgraded, r.Outcome);
            Assert.AreEqual(20f, inv.Equipped(Slot.Vest)!.Affixes[0].Magnitude);
            Assert.Greater(inv.Scrip, scripBefore, "the displaced piece is scrapped when the pack is full");
        }

        [Test]
        public void EquippingFromThePackSwapsBothWays()
        {
            var inv = new Inventory();
            inv.Pickup(Item(Slot.Boots, Rarity.Hardened, 10, new Affix(AffixKind.Runner, 9f)));
            inv.Pickup(Item(Slot.Boots, Rarity.Issued, 10, new Affix(AffixKind.Runner, 7f)));
            Assert.AreEqual(1, inv.Pack.Count);

            Assert.IsTrue(inv.EquipFromPack(0));
            Assert.AreEqual(7f, inv.Equipped(Slot.Boots)!.Affixes[0].Magnitude);
            Assert.AreEqual(1, inv.Pack.Count, "the piece that came off went back to the pack");
        }

        [Test]
        public void SellingThePackPaysScripAndEmptiesIt()
        {
            var inv = new Inventory();
            inv.Pickup(Item(Slot.Boots, Rarity.Hardened, 10, new Affix(AffixKind.Runner, 9f)));
            inv.Pickup(Item(Slot.Boots, Rarity.Hardened, 10, new Affix(AffixKind.Runner, 8f)));

            int gained = inv.SellPack();
            Assert.Greater(gained, 0);
            Assert.AreEqual(0, inv.Pack.Count);
            Assert.AreEqual(gained, inv.Scrip);
        }

        [Test]
        public void EquippedStatsTotalEveryWornPiece()
        {
            var inv = new Inventory();
            inv.Pickup(Item(Slot.Vest, Rarity.Issued, 10, new Affix(AffixKind.Conditioned, 20f)));
            inv.Pickup(Item(Slot.Helm, Rarity.Issued, 10, new Affix(AffixKind.Conditioned, 10f)));

            Assert.AreEqual(0.30f, inv.EquippedStats().Percent(StatKind.MaxHealth), 0.0001f);
        }

        // ---------------------------------------------------------------- upgrades

        [Test]
        public void AnOfferIsThreeDistinctCards()
        {
            var deck = new ImprovisationDeck(1);
            var offer = deck.Deal();
            Assert.AreEqual(3, offer.Count);
            CollectionAssert.AllItemsAreUnique(offer);
        }

        [Test]
        public void ACardIsNeverOfferedTwiceInARun()
        {
            var deck = new ImprovisationDeck(5);
            var taken = new HashSet<string>();
            for (int i = 0; i < 8; i++)
            {
                var offer = deck.Deal();
                if (offer.Count == 0) break;
                foreach (var c in offer)
                    Assert.IsFalse(taken.Contains(c.Id), $"{c.Id} offered after being taken");
                var card = deck.Take(0);
                Assert.IsNotNull(card);
                taken.Add(card!.Id);
            }
        }

        [Test]
        public void CapstonesStayLockedUntilThePathIsCommittedTo()
        {
            var deck = new ImprovisationDeck(3);
            Assert.IsFalse(deck.CapstoneUnlocked(Path.Trigger));
            for (int i = 0; i < 40; i++)
            {
                foreach (var c in deck.Deal())
                    Assert.IsFalse(c.IsCapstone, "a capstone appeared with no path commitment");
                deck.Skip();
            }
        }

        [Test]
        public void ThreeOfAPathUnlocksItsCapstone()
        {
            var deck = new ImprovisationDeck(11, TriggerOnly());
            for (int i = 0; i < 3; i++) { deck.Deal(); deck.Take(0); }

            Assert.AreEqual(3, deck.CountFor(Path.Trigger));
            Assert.IsTrue(deck.CapstoneUnlocked(Path.Trigger));
            Assert.AreEqual(Path.Trigger, deck.DominantPath());
        }

        [Test]
        public void DominantPathIsNullWhilePicksAreEven()
        {
            var deck = new ImprovisationDeck(2);
            Assert.IsNull(deck.DominantPath(), "nothing taken yet");
        }

        [Test]
        public void TakenCardsTotalIntoStats()
        {
            var deck = new ImprovisationDeck(1, new List<Improvisation>
            {
                new Improvisation("a", "A", Path.Trigger, "", b => b.AddPercent(StatKind.GunDamage, 0.15f)),
                new Improvisation("b", "B", Path.Trigger, "", b => b.AddPercent(StatKind.GunDamage, 0.22f)),
                new Improvisation("c", "C", Path.Trigger, "", b => b.AddPercent(StatKind.GunDamage, 0.10f)),
            });
            deck.Deal();
            deck.Take(0);
            deck.Deal();
            deck.Take(0);

            Assert.Greater(deck.TotalStats().Percent(StatKind.GunDamage), 0.19f);
            Assert.AreEqual(2, deck.Taken.Count);
        }

        [Test]
        public void TakingAnInvalidIndexDoesNothing()
        {
            var deck = new ImprovisationDeck(1);
            deck.Deal();
            Assert.IsNull(deck.Take(9));
            Assert.AreEqual(0, deck.Taken.Count);
        }

        [Test]
        public void EveryCatalogueCardActuallyChangesSomething()
        {
            foreach (var card in ImprovisationCatalogue.All)
            {
                var b = new StatBlock();
                card.ApplyTo(b);
                Assert.IsFalse(b.IsEmpty, $"{card.Id} does nothing");
            }
        }

        [Test]
        public void EveryPathHasExactlyOneCapstone()
        {
            foreach (Path path in System.Enum.GetValues(typeof(Path)))
            {
                int caps = 0;
                foreach (var c in ImprovisationCatalogue.All)
                    if (c.Path == path && c.IsCapstone) caps++;
                Assert.AreEqual(1, caps, $"{path} has {caps} capstones");
            }
        }

        private static List<Improvisation> TriggerOnly()
        {
            var list = new List<Improvisation>();
            foreach (var c in ImprovisationCatalogue.All)
                if (c.Path == Path.Trigger) list.Add(c);
            return list;
        }

        // ---------------------------------------------------------------- loadout

        [Test]
        public void ABareLoadoutMatchesTheBaseConfig()
        {
            var base_ = new HeroConfig();
            var loadout = new Loadout(base_);
            Assert.AreEqual(base_.GunDamage, loadout.Effective.GunDamage, 0.0001f);
            Assert.AreEqual(base_.MaxHealth, loadout.Effective.MaxHealth, 0.0001f);
        }

        [Test]
        public void GearAndCardsBothReachTheHero()
        {
            var loadout = new Loadout(new HeroConfig());
            float baseDamage = loadout.Effective.GunDamage;

            loadout.Pickup(Item(Slot.Weapon, Rarity.Issued, 10, new Affix(AffixKind.HandLoaded, 20f)));
            Assert.AreEqual(baseDamage * 1.20f, loadout.Effective.GunDamage, 0.001f);

            loadout.Deck.Deal();
            // Take a known card directly rather than relying on the shuffle.
            var deck = new ImprovisationDeck(1, new List<Improvisation>
            {
                new Improvisation("z", "Z", Path.Trigger, "", b => b.AddPercent(StatKind.GunDamage, 0.30f)),
            });
            var combined = new Loadout(new HeroConfig(), loadout.Inventory, deck);
            deck.Deal();
            combined.TakeImprovisation(0);

            // 20% from gear plus 30% from the card, added, not compounded.
            Assert.AreEqual(baseDamage * 1.50f, combined.Effective.GunDamage, 0.001f);
        }

        [Test]
        public void FireRateRaisesRateByLoweringTheInterval()
        {
            var loadout = new Loadout(new HeroConfig());
            float baseInterval = loadout.Effective.FireInterval;
            loadout.Pickup(Item(Slot.Weapon, Rarity.Issued, 10, new Affix(AffixKind.Cyclic, 25f)));
            Assert.Less(loadout.Effective.FireInterval, baseInterval);
        }

        [Test]
        public void ArmourReducesContactDamageButNeverToNothing()
        {
            var loadout = new Loadout(new HeroConfig());
            loadout.Pickup(Item(Slot.Vest, Rarity.Issued, 10, new Affix(AffixKind.Plated, 4f)));
            Assert.AreEqual(2f, loadout.MitigateContact(6f), 0.0001f);

            var heavy = new Loadout(new HeroConfig());
            heavy.Pickup(Item(Slot.Vest, Rarity.Legacy, 30, new Affix(AffixKind.Plated, 500f)));
            Assert.AreEqual(0.6f, heavy.MitigateContact(6f), 0.0001f, "floored at a tenth");
        }

        [Test]
        public void ScroungerMultipliesKillCash()
        {
            var loadout = new Loadout(new HeroConfig());
            Assert.AreEqual(50, loadout.CashForKills(10, 5));
            loadout.Pickup(Item(Slot.Weapon, Rarity.Issued, 10, new Affix(AffixKind.Scrounger, 30f)));
            Assert.AreEqual(65, loadout.CashForKills(10, 5));
        }

        [Test]
        public void TheOrdnanceCapstoneCanRemoveSelfDamageEntirely()
        {
            var deck = new ImprovisationDeck(1, new List<Improvisation>
            {
                new Improvisation("cap", "Danger Close", Path.Ordnance, "",
                    b => b.AddPercent(StatKind.AirstrikeSelfDamage, 1f)),
            });
            var loadout = new Loadout(new HeroConfig(), null, deck);
            deck.Deal();
            loadout.TakeImprovisation(0);
            Assert.AreEqual(0f, loadout.Effective.AirstrikeSelfDamage, 0.0001f);
        }

        [Test]
        public void GeometryTheePlayerHasLearnedIsNeverScaled()
        {
            var base_ = new HeroConfig();
            var loadout = new Loadout(base_);
            loadout.Pickup(Item(Slot.Weapon, Rarity.Legacy, 28,
                new Affix(AffixKind.HandLoaded, 50f), new Affix(AffixKind.Cyclic, 50f)));

            Assert.AreEqual(base_.GunRange, loadout.Effective.GunRange, 0.0001f);
            Assert.AreEqual(base_.AirstrikeMaxRange, loadout.Effective.AirstrikeMaxRange, 0.0001f);
            Assert.AreEqual(base_.AirstrikeBombCount, loadout.Effective.AirstrikeBombCount);
        }
    }
}
