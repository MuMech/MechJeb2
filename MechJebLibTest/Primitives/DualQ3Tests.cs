/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using Xunit;
using static MechJebLib.Utils.AutoDiff;

namespace MechJebLibTest.Primitives
{
    public class DualQ3Tests
    {
        private static readonly Q3 _a  = new Q3(0.1, -0.2, 0.3, 0.9);
        private static readonly Q3 _da = new Q3(0.5, 0.25, -0.75, 1.5);
        private static readonly Q3 _b  = new Q3(-0.4, 0.6, 0.2, 0.5);
        private static readonly Q3 _db = new Q3(-1.0, 0.3, 0.7, -0.2);

        [Fact]
        private void HamiltonProductValue()
        {
            DualQ3 p = new DualQ3(_a, _da) * new DualQ3(_b, _db);

            p.M.ShouldEqual(_a * _b);
        }

        [Fact]
        private void HamiltonProductDerivativeMatchesFiniteDifference()
        {
            DualQ3 p = new DualQ3(_a, _da) * new DualQ3(_b, _db);

            const double h = 1e-6;
            Q3 fd = ((_a + h * _da) * (_b + h * _db) - (_a - h * _da) * (_b - h * _db)) / (2 * h);

            p.D.ShouldEqual(fd, 1e-8);
        }

        [Fact]
        private void Conjugate()
        {
            DualQ3 c = new DualQ3(_a, _da).conjugate;

            c.M.ShouldEqual(new Q3(-_a.x, -_a.y, -_a.z, _a.w));
            c.D.ShouldEqual(new Q3(-_da.x, -_da.y, -_da.z, _da.w));
        }

        [Fact]
        private void Subtraction()
        {
            DualQ3 d = new DualQ3(_a, _da) - new DualQ3(_b, _db);

            d.M.ShouldEqual(_a - _b);
            d.D.ShouldEqual(_da - _db);
        }

        private static DualV3 AttitudeError(DualQ3[] x)
        {
            DualQ3 e = x[0].conjugate * x[1];
            return new DualV3(e.x, e.y, e.z);
        }

        private static V3 AttitudeError(Q3 q0, Q3 q1)
        {
            Q3 e = q0.conjugate * q1;
            return new V3(e.x, e.y, e.z);
        }

        [Fact]
        private void AttitudeErrorZeroForSameRotation()
        {
            Q3 q = Q3.AngleAxis(0.7, new V3(1, 2, 3));

            (V3 same, Vec sx, Vec sy, Vec sz) = JacobianQ3(AttitudeError, new[] { q, q });
            same.ShouldBeZero();
            sx.Dispose();
            sy.Dispose();
            sz.Dispose();

            // q and -q are the same rotation
            (V3 flipped, Vec fx, Vec fy, Vec fz) = JacobianQ3(AttitudeError, new[] { q, -q });
            flipped.ShouldBeZero();
            fx.Dispose();
            fy.Dispose();
            fz.Dispose();
        }

        [Fact]
        private void AttitudeErrorJacobianMatchesFiniteDifference()
        {
            Q3[] point = { Q3.AngleAxis(0.7, new V3(1, 2, 3)), Q3.AngleAxis(-1.3, new V3(-2, 0.5, 1)) };

            (V3 value, Vec partialX, Vec partialY, Vec partialZ) = JacobianQ3(AttitudeError, point);

            value.ShouldEqual(AttitudeError(point[0], point[1]));

            const double h = 1e-6;
            Q3[] seeds = { Q3.xaxis, Q3.yaxis, Q3.zaxis, Q3.waxis };

            for (int i = 0; i < point.Length; i++)
            {
                for (int k = 0; k < 4; k++)
                {
                    var plus  = (Q3[])point.Clone();
                    var minus = (Q3[])point.Clone();
                    plus[i]  += h * seeds[k];
                    minus[i] -= h * seeds[k];

                    V3 fd = (AttitudeError(plus[0], plus[1]) - AttitudeError(minus[0], minus[1])) / (2 * h);

                    new V3(partialX[4 * i + k], partialY[4 * i + k], partialZ[4 * i + k]).ShouldEqual(fd, 1e-8);
                }
            }

            partialX.Dispose();
            partialY.Dispose();
            partialZ.Dispose();
        }
    }
}
