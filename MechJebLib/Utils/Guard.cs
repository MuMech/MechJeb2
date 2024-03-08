using System;
using System.Collections.Generic;

namespace MechJebLib.Utils
{
    public static class Guard
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

        /*
         * Arrays
         */

        public static void CanContain(double[] arry, int len) => DoCheck(arry.Length >= len);

        public static void CanContain(IReadOnlyList<double> arry, int len) => DoCheck(arry.Count >= len);
    }
}
