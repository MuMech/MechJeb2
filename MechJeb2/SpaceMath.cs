using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MuMech
{
    public static class SpaceMath
    {
        /// <summary>
        /// Single impulse transfer from an ellipitical, non-coplanar parking orbit to an arbitrary hyperbolic v-infinity target.
        ///
        /// Ocampo, C., & Saudemont, R. R. (2010). Initial Trajectory Model for a Multi-Maneuver Moon-to-Earth Abort Sequence.
        /// Journal of Guidance, Control, and Dynamics, 33(4), 1184–1194.
        /// </summary>
        ///
        /// <param name="mu">Gravitational parameter of central body.</param>
        /// <param name="r0">Reference position on the parking orbit.</param>
        /// <param name="v0">Reference velocity on the parking orbit.</param>
        /// <param name="v_inf">Target hyperbolic v-infinity vector.</param>
        /// <param name="vneg">Velocity on the parking orbit before the burn.</param>
        /// <param name="vpos">Velocity on the hyperboliic ejection orbit after the burn.</param>
        /// <param name="r">Position of the burn.</param>
        /// <param name="dt">Coasting time on the parking orbit from the reference to the burn.</param>
        ///
        public static void singleImpulseHyperbolicBurn(double mu, Vector3d r0, Vector3d v0, Vector3d v_inf,
                ref Vector3d vneg, ref Vector3d vpos, ref Vector3d r, ref double dt)
        {
            // angular momentum of the parking orbit
            Vector3d h0 = Vector3d.Cross(r0, v0);

            // semi major axis of parking orbit
            double a0 = 1.0 / (2.0 / r0.magnitude - v0.sqrMagnitude / mu);

            // sma of hyperbolic ejection orbit
            double af = - mu / v_inf.sqrMagnitude;

            // parking orbit angular momentum unit
            Vector3d h0_hat = h0/h0.magnitude;

            // eccentricity vector of the parking orbit
            Vector3d ecc = Vector3d.Cross(v0,h0)/mu - r0/r0.magnitude;

            // eccentricity of the parking orbit.
            double e0 = ecc.magnitude;

            // semilatus rectum of parking orbit
            double p0 = a0 * ( 1 - e0 * e0 );

            // parking orbit periapsis position unit vector
            Vector3d rp0_hat;
            if ( Math.Abs(e0) > 1e-14 )
                rp0_hat = ecc/e0;
            else
                rp0_hat = r0/r0.magnitude;

            // parking orbit periapsis velocity unit vector
            Vector3d vp0_hat = Vector3d.Cross(h0, rp0_hat);
            vp0_hat = vp0_hat/vp0_hat.magnitude;

            // direction of hyperbolic v-infinity vector
            Vector3d v_inf_hat = v_inf/v_inf.magnitude;

            // 3 cases for finding hf_hat
            double dotp = Vector3d.Dot(h0_hat, v_inf_hat);
            Vector3d hf_hat;
            if ( dotp == 0 )
            {
                // zero plane change case
                hf_hat = h0_hat;
            }
            else if ( Math.Abs(dotp) == 1 )
            {
                // 90 degree plane change case
                hf_hat = Vector3d.Cross(rp0_hat, v_inf_hat);
                hf_hat = hf_hat / hf_hat.magnitude;
            }
            else
            {
                // general case (also works for dotp = 0)
                hf_hat = Vector3d.Cross(v_inf_hat, Vector3d.Cross(h0_hat, v_inf_hat));
                hf_hat = hf_hat / hf_hat.magnitude;
            }

            // unit vector pointing at the position of the burn on the parking orbit
            Vector3d r1_hat = Vector3d.Cross(v_inf_hat, hf_hat);
            r1_hat = r1_hat/r1_hat.magnitude;

            // true anomaly of r1 on the parking orbit
            double nu_10 = Math.Sign(Vector3d.Dot(h0_hat, Vector3d.Cross(rp0_hat, r1_hat))) * Math.Acos(Vector3d.Dot(rp0_hat,r1_hat));

            // length of the position vector of the burn on the parking orbit
            double r1 = p0 / ( 1 + e0 * Math.Cos(nu_10) );

            // position of the burn
            r = r1 * r1_hat;

            // constant
            double k = - af / r1;

            // eccentricity of the hyperbolic ejection orbit
            double ef = Math.Sqrt(1 + 2*k*k + 2*k + Math.Sqrt(1 + 4*k))/(Math.Sqrt(2)*k);

            // semilatus rectum of hyperbolic ejection orbit
            double pf = af * ( 1 - ef*ef );

            // true anomaly of the v_inf on the hyperbolic ejection orbit
            double nu_inf = Math.Acos(-1/ef);

            // true anomaly of the burn on the hyperbolic ejection orbit
            double nu_1f = nu_inf - Math.PI/2;

            // turning angle of the hyperbolic orbit
            double delta = 2 * Math.Asin(1/ef);

            // incoming hyperbolic velocity unit vector
            Vector3d v_inf_minus_hat = Math.Cos(delta) * v_inf_hat + Math.Sin(delta) * Vector3d.Cross(v_inf_hat, hf_hat);

            // periapsis position and velocity vectors of the hyperbolic ejection orbit
            Vector3d rpf_hat = v_inf_minus_hat - v_inf_hat;
            rpf_hat = rpf_hat / rpf_hat.magnitude;
            Vector3d vpf_hat = v_inf_minus_hat + v_inf_hat;
            vpf_hat = vpf_hat / vpf_hat.magnitude;

            // compute the velocity on the hyperbola and the parking orbit
            vpos = Math.Sqrt(mu/pf) * ( -Math.Sin(nu_1f) * rpf_hat + ( ef + Math.Cos(nu_1f) ) * vpf_hat );
            vneg = Math.Sqrt(mu/p0) * ( -Math.Sin(nu_10) * rp0_hat + ( e0 + Math.Cos(nu_10) ) * vp0_hat );

            // compute nu of the reference position on the parking orbit
            Vector3d r0_hat = r0/r0.magnitude;
            double nu0 = Math.Sign(Vector3d.Dot(h0_hat, Vector3d.Cross(rp0_hat, r0_hat))) * Math.Acos(Vector3d.Dot(rp0_hat,r0_hat));

            // mean angular motion of the parking orbit
            double n = 1/Math.Sqrt( a0*a0*a0 / mu );

            // eccentric anomalies of reference position and r1 on the parking orbit
            double E0 = Math.Atan2(Math.Sqrt(1 - e0*e0) * Math.Sin(nu0), e0 + Math.Cos(nu0));
            double E1 = Math.Atan2(Math.Sqrt(1 - e0*e0) * Math.Sin(nu_10), e0 + Math.Cos(nu_10));

            // mean anomalies of reference position and r1 on the parking orbit
            double M0 = E0 - e0 * Math.Sin( E0 );
            double M1 = E1 - e0 * Math.Sin( E1 );

            // coast time on the parking orbit
            dt = ( M1 - M0 ) / n;
            if ( dt < 0 )
            {
                dt += 2 * Math.PI / n;
            }
        }
    }
}
