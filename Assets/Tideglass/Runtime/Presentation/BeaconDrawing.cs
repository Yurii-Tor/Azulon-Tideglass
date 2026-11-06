using UnityEngine;
using UnityEngine.UIElements;

namespace Tideglass.Presentation
{
    sealed class BeaconDrawing : VisualElement
    {
        readonly bool shining;
        public BeaconDrawing(bool shining)
        {
            this.shining = shining;
            AddToClassList("beacon-art");
            generateVisualContent += Draw;
        }

        void Draw(MeshGenerationContext context)
        {
            var p = context.painter2D;
            float w = contentRect.width, h = contentRect.height;
            if (w <= 0 || h <= 0) return;
            float width = Mathf.Min(w, h * 1.85f);
            float height = width / 1.85f;
            float left = (w - width) / 2f;
            float top = (h - height) / 2f;
            Vector2 Pt(float x, float y) => new Vector2(left + x * width, top + y * height);
            void Shape(Color color, params Vector2[] pts)
            {
                p.fillColor = color;
                p.BeginPath();
                p.MoveTo(pts[0]);
                for (int i = 1; i < pts.Length; i++) p.LineTo(pts[i]);
                p.ClosePath();
                p.Fill();
            }
            Color brass = new Color32(235, 201, 126, 255), glass = new Color32(149, 223, 207, 255);
            Shape(glass, Pt(.18f,.22f), Pt(.185f,.24f), Pt(.197f,.245f), Pt(.185f,.255f), Pt(.18f,.275f), Pt(.176f,.255f), Pt(.163f,.245f), Pt(.176f,.24f));
            if (shining) Shape(new Color32(231, 206, 132, 105), Pt(.52f,.28f), Pt(1,.05f), Pt(1,.51f));
            Shape(new Color32(37,74,78,255), Pt(0,.8f), Pt(.18f,.64f), Pt(.37f,.76f), Pt(.56f,.59f), Pt(.82f,.75f), Pt(1,.68f), Pt(1,1), Pt(0,1));
            Shape(brass, Pt(.40f,.78f), Pt(.44f,.34f), Pt(.54f,.34f), Pt(.58f,.78f));
            Shape(new Color32(35,73,72,255), Pt(.42f,.59f), Pt(.43f,.50f), Pt(.56f,.55f), Pt(.57f,.64f));
            Shape(shining ? brass : glass, Pt(.435f,.33f), Pt(.435f,.22f), Pt(.545f,.22f), Pt(.545f,.33f));
            Shape(brass, Pt(.415f,.22f), Pt(.49f,.12f), Pt(.565f,.22f));
            Shape(glass, Pt(.28f,.82f), Pt(.38f,.77f), Pt(.60f,.77f), Pt(.71f,.82f));
            for (var i = 0; i < 3; i++)
            {
                float y = .86f + i * .045f;
                Shape(new Color32(91,140,136,255), Pt(.10f+i*.09f,y), Pt(.83f-i*.05f,y), Pt(.78f-i*.05f,y+.009f), Pt(.12f+i*.09f,y+.009f));
            }
        }
    }
}
