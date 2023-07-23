using System.Collections.Generic;
using JetBrains.Annotations;
using KSP.Localization;

namespace MuMech
{
    [UsedImplicitly]
    public class OperationKillRelVel : Operation
    {
        public override string GetName() { return Localizer.Format("#MechJeb_match_v_title"); } //match velocities with target

        private readonly TimeSelector _timeSelector = new TimeSelector(new[] { TimeReference.CLOSEST_APPROACH, TimeReference.X_FROM_NOW });

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            _timeSelector.DoChooseTimeGUI();
        }

        protected override List<ManeuverParameters> MakeNodesImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            if (!target.NormalTargetExists)
                throw new OperationException(Localizer.Format("#MechJeb_match_v_Exception1")); //must select a target to match velocities with.
            if (o.referenceBody != target.TargetOrbit.referenceBody)
                throw new OperationException(Localizer.Format("#MechJeb_match_v_Exception2")); //target must be in the same sphere of influence.

            double ut = _timeSelector.ComputeManeuverTime(o, universalTime, target);

            Vector3d dV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(o, ut, target.TargetOrbit);

            return new List<ManeuverParameters> { new ManeuverParameters(dV, ut) };
        }
    }
}
