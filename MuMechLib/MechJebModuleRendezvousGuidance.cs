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

        protected override void FlightWindowGUI(int windowID)
        {
            if (!core.target.NormalTargetExists)
            {
                GUILayout.Label("Select a target to rendezvous with.");
                base.FlightWindowGUI(windowID);
                return;
            }


            GUILayout.BeginVertical();

            
            double leadTime = 30;

            GUILayout.Label("First, bring your relative inclination to zero by aligning your orbital plane with the target's orbital plane:");
            GUILayout.Label("Relative inclination: " + orbit.RelativeInclination(core.target.Orbit).ToString("F2") + " degrees");

            if (GUILayout.Button("Align Planes"))
            {
                double UT;
                Vector3d dV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesAscending(orbit, core.target.Orbit, vesselState.time, out UT);
                vessel.PlaceManeuverNode(orbit, dV, UT);
            }

            double phasingOrbitRadius = 0.9 * core.target.Orbit.PeR;
            double phasingOrbitAltitudeKm = (phasingOrbitRadius - mainBody.Radius) / 1000.0;
            GUILayout.Label("Next, establish a circular phasing orbit just beneath the target orbit.");
            GUILayout.Label("Target orbit: " + (core.target.Orbit.PeA / 1000).ToString("F0") + "km x " + (core.target.Orbit.ApA / 1000).ToString("F0") + "km");
            GUILayout.Label("Suggested phasing orbit: " + phasingOrbitAltitudeKm.ToString("F0") + "km x " + phasingOrbitAltitudeKm.ToString("F0") + "km");
            GUILayout.Label("Current orbit: " + (orbit.PeA / 1000).ToString("F0") + "km x " + (orbit.ApA / 1000).ToString("F0") + "km");

            if (GUILayout.Button("Establish Phasing Orbit"))
            {
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

            GUILayout.Label("Once in the phasing orbit, transfer to the target orbit at just the right time to intercept the target:");

            if (GUILayout.Button("Intercept with Hohmann transfer"))
            {
                double UT;
                Vector3d dV = OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(orbit, core.target.Orbit, vesselState.time, out UT);
                vessel.PlaceManeuverNode(orbit, dV, UT);
            }

            double closestApproachTime = orbit.NextClosestApproachTime(core.target.Orbit, vesselState.time);

            GUILayout.Label("Once on a transfer trajectory, match orbits by zeroing out your relative velocity at closest approach:");
            GUILayout.Label("Time until closest approach: " + (closestApproachTime - vesselState.time).ToString("F0") + "s");
            GUILayout.Label("Separation at closest approach: " + orbit.Separation(core.target.Orbit, closestApproachTime).ToString("F0") + "m");

            if (GUILayout.Button("Kill relvel at closest approach"))
            {
                double UT = closestApproachTime;
                Vector3d dV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(orbit, UT, core.target.Orbit);
                vessel.PlaceManeuverNode(orbit, dV, UT);
            }

            GUILayout.Label("If you aren't close enough after killing relative velocities, thrust gently toward the target:");

            if (GUILayout.Button("Get closer"))
            {
                double UT = vesselState.time;
                double interceptUT = UT + 100;
                Vector3d dV = OrbitalManeuverCalculator.DeltaVToInterceptAtTime(orbit, UT, core.target.Orbit, interceptUT);
                vessel.PlaceManeuverNode(orbit, dV, UT);
            }

            GUILayout.Label("Then kill your relative velocity again at closest approach");


            MechJebModuleRendezvousAutopilot autopilot = core.GetComputerModule<MechJebModuleRendezvousAutopilot>();
            autopilot.enabled = GUILayout.Toggle(autopilot.enabled, "Autopilot enable");
            if (autopilot.enabled) GUILayout.Label("Status: " + autopilot.status);


            GUILayout.EndVertical();

            base.FlightWindowGUI(windowID);
        }

        public override GUILayoutOption[] FlightWindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(300), GUILayout.Height(150) };
        }

        public override string GetName()
        {
            return "Rendezvous Guidance";
        }
    }
}
