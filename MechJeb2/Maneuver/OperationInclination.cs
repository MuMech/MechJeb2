using System;

namespace MuMech
{
    public class OperationInclination : Operation
    {
        public override string getName() { return "change inclination";}

        [Persistent(pass = (int)Pass.Global)]
        public EditableDouble newInc = 0;
        private TimeSelector timeSelector;

        public OperationInclination ()
        {
            timeSelector = new TimeSelector(new TimeReference[] { TimeReference.EQ_ASCENDING, TimeReference.EQ_DESCENDING, TimeReference.X_FROM_NOW });
        }

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            GuiUtils.SimpleTextBox("New inclination:", newInc, "º");
            timeSelector.DoChooseTimeGUI();
        }

        public override ManeuverParameters MakeNodeImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            double UT = timeSelector.ComputeManeuverTime(o, universalTime, target);

            return new ManeuverParameters(OrbitalManeuverCalculator.DeltaVToChangeInclination(o, UT, newInc), UT);
        }
    }
}

