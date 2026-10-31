using UnityEngine;
using UnityEngine.UI;

namespace KodeUI
{
    public class LayoutAnchor : UIObject
    {
        AnchorLayoutGroup anchor;
    
		protected override string GetStylePath(bool isParent=false)
		{
			if (isParent) {
				return GetParentStylePath ();
			} else {
				return base.GetStylePath(isParent);
			}
		}

        public override void CreateUI()
        {
			anchor = gameObject.AddComponent<AnchorLayoutGroup> ();
        }

        public override void Style()
        {
        }

		public LayoutAnchor DoMinWidth (bool enable)
		{
			anchor.doMinWidth = enable;
			return this;
		}

		public LayoutAnchor DoMinHeight (bool enable)
		{
			anchor.doMinHeight = enable;
			return this;
		}

		public LayoutAnchor DoPreferredWidth (bool enable)
		{
			anchor.doPreferredWidth = enable;
			return this;
		}

		public LayoutAnchor DoPreferredHeight (bool enable)
		{
			anchor.doPreferredHeight = enable;
			return this;
		}

		public LayoutAnchor DoFlexibleWidth (bool enable)
		{
			anchor.doFlexibleWidth = enable;
			return this;
		}

		public LayoutAnchor DoFlexibleHeight (bool enable)
		{
			anchor.doFlexibleHeight = enable;
			return this;
		}

    }
}
