using Tideglass.Content;
using Tideglass.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tideglass
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class Station : MonoBehaviour
    {
        [SerializeField] private CoastDefinition coast;
        [SerializeField] private VisualTreeAsset lensCard;

        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            new TideglassView(root, lensCard, coast.CreateSession(), coast.IconFor, coast.NoteFor);
        }
    }
}
