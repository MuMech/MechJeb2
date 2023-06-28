#nullable enable

using System;

namespace MechJebLib.Simulations
{
    public abstract class SimPartModule : IDisposable
    {
        public bool    IsEnabled;
        public SimPart Part = null!;

        public abstract void Dispose();
    }
}
