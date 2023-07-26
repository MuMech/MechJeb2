using System.Collections.Generic;
using JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class OperationCourseCorrection : Operation
    {
        private static readonly string _name = Localizer.Format("#MechJeb_approach_title");
        public override         string GetName() => _name;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public EditableDoubleMult CourseCorrectFinalPeA = new EditableDoubleMult(200000, 1000);

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public EditableDoubleMult InterceptDistance = new EditableDoubleMult(200);

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            if (target.Target is CelestialBody)
                GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_approach_label1"), CourseCorrectFinalPeA, "km"); //Approximate final periapsis
            else
                GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_approach_label2"), InterceptDistance, "m"); //Closest approach distance
            GUILayout.Label(Localizer.Format("#MechJeb_approach_label3")); //Schedule the burn to minimize the required ΔV.
        }

        protected override List<ManeuverParameters> MakeNodesImpl(Orbit o, double ut, MechJebModuleTargetController target)
        {
            if (!target.NormalTargetExists)
                throw new OperationException(Localizer.Format("#MechJeb_approach_Exception1")); //must select a target for the course correction.

            Orbit correctionPatch = o;
            while (correctionPatch != null)
            {
                if (correctionPatch.referenceBody == target.TargetOrbit.referenceBody)
                {
                    o  = correctionPatch;
                    ut = correctionPatch.StartUT;
                    break;
                }

                correctionPatch = target.Core.vessel.GetNextPatch(correctionPatch);
            }

            if (correctionPatch == null || correctionPatch.referenceBody != target.TargetOrbit.referenceBody)
                throw
                    new OperationException(
                        Localizer.Format("#MechJeb_approach_Exception2")); //"target for course correction must be in the same sphere of influence"

            if (o.NextClosestApproachTime(target.TargetOrbit, ut) < ut + 1 ||
                o.NextClosestApproachDistance(target.TargetOrbit, ut) > target.TargetOrbit.semiMajorAxis * 0.2)
            {
                ErrorMessage = Localizer.Format(
                    "#MechJeb_Approach_errormsg"); //Warning: orbit before course correction doesn't seem to approach target very closely. Planned course correction may be extreme. Recommend plotting an approximate intercept orbit and then plotting a course correction.
            }

            var targetBody = target.Target as CelestialBody;
            Vector3d dV = targetBody != null
                ? OrbitalManeuverCalculator.DeltaVAndTimeForCheapestCourseCorrection(o, ut, target.TargetOrbit, targetBody,
                    targetBody.Radius + CourseCorrectFinalPeA, out ut)
                : OrbitalManeuverCalculator.DeltaVAndTimeForCheapestCourseCorrection(o, ut, target.TargetOrbit, InterceptDistance, out ut);


            return new List<ManeuverParameters> { new ManeuverParameters(dV, ut) };
        }
    }
}
