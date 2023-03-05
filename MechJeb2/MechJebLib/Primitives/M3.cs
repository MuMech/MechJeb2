/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

#nullable enable

using System;
using System.Globalization;
using UnityEngine;

namespace MechJebLib.Primitives
{
    public struct M3 : IEquatable<M3>, IFormattable
    {
        // m00 m10 m20
        // m01 m11 m21
        // m02 m12 m22

        // row 0:
        public double m00;
        public double m10;
        public double m20;

        // row 1:
        public double m01;
        public double m11;
        public double m21;

        // row 2:
        public double m02;
        public double m12;
        public double m22;

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

        public M3(V3 column0, V3 column1, V3 column2)
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

        // Access element at [row, column].
        public double this[int row, int column]
        {
            get => this[row + column * 3];

            set => this[row + column * 3] = value;
        }

        // Access element at sequential index (0..8 inclusive).
        public double this[int index]
        {
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

            set
            {
                switch (index)
                {
                    case 0:
                        m00 = value;
                        break;
                    case 1:
                        m10 = value;
                        break;
                    case 2:
                        m20 = value;
                        break;
                    case 3:
                        m01 = value;
                        break;
                    case 4:
                        m11 = value;
                        break;
                    case 5:
                        m21 = value;
                        break;
                    case 6:
                        m02 = value;
                        break;
                    case 7:
                        m12 = value;
                        break;
                    case 8:
                        m22 = value;
                        break;

                    default:
                        throw new IndexOutOfRangeException("Invalid matrix index!");
                }
            }
        }

        // used to allow Matrix4x4s to be used as keys in hash tables
        public override int GetHashCode()
        {
            return GetColumn(0).GetHashCode() ^ (GetColumn(1).GetHashCode() << 2) ^ (GetColumn(2).GetHashCode() >> 2); // FIXME?
        }

        // also required for being able to use M3 as keys in hash tables
        public override bool Equals(object other)
        {
            if (!(other is M3)) return false;

            return Equals((M3)other);
        }

        public bool Equals(M3 other)
        {
            return GetColumn(0).Equals(other.GetColumn(0))
                   && GetColumn(1).Equals(other.GetColumn(1))
                   && GetColumn(2).Equals(other.GetColumn(2));
        }

        // Multiplies two matrices.
        public static M3 operator *(M3 lhs, M3 rhs)
        {
            M3 res;
            res.m00 = lhs.m00 * rhs.m00 + lhs.m01 * rhs.m10 + lhs.m02 * rhs.m20;
            res.m01 = lhs.m00 * rhs.m01 + lhs.m01 * rhs.m11 + lhs.m02 * rhs.m21;
            res.m02 = lhs.m00 * rhs.m02 + lhs.m01 * rhs.m12 + lhs.m02 * rhs.m22;

            res.m10 = lhs.m10 * rhs.m00 + lhs.m11 * rhs.m10 + lhs.m12 * rhs.m20;
            res.m11 = lhs.m10 * rhs.m01 + lhs.m11 * rhs.m11 + lhs.m12 * rhs.m21;
            res.m12 = lhs.m10 * rhs.m02 + lhs.m11 * rhs.m12 + lhs.m12 * rhs.m22;

            res.m20 = lhs.m20 * rhs.m00 + lhs.m21 * rhs.m10 + lhs.m22 * rhs.m20;
            res.m21 = lhs.m20 * rhs.m01 + lhs.m21 * rhs.m11 + lhs.m22 * rhs.m21;
            res.m22 = lhs.m20 * rhs.m02 + lhs.m21 * rhs.m12 + lhs.m22 * rhs.m22;

            return res;
        }

        // Transforms a [[V3]] by a matrix.
        public static V3 operator *(M3 lhs, V3 vector)
        {
            V3 res;
            res.x = lhs.m00 * vector.x + lhs.m01 * vector.y + lhs.m02 * vector.z;
            res.y = lhs.m10 * vector.x + lhs.m11 * vector.y + lhs.m12 * vector.z;
            res.z = lhs.m20 * vector.x + lhs.m21 * vector.y + lhs.m22 * vector.z;
            return res;
        }

        // Multiplies a matrix by a number
        public static M3 operator *(M3 lhs, double value)
        {
            M3 res;
            res.m00 = lhs.m00 * value;
            res.m01 = lhs.m00 * value;
            res.m02 = lhs.m00 * value;

            res.m10 = lhs.m10 * value;
            res.m11 = lhs.m10 * value;
            res.m12 = lhs.m10 * value;

            res.m20 = lhs.m20 * value;
            res.m21 = lhs.m20 * value;
            res.m22 = lhs.m20 * value;

            return res;
        }

        public static M3 operator *(double value, M3 rhs)
        {
            return rhs * value;
        }

        // Divides a matrix by a number
        public static M3 operator /(M3 lhs, double value)
        {
            M3 res;
            res.m00 = lhs.m00 / value;
            res.m01 = lhs.m00 / value;
            res.m02 = lhs.m00 / value;

            res.m10 = lhs.m10 / value;
            res.m11 = lhs.m10 / value;
            res.m12 = lhs.m10 / value;

            res.m20 = lhs.m20 / value;
            res.m21 = lhs.m20 / value;
            res.m22 = lhs.m20 / value;

            return res;
        }

        public static M3 operator /(double value, M3 rhs)
        {
            return rhs / value;
        }

        public static bool operator ==(M3 lhs, M3 rhs)
        {
            // Returns false in the presence of NaN values.
            return lhs.GetColumn(0) == rhs.GetColumn(0)
                   && lhs.GetColumn(1) == rhs.GetColumn(1)
                   && lhs.GetColumn(2) == rhs.GetColumn(2);
        }

        public static bool operator !=(M3 lhs, M3 rhs)
        {
            // Returns true in the presence of NaN values.
            return !(lhs == rhs);
        }

        // Get a column of the matrix.
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

        // Returns a row of the matrix.
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

        // Sets a column of the matrix.
        public void SetColumn(int index, V3 column)
        {
            this[0, index] = column.x;
            this[1, index] = column.y;
            this[2, index] = column.z;
        }

        // Sets a row of the matrix.
        public void SetRow(int index, V3 row)
        {
            this[index, 0] = row.x;
            this[index, 1] = row.y;
            this[index, 2] = row.z;
        }

        // Transforms a direction by this matrix.
        public V3 MultiplyVector(V3 vector)
        {
            V3 res;
            res.x = m00 * vector.x + m01 * vector.y + m02 * vector.z;
            res.y = m10 * vector.x + m11 * vector.y + m12 * vector.z;
            res.z = m20 * vector.x + m21 * vector.y + m22 * vector.z;
            return res;
        }

        // Creates a rotation matrix. Note: Assumes unit quaternion
        public static M3 Rotate(QuaternionD q)
        {
            // Precalculate coordinate products
            double x = q.x * 2.0F;
            double y = q.y * 2.0F;
            double z = q.z * 2.0F;
            double xx = q.x * x;
            double yy = q.y * y;
            double zz = q.z * z;
            double xy = q.x * y;
            double xz = q.x * z;
            double yz = q.y * z;
            double wx = q.w * x;
            double wy = q.w * y;
            double wz = q.w * z;

            // Calculate 3x3 matrix from orthonormal basis
            M3 m;
            m.m00 = 1.0f - (yy + zz);
            m.m10 = xy + wz;
            m.m20 = xz - wy;
            m.m01 = xy - wz;
            m.m11 = 1.0f - (xx + zz);
            m.m21 = yz + wx;
            m.m02 = xz + wy;
            m.m12 = yz - wx;
            m.m22 = 1.0f - (xx + yy);
            return m;
        }

        // Returns a matrix with all elements set to zero (RO).
        public static M3 zero { get; } = new M3(new V3(0, 0, 0),
            new V3(0, 0, 0),
            new V3(0, 0, 0));

        // Returns the identity matrix (RO).
        public static M3 identity { get; } = new M3(new V3(1, 0, 0),
            new V3(0, 1, 0),
            new V3(0, 0, 1));

        public override string ToString()
        {
            return ToString(null, CultureInfo.InvariantCulture.NumberFormat);
        }

        public string ToString(string? format)
        {
            return ToString(format, CultureInfo.InvariantCulture.NumberFormat);
        }

        public string ToString(string? format, IFormatProvider formatProvider)
        {
            if (string.IsNullOrEmpty(format))
                format = "G9";
            return string.Format("{0}\t{1}\t{2}\n{3}\t{4}\t{5}\n{6}\t{7}\t{8}\n",
                m00.ToString(format, formatProvider), m01.ToString(format, formatProvider), m02.ToString(format, formatProvider),
                m10.ToString(format, formatProvider), m11.ToString(format, formatProvider), m12.ToString(format, formatProvider),
                m20.ToString(format, formatProvider), m21.ToString(format, formatProvider), m22.ToString(format, formatProvider));
        }

        // private QuaternionD    GetRotation();

        private bool IsIdentity()
        {
            // FIXME: epsilon/ULPs?
            return m00 == 1.0 && m10 == 0.0 && m20 == 0.0 &&
                   m01 == 0.0 && m11 == 1.0 && m21 == 0.0 &&
                   m02 == 0.0 && m12 == 0.0 && m22 == 1.0;
        }

        private double GetDeterminant()
        {
            return m00 * (m11 * m22 - m21 * m12) - m10 * (m01 * m22 - m21 * m02) + m20 * (m01 * m12 - m11 * m02);
        }

        // public QuaternionD rotation               { get { return GetRotation(); } }
        public        bool   isIdentity        => IsIdentity();
        public        double determinant       => GetDeterminant();
        public static double Determinant(M3 m) { return m.determinant; }

        // FIXME: really needs testing
        public static M3 Inverse(M3 m)
        {
            double a = m.m11 * m.m22 - m.m21 * m.m12;
            double b = m.m21 * m.m02 - m.m01 * m.m22;
            double c = m.m01 * m.m12 - m.m11 * m.m02;
            double d = m.m20 * m.m12 - m.m10 * m.m22;
            double e = m.m00 * m.m22 - m.m20 * m.m02;
            double f = m.m10 * m.m02 - m.m00 * m.m12;
            double g = m.m10 * m.m21 - m.m20 * m.m11;
            double h = m.m20 * m.m01 - m.m00 * m.m21;
            double i = m.m10 * m.m01 - m.m11 * m.m00;
            return new M3(new V3(a, b, c),
                new V3(d, e, f),
                new V3(g, h, i)) * (1 / (m.m00 * a + m.m10 * b + m.m20 * c));
        }

        public M3 inverse => Inverse(this);

        public static M3 Transpose(M3 m)
        {
            return new M3(new V3(m.m00, m.m10, m.m20),
                new V3(m.m01, m.m11, m.m12),
                new V3(m.m02, m.m12, m.m22));
        }

        public M3 transpose => Transpose(this);

        //        private V3       GetLossyScale();
//        private FrustumPlanes DecomposeProjection();
//        public V3 lossyScale                { get { return GetLossyScale(); } }
//        public FrustumPlanes decomposeProjection { get { return DecomposeProjection(); } }
//        public bool ValidTRS();
//        public static M3 TRS(V3 pos, QuaternionD q, V3 s);
//        public void SetTRS(V3 pos, QuaternionD q, V3 s) { this = M3.TRS(pos, q, s); }
//        public static bool Inverse3DAffine(M3 input, ref M3 result);
//        public static M3 Ortho(double left, double right, double bottom, double top, double zNear, double zFar);
//        public static M3 Perspective(double fov, double aspect, double zNear, double zFar);
//        public static M3 LookAt(V3 from, V3 to, V3 up);
//        public static M3 Frustum(double left, double right, double bottom, double top, double zNear, double zFar);
//        public static M3 Frustum(FrustumPlanes fp) { return Frustum(fp.left, fp.right, fp.bottom, fp.top, fp.zNear, fp.zFar); }
    }
}
