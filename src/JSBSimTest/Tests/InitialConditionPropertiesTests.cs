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
    /// Some Initial Conditial Tests: InputOutput.
    /// </summary>
    [TestFixture]
    public class InitialConditionPropertiesTests
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
        public void CheckPositionAttributes()
        {
            string testIC =
                @"<?xml version=""1.0""?>
                    <initialize name=""reset00"">
                      <!--
                        some comments.
                      -->
                      <latitude unit=""DEG"">   3.0  </latitude>
                      <longitude unit=""DEG"">  7.0  </longitude>
                      <altitude unit=""FT"">    29.0  </altitude>
                    </initialize>";
            
            string testProperties =
                @"<?xml version=""1.0""?>
                  <?xml-stylesheet href=""JSBSim.xsl"" type=""application/xml""?>
                    <function NAME=""aero/coefficient/ClDf2"">
                        <sum>
                          <property>ic/lat-gc-deg</property>
                          <property>ic/long-gc-deg</property>
                          <property>ic/h-sl-ft</property>
                        </sum>
                    </function>";

            FDMExecutive fdm = new FDMExecutive();

            XmlElement elemIc = BuildXmlConfig(testIC, "initialize");
            InitialCondition IC = fdm.GetIC;
            IC.Load(elemIc, false);

            if (log.IsDebugEnabled)
            {
                log.Debug("Testing JSBSim IC InputOutput: Lat., Lon., Alt.");
            }

            //Checks values 
            Assert.AreEqual(3.0, IC.LatitudeDegIC, "Cheking latitude in deg. If you have an error, try to change USEJSBSIM in CommonUtils.MathLib.Constants");
            Assert.AreEqual(7.0, IC.LongitudeDegIC, "Cheking Longitude in deg.If you have an error, try to change USEJSBSIM in CommonUtils.MathLib.Constants");
            Assert.AreEqual(29.0, IC.AltitudeFtIC, "Cheking Altitude in Ft.If you have an error, try to change USEJSBSIM in CommonUtils.MathLib.Constants");

            XmlElement elemFunction = BuildXmlConfig(testProperties, "function");
            Function func = new Function(fdm.PropertyManager, elemFunction);

            //Checks InputOutput 
            Assert.AreEqual(IC.LatitudeDegIC + IC.LongitudeDegIC + IC.AltitudeFtIC, func.GetValue());
        }

        [Test]
        public void CheckOrientationAttributes()
        {
            string testIC =
                @"<?xml version=""1.0""?>
                    <initialize name=""reset00"">
                      <!--
                        some comments.
                      -->
                      <alpha unit=""DEG"">  10.0  </alpha>
                      <beta unit=""DEG"">  20.0  </beta>
                      <theta unit=""DEG"">  30.0  </theta>
                      <phi unit=""DEG"">  40.0  </phi>
                      <psi unit=""DEG"">  50.0  </psi>
                    </initialize>";

            string testProperties =
                @"<?xml version=""1.0""?>
                  <?xml-stylesheet href=""JSBSim.xsl"" type=""application/xml""?>
                    <function NAME=""aero/coefficient/ClDf2"">
                        <sum>
                          <property>ic/alpha-deg</property>
                          <property>ic/beta-deg</property>
                          <property>ic/theta-deg</property>
                          <property>ic/phi-deg</property>
                          <property>ic/psi-true-deg</property>
                        </sum>
                    </function>";

            FDMExecutive fdm = new FDMExecutive();

            XmlElement elemIc = BuildXmlConfig(testIC, "initialize");
            InitialCondition IC = fdm.GetIC;
            IC.Load(elemIc, false);

            if (log.IsDebugEnabled)
            {
                log.Debug("Testing JSBSim IC InputOutput: Orientation Attributes.");
            }

            //Checks values 
            Assert.AreEqual(10.0, IC.AlphaDegIC, "Cheking Alpha in deg. If you have an error, try to change USEJSBSIM in CommonUtils.MathLib.Constants");
            Assert.AreEqual(20.0, IC.BetaDegIC, "Cheking Beta in deg. If you have an error, try to change USEJSBSIM in CommonUtils.MathLib.Constants");
            Assert.AreEqual(30.0, IC.ThetaDegIC, "Cheking Theta in deg. If you have an error, try to change USEJSBSIM in CommonUtils.MathLib.Constants");
            Assert.AreEqual(40.0, IC.PhiDegIC, "Cheking Phi in deg. If you have an error, try to change USEJSBSIM in CommonUtils.MathLib.Constants");
            Assert.AreEqual(50.0, IC.PsiDegIC, "Cheking Psi in deg. If you have an error, try to change USEJSBSIM in CommonUtils.MathLib.Constants");

            XmlElement elemFunction = BuildXmlConfig(testProperties, "function");
            Function func = new Function(fdm.PropertyManager, elemFunction);

            //Checks InputOutput 
            Assert.AreEqual(IC.AlphaDegIC + IC.BetaDegIC + IC.ThetaDegIC + IC.PhiDegIC + IC.PsiDegIC, func.GetValue());
        }

        private XmlElement BuildXmlConfig(string config, string tag)
        {
            XmlDocument doc = new XmlDocument();
            Stream configStream = new MemoryStream(Encoding.Unicode.GetBytes(config));
            // Create a validating reader arround a text reader for the file stream
            XmlReader xmlReader = new XmlTextReader(configStream);

            // load the data into the dom
            doc.Load(xmlReader);
            XmlNodeList childNodes = doc.GetElementsByTagName(tag);

            return childNodes[0] as XmlElement;
        }
    }
}
