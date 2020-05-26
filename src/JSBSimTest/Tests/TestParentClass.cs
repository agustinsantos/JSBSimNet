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

namespace JSBSim.Tests
{
    using System;
    using CommonUtils.MathLib;
    using JSBSim;
    using NUnit.Framework;

    /// <summary>
    /// A base class for testing
    /// </summary>
    public class TestParentClass
    {
        public const double default_tolerance = 100.0 * MathExt.DBL_EPSILON;

        public void CheckLocation(Location loc, Vector3D vec, double delta = default_tolerance)
        {
            Quaternion qloc = new Quaternion(Quaternion.EulerAngles.eTht, -0.5 * Math.PI);
            Quaternion q = new Quaternion();
            double r = vec.Magnitude();

            Assert.AreEqual(vec.X, loc[1], r * delta);
            Assert.AreEqual(vec.Y, loc[2], r * delta);
            Assert.AreEqual(vec.Z, loc[3], r * delta);
            Assert.AreEqual(r, loc.Radius, r * delta);

            vec.Normalize();
            double lon = Math.Atan2(vec.Y, vec.X);
            double lat = Math.Asin(vec.Z);

            Assert.AreEqual(lon, loc.Longitude, delta);
            Assert.AreEqual(lat, loc.Latitude, delta);
            Assert.AreEqual(Math.Sin(lon), loc.SinLongitude, delta);
            Assert.AreEqual(Math.Cos(lon), loc.CosLongitude, delta);
            Assert.AreEqual(Math.Sin(lat), loc.SinLatitude, delta);
            Assert.AreEqual(Math.Cos(lat), loc.CosLatitude, delta);
            Assert.AreEqual(Math.Tan(lat), loc.TanLatitude, delta);

            q = new Quaternion(0.0, -lat, lon);
            Matrix3D m = (q * qloc).GetTransformationMatrix();
            AssertMatrixEqual(m, loc.GetTec2l());
            AssertMatrixEqual(m.GetTranspose(), loc.GetTl2ec());
        }
        public void AssertVectorEqual(Vector3D v1, Vector3D v2, double delta = default_tolerance)
        {
            Assert.AreEqual(v1.X, v2.X, delta);
            Assert.AreEqual(v1.Y, v2.Y, delta);
            Assert.AreEqual(v1.Z, v2.Z, delta);
        }
        public void AssertMatrixEqual(Matrix3D x, Matrix3D y, double delta = default_tolerance)
        {
            Assert.AreEqual(x.M11, y.M11, delta);
            Assert.AreEqual(x.M12, y.M12, delta);
            Assert.AreEqual(x.M13, y.M13, delta);
            Assert.AreEqual(x.M21, y.M21, delta);
            Assert.AreEqual(x.M22, y.M22, delta);
            Assert.AreEqual(x.M23, y.M23, delta);
            Assert.AreEqual(x.M31, y.M31, delta);
            Assert.AreEqual(x.M32, y.M32, delta);
            Assert.AreEqual(x.M33, y.M33, delta);
        }
        public void AssertMatrixIsIdentity(Matrix3D x, double delta = default_tolerance)
        {
            Assert.AreEqual(x.M11, 1.0, delta);
            Assert.AreEqual(x.M12, 0.0, delta);
            Assert.AreEqual(x.M13, 0.0, delta);
            Assert.AreEqual(x.M21, 0.0, delta);
            Assert.AreEqual(x.M22, 1.0, delta);
            Assert.AreEqual(x.M23, 0.0, delta);
            Assert.AreEqual(x.M31, 0.0, delta);
            Assert.AreEqual(x.M32, 0.0, delta);
            Assert.AreEqual(x.M33, 1.0, delta);
        }
    }
}
