namespace MechJebLib.Control
{
    public interface IPIDLoop
    {
        double Update(double r, double y);
    }
}
