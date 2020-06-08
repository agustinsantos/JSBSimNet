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
    using System.Xml;
    using CommonUtils.IO;
    using JSBSim.Format;
    using JSBSim.MathValues;
    using JSBSim.Models;
    // Import log4net classes.
    using log4net;

    /// <summary>
    /// Models a deadband object.
    /// This is a component that allows for some "play" in a control path, in the
    /// form of a dead zone, or deadband.The form of the deadband component
    /// specification is:
    /// <code>
    /// <pre>
    /// <deadband name="Windup Trigger">
    ///  <input> {[-] property name | value </input>
    ///    <width> {[-] property name | value} </width>
    ///    [<gain> { value } </gain>
    ///    <clipto>
    ///      <min> {[-] property name | value} </min>
    ///      <max> {[-] property name | value} </max>
    ///    </clipto>]
    ///    [<output> {property} </output>]
    ///  </deadband>
    /// </pre>
    /// </code>
    /// The width value is the total deadband region within which an input will
    /// produce no output.For example, say that the width value is 2.0. If the
    /// input is between -1.0 and +1.0, the output will be zero.
    /// </summary>
    public class DeadBand : FCSComponent
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


        public DeadBand(FlightControlSystem fcs, XmlElement element)
            : base(fcs, element)
        {
            width = null;
            gain = 1.0;

            XmlElement  width_element = element.FindElement("width");
            if (width_element != null)
                width = new  ParameterValue(width_element,propertyManager);
            else
                width = new  RealValue(0.0);

            XmlElement gain_element = element.FindElement("gain");
            if (gain_element != null)
                gain = FormatHelper.ValueAsNumber(gain_element);

            Bind(element);
            Debug(0);
        }

        public override bool Run()
        {
            input = inputNodes[0].GetDoubleValue();

            double HalfWidth = 0.5 * width.GetValue();

            if (input < -HalfWidth)
            {
                output = (input + HalfWidth) * gain;
            }
            else if (input > HalfWidth)
            {
                output = (input - HalfWidth) * gain;
            }
            else
            {
                output = 0.0;
            }

            Clip();
            SetOutput();

            return true;
        }

        private IParameter width;
        private double gain = 1.0;
    }
}
