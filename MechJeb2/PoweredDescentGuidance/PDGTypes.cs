// PDGTypes.cs
// Shared data types for the Powered Descent Guidance (PDG) system.
// All types here are plain data containers with no game-logic dependencies.

using UnityEngine;

namespace MuMech.Landing
{
    // -------------------------------------------------------------------------
    // Solver result
    // -------------------------------------------------------------------------

    /// <summary>
    /// Output from <see cref="PdgSolver.Solve"/> or <see cref="PdgSolver.Iterate"/>.
    /// Contains both the trajectory solution and full diagnostic state for telemetry.
    /// </summary>
    internal class CTGResult
    {
        // --- Solution ---

        /// <summary>Converged time-of-flight [s].</summary>
        public double tf;

        /// <summary>Commanded thrust magnitude [N].</summary>
        public double T_mag;

        /// <summary>Predicted downrange landing position [m].</summary>
        public double x_f;

        /// <summary>Sensitivity dx/dT used by the outer loop [m/N].</summary>
        public double dx_dT;

        /// <summary>Thrust-to-mass polynomial coefficients.</summary>
        public double[] rho;

        /// <summary>Polynomial gravity-field approximation coefficients.</summary>
        public Vector3d[] g_coeffs;

        /// <summary>Optimal co-state multipliers for the lateral axes.</summary>
        public double lam2, lam3, C2, C3;

        /// <summary>Whether the Newton iteration converged.</summary>
        public bool converged;

        // --- Diagnostics ---

        /// <summary>Number of inner Newton iterations performed.</summary>
        public int iterations;

        /// <summary>Number of outer thrust-adjustment iterations performed.</summary>
        public int outerIterations;

        /// <summary>Initial tf warm-start that was passed in.</summary>
        public double tf_initial;

        /// <summary>Final residual of the tf Newton equation.</summary>
        public double last_f_tf;

        /// <summary>Final derivative of the tf Newton equation.</summary>
        public double last_df_dtf;

        /// <summary>Last computed matrix determinant (used for diagnosing singularities).</summary>
        public double last_det;

        /// <summary>Nominal radial position at tf (signed; negative means below-surface).</summary>
        public double y_f_nominal;

        /// <summary>Radial position actually used in the solver.</summary>
        public double y_f_used;

        /// <summary>Effective target radius used in this solve [m].</summary>
        public double R_effective;

        /// <summary>Named stage at which the solver stopped ("converged", "matrix", "newton", etc.).</summary>
        public string stage;

        /// <summary>Human-readable failure reason (empty on convergence).</summary>
        public string null_reason;

        /// <summary>Per-iteration log string for deep debugging.</summary>
        public string iteration_log;
    }

    // -------------------------------------------------------------------------
    // Terminal guidance outputs
    // -------------------------------------------------------------------------

    /// <summary>Output from <see cref="PdgTerminalSolver.ComputeApolloZeroJerkCommand"/>.</summary>
    internal struct ApolloGuidanceOutput
    {
        public bool     valid;
        public string   reason;
        public double   tgo;
        public double   requiredThrust;
        public Vector3d accelCmdWorld;
        public Vector3d thrustAccelWorld;
        public Vector3d thrustDirWorld;
        public float    throttle;
    }

    /// <summary>Output from <see cref="PdgTerminalSolver.ComputeGravityTurnCommand"/>.</summary>
    internal struct GTGuidanceOutput
    {
        public bool     valid;
        public string   reason;
        public double   tgo;
        public double   beta;
        public double   gammaStar;
        public Vector3d thrustDirWorld;
        public double   requiredThrust;
        public float    throttle;
    }

    // -------------------------------------------------------------------------
    // Engine state snapshot
    // -------------------------------------------------------------------------

    /// <summary>
    /// Aggregated propulsion state for the current vehicle, produced by
    /// <see cref="PDGEngine.ReadEngineState"/>.
    /// </summary>
    internal class PDGEngineState
    {
        public bool   Valid;
        public string FailureReason;
        public double Mass;
        public double RawMaxThrust;
        public double AvailableThrust;
        public double EffectiveExhaustVelocity;
        public double MassFlow;
    }
}