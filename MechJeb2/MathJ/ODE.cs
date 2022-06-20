#nullable enable
using System.Collections.Generic;
using System.Threading;
using MechJebLib.Primitives;
using MechJebLib.Utils;

//using UnityEngine;

namespace MuMech.MathJ
{
    public abstract class ODE<T> where T : ODESolver, new()
    {
        public abstract int N { get; }

        public CancellationToken CancellationToken { get; set; }

        public bool FiredEvent(Event e)
        {
            return Integrator.EventsFired.Contains(e);
        }

        public readonly T Integrator;

        protected ODE(ICollection<Event>? events = null)
        {
            Integrator = new T();
            // ReSharper disable VirtualMemberCallInConstructor
            Integrator.Initialize(dydt_internal, N);

            if (events != null)
                foreach (Event e in events)
                    Integrator.AddEvent(e);
        }

        public void Integrate(DD y0, DD yf, double t0, double tf, Hn? interpolant = null)
        {
            Integrator.Integrate(y0, yf, t0, tf, interpolant);
        }

        private void dydt_internal(DD y, double x, DD dy)
        {
            CancellationToken.ThrowIfCancellationRequested();

            dydt(y, x, dy);
        }

        // ReSharper disable once InconsistentNaming
        protected abstract void dydt(DD y, double x, DD dy);

        public Hn GetInterpolant()
        {
            return Hn.Get(N);
        }
    }
}
