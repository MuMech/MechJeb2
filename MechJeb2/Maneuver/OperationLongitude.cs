using System.Collections.Generic;
using JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class OperationLongitude : Operation
    {
        private static readonly string _name = Localizer.Format("#MechJeb_la_title");
        public override         string GetName() => _name;

        private static readonly TimeReference[] _timeReferences = { TimeReference.APOAPSIS, TimeReference.PERIAPSIS };

        private static readonly TimeSelector _timeSelector = new TimeSelector(_timeReferences);

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

            return new List<ManeuverParameters> { new ManeuverParameters(dV, ut) };
        }
    }
}
