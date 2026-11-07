using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Tideglass.Content;
using Tideglass.Domain;
using UnityEditor;

namespace Tideglass.Tests
{
    public class CoastRouteTests
    {
        private CoastDefinition coast;
        private GameSession game;

        [SetUp]
        public void LoadCoast()
        {
            coast = AssetDatabase.LoadAssetAtPath<CoastDefinition>("Assets/Tideglass/Data/Coast.asset");
            Assert.That(coast, Is.Not.Null);
            game = coast.CreateSession();
        }

        [Test]
        public void MainRouteKeepsTheThirdLensAndUsesBothResonanceLenses()
        {
            Collect("saltglass", "copper-eye", "seafoam-wheel");
            Assert.That(game.Inventory.Count, Is.EqualTo(3));
            Assert.That(game.Mounted, Is.Empty);
            Assert.That(game.BeamPower, Is.Zero);
            Mount("saltglass", "seafoam-wheel");
            Assert.That(game.BeamPower, Is.EqualTo(6));
            Assert.That(game.CanIgnite, Is.False);
            game.ToggleMount("seafoam-wheel");
            Assert.That(game.Inventory.Any(item => item.Id == "seafoam-wheel"), Is.True);
            game.ToggleMount("copper-eye");
            Light(5);
            Assert.That(game.Inventory.Select(item => item.Id), Is.EquivalentTo(new[] { "seafoam-wheel" }));
            Assert.That(StateOf("saltglass"), Is.EqualTo(LensAvailability.Spent));
            Assert.That(StateOf("copper-eye"), Is.EqualTo(LensAvailability.Spent));

            Collect("twinwake-prism");
            Mount("seafoam-wheel", "twinwake-prism");
            Light(8);
            Collect("storm-fresnel", "fog-pearl");
            Mount("storm-fresnel", "fog-pearl");
            Light(10);
            Assert.That(game.Finished, Is.True);
        }

        [Test]
        public void AlternativeRouteUsesAnchorThenCrownAndKeepsTheWheel()
        {
            Collect("saltglass", "copper-eye", "seafoam-wheel");
            Mount("saltglass", "copper-eye");
            Light(5);
            Collect("anchor-glass", "fog-pearl");
            Mount("anchor-glass", "fog-pearl");
            Light(8);
            Assert.That(game.Inventory.Select(item => item.Id), Is.EquivalentTo(new[] { "seafoam-wheel" }));
            Collect("crown-glass");
            Mount("seafoam-wheel", "crown-glass");
            Light(10);
            Assert.That(game.Finished, Is.True);
        }

        [Test]
        public void AnchorAndCrownTogetherReachTenWithoutResonance()
        {
            Collect("saltglass", "copper-eye", "seafoam-wheel");
            Mount("saltglass", "copper-eye");
            Light(5);
            Collect("twinwake-prism", "anchor-glass");
            Mount("seafoam-wheel", "twinwake-prism");
            Light(8);
            Collect("crown-glass");
            Mount("anchor-glass", "crown-glass");
            Light(10);
            Assert.That(game.Finished, Is.True);
        }

        [Test]
        public void EveryReachableBagAndPairCanStillFinishTheRoute()
        {
            var nodes = new Dictionary<string, SearchNode>();
            var pending = new Queue<SearchNode>();
            var fullBags = new Dictionary<int, HashSet<string>>();
            var routes = new HashSet<string>();
            AddNode(new List<Step>(), "", nodes, pending);

            while (pending.Count > 0)
            {
                SearchNode node = pending.Dequeue();
                GameSession state = Replay(node.Path);
                node.Finished = state.Finished;
                if (state.Finished)
                {
                    routes.Add(node.Route);
                    continue;
                }

                if (state.Inventory.Count == GameSession.InventoryCapacity)
                {
                    if (!fullBags.ContainsKey(state.LitCount)) fullBags[state.LitCount] = new HashSet<string>();
                    fullBags[state.LitCount].Add(StateKey(state));
                }

                foreach (var item in state.Items.Where(item => state.AvailabilityFor(item) == LensAvailability.Available))
                    AddEdge(node, new Step(item.Id), node.Route, nodes, pending);

                // All mounted configurations can return to empty sockets without changing the bag.
                // Try each pair through the real model, then explore every successful ignition.
                for (int first = 0; first < state.Inventory.Count; first++)
                {
                    for (int second = first + 1; second < state.Inventory.Count; second++)
                    {
                        string firstId = state.Inventory[first].Id;
                        string secondId = state.Inventory[second].Id;
                        state.ToggleMount(firstId);
                        state.ToggleMount(secondId);
                        bool matches = state.CanIgnite;
                        state.ToggleMount(firstId);
                        state.ToggleMount(secondId);
                        Assert.That(state.Mounted, Is.Empty, "Unmounting must restore empty sockets.");
                        if (!matches) continue;

                        string pair = string.Join("+", new[] { firstId, secondId }.OrderBy(id => id));
                        string route = node.Route.Length == 0 ? pair : node.Route + "/" + pair;
                        AddEdge(node, new Step(firstId, secondId), route, nodes, pending);
                    }
                }
            }

            var canFinish = new Dictionary<string, bool>();
            foreach (var node in nodes.Values)
                Assert.That(CanFinish(node, nodes, canFinish), Is.True, $"Unwinnable reachable state: {node.Key}");

            int[] bagCounts = Enumerable.Range(0, game.Beacons.Count)
                .Select(stage => fullBags.TryGetValue(stage, out var bags) ? bags.Count : 0).ToArray();
            bool originalCoast = IsOriginalEightLensCoast();
            TestContext.WriteLine($"Reachable states: {nodes.Count}. Complete pair sequences: {routes.Count}. Full bags by beacon: {string.Join(", ", bagCounts)}. Original eight-lens coast: {originalCoast}.");
            if (originalCoast)
            {
                Assert.That(routes.Count, Is.EqualTo(7));
                Assert.That(bagCounts, Is.EqualTo(new[] { 1, 4, 12 }));
            }
        }

        private void AddEdge(SearchNode from, Step step, string route,
            Dictionary<string, SearchNode> nodes, Queue<SearchNode> pending)
        {
            var path = new List<Step>(from.Path) { step };
            SearchNode next = AddNode(path, route, nodes, pending);
            from.Next.Add(next.Key);
        }

        private SearchNode AddNode(List<Step> path, string route,
            Dictionary<string, SearchNode> nodes, Queue<SearchNode> pending)
        {
            GameSession state = Replay(path);
            string key = StateKey(state) + "|" + route;
            if (nodes.TryGetValue(key, out var existing)) return existing;

            var node = new SearchNode { Key = key, Path = path, Route = route };
            nodes.Add(key, node);
            pending.Enqueue(node);
            return node;
        }

        private GameSession Replay(IEnumerable<Step> path)
        {
            GameSession state = coast.CreateSession();
            foreach (var step in path)
            {
                if (step.Second == null)
                {
                    var item = state.Items.Single(candidate => candidate.Id == step.First);
                    Assert.That(state.AvailabilityFor(item), Is.EqualTo(LensAvailability.Available));
                    state.Collect(step.First);
                }
                else
                {
                    state.ToggleMount(step.First);
                    state.ToggleMount(step.Second);
                    Assert.That(state.CanIgnite, Is.True, $"Cannot light with {step.First} and {step.Second}.");
                    state.Ignite();
                }
            }
            return state;
        }

        private static string StateKey(GameSession state)
        {
            string spent = string.Join(",", state.Items
                .Where(item => state.AvailabilityFor(item) == LensAvailability.Spent)
                .Select(item => item.Id).OrderBy(id => id));
            string bag = string.Join(",", state.Inventory.Select(item => item.Id).OrderBy(id => id));
            return $"{state.LitCount}|spent={spent}|bag={bag}";
        }

        private static bool CanFinish(SearchNode node, Dictionary<string, SearchNode> nodes,
            Dictionary<string, bool> memo)
        {
            if (node.Finished) return true;
            if (memo.TryGetValue(node.Key, out bool known)) return known;
            bool result = node.Next.Any(key => CanFinish(nodes[key], nodes, memo));
            memo[node.Key] = result;
            return result;
        }

        private bool IsOriginalEightLensCoast()
        {
            var expected = new[]
            {
                "anchor-glass:7:1:0:0", "copper-eye:3:0:0:0", "crown-glass:3:2:3:0",
                "fog-pearl:1:1:3:0", "saltglass:2:0:0:0", "seafoam-wheel:4:0:0:0",
                "storm-fresnel:6:2:0:0", "twinwake-prism:2:1:2:0"
            };
            var actual = game.Items.OrderBy(item => item.Id)
                .Select(item => $"{item.Id}:{item.BasePower}:{item.UnlockAfter}:{item.EffectivePower(2) - item.BasePower}:{item.EffectivePower(3) - item.BasePower}");
            return actual.SequenceEqual(expected) && game.Beacons.Select(beacon => beacon.Target).SequenceEqual(new[] { 5, 8, 10 });
        }

        private LensAvailability StateOf(string id) => game.AvailabilityFor(game.Items.Single(item => item.Id == id));

        private void Collect(params string[] ids)
        {
            foreach (string id in ids)
            {
                Assert.That(StateOf(id), Is.EqualTo(LensAvailability.Available), id);
                game.Collect(id);
            }
        }

        private void Mount(string first, string second)
        {
            game.ToggleMount(first);
            game.ToggleMount(second);
            Assert.That(game.Mounted.Select(item => item.Id), Is.EquivalentTo(new[] { first, second }));
        }

        private void Light(int target)
        {
            Assert.That(game.BeamPower, Is.EqualTo(target));
            Assert.That(game.CanIgnite, Is.True);
            game.Ignite();
        }

        private sealed class SearchNode
        {
            public string Key;
            public string Route;
            public List<Step> Path;
            public readonly List<string> Next = new List<string>();
            public bool Finished;
        }

        private sealed class Step
        {
            public readonly string First;
            public readonly string Second;

            public Step(string first, string second = null)
            {
                First = first;
                Second = second;
            }
        }
    }
}
