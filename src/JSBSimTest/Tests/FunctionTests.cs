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
    /// Some Function Tests: load and access.
    /// </summary>
    [TestFixture]
    public class FunctionTests
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
                log.Debug("Starting JSBSim Functions tests");
            }

        }

        [Test]
        public void CheckFunctionProduct()
        {
            string test =
                @"<?xml version=""1.0""?>
                  <?xml-stylesheet href=""JSBSim.xsl"" type=""application/xml""?>
                  <function name=""aero/coefficient/CnDr"">
                <description>
                   Yaw Coefficient due to rudder(DATCOM does not calculate)
                   High value for controllablity, low value
                   for good dynamic stability
                </description>
                <product>
                   <property>aero/qbar-area</property>
                   <property>metrics/bw-ft</property>
                   <property>fcs/rudder-pos-deg</property>
                   <value>  -.1047E-02</value>
                </product>
                </function>";

            FDMExecutive fdmex = new FDMExecutive();
            ClassWithPropertiesForFunctions class1 = new ClassWithPropertiesForFunctions("", fdmex.PropertyManager);

            XmlElement elem = BuildXmlConfig(test);
            Function func = new Function(fdmex, elem);

            if (log.IsDebugEnabled)
            {
                log.Debug("Testing JSBSim Functions: Products");
            }

            //Checks values 
            class1.QbarArea = 0.3491;
            class1.BwFt = 1.0000;
            class1.RudderPosDeg = 1.0;
            Assert.AreEqual(0.3491 * 1.0000 * 1.0 * -.1047E-02, func.GetValue());

            class1.QbarArea = 0.1;
            class1.BwFt = 0.2;
            class1.RudderPosDeg = 0.3;
            Assert.AreEqual(0.1 * 0.2 * 0.3 * -.1047E-02, func.GetValue());

            class1.QbarArea = 1;
            class1.BwFt = 2;
            class1.RudderPosDeg = 3;
            Assert.AreEqual(1 * 2 * 3 * -.1047E-02, func.GetValue());
        }

        [Test]
        public void CheckFunctionQuotient()
        {
            string test =
                @"<?xml version=""1.0""?>
                  <?xml-stylesheet href=""JSBSim.xsl"" type=""application/xml""?>
                    <function NAME=""aero/coefficient/ClDf2"">
                        <quotient>
                          <property>aero/coefficient/CLDf2R</property>
                          <value>833.33</value>
                        </quotient>
                    </function>";

            FDMExecutive fdmex = new FDMExecutive();
            ClassWithPropertiesForFunctions class1 = new ClassWithPropertiesForFunctions("", fdmex.PropertyManager);

            XmlElement elem = BuildXmlConfig(test);
            Function func = new Function(fdmex, elem);

            if (log.IsDebugEnabled)
            {
                log.Debug("Testing JSBSim Functions: quotient");
            }

            //Checks values 
            class1.aeroCLDf2R = 1000.0;
            Assert.AreEqual(1000.0 / 833.33, func.GetValue());

            //Checks values 
            class1.aeroCLDf2R = 0.0;
            Assert.AreEqual(0.0 / 833.33, func.GetValue());
        }

        [Test]
        public void CheckFunctionDifference()
        {
            string test =
                @"<?xml version=""1.0""?>
                  <?xml-stylesheet href=""JSBSim.xsl"" type=""application/xml""?>
                    <function name=""aero/coefficient/ClDf2"">
                        <description>
                           Airbus A380 SYMETRIC Trailing Edge Flaps                                   
                           Roll Moment Coefficient due to Asymetrical Single Slotted Flap Deflection
                           calculated as difference between left and right flap lift coef,
                           times distance from centerline to MAC of surface
                        </description>
                        <difference>
                          <property>aero/coefficient/CLDf2R</property>
                          <property>aero/coefficient/CLDf2L</property>
                        </difference>
                    </function>";

            FDMExecutive fdmex = new FDMExecutive();
            ClassWithPropertiesForFunctions class1 = new ClassWithPropertiesForFunctions("", fdmex.PropertyManager);

            XmlElement elem = BuildXmlConfig(test);
            Function func = new Function(fdmex, elem);

            if (log.IsDebugEnabled)
            {
                log.Debug("Testing JSBSim Functions: Difference");
            }

            //Checks values 
            class1.aeroCLDf2R = 0.1;
            class1.aeroCLDf2L = 0.2;
            Assert.AreEqual(0.1 - 0.2, func.GetValue());

            //Checks values 
            class1.aeroCLDf2R = 0.2;
            class1.aeroCLDf2L = 0.1;
            Assert.AreEqual(0.2 - 0.1, func.GetValue());

            //Checks values 
            class1.aeroCLDf2R = 1;
            class1.aeroCLDf2L = 2;
            Assert.AreEqual(1 - 2, func.GetValue());

            //Checks values 
            class1.aeroCLDf2R = 2;
            class1.aeroCLDf2L = 1;
            Assert.AreEqual(2 - 1, func.GetValue());

        }

        [Test]
        public void CheckFunctionSum()
        {
            string test =
                @"<?xml version=""1.0""?>
                  <?xml-stylesheet href=""JSBSim.xsl"" type=""application/xml""?>
                  <function name=""aero/coefficient/CnDr"">
                    <description>
                       Yaw Coefficient due to rudder(DATCOM does not calculate)
                       High value for controllablity, low value
                       for good dynamic stability
                    </description>
                    <sum>
                       <property>aero/qbar-area</property>
                       <property>metrics/bw-ft</property>
                       <property>fcs/rudder-pos-deg</property>
                       <value>  -.1047E-02</value>
                    </sum>
                </function>";

            FDMExecutive fdmex = new FDMExecutive();
            ClassWithPropertiesForFunctions class1 = new ClassWithPropertiesForFunctions("", fdmex.PropertyManager);

            XmlElement elem = BuildXmlConfig(test);
            Function func = new Function(fdmex, elem);

            if (log.IsDebugEnabled)
            {
                log.Debug("Testing JSBSim Functions: Sums");
            }

            //Checks values 
            class1.QbarArea = 0.3491;
            class1.BwFt = 1.0000;
            class1.RudderPosDeg = 1.0;
            Assert.AreEqual(0.3491 + 1.0000 + 1.0 + -.1047E-02, func.GetValue());

            class1.QbarArea = 0.1;
            class1.BwFt = 0.2;
            class1.RudderPosDeg = 0.3;
            Assert.AreEqual(0.1 + 0.2 + 0.3 + -.1047E-02, func.GetValue());

            class1.QbarArea = 1;
            class1.BwFt = 2;
            class1.RudderPosDeg = 3;
            Assert.AreEqual(1 + 2 + 3 + -.1047E-02, func.GetValue());
        }

        [Test]
        public void CheckFunctionPow()
        {
            string test =
                @"<?xml version=""1.0""?>
                  <?xml-stylesheet href=""JSBSim.xsl"" type=""application/xml""?>
                    <function name=""aero/coefficient/ClDf2"">
                        <pow>
                          <property>aero/coefficient/CLDf2R</property>
                          <property>aero/coefficient/CLDf2L</property>
                        </pow>
                    </function>";

            FDMExecutive fdmex = new FDMExecutive();
            ClassWithPropertiesForFunctions class1 = new ClassWithPropertiesForFunctions("", fdmex.PropertyManager);

            XmlElement elem = BuildXmlConfig(test);
            Function func = new Function(fdmex, elem);

            if (log.IsDebugEnabled)
            {
                log.Debug("Testing JSBSim Functions: Pow");
            }

            //Checks values 
            class1.aeroCLDf2R = 0.1;
            class1.aeroCLDf2L = 0.2;
            Assert.AreEqual(Math.Pow(0.1, 0.2), func.GetValue());

            //Checks values 
            class1.aeroCLDf2R = 0.2;
            class1.aeroCLDf2L = 0.1;
            Assert.AreEqual(Math.Pow(0.2, 0.1), func.GetValue());

            //Checks values 
            class1.aeroCLDf2R = 1;
            class1.aeroCLDf2L = 2;
            Assert.AreEqual(Math.Pow(1, 2), func.GetValue());

            //Checks values 
            class1.aeroCLDf2R = 2;
            class1.aeroCLDf2L = 1;
            Assert.AreEqual(Math.Pow(2, 1), func.GetValue());

        }

        [Test]
        public void CheckFunctionSin()
        {
            string test =
                @"<?xml version=""1.0""?>
                  <?xml-stylesheet href=""JSBSim.xsl"" type=""application/xml""?>
                    <function name=""aero/coefficient/ClDf2"">
                        <sin>
                          <property>aero/coefficient/CLDf2R</property>
                        </sin>
                    </function>";

            FDMExecutive fdmex = new FDMExecutive();
            ClassWithPropertiesForFunctions class1 = new ClassWithPropertiesForFunctions("", fdmex.PropertyManager);

            XmlElement elem = BuildXmlConfig(test);
            Function func = new Function(fdmex, elem);

            if (log.IsDebugEnabled)
            {
                log.Debug("Testing JSBSim Functions: Sin");
            }

            //Checks values 
            class1.aeroCLDf2R = 0.1;
            Assert.AreEqual(Math.Sin(0.1), func.GetValue());

            //Checks values 
            class1.aeroCLDf2R = 0.0;
            Assert.AreEqual(Math.Sin(0.0), func.GetValue());
        }

        [Test]
        public void CheckFunctionMixProductDifference()
        {
            string test =
                @"<?xml version=""1.0""?>
                  <?xml-stylesheet href=""JSBSim.xsl"" type=""application/xml""?>
                    <function name=""aero/coefficient/ClDf2"">
                        <description>
                           Airbus A380 SYMETRIC Trailing Edge Flaps                                   
                           Roll Moment Coefficient due to Asymetrical Single Slotted Flap Deflection
                           calculated as difference between left and right flap lift coef,
                           times distance from centerline to MAC of surface
                        </description>
                        <product>
                           <value>     33.74</value>
                           <difference>
                              <property>aero/coefficient/CLDf2R</property>
                              <property>aero/coefficient/CLDf2L</property>
                           </difference>
                        </product>
                    </function>";

            FDMExecutive fdmex = new FDMExecutive();
            ClassWithPropertiesForFunctions class1 = new ClassWithPropertiesForFunctions("", fdmex.PropertyManager);

            XmlElement elem = BuildXmlConfig(test);
            Function func = new Function(fdmex, elem);

            if (log.IsDebugEnabled)
            {
                log.Debug("Testing JSBSim Functions: Products with difference");
            }

            //Checks values 
            class1.CLDf2R = 0.3491;
            class1.CLDf2L = 1.0000;
            Assert.AreEqual(33.74 * (0.3491 - 1.0), func.GetValue());
        }

        [Test]
        public void CheckFunctionMix2()
        {
            string test =
                @"<?xml version=""1.0""?>
                  <?xml-stylesheet href=""JSBSim.xsl"" type=""application/xml""?>
                    <function>
                      <quotient>
                        <difference>
                          <value>25.70</value>
                          <quotient>
                            <property>aero/coefficient/CLDf2R</property>
                            <value>833.33</value>
                          </quotient>
                        </difference>
                        <value>100.0</value>
                      </quotient>
                    </function>";

            FDMExecutive fdmex = new FDMExecutive();
            ClassWithPropertiesForFunctions class1 = new ClassWithPropertiesForFunctions("", fdmex.PropertyManager);

            XmlElement elem = BuildXmlConfig(test);
            Function func = new Function(fdmex, elem);

            if (log.IsDebugEnabled)
            {
                log.Debug("Testing JSBSim Functions: Products with difference");
            }

            //Checks values 
            class1.CLDf2R = 0.3491;
            class1.CLDf2L = 1.0000;
            Assert.AreEqual((25.70 - class1.CLDf2R / 833.33) / (100.0), func.GetValue());
        }

        private XmlElement BuildXmlConfig(string config)
        {
            XmlDocument doc = new XmlDocument();
            Stream configStream = new MemoryStream(Encoding.Unicode.GetBytes(config));

            XmlReader xmlReader = new XmlTextReader(configStream);
            // load the data into the dom
            doc.Load(xmlReader);
            xmlReader.Close();

            XmlNodeList childNodes = doc.GetElementsByTagName("function");

            return childNodes[0] as XmlElement;
        }

        //TODO
        //To test: Cos, Acos, Asi, etc...
        //function within a function
    }

    /// <summary>
    ///  A class with some properties to be tested within function.
    /// </summary>
    public class ClassWithPropertiesForFunctions
    {
        public ClassWithPropertiesForFunctions(string path, PropertyManager propMngr)
        {
            propMngr.Bind(path, this);
        }

        [ScriptAttribute("aero/qbar-area", "A test property")]
        public double QbarArea
        {
            get { return this.qbarArea; }
            set { this.qbarArea = value; }
        }

        [ScriptAttribute("metrics/bw-ft", "A test property")]
        public double BwFt
        {
            get { return this.bwFt; }
            set { this.bwFt = value; }
        }

        [ScriptAttribute("fcs/rudder-pos-deg", "A test property")]
        public double RudderPosDeg
        {
            get { return this.rudderPosDeg; }
            set { this.rudderPosDeg = value; }
        }

        [ScriptAttribute("aero/coefficient/CLDf2R", "A test property")]
        public double CLDf2R
        {
            get { return this.aeroCLDf2R; }
            set { this.aeroCLDf2R = value; }
        }

        [ScriptAttribute("aero/coefficient/CLDf2L", "A test property")]
        public double CLDf2L
        {
            get { return this.aeroCLDf2L; }
            set { this.aeroCLDf2L = value; }
        }

        public double qbarArea = 1.0;
        public double bwFt = 1.0;
        public double rudderPosDeg = 1.0;
        public double aeroCLDf2R = 1.0;
        public double aeroCLDf2L = 1.0;
    }
}
