/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using System;
using MechJebLib.Core.FunctionImpls;
using MechJebLib.Core.Functions;
using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;

#nullable enable

namespace MechJebLib.Core
{
    public static class Maths
    {
        public static double VmagFromVisViva(double mu, double sma, double r)
        {
            return Math.Sqrt(mu * (2 / r - 1 / sma));
        }

        public static double HmagFromKeplerian(double mu, double sma, double ecc)
        {
            return Math.Sqrt(mu * sma * (1 - ecc * ecc));
        }

        // FIXME: busted with hyperbolic and NANs.
        public static double HmagFromApsides(double mu, double peR, double apR)
        {
            (double sma, double ecc) = SmaEccFromApsides(peR, apR);
            return HmagFromKeplerian(mu, sma, ecc);
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
            if (apo >= 0)
                return apo;
            return hyperbolic ? apo : double.MaxValue;
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

        public static double EccFromStateVectors(double mu, V3 r, V3 v)
        {
            V3 eccvec = V3.Cross(v / mu, V3.Cross(r, v)) - r.normalized;
            return eccvec.magnitude;
        }

        public static (double sma, double ecc) SmaEccFromStateVectors(double mu, V3 r, V3 v)
        {
            var h = V3.Cross(r, v);
            double sma = SmaFromStateVectors(mu, r, v);
            return (sma, Math.Sqrt(Math.Max(1 - h.sqrMagnitude / (sma * mu), 0)));
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

        public static double RadiusFromTrueAnomaly(double sma, double ecc, double nu)
        {
            return sma * (1 - ecc * ecc) / (1 + ecc * Math.Cos(nu));
        }

        public static double RadiusFromTrueAnomaly(double mu, V3 r, V3 v, double nu)
        {
            (double sma, double ecc) = SmaEccFromStateVectors(mu, r, v);

            return RadiusFromTrueAnomaly(sma, ecc, nu);
        }

        public static double TrueAnomalyFromRadius(double sma, double ecc, double radius)
        {
            double l = sma * (1 - ecc * ecc);
            return SafeAcos((l / radius - 1) / ecc);
        }

        public static double TrueAnomalyFromRadius(double mu, V3 r, V3 v, double radius)
        {
            (double sma, double ecc) = SmaEccFromStateVectors(mu, r, v);

            return TrueAnomalyFromRadius(sma, ecc, radius);
        }

        /// <summary>
        ///     True Anomaly from the Eccentric Anomaly.
        /// </summary>
        /// <param name="mu">Gravitational parameter</param>
        /// <param name="ecc">Eccentricity</param>
        /// <param name="eanom">Eccentric Anomaly</param>
        /// <returns>True Anomaly</returns>
        public static double TrueAnomalyFromEccentricAnomaly(double ecc, double eanom)
        {
            if (ecc < 1)
                return Clamp2Pi(2.0 * Math.Atan(Math.Sqrt((1 + ecc) / (1 - ecc)) * Math.Tan(eanom / 2.0)));
            if (ecc > 1)
                return Clamp2Pi(2.0 * Math.Atan(Math.Sqrt((ecc + 1) / (ecc - 1)) * Math.Tanh(eanom / 2.0)));

            return Clamp2Pi(2 * Math.Atan(eanom));
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

        public static V3 VelocityForFPA(V3 r, V3 v, double newFPA)
        {
            V3 v0 = ECIToENU(r, v);
            double vmag = v0.magnitude;
            V3 vf = new V3(v0.x, v0.y).normalized * Math.Cos(newFPA) * vmag;
            vf.z = Math.Sin(newFPA) * vmag;
            vf   = ENUToECI(r, vf);
            return vf;
        }

        public static V3 CircularVelocityFromHvec(double mu, V3 r, V3 h)
        {
            return V3.Cross(h, r).normalized * CircularVelocity(mu, r.magnitude);
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

        public static double MeanMotion(double mu, double sma)
        {
            return Math.Sqrt(Math.Abs(mu / (sma * sma * sma)));
        }

        // FIXME: hyperbolic and circular orbits
        public static double TimeToNextApoapsis(double mu, V3 r, V3 v)
        {
            (double sma, double ecc, _, _, _, double nu, _) = KeplerianFromStateVectors(mu, r, v);

            double meanMotion = MeanMotion(mu, sma);

            double manom = Angles.MFromNu(nu, ecc);

            return Clamp2Pi(PI - manom) / meanMotion;
        }

        public static double TimeToNextPeriapsis(double mu, V3 r, V3 v)
        {
            (double sma, double ecc, _, _, _, double nu, _) = KeplerianFromStateVectors(mu, r, v);

            double meanMotion = MeanMotion(mu, sma);

            double manom = Angles.MFromNu(nu, ecc);

            return Clamp2Pi(-manom) / meanMotion;
        }

        public static double TimeToNextTrueAnomaly(double mu, V3 r, V3 v, double nu2)
        {
            (double sma, double ecc, _, _, _, double nu1, _) = KeplerianFromStateVectors(mu, r, v);

            double meanMotion = MeanMotion(mu, sma);

            double manom1 = Angles.MFromNu(nu1, ecc);
            double manom2 = Angles.MFromNu(nu2, ecc);

            if (ecc < 1)
                return Clamp2Pi(manom2 - manom1) / meanMotion;

            return (manom2 - manom1) / meanMotion;
        }

        public static double TimeToNextRadius(double mu, V3 r, V3 v, double radius)
        {
            double nu1 = TrueAnomalyFromRadius(mu, r, v, radius);
            double nu2 = -nu1;
            double time1 = TimeToNextTrueAnomaly(mu, r, v, nu1);
            double time2 = TimeToNextTrueAnomaly(mu, r, v, nu2);
            if (time1 >= 0 && time2 >= 0)
                return Math.Min(time1, time2);
            if (time1 < 0 && time2 < 0)
                return Math.Max(time1, time2);
            return time1 >= 0 ? time1 : time2;
        }

        /// <summary>
        ///     Kepler's Equation for time since periapsis from the Eccentric Anomaly.
        /// </summary>
        /// <param name="mu">Gravitational parameter</param>
        /// <param name="sma">Semimajor axis</param>
        /// <param name="ecc">Eccentricity</param>
        /// <param name="eanom">Eccentric Anomaly</param>
        /// <returns>Time of flight</returns>
        public static double TimeSincePeriapsisFromEccentricAnomaly(double mu, double sma, double ecc, double eanom)
        {
            double k = Math.Sqrt(Math.Abs(mu / (sma * sma * sma)));
            if (ecc < 1)
                return (eanom - ecc * Math.Sin(eanom)) / k;
            if (ecc > 1)
                return (ecc * Math.Sinh(eanom) - eanom) / k;
            return Math.Sqrt(2) * (eanom + eanom * eanom * eanom / 3.0) / k;
        }

        public static Q3 PerifocalToECIMatrix(double inc, double argp, double lan)
        {
            return Q3.AngleAxis(lan, V3.zaxis) * Q3.AngleAxis(inc, V3.xaxis) * Q3.AngleAxis(argp, V3.zaxis);
        }

        public static (V3 p, V3 q, Q3 rot) PerifocalFromStateVectors(double mu, V3 r, V3 v)
        {
            (_, double ecc, double inc, double lan, double argp, double nu, double l) = KeplerianFromStateVectors(mu, r, v);
            Q3 rot = PerifocalToECIMatrix(inc, argp, lan);
            (V3 p, V3 q) = PerifocalFromElements(mu, l, ecc, nu);

            return (p, q, rot);
        }

        public static (V3 p, V3 q) PerifocalFromElements(double mu, double l, double ecc, double nu)
        {
            double cnu = Math.Cos(nu);
            double snu = Math.Sin(nu);

            var one = new V3(cnu, snu, 0);
            var two = new V3(-snu, ecc + cnu, 0);

            return (one * l / (1 + ecc * cnu), two * Math.Sqrt(mu / l));
        }

        public static (V3 r, V3 v) StateVectorsFromKeplerian(double mu, double l, double ecc, double inc, double lan, double argp, double nu)
        {
            (V3 p, V3 q) = PerifocalFromElements(mu, l, ecc, nu);
            Q3 rot = PerifocalToECIMatrix(inc, argp, lan);

            return (rot * p, rot * q);
        }

        public static (double sma, double ecc, double inc, double lan, double argp, double nu, double l) KeplerianFromStateVectors(double mu,
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
            double l = hv.sqrMagnitude / mu;

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
            double nu = Clamp2Pi(xlambdot - lan - argp);

            return (sma, ecc, inc, lan, argp, nu, l);
        }

        // Danby's method
        public static (double eanom, double nu) AnomaliesFromMean(double manom, double ecc)
        {
            double xma = manom - TAU * Math.Truncate(manom / TAU);
            double eanom, nu;

            if (ecc == 0)
            {
                eanom = nu = xma;
                return (eanom, nu);
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

            nu = Math.Atan2(sta, cta);

            return (eanom, nu);
        }

        public static (V3 vNeg, V3 vPos, V3 r, double dt) SingleImpulseHyperbolicBurn(double mu, V3 r0, V3 v0, V3 vInf, bool debug = false)
        {
            return RealSingleImpulseHyperbolicBurn.Run(mu, r0, v0, vInf, debug);
        }
    }
}
