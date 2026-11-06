using System;

namespace Tideglass.Domain
{
    public sealed class ItemSpec
    {
        private readonly Func<int, int> adjustPower;

        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
        public int UnlockAfter { get; }
        public int BasePower { get; }

        public ItemSpec(string id, string title, string description, int unlockAfter, int basePower,
            Func<int, int> adjustPower = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A lens needs a stable ID.", nameof(id));
            if (unlockAfter < 0) throw new ArgumentOutOfRangeException(nameof(unlockAfter));
            if (basePower <= 0) throw new ArgumentOutOfRangeException(nameof(basePower));

            Id = id;
            Title = title;
            Description = description;
            UnlockAfter = unlockAfter;
            BasePower = basePower;
            this.adjustPower = adjustPower;
        }

        public int EffectivePower(int partnerBasePower)
        {
            return BasePower + (adjustPower?.Invoke(partnerBasePower) ?? 0);
        }
    }
}
