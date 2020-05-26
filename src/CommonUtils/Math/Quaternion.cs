#region Copyright(C)  Licensed under GNU GPL.
/// Copyright (C) 2005-2020 Agustin Santos Mendez
/// 
/// JSBSim was developed by Jon S. Berndt, Tony Peden, and
/// David Megginson. 
/// Agustin Santos Mendez implemented and maintains this C# version.
/// 
/// This program is free software; you can redistribute it and/or
/// modify it under the terms of the GNU General Public License
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
/// 
/// Further information about the GNU Lesser General Public License can also be found on
/// the world wide web at http://www.gnu.org.
#endregion
namespace CommonUtils.MathLib
{
    using System;
    using System.Diagnostics;
    using System.Runtime.Serialization;
    using System.Security.Permissions;

    /// <summary>
    /// <para>This class represents a Quaternion.</para>
    /// <para>
    /// A quaternion can be thought of as a 4D vector of form:
    /// q = [w, x, y, z] = w + xi + yj +zk.
    /// </para>
    /// <para>
    /// A Quaternion is often written as q = s + V where S represents
    /// the scalar part (w component) and V is a 3D vector representing
    /// the imaginery coefficients (x,y,z components).
    /// </para>
    /// Models the Quaternion representation of rotations.
    /// Quaternion is a representation of an arbitrary rotation through a
    /// quaternion. It has vector properties. This class also contains access
    /// functions to the euler angle representation of rotations and access to
    /// transformation matrices for 3D vectors. Transformations and euler angles are
    /// therefore computed once they are requested for the first time. Then they are
    /// cached for later usage as long as the class is not accessed trough
    /// a nonconst member function.
    /// 
    /// 
    /// Cooke, Zyda, Pratt, and McGhee, "NPSNET: Flight Simulation Dynamic Modeling
    /// Using Quaternions", Presence, Vol. 1, No. 4, pp. 404-420  Naval Postgraduate
    /// School, January 1994
    /// D. M. Henderson, "Euler Angles, Quaternions, and Transformation Matrices",
    /// JSC 12960, July 1977
    /// Richard E. McFarland, "A Standard Kinematic Model for Flight Simulation at
    /// NASA-Ames", NASA CR-2497, January 1975
    /// Barnes W. McCormick, "Aerodynamics, Aeronautics, and Flight Mechanics",
    /// Wiley &#38; Sons, 1979 ISBN 0-471-03032-5
    ///	Bernard Etkin, "Dynamics of Flight, Stability and Control", Wiley &#38; Sons,
    /// 1982 ISBN 0-471-08936-2		
    /// </summary>
    /// <remarks>
    /// Note: The order of rotations used in this class corresponds to a 3-2-1 sequence,
    /// or Y-P-R, or Z-Y-X, if you prefer.
    /// </remarks>
    [Serializable]
    public sealed class Quaternion : ICloneable, ISerializable
    {
        /// Euler angles Phi, Theta, Psi
        public enum EulerAngles { ePhi = 1, eTht, ePsi };

        #region Public variables
        /// <summary>
        /// W coordinate.
        /// </summary>
        public double W;
        /// <summary>
        /// X coordinate.
        /// </summary>
        public double X;
        /// <summary>
        /// Y coordinate.
        /// </summary>
        public double Y;
        /// <summary>
        /// Z coordinate.
        /// </summary>
        public double Z;

        #endregion


        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="Quaternion"/> class with the identity rotation.
        /// </summary>
        public Quaternion()
        {
            this.W = 1.0;
            this.X = 0.0;
            this.Y = 0.0;
            this.Z = 0.0;
            this.cache = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Quaternion"/> class using given values.
        /// </summary>
        /// <param name="w"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public Quaternion(double w, double x, double y, double z)
        {
            this.W = w;
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.cache = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Quaternion"/> class using the three euler angles.
        /// </summary>
        /// <param name="phi">The euler X axis (roll) angle in radians</param>
        /// <param name="tht">The euler Y axis (attitude) angle in radians</param>
        /// <param name="psi">The euler Z axis (heading) angle in radians</param>
        public Quaternion(double phi, double tht, double psi)
        {
            double thtd2 = 0.5 * tht;
            double psid2 = 0.5 * psi;
            double phid2 = 0.5 * phi;

            double Sthtd2 = Math.Sin(thtd2);
            double Spsid2 = Math.Sin(psid2);
            double Sphid2 = Math.Sin(phid2);

            double Cthtd2 = Math.Cos(thtd2);
            double Cpsid2 = Math.Cos(psid2);
            double Cphid2 = Math.Cos(phid2);

            double Cphid2Cthtd2 = Cphid2 * Cthtd2;
            double Cphid2Sthtd2 = Cphid2 * Sthtd2;
            double Sphid2Sthtd2 = Sphid2 * Sthtd2;
            double Sphid2Cthtd2 = Sphid2 * Cthtd2;

            this.W = Cphid2Cthtd2 * Cpsid2 + Sphid2Sthtd2 * Spsid2;
            this.X = Sphid2Cthtd2 * Cpsid2 - Cphid2Sthtd2 * Spsid2;
            this.Y = Cphid2Sthtd2 * Cpsid2 + Sphid2Cthtd2 * Spsid2;
            this.Z = Cphid2Cthtd2 * Spsid2 - Sphid2Sthtd2 * Cpsid2;
            this.cache = false;
        }

        /// <summary>
        /// Initializer by one euler angle.
        ///  Initialize the quaternion with the single euler angle where its index
        ///  is given in the first argument.
        /// </summary>
        /// <param name="idx">Index of the euler angle to initialize</param>
        /// <param name="angle">The euler angle in radians</param>
        public Quaternion(EulerAngles idx, double angle)
        {
            this.cache = false;

            double angle2 = 0.5 * angle;

            double Sangle2 = Math.Sin(angle2);
            double Cangle2 = Math.Cos(angle2);

            if (idx == EulerAngles.ePhi)
            {
                this.W = Cangle2;
                this.X = Sangle2;
                this.Y = 0.0;
                this.Z = 0.0;

            }
            else if (idx == EulerAngles.eTht)
            {
                this.W = Cangle2;
                this.X = 0.0;
                this.Y = Sangle2;
                this.Z = 0.0;

            }
            else
            {
                this.W = Cangle2;
                this.X = 0.0;
                this.Y = 0.0;
                this.Z = Sangle2;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Quaternion"/> class using the three euler angles.
        /// </summary>
        /// <param name="vOrient"> A vector with the euler X axis (roll),Y axis (attitude) and Z axis (heading) angles in radians</param>
        public Quaternion(Vector3D vOrient) : this(vOrient.Phi, vOrient.Theta, vOrient.Psi)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Quaternion"/> class using values from a given array of double.
        /// </summary>
        /// <param name="val">Quaternion parameters in order : W,X,Y,Z</param>
        public Quaternion(double[] val)
        {
            Debug.Assert(val != null);
            Debug.Assert(val.Length >= 4);

            this.W = val[0];
            this.X = val[1];
            this.Y = val[2];
            this.Z = val[3];
            this.cache = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Quaternion"/> class using values from a given quaternion.
        /// </summary>
        /// <param name="q">Quaternion to copy parameters from.</param>
        public Quaternion(Quaternion q)
        {
            // Copy the master values ...
            W = q.W;
            X = q.X;
            Y = q.Y;
            Z = q.Z;
            this.cache = q.cache;
            // .. and copy the derived values if they are valid
            if (this.cache)
            {
                this.mT = q.mT;
                this.mTInv = q.mTInv;
                this.mEulerAngles = q.mEulerAngles;
                this.mEulerSines = q.mEulerSines;
                this.mEulerCosines = q.mEulerCosines;
            }
        }

        /// <summary>
        /// Initializer by matrix.
        /// Initialize the quaternion with the matrix representing a transform from one frame
        /// to another using the standard aerospace sequence, Yaw-Pitch-Roll(3-2-1).
        /// </summary>
        /// <param name="q">the rotation matrix</param>
        public Quaternion(Matrix3D m)
        {
            //mCacheValid = false;

            this.W = 0.50 * Math.Sqrt(1.0 + m.M11 + m.M22 + m.M33);
            double t = 0.25 / this.W;
            this.X = t * (m.M23 - m.M32);
            this.Y = t * (m.M31 - m.M13);
            this.Z = t * (m.M12 - m.M21);

            Normalize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Quaternion"/> class with serialized data.
        /// </summary>
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
        private Quaternion(SerializationInfo info, StreamingContext context)
        {
            this.W = info.GetSingle("W");
            this.X = info.GetSingle("X");
            this.Y = info.GetSingle("Y");
            this.Z = info.GetSingle("Z");
            this.cache = false;
        }

        #endregion

        #region Constants
        /// <summary>
        /// The zero questernion.
        /// </summary>
        public static readonly Quaternion Zero = new Quaternion(0.0, 0.0, 0.0, 0.0);
        /// <summary>
        /// The identity quaternion.
        /// </summary>
        public static readonly Quaternion Identity = new Quaternion(1.0, 0.0, 0.0, 0.0);
        /// <summary>
        /// X-Axis.
        /// </summary>
        public static readonly Quaternion XAxis = new Quaternion(0.0, 1.0, 0.0, 0.0);
        /// <summary>
        /// Y-Axis.
        /// </summary>
        public static readonly Quaternion YAxis = new Quaternion(0.0, 0.0, 1.0, 0.0);
        /// <summary>
        /// Z-Axis.
        /// </summary>
        public static readonly Quaternion ZAxis = new Quaternion(0.0, 0.0, 0.0, 1.0);
        /// <summary>
        /// W-Axis.
        /// </summary>
        public static readonly Quaternion WAxis = new Quaternion(1.0, 0.0, 0.0, 0.0);
        #endregion

        #region ICloneable Members
        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public object Clone()
        {
            return new Quaternion(this);
        }

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
            info.AddValue("W", this.W);
            info.AddValue("X", this.X);
            info.AddValue("Y", this.Y);
            info.AddValue("Z", this.Z);
        }

        #endregion

        #region Overrides
        /// <summary>
        /// Get the hashcode for this quaternion instance.
        /// </summary>
        /// <returns>Returns the hash code for this vector instance.</returns>
        public override int GetHashCode()
        {
            return W.GetHashCode() ^ X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
        }
        /// <summary>
        /// Checks if a given quaternion equals to self.
        /// </summary>
        /// <param name="o">Object to check if equal to.</param>
        /// <returns></returns>
        public override bool Equals(object o)
        {
            if (o is Quaternion q)
            {
                //return ( this.W == q.W ) && ( this.X == q.X ) && ( this.Y == q.Y ) && ( this.Z == q.Z );
                return (Math.Abs(this.W - q.W) <= MathExt.DBL_EPSILON) &&
                       (Math.Abs(this.X - q.X) <= MathExt.DBL_EPSILON) &&
                       (Math.Abs(this.Y - q.Y) <= MathExt.DBL_EPSILON) &&
                       (Math.Abs(this.Z - q.Z) <= MathExt.DBL_EPSILON);
            }
            return false;
        }

        #endregion

        #region Public Methods
        /// <summary>
        /// Get the magnitude of the Quaternion.
        /// </summary>
        /// <returns>The magnitude of the vector :  Sqrt(W*W + X*X + Y*Y + Z*Z).</returns>
        public double GetMagnitude()
        {
            return System.Math.Sqrt(W * W + X * X + Y * Y + Z * Z);
        }
        /// <summary>
        /// Get the squared magnitude of the Quaternion.
        /// </summary>
        /// <returns>The squared magnitude of the vector : (W*W + X*X + Y*Y + Z*Z).</returns>
        public double GetMagnitudeSquared()
        {
            return W * W + X * X + Y * Y + Z * Z;
        }


        /// <summary>
        /// Returns the derivative of the quaternion coresponding to the
        /// angular velocities PQR.
        /// </summary>
        public Quaternion GetQDot(Vector3D pqr)
        {
            double norm = GetMagnitude();
            if (norm == 0.0)
                return Quaternion.Zero;
            double rnorm = 1.0 / norm;

            Quaternion qDot = new Quaternion();

            qDot.W = -0.5 * (X * pqr.X + Y * pqr.Y + Z * pqr.Z);
            qDot.X = 0.5 * (W * pqr.X + Y * pqr.Z - Z * pqr.Y);
            qDot.Y = 0.5 * (W * pqr.Y + Z * pqr.X - X * pqr.Z);
            qDot.Z = 0.5 * (W * pqr.Z + X * pqr.Y - Y * pqr.X);
            return rnorm * qDot;
        }

        /// <summary>
        /// Transformation matrix
        /// </summary>
        /// <returns>
        /// the transformation/rotation matrix
        /// corresponding to this quaternion rotation
        /// </returns>
        public Matrix3D GetTransformationMatrix()
        {
            ComputeDerived();
            return mT;
        }

        /// <summary>
        /// Transformation matrix
        /// </summary>
        /// <returns>
        /// the transformation/rotation matrix
        /// corresponding to this quaternion rotation
        /// </returns>
        public Matrix3D GetT() { ComputeDerived(); return mT; }

        /// <summary>
        /// Backward transformation matrix.
        /// </summary>
        /// <returns>
        /// inverse transformation/rotation matrix
        /// corresponding to this quaternion rotation
        /// </returns>
        public Matrix3D GetTInv() { ComputeDerived(); return mTInv; }

        /// <summary>
        /// Inverse transformation matrix
        /// </summary>
        /// <returns>
        /// the inverse transformation/rotation matrix
        /// corresponding to this quaternion rotation
        /// </returns>
        public Matrix3D GetInverseTransformationMatrix()
        {
            ComputeDerived();
            return mTInv;
        }


        /// <summary>
        /// Retrieves the Euler angles.
        /// units radians.
        /// </summary>
        /// <returns>
        /// the triad of euler angles corresponding to this quaternion rotation.
        /// units radians
        /// </returns>
        public Vector3D GetEuler()
        {
            ComputeDerived();
            return mEulerAngles;
        }

        /// <summary>
        /// Retrieves the Euler angles.
        /// units radians.
        /// </summary>
        /// <param name="i">the Euler angle index.</param>
        /// <returns>a reference to the i-th euler angles corresponding
        /// to this quaternion rotation.
        /// </returns>
        public double GetEuler(EulerAngles i)
        {
            ComputeDerived();
            return mEulerAngles[(int)i - 1];
        }

        /// <summary>
        /// Retrieves the Euler angles.
        /// units degrees
        /// </summary>
        /// <param name="i">the Euler angle index.</param>
        /// <returns>a reference to the i-th euler angles corresponding
        /// to this quaternion rotation.
        /// </returns>
        public double GetEulerDeg(EulerAngles i)
        {
            ComputeDerived();
            return Constants.radtodeg * mEulerAngles[(int)i - 1];
        }

        /// <summary>
        ///  Retrieves the Euler angle vector.
        ///  units degrees
        /// </summary>
        /// <returns>an Euler angle column vector corresponding
        /// to this quaternion rotation.
        /// </returns>
        public Vector3D GetEulerDeg()
        {
            ComputeDerived();
            return Constants.radtodeg * mEulerAngles;
        }

        /// <summary>
        /// Retrieves sine of the euler angles.
        /// </summary>
        /// <returns>
        /// the sine of the Euler angle theta (pitch attitude) corresponding
        /// to this quaternion rotation.
        /// </returns>
        public Vector3D GetSinEuler()
        {
            ComputeDerived();
            return mEulerSines;
        }

        /// <summary>
        /// Retrieves sine of the given euler angle.
        /// </summary>
        /// <param name="i">the sine of the Euler angle theta (pitch attitude) corresponding
        /// to this quaternion rotation.</param>
        /// <returns></returns>
        public double GetSinEuler(EulerAngles i)
        {
            ComputeDerived();
            return mEulerSines[(int)i - 1];
        }

        /// <summary>
        /// Retrieves cosine of the euler angles.
        /// </summary>
        /// <returns>
        /// the sine of the Euler angle theta (pitch attitude) corresponding
        /// to this quaternion rotation.
        /// </returns>
        public Vector3D GetCosEuler()
        {
            ComputeDerived();
            return mEulerCosines;
        }

        /// <summary>
        /// Retrieves cosine of the given euler angle.
        /// </summary>
        /// <param name="i">the sine of the Euler angle theta (pitch attitude) corresponding
        ///  to this quaternion rotation.</param>
        /// <returns></returns>
        public double GetCosEuler(EulerAngles i)
        {
            ComputeDerived();
            return mEulerCosines[(int)i - 1];
        }

        /// <summary>
        /// Inverse self.
        /// Applies to non-zero Quaternions.
        /// </summary>
        public void Inverse()
        {
            double Norm = GetMagnitude();
            if (Norm > 0.0f)
            {
                double InvNorm = 1.0f / Norm;
                W *= InvNorm;
                X *= -InvNorm;
                Y *= -InvNorm;
                Z *= -InvNorm;
            }
            else
            {
                throw new QuaternionNotInvertibleException("Quaternion " + this.ToString() + " is not invertable");
            }
        }
        /// <summary>
        /// Inverse self.
        /// Apply to unit-length quaternion only.
        /// </summary>
        public void UnitInverse()
        {
            Debug.Assert(GetMagnitude() == 1);

            X = -X;
            Y = -Y;
            Z = -Z;
        }

        /// <summary>
        /// Create a new quaternion that is the transpose of self.
        /// </summary>
        public Quaternion GetInverse()
        {
            Quaternion q = new Quaternion(this);
            q.Inverse();
            return q;
        }

        /// <summary>
        /// Create a new quaternion that is the transpose of self.
        /// Note : Can be used only on unit quaternions.
        /// </summary>
        public Quaternion GetUnitInverse()
        {
            Quaternion q = new Quaternion(this);
            q.UnitInverse();
            return q;
        }

        /// <summary>
        /// Quaternion exponential
        /// Calculate the unit quaternion which is the result of the exponentiation of
        /// the vector 'omega'.
        /// </summary>
        /// <param name="omega">rotation velocity</param>
        /// <returns></returns>
        public static Quaternion QExp(Vector3D omega)
        {
            Quaternion qexp = new Quaternion();
            double angle = omega.Magnitude();
            double sina_a = angle > 0.0 ? Math.Sin(angle) / angle : 1.0;

            qexp.W = Math.Cos(angle);
            qexp.X = omega.X * sina_a;
            qexp.Y = omega.Y * sina_a;
            qexp.Z = omega.Z * sina_a;

            return qexp;
        }

        /// <summary>
        /// Gets a 3x3 rotation matrix from this Quaternion.
        /// </summary>
        /// <returns></returns>
        public Matrix3D ToRotationMatrix()
        {
            Matrix3D rotation = new Matrix3D();

            double tx = 2.0f * this.X;
            double ty = 2.0f * this.Y;
            double tz = 2.0f * this.Z;
            double twx = tx * this.W;
            double twy = ty * this.W;
            double twz = tz * this.W;
            double txx = tx * this.X;
            double txy = ty * this.X;
            double txz = tz * this.X;
            double tyy = ty * this.Y;
            double tyz = tz * this.Y;
            double tzz = tz * this.Z;

            rotation.M11 = 1.0f - (tyy + tzz);
            rotation.M12 = txy - twz;
            rotation.M13 = txz + twy;
            rotation.M21 = txy + twz;
            rotation.M22 = 1.0f - (txx + tzz);
            rotation.M23 = tyz - twx;
            rotation.M31 = txz - twy;
            rotation.M32 = tyz + twx;
            rotation.M33 = 1.0f - (txx + tyy);

            return rotation;
        }


        /// <summary>
        ///    
        /// </summary>
        /// <param name="angle"></param>
        /// <param name="axis"></param>
        /// <returns></returns>
        public void ToAngleAxis(ref double angle, ref Vector3D axis)
        {
            // The quaternion representing the rotation is
            //   q = cos(A/2)+sin(A/2)*(x*i+y*j+z*k)

            double sqrLength = X * X + Y * Y + Z * Z;

            if (sqrLength > 0.0f)
            {
                angle = 2.0f * Math.Acos(W);
                double invLength = 1.0 / Math.Sqrt(sqrLength);
                axis.X = X * invLength;
                axis.Y = Y * invLength;
                axis.Z = Z * invLength;
            }
            else
            {
                angle = 0.0f;
                axis.X = 1.0f;
                axis.Y = 0.0f;
                axis.Z = 0.0f;
            }
        }

        /// <summary>
        /// Normalize the vector to have the Magnitude() == 1.0. If the vector
        /// is equal to zero it is left untouched.
        /// </summary>
        public void Normalize()
        {
            // Note: this does not touch the cache
            // since it does not change the orientation ...

            double norm = GetMagnitude();
            if (norm == 0.0 || Math.Abs(norm - 1.000) < 1e-10)
                return;

            double rnorm = 1.0 / norm;
            this.W *= rnorm;
            this.X *= rnorm;
            this.Y *= rnorm;
            this.Z *= rnorm;
        }

        /// <summary>
        /// Computation of derived values.
        /// This function checks if the derived values like euler angles and
        /// transformation matrices are already computed. If so, it
        /// returns. If they need to be computed the real worker routine
        /// <code>ComputeDerivedUnconditional() </code>
        /// is called.
        /// This function is inlined to avoid function calls in the fast path.
        /// </summary>
        private void ComputeDerived()
        {
            if (!cache)
                ComputeDerivedUnconditional();
        }

        // Compute the derived values if required ...
        private void ComputeDerivedUnconditional()
        {
            cache = true;

            //// First normalize the 4-vector
            //double norm = GetMagnitude();
            //if (norm == 0.0)
            //    return;

            //double rnorm = 1.0 / norm;
            //double q1 = rnorm * W;
            //double q2 = rnorm * X;
            //double q3 = rnorm * Y;
            //double q4 = rnorm * Z;
            double q1 = W;
            double q2 = X;
            double q3 = Y;
            double q4 = Z;

            // Now compute the transformation matrix.
            double q1q1 = q1 * q1;
            double q2q2 = q2 * q2;
            double q3q3 = q3 * q3;
            double q4q4 = q4 * q4;
            double q1q2 = q1 * q2;
            double q1q3 = q1 * q3;
            double q1q4 = q1 * q4;
            double q2q3 = q2 * q3;
            double q2q4 = q2 * q4;
            double q3q4 = q3 * q4;

            mT.M11 = q1q1 + q2q2 - q3q3 - q4q4;
            mT.M12 = 2.0 * (q2q3 + q1q4);
            mT.M13 = 2.0 * (q2q4 - q1q3);
            mT.M21 = 2.0 * (q2q3 - q1q4);
            mT.M22 = q1q1 - q2q2 + q3q3 - q4q4;
            mT.M23 = 2.0 * (q3q4 + q1q2);
            mT.M31 = 2.0 * (q2q4 + q1q3);
            mT.M32 = 2.0 * (q3q4 - q1q2);
            mT.M33 = q1q1 - q2q2 - q3q3 + q4q4;

            // Since this is an orthogonal matrix, the inverse is simply
            // the transpose.
            mTInv = mT;
            mTInv.Transpose();

            // Compute the Euler-angles
            mEulerAngles = mT.GetEuler();


            // FIXME: may be one can compute those values easier ???
            mEulerSines.Phi = Math.Sin(mEulerAngles.Phi);
            mEulerSines.Theta = -mT.M13;
            mEulerSines.Psi = Math.Sin(mEulerAngles.Psi);
            mEulerCosines.Phi = Math.Cos(mEulerAngles.Phi);
            mEulerCosines.Theta = Math.Cos(mEulerAngles.Theta);
            mEulerCosines.Psi = Math.Cos(mEulerAngles.Psi);
        }

        #endregion

        #region Quaternion Arithmetics
        /// <summary>
        /// Add Quaternion to self.
        /// </summary>
        /// <param name="q"></param>
        public void Add(Quaternion q)
        {
            W += q.W;
            X += q.X;
            Y += q.Y;
            Z += q.Z;
            this.cache = false;
        }

        /// <summary>
        /// Subtract Quaternion from self.
        /// </summary>
        /// <param name="q"></param>
        public void Subtract(Quaternion q)
        {
            this.W -= q.W;
            this.X -= q.X;
            this.Y -= q.Y;
            this.Z -= q.Z;
            this.cache = false;
        }

        /// <summary>
        /// Multiply self by Quaternion.
        /// Note that this operation is NOT commutative.
        /// </summary>
        /// <param name="q"></param>
        public void Multiply(Quaternion q)
        {
            this.W = W * q.W - X * q.X - Y * q.Y - Z * q.Z;
            this.X = W * q.X + X * q.W + Y * q.Z - Z * q.Y;
            this.Y = W * q.Y + Y * q.W + Z * q.X - X * q.Z;
            this.Z = W * q.Z + Z * q.W + X * q.Y - Y * q.X;
            this.cache = false;
        }

        /// <summary>
        /// Multiply self by a scalar.
        /// </summary>
        /// <param name="f"></param>
        public void Multiply(double f)
        {
            this.W *= f;
            this.X *= f;
            this.Y *= f;
            this.Z *= f;
            this.cache = false;
        }

        /// <summary>
        /// Divide self by a scalar.
        /// </summary>
        /// <param name="f"></param>
        public void Divide(double f)
        {
            if (f == 0)
            {
                throw new DivideByZeroException("Dividing quaternion by zero");
            }

            this.W /= f;
            this.X /= f;
            this.Y /= f;
            this.Z /= f;
            this.cache = false;
        }

        /// <summary>
        /// Calculate the dot product of two vectors.
        /// </summary>
        /// <param name="a">The first vector for the operation.</param>
        /// <param name="b">The second vector for the operation.</param>
        /// <returns></returns>
        public static double Dot(Quaternion a, Quaternion b)
        {
            return a.W * b.W + a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        }
        #endregion

        #region Operators
        /// <summary>
        /// Checks if the two quternions are equal.
        /// </summary>
        /// <param name="a">The first of two quaternions to compare.</param>
        /// <param name="b">The second of two quaternions to compare.</param>
        /// <returns></returns>
        public static bool operator ==(Quaternion a, Quaternion b)
        {
            if (Object.Equals(a, null) == true)
            {
                return Object.Equals(b, null);
            }

            if (Object.Equals(b, null) == true)
            {
                return Object.Equals(a, null);
            }

            return (a.X == b.X) && (a.Y == b.Y) && (a.Z == b.Z) && (a.W == b.W);
        }

        /// <summary>
        /// Checks if the two given quaternions are not equal.
        /// </summary>
        /// <param name="a">The first of two quaternions to compare.</param>
        /// <param name="b">The second of two quaternions to compare.</param>
        /// <returns></returns>
        public static bool operator !=(Quaternion a, Quaternion b)
        {
            if (Object.Equals(a, null) == true)
            {
                return !Object.Equals(b, null);
            }
            else if (Object.Equals(b, null) == true)
            {
                return !Object.Equals(a, null);
            }

            return (a.X != b.X) || (a.Y != b.Y) || (a.Z != b.Z) || (a.W != b.W);
        }

        /// <summary>
        /// Multiply a quaternion by a double value.
        /// </summary>
        /// <param name="f">The double value to use.</param>
        /// <param name="q">The quaternion to multiply.</param>
        /// <returns></returns>
        public static Quaternion operator *(double f, Quaternion q)
        {
            return new Quaternion(q.W * f,
                q.X * f,
                q.Y * f,
                q.Z * f);
        }

        /// <summary>
        /// Multiply a quaternion by a double value.
        /// </summary>
        /// <param name="f">The double value to use.</param>
        /// <param name="q">The quaternion to multiply.</param>
        /// <returns></returns>
        public static Quaternion operator *(Quaternion q, double f)
        {
            return new Quaternion(q.W * f,
                q.X * f,
                q.Y * f,
                q.Z * f);
        }

        /// <summary>
        /// Divides a quaternion by a double value.
        /// </summary>
        /// <param name="f">The double value to use.</param>
        /// <param name="q">The quaternion to use.</param>
        /// <returns></returns>
        public static Quaternion operator /(Quaternion q, double f)
        {
            if (f == 0)
            {
                throw new DivideByZeroException("can not divide a vector by zero");
            }
            return new Quaternion(q.W / f,
                q.X / f,
                q.Y / f,
                q.Z / f);
        }

        /// <summary>
        /// Adds two quaternions.
        /// </summary>
        /// <param name="a">The first of two quaternions to add.</param>
        /// <param name="b">The second of two quaternions to add.</param>
        /// <returns></returns>
        public static Quaternion operator +(Quaternion a, Quaternion b)
        {
            return new Quaternion(a.W + b.W,
                a.X + b.X,
                a.Y + b.Y,
                a.Z + b.Z);
        }

        /// <summary>
        /// Multiplication of two quaternions is like performing successive rotations.
        /// </summary>
        /// <param name="a">The first of two quaternions to be multiplied.</param>
        /// <param name="b">The second of two quaternions to be multiplied.</param>
        /// <returns>a quaternion representing Q, where Q = a * b</returns>
        public static Quaternion operator *(Quaternion a, Quaternion b)
        {
            double q0 = a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z;
            double q1 = a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y;
            double q2 = a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X;
            double q3 = a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W;

            return new Quaternion(q0, q1, q2, q3);
        }

        /// <summary>
        /// Substract two quaternions.
        /// </summary>
        /// <param name="a">The quaternions to substract from.</param>
        /// <param name="b">The quaternions to substract.</param>
        /// <returns></returns>
        public static Quaternion operator -(Quaternion a, Quaternion b)
        {
            return new Quaternion(a.W - b.W,
                a.X - b.X,
                a.Y - b.Y,
                a.Z - b.Z);
        }


        /// <summary>
        /// An index accessor ( [w, x, y, z] ).
        /// </summary>
        public double this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return W;
                    case 1:
                        return X;
                    case 2:
                        return Y;
                    case 3:
                        return Z;
                    default:
                        Debug.Fail("invalid index: " + index);
                        return 0;
                }
            }
            set
            {
                switch (index)
                {
                    case 0:
                        W = value;
                        break;
                    case 1:
                        X = value;
                        break;
                    case 2:
                        Y = value;
                        break;
                    case 3:
                        Z = value;
                        break;
                    default:
                        Debug.Fail("invalid index: " + index);
                        break;
                }
                return;
            }
        }


        /// <summary>
        /// Converts the vector to an array of double.
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public static explicit operator double[] (Quaternion q)
        {
            double[] ret = new double[4];
            ret[0] = q.W;
            ret[1] = q.X;
            ret[2] = q.Y;
            ret[3] = q.Z;
            return ret;
        }

        #endregion

        #region Private variables

        private bool cache = false;

        /** This stores the transformation matrices.  */
        private Matrix3D mT;
        private Matrix3D mTInv;

        /** The cached euler angles.  */
        private Vector3D mEulerAngles;

        /** The cached sines and cosines of the euler angles.  */
        private Vector3D mEulerSines;
        private Vector3D mEulerCosines;

        #endregion

    }

    /// <exception cref="System.ApplicationException">Thrown when trying to invert an uninvertible quaternion.</exception>
    [Serializable]
    public class QuaternionNotInvertibleException : ApplicationException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QuaternionNotInvertibleException"/> class.
        /// </summary>
        public QuaternionNotInvertibleException() : base() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="QuaternionNotInvertibleException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">A message that describes the error.</param>
        public QuaternionNotInvertibleException(string message) : base(message) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="QuaternionNotInvertibleException"/> class 
        /// with a specified error message and a reference to the inner exception that is 
        /// the cause of this exception.
        /// </summary>
        /// <param name="message">A message that describes the error.</param>
        /// <param name="inner">
        /// The exception that is the cause of the current exception. 
        /// If the innerException parameter is not a null reference, the current exception is raised 
        /// in a catch block that handles the inner exception.
        /// </param>
        public QuaternionNotInvertibleException(string message, Exception inner) : base(message, inner) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="QuaternionNotInvertibleException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
        protected QuaternionNotInvertibleException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
