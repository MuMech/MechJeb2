/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.Utils
{
    /// <summary>
    ///     Always-compiled input validation, throwing <see cref="ArgumentException" />.  This is the Release-build twin of
    ///     <see cref="CheckDebug" /> and should be used sparingly: only at user-facing entry points where a value the user
    ///     (UI or kOS) can set may arrive as NaN or out of range.  Internal invariants and hot code (anything inside an ODE
    ///     RHS, a rootfinder or optimizer loop, or the Astro/Angles functions) should use <see cref="CheckDebug" />.
    /// </summary>
    public static class Check
    {
        /*
         * Failure paths are kept out of line so that the success path at each call site is just the comparison.
         *
         * The name is put into the message rather than passed as ArgumentException.ParamName, because the latter makes
         * the runtime append a "Parameter name: ..." line to Message, which is what gets shown to the user in the UI.
         */

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Fail(string? message) => throw new ArgumentException(message ?? "check failed");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void FailNull(string? name) => throw new ArgumentException($"{name ?? "argument"} must not be null");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Fail(string? name, string requirement, double d) =>
            throw new ArgumentException($"{name ?? "argument"} {requirement}, got {d.ToString(CultureInfo.InvariantCulture)}");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Fail(string? name, string requirement, V3 v) =>
            throw new ArgumentException($"{name ?? "argument"} {requirement}, got {v}");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Fail(string? name, string requirement, int len) =>
            throw new ArgumentException($"{name ?? "argument"} {requirement}, got length {len}");

        /*
         * Booleans
         */

        public static void True(bool b, string? message = null)
        {
            if (!b) Fail(message);
        }

        public static void False(bool b, string? message = null)
        {
            if (b) Fail(message);
        }

        /*
         * Null
         */

        public static void NotNull<T>(T? obj, string? name = null) where T : class
        {
            if (obj == null) FailNull(name);
        }

        public static void NotNull<T>(T? obj, string? name = null) where T : struct
        {
            if (obj == null) FailNull(name);
        }

        /*
         * Floats
         */

        public static void Finite(double d, string? name = null)
        {
            if (!IsFinite(d)) Fail(name, "must be finite", d);
        }

        public static void Positive(double d, string? name = null)
        {
            if (!(d > 0)) Fail(name, "must be positive", d);
        }

        public static void PositiveFinite(double d, string? name = null)
        {
            if (!(d > 0 && IsFinite(d))) Fail(name, "must be positive and finite", d);
        }

        public static void NonNegative(double d, string? name = null)
        {
            if (!(d >= 0)) Fail(name, "must be non-negative", d);
        }

        public static void NonNegativeFinite(double d, string? name = null)
        {
            if (!(d >= 0 && IsFinite(d))) Fail(name, "must be non-negative and finite", d);
        }

        public static void Negative(double d, string? name = null)
        {
            if (!(d < 0)) Fail(name, "must be negative", d);
        }

        public static void NegativeFinite(double d, string? name = null)
        {
            if (!(d < 0 && IsFinite(d))) Fail(name, "must be negative and finite", d);
        }

        public static void NonPositive(double d, string? name = null)
        {
            if (!(d <= 0)) Fail(name, "must be non-positive", d);
        }

        public static void NonPositiveFinite(double d, string? name = null)
        {
            if (!(d <= 0 && IsFinite(d))) Fail(name, "must be non-positive and finite", d);
        }

        public static void Zero(double d, string? name = null)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (d != 0) Fail(name, "must be zero", d);
        }

        public static void NonZero(double d, string? name = null)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (d == 0) Fail(name, "must be non-zero", d);
        }

        public static void NonZeroFinite(double d, string? name = null)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (d == 0 || !IsFinite(d)) Fail(name, "must be non-zero and finite", d);
        }

        /*
         * Vectors
         */

        public static void Finite(V3 v, string? name = null)
        {
            if (!IsFinite(v)) Fail(name, "must be finite", v);
        }

        public static void NonZero(V3 v, string? name = null)
        {
            if (v == V3.zero) Fail(name, "must be non-zero", v);
        }

        public static void NonZeroFinite(V3 v, string? name = null)
        {
            if (v == V3.zero || !IsFinite(v)) Fail(name, "must be non-zero and finite", v);
        }

        /*
         * Arrays
         */

        public static void CanContain(double[] arry, int len, string? name = null)
        {
            if (arry.Length < len) Fail(name, $"must have length >= {len}", arry.Length);
        }

        public static void CanContain(IReadOnlyList<double> arry, int len, string? name = null)
        {
            if (arry.Count < len) Fail(name, $"must have length >= {len}", arry.Count);
        }
    }
}
