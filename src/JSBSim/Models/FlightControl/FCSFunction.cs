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
    using JSBSim.MathValues;
    using JSBSim.Format;

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
            XmlNodeList childs = element.GetElementsByTagName("function");
            if (childs != null && childs.Count > 0)
                function = new Function(fcs.GetPropertyManager(), childs[0] as XmlElement);

            base.Bind();

        }
        public override bool Run()
        {
            output = function.Value;

            if (inputNodes.Count > 0)
            {
                input = inputNodes[0].GetDouble() * inputSigns[0];
                output *= input;
            }

            Clip();

            if (isOutput) SetOutput();

            return true;

        }

        private Function function;
    }
}
