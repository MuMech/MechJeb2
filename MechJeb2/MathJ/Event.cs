using System.Collections.Generic;

namespace MuMech.MathJ
{
    public abstract class Event
    {
        public virtual  bool   Stop    { get; set; } = true;
        public          bool   Enabled { get; set; } = true;
        public abstract double Evaluate(IList<double> y, double x);
    }
}
