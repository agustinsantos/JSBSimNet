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
    public class ConditionTests
    {

        [Test]
        public void CheckParser1()
        {
            string test1 = "              aero/qbar-area == 1";
            PropertyManager propMngr = new PropertyManager();
            ClassWithPropertiesForConditions class1 = new ClassWithPropertiesForConditions("", propMngr);

            Condition cond = new Condition(test1, propMngr);
            Assert.AreEqual("qbar-area == 1", cond.ToStringCondition(""));
        }

        [Test]
        public void CheckParser2()
        {
            string test1 = "              aero/qbar-area                      ==                 1              ";
            PropertyManager propMngr = new PropertyManager();
            ClassWithPropertiesForConditions class1 = new ClassWithPropertiesForConditions("", propMngr);

            Condition cond = new Condition(test1, propMngr);
            Assert.AreEqual("qbar-area == 1", cond.ToStringCondition(""));
        }

        [Test]
        public void CheckParser3()
        {
            string test1 = "aero/qbar-area == 1";
            PropertyManager propMngr = new PropertyManager();
            ClassWithPropertiesForConditions class1 = new ClassWithPropertiesForConditions("", propMngr);

            Condition cond = new Condition(test1, propMngr);
            Assert.AreEqual("qbar-area == 1", cond.ToStringCondition(""));
        }

        [Test]
        public void CheckParser4()
        {
            string test1 = "  aero/qbar-area == 1.0000";
            PropertyManager propMngr = new PropertyManager();
            ClassWithPropertiesForConditions class1 = new ClassWithPropertiesForConditions("", propMngr);

            Condition cond = new Condition(test1, propMngr);
            Assert.AreEqual("qbar-area == 1", cond.ToStringCondition(""));
        }

        [Test]
        public void CheckParser5()
        {
            string test1 = "aero/qbar-area == -.1";
            PropertyManager propMngr = new PropertyManager();
            ClassWithPropertiesForConditions class1 = new ClassWithPropertiesForConditions("", propMngr);

            Condition cond = new Condition(test1, propMngr);
            Assert.AreEqual("qbar-area == -0.1", cond.ToStringCondition(""));
        }

        [Test]
        public void CheckParser6()
        {
            string test1 = "aero/qbar-area == -.1e-19";
            PropertyManager propMngr = new PropertyManager();
            ClassWithPropertiesForConditions class1 = new ClassWithPropertiesForConditions("", propMngr);

            Condition cond = new Condition(test1, propMngr);
            Assert.AreEqual("qbar-area == -1E-20", cond.ToStringCondition(""));
        }

        [Test]
        public void CheckParser7()
        {
            string test1 = "aero/qbar-area == -.1e+19";
            PropertyManager propMngr = new PropertyManager();
            ClassWithPropertiesForConditions class1 = new ClassWithPropertiesForConditions("", propMngr);

            Condition cond = new Condition(test1, propMngr);
            Assert.AreEqual("qbar-area == -1E+18", cond.ToStringCondition(""));
        }
        [Test]
        public void CheckParser8()
        {
            string test1 = "aero/qbar-area == .1e+19";
            PropertyManager propMngr = new PropertyManager();
            ClassWithPropertiesForConditions class1 = new ClassWithPropertiesForConditions("", propMngr);

            Condition cond = new Condition(test1, propMngr);
            Assert.AreEqual("qbar-area == 1E+18", cond.ToStringCondition(""));
        }
        [Test]
        public void CheckParser9()
        {
            string test1 = "aero/qbar-area != -0.1e+19";
            PropertyManager propMngr = new PropertyManager();
            ClassWithPropertiesForConditions class1 = new ClassWithPropertiesForConditions("", propMngr);

            Condition cond = new Condition(test1, propMngr);
            Assert.AreEqual("qbar-area != -1E+18", cond.ToStringCondition(""));
        }

        [Test]
        public void CheckParser10()
        {
            string test1 = "aero/qbar-area NE -.12345678901234567890e+19";
            PropertyManager propMngr = new PropertyManager();
            ClassWithPropertiesForConditions class1 = new ClassWithPropertiesForConditions("", propMngr);

            Condition cond = new Condition(test1, propMngr);
            Assert.AreEqual("qbar-area NE -1.23456789012346E+18", cond.ToStringCondition(""));
        }

        [Test]
        public void CheckParser11()
        {
            string test1 = "        aero/qbar-area            NE              metrics/bw-ft                ";
            PropertyManager propMngr = new PropertyManager();
            ClassWithPropertiesForConditions class1 = new ClassWithPropertiesForConditions("", propMngr);

            Condition cond = new Condition(test1, propMngr);
            Assert.AreEqual("qbar-area NE bw-ft", cond.ToStringCondition(""));
        }

        [Test]
        public void CheckParser12()
        {
            string test1 = "aero/qbar-area gt metrics/bw-ft";
            PropertyManager propMngr = new PropertyManager();
            ClassWithPropertiesForConditions class1 = new ClassWithPropertiesForConditions("", propMngr);

            Condition cond = new Condition(test1, propMngr);
            Assert.AreEqual("qbar-area gt bw-ft", cond.ToStringCondition(""));
        }

        [Test]
        public void CheckParser13()
        {
            string test1 = "aero/qbar-area lt metrics/bw-ft";
            PropertyManager propMngr = new PropertyManager();
            ClassWithPropertiesForConditions class1 = new ClassWithPropertiesForConditions("", propMngr);

            Condition cond = new Condition(test1, propMngr);
            Assert.AreEqual("qbar-area lt bw-ft", cond.ToStringCondition(""));
        }

        //TODO test others parser cases (comparations operators or values)
        //TODO test evaluate
        //TODO test nested conditions

    }

    /// <summary>
    ///  A class with some properties to be tested within function.
	/// </summary>
    public class ClassWithPropertiesForConditions
    {
        public ClassWithPropertiesForConditions(string path, PropertyManager propMngr)
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

        public double qbarArea = 1.0;
        public double bwFt = 1.0;

    }
}
