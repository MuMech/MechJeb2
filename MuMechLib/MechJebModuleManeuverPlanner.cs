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

        enum Operation { CIRCULARIZE, ELLIPTICIZE, PERIAPSIS, APOAPSIS, INCLINATION, PLANE, TRANSFER, COURSE_CORRECTION, INTERPLANETARY_TRANSFER };
        enum TimeReference { NOW, APOAPSIS, PERIAPSIS, ASCENDING_NODE, DESCENDING_NODE };

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

                case Operation.TRANSFER:
                    break;

                case Operation.COURSE_CORRECTION:
                    break;

                case Operation.INTERPLANETARY_TRANSFER:
                    break;
            }

            double UT = vesselState.time;

            if (!anyNodeExists || createNode)
            {
                GuiUtils.SimpleTextBox("In (seconds): ", lead, 1);

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

                if (anyNodeExists && timeReferenceSinceLast)
                {
                    UT = vessel.patchedConicSolver.maneuverNodes.Last().UT;
                }

                switch (timeReference)
                {
                    case TimeReference.NOW:
                        break;
                    case TimeReference.APOAPSIS:
                        UT = orbit.NextApoapsisTime(UT);
                        break;
                    case TimeReference.PERIAPSIS:
                        UT = orbit.NextPeriapsisTime(UT);
                        break;
                    case TimeReference.ASCENDING_NODE:
                        if (Target.Exists())
                        {
                            UT = orbit.TimeOfAscendingNode(Target.Orbit(), UT);
                        }
                        break;
                    case TimeReference.DESCENDING_NODE:
                        if (Target.Exists())
                        {
                            UT = orbit.TimeOfDescendingNode(Target.Orbit(), UT);
                        }
                        break;
                }

                UT += lead;
            }

            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                double initialT = vesselState.time;
                double finalT = vesselState.time + 100;
                Vector3d initalRelPos = orbit.SwappedRelativePositionAtUT(initialT);
                Vector3d finalRelPos = orbit.SwappedRelativePositionAtUT(finalT);
                Vector3d knownInitialVel = orbit.SwappedOrbitalVelocityAtUT(initialT);
                Vector3d knownFinalVel = orbit.SwappedOrbitalVelocityAtUT(finalT);

                Vector3d computedInitialVel, computedFinalVel;
                LambertSolver.Solve(initalRelPos, initialT, finalRelPos, finalT, orbit.referenceBody, 0.01,
                    out computedInitialVel, out computedFinalVel);
                Debug.Log("--");
                Debug.Log("known initial velocity = " + knownInitialVel);
                Debug.Log("known final velocity = " + knownFinalVel);
            }


            if (Input.GetKeyDown(KeyCode.Alpha8))
            {
                Debug.Log("Maneuver nodes:");
                foreach (ManeuverNode mn in vessel.patchedConicSolver.maneuverNodes)
                {
                    Debug.Log(mn.ToString() + " at " + mn.UT + " - patch PeA = " + mn.patch.PeA + "; nextPatch PeA = " + (mn.nextPatch == null ? -66 : mn.nextPatch.PeA));
                }
            }


            if (GUILayout.Button("Go"))
            {
                Vector3d dV = Vector3d.zero;

                Orbit o = vessel.GetPatchAtUT(UT);
                double rad = o.referenceBody.Radius;

                Debug.Log("o.PeA = " + o.PeA);

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
                            dV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesAscending(o, FlightGlobals.fetch.VesselTarget.GetOrbit(), UT, out UT);
                        }
                        else
                        {
                            dV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesDescending(o, FlightGlobals.fetch.VesselTarget.GetOrbit(), UT, out UT);
                        }
                        break;

                    case Operation.TRANSFER:
                        dV = OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(o, FlightGlobals.fetch.VesselTarget.GetOrbit(), vesselState.time, out UT);
                        break;

                    case Operation.COURSE_CORRECTION:
                        dV = OrbitalManeuverCalculator.DeltaVForCourseCorrection(o, UT, FlightGlobals.fetch.VesselTarget.GetOrbit());
                        break;

                    case Operation.INTERPLANETARY_TRANSFER:
                        dV = OrbitalManeuverCalculator.DeltaVAndTimeForInterplanetaryTransferEjection(o, vesselState.time, FlightGlobals.fetch.VesselTarget.GetOrbit(), out UT);
                        break;
                }

                vessel.PlaceManeuverNode(o, dV, UT);
            }

            if (GUILayout.Button("Remove ALL nodes"))
            {
                while (vessel.patchedConicSolver.maneuverNodes.Count > 0)
                {
                    vessel.patchedConicSolver.RemoveManeuverNode(vessel.patchedConicSolver.maneuverNodes.Last());
                }
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
