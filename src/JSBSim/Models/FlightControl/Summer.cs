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
	using System.IO;

	// Import log4net classes.
	using log4net;

	using JSBSim.InputOutput;
	using JSBSim.Models;
	using CommonUtils.IO;
    using JSBSim.Format;

	/// <summary>
	/// Models a flight control system summing component.
	/// The Summer component sums two or more inputs. These can be pilot control
	/// inputs or state variables, and a bias can also be added in using the BIAS
	/// keyword.  The form of the summer component specification is:
	/// <pre>
	/// \<COMPONENT NAME="name" TYPE="SUMMER">
	/// INPUT \<property>
	/// INPUT \<property>
	/// [BIAS \<value>]
	/// [?]
	/// [CLIPTO \<min> \<max> 1]
	/// [OUTPUT \<property>]
	/// \</COMPONENT>
	/// </pre>
	/// Note that in the case of an input property the property name may be
	/// immediately preceded by a minus sign. Here's an example of a summer
	/// component specification:
	/// <pre>
	/// \<COMPONENT NAME="Roll A/P Error summer" TYPE="SUMMER">
	/// INPUT  velocities/p-rad_sec
	/// INPUT -fcs/roll-ap-wing-leveler
	/// INPUT  fcs/roll-ap-error-integrator
	/// CLIPTO -1 1
	/// \</COMPONENT>
	/// </pre>
	/// Note that there can be only one BIAS statement per component.
    /// 
	/// </summary>
	public class Summer : FCSComponent
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

        public Summer(FlightControlSystem fcs, XmlElement element)
            : base(fcs, element)
        {
            XmlNodeList childs = element.GetElementsByTagName("bias");
            if (childs != null && childs.Count > 0)
                Bias = FormatHelper.ValueAsNumber(childs[0] as XmlElement);

            base.Bind();
        }



		/// The execution method for this FCS component.
		public override bool Run()
		{
			int idx;

			// The Summer takes several inputs, so do not call the base class Run()
			// FGFCSComponent::Run();

			output = 0.0;

			for (idx=0; idx<inputNodes.Count; idx++) 
			{
				output += inputNodes[idx].GetDouble()*inputSigns[idx];
			}

			output += Bias;

            Clip();
			if (isOutput) SetOutput();

			return true;
		}

        private double Bias = 0.0;
	}
}
