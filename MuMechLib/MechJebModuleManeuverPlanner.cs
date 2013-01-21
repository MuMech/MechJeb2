using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleManeuverPlanner : DisplayModule
    {
        public MechJebModuleManeuverPlanner(MechJebCore core) : base(core) { }

        enum Operation { CIRCULARIZE, ELLIPTICIZE, PERIAPSIS, APOAPSIS, INCLINATION, PLANE, TRANSFER, COURSE_CORRECTION, 
            INTERPLANETARY_TRANSFER, LAMBERT};
        enum TimeReference { NOW, APOAPSIS, PERIAPSIS, ASCENDING_NODE, DESCENDING_NODE, NINETY_BEFORE_AP };

        Operation operation = Operation.CIRCULARIZE;
        TimeReference timeReference = TimeReference.NOW;
        bool timeReferenceSinceLast = true;
        bool createNode = true;

        enum Node { ASCENDING, DESCENDING };
        Node planeMatchNode;

        EditableDouble pe = new EditableDouble(0, 1000);
        EditableDouble ap = new EditableDouble(0, 1000);
        EditableDouble inc = new EditableDouble(0);
        EditableTime lead = new EditableTime(0);
        EditableTime interceptInterval = new EditableTime(3600);

        protected override void FlightWindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            bool anyNodeExists = (vessel.patchedConicSolver.maneuverNodes.Count > 0);

            if (anyNodeExists)
            {
                if (GUILayout.Button(createNode ? "Create a new" : "Change the last"))
                {
                    createNode = !createNode;
                }
                GUILayout.Label("maneuver node to:");
            }
            else
            {
                GUILayout.Label("Create a new maneuver node to:");
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("◅")) operation = (Operation)(((int)operation - 1 + Enum.GetNames(typeof(Operation)).Length) % Enum.GetNames(typeof(Operation)).Length);
            GUILayout.Label(operation.ToString());
            if (GUILayout.Button("▻")) operation = (Operation)(((int)operation + 1 + Enum.GetNames(typeof(Operation)).Length) % Enum.GetNames(typeof(Operation)).Length);
            GUILayout.EndHorizontal();

            switch (operation)
            {
                case Operation.CIRCULARIZE:
                    break;

                case Operation.ELLIPTICIZE:
                    GuiUtils.SimpleTextBox("Pe (km)", pe);
                    GuiUtils.SimpleTextBox("Ap (km)", ap);
                    break;

                case Operation.PERIAPSIS:
                    GuiUtils.SimpleTextBox("Pe (km)", pe);
                    break;

                case Operation.APOAPSIS:
                    GuiUtils.SimpleTextBox("Ap (km)", ap);
                    break;

                case Operation.INCLINATION:
                    GuiUtils.SimpleTextBox("Inc (deg)", inc);
                    break;

                case Operation.PLANE:
                    if (GUILayout.Button(planeMatchNode.ToString())) planeMatchNode = (Node)(((int)planeMatchNode + 1) % 2);
                    break;

                case Operation.TRANSFER:
                    break;

                case Operation.COURSE_CORRECTION:
                    break;

                case Operation.INTERPLANETARY_TRANSFER:
                    break;

                case Operation.LAMBERT:
                    GuiUtils.SimpleTextBox("Transfer time of flight: ", interceptInterval);
                    break;

            }

            double UT = vesselState.time;

            if (!anyNodeExists || createNode)
            {
                GuiUtils.SimpleTextBox("In (seconds): ", lead);

                GUILayout.BeginHorizontal();
                if (anyNodeExists)
                {
                    if (GUILayout.Button(timeReferenceSinceLast ? "After the last node" : "Now"))
                    {
                        timeReferenceSinceLast = !timeReferenceSinceLast;
                    }
                    GUILayout.Label(", after the next:");
                }
                else
                {
                    GUILayout.Label("After the next:");
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("◅")) timeReference = (TimeReference)(((int)timeReference - 1 + Enum.GetNames(typeof(TimeReference)).Length) % Enum.GetNames(typeof(TimeReference)).Length);
                GUILayout.Label(timeReference.ToString());
                if (GUILayout.Button("▻")) timeReference = (TimeReference)(((int)timeReference + 1 + Enum.GetNames(typeof(TimeReference)).Length) % Enum.GetNames(typeof(TimeReference)).Length);
                GUILayout.EndHorizontal();

                Orbit o = orbit;

                if (anyNodeExists && timeReferenceSinceLast)
                {
                    UT = vessel.patchedConicSolver.maneuverNodes.Last().UT;
                    o = vessel.patchedConicSolver.maneuverNodes.Last().nextPatch;
                }

                switch (timeReference)
                {
                    case TimeReference.NOW:
                        break;
                    case TimeReference.APOAPSIS:
                        UT = o.NextApoapsisTime(UT);
                        break;
                    case TimeReference.PERIAPSIS:
                        UT = o.NextPeriapsisTime(UT);
                        break;
                    case TimeReference.ASCENDING_NODE:
                        if (core.target.NormalTargetExists)
                        {
                            UT = o.TimeOfAscendingNode(core.target.Orbit, UT);
                        }
                        break;
                    case TimeReference.DESCENDING_NODE:
                        if (core.target.NormalTargetExists)
                        {
                            UT = o.TimeOfDescendingNode(core.target.Orbit, UT);
                        }
                        break;
                    case TimeReference.NINETY_BEFORE_AP:
                        UT = o.TimeOfTrueAnomaly(90, UT);
                        break;
                }

                UT += lead;
            }

            if (GUILayout.Button("Go"))
            {
                Vector3d dV = Vector3d.zero;

                Orbit o = vessel.GetPatchAtUT(UT);
                double rad = o.referenceBody.Radius;

                switch (operation)
                {
                    case Operation.CIRCULARIZE:
                        dV = OrbitalManeuverCalculator.DeltaVToCircularize(o, UT);
                        break;

                    case Operation.ELLIPTICIZE:
                        dV = OrbitalManeuverCalculator.DeltaVToEllipticize(o, UT, pe + rad, ap + rad);
                        break;

                    case Operation.PERIAPSIS:
                        dV = OrbitalManeuverCalculator.DeltaVToChangePeriapsis(o, UT, pe + rad);
                        break;

                    case Operation.APOAPSIS:
                        dV = OrbitalManeuverCalculator.DeltaVToChangeApoapsis(o, UT, ap + rad);
                        break;

                    case Operation.INCLINATION:
                        dV = OrbitalManeuverCalculator.DeltaVToChangeInclination(o, UT, inc);
                        break;

                    case Operation.PLANE:
                        if (planeMatchNode == Node.ASCENDING)
                        {
                            dV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesAscending(o, core.target.Orbit, UT, out UT);
                        }
                        else
                        {
                            dV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesDescending(o, core.target.Orbit, UT, out UT);
                        }
                        break;

                    case Operation.TRANSFER:
                        dV = OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(o, core.target.Orbit, vesselState.time, out UT);
                        break;

                    case Operation.COURSE_CORRECTION:
                        dV = OrbitalManeuverCalculator.DeltaVForCourseCorrection(o, UT, core.target.Orbit);
                        break;

                    case Operation.INTERPLANETARY_TRANSFER:
                        dV = OrbitalManeuverCalculator.DeltaVAndTimeForInterplanetaryTransferEjection(o, UT, core.target.Orbit, out UT);
                        break;

                    case Operation.LAMBERT:
                        dV = OrbitalManeuverCalculator.DeltaVToInterceptAtTime(o, UT, core.target.Orbit, UT + interceptInterval);
                        break;
                }

                vessel.PlaceManeuverNode(o, dV, UT);
            }

            if (GUILayout.Button("Remove ALL nodes"))
            {
                vessel.RemoveAllManeuverNodes();
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
            return "Maneuver Planner";
        }
    }
}
