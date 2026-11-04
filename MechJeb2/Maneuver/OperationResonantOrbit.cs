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
            if (ResonanceNumerator.Val <= 0 || ResonanceDenominator.Val <= 0)
                throw new OperationException(Localizer.Format("#MechJeb_resonant_Exception1")); //the period ratio needs two numbers above zero.

            if (o.eccentricity >= 1)
                throw new OperationException(Localizer.Format("#MechJeb_resonant_Exception2")); //a hyperbolic orbit has no period.

            double ut = _timeSelector.ComputeManeuverTime(o, universalTime, target);
            double f  = (double)ResonanceNumerator.Val / ResonanceDenominator.Val;

            // An orbit through the burn point can't have a semi-major axis below half the burn radius
            if (2 * OrbitalManeuverCalculator.ResonantOrbitSemiMajorAxis(o, f) <= o.Radius(ut))
                throw new OperationException(Localizer.Format("#MechJeb_resonant_Exception3")); //this period is too short for an orbit through the burn point.

            Vector3d dV = OrbitalManeuverCalculator.DeltaVToResonantOrbit(o, ut, f);

            return new List<ManeuverParameters> { new ManeuverParameters(dV, ut) };
        }
    }
}
