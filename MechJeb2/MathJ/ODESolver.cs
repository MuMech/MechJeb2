#nullable enable
using System;
using System.Collections.Generic;
using MechJebLib.Primitives;
using MechJebLib.Utils;

namespace MuMech.MathJ
{
    public abstract class ODESolver
    {
        public abstract void        Integrate(DD y0, DD yf, double t0, double tf, Hn? interpolant = null);
        public abstract void        Initialize(Action<DD, double, DD> dydt, int n);
        public abstract void        AddEvent(Event e);
        public          List<Event> EventsFired { get; } = new List<Event>();
    }
}
