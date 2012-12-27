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
            return v.Reorder(132);
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
            return o.referenceBody.position + o.SwappedRelativePositionAtUT(UT);
        }

        //convention: as you look down along the orbit normal, the satellite revolves counterclockwise
        public static Vector3d SwappedOrbitNormal(this Orbit o)
        {
            return -SwapYZ(o.GetOrbitNormal()).normalized;
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
            return Vector3d.Exclude(o.Up(UT), (o.referenceBody.transform.up * (float)o.referenceBody.Radius) - o.SwappedRelativePositionAtUT(UT)).normalized;
        }

        public static Vector3d East(this Orbit o, double UT)
        {
            return Vector3d.Cross(o.Up(UT), o.North(UT)); //I think this is the opposite of what it should be, but it gives the right answer
        }

        public static double Radius(this Orbit o, double UT)
        {
            return o.SwappedRelativePositionAtUT(UT).magnitude;
        }

        //returns a new Orbit object that represents the result of applying dV to o at UT
        public static Orbit PerturbedOrbit(this Orbit o, double UT, Vector3d dV)
        {
            //should these in fact be swapped?
            return MuUtils.OrbitFromStateVectors(o.SwappedAbsolutePositionAtUT(UT), o.SwappedOrbitalVelocityAtUT(UT) + dV, o.referenceBody, UT);
        }

        //mean motion is rate of increase of the mean anomaly
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

        public static double UTAtMeanAnomaly(this Orbit o, double meanAnomaly, double UT)
        {
            if (double.IsNaN(meanAnomaly)) return double.NaN;
            double currentMeanAnomaly = o.MeanAnomalyAtUT(UT);
            double meanDifference = meanAnomaly - currentMeanAnomaly;
            if (o.eccentricity < 1) meanDifference = MuUtils.ClampRadiansTwoPi(meanDifference);
            return UT + meanDifference / o.MeanMotion();
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
            Vector3d vectorToAN = Vector3d.Cross(a.SwappedOrbitNormal(), b.SwappedOrbitNormal());
            return a.TrueAnomalyFromVector(vectorToAN);
        }

        //Converts a direction, specified by a Vector3d, into a true anomaly
        public static double TrueAnomalyFromVector(this Orbit o, Vector3d vec) 
        {
            Vector3d projected = Vector3d.Exclude(o.SwappedOrbitNormal(), vec);
            Vector3d vectorToPe = SwapYZ(o.eccVec);
            double angleFromPe = Math.Abs(Vector3d.Angle(vectorToPe, projected));

            //If the vector points to the infalling part of the orbit then we need to do 360 minus the
            //angle from Pe to get the true anomaly. Test this by taking the the cross product of the
            //orbit normal and vector to the periapsis. This gives a vector that points to center of the 
            //outgoing side of the orbit. If vectorToAN is more than 90 degrees from this vector, it occurs
            //during the infalling part of the orbit.
            if (Math.Abs(Vector3d.Angle(projected, Vector3d.Cross(o.SwappedOrbitNormal(), vectorToPe))) < 90)
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

        /// <summary>
        /// Takes a true anomaly(degrees from Periapsis), and computes Eccentric Anomaly at that target
        /// </summary>
        /// <returns>
        /// Returns Eccentric Anomaly (0-2pi). If the given trueAnomaly is never attained, returns NaN (can happen
        /// for abs(trueAnomaly) > 90 and o hyperbolic).
        /// </returns>
        /// <param name='o'>Orbit</param>">
        /// <param name='trueAnomaly'>
        /// True anomaly (0-360 for elliptical orbits, -something to +something for hyperbolic orbits)
        /// </param>
        /// 
        public static double GetEccentricAnomalyAtTrueAnomaly(this Orbit o, double trueAnomaly)
        {
            double e = o.eccentricity;
            trueAnomaly = trueAnomaly * (Math.PI / 180);
            
            if (e < 1) //elliptical orbits
            {
                double cosE = (e + Math.Cos(trueAnomaly)) / (1 + e * Math.Cos(trueAnomaly));
                double sinE = Math.Sqrt(1 - (cosE * cosE));
                if (trueAnomaly > Math.PI)
                {
                    sinE *= -1;
                }
                return MuUtils.ClampRadiansTwoPi(Math.Atan2(sinE, cosE));
            }
            else  //hyperbolic orbits
            {
                double coshE = (e + Math.Cos(trueAnomaly)) / (1 + e * Math.Cos(trueAnomaly));
                if (coshE < 1) return double.NaN; //this true anomaly is not attained on this hyperbolic orbit
                double E = MuUtils.Acosh(coshE);
                E *= Math.Sign(trueAnomaly);
                return E;
            }
        }

        /// <summary>
        /// You provide the eccentric anomaly, this method will bring the mean.
        /// </summary>
        /// <returns>
        /// Mean anomaly (0-2pi)
        /// </returns>
        /// <param name='E'>
        /// orbit.eccentricAnomaly (0-2pi)
        /// </param>
        public static double GetMeanAnomalyAtEccentricAnomaly(this Orbit o, double E)
        {
            if (double.IsNaN(E)) return double.NaN;
            double e = o.eccentricity;
            if (e < 1) //elliptical orbits
            {
                return MuUtils.ClampRadiansTwoPi(E - (e * Math.Sin(E)));
            }
            else //hyperbolic orbits
            {
                return (e * Math.Sinh(E)) - E;
            }
        }

        public static double GetMeanAnomalyAtTrueAnomaly(this Orbit o, double trueAnomaly)
        {
            return o.GetMeanAnomalyAtEccentricAnomaly(o.GetEccentricAnomalyAtTrueAnomaly(trueAnomaly));
        }

        //NOTE: this function can return NaN, if a is a hyperbolic orbit with an eccentricity
        //large enough that it never attains the given true anomaly
        public static double TimeOfTrueAnomaly(this Orbit o, double trueAnomaly, double UT)
        {
            return o.UTAtMeanAnomaly(o.GetMeanAnomalyAtEccentricAnomaly(o.GetEccentricAnomalyAtTrueAnomaly(trueAnomaly)), UT);
        }

        //NOTE: this function can return NaN, if a is a hyperbolic orbit and the "ascending node"
        //occurs at a true anomaly that a does not actually ever attain
        public static double TimeOfAscendingNode(this Orbit a, Orbit b, double UT)
        {
            return a.TimeOfTrueAnomaly(a.AscendingNodeTrueAnomaly(b), UT);
        }

        //NOTE: this function can return NaN, if a is a hyperbolic orbit and the "descending node"
        //occurs at a true anomaly that a does not actually ever attain
        public static double TimeOfDescendingNode(this Orbit a, Orbit b, double UT)
        {
            return a.TimeOfTrueAnomaly(a.DescendingNodeTrueAnomaly(b), UT);
        }

        public static double SynodicPeriod(this Orbit a, Orbit b)
        {
            return Math.Abs(1.0 / (1.0 / a.period - 1.0 / b.period)); //period after which the phase angle repeats
        }
    }
}
