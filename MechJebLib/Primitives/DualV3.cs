/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Runtime.CompilerServices;

// ReSharper disable InconsistentNaming
namespace MechJebLib.Primitives
{
    public readonly struct DualV3 : IEquatable<DualV3>, IComparable<DualV3>, IFormattable
    {
        public readonly V3 M;
        public readonly V3 D;

        public Dual x => new Dual(M.x, D.x);
        public Dual y => new Dual(M.y, D.y);
        public Dual z => new Dual(M.z, D.z);

        public DualV3(V3 m, V3? dx = null)
        {
            M = m;
            D = dx ?? V3.zero;
        }

        public DualV3(Dual x, Dual y, Dual z) : this(x.M, y.M, z.M, x.D, y.D, z.D) { }

        public DualV3(double mx, double my, double mz, double dx, double dy, double dz) : this(new V3(mx, my, mz), new V3(dx, dy, dz)) { }

        public Dual sqrMagnitude
        {
            get
            {
                double m = M.x * M.x + M.y * M.y + M.z * M.z;
                double d = 2 * (M.x * D.x + M.y * D.y + M.z * D.z);
                return new Dual(m, d);
            }
        }

        public Dual magnitude
        {
            get
            {
                double m2 = M.x * M.x + M.y * M.y + M.z * M.z;
                double m  = Math.Sqrt(m2);
                double d  = (M.x * D.x + M.y * D.y + M.z * D.z) / m;
                return new Dual(m, d);
            }
        }

        public DualV3 normalized => this / magnitude;
        public DualV3 sph2cart   => x * new DualV3(Dual.Cos(z) * Dual.Sin(y), Dual.Sin(z) * Dual.Sin(y), Dual.Cos(y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DualV3 operator +(DualV3 a, DualV3 b) => new DualV3(a.M + b.M, a.D + b.D);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DualV3 operator -(DualV3 a, DualV3 b) => new DualV3(a.M - b.M, a.D - b.D);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DualV3 operator *(DualV3 a, DualV3 b) => new DualV3(
            new V3(a.M.x * b.M.x, a.M.y * b.M.y, a.M.z * b.M.z),
            new V3(a.M.x * b.D.x + a.D.x * b.M.x, a.M.y * b.D.y + a.D.y * b.M.y, a.M.z * b.D.z + a.D.z * b.M.z)
        );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DualV3 operator /(DualV3 a, DualV3 b) => new DualV3(
            new V3(a.M.x / b.M.x, a.M.y / b.M.y, a.M.z / b.M.z),
            new V3((a.D.x * b.M.x - a.M.x * b.D.x) / (b.M.x * b.M.x),
                (a.D.y * b.M.y - a.M.y * b.D.y) / (b.M.y * b.M.y),
                (a.D.z * b.M.z - a.M.z * b.D.z) / (b.M.z * b.M.z))
        );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DualV3 operator -(DualV3 a) => new DualV3(-a.M, -a.D);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DualV3 operator *(DualV3 a, Dual d) => new DualV3(
            a.M * d.M,
            a.M * d.D + a.D * d.M
        );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DualV3 operator *(Dual d, DualV3 a) => new DualV3(
            a.M * d.M,
            a.M * d.D + a.D * d.M
        );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DualV3 operator /(DualV3 a, Dual d) => new DualV3(
            a.M / d.M,
            (a.D * d.M - a.M * d.D) / (d.M * d.M)
        );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator V3(DualV3 d) => d.M;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator DualV3(V3 d) => new DualV3(d);

        public bool   Equals(DualV3 other)                                    => throw new NotImplementedException();
        public int    CompareTo(DualV3 other)                                 => throw new NotImplementedException();
        public string ToString(string format, IFormatProvider formatProvider) => throw new NotImplementedException();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DualV3 Cross(DualV3 v1, DualV3 v2) => new DualV3(
            new V3(
                v1.M.y * v2.M.z - v1.M.z * v2.M.y,
                v1.M.z * v2.M.x - v1.M.x * v2.M.z,
                v1.M.x * v2.M.y - v1.M.y * v2.M.x
            ),
            new V3(
                v1.D.y * v2.M.z + v1.M.y * v2.D.z - (v1.D.z * v2.M.y + v1.M.z * v2.D.y),
                v1.D.z * v2.M.x + v1.M.z * v2.D.x - (v1.D.x * v2.M.z + v1.M.x * v2.D.z),
                v1.D.x * v2.M.y + v1.M.x * v2.D.y - (v1.D.y * v2.M.x + v1.M.y * v2.D.x)
            )
        );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Dual Dot(DualV3 v1, DualV3 v2) => new Dual(
            v1.M.x * v2.M.x + v1.M.y * v2.M.y + v1.M.z * v2.M.z,
            v1.D.x * v2.M.x + v1.M.x * v2.D.x + v1.D.y * v2.M.y + v1.M.y * v2.D.y + v1.D.z * v2.M.z + v1.M.z * v2.D.z
        );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Dual Angle(DualV3 a, DualV3 b)
        {
            DualV3 a2 = a.normalized;
            DualV3 b2 = b.normalized;

            return Dual.Atan2(Cross(a2, b2).magnitude, Dot(a2, b2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Dual AngleUnit(DualV3 a, DualV3 b) => Dual.Atan2(Cross(a, b).magnitude, Dot(a, b));
    }
}
