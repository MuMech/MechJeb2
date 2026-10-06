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
    ///     A 2x2 double-precision matrix.  Integrates with V2 (2D vectors) for planar rotations, with angles positive
    ///     counter-clockwise.
    /// </summary>
    /// <remarks>
    ///     Matrix layout (row, column):
    ///     <code>
    ///         m00 m01
    ///         m10 m11
    ///     </code>
    ///     Internal storage is column-major.
    ///     This is a readonly struct - all operations return new instances.
    /// </remarks>
    public readonly struct M2 : IEquatable<M2>, IFormattable
    {
        #region Fields

        // Column 0 elements
        /// <summary>Element at row 0, column 0.</summary>
        public readonly double m00;

        /// <summary>Element at row 1, column 0.</summary>
        public readonly double m10;

        // Column 1 elements
        /// <summary>Element at row 0, column 1.</summary>
        public readonly double m01;

        /// <summary>Element at row 1, column 1.</summary>
        public readonly double m11;

        #endregion

        #region Constructors

        /// <summary>
        ///     Constructs a matrix from 4 scalar values in row-major order.
        /// </summary>
        /// <param name="m00">Element at row 0, column 0.</param>
        /// <param name="m01">Element at row 0, column 1.</param>
        /// <param name="m10">Element at row 1, column 0.</param>
        /// <param name="m11">Element at row 1, column 1.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public M2(double m00, double m01, double m10, double m11)
        {
            this.m00 = m00;
            this.m10 = m10;
            this.m01 = m01;
            this.m11 = m11;
        }

        /// <summary>
        ///     Constructs a matrix from two column vectors.
        /// </summary>
        /// <param name="column0">First column vector.</param>
        /// <param name="column1">Second column vector.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public M2(in V2 column0, in V2 column1)
        {
            m00 = column0.x;
            m10 = column0.y;
            m01 = column1.x;
            m11 = column1.y;
        }

        #endregion

        #region Static Constants

        /// <summary>
        ///     The zero matrix (all elements are 0).
        /// </summary>
        public static M2 zero { get; } = new M2(0, 0, 0, 0);

        /// <summary>
        ///     The identity matrix (diagonal elements are 1, all others are 0).
        /// </summary>
        public static M2 identity { get; } = new M2(1, 0, 0, 1);

        #endregion

        #region Indexers

        /// <summary>
        ///     Accesses matrix elements by row and column indices (read-only).
        /// </summary>
        /// <param name="row">Row index [0..1].</param>
        /// <param name="column">Column index [0..1].</param>
        /// <returns>The element at the specified position.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown when row or column is outside [0..1].</exception>
        public double this[int row, int column]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                // check each index separately, an out-of-range row could otherwise alias another column
                if (row < 0 || row > 1 || column < 0 || column > 1)
                    throw new IndexOutOfRangeException("Invalid matrix index!");

                return this[row + column * 2];
            }
        }

        /// <summary>
        ///     Accesses matrix elements by linear index in column-major order (read-only).
        /// </summary>
        /// <param name="index">Linear index [0..3] in column-major order.</param>
        /// <returns>The element at the specified index.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown when index is outside [0..3].</exception>
        public double this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                switch (index)
                {
                    case 0: return m00;
                    case 1: return m10;
                    case 2: return m01;
                    case 3: return m11;
                    default:
                        throw new IndexOutOfRangeException("Invalid matrix index!");
                }
            }
        }

        #endregion

        #region Row and Column Access

        /// <summary>
        ///     Gets a column of the matrix as a vector.
        /// </summary>
        /// <param name="index">Column index [0..1].</param>
        /// <returns>The column as a V2 vector.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown when index is outside [0..1].</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public V2 GetColumn(int index)
        {
            switch (index)
            {
                case 0: return new V2(m00, m10);
                case 1: return new V2(m01, m11);
                default:
                    throw new IndexOutOfRangeException("Invalid column index!");
            }
        }

        /// <summary>
        ///     Gets a row of the matrix as a vector.
        /// </summary>
        /// <param name="index">Row index [0..1].</param>
        /// <returns>The row as a V2 vector.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown when index is outside [0..1].</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public V2 GetRow(int index)
        {
            switch (index)
            {
                case 0: return new V2(m00, m01);
                case 1: return new V2(m10, m11);
                default:
                    throw new IndexOutOfRangeException("Invalid row index!");
            }
        }

        /// <summary>
        ///     Returns a new matrix with the specified column replaced.
        /// </summary>
        /// <param name="index">Column index [0..1].</param>
        /// <param name="column">Vector containing the new column values.</param>
        /// <returns>A new matrix with the column replaced.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown when index is outside [0..1].</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public M2 WithColumn(int index, in V2 column)
        {
            switch (index)
            {
                case 0: return new M2(column.x, m01, column.y, m11);
                case 1: return new M2(m00, column.x, m10, column.y);
                default:
                    throw new IndexOutOfRangeException("Invalid column index!");
            }
        }

        /// <summary>
        ///     Returns a new matrix with the specified row replaced.
        /// </summary>
        /// <param name="index">Row index [0..1].</param>
        /// <param name="row">Vector containing the new row values.</param>
        /// <returns>A new matrix with the row replaced.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown when index is outside [0..1].</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public M2 WithRow(int index, in V2 row)
        {
            switch (index)
            {
                case 0: return new M2(row.x, row.y, m10, m11);
                case 1: return new M2(m00, m01, row.x, row.y);
                default:
                    throw new IndexOutOfRangeException("Invalid row index!");
            }
        }

        /// <summary>
        ///     Returns a new matrix with two rows swapped.
        /// </summary>
        /// <param name="i">First row index [0..1].</param>
        /// <param name="j">Second row index [0..1].</param>
        /// <returns>A new matrix with the rows swapped.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public M2 WithSwappedRows(int i, int j)
        {
            V2 rowI = GetRow(i);
            V2 rowJ = GetRow(j);
            return WithRow(i, rowJ).WithRow(j, rowI);
        }

        /// <summary>
        ///     Returns a new matrix with two columns swapped.
        /// </summary>
        /// <param name="i">First column index [0..1].</param>
        /// <param name="j">Second column index [0..1].</param>
        /// <returns>A new matrix with the columns swapped.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public M2 WithSwappedColumns(int i, int j)
        {
            V2 colI = GetColumn(i);
            V2 colJ = GetColumn(j);
            return WithColumn(i, colJ).WithColumn(j, colI);
        }

        #endregion

        #region Diagonal Access

        /// <summary>
        ///     Gets the diagonal elements as a vector.
        /// </summary>
        public V2 diagonal => new V2(m00, m11);

        /// <summary>
        ///     Returns a new matrix with the diagonal elements replaced.
        /// </summary>
        /// <param name="v">Vector containing the diagonal values.</param>
        /// <returns>A new matrix with the diagonal replaced.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public M2 WithDiagonal(in V2 v) => new M2(v.x, m01, m10, v.y);

        /// <summary>
        ///     Returns a new matrix with the diagonal elements replaced.
        /// </summary>
        /// <param name="x">First diagonal element (m00).</param>
        /// <param name="y">Second diagonal element (m11).</param>
        /// <returns>A new matrix with the diagonal replaced.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public M2 WithDiagonal(double x, double y) => new M2(x, m01, m10, y);

        #endregion

        #region Arithmetic Operators

        /// <summary>
        ///     Multiplies two matrices together.
        /// </summary>
        /// <param name="lhs">Left-hand side matrix.</param>
        /// <param name="rhs">Right-hand side matrix.</param>
        /// <returns>The product matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M2 operator *(in M2 lhs, in M2 rhs) =>
            new M2(
                lhs.m00 * rhs.m00 + lhs.m01 * rhs.m10,
                lhs.m00 * rhs.m01 + lhs.m01 * rhs.m11,
                lhs.m10 * rhs.m00 + lhs.m11 * rhs.m10,
                lhs.m10 * rhs.m01 + lhs.m11 * rhs.m11
            );

        /// <summary>
        ///     Transforms a vector by a matrix (M * v).
        /// </summary>
        /// <param name="lhs">The matrix.</param>
        /// <param name="vector">The vector to transform.</param>
        /// <returns>The transformed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static V2 operator *(in M2 lhs, in V2 vector) =>
            new V2(
                lhs.m00 * vector.x + lhs.m01 * vector.y,
                lhs.m10 * vector.x + lhs.m11 * vector.y
            );

        /// <summary>
        ///     Multiplies all matrix elements by a scalar.
        /// </summary>
        /// <param name="lhs">The matrix.</param>
        /// <param name="value">The scalar value.</param>
        /// <returns>The scaled matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M2 operator *(in M2 lhs, double value) =>
            new M2(
                lhs.m00 * value, lhs.m01 * value,
                lhs.m10 * value, lhs.m11 * value
            );

        /// <summary>
        ///     Multiplies all matrix elements by a scalar.
        /// </summary>
        /// <param name="value">The scalar value.</param>
        /// <param name="rhs">The matrix.</param>
        /// <returns>The scaled matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M2 operator *(double value, in M2 rhs) => rhs * value;

        /// <summary>
        ///     Divides all matrix elements by a scalar.
        /// </summary>
        /// <param name="lhs">The matrix.</param>
        /// <param name="value">The scalar divisor.</param>
        /// <returns>The scaled matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M2 operator /(in M2 lhs, double value) =>
            new M2(
                lhs.m00 / value, lhs.m01 / value,
                lhs.m10 / value, lhs.m11 / value
            );

        /// <summary>
        ///     Adds two matrices element-wise.
        /// </summary>
        /// <param name="lhs">First matrix.</param>
        /// <param name="rhs">Second matrix.</param>
        /// <returns>The sum of the matrices.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M2 operator +(in M2 lhs, in M2 rhs) =>
            new M2(
                lhs.m00 + rhs.m00, lhs.m01 + rhs.m01,
                lhs.m10 + rhs.m10, lhs.m11 + rhs.m11
            );

        /// <summary>
        ///     Subtracts two matrices element-wise.
        /// </summary>
        /// <param name="lhs">First matrix.</param>
        /// <param name="rhs">Second matrix to subtract.</param>
        /// <returns>The difference of the matrices.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M2 operator -(in M2 lhs, in M2 rhs) =>
            new M2(
                lhs.m00 - rhs.m00, lhs.m01 - rhs.m01,
                lhs.m10 - rhs.m10, lhs.m11 - rhs.m11
            );

        /// <summary>
        ///     Negates all matrix elements.
        /// </summary>
        /// <param name="m">The matrix to negate.</param>
        /// <returns>The negated matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M2 operator -(in M2 m) => new M2(-m.m00, -m.m01, -m.m10, -m.m11);

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
        public static bool operator ==(in M2 lhs, in M2 rhs) =>
            lhs.m00 == rhs.m00 && lhs.m01 == rhs.m01 &&
            lhs.m10 == rhs.m10 && lhs.m11 == rhs.m11;

        /// <summary>
        ///     Tests for strict element-wise inequality between two matrices.
        ///     Returns true if any element is NaN.
        /// </summary>
        /// <param name="lhs">First matrix.</param>
        /// <param name="rhs">Second matrix.</param>
        /// <returns>True if any element differs.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in M2 lhs, in M2 rhs) => !(lhs == rhs);

        #endregion

        #region Matrix Properties

        /// <summary>
        ///     Gets the trace of the matrix (sum of diagonal elements).
        /// </summary>
        public double trace => m00 + m11;

        /// <summary>
        ///     Gets the determinant of the matrix.
        /// </summary>
        public double determinant => m00 * m11 - m01 * m10;

        /// <summary>
        ///     Gets the transpose of the matrix (rows and columns swapped).
        /// </summary>
        public M2 transpose => new M2(m00, m10, m01, m11);

        /// <summary>
        ///     Gets the transpose of the matrix (rows and columns swapped).
        /// </summary>
        public M2 T() => transpose;

        /// <summary>
        ///     Gets the inverse of the matrix.
        /// </summary>
        public M2 inverse => Inverse(this);

        /// <summary>
        ///     Gets whether this matrix is the identity matrix (strict comparison).
        /// </summary>
        public bool isIdentity =>
            m00 == 1.0 && m10 == 0.0 &&
            m01 == 0.0 && m11 == 1.0;

        /// <summary>
        ///     Gets whether this matrix is orthogonal (M * M^T ≈ I to within 1e-12, which allows for the rounding error
        ///     accumulated over long chains of rotations).
        /// </summary>
        public bool isOrthogonal => NearlyEqual(this * transpose, identity, 1e-12);

        /// <summary>
        ///     Gets whether this matrix is symmetric (M ≈ M^T).
        /// </summary>
        public bool isSymmetric => NearlyEqual(this, transpose);

        /// <summary>
        ///     Gets whether this matrix is skew-symmetric (M ≈ -M^T).
        /// </summary>
        public bool isSkewSymmetric => NearlyEqual(this, -transpose);

        /// <summary>
        ///     Gets whether this matrix is singular: the determinant is within rounding error of zero relative to the
        ///     product of the row lengths, which bounds it by Hadamard's inequality.
        /// </summary>
        public bool isSingular
        {
            get
            {
                V2 r0 = GetRow(0);
                V2 r1 = GetRow(1);

                double s0 = r0.max_magnitude;
                double s1 = r1.max_magnitude;

                if (s0 == 0 || s1 == 0)
                    return true;

                // scale each row to a largest element of one so the determinant can't overflow or underflow, the rows
                // go in as columns, which doesn't change the determinant
                r0 /= s0;
                r1 /= s1;

                return Abs(new M2(r0, r1).determinant) <= 16 * EPS * r0.magnitude * r1.magnitude;
            }
        }

        /// <summary>
        ///     Gets the maximum absolute element value in the matrix.
        /// </summary>
        public double max_magnitude => Max(Max(Abs(m00), Abs(m10)), Max(Abs(m01), Abs(m11)));

        /// <summary>
        ///     Gets the minimum absolute element value in the matrix.
        /// </summary>
        public double min_magnitude => Min(Min(Abs(m00), Abs(m10)), Min(Abs(m01), Abs(m11)));

        #endregion

        #region Matrix Norms

        /// <summary>
        ///     Gets the Frobenius norm (square root of sum of squared elements).
        /// </summary>
        public double frobeniusNorm => Sqrt(
            m00 * m00 + m01 * m01 +
            m10 * m10 + m11 * m11
        );

        /// <summary>
        ///     Gets the infinity norm (maximum absolute row sum).
        /// </summary>
        public double infinityNorm => Max(
            Abs(m00) + Abs(m01),
            Abs(m10) + Abs(m11)
        );

        /// <summary>
        ///     Computes the Frobenius norm of a matrix.
        /// </summary>
        /// <param name="m">The matrix.</param>
        /// <returns>The Frobenius norm.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double FrobeniusNorm(in M2 m) => m.frobeniusNorm;

        #endregion

        #region Matrix Operations (Static Methods)

        /// <summary>
        ///     Computes the trace of a matrix (sum of diagonal elements).
        /// </summary>
        /// <param name="m">The matrix.</param>
        /// <returns>The trace value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Trace(in M2 m) => m.trace;

        /// <summary>
        ///     Computes the determinant of a matrix.
        /// </summary>
        /// <param name="m">The matrix.</param>
        /// <returns>The determinant value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Determinant(in M2 m) => m.determinant;

        /// <summary>
        ///     Computes the transpose of a matrix (rows and columns swapped).
        /// </summary>
        /// <param name="m">The matrix.</param>
        /// <returns>The transposed matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M2 Transpose(in M2 m) => m.transpose;

        /// <summary>
        ///     Computes the inverse of a matrix.
        /// </summary>
        /// <param name="m">The matrix to invert.</param>
        /// <returns>The inverse matrix.</returns>
        /// <remarks>
        ///     Uses the classical adjoint method. Does not check for singularity.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M2 Inverse(in M2 m)
        {
            double invDet = 1.0 / (m.m00 * m.m11 - m.m01 * m.m10);
            return new M2(
                m.m11 * invDet, -m.m01 * invDet,
                -m.m10 * invDet, m.m00 * invDet
            );
        }

        /// <summary>
        ///     Linearly interpolates between two matrices element-wise.
        /// </summary>
        /// <param name="a">Starting matrix (t=0).</param>
        /// <param name="b">Ending matrix (t=1).</param>
        /// <param name="t">Interpolation parameter, clamped to [0, 1].</param>
        /// <returns>The interpolated matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M2 Lerp(in M2 a, in M2 b, double t)
        {
            t = Clamp01(t);
            return a + t * (b - a);
        }

        #endregion

        #region Matrix Transformation Methods

        /// <summary>
        ///     Transforms a vector by this matrix (equivalent to M * v).
        /// </summary>
        /// <param name="vector">The vector to transform.</param>
        /// <returns>The transformed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public V2 MultiplyVector(in V2 vector) => this * vector;

        #endregion

        #region Orthonormalization

        /// <summary>
        ///     Gets an orthonormalized copy of this matrix using the Gram-Schmidt process on the columns.
        /// </summary>
        /// <remarks>
        ///     The resulting columns form an orthonormal basis (det = 1 or -1), with the first column parallel to the
        ///     original first column.  With only two columns the classical and modified Gram-Schmidt processes are
        ///     identical.
        /// </remarks>
        public M2 orthonormalized
        {
            get
            {
                V2 x = GetColumn(0);
                V2 y = GetColumn(1);

                x = x.normalized;
                y = (y - x * V2.Dot(x, y)).normalized;

                return new M2(x, y);
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
        public static M2 Diagonal(double d) => new M2(d, 0, 0, d);

        /// <summary>
        ///     Creates a diagonal matrix from a vector.
        /// </summary>
        /// <param name="v">Vector containing diagonal values.</param>
        /// <returns>A diagonal matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M2 Diagonal(in V2 v) => new M2(v.x, 0, 0, v.y);

        /// <summary>
        ///     Creates a diagonal matrix from two scalar values.
        /// </summary>
        /// <param name="x">First diagonal element (m00).</param>
        /// <param name="y">Second diagonal element (m11).</param>
        /// <returns>A diagonal matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M2 Diagonal(double x, double y) => new M2(x, 0, 0, y);

        /// <summary>
        ///     Creates the skew-symmetric (cross-product) matrix of a scalar rate about the out-of-plane axis.
        ///     For a vector v: Skew(omega) * v = omega * (-v.y, v.x), which is (omega z) × v in 3D.
        /// </summary>
        /// <param name="omega">The scalar rate (positive counter-clockwise).</param>
        /// <returns>A skew-symmetric matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M2 Skew(double omega) => new M2(0, -omega, omega, 0);

        #endregion

        #region Rotation Matrix Construction

        /// <summary>
        ///     Creates a counter-clockwise rotation matrix.
        /// </summary>
        /// <param name="angle">Rotation angle in radians.</param>
        /// <returns>The rotation matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M2 Rotate(double angle)
        {
            double c = Cos(angle);
            double s = Sin(angle);

            return new M2(c, -s, s, c);
        }

        #endregion

        #region Rotation Angle Extraction

        /// <summary>
        ///     Gets the counter-clockwise rotation angle of this rotation matrix in [-π, π].
        /// </summary>
        /// <remarks>
        ///     Assumes the matrix is a rotation; only the first column is used.
        /// </remarks>
        public double angle => Atan2(m10, m00);

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
        public static M2 CopyFrom(double[,] array, int x, int y) =>
            new M2(
                array[x, y], array[x, y + 1],
                array[x + 1, y], array[x + 1, y + 1]
            );

        /// <summary>
        ///     Creates a matrix from a 1D array in row-major order.
        /// </summary>
        /// <param name="array">Source 1D array.</param>
        /// <param name="offset">Starting index in the source array.</param>
        /// <returns>A new matrix containing the values from the array.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M2 CopyFrom(IList<double> array, int offset) =>
            new M2(
                array[offset], array[offset + 1],
                array[offset + 2], array[offset + 3]
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
            other[x, y] = m00;
            other[x, y + 1] = m01;
            other[x + 1, y] = m10;
            other[x + 1, y + 1] = m11;
        }

        /// <summary>
        ///     Copies this matrix to a 1D array in row-major order.
        /// </summary>
        /// <param name="other">Target 1D array.</param>
        /// <param name="offset">Starting index in the target array.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(IList<double> other, int offset)
        {
            other[offset] = m00;
            other[offset + 1] = m01;
            other[offset + 2] = m10;
            other[offset + 3] = m11;
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
        public bool Equals(M2 other) =>
            m00.Equals(other.m00) && m01.Equals(other.m01) &&
            m10.Equals(other.m10) && m11.Equals(other.m11);

        /// <summary>
        ///     Tests for equality with an object.
        /// </summary>
        /// <param name="other">Object to compare with.</param>
        /// <returns>True if the object is an M2 with equal elements.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object? other) => other is M2 m && Equals(m);

        /// <summary>
        ///     Computes a hash code for the matrix.
        /// </summary>
        /// <returns>A hash code combining all elements.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = m00.GetHashCode();
                hash = (hash * 397) ^ m01.GetHashCode();
                hash = (hash * 397) ^ m10.GetHashCode();
                hash = (hash * 397) ^ m11.GetHashCode();
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
            return string.Format("[{0}, {1}\n{2}, {3}]\n",
                m00.ToString(format, formatProvider), m01.ToString(format, formatProvider),
                m10.ToString(format, formatProvider), m11.ToString(format, formatProvider));
        }

        #endregion
    }
}
