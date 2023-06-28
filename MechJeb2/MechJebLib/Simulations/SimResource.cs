#nullable enable

namespace MechJebLib.Simulations
{
    public class SimResource
    {
        public bool   Electricity;
        public bool   Free;
        public double MaxAmount;
        public double Amount;
        public int    Id;
        public double Density;
        public double Residual;

        public double ResidualThreshold => Residual * MaxAmount;

        public void Drain(double resourceDrain)
        {
            Amount -= resourceDrain;
            if (Amount < 0) Amount = 0;
        }
    }
}
