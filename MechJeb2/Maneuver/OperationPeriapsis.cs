using System.Collections.Generic;
using JetBrains.Annotations;
using KSP.Localization;
using static MechJebLib.Utils.Statics;

namespace MuMech
{
    [UsedImplicitly]
    public class OperationPeriapsis : Operation
    {
        public override string GetName() { return Localizer.Format("#MechJeb_Pe_title"); } //change periapsis

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.Global)]
        public readonly EditableDoubleMult NewPeA = new EditableDoubleMult(100000, 1000);

        private readonly TimeSelector _timeSelector;

        public OperationPeriapsis()
        {
            _timeSelector = new TimeSelector(new[] { TimeReference.APOAPSIS, TimeReference.PERIAPSIS, TimeReference.X_FROM_NOW });
        }

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_Pe_label"), NewPeA, "km"); //New periapsis:
            _timeSelector.DoChooseTimeGUI();
        }

        protected override List<ManeuverParameters> MakeNodesImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            double ut = _timeSelector.ComputeManeuverTime(o, universalTime, target);

            if (o.referenceBody.Radius + NewPeA > o.Radius(ut))
            {
                string burnAltitude = (o.Radius(ut) - o.referenceBody.Radius).ToSI() + "m";
                throw new OperationException(Localizer.Format("#MechJeb_Pe_Exception1") + " (" + burnAltitude +
                                             ")"); //new periapsis cannot be higher than the altitude of the burn
            }

            if (NewPeA < -o.referenceBody.Radius)
            {
                throw new OperationException(Localizer.Format("#MechJeb_Pe_Exception2", o.referenceBody.displayName.LocalizeRemoveGender()) + "(-" +
                                             o.referenceBody.Radius.ToSI(3) + "m)"); //new periapsis cannot be lower than minus the radius of <<1>>
            }

            return new List<ManeuverParameters>
            {
                new ManeuverParameters(OrbitalManeuverCalculator.DeltaVToChangePeriapsis(o, ut, NewPeA + o.referenceBody.Radius), ut)
            };
            
        }

        public TimeSelector GetTimeSelector() //Required for scripts to save configuration
        {
            return _timeSelector;
        }
    }
}
