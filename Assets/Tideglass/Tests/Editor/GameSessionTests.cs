using System;
using NUnit.Framework;
using Tideglass.Domain;

namespace Tideglass.Tests
{
    public class GameSessionTests
    {
        private readonly ItemSpec reed = new ItemSpec("reed", "Reed", "", 0, 2);
        private readonly ItemSpec amber = new ItemSpec("amber", "Amber", "", 0, 3);
        private readonly ItemSpec wheel = new ItemSpec("wheel", "Wheel", "", 0, 4);
        private readonly ItemSpec spare = new ItemSpec("spare", "Spare", "", 0, 6);
        private readonly ItemSpec pearl = new ItemSpec("pearl", "Pearl", "", 1, 4);
        private readonly ItemSpec echo = new ItemSpec("echo", "Echo", "", 1, 5);
        private readonly ItemSpec glass = new ItemSpec("glass", "Glass", "", 2, 6);
        private GameSession game;

        [SetUp]
        public void SetUp()
        {
            game = new GameSession(new[] { reed, amber, wheel, spare, pearl, echo, glass }, new[]
            {
                new BeaconSpec("Pilot", "Shore", 5),
                new BeaconSpec("Watch", "Reef", 8),
                new BeaconSpec("Crown", "Narrows", 11)
            });
        }

        [Test]
        public void CollectingDoesNotMountALensOrChangeTheBeam()
        {
            game.Collect(reed.Id);
            game.Collect(reed.Id);
            game.Collect(null);
            game.Collect("missing");
            game.Collect(pearl.Id);

            Assert.That(game.Inventory, Is.EqualTo(new[] { reed }));
            Assert.That(game.Mounted, Is.Empty);
            Assert.That(game.BeamPower, Is.Zero);
            Assert.That(game.AvailabilityFor(reed), Is.EqualTo(LensAvailability.InInventory));
            Assert.That(game.AvailabilityFor(pearl), Is.EqualTo(LensAvailability.Locked));
        }

        [Test]
        public void AFullBagRejectsAFourthLensIncludingWhenTwoAreMounted()
        {
            Collect(reed, amber, wheel);
            game.ToggleMount(reed.Id);
            game.ToggleMount(amber.Id);
            game.Collect(spare.Id);

            Assert.That(game.Inventory, Is.EqualTo(new[] { reed, amber, wheel }));
            Assert.That(game.Mounted, Is.EqualTo(new[] { reed, amber }));
            Assert.That(game.BeamPower, Is.EqualTo(5));
            Assert.That(game.AvailabilityFor(spare), Is.EqualTo(LensAvailability.InventoryFull));
        }

        [Test]
        public void UnmountingKeepsTheLensAndLetsThePlayerChooseTheSpare()
        {
            Collect(reed, amber, wheel);
            game.ToggleMount(reed.Id);
            game.ToggleMount(wheel.Id);
            game.ToggleMount(amber.Id);
            Assert.That(game.Mounted, Is.EqualTo(new[] { reed, wheel }));
            Assert.That(game.BeamPower, Is.EqualTo(6));

            game.ToggleMount(wheel.Id);
            Assert.That(game.Inventory, Is.EqualTo(new[] { reed, amber, wheel }));
            Assert.That(game.Mounted, Is.EqualTo(new[] { reed }));
            Assert.That(game.BeamPower, Is.EqualTo(2));
            game.ToggleMount(amber.Id);
            Assert.That(game.Mounted, Is.EqualTo(new[] { reed, amber }));
            Assert.That(game.BeamPower, Is.EqualTo(5));
        }

        [Test]
        public void UnknownAndUnownedLensesCannotBeMounted()
        {
            game.ToggleMount(null);
            game.ToggleMount("missing");
            game.ToggleMount(reed.Id);
            game.ToggleMount(pearl.Id);
            Assert.That(game.Inventory, Is.Empty);
            Assert.That(game.Mounted, Is.Empty);
            Assert.That(game.BeamPower, Is.Zero);
        }

        [Test]
        public void FailedIgnitionPreservesBagSocketsAndCharges()
        {
            Collect(reed, amber, wheel);
            game.ToggleMount(reed.Id);
            game.Ignite();
            Assert.That(game.LitCount, Is.Zero);
            Assert.That(game.Inventory, Is.EqualTo(new[] { reed, amber, wheel }));
            Assert.That(game.Mounted, Is.EqualTo(new[] { reed }));

            game.ToggleMount(wheel.Id);
            Assert.That(game.BeamPower, Is.EqualTo(6));
            Assert.That(game.CanIgnite, Is.False);
            game.Ignite();
            Assert.That(game.LitCount, Is.Zero);
            Assert.That(game.Inventory, Is.EqualTo(new[] { reed, amber, wheel }));
            Assert.That(game.Mounted, Is.EqualTo(new[] { reed, wheel }));
            Assert.That(game.AvailabilityFor(reed), Is.EqualTo(LensAvailability.InInventory));
            Assert.That(game.AvailabilityFor(wheel), Is.EqualTo(LensAvailability.InInventory));
        }

        [Test]
        public void SuccessfulIgnitionSpendsOnlyThePairAndRetainsTheSpare()
        {
            Collect(reed, amber, wheel);
            game.ToggleMount(reed.Id);
            game.ToggleMount(amber.Id);
            Assert.That(game.CanIgnite, Is.True);
            game.Ignite();

            Assert.That(game.LitCount, Is.EqualTo(1));
            Assert.That(game.Inventory, Is.EqualTo(new[] { wheel }));
            Assert.That(game.Mounted, Is.Empty);
            Assert.That(game.BeamPower, Is.Zero);
            Assert.That(game.AvailabilityFor(reed), Is.EqualTo(LensAvailability.Spent));
            Assert.That(game.AvailabilityFor(amber), Is.EqualTo(LensAvailability.Spent));
            Assert.That(game.AvailabilityFor(pearl), Is.EqualTo(LensAvailability.Available));
            Assert.That(game.AvailabilityFor(glass), Is.EqualTo(LensAvailability.Locked));
            game.Collect(reed.Id);
            Assert.That(game.Inventory, Is.EqualTo(new[] { wheel }));
        }

        [Test]
        public void AValidPairCanLightTheBeaconWhenTheThirdBagSlotIsEmpty()
        {
            Collect(reed, amber);
            game.ToggleMount(reed.Id);
            game.ToggleMount(amber.Id);
            Assert.That(game.CanIgnite, Is.True);
            game.Ignite();
            Assert.That(game.LitCount, Is.EqualTo(1));
            Assert.That(game.Inventory, Is.Empty);
        }

        [Test]
        public void PartnerEffectsUseBasePowerInEitherSocket()
        {
            var mirror = new ItemSpec("mirror", "Mirror", "", 0, 2, partner => partner);
            var prism = new ItemSpec("prism", "Prism", "", 0, 3, partner => partner * 2);
            game = new GameSession(new[] { mirror, prism }, new[] { new BeaconSpec("A", "B", 12) });

            Collect(mirror, prism);
            game.ToggleMount(mirror.Id);
            Assert.That(game.BeamPower, Is.EqualTo(2));
            game.ToggleMount(prism.Id);
            Assert.That(game.BeamPower, Is.EqualTo(12));
            game.ToggleMount(mirror.Id);
            Assert.That(game.BeamPower, Is.EqualTo(3));
            game.ToggleMount(mirror.Id);
            Assert.That(game.BeamPower, Is.EqualTo(12));
        }

        [Test]
        public void CompletionCannotSpendMoreLensesOrAdvanceAgain()
        {
            Collect(reed, amber);
            MountAndIgnite(reed, amber);
            Collect(wheel, pearl);
            MountAndIgnite(wheel, pearl);
            Collect(echo, glass);
            MountAndIgnite(echo, glass);
            game.Ignite();
            game.Collect(spare.Id);
            game.ToggleMount(spare.Id);

            Assert.That(game.Finished, Is.True);
            Assert.That(game.LitCount, Is.EqualTo(3));
            Assert.That(game.CurrentBeacon, Is.Null);
            Assert.That(game.CanIgnite, Is.False);
            Assert.That(game.Inventory, Is.Empty);
            Assert.That(game.Mounted, Is.Empty);
            Assert.That(game.AvailabilityFor(spare), Is.EqualTo(LensAvailability.RouteComplete));
        }

        [Test]
        public void DuplicateItemIdsAreRejectedWhenLoadingTheCatalogue()
        {
            Assert.Throws<ArgumentException>(() => new GameSession(new[] { reed, reed }, game.Beacons));
        }

        private void Collect(params ItemSpec[] items)
        {
            foreach (var item in items) game.Collect(item.Id);
        }

        private void MountAndIgnite(ItemSpec first, ItemSpec second)
        {
            game.ToggleMount(first.Id);
            game.ToggleMount(second.Id);
            Assert.That(game.CanIgnite, Is.True);
            game.Ignite();
        }
    }
}
