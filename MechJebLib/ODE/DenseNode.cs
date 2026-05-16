using System;
using MechJebLib.Primitives;

namespace MechJebLib.ODE
{
    public abstract class DenseNode : IDisposable
    {
        public double T; // left endpoint
        public int N;
        public double H; // signed step (Habs * Direction)

        // ReSharper disable once NullableWarningSuppressionIsUsed
        public Vec Y = null!; // y at T

        public abstract void Evaluate(double t, Vec yout);

        public virtual void Dispose() => Y.Dispose();
    }

    /// <summary>
    ///     This is a fake "interpolant" for zero-length t0 == tf "integration".
    /// </summary>
    public class ConstantNode : DenseNode
    {
        public ConstantNode(double t, Vec y)
        {
            T = t;
            Y = y.Dup();
            N = y.Length;
            H = 0;
        }

        public override void Evaluate(double t, Vec yout) => yout.CopyFrom(Y);
    }
}
