using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MuMech
{
    public class OperationGeneric : Operation
    {
        public override string getName() { return "Hohmann transfer to target";}

        public OperationGeneric ()
        {
        }

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            GUILayout.Label("Schedule the burn at the next transfer window.");
        }

        public override ManeuverParameters MakeNodeImpl(Orbit o, double UT, MechJebModuleTargetController target)
        {
            if (!target.NormalTargetExists)
            {
                throw new OperationException("must select a target for the Hohmann transfer.");
            }
            else if (o.referenceBody != target.TargetOrbit.referenceBody)
            {
                throw new OperationException("target for Hohmann transfer must be in the same sphere of influence.");
            }
            else if (o.eccentricity > 1)
            {
                throw new OperationException("starting orbit for Hohmann transfer must not be hyperbolic.");
            }
            else if (target.TargetOrbit.eccentricity > 1)
            {
                throw new OperationException("target orbit for Hohmann transfer must not be hyperbolic.");
            }
            else if (o.RelativeInclination(target.TargetOrbit) > 30 && o.RelativeInclination(target.TargetOrbit) < 150)
            {
                errorMessage = "Warning: target's orbital plane is at a " + o.RelativeInclination(target.TargetOrbit).ToString("F0") + "º angle to starting orbit's plane (recommend at most 30º). Planned transfer may not intercept target properly.";
            }
            else if (o.eccentricity > 0.2)
            {
                errorMessage = "Warning: Recommend starting Hohmann transfers from a near-circular orbit (eccentricity < 0.2). Planned transfer is starting from an orbit with eccentricity " + o.eccentricity.ToString("F2") + " and so may not intercept target properly.";
            }

            Vector3d dV = OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(o, target.TargetOrbit, UT, out UT);

            return new ManeuverParameters(dV, UT);
        }
    }
}

