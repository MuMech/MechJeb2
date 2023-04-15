/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Copyright Sebastien Gaggini (sebastien.gaggini@gmail.com)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using System;
using System.Collections.Generic;
using MechJebLib.Primitives;
using MechJebLib.Utils;

#nullable enable

namespace MuMech.MechJebLib.Maths.ODE
{
    public abstract class ODESolver
    {
        public abstract void        Integrate(DD y0, DD yf, double t0, double tf, Hn? interpolant = null);
        public abstract void        Initialize(Action<DD, double, DD> dydt, int n);
        public abstract void        AddEvent(Event e);
        public          List<Event> EventsFired { get; } = new List<Event>();
    }
}
