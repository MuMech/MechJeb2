using KSP.Localization;
using System.Collections.Generic;

namespace MuMech
{
    public class OperationKillRelVel : Operation
    {
        public override string getName() { return Localizer.Format("#MechJeb_match_v_title");}//match velocities with target

        private TimeSelector timeSelector;

        public OperationKillRelVel ()
        {
            timeSelector = new TimeSelector(new TimeReference[] { TimeReference.CLOSEST_APPROACH, TimeReference.X_FROM_NOW });
        }

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            timeSelector.DoChooseTimeGUI();
        }

        public override List<ManeuverParameters> MakeNodesImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            if (!target.NormalTargetExists)
                throw new OperationException(Localizer.Format("#MechJeb_match_v_Exception1"));//must select a target to match velocities with.
            else if (o.referenceBody != target.TargetOrbit.referenceBody)
                throw new OperationException(Localizer.Format("#MechJeb_match_v_Exception2"));//target must be in the same sphere of influence.

            double UT = timeSelector.ComputeManeuverTime(o, universalTime, target);

            var dV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(o, UT, target.TargetOrbit);

            List<ManeuverParameters> NodeList = new List<ManeuverParameters>();
            NodeList.Add(new ManeuverParameters(dV, UT));
            return NodeList;
        }

        public TimeSelector getTimeSelector() //Required for scripts to save configuration
        {
            return this.timeSelector;
        }
    }
}
