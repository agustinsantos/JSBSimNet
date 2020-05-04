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

	/// <summary>
	/// Some table Tests: load and access.
	/// </summary>
	[TestFixture]
	public class TableTests
	{
        private const double tolerance = 10E-12;

        [Test]
		public void CheckLoad2D()
		{
			string test = 
				@"<?xml version=""1.0""?>
				  <?xml-stylesheet href=""JSBSim.xsl"" type=""application/xml""?>
				  <table>
					<independentVar lookup=""row"">aero/alpha-rad</independentVar>
					<independentVar lookup=""column"">fcs/flap-pos-deg</independentVar>
					<tableData>
						0.0000	1.0000	25.0000	40.0000	
						-0.0873	0.0041	0.0000	0.0005	0.0014	
						-0.0698	0.0013	0.0004	0.0025	0.0041	
						-0.0524	0.0001	0.0023	0.0059	0.0084	
						-0.0349	0.0003	0.0057	0.0108	0.0141	
						-0.0175	0.0020	0.0105	0.0172	0.0212	
						0.0000	0.0052	0.0168	0.0251	0.0399	
						0.0175	0.0099	0.0248	0.0346	0.0502	
						0.0349	0.0162	0.0342	0.0457	0.0621	
						0.0524	0.0240	0.0452	0.0583	0.0755	
						0.0698	0.0334	0.0577	0.0724	0.0904	
						0.0873	0.0442	0.0718	0.0881	0.1068	
						0.1047	0.0566	0.0874	0.1053	0.1248	
						0.1222	0.0706	0.1045	0.1240	0.1443	
						0.1396	0.0860	0.1232	0.1442	0.1654	
						0.1571	0.0962	0.1353	0.1573	0.1790	
						0.1745	0.1069	0.1479	0.1708	0.1930	
						0.1920	0.1180	0.1610	0.1849	0.2075	
						0.2094	0.1298	0.1746	0.1995	0.2226	
						0.2269	0.1424	0.1892	0.2151	0.2386	
						0.2443	0.1565	0.2054	0.2323	0.2564	
						0.2618	0.1727	0.2240	0.2521	0.2767	
						0.2793	0.1782	0.2302	0.2587	0.2835	
						0.2967	0.1716	0.2227	0.2507	0.2753	
						0.3142	0.1618	0.2115	0.2388	0.2631	
						0.3316	0.1475	0.1951	0.2214	0.2451	
						0.3491	0.1097	0.1512	0.1744	0.1966	
					</tableData>
				</table>";
			PropertyManager propMngr = new PropertyManager();
			ClassWithPropertiesForTables class1 = new ClassWithPropertiesForTables("", propMngr);

			XmlElement elem = BuildXmlConfig(test);
			Table table = new Table(propMngr,elem);

			//Checks values
			//0.3491	0.1097	0.1512	0.1744	0.1966
			class1.alphaRad = 0.3491;
			class1.flapPosDeg = 0.0000;
			Assert.AreEqual(0.1097, table.GetValue(), tolerance);

			class1.flapPosDeg =	1.0000;
			Assert.AreEqual(0.1512, table.GetValue(), tolerance);

			class1.flapPosDeg =	25.0000;
			Assert.AreEqual(0.1744, table.GetValue(), tolerance);

			class1.flapPosDeg = 40.0000;
			Assert.AreEqual(0.1966, table.GetValue(), tolerance);

			//-0.0873	0.0041	0.0000	0.0005	0.0014
			class1.alphaRad = -0.0873;
			class1.flapPosDeg = 0.0000;
			Assert.AreEqual(0.0041, table.GetValue(), tolerance);

			class1.flapPosDeg =	1.0000;
			Assert.AreEqual(0.0000, table.GetValue(), tolerance);

			class1.flapPosDeg =	25.0000;
			Assert.AreEqual(0.0005, table.GetValue(), tolerance);

			class1.flapPosDeg = 40.0000;
			Assert.AreEqual(0.0014, table.GetValue(), tolerance);

			//0.1571	0.0962	0.1353	0.1573	0.1790
			class1.alphaRad = 0.1571;
			class1.flapPosDeg = 0.0000;
			Assert.AreEqual(0.0962, table.GetValue(), tolerance);

			class1.flapPosDeg =	1.0000;
			Assert.AreEqual(0.1353, table.GetValue(), tolerance);

			class1.flapPosDeg =	25.0000;
			Assert.AreEqual(0.1573, table.GetValue(), tolerance);

			class1.flapPosDeg = 40.0000;
			Assert.AreEqual(0.1790, table.GetValue(), tolerance);
			
			//checks limits

			///Key underneath table
			class1.alphaRad = -1.0;
			class1.flapPosDeg = 40.0000;
			Assert.AreEqual(0.0014, table.GetValue(), tolerance);

			///Key over table
			class1.alphaRad = 1.0;
			Assert.AreEqual(0.1966, table.GetValue(), tolerance);

			///Key underneath table
			class1.alphaRad = 0.3491;
			class1.flapPosDeg = -1.0;
			Assert.AreEqual(0.1097, table.GetValue(), tolerance);

			///Key over table
			class1.flapPosDeg = 50.0000;
			Assert.AreEqual(0.1966, table.GetValue(), tolerance);

			//0.1396	0.0860	0.1232	0.1442	0.1654	
			//0.1571	0.0962	0.1353	0.1573	0.1790
			class1.alphaRad = (0.1396+0.1571)/2;
			class1.flapPosDeg = 1.0;
			Assert.AreEqual((0.1232+0.1353)/2, table.GetValue(), tolerance);

			class1.alphaRad = 0.1396;
			class1.flapPosDeg = (1.0+25.0)/2;
			Assert.AreEqual((0.1232+0.1442)/2, table.GetValue(), tolerance);

			class1.alphaRad = (0.1396+0.1571)/2;
			class1.flapPosDeg = (1.0+25.0)/2;
			Assert.AreEqual((0.1232+0.1442+0.1353+0.1573)/4, table.GetValue(), tolerance);
		}


		[Test]
		public void CheckLoad1D()
		{
			string test = 
					@"<?xml version=""1.0""?>
					<?xml-stylesheet href=""JSBSim.xsl"" type=""application/xml""?>
					<table>
                          <independentVar>aero/alpha-rad</independentVar>
                          <tableData>
                              -0.2000	-0.6800	
                              0.0000	0.2000	
                              0.2300	1.2000	
                              0.6000	0.6000
                          </tableData>
                      </table>";
			PropertyManager propMngr = new PropertyManager();
			ClassWithPropertiesForTables class1 = new ClassWithPropertiesForTables("", propMngr);

			XmlElement elem = BuildXmlConfig(test);
			Table table = new Table(propMngr,elem);
			
			//Checks values
			class1.alphaRad = -0.2000;
			Assert.AreEqual(-0.6800, table.GetValue());

			class1.alphaRad = 0.0000;
			Assert.AreEqual(0.2000, table.GetValue(), tolerance);

			class1.alphaRad = 0.2300;
			Assert.AreEqual(1.2000, table.GetValue(), tolerance);

			class1.alphaRad = 0.6000;
			Assert.AreEqual(0.6000, table.GetValue(), tolerance);

			//Checks interpolation
			class1.alphaRad = (0.6000+0.2300)/2;
			Assert.AreEqual((0.6000+1.2000)/2, table.GetValue(), tolerance);

			//checks limits

			///Key underneath table
			class1.alphaRad = -1.0;
			Assert.AreEqual(-0.6800, table.GetValue(), tolerance);

			///Key over table
			class1.alphaRad = 1.0;
			Assert.AreEqual(0.6000, table.GetValue(), tolerance);
		}






		private XmlElement BuildXmlConfig(string config)
		{
            XmlDocument doc = new XmlDocument();
            Stream configStream = new MemoryStream(Encoding.Unicode.GetBytes(config));

            XmlReader xmlReader = new XmlTextReader(configStream);
            // load the data into the dom
            doc.Load(xmlReader);
            xmlReader.Close();
            
            XmlNodeList childNodes = doc.GetElementsByTagName("table");

			return childNodes[0] as XmlElement;
		}
	}
	
	/// <summary>
	///  A class with some properties to be tested within table.
	/// </summary>
	public class ClassWithPropertiesForTables
	{		
		public ClassWithPropertiesForTables(string path, PropertyManager propMngr)
		{
			propMngr.Bind(path, this);
		}

		[ScriptAttribute("aero/alpha-rad", "A test property")]
		public double AlphaRad
		{
			get { return this.alphaRad; }
			set { this.alphaRad = value; }
		}

		[ScriptAttribute("fcs/flap-pos-deg", "A test property")]
		public double FlapPosDeg
		{
			get { return this.flapPosDeg; }
			set { this.flapPosDeg = value; }
		}


		public double alphaRad = 1.0;
		public double flapPosDeg = 1.0;
	}
}
