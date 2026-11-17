/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Linq;
using kOS;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Execution;
using kOS.Safe.Utilities;
using kOS.Suffixed;
using MuMech.AttitudeControllers;
using UnityEngine;

namespace MuMech.MechJebKos
{
    // ADDONS:MECHJEB:ATTITUDE - MechJeb's attitude controller (MechJebModuleAttitudeController), driven directly.
    //
    // A pointing command makes this kOS processor a user of the controller, like the node executor or the ascent autopilot, and
    // SmartASS steps aside (ADDONS:MECHJEB:SMARTASS drives SmartASS itself). The claim lasts until RELEASE(), or until the program
    // that made it ends, like LOCK STEERING; one made at the terminal lasts until RELEASE(). Don't LOCK STEERING at the same time:
    // kOS and MechJeb would both steer.
    //
    // MechJeb follows the reference frame every tick, so a target in a moving frame needs no script loop:
    //
    //   SET att TO ADDONS:MECHJEB:ATTITUDE.
    //   att:ATTITUDETO(V(0,0,1), "ORBIT").           // prograde (in a reference frame z is forward, y up, x right)
    //   att:HEADINGTO(90, 45, 0).                     // compass heading 90, pitch 45, roll 0
    //   att:ATTITUDETO(HEADING(90, 45), "INERTIAL").  // a kOS Direction, as LOCK STEERING takes it
    //   SET att:CONTROLROLL TO FALSE.                  // leave roll alone
    //   att:RELEASE().
    //
    // The controller and its tuning: CONTROLLER selects MJ, KOS, HYBRID or BETTER (the Attitude Adjustment window's choice), and
    // ATTITUDE:MJ, :KOS, :HYBRID and :BETTER hold each one's settings, which can be changed whichever is active.
    [KOSNomenclature("MechJebAttitude")]
    public class AttitudeBinding : ComputerModuleBinding<MechJebModuleAttitudeController>, IPopContextNotifyee
    {
        private readonly SharedObjects _shared;

        private readonly MJControllerBinding     _mj;
        private readonly KosControllerBinding    _kos;
        private readonly HybridControllerBinding _hybrid;
        private readonly BetterControllerBinding _better;

        // the per-axis options, applied after every command (AttitudeTo resets them)
        private bool _controlPitch = true, _controlYaw = true, _controlRoll = true;
        private bool _actuatePitch = true, _actuateYaw = true, _actuateRoll = true;

        // commanded rates in rad/s, NaN for none: where none is set, the rate the last command chose stays (a direction holds roll still)
        private double _pitchRate = double.NaN, _yawRate = double.NaN, _rollRate = double.NaN;
        private Vector3d _commandOmega = new Vector3d(double.NaN, double.NaN, double.NaN);

        // the kOS program context whose end releases the claim
        private IProgramContext? _claimContext;

        public AttitudeBinding(Func<MechJebCore?> core, SharedObjects shared) : base(core)
        {
            _shared = shared;
            _mj     = new MJControllerBinding(core);
            _kos    = new KosControllerBinding(core);
            _hybrid = new HybridControllerBinding(core);
            _better = new BetterControllerBinding(core);
        }

        protected override void InitializeSuffixes()
        {
            // --- the controller ---
            AddSuffix("CONTROLLER", new SetSuffix<StringValue>(() => ControllerName(Module.Controller), value => SetController(value),
                "The attitude controller in use: MJ, KOS, HYBRID or BETTER (as chosen in the Attitude Adjustment window)."));
            AddSuffix("CONTROLLERS", new Suffix<ListValue>(
                () => new ListValue(Module.Controllers.Select(c => (Structure)new StringValue(ControllerName(c)))),
                "The CONTROLLER values."));
            AddSuffix("MJ", new Suffix<MJControllerBinding>(() => _mj, "The MJ controller's settings."));
            AddSuffix("KOS", new Suffix<KosControllerBinding>(() => _kos, "The kOS-style controller's settings."));
            AddSuffix("HYBRID", new Suffix<HybridControllerBinding>(() => _hybrid, "The hybrid controller's settings."));
            AddSuffix("BETTER", new Suffix<BetterControllerBinding>(() => _better, "The BetterController's settings."));
            AddSuffix("RCSAUTO", new SetSuffix<BooleanValue>(() => Module.RCSAuto, value => Module.RCSAuto = value,
                "Switch RCS on for large attitude errors and off again once aligned."));

            // --- commands ---
            AddSuffix("ATTITUDETO", new TwoArgsSuffix<Structure, StringValue>(AttitudeTo,
                "Point at a Direction or a Vector in a reference frame (INERTIAL, ORBIT, SURFACE_NORTH, TARGET, MANEUVER_NODE, ...). " +
                "In INERTIAL a Direction is a world one, as LOCK STEERING takes it; in the other frames it is relative to the frame " +
                "(z forward, y up, x right). A Vector is a direction in the frame, the roll left to the controller."));
            AddSuffix("HEADINGTO", new ThreeArgsSuffix<ScalarValue, ScalarValue, ScalarValue>(HeadingTo,
                "Hold a compass heading, a pitch above the horizon and a roll, in degrees, following the surface frame as the vessel moves."));
            AddSuffix("KILLROT", new NoArgsVoidSuffix(KillRot,
                "Hold the current attitude, taking up the new one whenever the pilot steers (SmartASS KILL ROT)."));
            AddSuffix("RELEASE", new NoArgsVoidSuffix(Release,
                "Give up this processor's control of the attitude."));

            // --- per-axis options ---
            AxisOption("CONTROLPITCH", () => _controlPitch, v => _controlPitch = v, "Control the pitch axis (false: leave it to the pilot).");
            AxisOption("CONTROLYAW", () => _controlYaw, v => _controlYaw = v, "Control the yaw axis (false: leave it to the pilot).");
            AxisOption("CONTROLROLL", () => _controlRoll, v => _controlRoll = v, "Control the roll axis (false: leave it to the pilot).");
            AxisOption("ACTUATEPITCH", () => _actuatePitch, v => _actuatePitch = v, "Send the pitch output to the vessel (false: zero it).");
            AxisOption("ACTUATEYAW", () => _actuateYaw, v => _actuateYaw = v, "Send the yaw output to the vessel (false: zero it).");
            AxisOption("ACTUATEROLL", () => _actuateRoll, v => _actuateRoll = v, "Send the roll output to the vessel (false: zero it).");
            RateOption("PITCHRATE", () => _pitchRate, v => _pitchRate = v);
            RateOption("YAWRATE", () => _yawRate, v => _yawRate = v);
            RateOption("ROLLRATE", () => _rollRate, v => _rollRate = v);
            AddSuffix("FREERATES", new NoArgsVoidSuffix(() =>
                {
                    _pitchRate = _yawRate = _rollRate = double.NaN;
                    ApplyOptionsIfEngaged();
                },
                "Clear PITCHRATE, YAWRATE and ROLLRATE."));

            // --- state ---
            AddSuffix("ENABLED", new Suffix<BooleanValue>(() => Module.Enabled,
                "True while anything (this processor, SmartASS, an autopilot) uses the attitude controller."));
            AddSuffix("ENGAGED", new Suffix<BooleanValue>(() => Module.Users.Contains(this),
                "True while this processor holds the attitude controller."));
            AddSuffix("REFERENCE", new Suffix<StringValue>(() => Module.AttitudeReference.ToString(),
                "The reference frame of the current attitude target."));
            AddSuffix("REQUESTEDATTITUDE", new Suffix<Direction>(() => new Direction(Module.RequestedAttitude),
                "The attitude being steered to, as a world Direction (updated every tick while ENABLED)."));
            AddSuffix("TARGETVECTOR", new Suffix<Vector>(() => new Vector(Module.TargetAttitude()),
                "The direction being steered to (zero when not ENABLED)."));
            AddSuffix("ERROR", new Suffix<ScalarValue>(() => Module.AttitudeError,
                "Degrees between the vessel's forward and the target direction, ignoring roll (updated every tick while ENABLED)."));
            AddSuffix("STEERINGERROR", new Suffix<ScalarValue>(() => Module.SteeringError.Value,
                "ERROR averaged over the last ticks."));
            AddSuffix("TORQUE", new Suffix<Vector>(() => new Vector(Module.Torque),
                "Torque available to the controller, as (pitch, roll, yaw)."));
            AddSuffix("STOPPINGDISTANCE", new Suffix<Vector>(() => new Vector(Module.Inertia),
                "Angle (rad) needed to stop the current rotation at full torque, as (pitch, roll, yaw)."));
            AddSuffix("AXISCONTROL", new Suffix<Vector>(() => new Vector(Module.AxisControl),
                "The controlled axes in effect, 1 or 0, as (pitch, roll, yaw)."));
            AddSuffix("ACTUATIONCONTROL", new Suffix<Vector>(() => new Vector(Module.ActuationControl),
                "The actuated axes in effect, 1 or 0, as (pitch, roll, yaw)."));
        }

        private void AxisOption(string name, Func<bool> get, Action<bool> set, string description) =>
            AddSuffix(name, new SetSuffix<BooleanValue>(() => get(), value =>
            {
                set(value);
                ApplyOptionsIfEngaged();
            }, description + " Kept across commands."));

        private void RateOption(string name, Func<double> get, Action<double> set) =>
            AddSuffix(name, new SetSuffix<ScalarValue>(() => get(), value =>
                {
                    double d = value.GetDoubleValue();
                    if (double.IsInfinity(d))
                        throw new KOSException($"{name} must be a finite number (or NaN for none).");
                    set(d);
                    ApplyOptionsIfEngaged();
                },
                "Commanded rotation rate (rad/s) instead of an angle on this axis; NaN or FREERATES() for none. Kept across commands. " +
                "Only the BETTER controller follows it."));

        // ---- the controller ----

        private static string ControllerName(BaseAttitudeController controller) =>
            controller switch
            {
                MJAttitudeController _  => "MJ",
                KosAttitudeController _ => "KOS",
                HybridController _      => "HYBRID",
                BetterController _      => "BETTER",
                _                       => controller.GetType().Name.ToUpperInvariant()
            };

        private void SetController(StringValue value)
        {
            MechJebModuleAttitudeController attitude = Module;
            string name = value.ToString().Trim();

            for (int i = 0; i < attitude.Controllers.Count; i++)
            {
                if (!string.Equals(ControllerName(attitude.Controllers[i]), name, StringComparison.OrdinalIgnoreCase))
                    continue;
                // as the Attitude Adjustment window: only a change restarts the controller
                if (attitude.ActiveController != i)
                    attitude.SetActiveController(i);
                return;
            }

            throw new KOSException(
                $"Invalid attitude controller '{value}'. Expected one of: {string.Join(", ", attitude.Controllers.Select(ControllerName))}.");
        }

        // ---- commands ----

        private void AttitudeTo(Structure target, StringValue referenceName)
        {
            AttitudeReference reference = KosArgs.ParseEnum<AttitudeReference>(referenceName, "attitude reference");
            CheckReference(Core, reference);
            MechJebModuleAttitudeController attitude = Module;

            switch (target)
            {
                case Direction direction:
                    attitude.AttitudeTo((QuaternionD)direction.Rotation, reference, this);
                    break;
                case Vector vector:
                    Vector3d v = vector.ToVector3D();
                    if (!(v.sqrMagnitude > 0) || double.IsInfinity(v.sqrMagnitude))
                        throw new KOSException("ATTITUDETO needs a non-zero, finite vector.");
                    attitude.AttitudeTo(v.normalized, reference, this);
                    break;
                default:
                    throw new KOSException($"ATTITUDETO takes a Direction or a Vector, not a {target.KOSName}.");
            }

            Claimed(attitude, false);
        }

        private void HeadingTo(ScalarValue heading, ScalarValue pitch, ScalarValue roll)
        {
            double h = KosArgs.Finite(heading, "HEADINGTO heading");
            double p = KosArgs.Finite(pitch, "HEADINGTO pitch");
            double r = KosArgs.Finite(roll, "HEADINGTO roll");

            MechJebModuleAttitudeController attitude = Module;
            attitude.AttitudeTo(h, p, r, this);
            Claimed(attitude, false);
        }

        private void KillRot()
        {
            MechJebModuleAttitudeController attitude = Module;
            Transform transform = Core.vessel.GetTransform();
            attitude.AttitudeTo(QuaternionD.LookRotation(transform.up, -transform.forward), AttitudeReference.INERTIAL, this);
            Claimed(attitude, true);
        }

        /// <summary>Gives up this processor's claim (also used by the SmartASS binding before SmartASS engages).</summary>
        internal void Release()
        {
            _claimContext = null;

            MechJebModuleAttitudeController? attitude = TryModule;
            if (attitude == null || !attitude.Users.Contains(this))
                return;

            attitude.Users.Remove(this);
            // nobody left: also clear the KILLROT flag, which would otherwise take the next user's INERTIAL target
            if (attitude.Users.Count == 0)
                attitude.AttitudeDeactivate();
        }

        // after a command made this processor a user: the KILLROT flag, the per-axis options and the release at the program's end
        private void Claimed(MechJebModuleAttitudeController attitude, bool killRot)
        {
            // whoever commands last owns the one attitude target, so a KILLROT left by another user goes too
            attitude.AttitudeKillRot = killRot;
            _commandOmega = attitude.OmegaTarget;
            ApplyOptions(attitude);

            IProgramContext context = _shared.Cpu.GetCurrentContext();
            if (ReferenceEquals(context, _claimContext))
                return;
            _claimContext = context;
            _shared.Cpu.AddPopContextNotifyee(this);
        }

        private void ApplyOptionsIfEngaged()
        {
            MechJebModuleAttitudeController attitude = Module;
            if (attitude.Users.Contains(this))
                ApplyOptions(attitude);
        }

        private void ApplyOptions(MechJebModuleAttitudeController attitude)
        {
            attitude.SetAxisControl(_controlPitch, _controlYaw, _controlRoll);
            attitude.SetActuationControl(_actuatePitch, _actuateYaw, _actuateRoll);
            // OmegaTarget is (pitch, roll, yaw)
            attitude.SetOmegaTarget(
                double.IsNaN(_pitchRate) ? _commandOmega.x : _pitchRate,
                double.IsNaN(_yawRate) ? _commandOmega.z : _yawRate,
                double.IsNaN(_rollRate) ? _commandOmega.y : _rollRate);
        }

        // the end of the program that made the claim releases it; the interpreter's context (the terminal) never ends
        public bool OnPopContext(IProgramContext context)
        {
            if (_claimContext == null)
                return false;
            if (!ReferenceEquals(context, _claimContext))
                return true;

            Release();
            return false;
        }

        /// <summary>
        /// A KOSException for a reference frame that can't be used now: AttitudeGetReferenceRotation would otherwise drop every user of
        /// the attitude controller on its next tick.
        /// </summary>
        internal static void CheckReference(MechJebCore core, AttitudeReference reference)
        {
            switch (reference)
            {
                case AttitudeReference.TARGET:
                case AttitudeReference.RELATIVE_VELOCITY:
                case AttitudeReference.TARGET_ORIENTATION:
                    // a SET TARGET earlier in this physics tick may not have reached MechJeb yet
                    core.Target.SyncWithVesselTarget();
                    if (core.Target.Target == null)
                        throw new KOSException($"The {reference} attitude reference needs a target.");
                    break;
                case AttitudeReference.MANEUVER_NODE:
                case AttitudeReference.MANEUVER_NODE_COT:
                    if (core.vessel.patchedConicSolver == null || core.vessel.patchedConicSolver.maneuverNodes.Count == 0)
                        throw new KOSException($"The {reference} attitude reference needs a maneuver node.");
                    break;
            }
        }
    }
}
