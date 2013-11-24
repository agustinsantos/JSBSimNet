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
	using System.Collections;
    using System.Collections.Generic;
	using System.Xml;
	
	// Import log4net classes.
	using log4net;

	using JSBSim.InputOutput;
	using JSBSim.Models;
    using JSBSim.Format;

	/// <summary>
	/// Base class for JSBSim Flight Control System Components.
	/// The Flight Control System (FCS) for JSBSim consists of the FCS container
	/// class (see \URL[FGFCS]{FGFCS.html}), the FGFCSComponent base class, and the
	/// component classes from which can be constructed a string, or channel. See:
	/// 
	/// - Switch
	/// - Gain
	/// - Kinemat
	/// - Filter
	/// - DeadBand
	/// - Summer
	/// - Gradient
	/// 
	/// author Jon S. Berndt
	/// Documentation for the FGFCS class, and for the configuration file class
	/// </summary>
	public class FCSComponent
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

		/// Constructor
        public FCSComponent(FlightControlSystem fcsParent, XmlElement element)
        {
            fcs = fcsParent;
            propertyManager = fcs.GetPropertyManager();

            compType = "";
            isOutput = false;
            name = element.GetAttribute("name");
            compType = element.GetAttribute("type"); // Old, deprecated format
            if (compType.Length == 0)
            {
                if (element.LocalName.Equals("lag_filter"))
                {
                    compType = "LAG_FILTER";
                }
                else if (element.LocalName.Equals("lead_lag_filter"))
                {
                    compType = "LEAD_LAG_FILTER";
                }
                else if (element.LocalName.Equals("washout_filter"))
                {
                    compType = "WASHOUT_FILTER";
                }
                else if (element.LocalName.Equals("second_order_filter"))
                {
                    compType = "SECOND_ORDER_FILTER";
                }
                else if (element.LocalName.Equals("integrator"))
                {
                    compType = "INTEGRATOR";
                }
                else if (element.LocalName.Equals("summer"))
                {
                    compType = "SUMMER";
                }
                else if (element.LocalName.Equals("pure_gain"))
                {
                    compType = "PURE_GAIN";
                }
                else if (element.LocalName.Equals("scheduled_gain"))
                {
                    compType = "SCHEDULED_GAIN";
                }
                else if (element.LocalName.Equals("aerosurface_scale"))
                {
                    compType = "AEROSURFACE_SCALE";
                }
                else if (element.LocalName.Equals("switch"))
                {
                    compType = "SWITCH";
                }
                else if (element.LocalName.Equals("kinematic"))
                {
                    compType = "KINEMATIC";
                }
                else if (element.LocalName.Equals("deadband"))
                {
                    compType = "DEADBAND";
                }
                else if (element.LocalName.Equals("fcs_function"))
                {
                    compType = "FCS_FUNCTION";
                }
                else if (element.LocalName.Equals("sensor"))
                {
                    compType = "SENSOR";
                }
                else
                { // illegal component in this channel
                    compType = "UNKNOWN";
                }
            }


            foreach (XmlNode currentNode in element.ChildNodes)
            {
                if (currentNode.NodeType == XmlNodeType.Element)
                {
                    XmlElement currentElement = (XmlElement)currentNode;

                    if (currentElement.LocalName.Equals("input"))
                    {
                        string inputTxt = currentElement.InnerText.Trim();
                        if (inputTxt[0] == '-')
                        {
                            inputSigns.Add(-1.0f);
                            inputTxt = inputTxt.Remove(0, 1);
                        }
                        else
                        {
                            inputSigns.Add(1.0f);
                        }
                        inputNodes.Add(ResolveSymbol(inputTxt));
                    }
                    else if (currentElement.LocalName.Equals("output"))
                    {
                        isOutput = true;
                        outputNode = propertyManager.GetPropertyNode(currentElement.InnerText.Trim());
                        if (outputNode == null)
                        {
                            log.Error("  Unable to process property: " + currentElement.InnerText);
                            throw new Exception("Invalid output property name in flight control definition");
                        }
                        else if (currentElement.LocalName.Equals("clipto"))
                        {
                            XmlNodeList childs;
                            string clip_string;
                            childs = currentElement.GetElementsByTagName("min");
                            if (childs != null)
                                clip_string = ((XmlElement)childs[0]).InnerText.Trim();
                            else
                                throw new Exception("clipto doesn't have a min tag");

                            if (!clip_string.StartsWith("+-.0123456789"))
                            { // it's a property
                                if (clip_string[0] == '-') clipMinSign = -1.0f;
                                clip_string = clip_string.Remove(0, 1); //TODO test it
                                ClipMinPropertyNode = propertyManager.GetPropertyNode(clip_string);
                            }
                            else
                            {
                                clipmin = double.Parse(clip_string, FormatHelper.numberFormatInfo);
                            }

                            childs = currentElement.GetElementsByTagName("max");
                            if (childs != null)
                                clip_string = ((XmlElement)childs[0]).InnerText.Trim();
                            else
                                throw new Exception("clipto doesn't have a max tag");
                            if (!clip_string.StartsWith("+-.0123456789"))
                            { // it's a property
                                if (clip_string[0] == '-') clipMaxSign = -1.0f;
                                clip_string = clip_string.Remove(0, 1); //TODO test it
                                ClipMaxPropertyNode = propertyManager.GetPropertyNode(clip_string);
                            }
                            else
                            {
                                clipmax = double.Parse(clip_string, FormatHelper.numberFormatInfo);
                            }

                            clip = true;
                        }
                    }
                }
            }
        }
        
    

		public virtual bool Run()
		{
			return true;
		}


		public virtual void SetOutput()
		{
			outputNode.Set(output);
		}

		public  double GetOutput () {return output;}


		public PropertyNode GetOutputNode() { return outputNode; }
		public string GetName()  {return name;}
		public string GetComponentType()  { return compType; }
		public virtual double GetOutputPct()  { return 0; }

		public virtual void Convert() {} 
		public virtual void Bind() 
        {
            string tmp = "fcs/" + PropertyManager.MakePropertyName(name, true);
            fcs.GetPropertyManager().Tie(tmp, this.GetOutput, null);
        }

        protected PropertyNode ResolveSymbol(string token)
        {
            PropertyNode tmp = propertyManager.GetPropertyNode(token, true);
            if (tmp == null)
            {
                if (log.IsWarnEnabled)
                    log.Warn("Not implemented: Creating new property " + token);
                /* TODO ????
                string prop;

                if (!token.Contains("/")) 
                    prop = "model/" + token;
                if (log.IsDebugEnabled)
                    log.Debug("Creating new property " + prop);
                tmp = propertyManager.GetPropertyNode(token, true);
                if (tmp == null) throw new Exception("Property Node " + token + " not found");
                */
            }
            return tmp;
        }

        protected void Clip()
        {
            if (clip)
            {
                if (ClipMinPropertyNode != null) clipmin = clipMinSign * ClipMinPropertyNode.GetDouble();
                if (ClipMaxPropertyNode != null) clipmax = clipMaxSign * ClipMaxPropertyNode.GetDouble();
                if (output > clipmax) output = clipmax;
                else if (output < clipmin) output = clipmin;
            }
        }

        protected PropertyNode ClipMinPropertyNode;
        protected PropertyNode ClipMaxPropertyNode;
        protected List<PropertyNode> inputNodes = new List<PropertyNode>();
        protected List<float> inputSigns = new List<float>();
        protected double clipmax = 0.0, clipmin = 0.0;
        protected float clipMinSign = 1.0f, clipMaxSign = 1.0f;
        protected bool clip = false;
		protected FlightControlSystem fcs;
		protected PropertyManager propertyManager;
		protected PropertyManager treenode;
		protected string compType;
		protected string name;

		protected double input = 0.0;
		protected PropertyNode outputNode;
		protected double output = 0.0;
		protected bool isOutput = false;

		private const string IdSrc = "$Id: FGFCSComponent.cpp,v 1.36 2004/11/29 20:30:44 dpculp Exp $";
	}
}
