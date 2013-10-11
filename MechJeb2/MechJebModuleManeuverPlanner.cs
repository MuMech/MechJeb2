using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleManeuverPlanner : DisplayModule
    {
        public MechJebModuleManeuverPlanner(MechJebCore core) : base(core)
        {
            references[Operation.CIRCULARIZE] = new TimeReference[] { TimeReference.APOAPSIS, TimeReference.PERIAPSIS, TimeReference.ALTITUDE, TimeReference.X_FROM_NOW };
            references[Operation.PERIAPSIS] = new TimeReference[] { TimeReference.X_FROM_NOW, TimeReference.APOAPSIS, TimeReference.PERIAPSIS };
            references[Operation.APOAPSIS] = new TimeReference[] { TimeReference.X_FROM_NOW, TimeReference.APOAPSIS, TimeReference.PERIAPSIS };
            references[Operation.ELLIPTICIZE] = new TimeReference[] { TimeReference.X_FROM_NOW };
            references[Operation.INCLINATION] = new TimeReference[] { TimeReference.EQ_ASCENDING, TimeReference.EQ_DESCENDING, TimeReference.X_FROM_NOW };
            references[Operation.PLANE] = new TimeReference[] { TimeReference.REL_ASCENDING, TimeReference.REL_DESCENDING };
            references[Operation.TRANSFER] = new TimeReference[] { TimeReference.COMPUTED };
            references[Operation.MOON_RETURN] = new TimeReference[] { TimeReference.COMPUTED };
            references[Operation.INTERPLANETARY_TRANSFER] = new TimeReference[] { TimeReference.COMPUTED };
            references[Operation.COURSE_CORRECTION] = new TimeReference[] { TimeReference.COMPUTED };
            references[Operation.LAMBERT] = new TimeReference[] { TimeReference.X_FROM_NOW };
            references[Operation.KILL_RELVEL] = new TimeReference[] { TimeReference.CLOSEST_APPROACH, TimeReference.X_FROM_NOW };
        }

        public enum Operation
        {
            CIRCULARIZE, PERIAPSIS, APOAPSIS, ELLIPTICIZE, INCLINATION, PLANE, TRANSFER, MOON_RETURN,
            INTERPLANETARY_TRANSFER, COURSE_CORRECTION, LAMBERT, KILL_RELVEL
        };
        static int numOperations = Enum.GetNames(typeof(Operation)).Length;
        string[] operationStrings = new string[]{"circularize", "change periapsis", "change apoapsis", "change both Pe and Ap",
                  "change inclination", "match planes with target", "Hohmann transfer to target", "return from a moon",
                  "transfer to another planet", "fine tune closest approach to target", "intercept target at chosen time", "match velocities with target"};

        public enum TimeReference
        {
            COMPUTED, X_FROM_NOW, APOAPSIS, PERIAPSIS, ALTITUDE, EQ_ASCENDING, EQ_DESCENDING,
            REL_ASCENDING, REL_DESCENDING, CLOSEST_APPROACH
        };
        static int numTimeReferences = Enum.GetNames(typeof(TimeReference)).Length;

        Operation operation = Operation.CIRCULARIZE;
        TimeReference timeReference = TimeReference.X_FROM_NOW;
        bool createNode = true;

        [Persistent(pass = (int)Pass.Global)]
        public EditableDoubleMult newPeA = new EditableDoubleMult(100000, 1000);
        [Persistent(pass = (int)Pass.Global)]
        public EditableDoubleMult newApA = new EditableDoubleMult(200000, 1000);
        [Persistent(pass = (int)Pass.Global)]
        public EditableDoubleMult courseCorrectFinalPeA = new EditableDoubleMult(200000, 1000);
        [Persistent(pass = (int)Pass.Global)]
        public EditableDoubleMult moonReturnAltitude = new EditableDoubleMult(100000, 1000);
        [Persistent(pass = (int)Pass.Global)]
        public EditableDouble newInc = 0;
        [Persistent(pass = (int)Pass.Global)]
        public EditableTime leadTime = 0;
        [Persistent(pass = (int)Pass.Global)]
        public EditableDoubleMult circularizeAltitude = new EditableDoubleMult(150000, 1000);
        [Persistent(pass = (int)Pass.Global)]
        public EditableTime interceptInterval = 3600;

        Dictionary<Operation, TimeReference[]> references = new Dictionary<Operation, TimeReference[]>();

        string errorMessage = "";

        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            List<ManeuverNode> maneuverNodes = GetManeuverNodes();
            bool anyNodeExists = (maneuverNodes.Count() > 0);

            if (anyNodeExists)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(createNode ? "Create a new" : "Change the last"))
                {
                    createNode = !createNode;
                }
                GUILayout.Label("maneuver node to:");
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label("Create a new maneuver node to:");
                createNode = true;
            }

            operation = (Operation)GuiUtils.ArrowSelector((int)operation, numOperations, operationStrings[(int)operation]);

            DoOperationParametersGUI();

            double UT = DoChooseTimeGUI();

            bool makingNode = false;
            bool executingNode = false;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Create node"))
            {
                makingNode = true;
                executingNode = false;
            }
            if (core.node != null && GUILayout.Button("Create and execute"))
            {
                makingNode = true;
                executingNode = true;
            }
            GUILayout.EndHorizontal();

            if (makingNode)
            {
                //handle updating an existing node by removing it and then re-creating it
                ManeuverNode removedNode = null;
                if (!createNode)
                {
                    removedNode = maneuverNodes.Last();
                    vessel.patchedConicSolver.RemoveManeuverNode(removedNode);
                }

                Orbit o = vessel.GetPatchAtUT(UT);
                if (CheckPreconditions(o, UT))
                {
                    MakeNodeForOperation(o, UT);
                    if (executingNode && core.node != null) core.node.ExecuteOneNode(this);
                }
                else if (!createNode)
                {
                    //Add removed node back in, since we decided not to create a new one.
                    vessel.patchedConicSolver.AddManeuverNode(removedNode.UT).OnGizmoUpdated(removedNode.DeltaV, removedNode.UT);
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

            if (core.node != null)
            {
                if (anyNodeExists && !core.node.enabled)
                {
                    if (GUILayout.Button("Execute next node"))
                    {
                        core.node.ExecuteOneNode(this);
                    }

                    if (vessel.patchedConicSolver.maneuverNodes.Count > 1)
                    {
                        if (GUILayout.Button("Execute all nodes"))
                        {
                            core.node.ExecuteAllNodes(this);
                        }
                    }
                }
                else if (core.node.enabled)
                {
                    if (GUILayout.Button("Abort node execution"))
                    {
                        core.node.Abort();
                    }
                }

                GUILayout.BeginHorizontal();
                core.node.autowarp = GUILayout.Toggle(core.node.autowarp, "Auto-warp", GUILayout.ExpandWidth(true));
                GUILayout.Label("Tolerance:", GUILayout.ExpandWidth(false));
                core.node.tolerance.text = GUILayout.TextField(core.node.tolerance.text, GUILayout.Width(35), GUILayout.ExpandWidth(false));
                GUILayout.Label("m/s", GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        List<ManeuverNode> GetManeuverNodes()
        {
            MechJebModuleLandingPredictions predictor = core.GetComputerModule<MechJebModuleLandingPredictions>();
            if (predictor == null) return vessel.patchedConicSolver.maneuverNodes;
            else return vessel.patchedConicSolver.maneuverNodes.Where(n => n != predictor.aerobrakeNode).ToList();
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
                    break;

                case Operation.TRANSFER:
                    GUILayout.Label("Schedule the burn at the next transfer window.");
                    break;

                case Operation.MOON_RETURN:
                    GuiUtils.SimpleTextBox("Approximate final periapsis:", moonReturnAltitude, "km");
                    GUILayout.Label("Schedule the burn at the next return window.");
                    break;

                case Operation.INTERPLANETARY_TRANSFER:
                    GUILayout.Label("Schedule the burn at the next transfer window.");
                    break;

                case Operation.COURSE_CORRECTION:
                    if (core.target.Target is CelestialBody) GuiUtils.SimpleTextBox("Approximate final periapsis", courseCorrectFinalPeA, "km");
                    GUILayout.Label("Schedule the burn to minimize the required ΔV.");
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

        double DoChooseTimeGUI() // function to maintain 'legacy' use
        {
        	string error;
        	return DoChooseTimeGUI(operation, TimeReference.COMPUTED, out error, true);
        }

        double DoChooseTimeGUI(Operation op, TimeReference timeRef, out string timeErrorMessage, bool InvolveGUI = true, double leadingTime = 0)
        {
            if (InvolveGUI) {
	            TimeReference[] allowedReferences = references[op];

	            int referenceIndex = 0;
	            if (allowedReferences.Contains(timeReference)) referenceIndex = Array.IndexOf(allowedReferences, timeReference);

	            referenceIndex = GuiUtils.ArrowSelector(referenceIndex, allowedReferences.Length, () =>
	                {
	                    switch (timeReference)
	                    {
	                        case TimeReference.APOAPSIS: GUILayout.Label("at the next apoapsis"); break;
	                        case TimeReference.CLOSEST_APPROACH: GUILayout.Label("at closest approach to target"); break;
	                        case TimeReference.EQ_ASCENDING: GUILayout.Label("at the equatorial AN"); break;
	                        case TimeReference.EQ_DESCENDING: GUILayout.Label("at the equatorial DN"); break;
	                        case TimeReference.PERIAPSIS: GUILayout.Label("at the next periapsis"); break;
	                        case TimeReference.REL_ASCENDING: GUILayout.Label("at the next AN with the target."); break;
	                        case TimeReference.REL_DESCENDING: GUILayout.Label("at the next DN with the target."); break;
	
	                        case TimeReference.X_FROM_NOW:
	                            leadTime.text = GUILayout.TextField(leadTime.text, GUILayout.Width(50));
	                            GUILayout.Label(" from now");
	                            break;
	
	                        case TimeReference.ALTITUDE:
	                            GuiUtils.SimpleTextBox("at an altitude of", circularizeAltitude, "km");
	                            break;
	                    }
	                });

	            timeReference = allowedReferences[referenceIndex];
            }

            timeErrorMessage = "";

            bool error = false;

            double UT = vesselState.time;

            Orbit o = orbit;

            List<ManeuverNode> maneuverNodes = GetManeuverNodes();
            if (maneuverNodes.Count() > 0)
            {
            	if (InvolveGUI) { GUILayout.Label("after the last maneuver node."); }
                ManeuverNode last = maneuverNodes.Last();
                UT = last.UT;
                o = last.nextPatch;
            }

            switch (InvolveGUI ? timeReference : timeRef)
            {
                case TimeReference.X_FROM_NOW:
            		UT += (InvolveGUI ? leadTime.val : leadingTime);
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

                case TimeReference.ALTITUDE:
                    if (circularizeAltitude > o.PeA && (circularizeAltitude < o.ApA || o.eccentricity >= 1))
                    {
                        UT = o.NextTimeOfRadius(UT, o.referenceBody.Radius + circularizeAltitude);
                    }
                    else
                    {
                        error = true;
                        timeErrorMessage = "Warning: can't circularize at this altitude, since current orbit does not reach it.";
                    }
                    break;

                case TimeReference.EQ_ASCENDING:
                    if (o.AscendingNodeEquatorialExists())
                    {
                        UT = o.TimeOfAscendingNodeEquatorial(UT);
                    }
                    else
                    {
                        error = true;
                        timeErrorMessage = "Warning: equatorial ascending node doesn't exist.";
                    }
                    break;

                case TimeReference.EQ_DESCENDING:
                    if (o.DescendingNodeEquatorialExists())
                    {
                        UT = o.TimeOfDescendingNodeEquatorial(UT);
                    }
                    else
                    {
                        error = true;
                        timeErrorMessage = "Warning: equatorial descending node doesn't exist.";
                    }
                    break;

            }

            if (op == Operation.COURSE_CORRECTION && core.target.NormalTargetExists)
            {
                Orbit correctionPatch = o;
                while (correctionPatch != null)
                {
                    if (correctionPatch.referenceBody == core.target.Orbit.referenceBody)
                    {
                        o = correctionPatch;
                        UT = correctionPatch.StartUT;
                        break;
                    }
                    correctionPatch = vessel.GetNextPatch(correctionPatch);
                }
            }

            if (error && InvolveGUI)
            {
                GUIStyle s = new GUIStyle(GUI.skin.label);
                s.normal.textColor = Color.yellow;
                GUILayout.Label(timeErrorMessage, s);
            }

            return UT;
        }

        bool CheckPreconditions(Orbit o, double UT)
        {
        	return CheckPreconditions(o, UT, operation, newPeA, newApA, newInc);
        }
        
        bool CheckPreconditions(Orbit o, double UT, Operation op, double newPeA, double newApA, double newInc)
        {
            errorMessage = "";
            bool error = false;

            string burnAltitude = MuUtils.ToSI(o.Radius(UT) - o.referenceBody.Radius) + "m";

            switch (op)
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
                    else if (timeReference == TimeReference.REL_ASCENDING)
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
                    else if (o.referenceBody.orbit.RelativeInclination(core.target.Orbit) > 30)
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

                case Operation.MOON_RETURN:
                    if (o.referenceBody.referenceBody == null)
                    {
                        error = true;
                        errorMessage = o.referenceBody.theName + " is not orbiting another body you could return to.";
                    }
                    else if (o.eccentricity > 0.2)
                    {
                        errorMessage = "Warning: Recommend starting moon returns from a near-circular orbit (eccentricity < 0.2). Planned return is starting from an orbit with eccentricity " + o.eccentricity.ToString("F2") + " and so may not be accurate.";
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
        	MakeNodeForOperation(o, UT, operation, newPeA, newApA, newInc, courseCorrectFinalPeA, moonReturnAltitude, interceptInterval);
        }
        
        void MakeNodeForOperation(Orbit o, double UT, Operation op, double newPeA, double newApA, double newInc, double courseCorrectFinalPeA, double moonReturnAltitude, double interceptInterval)
        {
            Vector3d dV = Vector3d.zero;

            double bodyRadius = o.referenceBody.Radius;
            
//            print(newPeA + " - " + this.newPeA + "\n" + 
//                  newApA + " - " + this.newApA + "\n" + 
//                  newInc + " - " + this.newInc + "\n" + 
//                  courseCorrectFinalPeA + " - " + this.courseCorrectFinalPeA + "\n" + 
//                  moonReturnAltitude + " - " + this.moonReturnAltitude + "\n" + 
//                  interceptInterval + " - " + this.interceptInterval);

            switch (op)
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
                    if (timeReference == TimeReference.REL_ASCENDING)
                    {
                        dV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesAscending(o, core.target.Orbit, UT, out UT);
                    }
                    else
                    {
                        dV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesDescending(o, core.target.Orbit, UT, out UT);
                    }
                    break;

                case Operation.TRANSFER:
                    dV = OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(o, core.target.Orbit, UT, out UT);
                    break;

                case Operation.MOON_RETURN:
                    dV = OrbitalManeuverCalculator.DeltaVAndTimeForMoonReturnEjection(o, UT, o.referenceBody.referenceBody.Radius + moonReturnAltitude, out UT);
                    break;

                case Operation.COURSE_CORRECTION:
                    CelestialBody targetBody = core.target.Target as CelestialBody;
                    if (targetBody != null)
                    {
                        dV = OrbitalManeuverCalculator.DeltaVAndTimeForCheapestCourseCorrection(o, UT, core.target.Orbit, targetBody, targetBody.Radius + courseCorrectFinalPeA, out UT);
                    }
                    else
                    {
                        dV = OrbitalManeuverCalculator.DeltaVAndTimeForCheapestCourseCorrection(o, UT, core.target.Orbit, out UT);
                    }
                    break;

                case Operation.INTERPLANETARY_TRANSFER:
                    dV = OrbitalManeuverCalculator.DeltaVAndTimeForInterplanetaryTransferEjection(o, UT, core.target.Orbit, true, out UT);
                    break;

                case Operation.LAMBERT:
                    dV = OrbitalManeuverCalculator.DeltaVToInterceptAtTime(o, UT, core.target.Orbit, UT + interceptInterval);
                    break;

                case Operation.KILL_RELVEL:
                    dV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(o, UT, core.target.Orbit);
                    break;
            }

            vessel.PlaceManeuverNode(o, dV, UT);
        }

        public struct NodePlanningResult
        {
        	public bool Success;
        	public string Error;
        	public string TimeError;
        }
        
        public NodePlanningResult PlanNode(Operation op, TimeReference timeRef, double leadingTime, double newPeA, double newApA, double newInc, double courseCorrectFinalPeA, double moonReturnAltitude, double interceptInterval, bool planLast = false)
        {
        	NodePlanningResult result = new MechJebModuleManeuverPlanner.NodePlanningResult();
        	result.Success = true;
        	var UT = DoChooseTimeGUI(op, timeRef, out result.TimeError, false, leadingTime);
        	if (result.TimeError == "" && CheckPreconditions(vessel.GetPatchAtUT(UT), UT, op, newPeA, newApA, newInc)) {
       			MakeNodeForOperation(vessel.GetPatchAtUT(UT), UT, op, newPeA, newApA, newInc, courseCorrectFinalPeA, moonReturnAltitude, interceptInterval);
        	}
        	else {
        		result.Success = false;
        	}
       		result.Error = errorMessage;
       		errorMessage = "";
       		result.Success = (result.Success == true && result.Error == "" && result.TimeError == "");
        	return result;
        }
        
        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(300), GUILayout.Height(150) };
        }

        public override string GetName()
        {
            return "Maneuver Planner";
        }
    }
}
