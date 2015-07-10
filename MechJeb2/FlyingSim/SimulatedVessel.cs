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

        static public SimulatedVessel New(Vessel v, ReentrySimulation.SimCurves simCurves, double startTime, int limitChutesStage)
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
                    if (oParts[i].Modules[j] is ModuleParachute)
                    {
                        special = true;
                        simulatedPart = SimulatedParachute.New((ModuleParachute)oParts[i].Modules[j], simCurves, startTime, limitChutesStage);
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

        public Vector3 Drag(Vector3 localVelocity, float dynamicPressurekPa, float mach)
        {
            Vector3 drag = Vector3.zero;

            for (int i = 0; i < count; i++)
            {
                SimulatedPart part = parts[i];
                drag += part.Drag(localVelocity, dynamicPressurekPa, mach);
            }

            return -localVelocity.normalized * drag.magnitude;
        }

        public Vector3 Lift(Vector3 localVelocity, float dynamicPressurekPa, float mach)
        {
            Vector3 lift = Vector3.zero;

            for (int i = 0; i < count; i++)
            {
                SimulatedPart part = parts[i];
                lift += part.Lift(localVelocity, dynamicPressurekPa, mach);
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
