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
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Xml;
    using CommonUtils.IO;
    using JSBSim.Format;
    using JSBSim.InputOutput;
    using JSBSim.MathValues;
    // Import log4net classes.
    using log4net;

    /// <summary>
    /// Encapsulates a condition, which is used in parts of JSBSim including switches
    /// </summary>
    public class Condition
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
        /// This constructor is called when tests are inside an element
        /// </summary>
        /// <param name="element"></param>
        /// <param name="propertyManager"></param>
        public Condition(XmlElement element, PropertyManager propertyManager)
        {
            InitializeConditionals();

            string logic = element.GetAttribute("logic");
            if (!string.IsNullOrEmpty(logic))
                if (logic.Equals("OR"))
                    Logic = eLogic.eOR;
                else if (logic.Equals("AND"))
                    Logic = eLogic.eAND;
                else
                {
                    if (log.IsErrorEnabled)
                        log.Error("Unrecognized LOGIC token " + logic + " in switch component.");
                    throw new Exception("Unrecognized LOGIC token " + logic + " in switch component");
                }
            else
            {
                Logic = eLogic.eAND; // default
            }

            ReaderText rtxt = new ReaderText(new StringReader(element.InnerText));
            while (rtxt.Done)
            {
                string tmp = rtxt.ReadLine().Trim();
                conditions.Add(new Condition(tmp, propertyManager));
            }
            string elName = element.Name;
            foreach (XmlNode currentNode in element.ChildNodes)
            {
                if (currentNode.NodeType == XmlNodeType.Element)
                {
                    XmlElement condition_element = currentNode as XmlElement;
                    string tagName = condition_element.Name;

                    if (tagName != elName)
                    {
                        log.Error("Unrecognized tag <" + tagName + "> in the condition statement.");
                        throw new Exception("Illegal argument");
                    }
                    conditions.Add(new Condition(currentNode as XmlElement, propertyManager));
                }
            }


        }

        /// <summary>
        /// This constructor is called when there are no nested test groups inside the
        /// condition
        /// </summary>
        /// <param name="testStr"></param>
        /// <param name="propertyManager"></param>
        public Condition(string testStr, PropertyManager propertyManager, XmlElement el = null)
        {
            InitializeConditionals();

            Match match = testRegex.Match(testStr);
            if (match.Success)
            {
                if (!string.IsNullOrEmpty(match.Groups["prop2"].Value) || !string.IsNullOrEmpty(match.Groups["val"].Value))
                {
                    TestParam1 = new PropertyValue(match.Groups["prop1"].Value, propertyManager);
                    conditional = match.Groups["cond"].Value;
                    if (!string.IsNullOrEmpty(match.Groups["prop2"].Value))
                        TestParam2 = new ParameterValue(match.Groups["prop2"].Value, propertyManager);
                    else
                        TestParam2 = new ParameterValue(match.Groups["val"].Value, propertyManager);

                    Comparison = mComparison[conditional];
                }
                else
                {
                    log.Error("Conditional test is invalid .");
                    throw new Exception("Error in test condition.");
                }
            }
            else
            {
                if (log.IsErrorEnabled)
                    log.Error("Error parsing Condition: " + testStr);
                throw new ArgumentException("Error parsing Condition: " + testStr);
            }
            if (Comparison == eComparison.ecUndef)
            {
                throw new Exception("Comparison operator: \"" + conditional
                      + "\" does not exist.  Please check the conditional.");
            }
        }

        public bool Evaluate()
        {
            bool pass = false;

            if (TestParam1 == null)
            {

                if (Logic == eLogic.eAND)
                {

                    pass = true;
                    foreach (var cond in conditions)
                    {
                        if (!cond.Evaluate()) pass = false;
                    }

                }
                else
                { // Logic must be eOR

                    pass = false;
                    foreach (var cond in conditions)
                    {
                        if (cond.Evaluate()) pass = true;
                    }

                }

            }
            else
            {

                double compareValue = TestParam2.GetValue();

                switch (Comparison)
                {
                    case eComparison.ecUndef:
                        log.Error("Undefined comparison operator.");
                        break;
                    case eComparison.eEQ:
                        pass = TestParam1.GetDoubleValue() == compareValue;
                        break;
                    case eComparison.eNE:
                        pass = TestParam1.GetDoubleValue() != compareValue;
                        break;
                    case eComparison.eGT:
                        pass = TestParam1.GetDoubleValue() > compareValue;
                        break;
                    case eComparison.eGE:
                        pass = TestParam1.GetDoubleValue() >= compareValue;
                        break;
                    case eComparison.eLT:
                        pass = TestParam1.GetDoubleValue() < compareValue;
                        break;
                    case eComparison.eLE:
                        pass = TestParam1.GetDoubleValue() <= compareValue;
                        break;
                    default:
                        log.Error("Unknown comparison operator.");
                        break;
                }
            }

            return pass;
        }

        public void PrintCondition()
        {
            if (log.IsInfoEnabled)
                log.Info(this.ToString());
        }
        public string ToStringCondition(string indent = "  ")
        {
            String scratch = "";

            if (conditions.Count != 0)
            {

                switch (Logic)
                {
                    case eLogic.elUndef:
                        scratch = " UNSET";
                        log.Error("unset logic for test condition");
                        break;
                    case eLogic.eAND:
                        scratch = indent + "if all of the following are true: {";
                        break;
                    case eLogic.eOR:
                        scratch = indent + "if any of the following are true: {";
                        break;
                    default:
                        scratch = " UNKNOWN";
                        log.Error("Unknown logic for test condition");
                        break;
                }
                scratch += "\n";

                foreach (var cond in conditions)
                {
                    scratch += cond.ToStringCondition(indent + "  ");
                    scratch += "\n";
                }

                scratch += indent + "}";

            }
            else
            {
                scratch += indent + TestParam1.GetName() + " " + conditional
                     + " " + TestParam2.GetName();
            }
            return scratch;
        }

        private enum eComparison { ecUndef = 0, eEQ, eNE, eGT, eGE, eLT, eLE };
        private enum eLogic { elUndef = 0, eAND, eOR };

        private Dictionary<string, eComparison> mComparison = new Dictionary<string, eComparison>();
        private eLogic Logic = eLogic.elUndef;

        private PropertyValue TestParam1;
        private IParameter TestParam2;
        private eComparison Comparison = eComparison.ecUndef;
        private string conditional;

        private const string testRegExpStr =
                    "(( *)(?<prop1>" + FormatHelper.propertyStr + ")" + FormatHelper.subsequentSpaces +
                    "(?<cond>" + FormatHelper.conditionStr + ")" + FormatHelper.subsequentSpaces +
                    "((?<prop2>" + FormatHelper.propertyStr + ")|(?<val>" + FormatHelper.valueStr + ")))";
        private static readonly Regex testRegex = new Regex(testRegExpStr, RegexOptions.Compiled);

        //delete private double TestValue = 0.0;
        private List<Condition> conditions = new List<Condition>();

        private void InitializeConditionals()
        {
            mComparison["EQ"] = eComparison.eEQ;
            mComparison["NE"] = eComparison.eNE;
            mComparison["GT"] = eComparison.eGT;
            mComparison["GE"] = eComparison.eGE;
            mComparison["LT"] = eComparison.eLT;
            mComparison["LE"] = eComparison.eLE;
            mComparison["eq"] = eComparison.eEQ;
            mComparison["ne"] = eComparison.eNE;
            mComparison["gt"] = eComparison.eGT;
            mComparison["ge"] = eComparison.eGE;
            mComparison["lt"] = eComparison.eLT;
            mComparison["le"] = eComparison.eLE;
            mComparison["=="] = eComparison.eEQ;
            mComparison["!="] = eComparison.eNE;
            mComparison[">"] = eComparison.eGT;
            mComparison[">="] = eComparison.eGE;
            mComparison["<"] = eComparison.eLT;
            mComparison["<="] = eComparison.eLE;
        }

    }
}
