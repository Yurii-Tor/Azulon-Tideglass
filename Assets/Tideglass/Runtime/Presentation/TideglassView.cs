using System;
using Tideglass.Domain;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tideglass.Presentation
{
    public sealed class TideglassView
    {
        readonly VisualElement root;
        readonly GameSession session;
        readonly Func<string, Texture2D> iconFor;
        readonly Func<string, string> noteFor;
        VisualElement body;
        Label status;
        string message = "Collect lenses in your bag, then mount two to match the beacon's target.";

        public TideglassView(VisualElement root, GameSession session, Func<string, Texture2D> iconFor, Func<string, string> noteFor)
        {
            this.root = root;
            this.session = session;
            this.iconFor = iconFor;
            this.noteFor = noteFor;
        }

        public void Show()
        {
            root.Clear();
            root.AddToClassList("tideglass");
            root.RegisterCallback<GeometryChangedEvent>(Resize);
            root.EnableInClassList("narrow", root.resolvedStyle.width < 800);
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            root.Add(scroll);
            body = Box(scroll, "page");
            status = Text(root, message, "status", "status");
            Render();
        }

        void Resize(GeometryChangedEvent evt) => root.EnableInClassList("narrow", evt.newRect.width < 800);

        static VisualElement Box(VisualElement parent, string style, string name = null)
        {
            var box = new VisualElement { name = name };
            box.AddToClassList(style);
            parent.Add(box);
            return box;
        }

        static Label Text(VisualElement parent, string value, string style, string name = null)
        {
            var label = new Label(value) { name = name };
            label.AddToClassList(style);
            parent.Add(label);
            return label;
        }

        Button Action(VisualElement parent, string title, string name, Func<string> run)
        {
            var button = new Button(() =>
            {
                bool hadFocus = root.focusController.focusedElement is Button focused && focused.name == name;
                message = run();
                Render();
                if (!hadFocus) return;

                var next = root.Q<Button>(name);
                if (next == null || !next.enabledInHierarchy)
                {
                    var ignite = root.Q<Button>("ignite");
                    next = ignite != null && ignite.enabledInHierarchy
                        ? ignite
                        : root.Q<Button>(className: "collect-lens") ?? root.Q<Button>(className: "mount-lens");
                }
                next?.Focus();
            }) { text = title, name = name };
            parent.Add(button);
            return button;
        }

        void Render()
        {
            body.Clear();
            status.text = message;
            Text(body, "COASTAL SIGNAL SERVICE   /   KEEPER’S DESK", "eyebrow");
            var header = Box(body, "header");
            Text(header, "Tideglass", "masthead");
            Text(header, $"{session.LitCount} / {session.Beacons.Count} beacons lit", "badge", "progress");
            DrawStation();
            DrawCatalogue();
        }

        void DrawStation()
        {
            var station = Box(body, "station");
            var chart = Box(station, "chart");
            chart.Add(new BeaconDrawing(session.Finished || session.CanIgnite));
            var lights = Box(chart, "route-lights");
            for (int i = 0; i < session.Beacons.Count; i++)
                Box(lights, i < session.LitCount ? "route-lit" : "route-unlit");
            var controls = Box(station, "controls");
            var beacon = session.CurrentBeacon;
            Text(controls, session.Finished ? "ROUTE COMPLETE" : $"STATION {session.LitCount + 1:00}   /   {beacon.Location.ToUpperInvariant()}", "eyebrow");
            Text(controls, session.Finished ? "The coast is alight" : beacon.Title, "headline");
            if (session.Finished)
            {
                Text(controls, "Passage safe", "power", "beam");
                Text(controls, "Every beacon is lit. Ships can follow your lights home.", "copy");
            }
            else
            {
                Text(controls, $"Beam power  {session.BeamPower} / {beacon.Target} target", "power", "beam");
                var track = Box(controls, "track");
                Box(track, "fill").style.width = Length.Percent(Mathf.Clamp01((float)session.BeamPower / beacon.Target) * 100);
                DrawSockets(controls);
                Text(controls, BeamHint(beacon.Target), "beam-hint", "beam-hint");
                var ignite = Action(controls, "Light beacon", "ignite", session.Ignite);
                ignite.AddToClassList("primary");
                ignite.SetEnabled(session.CanIgnite);
            }
            DrawInventory(station);
        }

        string BeamHint(int target)
        {
            if (session.Mounted.Count == 0) return "Mount two lenses from your bag to tune the lamp.";
            if (session.Mounted.Count < GameSession.SocketCount) return "Mount one more lens from your bag.";
            if (session.CanIgnite) return "Target matched. Lighting consumes the mounted pair.";

            int difference = session.BeamPower - target;
            return $"{Math.Abs(difference)} {(difference > 0 ? "over" : "under")} target. Unmount a lens and try another.";
        }

        void DrawSockets(VisualElement controls)
        {
            Text(controls, "LAMP SOCKETS", "eyebrow").AddToClassList("sockets-label");
            for (int i = 0; i < GameSession.SocketCount; i++)
            {
                string detail = "Empty";
                if (i < session.Mounted.Count)
                {
                    var lens = session.Mounted[i];
                    int bonus = session.Mounted.Count == GameSession.SocketCount
                        ? lens.EffectivePower(session.Mounted[1 - i].BasePower) - lens.BasePower
                        : 0;
                    detail = bonus == 0
                        ? $"{lens.Title} · {lens.BasePower} power"
                        : $"{lens.Title} · {lens.BasePower} power ({bonus:+#;-#} resonance)";
                }
                Text(controls, $"0{i + 1}   {detail}", "socket", $"socket-{i + 1}");
            }
        }

        void DrawCatalogue()
        {
            var catalogue = Box(body, "catalogue", "catalogue");
            Text(catalogue, "Lens catalogue", "section");
            Text(catalogue, "Collect lenses into your bag. Each lens can light one beacon.", "copy");
            var grid = Box(catalogue, "grid");
            foreach (var item in session.Items)
            {
                var card = Box(grid, "card");
                var row = Box(card, "row");
                var image = new Image { image = iconFor(item.Id), scaleMode = ScaleMode.ScaleToFit };
                image.AddToClassList("icon");
                row.Add(image);
                Text(row, item.Title, "item-title");
                Text(card, $"BASE POWER {item.BasePower}   ·   1 CHARGE", "spec");
                Text(card, item.Description, "copy");
                var note = noteFor(item.Id);
                if (!string.IsNullOrWhiteSpace(note)) Text(card, note, "effect");
                var choice = Box(card, "card-choice");
                var availability = session.AvailabilityFor(item);
                if (availability == LensAvailability.Available)
                {
                    Action(choice, "Collect", "collect-" + item.Id, () => session.Collect(item.Id)).AddToClassList("collect-lens");
                }
                else
                {
                    var label = Text(choice, CatalogueStatus(item, availability), "lens-status", "availability-" + item.Id);
                    label.EnableInClassList("locked", availability == LensAvailability.Locked);
                }
            }
        }

        string CatalogueStatus(ItemSpec item, LensAvailability availability)
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

        void DrawInventory(VisualElement station)
        {
            var inventory = Box(station, "inventory", "inventory");
            var heading = Box(inventory, "inventory-heading");
            Text(heading, "Keeper’s bag", "section");
            Text(heading, $"{session.Inventory.Count} / {GameSession.InventoryCapacity} slots", "spec", "inventory-capacity");
            Text(inventory, "Mount up to two lenses. Unmount to swap them.", "copy");
            for (int i = 0; i < GameSession.InventoryCapacity; i++)
            {
                var slot = Box(inventory, "slot");
                if (i >= session.Inventory.Count)
                {
                    Text(slot, $"0{i + 1}    Empty slot", "slot-empty");
                    continue;
                }

                var lens = session.Inventory[i];
                var icon = new Image { image = iconFor(lens.Id), scaleMode = ScaleMode.ScaleToFit };
                icon.AddToClassList("icon");
                slot.Add(icon);
                var detail = Box(slot, "slot-detail");
                Text(detail, lens.Title, "slot-title");
                bool mounted = session.IsMounted(lens.Id);
                Text(detail, mounted ? $"{lens.BasePower} power · Mounted" : $"{lens.BasePower} power · Spare", "slot-power");
                var action = Action(slot, mounted ? "Unmount" : "Mount", "mount-" + lens.Id, () => session.ToggleMount(lens.Id));
                action.AddToClassList("mount-lens");
                action.SetEnabled(!session.Finished && (mounted || session.Mounted.Count < GameSession.SocketCount));
            }
        }
    }
}
