using System;
using System.Runtime.CompilerServices;
using MechJebLib.Functions;
using MechJebLib.Primitives;
using MechJebLib.TwoBody;
using MechJebLibBindings;
using UnityEngine;
using static MechJebLib.Utils.Statics;
using static System.Math;

namespace MuMech
{
    public static class OrbitExtensions
    {
        /// <summary>
        ///     Get the orbital velocity at a given time in left handed world coordinates.  This value will rotate
        ///     due to the inverse rotation tick-to-tick.
        /// </summary>
        /// <param name="o">Orbit</param>
        /// <param name="ut">Universal Time</param>
        /// <returns>World Velocity</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d WorldOrbitalVelocityAtUT(this Orbit o, double ut) => o.getOrbitalVelocityAtUT(ut).xzy;

        /// <summary>
        ///     Get the body centered inertial position at a given time in left handed world coordinates.  This value
        ///     will rotate due to the inverse rotation tick-to-tick.
        /// </summary>
        /// <param name="o">Orbit</param>
        /// <param name="ut">Universal Time</param>
        /// <returns>BCI World Position</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d WorldBCIPositionAtUT(this Orbit o, double ut) => o.getRelativePositionAtUT(ut).xzy;

        /// <summary>
        ///     Get the world space position at a given time in left handed world coordinates.  This value
        ///     will rotate due to the inverse rotation tick-to-tick.
        /// </summary>
        /// <param name="o">Orbit</param>
        /// <param name="ut">Universal Time</param>
        /// <returns>World Position</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d WorldPositionAtUT(this Orbit o, double ut) => o.referenceBody.position + o.WorldBCIPositionAtUT(ut);

        /// <summary>
        ///     Get the orbital velocity at a given time in right handed coordinates.  This value will rotate
        ///     due to the inverse rotation tick-to-tick.
        /// </summary>
        /// <param name="o">Orbit</param>
        /// <param name="ut">Universal Time</param>
        /// <returns>World Velocity</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static V3 RightHandedOrbitalVelocityAtUT(this Orbit o, double ut) => o.getOrbitalVelocityAtUT(ut).ToV3();

        /// <summary>
        ///     Get the body centered inertial position at a given time in right handed coordinates.  This value
        ///     will rotate due to the inverse rotation tick-to-tick.
        /// </summary>
        /// <param name="o">Orbit</param>
        /// <param name="ut">Universal Time</param>
        /// <returns>BCI World Position</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static V3 RightHandedBCIPositionAtUT(this Orbit o, double ut) => o.getRelativePositionAtUT(ut).ToV3();

        /// <summary>
        ///     Get both position and velocity state vectors at a given time in right handed coordinates.  This value
        ///     will rotate due to the inverse rotation tick-to-tick.
        /// </summary>
        /// <param name="o">Orbit</param>
        /// <param name="ut">Universal Time</param>
        /// <returns>BCI World Position</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (V3 pos, V3 vel) RightHandedStateVectorsAtUT(this Orbit o, double ut)
        {
            // GetOrbitalStateVectorsAtUT() uses a crazy future-rotation-at-UT to rotate vectors, so carefully avoid it.
            o.GetOrbitalStateVectorsAtTrueAnomaly(o.TrueAnomalyAtT(o.getObtAtUT(ut)), ut, false, out Vector3d pos, out Vector3d vel);
            pos = Planetarium.Zup.WorldToLocal(pos);
            vel = Planetarium.Zup.WorldToLocal(vel);
            return (pos.ToV3(), vel.ToV3());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FixedGetOrbitalStateVectorsAtUT(this Orbit o, double ut, out Vector3d pos, out Vector3d vel)
        {
            o.GetOrbitalStateVectorsAtTrueAnomaly(o.TrueAnomalyAtT(o.getObtAtUT(ut)), ut, false, out pos, out vel);
            pos = Planetarium.Zup.WorldToLocal(pos);
            vel = Planetarium.Zup.WorldToLocal(vel);
        }

        //normalized vector perpendicular to the orbital plane
        //convention: as you look down along the orbit normal, the satellite revolves counterclockwise
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d OrbitNormal(this Orbit o) => -o.GetOrbitNormal().xzy.normalized;

        //normalized vector pointing radially outward and perpendicular to prograde
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d RadialPlus(this Orbit o, double ut) => Vector3d.Exclude(o.Prograde(ut), o.Up(ut)).normalized;

        //another name for the orbit normal; this form makes it look like the other directions
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d NormalPlus(this Orbit o, double ut) => o.OrbitNormal();

        //normalized vector parallel to the planet's surface, and pointing in the same general direction as the orbital velocity
        //(parallel to an ideally spherical planet's surface, anyway)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d Horizontal(this Orbit o, double ut) => Vector3d.Exclude(o.Up(ut), o.Prograde(ut)).normalized;

        //horizontal component of the velocity vector
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d HorizontalVelocity(this Orbit o, double ut) => Vector3d.Exclude(o.Up(ut), o.WorldOrbitalVelocityAtUT(ut));

        //vertical component of the velocity vector
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d VerticalVelocity(this Orbit o, double ut) => Vector3d.Dot(o.Up(ut), o.WorldOrbitalVelocityAtUT(ut)) * o.Up(ut);

        //normalized vector parallel to the planet's surface and pointing in the northward direction
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d North(this Orbit o, double ut) =>
            Vector3d.Exclude(o.Up(ut), o.referenceBody.transform.up * (float)o.referenceBody.Radius - o.WorldBCIPositionAtUT(ut))
               .normalized;

        //normalized vector parallel to the planet's surface and pointing in the eastward direction
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3d East(this Orbit o, double ut) => Vector3d.Cross(o.Up(ut), o.North(ut));

        //distance from the center of the planet
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Radius(this Orbit o, double ut) => o.WorldBCIPositionAtUT(ut).magnitude;

        //returns a new Orbit object that represents the result of applying a given dV to o at UT
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Orbit PerturbedOrbit(this Orbit o, double ut, Vector3d dV) =>
            MuUtils.OrbitFromStateVectors(o.WorldPositionAtUT(ut), o.WorldOrbitalVelocityAtUT(ut) + dV, o.referenceBody, ut);

        // circular orbital speed at this instant
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double CircularOrbitSpeed(this Orbit o) => Astro.CircularVelocity(o.referenceBody.gravParameter, o.radius);

        // circular orbital period at this instant
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double CircularOrbitPeriod(this Orbit o) => TAU * o.radius / o.CircularOrbitSpeed();

        //distance between two orbiting objects at a given time
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Separation(this Orbit a, Orbit b, double ut) => (a.WorldPositionAtUT(ut) - b.WorldPositionAtUT(ut)).magnitude;

        /// <summary>
        ///     Finds when object a is nearest to object b during the window starting at ut.  This is the global minimum of the
        ///     separation over the window, NOT the next local minimum: passes with a larger separation are skipped, however much
        ///     sooner they come.
        ///     The window is a's period, or the next 100 units of mean anomaly if a is hyperbolic.  That is quite a large segment
        ///     of the hyperbolic arc, but with an extremely high eccentricity it may not reach the actual minimum.  The window
        ///     stops where the patch of a ends by leaving the sphere of influence, meeting a body or hitting one, since a isn't on
        ///     this orbit after that.
        ///     The result can be an end of the window: ut when nothing ahead is closer, and the end when still closing there.
        ///     Both orbits must share a reference body.
        /// </summary>
        /// <param name="a">The orbit that sets the window</param>
        /// <param name="b">The other orbit</param>
        /// <param name="ut">Start of the window</param>
        /// <returns>The UT of the smallest separation, or ut if either orbit is not valid</returns>
        public static double MinimumSeparationTime(this Orbit a, Orbit b, double ut)
        {
            if (a.referenceBody != b.referenceBody)
                throw new ArgumentException($"MinimumSeparationTime: the orbits must share a reference body, not {a.referenceBody.bodyName} and {b.referenceBody.bodyName}");

            double interval = a.period;
            if (a.eccentricity >= 1)
            {
                interval = 100 / a.meanMotion; //this should be an interval of time that covers a large chunk of the hyperbolic arc
            }

            bool patchEnds = a.patchEndTransition == Orbit.PatchTransitionType.ENCOUNTER || a.patchEndTransition == Orbit.PatchTransitionType.ESCAPE ||
                a.patchEndTransition == Orbit.PatchTransitionType.IMPACT;
            if (patchEnds && a.EndUT > ut) interval = Min(interval, a.EndUT - ut);

            //The UI calls this every frame, so a bad orbit returns ut instead of reaching the solver, which would throw.
            if (double.IsNaN(a.semiMajorAxis) || double.IsNaN(b.semiMajorAxis) || !IsFinite(interval)) return ut;

            (V3 ra, V3 va) = a.RightHandedStateVectorsAtUT(ut);
            (V3 rb, V3 vb) = b.RightHandedStateVectorsAtUT(ut);
            (double dt, double _) = MinimumSeparation.Solve(a.referenceBody.gravParameter, ra, va, rb, vb, interval);

            return ut + dt;
        }

        //Distance between a and b at the time found by MinimumSeparationTime: see there for what that does and doesn't find.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double MinimumSeparationDistance(this Orbit a, Orbit b, double ut) => a.Separation(b, a.MinimumSeparationTime(b, ut));

        //The mean anomaly of the orbit.
        //For elliptical orbits, the value return is always between 0 and 2pi
        //For hyperbolic orbits, the value can be any number.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double MeanAnomalyAtUT(this Orbit o, double ut)
        {
            // We use ObtAtEpoch and not meanAnomalyAtEpoch because somehow meanAnomalyAtEpoch
            // can be wrong when using the RealSolarSystem mod. ObtAtEpoch is always correct.
            double ret = (o.ObTAtEpoch + (ut - o.epoch)) * o.meanMotion;
            if (o.eccentricity < 1) ret = Clamp2Pi(ret);
            return ret;
        }

        //The next time at which the orbiting object will reach the given mean anomaly.
        //For elliptical orbits, this will be a time between UT and UT + o.period
        //For hyperbolic orbits, this can be any time, including a time in the past, if
        //the given mean anomaly occurred in the past
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double UTAtMeanAnomaly(this Orbit o, double meanAnomaly, double ut)
        {
            double currentMeanAnomaly = o.MeanAnomalyAtUT(ut);
            double meanDifference = meanAnomaly - currentMeanAnomaly;
            if (o.eccentricity < 1) meanDifference = Clamp2Pi(meanDifference);
            return ut + meanDifference / o.meanMotion;
        }

        //The next time at which the orbiting object will be at periapsis.
        //For elliptical orbits, this will be between UT and UT + o.period.
        //For hyperbolic orbits, this can be any time, including a time in the past,
        //if the periapsis is in the past.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double NextPeriapsisTime(this Orbit o, double ut)
        {
            if (o.eccentricity < 1)
            {
                return o.TimeOfTrueAnomaly(0, ut);
            }

            return ut - o.MeanAnomalyAtUT(ut) / o.meanMotion;
        }

        //Returns the next time at which the orbiting object will be at apoapsis.
        //For elliptical orbits, this is a time between UT and UT + period.
        //For hyperbolic orbits, this throws an ArgumentException.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double NextApoapsisTime(this Orbit o, double ut)
        {
            if (o.eccentricity < 1)
            {
                return o.TimeOfTrueAnomaly(PI, ut);
            }

            throw new ArgumentException("OrbitExtensions.NextApoapsisTime cannot be called on hyperbolic orbits");
        }

        //Gives the true anomaly (in a's orbit) at which a crosses its ascending node
        //with b's orbit.
        //The returned value is always between 0 and 2 * PI.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double AscendingNodeTrueAnomaly(this Orbit a, Orbit b)
        {
            var vectorToAN = Vector3d.Cross(a.OrbitNormal(), b.OrbitNormal());
            return a.TrueAnomalyFromVector(vectorToAN);
        }

        //Gives the true anomaly (in a's orbit) at which a crosses its descending node
        //with b's orbit.
        //The returned value is always between 0 and 2 * PI.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double DescendingNodeTrueAnomaly(this Orbit a, Orbit b) => Clamp2Pi(a.AscendingNodeTrueAnomaly(b) + PI);

        //Gives the true anomaly at which o crosses the equator going northwards, if o is east-moving,
        //or southwards, if o is west-moving.
        //The returned value is always between 0 and 2 * PI.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double AscendingNodeEquatorialTrueAnomaly(this Orbit o)
        {
            var vectorToAN = Vector3d.Cross(o.referenceBody.transform.up, o.OrbitNormal());
            return o.TrueAnomalyFromVector(vectorToAN);
        }

        //Gives the true anomaly at which o crosses the equator going southwards, if o is east-moving,
        //or northwards, if o is west-moving.
        //The returned value is always between 0 and 2 * PI.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double DescendingNodeEquatorialTrueAnomaly(this Orbit o) =>
            Clamp2Pi(o.AscendingNodeEquatorialTrueAnomaly() + PI);

        //For hyperbolic orbits, the true anomaly only takes on values in the range
        // -M < true anomaly < +M for some M. This function computes M.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double MaximumTrueAnomaly(this Orbit o)
        {
            if (o.eccentricity < 1) return PI;
            return Acos(-1 / o.eccentricity);
        }

        //Returns whether a has an ascending node with b. This can be false
        //if a is hyperbolic and the would-be ascending node is within the opening
        //angle of the hyperbola.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AscendingNodeExists(this Orbit a, Orbit b) =>
            Abs(ClampPi(a.AscendingNodeTrueAnomaly(b))) <= a.MaximumTrueAnomaly();

        //Returns whether a has a descending node with b. This can be false
        //if a is hyperbolic and the would-be descending node is within the opening
        //angle of the hyperbola.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool DescendingNodeExists(this Orbit a, Orbit b) =>
            Abs(ClampPi(a.DescendingNodeTrueAnomaly(b))) <= a.MaximumTrueAnomaly();

        //Returns whether o has an ascending node with the equator. This can be false
        //if o is hyperbolic and the would-be ascending node is within the opening
        //angle of the hyperbola.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AscendingNodeEquatorialExists(this Orbit o) =>
            Abs(ClampPi(o.AscendingNodeEquatorialTrueAnomaly())) <= o.MaximumTrueAnomaly();

        //Returns whether o has a descending node with the equator. This can be false
        //if o is hyperbolic and the would-be descending node is within the opening
        //angle of the hyperbola.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool DescendingNodeEquatorialExists(this Orbit o) =>
            Abs(ClampPi(o.DescendingNodeEquatorialTrueAnomaly())) <= o.MaximumTrueAnomaly();

        //Returns the vector from the primary to the orbiting body at periapsis
        //Better than using Orbit.eccVec because that is zero for circular orbits
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector3d WorldBCIPositionAtPeriapsis(this Orbit o)
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
            if (Abs(Vector3d.Angle(projected, Vector3d.Cross(oNormal, vectorToPe))) < 90)
            {
                return angleFromPe * UtilMath.Deg2Rad;
            }

            return (360 - angleFromPe) * UtilMath.Deg2Rad;
        }

        //Converts a true anomaly into a mean anomaly (via the intermediate step of the eccentric anomaly)
        //For elliptical orbits, the output is between 0 and 2pi
        //For hyperbolic orbits, the output can be any number
        //NOTE: For a hyperbolic orbit, if a true anomaly is requested that does not exist (a true anomaly
        //past the true anomaly of the asymptote) then the output is infinite
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double GetMeanAnomalyAtTrueAnomaly(this Orbit o, double tanom) => o.GetMeanAnomaly(o.GetEccentricAnomaly(tanom));

        //Returns the next time at which a will cross its ascending node with b.
        //For elliptical orbits this is a time between UT and UT + a.period.
        //For hyperbolic orbits this can be any time, including a time in the past if
        //the ascending node is in the past.
        //NOTE: the time is infinite if a is a hyperbolic orbit and the "ascending node"
        //occurs at a true anomaly that a does not actually ever attain
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double TimeOfAscendingNode(this Orbit a, Orbit b, double ut) => a.TimeOfTrueAnomaly(a.AscendingNodeTrueAnomaly(b), ut);

        //Returns the next time at which a will cross its descending node with b.
        //For elliptical orbits this is a time between UT and UT + a.period.
        //For hyperbolic orbits this can be any time, including a time in the past if
        //the descending node is in the past.
        //NOTE: the time is infinite if a is a hyperbolic orbit and the "descending node"
        //occurs at a true anomaly that a does not actually ever attain
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double TimeOfDescendingNode(this Orbit a, Orbit b, double ut) => a.TimeOfTrueAnomaly(a.DescendingNodeTrueAnomaly(b), ut);

        //Returns the next time at which the orbiting object will cross the equator
        //moving northward, if o is east-moving, or southward, if o is west-moving.
        //For elliptical orbits this is a time between UT and UT + o.period.
        //For hyperbolic orbits this can by any time, including a time in the past if the
        //ascending node is in the past.
        //NOTE: the time is infinite if o is a hyperbolic orbit and the
        //"ascending node" occurs at a true anomaly that o does not actually ever attain.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double TimeOfAscendingNodeEquatorial(this Orbit o, double ut) =>
            o.TimeOfTrueAnomaly(o.AscendingNodeEquatorialTrueAnomaly(), ut);

        //Returns the next time at which the orbiting object will cross the equator
        //moving southward, if o is east-moving, or northward, if o is west-moving.
        //For elliptical orbits this is a time between UT and UT + o.period.
        //For hyperbolic orbits this can by any time, including a time in the past if the
        //descending node is in the past.
        //NOTE: the time is infinite if o is a hyperbolic orbit and the
        //"descending node" occurs at a true anomaly that o does not actually ever attain.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double TimeOfDescendingNodeEquatorial(this Orbit o, double ut) =>
            o.TimeOfTrueAnomaly(o.DescendingNodeEquatorialTrueAnomaly(), ut);

        //Computes the period of the phase angle between orbiting objects a and b.
        //This only really makes sense for approximately circular orbits in similar planes.
        //For noncircular orbits the time variation of the phase angle is only "quasiperiodic"
        //and for high eccentricities and/or large relative inclinations, the relative motion is
        //not really periodic at all.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double SynodicPeriod(this Orbit a, Orbit b)
        {
            int sign = Vector3d.Dot(a.OrbitNormal(), b.OrbitNormal()) > 0 ? 1 : -1; //detect relative retrograde motion
            return Abs(1.0 / (1.0 / a.period - sign * 1.0 / b.period)); //period after which the phase angle repeats
        }

        //Computes the phase angle between two orbiting objects.
        //This only makes sense if a.referenceBody == b.referenceBody.
        public static double PhaseAngle(this Orbit a, Orbit b, double ut)
        {
            Vector3d normalA = a.OrbitNormal();
            Vector3d posA = a.WorldBCIPositionAtUT(ut);
            var projectedB = Vector3d.Exclude(normalA, b.WorldBCIPositionAtUT(ut));
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double RelativeInclination(this Orbit a, Orbit b) => Abs(Vector3d.Angle(a.OrbitNormal(), b.OrbitNormal()));

        //Finds the next time at which the orbiting object will achieve a given radius
        //from the center of the primary.
        //If the given radius is impossible for this orbit, an ArgumentException is thrown.
        //For elliptical orbits this will be a time between UT and UT + period
        //For hyperbolic orbits this can be any time. If the given radius will be achieved
        //in the future then the next time at which that radius will be achieved will be returned.
        //If the given radius was only achieved in the past, then there are no guarantees
        //about which of the two times in the past will be returned.
        public static double NextTimeOfRadius(this Orbit o, double ut, double radius)
        {
            if (radius < o.PeR || (o.eccentricity < 1 && radius > o.ApR))
                throw new ArgumentException("OrbitExtensions.NextTimeOfRadius: given radius of " + radius + " is never achieved: o.PeR = " + o.PeR +
                    " and o.ApR = " + o.ApR);

            double trueAnomaly1 = o.TrueAnomalyAtRadius(radius);
            double trueAnomaly2 = 2 * PI - trueAnomaly1;
            double time1 = o.TimeOfTrueAnomaly(trueAnomaly1, ut);
            double time2 = o.TimeOfTrueAnomaly(trueAnomaly2, ut);
            if (time2 < time1 && time2 > ut) return time2;
            return time1;
        }

        public static Vector3d DeltaVToManeuverNodeCoordinates(this Orbit o, double ut, Vector3d dV) =>
            new Vector3d(Vector3d.Dot(o.RadialPlus(ut), dV),
                Vector3d.Dot(-o.NormalPlus(ut), dV),
                Vector3d.Dot(o.Prograde(ut), dV));

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

        public static string MuString(this Orbit o) =>
            "PeA:" + o.PeA + " ApA:" + o.ApA + " SMA:" + o.semiMajorAxis + " ECC:" + o.eccentricity + " INC:" + o.inclination + " LAN:" +
            o.LAN + " ArgP:" + o.argumentOfPeriapsis + " TA:" + o.trueAnomaly;
    }
}
