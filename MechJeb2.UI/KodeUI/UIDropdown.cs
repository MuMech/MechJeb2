using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace KodeUI
{
	using OptionData = TMP_Dropdown.OptionData;

	public class UIDropdown : UIObject, ILayoutElement
	{
		class KodeUI_Dropdown : TMP_Dropdown
		{
			protected override GameObject CreateDropdownList(GameObject template)
			{
				var dropdown = base.CreateDropdownList(template);
				var canvas = dropdown.GetComponent<Canvas> ();
				var rootCanvas = gameObject.GetComponentInParent<Canvas>().rootCanvas;
				canvas.sortingLayerID = rootCanvas.sortingLayerID;
				return dropdown;
			}
		}
		Image background;
		KodeUI_Dropdown dropdown;
		ScrollView scrollView;

		const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
		static FieldInfo m_Value = typeof(TMP_Dropdown).GetField ("m_Value", bindingFlags);

		public class TextItem : UIToggle
		{
			UIText text;

			public UIText Text { get { return text; } }

			public override void CreateUI()
			{
				base.CreateUI();

				Add<UIText>(out text, "ItemText").Text("ItemText").Anchor(AnchorPresets.StretchAll).SizeDelta(0, 0).Finish();

				Anchor(AnchorPresets.HorStretchMiddle);
				Pivot(PivotPresets.MiddleCenter);
				Vector2 size = text.tmpText.GetPreferredValues ();
				SizeDelta(0, size.y);

				checkMark.Anchor(AnchorPresets.VertStretchLeft).Pivot(PivotPresets.MiddleLeft).SizeDelta(size.y, 0);
				text.SizeDelta(-size.y, 0).X(size.y);
			}
		}

		public Layout Content { get { return scrollView.Content; } }

		public bool interactable
		{
			get { return dropdown.interactable; }
			set { dropdown.interactable = value; }
		}

		UIImage arrow;
		public UIImage Arrow
		{
			get { return arrow; }
			set { arrow = value; }
		}

		UIText caption;
		public UIText Caption
		{
			get { return caption; }
			set {
				caption = value;
				if (caption != null) {
					dropdown.captionText = caption.tmpText;
				}
			}
		}

		TextItem item;
		public TextItem Item
		{
			get { return item; }
			set {
				item = value;
				if (item != null) {
					item.rectTransform.SetParent (Content.rectTransform, true);
				}
			}
		}

		Vector2 minSize;
		Vector2 preferredSize;

		public void CalculateLayoutInputHorizontal()
		{
			float cm = LayoutUtility.GetMinSize(caption.rectTransform, 0);
			float am = LayoutUtility.GetMinSize(arrow.rectTransform, 0);
			float cp = LayoutUtility.GetPreferredSize(caption.rectTransform, 0);
			float ap = LayoutUtility.GetPreferredSize(arrow.rectTransform, 0);
			Vector2 size = caption.tmpText.GetPreferredValues ();
			minSize.x = cm + am;
			preferredSize.x = Mathf.Max(cp, size.x) + ap;
		}

		public void CalculateLayoutInputVertical()
		{
			float cm = LayoutUtility.GetMinSize(caption.rectTransform, 1);
			float am = LayoutUtility.GetMinSize(arrow.rectTransform, 1);
			float cp = LayoutUtility.GetPreferredSize(caption.rectTransform, 1);
			float ap = LayoutUtility.GetPreferredSize(arrow.rectTransform, 1);
			Vector2 size = caption.tmpText.GetPreferredValues ();
			minSize.y = Mathf.Max(cm, am);
			preferredSize.y = Mathf.Max(Mathf.Max(cp, size.y), ap);
		}

		public int layoutPriority { get { return 0; } }
		public float minWidth { get { return minSize.x; } }
		public float preferredWidth { get { return preferredSize.x; } }
		public float flexibleWidth  { get { return -1; } }
		public float minHeight { get { return minSize.y; } }
		public float preferredHeight { get { return preferredSize.y; } }
		public float flexibleHeight  { get { return -1; } }

        public override void CreateUI()
		{
			background = gameObject.AddComponent<Image>();

			dropdown = gameObject.AddComponent<KodeUI_Dropdown>();

			Add<UIText> (out caption, "Label").Text("D").Alignment (TextAlignmentOptions.Left).Margin (5, 5, 6, 6).Anchor(AnchorPresets.StretchAll).SizeDelta (0, 0).Finish();
			Add<UIImage> (out arrow, "Arrow").Anchor(AnchorPresets.VertStretchRight).X(-15).Y(0).PreferredHeight(0).Pivot(PivotPresets.MiddleCenter).SizeDelta(20,0).Finish();

			UIScrollbar scrollbar;
			Add<ScrollView>(out scrollView, "Template").Horizontal(false).Vertical(true).Anchor(AnchorPresets.HorStretchBottom).Pivot(PivotPresets.TopCenter).SizeDelta(0, 150)
				.Add<UIScrollbar>(out scrollbar, "Scrollbar").Direction(Scrollbar.Direction.BottomToTop).Anchor(AnchorPresets.VertStretchRight).SizeDelta(10,0).Finish()
			.Finish();

			scrollView.VerticalScrollbar = scrollbar;
			scrollView.Viewport.Pivot(PivotPresets.TopLeft);
			scrollView.Content.Anchor(AnchorPresets.HorStretchTop).Pivot(PivotPresets.TopCenter)
				.Add<TextItem>(out item, "Item").Finish()
			.SizeDelta(0, item.rectTransform.sizeDelta.y).Finish();

			scrollView.SetActive(false);

			dropdown.template = scrollView.rectTransform;
			dropdown.itemText = item.Text.tmpText;
			Caption = caption;
		}

        public override void Style()
		{
			dropdown.colors = style.stateColors ?? ColorBlock.defaultColorBlock;
			if (style.stateSprites.HasValue) {
				dropdown.spriteState = style.stateSprites.Value;
			}
			background.sprite = style.sprite;
			background.color = style.color ?? UnityEngine.Color.white;
			background.type = style.type ?? Image.Type.Sliced;
		}

		public UIDropdown Options(List<OptionData> options)
		{
			dropdown.options = options;
			return this;
		}

		public UIDropdown OnValueChanged (UnityAction<int> action)
		{
			dropdown.onValueChanged.AddListener(action);
			return this;
		}

		public UIDropdown SetValueWithoutNotify (int value)
		{
			value = Mathf.Clamp(value, 0, dropdown.options.Count - 1);
			m_Value.SetValue (dropdown, value);
			dropdown.RefreshShownValue ();
			return this;
		}
	}
}
