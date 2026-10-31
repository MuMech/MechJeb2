using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace KodeUI
{
	using OptionData = TMP_Dropdown.OptionData;

	public class UIDropdown : UIObject
	{
		Image background;
		TMP_Dropdown dropdown;
		ScrollView scrollView;

		public class TextItem : UIObject
		{
			Toggle toggle;
			UIImage background;
			UIText text;

			public UIImage Background { get { return background; } }
			public UIText Text { get { return text; } }

			public override void CreateUI()
			{
				toggle = gameObject.AddComponent<Toggle>();

				Add<UIImage>(out background).Anchor(AnchorPresets.StretchAll).SizeDelta(0, 0).Finish()
				.Add<UIText>(out text).Text("Item").Anchor(AnchorPresets.StretchAll).SizeDelta(0, 0).Finish();
				Anchor(AnchorPresets.HorStretchMiddle);
				Pivot(PivotPresets.MiddleCenter);
				Vector2 size = text.tmpText.GetPreferredValues ();
				SizeDelta(0, size.y);

				toggle.image = background.image;
			}

			public override void Style()
			{
			}
		}

		public Layout Content { get { return scrollView.Content; } }

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

        public override void CreateUI()
		{
			background = gameObject.AddComponent<Image>();
			background.color = Color.white;
			ImageLoader.SetupImage (background, "KodeUI/Default/background");

			dropdown = gameObject.AddComponent<TMP_Dropdown>();

			Add<UIText> (out caption, "Label").Anchor(AnchorPresets.StretchAll).SizeDelta (0, 0).Finish();
			Add<UIImage> (out arrow, "Arrow").Anchor(AnchorPresets.VertStretchRight).X(-15).Y(0).Pivot(PivotPresets.MiddleCenter).SizeDelta(20,0).Finish();

			UIScrollbar scrollbar;
			Add<ScrollView>(out scrollView, "Template").Horizontal(false).Vertical(true).Anchor(AnchorPresets.HorStretchBottom).Pivot(PivotPresets.TopCenter).SizeDelta(0, 150)
				.Add<UIScrollbar>(out scrollbar, "Scrollbar").Direction(Scrollbar.Direction.BottomToTop).Anchor(AnchorPresets.VertStretchRight).SizeDelta(10,0).Finish()
			.Finish();

			scrollView.VerticalScrollbar = scrollbar;
			scrollView.Viewport.Pivot(PivotPresets.TopLeft);
			scrollView.Content.Anchor(AnchorPresets.HorStretchTop).Pivot(PivotPresets.TopCenter)
				.Add<TextItem>(out item, "Item").Finish()
			.SizeDelta(0, item.rectTransform.sizeDelta.y).Finish();

			scrollView.gameObject.SetActive(false);

			dropdown.template = scrollView.rectTransform;
			dropdown.itemText = item.Text.tmpText;
			Caption = caption;
		}

        public override void Style()
		{
		}

		public UIDropdown Options(List<OptionData> options)
		{
			dropdown.options = options;
			return this;
		}
	}
}
