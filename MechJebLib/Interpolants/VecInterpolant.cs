/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.Primitives;
using MechJebLib.Utils;

namespace MechJebLib.Interpolants
{
    public class VecInterpolant : Interpolant<Vec>, IDisposable
    {
        private static readonly ObjectPool<VecInterpolant> _pool = new ObjectPool<VecInterpolant>(New, Clear);

        public static VecInterpolant Rent() => _pool.Borrow();

        private static VecInterpolant New() => new VecInterpolant();

        public override void Dispose()
        {
            base.Dispose();
            _pool.Release(this);
        }
    }
}
