using System;
using System.Collections.Generic;
using KodeKit.Headless;
using KodeUI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KodeKit
{
    /// <summary>
    /// A <see cref="TimelineModel"/> drawn as a horizontal strip: each lane's rows top to bottom, a span as a bar in its kind's
    /// colour, an instant as a tick, a group of items too close to tell apart as a grey box with their count, "now" as a green
    /// line, the selected item framed (past items faded, unless <see cref="FadePast"/> is off). Labels sit inside a span that has
    /// room for them, else after the item up to the next one of its row, else before it, and are hidden when neither leaves room.
    /// The strip pans and zooms through the model's <see cref="ViewportModel"/> (the
    /// wheel zooms about the pointer, a left drag pans, a double-click resets), so a strip on a graph's view moves with the graph.
    /// A click selects (see <see cref="TimelineModel.Click"/>). Keys are not routed: in flight they are flight and camera keys;
    /// a host or the agent bridge calls <see cref="TimelineModel.Key"/>.
    /// <see cref="Sync"/> runs every frame (registered on the window) and places nothing until the items, the selection, the
    /// view, "now" or the strip's width changed; it allocates nothing then, except a mark the first time an item needs one and
    /// the labels' texts when the items change.
    /// </summary>
    public sealed class Timeline
    {
        public readonly TimelineModel Model;

        /// <summary>The strip: the items are placed in it by anchors, from its top.</summary>
        public readonly RectTransform Strip;

        /// <summary>The strip's pointer input.</summary>
        public readonly TimelineInput Input;

        /// <summary>The strip's layout cell: its height follows the model's rows.</summary>
        public readonly LayoutElement Size;

        /// <summary>The text drawn for an item, asked when the items change or the language does (null: its label key, localized).</summary>
        public Func<TimelineItem, string> LabelText;

        /// <summary>An item's colour (null: a colour per kind from <see cref="KindColors"/>).</summary>
        public Func<TimelineItem, Color> ItemColor;

        /// <summary>How near an item the pointer picks it, and how close items are grouped, in the strip's pixels.</summary>
        public float PickPixels = 6, GroupPixels = 8;

        /// <summary>The height of one row, from the theme's text.</summary>
        public float RowHeight { get; private set; } = 18;

        public static Color BackColor = new Color(0.07f, 0.08f, 0.09f, 1f);
        public static Color NowColor = new Color(0.45f, 1f, 0.55f, 0.95f);
        public static Color GroupColor = new Color(0.62f, 0.64f, 0.70f, 1f);
        public static Color SelectColor = new Color(1f, 1f, 1f, 1f);
        public static Color LaneLineColor = new Color(1f, 1f, 1f, 0.10f);

        public static Color[] KindColors =
        {
            new Color(0.35f, 0.65f, 1f), new Color(1f, 0.85f, 0.35f), new Color(0.95f, 0.45f, 0.35f), new Color(0.55f, 0.85f, 0.45f),
            new Color(0.80f, 0.55f, 0.95f), new Color(0.40f, 0.85f, 0.85f),
        };

        /// <summary>The alpha of an item that finished before "now".</summary>
        public static float PastAlpha = 0.45f;

        /// <summary>Items that finished before "now" are drawn faded (off where everything shown is past: a recording).</summary>
        public bool FadePast = true;

        // one item's or one group's graphics: a bar or a tick, and a label beside it
        sealed class Mark
        {
            public Image Body;
            public RectTransform Rect;
            public UIText Label;
            public RectTransform LabelRect;
            public string Text;
            public float TextWidth;
        }

        readonly UIObject surface;
        readonly List<Mark> itemMarks = new List<Mark>();
        readonly List<Mark> groupMarks = new List<Mark>();
        readonly List<Image> laneLines = new List<Image>();
        readonly Image nowLine;
        readonly RectTransform selection;
        float[] rowEdge = new float[4], prevInRow = new float[8];
        Theme theme;
        float labelSize = 12;

        int shownVersion = -1, shownViewVersion = -1, shownNow = -1, shownRows = -1, labelled = -1;
        float shownWidth = -1;
        double shownGroupSpan = double.NaN;

        /// <param name="width">The strip's preferred width; it takes the row's spare width too.</param>
        public Timeline(KodeWindow w, UIObject parent, TimelineModel model, float width)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            // its description's label: the window's title unless the host named the timeline
            if (string.IsNullOrEmpty(model.Label)) model.Label = w.Behaviour.Frame.Label;
            Strip = Graphs.Surface(parent, width, RowHeight + 2, "Timeline");
            surface = Strip.GetComponent<UIObject>();
            Size = Strip.GetComponent<LayoutElement>();
            Size.flexibleWidth = 1;
            Strip.gameObject.AddComponent<RectMask2D>();
            var back = Graphs.Fill<Image>(Strip, "Background");
            back.color = BackColor;
            back.raycastTarget = false;

            Input = TimelineInput.Add(this);

            var outline = Graphs.Fill<Image>(Strip, "Frame");
            outline.sprite = Sprites.Outline;
            outline.type = Image.Type.Sliced;
            outline.color = new Color(0.30f, 0.32f, 0.35f, 1f);
            outline.raycastTarget = false;

            var sel = Graphs.Fill<Image>(Strip, "Selection");
            sel.sprite = Sprites.Outline;
            sel.type = Image.Type.Sliced;
            sel.color = SelectColor;
            sel.raycastTarget = false;
            selection = sel.rectTransform;
            selection.anchorMin = selection.anchorMax = new Vector2(0, 1);
            selection.gameObject.SetActive(false);

            nowLine = Graphs.Fill<Image>(Strip, "Now");
            nowLine.color = NowColor;
            nowLine.raycastTarget = false;
            nowLine.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            nowLine.gameObject.SetActive(false);

            KodeWindow.Styled(Strip.gameObject, Restyle);
            w.Behaviour.AddRelocalize(() => labelled = -1);
            w.AddSync(Sync);
        }

        void Restyle(Theme th)
        {
            theme = th;
            labelSize = Mathf.Max(9, th.FontSize * 0.85f);
            RowHeight = Mathf.Max(16, Mathf.Round(labelSize * 1.5f));
            foreach (Mark m in itemMarks) StyleLabel(m);
            foreach (Mark m in groupMarks) StyleLabel(m);
            labelled = shownRows = -1;
            shownVersion = -1;
        }

        void StyleLabel(Mark m)
        {
            if (theme == null) return;
            theme.ApplyText(m.Label.tmpText);
            m.Label.tmpText.fontSize = labelSize;
            m.Text = null; // measured again
        }

        Mark NewMark(string name, bool label)
        {
            var m = new Mark { Body = Graphs.Fill<Image>(Strip, name) };
            m.Body.raycastTarget = false;
            m.Rect = m.Body.rectTransform;
            m.Rect.anchorMin = m.Rect.anchorMax = new Vector2(0, 1);
            // under the frame, the selection and "now"
            m.Rect.SetSiblingIndex(Input.transform.GetSiblingIndex() + 1);
            if (label)
            {
                m.Label = surface.Add<UIText>(name + "Label");
                m.Label.Finish();
                TMP_Text t = m.Label.tmpText;
                t.enableWordWrapping = false;
                t.overflowMode = TextOverflowModes.Overflow;
                t.alignment = TextAlignmentOptions.Left;
                t.raycastTarget = false;
                m.LabelRect = m.Label.rectTransform;
                m.LabelRect.anchorMin = m.LabelRect.anchorMax = new Vector2(0, 1);
                m.LabelRect.pivot = new Vector2(0, 1);
                StyleLabel(m);
            }

            return m;
        }

        /// <summary>The label drawn for an item (null when it has no mark yet).</summary>
        public TMP_Text LabelOf(TimelineItem item)
        {
            int i = item?.Order ?? -1;
            return i >= 0 && i < itemMarks.Count ? itemMarks[i].Label.tmpText : null;
        }

        /// <summary>The graphic drawn for an item (null when it has no mark yet).</summary>
        public RectTransform MarkOf(TimelineItem item)
        {
            int i = item?.Order ?? -1;
            return i >= 0 && i < itemMarks.Count ? itemMarks[i].Rect : null;
        }

        /// <summary>The box drawn for a group (null past the ones drawn).</summary>
        public RectTransform GroupMark(int g) => g >= 0 && g < groupMarks.Count ? groupMarks[g].Rect : null;

        /// <summary>The selection's frame (inactive when nothing selected is in view).</summary>
        public RectTransform SelectionFrame => selection;

        /// <summary>The "now" line (inactive when "now" is out of view).</summary>
        public RectTransform NowLine => nowLine.rectTransform;

        /// <summary>Where a display row's middle is, from the strip's top, in pixels (down is negative).</summary>
        public float RowMiddle(int row) => -(row + 0.5f) * RowHeight - 1;

        /// <summary>Places the graphics when the items, the selection, the view, "now" or the width changed.</summary>
        public void Sync()
        {
            float width = Strip.rect.width;
            ViewportModel view = Model.View;
            if (width > 0)
            {
                Model.PickFraction = PickPixels / width;
                Model.GroupFraction = GroupPixels / width;
            }

            bool place = Model.Version != shownVersion || view.Version != shownViewVersion || width != shownWidth ||
                !view.Span.Equals(shownGroupSpan);
            bool now = Model.NowVersion != shownNow;
            if (!place && !now) return;
            shownNow = Model.NowVersion;
            if (place)
            {
                shownVersion = Model.Version;
                shownViewVersion = view.Version;
                shownWidth = width;
                shownGroupSpan = view.Span;
                Place(width);
            }
            else Recolor();

            PlaceNow(width);
        }

        void Place(float width)
        {
            int rows = Math.Max(1, Model.RowCount);
            if (rows != shownRows)
            {
                shownRows = rows;
                Size.preferredHeight = Size.minHeight = rows * RowHeight + 2;
            }

            PlaceLaneLines();
            bool relabel = labelled != Model.Version;
            labelled = Model.Version;

            int count = Model.Count;
            while (itemMarks.Count < count) itemMarks.Add(NewMark("Item" + itemMarks.Count, true));
            if (rowEdge.Length < rows) rowEdge = new float[rows * 2];
            if (prevInRow.Length < count) prevInRow = new float[count * 2];

            // first in time order: the labels, and where the previous item of each row (with a label after it) ends
            for (int r = 0; r < rows; r++) rowEdge[r] = float.NegativeInfinity;
            for (int i = 0; i < count; i++)
            {
                TimelineItem it = Model.Ordered(i);
                Mark m = itemMarks[i];
                if (relabel || m.Text == null) SetLabel(m, Text(it));
                TimelineGroup g = Model.GroupOf(it);
                int row = it.DisplayRow;
                prevInRow[i] = rowEdge[row];
                rowEdge[row] = g != null ? X(g.End, width) + 6 : X(it.Finish, width) + 4 + m.TextWidth;
            }

            // then from the last item back, so each knows where the next one of its row starts
            for (int r = 0; r < rows; r++) rowEdge[r] = float.PositiveInfinity;
            for (int i = count - 1; i >= 0; i--)
            {
                TimelineItem it = Model.Ordered(i);
                Mark m = itemMarks[i];
                TimelineGroup g = Model.GroupOf(it);
                float x0 = X(it.Start, width), x1 = X(it.Finish, width);
                int row = it.DisplayRow;
                float next = Math.Min(rowEdge[row], width);
                rowEdge[row] = g != null ? X(g.Start, width) : x0;
                if (g != null || x1 < -1 || x0 > width + 1)
                {
                    Hide(m);
                    continue;
                }

                Show(m.Body.gameObject, true);
                float top = -row * RowHeight - 2, bottom = -(row + 1) * RowHeight + 1;
                if (it.IsInstant) PlaceRect(m.Rect, x0 - 1, x0 + 1, top, bottom);
                else PlaceRect(m.Rect, x0, Math.Max(x1, x0 + 2), top, bottom);
                m.Body.color = ColorOf(it);

                // the label inside a span that has room, else after the item up to the next one of its row (or the strip's
                // end), else before it down to the previous one, else none
                float inside = it.IsInstant ? 0 : Math.Min(x1, width) - Math.Max(x0, 0) - 6;
                float after = (it.IsInstant ? x0 + 1 : x1) + 3, before = (it.IsInstant ? x0 - 1 : x0) - 3 - m.TextWidth;
                if (m.TextWidth <= inside) PlaceLabel(m, Math.Max(x0, 0) + 3, row, true, it);
                else if (next - after - 3 >= m.TextWidth) PlaceLabel(m, after, row, true, it);
                else PlaceLabel(m, before, row, before >= Math.Max(0, prevInRow[i]), it);
            }

            for (int i = count; i < itemMarks.Count; i++) Hide(itemMarks[i]);

            PlaceGroups(width);
            PlaceSelection(width);
        }

        void PlaceGroups(float width)
        {
            int groups = Model.GroupCount;
            while (groupMarks.Count < groups) groupMarks.Add(NewMark("Group" + groupMarks.Count, true));
            for (int g = 0; g < groupMarks.Count; g++)
            {
                Mark m = groupMarks[g];
                if (g >= groups)
                {
                    Hide(m);
                    continue;
                }

                TimelineGroup gr = Model.Group(g);
                SetLabel(m, CountText(gr.Count));
                float x0 = X(gr.Start, width), x1 = X(gr.End, width);
                float w = Math.Max(x1 - x0, m.TextWidth + 6);
                float mid = (x0 + x1) / 2;
                x0 = mid - w / 2;
                x1 = mid + w / 2;
                if (x1 < 0 || x0 > width)
                {
                    Hide(m);
                    continue;
                }

                Show(m.Body.gameObject, true);
                int row = gr.DisplayRow;
                PlaceRect(m.Rect, x0, x1, -row * RowHeight - 2, -(row + 1) * RowHeight + 1);
                bool past = FadePast && gr.End < Model.Now;
                m.Body.color = past ? Faded(GroupColor) : GroupColor;
                PlaceLabel(m, x0 + 3, row, true, null);
                m.Label.tmpText.color = Color.black;
            }
        }

        void PlaceSelection(float width)
        {
            TimelineItem sel = Model.Selected;
            RectTransform target = null;
            if (sel != null)
            {
                TimelineGroup g = Model.GroupOf(sel);
                if (g != null)
                {
                    for (int i = 0; i < Model.GroupCount; i++)
                        if (Model.Group(i) == g)
                            target = groupMarks[i].Rect;
                }
                else target = itemMarks[sel.Order].Rect;
            }

            bool shown = target != null && target.gameObject.activeSelf;
            Show(selection.gameObject, shown);
            if (!shown) return;
            selection.anchorMin = selection.anchorMax = new Vector2(0, 1);
            selection.pivot = target.pivot;
            selection.offsetMin = target.offsetMin - new Vector2(2, 2);
            selection.offsetMax = target.offsetMax + new Vector2(2, 2);
        }

        // "now" moved alone: the line, and the items it went past
        void Recolor()
        {
            int count = Model.Count;
            for (int i = 0; i < count && i < itemMarks.Count; i++)
            {
                Mark m = itemMarks[i];
                if (!m.Body.gameObject.activeSelf) continue;
                TimelineItem it = Model.Ordered(i);
                Color c = ColorOf(it);
                if (m.Body.color != c) m.Body.color = c;
                Color tc = LabelColor(it);
                if (m.Label.tmpText.color != tc) m.Label.tmpText.color = tc;
            }
        }

        void PlaceNow(float width)
        {
            double f = Model.NowFraction;
            bool shown = f >= 0 && f <= 1;
            Show(nowLine.gameObject, shown);
            if (!shown) return;
            RectTransform r = nowLine.rectTransform;
            r.anchorMin = new Vector2((float)f, 0);
            r.anchorMax = new Vector2((float)f, 1);
            r.sizeDelta = new Vector2(2, 0);
            r.anchoredPosition = Vector2.zero;
        }

        void PlaceLaneLines()
        {
            int lanes = 0;
            for (int lane = 1; Model.TopOf(lane) >= 0; lane++)
            {
                if (Model.RowsOf(lane) == 0) continue;
                if (lanes == laneLines.Count)
                {
                    var line = Graphs.Fill<Image>(Strip, "Lane" + lanes);
                    line.color = LaneLineColor;
                    line.raycastTarget = false;
                    line.rectTransform.SetSiblingIndex(Input.transform.GetSiblingIndex() + 1);
                    laneLines.Add(line);
                }

                RectTransform r = laneLines[lanes].rectTransform;
                Show(r.gameObject, true);
                r.anchorMin = new Vector2(0, 1);
                r.anchorMax = new Vector2(1, 1);
                float y = -Model.TopOf(lane) * RowHeight - 0.5f;
                r.offsetMin = new Vector2(0, y - 0.5f);
                r.offsetMax = new Vector2(0, y + 0.5f);
                lanes++;
            }

            for (int i = lanes; i < laneLines.Count; i++) Show(laneLines[i].gameObject, false);
        }

        string Text(TimelineItem it) => LabelText != null ? LabelText(it) : KodeWindow.Loc(it.LabelKey);

        static readonly string[] counts = new string[100];

        static string CountText(int n)
        {
            if (n >= counts.Length) return n.ToString();
            return counts[n] ?? (counts[n] = n.ToString());
        }

        void SetLabel(Mark m, string text)
        {
            text = text ?? "";
            if (text == m.Text) return;
            m.Text = text;
            m.Label.tmpText.text = text;
            m.TextWidth = m.Label.tmpText.GetPreferredValues(text).x;
        }

        void PlaceLabel(Mark m, float x, int row, bool shown, TimelineItem it)
        {
            shown = shown && m.Text.Length > 0;
            Show(m.Label.gameObject, shown);
            if (!shown) return;
            m.LabelRect.sizeDelta = new Vector2(m.TextWidth + 2, RowHeight);
            m.LabelRect.anchoredPosition = new Vector2(x, -row * RowHeight - 1);
            if (it != null) m.Label.tmpText.color = LabelColor(it);
        }

        Color ColorOf(TimelineItem it)
        {
            Color c = ItemColor != null ? ItemColor(it) : KindColors[(it.Kind % KindColors.Length + KindColors.Length) % KindColors.Length];
            if (FadePast && Model.IsPast(it)) return Faded(c);
            // a span drawn under its label is darker than an instant's tick
            if (!it.IsInstant && !Model.IsCurrent(it)) c.a *= 0.7f;
            return c;
        }

        Color LabelColor(TimelineItem it)
        {
            Color c = theme != null ? theme.Text : Color.white;
            return FadePast && Model.IsPast(it) ? Faded(c) : c;
        }

        static Color Faded(Color c)
        {
            c.a *= PastAlpha;
            return c;
        }

        // a data value's x in the strip, held to a few widths either side (a far value would make a huge rect)
        float X(double value, float width)
        {
            double f = Model.View.ToFraction(value);
            return (float)(Math.Max(-2, Math.Min(3, f)) * width);
        }

        static void PlaceRect(RectTransform r, float x0, float x1, float top, float bottom)
        {
            r.pivot = new Vector2(0, 1);
            r.offsetMin = new Vector2(x0, bottom);
            r.offsetMax = new Vector2(x1, top);
        }

        static void Hide(Mark m)
        {
            Show(m.Body.gameObject, false);
            if (m.Label != null) Show(m.Label.gameObject, false);
        }

        static void Show(GameObject go, bool on)
        {
            if (go.activeSelf != on) go.SetActive(on);
        }
    }

    /// <summary>
    /// The pointer on a <see cref="Timeline"/>: the wheel zooms about the pointer, a left drag pans (the value grabbed stays
    /// under the pointer, anchored to the press so a real mouse's drag threshold loses nothing), a click selects
    /// (<see cref="TimelineModel.Click"/>), a double-click resets the view. It takes the drag, so a drag on the strip doesn't move
    /// the window. The right button does nothing.
    /// </summary>
    public sealed class TimelineInput : MonoBehaviour, IScrollHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        public Timeline Owner;
        RectTransform rt;
        double grabbed;

        /// <summary>A drag is panning the view.</summary>
        public bool Dragging { get; private set; }

        /// <summary>What the last click did (for tests).</summary>
        public TimelineClick LastClick { get; private set; }

        public static TimelineInput Add(Timeline owner)
        {
            var area = Graphs.Fill<RaycastArea>(owner.Strip, "TimelineInput");
            area.color = Color.clear;
            TimelineInput input = area.gameObject.AddComponent<TimelineInput>();
            input.Owner = owner;
            input.rt = area.rectTransform;
            return input;
        }

        ViewportModel View => Owner.Model.View;

        Vector2 Local(Vector2 screen, Camera eventCam)
        {
            if (eventCam == null)
            {
                // events without a camera (e.g. sent by code) on a ScreenSpaceCamera canvas: use the canvas camera
                Canvas root = GetComponentInParent<Canvas>().rootCanvas;
                if (root.renderMode != RenderMode.ScreenSpaceOverlay) eventCam = root.worldCamera;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screen, eventCam, out Vector2 local);
            return local;
        }

        /// <summary>Where a screen point falls across the strip: 0 at its left edge, 1 at its right (not clamped).</summary>
        public double Fraction(Vector2 screen, Camera eventCam)
        {
            Vector2 local = Local(screen, eventCam);
            Rect r = rt.rect;
            return r.width > 0 ? (local.x - r.xMin) / r.width : 0.5;
        }

        /// <summary>The display row under a screen point (-1 above the strip).</summary>
        public int Row(Vector2 screen, Camera eventCam)
        {
            Vector2 local = Local(screen, eventCam);
            float down = rt.rect.yMax - 1 - local.y;
            return down < 0 ? -1 : (int)(down / Owner.RowHeight);
        }

        public void OnScroll(PointerEventData e) =>
            View.Wheel(e.scrollDelta.y, Math.Max(0, Math.Min(1, Fraction(e.position, e.enterEventCamera))), KodeWindow.Pointer);

        public void OnBeginDrag(PointerEventData e)
        {
            Dragging = false;
            if (e.button != PointerEventData.InputButton.Left) return;
            Dragging = true;
            grabbed = View.FromFraction(Fraction(e.pressPosition, e.pressEventCamera));
            OnDrag(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (!Dragging) return;
            View.PanTo(grabbed - Fraction(e.position, e.pressEventCamera) * View.Span, KodeWindow.Pointer);
        }

        public void OnEndDrag(PointerEventData e) => Dragging = false;

        public void OnPointerClick(PointerEventData e)
        {
            // uGUI clicks after a drag that ended on the object it started on: that was a drag, not a click
            if (e.dragging || e.button != PointerEventData.InputButton.Left) return;
            if (e.clickCount == 2)
            {
                View.Reset(KodeWindow.Pointer);
                return;
            }

            LastClick = Owner.Model.Click(Fraction(e.position, e.pressEventCamera), Row(e.position, e.pressEventCamera), KodeWindow.Pointer);
        }

        void OnDisable() => Dragging = false;
    }
}
