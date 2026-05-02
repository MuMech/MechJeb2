// PdgSolver.cs
// Implementation of DCTGIG (Dynamic constant thrust general integral guidance) solver.
// Zhang et al. 2025, DOI: 10.1016/j.cja.2025.104030.
// 
// DCTGIG overview:
//   The inner CTGIG loop uses predictor-corrector / Newton iteration on the
//   time of flight (tf). For each tf guess, it fits polynomial approximations
//   (least-squares fit) for the thrust-direction profile and central-gravity field, 
//   evaluates the thrust and gravity integrals analytically, and iterates until 
//   the terminal velocity and target-radius constraints close. This produces a predicted
//   terminal downrange for the current constant-thrust assumption.
//
//   The outer DCTGIG loop treats the remaining-flight thrust magnitude as the
//   correction parameter. It updates that constant thrust level and reruns CTGIG
//   until the predicted downrange matches the target.
//
//   The result is an indirect, computationally efficient powered-descent
//   guidance law with analytical state prediction and iterative correction,
//   with propellant consumption reported close to optimal in the reference paper.
//
// Intentionally free of KSP runtime dependencies for modularity.

using System;
using System.Text;
using UnityEngine;

namespace MuMech.Landing
{
    /// <summary>
    /// DCTGIG powered-descent solver.
    /// </summary>
    internal static class PdgSolver
    {
        // Convergence tolerances for the inner Newton step on tf.
        private const double DtTol  = 1e-2;
        private const double FTfTol = 1e-2;

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        /// <summary>
        /// Outer DCTGIG loop: adjusts thrust magnitude T_mag to drive the predicted
        /// downrange landing position toward <paramref name="targetX"/>.
        /// </summary>
        /// <param name="r0">Initial position in the solver local frame [m].</param>
        /// <param name="v0">Initial velocity in the solver local frame [m/s].</param>
        /// <param name="vf">Desired final velocity in the solver local frame [m/s].</param>
        /// <param name="targetX">Desired downrange landing distance [m].</param>
        /// <param name="m0">Vehicle mass at the start of the burn [kg].</param>
        /// <param name="currentTf">Warm-start time-of-flight [s]; pass 0 to auto-initialise.</param>
        /// <param name="ve">Effective exhaust velocity [m/s].</param>
        /// <param name="availableThrust">Maximum available thrust [N].</param>
        /// <param name="planThrottle">Nominal throttle fraction (0–1) used to seed T_mag.</param>
        /// <param name="mu">Gravitational parameter of the central body [m³/s²].</param>
        /// <param name="R">Target radial distance from the body centre [m].</param>
        /// <param name="polyOrder">Polynomial order for gravity / rho approximations (2–4 recommended).</param>
        /// <returns>
        /// Converged <see cref="PdgSolverResult"/> on success, or <c>null</c> if the solver
        /// failed to converge within the outer iteration budget.
        /// </returns>
        public static PdgSolverResult Solve(
            Vector3d r0, Vector3d v0, Vector3d vf,
            double targetX, double m0, double currentTf,
            double thrustGuess,
            double ve, double availableThrust, double planThrottle,
            double mu, double R, int polyOrder)
        {
            double tSeed = thrustGuess > 0.0 && IsFinite(thrustGuess)
                ? thrustGuess
                : availableThrust * planThrottle;

            double tMag = Math.Max(availableThrust * 0.1, Math.Min(availableThrust, tSeed));

            double   tf   = currentTf > 0 ? currentTf : 1.0;
            PdgSolverResult best = null;

            for (int k = 0; k < 10; k++)
            {
                double alpha  = (m0 * ve) / Math.Max(1.0, tMag);
                PdgSolverResult res = IterateCore(tf, r0, v0, vf, ve, alpha, tMag, mu, R, polyOrder);

                if (res == null || !res.converged || !IsFinite(res.x_f) || !IsFinite(res.dx_dT))
                    break;

                tf           = res.tf;
                best         = res;
                best.T_mag   = tMag;

                double err = res.x_f - targetX;
                if (Math.Abs(err) < 0.1) break;

                // Clamp T changes to ±1 % per outer step to keep throttle smooth.
                double dT = Math.Max(tMag * -0.01, Math.Min(tMag * 0.01, err / res.dx_dT));
                tMag      = Math.Max(availableThrust * 0.1, Math.Min(availableThrust, tMag - dT));

                best.outerIterations = k + 1;
            }

            if (best != null && best.converged)
                return best;

            return null;
        }

        /// <summary>
        /// Runs a single inner CTG Newton iteration from a given tf0 and thrust magnitude.
        /// Used directly for coast-phase downrange probing as well as inside
        /// <see cref="Solve"/>.
        /// </summary>
        /// <param name="tf0">Initial time-of-flight guess [s].</param>
        /// <param name="r0">Position in solver frame [m].</param>
        /// <param name="v0">Velocity in solver frame [m/s].</param>
        /// <param name="vf">Desired final velocity in solver frame [m/s].</param>
        /// <param name="ve">Effective exhaust velocity [m/s].</param>
        /// <param name="alpha">Thrust time constant m0·ve / T [s].</param>
        /// <param name="thrust">Thrust magnitude [N].</param>
        /// <param name="mu">Gravitational parameter [m³/s²].</param>
        /// <param name="R">Target radial distance [m].</param>
        /// <param name="polyOrder">Polynomial order for approximations.</param>
        public static PdgSolverResult Iterate(
            double tf0, Vector3d r0, Vector3d v0, Vector3d vf,
            double ve, double alpha, double thrust,
            double mu, double R, int polyOrder)
            => IterateCore(tf0, r0, v0, vf, ve, alpha, thrust, mu, R, polyOrder);

        // -------------------------------------------------------------------------
        // Inner Newton loop
        // -------------------------------------------------------------------------

        private static PdgSolverResult IterateCore(
            double tf0, Vector3d r0, Vector3d v0, Vector3d vf,
            double ve, double alpha, double T, double mu, double R, int polyOrder)
        {
            // Local helper — builds a failed result without allocating multiple times.
            PdgSolverResult Fail(string stage, string reason, int iters, double tfC,
                           double fTf, double dfDtf, double det, double yNom, double yUsed,
                           string log) => new PdgSolverResult
            {
                converged     = false, tf          = tfC,  tf_initial    = tf0,
                iterations    = iters, last_f_tf   = fTf,  last_df_dtf   = dfDtf,
                last_det      = det,   y_f_nominal = yNom, y_f_used      = yUsed,
                R_effective   = R,     stage        = stage, null_reason  = reason,
                iteration_log = log ?? ""
            };

            if (!IsFinite(tf0) || !IsFinite(alpha) || !IsFinite(T) || !IsFinite(ve) ||
                tf0 <= 0 || alpha <= 0 || T <= 0 || ve <= 0)
                return Fail("input", "invalid scalar input", 0, tf0, 0, 0, 0, 0, 0, "");

            double tf = Math.Min(tf0, alpha - 1e-3);
            if (tf <= 1e-3)
                return Fail("input", "tf <= 1e-3", 0, tf, 0, 0, 0, 0, 0, "");

            double[]   rho     = new double[polyOrder + 1]; rho[0] = 1.0;
            Vector3d[] gCoeffs = new Vector3d[polyOrder + 1];
            double     lam2 = 0, lam3 = 0, C2 = 0, C3 = 0;

            // Debug scratch — final values are reported in the PdgSolverResult.
            double detDbg = 0, yNomDbg = 0, yUsedDbg = 0, fTfDbg = 0, dfDtfDbg = 0;
            StringBuilder iterSb = new StringBuilder();

            for (int j = 0; j < 50; j++)
            {
                double tfOld = tf;

                GetBaseIntegrals(tf, ve, alpha, T, polyOrder + 1,
                    out double[] Ib, out double[] Jb, out double[] Is, out double[] Js);

                Vector3d dvG = CalcGravVel(tf, gCoeffs, polyOrder);
                Vector3d drG = CalcGravPos(tf, gCoeffs, polyOrder);

                double ir0 = 0, ir1 = 0, jr0 = 0, jr1 = 0;
                for (int i = 0; i <= polyOrder; i++)
                {
                    ir0 += rho[i] * Ib[i];     ir1 += rho[i] * Ib[i + 1];
                    jr0 += rho[i] * Jb[i];     jr1 += rho[i] * Jb[i + 1];
                }

                // Predicted terminal horizontal distance from origin.
                double xF       = r0.x + v0.x * tf + drG.x - jr0;
                double rHorizSq = xF * xF; // z_f = 0 by construction

                yNomDbg = rHorizSq < R * R
                    ?  Math.Sqrt(R * R - rHorizSq)
                    : -Math.Sqrt(rHorizSq - R * R);
                yUsedDbg = yNomDbg;

                Vector3d vDiff = vf - v0 - dvG;
                double   Y     = yUsedDbg - r0.y - v0.y * tf - drG.y;
                double   Z     = -r0.z    - v0.z * tf - drG.z; // z_f = 0

                detDbg = jr1 * ir0 - ir1 * jr0;
                if (!IsFinite(detDbg) || Math.Abs(detDbg) < 1e-12)
                    return Fail("matrix", "det too small", j + 1, tf, 0, 0, detDbg,
                                yNomDbg, yUsedDbg, iterSb.ToString());

                lam2 = (jr0 * vDiff.y - ir0 * Y) / detDbg;
                C2   = (jr1 * vDiff.y - ir1 * Y) / detDbg;
                lam3 = (jr0 * vDiff.z - ir0 * Z) / detDbg;
                C3   = (jr1 * vDiff.z - ir1 * Z) / detDbg;

                // --- Fit thrust-profile polynomial rho(t) ---

                const int nSamples = 12;
                double[] tSamp = new double[nSamples];
                for (int i = 0; i < nSamples; i++) tSamp[i] = tf * i / (nSamples - 1.0);

                double rho0 = 1.0 / Math.Sqrt(1.0 + C2 * C2 + C3 * C3);
                rho[0] = rho0;

                if (polyOrder > 0)
                {
                    double[] tNonzero   = new double[nSamples - 1];
                    double[] tgtShifted = new double[nSamples - 1];
                    for (int i = 1; i < nSamples; i++)
                    {
                        tNonzero[i - 1] = tSamp[i];
                        double lam4Bar  = 1.0 / Math.Sqrt(1.0
                            + (-lam2 * tSamp[i] + C2) * (-lam2 * tSamp[i] + C2)
                            + (-lam3 * tSamp[i] + C3) * (-lam3 * tSamp[i] + C3));
                        tgtShifted[i - 1] = (lam4Bar - rho0) / tSamp[i];
                    }
                    double[] rhoRest = PolynomialFit(tNonzero, tgtShifted, polyOrder - 1);
                    if (rhoRest == null)
                        return Fail("rho", "polyfit rho failed", j + 1, tf, 0, 0, detDbg,
                                    yNomDbg, yUsedDbg, iterSb.ToString());
                    for (int i = 0; i < polyOrder; i++) rho[i + 1] = rhoRest[i];
                }

                // --- Sample and fit gravity along the predicted trajectory ---

                double[] gx = new double[nSamples],
                         gy = new double[nSamples],
                         gz = new double[nSamples];
                for (int i = 0; i < nSamples; i++)
                {
                    Vector3d rt = PredictPositionAtT(
                        tSamp[i], r0, v0, rho, gCoeffs, lam2, C2, lam3, C3, ve, alpha, T);
                    double rm = Math.Max(1.0, rt.magnitude);
                    Vector3d g = -(mu / (rm * rm * rm)) * rt;
                    gx[i] = g.x; gy[i] = g.y; gz[i] = g.z;
                }

                double[] gxFit = PolynomialFit(tSamp, gx, polyOrder);
                double[] gyFit = PolynomialFit(tSamp, gy, polyOrder);
                double[] gzFit = PolynomialFit(tSamp, gz, polyOrder);
                if (gxFit == null || gyFit == null || gzFit == null)
                    return Fail("gravity", "polyfit g failed", j + 1, tf, 0, 0, detDbg,
                                yNomDbg, yUsedDbg, iterSb.ToString());
                for (int i = 0; i <= polyOrder; i++)
                    gCoeffs[i] = new Vector3d(gxFit[i], gyFit[i], gzFit[i]);

                // --- Newton step on tf ---

                Vector3d newDvg = CalcGravVel(tf, gCoeffs, polyOrder);
                double   newIr0 = 0;
                for (int i = 0; i <= polyOrder; i++) newIr0 += rho[i] * Ib[i];

                double fTf   = (vf.x - v0.x - newDvg.x) + newIr0;
                double tauTf = ve / Math.Max(1e-6, alpha - tf);
                double rhoTf = 0, gxTf = 0;
                for (int i = 0; i <= polyOrder; i++)
                {
                    rhoTf += rho[i] * Math.Pow(tf, i);
                    gxTf  += gCoeffs[i].x * Math.Pow(tf, i);
                }
                double dfDtf = tauTf * rhoTf - gxTf;

                fTfDbg   = fTf;
                dfDtfDbg = dfDtf;
                iterSb.AppendLine($"j={j} tf={tf:F6} f={fTf:E3} df={dfDtf:E3}");

                if (!IsFinite(dfDtf) || Math.Abs(dfDtf) < 1e-12)
                    return Fail("newton", "df_dtf too small", j + 1, tf, fTf, dfDtf, detDbg,
                                yNomDbg, yUsedDbg, iterSb.ToString());

                double tfNew = tf - fTf / dfDtf;
                if (!IsFinite(tfNew))
                    return Fail("newton", "tf_new non-finite", j + 1, tf, fTf, dfDtf, detDbg,
                                yNomDbg, yUsedDbg, iterSb.ToString());

                tfNew = Math.Max(1e-3, Math.Min(alpha - 1e-3, tfNew));
                tf    = tfNew;

                // --- Convergence check ---

                if (Math.Abs(tfNew - tfOld) < DtTol && Math.Abs(fTf) < FTfTol)
                {
                    // Recompute integrals at the converged tf for an accurate x_f.
                    GetBaseIntegrals(tf, ve, alpha, T, polyOrder + 1,
                        out _, out double[] jbFinal, out double[] isFinal, out double[] jsFinal);

                    double jr0Final = 0;
                    for (int i = 0; i <= polyOrder; i++) jr0Final += rho[i] * jbFinal[i];
                    Vector3d drGFinal = CalcGravPos(tf, gCoeffs, polyOrder);

                    // Thrust-sensitivity used by the outer loop (dx/dT).
                    double sv = 0, sr = 0;
                    for (int i = 0; i <= polyOrder; i++) { sv += rho[i] * isFinal[i]; sr -= rho[i] * jsFinal[i]; }
                    double dtfDT  = -sv / dfDtf;
                    double dxDT   = sr + vf.x * dtfDT;

                    return new PdgSolverResult
                    {
                        converged     = true,
                        tf            = tf,
                        tf_initial    = tf0,
                        iterations    = j + 1,
                        rho           = rho,
                        g_coeffs      = gCoeffs,
                        lam2          = lam2,  lam3        = lam3,
                        C2            = C2,    C3          = C3,
                        x_f           = r0.x + v0.x * tf + drGFinal.x - jr0Final,
                        dx_dT         = dxDT,
                        T_mag         = T,
                        last_f_tf     = fTf,
                        last_df_dtf   = dfDtf,
                        last_det      = detDbg,
                        y_f_nominal   = yNomDbg,
                        y_f_used      = yUsedDbg,
                        R_effective   = R,
                        stage         = "converged",
                        null_reason   = "",
                        iteration_log = iterSb.ToString()
                    };
                }
            }

            return Fail("iterate", "max iterations reached", 50, tf,
                        fTfDbg, dfDtfDbg, detDbg, yNomDbg, yUsedDbg, iterSb.ToString());
        }

        // -------------------------------------------------------------------------
        // Integral basis functions
        // -------------------------------------------------------------------------

        /// <summary>
        /// Computes the basis integrals I_b, J_b (thrust-direction) and I_s, J_s
        /// (sensitivity) up to order <paramref name="maxOrder"/> at time <paramref name="t"/>.
        /// Jb[n] = t·Ib[n] − Ib[n+1], etc.
        /// </summary>
        private static void GetBaseIntegrals(
            double t, double ve, double alpha, double T, int maxOrder,
            out double[] Ib, out double[] Jb, out double[] Is, out double[] Js)
        {
            Ib = new double[maxOrder + 2]; Is = new double[maxOrder + 2];
            Jb = new double[maxOrder + 1]; Js = new double[maxOrder + 1];

            double denom = Math.Max(0.01, alpha - t);
            Ib[0] = ve * Math.Log(alpha / denom);
            Is[0] = ve * t / (T * denom);

            for (int n = 1; n < maxOrder + 2; n++)
            {
                Ib[n] = alpha * Ib[n - 1] - ve * Math.Pow(t, n) / n;
                Is[n] = alpha * Is[n - 1] - alpha / T * Ib[n - 1];
            }
            for (int n = 0; n < maxOrder + 1; n++)
            {
                Jb[n] = t * Ib[n] - Ib[n + 1];
                Js[n] = t * Is[n] - Is[n + 1];
            }
        }

        // -------------------------------------------------------------------------
        // Least-squares polynomial fit
        // -------------------------------------------------------------------------

        /// <summary>
        /// Fits a polynomial of degree <paramref name="order"/> to the supplied
        /// data using normal equations solved by Gaussian elimination with partial
        /// pivoting. x-data is normalised internally for numerical stability.
        /// Returns null if the system is singular.
        /// </summary>
        private static double[] PolynomialFit(double[] xData, double[] yData, int order)
        {
            if (xData == null || yData == null ||
                xData.Length != yData.Length || xData.Length == 0 || order < 0)
                return null;

            int n = xData.Length, nv = order + 1;

            // Normalise x to improve conditioning of the Vandermonde system.
            double xMax = 0.0;
            for (int k = 0; k < n; k++) xMax = Math.Max(xMax, Math.Abs(xData[k]));
            if (xMax < 1e-12) xMax = 1.0;

            double[] xn = new double[n];
            for (int k = 0; k < n; k++) xn[k] = xData[k] / xMax;

            // Build normal-equation matrix A and right-hand side B.
            double[,] A = new double[nv, nv];
            double[]  B = new double[nv];
            for (int i = 0; i < nv; i++)
            {
                for (int j = 0; j < nv; j++)
                {
                    double s = 0;
                    for (int k = 0; k < n; k++) s += Math.Pow(xn[k], i + j);
                    A[i, j] = s;
                }
                double sxy = 0;
                for (int k = 0; k < n; k++) sxy += yData[k] * Math.Pow(xn[k], i);
                B[i] = sxy;
            }

            // Gaussian elimination with partial pivoting.
            for (int i = 0; i < nv; i++)
            {
                int    pivRow = i;
                double pivAbs = Math.Abs(A[i, i]);
                for (int k = i + 1; k < nv; k++)
                {
                    double c = Math.Abs(A[k, i]);
                    if (c > pivAbs) { pivAbs = c; pivRow = k; }
                }
                if (pivAbs < 1e-25) return null;

                if (pivRow != i)
                {
                    for (int j = i; j < nv; j++) { double tmp = A[i, j]; A[i, j] = A[pivRow, j]; A[pivRow, j] = tmp; }
                    double tmpB = B[i]; B[i] = B[pivRow]; B[pivRow] = tmpB;
                }
                for (int k = i + 1; k < nv; k++)
                {
                    double fac = A[k, i] / A[i, i];
                    for (int j = i; j < nv; j++) A[k, j] -= fac * A[i, j];
                    B[k] -= fac * B[i];
                }
            }

            // Back-substitution.
            double[] bc = new double[nv];
            for (int i = nv - 1; i >= 0; i--)
            {
                double sum = 0;
                for (int j = i + 1; j < nv; j++) sum += A[i, j] * bc[j];
                if (Math.Abs(A[i, i]) < 1e-25) return null;
                bc[i] = (B[i] - sum) / A[i, i];
            }

            // Undo normalisation: coefficient of x^i acquires a factor of xMax^-i.
            double[] ac = new double[nv];
            for (int i = 0; i < nv; i++) ac[i] = bc[i] / Math.Pow(xMax, i);
            return ac;
        }

        // -------------------------------------------------------------------------
        // Trajectory prediction helpers
        // -------------------------------------------------------------------------

        /// <summary>
        /// Predicts the vehicle position at time <paramref name="t"/> along the
        /// guidance trajectory given the current polynomial fit state.
        /// </summary>
        private static Vector3d PredictPositionAtT(
            double t, Vector3d r0, Vector3d v0, double[] rho, Vector3d[] gCoeffs,
            double lam2, double C2, double lam3, double C3,
            double ve, double alpha, double T)
        {
            int      polyOrder = rho.Length - 1;
            Vector3d drGt      = CalcGravPos(t, gCoeffs, polyOrder);

            GetBaseIntegrals(t, ve, alpha, T, polyOrder + 1, out _, out double[] jBaseT, out _, out _);

            double jr0T = 0, jr1T = 0;
            for (int i = 0; i <= polyOrder; i++) { jr0T += rho[i] * jBaseT[i]; jr1T += rho[i] * jBaseT[i + 1]; }

            return new Vector3d(
                r0.x + v0.x * t + drGt.x - jr0T,
                r0.y + v0.y * t + drGt.y + (-lam2 * jr1T + C2 * jr0T),
                r0.z + v0.z * t + drGt.z + (-lam3 * jr1T + C3 * jr0T));
        }

        /// <summary>Velocity impulse due to the polynomial gravity field up to time <paramref name="tf"/>.</summary>
        private static Vector3d CalcGravVel(double tf, Vector3d[] g, int p)
        {
            Vector3d v = Vector3d.zero;
            for (int n = 0; n <= p; n++) v += g[n] * (Math.Pow(tf, n + 1) / (n + 1));
            return v;
        }

        /// <summary>Position offset due to the polynomial gravity field up to time <paramref name="tf"/>.</summary>
        private static Vector3d CalcGravPos(double tf, Vector3d[] g, int p)
        {
            Vector3d r = Vector3d.zero;
            for (int n = 0; n <= p; n++) r += g[n] * (Math.Pow(tf, n + 2) / ((n + 1) * (n + 2)));
            return r;
        }

        private static bool IsFinite(double v) => PDGMathUtils.IsFinite(v);
    }
}