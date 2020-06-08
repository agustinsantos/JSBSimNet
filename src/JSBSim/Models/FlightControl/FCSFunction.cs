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
    using JSBSim.MathValues;
    using JSBSim.Models;
    // Import log4net classes.
    using log4net;


    /// <summary>
    /// Models a FCSFunction object.
    /// original C++ author Jon S.Berndt
    /// 
    /// One of the most recent additions to the FCS component set is the FCS Function
    /// component.This component allows a function to be created when no other
    /// component is suitable.Available mathematical operations are described in the
    /// FGFunction class.
    /// The function component is defined as follows:
    /// 
    /// @code
    /// <fcs_function name="Windup Trigger">
    ///   [< input > [-]property </ input >]
    ///   <function>
    ///     ...
    ///   </function>
    ///   [<clipto>
    ///     <min> {[-] property name | value </min>
    ///     <max> {[-] property name | value} </max>
    ///   </clipto>]
    ///   [<output> {property} </output>]
    /// </ fcs_function >
    /// @endcode
    /// 
    /// The function definition itself can include a nested series of products, sums,
    /// quotients, etc. as well as trig and other math functions.Here's an example of
    /// a function(from an aero specification):
    /// 
    /// @code
    /// <function name="aero/coefficient/CDo">
    ///     <description>Drag_at_zero_lift</description>
    ///     <product>
    ///         <property>aero/qbar-psf</property>
    ///         <property>metrics/Sw-sqft</property>
    ///         <table>
    ///             <independentVar>velocities/mach</independentVar>
    ///             <tableData>
    ///                 0.0000  0.0220
    ///                 0.2000  0.0200
    ///                 0.6500  0.0220
    ///                 0.9000  0.0240
    ///                 0.9700  0.0500
    ///             </tableData>
    ///         </table>
    ///     </product>
    /// </function>
    /// @endcode
    /// </summary>
    public class FCSFunction : FCSComponent
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

        public FCSFunction(FlightControlSystem fcs, XmlElement element)
            : base(fcs, element)
        {
            XmlElement function_element = element.FindElement("function");

            if (function_element != null)
                function = new Function(fcs.GetExec(), function_element);
            else
            {
                log.Error("FCS Function should contain a \"function\" element");
                throw new Exception("Malformed FCS function specification.");
            }

            Bind(element);
            Debug(0);
        }

        public override bool Run()
        {
            output = function.GetValue();

            if (inputNodes.Count > 0)
            {
                input = inputNodes[0].GetDoubleValue();
                output *= input;
            }

            Clip();
            SetOutput();

            return true;
        }

        private Function function;
    }
}
