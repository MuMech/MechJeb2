using UnityEngine;

namespace MuMech.Landing
{
    internal class CTGResult
    {
        public double tf;
        public double T_mag;
        public double x_f;
        public double dx_dT;
        public double[] rho;
        public Vector3d[] g_coeffs;
        public double lam2;
        public double lam3;
        public double C2;
        public double C3;
        public bool converged;
        public int iterations;
        public double tf_initial;
        public double last_f_tf;
        public double last_df_dtf;
        public double last_det;
        public double y_f_nominal;
        public double y_f_used;
        public double R_effective;
        public string stage;
        public string null_reason;
        public string iteration_log;
    }

    internal struct ApolloGuidanceOutput
    {
        public bool valid;
        public string reason;
        public double tgo;
        public double requiredThrust;
        public Vector3d accelCmdWorld;
        public Vector3d thrustAccelWorld;
        public Vector3d thrustDirWorld;
        public float throttle;
    }

    internal struct GTGuidanceOutput
    {
        public bool valid;
        public string reason;
        public double tgo;
        public double beta;
        public double gammaStar;
        public Vector3d thrustDirWorld;
        public double requiredThrust;
        public float throttle;
    }

    internal class PDGEngineState
    {
        public bool Valid;
        public string FailureReason;
        public double Mass;
        public double RawMaxThrust;
        public double AvailableThrust;
        public double EffectiveExhaustVelocity;
        public double MassFlow;
    }
}