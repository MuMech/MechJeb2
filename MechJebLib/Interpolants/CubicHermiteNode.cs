using MechJebLib.Primitives;
using MechJebLib.Utils;

namespace MechJebLib.Interpolants
{
    internal class CubicHermiteNode : InterpolantNode
    {
        private static readonly ObjectPool<CubicHermiteNode> _pool = new ObjectPool<CubicHermiteNode>(New, Clear);

        public static CubicHermiteNode Rent(double t, double h, Vec y, Vec dy, Vec ynew, Vec dynew)
        {
            CubicHermiteNode node = _pool.Borrow();
            node.T = t;
            node.H = h;
            node.Y = y.Dup();
            node.N = y.Length;
            node._dy = dy.Dup();
            node._ynew = ynew.Dup();
            node._dynew = dynew.Dup();
            return node;
        }

        private CubicHermiteNode() { }

        private static CubicHermiteNode New() => new CubicHermiteNode();

        private static void Clear(CubicHermiteNode o)
        {
            // ReSharper disable once NullableWarningSuppressionIsUsed
            o.Y = o._dy = o._ynew = o._dynew = null!;
            o.N = -1;
            o.T = 0;
            o.H = 0;
        }

        // ReSharper disable NullableWarningSuppressionIsUsed
        private Vec _dy = null!;
        private Vec _ynew = null!;
        private Vec _dynew = null!;
        // ReSharper restore NullableWarningSuppressionIsUsed

        public override void Evaluate(double x, Vec yout) =>
            yout.CubicHermiteInterpolant(T, Y, _dy, T + H, _ynew, _dynew, x);

        public override void Dispose()
        {
            base.Dispose();
            _dy.Dispose();
            _ynew.Dispose();
            _dynew.Dispose();
            _pool.Release(this);
        }
    }
}
