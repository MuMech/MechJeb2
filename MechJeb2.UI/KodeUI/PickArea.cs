using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KodeUI
{
    /// <summary>Normalised pointer position over a rect while pressed/dragged (colour picker areas, plots).</summary>
    public class PickArea : MonoBehaviour, IPointerDownHandler, IDragHandler, IBeginDragHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public Action<Vector2> Picked;      // normalised 0..1, while pressed
        public Action<Vector2> Released;    // normalised, on pointer up
        public Action<Vector2> Hover;       // normalised, while hovered (polled in Update)
        public bool Hovered { get; private set; }
        RectTransform rt;
        Camera cam;

        void Awake() => rt = (RectTransform)transform;

        public Vector2 Normalised(Vector2 screen, Camera eventCam)
        {
            if (eventCam == null)
            {
                // events without a camera (e.g. sent by code) on a ScreenSpaceCamera canvas: use the canvas camera
                Canvas root = GetComponentInParent<Canvas>().rootCanvas;
                if (root.renderMode != RenderMode.ScreenSpaceOverlay) eventCam = root.worldCamera;
            }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screen, eventCam, out Vector2 local);
            Rect r = rt.rect;
            return new Vector2(Mathf.Clamp01((local.x - r.xMin) / r.width), Mathf.Clamp01((local.y - r.yMin) / r.height));
        }

        public void OnPointerDown(PointerEventData e)
        {
            cam = e.pressEventCamera;
            Picked?.Invoke(Normalised(e.position, cam));
        }

        // implementing drag here keeps the window from being dragged while picking
        public void OnBeginDrag(PointerEventData e) { }
        public void OnDrag(PointerEventData e) => Picked?.Invoke(Normalised(e.position, e.pressEventCamera));
        public void OnPointerUp(PointerEventData e) => Released?.Invoke(Normalised(e.position, e.pressEventCamera));
        public void OnPointerEnter(PointerEventData e) { Hovered = true; cam = e.enterEventCamera; }
        public void OnPointerExit(PointerEventData e) => Hovered = false;

        void Update()
        {
            if (Hovered && Hover != null) Hover(Normalised(Input.mousePosition, cam));
        }
    }
}
