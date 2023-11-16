/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;

#nullable enable

namespace MechJebLib.Utils
{
    public static class Check
    {
        private static void DoCheck(bool b)
        {
            if (!b)
                throw new FailedCheck("check failed");
        }

        public class FailedCheck : Exception
        {
            internal FailedCheck(string msg) : base(msg)
            {
            }
        }

        private static string GetTypeName<T>() => typeof(T).FullName;

        /*
         * Booleans
         */

        [Conditional("DEBUG")]
        public static void True(bool b) => DoCheck(b);

        [Conditional("DEBUG")]
        public static void False(bool b) => DoCheck(!b);

        /*
         * Null
         */

        [Conditional("DEBUG")]
        public static void NotNull<T>(T? obj) where T : class => DoCheck(obj != null);

        [Conditional("DEBUG")]
        public static void NotNull<T>(T? obj) where T : struct => DoCheck(obj != null);

        /*
         * Floats
         */

        [Conditional("DEBUG")]
        public static void Finite(double d) => DoCheck(IsFinite(d));

        [Conditional("DEBUG")]
        public static void Positive(double d) => DoCheck(d > 0);

        [Conditional("DEBUG")]
        public static void PositiveFinite(double d)
        {
            DoCheck(d > 0);
            DoCheck(IsFinite(d));
        }

        [Conditional("DEBUG")]
        public static void NonNegative(double d) => DoCheck(d >= 0);

        [Conditional("DEBUG")]
        public static void NonNegativeFinite(double d)
        {
            DoCheck(d >= 0);
            DoCheck(IsFinite(d));
        }

        [Conditional("DEBUG")]
        public static void Negative(double d) => DoCheck(d < 0);

        [Conditional("DEBUG")]
        public static void NegativeFinite(double d)
        {
            DoCheck(d < 0);
            DoCheck(IsFinite(d));
        }

        [Conditional("DEBUG")]
        public static void NonPositive(double d) => DoCheck(d <= 0);

        [Conditional("DEBUG")]
        public static void NonPositiveFinite(double d)
        {
            DoCheck(d <= 0);
            DoCheck(IsFinite(d));
        }

        [Conditional("DEBUG")]
        public static void Zero(double d) => DoCheck(d == 0);

        [Conditional("DEBUG")]
        public static void NonZero(double d) => DoCheck(d != 0);

        [Conditional("DEBUG")]
        public static void NonZeroFinite(double d)
        {
            DoCheck(d != 0);
            DoCheck(IsFinite(d));
        }

        /*
         * Vectors
         */

        /*
        [Conditional("DEBUG")]
        public static void Finite(Vector3d v)
        {
            DoCheck(IsFinite(v));
        }
        */

        [Conditional("DEBUG")]
        public static void Finite(V3 v) => DoCheck(IsFinite(v));

        [Conditional("DEBUG")]
        public static void NonZero(V3 v) => DoCheck(v != V3.zero);

        [Conditional("DEBUG")]
        public static void NonZeroFinite(V3 v)
        {
            DoCheck(v != V3.zero);
            DoCheck(IsFinite(v));
        }

        /*
         * Arrays
         */

        [Conditional("DEBUG")]
        public static void CanContain(double[] arry, int len) => DoCheck(arry.Length >= len);

        [Conditional("DEBUG")]
        public static void CanContain(IReadOnlyList<double> arry, int len) => DoCheck(arry.Count >= len);
    }
}
