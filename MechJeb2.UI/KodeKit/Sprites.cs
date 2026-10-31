using UnityEngine;

namespace KodeKit
{
    /// <summary>
    /// Small sprites drawn in code (no asset bundle): a checkbox frame and a tick, so toggles read clearly
    /// (KSP's stock toggle sprite is a small dot whose on and off states look almost the same).
    /// </summary>
    public static class Sprites
    {
        static Sprite box, boxHover, tick;

        /// <summary>A square frame: light border, dark fill; 9-sliced so it scales to any size.</summary>
        public static Sprite CheckBox => box ??= Box(new Color(0.72f, 0.72f, 0.72f, 1f), new Color(0.08f, 0.08f, 0.08f, 0.85f));

        /// <summary>CheckBox with a brighter border, for hover and press.</summary>
        public static Sprite CheckBoxHover => boxHover ??= Box(new Color(1f, 1f, 1f, 1f), new Color(0.16f, 0.16f, 0.16f, 0.9f));

        static Sprite outline;

        /// <summary>A white 2 px frame with a transparent inside (tint it); 9-sliced, like GUI.skin.box's border.</summary>
        public static Sprite Outline => outline ??= Box(Color.white, new Color(1, 1, 1, 0));

        /// <summary>A white tick (tint it), drawn anti-aliased on a transparent square.</summary>
        public static Sprite Tick => tick ??= MakeTick(32);

        static Sprite circle, caret;

        /// <summary>A white anti-aliased disc (slider handles).</summary>
        public static Sprite Circle => circle ??= MakeShape(32, p => (p - new Vector2(0.5f, 0.5f)).magnitude - 0.44f);

        /// <summary>A white downward caret (combo boxes): KSP's fonts have no ▾.</summary>
        public static Sprite Caret => caret ??= MakeShape(32, p =>
        {
            // inside the triangle (0.12,0.72) (0.88,0.72) (0.5,0.28): negative distance
            float top = p.y - 0.72f;
            float left = Vector2.Dot(p - new Vector2(0.12f, 0.72f), new Vector2(-0.756f, -0.654f));
            float right = Vector2.Dot(p - new Vector2(0.88f, 0.72f), new Vector2(0.756f, -0.654f));
            return Mathf.Max(top, Mathf.Max(left, right));
        });

        // ---- skins: rounded boxes ----

        static readonly System.Collections.Generic.Dictionary<string, Sprite> boxes = new System.Collections.Generic.Dictionary<string, Sprite>();

        /// <summary>
        /// A rounded rectangle with a border, anti-aliased, 9-sliced at its corner radius (pixels at 100 pixels per unit).
        /// The skins' window, button and field backgrounds. Cached per parameters.
        /// </summary>
        public static Sprite RoundedBox(int radius, int border, Color fill, Color edge)
        {
            string key = radius + "|" + border + "|" + fill + "|" + edge;
            if (boxes.TryGetValue(key, out Sprite s)) return s;
            int n = radius * 2 + 4;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[n * n];
            float half = n / 2f;
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                // signed distance to the rounded rectangle (negative inside), in pixels
                var q = new Vector2(Mathf.Abs(x + 0.5f - half), Mathf.Abs(y + 0.5f - half)) - new Vector2(half - 0.5f - radius, half - 0.5f - radius);
                float d = new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - radius;
                float outer = Mathf.Clamp01(0.5f - d);
                float inner = border <= 0 ? 1 : Mathf.Clamp01(0.5f - (d + border));
                Color c = Color.Lerp(edge, fill, inner);
                c.a *= outer;
                px[y * n + x] = c;
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            float b = radius + 1;
            return boxes[key] = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect,
                new Vector4(b, b, b, b));
        }

        static Sprite pill, pillOutline, rounded;

        /// <summary>A white pill (fully rounded ends), 9-sliced: switch tracks and chips (tint it).</summary>
        public static Sprite Pill => pill ??= RoundedBox(9, 0, Color.white, Color.white);

        /// <summary>A white pill outline with a transparent inside (tint it).</summary>
        public static Sprite PillOutline => pillOutline ??= RoundedBox(9, 1, new Color(1, 1, 1, 0), Color.white);

        /// <summary>A white box with 3 px rounded corners (tint it): status bars, flat icon buttons.</summary>
        public static Sprite Rounded => rounded ??= RoundedBox(3, 0, Color.white, Color.white);

        // ---- navball markers (white: tint them with the navball colours) ----

        public enum Marker { Prograde, Retrograde, Normal, AntiNormal, RadialOut, RadialIn }

        /// <summary>The navball colours of the markers: prograde yellow, normal magenta, radial cyan.</summary>
        public static Color MarkerColor(Marker m) =>
            m == Marker.Prograde || m == Marker.Retrograde ? new Color(0.91f, 0.77f, 0.23f)
            : m == Marker.Normal || m == Marker.AntiNormal ? new Color(0.82f, 0.36f, 0.84f)
            : new Color(0.25f, 0.77f, 0.88f);

        static readonly Sprite[] markers = new Sprite[6];

        /// <summary>KSP's navball marker shapes, drawn as strokes: SmartASS's direction buttons.</summary>
        public static Sprite NavballMarker(Marker m)
        {
            int i = (int)m;
            if (markers[i] != null) return markers[i];
            const float w = 0.09f; // stroke width
            System.Func<Vector2, float> d = m switch
            {
                Marker.Prograde => p => Min(Ring(p, 0.5f, 0.45f, 0.2f, w), Seg(p, 0.5f, 0.66f, 0.5f, 0.9f, w), Seg(p, 0.1f, 0.45f, 0.3f, 0.45f, w),
                    Seg(p, 0.7f, 0.45f, 0.9f, 0.45f, w)),
                Marker.Retrograde => p => Min(Ring(p, 0.5f, 0.5f, 0.22f, w), Seg(p, 0.38f, 0.38f, 0.62f, 0.62f, w), Seg(p, 0.38f, 0.62f, 0.62f, 0.38f, w),
                    Seg(p, 0.5f, 0.28f, 0.5f, 0.1f, w)),
                Marker.Normal => p => Min(Mathf.Abs(TriangleSd(p, new Vector2(0.15f, 0.2f), new Vector2(0.85f, 0.2f), new Vector2(0.5f, 0.82f))) - w / 2,
                    Dot(p, 0.5f, 0.42f, 0.06f)),
                Marker.AntiNormal => p => Min(Mathf.Abs(TriangleSd(p, new Vector2(0.5f, 0.18f), new Vector2(0.85f, 0.8f), new Vector2(0.15f, 0.8f))) - w / 2,
                    Seg(p, 0.5f, 0.45f, 0.5f, 0.65f, w)),
                Marker.RadialOut => p => Min(Ring(p, 0.5f, 0.5f, 0.22f, w), Dot(p, 0.5f, 0.5f, 0.06f), Seg(p, 0.5f, 0.72f, 0.5f, 0.92f, w),
                    Seg(p, 0.5f, 0.28f, 0.5f, 0.08f, w), Seg(p, 0.08f, 0.5f, 0.28f, 0.5f, w), Seg(p, 0.72f, 0.5f, 0.92f, 0.5f, w)),
                _ => p => Min(Ring(p, 0.5f, 0.5f, 0.3f, w), Seg(p, 0.3f, 0.3f, 0.42f, 0.42f, w), Seg(p, 0.7f, 0.3f, 0.58f, 0.42f, w),
                    Seg(p, 0.3f, 0.7f, 0.42f, 0.58f, w), Seg(p, 0.7f, 0.7f, 0.58f, 0.58f, w))
            };
            return markers[i] = MakeShape(48, d);
        }

        static Sprite edit, copy;

        static Sprite cross, chevronUp, chevronDown;

        /// <summary>An X (title-bar "close" button).</summary>
        public static Sprite Cross => cross ??= MakeShape(32, p => Mathf.Min(Seg(p, 0.22f, 0.22f, 0.78f, 0.78f, 0.13f), Seg(p, 0.22f, 0.78f, 0.78f, 0.22f, 0.13f)));

        /// <summary>A chevron pointing up (steppers, folded sections).</summary>
        public static Sprite ChevronUp => chevronUp ??= MakeShape(32, p => Mathf.Min(Seg(p, 0.2f, 0.36f, 0.5f, 0.66f, 0.13f), Seg(p, 0.5f, 0.66f, 0.8f, 0.36f, 0.13f)));

        /// <summary>A chevron pointing down.</summary>
        public static Sprite ChevronDown => chevronDown ??= MakeShape(32, p => Mathf.Min(Seg(p, 0.2f, 0.64f, 0.5f, 0.34f, 0.13f), Seg(p, 0.5f, 0.34f, 0.8f, 0.64f, 0.13f)));

        /// <summary>A pencil (title-bar "edit" button).</summary>
        public static Sprite Edit => edit ??= MakeShape(32, p => Min(Seg(p, 0.3f, 0.3f, 0.78f, 0.78f, 0.16f),
            Mathf.Abs(TriangleSd(p, new Vector2(0.12f, 0.12f), new Vector2(0.3f, 0.18f), new Vector2(0.18f, 0.3f))) - 0.05f));

        /// <summary>Two overlapping sheets (title-bar "copy" button).</summary>
        public static Sprite Copy => copy ??= MakeShape(32, p => Mathf.Min(Mathf.Abs(BoxSd(p, new Vector2(0.4f, 0.4f), new Vector2(0.24f, 0.26f))) - 0.045f,
            Mathf.Max(Mathf.Abs(BoxSd(p, new Vector2(0.6f, 0.6f), new Vector2(0.24f, 0.26f))) - 0.045f, -BoxSd(p, new Vector2(0.4f, 0.4f), new Vector2(0.29f, 0.31f)))));

        // stroke distances in unit coordinates (y up); negative inside the stroke
        static float Seg(Vector2 p, float ax, float ay, float bx, float by, float width) =>
            Segment(p, new Vector2(ax, ay), new Vector2(bx, by)) - width / 2;

        static float Ring(Vector2 p, float cx, float cy, float r, float width) => Mathf.Abs((p - new Vector2(cx, cy)).magnitude - r) - width / 2;

        static float Dot(Vector2 p, float cx, float cy, float r) => (p - new Vector2(cx, cy)).magnitude - r;

        static float BoxSd(Vector2 p, Vector2 centre, Vector2 half)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x - centre.x), Mathf.Abs(p.y - centre.y)) - half;
            return new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0);
        }

        // a triangle's signed distance, its vertices in either winding
        static float TriangleSd(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float area = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
            return area >= 0 ? Triangle(p, a, b, c) : Triangle(p, a, c, b);
        }

        static float Min(params float[] v)
        {
            float m = v[0];
            for (int i = 1; i < v.Length; i++) m = Mathf.Min(m, v[i]);
            return m;
        }

        public enum Direction { Up, Down, Left, Right }

        static readonly Sprite[] arrows = new Sprite[4];

        /// <summary>
        /// A white solid triangle pointing up, down, left or right (tint it): KSP's fonts have no ▲▼◄►, which IMGUI's
        /// MechJeb windows draw with Unity's built-in font.
        /// </summary>
        public static Sprite Arrow(Direction d)
        {
            int i = (int)d;
            if (arrows[i] != null) return arrows[i];
            // an upward triangle (y up), turned for the other directions
            Vector2 a = new Vector2(0.14f, 0.24f), b = new Vector2(0.86f, 0.24f), c = new Vector2(0.5f, 0.80f);
            System.Func<Vector2, Vector2> turn = d switch
            {
                Direction.Down => p => new Vector2(p.x, 1 - p.y),
                // the point is mapped back into the upward triangle's frame: apex at x = 0.2 for left, 0.8 for right
                Direction.Left => p => new Vector2(p.y, 1 - p.x),
                Direction.Right => p => new Vector2(p.y, p.x),
                _ => p => p
            };
            return arrows[i] = MakeShape(32, p => Triangle(turn(p), a, b, c));
        }

        // signed distance to a counter-clockwise triangle: the largest distance outside its three edges
        static float Triangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c) =>
            Mathf.Max(Edge(p, a, b), Mathf.Max(Edge(p, b, c), Edge(p, c, a)));

        static float Edge(Vector2 p, Vector2 from, Vector2 to)
        {
            Vector2 e = (to - from).normalized;
            return Vector2.Dot(p - from, new Vector2(e.y, -e.x)); // outward normal of a counter-clockwise edge
        }

        /// <summary>Rasterise a shape given by a signed distance (negative inside), anti-aliased over one pixel.</summary>
        static Sprite MakeShape(int n, System.Func<Vector2, float> distance)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = distance(new Vector2((x + 0.5f) / n, (y + 0.5f) / n));
                px[y * n + x] = new Color(1, 1, 1, Mathf.Clamp01(0.5f - d * n));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100);
        }

        static Sprite Box(Color border, Color fill)
        {
            const int n = 16;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
                px[y * n + x] = x < 2 || y < 2 || x >= n - 2 || y >= n - 2 ? border : fill;
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(3, 3, 3, 3));
        }

        static Sprite MakeTick(int n)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[n * n];
            // two strokes in unit coordinates (y up): short down-stroke then long up-stroke
            Vector2 a = new Vector2(0.10f, 0.52f), b = new Vector2(0.40f, 0.18f), c = new Vector2(0.92f, 0.86f);
            const float half = 0.085f; // stroke half-width
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                var p = new Vector2((x + 0.5f) / n, (y + 0.5f) / n);
                float d = Mathf.Min(Segment(p, a, b), Segment(p, b, c));
                float alpha = Mathf.Clamp01((half - d) * n + 0.5f);
                px[y * n + x] = new Color(1, 1, 1, alpha);
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100);
        }

        static float Segment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return (p - (a + t * ab)).magnitude;
        }
    }
}
