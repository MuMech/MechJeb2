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

            if (core.target.Orbit.referenceBody != orbit.referenceBody)
            {
                GUILayout.Label("Rendezvous target must be in the same sphere of influence.");
                base.WindowGUI(windowID);
                return;
            }

            GUILayout.BeginVertical();

            //Information readouts:

            GuiUtils.SimpleLabel("Rendezvous target", core.target.Name);

            double leadTime = 30;
            GuiUtils.SimpleLabel("Target orbit", MuUtils.ToSI(core.target.Orbit.PeA, 3) + "m x " + MuUtils.ToSI(core.target.Orbit.ApA, 3) + "m");
            GuiUtils.SimpleLabel("Current orbit", MuUtils.ToSI(orbit.PeA, 3) + "m x " + MuUtils.ToSI(orbit.ApA, 3) + "m");
            GuiUtils.SimpleLabel("Relative inclination", orbit.RelativeInclination(core.target.Orbit).ToString("F2") + "º");

            double closestApproachTime = orbit.NextClosestApproachTime(core.target.Orbit, vesselState.time);
            GuiUtils.SimpleLabel("Time until closest approach", GuiUtils.TimeToDHMS(closestApproachTime - vesselState.time));
            GuiUtils.SimpleLabel("Separation at closest approach", MuUtils.ToSI(orbit.Separation(core.target.Orbit, closestApproachTime), 0) + "m");


            //Maneuver planning buttons:

            if (GUILayout.Button("Align Planes"))
            {
                double UT;
                Vector3d dV;
                if (orbit.AscendingNodeExists(core.target.Orbit))
                {
                    dV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesAscending(orbit, core.target.Orbit, vesselState.time, out UT);
                }
                else
                {
                    dV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesDescending(orbit, core.target.Orbit, vesselState.time, out UT);
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
                Vector3d dV = OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(orbit, core.target.Orbit, vesselState.time, out UT);
                vessel.RemoveAllManeuverNodes();
                vessel.PlaceManeuverNode(orbit, dV, UT);
            }

            if (GUILayout.Button("Match velocities at closest approach"))
            {
                double UT = closestApproachTime;
                Vector3d dV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(orbit, UT, core.target.Orbit);
                vessel.RemoveAllManeuverNodes();
                vessel.PlaceManeuverNode(orbit, dV, UT);
            }

            if (GUILayout.Button("Get closer"))
            {
                double UT = vesselState.time;
                double interceptUT = UT + 100;
                Vector3d dV = OrbitalManeuverCalculator.DeltaVToInterceptAtTime(orbit, UT, core.target.Orbit, interceptUT, 10);
                vessel.RemoveAllManeuverNodes();
                vessel.PlaceManeuverNode(orbit, dV, UT);
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
    }
}
