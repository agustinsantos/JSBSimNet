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
    using System.IO;
    using CommonUtils.MathLib;
    using JSBSim;
    using JSBSim.InputOutput;

    // Import log4net classes.
    using log4net;
    using NUnit.Framework;

    /// <summary>
    /// Some GroundCallback Tests: load and access.
    /// </summary>
    [TestFixture]
    public class GroundCallbackTests : TestParentClass
    {
        private const double tolerance = 10E-12;
        private const double RadiusReference = 20925646.32546;
        private const double a = 20925646.32546; // WGS84 semimajor axis length in feet
        private const double b = 20855486.5951;  // WGS84 semiminor axis length in feet

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
        /// Configures log4Net at startup
        /// </summary>
        [SetUp]
        public void Init()
        {
            FileInfo logFile = new System.IO.FileInfo("Log4Net.config");
            if (logFile.Exists)
            {
                // Log4Net is configured using a DOMConfigurator.
                log4net.Config.XmlConfigurator.Configure(logFile);
            }
            else
            {
                // Set up a simple configuration that logs on the console.
                log4net.Config.BasicConfigurator.Configure();
            }

            // Log an info level message
            if (log.IsDebugEnabled)
            {
                log.Debug("Starting JSBSim tests");
            }

        }

        [Test]
        public void TestSphericalEarthSurface()
        {
            GroundCallback cb = new DefaultGroundCallback(RadiusReference, RadiusReference);

            Location loc, contact;
            Vector3D normal, v, w;
            Vector3D zero = new Vector3D(0.0, 0.0, 0.0);

            // Check that, for a point located, on the sea level radius the AGL is 0.0
            for (double lat = -90.0; lat <= 90.0; lat += 30.0)
            {
                for (double lon = 0.0; lon <= 360.0; lon += 45.0)
                {
                    double lon_rad = lon * Math.PI / 180.0;
                    double lat_rad = lat * Math.PI / 180.0;
                    loc = new Location(lon_rad, lat_rad, RadiusReference);
                    double agl = cb.GetAGLevel(loc, out contact, out normal, out v, out w);
                    Assert.AreEqual(0.0, agl, 1e-8);
                    AssertVectorEqual(v, zero);
                    AssertVectorEqual(w, zero);
                    Vector3D vLoc = (Vector3D)loc;
                    Vector3D vContact = (Vector3D)contact;
                    Assert.AreEqual(vContact.Magnitude(), RadiusReference, tolerance);
                    Assert.AreEqual(vLoc.X, vContact.X, 1e-8);
                    Assert.AreEqual(vLoc.Y, vContact.Y, 1e-8);
                    Assert.AreEqual(vLoc.Z, vContact.Z, 1e-8);
                    Assert.AreEqual(normal.X, Math.Cos(lat_rad) * Math.Cos(lon_rad), tolerance);
                    Assert.AreEqual(normal.Y, Math.Cos(lat_rad) * Math.Sin(lon_rad), tolerance);
                    Assert.AreEqual(normal.Z, Math.Sin(lat_rad), tolerance);
                    vContact.Normalize();
                    AssertVectorEqual(vContact, normal);
                }
            }
        }

        [Test]
        public void TestSphericalEarthAltitude()
        {
            GroundCallback cb = new DefaultGroundCallback(RadiusReference, RadiusReference);
            Location loc, contact;
            Vector3D normal, v, w;
            Vector3D zero = new Vector3D(0.0, 0.0, 0.0);
            double h = 100000.0;

            // Check that, for a point located, on the sea level radius the AGL is 0.0
            for (double lat = -90.0; lat <= 90.0; lat += 30.0)
            {
                for (double lon = 0.0; lon <= 360.0; lon += 45.0)
                {
                    double lon_rad = lon * Math.PI / 180.0;
                    double lat_rad = lat * Math.PI / 180.0;
                    loc = new Location(lon_rad, lat_rad, RadiusReference + h);
                    double agl = cb.GetAGLevel(loc, out contact, out normal, out v, out w);
                    Assert.AreEqual(h / agl, 1.0, tolerance * 100.0);
                    AssertVectorEqual(v, zero);
                    AssertVectorEqual(w, zero);
                    Vector3D vLoc = (Vector3D)loc;
                    Vector3D vContact = (Vector3D)contact;
                    Assert.AreEqual(vContact.Magnitude(), RadiusReference, tolerance);
                    Vector3D vtest = vLoc / (1.0 + h / RadiusReference);
                    Assert.AreEqual(vtest.X, vContact.X, 1e-8);
                    Assert.AreEqual(vtest.Y, vContact.Y, 1e-8);
                    Assert.AreEqual(vtest.Z, vContact.Z, 1e-8);
                    Assert.AreEqual(normal.X, Math.Cos(lat_rad) * Math.Cos(lon_rad), tolerance);
                    Assert.AreEqual(normal.Y, Math.Cos(lat_rad) * Math.Sin(lon_rad), tolerance);
                    Assert.AreEqual(normal.Z, Math.Sin(lat_rad), tolerance);
                    vContact.Normalize();
                    AssertVectorEqual(vContact, normal);
                }
            }
        }

        [Test]
        public void TestSphericalEarthAltitudeWithTerrainElevation()
        {
            GroundCallback cb = new DefaultGroundCallback(RadiusReference, RadiusReference);
            Location loc, contact;
            Vector3D normal, v, w;
            Vector3D zero = new Vector3D(0.0, 0.0, 0.0);
            double h = 100000.0;
            double elevation = 2000.0;

            cb.SetTerrainElevation(elevation);

            // Check that, for a point located, on the sea level radius the AGL is 0.0
            for (double lat = -90.0; lat <= 90.0; lat += 30.0)
            {
                for (double lon = 0.0; lon <= 360.0; lon += 45.0)
                {
                    double lon_rad = lon * Math.PI / 180.0;
                    double lat_rad = lat * Math.PI / 180.0;
                    loc = new Location(lon_rad, lat_rad, RadiusReference + h);
                    double agl = cb.GetAGLevel(loc, out contact, out normal, out v, out w);
                    Assert.AreEqual((h - elevation) / agl, 1.0, tolerance * 100.0);
                    AssertVectorEqual(v, zero);
                    AssertVectorEqual(w, zero);
                    Vector3D vLoc = (Vector3D)loc;
                    Vector3D vContact = (Vector3D)contact;
                    Assert.AreEqual(vContact.Magnitude() / (RadiusReference + elevation), 1.0,
                                    tolerance);
                    AssertVectorEqual(vLoc / (RadiusReference + h),
                                            vContact / (RadiusReference + elevation));
                    Assert.AreEqual(normal.X, Math.Cos(lat_rad) * Math.Cos(lon_rad), tolerance);
                    Assert.AreEqual(normal.Y, Math.Cos(lat_rad) * Math.Sin(lon_rad), tolerance);
                    Assert.AreEqual(normal.Z, Math.Sin(lat_rad), tolerance);
                    vContact.Normalize();
                    AssertVectorEqual(vContact, normal);
                }
            }
        }

        [Test]
        public void TestWGS84EarthSurface()
        {
            GroundCallback cb = new DefaultGroundCallback(a, b);
            Location loc = new Location(), contact = new Location();
            Vector3D normal, v, w;
            Vector3D zero = new Vector3D(0.0, 0.0, 0.0);

            loc.SetEllipse(a, b);
            contact.SetEllipse(a, b);

            // Check that, for a point located, on the sea level radius the AGL is 0.0
            for (double lat = -90.0; lat <= 90.0; lat += 30.0)
            {
                for (double lon = 0.0; lon <= 360.0; lon += 45.0)
                {
                    double lon_rad = lon * Math.PI / 180.0;
                    double lat_rad = lat * Math.PI / 180.0;
                    loc.SetPositionGeodetic(lon_rad, lat_rad, 0.0);
                    double agl = cb.GetAGLevel(loc, out contact, out normal, out v, out w);
                    Assert.AreEqual(0.0, agl, 1e-8);
                    AssertVectorEqual(v, zero);
                    AssertVectorEqual(w, zero);
                    Vector3D vLoc = (Vector3D)loc;
                    Vector3D vContact = (Vector3D)contact;
                    Assert.AreEqual(vLoc.X, vContact.X, 1e-8);
                    Assert.AreEqual(vLoc.Y, vContact.Y, 1e-8);
                    Assert.AreEqual(vLoc.Z, vContact.Z, 1e-8);
                    Assert.AreEqual(normal.X, Math.Cos(lat_rad) * Math.Cos(lon_rad), tolerance);
                    Assert.AreEqual(normal.Y, Math.Cos(lat_rad) * Math.Sin(lon_rad), tolerance);
                    Assert.AreEqual(normal.Z, Math.Sin(lat_rad), tolerance);
                }
            }
        }

        [Test]
        public void TestWGS84EarthAltitude()
        {
            GroundCallback cb = new DefaultGroundCallback(a, b);
            Location loc = new Location(), contact = new Location();
            Vector3D normal, v, w;
            Vector3D zero = new Vector3D(0.0, 0.0, 0.0);
            double h = 100000.0;

            loc.SetEllipse(a, b);
            contact.SetEllipse(a, b);

            // Check that, for a point located, on the sea level radius the AGL is 0.0
            for (double lat = -90.0; lat <= 90.0; lat += 30.0)
            {
                for (double lon = 0.0; lon <= 360.0; lon += 45.0)
                {
                    double lon_rad = lon * Math.PI / 180.0;
                    double lat_rad = lat * Math.PI / 180.0;
                    loc.SetPositionGeodetic(lon_rad, lat_rad, h);
                    double agl = cb.GetAGLevel(loc, out contact, out normal, out v, out w);
                    Assert.AreEqual(h, agl, 1e-8);
                    AssertVectorEqual(v, zero);
                    AssertVectorEqual(w, zero);
                    Assert.AreEqual(normal.X, Math.Cos(lat_rad) * Math.Cos(lon_rad), tolerance);
                    Assert.AreEqual(normal.Y, Math.Cos(lat_rad) * Math.Sin(lon_rad), tolerance);
                    Assert.AreEqual(normal.Z, Math.Sin(lat_rad), tolerance);
                    Vector3D vLoc = (Vector3D)loc - h * normal;
                    Vector3D vContact = (Vector3D)contact;
                    Assert.AreEqual(vLoc.X, vContact.X, 1e-7);
                    Assert.AreEqual(vLoc.Y, vContact.Y, 1e-7);
                    Assert.AreEqual(vLoc.Z, vContact.Z, 1e-7);
                }
            }
        }

        [Test]
        public void TestWGS84EarthAltitudeWithTerrainElevation()
        {
            GroundCallback cb = new DefaultGroundCallback(a, b);
            Location loc = new Location(), contact = new Location();
            Vector3D normal, v, w;
            Vector3D zero = new Vector3D(0.0, 0.0, 0.0);
            double h = 100000.0;
            double elevation = 2000.0;

            loc.SetEllipse(a, b);
            contact.SetEllipse(a, b);
            cb.SetTerrainElevation(elevation);

            // Check that, for a point located, on the sea level radius the AGL is 0.0
            for (double lat = -90.0; lat <= 90.0; lat += 30.0)
            {
                for (double lon = 0.0; lon <= 360.0; lon += 45.0)
                {
                    double lon_rad = lon * Math.PI / 180.0;
                    double lat_rad = lat * Math.PI / 180.0;
                    loc.SetPositionGeodetic(lon_rad, lat_rad, h);
                    double agl = cb.GetAGLevel(loc, out contact, out normal, out v, out w);
                    Assert.AreEqual(h - elevation, agl, 1e-8);
                    AssertVectorEqual(v, zero);
                    AssertVectorEqual(w, zero);
                    Assert.AreEqual(normal.X, Math.Cos(lat_rad) * Math.Cos(lon_rad), tolerance);
                    Assert.AreEqual(normal.Y, Math.Cos(lat_rad) * Math.Sin(lon_rad), tolerance);
                    Assert.AreEqual(normal.Z, Math.Sin(lat_rad), tolerance);
                    Vector3D vLoc = (Vector3D)loc - (h - elevation) * normal;
                    Vector3D vContact = (Vector3D)contact;
                    Assert.AreEqual(vLoc.X, vContact.X, 1e-7);
                    Assert.AreEqual(vLoc.Y, vContact.Y, 1e-7);
                    Assert.AreEqual(vLoc.Z, vContact.Z, 1e-7);
                }
            }
        }
    }
}
