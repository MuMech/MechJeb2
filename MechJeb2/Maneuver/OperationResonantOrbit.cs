extern alias JetBrainsAnnotations;
using System.Collections.Generic;
using JetBrainsAnnotations::JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class OperationResonantOrbit : Operation
    {
        private static readonly string _name = Localizer.Format("#MechJeb_resonant_title");
        public override string GetName() => _name;

        [UsedImplicitly, Persistent(pass = (int)Pass.GLOBAL)]
        public EditableInt ResonanceNumerator = 2;

        [UsedImplicitly, Persistent(pass = (int)Pass.GLOBAL)]
        public EditableInt ResonanceDenominator = 3;

        internal readonly TimeSelector _timeSelector =
            new TimeSelector(new[] { TimeReference.APOAPSIS, TimeReference.PERIAPSIS, TimeReference.X_FROM_NOW, TimeReference.ALTITUDE });

        protected override List<ManeuverParameters> MakeNodesImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            double ut = _timeSelector.ComputeManeuverTime(o, universalTime, target);
            Vector3d dV = OrbitalManeuverCalculator.DeltaVToResonantOrbit(o, ut, (double)ResonanceNumerator.Val / ResonanceDenominator.Val);

            return new List<ManeuverParameters> { new ManeuverParameters(dV, ut) };
        }
    }
}
