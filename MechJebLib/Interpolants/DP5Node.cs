using MechJebLib.ODE;
using MechJebLib.Primitives;
using MechJebLib.Utils;

namespace MechJebLib.Interpolants
{
    internal class DP5Node : InterpolantNode
    {
        private static readonly ObjectPool<DP5Node> _pool = new ObjectPool<DP5Node>(New, Clear);

        public static DP5Node Rent(double t, double h, Vec y, Vec k1, Vec k3, Vec k4, Vec k5, Vec k6, Vec k7)
        {
            DP5Node node = _pool.Borrow();
            node.T = t;
            node.H = h;
            node.Y = y.Dup();
            node.N = y.Length;
            node._k1 = k1.Dup();
            node._k3 = k3.Dup();
            node._k4 = k4.Dup();
            node._k5 = k5.Dup();
            node._k6 = k6.Dup();
            node._k7 = k7.Dup();
            return node;
        }

        private DP5Node() { }

        private static DP5Node New() => new DP5Node();

        private static void Clear(DP5Node o)
        {
            // ReSharper disable once NullableWarningSuppressionIsUsed
            o.Y = o._k1 = o._k3 = o._k4 = o._k5 = o._k6 = o._k7 = null!;
            o.N = -1;
            o.T = 0;
            o.H = 0;
        }

        // ReSharper disable NullableWarningSuppressionIsUsed
        private Vec _k1 = null!;
        private Vec _k3 = null!;
        private Vec _k4 = null!;
        private Vec _k5 = null!;
        private Vec _k6 = null!;
        private Vec _k7 = null!;
        // ReSharper restore NullableWarningSuppressionIsUsed

        public override void Evaluate(double x, Vec yout) =>
            DP5Math.Interpolate(x, T, H, Y, _k1, _k3, _k4, _k5, _k6, _k7, yout);

        public override void Dispose()
        {
            base.Dispose();
            _k1.Dispose();
            _k3.Dispose();
            _k4.Dispose();
            _k5.Dispose();
            _k6.Dispose();
            _k7.Dispose();
            _pool.Release(this);
        }
    }
}
