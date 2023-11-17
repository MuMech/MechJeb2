/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;

namespace MechJebLib.Primitives
{
    public class DualV3 : IEquatable<DualV3>, IComparable<DualV3>, IFormattable
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

        public Dual   sqrMagnitude => x * x + y * y + z * z;
        public Dual   magnitude    => Dual.Sqrt(x * x + y * y + z * z);
        public DualV3 normalized   => this / magnitude;
        public DualV3 sph2cart     => x * new DualV3(Dual.Cos(z) * Dual.Sin(y), Dual.Sin(z) * Dual.Sin(y), Dual.Cos(y));

        public static DualV3 operator +(DualV3 a, DualV3 b) => new DualV3(a.x + b.x, a.y + b.y, a.z + b.z);

        public static DualV3 operator -(DualV3 a, DualV3 b) => new DualV3(a.x - b.x, a.y - b.y, a.z - b.z);

        public static DualV3 operator *(DualV3 a, DualV3 b) => new DualV3(a.x * b.x, a.y * b.y, a.z * b.z);

        public static DualV3 operator /(DualV3 a, DualV3 b) => new DualV3(a.x / b.x, a.y / b.y, a.z / b.z);

        public static DualV3 operator -(DualV3 a) => new DualV3(-a.x, -a.y, -a.z);

        public static DualV3 operator *(DualV3 a, Dual d) => new DualV3(a.x * d, a.y * d, a.z * d);

        public static DualV3 operator *(Dual d, DualV3 a) => new DualV3(a.x * d, a.y * d, a.z * d);

        public static DualV3 operator /(DualV3 a, Dual d) => new DualV3(a.x / d, a.y / d, a.z / d);

        public static explicit operator V3(DualV3 d) => d.M;
        public static implicit operator DualV3(V3 d) => new DualV3(d);

        public bool   Equals(DualV3 other)                                    => throw new NotImplementedException();
        public int    CompareTo(DualV3 other)                                 => throw new NotImplementedException();
        public string ToString(string format, IFormatProvider formatProvider) => throw new NotImplementedException();

        public static DualV3 Cross(DualV3 v1, DualV3 v2) =>
            new DualV3(v1.y * v2.z - v1.z * v2.y, v1.z * v2.x - v1.x * v2.z, v1.x * v2.y - v1.y * v2.x);

        public static Dual Dot(DualV3 v1, DualV3 v2) => v1.x * v2.x + v1.y * v2.y + v1.z * v2.z;
    }
}
