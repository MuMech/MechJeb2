using System;
using KodeKit;
using KodeUI;
using UnityEngine;

namespace MuMech.UI
{
    /// <summary>What an operation builder gets: the window, the panel to fill, the operation and the planner it belongs to.</summary>
    internal sealed class OpContext
    {
        public KodeWindow W;
        public Layout Panel;
        public Operation Op;
        public MechJebModuleManeuverPlanner Planner;

        public MechJebModuleTargetController Target => Planner.Core.Target;

        /// <summary>The target is a celestial body (getters run every frame, so no target is not an error).</summary>
        public bool TargetIsCelestial => Target != null && Target.Target is CelestialBody;
    }

    /// <summary>
    /// The pieces the maneuver operations share, standing in for GuiUtils.SimpleTextBox / ToggledTextBox / TimeSelector.DoChooseTimeGUI
    /// and EditableAngle.DrawEditGUI. They bind to getters (not to the editable itself) because Persistent loading and some
    /// operations replace the editable objects.
    /// </summary>
    internal static class OpUi
    {
        /// <summary>A horizontal group that does not take the free width of its row.</summary>
        public static Layout Group(UIObject parent)
        {
            Layout g = parent.Add<Layout>("Group");
            g.Horizontal().ControlChildSize(true, true).ChildForceExpand(false, false).Spacing(4).ChildAlignment(TextAnchor.MiddleLeft);
            g.Finish();
            return g;
        }

        /// <summary>A numeric/time field bound to an editable (red while its text does not parse).</summary>
        public static void Field(KodeWindow w, UIObject parent, Func<EditableDoubleMult> ed, float width = 100, Action edited = null)
        {
            EditFields.Field(w, parent, () => ed().Text, s =>
            {
                ed().Text = s;
                edited?.Invoke();
            }, width, () => ed().IsParsed);
        }

        /// <summary>GuiUtils.SimpleTextBox: label [field] unit. <paramref name="stacked"/> puts a long label on its own line.</summary>
        public static void LabeledField(KodeWindow w, UIObject parent, string label, Func<EditableDoubleMult> ed, string unit = null, bool stacked = false,
            float width = 100, Action edited = null)
        {
            if (stacked) KodeWindow.Label(parent, label);
            Layout row = KodeWindow.Row(parent);
            if (!stacked) KodeWindow.Label(row, label);
            Field(w, row, ed, width, edited);
            if (!string.IsNullOrEmpty(unit)) KodeWindow.Label(row, unit);
        }

        /// <summary>GuiUtils.ToggledTextBox: [x] label [field] unit</summary>
        public static void ToggledField(KodeWindow w, UIObject parent, string label, Func<bool> get, Action<bool> set, Func<EditableDoubleMult> ed, string unit,
            float width = 100)
        {
            Layout row = KodeWindow.Row(parent);
            w.Toggle(row, label, get, set);
            Field(w, row, ed, width);
            if (!string.IsNullOrEmpty(unit)) KodeWindow.Label(row, unit);
        }

        static bool Is(TimeSelector ts, TimeReference r)
        {
            int i = ts._currentTimeRef;
            return i >= 0 && i < ts.TimeRefNames.Length && ts.TimeReference == r;
        }

        /// <summary>TimeSelector.DoChooseTimeGUI: "Schedule the burn" [reference v] [of] [field]</summary>
        public static void TimeSelectorRows(KodeWindow w, UIObject parent, TimeSelector ts)
        {
            KodeWindow.Label(parent, "#MechJeb_Maneu_STB"); //Schedule the burn
            Layout row = KodeWindow.Row(parent);
            new ComboBox(w, row, () => ts._currentTimeRef, i => ts._currentTimeRef = i, () => ts.TimeRefNames);

            Layout lead = Group(row);
            KodeWindow.Label(lead, "#MechJeb_of"); //"of"
            Field(w, lead, () => ts.LeadTime);
            w.Visible(lead.gameObject, () => Is(ts, TimeReference.X_FROM_NOW));

            Layout altitude = Group(row);
            KodeWindow.Label(altitude, "#MechJeb_of");
            Field(w, altitude, () => ts.CircularizeAltitude);
            KodeWindow.Label(altitude, "km");
            w.Visible(altitude.gameObject, () => Is(ts, TimeReference.ALTITUDE));
        }

        /// <summary>EditableAngle.DrawEditGUI: [deg]° [min]' [sec]" [N/S/E/W]</summary>
        static readonly string[] northSouth = { "N", "S" }, eastWest = { "E", "W" };

        public static void AngleRow(KodeWindow w, UIObject parent, Func<EditableAngle> angle, EditableAngle.Direction direction, bool ownRow = true)
        {
            UIObject row = ownRow ? (UIObject)KodeWindow.Row(parent) : parent;
            EditFields.Field(w, row, () => angle().Degrees.Text, s => angle().Degrees.Text = s, 30, () => angle().Degrees.IsParsed);
            KodeWindow.Label(row, "°");
            EditFields.Field(w, row, () => angle().Minutes.Text, s => angle().Minutes.Text = s, 30, () => angle().Minutes.IsParsed);
            KodeWindow.Label(row, "'");
            EditFields.Field(w, row, () => angle().Seconds.Text, s => angle().Seconds.Text = s, 30, () => angle().Seconds.IsParsed);
            KodeWindow.Label(row, "\"");

            if (KodeWindow.ModernLayout)
            {
                string[] hemispheres = direction == EditableAngle.Direction.NS ? northSouth : eastWest;
                w.Segmented(row, hemispheres, () => angle().Negative ? 1 : 0, i => angle().Negative = i == 1).FlexibleLayout(0f, 0f);
                return;
            }

            string Dir(EditableAngle a) => direction == EditableAngle.Direction.NS ? a.Negative ? "S" : "N" : a.Negative ? "W" : "E";
            UIButton button = KodeWindow.Button(row, Dir(angle()), () => angle().Negative = !angle().Negative);
            string shown = Dir(angle());
            w.AddSync(() =>
            {
                string d = Dir(angle()); // literals: no allocation
                if ((object)d == shown) return;
                shown = d;
                button.Text(d);
            });
        }

        /// <summary>A wrapping text that changes (warnings, descriptions); re-laid out only when the text changes.</summary>
        public static UIText DynamicParagraph(KodeWindow w, UIObject parent, Func<string> text, bool warn = false)
        {
            string shown = text();
            UIText p = KodeWindow.Paragraph(parent, string.IsNullOrEmpty(shown) ? " " : shown);
            if (warn)
            {
                p.tmpText.color = Theme.Current.Warn;
                w.Behaviour.AddRestyle(() => p.tmpText.color = Theme.Current.Warn);
            }

            w.AddSync(() =>
            {
                string s = text();
                if ((object)s == shown || s == shown) return;
                shown = s;
                p.Text(s);
            });
            return p;
        }
    }
}
