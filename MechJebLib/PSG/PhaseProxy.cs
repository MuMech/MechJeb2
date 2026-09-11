/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.PSG
{
    public class PhaseProxy
    {
        public class DoubleArrayProxy
        {
            // reference to the array of decision variables
            // ReSharper disable once NullableWarningSuppressionIsUsed (late initialized in WrapVars())
            private double[] _vars = null!;
            private readonly int _offset;

            public int Length { get; }

            public DoubleArrayProxy(int k, int offset)
            {
                Length = k;
                _offset = offset;
            }

            public double this[int index]
            {
                get
                {
                    if (index < 0)
                        index = Length + index;
                    if (index < 0 || index >= Length)
                        throw new Exception("out of bounds access in proxy");
                    return _vars[_offset + index];
                }
                set
                {
                    if (index < 0)
                        index = Length + index;
                    if (index < 0 || index >= Length)
                        throw new Exception("out of bounds access in proxy");
                    _vars[_offset + index] = value;
                }
            }

            public int Idx(int index)
            {
                if (index < 0)
                    index = Length + index;
                if (index < 0 || index >= Length)
                    throw new Exception("out of bounds access in proxy");
                return _offset + index;
            }

            public void WrapVars(double[] vars) => _vars = vars;
        }

        public class ControlArrayProxy
        {
            // reference to the array of decision variables
            // ReSharper disable once NullableWarningSuppressionIsUsed (late initialized in WrapVars())
            private double[] _vars = null!;
            private readonly int _offset;
            private readonly bool _mangle;

            public int Length { get; }

            public ControlArrayProxy(int k, int offset, bool mangle = true)
            {
                Length = k;
                _offset = offset;
                _mangle = mangle;
            }

            private int MangleIndex(int index)
            {
                if (!_mangle)
                    return index;

                if (index < -1)
                    throw new Exception("i was wrong"); // TODO: remove this exception

                // mostly negative is just used for -1 to get the last value
                if (index < 0)
                    return index;

                if (index % 3 == 0)
                    throw new Exception("internal transcription error - accessing control at invalid point");

                return index / 3 * 2 + index % 3 - 1;
            }

            public double this[int index]
            {
                get
                {
                    index = MangleIndex(index);
                    if (index < 0)
                        index = Length + index;
                    if (index < 0 || index >= Length)
                        throw new Exception("out of bounds access in proxy");
                    return _vars[_offset + index];
                }
                set
                {
                    index = MangleIndex(index);
                    if (index < 0)
                        index = Length + index;
                    if (index < 0 || index >= Length)
                        throw new Exception("out of bounds access in proxy");
                    _vars[_offset + index] = value;
                }
            }

            public int Idx(int index)
            {
                index = MangleIndex(index);
                if (index < 0)
                    index = Length + index;
                if (index < 0 || index >= Length)
                    throw new Exception("out of bounds access in proxy");
                return _offset + index;
            }

            public void WrapVars(double[] vars) => _vars = vars;
        }

        public class V3ArrayProxy
        {
            // reference to the array of decision variables
            // ReSharper disable once NullableWarningSuppressionIsUsed (late initialized in WrapVars())
            private double[] _vars = null!;

            private readonly int _xOffset, _yOffset, _zOffset;

            public int Length { get; }

            public V3ArrayProxy(int k, int xOffset, int yOffset, int zOffset)
            {
                Length = k;
                _xOffset = xOffset;
                _yOffset = yOffset;
                _zOffset = zOffset;
            }

            public V3 this[int index]
            {
                get
                {
                    if (index < 0)
                        index = Length + index;
                    if (index < 0 || index >= Length)
                        throw new Exception("out of bounds access in proxy");
                    return new V3(_vars[_xOffset + index], _vars[_yOffset + index], _vars[_zOffset + index]);
                }
                set
                {
                    if (index < 0)
                        index = Length + index;
                    if (index < 0 || index >= Length)
                        throw new Exception("out of bounds access in proxy");
                    _vars[_xOffset + index] = value.x;
                    _vars[_yOffset + index] = value.y;
                    _vars[_zOffset + index] = value.z;
                }
            }

            public (int, int, int) Idx(int index)
            {
                if (index < 0)
                    index = Length + index;
                if (index < 0 || index >= Length)
                    throw new Exception("out of bounds access in proxy");
                return (_xOffset + index, _yOffset + index, _zOffset + index);
            }

            public void WrapVars(double[] vars) => _vars = vars;
        }

        public class ControlV3ArrayProxy
        {
            // reference to the array of decision variables
            // ReSharper disable once NullableWarningSuppressionIsUsed (late initialized in WrapVars())
            private double[] _vars = null!;

            private readonly int _xOffset, _yOffset, _zOffset;
            private readonly bool _mangle;

            public int Length { get; }

            public ControlV3ArrayProxy(int k, int xOffset, int yOffset, int zOffset, bool mangle = true)
            {
                Length = k;
                _xOffset = xOffset;
                _yOffset = yOffset;
                _zOffset = zOffset;
                _mangle = mangle;
            }

            private int MangleIndex(int index)
            {
                if (!_mangle)
                    return index;

                if (index % 3 == 0)
                    throw new Exception("internal transcription error - accessing control at invalid point");

                if (index < 0)
                    return Length / 3 * 2 - (-index % 3);

                return index / 3 * 2 + index % 3 - 1;
            }

            public V3 this[int index]
            {
                get
                {
                    index = MangleIndex(index);
                    if (index < 0)
                        index = Length + index;
                    if (index < 0 || index >= Length)
                        throw new Exception("out of bounds access in proxy");
                    return new V3(_vars[_xOffset + index], _vars[_yOffset + index], _vars[_zOffset + index]);
                }
                set
                {
                    index = MangleIndex(index);
                    if (index < 0)
                        index = Length + index;
                    if (index < 0 || index >= Length)
                        throw new Exception("out of bounds access in proxy");
                    _vars[_xOffset + index] = value.x;
                    _vars[_yOffset + index] = value.y;
                    _vars[_zOffset + index] = value.z;
                }
            }

            public (int, int, int) Idx(int index)
            {
                index = MangleIndex(index);
                if (index < 0)
                    index = Length + index;
                if (index < 0 || index >= Length)
                    throw new Exception("out of bounds access in proxy");
                return (_xOffset + index, _yOffset + index, _zOffset + index);
            }

            public void WrapVars(double[] vars) => _vars = vars;
        }

        // reference to the array of decision variables
        // ReSharper disable once NullableWarningSuppressionIsUsed (late initialized in WrapVars())
        private double[] _vars = null!;
        private readonly int _btOffset;

        public PhaseProxy(Problem problem, int n, int idx, int p, Phase phase)
        {
            int k = 3 * n + 1;
            int k2 = 2 * n;
            int start = idx;

            // position grid points
            Rx = new DoubleArrayProxy(k, idx);
            Ry = new DoubleArrayProxy(k, idx + k);
            Rz = new DoubleArrayProxy(k, idx + 2 * k);
            R = new V3ArrayProxy(k, idx, idx + k, idx + 2 * k);
            idx += 3 * k;

            // velocity grid points
            Vx = new DoubleArrayProxy(k, idx);
            Vy = new DoubleArrayProxy(k, idx + k);
            Vz = new DoubleArrayProxy(k, idx + 2 * k);
            V = new V3ArrayProxy(k, idx, idx + k, idx + 2 * k);
            idx += 3 * k;

            if (phase.Coast)
            {
                // coast phases only have one M decision variable
                M = new DoubleArrayProxy(1, idx);
                idx += 1;
            }
            else
            {
                // mass grid points
                M = new DoubleArrayProxy(k, idx);
                idx += k;
            }

            if (phase.Unguided)
            {
                // unguided phases (including coasts) only have one set of control decision variables
                Ux = new ControlArrayProxy(1, idx, false);
                Uy = new ControlArrayProxy(1, idx + 1, false);
                Uz = new ControlArrayProxy(1, idx + 2, false);
                U = new ControlV3ArrayProxy(1, idx, idx + 1, idx + 2, false);
                idx += 3;
            }
            else if (phase.GuidedCoast)
            {
                // guided coasts have no controls (freely interpolates between the two endpoints
                Ux = new ControlArrayProxy(0, idx, false);
                Uy = new ControlArrayProxy(0, idx + 2, false);
                Uz = new ControlArrayProxy(0, idx + 4, false);
                U = new ControlV3ArrayProxy(0, idx, idx + 2, idx + 4, false);
            }
            else
            {
                // control grid points only at the midpoints
                Ux = new ControlArrayProxy(k, idx);
                Uy = new ControlArrayProxy(k, idx + k2);
                Uz = new ControlArrayProxy(k, idx + 2 * k2);
                U = new ControlV3ArrayProxy(k, idx, idx + k2, idx + 2 * k2);
                idx += 3 * k2;
            }

            _btOffset = idx;
            idx++;
            NumVars = idx - start;

            NumConstraints += (k - 1) * 6; // dynamical constraints for r, v
            NumConstraints += 1; // staging constraint
            if (!phase.GuidedCoast)
                NumConstraints += phase.Unguided ? 1 : k2; // control magnitude constraint
            if (!phase.Coast)
                NumConstraints += k - 1; // dynamical constraints for m
            if (p > 0)
            {
                NumConstraints += 6; // continuity constraints
                if (phase.ControlContinuity)
                    NumConstraints += 3;
            }
            if (p > 0 && phase.MassContinuity)
                NumConstraints += 1; // mass continuity with previous stage
            if (problem.H0 > 0 && problem.Rho0InvQAlphaMax > 0 && !phase.GuidedCoast)
                NumConstraints += k2;
            if (problem.H0 > 0 && problem.Rho0InvQMax > 0)
                NumConstraints += k;
        }

        public     DoubleArrayProxy    Rx             { get; }
        public     DoubleArrayProxy    Ry             { get; }
        public     DoubleArrayProxy    Rz             { get; }
        public     V3ArrayProxy        R              { get; }
        public     DoubleArrayProxy    Vx             { get; }
        public     DoubleArrayProxy    Vy             { get; }
        public     DoubleArrayProxy    Vz             { get; }
        public     V3ArrayProxy        V              { get; }
        public     DoubleArrayProxy    M              { get; }
        public     ControlArrayProxy   Ux             { get; }
        public     ControlArrayProxy   Uy             { get; }
        public     ControlArrayProxy   Uz             { get; }
        public     ControlV3ArrayProxy U              { get; }
        public ref double              Bt()           => ref _vars[_btOffset];
        public     int                 BtIdx()        => _btOffset;
        public     int                 NumVars        { get; }
        public     int                 NumConstraints { get; }

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
