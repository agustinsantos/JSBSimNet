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
#region Identification
/// $Id:$
#endregion
namespace JSBSim.Script
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Xml;

    // Import log4net classes.
    using log4net;

    using JSBSim.InputOutput;
    using JSBSim.Format;

    /// <summary>
    /// Encapsulates the JSBSim scripting capability.
    /// <h4>Scripting support provided via FGScript.</h4>
    /// 
    /// <p>There is simple scripting support provided in the FGScript
    /// class. Commands are specified using the <em>Simple Scripting
    /// Directives for JSBSim</em> (SSDJ). The script file is in XML
    /// format. A test condition (or conditions) can be set up in the
    /// script and when the condition evaluates to true, the specified
    /// action[s] is/are taken. A test condition can be <em>persistent</em>,
    /// meaning that if a test condition evaluates to true, then passes
    /// and evaluates to false, the condition is reset and may again be
    /// triggered. When the set of tests evaluates to true for a given
    /// condition, an item may be set to another value. This value might
    /// be a boolean, a value, or a delta value, and the change from the
    /// current value to the new value can be either via a step function,
    /// a ramp, or an exponential approach. The speed of a ramp or
    /// approach is specified via the time constant. Here is the format
    /// of the script file:</p>
    /// 
    /// <pre><strong>&lt;?xml version=&quot;1.0&quot;?&gt;
    /// &lt;runscript name=&quot;C172-01A&quot;&gt;
    /// 
    /// &lt;!--
    /// This run is for testing C172 runs
    /// --&gt;
    /// 
    /// &lt;use aircraft=&quot;c172&quot;&gt;
    /// &lt;use initialize=&quot;reset00&quot;&gt;
    /// 
    /// &lt;run start=&quot;0.0&quot; end=&quot;4.5&quot; dt=&quot;0.05&quot;&gt;
    ///   &lt;when&gt;
    ///     &lt;parameter name=&quot;FG_TIME&quot; comparison=&quot;ge&quot; value=&quot;0.25&quot;&gt;
    ///     &lt;parameter name=&quot;FG_TIME&quot; comparison=&quot;le&quot; value=&quot;0.50&quot;&gt;
    ///     &lt;set name=&quot;FG_AILERON_CMD&quot; type=&quot;FG_VALUE&quot; value=&quot;0.25&quot;
    ///     action=&quot;FG_STEP&quot; persistent=&quot;false&quot; tc =&quot;0.25&quot;&gt;
    ///   &lt;/when&gt;
    ///   &lt;when&gt;
    ///     &lt;parameter name=&quot;FG_TIME&quot; comparison=&quot;ge&quot; value=&quot;0.5&quot;&gt;
    ///     &lt;parameter name=&quot;FG_TIME&quot; comparison=&quot;le&quot; value=&quot;1.5&quot;&gt;
    ///     &lt;set name=&quot;FG_AILERON_CMD&quot; type=&quot;FG_DELTA&quot; value=&quot;0.5&quot;
    ///     action=&quot;FG_EXP&quot; persistent=&quot;false&quot; tc =&quot;0.5&quot;&gt;
    ///   &lt;/when&gt;
    ///   &lt;when&gt;
    ///     &lt;parameter name=&quot;FG_TIME&quot; comparison=&quot;ge&quot; value=&quot;1.5&quot;&gt;
    ///     &lt;parameter name=&quot;FG_TIME&quot; comparison=&quot;le&quot; value=&quot;2.5&quot;&gt;
    ///     &lt;set name=&quot;FG_RUDDER_CMD&quot; type=&quot;FG_DELTA&quot; value=&quot;0.5&quot;
    ///     action=&quot;FG_RAMP&quot; persistent=&quot;false&quot; tc =&quot;0.5&quot;&gt;
    ///   &lt;/when&gt;
    /// &lt;/run&gt;
    /// 
    /// &lt;/runscript&gt;</strong></pre>
    /// 
    /// <p>The first line must always be present. The second line
    /// identifies this file as a script file, and gives a descriptive
    /// name to the script file. Comments are next, delineated by the
    /// &lt;!-- and --&gt; symbols. The aircraft and initialization files
    /// to be used are specified in the &quot;use&quot; lines. Next,
    /// comes the &quot;run&quot; section, where the conditions are
    /// described in &quot;when&quot; clauses.</p>
    /// @author Jon S. Berndt
    /// @version "$Id: FGScript.h,v 1.12 2003/12/04 05:12:53 jberndt Exp $
    /// </summary>
    public class Script
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

        /// Default constructor
        public Script(FDMExecutive exec)
        {
            FDMExec = exec;
            state = FDMExec.State;
            propertyManager = FDMExec.PropertyManager;
        }

        /// <summary>
        /// Loads a script to drive JSBSim (usually in standalone mode).
        /// The language is the Simple Script Directives for JSBSim (SSDJ).
        /// </summary>
        /// <param name="script">the filename (including path name, if any) for the script.</param>
        public void LoadScript(string script)
        {
            try
            {
                XmlTextReader reader = new XmlTextReader(script);
                XmlDocument doc = new XmlDocument();
                // load the data into the dom
                doc.Load(reader);
                XmlNodeList childNodes = doc.GetElementsByTagName("runscript");
                if (childNodes.Count == 0)
                {
                    if (log.IsErrorEnabled)
                        log.Error("File: " + script + " is not a script file. Tag runscript not found");
                }
                Load(childNodes[0] as XmlElement);
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                {
                    log.Error("Exception reading script file: " + e);
                }
            }
        }

        public bool Load(XmlElement root)
        {
            if (log.IsDebugEnabled)
            {
                log.Debug("Reading and running from script file " + scriptName);
            }

            string name = root.GetAttribute("name");
            if (name.Length != 0)
            {
                this.scriptName = name;
            }

            XmlNodeList childNodes = root.GetElementsByTagName("use");
            if (childNodes.Count != 2 && log.IsErrorEnabled)
            {
                if (log.IsErrorEnabled)
                    log.Error("Two <use> tags must be specified in script file.");
                return false;

            }
            string aircraft = (childNodes[0] as XmlElement).GetAttribute("aircraft");
            string initialize = (childNodes[1] as XmlElement).GetAttribute("initialize");

            if (aircraft.Length != 0)
            {
                try
                {
                    FDMExec.LoadModel(aircraft);

                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                        log.Error("Exception reading script file: " + e);
                    return false;

                }
            }
            else
            {
                if (log.IsErrorEnabled)
                    log.Error("Aircraft must be specified first in script file.");
                return false;
            }

            childNodes = root.GetElementsByTagName("run");
            if (childNodes.Count != 1)
            {
                if (log.IsErrorEnabled) 
                    log.Error("One \"<run>\" tag must be specified in script file");
                return false;
            }

            // Set sim timing
            XmlElement run_element = childNodes[0] as XmlElement;
            startTime =  double.Parse(run_element.GetAttribute("start"), FormatHelper.numberFormatInfo);
            FDMExec.State.SimTime = startTime;
            endTime = double.Parse(run_element.GetAttribute("end"), FormatHelper.numberFormatInfo);
            FDMExec.State.DeltaTime = double.Parse(run_element.GetAttribute("dt"), FormatHelper.numberFormatInfo);

            foreach (XmlNode currentNode in root.GetElementsByTagName("when"))
            {
                ReadWhenClause(currentNode as XmlElement);
            }

            try
            {
                FDMExec.GetIC().Load(initialize, true);
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error("Initialization unsuccessful. Exception " + e);
            }

            return true;
        }

        private void ReadWhenClause(XmlElement element)
        {
            Condition newCondition = new Condition();
            foreach (XmlNode currentNode in element.ChildNodes)
            {
                if (currentNode.NodeType == XmlNodeType.Element)
                {
                    XmlElement currentElement = (XmlElement)currentNode;
                    if (currentElement.LocalName.Equals("parameter"))
                    {
                        // read parameters
                        string prop_name = currentElement.GetAttribute("name");
                        newCondition.TestParam.Add(FDMExec.PropertyManager.GetPropertyNode(prop_name));
                        double value = double.Parse(currentElement.GetAttribute("value"), FormatHelper.numberFormatInfo);
                        newCondition.TestValue.Add(value);
                        string comparison = currentElement.GetAttribute("comparison");
                        newCondition.Comparison.Add(comparison);

                    }
                    else if (currentElement.LocalName.Equals("set"))
                    {
                        // read set definitions
                        string prop_name = currentElement.GetAttribute("name");
                        newCondition.SetParam.Add(FDMExec.PropertyManager.GetPropertyNode(prop_name));

                        double value = double.Parse(currentElement.GetAttribute("value"), FormatHelper.numberFormatInfo);

                        newCondition.SetValue.Add(value);
                        newCondition.Triggered.Add(false);
                        newCondition.OriginalValue.Add(0.0);
                        newCondition.newValue.Add(0.0);
                        newCondition.StartTime.Add(0.0);
                        newCondition.EndTime.Add(0.0);

                        string tempCompare = currentElement.GetAttribute("type");

                        if (tempCompare == "FG_DELTA")
                            newCondition.Type.Add(eType.FG_DELTA);
                        else if (tempCompare == "FG_BOOL")
                            newCondition.Type.Add(eType.FG_BOOL);
                        else if (tempCompare == "FG_VALUE")
                            newCondition.Type.Add(eType.FG_VALUE);
                        else
                            newCondition.Type.Add(eType.FG_VALUE); // DEFAULT
                        
                        tempCompare = currentElement.GetAttribute("action");
                        if (tempCompare == "FG_RAMP")
                            newCondition.Action.Add(eAction.FG_RAMP);
                        else if (tempCompare == "FG_STEP")
                            newCondition.Action.Add(eAction.FG_STEP);
                        else if (tempCompare == "FG_EXP")
                            newCondition.Action.Add(eAction.FG_EXP);
                        else
                            newCondition.Action.Add(eAction.FG_STEP); // DEFAULT

                        if (currentElement.GetAttribute("persistent") == "true")
                            newCondition.Persistent.Add(true);
                        else
                            newCondition.Persistent.Add(false); // DEFAULT

                        string tc = currentElement.GetAttribute("tc");
                        if (tc.Length != 0)
                            newCondition.TC.Add(double.Parse(tc, FormatHelper.numberFormatInfo));
                        else
                            newCondition.TC.Add(1.0); // DEFAULT
                    }
                }
            }
            conditions.Add(newCondition);
        }

        public bool RunScript()
        {
            bool truth = false;
            bool WholeTruth = false;

            double currentTime = FDMExec.State.SimTime;
            double newSetValue = 0;

            if (currentTime > endTime) return false;

            foreach (Condition iC in conditions)
            {
                // determine whether the set of conditional tests for this condition equate
                // to true
                for (int i = 0; i < iC.TestValue.Count; i++)
                {
                    if (iC.Comparison[i].Equals("lt"))
                        truth = iC.TestParam[i].GetDouble() < iC.TestValue[i];
                    else if (iC.Comparison[i].Equals("le"))
                        truth = iC.TestParam[i].GetDouble() <= iC.TestValue[i];
                    else if (iC.Comparison[i].Equals("eq"))
                        truth = iC.TestParam[i].GetDouble() == iC.TestValue[i];
                    else if (iC.Comparison[i].Equals("ge"))
                        truth = iC.TestParam[i].GetDouble() >= iC.TestValue[i];
                    else if (iC.Comparison[i].Equals("gt"))
                        truth = iC.TestParam[i].GetDouble() > iC.TestValue[i];
                    else if (iC.Comparison[i].Equals("ne"))
                        truth = iC.TestParam[i].GetDouble() != iC.TestValue[i];
                    else
                        if (log.IsErrorEnabled)
                            log.Error("Bad comparison in scrip:" + iC.Comparison[i]);

                    if (i == 0) WholeTruth = truth;
                    else WholeTruth = WholeTruth && truth;

                    if (!truth && iC.Persistent[i] && iC.Triggered[i])
                        iC.Triggered[i] = false;
                }

                // if the conditions are true, do the setting of the desired parameters

                if (WholeTruth)
                {
                    for (int i = 0; i < iC.SetValue.Count; i++)
                    {
                        if (!iC.Triggered[i])
                        {
                            iC.OriginalValue[i] = iC.SetParam[i].GetDouble();
                            switch (iC.Type[i])
                            {
                                case eType.FG_VALUE:
                                    iC.newValue[i] = iC.SetValue[i];
                                    break;
                                case eType.FG_DELTA:
                                    iC.newValue[i] = iC.OriginalValue[i] + iC.SetValue[i];
                                    break;
                                case eType.FG_BOOL:
                                    iC.newValue[i] = iC.SetValue[i];
                                    break;
                            }
                            iC.Triggered[i] = true;
                            iC.StartTime[i] = currentTime;
                        }

                        double time_span = currentTime - iC.StartTime[i];
                        double value_span = iC.newValue[i] - iC.OriginalValue[i];

                        switch (iC.Action[i])
                        {
                            case eAction.FG_RAMP:
                                if (time_span <= iC.TC[i])
                                    newSetValue = time_span / iC.TC[i] * value_span + iC.OriginalValue[i];
                                else
                                    newSetValue = iC.newValue[i];
                                break;
                            case eAction.FG_STEP:
                                newSetValue = iC.newValue[i];
                                break;
                            case eAction.FG_EXP:
                                newSetValue = (1 - Math.Exp(-time_span / iC.TC[i])) * value_span + iC.OriginalValue[i];
                                break;
                        }
                        iC.SetParam[i].Set(newSetValue);
                    }
                }
            }
            return true;
        }

        public void LoadScript(XmlReader reader)
        {
            throw new Exception("Deprecated");
        }

        private enum eAction
        {
            FG_RAMP = 1,
            FG_STEP = 2,
            FG_EXP = 3
        };

        private enum eType
        {
            FG_VALUE = 1,
            FG_DELTA = 2,
            FG_BOOL = 3
        };

        private class Condition
        {
            public List<PropertyNode>       TestParam = new List<PropertyNode>(); //vector <FGPropertyManager*>
            public List<PropertyNode>       SetParam = new List<PropertyNode>(); //vector <FGPropertyManager*>
            public List<double>             TestValue = new List<double>(); //vector <double>  
            public List<double>             SetValue = new List<double>(); //vector <double>  
            public List<string>             Comparison = new List<string>(); //vector <string>  
            public List<double>             TC = new List<double>(); //vector <double>  
            public List<bool>               Persistent = new List<bool>(); //vector <bool> 
            public List<eAction>            Action = new List<eAction>(); //vector <eAction> 
            public List<eType>              Type = new List<eType>(); //vector <eType>   
            public List<bool>               Triggered = new List<bool>(); //vector <bool>    
            public List<double>             newValue = new List<double>(); //vector <double>  
            public List<double>             OriginalValue = new List<double>(); //vector <double>  
            public List<double>             StartTime = new List<double>(); //vector <double>  
            public List<double>             EndTime = new List<double>(); //vector <double>  
        };

        private string scriptName;
        private double startTime;
        private double endTime;
        private List<Condition> conditions = new List<Condition>(); // vector <struct condition>

        private FDMExecutive FDMExec;
        private State state;
        private PropertyManager propertyManager;
    }
}
