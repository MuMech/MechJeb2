using KSP.Localization;
using System.Collections.Generic;

namespace MuMech
{
    public class OperationEllipticize : Operation
    {
        public override string getName() { return Localizer.Format("#MechJeb_both_title");}//change both Pe and Ap

        [Persistent(pass = (int)Pass.Global)]
        public EditableDoubleMult newApA = new EditableDoubleMult(200000, 1000);
        [Persistent(pass = (int)Pass.Global)]
        public EditableDoubleMult newPeA = new EditableDoubleMult(100000, 1000);

        private TimeSelector timeSelector;

        public OperationEllipticize ()
        {
            timeSelector = new TimeSelector(new TimeReference[] {TimeReference.X_FROM_NOW});
        }

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_both_label1"), newPeA, "km");//New periapsis:
            GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_both_label2"), newApA, "km");//New apoapsis:
            timeSelector.DoChooseTimeGUI();
        }

        public override List<ManeuverParameters> MakeNodesImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            double UT = timeSelector.ComputeManeuverTime(o, universalTime, target);

            string burnAltitude = MuUtils.ToSI(o.Radius(UT) - o.referenceBody.Radius) + "m";
            if (o.referenceBody.Radius + newPeA > o.Radius(UT))
            {
                throw new OperationException(Localizer.Format("#MechJeb_both_Exception1",burnAltitude));//new periapsis cannot be higher than the altitude of the burn (<<1>>)
            }
            else if (o.referenceBody.Radius + newApA < o.Radius(UT))
            {
                throw new OperationException(Localizer.Format("#MechJeb_both_Exception2") + "(" + burnAltitude + ")");//new apoapsis cannot be lower than the altitude of the burn
            }
            else if (newPeA < -o.referenceBody.Radius)
            {
                throw new OperationException(Localizer.Format("#MechJeb_both_Exception3",o.referenceBody.displayName) + "(-" + MuUtils.ToSI(o.referenceBody.Radius, 3) + "m)");//"new periapsis cannot be lower than minus the radius of <<1>>"
            }

            List<ManeuverParameters> NodeList = new List<ManeuverParameters>();
            NodeList.Add( new ManeuverParameters(OrbitalManeuverCalculator.DeltaVToEllipticize(o, UT, newPeA + o.referenceBody.Radius, newApA + o.referenceBody.Radius), UT));
            return NodeList;
        }

        public TimeSelector getTimeSelector() //Required for scripts to save configuration
        {
            return this.timeSelector;
        }
    }
}
