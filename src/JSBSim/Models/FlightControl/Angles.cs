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
    using System.Xml;
    using CommonUtils.IO;
    using JSBSim.InputOutput;
    using JSBSim.Models;
    // Import log4net classes.
    using log4net;


    /// <summary>
    ///  Provides a way to determine the smallest included angle.
    /// 
    ///  @code
    ///  <angle name="component_name" unit="DEG|RAD">
    ///    <source_angle unit = "DEG|RAD" > property_name </ source_angle >
    ///    < target_angle unit="DEG|RAD">  property_name</target_angle>
    ///      [< clipto >
    /// 
    ///        < min > {[-] property name | value } </min>
    ///        <max> {[-] property name | value} </max>
    ///      </clipto>]
    ///      [<output> {property} </output>]
    ///    </angle>
    ///    @endcode
    ///    
    ///  @author Jon S.Berndt
    /// </summary>
    public class Angles : FCSComponent
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

        public Angles(FlightControlSystem fcs, XmlElement element)
            : base(fcs, element)
        {
                 source_angle = 0.0;
                target_angle = 0.0;
                source_angle_unit = 1.0;
                target_angle_unit = 1.0;
                output_unit = 1.0;

                if (element.FindElement("target_angle") != null)
                {
                    target_angle_pNode = propertyManager.GetNode(element.FindElementValue("target_angle"));
                    if (element.FindElement("target_angle").HasAttribute("unit"))
                    {
                        if (element.FindElement("target_angle").GetAttribute ("unit") == "DEG")
                        {
                            target_angle_unit = 0.017453293;
                        }
                    }
                }
                else
                {
                    throw new Exception("Target angle is required for component: " + name);
                }

                if (element.FindElement("source_angle") !=null)
                {
                    source_angle_pNode = propertyManager.GetNode(element.FindElementValue("source_angle"));
                    if (element.FindElement("source_angle").HasAttribute("unit"))
                    {
                        if (element.FindElement("source_angle").GetAttribute ("unit") == "DEG")
                        {
                            source_angle_unit = 0.017453293;
                        }
                    }
                }
                else
                {
                    throw new Exception("Source latitude is required for Angles component: " + name);
                }

                unit = element.GetAttribute ("unit");
                if (!string.IsNullOrEmpty(unit ))
                {
                    if (unit == "DEG") output_unit = 180.0 / Math.PI;
                    else if (unit == "RAD") output_unit = 1.0;
                    else throw new Exception("Unknown unit " + unit + " in angle component, " +name);
                }
                else
                {
                    output_unit = 1.0; // Default is radians (1.0) if unspecified
                }

                Bind(element);
                Debug(0);
            }
        public override bool Run()
        {
            source_angle = source_angle_pNode.GetDouble() * source_angle_unit;
            target_angle = target_angle_pNode.GetDouble() * target_angle_unit;

            double x1 = Math.Cos(source_angle);
            double y1 = Math.Sin(source_angle);
            double x2 = Math.Cos(target_angle);
            double y2 = Math.Sin(target_angle);

            double x1x2_y1y2 = Math.Max(-1.0, Math.Min(x1 * x2 + y1 * y2, 1.0));
            double angle_to_heading_rad = Math.Acos(x1x2_y1y2);
            double x1y2 = x1 * y2;
            double x2y1 = x2 * y1;

            if (x1y2 >= x2y1) output = angle_to_heading_rad * output_unit;
            else output = -angle_to_heading_rad * output_unit;

            Clip();
            SetOutput();

            return true;
        }

        private PropertyNode target_angle_pNode;
        private PropertyNode source_angle_pNode;
        private double target_angle;
        private double source_angle;
        private double target_angle_unit;
        private double source_angle_unit;
        private double output_unit;
        private string unit;

         protected override void Bind(XmlElement el) { }
        protected override void Debug(int from) { }
    }
}
