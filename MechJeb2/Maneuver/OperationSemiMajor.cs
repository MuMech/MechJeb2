using System.Collections.Generic;
using JetBrains.Annotations;
using KSP.Localization;
using static MechJebLib.Utils.Statics;

namespace MuMech
{
    [UsedImplicitly]
    public class OperationSemiMajor : Operation
    {
        private static readonly string _name = Localizer.Format("#MechJeb_Sa_title");
        public override         string GetName() => _name;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public EditableDoubleMult NewSma = new EditableDoubleMult(800000, 1000);

        private static readonly TimeReference[] _timeReferences =
        {
            TimeReference.APOAPSIS, TimeReference.PERIAPSIS, TimeReference.X_FROM_NOW, TimeReference.ALTITUDE
        };

        private static readonly TimeSelector _timeSelector = new TimeSelector(_timeReferences);

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_Sa_label"), NewSma, "km"); //New Semi-Major Axis:
            _timeSelector.DoChooseTimeGUI();
        }

        protected override List<ManeuverParameters> MakeNodesImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            double ut = _timeSelector.ComputeManeuverTime(o, universalTime, target);

            if (2 * NewSma > o.Radius(ut) + o.referenceBody.sphereOfInfluence)
            {
                ErrorMessage = Localizer.Format(
                    "#MechJeb_Sa_errormsg"); //Warning: new Semi-Major Axis is very large, and may result in a hyberbolic orbit
            }

            if (o.Radius(ut) > 2 * NewSma)
            {
                throw new OperationException(Localizer.Format("#MechJeb_Sa_Exception", o.referenceBody.displayName.LocalizeRemoveGender()) + "(" +
                                             o.referenceBody.Radius.ToSI(3) +
                                             "m)"); //cannot make Semi-Major Axis less than twice the burn altitude plus the radius of <<1>>
            }

            return new List<ManeuverParameters> { new ManeuverParameters(OrbitalManeuverCalculator.DeltaVForSemiMajorAxis(o, ut, NewSma), ut) };
        }
    }
}
