using System;
using KodeKit;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MuMech.UI
{
    /// <summary>
    /// The usual resize grip in a window's bottom-right corner. Dragging it reports the size the window's right and bottom edges
    /// would give, in the window's own units, from the drag's start: Resized(width, height). The window applies what it can.
    /// </summary>
    sealed class ResizeGrip : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public RectTransform Root;
        public Action<float, float> Resized;
        public Action Done;
        public bool Dragging { get; private set; }
        Vector2 offset;

        static Sprite sprite;

        // three diagonal stripes in the bottom-right corner (KodeUI's sprites have none)
        static Sprite Sprite
        {
            get
            {
                if (sprite != null) return sprite;
                const int n = 12;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Point };
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int d = n - 1 - x + y; // 0 in the bottom-right corner
                    tex.SetPixel(x, y, d < n && d % 4 >= 2 ? Color.white : Color.clear);
                }
                tex.Apply();
                return sprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
            }
        }

        /// <summary>A grip in the window's bottom-right corner, out of its layout.</summary>
        public static ResizeGrip Add(KodeWindow w, Action<float, float> resized, Action done)
        {
            var go = new GameObject("ResizeGrip", typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(ResizeGrip));
            go.transform.SetParent(w.Root.transform, false);
            go.GetComponent<LayoutElement>().ignoreLayout = true;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1, 0);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(12, 12);
            var img = go.GetComponent<Image>();
            img.sprite = Sprite;
            KodeWindow.Styled(go, th => img.color = new Color(th.Text.r, th.Text.g, th.Text.b, 0.45f));
            var g = go.GetComponent<ResizeGrip>();
            g.Root = w.Root.rectTransform;
            g.Resized = resized;
            g.Done = done;
            return g;
        }

        // a screen point from the window's top-left corner, x right and y down
        bool Local(Vector2 screen, Camera cam, out Vector2 p)
        {
            bool ok = RectTransformUtility.ScreenPointToLocalPointInRectangle(Root, screen, cam, out Vector2 local);
            Rect r = Root.rect;
            p = new Vector2(local.x + Root.pivot.x * r.width, (1 - Root.pivot.y) * r.height - local.y);
            return ok;
        }

        // from where the button went down: a drag begins only once the pointer has passed the event system's drag threshold,
        // and measuring from there would lose those first pixels
        public void OnBeginDrag(PointerEventData e)
        {
            if (!Local(e.pressPosition, e.pressEventCamera, out Vector2 p)) return;
            offset = new Vector2(Root.rect.width, Root.rect.height) - p;
            Dragging = true;
            Resized(Root.rect.width, Root.rect.height);
        }

        public void OnDrag(PointerEventData e)
        {
            if (Dragging && Local(e.position, e.pressEventCamera, out Vector2 p)) Resized(p.x + offset.x, p.y + offset.y);
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (!Dragging) return;
            Dragging = false;
            Done();
        }

        // hidden mid-drag (the window locked, or closed): the drag gets no end, so end it here
        void OnDisable() => OnEndDrag(null);
    }
}
