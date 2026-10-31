using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KodeUI
{
	public class UIScrollbar : UIObject
	{
		Image image;
		UIEmpty slidingArea;
		UIImage handle;
		public Scrollbar scrollbar { get; private set; }

		public bool interactable
		{
			get { return scrollbar.interactable; }
			set { scrollbar.interactable = value; }
		}

		public override void CreateUI()
		{
			image = gameObject.AddComponent<Image>();

			scrollbar = gameObject.AddComponent<Scrollbar>();

			Add<UIEmpty> (out slidingArea, "Sliding Area").Anchor(AnchorPresets.StretchAll).SizeDelta(0, 0)
				.Add<UIImage> (out handle, "Handle").Type(Image.Type.Sliced).Anchor(AnchorPresets.StretchAll).SizeDelta(0, 0).Finish()
			.Finish();

			scrollbar.handleRect = handle.rectTransform;
		}

		public override void Style()
		{
			scrollbar.colors = style.stateColors ?? ColorBlock.defaultColorBlock;
			scrollbar.transition = style.transition ?? Selectable.Transition.ColorTint;

			if (style.stateSprites.HasValue) {
				scrollbar.spriteState = style.stateSprites.Value;
			}

			image.sprite = style.sprite;
			image.color = style.color ?? UnityEngine.Color.white;
			image.type = style.type ?? Image.Type.Sliced;
		}

		public UIScrollbar Direction(Scrollbar.Direction direction)
		{
			scrollbar.direction = direction;
			return this;
		}
	}
}
