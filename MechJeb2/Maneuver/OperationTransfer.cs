using System.Collections.Generic;
using JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class OperationGeneric : Operation
    {
        private static readonly string _name = Localizer.Format("#MechJeb_Hohm_title");
        public override         string GetName() => _name;

        [UsedImplicitly]
        public bool InterceptOnly;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public EditableDouble PeriodOffset = 0;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public EditableTime MinDepartureUT = 0;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public EditableTime MaxDepartureUT = 0;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool SimpleTransfer;

        private static readonly TimeReference[] _timeReferences =
        {
            TimeReference.COMPUTED, TimeReference.PERIAPSIS, TimeReference.APOAPSIS, TimeReference.X_FROM_NOW, TimeReference.ALTITUDE,
            TimeReference.EQ_DESCENDING, TimeReference.EQ_ASCENDING, TimeReference.REL_NEAREST_AD, TimeReference.REL_ASCENDING,
            TimeReference.REL_DESCENDING, TimeReference.CLOSEST_APPROACH
        };

        private static readonly TimeSelector _timeSelector = new TimeSelector(_timeReferences);

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            InterceptOnly =
                GUILayout.Toggle(InterceptOnly, Localizer.Format("#MechJeb_Hohm_intercept_only")); //intercept only, no capture burn (impact/flyby)
            SimpleTransfer = GUILayout.Toggle(SimpleTransfer, Localizer.Format("#MechJeb_Hohm_simpleTransfer")); //simple coplanar Hohmann transfer
            GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_Hohm_Label1"), PeriodOffset); //fractional target period offset
            if (!SimpleTransfer)
            {
                _timeSelector.DoChooseTimeGUI();
            }
        }

        protected override List<ManeuverParameters> MakeNodesImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            double ut;

            if (!target.NormalTargetExists)
            {
                throw new OperationException(Localizer.Format("#MechJeb_Hohm_Exception1")); //must select a target for the bi-impulsive transfer.
            }

            if (o.referenceBody != target.TargetOrbit.referenceBody)
            {
                throw
                    new OperationException(
                        Localizer.Format("#MechJeb_Hohm_Exception2")); //target for bi-impulsive transfer must be in the same sphere of influence.
            }

            Vector3d dV;

            Orbit targetOrbit = target.TargetOrbit;

            if (PeriodOffset != 0)
            {
                targetOrbit = target.TargetOrbit.Clone();
                targetOrbit.MutatedOrbit(PeriodOffset);
            }

            if (SimpleTransfer)
            {
                dV = OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(o, targetOrbit, universalTime, out ut);
            }
            else
            {
                if (_timeSelector.TimeReference == TimeReference.COMPUTED)
                {
                    dV = OrbitalManeuverCalculator.DeltaVAndTimeForBiImpulsiveAnnealed(o, targetOrbit, universalTime, out ut,
                        intercept_only: InterceptOnly);
                }
                else
                {
                    bool anExists = o.AscendingNodeExists(target.TargetOrbit);
                    bool dnExists = o.DescendingNodeExists(target.TargetOrbit);

                    if (_timeSelector.TimeReference == TimeReference.REL_ASCENDING && !anExists)
                    {
                        throw new OperationException(Localizer.Format("#MechJeb_Hohm_Exception3")); //ascending node with target doesn't exist.
                    }

                    if (_timeSelector.TimeReference == TimeReference.REL_DESCENDING && !dnExists)
                    {
                        throw new OperationException(Localizer.Format("#MechJeb_Hohm_Exception4")); //descending node with target doesn't exist.
                    }

                    if (_timeSelector.TimeReference == TimeReference.REL_NEAREST_AD && !(anExists || dnExists))
                    {
                        throw new OperationException(
                            Localizer.Format("#MechJeb_Hohm_Exception5")); //neither ascending nor descending node with target exists.
                    }

                    ut = _timeSelector.ComputeManeuverTime(o, universalTime, target);
                    dV = OrbitalManeuverCalculator.DeltaVAndTimeForBiImpulsiveAnnealed(o, targetOrbit, ut, out ut, intercept_only: InterceptOnly,
                        fixed_ut: true);
                }
            }

            return new List<ManeuverParameters> { new ManeuverParameters(dV, ut) };
        }
    }
}
