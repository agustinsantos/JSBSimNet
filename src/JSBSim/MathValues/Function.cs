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
namespace JSBSim.MathValues
{
	using System;
	using System.Collections.Generic;
	using System.Xml;
	
	// Import log4net classes.
	using log4net;

	using JSBSim.InputOutput;
	using JSBSim.Format;

	/// <summary>
	/// Represents various types of parameters.
	/// </summary>
	public class Function : IParameter
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

		public Function(PropertyManager propMan, XmlElement element) :this(propMan, element, "")
		{
		}

        public Function(PropertyManager propMan, XmlElement element, string strPrefix)
        {
            //if (log.IsDebugEnabled)
            //    log.Debug("In Function.Ctor");

            propertyManager = propMan;

            prefix = strPrefix;

            name = element.GetAttribute("name");
            string operation = element.LocalName;
            if (operation.Equals("function"))
            {
                functionType = FunctionType.TopLevel;
                Bind();
            }
            else if (operation.Equals("product"))
                functionType = FunctionType.Product;
            else if (operation.Equals("product"))
                functionType = FunctionType.TopLevel;
            else if (operation.Equals("difference"))
                functionType = FunctionType.Difference;
            else if (operation.Equals("sum"))
                functionType = FunctionType.Sum;
            else if (operation.Equals("quotient"))
                functionType = FunctionType.Quotient;
            else if (operation.Equals("pow"))
                functionType = FunctionType.Pow;
            else if (operation.Equals("abs"))
                functionType = FunctionType.Abs;
            else if (operation.Equals("sin"))
                functionType = FunctionType.Sin;
            else if (operation.Equals("cos"))
                functionType = FunctionType.Cos;
            else if (operation.Equals("tan"))
                functionType = FunctionType.Tan;
            else if (operation.Equals("asin"))
                functionType = FunctionType.ASin;
            else if (operation.Equals("acos"))
                functionType = FunctionType.ACos;
            else if (operation.Equals("atan"))
                functionType = FunctionType.ATan;
            else if (operation.Equals("atan2"))
                functionType = FunctionType.ATan2;
            else if (!operation.Equals("description"))
            {
                log.Error("Bad operation <" + operation + "> detected in configuration file");
            }

            foreach (XmlNode currentNode in element.ChildNodes)
            {
                if (currentNode.NodeType == XmlNodeType.Element)
                {
                    XmlElement currentElement = (XmlElement)currentNode;

                    operation = currentElement.LocalName;
                    //if (log.IsDebugEnabled)
                    //    log.Debug("In Function.Ctor, Procesing tag=" + operation);

                    if (operation.Equals("property"))
                    {
                        string property_name = currentElement.InnerText;
                        parameters.Add(new PropertyValue(propertyManager.GetPropertyNode(property_name)));
                    }
                    else if (operation.Equals("value"))
                    {
                        parameters.Add(new RealValue(FormatHelper.ValueAsNumber(currentElement)));
                    }
                    else if (operation.Equals("table"))
                    {
                        parameters.Add(new Table(propertyManager, currentElement));
                        // operations
                    }
                    else if (operation.Equals("product") ||
                        operation.Equals("difference") ||
                        operation.Equals("sum") ||
                        operation.Equals("quotient") ||
                        operation.Equals("pow") ||
                        operation.Equals("abs") ||
                        operation.Equals("sin") ||
                        operation.Equals("cos") ||
                        operation.Equals("tan") ||
                        operation.Equals("asin") ||
                        operation.Equals("acos") ||
                        operation.Equals("atan") ||
                        operation.Equals("atan2"))
                    {
                        parameters.Add(new Function(propertyManager, currentElement));
                    }
                    else if (!operation.Equals("description"))
                    {
                        log.Error("Bad operation <" + operation + "> detected in configuration file");
                    }
                }
            }
            //Bind();
        }

		public double Value 
		{
			get { return GetValue(); }
		}

		public double GetValue() 
		{
			if (cached) return cachedValue;
			double temp = parameters[0].GetValue();

			switch (functionType) 
			{
				case FunctionType.TopLevel:
					break;
				case FunctionType.Product:
					for (int i=1;i<parameters.Count;i++) temp *= parameters[i].GetValue();
					break;
				case FunctionType.Difference:
					for (int i=1;i<parameters.Count;i++) temp -= parameters[i].GetValue();
					break;
				case FunctionType.Sum:
					for (int i=1;i<parameters.Count;i++) temp += parameters[i].GetValue();
					break;
				case FunctionType.Quotient:
					temp /= parameters[1].GetValue();
					break;
				case FunctionType.Pow:
					temp = System.Math.Pow(temp, parameters[1].GetValue());
					break;
				case FunctionType.Abs:
					temp = System.Math.Abs(temp);
					break;
				case FunctionType.Sin:
					temp = System.Math.Sin(temp);
					break;
				case FunctionType.Cos:
					temp = System.Math.Cos(temp);
					break;
				case FunctionType.Tan:
					temp = System.Math.Tan(temp);
					break;
				case FunctionType.ACos:
					temp = System.Math.Acos(temp);
					break;
				case FunctionType.ASin:
					temp = System.Math.Asin(temp);
					break;
				case FunctionType.ATan:
					temp = System.Math.Atan(temp);
					break;
                case FunctionType.ATan2:
                    temp = System.Math.Atan2(temp, parameters[1].GetValue());
                    break;
                default:
					log.Error("Unknown function operation type");
					break;
			}

			return temp;
		}

		public string GetValueAsString() 
		{
			return GetValue().ToString("f9.6",FormatHelper.numberFormatInfo); //TODO, test this format with culture != US
		}

	//%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%

		public void Bind()
		{
			//TODO Check that
/*
			string tmp = propertyManager.mkPropertyName(Prefix + Name, false); // Allow upper case
			propertyManager.Tie( tmp, this, GetValue);
*/
            if (Name.Length != 0)
            {
                propertyManager.Tie(prefix + Name, this, this.GetType().GetProperty("Value"), false);
            }
		}

		public string Name {get {return name;}}

		public void CacheValue(bool mustCache)
		{
            cached = false; // Must set cached to false prior to calling GetValue(), else
                            // it will _never_ calculate the value;
            if (mustCache)
            {
                cachedValue = GetValue();
                cached = true;
            }
		}

		private List<IParameter> parameters = new List<IParameter>(); //vector <FGParameter*>
		private PropertyManager propertyManager;
		private bool cached = false;
		private string prefix;
		private double cachedValue;
		
		private enum FunctionType 
		{
			TopLevel=0, Product, Difference, Sum, Quotient, Pow,
			Abs, Sin, Cos, Tan, ASin, ACos, ATan, ATan2};

		private FunctionType functionType;
		private string name;
	}
}
