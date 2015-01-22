using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleDockingGuidance : DisplayModule
    {
        public MechJebModuleDockingGuidance(MechJebCore core) : base(core) { }

        MechJebModuleDockingAutopilot autopilot;

        public override void OnStart(PartModule.StartState state)
        {
            autopilot = core.GetComputerModule<MechJebModuleDockingAutopilot>();
        }

        protected override void WindowGUI(int windowID)
        {
            if (!core.target.NormalTargetExists)
            {
                GUILayout.Label("Choose a target to dock with");
                base.WindowGUI(windowID);
                return;
            }

            GUILayout.BeginVertical();

            // GetReferenceTransformPart is null after undocking ...
            if (vessel.GetReferenceTransformPart() == null || !vessel.GetReferenceTransformPart().Modules.Contains("ModuleDockingNode"))
            {
                GUIStyle s = new GUIStyle(GUI.skin.label);
                s.normal.textColor = Color.yellow;
                GUILayout.Label("Warning: You need to control the vessel from a docking port. Right click a docking port and select \"Control from here\"",s);
            }

            if (!(core.target.Target is ModuleDockingNode))
            {
                GUIStyle s = new GUIStyle(GUI.skin.label);
                s.normal.textColor = Color.yellow;
                GUILayout.Label("Warning: target is not a docking port. Right click the target docking port and select \"Set as target\"", s);
            }

            bool onAxisNodeExists = false;
            foreach (ITargetable node in vessel.GetTargetables().Where(t => t.GetTargetingMode() == VesselTargetModes.DirectionVelocityAndOrientation))
            {
                if (Vector3d.Angle(node.GetTransform().forward, vessel.ReferenceTransform.up) < 2)
                {
                    onAxisNodeExists = true;
                    break;
                }
            }

            if (!onAxisNodeExists)
            {
                GUIStyle s = new GUIStyle(GUI.skin.label);
                s.normal.textColor = Color.yellow;
                GUILayout.Label("Warning: this vessel is not controlled from a docking node. Right click the desired docking node on this vessel and select \"Control from here.\"", s);
            }

            bool active = GUILayout.Toggle(autopilot.enabled, "Autopilot enabled");
            GuiUtils.SimpleTextBox("Speed limit", autopilot.speedLimit, "m/s");

            autopilot.overrideSafeDistance = GUILayout.Toggle(autopilot.overrideSafeDistance, "Override Safe Distance");
            if (autopilot.overrideSafeDistance)
                GuiUtils.SimpleTextBox("Safe Distance", autopilot.overridenSafeDistance, "m");

            autopilot.overrideTargetSize = GUILayout.Toggle(autopilot.overrideTargetSize, "Override Start Distance");
            if (autopilot.overrideTargetSize)
                GuiUtils.SimpleTextBox("Start Distance", autopilot.overridenTargetSize, "m");

            if (autopilot.overridenSafeDistance < 0)
                autopilot.overridenSafeDistance = 0;

            if (autopilot.overridenTargetSize < 10)
                autopilot.overridenTargetSize = 10;

            autopilot.drawBoundingBox = GUILayout.Toggle(autopilot.drawBoundingBox, "Draw Bounding Box");

            if (GUILayout.Button("Dump Bounding Box Info"))
            {
                vessel.GetBoundingBox(true);

                if (core.target.Target != null)
                {
                    Vessel targetVessel = core.target.Target.GetVessel();
                    targetVessel.GetBoundingBox(true);
                }
            }


            GUILayout.Label("safeDistance " + autopilot.safeDistance.ToString("F2"), GUILayout.ExpandWidth(false));
            GUILayout.Label("targetSize   " + autopilot.targetSize.ToString("F2"), GUILayout.ExpandWidth(false));
			
            if (autopilot.speedLimit < 0)
                autopilot.speedLimit = 0;


            GUILayout.BeginHorizontal();
            autopilot.forceRol = GUILayout.Toggle(autopilot.forceRol, "Force Roll :", GUILayout.ExpandWidth(false));

            autopilot.rol.text = GUILayout.TextField(autopilot.rol.text, GUILayout.Width(30));
            GUILayout.Label("°", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            if (autopilot.enabled != active)
            {
                if (active)
                {
                    autopilot.users.Add(this);
                }
                else
                {
                    autopilot.users.Remove(this);
                }
            }

            if (autopilot.enabled)
            {
                GUILayout.Label("Status: " + autopilot.status);
                Vector3d error = core.rcs.targetVelocity - vesselState.orbitalVelocity;
                double error_x = Vector3d.Dot(error, vessel.GetTransform().right);
                double error_y = Vector3d.Dot(error, vessel.GetTransform().forward);
                double error_z = Vector3d.Dot(error, vessel.GetTransform().up);
                GUILayout.Label("Error X: " + error_x.ToString("F2") + " m/s  [L/J]");
                GUILayout.Label("Error Y: " + error_y.ToString("F2") + " m/s  [I/K]");
                GUILayout.Label("Error Z: " + error_z.ToString("F2") + " m/s  [H/N]");

                GUILayout.Label("Distance Dock: " + autopilot.zSep.ToString("F2") + "m");
                GUILayout.Label("Distance Axe Dock: " + autopilot.lateralSep.magnitude.ToString("F2") + "m");

            }

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(300), GUILayout.Height(50) };
        }

        public override void OnModuleDisabled()
        {
            if (autopilot != null) autopilot.users.Remove(this);
        }

        public override string GetName()
        {
            return "Docking Autopilot";
        }
    }
}
