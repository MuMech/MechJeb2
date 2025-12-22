/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using Xunit;
using static MechJebLib.Utils.Statics;
using static System.Math;

namespace MechJebLibTest.Primitives.M3Tests
{
    public class RotationTests
    {
        [Fact]
        private void RotateFromIdentityQuaternion()
        {
            M3.Rotate(Q3.identity).ShouldEqual(M3.identity);
        }

        [Fact]
        private void Rotate90DegreesAroundX()
        {
            var q = Q3.AngleAxis(PI / 2, V3.xaxis);
            var m = M3.Rotate(q);

            (m * V3.yaxis).ShouldEqual(V3.zaxis, 1e-15);
            (m * V3.zaxis).ShouldEqual(-V3.yaxis, 1e-15);
            (m * V3.xaxis).ShouldEqual(V3.xaxis, 1e-15);
        }

        [Fact]
        private void Rotate90DegreesAroundY()
        {
            var q = Q3.AngleAxis(PI / 2, V3.yaxis);
            var m = M3.Rotate(q);

            (m * V3.zaxis).ShouldEqual(V3.xaxis, 1e-15);
            (m * V3.xaxis).ShouldEqual(-V3.zaxis, 1e-15);
            (m * V3.yaxis).ShouldEqual(V3.yaxis, 1e-15);
        }

        [Fact]
        private void Rotate90DegreesAroundZ()
        {
            var q = Q3.AngleAxis(PI / 2, V3.zaxis);
            var m = M3.Rotate(q);

            (m * V3.xaxis).ShouldEqual(V3.yaxis, 1e-15);
            (m * V3.yaxis).ShouldEqual(-V3.xaxis, 1e-15);
            (m * V3.zaxis).ShouldEqual(V3.zaxis, 1e-15);
        }

        [Fact]
        private void Rotate45DegreesAroundArbitraryAxis()
        {
            V3 axis = new V3(1, 1, 1).normalized;
            var q = Q3.AngleAxis(PI / 4, axis);
            var m = M3.Rotate(q);

            m.determinant.ShouldEqual(1.0, 1e-14);
            (m.transpose * m).ShouldEqual(M3.identity, 1e-14);
        }

        [Fact]
        private void RotateCompositionMatchesQuaternions()
        {
            var q1 = Q3.AngleAxis(PI / 3, V3.xaxis);
            var q2 = Q3.AngleAxis(PI / 4, V3.yaxis);
            Q3 qCombined = q2 * q1;

            var m1 = M3.Rotate(q1);
            var m2 = M3.Rotate(q2);
            var mCombined = M3.Rotate(qCombined);

            (m2 * m1).ShouldEqual(mCombined, 1e-14);
        }

        [Fact]
        private void RotatePreservesVectorMagnitude()
        {
            var q = Q3.AngleAxis(1.234, new V3(1, 2, 3).normalized);
            var m = M3.Rotate(q);
            var v = new V3(4, 5, 6);

            (m * v).magnitude.ShouldEqual(v.magnitude, 1e-14);
        }

        [Fact]
        private void RotateInverseIsTranspose()
        {
            var q = Q3.AngleAxis(0.789, new V3(3, -2, 1).normalized);
            var m = M3.Rotate(q);

            (m * m.transpose).ShouldEqual(M3.identity, 1e-14);
            (m.transpose * m).ShouldEqual(M3.identity, 1e-14);
        }

        [Fact]
        private void RotateSmallAngles()
        {
            const double TINY = 1e-10;
            var q = Q3.AngleAxis(TINY, V3.zaxis);
            var m = M3.Rotate(q);

            m.m00.ShouldEqual(1.0, 1e-20);
            m.m01.ShouldEqual(-TINY, 1e-20);
            m.m10.ShouldEqual(TINY, 1e-20);
            m.m11.ShouldEqual(1.0, 1e-20);
        }

        [Fact]
        private void RotateLargeAngles()
        {
            var q = Q3.AngleAxis(100 * PI, V3.xaxis);
            var m = M3.Rotate(q);

            (m * V3.yaxis).ShouldEqual(V3.yaxis, 1e-14);
            (m * V3.zaxis).ShouldEqual(V3.zaxis, 1e-14);
        }

        [Fact]
        private void RotateNegativeAngles()
        {
            var qPos = Q3.AngleAxis(PI / 6, V3.xaxis);
            var qNeg = Q3.AngleAxis(-PI / 6, V3.xaxis);
            var mPos = M3.Rotate(qPos);
            var mNeg = M3.Rotate(qNeg);

            (mPos * mNeg).ShouldEqual(M3.identity, 1e-14);
        }

        [Fact]
        private void RotateGimbalLockConfiguration()
        {
            var q = Q3.AngleAxis(PI / 2, V3.yaxis);
            var m = M3.Rotate(q);

            m.m02.ShouldEqual(1.0, 1e-15);
            m.m20.ShouldEqual(-1.0, 1e-15);
            m.m00.ShouldBeZero(1e-15);
            m.m22.ShouldBeZero(1e-15);
        }

        [Fact]
        private void RotateFromEulerAngles()
        {
            double roll = PI / 6;
            double pitch = PI / 4;
            double yaw = PI / 3;

            var qRoll = Q3.AngleAxis(roll, V3.forward);
            var qPitch = Q3.AngleAxis(pitch, V3.left);
            var qYaw = Q3.AngleAxis(yaw, V3.up);
            Q3 qCombined = qYaw * qPitch * qRoll;

            var m = M3.Rotate(qCombined);

            m.determinant.ShouldEqual(1.0, 1e-14);
            Assert.True(IsFinite(m.m00));
            Assert.True(IsFinite(m.m11));
            Assert.True(IsFinite(m.m22));
        }

        [Fact]
        private void RotateOrthogonalityPreserved()
        {
            var q = Q3.AngleAxis(1.23, new V3(4, -5, 6).normalized);
            var m = M3.Rotate(q);

            V3 col0 = m.GetColumn(0);
            V3 col1 = m.GetColumn(1);
            V3 col2 = m.GetColumn(2);

            V3.Dot(col0, col1).ShouldBeZero(1e-14);
            V3.Dot(col1, col2).ShouldBeZero(1e-14);
            V3.Dot(col0, col2).ShouldBeZero(1e-14);

            col0.magnitude.ShouldEqual(1.0, 1e-14);
            col1.magnitude.ShouldEqual(1.0, 1e-14);
            col2.magnitude.ShouldEqual(1.0, 1e-14);
        }

        [Fact]
        private void RotateRightHandedness()
        {
            var q = Q3.AngleAxis(0.456, new V3(1, -2, 3).normalized);
            var m = M3.Rotate(q);

            V3 col0 = m.GetColumn(0);
            V3 col1 = m.GetColumn(1);
            V3 col2 = m.GetColumn(2);

            V3.Cross(col0, col1).ShouldEqual(col2, 1e-14);
        }

        [Fact]
        private void RotateConsistentWithQuaternionMultiplication()
        {
            var q = Q3.AngleAxis(0.789, new V3(2, 3, -1).normalized);
            var m = M3.Rotate(q);
            var v = new V3(5, -3, 7);

            (m * v).ShouldEqual(q * v, 1e-14);
        }

        [Fact]
        private void Rotate180Degrees()
        {
            var q = Q3.AngleAxis(PI, V3.xaxis);
            var m = M3.Rotate(q);

            (m * V3.yaxis).ShouldEqual(-V3.yaxis, 1e-14);
            (m * V3.zaxis).ShouldEqual(-V3.zaxis, 1e-14);
            (m * V3.xaxis).ShouldEqual(V3.xaxis, 1e-14);
        }

        [Fact]
        private void RotateNearlyParallelAxis()
        {
            V3 axis = new V3(1, 1e-10, 1e-10).normalized;
            var q = Q3.AngleAxis(PI / 3, axis);
            var m = M3.Rotate(q);

            m.determinant.ShouldEqual(1.0, 1e-14);
            Assert.True(IsFinite(m.m00));
            Assert.True(IsFinite(m.m11));
            Assert.True(IsFinite(m.m22));
        }

        [Fact]
        private void RotateMatrixConsistency()
        {
            var q = Q3.AngleAxis(2.34, new V3(-1, 2, 1).normalized);
            var m = M3.Rotate(q);

            // Rotation matrix specific properties
            m.determinant.ShouldEqual(1.0, 1e-14);
            (m * m.transpose).ShouldEqual(M3.identity, 1e-14);

            // Check eigenvalue magnitude (should have at least one eigenvalue = 1)
            double trace = m.m00 + m.m11 + m.m22;
            double angle = Acos((trace - 1) / 2);
            angle.ShouldEqual(2.34, 1e-14);
        }

        [Fact]
        private void RotateAxisExtractionConsistency()
        {
            V3 originalAxis = new V3(3, -4, 5).normalized;
            double originalAngle = 1.234;
            var q = Q3.AngleAxis(originalAngle, originalAxis);
            var m = M3.Rotate(q);

            // The rotation axis should be invariant under the rotation
            (m * originalAxis).ShouldEqual(originalAxis, 1e-14);
        }

        [Fact]
        private void RotateDoubleRotationConsistency()
        {
            var q = Q3.AngleAxis(PI / 7, new V3(1, 1, 0).normalized);
            var m = M3.Rotate(q);
            var m2 = M3.Rotate(q * q);

            (m * m).ShouldEqual(m2, 1e-14);
        }
    }
}
