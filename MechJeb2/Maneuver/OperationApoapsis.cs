using System;

namespace MuMech
{
    public class OperationApoapsis : Operation
    {
        public override string getName() { return "change apoapsis";}

        [Persistent(pass = (int)Pass.Global)]
        public EditableDoubleMult newApA = new EditableDoubleMult(200000, 1000);
        private TimeSelector timeSelector;

        public OperationApoapsis ()
        {
            timeSelector = new TimeSelector(new TimeReference[] {TimeReference.PERIAPSIS, TimeReference.APOAPSIS, TimeReference.X_FROM_NOW, TimeReference.EQ_DESCENDING, TimeReference.EQ_ASCENDING});
        }

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            GuiUtils.SimpleTextBox("New apoapsis:", newApA, "km");
            timeSelector.DoChooseTimeGUI();
        }

        public override ManeuverParameters MakeNodeImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            double UT = timeSelector.ComputeManeuverTime(o, universalTime, target);
            if (o.referenceBody.Radius + newApA < o.Radius(UT))
            {
                string burnAltitude = MuUtils.ToSI(o.Radius(UT) - o.referenceBody.Radius) + "m";
                throw new OperationException("new apoapsis cannot be lower than the altitude of the burn (" + burnAltitude + ")");
            }

            return new ManeuverParameters(OrbitalManeuverCalculator.DeltaVToChangeApoapsis(o, UT, newApA + o.referenceBody.Radius), UT);
        }
    }
}

