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

            
            double leadTime = 30;

            GUILayout.Label("First, bring your relative inclination to zero by aligning your orbital plane with the target's orbital plane:");
            GUILayout.Label("Relative inclination: " + orbit.RelativeInclination(core.target.Orbit).ToString("F2") + "º");

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
                vessel.PlaceManeuverNode(orbit, dV, UT);
            }

            double phasingOrbitRadius = 0.9 * core.target.Orbit.PeR;
            if (phasingOrbitRadius < orbit.referenceBody.Radius + orbit.referenceBody.RealMaxAtmosphereAltitude())
            {
                phasingOrbitRadius = 1.1 * core.target.Orbit.ApR;
            }
            double phasingOrbitAltitude = phasingOrbitRadius - mainBody.Radius;
            GUILayout.Label("Next, establish a circular phasing orbit close to the target orbit.");
            GUILayout.Label("Target orbit: " + MuUtils.ToSI(core.target.Orbit.PeA, 3) + "m x " + MuUtils.ToSI(core.target.Orbit.ApA, 3) + "m");
            GUILayout.Label("Suggested phasing orbit: " + MuUtils.ToSI(phasingOrbitAltitude, 3) + "m x " + MuUtils.ToSI(phasingOrbitAltitude, 3) + "m");
            GUILayout.Label("Current orbit: " + MuUtils.ToSI(orbit.PeA, 3) + "m x " + MuUtils.ToSI(orbit.ApA, 3) + "m");

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

            GUILayout.Label("Once on a transfer trajectory, match velocities at closest approach:");
            GUILayout.Label("Time until closest approach: " + MuUtils.ToSI(closestApproachTime - vesselState.time, 0) + "s");
            GUILayout.Label("Separation at closest approach: " + MuUtils.ToSI(orbit.Separation(core.target.Orbit, closestApproachTime), 0) + "m");

            if (GUILayout.Button("Match velocities at closest approach"))
            {
                double UT = closestApproachTime;
                Vector3d dV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(orbit, UT, core.target.Orbit);
                vessel.PlaceManeuverNode(orbit, dV, UT);
            }

            GUILayout.Label("If you aren't close enough after matching velocities, thrust gently toward the target:");

            if (GUILayout.Button("Get closer"))
            {
                double UT = vesselState.time;
                double interceptUT = UT + 100;
                Vector3d dV = OrbitalManeuverCalculator.DeltaVToInterceptAtTime(orbit, UT, core.target.Orbit, interceptUT, 10);
                vessel.PlaceManeuverNode(orbit, dV, UT);
            }

            GUILayout.Label("Then match velocities again at closest approach");


            MechJebModuleRendezvousAutopilot autopilot = core.GetComputerModule<MechJebModuleRendezvousAutopilot>();
            autopilot.enabled = GUILayout.Toggle(autopilot.enabled, "Autopilot enable");
            if (autopilot.enabled) GUILayout.Label("Status: " + autopilot.status);


            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(300), GUILayout.Height(150) };
        }

        public override string GetName()
        {
            return "Rendezvous Guidance";
        }
    }
}
