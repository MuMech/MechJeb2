#nullable enable

using MechJebLib.Utils;

namespace MechJebLib.Simulations.PartModules
{
    public class SimProceduralFairingDecoupler : SimPartModule
    {
        private static readonly ObjectPool<SimProceduralFairingDecoupler> _pool = new ObjectPool<SimProceduralFairingDecoupler>(New, Clear);

        public bool IsDecoupled;

        public override void Dispose() => _pool.Release(this);

        public static SimProceduralFairingDecoupler Borrow(SimPart part)
        {
            SimProceduralFairingDecoupler decoupler = _pool.Borrow();
            decoupler.Part = part;
            return decoupler;
        }

        private static SimProceduralFairingDecoupler New() => new SimProceduralFairingDecoupler();

        private static void Clear(SimProceduralFairingDecoupler m)
        {
        }
    }
}
