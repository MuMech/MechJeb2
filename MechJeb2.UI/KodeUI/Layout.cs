using UnityEngine;
using UnityEngine.UI;

namespace KodeUI
{
    public class Layout : UIObject
    {
        private HorizontalOrVerticalLayoutGroup lg;
    
        public override void CreateUI()
        {
            Pivot(PivotPresets.TopLeft);
        }

        public override void Style()
        {
        }

        public Layout Vertical()
        {
            lg = gameObject.AddComponent<VerticalLayoutGroup>();
            return this;
        }

        public Layout Horizontal()
        {
            lg = gameObject.AddComponent<HorizontalLayoutGroup>();
            return this;
        }

        public Layout Spacing(int spacing)
        {
            if (lg == null)
            {
                Debug.LogError("Call Vertical() or Horizontal() before Spacing()");
            }
            lg.spacing = spacing;
            return this;
        }

        public Layout Padding(int padding)
        {
            if (lg == null)
            {
                Debug.LogError("Call Vertical() or Horizontal() before Padding()");
            }
            lg.padding = new RectOffset(padding,padding,padding,padding);
            return this;
        }

        public Layout Padding(int left, int right, int top, int bottom)
        {
            if (lg == null)
            {
                Debug.LogError("Call Vertical() or Horizontal() before Padding()");
            }
            lg.padding = new RectOffset(left, right, top, bottom);
            return this;
        }

        public Layout ChildForceExpand(bool width, bool height)
        {
            if (lg == null)
            {
                Debug.LogError("Call Vertical() or Horizontal() before ChildForceExpand()");
            }
            lg.childForceExpandWidth = width;
            lg.childForceExpandHeight = height;
            return this;
        }

        public Layout ChildForceExpandHeight(bool state)
        {
            if (lg == null)
            {
                Debug.LogError("Call Vertical() or Horizontal() before ChildForceExpandHeight()");
            }
            lg.childForceExpandHeight = state;
            return this;
        }

        public Layout ChildForceExpandWidth(bool state)
        {
            if (lg == null)
            {
                Debug.LogError("Call Vertical() or Horizontal() before ChildForceExpandWidth()");
            }
            lg.childForceExpandWidth = state;
            return this;
        }

        public Layout ControlChildSize(bool width, bool height)
        {
            if (lg == null)
            {
                Debug.LogError("Call Vertical() or Horizontal() before ControlChildSize()");
            }
            lg.childControlWidth = width;
            lg.childControlHeight = height;
            return this;
        }

        public Layout ControlChildSizeWidth (bool state)
        {
            if (lg == null)
            {
                Debug.LogError("Call Vertical() or Horizontal() before ControlChildSizeWidth()");
            }
            lg.childControlWidth = state;
            return this;
        }

        public Layout ControlChildSizeHeight (bool state)
        {
            if (lg == null)
            {
                Debug.LogError("Call Vertical() or Horizontal() before ControlChildSizeHeight()");
            }
            lg.childControlHeight = state;
            return this;
        }

        public Layout ChildAlignment(TextAnchor anchor)
        {
            
            if (lg == null)
            {
                Debug.LogError("Call Vertical() or Horizontal() before ChildAlignment()");
            }
            lg.childAlignment = anchor;
            return this;
        }
    }
}
