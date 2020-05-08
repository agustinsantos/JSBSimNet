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
namespace JSBSim.MathValues
{
    using System.Xml;
    using JSBSim.InputOutput;

    // Import log4net classes.
    using log4net;

    public class TemplateFunc : Function
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


        public TemplateFunc(FDMExecutive fdmex, XmlElement element) : base(fdmex.PropertyManager)
        {
            var = new PropertyValue(null);
            Load(element, var, fdmex);
            CheckMinArguments(element, 1);
            CheckMaxArguments(element, 1);
        }

        public double GetValue(PropertyNode node)
        {
            var.SetNode(node);
            return base.GetValue();
        }


        /// <summary>
        /// TemplateFunc must not be bound to the property manager. The bind method
        ///  is therefore made private and overloaded as a no-op
        /// </summary>
        /// <param name="el"></param>
        /// <param name="str"></param>
        protected override void Bind(XmlElement el, string str) { }
        private PropertyValue var;
    }
}
