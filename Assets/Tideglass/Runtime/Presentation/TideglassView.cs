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
        ScrollView scroll;
        VisualElement body;
        Label status;
        string message = "Collect a lens from the catalogue, then mount it from your inventory.";

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
            scroll = new ScrollView(ScrollViewMode.Vertical);
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
                    next = root.Q<Button>(name.StartsWith("collect-", StringComparison.Ordinal) ? "jump-inventory" : "jump-catalogue");
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
            var navigation = Box(body, "jump-row");
            JumpButton(navigation, "Catalogue", "catalogue");
            JumpButton(navigation, "Inventory", "inventory");
            var columns = Box(body, "columns");
            DrawCatalogue(columns);
            DrawInventory(columns);
        }

        void JumpButton(VisualElement row, string title, string section)
        {
            var button = new Button(() =>
            {
                var target = root.Q(section);
                scroll.ScrollTo(target);
                var action = target.Query<Button>().Where(b => b.enabledInHierarchy).First();
                action?.Focus();
            }) { name = "jump-" + section, text = title };
            row.Add(button);
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
            Text(controls, session.Finished ? "Every beacon is lit. Ships can follow your lights home." : "Fit two different lenses to match the station’s target.", "copy");
            Text(controls, session.Finished ? "Passage safe" : $"Beam  {session.BeamPower} / {beacon.Target}", "power", "beam");
            if (!session.Finished)
            {
                var track = Box(controls, "track");
                Box(track, "fill").style.width = Length.Percent(Mathf.Clamp01((float)session.BeamPower / beacon.Target) * 100);
                if (session.Mounted.Count == 2 && !session.CanIgnite)
                {
                    int difference = session.BeamPower - beacon.Target;
                    Text(controls, $"{Math.Abs(difference)} {(difference > 0 ? "over" : "under")} target. Swap one lens.", "effect");
                }
                var ignite = Action(controls, "Light beacon", "ignite", session.Ignite);
                ignite.AddToClassList("primary");
                ignite.SetEnabled(session.CanIgnite);
            }
        }

        void DrawCatalogue(VisualElement columns)
        {
            var catalogue = Box(columns, "catalogue", "catalogue");
            Text(catalogue, "Lens catalogue", "section");
            Text(catalogue, "Collect any available lens for free.", "copy");
            var grid = Box(catalogue, "grid");
            foreach (var item in session.Items)
            {
                var card = Box(grid, "card");
                var row = Box(card, "row");
                var image = new Image { image = iconFor(item.Id), scaleMode = ScaleMode.ScaleToFit };
                image.AddToClassList("icon");
                row.Add(image);
                Text(row, item.Title, "item-title");
                string reach = item.UnlockAfter == 0 ? "HARBOR SALVAGE" : $"{item.UnlockAfter} {(item.UnlockAfter == 1 ? "BEACON" : "BEACONS")} TO REACH";
                Text(card, $"POWER {item.BasePower}   /   {reach}", "spec");
                Text(card, item.Description, "copy");
                var note = noteFor(item.Id);
                if (!string.IsNullOrWhiteSpace(note)) Text(card, note, "effect");
                var owned = session.IsOwned(item.Id);
                var unlocked = session.IsUnlocked(item);
                var collect = Action(card, owned ? "Collected" : unlocked ? "Collect lens" : "Locked", "collect-" + item.Id, () => session.Collect(item.Id));
                collect.SetEnabled(!owned && unlocked);
            }
        }

        void DrawInventory(VisualElement columns)
        {
            var inventory = Box(columns, "inventory", "inventory");
            Text(inventory, "Keeper’s inventory", "section");
            Text(inventory, "LENS MOUNTS", "eyebrow");
            for (var i = 0; i < 2; i++)
            {
                var detail = "Empty mount";
                if (i < session.Mounted.Count)
                {
                    var lens = session.Mounted[i];
                    var effective = session.Mounted.Count == 2
                        ? lens.EffectivePower(session.Mounted[1 - i].BasePower) : lens.BasePower;
                    var bonus = effective - lens.BasePower;
                    detail = $"{lens.Title}   ·   {lens.BasePower}";
                    if (bonus != 0) detail += $" ({bonus:+#;-#} bonus)";
                }
                Text(inventory, $"0{i + 1}    {detail}", "slot");
            }
            if (session.CanIgnite)
            {
                var light = Action(inventory, $"Light {session.CurrentBeacon.Title}", "light-from-inventory", session.Ignite);
                light.AddToClassList("primary");
            }
            else if (session.Mounted.Count == 2)
                Text(inventory, "Both sockets are full. Unmount a lens to swap it.", "copy");
            Text(inventory, "OWNED LENSES", "eyebrow");
            var count = 0;
            foreach (var item in session.Items)
            {
                if (!session.IsOwned(item.Id)) continue;
                count++;
                var row = Box(inventory, "owned-row");
                Text(row, $"{item.Title}   ·   {item.BasePower}", "item-title");
                var mounted = session.IsMounted(item.Id);
                var mount = Action(row, mounted ? "Unmount" : "Mount", "mount-" + item.Id, () => session.ToggleMount(item.Id));
                mount.SetEnabled(!session.Finished && (mounted || session.Mounted.Count < 2));
            }
            if (count == 0) Text(inventory, "Collected lenses appear here.", "copy");
        }
    }
}
