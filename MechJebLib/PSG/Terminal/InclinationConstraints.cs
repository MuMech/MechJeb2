/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using static MechJebLib.Utils.AutoDiff;
using static System.Math;

namespace MechJebLib.PSG.Terminal
{
    /// <summary>
    ///     The inclination constraint for terminal conditions without a LAN target.  Away from the equator it is the single
    ///     row h_z/|h| - cos(incT).  At incT = 0 or PI that row is -tilt^2/2, which has a vanishing gradient at the solution,
    ///     so the SQP can't linearize it: a feasibility tolerance of tol only bounds the tilt by sqrt(2*tol), and warm-started
    ///     solves can stall a couple of iterations in once the row reads as satisfied.  Equatorial targets are instead
    ///     constrained to the equatorial plane with the two linear rows r.z = 0 and v.z = 0 (the plane-orthogonality rows of
    ///     FlightPathAngle4Energy with iHT = zhat), which are regular and exact under linearization.  Those rows don't
    ///     distinguish prograde from retrograde, which is left to the initial guess (the AscentGuesser heads by IncT()).
    /// </summary>
    internal static class InclinationConstraints
    {
        // Targets with |sin(incT)| below this are snapped to exactly equatorial.  The degenerate region of the single row
        // scales like sqrt(feasibility) which is ~1e-4 rad for the feasibilities the optimizer reaches, this leaves margin
        // over that while staying under the 0.097 degree latitude of KSC.
        private const double EQUATORIAL_SIN_INC = 1e-3;

        public static bool IsEquatorial(double incT) => Abs(Sin(incT)) < EQUATORIAL_SIN_INC;

        public static int NumConstraints(double incT) => IsEquatorial(incT) ? 2 : 1;

        public static int Apply(double incT, V3 rf, V3 vf, (int, int, int) ri, (int, int, int) vi, double[] f, alglib.sparsematrix j, int ci)
        {
            if (IsEquatorial(incT))
            {
                ci = ApplyScalarConstraintV3(f, j, ci, ZComponent, new[] { rf }, new[] { ri });
                return ApplyScalarConstraintV3(f, j, ci, ZComponent, new[] { vf }, new[] { vi });
            }

            double cosIncT = Cos(Abs(incT));

            return ApplyScalarConstraintV3(f, j, ci, InclinationConstraint, new[] { rf, vf }, new[] { ri, vi });

            static Dual ZComponent(DualV3[] p) => p[0].z;

            Dual InclinationConstraint(DualV3[] p) => DualV3.Cross(p[0], p[1]).normalized.z - cosIncT;
        }
    }
}
