using System;
using System.Collections.Generic;
using Tideglass.Domain;
using UnityEngine;

namespace Tideglass.Content
{
    [CreateAssetMenu(menuName = "Tideglass/Coast")]
    public sealed class CoastDefinition : ScriptableObject
    {
        [Serializable]
        private struct Beacon
        {
            public string title;
            public string location;
            [Min(1)] public int target;
        }

        [SerializeField] private LensDefinition[] lenses = Array.Empty<LensDefinition>();
        [SerializeField] private Beacon[] beacons = Array.Empty<Beacon>();

        public GameSession CreateSession()
        {
            if (lenses.Length < 2)
                throw new InvalidOperationException($"Coast '{name}' needs at least two lenses for the lamp.");

            var items = new ItemSpec[lenses.Length];
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < lenses.Length; i++)
            {
                var lens = lenses[i];
                if (lens == null)
                    throw new InvalidOperationException($"Coast '{name}': assign a lens to catalogue slot {i + 1}.");

                var item = lens.ToSpec();
                if (!ids.Add(item.Id))
                    throw new InvalidOperationException($"Coast '{name}': duplicate lens ID '{item.Id}' at catalogue slot {i + 1}.");

                items[i] = item;
            }
            var route = Array.ConvertAll(beacons, b => new BeaconSpec(b.title, b.location, b.target));
            return new GameSession(items, route);
        }

        public Texture2D IconFor(string id) => FindLens(id).Icon;
        public string NoteFor(string id) => FindLens(id).Hint;

        private LensDefinition FindLens(string id) => Array.Find(lenses, lens => lens.Id == id);
    }
}
