#nullable enable

using MechJebLib.Utils;

namespace MechJebLib.Simulations.PartModules
{
    public class SimModuleDecouple : SimPartModule
    {
        private static readonly ObjectPool<SimModuleDecouple> _pool = new ObjectPool<SimModuleDecouple>(New, Clear);

        public bool     IsDecoupled;
        public bool     StagingEnabled;
        public bool     IsOmniDecoupler;
        public bool     Staged;
        public SimPart? AttachedPart;

        public override void Dispose()
        {
            _pool.Release(this);
        }

        public static SimModuleDecouple Borrow(SimPart part)
        {
            SimModuleDecouple decoupler = _pool.Borrow();
            decoupler.Part = part;
            return decoupler;
        }

        private static SimModuleDecouple New()
        {
            return new SimModuleDecouple();
        }

        private static void Clear(SimModuleDecouple m)
        {
            m.AttachedPart = null;
        }
    }
}
