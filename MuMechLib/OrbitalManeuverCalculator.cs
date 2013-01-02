using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    //Todo: add transfer calculations
    //      add interplanetary transfer calculations
    //      add course correction calculations
    public static class OrbitalManeuverCalculator
    {
        public static double CircularOrbitSpeed(CelestialBody body, double radius)
        {
            //v = sqrt(GM/r)
            return Math.Sqrt(body.gravParameter / radius);
        }

        public static Vector3d DeltaVToCircularize(Orbit o, double UT)
        {
            Vector3d desiredVelocity = CircularOrbitSpeed(o.referenceBody, o.Radius(UT)) * o.Horizontal(UT);
            Vector3d actualVelocity = o.SwappedOrbitalVelocityAtUT(UT);
            return desiredVelocity - actualVelocity;
        }

        public static Vector3d DeltaVToEllipticize(Orbit o, double UT, double newPeR, double newApR)
        {
            double radius = o.Radius(UT);
            if (radius < newPeR || radius > newApR || newPeR < 0)
            {
                return Vector3d.zero;
            }
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



        public static Vector3d DeltaVToChangePeriapsis(Orbit o, double UT, double newPeR)
        {
            double radius = o.Radius(UT);

            //don't bother with impossible maneuvers:
            if (newPeR > radius || newPeR < 0) return Vector3d.zero;

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


        public static Vector3d DeltaVToChangeApoapsis(Orbit o, double UT, double newApR)
        {
            double radius = o.Radius(UT);

            //don't bother with impossible maneuvers:
            if (newApR < radius) return Vector3d.zero;

            //are we raising or lowering the periapsis?
            bool raising = (o.ApR > 0 && newApR > o.ApR);

            Vector3d burnDirection = (raising ? 1 : -1) * o.Prograde(UT);

            double minDeltaV = 0;
            double maxDeltaV;
            if (raising)
            {
                //put an upper bound on the required deltaV:
                maxDeltaV = 0.25;

                double ap = o.ApR;
                while (ap > 0 && ap < newApR)
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

                if ((raising && (testApoapsis < 0 || testApoapsis > newApR)) ||
                    (!raising && (testApoapsis > 0 && testApoapsis < newApR)))
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


        //Aome 3d geometry relates our heading with the inclination and the latitude.
        //Both inputs are in degrees.
        //Convention: At equator, inclination    0 => heading 90 (east) 
        //                        inclination   90 => heading 0  (north)
        //                        inclination  -90 => heading 180 (south)
        //                        inclination ±180 => heading 270 (west)
        public static double HeadingForInclination(double desiredInclination, double latitudeDegrees)
        {
            double cosDesiredSurfaceAngle = Math.Cos(desiredInclination * Math.PI / 180) / Math.Cos(latitudeDegrees * Math.PI / 180);
            if (Math.Abs(cosDesiredSurfaceAngle) > 1.0)
            {
                //If inclination < latitude, we get this case: the desired inclination is impossible
                if (Math.Abs(MuUtils.ClampDegrees180(desiredInclination)) < 90) return 90;
                else return 270;
            }
            else
            {
                double angleFromEast = (180 / Math.PI) * Math.Acos(cosDesiredSurfaceAngle); //an angle between 0 and 180
                if (desiredInclination < 0) angleFromEast *= -1;
                //now angleFromEast is between -180 and 180

                return MuUtils.ClampDegrees360(90 - angleFromEast);
            }
        }

        //inclination convention: 
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

        public static Vector3d DeltaVAndTimeToMatchPlanesAscending(Orbit o, Orbit target, double UT, out double burnUT)
        {
            burnUT = o.TimeOfAscendingNode(target, UT);
            Vector3d desiredHorizontal = Vector3d.Cross(target.SwappedOrbitNormal(), o.Up(burnUT));
            Vector3d actualHorizontalVelocity = Vector3d.Exclude(o.Up(burnUT), o.SwappedOrbitalVelocityAtUT(burnUT));
            Vector3d desiredHorizontalVelocity = actualHorizontalVelocity.magnitude * desiredHorizontal;
            return desiredHorizontalVelocity - actualHorizontalVelocity;
        }

        public static Vector3d DeltaVAndTimeToMatchPlanesDescending(Orbit o, Orbit target, double UT, out double burnUT)
        {
            burnUT = o.TimeOfDescendingNode(target, UT);
            Vector3d desiredHorizontal = Vector3d.Cross(target.SwappedOrbitNormal(), o.Up(burnUT));
            Vector3d actualHorizontalVelocity = Vector3d.Exclude(o.Up(burnUT), o.SwappedOrbitalVelocityAtUT(burnUT));
            Vector3d desiredHorizontalVelocity = actualHorizontalVelocity.magnitude * desiredHorizontal;
            return desiredHorizontalVelocity - actualHorizontalVelocity;
        }


        //Assumes o and target are in approximately the same plane, and orbiting in the same direction.
        //Also assumes that o is a perfectly circular orbit.
        //Computes the time and dV of a Hohmman transfer injection burn such that at apoapsis the transfer
        //orbit passes as close as possible to the target.
        public static Vector3d DeltaVAndTimeForHohmannTransfer(Orbit o, Orbit target, double UT, out double burnUT)
        {
            if (o.eccentricity > 1 || target.eccentricity > 1) throw new ArgumentException("Orbits must be elliptical (o.eccentricity = " + o.eccentricity + "; target.eccentricity = " + target.eccentricity + ")");

            double synodicPeriod = o.SynodicPeriod(target);

            Vector3d burnDV = Vector3d.zero;
            burnUT = UT;
            double bestApproachDistance = Double.MaxValue;
            double minTime = UT;
            double maxTime = UT + synodicPeriod;
            int numDivisions = 20;

            for (int iter = 0; iter < 8; iter++)
            {
                double dt = (maxTime - minTime) / numDivisions;
                for (int i = 0; i < numDivisions; i++)
                {
                    double t = minTime + i * dt;
                    Vector3d apsisDirection = -o.SwappedRelativePositionAtUT(t);
                    double desiredApsis = target.RadiusAtTrueAnomaly(target.TrueAnomalyFromVector(apsisDirection));

                    double approachDistance;
                    Vector3d burn;
                    if (desiredApsis > o.ApR)
                    {
                        burn = DeltaVToChangeApoapsis(o, t, desiredApsis);
                        Orbit transferOrbit = o.PerturbedOrbit(t, burn);
                        approachDistance = transferOrbit.Separation(target, transferOrbit.NextApoapsisTime(t));
                    }
                    else
                    {
                        burn = DeltaVToChangePeriapsis(o, t, desiredApsis);
                        Orbit transferOrbit = o.PerturbedOrbit(t, burn);
                        approachDistance = transferOrbit.Separation(target, transferOrbit.NextPeriapsisTime(t));
                    }

                    if (approachDistance < bestApproachDistance)
                    {
                        bestApproachDistance = approachDistance;
                        burnUT = t;
                        burnDV = burn;
                    }
                }
                minTime = MuUtils.Clamp(burnUT - dt, UT, UT + synodicPeriod);
                maxTime = MuUtils.Clamp(burnUT + dt, UT, UT + synodicPeriod);
            }

            return burnDV;
        }

        public static Vector3d DeltaVToInterceptAtTime(Orbit o, double UT, Orbit target, double interceptUT)
        {
            double initialT = UT;
            Vector3d initialRelPos = o.SwappedRelativePositionAtUT(initialT);
            double finalT = interceptUT;
            Vector3d finalRelPos = target.SwappedRelativePositionAtUT(finalT);

            double targetOrbitalSpeed = o.SwappedOrbitalVelocityAtUT(finalT).magnitude;
            double deltaTPrecision = 20.0 / targetOrbitalSpeed;

            Debug.Log("initialT = " + initialT);
            Debug.Log("finalT = " + finalT);
            Debug.Log("deltaTPrecision = " + deltaTPrecision);

            Vector3d initialVelocity, finalVelocity;
            LambertSolver.Solve(initialRelPos, finalRelPos, finalT - initialT, o.referenceBody, true, out initialVelocity, out finalVelocity);

            Vector3d currentInitialVelocity = o.SwappedOrbitalVelocityAtUT(initialT);
            return initialVelocity - currentInitialVelocity;
        }

        public static Vector3d DeltaVForCourseCorrection(Orbit o, double UT, Orbit target)
        {
            double closestApproachTime = o.NextClosestApproachTime(target, UT);
            return DeltaVToInterceptAtTime(o, UT, target, closestApproachTime);
        }

        public static Vector3d DeltaVAndTimeForInterplanetaryTransferEjection(Orbit o, double UT, Orbit target, out double burnUT)
        {
            Orbit planetOrbit = o.referenceBody.orbit;

            //Compute the time and dV for a Hohmann transfer where we pretend that we are the planet we are orbiting.
            //This gives us the "ideal" deltaV and UT of the ejection burn, if we didn't have to worry about waiting for the right
            //ejection angle and if we didn't have to worry about the planet's gravity dragging us back and increasing the required dV.
            double idealBurnUT;
            Vector3d idealDeltaV = DeltaVAndTimeForHohmannTransfer(planetOrbit, target, UT, out idealBurnUT);

            Debug.Log("idealBurnUT - UT = " + (idealBurnUT - UT));
            Debug.Log("idealDeltaV = " + idealDeltaV);

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

            Debug.Log("soiExitVelocity = " + soiExitVelocity);
            Debug.Log("inPlaneSoiExitDirection = " + inPlaneSoiExitDirection);


            //compute the angle by which the trajectory turns between periapsis (where we do the ejection burn) 
            //and SOI exit (approximated as radius = infinity)
            double soiExitEnergy = 0.5 * soiExitVelocity.sqrMagnitude - o.referenceBody.gravParameter / o.referenceBody.sphereOfInfluence;
            double ejectionRadius = o.semiMajorAxis; //a guess, good for nearly circular orbits

            double ejectionKineticEnergy = soiExitEnergy + o.referenceBody.gravParameter / ejectionRadius;
            double ejectionSpeed = Math.Sqrt(2 * ejectionKineticEnergy);

            Debug.Log("soiExitEnergy = " + soiExitEnergy);
            Debug.Log("ejectionRadius = " + ejectionRadius);
            Debug.Log("ejectionKineticEnergy = " + ejectionKineticEnergy);
            Debug.Log("ejectionSpeed = " + ejectionSpeed);

            //construct a sample ejection orbit
            Vector3d ejectionOrbitInitialVelocity = ejectionSpeed * (Vector3d)o.referenceBody.transform.right;
            Orbit sampleEjectionOrbit = MuUtils.OrbitFromStateVectors(o.referenceBody.position + ejectionRadius * (Vector3d)o.referenceBody.transform.up, ejectionOrbitInitialVelocity, o.referenceBody, 0);
            //double ejectionOrbitFinalTrueAnomaly = 180 / Math.PI * sampleEjectionOrbit.TrueAnomalyAtRadius(o.referenceBody.sphereOfInfluence);
            //double ejectionOrbitDuration = sampleEjectionOrbit.TimeOfTrueAnomaly(ejectionOrbitFinalTrueAnomaly, 0);
            double ejectionOrbitDuration = sampleEjectionOrbit.NextTimeOfRadius(0, o.referenceBody.sphereOfInfluence);
            Vector3d ejectionOrbitFinalVelocity = sampleEjectionOrbit.SwappedOrbitalVelocityAtUT(ejectionOrbitDuration);

            double turningAngle = Math.Abs(Vector3d.Angle(ejectionOrbitInitialVelocity, ejectionOrbitFinalVelocity));

            Debug.Log("ejectionOrbitInitialVElocity = " + ejectionOrbitInitialVelocity);
            //Debug.Log("ejectionOrbitFinalTrueAnomaly = " + ejectionOrbitFinalTrueAnomaly);
            Debug.Log("ejectionOrbitDuration = " + ejectionOrbitDuration);
            Debug.Log("ejectionOrbitFinalVelocity = " + ejectionOrbitFinalVelocity);
            Debug.Log("turningAngle = " + turningAngle);

/*            double hyperbolaSemiMajorAxis = -o.referenceBody.gravParameter / (2 * soiExitEnergy);
            double hyperbolaEccentricity = 1 - ejectionRadius / hyperbolaSemiMajorAxis;
            double turningAngle = 180 / Math.PI * Math.Asin(1 / hyperbolaEccentricity);

            Debug.Log("ejectionRadius = " + ejectionRadius);
            Debug.Log("hyperbolaEccentricity = " + hyperbolaEccentricity);
            Debug.Log("turningAngle = " + turningAngle);*/

            //rotate the exit direction by 90 + the turning angle to get a vector pointing to the spot in our orbit
            //where we should do the ejection burn. Then convert this to a true anomaly and compute the time closest
            //to planetUT at which we will pass through that true anomaly.
            Vector3d ejectionPointDirection = Quaternion.AngleAxis(-(float)(90+turningAngle), o.SwappedOrbitNormal()) * inPlaneSoiExitDirection;
            double ejectionTrueAnomaly = o.TrueAnomalyFromVector(ejectionPointDirection);
            burnUT = o.TimeOfTrueAnomaly(ejectionTrueAnomaly, idealBurnUT - o.period);
            
            if (idealBurnUT - burnUT > o.period / 2)
            {
                Debug.Log("upping burnUT by one period");
                burnUT += o.period;
            }
            Debug.Log("ejectionPointDirection = " + ejectionPointDirection);
            Debug.Log("relPos at burnUT = " + o.SwappedRelativePositionAtUT(burnUT));


            Debug.Log("ejectionTrueAnomaly = " + ejectionTrueAnomaly);
            Debug.Log("(burnUT - UT) = " + (burnUT - UT));


            Debug.Log("ejectionSpeed = " + ejectionSpeed);

            //rotate the exit direction by the turning angle to get a vector pointing to the spot in our orbit
            //where we should do the ejection burn
            Vector3d ejectionBurnDirection = Quaternion.AngleAxis(-(float)(turningAngle), o.SwappedOrbitNormal()) * inPlaneSoiExitDirection;
            Vector3d ejectionVelocity = ejectionSpeed * ejectionBurnDirection;

            Debug.Log("ejectionVelocity = " + ejectionVelocity);
            Debug.Log("obtvel at burnUT = " + o.SwappedOrbitalVelocityAtUT(burnUT));

            Vector3d preEjectionVelocity = o.SwappedOrbitalVelocityAtUT(burnUT);

            return ejectionVelocity - preEjectionVelocity;
        }


        public static Vector3d DeltaVToMatchVelocities(Orbit o, double UT, Orbit target)
        {
            return target.SwappedOrbitalVelocityAtUT(UT) - o.SwappedOrbitalVelocityAtUT(UT);
        }
    }

}
    