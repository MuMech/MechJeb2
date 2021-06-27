#nullable enable
using System;
using System.Collections.Generic;

namespace MuMech.MathJ
{
    public abstract class ODESolver
    {
        public abstract void        Integrate(double[] y0, double[] yf, double t0, double tf, CN? interpolant = null);
        public abstract void        Initialize(Action<double[], double, double[]> dydt, int n, int m);
        public abstract void        AddEvent(Event e);
        public          List<Event> EventsFired { get; } = new List<Event>();
    }
}
