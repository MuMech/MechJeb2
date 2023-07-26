using System.Collections.Generic;
using JetBrains.Annotations;
using KSP.Localization;

namespace MuMech
{
    [UsedImplicitly]
    public class OperationLambert : Operation
    {
        private static readonly string _name = Localizer.Format("#MechJeb_intercept_title");
        public override         string GetName() => _name;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public EditableTime InterceptInterval = 3600;

        private static readonly TimeReference[] _timeReferences = { TimeReference.X_FROM_NOW };

        private static readonly TimeSelector _timeSelector = new TimeSelector(_timeReferences);

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_intercept_label"), InterceptInterval); //Time after burn to intercept target:
            _timeSelector.DoChooseTimeGUI();
        }

        protected override List<ManeuverParameters> MakeNodesImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            if (!target.NormalTargetExists)
                throw new OperationException(Localizer.Format("#MechJeb_intercept_Exception1")); //must select a target to intercept.
            if (o.referenceBody != target.TargetOrbit.referenceBody)
                throw new OperationException(Localizer.Format("#MechJeb_intercept_Exception2")); //target must be in the same sphere of influence.

            double ut = _timeSelector.ComputeManeuverTime(o, universalTime, target);

            (Vector3d dV, _) = OrbitalManeuverCalculator.DeltaVToInterceptAtTime(o, ut, target.TargetOrbit, ut + InterceptInterval);

            return new List<ManeuverParameters> { new ManeuverParameters(dV, ut) };
        }
    }
}
