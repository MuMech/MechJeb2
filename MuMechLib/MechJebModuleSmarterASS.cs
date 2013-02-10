using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleSmarterASS : DisplayModule
    {
        [Persistent(pass = (int)Pass.Local)]
        AttitudeReference reference = AttitudeReference.INERTIAL;
        [Persistent(pass = (int)Pass.Local)]
        Vector6.Direction direction = Vector6.Direction.FORWARD;

        public MechJebModuleSmarterASS(MechJebCore core) : base(core) { }

        protected override void FlightWindowGUI(int windowID)
        {
            bool changed = false;

            GUILayout.BeginVertical();

            GUILayout.Label("Reference:");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("◅"))
            {
                changed = true;
                reference = (AttitudeReference)(((int)reference - 1 + Enum.GetValues(typeof(AttitudeReference)).Length) % Enum.GetValues(typeof(AttitudeReference)).Length);
            }
            GUILayout.Label(reference.ToString());
            if (GUILayout.Button("▻"))
            {
                changed = true;
                reference = (AttitudeReference)(((int)reference + 1 + Enum.GetValues(typeof(AttitudeReference)).Length) % Enum.GetValues(typeof(AttitudeReference)).Length);
            }
            GUILayout.EndHorizontal();

            GUILayout.Label("Direction:");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("◅"))
            {
                changed = true;
                direction = (Vector6.Direction)(((int)direction - 1 + Enum.GetValues(typeof(Vector6.Direction)).Length) % Enum.GetValues(typeof(Vector6.Direction)).Length);
            }
            GUILayout.Label(direction.ToString());
            if (GUILayout.Button("▻"))
            {
                changed = true;
                direction = (Vector6.Direction)(((int)direction + 1 + Enum.GetValues(typeof(Vector6.Direction)).Length) % Enum.GetValues(typeof(Vector6.Direction)).Length);
            }
            GUILayout.EndHorizontal();

            bool wasEnabled = core.attitude.enabled;
            bool newEnabled = GUILayout.Toggle(core.attitude.enabled, "Enable");

            if (newEnabled && !wasEnabled)
            {
                core.attitude.users.Add(this); 
                changed = true;
            }

            if (!newEnabled && wasEnabled) core.attitude.users.Remove(this);


            if (changed)
            {
                core.attitude.attitudeTo(Vector6.directions[direction], reference, this);
            }

            GUILayout.EndVertical();

            base.FlightWindowGUI(windowID);
        }

        public override GUILayoutOption[] FlightWindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(300), GUILayout.Height(150) };
        }

        public override string GetName()
        {
            return "Smarter A.S.S.";
        }
    }
}
