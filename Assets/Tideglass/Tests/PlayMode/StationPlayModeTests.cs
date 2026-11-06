using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Tideglass.Tests
{
    public class StationPlayModeTests
    {
        [UnityTest]
        public IEnumerator KeeperCanRestoreTheCoastThroughTheStationControls()
        {
            const string scenePath = "Assets/Tideglass/Scenes/Station.unity";
            int stationIndex = SceneUtility.GetBuildIndexByScenePath(scenePath);
            Assert.That(stationIndex, Is.GreaterThanOrEqualTo(0), "Station must be in the build scene list.");
            yield return SceneManager.LoadSceneAsync(stationIndex);
            yield return null;

            var document = Object.FindFirstObjectByType<UIDocument>();
            Assert.That(document, Is.Not.Null);
            VisualElement root = document.rootVisualElement;
            Assert.That(root.panel, Is.Not.Null);
            Assert.That(root.Q<Label>("progress").text, Does.Contain("0 / 3"));
            Assert.That(root.Q("catalogue").Q<Button>("collect-saltglass"), Is.Not.Null);
            Assert.That(root.Q("inventory").Q<Button>("mount-saltglass"), Is.Null);
            Assert.That(root.Q<Button>("collect-fog-pearl").enabledInHierarchy, Is.False);
            Assert.That(root.Q<Button>("ignite").enabledInHierarchy, Is.False);

            Submit(root, "collect-saltglass");
            Submit(root, "collect-copper-eye");
            Submit(root, "collect-seafoam-wheel");
            yield return null;
            Assert.That(root.Q<Button>("collect-saltglass").enabledInHierarchy, Is.False);
            Assert.That(root.Q("inventory").Q<Button>("mount-copper-eye"), Is.Not.Null);

            Submit(root, "mount-saltglass");
            Assert.That(root.Q<Label>("beam").text, Does.Contain("2 / 5"));
            Submit(root, "mount-seafoam-wheel");
            Assert.That(root.Q<Label>("beam").text, Does.Contain("6 / 5"));
            Assert.That(root.Q<Button>("ignite").enabledInHierarchy, Is.False);
            Submit(root, "mount-seafoam-wheel");
            Submit(root, "mount-copper-eye");
            yield return null;
            Assert.That(root.Q<Label>("beam").text, Does.Contain("5 / 5"));
            Assert.That(root.Q<Label>("progress").text, Does.Contain("0 / 3"));

            Submit(root, "ignite");
            yield return null;
            Assert.That(root.Q<Label>("progress").text, Does.Contain("1 / 3"));
            Assert.That(root.Q<Label>("beam").text, Does.Contain("0 / 8"));
            Assert.That(root.Q<Button>("collect-fog-pearl").enabledInHierarchy, Is.True);
            Assert.That(root.Q("inventory").Q<Button>("mount-saltglass"), Is.Not.Null);
            Assert.That(root.Q<Button>("collect-storm-fresnel").enabledInHierarchy, Is.False);

            Submit(root, "collect-twinwake-prism");
            Submit(root, "mount-seafoam-wheel");
            Submit(root, "mount-twinwake-prism");
            Assert.That(root.Q<Label>("beam").text, Does.Contain("8 / 8"), "The prism adds a bonus with an even partner.");
            Submit(root, "light-from-inventory");
            yield return null;
            Assert.That(root.Q<Label>("progress").text, Does.Contain("2 / 3"));
            Assert.That(root.Q<Label>("beam").text, Does.Contain("0 / 10"));

            Submit(root, "collect-storm-fresnel");
            Submit(root, "mount-storm-fresnel");
            Submit(root, "mount-twinwake-prism");
            Assert.That(root.Q<Label>("beam").text, Does.Contain("10 / 10"));
            Submit(root, "ignite");
            yield return null;
            Assert.That(root.Q<Label>("progress").text, Does.Contain("3 / 3"));
            Assert.That(root.Q<Label>("beam").text, Is.EqualTo("Passage safe"));
            Assert.That(root.Q<Button>("ignite"), Is.Null);
            Assert.That(root.Q("inventory").Q<Button>("mount-saltglass").enabledInHierarchy, Is.False);
        }

        private static void Submit(VisualElement root, string name)
        {
            Button button = root.Q<Button>(name);
            Assert.That(button, Is.Not.Null, $"No button named {name}");
            Assert.That(button.enabledInHierarchy, Is.True, $"{name} is disabled");
            button.Focus();
            using (var submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = button;
                button.SendEvent(submit);
            }
        }
    }
}
