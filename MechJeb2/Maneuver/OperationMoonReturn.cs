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
            if (o.referenceBody.referenceBody == null)
            {
                throw new OperationException(Localizer.Format("#MechJeb_return_Exception",
                    o.referenceBody.displayName.LocalizeRemoveGender())); //<<1>> is not orbiting another body you could return to.
            }

            // fixed 30 second delay for hyperbolic orbits (this doesn't work for elliptical and i don't want to deal
            // with requests to "fix" it by surfacing it as a tweakable).
            double t0 = o.eccentricity >= 1 ? universalTime + 30 : universalTime;

            (Vector3d dV, double ut) = OrbitalManeuverCalculator.DeltaVAndTimeForMoonReturnEjection(o, t0,
                o.referenceBody.referenceBody.Radius + MoonReturnAltitude);

            return new List<ManeuverParameters>
            {
                new ManeuverParameters(dV, ut)
            };
        }
    }
}
