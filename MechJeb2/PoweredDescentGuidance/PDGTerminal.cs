using System;
using UnityEngine;

namespace MuMech.Landing
{
    public partial class PDGGuidanceLoop
    {
        #region Apollo zero-jerk terminal law

        private ApolloGuidanceOutput ComputeApolloZeroJerkCommand(
            Vector3d pos, Vector3d vel, Vector3d targetPos,
            Vector3d x_hat, Vector3d y_hat, Vector3d z_hat,
            double mu, double mass, double availThrust,
            Vector3d? vfWorldOpt = null, Vector3d? aFinalWorldOpt = null, Vector3d? jFinalWorldOpt = null,
            double tgoGuess = 10.0, int tgoAxis = 1, int maxIter = 15, double tol = 1e-3)
        {
            ApolloGuidanceOutput outp = new ApolloGuidanceOutput {
                valid = false, reason = "Unknown", tgo = double.NaN,
                accelCmdWorld = Vector3d.zero, thrustAccelWorld = Vector3d.zero,
                thrustDirWorld = Vector3d.zero, requiredThrust = 0.0, throttle = 0f
            };

            if (mass <= 0.0) { outp.reason = "Mass <= 0"; return outp; }
            if (availThrust <= 1e-6) { outp.reason = "No thrust"; return outp; }
            if (x_hat.magnitude < 1e-6 || y_hat.magnitude < 1e-6 || z_hat.magnitude < 1e-6)
                { outp.reason = "Invalid frame"; return outp; }

            Vector3d rel  = pos - targetPos;
            Vector3d RG   = new Vector3d(Vector3d.Dot(rel, x_hat), Vector3d.Dot(rel, y_hat), Vector3d.Dot(rel, z_hat));
            Vector3d VG   = new Vector3d(Vector3d.Dot(vel, x_hat), Vector3d.Dot(vel, y_hat), Vector3d.Dot(vel, z_hat));

            Vector3d vfW  = vfWorldOpt ?? Vector3d.zero;
            Vector3d afW  = aFinalWorldOpt ?? Vector3d.zero;
            Vector3d jfW  = jFinalWorldOpt ?? Vector3d.zero;
            Vector3d VDG  = new Vector3d(Vector3d.Dot(vfW, x_hat), Vector3d.Dot(vfW, y_hat), Vector3d.Dot(vfW, z_hat));
            Vector3d ADG  = new Vector3d(Vector3d.Dot(afW, x_hat), Vector3d.Dot(afW, y_hat), Vector3d.Dot(afW, z_hat));
            Vector3d JDG  = new Vector3d(Vector3d.Dot(jfW, x_hat), Vector3d.Dot(jfW, y_hat), Vector3d.Dot(jfW, z_hat));
            Vector3d RDG  = Vector3d.zero;

            double T = Math.Max(1.0, tgoGuess);
            double Raxis  = (tgoAxis == 0) ? RG.x : (tgoAxis == 1 ? RG.y : RG.z);
            double Vaxis  = (tgoAxis == 0) ? VG.x : (tgoAxis == 1 ? VG.y : VG.z);
            double RdAxis = 0.0;
            double VdAxis = (tgoAxis == 0) ? VDG.x : (tgoAxis == 1 ? VDG.y : VDG.z);
            double AdAxis = (tgoAxis == 0) ? ADG.x : (tgoAxis == 1 ? ADG.y : ADG.z);
            double JdAxis = (tgoAxis == 0) ? JDG.x : (tgoAxis == 1 ? JDG.y : JDG.z);

            bool converged = false;
            for (int i = 0; i < maxIter; i++)
            {
                double dR = RdAxis - Raxis;
                double F  = JdAxis * T * T * T - 6.0 * AdAxis * T * T + 6.0 * Vaxis * T + 18.0 * VdAxis * T - 24.0 * dR;
                double dF = 3.0 * JdAxis * T * T - 12.0 * AdAxis * T + 6.0 * Vaxis + 18.0 * VdAxis;
                if (Math.Abs(dF) < 1e-9) { outp.reason = "Apollo dF too small"; return outp; }
                double dT = -F / dF; T += dT;
                if (!IsFinite(T) || T <= 0.0) { outp.reason = "Apollo T invalid"; return outp; }
                if (Math.Abs(dT) < tol) { converged = true; break; }
            }
            if (!converged) { outp.reason = "Apollo TGO no converge"; return outp; }
            T = Math.Max(1.0, T);
            outp.tgo = T;

            Vector3d ACG          = ADG + (12.0 / (T * T)) * (RDG - RG) - (6.0 / T) * (VDG + VG);
            Vector3d accelCmdW    = ACG.x * x_hat + ACG.y * y_hat + ACG.z * z_hat;
            double   rmag         = pos.magnitude;
            if (rmag < 1.0) { outp.reason = "pos magnitude too small"; return outp; }
            Vector3d gVec         = -(mu / (rmag * rmag * rmag)) * pos;
            Vector3d thrustAccelW = accelCmdW - gVec;
            double   taMag        = thrustAccelW.magnitude;
            if (!IsFinite(taMag) || taMag < 1e-6) { outp.reason = "thrust accel too small"; return outp; }

            outp.valid          = true;
            outp.reason         = "OK";
            outp.accelCmdWorld  = accelCmdW;
            outp.thrustAccelWorld = thrustAccelW;
            outp.thrustDirWorld = thrustAccelW / taMag;
            outp.requiredThrust = mass * taMag;
            outp.throttle       = (float)Math.Min(1.0, Math.Max(0.0, outp.requiredThrust / availThrust));
            return outp;
        }

        #endregion

        #region Gravity-turn terminal law (Han, Jo & Ho arXiv:2409.01465)


        private static double GT_Sigma(double x, double lo, double hi)
        {
            if (x <= lo) return 0.0; if (x >= hi) return 1.0;
            return (x - lo) / (hi - lo);
        }

        private static Vector3d GT_Sat(Vector3d x, double lo, double hi)
        {
            double mag = x.magnitude;
            if (mag < 1e-12) return Vector3d.zero;
            if (mag < lo)    return (lo  / mag) * x;
            if (mag > hi)    return (hi  / mag) * x;
            return x;
        }

        private static Vector3d GT_Fit(Vector3d x, Vector3d y, double c)
        {
            double xmag = x.magnitude;
            if (xmag > c)     return Vector3d.zero;
            if (xmag < 1e-12) return GT_Sat(y, 0.0, c);
            Vector3d xhat = x / xmag;
            if (Vector3d.Dot(x, y) < 0.0)
            {
                Vector3d yperp = y - Vector3d.Dot(y, xhat) * xhat;
                double   rim   = Math.Sqrt(Math.Max(0.0, c * c - xmag * xmag));
                return GT_Sat(yperp, 0.0, rim);
            }
            double ymag     = y.magnitude; if (ymag < 1e-12) return Vector3d.zero;
            double xDotYhat = Vector3d.Dot(x, y / ymag);
            double upper    = xDotYhat + Math.Sqrt(Math.Max(0.0, xDotYhat * xDotYhat + c * c - xmag * xmag));
            return GT_Sat(y, 0.0, upper);
        }

        private GTGuidanceOutput ComputeGravityTurnCommand(
            Vector3d pos, Vector3d vel, Vector3d targetPos,
            Vector3d x_hat, Vector3d y_hat, Vector3d z_hat,
            double mu, double mass, double availThrust, double Ve,
            double Tmin_abs, double C_b = 0.95, double k_gain = 2.4, double C_e = 20.0,
            double phi_rad = 0.0, double delta = 5.0, double C_col_lower = 0.75, double C_col_upper = 0.95)
        {
            GTGuidanceOutput outp = new GTGuidanceOutput {
                valid = false, reason = "Unknown", tgo = double.NaN, beta = double.NaN,
                gammaStar = double.NaN, thrustDirWorld = Vector3d.zero, requiredThrust = 0, throttle = 0f
            };

            if (mass <= 0.0 || availThrust <= 1e-6 || Ve <= 1e-6)
                { outp.reason = "Invalid mass/thrust/Ve"; return outp; }

            double Tmax = availThrust; double Tmin = Tmin_abs;
            double rmag = pos.magnitude; if (rmag < 1.0) { outp.reason = "Invalid pos"; return outp; }
            double g    = mu / (rmag * rmag);

            // Frame mapping: paper L ← KSP solver frame
            // paper x=downrange(x_hat), paper y=lateral(z_hat), paper z=vertical(y_hat)
            Vector3d rel  = pos - targetPos;
            double r_xL   = Vector3d.Dot(rel, x_hat);
            double r_yL   = Vector3d.Dot(rel, z_hat);
            double r_zL   = Vector3d.Dot(rel, y_hat);
            double v_xL   = Vector3d.Dot(vel, x_hat);
            double v_yL   = Vector3d.Dot(vel, z_hat);
            double v_zL   = Vector3d.Dot(vel, y_hat);

            // Step 1 — β, β̇, G-frame axes (Eq. 21-22, 44-45)
            double Beta     = C_b * Tmax / (mass * g);
            double Beta_dot = Beta * Beta * g / Ve;
            if (Beta <= 1.0) { outp.reason = $"β={Beta:F3}≤1"; return outp; }

            double x_go_raw = Math.Sqrt(r_xL * r_xL + r_yL * r_yL);
            Vector3d xG, yG, zG;
            if (x_go_raw > 0.5)
            {
                xG = (-r_xL * x_hat - r_yL * z_hat) / x_go_raw;
                zG = y_hat; yG = Vector3d.Cross(zG, xG);
            }
            else { xG = x_hat; zG = y_hat; yG = z_hat; }

            Vector3d v_G  = new Vector3d(Vector3d.Dot(vel, xG), Vector3d.Dot(vel, yG), Vector3d.Dot(vel, zG));
            Vector3d gVec = -(mu / (rmag * rmag * rmag)) * pos;
            Vector3d g_G  = new Vector3d(Vector3d.Dot(gVec, xG), Vector3d.Dot(gVec, yG), Vector3d.Dot(gVec, zG));

            // Step 2 — x_go, z_go, γ*, v* (Eq. 13-17)
            double x_go = x_go_raw;
            double z_go = -r_zL;
            double gamma_star, v_star;
            if (x_go < 0.5)
            {
                gamma_star = -Math.PI / 2.0;
                v_star     = Math.Sqrt(Math.Max(0.0, 2.0 * (Beta - 1.0) * g * Math.Abs(z_go)));
            }
            else
            {
                double kappa = ((4.0 * Beta * Beta - 4.0) * z_go) / ((4.0 * Beta * Beta - 1.0) * x_go);
                double gamma = Math.Atan2(z_go, x_go);
                bool   conv  = false;
                for (int i = 0; i < 100; i++)
                {
                    double sy = Math.Sin(gamma), cy = Math.Cos(gamma);
                    double dh_denom = 2.0 * Beta * cy - sy * cy;
                    if (Math.Abs(dh_denom) < 1e-12) break;
                    double h  = (2.0 * Beta * sy - sy * sy - 1.0) / dh_denom - kappa;
                    double dh = (3.0 * (Beta - sy) * (Beta - sy) + Beta * Beta - 1.0) / (dh_denom * dh_denom);
                    if (!IsFinite(h) || !IsFinite(dh) || Math.Abs(dh) < 1e-12) break;
                    double gnew = gamma - h / dh;
                    if (!IsFinite(gnew)) break;
                    bool done = Math.Abs(gnew - gamma) < 1e-10;
                    gamma = gnew;
                    if (done) { conv = true; break; }
                }
                if (!conv) { outp.reason = "γ* no converge"; return outp; }
                gamma_star = gamma;
                double sy2 = Math.Sin(gamma_star), cy2 = Math.Cos(gamma_star);
                double vSq = (4.0 * Beta * Beta - 1.0) * g * x_go / ((2.0 * Beta - sy2) * cy2);
                if (vSq <= 0.0) { outp.reason = "v* imaginary"; return outp; }
                v_star = Math.Sqrt(vSq);
            }
            if (!IsFinite(gamma_star) || !IsFinite(v_star)) { outp.reason = "γ*/v* not finite"; return outp; }

            // Step 3 — v_d^G, tracking error, t_go (Eq. 24, 33)
            double vx_star = v_star * Math.Cos(gamma_star);
            double vz_star = v_star * Math.Sin(gamma_star);
            Vector3d vd_G  = new Vector3d(vx_star, 0.0, vz_star);
            Vector3d e_G   = vd_G - v_G;
            double t_go    = (Beta * v_star - vz_star) / ((Beta * Beta - 1.0) * g) + e_G.magnitude / (Beta * g);
            t_go = Math.Max(t_go, 1e-3);

            // Step 4 — Jacobians, tracking command (Eq. 27-34)
            double dfx_dvx = (1.0 / v_star) * (2.0 * Beta * vx_star * vx_star + 2.0 * Beta * v_star * v_star - v_star * vz_star);
            double dfx_dvz = (1.0 / v_star) * (2.0 * Beta * vx_star * vz_star - v_star * vx_star);
            double dfz_dvx = (1.0 / v_star) * (2.0 * Beta * vx_star * vz_star - 2.0 * v_star * vx_star);
            double dfz_dvz = (1.0 / v_star) * (2.0 * Beta * vz_star * vz_star + 2.0 * Beta * v_star * v_star - 4.0 * v_star * vz_star);
            double det     = dfx_dvx * dfz_dvz - dfz_dvx * dfx_dvz;
            if (!IsFinite(det) || Math.Abs(det) < 1e-12) { outp.reason = "F†_vd singular"; return outp; }

            double dfx_dxgo  = -(4.0 * Beta * Beta - 1.0) * g;
            double dfz_dzgo  = -(4.0 * Beta * Beta - 4.0) * g;
            double dfx_dbeta = 2.0 * v_star * vx_star - 8.0 * Beta * g * x_go;
            double dfz_dbeta = 2.0 * v_star * vz_star - 8.0 * Beta * g * z_go;
            double omega_z   = (x_go > 1e-9) ? -v_G.y / x_go : 0.0;
            Vector3d omCrossVd = new Vector3d(0.0, omega_z * vx_star, 0.0);

            double ff00 =  dfz_dvz * dfx_dxgo / det;
            double ff02 = -dfx_dvz * dfz_dzgo / det;
            double ff20 = -dfz_dvx * dfx_dxgo / det;
            double ff22 =  dfx_dvx * dfz_dzgo / det;
            Vector3d FdagFrgoVg = new Vector3d(ff00 * v_G.x + ff02 * v_G.z, 0.0, ff20 * v_G.x + ff22 * v_G.z);
            double fb_x  = ( dfz_dvz * dfx_dbeta - dfx_dvz * dfz_dbeta) / det;
            double fb_z  = (-dfz_dvx * dfx_dbeta + dfx_dvx * dfz_dbeta) / det;
            Vector3d FdagFbeta = new Vector3d(fb_x, 0.0, fb_z);

            Vector3d a_G_trk     = FdagFrgoVg - FdagFbeta * Beta_dot + omCrossVd - g_G + (k_gain / t_go) * e_G;
            Vector3d a_trk_world = a_G_trk.x * xG + a_G_trk.y * yG + a_G_trk.z * zG;

            // Step 5 — Collision avoidance (Eq. 46-52)
            Vector3d a_col_world = Vector3d.zero;
            if (e_G.magnitude >= C_e || a_trk_world.magnitude >= Tmax / mass)
            {
                double sin2 = Math.Sin(phi_rad) * Math.Sin(phi_rad);
                double cos2 = Math.Cos(phi_rad) * Math.Cos(phi_rad);
                double rDotV = Vector3d.Dot(rel, vel), rSq = rel.sqrMagnitude, vSq = vel.sqrMagnitude;
                double a_p   = v_zL * v_zL - vSq * sin2;
                double b_p   = r_zL * v_zL - rDotV * sin2;
                double c_p   = r_zL * r_zL - rSq   * sin2;
                double disc  = b_p * b_p - a_p * c_p;
                double t_p   = (Math.Abs(a_p) > 1e-12 && disc >= 0.0)
                               ? Math.Max(0.0, (-b_p - Math.Sqrt(disc)) / a_p) : 0.0;
                Vector3d r_p = rel + vel * t_p;

                double r_px_L = Vector3d.Dot(r_p, x_hat);
                double r_py_L = Vector3d.Dot(r_p, z_hat);
                double r_pz_L = Vector3d.Dot(r_p, y_hat);
                double np_den = Math.Max(1e-6, Math.Sqrt(
                    (r_px_L * r_px_L + r_py_L * r_py_L) * sin2 * sin2 + r_pz_L * r_pz_L * cos2 * cos2));
                Vector3d n_p  = (1.0 / np_den) * (-r_px_L * sin2 * x_hat - r_py_L * sin2 * z_hat + r_pz_L * cos2 * y_hat);

                double s     = Math.Max(Vector3d.Dot(rel - r_p, n_p) - delta, 0.1);
                double vDotN = Vector3d.Dot(vel, n_p);
                Vector3d a_n = Vector3d.zero;
                if (vDotN < 0.0) { double gDotN = Vector3d.Dot(gVec, n_p); a_n = (-gDotN + (vDotN * vDotN) / (2.0 * s)) * n_p; }

                double sig   = GT_Sigma(a_n.magnitude, C_col_lower * Tmax / mass, C_col_upper * Tmax / mass);
                a_col_world  = sig * a_n;
            }

            // Step 6 — Final sat/fit command (Eq. 55)
            Vector3d u_world = GT_Sat(a_col_world + GT_Fit(a_col_world, a_trk_world, Tmax / mass), Tmin / mass, Tmax / mass);
            double   u_mag   = u_world.magnitude;
            if (!IsFinite(u_mag) || u_mag < 1e-6) { outp.reason = "u_mag invalid"; return outp; }

            outp.valid          = true;
            outp.reason         = $"OK β={Beta:F3} γ*={gamma_star * 180.0 / Math.PI:F1}° tgo={t_go:F1}s";
            outp.tgo            = t_go;
            outp.beta           = Beta;
            outp.gammaStar      = gamma_star * 180.0 / Math.PI;
            outp.thrustDirWorld = u_world / u_mag;
            outp.requiredThrust = mass * u_mag;
            outp.throttle       = (float)Math.Min(1.0, Math.Max(0.0, outp.requiredThrust / Tmax));
            return outp;
        }

        #endregion
    }
}