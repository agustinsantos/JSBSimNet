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
    using System;
    using JSBSim.InputOutput;

    /// <summary>
    /// Represents a property value which can use late binding.
    /// @author Jon Berndt, Anders Gidenstam
    /// </summary>
    public class PropertyValue : Parameter
    {
        public PropertyValue(PropertyNode propNode)
        {
            this.propertyManager = null;
            this.propertyNode = propNode;
            this.sign = 1.0;

        }

        public PropertyValue(string propName, PropertyManager propertyManager)
        {
            this.propertyManager = propertyManager;
            this.propertyNode = null;
            this.propertyName = propName;
            this.sign = 1.0;

            if (propertyName[0] == '-')
            {
                propertyName = propertyName.Remove(0, 1);
                sign = -1.0;
            }

            if (propertyManager.HasNode(propertyName))
                propertyNode = propertyManager.GetNode(propertyName);

        }

        public override double GetValue()
        {
            return GetNode().GetDouble() * sign; ;
            //return doubleDelegate() * sign; ;
        }

        public override bool IsConstant()
        {
            return propertyNode != null && (!propertyNode.IsTied()
                                        && !propertyNode.GetAttribute(PropertyNode.Attribute.WRITE));
        }

        public void SetNode(PropertyNode node) { propertyNode = node; }

        public void SetValue(double value)
        {
            GetNode().Set(value);
        }

        public bool IsLateBound() { return propertyNode == null; }

        public override string GetName()
        {
            if (propertyNode != null)
                return propertyNode.GetName();
            else
                return propertyName;
        }

        public virtual string GetNameWithSign()
        {
            string name = "";

            if (sign < 0.0) name = "-";

            name += GetName();

            return name;
        }

        public virtual string GetFullyQualifiedName()
        {
            if (propertyNode != null)
                return propertyNode.GetFullyQualifiedName();
            else
                return propertyName;
        }

        public virtual string GetPrintableName()
        {
            if (propertyNode != null)
                return propertyNode.GetPrintableName();
            else
                return propertyName;
        }


        protected PropertyNode GetNode()
        {
            if (propertyNode == null)
            {
                PropertyNode node = propertyManager.GetPropertyNode(propertyName);

                if (node == null)
                    throw new Exception("PropertyValue.GetValue() The property " +
                                       propertyName + " does not exist.");

                propertyNode = node;
            }

            return propertyNode;
        }

        public PropertyManager propertyManager; // Property root used to do late binding.
        private PropertyNode propertyNode;
        private string propertyName;
        private double sign;
    }
}
