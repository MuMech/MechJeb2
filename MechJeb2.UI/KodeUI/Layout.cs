using UnityEngine;
using UnityEngine.UI;

namespace KodeUI
{
    public class Layout : UIObject
    {
        public LayoutGroup layoutGroup { get; private set; }
    
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
            Pivot(PivotPresets.TopLeft);
        }

        public override void Style()
        {
            if (style.padding is RectOffset ro) {
                layoutGroup.padding = ro;
            }
            if (layoutGroup is HorizontalOrVerticalLayoutGroup lg && style.spacing is float s) {
                lg.spacing = s;
            }
        }

        public Layout VertiLink()
        {
            layoutGroup = gameObject.AddComponent<VertiLinkLayoutGroup>();
            return this;
        }

        public Layout Vertical()
        {
            layoutGroup = gameObject.AddComponent<VerticalLayoutGroup>();
            return this;
        }

        public Layout Horizontal()
        {
            layoutGroup = gameObject.AddComponent<HorizontalLayoutGroup>();
            return this;
        }

        public Layout Grid()
        {
            layoutGroup = gameObject.AddComponent<GridLayoutGroup>();
            return this;
        }

        public Layout Spacing(int spacing)
        {
            if (layoutGroup is HorizontalOrVerticalLayoutGroup lg) {
                lg.spacing = spacing;
            } else if (layoutGroup is GridLayoutGroup glg) {
                glg.spacing = new Vector2 (spacing, spacing);
            } else {
                Debug.LogError("Call Vertical() or Horizontal() or Grid () before Spacing()");
            }
            return this;
        }

        public Layout Spacing(float x, float y)
        {
            return Spacing (new Vector2(x, y));
        }

        public Layout Spacing(Vector2 spacing)
        {
            if (layoutGroup is GridLayoutGroup glg) {
                glg.spacing = spacing;
            } else {
                Debug.LogError("Call Grid() before Spacing()");
            }
            return this;
        }

        public Layout Padding(int padding)
        {
            var lg = layoutGroup;
            if (lg == null)
            {
                Debug.LogError("Call Vertical() or Horizontal() before Padding()");
            }
            lg.padding = new RectOffset(padding,padding,padding,padding);
            return this;
        }

        public Layout Padding(int left, int right, int top, int bottom)
        {
            var lg = layoutGroup;
            if (lg == null)
            {
                Debug.LogError("Call Vertical() or Horizontal() before Padding()");
            }
            lg.padding = new RectOffset(left, right, top, bottom);
            return this;
        }

        public Layout ChildForceExpand(bool width, bool height)
        {
            if (layoutGroup is HorizontalOrVerticalLayoutGroup lg) {
                lg.childForceExpandWidth = width;
                lg.childForceExpandHeight = height;
            } else {
                Debug.LogError("Call Vertical() or Horizontal() before ChildForceExpand()");
            }
            return this;
        }

        public Layout ChildForceExpandHeight(bool state)
        {
            if (layoutGroup is HorizontalOrVerticalLayoutGroup lg) {
                lg.childForceExpandHeight = state;
            } else {
                Debug.LogError("Call Vertical() or Horizontal() before ChildForceExpandHeight()");
            }
            return this;
        }

        public Layout ChildForceExpandWidth(bool state)
        {
            if (layoutGroup is HorizontalOrVerticalLayoutGroup lg) {
                lg.childForceExpandWidth = state;
            } else {
                Debug.LogError("Call Vertical() or Horizontal() before ChildForceExpandWidth()");
            }
            return this;
        }

        public Layout ControlChildSize(bool width, bool height)
        {
            if (layoutGroup is HorizontalOrVerticalLayoutGroup lg) {
                lg.childControlWidth = width;
                lg.childControlHeight = height;
            } else {
                Debug.LogError("Call Vertical() or Horizontal() before ControlChildSize()");
            }
            return this;
        }

        public Layout ControlChildSizeWidth (bool state)
        {
            if (layoutGroup is HorizontalOrVerticalLayoutGroup lg) {
                lg.childControlWidth = state;
            } else {
                Debug.LogError("Call Vertical() or Horizontal() before ControlChildSizeWidth()");
            }
            return this;
        }

        public Layout ControlChildSizeHeight (bool state)
        {
            if (layoutGroup is HorizontalOrVerticalLayoutGroup lg) {
                lg.childControlHeight = state;
            } else {
                Debug.LogError("Call Vertical() or Horizontal() before ControlChildSizeHeight()");
            }
            return this;
        }

        public Layout ChildAlignment(TextAnchor anchor)
        {
            if (layoutGroup is HorizontalOrVerticalLayoutGroup lg) {
                lg.childAlignment = anchor;
            } else if (layoutGroup is GridLayoutGroup glg) {
                glg.childAlignment = anchor;
            } else {
                Debug.LogError("Call Vertical() or Horizontal() before ChildAlignment()");
            }
            return this;
        }

        public Layout StartCorner (GridLayoutGroup.Corner startCorner)
        {
            if (layoutGroup is GridLayoutGroup glg) {
                glg.startCorner = startCorner;
            } else {
                Debug.LogError("Call Grid() before StartCorner()");
            }
            return this;
        }

        public Layout StartAxis (GridLayoutGroup.Axis startAxis)
        {
            if (layoutGroup is GridLayoutGroup glg) {
                glg.startAxis = startAxis;
            } else {
                Debug.LogError("Call Grid() before StartAxis()");
            }
            return this;
        }

        public Layout CellSize (Vector2 cellSize)
        {
            if (layoutGroup is GridLayoutGroup glg) {
                glg.cellSize = cellSize;
            } else {
                Debug.LogError("Call Grid() before CellSize()");
            }
            return this;
        }

        public Layout CellSize (float w, float h)
        {
            return CellSize (new Vector2 (w, h));
        }

        public Layout Constraint (GridLayoutGroup.Constraint constraint)
        {
            if (layoutGroup is GridLayoutGroup glg) {
                glg.constraint = constraint;
            } else {
                Debug.LogError("Call Grid() before Constraint()");
            }
            return this;
        }

        public Layout ConstraintCount (int constraintCount)
        {
            if (layoutGroup is GridLayoutGroup glg) {
                glg.constraintCount = constraintCount;
            } else {
                Debug.LogError("Call Grid() before ConstraintCount()");
            }
            return this;
        }
    }
}
