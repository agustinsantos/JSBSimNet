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

namespace JSBSim.Models.FlightControl
{
    using System.Collections.Generic;
    using System.Xml;
    using CommonUtils.IO;
    using JSBSim.InputOutput;
    using JSBSim.MathValues;
    using JSBSim.Models;
    // Import log4net classes.
    using log4net;

    /// <summary>
    /// Encapsulates a switch for the flight control system.
    /// 
    /// The switch component models a switch - either on/off or a multi-choice rotary
    /// switch. The switch can represent a physical cockpit switch, or can represent a
    /// logical switch, where several conditions might need to be satisfied before a
    /// particular state is reached.The value of the switch - the output value for the
    /// component - is chosen depending on the state of the switch. Each switch is
    /// comprised of one or more tests.Each test has a value associated with it.The
    /// first test that evaluates to true will set the output value of the switch
    /// according to the value parameter belonging to that test.Each test contains one
    /// or more conditions, which each must be logically related (if there are more than
    /// one) given the value of the logic attribute, and which takes the form:
    /// 
    ///   property conditional property|value
    /// 
    /// e.g.
    /// 
    ///   qbar ge 21.0
    /// 
    /// or,
    /// 
    ///   roll_rate == pitch_rate
    /// 
    /// Within a test, additional tests can be specified, which allows for
    /// complex groupings of logical comparisons.Each test contains
    /// additional conditions, as well as possibly additional tests.
    /// 
    /// @code
    /// <switch name="switch1">
    ///   <default value= "{property|value}" />
    ///   < test logic= "{AND|OR}" value= "{property|value}" >
    ///     { property} {conditional
    /// } {property|value}
    ///     <test logic = "{AND|OR}" >
    ///       {property} {conditional} {property|value}
    ///       ...
    ///     </test>
    ///     ...
    ///   </test>
    ///   <test logic = "{AND|OR}" value="{property|value}">
    ///     {property} {conditional} {property|value}
    ///     ...
    ///   </test>
    ///   ...
    ///   [<output> {property} </output>]
    /// </switch>
    /// @endcode
    /// 
    /// Here's an example:
    /// 
    /// @code
    /// <switch name="roll a/p autoswitch">
    ///   <default value="0.0"/>
    ///   <test value = "fcs/roll-ap-error-summer" >
    ///     ap / attitude_hold == 1
    ///   </ test >
    /// </switch>
    /// @endcode
    /// 
    /// Note: In the "logic" attribute, "AND" is the default logic, if none is supplied.
    /// 
    /// The above example specifies that the default value of the component(i.e.the
    /// output property of the component, addressed by the property,
    /// ap/roll-ap-autoswitch) is 0.0.
    ///
    /// If or when the attitude hold switch is selected(property ap/attitude_hold takes
    /// the value 1), the value of the switch component will be whatever value
    /// fcs/roll-ap-error-summer is.
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
            string value;
            TestSwitch current_test;

            Bind(element); // Bind() this component here in case it is used in its own
                           // definition for a sample-and-hold
            XmlElement test_element = element.FindElement("default");
            if (test_element != null)
            {
                current_test = new TestSwitch();
                value = test_element.GetAttribute("value");
                current_test.SetTestValue(value, name, propertyManager);
                current_test.Default = true;
                double tmp;
                if (delay > 0 && double.TryParse(value, out tmp))
                {        // If there is a delay, initialize the
                    for (int i = 0; i < delay - 1; i++)
                    {  // delay buffer to the default value
                        output_array[i] = tmp;  // for the switch if that value is a number.
                    }
                }
                tests.Add(current_test);
            }

            var nodeList = element.GetElementsByTagName("test");
            foreach (var elem in nodeList)
            {
                if (elem is XmlElement)
                {
                    test_element = elem as XmlElement;
                    current_test = new TestSwitch();
                    current_test.condition = new Condition(test_element, propertyManager);
                    value = test_element.GetAttribute("value");
                    current_test.SetTestValue(value, name, propertyManager);
                    tests.Add(current_test);
                }
            }

            Debug(0);
        }


        public override bool Run()
        {
            bool pass = false;
            double default_output = 0.0;

            // To detect errors early, make sure all conditions and values can be
            // evaluated in the first time step.
            if (!initialized)
            {
                initialized = true;
                VerifyProperties();
            }

            foreach (var test in tests)
            {
                if (test.Default)
                {
                    default_output = test.OutputValue.GetValue();
                }
                else
                {
                    pass = test.condition.Evaluate();
                }

                if (pass)
                {
                    output = test.OutputValue.GetValue();
                    break;
                }
            }

            if (!pass) output = default_output;

            if (delay != 0) Delay();
            Clip();
            SetOutput();

            return true;
        }

        public enum eLogic { elUndef = 0, eAND, eOR, eDefault };
        public enum eComparison { ecUndef = 0, eEQ, eNE, eGT, eGE, eLT, eLE };

        private class TestSwitch
        {
            public Condition condition = null;
            public bool Default = false;
            public IParameter OutputValue;

            // constructor for the test structure
            public TestSwitch() { }

            public void SetTestValue(string value, string name,
                       PropertyManager pm)
            {
                if (string.IsNullOrEmpty(value))
                {
                    log.Error("No VALUE supplied for switch component: " + name);
                }
                else
                    OutputValue = new ParameterValue(value, pm);
            }

            public string GetOutputName()
            {
                return OutputValue.GetName();
            }
        }

        private List<TestSwitch> tests = new List<TestSwitch>();
        private bool initialized = false;

        private void VerifyProperties()
        {
            foreach (var test in tests)
            {
                if (!test.Default)
                {
                    test.condition.Evaluate();
                }
                test.OutputValue.GetValue();
            }
        }
        protected override void Debug(int from) { }
    }
}
