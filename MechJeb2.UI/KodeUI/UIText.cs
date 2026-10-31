using TMPro;
using UnityEngine;

namespace KodeUI
{
    public class UIText : Layout
    {
        public TextMeshProUGUI tmpText { get; private set; }

        public override void CreateUI()
        {
            Pivot(PivotPresets.TopLeft);
            tmpText = gameObject.AddComponent<TextMeshProUGUI>();
        }

        public override void Style()
        {
            tmpText.fontSize = 18;
            tmpText.color = Color.white;
        }

        public UIText Text(string text)
        {
            tmpText.text = text;
            tmpText.SetLayoutDirty();
            return this;
        }

        public UIText Text(char[] text)
        {
            tmpText.SetCharArray(text);
            tmpText.SetLayoutDirty();
            return this;
        }
        
        public UIText AutoSize()
        {
            tmpText.autoSizeTextContainer = true;
            return this;
        }

        public UIText IsOverlay()
        {
            tmpText.isOverlay = true;
            return this;
        }

        public UIText Size(float s)
        {
            tmpText.fontSize = s;
            return this;
        }

        public UIText Alignment(TextAlignmentOptions align)
        {
            tmpText.alignment = align;
            return this;
        }

        public UIText Overflow(TextOverflowModes mode)
        {
            tmpText.overflowMode = mode;
            return this;
        }

    }
}
