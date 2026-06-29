/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Text;
using MechJebLib.Primitives;
using MechJebLib.Utils;
using static MechJebLib.Utils.Statics;
using static System.Math;

namespace MechJebLib.PSG
{
    public class Phase : IDisposable
    {
        // starting mass of the phase/stage
        public double M0;

        // ending mass of the phase/stage
        public double Mf;

        // minimum mass constraint used by the optimizer, other callers should use Mf vs. MinM
        public double MinM;

        // burntime of the stage
        public double Bt;

        // minimum coast/burn time of the phase
        public double MinT;

        // maximum coast/burn time of the phase
        public double MaxT;

        // exhaust velocity in vacuum (0 for coast)
        public double VexVacuum;

        // exhaust velocity at current conditions (0 for coast)
        public double VexCurrent;

        // mdot of phase (0 for coast)
        public double Mdot;

        // minimum supported throttle
        public double MinThrottle;

        // assigned in the solution for a stage which needs a precise shutdown.
        // (AllowShutdown stages which the optimizer burns to completion do not get this tag)
        public bool PreciseShutdown;

        // assigned in the solution for the 'top' of the rocket.
        public bool TerminalStage;

        // mass continuity assigned to the last two stages in a burn-coast-burn sequence in the same phase (coast during)
        public bool MassContinuity;

        // true if the stage is inertially fixed
        public bool Unguided;

        // true if this phase has been converted to normalized units
        public bool Normalized;

        // used for "boot" process in Ascent
        public bool Tagged;

        // if this stage is allowed to let the optimizer tune the burntime
        public bool AllowShutdown;

        // the KSP stage of the rocket (KSP stage sequencing number)
        public int KSPStage;

        // the MJ phase of the rocket (index into stage stats)
        public int MJPhase;

        public double VacThrust   => Mdot * VexVacuum;
        public double Tau         => Coast ? double.PositiveInfinity : VexVacuum / VacThrust * M0;
        public bool   Coast       => Mdot == 0;
        public bool   GuidedCoast => Coast && !Unguided;

        private static readonly ObjectPool<Phase> _pool = new ObjectPool<Phase>(New, Clear);

        private Phase()
        {
        }

        private static Phase New() => new Phase();

        private static void Clear(Phase phase)
        {
            phase.M0 = 0;
            phase.Mf = 0;
            phase.MinM = 0;
            phase.Bt = 0;
            phase.MaxT = 0;
            phase.MinT = 0;
            phase.VexVacuum = 0;
            phase.VexCurrent = 0;
            phase.Mdot = 0;
            phase.MinThrottle = 0;

            phase.PreciseShutdown = false;
            phase.TerminalStage = false;
            phase.MassContinuity = false;
            phase.Unguided = false;
            phase.Normalized = false;
            phase.Tagged = false;
            phase.AllowShutdown = false;

            phase.KSPStage = 0;
            phase.MJPhase = 0;
        }

        public static Phase Rent() => _pool.Borrow();

        public Phase DeepCopy()
        {
            var newPhase = (Phase)MemberwiseClone();

            return newPhase;
        }

        private void Set(double m0, double vacThrust, double ispVacuum, double mf, double bt, int kspStage, int mjPhase, double ispCurrent = -1)
        {
            KSPStage = kspStage;
            MJPhase = mjPhase;
            M0 = m0;
            Mf = mf;
            MinM = mf;
            Bt = bt;
            VexVacuum = ispVacuum * G0;
            VexCurrent = ispCurrent >= 0 ? ispCurrent * G0 : VexVacuum;
            Mdot = VexVacuum == 0 ? 0 : vacThrust / VexVacuum;
        }

        public Phase Rescale(Scale scale)
        {
            Check.False(Normalized);

            var phase = (Phase)MemberwiseClone();

            phase.VexVacuum = VexVacuum / scale.VelocityScale;
            phase.VexCurrent = VexCurrent / scale.VelocityScale;
            phase.Mdot = Mdot / scale.MdotScale;
            phase.Bt = Bt / scale.TimeScale;
            phase.MinT = MinT / scale.TimeScale;
            phase.MaxT = MaxT / scale.TimeScale;
            phase.M0 = M0 / scale.MassScale;
            phase.Mf = Mf / scale.MassScale;
            phase.MinM = MinM / scale.MassScale;
            phase.Normalized = true;

            return phase;
        }

        public static Phase NewStage(double m0, double mf, double thrust, double isp, int kspStage, int mjPhase,
            bool unguided = false, bool allowShutdown = true, bool massContinuity = false, double ispCurrent = -1, double minThrottle = 1.0)
        {
            Check.PositiveFinite(m0);
            Check.PositiveFinite(thrust);
            Check.PositiveFinite(mf);
            Check.PositiveFinite(isp);
            Check.NonNegativeFinite(minThrottle);

            double mdot = thrust / (isp * G0);
            double bt = (m0 - mf) / mdot;

            Check.PositiveFinite(mdot);
            Check.PositiveFinite(isp);

            Phase phase = Rent();
            phase.Set(m0, thrust, isp, mf, bt, kspStage, mjPhase, ispCurrent);

            phase.AllowShutdown = allowShutdown;
            phase.MinT = allowShutdown ? 0 : bt;
            phase.MaxT = bt / minThrottle;
            phase.Unguided = unguided;
            phase.MassContinuity = massContinuity;
            phase.MinThrottle = minThrottle;

            return phase;
        }

        public static Phase NewCoast(double m0, double mf, double minT, double maxT, int kspStage, int mjPhase, bool unguided = false, bool massContinuity = false)
        {
            Phase phase = Rent();
            phase.Set(m0, 0, 0, mf, minT, kspStage, mjPhase);

            phase.AllowShutdown = true;
            phase.MinT = minT;
            phase.MaxT = maxT;
            phase.Unguided = unguided;
            phase.MassContinuity = massContinuity;
            return phase;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"stage: {KSPStage} m0: {M0:F4} mf: {Mf:F4} thrust: {VacThrust:F4} bt: {MinT:F4}<={Bt:F4}<={MaxT:F4} ve: {VexVacuum:F4}/{VexCurrent:F4} mdot: {Mdot:F4} minThrottle: {MinThrottle:P1}");
            if (Unguided)
                sb.Append(" (unguided)");
            sb.Append(!AllowShutdown ? " (no shutdown)" : " (allow shutdown)");
            if (MassContinuity)
                sb.Append(" (mass-continuity)");
            return sb.ToString();
        }

        public double BurnTimeFromMass(double m)           => Coast ? double.PositiveInfinity : (m - Mf) / Mdot;
        public double DeltaVFromMass(double m0, double mf) => Coast ? 0 : VexVacuum * Log(m0 / mf);

        public void Dispose() => _pool.Release(this);
    }
}
