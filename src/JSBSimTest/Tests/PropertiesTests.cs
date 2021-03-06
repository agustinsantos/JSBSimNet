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

    using NUnit.Framework;

    using JSBSim;
    using JSBSim.InputOutput;
    using JSBSim.Script;
    using System.Collections.Generic;

    /// <summary>
    /// Some JSBSim  properties, parameters, and functions Tests.
    /// </summary>
    [TestFixture]
    public class PropertiesTests
    {
        private const int maxCnt = 10000000;
        private const double tolerance = 10E-12;

        [Test]
        public void CheckValidName()
        {
            bool isValid = PathComponentUtis.ValidateName("isOk");
            Assert.IsTrue(isValid);

            isValid = PathComponentUtis.ValidateName("is_Ok");
            Assert.IsTrue(isValid);

            isValid = PathComponentUtis.ValidateName("is-Ok");
            Assert.IsTrue(isValid);

            isValid = PathComponentUtis.ValidateName("isOk1");
            Assert.IsTrue(isValid);

            isValid = PathComponentUtis.ValidateName("is.Ok1");
            Assert.IsTrue(isValid);

            isValid = PathComponentUtis.ValidateName("_isOk1");
            Assert.IsTrue(isValid);

            isValid = PathComponentUtis.ValidateName("isOk1Yes");
            Assert.IsTrue(isValid);

            isValid = PathComponentUtis.ValidateName("is*NotOk");
            Assert.IsFalse(isValid);

            isValid = PathComponentUtis.ValidateName("isNotOk/");
            Assert.IsFalse(isValid);

            isValid = PathComponentUtis.ValidateName("2isNotOk");
            Assert.IsFalse(isValid);

            isValid = PathComponentUtis.ValidateName("-isNotOk");
            Assert.IsFalse(isValid);

            isValid = PathComponentUtis.ValidateName(".isNotOk");
            Assert.IsFalse(isValid);
        }

        [Test]
        public void CheckParseName()
        {
            int i = 0;

            string name = PathComponentUtis.ParseName(".", ref i);
            Assert.AreEqual(".", name);

            i = 0;
            name = PathComponentUtis.ParseName("..", ref i);
            Assert.AreEqual("..", name);

            i = 0;
            name = PathComponentUtis.ParseName("aName", ref i);
            Assert.AreEqual("aName", name);

            i = 0;
            name = PathComponentUtis.ParseName("aName2", ref i);
            Assert.AreEqual("aName2", name);

            i = 0;
            name = PathComponentUtis.ParseName("aName-2", ref i);
            Assert.AreEqual("aName-2", name);

            i = 0;
            name = PathComponentUtis.ParseName("aName[0]", ref i);
            Assert.AreEqual("aName", name);

            i = 0;
            name = PathComponentUtis.ParseName("aName/other", ref i);
            Assert.AreEqual("aName", name);

            i = 0;
            name = PathComponentUtis.ParseName("aName[0]/other", ref i);
            Assert.AreEqual("aName", name);
        }

        [Test]
        public void CheckParseIndex()
        {
            int i = 0;

            int index = PathComponentUtis.ParseIndex("[10]", ref i);
            Assert.AreEqual(10, index);
            Assert.AreEqual(4, i);

            i = 1;
            index = PathComponentUtis.ParseIndex(" [20]", ref i);
            Assert.AreEqual(20, index);
            Assert.AreEqual(5, i);

            i = 2;
            index = PathComponentUtis.ParseIndex("  [30]", ref i);
            Assert.AreEqual(30, index);
            Assert.AreEqual(6, i);
        }

        [Test]
        public void CheckParsePath()
        {
            List<PathComponent> components = new List<PathComponent>();

            PathComponentUtis.ParsePath("entry", components);
            Assert.AreEqual(1, components.Count);
            Assert.AreEqual("entry", components[0].name);
            Assert.AreEqual(0, components[0].index);

            components.Clear();
            PathComponentUtis.ParsePath("en-t.r_y", components);
            Assert.AreEqual(1, components.Count);
            Assert.AreEqual("en-t.r_y", components[0].name);
            Assert.AreEqual(0, components[0].index);

            components.Clear();
            PathComponentUtis.ParsePath("entry[2]", components);
            Assert.AreEqual(1, components.Count);
            Assert.AreEqual("entry", components[0].name);
            Assert.AreEqual(2, components[0].index);

            components.Clear();
            PathComponentUtis.ParsePath("entry01/entry02/entry03", components);
            Assert.AreEqual(3, components.Count);
            Assert.AreEqual("entry01", components[0].name);
            Assert.AreEqual(0, components[0].index);
            Assert.AreEqual("entry02", components[1].name);
            Assert.AreEqual(0, components[1].index);
            Assert.AreEqual("entry03", components[2].name);
            Assert.AreEqual(0, components[2].index);

            components.Clear();
            PathComponentUtis.ParsePath("entry01[5]/entry02[10]", components);
            Assert.AreEqual(2, components.Count);
            Assert.AreEqual("entry01", components[0].name);
            Assert.AreEqual(5, components[0].index);
            Assert.AreEqual("entry02", components[1].name);
            Assert.AreEqual(10, components[1].index);

            components.Clear();
            PathComponentUtis.ParsePath("../entry02[10]", components);
            Assert.AreEqual(2, components.Count);
            Assert.AreEqual("..", components[0].name);
            Assert.AreEqual(-1, components[0].index);
            Assert.AreEqual("entry02", components[1].name);
            Assert.AreEqual(10, components[1].index);
        }

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
        public void CheckPropertyName01()
        {
            PropertyManager propertyManager = new PropertyManager();

            ClassWithProperties class1 = new ClassWithProperties();
            class1.Bind("c1", propertyManager);

            PropertyNode intNode1 = propertyManager.GetPropertyNode("/c1/AnIntProperty");
            Assert.AreEqual("AnIntProperty", intNode1.GetName());
            Assert.AreEqual("AnIntProperty", intNode1.GetPrintableName());
            Assert.AreEqual("/c1/AnIntProperty", intNode1.GetFullyQualifiedName());
            Assert.AreEqual("AnIntProperty", intNode1.GetDisplayName(true));
            Assert.AreEqual("AnIntProperty[0]", intNode1.GetDisplayName(false));
            Assert.AreEqual("/c1/AnIntProperty", intNode1.GetPath(true));
            Assert.AreEqual("/c1[0]/AnIntProperty[0]", intNode1.GetPath(false));
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
            PropertyNode boolNode1 = propertyManager.GetPropertyNode("c1/ABoolProperty");
            stringNode1.Set("Hello World!");
            intNode1.Set(10);
            doubleNode1.Set(10.123);
            floatNode1.Set(10.321f);
            boolNode1.Set(true);

            PropertyNode stringNode2 = propertyManager.GetPropertyNode("c2/AStringProperty");
            PropertyNode intNode2 = propertyManager.GetPropertyNode("c2/AnIntProperty");
            PropertyNode doubleNode2 = propertyManager.GetPropertyNode("c2/ADoubleProperty");
            PropertyNode floatNode2 = propertyManager.GetPropertyNode("c2/AFloatProperty");
            PropertyNode boolNode2 = propertyManager.GetPropertyNode("c2/ABoolProperty");
            stringNode2.Set("Goodbye World!");
            intNode2.Set(20);
            doubleNode2.Set(20.123);
            floatNode2.Set(20.321f);
            boolNode2.Set(true);

            Assert.AreEqual("Hello World!", stringNode1.Get());
            Assert.AreEqual(10, intNode1.Get());
            Assert.AreEqual(10.123, doubleNode1.Get());
            Assert.AreEqual(10.321f, floatNode1.Get());
            Assert.AreEqual(true, boolNode1.Get());

            Assert.AreEqual("Goodbye World!", stringNode2.Get());
            Assert.AreEqual(20, intNode2.Get());
            Assert.AreEqual(20.123, doubleNode2.Get());
            Assert.AreEqual(20.321f, floatNode2.Get());
            Assert.AreEqual(true, boolNode2.Get());

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
            PropertyNode boolNode1 = propertyManager.GetPropertyNode("ABoolProperty");
            stringNode1.Set("Hello World!");
            intNode1.Set(10);
            doubleNode1.Set(10.123);
            floatNode1.Set(10.321f);
            boolNode1.Set(true);

            Assert.AreEqual("Hello World!", stringNode1.Get());
            Assert.AreEqual(10, intNode1.Get());
            Assert.AreEqual(10.123, doubleNode1.Get());
            Assert.AreEqual(10.321f, floatNode1.Get());
            Assert.AreEqual(true, boolNode1.Get());
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

        [ScriptAttribute("ABoolProperty", "A Bool property")]
        public bool PropertyBool
        {
            get { return this.propertyBool; }
            set { this.propertyBool = value; }
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


        private int propertyInt;
        private float propertyFloat;
        private double propertyDouble;
        private string propertyString;
        private bool propertyBool;
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
