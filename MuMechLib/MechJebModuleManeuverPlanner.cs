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

        enum Node { ASCENDING, DESCENDING };
        Node planeMatchNode;

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

                case Operation.PLANE:
                    if (GUILayout.Button(planeMatchNode.ToString())) planeMatchNode = (Node)(((int)planeMatchNode + 1) % 2);
                    break;
            }

            GuiUtils.SimpleTextBox("In (seconds): ", lead, 1);
            double UT = vesselState.time + lead;

            if (GUILayout.Button("Go"))
            {
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

                    case Operation.PLANE:
                        if (planeMatchNode == Node.ASCENDING)
                        {
                            dV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesAscending(part.vessel.orbit, FlightGlobals.fetch.VesselTarget.GetOrbit(), vesselState.time, out UT);
                        }
                        else
                        {
                            dV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesDescending(part.vessel.orbit, FlightGlobals.fetch.VesselTarget.GetOrbit(), vesselState.time, out UT);
                        }
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
            //which uses (x, y, z) = (radial+, normal-, prograde)
            Vector3d nodeDV = new Vector3d(Vector3d.Dot(o.RadialPlus(UT), dV),
                                           Vector3d.Dot(-o.NormalPlus(UT), dV),
                                           Vector3d.Dot(o.Prograde(UT), dV));
            ManeuverNode mn = part.vessel.patchedConicSolver.AddManeuverNode(UT);
            mn.OnGizmoUpdated(nodeDV, UT);
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(300), GUILayout.Height(150) };
        }

    }
}
