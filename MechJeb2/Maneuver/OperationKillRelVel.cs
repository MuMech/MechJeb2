using System.Collections.Generic;
using JetBrains.Annotations;
using KSP.Localization;

namespace MuMech
{
    [UsedImplicitly]
    public class OperationKillRelVel : Operation
    {
        private static readonly string _name = Localizer.Format("#MechJeb_match_v_title");
        public override         string GetName() => _name;

        private static readonly TimeReference[] _timeReferences = { TimeReference.CLOSEST_APPROACH, TimeReference.X_FROM_NOW };

        private static readonly TimeSelector _timeSelector = new TimeSelector(_timeReferences);

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target) => _timeSelector.DoChooseTimeGUI();

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
