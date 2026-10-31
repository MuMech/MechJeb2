using System;
using System.Collections.Generic;
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
            return go.GetComponent<Scrollbar>();
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
    /// stage markers, Y scale labels, horizontal scrollbar + follow toggle. Call Refresh() when data changed (10 Hz).
    /// </summary>
    public class LineGraph
    {
        public readonly RectTransform Plot;
        public readonly List<LineSeriesGraphic> Series = new List<LineSeriesGraphic>();
        public readonly LineSeriesGraphic Stages;
        readonly List<UIText> yLabels = new List<UIText>();
        readonly CharText[] yChars;
        public readonly Scrollbar Scroll;
        public bool Follow = true;
        public double VisibleWidth = 100;   // data units across the plot
        public double DataMaxX;             // rightmost data x
        public double ViewMin;              // left edge of the view

        /// <summary>Give each plot its own nested Canvas.</summary>
        public static bool NestedCanvas;

        readonly RectTransform frame;

        /// <param name="scrollbarParent">Where the scrollbar row goes (null: under the plot, indented past the Y labels).
        /// The Flight Recorder passes its window, so the scrollbar spans it as in IMGUI.</param>
        public LineGraph(KodeWindow w, UIObject parent, float width, float height, int labelCount = 5, UIObject scrollbarParent = null)
        {
            Layout area = KodeWindow.Row(parent);
            // Y labels column, fixed width, absolute positions
            RectTransform labels = Graphs.Surface(area, 70, height, "YLabels");
            Plot = Graphs.Surface(area, width, height, "Plot");
            if (NestedCanvas)
            {
                // own canvas: redrawing the lines re-batches only the plot, not the whole appCanvas
                Plot.gameObject.AddComponent<Canvas>();
            }
            Plot.gameObject.AddComponent<RectMask2D>();
            Graphs.Background(Plot, new Color(0.15f, 0.25f, 0.45f), new Color(0.02f, 0.02f, 0.08f));
            // GUILayout.Box's border around the plot, kept above the series (AddSeries moves it back on top)
            var outline = Graphs.Fill<Image>(Plot, "Frame");
            outline.sprite = Sprites.Outline;
            outline.type = Image.Type.Sliced;
            outline.color = new Color(0.30f, 0.32f, 0.35f, 1f);
            outline.raycastTarget = false;
            frame = outline.rectTransform;

            Stages = Graphs.Fill<LineSeriesGraphic>(Plot, "Stages");
            Stages.VerticalMarkers = true;
            Stages.Thickness = 1;
            Stages.color = new Color(0.5f, 0.5f, 0.5f);
            Stages.raycastTarget = false;

            yChars = new CharText[labelCount];
            for (int i = 0; i < labelCount; i++)
            {
                // the labels column is a KodeUI UIEmpty without layout group: cells are placed by anchors
                UIText t = KodeWindow.LiveLabel(labels.GetComponent<UIObject>(), "-000.0 k", false);
                RectTransform cell = (RectTransform)t.transform.parent;
                cell.anchorMin = cell.anchorMax = new Vector2(1, i / (labelCount - 1f));
                cell.pivot = new Vector2(1, 0.5f);
                cell.anchoredPosition = new Vector2(-4, 0);
                cell.sizeDelta = new Vector2(66, 18);
                t.tmpText.alignment = TextAlignmentOptions.Right;
                yLabels.Add(t);
                yChars[i] = new CharText(t.tmpText);
            }

            Layout bottom = KodeWindow.Row(scrollbarParent ?? parent);
            if (scrollbarParent == null) KodeWindow.Space(bottom, 74);
            Scroll = Graphs.HorizontalScrollbar(bottom);
            Scroll.onValueChanged.AddListener(v =>
            {
                double range = Math.Max(VisibleWidth, DataMaxX) - VisibleWidth;
                ViewMin = v * range;
                Follow = v > 0.999f;
            });
            // the follow toggle has no text: keep it to its box, so the scrollbar gets all the spare width
            Toggle follow = w.Toggle(bottom, "", () => Follow, v => Follow = v);
            var followCell = follow.transform.parent.GetComponent<LayoutElement>() ?? follow.transform.parent.gameObject.AddComponent<LayoutElement>();
            followCell.flexibleWidth = 0;
            w.AddSync(Tick);
        }

        public LineSeriesGraphic AddSeries(IGraphSource source, Color color)
        {
            var g = Graphs.Fill<LineSeriesGraphic>(Plot, "Series" + Series.Count);
            g.Source = source;
            g.color = color;
            g.raycastTarget = false;
            Series.Add(g);
            frame.SetAsLastSibling();
            return g;
        }

        /// <summary>Update the view window and Y range, redraw the meshes and labels. No allocation in steady state.</summary>
        public void Refresh(double yMin, double yMax)
        {
            if (Follow) ViewMin = Math.Max(0, DataMaxX - VisibleWidth);
            double range = Math.Max(VisibleWidth, DataMaxX) - VisibleWidth;
            Scroll.SetValueWithoutNotify(range > 0 ? (float)(ViewMin / range) : 1);
            Scroll.size = (float)Math.Min(1, VisibleWidth / Math.Max(VisibleWidth, DataMaxX));

            foreach (LineSeriesGraphic s in Series) Apply(s, yMin, yMax);
            Apply(Stages, 0, 1);
            if (Stagger) pending = Series.Count + 1;

            for (int i = 0; i < yChars.Length; i++)
            {
                double v = yMin + (yMax - yMin) * i / (yChars.Length - 1);
                yChars[i].Clear().AppendSI(v, 3).Commit();
            }
        }

        void Apply(LineSeriesGraphic s, double yMin, double yMax)
        {
            s.XMin = ViewMin;
            s.XMax = ViewMin + VisibleWidth;
            s.YMin = yMin;
            s.YMax = yMax;
            if (!Stagger) s.SetVerticesDirty();
        }

        /// <summary>Rebuild one series per frame after a Refresh instead of all at once (spreads the mesh cost).</summary>
        public static bool Stagger = true;

        int pending;

        /// <summary>Called every frame (registered as a window sync).</summary>
        void Tick()
        {
            if (pending <= 0) return;
            pending--;
            (pending < Series.Count ? Series[pending] : Stages).SetVerticesDirty();
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
        readonly Action<Color> set;
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
            this.set = set;
            Layout top = KodeWindow.Row(parent);

            RectTransform svArea = Graphs.Surface(top, SV, SV, "SV");
            sv = new Texture2D(SV, SV, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            svPixels = new Color32[SV * SV];
            var svImg = Graphs.Fill<RawImage>(svArea, "SVTexture");
            svImg.texture = OwnedTextures.Own(svImg, sv);
            svImg.gameObject.AddComponent<PickArea>().Picked = n => { s = n.x; v = n.y; PushHsv(); };
            svMarker = Marker(svArea);

            RectTransform hueArea = Graphs.Surface(top, 16, SV, "Hue");
            var hueTex = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 64; y++) hueTex.SetPixel(0, y, Color.HSVToRGB(y / 63f, 1, 1));
            hueTex.Apply();
            var hueImg = Graphs.Fill<RawImage>(hueArea, "HueTexture");
            hueImg.texture = OwnedTextures.Own(hueImg, hueTex);
            hueImg.gameObject.AddComponent<PickArea>().Picked = n => { h = n.y; PushHsv(); };
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
                img.gameObject.AddComponent<PickArea>().Picked = n =>
                {
                    Color col = get();
                    col[ch] = n.x;
                    Color.RGBToHSV(col, out h, out s, out v);
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

        void Push(Color c)
        {
            set(c);
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
