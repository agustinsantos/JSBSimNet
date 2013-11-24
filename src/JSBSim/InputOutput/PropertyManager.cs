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
#region Identification
/// $Id:$
#endregion
namespace JSBSim.InputOutput
{
	using System;
	using System.Reflection;
	using System.Collections;
    using System.Collections.Generic;
	using System.Text.RegularExpressions;
    
    // Import log4net classes.
    using log4net;

	using JSBSim.Script;

	/// <summary>
	/// Summary description for PropertyManager.
	/// </summary>
	public class PropertyManager
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
		public PropertyManager() : this(false)
		{
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public PropertyManager(bool buildStaticMethods) 
		{
			if (buildStaticMethods)
			{
				Type[] types = Assembly.GetCallingAssembly().GetTypes();

				// loop through all types and look for properties
				foreach (Type type in types) 
				{
					PropertyInfo[] propertiesInfo = type.GetProperties(BindingFlags.Public|BindingFlags.Static);

					// loop through all properties and look for ones marked with attributes
					foreach(PropertyInfo propInfo in propertiesInfo) 
					{
						// get as many script attributes as there are on this type
						ScriptAttribute[] scriptAtts = 
							(ScriptAttribute[])propInfo.GetCustomAttributes(typeof(ScriptAttribute), true);

						// loop through each one we found and register its command
						for(int j = 0; j < scriptAtts.Length; j++) 
						{
							ScriptAttribute scriptAtt = scriptAtts[j];

							TieStatic(scriptAtt.Name, type, propInfo, false);
						} // for
					} // for
				}
			}
		}

		public void Bind(string path, object target)
		{
			PropertyInfo[] propertiesInfo = target.GetType().GetProperties();

			// loop through all properties and look for ones marked with attributes
			foreach(PropertyInfo propInfo in propertiesInfo) 
			{
				// get as many script attributes as there are on this type

				/* TODO I dont understand why this code doesnt work....???
				ScriptAttribute[] scriptAtts = 
					(ScriptAttribute[])propInfo.GetCustomAttributes(typeof(ScriptAttribute), true);
				*/
				ScriptAttribute[] scriptAtts = 
					(ScriptAttribute[])Attribute.GetCustomAttributes(propInfo, typeof(ScriptAttribute), true);


				// loop through each one we found and register its command
				for(int j = 0; j < scriptAtts.Length; j++) 
				{
					ScriptAttribute scriptAtt = scriptAtts[j];
						
					if (path != null && path.Length != 0)
						Tie(path+"/"+scriptAtt.Name, target, propInfo, false);
					else 
						Tie(scriptAtt.Name, target, propInfo, false);

				} // for
			} // for

		}

		public void Unbind(string path, object target)
		{
			//TODO
		}

		/// <summary>
		/// Get a property node.
		/// </summary>
		/// <param name="path">The path of the node, relative to root.</param>
		/// <param name="create">true to create the node if it doesn't exist.</param>
		/// <returns>The node, or null if none exists and none was created.</returns>
		public PropertyNode GetNode____Obsolete (string path, bool create)
		{
			return null; 
		}

		public PropertyNode GetNode____Obsolete (string path, int index, bool create)
		{
			return null;
		}

		/// <summary>
		/// Gets a property.
		/// </summary>
		public PropertyNode GetPropertyNode(string name)
		{
            if (propertyNodes.ContainsKey(name))
                return propertyNodes[name];
            else
            {
                foreach (string key in propertyNodes.Keys)
                {
                    if (key.StartsWith(name))
                        return propertyNodes[key];
                }

				throw new Exception("Property "+ name +" does not exist");
            }	
            /* TODO
			if (name.Length > 1)
				if (name[0] == '/')
					return (PropertyNode)propertyNodes[name];
				else
					return (PropertyNode)propertyNodes["/"+name];
			else return null;
			*/
		}

        public PropertyNode GetPropertyNode(string name, bool create)
        {
            if (propertyNodes.ContainsKey(name))
                return propertyNodes[name];
            else
            {
                foreach (string key in propertyNodes.Keys)
                {
                    if (key.StartsWith(name))
                        return propertyNodes[key];
                }

                if (create)
                {
                    DoubleWrapper doubleWrapper = new DoubleWrapper();
                    PropertyNode node = new PropertyNode(name,
                        (PropertyNode.GetDoubleValueDelegate)doubleWrapper.GetDoubleValue,
                        (PropertyNode.SetDoubleValueDelegate)doubleWrapper.SetDoubleValue);
                    propertyNodes.Add(name, node);
      
                    if (log.IsWarnEnabled)
                        log.Warn("Creating property " + name + ". This is not fully implemented!!");

                    return node;
                }else
                throw new Exception("Property " + name + " does not exist");
            }
        }

		public PropertyNode GetPropertyNode() 
		{
			return null; 
		}

		
		/// <summary>
		/// Test whether a given node exists.
		/// </summary>
		/// <param name="path">The path of the node, relative to root.</param>
		/// <returns>true if the node exists, false otherwise.</returns>
		public bool HasNode (string path)
		{ return false;}


		/// <summary>
		/// Get the name of a node
		/// </summary>
		/// <returns></returns>
		public string GetName() { return "";}


		/// <summary>
		/// Get the fully qualified name of a node
		/// This function is very slow, so is probably useful for debugging only.
		/// </summary>
		/// <returns>the fully qualified name</returns>
		public string GetFullyQualifiedName() { return "";}




		////////////////////////////////////////////////////////////////////////
		// Convenience functions for setting property attributes.
		////////////////////////////////////////////////////////////////////////


		/**
			 * Set the state of the archive attribute for a property.
			 *
			 * If the archive attribute is true, the property will be written
			 * when a flight is saved; if it is false, the property will be
			 * skipped.
			 *
			 * A warning message will be printed if the property does not exist.
			 *
			 * @param name The property name.
			 * @param state The state of the archive attribute (defaults to true).
			 */
		public void SetArchivable (string name, bool state)
		{
			///TODO
		}


		/**
			 * Set the state of the read attribute for a property.
			 *
			 * If the read attribute is true, the property value will be readable;
			 * if it is false, the property value will always be the default value
			 * for its type.
			 *
			 * A warning message will be printed if the property does not exist.
			 *
			 * @param name The property name.
			 * @param state The state of the read attribute (defaults to true).
			 */
		public void SetReadable (string name, bool state)		
		{
			///TODO
		}


		/**
			 * Set the state of the write attribute for a property.
			 *
			 * If the write attribute is true, the property value may be modified
			 * (depending on how it is tied); if the write attribute is false, the
			 * property value may not be modified.
			 *
			 * A warning message will be printed if the property does not exist.
			 *
			 * @param name The property name.
			 * @param state The state of the write attribute (defaults to true).
			 */
		public void SetWritable (string name, bool state)		
		{
			///TODO
		}

		/// <summary>
		/// Untie a property from an external data source.
		/// </summary>
		/// <param name="name">Classes should use this function to release control of any
		/// properties they are managing.</param>
		public void Untie (string name)		
		{
			///TODO
		}


		/// <summary>
		/// Tie a property to an external variable.
		/// The property's value will automatically mirror the variable's
		/// value, and vice-versa, until the property is untied.
		/// </summary>
		/// <param name="name">The property name to tie (full path).</param>
		/// <param name="val">the object to be tied</param>
		/// <param name="useDefault">true if any existing property value should be
		/// copied to the variable; false if the variable should not
		/// be modified; defaults to true.</param>
		public void	Tie (string name,  object val, PropertyInfo prop, bool useDefault)		
		{
            if (!propertyNodes.ContainsKey(name))
            {
                PropertyNode node = new PropertyNode(name, val, prop);
                propertyNodes.Add(name, node);
            }
            else
            {
                if (log.IsWarnEnabled)
                    log.Warn("PropertyManager. Key already included:" + name);
            }
		}

        public void Tie(string name, int index, 
                        PropertyNode.GetIndexedPropertyDelegate getDel,
                        PropertyNode.SetIndexedPropertyDelegate setDel)
        {
            if (!propertyNodes.ContainsKey(name))
            {
                PropertyNode node = new PropertyNode(name, index, getDel, setDel);
                propertyNodes.Add(name, node);
            }
            else
            {
                if (log.IsWarnEnabled)
                    log.Warn("PropertyManager. Key already included:" + name);
            }
        }

        public void TieInt32(string name,
        PropertyNode.GetInt32ValueDelegate getDel,
        PropertyNode.SetInt32ValueDelegate setDel)
        {
            if (!propertyNodes.ContainsKey(name))
            {
                PropertyNode node = new PropertyNode(name, getDel, setDel);
                propertyNodes.Add(name, node);
            }
            else
            {
                if (log.IsWarnEnabled)
                    log.Warn("PropertyManager. Key already included:" + name);
            }
        }

        public void Tie(string name,
                PropertyNode.GetDoubleValueDelegate getDel,
                PropertyNode.SetDoubleValueDelegate setDel)
        {
            if (!propertyNodes.ContainsKey(name))
            {
                PropertyNode node = new PropertyNode(name, getDel, setDel);
                propertyNodes.Add(name, node);
            }
            else
            {
                if (log.IsWarnEnabled)
                    log.Warn("PropertyManager. Key already included:" + name);
            }
        }

        public void TieStatic(string name, Type type, PropertyInfo prop, bool useDefault)
        {
            if (!propertyNodes.ContainsKey(name))
            {
                PropertyNode node = new PropertyNode(name, type, prop);
                propertyNodes.Add(name, node);
            }
            else
            {
                if (log.IsWarnEnabled)
                    log.Warn("PropertyManager. Key already included:" + name);
            }
        }

        static public string MakePropertyName(string name, bool lowercase)
        {
            /* do this two pass to avoid problems with characters getting skipped
               because the index changed */
            char[] nameOutput = new char[name.Length];
            for (int i = 0; i < name.Length; i++)
            {
                if (lowercase && char.IsUpper(name[i]))
                    nameOutput[i] = char.ToLower(name[i]);
                else if (char.IsSeparator(name[i]))
                    nameOutput[i] = '-';
                else
                    nameOutput[i] = name[i];
            }
            return new string(nameOutput);
        }

        protected Dictionary<string, PropertyNode> propertyNodes = new Dictionary<string, PropertyNode>();
	}
}
