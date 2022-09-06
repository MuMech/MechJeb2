/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using System;
using System.Collections.Generic;
using static MechJebLib.Utils.Statics;

#nullable enable

namespace MechJebLib.Maths
{
    public static class Functions
    {
        /// <summary>
        ///     Find the time to a target plane defined by the LAN and inc for a rocket on the ground.  Wrapper which handles
        ///     picking
        ///     the soonest of the northgoing and southgoing ground tracks.
        /// </summary>
        /// <param name="rotationPeriod">Rotation period of the central body (seconds).</param>
        /// <param name="latitude">Latitude of the launch site (degrees).</param>
        /// <param name="celestialLongitude">Celestial longitude of the current position of the launch site.</param>
        /// <param name="lan">Longitude of the Ascending Node of the target plane (degrees).</param>
        /// <param name="inc">Inclination of the target plane (degrees).</param>
        public static (double time, double inclination) MinimumTimeToPlane(double rotationPeriod, double latitude, double celestialLongitude,
            double lan, double inc)
        {
            double north = TimeToPlane(rotationPeriod, latitude, celestialLongitude, lan, Math.Abs(inc));
            double south = TimeToPlane(rotationPeriod, latitude, celestialLongitude, lan, -Math.Abs(inc));
            return north < south ? (north, Math.Abs(inc)) : (south, -Math.Abs(inc));
        }

        /// <summary>
        ///     Find the time to a target plane defined by the LAN and inc for a rocket on the ground.
        /// </summary>
        /// <param name="rotationPeriod">Rotation period of the central body (seconds).</param>
        /// <param name="latitude">Latitude of the launch site (degrees).</param>
        /// <param name="celestialLongitude">Celestial longitude of the current position of the launch site (degrees).</param>
        /// <param name="lan">Longitude of the Ascending Node of the target plane (degrees).</param>
        /// <param name="inc">Inclination of the target plane (degrees).</param>
        public static double TimeToPlane(double rotationPeriod, double latitude, double celestialLongitude, double lan, double inc)
        {
            latitude           = Deg2Rad(latitude);
            celestialLongitude = Deg2Rad(celestialLongitude);
            lan                = Deg2Rad(lan);
            inc                = Deg2Rad(inc);

            // handle singularities at the poles where tan(lat) is infinite
            if (Math.Abs(Math.Abs(latitude) - PI / 2) < EPS)
                return 0;

            // Napier's rules for spherical trig
            // the clamped Asin produces correct results for abs(inc) < abs(lat)
            double angleEastOfAN = SafeAsin(Math.Tan(latitude) / Math.Tan(Math.Abs(inc)));

            // handle south going trajectories (and the other two quadrants that Asin doesn't cover).
            // if you are launching to the north your AN is always going to be [-90,90] relative to
            // the zero of the launch site.  or facing the launch site your AN is always going to be
            // in "front" of the planet.  but launching south the AN is [90,270] and the AN is always
            // "behind" the planet.
            if (inc < 0)
                angleEastOfAN = PI - angleEastOfAN;

            double lanNow = celestialLongitude - angleEastOfAN;

            double lanDiff = lan - lanNow;

            // handle planets that rotate backwards
            if (rotationPeriod < 0)
                lanDiff = -lanDiff;

            return Clamp2Pi(lanDiff) / TAU * Math.Abs(rotationPeriod);
        }

        public static void CubicHermiteInterpolant(double x1, IList<double> y1, IList<double> yp1, double x2, IList<double> y2,
            IList<double> yp2, double x, int n, IList<double> y)
        {
            double t = (x - x1) / (x2 - x1);
            double t2 = t * t;
            double t3 = t2 * t;
            double h00 = 2 * t3 - 3 * t2 + 1;
            double h10 = t3 - 2 * t2 + t;
            double h01 = -2 * t3 + 3 * t2;
            double h11 = t3 - t2;
            for (int i = 0; i < n; i++)
                y[i] = h00 * y1[i] + h10 * (x2 - x1) * yp1[i] + h01 * y2[i] + h11 * (x2 - x1) * yp2[i];
        }

        public static double CubicHermiteInterpolant(double x1, double y1, double yp1, double x2, double y2,
            double yp2, double x)
        {
            double t = (x - x1) / (x2 - x1);
            double t2 = t * t;
            double t3 = t2 * t;
            double h00 = 2 * t3 - 3 * t2 + 1;
            double h10 = t3 - 2 * t2 + t;
            double h01 = -2 * t3 + 3 * t2;
            double h11 = t3 - t2;

            return h00 * y1 + h10 * (x2 - x1) * yp1 + h01 * y2 + h11 * (x2 - x1) * yp2;
        }

        public static Vector3d CubicHermiteInterpolant(double x1, Vector3d y1, Vector3d yp1, double x2, Vector3d y2,
            Vector3d yp2, double x)
        {
            double t = (x - x1) / (x2 - x1);
            double t2 = t * t;
            double t3 = t2 * t;
            double h00 = 2 * t3 - 3 * t2 + 1;
            double h10 = t3 - 2 * t2 + t;
            double h01 = -2 * t3 + 3 * t2;
            double h11 = t3 - t2;

            return h00 * y1 + h10 * (x2 - x1) * yp1 + h01 * y2 + h11 * (x2 - x1) * yp2;
        }
    }
}
