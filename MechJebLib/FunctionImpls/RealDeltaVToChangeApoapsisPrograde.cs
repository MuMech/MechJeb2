/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Threading;
using MechJebLib.Functions;
using MechJebLib.Primitives;
using MechJebLib.Rootfinding;

namespace MechJebLib.FunctionImpls
{
    public static class RealDeltaVToChangeApoapsisPrograde
    {
        private class Args
        {
            public double Mu;
            public V3     R;
            public V3     V;
            public double NewApR;

            public void Set(double mu, V3 r, V3 v, double newApR)
            {
                Mu     = mu;
                R      = r;
                V      = v;
                NewApR = newApR;
            }
        }

        private static readonly ThreadLocal<Args> _threadArgs =
            new ThreadLocal<Args>(() => new Args());

        private static readonly Func<double, object?, double> _f = F;

        private static double F(double x, object? o)
        {
            var args = (Args)o!;
            return Astro.ApoapsisFromStateVectors(args.Mu, args.R, args.V.normalized * x) - args.NewApR;
        }

        public static V3 Run(double mu, V3 r, V3 v, double newApR)
        {
            if (r.magnitude >= newApR)
                return V3.zero;

            Args args = _threadArgs.Value;

            args.Set(mu, r, v, newApR);

            double vfm = BrentRoot.Solve(_f, 0, Astro.EscapeVelocity(mu, r.magnitude) - 1, args);

            return vfm * v.normalized - v;
        }
    }
}
