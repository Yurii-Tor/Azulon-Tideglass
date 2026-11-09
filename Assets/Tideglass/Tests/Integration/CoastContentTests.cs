using System.Linq;
using NUnit.Framework;
using Tideglass.Content;
using Tideglass.Domain;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
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
                Assert.That(item.UnlockAfter, Is.LessThan(game.Beacons.Count), $"{item.Id} must unlock before the route ends");
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
        public void PlayerStartsInStationAndCanLoadBothInterfaceStyles()
        {
            var firstScene = EditorBuildSettings.scenes.FirstOrDefault(scene => scene.enabled);
            Assert.That(firstScene, Is.Not.Null, "No enabled scene in Build Settings.");
            Assert.That(firstScene.path, Is.EqualTo(ScenePath));
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath), Is.Not.Null);
            Assert.That(Resources.Load<StyleSheet>("Interface/Tideglass"), Is.Not.Null);
            Assert.That(Resources.Load<ThemeStyleSheet>("Interface/TideglassTheme"), Is.Not.Null);
        }

        [Test]
        public void StationSceneUsesTheAuthoredStationAndCardLayouts()
        {
            const string interfacePath = "Assets/Tideglass/Resources/Interface/";
            var layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(interfacePath + "Station.uxml");
            var cardLayout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(interfacePath + "LensCard.uxml");
            Assert.That(layout, Is.Not.Null, "Station.uxml failed to import.");
            Assert.That(cardLayout, Is.Not.Null, "LensCard.uxml failed to import.");

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedForTest = !scene.isLoaded;
            if (openedForTest) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var station = scene.GetRootGameObjects().SingleOrDefault(obj => obj.name == "Tideglass Station");
                Assert.That(station, Is.Not.Null);
                var document = station.GetComponent<UIDocument>();
                Assert.That(document, Is.Not.Null);
                Assert.That(document.visualTreeAsset, Is.EqualTo(layout));

                VisualElement tree = layout.Instantiate();
                Assert.That(tree.Q<Button>("ignite"), Is.Not.Null);
                Assert.That(tree.Q<Label>("beam"), Is.Not.Null);
                Assert.That(tree.Q<VisualElement>("catalogue"), Is.Not.Null);
                var preview = tree.Q<TemplateContainer>("catalogue-preview");
                Assert.That(preview, Is.Not.Null, "Station.uxml needs a catalogue-preview template instance.");
                Assert.That(preview.templateSource, Is.EqualTo(cardLayout));
            }
            finally
            {
                if (openedForTest) EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
