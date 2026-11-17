extern alias JetBrainsAnnotations;
using System.Collections.Generic;
using JetBrainsAnnotations::JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class OperationLongitude : Operation
    {
        private static readonly string _name = Localizer.Format("#MechJeb_la_title");
        public override string GetName() => _name;

        private static readonly TimeReference[] _timeReferences = { TimeReference.APOAPSIS, TimeReference.PERIAPSIS };

        internal readonly TimeSelector _timeSelector = new TimeSelector(_timeReferences);

        internal override TimeSelector Timing => _timeSelector;

        /// <summary>
        /// The new longitude in degrees, in place of the target controller's targetLongitude (which the window edits, and which also
        /// places a position target). Set by kOS only.
        /// </summary>
        internal double? LongitudeOverride;

        protected override List<ManeuverParameters> MakeNodesImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            double ut = _timeSelector.ComputeManeuverTime(o, universalTime, target);
            Vector3d dV = OrbitalManeuverCalculator.DeltaVToShiftNodeLongitude(o, ut, LongitudeOverride ?? (double)target.targetLongitude);

            return new List<ManeuverParameters> { new ManeuverParameters(dV, ut) };
        }
    }
}
