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

        protected override void FlightWindowGUI(int windowID)
        {
            if (!Target.Exists())
            {
                GUILayout.Label("Choose a target to dock with");
                base.FlightWindowGUI(windowID);
                return;
            }

            Vector3d relVel = Target.RelativeVelocity(part.vessel);

            double relVel_x = Vector3d.Dot(relVel, part.vessel.GetTransform().right);
            double relVel_y = Vector3d.Dot(relVel, part.vessel.GetTransform().forward);
            double relVel_z = Vector3d.Dot(relVel, part.vessel.GetTransform().up);

            Vector3d sep = Target.RelativePosition(part.vessel);

            double sep_x = Vector3d.Dot(sep, part.vessel.GetTransform().right);
            double sep_y = Vector3d.Dot(sep, part.vessel.GetTransform().forward);
            double sep_z = Vector3d.Dot(sep, part.vessel.GetTransform().up);

            GUILayout.BeginVertical();

            GUILayout.Label("Relative velocity:");
            GUILayout.Label("X : " + relVel_x.ToString("F2") + " m/s  [L/J]");
            GUILayout.Label("Y: " + relVel_y.ToString("F2") + " m/s  [I/K]");
            GUILayout.Label("Z: " + relVel_z.ToString("F2") + " m/s  [H/N]");

            GUILayout.Label("Separation:");
            GUILayout.Label("X: " + sep_x.ToString("F2") + " m  [L/J]");
            GUILayout.Label("Y: " + sep_y.ToString("F2") + " m  [I/K]");
            GUILayout.Label("Z: " + sep_z.ToString("F2") + " m  [H/N]");

            MechJebModuleDockingAutopilot autopilot = core.GetComputerModule<MechJebModuleDockingAutopilot>();
            autopilot.enabled = GUILayout.Toggle(autopilot.enabled, "Autopilot enabled.");

            if (autopilot.enabled)
            {
                GUILayout.Label("Status: " + autopilot.status);
                Vector3d error = core.rcs.targetVelocity - vesselState.velocityVesselOrbit;
                double error_x = Vector3d.Dot(error, part.vessel.GetTransform().right);
                double error_y = Vector3d.Dot(error, part.vessel.GetTransform().forward);
                double error_z = Vector3d.Dot(error, part.vessel.GetTransform().up);
                GUILayout.Label("Error X: " + error_x.ToString("F2") + " m/s  [L/J]");
                GUILayout.Label("Error Y: " + error_y.ToString("F2") + " m/s  [I/K]");
                GUILayout.Label("Error Z: " + error_z.ToString("F2") + " m/s  [H/N]");

            }


            GUILayout.EndVertical();

            base.FlightWindowGUI(windowID);
        }

        public override GUILayoutOption[] FlightWindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(300), GUILayout.Height(50) };
        }

        public override string GetName()
        {
            return "Docking Guidance";
        }
    }
}
