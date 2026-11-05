extern alias JetBrainsAnnotations;
using System.Collections.Generic;
using JetBrainsAnnotations::JetBrains.Annotations;
using KSP.Localization;
using static MechJebLib.Utils.Statics;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class OperationMoonReturn : Operation
    {
        private static readonly string _name = Localizer.Format("#MechJeb_return_title");
        public override string GetName() => _name;

        [UsedImplicitly, Persistent(pass = (int)Pass.GLOBAL)]
        public EditableDoubleMult Periapsis = new EditableDoubleMult(100000, 1000);

        [UsedImplicitly, Persistent(pass = (int)Pass.GLOBAL)]
        public EditableDouble Inclination = new EditableDouble(-90);

        [UsedImplicitly, Persistent(pass = (int)Pass.GLOBAL)]
        public bool InclinationFlag;

        protected override List<ManeuverParameters> MakeNodesImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            if (o.referenceBody.referenceBody == null)
            {
                throw new OperationException(Localizer.Format("#MechJeb_return_Exception",
                    o.referenceBody.displayName.LocalizeRemoveGender())); //<<1>> is not orbiting another body you could return to.
            }

            double per = o.referenceBody.referenceBody.Radius + Periapsis;
            // a periapsis at the centre has no scale for the solver
            if (!(per > 0))
            {
                throw new OperationException(Localizer.Format("#MechJeb_Pe_Exception2", o.referenceBody.referenceBody.displayName.LocalizeRemoveGender()) +
                    " (-" + o.referenceBody.referenceBody.Radius.ToSI(3) + "m)"); //new periapsis cannot be lower than minus the radius of <<1>>
            }

            double inc = InclinationFlag ? Inclination.Val : double.NaN;

            (Vector3d dV, double ut) = OrbitalManeuverCalculator.DeltaVAndTimeForMoonReturnEjection(o, universalTime, per, inc);

            return new List<ManeuverParameters> { new ManeuverParameters(dV, ut) };
        }
    }
}
