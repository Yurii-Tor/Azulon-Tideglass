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
            yield return LoadStation();
            VisualElement root = Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
            Assert.That(root.panel, Is.Not.Null);
            Assert.That(root.Q<Label>("progress").text, Does.Contain("0 / 3"));
            Assert.That(root.Q<Label>("inventory-capacity").text, Is.EqualTo("0 / 3 slots"));
            Assert.That(root.Q<Label>("beam").text, Does.Contain("0 / 5"));
            Assert.That(root.Q<Label>("socket-1").text, Does.Contain("Empty"));
            Assert.That(root.Q<Label>("socket-2").text, Does.Contain("Empty"));
            ExpectUnavailable(root, "fog-pearl", "Light Dock Lantern to unlock.");
            ExpectUnavailable(root, "anchor-glass", "Light Dock Lantern to unlock.");
            ExpectUnavailable(root, "crown-glass", "Light Bell Buoy to unlock.");

            Submit(root, "collect-saltglass");
            Submit(root, "collect-copper-eye");
            Submit(root, "collect-seafoam-wheel");
            Assert.That(root.Q<Label>("inventory-capacity").text, Is.EqualTo("3 / 3 slots"));
            Assert.That(root.Q<Label>("beam").text, Does.Contain("0 / 5"));
            Assert.That(root.Q<Label>("socket-1").text, Does.Contain("Empty"));
            Assert.That(root.Q<Label>("socket-2").text, Does.Contain("Empty"));
            ExpectUnavailable(root, "saltglass", "Collected");

            Submit(root, "mount-saltglass");
            Submit(root, "mount-seafoam-wheel");
            Assert.That(root.Q<Label>("beam").text, Does.Contain("6 / 5"));
            Assert.That(root.Q<Label>("socket-1").text, Does.Contain("Saltglass"));
            Assert.That(root.Q<Label>("socket-2").text, Does.Contain("Seafoam Wheel"));
            Assert.That(root.Q<Button>("ignite").enabledInHierarchy, Is.False);
            Assert.That(root.Q<Button>("mount-copper-eye").enabledInHierarchy, Is.False);
            Assert.That(root.Q<Label>("beam-hint").text, Does.Contain("1 over target"));
            Submit(root, "mount-seafoam-wheel");
            Assert.That(root.Q<Button>("mount-seafoam-wheel").text, Is.EqualTo("Mount"));
            Assert.That(root.Q<Label>("inventory-capacity").text, Is.EqualTo("3 / 3 slots"));
            Submit(root, "mount-copper-eye");
            Assert.That(root.Q<Label>("beam").text, Does.Contain("5 / 5"));
            Assert.That(root.Q<Label>("beam-hint").text, Does.Contain("consumes the mounted pair"));

            Submit(root, "ignite");
            yield return null;
            Assert.That(root.Q<Label>("progress").text, Does.Contain("1 / 3"));
            Assert.That(root.Q<Label>("beam").text, Does.Contain("0 / 8"));
            Assert.That(root.Q<Label>("inventory-capacity").text, Is.EqualTo("1 / 3 slots"));
            Assert.That(root.Q<Button>("mount-seafoam-wheel"), Is.Not.Null);
            ExpectUnavailable(root, "saltglass", "Spent");
            ExpectUnavailable(root, "copper-eye", "Spent");
            ExpectUnavailable(root, "crown-glass", "Light Bell Buoy to unlock.");

            Submit(root, "collect-twinwake-prism");
            Submit(root, "mount-seafoam-wheel");
            Submit(root, "mount-twinwake-prism");
            Assert.That(root.Q<Label>("beam").text, Does.Contain("8 / 8"));
            Submit(root, "ignite");
            yield return null;
            Assert.That(root.Q<Label>("progress").text, Does.Contain("2 / 3"));
            Assert.That(root.Q<Label>("beam").text, Does.Contain("0 / 10"));

            Submit(root, "collect-storm-fresnel");
            Submit(root, "collect-fog-pearl");
            Submit(root, "mount-storm-fresnel");
            Submit(root, "mount-fog-pearl");
            Assert.That(root.Q<Label>("beam").text, Does.Contain("10 / 10"));
            Submit(root, "ignite");
            yield return null;
            Assert.That(root.Q<Label>("progress").text, Does.Contain("3 / 3"));
            Assert.That(root.Q<Label>("beam").text, Is.EqualTo("Passage safe"));
            Assert.That(root.Q<Button>("ignite"), Is.Null);
            ExpectUnavailable(root, "storm-fresnel", "Spent");
            ExpectUnavailable(root, "fog-pearl", "Spent");
        }

        [UnityTest]
        public IEnumerator NarrowLayoutCanScrollToTheLastCatalogueCard()
        {
            yield return LoadStation();
            VisualElement root = Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
            root.style.width = 600;
            root.style.height = 720;
            yield return null;
            yield return null;

            var scroll = root.Q<ScrollView>();
            var lastCard = root.Q<Label>("availability-crown-glass");
            Assert.That(root.ClassListContains("narrow"), Is.True);
            Assert.That(lastCard, Is.Not.Null);
            scroll.ScrollTo(lastCard);
            yield return null;
            yield return null;
            Assert.That(scroll.scrollOffset.y, Is.GreaterThan(0));
            Assert.That(lastCard.worldBound.yMin, Is.GreaterThanOrEqualTo(scroll.contentViewport.worldBound.yMin - 1));
            Assert.That(lastCard.worldBound.yMax, Is.LessThanOrEqualTo(scroll.contentViewport.worldBound.yMax + 1));
        }

        private static IEnumerator LoadStation()
        {
            int index = SceneUtility.GetBuildIndexByScenePath("Assets/Tideglass/Scenes/Station.unity");
            Assert.That(index, Is.GreaterThanOrEqualTo(0), "Station must be in the build scene list.");
            yield return SceneManager.LoadSceneAsync(index);
            yield return null;
        }

        private static void ExpectUnavailable(VisualElement root, string id, string reason)
        {
            Assert.That(root.Q<Button>("collect-" + id), Is.Null);
            Assert.That(root.Q<Label>("availability-" + id)?.text, Does.Contain(reason));
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
