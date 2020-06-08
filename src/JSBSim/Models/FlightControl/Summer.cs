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
    using JSBSim.Models;
    // Import log4net classes.
    using log4net;

    /// <summary>
    /// Models a flight control system summing component.
    /// The Summer component sums two or more inputs.These can be pilot control
    /// inputs or state variables, and a bias can also be added in using the BIAS
    /// keyword.The form of the summer component specification is:
    /// @code
    ///     <summer name="{string}">
    ///       <input> { string} </input>
    ///       <input> {string} </input>
    ///       <bias> {number} </bias>
    ///       <clipto>
    ///          <min> {number} </min>
    ///          <max> {number} </max>
    ///       </clipto>
    ///       <output> {string} </output>
    ///     </summer>
    /// @endcode
    /// 
    ///     Note that in the case of an input property the property name may be
    ///     immediately preceded by a minus sign.Here's an example of a summer
    ///     component specification:
    /// 
    /// @code
    ///     <summer name="Roll A/P Error summer">
    ///       <input> velocities/p-rad_sec</input>
    ///       <input> -fcs/roll-ap-wing-leveler</input>
    ///       <input> fcs/roll-ap-error-integrator</input>
    ///       <clipto>
    ///          <min> -1 </min>
    ///          <max>  1 </max> 
    ///       </clipto>
    ///     </summer>
    /// @endcode
    /// 
    /// <pre>
    ///     Notes:
    /// 
    ///     There can be only one BIAS statement per component.
    /// 
    ///     There may be any number of inputs.
    /// </pre>
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


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fcs">the parent FGFCS object.</param>
        /// <param name="element">the configuration file node.the configuration file node.</param>
        public Summer(FlightControlSystem fcs, XmlElement element)
            : base(fcs, element)
        {
            XmlElement elem = element.FindElement("bias");
            if (elem != null)
                Bias = FormatHelper.ValueAsNumber(elem);

            base.Bind();
            Debug(0);
        }



        /// <summary>
        /// The execution method for this FCS component.
        /// </summary>
        /// <returns></returns>
        public override bool Run()
        {
            output = 0.0;

            foreach (var node in inputNodes)
                output += node.GetDoubleValue();

            output += Bias;

            Clip();
            SetOutput();

            return true;
        }

        private double Bias = 0.0;
    }
}
