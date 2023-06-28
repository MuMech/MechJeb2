#nullable enable

using MechJebLib.Utils;

namespace MechJebLib.Simulations.PartModules
{
    public class SimLaunchClamp : SimPartModule
    {
        private static readonly ObjectPool<SimLaunchClamp> _pool = new ObjectPool<SimLaunchClamp>(New, Clear);

        public override void Dispose()
        {
            _pool.Release(this);
        }

        public static SimLaunchClamp Borrow(SimPart part)
        {
            SimLaunchClamp clamp = _pool.Borrow();
            clamp.Part = part;
            return clamp;
        }

        private static SimLaunchClamp New()
        {
            return new SimLaunchClamp();
        }

        private static void Clear(SimLaunchClamp m)
        {
        }
    }
}
