using System.Collections.Generic;
using JetBrains.Annotations;
using KSP.Localization;

namespace MuMech
{
    [UsedImplicitly]
    public class OperationApoapsis : Operation
    {
        public override string GetName() { return Localizer.Format("#MechJeb_Ap_title"); } //change apoapsis

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDoubleMult NewApA = new EditableDoubleMult(200000, 1000);

        private readonly TimeSelector _timeSelector;

        public OperationApoapsis()
        {
            _timeSelector = new TimeSelector(new[]
            {
                TimeReference.PERIAPSIS, TimeReference.APOAPSIS, TimeReference.X_FROM_NOW, TimeReference.ALTITUDE, TimeReference.EQ_DESCENDING, TimeReference.EQ_ASCENDING
            });
        }

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_Ap_label1"), NewApA, "km"); //New apoapsis:
            _timeSelector.DoChooseTimeGUI();
        }

        protected override List<ManeuverParameters> MakeNodesImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            double ut = _timeSelector.ComputeManeuverTime(o, universalTime, target);

            return new List<ManeuverParameters>
            {
                new ManeuverParameters(OrbitalManeuverCalculator.DeltaVToChangeApoapsis(o, ut, NewApA + o.referenceBody.Radius), ut)
            };
        }

        public TimeSelector GetTimeSelector() //Required for scripts to save configuration
        {
            return _timeSelector;
        }
    }
}
