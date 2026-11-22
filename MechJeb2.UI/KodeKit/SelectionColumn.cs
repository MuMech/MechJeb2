using System;
using KodeKit.Headless;
using KodeUI;
using KSP.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace KodeKit
{
    /// <summary>
    /// A toggle column on a <see cref="VirtualList{TRow}"/> of <see cref="KodeWindow.ListRow"/>s, drawn from a
    /// <see cref="SelectionSet{TKey}"/>: a check box at the start of each row (rows the pool makes later get one too), bound to the
    /// row's index at each frame, so a scroll or a rebind shows the state of the row's key. A click on the box toggles the row; a
    /// click on the row's text does what it did before. No key or modifier is read.
    /// </summary>
    public static class SelectionColumn
    {
        /// <summary>
        /// Adds the column to <paramref name="list"/>'s rows; <paramref name="name"/> names each box (a '#key' or a text). With
        /// <paramref name="rowName"/>, each box is named after the row it is bound to instead, through <paramref name="rowFormat"/>
        /// (a '#key' whose <c>&lt;&lt;1&gt;&gt;</c> is the row's name, "Select Apoapsis"): the name an agent and the pattern checks
        /// read, so the boxes of a list don't share one. Nothing is drawn for it. The name is made again only when the box's row
        /// changes (a scroll, a rebind) or a language switch relabelled it, so a frame with no change makes no garbage.
        /// </summary>
        public static void Add<TKey>(KodeWindow w, VirtualList<UIButton> list, SelectionSet<TKey> selection, string name,
            Func<int, string> rowName = null, string rowFormat = null)
        {
            void Attach(GameObject go)
            {
                var row = go.GetComponent<UIButton>();
                if (row == null || BoxOf(row) != null) return;
                ToggleModel model = null;
                int namedIndex = -1, namedVersion = -1;
                string named = null;
                bool Get()
                {
                    int i = list.IndexOf(row);
                    // made again when the row's index or the list's data changed (a reorder, a remove, a rename keep the index
                    // but rebind), or a language switch relabelled the model; else two compares, no garbage
                    if (rowName != null && model != null && i >= 0 && i < list.Model.Count &&
                        (i != namedIndex || namedVersion != list.BindVersion || !ReferenceEquals(model.Label, named)))
                    {
                        string rn = rowName(i);
                        named = string.IsNullOrEmpty(rn) ? model.Label : Localizer.Format(rowFormat, rn);
                        model.Label = named;
                        namedIndex = i;
                        namedVersion = list.BindVersion;
                    }
                    return selection.IsSelected(i);
                }
                Toggle box = w.SelectBox(row.Content, Get, v => selection.Set(list.IndexOf(row), v, KodeWindow.Pointer), name);
                model = w.Commits.Find(box)?.Model as ToggleModel;
                GameObject boxRow = KodeWindow.RowOf(box);
                boxRow.name = BoxName;
                boxRow.transform.SetAsFirstSibling();
            }
            foreach (GameObject row in list.RowObjects) Attach(row);
            list.RowCreated += Attach;
        }

        const string BoxName = "SelectBox";

        /// <summary>The check box of a row of a list with a toggle column, or null.</summary>
        public static Toggle BoxOf(UIButton row)
        {
            Transform t = row != null ? row.Content.transform.Find(BoxName) : null;
            return t != null ? t.GetComponentInChildren<Toggle>() : null;
        }

        /// <summary>
        /// The list's "all" check box, with its label: on when every shown row is selected; a click selects every shown row, or none
        /// when all were.
        /// </summary>
        public static Toggle AllBox<TKey>(KodeWindow w, UIObject parent, SelectionSet<TKey> selection, string text) =>
            w.SelectBox(parent, () => selection.AllSelected, v => selection.SetAll(v, KodeWindow.Pointer), text, text);
    }
}
