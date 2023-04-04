using System.Collections.Generic;
using JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class OperationLongitude : Operation
    {
        public override string GetName() { return Localizer.Format("#MechJeb_la_title"); } //change surface longitude of apsis

        private readonly TimeSelector _timeSelector;

        public OperationLongitude()
        {
            _timeSelector = new TimeSelector(new[] { TimeReference.APOAPSIS, TimeReference.PERIAPSIS });
        }

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            _timeSelector.DoChooseTimeGUI();
            GUILayout.Label(Localizer.Format("#MechJeb_la_label")); //New Surface Longitude after one orbit:
            target.targetLongitude.DrawEditGUI(EditableAngle.Direction.EW);
        }

        protected override List<ManeuverParameters> MakeNodesImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            double ut = _timeSelector.ComputeManeuverTime(o, universalTime, target);
            Vector3d dV = OrbitalManeuverCalculator.DeltaVToShiftNodeLongitude(o, ut, target.targetLongitude);

            return new List<ManeuverParameters>
            {
                new ManeuverParameters(dV, ut)
            };
        }

        public TimeSelector GetTimeSelector() //Required for scripts to save configuration
        {
            return _timeSelector;
        }
    }
}
