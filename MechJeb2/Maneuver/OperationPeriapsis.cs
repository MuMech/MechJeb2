using System;

namespace MuMech
{
    public class OperationPeriapsis : Operation
    {
        public override string getName() { return "change periapsis";}

        [Persistent(pass = (int)Pass.Global)]
        public EditableDoubleMult newPeA = new EditableDoubleMult(100000, 1000);
        private TimeSelector timeSelector;

        public OperationPeriapsis ()
        {
            timeSelector = new TimeSelector(new TimeReference[] {TimeReference.APOAPSIS, TimeReference.PERIAPSIS, TimeReference.X_FROM_NOW});
        }

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            GuiUtils.SimpleTextBox("New periapsis:", newPeA, "km");
            timeSelector.DoChooseTimeGUI();
        }

        public override ManeuverParameters MakeNodeImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            double UT = timeSelector.ComputeManeuverTime(o, universalTime, target);

            if (o.referenceBody.Radius + newPeA > o.Radius(UT))
            {
                string burnAltitude = MuUtils.ToSI(o.Radius(UT) - o.referenceBody.Radius) + "m";
                throw new OperationException("new periapsis cannot be higher than the altitude of the burn (" + burnAltitude + ")");
            }
            else if (newPeA < -o.referenceBody.Radius)
            {
                throw new OperationException("new periapsis cannot be lower than minus the radius of " + o.referenceBody.theName + "(-" + MuUtils.ToSI(o.referenceBody.Radius, 3) + "m)");
            }

            return new ManeuverParameters(OrbitalManeuverCalculator.DeltaVToChangePeriapsis(o, UT, newPeA + o.referenceBody.Radius), UT);
        }
    }
}

