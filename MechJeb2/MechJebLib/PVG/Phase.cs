/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using System;
using System.Text;
using MechJebLib.Primitives;
using MechJebLib.PVG.Integrators;
using static MechJebLib.Utils.Statics;

#nullable enable

namespace MechJebLib.PVG
{
    public class Phase
    {
        public double         m0;
        public double         thrust;
        public double         isp;
        public double         mf;
        public double         bt;
        public double         ve;
        public double         a0;
        public double         tau;
        public double         mdot;
        public double         dv;
        public double         c;
        public double         mdot_bar;
        public double         thrust_bar;
        public double         bt_bar;
        public double         m0_bar;
        public double         mf_bar;
        public double         ve_bar;
        public double         tau_bar;
        public double         DropMass = 0.0;
        public double         DropMassBar;
        public bool           OptimizeTime;
        public bool           Infinite = false;
        public bool           Unguided;
        public bool           MassContinuity = false;
        public bool           LastFreeBurn   = false;
        public int            KSPStage;
        public IPVGIntegrator Integrator;
        public bool           Coast => thrust_bar == 0;
        public V3             u0;

        private Phase(double m0, double thrust, double isp, double mf, double bt, int kspStage)
        {
            this.KSPStage = kspStage;
            this.m0       = m0;
            this.thrust   = thrust;
            this.isp      = isp;
            this.mf       = mf;
            this.bt       = bt;
            ve            = isp * G0;
            a0            = thrust / m0;
            tau           = thrust == 0 ? double.PositiveInfinity : ve / a0;
            mdot          = ve == 0 ? 0 : thrust / ve;
            dv            = thrust == 0 ? 0 : -ve * Math.Log(1 - bt / tau);
            OptimizeTime  = false;
        }

        public void Rescale(Scale scale)
        {
            c           = G0 * isp / scale.timeScale;
            ve_bar      = ve / scale.velocityScale;
            tau_bar     = tau / scale.timeScale;
            mdot_bar    = mdot / scale.mdotScale;
            thrust_bar  = thrust / scale.forceScale;
            bt_bar      = bt / scale.timeScale;
            m0_bar      = m0 / scale.massScale;
            mf_bar      = mf / scale.massScale;
            DropMassBar = DropMass / scale.massScale;
        }

        public void Integrate(DD y0, DD yf, double t0, double tf)
        {
            Integrator.Integrate(y0, yf, this, t0, tf);
        }

        public void Integrate(DD y0, DD yf, double t0, double tf, Solution solution)
        {
            Integrator.Integrate(y0, yf, this, t0, tf, solution);
        }

        public static Phase NewStageUsingBurnTime(double m0, double thrust, double isp, double bt, int kspStage, bool optimizeTime = false, bool unguided = false)
        {
            double mdot = thrust / (isp * G0);
            double mf = m0 - mdot * bt;
            var phase = new Phase(m0, thrust, isp, mf, bt, kspStage) { OptimizeTime = optimizeTime, Unguided = unguided };

            return phase;
        }

        public static Phase NewStageUsingFinalMass(double m0, double thrust, double isp, double mf, int kspStage, bool optimizeTime = false)
        {
            double mdot = thrust / (isp * G0);
            double bt = (m0 - mf) / mdot;

            var phase = new Phase(m0, thrust, isp, mf, bt, kspStage) { OptimizeTime = optimizeTime };
            return phase;
        }

        public static Phase NewCoast(double m0, double ct, int kspStage)
        {
            return new Phase(m0, 0, 0, m0, ct, kspStage);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"stage: {KSPStage} m0: {m0} bt: {bt}");
            if (OptimizeTime)
                sb.Append(" (optimized)");
            if (Infinite)
                sb.Append(" (infinite)");
            if (Unguided)
                sb.Append(" (unguided)");
            return sb.ToString();
        }
    }
}
