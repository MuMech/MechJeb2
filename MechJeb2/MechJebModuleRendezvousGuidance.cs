using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleRendezvousGuidance : DisplayModule
    {
        public MechJebModuleRendezvousGuidance(MechJebCore core) : base(core) { }
        
        EditableDoubleMult phasingOrbitAltitude = new EditableDoubleMult(200000, 1000);

        protected override void WindowGUI(int windowID)
        {
            if (!core.target.NormalTargetExists)
            {
                GUILayout.Label("Select a target to rendezvous with.");
                base.WindowGUI(windowID);
                return;
            }

            if (core.target.TargetOrbit.referenceBody != orbit.referenceBody)
            {
                GUILayout.Label("Rendezvous target must be in the same sphere of influence.");
                base.WindowGUI(windowID);
                return;
            }

            GUILayout.BeginVertical();

            //Information readouts:

            GuiUtils.SimpleLabel("Rendezvous target", core.target.Name);

            const double leadTime = 30;
            GuiUtils.SimpleLabel("Target orbit", MuUtils.ToSI(core.target.TargetOrbit.PeA, 3) + "m x " + MuUtils.ToSI(core.target.TargetOrbit.ApA, 3) + "m");
            GuiUtils.SimpleLabel("Current orbit", MuUtils.ToSI(orbit.PeA, 3) + "m x " + MuUtils.ToSI(orbit.ApA, 3) + "m");
            GuiUtils.SimpleLabel("Relative inclination", orbit.RelativeInclination(core.target.TargetOrbit).ToString("F2") + "º");

            double closestApproachTime = orbit.NextClosestApproachTime(core.target.TargetOrbit, vesselState.time);
            GuiUtils.SimpleLabel("Time until closest approach", GuiUtils.TimeToDHMS(closestApproachTime - vesselState.time));
            GuiUtils.SimpleLabel("Separation at closest approach", MuUtils.ToSI(orbit.Separation(core.target.TargetOrbit, closestApproachTime), 0) + "m");


            //Maneuver planning buttons:

            if (GUILayout.Button("Align Planes"))
            {
                double UT;
                Vector3d dV;
                if (orbit.AscendingNodeExists(core.target.TargetOrbit))
                {
                    dV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesAscending(orbit, core.target.TargetOrbit, vesselState.time, out UT);
                }
                else
                {
                    dV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesDescending(orbit, core.target.TargetOrbit, vesselState.time, out UT);
                }
                vessel.RemoveAllManeuverNodes();
                vessel.PlaceManeuverNode(orbit, dV, UT);
            }


            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Establish new orbit at"))
            {
                double phasingOrbitRadius = phasingOrbitAltitude + mainBody.Radius;

                vessel.RemoveAllManeuverNodes();
                if (orbit.ApR < phasingOrbitRadius)
                {
                    double UT1 = vesselState.time + leadTime;
                    Vector3d dV1 = OrbitalManeuverCalculator.DeltaVToChangeApoapsis(orbit, UT1, phasingOrbitRadius);
                    vessel.PlaceManeuverNode(orbit, dV1, UT1);
                    Orbit transferOrbit = vessel.patchedConicSolver.maneuverNodes[0].nextPatch;
                    double UT2 = transferOrbit.NextApoapsisTime(UT1);
                    Vector3d dV2 = OrbitalManeuverCalculator.DeltaVToCircularize(transferOrbit, UT2);
                    vessel.PlaceManeuverNode(transferOrbit, dV2, UT2);
                }
                else if (orbit.PeR > phasingOrbitRadius)
                {
                    double UT1 = vesselState.time + leadTime;
                    Vector3d dV1 = OrbitalManeuverCalculator.DeltaVToChangePeriapsis(orbit, UT1, phasingOrbitRadius);
                    vessel.PlaceManeuverNode(orbit, dV1, UT1);
                    Orbit transferOrbit = vessel.patchedConicSolver.maneuverNodes[0].nextPatch;
                    double UT2 = transferOrbit.NextPeriapsisTime(UT1);
                    Vector3d dV2 = OrbitalManeuverCalculator.DeltaVToCircularize(transferOrbit, UT2);
                    vessel.PlaceManeuverNode(transferOrbit, dV2, UT2);
                }
                else
                {
                    double UT = orbit.NextTimeOfRadius(vesselState.time, phasingOrbitRadius);
                    Vector3d dV = OrbitalManeuverCalculator.DeltaVToCircularize(orbit, UT);
                    vessel.PlaceManeuverNode(orbit, dV, UT);
                }
            }
            phasingOrbitAltitude.text = GUILayout.TextField(phasingOrbitAltitude.text, GUILayout.Width(70));
            GUILayout.Label("km", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Intercept with Hohmann transfer"))
            {
                double UT;
                Vector3d dV = OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(orbit, core.target.TargetOrbit, vesselState.time, out UT);
                vessel.RemoveAllManeuverNodes();
                vessel.PlaceManeuverNode(orbit, dV, UT);
            }

            if (GUILayout.Button("Match velocities at closest approach"))
            {
                double UT = closestApproachTime;
                Vector3d dV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(orbit, UT, core.target.TargetOrbit);
                vessel.RemoveAllManeuverNodes();
                vessel.PlaceManeuverNode(orbit, dV, UT);
            }

            if (GUILayout.Button("Get closer"))
            {
                double UT = vesselState.time;
                double interceptUT = UT + 100;
                Vector3d dV = OrbitalManeuverCalculator.DeltaVToInterceptAtTime(orbit, UT, core.target.TargetOrbit, interceptUT, 10);
                vessel.RemoveAllManeuverNodes();
                vessel.PlaceManeuverNode(orbit, dV, UT);
            }

            if (core.node != null)
            {
                if (vessel.patchedConicSolver.maneuverNodes.Any() && !core.node.enabled)
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
                if (GUILayout.Button("+", GUILayout.ExpandWidth(false)))
                {
                    core.node.tolerance.val += 0.1;
                }
                if (GUILayout.Button("-", GUILayout.ExpandWidth(false)))
                {
                    core.node.tolerance.val -= core.node.tolerance.val > 0.1 ? 0.1 : 0.0;
                }
                if (GUILayout.Button("R", GUILayout.ExpandWidth(false)))
                {
                    core.node.tolerance.val = 0.1;
                }
                GUILayout.Label("m/s", GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(300), GUILayout.Height(150) };
        }

        public override string GetName()
        {
            return "Rendezvous Planner";
        }

        public override bool IsSpaceCenterUpgradeUnlocked()
        {
            return vessel.patchedConicsUnlocked();
        }
    }
}
