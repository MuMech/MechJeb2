extern alias JetBrainsAnnotations;
using System.Collections.Generic;
using JetBrainsAnnotations::JetBrains.Annotations;
using KSP.Localization;
using static MechJebLib.Utils.Statics;

namespace MuMech
{
    [UsedImplicitly]
    public class OperationApoapsis : Operation
    {
        private static readonly string _name = Localizer.Format("#MechJeb_Ap_title");
        public override string GetName() => _name;

        [UsedImplicitly, Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDoubleMult NewApA = new EditableDoubleMult(200000, 1000);

        private static readonly TimeReference[] _timeReferences = { TimeReference.PERIAPSIS, TimeReference.APOAPSIS, TimeReference.X_FROM_NOW, TimeReference.ALTITUDE, TimeReference.EQ_DESCENDING, TimeReference.EQ_ASCENDING };

        internal static readonly TimeSelector _timeSelector = new TimeSelector(_timeReferences);

        protected override List<ManeuverParameters> MakeNodesImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            double ut = _timeSelector.ComputeManeuverTime(o, universalTime, target);

            // below minus the radius is a hyperbolic orbit, which is fine; between that and the burn there's no such orbit
            double newApR = NewApA + o.referenceBody.Radius;
            if (newApR >= 0 && newApR < o.Radius(ut))
            {
                throw new OperationException(Localizer.Format("#MechJeb_Ap_Exception",
                    (o.Radius(ut) - o.referenceBody.Radius).ToSI() + "m")); //new apoapsis cannot be lower than the altitude of the burn (<<1>>)
            }

            Vector3d deltaV = OrbitalManeuverCalculator.DeltaVToChangeApoapsis(o, ut, newApR);

            return new List<ManeuverParameters> { new ManeuverParameters(deltaV, ut) };
        }
    }
}
