using System;
using UnityEngine;

namespace MuMech
{
    // The purpose of this module is to collect pure-math functions with minimal coupling to MechJeb and/or KSP.  Chunks of the OrbitalManeuverCalculator
    // class should probably be moved here, where that class should be more tightly coupled to the maneuver node planner.
    //
    public static class SpaceMath
    {
        /// <summary>
        /// Find the time to a target plane defined by the LAN and inc for a rocket on the ground.  Wrapper which handles picking
        /// the soonest of the northgoing and southgoing ground tracks.
        /// </summary>
        ///
        /// <param name="rotationPeriod">Rotation period of the central body (seconds).</param>
        /// <param name="latitude">Latitude of the launchite (degrees).</param>
        /// <param name="celestialLongitude">Celestial longitude of the current position of the launchsite.</param>
        /// <param name="LAN">Longitude of the Ascending Node of the target plane (degrees).</param>
        /// <param name="inc">Inclination of the target plane (degrees).</param>
        ///
        public static double MinimumTimeToPlane(double rotationPeriod, double latitude, double celestialLongitude, double LAN, double inc)
        {
            double one = TimeToPlane(rotationPeriod, latitude, celestialLongitude, LAN, inc);
            double two = TimeToPlane(rotationPeriod, latitude, celestialLongitude, LAN, -inc);
            return Math.Min(one, two);
        }

        /// <summary>
        /// Find the time to a target plane defined by the LAN and inc for a rocket on the ground.
        /// </summary>
        ///
        /// <param name="rotationPeriod">Rotation period of the central body (seconds).</param>
        /// <param name="latitude">Latitude of the launchite (degrees).</param>
        /// <param name="celestialLongitude">Celestial longitude of the current position of the launchsite.</param>
        /// <param name="LAN">Longitude of the Ascending Node of the target plane (degrees).</param>
        /// <param name="inc">Inclination of the target plane (degrees).</param>
        ///
        public static double TimeToPlane(double rotationPeriod, double latitude, double celestialLongitude, double LAN, double inc)
        {
            // alpha is the 90 degree angle between the line of longitude and the equator and omitted
            double beta = OrbitalManeuverCalculator.HeadingForInclination(inc, latitude) * UtilMath.Deg2Rad;
            double c = Math.Abs(latitude) * UtilMath.Deg2Rad; // Abs for south hemisphere launch sites
            // b is how many radians to the west of the launch site that the LAN is (east in south hemisphere)
            double b = Math.Atan2( 2 * Math.Sin(beta), Math.Cos(beta) / Math.Tan(c/2) + Math.Tan(c/2) * Math.Cos(beta) ); // napier's analogies
            // LAN if we launched now
            double LANnow = celestialLongitude - Math.Sign(latitude) * b * UtilMath.Rad2Deg;


            return MuUtils.ClampDegrees360( LAN - LANnow ) / 360 * rotationPeriod;
        }

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
                ref Vector3d vneg, ref Vector3d vpos, ref Vector3d r, ref double dt, bool debug = false)
        {
            double rot, dv;
            BrentFun f = delegate(double testrot, object ign) {
                double dt = 0;
                Vector3d vneg = new Vector3d();
                Vector3d vpos = new Vector3d();
                Vector3d r = new Vector3d();
                singleImpulseHyperbolicBurn(mu, r0, v0, v_inf, ref vneg, ref vpos, ref r, ref dt, (float)testrot, debug);
                return (vpos - vneg).magnitude;
            };
            Brent.Minimize(f, -30, 30, 1e-6, out rot, out dv, null);
            singleImpulseHyperbolicBurn(mu, r0, v0, v_inf, ref vneg, ref vpos, ref r, ref dt, (float)rot, debug);
        }

        /// <summary>
        /// This is the implementation function of the single impulse transfer from an elliptical, non-coplanar parking orbit.
        ///
        /// It could be called directly with e.g. rotation of zero to bypass the line search for the rotation which wil be nearly
        /// optimal in many cases, but fails in the kinds of nearly coplanar conditions which are common in KSP.
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
        /// <param name="rot">Rotation of hf_hat around v_inf_hat (or r1_hat around h0_hat) [degrees].</param>
        ///
        public static void singleImpulseHyperbolicBurn(double mu, Vector3d r0, Vector3d v0, Vector3d v_inf,
                ref Vector3d vneg, ref Vector3d vpos, ref Vector3d r, ref double dt, float rot, bool debug = false)
        {
            if (debug)
            {
                Debug.Log("[MechJeb] singleImpulseHyperbolicBurn mu = " + mu + " r0 = " + r0 + " v0 = " + v0 + " v_inf = " + v_inf);
            }

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
            Vector3d vp0_hat = Vector3d.Cross(h0, rp0_hat).normalized;

            // direction of hyperbolic v-infinity vector
            Vector3d v_inf_hat = v_inf.normalized;

            // 2 cases for finding hf_hat
            Vector3d hf_hat;
            if ( Math.Abs(Vector3d.Dot(h0_hat, v_inf_hat)) == 1 )
            {
                // 90 degree plane change case
                hf_hat = Vector3d.Cross(rp0_hat, v_inf_hat);
                hf_hat = hf_hat.normalized;
            }
            else
            {
                // general case
                hf_hat = Vector3d.Cross(v_inf_hat, Vector3d.Cross(h0_hat, v_inf_hat));
                hf_hat = hf_hat.normalized;
            }

            Vector3d r1_hat;

            if ( Math.Abs(Vector3d.Dot(h0_hat, v_inf_hat)) > 2.22044604925031e-16 )
            {
                // if the planes are not coincident, rotate hf_hat by applying rodriguez formula around v_inf_hat
                hf_hat = Quaternion.AngleAxis(rot, v_inf_hat) * hf_hat;
                // unit vector pointing at the position of the burn on the parking orbit
                r1_hat = Math.Sign(Vector3d.Dot(h0_hat, v_inf_hat)) * Vector3d.Cross(h0_hat, hf_hat).normalized;
            }
            else
            {
                // unit vector pointing at the position of the burn on the parking orbit
                r1_hat = Vector3d.Cross(v_inf_hat, hf_hat).normalized;
                // if the planes are coincident, rotate r1_hat by applying rodriguez formula around h0_hat
                r1_hat = Quaternion.AngleAxis(rot, h0_hat) * r1_hat;
            }

            // true anomaly of r1 on the parking orbit
            double nu_10 = Math.Sign(Vector3d.Dot(h0_hat, Vector3d.Cross(rp0_hat, r1_hat))) * Math.Acos(Vector3d.Dot(rp0_hat,r1_hat));

            // length of the position vector of the burn on the parking orbit
            double r1 = p0 / ( 1 + e0 * Math.Cos(nu_10) );

            // position of the burn
            r = r1 * r1_hat;

            // constant
            double k = - af / r1;

            // angle between v_inf and the r1 burn
            double delta_nu = Math.Acos(MuUtils.Clamp(Vector3d.Dot(r1_hat, v_inf_hat), -1, 1));

            // eccentricity of the hyperbolic ejection orbit
            double sindnu  = Math.Sin(delta_nu);
            double sin2dnu = sindnu * sindnu;
            double cosdnu  = Math.Cos(delta_nu);
            double ef = Math.Max(Math.Sqrt(sin2dnu + 2*k*k + 2*k*(1-cosdnu) + sindnu*Math.Sqrt(sin2dnu + 4*k*(1-cosdnu)))/(Math.Sqrt(2)*k), 1 + MuUtils.DBL_EPSILON);

            // semilatus rectum of hyperbolic ejection orbit
            double pf = af * ( 1 - ef*ef );

            // true anomaly of the v_inf on the hyperbolic ejection orbit
            double nu_inf = Math.Acos(-1/ef);

            // true anomaly of the burn on the hyperbolic ejection orbit
            double nu_1f = Math.Acos(MuUtils.Clamp(-1/ef * cosdnu + Math.Sqrt(ef*ef-1)/ef * sindnu, -1, 1));

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

            if (debug)
            {
                Debug.Log("[MechJeb] singleImpulseHyperbolicBurn vneg = " + vneg + " vpos = " + vpos + " r = " + r + " dt = " + dt);
            }
        }
    }
}
