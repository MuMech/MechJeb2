using System;
using UnityEngine;

namespace MuMech
{
    public class OperationLambert : Operation
    {
        public override string getName() { return "intercept target at chosen time";}

        [Persistent(pass = (int)Pass.Global)]
        public EditableTime interceptInterval = 3600;
        private TimeSelector timeSelector;

        public OperationLambert ()
        {
            timeSelector = new TimeSelector(new TimeReference[] { TimeReference.X_FROM_NOW });
        }

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            GuiUtils.SimpleTextBox("Time after burn to intercept target:", interceptInterval);
            timeSelector.DoChooseTimeGUI();
        }

        public override ManeuverParameters MakeNodeImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            if (!target.NormalTargetExists)
                throw new OperationException("must select a target to intercept.");
            if (o.referenceBody != target.TargetOrbit.referenceBody)
                throw new OperationException("target must be in the same sphere of influence.");

            double UT = timeSelector.ComputeManeuverTime(o, universalTime, target);

            var dV = OrbitalManeuverCalculator.DeltaVToInterceptAtTime(o, UT, target.TargetOrbit, UT + interceptInterval);
            return new ManeuverParameters(dV, UT);
        }
    }
}

