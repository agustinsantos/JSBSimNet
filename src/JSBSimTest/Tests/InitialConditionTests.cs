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
    // Import log4net classes.
    using log4net;

    using JSBSim;
    using JSBSim.MathValues;
    using JSBSim.InputOutput;
    using JSBSim.Script;

    /// <summary>
    /// Some Initial Conditial Tests: load and access.
    /// </summary>
    [TestFixture]
    public class InitialConditionTests
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
                log.Debug("Starting JSBSim IC tests");
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
            InitialCondition IC = fdm.GetIC;
            IC.Load(elem, false);

            if (log.IsDebugEnabled)
            {
                log.Debug("Testing JSBSim Initial Conditions: Lat, long, alt.");
            }

            //Checks values 
            Assert.AreEqual(47.0, IC.LatitudeDegIC, "latitude in deg.");
            Assert.AreEqual(-110.0, IC.LongitudeDegIC, "longitude in deg.");
            Assert.AreEqual(10000.0, IC.AltitudeFtIC, "Altitude in Ft");
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
                      <alpha unit=""DEG"">  10.0  </alpha>
                      <beta unit=""DEG"">  20.0  </beta>
                      <theta unit=""DEG"">  30.0  </theta>
                      <phi unit=""DEG"">  40.0  </phi>
                    </initialize>";

            FDMExecutive fdm = new FDMExecutive();

            XmlElement elem = BuildXmlConfig(test);
            InitialCondition IC = fdm.GetIC;
            IC.Load(elem, false);

            if (log.IsDebugEnabled)
            {
                log.Debug("Testing JSBSim Initial Conditions: Orientation");
            }

            //Checks values 
            Assert.AreEqual(10.0, IC.AlphaDegIC, "initial angle of attack");
            Assert.AreEqual(20.0, IC.BetaDegIC, "initial sideslip angle");
            Assert.AreEqual(30.0, IC.ThetaDegIC, "initial pitch angle");
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
            InitialCondition IC = fdm.GetIC;
            IC.Load(elem, false);

            if (log.IsDebugEnabled)
            {
                log.Debug("Testing JSBSim Initial Conditions: Velocity");
            }

            //Checks values 
            Assert.AreEqual(100.0, IC.UBodyFpsIC);
            Assert.AreEqual(200.0, IC.VBodyFpsIC);
            Assert.AreEqual(300.0, IC.WBodyFpsIC);
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
