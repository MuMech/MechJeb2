using System;
using UnityEngine;

namespace MuMech.Landing
{
    public partial class CTGIGLandingStep
    {
        private void UpdatePerformanceStats(Vector3d pos, double mass, double availThrust, double ve)
        {
            double r = Math.Max(1.0, pos.magnitude);
            double mu = MainBody.gravParameter;

            LocalG = mu / (r * r);

            AvailableTWR = availThrust > 1e-6 ? availThrust / (mass * LocalG) : 0.0;
            CurrentTWR = _current_T > 1e-6 ? _current_T / (mass * LocalG) : 0.0;
            DvUsed = (_guidanceStartMass > mass && mass > 1e-6 && ve > 1e-6)
                ? ve * Math.Log(_guidanceStartMass / mass)
                : 0.0;
        }

        private void ApplyDbgFromResult(CTGResult res)
        {
            if (res == null) return;

            DbgInnerIter = res.iterations;
            DbgTfInitial = res.tf_initial;
            DbgTfLast = res.tf;
            DbgFTf = res.last_f_tf;
            DbgDfDtf = res.last_df_dtf;
            DbgNullReason = res.null_reason ?? "";
            DbgStage = res.stage ?? "";
            DbgIterLog = res.iteration_log ?? "";
        }

        private void ResetInnerDebug(double tf0)
        {
            DbgInnerIter = 0;
            DbgTfInitial = tf0;
            DbgTfLast = tf0;
            DbgFTf = 0;
            DbgDfDtf = 0;
            _dbgDet = 0;
            _dbgYfNominal = 0;
            _dbgYfUsed = 0;
            DbgNullReason = "";
            DbgStage = "running";
            DbgIterLog = "";
        }
    }
}