using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace KodeUI
{
    public class UIEmpty : Layout
    {
        public override void CreateUI()
        {
        }

        public override void Style()
        {
        }

		protected override string GetStylePath(bool isParent=false)
		{
			if (isParent) {
				return GetParentStylePath ();
			} else {
				return base.GetStylePath(isParent);
			}
		}
    }
}
