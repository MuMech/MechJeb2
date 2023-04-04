using System.Collections.Generic;
using JetBrains.Annotations;
using KSP.Localization;
using static MechJebLib.Utils.Statics;

namespace MuMech
{
    [UsedImplicitly]
    public class OperationEllipticize : Operation
    {
        public override string GetName() { return Localizer.Format("#MechJeb_both_title"); } //change both Pe and Ap

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.Global)]
        public EditableDoubleMult NewApA = new EditableDoubleMult(200000, 1000);

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.Global)]
        public EditableDoubleMult NewPeA = new EditableDoubleMult(100000, 1000);

        private readonly TimeSelector _timeSelector;

        public OperationEllipticize()
        {
            _timeSelector = new TimeSelector(new[] { TimeReference.X_FROM_NOW });
        }

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_both_label1"), NewPeA, "km"); //New periapsis:
            GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_both_label2"), NewApA, "km"); //New apoapsis:
            _timeSelector.DoChooseTimeGUI();
        }

        protected override List<ManeuverParameters> MakeNodesImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            double ut = _timeSelector.ComputeManeuverTime(o, universalTime, target);

            string burnAltitude = (o.Radius(ut) - o.referenceBody.Radius).ToSI() + "m";
            if (o.referenceBody.Radius + NewPeA > o.Radius(ut))
            {
                throw new OperationException(Localizer.Format("#MechJeb_both_Exception1",
                    burnAltitude)); //new periapsis cannot be higher than the altitude of the burn (<<1>>)
            }

            if (o.referenceBody.Radius + NewApA < o.Radius(ut))
            {
                throw new OperationException(Localizer.Format("#MechJeb_both_Exception2") + "(" + burnAltitude +
                                             ")"); //new apoapsis cannot be lower than the altitude of the burn
            }

            if (NewPeA < -o.referenceBody.Radius)
            {
                throw new OperationException(Localizer.Format("#MechJeb_both_Exception3", o.referenceBody.displayName.LocalizeRemoveGender()) + "(-" +
                                             o.referenceBody.Radius.ToSI(3) + "m)"); //"new periapsis cannot be lower than minus the radius of <<1>>"
            }

            return new List<ManeuverParameters>
            {
                new ManeuverParameters(
                    OrbitalManeuverCalculator.DeltaVToEllipticize(o, ut, NewPeA + o.referenceBody.Radius, NewApA + o.referenceBody.Radius), ut)
            };
        }

        public TimeSelector GetTimeSelector() //Required for scripts to save configuration
        {
            return _timeSelector;
        }
    }
}
