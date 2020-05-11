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

    public class ParameterValue : Parameter
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

        public ParameterValue(XmlElement el, PropertyManager pm)
        {
            string value = el.InnerText;

            if (string.IsNullOrEmpty(value))
            {
                log.Error("The element <" + el.Name + "> must either contain a value number or a property name.");
                throw new System.Exception("Illegal argument");
            }

            Construct(value, pm);
        }

        public ParameterValue(string value, PropertyManager pm)
        {
            Construct(value, pm);
        }

        public override double GetValue()
        {
            return param.GetValue();
        }
        public override bool IsConstant() { return param.IsConstant(); }

        public override string GetName()
        {
            PropertyValue v = param as PropertyValue;
            if (v != null)
                return v.GetNameWithSign();
            else
                return param.GetValue().ToString();
        }

        public bool IsLateBound()
        {
            PropertyValue v = param as PropertyValue;
            return v != null && v.IsLateBound();
        }
        private IParameter param;

        private void Construct(string value, PropertyManager pm)
        {
            double tmp;
            if (double.TryParse(value, out tmp))
            {
                param = new RealValue(tmp);
            }
            else
            {
                // "value" must be a property if execution passes to here.
                param = new PropertyValue(value, pm);
            }
        }
    }
}
