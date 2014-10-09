using System;
using UnityEngine;

namespace MuMech
{
    public class OperationCircularize : Operation
    {

        public override string getName() {return "circularize";}

        private TimeSelector timeSelector;

        public OperationCircularize()
        {
            timeSelector = new TimeSelector(new TimeReference[] { TimeReference.APOAPSIS, TimeReference.PERIAPSIS, TimeReference.ALTITUDE, TimeReference.X_FROM_NOW });
        }

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            timeSelector.DoChooseTimeGUI();
        }

        public override ManeuverParameters MakeNodeImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            double UT = timeSelector.ComputeManeuverTime(o, universalTime, target);
            return new ManeuverParameters(OrbitalManeuverCalculator.DeltaVToCircularize(o, UT), UT);
        }
        
    }
}

