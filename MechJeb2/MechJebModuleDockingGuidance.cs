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
        internal ModuleDockingNode selectedTargetPort;
        private Vessel selectedTargetVessel;

        public override void OnStart(PartModule.StartState state) => autopilot = Core.GetComputerModule<MechJebModuleDockingAutopilot>();

        internal enum PortSelectorState { Hidden, Unavailable, Ready }

        // Model of the target port selector, filled by UpdateTargetPortSelector (shared by IMGUI and the new UI).
        internal readonly List<ModuleDockingNode> targetPorts = new List<ModuleDockingNode>();
        internal int currentPortIndex = -1;
        internal string targetPortLabel = "";

        internal bool OnAxisNodeExists()
        {
            // GetTargetables without its list: both UIs check this while the window is open
            if (Vessel == null) return false;
            foreach (Part part in Vessel.Parts)
                for (int i = 0; i < part.Modules.Count; i++)
                    if (part.Modules[i] is ITargetable node && node.GetTargetingMode() == VesselTargetModes.DirectionVelocityAndOrientation &&
                        Vector3d.Angle(node.GetTransform().forward, Vessel.ReferenceTransform.up) < 2)
                        return true;

            return false;
        }

        internal void DumpBoundingBoxInfo()
        {
            Vessel.GetBoundingBox(true);

            if (Core.Target.Target != null)
            {
                Vessel targetVessel = Core.Target.Target.GetVessel();
                targetVessel.GetBoundingBox(true);
            }
        }

        internal PortSelectorState UpdateTargetPortSelector()
        {
            targetPorts.Clear();
            currentPortIndex = -1;
            Vessel targetVessel = GetTargetVessel();
            if (targetVessel == null || targetVessel == Vessel)
            {
                selectedTargetPort = null;
                selectedTargetVessel = null;
                return PortSelectorState.Hidden;
            }

            if (selectedTargetVessel != targetVessel)
            {
                selectedTargetPort = null;
                selectedTargetVessel = targetVessel;
            }

            ModuleDockingNode referencePort = GetReferenceDockingPort();
            if (!targetVessel.loaded || targetVessel.packed)
                return PortSelectorState.Unavailable;

            foreach (ModuleDockingNode port in targetVessel.GetModules<ModuleDockingNode>())
            {
                if (IsAvailableTargetPort(port) && port.GetTransform() != null && ArePortsCompatible(referencePort, port))
                    targetPorts.Add(port);
            }

            var currentPort = Core.Target.Target as ModuleDockingNode;
            if (currentPort != null && targetPorts.Contains(currentPort))
                selectedTargetPort = currentPort;
            else if (selectedTargetPort != null && !targetPorts.Contains(selectedTargetPort))
                selectedTargetPort = null;

            if (currentPort == null || !targetPorts.Contains(currentPort))
                currentPort = selectedTargetPort;

            currentPortIndex = targetPorts.IndexOf(currentPort);
            if (currentPortIndex >= 0)
            {
                targetPortLabel = Localizer.Format("#MechJeb_Docking_targetPortSelected", GetPortName(currentPort), currentPortIndex + 1,
                    targetPorts.Count);
            }
            else if (targetPorts.Count > 0)
            {
                targetPortLabel = Localizer.Format("#MechJeb_Docking_targetPortSelect", targetPorts.Count);
            }
            else
            {
                targetPortLabel = Localizer.Format("#MechJeb_Docking_targetPortNone");
            }

            return PortSelectorState.Ready;
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

        internal void SelectTargetPort(IReadOnlyList<ModuleDockingNode> targetPorts, int currentIndex, int direction)
        {
            int nextIndex = currentIndex < 0
                ? direction > 0 ? 0 : targetPorts.Count - 1
                : (currentIndex + direction + targetPorts.Count) % targetPorts.Count;

            SelectTargetPort(targetPorts[nextIndex]);
        }

        internal void SelectTargetPort(ModuleDockingNode targetPort)
        {
            selectedTargetPort = targetPort;
            Core.Target.Set(targetPort);
            // Keep KSP's global target in sync so other docking aids observe the port change immediately.
            FlightGlobals.fetch.SetVesselTarget(targetPort);
        }

        internal static string GetPortName(ModuleDockingNode port)
        {
            string portName = port.GetName();
            if (!string.IsNullOrEmpty(portName))
                return portName;

            return port.part?.partInfo?.title ?? port.part?.name ?? "?";
        }

        protected override void OnModuleDisabled()
        {
            if (autopilot != null) autopilot.Users.Remove(this);
        }

        public override string GetName() => Localizer.Format("#MechJeb_Docking_title"); //"Docking Autopilot"

        public override string IconName() => "Docking Autopilot";
    }
}
