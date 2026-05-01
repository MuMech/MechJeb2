using System;
using System.Text;
using UnityEngine;

namespace MuMech.Landing
{
    public partial class PDGGuidanceLoop
    {
        

        #region DCTGIG outer loop

        private CTGResult SolveDCTGIG_Outer(
            Vector3d r0, Vector3d v0, Vector3d vf,
            double target_x, double m0, double T_guess, double Ve,
            double mu, double R, int poly_order)
        {
            double T_mag = Math.Max(_availableThrust * 0.1,
                           Math.Min(_availableThrust,
                                    T_guess > 0 ? T_guess : _availableThrust * PlanThrottle));
            double tf_current = _current_tf > 0 ? _current_tf : 1.0;
            CTGResult best = null;

            for (int k = 0; k < 10; k++)
            {
                DbgOuterIter = k + 1;
                double alpha = (m0 * Ve) / Math.Max(1.0, T_mag);
                CTGResult res = IterateCTG(tf_current, r0, v0, vf, Ve, alpha, T_mag, mu, R, poly_order);
                if (res == null || !res.converged || !IsFinite(res.x_f) || !IsFinite(res.dx_dT)) break;

                tf_current = res.tf;
                best = res;

                double err = res.x_f - target_x;
                if (Math.Abs(err) < 0.1) break;

                DbgDxDt = res.dx_dT;
                // Limit T change to 1% per outer step to smooth throttle
                T_mag = T_mag - Math.Max(T_mag * -0.01, Math.Min(T_mag * 0.01, err / res.dx_dT));
                T_mag = Math.Max(_availableThrust * 0.1, Math.Min(_availableThrust, T_mag));
                best.T_mag = T_mag;
                DbgTMagBest = T_mag;
            }
            return (best != null && best.converged) ? best : null;
        }

        #endregion

        #region DCTGIG inner iterator

        private CTGResult IterateCTG(
            double tf0, Vector3d r0, Vector3d v0, Vector3d vf,
            double Ve, double alpha, double T, double mu, double R, int poly_order)
        {
            ResetInnerDebug(tf0);
            CTGResult res = IterateCTGCore(tf0, r0, v0, vf, Ve, alpha, T, mu, R, poly_order);
            return res;
        }

        private CTGResult IterateCTGCore(
            double tf0, Vector3d r0, Vector3d v0, Vector3d vf,
            double Ve, double alpha, double T, double mu, double R, int poly_order)
        {
            // Local helper to build a failed result
            CTGResult Fail(string stage, string reason, int iters, double tfC, double fTf, double dfDtf, double det, double yNom, double yUsed, string log)
            {
                return new CTGResult {
                    converged = false, tf = tfC, tf_initial = tf0, iterations = iters,
                    last_f_tf = fTf, last_df_dtf = dfDtf, last_det = det,
                    y_f_nominal = yNom, y_f_used = yUsed, R_effective = R,
                    stage = stage, null_reason = reason, iteration_log = log ?? ""
                };
            }

            if (!IsFinite(tf0) || !IsFinite(alpha) || !IsFinite(T) || !IsFinite(Ve) ||
                tf0 <= 0 || alpha <= 0 || T <= 0 || Ve <= 0)
                return Fail("input", "invalid scalar input", 0, tf0, 0, 0, 0, 0, 0, "");

            double tf = Math.Min(tf0, alpha - 1e-3);
            if (tf <= 1e-3) return Fail("input", "tf <= 1e-3", 0, tf, 0, 0, 0, 0, 0, "");

            double[]   rho      = new double[poly_order + 1]; rho[0] = 1.0;
            Vector3d[] g_coeffs = new Vector3d[poly_order + 1];
            double     lam2 = 0, lam3 = 0, C2 = 0, C3 = 0;
            double     dx_dT_total = 0;
            StringBuilder iterSb = new StringBuilder();

            for (int j = 0; j < 50; j++)
            {
                DbgInnerIter = j + 1;
                double tf_old = tf;
                DbgTfLast     = tf;

                GetBaseIntegrals(tf, Ve, alpha, T, poly_order + 1,
                    out double[] Ib, out double[] Jb, out double[] Is, out double[] Js);
                Vector3d dv_g = CalcGravVel(tf, g_coeffs, poly_order);
                Vector3d dr_g = CalcGravPos(tf, g_coeffs, poly_order);

                double Ir0 = 0, Ir1 = 0, Jr0 = 0, Jr1 = 0;
                for (int i = 0; i <= poly_order; i++)
                {
                    Ir0 += rho[i] * Ib[i]; Ir1 += rho[i] * Ib[i + 1];
                    Jr0 += rho[i] * Jb[i]; Jr1 += rho[i] * Jb[i + 1];
                }

                double x_f = r0.x + v0.x * tf + dr_g.x - Jr0;
                double z_f = 0.0;
                double r_horiz_sq = x_f * x_f + z_f * z_f;

                _dbgYfNominal = r_horiz_sq < R * R
                    ?  Math.Sqrt(R * R - r_horiz_sq)
                    : -Math.Sqrt(r_horiz_sq - R * R);
                _dbgYfUsed = _dbgYfNominal;

                Vector3d V_diff = vf - v0 - dv_g;
                double   Y      = _dbgYfUsed - r0.y - v0.y * tf - dr_g.y;
                double   Z      = z_f        - r0.z - v0.z * tf - dr_g.z;

                double det = (Jr1 * Ir0) - (Ir1 * Jr0);
                _dbgDet = det;
                if (!IsFinite(det) || Math.Abs(det) < 1e-12)
                    return Fail("matrix", "det too small", j + 1, tf, 0, 0, det, _dbgYfNominal, _dbgYfUsed, iterSb.ToString());

                lam2 = (Jr0 * V_diff.y - Ir0 * Y) / det;
                C2   = (Jr1 * V_diff.y - Ir1 * Y) / det;
                lam3 = (Jr0 * V_diff.z - Ir0 * Z) / det;
                C3   = (Jr1 * V_diff.z - Ir1 * Z) / det;

                int    n_samples = 12;
                double[] t_samp  = new double[n_samples];
                for (int i = 0; i < n_samples; i++) t_samp[i] = tf * i / (n_samples - 1.0);

                double rho_0 = 1.0 / Math.Sqrt(1.0 + C2 * C2 + C3 * C3);
                rho[0] = rho_0;

                if (poly_order > 0)
                {
                    double[] t_nonzero = new double[n_samples - 1];
                    double[] tgt_shifted = new double[n_samples - 1];
                    for (int i = 1; i < n_samples; i++)
                    {
                        t_nonzero[i - 1]  = t_samp[i];
                        double lam4_bar   = 1.0 / Math.Sqrt(1.0
                            + Math.Pow(-lam2 * t_samp[i] + C2, 2)
                            + Math.Pow(-lam3 * t_samp[i] + C3, 2));
                        tgt_shifted[i - 1] = (lam4_bar - rho_0) / t_samp[i];
                    }
                    double[] rho_rest = ManualPolyFit(t_nonzero, tgt_shifted, poly_order - 1);
                    if (rho_rest == null)
                        return Fail("rho", "polyfit rho failed", j + 1, tf, 0, 0, det, _dbgYfNominal, _dbgYfUsed, iterSb.ToString());
                    for (int i = 0; i < poly_order; i++) rho[i + 1] = rho_rest[i];
                }

                double[] gx = new double[n_samples], gy = new double[n_samples], gz = new double[n_samples];
                for (int i = 0; i < n_samples; i++)
                {
                    Vector3d rt = PredictPositionAtT(t_samp[i], r0, v0, rho, g_coeffs, lam2, C2, lam3, C3, Ve, alpha, T);
                    Vector3d g  = -(mu / Math.Pow(Math.Max(1.0, rt.magnitude), 3)) * rt;
                    gx[i] = g.x; gy[i] = g.y; gz[i] = g.z;
                }

                double[] gxFit = ManualPolyFit(t_samp, gx, poly_order);
                double[] gyFit = ManualPolyFit(t_samp, gy, poly_order);
                double[] gzFit = ManualPolyFit(t_samp, gz, poly_order);
                if (gxFit == null || gyFit == null || gzFit == null)
                    return Fail("gravity", "polyfit g failed", j + 1, tf, 0, 0, det, _dbgYfNominal, _dbgYfUsed, iterSb.ToString());
                for (int i = 0; i <= poly_order; i++) g_coeffs[i] = new Vector3d(gxFit[i], gyFit[i], gzFit[i]);

                Vector3d newDvg  = CalcGravVel(tf, g_coeffs, poly_order);
                double   newVx   = vf.x - v0.x - newDvg.x;
                double   newIr0  = 0;
                for (int i = 0; i <= poly_order; i++) newIr0 += rho[i] * Ib[i];

                double f_tf   = newVx + newIr0;
                double tau_tf = Ve / Math.Max(1e-6, alpha - tf);
                double rho_tf = 0, gx_tf = 0;
                for (int i = 0; i <= poly_order; i++)
                {
                    rho_tf += rho[i] * Math.Pow(tf, i);
                    gx_tf  += g_coeffs[i].x * Math.Pow(tf, i);
                }
                double df_dtf = tau_tf * rho_tf - gx_tf;

                _dbgLastFTf  = f_tf; _dbgLastDfDtf = df_dtf;
                iterSb.AppendLine(string.Format("j={0} tf={1:F6} f={2:E3} df={3:E3}", j, tf, f_tf, df_dtf));

                if (!IsFinite(df_dtf) || Math.Abs(df_dtf) < 1e-12)
                    return Fail("newton", "df_dtf too small", j + 1, tf, f_tf, df_dtf, det, _dbgYfNominal, _dbgYfUsed, iterSb.ToString());

                double tf_new = tf - (f_tf / df_dtf);
                if (!IsFinite(tf_new))
                    return Fail("newton", "tf_new non-finite", j + 1, tf, f_tf, df_dtf, det, _dbgYfNominal, _dbgYfUsed, iterSb.ToString());

                tf_new        = Math.Max(1e-3, Math.Min(alpha - 1e-3, tf_new));
                double dt_tf  = tf_new - tf_old;
                tf            = tf_new;
                DbgTfLast     = tf;

                if (Math.Abs(dt_tf) < _dt_tol && Math.Abs(f_tf) < _f_tf_tol)
                {
                    double Sv = 0, Sr = 0;
                    for (int i = 0; i <= poly_order; i++) { Sv += rho[i] * Is[i]; Sr -= rho[i] * Js[i]; }
                    double dtf_dT   = -Sv / df_dtf;
                    dx_dT_total     = Sr + vf.x * dtf_dT;

                    return new CTGResult {
                        tf = tf, rho = rho, g_coeffs = g_coeffs,
                        lam2 = lam2, lam3 = lam3, C2 = C2, C3 = C3,
                        x_f = r0.x + v0.x * tf + dr_g.x - Jr0,
                        dx_dT = dx_dT_total, T_mag = T, converged = true, iterations = j + 1,
                        tf_initial = tf0, last_f_tf = f_tf, last_df_dtf = df_dtf, last_det = det,
                        y_f_nominal = _dbgYfNominal, y_f_used = _dbgYfUsed, R_effective = R,
                        stage = "converged", null_reason = "", iteration_log = iterSb.ToString()
                    };
                }
            }
            return Fail("iterate", "max iterations", DbgInnerIter, tf, _dbgLastFTf, _dbgLastDfDtf, _dbgDet, _dbgYfNominal, _dbgYfUsed, iterSb.ToString());
        }

        #endregion

        #region Integral and polynomial helpers

        private void GetBaseIntegrals(double t, double Ve, double alpha, double T, int max_order,
            out double[] Ib, out double[] Jb, out double[] Is, out double[] Js)
        {
            Ib = new double[max_order + 2]; Is = new double[max_order + 2];
            Jb = new double[max_order + 1]; Js = new double[max_order + 1];

            double denom = Math.Max(0.01, alpha - t);
            Ib[0] = Ve * Math.Log(alpha / denom);
            Is[0] = (Ve * t) / (T * denom);

            for (int n = 1; n < max_order + 2; n++)
            {
                Ib[n] = alpha * Ib[n - 1] - (Ve * Math.Pow(t, n) / n);
                Is[n] = alpha * Is[n - 1] - (alpha / T) * Ib[n - 1];
            }
            for (int n = 0; n < max_order + 1; n++)
            {
                Jb[n] = t * Ib[n] - Ib[n + 1];
                Js[n] = t * Is[n] - Is[n + 1];
            }
        }

        private double[] ManualPolyFit(double[] xData, double[] yData, int order)
        {
            if (xData == null || yData == null || xData.Length != yData.Length ||
                xData.Length == 0 || order < 0) return null;

            int n = xData.Length, nv = order + 1;
            double xMax = 0.0;
            for (int k = 0; k < n; k++) if (Math.Abs(xData[k]) > xMax) xMax = Math.Abs(xData[k]);
            if (xMax < 1e-12) xMax = 1.0;

            double[] xn = new double[n];
            for (int k = 0; k < n; k++) xn[k] = xData[k] / xMax;

            double[,] A = new double[nv, nv];
            double[]  B = new double[nv];
            for (int i = 0; i < nv; i++)
            {
                for (int j = 0; j < nv; j++) { double s = 0; for (int k = 0; k < n; k++) s += Math.Pow(xn[k], i + j); A[i, j] = s; }
                double sxy = 0; for (int k = 0; k < n; k++) sxy += yData[k] * Math.Pow(xn[k], i); B[i] = sxy;
            }
            for (int i = 0; i < nv; i++)
            {
                int pRow = i; double pAbs = Math.Abs(A[i, i]);
                for (int k = i + 1; k < nv; k++) { double c = Math.Abs(A[k, i]); if (c > pAbs) { pAbs = c; pRow = k; } }
                if (pAbs < 1e-25) return null;
                if (pRow != i)
                {
                    for (int j = i; j < nv; j++) { double tmp = A[i, j]; A[i, j] = A[pRow, j]; A[pRow, j] = tmp; }
                    double tmpB = B[i]; B[i] = B[pRow]; B[pRow] = tmpB;
                }
                for (int k = i + 1; k < nv; k++)
                {
                    double fac = A[k, i] / A[i, i];
                    for (int j = i; j < nv; j++) A[k, j] -= fac * A[i, j];
                    B[k] -= fac * B[i];
                }
            }
            double[] bc = new double[nv];
            for (int i = nv - 1; i >= 0; i--)
            {
                double sum = 0; for (int j = i + 1; j < nv; j++) sum += A[i, j] * bc[j];
                if (Math.Abs(A[i, i]) < 1e-25) return null;
                bc[i] = (B[i] - sum) / A[i, i];
            }
            double[] ac = new double[nv];
            for (int i = 0; i < nv; i++) ac[i] = bc[i] / Math.Pow(xMax, i);
            return ac;
        }

        private Vector3d PredictPositionAtT(double t, Vector3d r0, Vector3d v0, double[] rho,
            Vector3d[] g_coeffs, double lam2, double C2, double lam3, double C3, double Ve, double alpha, double T)
        {
            int poly_order = rho.Length - 1;
            Vector3d dr_gt = CalcGravPos(t, g_coeffs, poly_order);
            GetBaseIntegrals(t, Ve, alpha, T, poly_order + 1, out _, out double[] J_base_t, out _, out _);
            double Jr0_t = 0, Jr1_t = 0;
            for (int i = 0; i <= poly_order; i++) { Jr0_t += rho[i] * J_base_t[i]; Jr1_t += rho[i] * J_base_t[i + 1]; }
            return new Vector3d(
                r0.x + v0.x * t + dr_gt.x - Jr0_t,
                r0.y + v0.y * t + dr_gt.y + (-lam2 * Jr1_t + C2 * Jr0_t),
                r0.z + v0.z * t + dr_gt.z + (-lam3 * Jr1_t + C3 * Jr0_t));
        }

        private Vector3d CalcGravVel(double tf, Vector3d[] g, int p)
        {
            Vector3d v = Vector3d.zero;
            for (int n = 0; n <= p; n++) v += g[n] * (Math.Pow(tf, n + 1) / (n + 1));
            return v;
        }

        private Vector3d CalcGravPos(double tf, Vector3d[] g, int p)
        {
            Vector3d r = Vector3d.zero;
            for (int n = 0; n <= p; n++) r += g[n] * (Math.Pow(tf, n + 2) / ((n + 1) * (n + 2)));
            return r;
        }

        #endregion

        
    }
}