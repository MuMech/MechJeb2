extern alias JetBrainsAnnotations;
using JetBrainsAnnotations::JetBrains.Annotations;

namespace MuMech
{
    /// <summary>
    ///     The throttle limits the thrust controller applies. The controller's own values (Utilities) apply while no autopilot has
    ///     given it its own set (MechJebModuleThrustController.SetLimitsOverride): then the controller reads that set instead, and
    ///     its own values stay as they are.
    /// </summary>
    public interface IThrustLimits
    {
        bool LimitToPreventOverheats { get; set; }

        bool LimitDynamicPressure { get; set; }

        EditableDouble MaxDynamicPressure { get; }

        bool LimitAcceleration { get; set; }

        EditableDouble MaxAcceleration { get; }

        bool LimitThrottle { get; set; }

        EditableDoubleMult MaxThrottle { get; }

        bool LimiterMinThrottle { get; set; }

        EditableDoubleMult MinThrottle { get; }

        bool ElectricThrottle { get; set; }

        EditableDoubleMult ElectricThrottleLo { get; }

        EditableDoubleMult ElectricThrottleHi { get; }

        bool LimitToPreventUnstableIgnition { get; set; }

        bool AutoRCSUllaging { get; set; }
    }

    public static class ThrustLimits
    {
        /// <summary>Copies every limit of <paramref name="from" /> into <paramref name="to" />.</summary>
        public static void Copy(IThrustLimits from, IThrustLimits to)
        {
            to.LimitToPreventOverheats        = from.LimitToPreventOverheats;
            to.LimitDynamicPressure           = from.LimitDynamicPressure;
            to.MaxDynamicPressure.Val         = from.MaxDynamicPressure.Val;
            to.LimitAcceleration              = from.LimitAcceleration;
            to.MaxAcceleration.Val            = from.MaxAcceleration.Val;
            to.LimitThrottle                  = from.LimitThrottle;
            to.MaxThrottle.Val                = from.MaxThrottle.Val;
            to.LimiterMinThrottle             = from.LimiterMinThrottle;
            to.MinThrottle.Val                = from.MinThrottle.Val;
            to.ElectricThrottle               = from.ElectricThrottle;
            to.ElectricThrottleLo.Val         = from.ElectricThrottleLo.Val;
            to.ElectricThrottleHi.Val         = from.ElectricThrottleHi.Val;
            to.LimitToPreventUnstableIgnition = from.LimitToPreventUnstableIgnition;
            to.AutoRCSUllaging                = from.AutoRCSUllaging;
        }
    }

    /// <summary>A module's own throttle limits, saved with the module's settings: the defaults are the thrust controller's.</summary>
    public class ThrustLimitSet : IThrustLimits
    {
        [UsedImplicitly, Persistent]
        public bool LimitToPreventOverheats;

        [UsedImplicitly, Persistent]
        public bool LimitDynamicPressure;

        [UsedImplicitly, Persistent]
        public readonly EditableDouble MaxDynamicPressure = new EditableDouble(20000);

        [UsedImplicitly, Persistent]
        public bool LimitAcceleration;

        [UsedImplicitly, Persistent]
        public readonly EditableDouble MaxAcceleration = 40;

        [UsedImplicitly, Persistent]
        public bool LimitThrottle;

        [UsedImplicitly, Persistent]
        public readonly EditableDoubleMult MaxThrottle = new EditableDoubleMult(1, 0.01);

        [UsedImplicitly, Persistent]
        public bool LimiterMinThrottle;

        [UsedImplicitly, Persistent]
        public readonly EditableDoubleMult MinThrottle = new EditableDoubleMult(0.05, 0.01);

        [UsedImplicitly, Persistent]
        public bool ElectricThrottle;

        [UsedImplicitly, Persistent]
        public readonly EditableDoubleMult ElectricThrottleLo = new EditableDoubleMult(0.05, 0.01);

        [UsedImplicitly, Persistent]
        public readonly EditableDoubleMult ElectricThrottleHi = new EditableDoubleMult(0.15, 0.01);

        [UsedImplicitly, Persistent]
        public bool LimitToPreventUnstableIgnition;

        [UsedImplicitly, Persistent]
        public bool AutoRCSUllaging = true;

        bool IThrustLimits.LimitToPreventOverheats { get => LimitToPreventOverheats; set => LimitToPreventOverheats = value; }
        bool IThrustLimits.LimitDynamicPressure { get => LimitDynamicPressure; set => LimitDynamicPressure = value; }
        EditableDouble IThrustLimits.MaxDynamicPressure => MaxDynamicPressure;
        bool IThrustLimits.LimitAcceleration { get => LimitAcceleration; set => LimitAcceleration = value; }
        EditableDouble IThrustLimits.MaxAcceleration => MaxAcceleration;
        bool IThrustLimits.LimitThrottle { get => LimitThrottle; set => LimitThrottle = value; }
        EditableDoubleMult IThrustLimits.MaxThrottle => MaxThrottle;
        bool IThrustLimits.LimiterMinThrottle { get => LimiterMinThrottle; set => LimiterMinThrottle = value; }
        EditableDoubleMult IThrustLimits.MinThrottle => MinThrottle;
        bool IThrustLimits.ElectricThrottle { get => ElectricThrottle; set => ElectricThrottle = value; }
        EditableDoubleMult IThrustLimits.ElectricThrottleLo => ElectricThrottleLo;
        EditableDoubleMult IThrustLimits.ElectricThrottleHi => ElectricThrottleHi;
        bool IThrustLimits.LimitToPreventUnstableIgnition { get => LimitToPreventUnstableIgnition; set => LimitToPreventUnstableIgnition = value; }
        bool IThrustLimits.AutoRCSUllaging { get => AutoRCSUllaging; set => AutoRCSUllaging = value; }
    }

    /// <summary>
    ///     Another set's limits without its throttle cap and electric limit, which read as off: PSG plans the ascent at full thrust
    ///     and manages the throttle itself. Writes go to the other set.
    /// </summary>
    public class ThrustLimitsWithoutThrottleCap : IThrustLimits
    {
        private readonly IThrustLimits _inner;

        public ThrustLimitsWithoutThrottleCap(IThrustLimits inner) => _inner = inner;

        public bool LimitToPreventOverheats { get => _inner.LimitToPreventOverheats; set => _inner.LimitToPreventOverheats = value; }
        public bool LimitDynamicPressure { get => _inner.LimitDynamicPressure; set => _inner.LimitDynamicPressure = value; }
        public EditableDouble MaxDynamicPressure => _inner.MaxDynamicPressure;
        public bool LimitAcceleration { get => _inner.LimitAcceleration; set => _inner.LimitAcceleration = value; }
        public EditableDouble MaxAcceleration => _inner.MaxAcceleration;
        public bool LimitThrottle { get => false; set => _inner.LimitThrottle = value; }
        public EditableDoubleMult MaxThrottle => _inner.MaxThrottle;
        public bool LimiterMinThrottle { get => _inner.LimiterMinThrottle; set => _inner.LimiterMinThrottle = value; }
        public EditableDoubleMult MinThrottle => _inner.MinThrottle;
        public bool ElectricThrottle { get => false; set => _inner.ElectricThrottle = value; }
        public EditableDoubleMult ElectricThrottleLo => _inner.ElectricThrottleLo;
        public EditableDoubleMult ElectricThrottleHi => _inner.ElectricThrottleHi;
        public bool LimitToPreventUnstableIgnition { get => _inner.LimitToPreventUnstableIgnition; set => _inner.LimitToPreventUnstableIgnition = value; }
        public bool AutoRCSUllaging { get => _inner.AutoRCSUllaging; set => _inner.AutoRCSUllaging = value; }
    }

    /// <summary>
    ///     Another set's limits with a module's own minimum throttle ("Keep limited throttle over"): the landing autopilot keeps its
    ///     own and takes the general others. Writes to the minimum throttle go to the module's values, the rest to the other set.
    /// </summary>
    public class ThrustLimitsWithOwnMinThrottle : IThrustLimits
    {
        private readonly IThrustLimits      _inner;
        private readonly System.Func<bool>   _getMin;
        private readonly System.Action<bool> _setMin;
        private readonly EditableDoubleMult _min;

        public ThrustLimitsWithOwnMinThrottle(IThrustLimits inner, System.Func<bool> getMin, System.Action<bool> setMin, EditableDoubleMult min)
        {
            _inner  = inner;
            _getMin = getMin;
            _setMin = setMin;
            _min    = min;
        }

        public bool LimitToPreventOverheats { get => _inner.LimitToPreventOverheats; set => _inner.LimitToPreventOverheats = value; }
        public bool LimitDynamicPressure { get => _inner.LimitDynamicPressure; set => _inner.LimitDynamicPressure = value; }
        public EditableDouble MaxDynamicPressure => _inner.MaxDynamicPressure;
        public bool LimitAcceleration { get => _inner.LimitAcceleration; set => _inner.LimitAcceleration = value; }
        public EditableDouble MaxAcceleration => _inner.MaxAcceleration;
        public bool LimitThrottle { get => _inner.LimitThrottle; set => _inner.LimitThrottle = value; }
        public EditableDoubleMult MaxThrottle => _inner.MaxThrottle;
        public bool LimiterMinThrottle { get => _getMin(); set => _setMin(value); }
        public EditableDoubleMult MinThrottle => _min;
        public bool ElectricThrottle { get => _inner.ElectricThrottle; set => _inner.ElectricThrottle = value; }
        public EditableDoubleMult ElectricThrottleLo => _inner.ElectricThrottleLo;
        public EditableDoubleMult ElectricThrottleHi => _inner.ElectricThrottleHi;
        public bool LimitToPreventUnstableIgnition { get => _inner.LimitToPreventUnstableIgnition; set => _inner.LimitToPreventUnstableIgnition = value; }
        public bool AutoRCSUllaging { get => _inner.AutoRCSUllaging; set => _inner.AutoRCSUllaging = value; }
    }
}
