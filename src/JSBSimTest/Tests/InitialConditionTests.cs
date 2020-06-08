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
    using System.Text;
    using System.Xml;
    using JSBSim;
    // Import log4net classes.
    using log4net;
    using NUnit.Framework;

    /// <summary>
    /// Some Initial Conditial Tests: load and access.
    /// </summary>
    [TestFixture]
    public class InitialConditionTests
    {
        private const double tolerance = 10E-12;

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
        public void TestDefaultConstructor()
        {
            FDMExecutive fdmex = new FDMExecutive();
            InitialCondition ic = new InitialCondition(fdmex);

            Assert.AreEqual(0.0, ic.GetLatitudeDegIC());
            Assert.AreEqual(0.0, ic.GetLatitudeRadIC());
            Assert.AreEqual(0.0, ic.GetLongitudeDegIC());
            Assert.AreEqual(0.0, ic.GetLongitudeRadIC());
            Assert.AreEqual(0.0, ic.GetGeodLatitudeDegIC());
            Assert.AreEqual(0.0, ic.GetGeodLatitudeRadIC());
            Assert.AreEqual(0.0, ic.GetThetaDegIC());
            Assert.AreEqual(0.0, ic.GetThetaRadIC());
            Assert.AreEqual(0.0, ic.GetPhiDegIC());
            Assert.AreEqual(0.0, ic.GetPhiRadIC());
            Assert.AreEqual(0.0, ic.GetPsiDegIC());
            Assert.AreEqual(0.0, ic.GetPsiRadIC());
            Assert.AreEqual(0.0, ic.GetAltitudeASLFtIC());
            Assert.AreEqual(0.0, ic.GetAltitudeAGLFtIC());
            Assert.AreEqual(0.0, ic.GetEarthPositionAngleIC());
            Assert.AreEqual(0.0, ic.GetTerrainElevationFtIC());
            Assert.AreEqual(0.0, ic.GetVcalibratedKtsIC());
            Assert.AreEqual(0.0, ic.GetVequivalentKtsIC());
            Assert.AreEqual(0.0, ic.GetVgroundFpsIC());
            Assert.AreEqual(0.0, ic.GetVtrueFpsIC());
            Assert.AreEqual(0.0, ic.GetMachIC());
            Assert.AreEqual(0.0, ic.GetClimbRateFpsIC());
            Assert.AreEqual(0.0, ic.GetFlightPathAngleDegIC());
            Assert.AreEqual(0.0, ic.GetFlightPathAngleRadIC());
            Assert.AreEqual(0.0, ic.GetAlphaDegIC());
            Assert.AreEqual(0.0, ic.GetAlphaRadIC());
            Assert.AreEqual(0.0, ic.GetBetaDegIC());
            Assert.AreEqual(0.0, ic.GetBetaDegIC());
            Assert.AreEqual(0.0, ic.GetBetaRadIC());
            Assert.AreEqual(0.0, ic.GetWindFpsIC());
            Assert.AreEqual(0.0, ic.GetWindDirDegIC());
            Assert.AreEqual(0.0, ic.GetWindUFpsIC());
            Assert.AreEqual(0.0, ic.GetWindVFpsIC());
            Assert.AreEqual(0.0, ic.GetWindWFpsIC());
            Assert.AreEqual(0.0, ic.GetWindNFpsIC());
            Assert.AreEqual(0.0, ic.GetWindEFpsIC());
            Assert.AreEqual(0.0, ic.GetWindDFpsIC());
            Assert.AreEqual(0.0, ic.GetUBodyFpsIC());
            Assert.AreEqual(0.0, ic.GetVBodyFpsIC());
            Assert.AreEqual(0.0, ic.GetWBodyFpsIC());
            Assert.AreEqual(0.0, ic.GetVNorthFpsIC());
            Assert.AreEqual(0.0, ic.GetVEastFpsIC());
            Assert.AreEqual(0.0, ic.GetVDownFpsIC());
            Assert.AreEqual(0.0, ic.GetPRadpsIC());
            Assert.AreEqual(0.0, ic.GetQRadpsIC());
            Assert.AreEqual(0.0, ic.GetRRadpsIC());
            //  TS_ASSERT_VECTOR_EQUALS(ic.GetWindNEDFpsIC(), zero);
            //TS_ASSERT_VECTOR_EQUALS(ic.GetUVWFpsIC(), zero);
            //TS_ASSERT_VECTOR_EQUALS(ic.GetPQRRadpsIC(), zero);
        }

        [Test]
        public void TestSetPositionASL()
        {
            FDMExecutive fdmex = new FDMExecutive();
            InitialCondition ic = new InitialCondition(fdmex);

            for (double lon = -180.0; lon <= 180.0; lon += 30.0)
            {
                ic.SetLongitudeDegIC(lon);

                // Altitude first, then latitude
                for (double asl = 1.0; asl <= 1000001.0; asl += 10000.0)
                {
                    ic.SetAltitudeASLFtIC(asl);
                    for (double lat = -90.0; lat <= 90.0; lat += 10.0)
                    {
                        ic.SetLatitudeDegIC(lat);

                        Assert.AreEqual(lon, ic.GetLongitudeDegIC(), tolerance * 100.0);
                        Assert.AreEqual(lon * Math.PI / 180.0, ic.GetLongitudeRadIC(), tolerance);
                        Assert.AreEqual(1.0, ic.GetAltitudeASLFtIC() / asl, 2E-8);
                        Assert.AreEqual(1.0, ic.GetAltitudeAGLFtIC() / asl, 2E-8);
                        Assert.AreEqual(lat, ic.GetLatitudeDegIC(), tolerance);
                        Assert.AreEqual(lat * Math.PI / 180.0, ic.GetLatitudeRadIC(), tolerance);
                    }
                }

                // Latitude first, then altitude
                for (double lat = -90.0; lat <= 90.0; lat += 10.0)
                {
                    ic.SetLatitudeDegIC(lat);
                    for (double asl = 1.0; asl <= 1000001.0; asl += 10000.0)
                    {
                        ic.SetAltitudeASLFtIC(asl);

                        Assert.AreEqual(lon, ic.GetLongitudeDegIC(), tolerance * 100.0);
                        Assert.AreEqual(lon * Math.PI / 180.0, ic.GetLongitudeRadIC(), tolerance);
                        Assert.AreEqual(1.0, ic.GetAltitudeASLFtIC() / asl, 2E-8);
                        Assert.AreEqual(1.0, ic.GetAltitudeAGLFtIC() / asl, 2E-8);
                        Assert.AreEqual(lat, ic.GetLatitudeDegIC(), tolerance * 100.0);
                        Assert.AreEqual(lat * Math.PI / 180.0, ic.GetLatitudeRadIC(), tolerance);
                    }
                }
            }
        }

        [Test]
        public void TestSetPositionAGL()
        {
            FDMExecutive fdmex = new FDMExecutive();
            InitialCondition ic = new InitialCondition(fdmex);

            ic.SetTerrainElevationFtIC(2000.0);

            for (double lon = -180.0; lon <= 180.0; lon += 30.0)
            {
                ic.SetLongitudeDegIC(lon);

                // Altitude first, then latitude
                for (double agl = 1.0; agl <= 1000001.0; agl += 10000.0)
                {
                    ic.SetAltitudeAGLFtIC(agl);
                    for (double lat = -90.0; lat <= 90.0; lat += 10.0)
                    {
                        ic.SetLatitudeDegIC(lat);

                        Assert.AreEqual(lon, ic.GetLongitudeDegIC(), tolerance * 100.0);
                        Assert.AreEqual(lon * Math.PI / 180.0, ic.GetLongitudeRadIC(), tolerance);
                        Assert.AreEqual(1.0, ic.GetAltitudeASLFtIC() / (agl + 2000.0), 2E-8);
                        Assert.AreEqual(1.0, ic.GetAltitudeAGLFtIC() / agl, 2E-8);
                        Assert.AreEqual(lat, ic.GetLatitudeDegIC(), tolerance * 10.0);
                        Assert.AreEqual(lat * Math.PI / 180.0, ic.GetLatitudeRadIC(), tolerance);
                    }
                }

                // Latitude first, then altitude
                for (double lat = -90.0; lat <= 90.0; lat += 10.0)
                {
                    ic.SetLatitudeDegIC(lat);
                    for (double agl = 1.0; agl <= 1000001.0; agl += 10000.0)
                    {
                        ic.SetAltitudeAGLFtIC(agl);

                        Assert.AreEqual(lon, ic.GetLongitudeDegIC(), tolerance * 100.0);
                        Assert.AreEqual(lon * Math.PI / 180.0, ic.GetLongitudeRadIC(), tolerance);
                        Assert.AreEqual(1.0, ic.GetAltitudeASLFtIC() / (agl + 2000.0), 2E-8);
                        Assert.AreEqual(1.0, ic.GetAltitudeAGLFtIC() / agl, 2E-8);
                        Assert.AreEqual(lat, ic.GetLatitudeDegIC(), tolerance * 100.0);
                        Assert.AreEqual(lat * Math.PI / 180.0, ic.GetLatitudeRadIC(), tolerance);
                    }
                }
            }
        }

        [Test]
        public void TestBodyVelocity()
        {
            FDMExecutive fdmex = new FDMExecutive();
            InitialCondition ic = new InitialCondition(fdmex);

            ic.SetUBodyFpsIC(100.0);
            Assert.AreEqual(100.0, ic.GetUBodyFpsIC(), tolerance);
            Assert.AreEqual(0.0, ic.GetVBodyFpsIC(), tolerance);
            Assert.AreEqual(0.0, ic.GetWBodyFpsIC(), tolerance);
            Assert.AreEqual(100.0, ic.GetVtrueFpsIC(), tolerance);
            Assert.AreEqual(100.0, ic.GetVgroundFpsIC(), tolerance);
            Assert.AreEqual(0.0, ic.GetAlphaDegIC(), tolerance);
            Assert.AreEqual(0.0, ic.GetBetaDegIC(), tolerance);

            for (double theta = -90.0; theta <= 90.0; theta += 10.0)
            {
                ic.SetThetaDegIC(theta);

                Assert.AreEqual(100.0, ic.GetUBodyFpsIC(), tolerance * 10.0);
                Assert.AreEqual(0.0, ic.GetVBodyFpsIC(), tolerance);
                Assert.AreEqual(0.0, ic.GetWBodyFpsIC(), tolerance);
                Assert.AreEqual(100.0 * Math.Cos(theta * Math.PI / 180.0), ic.GetVNorthFpsIC(), tolerance);
                Assert.AreEqual(0.0, ic.GetVEastFpsIC(), tolerance);
                Assert.AreEqual(-100.0 * Math.Sin(theta * Math.PI / 180.0), ic.GetVDownFpsIC(), tolerance * 10.0);
                Assert.AreEqual(0.0, ic.GetAlphaDegIC(), tolerance * 10.0);
                Assert.AreEqual(0.0, ic.GetBetaDegIC(), tolerance);
                Assert.AreEqual(100.0, ic.GetVtrueFpsIC(), tolerance * 10.0);
                Assert.AreEqual(Math.Abs(100.0 * Math.Cos(theta * Math.PI / 180.0)), ic.GetVgroundFpsIC(), tolerance);
                Assert.AreEqual(0.0, ic.GetPhiDegIC(), tolerance);
                Assert.AreEqual(theta, ic.GetThetaDegIC(), tolerance * 10.0);
                Assert.AreEqual(0.0, ic.GetPsiDegIC(), tolerance);
            }

            ic.SetThetaRadIC(0.0);
            for (double phi = -180.0; phi <= 180.0; phi += 10.0)
            {
                ic.SetPhiDegIC(phi);

                Assert.AreEqual(100.0, ic.GetUBodyFpsIC(), tolerance * 100.0);
                Assert.AreEqual(0.0, ic.GetVBodyFpsIC(), tolerance);
                Assert.AreEqual(0.0, ic.GetWBodyFpsIC(), tolerance);
                Assert.AreEqual(100.0, ic.GetVtrueFpsIC(), tolerance * 100.0);
                Assert.AreEqual(100.0, ic.GetVgroundFpsIC(), tolerance * 100.0);
                Assert.AreEqual(0.0, ic.GetAlphaDegIC(), tolerance);
                Assert.AreEqual(0.0, ic.GetBetaDegIC(), tolerance);
                Assert.AreEqual(phi, ic.GetPhiDegIC(), tolerance);
                Assert.AreEqual(0.0, ic.GetThetaDegIC(), tolerance);
                Assert.AreEqual(0.0, ic.GetPsiDegIC(), tolerance);
            }

            ic.SetPhiDegIC(0.0);
            for (double psi = 0.0; psi <= 360.0; psi += 10.0)
            {
                ic.SetPsiDegIC(psi);

                Assert.AreEqual(100.0, ic.GetUBodyFpsIC(), tolerance * 100.0);
                Assert.AreEqual(0.0, ic.GetVBodyFpsIC(), tolerance * 10.0);
                Assert.AreEqual(0.0, ic.GetWBodyFpsIC(), tolerance);
                Assert.AreEqual(100.0, ic.GetVtrueFpsIC(), tolerance * 100.0);
                Assert.AreEqual(100.0, ic.GetVgroundFpsIC(), tolerance * 100.0);
                Assert.AreEqual(0.0, ic.GetAlphaDegIC(), tolerance);
                Assert.AreEqual(0.0, ic.GetBetaDegIC(), tolerance * 10.0);
                Assert.AreEqual(0.0, ic.GetPhiDegIC(), tolerance);
                Assert.AreEqual(0.0, ic.GetThetaDegIC(), tolerance);
                Assert.AreEqual(psi, ic.GetPsiDegIC(), tolerance * 10.0);
            }
        }

        [Test]
        public void CheckPosition()
        {
            string test =
                @"<?xml version=""1.0""?>
                    <initialize name=""reset00"">
                      <!--
                        This file sets up the mk82 to start off
                        from altitude.
                      -->
                      <latitude unit=""DEG"">   47.0  </latitude>
                      <longitude unit=""DEG"">-110.0  </longitude>
                      <altitude unit=""FT""> 10000.0  </altitude>
                    </initialize>";

            FDMExecutive fdm = new FDMExecutive();
            XmlElement elem = BuildXmlConfig(test);
            InitialCondition IC = fdm.GetIC();
            IC.Load(elem, false);

            if (log.IsDebugEnabled)
            {
                log.Debug("Testing JSBSim Initial Conditions: Lat, long, alt.");
            }

            //Checks values 
            Assert.AreEqual(47.0, IC.GetLatitudeDegIC(), tolerance, "latitude in deg.");
            Assert.AreEqual(-110.0, IC.GetLongitudeDegIC(), tolerance, "longitude in deg.");
            Assert.AreEqual(10000.0, IC.GetAltitudeASLFtIC(), tolerance * 1000, "Altitude in Ft");
        }

        [Test]
        public void CheckOrientation()
        {
            string test =
                @"<?xml version=""1.0""?>
                    <initialize name=""reset00"">
                      <!--
                        This file sets up the mk82 to start off
                        from altitude.
                      -->
                      <ubody unit=""FT/SEC""> 100.0 </ubody> 
                      <alpha unit=""DEG"">  10.0  </alpha>
                      <beta unit=""DEG"">  20.0  </beta>
                    </initialize>";

            FDMExecutive fdm = new FDMExecutive();
            XmlElement elem = BuildXmlConfig(test);
            InitialCondition IC = fdm.GetIC();
            IC.Load(elem, false);

            if (log.IsDebugEnabled)
            {
                log.Debug("Testing JSBSim Initial Conditions: Orientation");
            }

            //Checks values 
            Assert.AreEqual(92.541657839832311, IC.GetUBodyFpsIC(), tolerance);
            Assert.AreEqual(10.0, IC.GetAlphaDegIC(), tolerance, "initial angle of attack");
            Assert.AreEqual(20.0, IC.GetBetaDegIC(), tolerance, "initial sideslip angle");
        }
        [Test]
        public void CheckOrientation02()
        {
            string test =
                @"<?xml version=""1.0""?>
                    <initialize name=""reset00"">
                        <!--
                        This file sets up the ball in orbit.
                        Velocity of Earth surface at equator:                   1525.92 ft/sec. at ground level.
                        Velocity of Earth surface-synchronous point at equator: 1584.26 ft/sec. at 800 kft.
                        1584.2593825 + 23869.9759596 = 25454.235342 ft/sec
                        -->
                        <ubody unit=""FT/SEC""> 23869.9759596 </ubody> 
                        <latitude unit = ""DEG"" > 0.0 </latitude>
                        <longitude unit = ""DEG"" > 0.0 </longitude>
                        <psi unit = ""DEG"" > 90.0 </psi>
                        <altitude unit = ""FT"" > 800000.0 </altitude>
                    </initialize>";

            FDMExecutive fdm = new FDMExecutive();
            XmlElement elem = BuildXmlConfig(test);
            InitialCondition IC = fdm.GetIC();
            IC.Load(elem, false);

            if (log.IsDebugEnabled)
            {
                log.Debug("Testing JSBSim Initial Conditions: Orientation");
            }

            //Checks values 
            Assert.AreEqual(23869.9759596, IC.GetUBodyFpsIC(), tolerance);
            Assert.AreEqual(90.0, IC.GetPsiDegIC(), tolerance, "initial heading angle");
        }
        [Test]
        public void CheckVelocity()
        {
            string test =
                @"<?xml version=""1.0""?>
                    <initialize name=""reset00"">
                      <!--
                        This file sets up the mk82 to start off
                        from altitude.
                      -->
                      <ubody unit=""FT/SEC"">  10.0  </ubody>
                      <vbody unit=""FT/SEC"">  20.0  </vbody>
                      <wbody unit=""FT/SEC"">  30.0  </wbody>
                    </initialize>";

            FDMExecutive fdm = new FDMExecutive();
            XmlElement elem = BuildXmlConfig(test);
            InitialCondition IC = fdm.GetIC();
            IC.Load(elem, false);

            if (log.IsDebugEnabled)
            {
                log.Debug("Testing JSBSim Initial Conditions: Velocity");
            }

            //Checks values 
            Assert.AreEqual(10.0, IC.GetUBodyFpsIC(), tolerance);
            Assert.AreEqual(20.0, IC.GetVBodyFpsIC(), tolerance);
            Assert.AreEqual(30.0, IC.GetWBodyFpsIC(), tolerance);
        }


        private XmlElement BuildXmlConfig(string config)
        {
            XmlDocument doc = new XmlDocument();
            Stream configStream = new MemoryStream(Encoding.Unicode.GetBytes(config));
            // Create a validating reader arround a text reader for the file stream
            XmlReader xmlReader = new XmlTextReader(configStream);

            // load the data into the dom
            doc.Load(xmlReader);
            XmlNodeList childNodes = doc.GetElementsByTagName("initialize");

            return childNodes[0] as XmlElement;
        }
    }
}
