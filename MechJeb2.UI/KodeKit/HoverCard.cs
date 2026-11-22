using System;
using System.Collections.Generic;
using System.Text;
using KodeUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KodeKit
{
    /// <summary>
    /// A rich tooltip's content (<see cref="Tooltips.Add(GameObject, Action{HoverCard}, object, Func{RectTransform, Headless.FrameBox})"/>):
    /// a small vertical panel the trigger's builder fills each time the card opens. A title, wrapped lines of text, and live values: a
    /// value row's text is written into a <see cref="CharText"/> when the card opens and then at <see cref="Tooltips.LiveInterval"/>
    /// (4 a second) while it is shown, so a shown card allocates nothing per frame. The card catches no pointer, takes the theme of
    /// the trigger's window, is drawn at that window's scale and is no wider than <see cref="Tooltips.CardMaxWidth"/> (its lines
    /// wrap). There is one card for all windows: its lines and value rows are kept between opens and reused in the order the builder
    /// asks for them, so opening a card builds no new text object once enough exist; parts a builder adds to <see cref="Root"/>
    /// itself are destroyed when the next card opens.
    /// </summary>
    public sealed class HoverCard
    {
        sealed class ValueRow
        {
            public Layout Row;
            public UIText Label, Value;
            public CharText Text;
            public Image Swatch; // made the first time a row has one
        }

        readonly LayoutPanel panel;
        readonly List<UIText> lines = new List<UIText>();
        readonly List<ValueRow> rows = new List<ValueRow>();
        readonly HashSet<Transform> pooled = new HashSet<Transform>();
        readonly List<Action<CharText>> fills = new List<Action<CharText>>();
        readonly List<CharText> values = new List<CharText>();
        readonly List<TMP_Text> texts = new List<TMP_Text>();
        readonly List<bool> dims = new List<bool>();
        int usedLines, usedRows, order, childrenAtBegin;
        bool threw;

        internal HoverCard(LayoutPanel panel) => this.panel = panel;

        /// <summary>A value row's colour swatch's side, in the card's units.</summary>
        public const float SwatchSize = 10;

        /// <summary>The card's panel: a builder may add its own parts here (they catch no pointer either).</summary>
        public UIObject Root => panel;

        /// <summary>The theme of the trigger's window.</summary>
        public Theme Theme { get; private set; }

        /// <summary>How many times the live values were written since the card opened (once at the open, then 4 a second).</summary>
        public int Updates { get; private set; }

        /// <summary>Whether the builder added nothing (the card is not shown).</summary>
        public bool IsEmpty => usedLines == 0 && usedRows == 0 && panel.transform.childCount == childrenAtBegin;

        /// <summary>Starts a new card in <paramref name="theme"/>: the last one's parts are put away, a builder's own parts destroyed.</summary>
        internal void Begin(Theme theme)
        {
            Theme = theme;
            Updates = 0;
            threw = false;
            usedLines = usedRows = order = 0;
            fills.Clear();
            values.Clear();
            texts.Clear();
            dims.Clear();
            Transform t = panel.transform;
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                Transform c = t.GetChild(i);
                if (c.gameObject.activeSelf) c.gameObject.SetActive(false);
                if (!pooled.Contains(c)) UnityEngine.Object.Destroy(c.gameObject); // at the frame's end: hidden until then
            }
            childrenAtBegin = t.childCount;
        }

        /// <summary>A title line (bold, one line). Text may be a '#key'.</summary>
        public UIText Title(string textOrKey)
        {
            UIText t = Line(KodeWindow.Loc(textOrKey), false, false);
            t.tmpText.fontStyle = FontStyles.Bold;
            return t;
        }

        /// <summary>A line of text that wraps at the card's width (a description). Text may be a '#key'; empty adds nothing.</summary>
        public UIText Text(string textOrKey, bool dim = false)
        {
            string s = KodeWindow.Loc(textOrKey);
            return string.IsNullOrEmpty(s) ? null : Line(s, true, dim);
        }

        UIText Line(string s, bool wrap, bool dim)
        {
            UIText t;
            if (usedLines < lines.Count) t = lines[usedLines];
            else
            {
                t = KodeWindow.Label(panel, "");
                lines.Add(t);
                pooled.Add(t.transform);
            }
            usedLines++;
            Use(t.gameObject);
            t.tmpText.fontStyle = FontStyles.Normal;
            t.tmpText.enableWordWrapping = wrap;
            t.tmpText.text = s;
            Track(t.tmpText, dim);
            return t;
        }

        /// <summary>
        /// A row "label  value": <paramref name="fill"/> writes the value into the cleared buffer when the card opens and at each
        /// live update. It must not allocate (no string building); a fixed text is <see cref="CharText.Append(string)"/>.
        /// <paramref name="swatch"/> puts a small square of that colour before the label (a graph's series).
        /// </summary>
        public CharText Value(string labelOrKey, Action<CharText> fill, Color? swatch = null)
        {
            ValueRow r;
            if (usedRows < rows.Count) r = rows[usedRows];
            else
            {
                r = new ValueRow { Row = panel.Add<Layout>("Value") };
                r.Row.Horizontal().ControlChildSize(true, true).ChildForceExpand(false, false).Spacing(8).ChildAlignment(TextAnchor.MiddleLeft);
                r.Row.Finish();
                r.Label = KodeWindow.Label(r.Row, "");
                r.Value = KodeWindow.Label(r.Row, "");
                r.Value.tmpText.enableWordWrapping = true;
                // the value takes the room the label leaves (it wraps), the label is never squeezed (its minimum is its width)
                LayoutElement v = r.Value.GetComponent<LayoutElement>() ?? r.Value.gameObject.AddComponent<LayoutElement>();
                v.flexibleWidth = 1;
                v.minWidth = 0;
                r.Text = new CharText(r.Value.tmpText);
                rows.Add(r);
                pooled.Add(r.Row.transform);
            }
            usedRows++;
            Use(r.Row.gameObject);
            if (swatch.HasValue && r.Swatch == null)
            {
                RectTransform cell = Graphs.Surface(r.Row, SwatchSize, SwatchSize, "Swatch");
                cell.SetAsFirstSibling();
                r.Swatch = Graphs.Fill<Image>(cell, "Colour");
                r.Swatch.raycastTarget = false;
            }
            if (r.Swatch != null)
            {
                GameObject cell = r.Swatch.transform.parent.gameObject;
                if (cell.activeSelf != swatch.HasValue) cell.SetActive(swatch.HasValue);
                if (swatch.HasValue) r.Swatch.color = swatch.Value;
            }
            string label = KodeWindow.Loc(labelOrKey);
            r.Label.tmpText.text = label;
            Track(r.Label.tmpText, true);
            LayoutElement le = r.Label.GetComponent<LayoutElement>() ?? r.Label.gameObject.AddComponent<LayoutElement>();
            le.minWidth = TextMeasure.Measure(label, Theme).x + 1;
            Track(r.Value.tmpText, false);
            values.Add(r.Text);
            fills.Add(fill);
            return r.Text;
        }

        // shown, in the order the builder asked for it
        void Use(GameObject go)
        {
            go.SetActive(true);
            go.transform.SetSiblingIndex(order++);
        }

        void Track(TMP_Text t, bool dim)
        {
            texts.Add(t);
            dims.Add(dim);
            Theme.ApplyText(t, dim ? Theme.Dim : (Color?)null);
        }

        /// <summary>Writes every live value (the card's host calls it at the open and at <see cref="Tooltips.LiveInterval"/>).</summary>
        internal void Tick()
        {
            Updates++;
            for (int i = 0; i < fills.Count; i++)
            {
                CharText ct = values[i].Clear();
                try
                {
                    fills[i](ct);
                }
                catch (Exception e)
                {
                    // a value that throws shows nothing; the card stays
                    ct.Clear();
                    if (!threw)
                    {
                        threw = true;
                        Debug.LogException(e);
                    }
                }
                ct.Commit();
            }
        }

        /// <summary>No part catches the pointer (a builder's own parts too).</summary>
        internal void Seal()
        {
            foreach (Graphic g in panel.GetComponentsInChildren<Graphic>(true))
                if (g.raycastTarget) g.raycastTarget = false;
        }

        /// <summary>For tests: the shown card's text, line by line, values as written (allocates).</summary>
        public string AllText
        {
            get
            {
                var sb = new StringBuilder();
                foreach (TMP_Text t in panel.GetComponentsInChildren<TMP_Text>(false))
                {
                    if (sb.Length > 0) sb.Append('\n');
                    // a value pushed with SetCharArray: its buffer, not text (TMP's text is the last string set)
                    int i = IndexOfValue(t);
                    sb.Append(i >= 0 ? values[i].ToString() : t.text);
                }
                return sb.ToString();
            }
        }

        int IndexOfValue(TMP_Text t)
        {
            for (int i = 0; i < values.Count; i++)
                if (values[i].Target == t) return i;
            return -1;
        }

        /// <summary>The live value rows' texts as last written (tests).</summary>
        public IReadOnlyList<CharText> Values => values;
    }
}
