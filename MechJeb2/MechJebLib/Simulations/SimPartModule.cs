#nullable enable

using System;

namespace MechJebLib.Simulations
{
    public abstract class SimPartModule : IDisposable
    {
        public bool    IsEnabled;
        public SimPart Part = null!;
        public bool    ModuleIsEnabled;
        public bool    StagingEnabled;

        public abstract void Dispose();
    }
}
