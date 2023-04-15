/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Copyright Sebastien Gaggini (sebastien.gaggini@gmail.com)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using System;
using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;

#nullable enable

namespace MechJebLib.Core.FunctionImpls
{
    public static class RealSingleImpulseHyperbolicBurn
    {
        /// <summary>
        ///     Single impulse transfer from an ellipitical, non-coplanar parking orbit to an arbitrary hyperbolic v-infinity
        ///     target.
        ///     Ocampo, C., & Saudemont, R. R. (2010). Initial Trajectory Model for a Multi-Maneuver Moon-to-Earth Abort Sequence.
        ///     Journal of Guidance, Control, and Dynamics, 33(4), 1184–1194.
        /// </summary>
        /// <param name="mu">Gravitational parameter of central body.</param>
        /// <param name="r0">Reference position on the parking orbit.</param>
        /// <param name="v0">Reference velocity on the parking orbit.</param>
        /// <param name="vInf">Target hyperbolic v-infinity vector.</param>
        /// <param name="vNeg">Velocity on the parking orbit before the burn.</param>
        /// <param name="vPos">Velocity on the hyperboliic ejection orbit after the burn.</param>
        /// <param name="r">Position of the burn.</param>
        /// <param name="dt">Coasting time on the parking orbit from the reference to the burn.</param>
        public static ( V3 vNeg, V3 vPos, V3 r, double dt) Run(double mu, V3 r0, V3 v0, V3 vInf, bool debug = false)
        {
            double Func(double testrot, object? ign)
            {
                (V3 vneg, V3 vpos, _, _) = Run2(mu, r0, v0, vInf, (float)testrot, debug);
                return (vpos - vneg).magnitude;
            }

            (double rot, _) = BrentMin.Solve(Func, -30, 30, null, 1e-6);
            return Run2(mu, r0, v0, vInf, (float)rot, debug);
        }

        /// <summary>
        ///     This is the implementation function of the single impulse transfer from an elliptical, non-coplanar parking orbit.
        ///     It could be called directly with e.g. rotation of zero to bypass the line search for the rotation which wil be
        ///     nearly
        ///     optimal in many cases, but fails in the kinds of nearly coplanar conditions which are common in KSP.
        /// </summary>
        /// <param name="mu">Gravitational parameter of central body.</param>
        /// <param name="r0">Reference position on the parking orbit (right handed).</param>
        /// <param name="v0">Reference velocity on the parking orbit (right handed).</param>
        /// <param name="vInf">Target hyperbolic v-infinity vector (right handed).</param>
        /// <param name="vNeg">Velocity on the parking orbit before the burn (right handed).</param>
        /// <param name="vPos">Velocity on the hyperboliic ejection orbit after the burn (right handed).</param>
        /// <param name="r">Position of the burn. (right handed)</param>
        /// <param name="dt">Coasting time on the parking orbit from the reference to the burn.</param>
        /// <param name="rot">Rotation of hf_hat around v_inf_hat. or r1_hat around h0_hat (degrees, right handed).</param>
        /// <param name="debug">Flag to debug log the parameters this function is called with</param>
        public static ( V3 vNeg, V3 vPos, V3 r, double dt) Run2(double mu, V3 r0, V3 v0, V3 vInf, float rot, bool debug = false)
        {
            if (debug)
            {
                Log("[MechJeb] singleImpulseHyperbolicBurn mu = " + mu + " r0 = " + r0 + " v0 = " + v0 + " vInf = " + vInf + " rot = " + rot);
            }

            // angular momentum of the parking orbit
            var h0 = V3.Cross(r0, v0);

            // semi major axis of parking orbit
            double a0 = 1.0 / (2.0 / r0.magnitude - v0.sqrMagnitude / mu);

            // sma of hyperbolic ejection orbit
            double af = -mu / vInf.sqrMagnitude;

            // parking orbit angular momentum unit
            V3 h0Hat = h0 / h0.magnitude;

            // eccentricity vector of the parking orbit
            V3 ecc = V3.Cross(v0, h0) / mu - r0 / r0.magnitude;

            // eccentricity of the parking orbit.
            double ecc0 = ecc.magnitude;

            if (ecc0 >= 1)
                throw new Exception("SingleImpulseHyperbolicBurn does not work with a hyperbolic initial orbit");

            // semilatus rectum of parking orbit
            double p0 = a0 * (1 - ecc0 * ecc0);

            // parking orbit periapsis position unit vector
            V3 rp0Hat;
            if (Math.Abs(ecc0) > 1e-14)
                rp0Hat = ecc / ecc0;
            else
                rp0Hat = r0 / r0.magnitude;

            if (debug)
            {
                Log("rp0hat: " + rp0Hat + " e0: " + Math.Abs(ecc0));
            }

            // parking orbit periapsis velocity unit vector
            V3 vp0Hat = V3.Cross(h0, rp0Hat).normalized;

            // direction of hyperbolic v-infinity vector
            V3 vInfHat = vInf.normalized;

            // 2 cases for finding hf_hat
            V3 hfHat;
            if (Math.Abs(V3.Dot(h0Hat, vInfHat)) == 1)
            {
                // 90 degree plane change case
                hfHat = V3.Cross(rp0Hat, vInfHat);
                hfHat = hfHat.normalized;
            }
            else
            {
                // general case
                hfHat = V3.Cross(vInfHat, V3.Cross(h0Hat, vInfHat));
                hfHat = hfHat.normalized;
            }

            V3 r1Hat;

            if (Math.Abs(V3.Dot(h0Hat, vInfHat)) > 2.22044604925031e-16)
            {
                // if the planes are not coincident, rotate hf_hat by applying rodriguez formula around v_inf_hat
                hfHat = Q3.AngleAxis(-rot, vInfHat) * hfHat;
                // unit vector pointing at the position of the burn on the parking orbit
                r1Hat = Math.Sign(V3.Dot(h0Hat, vInfHat)) * V3.Cross(h0Hat, hfHat).normalized;
            }
            else
            {
                // unit vector pointing at the position of the burn on the parking orbit
                r1Hat = V3.Cross(vInfHat, hfHat).normalized;
                // if the planes are coincident, rotate r1_hat by applying rodriguez formula around h0_hat
                r1Hat = Q3.AngleAxis(-rot, h0Hat) * r1Hat;
            }

            // true anomaly of r1 on the parking orbit
            double nu10 = Math.Sign(V3.Dot(h0Hat, V3.Cross(rp0Hat, r1Hat))) * Math.Acos(V3.Dot(rp0Hat, r1Hat));

            // length of the position vector of the burn on the parking orbit
            double r1 = p0 / (1 + ecc0 * Math.Cos(nu10));

            // position of the burn
            V3 r = r1 * r1Hat;

            if (debug)
            {
                Log("position of burn: " + r);
            }

            // constant
            double k = -af / r1;

            // angle between vInf and the r1 burn
            double deltaNu = SafeAcos(V3.Dot(r1Hat, vInfHat));

            // eccentricity of the hyperbolic ejection orbit
            double sindnu = Math.Sin(deltaNu);
            double sin2dnu = sindnu * sindnu;
            double cosdnu = Math.Cos(deltaNu);
            double ef = Math.Max(
                Math.Sqrt(sin2dnu + 2 * k * k + 2 * k * (1 - cosdnu) + sindnu * Math.Sqrt(sin2dnu + 4 * k * (1 - cosdnu))) / (Math.Sqrt(2) * k),
                1 + EPS);

            // semilatus rectum of hyperbolic ejection orbit
            double pf = af * (1 - ef * ef);

            // true anomaly of the vInf on the hyperbolic ejection orbit
            double nuInf = Math.Acos(-1 / ef);

            // true anomaly of the burn on the hyperbolic ejection orbit
            double nu1F = SafeAcos(-1 / ef * cosdnu + Math.Sqrt(ef * ef - 1) / ef * sindnu);

            // turning angle of the hyperbolic orbit
            double delta = 2 * Math.Asin(1 / ef);

            // incoming hyperbolic velocity unit vector
            V3 vInfMinusHat = Math.Cos(delta) * vInfHat + Math.Sin(delta) * V3.Cross(vInfHat, hfHat);

            // periapsis position and velocity vectors of the hyperbolic ejection orbit
            V3 rpfHat = vInfMinusHat - vInfHat;
            rpfHat /= rpfHat.magnitude;
            V3 vpfHat = vInfMinusHat + vInfHat;
            vpfHat /= vpfHat.magnitude;

            // compute the velocity on the hyperbola and the parking orbit
            V3 vPos = Math.Sqrt(mu / pf) * (-Math.Sin(nu1F) * rpfHat + (ef + Math.Cos(nu1F)) * vpfHat);
            V3 vNeg = Math.Sqrt(mu / p0) * (-Math.Sin(nu10) * rp0Hat + (ecc0 + Math.Cos(nu10)) * vp0Hat);

            // compute nu of the reference position on the parking orbit
            V3 r0Hat = r0 / r0.magnitude;
            double nu0 = Math.Sign(V3.Dot(h0Hat, V3.Cross(rp0Hat, r0Hat))) * Math.Acos(V3.Dot(rp0Hat, r0Hat));

            // mean angular motion of the parking orbit (rad/time)
            double n = 1 / Math.Sqrt(a0 * a0 * a0 / mu);

            // eccentric anomalies of reference position and r1 on the parking orbit (rad)
            double e0 = Math.Atan2(Math.Sqrt(1 - ecc0 * ecc0) * Math.Sin(nu0), ecc0 + Math.Cos(nu0));
            double e1 = Math.Atan2(Math.Sqrt(1 - ecc0 * ecc0) * Math.Sin(nu10), ecc0 + Math.Cos(nu10));

            // mean anomalies of reference position and r1 on the parking orbit (rad)
            double m0 = e0 - ecc0 * Math.Sin(e0);
            double m1 = e1 - ecc0 * Math.Sin(e1);

            if (debug)
            {
                Log("mean motion: " + n);
                Log("true anomaly of ref position: " + nu0 + " true anomaly of burn: " + nu10);
                Log("eccentric anomaly of ref position: " + e0 + " eccentric anomaly of burn: " + e1);
                Log("mean anomaly of ref posotion: " + m0 + " mean anomaly of burn: " + m1);
            }

            // coast time on the parking orbit
            double dt = (m1 - m0) / n;
            if (dt < 0)
            {
                dt += 2 * Math.PI / n;
            }

            if (debug)
            {
                Log("[MechJeb] singleImpulseHyperbolicBurn vNeg = " + vNeg + " vPos = " + vPos + " r = " + r + " dt = " + dt);
            }

            return (vNeg, vPos, r, dt);
        }
    }
}
