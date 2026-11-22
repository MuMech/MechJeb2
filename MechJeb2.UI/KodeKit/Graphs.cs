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
    public static class Graphs
    {
        static Texture2D VerticalGradient(Color bottom, Color top, int h = 64)
        {
            var t = new Texture2D(1, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < h; y++) t.SetPixel(0, y, Color.Lerp(bottom, top, y / (h - 1f)));
            t.Apply();
            return t;
        }

        /// <summary>A fixed-size layout cell; children are positioned by anchors (no layout group: redraws never relayout).</summary>
        public static RectTransform Surface(UIObject parent, float width, float height, string name = "Surface")
        {
            UIEmpty cell = parent.Add<UIEmpty>(name);
            cell.Finish();
            var le = cell.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = height;
            return cell.rectTransform;
        }

        public static T Fill<T>(RectTransform parent, string name) where T : Component
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = parent.gameObject.layer };
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return go.AddComponent<T>();
        }

        /// <summary>Unity's DefaultControls scrollbar, moved to the UI layer (DefaultControls creates on layer 0).</summary>
        public static Scrollbar HorizontalScrollbar(UIObject parent)
        {
            var res = new DefaultControls.Resources();
            Theme th = Theme.Current ?? Theme.KodeDefault();
            res.background = th.Box.Normal;
            res.standard = th.Button.Normal;
            GameObject go = DefaultControls.CreateScrollbar(res);
            go.transform.SetParent(parent.rectTransform, false);
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = parent.gameObject.layer;
            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.preferredHeight = 14;
            return KodeWindow.NoSelect(go.GetComponent<Scrollbar>());
        }

        public static RawImage Background(RectTransform surface, Color bottom, Color top)
        {
            var img = Fill<RawImage>(surface, "Background");
            img.texture = OwnedTextures.Own(img, VerticalGradient(bottom, top));
            img.raycastTarget = false;
            return img;
        }
    }

    /// <summary>
    /// MechJebModuleFlightRecorderGraph's drawing, retained: background gradient, one mesh per series,
    /// stage markers, Y scale labels at round values, horizontal scrollbar + follow toggle. Call Refresh() when data changed (10 Hz).
    /// The X axis is a <see cref="ViewportModel"/> (<see cref="View"/>): the host gives it the data's range and the span its scale
    /// controls pick; the wheel over the plot zooms about the pointer, a drag pans, a double-click resets, and the scrollbar and
    /// the follow toggle are views of it. Refresh again when <see cref="ViewMoved"/>. <see cref="EnableRanges"/> adds a range and
    /// marked points on the same axis (<see cref="RangeModel"/>), drawn over the plot.
    /// </summary>
    public class LineGraph
    {
        public readonly RectTransform Plot;
        public readonly List<LineSeriesGraphic> Series = new List<LineSeriesGraphic>();
        public readonly LineSeriesGraphic Stages;
        readonly LabelColumn yLabels;
        readonly Layout area;
        readonly float builtHeight;
        const float LabelHeight = 18;

        /// <summary>The Y labels on the right of the plot, for a second graph: null until AddRightLabels.</summary>
        public RectTransform RightLabels => rightLabels?.Column;
        LabelColumn rightLabels;
        public readonly Scrollbar Scroll;

        /// <summary>The X axis's window over the data: what the plot shows, and what the pointer pans and zooms.</summary>
        public readonly ViewportModel View;

        /// <summary>The plot's pointer input (wheel, drag, double-click) on the view.</summary>
        public readonly ViewportInput Input;

        /// <summary>The range and points picked on the plot: null until <see cref="EnableRanges"/>.</summary>
        public RangeModel Ranges { get; private set; }

        /// <summary>The range and points' graphics: null until <see cref="EnableRanges"/>.</summary>
        public RangeOverlay RangeOverlay { get; private set; }

        public bool Follow
        {
            get => View.Follow;
            set => View.Follow = value;
        }

        public double VisibleWidth => View.Span;  // data units across the plot
        public double DataMaxX => View.DataMax;   // rightmost data x
        public double ViewMin => View.Start;      // left edge of the view

        int refreshedMove = -1;

        /// <summary>A pan, zoom or reset moved the view since the last Refresh: refresh now, the series are drawn with the old
        /// window. The data growing is not a move: the host's own refresh rate covers it.</summary>
        public bool ViewMoved => View.MoveVersion != refreshedMove;

        /// <summary>
        /// Writes a Y label: the text to fill, the value and the step between labels (0 when the range has no ticks). Null: the
        /// value in SI with the significant figures that tell neighbours apart, 3 at least.
        /// </summary>
        public Action<CharText, double, double> LabelFormat;

        /// <summary>
        /// A LabelFormat at the precision of the step between labels: whole numbers for a step of 1 or more, as many decimals as
        /// a smaller step needs, and k or M for large values (90, 80 … rather than 90.0, 80.0; 100 k, 90 k rather than 100 k, 90.0 k).
        /// </summary>
        public static void StepLabel(CharText t, double v, double step)
        {
            double unit = Math.Abs(step) >= 1e6 ? 1e6 : Math.Abs(step) >= 1e3 ? 1e3 : 1;
            double scaled = step > 0 ? step / unit : 0.1;
            int decimals = Math.Max(0, (int)Math.Ceiling(-Math.Log10(scaled) - 1e-9));
            t.Append(v / unit, decimals);
            if (unit == 1e6) t.Append(" M");
            else if (unit == 1e3) t.Append(" k");
        }

        readonly RectTransform frame;

        /// <param name="scrollbarParent">Where the scrollbar row goes (null: under the plot, indented past the Y labels).
        /// The Flight Recorder passes its window, so the scrollbar spans it as in IMGUI.</param>
        /// <param name="labelCount">The most Y labels shown: how many the height has room for.</param>
        /// <param name="view">The X axis's viewport, which a host keeps across rebuilds to keep the zoom (null: a new one,
        /// following the data's end).</param>
        public LineGraph(KodeWindow w, UIObject parent, float width, float height, int labelCount = 5, UIObject scrollbarParent = null,
            ViewportModel view = null)
        {
            View = view ?? new ViewportModel(0, 0, 100) { Follow = true };
            // its description's label: the window's title unless the host named the graph
            if (string.IsNullOrEmpty(View.Label)) View.Label = w.Behaviour.Frame.Label;
            area = KodeWindow.Row(parent);
            // Y labels column, fixed width, absolute positions
            RectTransform labels = Graphs.Surface(area, 70, height, "YLabels");
            builtHeight = height;
            Plot = Graphs.Surface(area, width, height, "Plot");
            Plot.gameObject.AddComponent<RectMask2D>();
            Graphs.Background(Plot, new Color(0.15f, 0.25f, 0.45f), new Color(0.02f, 0.02f, 0.08f));
            // GUILayout.Box's border around the plot, kept above the series (AddSeries moves it back on top)
            var outline = Graphs.Fill<Image>(Plot, "Frame");
            outline.sprite = Sprites.Outline;
            outline.type = Image.Type.Sliced;
            outline.color = new Color(0.30f, 0.32f, 0.35f, 1f);
            outline.raycastTarget = false;
            frame = outline.rectTransform;

            // under the series: takes the pointer for the view (the background and the series don't raycast)
            Input = ViewportInput.Add(Plot, View);

            Stages = Graphs.Fill<LineSeriesGraphic>(Plot, "Stages");
            Stages.VerticalMarkers = true;
            Stages.Thickness = 1;
            Stages.color = new Color(0.5f, 0.5f, 0.5f);
            Stages.raycastTarget = false;

            yLabels = new LabelColumn(labels, labelCount, height, false);

            Layout bottom = KodeWindow.Row(scrollbarParent ?? parent);
            if (scrollbarParent == null) KodeWindow.Space(bottom, 74);
            Scroll = Graphs.HorizontalScrollbar(bottom);
            Scroll.onValueChanged.AddListener(v => View.ScrollTo(v, KodeWindow.Pointer));
            // the follow toggle has no text: keep it to its box, so the scrollbar gets all the spare width
            Toggle follow = w.Named(w.Toggle(bottom, "", () => View.Follow, SetFollow), "#MechJeb_NewUI_Label_FollowEnd"); //Follow the end
            var followCell = follow.transform.parent.GetComponent<LayoutElement>() ?? follow.transform.parent.gameObject.AddComponent<LayoutElement>();
            followCell.flexibleWidth = 0;
            w.AddSync(Tick);
        }

        // on, the view goes to the data's end; off, it stays where it is
        void SetFollow(bool on)
        {
            // after the move to the end: without AutoFollow (the PSG window) the move itself would leave Follow off
            if (on) View.Key(ViewportKey.End, KodeWindow.Pointer);
            View.Follow = on;
        }

        /// <summary>
        /// A range and marked points on the X axis (a host keeps the model across rebuilds as it keeps the view; it must be on this
        /// graph's <see cref="View"/>; null: a new one). Over the plot, a right drag selects a range (from the body: moves it; from
        /// an end: moves that end), a left drag on an end or a point moves it (elsewhere it pans), a right click marks a point or
        /// removes the one under it. Drawn as a shaded band with its two ends and a line per point, kept in place each frame.
        /// </summary>
        public RangeModel EnableRanges(RangeModel ranges = null)
        {
            if (Ranges != null) return Ranges;
            if (ranges != null && ranges.View != View) throw new ArgumentException("the range model is on another viewport", nameof(ranges));
            Ranges = ranges ?? new RangeModel(View);
            if (string.IsNullOrEmpty(Ranges.Label)) Ranges.Label = View.Label;
            Input.Ranges = Ranges;
            RangeOverlay = new RangeOverlay(Plot, Input.transform, Ranges);
            frame.SetAsLastSibling();
            return Ranges;
        }

        /// <summary>The hover read-out: null until <see cref="EnableReadout"/>.</summary>
        public GraphReadout Readout { get; private set; }

        /// <summary>
        /// A hover read-out (<see cref="GraphReadout"/>): while the pointer rests or moves over the plot with no button held, a
        /// cursor line at it and a card beside it with the X there and the value of each series the host adds
        /// (<see cref="GraphReadout.AddRow"/>), read on this graph's <see cref="View"/>, so zoomed and panned too.
        /// </summary>
        public GraphReadout EnableReadout()
        {
            if (Readout != null) return Readout;
            Readout = new GraphReadout(this, new GraphReadoutModel(View));
            Input.Readout = Readout;
            frame.SetAsLastSibling();
            return Readout;
        }

        /// <summary>A second column of Y labels on the right of the plot, for another graph's range: see RefreshRight.</summary>
        public RectTransform AddRightLabels(int labelCount)
        {
            RectTransform column = Graphs.Surface(area, 70, builtHeight, "YLabelsRight");
            rightLabels = new LabelColumn(column, labelCount, builtHeight, true);
            return column;
        }

        /// <summary>The right labels for this Y range, in this colour (no allocation in steady state).</summary>
        public void RefreshRight(double yMin, double yMax, Color color)
        {
            if (rightLabels == null) return;
            rightLabels.Show(yMin, yMax, LabelFormat);
            rightLabels.Tint(color);
        }

        /// <summary>A series, drawn over the view's window up to the plot's edges (see <see cref="WindowedSource"/>).</summary>
        public LineSeriesGraphic AddSeries(IGraphSource source, Color color)
        {
            var g = Graphs.Fill<LineSeriesGraphic>(Plot, "Series" + Series.Count);
            g.Source = source == null ? null : new WindowedSource(source);
            g.color = color;
            g.raycastTarget = false;
            Series.Add(g);
            frame.SetAsLastSibling();
            return g;
        }

        /// <summary>Update the view window and Y range, redraw the meshes and labels. No allocation in steady state. After a move of
        /// the view (<see cref="ViewMoved"/>: a pan, zoom or reset) every mesh is rebuilt in the next frame, so it shows at once;
        /// otherwise (the data grew) one per frame, as before.</summary>
        public void Refresh(double yMin, double yMax)
        {
            bool moved = ViewMoved;
            refreshedMove = View.MoveVersion;
            Scroll.SetValueWithoutNotify((float)View.ScrollValue);
            float size = (float)View.ScrollSize;
            if (Scroll.size != size) Scroll.size = size;

            foreach (LineSeriesGraphic s in Series) Apply(s, yMin, yMax);
            Apply(Stages, 0, 1);
            if (moved)
            {
                foreach (LineSeriesGraphic s in Series) s.SetVerticesDirty();
                Stages.SetVerticesDirty();
                pending = 0;
            }
            else pending = Series.Count + 1;

            yLabels.Show(yMin, yMax, LabelFormat);
        }

        static readonly int[] stepMultiples = { 1, 2, 5 };

        // a column of Y labels beside the plot: right-aligned against its left edge, or left-aligned against its right edge
        sealed class LabelColumn
        {
            public readonly RectTransform Column;
            readonly CharText[] chars;
            readonly TMP_Text[] texts;
            readonly RectTransform[] cells;
            readonly float builtHeight;
            readonly bool right;
            double labelledMin = double.NaN, labelledMax = double.NaN;
            float labelledHeight = -1;

            public LabelColumn(RectTransform column, int count, float builtHeight, bool right)
            {
                Column = column;
                this.builtHeight = builtHeight;
                this.right = right;
                chars = new CharText[count];
                texts = new TMP_Text[count];
                cells = new RectTransform[count];
                for (int i = 0; i < count; i++)
                {
                    // the column is a KodeUI UIEmpty without layout group: Show() places the cells, from the bottom
                    UIText t = KodeWindow.LiveLabel(column.GetComponent<UIObject>(), "-000.0 k", false);
                    RectTransform cell = (RectTransform)t.transform.parent;
                    cell.anchorMin = cell.anchorMax = new Vector2(right ? 0 : 1, 0);
                    cell.pivot = new Vector2(right ? 0 : 1, 0.5f);
                    cell.sizeDelta = new Vector2(66, LabelHeight);
                    t.tmpText.alignment = right ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;
                    chars[i] = new CharText(t.tmpText);
                    texts[i] = t.tmpText;
                    cells[i] = cell;
                }
            }

            // A label at each whole multiple of the step in the range. The cells left over are hidden. A range with no
            // extent, or one that is not a number, has one label: its lower limit.
            public void Show(double yMin, double yMax, Action<CharText, double, double> format)
            {
                float height = Column.rect.height > 0 ? Column.rect.height : builtHeight;
                if (yMin.Equals(labelledMin) && yMax.Equals(labelledMax) && height == labelledHeight) return;
                labelledMin = yMin;
                labelledMax = yMax;
                labelledHeight = height;

                double range = yMax - yMin;
                bool ticked = Ticks(yMin, yMax, chars.Length, out double step, out double first, out int count, out int sigFigs);
                if (!ticked) count = Math.Min(1, chars.Length);

                for (int i = 0; i < chars.Length; i++)
                {
                    GameObject cell = cells[i].gameObject;
                    if (cell.activeSelf != i < count) cell.SetActive(i < count);
                    if (i >= count) continue;

                    double v = ticked ? (first + i) * step : yMin;
                    // centred on its tick, an end label would stick out over the rows around the plot
                    float y = ticked ? (float)((v - yMin) / range) * height : 0;
                    cells[i].anchoredPosition = new Vector2(right ? 4 : -4, Mathf.Clamp(y, LabelHeight / 2, height - LabelHeight / 2));
                    if (format != null) format(chars[i].Clear(), v, ticked ? step : 0);
                    else chars[i].Clear().AppendSI(v, sigFigs);
                    chars[i].Commit();
                }
            }

            public void Tint(Color color)
            {
                foreach (TMP_Text t in texts)
                    if (t.color != color) t.color = color;
            }
        }

        /// <summary>
        /// The round values of a range: count of them, from first × step up, one step apart. The step is the smallest
        /// 1, 2 or 5 × 10^n that gives no more than cells values. sigFigs is how many significant figures tell two
        /// neighbours apart, 3 at least. False, with no value, when the range has no extent or
        /// is not a number.
        /// </summary>
        internal static bool Ticks(double yMin, double yMax, int cells, out double step, out double first, out int count, out int sigFigs)
        {
            step = first = 0;
            count = 0;
            sigFigs = 3;
            double range = yMax - yMin;
            if (cells < 1 || !(range > 0) || double.IsInfinity(range)) return false;

            double ticks = 0;
            int exponent = (int)Math.Floor(Math.Log10(range / cells));
            for (bool found = false;; exponent++)
            {
                double magnitude = Math.Pow(10, exponent);
                foreach (int multiple in stepMultiples)
                {
                    step = magnitude * multiple;
                    // the tolerance keeps a limit that is a multiple of the step, short of it by a rounding error:
                    // that error grows with how far the range is from zero, in steps
                    double tolerance = Math.Max(1e-6, 1e-12 * Math.Max(Math.Abs(yMin), Math.Abs(yMax)) / step);
                    first = Math.Ceiling(yMin / step - tolerance);
                    ticks = Math.Floor(yMax / step + tolerance) - first + 1;
                    found = ticks <= cells;
                    if (found) break;
                }
                if (found) break;
            }

            count = (int)Math.Max(ticks, 0);
            // the figures that reach from the largest value's first one down to the step's
            double largest = Math.Max(Math.Abs(first), Math.Abs(first + count - 1)) * step;
            if (largest > 0) sigFigs = Mathf.Clamp((int)Math.Floor(Math.Log10(largest) + 1e-9) - exponent + 1, 3, 15);
            return true;
        }

        void Apply(LineSeriesGraphic s, double yMin, double yMax)
        {
            s.XMin = View.Start;
            s.XMax = View.End;
            s.YMin = yMin;
            s.YMax = yMax;
            if (s.Source is WindowedSource ws) ws.SetWindow(View.Start, View.End);
        }

        // after a Refresh, one series is rebuilt per frame instead of all at once (spreads the mesh cost)
        int pending;

        /// <summary>Called every frame (registered as a window sync).</summary>
        void Tick()
        {
            RangeOverlay?.Sync();
            if (pending <= 0) return;
            pending--;
            (pending < Series.Count ? Series[pending] : Stages).SetVerticesDirty();
        }
    }

    /// <summary>
    /// A series as a graphic over a window of X reads it: the points inside the window, with a point on each of the window's
    /// edges interpolated from the two points around it, so a zoomed line runs to the plot's edges instead of stopping at the
    /// first and last point inside. The points are taken as the graphics take them: from the first at or after the window's
    /// start, up to the first after its end. The window is read when the graphic asks for <see cref="Count"/>, at its rebuild,
    /// so points added since are included. No allocation.
    /// </summary>
    public sealed class WindowedSource : IGraphSource
    {
        public readonly IGraphSource Inner;
        double xMin = double.NegativeInfinity, xMax = double.PositiveInfinity;
        int first, end;              // the inner points shown: first .. end - 1
        bool leftEdge, rightEdge;    // an interpolated point before first, after end - 1
        double leftY, rightX, rightY;

        public WindowedSource(IGraphSource inner) => Inner = inner;

        public void SetWindow(double min, double max)
        {
            xMin = min;
            xMax = max;
        }

        public int Count
        {
            get
            {
                int n = Inner.Count;
                first = 0;
                while (first < n && Inner.X(first) < xMin) first++;
                end = first;
                while (end < n && Inner.X(end) <= xMax) end++;
                leftEdge = first > 0 && first < n;
                if (leftEdge) leftY = Lerp(first - 1, first, xMin);
                rightEdge = end < n && end > 0;
                // a hair inside the end: on it, float rounding can put the point past the plot's edge, where the graphic stops
                rightX = xMax - (xMax - xMin) * 1e-6;
                if (rightEdge) rightY = Lerp(end - 1, end, rightX);
                return end - first + (leftEdge ? 1 : 0) + (rightEdge ? 1 : 0);
            }
        }

        double Lerp(int a, int b, double x)
        {
            double xa = Inner.X(a), xb = Inner.X(b), ya = Inner.Y(a), yb = Inner.Y(b);
            if (!(xb > xa)) return yb;
            return ya + (yb - ya) * (x - xa) / (xb - xa);
        }

        public double X(int i)
        {
            if (leftEdge)
            {
                if (i == 0) return xMin;
                i--;
            }

            return i < end - first ? Inner.X(first + i) : rightX;
        }

        public double Y(int i)
        {
            if (leftEdge)
            {
                if (i == 0) return leftY;
                i--;
            }

            return i < end - first ? Inner.Y(first + i) : rightY;
        }
    }

    /// <summary>A graphic that draws nothing and takes the pointer: a plot's input area.</summary>
    public sealed class RaycastArea : Graphic
    {
        protected override void OnPopulateMesh(VertexHelper vh) => vh.Clear();
    }

    /// <summary>
    /// The pointer on a <see cref="ViewportModel"/>: over its rect, the wheel zooms about the pointer, a drag pans (the data
    /// value grabbed stays under the pointer), a double-click resets. It takes the drag, so a drag on the plot doesn't move the
    /// window. Keys are not routed: in flight every key a graph would use is a flight or camera key.
    /// With a <see cref="RangeModel"/> (<see cref="Ranges"/>): a right drag selects a range, or moves it or one of its ends when
    /// it starts on them; a left drag that starts on an end or a point moves it, and pans elsewhere; a right click marks a point
    /// or removes the one under it. While the right button is held on the plot, KSP's camera controls are locked: KSP orbits its
    /// camera on a right drag even over a window (its hover lock leaves the cameras alone). The lock and a right-button range drag
    /// end with the button's release, and also in the first frame the button is found up or the game loses focus: a release
    /// outside the game (Alt-Tab mid-drag) sends uGUI no pointer-up, and the lock would keep the camera and its keys from flight.
    /// </summary>
    public sealed class ViewportInput : MonoBehaviour, IScrollHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler,
        IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public ViewportModel View;

        /// <summary>The hover read-out the pointer drives (null: none).</summary>
        public GraphReadout Readout;

        /// <summary>The range and points the pointer picks (null: none, the right button does nothing).</summary>
        public RangeModel Ranges;

        RectTransform rt;
        double grabbed;
        string lockId;
        bool rightRange; // the range drag in progress is the right button's

        /// <summary>A drag is panning the view.</summary>
        public bool Dragging { get; private set; }

        /// <summary>A drag is moving or drawing the range, or moving a point.</summary>
        public bool DraggingRange { get; private set; }

        /// <summary>KSP's camera controls are locked (the right button is held on the plot).</summary>
        public bool CameraLocked { get; private set; }

        /// <summary>The last wheel delta seen (for tests: what one notch gives on this machine).</summary>
        public float LastWheel { get; private set; }

        public static ViewportInput Add(RectTransform plot, ViewportModel view)
        {
            var area = Graphs.Fill<RaycastArea>(plot, "ViewportInput");
            area.color = Color.clear;
            ViewportInput input = area.gameObject.AddComponent<ViewportInput>();
            input.View = view;
            input.rt = area.rectTransform;
            return input;
        }

        /// <summary>Where a screen point falls across the rect: 0 at its left edge, 1 at its right (not clamped).</summary>
        public double Fraction(Vector2 screen, Camera eventCam)
        {
            if (eventCam == null)
            {
                // events without a camera (e.g. sent by code) on a ScreenSpaceCamera canvas: use the canvas camera
                Canvas root = GetComponentInParent<Canvas>().rootCanvas;
                if (root.renderMode != RenderMode.ScreenSpaceOverlay) eventCam = root.worldCamera;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screen, eventCam, out Vector2 local);
            Rect r = rt.rect;
            return r.width > 0 ? (local.x - r.xMin) / r.width : 0.5;
        }

        public void OnScroll(PointerEventData e)
        {
            LastWheel = e.scrollDelta.y;
            View.Wheel(e.scrollDelta.y, Math.Max(0, Math.Min(1, Fraction(e.position, e.enterEventCamera))), KodeWindow.Pointer);
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Right && Ranges != null) LockCamera(true);
            Readout?.Press(true);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Right) LockCamera(false);
            Readout?.Press(false);
        }

        public void OnPointerEnter(PointerEventData e) => Readout?.Enter(e.position);

        public void OnPointerExit(PointerEventData e) => Readout?.Exit();

        public void OnBeginDrag(PointerEventData e)
        {
            // a drag left over from one that never ended must not steer this one
            DraggingRange = Dragging = rightRange = false;
            bool right = e.button == PointerEventData.InputButton.Right;
            if (Ranges != null && (right || e.button == PointerEventData.InputButton.Left) &&
                Ranges.BeginDrag(Fraction(e.pressPosition, e.pressEventCamera), right, KodeWindow.Pointer))
            {
                DraggingRange = true;
                rightRange = right;
                Watch();
                OnDrag(e);
                return;
            }

            if (e.button != PointerEventData.InputButton.Left) return;
            Dragging = true;
            grabbed = View.FromFraction(Fraction(e.pressPosition, e.pressEventCamera));
            OnDrag(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (DraggingRange)
            {
                Ranges.DragTo(Fraction(e.position, e.pressEventCamera), KodeWindow.Pointer);
                return;
            }

            if (!Dragging) return;
            View.PanTo(grabbed - Fraction(e.position, e.pressEventCamera) * View.Span, KodeWindow.Pointer);
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (DraggingRange) Ranges.EndDrag();
            DraggingRange = rightRange = false;
            Dragging = false;
            Watch();
        }

        // the right button found up with the lock held (released outside the game): let the camera go, end the right drag. A small
        // component of its own, enabled only while the right button holds the lock or a range drag: a graph runs nothing per frame
        // otherwise (this one's handlers need it enabled)
        sealed class RightRelease : MonoBehaviour
        {
            internal ViewportInput Owner;

            void Update()
            {
                if (Owner == null) enabled = false;
                else if (!Input.GetMouseButton(1)) Owner.ReleaseRight();
            }
        }

        RightRelease watch;

        void Watch()
        {
            bool on = CameraLocked || rightRange;
            if (watch == null)
            {
                if (!on) return;
                watch = gameObject.AddComponent<RightRelease>();
                watch.Owner = this;
            }
            if (watch.enabled != on) watch.enabled = on;
        }

        void OnApplicationFocus(bool focus)
        {
            if (!focus) ReleaseRight();
        }

        void ReleaseRight()
        {
            if (rightRange)
            {
                Ranges?.EndDrag();
                DraggingRange = rightRange = false;
            }

            LockCamera(false);
            Watch();
        }

        public void OnPointerClick(PointerEventData e)
        {
            // uGUI clicks after a drag that ended on the object it started on: that was a drag, not a click
            if (e.dragging) return;
            if (e.button == PointerEventData.InputButton.Left && e.clickCount == 2) View.Reset(KodeWindow.Pointer);
            else if (e.button == PointerEventData.InputButton.Right && Ranges != null)
                Ranges.Click(Fraction(e.position, e.pressEventCamera), KodeWindow.Pointer);
        }

        void LockCamera(bool on)
        {
            if (on == CameraLocked) return;
            lockId = lockId ?? "KodeKit.ViewportInput." + GetInstanceID();
            if (on) InputLockManager.SetControlLock(ControlTypes.CAMERACONTROLS, lockId);
            else InputLockManager.RemoveControlLock(lockId);
            CameraLocked = on;
            Watch();
        }

        void OnDisable()
        {
            Readout?.Exit();
            if (DraggingRange) Ranges?.EndDrag();
            DraggingRange = rightRange = false;
            Dragging = false;
            LockCamera(false);
            Watch();
        }
    }

    /// <summary>
    /// A <see cref="RangeModel"/> drawn on a plot: the range as a shaded band between two edge lines, each point as a line (the
    /// active one brighter and wider). Placed by anchors from the model's viewport, so a pan or zoom moves them with the data;
    /// what lies outside the window is hidden. <see cref="Sync"/> runs every frame and does nothing (no allocation) until the
    /// model, its viewport or the plot's width changed. It also keeps the model's pick distance at <see cref="PickPixels"/>.
    /// </summary>
    public sealed class RangeOverlay
    {
        public readonly RangeModel Model;
        public readonly RectTransform Plot;
        public readonly Image Fill;
        public readonly RectTransform StartLine, EndLine;
        readonly List<Image> pointLines = new List<Image>();

        /// <summary>How near an end or a point the pointer grabs it, in the plot's pixels.</summary>
        public float PickPixels = 6;

        public static Color FillColor = new Color(1f, 1f, 1f, 0.12f);
        public static Color EdgeColor = new Color(1f, 1f, 1f, 0.85f);
        public static Color PointColor = new Color(1f, 0.85f, 0.35f, 0.6f);
        public static Color ActivePointColor = new Color(1f, 0.85f, 0.35f, 1f);

        int shownVersion = -1, shownViewVersion = -1;
        float shownWidth = -1;

        public RangeOverlay(RectTransform plot, Transform below, RangeModel model)
        {
            Plot = plot;
            Model = model;
            // the band under the series, just above the input area; the lines over the series
            Fill = Graphs.Fill<Image>(plot, "RangeFill");
            Fill.color = FillColor;
            Fill.raycastTarget = false;
            Fill.transform.SetSiblingIndex(below.GetSiblingIndex() + 1);
            StartLine = Line("RangeStart", EdgeColor).rectTransform;
            EndLine = Line("RangeEnd", EdgeColor).rectTransform;
            Fill.gameObject.SetActive(false);
            StartLine.gameObject.SetActive(false);
            EndLine.gameObject.SetActive(false);
        }

        Image Line(string name, Color color)
        {
            var img = Graphs.Fill<Image>(Plot, name);
            img.color = color;
            img.raycastTarget = false;
            img.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            return img;
        }

        /// <summary>The line drawn for point <paramref name="i"/> (null past the lines made).</summary>
        public Image PointLine(int i) => i < pointLines.Count ? pointLines[i] : null;

        /// <summary>Places the graphics, when the model, the viewport or the plot's width changed.</summary>
        public void Sync()
        {
            float width = Plot.rect.width;
            ViewportModel view = Model.View;
            if (Model.Version == shownVersion && view.Version == shownViewVersion && width == shownWidth) return;
            shownVersion = Model.Version;
            shownViewVersion = view.Version;
            shownWidth = width;
            if (width > 0) Model.PickFraction = PickPixels / width;

            bool range = Model.HasRange;
            double f0 = range ? view.ToFraction(Model.Start) : 0, f1 = range ? view.ToFraction(Model.End) : 0;
            bool band = range && f1 >= 0 && f0 <= 1;
            SetActive(Fill.gameObject, band);
            if (band)
            {
                RectTransform r = Fill.rectTransform;
                r.anchorMin = new Vector2((float)Math.Max(0, f0), 0);
                r.anchorMax = new Vector2((float)Math.Min(1, f1), 1);
                r.offsetMin = r.offsetMax = Vector2.zero;
            }

            Place(StartLine, range, f0, 2);
            Place(EndLine, range, f1, 2);

            for (int i = 0; i < Model.PointCount || i < pointLines.Count; i++)
            {
                // a line is made the first time a point needs it (an allocation per new point, not per frame)
                if (i >= pointLines.Count) pointLines.Add(Line("Point" + i, PointColor));
                Image line = pointLines[i];
                bool shown = i < Model.PointCount, active = i == Model.ActivePoint;
                Color color = active ? ActivePointColor : PointColor;
                if (shown && line.color != color) line.color = color;
                Place(line.rectTransform, shown, shown ? view.ToFraction(Model.Point(i)) : 0, active ? 2 : 1);
            }
        }

        static void Place(RectTransform r, bool shown, double f, float thickness)
        {
            shown = shown && f >= 0 && f <= 1;
            SetActive(r.gameObject, shown);
            if (!shown) return;
            r.anchorMin = new Vector2((float)f, 0);
            r.anchorMax = new Vector2((float)f, 1);
            r.sizeDelta = new Vector2(thickness, 0);
            r.anchoredPosition = Vector2.zero;
        }

        static void SetActive(GameObject go, bool on)
        {
            if (go.activeSelf != on) go.SetActive(on);
        }
    }

    /// <summary>
    /// A polyline drawn dashed, as one uGUI mesh: Dash pixels drawn, Gap pixels left, the pattern running on across the points.
    /// Like LineSeriesGraphic it draws Source between XMin and XMax, the points scaled into its rect and Y clamped to it, and is
    /// rebuilt only on SetVerticesDirty. For short series (no decimation): a few hundred points.
    /// </summary>
    public class DashedLineGraphic : MaskableGraphic
    {
        public IGraphSource Source;
        public double XMin, XMax, YMin, YMax;
        public float Thickness = 1.5f;
        public float Dash = 6, Gap = 4;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (Source == null || XMax <= XMin || YMax <= YMin || Dash <= 0) return;
            Rect r = rectTransform.rect;
            double sx = r.width / (XMax - XMin), sy = r.height / (YMax - YMin);

            bool havePrev = false, on = true;
            float phase = 0; // how far into the current dash or gap
            Vector2 prev = default;
            for (int i = 0; i < Source.Count; i++)
            {
                double xd = Source.X(i);
                if (xd < XMin) continue;
                var p = new Vector2(r.xMin + (float)((xd - XMin) * sx),
                    Mathf.Clamp(r.yMin + (float)((Source.Y(i) - YMin) * sy), r.yMin, r.yMax));
                if (!havePrev)
                {
                    prev = p;
                    havePrev = true;
                    continue;
                }

                Vector2 d = p - prev;
                float len = d.magnitude;
                if (len > 0)
                {
                    Vector2 dir = d / len;
                    for (float t = 0; t < len;)
                    {
                        float step = Mathf.Min((on ? Dash : Gap) - phase, len - t);
                        if (on) Quad(vh, prev + dir * t, prev + dir * (t + step));
                        t += step;
                        phase += step;
                        if (phase >= (on ? Dash : Gap))
                        {
                            on = !on;
                            phase = 0;
                        }
                    }
                }

                prev = p;
                if (p.x > r.xMax) break; // the segment towards the first point outside, for a parent RectMask2D to clip
            }
        }

        void Quad(VertexHelper vh, Vector2 a, Vector2 b)
        {
            if (vh.currentVertCount > 64000) return; // 16-bit index limit of UI meshes
            Vector2 d = b - a;
            float len = d.magnitude;
            if (len < 1e-4f) return;
            Vector2 n = new Vector2(-d.y, d.x) / len * (Thickness * 0.5f);
            int i = vh.currentVertCount;
            vh.AddVert(a - n, color, Vector2.zero);
            vh.AddVert(a + n, color, Vector2.zero);
            vh.AddVert(b + n, color, Vector2.zero);
            vh.AddVert(b - n, color, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i, i + 2, i + 3);
        }
    }

    /// <summary>
    /// PlotArea (porkchop) in uGUI: a texture with hover pixel, click-to-select and drag-a-box-to-zoom.
    /// Pixel coordinates are texture pixels with y up, like PlotArea.HoveredPoint/SelectedPoint.
    /// </summary>
    public class PlotView
    {
        public readonly RawImage Image;
        readonly Image selection;
        readonly PickArea pick;
        Vector2? pressed;
        public int[] HoveredPoint, SelectedPoint;
        public Action<int, int, int, int> ZoomBox;   // x0, x1, y0, y1 in texture pixels
        public Action<int, int> Clicked;

        public PlotView(UIObject parent, Texture2D texture)
        {
            RectTransform surface = Graphs.Surface(parent, texture.width, texture.height, "Plot");
            Image = Graphs.Fill<RawImage>(surface, "Texture");
            Image.texture = texture;
            pick = Image.gameObject.AddComponent<PickArea>();
            selection = Graphs.Fill<Image>(surface, "Selection");
            selection.color = new Color(0, 0, 1, 0.3f);   // PlotArea's selection style
            selection.raycastTarget = false;
            selection.gameObject.SetActive(false);

            // hovering runs every frame: one buffer (readers compare its values)
            int[] hovered = new int[2];
            pick.Hover = n => HoveredPoint = Pixel(n, hovered);
            pick.Picked = n =>
            {
                pressed ??= n;
                ShowSelection(pressed.Value, n);
            };
            pick.Released = n =>
            {
                if (pressed == null) return;
                int[] a = Pixel(pressed.Value), b = Pixel(n);
                pressed = null;
                selection.gameObject.SetActive(false);
                if (Math.Abs(a[0] - b[0]) > 5 && Math.Abs(a[1] - b[1]) > 5)
                    ZoomBox?.Invoke(Math.Min(a[0], b[0]), Math.Max(a[0], b[0]), Math.Min(a[1], b[1]), Math.Max(a[1], b[1]));
                else
                {
                    SelectedPoint = b;
                    Clicked?.Invoke(b[0], b[1]);
                }
            };
        }

        int[] Pixel(Vector2 n, int[] into = null)
        {
            var t = Image.texture;
            into ??= new int[2];
            into[0] = Mathf.Clamp((int)(n.x * t.width), 0, t.width - 1);
            into[1] = Mathf.Clamp((int)(n.y * t.height), 0, t.height - 1);
            return into;
        }

        void ShowSelection(Vector2 a, Vector2 b)
        {
            var rt = selection.rectTransform;
            rt.anchorMin = Vector2.Min(a, b);
            rt.anchorMax = Vector2.Max(a, b);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            selection.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// ColorPickerHSV + ColorPickerRGB in one widget: saturation/value square, hue bar, RGBA bars and a swatch,
    /// bound to get/set of a Color (e.g. a custom window's background or text colour).
    /// </summary>
    public class ColorPicker
    {
        readonly Func<Color> get;
        // a colour, not a setting the window's owner saves: commits, but no Edited, as before the commit path
        readonly Headless.CommitTarget<Color> target;
        readonly Texture2D sv;
        readonly Color32[] svPixels;
        readonly RectTransform svMarker, hueMarker;
        readonly RectTransform[] rgbaMarkers = new RectTransform[4];
        readonly Texture2D[] rgbaTex = new Texture2D[4];
        readonly RawImage swatch;
        float h, s, v;
        Color shown = new Color(-1, -1, -1, -1);
        float svHue = -1;

        const int SV = 96;

        public ColorPicker(KodeWindow w, UIObject parent, Func<Color> get, Action<Color> set)
        {
            this.get = get;
            target = w.Target("ColorPicker", get, set, null, false);
            Layout top = KodeWindow.Row(parent);

            RectTransform svArea = Graphs.Surface(top, SV, SV, "SV");
            sv = new Texture2D(SV, SV, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            svPixels = new Color32[SV * SV];
            var svImg = Graphs.Fill<RawImage>(svArea, "SVTexture");
            svImg.texture = OwnedTextures.Own(svImg, sv);
            Grouped(svImg.gameObject.AddComponent<PickArea>()).Picked = n => { s = n.x; v = n.y; PushHsv(); };
            svMarker = Marker(svArea);

            RectTransform hueArea = Graphs.Surface(top, 16, SV, "Hue");
            var hueTex = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 64; y++) hueTex.SetPixel(0, y, Color.HSVToRGB(y / 63f, 1, 1));
            hueTex.Apply();
            var hueImg = Graphs.Fill<RawImage>(hueArea, "HueTexture");
            hueImg.texture = OwnedTextures.Own(hueImg, hueTex);
            Grouped(hueImg.gameObject.AddComponent<PickArea>()).Picked = n => { h = n.y; PushHsv(); };
            hueMarker = Marker(hueArea);

            RectTransform swatchArea = Graphs.Surface(top, 32, SV, "Swatch");
            swatch = Graphs.Fill<RawImage>(swatchArea, "Color");
            swatch.texture = Texture2D.whiteTexture;

            string[] names = { "R", "G", "B", "A" };
            for (int c = 0; c < 4; c++)
            {
                int ch = c;
                Layout row = KodeWindow.Row(parent);
                KodeWindow.Label(row, names[c]);
                RectTransform bar = Graphs.Surface(row, SV + 48, 12, "Bar" + names[c]);
                rgbaTex[c] = new Texture2D(32, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                var img = Graphs.Fill<RawImage>(bar, "Gradient");
                img.texture = OwnedTextures.Own(img, rgbaTex[c]);
                Grouped(img.gameObject.AddComponent<PickArea>()).Picked = n =>
                {
                    Color col = get();
                    col[ch] = n.x;
                    // Sync takes the new saturation and value, and the hue unless the colour is grey or black
                    Push(col);
                };
                rgbaMarkers[c] = Marker(bar);
            }

            w.AddSync(Sync);
            Sync();
        }

        static RectTransform Marker(RectTransform area)
        {
            var img = Graphs.Fill<Image>(area, "Marker");
            img.color = Color.white;
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.sizeDelta = Vector2.zero;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.sizeDelta = new Vector2(4, 4);
            return rt;
        }

        // a press and its drag are one commit, closed on the release, or when the picker is hidden in the middle of a drag (the
        // handle closes its group when disabled)
        PickArea Grouped(PickArea area)
        {
            if (target.Control == null) target.Control = area;
            area.gameObject.AddComponent<CommitGroupHandle>().Target = target;
            return area;
        }

        void Push(Color c)
        {
            target.Set(c, KodeWindow.Pointer);
            Sync();
        }

        // HSVToRGB's colour is opaque: keep the colour's alpha (only the A bar changes it)
        void PushHsv()
        {
            Color c = Color.HSVToRGB(h, s, v);
            c.a = get().a;
            Push(c);
        }

        void Sync()
        {
            Color c = get();
            if (c == shown) return;
            // keep hue when the colour is grey or black (HSV would lose it)
            Color.RGBToHSV(c, out float nh, out float ns, out float nv);
            if (ns > 0.001f && nv > 0.001f) h = nh;
            s = ns;
            v = nv;
            shown = c;

            if (!Mathf.Approximately(svHue, h))
            {
                svHue = h;
                for (int y = 0; y < SV; y++)
                for (int x = 0; x < SV; x++)
                    svPixels[y * SV + x] = Color.HSVToRGB(h, x / (SV - 1f), y / (SV - 1f));
                sv.SetPixels32(svPixels);
                sv.Apply();
            }
            for (int ch = 0; ch < 4; ch++)
            {
                for (int x = 0; x < 32; x++)
                {
                    Color g = c;
                    g[ch] = x / 31f;
                    if (ch < 3) g.a = 1;
                    rgbaTex[ch].SetPixel(x, 0, g);
                }
                rgbaTex[ch].Apply();
                Place(rgbaMarkers[ch], new Vector2(c[ch], 0.5f));
            }
            Place(svMarker, new Vector2(s, v));
            Place(hueMarker, new Vector2(0.5f, h));
            swatch.color = c;
        }

        static void Place(RectTransform marker, Vector2 n)
        {
            marker.anchorMin = marker.anchorMax = n;
            marker.anchoredPosition = Vector2.zero;
        }

        /// <summary>For tests: the picker's areas.</summary>
        public PickArea[] Areas(UIObject root) => root.GetComponentsInChildren<PickArea>();
    }
}
