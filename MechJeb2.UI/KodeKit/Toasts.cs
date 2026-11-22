using System;
using KodeKit.Headless;
using KSP.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KodeKit
{
    /// <summary>Where the toast stack sits on the screen.</summary>
    public enum ToastCorner { BottomRight, BottomLeft, TopRight, TopLeft }

    /// <summary>
    /// Short notices in a corner of the windows' canvas, over a <see cref="ToastQueue"/>: <c>Toasts.Show("#key", severity)</c>
    /// from any UI code on the main thread. The text may be a '#key' (localized when shown) or text already made. Each
    /// toast is logged. A click on a toast dismisses it; the pointer over the stack stops the timers. The stack follows the
    /// canvas scale and the current theme (the Tooltip style for the panel, the theme's colours for the severity).
    /// <see cref="Queue"/> is public for an observer (the agent bridge subscribes to its Posted and Closed events).
    /// <list type="bullet">
    /// <item>The pointer over a toast counts as over a window (<see cref="PointerOver"/>, in the host's hover check): MechJeb's
    /// click-through lock is on, so a click that dismisses a toast doesn't also pick the part or the map under it.</item>
    /// <item>The stack's update runs only while a toast is on screen or waiting: nothing per frame otherwise.</item>
    /// <item>A scene change dismisses every toast: a notice is about the scene it was posted in.</item>
    /// <item>The toasts draw under a dialog (<see cref="Overlay.ToastOrder"/>): its blocker dims them and takes their clicks.</item>
    /// <item>The stack sits in its corner, 16 units in, at MechJeb's scale, kept on the screen: <see cref="PopupPlacer.Corner"/>, each time
    /// it is drawn or its scale changes.</item>
    /// </list>
    /// </summary>
    public static class Toasts
    {
        public static readonly ToastQueue Queue = new ToastQueue { Label = "Notifications" };

        /// <summary>MechJeb's own UI scale (the windows' scale): the host sets it when the setting changes. KSP's UI scale applies through the canvas.</summary>
        public static float Scale = 1;

        /// <summary>The corner the stack sits in; takes effect when the stack is next built.</summary>
        public static ToastCorner Corner = ToastCorner.BottomRight;

        /// <summary>Posts a notice and logs it. Returns the toast (null for an empty text).</summary>
        public static Toast Show(string textOrKey, ToastSeverity severity = ToastSeverity.Info)
        {
            string text = KodeWindow.Loc(textOrKey);
            return Queue.Show(text, severity, (double)Time.unscaledTime);
        }

        // every post, through Show or straight into the queue (the agent bridge): logged, and the stack that draws it exists
        static Toasts() => Queue.Posted += (t, repeat) =>
        {
            Debug.Log("[MechJeb2.UI] toast (" + t.Severity + "): " + t.Text + (t.Count > 1 ? " (x" + t.Count + ")" : ""));
            ToastStack.Ensure();
        };

        /// <summary>Where the stack was last placed (its box on the toast canvas).</summary>
        public static PopupPlacement Placement => ToastStack.Placed;

        /// <summary>Whether the pointer is over a toast on screen (the host's hover check counts it as over a window).</summary>
        public static bool PointerOver => Queue.Hovered && Queue.Visible.Count > 0;

        /// <summary>The text of the toasts on screen, oldest first (for tests and an agent).</summary>
        public static string[] VisibleTexts()
        {
            var r = new string[Queue.Visible.Count];
            for (int i = 0; i < r.Length; i++) r[i] = Queue.Visible[i].Text;
            return r;
        }
    }

    /// <summary>
    /// Draws the queue's visible toasts in rows made once (MaxVisible of them) and reused: a frame with no change does no
    /// allocation. The rows are redrawn only when the queue's Version or the theme changes.
    /// </summary>
    class ToastStack : MonoBehaviour
    {
        const float Width = 300;
        const float Margin = 16;

        static ToastStack instance;

        internal static PopupPlacement Placed;

        sealed class Row : MonoBehaviour, IPointerClickHandler
        {
            public int Id;
            public RectTransform Rect;
            public Image Background, Strip;
            public TextMeshProUGUI Message, Count;

            public void OnPointerClick(PointerEventData e) => Toasts.Queue.Dismiss(Id, KodeWindow.Pointer, (double)Time.unscaledTime);
        }

        RectTransform container;
        Canvas canvasOf; // the canvas the stack was built under
        Row[] rows;
        int drawnVersion = -1, drawnTheme = -1;
        float drawnScale = 1;
        bool builtBottom, builtRight; // the corner the stack was built in

        static bool sceneHooked;

        /// <summary>Makes the stack if there is none, and runs its update (a toast was posted).</summary>
        public static void Ensure()
        {
            if (!sceneHooked)
            {
                sceneHooked = true;
                // a notice is about its scene: none shows over the next one
                GameEvents.onGameSceneLoadRequested.Add(_ => Toasts.Queue.DismissAll((double)Time.unscaledTime));
            }
            if (instance == null)
            {
                var go = new GameObject("KodeKit.Toasts");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<ToastStack>();
            }
            instance.enabled = true;
        }

        void Build()
        {
            Canvas canvas = Overlay.Root;
            canvasOf = canvas;
            var root = new GameObject("ToastCanvas", typeof(RectTransform)) { layer = canvas.gameObject.layer };
            var rt = (RectTransform)root.transform;
            rt.SetParent(canvas.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            Overlay.Setup(root.AddComponent<Canvas>(), canvas, Overlay.ToastOrder); // above windows and combo popups, under the dialogs and the tooltip
            root.AddComponent<GraphicRaycaster>();

            bool bottom = Toasts.Corner == ToastCorner.BottomLeft || Toasts.Corner == ToastCorner.BottomRight;
            bool right = Toasts.Corner == ToastCorner.BottomRight || Toasts.Corner == ToastCorner.TopRight;
            builtBottom = bottom;
            builtRight = right;
            var stack = new GameObject("Stack", typeof(RectTransform)) { layer = root.layer };
            container = (RectTransform)stack.transform;
            container.SetParent(rt, false);
            var anchor = new Vector2(right ? 1 : 0, bottom ? 0 : 1);
            container.anchorMin = container.anchorMax = container.pivot = anchor;
            container.anchoredPosition = new Vector2(right ? -Margin : Margin, bottom ? Margin : -Margin);
            var layout = stack.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            layout.childAlignment = bottom ? (right ? TextAnchor.LowerRight : TextAnchor.LowerLeft) : (right ? TextAnchor.UpperRight : TextAnchor.UpperLeft);
            var fit = stack.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            rows = new Row[Toasts.Queue.MaxVisible];
            for (int i = 0; i < rows.Length; i++) rows[i] = BuildRow(container, i);
            drawnVersion = drawnTheme = -1;
            container.localScale = new Vector3(Toasts.Scale, Toasts.Scale, 1);
            drawnScale = Toasts.Scale;
        }

        static Row BuildRow(RectTransform parent, int index)
        {
            var go = new GameObject("Toast" + index, typeof(RectTransform)) { layer = parent.gameObject.layer };
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            var row = go.AddComponent<Row>();
            row.Rect = rect;
            row.Background = go.AddComponent<Image>(); // catches the click and the pointer
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(0, 8, 5, 5);
            h.spacing = 8;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            h.childAlignment = TextAnchor.MiddleLeft;
            go.AddComponent<LayoutElement>().preferredWidth = Width;

            var strip = new GameObject("Severity", typeof(RectTransform)) { layer = go.layer };
            strip.transform.SetParent(rect, false);
            row.Strip = strip.AddComponent<Image>();
            row.Strip.raycastTarget = false;
            var sle = strip.AddComponent<LayoutElement>();
            sle.preferredWidth = 4;
            sle.flexibleHeight = 1;

            row.Message = Text(rect, "Message");
            row.Message.enableWordWrapping = true;
            row.Message.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            row.Count = Text(rect, "Count");
            row.Count.enableWordWrapping = false;
            row.Count.alignment = TextAlignmentOptions.Right;
            go.SetActive(false);
            return row;
        }

        static TextMeshProUGUI Text(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = parent.gameObject.layer };
            go.transform.SetParent(parent, false);
            TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
            t.extraPadding = true;
            t.raycastTarget = false;
            return t;
        }

        void Update()
        {
            ToastQueue q = Toasts.Queue;
            UIMasterController master = UIMasterController.Instance;
            if (master == null || master.dialogCanvas == null)
            {
                // between scenes: the queue still counts time
                q.Update((double)Time.unscaledTime);
                if (q.Empty) Idle(q, (double)Time.unscaledTime);
                return;
            }
            // a new scene has a new canvas: the stack is made again under it
            if (container == null || canvasOf != Overlay.Root)
            {
                if (container != null) Destroy(container.parent.gameObject);
                Build();
            }

            double now = (double)Time.unscaledTime;
            q.SetHovered(PointerOverStack(q), now);
            q.Update(now);

            if (drawnScale != Toasts.Scale)
            {
                drawnScale = Toasts.Scale;
                container.localScale = new Vector3(drawnScale, drawnScale, 1);
                Place();
            }

            if (q.Version != drawnVersion || Theme.Version != drawnTheme)
            {
                Theme th = Theme.Current ?? Theme.KodeDefault();
                drawnVersion = q.Version;
                drawnTheme = Theme.Version;
                Draw(q, th);
            }
            // the last toast gone (its rows hidden by the draw above): nothing to update until the next post
            if (q.Empty) Idle(q, now);
        }

        void Idle(ToastQueue q, double now)
        {
            q.SetHovered(false, now);
            enabled = false;
        }

        bool PointerOverStack(ToastQueue q)
        {
            int n = q.Visible.Count;
            if (n == 0) return false;
            Camera cam = Overlay.Root.worldCamera;
            Vector2 p = Input.mousePosition;
            for (int i = 0; i < rows.Length; i++)
                if (rows[i].gameObject.activeSelf && RectTransformUtility.RectangleContainsScreenPoint(rows[i].Rect, p, cam)) return true;
            return false;
        }

        static string CountText(int n) => n > 1 ? "×" + n : "";

        void Draw(ToastQueue q, Theme th)
        {
            for (int i = 0; i < rows.Length; i++)
            {
                Row row = rows[i];
                if (i >= q.Visible.Count)
                {
                    row.Id = 0;
                    row.gameObject.SetActive(false);
                    continue;
                }
                Toast t = q.Visible[i];
                row.Id = t.Id;
                th.Tooltip.ApplyImage(row.Background);
                row.Background.raycastTarget = true;
                row.Strip.color = Tone(th, t.Severity);
                th.ApplyText(row.Message);
                row.Message.text = t.Text;
                th.ApplyText(row.Count, th.Dim);
                row.Count.text = CountText(t.Count);
                row.Count.gameObject.SetActive(t.Count > 1);
                row.gameObject.SetActive(true);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(container);
            Place();
        }

        // the stack in its corner (PopupPlacer.Corner); the container is pivoted on that corner, so it grows away from it
        void Place()
        {
            var parent = (RectTransform)container.parent;
            Rect r = parent.rect;
            Vector2 size = container.rect.size;
            bool bottom = builtBottom, right = builtRight;
            PopupCorner corner = bottom ? (right ? PopupCorner.BottomRight : PopupCorner.BottomLeft) : (right ? PopupCorner.TopRight : PopupCorner.TopLeft);
            PopupPlacement p = PopupPlacer.Corner(size.x, size.y, Popups.Box(r), corner, Margin, container.localScale.x);
            Placed = p;
            container.anchoredPosition = new Vector2(right ? p.Box.Right - r.xMax : p.Box.Left - r.xMin, bottom ? p.Box.Bottom - r.yMin : p.Box.Top - r.yMax);
        }

        static Color Tone(Theme th, ToastSeverity s) =>
            s == ToastSeverity.Success ? th.Good : s == ToastSeverity.Warning ? th.Warn : s == ToastSeverity.Error ? th.Bad : th.Dim;

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
