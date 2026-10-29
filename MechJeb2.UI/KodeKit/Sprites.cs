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
