using System;
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

        [SerializeField] private LensDefinition[] lenses;
        [SerializeField] private Beacon[] beacons;

        public GameSession CreateSession()
        {
            var items = Array.ConvertAll(lenses, lens => lens.ToSpec());
            var route = Array.ConvertAll(beacons, b => new BeaconSpec(b.title, b.location, b.target));
            return new GameSession(items, route);
        }

        public Texture2D IconFor(string id) => FindLens(id).Icon;
        public string NoteFor(string id) => FindLens(id).Hint;

        private LensDefinition FindLens(string id) => Array.Find(lenses, lens => lens.Id == id);
    }
}
