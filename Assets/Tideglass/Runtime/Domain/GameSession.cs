using System;
using System.Collections.Generic;

namespace Tideglass.Domain
{
    public enum LensAvailability
    {
        Available,
        Locked,
        InInventory,
        InventoryFull,
        Spent,
        RouteComplete
    }

    public sealed class GameSession
    {
        public const int InventoryCapacity = 3;
        public const int SocketCount = 2;

        private readonly Dictionary<string, ItemSpec> catalogue = new Dictionary<string, ItemSpec>();
        private readonly HashSet<string> spent = new HashSet<string>();
        private readonly List<ItemSpec> inventory = new List<ItemSpec>(InventoryCapacity);
        private readonly List<ItemSpec> mounted = new List<ItemSpec>(SocketCount);

        public IReadOnlyList<ItemSpec> Items { get; }
        public IReadOnlyList<BeaconSpec> Beacons { get; }
        public IReadOnlyList<ItemSpec> Inventory { get; }
        public IReadOnlyList<ItemSpec> Mounted { get; }
        public int LitCount { get; private set; }
        public bool Finished => LitCount == Beacons.Count;
        public BeaconSpec CurrentBeacon => Finished ? null : Beacons[LitCount];
        public bool CanIgnite => !Finished && mounted.Count == SocketCount && BeamPower == CurrentBeacon.Target;

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
            Inventory = inventory.AsReadOnly();
            Mounted = mounted.AsReadOnly();
            foreach (ItemSpec item in Items) catalogue.Add(item.Id, item);
        }

        public LensAvailability AvailabilityFor(ItemSpec item)
        {
            if (spent.Contains(item.Id)) return LensAvailability.Spent;
            if (inventory.Exists(lens => lens.Id == item.Id)) return LensAvailability.InInventory;
            if (Finished) return LensAvailability.RouteComplete;
            if (item.UnlockAfter > LitCount) return LensAvailability.Locked;
            return inventory.Count < InventoryCapacity ? LensAvailability.Available : LensAvailability.InventoryFull;
        }

        public bool IsMounted(string id) => mounted.Exists(lens => lens.Id == id);

        public string Collect(string id)
        {
            if (id == null || !catalogue.TryGetValue(id, out ItemSpec item)) return "Unknown lens.";
            switch (AvailabilityFor(item))
            {
                case LensAvailability.Locked: return $"{item.Title} is still locked.";
                case LensAvailability.InInventory: return $"{item.Title} is already in your bag.";
                case LensAvailability.InventoryFull: return "Your bag is full. Light a beacon to make room.";
                case LensAvailability.Spent: return $"{item.Title} has spent its charge.";
                case LensAvailability.RouteComplete: return "All beacons are lit.";
            }

            inventory.Add(item);
            return $"Collected {item.Title}. Mount it from your bag to change the beam.";
        }

        public string ToggleMount(string id)
        {
            if (id == null || !catalogue.TryGetValue(id, out ItemSpec item)) return "Unknown lens.";
            if (Finished) return "All beacons are lit.";
            if (!inventory.Contains(item)) return $"Collect {item.Title} before mounting it.";
            if (mounted.Remove(item)) return $"Unmounted {item.Title}. It remains in your bag.";
            if (mounted.Count == SocketCount) return "Both sockets are full. Unmount a lens to swap it.";

            mounted.Add(item);
            return $"Mounted {item.Title}. Beam power is now {BeamPower}.";
        }

        public string Ignite()
        {
            if (Finished) return "All beacons are lit.";
            if (mounted.Count != SocketCount) return "Mount two different lenses to light this beacon.";

            BeaconSpec beacon = CurrentBeacon;
            int power = BeamPower;
            if (power != beacon.Target) return $"Your beam is {power}. Unmount a lens and try another pair to reach {beacon.Target}.";

            foreach (var lens in mounted)
            {
                spent.Add(lens.Id);
                inventory.Remove(lens);
            }
            mounted.Clear();
            LitCount++;
            if (Finished) return $"{beacon.Title} is lit. Both lenses spent. The passage is safe!";
            return $"{beacon.Title} is lit. Both lenses spent. Next stop is {CurrentBeacon.Location}.";
        }
    }
}
