/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Text;
using MechJebLib.Primitives;
using MechJebLib.PVG.Integrators;
using MechJebLib.Utils;
using static MechJebLib.Utils.Statics;
using static System.Math;

namespace MechJebLib.PVG
{
    public class Phase
    {
        public          double m0;
        public          double thrust;
        public readonly double isp;
        public          double mf;
        public          double bt;
        public          double maxt;
        public          double mint;
        public          double ve;
        public          double c => ve; // alias for normalized ve
        public readonly double a0;
        public          double tau;
        public          double mdot;
        public          double dv;
        public          double DropMass; // FIXME: unused
        public          bool   OptimizeTime;
        public          bool   Infinite = false;

        private bool _analytic = true;

        public bool Analytic
        {
            get => _analytic;
            set
            {
                _ipvgIntegrator = null;
                _analytic       = value;
            }
        }

        public          bool Unguided;
        public readonly bool MassContinuity = false;
        public          bool BeforeCoast    = false;
        public          bool Normalized;
        public readonly int  KSPStage;
        public readonly int  MJPhase;

        private IPVGIntegrator? _ipvgIntegrator;

        private IPVGIntegrator _integrator => _ipvgIntegrator ??= GetIntegrator();

        private IPVGIntegrator GetIntegrator()
        {
            if (Coast)
                return new VacuumCoastAnalytic();

            //return new VacuumThrustIntegrator();
            return Analytic ? (IPVGIntegrator)new VacuumThrustAnalytic() : new VacuumThrustIntegrator();
        }

        public bool Coast => thrust == 0;
        public V3   u0;

        public Phase DeepCopy()
        {
            var newphase = (Phase)MemberwiseClone();

            return newphase;
        }

        private Phase(double m0, double thrust, double isp, double mf, double bt, int kspStage, int mjPhase)
        {
            KSPStage     = kspStage;
            MJPhase      = mjPhase;
            this.m0      = m0;
            this.thrust  = thrust;
            this.isp     = isp;
            this.mf      = mf;
            this.bt      = bt;
            ve           = isp * G0;
            a0           = thrust / m0;
            tau          = thrust == 0 ? double.PositiveInfinity : ve / a0;
            mdot         = ve == 0 ? 0 : thrust / ve;
            dv           = thrust == 0 ? 0 : -ve * Log(1 - bt / tau);
            OptimizeTime = false;
        }

        public Phase Rescale(Scale scale)
        {
            Check.False(Normalized);

            var phase = (Phase)MemberwiseClone();

            phase.ve         = ve / scale.VelocityScale;
            phase.tau        = tau / scale.TimeScale;
            phase.mdot       = mdot / scale.MdotScale;
            phase.thrust     = thrust / scale.ForceScale;
            phase.bt         = bt / scale.TimeScale;
            phase.mint       = mint / scale.TimeScale;
            phase.maxt       = maxt / scale.TimeScale;
            phase.m0         = m0 / scale.MassScale;
            phase.mf         = mf / scale.MassScale;
            phase.DropMass   = DropMass / scale.MassScale;
            phase.Normalized = true;

            return phase;
        }

        public void Integrate(Vn y0, Vn yf, double t0, double tf) => _integrator.Integrate(y0, yf, this, t0, tf);

        public void Integrate(Vn y0, Vn yf, double t0, double tf, Solution solution) => _integrator.Integrate(y0, yf, this, t0, tf, solution);

        public static Phase NewStageUsingFinalMass(double m0, double mf, double isp, double bt, int kspStage, int mjPhase, bool optimizeTime = false,
            bool unguided = false)
        {
            Check.PositiveFinite(m0);
            Check.PositiveFinite(mf);
            Check.PositiveFinite(isp);
            Check.PositiveFinite(bt);

            double mdot = (m0 - mf) / bt;
            double thrust = mdot * (isp * G0);

            Check.PositiveFinite(mdot);
            Check.PositiveFinite(thrust);

            var phase = new Phase(m0, thrust, isp, mf, bt, kspStage, mjPhase) { OptimizeTime = optimizeTime, Unguided = unguided };

            return phase;
        }

        public static Phase NewStageUsingThrust(double m0, double thrust, double isp, double bt, int kspStage, int mjPhase, bool optimizeTime = false,
            bool unguided = false)
        {
            Check.PositiveFinite(m0);
            Check.PositiveFinite(thrust);
            Check.PositiveFinite(isp);
            Check.PositiveFinite(bt);

            double mdot = thrust / (isp * G0);
            double mf = m0 - mdot * bt;

            Check.PositiveFinite(mdot);
            Check.PositiveFinite(mf);

            var phase = new Phase(m0, thrust, isp, mf, bt, kspStage, mjPhase) { OptimizeTime = optimizeTime, Unguided = unguided };

            return phase;
        }

        public static Phase NewFixedCoast(double m0, double ct, int kspStage, int mjPhase) => new Phase(m0, 0, 0, m0, ct, kspStage, mjPhase);

        public static Phase NewOptimizedCoast(double m0, double mint, double maxt, int kspStage, int mjPhase)
        {
            var phase = new Phase(m0, 0, 0, m0, mint, kspStage, mjPhase) { OptimizeTime = true, mint = mint, maxt = maxt };
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
