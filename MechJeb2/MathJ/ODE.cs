#nullable enable
using System.Collections.Generic;
using System.Threading;
using MechJebLib.Structs;

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

        public void Integrate(IList<double> y0, IList<double> yf, double t0, double tf, Hn? interpolant = null)
        {
            Integrator.Integrate(y0, yf, t0, tf, interpolant);
        }

        private void dydt_internal(IList<double> y, double x, IList<double> dy)
        {
            CancellationToken.ThrowIfCancellationRequested();

            dydt(y, x, dy);
        }

        // ReSharper disable once InconsistentNaming
        protected abstract void dydt(IList<double> y, double x, IList<double> dy);

        public Hn GetInterpolant()
        {
            return Hn.Get(N);
        }
    }
}
