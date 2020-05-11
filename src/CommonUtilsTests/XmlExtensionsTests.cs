#region Copyright(C)  Licensed under GNU GPL.
/// Copyright (C) 2005-2006 Agustin Santos Mendez
/// 
/// JSBSim was developed by Jon S. Berndt, Tony Peden, and
/// David Megginson. 
/// Agustin Santos Mendez implemented and maintains this C# version.
/// 
/// This program is free software; you can redistribute it and/or
///  modify it under the terms of the GNU General Public License
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
#region Identification
/// $Id:$
#endregion
namespace CommonUtils.Tests
{
    using System;

    using NUnit.Framework;

    using CommonUtils.MathLib;
    using System.Xml;
    using System.IO;
    using System.Text;
    using System.Xml.Linq;
    using System.Collections.Generic;
    using CommonUtils.IO;

    /// <summary>
    /// some tests for XmlExtensions .
    /// </summary>
    [TestFixture]
    public class XmlExtensionsTests
    {
        [Test]
        public void CheckReadFrom01()
        {
            string test =
                @"<?xml version=""1.0""?>
                  <?xml-stylesheet href=""JSBSim.xsl"" type=""application/xml""?>
                    <function name=""aero/coefficient/ClDf2"">
                        <quotient>
                          <property>aero/coefficient/CLDf2R</property>
                          <value>833.33</value>
                        </quotient>
                    </function>";

            XDocument doc = BuildXmlConfig(test);
            IEnumerable<XElement> properties = doc.Descendants("property");
            foreach (XElement property in properties)
            {
                Assert.AreEqual("In line 5: position 28\n", property.ReadFrom());
            }
        }

        [Test]
        public void CheckReadFrom02()
        {
            string test =
                @"<?xml version=""1.0""?>" + "\n" +
                @"<?xml-stylesheet href=""JSBSim.xsl"" type=""application/xml""?>" + "\n" +
                @"<function name=""aero/coefficient/ClDf2"">" + "\n" +
                @"    <quotient>" + "\n" +
                @"        <property>aero/coefficient/CLDf2R</property>" + "\n" +
                @"            <value>833.33</value>" + "\n" +
                @"    </quotient>" + "\n" +
                @"</function>";

            XDocument doc = BuildXmlConfig(test);
            IEnumerable<XElement> properties = doc.Descendants("property");
            foreach (XElement property in properties)
            {
                Assert.AreEqual("In line 5: position 10\n", property.ReadFrom());
            }
        }
        private XDocument BuildXmlConfig(string config)
        {
            Stream configStream = new MemoryStream(Encoding.Unicode.GetBytes(config));

            XmlReader xmlReader = new XmlTextReader(configStream);

            // load the data into the dom
            XDocument doc = XDocument.Load(xmlReader, LoadOptions.SetBaseUri | LoadOptions.SetLineInfo);
            xmlReader.Close();

            return doc;
        }
    }
}
