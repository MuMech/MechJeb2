using UnityEngine;
using UnityEngine.UI;

namespace KodeUI
{
    public class LayoutPanel : Layout
    {
        private Image backGround;

        public Image BackGround
        {
            get { return backGround; }
        }

        public override void CreateUI()
        {
            base.CreateUI();

            Anchor(AnchorPresets.StretchAll).Pivot(PivotPresets.TopLeft);
            
            rectTransform.anchoredPosition = Vector2.zero;
            
            backGround = gameObject.AddComponent<Image>();
        }

        public override void Style()
        {
            base.Style();
            backGround.sprite = style.sprite;
            backGround.color = style.color ?? UnityEngine.Color.clear;
            backGround.type = style.type ?? Image.Type.Sliced;
            if (backGround.sprite == null) {
                backGround.enabled = false;
            }
        }

        public LayoutPanel Background(Sprite sprite, Image.Type type =  Image.Type.Simple)
        {
            backGround.sprite = sprite;
            backGround.type = type;
            style.sprite = sprite;
            return this;
        }

        public LayoutPanel Background(string image)
        {
            ImageLoader.SetupImage(backGround,image);
            style.sprite = backGround.sprite;
            return this;
        }

        public LayoutPanel BackgroundColor(Color color)
        {
            backGround.color = color;
            style.color = color;
            return this;
        }


    }
}
