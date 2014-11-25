using System;
using UnityEngine;

namespace MuMech
{
    public class OperationSemiMajor : Operation
    {
        public override string getName() { return "change semi-major axis";}

        [Persistent(pass = (int)Pass.Global)]
        public EditableDoubleMult newSMA = new EditableDoubleMult(800000, 1000);
        private TimeSelector timeSelector;

        public OperationSemiMajor ()
        {
            timeSelector = new TimeSelector(new TimeReference[] {TimeReference.X_FROM_NOW, TimeReference.APOAPSIS, TimeReference.PERIAPSIS});
        }

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            GuiUtils.SimpleTextBox ("New Semi-Major Axis:", newSMA, "km");
            timeSelector.DoChooseTimeGUI();
        }

        public override ManeuverParameters MakeNodeImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            double UT = timeSelector.ComputeManeuverTime(o, universalTime, target);

            if (2*newSMA > o.Radius(UT) + o.referenceBody.sphereOfInfluence)
            {
                errorMessage = "Warning: new Semi-Major Axis is very large, and may result in a hyberbolic orbit";
            }

            if(o.Radius(UT) > 2*newSMA)
            {
                throw new OperationException("cannot make Semi-Major Axis less than twice the burn altitude plus the radius of " + o.referenceBody.theName + "(" + MuUtils.ToSI(o.referenceBody.Radius, 3) + "m)");
            }

            return new ManeuverParameters(OrbitalManeuverCalculator.DeltaVForSemiMajorAxis (o, UT, newSMA), UT);
        }
    }
}

