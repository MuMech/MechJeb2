using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KodeUI
{
    public class UIImage : UIObject
    {
        public Image image { get; private set; }

        public override void CreateUI()
        {
            Pivot(PivotPresets.TopLeft);
            image = gameObject.AddComponent<Image>();
            image.color = UnityEngine.Color.white;
        }

        public override void Style()
        {
        }

        public UIImage Image(Sprite sprite)
        {
            image.sprite = sprite;
            return this;
        }

        public UIImage Type(Image.Type type)
        {
            image.type = type;
            return this;
        }

        public UIImage Color(Color color)
        {
            image.color = color;
            return this;
        }
    }
}
