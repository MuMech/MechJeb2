using UnityEngine;
using UnityEngine.UI;

namespace KodeUI
{
    public class UIToggle : UIToggleBase
    {
        private LayoutPanel checkMark;
        private Image image;

        public override void CreateUI()
        {
            base.CreateUI();
            
            image = gameObject.AddComponent<Image>();
            toggle.targetGraphic = image;

            ColorBlock colors = toggle.colors;
            colors.highlightedColor = Color.magenta;
            colors.pressedColor     = new Color(0.0f, 0.8f, 0.1f);
            colors.disabledColor    = Color.clear;
            colors.normalColor      = Color.white;
            toggle.colors = colors;
            
            Add<LayoutPanel>(out checkMark, "CheckMark").Anchor(AnchorPresets.StretchAll).Pivot(PivotPresets.MiddleCenter).Scale(0.625f).Finish();
            toggle.graphic = checkMark.BackGround;

            // TODO This should be handled in a parent class with some logic related to Stretch axis. In Finish ? Might need something similar with anchoredPosition
            checkMark.rectTransform.sizeDelta = Vector2.zero;
        }

        public override void Style()
        {
            base.Style();
            ImageLoader.SetupImage(image,"KodeUI/Default/toggle");
            checkMark.Background("KodeUI/Default/toggle_on").BackgroundColor(Color.white);
        }
    }
}
