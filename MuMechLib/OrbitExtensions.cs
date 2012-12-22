using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    //Todo: bring over the functions that are used to compute time from true anomaly. These
    //      are needed for the AN/DN predictions.
    public static class OrbitExtensions
    {
        public static Vector3d SwapYZ(Vector3d v)
        {
            return new Vector3d(v.x, v.z, v.y);
        }

        //
        // These "Swapped" functions translate preexisting Orbit class functions into world
        // space. For some reason, Orbit class functions seem to use a coordinate system
        // in which the Y and Z coordinates are swapped.
        //
        public static Vector3d SwappedOrbitalVelocityAtUT(this Orbit o, double UT)
        {
            return SwapYZ(o.getOrbitalVelocityAtUT(UT));
        }

        public static Vector3d SwappedRelativePositionAtUT(this Orbit o, double UT)
        {
            return SwapYZ(o.getRelativePositionAtUT(UT));
        }

        public static Vector3d SwappedAbsolutePositionAtUT(this Orbit o, double UT)
        {
            return o.referenceBody.position + o.SwappedAbsolutePositionAtUT(UT);
        }

        public static Vector3d SwappedOrbitNormal(this Orbit o)
        {
            return SwapYZ(o.GetOrbitNormal());
        }

        public static Vector3d Prograde(this Orbit o, double UT)
        {
            return o.SwappedOrbitalVelocityAtUT(UT).normalized;            
        }

        public static Vector3d Up(this Orbit o, double UT)
        {
            return o.SwappedRelativePositionAtUT(UT).normalized;
        }

        public static Vector3d RadialPlus(this Orbit o, double UT)
        {
            return Vector3d.Exclude(o.Prograde(UT), o.Up(UT)).normalized;
        }

        public static Vector3d NormalPlus(this Orbit o, double UT)
        {
            return Vector3d.Cross(o.RadialPlus(UT), o.Prograde(UT));
        }

        public static Vector3d Horizontal(this Orbit o, double UT)
        {
            return Vector3d.Exclude(o.Up(UT), o.Prograde(UT)).normalized;
        }

        public static Vector3d North(this Orbit o, double UT)
        {
            return Vector3d.Exclude(o.Up(UT), o.referenceBody.angularVelocity).normalized;
        }

        public static Vector3d East(this Orbit o, double UT)
        {
            return Vector3d.Cross(o.North(UT), o.Up(UT));
        }

        public static double Radius(this Orbit o, double UT)
        {
            return o.SwappedRelativePositionAtUT(UT).magnitude;
        }

        //returns a new Orbit object that represents the result of applying dV to o at UT
        public static Orbit PerturbedOrbit(this Orbit o, double UT, Vector3d dV)
        {
            //should these in fact be swapped?
            return MuUtils.OrbitFromStateVectors(o.SwappedRelativePositionAtUT(UT), o.SwappedOrbitalVelocityAtUT(UT) + dV, o.referenceBody, UT);
        }

        //mean motion is rate of increase of mean anomaly
        public static double MeanMotion(this Orbit o) 
        {
            return Math.Sqrt(o.referenceBody.gravParameter / Math.Abs(Math.Pow(o.semiMajorAxis, 3)));
        }

        public static double Separation(this Orbit a, Orbit b, double UT)
        {
            return (a.SwappedAbsolutePositionAtUT(UT) - b.SwappedAbsolutePositionAtUT(UT)).magnitude;
        }

        public static double NextClosestApproachTime(this Orbit a, Orbit b, double UT)
        {
            double closestApproachTime = UT;
            double closestApproachDistance = Double.MaxValue;
            double minTime = UT;
            double interval = a.period;
            if(a.eccentricity > 1) {
                interval = 100 / a.MeanMotion(); //this should be an interval of time that covers a large chunk of the hyperbolic arc
                MonoBehaviour.print("TimeOfNextClosestApproach: hyperbolic interval = " + interval);
            }
            double maxTime = UT + interval;
            int numDivisions = 20;

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
                        closestApproachTime = t;
                    }
                }
                minTime = MuUtils.Clamp(closestApproachTime - dt, UT, UT + interval);
                maxTime = MuUtils.Clamp(closestApproachTime + dt, UT, UT + interval);
            }

            return closestApproachTime;
        }

        public static double NextClosestApproachDistance(this Orbit a, Orbit b, double UT)
        {
            return a.Separation(b, a.NextClosestApproachTime(b, UT));
        }

        public static double MeanAnomalyAtUT(this Orbit o, double UT) 
        {
            double ret = o.meanAnomalyAtEpoch + o.MeanMotion() * (UT - o.epoch);
            if(o.eccentricity < 1) ret = MuUtils.ClampRadiansTwoPi(ret);
            return ret;
        }

        public static double NextPeriapsisTime(this Orbit o, double UT)
        {
            if (o.eccentricity < 1)
            {
                return UT + (2 * Math.PI - o.MeanAnomalyAtUT(UT)) / o.MeanMotion();
            }
            else
            {
                return UT  - o.MeanAnomalyAtUT(UT) / o.MeanMotion();
            }
        }

        public static double NextApoapsisTime(this Orbit o, double UT)
        {
            if (o.eccentricity < 1)
            {
                return UT + (Math.PI - MuUtils.ClampRadiansPi(o.MeanAnomalyAtUT(UT))) / o.MeanMotion();
            }
            else
            {
                return UT; //hyperbolic orbits don't have an apoapsis...
            }
        }

        public static double AscendingNodeTrueAnomaly(this Orbit a, Orbit b)
        {
            Vector3d normal = a.SwappedOrbitNormal();
            Vector3d targetNormal = b.SwappedOrbitNormal();
            Vector3d vectorToAN = Vector3d.Cross(normal, targetNormal);
            Vector3d vectorToPe = SwapYZ(a.eccVec);
            double angleFromPe = Vector3d.Angle(vectorToPe, vectorToAN);

            //If the AN is actually during the infalling part of the orbit then we need to add 180 to
            //angle from Pe to get the true anomaly. Test this by taking the the cross product of the
            //orbit normal and vector to the periapsis. This gives a vector that points to center of the 
            //outgoing side of the orbit. If vectorToAN is more than 90 degrees from this vector, it occurs
            //during the infalling part of the orbit.
            if (Math.Abs(Vector3d.Angle(vectorToAN, Vector3d.Cross(vectorToPe, normal))) < 90)
            {
                return angleFromPe;
            }
            else
            {
                return 360 - angleFromPe;
            }
        }

        public static double DescendingNodeTrueAnomaly(this Orbit a, Orbit b)
        {
            return MuUtils.ClampDegrees360(a.AscendingNodeTrueAnomaly(b) + 180);
        }


    }
}
