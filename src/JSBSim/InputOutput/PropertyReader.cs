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

namespace JSBSim.InputOutput
{
    using System.Collections.Generic;
    using System.Xml;
    using log4net;

    public class PropertyReader
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


        public void Load(XmlElement el, PropertyManager PM, bool _override)
        {
            XmlNodeList elemList = el.GetElementsByTagName("property");

            if (elemList.Count > 0 && log.IsDebugEnabled)
            {
                string cout = "\n    ";
                if (_override)
                    cout += "Overriding";
                else
                    cout += "Declared";
                cout += " properties \n\n";
                log.Debug(cout);
            }

            foreach (XmlNode xmlNode in elemList)
            {
                if (xmlNode.NodeType == XmlNodeType.Element)
                {
                    XmlElement property_element = xmlNode as XmlElement;
                    PropertyNode node = null;
                    double value = 0.0;
                    if (!string.IsNullOrEmpty(property_element.GetAttribute("value")))
                        value = double.Parse(property_element.GetAttribute("value"));

                    string interface_property_string = property_element.InnerText;
                    if (PM.HasNode(interface_property_string))
                    {
                        if (_override)
                        {
                            node = PM.GetNode(interface_property_string);

                            if (log.IsDebugEnabled)
                            {
                                if (interface_prop_initial_value.ContainsKey(node))
                                {
                                    log.Debug("  The following property will be overridden but it has not been\n" +
                                              "  defined in the current model '" + el.Name + "'");
                                }

                                log.Debug("      " + "Overriding value for property " + interface_property_string + "\n" +
                                                     "       (old value: " + node.Get() + "  new value: " + value + ")");
                            }

                            node.Set(value);
                        }
                        else
                        {
                            log.Error("      Property " + interface_property_string
                                 + " is already defined.");
                            continue;
                        }
                    }
                    else
                    {
                        node = PM.GetNode(interface_property_string, true);
                        if (node != null)
                        {
                            node.Set(value);

                            if (log.IsDebugEnabled)
                                log.Debug("      " + interface_property_string + " (initial value: "
                                     + value + ")"); ;
                        }
                        else
                        {
                            log.Error("Could not create property " + interface_property_string); ;
                            continue;
                        }
                    }
                    interface_prop_initial_value[node] = value;
                    if (property_element.GetAttribute("persistent") == "true")
                        node.SetAttribute(PropertyNode.Attribute.PRESERVE, true);

                }

                // End of interface property loading logic
            }
        }
        public bool ResetToIC()
        {
            foreach (var v in interface_prop_initial_value)
            {
                PropertyNode node = v.Key;
                if (!node.GetAttribute(PropertyNode.Attribute.PRESERVE))
                    node.Set(v.Value);
            }

            return true;
        }



        private Dictionary<PropertyNode, double> interface_prop_initial_value = new Dictionary<PropertyNode, double>();

    }
}
