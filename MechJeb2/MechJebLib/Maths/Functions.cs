/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using System;
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
        /// <param name="LAN">Longitude of the Ascending Node of the target plane (degrees).</param>
        /// <param name="inc">Inclination of the target plane (degrees).</param>
        public static double MinimumTimeToPlane(double rotationPeriod,double latitude,double celestialLongitude,double LAN,double inc)
        {
            double one = TimeToPlane(rotationPeriod,latitude,celestialLongitude,LAN,inc);
            double two = TimeToPlane(rotationPeriod,latitude,celestialLongitude,LAN,-inc);
            return Math.Min(one,two);
        }


        /// <summary>
        ///     Find the time to a target plane defined by the LAN and inc for a rocket on the ground.
        /// </summary>
        /// <param name="rotationPeriod">Rotation period of the central body (seconds).</param>
        /// <param name="latitude">Latitude of the launch site (degrees).</param>
        /// <param name="celestialLongitude">Celestial longitude of the current position of the launch site (degrees).</param>
        /// <param name="LAN">Longitude of the Ascending Node of the target plane (degrees).</param>
        /// <param name="inc">Inclination of the target plane (degrees).</param>
        public static double TimeToPlane(double rotationPeriod,double latitude,double celestialLongitude,double LAN,double inc)
        {
            latitude           = Deg2Rad(latitude);
            celestialLongitude = Deg2Rad(celestialLongitude);
            LAN                = Deg2Rad(LAN);
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

            double LANNow = celestialLongitude - angleEastOfAN;

            double LANDiff = LAN - LANNow;

            // handle planets that rotate backwards
            if (rotationPeriod < 0)
                LANDiff = -LANDiff;

            return Clamp2Pi(LANDiff) / TAU * Math.Abs(rotationPeriod);
        }
    }
}
