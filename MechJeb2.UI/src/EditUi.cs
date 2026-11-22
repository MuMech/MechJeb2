using System;
using KodeKit;
using KodeUI;
using TMPro;
using UnityEngine;

namespace MuMech.UI
{
    /// <summary>
    /// Text fields on MechJeb's editables, and the rows around them. Here rather than in KodeKit
    /// because IEditable is a MechJeb type. A field's text turns red while it does not parse (EditableDoubleMult only:
    /// the other editables have no such state). Every editable holds a number (or EditableIntList's numbers), so its field is a
    /// <see cref="FieldKind.Number"/> one, its text on the right.
    /// </summary>
    internal static class EditUi
    {
        public static Func<bool> Valid(IEditable ed) => ed is EditableDoubleMult m ? (Func<bool>)(() => m.IsParsed) : null;

        /// <summary>
        /// The check of a text typed for an editable, without writing it (an inline edit writes only when its edit ends); null, any
        /// text, for an editable that can't tell.
        /// </summary>
        public static Func<string, bool> Parses(IEditable ed)
        {
            switch (ed)
            {
                case EditableDoubleMult m: return m.Parses;
                case EditableInt i: return i.Parses;
                default: return null;
            }
        }

        /// <summary>The editable's own junk strip of a typed text, without writing it (null: the text as typed).</summary>
        public static Func<string, string> Normalise(IEditable ed)
        {
            switch (ed)
            {
                case EditableDoubleMult m: return m.Normalise;
                case EditableInt i: return i.Normalise;
                default: return null;
            }
        }

        /// <summary>
        /// label [read-out] unit in a property grid: the editable's text, edited in place when clicked (KodeKit's inline edit, on an
        /// editable only). <paramref name="color"/>: the read-out's colour (null: the theme's).
        /// </summary>
        public static Layout Inline(KodeWindow.PropertyGrid g, string label, IEditable ed, string unit, float width = 60, Func<Color?> color = null) =>
            g.InlineEdit(label, () => ed.Text, s => ed.Text = s, width, unit, Parses(ed), null, color, Normalise(ed), FieldKind.Number);

        /// <summary>
        /// A field on an editable that lives as long as the window. <paramref name="label"/> names it (a '#key' or a text; null: the label
        /// right before it in its row).
        /// </summary>
        public static TMP_InputField Field(KodeWindow w, UIObject parent, IEditable ed, float width = 100, Action edited = null, string label = null) =>
            EditFields.Field(w, parent, () => ed.Text, s =>
            {
                ed.Text = s;
                edited?.Invoke();
            }, width, Valid(ed), label: label);

        /// <summary>
        /// A field on an editable read through <paramref name="ed"/> on every access: its owner may replace it (an
        /// autopilot clamping a value) or be missing (the field then shows nothing and ignores typing).
        /// </summary>
        public static TMP_InputField Field<T>(KodeWindow w, UIObject parent, Func<T> ed, float width = 100, Action edited = null, string label = null)
            where T : class, IEditable =>
            EditFields.Field(w, parent, () => ed()?.Text ?? "", s =>
            {
                T e = ed();
                if (e == null) return;
                e.Text = s;
                edited?.Invoke();
            }, width, () => !(ed() is EditableDoubleMult m) || m.IsParsed, label: label);

        /// <summary>
        /// label [field] unit. <paramref name="stacked"/> puts a long label on its own line. <paramref name="history"/>: the field's
        /// input history key (its recent values with Up and Down; null: none).
        /// </summary>
        public static Layout Row<T>(KodeWindow w, UIObject parent, string label, Func<T> ed, string unit = null, float width = 100, bool stacked = false,
            Action edited = null, string history = null) where T : class, IEditable
        {
            if (stacked) KodeWindow.Label(parent, label);
            Layout row = KodeWindow.Row(parent);
            if (!stacked) KodeWindow.Label(row, label);
            Field(w, row, ed, width, edited, label).History(history);
            if (!string.IsNullOrEmpty(unit)) KodeWindow.Label(row, unit);
            return row;
        }

        /// <summary>label [field] unit in a property grid, the editable read through <paramref name="ed"/> on every access.</summary>
        public static Layout Grid<T>(KodeWindow.PropertyGrid g, string label, Func<T> ed, string unit, float width = 60) where T : class, IEditable =>
            g.Field(label, () => ed()?.Text ?? "", s =>
            {
                T e = ed();
                if (e != null) e.Text = s;
            }, width, unit, () => !(ed() is EditableDoubleMult m) || m.IsParsed, FieldKind.Number);
    }
}
