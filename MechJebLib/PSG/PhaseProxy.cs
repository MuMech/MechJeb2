/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.Primitives;

namespace MechJebLib.PSG
{
    public class PhaseProxy
    {
        public interface IDoubleArrayProxy
        {
            int Length { get; }
            double this[int index] { get; set; }
            int    Idx(int index);
            void   WrapVars(double[] vars);
            double First    { get; set; }
            double Last     { get; set; }
            int    FirstIdx { get; }
            int    LastIdx  { get; }
        }

        private class DoubleArrayProxy : IDoubleArrayProxy
        {
            // reference to the array of decision variables
            // ReSharper disable once NullableWarningSuppressionIsUsed (late initialized in WrapVars())
            private double[] _vars = null!;

            public int Length { get; }

            public double First
            {
                get => _vars[FirstIdx];
                set => _vars[FirstIdx] = value;
            }

            public double Last
            {
                get => _vars[FirstIdx + Length - 1];
                set => _vars[FirstIdx + Length - 1] = value;
            }

            public int FirstIdx { get; }

            public int LastIdx => FirstIdx + Length - 1;

            public DoubleArrayProxy(int k, int offset)
            {
                Length = k;
                FirstIdx = offset;
            }

            public double this[int index]
            {
                get
                {
                    if (index < 0 || index >= Length)
                        throw new Exception("out of bounds access in proxy");
                    return _vars[FirstIdx + index];
                }
                set
                {
                    if (index < 0 || index >= Length)
                        throw new Exception("out of bounds access in proxy");
                    _vars[FirstIdx + index] = value;
                }
            }

            public int Idx(int index)
            {
                if (index < 0 || index >= Length)
                    throw new Exception("out of bounds access in proxy");
                return FirstIdx + index;
            }

            public void WrapVars(double[] vars) => _vars = vars;
        }

        private class ControlArrayProxy : IDoubleArrayProxy
        {
            // reference to the array of decision variables
            // ReSharper disable once NullableWarningSuppressionIsUsed (late initialized in WrapVars())
            private double[] _vars = null!;

            public int Length { get; }

            public double First
            {
                get => _vars[FirstIdx];
                set => _vars[FirstIdx] = value;
            }

            public double Last
            {
                get => _vars[FirstIdx + Length - 1];
                set => _vars[FirstIdx + Length - 1] = value;
            }

            public int FirstIdx { get; }

            public int LastIdx => FirstIdx + Length - 1;

            public ControlArrayProxy(int k, int offset)
            {
                Length = k;
                FirstIdx = offset;
            }

            private int MangleIndex(int index)
            {
                if (index % 3 == 0)
                    throw new Exception("internal transcription error - accessing control at invalid point");

                return index / 3 * 2 + index % 3 - 1;
            }

            public double this[int index]
            {
                get
                {
                    index = MangleIndex(index);
                    if (index < 0 || index >= Length)
                        throw new Exception("out of bounds access in proxy");
                    return _vars[FirstIdx + index];
                }
                set
                {
                    index = MangleIndex(index);
                    if (index < 0 || index >= Length)
                        throw new Exception("out of bounds access in proxy");
                    _vars[FirstIdx + index] = value;
                }
            }

            public int Idx(int index)
            {
                index = MangleIndex(index);
                if (index < 0 || index >= Length)
                    throw new Exception("out of bounds access in proxy");
                return FirstIdx + index;
            }

            public void WrapVars(double[] vars) => _vars = vars;
        }

        public interface IV3ArrayProxy
        {
            int Length { get; }
            V3 this[int index] { get; set; }
            (int, int, int) Idx(int index);
            void            WrapVars(double[] vars);
            V3              First    { get; set; }
            V3              Last     { get; set; }
            (int, int, int) FirstIdx { get; }
            (int, int, int) LastIdx  { get; }
        }

        private class V3ArrayProxy : IV3ArrayProxy
        {
            // reference to the array of decision variables
            // ReSharper disable once NullableWarningSuppressionIsUsed (late initialized in WrapVars())
            private double[] _vars = null!;

            private readonly int _xOffset, _yOffset, _zOffset;

            public int Length { get; }

            public V3 First
            {
                get => new V3(_vars[_xOffset], _vars[_yOffset], _vars[_zOffset]);
                set
                {
                    _vars[_xOffset] = value.x;
                    _vars[_yOffset] = value.y;
                    _vars[_zOffset] = value.z;
                }
            }

            public V3 Last
            {
                get => new V3(_vars[_xOffset + Length - 1], _vars[_yOffset + Length - 1], _vars[_zOffset + Length - 1]);
                set
                {
                    _vars[_xOffset + Length - 1] = value.x;
                    _vars[_yOffset + Length - 1] = value.y;
                    _vars[_zOffset + Length - 1] = value.z;
                }
            }

            public (int, int, int) FirstIdx => (_xOffset, _yOffset, _zOffset);
            public (int, int, int) LastIdx  => (_xOffset + Length - 1, _yOffset + Length - 1, _zOffset + Length - 1);

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
                    if (index < 0 || index >= Length)
                        throw new Exception("out of bounds access in proxy");
                    return new V3(_vars[_xOffset + index], _vars[_yOffset + index], _vars[_zOffset + index]);
                }
                set
                {
                    if (index < 0 || index >= Length)
                        throw new Exception("out of bounds access in proxy");
                    _vars[_xOffset + index] = value.x;
                    _vars[_yOffset + index] = value.y;
                    _vars[_zOffset + index] = value.z;
                }
            }

            public (int, int, int) Idx(int index)
            {
                if (index < 0 || index >= Length)
                    throw new Exception("out of bounds access in proxy");
                return (_xOffset + index, _yOffset + index, _zOffset + index);
            }

            public void WrapVars(double[] vars) => _vars = vars;
        }

        private class ControlV3ArrayProxy : IV3ArrayProxy
        {
            // reference to the array of decision variables
            // ReSharper disable once NullableWarningSuppressionIsUsed (late initialized in WrapVars())
            private double[] _vars = null!;

            private readonly int _xOffset, _yOffset, _zOffset;

            public int Length { get; }

            public V3 First
            {
                get => new V3(_vars[_xOffset], _vars[_yOffset], _vars[_zOffset]);
                set
                {
                    _vars[_xOffset] = value.x;
                    _vars[_yOffset] = value.y;
                    _vars[_zOffset] = value.z;
                }
            }

            public V3 Last
            {
                get => new V3(_vars[_xOffset + Length - 1], _vars[_yOffset + Length - 1], _vars[_zOffset + Length - 1]);
                set
                {
                    _vars[_xOffset + Length - 1] = value.x;
                    _vars[_yOffset + Length - 1] = value.y;
                    _vars[_zOffset + Length - 1] = value.z;
                }
            }

            public (int, int, int) FirstIdx => (_xOffset, _yOffset, _zOffset);
            public (int, int, int) LastIdx  => (_xOffset + Length - 1, _yOffset + Length - 1, _zOffset + Length - 1);

            public ControlV3ArrayProxy(int k, int xOffset, int yOffset, int zOffset)
            {
                Length = k;
                _xOffset = xOffset;
                _yOffset = yOffset;
                _zOffset = zOffset;
            }

            private int MangleIndex(int index)
            {
                if (index % 3 == 0)
                    throw new Exception("internal transcription error - accessing control at invalid point");

                return index / 3 * 2 + index % 3 - 1;
            }

            public V3 this[int index]
            {
                get
                {
                    index = MangleIndex(index);
                    if (index < 0 || index >= Length)
                        throw new Exception("out of bounds access in proxy");
                    return new V3(_vars[_xOffset + index], _vars[_yOffset + index], _vars[_zOffset + index]);
                }
                set
                {
                    index = MangleIndex(index);
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
                if (index < 0 || index >= Length)
                    throw new Exception("out of bounds access in proxy");
                return (_xOffset + index, _yOffset + index, _zOffset + index);
            }

            public void WrapVars(double[] vars) => _vars = vars;
        }

        public interface IQ3ArrayProxy
        {
            int Length { get; }
            Q3 this[int index] { get; set; }
            (int, int, int, int) Idx(int index);
            void            WrapVars(double[] vars);
            Q3              First    { get; set; }
            Q3              Last     { get; set; }
            (int, int, int, int) FirstIdx { get; }
            (int, int, int, int) LastIdx  { get; }
        }

        private class Q3ArrayProxy : IQ3ArrayProxy
        {
            // reference to the array of decision variables
            // ReSharper disable once NullableWarningSuppressionIsUsed (late initialized in WrapVars())
            private double[] _vars = null!;

            private readonly int _xOffset, _yOffset, _zOffset, _wOffset;

            public int Length { get; }

            public Q3 First
            {
                get => new Q3(_vars[_xOffset], _vars[_yOffset], _vars[_zOffset], _vars[_wOffset]);
                set
                {
                    _vars[_xOffset] = value.x;
                    _vars[_yOffset] = value.y;
                    _vars[_zOffset] = value.z;
                    _vars[_wOffset] = value.w;
                }
            }

            public Q3 Last
            {
                get => new Q3(_vars[_xOffset + Length - 1], _vars[_yOffset + Length - 1], _vars[_zOffset + Length - 1], _vars[_wOffset + Length - 1]);
                set
                {
                    _vars[_xOffset + Length - 1] = value.x;
                    _vars[_yOffset + Length - 1] = value.y;
                    _vars[_zOffset + Length - 1] = value.z;
                    _vars[_wOffset + Length - 1] = value.w;
                }
            }

            public (int, int, int, int) FirstIdx => (_xOffset, _yOffset, _zOffset, _wOffset);
            public (int, int, int, int) LastIdx  => (_xOffset + Length - 1, _yOffset + Length - 1, _zOffset + Length - 1, _wOffset + Length - 1);

            public Q3ArrayProxy(int k, int xOffset, int yOffset, int zOffset, int wOffset)
            {
                Length = k;
                _xOffset = xOffset;
                _yOffset = yOffset;
                _zOffset = zOffset;
                _wOffset = wOffset;
            }

            public Q3 this[int index]
            {
                get
                {
                    if (index < 0 || index >= Length)
                        throw new Exception("out of bounds access in proxy");
                    return new Q3(_vars[_xOffset + index], _vars[_yOffset + index], _vars[_zOffset + index], _vars[_wOffset + index]);
                }
                set
                {
                    if (index < 0 || index >= Length)
                        throw new Exception("out of bounds access in proxy");
                    _vars[_xOffset + index] = value.x;
                    _vars[_yOffset + index] = value.y;
                    _vars[_zOffset + index] = value.z;
                    _vars[_wOffset + index] = value.w;
                }
            }

            public (int, int, int, int) Idx(int index)
            {
                if (index < 0 || index >= Length)
                    throw new Exception("out of bounds access in proxy");
                return (_xOffset + index, _yOffset + index, _zOffset + index, _wOffset + index);
            }

            public void WrapVars(double[] vars) => _vars = vars;
        }

        private class ControlQ3ArrayProxy : IQ3ArrayProxy
        {
            // reference to the array of decision variables
            // ReSharper disable once NullableWarningSuppressionIsUsed (late initialized in WrapVars())
            private double[] _vars = null!;

            private readonly int _xOffset, _yOffset, _zOffset, _wOffset;

            public int Length { get; }

            public Q3 First
            {
                get => new Q3(_vars[_xOffset], _vars[_yOffset], _vars[_zOffset], _vars[_wOffset]);
                set
                {
                    _vars[_xOffset] = value.x;
                    _vars[_yOffset] = value.y;
                    _vars[_zOffset] = value.z;
                    _vars[_wOffset] = value.w;
                }
            }

            public Q3 Last
            {
                get => new Q3(_vars[_xOffset + Length - 1], _vars[_yOffset + Length - 1], _vars[_zOffset + Length - 1], _vars[_wOffset + Length - 1]);
                set
                {
                    _vars[_xOffset + Length - 1] = value.x;
                    _vars[_yOffset + Length - 1] = value.y;
                    _vars[_zOffset + Length - 1] = value.z;
                    _vars[_wOffset + Length - 1] = value.w;
                }
            }

            public (int, int, int, int) FirstIdx => (_xOffset, _yOffset, _zOffset,  _wOffset);
            public (int, int, int, int) LastIdx  => (_xOffset + Length - 1, _yOffset + Length - 1, _zOffset + Length - 1,  _wOffset + Length - 1);

            public ControlQ3ArrayProxy(int k, int xOffset, int yOffset, int zOffset, int wOffset)
            {
                Length = k;
                _xOffset = xOffset;
                _yOffset = yOffset;
                _zOffset = zOffset;
                _wOffset = wOffset;
            }

            private int MangleIndex(int index)
            {
                if (index % 3 == 0)
                    throw new Exception("internal transcription error - accessing control at invalid point");

                return index / 3 * 2 + index % 3 - 1;
            }

            public Q3 this[int index]
            {
                get
                {
                    index = MangleIndex(index);
                    if (index < 0 || index >= Length)
                        throw new Exception("out of bounds access in proxy");
                    return new Q3(_vars[_xOffset + index], _vars[_yOffset + index], _vars[_zOffset + index],_vars[_wOffset + index]);
                }
                set
                {
                    index = MangleIndex(index);
                    if (index < 0 || index >= Length)
                        throw new Exception("out of bounds access in proxy");
                    _vars[_xOffset + index] = value.x;
                    _vars[_yOffset + index] = value.y;
                    _vars[_zOffset + index] = value.z;
                    _vars[_wOffset + index] = value.w;
                }
            }

            public (int, int, int, int) Idx(int index)
            {
                index = MangleIndex(index);
                if (index < 0 || index >= Length)
                    throw new Exception("out of bounds access in proxy");
                return (_xOffset + index, _yOffset + index, _zOffset + index,  _wOffset + index);
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
                UX = new DoubleArrayProxy(1, idx);
                UY = new DoubleArrayProxy(1, idx + 1);
                UZ = new DoubleArrayProxy(1, idx + 2);
                UW = new DoubleArrayProxy(1, idx + 3);
                U = new Q3ArrayProxy(1, idx, idx + 1, idx + 2, idx + 3);
                T = new DoubleArrayProxy(1, idx + 4);
                idx += 5;
            }
            else if (phase.GuidedCoast)
            {
                // guided coasts have no controls (freely interpolates between the two endpoints
                UX = new DoubleArrayProxy(0, -1);
                UY = new DoubleArrayProxy(0, -1);
                UZ = new DoubleArrayProxy(0, -1);
                UW = new DoubleArrayProxy(0, -1);
                U = new Q3ArrayProxy(0, -1, -1, -1, -1);
                T = new DoubleArrayProxy(0, -1);
            }
            else
            {
                // control grid points only at the midpoints
                UX = new ControlArrayProxy(k2, idx);
                UY = new ControlArrayProxy(k2, idx + k2);
                UZ = new ControlArrayProxy(k2, idx + 2 * k2);
                UW = new ControlArrayProxy(k2, idx + 3 * k2);
                U = new ControlQ3ArrayProxy(k2, idx, idx + k2, idx + 2 * k2,  idx + 3 * k2);
                T = new ControlArrayProxy(k2, idx + 4 * k2);
                idx += 5 * k2;
            }

            _btOffset = idx;
            idx++;
            NumVars = idx - start;

            NumConstraints += (k - 1) * 6; // dynamical constraints for r, v
            NumConstraints += 1; // staging constraint
            if (!phase.GuidedCoast)
            {
                NumConstraints += phase.Unguided ? 1 : k2; // unit quaternion constraint
                NumConstraints += phase.Unguided ? 1 : k2; // thrust magnitude constraint
            }
            if (!phase.Coast)
                NumConstraints += k - 1; // dynamical constraints for m
            if (p > 0)
            {
                NumConstraints += 6; // continuity constraints
                if (phase.ControlContinuity)
                    NumConstraints += 4;
            }

            if (p > 0 && phase.MassContinuity)
                NumConstraints += 1; // mass continuity with previous stage
            if (problem.H0 > 0 && problem.Rho0InvQAlphaMax > 0 && !phase.GuidedCoast)
                NumConstraints += k2;
            if (problem.H0 > 0 && problem.Rho0InvQMax > 0)
                NumConstraints += k;
        }

        public     IDoubleArrayProxy Rx             { get; }
        public     IDoubleArrayProxy Ry             { get; }
        public     IDoubleArrayProxy Rz             { get; }
        public     IV3ArrayProxy     R              { get; }
        public     IDoubleArrayProxy Vx             { get; }
        public     IDoubleArrayProxy Vy             { get; }
        public     IDoubleArrayProxy Vz             { get; }
        public     IV3ArrayProxy     V              { get; }
        public     IDoubleArrayProxy M              { get; }
        public     IDoubleArrayProxy UX             { get; }
        public     IDoubleArrayProxy UY             { get; }
        public     IDoubleArrayProxy UZ             { get; }
        public     IDoubleArrayProxy UW             { get; }
        public     IQ3ArrayProxy     U              { get; }
        public     IDoubleArrayProxy T              { get; }
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
            UX.WrapVars(vars);
            UY.WrapVars(vars);
            UZ.WrapVars(vars);
            UW.WrapVars(vars);
            U.WrapVars(vars);
            T.WrapVars(vars);
        }
    }
}
