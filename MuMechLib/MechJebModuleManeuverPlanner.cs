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

        enum Operation
        {
            CIRCULARIZE, PERIAPSIS, APOAPSIS, ELLIPTICIZE, INCLINATION, PLANE, TRANSFER,
            INTERPLANETARY_TRANSFER, COURSE_CORRECTION, LAMBERT, KILL_RELVEL
        };
        static int numOperations = Enum.GetNames(typeof(Operation)).Length;
        string[] operationStrings = new string[]{"circularize", "change periapsis", "change apoapsis", "change both Pe and Ap",
                  "change inclination", "match planes with target", "Hohmann transfer to target", 
                  "transfer to another planet", "fine tune closest approach to target", "intercept target at chosen time", "match velocities with target"};

        enum TimeReference { NOW, APOAPSIS, PERIAPSIS, CLOSEST_APPROACH };
        static int numTimeReferences = Enum.GetNames(typeof(TimeReference)).Length;
        string[] timeReferenceStrings = new string[] { "", "the next apoapsis after", "the next periapsis after", "the closest approach to the target after"};

        Operation operation = Operation.CIRCULARIZE;
        TimeReference timeReference = TimeReference.NOW;
        bool timeReferenceSinceLast = true;
        bool createNode = true;

        enum Node { ASCENDING, DESCENDING };
        string[] nodeStrings = new string[] { "at the next ascending node after", "at the next descending node after" };
        Node planeMatchNode;

        EditableDouble newPeA = new EditableDouble(0, 1000);
        EditableDouble newApA = new EditableDouble(0, 1000);
        EditableDouble newInc = new EditableDouble(0);
        EditableTime leadTime = new EditableTime(0);
        EditableTime interceptInterval = new EditableTime(3600);

        string errorMessage = "";

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
            if (GUILayout.Button("◀", GUILayout.ExpandWidth(false))) operation = (Operation)(((int)operation - 1 + numOperations) % numOperations);
            GUILayout.Label(operationStrings[(int)operation]);
            if (GUILayout.Button("▶", GUILayout.ExpandWidth(false))) operation = (Operation)(((int)operation + 1 + numOperations) % numOperations);
            GUILayout.EndHorizontal();

            DoOperationParametersGUI();

            double UT = vesselState.time; ;

            if (!anyNodeExists || createNode)
            {
                UT = DoChooseTimeGUI();
            }

            if (GUILayout.Button("Go"))
            {
                Orbit o = vessel.GetPatchAtUT(UT);
                if (CheckPreconditions(o, UT))
                {
                    MakeNodeForOperation(o, UT);
                }
            }

            if (errorMessage.Length > 0)
            {
                GUIStyle s = new GUIStyle(GUI.skin.label);
                s.normal.textColor = Color.yellow;
                GUILayout.Label(errorMessage, s);
            }

            if (GUILayout.Button("Remove ALL nodes"))
            {
                vessel.RemoveAllManeuverNodes();
            }

            if (anyNodeExists && !core.node.enabled)
            {
                if (GUILayout.Button("Execute next node"))
                {
                    core.node.ExecuteOneNode();
                }

                if (vessel.patchedConicSolver.maneuverNodes.Count > 1)
                {
                    if (GUILayout.Button("Execute all nodes"))
                    {
                        core.node.ExecuteAllNodes();
                    }
                }
            }
            else if (core.node.enabled)
            {
                if (GUILayout.Button("ABORT"))
                {
                    core.node.enabled = false;
                }
            }

            GUILayout.EndVertical();



            base.FlightWindowGUI(windowID);
        }

        void DoOperationParametersGUI()
        {
            switch (operation)
            {
                case Operation.CIRCULARIZE:
                    GUILayout.Label("Schedule the burn");
                    break;

                case Operation.ELLIPTICIZE:
                    GuiUtils.SimpleTextBox("New periapsis:", newPeA, "km");
                    GuiUtils.SimpleTextBox("New apoapsis:", newApA, "km");
                    GUILayout.Label("Schedule the burn");
                    break;

                case Operation.PERIAPSIS:
                    GuiUtils.SimpleTextBox("New periapsis:", newPeA, "km");
                    GUILayout.Label("Schedule the burn");
                    break;

                case Operation.APOAPSIS:
                    GuiUtils.SimpleTextBox("New apoapsis:", newApA, "km");
                    GUILayout.Label("Schedule the burn");
                    break;

                case Operation.INCLINATION:
                    GuiUtils.SimpleTextBox("New inclination:", newInc, "º");
                    GUILayout.Label("Schedule the burn");
                    break;

                case Operation.PLANE:
                    GUILayout.Label("Schedule the burn");
                    if (GUILayout.Button(nodeStrings[(int)planeMatchNode])) planeMatchNode = (Node)(((int)planeMatchNode + 1) % 2);
                    break;

                case Operation.TRANSFER:
                    GUILayout.Label("Schedule the burn at the next transfer window starting");
                    break;

                case Operation.INTERPLANETARY_TRANSFER:
                    GUILayout.Label("Schedule the burn at the next transfer window starting");
                    break;

                case Operation.COURSE_CORRECTION:
                    GUILayout.Label("Schedule the burn");
                    break;

                case Operation.LAMBERT:
                    GuiUtils.SimpleTextBox("Time after burn to intercept target:", interceptInterval);
                    GUILayout.Label("Schedule the burn");
                    break;

                case Operation.KILL_RELVEL:
                    GUILayout.Label("Schedule the burn");
                    break;
            }
        }

        double DoChooseTimeGUI()
        {
            double UT = vesselState.time;

            bool anyNodeExists = (vessel.patchedConicSolver.maneuverNodes.Count > 0);

            GuiUtils.SimpleTextBox("", leadTime, "after");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("◀", GUILayout.ExpandWidth(false))) timeReference = (TimeReference)(((int)timeReference - 1 + numTimeReferences) % numTimeReferences);
            GUILayout.Label(timeReferenceStrings[(int)timeReference]);
            if (GUILayout.Button("▶", GUILayout.ExpandWidth(false))) timeReference = (TimeReference)(((int)timeReference + 1 + numTimeReferences) % numTimeReferences);
            GUILayout.EndHorizontal();

            bool error = false;
            string timeErrorMessage = "";

            if (anyNodeExists)
            {
                if (GUILayout.Button(timeReferenceSinceLast ? "the last maneuver node." : "now."))
                {
                    timeReferenceSinceLast = !timeReferenceSinceLast;
                }
            }
            else
            {
                GUILayout.Label("now.");
            }

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
                    if (o.eccentricity < 1)
                    {
                        UT = o.NextApoapsisTime(UT);
                    }
                    else
                    {
                        error = true;
                        timeErrorMessage = "Warning: orbit is hyperbolic, so apoapsis doesn't exist.";
                    }
                    break;
                case TimeReference.PERIAPSIS:
                    UT = o.NextPeriapsisTime(UT);
                    break;
                case TimeReference.CLOSEST_APPROACH:
                    if (core.target.NormalTargetExists)
                    {
                        UT = o.NextClosestApproachTime(core.target.Orbit, UT);
                    }
                    else
                    {
                        error = true;
                        timeErrorMessage = "Warning: no target selected.";
                    }
                    break;
            }

            UT += leadTime;

            if (error)
            {
                GUIStyle s = new GUIStyle(GUI.skin.label);
                s.normal.textColor = Color.yellow;
                GUILayout.Label(timeErrorMessage, s);
            }

            return UT;
        }


        bool CheckPreconditions(Orbit o, double UT)
        {
            errorMessage = "";
            bool error = false;
            
            string burnAltitude = MuUtils.ToSI(o.Radius(UT) - o.referenceBody.Radius) + "m";

            switch (operation)
            {
                case Operation.CIRCULARIZE:
                    break;

                case Operation.ELLIPTICIZE:
                    if (o.referenceBody.Radius + newPeA > o.Radius(UT))
                    {
                        error = true;
                        errorMessage = "new periapsis cannot be higher than the altitude of the burn (" + burnAltitude + ")";
                    }
                    else if (o.referenceBody.Radius + newApA < o.Radius(UT))
                    {
                        error = true;
                        errorMessage = "new apoapsis cannot be lower than the altitude of the burn (" + burnAltitude + ")";
                    }
                    else if (newPeA < -o.referenceBody.Radius)
                    {
                        error = true;
                        errorMessage = "new periapsis cannot be lower than minus the radius of " + o.referenceBody.theName + "(-" + MuUtils.ToSI(o.referenceBody.Radius, 3) + "m)";
                    }
                    break;

                case Operation.PERIAPSIS:
                    if (o.referenceBody.Radius + newPeA > o.Radius(UT))
                    {
                        error = true;
                        errorMessage = "new periapsis cannot be higher than the altitude of the burn (" + burnAltitude + ")";
                    }
                    else if (newPeA < -o.referenceBody.Radius)
                    {
                        error = true;
                        errorMessage = "new periapsis cannot be lower than minus the radius of " + o.referenceBody.theName + "(-" + MuUtils.ToSI(o.referenceBody.Radius, 3) + "m)";
                    }
                    break;

                case Operation.APOAPSIS:
                    if (o.referenceBody.Radius + newApA < o.Radius(UT))
                    {
                        error = true;
                        errorMessage = "new apoapsis cannot be lower than the altitude of the burn (" + burnAltitude + ")";
                    }
                    break;

                case Operation.INCLINATION:
                    break;

                case Operation.PLANE:
                    if (!core.target.NormalTargetExists)
                    {
                        error = true;
                        errorMessage = "must select a target to match planes with.";
                    }
                    else if (o.referenceBody != core.target.Orbit.referenceBody)
                    {
                        error = true;
                        errorMessage = "can only match planes with an object in the same sphere of influence.";
                    }
                    else if (planeMatchNode == Node.ASCENDING)
                    {
                        if (!o.AscendingNodeExists(core.target.Orbit))
                        {
                            error = true;
                            errorMessage = "ascending node with target doesn't exist.";
                        }
                    }
                    else
                    {
                        if (!o.DescendingNodeExists(core.target.Orbit))
                        {
                            error = true;
                            errorMessage = "descending node with target doesn't exist.";
                        }
                    }
                    break;

                case Operation.TRANSFER:
                    if (!core.target.NormalTargetExists)
                    {
                        error = true;
                        errorMessage = "must select a target for the Hohmann transfer.";
                    }
                    else if (o.referenceBody != core.target.Orbit.referenceBody)
                    {
                        error = true;
                        errorMessage = "target for Hohmann transfer must be in the same sphere of influence.";
                    }
                    else if (o.eccentricity > 1)
                    {
                        error = true;
                        errorMessage = "starting orbit for Hohmann transfer must not be hyperbolic.";
                    }
                    else if (core.target.Orbit.eccentricity > 1)
                    {
                        error = true;
                        errorMessage = "target orbit for Hohmann transfer must not be hyperbolic.";
                    }
                    else if (o.RelativeInclination(core.target.Orbit) > 30 && o.RelativeInclination(core.target.Orbit) < 150)
                    {
                        errorMessage = "Warning: target's orbital plane is at a " + o.RelativeInclination(core.target.Orbit).ToString("F0") + "º angle to starting orbit's plane (recommend at most 30º). Planned transfer may not intercept target properly.";
                    }
                    else if (o.eccentricity > 0.2)
                    {
                        errorMessage = "Warning: Recommend starting Hohmann transfers from a near-circular orbit (eccentricity < 0.2). Planned transfer is starting from an orbit with eccentricity " + o.eccentricity.ToString("F2") + " and so may not intercept target properly.";
                    }
                    break;

                case Operation.COURSE_CORRECTION:
                    if (!core.target.NormalTargetExists)
                    {
                        error = true;
                        errorMessage = "must select a target for the course correction.";
                    }
                    else if (o.referenceBody != core.target.Orbit.referenceBody)
                    {
                        error = true;
                        errorMessage = "target for course correction must be in the same sphere of influence";
                    }
                    else if (o.NextClosestApproachTime(core.target.Orbit, UT) < UT + 1 ||
                        o.NextClosestApproachDistance(core.target.Orbit, UT) > core.target.Orbit.semiMajorAxis * 0.2)
                    {
                        errorMessage = "Warning: orbit before course correction doesn't seem to approach target very closely. Planned course correction may be extreme. Recommend plotting an approximate intercept orbit and then plotting a course correction.";
                    }
                    break;

                case Operation.INTERPLANETARY_TRANSFER:
                    if (!core.target.NormalTargetExists)
                    {
                        error = true;
                        errorMessage = "must select a target for the interplanetary transfer.";
                    }
                    else if (o.referenceBody.referenceBody == null)
                    {
                        error = true;
                        errorMessage = "doesn't make sense to plot an interplanetary transfer from an orbit around " + o.referenceBody.theName + ".";
                    }
                    else if (o.referenceBody.referenceBody != core.target.Orbit.referenceBody)
                    {
                        error = true;
                        if (o.referenceBody == core.target.Orbit.referenceBody) errorMessage = "use regular Hohmann transfer function to intercept another body orbiting " + o.referenceBody.theName + ".";
                        else errorMessage = "an interplanetary transfer from within " + o.referenceBody.theName + "'s sphere of influence must target a body that orbits " + o.referenceBody.theName + "'s parent, " + o.referenceBody.referenceBody.theName + ".";
                    }
                    else if(o.referenceBody.orbit.RelativeInclination(core.target.Orbit) > 30) 
                    {
                        errorMessage = "Warning: target's orbital plane is at a " + o.RelativeInclination(core.target.Orbit).ToString("F0") + "º angle to " + o.referenceBody.theName + "'s orbital plane (recommend at most 30º). Planned interplanetary transfer may not intercept target properly."; 
                    }
                    else
                    {
                        double relativeInclination = Vector3d.Angle(o.SwappedOrbitNormal(), o.referenceBody.orbit.SwappedOrbitNormal());
                        if (relativeInclination > 10)
                        {
                            errorMessage = "Warning: Recommend starting interplanetary transfers from " + o.referenceBody.theName + " from an orbit in the same plane as " + o.referenceBody.theName + "'s orbit around " + o.referenceBody.referenceBody.theName + ". Starting orbit around " + o.referenceBody.theName + " is inclined " + relativeInclination.ToString("F1") + "º with respect to " + o.referenceBody.theName + "'s orbit around " + o.referenceBody.referenceBody.theName + " (recommend < 10º). Planned transfer may not intercept target properly.";
                        }
                        else if (o.eccentricity > 0.2)
                        {
                            errorMessage = "Warning: Recommend starting interplanetary transfers from a near-circular orbit (eccentricity < 0.2). Planned transfer is starting from an orbit with eccentricity " + o.eccentricity.ToString("F2") + " and so may not intercept target properly.";
                        }
                    }
                    break;

                case Operation.LAMBERT:
                    if (!core.target.NormalTargetExists)
                    {
                        error = true;
                        errorMessage = "must select a target to intercept.";
                    }
                    else if (o.referenceBody != core.target.Orbit.referenceBody)
                    {
                        error = true;
                        errorMessage = "target must be in the same sphere of influence.";
                    }
                    break;

                case Operation.KILL_RELVEL:
                    if (!core.target.NormalTargetExists)
                    {
                        error = true;
                        errorMessage = "must select a target to match velocities with.";
                    }
                    else if (o.referenceBody != core.target.Orbit.referenceBody)
                    {
                        error = true;
                        errorMessage = "target must be in the same sphere of influence.";
                    }
                    break;
            }

            if (error) errorMessage = "Couldn't plot maneuver: " + errorMessage;

            return !error;
        }


        void MakeNodeForOperation(Orbit o, double UT)
        {
            Vector3d dV = Vector3d.zero;

            double bodyRadius = o.referenceBody.Radius;

            switch (operation)
            {
                case Operation.CIRCULARIZE:
                    dV = OrbitalManeuverCalculator.DeltaVToCircularize(o, UT);
                    break;

                case Operation.ELLIPTICIZE:
                    dV = OrbitalManeuverCalculator.DeltaVToEllipticize(o, UT, newPeA + bodyRadius, newApA + bodyRadius);
                    break;

                case Operation.PERIAPSIS:
                    dV = OrbitalManeuverCalculator.DeltaVToChangePeriapsis(o, UT, newPeA + bodyRadius);
                    break;

                case Operation.APOAPSIS:
                    dV = OrbitalManeuverCalculator.DeltaVToChangeApoapsis(o, UT, newApA + bodyRadius);
                    break;

                case Operation.INCLINATION:
                    dV = OrbitalManeuverCalculator.DeltaVToChangeInclination(o, UT, newInc);
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
                    
                case Operation.KILL_RELVEL:
                    dV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(o, UT, core.target.Orbit);
                    break;
            }


            //handle updating an existing node by removing it and then re-creating it
            if (!createNode) vessel.patchedConicSolver.RemoveManeuverNode(vessel.patchedConicSolver.maneuverNodes.Last());

            vessel.PlaceManeuverNode(o, dV, UT);
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
