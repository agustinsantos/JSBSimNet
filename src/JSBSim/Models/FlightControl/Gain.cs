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
    using System.Text.RegularExpressions;


	// Import log4net classes.
	using log4net;
	
	using JSBSim.InputOutput;
	using JSBSim.Models;
	using JSBSim.MathValues;
    using JSBSim.Format;


	/// <summary>
	/// Encapsulates a gain component for the flight control system.
	/// The gain component merely multiplies the input by a gain.  The form of the
	/// gain component specification is:
	/// <pre>
	/// \<COMPONENT NAME="name" TYPE="PURE_GAIN">
	/// INPUT \<property>
	/// GAIN  \<value>
	/// [OUTPUT \<property>]
	/// \</COMPONENT>
	/// </pre>
	/// Note: as is the case with the Summer component, the input property name may be
	/// immediately preceded by a minus sign to invert that signal.
	/// 
	/// The scheduled gain component multiplies the input by a variable gain that is
	/// dependent on another property (such as qbar, altitude, etc.).  The lookup
	/// mapping is in the form of a table.  This kind of component might be used, for
	/// example, in a case where aerosurface deflection must only be commanded to
	/// acceptable settings - i.e at higher qbar the commanded elevator setting might
	/// be attenuated.  The form of the scheduled gain component specification is:
	/// <pre>
	/// \<COMPONENT NAME="name" TYPE="SCHEDULED_GAIN">
	/// INPUT \<property>
	/// [GAIN  \<value>]
	/// SCHEDULED_BY \<property>
	/// ROWS \<number_of_rows>
	/// \<lookup_value  gain_value>
	/// ?
	/// [CLIPTO \<min> \<max> 1]
	/// [OUTPUT \<property>]
	/// \</COMPONENT>
	/// </pre>
	/// An overall GAIN may be supplied that is multiplicative with the scheduled gain.
	/// 
	/// Note: as is the case with the Summer component, the input property name may
	/// be immediately preceded by a minus sign to invert that signal.
	/// 
	/// Here is an example of a scheduled gain component specification:
	/// <pre>
	/// \<COMPONENT NAME="Pitch Scheduled Gain 1" TYPE="SCHEDULED_GAIN">
	/// INPUT        fcs/pitch-gain-1
	/// GAIN         0.017
	/// SCHEDULED_BY fcs/elevator-pos-rad
	/// ROWS         22
	/// -0.68  -26.548
	/// -0.595 -20.513
	/// -0.51  -15.328
	/// -0.425 -10.993
	/// -0.34   -7.508
	/// -0.255  -4.873
	/// -0.17   -3.088
	/// -0.085  -2.153
	/// 0      -2.068
	/// 0.085  -2.833
	/// 0.102  -3.088
	/// 0.119  -3.377
	/// 0.136  -3.7
	/// 0.153  -4.057
	/// 0.17   -4.448
	/// 0.187  -4.873
	/// 0.272  -7.508
	/// 0.357 -10.993
	/// 0.442 -15.328
	/// 0.527 -20.513
	/// 0.612 -26.548
	/// 0.697 -33.433
	/// \</COMPONENT>
	/// </pre>
	/// In the example above, we see the utility of the overall GAIN value in
	/// effecting a degrees-to-radians conversion.
	/// 
	/// The aerosurface scale component is a modified version of the simple gain
	/// component.  The normal purpose
	/// for this component is to take control inputs that range from -1 to +1 or
	/// from 0 to +1 and scale them to match the expected inputs to a flight control
	/// system.  For instance, the normal and expected ability of a pilot to push or
	/// pull on a control stick is about 50 pounds.  The input to the pitch channelb
	/// lock diagram of a flight control system is in units of pounds.  Yet, the
	/// joystick control input is usually in a range from -1 to +1.  The form of the
	/// aerosurface scaling component specification is:
	/// <pre>
	/// \<COMPONENT NAME="name" TYPE="AEROSURFACE_SCALE">
	/// INPUT \<property>
	/// MIN \<value>
	/// MAX \<value>
	/// [GAIN  \<value>]
	/// [OUTPUT \<property>]
	/// \</COMPONENT>
	/// </pre>
	/// Note: as is the case with the Summer component, the input property name may be
	/// immediately preceded by a minus sign to invert that signal.
	/// 
	/// author Jon S. Berndt
	/// </summary>
	public class Gain : FCSComponent
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

        public Gain(FlightControlSystem fcs, XmlElement element)
            : base(fcs, element)
        {
            XmlElement scale_element, zero_centered;
            XmlNodeList childs;
            string gain_string, sZeroCentered;

            XmlElement gainElement = element.GetElementsByTagName("gain")[0] as XmlElement;
            if (compType.Equals("PURE_GAIN"))
            {
                if (gainElement == null)
                {
                    if (log.IsErrorEnabled)
                        log.Error("No GAIN specified (default: 1.0)");
                }
            }
            if (gainElement != null)
            {
                gain_string = gainElement.InnerText.Trim();
                Match match = testRegex.Match(gain_string);
                if (match.Success && match.Groups["prop1"].Value.Length != 0)
                { // property
                    gainPropertyNode = fcs.GetPropertyManager().GetPropertyNode(match.Groups["prop1"].Value);
                }
                else
                {
                    gain = FormatHelper.ValueAsNumber(gainElement);
                }
            }
            if (compType.Equals("AEROSURFACE_SCALE"))
            {
                scale_element = element.GetElementsByTagName("domain")[0] as XmlElement;
                if (scale_element != null)
                {
                    childs = scale_element.GetElementsByTagName("max");
                    inMax = FormatHelper.ValueAsNumber(childs[0] as XmlElement);
                    childs = scale_element.GetElementsByTagName("min");
                    inMin = FormatHelper.ValueAsNumber(childs[0] as XmlElement);
                }

                scale_element = element.GetElementsByTagName("range")[0] as XmlElement;
                if (scale_element == null)
                {
                    if (log.IsErrorEnabled)
                        log.Error("No range supplied for aerosurface scale component");

                    throw new Exception("No range supplied for aerosurface scale component");
                }
                try
                {
                    childs = scale_element.GetElementsByTagName("max");
                    outMax = FormatHelper.ValueAsNumber(childs[0] as XmlElement);
                    childs = scale_element.GetElementsByTagName("min");
                    outMin = FormatHelper.ValueAsNumber(childs[0] as XmlElement);
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                        log.Error("Maximum and minimum output values must be supplied for the " +
                            "aerosurface scale component");
                    throw new Exception("Maximum and minimum output values must be supplied. Catch exception: " + e);
                }
                zeroCentered = true;
                zero_centered = element.GetElementsByTagName("zero_centered")[0] as XmlElement;
                if (zero_centered != null)
                {
                    sZeroCentered = zero_centered.InnerText.Trim();
                    if (sZeroCentered.Equals("0") || sZeroCentered.Equals("false"))
                    {
                        zeroCentered = false;
                    }
                }
            }

            if (compType.Equals("SCHEDULED_GAIN"))
            {
                XmlElement tableElement = element.GetElementsByTagName("table")[0] as XmlElement;
                if (tableElement != null)
                {
                    table = new Table(fcs.GetPropertyManager(), tableElement);
                }
                else
                {
                    if (log.IsErrorEnabled)
                        log.Error("A table must be provided for the scheduled gain component");
                    throw new Exception("A table must be provided for the scheduled gain component");
                }
            }

            base.Bind();
        }

        public override bool Run()
        {
            double schedGain = 1.0;

            input = inputNodes[0].GetDouble() * inputSigns[0];

            if (gainPropertyNode != null)
                gain = gainPropertyNode.GetDouble();

            if (compType.Equals("PURE_GAIN"))
            {                       // PURE_GAIN

                output = gain * input;

            }
            else if (compType.Equals("SCHEDULED_GAIN"))
            {           // SCHEDULED_GAIN

                schedGain = table.GetValue();
                output = gain * schedGain * input;

            }
            else if (compType.Equals("AEROSURFACE_SCALE"))
            {        // AEROSURFACE_SCALE

                if (zeroCentered)
                {
                    if (input == 0.0)
                    {
                        output = 0.0;
                    }
                    else if (input > 0)
                    {
                        output = (input / inMax) * outMax;
                    }
                    else
                    {
                        output = (input / inMin) * outMin;
                    }
                }
                else
                {
                    output = outMin + ((input - inMin) / (inMax - inMin)) * (outMax - outMin);
                }

                output *= gain;
            }

            Clip();

            if (isOutput) SetOutput();

            return true;
        }


		private Table table;
        private PropertyNode gainPropertyNode;
        private double gain = 1.000;
        private double inMin = -1.0, inMax = 1.0, outMin = 0.0, outMax = 0.0;
        private int rows = 0;
        private bool zeroCentered;

        private const string testRegExpStr = "(?<prop1>" +  FormatHelper.propertyStr + ")";
        private static readonly Regex testRegex = new Regex(testRegExpStr, RegexOptions.Compiled);

	}
}
