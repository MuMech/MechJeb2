using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KodeUI
{
	public class UIRepeatButton : UIButton, IPointerDownHandler, IPointerUpHandler
	{
		float interval = 1;
		bool pressed;

		public override void CreateUI()
		{
			base.CreateUI();
		}

		public UIRepeatButton Rate(float perSecond)
		{
			if (perSecond < 1) {
				perSecond = 1;
			}
			interval = 1 / perSecond;
			return this;
		}

		IEnumerator RepeatPress()
		{
			while (pressed) {
				button.onClick.Invoke();
				yield return new WaitForSecondsRealtime(interval);
			}
		}

		public virtual void OnPointerDown(PointerEventData eventData)
		{
			pressed = true;
			StartCoroutine(RepeatPress());
		}

		public virtual void OnPointerUp(PointerEventData eventData)
		{
			pressed = false;
			StopAllCoroutines();
		}
	}
}
