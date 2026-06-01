extern alias JetBrainsAnnotations;
using System.Collections.Generic;
using JetBrainsAnnotations::JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class OperationMoonReturn : Operation
    {
        private static readonly string _name = Localizer.Format("#MechJeb_return_title");
        public override         string GetName() => _name;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public EditableDoubleMult Periapsis = new EditableDoubleMult(100000, 1000);

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public EditableDouble Inclination = new EditableDouble(-90);

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool InclinationFlag;

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_return_label1"), Periapsis, "km");   //Approximate final periapsis:
            GuiUtils.ToggledTextBox(ref InclinationFlag, "Inclination", Inclination, "°");         //Inclination
            GUILayout.Label(Localizer.Format("#MechJeb_return_label2"));                           //Schedule the burn at the next return window.
        }

        protected override List<ManeuverParameters> MakeNodesImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            if (o.referenceBody.referenceBody == null)
            {
                throw new OperationException(Localizer.Format("#MechJeb_return_Exception",
                    o.referenceBody.displayName.LocalizeRemoveGender())); //<<1>> is not orbiting another body you could return to.
            }

            double per = o.referenceBody.referenceBody.Radius + Periapsis;
            double inc = InclinationFlag ? Inclination.Val : double.NaN;

            (Vector3d dV, double ut) = OrbitalManeuverCalculator.DeltaVAndTimeForMoonReturnEjection(o, universalTime, per, inc);

            return new List<ManeuverParameters> { new ManeuverParameters(dV, ut) };
        }
    }
}
