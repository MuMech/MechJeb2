extern alias JetBrainsAnnotations;
using System.Collections.Generic;
using JetBrainsAnnotations::JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class OperationLan : Operation
    {
        private static readonly string _name = Localizer.Format("#MechJeb_AN_title");
        public override string GetName() => _name;

        private static readonly TimeReference[] _timeReferences = { TimeReference.APOAPSIS, TimeReference.PERIAPSIS, TimeReference.X_FROM_NOW };

        internal readonly TimeSelector _timeSelector = new TimeSelector(_timeReferences);

        internal override TimeSelector Timing => _timeSelector;

        /// <summary>
        /// The new LAN in degrees, in place of the target controller's targetLongitude (which the window edits, and which also
        /// places a position target). Set by kOS only.
        /// </summary>
        internal double? LongitudeOverride;

        protected override List<ManeuverParameters> MakeNodesImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            if (o.inclination < 10)
                ErrorMessage = Localizer.Format("#MechJeb_AN_error",
                    o.inclination); //"Warning: orbital plane has a low inclination of <<1>>º (recommend > 10º) and so maneuver may not be accurate"

            double ut = _timeSelector.ComputeManeuverTime(o, universalTime, target);

            Vector3d dV = OrbitalManeuverCalculator.DeltaVToShiftLAN(o, ut, LongitudeOverride ?? (double)target.targetLongitude);

            return new List<ManeuverParameters> { new ManeuverParameters(dV, ut) };
        }
    }
}
