using System;
using System.Collections.Generic;
using KodeKit;
using KodeUI;
using TMPro;
using UnityEngine;

namespace MuMech.UI
{
    /// <summary>Formatting that MechJeb's strings do (MuUtils.PadPositive / PrettyPrint) into a CharText, without allocating.</summary>
    internal static class InfoText
    {
        /// <summary>MuUtils.PadPositive(v, "F" + decimals): a leading space unless the number starts with '-'.</summary>
        public static CharText PadPositive(this CharText ct, double v, int decimals)
        {
            // CharText prints '-' only when the rounded value is not zero (like .NET Framework's ToString("F"))
            bool negative = v < 0 && -v >= 0.5 * Math.Pow(10, -decimals);
            if (!negative) ct.Append(' ');
            return ct.Append(v, decimals);
        }

        /// <summary>MuUtils.PrettyPrint(Vector3d): "[ x, y, z ]" with padded components.</summary>
        public static CharText PrettyPrint(this CharText ct, Vector3d v, int decimals = 3)
        {
            ct.Append('[').PadPositive(v.x, decimals).Append(", ");
            ct.PadPositive(v.y, decimals).Append(", ");
            return ct.PadPositive(v.z, decimals).Append(" ]");
        }

        /// <summary>A static-looking text that wraps to the window width and is refilled when the string changes.</summary>
        public static UIText WrappedLive(KodeWindow w, UIObject parent, Func<string> text)
        {
            UIText t = GuidanceUi.Wrapped(parent, " ");
            string shown = null;
            w.AddSync(() =>
            {
                string s = text() ?? "";
                if (shown != null && s == shown) return;
                shown = s;
                t.tmpText.text = s.Length == 0 ? " " : s; // not through SetText: the string may start with '#'
            });
            return t;
        }

        /// <summary>A wrapping text filled every frame from a char buffer (long lists of numbers); the text only reaches TMP when it changes.</summary>
        public static CharText WrappedChars(UIObject parent, out UIText label)
        {
            label = GuidanceUi.Wrapped(parent, " ");
            return new CharText(label.tmpText);
        }

        /// <summary>A vertical group that does not stretch its children (a column of a table).</summary>
        public static Layout Column(UIObject parent, bool flexible)
        {
            Layout l = parent.Add<Layout>("Column");
            l.Vertical().ControlChildSize(true, true).ChildForceExpand(false, false).Spacing(0);
            if (flexible) l.FlexibleLayout(true, false);
            l.Finish();
            return l;
        }
    }

    /// <summary>
    /// A list of "name ... value" rows whose length changes at runtime (phase angles per body, pools). Rows come from a
    /// pool: created the first time they are needed, then only shown or hidden. Use Begin(), Add(name) per row writing the
    /// value into the returned buffer, then End().
    /// </summary>
    internal sealed class LabelValueRows
    {
        sealed class Row
        {
            public Layout Root;
            public UIText Name;
            public CharText Value;
            public string NameShown;
        }

        readonly UIObject container;
        readonly string template;
        readonly List<Row> rows = new List<Row>();
        int used;

        public LabelValueRows(UIObject container, string valueTemplate)
        {
            this.container = container;
            template = valueTemplate;
        }

        /// <summary>False while the container is hidden: rows are only created under an active parent (TMP needs its Awake).</summary>
        public bool CanBuild => container.gameObject.activeInHierarchy;

        public void Begin() => used = 0;

        public CharText Add(string name)
        {
            if (used == rows.Count) rows.Add(Create());
            Row r = rows[used++];
            if (!ReferenceEquals(name, r.NameShown))
            {
                r.NameShown = name;
                r.Name.tmpText.text = name;
            }

            return r.Value.Clear();
        }

        Row Create()
        {
            var r = new Row { Root = KodeWindow.Row(container) };
            r.Name = KodeWindow.Label(r.Root, " ", true);
            UIText value = KodeWindow.LiveLabel(r.Root, template, false);
            value.tmpText.alignment = TextAlignmentOptions.Right;
            r.Value = new CharText(value.tmpText);
            return r;
        }

        public void End()
        {
            for (int i = 0; i < rows.Count; i++)
            {
                bool on = i < used;
                GameObject go = rows[i].Root.gameObject;
                if (go.activeSelf != on) go.SetActive(on);
                if (on) rows[i].Value.Commit();
            }
        }
    }
}
