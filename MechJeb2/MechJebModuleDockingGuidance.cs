extern alias JetBrainsAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleDockingGuidance : DisplayModule
    {
        public MechJebModuleDockingGuidance(MechJebCore core) : base(core) { }

        private MechJebModuleDockingAutopilot autopilot;

        private static readonly char[] DockingNodeTypeSeparator = { ',' };

        // KSP changes a docking-port target back to its parent vessel while the
        // target is farther than the part-target range.  Keep the user's port
        // choice locally so that cycling does not jump back to the first port.
        private ModuleDockingNode selectedTargetPort;
        private Vessel selectedTargetVessel;

        public override void OnStart(PartModule.StartState state) => autopilot = Core.GetComputerModule<MechJebModuleDockingAutopilot>();

        protected override void WindowGUI(int windowID)
        {
            if (!Core.Target.NormalTargetExists)
            {
                GUILayout.Label(Localizer.Format("#MechJeb_Docking_label1")); //"Choose a target to dock with"
                base.WindowGUI(windowID);
                return;
            }

            GUILayout.BeginVertical();

            // GetReferenceTransformPart is null after undocking ...
            if (Vessel.GetReferenceTransformPart() == null || !Vessel.GetReferenceTransformPart().Modules.Contains("ModuleDockingNode"))
            {
                GUILayout.Label(Localizer.Format("#MechJeb_Docking_label2"),
                    GuiUtils.YellowLabel); //Warning: You need to control the vessel from a docking port. Right click a docking port and select "Control from here"
            }

            DrawTargetPortSelector();

            if (!(Core.Target.Target is ModuleDockingNode) && selectedTargetPort == null)
            {
                GUILayout.Label(Localizer.Format("#MechJeb_Docking_label3"),
                    GuiUtils.YellowLabel); //Warning: target is not a docking port. Right click the target docking port and select "Set as target"
            }

            bool onAxisNodeExists = false;
            foreach (ITargetable node in Vessel.GetTargetables()
                        .Where(t => t.GetTargetingMode() == VesselTargetModes.DirectionVelocityAndOrientation))
            {
                if (Vector3d.Angle(node.GetTransform().forward, Vessel.ReferenceTransform.up) < 2)
                {
                    onAxisNodeExists = true;
                    break;
                }
            }

            if (!onAxisNodeExists)
            {
                GUILayout.Label(Localizer.Format("#MechJeb_Docking_label4"),
                    GuiUtils.YellowLabel); //Warning: this vessel is not controlled from a docking node. Right click the desired docking node on this vessel and select "Control from here."
            }

            bool active = GUILayout.Toggle(autopilot.Enabled, Localizer.Format("#MechJeb_Docking_checkbox1")); // "Autopilot enabled"
            GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_Docking_label5"), autopilot.speedLimit, "m/s"); //"Speed limit"

            autopilot.overrideSafeDistance =
                GUILayout.Toggle(autopilot.overrideSafeDistance, Localizer.Format("#MechJeb_Docking_checkbox2")); //"Override Safe Distance"
            if (autopilot.overrideSafeDistance)
                GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_Docking_checkbox3"), autopilot.overridenSafeDistance, "m"); //"Safe Distance"

            autopilot.overrideTargetSize =
                GUILayout.Toggle(autopilot.overrideTargetSize, Localizer.Format("#MechJeb_Docking_checkbox4")); //"Override Start Distance"
            if (autopilot.overrideTargetSize)
                GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_Docking_label6"), autopilot.overridenTargetSize, "m"); //"Start Distance"

            if (autopilot.overridenSafeDistance < 0)
                autopilot.overridenSafeDistance = 0;

            if (autopilot.overridenTargetSize < 10)
                autopilot.overridenTargetSize = 10;

            autopilot.drawBoundingBox =
                GUILayout.Toggle(autopilot.drawBoundingBox, Localizer.Format("#MechJeb_Docking_checkbox5")); //"Draw Bounding Box"

            if (GUILayout.Button(Localizer.Format("#MechJeb_Docking_button"))) //"Dump Bounding Box Info"
            {
                Vessel.GetBoundingBox(true);

                if (Core.Target.Target != null)
                {
                    Vessel targetVessel = Core.Target.Target.GetVessel();
                    targetVessel.GetBoundingBox(true);
                }
            }


            GUILayout.Label(Localizer.Format("#MechJeb_Docking_label7", autopilot.safeDistance.ToString("F2")),
                GuiUtils.LayoutNoExpandWidth); //"safeDistance "
            GUILayout.Label(Localizer.Format("#MechJeb_Docking_label8", autopilot.targetSize.ToString("F2")),
                GuiUtils.LayoutNoExpandWidth); //"targetSize   "

            if (autopilot.speedLimit < 0)
                autopilot.speedLimit = 0;


            GUILayout.BeginHorizontal();
            autopilot.forceRol =
                GUILayout.Toggle(autopilot.forceRol, Localizer.Format("#MechJeb_Docking_checkbox6"), GuiUtils.LayoutNoExpandWidth); //"Force Roll :"

            autopilot.rol.Text = GUILayout.TextField(autopilot.rol.Text, GuiUtils.LayoutWidth(30));
            GUILayout.Label("°", GuiUtils.LayoutNoExpandWidth);
            GUILayout.EndHorizontal();

            if (autopilot.Enabled != active)
            {
                if (active)
                {
                    autopilot.Users.Add(this);
                }
                else
                {
                    autopilot.Users.Remove(this);
                }
            }

            if (autopilot.Enabled)
            {
                GUILayout.Label(Localizer.Format("#MechJeb_Docking_label9", autopilot.status)); //"Status: <<1>>"
                Vector3d error = Core.RCS.targetVelocity - VesselState.OrbitalVelocity;
                double error_x = Vector3d.Dot(error, Vessel.GetTransform().right);
                double error_y = Vector3d.Dot(error, Vessel.GetTransform().forward);
                double error_z = Vector3d.Dot(error, Vessel.GetTransform().up);
                GUILayout.Label(Localizer.Format("#MechJeb_Docking_label10", error_x.ToString("F2")) + " m/s  [L/J]"); //Error X: <<1>>
                GUILayout.Label(Localizer.Format("#MechJeb_Docking_label11", error_y.ToString("F2")) + " m/s  [I/K]"); //Error Y: <<1>>
                GUILayout.Label(Localizer.Format("#MechJeb_Docking_label12", error_z.ToString("F2")) + " m/s  [H/N]"); //Error Z: <<1>>

                GUILayout.Label(Localizer.Format("#MechJeb_Docking_label13", autopilot.zSep.ToString("F2")) + "m"); //Distance Dock: <<1>>
                GUILayout.Label(Localizer.Format("#MechJeb_Docking_label14", autopilot.lateralSep.magnitude.ToString("F2")) +
                    "m"); //Distance Dock Axis: <<1>>
            }

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        private void DrawTargetPortSelector()
        {
            Vessel targetVessel = GetTargetVessel();
            if (targetVessel == null || targetVessel == Vessel)
            {
                selectedTargetPort = null;
                selectedTargetVessel = null;
                return;
            }

            if (selectedTargetVessel != targetVessel)
            {
                selectedTargetPort = null;
                selectedTargetVessel = targetVessel;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#MechJeb_Docking_targetPort"), GuiUtils.LayoutNoExpandWidth);

            ModuleDockingNode referencePort = GetReferenceDockingPort();
            if (!targetVessel.loaded || targetVessel.packed)
            {
                GUILayout.Label(Localizer.Format("#MechJeb_Docking_targetPortUnavailable"), GuiUtils.ArrowSelectorStyeGuiStyleExpand);
                GUILayout.EndHorizontal();
                return;
            }

            List<ModuleDockingNode> targetPorts = targetVessel.GetModules<ModuleDockingNode>()
                .Where(port => IsAvailableTargetPort(port) && port.GetTransform() != null &&
                    ArePortsCompatible(referencePort, port))
                .ToList();

            ModuleDockingNode currentPort = Core.Target.Target as ModuleDockingNode;
            if (currentPort != null && targetPorts.Contains(currentPort))
                selectedTargetPort = currentPort;
            else if (selectedTargetPort != null && !targetPorts.Contains(selectedTargetPort))
                selectedTargetPort = null;

            if (currentPort == null || !targetPorts.Contains(currentPort))
                currentPort = selectedTargetPort;

            int currentIndex = targetPorts.IndexOf(currentPort);
            string portLabel;
            if (currentIndex >= 0)
            {
                portLabel = Localizer.Format("#MechJeb_Docking_targetPortSelected", GetPortName(currentPort), currentIndex + 1,
                    targetPorts.Count);
            }
            else if (targetPorts.Count > 0)
            {
                portLabel = Localizer.Format("#MechJeb_Docking_targetPortSelect", targetPorts.Count);
            }
            else
            {
                portLabel = Localizer.Format("#MechJeb_Docking_targetPortNone");
            }

            bool guiEnabled = GUI.enabled;
            GUI.enabled = guiEnabled && targetPorts.Count > 0 && !autopilot.Enabled;
            if (GUILayout.Button("<", GUILayout.ExpandWidth(false)))
                SelectTargetPort(targetPorts, currentIndex, -1);
            GUILayout.Label(portLabel, GuiUtils.ArrowSelectorStyeGuiStyleExpand);
            if (GUILayout.Button(">", GUILayout.ExpandWidth(false)))
                SelectTargetPort(targetPorts, currentIndex, 1);
            GUI.enabled = guiEnabled;
            GUILayout.EndHorizontal();
        }

        private Vessel GetTargetVessel()
        {
            if (Core.Target.Target is Vessel targetVessel)
                return targetVessel;

            return Core.Target.Target?.GetVessel();
        }

        private ModuleDockingNode GetReferenceDockingPort()
        {
            Part referencePart = Vessel.GetReferenceTransformPart();
            if (referencePart == null || Vessel.ReferenceTransform == null)
                return null;

            ModuleDockingNode referencePort = null;
            double smallestAngle = 2;
            foreach (ModuleDockingNode port in referencePart.Modules.OfType<ModuleDockingNode>())
            {
                Transform portTransform = port.GetTransform();
                if (portTransform == null)
                    continue;

                double angle = Vector3d.Angle(portTransform.forward, Vessel.ReferenceTransform.up);
                if (angle < smallestAngle)
                {
                    smallestAngle = angle;
                    referencePort = port;
                }
            }

            return referencePort;
        }

        private static bool IsAvailableTargetPort(ModuleDockingNode port) =>
            port != null && !IsDockedState(port.state);

        private static bool IsDockedState(string state) =>
            state != null && (state.StartsWith("Docked", StringComparison.Ordinal) || state == "PreAttached");

        private static bool ArePortsCompatible(ModuleDockingNode sourcePort, ModuleDockingNode targetPort)
        {
            if (sourcePort == null || targetPort == null)
                return false;

            return ArePortsCompatible(sourcePort.nodeType, sourcePort.gendered, sourcePort.genderFemale, targetPort.nodeType,
                targetPort.gendered, targetPort.genderFemale);
        }

        private static bool ArePortsCompatible(string sourceNodeType, bool sourceGendered, bool sourceFemale, string targetNodeType,
            bool targetGendered, bool targetFemale)
        {
            if (sourceGendered != targetGendered)
                return false;

            if (sourceGendered && sourceFemale == targetFemale)
                return false;

            if (string.IsNullOrEmpty(sourceNodeType) || string.IsNullOrEmpty(targetNodeType))
                return false;

            string[] sourceNodeTypes = sourceNodeType.Split(DockingNodeTypeSeparator, StringSplitOptions.RemoveEmptyEntries);
            string[] targetNodeTypes = targetNodeType.Split(DockingNodeTypeSeparator, StringSplitOptions.RemoveEmptyEntries);
            return sourceNodeTypes.Intersect(targetNodeTypes).Any();
        }

        private void SelectTargetPort(IReadOnlyList<ModuleDockingNode> targetPorts, int currentIndex, int direction)
        {
            int nextIndex = currentIndex < 0
                ? direction > 0 ? 0 : targetPorts.Count - 1
                : (currentIndex + direction + targetPorts.Count) % targetPorts.Count;

            ModuleDockingNode targetPort = targetPorts[nextIndex];
            selectedTargetPort = targetPort;
            Core.Target.Set(targetPort);
            // Keep KSP's global target in sync so other docking aids observe the port change immediately.
            FlightGlobals.fetch.SetVesselTarget(targetPort);
        }

        private static string GetPortName(ModuleDockingNode port)
        {
            string portName = port.GetName();
            if (!string.IsNullOrEmpty(portName))
                return portName;

            return port.part?.partInfo?.title ?? port.part?.name ?? "?";
        }

        protected override GUILayoutOption[] WindowOptions() => new[] { GuiUtils.LayoutWidth(300), GUILayout.Height(50) };

        protected override void OnModuleDisabled()
        {
            if (autopilot != null) autopilot.Users.Remove(this);
        }

        public override string GetName() => Localizer.Format("#MechJeb_Docking_title"); //"Docking Autopilot"

        public override string IconName() => "Docking Autopilot";
    }
}
