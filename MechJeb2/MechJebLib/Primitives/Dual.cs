/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

#nullable enable

using System;
using System.Globalization;
using MechJebLib.Utils;

namespace MechJebLib.Primitives
{
    public class Dual : IEquatable<Dual>, IComparable<Dual>, IFormattable
    {
        public readonly double M;
        public readonly double D;

        public Dual(double m, double d = 0)
        {
            M = m;
            D = d;
        }

        public static Dual Sin(Dual d) => new Dual(Math.Sin(d.M), Math.Cos(d.M) * d.D);
        public static Dual Cos(Dual d) => new Dual(Math.Cos(d.M), -Math.Sin(d.M) * d.D);

        public static Dual Tan(Dual d)
        {
            double t = Math.Tan(d.M);
            return new Dual(t, (1.0 + t * t) * d.D);
        }

        public static Dual Log(Dual d)         => new Dual(Math.Log(d.M), 1 / d.M * d.D);
        public static Dual Powi(Dual d, int k) => new Dual(Statics.Powi(d.M, k), k * Statics.Powi(d.M, k - 1) * d.D);
        public static Dual Abs(Dual d)         => new Dual(Math.Abs(d.M), (d.M < 0 ? -1 : 1) * d.D);
        public static Dual Exp(Dual d)         => new Dual(Math.Exp(d.M), Math.Exp(d.M) * d.D);
        public static Dual Sqrt(Dual d)        => new Dual(Math.Sqrt(d.M), 1.0 / (2 * Math.Sqrt(d.M)) * d.D);
        public static Dual Max(Dual d, int i)  => new Dual(Math.Max(d.M, i), d.M > i ? d.D : 0);

        public static Dual operator -(Dual x)             => new Dual(-x.M, -x.D);
        public static Dual operator +(Dual lhs, Dual rhs) => new Dual(lhs.M + rhs.M, lhs.D + rhs.D);
        public static Dual operator -(Dual lhs, Dual rhs) => new Dual(lhs.M - rhs.M, lhs.D - rhs.D);
        public static Dual operator *(Dual lhs, Dual rhs) => new Dual(lhs.M * rhs.M, lhs.D * rhs.M + rhs.D * lhs.M);
        public static Dual operator /(Dual lhs, Dual rhs) => new Dual(lhs.M / rhs.M, (lhs.D * rhs.M - lhs.M * rhs.D) / (rhs.M * rhs.M));

        public static explicit operator double(Dual d) => d.M;
        public static implicit operator Dual(double d) => new Dual(d);

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        public bool Equals(Dual other) => M == other.M && D.Equals(other.D);

        public int CompareTo(Dual other) => M.CompareTo(other.M);

        public override string ToString() => ToString(null, CultureInfo.InvariantCulture.NumberFormat);

        public string ToString(string? format) => ToString(format, CultureInfo.InvariantCulture.NumberFormat);

        public string ToString(string? format, IFormatProvider formatProvider)
        {
            if (string.IsNullOrEmpty(format))
                format = "G";
            return
                $"{M.ToString(format, formatProvider)} + {D.ToString(format, formatProvider)}ϵ";
        }
    }
}
