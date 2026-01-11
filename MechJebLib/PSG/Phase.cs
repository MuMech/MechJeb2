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
        public double M0;
        public double Mf;
        public double Bt;
        public double MaxT;
        public double MinT;
        public double VexVacuum;
        public double VexCurrent;
        public double Mdot;

        public bool PreciseShutdown = false;
        public bool TerminalStage   = false;
        public bool MassContinuity;
        public bool Unguided;
        public bool Normalized;
        public bool Tagged;

        public readonly int KSPStage;
        public readonly int MJPhase;

        public bool AllowShutdown => MinT < MaxT;
        public double VacThrust => Mdot * VexVacuum;
        public double Tau => Coast ? double.PositiveInfinity : VexVacuum / VacThrust * M0;
        public bool   Coast       => Mdot == 0;
        public bool   GuidedCoast => Coast && !Unguided;

        public Phase DeepCopy()
        {
            var newPhase = (Phase)MemberwiseClone();

            return newPhase;
        }

        private Phase(double m0, double vacThrust, double ispVacuum, double mf, double bt, int kspStage, int mjPhase, double ispCurrent = -1)
        {
            KSPStage   = kspStage;
            MJPhase    = mjPhase;
            this.M0    = m0;
            this.Mf    = mf;
            this.Bt    = bt;
            VexVacuum  = ispVacuum * G0;
            VexCurrent = ispCurrent >= 0 ? ispCurrent * G0 : VexVacuum;
            Mdot = VexVacuum == 0 ? 0 : vacThrust / VexVacuum;
        }

        public Phase Rescale(Scale scale)
        {
            Check.False(Normalized);

            var phase = (Phase)MemberwiseClone();

            phase.VexVacuum  = VexVacuum / scale.VelocityScale;
            phase.VexCurrent = VexCurrent / scale.VelocityScale;
            phase.Mdot       = Mdot / scale.MdotScale;
            phase.Bt         = Bt / scale.TimeScale;
            phase.MinT       = MinT / scale.TimeScale;
            phase.MaxT       = MaxT / scale.TimeScale;
            phase.M0         = M0 / scale.MassScale;
            phase.Mf         = Mf / scale.MassScale;
            phase.Normalized = true;

            return phase;
        }

        public static Phase NewStage(double m0, double mf, double thrust, double isp, int kspStage, int mjPhase,
            bool unguided = false, bool allowShutdown = true, bool massContinuity = false, double ispCurrent = -1)
        {
            Check.PositiveFinite(m0);
            Check.PositiveFinite(thrust);
            Check.PositiveFinite(mf);
            Check.PositiveFinite(isp);

            double mdot = thrust / (isp * G0);
            double bt   = (m0 - mf) / mdot;

            Check.PositiveFinite(mdot);
            Check.PositiveFinite(isp);

            var phase = new Phase(m0, thrust, isp, mf, bt, kspStage, mjPhase, ispCurrent)
            {
                MinT           = allowShutdown ? 0 : bt,
                MaxT           = bt,
                Unguided       = unguided,
                MassContinuity = massContinuity
            };

            return phase;
        }

        public static Phase NewCoast(double m0, double minT, double maxT, int kspStage, int mjPhase, bool unguided = false, bool massContinuity = false)
        {
            var phase = new Phase(m0, 0, 0, m0, minT, kspStage, mjPhase) { MinT = minT, MaxT = maxT, Unguided = unguided, MassContinuity = massContinuity };
            return phase;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"stage: {KSPStage} m0: {M0} mf: {Mf} thrust: {VacThrust} bt: {MinT}<={Bt}<={MaxT} ve: {VexVacuum}/{VexCurrent} mdot: {Mdot}");
            if (Unguided)
                sb.Append(" (unguided)");
            sb.Append(!AllowShutdown ? " (no shutdown)" : " (allow shutdown)");
            if (MassContinuity)
                sb.Append(" (mass-continuity)");
            return sb.ToString();
        }

        public double DeltaVForTime(double m, double t) => Coast ? 0 : -VexVacuum * Log(1 - t * VacThrust / (VexVacuum * m));
        public double BurnTimeFromMass(double m)        => Coast ? double.PositiveInfinity : (m - Mf) / Mdot;
        public double TauFromMass(double m)             => Coast ? double.PositiveInfinity : m / Mdot;
    }
}
