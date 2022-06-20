/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using System;
using System.Collections.Generic;
using MechJebLib.Maths.FunctionImpls;
using MechJebLib.Primitives;
using MechJebLib.Utils;
using static MechJebLib.Utils.Statics;

#nullable enable

namespace MechJebLib.Maths
{
    public static class Functions
    {
        public static double VmagFromVisViva(double mu, double sma, double r)
        {
            return Math.Sqrt(mu * (2 / r - 1 / sma));
        }

        public static double HmagFromKeplerian(double mu, double sma, double ecc)
        {
            return Math.Sqrt(mu * sma * (1 - ecc * ecc));
        }

        public static V3 HunitFromKeplerian(double inc, double lan)
        {
            return new V3(Math.Sin(lan) * Math.Sin(inc), -Math.Cos(lan) * Math.Sin(inc), Math.Cos(inc));
        }

        public static V3 HvecFromKeplerian(double mu, double sma, double ecc, double inc, double lan)
        {
            return HunitFromKeplerian(inc, lan) * HmagFromKeplerian(mu, sma, ecc);
        }

        public static V3 HvecFromFlightPathAngle(double r, double v, double gamma, double inc, double lan)
        {
            return HunitFromKeplerian(inc, lan) * HmagFromFlightPathAngle(r, v, gamma);
        }

        private static double HmagFromFlightPathAngle(double r, double v, double gamma)
        {
            return r * v * Math.Cos(gamma);
        }

        public static V3 EvecFromKeplerian(double ecc, double inc, double lan, double argP)
        {
            return new V3(Math.Cos(argP) * Math.Cos(lan) - Math.Cos(inc) * Math.Sin(argP) * Math.Sin(lan),
                Math.Cos(argP) * Math.Sin(lan) + Math.Cos(inc) * Math.Cos(lan) * Math.Sin(argP),
                Math.Sin(argP) * Math.Sin(inc)) * ecc;
        }

        public static double SmaFromApsides(double peR, double apR)
        {
            return (peR + apR) / 2;
        }

        public static double EccFromApsides(double peR, double apR)
        {
            return (apR - peR) / (apR + peR);
        }

        public static (double sma, double ecc) SmaEccFromApsides(double peR, double apR)
        {
            // remap nonsense to circular orbits
            if (apR > 0 && apR < peR)
                apR = peR;
            double sma = SmaFromApsides(peR, apR);
            double ecc = EccFromApsides(peR, apR);
            return (sma, ecc);
        }

        public static double FlightPathAngleFromAngularVelocity(double h, double r, double v)
        {
            return SafeAcos(h / (r * v));
        }

        public static (double vT, double gammaT) ConvertApsidesTargetToFPA(double peR, double apR, double attR, double mu)
        {
            if (attR < peR)
                attR = peR;
            if (apR > peR && attR > apR)
                attR = apR;

            (double smaT, double eccT) = SmaEccFromApsides(peR, apR);
            double h = HmagFromKeplerian(mu, smaT, eccT);
            double vT = VmagFromVisViva(mu, smaT, attR);
            double gammaT = FlightPathAngleFromAngularVelocity(h, attR, vT);

            return (vT, gammaT);
        }

        public static double EscapeVelocity(double mu, double r)
        {
            return Math.Sqrt(2 * mu / r);
        }

        public static double CircularVelocity(double mu, double r)
        {
            return Math.Sqrt(mu / r);
        }

        public static double PeriapsisFromKeplerian(double sma, double ecc)
        {
            return sma * (1 - ecc);
        }

        public static double ApoapsisFromKeplerian(double sma, double ecc, bool hyperbolic = true)
        {
            double apo = sma * (1 + ecc);
            return apo < 0 ? double.MaxValue : apo;
        }

        public static double PeriapsisFromStateVectors(double mu, V3 r, V3 v)
        {
            double sma, ecc;
            (sma, ecc) = SmaEccFromStateVectors(mu, r, v);
            return PeriapsisFromKeplerian(sma, ecc);
        }

        public static double ApoapsisFromStateVectors(double mu, V3 r, V3 v, bool hyperbolic = true)
        {
            double sma, ecc;
            (sma, ecc) = SmaEccFromStateVectors(mu, r, v);
            return ApoapsisFromKeplerian(sma, ecc, hyperbolic);
        }

        public static (double sma, double ecc) SmaEccFromStateVectors(double mu, V3 r, V3 v)
        {
            var h = V3.Cross(r, v);
            double sma = SmaFromStateVectors(mu, r, v);
            return (sma, Math.Sqrt(1 - V3.Dot(h, h) / (sma * mu)));
        }

        public static double EccFromStateVectors(double mu, V3 r, V3 v)
        {
            (double _, double ecc) = SmaEccFromStateVectors(mu, r, v);
            return ecc;
        }

        public static double SmaFromStateVectors(double mu, V3 r, V3 v)
        {
            return mu / (2.0 * mu / r.magnitude - V3.Dot(v, v));
        }

        public static double IncFromStateVectors(V3 r, V3 v)
        {
            V3 hhat = V3.Cross(r, v).normalized;
            return Math.Acos(hhat[2]);
        }

        public static double PeriodFromStateVectors(double mu, V3 r, V3 v)
        {
            double sma = SmaFromStateVectors(mu, r, v);
            if (sma < 0)
                throw new Exception("cannot find period of hyperbolic orbit, sma = " + sma);
            return TAU * Math.Sqrt(sma * sma * sma / mu);
        }

        public static double RadiusFromTrueAnomaly(double sma, double ecc, double tanom)
        {
            return sma * (1 - ecc * ecc) / (1 + ecc * Math.Cos(tanom));
        }

        public static double RadiusFromTrueAnomaly(double mu, V3 r, V3 v, double tanom)
        {
            (double sma, double ecc) = SmaEccFromStateVectors(mu, r, v);

            return RadiusFromTrueAnomaly(sma, ecc, tanom);
        }

        public static double TrueAnomalyFromRadius(double sma, double ecc, double radius)
        {
            return SafeAcos((sma * (1 - ecc * ecc) / radius - 1) / ecc);
        }

        public static double TrueAnomalyFromRadius(double mu, V3 r, V3 v, double radius)
        {
            (double sma, double ecc) = SmaEccFromStateVectors(mu, r, v);

            return TrueAnomalyFromRadius(sma, ecc, radius);
        }

        public static V3 RhatFromLatLng(double lat, double lng)
        {
            return new V3(Math.Cos(lat) * Math.Cos(lng), Math.Cos(lat) * Math.Sin(lng), Math.Sin(lat));
        }

        public static double PitchAngle(V3 v, V3 up)
        {
            return PI * 0.5 - V3.Angle(v, up);
        }

        public static double FlightPathAngle(V3 r, V3 v)
        {
            return SafeAsin(V3.Dot(r.normalized, v.normalized));
        }

        // r is the ECI reference point, v is the vector in ECI to be converted to ENU
        public static V3 ENUToECI(V3 pos, V3 vec)
        {
            double lat = LatitudeFromBCI(pos); // should be geodetic, but we don't care for now
            double lng = LongitudeFromBCI(pos);

            double slat = Math.Sin(lat);
            double slng = Math.Sin(lng);
            double clat = Math.Cos(lat);
            double clng = Math.Cos(lng);

            var m = new M3(
                -slng, -slat * clng, clat * clng,
                clng, -slat * slng, clat * slng,
                0, clat, slat
            );

            return m * vec;
        }

        public static V3 VelocityForHeading(V3 r, V3 v, double newHeading)
        {
            V3 vtemp = ECIToENU(r, v);
            double hmag = new V3(vtemp.x, vtemp.y).magnitude;
            vtemp[0] = hmag * Math.Sin(newHeading);
            vtemp[1] = hmag * Math.Cos(newHeading);
            return ENUToECI(r, vtemp);
        }

        public static V3 ENUHeadingForInclination(double inc, V3 r)
        {
            return ENUHeadingForInclination(inc, LatitudeFromBCI(r));
        }

        public static V3 ENUHeadingForInclination(double inc, double lat)
        {
            double angle = AngleForInclination(inc, lat);

            return new V3(Math.Cos(angle), Math.Sin(angle), 0);
        }

        public static double HeadingForInclination(double inc, V3 r)
        {
            return HeadingForInclination(inc, LatitudeFromBCI(r));
        }

        public static double HeadingForInclination(double inc, double lat)
        {
            double angle = AngleForInclination(inc, lat);

            return Clamp2Pi(Deg2Rad(90) - angle);
        }

        private static double AngleForInclination(double inc, double lat)
        {
            double cosAngle = Math.Cos(inc) / Math.Cos(lat);

            if (Math.Abs(cosAngle) > 1.0)
                // for impossible inclinations return due east or west
                return Math.Abs(ClampPi(inc)) < PI * 0.5 ? 0 : Deg2Rad(180);

            // angle is from east, with 90 degrees due north
            double angle = Math.Acos(cosAngle);

            // negative inclinations are conventionally south-going
            if (inc < 0) angle *= -1;

            return angle;
        }

        public static double LatitudeFromBCI(V3 r)
        {
            return Math.Asin(Clamp(r.z / r.magnitude, -1, 1));
        }

        public static double LongitudeFromBCI(V3 r)
        {
            return Math.Atan2(r.y, r.x);
        }

        // r is the ECI reference point, v is the vector in ENU to be converted to ECI
        public static V3 ECIToENU(V3 r, V3 v)
        {
            double lat = LatitudeFromBCI(r);
            double lng = LongitudeFromBCI(r);

            double slat = Math.Sin(lat);
            double slng = Math.Sin(lng);
            double clat = Math.Cos(lat);
            double clng = Math.Cos(lng);

            var m = new M3(
                -slng, clng, 0.0,
                -slat * clng, -slat * slng, clat,
                clat * clng, clat * slng, slat
            );

            return m * v;
        }

        public static V3 VelocityForInclination(V3 r, V3 v, double newInc)
        {
            V3 v0 = ECIToENU(r, v);
            double horizMag = new V3(v0.x, v0.y).magnitude;
            V3 vf = ENUHeadingForInclination(newInc, r) * horizMag;
            vf.z = v0.z;
            vf   = ENUToECI(r, vf);
            return vf;
        }

        public static V3 DeltaVToChangeInclination(V3 r, V3 v, double newInc)
        {
            return VelocityForInclination(r, v, newInc) - v;
        }

        public static V3 VelocityForFPA(V3 r, V3 v, double newFPA)
        {
            V3 v0 = ECIToENU(r, v);
            double vmag = v0.magnitude;
            V3 vf = new V3(v0.x, v0.y).normalized * Math.Cos(newFPA) * vmag;
            vf.z = Math.Sin(newFPA) * vmag;
            vf   = ENUToECI(r, vf);
            return vf;
        }

        public static V3 DeltaVToChangeFPA(V3 r, V3 v, double newFPA)
        {
            return VelocityForFPA(r, v, newFPA) - v;
        }

        public static V3 CircularVelocityFromHvec(double mu, V3 r, V3 h)
        {
            return V3.Cross(h.normalized, r).normalized * CircularVelocity(mu, r.magnitude);
        }

        public static (double eanom, double manom) AnomaliesFromTrue(double tanom, double ecc)
        {
            double eanom = EccentricAnomalyFromTrue(tanom, ecc);
            double manom = Clamp2Pi(eanom - ecc * Math.Sin(eanom));
            return (eanom, manom);
        }

        public static double EccentricAnomalyFromTrue(double tanom, double ecc)
        {
            return Clamp2Pi(Math.Atan2(Math.Sqrt(1 - ecc * ecc) * Math.Sin(tanom), ecc + Math.Cos(tanom)));
        }

        public static double MeanAnomalyFromTrue(double tanom, double ecc)
        {
            (double _, double manom) = AnomaliesFromTrue(tanom, ecc);
            return manom;
        }

        // r is the ECI reference point, v is the vector in ECI to be converted to pitch, heading angles
        public static (double pitch, double heading) ECIToPitchHeading(V3 r, V3 v)
        {
            V3 enu = ECIToENU(r, v).normalized;
            return (Math.Asin(enu.z), Clamp2Pi(Math.Atan2(enu.x, enu.y)));
        }

        /// <summary>
        ///     Find the time to a target plane defined by the LAN and inc for a rocket on the ground.  Wrapper which handles
        ///     picking the soonest of the northgoing and southgoing ground tracks.
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

        public static V3 CubicHermiteInterpolant(double x1, V3 y1, V3 yp1, double x2, V3 y2,
            V3 yp2, double x)
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

        public static double MeanMotion(double mu, double sma)
        {
            return Math.Sqrt(mu / (sma * sma * sma));
        }

        // FIXME: hyperbolic and circular orbits
        public static double TimeToNextApoapsis(double mu, V3 r, V3 v)
        {
            (double sma, double ecc, double _, double _, double _, double tanom) = KeplerianFromStateVectors(mu, r, v);

            double meanMotion = MeanMotion(mu, sma);

            double manom = MeanAnomalyFromTrue(tanom, ecc);

            return Clamp2Pi(PI - manom) / meanMotion;
        }

        // FIXME: hyperbolic and circular orbits
        public static double TimeToNextPeriapsis(double mu, V3 r, V3 v)
        {
            (double sma, double ecc, double _, double _, double _, double tanom) = KeplerianFromStateVectors(mu, r, v);

            double meanMotion = MeanMotion(mu, sma);

            double manom = MeanAnomalyFromTrue(tanom, ecc);

            return Clamp2Pi(-manom) / meanMotion;
        }

        public static double TimeToNextTrueAnomaly(double mu, V3 r, V3 v, double tanom2)
        {
            (double sma, double ecc, double _, double _, double _, double tanom1) = KeplerianFromStateVectors(mu, r, v);

            double meanMotion = MeanMotion(mu, sma);

            double manom1 = MeanAnomalyFromTrue(tanom1, ecc);
            double manom2 = MeanAnomalyFromTrue(tanom2, ecc);

            return Clamp2Pi(manom2 - manom1) / meanMotion;
        }

        public static double TimeToNextRadius(double mu, V3 r, V3 v, double radius)
        {
            double tanom1 = TrueAnomalyFromRadius(mu, r, v, radius);
            double tanom2 = TAU - tanom1;
            double time1 = TimeToNextTrueAnomaly(mu, r, v, tanom1);
            double time2 = TimeToNextTrueAnomaly(mu, r, v, tanom2);
            return time1 < time2 ? time1 : time2;
        }

        public static V3 DeltaVToCircularizeAfterTime(double mu, V3 r, V3 v, double dt)
        {
            Check.Finite(mu);
            Check.Finite(r);
            Check.Finite(v);
            Check.Finite(dt);
            Check.Positive(mu);
            Check.NonZero(r);

            Shepperd.Solve(mu, dt, r, v, out V3 r1, out V3 v1);
            var h = V3.Cross(r1, v1);
            return CircularVelocityFromHvec(mu, r, h) - v1;
        }

        public static V3 DeltaVToCircularizeAtPeriapsis(double mu, V3 r, V3 v)
        {
            Check.Finite(mu);
            Check.Finite(r);
            Check.Finite(v);
            Check.Positive(mu);
            Check.NonZero(r);

            double dt = TimeToNextPeriapsis(mu, r, v);

            Check.Finite(dt);

            return DeltaVToCircularizeAfterTime(mu, r, v, dt);
        }

        public static V3 DeltaVToCircularizeAtApoapsis(double mu, V3 r, V3 v)
        {
            Check.Finite(mu);
            Check.Finite(r);
            Check.Finite(v);
            Check.Positive(mu);
            Check.NonZero(r);

            double dt = TimeToNextApoapsis(mu, r, v);

            Check.Finite(dt);

            return DeltaVToCircularizeAfterTime(mu, r, v, dt);
        }

        /// <summary>
        ///     Returns the vector delta V required to be applied in the prograde (or retrograde) direction
        ///     to change the orbit Apoapsis to the desired value.
        /// </summary>
        /// <param name="mu">Gravitational parameter</param>
        /// <param name="r">Current radius</param>
        /// <param name="v">Current velocity</param>
        /// <param name="newApR">Desired apoapsis</param>
        /// <returns>Delta-V</returns>
        public static V3 DeltaVToChangeApoapsisPrograde(double mu, V3 r, V3 v, double newApR)
        {
            Check.Finite(v);
            Check.PositiveFinite(mu);
            Check.NonZeroFinite(r);
            Check.PositiveFinite(newApR);

            return RealDeltaVToChangeApoapsisPrograde.Run(mu, r, v, newApR);
        }

        public static (double sma, double ecc, double inc, double lan, double argp, double tanom) KeplerianFromStateVectors(double mu,
            V3 r, V3 v)
        {
            double rmag = r.magnitude;
            double vmag = v.magnitude;
            V3 rhat = r.normalized;
            var hv = V3.Cross(r, v);
            V3 hhat = hv.normalized;
            V3 vtmp = v / mu;
            V3 eccvec = V3.Cross(vtmp, hv) - rhat;

            double sma = 1.0 / (2.0 / rmag - vmag * vmag / mu);

            double d = 1.0 + hhat[2];
            double p = d == 0 ? 0 : hhat[0] / d;
            double q = d == 0 ? 0 : -hhat[1] / d;

            double const1 = 1.0 / (1.0 + p * p + q * q);

            var fhat = new V3(
                const1 * (1.0 - p * p + q * q),
                const1 * 2.0 * p * q,
                -const1 * 2.0 * p
            );

            var ghat = new V3(
                const1 * 2.0 * p * q,
                const1 * (1.0 + p * p - q * q),
                const1 * 2.0 * q
            );

            double h = V3.Dot(eccvec, ghat);
            double xk = V3.Dot(eccvec, fhat);
            double x1 = V3.Dot(r, fhat);
            double y1 = V3.Dot(r, ghat);
            double xlambdot = Math.Atan2(y1, x1);

            double ecc = Math.Sqrt(h * h + xk * xk);
            double inc = 2.0 * Math.Atan(Math.Sqrt(p * p + q * q));
            double lan = Clamp2Pi(inc > EPS ? Math.Atan2(p, q) : 0.0);
            double argp = Clamp2Pi(ecc > EPS ? Math.Atan2(h, xk) - lan : 0.0);
            double tanom = Clamp2Pi(xlambdot - lan - argp);

            return (sma, ecc, inc, argp, lan, tanom);
        }

        // Danby's method
        public static (double eanom, double tanom) AnomaliesFromMean(double manom, double ecc)
        {
            double xma = manom - TAU * Math.Truncate(manom / TAU);
            double eanom, tanom;

            if (ecc == 0)
            {
                eanom = tanom = xma;
                return (eanom, tanom);
            }

            if (ecc < 1) // elliptic initial guess
                eanom = xma + 0.85 * Math.Sign(Math.Sin(xma)) * ecc;
            else // hyperbolic initial guess
                eanom = Math.Log(2 * xma / ecc + 1.8);

            int n = 0;

            while (true)
            {
                double s, c, f, fp, fpp, fppp;

                if (ecc < 1)
                {
                    // elliptic orbit
                    s    = ecc * Math.Sin(eanom);
                    c    = ecc * Math.Cos(eanom);
                    f    = eanom - s - xma;
                    fp   = 1 - c;
                    fpp  = s;
                    fppp = c;
                }
                else
                {
                    // hyperbolic orbit
                    s    = ecc * Math.Sinh(eanom);
                    c    = ecc * Math.Cosh(eanom);
                    f    = s - eanom - xma;
                    fp   = c - 1;
                    fpp  = s;
                    fppp = c;
                }

                if (Math.Abs(f) <= EPS || ++n > 20)
                    break;

                // update eccentric anomaly
                double delta = -f / fp;
                double deltastar = -f / (fp + 0.5 * delta * fpp);
                double deltak = -f / (fp + 0.5 * deltastar * fpp + deltastar * deltastar * fppp / 6);
                eanom += deltak;
            }

            // compute true anomaly
            double sta, cta;

            if (ecc < 1)
            {
                // elliptic
                sta = Math.Sqrt(1 - ecc * ecc) * Math.Sin(eanom);
                cta = Math.Cos(eanom) - ecc;
            }
            else
            {
                // hyperbolic
                sta = Math.Sqrt(ecc * ecc - 1) * Math.Sinh(eanom);
                cta = ecc - Math.Cosh(eanom);
            }

            tanom = Math.Atan2(sta, cta);

            return (eanom, tanom);
        }
    }
}
