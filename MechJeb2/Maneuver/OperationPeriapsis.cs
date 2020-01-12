using KSP.Localization;
using System.Collections.Generic;

namespace MuMech
{
    public class OperationPeriapsis : Operation
    {
        public override string getName() { return Localizer.Format("#MechJeb_Pe_title");}//change periapsis

        [Persistent(pass = (int)Pass.Global)]
        public EditableDoubleMult newPeA = new EditableDoubleMult(100000, 1000);
        private TimeSelector timeSelector;

        public OperationPeriapsis ()
        {
            timeSelector = new TimeSelector(new TimeReference[] {TimeReference.APOAPSIS, TimeReference.PERIAPSIS, TimeReference.X_FROM_NOW});
        }

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_Pe_label"), newPeA, "km");//New periapsis:
            timeSelector.DoChooseTimeGUI();
        }

        public override List<ManeuverParameters> MakeNodesImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            double UT = timeSelector.ComputeManeuverTime(o, universalTime, target);

            if (o.referenceBody.Radius + newPeA > o.Radius(UT))
            {
                string burnAltitude = MuUtils.ToSI(o.Radius(UT) - o.referenceBody.Radius) + "m";
                throw new OperationException(Localizer.Format("#MechJeb_Pe_Exception1") +" (" + burnAltitude + ")");//new periapsis cannot be higher than the altitude of the burn
            }
            else if (newPeA < -o.referenceBody.Radius)
            {
                throw new OperationException(Localizer.Format("#MechJeb_Pe_Exception2",o.referenceBody.displayName) + "(-" + MuUtils.ToSI(o.referenceBody.Radius, 3) + "m)");//new periapsis cannot be lower than minus the radius of <<1>>
            }

            List<ManeuverParameters> NodeList = new List<ManeuverParameters>();
            NodeList.Add(new ManeuverParameters(OrbitalManeuverCalculator.DeltaVToChangePeriapsis(o, UT, newPeA + o.referenceBody.Radius), UT));

            return NodeList;
        }

        public TimeSelector getTimeSelector() //Required for scripts to save configuration
        {
            return this.timeSelector;
        }
    }
}
