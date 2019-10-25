using KSP.Localization;
namespace MuMech
{
    public class OperationApoapsis : Operation
    {
        public override string getName() { return Localizer.Format("#MechJeb_Ap_title");}//change apoapsis

        [Persistent(pass = (int)Pass.Global)]
        public EditableDoubleMult newApA = new EditableDoubleMult(200000, 1000);
        private TimeSelector timeSelector;

        public OperationApoapsis ()
        {
            timeSelector = new TimeSelector(new TimeReference[] {TimeReference.PERIAPSIS, TimeReference.APOAPSIS, TimeReference.X_FROM_NOW, TimeReference.EQ_DESCENDING, TimeReference.EQ_ASCENDING});
        }

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_Ap_label1"), newApA, "km");//New apoapsis:
            timeSelector.DoChooseTimeGUI();
        }

        public override ManeuverParameters MakeNodeImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            double UT = timeSelector.ComputeManeuverTime(o, universalTime, target);
            if (o.referenceBody.Radius + newApA < o.Radius(UT))
            {
                string burnAltitude = MuUtils.ToSI(o.Radius(UT) - o.referenceBody.Radius) + "m";
                throw new OperationException(Localizer.Format("#MechJeb_Ap_Exception",burnAltitude));//new apoapsis cannot be lower than the altitude of the burn (<<1>>)
            }

            return new ManeuverParameters(OrbitalManeuverCalculator.DeltaVToChangeApoapsis(o, UT, newApA + o.referenceBody.Radius), UT);
        }

		public TimeSelector getTimeSelector() //Required for scripts to save configuration
		{
			return this.timeSelector;
		}
    }
}

