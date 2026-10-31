using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KodeUI
{
    public class UIRectMask : UIObject
    {
        public RectMask2D mask { get; private set; }

        public override void CreateUI()
        {
            Pivot(PivotPresets.TopLeft);
            mask = gameObject.AddComponent<RectMask2D>();
        }

        public override void Style()
        {
        }
    }
}
