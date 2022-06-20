using MechJebLib.Primitives;

namespace MechJebLib.PVG.Terminal
{
    /// <summary>
    /// 6 constrant match to orbital state vectors.
    ///
    /// This may work to bootstrap a problem, but will not be very useful for closed loop guidance once the exact solution
    /// becomes impossible.
    /// </summary>
    public class Intercept6Cartesian : IPVGTerminal
    {
        private V3 rT;
        private V3 vT;

        public Intercept6Cartesian(V3 rT, V3 vT)
        {
            this.rT = rT;
            this.vT = vT;
        }
        
        public (double a, double b, double c, double d, double e, double f) TerminalConstraints(ArrayWrapper yf)
        {
            V3 rmiss = yf.R - rT;
            V3 vmiss = yf.V - vT;

            return (rmiss[0], rmiss[1], rmiss[2], vmiss[0], vmiss[1], vmiss[2]);
        }
        
    }
}
