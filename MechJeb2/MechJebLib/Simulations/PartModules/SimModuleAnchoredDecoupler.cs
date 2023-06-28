#nullable enable

using MechJebLib.Utils;

namespace MechJebLib.Simulations.PartModules
{
    public class SimModuleAnchoredDecoupler : SimPartModule
    {
        private static readonly ObjectPool<SimModuleAnchoredDecoupler> _pool = new ObjectPool<SimModuleAnchoredDecoupler>(New, Clear);

        public bool     IsDecoupled;
        public bool     StagingEnabled;
        public bool     Staged;
        public SimPart? AttachedPart;

        public override void Dispose()
        {
            _pool.Release(this);
        }

        public static SimModuleAnchoredDecoupler Borrow(SimPart part)
        {
            SimModuleAnchoredDecoupler decoupler = _pool.Borrow();
            decoupler.Part = part;
            return decoupler;
        }

        private static SimModuleAnchoredDecoupler New()
        {
            return new SimModuleAnchoredDecoupler();
        }

        private static void Clear(SimModuleAnchoredDecoupler m)
        {
            m.AttachedPart = null;
        }
    }
}
