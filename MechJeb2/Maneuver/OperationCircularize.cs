﻿using KSP.Localization;
using System.Collections.Generic;

namespace MuMech
{
    public class OperationCircularize : Operation
    {

        public override string getName() {return Localizer.Format("#MechJeb_Maneu_circularize_title");}//"circularize"

        private readonly TimeSelector timeSelector;

        public OperationCircularize()
        {
            timeSelector = new TimeSelector(new TimeReference[] { TimeReference.APOAPSIS, TimeReference.PERIAPSIS, TimeReference.ALTITUDE, TimeReference.X_FROM_NOW });
        }

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            timeSelector.DoChooseTimeGUI();
        }

        public override List<ManeuverParameters> MakeNodesImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            var UT = timeSelector.ComputeManeuverTime(o, universalTime, target);
            var NodeList = new List<ManeuverParameters>();
            NodeList.Add( new ManeuverParameters(OrbitalManeuverCalculator.DeltaVToCircularize(o, UT), UT) );
            return NodeList;
        }

        public TimeSelector getTimeSelector() //Required for scripts to save configuration
        {
            return this.timeSelector;
        }
    }
}
