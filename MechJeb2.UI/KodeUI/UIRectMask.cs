using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KodeUI
{
    public class UIRectMask : Layout
    {
        public RectMask2D mask { get; private set; }

        public override void CreateUI()
        {
            Pivot(PivotPresets.TopLeft);
            mask = gameObject.AddComponent<RectMask2D>();
        }
    }
}
