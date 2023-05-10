/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: MIT-0 OR LGPL-2.1+ OR CC0-1.0
 */

#nullable enable

using System;
using MechJebLib.Utils;

// ReSharper disable CompareOfFloatsByEqualityOperator
namespace MechJebLib.Core
{
    using RootFunc = Func<double, object?, double>;

    public static class Bisection
    {
        /// <summary>
        ///     Bisection search.
        /// </summary>
        /// <param name="f">1 dimensional function</param>
        /// <param name="a">minimum bounds</param>
        /// <param name="b">maximum bounds</param>
        /// <param name="o">state object to pass to function</param>
        /// <param name="rtol">tolerance</param>
        /// <param name="preferLeft"></param>
        /// <returns>value for which the function evaluates to zero</returns>
        /// <exception cref="ArgumentException">guess does not brack the root</exception>
        public static (double c, double fc) Solve(RootFunc f, double a, double b, object? o, double tolX = 0,
            bool preferLeft = true)
        {
            Check.Finite(a);
            Check.Finite(b);
            Check.NonNegative(tolX);

            if (a > b)
            {
                (a, b)     = (b, a);
                preferLeft = !preferLeft;
            }

            double fa = f(a, o);
            double fb = f(b, o);

            Check.Finite(fa);
            Check.Finite(fb);

            if (fa == 0)
                return (a, fa);

            if (fb == 0)
                return (b, fb);

            if (fa * fb > 0)
                throw new ArgumentException("Bisection rootfinding method: guess does not bracket the root");

            while (true)
            {
                double c = (a + b) / 2;

                if (c == a || c == b || b - c < tolX)
                    break;

                double fc = f(c, o);

                if (fc * fa < 0)
                {
                    b  = c;
                    fb = fc;
                }
                else
                {
                    a  = c;
                    fa = fc;
                }
            }

            if (preferLeft)
                return (a, fa);
            return (b, fb);
        }
    }
}
