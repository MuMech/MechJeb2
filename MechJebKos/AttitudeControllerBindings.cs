/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;
using kOS.Suffixed;
using MuMech.AttitudeControllers;

namespace MuMech.MechJebKos
{
    // ADDONS:MECHJEB:ATTITUDE:MJ, :KOS, :HYBRID and :BETTER - each attitude controller's settings, the ones of the Attitude Adjustment
    // window. They can be changed whichever controller is active (ATTITUDE:CONTROLLER), and are saved with MechJeb's settings. Angles
    // are in degrees. The vector read-outs are (pitch, roll, yaw), in the vessel's axes.
    [KOSNomenclature("MechJebAttitudeControllerSettings")]
    public abstract class AttitudeControllerBinding : Structure
    {
        private readonly Func<MechJebCore?> _core;

        protected AttitudeControllerBinding(Func<MechJebCore?> core)
        {
            _core = core;
            RegisterInitializer(InitializeSuffixes);
        }

        // re-evaluated on every call, as in ComputerModuleBinding
        protected MechJebModuleAttitudeController Attitude
        {
            get
            {
                MechJebCore core = _core() ?? throw new KOSException("MechJeb is not available on this vessel.");
                return core.GetComputerModule<MechJebModuleAttitudeController>() ??
                    throw new KOSException("MechJeb module MechJebModuleAttitudeController is not available on this vessel.");
            }
        }

        protected abstract BaseAttitudeController Controller { get; }

        protected abstract void InitializeSuffixes();

        protected void Common() =>
            AddSuffix("ACTIVE", new Suffix<BooleanValue>(() => ReferenceEquals(Attitude.Controller, Controller),
                "True if this is the controller in use (ATTITUDE:CONTROLLER)."));

        protected void Number(string name, Func<double> get, Action<double> set, string description) =>
            AddSuffix(name, new SetSuffix<ScalarValue>(() => get(), value => set(KosArgs.Finite(value, name)), description));

        protected void Flag(string name, Func<bool> get, Action<bool> set, string description) =>
            AddSuffix(name, new SetSuffix<BooleanValue>(() => get(), value => set(value), description));

        protected void Readout(string name, Func<Vector3d> get, string description) =>
            AddSuffix(name, new Suffix<Vector>(() => new Vector(get()), description));
    }

    // ADDONS:MECHJEB:ATTITUDE:BETTER - BetterController, MechJeb's default: a position PID cascaded into a rate PID per axis.
    [KOSNomenclature("MechJebBetterController")]
    public class BetterControllerBinding : AttitudeControllerBinding
    {
        public BetterControllerBinding(Func<MechJebCore?> core) : base(core) { }

        // settings from before the current version are reset on the first engage, so do that before reading or changing one
        private BetterController C
        {
            get
            {
                BetterController c = Attitude.GetController<BetterController>();
                c.UpgradeSettings();
                return c;
            }
        }

        protected override BaseAttitudeController Controller => Attitude.GetController<BetterController>();

        protected override void InitializeSuffixes()
        {
            Common();

            Pid("POS", "position (angle to rate)", c => c.PosKp, c => c.PosTi, c => c.PosTd, c => c.PosN, c => c.PosB, c => c.PosC,
                c => c.PosDeadband, c => c.PosSmoothIn, c => c.PosSmoothOut, c => c.PosClegg, (c, v) => c.PosClegg = v);
            Pid("VEL", "velocity (rate to acceleration)", c => c.VelKp, c => c.VelTi, c => c.VelTd, c => c.VelN, c => c.VelB, c => c.VelC,
                c => c.VelDeadband, c => c.VelSmoothIn, c => c.VelSmoothOut, c => c.VelClegg, (c, v) => c.VelClegg = v);

            Flag("USESTOPPINGTIME", () => C.UseStoppingTime, v => Toggle(c => c.UseStoppingTime = v),
                "Limit the rotation rate by MAXSTOPPINGTIME. Turning it off also turns off USEFLIPTIME.");
            Number("MAXSTOPPINGTIME", () => C.MaxStoppingTime.Val, v => C.MaxStoppingTime.Val = v,
                "Rotate no faster than can be stopped in this many seconds.");
            Flag("USEFLIPTIME", () => C.UseFlipTime, v => Toggle(c => c.UseFlipTime = v),
                "Allow at least the rate that flips the vessel in MINFLIPTIME. Needs USESTOPPINGTIME.");
            Number("MINFLIPTIME", () => C.MinFlipTime.Val, v => C.MinFlipTime.Val = v,
                "Seconds for a 180 degree flip that the rate limit always allows.");
            Flag("USECONTROLRANGE", () => C.UseControlRange, v => C.UseControlRange = v,
                "Hold roll still until the pointing error is within ROLLCONTROLRANGE.");
            Number("ROLLCONTROLRANGE", () => C.RollControlRange.Val, v => C.RollControlRange.Val = v,
                "Pointing error (degrees) within which roll is controlled.");
            Number("SMOOTHTORQUE", () => C.SmoothTorque.Val, v => C.SmoothTorque.Val = v,
                "Low-pass factor (0-1) on the available torque.");
            Number("SOFTEN", () => C.Soften.Val, v => C.Soften.Val = v,
                "Factor (0-1) on the stopping rate, to reduce overshoot on large turns.");
            AddSuffix("DEFAULTS", new NoArgsVoidSuffix(() => C.Defaults(), "Reset all these settings to MechJeb's defaults."));

            Readout("CURRENT", () => C._current, "The vessel's attitude as the controller tracks it.");
            Readout("DESIRED", () => C._desired, "The attitude asked for, as the controller tracks it.");
            Readout("ERRORVECTOR", () => C._error, "The attitude error (rad).");
            Readout("TARGETOMEGA", () => C._targetOmega, "The rotation rate the position PID asks for (rad/s).");
            Readout("TARGETTORQUE", () => C._targetTorque, "The torque the rate PID asks for.");
            Readout("ACTUATION", () => C._actuation, "The control output (-1 to 1).");
            Readout("CONTROLTORQUE", () => C._controlTorque, "The filtered torque available.");
            Readout("MAXALPHA", () => C._maxAlpha, "The largest angular acceleration available (rad/s^2).");
        }

        // the window's sync: Minimum Flip Time needs Maximum Stopping Time
        private void Toggle(Action<BetterController> set)
        {
            BetterController c = C;
            set(c);
            c.FixFlipTime();
        }

        private void Pid(string prefix, string loop,
            Func<BetterController, EditableDoubleMult> kp, Func<BetterController, EditableDoubleMult> ti,
            Func<BetterController, EditableDoubleMult> td, Func<BetterController, EditableDoubleMult> n,
            Func<BetterController, EditableDoubleMult> b, Func<BetterController, EditableDoubleMult> c,
            Func<BetterController, EditableDoubleMult> deadband, Func<BetterController, EditableDoubleMult> smoothIn,
            Func<BetterController, EditableDoubleMult> smoothOut, Func<BetterController, bool> getClegg, Action<BetterController, bool> setClegg)
        {
            PidNumber(prefix + "KP", kp, $"Proportional gain of the {loop} PID.");
            PidNumber(prefix + "TI", ti, $"Integral time (s) of the {loop} PID; 0 for no integral.");
            PidNumber(prefix + "TD", td, $"Derivative time (s) of the {loop} PID.");
            PidNumber(prefix + "N", n, $"Derivative filter coefficient N (Tf = TD / N) of the {loop} PID.");
            PidNumber(prefix + "B", b, $"Setpoint weight on the proportional term of the {loop} PID.");
            PidNumber(prefix + "C", c, $"Setpoint weight on the derivative term of the {loop} PID.");
            PidNumber(prefix + "DEADBAND", deadband, $"Integral deadband of the {loop} PID, as a fraction of its output limit.");
            PidNumber(prefix + "SMOOTHIN", smoothIn, $"Input smoothing (0-1) of the {loop} PID; 1 for none.");
            PidNumber(prefix + "SMOOTHOUT", smoothOut, $"Output smoothing (0-1) of the {loop} PID; 1 for none.");
            Flag(prefix + "CLEGG", () => getClegg(C), v => setClegg(C, v), $"Clegg integrator in the {loop} PID: the integral resets when the error changes sign.");
        }

        private void PidNumber(string name, Func<BetterController, EditableDoubleMult> field, string description) =>
            Number(name, () => field(C).Val, v => field(C).Val = v, description);
    }

    // ADDONS:MECHJEB:ATTITUDE:MJ - MJAttitudeController, MechJeb's original PID controller.
    [KOSNomenclature("MechJebMJController")]
    public class MJControllerBinding : AttitudeControllerBinding
    {
        public MJControllerBinding(Func<MechJebCore?> core) : base(core) { }

        private MJAttitudeController C => Attitude.GetController<MJAttitudeController>();

        protected override BaseAttitudeController Controller => C;

        protected override void InitializeSuffixes()
        {
            Common();

            Flag("TFAUTOTUNE", () => C.TfAutoTune, v => C.SetTfAutoTune(v),
                "Tune Tf from the vessel's moment of inertia and torque, within TFMIN and TFMAX.");
            AddSuffix("TF", new SetSuffix<Vector>(() => new Vector(C._tfV), value => C.SetManualTf(FiniteVector(value, "TF")),
                "The Tf (s) in use, auto-tuned while TFAUTOTUNE; setting it sets the manual Tf (each at least 0.01) used without TFAUTOTUNE."));
            Number("TFMIN", () => C.TfMin, v => C.SetTfMin(v), "Least auto-tuned Tf (s), at least 0.01.");
            Number("TFMAX", () => C.TfMax, v => C.SetTfMax(v), "Greatest auto-tuned Tf (s), at least 0.01.");
            Flag("LOWPASSFILTER", () => C.LowPassFilter, v => C.SetLowPassFilter(v), "Low-pass filter the output with the time constant Tf.");
            Number("KPFACTOR", () => C.KpFactor, v =>
            {
                MJAttitudeController c = C;
                c.SetFactors(v, c.KiFactor, c.KdFactor);
            }, "Kp = Kd / (KPFACTOR * sqrt(2) * Tf), at least 0.01.");
            Number("KIFACTOR", () => C.KiFactor, v =>
            {
                MJAttitudeController c = C;
                c.SetFactors(c.KpFactor, v, c.KdFactor);
            }, "Ki = Kp / (KIFACTOR * sqrt(2) * Tf), at least 0.01.");
            Number("KDFACTOR", () => C.KdFactor, v =>
            {
                MJAttitudeController c = C;
                c.SetFactors(c.KpFactor, c.KiFactor, v);
            }, "Kd = KDFACTOR / Tf, at least 0.01.");
            Number("DEADBAND", () => C.Deadband, v => C.SetDeadband(v), "Outputs smaller than this are zeroed.");
            Number("KWLIMIT", () => C.KWlimit.Val, v => C.KWlimit.Val = v, "Maximum relative angular velocity (0-1).");
            AddSuffix("RESET", new NoArgsVoidSuffix(() => C.ResetConfig(), "Reset all these settings to MechJeb's defaults."));

            Readout("ERRORVECTOR", () => C._error, "The attitude error (rad).");
        }

        private static Vector3d FiniteVector(Vector value, string what)
        {
            Vector3d v = value.ToVector3D();
            if (!v.IsFinite())
                throw new KOSException($"{what} must be a finite vector.");
            return v;
        }
    }

    // ADDONS:MECHJEB:ATTITUDE:KOS - KosAttitudeController, after kOS's own steering manager.
    [KOSNomenclature("MechJebKosController")]
    public class KosControllerBinding : AttitudeControllerBinding
    {
        public KosControllerBinding(Func<MechJebCore?> core) : base(core) { }

        private KosAttitudeController C => Attitude.GetController<KosAttitudeController>();

        protected override BaseAttitudeController Controller => C;

        protected override void InitializeSuffixes()
        {
            Common();

            Number("MAXSTOPPINGTIME", () => C.MaxStoppingTime.Val, v => C.MaxStoppingTime.Val = v,
                "Rotate no faster than can be stopped in this many seconds.");
            Number("ROLLCONTROLRANGE", () => C.RollControlRange.Val * UtilMath.Rad2Deg, v => C.RollControlRange.Val = v * UtilMath.Deg2Rad,
                "Pointing error (degrees) within which roll is controlled.");

            Readout("PHIVECTOR", () => C._phiVector, "The attitude error (rad).");
            Readout("TARGETOMEGA", () => C._targetOmega, "The rotation rate asked for (rad/s).");
            Readout("MAXOMEGA", () => C._maxOmega, "The rotation rate limit (rad/s).");
            Readout("TARGETTORQUE", () => C._targetTorque, "The torque asked for.");
            Readout("ACTUATION", () => C._actuation, "The control output (-1 to 1).");
        }
    }

    // ADDONS:MECHJEB:ATTITUDE:HYBRID - HybridController, the kOS-style controller with a great-circle error and stopping distance.
    [KOSNomenclature("MechJebHybridController")]
    public class HybridControllerBinding : AttitudeControllerBinding
    {
        public HybridControllerBinding(Func<MechJebCore?> core) : base(core) { }

        private HybridController C => Attitude.GetController<HybridController>();

        protected override BaseAttitudeController Controller => C;

        protected override void InitializeSuffixes()
        {
            Common();

            Flag("USEINERTIA", () => C.UseInertia, v => C.UseInertia = v, "Take the distance needed to stop the rotation off the error.");
            Number("MAXSTOPPINGTIME", () => C.MaxStoppingTime.Val, v => C.MaxStoppingTime.Val = v,
                "Rotate no faster than can be stopped in this many seconds.");
            Flag("USECONTROLRANGE", () => C.UseControlRange, v => C.UseControlRange = v,
                "Hold roll still until the pointing error is within ROLLCONTROLRANGE.");
            Number("ROLLCONTROLRANGE", () => C.RollControlRange.Val * UtilMath.Rad2Deg, v => C.RollControlRange.Val = v * UtilMath.Deg2Rad,
                "Pointing error (degrees) within which roll is controlled.");

            Readout("PHIVECTOR", () => C._phiVector, "The attitude error (rad).");
            Readout("TARGETOMEGA", () => C._targetOmega, "The rotation rate asked for (rad/s).");
            Readout("MAXOMEGA", () => C._maxOmega, "The rotation rate limit (rad/s).");
            Readout("TARGETTORQUE", () => C._targetTorque, "The torque asked for.");
            Readout("ACTUATION", () => C._actuation, "The control output (-1 to 1).");
        }
    }
}
