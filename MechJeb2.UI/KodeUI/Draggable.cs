using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace KodeUI
{
    public class Draggable : UIBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
    {
        Vector2 dragAnchor;

		RectTransform rectTransform
		{
			get { return transform as RectTransform; }
		}

        void DoDrag(PointerEventData eventData)
        {
            Vector2 currentPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out currentPos);
            Vector3 localDelta = currentPos - dragAnchor;
            rectTransform.localPosition += localDelta;
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            DoDrag(eventData);
        }

        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            DoDrag(eventData);
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {
            DoDrag(eventData);
        }

        public virtual void OnPointerDown(PointerEventData eventData)
        {
			if (eventData.button == PointerEventData.InputButton.Left) {
				RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out dragAnchor);
			}
        }
    }
}
