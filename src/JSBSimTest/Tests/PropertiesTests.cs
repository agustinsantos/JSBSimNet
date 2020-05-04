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

    using NUnit.Framework;

    using JSBSim;
    using JSBSim.InputOutput;
    using JSBSim.Script;

    /// <summary>
    /// Some JSBSim  properties, parameters, and functions Tests.
    /// </summary>
    [TestFixture]
    public class PropertiesTests
    {
        private const int maxCnt = 10000000;
        private const double tolerance = 10E-12;

        [Test]
        public void CheckPropertyString()
        {
            PropertyManager propertyManager = new PropertyManager();

            ClassWithProperties class1 = new ClassWithProperties();
            class1.Bind("c1", propertyManager);

            PropertyNode stringNode1 = propertyManager.GetPropertyNode("c1/AStringProperty");
            stringNode1.Set("Hello World!");
            Assert.AreEqual("Hello World!", stringNode1.Get());
        }

        [Test]
        public void CheckPropertyInt()
        {
            PropertyManager propertyManager = new PropertyManager();

            ClassWithProperties class1 = new ClassWithProperties();
            class1.Bind("c1", propertyManager);

            PropertyNode intNode1 = propertyManager.GetPropertyNode("c1/AnIntProperty");
            intNode1.Set(10);
            Assert.AreEqual(10, intNode1.Get());
        }

        [Test]
        public void CheckPropertyDouble()
        {
            PropertyManager propertyManager = new PropertyManager();

            ClassWithProperties class1 = new ClassWithProperties();
            class1.Bind("c1", propertyManager);

            PropertyNode doubleNode1 = propertyManager.GetPropertyNode("c1/ADoubleProperty");
            doubleNode1.Set(20.123);
            Assert.AreEqual(20.123, doubleNode1.Get());
        }

        [Test]
        public void CheckPropertyFloat()
        {
            PropertyManager propertyManager = new PropertyManager();

            ClassWithProperties class1 = new ClassWithProperties();
            class1.Bind("c1", propertyManager);

            PropertyNode floatNode1 = propertyManager.GetPropertyNode("c1/AFloatProperty");
            floatNode1.Set(20.123f);
            Assert.AreEqual(20.123f, floatNode1.Get());
        }

        [Test]
        public void CheckPropertyClass1()
        {
            PropertyManager propertyManager = new PropertyManager();

            ClassWithProperties class1 = new ClassWithProperties();
            ClassWithProperties class2 = new ClassWithProperties();
            class1.Bind("c1", propertyManager);
            class2.Bind("c2", propertyManager);

            PropertyNode stringNode1 = propertyManager.GetPropertyNode("c1/AStringProperty");
            PropertyNode intNode1 = propertyManager.GetPropertyNode("c1/AnIntProperty");
            PropertyNode doubleNode1 = propertyManager.GetPropertyNode("c1/ADoubleProperty");
            PropertyNode floatNode1 = propertyManager.GetPropertyNode("c1/AFloatProperty");
            stringNode1.Set("Hello World!");
            intNode1.Set(10);
            doubleNode1.Set(10.123);
            floatNode1.Set(10.321f);

            PropertyNode stringNode2 = propertyManager.GetPropertyNode("c2/AStringProperty");
            PropertyNode intNode2 = propertyManager.GetPropertyNode("c2/AnIntProperty");
            PropertyNode doubleNode2 = propertyManager.GetPropertyNode("c2/ADoubleProperty");
            PropertyNode floatNode2 = propertyManager.GetPropertyNode("c2/AFloatProperty");
            stringNode2.Set("Goodbye World!");
            intNode2.Set(20);
            doubleNode2.Set(20.123);
            floatNode2.Set(20.321f);

            Assert.AreEqual("Hello World!", stringNode1.Get());
            Assert.AreEqual(10, intNode1.Get());
            Assert.AreEqual(10.123, doubleNode1.Get());
            Assert.AreEqual(10.321f, floatNode1.Get());

            Assert.AreEqual("Goodbye World!", stringNode2.Get());
            Assert.AreEqual(20, intNode2.Get());
            Assert.AreEqual(20.123, doubleNode2.Get());
            Assert.AreEqual(20.321f, floatNode2.Get());

        }

        [Test]
        public void CheckPropertyClassNullPath()
        {
            PropertyManager propertyManager = new PropertyManager();

            ClassWithProperties class1 = new ClassWithProperties();
            class1.Bind("", propertyManager);

            PropertyNode stringNode1 = propertyManager.GetPropertyNode("AStringProperty");
            PropertyNode intNode1 = propertyManager.GetPropertyNode("AnIntProperty");
            PropertyNode doubleNode1 = propertyManager.GetPropertyNode("ADoubleProperty");
            PropertyNode floatNode1 = propertyManager.GetPropertyNode("AFloatProperty");
            stringNode1.Set("Hello World!");
            intNode1.Set(10);
            doubleNode1.Set(10.123);
            floatNode1.Set(10.321f);

            Assert.AreEqual("Hello World!", stringNode1.Get());
            Assert.AreEqual(10, intNode1.Get());
            Assert.AreEqual(10.123, doubleNode1.Get());
            Assert.AreEqual(10.321f, floatNode1.Get());
        }

        [Test]
        public void CheckPropertyClass2()
        {
            PropertyManager propertyManager = new PropertyManager();

            ClassWithProperties2 class1 = new ClassWithProperties2();
            ClassWithProperties2 class2 = new ClassWithProperties2();
            class1.Bind("c2_1", propertyManager);
            class2.Bind("c2_2", propertyManager);

            PropertyNode stringNode1 = propertyManager.GetPropertyNode("c2_1/AStringProperty");
            PropertyNode intNode1 = propertyManager.GetPropertyNode("c2_1/AnIntProperty");
            PropertyNode doubleNode1 = propertyManager.GetPropertyNode("c2_1/ADoubleProperty");
            PropertyNode floatNode1 = propertyManager.GetPropertyNode("c2_1/AFloatProperty");
            PropertyNode newIntNode1 = propertyManager.GetPropertyNode("c2_1/ANewIntProperty");
            PropertyNode newDoubleNode1 = propertyManager.GetPropertyNode("c2_1/ANewDoubleProperty");

            stringNode1.Set("Hello World!");
            intNode1.Set(10);
            doubleNode1.Set(10.123);
            floatNode1.Set(10.321f);
            newIntNode1.Set(100);
            newDoubleNode1.Set(100.123);

            PropertyNode stringNode2 = propertyManager.GetPropertyNode("c2_2/AStringProperty");
            PropertyNode intNode2 = propertyManager.GetPropertyNode("c2_2/AnIntProperty");
            PropertyNode doubleNode2 = propertyManager.GetPropertyNode("c2_2/ADoubleProperty");
            PropertyNode floatNode2 = propertyManager.GetPropertyNode("c2_2/AFloatProperty");
            PropertyNode newIntNode2 = propertyManager.GetPropertyNode("c2_2/ANewIntProperty");
            PropertyNode newDoubleNode2 = propertyManager.GetPropertyNode("c2_2/ANewDoubleProperty");

            stringNode2.Set("Goodbye World!");
            intNode2.Set(20);
            doubleNode2.Set(20.123);
            floatNode2.Set(20.321f);
            newIntNode2.Set(200);
            newDoubleNode2.Set(200.123);

            Assert.AreEqual("Hello World!", stringNode1.Get());
            Assert.AreEqual(10, intNode1.Get());
            Assert.AreEqual(10.123, doubleNode1.Get());
            Assert.AreEqual(10.321f, floatNode1.Get());
            Assert.AreEqual(100, newIntNode1.Get());
            Assert.AreEqual(100.123, newDoubleNode1.Get());

            Assert.AreEqual("Goodbye World!", stringNode2.Get());
            Assert.AreEqual(20, intNode2.Get());
            Assert.AreEqual(20.123, doubleNode2.Get());
            Assert.AreEqual(20.321f, floatNode2.Get());
            Assert.AreEqual(200, newIntNode2.Get());
            Assert.AreEqual(200.123, newDoubleNode2.Get());
        }

        [Test]
        public void CheckPropertyOverrideClass()
        {
            PropertyManager propertyManager = new PropertyManager();

            ClassWithOverrideProperties class1 = new ClassWithOverrideProperties("c3_1", propertyManager);

            PropertyNode intNode1 = propertyManager.GetPropertyNode("c3_1/AnIntProperty");
            PropertyNode doubleNode1 = propertyManager.GetPropertyNode("c3_1/ADoubleProperty");

            intNode1.Set(10);
            doubleNode1.Set(10.123);

            Assert.AreEqual(10, intNode1.Get());
            Assert.AreEqual(10.123, doubleNode1.Get());
        }

        [Test]
        public void CheckPropertyStaticClass()
        {
            PropertyManager propertyManager = new PropertyManager(true);

            PropertyNode intNode1 = propertyManager.GetPropertyNode("AStaticIntProperty");
            PropertyNode doubleNode1 = propertyManager.GetPropertyNode("AStaticDoubleProperty");

            intNode1.Set(10);
            doubleNode1.Set(10.123);

            Assert.AreEqual(10, intNode1.Get());
            Assert.AreEqual(10.123, doubleNode1.Get());
        }

        [Test]
        public void CheckPropertyStaticClassDelegate()
        {
            PropertyManager propertyManager = new PropertyManager(true);

            PropertyNode intNode1 = propertyManager.GetPropertyNode("AStaticIntProperty");
            PropertyNode doubleNode1 = propertyManager.GetPropertyNode("AStaticDoubleProperty");

            intNode1.Set(10);
            doubleNode1.Set(10.123);

            PropertyNode.GetInt32ValueDelegate propInt = intNode1.GetInt32Delegate;
            PropertyNode.GetDoubleValueDelegate propDouble = doubleNode1.GetDoubleDelegate;

            Assert.AreEqual(10, propInt());
            Assert.AreEqual(10.123, propDouble());
        }


        [Test]
        public void CheckPropertyIntDelegate()
        {
            PropertyManager propertyManager = new PropertyManager();

            ClassWithProperties class1 = new ClassWithProperties();

            class1.Bind("c1", propertyManager);

            PropertyNode intNode1 = propertyManager.GetPropertyNode("c1/AnIntProperty");
            intNode1.Set(10);

            PropertyNode.GetInt32ValueDelegate prop = intNode1.GetInt32Delegate;
            Assert.AreEqual(10, prop());
        }

        [Test]
        public void CheckPropertyDoubleDelegate()
        {
            PropertyManager propertyManager = new PropertyManager();

            ClassWithProperties class1 = new ClassWithProperties();

            class1.Bind("c1", propertyManager);

            PropertyNode doubleNode1 = propertyManager.GetPropertyNode("c1/ADoubleProperty");
            doubleNode1.Set(10.123456);

            PropertyNode.GetDoubleValueDelegate prop = doubleNode1.GetDoubleDelegate;

            Assert.AreEqual(10.123456, prop());
        }

        [Test]
        public void CheckPropertyOverrideClassDelegates()
        {
            PropertyManager propertyManager = new PropertyManager();

            ClassWithOverrideProperties class1 = new ClassWithOverrideProperties("c3_1", propertyManager);

            PropertyNode intNode1 = propertyManager.GetPropertyNode("c3_1/AnIntProperty");
            PropertyNode doubleNode1 = propertyManager.GetPropertyNode("c3_1/ADoubleProperty");

            intNode1.Set(10);
            doubleNode1.Set(10.123);

            PropertyNode.GetInt32ValueDelegate propInt = intNode1.GetInt32Delegate;
            PropertyNode.GetDoubleValueDelegate propDouble = doubleNode1.GetDoubleDelegate;

            Assert.AreEqual(10, propInt());
            Assert.AreEqual(10.123, propDouble());
        }

        [Test]
        public void CheckPropertyClassPerformance()
        {
            PropertyManager propertyManager = new PropertyManager();

            ClassWithProperties class1 = new ClassWithProperties();
            class1.Bind("", propertyManager);

            PropertyNode doubleNode1 = propertyManager.GetPropertyNode("ADoubleProperty");
            doubleNode1.Set(10.123);

            double val;
            DateTime time = DateTime.Now;
            for (int i = 0; i <= maxCnt; i++)
            {
                val = class1.PropertyInt;
            }
            long mlsg = DateTime.Now.Ticks - time.Ticks;
            Console.WriteLine("Time for Performance test (using property directly) =" + (double)mlsg / 10000000.0f);

            time = DateTime.Now;
            for (int i = 0; i <= maxCnt; i++)
            {
                val = doubleNode1.GetDouble();
            }
            mlsg = DateTime.Now.Ticks - time.Ticks;
            Console.WriteLine("Time for Performance test (using PropertyNode) =" + (double)mlsg / 10000000.0f);

            PropertyNode.GetDoubleValueDelegate prop = doubleNode1.GetDoubleDelegate;

            time = DateTime.Now;
            for (int i = 0; i <= maxCnt; i++)
            {
                val = prop();
            }
            mlsg = DateTime.Now.Ticks - time.Ticks;
            Console.WriteLine("Time for Performance test (using PropertyNode's delegate) =" + (double)mlsg / 10000000.0f);
        }
    }

    /// <summary>
    /// A class with some properties to be tested.
    /// </summary>
    public class ClassWithProperties
    {
        public ClassWithProperties()
        {
        }

        [ScriptAttribute("AnIntProperty", "An int property")]
        public int PropertyInt
        {
            get { return this.propertyInt; }
            set { this.propertyInt = value; }
        }

        [ScriptAttribute("AStringProperty", "A String property")]
        public string PropertyString
        {
            get { return this.propertyString; }
            set { this.propertyString = value; }
        }

        [ScriptAttribute("ADoubleProperty", "A Double property")]
        public double PropertyDouble
        {
            get { return this.propertyDouble; }
            set { this.propertyDouble = value; }
        }

        [ScriptAttribute("AFloatProperty", "A Float property")]
        public float PropertyFloat
        {
            get { return this.propertyFloat; }
            set { this.propertyFloat = value; }
        }

        public int GetPropertyInt()
        {
            return this.propertyInt;
        }

        public double GetDouble()
        {
            return this.propertyDouble;
        }

        public void Bind(string path, PropertyManager propMngr)
        {
            propMngr.Bind(path, this);
        }


        public int propertyInt;
        private float propertyFloat;
        private double propertyDouble;
        private string propertyString;
    }


    /// <summary>
    ///  A class derived from other class with some properties to be tested.
    /// </summary>
    public class ClassWithProperties2 : ClassWithProperties
    {
        public ClassWithProperties2()
        {
        }

        [ScriptAttribute("ANewIntProperty", "A new int property from a derived class")]
        public int NewPropertyInt
        {
            get { return this.newPropertyInt; }
            set { this.newPropertyInt = value; }
        }

        [ScriptAttribute("ANewDoubleProperty", "A new Double property from a derived class")]
        public double NewPropertyDouble
        {
            get { return this.newPropertyDouble; }
            set { this.newPropertyDouble = value; }
        }

        private int newPropertyInt;
        private double newPropertyDouble;
    }

    /// <summary>
    /// A class with some properties to be tested.
    /// </summary>
    public class ClassWithVirtualProperties
    {
        public ClassWithVirtualProperties(string path, PropertyManager propMngr)
        {
            propMngr.Bind(path, this);
        }

        [ScriptAttribute("AnIntProperty", "An int property")]
        public virtual int PropertyInt
        {
            get { return this.propertyInt; }
            set { this.propertyInt = value; }
        }

        [ScriptAttribute("ADoubleProperty", "A Double property")]
        public virtual double PropertyDouble
        {
            get { return this.propertyDouble; }
            set { this.propertyDouble = value; }
        }

        public int propertyInt;
        private double propertyDouble;
    }

    /// <summary>
    ///  A class derived from other class with some overrided properties to be tested.
    ///  ScriptAttribute is defined with Inherited = true, so the overrided properties
    ///  must have the attributes defined.
    /// </summary>
    public class ClassWithOverrideProperties : ClassWithVirtualProperties
    {
        public ClassWithOverrideProperties(string path, PropertyManager propMngr) :
            base(path, propMngr)
        {
        }

        public override int PropertyInt
        {
            get { return this.newPropertyInt; }
            set { this.newPropertyInt = value; }
        }

        public override double PropertyDouble
        {
            get { return this.newPropertyDouble; }
            set { this.newPropertyDouble = value; }
        }

        private int newPropertyInt;
        private double newPropertyDouble;
    }

    /// <summary>
    /// A class with some static properties to be tested.
    /// </summary>
    public class StaticClassWithProperties
    {
        [ScriptAttribute("AStaticIntProperty", "An static int property")]
        public static int PropertyInt
        {
            get { return propertyInt; }
            set { propertyInt = value; }
        }

        [ScriptAttribute("AStaticDoubleProperty", "A static double property")]
        public static double PropertyDouble
        {
            get { return propertyDouble; }
            set { propertyDouble = value; }
        }

        public static int propertyInt;
        private static double propertyDouble;
    }
}
