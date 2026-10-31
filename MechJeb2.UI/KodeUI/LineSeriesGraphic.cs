using UnityEngine;
using UnityEngine.UI;

namespace KodeUI
{
    /// <summary>Data for one graph line, read without allocation.</summary>
    public interface IGraphSource
    {
        int Count { get; }
        double X(int i);
        double Y(int i);
    }

    /// <summary>
    /// A polyline drawn as one uGUI mesh (a quad per segment). Segments shorter than a pixel are skipped. Rebuilt only on
    /// SetVerticesDirty.
    /// </summary>
    public class LineSeriesGraphic : MaskableGraphic
    {
        public IGraphSource Source;
        public double XMin, XMax, YMin, YMax;
        public float Thickness = 2;
        /// <summary>When true, each point is a full-height vertical line at X (stage markers).</summary>
        public bool VerticalMarkers;
        /// <summary>
        /// Draw the polyline past the graph's edges, up to the first point beyond XMax, for a parent RectMask2D to clip.
        /// Default: points are clamped into the rect.
        /// </summary>
        public bool Unclamped;

        public int LastSegments { get; private set; }

        /// <summary>Total time spent building line meshes (for benchmarks).</summary>
        public static readonly System.Diagnostics.Stopwatch PopulateTimer = new System.Diagnostics.Stopwatch();

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            PopulateTimer.Start();
            try { Populate(vh); }
            finally { PopulateTimer.Stop(); }
        }

        void Populate(VertexHelper vh)
        {
            vh.Clear();
            LastSegments = 0;
            if (Source == null || XMax <= XMin || YMax <= YMin) return;
            Rect r = rectTransform.rect;
            double sx = r.width / (XMax - XMin), sy = r.height / (YMax - YMin);
            int n = Source.Count;

            if (VerticalMarkers)
            {
                for (int i = 0; i < n; i++)
                {
                    float x = r.xMin + (float)((Source.X(i) - XMin) * sx);
                    if (x < r.xMin || x > r.xMax) continue;
                    Quad(vh, new Vector2(x, r.yMin), new Vector2(x, r.yMax), Thickness);
                }
                return;
            }

            if (Decimate && !Unclamped)
            {
                PopulateDecimated(vh, r, sx, sy, n);
                return;
            }

            bool havePrev = false;
            Vector2 prev = default;
            for (int i = 0; i < n; i++)
            {
                double xd = Source.X(i);
                if (xd < XMin) continue;
                var p = new Vector2(r.xMin + (float)((xd - XMin) * sx), r.yMin + (float)((Source.Y(i) - YMin) * sy));
                if (p.x > r.xMax)
                {
                    // unclamped: the segment towards the first point outside, cut by the mask
                    if (Unclamped && havePrev) Quad(vh, prev, p, Thickness);
                    break;
                }
                if (!Unclamped) p.y = Mathf.Clamp(p.y, r.yMin, r.yMax);
                if (!havePrev) { prev = p; havePrev = true; continue; }
                if ((p - prev).sqrMagnitude < 1f) continue; // sub-pixel: skip
                Quad(vh, prev, p, Thickness);
                prev = p;
            }
        }

        /// <summary>
        /// Min/max per pixel column: per column one vertical stroke (min..max) and one connector from the previous
        /// column. At most ~2 quads per pixel of width whatever the point count; looks the same as the full polyline.
        /// </summary>
        public static bool Decimate = true;

        void PopulateDecimated(VertexHelper vh, Rect r, double sx, double sy, int n)
        {
            int col = int.MinValue;
            float first = 0, last = 0, min = 0, max = 0, prevLast = 0, prevX = 0;
            bool havePrev = false;
            for (int i = 0; i < n; i++)
            {
                double xd = Source.X(i);
                if (xd < XMin) continue;
                float x = r.xMin + (float)((xd - XMin) * sx);
                if (x > r.xMax) break;
                float y = Mathf.Clamp(r.yMin + (float)((Source.Y(i) - YMin) * sy), r.yMin, r.yMax);
                int c = Mathf.FloorToInt(x); // local x can be negative (centred pivot): floor, not truncate
                if (c != col)
                {
                    if (col != int.MinValue) EmitColumn(vh, col, first, last, min, max, ref havePrev, ref prevX, ref prevLast);
                    col = c;
                    first = last = min = max = y;
                }
                else
                {
                    last = y;
                    if (y < min) min = y;
                    if (y > max) max = y;
                }
            }
            if (col != int.MinValue) EmitColumn(vh, col, first, last, min, max, ref havePrev, ref prevX, ref prevLast);
        }

        void EmitColumn(VertexHelper vh, int col, float first, float last, float min, float max, ref bool havePrev, ref float prevX, ref float prevLast)
        {
            float x = col + 0.5f;
            if (havePrev) Quad(vh, new Vector2(prevX, prevLast), new Vector2(x, first), Thickness);
            if (max - min >= 1f) Quad(vh, new Vector2(x, min), new Vector2(x, max), Thickness);
            havePrev = true;
            prevX = x;
            prevLast = last;
        }

        void Quad(VertexHelper vh, Vector2 a, Vector2 b, float w)
        {
            if (vh.currentVertCount > 64000) return; // 16-bit index limit of UI meshes
            Vector2 d = b - a;
            float len = d.magnitude;
            if (len < 1e-4f) return;
            Vector2 n = new Vector2(-d.y, d.x) / len * (w * 0.5f);
            int i = vh.currentVertCount;
            vh.AddVert(a - n, color, Vector2.zero);
            vh.AddVert(a + n, color, Vector2.zero);
            vh.AddVert(b + n, color, Vector2.zero);
            vh.AddVert(b - n, color, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i, i + 2, i + 3);
            LastSegments++;
        }
    }
}
