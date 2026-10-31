using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KodeUI
{
	public class Viewport : UIObject
	{
		Image image;
		Mask mask;

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
			image = gameObject.AddComponent<Image>();

			mask = gameObject.AddComponent<Mask>();
			mask.showMaskGraphic = false;
		}

		public override void Style()
		{
			image.sprite = style.sprite;
			image.color = style.color ?? UnityEngine.Color.white;
			image.type = style.type ?? UnityEngine.UI.Image.Type.Sliced;
		}
	}
}
