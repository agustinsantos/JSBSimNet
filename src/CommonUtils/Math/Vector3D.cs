#region Sharp3D.Math, Copyright(C) 2003-2004 Eran Kampf, Licensed under LGPL.
//	Sharp3D.Math math library
//	Copyright (C) 2003-2004  
//	Eran Kampf
//	tentacle@zahav.net.il
//	http://tentacle.flipcode.com
//
//	This library is free software; you can redistribute it and/or
//	modify it under the terms of the GNU Lesser General Public
//	License as published by the Free Software Foundation; either
//	version 2.1 of the License, or (at your option) any later version.
//
//	This library is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//	Lesser General Public License for more details.
//
//	You should have received a copy of the GNU Lesser General Public
//	License along with this library; if not, write to the Free Software
//	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
#endregion
namespace CommonUtils.MathLib
{
    using System;
    using System.Diagnostics;
    using System.Runtime.Serialization;

    /// <summary>
    /// Vector class for 3D.
    /// </summary>
    [Serializable]
    public struct Vector3D : ISerializable, ICloneable
    {
        #region Public variables
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

        #region Construction
        /// <summary>
        /// Initializes a new instance of the <see cref="Vector3D"/> class using given coordinates.
        /// </summary>
        /// <param name="x">The vector's X coordinate.</param>
        /// <param name="y">The vector's Y coordinate.</param>
        /// <param name="z">The vector's Z coordinate.</param>
        public Vector3D(double x, double y, double z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Vector3D"/> class using given coordinates array.
        /// </summary>
        /// <param name="coordinates">An array of coordinate parameters.</param>
        public Vector3D(double[] coordinates)
        {
            Debug.Assert(coordinates != null);
            Debug.Assert(coordinates.Length >= 3);

            X = coordinates[0];
            Y = coordinates[1];
            Z = coordinates[2];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Vector3D"/> class using given coordinates from another vector.
        /// </summary>
        /// <param name="v">A 3D vector to assign values from.</param>
        public Vector3D(Vector3D v)
        {
            X = v.X;
            Y = v.Y;
            Z = v.Z;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Vector3D"/> class with serialized data.
        /// </summary>
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
        private Vector3D(SerializationInfo info, StreamingContext context)
        {
            X = info.GetDouble("X");
            Y = info.GetDouble("Y");
            Z = info.GetDouble("Z");
        }
        #endregion

        #region Constants
        /// <summary>
        /// 3D Zero vector.
        /// </summary>
        public static readonly Vector3D Zero = new Vector3D(0.0, 0.0, 0.0);
        /// <summary>
        /// 3D X Axis.
        /// </summary>
        public static readonly Vector3D XAxis = new Vector3D(1.0, 0.0, 0.0);
        /// <summary>
        /// 3D Y Axis.
        /// </summary>
        public static readonly Vector3D YAxis = new Vector3D(0.0, 1.0, 0.0);
        /// <summary>
        /// 3D Z Axis.
        /// </summary>
        public static readonly Vector3D ZAxis = new Vector3D(0.0, 0.0, 1.0);
        #endregion

        #region ISerializable Members
        /// <summary>
        /// Populates a <see cref="SerializationInfo"/> with the data needed to serialize the target object.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> to populate with data. </param>
        /// <param name="context">The destination (see <see cref="StreamingContext"/>) for this serialization.</param>
        //[SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter=true)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("X", this.X);
            info.AddValue("Y", this.Y);
            info.AddValue("Z", this.Z);
        }
        #endregion

        #region ICloneable Members
        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        object ICloneable.Clone()
        {
            return new Vector3D(this);
        }
        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public Vector3D Clone()
        {
            return new Vector3D(this);
        }
        #endregion

        #region Overrides
        /// <summary>
        /// Get the hashcode for this vector instance.
        /// </summary>
        /// <returns>Returns the hash code for this vector instance.</returns>
        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
        }
        /// <summary>
        /// Checks if a given vector equals to self.
        /// </summary>
        /// <param name="o">Object to check if equal to.</param>
        /// <returns></returns>
        public override bool Equals(object o)
        {
            if (o is Vector3D vec)
            {
                return (this.X == vec.X) && (this.Y == vec.Y) && (this.Z == vec.Z);
            }
            return false;
        }
        /// <summary>
        /// Convert Vector3D to a string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("{0}, {1}, {2}", X, Y, Z);
        }

        public string ToString(string formatDouble, IFormatProvider provider)
        {
            return X.ToString(formatDouble, provider) + ", " +
                    Y.ToString(formatDouble, provider) + ", " + Z.ToString(formatDouble, provider);
        }

        #endregion

        #region Public methods
        /// <summary>
        /// Scale the vector so that the magnitude is 1.
        /// </summary>
        public void Normalize()
        {
            double length = Magnitude();
            if (length == 0)
            {
                return;
                //throw new DivideByZeroException("Trying to normalize a vector of magnitude 0");
            }

            X /= length;
            Y /= length;
            Z /= length;
        }

        /// <summary>
        /// Get the magnitude of the vector.
        /// </summary>
        /// <returns>The magnitude of the vector : Sqrt( X*X + Y*Y + Z*Z ).</returns>
        public double Magnitude()
        {
            return System.Math.Sqrt(X * X + Y * Y + Z * Z);
        }

        /// <summary>
        /// Length of the vector in a coordinate axis plane.
        /// 
        /// </summary>
        /// <returns>Compute and return the euclidean norm of this vector projected into
        /// the coordinate axis plane idx1-idx2.</returns>
        public double GetMagnitude(int idx1, int idx2)
        {
            return (double)System.Math.Sqrt(Entry(idx1) * Entry(idx1) + Entry(idx2) * Entry(idx2));
        }
        /// <summary>
        /// Get the squared magnitude of the vector.
        /// </summary>
        /// <returns>The squared magnitude of the vector : ( X*X + Y*Y + Z*Z ).</returns>
        public double GetMagnitudeSquared()
        {
            return X * X + Y * Y + Z * Z;
        }

        /// <summary>
        /// Get a unit vector representation of the current vector.
        /// </summary>
        /// <returns>A unit representation of the current vector.</returns>
        public Vector3D GetUnit()
        {
            Vector3D vec = new Vector3D(this);
            vec.Normalize();
            return vec;
        }

        #endregion

        #region Vector arithmetic
        /// <summary>
        /// Add vector to self.
        /// </summary>
        /// <param name="value">The vector to add</param>
        public void Add(Vector3D value)
        {
            this.X += value.X;
            this.Y += value.Y;
            this.Z += value.Z;
        }

        /// <summary>
        /// Subtract vector from self.
        /// </summary>
        /// <param name="value">The vector to substract.</param>
        public void Subtract(Vector3D value)
        {
            this.X -= value.X;
            this.Y -= value.Y;
            this.Z -= value.Z;
        }

        /// <summary>
        /// Multiply self by a double value <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The double value to use.</param>
        public void Multiply(double value)
        {
            this.X *= value;
            this.Y *= value;
            this.Z *= value;
        }

        /// <summary>
        /// Multiply self by a double value <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The double value to use.</param>
        public void Divide(double value)
        {
            if (value == 0)
            {
                throw new DivideByZeroException("can not divide a vector by zero");
            }
            this.X /= value;
            this.Y /= value;
            this.Z /= value;
        }

        /// <summary>
        /// Calculate the dot product (i.e. inner product) of <paramref name="a"/> and <paramref name="b"/>.
        /// </summary>
        /// <param name="a">The first of two 3D vectors to dot.</param>
        /// <param name="b">The second of two 3D vectors to dot.</param>
        /// <returns>The dot product of <paramref name="v1"/> and <paramref name="v2"/>.</returns>
        public static double Dot(Vector3D a, Vector3D b)
        {
            return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        }

        /// <summary>
        /// Calculate the cross product (i.e. outer product) of <paramref name="a"/> and <paramref name="b"/>.
        /// The cross product is calculated using right-handed rule.
        /// When using left-handed rule (which some graphics APIs like DirectX require) the
        /// sign of the cross product should be changed.
        /// </summary>
        /// <param name="a">The first of two 3D vectors to cross.</param>
        /// <param name="b">The second of two 3D vectors to cross.</param>
        /// <returns>The cross product of the two given vectors.</returns>
        public static Vector3D Cross(Vector3D a, Vector3D b)
        {
            return new Vector3D(a.Y * b.Z - a.Z * b.Y,
                                a.Z * b.X - a.X * b.Z,
                                a.X * b.Y - a.Y * b.X);
        }

        /// <summary>
        /// Calculate the cross product (i.e. outer product) of <paramref name="a"/> and <paramref name="b"/>.
        /// The cross product is calculated using right-handed rule.
        /// When using left-handed rule (which some graphics APIs like DirectX require) the
        /// sign of the cross product should be changed.
        /// </summary>
        /// <param name="a">The first of two 3D vectors to cross.</param>
        /// <param name="b">The second of two 3D vectors to cross.</param>
        /// <returns>The cross product of the two given vectors.</returns>
        public static Vector3D operator *(Vector3D a, Vector3D b)
        {
            return Vector3D.Cross(a, b);
        }


        public static Vector3D MultiplyElements(Vector3D a, Vector3D b)
        {
            return new Vector3D(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
        }

        /// <summary>
        /// Calculate the cross product (i.e. outer product) of <paramref name="a"/> and <paramref name="b"/>.
        /// The cross product is calculated using right-handed rule.
        /// When using left-handed rule (which some graphics APIs like DirectX require) the
        /// sign of the cross operation result should be changed.
        /// </summary>
        /// <param name="a">The first of two 3D vectors to cross.</param>
        /// <param name="b">The second of two 3D vectors to cross.</param>
        /// <returns>The normalized cross product of the two given vectors.</returns>
        public static Vector3D UnitCross(Vector3D a, Vector3D b)
        {
            return Cross(a, b).GetUnit();
        }

        /// <summary>
        /// Linear interpolation between two vectors.
        /// </summary>
        /// <param name="a">First vector.</param>
        /// <param name="b">Second vector.</param>
        /// <param name="time">Interpolation time [0..1].</param>
        /// <returns>Interpolated vector.</returns>
        public static Vector3D Lerp(Vector3D a, Vector3D b, double time)
        {
            return new Vector3D(a.X + (b.X - a.X) * time,
                a.Y + (b.Y - a.Y) * time,
                a.Z + (b.Z - a.Z) * time);
        }
        #endregion

        #region Operators
        /// <summary>
        /// Checks if the two given vectors are equal.
        /// </summary>
        /// <param name="a">The first of two 3D vectors to compare.</param>
        /// <param name="b">The second of two 3D vectors to compare.</param>
        /// <returns><b>true</b> if the vectors are equal; otherwise, <b>false</b>.</returns>
        public static bool operator ==(Vector3D a, Vector3D b)
        {
            if (Object.Equals(a, null) == true)
            {
                return Object.Equals(b, null);
            }

            if (Object.Equals(b, null) == true)
            {
                return Object.Equals(a, null);
            }

            return (a.X == b.X) && (a.Y == b.Y) && (a.Z == b.Z);
        }

        /// <summary>
        /// Checks if the two given vectors are not equal.
        /// </summary>
        /// <param name="a">The first of two 3D vectors to compare.</param>
        /// <param name="b">The second of two 3D vectors to compare.</param>
        /// <returns><b>true</b> if the vectors are not equal; otherwise, <b>false</b>.</returns>
        public static bool operator !=(Vector3D a, Vector3D b)
        {
            if (Object.Equals(a, null) == true)
            {
                return !Object.Equals(b, null);
            }
            else if (Object.Equals(b, null) == true)
            {
                return !Object.Equals(a, null);
            }
            return !((a.X == b.X) && (a.Y == b.Y) && (a.Z == b.Z));
        }

        /// <summary>
        /// Checks if the vector on the left side of the operator is less than the vector on the right side.
        /// </summary>
        /// <param name="vec1">The first of two 3D vectors to check.</param>
        /// <param name="vec2">The second of two 3D vectors to check.</param>
        /// <returns><b>true</b> if <paramref name="vec1"/> is smaller than <paramref name="vec2"/>; otherwise, <b>false</b>.</returns>
        public static bool operator <(Vector3D vec1, Vector3D vec2)
        {
            return ((vec1.X < vec2.X) && (vec1.Y < vec2.Y) && (vec1.Z < vec2.Z));
        }
        /// <summary>
        /// Checks if the vector on the left side of the operator is less than or equal to the vector on the right side.
        /// </summary>
        /// <param name="vec1">The first of two 3D vectors to check.</param>
        /// <param name="vec2">The second of two 3D vectors to check.</param>
        /// <returns><b>true</b> if <paramref name="vec1"/> is smaller or equal to <paramref name="vec2"/>; otherwise, <b>false</b>.</returns>
        public static bool operator <=(Vector3D vec1, Vector3D vec2)
        {
            return ((vec1.X <= vec2.X) && (vec1.Y <= vec2.Y) && (vec1.Z <= vec2.Z));
        }
        /// <summary>
        /// Checks if the vector on the left side of the operator is greater than the vector on the right side.
        /// </summary>
        /// <param name="vec1">The first of two 3D vectors to check.</param>
        /// <param name="vec2">The second of two 3D vectors to check.</param>
        /// <returns><b>true</b> if <paramref name="vec1"/> is larger than <paramref name="vec2"/>; otherwise, <b>false</b>.</returns>
        public static bool operator >(Vector3D vec1, Vector3D vec2)
        {
            return ((vec1.X > vec2.X) && (vec1.Y > vec2.Y) && (vec1.Z > vec2.Z));
        }
        /// <summary>
        /// Checks if the vector on the left side of the operator is  greater than or equal to the vector on the right side.
        /// </summary>
        /// <param name="vec1">The first of two 3D vectors to check.</param>
        /// <param name="vec2">The second of two 3D vectors to check.</param>
        /// <returns><b>true</b> if <paramref name="vec1"/> is larger or equal to <paramref name="vec2"/>; otherwise, <b>false</b>.</returns>
        public static bool operator >=(Vector3D vec1, Vector3D vec2)
        {
            return ((vec1.X >= vec2.X) && (vec1.Y >= vec2.Y) && (vec1.Z >= vec2.Z));
        }

        /// <summary>
        /// Invert the direction of the vector.
        /// </summary>
        /// <param name="p">The vector to invert.</param>
        /// <returns>Result is ( -vec.x, -vec.y, -vec.z ).</returns>
        public static Vector3D operator -(Vector3D p)
        {
            return new Vector3D(-p.X, -p.Y, -p.Z);
        }

        /// <summary>
        /// Multiply vector <paramref name="vec"/> by a double value <paramref name="f"/>.
        /// </summary>
        /// <param name="f">The double value.</param>
        /// <param name="vec">The vector.</param>
        /// <returns>Result is ( vec.X*f, vec.Y*f, vec.Z*f ).</returns>
        public static Vector3D operator *(double f, Vector3D vec)
        {
            return new Vector3D(vec.X * f, vec.Y * f, vec.Z * f);
        }

        /// <summary>
        /// Multiply vector <paramref name="vec"/> by a double value <paramref name="f"/>.
        /// </summary>
        /// <param name="f">The double value.</param>
        /// <param name="vec">The vector.</param>
        /// <returns>Result is ( vec.X*f, vec.Y*f, vec.Z*f ).</returns>
        public static Vector3D operator *(Vector3D vec, double f)
        {
            return new Vector3D(vec.X * f, vec.Y * f, vec.Z * f);
        }

        /// <summary>
        /// Divide vector <paramref name="vec"/> by a double value <paramref name="f"/>.
        /// </summary>
        /// <param name="vec">The vector.</param>
        /// <param name="f">The double value.</param>
        /// <returns>Result is ( vec.X/f, vec.Y/f, vec.Z/f ).</returns>
        public static Vector3D operator /(Vector3D vec, double f)
        {
            if (f == 0)
            {
                throw new DivideByZeroException("can not divide a vector by zero");
            }
            return new Vector3D(vec.X / f, vec.Y / f, vec.Z / f);
        }
        /// <summary>
        /// Add two vectors.
        /// </summary>
        /// <param name="vec1">The first vector to add.</param>
        /// <param name="vec2">The second vector to add.</param>
        /// <returns>Result is ( vec1.X + vec2.X, vec1.Y + vec2.Y, vec1.Z + vec2.Z )</returns>
        public static Vector3D operator +(Vector3D vec1, Vector3D vec2)
        {
            return new Vector3D(vec1.X + vec2.X, vec1.Y + vec2.Y, vec1.Z + vec2.Z);
        }

        /// <summary>
        /// Subtract two vectors.
        /// </summary>
        /// <param name="vec1">The vector to substract from.</param>
        /// <param name="vec2">The vector to substract.</param>
        /// <returns>Result is ( vec1.X - vec2.X, vec1.Y - vec2.Y, vec1.Z - vec2.Z )</returns>
        public static Vector3D operator -(Vector3D vec1, Vector3D vec2)
        {
            return new Vector3D(vec1.X - vec2.X, vec1.Y - vec2.Y, vec1.Z - vec2.Z);
        }

        /// <summary>
        /// An index accessor ( [X, Y, Z] ).
        /// </summary>
        public double this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return X;
                    case 1:
                        return Y;
                    case 2:
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
                        X = value;
                        break;
                    case 1:
                        Y = value;
                        break;
                    case 2:
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
        /// Converts the vector to an array of doubles.
        /// </summary>
        /// <param name="value">The vector value to convert.</param>
        /// <returns>Result is an array of double values containing the vector coordinates.</returns>
        public static explicit operator double[] (Vector3D value)
        {
            double[] doublesArray = new double[3];
            doublesArray[0] = value.X;
            doublesArray[1] = value.Y;
            doublesArray[2] = value.Z;
            return doublesArray;
        }
        #endregion

        /// <summary>
        /// Checks if two given vectors are approximately equal.
        /// </summary>
        /// <param name="vec1">The first of two 3D vectors to compare.</param>
        /// <param name="vec2">The second of two 3D vectors to compare.</param>
        /// <returns><b>true</b> if the vectors are approximately equal; otherwise, <b>false</b>.</returns>
        public static bool ApproxEquals(Vector3D vec1, Vector3D vec2)
        {
            return (
                (System.Math.Abs(vec1.X - vec2.X) < Double.Epsilon) &&
                (System.Math.Abs(vec1.Y - vec2.Y) < Double.Epsilon) &&
                (System.Math.Abs(vec1.Z - vec2.Z) < Double.Epsilon));
        }
        /// <summary>
        /// Checks if two given vectors are approximately equal.
        /// </summary>
        /// <param name="vec1">The first of two 3D vectors to compare.</param>
        /// <param name="vec2">The second of two 3D vectors to compare.</param>
        /// <param name="epsilon">The epsilon value to use.</param>
        /// <returns><b>true</b> if the vectors are approximately equal; otherwise, <b>false</b>.</returns>
        public static bool ApproxEquals(Vector3D vec1, Vector3D vec2, double epsilon)
        {
            return (
                (System.Math.Abs(vec1.X - vec2.X) < epsilon) &&
                (System.Math.Abs(vec1.Y - vec2.Y) < epsilon) &&
                (System.Math.Abs(vec1.Z - vec2.Z) < epsilon));
        }



        ///////////////////////////////////////////////
        /// TODO. Review this desing. 
        //////////////////////////////////////////////

        private double Entry(int idx) { return this[idx]; }

        /// <summary>
        /// X coordinate.
        /// </summary>
        public double eX
        {
            get { return X; }
            set { X = value; }
        }

        /// <summary>
        /// Y coordinate.
        /// </summary>
        public double eY
        {
            get { return Y; }
            set { Y = value; }
        }

        /// <summary>
        /// Z coordinate.
        /// </summary>
        public double eZ
        {
            get { return Z; }
            set { Z = value; }
        }

        /// <summary>
        /// An index accessor ( [X, Y, Z] ).
        /// </summary>
        public double this[PositionType index]
        {
            get
            {
                switch (index)
                {
                    case PositionType.eX:
                        return X;
                    case PositionType.eY:
                        return Y;
                    case PositionType.eZ:
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
                    case PositionType.eX:
                        X = value;
                        break;
                    case PositionType.eY:
                        Y = value;
                        break;
                    case PositionType.eZ:
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
        /// P Rate.
        /// </summary>
        public double eP
        {
            get { return X; }
            set { X = value; }
        }

        /// <summary>
        /// Q Rate.
        /// </summary>
        public double eQ
        {
            get { return Y; }
            set { Y = value; }
        }

        /// <summary>
        /// R Rate.
        /// </summary>
        public double eR
        {
            get { return Z; }
            set { Z = value; }
        }

        /// <summary>
        /// An Rate accessor ( [P, Q, R] ).
        /// </summary>
        public double this[RateType index]
        {
            get
            {
                switch (index)
                {
                    case RateType.eP:
                        return X;
                    case RateType.eQ:
                        return Y;
                    case RateType.eR:
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
                    case RateType.eP:
                        X = value;
                        break;
                    case RateType.eQ:
                        Y = value;
                        break;
                    case RateType.eR:
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
        /// Local frame orientation Roll.
        /// </summary>
        public double Roll
        {
            get { return X; }
            set { X = value; }
        }

        /// <summary>
        ///Local frame orientation Pitch.
        /// </summary>
        public double Pitch
        {
            get { return Y; }
            set { Y = value; }
        }

        /// <summary>
        /// Local frame orientation Yaw.
        /// </summary>
        public double Yaw
        {
            get { return Z; }
            set { Z = value; }
        }


        /// <summary>
        /// Euler angles Phi
        /// </summary>
        public double Phi
        {
            get { return X; }
            set { X = value; }
        }

        /// <summary>
        ///Euler angles Theta
        /// </summary>
        public double Theta
        {
            get { return Y; }
            set { Y = value; }
        }

        /// <summary>
        /// Euler angles Psi
        /// </summary>
        public double Psi
        {
            get { return Z; }
            set { Z = value; }
        }


        /// <summary>
        /// Velocities U
        /// </summary>
        public double U
        {
            get { return X; }
            set { X = value; }
        }

        /// <summary>
        ///Velocities V
        /// </summary>
        public double V
        {
            get { return Y; }
            set { Y = value; }
        }

        /// <summary>
        /// Velocities W
        /// </summary>
        public double W
        {
            get { return Z; }
            set { Z = value; }
        }

        // <summary>
        /// Angular velocity   P 
        /// </summary>
        public double P
        {
            get { return X; }
            set { X = value; }
        }

        /// <summary>
        /// Angular velocity   Q
        /// </summary>
        public double Q
        {
            get { return Y; }
            set { Y = value; }
        }

        /// <summary>
        /// Angular velocity   R
        /// </summary>
        public double R
        {
            get { return Z; }
            set { Z = value; }

        }

        /// <summary>
        /// Local frame position North
        /// </summary>
        public double North
        {
            get { return X; }
            set { X = value; }
        }

        /// <summary>
        /// Local frame position East
        /// </summary>
        public double East
        {
            get { return Y; }
            set { Y = value; }
        }

        /// <summary>
        /// Local frame position Down
        /// </summary>
        public double Down
        {
            get { return Z; }
            set { Z = value; }
        }
    }
}
