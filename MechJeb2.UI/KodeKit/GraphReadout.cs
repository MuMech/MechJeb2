using System;
using System.Collections.Generic;
using KodeKit.Headless;
using UnityEngine;
using UnityEngine.UI;

namespace KodeKit
{
    /// <summary>
    /// A <see cref="LineGraph"/>'s hover read-out (<see cref="LineGraph.EnableReadout"/>): while the pointer is over the plot and
    /// no button is held there, a thin vertical cursor line at the pointer and, beside the pointer, the hover card (the tooltip's
    /// card, <see cref="Tooltips.FollowCard"/>) with the X under the pointer and each shown series' value there, a swatch of its
    /// colour before its name. The values are a <see cref="GraphReadoutModel"/>'s: read through the graph's viewport, so zoomed
    /// and panned too, interpolated as the line is drawn. The plot's <see cref="ViewportInput"/> reports the pointer entering,
    /// leaving and pressing; while it is over the plot a small component reads its position each frame (Unity 2019's uGUI sends
    /// no pointer move) and redraws only when the model's read-out changed or the pointer moved. A drag (pan, range, point) holds
    /// a button, so the read-out waits for its release. No allocation per frame or per move.
    /// </summary>
    public sealed class GraphReadout
    {
        public readonly LineGraph Graph;
        public readonly GraphReadoutModel Model;

        /// <summary>The cursor line, at the pointer's X while the read-out shows.</summary>
        public readonly Image Cursor;

        public static Color CursorColor = new Color(1f, 1f, 1f, 0.55f);

        /// <summary>Seconds between two reads at a still pointer (the data under it changes as it records).</summary>
        public static float RefreshInterval = 0.25f;

        readonly List<Color> colors = new List<Color>();
        readonly List<Action<CharText>> fills = new List<Action<CharText>>();
        readonly Action<CharText> fillX;
        readonly Action<HoverCard> build;
        readonly Tracker tracker;
        int builtShape = -1, drawnVersion = -1;
        Vector2 drawnPointer;

        /// <summary>The pointer's last screen position over the plot.</summary>
        public Vector2 Pointer { get; private set; }

        /// <summary>How many times the card was built and how many times its values were written (tests: a still pointer writes nothing).</summary>
        public int Builds { get; private set; }

        public int Writes { get; private set; }

        /// <summary>The read-out's card shows now.</summary>
        public bool CardShown => ReferenceEquals(Tooltips.FollowCardOwner, this);

        internal GraphReadout(LineGraph graph, GraphReadoutModel model)
        {
            Graph = graph;
            Model = model;
            Cursor = Graphs.Fill<Image>(graph.Plot, "ReadoutCursor");
            Cursor.color = CursorColor;
            Cursor.raycastTarget = false;
            RectTransform c = Cursor.rectTransform;
            c.pivot = new Vector2(0.5f, 0.5f);
            c.anchorMin = new Vector2(0, 0);
            c.anchorMax = new Vector2(0, 1);
            c.sizeDelta = new Vector2(1, 0);
            Cursor.gameObject.SetActive(false);
            fillX = ct => Model.WriteX(ct.Chars);
            build = Build;
            tracker = graph.Input.gameObject.AddComponent<Tracker>();
            tracker.Owner = this;
            tracker.enabled = false;
        }

        /// <summary>
        /// A series in the read-out: its samples, its name as shown (localized), how its value is written with its unit (null: SI
        /// at 4 figures), whether it is shown now (null: always) and its colour (the swatch). Call it when the graph is built.
        /// </summary>
        public int AddRow(ISamples samples, string name, Action<CharBuffer, double> format, Func<bool> shown, Color color)
        {
            int row = Model.Add(samples, name, format, shown);
            colors.Add(color);
            fills.Add(ct => Model.WriteValue(row, ct.Chars));
            return row;
        }

        // ---- the pointer, from ViewportInput and the tracker ----

        internal void Enter(Vector2 screen)
        {
            tracker.Watch();
            PointerAt(screen);
        }

        internal void Exit()
        {
            Model.Leave();
            if (tracker != null) tracker.enabled = false;
            // also on the plot's way out (its window closing): the cursor may be gone already
            if (CardShown) Tooltips.EndFollowCard(this);
            if (Cursor != null && Cursor.gameObject.activeSelf) Cursor.gameObject.SetActive(false);
            builtShape = -1;
        }

        internal void Press(bool down)
        {
            if (!Model.Hovering && down) return;
            Model.Press(down);
            Draw();
        }

        /// <summary>The pointer over the plot at <paramref name="screen"/> (what a pointer move does; tests call it with uGUI's
        /// enter event, as the real pointer gives no move event).</summary>
        public void PointerAt(Vector2 screen)
        {
            Pointer = screen;
            Model.Move(Graph.Input.Fraction(screen, null), KodeWindow.Pointer);
            Draw();
        }

        // a still pointer: read again when the view moved under it, and now and then for new data
        void Still()
        {
            bool changed = Model.Sync();
            if (!changed && Time.unscaledTime >= nextRefresh)
            {
                nextRefresh = Time.unscaledTime + RefreshInterval;
                Model.Refresh();
            }
            Draw();
        }

        float nextRefresh;

        void Draw()
        {
            if (!Model.Active)
            {
                if (Cursor.gameObject.activeSelf) Cursor.gameObject.SetActive(false);
                if (CardShown) Tooltips.EndFollowCard(this);
                builtShape = -1;
                return;
            }

            RectTransform c = Cursor.rectTransform;
            float f = (float)Model.Fraction;
            if (c.anchorMin.x != f)
            {
                c.anchorMin = new Vector2(f, 0);
                c.anchorMax = new Vector2(f, 1);
                c.anchoredPosition = Vector2.zero;
            }
            if (!Cursor.gameObject.activeSelf) Cursor.gameObject.SetActive(true);

            if (!CardShown || Model.ShapeVersion != builtShape)
            {
                builtShape = Model.ShapeVersion;
                drawnVersion = Model.Version;
                drawnPointer = Pointer;
                Builds++;
                Writes++;
                Tooltips.FollowCard(this, Graph.Plot, build, Pointer);
                return;
            }

            bool values = Model.Version != drawnVersion;
            if (!values && Pointer == drawnPointer) return;
            drawnVersion = Model.Version;
            drawnPointer = Pointer;
            if (values) Writes++;
            Tooltips.MoveFollowCard(this, Pointer, values);
        }

        void Build(HoverCard card)
        {
            card.Value(Model.XName, fillX);
            for (int i = 0; i < Model.Count; i++)
                if (Model.Shown(i)) card.Value(Model.Name(i), fills[i], colors[i]);
        }

        // reads the pointer each frame while it is over the plot (enabled only then)
        sealed class Tracker : MonoBehaviour
        {
            internal GraphReadout Owner;
            Vector2 lastMouse;

            internal void Watch()
            {
                // the real pointer's place now: only a move of it from here moves the read-out (a test's synthetic pointer stays)
                lastMouse = Input.mousePosition;
                enabled = true;
            }

            void Update()
            {
                if (Owner == null || !Owner.Model.Hovering)
                {
                    enabled = false;
                    return;
                }

                Vector2 mouse = Input.mousePosition;
                if (mouse != lastMouse)
                {
                    lastMouse = mouse;
                    Owner.PointerAt(mouse);
                }
                else Owner.Still();
            }
        }
    }
}
