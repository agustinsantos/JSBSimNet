#region Copyright(C)  Licensed under GNU GPL.
/// Copyright (C) 2005-2006 Agustin Santos Mendez
/// 
/// JSBSim was developed by Jon S. Berndt, Tony Peden, and
/// David Megginson. 
/// Agustin Santos Mendez implemented and maintains this C# version.
/// 
/// This program is free software; you can redistribute it and/or
///  modify it under the terms of the GNU General Public License
/// as published by the Free Software Foundation; either version 2
/// of the License, or (at your option) any later version.
///  
/// This program is distributed in the hope that it will be useful,
/// but WITHOUT ANY WARRANTY; without even the implied warranty of
/// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
/// GNU General Public License for more details.
///  
/// You should have received a copy of the GNU General Public License
/// along with this program; if not, write to the Free Software
/// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
#endregion
namespace CommonUtils.MathLib
{
    using System;
    using System.Diagnostics;
    using System.Collections;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Security.Permissions;

    /// <summary>
    /// Matrix class for 3D.
    /// </summary>
    [Serializable]
    public struct Matrix3D : ISerializable, ICloneable
    {
        #region Public Variables
        /// <summary>
        /// First row.
        /// </summary>
        public double M11, M12, M13;
        /// <summary>
        /// Second row.
        /// </summary>
        public double M21, M22, M23;
        /// <summary>
        /// Third row.
        /// </summary>
        public double M31, M32, M33;
        #endregion

        #region Private helper functions
        private void Swap(ref double a, ref double b)
        {
            double c = a;
            a = b;
            b = c;
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="Matrix3D"/> class using given values.
        /// </summary>
        public Matrix3D(
            double m11, double m12, double m13,
            double m21, double m22, double m23,
            double m31, double m32, double m33)
        {
            M11 = m11; M12 = m12; M13 = m13;
            M21 = m21; M22 = m22; M23 = m23;
            M31 = m31; M32 = m32; M33 = m33;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Matrix3D"/> class using values from a given array.
        /// </summary>
        /// <param name="elements">The matrix values.</param>
        public Matrix3D(double[] elements)
        {
            Debug.Assert(elements != null);
            Debug.Assert(elements.Length == 9);

            M11 = elements[0]; M12 = elements[1]; M13 = elements[2];
            M21 = elements[3]; M22 = elements[4]; M23 = elements[5];
            M31 = elements[6]; M32 = elements[7]; M33 = elements[8];
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Matrix3D"/> class using given 3D vectors.
        /// </summary>
        /// <param name="column1">The vector to use as the first column.</param>
        /// <param name="column2">The vector to use as the second column.</param>
        /// <param name="column3">The vector to use as the third column.</param>
        public Matrix3D(Vector3D column1, Vector3D column2, Vector3D column3)
        {
            //Debug.Assert(column1 != null);
            //Debug.Assert(column2 != null);
            //Debug.Assert(column3 != null);

            M11 = column1.X; M12 = column2.X; M13 = column3.X;
            M21 = column1.Y; M22 = column2.Y; M23 = column3.Y;
            M31 = column1.Z; M32 = column2.Z; M33 = column3.Z;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Matrix3D"/> class using a given matrix.
        /// </summary>
        /// <param name="m">The matrix to copy values from.</param>
        public Matrix3D(Matrix3D m)
        {
            //Debug.Assert(m != null);

            M11 = m.M11; M12 = m.M12; M13 = m.M13;
            M21 = m.M21; M22 = m.M22; M23 = m.M23;
            M31 = m.M31; M32 = m.M32; M33 = m.M33;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Vector3D"/> class with serialized data.
        /// </summary>
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
        private Matrix3D(SerializationInfo info, StreamingContext context)
        {
            // Get the first row
            this.M11 = info.GetSingle("M11");
            this.M12 = info.GetSingle("M12");
            this.M13 = info.GetSingle("M13");

            // Get the second row
            this.M21 = info.GetSingle("M21");
            this.M22 = info.GetSingle("M22");
            this.M23 = info.GetSingle("M23");

            // Get the third row
            this.M31 = info.GetSingle("M31");
            this.M32 = info.GetSingle("M32");
            this.M33 = info.GetSingle("M33");
        }
        #endregion

        #region Constants
        /// <summary>
        /// Zero matrix.
        /// </summary>
        public static readonly Matrix3D Zero = new Matrix3D(0, 0, 0, 0, 0, 0, 0, 0, 0);
        /// <summary>
        /// Identity matrix.
        /// </summary>
        public static readonly Matrix3D Identity = new Matrix3D(1, 0, 0, 0, 1, 0, 0, 0, 1);
        #endregion

        #region ISerializable Members
        /// <summary>
        /// Populates a <see cref="SerializationInfo"/> with the data needed to serialize the target object.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> to populate with data. </param>
        /// <param name="context">The destination (see <see cref="StreamingContext"/>) for this serialization.</param>
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // First row
            info.AddValue("M11", M11);
            info.AddValue("M12", M12);
            info.AddValue("M13", M13);

            // Second row
            info.AddValue("M21", M21);
            info.AddValue("M22", M22);
            info.AddValue("M23", M23);

            // Third row
            info.AddValue("M31", M31);
            info.AddValue("M32", M32);
            info.AddValue("M33", M33);
        }
        #endregion

        #region ICloneable Members
        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        object ICloneable.Clone()
        {
            return new Matrix3D(this);
        }
        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public Matrix3D Clone()
        {
            return new Matrix3D(this);
        }
        #endregion

        #region Overrides
        /// <summary>
        /// Get the hashcode for this <see cref="Matrix3D"/> instance.
        /// </summary>
        /// <returns>Returns the hash code for this <see cref="Matrix3D"/> instance.</returns>
        public override int GetHashCode()
        {
            return
                M11.GetHashCode() ^ M12.GetHashCode() ^ M13.GetHashCode() ^
                M21.GetHashCode() ^ M22.GetHashCode() ^ M23.GetHashCode() ^
                M31.GetHashCode() ^ M32.GetHashCode() ^ M33.GetHashCode();
        }
        /// <summary>
        /// Checks if a given matrix equals to self.
        /// </summary>
        /// <param name="o">Object to check if equal to.</param>
        /// <returns></returns>
        public override bool Equals(object o)
        {
            if (o is Matrix3D m)
            {
                // Check the matrix elements
                if (M11 != m.M11) return false;
                if (M12 != m.M12) return false;
                if (M13 != m.M13) return false;

                if (M21 != m.M21) return false;
                if (M22 != m.M22) return false;
                if (M23 != m.M23) return false;

                if (M31 != m.M31) return false;
                if (M32 != m.M32) return false;
                if (M33 != m.M33) return false;

                return true;
            }
            return false;
        }
        /// <summary>
        /// Convert Matrix3D to a string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder s = new StringBuilder();
            s.Append(String.Format("[ {0}, {1}, {2} ]\n", M11, M12, M13));
            s.Append(String.Format("[ {0}, {1}, {2} ]\n", M21, M22, M23));
            s.Append(String.Format("[ {0}, {1}, {2} ]\n", M31, M32, M33));

            return s.ToString();
        }

        public string ToString(string formatDouble, IFormatProvider provider)
        {
            StringBuilder s = new StringBuilder();
            s.Append(M11.ToString(formatDouble, provider));
            s.Append(", ");
            s.Append(M12.ToString(formatDouble, provider));
            s.Append(", ");
            s.Append(M13.ToString(formatDouble, provider));
            s.Append(", ");
            s.Append(M21.ToString(formatDouble, provider));
            s.Append(", ");
            s.Append(M22.ToString(formatDouble, provider));
            s.Append(", ");
            s.Append(M23.ToString(formatDouble, provider));
            s.Append(", ");
            s.Append(M31.ToString(formatDouble, provider));
            s.Append(", ");
            s.Append(M32.ToString(formatDouble, provider));
            s.Append(", ");
            s.Append(M33.ToString(formatDouble, provider));

            return s.ToString();
        }


        #endregion

        #region Public Methods
        /// <summary>
        /// Calcluates the determinant of the matrix.
        /// </summary>
        /// <returns>The matrix determinant value.</returns>
        public double Determinant()
        {
            // rule of Sarrus
            return
                M11 * M22 * M33 + M12 * M23 * M31 + M13 * M21 * M32 -
                M13 * M22 * M31 - M11 * M23 * M32 - M12 * M21 * M33;
        }


        /// <summary>
        /// Gets the trace of the matrix which is the sum of the diagonal entries.
        /// </summary>
        /// <returns>Returns the trace of the matrix.</returns>
        public double Trace()
        {
            return M11 + M22 + M33;
        }


        /// <summary>
        /// Transpose this matrix.
        /// </summary>
        public void Transpose()
        {
            this.Swap(ref M12, ref M21);
            this.Swap(ref M13, ref M31);
            this.Swap(ref M23, ref M32);
        }


        /// <summary>
        /// Gets the transpose of this matrix.
        /// </summary>
        /// <returns>Returns a trasposed matrix.</returns>
        public Matrix3D GetTranspose()
        {
            Matrix3D m = new Matrix3D(this);
            m.Transpose();
            return m;
        }
        /// <summary>
        /// Gets the transpose of this matrix.
        /// </summary>
        /// <returns>Returns a trasposed matrix.</returns>
        public Matrix3D Transposed()
        { return GetTranspose();  }

        /// <summary>
        /// Multiplies self by a given matrix.
        /// </summary>
        /// <param name="m">The matrix to multiply with.</param>
        public void Multiply(Matrix3D m)
        {
            // Save our current matrix values for use in calculations
            Matrix3D s = this;

            M11 = s.M11 * m.M11 + s.M12 * m.M21 + s.M13 * m.M31;
            M12 = s.M11 * m.M12 + s.M12 * m.M22 + s.M13 * m.M32;
            M13 = s.M11 * m.M13 + s.M12 * m.M23 + s.M13 * m.M33;

            M21 = s.M21 * m.M11 + s.M22 * m.M21 + s.M23 * m.M31;
            M22 = s.M21 * m.M12 + s.M22 * m.M22 + s.M23 * m.M32;
            M23 = s.M21 * m.M13 + s.M22 * m.M23 + s.M23 * m.M33;

            M31 = s.M31 * m.M11 + s.M32 * m.M21 + s.M33 * m.M31;
            M32 = s.M31 * m.M12 + s.M32 * m.M22 + s.M33 * m.M32;
            M33 = s.M31 * m.M13 + s.M32 * m.M23 + s.M33 * m.M33;
        }

        /// <summary>
        /// Multiply self by a scalar.
        /// </summary>
        /// <param name="scalar">The scalar to multiply with.</param>
        public void Multiply(double scalar)
        {
            M11 *= scalar;
            M12 *= scalar;
            M13 *= scalar;

            M21 *= scalar;
            M22 *= scalar;
            M23 *= scalar;

            M31 *= scalar;
            M32 *= scalar;
            M33 *= scalar;
        }

        /// <summary>
        /// Divides self by a scalar.
        /// </summary>
        /// <param name="scalar">The scalar to divide with.</param>
        public void Divide(double scalar)
        {
            M11 /= scalar;
            M12 /= scalar;
            M13 /= scalar;

            M21 /= scalar;
            M22 /= scalar;
            M23 /= scalar;

            M31 /= scalar;
            M32 /= scalar;
            M33 /= scalar;
        }

        /// <summary>
        /// Returns the quaternion associated with this direction cosine (rotation) matrix.
        /// </summary>
        /// <returns></returns>
        public Quaternion GetQuaternion()
        {
            Quaternion Q = new Quaternion();

            double[] tempQ = new double[4];
            int idx;

            tempQ[0] = 1.0 + this.M11 + this.M22 + this.M33;
            tempQ[1] = 1.0 + this.M11 - this.M22 - this.M33;
            tempQ[2] = 1.0 - this.M11 + this.M22 - this.M33;
            tempQ[3] = 1.0 - this.M11 - this.M22 + this.M33;

            // Find largest of the above
            idx = 0;
            for (int i = 1; i < 4; i++) if (tempQ[i] > tempQ[idx]) idx = i;

            switch (idx)
            {
                case 0:
                    Q.W = 0.50 * Math.Sqrt(tempQ[0]);
                    Q.X = 0.25 * (M32 - M23) / Q.W;
                    Q.Y = 0.25 * (M13 - M31) / Q.W;
                    Q.Z = 0.25 * (M21 - M12) / Q.W;
                    break;
                case 1:
                    Q.X = 0.50 * Math.Sqrt(tempQ[1]);
                    Q.W = 0.25 * (M32 - M23) / Q.X;
                    Q.Y = 0.25 * (M21 + M12) / Q.X;
                    Q.Z = 0.25 * (M13 + M31) / Q.X;
                    break;
                case 2:
                    Q.Y = 0.50 * Math.Sqrt(tempQ[2]);
                    Q.W = 0.25 * (M13 - M31) / Q.Y;
                    Q.X = 0.25 * (M21 + M12) / Q.Y;
                    Q.Z = 0.25 * (M32 + M23) / Q.Y;
                    break;
                case 3:
                    Q.Z = 0.50 * Math.Sqrt(tempQ[3]);
                    Q.W = 0.25 * (M21 - M12) / Q.Z;
                    Q.X = 0.25 * (M31 + M13) / Q.Z;
                    Q.Y = 0.25 * (M32 + M23) / Q.Z;
                    break;
                default:
                    //error
                    break;
            }

            return Q;
        }

        /// <summary>
        /// Compute the Euler-angles
        /// Also see Jack Kuipers, "Quaternions and Rotation Sequences", section 7.8.
        /// </summary>
        /// <returns>the Euler-angles</returns>
        public Vector3D GetEuler()
        {
            Vector3D mEulerAngles = new Vector3D();
            bool GimbalLock = false;

            if (this.M13 <= -1.0)
            {
                mEulerAngles.Theta = 0.5 * Math.PI;
                GimbalLock = true;
            }
            else if (1.0 <= this.M13)
            {
                mEulerAngles.Theta = -0.5 * Math.PI;
                GimbalLock = true;
            }
            else
                mEulerAngles.Theta = Math.Asin(-this.M13);

            if (GimbalLock)
                mEulerAngles.Phi = Math.Atan2(-this.M32, this.M22);
            else
                mEulerAngles.Phi = Math.Atan2(this.M23, this.M33);

            if (GimbalLock)
                mEulerAngles.Psi = 0.0;
            else
            {
                double psi = Math.Atan2(this.M12, this.M11);
                if (psi < 0.0)
                    psi += 2 * Math.PI;
                mEulerAngles.Psi = psi;
            }

            return mEulerAngles;
        }

        #endregion

        #region Operators
        /// <summary>
        /// Checks if the two given matrices are equal.
        /// </summary>
        /// <param name="a">The first of two 3D matrices to compare.</param>
        /// <param name="b">The second of two 3D matrices to compare.</param>
        /// <returns><b>true</b> if the matrices are equal; otherwise, <b>false</b>.</returns>
        public static bool operator ==(Matrix3D a, Matrix3D b)
        {
            if (Object.Equals(a, null) == true)
            {
                return Object.Equals(b, null);
            }

            if (Object.Equals(b, null) == true)
            {
                return Object.Equals(a, null);
            }

            return
                (a.M11 == b.M11) && (a.M12 == b.M12) && (a.M13 == b.M13) &&
                (a.M21 == b.M21) && (a.M22 == b.M22) && (a.M23 == b.M23) &&
                (a.M31 == b.M31) && (a.M32 == b.M32) && (a.M33 == b.M33);
        }

        /// <summary>
        /// Checks if the two given vectors are not equal.
        /// </summary>
        /// <param name="a">The first of two 3D vectors to compare.</param>
        /// <param name="b">The second of two 3D vectors to compare.</param>
        /// <returns><b>true</b> if the matrices are not equal; otherwise, <b>false</b>.</returns>
        public static bool operator !=(Matrix3D a, Matrix3D b)
        {
            if (Object.Equals(a, null) == true)
            {
                return !Object.Equals(b, null);
            }
            else if (Object.Equals(b, null) == true)
            {
                return !Object.Equals(a, null);
            }
            return !((a.M11 == b.M11) && (a.M12 == b.M12) && (a.M13 == b.M13) &&
                (a.M21 == b.M21) && (a.M22 == b.M22) && (a.M23 == b.M23) &&
                (a.M31 == b.M31) && (a.M32 == b.M32) && (a.M33 == b.M33));
        }

        /// <summary>
        /// An index accessor.
        /// </summary>
        public unsafe double this[int index]
        {
            get
            {
                if (index < 0 || index >= 9)
                    throw new IndexOutOfRangeException("Invalid matrix index!");

                fixed (double* f = &this.M11)
                {
                    return *(f + index);
                }
            }
            set
            {
                if (index < 0 || index >= 9)
                    throw new IndexOutOfRangeException("Invalid matrix index!");

                fixed (double* f = &this.M11)
                {
                    *(f + index) = value;
                }
            }
        }
        /// <summary>
        /// An index accessor.
        /// </summary>
        private double this[int row, int column]
        {
            get
            {
                return this[row * 3 + column];
            }
            set
            {
                this[row * 3 + column] = value;
            }
        }
        /// <summary>
        /// Multiplies two matrices.
        /// </summary>
        /// <param name="a">First matrix.</param>
        /// <param name="b">Second matrix.</param>
        /// <returns>The result of the multiply operation.</returns>
        public static Matrix3D operator *(Matrix3D a, Matrix3D b)
        {
            Matrix3D result = a;
            result.Multiply(b);

            return result;
        }
        /// <summary>
        /// Multiplies a matrix with a scalar.
        /// </summary>
        /// <param name="source">The matrix.</param>
        /// <param name="scalar">The scalar.</param>
        /// <returns></returns>
        public static Matrix3D operator *(Matrix3D source, double scalar)
        {
            Matrix3D result = source;
            result.Multiply(scalar);

            return result;
        }
        /// <summary>
        /// Divides a matrix by a scalar.
        /// </summary>
        /// <param name="source">The matrix.</param>
        /// <param name="scalar">The scalar.</param>
        /// <returns></returns>
        public static Matrix3D operator /(Matrix3D source, double scalar)
        {
            Matrix3D result = source;
            result.Divide(scalar);

            return result;
        }

        /// <summary>
        /// Add two matrices.
        /// </summary>
        /// <param name="m1">The first matrix to add.</param>
        /// <param name="m2">The second matrix to add.</param>
        /// <returns>Result is ( m1.M11 + m2.M11, m1.M12 + m2.M12, ...)</returns>
        public static Matrix3D operator +(Matrix3D m1, Matrix3D m2)
        {
            return new Matrix3D(
                m1.M11 + m2.M11, m1.M12 + m2.M12, m1.M13 + m2.M13,
                m1.M21 + m2.M21, m1.M22 + m2.M22, m1.M23 + m2.M23,
                m1.M31 + m2.M31, m1.M32 + m2.M32, m1.M33 + m2.M33);

        }

        public static Vector3D operator *(Matrix3D m1, Vector3D v)
        {
            return new Vector3D(
                m1.M11 * v.X + m1.M12 * v.Y + m1.M13 * v.Z,
                m1.M21 * v.X + m1.M22 * v.Y + m1.M23 * v.Z,
                m1.M31 * v.X + m1.M32 * v.Y + m1.M33 * v.Z);

        }

        #endregion
    }
}
