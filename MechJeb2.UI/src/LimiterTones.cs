using System;
using MechJebLibBindings;
using UnityEngine;
using Tone = KodeKit.KodeWindow.Tone;
using static MuMech.MechJebModuleThrustController;

namespace MuMech.UI
{
    /// <summary>
    /// The colours of the thrust limiter labels, as MechJebModuleThrustController's info items draw them: green while
    /// that limiter acts, with the extra states of the throttle, min-throttle, electric and differential rows. Shared by
    /// the classic builders (GeneralItemBuilders) and the modern rows (ModernRows, Landing, Utilities).
    /// </summary>
    internal static class LimiterTones
    {
        public static Tone Of(MechJebModuleThrustController t, LimitMode mode) => t.Limiter == mode ? Tone.Good : Tone.Normal;

        public static Func<Tone> For(MechJebModuleThrustController t, LimitMode mode) => () => Of(t, mode);

        /// <summary>Red while the throttle limit acts at 0 %.</summary>
        public static Tone Throttle(MechJebModuleThrustController t) =>
            t.Limiter == LimitMode.THROTTLE ? t.MaxThrottle > 0d ? Tone.Good : Tone.Bad : Tone.Normal;

        /// <summary>Yellow when the setting looks wrong for the fuel model.</summary>
        public static Tone MinThrottle(MechJebModuleThrustController t)
        {
            if (t.Limiter == LimitMode.MIN_THROTTLE) return Tone.Good;
            // RF users should almost always have this set, "stock" users should almost always never have this set
            bool warn = ReflectionUtils.IsLoadedRealFuels ? !t.LimiterMinThrottle || t.MinThrottle <= 0d : t.LimiterMinThrottle && t.MinThrottle > 0d;
            return warn ? Tone.Warn : Tone.Normal;
        }

        /// <summary>Red or yellow while limiting, green while an electric engine runs (checked every 0.25 s: it scans the parts).</summary>
        public static Func<Tone> Electric(MechJebModuleThrustController t)
        {
            float nextCheck = 0;
            bool running = false;
            return () =>
            {
                if (t.Limiter == LimitMode.ELECTRIC) return t.ThrottleLimit < 0.001 ? Tone.Bad : Tone.Warn;
                if (Time.unscaledTime >= nextCheck)
                {
                    nextCheck = Time.unscaledTime + 0.25f;
                    running = t.ElectricEngineRunning();
                }

                return running ? Tone.Good : Tone.Normal;
            };
        }

        /// <summary>After lift-off: green when the differential throttle found a solution, yellow when it failed.</summary>
        public static Tone Differential(MechJebModuleThrustController t)
        {
            Vessel v = t.Vessel;
            if (!t.DifferentialThrottle || v == null || !v.LiftedOff()) return Tone.Normal;
            return t.DifferentialThrottleSuccess == DifferentialThrottleStatus.SUCCESS ? Tone.Good : Tone.Warn;
        }

        /// <summary>The differential throttle switch's setter: turning it off releases the engines.</summary>
        public static Action<bool> SetDifferential(MechJebModuleThrustController t) => v =>
        {
            bool old = t.DifferentialThrottle;
            t.DifferentialThrottle = v;
            if (old && !v) t.DisableDifferentialThrottle();
        };
    }
}
