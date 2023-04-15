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

            var now = Planetarium.GetUniversalTime();
            var planetmu = o.referenceBody.referenceBody.gravParameter;
            var moonmu = o.referenceBody.gravParameter;
            var moonr0 = o.referenceBody.orbit.getRelativePositionAtUT(now);
            var moonv0 = o.referenceBody.orbit.getOrbitalVelocityAtUT(now);
            var moonsoi = o.referenceBody.sphereOfInfluence;
            var r0 = o.getRelativePositionAtUT(now);
            var v0 = o.getOrbitalVelocityAtUT(now);

            Debug.Log($"ManeuverToReturnFromMoon({planetmu}, {moonmu}, {moonr0}, {moonv0}, {moonsoi}, {r0}, {v0}, double peR, double inc)");

            (Vector3d dV, double ut) = OrbitalManeuverCalculator.DeltaVAndTimeForMoonReturnEjection(o, universalTime,
                o.referenceBody.referenceBody.Radius + MoonReturnAltitude);

            Debug.Log($"dv: {dV.xzy} ut: {ut} now: {now}");

            return new List<ManeuverParameters>
            {
                new ManeuverParameters(dV, ut)
            };
        }
    }
}
