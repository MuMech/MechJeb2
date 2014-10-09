using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public static class OrbitalManeuverCalculator
    {
        //Computes the speed of a circular orbit of a given radius for a given body.
        public static double CircularOrbitSpeed(CelestialBody body, double radius)
        {
            //v = sqrt(GM/r)
            return Math.Sqrt(body.gravParameter / radius);
        }

        //Computes the deltaV of the burn needed to circularize an orbit at a given UT.
        public static Vector3d DeltaVToCircularize(Orbit o, double UT)
        {
            Vector3d desiredVelocity = CircularOrbitSpeed(o.referenceBody, o.Radius(UT)) * o.Horizontal(UT);
            Vector3d actualVelocity = o.SwappedOrbitalVelocityAtUT(UT);
            return desiredVelocity - actualVelocity;
        }

        //Computes the deltaV of the burn needed to set a given PeR and ApR at at a given UT.
        public static Vector3d DeltaVToEllipticize(Orbit o, double UT, double newPeR, double newApR)
        {
            double radius = o.Radius(UT);

            //sanitize inputs
            newPeR = MuUtils.Clamp(newPeR, 0 + 1, radius - 1);
            newApR = Math.Max(newApR, radius + 1);

            double GM = o.referenceBody.gravParameter;
            double E = -GM / (newPeR + newApR); //total energy per unit mass of new orbit
            double L = Math.Sqrt(Math.Abs((Math.Pow(E * (newApR - newPeR), 2) - GM * GM) / (2 * E))); //angular momentum per unit mass of new orbit
            double kineticE = E + GM / radius; //kinetic energy (per unit mass) of new orbit at UT
            double horizontalV = L / radius;   //horizontal velocity of new orbit at UT
            double verticalV = Math.Sqrt(Math.Abs(2 * kineticE - horizontalV * horizontalV)); //vertical velocity of new orbit at UT

            Vector3d actualVelocity = o.SwappedOrbitalVelocityAtUT(UT);

            //untested:
            verticalV *= Math.Sign(Vector3d.Dot(o.Up(UT), actualVelocity));

            Vector3d desiredVelocity = horizontalV * o.Horizontal(UT) + verticalV * o.Up(UT);
            return desiredVelocity - actualVelocity;
        }

        //Computes the delta-V of the burn required to attain a given periapsis, starting from
        //a given orbit and burning at a given UT. Throws an ArgumentException if given an impossible periapsis.
        //The computed burn is always horizontal, though this may not be strictly optimal.
        public static Vector3d DeltaVToChangePeriapsis(Orbit o, double UT, double newPeR)
        {
            double radius = o.Radius(UT);

            //sanitize input
            newPeR = MuUtils.Clamp(newPeR, 0 + 1, radius - 1);

            //are we raising or lowering the periapsis?
            bool raising = (newPeR > o.PeR);
            Vector3d burnDirection = (raising ? 1 : -1) * o.Horizontal(UT);

            double minDeltaV = 0;
            double maxDeltaV;
            if (raising)
            {
                //put an upper bound on the required deltaV:
                maxDeltaV = 0.25;
                while (o.PerturbedOrbit(UT, maxDeltaV * burnDirection).PeR < newPeR)
                {
                    maxDeltaV *= 2;
                    if (maxDeltaV > 100000) break; //a safety precaution
                }
            }
            else
            {
                //when lowering periapsis, we burn horizontally, and max possible deltaV is the deltaV required to kill all horizontal velocity
                maxDeltaV = Math.Abs(Vector3d.Dot(o.SwappedOrbitalVelocityAtUT(UT), burnDirection));
            }

            //now do a binary search to find the needed delta-v
            while (maxDeltaV - minDeltaV > 0.01)
            {
                double testDeltaV = (maxDeltaV + minDeltaV) / 2.0;
                double testPeriapsis = o.PerturbedOrbit(UT, testDeltaV * burnDirection).PeR;

                if ((testPeriapsis > newPeR && raising) || (testPeriapsis < newPeR && !raising))
                {
                    maxDeltaV = testDeltaV;
                }
                else
                {
                    minDeltaV = testDeltaV;
                }
            }

            return ((maxDeltaV + minDeltaV) / 2) * burnDirection;
        }

        public static bool ApoapsisIsHigher(double ApR, double than)
        {
            if (than > 0 && ApR < 0) return true;
            if (than < 0 && ApR > 0) return false;
            return ApR > than;
        }

        //Computes the delta-V of the burn at a given UT required to change an orbits apoapsis to a given value.
        //The computed burn is always prograde or retrograde, though this may not be strictly optimal.
        //Note that you can pass in a negative apoapsis if the desired final orbit is hyperbolic
        public static Vector3d DeltaVToChangeApoapsis(Orbit o, double UT, double newApR)
        {
            double radius = o.Radius(UT);

            //sanitize input
            if (newApR > 0) newApR = Math.Max(newApR, radius + 1);

            //are we raising or lowering the periapsis?
            bool raising = ApoapsisIsHigher(newApR, o.ApR);

            Vector3d burnDirection = (raising ? 1 : -1) * o.Prograde(UT);

            double minDeltaV = 0;
            double maxDeltaV;
            if (raising)
            {
                //put an upper bound on the required deltaV:
                maxDeltaV = 0.25;

                double ap = o.ApR;
                while (ApoapsisIsHigher(newApR, ap))
                {
                    maxDeltaV *= 2;
                    ap = o.PerturbedOrbit(UT, maxDeltaV * burnDirection).ApR;
                    if (maxDeltaV > 100000) break; //a safety precaution
                }
            }
            else
            {
                //when lowering apoapsis, we burn retrograde, and max possible deltaV is total velocity
                maxDeltaV = o.SwappedOrbitalVelocityAtUT(UT).magnitude;
            }

            //now do a binary search to find the needed delta-v
            while (maxDeltaV - minDeltaV > 0.01)
            {
                double testDeltaV = (maxDeltaV + minDeltaV) / 2.0;
                double testApoapsis = o.PerturbedOrbit(UT, testDeltaV * burnDirection).ApR;

                bool above = ApoapsisIsHigher(testApoapsis, newApR);

                if ((raising && above) || (!raising && !above))
                {
                    maxDeltaV = testDeltaV;
                }
                else
                {
                    minDeltaV = testDeltaV;
                }
            }

            return ((maxDeltaV + minDeltaV) / 2) * burnDirection;
        }

        //Computes the heading of the ground track of an orbit with a given inclination at a given latitude.
        //Both inputs are in degrees.
        //Convention: At equator, inclination    0 => heading 90 (east) 
        //                        inclination   90 => heading 0  (north)
        //                        inclination  -90 => heading 180 (south)
        //                        inclination ±180 => heading 270 (west)
        //Returned heading is in degrees and in the range 0 to 360.
        //If the given latitude is too large, so that an orbit with a given inclination never attains the 
        //given latitude, then this function returns either 90 (if -90 < inclination < 90) or 270.
        public static double HeadingForInclination(double inclinationDegrees, double latitudeDegrees)
        {
            double cosDesiredSurfaceAngle = Math.Cos(inclinationDegrees * Math.PI / 180) / Math.Cos(latitudeDegrees * Math.PI / 180);
            if (Math.Abs(cosDesiredSurfaceAngle) > 1.0)
            {
                //If inclination < latitude, we get this case: the desired inclination is impossible
                if (Math.Abs(MuUtils.ClampDegrees180(inclinationDegrees)) < 90) return 90;
                else return 270;
            }
            else
            {
                double angleFromEast = (180 / Math.PI) * Math.Acos(cosDesiredSurfaceAngle); //an angle between 0 and 180
                if (inclinationDegrees < 0) angleFromEast *= -1;
                //now angleFromEast is between -180 and 180

                return MuUtils.ClampDegrees360(90 - angleFromEast);
            }
        }

        //Computes the delta-V of the burn required to change an orbit's inclination to a given value
        //at a given UT. If the latitude at that time is too high, so that the desired inclination
        //cannot be attained, the burn returned will achieve as low an inclination as possible (namely, inclination = latitude).
        //The input inclination is in degrees.
        //Note that there are two orbits through each point with a given inclination. The convention used is:
        //   - first, clamp newInclination to the range -180, 180
        //   - if newInclination > 0, do the cheaper burn to set that inclination
        //   - if newInclination < 0, do the more expensive burn to set that inclination
        public static Vector3d DeltaVToChangeInclination(Orbit o, double UT, double newInclination)
        {
            double latitude = o.referenceBody.GetLatitude(o.SwappedAbsolutePositionAtUT(UT));
            double desiredHeading = HeadingForInclination(newInclination, latitude);
            Vector3d actualHorizontalVelocity = Vector3d.Exclude(o.Up(UT), o.SwappedOrbitalVelocityAtUT(UT));
            Vector3d eastComponent = actualHorizontalVelocity.magnitude * Math.Sin(Math.PI / 180 * desiredHeading) * o.East(UT);
            Vector3d northComponent = actualHorizontalVelocity.magnitude * Math.Cos(Math.PI / 180 * desiredHeading) * o.North(UT);
            if (Vector3d.Dot(actualHorizontalVelocity, northComponent) < 0) northComponent *= -1;
            if (MuUtils.ClampDegrees180(newInclination) < 0) northComponent *= -1;
            Vector3d desiredHorizontalVelocity = eastComponent + northComponent;
            return desiredHorizontalVelocity - actualHorizontalVelocity;
        }

        //Computes the delta-V and time of a burn to match planes with the target orbit. The output burnUT
        //will be equal to the time of the first ascending node with respect to the target after the given UT.
        //Throws an ArgumentException if o is hyperbolic and doesn't have an ascending node relative to the target.
        public static Vector3d DeltaVAndTimeToMatchPlanesAscending(Orbit o, Orbit target, double UT, out double burnUT)
        {
            burnUT = o.TimeOfAscendingNode(target, UT);
            Vector3d desiredHorizontal = Vector3d.Cross(target.SwappedOrbitNormal(), o.Up(burnUT));
            Vector3d actualHorizontalVelocity = Vector3d.Exclude(o.Up(burnUT), o.SwappedOrbitalVelocityAtUT(burnUT));
            Vector3d desiredHorizontalVelocity = actualHorizontalVelocity.magnitude * desiredHorizontal;
            return desiredHorizontalVelocity - actualHorizontalVelocity;
        }

        //Computes the delta-V and time of a burn to match planes with the target orbit. The output burnUT
        //will be equal to the time of the first descending node with respect to the target after the given UT.
        //Throws an ArgumentException if o is hyperbolic and doesn't have a descending node relative to the target.
        public static Vector3d DeltaVAndTimeToMatchPlanesDescending(Orbit o, Orbit target, double UT, out double burnUT)
        {
            burnUT = o.TimeOfDescendingNode(target, UT);
            Vector3d desiredHorizontal = Vector3d.Cross(target.SwappedOrbitNormal(), o.Up(burnUT));
            Vector3d actualHorizontalVelocity = Vector3d.Exclude(o.Up(burnUT), o.SwappedOrbitalVelocityAtUT(burnUT));
            Vector3d desiredHorizontalVelocity = actualHorizontalVelocity.magnitude * desiredHorizontal;
            return desiredHorizontalVelocity - actualHorizontalVelocity;
        }

        //Computes the dV of a Hohmann transfer burn at time UT that will put the apoapsis or periapsis
        //of the transfer orbit on top of the target orbit.
        //The output value apsisPhaseAngle is the phase angle between the transferring vessel and the
        //target object as the transferring vessel crosses the target orbit at the apoapsis or periapsis
        //of the transfer orbit.
        //Actually, it's not exactly the phase angle. It's a sort of mean anomaly phase angle. The
        //difference is not important for how this function is used by DeltaVAndTimeForHohmannTransfer.
        private static Vector3d DeltaVAndApsisPhaseAngleOfHohmannTransfer(Orbit o, Orbit target, double UT, out double apsisPhaseAngle)
        {
            Vector3d apsisDirection = -o.SwappedRelativePositionAtUT(UT);
            double desiredApsis = target.RadiusAtTrueAnomaly(target.TrueAnomalyFromVector(apsisDirection));

            Vector3d dV;
            if (desiredApsis > o.ApR)
            {
                dV = DeltaVToChangeApoapsis(o, UT, desiredApsis);
                Orbit transferOrbit = o.PerturbedOrbit(UT, dV);
                double transferApTime = transferOrbit.NextApoapsisTime(UT);
                Vector3d transferApDirection = transferOrbit.SwappedRelativePositionAtApoapsis();  // getRelativePositionAtUT was returning NaNs! :(((((
                double targetTrueAnomaly = target.TrueAnomalyFromVector(transferApDirection);
                double meanAnomalyOffset = 360 * (target.TimeOfTrueAnomaly(targetTrueAnomaly, UT) - transferApTime) / target.period;
                apsisPhaseAngle = meanAnomalyOffset;
            }
            else
            {
                dV = DeltaVToChangePeriapsis(o, UT, desiredApsis);
                Orbit transferOrbit = o.PerturbedOrbit(UT, dV);
                double transferPeTime = transferOrbit.NextPeriapsisTime(UT);
                Vector3d transferPeDirection = transferOrbit.SwappedRelativePositionAtPeriapsis();  // getRelativePositionAtUT was returning NaNs! :(((((
                double targetTrueAnomaly = target.TrueAnomalyFromVector(transferPeDirection);
                double meanAnomalyOffset = 360 * (target.TimeOfTrueAnomaly(targetTrueAnomaly, UT) - transferPeTime) / target.period;
                apsisPhaseAngle = meanAnomalyOffset;
            }

            apsisPhaseAngle = MuUtils.ClampDegrees180(apsisPhaseAngle);

            return dV;
        }

        //Computes the time and dV of a Hohmann transfer injection burn such that at apoapsis the transfer
        //orbit passes as close as possible to the target.
        //The output burnUT will be the first transfer window found after the given UT.
        //Assumes o and target are in approximately the same plane, and orbiting in the same direction.
        //Also assumes that o is a perfectly circular orbit (though result should be OK for small eccentricity).
        public static Vector3d DeltaVAndTimeForHohmannTransfer(Orbit o, Orbit target, double UT, out double burnUT)
        {
            //We do a binary search for the burn time that zeros out the phase angle between the
            //transferring vessel and the target at the apsis of the transfer orbit.
            double synodicPeriod = o.SynodicPeriod(target);

            double lastApsisPhaseAngle;
            Vector3d immediateBurnDV = DeltaVAndApsisPhaseAngleOfHohmannTransfer(o, target, UT, out lastApsisPhaseAngle);

            double minTime = UT;
            double maxTime = UT + 1.5 * synodicPeriod;

            //first find roughly where the zero point is
            const int numDivisions = 30;
            double dt = (maxTime - minTime) / numDivisions;
            for (int i = 1; i <= numDivisions; i++)
            {
                double t = minTime + dt * i;

                double apsisPhaseAngle;
                Vector3d dV = DeltaVAndApsisPhaseAngleOfHohmannTransfer(o, target, t, out apsisPhaseAngle);

                if ((Math.Abs(apsisPhaseAngle) < 90) && (Math.Sign(lastApsisPhaseAngle) != Math.Sign(apsisPhaseAngle)))
                {
                    minTime = t - dt;
                    maxTime = t;
                    break;
                }

                if ((i == 1) && (Math.Abs(lastApsisPhaseAngle) < 0.5) && (Math.Sign(lastApsisPhaseAngle) == Math.Sign(apsisPhaseAngle)))
                {
                    //In this case we are JUST passed the center of the transfer window, but probably we
                    //can still do the transfer just fine. Don't do a search, just return an immediate burn
                    burnUT = UT;
                    return immediateBurnDV;
                }

                lastApsisPhaseAngle = apsisPhaseAngle;

                if (i == numDivisions)
                {
                    throw new ArgumentException("OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer: couldn't find the transfer window!!");
                }
            }

            int minTimeApsisPhaseAngleSign = Math.Sign(lastApsisPhaseAngle);

            //then do a binary search
            while (maxTime - minTime > 0.01)
            {
                double testTime = (maxTime + minTime) / 2;

                double testApsisPhaseAngle;
                Vector3d dV = DeltaVAndApsisPhaseAngleOfHohmannTransfer(o, target, testTime, out testApsisPhaseAngle);

                if (Math.Sign(testApsisPhaseAngle) == minTimeApsisPhaseAngleSign)
                {
                    minTime = testTime;
                }
                else
                {
                    maxTime = testTime;
                }
            }

            burnUT = (minTime + maxTime) / 2;
            double finalApsisPhaseAngle;
            Vector3d burnDV = DeltaVAndApsisPhaseAngleOfHohmannTransfer(o, target, burnUT, out finalApsisPhaseAngle);

            return burnDV;
        }

        //Computes the delta-V of a burn at a given time that will put an object with a given orbit on a
        //course to intercept a target at a specific interceptUT.
        public static Vector3d DeltaVToInterceptAtTime(Orbit o, double UT, Orbit target, double interceptUT, double offsetDistance = 0, bool shortway = true)
        {
            double initialT = UT;
            Vector3d initialRelPos = o.SwappedRelativePositionAtUT(initialT);
            double finalT = interceptUT;
            Vector3d finalRelPos = target.SwappedRelativePositionAtUT(finalT);

            Vector3d initialVelocity, finalVelocity;
            LambertSolver.Solve(initialRelPos, finalRelPos, finalT - initialT, o.referenceBody, shortway, out initialVelocity, out finalVelocity);

            if (offsetDistance != 0)
            {
                finalRelPos += offsetDistance * Vector3d.Cross(finalVelocity, finalRelPos).normalized;
                LambertSolver.Solve(initialRelPos, finalRelPos, finalT - initialT, o.referenceBody, shortway, out initialVelocity, out finalVelocity);
            }

            Vector3d currentInitialVelocity = o.SwappedOrbitalVelocityAtUT(initialT);
            return initialVelocity - currentInitialVelocity;
        }


        //First finds the time of closest approach to the target during the next orbit after the
        //time UT. Then returns the delta-V of a burn at UT that will change the separation at
        //that closest approach time to zero.
        //This will likely only return sensible results when the given orbit is already an 
        //approximate intercept trajectory.
        public static Vector3d DeltaVForCourseCorrection(Orbit o, double UT, Orbit target)
        {
            double closestApproachTime = o.NextClosestApproachTime(target, UT + 1); //+1 so that closestApproachTime is definitely > UT
            Vector3d dV = DeltaVToInterceptAtTime(o, UT, target, closestApproachTime);
            return dV;
        }

        public static Vector3d DeltaVAndTimeForCheapestCourseCorrection(Orbit o, double UT, Orbit target, out double burnUT)
        {
            double closestApproachTime = o.NextClosestApproachTime(target, UT + 2); //+2 so that closestApproachTime is definitely > UT

            burnUT = UT;
            Vector3d dV = DeltaVToInterceptAtTime(o, burnUT, target, closestApproachTime);

            const int fineness = 20;
            for (double step = 0.5; step < fineness; step += 1.0)
            {
                double testUT = UT + (closestApproachTime - UT) * step / fineness;
                Vector3d testDV = DeltaVToInterceptAtTime(o, testUT, target, closestApproachTime);

                if (testDV.magnitude < dV.magnitude)
                {
                    dV = testDV;
                    burnUT = testUT;
                }
            }

            return dV;
        }

        public static Vector3d DeltaVAndTimeForCheapestCourseCorrection(Orbit o, double UT, Orbit target, CelestialBody targetBody, double finalPeR, out double burnUT)
        {
            Vector3d collisionDV = DeltaVAndTimeForCheapestCourseCorrection(o, UT, target, out burnUT);
            Orbit collisionOrbit = o.PerturbedOrbit(burnUT, collisionDV);
            double collisionUT = collisionOrbit.NextClosestApproachTime(target, burnUT);
            Vector3d collisionPosition = target.SwappedAbsolutePositionAtUT(collisionUT);
            Vector3d collisionRelVel = collisionOrbit.SwappedOrbitalVelocityAtUT(collisionUT) - target.SwappedOrbitalVelocityAtUT(collisionUT);

            double soiEnterUT = collisionUT - targetBody.sphereOfInfluence / collisionRelVel.magnitude;
            Vector3d soiEnterRelVel = collisionOrbit.SwappedOrbitalVelocityAtUT(soiEnterUT) - target.SwappedOrbitalVelocityAtUT(soiEnterUT);

            double E = 0.5 * soiEnterRelVel.sqrMagnitude - targetBody.gravParameter / targetBody.sphereOfInfluence; //total orbital energy on SoI enter
            double finalPeSpeed = Math.Sqrt(2 * (E + targetBody.gravParameter / finalPeR)); //conservation of energy gives the orbital speed at finalPeR.
            double desiredImpactParameter = finalPeR * finalPeSpeed / soiEnterRelVel.magnitude; //conservation of angular momentum gives the required impact parameter

            Vector3d displacementDir = Vector3d.Cross(collisionRelVel, o.SwappedOrbitNormal()).normalized;
            Vector3d interceptTarget = collisionPosition + desiredImpactParameter * displacementDir;

            Vector3d velAfterBurn;
            Vector3d arrivalVel;
            LambertSolver.Solve(o.SwappedRelativePositionAtUT(burnUT), interceptTarget - o.referenceBody.position, collisionUT - burnUT, o.referenceBody, true, out velAfterBurn, out arrivalVel);

            Vector3d deltaV = velAfterBurn - o.SwappedOrbitalVelocityAtUT(burnUT);
            return deltaV;
        }

        public static Vector3d DeltaVAndTimeForCheapestCourseCorrection(Orbit o, double UT, Orbit target, double caDistance, out double burnUT)
        {
            Vector3d collisionDV = DeltaVAndTimeForCheapestCourseCorrection(o, UT, target, out burnUT);
            Orbit collisionOrbit = o.PerturbedOrbit(burnUT, collisionDV);
            double collisionUT = collisionOrbit.NextClosestApproachTime(target, burnUT);
            Vector3d position = o.SwappedAbsolutePositionAtUT(collisionUT);
            Vector3d targetPos = target.SwappedAbsolutePositionAtUT(collisionUT);
            Vector3d direction = targetPos - position;

            Vector3d interceptTarget = targetPos + target.NormalPlus(collisionUT) * caDistance;

            Vector3d velAfterBurn;
            Vector3d arrivalVel;
            LambertSolver.Solve(o.SwappedRelativePositionAtUT(burnUT), interceptTarget - o.referenceBody.position, collisionUT - burnUT, o.referenceBody, true, out velAfterBurn, out arrivalVel);

            Vector3d deltaV = velAfterBurn - o.SwappedOrbitalVelocityAtUT(burnUT);
            return deltaV;
        }

        //Computes the time and delta-V of an ejection burn to a Hohmann transfer from one planet to another. 
        //It's assumed that the initial orbit around the first planet is circular, and that this orbit
        //is in the same plane as the orbit of the first planet around the sun. It's also assumed that
        //the target planet has a fairly low relative inclination with respect to the first planet. If the
        //inclination change is nonzero you should also do a mid-course correction burn, as computed by
        //DeltaVForCourseCorrection.
        public static Vector3d DeltaVAndTimeForInterplanetaryTransferEjection(Orbit o, double UT, Orbit target, bool syncPhaseAngle, out double burnUT)
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
                idealDeltaV = DeltaVAndTimeForHohmannTransfer(planetOrbit, target, UT, out idealBurnUT);
            }
            else
            {
                //don't time the ejection burn to intercept the target; we just care about the final peri/apoapsis
                idealBurnUT = UT;
                if (target.semiMajorAxis < planetOrbit.semiMajorAxis) idealDeltaV = DeltaVToChangePeriapsis(planetOrbit, idealBurnUT, target.semiMajorAxis);
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
            Vector3d inPlaneSoiExitDirection = Vector3d.Exclude(o.SwappedOrbitNormal(), soiExitVelocity).normalized;

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
            Vector3d ejectionOrbitFinalVelocity = sampleEjectionOrbit.SwappedOrbitalVelocityAtUT(ejectionOrbitDuration);

            double turningAngle = Math.Abs(Vector3d.Angle(ejectionOrbitInitialVelocity, ejectionOrbitFinalVelocity));

            //rotate the exit direction by 90 + the turning angle to get a vector pointing to the spot in our orbit
            //where we should do the ejection burn. Then convert this to a true anomaly and compute the time closest
            //to planetUT at which we will pass through that true anomaly.
            Vector3d ejectionPointDirection = Quaternion.AngleAxis(-(float)(90 + turningAngle), o.SwappedOrbitNormal()) * inPlaneSoiExitDirection;
            double ejectionTrueAnomaly = o.TrueAnomalyFromVector(ejectionPointDirection);
            burnUT = o.TimeOfTrueAnomaly(ejectionTrueAnomaly, idealBurnUT - o.period);

            if ((idealBurnUT - burnUT > o.period / 2) || (burnUT < UT))
            {
                burnUT += o.period;
            }

            //rotate the exit direction by the turning angle to get a vector pointing to the spot in our orbit
            //where we should do the ejection burn
            Vector3d ejectionBurnDirection = Quaternion.AngleAxis(-(float)(turningAngle), o.SwappedOrbitNormal()) * inPlaneSoiExitDirection;
            Vector3d ejectionVelocity = ejectionSpeed * ejectionBurnDirection;

            Vector3d preEjectionVelocity = o.SwappedOrbitalVelocityAtUT(burnUT);

            return ejectionVelocity - preEjectionVelocity;
        }

        //Like DeltaVAndTimeForHohmannTransfer, but adds an additional step that uses the Lambert
        //solver to adjust the initial burn to produce an exact intercept instead of an approximate
        public static Vector3d DeltaVAndTimeForHohmannLambertTransfer(Orbit o, Orbit target, double UT, out double burnUT, double subtractProgradeDV = 0)
        {
            Vector3d hohmannDV = DeltaVAndTimeForHohmannTransfer(o, target, UT, out burnUT);
            Vector3d subtractedProgradeDV = subtractProgradeDV * hohmannDV.normalized;

            Orbit hohmannOrbit = o.PerturbedOrbit(burnUT, hohmannDV);
            double apsisTime; //approximate target  intercept time
            if (hohmannOrbit.semiMajorAxis > o.semiMajorAxis) apsisTime = hohmannOrbit.NextApoapsisTime(burnUT);
            else apsisTime = hohmannOrbit.NextPeriapsisTime(burnUT);

            Debug.Log("hohmannDV = " + (Vector3)hohmannDV + ", apsisTime = " + apsisTime);

            Vector3d dV = Vector3d.zero;
            double minCost = 999999;

            double minInterceptTime = apsisTime - hohmannOrbit.period / 4;
            double maxInterceptTime = apsisTime + hohmannOrbit.period / 4;
            const int subdivisions = 30;
            for (int i = 0; i < subdivisions; i++)
            {
                double interceptUT = minInterceptTime + i * (maxInterceptTime - minInterceptTime) / subdivisions;

                Debug.Log("i + " + i + ", trying for intercept at UT = " + interceptUT);

                //Try both short and long way
                bool shortway = true;
                Vector3d interceptBurn = DeltaVToInterceptAtTime(o, burnUT, target, interceptUT, 0, shortway);
                double cost = (interceptBurn - subtractedProgradeDV).magnitude;
                Debug.Log("short way dV = " + interceptBurn.magnitude + "; subtracted cost = " + cost);
                if (cost < minCost)
                {
                    dV = interceptBurn;
                    minCost = cost;
                }

                shortway = false;
                interceptBurn = DeltaVToInterceptAtTime(o, burnUT, target, interceptUT, 0, shortway);
                cost = (interceptBurn - subtractedProgradeDV).magnitude;
                Debug.Log("long way dV = " + interceptBurn.magnitude + "; subtracted cost = " + cost);
                if (cost < minCost)
                {
                    dV = interceptBurn;
                    minCost = cost;
                }
            }

            return dV;
        }

        //Computes the time and delta-V of an ejection burn to a Hohmann transfer from one planet to another. 
        //It's assumed that the initial orbit around the first planet is circular, and that this orbit
        //is in the same plane as the orbit of the first planet around the sun. It's also assumed that
        //the target planet has a fairly low relative inclination with respect to the first planet. If the
        //inclination change is nonzero you should also do a mid-course correction burn, as computed by
        //DeltaVForCourseCorrection.
        public static Vector3d DeltaVAndTimeForInterplanetaryLambertTransferEjection(Orbit o, double UT, Orbit target, out double burnUT)
        {
            Orbit planetOrbit = o.referenceBody.orbit;

            //Compute the time and dV for a Hohmann transfer where we pretend that we are the planet we are orbiting.
            //This gives us the "ideal" deltaV and UT of the ejection burn, if we didn't have to worry about waiting for the right
            //ejection angle and if we didn't have to worry about the planet's gravity dragging us back and increasing the required dV.
            double idealBurnUT;
            Vector3d idealDeltaV;

            //time the ejection burn to intercept the target
            //idealDeltaV = DeltaVAndTimeForHohmannTransfer(planetOrbit, target, UT, out idealBurnUT);
            double vesselOrbitVelocity = OrbitalManeuverCalculator.CircularOrbitSpeed(o.referenceBody, o.semiMajorAxis);
            idealDeltaV = DeltaVAndTimeForHohmannLambertTransfer(planetOrbit, target, UT, out idealBurnUT, vesselOrbitVelocity);

            Debug.Log("idealBurnUT = " + idealBurnUT + ", idealDeltaV = " + idealDeltaV);

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
            Debug.Log("soiExitVelocity = " + (Vector3)soiExitVelocity);

            //compute the angle by which the trajectory turns between periapsis (where we do the ejection burn) 
            //and SOI exit (approximated as radius = infinity)
            double soiExitEnergy = 0.5 * soiExitVelocity.sqrMagnitude - o.referenceBody.gravParameter / o.referenceBody.sphereOfInfluence;
            double ejectionRadius = o.semiMajorAxis; //a guess, good for nearly circular orbits
            Debug.Log("soiExitEnergy = " + soiExitEnergy);
            Debug.Log("ejectionRadius = " + ejectionRadius);

            double ejectionKineticEnergy = soiExitEnergy + o.referenceBody.gravParameter / ejectionRadius;
            double ejectionSpeed = Math.Sqrt(2 * ejectionKineticEnergy);
            Debug.Log("ejectionSpeed = " + ejectionSpeed);

            //construct a sample ejection orbit
            Vector3d ejectionOrbitInitialVelocity = ejectionSpeed * (Vector3d)o.referenceBody.transform.right;
            Vector3d ejectionOrbitInitialPosition = o.referenceBody.position + ejectionRadius * (Vector3d)o.referenceBody.transform.up;
            Orbit sampleEjectionOrbit = MuUtils.OrbitFromStateVectors(ejectionOrbitInitialPosition, ejectionOrbitInitialVelocity, o.referenceBody, 0);
            double ejectionOrbitDuration = sampleEjectionOrbit.NextTimeOfRadius(0, o.referenceBody.sphereOfInfluence);
            Vector3d ejectionOrbitFinalVelocity = sampleEjectionOrbit.SwappedOrbitalVelocityAtUT(ejectionOrbitDuration);

            double turningAngle = Vector3d.Angle(ejectionOrbitInitialVelocity, ejectionOrbitFinalVelocity);
            Debug.Log("turningAngle = " + turningAngle);

            //sine of the angle between the vessel orbit and the desired SOI exit velocity
            double outOfPlaneAngle = (Math.PI / 180) * (90 - Vector3d.Angle(soiExitVelocity, o.SwappedOrbitNormal()));
            Debug.Log("outOfPlaneAngle (rad) = " + outOfPlaneAngle);

            double coneAngle = Math.PI / 2 - (Math.PI / 180) * turningAngle;
            Debug.Log("coneAngle (rad) = " + coneAngle);

            Vector3d exitNormal = Vector3d.Cross(-soiExitVelocity, o.SwappedOrbitNormal()).normalized;
            Vector3d normal2 = Vector3d.Cross(exitNormal, -soiExitVelocity).normalized;

            //unit vector pointing to the spot on our orbit where we will burn.
            //fails if outOfPlaneAngle > coneAngle.
            Vector3d ejectionPointDirection = Math.Cos(coneAngle) * (-soiExitVelocity.normalized)
                + Math.Cos(coneAngle) * Math.Tan(outOfPlaneAngle) * normal2
                - Math.Sqrt(Math.Pow(Math.Sin(coneAngle), 2) - Math.Pow(Math.Cos(coneAngle) * Math.Tan(outOfPlaneAngle), 2)) * exitNormal;

            Debug.Log("soiExitVelocity = " + (Vector3)soiExitVelocity);
            Debug.Log("vessel orbit normal = " + (Vector3)(1000 * o.SwappedOrbitNormal()));
            Debug.Log("exitNormal = " + (Vector3)(1000 * exitNormal));
            Debug.Log("normal2 = " + (Vector3)(1000 * normal2));
            Debug.Log("ejectionPointDirection = " + ejectionPointDirection);


            double ejectionTrueAnomaly = o.TrueAnomalyFromVector(ejectionPointDirection);
            burnUT = o.TimeOfTrueAnomaly(ejectionTrueAnomaly, idealBurnUT - o.period);

            if ((idealBurnUT - burnUT > o.period / 2) || (burnUT < UT))
            {
                burnUT += o.period;
            }

            Vector3d ejectionOrbitNormal = Vector3d.Cross(ejectionPointDirection, soiExitVelocity).normalized;
            Debug.Log("ejectionOrbitNormal = " + ejectionOrbitNormal);
            Vector3d ejectionBurnDirection = Quaternion.AngleAxis(-(float)(turningAngle), ejectionOrbitNormal) * soiExitVelocity.normalized;
            Debug.Log("ejectionBurnDirection = " + ejectionBurnDirection);
            Vector3d ejectionVelocity = ejectionSpeed * ejectionBurnDirection;

            Vector3d preEjectionVelocity = o.SwappedOrbitalVelocityAtUT(burnUT);

            return ejectionVelocity - preEjectionVelocity;
        }

        public static Vector3d DeltaVAndTimeForMoonReturnEjection(Orbit o, double UT, double targetPrimaryRadius, out double burnUT)
        {
            CelestialBody moon = o.referenceBody;
            CelestialBody primary = moon.referenceBody;

            //construct an orbit at the target radius around the primary, in the same plane as the moon. This is a fake target
            Orbit primaryOrbit = new Orbit(moon.orbit.inclination, moon.orbit.eccentricity, targetPrimaryRadius, moon.orbit.LAN, moon.orbit.argumentOfPeriapsis, moon.orbit.meanAnomalyAtEpoch, moon.orbit.epoch, primary);

            return DeltaVAndTimeForInterplanetaryTransferEjection(o, UT, primaryOrbit, false, out burnUT);
        }

        //Computes the delta-V of the burn at a given time required to zero out the difference in orbital velocities
        //between a given orbit and a target.
        public static Vector3d DeltaVToMatchVelocities(Orbit o, double UT, Orbit target)
        {
            return target.SwappedOrbitalVelocityAtUT(UT) - o.SwappedOrbitalVelocityAtUT(UT);
        }

        // Compute the delta-V of the burn at the givent time required to enter an orbit with a period of (resonanceDivider-1)/resonanceDivider of the starting orbit period        
        public static Vector3d DeltaVToResonantOrbit(Orbit o, double UT, double f)
        {
            double a = o.ApR;
            double p = o.PeR;

            // Thanks wolframAlpha for the Math 
            // x = (a^3 f^2 + 3 a^2 f^2 p + 3 a f^2 p^2 + f^2 p^3)^(1/3)-a
            double x = Math.Pow(Math.Pow(a, 3) * Math.Pow(f, 2) + 3 * Math.Pow(a, 2) * Math.Pow(f, 2) * p + 3 * a * Math.Pow(f, 2) * Math.Pow(p, 2) + Math.Pow(f, 2) * Math.Pow(p, 3), 1d / 3) - a;

            if (x < 0)
                return Vector3d.zero;

            if (f > 1)
                return OrbitalManeuverCalculator.DeltaVToChangeApoapsis(o, UT, x);
            else
                return OrbitalManeuverCalculator.DeltaVToChangePeriapsis(o, UT, x);
        }

        // Compute the angular distance between two points on a unit sphere
        public static double Distance(double lat_a, double long_a, double lat_b, double long_b)
        {
            // Using Great-Circle Distance 2nd computational formula from http://en.wikipedia.org/wiki/Great-circle_distance
            // Note the switch from degrees to radians and back
            double lat_a_rad = Math.PI / 180 * lat_a;
            double lat_b_rad = Math.PI / 180 * lat_b;
            double long_diff_rad = Math.PI / 180 * (long_b - long_a);

            return 180 / Math.PI * Math.Atan2(Math.Sqrt(Math.Pow(Math.Cos(lat_b_rad) * Math.Sin(long_diff_rad), 2) +
                Math.Pow(Math.Cos(lat_a_rad) * Math.Sin(lat_b_rad) - Math.Sin(lat_a_rad) * Math.Cos(lat_b_rad) * Math.Cos(long_diff_rad), 2)),
                Math.Sin(lat_a_rad) * Math.Sin(lat_b_rad) + Math.Cos(lat_a_rad) * Math.Cos(lat_b_rad) * Math.Cos(long_diff_rad));
        }

        // Compute an angular heading from point a to point b on a unit sphere
        public static double Heading(double lat_a, double long_a, double lat_b, double long_b)
        {
            // Using Great-Circle Navigation formula for initial heading from http://en.wikipedia.org/wiki/Great-circle_navigation
            // Note the switch from degrees to radians and back
            // Original equation returns 0 for due south, increasing clockwise. We add 180 and clamp to 0-360 degrees to map to compass-type headings
            double lat_a_rad = Math.PI / 180 * lat_a;
            double lat_b_rad = Math.PI / 180 * lat_b;
            double long_diff_rad = Math.PI / 180 * (long_b - long_a);

            return MuUtils.ClampDegrees360(180.0 / Math.PI * Math.Atan2(
                Math.Sin(long_diff_rad),
                Math.Cos(lat_a_rad) * Math.Tan(lat_b_rad) - Math.Sin(lat_a_rad) * Math.Cos(long_diff_rad)));
        }

        //Computes the deltaV of the burn needed to set a given LAN at a given UT.
        public static Vector3d DeltaVToShiftLAN(Orbit o, double UT, double newLAN)
        {
            Vector3d pos = o.SwappedAbsolutePositionAtUT(UT);
            // Burn position in the same reference frame as LAN
            double burn_latitude = o.referenceBody.GetLatitude(pos);
            double burn_longitude = o.referenceBody.GetLongitude(pos) + o.referenceBody.rotationAngle;

            const double target_latitude = 0; // Equator
            double target_longitude = 0; // Prime Meridian

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
            Vector3d actualHorizontalVelocity = Vector3d.Exclude(o.Up(UT), o.SwappedOrbitalVelocityAtUT(UT));
            Vector3d eastComponent = actualHorizontalVelocity.magnitude * Math.Sin(Math.PI / 180 * desiredHeading) * o.East(UT);
            Vector3d northComponent = actualHorizontalVelocity.magnitude * Math.Cos(Math.PI / 180 * desiredHeading) * o.North(UT);
            Vector3d desiredHorizontalVelocity = eastComponent + northComponent;
            return desiredHorizontalVelocity - actualHorizontalVelocity;
        }


        public static Vector3d DeltaVForSemiMajorAxis(Orbit o, double UT, double newSMA)
        {
            bool raising = o.semiMajorAxis < newSMA;
            Vector3d burnDirection = (raising ? 1 : -1) * o.Prograde(UT);
            double minDeltaV = 0;
            double maxDeltaV;
            if (raising)
            {
                //put an upper bound on the required deltaV:
                maxDeltaV = 0.25;
                while (o.PerturbedOrbit(UT, maxDeltaV * burnDirection).semiMajorAxis < newSMA)
                {
                    maxDeltaV *= 2;
                    if (maxDeltaV > 100000)
                        break; //a safety precaution
                }
            }
            else
            {
                //when lowering the SMA, we burn horizontally, and max possible deltaV is the deltaV required to kill all horizontal velocity
                maxDeltaV = Math.Abs(Vector3d.Dot(o.SwappedOrbitalVelocityAtUT(UT), burnDirection));
            }
            // Debug.Log (String.Format ("We are {0} SMA to {1}", raising ? "raising" : "lowering", newSMA));
            // Debug.Log (String.Format ("Starting SMA iteration with maxDeltaV of {0}", maxDeltaV));
            //now do a binary search to find the needed delta-v
            while (maxDeltaV - minDeltaV > 0.01)
            {
                double testDeltaV = (maxDeltaV + minDeltaV) / 2.0;
                double testSMA = o.PerturbedOrbit(UT, testDeltaV * burnDirection).semiMajorAxis;
                // Debug.Log (String.Format ("Testing dV of {0} gave an SMA of {1}", testDeltaV, testSMA));

                if ((testSMA < 0) || (testSMA > newSMA && raising) || (testSMA < newSMA && !raising))
                {
                    maxDeltaV = testDeltaV;
                }
                else
                {
                    minDeltaV = testDeltaV;
                }
            }

            return ((maxDeltaV + minDeltaV) / 2) * burnDirection;
        }

        public static Vector3d DeltaVToShiftNodeLongitude(Orbit o, double UT, double newNodeLong)
        {
            // Get the location underneath the burn location at the current moment.
            // Note that this does NOT account for the rotation of the body that will happen between now
            // and when the vessel reaches the apoapsis.
            Vector3d pos = o.SwappedAbsolutePositionAtUT (UT);
            double burnRadius = o.Radius (UT);
            double oppositeRadius = 0;

            // Back out the rotation of the body to calculate the longitude of the apoapsis when the vessel reaches the node
            double degreeRotationToNode = (UT - Planetarium.GetUniversalTime ()) * 360 / o.referenceBody.rotationPeriod;
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

            while (oppositeRadius-o.referenceBody.Radius < o.referenceBody.timeWarpAltitudeLimits [4] && N < 20) {
                    N++;
                    double target_period = o.referenceBody.rotationPeriod * (LongitudeOffset / 360 + N);
                    target_sma = Math.Pow ((o.referenceBody.gravParameter * target_period * target_period) / (4 * Math.PI * Math.PI), 1.0 / 3.0); // cube roo
                    oppositeRadius = 2 * (target_sma) - burnRadius;
                }
            return DeltaVForSemiMajorAxis (o, UT, target_sma);
        }
    }
}
