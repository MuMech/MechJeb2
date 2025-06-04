using System;

namespace MechJebLib.Control
{
    public class MultiVariableInterpolator
    {
        private readonly MultiVariableGrid _grid;
        private readonly double[,,]        _kValues; // Known K values at grid points

        public MultiVariableInterpolator(MultiVariableGrid grid, double[,,] kValues)
        {
            _grid    = grid;
            _kValues = kValues;

            (int Grr, int Ts, int M) dims = grid.GetDimensions();
            if (kValues.GetLength(0) != dims.Grr ||
                kValues.GetLength(1) != dims.Ts ||
                kValues.GetLength(2) != dims.M)
                throw new ArgumentException("K values array dimensions do not match grid dimensions");
        }

        public double Interpolate(double grr, double ts, double m)
        {
            grr = Math.Log(grr);
            m   = Math.Log(m);

            // Find surrounding indices for each dimension
            int i1 = FindLowerIndex(_grid.GrrValues, grr);
            int j1 = FindLowerIndex(_grid.TsValues, ts);
            int k1 = FindLowerIndex(_grid.MValues, m);

            int i2 = i1 + 1;
            int j2 = j1 + 1;
            int k2 = k1 + 1;

            // Calculate interpolation weights
            double xd = (grr - _grid.GrrValues[i1]) / (_grid.GrrValues[i2] - _grid.GrrValues[i1]);
            double yd = (ts - _grid.TsValues[j1]) / (_grid.TsValues[j2] - _grid.TsValues[j1]);
            double zd = (m - _grid.MValues[k1]) / (_grid.MValues[k2] - _grid.MValues[k1]);

            // Perform trilinear interpolation
            double c00 = _kValues[i1, j1, k1] * (1 - xd) + _kValues[i2, j1, k1] * xd;
            double c01 = _kValues[i1, j1, k2] * (1 - xd) + _kValues[i2, j1, k2] * xd;
            double c10 = _kValues[i1, j2, k1] * (1 - xd) + _kValues[i2, j2, k1] * xd;
            double c11 = _kValues[i1, j2, k2] * (1 - xd) + _kValues[i2, j2, k2] * xd;

            double c0 = c00 * (1 - yd) + c10 * yd;
            double c1 = c01 * (1 - yd) + c11 * yd;

            return c0 * (1 - zd) + c1 * zd;
        }

        // Helper method to find the index of the largest value that is less than or equal to the target
        private int FindLowerIndex(double[] values, double target)
        {
            if (target < values[1]) return 0;
            if (target > values[values.Length - 2]) return values.Length - 2;

            int left  = 1;
            int right = values.Length - 2;

            while (left <= right)
            {
                int mid = (left + right) / 2;

                if (values[mid] <= target && (mid == values.Length - 2 || values[mid + 1] > target))
                    return mid;

                if (values[mid] < target)
                    left = mid + 1;
                else
                    right = mid - 1;
            }

            return left - 1;
        }
    }
}
