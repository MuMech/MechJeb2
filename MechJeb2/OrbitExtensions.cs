﻿using System;
using UnityEngine;

namespace MuMech
{
    public static class OrbitExtensions
    {
        /// <summary>
        /// Get the orbital velocity at a given time in left handed world coordinates.  This value will rotate
        /// due to the inverse rotation tick-to-tick.
        /// </summary>
        /// <param name="o">Orbit</param>
        /// <param name="UT">Universal Time</param>
        /// <returns>World Velocity</returns>
        public static Vector3d WorldOrbitalVelocityAtUT(this Orbit o, double UT)
        {
            return o.getOrbitalVelocityAtUT(UT).xzy;
        }

        /// <summary>
        /// Get the body centered inertial position at a given time in left handed world coordinates.  This value
        /// will rotate due to the inverse rotation tick-to-tick.
        /// </summary>
        /// <param name="o">Orbit</param>
        /// <param name="UT">Universal Time</param>
        /// <returns>BCI World Position</returns>
        public static Vector3d WorldBCIPositionAtUT(this Orbit o, double UT)
        {
            return o.getRelativePositionAtUT(UT).xzy;
        }

        /// <summary>
        /// Get the world space position at a given time in left handed world coordinates.  This value
        /// will rotate due to the inverse rotation tick-to-tick.
        /// </summary>
        /// <param name="o">Orbit</param>
        /// <param name="UT">Universal Time</param>
        /// <returns>World Position</returns>
        public static Vector3d WorldPositionAtUT(this Orbit o, double UT)
        {
            return o.referenceBody.position + o.WorldBCIPositionAtUT(UT);
        }

        //normalized vector perpendicular to the orbital plane
        //convention: as you look down along the orbit normal, the satellite revolves counterclockwise
        public static Vector3d OrbitNormal(this Orbit o)
        {
            return -o.GetOrbitNormal().xzy.normalized;
        }

        //normalized vector pointing radially outward and perpendicular to prograde
        public static Vector3d RadialPlus(this Orbit o, double UT)
        {
            return Vector3d.Exclude(o.Prograde(UT), o.Up(UT)).normalized;
        }

        //another name for the orbit normal; this form makes it look like the other directions
        public static Vector3d NormalPlus(this Orbit o, double UT)
        {
            return o.OrbitNormal();
        }

        //normalized vector parallel to the planet's surface, and pointing in the same general direction as the orbital velocity
        //(parallel to an ideally spherical planet's surface, anyway)
        public static Vector3d Horizontal(this Orbit o, double UT)
        {
            return Vector3d.Exclude(o.Up(UT), o.Prograde(UT)).normalized;
        }

        //horizontal component of the velocity vector
        public static Vector3d HorizontalVelocity(this Orbit o, double UT)
        {
            return Vector3d.Exclude(o.Up(UT), o.WorldOrbitalVelocityAtUT(UT));
        }

        //vertical component of the velocity vector
        public static Vector3d VerticalVelocity(this Orbit o, double UT)
        {
            return Vector3d.Dot(o.Up(UT), o.WorldOrbitalVelocityAtUT(UT)) * o.Up(UT);
        }

        //normalized vector parallel to the planet's surface and pointing in the northward direction
        public static Vector3d North(this Orbit o, double UT)
        {
            return Vector3d.Exclude(o.Up(UT), o.referenceBody.transform.up * (float)o.referenceBody.Radius - o.WorldBCIPositionAtUT(UT))
                .normalized;
        }

        //normalized vector parallel to the planet's surface and pointing in the eastward direction
        public static Vector3d East(this Orbit o, double UT)
        {
            return Vector3d.Cross(o.Up(UT), o.North(UT));
        }

        //distance from the center of the planet
        public static double Radius(this Orbit o, double UT)
        {
            return o.WorldBCIPositionAtUT(UT).magnitude;
        }

        //returns a new Orbit object that represents the result of applying a given dV to o at UT
        public static Orbit PerturbedOrbit(this Orbit o, double UT, Vector3d dV)
        {
            return MuUtils.OrbitFromStateVectors(o.WorldPositionAtUT(UT), o.WorldOrbitalVelocityAtUT(UT) + dV, o.referenceBody, UT);
        }

        // returns a new orbit that is identical to the current one (although the epoch will change)
        // (i tried many different APIs in the orbit class, but the GetOrbitalStateVectors/UpdateFromStateVectors route was the only one that worked)
        public static Orbit Clone(this Orbit o, double UT = double.NegativeInfinity)
        {
            Vector3d pos, vel;

            // hack up a dynamic default value to the current time
            if (UT == double.NegativeInfinity)
                UT = Planetarium.GetUniversalTime();

            var newOrbit = new Orbit();
            o.GetOrbitalStateVectorsAtUT(UT, out pos, out vel);
            newOrbit.UpdateFromStateVectors(pos, vel, o.referenceBody, UT);

            return newOrbit;
        }

        // calculate the next patch, which makes patchEndTransition be valid
        //
        public static Orbit CalculateNextOrbit(this Orbit o, double UT = double.NegativeInfinity)
        {
            var solverParameters = new PatchedConics.SolverParameters();

            // hack up a dynamic default value to the current time
            if (UT == double.NegativeInfinity)
                UT = Planetarium.GetUniversalTime();

            o.StartUT = UT;
            o.EndUT   = o.eccentricity >= 1.0 ? o.period : UT + o.period;
            var nextOrbit = new Orbit();
            PatchedConics.CalculatePatch(o, nextOrbit, UT, solverParameters, null);

            return nextOrbit;
        }

        // This does not allocate a new orbit object and the caller should call new Orbit if/when required
        public static void MutatedOrbit(this Orbit o, double periodOffset = double.NegativeInfinity)
        {
            double UT = Planetarium.GetUniversalTime();

            if (periodOffset.IsFinite())
            {
                Vector3d pos, vel;
                o.GetOrbitalStateVectorsAtUT(UT + o.period * periodOffset, out pos, out vel);
                o.UpdateFromStateVectors(pos, vel, o.referenceBody, UT);
            }
        }

        //mean motion is rate of increase of the mean anomaly
        public static double MeanMotion(this Orbit o)
        {
            if (o.eccentricity > 1)
            {
                return Math.Sqrt(o.referenceBody.gravParameter / Math.Abs(Math.Pow(o.semiMajorAxis, 3)));
            }

            // The above formula is wrong when using the RealSolarSystem mod, which messes with orbital periods.
            // This simpler formula should be foolproof for elliptical orbits:
            return 2 * Math.PI / o.period;
        }

        //distance between two orbiting objects at a given time
        public static double Separation(this Orbit a, Orbit b, double UT)
        {
            return (a.WorldPositionAtUT(UT) - b.WorldPositionAtUT(UT)).magnitude;
        }

        //Time during a's next orbit at which object a comes nearest to object b.
        //If a is hyperbolic, the examined interval is the next 100 units of mean anomaly.
        //This is quite a large segment of the hyperbolic arc. However, for extremely high
        //hyperbolic eccentricity it may not find the actual closest approach.
        public static double NextClosestApproachTime(this Orbit a, Orbit b, double UT)
        {
            double closestApproachTime = UT;
            double closestApproachDistance = double.MaxValue;
            double minTime = UT;
            double interval = a.period;
            if (a.eccentricity > 1)
            {
                interval = 100 / a.MeanMotion(); //this should be an interval of time that covers a large chunk of the hyperbolic arc
            }

            double maxTime = UT + interval;
            const int numDivisions = 20;

            for (int iter = 0; iter < 8; iter++)
            {
                double dt = (maxTime - minTime) / numDivisions;
                for (int i = 0; i < numDivisions; i++)
                {
                    double t = minTime + i * dt;
                    double distance = a.Separation(b, t);
                    if (distance < closestApproachDistance)
                    {
                        closestApproachDistance = distance;
                        closestApproachTime     = t;
                    }
                }

                minTime = MuUtils.Clamp(closestApproachTime - dt, UT, UT + interval);
                maxTime = MuUtils.Clamp(closestApproachTime + dt, UT, UT + interval);
            }

            return closestApproachTime;
        }

        //Distance between a and b at the closest approach found by NextClosestApproachTime
        public static double NextClosestApproachDistance(this Orbit a, Orbit b, double UT)
        {
            return a.Separation(b, a.NextClosestApproachTime(b, UT));
        }

        //The mean anomaly of the orbit.
        //For elliptical orbits, the value return is always between 0 and 2pi
        //For hyperbolic orbits, the value can be any number.
        public static double MeanAnomalyAtUT(this Orbit o, double UT)
        {
            // We use ObtAtEpoch and not meanAnomalyAtEpoch because somehow meanAnomalyAtEpoch
            // can be wrong when using the RealSolarSystem mod. ObtAtEpoch is always correct.
            double ret = (o.ObTAtEpoch + (UT - o.epoch)) * o.MeanMotion();
            if (o.eccentricity < 1) ret = MuUtils.ClampRadiansTwoPi(ret);
            return ret;
        }

        //The next time at which the orbiting object will reach the given mean anomaly.
        //For elliptical orbits, this will be a time between UT and UT + o.period
        //For hyperbolic orbits, this can be any time, including a time in the past, if
        //the given mean anomaly occurred in the past
        public static double UTAtMeanAnomaly(this Orbit o, double meanAnomaly, double UT)
        {
            double currentMeanAnomaly = o.MeanAnomalyAtUT(UT);
            double meanDifference = meanAnomaly - currentMeanAnomaly;
            if (o.eccentricity < 1) meanDifference = MuUtils.ClampRadiansTwoPi(meanDifference);
            return UT + meanDifference / o.MeanMotion();
        }

        //The next time at which the orbiting object will be at periapsis.
        //For elliptical orbits, this will be between UT and UT + o.period.
        //For hyperbolic orbits, this can be any time, including a time in the past,
        //if the periapsis is in the past.
        public static double NextPeriapsisTime(this Orbit o, double UT)
        {
            if (o.eccentricity < 1)
            {
                return o.TimeOfTrueAnomaly(0, UT);
            }

            return UT - o.MeanAnomalyAtUT(UT) / o.MeanMotion();
        }

        //Returns the next time at which the orbiting object will be at apoapsis.
        //For elliptical orbits, this is a time between UT and UT + period.
        //For hyperbolic orbits, this throws an ArgumentException.
        public static double NextApoapsisTime(this Orbit o, double UT)
        {
            if (o.eccentricity < 1)
            {
                return o.TimeOfTrueAnomaly(Math.PI, UT);
            }

            throw new ArgumentException("OrbitExtensions.NextApoapsisTime cannot be called on hyperbolic orbits");
        }

        //Gives the true anomaly (in a's orbit) at which a crosses its ascending node
        //with b's orbit.
        //The returned value is always between 0 and 2 * PI.
        public static double AscendingNodeTrueAnomaly(this Orbit a, Orbit b)
        {
            var vectorToAN = Vector3d.Cross(a.OrbitNormal(), b.OrbitNormal());
            return a.TrueAnomalyFromVector(vectorToAN);
        }

        //Gives the true anomaly (in a's orbit) at which a crosses its descending node
        //with b's orbit.
        //The returned value is always between 0 and 2 * PI.
        public static double DescendingNodeTrueAnomaly(this Orbit a, Orbit b)
        {
            return MuUtils.ClampRadiansTwoPi(a.AscendingNodeTrueAnomaly(b) + Math.PI);
        }

        //Gives the true anomaly at which o crosses the equator going northwards, if o is east-moving,
        //or southwards, if o is west-moving.
        //The returned value is always between 0 and 2 * PI.
        public static double AscendingNodeEquatorialTrueAnomaly(this Orbit o)
        {
            var vectorToAN = Vector3d.Cross(o.referenceBody.transform.up, o.OrbitNormal());
            return o.TrueAnomalyFromVector(vectorToAN);
        }

        //Gives the true anomaly at which o crosses the equator going southwards, if o is east-moving,
        //or northwards, if o is west-moving.
        //The returned value is always between 0 and 2 * PI.
        public static double DescendingNodeEquatorialTrueAnomaly(this Orbit o)
        {
            return MuUtils.ClampRadiansTwoPi(o.AscendingNodeEquatorialTrueAnomaly() + Math.PI);
        }

        //For hyperbolic orbits, the true anomaly only takes on values in the range
        // -M < true anomaly < +M for some M. This function computes M.
        public static double MaximumTrueAnomaly(this Orbit o)
        {
            if (o.eccentricity < 1) return Math.PI;
            return Math.Acos(-1 / o.eccentricity);
        }

        //Returns whether a has an ascending node with b. This can be false
        //if a is hyperbolic and the would-be ascending node is within the opening
        //angle of the hyperbola.
        public static bool AscendingNodeExists(this Orbit a, Orbit b)
        {
            return Math.Abs(MuUtils.ClampRadiansPi(a.AscendingNodeTrueAnomaly(b))) <= a.MaximumTrueAnomaly();
        }

        //Returns whether a has a descending node with b. This can be false
        //if a is hyperbolic and the would-be descending node is within the opening
        //angle of the hyperbola.
        public static bool DescendingNodeExists(this Orbit a, Orbit b)
        {
            return Math.Abs(MuUtils.ClampRadiansPi(a.DescendingNodeTrueAnomaly(b))) <= a.MaximumTrueAnomaly();
        }

        //Returns whether o has an ascending node with the equator. This can be false
        //if o is hyperbolic and the would-be ascending node is within the opening
        //angle of the hyperbola.
        public static bool AscendingNodeEquatorialExists(this Orbit o)
        {
            return Math.Abs(MuUtils.ClampRadiansPi(o.AscendingNodeEquatorialTrueAnomaly())) <= o.MaximumTrueAnomaly();
        }

        //Returns whether o has a descending node with the equator. This can be false
        //if o is hyperbolic and the would-be descending node is within the opening
        //angle of the hyperbola.
        public static bool DescendingNodeEquatorialExists(this Orbit o)
        {
            return Math.Abs(MuUtils.ClampRadiansPi(o.DescendingNodeEquatorialTrueAnomaly())) <= o.MaximumTrueAnomaly();
        }

        //Returns the vector from the primary to the orbiting body at periapsis
        //Better than using Orbit.eccVec because that is zero for circular orbits
        public static Vector3d WorldBCIPositionAtPeriapsis(this Orbit o)
        {
            Vector3d vectorToAN = Quaternion.AngleAxis(-(float)o.LAN, Planetarium.up) * Planetarium.right;
            Vector3d vectorToPe = Quaternion.AngleAxis((float)o.argumentOfPeriapsis, o.OrbitNormal()) * vectorToAN;
            return o.PeR * vectorToPe;
        }

        //Returns the vector from the primary to the orbiting body at apoapsis
        //Better than using -Orbit.eccVec because that is zero for circular orbits
        public static Vector3d WorldBCIPositionAtApoapsis(this Orbit o)
        {
            Vector3d vectorToAN = Quaternion.AngleAxis(-(float)o.LAN, Planetarium.up) * Planetarium.right;
            Vector3d vectorToPe = Quaternion.AngleAxis((float)o.argumentOfPeriapsis, o.OrbitNormal()) * vectorToAN;
            Vector3d ret = -o.ApR * vectorToPe;
            if (double.IsNaN(ret.x))
            {
                Debug.LogError("OrbitExtensions.WorldBCIPositionAtApoapsis got a NaN result!");
                Debug.LogError("o.LAN = " + o.LAN);
                Debug.LogError("o.inclination = " + o.inclination);
                Debug.LogError("o.argumentOfPeriapsis = " + o.argumentOfPeriapsis);
                Debug.LogError("o.OrbitNormal() = " + o.OrbitNormal());
            }

            return ret;
        }

        //Converts a direction, specified by a Vector3d, into a true anomaly.
        //The vector is projected into the orbital plane and then the true anomaly is
        //computed as the angle this vector makes with the vector pointing to the periapsis.
        //The returned value is always between 0 and 360.
        public static double TrueAnomalyFromVector(this Orbit o, Vector3d vec)
        {
            Vector3d oNormal = o.OrbitNormal();
            var projected = Vector3d.Exclude(oNormal, vec);
            Vector3d vectorToPe = o.WorldBCIPositionAtPeriapsis();
            double angleFromPe = Vector3d.Angle(vectorToPe, projected);

            //If the vector points to the infalling part of the orbit then we need to do 360 minus the
            //angle from Pe to get the true anomaly. Test this by taking the the cross product of the
            //orbit normal and vector to the periapsis. This gives a vector that points to center of the
            //outgoing side of the orbit. If vectorToAN is more than 90 degrees from this vector, it occurs
            //during the infalling part of the orbit.
            if (Math.Abs(Vector3d.Angle(projected, Vector3d.Cross(oNormal, vectorToPe))) < 90)
            {
                return angleFromPe * UtilMath.Deg2Rad;
            }

            return (360 - angleFromPe) * UtilMath.Deg2Rad;
        }

        //Originally by Zool, revised by The_Duck
        //Converts a true anomaly into an eccentric anomaly.
        //For elliptical orbits this returns a value between 0 and 2pi
        //For hyperbolic orbits the returned value can be any number.
        //NOTE: For a hyperbolic orbit, if a true anomaly is requested that does not exist (a true anomaly
        //past the true anomaly of the asymptote) then an ArgumentException is thrown
        public static double GetEccentricAnomalyAtTrueAnomaly(this Orbit o, double trueAnomaly)
        {
            double e = o.eccentricity;
            trueAnomaly = MuUtils.ClampRadiansTwoPi(trueAnomaly);

            if (e < 1) //elliptical orbits
            {
                double cosE = (e + Math.Cos(trueAnomaly)) / (1 + e * Math.Cos(trueAnomaly));
                double sinE = Math.Sqrt(1 - cosE * cosE);
                if (trueAnomaly > Math.PI) sinE *= -1;

                return MuUtils.ClampRadiansTwoPi(Math.Atan2(sinE, cosE));
            }

            //hyperbolic orbits
            double coshE = (e + Math.Cos(trueAnomaly)) / (1 + e * Math.Cos(trueAnomaly));
            if (coshE < 1)
                throw new ArgumentException("OrbitExtensions.GetEccentricAnomalyAtTrueAnomaly: True anomaly of " + trueAnomaly +
                                            " radians is not attained by orbit with eccentricity " + o.eccentricity);

            double E = MuUtils.Acosh(coshE);
            if (trueAnomaly > Math.PI) E *= -1;

            return E;
        }

        //Originally by Zool, revised by The_Duck
        //Converts an eccentric anomaly into a mean anomaly.
        //For an elliptical orbit, the returned value is between 0 and 2pi
        //For a hyperbolic orbit, the returned value is any number
        public static double GetMeanAnomalyAtEccentricAnomaly(this Orbit o, double E)
        {
            double e = o.eccentricity;
            if (e < 1) //elliptical orbits
            {
                return MuUtils.ClampRadiansTwoPi(E - e * Math.Sin(E));
            }

            //hyperbolic orbits
            return e * Math.Sinh(E) - E;
        }

        //Converts a true anomaly into a mean anomaly (via the intermediate step of the eccentric anomaly)
        //For elliptical orbits, the output is between 0 and 2pi
        //For hyperbolic orbits, the output can be any number
        //NOTE: For a hyperbolic orbit, if a true anomaly is requested that does not exist (a true anomaly
        //past the true anomaly of the asymptote) then an ArgumentException is thrown
        public static double GetMeanAnomalyAtTrueAnomaly(this Orbit o, double trueAnomaly)
        {
            return o.GetMeanAnomalyAtEccentricAnomaly(o.GetEccentricAnomalyAtTrueAnomaly(trueAnomaly));
        }

        //Returns the next time at which a will cross its ascending node with b.
        //For elliptical orbits this is a time between UT and UT + a.period.
        //For hyperbolic orbits this can be any time, including a time in the past if
        //the ascending node is in the past.
        //NOTE: this function will throw an ArgumentException if a is a hyperbolic orbit and the "ascending node"
        //occurs at a true anomaly that a does not actually ever attain
        public static double TimeOfAscendingNode(this Orbit a, Orbit b, double UT)
        {
            return a.TimeOfTrueAnomaly(a.AscendingNodeTrueAnomaly(b), UT);
        }

        //Returns the next time at which a will cross its descending node with b.
        //For elliptical orbits this is a time between UT and UT + a.period.
        //For hyperbolic orbits this can be any time, including a time in the past if
        //the descending node is in the past.
        //NOTE: this function will throw an ArgumentException if a is a hyperbolic orbit and the "descending node"
        //occurs at a true anomaly that a does not actually ever attain
        public static double TimeOfDescendingNode(this Orbit a, Orbit b, double UT)
        {
            return a.TimeOfTrueAnomaly(a.DescendingNodeTrueAnomaly(b), UT);
        }

        //Returns the next time at which the orbiting object will cross the equator
        //moving northward, if o is east-moving, or southward, if o is west-moving.
        //For elliptical orbits this is a time between UT and UT + o.period.
        //For hyperbolic orbits this can by any time, including a time in the past if the
        //ascending node is in the past.
        //NOTE: this function will throw an ArgumentException if o is a hyperbolic orbit and the
        //"ascending node" occurs at a true anomaly that o does not actually ever attain.
        public static double TimeOfAscendingNodeEquatorial(this Orbit o, double UT)
        {
            return o.TimeOfTrueAnomaly(o.AscendingNodeEquatorialTrueAnomaly(), UT);
        }

        //Returns the next time at which the orbiting object will cross the equator
        //moving southward, if o is east-moving, or northward, if o is west-moving.
        //For elliptical orbits this is a time between UT and UT + o.period.
        //For hyperbolic orbits this can by any time, including a time in the past if the
        //descending node is in the past.
        //NOTE: this function will throw an ArgumentException if o is a hyperbolic orbit and the
        //"descending node" occurs at a true anomaly that o does not actually ever attain.
        public static double TimeOfDescendingNodeEquatorial(this Orbit o, double UT)
        {
            return o.TimeOfTrueAnomaly(o.DescendingNodeEquatorialTrueAnomaly(), UT);
        }

        //Computes the period of the phase angle between orbiting objects a and b.
        //This only really makes sense for approximately circular orbits in similar planes.
        //For noncircular orbits the time variation of the phase angle is only "quasiperiodic"
        //and for high eccentricities and/or large relative inclinations, the relative motion is
        //not really periodic at all.
        public static double SynodicPeriod(this Orbit a, Orbit b)
        {
            int sign = Vector3d.Dot(a.OrbitNormal(), b.OrbitNormal()) > 0 ? 1 : -1; //detect relative retrograde motion
            return Math.Abs(1.0 / (1.0 / a.period - sign * 1.0 / b.period));                      //period after which the phase angle repeats
        }

        //Computes the phase angle between two orbiting objects.
        //This only makes sense if a.referenceBody == b.referenceBody.
        public static double PhaseAngle(this Orbit a, Orbit b, double UT)
        {
            Vector3d normalA = a.OrbitNormal();
            Vector3d posA = a.WorldBCIPositionAtUT(UT);
            var projectedB = Vector3d.Exclude(normalA, b.WorldBCIPositionAtUT(UT));
            double angle = Vector3d.Angle(posA, projectedB);
            if (Vector3d.Dot(Vector3d.Cross(normalA, posA), projectedB) < 0)
            {
                angle = 360 - angle;
            }

            return angle;
        }

        //Computes the angle between two orbital planes. This will be a number between 0 and 180
        //Note that in the convention used two objects orbiting in the same plane but in
        //opposite directions have a relative inclination of 180 degrees.
        public static double RelativeInclination(this Orbit a, Orbit b)
        {
            return Math.Abs(Vector3d.Angle(a.OrbitNormal(), b.OrbitNormal()));
        }

        //Finds the next time at which the orbiting object will achieve a given radius
        //from the center of the primary.
        //If the given radius is impossible for this orbit, an ArgumentException is thrown.
        //For elliptical orbits this will be a time between UT and UT + period
        //For hyperbolic orbits this can be any time. If the given radius will be achieved
        //in the future then the next time at which that radius will be achieved will be returned.
        //If the given radius was only achieved in the past, then there are no guarantees
        //about which of the two times in the past will be returned.
        public static double NextTimeOfRadius(this Orbit o, double UT, double radius)
        {
            if (radius < o.PeR || (o.eccentricity < 1 && radius > o.ApR))
                throw new ArgumentException("OrbitExtensions.NextTimeOfRadius: given radius of " + radius + " is never achieved: o.PeR = " + o.PeR +
                                            " and o.ApR = " + o.ApR);

            double trueAnomaly1 = o.TrueAnomalyAtRadius(radius);
            double trueAnomaly2 = 2 * Math.PI - trueAnomaly1;
            double time1 = o.TimeOfTrueAnomaly(trueAnomaly1, UT);
            double time2 = o.TimeOfTrueAnomaly(trueAnomaly2, UT);
            if (time2 < time1 && time2 > UT) return time2;
            return time1;
        }

        public static Vector3d DeltaVToManeuverNodeCoordinates(this Orbit o, double UT, Vector3d dV)
        {
            return new Vector3d(Vector3d.Dot(o.RadialPlus(UT), dV),
                Vector3d.Dot(-o.NormalPlus(UT), dV),
                Vector3d.Dot(o.Prograde(UT), dV));
        }

        // Return the orbit of the parent body orbiting the sun
        public static Orbit TopParentOrbit(this Orbit orbit)
        {
            Orbit result = orbit;
            while (result.referenceBody != Planetarium.fetch.Sun)
            {
                result = result.referenceBody.orbit;
            }

            return result;
        }

        public static string MuString(this Orbit o)
        {
            return "PeA:" + o.PeA + " ApA:" + o.ApA + " SMA:" + o.semiMajorAxis + " ECC:" + o.eccentricity + " INC:" + o.inclination + " LAN:" +
                   o.LAN + " ArgP:" + o.argumentOfPeriapsis + " TA:" + o.trueAnomaly;
        }

        public static double SuicideBurnCountdown(Orbit orbit, VesselState vesselState, Vessel vessel)
        {
            if (vesselState.mainBody == null) return 0;
            if (orbit.PeA > 0) return double.PositiveInfinity;

            double angleFromHorizontal = 90 - Vector3d.Angle(-vessel.srf_velocity, vesselState.up);
            angleFromHorizontal = MuUtils.Clamp(angleFromHorizontal, 0, 90);
            double sine = Math.Sin(angleFromHorizontal * UtilMath.Deg2Rad);
            double g = vesselState.localg;
            double T = vesselState.limitedMaxThrustAccel;

            double effectiveDecel = 0.5 * (-2 * g * sine + Math.Sqrt(2 * g * sine * (2 * g * sine) + 4 * (T * T - g * g)));
            double decelTime = vesselState.speedSurface / effectiveDecel;

            Vector3d estimatedLandingSite = vesselState.CoM + 0.5 * decelTime * vessel.srf_velocity;
            double terrainRadius = vesselState.mainBody.Radius + vesselState.mainBody.TerrainAltitude(estimatedLandingSite);
            double impactTime = 0;
            try
            {
                impactTime = orbit.NextTimeOfRadius(vesselState.time, terrainRadius);
            }
            catch (ArgumentException)
            {
                return 0;
            }

            return impactTime - decelTime / 2 - vesselState.time;
        }
    }
}
