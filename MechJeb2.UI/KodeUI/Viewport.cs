using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KodeUI
{
	public class Viewport : UIObject
	{
		Image image;
		Mask mask;

		public override void CreateUI()
		{
			image = gameObject.AddComponent<Image>();
			image.color = Color.white;
			ImageLoader.SetupImage (image, "KodeUI/Default/mask");

			mask = gameObject.AddComponent<Mask>();
			mask.showMaskGraphic = false;
		}

		public override void Style()
		{
		}
	}
}
