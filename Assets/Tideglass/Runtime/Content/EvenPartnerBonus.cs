using UnityEngine;

namespace Tideglass.Content
{
    [CreateAssetMenu(menuName = "Tideglass/Effects/Even partner bonus")]
    public sealed class EvenPartnerBonus : LensEffect
    {
        [Min(1), SerializeField] private int bonus = 2;

        public override string Hint => $"+{bonus} beam with an even-power partner.";
        public override int BonusFor(int partnerBasePower) => partnerBasePower % 2 == 0 ? bonus : 0;
    }
}
