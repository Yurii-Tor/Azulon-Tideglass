using System;
using System.Collections.Generic;

namespace Tideglass.Domain
{
    public sealed class GameSession
    {
        private readonly Dictionary<string, ItemSpec> catalogue = new Dictionary<string, ItemSpec>();
        private readonly HashSet<string> owned = new HashSet<string>();
        private readonly List<ItemSpec> mounted = new List<ItemSpec>(2);

        public IReadOnlyList<ItemSpec> Items { get; }
        public IReadOnlyList<BeaconSpec> Beacons { get; }
        public IReadOnlyList<ItemSpec> Mounted { get; }
        public int LitCount { get; private set; }
        public bool Finished => LitCount == Beacons.Count;
        public BeaconSpec CurrentBeacon => Finished ? null : Beacons[LitCount];
        public bool CanIgnite => !Finished && mounted.Count == 2 && BeamPower == CurrentBeacon.Target;

        public int BeamPower
        {
            get
            {
                if (mounted.Count == 0) return 0;
                if (mounted.Count == 1) return mounted[0].BasePower;

                ItemSpec first = mounted[0];
                ItemSpec second = mounted[1];
                return first.EffectivePower(second.BasePower) + second.EffectivePower(first.BasePower);
            }
        }

        public GameSession(IReadOnlyList<ItemSpec> items, IReadOnlyList<BeaconSpec> beacons)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (beacons == null) throw new ArgumentNullException(nameof(beacons));
            if (beacons.Count == 0) throw new ArgumentException("Configure at least one beacon.", nameof(beacons));

            Items = new List<ItemSpec>(items).AsReadOnly();
            Beacons = new List<BeaconSpec>(beacons).AsReadOnly();
            Mounted = mounted.AsReadOnly();
            foreach (ItemSpec item in Items) catalogue.Add(item.Id, item);
        }

        public bool IsOwned(string id) => owned.Contains(id);
        public bool IsUnlocked(ItemSpec item) => item != null && item.UnlockAfter <= LitCount;
        public bool IsMounted(string id) => mounted.Exists(item => item.Id == id);

        public string Collect(string id)
        {
            if (id == null || !catalogue.TryGetValue(id, out ItemSpec item)) return "Unknown lens.";
            if (!IsUnlocked(item))
            {
                string required = item.UnlockAfter == 1 ? "the first beacon" : $"the first {item.UnlockAfter} beacons";
                return $"Light {required} to reach {item.Title}.";
            }
            if (!owned.Add(id)) return $"{item.Title} is already in your inventory.";

            return $"Collected {item.Title}. Mount it from your inventory.";
        }

        public string ToggleMount(string id)
        {
            if (id == null || !catalogue.TryGetValue(id, out ItemSpec item)) return "Unknown lens.";
            if (Finished) return "All beacons are lit.";
            if (!IsOwned(id)) return $"Collect {item.Title} before mounting it.";
            if (mounted.Remove(item)) return $"Removed {item.Title} from the lamp.";
            if (mounted.Count == 2) return "Both sockets are full. Remove a lens to make room.";

            mounted.Add(item);
            return $"Mounted {item.Title}.";
        }

        public string Ignite()
        {
            if (Finished) return "All beacons are lit.";
            if (mounted.Count != 2) return "Mount two different lenses to light this beacon.";

            BeaconSpec beacon = CurrentBeacon;
            int power = BeamPower;
            if (power != beacon.Target) return $"Your beam is {power}. Tune it to {beacon.Target} with a different pair.";

            LitCount++;
            mounted.Clear();
            if (Finished) return $"{beacon.Title} is lit. The passage is safe!";

            return $"{beacon.Title} is lit. Next stop: {CurrentBeacon.Location}.";
        }
    }
}
