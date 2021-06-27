#nullable enable
using System.Collections.Generic;
using System.Threading;

//using UnityEngine;

namespace MuMech.MathJ
{
    public abstract class ODE<T> where T : ODESolver, new()
    {
        public abstract int N { get; }
        public abstract int M { get; }

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
            Integrator.Initialize(dydt_internal, N, M);

            if (events != null)
                foreach (Event e in events)
                    Integrator.AddEvent(e);
        }

        public void Integrate(double[] y0, double[] yf, double t0, double tf, CN? interpolant = null)
        {
            Integrator.Integrate(y0, yf, t0, tf, interpolant);
        }

        private void dydt_internal(double[] y, double x, double[] dy)
        {
            CancellationToken.ThrowIfCancellationRequested();

            dydt(y, x, dy);
        }

        // ReSharper disable once InconsistentNaming
        protected abstract void dydt(double[] y, double x, double[] dy);

        public CN GetInterpolant()
        {
            return new CN(N + M) {AutotangentOffset = N};
        }
    }
}
