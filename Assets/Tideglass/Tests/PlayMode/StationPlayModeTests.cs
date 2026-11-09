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

            yield return Submit(root, "collect-saltglass");
            Assert.That(root.focusController.focusedElement, Is.Not.SameAs(root.Q<Button>("collect-saltglass")));
            ExpectActionFocus(root);
            yield return Submit(root, "collect-copper-eye");
            yield return Submit(root, "collect-seafoam-wheel");
            Assert.That(root.Q<Label>("inventory-capacity").text, Is.EqualTo("3 / 3 slots"));
            Assert.That(root.Q<Label>("beam").text, Does.Contain("0 / 5"));
            Assert.That(root.Q<Label>("socket-1").text, Does.Contain("Empty"));
            Assert.That(root.Q<Label>("socket-2").text, Does.Contain("Empty"));
            ExpectUnavailable(root, "saltglass", "Collected");

            yield return Submit(root, "mount-saltglass");
            Assert.That(root.focusController.focusedElement, Is.SameAs(root.Q<Button>("mount-saltglass")));
            yield return Submit(root, "mount-seafoam-wheel");
            Assert.That(root.Q<Label>("beam").text, Does.Contain("6 / 5"));
            Assert.That(root.Q<Label>("socket-1").text, Does.Contain("Saltglass"));
            Assert.That(root.Q<Label>("socket-2").text, Does.Contain("Seafoam Wheel"));
            Assert.That(root.Q<Button>("ignite").enabledInHierarchy, Is.False);
            Assert.That(root.Q<Button>("mount-copper-eye").enabledInHierarchy, Is.False);
            Assert.That(root.Q<Label>("beam-hint").text, Does.Contain("1 over target"));
            yield return Submit(root, "mount-seafoam-wheel");
            Assert.That(root.Q<Button>("mount-seafoam-wheel").text, Is.EqualTo("Mount"));
            Assert.That(root.focusController.focusedElement, Is.SameAs(root.Q<Button>("mount-seafoam-wheel")));
            Assert.That(root.Q<Label>("inventory-capacity").text, Is.EqualTo("3 / 3 slots"));
            yield return Submit(root, "mount-copper-eye");
            Assert.That(root.Q<Label>("beam").text, Does.Contain("5 / 5"));
            Assert.That(root.Q<Label>("beam-hint").text, Does.Contain("consumes the mounted pair"));

            yield return Submit(root, "ignite");
            Assert.That(root.focusController.focusedElement, Is.Not.SameAs(root.Q<Button>("ignite")));
            ExpectActionFocus(root);
            Assert.That(root.Q<Label>("progress").text, Does.Contain("1 / 3"));
            Assert.That(root.Q<Label>("beam").text, Does.Contain("0 / 8"));
            Assert.That(root.Q<Label>("inventory-capacity").text, Is.EqualTo("1 / 3 slots"));
            Assert.That(root.Q<Button>("mount-seafoam-wheel"), Is.Not.Null);
            ExpectUnavailable(root, "saltglass", "Spent");
            ExpectUnavailable(root, "copper-eye", "Spent");
            ExpectUnavailable(root, "crown-glass", "Light Bell Buoy to unlock.");

            yield return Submit(root, "collect-twinwake-prism");
            yield return Submit(root, "mount-seafoam-wheel");
            yield return Submit(root, "mount-twinwake-prism");
            Assert.That(root.Q<Label>("beam").text, Does.Contain("8 / 8"));
            yield return Submit(root, "ignite");
            Assert.That(root.Q<Label>("progress").text, Does.Contain("2 / 3"));
            Assert.That(root.Q<Label>("beam").text, Does.Contain("0 / 10"));

            yield return Submit(root, "collect-storm-fresnel");
            yield return Submit(root, "collect-fog-pearl");
            yield return Submit(root, "mount-storm-fresnel");
            yield return Submit(root, "mount-fog-pearl");
            Assert.That(root.Q<Label>("beam").text, Does.Contain("10 / 10"));
            yield return Submit(root, "ignite");
            Assert.That(root.Q<Label>("progress").text, Does.Contain("3 / 3"));
            Assert.That(IsShown(root.Q<Label>("beam")), Is.False);
            var completed = root.Q<Label>("completed-beam");
            Assert.That(IsShown(completed), Is.True);
            Assert.That(completed.text, Is.EqualTo("Passage safe"));
            ExpectHiddenButton(root, "ignite");
            ExpectUnavailable(root, "storm-fresnel", "Spent");
            ExpectUnavailable(root, "fog-pearl", "Spent");
        }

        [UnityTest]
        public IEnumerator ActionsPreserveRearrangedStationAndCardElements()
        {
            yield return LoadStation();
            VisualElement root = Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
            var masthead = root.Q<Label>("masthead");
            var ignite = root.Q<Button>("ignite");
            var indicator = root.Q<VisualElement>("beam-indicator");
            var card = root.Q<VisualElement>("card-saltglass");
            var title = card.Q<Label>("lens-title");
            var icon = card.Q<VisualElement>("lens-icon");

            // Simulate design edits on the live clone without modifying shared UXML assets.
            masthead.text = "Keeper's test desk";
            ignite.text = "Send signal";
            MoveBefore(ignite, indicator);
            MoveBefore(title, icon);

            yield return Submit(root, "collect-saltglass");
            yield return Submit(root, "collect-copper-eye");
            yield return Submit(root, "mount-saltglass");
            yield return Submit(root, "mount-copper-eye");

            Assert.That(root.Q<Label>("masthead").text, Is.EqualTo("Keeper's test desk"));
            Assert.That(root.Q<Button>("ignite"), Is.SameAs(ignite));
            Assert.That(ignite.text, Is.EqualTo("Send signal"));
            Assert.That(ignite.parent, Is.SameAs(indicator.parent));
            Assert.That(ignite.parent.IndexOf(ignite), Is.LessThan(ignite.parent.IndexOf(indicator)));
            Assert.That(root.Q<VisualElement>("card-saltglass"), Is.SameAs(card));
            Assert.That(card.Q<Label>("lens-title"), Is.SameAs(title));
            Assert.That(title.parent, Is.SameAs(icon.parent));
            Assert.That(title.parent.IndexOf(title), Is.LessThan(title.parent.IndexOf(icon)));

            yield return Submit(root, "ignite");
            Assert.That(root.Q<Label>("progress").text, Does.Contain("1 / 3"));
            Assert.That(root.Q<Label>("beam").text, Does.Contain("0 / 8"));
        }

        [UnityTest]
        public IEnumerator WideAndNarrowLayoutsCanScrollToTheLastCatalogueCard()
        {
            yield return LoadStation();
            VisualElement root = Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
            root.style.height = 720;
            var scroll = root.Q<ScrollView>();
            var lastCard = root.Q<Label>("availability-crown-glass");
            Assert.That(lastCard, Is.Not.Null);
            foreach (int width in new[] { 1280, 600 })
            {
                root.style.width = width;
                yield return null;
                yield return null;
                Assert.That(root.layout.width, Is.EqualTo(width).Within(1));
                Assert.That(root.ClassListContains("narrow"), Is.EqualTo(width == 600));
                scroll.ScrollTo(lastCard);
                yield return null;
                yield return null;
                Assert.That(scroll.scrollOffset.y, Is.GreaterThan(0));
                Assert.That(lastCard.worldBound.yMin, Is.GreaterThanOrEqualTo(scroll.contentViewport.worldBound.yMin - 1));
                Assert.That(lastCard.worldBound.yMax, Is.LessThanOrEqualTo(scroll.contentViewport.worldBound.yMax + 1));
            }
        }

        private static IEnumerator LoadStation()
        {
            int index = SceneUtility.GetBuildIndexByScenePath("Assets/Tideglass/Scenes/Station.unity");
            Assert.That(index, Is.GreaterThanOrEqualTo(0), "Station must be in the build scene list.");
            yield return SceneManager.LoadSceneAsync(index);
            yield return null;
            yield return null;
        }

        private static void ExpectUnavailable(VisualElement root, string id, string reason)
        {
            ExpectHiddenButton(root, "collect-" + id);
            var status = root.Q<Label>("availability-" + id);
            Assert.That(IsShown(status), Is.True, $"No visible availability status for {id}");
            Assert.That(status.text, Does.Contain(reason));
        }

        private static void ExpectHiddenButton(VisualElement root, string name)
        {
            var button = root.Q<Button>(name);
            Assert.That(button, Is.Not.Null, $"No button named {name}");
            Assert.That(IsShown(button), Is.False, $"{name} is still displayed");
            Assert.That(button.enabledInHierarchy, Is.False, $"{name} can still be activated");
        }

        private static void ExpectActionFocus(VisualElement root)
        {
            var button = root.focusController.focusedElement as Button;
            Assert.That(IsShown(button), Is.True, "Focus must move to a visible action");
            Assert.That(button.enabledInHierarchy, Is.True, "Focus must move to an enabled action");
        }

        private static bool IsShown(VisualElement element)
        {
            if (element?.panel == null) return false;
            for (VisualElement current = element; current != null; current = current.parent)
            {
                if (current.resolvedStyle.display == DisplayStyle.None ||
                    current.resolvedStyle.visibility == Visibility.Hidden)
                    return false;
            }
            return true;
        }

        private static void MoveBefore(VisualElement element, VisualElement sibling)
        {
            element.RemoveFromHierarchy();
            sibling.parent.Insert(sibling.parent.IndexOf(sibling), element);
        }

        private static IEnumerator Submit(VisualElement root, string name)
        {
            Button button = root.Q<Button>(name);
            Assert.That(button, Is.Not.Null, $"No button named {name}");
            Assert.That(IsShown(button), Is.True, $"{name} is hidden");
            Assert.That(button.enabledInHierarchy, Is.True, $"{name} is disabled");
            button.Focus();
            using (var submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = button;
                button.SendEvent(submit);
            }
            yield return null;
        }
    }
}
