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
    /// MechJebModuleDockingGuidance ("Docking Autopilot") in KodeKit.
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
        bool HasTarget => Core.Target != null && Core.Target.NormalTargetExists && Core.Target.Target.GetVessel() != null;
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

                // the values are set in place: a custom window's field edits these objects
                if (ap.overridenSafeDistance < 0)
                    ap.overridenSafeDistance.Val = 0;

                if (ap.overridenTargetSize < 10)
                    ap.overridenTargetSize.Val = 10;

                // 0 means no limit, so a negative value must not fall to 0
                if (ap.speedLimit < 0)
                    ap.speedLimit.Val = 0.1;

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

        // The setup problems in one status bar at the top, the options in a property grid, the running
        // state in a status bar at the bottom
        protected override void BuildContents(KodeWindow w)
        {
            UIObject r = w.Root;

            Layout main = TargetGate(w, r);

            bool NoTargetPort() => !(Core.Target.Target is ModuleDockingNode);
            // controlling from a port that is off the control axis is one problem with the port: one line for both
            bool OffAxis() => noOnAxisNode && !noDockingPortControl;
            Layout problems = w.StatusBar(main, () => KodeWindow.Tone.Warn);
            w.Visible(problems.gameObject, () => noDockingPortControl || OffAxis() || NoTargetPort());
            w.Visible(GuidanceUi.Wrapped(problems, "#MechJeb_NewUI_DockControlFromPort").gameObject, () => noDockingPortControl);
            w.Visible(GuidanceUi.Wrapped(problems, "#MechJeb_NewUI_DockPortOffAxis").gameObject, OffAxis);
            w.Visible(GuidanceUi.Wrapped(problems, "#MechJeb_NewUI_DockTargetAPort").gameObject, NoTargetPort);

            TargetPortSelector(w, main);

            EngageButton(w, main);

            // the one setting without a switch in a grid of its own, so its label starts at the left edge
            KodeWindow.PropertyGrid limits = w.Properties(main);
            GridField(limits, "#MechJeb_Docking_label5", () => Ap?.speedLimit, "m/s"); //"Speed limit (0: none)"

            // each override on one row, the distance in use under it
            KodeWindow.PropertyGrid g = w.Properties(main);
            g.Switch("#MechJeb_NewUI_DockSafeDistance", () => Ap != null && Ap.overrideSafeDistance, v => //Override safe distance
            {
                if (Ap != null) Ap.overrideSafeDistance = v;
            }, () => Ap?.overridenSafeDistance.Text ?? "", s =>
            {
                if (Ap != null) Ap.overridenSafeDistance.Text = s;
            }, 70, "m", () => Ap?.overridenSafeDistance.IsParsed ?? true);
            InUse(g, () => Ap?.safeDistance ?? 0);
            g.Switch("#MechJeb_NewUI_DockStartDistance", () => Ap != null && Ap.overrideTargetSize, v => //Override start distance
            {
                if (Ap != null) Ap.overrideTargetSize = v;
            }, () => Ap?.overridenTargetSize.Text ?? "", s =>
            {
                if (Ap != null) Ap.overridenTargetSize.Text = s;
            }, 70, "m", () => Ap?.overridenTargetSize.IsParsed ?? true);
            InUse(g, () => Ap?.targetSize ?? 0);
            g.Switch("#MechJeb_Docking_checkbox7", () => Ap != null && Ap.stopOnContact, v => //"Stop on contact"
            {
                if (Ap != null) Ap.stopOnContact = v;
            });
            g.Switch("#MechJeb_NewUI_DockForceRoll", () => Ap != null && Ap.forceRol, v => //Force roll
            {
                if (Ap != null) Ap.forceRol = v;
            }, () => Ap?.rol.Text ?? "", s =>
            {
                if (Ap != null) Ap.rol.Text = s;
            }, 50, "°", () => Ap?.rol.IsParsed ?? true);

            // the bounding box tools, for checking the autopilot's idea of the vessels' size
            Layout debug = w.Section(main, Localizer.Format("#MechJeb_NewUI_Debug"), false, "DockingDebug"); //Debug
            KodeWindow.PropertyGrid dg = w.Properties(debug);
            dg.Switch("#MechJeb_Docking_checkbox5", () => Ap != null && Ap.drawBoundingBox, v => //"Draw Bounding Box"
            {
                if (Ap != null) Ap.drawBoundingBox = v;
            });
            KodeWindow.Button(debug, "#MechJeb_Docking_button", guidance.DumpBoundingBoxInfo, true); //"Dump Bounding Box Info"

            Layout running = w.StatusBar(r, () => KodeWindow.Tone.Normal);
            w.Visible(running.gameObject, () => HasTarget && ApEnabled);
            RunningLabels(w, running);
        }

        // the target gate, the autopilot switch and the running state

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

        // Engage autopilot (the main action), Disengage autopilot while it runs. The setup problems above don't disable it:
        // without a port pair the autopilot flies to the target vessel and stops short of it
        void EngageButton(KodeWindow w, UIObject parent)
        {
            UIButton engage = KodeWindow.Button(parent, "#MechJeb_RZauto_button1", () => Ap?.Users.Add(guidance), ButtonRole.Primary, true); //Engage autopilot
            w.Visible(engage.gameObject, () => Ap != null && !ApEnabled);
            UIButton disengage = KodeWindow.Button(parent, "#MechJeb_RZauto_button2", () => Ap?.Users.Clear(), ButtonRole.Danger, true); //Disengage autopilot
            w.Visible(disengage.gameObject, () => ApEnabled); // Clear: also a run kOS started
        }

        // "In use  10.00 m": the distance the autopilot works with, overridden or computed
        static void InUse(KodeWindow.PropertyGrid g, Func<float> value)
        {
            Layout row = g.Value("#MechJeb_NewUI_DockInUse", Memo.Of(value, v => v.ToString("F2") + " m")); //In use
            foreach (UIText t in row.GetComponentsInChildren<UIText>(true)) KodeWindow.ToneText(t, KodeWindow.Tone.Dim);
        }

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

        // DrawTargetPortSelector: "Target port" and a dropdown of the ports, or a note when the target is out of range
        void TargetPortSelector(KodeWindow w, UIObject parent)
        {
            // out of range: one line instead of the row, so the note doesn't wrap in the room left by the label
            UIText unavailable = KodeWindow.Paragraph(parent, "#MechJeb_NewUI_DockPortUnavailable"); //Target port: available when the target vessel is nearby.
            KodeWindow.ToneText(unavailable, KodeWindow.Tone.Dim);
            w.Visible(unavailable.gameObject, () => portState == MechJebModuleDockingGuidance.PortSelectorState.Unavailable);

            Layout row = KodeWindow.Row(parent);
            w.Visible(row.gameObject, () => portState == MechJebModuleDockingGuidance.PortSelectorState.Ready);
            KodeWindow.Label(row, "#MechJeb_Docking_targetPort");

            Func<bool> ready = () => portState == MechJebModuleDockingGuidance.PortSelectorState.Ready;
            Func<bool> canSelect = () => guidance.targetPorts.Count > 0 && !ApEnabled;

            // The target vessel's free compatible ports in a dropdown, by name. The combo shows itself as
            // [<] entry [>] when MechJeb's "no drop-down menus" setting is on, so its own row is shown and hidden here
            Layout combo = KodeWindow.Row(row);
            combo.FlexibleLayout(true, false);
            new ComboBox(w, combo, () => guidance.currentPortIndex < 0 ? 0 : guidance.currentPortIndex, SelectPort, PortNames);
            w.Visible(combo.gameObject, ready);
            foreach (UIButton b in combo.GetComponentsInChildren<UIButton>(true)) GuidanceUi.Enable(w, b, canSelect);
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
