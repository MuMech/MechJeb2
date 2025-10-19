using MechJebLib.Primitives;

namespace MechJebLib.PSG
{
    public class PhaseProxy
    {
        public class DoubleArrayProxy
        {
            // reference to the array of decision variables
            // ReSharper disable once NullableWarningSuppressionIsUsed (late initialized in WrapVars())
            private          double[] _vars = null!;
            private readonly int      _k;
            private readonly int      _offset;

            public DoubleArrayProxy(int k, int offset)
            {
                _k      = k;
                _offset = offset;
            }

            public double this[int index]
            {
                get
                {
                    if (index < 0)
                        index = _k + index;
                    return _vars[_offset + index];
                }
                set
                {
                    if (index < 0)
                        index = _k + index;
                    _vars[_offset + index] = value;
                }
            }

            public int Idx(int index)
            {
                if (index < 0)
                    index = _k + index;
                return _offset + index;
            }

            public void WrapVars(double[] vars) => _vars = vars;
        }

        public class V3ArrayProxy
        {
            // reference to the array of decision variables
            // ReSharper disable once NullableWarningSuppressionIsUsed (late initialized in WrapVars())
            private double[] _vars = null!;

            private readonly int _k;
            private readonly int _xOffset, _yOffset, _zOffset;

            public V3ArrayProxy(int k, int xOffset, int yOffset, int zOffset)
            {
                _k       = k;
                _xOffset = xOffset;
                _yOffset = yOffset;
                _zOffset = zOffset;
            }

            public V3 this[int index]
            {
                get
                {
                    if (index < 0)
                        index = _k + index;
                    return new V3(_vars[_xOffset + index], _vars[_yOffset + index], _vars[_zOffset + index]);
                }
                set
                {
                    if (index < 0)
                        index = _k + index;
                    _vars[_xOffset + index] = value.x;
                    _vars[_yOffset + index] = value.y;
                    _vars[_zOffset + index] = value.z;
                }
            }

            public (int, int, int) Idx(int index)
            {
                if (index < 0)
                    index = _k + index;
                return (_xOffset + index, _yOffset + index, _zOffset + index);
            }

            public void WrapVars(double[] vars) => _vars = vars;
        }

        // reference to the array of decision variables
        // ReSharper disable once NullableWarningSuppressionIsUsed (late initialized in WrapVars())
        private          double[] _vars = null!;
        private readonly int      _btOffset;

        public PhaseProxy(int n, int idx, Phase phase)
        {
            int k     = 2 * n - 1;
            int start = idx;

            // position grid points
            Rx  =  new DoubleArrayProxy(k, idx);
            Ry  =  new DoubleArrayProxy(k, idx + k);
            Rz  =  new DoubleArrayProxy(k, idx + 2 * k);
            R   =  new V3ArrayProxy(k, idx, idx + k, idx + 2 * k);
            idx += 3 * k;

            // velocity grid points
            Vx  =  new DoubleArrayProxy(k, idx);
            Vy  =  new DoubleArrayProxy(k, idx + k);
            Vz  =  new DoubleArrayProxy(k, idx + 2 * k);
            V   =  new V3ArrayProxy(k, idx, idx + k, idx + 2 * k);
            idx += 3 * k;

            if (phase.Coast)
            {
                // coast phases only have one M decision variable
                M   =  new DoubleArrayProxy(1, idx);
                idx += 1;
            }
            else
            {
                // mass grid points
                M   =  new DoubleArrayProxy(k, idx);
                idx += k;
            }

            if (phase.Unguided)
            {
                // unguided phases (including coasts) only have one set of control decision variables
                Ux  =  new DoubleArrayProxy(1, idx);
                Uy  =  new DoubleArrayProxy(1, idx + 1);
                Uz  =  new DoubleArrayProxy(1, idx + 2);
                U   =  new V3ArrayProxy(1, idx, idx + 1, idx + 2);
                idx += 3;
            }
            else if (phase.GuidedCoast)
            {
                // guided coasts have initial and terminal controls (unconstrained other than linkages)
                Ux  =  new DoubleArrayProxy(2, idx);
                Uy  =  new DoubleArrayProxy(2, idx + 2);
                Uz  =  new DoubleArrayProxy(2, idx + 4);
                U   =  new V3ArrayProxy(2, idx, idx + 2, idx + 4);
                idx += 6;
            }
            else
            {
                // control grid points
                Ux  =  new DoubleArrayProxy(k, idx);
                Uy  =  new DoubleArrayProxy(k, idx + k);
                Uz  =  new DoubleArrayProxy(k, idx + 2 * k);
                U   =  new V3ArrayProxy(k, idx, idx + k, idx + 2 * k);
                idx += 3 * k;
            }

            _btOffset = idx;
            idx++;
            NumVars = idx - start;

            NumConstraints += 1;                          // complementarity constraint
            if (!phase.GuidedCoast)
                NumConstraints += phase.Unguided ? 1 : k; // control magnitude constraint
            if (phase.Unguided)
                NumConstraints += 3;                      // unguided control constraint
            NumConstraints += (n - 1) * 6 * 2;            // dynamical constraints for r, v
            if (!phase.Coast)
                NumConstraints += (n - 1) * 2;            // dynamical constraints for m
        }

        public     DoubleArrayProxy  Rx             { get; }
        public     DoubleArrayProxy  Ry             { get; }
        public     DoubleArrayProxy  Rz             { get; }
        public     V3ArrayProxy      R              { get; }
        public     DoubleArrayProxy  Vx             { get; }
        public     DoubleArrayProxy  Vy             { get; }
        public     DoubleArrayProxy  Vz             { get; }
        public     V3ArrayProxy      V              { get; }
        public     DoubleArrayProxy  M              { get; }
        public     DoubleArrayProxy  Ux             { get; }
        public     DoubleArrayProxy  Uy             { get; }
        public     DoubleArrayProxy  Uz             { get; }
        public     V3ArrayProxy     U              { get; }
        public ref double            Bt()           => ref _vars[_btOffset];
        public     int               BtIdx()        => _btOffset;
        public     int               NumVars        { get; }
        public     int               NumConstraints { get; }

        public void WrapVars(double[] vars)
        {
            _vars = vars;
            Rx.WrapVars(vars);
            Ry.WrapVars(vars);
            Rz.WrapVars(vars);
            R.WrapVars(vars);
            Vx.WrapVars(vars);
            Vy.WrapVars(vars);
            Vz.WrapVars(vars);
            V.WrapVars(vars);
            M.WrapVars(vars);
            Ux.WrapVars(vars);
            Uy.WrapVars(vars);
            Uz.WrapVars(vars);
            U.WrapVars(vars);
        }
    }
}
