using System;

namespace Tideglass.Domain
{
    public sealed class BeaconSpec
    {
        public string Title { get; }
        public string Location { get; }
        public int Target { get; }

        public BeaconSpec(string title, string location, int target)
        {
            if (target <= 0) throw new ArgumentOutOfRangeException(nameof(target));

            Title = title;
            Location = location;
            Target = target;
        }
    }
}
