using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KodeUI
{
    public class Window : LayoutPanel, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private bool isDragging = false;
        private RectTransform canvasRectTransform;
        
        public override void CreateUI()
        {
            base.CreateUI();

            // Should be handled in UIObject with more Properties ? It would make moving to a new canvas easier ?
            Canvas canvas = GetComponentInParent<Canvas>();
            canvasRectTransform = canvas.GetComponent<RectTransform>();
            
            BackgroundColor(Color.white);
        }

        public override void Style()
        {
            base.Style();
            ImageLoader.SetupImage(BackGround,"KodeUI/Default/window");
            Padding(4, 4, 0, 4);
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            if (!isDragging) 
                return;
            
            // TODO test  getting the event position  in the  window and in the canvas  and use that to move instead  of delta ?

            // For our use case rectTransform.localPosition += eventData.delta works fine but this should solve the general case
            Vector2 currentPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, eventData.position, eventData.pressEventCamera, out currentPos);
            Vector2 previousPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, eventData.position - eventData.delta, eventData.pressEventCamera, out previousPos);

            Vector3 localDelta = currentPos - previousPos;
            rectTransform.localPosition += localDelta;
        }
        
        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.pointerCurrentRaycast.gameObject == null)
                return;

            if (eventData.pointerCurrentRaycast.gameObject == gameObject)
            {
                isDragging = true;
            }
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
        }
    }
}
