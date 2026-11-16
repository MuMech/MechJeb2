using System;
using KodeKit;
using KodeUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MuMech.UI
{
    /// <summary>
    /// MechJebModuleRoverWindow ("Rover Autopilot") in KodeKit.
    /// The registry items it draws (toggles, editable fields, values) are rebuilt from the same InfoItems.
    /// </summary>
    [KodeViewFor(typeof(MechJebModuleRoverWindow))]
    public class RoverView : KodeView
    {
        readonly MechJebModuleRoverWindow window;

        public RoverView(MechJebModuleRoverWindow window) : base(window) => this.window = window;

        protected override float MinWidth => 200;

        MechJebCore Core => window.Core;
        MechJebModuleRoverController Ap => window.autopilot;

        static bool Alt => GameSettings.MODIFIER_KEY.GetKey();

        bool HasTarget() => Core.Target != null && Core.Target.Target != null;

        protected override void BuildContents(KodeWindow w)
        {
            UIObject r = w.Root;
            w.TitleButton("?", window.OpenHelp);
            MechJebModuleCustomWindowEditor ed = Core.GetComputerModule<MechJebModuleCustomWindowEditor>();

            KodeWindow.Header(r, "#MechJeb_NewUI_RoverHeading"); //Heading
            RoverUi.ToggleRow(w, r, ed, "Toggle:RoverController.ControlHeading", () => Ap != null && Ap.ControlHeading);
            // steppers (Shift: ×10)
            RoverUi.StepperRow(w, r, ed, "Editable:RoverController.heading", d => Ap.heading.Val += d);

            RoverUi.ValueRow(w, r, ed, "Value:RoverController.headingErr", () => Ap?.headingErr ?? 0, "-180.0 °");
            KodeWindow.Header(r, "#MechJeb_NewUI_RoverSpeed"); //Speed
            RoverUi.ToggleRow(w, r, ed, "Toggle:RoverController.ControlSpeed", () => Ap != null && Ap.ControlSpeed);
            RoverUi.StepperRow(w, r, ed, "Editable:RoverController.speed", d => Ap.speed.Val += d);

            RoverUi.ValueRow(w, r, ed, "Value:RoverController.speedErr", () => Ap?.speedErr ?? 0, "-999.9 m/s");

            // the speed the controller asks for now (the setting, slowed for turns and slopes)
            Layout tgt = KodeWindow.Row(r);
            KodeWindow.Label(tgt, "#MechJeb_NewUI_RoverCommandedSpeed", true); //Commanded speed
            UIText tgtValue = KodeWindow.LiveLabel(tgt, "000.0", false);
            tgtValue.tmpText.alignment = TextAlignmentOptions.Right;
            var tgtText = new CharText(tgtValue.tmpText);
            w.AddSync(() =>
            {
                tgtText.Clear().Append(Ap?.tgtSpeed ?? 0, 1);
                tgtText.Commit();
            });

            KodeWindow.Header(r, "#MechJeb_NewUI_RoverSafety"); //Safety
            RoverUi.ToggleRow(w, r, ed, "Toggle:RoverController.StabilityControl", () => Ap != null && Ap.StabilityControl);

            GameObject eject = RoverUi.ToggleRow(w, r, ed, "Toggle:RoverController.BrakeOnEject", () => Ap != null && Ap.BrakeOnEject);
            if (eject != null) w.Visible(eject, () => !Core.Settings.HideBrakeOnEject);

            RoverUi.ToggleRow(w, r, ed, "Toggle:RoverController.BrakeOnEnergyDepletion", () => Ap != null && Ap.BrakeOnEnergyDepletion);
            GameObject daylight = RoverUi.ToggleRow(w, r, ed, "Toggle:RoverController.WarpToDaylight", () => Ap != null && Ap.WarpToDaylight);
            if (daylight != null) w.Visible(daylight, () => Ap != null && Ap.BrakeOnEnergyDepletion);

            // "Waypoint  n of m", the target's buttons, then Drive / Stop and the waypoint list
            KodeWindow.Header(r, "#MechJeb_Rover_label2"); //Waypoints
            Layout index = KodeWindow.Row(r);
            KodeWindow.Label(index, "#MechJeb_NewUI_RoverWaypoint", true); //Waypoint
            UIText indexValue = KodeWindow.LiveLabel(index, "000 of 000", false);
            indexValue.tmpText.alignment = TextAlignmentOptions.Right;
            var indexText = new CharText(indexValue.tmpText);
            w.AddSync(() =>
            {
                indexText.Clear();
                if (Ap != null) indexText.AppendInt(Ap.WaypointIndex + 1).Append(" of ").AppendInt(Ap.Waypoints.Count);
                else indexText.Append("0 of 0");
                indexText.Commit();
            });

            Layout targets = KodeWindow.Row(r);
            KodeWindow.Button(targets, "#MechJeb_Rover_button1", () => window.ToTarget(Alt), true); // "To Target"
            KodeWindow.Button(targets, "#MechJeb_Rover_button2", window.AddTarget, true);          // "Add Target"
            w.Visible(targets.gameObject, HasTarget);

            Layout drive = KodeWindow.Row(r);
            UIButton go = KodeWindow.Button(drive, "#MechJeb_Rover_button3", () => window.Drive(Alt), true); // "Drive"
            w.Visible(go.gameObject, () => Ap != null && Ap.Waypoints.Count > 0 && (!Ap.ControlHeading || !Ap.ControlSpeed));
            UIButton stop = KodeWindow.Button(drive, "#MechJeb_Rover_button4", window.Stop, true); // "Stop"
            w.Visible(stop.gameObject, () => Ap != null && Ap.Waypoints.Count > 0 && Ap.ControlHeading && Ap.ControlSpeed);
            KodeWindow.Role(go, ButtonRole.Primary);
            KodeWindow.Role(stop, ButtonRole.Danger);
            KodeWindow.Button(drive, "#MechJeb_Rover_button5", window.ToggleWaypointWindow, true); // "Waypoints"
        }
    }

    /// <summary>Widget helpers shared by the rover and waypoint windows: registry items (InfoItems) and a scroll list.</summary>
    internal static class RoverUi
    {
        /// <summary>A button whose text changes (Alt held, state): the cell has a fixed width so the text never drives the layout.</summary>
        public static UIButton DynButton(KodeWindow w, UIObject parent, Func<string> text, Action click, float width, bool flexible = false)
        {
            string shown = text();
            UIButton b = KodeWindow.Button(parent, shown, click, flexible);
            if (width > 0) b.PreferredWidth(width);
            w.AddSync(() =>
            {
                string s = text();
                if ((object)s == shown || s == shown) return;
                shown = s;
                b.Text(s);
            });
            return b;
        }

        /// <summary>A button with left-aligned text whose colour follows a state (MechJeb's green "active" button style).</summary>
        public static UIButton StateButton(KodeWindow w, UIObject parent, string text, Action click, Func<bool> active, bool flexible = true)
        {
            UIButton b = KodeWindow.Button(parent, text, click, flexible);
            b.Content.ChildAlignment(TextAnchor.MiddleLeft);
            TMP_Text label = b.GetComponentInChildren<TMP_Text>();
            label.alignment = TextAlignmentOptions.Left;
            w.ToneText(label, () => active() ? KodeWindow.Tone.Good : KodeWindow.Tone.Normal, true);
            return b;
        }

        // ---- registry items (InfoItems) ----

        static InfoItem Find(MechJebModuleCustomWindowEditor ed, string id) => ed?.registry.Find(i => i.id == id);

        /// <summary>
        /// A ToggleInfoItem (its key as the label, so it follows a language switch); the
        /// value is read through <paramref name="get"/>, or the item's compiled getter. Returns its row.
        /// </summary>
        public static GameObject ToggleRow(KodeWindow w, UIObject parent, MechJebModuleCustomWindowEditor ed, string id, Func<bool> get = null)
        {
            if (!(Find(ed, id) is ToggleInfoItem item)) return null;
            UnityEngine.UI.Toggle t = w.Toggle(parent, item.name, get ?? (() => item.Value), v => item.Value = v);
            Tooltips.Add(KodeWindow.LabelOf(t).gameObject, item.localizedTooltip);
            return KodeWindow.RowOf(t);
        }

        /// <summary>An EditableInfoItem with nudges: label [−][field][+] unit; − and + add ∓1 (×10 with Shift).</summary>
        public static Layout StepperRow(KodeWindow w, UIObject parent, MechJebModuleCustomWindowEditor ed, string id, Action<double> add)
        {
            if (!(Find(ed, id) is EditableInfoItem item) || item.Editable == null) return null;
            IEditable e = item.Editable;
            var parsed = e as EditableDoubleMult;
            Layout row = KodeWindow.Row(parent);
            UIText label = KodeWindow.Label(row, item.name, true);
            Tooltips.Add(label.gameObject, item.localizedTooltip);
            w.Stepper(row, () => e.Text, s => e.Text = s, add, () => 1, Math.Max(item.width, 45),
                parsed != null ? (Func<bool>)(() => parsed.IsParsed) : null);
            if (!string.IsNullOrEmpty(item.rightLabel)) KodeWindow.Label(row, item.rightLabel);
            return row;
        }

        /// <summary>An EditableInfoItem: label, field, right label. Returns the row so buttons can be added.</summary>
        public static Layout EditableRow(KodeWindow w, UIObject parent, MechJebModuleCustomWindowEditor ed, string id)
        {
            if (!(Find(ed, id) is EditableInfoItem item) || item.Editable == null) return null;
            IEditable e = item.Editable;
            var parsed = e as EditableDoubleMult;
            Layout row = KodeWindow.Row(parent);
            UIText label = KodeWindow.Label(row, item.name, true);
            Tooltips.Add(label.gameObject, item.localizedTooltip);
            EditUi.Field(w, row, e, item.width);
            if (!string.IsNullOrEmpty(item.rightLabel)) KodeWindow.Label(row, item.rightLabel);
            return row;
        }

        /// <summary>A ValueInfoItem: label and the formatted value (garbage-free).</summary>
        public static void ValueRow(KodeWindow w, UIObject parent, MechJebModuleCustomWindowEditor ed, string id, Func<double> get, string template)
        {
            if (!(Find(ed, id) is ValueInfoItem item)) return;
            Layout row = KodeWindow.Row(parent);
            UIText label = KodeWindow.Label(row, item.name, true);
            UIText value = KodeWindow.LiveLabel(row, template, false);
            value.tmpText.alignment = TextAlignmentOptions.Right;
            Tooltips.Add(label.gameObject, item.localizedTooltip);

            var text = new CharText(value.tmpText);
            string units = item.Units ?? "";
            string format = item.Format ?? "";
            bool si = format == ValueInfoItem.SI;
            int decimals = format.Length >= 2 && format[0] == 'F' && int.TryParse(format.Substring(1), out int d) ? d : -1;
            int sigFigs = item.SiSigFigs, maxPrecision = item.SiMaxPrecision;
            double scale = item.Scale;
            w.AddSync(() =>
            {
                double v = scale * get();
                text.Clear();
                if (si) text.AppendSI(v, sigFigs, maxPrecision).Append(units);
                else if (decimals >= 0) text.Append(v, decimals).Append(' ').Append(units);
                else text.Append(v.ToString(format)).Append(' ').Append(units);
                text.Commit();
            });
        }
    }
}
