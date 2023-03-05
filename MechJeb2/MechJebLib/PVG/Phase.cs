/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using System;
using System.Text;
using MechJebLib.Primitives;
using MechJebLib.PVG.Integrators;
using MechJebLib.Utils;
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
        public double         maxt;
        public double         mint;
        public double         ve;
        public double         a0;
        public double         tau;
        public double         mdot;
        public double         dv;
        public double         DropMass = 0.0; // FIXME: unused
        public bool           OptimizeTime;
        public bool           Infinite = false;
        public bool           Unguided;
        public bool           MassContinuity = false;
        public bool           LastFreeBurn   = false;
        public bool           Normalized     = false;
        public int            KSPStage;
        public IPVGIntegrator Integrator;
        public bool           Coast => thrust == 0;
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

        public Phase Rescale(Scale scale)
        {
            Check.False(Normalized);

            var phase = (Phase)this.MemberwiseClone();
            
            phase.ve         = ve / scale.velocityScale;
            phase.tau        = tau / scale.timeScale;
            phase.mdot       = mdot / scale.mdotScale;
            phase.thrust     = thrust / scale.forceScale;
            phase.bt         = bt / scale.timeScale;
            phase.mint       = mint / scale.timeScale;
            phase.maxt       = maxt / scale.timeScale;
            phase.m0         = m0 / scale.massScale;
            phase.mf         = mf / scale.massScale;
            phase.DropMass   = DropMass / scale.massScale;
            phase.Normalized = true;

            return phase;
        }

        public void Integrate(DD y0, DD yf, double t0, double tf)
        {
            Integrator.Integrate(y0, yf, this, t0, tf);
        }

        public void Integrate(DD y0, DD yf, double t0, double tf, Solution solution)
        {
            Integrator.Integrate(y0, yf, this, t0, tf, solution);
        }

        public static Phase NewStageUsingFinalMass(double m0, double mf, double isp, double bt, int kspStage, bool optimizeTime = false, bool unguided = false)
        {
            Check.PositiveFinite(m0);
            Check.PositiveFinite(mf);
            Check.PositiveFinite(isp);
            Check.PositiveFinite(bt);
            
            double mdot = (m0 - mf) / bt;
            double thrust = mdot * (isp * G0);
            
            Check.PositiveFinite(mdot);
            Check.PositiveFinite(thrust);
            
            var phase = new Phase(m0, thrust, isp, mf, bt, kspStage) { OptimizeTime = optimizeTime, Unguided = unguided };

            return phase;
        }
        
        public static Phase NewStageUsingThrust(double m0, double thrust, double isp, double bt, int kspStage, bool optimizeTime = false, bool unguided = false)
        {
            Check.PositiveFinite(m0);
            Check.PositiveFinite(thrust);
            Check.PositiveFinite(isp);
            Check.PositiveFinite(bt);
            
            double mdot = thrust / (isp * G0);
            double mf = m0 - mdot * bt;
            
            Check.PositiveFinite(mdot);
            Check.PositiveFinite(mf);
            
            var phase = new Phase(m0, thrust, isp, mf, bt, kspStage) { OptimizeTime = optimizeTime, Unguided = unguided };

            return phase;
        }

        public static Phase NewFixedCoast(double m0, double ct, int kspStage)
        {
            return new Phase(m0, 0, 0, m0, ct, kspStage);
        }
        
        public static Phase NewOptimizedCoast(double m0, double mint, double maxt, int kspStage)
        {
            var phase = new Phase(m0, 0, 0, m0, mint, kspStage) { OptimizeTime = true, mint = mint, maxt = maxt };
            return phase;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"stage: {KSPStage} m0: {m0} mf: {mf} thrust: {thrust} bt: {bt} isp: {isp} mdot: {mdot}");
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
