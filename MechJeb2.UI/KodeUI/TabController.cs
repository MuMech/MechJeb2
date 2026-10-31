using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace KodeUI
{

    public class TabController : LayoutPanel
    {
        public interface ITabItem
        {
			string TabName { get; }
			bool TabEnabled { get; }
			void SetTabVisible (bool visible);
        }

		class TabItemList : List<ITabItem>, UIKit.IListObject
		{
			ToggleGroup group;
			public Layout Content { get; set; }
			public RectTransform RectTransform
			{
				get { return Content.rectTransform; }
			}

			public void Create (int index)
			{
				Content
					.Add<TabItemView> ()
						.Group (group)
						.Item (this[index])
						.Finish()
					;
			}

			public void Update (GameObject obj, int index)
			{
				var view = obj.GetComponent<TabItemView> ();
				view.Item (this[index]);
			}

			public void Select (int index)
			{
				if (index >= 0 && index < Count) {
					group.SetAllTogglesOff (true);
					var child = Content.rectTransform.GetChild (index);
					var view = child.GetComponent<TabItemView> ();
					view.Select ();
				}
			}

			public TabItemList (ToggleGroup group)
			{
				this.group = group;
			}
		}

        class TabItemView : UIObject, ILayoutElement
        {
            Toggle toggle;
			Image image;

			ITabItem item;
			UIText text;
			UIImage selected;

            public override void CreateUI()
            {
                image = gameObject.AddComponent<Image> ();

				toggle = gameObject.AddComponent<Toggle> ();
				toggle.onValueChanged.AddListener (onValueChanged);

				this.Add<UIImage>(out selected, "Selected")
						.Anchor(AnchorPresets.StretchAll)
						.Pivot(PivotPresets.MiddleCenter)
						.SizeDelta(0, 0)
						.Finish()
					.Add<UIText>(out text, "Label")
						.Alignment(TextAlignmentOptions.Center)
						.Anchor(AnchorPresets.StretchAll)
						.Pivot(PivotPresets.MiddleCenter)
						.SizeDelta(0, 0)
						.BlocksRaycasts (false)
						.Finish();

                Anchor(AnchorPresets.HorStretchMiddle);
                Pivot(PivotPresets.MiddleCenter);
                Vector2 size = text.tmpText.GetPreferredValues ();
                SizeDelta(0, size.y);
            }

            public override void Style()
            {
                image.sprite = style.sprite;
                image.color = style.color ?? UnityEngine.Color.white;
                image.type = style.type ?? Image.Type.Sliced;
            }

			void onValueChanged (bool on)
			{
				item.SetTabVisible (on);
				image.enabled = !on;
				selected.SetActive (on);
			}

            public TabItemView Item(ITabItem item)
            {
				this.item = item;
				text.Text (item.TabName);
				UpdateTabState ();
                return this;
            }

            public TabItemView OnValueChanged(UnityAction<bool> action)
            {
                toggle.onValueChanged.AddListener(action);
                return this;
            }

			public TabItemView UpdateTabState ()
			{
				toggle.interactable = item.TabEnabled;
				if (!item.TabEnabled) {
					item.SetTabVisible (false);
				}
				return this;
			}

			public TabItemView Group (ToggleGroup group)
			{
				toggle.group = group;
				return this;
			}

			public TabItemView Select ()
			{
				toggle.isOn = true;
				return this;
			}
#region ILayoutElement
			Vector2 minSize;
			Vector2 preferredSize;

			public void CalculateLayoutInputHorizontal()
			{
				minSize = Vector2.zero;
				Vector2 size = text.tmpText.GetPreferredValues ();
				preferredSize = size;
			}

			public void CalculateLayoutInputVertical()
			{
			}

			public int layoutPriority { get { return 0; } }
			public float minWidth { get { return minSize.x; } }
			public float preferredWidth { get { return preferredSize.x; } }
			public float flexibleWidth  { get { return -1; } }
			public float minHeight { get { return minSize.y; } }
			public float preferredHeight { get { return preferredSize.y; } }
			public float flexibleHeight  { get { return -1; } }
#endregion
        }

        TabItemList items;

        public override void CreateUI()
        {
            base.CreateUI();
			ToggleGroup group;
            ToggleGroup (out group);
			items = new TabItemList (group);
			items.Content = this;
        }

        public override void Style()
        {
            base.Style();
        }

        public TabController Items (List<ITabItem> items)
        {
			this.items.Clear ();
			this.items.AddRange (items);
			UIKit.UpdateListContent (this.items);
            return this;
        }

		public TabController UpdateTabStates ()
		{
			for (int i = transform.childCount; i-- > 0; ) {
				var item = transform.GetChild (i).GetComponent<TabItemView> ();
				item.UpdateTabState ();
			}
			return this;
		}

		public int TabCount { get { return items.Count; } }

		public TabController SelectTab (int index)
		{
			items.Select (index);
			return this;
		}
    }
}
