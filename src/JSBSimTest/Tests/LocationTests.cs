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
    /// Some table Tests: load and access.
    /// </summary>
    [TestFixture]
    public class LocationTests : TestParentClass
    {
        private const double tolerance = default_tolerance; 

        [Test]
        public void TestConstructors01()
        {
            Location l0 = new Location();
            Assert.AreEqual(1.0, l0[1]);
            Assert.AreEqual(0.0, l0[2]);
            Assert.AreEqual(0.0, l0[3]);
            Assert.AreEqual(1.0, l0.Entry(1));
            Assert.AreEqual(0.0, l0.Entry(2));
            Assert.AreEqual(0.0, l0.Entry(3));
            Assert.AreEqual(0.0, l0.Longitude);
            Assert.AreEqual(0.0, l0.Latitude);
            Assert.AreEqual(0.0, l0.LongitudeDeg);
            Assert.AreEqual(0.0, l0.LatitudeDeg);
            Assert.AreEqual(1.0, l0.Radius);
            Assert.AreEqual(0.0, l0.SinLongitude);
            Assert.AreEqual(1.0, l0.CosLongitude);
            Assert.AreEqual(0.0, l0.SinLatitude);
            Assert.AreEqual(1.0, l0.CosLatitude);
            Assert.AreEqual(0.0, l0.TanLatitude);
        }

        [Test]
        public void TestSetEllipse01()
        {
            Location l0 = new Location();
            l0.SetEllipse(1.0, 1.0);
            Assert.AreEqual(0.0, l0.GeodLatitudeRad);
            Assert.AreEqual(0.0, l0.GeodLatitudeDeg);
            Assert.AreEqual(0.0, l0.GeodAltitude);
        }

        [Test]
        public void TestConstructors02()
        {
            double lat = -0.25 * Math.PI;
            double lon = Math.PI / 6.0;
            Location l = new Location(lon, lat, 1.0);
            Assert.AreEqual(lon, l.Longitude, tolerance);
            Assert.AreEqual(lat, l.Latitude, tolerance);
            Assert.AreEqual(1.0, l.Radius, tolerance);
            Assert.AreEqual(30.0, l.LongitudeDeg, tolerance);
            Assert.AreEqual(-45.0, l.LatitudeDeg, tolerance);
            Assert.AreEqual(0.5, l.SinLongitude, tolerance);
            Assert.AreEqual(0.5 * Math.Sqrt(3.0), l.CosLongitude, tolerance);
            Assert.AreEqual(-0.5 * Math.Sqrt(2.0), l.SinLatitude, tolerance);
            Assert.AreEqual(0.5 * Math.Sqrt(2.0), l.CosLatitude, tolerance);
            Assert.AreEqual(-1.0, l.TanLatitude, tolerance);
        }


        [Test]
        public void TestSetEllipse02()
        {
            double lat = -0.25 * Math.PI;
            double lon = Math.PI / 6.0;
            Location l = new Location(lon, lat, 1.0);
            l.SetEllipse(1.0, 1.0);
            Assert.AreEqual(lat, l.GeodLatitudeRad);
            Assert.AreEqual(-45, l.GeodLatitudeDeg);
            Assert.AreEqual(0.0, l.GeodAltitude, tolerance);
        }

        [Test]
        public void TestGetTl2ec01()
        {
            double lat = -0.25 * Math.PI;
            double lon = Math.PI / 6.0;
            Location l = new Location(lon, lat, 1.0);
            l.SetEllipse(1.0, 1.0);
            Quaternion qloc = new Quaternion(Quaternion.EulerAngles.eTht, -0.5 * Math.PI);
            Quaternion q = new Quaternion(0.0, -lat, lon);
            Matrix3D m = (q * qloc).GetTransformationMatrix();
            AssertMatrixEqual(m, l.GetTec2l(), tolerance);
            AssertMatrixEqual(m.GetTranspose(), l.GetTl2ec(), tolerance);
        }

        [Test]
        public void TestConstructors03()
        {
            Vector3D v = new Vector3D(1.0, 0.0, 1.0);
            Location lv1 = new Location(v);
            Assert.AreEqual(v.X, lv1[1]);
            Assert.AreEqual(v.Y, lv1[2]);
            Assert.AreEqual(v.Z, lv1[3]);
            Assert.AreEqual(0.0, lv1.Longitude, tolerance);
            Assert.AreEqual(0.25 * Math.PI, lv1.Latitude, tolerance);
            Assert.AreEqual(Math.Sqrt(2.0), lv1.Radius, tolerance);

            Quaternion qloc = new Quaternion(Quaternion.EulerAngles.eTht, -0.5 * Math.PI);
            Quaternion qlat = new Quaternion(Quaternion.EulerAngles.eTht, -lv1.Latitude);
            Matrix3D m = (qlat * qloc).GetTransformationMatrix();
            AssertMatrixEqual(m, lv1.GetTec2l(), tolerance);
            AssertMatrixEqual(m.GetTranspose(), lv1.GetTl2ec(), tolerance);
        }

        [Test]
        public void TestConstructors04()
        {
            Vector3D v = new Vector3D(1.0, 1.0, 0.0);
            Location lv2 = new Location(v);
            Assert.AreEqual(v.X, lv2[1]);
            Assert.AreEqual(v.Y, lv2[2]);
            Assert.AreEqual(v.Z, lv2[3]);
            Assert.AreEqual(0.25 * Math.PI, lv2.Longitude, tolerance);
            Assert.AreEqual(0.0, lv2.Latitude, tolerance);
            Assert.AreEqual(Math.Sqrt(2.0), lv2.Radius, tolerance);

            Quaternion qloc = new Quaternion(Quaternion.EulerAngles.eTht, -0.5 * Math.PI);
            Quaternion qlon = new Quaternion(Quaternion.EulerAngles.ePsi,  lv2.Longitude);
            Matrix3D m = (qlon * qloc).GetTransformationMatrix();
            AssertMatrixEqual(m, lv2.GetTec2l(), tolerance);
            AssertMatrixEqual(m.GetTranspose(), lv2.GetTl2ec(), tolerance);
        }

        [Test]
        public void TestConstructors05()
        {
            Vector3D v = new Vector3D(1.5, -2.0, 3.0);
            Location lv3 = new Location(v);

            CheckLocation(lv3, v);
        }

        [Test]
        public void TestCopyConstructor()
        {
            Vector3D v = new Vector3D(1.5, -2.0, 3.0);
            Location l = new Location(v);
            Location lv = new Location(l);

            Assert.AreEqual(l[1], lv[1], tolerance);
            Assert.AreEqual(l[2], lv[2], tolerance);
            Assert.AreEqual(l[3], lv[3], tolerance);

            CheckLocation(l, v);
            CheckLocation(lv, v);

            // Check that FGLocation use a copy of the values contained in the vector v
            // If a value of v is modified, then the FGLocation instances shall not be
            // affected.
            Vector3D v0 = v;
            v.Y = 1.0;
            Assert.AreEqual(l[1], lv[1], tolerance);
            Assert.AreEqual(-2.0, lv[2], tolerance);
            Assert.AreEqual(1.0, v.Y, tolerance);
            Assert.AreEqual(l[3], lv[3], tolerance);

            CheckLocation(l, v0);
            CheckLocation(lv, v0);

            // Check that the copy 'lv' is not altered if the FGLocation 'l' is modified
            l[2] = 1.0;
            CheckLocation(l, v);
            CheckLocation(lv, v0);

            // Check the copy constructor for an FGLocation with cached values.
            Location lv2 = new Location(l);

            Assert.AreEqual(l[1], lv2[1], tolerance);
            Assert.AreEqual(l[2], lv2[2], tolerance);
            Assert.AreEqual(l[3], lv2[3], tolerance);

            CheckLocation(lv2, v);
        }

        [Test]
        public void TestEquality()
        {
            Vector3D v = new Vector3D(1.5, -2.0, 3.0);
            Location l = new Location(v);
            Location lv = new Location(l);

            Assert.AreEqual(l, lv);

            for (int i = 1; i < 4; i++)
            {
                l = lv.Clone();
                l[i] = lv.Entry(i) + 1.0;
                Assert.AreNotEqual(l, lv);

                for (int j = 1; j < 4; j++)
                {
                    if (i == j)
                        l[i] = lv.Entry(i);
                    else
                        l[j] = lv.Entry(j) + 1.0;
                }

                Assert.AreNotEqual(l, lv);
            }
        }
        [Test]
        public void TestAssignment()
        {
            Vector3D v = new Vector3D(1.5, -2.0, 3.0);
            Location lv = new Location(v);
            Location l = new Location();

            Assert.AreEqual(1.0, l[1]);
            Assert.AreEqual(0.0, l[2]);
            Assert.AreEqual(0.0, l[3]);

            l = lv.Clone();
            Assert.AreEqual(l[1], lv[1]);
            Assert.AreEqual(l[2], lv[2]);
            Assert.AreEqual(l[3], lv[3]);
            CheckLocation(l, v);

            // Make sure that l and lv are distinct copies
            lv[1] = -3.4;
            Assert.AreEqual(v.X, l[1]);
            Assert.AreEqual(v.Y, l[2]);
            Assert.AreEqual(v.Z, l[3]);
            lv[1] = 1.5;

            for (int i = 1; i < 4; i++)
            {
                l = lv.Clone();
                double x = v[i - 1] + 1.0;
                l[i] = x;

                for (int j = 1; j < 4; j++)
                {
                    if (i == j)
                    {
                        Assert.AreEqual(l[i], x);
                        Assert.AreEqual(l.Entry(i), x);
                    }
                    else
                    {
                        Assert.AreEqual(l[j], v[j - 1]);
                        Assert.AreEqual(l.Entry(j), v[j - 1]);
                    }
                }

                CheckLocation(l, new Vector3D(l[1], l[2], l[3]));
            }

            l = new Location(v);
            Assert.AreEqual(l[1], v.X);
            Assert.AreEqual(l[2], v.Y);
            Assert.AreEqual(l[3], v.Z);
            CheckLocation(l, v);

            // Make sure that l and v are distinct copies
            v.Y = -3.4;
            Assert.AreEqual(lv[1], l[1]);
            Assert.AreEqual(lv[2], l[2]);
            Assert.AreEqual(lv[3], l[3]);
            v.Y = -2.0;

            for (int i = 1; i < 4; i++)
            {
                l = new Location(v);
                double x = v[i - 1] + 1.0;
                l[i] = x;

                for (int j = 1; j < 4; j++)
                {
                    if (i == j)
                    {
                        Assert.AreEqual(l[i], x);
                        Assert.AreEqual(l.Entry(i), x);
                    }
                    else
                    {
                        Assert.AreEqual(l[j], v[j - 1]);
                        Assert.AreEqual(l.Entry(j), v[j - 1]);
                    }
                }

                CheckLocation(l, new Vector3D(l[1], l[2], l[3]));
            }

            // Check the copy assignment operator for an FGLocation with cached values.
            l = new Location(v);
            CheckLocation(l, v);

            lv = l;

            Assert.AreEqual(l[1], lv[1], tolerance);
            Assert.AreEqual(l[2], lv[2], tolerance);
            Assert.AreEqual(l[3], lv[3], tolerance);

            CheckLocation(lv, v);
        }

        // longitude in rad of the location
        // The returned values are in the range between
        // -pi <= lon <= pi 
        [Test]
        public void CheckLongitude01()
        {
            Location loc = new Location();

            loc.Longitude = 0;
            Assert.AreEqual(0, loc.Longitude, tolerance);
        }

        [Test]
        public void CheckLongitude02()
        {
            Location loc = new Location();

            loc.Longitude = Math.PI / 2;
            Assert.AreEqual(Math.PI / 2, loc.Longitude, tolerance);
        }

        [Test]
        public void CheckLongitude03()
        {
            Location loc = new Location();

            loc.Longitude = 2 * Math.PI;
            Assert.AreEqual(0, loc.Longitude, tolerance);
        }

        [Test]
        public void CheckLongitude04()
        {
            Location loc = new Location();

            loc.Longitude = 10 * 2 * Math.PI;
            Assert.AreEqual(0, loc.Longitude, tolerance);
        }

        [Test]
        public void CheckLongitude05()
        {
            Location loc = new Location();

            loc.Longitude = -10 * 2 * Math.PI;
            Assert.AreEqual(0, loc.Longitude, tolerance);
        }

        [Test]
        public void CheckLongitude06()
        {
            Location loc = new Location();

            loc.Longitude = Math.PI;
            Assert.AreEqual(Math.PI, loc.Longitude, tolerance);
        }

        [Test]
        public void CheckLongitude07()
        {
            Location loc = new Location();

            loc.Longitude = -Math.PI;
            Assert.AreEqual(-Math.PI, loc.Longitude, tolerance);
        }

        [Test]
        public void CheckLongitude08()
        {
            Location loc = new Location();

            loc.Latitude = Math.PI / 2;
            loc.Radius = 1000;

            // The latitude and the radius value are preserved
            loc.Longitude = Math.PI;

            Assert.AreEqual(Math.PI, loc.Longitude, tolerance);
            Assert.AreEqual(Math.PI / 2, loc.Latitude, tolerance);
            Assert.AreEqual(1000, loc.Radius, tolerance);
        }

        [Test]
        public void CheckLongitude09()
        {
            Location loc = new Location();

            loc.Latitude = Math.PI;
            loc.Radius = 0;
            // If the radius is previously set to zero it is changed to be
            // equal to 1.0 past this call.
            loc.Longitude = Math.PI / 2;

            Assert.AreEqual(Math.PI / 2, loc.Longitude, tolerance);
            Assert.AreEqual(1, loc.Radius, tolerance);
        }

        // longitude in rad of the location
        // The returned values are in the range between
        // -pi/2 <= lat <= pi/2
        [Test]
        public void CheckLatitude01()
        {
            Location loc = new Location();

            loc.Latitude = 0;
            Assert.AreEqual(0, loc.Latitude, tolerance);
        }

        [Test]
        public void CheckLatitude02()
        {
            Location loc = new Location();

            loc.Latitude = Math.PI / 2;
            Assert.AreEqual(Math.PI / 2, loc.Latitude, tolerance);
        }

        [Test]
        public void CheckLatitude03()
        {
            Location loc = new Location();

            loc.Latitude = 2 * Math.PI;
            Assert.AreEqual(0, loc.Latitude, tolerance);
        }

        [Test]
        public void CheckLatitude04()
        {
            Location loc = new Location();

            loc.Latitude = 10 * 2 * Math.PI;
            Assert.AreEqual(0, loc.Latitude, tolerance);
        }

        [Test]
        public void CheckLatitude05()
        {
            Location loc = new Location();

            loc.Latitude = -10 * 2 * Math.PI;
            Assert.AreEqual(0, loc.Latitude, tolerance);
        }

        [Test]
        public void CheckLatitude06()
        {
            Location loc = new Location();

            loc.Latitude = Math.PI;
            Assert.AreEqual(0, loc.Latitude, tolerance);
        }

        [Test]
        public void CheckLatitude07()
        {
            Location loc = new Location();

            loc.Latitude = -Math.PI;
            Assert.AreEqual(0, loc.Latitude, tolerance);
        }

        [Test]
        public void CheckLatitude08()
        {
            Location loc = new Location();

            loc.Longitude = Math.PI;
            loc.Radius = 1000;
            // The longitude and the radius value are preserved
            loc.Latitude = Math.PI / 2;

            Assert.AreEqual(Math.PI, loc.Longitude, tolerance);
            Assert.AreEqual(Math.PI / 2, loc.Latitude, tolerance);
            Assert.AreEqual(1000, loc.Radius, tolerance);
        }

        [Test]
        public void CheckLatitude09()
        {
            Location loc = new Location();

            loc.Longitude = Math.PI;
            loc.Radius = 0;
            // If the radius is previously set to zero it is changed to be
            // equal to 1.0 past this call.
            loc.Latitude = Math.PI / 2;

            Assert.AreEqual(Math.PI / 2, loc.Latitude, tolerance);
            Assert.AreEqual(1, loc.Radius, tolerance);
        }

        [Test]
        public void CheckAdd01()
        {
            Location loc1 = new Location();
            Location loc2 = new Location();

            Location loc3 = loc1 + loc2;

            Assert.AreEqual(0, loc3.Longitude, tolerance);
            Assert.AreEqual(0, loc3.Latitude, tolerance);
            Assert.AreEqual(2, loc3.Radius, tolerance);
        }

        [Test]
        public void CheckLongitudeDeg01()
        {
            Location loc = new Location();

            loc.Longitude = 0;
            Assert.AreEqual(0, loc.LongitudeDeg, tolerance);
        }

        [Test]
        public void CheckLongitudeDeg02()
        {
            Location loc = new Location();

            loc.Longitude = Math.PI / 2;
            Assert.AreEqual(90, loc.LongitudeDeg, tolerance);
        }
        [Test]
        public void CheckLatitudeDeg01()
        {
            Location loc = new Location();

            loc.Latitude = 0;
            Assert.AreEqual(0, loc.LatitudeDeg, tolerance);
        }

        [Test]
        public void CheckLatitudeDeg02()
        {
            Location loc = new Location();

            loc.Latitude = Math.PI / 2;
            Assert.AreEqual(90, loc.LatitudeDeg, tolerance);
        }

    }
}
