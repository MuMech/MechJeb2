using UnityEngine;
using KSP.Localization;
using System.Collections.Generic;
namespace MuMech
{
    public class OperationCourseCorrection : Operation
    {
        public override string getName() { return Localizer.Format("#MechJeb_approach_title");}//fine tune closest approach to target

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
                GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_approach_label1"), courseCorrectFinalPeA, "km");//Approximate final periapsis
            else
                GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_approach_label2"), interceptDistance, "m");//Closest approach distance
            GUILayout.Label(Localizer.Format("#MechJeb_approach_label3"));//Schedule the burn to minimize the required ΔV.
        }

        public override List<ManeuverParameters> MakeNodesImpl(Orbit o, double UT, MechJebModuleTargetController target)
        {
            if (!target.NormalTargetExists)
                throw new OperationException(Localizer.Format("#MechJeb_approach_Exception1"));//must select a target for the course correction.

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
                throw new OperationException(Localizer.Format("#MechJeb_approach_Exception2"));//"target for course correction must be in the same sphere of influence"

            if (o.NextClosestApproachTime(target.TargetOrbit, UT) < UT + 1 ||
                    o.NextClosestApproachDistance(target.TargetOrbit, UT) > target.TargetOrbit.semiMajorAxis * 0.2)
            {
                errorMessage = Localizer.Format("#MechJeb_Approach_errormsg");//Warning: orbit before course correction doesn't seem to approach target very closely. Planned course correction may be extreme. Recommend plotting an approximate intercept orbit and then plotting a course correction.
            }

            CelestialBody targetBody = target.Target as CelestialBody;
            Vector3d dV = targetBody != null ?
                OrbitalManeuverCalculator.DeltaVAndTimeForCheapestCourseCorrection(o, UT, target.TargetOrbit, targetBody, targetBody.Radius + courseCorrectFinalPeA, out UT):
                OrbitalManeuverCalculator.DeltaVAndTimeForCheapestCourseCorrection(o, UT, target.TargetOrbit, interceptDistance, out UT);


            List<ManeuverParameters> NodeList = new List<ManeuverParameters>();
            NodeList.Add( new ManeuverParameters(dV, UT) );
            return NodeList;
        }
    }
}
