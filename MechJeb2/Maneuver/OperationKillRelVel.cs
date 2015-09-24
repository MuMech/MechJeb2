using System;

namespace MuMech
{
    public class OperationKillRelVel : Operation
    {
        public override string getName() { return "match velocities with target";}

        private TimeSelector timeSelector;

        public OperationKillRelVel ()
        {
            timeSelector = new TimeSelector(new TimeReference[] { TimeReference.CLOSEST_APPROACH, TimeReference.X_FROM_NOW });
        }

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            timeSelector.DoChooseTimeGUI();
        }

        public override ManeuverParameters MakeNodeImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            if (!target.NormalTargetExists)
                throw new OperationException("must select a target to match velocities with.");
            else if (o.referenceBody != target.TargetOrbit.referenceBody)
                throw new OperationException("target must be in the same sphere of influence.");

            double UT = timeSelector.ComputeManeuverTime(o, universalTime, target);

            var dV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(o, UT, target.TargetOrbit);
            return new ManeuverParameters(dV, UT);
        }
    }
}

