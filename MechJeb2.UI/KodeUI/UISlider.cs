using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KodeUI
{
	public class UISlider : UIObject
	{
		public Slider slider { get; private set; }

		UIObject fill;
		public UIObject Fill
		{
			get { return fill; }
			set {
				fill = value;
				if (fill != null) {
					slider.fillRect = fill.rectTransform;
				} else {
					slider.fillRect = null;
				}
			}
		}

		UIObject handle;
		public UIObject Handle
		{
			get { return handle; }
			set {
				handle = value;
				if (handle != null) {
					slider.handleRect = handle.rectTransform;
				} else {
					slider.handleRect = null;
				}
			}
		}

		public override void CreateUI()
		{
			slider = gameObject.AddComponent<Slider>();
		}

		public override void Style()
		{
		}

		public UISlider Direction(Slider.Direction direction)
		{
			slider.direction = direction;
			return this;
		}
	}
}
