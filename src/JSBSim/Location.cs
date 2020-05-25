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

namespace JSBSim
{
    using System;

    // Import log4net classes.
    using log4net;

    using CommonUtils.MathLib;
    using System.Collections.Generic;

    /// <summary>
    /// Location holds an arbitrary location in the Earth centered Earth fixed
    /// reference frame(ECEF). The coordinate frame ECEF has its center in the
    /// middle of the earth.The X-axis points from the center of the Earth towards
    /// a location with zero latitude and longitude on the Earth surface.The Y-axis
    /// points from the center of the Earth towards a location with zero latitude
    /// and 90 deg East longitude on the Earth surface.The Z-axis points from the
    /// Earth center to the geographic north pole.
    /// 
    /// This class provides access functions to set and get the location as either
    /// the simple X, Y and Z values in ft or longitude/latitude and the radial
    /// distance of the location from the Earth center.
    /// 
    /// It is common to associate a parent frame with a location.This frame is
    /// usually called the local horizontal frame or simply the local frame.It is
    /// also called the NED frame (North, East, Down), as well as the Navigation
    /// frame.This frame has its X/Y plane parallel to the surface of the Earth
    /// (with the assumption of a spherical Earth). The X-axis points towards north,
    /// the Y-axis points east and the Z-axis points to the center of the Earth.
    ///
    /// Since the local frame is determined by the location (and NOT by the
    /// orientation of the vehicle IN any frame), this class also provides the
    /// rotation matrices required to transform from the Earth centered(ECEF) frame
    /// to the local horizontal frame and back.This class "owns" the
    /// transformations that go from the ECEF frame to and from the local frame.
    /// Again, this is because the ECEF, and local frames do not involve the actual
    /// orientation of the vehicle - only the location on the Earth surface.There
    /// are conversion functions for conversion of position vectors given in the one
    /// frame to positions in the other frame.
    /// The Earth centered reference frame is NOT an inertial frame since it rotates
    /// with the Earth.
    /// 
    /// The cartesian coordinates (X, Y, Z) in the Earth centered frame are the master
    /// values. All other values are computed from these master values and are
    /// cached as long as the location is changed by access through a non-const
    /// member function. Values are cached to improve performance. It is best
    /// practice to work with a natural set of master values.Other parameters that
    /// are derived from these master values are calculated only when needed, and IF
    /// they are needed and calculated, then they are cached (stored and remembered)
    /// so they do not need to be re-calculated until the master values they are
    /// derived from are themselves changed(and become stale).
    ///
    ///  Accuracy and round off
    /// 
    ///  Given,
    /// - that we model a vehicle near the Earth
    /// - that the Earth surface radius is about 2*10^7, ft
    /// - that we use double values for the representation of the location
    ///
    /// we have an accuracy of about
    /// 
    /// 1e-16*2e7ft/1 = 2e-9 ft
    /// 
    /// left.This should be sufficient for our needs. Note that this is the same
    /// relative accuracy we would have when we compute directly with
    /// lon/lat/radius.For the radius value this is clear.For the lon/lat pair
    /// this is easy to see.Take for example KSFO located at about 37.61 deg north
    ///  122.35 deg west, which corresponds to 0.65642 rad north and 2.13541 rad
    /// west. Both values are of magnitude of about 1. But 1 ft corresponds to about
    /// 1/(2e7*2* pi) = 7.9577e-09 rad.So the left accuracy with this representation
    ///  is also about 1*1e-16/7.9577e-09 = 1.2566e-08 which is of the same magnitude
    /// as the representation chosen here.
    /// 
    /// The advantage of this representation is that it is a linear space without
    /// 
    /// singularities.The singularities are the north and south pole and most
    /// 
    /// notably the non-steady jump at -pi to pi.It is harder to track this jump
    /// correctly especially when we need to work with error norms and derivatives
    /// of the equations of motion within the time-stepping code. Also, the rate of
    /// change is of the same magnitude for all components in this representation
    /// which is an advantage for numerical stability in implicit time-stepping.
    /// 
    /// Note: The latitude is a GEOCENTRIC value.FlightGear converts latitude to a
    ///  geodetic value and uses that. In order to get best matching relative to a
    /// map, geocentric latitude must be converted to geodetic.
    /// 
    ///  see Stevens and Lewis, "Aircraft Control and Simulation", Second edition
    /// 
    /// see W. C.Durham "Aircraft Dynamics & Control", section 2.2
    /// 
    /// this code is based on FGLocation class written by Jon S. Berndt, Mathias Froehlich
    /// </summary>
    public class Location : ICloneable
    {
        /// <summary>
        /// Define a static logger variable so that it references the
        ///	Logger instance.
        /// 
        /// NOTE that using System.Reflection.MethodBase.GetCurrentMethod().DeclaringType
        /// is equivalent to typeof(LoggingExample) but is more portable
        /// i.e. you can copy the code directly into another class without
        /// needing to edit the code.
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Location()
        {
            mECLoc = new Vector3D(1.0, 0.0, 0.0);
            mCacheValid = false;

            e2 = c = 0.0;
            a = ec = ec2 = 1.0;

            mLon = mLat = mRadius = 0.0;
            mGeodLat = GeodeticAltitude = 0.0;

            mTl2ec = new Matrix3D();
            mTec2l = new Matrix3D();
        }

        /// <summary>
        /// Constructor to initialize the location with the cartesian coordinates
        /// (X, Y, Z) contained in the input Vector3D. Distances are in feet,
        /// the position is expressed in the ECEF frame.
        /// </summary>
        /// <param name="lv">vector that contain the cartesian coordinates</param>
        public Location(Vector3D lv)
        {
            mECLoc = lv;
            mCacheValid = false;

            e2 = c = 0.0;
            a = ec = ec2 = 1.0;

            mLon = mLat = mRadius = 0.0;
            mGeodLat = GeodeticAltitude = 0.0;

            mTl2ec = new Matrix3D();
            mTec2l = new Matrix3D();
        }

        /// <summary>
        /// Constructor to set the longitude, latitude and the distance
        /// from the center of the earth.
        /// </summary>
        /// <param name="lon">longitude</param>
        /// <param name="lat">GEOCENTRIC latitude</param>
        /// <param name="radius">distance from center of earth to vehicle in feet</param>
        public Location(double lon, double lat, double radius)
        {
            mCacheValid = false;
            e2 = c = 0.0;
            a = ec = ec2 = 1.0;

            mLon = mLat = mRadius = 0.0;
            mGeodLat = GeodeticAltitude = 0.0;

            mTl2ec = new Matrix3D();
            mTec2l = new Matrix3D();

            double sinLat = Math.Sin(lat);
            double cosLat = Math.Cos(lat);
            double sinLon = Math.Sin(lon);
            double cosLon = Math.Cos(lon);
            mECLoc = new Vector3D(radius * cosLat * cosLon,
                                  radius * cosLat * sinLon,
                                  radius * sinLat);
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="l"></param>
        public Location(Location l)
        {
            mECLoc = l.mECLoc; mCacheValid = l.mCacheValid;
            a = l.a;
            e2 = l.e2;
            c = l.c;
            ec = l.ec;
            ec2 = l.ec2;
            mEllipseSet = l.mEllipseSet;

            /*ag
             * if the cache is not valid, all of the following values are unset.
             * They will be calculated once ComputeDerivedUnconditional is called.
             * If unset, they may possibly contain NaN and could thus trigger floating
             * point exceptions.
             */
            if (!mCacheValid) return;

            mLon = l.mLon;
            mLat = l.mLat;
            mRadius = l.mRadius;

            mTl2ec = l.mTl2ec;
            mTec2l = l.mTec2l;

            mGeodLat = l.mGeodLat;
            GeodeticAltitude = l.GeodeticAltitude;
        }

        /// <summary>
        /// Get/set the longitude.
        /// 
        /// Return the longitude in rad of the location represented with this
        /// class instance. The returned values are in the range between
        /// <code>-pi <= lon <= pi</code>.
        /// Longitude is positive east and negative west.
        /// Sets the longitude of the location represented with this class
        /// instance to the value of the given argument. The value is meant
        /// to be in rad. The latitude and the radius value are preserved
        /// with this call with the exception of radius being equal to
        /// zero. If the radius is previously set to zero it is changed to be
        /// equal to 1.0 past this call. Longitude is positive east and negative west.
        /// </summary>
        public double Longitude
        {
            get { ComputeDerived(); return mLon; }
            set
            {
                double rtmp = mECLoc.GetMagnitude((int)PositionType.eX - 1, (int)PositionType.eY - 1);
                // Check if we have zero radius.
                // If so set it to 1, so that we can set a position
                if (0.0 == mECLoc.Magnitude())
                    rtmp = 1.0;

                // Fast return if we are on the north or south pole ...
                if (rtmp == 0.0)
                    return;

                mCacheValid = false;

                mECLoc.X = rtmp * Math.Cos(value);
                mECLoc.Y = rtmp * Math.Sin(value);
            }
        }

        /// <summary>
        /// Get the longitude.
        /// return the longitude in deg of the location represented with this
        /// class instance. The returned values are in the range between
        /// <code>-180 <= lon <= 180</code>.
        /// Longitude is positive east and negative west.
        /// </summary>
        public double LongitudeDeg { get { ComputeDerived(); return Constants.radtodeg * mLon; } }


        /// <summary>
        /// Get the sine of Longitude. 
        /// </summary>
        public double SinLongitude { get { ComputeDerived(); return -mTec2l.M21; } }

        /// <summary>
        /// Get the cosine of Longitude. 
        /// </summary>
        public double CosLongitude { get { ComputeDerived(); return mTec2l.M22; } }


        /// <summary>
        /// Get/Set the latitude.
        /// 
        /// Return the latitude in rad of the location represented with this
        /// class instance. The returned values are in the range between
        /// <code>-pi/2 <= lat <= pi/2</code>.
        /// Latitude is positive north and negative south.
        /// Sets the latitude of the location represented with this class
        /// instance to the value of the given argument. The value is meant
        /// to be in rad. The longitude and the radius value are preserved
        /// with this call with the exception of radius being equal to
        /// zero. If the radius is previously set to zero it is changed to be
        /// equal to 1.0 past this call.
        /// Latitude is positive north and negative south.
        /// The arguments should be within the bounds of -pi/2 <= lat <= pi/2.
        /// The behavior of this function with arguments outside this range is
        /// left as an exercise to the gentle reader ...
        /// </summary>
        public double Latitude
        {
            get { ComputeDerived(); return mLat; }
            set
            {
                mCacheValid = false;

                double r = mECLoc.Magnitude();
                if (r == 0.0)
                {
                    mECLoc.X = 1.0;
                    r = 1.0;
                }

                double rtmp = mECLoc.GetMagnitude((int)PositionType.eX - 1, (int)PositionType.eY - 1);
                if (rtmp != 0.0)
                {
                    double fac = r / rtmp * Math.Cos(value);
                    mECLoc.X *= fac;
                    mECLoc.Y *= fac;
                }
                else
                {
                    mECLoc.X = r * Math.Cos(value);
                    mECLoc.Y = 0.0;
                }
                mECLoc.Z = r * Math.Sin(value);
            }
        }

        /// <summary>
        ///  Get the geodetic latitude.
        ///  return the geodetic latitude in rad of the location represented with this
        ///  class instance. The returned values are in the range between
        ///  -pi/2 <= lon <= pi/2. Latitude is positive north and negative south.
        /// </summary>
        public double GeodLatitudeRad { get { ComputeDerived(); return mGeodLat; } }

        /// <summary>
        /// Get the latitude
        /// Return the latitude in deg of the location represented with this
        /// class instance. The returned values are in the range between
        /// <code>-90 <= lon <= 90</code>.
        /// Latitude is positive north and negative south.
        /// </summary>
        public double LatitudeDeg { get { ComputeDerived(); return Constants.radtodeg * mLat; } }

        /// <summary>
        /// Get the geodetic latitude in degrees.
        /// return the geodetic latitude in degrees of the location represented by
        /// this class instance. The returned value is in the range between
        /// -90 <= lon <= 90. Latitude is positive north and negative south.
        /// </summary>
        public double GeodLatitudeDeg { get { ComputeDerived(); return Constants.radtodeg * mGeodLat; } }

        /// <summary>
        /// Gets the geodetic altitude in feet.
        /// </summary>
        public double GeodAltitude { get { ComputeDerived(); return GeodeticAltitude; } }

        /// <summary>
        /// Get the sine of Latitude.
        /// </summary>
        public double SinLatitude { get { ComputeDerived(); return -mTec2l.M33; } }

        /// <summary>
        /// Get the cosine of Latitude.
        /// </summary>
        public double CosLatitude { get { ComputeDerived(); return mTec2l.M13; } }

        /// <summary>
        /// Get the Tan of Latitude
        /// </summary>
        public double TanLatitude
        {
            get
            {
                ComputeDerived();
                double cLat = mTec2l.M13;
                if (cLat == 0.0)
                    return 0.0;
                else
                    return -mTec2l.M33 / cLat;
            }
        }

        /// <summary>
        /// Get the sea level radius below the current location.
        /// </summary>
        public double SeaLevelRadius
        {
            get
            {
                if (!mCacheValid) ComputeDerivedUnconditional();

                double sinGeodLat = Math.Sin(mGeodLat);

                return a / Math.Sqrt(1 + e2 * sinGeodLat * sinGeodLat / ec2);
            }
        }

        /// <summary>
        /// Get/Set the distance from the center of the earth.
        /// return the distance of the location represented with this class
        /// instance to the center of the earth in ft. The radius value is
        /// always positive.
        /// Sets the radius of the location represented with this class
        /// instance to the value of the given argument. The value is meant
        /// to be in ft. The latitude and longitude values are preserved
        /// with this call with the exception of radius being equal to
        /// zero. If the radius is previously set to zero, latitude and
        /// longitude is set equal to zero past this call.
        /// The argument should be positive.
        /// The behavior of this function called with a negative argument is
        /// left as an exercise to the gentle reader ... 
        /// </summary>
        public double Radius
        {
            get { ComputeDerived(); return mRadius; }
            set
            {
                mCacheValid = false;

                double rold = mECLoc.Magnitude();
                if (rold == 0.0)
                    mECLoc.X = value;
                else
                    mECLoc *= value / rold;
            }
        }

        /// <summary>
        /// Sets the longitude, latitude and the distance from the center of the earth.
        /// </summary>
        /// <param name="lon">longitude in radians</param>
        /// <param name="lat">GEOCENTRIC latitude in radians</param>
        /// <param name="radius">distance from center of earth to vehicle in feet</param>
        public void SetPosition(double lon, double lat, double radius)
        {
            mCacheValid = false;

            double sinLat = Math.Sin(lat);
            double cosLat = Math.Cos(lat);
            double sinLon = Math.Sin(lon);
            double cosLon = Math.Cos(lon);

            mECLoc = new Vector3D(
                                radius * cosLat * cosLon,
                                radius * cosLat * sinLon,
                                radius * sinLat
                            );
        }

        /// <summary>
        /// Sets the longitude, latitude and the distance above the reference ellipsoid.
        /// </summary>
        /// <param name="lon">longitude in radians</param>
        /// <param name="lat">GEODETIC latitude in radians</param>
        /// <param name="height">distance above the reference ellipsoid to vehicle in feet</param>
        public void SetPositionGeodetic(double lon, double lat, double height)
        {
            //assert(mEllipseSet);
            mCacheValid = false;

            double slat = Math.Sin(lat);
            double clat = Math.Cos(lat);
            double RN = a / Math.Sqrt(1.0 - e2 * slat * slat);

            mECLoc.eX = (RN + height) * clat * Math.Cos(lon);
            mECLoc.eY = (RN + height) * clat * Math.Sin(lon);
            mECLoc.eZ = ((1 - e2) * RN + height) * slat;
        }


        /// <summary>
        /// Sets the semimajor and semiminor axis lengths for this planet.
        /// The eccentricity and flattening are calculated from the semimajor
        /// and semiminor axis lengths
        /// </summary>
        /// <param name="semimajor"></param>
        /// <param name="semiminor"></param>
        public void SetEllipse(double semimajor, double semiminor)
        {
            mCacheValid = false;
            mEllipseSet = true;

            a = semimajor;
            ec = semiminor / a;
            ec2 = ec * ec;
            e2 = 1.0 - ec2;
            c = a * e2;
        }


        /// <summary>
        /// Access the X entry of the vector.
        /// used internally to access the elements in a more convenient way.
        /// </summary>
        public double X
        {
            get { return mECLoc.X; }
            set { mCacheValid = false; mECLoc.X = value; }
        }

        /// <summary>
        /// Access the Y entry of the vector.
        /// used internally to access the elements in a more convenient way.
        /// </summary>
        public double Y
        {
            get { return mECLoc.Y; }
            set { mCacheValid = false; mECLoc.Y = value; }
        }

        /// <summary>
        /// Access the Z entry of the vector.
        /// used internally to access the elements in a more convenient way.
        /// </summary>
        public double Z
        {
            get { return mECLoc.Z; }
            set { mCacheValid = false; mECLoc.Z = value; }
        }

        /// <summary>
        /// Read access the entries of the vector.
        /// Indices are counted starting with 1.
        /// Note that the index given in the argument is unchecked.
        /// </summary>
        /// <param name="index">the component index</param>
        /// <returns>the value of the matrix entry at the given index.</returns>
        public double this[int idx]
        {
            get { return mECLoc[idx - 1]; }
            set { mCacheValid = false; mECLoc[idx - 1] = value; }
        }

        /// <summary>
        ///  Read access the entries of the vector.
        ///  Indices are counted starting with 1.
        ///  used internally to access the elements in a more convenient way.
        ///  Note that the index given in the argument is unchecked.
        /// </summary>
        /// <param name=""></param>
        /// <param name="idx">idx the component index.</param>
        /// <returns>the value of the matrix entry at the given index.</returns>
        public double Entry(int idx) { return mECLoc[idx - 1]; }


        /// <summary>
        /// Subtract two locations.
        /// </summary>
        /// <param name="vec1">The location to substract from.</param>
        /// <param name="vec2">The location to substract.</param>
        /// <returns>Result is ( vec1.X - vec2.X, vec1.Y - vec2.Y, vec1.Z - vec2.Z )</returns>
        public static Location operator -(Location vec1, Location vec2)
        {
            return new Location(vec1.mECLoc - vec2.mECLoc); ;
        }

        /// <summary>
        /// Add two locations.
        /// </summary>
        /// <param name="vec1">The first location to add.</param>
        /// <param name="vec2">The second location to add.</param>
        /// <returns>Result is ( vec1.X + vec2.X, vec1.Y + vec2.Y, vec1.Z + vec2.Z )</returns>
        public static Location operator +(Location vec1, Location vec2)
        {
            return new Location(vec1.mECLoc + vec2.mECLoc);
        }

        /// <summary>
        /// Multiply Location <paramref name="vec"/> by a double value <paramref name="f"/>.
        /// </summary>
        /// <param name="f">The double value.</param>
        /// <param name="loc">The Location.</param>
        /// <returns>Result is ( vec.X*f, vec.Y*f, vec.Z*f ).</returns>
        public static Location operator *(double f, Location loc)
        {
            return new Location(loc.mECLoc * f);
        }

        /// <summary>
        /// Multiply Location <paramref name="vec"/> by a double value <paramref name="f"/>.
        /// </summary>
        /// <param name="f">The double value.</param>
        /// <param name="loc">The Location.</param>
        /// <returns>Result is ( vec.X*f, vec.Y*f, vec.Z*f ).</returns>
        public static Location operator *(Location loc, double f)
        {
            return new Location(loc.mECLoc * f);
        }

        /// <summary>
        /// Computation of derived values.
        /// This function re-computes the derived values like lat/lon and
        /// transformation matrices. It does this unconditionally.
        /// </summary>
        private void ComputeDerivedUnconditional()
        {
            // The radius is just the Euclidean norm of the vector.
            mRadius = mECLoc.Magnitude();

            // The distance of the location to the y-axis, which is the axis
            // through the poles.
            double rxy = Math.Sqrt(mECLoc.X * mECLoc.X + mECLoc.Y * mECLoc.Y);

            // Compute the sin/cos values of the longitude
            double sinLon, cosLon;
            if (rxy == 0.0)
            {
                sinLon = 0.0;
                cosLon = 1.0;
            }
            else
            {
                sinLon = mECLoc.Y / rxy;
                cosLon = mECLoc.X / rxy;
            }

            // Compute the sin/cos values of the latitude
            double sinLat, cosLat;
            if (mRadius == 0.0)
            {
                sinLat = 0.0;
                cosLat = 1.0;
            }
            else
            {
                sinLat = mECLoc.Z / mRadius;
                cosLat = rxy / mRadius;
            }

            // Compute the longitude and latitude itself
            if (mECLoc.X == 0.0 && mECLoc.Y == 0.0)
                mLon = 0.0;
            else
                mLon = Math.Atan2(mECLoc.Y, mECLoc.X);

            if (rxy == 0.0 && mECLoc.Z == 0.0)
                mLat = 0.0;
            else
                mLat = Math.Atan2(mECLoc.Z, rxy);

            // Compute the transform matrices from and to the earth centered frame.
            // See Stevens and Lewis, "Aircraft Control and Simulation", Second Edition,
            // Eqn. 1.4-13, page 40. In Stevens and Lewis notation, this is C_n/e - the
            // orientation of the navigation (local) frame relative to the ECEF frame,
            // and a transformation from ECEF to nav (local) frame.
            mTec2l = new Matrix3D(-cosLon * sinLat, -sinLon * sinLat, cosLat,
                -sinLon, cosLon, 0.0,
                -cosLon * cosLat, -sinLon * cosLat, -sinLat);

            // In Stevens and Lewis notation, this is C_e/n - the
            // orientation of the ECEF frame relative to the nav (local) frame,
            // and a transformation from nav (local) to ECEF frame.

            mTl2ec = mTec2l.GetTranspose();

            // Calculate the geodetic latitude based on "Transformation from Cartesian to
            // geodetic coordinates accelerated by Halley's method", Fukushima T. (2006)
            // Journal of Geodesy, Vol. 79, pp. 689-693
            // Unlike I. Sofair's method which uses a closed form solution, Fukushima's
            // method is an iterative method whose convergence is so fast that only one
            // iteration suffices. In addition, Fukushima's method has a much better
            // numerical stability over Sofair's method at the North and South poles and
            // it also gives the correct result for a spherical Earth.
            if (mEllipseSet)
            {
                double s0 = Math.Abs(mECLoc.eZ);
                double zc = ec * s0;
                double c0 = ec * rxy;
                double c02 = c0 * c0;
                double s02 = s0 * s0;
                double a02 = c02 + s02;
                double a0 = Math.Sqrt(a02);
                double a03 = a02 * a0;
                double s1 = zc * a03 + c * s02 * s0;
                double c1 = rxy * a03 - c * c02 * c0;
                double cs0c0 = c * c0 * s0;
                double b0 = 1.5 * cs0c0 * ((rxy * s0 - zc * c0) * a0 - cs0c0);
                s1 = s1 * a03 - b0 * s0;
                double cc = ec * (c1 * a03 - b0 * c0);
                mGeodLat = MathExt.Sign(mECLoc.eZ) * Math.Atan(s1 / cc);
                double s12 = s1 * s1;
                double cc2 = cc * cc;
                GeodeticAltitude = (rxy * cc + s0 * s1 - a * Math.Sqrt(ec2 * s12 + cc2)) / Math.Sqrt(s12 + cc2);
            }

            // Mark the cached values as valid
            mCacheValid = true;
        }

        /// <summary>
        /// Computation of derived values
        /// This function checks if the derived values like lat/lon and
        /// transformation matrices are already computed. If so, it
        /// returns. If they need to be computed this is done here.
        /// </summary>
        private void ComputeDerived()
        {
            if (!mCacheValid)
                ComputeDerivedUnconditional();
        }


        /// <summary>
        /// Transform matrix from local horizontal to earth centered frame.
        /// Returns a copy of the rotation matrix of the transform from
        /// the local horizontal frame to the earth centered frame.
        /// </summary>
        public Matrix3D GetTl2ec() { ComputeDerived(); return mTl2ec; }

        /// <summary>
        /// Transform matrix from the earth centered to local horizontal frame.
        /// Returns a const reference to the rotation matrix of the transform from
        /// the earth centered frame to the local horizontal frame.
        /// </summary>
        public Matrix3D GetTec2l() { ComputeDerived(); return mTec2l; }

        /// <summary>
        ///  Get the geodetic distance between the current location and a given
        ///  location.This corresponds to the shortest distance between the two
        /// </summary>
        /// <param name="target_longitude">the target longitude</param>
        /// <param name="target_latitude">the target latitude</param>
        /// <returns>The geodetic distance between the two locations</returns>
        public double GetDistanceTo(double target_longitude, double target_latitude)
        {
            //  The calculations, below, implement the Haversine formulas to calculate
            //  heading and distance to a set of lat/long coordinates from the current
            //  position.
            //
            //  The basic equations are (lat1, long1 are source positions; lat2
            //  long2 are target positions):
            //
            //  R = earth’s radius
            //  Δlat = lat2 − lat1
            //  Δlong = long2 − long1
            //
            //  For the waypoint distance calculation:
            //
            //  a = sin²(Δlat/2) + cos(lat1)∙cos(lat2)∙sin²(Δlong/2)
            //  c = 2∙atan2(√a, √(1−a))
            //  d = R∙c
            double delta_lat_rad = target_latitude - this.Latitude;
            double delta_lon_rad = target_longitude - this.Longitude;

            double distance_a = Math.Pow(Math.Sin(0.5 * delta_lat_rad), 2.0)
                                + (this.CosLatitude * Math.Cos(target_latitude)
                                * (Math.Pow(Math.Sin(0.5 * delta_lon_rad), 2.0)));

            return 2.0 * this.Radius * Math.Atan2(Math.Sqrt(distance_a), Math.Sqrt(1.0 - distance_a));
        }

        /// <summary>
        /// Get the heading that should be followed from the current location to
        /// a given location along the shortest path.Earth curvature is
        ///  taken into account.
        /// </summary>
        /// <param name="target_longitude">the target longitude</param>
        /// <param name="target_latitude">the target latitude</param>
        /// <returns>The heading that should be followed to reach the targeted
        /// location along the shortest path
        /// </returns>
        public double GetHeadingTo(double target_longitude, double target_latitude)
        {
            //  The calculations, below, implement the Haversine formulas to calculate
            //  heading and distance to a set of lat/long coordinates from the current
            //  position.
            //
            //  The basic equations are (lat1, long1 are source positions; lat2
            //  long2 are target positions):
            //
            //  R = earth’s radius
            //  Δlat = lat2 − lat1
            //  Δlong = long2 − long1
            //
            //  For the heading angle calculation:
            //
            //  θ = atan2(sin(Δlong)∙cos(lat2), cos(lat1)∙sin(lat2) − sin(lat1)∙cos(lat2)∙cos(Δlong))

            double delta_lon_rad = target_longitude - this.Longitude;

            double Y = Math.Sin(delta_lon_rad) * Math.Cos(target_latitude);
            double X = this.CosLatitude * Math.Sin(target_latitude)
              - this.SinLatitude * Math.Cos(target_latitude) * Math.Cos(delta_lon_rad);

            double heading_to_waypoint_rad = Math.Atan2(Y, X);
            if (heading_to_waypoint_rad < 0) heading_to_waypoint_rad += 2.0 * Math.PI;

            return heading_to_waypoint_rad;
        }


        /// <summary>
        /// Conversion from Local frame coordinates to a location in the
        /// earth centered and fixed frame.
        /// </summary>
        /// <param name="lvec">Vector in the local horizontal coordinate frame</param>
        /// <returns>The location in the earth centered and fixed frame</returns>
        public Location LocalToLocation(Vector3D lvec)
        {
            ComputeDerived();
            return (Location)(mTl2ec * lvec + mECLoc);
        }

        /// <summary>
        /// Conversion from a location in the earth centered and fixed frame
        /// to local horizontal frame coordinates.
        /// </summary>
        /// <param name="ecvec">Vector in the earth centered and fixed frame</param>
        /// <returns>The vector in the local horizontal coordinate frame</returns>
        public Vector3D LocationToLocal(Vector3D ecvec)
        {
            ComputeDerived();
            return mTec2l * (ecvec - mECLoc);
        }

        /// <summary>
        /// This operator returns true if the ECEF location vectors for the two
        /// location objects are equal.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var location = obj as Location;
            return location != null &&
                   EqualityComparer<Vector3D>.Default.Equals(mECLoc, location.mECLoc);
        }
        public static bool operator ==(Location leftSide, Location rightSide)
        {
            if (Object.Equals(leftSide, null) == true)
            {
                return Object.Equals(rightSide, null);
            }

            if (Object.Equals(rightSide, null) == true)
            {
                return Object.Equals(leftSide, null);
            }

            return leftSide.mECLoc == rightSide.mECLoc;
        }

        public static bool operator !=(Location leftSide, Location rightSide)
        {
            return !(leftSide == rightSide);
        }
        public override int GetHashCode()
        {
            return -2012594870 + EqualityComparer<Vector3D>.Default.GetHashCode(mECLoc);
        }

        #region ICloneable Members
        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        object ICloneable.Clone()
        {
            return new Location(this);
        }
        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public Location Clone()
        {
            return new Location(this);
        }
        #endregion

        /// <summary>
        /// Converts the Vector3D to a location.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static explicit operator Location(Vector3D v)
        {
            Location loc = new Location(v);
            return loc;
        }

        /// <summary>
        /// Cast to a simple 3d vector
        /// </summary>
        /// <param name="x"></param>
        public static explicit operator Vector3D(Location loc)
        {
            return loc.mECLoc;
        }

        /// <summary>
        /// The coordinates in the earth centered frame. This is the master copy.
        /// The coordinate frame has its center in the middle of the earth.
        /// Its x-axis points from the center of the earth towards a
        /// location with zero latitude and longitude on the earths
        /// surface. The y-axis points from the center of the earth towards a
        /// location with zero latitude and 90deg longitude on the earths
        /// surface. The z-axis points from the earths center to the
        /// geographic north pole.
        /// see W. C. Durham "Aircraft Dynamics & Control", section 2.2
        /// 
        /// </summary>
        private Vector3D mECLoc;

        /** The cached lon/lat/radius values. */
        private double mLon;
        private double mLat;
        private double mRadius;
        private double mGeodLat;
        private double GeodeticAltitude;

        /** The cached rotation matrices from and to the associated frames. */
        private Matrix3D mTl2ec;
        private Matrix3D mTec2l;

        /* Terms for geodetic latitude calculation. Values are from WGS84 model */
        private double a;    // Earth semimajor axis in feet
        private double e2;   // Earth eccentricity squared
        private double c;
        private double ec;
        private double ec2;

        /// <summary>
        /// A data validity flag.
        /// This class implements caching of the derived values like the
        /// orthogonal rotation matrices or the lon/lat/radius values. For caching we
        /// carry a flag which signals if the values are valid or not.
        /// </summary>
        private bool mCacheValid = false;

        // Flag that checks that geodetic methods are called after SetEllipse() has
        // been called.
        private bool mEllipseSet = false;
    }
}
