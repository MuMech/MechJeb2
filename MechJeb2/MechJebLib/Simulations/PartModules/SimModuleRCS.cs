#nullable enable

using System.Collections.Generic;
using MechJebLib.Utils;

namespace MechJebLib.Simulations.PartModules
{
    public class SimModuleRCS : SimPartModule
    {
        private static readonly ObjectPool<SimModuleRCS>     _pool                = new ObjectPool<SimModuleRCS>(New, Clear);
        public readonly         List<SimPropellant>          Propellants          = new List<SimPropellant>();
        public readonly         Dictionary<int, SimFlowMode> PropellantFlowModes  = new Dictionary<int, SimFlowMode>();
        public readonly         Dictionary<int, double>      ResourceConsumptions = new Dictionary<int, double>();

        public double G;
        public double Isp;
        public double Thrust;
        public bool   RcsEnabled;

        public override void Dispose()
        {
            _pool.Release(this);
        }

        public static SimModuleRCS Borrow(SimPart part)
        {
            SimModuleRCS clamp = _pool.Borrow();
            clamp.Part = part;
            return clamp;
        }

        private static SimModuleRCS New()
        {
            return new SimModuleRCS();
        }

        public void Activate()
        {
            if (StagingEnabled && ModuleIsEnabled)
            {
                RcsEnabled = true;
            }
        }

        private static void Clear(SimModuleRCS m)
        {
        }
    }
}
