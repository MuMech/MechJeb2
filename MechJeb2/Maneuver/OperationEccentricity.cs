using System.Collections.Generic;
using JetBrains.Annotations;

namespace MuMech
{
    public class OperationEccentricity : Operation
    {
        public override string GetName() { return "change eccentricity"; }

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.Global)]
        public readonly EditableDoubleMult NewEcc = new EditableDouble(0);

        private readonly TimeSelector _timeSelector;

        public OperationEccentricity()
        {
            _timeSelector = new TimeSelector(new[]
            {
                TimeReference.APOAPSIS, TimeReference.PERIAPSIS, TimeReference.X_FROM_NOW, TimeReference.ALTITUDE
            });
        }

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            GuiUtils.SimpleTextBox("New eccentricity:", NewEcc);
            _timeSelector.DoChooseTimeGUI();
        }

        protected override List<ManeuverParameters> MakeNodesImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            double ut = _timeSelector.ComputeManeuverTime(o, universalTime, target);

            return new List<ManeuverParameters> { new ManeuverParameters(OrbitalManeuverCalculator.DeltaVToChangeEccentricity(o, ut, NewEcc), ut) };
        }

        public TimeSelector GetTimeSelector() //Required for scripts to save configuration
        {
            return _timeSelector;
        }
    }
}
