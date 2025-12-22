/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using Xunit;
using static MechJebLib.Utils.Statics;
using static System.Math;

namespace MechJebLibTest.Primitives.Q3Tests
{
    public class MatrixConversionTests
    {
        [Fact]
        private void LookRotationForwardOnly()
        {
            var q = Q3.LookRotation(V3.forward);

            (q * V3.forward).ShouldEqual(V3.forward, 1e-14);
            (q * V3.up).ShouldEqual(V3.up, 1e-14);
            q.ShouldEqual(Q3.identity, 1e-14);
        }


            [Fact]
            private void LookRotationBasicDirections()
            {
                var q = Q3.LookRotation(V3.forward);
                (q * V3.forward).ShouldEqual(V3.forward, 1e-14);

                q = Q3.LookRotation(V3.back);
                (q * V3.forward).ShouldEqual(V3.back, 1e-14);

                q = Q3.LookRotation(V3.right);
                (q * V3.forward).ShouldEqual(V3.right, 1e-14);

                q = Q3.LookRotation(V3.left);
                (q * V3.forward).ShouldEqual(V3.left, 1e-14);
            }

        [Fact]
        private void LookRotationWithCustomUp()
        {
            V3 forward = V3.right;
            V3 up = V3.forward;
            var q = Q3.LookRotation(forward, up);

            (q * V3.forward).ShouldEqual(forward.normalized, 1e-14);

            V3 transformedUp = q * V3.up;
            V3.Dot(transformedUp, up).ShouldBePositive();
        }

        [Fact]
        private void LookRotationNormalizesInput()
        {
            var forward = new V3(10, 0, 0);
            var q = Q3.LookRotation(forward);

            (q * V3.forward).ShouldEqual(V3.xaxis, 1e-14);

            forward = new V3(3, 4, 0);
            q = Q3.LookRotation(forward);
            (q * V3.forward).ShouldEqual(forward.normalized, 1e-14);
        }

        [Fact]
        private void LookRotationArbitraryDirection()
        {
            V3 forward = new V3(1, 2, 3).normalized;
            var q = Q3.LookRotation(forward);

            (q * V3.forward).ShouldEqual(forward, 1e-14);

            V3 rotatedUp = q * V3.up;
            V3.Dot(rotatedUp, forward).ShouldBeZero(1e-14);
        }

        [Fact]
        private void LookRotationWithParallelUpAndForward()
        {
            V3  forward = V3.up;
            V3  up      = V3.up;
            var q       = Q3.LookRotation(forward, up);

            // Degenerate case falls back to FromToRotation(V3.forward, <forward argument>)
            (q * V3.forward).ShouldEqual(V3.up, 1e-14);
        }

        [Fact]
        private void LookRotationDegenerateCaseProducesIdentity()
        {
            var q = Q3.LookRotation(V3.forward, V3.forward);

            q.ShouldEqual(Q3.identity, 1e-14);
        }

        [Fact]
        private void LookRotationOppositeDirection()
        {
            var q = Q3.LookRotation(V3.back);

            (q * V3.forward).ShouldEqual(V3.back, 1e-14);

            double angle = Q3.Angle(Q3.identity, q);
            angle.ShouldEqual(PI, 1e-14);
        }

        [Fact]
        private void LookRotationSmallAngles()
        {
            V3 forward = new V3(1, 1e-10, 0).normalized;
            var q = Q3.LookRotation(forward);

            (q * V3.forward).ShouldEqual(forward, 1e-14);
            Assert.True(IsFinite(q.x));
            Assert.True(IsFinite(q.y));
            Assert.True(IsFinite(q.z));
            Assert.True(IsFinite(q.w));
        }

        [Fact]
        private void LookRotationConsistencyWithMatrix()
        {
            V3  forward = new V3(1, 2, 3).normalized;
            V3  up      = new V3(0, 1, 1).normalized;
            var q       = Q3.LookRotation(forward, up);
            var m       = M3.Rotate(q);

            (q * V3.forward).ShouldEqual(forward, 1e-14);
            (m * V3.forward).ShouldEqual(forward, 1e-14);
            V3.Dot(q * V3.up, up).ShouldBePositive();
        }

        [Fact]
        private void FromToRotationBasicAxes()
        {
            var q = Q3.FromToRotation(V3.forward, V3.right);

            (q * V3.forward).ShouldEqual(V3.right, 1e-14);

            q = Q3.FromToRotation(V3.up, V3.down);
            (q * V3.up).ShouldEqual(V3.down, 1e-14);
        }

        [Fact]
        private void FromToRotationIdentity()
        {
            var q = Q3.FromToRotation(V3.forward, V3.forward);

            q.ShouldEqual(Q3.identity, 1e-14);
        }

        [Fact]
        private void FromToRotationOpposite()
        {
            var q = Q3.FromToRotation(V3.forward, V3.back);

            (q * V3.forward).ShouldEqual(V3.back, 1e-14);

            double angle = Q3.Angle(Q3.identity, q);
            angle.ShouldEqual(PI, 1e-14);
        }

        [Fact]
        private void FromToRotationArbitraryVectors()
        {
            V3 from = new V3(1, 2, 3).normalized;
            V3 to = new V3(4, -5, 6).normalized;
            var q = Q3.FromToRotation(from, to);

            (q * from).ShouldEqual(to, 1e-14);
        }

        [Fact]
        private void FromToRotationNonNormalizedInput()
        {
            var from = new V3(2, 0, 0);
            var to = new V3(0, 3, 0);
            var q = Q3.FromToRotation(from, to);

            (q * from.normalized).ShouldEqual(to.normalized, 1e-14);
        }

        [Fact]
        private void FromToRotationSmallAngle()
        {
            var from = new V3(1, 0, 0);
            V3 to = new V3(1, 1e-10, 0).normalized;
            var q = Q3.FromToRotation(from, to);

            (q * from).ShouldEqual(to);

            double angle = Q3.Angle(Q3.identity, q);
            angle.ShouldEqual(V3.Angle(from, to));
        }

        [Fact]
        private void FromToRotationPreservesOrthogonalVectors()
        {
            V3 from = V3.forward;
            V3 to = V3.right;
            var q = Q3.FromToRotation(from, to);

            V3 orthogonal = V3.up;
            V3 rotatedOrthogonal = q * orthogonal;

            V3.Dot(rotatedOrthogonal, to).ShouldBeZero(1e-14);
            rotatedOrthogonal.magnitude.ShouldEqual(1.0, 1e-14);
        }

        [Fact]
        private void FromToRotationChaining()
        {
            var v1 = new V3(1, 0, 0);
            V3 v2 = new V3(1, 1, 0).normalized;
            var v3 = new V3(0, 1, 0);

            var q1 = Q3.FromToRotation(v1, v2);
            var q2 = Q3.FromToRotation(v2, v3);
            var qDirect = Q3.FromToRotation(v1, v3);

            (q2 * q1).ShouldEqual(qDirect, 1e-14);
        }

        [Fact]
        private void AngleAxisBasicRotations()
        {
            var q = Q3.AngleAxis(PI / 2, V3.up);

            (q * V3.forward).ShouldEqual(V3.left, 1e-14);
            (q * V3.right).ShouldEqual(V3.forward, 1e-14);
        }

        [Fact]
        private void AngleAxisZeroAngle()
        {
            var q = Q3.AngleAxis(0, V3.up);

            q.ShouldEqual(Q3.identity, 1e-14);
        }

        [Fact]
        private void AngleAxisNormalizesAxis()
        {
            var q1 = Q3.AngleAxis(PI / 3, new V3(2, 0, 0));
            var q2 = Q3.AngleAxis(PI / 3, V3.xaxis);

            q1.ShouldEqual(q2, 1e-14);
        }

        [Fact]
        private void AngleAxisNegativeAngle()
        {
            var qPos = Q3.AngleAxis(PI / 4, V3.up);
            var qNeg = Q3.AngleAxis(-PI / 4, V3.up);

            (qPos * qNeg).ShouldEqual(Q3.identity, 1e-14);
        }

        [Fact]
        private void AngleAxisLargeAngle()
        {
            var q1 = Q3.AngleAxis(4 * PI + PI / 3, V3.up);
            var q2 = Q3.AngleAxis(PI / 3, V3.up);

            q1.ShouldEqual(q2, 1e-14);
        }

        [Fact]
        private void InverseBasicQuaternions()
        {
            var q = Q3.AngleAxis(PI / 3, new V3(1, 2, 3).normalized);
            var qInv = Q3.Inverse(q);

            (q * qInv).ShouldEqual(Q3.identity, 1e-14);
            (qInv * q).ShouldEqual(Q3.identity, 1e-14);
        }

        [Fact]
        private void InverseIdentity()
        {
            Q3.Inverse(Q3.identity).ShouldEqual(Q3.identity, 1e-14);
        }

        [Fact]
        private void InverseUndoesRotation()
        {
            var q = Q3.AngleAxis(1.234, new V3(3, -2, 1).normalized);
            var v = new V3(5, 6, 7);

            V3 rotated = q * v;
            V3 unrotated = Q3.Inverse(q) * rotated;

            unrotated.ShouldEqual(v, 1e-14);
        }

        [Fact]
        private void InverseOfNormalizedQuaternion()
        {
            var q = Q3.AngleAxis(0.789, new V3(1, 1, 1).normalized);
            q = Q3.Normalize(q);

            var qInv = Q3.Inverse(q);

            qInv.x.ShouldEqual(-q.x, 1e-14);
            qInv.y.ShouldEqual(-q.y, 1e-14);
            qInv.z.ShouldEqual(-q.z, 1e-14);
            qInv.w.ShouldEqual(q.w, 1e-14);
        }

        [Fact]
        private void NormalizeBasicQuaternions()
        {
            var q = new Q3(1, 1, 1, 1);
            var normalized = Q3.Normalize(q);

            normalized.x.ShouldEqual(0.5, 1e-14);
            normalized.y.ShouldEqual(0.5, 1e-14);
            normalized.z.ShouldEqual(0.5, 1e-14);
            normalized.w.ShouldEqual(0.5, 1e-14);

            double mag = Sqrt(normalized.x * normalized.x + normalized.y * normalized.y +
                          normalized.z * normalized.z + normalized.w * normalized.w);
            mag.ShouldEqual(1.0, 1e-14);
        }

        [Fact]
        private void NormalizeZeroQuaternion()
        {
            var q = new Q3(0, 0, 0, 0);
            var normalized = Q3.Normalize(q);

            normalized.ShouldEqual(Q3.identity, 1e-14);
        }

        [Fact]
        private void NormalizeSmallQuaternion()
        {
            var q = new Q3(1e-200, 1e-200, 1e-200, 1e-200);
            var normalized = Q3.Normalize(q);

            double mag = Sqrt(normalized.x * normalized.x + normalized.y * normalized.y +
                          normalized.z * normalized.z + normalized.w * normalized.w);
            mag.ShouldEqual(1.0, 1e-14);
        }

        [Fact]
        private void NormalizeInPlace()
        {
            var q = new Q3(3, 4, 0, 0);
            q.Normalize();

            double mag = Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            mag.ShouldEqual(1.0, 1e-14);
        }

        [Fact]
        private void NormalizedProperty()
        {
            var q = new Q3(2, 3, 6, 7);
            Q3 normalized = q.normalized;

            double mag = Sqrt(normalized.x * normalized.x + normalized.y * normalized.y +
                          normalized.z * normalized.z + normalized.w * normalized.w);
            mag.ShouldEqual(1.0, 1e-14);
        }
    }
}
