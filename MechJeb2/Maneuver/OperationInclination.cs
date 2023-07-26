using System.Collections.Generic;
using JetBrains.Annotations;
using KSP.Localization;

namespace MuMech
{
    [UsedImplicitly]
    public class OperationInclination : Operation
    {
        private static readonly string _name = Localizer.Format("#MechJeb_inclination_title");
        public override         string GetName() => _name;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public EditableDouble NewInc = 0;

        private static readonly TimeReference[] _timeReferences =
        {
            TimeReference.EQ_HIGHEST_AD, TimeReference.EQ_NEAREST_AD, TimeReference.EQ_ASCENDING, TimeReference.EQ_DESCENDING,
            TimeReference.X_FROM_NOW
        };

        private static readonly TimeSelector _timeSelector = new TimeSelector(_timeReferences);

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_inclination_label"), NewInc, "º"); //New inclination:
            _timeSelector.DoChooseTimeGUI();
        }

        protected override List<ManeuverParameters> MakeNodesImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            double ut = _timeSelector.ComputeManeuverTime(o, universalTime, target);

            return new List<ManeuverParameters> { new ManeuverParameters(OrbitalManeuverCalculator.DeltaVToChangeInclination(o, ut, NewInc), ut) };
        }
    }
}
