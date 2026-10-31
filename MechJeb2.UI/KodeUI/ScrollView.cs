using UnityEngine;
using UnityEngine.UI;

namespace KodeUI
{
	public class ScrollView : Layout
	{
		private Image backGround;
		private ScrollRect scrollRect;
		private Layout content;
		private Viewport viewport;
		private UIScrollbar hScrollbar;
		private UIScrollbar vScrollbar;

		public Image BackGround
		{
			get { return backGround; }
		}

		public Layout Content
		{
			get { return content; }
			set {
				content = value;
				if (content != null) {
					scrollRect.content = content.rectTransform;
				} else {
					scrollRect.content = null;
				}
			}
		}

		public Viewport Viewport
		{
			get { return viewport; }
			set {
				viewport = value;
				if (viewport != null) {
					scrollRect.viewport = viewport.rectTransform;
				} else {
					scrollRect.viewport = null;
				}
			}
		}

		public UIScrollbar HorizontalScrollbar
		{
			get { return hScrollbar; }
			set { 
				hScrollbar = value;
				if (hScrollbar != null) {
					scrollRect.horizontalScrollbar = hScrollbar.scrollbar;
				} else {
					scrollRect.horizontalScrollbar = null;
				}
			}
		}

		public UIScrollbar VerticalScrollbar
		{
			get { return vScrollbar; }
			set { 
				vScrollbar = value;
				if (vScrollbar != null) {
					scrollRect.verticalScrollbar = vScrollbar.scrollbar;
				} else {
					scrollRect.verticalScrollbar = null;
				}
			}
		}

		public ScrollRect ScrollRect
		{
			get { return scrollRect; }
		}

		public override void CreateUI()
		{
			base.CreateUI();

			Anchor(AnchorPresets.StretchAll).Pivot(PivotPresets.TopLeft);
			
			rectTransform.anchoredPosition = Vector2.zero;
			
			backGround = gameObject.AddComponent<Image>();
			backGround.color = Color.clear;;

			scrollRect = gameObject.AddComponent<ScrollRect>();

			Add<Viewport>(out viewport).Anchor(AnchorPresets.StretchAll).SizeDelta(0, 0)
				.Add<Layout>(out content, "Content")
			.Finish();
			Viewport = viewport;
			Content = content;
		}

		public override void Style()
		{
			base.Style();
		}

		public ScrollView Background(Sprite sprite, Image.Type type =  Image.Type.Simple)
		{
			backGround.sprite = sprite;
			backGround.type = type;
			return this;
		}

		public ScrollView Background(string image)
		{
			ImageLoader.SetupImage(backGround,image);
			return this;
		}

		public ScrollView BackgroundColor(Color color)
		{
			backGround.color = color;
			return this;
		}

		public ScrollView Horizontal(bool enable)
		{
			scrollRect.horizontal = enable;
			return this;
		}

		public ScrollView Vertical(bool enable)
		{
			scrollRect.vertical = enable;
			return this;
		}
	}
}
