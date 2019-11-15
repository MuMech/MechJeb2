using UnityEngine;
using KSP.Localization;
namespace MuMech
{
    public class OperationLongitude : Operation
    {
        public override string getName() { return Localizer.Format("#MechJeb_la_title");}//change surface longitude of apsis

        [Persistent(pass = (int)Pass.Global)]
        public EditableDouble newLAN = 0;
        private TimeSelector timeSelector;

        public OperationLongitude ()
        {
            timeSelector = new TimeSelector(new TimeReference[] { TimeReference.APOAPSIS, TimeReference.PERIAPSIS });
        }

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            timeSelector.DoChooseTimeGUI();
            GUILayout.Label(Localizer.Format("#MechJeb_la_label"));//New Surface Longitude after one orbit:
            target.targetLongitude.DrawEditGUI(EditableAngle.Direction.EW);
        }

        public override ManeuverParameters MakeNodeImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            double UT = timeSelector.ComputeManeuverTime(o, universalTime, target);
            var dV = OrbitalManeuverCalculator.DeltaVToShiftNodeLongitude(o, UT, target.targetLongitude);

            return new ManeuverParameters(dV, UT);
        }

		public TimeSelector getTimeSelector() //Required for scripts to save configuration
		{
			return this.timeSelector;
		}
    }
}

