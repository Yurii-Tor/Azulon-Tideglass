using System;
using System.Collections.Generic;
using Tideglass.Domain;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tideglass.Presentation
{
    internal sealed class TideglassView
    {
        private readonly VisualElement root;
        private readonly GameSession session;
        private readonly Func<string, Texture2D> iconFor;
        private readonly List<LensCard> cards = new List<LensCard>();
        private readonly BagSlot[] bag = new BagSlot[GameSession.InventoryCapacity];
        private readonly Label[] sockets = new Label[GameSession.SocketCount];
        private readonly VisualElement[] routeLights;
        private readonly VisualElement beaconUnlit;
        private readonly VisualElement beaconLit;
        private readonly VisualElement beamIndicator;
        private readonly VisualElement beamFill;
        private readonly VisualElement lampSockets;
        private readonly VisualElement routeComplete;
        private readonly Label progress;
        private readonly Label beaconLocation;
        private readonly Label beaconTitle;
        private readonly Label beam;
        private readonly Label beamHint;
        private readonly Label inventoryCapacity;
        private readonly Label status;
        private readonly Button ignite;

        public TideglassView(VisualElement root, GameSession session,
            Func<string, Texture2D> iconFor, Func<string, string> noteFor)
        {
            this.root = root;
            this.session = session;
            this.iconFor = iconFor;
            var preview = Find<TemplateContainer>(root, "catalogue-preview");
            var lensCard = preview.templateSource;
            if (lensCard == null)
                throw new InvalidOperationException("Assign LensCard.uxml to the catalogue-preview template in Station.uxml.");

            beaconUnlit = Find<VisualElement>(root, "beacon-unlit");
            beaconLit = Find<VisualElement>(root, "beacon-lit");
            beamIndicator = Find<VisualElement>(root, "beam-indicator");
            beamFill = Find<VisualElement>(root, "beam-fill");
            lampSockets = Find<VisualElement>(root, "lamp-sockets");
            routeComplete = Find<VisualElement>(root, "route-complete");
            progress = Find<Label>(root, "progress");
            beaconLocation = Find<Label>(root, "beacon-location");
            beaconTitle = Find<Label>(root, "beacon-title");
            beam = Find<Label>(root, "beam");
            beamHint = Find<Label>(root, "beam-hint");
            inventoryCapacity = Find<Label>(root, "inventory-capacity");
            status = Find<Label>(root, "status");
            ignite = Find<Button>(root, "ignite");
            ignite.clicked += () => Run(ignite, session.Ignite);

            for (int i = 0; i < sockets.Length; i++)
                sockets[i] = Find<Label>(root, $"socket-{i + 1}");

            for (int i = 0; i < bag.Length; i++)
            {
                int slotIndex = i;
                var slot = new BagSlot(Find<VisualElement>(root, $"bag-slot-{i + 1}"), i + 1);
                slot.Action.clicked += () => Run(slot.Action, () => session.ToggleMount(session.Inventory[slotIndex].Id));
                bag[i] = slot;
            }

            var lights = Find<VisualElement>(root, "route-lights");
            lights.Q("route-preview")?.RemoveFromHierarchy();
            routeLights = new VisualElement[session.Beacons.Count];
            for (int i = 0; i < routeLights.Length; i++)
            {
                routeLights[i] = new VisualElement();
                lights.Add(routeLights[i]);
            }

            preview.RemoveFromHierarchy();
            var grid = Find<VisualElement>(root, "catalogue-grid");
            foreach (var item in session.Items)
            {
                var instance = lensCard.Instantiate();
                instance.name = "card-" + item.Id;
                instance.AddToClassList("card-host");
                var card = new LensCard(instance, item, iconFor(item.Id), noteFor(item.Id));
                card.Collect.clicked += () => Run(card.Collect, () => session.Collect(item.Id));
                cards.Add(card);
                grid.Add(instance);
            }

            root.RegisterCallback<GeometryChangedEvent>(evt => SetWidth(evt.newRect.width));
            SetWidth(root.resolvedStyle.width);
            Refresh();
        }

        private static T Find<T>(VisualElement parent, string name) where T : VisualElement =>
            parent.Q<T>(name) ?? throw new InvalidOperationException($"The UI layout needs a {typeof(T).Name} named '{name}'.");

        private static void Show(VisualElement element, bool visible) =>
            element.EnableInClassList("is-hidden", !visible);

        private void SetWidth(float width) => root.EnableInClassList("narrow", width < 800);

        private void Run(Button source, Func<string> action)
        {
            if (!source.enabledInHierarchy) return;
            bool hadFocus = source.focusController?.focusedElement == source;
            status.text = action();
            Refresh();
            if (hadFocus && !source.enabledInHierarchy && !FocusNextAction())
                root.schedule.Execute(() => FocusNextAction());
        }

        private bool FocusNextAction()
        {
            if (root.focusController?.focusedElement is Button active && active.enabledInHierarchy) return true;
            if (TryFocus(ignite)) return true;

            foreach (var card in cards)
                if (TryFocus(card.Collect)) return true;

            foreach (var slot in bag)
                if (TryFocus(slot.Action)) return true;

            return false;
        }

        private static bool TryFocus(Button button)
        {
            if (!button.canGrabFocus) return false;
            // Newly unlocked actions may still have the previous layout until the panel updates.
            for (VisualElement element = button; element != null; element = element.parent)
                if (element.resolvedStyle.display == DisplayStyle.None) return false;

            button.Focus();
            return true;
        }

        private void Refresh()
        {
            progress.text = $"{session.LitCount} / {session.Beacons.Count}";
            bool finished = session.Finished;
            bool canIgnite = session.CanIgnite;
            bool shining = finished || canIgnite;
            Show(beaconUnlit, !shining);
            Show(beaconLit, shining);
            for (int i = 0; i < routeLights.Length; i++)
            {
                bool lit = i < session.LitCount;
                routeLights[i].EnableInClassList("route-lit", lit);
                routeLights[i].EnableInClassList("route-unlit", !lit);
            }

            Show(beaconLocation, !finished);
            Show(beaconTitle, !finished);
            Show(beamIndicator, !finished);
            Show(lampSockets, !finished);
            Show(beamHint, !finished);
            Show(ignite, !finished);
            Show(routeComplete, finished);
            ignite.SetEnabled(canIgnite);
            if (!finished)
            {
                var beacon = session.CurrentBeacon;
                beaconLocation.text = $"STATION {session.LitCount + 1:00}   /   {beacon.Location.ToUpperInvariant()}";
                beaconTitle.text = beacon.Title;
                int power = session.BeamPower;
                beam.text = $"{power} / {beacon.Target}";
                beamFill.style.width = Length.Percent(Mathf.Clamp01((float)power / beacon.Target) * 100);
                beamHint.text = BeamHint(power, beacon.Target, canIgnite);
                for (int i = 0; i < sockets.Length; i++) sockets[i].text = SocketDetail(i);
            }

            inventoryCapacity.text = $"{session.Inventory.Count} / {GameSession.InventoryCapacity} slots";
            for (int i = 0; i < bag.Length; i++)
            {
                var item = i < session.Inventory.Count ? session.Inventory[i] : null;
                bool mounted = item != null && session.IsMounted(item.Id);
                bool canMount = !finished && (mounted || session.Mounted.Count < GameSession.SocketCount);
                bag[i].Update(item, item == null ? null : iconFor(item.Id), mounted, canMount);
            }

            foreach (var card in cards)
            {
                var availability = session.AvailabilityFor(card.Item);
                card.Update(availability, CatalogueStatus(card.Item, availability));
            }
        }

        private string BeamHint(int power, int target, bool canIgnite)
        {
            if (session.Mounted.Count == 0) return "Mount two lenses from your bag to tune the lamp.";
            if (session.Mounted.Count < GameSession.SocketCount) return "Mount one more lens from your bag.";
            if (canIgnite) return "Target matched. Lighting consumes the mounted pair.";

            int difference = power - target;
            return $"{Math.Abs(difference)} {(difference > 0 ? "over" : "under")} target. Unmount a lens and try another.";
        }

        private string SocketDetail(int index)
        {
            if (index >= session.Mounted.Count) return "Empty";
            var lens = session.Mounted[index];
            int bonus = session.Mounted.Count == GameSession.SocketCount
                ? lens.EffectivePower(session.Mounted[1 - index].BasePower) - lens.BasePower
                : 0;
            return bonus == 0
                ? $"{lens.Title} · {lens.BasePower} power"
                : $"{lens.Title} · {lens.BasePower} power ({bonus:+#;-#} resonance)";
        }

        private string CatalogueStatus(ItemSpec item, LensAvailability availability)
        {
            switch (availability)
            {
                case LensAvailability.Locked:
                    return item.UnlockAfter < session.Beacons.Count
                        ? $"Light {session.Beacons[item.UnlockAfter - 1].Title} to unlock."
                        : "Unavailable on this route.";
                case LensAvailability.InInventory: return "Collected · in your bag";
                case LensAvailability.InventoryFull: return "Bag full. Light a beacon to make room.";
                case LensAvailability.Spent: return "Spent at a beacon";
                case LensAvailability.RouteComplete: return "Route complete";
                default: return string.Empty;
            }
        }

        private sealed class LensCard
        {
            private readonly Label status;
            public ItemSpec Item { get; }
            public Button Collect { get; }

            public LensCard(VisualElement root, ItemSpec item, Texture2D icon, string note)
            {
                Item = item;
                Find<VisualElement>(root, "lens-icon").style.backgroundImage = new StyleBackground(icon);
                Find<Label>(root, "lens-title").text = item.Title;
                Find<Label>(root, "lens-power").text = item.BasePower.ToString();
                Find<Label>(root, "lens-description").text = item.Description;
                var effect = Find<Label>(root, "lens-effect");
                effect.text = note;
                Show(effect, !string.IsNullOrWhiteSpace(note));
                status = Find<Label>(root, "lens-availability");
                status.name = "availability-" + item.Id;
                Collect = Find<Button>(root, "lens-collect");
                Collect.name = "collect-" + item.Id;
            }

            public void Update(LensAvailability availability, string reason)
            {
                bool available = availability == LensAvailability.Available;
                Collect.SetEnabled(available);
                Show(Collect, available);
                Show(status, !available);
                status.text = reason;
                status.EnableInClassList("locked", availability == LensAvailability.Locked);
            }
        }

        private sealed class BagSlot
        {
            private readonly VisualElement empty;
            private readonly VisualElement filled;
            private readonly VisualElement icon;
            private readonly Label title;
            private readonly Label power;
            private readonly string emptyActionName;
            private readonly string mountCaption;
            public Button Action { get; }

            public BagSlot(VisualElement root, int number)
            {
                empty = Find<VisualElement>(root, "slot-empty");
                filled = Find<VisualElement>(root, "slot-filled");
                icon = Find<VisualElement>(root, "slot-icon");
                title = Find<Label>(root, "slot-title");
                power = Find<Label>(root, "slot-power");
                Action = Find<Button>(root, "slot-action");
                mountCaption = Action.text;
                emptyActionName = $"mount-empty-{number}";
                Find<Label>(root, "slot-number").text = number.ToString("00");
            }

            public void Update(ItemSpec item, Texture2D texture, bool mounted, bool canMount)
            {
                bool occupied = item != null;
                Show(empty, !occupied);
                Show(filled, occupied);
                Action.SetEnabled(occupied && canMount);
                Action.name = occupied ? "mount-" + item.Id : emptyActionName;
                if (!occupied) return;

                icon.style.backgroundImage = new StyleBackground(texture);
                title.text = item.Title;
                power.text = $"{item.BasePower} power · {(mounted ? "Mounted" : "Spare")}";
                Action.text = mounted ? "Unmount" : mountCaption;
            }
        }
    }
}
