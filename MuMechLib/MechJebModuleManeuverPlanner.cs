using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    class MechJebModuleManeuverPlanner : DisplayModule
    {
        public MechJebModuleManeuverPlanner(MechJebCore core) : base(core) { }

        enum Operation { CIRCULARIZE, ELLIPTICIZE, PERIAPSIS, APOAPSIS, INCLINATION, PLANE };
        static int numOperations = Enum.GetNames(typeof(Operation)).Length;
        Operation operation = Operation.CIRCULARIZE;

        EditableDouble pe = new EditableDouble(0, 1000);
        EditableDouble ap = new EditableDouble(0, 1000);
        EditableDouble inc = new EditableDouble(0);
        EditableDouble lead = new EditableDouble(0);

        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            GUILayout.Label("Insert a maneuver node to:");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("◅")) operation = (Operation)(((int)operation - 1 + numOperations) % numOperations);
            GUILayout.Label(operation.ToString());
            if (GUILayout.Button("▻")) operation = (Operation)(((int)operation + 1 + numOperations) % numOperations);
            GUILayout.EndHorizontal();

            switch (operation)
            {
                case Operation.CIRCULARIZE:
                    break;

                case Operation.ELLIPTICIZE:
                    GuiUtils.SimpleTextBox("Pe (km)", pe, 1000);
                    GuiUtils.SimpleTextBox("Ap (km)", ap, 1000);
                    break;

                case Operation.PERIAPSIS:
                    GuiUtils.SimpleTextBox("Pe (km)", pe, 1000);
                    break;

                case Operation.APOAPSIS:
                    GuiUtils.SimpleTextBox("Ap (km)", ap, 1000);
                    break;

                case Operation.INCLINATION:
                    GuiUtils.SimpleTextBox("Inc (deg)", inc, 1000);
                    break;
            }

            GuiUtils.SimpleTextBox("In (seconds): ", lead, 1);
            double UT = vesselState.time + lead;

            if (GUILayout.Button("Go"))
            {
/*                Orbit test1 = MuUtils.OrbitFromStateVectors(part.vessel.orbit.SwappedRelativePositionAtUT(vesselState.time), part.vessel.orbit.SwappedOrbitalVelocityAtUT(vesselState.time), part.vessel.orbit.referenceBody, vesselState.time);
                Orbit test2 = MuUtils.OrbitFromStateVectors(vesselState.CoM - vesselState.mainBody.position, vesselState.velocityVesselOrbit, vesselState.mainBody, vesselState.time);
                Orbit test3 = MuUtils.OrbitFromStateVectors(part.vessel.orbit.getRelativePositionAtUT(vesselState.time), part.vessel.orbit.getOrbitalVelocityAtUT(vesselState.time), vesselState.mainBody, vesselState.time);
                Orbit test4 = MuUtils.OrbitFromStateVectors(part.vessel.orbit.getPositionAtUT(vesselState.time), part.vessel.orbit.getOrbitalVelocityAtUT(vesselState.time), vesselState.mainBody, vesselState.time);

                MonoBehaviour.print("test1.apa = " + test1.ApA);
                MonoBehaviour.print("test2.apa = " + test2.ApA);
                MonoBehaviour.print("test3.apa = " + test3.ApA);
                MonoBehaviour.print("test4.apa = " + test4.ApA);*/



                Vector3d dV = Vector3d.zero;
                double rad = part.vessel.mainBody.Radius;
                switch (operation)
                {
                    case Operation.CIRCULARIZE:
                        dV = OrbitalManeuverCalculator.DeltaVToCircularize(part.vessel.orbit, UT);
                        break;

                    case Operation.ELLIPTICIZE:
                        dV = OrbitalManeuverCalculator.DeltaVToEllipticize(part.vessel.orbit, UT, pe + rad, ap + rad);
                        break;

                    case Operation.PERIAPSIS:
                        dV = OrbitalManeuverCalculator.DeltaVToChangePeriapsis(part.vessel.orbit, UT, pe + rad);
                        break;

                    case Operation.APOAPSIS:
                        dV = OrbitalManeuverCalculator.DeltaVToChangeApoapsis(part.vessel.orbit, UT, ap + rad);
                        break;

                    case Operation.INCLINATION:
                        dV = OrbitalManeuverCalculator.DeltaVToChangeInclination(part.vessel.orbit, UT, inc);
                        break;
                }
                PlaceManeuverNode(part.vessel.orbit, dV, UT);
            }

            GUILayout.EndVertical();



            base.WindowGUI(windowID);
        }




        //input dV should be in world coordinates
        public void PlaceManeuverNode(Orbit o, Vector3d dV, double UT)
        {
            //convert a dV in world coordinates into the coordinate system of the maneuver node,
            //where (x, y, z) are (radial+, normal+, prograde)
            Vector3d nodeDV = new Vector3d(Vector3d.Dot(o.RadialPlus(UT), dV),
                                           Vector3d.Dot(o.NormalPlus(UT), dV),
                                           Vector3d.Dot(o.Prograde(UT), dV));
            ManeuverNode mn = part.vessel.patchedConicSolver.AddManeuverNode(UT);
            mn.OnGizmoUpdated(nodeDV, UT);
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(300), GUILayout.Height(300) };
        }

    }
}
