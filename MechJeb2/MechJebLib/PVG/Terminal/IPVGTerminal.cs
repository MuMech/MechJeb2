namespace MechJebLib.PVG.Terminal
{
    public interface IPVGTerminal
    {
        public (double a, double b, double c, double d, double e, double f) TerminalConstraints(ArrayWrapper yf);
    }
}
