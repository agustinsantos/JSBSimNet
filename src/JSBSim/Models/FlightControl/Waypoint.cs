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
    using CommonUtils.MathLib;
    using JSBSim.Format;
    using JSBSim.MathValues;
    using JSBSim.Models;
    // Import log4net classes.
    using log4net;


    /// <summary>
    /// Models a Waypoint object. 
    /// The waypoint_heading component returns the heading to a specified waypoint
    /// lat/long from another specified point.
    /// The waypoint_distance component returns the distance between
    /// 
    ///  @code
    /// <waypoint_heading name="component_name" unit="DEG|RAD">
    ///   <target_latitude unit = "DEG|RAD" > property_name </ target_latitude >
    ///    < target_longitude unit= "DEG|RAD" > property_name </ target_longitude >
    ///    < source_latitude unit= "DEG|RAD" > property_name </ source_latitude >
    ///    < source_longitude unit= "DEG|RAD" > property_name </ source_longitude >
    ///    [< clipto >
    ///      < min > {[-] property name | value </min>
    ///      <max> {[-] property name | value} </max>
    ///    </clipto>]
    ///    [<output> {property} </output>]
    ///  </waypoint_heading>
    /// 
    ///  <waypoint_distance name = "component_name" unit="FT|M">
    ///    <target_latitude unit = "DEG|RAD" > property_name </ target_latitude >
    ///     < target_longitude unit="DEG|RAD"> property_name</target_longitude>
    ///    <source_latitude unit = "DEG|RAD" > property_name </ source_latitude >
    ///    < source_longitude unit="DEG|RAD"> property_name</source_longitude>
    ///    [< radius > {value} </radius>]
    ///   [<clipto>
    ///     <min> {[-] property name | value} </min>
    ///     <max> {[-] property name | value} </max>
    ///    </clipto>]
    ///    [<output> {property} </output>]
    ///  </waypoint_distance>
    /// @endcode
    /// 
    /// @author Jon S.Berndt
    /// </summary>
    public class Waypoint : FCSComponent
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

        public Waypoint(FlightControlSystem fcs, XmlElement element) : base(fcs, element)
        {
            if (compType == "WAYPOINT_HEADING") WaypointType = eWaypointType.eHeading;
            else if (compType == "WAYPOINT_DISTANCE") WaypointType = eWaypointType.eDistance;

            target_latitude_unit = 1.0;
            target_longitude_unit = 1.0;
            source_latitude_unit = 1.0;
            source_longitude_unit = 1.0;
            source = fcs.GetExec().GetIC().GetPosition();

            if (element.FindElement("target_latitude") != null)
            {
                target_latitude = new PropertyValue(element.FindElementValue("target_latitude"),
                                                          propertyManager);
                if (element.FindElement("target_latitude").HasAttribute("unit"))
                {
                    if (element.FindElement("target_latitude").GetAttribute("unit") == "DEG")
                    {
                        target_latitude_unit = 0.017453293;
                    }
                }
            }
            else
            {
                log.Error("Target latitude is required for waypoint component: " + name);
                throw new Exception("Malformed waypoint definition");
            }

            if (element.FindElement("target_longitude") != null)
            {
                target_longitude = new PropertyValue(element.FindElementValue("target_longitude"),
                                                           propertyManager);
                if (element.FindElement("target_longitude").HasAttribute("unit"))
                {
                    if (element.FindElement("target_longitude").GetAttribute("unit") == "DEG")
                    {
                        target_longitude_unit = 0.017453293;
                    }
                }
            }
            else
            {
                log.Error("Target longitude is required for waypoint component: " + name);
                throw new Exception("Malformed waypoint definition");
            }

            if (element.FindElement("source_latitude") != null)
            {
                source_latitude = new PropertyValue(element.FindElementValue("source_latitude"),
                                                          propertyManager);
                if (element.FindElement("source_latitude").HasAttribute("unit"))
                {
                    if (element.FindElement("source_latitude").GetAttribute("unit") == "DEG")
                    {
                        source_latitude_unit = 0.017453293;
                    }
                }
            }
            else
            {
                log.Error("Source latitude is required for waypoint component: " + name);
                throw new Exception("Malformed waypoint definition");
            }

            if (element.FindElement("source_longitude") != null)
            {
                source_longitude = new PropertyValue(element.FindElementValue("source_longitude"),
                                                           propertyManager);
                if (element.FindElement("source_longitude").HasAttribute("unit"))
                {
                    if (element.FindElement("source_longitude").GetAttribute("unit") == "DEG")
                    {
                        source_longitude_unit = 0.017453293;
                    }
                }
            }
            else
            {
                log.Error("Source longitude is required for waypoint component: " + name);
                throw new Exception("Malformed waypoint definition");
            }

            if (element.FindElement("radius") != null)
            {
                XmlElement elem = element.FindElement("radius");
                radius = FormatHelper.ValueAsNumberConvertTo(elem, "FT");
            }
            else
            {
                radius = -1.0;
            }

            unit = element.GetAttribute("unit");
            if (WaypointType == eWaypointType.eHeading)
            {
                if (!string.IsNullOrEmpty(unit))
                {
                    if (unit == "DEG") eUnit = eUnitType.eDeg;
                    else if (unit == "RAD") eUnit = eUnitType.eRad;
                    else
                    {
                        log.Error("Unknown unit " + unit + " in HEADING waypoint component, "
                             + name);
                        throw new Exception("Malformed waypoint definition");
                    }
                }
                else
                {
                    eUnit = eUnitType.eRad; // Default is radians if unspecified
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(unit))
                {
                    if (unit == "FT") eUnit = eUnitType.eFeet;
                    else if (unit == "M") eUnit = eUnitType.eMeters;
                    else
                    {
                        log.Error("Unknown unit " + unit + " in DISTANCE waypoint component, "
                             + name);
                        throw new Exception("Malformed waypoint definition");
                    }
                }
                else
                {
                    eUnit = eUnitType.eFeet; // Default is feet if unspecified
                }
            }

            Bind(element);
            Debug(0);
        }
        // ~ Waypoint();

        public override bool Run()
        {
            double source_latitude_rad = source_latitude.GetValue() * source_latitude_unit;
            double source_longitude_rad = source_longitude.GetValue() * source_longitude_unit;
            double target_latitude_rad = target_latitude.GetValue() * target_latitude_unit;
            double target_longitude_rad = target_longitude.GetValue() * target_longitude_unit;
            if (radius > 0.0)
                source.SetPosition(source_longitude_rad, source_latitude_rad, radius);
            else
                source.SetPositionGeodetic(source_longitude_rad, source_latitude_rad, 0.0);

            if (WaypointType == eWaypointType.eHeading)
            {     // Calculate Heading
                double heading_to_waypoint_rad = source.GetHeadingTo(target_longitude_rad,
                                                                     target_latitude_rad);

                if (eUnit == eUnitType.eDeg) output = heading_to_waypoint_rad * Constants.radtodeg;
                else output = heading_to_waypoint_rad;

            }
            else
            {                            // Calculate Distance
                double wp_distance = source.GetDistanceTo(target_longitude_rad,
                                                          target_latitude_rad);

                if (eUnit == eUnitType.eMeters) output = Conversion.FeetToMeters(wp_distance);
                else output = wp_distance;
            }

            Clip();
            SetOutput();

            return true;
        }


        private Location source;
        private PropertyValue target_latitude;
        private PropertyValue target_longitude;
        private PropertyValue source_latitude;
        private PropertyValue source_longitude;
        private double target_latitude_unit;
        private double target_longitude_unit;
        private double source_latitude_unit;
        private double source_longitude_unit;
        private double radius;
        private string unit;

        private enum eUnitType { eNone = 0, eDeg, eRad, eFeet, eMeters }
        private eUnitType eUnit;

        private enum eWaypointType { eNoType = 0, eHeading, eDistance }
        private eWaypointType WaypointType;

        protected override void Debug(int from)
        {
            //TODO. Not yet implemented
        }
    }
}
