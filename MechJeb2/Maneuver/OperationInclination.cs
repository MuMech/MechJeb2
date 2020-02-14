using KSP.Localization;
using System.Collections.Generic;

namespace MuMech
{
    public class OperationInclination : Operation
    {
        public override string getName() { return Localizer.Format("#MechJeb_inclination_title");}//change inclination

        [Persistent(pass = (int)Pass.Global)]
        public EditableDouble newInc = 0;
        private TimeSelector timeSelector;

        public OperationInclination ()
        {
            timeSelector = new TimeSelector(new TimeReference[]
                    {
                    TimeReference.EQ_HIGHEST_AD, TimeReference.EQ_NEAREST_AD,
                    TimeReference.EQ_ASCENDING, TimeReference.EQ_DESCENDING,
                    TimeReference.X_FROM_NOW
                    });
        }

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_inclination_label"), newInc, "º");//New inclination:
            timeSelector.DoChooseTimeGUI();
        }

        public override List<ManeuverParameters> MakeNodesImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            double UT = timeSelector.ComputeManeuverTime(o, universalTime, target);

            List<ManeuverParameters> NodeList = new List<ManeuverParameters>();
            NodeList.Add(new ManeuverParameters(OrbitalManeuverCalculator.DeltaVToChangeInclination(o, UT, newInc), UT));

            return NodeList;
        }

        public TimeSelector getTimeSelector() //Required for scripts to save configuration
        {
            return this.timeSelector;
        }
    }
}
