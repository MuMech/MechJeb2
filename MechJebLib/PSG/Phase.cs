/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Text;
using MechJebLib.Primitives;
using MechJebLib.Utils;
using static MechJebLib.Utils.Statics;
using static System.Math;

namespace MechJebLib.PSG
{
    public class Phase
    {
        public          double m0;
        public readonly double isp;
        public          double mf;
        public          double bt;
        public          double maxt;
        public          double mint;
        public          double ve;
        public readonly double a0;
        public          double tau;
        private         double _mdot;
        public          double dv;

        public bool AllowInfiniteBurntime;
        public bool Infinite        = false; // remove?
        public bool AllowShutdown   = true;
        public bool PreciseShutdown = false;
        public bool TerminalStage   = false;
        public bool MassContinuity;
        public bool Unguided;
        public bool Normalized;
        public bool Tagged;

        public readonly int KSPStage;
        public readonly int MJPhase;

        public bool   Coast       => _thrust == 0;
        public bool   GuidedCoast => Coast && !Unguided;
        public double Thrust      => Infinite ? 2 * _thrust : _thrust;
        public double Mdot        => Infinite ? 0 : _mdot;

        private double _thrust;

        public Phase DeepCopy()
        {
            var newphase = (Phase)MemberwiseClone();

            return newphase;
        }

        private Phase(double m0, double thrust, double isp, double mf, double bt, int kspStage, int mjPhase)
        {
            KSPStage              = kspStage;
            MJPhase               = mjPhase;
            this.m0               = m0;
            _thrust               = thrust;
            this.isp              = isp;
            this.mf               = mf;
            this.bt               = bt;
            ve                    = isp * G0;
            a0                    = thrust / m0;
            tau                   = thrust == 0 ? double.PositiveInfinity : ve / a0;
            _mdot                 = ve == 0 ? 0 : thrust / ve;
            dv                    = DeltaVForTime(m0, bt);
            AllowInfiniteBurntime = false;
        }

        public Phase Rescale(Scale scale)
        {
            Check.False(Normalized);

            var phase = (Phase)MemberwiseClone();

            phase.ve         = ve / scale.VelocityScale;
            phase.tau        = tau / scale.TimeScale;
            phase._mdot      = _mdot / scale.MdotScale;
            phase._thrust    = _thrust / scale.ForceScale;
            phase.bt         = bt / scale.TimeScale;
            phase.mint       = mint / scale.TimeScale;
            phase.maxt       = maxt / scale.TimeScale;
            phase.m0         = m0 / scale.MassScale;
            phase.mf         = mf / scale.MassScale;
            phase.Normalized = true;

            return phase;
        }

        public static Phase NewStageUsingFinalMass(double m0, double mf, double isp, double bt, int kspStage, int mjPhase,
            bool unguided = false, bool allowShutdown = true, bool massContinuity = false)
        {
            Check.PositiveFinite(m0);
            Check.PositiveFinite(mf);
            Check.PositiveFinite(isp);
            Check.PositiveFinite(bt);

            double mdot   = (m0 - mf) / bt;
            double thrust = mdot * (isp * G0);

            Check.PositiveFinite(mdot);
            Check.PositiveFinite(thrust);

            var phase = new Phase(m0, thrust, isp, mf, bt, kspStage, mjPhase) { AllowShutdown = allowShutdown, Unguided = unguided, MassContinuity = massContinuity };

            return phase;
        }

        public static Phase NewStageUsingThrust(double m0, double thrust, double isp, double bt, int kspStage, int mjPhase,
            bool unguided = false, bool allowShutdown = true, bool massContinuity = false)
        {
            Check.PositiveFinite(m0);
            Check.PositiveFinite(thrust);
            Check.PositiveFinite(isp);
            Check.PositiveFinite(bt);

            double mdot = thrust / (isp * G0);
            double mf   = m0 - mdot * bt;

            Check.PositiveFinite(mdot);
            Check.PositiveFinite(mf);

            var phase = new Phase(m0, thrust, isp, mf, bt, kspStage, mjPhase) { AllowShutdown = allowShutdown, Unguided = unguided, MassContinuity = massContinuity };

            return phase;
        }

        public static Phase NewStageUsingFinalMassAndThrust(double m0, double mf, double thrust, double bt, int kspStage, int mjPhase,
            bool unguided = false, bool allowShutdown = true, bool massContinuity = false)
        {
            Check.PositiveFinite(m0);
            Check.PositiveFinite(thrust);
            Check.PositiveFinite(mf);
            Check.PositiveFinite(bt);

            double mdot = (m0 - mf) / bt;
            double isp  = thrust / mdot / G0;

            Check.PositiveFinite(mdot);
            Check.PositiveFinite(isp);

            var phase = new Phase(m0, thrust, isp, mf, bt, kspStage, mjPhase) { AllowShutdown = allowShutdown, Unguided = unguided, MassContinuity = massContinuity };

            return phase;
        }

        public static Phase NewStageUsingFinalMassThrustAndIsp(double m0, double mf, double thrust, double isp, int kspStage, int mjPhase,
            bool unguided = false, bool allowShutdown = true, bool massContinuity = false)
        {
            Check.PositiveFinite(m0);
            Check.PositiveFinite(thrust);
            Check.PositiveFinite(mf);
            Check.PositiveFinite(isp);

            double mdot = thrust / (isp * G0);
            double bt   = (m0 - mf) / mdot;

            Check.PositiveFinite(mdot);
            Check.PositiveFinite(isp);

            var phase = new Phase(m0, thrust, isp, mf, bt, kspStage, mjPhase) { AllowShutdown = allowShutdown, Unguided = unguided, MassContinuity = massContinuity };

            return phase;
        }

        public static Phase NewCoast(double m0, double mint, double maxt, int kspStage, int mjPhase, bool unguided = false, bool massContinuity = false)
        {
            var phase = new Phase(m0, 0, double.PositiveInfinity, m0, mint, kspStage, mjPhase) { mint = mint, maxt = maxt, Unguided = unguided, MassContinuity = massContinuity };
            return phase;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"stage: {KSPStage} m0: {m0} mf: {mf} thrust: {Thrust} bt: {bt} isp: {isp} mdot: {Mdot}");
            if (AllowInfiniteBurntime)
                sb.Append(" (infinite burn time)");
            if (Infinite)
                sb.Append(" (infinite isp)");
            if (Unguided)
                sb.Append(" (unguided)");
            sb.Append(!AllowShutdown ? " (no shutdown)" : " (allow shutdown)");
            if (MassContinuity)
                sb.Append(" (mass-continuity)");
            return sb.ToString();
        }

        public double DeltaVForTime(double m, double t) => Coast ? 0 : -ve * Log(1 - t * Thrust / (ve * m));
        public double BurnTimeFromMass(double m)        => Coast ? double.PositiveInfinity : (m - mf) / _mdot;
        public double TauFromMass(double m)             => Coast ? double.PositiveInfinity : m / _mdot;
    }
}
