using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    class MechJebModuleSmarterASS : DisplayModule
    {
        enum Direction { FORWARD, BACK, UP, DOWN, LEFT, RIGHT };
        Dictionary<Direction, Vector3d> directions;

        AttitudeReference reference = AttitudeReference.INERTIAL;
        Direction direction = Direction.FORWARD;

        public MechJebModuleSmarterASS(MechJebCore core)
            : base(core)
        {
            directions = new Dictionary<Direction, Vector3d>();
            directions.Add(Direction.FORWARD, Vector3d.forward);
            directions.Add(Direction.BACK, Vector3d.back);
            directions.Add(Direction.UP, Vector3d.up);
            directions.Add(Direction.DOWN, Vector3d.down);
            directions.Add(Direction.LEFT, Vector3d.left);
            directions.Add(Direction.RIGHT, Vector3d.right);
        }

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
                direction = (Direction)(((int)direction - 1 + Enum.GetValues(typeof(Direction)).Length) % Enum.GetValues(typeof(Direction)).Length);
            }
            GUILayout.Label(direction.ToString());
            if (GUILayout.Button("▻"))
            {
                changed = true;
                direction = (Direction)(((int)direction + 1 + Enum.GetValues(typeof(Direction)).Length) % Enum.GetValues(typeof(Direction)).Length);
            }
            GUILayout.EndHorizontal();

            bool wasEnabled = core.attitude.enabled;
            core.attitude.enabled = GUILayout.Toggle(core.attitude.enabled, "Enable");

            if (wasEnabled != core.attitude.enabled)
            {
                changed = true;
            }

            if (changed)
            {
                core.attitude.attitudeTo(directions[direction], reference, this);
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
