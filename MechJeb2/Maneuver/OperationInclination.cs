extern alias JetBrainsAnnotations;
using System;
using System.Collections.Generic;
using JetBrainsAnnotations::JetBrains.Annotations;
using KSP.Localization;
using MechJebLib.Functions;
using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;

namespace MuMech
{
    [UsedImplicitly]
    public class OperationInclination : Operation
    {
        private static readonly string _name = Localizer.Format("#MechJeb_inclination_title");
        public override string GetName() => _name;

        [UsedImplicitly, Persistent(pass = (int)Pass.GLOBAL)]
        public EditableDouble NewInc = 0;

        private static readonly TimeReference[] _timeReferences =
        {
            TimeReference.EQ_HIGHEST_AD, TimeReference.EQ_NEAREST_AD, TimeReference.EQ_ASCENDING, TimeReference.EQ_DESCENDING, TimeReference.APOAPSIS,
            TimeReference.PERIAPSIS, TimeReference.X_FROM_NOW, TimeReference.ALTITUDE
        };

        internal static readonly TimeSelector _timeSelector = new TimeSelector(_timeReferences);

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_inclination_label"), NewInc, "º"); //New inclination:
            _timeSelector.DoChooseTimeGUI();
        }

        protected override List<ManeuverParameters> MakeNodesImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            double ut = _timeSelector.ComputeManeuverTime(o, universalTime, target);

            // away from the equatorial nodes the latitude of the burn bounds the inclination to [ |lat|, 180 - |lat| ]
            (V3 r, _) = o.RightHandedStateVectorsAtUT(ut);
            double lat = Rad2Deg(Math.Abs(Astro.LatitudeFromBCI(r)));
            double inc = Math.Abs(Clamp180(NewInc));
            if (inc < lat - 0.1 || inc > 180 - lat + 0.1)
                ErrorMessage = Localizer.Format("#MechJeb_inclination_error", lat.ToString("F1"),
                    (inc < 90 ? lat : 180 - lat).ToString("F1")); //"Warning: burn point is at latitude <<1>>º, so the closest reachable inclination is <<2>>º"

            return new List<ManeuverParameters> { new ManeuverParameters(OrbitalManeuverCalculator.DeltaVToChangeInclination(o, ut, NewInc), ut) };
        }
    }
}
