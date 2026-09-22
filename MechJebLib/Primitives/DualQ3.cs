/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Runtime.CompilerServices;

// ReSharper disable InconsistentNaming
namespace MechJebLib.Primitives
{
    public readonly struct DualQ3 //: IEquatable<DualQ3>, IComparable<DualQ3>, IFormattable
    {
        public readonly Q3 M;
        public readonly Q3 D;

        public Dual x
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Dual(M.x, D.x);
        }

        public Dual y
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Dual(M.y, D.y);
        }

        public Dual z
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Dual(M.z, D.z);
        }

        public Dual w
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Dual(M.w, D.w);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DualQ3(Q3 m, Q3? dx = null)
        {
            M = m;
            D = dx ?? Q3.zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DualQ3(Dual x, Dual y, Dual z, Dual w) : this(x.M, y.M, z.M, w.M, x.D, y.D, z.D, w.D) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DualQ3(double mx, double my, double mz, double mw, double dx, double dy, double dz, double dw) : this(new Q3(mx, my, mz, mw), new Q3(dx, dy, dz, dw)) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator DualQ3(Q3 d) => new DualQ3(d);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DualQ3 operator -(DualQ3 lhs, DualQ3 rhs) => new DualQ3(lhs.M - rhs.M, lhs.D - rhs.D);

        // Hamilton product, the derivative follows the (non-commutative) product rule
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DualQ3 operator *(DualQ3 a, DualQ3 b) => new DualQ3(a.M * b.M, a.M * b.D + a.D * b.M);

        public DualQ3 conjugate
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new DualQ3(M.conjugate, D.conjugate);
        }

        public static DualV3 operator *(DualQ3 q, DualV3 v)
        {
            Dual x = q.x * 2.0;
            Dual y = q.y * 2.0;
            Dual z = q.z * 2.0;
            Dual xx = q.x * x;
            Dual yy = q.y * y;
            Dual zz = q.z * z;
            Dual xy = q.x * y;
            Dual xz = q.x * z;
            Dual yz = q.y * z;
            Dual wx = q.w * x;
            Dual wy = q.w * y;
            Dual wz = q.w * z;

            var res = new DualV3(
                (1.0 - (yy + zz)) * v.x + (xy - wz) * v.y + (xz + wy) * v.z,
                (xy + wz) * v.x + (1.0 - (xx + zz)) * v.y + (yz - wx) * v.z,
                (xz - wy) * v.x + (yz + wx) * v.y + (1.0 - (xx + yy)) * v.z
            );
            return res;
        }

        public Dual magnitude
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                double m2 = M.x * M.x + M.y * M.y + M.z * M.z + M.w * M.w;
                double m = Math.Sqrt(m2);
                double d = (M.x * D.x + M.y * D.y + M.z * D.z + M.w * D.w) / m;
                return new Dual(m, d);
            }
        }
    }
}
