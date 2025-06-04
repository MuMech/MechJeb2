using System;

namespace MechJebLib.Control
{
    public class MultiVariableGrid
    {
        public double[] GrrValues { get; }
        public double[] TsValues  { get; }
        public double[] MValues   { get; }

        public MultiVariableGrid(double[] grrValues, double[] tsValues, double[] mValues)
        {
            GrrValues = new double[grrValues.Length];
            TsValues  = tsValues;
            MValues   = new double[mValues.Length];

            for (int i = 0; i < grrValues.Length; i++)
                GrrValues[i] = Math.Log(grrValues[i]);
            for (int i = 0; i < mValues.Length; i++)
                MValues[i] = Math.Log(mValues[i]);
        }

        public (int Grr, int Ts, int M) GetDimensions() => (GrrValues.Length, TsValues.Length, MValues.Length);
    }
}
