using System.Collections.Generic;
using JetBrains.Annotations;
using KSP.Localization;

namespace MuMech
{
    [UsedImplicitly]
    public class OperationPlane : Operation
    {
        private static readonly string _name = Localizer.Format("#MechJeb_match_planes_title");
        public override         string GetName() => _name;

        private static readonly TimeReference[] _timeReferences =
        {
            TimeReference.REL_HIGHEST_AD, TimeReference.REL_NEAREST_AD, TimeReference.REL_ASCENDING, TimeReference.REL_DESCENDING
        };

        private static readonly TimeSelector _timeSelector = new TimeSelector(_timeReferences);

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target) => _timeSelector.DoChooseTimeGUI();

        protected override List<ManeuverParameters> MakeNodesImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            double ut = _timeSelector.ComputeManeuverTime(o, universalTime, target);

            if (!target.NormalTargetExists)
            {
                throw new OperationException(Localizer.Format("#MechJeb_match_planes_Exception1")); //must select a target to match planes with.
            }

            if (o.referenceBody != target.TargetOrbit.referenceBody)
            {
                throw
                    new OperationException(
                        Localizer.Format("#MechJeb_match_planes_Exception2")); //can only match planes with an object in the same sphere of influence.
            }

            bool anExists = o.AscendingNodeExists(target.TargetOrbit);
            bool dnExists = o.DescendingNodeExists(target.TargetOrbit);
            double anTime = 0;
            double dnTime = 0;
            Vector3d anDeltaV = anExists
                ? OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesAscending(o, target.TargetOrbit, ut, out anTime)
                : Vector3d.zero;
            Vector3d dnDeltaV = anExists
                ? OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesDescending(o, target.TargetOrbit, ut, out dnTime)
                : Vector3d.zero;
            Vector3d dV;

            if (_timeSelector.TimeReference == TimeReference.REL_ASCENDING)
            {
                if (!anExists)
                {
                    throw new OperationException(Localizer.Format("#MechJeb_match_planes_Exception3")); //ascending node with target doesn't exist.
                }

                ut = anTime;
                dV = anDeltaV;
            }
            else if (_timeSelector.TimeReference == TimeReference.REL_DESCENDING)
            {
                if (!dnExists)
                {
                    throw new OperationException(Localizer.Format("#MechJeb_match_planes_Exception4")); //descending node with target doesn't exist.
                }

                ut = dnTime;
                dV = dnDeltaV;
            }
            else if (_timeSelector.TimeReference == TimeReference.REL_NEAREST_AD)
            {
                if (!anExists && !dnExists)
                {
                    throw new OperationException(
                        Localizer.Format("#MechJeb_match_planes_Exception5")); //neither ascending nor descending node with target exists.
                }

                if (!dnExists || anTime <= dnTime)
                {
                    ut = anTime;
                    dV = anDeltaV;
                }
                else
                {
                    ut = dnTime;
                    dV = dnDeltaV;
                }
            }
            else if (_timeSelector.TimeReference == TimeReference.REL_HIGHEST_AD)
            {
                if (!anExists && !dnExists)
                {
                    throw new OperationException(
                        Localizer.Format("#MechJeb_match_planes_Exception5")); //neither ascending nor descending node with target exists.
                }

                if (!dnExists || anDeltaV.magnitude <= dnDeltaV.magnitude)
                {
                    ut = anTime;
                    dV = anDeltaV;
                }
                else
                {
                    ut = dnTime;
                    dV = dnDeltaV;
                }
            }
            else
            {
                throw new OperationException(Localizer.Format("#MechJeb_match_planes_Exception6")); //wrong time reference.
            }

            return new List<ManeuverParameters> { new ManeuverParameters(dV, ut) };
        }
    }
}
