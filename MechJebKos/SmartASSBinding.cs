/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Linq;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;
using Target = MuMech.MechJebModuleSmartASS.Target;

namespace MuMech.MechJebKos
{
    // ADDONS:MECHJEB:SMARTASS - SmartASS (MechJebModuleSmartASS), the same one its window shows: what a script selects lights up in
    // the window, the pilot can change it there, and it stays on after the script ends (TARGET "OFF" turns it off).
    //
    // Setting TARGET engages it, like the window's buttons; changing an angle or option while SmartASS steers applies it at once.
    //
    //   SET ss TO ADDONS:MECHJEB:SMARTASS.
    //   SET ss:TARGET TO "PROGRADE".
    //   ss:SURFACE(90, 45, 0).               // heading 90, pitch 45, roll 0
    //   SET ss:SURFACEHEADING TO 80.
    //   SET ss:TARGET TO "OFF".
    //
    // SmartASS steps aside while another MechJeb autopilot steers (its window shows AUTO), and engaging it then throws. Engaging it
    // ends this processor's ADDONS:MECHJEB:ATTITUDE control. For attitudes SmartASS can't hold, use ADDONS:MECHJEB:ATTITUDE.
    [KOSNomenclature("MechJebSmartASS")]
    public class SmartASSBinding : ComputerModuleBinding<MechJebModuleSmartASS>
    {
        // the targets a script can choose: AUTO is only what the window shows while another module steers
        private static readonly Target[] _targets = ((Target[])Enum.GetValues(typeof(Target))).Where(t => t != Target.AUTO).ToArray();

        private readonly AttitudeBinding _attitude;

        public SmartASSBinding(Func<MechJebCore?> core, AttitudeBinding attitude) : base(core)
        {
            _attitude = attitude;
        }

        protected override void InitializeSuffixes()
        {
            AddSuffix("TARGET", new SetSuffix<StringValue>(() => Module.target.ToString(),
                value => SetTarget(KosArgs.ParseEnum(value, "SmartASS target", _targets)),
                "What SmartASS points at, one of TARGETS (OFF, KILLROT, NODE, PROGRADE, SURFACE, ...); setting it engages SmartASS."));
            AddSuffix("TARGETS", new Suffix<ListValue>(() => new ListValue(_targets.Select(t => (Structure)new StringValue(t.ToString()))),
                "The TARGET values."));
            AddSuffix("ENGAGE", new NoArgsVoidSuffix(() => SetTarget(Module.target),
                "Engage SmartASS on its TARGET again (taking up the current attitude for KILLROT)."));
            AddSuffix("SURFACE", new ThreeArgsSuffix<ScalarValue, ScalarValue, ScalarValue>(Surface,
                "Set SURFACEHEADING, SURFACEPITCH and SURFACEROLL and engage the SURFACE target."));
            AddSuffix("ENGAGED", new Suffix<BooleanValue>(() => Module.target != Target.OFF && Core.Attitude.Users.Contains(Module),
                "True while SmartASS steers (or would, but for another module: see OVERRIDDEN)."));
            AddSuffix("OVERRIDDEN", new Suffix<BooleanValue>(() => Module.AttitudeHeldByOthers(),
                "True while another module (an autopilot, a kOS ATTITUDE command) steers: the window shows AUTO."));

            // SURFACE target
            Angle("SURFACEHEADING", s => s.srfHdg, "Compass heading (degrees) for the SURFACE target.");
            Angle("SURFACEPITCH", s => s.srfPit, "Pitch above the horizon (degrees) for the SURFACE target.");
            Angle("SURFACEROLL", s => s.srfRol, "Roll (degrees) for the SURFACE target.");

            // SURFACE_PROGRADE and SURFACE_RETROGRADE targets
            Angle("VELOCITYYAW", s => s.srfVelYaw, "Yaw (degrees) off the surface velocity for SURFACE_PROGRADE and SURFACE_RETROGRADE.");
            Angle("VELOCITYPITCH", s => s.srfVelPit, "Pitch (degrees) off the surface velocity for SURFACE_PROGRADE and SURFACE_RETROGRADE.");
            Angle("VELOCITYROLL", s => s.srfVelRol, "Roll (degrees) for SURFACE_PROGRADE and SURFACE_RETROGRADE.");

            Option("FORCEPITCH", s => s.forcePitch, (s, v) => s.forcePitch = v,
                "Control pitch on the surface targets (false: leave it to the pilot).");
            Option("FORCEYAW", s => s.forceYaw, (s, v) => s.forceYaw = v,
                "Control yaw (heading) on the surface targets (false: leave it to the pilot).");
            Option("FORCEROLL", s => s.forceRol, (s, v) => s.forceRol = v,
                "Hold a roll: ROLL on the direction targets (PROGRADE, NORMAL_PLUS, ...), the target's roll on the surface targets.");
            Angle("ROLL", s => s.rol, "Roll (degrees) held on the direction targets when FORCEROLL.");

            // ADVANCED target
            AddSuffix("ADVANCEDREFERENCE", new SetSuffix<StringValue>(() => Module.advReference.ToString(), value =>
                {
                    AttitudeReference reference = KosArgs.ParseEnum<AttitudeReference>(value, "attitude reference");
                    if (Module.target == Target.ADVANCED && Steering())
                        AttitudeBinding.CheckReference(Core, reference);
                    Module.advReference = reference;
                    Changed();
                },
                "Reference frame for the ADVANCED target (INERTIAL, ORBIT, SURFACE_NORTH, TARGET, MANEUVER_NODE, SUN, ...)."));
            AddSuffix("ADVANCEDDIRECTION", new SetSuffix<StringValue>(() => Module.advDirection.ToString(), value =>
                {
                    Module.advDirection = KosArgs.ParseEnum<Vector6.Direction>(value, "direction");
                    Changed();
                },
                "Axis of ADVANCEDREFERENCE to point at for the ADVANCED target: FORWARD, BACK, UP, DOWN, RIGHT or LEFT."));

            // smooth control
            AddSuffix("SMOOTHCONTROL", new SetSuffix<BooleanValue>(() => Module.smoothControl, value => SetSmoothControl(value),
                "Turn toward the surface targets at SMOOTHRATE. Only for SURFACE, SURFACE_PROGRADE and SURFACE_RETROGRADE: set TARGET first."));
            AddSuffix("SMOOTHRATE", new SetSuffix<ScalarValue>(() => Module.degreesPerSecond.Val, value =>
                {
                    Module.degreesPerSecond.Val = KosArgs.Finite(value, "SMOOTHRATE");
                    Changed();
                },
                "Turn rate (degrees per second) of SMOOTHCONTROL."));

            AddSuffix("AUTODISABLE", new SetSuffix<BooleanValue>(() => Module.autoDisableSmartASS, value => Module.autoDisableSmartASS = value,
                "Turn SmartASS off when another module takes the attitude, so it doesn't come back when that one finishes."));
        }

        private void Angle(string name, Func<MechJebModuleSmartASS, EditableDouble> field, string description) =>
            AddSuffix(name, new SetSuffix<ScalarValue>(() => field(Module).Val, value =>
            {
                field(Module).Val = KosArgs.Finite(value, name);
                Changed();
            }, description));

        private void Option(string name, Func<MechJebModuleSmartASS, bool> get, Action<MechJebModuleSmartASS, bool> set, string description) =>
            AddSuffix(name, new SetSuffix<BooleanValue>(() => get(Module), value =>
            {
                set(Module, value);
                Changed();
            }, description));

        private void Surface(ScalarValue heading, ScalarValue pitch, ScalarValue roll)
        {
            double h = KosArgs.Finite(heading, "SURFACE heading");
            double p = KosArgs.Finite(pitch, "SURFACE pitch");
            double r = KosArgs.Finite(roll, "SURFACE roll");

            MechJebModuleSmartASS ss = Module;
            ss.srfHdg.Val = h;
            ss.srfPit.Val = p;
            ss.srfRol.Val = r;
            SetTarget(Target.SURFACE);
        }

        // SmartASS steers now: so a changed setting is applied at once
        private bool Steering()
        {
            MechJebModuleSmartASS ss = Module;
            return ss.target != Target.OFF && Core.Attitude.Users.Contains(ss) && !ss.AttitudeHeldByOthers();
        }

        // as the window's +/- buttons: engage again, without resetting the controller
        private void Changed()
        {
            if (Steering())
                Module.Engage(false);
        }

        private void SetTarget(Target target)
        {
            MechJebModuleSmartASS ss = Module;

            if (target == Target.OFF)
            {
                ss.target = Target.OFF;
                // OFF drops every user of the attitude controller: leave another module's control alone, as the window does (it
                // only shows AUTO then)
                if (ss.AttitudeHeldByOthers())
                    Core.Attitude.Users.Remove(ss);
                else
                    ss.Engage();
                return;
            }

            // this processor's own ATTITUDE claim gives way below; anyone else's would make SmartASS step aside at once
            if (Core.Attitude.Enabled && Core.Attitude.Users.Any(u => !ReferenceEquals(u, ss) && !ReferenceEquals(u, _attitude)))
                throw new KOSException("Another MechJeb module controls the attitude (SmartASS shows AUTO); SmartASS can't engage now.");

            AttitudeBinding.CheckReference(Core, ReferenceOf(target, ss));

            // SmartASS would see this processor's claim as another user and step aside
            _attitude.Release();

            ss.target = target;
            // the window's tab for the target: the surface targets' Force toggles and smooth control apply only in the SURFACE mode
            ss.mode = MechJebModuleSmartASS.Target2Mode[(int)target];
            ss.Engage();
        }

        private void SetSmoothControl(bool smooth)
        {
            MechJebModuleSmartASS ss = Module;
            // SmartASS turns it off again every tick on the other targets
            if (smooth && !ss.ShowsSurfaceAngles)
                throw new KOSException("SMOOTHCONTROL is only for the SURFACE, SURFACE_PROGRADE and SURFACE_RETROGRADE targets: set TARGET first.");

            ss.smoothControl = smooth;
            Changed();
        }

        // the reference frame a target steers in, to check it can be used now
        private static AttitudeReference ReferenceOf(Target target, MechJebModuleSmartASS ss)
        {
            switch (target)
            {
                case Target.NODE:
                    return AttitudeReference.MANEUVER_NODE;
                case Target.RELATIVE_PLUS:
                case Target.RELATIVE_MINUS:
                    return AttitudeReference.RELATIVE_VELOCITY;
                case Target.TARGET_PLUS:
                case Target.TARGET_MINUS:
                    return AttitudeReference.TARGET;
                case Target.PARALLEL_PLUS:
                case Target.PARALLEL_MINUS:
                    return AttitudeReference.TARGET_ORIENTATION;
                case Target.ADVANCED:
                    return ss.advReference;
                default:
                    return AttitudeReference.ORBIT; // needs nothing
            }
        }
    }
}
