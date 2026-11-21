using System;
using KodeKit.Headless;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KodeKit
{
    /// <summary>
    /// Drag on the rows of a <see cref="VirtualList{TRow}"/>: to reorder the list (<see cref="ReorderModel"/>) or to drop a row into
    /// another list (<see cref="TransferModel"/>), drawn with a drop indicator in the target list, a label of the dragged row by the
    /// pointer, and auto-scroll near the target's top and bottom edges (<see cref="AutoScrollModel"/>).
    /// <list type="bullet">
    /// <item>Each row is the nearest drag handler under the pointer, so a drag that starts on a row moves neither the window
    /// (its DragHandle) nor the list (its ScrollRect); the wheel and the scrollbar still scroll the list.</item>
    /// <item>Only the left button drags. A drag never clicks: its row's click is cancelled when the drag starts.</item>
    /// <item>The list is unchanged until the drop; a release outside the target list's viewport drops nothing (the way to cancel:
    /// Escape is KSP's pause key and isn't read).</item>
    /// <item>The pointer is read every frame during the drag, not only when it moves, so the indicator follows an auto-scroll.</item>
    /// <item>When the drag starts, uGUI's drag target becomes an object of the list that stays active (<see cref="ListDragCatcher"/>),
    /// so a row recycled out of view mid-drag doesn't lose the drag's events. Only its end event drops: a drag uGUI lets go of without
    /// one (<c>dragging</c> false, or another <c>pointerDrag</c>), or the game losing the focus (a release outside it), cancels.</item>
    /// <item>No key moves a row: Alt is KSP's MODIFIER_KEY and the arrows turn the camera, and a list row holds no keyboard lock.
    /// Keyboard and agent moves call the model (<see cref="ReorderModel.MoveBy"/>, <see cref="TransferModel.Transfer"/>).</item>
    /// </list>
    /// </summary>
    public sealed class ListDrag
    {
        /// <summary>How far outside the target's viewport (above or below, in list units) the pointer still auto-scrolls it.</summary>
        public const float ScrollReach = 40;

        public readonly IVirtualList Source, Target;
        public readonly ReorderModel Reorder;
        public readonly TransferModel Transfer;
        public readonly AutoScrollModel AutoScroll = new AutoScrollModel();

        readonly KodeWindow window;
        readonly Func<int, string> label;
        readonly Image indicator;
        readonly RectTransform indicatorRect;
        RectTransform ghost;
        TextMeshProUGUI ghostText;
        Image ghostBack;

        PointerEventData ped;
        readonly GameObject catcher;
        Camera cam;

        /// <summary>What the last drop did (for tests and the agent bridge).</summary>
        public ListMove LastMove { get; private set; }

        public ListTransfer LastTransfer { get; private set; }

        /// <summary>Drags cancelled because uGUI let go of them without an end event, or the game lost the focus (for tests).</summary>
        public int CancelledWithoutEnd { get; private set; }

        /// <summary>A drag to reorder <paramref name="list"/>; <paramref name="label"/> gives a row's text for the label by the pointer.</summary>
        public static ListDrag ForReorder(KodeWindow w, IVirtualList list, ReorderModel model, Func<int, string> label) =>
            new ListDrag(w, list, list, model, null, label);

        /// <summary>A drag from <paramref name="source"/>'s rows into <paramref name="target"/>.</summary>
        public static ListDrag ForTransfer(KodeWindow w, IVirtualList source, IVirtualList target, TransferModel model, Func<int, string> label) =>
            new ListDrag(w, source, target, null, model, label);

        ListDrag(KodeWindow w, IVirtualList source, IVirtualList target, ReorderModel reorder, TransferModel transfer, Func<int, string> label)
        {
            window = w;
            Source = source;
            Target = target;
            Reorder = reorder;
            Transfer = transfer;
            this.label = label;

            foreach (GameObject row in source.RowObjects) Attach(row);
            source.RowCreated += Attach;

            // the drag's own target while it lasts: an empty object of the source list, active as long as the list is
            catcher = new GameObject("DragCatcher", typeof(RectTransform), typeof(LayoutElement)) { layer = source.ScrollRect.gameObject.layer };
            catcher.transform.SetParent(source.ScrollRect.transform, false);
            catcher.GetComponent<LayoutElement>().ignoreLayout = true;
            ((RectTransform)catcher.transform).sizeDelta = Vector2.zero;
            catcher.AddComponent<ListDragCatcher>().Owner = this;

            // the indicator: a line across the target's content, drawn over its rows, never hit by the pointer
            var go = new GameObject("DropIndicator", typeof(RectTransform), typeof(Image), typeof(LayoutElement)) { layer = target.ContentRect.gameObject.layer };
            go.transform.SetParent(target.ContentRect, false);
            go.GetComponent<LayoutElement>().ignoreLayout = true;
            indicatorRect = (RectTransform)go.transform;
            indicatorRect.anchorMin = new Vector2(0, 1);
            indicatorRect.anchorMax = new Vector2(1, 1);
            indicatorRect.pivot = new Vector2(0.5f, 0.5f);
            indicatorRect.offsetMin = new Vector2(2, 0);
            indicatorRect.offsetMax = new Vector2(-2, 0);
            indicatorRect.sizeDelta = new Vector2(indicatorRect.sizeDelta.x, 2);
            indicator = go.GetComponent<Image>();
            indicator.raycastTarget = false;
            KodeWindow.Styled(go, th => indicator.color = th.Accent);
            go.SetActive(false);

            w.AddSync(Tick);
        }

        public bool Dragging => Reorder != null ? Reorder.Dragging : Transfer.Dragging;

        /// <summary>The target's gap under the pointer (-1: outside it, or no drag).</summary>
        public int Gap => Reorder != null ? Reorder.Gap : Transfer.Gap;

        /// <summary>True while the indicator shows (a drop now changes the list).</summary>
        public bool IndicatorShown => indicator != null && indicator.gameObject.activeSelf;

        /// <summary>The indicator, for tests: its line in the target's content.</summary>
        public RectTransform Indicator => indicatorRect;

        /// <summary>The label that follows the pointer (null before the first drag).</summary>
        public RectTransform Ghost => ghost;

        void Attach(GameObject row)
        {
            ListDragRow h = row.GetComponent<ListDragRow>() ?? row.AddComponent<ListDragRow>();
            h.Owner = this;
        }

        internal void Begin(GameObject row, PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left) return;
            int index = Source.IndexOfObject(row);
            bool started = Reorder != null ? Reorder.Begin(index, KodeWindow.Pointer) : Transfer.Begin(index, KodeWindow.Pointer);
            if (!started) return;
            // released over the same row, a drag isn't a click
            e.eligibleForClick = false;
            // uGUI sends the rest of the drag to pointerDrag: an object that a scroll can't recycle
            e.pointerDrag = catcher;
            ped = e;
            // the canvas's camera, not the event's: an event sent by code has no raycast to take it from
            Canvas canvas = Target.ContentRect.GetComponentInParent<Canvas>().rootCanvas;
            cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            AutoScroll.Reset();
            ShowGhost(index);
            Follow(e.position);
        }

        internal void Moved(PointerEventData e)
        {
            if (e != ped || !Dragging) return;
            Follow(e.position);
        }

        internal void End(PointerEventData e)
        {
            if (e != ped || !Dragging) return;
            Follow(e.position);
            Finish();
        }

        /// <summary>The game lost the focus: a release outside it sends uGUI nothing, so the drag is cancelled.</summary>
        internal void FocusLost()
        {
            if (ped == null) return;
            CancelledWithoutEnd++;
            Cancel();
        }

        /// <summary>Ends a drag in progress without a change (the list changed, the view is rebuilt, the window closes).</summary>
        public void Cancel()
        {
            Reorder?.Cancel();
            Transfer?.Cancel();
            Clear();
        }

        void Finish()
        {
            if (Reorder != null) LastMove = Reorder.Drop();
            else LastTransfer = Transfer.Drop();
            Clear();
        }

        void Clear()
        {
            ped = null;
            AutoScroll.Reset();
            if (indicator.gameObject.activeSelf) indicator.gameObject.SetActive(false);
            if (ghost != null && ghost.gameObject.activeSelf) ghost.gameObject.SetActive(false);
        }

        void Tick()
        {
            if (ped == null) return;
            if (!Dragging)
            {
                Clear();
                return;
            }
            // uGUI let go of the drag without its end event: nothing is dropped
            if (!ped.dragging || ped.pointerDrag != catcher)
            {
                CancelledWithoutEnd++;
                Cancel();
                return;
            }
            Follow(ped.position);

            // auto-scroll: the pointer over the target's column, near (or past) its top or bottom edge
            RectTransform vp = Target.ViewportRect;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(vp, ped.position, cam, out Vector2 local)) return;
            Rect r = vp.rect;
            float fromTop = r.yMax - local.y;
            if (local.x < r.xMin || local.x > r.xMax || fromTop < -ScrollReach || fromTop > r.height + ScrollReach)
            {
                AutoScroll.Reset();
                return;
            }
            float step = AutoScroll.Step(fromTop, r.height, Time.unscaledDeltaTime);
            if (step == 0) return;
            float before = Target.Model.Offset;
            Target.SetOffset(before + step, KodeWindow.Pointer);
            if (Target.Model.Offset != before) Follow(ped.position);
        }

        // the pointer at a screen point: the gap under it in the target, the indicator, the label
        void Follow(Vector2 screen)
        {
            RectTransform vp = Target.ViewportRect;
            bool inside = RectTransformUtility.ScreenPointToLocalPointInRectangle(vp, screen, cam, out Vector2 inVp) && vp.rect.Contains(inVp);
            if (inside && RectTransformUtility.ScreenPointToLocalPointInRectangle(Target.ContentRect, screen, cam, out Vector2 inContent))
            {
                float y = -inContent.y;
                if (Reorder != null) Reorder.OverAt(Target.Model, y);
                else Transfer.OverAt(Target.Model, y);
            }
            else if (Reorder != null) Reorder.Outside();
            else Transfer.Outside();

            bool show = Reorder != null ? Reorder.ShowsIndicator : Transfer.ShowsIndicator;
            if (show)
            {
                float h = Target.Model.ContentHeight;
                // inside the content, so the mask doesn't cut the line at the list's ends
                float y = Mathf.Clamp(ListDrop.IndicatorY(Target.Model, Gap), 1, Mathf.Max(1, h - 1));
                Vector2 p = indicatorRect.anchoredPosition;
                if (Math.Abs(p.y + y) > 0.01f) indicatorRect.anchoredPosition = new Vector2(p.x, -y);
                if (!indicator.gameObject.activeSelf)
                {
                    indicator.transform.SetAsLastSibling();
                    indicator.gameObject.SetActive(true);
                }
            }
            else if (indicator.gameObject.activeSelf) indicator.gameObject.SetActive(false);

            if (ghost != null && ghost.gameObject.activeSelf)
            {
                var root = (RectTransform)ghost.parent;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, cam, out Vector2 inRoot))
                {
                    // right of and below the pointer, so it hides neither the indicator nor the row under the pointer
                    Rect rr = root.rect;
                    Vector2 at = new Vector2(inRoot.x - rr.xMin + 14, inRoot.y - rr.yMax - 10);
                    if ((ghost.anchoredPosition - at).sqrMagnitude > 0.01f) ghost.anchoredPosition = at;
                }
            }
        }

        void ShowGhost(int index)
        {
            if (label == null) return;
            if (ghost == null)
            {
                RectTransform root = window.Root.rectTransform;
                var go = new GameObject("DragLabel", typeof(RectTransform), typeof(Image), typeof(LayoutElement)) { layer = root.gameObject.layer };
                go.transform.SetParent(root, false);
                go.GetComponent<LayoutElement>().ignoreLayout = true;
                ghost = (RectTransform)go.transform;
                ghost.anchorMin = ghost.anchorMax = new Vector2(0, 1);
                ghost.pivot = new Vector2(0, 1);
                ghostBack = go.GetComponent<Image>();
                ghostBack.raycastTarget = false;
                // a frame in the accent colour, so the label stands out from the rows it passes over
                Outline frame = go.AddComponent<Outline>();
                frame.effectDistance = new Vector2(1, -1);

                var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)) { layer = go.layer };
                textGo.transform.SetParent(go.transform, false);
                var trt = (RectTransform)textGo.transform;
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = new Vector2(6, 1);
                trt.offsetMax = new Vector2(-6, -1);
                ghostText = textGo.GetComponent<TextMeshProUGUI>();
                ghostText.raycastTarget = false;
                ghostText.enableWordWrapping = false;
                ghostText.alignment = TextAlignmentOptions.Left;
                KodeWindow.Styled(go, th =>
                {
                    th.ApplyText(ghostText);
                    ghostBack.color = new Color(0.16f, 0.18f, 0.21f, 0.97f);
                    frame.effectColor = th.Accent;
                });
            }
            string text = label(index) ?? "";
            ghostText.text = text;
            Vector2 size = TextMeasure.Measure(text, KodeWindow.ThemeOf(ghost.gameObject));
            ghost.sizeDelta = new Vector2(size.x + 12, size.y + 2);
            ghost.SetAsLastSibling();
            ghost.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// The drag handler on each row of a <see cref="ListDrag"/>'s source list: the nearest one under the pointer, so the drag is the
    /// row's. It starts the drag, which then goes to the list's <see cref="ListDragCatcher"/>.
    /// </summary>
    public sealed class ListDragRow : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        internal ListDrag Owner;

        public void OnBeginDrag(PointerEventData e) => Owner?.Begin(gameObject, e);

        public void OnDrag(PointerEventData e) => Owner?.Moved(e);

        public void OnEndDrag(PointerEventData e) => Owner?.End(e);
    }

    /// <summary>A drag's target once it has started: an object of the list that a scroll doesn't recycle; it also hears the focus loss.</summary>
    public sealed class ListDragCatcher : MonoBehaviour, IDragHandler, IEndDragHandler
    {
        internal ListDrag Owner;

        public void OnDrag(PointerEventData e) => Owner?.Moved(e);

        public void OnEndDrag(PointerEventData e) => Owner?.End(e);

        void OnApplicationFocus(bool focus)
        {
            if (!focus) Owner?.FocusLost();
        }
    }
}
