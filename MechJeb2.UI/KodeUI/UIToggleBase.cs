using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;
using UnityEngine.UI;

namespace KodeUI
{
    public abstract class UIToggleBase : Layout
    {
        protected Toggle toggle;

        public bool isOn
        {
            get { return toggle.isOn; }
        }

        public override void CreateUI()
        {
            Pivot(PivotPresets.TopLeft);
            toggle = gameObject.AddComponent<Toggle>();
        }
       
        public UIToggleBase OnClick(UnityAction<bool> action)
        {
            toggle.onValueChanged.AddListener(action);
            return this;
        }

    }
}
