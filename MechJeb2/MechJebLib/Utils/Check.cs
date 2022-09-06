/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using static MechJebLib.Utils.Statics;

#nullable enable

namespace MechJebLib.Utils
{
    public static class Check
    {
                private static void DoCheck(bool b)
        {
            if (!b) throw new FailedCheck("check failed");
        }

        public class FailedCheck : Exception
        {
            internal FailedCheck(string msg) : base(msg)
            {
            }
        }

        private static string GetTypeName<T>()
        {
            return typeof(T).FullName;
        }

        /*
         * Booleans
         */

        [Conditional("DEBUG")]
        public static void True(bool b)
        {
            DoCheck(b);
        }

        [Conditional("DEBUG")]
        public static void False(bool b)
        {
            DoCheck(!b);
        }

        /*
         * Null
         */

        [Conditional("DEBUG")]
        public static void NotNull<T>(T? obj) where T : class
        {
            DoCheck(obj != null);
        }

        [Conditional("DEBUG")]
        public static void NotNull<T>(T? obj) where T : struct
        {
            DoCheck(obj != null);
        }

        /*
         * Floats
         */

        [Conditional("DEBUG")]
        public static void Finite(double d)
        {
            DoCheck(IsFinite(d));
        }

        [Conditional("DEBUG")]
        public static void Positive(double d)
        {
            DoCheck(d > 0);
        }

        [Conditional("DEBUG")]
        public static void Negative(double d)
        {
            DoCheck(d < 0);
        }

        [Conditional("DEBUG")]
        public static void Zero(double d)
        {
            DoCheck(d == 0);
        }

        [Conditional("DEBUG")]
        public static void NonZero(double d)
        {
            DoCheck(d != 0);
        }

        /*
         * Vectors
         */

        [Conditional("DEBUG")]
        public static void Finite(Vector3d v)
        {
            DoCheck(IsFinite(v));
        }

        [Conditional("DEBUG")]
        public static void NonZero(Vector3d v)
        {
            DoCheck(v != Vector3d.zero);
        }

        /*
         * Arrays
         */

        [Conditional("DEBUG")]
        public static void CanContain(double[] arry, int len)
        {
            DoCheck(arry.Length >= len);
        }

        [Conditional("DEBUG")]
        public static void CanContain(IReadOnlyList<double> arry, int len)
        {
            DoCheck(arry.Count >= len);
        }

    }
}
