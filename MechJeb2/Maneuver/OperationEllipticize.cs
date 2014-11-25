using System;

namespace MuMech
{
    public class OperationEllipticize : Operation
    {
        public override string getName() { return "change both Pe and Ap";}

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
            GuiUtils.SimpleTextBox("New periapsis:", newPeA, "km");
            GuiUtils.SimpleTextBox("New apoapsis:", newApA, "km");
            timeSelector.DoChooseTimeGUI();
        }

        public override ManeuverParameters MakeNodeImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            double UT = timeSelector.ComputeManeuverTime(o, universalTime, target);

            string burnAltitude = MuUtils.ToSI(o.Radius(UT) - o.referenceBody.Radius) + "m";
            if (o.referenceBody.Radius + newPeA > o.Radius(UT))
            {
                throw new OperationException("new periapsis cannot be higher than the altitude of the burn (" + burnAltitude + ")");
            }
            else if (o.referenceBody.Radius + newApA < o.Radius(UT))
            {
                throw new OperationException("new apoapsis cannot be lower than the altitude of the burn (" + burnAltitude + ")");
            }
            else if (newPeA < -o.referenceBody.Radius)
            {
                throw new OperationException("new periapsis cannot be lower than minus the radius of " + o.referenceBody.theName + "(-" + MuUtils.ToSI(o.referenceBody.Radius, 3) + "m)");
            }

            return new ManeuverParameters(OrbitalManeuverCalculator.DeltaVToEllipticize(o, UT, newPeA + o.referenceBody.Radius, newApA + o.referenceBody.Radius), UT);
        }
    }
}

