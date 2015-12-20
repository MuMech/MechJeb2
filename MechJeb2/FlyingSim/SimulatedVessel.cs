using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class SimulatedVessel
    {

        public List<SimulatedPart> parts = new List<SimulatedPart>();
        private int count;
        public double totalMass = 0;

        private ReentrySimulation.SimCurves simCurves;

        public static SimulatedVessel New(Vessel v, ReentrySimulation.SimCurves simCurves, double startTime, int limitChutesStage)
        {
            SimulatedVessel vessel = new SimulatedVessel();
            vessel.Set(v, simCurves, startTime, limitChutesStage);
            return vessel;
        }

        private void Set(Vessel v, ReentrySimulation.SimCurves _simCurves, double startTime, int limitChutesStage)
        {
            totalMass = 0;

            var oParts = v.Parts;
            count = oParts.Count;

            simCurves = _simCurves;

            if (parts.Capacity < count)
                parts.Capacity = count;

            for (int i=0; i < count; i++)
            {
                SimulatedPart simulatedPart = null;
                bool special = false;
                for (int j = 0; j < oParts[i].Modules.Count; j++)
                {
                    ModuleParachute mp = oParts[i].Modules[j] as ModuleParachute;
                    if (mp != null)
                    {
                        special = true;
                        simulatedPart = SimulatedParachute.New(mp, simCurves, startTime, limitChutesStage);
                    }
                }
                if (!special)
                {
                    simulatedPart = SimulatedPart.New(oParts[i], simCurves);
                }

                parts.Add(simulatedPart);
                totalMass += simulatedPart.totalMass;
            }
        }

        public Vector3d Drag(Vector3d localVelocity, double dynamicPressurekPa, float mach)
        {
            Vector3d drag = Vector3d.zero;

            double dragFactor = dynamicPressurekPa * PhysicsGlobals.DragCubeMultiplier * PhysicsGlobals.DragMultiplier;

            for (int i = 0; i < count; i++)
            {
                drag += parts[i].Drag(localVelocity, dragFactor, mach);
            }

            return -localVelocity.normalized * drag.magnitude;
        }

        public Vector3d Lift(Vector3d localVelocity, float dynamicPressurekPa, float mach)
        {
            Vector3d lift = Vector3d.zero;

            double liftFactor = dynamicPressurekPa * simCurves.LiftMachCurve.Evaluate(mach);

            for (int i = 0; i < count; i++)
            {
                lift += parts[i].Lift(localVelocity, liftFactor);
            }
            return lift;
        }


        public bool WillChutesDeploy(double altAGL, double altASL, double probableLandingSiteASL, double pressure, double t, double parachuteSemiDeployMultiplier)
        {
            for (int i = 0; i < count; i++)
            {
                if (parts[i].SimulateAndRollback(altAGL, altASL, probableLandingSiteASL, pressure, t, parachuteSemiDeployMultiplier))
                {
                    return true;
                }
            }
            return false;
        }

        public bool Simulate(double altATGL, double altASL, double endASL, double pressure, double time, double semiDeployMultiplier)
        {
            bool deploying = false;
            for (int i = 0; i < count; i++)
            {
                deploying |= parts[i].Simulate(altATGL, altASL, endASL, pressure, time, semiDeployMultiplier);
            }
            return deploying;
        }

    }
}
