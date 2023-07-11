#nullable enable

namespace MechJebLib.Simulations
{
    public struct SimResource
    {
        public bool   Free;
        public double MaxAmount;
        public double Amount;
        public int    Id;
        public double Density;
        public double Residual;

        public double ResidualThreshold => Residual * MaxAmount;

        public SimResource Drain(double resourceDrain)
        {
            Amount -= resourceDrain;
            if (Amount < 0) Amount = 0;
            return this;
        }
    }
}
