namespace MechJebLib.Control
{
    public interface IPIDLoop
    {
        public double Update(double r, double y);
    }
}
