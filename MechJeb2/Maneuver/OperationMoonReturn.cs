using System.Collections.Generic;
using JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class OperationMoonReturn : Operation
    {
        public override string GetName() { return Localizer.Format("#MechJeb_return_title"); } //return from a moon

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.Global)]
        public EditableDoubleMult MoonReturnAltitude = new EditableDoubleMult(100000, 1000);

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_return_label1"), MoonReturnAltitude, "km"); //Approximate final periapsis:
            GUILayout.Label(Localizer.Format("#MechJeb_return_label2")); //Schedule the burn at the next return window.
        }

        protected override List<ManeuverParameters> MakeNodesImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            if (o.eccentricity > 0.2)
            {
                ErrorMessage = Localizer.Format("#MechJeb_return_errormsg",
                    o.eccentricity.ToString(
                        "F2")); //"Warning: Recommend starting moon returns from a near-circular orbit (eccentricity < 0.2). Planned return is starting from an orbit with eccentricity "" and so may not be accurate."
            }

            if (o.referenceBody.referenceBody == null)
            {
                throw new OperationException(Localizer.Format("#MechJeb_return_Exception",
                    o.referenceBody.displayName.LocalizeRemoveGender())); //<<1>> is not orbiting another body you could return to.
            }

            Vector3d dV = OrbitalManeuverCalculator.DeltaVAndTimeForMoonReturnEjection(o, universalTime,
                o.referenceBody.referenceBody.Radius + MoonReturnAltitude, out double ut);

            return new List<ManeuverParameters>
            {
                new ManeuverParameters(dV, ut)
            };
        }
    }
}
