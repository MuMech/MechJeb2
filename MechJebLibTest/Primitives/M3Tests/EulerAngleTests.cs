/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using Xunit;
using static System.Math;

namespace MechJebLibTest.Primitives.M3Tests
{
    public class EulerAngleTests
    {
        [Fact]
        private void EulerAnglesZeroRotation()
        {
            var m = M3.EulerAngles(0, 0, 0);
            m.ShouldEqual(M3.identity);
        }

        [Fact]
        private void EulerAnglesZeroRotationFromVector()
        {
            var m = M3.EulerAngles(V3.zero);
            m.ShouldEqual(M3.identity);
        }

        [Fact]
        private void EulerAnglesRollOnly()
        {
            double roll = PI / 4;
            var    m    = M3.EulerAngles(roll, 0, 0);

            var expected = M3.AngleAxis(roll, V3.xaxis);
            m.ShouldEqual(expected, 1e-14);
        }

        [Fact]
        private void EulerAnglesPitchOnly()
        {
            double pitch = PI / 4;
            var    m     = M3.EulerAngles(0, pitch, 0);

            var expected = M3.AngleAxis(pitch, V3.yaxis);
            m.ShouldEqual(expected, 1e-14);
        }

        [Fact]
        private void EulerAnglesYawOnly()
        {
            double yaw = PI / 4;
            var    m   = M3.EulerAngles(0, 0, yaw);

            var expected = M3.AngleAxis(yaw, V3.zaxis);
            m.ShouldEqual(expected, 1e-14);
        }

        [Fact]
        private void EulerAnglesCombined()
        {
            double roll  = PI / 6;
            double pitch = PI / 4;
            double yaw   = PI / 3;

            var m = M3.EulerAngles(roll, pitch, yaw);

            m.isOrthogonal.ShouldBeTrue();
            m.determinant.ShouldEqual(1.0, 1e-14);
        }

        [Fact]
        private void EulerAnglesFromVectorMatchesScalars()
        {
            double roll  = PI / 6;
            double pitch = PI / 4;
            double yaw   = PI / 3;

            var m1 = M3.EulerAngles(roll, pitch, yaw);
            var m2 = M3.EulerAngles(new V3(roll, pitch, yaw));

            m1.ShouldEqual(m2);
        }

        [Fact]
        private void EulerAnglesNegativeAngles()
        {
            double roll  = -PI / 6;
            double pitch = -PI / 4;
            double yaw   = -PI / 3;

            var m = M3.EulerAngles(roll, pitch, yaw);

            m.isOrthogonal.ShouldBeTrue();
            m.determinant.ShouldEqual(1.0, 1e-14);
        }

        [Fact]
        private void EulerAngles90DegreePitch()
        {
            var m = M3.EulerAngles(0, PI / 2, 0);

            m.isOrthogonal.ShouldBeTrue();
            m.determinant.ShouldEqual(1.0, 1e-14);
        }

        [Fact]
        private void EulerAnglesLargeAngles()
        {
            var m1 = M3.EulerAngles(3 * PI, 0, 0);
            var m2 = M3.EulerAngles(PI, 0, 0);

            m1.ShouldEqual(m2, 1e-14);
        }

        [Fact]
        private void ToEulerAnglesIdentity()
        {
            V3 euler = M3.identity.ToEulerAngles();
            euler.ShouldEqual(V3.zero, 1e-14);
        }

        [Fact]
        private void ToEulerAnglesPropertyMatchesMethod()
        {
            var m = M3.EulerAngles(PI / 6, PI / 4, PI / 3);

            m.eulerAngles.ShouldEqual(m.ToEulerAngles());
        }

        [Fact]
        private void ToEulerAnglesRollOnly()
        {
            double roll = PI / 4;
            var    m    = M3.EulerAngles(roll, 0, 0);

            V3 euler = m.ToEulerAngles();
            euler.x.ShouldEqual(roll, 1e-14);
            euler.y.ShouldBeZero(1e-14);
            euler.z.ShouldBeZero(1e-14);
        }

        [Fact]
        private void ToEulerAnglesPitchOnly()
        {
            double pitch = PI / 4;
            var    m     = M3.EulerAngles(0, pitch, 0);

            V3 euler = m.ToEulerAngles();
            euler.x.ShouldBeZero(1e-14);
            euler.y.ShouldEqual(pitch, 1e-14);
            euler.z.ShouldBeZero(1e-14);
        }

        [Fact]
        private void ToEulerAnglesYawOnly()
        {
            double yaw = PI / 4;
            var    m   = M3.EulerAngles(0, 0, yaw);

            V3 euler = m.ToEulerAngles();
            euler.x.ShouldBeZero(1e-14);
            euler.y.ShouldBeZero(1e-14);
            euler.z.ShouldEqual(yaw, 1e-14);
        }

        [Fact]
        private void ToEulerAnglesCombined()
        {
            double roll  = PI / 6;
            double pitch = PI / 4;
            double yaw   = PI / 3;

            var m     = M3.EulerAngles(roll, pitch, yaw);
            V3  euler = m.ToEulerAngles();

            euler.x.ShouldEqual(roll, 1e-14);
            euler.y.ShouldEqual(pitch, 1e-14);
            euler.z.ShouldEqual(yaw, 1e-14);
        }

        [Fact]
        private void ToEulerAnglesNegativeAngles()
        {
            double roll  = -PI / 6;
            double pitch = -PI / 4;
            double yaw   = -PI / 3;

            var m     = M3.EulerAngles(roll, pitch, yaw);
            V3  euler = m.ToEulerAngles();

            euler.x.ShouldEqual(roll, 1e-14);
            euler.y.ShouldEqual(pitch, 1e-14);
            euler.z.ShouldEqual(yaw, 1e-14);
        }

        [Fact]
        private void EulerAnglesRoundTrip()
        {
            double roll  = 0.5;
            double pitch = 0.3;
            double yaw   = 0.7;

            var m     = M3.EulerAngles(roll, pitch, yaw);
            V3  euler = m.ToEulerAngles();
            var m2    = M3.EulerAngles(euler);

            m2.ShouldEqual(m, 1e-14);
        }

        [Fact]
        private void EulerAnglesRoundTripMultipleAngles()
        {
            double[] angles = { -PI / 3, -PI / 6, 0, PI / 6, PI / 3, PI / 2 * 0.9 };

            foreach (double roll in angles)
            foreach (double pitch in new[] { -PI / 3, 0, PI / 3 })
            foreach (double yaw in angles)
            {
                var m     = M3.EulerAngles(roll, pitch, yaw);
                V3  euler = m.ToEulerAngles();
                var m2    = M3.EulerAngles(euler);

                m2.ShouldEqual(m, 1e-13);
            }
        }

        [Fact]
        private void ToEulerAnglesGimbalLockPositive()
        {
            var m = M3.EulerAngles(0, PI / 2, 0);

            V3 euler = m.ToEulerAngles();

            euler.y.ShouldEqual(PI / 2, 1e-14);

            var m2 = M3.EulerAngles(euler);
            m2.ShouldEqual(m, 1e-14);
        }

        [Fact]
        private void ToEulerAnglesGimbalLockNegative()
        {
            var m = M3.EulerAngles(0, -PI / 2, 0);

            V3 euler = m.ToEulerAngles();

            euler.y.ShouldEqual(-PI / 2, 1e-14);

            var m2 = M3.EulerAngles(euler);
            m2.ShouldEqual(m, 1e-14);
        }

        [Fact]
        private void ToEulerAnglesNearGimbalLock()
        {
            double nearNinety = PI / 2 - 1e-10;
            var    m          = M3.EulerAngles(PI / 6, nearNinety, PI / 3);

            V3  euler = m.ToEulerAngles();
            var m2    = M3.EulerAngles(euler);

            m2.ShouldEqual(m, 1e-8);
        }

        [Fact]
        private void EulerAnglesConsistentWithQuaternion()
        {
            double roll  = PI / 6;
            double pitch = PI / 4;
            double yaw   = PI / 3;

            var qRoll  = Q3.AngleAxis(roll, V3.forward);
            var qPitch = Q3.AngleAxis(pitch, V3.right);
            var qYaw   = Q3.AngleAxis(yaw, V3.down);
            Q3  q      = qYaw * qPitch * qRoll;

            var mFromEuler = M3.EulerAngles(roll, pitch, yaw);
            var mFromQuat  = M3.Rotate(q);

            mFromEuler.ShouldEqual(mFromQuat, 1e-14);
        }

        [Fact]
        private void EulerAnglesSmallAngles()
        {
            double small = 1e-10;
            var    m     = M3.EulerAngles(small, small, small);

            V3 euler = m.ToEulerAngles();

            euler.x.ShouldEqual(small, 1e-20);
            euler.y.ShouldEqual(small, 1e-20);
            euler.z.ShouldEqual(small, 1e-20);
        }

        [Fact]
        private void ToEulerAnglesFromRotationMatrix()
        {
            var q = Q3.AngleAxis(0.5, new V3(1, 2, 3).normalized);
            var m = M3.Rotate(q);

            V3  euler = m.ToEulerAngles();
            var m2    = M3.EulerAngles(euler);

            m2.ShouldEqual(m, 1e-14);
        }
    }
}
