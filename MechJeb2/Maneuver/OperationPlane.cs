using System;

namespace MuMech
{
    public class OperationPlane : Operation
    {
        public override string getName() { return "match planes with target";}

        private TimeSelector timeSelector;

        public OperationPlane ()
        {
            timeSelector = new TimeSelector(new TimeReference[] { TimeReference.REL_ASCENDING, TimeReference.REL_DESCENDING });
        }

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            timeSelector.DoChooseTimeGUI();
        }

        public override ManeuverParameters MakeNodeImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            double UT = timeSelector.ComputeManeuverTime(o, universalTime, target);

            if (!target.NormalTargetExists)
            {
                throw new OperationException("must select a target to match planes with.");
            }
            else if (o.referenceBody != target.TargetOrbit.referenceBody)
            {
                throw new OperationException("can only match planes with an object in the same sphere of influence.");
            }
            else if (timeSelector.timeReference == TimeReference.REL_ASCENDING)
            {
                if (!o.AscendingNodeExists(target.TargetOrbit))
                {
                    throw new OperationException("ascending node with target doesn't exist.");
                }
            }
            else
            {
                if (!o.DescendingNodeExists(target.TargetOrbit))
                {
                    throw new OperationException("descending node with target doesn't exist.");
                }
            }

            Vector3d dV = (timeSelector.timeReference == TimeReference.REL_ASCENDING) ?
                OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesAscending(o, target.TargetOrbit, UT, out UT):
                OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesDescending(o, target.TargetOrbit, UT, out UT);

            return new ManeuverParameters(dV, UT);
        }
    }
}

