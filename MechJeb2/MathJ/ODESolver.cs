#nullable enable
using System;
using System.Collections.Generic;
using MechJebLib.Structs;

namespace MuMech.MathJ
{
    public abstract class ODESolver
    {
        public abstract void        Integrate(IList<double> y0, IList<double> yf, double t0, double tf, Hn? interpolant = null);
        public abstract void        Initialize(Action<IList<double>, double, IList<double>> dydt, int n);
        public abstract void        AddEvent(Event e);
        public          List<Event> EventsFired { get; } = new List<Event>();
    }
}
