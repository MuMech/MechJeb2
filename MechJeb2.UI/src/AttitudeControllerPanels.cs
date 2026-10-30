using System;
using System.Collections.Generic;
using MuMech.AttitudeControllers;
using KodeKit;
using KodeUI;
using UnityEngine;
using UnityEngine.UI;

namespace MuMech.UI
{
    /// <summary>
    /// The tuning panels of the attitude controllers, row for row with each controller's GUI().
    /// One builder per controller type; each builds its panel once (the view shows the one of the active controller).
    /// Every getter finds the live controller through the attitude module (so it keeps working if the controller
    /// objects are replaced) and does nothing when that controller is not the active one.
    /// </summary>
    internal static class AttitudeControllerPanels
    {
        internal static readonly Dictionary<Type, Action<KodeWindow, UIObject, MechJebModuleAttitudeController>> Builders =
            new Dictionary<Type, Action<KodeWindow, UIObject, MechJebModuleAttitudeController>>
            {
                { typeof(MJAttitudeController), (w, p, att) => BuildMJ(new Panel<MJAttitudeController>(w, p, att)) },
                { typeof(KosAttitudeController), (w, p, att) => BuildKos(new Panel<KosAttitudeController>(w, p, att)) },
                { typeof(HybridController), (w, p, att) => BuildHybrid(new Panel<HybridController>(w, p, att)) },
                { typeof(BetterController), (w, p, att) => BuildBetter(new Panel<BetterController>(w, p, att)) },
                { typeof(LQRController), (w, p, att) => BuildLQR(new Panel<LQRController>(w, p, att)) },
            };

        // ---- number formatting without allocation (also used by the window's own readouts) ----

        // MuUtils.PadPositive(x, "F3") without allocating: a space in front of non-negative numbers
        internal static void Padded(CharText t, double v, int decimals)
        {
            bool negative = double.IsNegativeInfinity(v) ||
                v < 0 && v > -1e15 && Math.Round(-(decimal)v, decimals, MidpointRounding.AwayFromZero) > 0;
            if (!negative) t.Append(' ');
            t.Append(v, decimals);
        }

        // MuUtils.PadPositiveSci(x, "F3"): above 1e6 "G3" (rare, allocates)
        internal static void PaddedSci(CharText t, double v)
        {
            if (v > 1000000) t.Append(' ').Append(v.ToString("G3"));
            else Padded(t, v, 3);
        }

        // MuUtils.PrettyPrint(vector): "[ x, y, z ]" with F3
        internal static void Plain(CharText t, Vector3d v)
        {
            t.Append('[');
            Padded(t, v.x, 3);
            t.Append(", ");
            Padded(t, v.y, 3);
            t.Append(", ");
            Padded(t, v.z, 3);
            t.Append(" ]");
        }

        // MuUtils.PrettyPrintSci(vector)
        internal static void PlainSci(CharText t, Vector3d v)
        {
            t.Append('[');
            PaddedSci(t, v.x);
            t.Append(", ");
            PaddedSci(t, v.y);
            t.Append(", ");
            PaddedSci(t, v.z);
            t.Append(" ]");
        }

        // "|" + magnitude F3 + "| " + MuUtils.PrettyPrint(vector)
        internal static void WithMagnitude(CharText t, Vector3d v)
        {
            t.Append('|').Append(v.magnitude, 3).Append("| ");
            Plain(t, v);
        }

        internal const string VectorTemplate = "|9999.999| [-9999.999, -9999.999, -9999.999 ]";

        /// <summary>
        /// The modern layout's vector read-outs: label | X | Y | Z (| |v|) in one table for consecutive rows, instead of
        /// "[ x, y, z ]" strings, so the axes line up and differences between them stand out.
        /// </summary>
        internal sealed class VectorGrid
        {
            readonly KodeWindow w;
            public readonly UIObject Parent;
            KodeWindow.PropertyGrid grid;

            readonly bool magnitudeColumn;

            /// <param name="magnitudeColumn">a |v| column even when the first row has no magnitude</param>
            public VectorGrid(KodeWindow w, UIObject parent, bool magnitudeColumn = false)
            {
                this.w = w;
                Parent = parent;
                this.magnitudeColumn = magnitudeColumn;
            }

            static UIText Cell(UIObject row, string text, bool dim)
            {
                UIText t = KodeWindow.LiveLabel(row, text, false);
                t.tmpText.alignment = TMPro.TextAlignmentOptions.Right;
                t.Text(dim ? text : "");
                if (dim) KodeWindow.ToneText(t, KodeWindow.Tone.Dim);
                return t;
            }

            /// <summary>A row; its numbers are refilled every frame while <paramref name="active"/> holds.</summary>
            public void Row(string label, Func<bool> active, Func<Vector3d> get, bool sci, bool magnitude)
            {
                // a new table when something else was added after the last one
                if (grid == null || grid.Table.transform.GetSiblingIndex() != Parent.transform.childCount - 1)
                {
                    grid = w.Properties(Parent);
                    grid.FlexibleColumn = 0;
                    Layout head = grid.Row();
                    KodeWindow.Label(head, "");
                    // the columns take the width of the numbers below
                    foreach (string axis in new[] { "X", "Y", "Z" }) Cell(head, axis, true);
                    if (magnitude || magnitudeColumn) Cell(head, "|v|", true);
                }

                Layout row = grid.Row();
                KodeWindow.Label(row, label);
                string t = sci ? "-9.99E+07" : "-9999.999";
                var x = new CharText(Cell(row, t, false).tmpText);
                var y = new CharText(Cell(row, t, false).tmpText);
                var z = new CharText(Cell(row, t, false).tmpText);
                CharText m = magnitude ? new CharText(Cell(row, "9999.999", false).tmpText) : null;
                w.AddSync(() =>
                {
                    if (!active()) return;
                    Vector3d v = get();
                    Fill(x, v.x, sci);
                    Fill(y, v.y, sci);
                    Fill(z, v.z, sci);
                    if (m == null) return;
                    m.Clear().Append(v.magnitude, 3).Commit();
                });
            }

            static void Fill(CharText t, double v, bool sci)
            {
                t.Clear();
                if (sci) PaddedSci(t, v);
                else Padded(t, v, 3);
                t.Commit();
            }
        }
        const string PlainTemplate = "[-9999.999, -9999.999, -9999.999 ]";
        const string SciTemplate = "[-9.99E+07, -9.99E+07, -9.99E+07 ]";

        // ---- building blocks ----

        sealed class Panel<T> where T : BaseAttitudeController
        {
            readonly KodeWindow w;
            readonly MechJebModuleAttitudeController att;
            public readonly UIObject Parent;

            public Panel(KodeWindow w, UIObject parent, MechJebModuleAttitudeController att)
            {
                this.w = w;
                Parent = parent;
                this.att = att;
            }

            public KodeWindow W => w;
            public MechJebModuleAttitudeController Att => att;

            /// <summary>The controller when it is the active one, otherwise null.</summary>
            public T C => att.Controller as T;

            public Layout Row(UIObject parent = null) => KodeWindow.Row(parent ?? Parent);

            public Func<bool> When(Func<T, bool> condition) => () =>
            {
                T c = C;
                return c != null && condition(c);
            };

            public void Hide(UIObject o, Func<T, bool> condition) => w.Visible(o.gameObject, When(condition));

            public Layout Indented(Func<T, bool> condition)
            {
                Layout row = Row();
                KodeWindow.Space(row, 20);
                Layout panel = KodeWindow.Panel(row);
                if (condition != null) Hide(row, condition);
                return panel;
            }

            public void Button(string text, Action<T> action) => KodeWindow.Button(Parent, text, () =>
            {
                T c = C;
                if (c != null) action(c);
            });

            public Toggle Tog(UIObject parent, string text, Func<T, bool> get, Action<T, bool> set) =>
                w.Toggle(parent, text, () =>
                {
                    T c = C;
                    return c != null && get(c);
                }, v =>
                {
                    T c = C;
                    if (c != null) set(c, v);
                });

            public void Tog(string text, Func<T, bool> get, Action<T, bool> set) => Tog(Parent, text, get, set);

            public void Fld(UIObject row, Func<T, EditableDoubleMult> ed, float width)
            {
                EditFields.Field(w, row, () =>
                {
                    T c = C;
                    return c == null ? "" : ed(c).Text;
                }, s =>
                {
                    T c = C;
                    if (c != null) ed(c).Text = s;
                }, width, () =>
                {
                    T c = C;
                    return c == null || ed(c).IsParsed;
                });
            }

            /// <summary>label (flexible)... [field]</summary>
            public void Edit(string label, Func<T, EditableDoubleMult> ed, float width, bool flexibleLabel = false)
            {
                Layout row = Row();
                KodeWindow.Label(row, label, flexibleLabel);
                Fld(row, ed, width);
            }

            /// <summary>[x] label [field]</summary>
            public void ToggleEdit(string label, Func<T, bool> get, Action<T, bool> set, Func<T, EditableDoubleMult> ed, float width)
            {
                Layout row = Row();
                Tog(row, label, get, set);
                Fld(row, ed, width);
            }

            /// <summary>label (flexible) + a live vector, committed only while this controller is the active one.</summary>
            VectorGrid vectors;

            public void Vec(string label, Func<T, Vector3d> get, Action<CharText, Vector3d> format = null, string template = null,
                UIObject parent = null)
            {
                // modern layout: the plain, scientific and magnitude vectors as X, Y, Z columns
                bool plain = format == null || format == Plain, sci = format == PlainSci, magnitude = format == WithMagnitude;
                if (KodeWindow.ModernLayout && (plain || sci || magnitude))
                {
                    UIObject into = parent ?? Parent;
                    if (vectors == null || vectors.Parent != into) vectors = new VectorGrid(w, into);
                    vectors.Row(label, () => C != null, () =>
                    {
                        T c = C;
                        return c == null ? Vector3d.zero : get(c);
                    }, sci, magnitude);
                    return;
                }

                Layout row = Row(parent);
                KodeWindow.Label(row, label, true);
                Value(row, get, format, template);
            }

            public void Value(UIObject row, Func<T, Vector3d> get, Action<CharText, Vector3d> format = null, string template = null)
            {
                format ??= Plain;
                UIText value = KodeWindow.LiveLabel(row, template ?? PlainTemplate, false);
                value.tmpText.alignment = TMPro.TextAlignmentOptions.Right;
                var text = new CharText(value.tmpText);
                w.AddSync(() =>
                {
                    T c = C;
                    if (c == null) return;
                    text.Clear();
                    format(text, get(c));
                    text.Commit();
                });
            }

            public void Sync(Action<T> action) => w.AddSync(() =>
            {
                T c = C;
                if (c != null) action(c);
            });
        }

        static void Magnitude<T>(Panel<T> p, string label, Func<T, Vector3d> get) where T : BaseAttitudeController =>
            p.Vec(label, get, WithMagnitude, VectorTemplate);

        static Vector3d Moi(Vessel v) => v == null ? Vector3d.zero : (Vector3d)v.MOI;

        // ---- MJAttitudeController ----

        static void BuildMJ(Panel<MJAttitudeController> p)
        {
            p.Button("#MechJeb_adv_reset_button", c => c.ResetConfig()); //"Reset"
            p.Tog("#MechJeb_AttitudeController_checkbox1", c => c.TfAutoTune, (c, v) => c.TfAutoTune = v); //" Auto-tuning"

            Layout tf = p.Indented(null);

            Layout manual = KodeWindow.Panel(tf);
            KodeWindow.Paragraph(manual, "#MechJeb_AttitudeController_label1"); //"Larger ship do better with a larger Tf"
            Layout row = p.Row(manual);
            KodeWindow.Label(row, "#MechJeb_AttitudeController_label2", true); //"Tf (s)"
            KodeWindow.Label(row, "#MechJeb_AttitudeController_label3"); //"P"
            p.Fld(row, c => c._uiTfX, 40);
            KodeWindow.Label(row, "#MechJeb_AttitudeController_label4"); //"Y"
            p.Fld(row, c => c._uiTfY, 40);
            KodeWindow.Label(row, "#MechJeb_AttitudeController_label5"); //"R"
            p.Fld(row, c => c._uiTfZ, 40);
            p.Hide(manual, c => !c.TfAutoTune);

            Layout auto = KodeWindow.Panel(tf);
            p.Vec("#MechJeb_AttitudeController_label6", c => c._tfV, parent: auto); //"Tf"
            Layout range = p.Row(auto);
            KodeWindow.Label(range, "#MechJeb_AttitudeController_label7", true); //"Tf range"
            KodeWindow.Label(range, "#MechJeb_AttitudeController_label8"); //"min"
            p.Fld(range, c => c._uiTfMin, 50);
            KodeWindow.Label(range, "#MechJeb_AttitudeController_label9"); //"max"
            p.Fld(range, c => c._uiTfMax, 50);
            p.Hide(auto, c => c.TfAutoTune);

            // the clamps GUI() applies after each text box
            p.Sync(c =>
            {
                if (c.TfAutoTune) c.ClampTfRange();
                else c.ClampTf();
            });

            p.Tog("#MechJeb_AttitudeController_checkbox2", c => c.LowPassFilter, (c, v) => c.SetLowPassFilter(v)); //" Low Pass Filter"

            KodeWindow.Label(p.Parent, "#MechJeb_AttitudeController_PIDF"); //"PID factors"
            Factor(p, "Kd = ", c => c._uiKdFactor, " / Tf");
            Factor(p, "Kp = pid.Kd / (", c => c._uiKpFactor, " * Math.Sqrt(2) * Tf)");
            Factor(p, "Ki = pid.Kp / (", c => c._uiKiFactor, " * Math.Sqrt(2) * Tf)");
            Factor(p, "#MechJeb_AttitudeController_PIDFactor1", c => c._uiDeadband, ""); //"Deadband = "

            p.Sync(c =>
            {
                c.ClampFactors();
                c.ClampDeadband();
            });
        }

        static void Factor(Panel<MJAttitudeController> p, string left, Func<MJAttitudeController, EditableDoubleMult> ed, string unit)
        {
            Layout row = p.Row();
            KodeWindow.Label(row, left);
            p.Fld(row, ed, 50);
            if (unit.Length > 0) KodeWindow.Label(row, unit);
        }


        // ---- KosAttitudeController ----

        static void BuildKos(Panel<KosAttitudeController> p)
        {
            p.Edit("MaxStoppingTime", c => c.MaxStoppingTime, 40);
            p.Edit("RollControlRange", c => c.RollControlRange, 40);
            p.Vec("Actuation", c => c._actuation);
            p.Vec("phiVector", c => c._phiVector);
            p.Vec("TargetTorque", c => c._targetTorque);
            p.Vec("ControlTorque", c => c._controlTorque);
        }

        // ---- HybridController ----

        static void BuildHybrid(Panel<HybridController> p)
        {
            p.Tog("#MechJeb_HybridController_checkbox1", c => c.UseInertia, (c, v) => c.UseInertia = v); //"useInertia"
            p.Edit("#MechJeb_HybridController_label1", c => c.MaxStoppingTime, 60); //"MaxStoppingTime"
            p.ToggleEdit("#MechJeb_HybridController_checkbox2", c => c.UseControlRange, (c, v) => c.UseControlRange = v, //"RollControlRange"
                c => c.RollControlRange, 60);
            p.Vec("#MechJeb_HybridController_label2", c => c._actuation); //"Actuation"
            p.Vec("#MechJeb_HybridController_label3", c => c._phiVector); //"phiVector"
            p.Vec("Omega", c => c._omega);
            p.Vec("MaxOmega", c => c._maxOmega);
            p.Vec("#MechJeb_HybridController_label4", c => c._targetTorque); //"TargetTorque"
            p.Vec("#MechJeb_HybridController_label5", c => c._controlTorque); //"ControlTorque"
            Magnitude(p, "#MechJeb_HybridController_label6", c => p.Att.Inertia); //"Inertia"
        }

        // ---- BetterController ----

        static void BuildBetter(Panel<BetterController> p)
        {
            p.ToggleEdit("Maximum Stopping Time", c => c.UseStoppingTime, (c, v) => c.UseStoppingTime = v, c => c.MaxStoppingTime, 60);
            p.ToggleEdit("Minimum Flip Time", c => c.UseFlipTime, (c, v) => c.UseFlipTime = v, c => c.MinFlipTime, 60);
            p.ToggleEdit("#MechJeb_HybridController_checkbox2", c => c.UseControlRange, (c, v) => c.UseControlRange = v, //"RollControlRange"
                c => c.RollControlRange, 60);
            p.Sync(c => c.FixFlipTime());

            const float COL1 = 100;
            const float COL = 56;

            Layout head = p.Row();
            Cell(head, "", COL1, true);
            Cell(head, "Velocity", COL, false);
            Cell(head, "Position", COL, false);

            Grid(p, "Kp", c => c.VelKp, c => c.PosKp);
            Grid(p, "Ti", c => c.VelTi, c => c.PosTi);
            Grid(p, "Td", c => c.VelTd, c => c.PosTd);
            Grid(p, "N", c => c.VelN, c => c.PosN);
            Grid(p, "B", c => c.VelB, c => c.PosB);
            Grid(p, "C", c => c.VelC, c => c.PosC);
            Grid(p, "Deadband", c => c.VelDeadband, c => c.PosDeadband);
            Grid(p, "SmoothIn", c => c.VelSmoothIn, c => c.PosSmoothIn);
            Grid(p, "SmoothOut", c => c.VelSmoothOut, c => c.PosSmoothOut);

            // each Clegg toggle in its column, under the Velocity and Position fields
            Layout clegg = p.Row();
            Cell(clegg, "", COL1, true);
            p.Tog(Column(clegg, COL), "Clegg", c => c.VelClegg, (c, v) => c.VelClegg = v);
            p.Tog(Column(clegg, COL), "Clegg", c => c.PosClegg, (c, v) => c.PosClegg = v);

            p.Vec("Pos PTerm", c => new Vector3d(c._posPID[0].PTerm, c._posPID[1].PTerm, c._posPID[2].PTerm), PlainSci, SciTemplate);
            p.Vec("Pos ITerm", c => new Vector3d(c._posPID[0].ITerm, c._posPID[1].ITerm, c._posPID[2].ITerm), PlainSci, SciTemplate);
            p.Vec("Pos DTerm", c => new Vector3d(c._posPID[0].DTerm, c._posPID[1].DTerm, c._posPID[2].DTerm), PlainSci, SciTemplate);
            p.Vec("Vel PTerm", c => new Vector3d(c._velPID[0].PTerm, c._velPID[1].PTerm, c._velPID[2].PTerm), PlainSci, SciTemplate);
            p.Vec("Vel ITerm", c => new Vector3d(c._velPID[0].ITerm, c._velPID[1].ITerm, c._velPID[2].ITerm), PlainSci, SciTemplate);
            p.Vec("Vel DTerm", c => new Vector3d(c._velPID[0].DTerm, c._velPID[1].DTerm, c._velPID[2].DTerm), PlainSci, SciTemplate);

            p.Edit("Smooth Torque", c => c.SmoothTorque, 50);
            p.Edit("Soften", c => c.Soften, 50);
            p.Button("Reset Tuning Values", c => c.Defaults());

            p.Vec("Current", c => c._current);
            p.Vec("Desired", c => c._desired);
            p.Vec("Error", c => c._error);
            p.Vec("TargetOmega", c => c._targetOmega);
            p.Vec("Omega", c => c._vessel == null ? Vector3d.zero : c._vessel.angularVelocityD);
            p.Vec("#MechJeb_HybridController_label2", c => c._actuation); //"Actuation"
            p.Vec("#MechJeb_HybridController_label4", c => c._targetTorque); //"TargetTorque"
            p.Vec("#MechJeb_HybridController_label5", c => c._controlTorque); //"ControlTorque"
            p.Vec("TargetAlpha", c => c._targetAlpha);
            p.Vec("MaxAlpha", c => c._maxAlpha);
            p.Vec("MOI", c => Moi(c._vessel));
            p.Vec("Response Speed", c => p.Att.VesselState == null ? Vector3d.zero : p.Att.VesselState.TorqueResponseSpeed);
        }

        /// <summary>A text in a cell of fixed width (or a flexible one), so the columns of a grid line up with its fields.</summary>
        static void Cell(UIObject row, string text, float width, bool flexible)
        {
            UIText t = KodeWindow.Label(row, text, flexible);
            var le = t.gameObject.GetComponent<LayoutElement>() ?? t.gameObject.AddComponent<LayoutElement>();
            le.minWidth = flexible ? 60 : width;
            if (!flexible) le.preferredWidth = width;
        }

        /// <summary>A fixed-width column in a grid row, for a widget that would otherwise stretch (a toggle row).</summary>
        static Layout Column(UIObject row, float width)
        {
            Layout col = row.Add<Layout>("Column");
            col.Horizontal().ControlChildSize(true, true).ChildForceExpand(true, false);
            col.Finish();
            var le = col.gameObject.GetComponent<LayoutElement>() ?? col.gameObject.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = width;
            le.flexibleWidth = 0;
            return col;
        }

        static void Grid(Panel<BetterController> p, string label, Func<BetterController, EditableDoubleMult> vel,
            Func<BetterController, EditableDoubleMult> pos)
        {
            Layout row = p.Row();
            Cell(row, label, 100, true);
            p.Fld(row, vel, 56);
            p.Fld(row, pos, 56);
        }

        // ---- LQRController ----

        static void BuildLQR(Panel<LQRController> p)
        {
            p.Edit("Smooth Torque", c => c.SmoothTorque, 50);
            p.Vec("Position", c => c._current);
            p.Vec("Desired", c => c._desired);
            p.Vec("Error", c => c._error);
            p.Vec("Velocity", c => c._velocity);
            p.Vec("Mass", c => c._mass);
            p.Vec("Actuation", c => c._actuation);
            p.Vec("MOI", c => Moi(c._vessel));
            p.Vec("Control Torque", c => c._controlTorque);
            p.Vec("Applied Torque", c => c._targetTorque);
        }
    }
}
