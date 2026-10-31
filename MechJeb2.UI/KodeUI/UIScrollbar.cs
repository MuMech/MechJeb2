using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KodeUI
{
	public class UIScrollbar : UIObject
	{
		Image image;
		RectTransform slidingArea;
		public Scrollbar scrollbar { get; private set; }
		Image handle;

		public override void CreateUI()
		{
			image = gameObject.AddComponent<Image>();
			image.color = Color.white;
			ImageLoader.SetupImage (image, "KodeUI/Default/background");

			scrollbar = gameObject.AddComponent<Scrollbar>();

			var go = new GameObject("Sliding Area", typeof(RectTransform));
			slidingArea = go.transform as RectTransform;
			slidingArea.SetParent (rectTransform, true);
			slidingArea.SetAnchor(AnchorPresets.StretchAll);
			slidingArea.sizeDelta = Vector2.zero;

			go = new GameObject("Handle Rect", typeof (RectTransform));
			var handleRect = go.transform as RectTransform;
			handleRect.SetParent (slidingArea, true);
			handleRect.SetAnchor(AnchorPresets.StretchAll);
			handleRect.sizeDelta = Vector2.zero;
			scrollbar.handleRect = handleRect;
			handle = go.AddComponent<Image>();
			handle.color = Color.white;
			ImageLoader.SetupImage (handle, "KodeUI/Default/background");
		}

		public override void Style()
		{
		}

		public UIScrollbar Direction(Scrollbar.Direction direction)
		{
			scrollbar.direction = direction;
			return this;
		}
	}
}
