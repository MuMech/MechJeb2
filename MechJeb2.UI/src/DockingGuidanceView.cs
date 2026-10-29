using System;
using KodeKit;
using KodeUI;
using KSP.Localization;
using TMPro;
using UnityEngine;

namespace MuMech.UI
{
    /// <summary>
    /// MechJebModuleDockingGuidance ("Docking Autopilot") in KodeKit, row for row with its WindowGUI.
    /// The target port selector's model and the docking-axis check are internal methods of the module, shared with IMGUI.
    /// The checks that walk the vessel's parts and targetables run five times a second, not every frame.
    /// </summary>
    [KodeViewFor(typeof(MechJebModuleDockingGuidance))]
    public class DockingGuidanceView : KodeView
    {
        readonly MechJebModuleDockingGuidance guidance;
        MechJebModuleDockingAutopilot autopilot;

        public DockingGuidanceView(MechJebModuleDockingGuidance guidance) : base(guidance) => this.guidance = guidance;

        protected override float MinWidth => 300;

        MechJebCore Core => guidance.Core;
        MechJebModuleDockingAutopilot Ap => autopilot ??= Core.GetComputerModule<MechJebModuleDockingAutopilot>();
        bool HasTarget => Core.Target != null && Core.Target.NormalTargetExists;
        bool ApEnabled => Ap != null && Ap.Enabled;

        // state refreshed by BeforeSync
        MechJebModuleDockingGuidance.PortSelectorState portState = MechJebModuleDockingGuidance.PortSelectorState.Hidden;
        bool noDockingPortControl, noOnAxisNode;
        float nextSlowUpdate;
        double errorX, errorY, errorZ;

        protected override void BeforeSync()
        {
            if (!HasTarget)
            {
                portState = MechJebModuleDockingGuidance.PortSelectorState.Hidden;
                return;
            }

            try
            {
                if (Time.unscaledTime >= nextSlowUpdate)
                {
                    nextSlowUpdate = Time.unscaledTime + 0.2f;
                    Part reference = guidance.Vessel.GetReferenceTransformPart();
                    // GetReferenceTransformPart is null after undocking ...
                    noDockingPortControl = reference == null || !reference.Modules.Contains("ModuleDockingNode");
                    portState = guidance.UpdateTargetPortSelector();
                    noOnAxisNode = !guidance.OnAxisNodeExists();
                }

                MechJebModuleDockingAutopilot ap = Ap;
                if (ap == null) return;

                if (ap.overridenSafeDistance < 0)
                    ap.overridenSafeDistance = 0;

                if (ap.overridenTargetSize < 10)
                    ap.overridenTargetSize = 10;

                if (ap.speedLimit < 0)
                    ap.speedLimit = 0;

                if (ap.Enabled)
                {
                    Vector3d error = Core.RCS.targetVelocity - guidance.VesselState.OrbitalVelocity;
                    Transform t = guidance.Vessel.GetTransform();
                    errorX = Vector3d.Dot(error, t.right);
                    errorY = Vector3d.Dot(error, t.forward);
                    errorZ = Vector3d.Dot(error, t.up);
                }
            }
            catch (Exception)
            {
                // no reference transform yet: try again next time
            }
        }

        // Localizer.Format(key, value.ToString("F2")) rebuilt only when the value changes
        static Func<string> FloatLabel(string key, Func<float> value)
        {
            float last = float.NaN;
            string text = null;
            return () =>
            {
                float v = value();
                if (text == null || v != last)
                {
                    last = v;
                    text = Localizer.Format(key, v.ToString("F2"));
                }

                return text;
            };
        }

        protected override void BuildContents(KodeWindow w)
        {
            UIObject r = w.Root;

            Layout none = KodeWindow.Panel(r);
            GuidanceUi.Wrapped(none, "#MechJeb_Docking_label1"); //"Choose a target to dock with"
            w.Visible(none.gameObject, () => !HasTarget);

            Layout main = KodeWindow.Panel(r);
            w.Visible(main.gameObject, () => HasTarget);

            UIText warnControl = GuidanceUi.Warning(w, main, "#MechJeb_Docking_label2");
            w.Visible(warnControl.gameObject, () => noDockingPortControl);

            TargetPortSelector(w, main);

            UIText warnTarget = GuidanceUi.Warning(w, main, "#MechJeb_Docking_label3");
            w.Visible(warnTarget.gameObject, () => !(Core.Target.Target is ModuleDockingNode) && guidance.selectedTargetPort == null);

            UIText warnAxis = GuidanceUi.Warning(w, main, "#MechJeb_Docking_label4");
            w.Visible(warnAxis.gameObject, () => noOnAxisNode);

            w.Toggle(main, "#MechJeb_Docking_checkbox1", () => ApEnabled, v => //"Autopilot enabled"
            {
                if (v) Ap.Users.Add(guidance);
                else Ap.Users.Remove(guidance);
            });
            EditRow(w, main, "#MechJeb_Docking_label5", () => Ap?.speedLimit, "m/s"); //"Speed limit"

            w.Toggle(main, "#MechJeb_Docking_checkbox2", () => Ap != null && Ap.overrideSafeDistance, v => //"Override Safe Distance"
            {
                if (Ap != null) Ap.overrideSafeDistance = v;
            });
            Layout safe = EditRow(w, main, "#MechJeb_Docking_checkbox3", () => Ap?.overridenSafeDistance, "m"); //"Safe Distance"
            w.Visible(safe.gameObject, () => Ap != null && Ap.overrideSafeDistance);

            w.Toggle(main, "#MechJeb_Docking_checkbox4", () => Ap != null && Ap.overrideTargetSize, v => //"Override Start Distance"
            {
                if (Ap != null) Ap.overrideTargetSize = v;
            });
            Layout start = EditRow(w, main, "#MechJeb_Docking_label6", () => Ap?.overridenTargetSize, "m"); //"Start Distance"
            w.Visible(start.gameObject, () => Ap != null && Ap.overrideTargetSize);

            w.Toggle(main, "#MechJeb_Docking_checkbox5", () => Ap != null && Ap.drawBoundingBox, v => //"Draw Bounding Box"
            {
                if (Ap != null) Ap.drawBoundingBox = v;
            });
            KodeWindow.Button(main, "#MechJeb_Docking_button", guidance.DumpBoundingBoxInfo); //"Dump Bounding Box Info"

            w.BoundLabel(main, FloatLabel("#MechJeb_Docking_label7", () => Ap?.safeDistance ?? 0), true); //"safeDistance "
            w.BoundLabel(main, FloatLabel("#MechJeb_Docking_label8", () => Ap?.targetSize ?? 0), true); //"targetSize   "

            Layout roll = KodeWindow.Row(main);
            w.Toggle(roll, "#MechJeb_Docking_checkbox6", () => Ap != null && Ap.forceRol, v => //"Force Roll :"
            {
                if (Ap != null) Ap.forceRol = v;
            });
            EditFields.Field(w, roll, () => Ap?.rol.Text ?? "", s =>
            {
                if (Ap != null) Ap.rol.Text = s;
            }, 30, () => Ap?.rol.IsParsed ?? true);
            KodeWindow.Label(roll, "°");

            // while the autopilot runs
            Layout running = KodeWindow.Panel(main);
            w.Visible(running.gameObject, () => ApEnabled);
            w.BoundLabel(running, GuidanceUi.Format1("#MechJeb_Docking_label9", () => Ap?.status), true); //"Status: <<1>>"
            ErrorLabel(w, running, "#MechJeb_Docking_label10", " m/s  [L/J]", () => errorX); //Error X: <<1>>
            ErrorLabel(w, running, "#MechJeb_Docking_label11", " m/s  [I/K]", () => errorY); //Error Y: <<1>>
            ErrorLabel(w, running, "#MechJeb_Docking_label12", " m/s  [H/N]", () => errorZ); //Error Z: <<1>>
            ErrorLabel(w, running, "#MechJeb_Docking_label13", "m", () => Ap.zSep); //Distance Dock: <<1>>
            ErrorLabel(w, running, "#MechJeb_Docking_label14", "m", () => Ap.lateralSep.magnitude); //Distance Dock Axis: <<1>>
        }

        /// <summary>"label: value suffix" refilled every frame without allocating.</summary>
        void ErrorLabel(KodeWindow w, UIObject parent, string key, string suffix, Func<double> value)
        {
            var parts = new GuidanceUi.Parts(key);
            GuidanceUi.Live(w, parent, parts.Pre + "-123.45" + parts.Post + suffix, true,
                ct => ct.Append(parts.Pre).Append(value(), 2).Append(parts.Post).Append(suffix), () => ApEnabled);
        }

        /// <summary>GuiUtils.SimpleTextBox(label, editable, unit); the editable is read again on every access (the autopilot replaces it when it clamps).</summary>
        Layout EditRow(KodeWindow w, UIObject parent, string key, Func<EditableDouble> ed, string unit)
        {
            Layout row = KodeWindow.Row(parent);
            KodeWindow.Label(row, key);
            EditFields.Field(w, row, () => ed()?.Text ?? "", s =>
            {
                EditableDouble e = ed();
                if (e != null) e.Text = s;
            }, 100, () => ed()?.IsParsed ?? true);
            KodeWindow.Label(row, unit);
            return row;
        }

        // DrawTargetPortSelector: "Target port" [<] port (n/m) [>], or a note when the target is out of range
        void TargetPortSelector(KodeWindow w, UIObject parent)
        {
            Layout row = KodeWindow.Row(parent);
            w.Visible(row.gameObject, () => portState != MechJebModuleDockingGuidance.PortSelectorState.Hidden);
            KodeWindow.Label(row, "#MechJeb_Docking_targetPort");

            UIText unavailable = KodeWindow.Label(row, "#MechJeb_Docking_targetPortUnavailable", true);
            w.Visible(unavailable.gameObject, () => portState == MechJebModuleDockingGuidance.PortSelectorState.Unavailable);

            Func<bool> ready = () => portState == MechJebModuleDockingGuidance.PortSelectorState.Ready;
            Func<bool> canSelect = () => guidance.targetPorts.Count > 0 && !ApEnabled;

            UIButton prev = KodeWindow.Button(row, "<", () =>
            {
                if (guidance.targetPorts.Count > 0) guidance.SelectTargetPort(guidance.targetPorts, guidance.currentPortIndex, -1);
            });
            w.Visible(prev.gameObject, ready);
            GuidanceUi.Enable(w, prev, canSelect);

            UIText label = GuidanceUi.Live(w, row, "Docking Port Jr. (12/12)", true, ct => ct.Append(guidance.targetPortLabel), ready);
            label.tmpText.alignment = TextAlignmentOptions.Center;
            w.Visible(label.transform.parent.gameObject, ready);

            UIButton next = KodeWindow.Button(row, ">", () =>
            {
                if (guidance.targetPorts.Count > 0) guidance.SelectTargetPort(guidance.targetPorts, guidance.currentPortIndex, 1);
            });
            w.Visible(next.gameObject, ready);
            GuidanceUi.Enable(w, next, canSelect);
        }
    }
}
