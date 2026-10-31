using System;
using KodeKit;
using KodeUI;
using TMPro;

namespace MuMech.UI
{
    /// <summary>
    /// Text fields on MechJeb's editables (GuiUtils.SimpleTextField and the rows around it). Here rather than in KodeKit
    /// because IEditable is a MechJeb type. A field's text turns red while it does not parse (EditableDoubleMult only:
    /// the other editables have no such state).
    /// </summary>
    internal static class EditUi
    {
        public static Func<bool> Valid(IEditable ed) => ed is EditableDoubleMult m ? (Func<bool>)(() => m.IsParsed) : null;

        /// <summary>A field on an editable that lives as long as the window.</summary>
        public static TMP_InputField Field(KodeWindow w, UIObject parent, IEditable ed, float width = 100, Action edited = null) =>
            EditFields.Field(w, parent, () => ed.Text, s =>
            {
                ed.Text = s;
                edited?.Invoke();
            }, width, Valid(ed));

        /// <summary>
        /// A field on an editable read through <paramref name="ed"/> on every access: its owner may replace it (an
        /// autopilot clamping a value) or be missing (the field then shows nothing and ignores typing).
        /// </summary>
        public static TMP_InputField Field<T>(KodeWindow w, UIObject parent, Func<T> ed, float width = 100, Action edited = null) where T : class, IEditable =>
            EditFields.Field(w, parent, () => ed()?.Text ?? "", s =>
            {
                T e = ed();
                if (e == null) return;
                e.Text = s;
                edited?.Invoke();
            }, width, () => !(ed() is EditableDoubleMult m) || m.IsParsed);

        /// <summary>GuiUtils.SimpleTextBox: label [field] unit. <paramref name="stacked"/> puts a long label on its own line.</summary>
        public static Layout Row<T>(KodeWindow w, UIObject parent, string label, Func<T> ed, string unit = null, float width = 100, bool stacked = false,
            Action edited = null) where T : class, IEditable
        {
            if (stacked) KodeWindow.Label(parent, label);
            Layout row = KodeWindow.Row(parent);
            if (!stacked) KodeWindow.Label(row, label);
            Field(w, row, ed, width, edited);
            if (!string.IsNullOrEmpty(unit)) KodeWindow.Label(row, unit);
            return row;
        }

        /// <summary>label [field] unit in a property grid, the editable read through <paramref name="ed"/> on every access.</summary>
        public static Layout Grid<T>(KodeWindow.PropertyGrid g, string label, Func<T> ed, string unit, float width = 60) where T : class, IEditable =>
            g.Field(label, () => ed()?.Text ?? "", s =>
            {
                T e = ed();
                if (e != null) e.Text = s;
            }, width, unit, () => !(ed() is EditableDoubleMult m) || m.IsParsed);
    }
}
