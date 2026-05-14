using System;
using MechJebLib.Primitives;

namespace MechJebLib.Interpolants
{
    public interface IInterpolant : IDisposable
    {
        double MaxT { get; }
        double MinT { get; }
        Vec    Evaluate(double x);
    }
}
