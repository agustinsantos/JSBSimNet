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

namespace JSBSim.Models.FlightControl
{
	using System;
	using System.Xml;
	using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

	// Import log4net classes.
	using log4net;

    using CommonUtils.IO;
    using JSBSim.Models;
    using JSBSim.InputOutput;
    using JSBSim.Format;

	/// <summary>
	/// Encapsulates a switch for the flight control system.
	/// 
	/// The SWITCH component models a switch - either on/off or a multi-choice rotary
	/// switch. The switch can represent a physical cockpit switch, or can represent a
	/// logical switch, where several conditions might need to be satisfied before a
	/// particular state is reached. The VALUE of the switch - the output value for the
	/// component - is chosen depending on the state of the switch. Each switch is
	/// comprised of two or more TESTs. Each TEST has a VALUE associated with it. The
	/// first TEST that evaluates to TRUE will set the output value of the switch
	/// according to the VALUE parameter belonging to that TEST. Each TEST contains one
	/// or more CONDITIONS, which each must be logically related (if there are more than
	/// one) given the value of the LOGIC parameter, and which takes the form:
	/// 
	/// property conditional property|value
	/// 
	/// e.g.
	/// 
	/// qbar GE 21.0
	/// 
	/// or,
	/// 
	/// roll_rate < pitch_rate
	/// 
	/// Within a TEST, a CONDITION_GROUP can be specified. A CONDITION_GROUP allows for
	/// complex groupings of logical comparisons. Each CONDITION_GROUP contains
	/// additional conditions, as well as possibly additional CONDITION_GROUPs.
	/// 
	/// <pre>
	/// \<COMPONENT NAME="switch1" TYPE="SWITCH"\>
	/// \<TEST LOGIC="{AND|OR|DEFAULT}" VALUE="{property|value}"\>
	/// {property} {conditional} {property|value}
	/// \<CONDITION_GROUP LOGIC="{AND|OR}"\>
	/// {property} {conditional} {property|value}
	/// ...
	/// \</CONDITION_GROUP\>
	/// ...
	/// \</TEST>
	/// \<TEST LOGIC="{AND|OR}" VALUE="{property|value}"\>
	/// {property} {conditional} {property|value}
	/// ...
	/// \</TEST\>
	/// ...
	/// [OUTPUT \<property>]
	/// \</COMPONENT\>
	/// </pre>
	/// 
	/// Here's an example:
	/// <pre>
	/// \<COMPONENT NAME="Roll A/P Autoswitch" TYPE="SWITCH">
	/// \<TEST LOGIC="DEFAULT" VALUE="0.0">
	/// \</TEST>
	/// \<TEST LOGIC="AND" VALUE="fcs/roll-ap-error-summer">
	/// ap/attitude_hold == 1
	/// \</TEST>
	/// \</COMPONENT>
	/// </pre>
	/// The above example specifies that the default value of the component (i.e. the
	/// output property of the component, addressed by the property, ap/roll-ap-autoswitch)
	/// is 0.0.  If or when the attitude hold switch is selected (property
	/// ap/attitude_hold takes the value 1), the value of the switch component will be
	/// whatever value fcs/roll-ap-error-summer is.
	/// </summary>
	public class Switch : FCSComponent
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

        public Switch(FlightControlSystem fcs, XmlElement element)
            : base(fcs, element)
        {
            string value, logic;
            TestSwitch current_test;

            foreach (XmlNode currentNode in element.ChildNodes)
            {


                if (currentNode.NodeType == XmlNodeType.Element)
                {
                    XmlElement currentElement = (XmlElement)currentNode;

                    current_test = new TestSwitch();
                    if (currentElement.LocalName.Equals("default"))
                    {
                        tests.Add(current_test);
                        current_test.Logic = eLogic.eDefault;

                    }
                    else if (currentElement.LocalName.Equals("test"))
                    {
                        tests.Add(current_test);
                        logic = currentElement.GetAttribute("logic");
                        if (logic.Equals("OR"))
                            current_test.Logic = eLogic.eOR;
                        else if (logic.Equals("AND"))
                            current_test.Logic = eLogic.eAND;
                        else if (logic.Length == 0)
                            current_test.Logic = eLogic.eAND; // default
                        else
                        { // error
                            if (log.IsErrorEnabled)
                                log.Error("Unrecognized LOGIC token " + logic + " in switch component: " + name);
                        }

                        ReaderText rtxt = new ReaderText(new StringReader(currentElement.InnerText));
                        while (rtxt.Done)
                        {
                            string tmp = rtxt.ReadLine().Trim();
                            if (tmp.Length != 0)
                                current_test.conditions.Add(new Condition(tmp, propertyManager));
                        }

                        foreach (XmlNode currentNode2 in currentElement.ChildNodes)
                        {
                            if (currentNode2.NodeType == XmlNodeType.Element)
                            {
                                current_test.conditions.Add(new Condition(currentNode2 as XmlElement, propertyManager));
                            }
                        }
                    }

                    if (!currentElement.LocalName.Equals("output"))
                    {
                        value = currentElement.GetAttribute("value");
                        if (value.Length == 0)
                        {
                            if (log.IsErrorEnabled)
                                log.Error("No VALUE supplied for switch component: " + name);
                        }
                        else
                        {
                            Match match = testRegex.Match(value);
                            if (match.Success)
                            {
                                if (match.Groups["prop"].Value == "") // if true (and execution falls into this block), "value" is a number.
                                {
                                    current_test.OutputVal = double.Parse(value, FormatHelper.numberFormatInfo);
                                }
                                else
                                {
                                    // "value" must be a property if execution passes to here.
                                    if (value[0] == '-')
                                    {
                                        current_test.sign = -1.0;
                                        value = value.Remove(0, 1);
                                    }
                                    else
                                    {
                                        current_test.sign = 1.0;
                                    }
                                    current_test.OutputProp = propertyManager.GetPropertyNode(value);
                                }

                            }
                        }
                    }
                }

            }
            base.Bind();
        }


        public override bool Run()
        {
            bool pass = false;

            foreach (TestSwitch iTests in tests)
            {
                if (iTests.Logic == eLogic.eDefault)
                {
                    output = iTests.GetValue();
                }
                else if (iTests.Logic == eLogic.eAND)
                {
                    pass = true;
                    foreach (Condition iConditions in iTests.conditions)
                    {
                        if (!iConditions.Evaluate())
                            pass = false;
                    }
                }
                else if (iTests.Logic == eLogic.eOR)
                {
                    pass = false;
                    foreach (Condition iConditions in iTests.conditions)
                    {
                        if (iConditions.Evaluate())
                            pass = true;
                    }
                }
                else
                {
                    if (log.IsErrorEnabled)
                        log.Error("Invalid logic test");
                    throw new Exception("Invalid logic test");
                }

                if (pass)
                {
                    output = iTests.GetValue();
                    break;
                }
            }

            Clip();
            if (isOutput) SetOutput();

            return true;
        }

        public enum eLogic {elUndef=0, eAND, eOR, eDefault};
		public enum eComparison {ecUndef=0, eEQ, eNE, eGT, eGE, eLT, eLE};

        private class TestSwitch
        {
            public List<Condition> conditions = new List<Condition>();
            public eLogic Logic = eLogic.elUndef;
            public double OutputVal = 0.0;
            public PropertyNode OutputProp;
            public double sign = 1.0;

            public double GetValue()
            {
                if (OutputProp == null)
                    return OutputVal;
                else
                    return OutputProp.GetDouble() * sign;
            }
        }

        private List<TestSwitch> tests = new List<TestSwitch>();

        private const string testRegExpStr =
            "((?<prop>" + FormatHelper.propertyStr + ")|(?<val>" + FormatHelper.valueStr + "))";
        private static readonly Regex testRegex = new Regex(testRegExpStr, RegexOptions.Compiled);

	}
}
