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
    using System.Xml;
    using CommonUtils.Collections;
    using CommonUtils.IO;
    using CommonUtils.MathLib;
    using JSBSim.InputOutput;
    using JSBSim.MathValues;
    using JSBSim.Models;
    // Import log4net classes.
    using log4net;

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
    /// - Sensor
    /// - FCSFunction
    /// - PID
    /// - Accelerometer
    /// - Gyro
    /// - Actuator
    /// - Waypoint
    /// - Angle
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
            input = output = delay_time = 0.0;
            delay = index = 0;
            ClipMin = ClipMax = new RealValue(0.0);
            clip = cyclic_clip = false;
            dt = fcs.GetChannelDeltaT();

            propertyManager = fcs.GetPropertyManager();

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
            else if (element.LocalName.Equals("pid"))
            {
                compType = "PID";
            }
            else if (element.LocalName.Equals("sensor"))
            {
                compType = "SENSOR";
            }
            else if (element.LocalName.Equals("accelerometer"))
            {
                compType = "ACCELEROMETER";
            }
            else if (element.LocalName.Equals("magnetometer"))
            {
                compType = "MAGNETOMETER";
            }
            else if (element.LocalName.Equals("gyro"))
            {
                compType = "GYRO";
            }
            else if (element.LocalName.Equals("actuator"))
            {
                compType = "ACTUATOR";
            }
            else if (element.LocalName.Equals("waypoint_heading"))
            {
                compType = "WAYPOINT_HEADING";
            }
            else if (element.LocalName.Equals("waypoint_distance"))
            {
                compType = "WAYPOINT_DISTANCE";
            }
            else if (element.LocalName.Equals("angle"))
            {
                compType = "ANGLE";
            }
            else if (element.LocalName.Equals("distributor"))
            {
                compType = "DISTRIBUTOR";
            }
            else
            { // illegal component in this channel
                compType = "UNKNOWN";
            }

            name = element.GetAttribute("name");

            foreach (XmlNode currentNode in element.GetElementsByTagName("init"))
            {
                if (currentNode.NodeType == XmlNodeType.Element)
                {
                    XmlElement init_element = (XmlElement)currentNode;
                    initNodes.Add(new PropertyValue(init_element.InnerText, propertyManager));
                }
            }
            foreach (XmlNode currentNode in element.GetElementsByTagName("input"))
            {
                if (currentNode.NodeType == XmlNodeType.Element)
                {
                    XmlElement input_element = (XmlElement)currentNode;
                    inputNodes.Add(new PropertyValue(input_element.InnerText, propertyManager));
                }
            }
            foreach (XmlNode currentNode in element.GetElementsByTagName("output"))
            {
                if (currentNode.NodeType == XmlNodeType.Element)
                {
                    XmlElement out_elem = (XmlElement)currentNode;
                    string output_node_name = out_elem.InnerText;
                    bool node_exists = propertyManager.HasNode(output_node_name);
                    PropertyNode OutputNode = propertyManager.GetNode(output_node_name, true);
                    if (OutputNode == null)
                    {
                        log.Error("  Unable to process property: " + output_node_name);
                        throw new Exception("Invalid output property name in flight control definition");
                    }
                    outputNodes.Add(OutputNode);
                    // If the node has just been created then it must be initialized to a
                    // sensible value since FGPropertyNode::GetNode() does not take care of
                    // that.  If the node was already existing, its current value is kept
                    // unchanged.
                    if (!node_exists)
                        OutputNode.Set(output);
                }
            }
            XmlElement delay_elem = element.FindElement("delay");
            if (delay_elem != null)
            {
                delay_time = delay_elem.GetDataAsNumber();
                string delayType = delay_elem.GetAttribute("type");
                if (!string.IsNullOrEmpty(delayType))
                {
                    if (delayType == "time")
                    {
                        delay = (int)(delay_time / dt);
                    }
                    else if (delayType == "frames")
                    {
                        delay = (int)delay_time;
                    }
                    else
                    {
                        log.Error("Unallowed delay type");
                    }
                }
                else
                {
                    delay = (int)(delay_time / dt);
                }
                output_array.Resize(delay);
                for (int i = 0; i < delay; i++) output_array[i] = 0.0;
            }

            XmlElement clip_el = element.FindElement("clipto");
            if (clip_el != null)
            {
                XmlElement el = clip_el.FindElement("min");
                if (el == null)
                {
                    log.Error("Element <min> is missing, <clipto> is ignored.");
                    return;
                }

                ClipMin = new ParameterValue(el, propertyManager);

                el = clip_el.FindElement("max");
                if (el == null)
                {
                    log.Error("Element <max> is missing, <clipto> is ignored.");
                    ClipMin = null;
                    return;
                }

                ClipMax = new ParameterValue(el, propertyManager);

                if (clip_el.GetAttribute("type") == "cyclic")
                    cyclic_clip = true;

                clip = true;
            }

            Debug(0);
        }
        public virtual void ResetPastStates()
        {
            index = 0;
            for (int i = 0; i < output_array.Count; i++)
                output_array[i] = 0.0;
        }

        public virtual bool Run()
        {
            return true;
        }


        public virtual void SetOutput()
        {
            for (int i = 0; i < outputNodes.Count; i++)
                outputNodes[i].Set(output);
        }

        public double GetOutput() { return output; }
        public string GetName() { return name; }
        public string GetComponentType() { return compType; }
        public virtual double GetOutputPct() { return 0; }

        public virtual void Convert() { }
        public virtual void Bind()
        {
            string tmp = "fcs/" + PropertyManager.MakePropertyName(name, true);
            fcs.GetPropertyManager().Tie(tmp, this.GetOutput, null);
        }

        protected void Delay()
        {
            output_array[index] = output;
            if (index == delay - 1) index = 0;
            else index++;
            output = output_array[index];
        }

        protected void Clip()
        {
            if (clip)
            {
                double vmin = ClipMin.GetValue();
                double vmax = ClipMax.GetValue();
                double range = vmax - vmin;

                if (range < 0.0)
                {
                    log.Error("Trying to clip with a max value " + ClipMax.GetName()
                         + " lower than the min value " + ClipMin.GetName());
                    throw new Exception("JSBSim aborts");
                }

                if (cyclic_clip && range != 0.0)
                {
                    double value = output - vmin;
                    output = (value % range) + vmin;
                    if (output < vmin)
                        output += range;
                }
                else
                    output = MathExt.Constrain(vmin, output, vmax);
            }
        }

        // The old way of naming FCS components allowed upper or lower case, spaces,
        // etc. but then the names were modified to fit into a property name
        // hierarchy. This was confusing (it wasn't done intentionally - it was a
        // carryover from the early design). We now support the direct naming of
        // properties in the FCS component name attribute. The old way is supported in
        // code at this time, but deprecated.
        protected virtual void Bind(XmlElement el)
        {
            // TODO PENDING 
        }
        protected virtual void Debug(int from)
        {
            // TODO PENDING 
        }


        protected FlightControlSystem fcs;
        protected PropertyManager propertyManager;
        protected List<PropertyNode> outputNodes;
        protected IParameter ClipMin, ClipMax;
        protected List<PropertyValue> initNodes = new List<PropertyValue>();
        protected List<PropertyValue> inputNodes = new List<PropertyValue>();
        protected List<double> output_array;
        protected string compType;
        protected string name;
        protected double input;
        protected double output;
        protected double delay_time;
        protected int delay;
        protected int index;
        protected double dt;
        protected bool clip, cyclic_clip;
    }
}
