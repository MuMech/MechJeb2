using System;
using UnityEngine;

namespace MuMech
{
    public class OperationInterplanetaryTransfer : Operation
    {
        public override string getName() { return "transfer to another planet";}

        [Persistent(pass = (int)(Pass.Local | Pass.Type | Pass.Global))]
        private bool waitForPhaseAngle = true;

        public OperationInterplanetaryTransfer ()
        {
        }

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            GUILayout.Label("Schedule the burn:");
            waitForPhaseAngle = GUILayout.Toggle(waitForPhaseAngle, "at the next transfer window.");
            waitForPhaseAngle = !GUILayout.Toggle(!waitForPhaseAngle, "as soon as possible");

            if (!waitForPhaseAngle)
            {
                GUIStyle s = new GUIStyle(GUI.skin.label) {normal = {textColor = Color.yellow}};
                GUILayout.Label("Using this mode voids your warranty", s);
            }
        }

        public override ManeuverParameters MakeNodeImpl(Orbit o, double UT, MechJebModuleTargetController target)
        {

            // Check preconditions
            if (!target.NormalTargetExists)
                throw new OperationException("must select a target for the interplanetary transfer.");

            if (o.referenceBody.referenceBody == null)
                throw new OperationException("doesn't make sense to plot an interplanetary transfer from an orbit around " + o.referenceBody.theName + ".");

            if (o.referenceBody.referenceBody != target.TargetOrbit.referenceBody)
            {
                if (o.referenceBody == target.TargetOrbit.referenceBody)
                    throw new OperationException("use regular Hohmann transfer function to intercept another body orbiting " + o.referenceBody.theName + ".");
                throw new OperationException("an interplanetary transfer from within " + o.referenceBody.theName + "'s sphere of influence must target a body that orbits " + o.referenceBody.theName + "'s parent, " + o.referenceBody.referenceBody.theName + ".");
            }

            // Simple warnings
            if (o.referenceBody.orbit.RelativeInclination(target.TargetOrbit) > 30)
            {
                errorMessage = "Warning: target's orbital plane is at a " + o.RelativeInclination(target.TargetOrbit).ToString("F0") + "º angle to " + o.referenceBody.theName + "'s orbital plane (recommend at most 30º). Planned interplanetary transfer may not intercept target properly.";
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

            var dV = OrbitalManeuverCalculator.DeltaVAndTimeForInterplanetaryTransferEjection(o, UT, target.TargetOrbit, waitForPhaseAngle, out UT);
            return new ManeuverParameters(dV, UT);
        }
    }
}

