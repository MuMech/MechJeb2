using UnityEngine;

namespace MuMech
{
    public class OperationGeneric : Operation
    {
        public override string getName() { return "bi-impulsive (Hohmann) transfer to target";}

        public bool intercept_only = false;

        public OperationGeneric ()
        {
        }

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            intercept_only = GUILayout.Toggle(intercept_only, "intercept only, no capture burn (impact/flyby)");
            GUILayout.Label("Schedule the burn at the next transfer window.");
        }

        public override ManeuverParameters MakeNodeImpl(Orbit o, double UT, MechJebModuleTargetController target)
        {
            if (!target.NormalTargetExists)
            {
                throw new OperationException("must select a target for the Bi-impulsive transfer.");
            }
            else if (o.referenceBody != target.TargetOrbit.referenceBody)
            {
                throw new OperationException("target for Bi-impulsive transfer must be in the same sphere of influence.");
            }

            Vector3d dV = OrbitalManeuverCalculator.DeltaVAndTimeForBiImpulsiveAnnealed(o, target.TargetOrbit, UT, out UT, intercept_only);

            return new ManeuverParameters(dV, UT);
        }
    }
}
