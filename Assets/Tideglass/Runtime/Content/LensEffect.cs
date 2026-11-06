using UnityEngine;

namespace Tideglass.Content
{
    public abstract class LensEffect : ScriptableObject
    {
        public abstract string Hint { get; }
        public abstract int BonusFor(int partnerBasePower);
    }
}
