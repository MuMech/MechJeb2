/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using Xunit;
using static System.Math;

namespace MechJebLibTest.Primitives.M3Tests
{
    public class OrthonormalizeQuaternionTests
    {
        [Fact]
        private void OrthonormalizeIdentityMatrix()
        {
            M3 m = M3.identity;
            m = m.orthonormalized;

            m.ShouldEqual(M3.identity);
        }

        [Fact]
        private void OrthonormalizeAlreadyOrthonormal()
        {
            var q        = Q3.AngleAxis(PI / 3, new V3(1, 2, 3).normalized);
            var m        = M3.Rotate(q);
            M3  original = m;

            m = m.orthonormalized;

            m.ShouldEqual(original, 1e-14);
        }

        [Fact]
        private void OrthonormalizeNearlyOrthogonal()
        {
            var m = new M3(1, 0.001, 0,
                0, 1, 0,
                0, 0, 1);

            m = m.orthonormalized;

            V3 col0 = m.GetColumn(0);
            V3 col1 = m.GetColumn(1);
            V3 col2 = m.GetColumn(2);

            col0.magnitude.ShouldEqual(1.0, 1e-14);
            col1.magnitude.ShouldEqual(1.0, 1e-14);
            col2.magnitude.ShouldEqual(1.0, 1e-14);

            V3.Dot(col0, col1).ShouldBeZero(1e-14);
            V3.Dot(col1, col2).ShouldBeZero(1e-14);
            V3.Dot(col0, col2).ShouldBeZero(1e-14);
        }

        [Fact]
        private void OrthonormalizeScaledMatrix()
        {
            M3 m = M3.identity * 2.5;

            m = m.orthonormalized;

            m.ShouldEqual(M3.identity, 1e-14);
        }

        [Fact]
        private void OrthonormalizeSkewedMatrix()
        {
            var m = new M3(1, 0.5, 0.3,
                0.2, 1, 0.4,
                0.1, 0.2, 1);

            m = m.orthonormalized;

            V3 col0 = m.GetColumn(0);
            V3 col1 = m.GetColumn(1);
            V3 col2 = m.GetColumn(2);

            col0.magnitude.ShouldEqual(1.0, 1e-14);
            col1.magnitude.ShouldEqual(1.0, 1e-14);
            col2.magnitude.ShouldEqual(1.0, 1e-14);

            V3.Dot(col0, col1).ShouldBeZero(1e-14);
            V3.Dot(col1, col2).ShouldBeZero(1e-14);
            V3.Dot(col0, col2).ShouldBeZero(1e-14);
        }

        [Fact]
        private void OrthonormalizePreservesFirstColumnDirection()
        {
            var m = new M3(2, 1, 0,
                0, 3, 1,
                0, 0, 4);

            V3 originalFirstDir = m.GetColumn(0).normalized;

            m = m.orthonormalized;

            m.GetColumn(0).ShouldEqual(originalFirstDir, 1e-14);
        }

        [Fact]
        private void OrthonormalizeHandlesSmallVectors()
        {
            var m = new M3(1e-100, 0, 0,
                0, 1e-100, 0,
                0, 0, 1e-100);

            m = m.orthonormalized;

            m.ShouldEqual(M3.identity, 1e-14);
        }

        [Fact]
        private void OrthonormalizeHandlesLargeVectors()
        {
            var m = new M3(1e100, 0, 0,
                0, 1e100, 0,
                0, 0, 1e100);

            m = m.orthonormalized;

            m.ShouldEqual(M3.identity, 1e-14);
        }

        [Fact]
        private void OrthonormalizedPropertyReturnsNewMatrix()
        {
            var m = new M3(1, 0.5, 0.3,
                0.2, 1, 0.4,
                0.1, 0.2, 1);

            M3 result = m.orthonormalized;

            m[0, 1].ShouldEqual(0.5);

            V3 col0 = result.GetColumn(0);
            V3 col1 = result.GetColumn(1);
            V3 col2 = result.GetColumn(2);

            col0.magnitude.ShouldEqual(1.0, 1e-14);
            col1.magnitude.ShouldEqual(1.0, 1e-14);
            col2.magnitude.ShouldEqual(1.0, 1e-14);

            V3.Dot(col0, col1).ShouldBeZero(1e-14);
            V3.Dot(col1, col2).ShouldBeZero(1e-14);
            V3.Dot(col0, col2).ShouldBeZero(1e-14);
        }

        [Fact]
        private void QuaternionFromIdentityMatrix()
        {
            Q3 q = M3.identity.quaternion;

            q.ShouldEqual(Q3.identity, 1e-14);
        }

        [Fact]
        private void QuaternionFrom90DegreeRotationX()
        {
            var original = Q3.AngleAxis(PI / 2, V3.xaxis);
            var m        = M3.Rotate(original);

            Q3 q = m.quaternion;

            q.ShouldEqual(original, 1e-14);
        }

        [Fact]
        private void QuaternionFrom90DegreeRotationY()
        {
            var original = Q3.AngleAxis(PI / 2, V3.yaxis);
            var m        = M3.Rotate(original);

            Q3 q = m.quaternion;

            q.ShouldEqual(original, 1e-14);
        }

        [Fact]
        private void QuaternionFrom90DegreeRotationZ()
        {
            var original = Q3.AngleAxis(PI / 2, V3.zaxis);
            var m        = M3.Rotate(original);

            Q3 q = m.quaternion;

            q.ShouldEqual(original, 1e-14);
        }

        [Fact]
        private void QuaternionFromArbitraryRotation()
        {
            var original = Q3.AngleAxis(1.234, new V3(1, 2, 3).normalized);
            var m        = M3.Rotate(original);

            Q3 q = m.quaternion;

            q.ShouldEqual(original, 1e-14);
        }

        [Fact]
        private void QuaternionFrom180DegreeRotation()
        {
            var original = Q3.AngleAxis(PI, V3.xaxis);
            var m        = M3.Rotate(original);

            Q3 q = m.quaternion;

            double angle = Q3.Angle(q, original);
            angle.ShouldBeZero(1e-14);
        }

        [Fact]
        private void QuaternionHandlesNegativeTrace()
        {
            var m = new M3(-1, 0, 0,
                0, -1, 0,
                0, 0, 1);

            Q3 q = m.quaternion;

            double mag = Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            mag.ShouldEqual(1.0, 1e-14);

            (M3.Rotate(q) * V3.xaxis).ShouldEqual(m * V3.xaxis, 1e-14);
            (M3.Rotate(q) * V3.yaxis).ShouldEqual(m * V3.yaxis, 1e-14);
            (M3.Rotate(q) * V3.zaxis).ShouldEqual(m * V3.zaxis, 1e-14);
        }

        [Fact]
        private void RotationQuaternionLargestElementX()
        {
            var m = new M3(0.5, -0.5, 0.707107,
                0.5, 0.5, -0.707107,
                -0.707107, 0.707107, 0);

            Q3 q = m.rotation_quaternion;

            double mag = Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            mag.ShouldEqual(1.0, 1e-14);
        }

        [Fact]
        private void RotationQuaternionLargestElementY()
        {
            var m = new M3(0, 0.707107, -0.707107,
                -0.707107, 0.5, 0.5,
                0.707107, 0.5, 0.5);

            Q3 q = m.rotation_quaternion;

            double mag = Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            mag.ShouldEqual(1.0, 1e-14);
        }

        [Fact]
        private void RotationQuaternionLargestElementZ()
        {
            var m = new M3(0.5, -0.707107, 0.5,
                0.707107, 0, -0.707107,
                0.5, 0.707107, 0.5);

            Q3 q = m.rotation_quaternion;

            double mag = Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            mag.ShouldEqual(1.0, 1e-14);
        }

        [Fact]
        private void RotationQuaternionFromNonOrthonormalMatrix()
        {
            var m = new M3(2, 0, 0,
                0, 2, 0,
                0, 0, 2);

            Q3 q = m.rotation_quaternion;

            q.ShouldEqual(Q3.identity, 1e-14);
        }

        [Fact]
        private void RotationQuaternionFromIdentity()
        {
            Q3 q = M3.identity.rotation_quaternion;

            q.ShouldEqual(Q3.identity, 1e-14);
        }

        [Fact]
        private void RotationQuaternionFromRotationMatrix()
        {
            var original = Q3.AngleAxis(0.789, new V3(3, -2, 1).normalized);
            var m        = M3.Rotate(original);

            Q3 q = m.rotation_quaternion;

            q.ShouldEqual(original, 1e-14);
        }

        [Fact]
        private void RotationQuaternionFromScaledMatrix()
        {
            var original = Q3.AngleAxis(PI / 4, V3.up);
            M3  m        = M3.Rotate(original) * 3.5;

            Q3 q = m.rotation_quaternion;

            double angle = Q3.Angle(q, original);
            angle.ShouldBeZero(1e-14);
        }

        [Fact]
        private void RotationQuaternionFromSkewedMatrix()
        {
            var m = new M3(1, 0.5, 0.3,
                0.2, 1, 0.4,
                0.1, 0.2, 1);

            Q3 q = m.rotation_quaternion;

            double mag = Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            mag.ShouldEqual(1.0, 1e-14);

            var rotMatrix = M3.Rotate(q);
            rotMatrix.determinant.ShouldEqual(1.0, 1e-14);
            (rotMatrix * rotMatrix.transpose).ShouldEqual(M3.identity, 1e-14);
        }

        [Fact]
        private void RotationQuaternionHandlesNegativeDeterminant()
        {
            var m = new M3(-1, 0, 0,
                0, 1, 0,
                0, 0, 1);

            Q3 q = m.rotation_quaternion;

            var rotMatrix = M3.Rotate(q);
            rotMatrix.determinant.ShouldEqual(1.0, 1e-14);
        }

        [Fact]
        private void RotationQuaternionRoundTrip()
        {
            var original = Q3.AngleAxis(1.234, new V3(5, -3, 7).normalized);
            var m        = M3.Rotate(original);
            Q3  q        = m.rotation_quaternion;
            var m2       = M3.Rotate(q);

            m2.ShouldEqual(m, 1e-14);
        }

        [Fact]
        private void OrthonormalizeQuaternionConsistency()
        {
            var m = new M3(1, 0.5, 0.3,
                0.2, 1, 0.4,
                0.1, 0.2, 1);

            Q3 q1 = m.rotation_quaternion;
            Q3 q2 = m.orthonormalized.quaternion;

            double angle = Q3.Angle(q1, q2);
            angle.ShouldBeZero(1e-14);
        }

        [Fact]
        private void QuaternionSmallAnglePrecision()
        {
            const double TINY     = 1e-10;
            var          original = Q3.AngleAxis(TINY, V3.zaxis);
            var          m        = M3.Rotate(original);

            Q3 q = m.quaternion;

            double angle = Q3.Angle(q, original);
            angle.ShouldBeZero(1e-20);
        }

        [Fact]
        private void OrthonormalizeNearlyParallelColumns()
        {
            var m = new M3(1, 1 + 1e-10, 0,
                0, 1e-10, 0,
                0, 0, 1);

            m = m.orthonormalized;

            V3 col0 = m.GetColumn(0);
            V3 col1 = m.GetColumn(1);
            V3 col2 = m.GetColumn(2);

            V3.Dot(col0, col1).ShouldBeZero(1e-14);
            V3.Dot(col1, col2).ShouldBeZero(1e-14);
            V3.Dot(col0, col2).ShouldBeZero(1e-14);
        }

        [Fact]
        private void QuaternionEulerAngleConsistency()
        {
            double roll  = PI / 6;
            double pitch = PI / 4;
            double yaw   = PI / 3;

            var qRoll     = Q3.AngleAxis(roll, V3.forward);
            var qPitch    = Q3.AngleAxis(pitch, V3.left);
            var qYaw      = Q3.AngleAxis(yaw, V3.up);
            Q3  qCombined = qYaw * qPitch * qRoll;

            var m           = M3.Rotate(qCombined);
            Q3  qFromMatrix = m.quaternion;

            double angle = Q3.Angle(qFromMatrix, qCombined);
            angle.ShouldBeZero(1e-14);
        }

        [Fact]
        private void OrthonormalizeRightHandedness()
        {
            var m = new M3(1, 1, 0,
                -1, 1, 0,
                0, 0, 1);

            m = m.orthonormalized;

            V3 col0 = m.GetColumn(0);
            V3 col1 = m.GetColumn(1);
            V3 col2 = m.GetColumn(2);

            V3.Cross(col0, col1).ShouldEqual(col2, 1e-14);
            m.determinant.ShouldEqual(1.0, 1e-14);
        }
    }
}
