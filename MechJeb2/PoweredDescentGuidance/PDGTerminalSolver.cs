// PdgTerminalSolver.cs
// Pure-math terminal descent guidance laws.
//
// Two independent laws are provided:
//   1. Apollo zero-jerk guidance (zero-order hold on jerk at final time).
//   2. Gravity-turn guidance (Han, Jo & Ho, arXiv:2409.01465).
//
// Both methods are static and free of KSP runtime state. They receive all
// required quantities as explicit parameters and return typed output structs.

using System;
using UnityEngine;

namespace MuMech.Landing
{
    internal static class PdgTerminalSolver
    {
        // =========================================================================
        // Apollo zero-jerk terminal law
        // =========================================================================

        /// <summary>
        /// Computes an Apollo-style zero-jerk guidance command.
        /// The law minimises jerk at the final time by constraining position, velocity,
        /// and acceleration, then Newton-iterates on time-to-go.
        /// </summary>
        /// <param name="pos">Vehicle position, body-centred inertial [m].</param>
        /// <param name="vel">Vehicle velocity in the world frame [m/s].</param>
        /// <param name="targetPos">Target position, body-centred inertial [m].</param>
        /// <param name="xHat">Solver-frame downrange unit vector.</param>
        /// <param name="yHat">Solver-frame radial (up) unit vector.</param>
        /// <param name="zHat">Solver-frame crossrange unit vector.</param>
        /// <param name="mu">Gravitational parameter of the central body [m³/s²].</param>
        /// <param name="mass">Vehicle mass [kg].</param>
        /// <param name="availThrust">Maximum available thrust [N].</param>
        /// <param name="vfWorld">Desired final velocity in the world frame [m/s]; defaults to zero.</param>
        /// <param name="aFinalWorld">Desired final acceleration in the world frame [m/s²]; defaults to zero.</param>
        /// <param name="jFinalWorld">Desired final jerk in the world frame [m/s³]; defaults to zero.</param>
        /// <param name="tgoGuess">Initial time-to-go guess [s].</param>
        /// <param name="tgoAxis">Index (0=x, 1=y, 2=z) of the axis used for the tgo Newton solve.</param>
        /// <param name="maxIter">Maximum Newton iterations.</param>
        /// <param name="tol">Convergence tolerance on the tgo update [s].</param>
        public static ApolloGuidanceOutput ComputeApolloZeroJerkCommand(
            Vector3d pos, Vector3d vel, Vector3d targetPos,
            Vector3d xHat, Vector3d yHat, Vector3d zHat,
            double mu, double mass, double availThrust,
            Vector3d? vfWorld      = null,
            Vector3d? aFinalWorld  = null,
            Vector3d? jFinalWorld  = null,
            double tgoGuess = 10.0, int tgoAxis = 1,
            int maxIter = 15, double tol = 1e-3)
        {
            ApolloGuidanceOutput outp = new ApolloGuidanceOutput
            {
                valid         = false, reason        = "Unknown", tgo = double.NaN,
                accelCmdWorld = Vector3d.zero, thrustAccelWorld = Vector3d.zero,
                thrustDirWorld = Vector3d.zero, requiredThrust = 0.0, throttle = 0f
            };

            if (mass <= 0.0)           { outp.reason = "Mass <= 0";       return outp; }
            if (availThrust <= 1e-6)   { outp.reason = "No thrust";       return outp; }
            if (xHat.magnitude < 1e-6 || yHat.magnitude < 1e-6 || zHat.magnitude < 1e-6)
                                       { outp.reason = "Invalid frame";   return outp; }

            // Project state into the guidance frame.
            Vector3d rel = pos - targetPos;
            Vector3d RG  = new Vector3d(Vector3d.Dot(rel, xHat), Vector3d.Dot(rel, yHat), Vector3d.Dot(rel, zHat));
            Vector3d VG  = new Vector3d(Vector3d.Dot(vel, xHat), Vector3d.Dot(vel, yHat), Vector3d.Dot(vel, zHat));

            // Desired final-state vectors projected into the guidance frame.
            Vector3d vfW = vfWorld     ?? Vector3d.zero;
            Vector3d afW = aFinalWorld ?? Vector3d.zero;
            Vector3d jfW = jFinalWorld ?? Vector3d.zero;
            Vector3d VDG = new Vector3d(Vector3d.Dot(vfW, xHat), Vector3d.Dot(vfW, yHat), Vector3d.Dot(vfW, zHat));
            Vector3d ADG = new Vector3d(Vector3d.Dot(afW, xHat), Vector3d.Dot(afW, yHat), Vector3d.Dot(afW, zHat));
            Vector3d JDG = new Vector3d(Vector3d.Dot(jfW, xHat), Vector3d.Dot(jfW, yHat), Vector3d.Dot(jfW, zHat));
            Vector3d RDG = Vector3d.zero; // target is the origin in relative frame

            // Extract the 1-D problem on the chosen axis for the tgo solve.
            double Raxis  = tgoAxis == 0 ? RG.x  : tgoAxis == 1 ? RG.y  : RG.z;
            double Vaxis  = tgoAxis == 0 ? VG.x  : tgoAxis == 1 ? VG.y  : VG.z;
            double RdAxis = 0.0;
            double VdAxis = tgoAxis == 0 ? VDG.x : tgoAxis == 1 ? VDG.y : VDG.z;
            double AdAxis = tgoAxis == 0 ? ADG.x : tgoAxis == 1 ? ADG.y : ADG.z;
            double JdAxis = tgoAxis == 0 ? JDG.x : tgoAxis == 1 ? JDG.y : JDG.z;

            // Newton iteration on tgo.
            double T         = Math.Max(1.0, tgoGuess);
            bool   converged = false;
            for (int i = 0; i < maxIter; i++)
            {
                double dR = RdAxis - Raxis;
                double F  = JdAxis * T * T * T - 6.0 * AdAxis * T * T + 6.0 * Vaxis * T + 18.0 * VdAxis * T - 24.0 * dR;
                double dF = 3.0 * JdAxis * T * T - 12.0 * AdAxis * T + 6.0 * Vaxis + 18.0 * VdAxis;

                if (Math.Abs(dF) < 1e-9)   { outp.reason = "Apollo dF too small"; return outp; }
                double dT = -F / dF;
                T += dT;
                if (!IsFinite(T) || T <= 0.0) { outp.reason = "Apollo T invalid"; return outp; }
                if (Math.Abs(dT) < tol) { converged = true; break; }
            }
            if (!converged) { outp.reason = "Apollo TGO no converge"; return outp; }

            T        = Math.Max(1.0, T);
            outp.tgo = T;

            // Compute commanded acceleration, subtract gravity to get thrust acceleration.
            Vector3d ACG       = ADG + (12.0 / (T * T)) * (RDG - RG) - (6.0 / T) * (VDG + VG);
            Vector3d accelCmdW = ACG.x * xHat + ACG.y * yHat + ACG.z * zHat;

            double rmag = pos.magnitude;
            if (rmag < 1.0) { outp.reason = "pos magnitude too small"; return outp; }

            Vector3d gVec         = -(mu / (rmag * rmag * rmag)) * pos;
            Vector3d thrustAccelW = accelCmdW - gVec;
            double   taMag        = thrustAccelW.magnitude;
            if (!IsFinite(taMag) || taMag < 1e-6) { outp.reason = "thrust accel too small"; return outp; }

            outp.valid            = true;
            outp.reason           = "OK";
            outp.accelCmdWorld    = accelCmdW;
            outp.thrustAccelWorld = thrustAccelW;
            outp.thrustDirWorld   = thrustAccelW / taMag;
            outp.requiredThrust   = mass * taMag;
            outp.throttle         = (float)Math.Min(1.0, Math.Max(0.0, outp.requiredThrust / availThrust));
            return outp;
        }

        // =========================================================================
        // Gravity-turn terminal law (Han, Jo & Ho, arXiv:2409.01465)
        // =========================================================================

        /// <summary>
        /// Computes a gravity-turn guidance command using the law from Han, Jo &amp; Ho
        /// (arXiv:2409.01465). Handles collision avoidance via a cone constraint and
        /// a smooth blending function.
        /// </summary>
        /// <param name="pos">Vehicle position, body-centred inertial [m].</param>
        /// <param name="vel">Vehicle velocity in the world frame [m/s].</param>
        /// <param name="targetPos">Target position, body-centred inertial [m].</param>
        /// <param name="xHat">Solver-frame downrange unit vector.</param>
        /// <param name="yHat">Solver-frame radial (up) unit vector.</param>
        /// <param name="zHat">Solver-frame crossrange unit vector.</param>
        /// <param name="mu">Gravitational parameter [m³/s²].</param>
        /// <param name="mass">Vehicle mass [kg].</param>
        /// <param name="availThrust">Maximum thrust [N].</param>
        /// <param name="ve">Effective exhaust velocity [m/s].</param>
        /// <param name="tMinAbs">Minimum thrust magnitude [N] (i.e. min throttle × availThrust).</param>
        /// <param name="cB">β throttle fraction (typically 0.95).</param>
        /// <param name="kGain">Tracking error gain (paper k, typically 2.4).</param>
        /// <param name="cE">Tracking error threshold that activates collision avoidance.</param>
        /// <param name="phiRad">Half-angle of the approach cone [rad].</param>
        /// <param name="delta">Cone buffer distance [m].</param>
        /// <param name="cColLower">Lower bound for collision-avoidance blending (fraction of Tmax/m).</param>
        /// <param name="cColUpper">Upper bound for collision-avoidance blending (fraction of Tmax/m).</param>
        public static GTGuidanceOutput ComputeGravityTurnCommand(
            Vector3d pos, Vector3d vel, Vector3d targetPos,
            Vector3d xHat, Vector3d yHat, Vector3d zHat,
            double mu, double mass, double availThrust, double ve,
            double tMinAbs,
            double cB        = 0.95, double kGain      = 2.4,
            double cE        = 20.0, double phiRad      = 0.0,
            double delta     = 5.0,  double cColLower   = 0.75,
            double cColUpper = 0.95)
        {
            GTGuidanceOutput outp = new GTGuidanceOutput
            {
                valid = false, reason = "Unknown", tgo = double.NaN,
                beta  = double.NaN, gammaStar = double.NaN,
                thrustDirWorld = Vector3d.zero, requiredThrust = 0, throttle = 0f
            };

            if (mass <= 0.0 || availThrust <= 1e-6 || ve <= 1e-6)
                { outp.reason = "Invalid mass/thrust/Ve"; return outp; }

            double tMax = availThrust;
            double tMin = tMinAbs;
            double rmag = pos.magnitude;
            if (rmag < 1.0) { outp.reason = "Invalid pos"; return outp; }
            double g = mu / (rmag * rmag);

            // Frame mapping: paper's (x, y, z) ↔ KSP solver (x, z, y).
            Vector3d rel = pos - targetPos;
            double rXL = Vector3d.Dot(rel, xHat);
            double rYL = Vector3d.Dot(rel, zHat);
            double rZL = Vector3d.Dot(rel, yHat);
            double vXL = Vector3d.Dot(vel, xHat);
            double vYL = Vector3d.Dot(vel, zHat);
            double vZL = Vector3d.Dot(vel, yHat);

            // --- Step 1: β, β̇, G-frame axes (Eq. 21–22, 44–45) ---

            double beta     = cB * tMax / (mass * g);
            double betaDot  = beta * beta * g / ve;
            if (beta <= 1.0) { outp.reason = $"β={beta:F3}≤1"; return outp; }

            double   xGoRaw = Math.Sqrt(rXL * rXL + rYL * rYL);
            Vector3d xG, yG, zG;
            if (xGoRaw > 0.5)
            {
                xG = (-rXL * xHat - rYL * zHat) / xGoRaw;
                zG = yHat; yG = Vector3d.Cross(zG, xG);
            }
            else { xG = xHat; zG = yHat; yG = zHat; }

            Vector3d vG   = new Vector3d(Vector3d.Dot(vel, xG), Vector3d.Dot(vel, yG), Vector3d.Dot(vel, zG));
            Vector3d gVec = -(mu / (rmag * rmag * rmag)) * pos;
            Vector3d gG   = new Vector3d(Vector3d.Dot(gVec, xG), Vector3d.Dot(gVec, yG), Vector3d.Dot(gVec, zG));

            // --- Step 2: x_go, z_go, γ*, v* (Eq. 13–17) ---

            double xGo = xGoRaw;
            double zGo = -rZL;
            double gammaStar, vStar;

            if (xGo < 0.5)
            {
                gammaStar = -Math.PI / 2.0;
                vStar     = Math.Sqrt(Math.Max(0.0, 2.0 * (beta - 1.0) * g * Math.Abs(zGo)));
            }
            else
            {
                double kappa = ((4.0 * beta * beta - 4.0) * zGo) / ((4.0 * beta * beta - 1.0) * xGo);
                double gamma = Math.Atan2(zGo, xGo);
                bool   conv  = false;
                for (int i = 0; i < 100; i++)
                {
                    double sy = Math.Sin(gamma), cy = Math.Cos(gamma);
                    double dhDenom = 2.0 * beta * cy - sy * cy;
                    if (Math.Abs(dhDenom) < 1e-12) break;
                    double h  = (2.0 * beta * sy - sy * sy - 1.0) / dhDenom - kappa;
                    double dh = (3.0 * (beta - sy) * (beta - sy) + beta * beta - 1.0) / (dhDenom * dhDenom);
                    if (!IsFinite(h) || !IsFinite(dh) || Math.Abs(dh) < 1e-12) break;
                    double gNew = gamma - h / dh;
                    if (!IsFinite(gNew)) break;
                    bool done = Math.Abs(gNew - gamma) < 1e-10;
                    gamma = gNew;
                    if (done) { conv = true; break; }
                }
                if (!conv) { outp.reason = "γ* no converge"; return outp; }

                gammaStar    = gamma;
                double sy2   = Math.Sin(gammaStar), cy2 = Math.Cos(gammaStar);
                double vSqRaw = (4.0 * beta * beta - 1.0) * g * xGo / ((2.0 * beta - sy2) * cy2);
                if (vSqRaw <= 0.0) { outp.reason = "v* imaginary"; return outp; }
                vStar = Math.Sqrt(vSqRaw);
            }
            if (!IsFinite(gammaStar) || !IsFinite(vStar)) { outp.reason = "γ*/v* not finite"; return outp; }

            // --- Step 3: v_d^G, tracking error, t_go (Eq. 24, 33) ---

            double   vxStar = vStar * Math.Cos(gammaStar);
            double   vzStar = vStar * Math.Sin(gammaStar);
            Vector3d vdG    = new Vector3d(vxStar, 0.0, vzStar);
            Vector3d eG     = vdG - vG;
            double   tGo    = (beta * vStar - vzStar) / ((beta * beta - 1.0) * g) + eG.magnitude / (beta * g);
            tGo = Math.Max(tGo, 1e-3);

            // --- Step 4: Jacobians, tracking command (Eq. 27–34) ---

            double dfxDvx = (1.0 / vStar) * (2.0 * beta * vxStar * vxStar + 2.0 * beta * vStar * vStar - vStar * vzStar);
            double dfxDvz = (1.0 / vStar) * (2.0 * beta * vxStar * vzStar - vStar * vxStar);
            double dfzDvx = (1.0 / vStar) * (2.0 * beta * vxStar * vzStar - 2.0 * vStar * vxStar);
            double dfzDvz = (1.0 / vStar) * (2.0 * beta * vzStar * vzStar + 2.0 * beta * vStar * vStar - 4.0 * vStar * vzStar);
            double det    = dfxDvx * dfzDvz - dfzDvx * dfxDvz;
            if (!IsFinite(det) || Math.Abs(det) < 1e-12) { outp.reason = "F†_vd singular"; return outp; }

            double dfxDxgo  = -(4.0 * beta * beta - 1.0) * g;
            double dfzDzgo  = -(4.0 * beta * beta - 4.0) * g;
            double dfxDBeta = 2.0 * vStar * vxStar - 8.0 * beta * g * xGo;
            double dfzDBeta = 2.0 * vStar * vzStar - 8.0 * beta * g * zGo;
            double omegaZ   = xGo > 1e-9 ? -vG.y / xGo : 0.0;

            Vector3d omCrossVd = new Vector3d(0.0, omegaZ * vxStar, 0.0);

            double ff00 =  dfzDvz * dfxDxgo  / det;
            double ff02 = -dfxDvz * dfzDzgo  / det;
            double ff20 = -dfzDvx * dfxDxgo  / det;
            double ff22 =  dfxDvx * dfzDzgo  / det;
            Vector3d fdagFrgoVg = new Vector3d(
                ff00 * vG.x + ff02 * vG.z, 0.0,
                ff20 * vG.x + ff22 * vG.z);

            double   fbX       = ( dfzDvz * dfxDBeta - dfxDvz * dfzDBeta) / det;
            double   fbZ       = (-dfzDvx * dfxDBeta + dfxDvx * dfzDBeta) / det;
            Vector3d fdagFbeta = new Vector3d(fbX, 0.0, fbZ);

            Vector3d aGTrk     = fdagFrgoVg - fdagFbeta * betaDot + omCrossVd - gG + (kGain / tGo) * eG;
            Vector3d aTrkWorld = aGTrk.x * xG + aGTrk.y * yG + aGTrk.z * zG;

            // --- Step 5: Collision avoidance (Eq. 46–52) ---

            Vector3d aColWorld = Vector3d.zero;
            if (eG.magnitude >= cE || aTrkWorld.magnitude >= tMax / mass)
            {
                double sin2   = Math.Sin(phiRad) * Math.Sin(phiRad);
                double cos2   = Math.Cos(phiRad) * Math.Cos(phiRad);
                double rDotV  = Vector3d.Dot(rel, vel);
                double rSq    = rel.sqrMagnitude;
                double vSqMag = vel.sqrMagnitude;

                double aP   = vZL * vZL - vSqMag * sin2;
                double bP   = rZL * vZL - rDotV  * sin2;
                double cP   = rZL * rZL - rSq    * sin2;
                double disc = bP * bP - aP * cP;
                double tP   = (Math.Abs(aP) > 1e-12 && disc >= 0.0)
                              ? Math.Max(0.0, (-bP - Math.Sqrt(disc)) / aP) : 0.0;

                Vector3d rP    = rel + vel * tP;
                double   rPxL  = Vector3d.Dot(rP, xHat);
                double   rPyL  = Vector3d.Dot(rP, zHat);
                double   rPzL  = Vector3d.Dot(rP, yHat);
                double   npDen = Math.Max(1e-6, Math.Sqrt(
                    (rPxL * rPxL + rPyL * rPyL) * sin2 * sin2 + rPzL * rPzL * cos2 * cos2));

                Vector3d nP   = (1.0 / npDen) * (-rPxL * sin2 * xHat - rPyL * sin2 * zHat + rPzL * cos2 * yHat);
                double   s    = Math.Max(Vector3d.Dot(rel - rP, nP) - delta, 0.1);
                double   vDotN = Vector3d.Dot(vel, nP);

                Vector3d aN = Vector3d.zero;
                if (vDotN < 0.0)
                {
                    double gDotN = Vector3d.Dot(gVec, nP);
                    aN = (-gDotN + vDotN * vDotN / (2.0 * s)) * nP;
                }

                double sig = Sigma(aN.magnitude, cColLower * tMax / mass, cColUpper * tMax / mass);
                aColWorld  = sig * aN;
            }

            // --- Step 6: Final sat/fit command (Eq. 55) ---

            Vector3d uWorld = Sat(aColWorld + Fit(aColWorld, aTrkWorld, tMax / mass), tMin / mass, tMax / mass);
            double   uMag   = uWorld.magnitude;
            if (!IsFinite(uMag) || uMag < 1e-6) { outp.reason = "u_mag invalid"; return outp; }

            outp.valid          = true;
            outp.reason         = $"OK β={beta:F3} γ*={gammaStar * 180.0 / Math.PI:F1}° tgo={tGo:F1}s";
            outp.tgo            = tGo;
            outp.beta           = beta;
            outp.gammaStar      = gammaStar * 180.0 / Math.PI;
            outp.thrustDirWorld = uWorld / uMag;
            outp.requiredThrust = mass * uMag;
            outp.throttle       = (float)Math.Min(1.0, Math.Max(0.0, outp.requiredThrust / tMax));
            return outp;
        }

        // =========================================================================
        // GT helper functions (Eq. 46–55 support)
        // =========================================================================

        /// <summary>Smooth ramp from 0 to 1 over the interval [lo, hi].</summary>
        private static double Sigma(double x, double lo, double hi)
        {
            if (x <= lo) return 0.0;
            if (x >= hi) return 1.0;
            return (x - lo) / (hi - lo);
        }

        /// <summary>Saturates the magnitude of <paramref name="x"/> to [lo, hi].</summary>
        private static Vector3d Sat(Vector3d x, double lo, double hi)
        {
            double mag = x.magnitude;
            if (mag < 1e-12)  return Vector3d.zero;
            if (mag < lo)     return (lo / mag) * x;
            if (mag > hi)     return (hi / mag) * x;
            return x;
        }

        /// <summary>
        /// Computes the fit vector from <paramref name="x"/> toward <paramref name="y"/>
        /// such that x + fit(x,y,c) lies on the ball of radius c (paper Eq. 55 helper).
        /// </summary>
        private static Vector3d Fit(Vector3d x, Vector3d y, double c)
        {
            double xmag = x.magnitude;
            if (xmag > c)     return Vector3d.zero;
            if (xmag < 1e-12) return Sat(y, 0.0, c);

            Vector3d xhat = x / xmag;
            if (Vector3d.Dot(x, y) < 0.0)
            {
                // y points away from x; use perpendicular component only.
                Vector3d yperp = y - Vector3d.Dot(y, xhat) * xhat;
                double   rim   = Math.Sqrt(Math.Max(0.0, c * c - xmag * xmag));
                return Sat(yperp, 0.0, rim);
            }
            double ymag     = y.magnitude;
            if (ymag < 1e-12) return Vector3d.zero;
            double xDotYhat = Vector3d.Dot(x, y / ymag);
            double upper    = xDotYhat + Math.Sqrt(Math.Max(0.0, xDotYhat * xDotYhat + c * c - xmag * xmag));
            return Sat(y, 0.0, upper);
        }

        private static bool IsFinite(double v) => PDGMathUtils.IsFinite(v);
    }
}