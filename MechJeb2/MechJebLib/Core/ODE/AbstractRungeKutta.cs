/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: MIT-0 OR LGPL-2.1+ OR CC0-1.0
 */

using System;
using System.Collections.Generic;
using MechJebLib.Primitives;
using MechJebLib.Utils;
using static MechJebLib.Utils.Statics;

#nullable enable

namespace MechJebLib.Core.ODE
{
    using IVPFunc = Action<Vn, double, Vn>;
    using IVPEvent = Func<double, Vn, Vn, (double x, bool dir, bool stop)>;

    public abstract class AbstractRungeKutta : AbstractIVP
    {
        protected override double Step(IVPFunc f, double t, double habs, int direction, Vn y, Vn dy, Vn ynew, Vn dynew, object data)
        {
            int n = y.Count;
            using var err = Vn.Rent(n);

            while (true)
            {
                RKStep(f, t, habs, direction, y, dy, ynew, dynew, err, data);

                double error = 0;
                for (int i = 0; i < n; i++)
                    // FIXME: look at dopri fortran code to see how they generate this
                    error = Math.Max(error, Math.Abs(err[i]));

                double s = 0.84 * Math.Pow(Accuracy / error, 1.0 / 5.0);

                if (s < 0.1)
                    s = 0.1;
                if (s > 4)
                    s = 4;
                habs *= s;

                if (Hmin > 0 && habs < Hmin)
                    habs = Hmin * habs;
                if (Hmax > 0 && habs > Hmax)
                    habs = Hmax * habs;

                if (error < Accuracy)
                    break;
            }

            return habs;
        }

        protected override double SelectInitialStep(double t0, double tf)
        {
            if (Hstart > 0)
                return Hstart;

            double v = Math.Abs(tf - t0);
            return 0.001 * v;
        }

        protected abstract void RKStep(IVPFunc f, double t, double habs, int direction, Vn y, Vn dy, Vn ynew, Vn dynew, Vn err, object data);
    }
}
