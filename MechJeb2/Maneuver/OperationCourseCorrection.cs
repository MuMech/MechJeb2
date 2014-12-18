using System;
using UnityEngine;

namespace MuMech
{
    public class OperationCourseCorrection : Operation
    {
        public override string getName() { return "fine tune closest approach to target";}

        [Persistent(pass = (int)Pass.Global)]
        public EditableDoubleMult courseCorrectFinalPeA = new EditableDoubleMult(200000, 1000);
        [Persistent(pass = (int)Pass.Global)]
        public EditableDoubleMult interceptDistance = new EditableDoubleMult(200, 1);

        public OperationCourseCorrection ()
        {
        }

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            if (target.Target is CelestialBody)
                GuiUtils.SimpleTextBox("Approximate final periapsis", courseCorrectFinalPeA, "km");
            else
                GuiUtils.SimpleTextBox("Closest approach distance", interceptDistance, "m");
            GUILayout.Label("Schedule the burn to minimize the required ΔV.");
        }

        public override ManeuverParameters MakeNodeImpl(Orbit o, double UT, MechJebModuleTargetController target)
        {
            if (!target.NormalTargetExists)
                throw new OperationException("must select a target for the course correction.");

            Orbit correctionPatch = o;
            while (correctionPatch != null)
            {
                if (correctionPatch.referenceBody == target.TargetOrbit.referenceBody)
                {
                    o = correctionPatch;
                    UT = correctionPatch.StartUT;
                    break;
                }
                correctionPatch = target.core.vessel.GetNextPatch(correctionPatch);
            }

            if (correctionPatch == null || correctionPatch.referenceBody != target.TargetOrbit.referenceBody)
                throw new OperationException("target for course correction must be in the same sphere of influence");

            if (o.NextClosestApproachTime(target.TargetOrbit, UT) < UT + 1 ||
                    o.NextClosestApproachDistance(target.TargetOrbit, UT) > target.TargetOrbit.semiMajorAxis * 0.2)
            {
                errorMessage = "Warning: orbit before course correction doesn't seem to approach target very closely. Planned course correction may be extreme. Recommend plotting an approximate intercept orbit and then plotting a course correction.";
            }

            CelestialBody targetBody = target.Target as CelestialBody;
            Vector3d dV = targetBody != null ?
                OrbitalManeuverCalculator.DeltaVAndTimeForCheapestCourseCorrection(o, UT, target.TargetOrbit, targetBody, targetBody.Radius + courseCorrectFinalPeA, out UT):
                OrbitalManeuverCalculator.DeltaVAndTimeForCheapestCourseCorrection(o, UT, target.TargetOrbit, interceptDistance, out UT);


            return new ManeuverParameters(dV, UT);
        }
    }
}

