using System;
using MechJebLib.Core;
using MechJebLib.Core.TwoBody;
using MechJebLib.Maneuvers;
using MechJebLib.Primitives;
using Smooth.Pools;
using UnityEngine;
using static MechJebLib.Statics;
using Debug = UnityEngine.Debug;

namespace MuMech
{
    public static class OrbitalManeuverCalculator
    {
        //Computes the deltaV of the burn needed to circularize an orbit at a given UT.
        public static Vector3d DeltaVToCircularize(Orbit o, double ut)
        {
            (V3 r, V3 v) = o.RightHandedStateVectorsAtUT(ut);

            V3 dv = Simple.DeltaVToCircularize(o.referenceBody.gravParameter, r, v);

            return dv.V3ToWorld();
        }

        //Computes the deltaV of the burn needed to set a given PeR and ApR at at a given UT.
        public static Vector3d DeltaVToEllipticize(Orbit o, double ut, double newPeR, double newApR)
        {
            double radius = o.Radius(ut);

            //sanitize inputs
            newPeR = MuUtils.Clamp(newPeR, 0 + 1, radius - 1);
            newApR = Math.Max(newApR, radius + 1);

            (V3 r, V3 v) = o.RightHandedStateVectorsAtUT(ut);

            V3 dv = Simple.DeltaVToEllipticize(o.referenceBody.gravParameter, r, v, newPeR, newApR);

            return dv.V3ToWorld();
        }

        //Computes the delta-V of the burn required to attain a given periapsis, starting from
        //a given orbit and burning at a given UT.
        public static Vector3d DeltaVToChangePeriapsis(Orbit o, double ut, double newPeR)
        {
            double radius = o.Radius(ut);

            //sanitize input
            newPeR = MuUtils.Clamp(newPeR, 0 + 1, radius - 1);

            (V3 r, V3 v) = o.RightHandedStateVectorsAtUT(ut);

            V3 dv = ChangeOrbitalElement.ChangePeriapsis(o.referenceBody.gravParameter, r, v, newPeR);

            return dv.V3ToWorld();
        }

        //Computes the delta-V of the burn at a given UT required to change an orbits apoapsis to a given value.
        //Note that you can pass in a negative apoapsis if the desired final orbit is hyperbolic
        public static Vector3d DeltaVToChangeApoapsis(Orbit o, double ut, double newApR)
        {
            double radius = o.Radius(ut);

            //sanitize input
            if (newApR > 0) newApR = Math.Max(newApR, radius + 1);

            (V3 r, V3 v) = o.RightHandedStateVectorsAtUT(ut);

            V3 dv = ChangeOrbitalElement.ChangeApoapsis(o.referenceBody.gravParameter, r, v, newApR);

            return dv.V3ToWorld();
        }

        public static Vector3d DeltaVToChangeEccentricity(Orbit o, double ut, double newEcc)
        {
            //sanitize input
            if (newEcc < 0) newEcc = 0;

            (V3 r, V3 v) = o.RightHandedStateVectorsAtUT(ut);

            V3 dv = ChangeOrbitalElement.ChangeECC(o.referenceBody.gravParameter, r, v, newEcc);

            return dv.V3ToWorld();
        }

        public static Vector3d DeltaVForSemiMajorAxis(Orbit o, double ut, double newSMA)
        {
            (V3 r, V3 v) = o.RightHandedStateVectorsAtUT(ut);

            V3 dv = ChangeOrbitalElement.ChangeSMA(o.referenceBody.gravParameter, r, v, newSMA);

            return dv.V3ToWorld();
        }

        //See #676
        //Computes the heading for a ground launch at the specified latitude accounting for the body rotation.
        //Both inputs are in degrees.
        //Convention: At equator, inclination    0 => heading 90 (east)
        //                        inclination   90 => heading 0  (north)
        //                        inclination  -90 => heading 180 (south)
        //                        inclination ±180 => heading 270 (west)
        //Returned heading is in degrees and in the range 0 to 360.
        //If the given latitude is too large, so that an orbit with a given inclination never attains the
        //given latitude, then this function returns either 90 (if -90 < inclination < 90) or 270.
        public static double HeadingForLaunchInclination(Orbit o, double inclinationDegrees)
        {
            (V3 r, V3 v) = o.RightHandedStateVectorsAtUT(Planetarium.GetUniversalTime());
            double rotFreq = TAU / o.referenceBody.rotationPeriod;

            return Rad2Deg(Simple.HeadingForLaunchInclination(o.referenceBody.gravParameter, r, v, Deg2Rad(inclinationDegrees), rotFreq));
        }

        //Computes the delta-V of the burn required to change an orbit's inclination to a given value
        //at a given UT. If the latitude at that time is too high, so that the desired inclination
        //cannot be attained, the burn returned will achieve as low an inclination as possible (namely, inclination = latitude).
        //The input inclination is in degrees.
        //Note that there are two orbits through each point with a given inclination. The convention used is:
        //   - first, clamp newInclination to the range -180, 180
        //   - if newInclination > 0, do the cheaper burn to set that inclination
        //   - if newInclination < 0, do the more expensive burn to set that inclination
        public static Vector3d DeltaVToChangeInclination(Orbit o, double ut, double newInclination)
        {
            (V3 r, V3 v) = o.RightHandedStateVectorsAtUT(ut);

            V3 dv = Simple.DeltaVToChangeInclination(r, v, Deg2Rad(newInclination));

            return dv.V3ToWorld();
        }

        //Computes the delta-V and time of a burn to match planes with the target orbit. The output burnUT
        //will be equal to the time of the first ascending node with respect to the target after the given UT.
        //Throws an ArgumentException if o is hyperbolic and doesn't have an ascending node relative to the target.
        public static Vector3d DeltaVAndTimeToMatchPlanesAscending(Orbit o, Orbit target, double UT, out double burnUT)
        {
            burnUT = o.TimeOfAscendingNode(target, UT);
            var desiredHorizontal = Vector3d.Cross(target.OrbitNormal(), o.Up(burnUT));
            var actualHorizontalVelocity = Vector3d.Exclude(o.Up(burnUT), o.WorldOrbitalVelocityAtUT(burnUT));
            Vector3d desiredHorizontalVelocity = actualHorizontalVelocity.magnitude * desiredHorizontal;
            return desiredHorizontalVelocity - actualHorizontalVelocity;
        }

        //Computes the delta-V and time of a burn to match planes with the target orbit. The output burnUT
        //will be equal to the time of the first descending node with respect to the target after the given UT.
        //Throws an ArgumentException if o is hyperbolic and doesn't have a descending node relative to the target.
        public static Vector3d DeltaVAndTimeToMatchPlanesDescending(Orbit o, Orbit target, double UT, out double burnUT)
        {
            burnUT = o.TimeOfDescendingNode(target, UT);
            var desiredHorizontal = Vector3d.Cross(target.OrbitNormal(), o.Up(burnUT));
            var actualHorizontalVelocity = Vector3d.Exclude(o.Up(burnUT), o.WorldOrbitalVelocityAtUT(burnUT));
            Vector3d desiredHorizontalVelocity = actualHorizontalVelocity.magnitude * desiredHorizontal;
            return desiredHorizontalVelocity - actualHorizontalVelocity;
        }

        //Computes the time and dV of a Hohmann transfer injection burn such that at apoapsis the transfer
        //orbit passes as close as possible to the target.
        //The output burnUT will be the first transfer window found after the given UT.
        //Assumes o and target are in approximately the same plane, and orbiting in the same direction.
        //Also assumes that o is a perfectly circular orbit (though result should be OK for small eccentricity).
        public static ( Vector3d dV, double burnUT) DeltaVAndTimeForHohmannTransfer(Orbit o, Orbit target, double ut, bool coplanar = true, bool rendezvous = true)
        {
            (V3 r1, V3 v1) = o.RightHandedStateVectorsAtUT(ut);
            (V3 r2, V3 v2) = target.RightHandedStateVectorsAtUT(ut);

            (V3 dv, double dt, _, _) =
                CoplanarTransfer.NextManeuver(o.referenceBody.gravParameter, r1, v1, r2, v2, coplanar: coplanar, rendezvous: rendezvous);

            return (dv.V3ToWorld(), dt);
        }

        // Computes the delta-V of a burn at a given time that will put an object with a given orbit on a
        // course to intercept a target at a specific interceptUT.
        //
        // offsetDistance: this is used by the Rendezvous Autopilot and is only going to be valid over very short distances
        // shortway: the shortway parameter to feed into the Lambert solver
        //
        public static (Vector3d v1, Vector3d v2) DeltaVToInterceptAtTime(Orbit o, double t0, Orbit target, double dt,
            double offsetDistance = 0, bool shortway = true)
        {
            (V3 ri, V3 vi) = o.RightHandedStateVectorsAtUT(t0);
            (V3 rf, V3 vf) = target.RightHandedStateVectorsAtUT(t0 + dt);

            (V3 transferVi, V3 transferVf) =
                Gooding.Solve(o.referenceBody.gravParameter, ri, vi, rf, shortway ? dt : -dt, 0);

            if (offsetDistance != 0)
            {
                rf -= offsetDistance * V3.Cross(vf, rf).normalized;
                (transferVi, transferVf) = Gooding.Solve(o.referenceBody.gravParameter, ri, vi, rf,
                    shortway ? dt : -dt, 0);
            }

            return ((transferVi - vi).V3ToWorld(), (vf - transferVf).V3ToWorld());
        }

        // Lambert Solver Driver function.
        //
        // This uses Shepperd's method instead of using KSP's Orbit class.
        //
        // The reference time is usually 'now' or the first time the burn can start.
        //
        // GM       - grav parameter of the celestial
        // pos      - position of the source orbit at a reference time
        // vel      - velocity of the source orbit at a reference time
        // tpos     - position of the target orbit at a reference time
        // tvel     - velocity of the target orbit at a reference time
        // DT       - time of the burn in seconds after the reference time
        // TT       - transfer time of the burn in seconds after the burn time
        // secondDV - the second burn dV
        // returns  - the first burn dV
        //
        private static Vector3d DeltaVToInterceptAtTime(double GM, Vector3d pos, Vector3d vel, Vector3d tpos, Vector3d tvel, double dt, double tt,
            out Vector3d secondDV, bool posigrade = true)
        {
            // advance the source orbit to ref + DT
            (V3 pos1, V3 vel1) = Shepperd.Solve(GM, dt, pos.ToV3(), vel.ToV3());

            // advance the target orbit to ref + DT + TT
            (V3 pos2, V3 vel2) = Shepperd.Solve(GM, dt + tt, tpos.ToV3(), tvel.ToV3());

            (V3 transferVi, V3 transferVf) = Gooding.Solve(GM, pos1, vel1, pos2, posigrade ? tt : -tt, 0);

            secondDV = (vel2 - transferVf).ToVector3d();

            return (transferVi - vel1).ToVector3d();
        }

        // This does a line-search to find the burnUT for the cheapest course correction that will intercept exactly
        public static Vector3d DeltaVAndTimeForCheapestCourseCorrection(Orbit o, double UT, Orbit target, out double burnUT)
        {
            double closestApproachTime = o.NextClosestApproachTime(target, UT + 2); //+2 so that closestApproachTime is definitely > UT

            burnUT           = UT;
            (Vector3d dV, _) = DeltaVToInterceptAtTime(o, burnUT, target, closestApproachTime - burnUT);

            // FIXME: replace with BrentRoot's 1-d minimization algorithm
            const int fineness = 20;
            for (double step = 0.5; step < fineness; step += 1.0)
            {
                double testUT = UT + (closestApproachTime - UT) * step / fineness;
                (Vector3d testDV, _) = DeltaVToInterceptAtTime(o, testUT, target, closestApproachTime - testUT);

                if (testDV.magnitude < dV.magnitude)
                {
                    dV     = testDV;
                    burnUT = testUT;
                }
            }

            return dV;
        }

        // This is the entry point for the course-correction to a target orbit which is a celestial
        public static Vector3d DeltaVAndTimeForCheapestCourseCorrection(Orbit o, double UT, Orbit target, CelestialBody targetBody, double finalPeR,
            out double burnUT)
        {
            Vector3d collisionDV = DeltaVAndTimeForCheapestCourseCorrection(o, UT, target, out burnUT);
            Orbit collisionOrbit = o.PerturbedOrbit(burnUT, collisionDV);
            double collisionUT = collisionOrbit.NextClosestApproachTime(target, burnUT);
            Vector3d collisionPosition = target.WorldPositionAtUT(collisionUT);
            Vector3d collisionRelVel = collisionOrbit.WorldOrbitalVelocityAtUT(collisionUT) - target.WorldOrbitalVelocityAtUT(collisionUT);

            double soiEnterUT = collisionUT - targetBody.sphereOfInfluence / collisionRelVel.magnitude;
            Vector3d soiEnterRelVel = collisionOrbit.WorldOrbitalVelocityAtUT(soiEnterUT) - target.WorldOrbitalVelocityAtUT(soiEnterUT);

            double E = 0.5 * soiEnterRelVel.sqrMagnitude -
                       targetBody.gravParameter / targetBody.sphereOfInfluence; //total orbital energy on SoI enter
            double finalPeSpeed =
                Math.Sqrt(2 * (E + targetBody.gravParameter / finalPeR)); //conservation of energy gives the orbital speed at finalPeR.
            double desiredImpactParameter =
                finalPeR * finalPeSpeed / soiEnterRelVel.magnitude; //conservation of angular momentum gives the required impact parameter

            Vector3d displacementDir = Vector3d.Cross(collisionRelVel, o.OrbitNormal()).normalized;
            Vector3d interceptTarget = collisionPosition + desiredImpactParameter * displacementDir;

            (V3 velAfterBurn, _) = Gooding.Solve(o.referenceBody.gravParameter, o.WorldBCIPositionAtUT(burnUT).ToV3(),
                o.WorldOrbitalVelocityAtUT(burnUT).ToV3(), (interceptTarget - o.referenceBody.position).ToV3(), collisionUT - burnUT, 0);

            Vector3d deltaV = velAfterBurn.ToVector3d() - o.WorldOrbitalVelocityAtUT(burnUT);
            return deltaV;
        }

        // This is the entry point for the course-correction to a target orbit which is not a celestial
        public static Vector3d DeltaVAndTimeForCheapestCourseCorrection(Orbit o, double UT, Orbit target, double caDistance, out double burnUT)
        {
            Vector3d collisionDV = DeltaVAndTimeForCheapestCourseCorrection(o, UT, target, out burnUT);
            Orbit collisionOrbit = o.PerturbedOrbit(burnUT, collisionDV);
            double collisionUT = collisionOrbit.NextClosestApproachTime(target, burnUT);
            Vector3d targetPos = target.WorldPositionAtUT(collisionUT);

            Vector3d interceptTarget = targetPos + target.NormalPlus(collisionUT) * caDistance;

            (V3 velAfterBurn, _) = Gooding.Solve(o.referenceBody.gravParameter, o.WorldBCIPositionAtUT(burnUT).ToV3(),
                o.WorldOrbitalVelocityAtUT(burnUT).ToV3(),
                (interceptTarget - o.referenceBody.position).ToV3(), collisionUT - burnUT, 0);

            Vector3d deltaV = velAfterBurn.ToVector3d() - o.WorldOrbitalVelocityAtUT(burnUT);
            return deltaV;
        }

        //Computes the time and delta-V of an ejection burn to a Hohmann transfer from one planet to another.
        //It's assumed that the initial orbit around the first planet is circular, and that this orbit
        //is in the same plane as the orbit of the first planet around the sun. It's also assumed that
        //the target planet has a fairly low relative inclination with respect to the first planet. If the
        //inclination change is nonzero you should also do a mid-course correction burn, as computed by
        //DeltaVForCourseCorrection (a function that has been removed due to being unused).
        public static Vector3d DeltaVAndTimeForInterplanetaryTransferEjection(Orbit o, double UT, Orbit target, bool syncPhaseAngle,
            out double burnUT)
        {
            Orbit planetOrbit = o.referenceBody.orbit;

            //Compute the time and dV for a Hohmann transfer where we pretend that we are the planet we are orbiting.
            //This gives us the "ideal" deltaV and UT of the ejection burn, if we didn't have to worry about waiting for the right
            //ejection angle and if we didn't have to worry about the planet's gravity dragging us back and increasing the required dV.
            double idealBurnUT;
            Vector3d idealDeltaV;

            if (syncPhaseAngle)
            {
                //time the ejection burn to intercept the target
                (idealDeltaV, idealBurnUT) = DeltaVAndTimeForHohmannTransfer(planetOrbit, target, UT);
            }
            else
            {
                //don't time the ejection burn to intercept the target; we just care about the final peri/apoapsis
                idealBurnUT = UT;
                if (target.semiMajorAxis < planetOrbit.semiMajorAxis)
                    idealDeltaV  = DeltaVToChangePeriapsis(planetOrbit, idealBurnUT, target.semiMajorAxis);
                else idealDeltaV = DeltaVToChangeApoapsis(planetOrbit, idealBurnUT, target.semiMajorAxis);
            }

            //Compute the actual transfer orbit this ideal burn would lead to.
            Orbit transferOrbit = planetOrbit.PerturbedOrbit(idealBurnUT, idealDeltaV);

            //Now figure out how to approximately eject from our current orbit into the Hohmann orbit we just computed.

            //Assume we want to exit the SOI with the same velocity as the ideal transfer orbit at idealUT -- i.e., immediately
            //after the "ideal" burn we used to compute the transfer orbit. This isn't quite right.
            //We intend to eject from our planet at idealUT and only several hours later will we exit the SOI. Meanwhile
            //the transfer orbit will have acquired a slightly different velocity, which we should correct for. Maybe
            //just add in (1/2)(sun gravity)*(time to exit soi)^2 ? But how to compute time to exit soi? Or maybe once we
            //have the ejection orbit we should just move the ejection burn back by the time to exit the soi?
            Vector3d soiExitVelocity = idealDeltaV;
            //project the desired exit direction into the current orbit plane to get the feasible exit direction
            Vector3d inPlaneSoiExitDirection = Vector3d.Exclude(o.OrbitNormal(), soiExitVelocity).normalized;

            //compute the angle by which the trajectory turns between periapsis (where we do the ejection burn)
            //and SOI exit (approximated as radius = infinity)
            double soiExitEnergy = 0.5 * soiExitVelocity.sqrMagnitude - o.referenceBody.gravParameter / o.referenceBody.sphereOfInfluence;
            double ejectionRadius = o.semiMajorAxis; //a guess, good for nearly circular orbits

            double ejectionKineticEnergy = soiExitEnergy + o.referenceBody.gravParameter / ejectionRadius;
            double ejectionSpeed = Math.Sqrt(2 * ejectionKineticEnergy);

            //construct a sample ejection orbit
            Vector3d ejectionOrbitInitialVelocity = ejectionSpeed * (Vector3d)o.referenceBody.transform.right;
            Vector3d ejectionOrbitInitialPosition = o.referenceBody.position + ejectionRadius * (Vector3d)o.referenceBody.transform.up;
            Orbit sampleEjectionOrbit = MuUtils.OrbitFromStateVectors(ejectionOrbitInitialPosition, ejectionOrbitInitialVelocity, o.referenceBody, 0);
            double ejectionOrbitDuration = sampleEjectionOrbit.NextTimeOfRadius(0, o.referenceBody.sphereOfInfluence);
            Vector3d ejectionOrbitFinalVelocity = sampleEjectionOrbit.WorldOrbitalVelocityAtUT(ejectionOrbitDuration);

            double turningAngle = Math.Abs(Vector3d.Angle(ejectionOrbitInitialVelocity, ejectionOrbitFinalVelocity));

            //rotate the exit direction by 90 + the turning angle to get a vector pointing to the spot in our orbit
            //where we should do the ejection burn. Then convert this to a true anomaly and compute the time closest
            //to planetUT at which we will pass through that true anomaly.
            Vector3d ejectionPointDirection = Quaternion.AngleAxis(-(float)(90 + turningAngle), o.OrbitNormal()) * inPlaneSoiExitDirection;
            double ejectionTrueAnomaly = o.TrueAnomalyFromVector(ejectionPointDirection);
            burnUT = o.TimeOfTrueAnomaly(ejectionTrueAnomaly, idealBurnUT - o.period);

            if (idealBurnUT - burnUT > o.period / 2 || burnUT < UT)
            {
                burnUT += o.period;
            }

            //rotate the exit direction by the turning angle to get a vector pointing to the spot in our orbit
            //where we should do the ejection burn
            Vector3d ejectionBurnDirection = Quaternion.AngleAxis(-(float)turningAngle, o.OrbitNormal()) * inPlaneSoiExitDirection;
            Vector3d ejectionVelocity = ejectionSpeed * ejectionBurnDirection;

            Vector3d preEjectionVelocity = o.WorldOrbitalVelocityAtUT(burnUT);

            return ejectionVelocity - preEjectionVelocity;
        }

        public static (Vector3d dv, double dt) DeltaVAndTimeForMoonReturnEjection(Orbit o, double ut, double targetPrimaryRadius)
        {
            CelestialBody moon = o.referenceBody;
            CelestialBody primary = moon.referenceBody;
            (V3 moonR0, V3 moonV0) = moon.orbit.RightHandedStateVectorsAtUT(ut);
            double moonSOI = moon.sphereOfInfluence;
            (V3 r0, V3 v0) = o.RightHandedStateVectorsAtUT(ut);

            double dtmin = o.eccentricity >= 1 ? 0 : double.NegativeInfinity;

            (V3 dv, double dt, double newPeR) = ReturnFromMoon.NextManeuver(primary.gravParameter, moon.gravParameter, moonR0,
                moonV0, moonSOI, r0, v0, targetPrimaryRadius, 0, dtmin);

            Debug.Log($"Solved PeR from calcluator: {newPeR}");

            return (dv.V3ToWorld(), ut + dt);
        }

        //Computes the delta-V of the burn at a given time required to zero out the difference in orbital velocities
        //between a given orbit and a target.
        public static Vector3d DeltaVToMatchVelocities(Orbit o, double UT, Orbit target) =>
            target.WorldOrbitalVelocityAtUT(UT) - o.WorldOrbitalVelocityAtUT(UT);

        // Compute the delta-V of the burn at the givent time required to enter an orbit with a period of (resonanceDivider-1)/resonanceDivider of the starting orbit period
        public static Vector3d DeltaVToResonantOrbit(Orbit o, double UT, double f)
        {
            double a = o.ApR;
            double p = o.PeR;

            // Thanks wolframAlpha for the Math
            // x = (a^3 f^2 + 3 a^2 f^2 p + 3 a f^2 p^2 + f^2 p^3)^(1/3)-a
            double x = Math.Pow(
                Math.Pow(a, 3) * Math.Pow(f, 2) + 3 * Math.Pow(a, 2) * Math.Pow(f, 2) * p + 3 * a * Math.Pow(f, 2) * Math.Pow(p, 2) +
                Math.Pow(f, 2) * Math.Pow(p, 3), 1d / 3) - a;

            if (x < 0)
                return Vector3d.zero;

            if (f > 1)
                return DeltaVToChangeApoapsis(o, UT, x);
            return DeltaVToChangePeriapsis(o, UT, x);
        }

        // Compute the angular distance between two points on a unit sphere
        public static double Distance(double lat_a, double long_a, double lat_b, double long_b)
        {
            // Using Great-Circle Distance 2nd computational formula from http://en.wikipedia.org/wiki/Great-circle_distance
            // Note the switch from degrees to radians and back
            double lat_a_rad = UtilMath.Deg2Rad * lat_a;
            double lat_b_rad = UtilMath.Deg2Rad * lat_b;
            double long_diff_rad = UtilMath.Deg2Rad * (long_b - long_a);

            return UtilMath.Rad2Deg * Math.Atan2(Math.Sqrt(Math.Pow(Math.Cos(lat_b_rad) * Math.Sin(long_diff_rad), 2) +
                                                           Math.Pow(
                                                               Math.Cos(lat_a_rad) * Math.Sin(lat_b_rad) - Math.Sin(lat_a_rad) * Math.Cos(lat_b_rad) *
                                                               Math.Cos(long_diff_rad), 2)),
                Math.Sin(lat_a_rad) * Math.Sin(lat_b_rad) + Math.Cos(lat_a_rad) * Math.Cos(lat_b_rad) * Math.Cos(long_diff_rad));
        }

        // Compute an angular heading from point a to point b on a unit sphere
        public static double Heading(double lat_a, double long_a, double lat_b, double long_b)
        {
            // Using Great-Circle Navigation formula for initial heading from http://en.wikipedia.org/wiki/Great-circle_navigation
            // Note the switch from degrees to radians and back
            // Original equation returns 0 for due south, increasing clockwise. We add 180 and clamp to 0-360 degrees to map to compass-type headings
            double lat_a_rad = UtilMath.Deg2Rad * lat_a;
            double lat_b_rad = UtilMath.Deg2Rad * lat_b;
            double long_diff_rad = UtilMath.Deg2Rad * (long_b - long_a);

            return MuUtils.ClampDegrees360(180.0 / Math.PI * Math.Atan2(
                Math.Sin(long_diff_rad),
                Math.Cos(lat_a_rad) * Math.Tan(lat_b_rad) - Math.Sin(lat_a_rad) * Math.Cos(long_diff_rad)));
        }

        //Computes the deltaV of the burn needed to set a given LAN at a given UT.
        public static Vector3d DeltaVToShiftLAN(Orbit o, double UT, double newLAN)
        {
            Vector3d pos = o.WorldPositionAtUT(UT);
            // Burn position in the same reference frame as LAN
            double burn_latitude = o.referenceBody.GetLatitude(pos);
            double burn_longitude = o.referenceBody.GetLongitude(pos) + o.referenceBody.rotationAngle;

            const double target_latitude = 0; // Equator
            double target_longitude = 0;      // Prime Meridian

            // Select the location of either the descending or ascending node.
            // If the descending node is closer than the ascending node, or there is no ascending node, target the reverse of the newLAN
            // Otherwise target the newLAN
            if (o.AscendingNodeEquatorialExists() && o.DescendingNodeEquatorialExists())
            {
                if (o.TimeOfDescendingNodeEquatorial(UT) < o.TimeOfAscendingNodeEquatorial(UT))
                {
                    // DN is closer than AN
                    // Burning for the AN would entail flipping the orbit around, and would be very expensive
                    // therefore, burn for the corresponding Longitude of the Descending Node
                    target_longitude = MuUtils.ClampDegrees360(newLAN + 180.0);
                }
                else
                {
                    // DN is closer than AN
                    target_longitude = MuUtils.ClampDegrees360(newLAN);
                }
            }
            else if (o.AscendingNodeEquatorialExists() && !o.DescendingNodeEquatorialExists())
            {
                // No DN
                target_longitude = MuUtils.ClampDegrees360(newLAN);
            }
            else if (!o.AscendingNodeEquatorialExists() && o.DescendingNodeEquatorialExists())
            {
                // No AN
                target_longitude = MuUtils.ClampDegrees360(newLAN + 180.0);
            }
            else
            {
                throw new ArgumentException("OrbitalManeuverCalculator.DeltaVToShiftLAN: No Equatorial Nodes");
            }

            double desiredHeading = MuUtils.ClampDegrees360(Heading(burn_latitude, burn_longitude, target_latitude, target_longitude));
            var actualHorizontalVelocity = Vector3d.Exclude(o.Up(UT), o.WorldOrbitalVelocityAtUT(UT));
            Vector3d eastComponent = actualHorizontalVelocity.magnitude * Math.Sin(UtilMath.Deg2Rad * desiredHeading) * o.East(UT);
            Vector3d northComponent = actualHorizontalVelocity.magnitude * Math.Cos(UtilMath.Deg2Rad * desiredHeading) * o.North(UT);
            Vector3d desiredHorizontalVelocity = eastComponent + northComponent;
            return desiredHorizontalVelocity - actualHorizontalVelocity;
        }

        public static Vector3d DeltaVToShiftNodeLongitude(Orbit o, double UT, double newNodeLong)
        {
            // Get the location underneath the burn location at the current moment.
            // Note that this does NOT account for the rotation of the body that will happen between now
            // and when the vessel reaches the apoapsis.
            Vector3d pos = o.WorldPositionAtUT(UT);
            double burnRadius = o.Radius(UT);
            double oppositeRadius = 0;

            // Back out the rotation of the body to calculate the longitude of the apoapsis when the vessel reaches the node
            double degreeRotationToNode = (UT - Planetarium.GetUniversalTime()) * 360 / o.referenceBody.rotationPeriod;
            double NodeLongitude = o.referenceBody.GetLongitude(pos) - degreeRotationToNode;

            double LongitudeOffset = NodeLongitude - newNodeLong; // Amount we need to shift the Ap's longitude

            // Calculate a semi-major axis that gives us an orbital period that will rotate the body to place
            // the burn location directly over the newNodeLong longitude, over the course of one full orbit.
            // N tracks the number of full body rotations desired in a vessal orbit.
            // If N=0, we calculate the SMA required to let the body rotate less than a full local day.
            // If the resulting SMA would drop us under the 5x time warp limit, we deem it to be too low, and try again with N+1.
            // In other words, we allow the body to rotate more than 1 day, but less then 2 days.
            // As long as the resulting SMA is below the 5x limit, we keep increasing N until we find a viable solution.
            // This may place the apside out the sphere of influence, however.
            // TODO: find the cheapest SMA, instead of the smallest
            int N = -1;
            double target_sma = 0;

            while (oppositeRadius - o.referenceBody.Radius < o.referenceBody.timeWarpAltitudeLimits[4] && N < 20)
            {
                N++;
                double target_period = o.referenceBody.rotationPeriod * (LongitudeOffset / 360 + N);
                target_sma = Math.Pow(o.referenceBody.gravParameter * target_period * target_period / (4 * Math.PI * Math.PI), 1.0 / 3.0); // cube roo
                oppositeRadius = 2 * target_sma - burnRadius;
            }

            return DeltaVForSemiMajorAxis(o, UT, target_sma);
        }

        //
        // Global OrbitPool for re-using Orbit objects
        //

        public static readonly Pool<Orbit> OrbitPool = new Pool<Orbit>(createOrbit, resetOrbit);
        private static         Orbit       createOrbit()       => new Orbit();
        private static         void        resetOrbit(Orbit o) { }

        private static readonly PatchedConics.SolverParameters solverParameters = new PatchedConics.SolverParameters();

        // Runs the PatchedConicSolver to do initial value "shooting" given an initial orbit, a maneuver dV and UT to execute, to a target Celestial's SOI
        //
        // initial   : initial parkig orbit
        // target    : the Body whose SOI we are shooting towards
        // dV        : the dV of the manuever off of the parking orbit
        // burnUT    : the time of the maneuver off of the parking orbit
        // arrivalUT : this is really more of an upper clamp on the simulation so that if we miss and never hit the body SOI it stops
        // intercept : this is the final computed intercept orbit, it should be in the SOI of the target body, but if it never hits it then the
        //             e.g. heliocentric orbit is returned instead, so the caller needs to check.
        //
        // FIXME: NREs when there's no next patch
        // FIXME: duplicates code with OrbitExtensions.CalculateNextOrbit()
        //
        public static void PatchedConicInterceptBody(Orbit initial, CelestialBody target, Vector3d dV, double burnUT, double arrivalUT,
            out Orbit intercept)
        {
            Orbit orbit = OrbitPool.Borrow();
            orbit.UpdateFromStateVectors(initial.getRelativePositionAtUT(burnUT), initial.getOrbitalVelocityAtUT(burnUT) + dV.xzy,
                initial.referenceBody, burnUT);
            orbit.StartUT = burnUT;
            orbit.EndUT   = orbit.eccentricity >= 1.0 ? orbit.period : burnUT + orbit.period;
            Orbit next_orbit = OrbitPool.Borrow();

            bool ok = PatchedConics.CalculatePatch(orbit, next_orbit, burnUT, solverParameters, null);
            while (ok && orbit.referenceBody != target && orbit.EndUT < arrivalUT)
            {
                OrbitPool.Release(orbit);
                orbit      = next_orbit;
                next_orbit = OrbitPool.Borrow();

                ok = PatchedConics.CalculatePatch(orbit, next_orbit, orbit.StartUT, solverParameters, null);
            }

            intercept = orbit;
            intercept.UpdateFromOrbitAtUT(orbit, arrivalUT, orbit.referenceBody);
            OrbitPool.Release(orbit);
            OrbitPool.Release(next_orbit);
        }

        // Takes an e.g. heliocentric orbit and a target planet celestial and finds the time of the SOI intercept.
        //
        //
        //
        public static void SOI_intercept(Orbit transfer, CelestialBody target, double UT1, double UT2, out double UT)
        {
            if (transfer.referenceBody != target.orbit.referenceBody)
                throw new ArgumentException("[MechJeb] SOI_intercept: transfer orbit must be in the same SOI as the target celestial");
            Func<double, object, double> f = delegate(double UT, object ign)
            {
                return (transfer.getRelativePositionAtUT(UT) - target.orbit.getRelativePositionAtUT(UT)).magnitude - target.sphereOfInfluence;
            };
            UT = 0;
            try { UT = BrentRoot.Solve(f, UT1, UT2, null); }
            catch (TimeoutException) { Debug.Log("[MechJeb] Brents method threw a timeout error (supressed)"); }
        }
    }
}
