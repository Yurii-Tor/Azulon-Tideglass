using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Tideglass.Content;
using Tideglass.Domain;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tideglass.Tests
{
    public class CoastContentTests
    {
        private const string CoastPath = "Assets/Tideglass/Data/Coast.asset";
        private const string ScenePath = "Assets/Tideglass/Scenes/Station.unity";
        private CoastDefinition coast;
        private GameSession game;

        [SetUp]
        public void LoadCatalogue()
        {
            coast = AssetDatabase.LoadAssetAtPath<CoastDefinition>(CoastPath);
            Assert.That(coast, Is.Not.Null, $"Missing coast asset at {CoastPath}");
            game = coast.CreateSession();
        }

        [Test]
        public void EachCatalogueEntryHasDisplayContentAndAnImportedIcon()
        {
            Assert.That(game.Items, Is.Not.Empty);
            foreach (ItemSpec item in game.Items)
            {
                Assert.That(string.IsNullOrWhiteSpace(item.Title), Is.False, item.Id);
                Assert.That(string.IsNullOrWhiteSpace(item.Description), Is.False, item.Id);
                Texture2D icon = coast.IconFor(item.Id);
                Assert.That(icon, Is.Not.Null, $"No imported icon for {item.Id}");
                Assert.That(AssetDatabase.GetAssetPath(icon), Does.StartWith("Assets/Tideglass/"), item.Id);
            }
        }

        [TestCase(false, "assign a lens to catalogue slot 2")]
        [TestCase(true, "duplicate lens ID 'saltglass' at catalogue slot 2")]
        public void InvalidLensSlotsIdentifyTheAuthoringProblem(bool duplicate, string diagnostic)
        {
            var invalidCoast = ScriptableObject.CreateInstance<CoastDefinition>();
            try
            {
                var lens = AssetDatabase.LoadAssetAtPath<LensDefinition>("Assets/Tideglass/Data/Lenses/Saltglass.asset");
                Assert.That(lens, Is.Not.Null);
                using var serializedCoast = new SerializedObject(invalidCoast);
                var lenses = serializedCoast.FindProperty("lenses");
                lenses.arraySize = 2;
                lenses.GetArrayElementAtIndex(0).objectReferenceValue = lens;
                lenses.GetArrayElementAtIndex(1).objectReferenceValue = duplicate ? lens : null;
                serializedCoast.ApplyModifiedPropertiesWithoutUndo();

                var error = Assert.Throws<System.InvalidOperationException>(() => invalidCoast.CreateSession());
                Assert.That(error.Message, Does.Contain(diagnostic));
            }
            finally
            {
                Object.DestroyImmediate(invalidCoast);
            }
        }

        [Test]
        public void EachBeaconAddsNewSalvageAndCanBeLitWithTwoReachableLenses()
        {
            var inventory = new List<ItemSpec>();
            Assert.That(game.Beacons, Is.Not.Empty);
            for (int stage = 0; stage < game.Beacons.Count; stage++)
            {
                BeaconSpec beacon = game.CurrentBeacon;
                Assert.That(beacon, Is.Not.Null);
                int previouslyOwned = inventory.Count;
                foreach (ItemSpec item in game.Items)
                {
                    if (!game.IsUnlocked(item) || game.IsOwned(item.Id)) continue;
                    game.Collect(item.Id);
                    Assert.That(game.IsOwned(item.Id), Is.True, item.Id);
                    inventory.Add(item);
                }

                Assert.That(inventory.Count, Is.GreaterThan(previouslyOwned), $"No new salvage for {beacon.Title}");
                Assert.That(MountMatchingPair(inventory), Is.True, $"No reachable pair gives {beacon.Target} for {beacon.Title}");
                game.Ignite();
                Assert.That(game.LitCount, Is.EqualTo(stage + 1));
            }
            Assert.That(game.Finished, Is.True);
            Assert.That(game.Items.All(item => game.IsOwned(item.Id)), Is.True, "Some catalogue entries are unreachable.");
        }

        private bool MountMatchingPair(List<ItemSpec> inventory)
        {
            for (int first = 0; first < inventory.Count; first++)
            {
                game.ToggleMount(inventory[first].Id);
                for (int second = first + 1; second < inventory.Count; second++)
                {
                    game.ToggleMount(inventory[second].Id);
                    if (game.CanIgnite) return true;
                    game.ToggleMount(inventory[second].Id);
                }
                game.ToggleMount(inventory[first].Id);
            }
            return false;
        }

        [Test]
        public void PlayerStartsInStationAndCanLoadBothInterfaceStyles()
        {
            var firstScene = EditorBuildSettings.scenes.FirstOrDefault(scene => scene.enabled);
            Assert.That(firstScene, Is.Not.Null, "No enabled scene in Build Settings.");
            Assert.That(firstScene.path, Is.EqualTo(ScenePath));
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath), Is.Not.Null);
            Assert.That(Resources.Load<StyleSheet>("Interface/Tideglass"), Is.Not.Null);
            Assert.That(Resources.Load<ThemeStyleSheet>("Interface/TideglassTheme"), Is.Not.Null);
        }
    }
}
