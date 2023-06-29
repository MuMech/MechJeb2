#nullable enable

using MechJebLib.Utils;

namespace MechJebLib.Simulations.PartModules
{
    public class SimModuleDockingNode : SimPartModule
    {
        private static readonly ObjectPool<SimModuleDockingNode> _pool = new ObjectPool<SimModuleDockingNode>(New, Clear);

        public bool     Staged;
        public SimPart? AttachedPart;

        public override void Dispose()
        {
            _pool.Release(this);
        }

        public static SimModuleDockingNode Borrow(SimPart part)
        {
            SimModuleDockingNode decoupler = _pool.Borrow();
            decoupler.Part = part;
            return decoupler;
        }

        private static SimModuleDockingNode New()
        {
            return new SimModuleDockingNode();
        }

        private static void Clear(SimModuleDockingNode m)
        {
            m.AttachedPart = null;
        }
    }
}
