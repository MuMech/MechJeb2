using System;
using System.Collections.Generic;
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
        static Func<string> FloatLabel(string key, Func<float> value) => Memo.Of(value, v => Localizer.Format(key, v.ToString("F2")));

        protected override void BuildContents(KodeWindow w)
        {
            if (KodeWindow.ModernLayout)
            {
                BuildModern(w);
                return;
            }

            UIObject r = w.Root;

            Layout main = TargetGate(w, r);

            UIText warnControl = GuidanceUi.Warning(w, main, "#MechJeb_Docking_label2");
            w.Visible(warnControl.gameObject, () => noDockingPortControl);

            TargetPortSelector(w, main);

            UIText warnTarget = GuidanceUi.Warning(w, main, "#MechJeb_Docking_label3");
            w.Visible(warnTarget.gameObject, () => !(Core.Target.Target is ModuleDockingNode) && guidance.selectedTargetPort == null);

            UIText warnAxis = GuidanceUi.Warning(w, main, "#MechJeb_Docking_label4");
            w.Visible(warnAxis.gameObject, () => noOnAxisNode);

            AutopilotSwitch(w, main);
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
            EditUi.Field(w, roll, () => Ap?.rol, 30);
            KodeWindow.Label(roll, "°");

            // while the autopilot runs
            Layout running = KodeWindow.Panel(main);
            w.Visible(running.gameObject, () => ApEnabled);
            RunningLabels(w, running);
        }

        // modern layout: the setup problems in one status bar at the top, the options in a property grid, the running
        // state in a status bar at the bottom
        void BuildModern(KodeWindow w)
        {
            UIObject r = w.Root;

            Layout main = TargetGate(w, r);

            bool NoTargetPort() => !(Core.Target.Target is ModuleDockingNode) && guidance.selectedTargetPort == null;
            Layout problems = w.StatusBar(main, () => KodeWindow.Tone.Warn);
            w.Visible(problems.gameObject, () => noDockingPortControl || noOnAxisNode || NoTargetPort());
            w.Visible(GuidanceUi.Wrapped(problems, "#MechJeb_Docking_label2").gameObject, () => noDockingPortControl);
            w.Visible(GuidanceUi.Wrapped(problems, "#MechJeb_Docking_label3").gameObject, NoTargetPort);
            w.Visible(GuidanceUi.Wrapped(problems, "#MechJeb_Docking_label4").gameObject, () => noOnAxisNode);

            TargetPortSelector(w, main);

            AutopilotSwitch(w, main);

            KodeWindow.PropertyGrid g = w.Properties(main);
            GridField(g, "#MechJeb_Docking_label5", () => Ap?.speedLimit, "m/s"); //"Speed limit"
            g.Switch("#MechJeb_Docking_checkbox2", () => Ap != null && Ap.overrideSafeDistance, v => //"Override Safe Distance"
            {
                if (Ap != null) Ap.overrideSafeDistance = v;
            });
            Layout safe = GridField(g, "#MechJeb_Docking_checkbox3", () => Ap?.overridenSafeDistance, "m"); //"Safe Distance"
            g.Dim(safe.gameObject, null, () => Ap == null || !Ap.overrideSafeDistance);
            g.Switch("#MechJeb_Docking_checkbox4", () => Ap != null && Ap.overrideTargetSize, v => //"Override Start Distance"
            {
                if (Ap != null) Ap.overrideTargetSize = v;
            });
            Layout start = GridField(g, "#MechJeb_Docking_label6", () => Ap?.overridenTargetSize, "m"); //"Start Distance"
            g.Dim(start.gameObject, null, () => Ap == null || !Ap.overrideTargetSize);
            g.Switch("#MechJeb_Docking_checkbox6", () => Ap != null && Ap.forceRol, v => //"Force Roll :"
            {
                if (Ap != null) Ap.forceRol = v;
            }, () => Ap?.rol.Text ?? "", s =>
            {
                if (Ap != null) Ap.rol.Text = s;
            }, 50, "°", () => Ap?.rol.IsParsed ?? true);
            g.Switch("#MechJeb_Docking_checkbox5", () => Ap != null && Ap.drawBoundingBox, v => //"Draw Bounding Box"
            {
                if (Ap != null) Ap.drawBoundingBox = v;
            });

            KodeWindow.Button(main, "#MechJeb_Docking_button", guidance.DumpBoundingBoxInfo, true); //"Dump Bounding Box Info"
            Layout box = KodeWindow.Row(main);
            UIText safeText = w.BoundLabel(box, FloatLabel("#MechJeb_Docking_label7", () => Ap?.safeDistance ?? 0), true); //"safeDistance "
            UIText sizeText = w.BoundLabel(box, FloatLabel("#MechJeb_Docking_label8", () => Ap?.targetSize ?? 0), true); //"targetSize   "
            KodeWindow.ToneText(safeText, KodeWindow.Tone.Dim);
            KodeWindow.ToneText(sizeText, KodeWindow.Tone.Dim);

            Layout running = w.StatusBar(r, () => KodeWindow.Tone.Normal);
            w.Visible(running.gameObject, () => HasTarget && ApEnabled);
            RunningLabels(w, running);
        }

        // the parts both layouts share: the target gate, the autopilot switch and the running state

        /// <summary>"Choose a target to dock with" without a target; returns the panel shown with one.</summary>
        Layout TargetGate(KodeWindow w, UIObject r)
        {
            Layout none = KodeWindow.Panel(r);
            GuidanceUi.Wrapped(none, "#MechJeb_Docking_label1"); //"Choose a target to dock with"
            w.Visible(none.gameObject, () => !HasTarget);

            Layout main = KodeWindow.Panel(r);
            w.Visible(main.gameObject, () => HasTarget);
            return main;
        }

        void AutopilotSwitch(KodeWindow w, UIObject parent) =>
            w.Toggle(parent, "#MechJeb_Docking_checkbox1", () => ApEnabled, v => //"Autopilot enabled"
            {
                if (v) Ap.Users.Add(guidance);
                else Ap.Users.Remove(guidance);
            });

        void RunningLabels(KodeWindow w, UIObject running)
        {
            w.Wrap(w.BoundLabel(running, GuidanceUi.Format1("#MechJeb_Docking_label9", () => Ap?.status), true)); //"Status: <<1>>"
            ErrorLabel(w, running, "#MechJeb_Docking_label10", " m/s  [L/J]", () => errorX); //Error X: <<1>>
            ErrorLabel(w, running, "#MechJeb_Docking_label11", " m/s  [I/K]", () => errorY); //Error Y: <<1>>
            ErrorLabel(w, running, "#MechJeb_Docking_label12", " m/s  [H/N]", () => errorZ); //Error Z: <<1>>
            ErrorLabel(w, running, "#MechJeb_Docking_label13", "m", () => Ap.zSep); //Distance Dock: <<1>>
            ErrorLabel(w, running, "#MechJeb_Docking_label14", "m", () => Ap.lateralSep.magnitude); //Distance Dock Axis: <<1>>
        }

        /// <summary>label [field] unit in a property grid; the editable is read again on every access (the autopilot replaces it).</summary>
        static Layout GridField(KodeWindow.PropertyGrid g, string key, Func<EditableDouble> ed, string unit) => EditUi.Grid(g, key, ed, unit, 70);

        /// <summary>"label: value suffix" refilled every frame without allocating.</summary>
        void ErrorLabel(KodeWindow w, UIObject parent, string key, string suffix, Func<double> value)
        {
            var parts = new GuidanceUi.Parts(key);
            GuidanceUi.Live(w, parent, parts.Pre + "-123.45" + parts.Post + suffix, true,
                ct => ct.Append(parts.Pre).Append(value(), 2).Append(parts.Post).Append(suffix), () => ApEnabled);
        }

        /// <summary>GuiUtils.SimpleTextBox(label, editable, unit); the editable is read again on every access (the autopilot replaces it when it clamps).</summary>
        static Layout EditRow(KodeWindow w, UIObject parent, string key, Func<EditableDouble> ed, string unit) => EditUi.Row(w, parent, key, ed, unit);

        // DrawTargetPortSelector: "Target port" [<] port (n/m) [>] (the modern layout: a dropdown), or a note when the target is out of range
        void TargetPortSelector(KodeWindow w, UIObject parent)
        {
            Layout row = KodeWindow.Row(parent);
            w.Visible(row.gameObject, () => portState != MechJebModuleDockingGuidance.PortSelectorState.Hidden);
            KodeWindow.Label(row, "#MechJeb_Docking_targetPort");

            // ArrowSelectorStyeGuiStyleExpand: centred and word-wrapped in the room left by the label
            UIText unavailable = GuidanceUi.Wrapped(row, "#MechJeb_Docking_targetPortUnavailable");
            unavailable.tmpText.alignment = TextAlignmentOptions.Center;
            w.Visible(unavailable.gameObject, () => portState == MechJebModuleDockingGuidance.PortSelectorState.Unavailable);

            Func<bool> ready = () => portState == MechJebModuleDockingGuidance.PortSelectorState.Ready;
            Func<bool> canSelect = () => guidance.targetPorts.Count > 0 && !ApEnabled;

            if (KodeWindow.ModernLayout)
            {
                // modern layout: the target vessel's free compatible ports in a dropdown, by name. The combo shows itself as
                // [<] entry [>] when MechJeb's "no drop-down menus" setting is on, so its own row is shown and hidden here
                Layout combo = KodeWindow.Row(row);
                combo.FlexibleLayout(true, false);
                new ComboBox(w, combo, () => guidance.currentPortIndex < 0 ? 0 : guidance.currentPortIndex, SelectPort, PortNames);
                w.Visible(combo.gameObject, ready);
                foreach (UIButton b in combo.GetComponentsInChildren<UIButton>(true)) GuidanceUi.Enable(w, b, canSelect);
                return;
            }

            UIButton prev = KodeWindow.Button(row, "<", () =>
            {
                if (guidance.targetPorts.Count > 0) guidance.SelectTargetPort(guidance.targetPorts, guidance.currentPortIndex, -1);
            });
            w.Visible(prev.gameObject, ready);
            GuidanceUi.Enable(w, prev, canSelect);

            UIText label = GuidanceUi.Live(w, row, "Docking Port Jr. (12/12)", true, ct => ct.Append(guidance.targetPortLabel), ready);
            label.tmpText.alignment = TextAlignmentOptions.Center;
            w.Visible(label, ready);

            UIButton next = KodeWindow.Button(row, ">", () =>
            {
                if (guidance.targetPorts.Count > 0) guidance.SelectTargetPort(guidance.targetPorts, guidance.currentPortIndex, 1);
            });
            w.Visible(next.gameObject, ready);
            GuidanceUi.Enable(w, next, canSelect);
        }

        // the dropdown's entries: "name (n/m)" per port, after "Select a port (m available)" while none is chosen. Rebuilt
        // only when the ports or the first entry change; SelectPort reads the ports the entries were made from
        ModuleDockingNode[] listedPorts = new ModuleDockingNode[0];
        string[] portNames = new string[0];
        bool listedPrompt;

        string[] PortNames()
        {
            List<ModuleDockingNode> ports = guidance.targetPorts;
            bool prompt = guidance.currentPortIndex < 0;
            bool same = prompt == listedPrompt && ports.Count == listedPorts.Length && (!prompt || portNames[0] == guidance.targetPortLabel);
            for (int i = 0; same && i < ports.Count; i++) same = ports[i] == listedPorts[i];
            if (same) return portNames;

            listedPorts = ports.ToArray();
            listedPrompt = prompt;
            int first = prompt ? 1 : 0;
            portNames = new string[ports.Count + first];
            if (prompt) portNames[0] = guidance.targetPortLabel; // "Select a port (m available)", or "No compatible undocked ports"
            for (int i = 0; i < ports.Count; i++)
                portNames[first + i] = Localizer.Format("#MechJeb_Docking_targetPortSelected", MechJebModuleDockingGuidance.GetPortName(ports[i]),
                    i + 1, ports.Count);
            return portNames;
        }

        void SelectPort(int entry)
        {
            int i = entry - (listedPrompt ? 1 : 0);
            if (i < 0 || i >= listedPorts.Length || listedPorts[i] == null || ApEnabled) return;
            guidance.SelectTargetPort(listedPorts[i]);
            portState = guidance.UpdateTargetPortSelector(); // show the choice now, not at the next slow update
        }
    }
}
