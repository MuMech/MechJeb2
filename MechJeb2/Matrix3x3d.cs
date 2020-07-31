using System;
using UnityEngine;
using System.Globalization;

namespace MuMech
{
    public struct Matrix3x3d : IEquatable<Matrix3x3d>, IFormattable
    {
        // memory layout:
        //
        //                row no (=vertical)
        //               |  0   1   2
        //            ---+------------
        //            0  | m00 m10 m20
        // column no  1  | m01 m11 m21
        // (=horiz)   2  | m02 m12 m22

        public double m00;
        public double m10;
        public double m20;

        public double m01;
        public double m11;
        public double m21;

        public double m02;
        public double m12;
        public double m22;

        public Matrix3x3d(Vector3d column0, Vector3d column1, Vector3d column2)
        {
            this.m00 = column0.x; this.m01 = column1.x; this.m02 = column2.x;
            this.m10 = column0.y; this.m11 = column1.y; this.m12 = column2.y;
            this.m20 = column0.z; this.m21 = column1.z; this.m22 = column2.z;
        }

        // Access element at [row, column].
        public double this[int row, int column]
        {
            get
            {
                return this[row + column * 3];
            }

            set
            {
                this[row + column * 3] = value;
            }
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
                    case 0: m00 = value; break;
                    case 1: m10 = value; break;
                    case 2: m20 = value; break;
                    case 3: m01 = value; break;
                    case 4: m11 = value; break;
                    case 5: m21 = value; break;
                    case 6: m02 = value; break;
                    case 7: m12 = value; break;
                    case 8: m22 = value; break;

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

        // also required for being able to use Matrix3x3d as keys in hash tables
        public override bool Equals(object other)
        {
            if (!(other is Matrix3x3d)) return false;

            return Equals((Matrix3x3d)other);
        }

        public bool Equals(Matrix3x3d other)
        {
            return GetColumn(0).Equals(other.GetColumn(0))
                && GetColumn(1).Equals(other.GetColumn(1))
                && GetColumn(2).Equals(other.GetColumn(2));
        }

        // Multiplies two matrices.
        public static Matrix3x3d operator*(Matrix3x3d lhs, Matrix3x3d rhs)
        {
            Matrix3x3d res;
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

        // Transforms a [[Vector3d]] by a matrix.
        public static Vector3d operator*(Matrix3x3d lhs, Vector3d vector)
        {
            Vector3d res;
            res.x = lhs.m00 * vector.x + lhs.m01 * vector.y + lhs.m02 * vector.z;
            res.y = lhs.m10 * vector.x + lhs.m11 * vector.y + lhs.m12 * vector.z;
            res.z = lhs.m20 * vector.x + lhs.m21 * vector.y + lhs.m22 * vector.z;
            return res;
        }

        // Multiplies a matrix by a number
        public static Matrix3x3d operator*(Matrix3x3d lhs, double value)
        {
            Matrix3x3d res;
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

        public static Matrix3x3d operator*(double value, Matrix3x3d rhs)
        {
            return rhs * value;
        }

        // Divides a matrix by a number
        public static Matrix3x3d operator/(Matrix3x3d lhs, double value)
        {
            Matrix3x3d res;
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

        public static Matrix3x3d operator/(double value, Matrix3x3d rhs)
        {
            return rhs / value;
        }

        public static bool operator==(Matrix3x3d lhs, Matrix3x3d rhs)
        {
            // Returns false in the presence of NaN values.
            return lhs.GetColumn(0) == rhs.GetColumn(0)
                && lhs.GetColumn(1) == rhs.GetColumn(1)
                && lhs.GetColumn(2) == rhs.GetColumn(2);
        }

        public static bool operator!=(Matrix3x3d lhs, Matrix3x3d rhs)
        {
            // Returns true in the presence of NaN values.
            return !(lhs == rhs);
        }

        // Get a column of the matrix.
        public Vector3d GetColumn(int index)
        {
            switch (index)
            {
                case 0: return new Vector3d(m00, m10, m20);
                case 1: return new Vector3d(m01, m11, m21);
                case 2: return new Vector3d(m02, m12, m22);
                default:
                    throw new IndexOutOfRangeException("Invalid column index!");
            }
        }

        // Returns a row of the matrix.
        public Vector3d GetRow(int index)
        {
            switch (index)
            {
                case 0: return new Vector3d(m00, m01, m02);
                case 1: return new Vector3d(m10, m11, m12);
                case 2: return new Vector3d(m20, m21, m22);
                default:
                    throw new IndexOutOfRangeException("Invalid row index!");
            }
        }

        // Sets a column of the matrix.
        public void SetColumn(int index, Vector3d column)
        {
            this[0, index] = column.x;
            this[1, index] = column.y;
            this[2, index] = column.z;
        }

        // Sets a row of the matrix.
        public void SetRow(int index, Vector3d row)
        {
            this[index, 0] = row.x;
            this[index, 1] = row.y;
            this[index, 2] = row.z;
        }

        // Transforms a direction by this matrix.
        public Vector3d MultiplyVector(Vector3d vector)
        {
            Vector3d res;
            res.x = this.m00 * vector.x + this.m01 * vector.y + this.m02 * vector.z;
            res.y = this.m10 * vector.x + this.m11 * vector.y + this.m12 * vector.z;
            res.z = this.m20 * vector.x + this.m21 * vector.y + this.m22 * vector.z;
            return res;
        }

        // Creates a rotation matrix. Note: Assumes unit quaternion
        public static Matrix3x3d Rotate(QuaternionD q)
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
            Matrix3x3d m;
            m.m00 = 1.0f - (yy + zz); m.m10 = xy + wz; m.m20 = xz - wy;
            m.m01 = xy - wz; m.m11 = 1.0f - (xx + zz); m.m21 = yz + wx;
            m.m02 = xz + wy; m.m12 = yz - wx; m.m22 = 1.0f - (xx + yy);
            return m;
        }

        static readonly Matrix3x3d zeroMatrix = new Matrix3x3d(new Vector3d(0, 0, 0),
            new Vector3d(0, 0, 0),
            new Vector3d(0, 0, 0));

        // Returns a matrix with all elements set to zero (RO).
        public static Matrix3x3d zero { get { return zeroMatrix; } }

        static readonly Matrix3x3d identityMatrix = new Matrix3x3d(new Vector3d(1, 0, 0),
            new Vector3d(0, 1, 0),
            new Vector3d(0, 0, 1));

        // Returns the identity matrix (RO).
        public static Matrix3x3d identity    { get { return identityMatrix; } }

        public override string ToString()
        {
            return ToString(null, CultureInfo.InvariantCulture.NumberFormat);
        }

        public string ToString(string format)
        {
            return ToString(format, CultureInfo.InvariantCulture.NumberFormat);
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (string.IsNullOrEmpty(format))
                format = "G9";
            return String.Format("{0}\t{1}\t{2}\n{3}\t{4}\t{5}\n{6}\t{7}\t{8}\n",
                m00.ToString(format, formatProvider), m01.ToString(format, formatProvider), m02.ToString(format, formatProvider),
                m10.ToString(format, formatProvider), m11.ToString(format, formatProvider), m12.ToString(format, formatProvider),
                m20.ToString(format, formatProvider), m21.ToString(format, formatProvider), m22.ToString(format, formatProvider));
        }

        // private QuaternionD    GetRotation();

        private bool          IsIdentity() {
            // FIXME: epsilon/ULPs?
            return m00 == 1.0 && m10 == 0.0 && m20 == 0.0 &&
                m01 == 0.0 && m11 == 1.0 && m21 == 0.0 &&
                m02 == 0.0 && m12 == 0.0 && m22 == 0.0;
        }
        private double         GetDeterminant() {
            return m00 * ( m11 * m22 - m21 * m12 ) - m10 * ( m01 * m22 - m21 * m02 ) + m20 * ( m01 * m12 - m11 * m02 );
        }

        // public QuaternionD rotation               { get { return GetRotation(); } }
        public bool isIdentity                   { get { return IsIdentity(); } }
        public double determinant                 { get { return GetDeterminant(); } }
        public static double Determinant(Matrix3x3d m) { return m.determinant; }

        // FIXME: really needs testing
        public static Matrix3x3d Inverse(Matrix3x3d m) {
            double a = m.m11 * m.m22 - m.m21 * m.m12;
            double b = m.m21 * m.m02 - m.m01 * m.m22;
            double c = m.m01 * m.m12 - m.m11 * m.m02;
            double d = m.m20 * m.m12 - m.m10 * m.m22;
            double e = m.m00 * m.m22 - m.m20 * m.m02;
            double f = m.m10 * m.m02 - m.m00 * m.m12;
            double g = m.m10 * m.m21 - m.m20 * m.m11;
            double h = m.m20 * m.m01 - m.m00 * m.m21;
            double i = m.m10 * m.m01 - m.m11 * m.m00;
            return new Matrix3x3d(new Vector3d(a, b, c),
                    new Vector3d(d, e, f),
                    new Vector3d(g, h, i)) * ( 1 / ( m.m00 * a + m.m10 * b + m.m20 * c ));
        }

        public Matrix3x3d inverse { get { return Matrix3x3d.Inverse(this); } }

        public static Matrix3x3d Transpose(Matrix3x3d m) {
            return new Matrix3x3d(new Vector3d(m.m00, m.m10, m.m20),
            new Vector3d(m.m01, m.m11, m.m12),
            new Vector3d(m.m02, m.m12, m.m22));
        }

        public Matrix3x3d transpose { get { return Matrix3x3d.Transpose(this); } }

//        private Vector3d       GetLossyScale();
//        private FrustumPlanes DecomposeProjection();
//        public Vector3d lossyScale                { get { return GetLossyScale(); } }
//        public FrustumPlanes decomposeProjection { get { return DecomposeProjection(); } }
//        public bool ValidTRS();
//        public static Matrix3x3d TRS(Vector3d pos, QuaternionD q, Vector3d s);
//        public void SetTRS(Vector3d pos, QuaternionD q, Vector3d s) { this = Matrix3x3d.TRS(pos, q, s); }
//        public static bool Inverse3DAffine(Matrix3x3d input, ref Matrix3x3d result);
//        public static Matrix3x3d Ortho(double left, double right, double bottom, double top, double zNear, double zFar);
//        public static Matrix3x3d Perspective(double fov, double aspect, double zNear, double zFar);
//        public static Matrix3x3d LookAt(Vector3d from, Vector3d to, Vector3d up);
//        public static Matrix3x3d Frustum(double left, double right, double bottom, double top, double zNear, double zFar);
//        public static Matrix3x3d Frustum(FrustumPlanes fp) { return Frustum(fp.left, fp.right, fp.bottom, fp.top, fp.zNear, fp.zFar); }
    }
}
