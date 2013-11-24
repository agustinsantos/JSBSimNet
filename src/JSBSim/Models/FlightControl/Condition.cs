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
    using System.Collections.Generic;
    using System.Text;
    using System.Xml;
    using System.IO;
    using System.Text.RegularExpressions;

    // Import log4net classes.
    using log4net;

    using CommonUtils.IO;
    using JSBSim.InputOutput;
    using JSBSim.Format;

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


        public Condition(XmlElement element, PropertyManager propertyManager)
        {
            string logic;
            
            isGroup = true;

            InitializeConditionals();

            logic = element.GetAttribute("logic");
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

            foreach (XmlNode currentNode in element.ChildNodes)
            {
                if (currentNode.NodeType == XmlNodeType.Element)
                {
                    conditions.Add(new Condition(currentNode as XmlElement, propertyManager));
                }
            }

            ReaderText rtxt = new ReaderText(new StringReader(element.InnerText));
            while (rtxt.Done)
            {
                string tmp = rtxt.ReadLine().Trim();
                conditions.Add(new Condition(tmp, propertyManager));
            }
        }


        public Condition(string testStr, PropertyManager propertyManager)
        {
            isGroup = false;

            InitializeConditionals();

            Match match = testRegex.Match(testStr);
            if (match.Success)
            {
                TestParam1 = propertyManager.GetPropertyNode(match.Groups["prop1"].Value);
                conditional = match.Groups["cond"].Value;
                Comparison = mComparison[conditional];
                if (match.Groups["prop2"].Value == "")
                {
                    TestValue = double.Parse(match.Groups["val"].Value, FormatHelper.numberFormatInfo);
                }
                else
                {
                    TestParam2 = propertyManager.GetPropertyNode(match.Groups["prop2"].Value);
                }
            }
            else
            {
                if (log.IsErrorEnabled)
                    log.Error("Error parsing Condition: " + testStr);
                throw new ArgumentException("Error parsing Condition: " + testStr);
            }
        }

        public bool Evaluate()
        {
            bool pass = false;
            double compareValue;

            if (Logic == eLogic.eAND)
            {
                pass = true;
                foreach (Condition iConditions in conditions)
                {
                    if (!iConditions.Evaluate())
                        pass = false;
                }
            }
            else if (Logic == eLogic.eOR)
            {
                pass = false;
                foreach (Condition iConditions in conditions)
                {
                    if (iConditions.Evaluate())
                        pass = true;
                }
            }
            else
            {
                if (TestParam2 != null) compareValue = TestParam2.GetDouble();
                else compareValue = TestValue;

                switch (Comparison)
                {
                    case eComparison.ecUndef:
                        if (log.IsErrorEnabled)
                            log.Error("Undefined comparison operator.");
                        break;
                    case eComparison.eEQ:
                        pass = TestParam1.GetDouble() == compareValue;
                        break;
                    case eComparison.eNE:
                        pass = TestParam1.GetDouble() != compareValue;
                        break;
                    case eComparison.eGT:
                        pass = TestParam1.GetDouble() > compareValue;
                        break;
                    case eComparison.eGE:
                        pass = TestParam1.GetDouble() >= compareValue;
                        break;
                    case eComparison.eLT:
                        pass = TestParam1.GetDouble() < compareValue;
                        break;
                    case eComparison.eLE:
                        pass = TestParam1.GetDouble() <= compareValue;
                        break;
                    default:
                        if (log.IsErrorEnabled)
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

        public override string ToString()
        {
            StringBuilder scratch = new StringBuilder();

            if (isGroup)
            {
                switch (Logic)
                {
                    case (eLogic.elUndef):
                        scratch.Append(" UNSET");
                        if (log.IsErrorEnabled)
                            log.Error("unset logic for test condition");
                        break;
                    case (eLogic.eAND):
                        scratch.Append(" if all of the following are true");
                        break;
                    case (eLogic.eOR):
                        scratch.Append(" if any of the following are true:");
                        break;
                    default:
                        scratch.Append(" UNKNOWN");
                        if (log.IsErrorEnabled)
                            log.Error("Unknown logic for test condition");
                        break;
                }

                scratch.Append("\n");
                foreach (Condition iConditions in conditions)
                {
                    scratch.Append(iConditions.ToString());
                }
            }
            else
            {
                if (TestParam2 != null)
                    scratch.Append(TestParam1.ShortName + " " + conditional + " " + TestParam2.ShortName);
                else
                    scratch.Append(TestParam1.ShortName + " " + conditional + " " + TestValue.ToString(FormatHelper.numberFormatInfo));
            }

            return scratch.ToString();
        }

        private enum eComparison { ecUndef = 0, eEQ, eNE, eGT, eGE, eLT, eLE };
        private enum eLogic { elUndef = 0, eAND, eOR };
        
        private Dictionary<string, eComparison> mComparison = new Dictionary<string, eComparison>();
        private eLogic Logic = eLogic.elUndef;

        private PropertyNode TestParam1, TestParam2;
        private double TestValue = 0.0;
        private eComparison Comparison = eComparison.ecUndef;
        private bool isGroup;
        private string conditional;

        private const string testRegExpStr =
                    "(( *)(?<prop1>" + FormatHelper.propertyStr + ")" + FormatHelper.subsequentSpaces +
                    "(?<cond>" + FormatHelper.conditionStr + ")" + FormatHelper.subsequentSpaces +
                    "((?<prop2>" + FormatHelper.propertyStr + ")|(?<val>" + FormatHelper.valueStr + ")))";
        private static readonly Regex testRegex = new Regex(testRegExpStr, RegexOptions.Compiled);

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
