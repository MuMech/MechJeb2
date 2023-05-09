#nullable enable

using System;
using System.Collections.Generic;

namespace MechJebLib.Core.ODE
{
    using ConditionFunc = Func<double, IList<double>, AbstractIVP, double>;
    using AssertFunc = Action<AbstractIVP>;

    public class Event : IComparable<Event>
    {
        public readonly ConditionFunc F;
        public readonly AssertFunc?   Assert;
        public          bool          SaveBefore = true;
        public          bool          SaveAfter  = true;
        public          bool          Terminal   = true;
        public          int           Direction  = 0;
        public          double        LastValue;
        public          double        NewValue;
        public          double        Time;

        public Event(ConditionFunc f, AssertFunc? assert = null)
        {
            F      = f;
            Assert = assert;
        }

        public int CompareTo(Event other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            return Time.CompareTo(other.Time);
        }
    }
}
