extern alias JetBrainsAnnotations;
using System.Collections.Generic;
using JetBrainsAnnotations::JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;
using static System.Math;

namespace MuMech
{
    [UsedImplicitly]
    public class OperationCourseCorrection : Operation
    {
        private static readonly string _name = Localizer.Format("#MechJeb_approach_title");
        public override         string GetName() => _name;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public EditableDoubleMult Periapsis = new EditableDoubleMult(200000, 1000);

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public EditableDouble Inclination = new EditableDouble(90);

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool InclinationFlag;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public EditableDoubleMult InterceptDistance = new EditableDoubleMult(200);

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            if (target.Target is CelestialBody)
            {
                GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_approach_label1"), Periapsis, "km"); //Approximate final periapsis
                GuiUtils.ToggledTextBox(ref InclinationFlag, "Inclination", Inclination, "°"); //Inclination
            }
            else
            {
                GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_approach_label2"), InterceptDistance, "m"); //Closest approach distance
            }
            GUILayout.Label(Localizer.Format("#MechJeb_approach_label3"));                       //Schedule the burn to minimize the required ΔV.
        }

        protected override List<ManeuverParameters> MakeNodesImpl(Orbit o, double ut, MechJebModuleTargetController target)
        {
            if (!target.NormalTargetExists)
                throw new OperationException(Localizer.Format("#MechJeb_approach_Exception1")); //must select a target for the course correction.

            // NOTE: it looks like this supports (e.g.) starting on a heliocentric course which intersects Jool, and then grabbing
            // that patch and dropping a course correction on it to fine tune an encounter with Laythe.  Need to at least consider that
            // use case and how close we need to be to Laythe.  It does not look like it supprorts dropping a maneuver on the helicentric
            // trajectory to fine tune the Jool trajectory to intersect Laythe.  It is possible that we should demand that a transfer
            // to Laythe is planned first and a maneuver node dropped in the Jool SOI to keep things simple here.

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
            // FIXME: add an explicit check that the current course intersects the SOI of the target body

            Vector3d dV;
            double   dt1 = 0;
            double   dt2 = 0;

            if (targetBody is null)
                dV = OrbitalManeuverCalculator.DeltaVAndTimeForCheapestCourseCorrection(o, ut, target.TargetOrbit, InterceptDistance, out ut);
            else
            {
                double inc = InclinationFlag ? Inclination.Val : double.NaN;
                (dV, dt1, dt2) = OrbitalManeuverCalculator.DeltaVAndTimeForCourseCorrectionToCelestial(o, ut, targetBody, targetBody.Radius + Periapsis, inc);
            }

            return new List<ManeuverParameters> { new ManeuverParameters(dV, ut+dt1) };
        }
    }
}
