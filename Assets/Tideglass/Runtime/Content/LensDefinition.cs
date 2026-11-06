using System;
using Tideglass.Domain;
using UnityEngine;

namespace Tideglass.Content
{
    [CreateAssetMenu(menuName = "Tideglass/Lens")]
    public sealed class LensDefinition : ScriptableObject
    {
        [Tooltip("Unique catalogue ID. Keep this stable when renaming a lens.")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [TextArea(2, 3), SerializeField] private string description;
        [SerializeField] private Texture2D icon;
        [Min(1), SerializeField] private int basePower = 1;
        [Min(0), SerializeField] private int unlockAfter;
        [SerializeField] private LensEffect effect;

        public string Id => id;
        public Texture2D Icon => icon;
        public string Hint => effect == null ? string.Empty : effect.Hint;

        public ItemSpec ToSpec()
        {
            Func<int, int> bonus = null;
            if (effect != null) bonus = effect.BonusFor;
            return new ItemSpec(id, displayName, description, unlockAfter, basePower, bonus);
        }
    }
}
