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
    ///     A 3x3 double-precision matrix for use in aerospace applications.
    ///     Uses a right-handed coordinate system with column-major internal storage.
    ///     Integrates with V3 (3D vectors) and Q3 (quaternions) for rotation operations.
    /// </summary>
    /// <remarks>
    ///     Matrix layout (row, column):
    ///     <code>
    ///         m00 m01 m02
    ///         m10 m11 m12
    ///         m20 m21 m22
    ///     </code>
    ///     Internal storage is column-major for compatibility with graphics APIs.
    ///     This is a readonly struct - all operations return new instances.
    /// </remarks>
    public readonly struct M3 : IEquatable<M3>, IFormattable
    {
        #region Fields

        // Column 0 elements
        /// <summary>Element at row 0, column 0.</summary>
        public readonly double m00;

        /// <summary>Element at row 1, column 0.</summary>
        public readonly double m10;

        /// <summary>Element at row 2, column 0.</summary>
        public readonly double m20;

        // Column 1 elements
        /// <summary>Element at row 0, column 1.</summary>
        public readonly double m01;

        /// <summary>Element at row 1, column 1.</summary>
        public readonly double m11;

        /// <summary>Element at row 2, column 1.</summary>
        public readonly double m21;

        // Column 2 elements
        /// <summary>Element at row 0, column 2.</summary>
        public readonly double m02;

        /// <summary>Element at row 1, column 2.</summary>
        public readonly double m12;

        /// <summary>Element at row 2, column 2.</summary>
        public readonly double m22;

        #endregion

        #region Constructors

        /// <summary>
        ///     Constructs a matrix from 9 scalar values in row-major order.
        /// </summary>
        /// <param name="m00">Element at row 0, column 0.</param>
        /// <param name="m01">Element at row 0, column 1.</param>
        /// <param name="m02">Element at row 0, column 2.</param>
        /// <param name="m10">Element at row 1, column 0.</param>
        /// <param name="m11">Element at row 1, column 1.</param>
        /// <param name="m12">Element at row 1, column 2.</param>
        /// <param name="m20">Element at row 2, column 0.</param>
        /// <param name="m21">Element at row 2, column 1.</param>
        /// <param name="m22">Element at row 2, column 2.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public M3(double m00, double m01, double m02, double m10, double m11, double m12, double m20, double m21, double m22)
        {
            this.m00 = m00;
            this.m10 = m10;
            this.m20 = m20;
            this.m01 = m01;
            this.m11 = m11;
            this.m21 = m21;
            this.m02 = m02;
            this.m12 = m12;
            this.m22 = m22;
        }

        /// <summary>
        ///     Constructs a matrix from three column vectors.
        /// </summary>
        /// <param name="column0">First column vector.</param>
        /// <param name="column1">Second column vector.</param>
        /// <param name="column2">Third column vector.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public M3(in V3 column0, in V3 column1, in V3 column2)
        {
            m00 = column0.x;
            m01 = column1.x;
            m02 = column2.x;
            m10 = column0.y;
            m11 = column1.y;
            m12 = column2.y;
            m20 = column0.z;
            m21 = column1.z;
            m22 = column2.z;
        }

        #endregion

        #region Static Constants

        /// <summary>
        ///     The zero matrix (all elements are 0).
        /// </summary>
        public static M3 zero { get; } = new M3(0, 0, 0, 0, 0, 0, 0, 0, 0);

        /// <summary>
        ///     The identity matrix (diagonal elements are 1, all others are 0).
        /// </summary>
        public static M3 identity { get; } = new M3(1, 0, 0, 0, 1, 0, 0, 0, 1);

        #endregion

        #region Indexers

        /// <summary>
        ///     Accesses matrix elements by row and column indices (read-only).
        /// </summary>
        /// <param name="row">Row index [0..2].</param>
        /// <param name="column">Column index [0..2].</param>
        /// <returns>The element at the specified position.</returns>
        public double this[int row, int column]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this[row + column * 3];
        }

        /// <summary>
        ///     Accesses matrix elements by linear index in column-major order (read-only).
        /// </summary>
        /// <param name="index">Linear index [0..8] in column-major order.</param>
        /// <returns>The element at the specified index.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown when index is outside [0..8].</exception>
        public double this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                switch (index)
                {
                    case 0: return m00;
                    case 1: return m10;
                    case 2: return m20;
                    case 3: return m01;
                    case 4: return m11;
                    case 5: return m21;
                    case 6: return m02;
                    case 7: return m12;
                    case 8: return m22;
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
        /// <param name="index">Column index [0..2].</param>
        /// <returns>The column as a V3 vector.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown when index is outside [0..2].</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public V3 GetColumn(int index)
        {
            switch (index)
            {
                case 0: return new V3(m00, m10, m20);
                case 1: return new V3(m01, m11, m21);
                case 2: return new V3(m02, m12, m22);
                default:
                    throw new IndexOutOfRangeException("Invalid column index!");
            }
        }

        /// <summary>
        ///     Gets a row of the matrix as a vector.
        /// </summary>
        /// <param name="index">Row index [0..2].</param>
        /// <returns>The row as a V3 vector.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown when index is outside [0..2].</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public V3 GetRow(int index)
        {
            switch (index)
            {
                case 0: return new V3(m00, m01, m02);
                case 1: return new V3(m10, m11, m12);
                case 2: return new V3(m20, m21, m22);
                default:
                    throw new IndexOutOfRangeException("Invalid row index!");
            }
        }

        /// <summary>
        ///     Returns a new matrix with the specified column replaced.
        /// </summary>
        /// <param name="index">Column index [0..2].</param>
        /// <param name="column">Vector containing the new column values.</param>
        /// <returns>A new matrix with the column replaced.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown when index is outside [0..2].</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public M3 WithColumn(int index, in V3 column)
        {
            switch (index)
            {
                case 0: return new M3(column.x, m01, m02, column.y, m11, m12, column.z, m21, m22);
                case 1: return new M3(m00, column.x, m02, m10, column.y, m12, m20, column.z, m22);
                case 2: return new M3(m00, m01, column.x, m10, m11, column.y, m20, m21, column.z);
                default:
                    throw new IndexOutOfRangeException("Invalid column index!");
            }
        }

        /// <summary>
        ///     Returns a new matrix with the specified row replaced.
        /// </summary>
        /// <param name="index">Row index [0..2].</param>
        /// <param name="row">Vector containing the new row values.</param>
        /// <returns>A new matrix with the row replaced.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown when index is outside [0..2].</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public M3 WithRow(int index, in V3 row)
        {
            switch (index)
            {
                case 0: return new M3(row.x, row.y, row.z, m10, m11, m12, m20, m21, m22);
                case 1: return new M3(m00, m01, m02, row.x, row.y, row.z, m20, m21, m22);
                case 2: return new M3(m00, m01, m02, m10, m11, m12, row.x, row.y, row.z);
                default:
                    throw new IndexOutOfRangeException("Invalid row index!");
            }
        }

        /// <summary>
        ///     Returns a new matrix with two rows swapped.
        /// </summary>
        /// <param name="i">First row index [0..2].</param>
        /// <param name="j">Second row index [0..2].</param>
        /// <returns>A new matrix with the rows swapped.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public M3 WithSwappedRows(int i, int j)
        {
            V3 rowI = GetRow(i);
            V3 rowJ = GetRow(j);
            return WithRow(i, rowJ).WithRow(j, rowI);
        }

        /// <summary>
        ///     Returns a new matrix with two columns swapped.
        /// </summary>
        /// <param name="i">First column index [0..2].</param>
        /// <param name="j">Second column index [0..2].</param>
        /// <returns>A new matrix with the columns swapped.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public M3 WithSwappedColumns(int i, int j)
        {
            V3 colI = GetColumn(i);
            V3 colJ = GetColumn(j);
            return WithColumn(i, colJ).WithColumn(j, colI);
        }

        #endregion

        #region Diagonal Access

        /// <summary>
        ///     Gets the diagonal elements as a vector.
        /// </summary>
        public V3 diagonal => new V3(m00, m11, m22);

        /// <summary>
        ///     Returns a new matrix with the diagonal elements replaced.
        /// </summary>
        /// <param name="v">Vector containing the diagonal values.</param>
        /// <returns>A new matrix with the diagonal replaced.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public M3 WithDiagonal(in V3 v) => new M3(v.x, m01, m02, m10, v.y, m12, m20, m21, v.z);

        /// <summary>
        ///     Returns a new matrix with the diagonal elements replaced.
        /// </summary>
        /// <param name="x">First diagonal element (m00).</param>
        /// <param name="y">Second diagonal element (m11).</param>
        /// <param name="z">Third diagonal element (m22).</param>
        /// <returns>A new matrix with the diagonal replaced.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public M3 WithDiagonal(double x, double y, double z) => new M3(x, m01, m02, m10, y, m12, m20, m21, z);

        #endregion

        #region Arithmetic Operators

        /// <summary>
        ///     Multiplies two matrices together.
        /// </summary>
        /// <param name="lhs">Left-hand side matrix.</param>
        /// <param name="rhs">Right-hand side matrix.</param>
        /// <returns>The product matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M3 operator *(in M3 lhs, in M3 rhs) =>
            new M3(
                lhs.m00 * rhs.m00 + lhs.m01 * rhs.m10 + lhs.m02 * rhs.m20,
                lhs.m00 * rhs.m01 + lhs.m01 * rhs.m11 + lhs.m02 * rhs.m21,
                lhs.m00 * rhs.m02 + lhs.m01 * rhs.m12 + lhs.m02 * rhs.m22,
                lhs.m10 * rhs.m00 + lhs.m11 * rhs.m10 + lhs.m12 * rhs.m20,
                lhs.m10 * rhs.m01 + lhs.m11 * rhs.m11 + lhs.m12 * rhs.m21,
                lhs.m10 * rhs.m02 + lhs.m11 * rhs.m12 + lhs.m12 * rhs.m22,
                lhs.m20 * rhs.m00 + lhs.m21 * rhs.m10 + lhs.m22 * rhs.m20,
                lhs.m20 * rhs.m01 + lhs.m21 * rhs.m11 + lhs.m22 * rhs.m21,
                lhs.m20 * rhs.m02 + lhs.m21 * rhs.m12 + lhs.m22 * rhs.m22
            );

        /// <summary>
        ///     Transforms a vector by a matrix (M * v).
        /// </summary>
        /// <param name="lhs">The matrix.</param>
        /// <param name="vector">The vector to transform.</param>
        /// <returns>The transformed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static V3 operator *(in M3 lhs, in V3 vector) =>
            new V3(
                lhs.m00 * vector.x + lhs.m01 * vector.y + lhs.m02 * vector.z,
                lhs.m10 * vector.x + lhs.m11 * vector.y + lhs.m12 * vector.z,
                lhs.m20 * vector.x + lhs.m21 * vector.y + lhs.m22 * vector.z
            );

        /// <summary>
        ///     Multiplies all matrix elements by a scalar.
        /// </summary>
        /// <param name="lhs">The matrix.</param>
        /// <param name="value">The scalar value.</param>
        /// <returns>The scaled matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M3 operator *(in M3 lhs, double value) =>
            new M3(
                lhs.m00 * value, lhs.m01 * value, lhs.m02 * value,
                lhs.m10 * value, lhs.m11 * value, lhs.m12 * value,
                lhs.m20 * value, lhs.m21 * value, lhs.m22 * value
            );

        /// <summary>
        ///     Multiplies all matrix elements by a scalar.
        /// </summary>
        /// <param name="value">The scalar value.</param>
        /// <param name="rhs">The matrix.</param>
        /// <returns>The scaled matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M3 operator *(double value, in M3 rhs) => rhs * value;

        /// <summary>
        ///     Divides all matrix elements by a scalar.
        /// </summary>
        /// <param name="lhs">The matrix.</param>
        /// <param name="value">The scalar divisor.</param>
        /// <returns>The scaled matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M3 operator /(in M3 lhs, double value) =>
            new M3(
                lhs.m00 / value, lhs.m01 / value, lhs.m02 / value,
                lhs.m10 / value, lhs.m11 / value, lhs.m12 / value,
                lhs.m20 / value, lhs.m21 / value, lhs.m22 / value
            );

        /// <summary>
        ///     Adds two matrices element-wise.
        /// </summary>
        /// <param name="lhs">First matrix.</param>
        /// <param name="rhs">Second matrix.</param>
        /// <returns>The sum of the matrices.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M3 operator +(in M3 lhs, in M3 rhs) =>
            new M3(
                lhs.m00 + rhs.m00, lhs.m01 + rhs.m01, lhs.m02 + rhs.m02,
                lhs.m10 + rhs.m10, lhs.m11 + rhs.m11, lhs.m12 + rhs.m12,
                lhs.m20 + rhs.m20, lhs.m21 + rhs.m21, lhs.m22 + rhs.m22
            );

        /// <summary>
        ///     Subtracts two matrices element-wise.
        /// </summary>
        /// <param name="lhs">First matrix.</param>
        /// <param name="rhs">Second matrix to subtract.</param>
        /// <returns>The difference of the matrices.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M3 operator -(in M3 lhs, in M3 rhs) =>
            new M3(
                lhs.m00 - rhs.m00, lhs.m01 - rhs.m01, lhs.m02 - rhs.m02,
                lhs.m10 - rhs.m10, lhs.m11 - rhs.m11, lhs.m12 - rhs.m12,
                lhs.m20 - rhs.m20, lhs.m21 - rhs.m21, lhs.m22 - rhs.m22
            );

        /// <summary>
        ///     Negates all matrix elements.
        /// </summary>
        /// <param name="m">The matrix to negate.</param>
        /// <returns>The negated matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M3 operator -(in M3 m) =>
            new M3(
                -m.m00, -m.m01, -m.m02,
                -m.m10, -m.m11, -m.m12,
                -m.m20, -m.m21, -m.m22
            );

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
        public static bool operator ==(in M3 lhs, in M3 rhs) =>
            lhs.m00 == rhs.m00 && lhs.m01 == rhs.m01 && lhs.m02 == rhs.m02 &&
            lhs.m10 == rhs.m10 && lhs.m11 == rhs.m11 && lhs.m12 == rhs.m12 &&
            lhs.m20 == rhs.m20 && lhs.m21 == rhs.m21 && lhs.m22 == rhs.m22;

        /// <summary>
        ///     Tests for strict element-wise inequality between two matrices.
        ///     Returns true if any element is NaN.
        /// </summary>
        /// <param name="lhs">First matrix.</param>
        /// <param name="rhs">Second matrix.</param>
        /// <returns>True if any element differs.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in M3 lhs, in M3 rhs) => !(lhs == rhs);

        #endregion

        #region Matrix Properties

        /// <summary>
        ///     Gets the trace of the matrix (sum of diagonal elements).
        /// </summary>
        public double trace => m00 + m11 + m22;

        /// <summary>
        ///     Gets the determinant of the matrix.
        /// </summary>
        public double determinant => m00 * (m11 * m22 - m21 * m12) - m10 * (m01 * m22 - m21 * m02) + m20 * (m01 * m12 - m11 * m02);

        /// <summary>
        ///     Gets the transpose of the matrix (rows and columns swapped).
        /// </summary>
        public M3 transpose => new M3(m00, m10, m20, m01, m11, m21, m02, m12, m22);

        /// <summary>
        ///     Gets the transpose of the matrix (rows and columns swapped).
        /// </summary>
        public M3 T() => new M3(m00, m10, m20, m01, m11, m21, m02, m12, m22);

        /// <summary>
        ///     Gets the inverse of the matrix.
        /// </summary>
        public M3 inverse => Inverse(this);

        /// <summary>
        ///     Gets whether this matrix is the identity matrix (strict comparison).
        /// </summary>
        public bool isIdentity =>
            m00 == 1.0 && m10 == 0.0 && m20 == 0.0 &&
            m01 == 0.0 && m11 == 1.0 && m21 == 0.0 &&
            m02 == 0.0 && m12 == 0.0 && m22 == 1.0;

        /// <summary>
        ///     Gets whether this matrix is orthogonal (M * M^T ≈ I).
        /// </summary>
        public bool isOrthogonal => NearlyEqual(this * transpose, identity, 1e-15);

        /// <summary>
        ///     Gets whether this matrix is symmetric (M ≈ M^T).
        /// </summary>
        public bool isSymmetric => NearlyEqual(this, transpose);

        /// <summary>
        ///     Gets whether this matrix is skew-symmetric (M ≈ -M^T).
        /// </summary>
        public bool isSkewSymmetric => NearlyEqual(this, -transpose);

        /// <summary>
        ///     Gets whether this matrix is singular (determinant ≈ 0).
        /// </summary>
        public bool isSingular => Abs(determinant) < EPS;

        /// <summary>
        ///     Gets the maximum element value in the matrix.
        /// </summary>
        public double max_magnitude =>
            Max(Max(Max(Max(Max(Max(Max(Max(m00, m10), m20), m01), m11), m21), m02), m12), m22);

        /// <summary>
        ///     Gets the minimum element value in the matrix.
        /// </summary>
        public double min_magnitude =>
            Min(Min(Min(Min(Min(Min(Min(Min(m00, m10), m20), m01), m11), m21), m02), m12), m22);

        #endregion

        #region Matrix Norms

        /// <summary>
        ///     Gets the Frobenius norm (square root of sum of squared elements).
        /// </summary>
        public double frobeniusNorm => Sqrt(
            m00 * m00 + m01 * m01 + m02 * m02 +
            m10 * m10 + m11 * m11 + m12 * m12 +
            m20 * m20 + m21 * m21 + m22 * m22
        );

        /// <summary>
        ///     Gets the infinity norm (maximum absolute row sum).
        /// </summary>
        public double infinityNorm => Max(
            Max(
                Abs(m00) + Abs(m01) + Abs(m02),
                Abs(m10) + Abs(m11) + Abs(m12)
            ),
            Abs(m20) + Abs(m21) + Abs(m22)
        );

        /// <summary>
        ///     Computes the Frobenius norm of a matrix.
        /// </summary>
        /// <param name="m">The matrix.</param>
        /// <returns>The Frobenius norm.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double FrobeniusNorm(in M3 m) => m.frobeniusNorm;

        #endregion

        #region Matrix Operations (Static Methods)

        /// <summary>
        ///     Computes the trace of a matrix (sum of diagonal elements).
        /// </summary>
        /// <param name="m">The matrix.</param>
        /// <returns>The trace value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Trace(in M3 m) => m.trace;

        /// <summary>
        ///     Computes the determinant of a matrix.
        /// </summary>
        /// <param name="m">The matrix.</param>
        /// <returns>The determinant value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Determinant(in M3 m) => m.determinant;

        /// <summary>
        ///     Computes the transpose of a matrix (rows and columns swapped).
        /// </summary>
        /// <param name="m">The matrix.</param>
        /// <returns>The transposed matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M3 Transpose(in M3 m) => m.transpose;

        /// <summary>
        ///     Computes the inverse of a matrix.
        /// </summary>
        /// <param name="m">The matrix to invert.</param>
        /// <returns>The inverse matrix.</returns>
        /// <remarks>
        ///     Uses the classical adjoint method. Does not check for singularity.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M3 Inverse(in M3 m)
        {
            double a      = m.m11 * m.m22 - m.m21 * m.m12;
            double b      = m.m21 * m.m02 - m.m01 * m.m22;
            double c      = m.m01 * m.m12 - m.m11 * m.m02;
            double d      = m.m20 * m.m12 - m.m10 * m.m22;
            double e      = m.m00 * m.m22 - m.m20 * m.m02;
            double f      = m.m10 * m.m02 - m.m00 * m.m12;
            double g      = m.m10 * m.m21 - m.m20 * m.m11;
            double h      = m.m20 * m.m01 - m.m00 * m.m21;
            double i      = m.m11 * m.m00 - m.m10 * m.m01;
            double invDet = 1.0 / (m.m00 * a + m.m10 * b + m.m20 * c);
            return new M3(
                a * invDet, b * invDet, c * invDet,
                d * invDet, e * invDet, f * invDet,
                g * invDet, h * invDet, i * invDet
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
        public static M3 Lerp(in M3 a, in M3 b, double t)
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
        public V3 MultiplyVector(in V3 vector) => this * vector;

        #endregion

        #region Orthonormalization

        /// <summary>
        ///     Gets an orthonormalized copy of this matrix using Gram-Schmidt process.
        /// </summary>
        /// <remarks>
        ///     The resulting columns form an orthonormal basis
        ///     and the matrix represents a pure rotation (det = 1 or -1).
        /// </remarks>
        public M3 orthonormalized
        {
            get
            {
                V3 x = GetColumn(0);
                V3 y = GetColumn(1);
                V3 z = GetColumn(2);

                x = x.normalized;
                y = (y - x * V3.Dot(x, y)).normalized;
                z = (z - x * V3.Dot(x, z) - y * V3.Dot(y, z)).normalized;

                return new M3(x, y, z);
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
        public static M3 Diagonal(double d) => new M3(d, 0, 0, 0, d, 0, 0, 0, d);

        /// <summary>
        ///     Creates a diagonal matrix from a vector.
        /// </summary>
        /// <param name="v">Vector containing diagonal values.</param>
        /// <returns>A diagonal matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M3 Diagonal(in V3 v) => new M3(v.x, 0, 0, 0, v.y, 0, 0, 0, v.z);

        /// <summary>
        ///     Creates a diagonal matrix from three scalar values.
        /// </summary>
        /// <param name="x">First diagonal element (m00).</param>
        /// <param name="y">Second diagonal element (m11).</param>
        /// <param name="z">Third diagonal element (m22).</param>
        /// <returns>A diagonal matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M3 Diagonal(double x, double y, double z) => new M3(x, 0, 0, 0, y, 0, 0, 0, z);

        /// <summary>
        ///     Creates a skew-symmetric (cross-product) matrix from a vector.
        ///     For vectors a and b: Skew(a) * b = Cross(a, b).
        /// </summary>
        /// <param name="v">The vector.</param>
        /// <returns>A skew-symmetric matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M3 Skew(in V3 v) => new M3(0, -v.z, v.y, v.z, 0, -v.x, -v.y, v.x, 0);

        #endregion

        #region Rotation Matrix Construction

        /// <summary>
        ///     Creates a rotation matrix from a unit quaternion.
        /// </summary>
        /// <param name="q">A unit quaternion representing the rotation.</param>
        /// <returns>The equivalent rotation matrix.</returns>
        /// <remarks>
        ///     The quaternion must be normalized for correct results.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M3 Rotate(in Q3 q)
        {
            double x  = q.x * 2.0;
            double y  = q.y * 2.0;
            double z  = q.z * 2.0;
            double xx = q.x * x;
            double yy = q.y * y;
            double zz = q.z * z;
            double xy = q.x * y;
            double xz = q.x * z;
            double yz = q.y * z;
            double wx = q.w * x;
            double wy = q.w * y;
            double wz = q.w * z;

            return new M3(
                1.0 - (yy + zz), xy - wz, xz + wy,
                xy + wz, 1.0 - (xx + zz), yz - wx,
                xz - wy, yz + wx, 1.0 - (xx + yy)
            );
        }

        /// <summary>
        ///     Creates a rotation matrix from a unit quaternion.
        ///     Alias for Rotate() for API discoverability.
        /// </summary>
        /// <param name="q">A unit quaternion representing the rotation.</param>
        /// <returns>The equivalent rotation matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M3 FromQuaternion(in Q3 q) => Rotate(q);

        /// <summary>
        ///     Creates a rotation matrix from an angle and axis using Rodrigues' rotation formula.
        /// </summary>
        /// <param name="angle">Rotation angle in radians.</param>
        /// <param name="axis">Rotation axis (will be normalized internally).</param>
        /// <returns>The rotation matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M3 AngleAxis(double angle, in V3 axis)
        {
            V3     a = axis.normalized;
            double c = Cos(angle);
            double s = Sin(angle);
            double t = 1.0 - c;

            return new M3(
                t * a.x * a.x + c, t * a.x * a.y - s * a.z, t * a.x * a.z + s * a.y,
                t * a.x * a.y + s * a.z, t * a.y * a.y + c, t * a.y * a.z - s * a.x,
                t * a.x * a.z - s * a.y, t * a.y * a.z + s * a.x, t * a.z * a.z + c
            );
        }

        /// <summary>
        ///     Creates a rotation matrix from Euler angles using intrinsic ZYX order (yaw, pitch, roll).
        ///     This matches the aerospace convention for NED coordinates.
        /// </summary>
        /// <param name="roll">Rotation about the forward axis (x) in radians.</param>
        /// <param name="pitch">Rotation about the right axis (y) in radians.</param>
        /// <param name="yaw">Rotation about the down axis (z) in radians.</param>
        /// <returns>The rotation matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M3 EulerAngles(double roll, double pitch, double yaw)
        {
            double cr = Cos(roll);
            double sr = Sin(roll);
            double cp = Cos(pitch);
            double sp = Sin(pitch);
            double cy = Cos(yaw);
            double sy = Sin(yaw);

            return new M3(
                cy * cp, cy * sp * sr - sy * cr, cy * sp * cr + sy * sr,
                sy * cp, sy * sp * sr + cy * cr, sy * sp * cr - cy * sr,
                -sp, cp * sr, cp * cr
            );
        }

        /// <summary>
        ///     Creates a rotation matrix from Euler angles using intrinsic ZYX order (yaw, pitch, roll).
        ///     This matches the aerospace convention for NED coordinates.
        /// </summary>
        /// <param name="angles">Vector containing (roll, pitch, yaw) in radians.</param>
        /// <returns>The rotation matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M3 EulerAngles(in V3 angles) => EulerAngles(angles.x, angles.y, angles.z);

        #endregion

        #region Quaternion Conversion

        /// <summary>
        ///     Converts this rotation matrix to a quaternion.
        ///     Does not ensure that the matrix is orthonormal with det = 1.
        /// </summary>
        /// <remarks>
        ///     For non-orthonormal matrices, use rotation_quaternion instead.
        /// </remarks>
        public Q3 quaternion
        {
            get
            {
                if (trace > 0.0)
                {
                    double s = Sqrt(trace + 1.0);
                    double w = s * 0.5;
                    s = 0.5 / s;

                    return new Q3(
                        (m21 - m12) * s,
                        (m02 - m20) * s,
                        (m10 - m01) * s,
                        w
                    );
                }

                int i = m00 < m11
                    ? m11 < m22 ? 2 : 1
                    : m00 < m22
                        ? 2
                        : 0;
                int j = (i + 1) % 3;
                int k = (i + 2) % 3;

                double s2 = Sqrt(this[i, i] - this[j, j] - this[k, k] + 1.0);

                double qi = s2 * 0.5;
                s2 = 0.5 / s2;

                double qw = (this[k, j] - this[j, k]) * s2;
                double qj = (this[j, i] + this[i, j]) * s2;
                double qk = (this[k, i] + this[i, k]) * s2;

                // Reorder based on which diagonal was largest
                switch (i)
                {
                    case 0:  return new Q3(qi, qj, qk, qw);
                    case 1:  return new Q3(qk, qi, qj, qw);
                    default: return new Q3(qj, qk, qi, qw);
                }
            }
        }

        /// <summary>
        ///     Converts this matrix to a rotation quaternion, ensuring proper rotation matrix form.
        ///     Orthonormalizes the matrix first and ensures det = 1.
        /// </summary>
        /// <remarks>
        ///     Use this for matrices that may have accumulated numerical error
        ///     or may include scaling.
        /// </remarks>
        public Q3 rotation_quaternion
        {
            get
            {
                M3 m = orthonormalized;
                if (m.determinant < 0)
                    m = -m;

                return m.quaternion;
            }
        }

        #endregion

        #region Euler Angle Extraction

        /// <summary>
        ///     Gets the Euler angles (roll, pitch, yaw) from this rotation matrix.
        ///     Assumes intrinsic ZYX order matching aerospace NED convention.
        /// </summary>
        public V3 eulerAngles
        {
            get
            {
                double pitch = SafeAsin(-m20);

                double roll, yaw;

                if (Abs(m20) < 1.0 - EPS)
                {
                    roll = Atan2(m21, m22);
                    yaw  = Atan2(m10, m00);
                }
                else
                {
                    // Gimbal lock: pitch is ±90°
                    roll = Atan2(-m12, m11);
                    yaw  = 0;
                }

                return new V3(roll, pitch, yaw);
            }
        }

        /// <summary>
        ///     Extracts Euler angles (roll, pitch, yaw) from this rotation matrix.
        ///     Assumes intrinsic ZYX order matching aerospace NED convention.
        /// </summary>
        /// <returns>Vector containing (roll, pitch, yaw) in radians.</returns>
        /// <remarks>
        ///     Handles gimbal lock when pitch is ±90°.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public V3 ToEulerAngles() => eulerAngles;

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
        public static M3 CopyFrom(double[,] array, int x, int y) =>
            new M3(
                array[x, y], array[x, y + 1], array[x, y + 2],
                array[x + 1, y], array[x + 1, y + 1], array[x + 1, y + 2],
                array[x + 2, y], array[x + 2, y + 1], array[x + 2, y + 2]
            );

        /// <summary>
        ///     Creates a matrix from a 1D array.
        /// </summary>
        /// <param name="array">Source 1D array.</param>
        /// <param name="offset">Starting index in the source array.</param>
        /// <returns>A new matrix containing the values from the array.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static M3 CopyFrom(IList<double> array, int offset) =>
            new M3(
                array[offset], array[offset + 1], array[offset + 2],
                array[offset + 3], array[offset + 4], array[offset + 5],
                array[offset + 6], array[offset + 7], array[offset + 8]
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
            other[x, y]         = m00;
            other[x, y + 1]     = m01;
            other[x, y + 2]     = m02;
            other[x + 1, y]     = m10;
            other[x + 1, y + 1] = m11;
            other[x + 1, y + 2] = m12;
            other[x + 2, y]     = m20;
            other[x + 2, y + 1] = m21;
            other[x + 2, y + 2] = m22;
        }

        /// <summary>
        ///     Copies this matrix to a 1D array.
        /// </summary>
        /// <param name="other">Target 1D array.</param>
        /// <param name="offset">Starting index in the target array.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(IList<double> other, int offset)
        {
            other[offset]     = m00;
            other[offset + 1] = m01;
            other[offset + 2] = m02;
            other[offset + 3] = m10;
            other[offset + 4] = m11;
            other[offset + 5] = m12;
            other[offset + 6] = m20;
            other[offset + 7] = m21;
            other[offset + 8] = m22;
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
        public bool Equals(M3 other) =>
            m00.Equals(other.m00) && m01.Equals(other.m01) && m02.Equals(other.m02) &&
            m10.Equals(other.m10) && m11.Equals(other.m11) && m12.Equals(other.m12) &&
            m20.Equals(other.m20) && m21.Equals(other.m21) && m22.Equals(other.m22);

        /// <summary>
        ///     Tests for equality with an object.
        /// </summary>
        /// <param name="other">Object to compare with.</param>
        /// <returns>True if the object is an M3 with equal elements.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object? other) => other is M3 m && Equals(m);

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
                hash = (hash * 397) ^ m02.GetHashCode();
                hash = (hash * 397) ^ m10.GetHashCode();
                hash = (hash * 397) ^ m11.GetHashCode();
                hash = (hash * 397) ^ m12.GetHashCode();
                hash = (hash * 397) ^ m20.GetHashCode();
                hash = (hash * 397) ^ m21.GetHashCode();
                hash = (hash * 397) ^ m22.GetHashCode();
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ToString(string? format, IFormatProvider formatProvider)
        {
            if (string.IsNullOrEmpty(format))
                format = "G";
            return string.Format("[{0}, {1}, {2}\n{3}, {4}, {5}\n{6}, {7}, {8}]\n",
                m00.ToString(format, formatProvider), m01.ToString(format, formatProvider), m02.ToString(format, formatProvider),
                m10.ToString(format, formatProvider), m11.ToString(format, formatProvider), m12.ToString(format, formatProvider),
                m20.ToString(format, formatProvider), m21.ToString(format, formatProvider), m22.ToString(format, formatProvider));
        }

        #endregion
    }
}
