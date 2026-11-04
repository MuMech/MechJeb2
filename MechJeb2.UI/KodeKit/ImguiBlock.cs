using System;
using System.Collections.Generic;
using KodeUI;
using KSP.UI;
using UnityEngine;
using UnityEngine.UI;

namespace KodeKit
{
    /// <summary>
    /// Migration escape hatch: hosts an unported GUILayout method (e.g. a MechJeb GeneralInfoItem) inside a
    /// KodeUI window. A placeholder cell takes part in the uGUI layout; one OnGUI draws every block with
    /// GUILayout.BeginArea over its placeholder and feeds the measured height back to the layout.
    /// Limits: IMGUI draws over all uGUI (an overlapping window does not hide it), and it keeps IMGUI's per-frame cost.
    /// Remove ImguiBlock once we know that no mod publishes info items: they are the main reason it stays. Its other users
    /// (maneuver operations from other mods, the autostage settings, the waypoint map marker) would need a port first.
    /// </summary>
    public class ImguiBlock
    {
        readonly Action draw;
        readonly RectTransform placeholder;
        readonly LayoutElement size;
        readonly bool layout;
        float measured = -1;
        static readonly Vector3[] corners = new Vector3[4];

        // a draw that threw is skipped for a second, then tried again; it is logged once
        const float RetryDelay = 1;
        bool logged, skipping;
        float retryAt = -1;

        public float Height => size.preferredHeight;
        public bool Visible => placeholder != null && placeholder.gameObject.activeInHierarchy;

        public ImguiBlock(UIObject parent, Action draw) : this(parent, draw, true) { }

        /// <summary>
        /// A block that only needs IMGUI's OnGUI (a GL draw on Repaint, e.g. a map marker): no GUILayout area, takes no room,
        /// runs while its placeholder is visible.
        /// </summary>
        public static ImguiBlock DrawOnly(UIObject parent, Action draw) => new ImguiBlock(parent, draw, false);

        ImguiBlock(UIObject parent, Action draw, bool layout)
        {
            this.draw = draw;
            this.layout = layout;
            UIEmpty cell = parent.Add<UIEmpty>("ImguiBlock");
            cell.Finish();
            placeholder = cell.rectTransform;
            size = cell.gameObject.AddComponent<LayoutElement>();
            size.flexibleWidth = 1;
            size.preferredHeight = layout ? 22 : 0;
            ImguiBlockHost.Register(this);
        }

        internal bool Alive => placeholder != null;

        /// <summary>Called from ImguiBlockHost.OnGUI.</summary>
        internal void OnGUI(Camera cam)
        {
            if (!placeholder.gameObject.activeInHierarchy) return;
            // decided on Layout and kept for the frame's other events, which must see the same controls
            if (Event.current.type == EventType.Layout) skipping = Time.unscaledTime < retryAt;
            if (skipping) return;
            if (!layout)
            {
                try { draw(); }
                catch (Exception e) when (!(e is ExitGUIException)) { Failed(e); }
                return;
            }

            Vector3[] c = corners;
            placeholder.GetWorldCorners(c);
            Vector2 bl = RectTransformUtility.WorldToScreenPoint(cam, c[0]);
            Vector2 tr = RectTransformUtility.WorldToScreenPoint(cam, c[2]);
            // uGUI scale (KSP UI scale x MechJeb scale) -> GUI.matrix, so text matches the window size
            float scale = Mathf.Max(0.01f, (tr.x - bl.x) / Mathf.Max(1, placeholder.rect.width));
            Matrix4x4 old = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));

            var area = new Rect(bl.x / scale, (Screen.height - tr.y) / scale, placeholder.rect.width, 10000);
            GUILayout.BeginArea(area);
            try
            {
                GUILayout.BeginVertical();
                bool drawn = false;
                try
                {
                    draw();
                    drawn = true;
                }
                catch (Exception e) when (!(e is ExitGUIException)) { Failed(e); }
                finally { GUILayout.EndVertical(); }
                if (drawn && Event.current.type == EventType.Repaint)
                {
                    float h = GUILayoutUtility.GetLastRect().height;
                    if (Mathf.Abs(h - measured) > 0.5f)
                    {
                        measured = h;
                        size.preferredHeight = h; // relayout only when the IMGUI content changes height
                    }
                }
            }
            finally
            {
                // closed even when the draw throws, so MechJeb's other OnGUI does not inherit an open area
                GUILayout.EndArea();
                GUI.matrix = old;
            }
        }

        void Failed(Exception e)
        {
            retryAt = Time.unscaledTime + RetryDelay;
            if (logged) return;
            logged = true;
            Debug.LogError($"[KodeKit] an IMGUI block could not be drawn (skipped for {RetryDelay} s, then retried; logged once): {e}");
        }
    }

    public class ImguiBlockHost : MonoBehaviour
    {
        static ImguiBlockHost instance;
        readonly List<ImguiBlock> blocks = new List<ImguiBlock>();

        /// <summary>Optional skin (MechJeb's GuiUtils.Skin); null keeps the current one.</summary>
        public static GUISkin Skin;

        public static void Register(ImguiBlock b)
        {
            if (instance == null)
            {
                var go = new GameObject("KodeKit.ImguiBlockHost");
                instance = go.AddComponent<ImguiBlockHost>();
            }
            instance.blocks.Add(b);
        }

        // this host dies with its scene: the static field would keep its blocks, and the views they draw, in memory until the next block
        void OnDestroy() => instance = null;

        void OnGUI()
        {
            blocks.RemoveAll(b => !b.Alive);
            if (blocks.Count == 0) return;
            if (Skin != null) GUI.skin = Skin;
            Camera cam = Overlay.Root.worldCamera;
            for (int i = 0; i < blocks.Count; i++)
                blocks[i].OnGUI(cam);
        }
    }
}
