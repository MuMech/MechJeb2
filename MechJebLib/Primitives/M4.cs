/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using static System.Math;
using static MechJebLib.Utils.Statics;

// ReSharper disable CompareOfFloatsByEqualityOperator
// ReSharper disable InconsistentNaming
namespace MechJebLib.Primitives
{
    /// <summary>
    ///     A 4x4 double-precision matrix.  Primarily for quadratic forms on quaternions (q^T * M * q), where the
    ///     quaternion is treated as a 4-vector.
    /// </summary>
    /// <remarks>
    ///     Matrix layout (row, column):
    ///     <code>
    ///         m00 m01 m02 m03
    ///         m10 m11 m12 m13
    ///         m20 m21 m22 m23
    ///         m30 m31 m32 m33
    ///     </code>
    ///     When multiplied with a Q3 the rows and columns are indexed in (x, y, z, w) order, matching the Q3 indexer.
    ///     Internal storage is column-major.
    ///     This is a readonly struct - all operations return new instances.
    /// </remarks>
    public readonly struct M4 : IEquatable<M4>, IFormattable
    {
        #region Fields

        // Column 0 elements
        /// <summary>Element at row 0, column 0.</summary>
        public readonly double m00;

        /// <summary>Element at row 1, column 0.</summary>
        public readonly double m10;

        /// <summary>Element at row 2, column 0.</summary>
        public readonly double m20;

        /// <summary>Element at row 3, column 0.</summary>
        public readonly double m30;

        // Column 1 elements
        /// <summary>Element at row 0, column 1.</summary>
        public readonly double m01;

        /// <summary>Element at row 1, column 1.</summary>
        public readonly double m11;

        /// <summary>Element at row 2, column 1.</summary>
        public readonly double m21;

        /// <summary>Element at row 3, column 1.</summary>
        public readonly double m31;

        // Column 2 elements
        /// <summary>Element at row 0, column 2.</summary>
        public readonly double m02;

        /// <summary>Element at row 1, column 2.</summary>
        public readonly double m12;

        /// <summary>Element at row 2, column 2.</summary>
        public readonly double m22;

        /// <summary>Element at row 3, column 2.</summary>
        public readonly double m32;

        // Column 3 elements
        /// <summary>Element at row 0, column 3.</summary>
        public readonly double m03;

        /// <summary>Element at row 1, column 3.</summary>
        public readonly double m13;

        /// <summary>Element at row 2, column 3.</summary>
        public readonly double m23;

        /// <summary>Element at row 3, column 3.</summary>
        public readonly double m33;

        #endregion

        #region Constructors

        /// <summary>
        ///     Constructs a matrix from 16 scalar values in row-major order.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public M4(double m00, double m01, double m02, double m03,
            double m10, double m11, double m12, double m13,
            double m20, double m21, double m22, double m23,
            double m30, double m31, double m32, double m33)
        {
            this.m00 = m00;
            this.m10 = m10;
            this.m20 = m20;
            this.m30 = m30;
            this.m01 = m01;
            this.m11 = m11;
            this.m21 = m21;
            this.m31 = m31;
            this.m02 = m02;
            this.m12 = m12;
            this.m22 = m22;
            this.m32 = m32;
            this.m03 = m03;
            this.m13 = m13;
            this.m23 = m23;
            this.m33 = m33;
        }

        #endregion

        #region Static Constants

        /// <summary>
        ///     The zero matrix (all elements are 0).
        /// </summary>
        public static M4 zero { get; } = new M4(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

        /// <summary>
        ///     The identity matrix (diagonal elements are 1, all others are 0).
        /// </summary>
        public static M4 identity { get; } = new M4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);

        #endregion

        #region Indexers

        /// <summary>
        ///     Accesses matrix elements by row and column indices (read-only).
        /// </summary>
        /// <param name="row">Row index [0..3].</param>
        /// <param name="column">Column index [0..3].</param>
        /// <returns>The element at the specified position.</returns>
        public double this[int row, int column]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this[row + column * 4];
        }

        /// <summary>
        ///     Accesses matrix elements by linear index in column-major order (read-only).
        /// </summary>
        /// <param name="index">Linear index [0..15] in column-major order.</param>
        /// <returns>The element at the specified index.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown when index is outside [0..15].</exception>
        public double this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                switch (index)
                {
                    case 0:  return m00;
                    case 1:  return m10;
                    case 2:  return m20;
                    case 3:  return m30;
                    case 4:  return m01;
                    case 5:  return m11;
                    case 6:  return m21;
                    case 7:  return m31;
                    case 8:  return m02;
                    case 9:  return m12;
                    case 10: return m22;
                    case 11: return m32;
                    case 12: return m03;
                    case 13: return m13;
                    case 14: return m23;
                    case 15: return m33;
                    default:
                        throw new IndexOutOfRangeException("Invalid matrix index!");
                }
            }
        }

        #endregion

        #region Arithmetic Operators

        /// <summary>
        ///     Multiplies two matrices together.
        /// </summary>
        /// <param name="lhs">Left-hand side matrix.</param>
        /// <param name="rhs">Right-hand side matrix.</param>
        /// <returns>The product matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M4 operator *(in M4 lhs, in M4 rhs) =>
            new M4(
                lhs.m00 * rhs.m00 + lhs.m01 * rhs.m10 + lhs.m02 * rhs.m20 + lhs.m03 * rhs.m30,
                lhs.m00 * rhs.m01 + lhs.m01 * rhs.m11 + lhs.m02 * rhs.m21 + lhs.m03 * rhs.m31,
                lhs.m00 * rhs.m02 + lhs.m01 * rhs.m12 + lhs.m02 * rhs.m22 + lhs.m03 * rhs.m32,
                lhs.m00 * rhs.m03 + lhs.m01 * rhs.m13 + lhs.m02 * rhs.m23 + lhs.m03 * rhs.m33,
                lhs.m10 * rhs.m00 + lhs.m11 * rhs.m10 + lhs.m12 * rhs.m20 + lhs.m13 * rhs.m30,
                lhs.m10 * rhs.m01 + lhs.m11 * rhs.m11 + lhs.m12 * rhs.m21 + lhs.m13 * rhs.m31,
                lhs.m10 * rhs.m02 + lhs.m11 * rhs.m12 + lhs.m12 * rhs.m22 + lhs.m13 * rhs.m32,
                lhs.m10 * rhs.m03 + lhs.m11 * rhs.m13 + lhs.m12 * rhs.m23 + lhs.m13 * rhs.m33,
                lhs.m20 * rhs.m00 + lhs.m21 * rhs.m10 + lhs.m22 * rhs.m20 + lhs.m23 * rhs.m30,
                lhs.m20 * rhs.m01 + lhs.m21 * rhs.m11 + lhs.m22 * rhs.m21 + lhs.m23 * rhs.m31,
                lhs.m20 * rhs.m02 + lhs.m21 * rhs.m12 + lhs.m22 * rhs.m22 + lhs.m23 * rhs.m32,
                lhs.m20 * rhs.m03 + lhs.m21 * rhs.m13 + lhs.m22 * rhs.m23 + lhs.m23 * rhs.m33,
                lhs.m30 * rhs.m00 + lhs.m31 * rhs.m10 + lhs.m32 * rhs.m20 + lhs.m33 * rhs.m30,
                lhs.m30 * rhs.m01 + lhs.m31 * rhs.m11 + lhs.m32 * rhs.m21 + lhs.m33 * rhs.m31,
                lhs.m30 * rhs.m02 + lhs.m31 * rhs.m12 + lhs.m32 * rhs.m22 + lhs.m33 * rhs.m32,
                lhs.m30 * rhs.m03 + lhs.m31 * rhs.m13 + lhs.m32 * rhs.m23 + lhs.m33 * rhs.m33
            );

        /// <summary>
        ///     Multiplies all matrix elements by a scalar.
        /// </summary>
        /// <param name="lhs">The matrix.</param>
        /// <param name="value">The scalar value.</param>
        /// <returns>The scaled matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M4 operator *(in M4 lhs, double value) =>
            new M4(
                lhs.m00 * value, lhs.m01 * value, lhs.m02 * value, lhs.m03 * value,
                lhs.m10 * value, lhs.m11 * value, lhs.m12 * value, lhs.m13 * value,
                lhs.m20 * value, lhs.m21 * value, lhs.m22 * value, lhs.m23 * value,
                lhs.m30 * value, lhs.m31 * value, lhs.m32 * value, lhs.m33 * value
            );

        /// <summary>
        ///     Multiplies all matrix elements by a scalar.
        /// </summary>
        /// <param name="value">The scalar value.</param>
        /// <param name="rhs">The matrix.</param>
        /// <returns>The scaled matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M4 operator *(double value, in M4 rhs) => rhs * value;

        /// <summary>
        ///     Divides all matrix elements by a scalar.
        /// </summary>
        /// <param name="lhs">The matrix.</param>
        /// <param name="value">The scalar divisor.</param>
        /// <returns>The scaled matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M4 operator /(in M4 lhs, double value) =>
            new M4(
                lhs.m00 / value, lhs.m01 / value, lhs.m02 / value, lhs.m03 / value,
                lhs.m10 / value, lhs.m11 / value, lhs.m12 / value, lhs.m13 / value,
                lhs.m20 / value, lhs.m21 / value, lhs.m22 / value, lhs.m23 / value,
                lhs.m30 / value, lhs.m31 / value, lhs.m32 / value, lhs.m33 / value
            );

        /// <summary>
        ///     Adds two matrices element-wise.
        /// </summary>
        /// <param name="lhs">First matrix.</param>
        /// <param name="rhs">Second matrix.</param>
        /// <returns>The sum of the matrices.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M4 operator +(in M4 lhs, in M4 rhs) =>
            new M4(
                lhs.m00 + rhs.m00, lhs.m01 + rhs.m01, lhs.m02 + rhs.m02, lhs.m03 + rhs.m03,
                lhs.m10 + rhs.m10, lhs.m11 + rhs.m11, lhs.m12 + rhs.m12, lhs.m13 + rhs.m13,
                lhs.m20 + rhs.m20, lhs.m21 + rhs.m21, lhs.m22 + rhs.m22, lhs.m23 + rhs.m23,
                lhs.m30 + rhs.m30, lhs.m31 + rhs.m31, lhs.m32 + rhs.m32, lhs.m33 + rhs.m33
            );

        /// <summary>
        ///     Subtracts two matrices element-wise.
        /// </summary>
        /// <param name="lhs">First matrix.</param>
        /// <param name="rhs">Second matrix to subtract.</param>
        /// <returns>The difference of the matrices.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M4 operator -(in M4 lhs, in M4 rhs) =>
            new M4(
                lhs.m00 - rhs.m00, lhs.m01 - rhs.m01, lhs.m02 - rhs.m02, lhs.m03 - rhs.m03,
                lhs.m10 - rhs.m10, lhs.m11 - rhs.m11, lhs.m12 - rhs.m12, lhs.m13 - rhs.m13,
                lhs.m20 - rhs.m20, lhs.m21 - rhs.m21, lhs.m22 - rhs.m22, lhs.m23 - rhs.m23,
                lhs.m30 - rhs.m30, lhs.m31 - rhs.m31, lhs.m32 - rhs.m32, lhs.m33 - rhs.m33
            );

        /// <summary>
        ///     Negates all matrix elements.
        /// </summary>
        /// <param name="m">The matrix to negate.</param>
        /// <returns>The negated matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M4 operator -(in M4 m) =>
            new M4(
                -m.m00, -m.m01, -m.m02, -m.m03,
                -m.m10, -m.m11, -m.m12, -m.m13,
                -m.m20, -m.m21, -m.m22, -m.m23,
                -m.m30, -m.m31, -m.m32, -m.m33
            );

        #endregion

        #region Quaternion Operators

        /// <summary>
        ///     Multiplies a matrix by a quaternion treated as a column 4-vector (x, y, z, w).
        /// </summary>
        /// <param name="lhs">The matrix.</param>
        /// <param name="q">The quaternion.</param>
        /// <returns>The product M * q as a quaternion.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Q3 operator *(in M4 lhs, in Q3 q) =>
            new Q3(
                lhs.m00 * q.x + lhs.m01 * q.y + lhs.m02 * q.z + lhs.m03 * q.w,
                lhs.m10 * q.x + lhs.m11 * q.y + lhs.m12 * q.z + lhs.m13 * q.w,
                lhs.m20 * q.x + lhs.m21 * q.y + lhs.m22 * q.z + lhs.m23 * q.w,
                lhs.m30 * q.x + lhs.m31 * q.y + lhs.m32 * q.z + lhs.m33 * q.w
            );

        /// <summary>
        ///     Multiplies a quaternion treated as a row 4-vector (x, y, z, w) by a matrix.  Equivalent to M^T * q.
        /// </summary>
        /// <remarks>
        ///     Beware that <c>q * M * q</c> parses as <c>(q * M) * q</c> which is the Hamilton product and returns a Q3,
        ///     not the scalar quadratic form.  Use <see cref="QuadraticForm(in Q3)" /> or <c>Q3.Dot(q, M * q)</c>.
        /// </remarks>
        /// <param name="q">The quaternion.</param>
        /// <param name="rhs">The matrix.</param>
        /// <returns>The product q^T * M as a quaternion.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Q3 operator *(in Q3 q, in M4 rhs) =>
            new Q3(
                q.x * rhs.m00 + q.y * rhs.m10 + q.z * rhs.m20 + q.w * rhs.m30,
                q.x * rhs.m01 + q.y * rhs.m11 + q.z * rhs.m21 + q.w * rhs.m31,
                q.x * rhs.m02 + q.y * rhs.m12 + q.z * rhs.m22 + q.w * rhs.m32,
                q.x * rhs.m03 + q.y * rhs.m13 + q.z * rhs.m23 + q.w * rhs.m33
            );

        /// <summary>
        ///     Multiplies a matrix by a dual quaternion treated as a column 4-vector (x, y, z, w).  Since the matrix is
        ///     constant this is linear in both the value and the derivative.
        /// </summary>
        /// <param name="lhs">The matrix.</param>
        /// <param name="q">The dual quaternion.</param>
        /// <returns>The product M * q as a dual quaternion.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DualQ3 operator *(in M4 lhs, in DualQ3 q) => new DualQ3(lhs * q.M, lhs * q.D);

        /// <summary>
        ///     Computes the quadratic form q^T * M * q with the quaternion treated as a 4-vector (x, y, z, w).
        /// </summary>
        /// <param name="q">The quaternion.</param>
        /// <returns>The scalar q^T * M * q.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double QuadraticForm(in Q3 q) => Q3.Dot(q, this * q);

        /// <summary>
        ///     Computes the quadratic form q^T * M * q with the dual quaternion treated as a 4-vector (x, y, z, w).
        ///     The derivative is dq^T * (M + M^T) * q.
        /// </summary>
        /// <param name="q">The dual quaternion.</param>
        /// <returns>The dual scalar q^T * M * q.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Dual QuadraticForm(in DualQ3 q)
        {
            DualQ3 mq = this * q;
            return q.x * mq.x + q.y * mq.y + q.z * mq.z + q.w * mq.w;
        }

        #endregion

        #region Equality Operators

        /// <summary>
        ///     Tests for strict element-wise equality between two matrices.
        ///     Returns false if any element is NaN.
        /// </summary>
        /// <param name="lhs">First matrix.</param>
        /// <param name="rhs">Second matrix.</param>
        /// <returns>True if all elements are exactly equal.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in M4 lhs, in M4 rhs) =>
            lhs.m00 == rhs.m00 && lhs.m01 == rhs.m01 && lhs.m02 == rhs.m02 && lhs.m03 == rhs.m03 &&
            lhs.m10 == rhs.m10 && lhs.m11 == rhs.m11 && lhs.m12 == rhs.m12 && lhs.m13 == rhs.m13 &&
            lhs.m20 == rhs.m20 && lhs.m21 == rhs.m21 && lhs.m22 == rhs.m22 && lhs.m23 == rhs.m23 &&
            lhs.m30 == rhs.m30 && lhs.m31 == rhs.m31 && lhs.m32 == rhs.m32 && lhs.m33 == rhs.m33;

        /// <summary>
        ///     Tests for strict element-wise inequality between two matrices.
        ///     Returns true if any element is NaN.
        /// </summary>
        /// <param name="lhs">First matrix.</param>
        /// <param name="rhs">Second matrix.</param>
        /// <returns>True if any element differs.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in M4 lhs, in M4 rhs) => !(lhs == rhs);

        #endregion

        #region Matrix Properties

        /// <summary>
        ///     Gets the trace of the matrix (sum of diagonal elements).
        /// </summary>
        public double trace => m00 + m11 + m22 + m33;

        /// <summary>
        ///     Gets the transpose of the matrix (rows and columns swapped).
        /// </summary>
        public M4 transpose =>
            new M4(
                m00, m10, m20, m30,
                m01, m11, m21, m31,
                m02, m12, m22, m32,
                m03, m13, m23, m33
            );

        /// <summary>
        ///     Gets the transpose of the matrix (rows and columns swapped).
        /// </summary>
        public M4 T() => transpose;

        /// <summary>
        ///     Gets whether this matrix is the identity matrix (strict comparison).
        /// </summary>
        public bool isIdentity =>
            m00 == 1.0 && m10 == 0.0 && m20 == 0.0 && m30 == 0.0 &&
            m01 == 0.0 && m11 == 1.0 && m21 == 0.0 && m31 == 0.0 &&
            m02 == 0.0 && m12 == 0.0 && m22 == 1.0 && m32 == 0.0 &&
            m03 == 0.0 && m13 == 0.0 && m23 == 0.0 && m33 == 1.0;

        /// <summary>
        ///     Gets whether this matrix is symmetric (M ≈ M^T).
        /// </summary>
        public bool isSymmetric => NearlyEqual(this, transpose);

        /// <summary>
        ///     Gets the maximum absolute element value in the matrix.
        /// </summary>
        public double max_magnitude
        {
            get
            {
                double max = 0;
                for (int i = 0; i < 16; i++)
                    max = Max(max, Abs(this[i]));
                return max;
            }
        }

        #endregion

        #region Matrix Construction (Static Factory Methods)

        /// <summary>
        ///     Creates a diagonal matrix with the same value on all diagonal elements.
        /// </summary>
        /// <param name="d">The diagonal value.</param>
        /// <returns>A diagonal matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M4 Diagonal(double d) => Diagonal(d, d, d, d);

        /// <summary>
        ///     Creates a diagonal matrix from four scalar values.
        /// </summary>
        /// <param name="x">First diagonal element (m00).</param>
        /// <param name="y">Second diagonal element (m11).</param>
        /// <param name="z">Third diagonal element (m22).</param>
        /// <param name="w">Fourth diagonal element (m33).</param>
        /// <returns>A diagonal matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M4 Diagonal(double x, double y, double z, double w) => new M4(x, 0, 0, 0, 0, y, 0, 0, 0, 0, z, 0, 0, 0, 0, w);

        #endregion

        #region Data Transfer

        /// <summary>
        ///     Creates a matrix from a 2D array.
        /// </summary>
        /// <param name="array">Source 2D array.</param>
        /// <param name="x">Starting row index in the source array.</param>
        /// <param name="y">Starting column index in the source array.</param>
        /// <returns>A new matrix containing the values from the array.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M4 CopyFrom(double[,] array, int x, int y) =>
            new M4(
                array[x, y], array[x, y + 1], array[x, y + 2], array[x, y + 3],
                array[x + 1, y], array[x + 1, y + 1], array[x + 1, y + 2], array[x + 1, y + 3],
                array[x + 2, y], array[x + 2, y + 1], array[x + 2, y + 2], array[x + 2, y + 3],
                array[x + 3, y], array[x + 3, y + 1], array[x + 3, y + 2], array[x + 3, y + 3]
            );

        /// <summary>
        ///     Creates a matrix from a 1D array in row-major order.
        /// </summary>
        /// <param name="array">Source 1D array.</param>
        /// <param name="offset">Starting index in the source array.</param>
        /// <returns>A new matrix containing the values from the array.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M4 CopyFrom(IList<double> array, int offset) =>
            new M4(
                array[offset], array[offset + 1], array[offset + 2], array[offset + 3],
                array[offset + 4], array[offset + 5], array[offset + 6], array[offset + 7],
                array[offset + 8], array[offset + 9], array[offset + 10], array[offset + 11],
                array[offset + 12], array[offset + 13], array[offset + 14], array[offset + 15]
            );

        /// <summary>
        ///     Copies this matrix to a 2D array.
        /// </summary>
        /// <param name="other">Target 2D array.</param>
        /// <param name="x">Starting row index in the target array.</param>
        /// <param name="y">Starting column index in the target array.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(double[,] other, int x, int y)
        {
            for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
                other[x + i, y + j] = this[i, j];
        }

        /// <summary>
        ///     Copies this matrix to a 1D array in row-major order.
        /// </summary>
        /// <param name="other">Target 1D array.</param>
        /// <param name="offset">Starting index in the target array.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(IList<double> other, int offset)
        {
            for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
                other[offset + i * 4 + j] = this[i, j];
        }

        #endregion

        #region Equality and Hashing

        /// <summary>
        ///     Tests for strict element-wise equality with another matrix.
        ///     Returns true for NaN comparisons (unlike the == operator).
        /// </summary>
        /// <param name="other">Matrix to compare with.</param>
        /// <returns>True if all elements are equal.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(M4 other) =>
            m00.Equals(other.m00) && m01.Equals(other.m01) && m02.Equals(other.m02) && m03.Equals(other.m03) &&
            m10.Equals(other.m10) && m11.Equals(other.m11) && m12.Equals(other.m12) && m13.Equals(other.m13) &&
            m20.Equals(other.m20) && m21.Equals(other.m21) && m22.Equals(other.m22) && m23.Equals(other.m23) &&
            m30.Equals(other.m30) && m31.Equals(other.m31) && m32.Equals(other.m32) && m33.Equals(other.m33);

        /// <summary>
        ///     Tests for equality with an object.
        /// </summary>
        /// <param name="other">Object to compare with.</param>
        /// <returns>True if the object is an M4 with equal elements.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object? other) => other is M4 m && Equals(m);

        /// <summary>
        ///     Computes a hash code for the matrix.
        /// </summary>
        /// <returns>A hash code combining all elements.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = m00.GetHashCode();
                hash = (hash * 397) ^ m01.GetHashCode();
                hash = (hash * 397) ^ m02.GetHashCode();
                hash = (hash * 397) ^ m03.GetHashCode();
                hash = (hash * 397) ^ m10.GetHashCode();
                hash = (hash * 397) ^ m11.GetHashCode();
                hash = (hash * 397) ^ m12.GetHashCode();
                hash = (hash * 397) ^ m13.GetHashCode();
                hash = (hash * 397) ^ m20.GetHashCode();
                hash = (hash * 397) ^ m21.GetHashCode();
                hash = (hash * 397) ^ m22.GetHashCode();
                hash = (hash * 397) ^ m23.GetHashCode();
                hash = (hash * 397) ^ m30.GetHashCode();
                hash = (hash * 397) ^ m31.GetHashCode();
                hash = (hash * 397) ^ m32.GetHashCode();
                hash = (hash * 397) ^ m33.GetHashCode();
                return hash;
            }
        }

        #endregion

        #region String Conversion

        /// <summary>
        ///     Converts the matrix to a string representation.
        /// </summary>
        /// <returns>A multi-line string showing the matrix in row format.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => ToString(null, CultureInfo.InvariantCulture.NumberFormat);

        /// <summary>
        ///     Converts the matrix to a string with a specified numeric format.
        /// </summary>
        /// <param name="format">Numeric format string (e.g., "F2", "G", "E3").</param>
        /// <returns>A formatted string representation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ToString(string? format) => ToString(format, CultureInfo.InvariantCulture.NumberFormat);

        /// <summary>
        ///     Converts the matrix to a string with specified format and culture.
        /// </summary>
        /// <param name="format">Numeric format string.</param>
        /// <param name="formatProvider">Culture-specific format provider.</param>
        /// <returns>A formatted string representation.</returns>
        public string ToString(string? format, IFormatProvider formatProvider)
        {
            if (string.IsNullOrEmpty(format))
                format = "G";
            return string.Format("[{0}, {1}, {2}, {3}\n{4}, {5}, {6}, {7}\n{8}, {9}, {10}, {11}\n{12}, {13}, {14}, {15}]\n",
                m00.ToString(format, formatProvider), m01.ToString(format, formatProvider), m02.ToString(format, formatProvider),
                m03.ToString(format, formatProvider),
                m10.ToString(format, formatProvider), m11.ToString(format, formatProvider), m12.ToString(format, formatProvider),
                m13.ToString(format, formatProvider),
                m20.ToString(format, formatProvider), m21.ToString(format, formatProvider), m22.ToString(format, formatProvider),
                m23.ToString(format, formatProvider),
                m30.ToString(format, formatProvider), m31.ToString(format, formatProvider), m32.ToString(format, formatProvider),
                m33.ToString(format, formatProvider));
        }

        #endregion
    }
}
