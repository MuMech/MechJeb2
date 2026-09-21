/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.Utils;

namespace MechJebLib.Interpolants
{
    public class DoubleInterpolant : Interpolant<double>, IDisposable
    {
        private static readonly ObjectPool<DoubleInterpolant> _pool = new ObjectPool<DoubleInterpolant>(New, Clear);

        public static DoubleInterpolant Rent() => _pool.Borrow();

        private static DoubleInterpolant New() => new DoubleInterpolant();

        public override void Dispose()
        {
            base.Dispose();
            _pool.Release(this);
        }
    }
}
