using System.Collections.Generic;
using JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class OperationResonantOrbit : Operation
    {
        private static readonly string _name = Localizer.Format("#MechJeb_resonant_title");
        public override         string GetName() => _name;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public EditableInt ResonanceNumerator = 2;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public EditableInt ResonanceDenominator = 3;

        private readonly TimeSelector _timeSelector =
            new TimeSelector(new[] { TimeReference.APOAPSIS, TimeReference.PERIAPSIS, TimeReference.X_FROM_NOW, TimeReference.ALTITUDE });

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            GUILayout.Label(Localizer.Format("#MechJeb_resonant_label1",
                ResonanceNumerator.val + "/" + ResonanceDenominator.val)); //"Change your orbital period to <<1>> of your current orbital period"
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#MechJeb_resonant_label2"), GUILayout.ExpandWidth(true)); //New orbital period ratio :
            ResonanceNumerator.text = GUILayout.TextField(ResonanceNumerator.text, GUILayout.Width(30));
            GUILayout.Label("/", GUILayout.ExpandWidth(false));
            ResonanceDenominator.text = GUILayout.TextField(ResonanceDenominator.text, GUILayout.Width(30));
            GUILayout.EndHorizontal();
            _timeSelector.DoChooseTimeGUI();
        }

        protected override List<ManeuverParameters> MakeNodesImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            double ut = _timeSelector.ComputeManeuverTime(o, universalTime, target);
            Vector3d dV = OrbitalManeuverCalculator.DeltaVToResonantOrbit(o, ut, (double)ResonanceNumerator.val / ResonanceDenominator.val);

            return new List<ManeuverParameters> { new ManeuverParameters(dV, ut) };
        }
    }
}
