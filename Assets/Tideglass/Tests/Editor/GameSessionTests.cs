using System;
using NUnit.Framework;
using Tideglass.Domain;

namespace Tideglass.Tests
{
    public class GameSessionTests
    {
        private readonly ItemSpec reed = new ItemSpec("reed", "Reed", "", 0, 2);
        private readonly ItemSpec amber = new ItemSpec("amber", "Amber", "", 0, 3);
        private readonly ItemSpec pearl = new ItemSpec("pearl", "Pearl", "", 1, 5);
        private readonly ItemSpec echo = new ItemSpec("echo", "Echo", "", 2, 1, partner => partner);
        private GameSession game;

        [SetUp]
        public void SetUp()
        {
            game = new GameSession(new[] { reed, amber, pearl, echo }, new[]
            {
                new BeaconSpec("Pilot", "Shore", 5),
                new BeaconSpec("Watch", "Reef", 8),
                new BeaconSpec("Crown", "Narrows", 11)
            });
        }

        [Test]
        public void CollectionShowsTheFullCatalogueBeforeAnythingIsOwned()
        {
            Assert.That(game.Items.Count, Is.EqualTo(4));
            Assert.That(game.IsUnlocked(reed), Is.True);
            Assert.That(game.IsOwned(reed.Id), Is.False);
            Assert.That(game.IsUnlocked(pearl), Is.False);
            Assert.That(game.IsOwned(pearl.Id), Is.False);
            Assert.That(game.BeamPower, Is.Zero);
        }

        [Test]
        public void UnknownAndLockedItemsDoNotEnterInventoryOrSockets()
        {
            Assert.That(game.Collect("missing"), Is.Not.Empty);
            Assert.That(game.Collect(pearl.Id), Is.Not.Empty);
            Assert.That(game.ToggleMount("missing"), Is.Not.Empty);
            Assert.That(game.ToggleMount(reed.Id), Is.Not.Empty);
            Assert.That(game.ToggleMount(pearl.Id), Is.Not.Empty);

            Assert.That(game.IsOwned("missing"), Is.False);
            Assert.That(game.IsOwned(pearl.Id), Is.False);
            Assert.That(game.Mounted, Is.Empty);
        }

        [Test]
        public void ASecondClickRemovesTheLensAndLeavesItOwned()
        {
            game.Collect(reed.Id);
            game.Collect(reed.Id);
            game.ToggleMount(reed.Id);
            Assert.That(game.BeamPower, Is.EqualTo(reed.BasePower));
            Assert.That(game.IsMounted(reed.Id), Is.True);

            game.ToggleMount(reed.Id);
            Assert.That(game.Mounted, Is.Empty);
            Assert.That(game.IsOwned(reed.Id), Is.True);
        }

        [Test]
        public void AFullLampRejectsAThirdLensWithoutEvictingEitherLens()
        {
            LightFirstBeacon();
            game.Collect(pearl.Id);
            game.ToggleMount(reed.Id);
            game.ToggleMount(amber.Id);
            Assert.That(game.ToggleMount(pearl.Id), Is.Not.Empty);

            Assert.That(game.Mounted, Is.EqualTo(new[] { reed, amber }));
            Assert.That(game.BeamPower, Is.EqualTo(5));
        }

        [Test]
        public void AValidPairUnlocksTheNextLocationOnlyWhenIgnited()
        {
            CollectAndMount(reed);
            CollectAndMount(amber);
            Assert.That(game.CanIgnite, Is.True);
            Assert.That(game.LitCount, Is.Zero);
            Assert.That(game.IsUnlocked(pearl), Is.False);

            game.Ignite();
            Assert.That(game.LitCount, Is.EqualTo(1));
            Assert.That(game.CurrentBeacon.Title, Is.EqualTo("Watch"));
            Assert.That(game.IsUnlocked(pearl), Is.True);
            Assert.That(game.IsUnlocked(echo), Is.False);
            Assert.That(game.IsOwned(pearl.Id), Is.False);
            Assert.That(game.IsOwned(reed.Id), Is.True);
            Assert.That(game.IsOwned(amber.Id), Is.True);
            Assert.That(game.Mounted, Is.Empty);
        }

        [Test]
        public void MissingOrMistunedPairCannotAdvanceTheRoute()
        {
            LightFirstBeacon();
            game.ToggleMount(amber.Id);
            Assert.That(game.CanIgnite, Is.False);
            game.Ignite();
            Assert.That(game.LitCount, Is.EqualTo(1));

            game.ToggleMount(reed.Id);
            Assert.That(game.BeamPower, Is.EqualTo(5));
            Assert.That(game.CanIgnite, Is.False);
            game.Ignite();
            Assert.That(game.LitCount, Is.EqualTo(1));
            Assert.That(game.Mounted, Is.EqualTo(new[] { amber, reed }));
        }

        [Test]
        public void PartnerEffectsReadBasePowerInEitherSocketAndOnlyWhenPaired()
        {
            var mirror = new ItemSpec("mirror", "Mirror", "", 0, 2, partner => partner);
            var prism = new ItemSpec("prism", "Prism", "", 0, 3, partner => partner * 2);
            game = new GameSession(new[] { mirror, prism }, new[] { new BeaconSpec("A", "B", 12) });

            CollectAndMount(mirror);
            Assert.That(game.BeamPower, Is.EqualTo(2));
            CollectAndMount(prism);
            Assert.That(game.BeamPower, Is.EqualTo((2 + 3) + (3 + 2 * 2)));
            game.ToggleMount(mirror.Id);
            Assert.That(game.BeamPower, Is.EqualTo(3));
            game.ToggleMount(mirror.Id);
            Assert.That(game.BeamPower, Is.EqualTo(12));
        }

        [Test]
        public void TheFinalIgnitionCompletesTheRouteOnlyOnceAndPreservesTheCollection()
        {
            LightFirstBeacon();
            game.ToggleMount(amber.Id);
            CollectAndMount(pearl);
            game.Ignite();
            Assert.That(game.IsUnlocked(echo), Is.True);

            game.ToggleMount(pearl.Id);
            CollectAndMount(echo);
            Assert.That(game.BeamPower, Is.EqualTo(11));
            game.Ignite();
            game.Ignite();
            game.ToggleMount(echo.Id);

            Assert.That(game.Finished, Is.True);
            Assert.That(game.LitCount, Is.EqualTo(3));
            Assert.That(game.CurrentBeacon, Is.Null);
            Assert.That(game.CanIgnite, Is.False);
            Assert.That(game.Mounted, Is.Empty);
            foreach (var item in game.Items) Assert.That(game.IsOwned(item.Id), Is.True);
        }

        [Test]
        public void DuplicateItemIdsAreRejectedWhenLoadingTheCatalogue()
        {
            Assert.Throws<ArgumentException>(() => new GameSession(new[] { reed, reed }, game.Beacons));
        }

        private void LightFirstBeacon()
        {
            CollectAndMount(reed);
            CollectAndMount(amber);
            game.Ignite();
        }

        private void CollectAndMount(ItemSpec item)
        {
            game.Collect(item.Id);
            game.ToggleMount(item.Id);
        }
    }
}
