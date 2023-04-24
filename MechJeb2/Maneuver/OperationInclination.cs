using System.Collections.Generic;
using JetBrains.Annotations;
using KSP.Localization;

namespace MuMech
{
    [UsedImplicitly]
    public class OperationInclination : Operation
    {
        public override string GetName() { return Localizer.Format("#MechJeb_inclination_title"); } //change inclination

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.Global)]
        public EditableDouble NewInc = 0;

        private readonly TimeSelector _timeSelector;

        public OperationInclination()
        {
            _timeSelector = new TimeSelector(new[]
            {
                TimeReference.EQ_HIGHEST_AD, TimeReference.EQ_NEAREST_AD, TimeReference.EQ_ASCENDING, TimeReference.EQ_DESCENDING,
                TimeReference.X_FROM_NOW
            });
        }

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

        public TimeSelector GetTimeSelector() //Required for scripts to save configuration
        {
            return _timeSelector;
        }
    }
}
