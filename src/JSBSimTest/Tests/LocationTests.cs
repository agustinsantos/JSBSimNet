#region Copyright(C)  Licensed under GNU GPL.
/// Copyright (C) 2005-2006 Agustin Santos Mendez
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
#endregion

namespace JSBSim.Tests
{
    using System;
    using System.Xml;
    using System.IO;
    using System.Text;

    using NUnit.Framework;

    using JSBSim;
    using JSBSim.MathValues;
    using JSBSim.InputOutput;
    using JSBSim.Script;
    using JSBSim.Models.FlightControl;

    /// <summary>
    /// Some table Tests: load and access.
    /// </summary>
    [TestFixture]
    public class LocationTests
    {
        private const double tolerance = 10E-15;

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
