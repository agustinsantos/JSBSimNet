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

	// Import log4net classes.
	using log4net;

	using JSBSim.InputOutput;
	using JSBSim.Models;
    using JSBSim.Format;

	/// <summary>
	/// Models a deadband object.
    /// Here is the format of the deadband control specification:
    /// <code>
    /// <pre>
    /// \<COMPONENT NAME="Deadbeat1" TYPE="DEADBAND">
    ///    INPUT {input}
    ///    WIDTH {deadband width}
    ///    MIN {minimum value}
    ///    MAX {maximum value}
    ///    [GAIN {optional deadband gain}]
    ///    [OUTPUT {optional output parameter to set}]
    /// \</COMPONENT>
    /// </pre>
    /// </code>
    /// The WIDTH value is the total deadband region within which an input will
    /// produce no output. For example, say that the WIDTH value is 2.0.  If the
    /// input is between -1.0 and +1.0, the output will be zero.
	/// </summary>
	public class DeadBand  : FCSComponent
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
            foreach (XmlNode currentNode in element.ChildNodes)
            {
                if (currentNode.NodeType == XmlNodeType.Element)
                {
                    XmlElement currentElement = (XmlElement)currentNode;

                    if (currentElement.LocalName.Equals("width"))
                    {
                        width = FormatHelper.ValueAsNumber(currentElement);

                    }
                    else if (currentElement.LocalName.Equals("gain"))
                    {
                        gain = FormatHelper.ValueAsNumber(currentElement);
                    }
                }

            }
            base.Bind();
        }

		public override bool Run()
		{
			input = inputNodes[0].GetDouble();

			if (input < -width/2.0) 
			{
				output = (input + width/2.0)*gain;
			} 
			else if (input > width/2.0) 
			{
				output = (input - width/2.0)*gain;
			} 
			else 
			{
				output = 0.0;
			}

            Clip();

			if (isOutput) SetOutput();

			return true;
		}

        private double width = 0.0;
		private double gain = 1.0;
	}
}
