using System.Collections.Generic;
using JetBrains.Annotations;

namespace MuMech
{
    public class OperationEccentricity : Operation
    {
        private static readonly string _name = "change eccentricity";
        public override         string GetName() => _name;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDoubleMult NewEcc = new EditableDouble(0);

        private static readonly TimeReference[] _timeReferences =
        {
            TimeReference.APOAPSIS, TimeReference.PERIAPSIS, TimeReference.X_FROM_NOW, TimeReference.ALTITUDE
        };

        private static readonly TimeSelector _timeSelector = new TimeSelector(_timeReferences);

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            GuiUtils.SimpleTextBox("New eccentricity:", NewEcc);
            _timeSelector.DoChooseTimeGUI();
        }

        protected override List<ManeuverParameters> MakeNodesImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            double ut = _timeSelector.ComputeManeuverTime(o, universalTime, target);
            Vector3d deltaV = OrbitalManeuverCalculator.DeltaVToChangeEccentricity(o, ut, NewEcc);

            return new List<ManeuverParameters> { new ManeuverParameters(deltaV, ut) };
        }
    }
}
