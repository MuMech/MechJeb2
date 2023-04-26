using System.Collections.Generic;
using JetBrains.Annotations;
using KSP.Localization;

namespace MuMech
{
    [UsedImplicitly]
    public class OperationCircularize : Operation
    {
        public override string GetName() { return Localizer.Format("#MechJeb_Maneu_circularize_title"); } //"circularize"

        private readonly TimeSelector _timeSelector;

        public OperationCircularize()
        {
            _timeSelector = new TimeSelector(new[]
            {
                TimeReference.APOAPSIS, TimeReference.PERIAPSIS, TimeReference.X_FROM_NOW, TimeReference.ALTITUDE
            });
        }

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            _timeSelector.DoChooseTimeGUI();
        }

        protected override List<ManeuverParameters> MakeNodesImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            double ut = _timeSelector.ComputeManeuverTime(o, universalTime, target);
            return new List<ManeuverParameters> { new ManeuverParameters(OrbitalManeuverCalculator.DeltaVToCircularize(o, ut), ut) };
        }

        public TimeSelector GetTimeSelector() //Required for scripts to save configuration
        {
            return _timeSelector;
        }
    }
}
