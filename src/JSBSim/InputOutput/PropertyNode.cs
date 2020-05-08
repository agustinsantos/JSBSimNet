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
    using System;
    using System.Collections.Generic;
    using System.Reflection;


    /// <summary>
    /// A node in a property tree.
    /// Class wrapper for property handling.
    ///  @author David Megginson, Tony Peden
    /// </summary>
    public class PropertyNode
    {

        /// <summary>
        /// Access mode attributes.
        /// 
        /// <p>The ARCHIVE attribute is strictly advisory, and controls
        /// whether the property should normally be saved and restored.</p>
        /// </summary>
        [Flags]
        public enum Attribute
        {
            NO_ATTR = 0,
            READ = 1,
            WRITE = 2,
            ARCHIVE = 4,
            REMOVED = 8,
            TRACE_READ = 16,
            TRACE_WRITE = 32,
            USERARCHIVE = 64,
            PRESERVE = 128
            // beware: if you add another attribute here,
            // also update value of "LAST_USED_ATTRIBUTE".
        }

        /// <summary>
        /// This event handler is used when a property has changed. 
        /// TODO: Not yet fully implemented and tested.
        /// </summary>
        public delegate void PropertyHasChangedEventHandler(object sender, EventArgs e);

        public delegate void SetIndexedPropertyDelegate(int Pos, double setting);
        public delegate double GetIndexedPropertyDelegate(int Pos);

        public delegate short GetInt16ValueDelegate();
        public delegate int GetInt32ValueDelegate();
        public delegate long GetInt64ValueDelegate();
        public delegate float GetSingleValueDelegate();
        public delegate double GetDoubleValueDelegate();
        public delegate bool GetBoolValueDelegate();
        public delegate string GetStringValueDelegate();

        public delegate void SetInt16ValueDelegate(short val);
        public delegate void SetInt32ValueDelegate(int val);
        public delegate void SetInt64ValueDelegate(long val);
        public delegate void SetSingleValueDelegate(float val);
        public delegate void SetDoubleValueDelegate(double val);
        public delegate void SetBoolValueDelegate(bool val);
        public delegate void SetStringValueDelegate(string val);

        public PropertyNode() { }

        public PropertyNode(string nodeName, int ind,
                        PropertyNode.GetIndexedPropertyDelegate getDel,
                        PropertyNode.SetIndexedPropertyDelegate setDel)
        {
            name = nodeName;
            index = ind;
            setIndexedDelegate = setDel;
            getIndexedDelegate = getDel;
        }

        public PropertyNode(string nodeName,
        GetInt32ValueDelegate getDel,
        SetInt32ValueDelegate setDel)
        {
            name = nodeName;
            this.Tie(getDel, setDel);
        }

        public PropertyNode(string nodeName,
                GetDoubleValueDelegate getDel,
                SetDoubleValueDelegate setDel)
        {
            name = nodeName;
            this.Tie(getDel, setDel);
        }

        public PropertyNode(string nodeName, object val, PropertyInfo prop)
        {
            name = nodeName;
            this.Tie(val, prop);
        }

        public PropertyNode(string nodeName, Type type, PropertyInfo prop)
        {
            name = nodeName;
            this.Tie(type, prop);
        }

        public bool Tie(GetIndexedPropertyDelegate getDel,
                        SetIndexedPropertyDelegate setDel,
                        bool useDefault = false)
        {
            setIndexedDelegate = setDel;
            getIndexedDelegate = getDel;
            return true;
        }
        public bool Tie(GetInt32ValueDelegate getDel,
                        SetInt32ValueDelegate setDel,
                        bool useDefault = false)
        {
            setInt32Delegate = setDel;
            getInt32Delegate = getDel;
            return true;
        }
        public bool Tie(GetInt64ValueDelegate getDel,
                        SetInt64ValueDelegate setDel,
                        bool useDefault = false)
        {
            setInt64Delegate = setDel;
            getInt64Delegate = getDel;
            return true;
        }

        public bool Tie(GetSingleValueDelegate getDel,
               SetSingleValueDelegate setDel,
               bool useDefault = false)
        {
            setSingleDelegate = setDel;
            getSingleDelegate = getDel;
            return true;
        }

        public bool Tie(GetDoubleValueDelegate getDel,
                        SetDoubleValueDelegate setDel,
                        bool useDefault = false)
        {
            _tied = true;

            setDoubleDelegate = setDel;
            getDoubleDelegate = getDel;
            return true;
        }
        public bool Tie(Type type, PropertyInfo prop, bool useDefault = false)
        {
            objectVal = null;
            propInfo = prop;
            _tied = false;

            if (propInfo.PropertyType == typeof(System.Int16))
            {
                if (propInfo.GetSetMethod() != null)
                    setInt16Delegate = (SetInt16ValueDelegate)Delegate.CreateDelegate(typeof(SetInt16ValueDelegate), type, propInfo.GetSetMethod().Name);
                if (propInfo.GetGetMethod() != null)
                    getInt16Delegate = (GetInt16ValueDelegate)Delegate.CreateDelegate(typeof(GetInt16ValueDelegate), type, propInfo.GetGetMethod().Name);
                _tied = true;
            }
            else if (propInfo.PropertyType == typeof(System.Int32))
            {
                if (propInfo.GetSetMethod() != null)
                    setInt32Delegate = (SetInt32ValueDelegate)Delegate.CreateDelegate(typeof(SetInt32ValueDelegate), type, propInfo.GetSetMethod().Name);
                if (propInfo.GetGetMethod() != null)
                    getInt32Delegate = (GetInt32ValueDelegate)Delegate.CreateDelegate(typeof(GetInt32ValueDelegate), type, propInfo.GetGetMethod().Name);
                _tied = true;
            }
            else if (propInfo.PropertyType == typeof(System.Int64))
            {
                if (propInfo.GetSetMethod() != null)
                    setInt64Delegate = (SetInt64ValueDelegate)Delegate.CreateDelegate(typeof(SetInt64ValueDelegate), type, propInfo.GetSetMethod().Name);
                if (propInfo.GetGetMethod() != null)
                    getInt64Delegate = (GetInt64ValueDelegate)Delegate.CreateDelegate(typeof(GetInt64ValueDelegate), type, propInfo.GetGetMethod().Name);
                _tied = true;
            }
            else if (propInfo.PropertyType == typeof(System.Double))
            {
                if (propInfo.GetSetMethod() != null)
                    setDoubleDelegate = (SetDoubleValueDelegate)Delegate.CreateDelegate(typeof(SetDoubleValueDelegate), type, propInfo.GetSetMethod().Name);
                if (propInfo.GetGetMethod() != null)
                    getDoubleDelegate = (GetDoubleValueDelegate)Delegate.CreateDelegate(typeof(GetDoubleValueDelegate), type, propInfo.GetGetMethod().Name);
                _tied = true;
            }
            else if (propInfo.PropertyType == typeof(System.Single))
            {
                if (propInfo.GetSetMethod() != null)
                    setSingleDelegate = (SetSingleValueDelegate)Delegate.CreateDelegate(typeof(SetSingleValueDelegate), type, propInfo.GetSetMethod().Name);
                if (propInfo.GetGetMethod() != null)
                    getSingleDelegate = (GetSingleValueDelegate)Delegate.CreateDelegate(typeof(GetSingleValueDelegate), type, propInfo.GetGetMethod().Name);
                _tied = true;
            }
            else if (propInfo.PropertyType == typeof(System.Boolean))
            {
                if (propInfo.GetSetMethod() != null)
                    setBoolDelegate = (SetBoolValueDelegate)Delegate.CreateDelegate(typeof(SetBoolValueDelegate), type, propInfo.GetSetMethod().Name);
                if (propInfo.GetGetMethod() != null)
                    getBoolDelegate = (GetBoolValueDelegate)Delegate.CreateDelegate(typeof(GetBoolValueDelegate), type, propInfo.GetGetMethod().Name);
                _tied = true;
            }
            else if (propInfo.PropertyType == typeof(System.String))
            {
                if (propInfo.GetSetMethod() != null)
                    setStringDelegate = (SetStringValueDelegate)Delegate.CreateDelegate(typeof(SetStringValueDelegate), type, propInfo.GetSetMethod().Name);
                if (propInfo.GetGetMethod() != null)
                    getStringDelegate = (GetStringValueDelegate)Delegate.CreateDelegate(typeof(GetStringValueDelegate), type, propInfo.GetGetMethod().Name);
                _tied = true;
            }
            return _tied;
        }
        public bool Tie(object val, PropertyInfo prop, bool useDefault = false)
        {
            objectVal = val;
            propInfo = prop;
            _tied = false;

            if (propInfo.PropertyType == typeof(System.Int16))
            {
                if (propInfo.GetSetMethod() != null)
                    setInt16Delegate = (SetInt16ValueDelegate)Delegate.CreateDelegate(typeof(SetInt16ValueDelegate), objectVal, propInfo.GetSetMethod().Name);
                if (propInfo.GetGetMethod() != null)
                    getInt16Delegate = (GetInt16ValueDelegate)Delegate.CreateDelegate(typeof(GetInt16ValueDelegate), objectVal, propInfo.GetGetMethod().Name);
                _tied = true;
            }
            else if (propInfo.PropertyType == typeof(System.Int32))
            {
                if (propInfo.GetSetMethod() != null)
                    setInt32Delegate = (SetInt32ValueDelegate)Delegate.CreateDelegate(typeof(SetInt32ValueDelegate), objectVal, propInfo.GetSetMethod().Name);
                if (propInfo.GetGetMethod() != null)
                    getInt32Delegate = (GetInt32ValueDelegate)Delegate.CreateDelegate(typeof(GetInt32ValueDelegate), objectVal, propInfo.GetGetMethod().Name);
                _tied = true;
            }
            else if (propInfo.PropertyType == typeof(System.Int64))
            {
                if (propInfo.GetSetMethod() != null)
                    setInt64Delegate = (SetInt64ValueDelegate)Delegate.CreateDelegate(typeof(SetInt64ValueDelegate), objectVal, propInfo.GetSetMethod().Name);
                if (propInfo.GetGetMethod() != null)
                    getInt64Delegate = (GetInt64ValueDelegate)Delegate.CreateDelegate(typeof(GetInt64ValueDelegate), objectVal, propInfo.GetGetMethod().Name);
                _tied = true;
            }
            else if (propInfo.PropertyType == typeof(System.Double))
            {
                if (propInfo.GetSetMethod() != null)
                    setDoubleDelegate = (SetDoubleValueDelegate)Delegate.CreateDelegate(typeof(SetDoubleValueDelegate), objectVal, propInfo.GetSetMethod().Name);
                if (propInfo.GetGetMethod() != null)
                    getDoubleDelegate = (GetDoubleValueDelegate)Delegate.CreateDelegate(typeof(GetDoubleValueDelegate), objectVal, propInfo.GetGetMethod().Name);
                _tied = true;
            }
            else if (propInfo.PropertyType == typeof(System.Single))
            {
                if (propInfo.GetSetMethod() != null)
                    setSingleDelegate = (SetSingleValueDelegate)Delegate.CreateDelegate(typeof(SetSingleValueDelegate), objectVal, propInfo.GetSetMethod().Name);
                if (propInfo.GetGetMethod() != null)
                    getSingleDelegate = (GetSingleValueDelegate)Delegate.CreateDelegate(typeof(GetSingleValueDelegate), objectVal, propInfo.GetGetMethod().Name);
                _tied = true;
            }
            else if (propInfo.PropertyType == typeof(System.Boolean))
            {
                if (propInfo.GetSetMethod() != null)
                    setBoolDelegate = (SetBoolValueDelegate)Delegate.CreateDelegate(typeof(SetBoolValueDelegate), objectVal, propInfo.GetSetMethod().Name);
                if (propInfo.GetGetMethod() != null)
                    getBoolDelegate = (GetBoolValueDelegate)Delegate.CreateDelegate(typeof(GetBoolValueDelegate), objectVal, propInfo.GetGetMethod().Name);
                _tied = true;
            }
            else if (propInfo.PropertyType == typeof(System.String))
            {
                if (propInfo.GetSetMethod() != null)
                    setStringDelegate = (SetStringValueDelegate)Delegate.CreateDelegate(typeof(SetStringValueDelegate), objectVal, propInfo.GetSetMethod().Name);
                if (propInfo.GetGetMethod() != null)
                    getStringDelegate = (GetStringValueDelegate)Delegate.CreateDelegate(typeof(GetStringValueDelegate), objectVal, propInfo.GetGetMethod().Name);
                _tied = true;
            }
            return false;
        }

        /// <summary>
        /// Test whether this node is bound to an external data source.
        /// </summary>
        /// <returns></returns>
        public bool IsTied() { return _tied; }

        /// <summary>
        /// Unbind this node from any external data source.
        /// </summary>
        /// <returns></returns>
        public bool Untie() { throw new NotImplementedException(); }

        /// <summary>
        /// Get a property node.
        /// </summary>
        /// <param name="path">The path of the node, relative to root.</param>
        /// <param name="create">true to create the node if it doesn't exist.</param>
        /// <returns>The node, or null if none exists and none was created.</returns>
        public PropertyNode GetNode(string path, bool create = false)
        {
            List<PathComponent> components = new List<PathComponent>();
            PathComponentUtis.ParsePath(path, components);
            return FindNode(this, components, 0, create);
        }

        /// <summary>
        /// Get a property node.
        /// </summary>
        /// <param name="relpath">The path of the node, relative to root.</param>
        /// <param name="create">true to create the node if it doesn't exist.</param>
        /// <returns>The node, or null if none exists and none was created.</returns>
        public PropertyNode GetNode(string relpath, int index, bool create = false)
        {
            List<PathComponent> components = new List<PathComponent>();
            PathComponentUtis.ParsePath(relpath, components);
            if (components.Count > 0)
                components[components.Count - 1].index = index;
            return FindNode(this, components, 0, create);
        }

        /// <summary>
        /// Test whether a given node exists.
        /// </summary>
        /// <param name="path">The path of the node, relative to root.</param>
        /// <returns>true if the node exists, false otherwise.</returns>
        public bool HasNode(string path)
        {
            //return (_type != PropertyType.NONE);
            return propInfo == null;
        }

        /// <summary>
        /// Get a bool value for a property.
        /// This method is convenient but inefficient.  It should be used
        /// infrequently(i.e. for initializing, loading, saving, etc.),
        /// not in the main loop.If you need to get a value frequently,
        /// it is better to look up the node itself using GetNode and then
        /// use the node's getBoolValue() method, to avoid the lookup overhead.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="defaultValue">The default value to return if the property does not exist.</param>
        /// <returns>The property's value as a bool, or the default value provided.</returns>
        public bool GetBool(string name, bool defaultValue = false)
        {
            PropertyNode node = GetNode(name);
            return (node == null ? defaultValue : node.GetBool());
        }

        /// <summary>
        /// Get a int value for a property.
        /// This method is convenient but inefficient.  It should be used
        /// infrequently(i.e. for initializing, loading, saving, etc.),
        /// not in the main loop.If you need to get a value frequently,
        /// it is better to look up the node itself using GetNode and then
        /// use the node's getIntValue() method, to avoid the lookup overhead.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="defaultValue">The default value to return if the property does not exist.</param>
        /// <returns>The property's value as a int, or the default value provided.</returns>
        public int GetInt(string name, int defaultValue = 0)
        {
            PropertyNode node = GetNode(name);
            return (node == null ? defaultValue : node.GetInt32());
        }

        /// <summary>
        /// Get a long value for a property.
        /// This method is convenient but inefficient.  It should be used
        /// infrequently(i.e. for initializing, loading, saving, etc.),
        /// not in the main loop.If you need to get a value frequently,
        /// it is better to look up the node itself using GetNode and then
        /// use the node's getLongValue() method, to avoid the lookup overhead.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="defaultValue">The default value to return if the property does not exist.</param>
        /// <returns>The property's value as a long, or the default value provided.</returns>
        public long GetLong(string name, long defaultValue = 0L)
        {
            PropertyNode node = GetNode(name);
            return (node == null ? defaultValue : node.GetInt64());
        }

        /// <summary>
        /// Get a float value for a property.
        /// This method is convenient but inefficient.  It should be used
        /// infrequently(i.e. for initializing, loading, saving, etc.),
        /// not in the main loop.If you need to get a value frequently,
        /// it is better to look up the node itself using GetNode and then
        /// use the node's getFloatValue() method, to avoid the lookup overhead.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="defaultValue">The default value to return if the property does not exist.</param>
        /// <returns>The property's value as a float, or the default value provided.</returns>
        public float GetFloat(string name, float defaultValue = 0.0f)
        {
            PropertyNode node = GetNode(name);
            return (node == null ? defaultValue : node.GetSingle());
        }

        /// <summary>
        /// Get a double value for a property.
        /// This method is convenient but inefficient.  It should be used
        /// infrequently(i.e. for initializing, loading, saving, etc.),
        /// not in the main loop.If you need to get a value frequently,
        /// it is better to look up the node itself using GetNode and then
        /// use the node's getDoubleValue() method, to avoid the lookup overhead.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="defaultValue">The default value to return if the property does not exist.</param>
        /// <returns>The property's value as a double, or the default value provided.</returns>
        public double GetDouble(string name, double defaultValue = 0.0)
        {
            PropertyNode node = GetNode(name);
            return (node == null ? defaultValue : node.GetDouble());
        }

        /// <summary>
        /// Get a string value for a property.
        /// This method is convenient but inefficient.  It should be used
        /// infrequently(i.e. for initializing, loading, saving, etc.),
        /// not in the main loop.If you need to get a value frequently,
        /// it is better to look up the node itself using GetNode and then
        /// use the node's getStringValue() method, to avoid the lookup overhead.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="defaultValue">The default value to return if the property does not exist.</param>
        /// <returns>The property's value as a string, or the default value provided.</returns>
        public string GetString(string name, string defaultValue = "")
        {
            PropertyNode node = GetNode(name);
            return (node == null ? defaultValue : node.GetString());
        }

        /// <summary>
        /// Set a bool value for a property.
        /// 
        ///  Assign a bool value to a property.If the property does not
        ///  yet exist, it will be created and its type will be set to
        ///  BOOL; if it has a type of UNKNOWN, the type will also be set to
        ///  BOOL; otherwise, the value type will be converted to the property's
        ///  type.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="value">The new value for the property.</param>
        /// <returns>true if the assignment succeeded, false otherwise.</returns>
        public bool SetBool(string name, bool value)
        {
            return GetNode(name, true).Set(value);
        }

        /// <summary>
        /// Set an int value for a property.
        /// 
        ///  Assign an int value to a property.If the property does not
        ///  yet exist, it will be created and its type will be set to
        ///  INT; if it has a type of UNKNOWN, the type will also be set to
        ///  INT; otherwise, the value type will be converted to the property's
        ///  type.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="value">The new value for the property.</param>
        /// <returns>true if the assignment succeeded, false otherwise.</returns>
        public bool SetInt(string name, int value)
        {
            return GetNode(name, true).Set(value);
        }

        /// <summary>
        /// Set an long value for a property.
        /// 
        ///  Assign an long value to a property.If the property does not
        ///  yet exist, it will be created and its type will be set to
        ///  LONG; if it has a type of UNKNOWN, the type will also be set to
        ///  LONG; otherwise, the value type will be converted to the property's
        ///  type.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="value">The new value for the property.</param>
        /// <returns>true if the assignment succeeded, false otherwise.</returns>
        public bool SetLong(string name, long value)
        {
            return GetNode(name, true).Set(value);
        }

        /// <summary>
        /// Set an float value for a property.
        /// 
        ///  Assign an float value to a property.If the property does not
        ///  yet exist, it will be created and its type will be set to
        ///  FLOAT; if it has a type of UNKNOWN, the type will also be set to
        ///  FLOAT; otherwise, the value type will be converted to the property's
        ///  type.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="value">The new value for the property.</param>
        /// <returns>true if the assignment succeeded, false otherwise.</returns>
        public bool SetFloat(string name, float value)
        {
            return GetNode(name, true).Set(value);
        }

        /// <summary>
        /// Set an double value for a property.
        /// 
        ///  Assign an double value to a property.If the property does not
        ///  yet exist, it will be created and its type will be set to
        ///  DOUBLE; if it has a type of UNKNOWN, the type will also be set to
        ///  DOUBLE; otherwise, the value type will be converted to the property's
        ///  type.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="value">The new value for the property.</param>
        /// <returns>true if the assignment succeeded, false otherwise.</returns>
        public bool SetDouble(string name, double value)
        {
            return GetNode(name, true).Set(value);
        }

        /// <summary>
        /// Set an string value for a property.
        /// 
        ///  Assign an string value to a property.If the property does not
        ///  yet exist, it will be created and its type will be set to
        ///  STRING; if it has a type of UNKNOWN, the type will also be set to
        ///  STRING; otherwise, the value type will be converted to the property's
        ///  type.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="value">The new value for the property.</param>
        /// <returns>true if the assignment succeeded, false otherwise.</returns>
        public bool SetString(string name, string value)
        {
            return GetNode(name, true).Set(value);
        }

        /// <summary>
        /// Gets the value for object.
        /// This method uses reflection and maybe some boxing/unboxing
        /// So don't use this fuction if you plan to use it frecuently. Performance hit is expected.
        /// Use GetInt16, GetInt32, GetInt64, GetDouble instead. Or even better, use delegates directly
        /// </summary>
        public object Get()
        {
            if (propInfo != null)
                return propInfo.GetValue(objectVal, null);
            else if (getDoubleDelegate != null)
                return getDoubleDelegate();
            else if (getSingleDelegate != null)
                return getSingleDelegate();
            else if (getIndexedDelegate != null)
                return getIndexedDelegate(index);
            else if (getInt32Delegate != null)
                return getInt32Delegate();
            else if (getInt16Delegate != null)
                return getInt16Delegate();
            else if (getInt64Delegate != null)
                return getInt64Delegate();
            else if (getBoolDelegate != null)
                return getBoolDelegate();
            else if (getStringDelegate != null)
                return getStringDelegate();
            else
                throw new Exception("This property doesn't return a value.");
        }

        /// <summary>
        /// Gets the int16 value for object.
        /// </summary>
        public Int16 GetInt16()
        {
            if (getInt16Delegate != null)
                return getInt16Delegate();
            else
                throw new Exception("This property doesn't return Int16.");
        }

        /// <summary>
        /// Gets the int32 value for object.
        /// </summary>
        public Int32 GetInt32()
        {
            if (getInt32Delegate != null)
                return getInt32Delegate();
            else
                throw new Exception("This property doesn't return Int32.");
        }

        /// <summary>
        /// Gets the int64 value for object.
        /// </summary>
        public Int64 GetInt64()
        {
            if (getInt64Delegate != null)
                return getInt64Delegate();
            else
                throw new Exception("This property doesn't return Int64.");
        }

        /// <summary>
        /// Gets the float value for object.
        /// </summary>
        public float GetSingle()
        {
            if (getSingleDelegate != null)
                return getSingleDelegate();
            else if (getInt32Delegate != null)
                return getInt32Delegate();
            else if (getInt16Delegate != null)
                return getInt16Delegate();
            else if (getInt64Delegate != null)
                return getInt64Delegate();
            else
                throw new Exception("This property doesn't return Float.");
        }

        /// <summary>
        /// Gets the Double value for object.
        /// </summary>
        public Double GetDouble()
        {
            if (getDoubleDelegate != null)
                return getDoubleDelegate();
            if (getSingleDelegate != null)
                return getSingleDelegate();
            else if (getIndexedDelegate != null)
                return getIndexedDelegate(index);
            else if (getInt32Delegate != null)
                return getInt32Delegate();
            else if (getInt16Delegate != null)
                return getInt16Delegate();
            else if (getInt64Delegate != null)
                return getInt64Delegate();
            else
                throw new Exception("This property doesn't return Double.");
        }

        /// <summary>
        /// Gets the Bool value for object.
        /// </summary>
        public bool GetBool()
        {
            if (getBoolDelegate != null)
                return getBoolDelegate();
            else
                throw new Exception("This property doesn't return Bool.");
        }

        /// <summary>
        /// Gets the String value for object.
        /// </summary>
        public string GetString()
        {
            if (getStringDelegate != null)
                return getStringDelegate();
            else
                throw new Exception("This property doesn't return String.");
        }

        /// <summary>
        /// Sets the value for this object.
        /// </summary>
        public bool Set(object param)
        {
            if (propInfo != null)
            {
                propInfo.SetValue(objectVal, param, null);
                return true;
            }
            else if (setIndexedDelegate != null)
            {
                setIndexedDelegate(index, (double)param);
                return true;
            }
            return false;
        }

        public System.Type PropertyType
        {
            get { return propInfo.PropertyType; }
        }

        /// <summary>
        /// Get the node's simple name.
        /// </summary>
        public string ShortName
        {
            get { return name; }
            set { name = value; }
        }

        /// <summary>
        /// Get the node's simple (XML) name.
        /// </summary>
        public string GetName() { return name; }

        /// <summary>
        /// Get the name of a node without underscores, etc.
        /// </summary>
        /// <returns></returns>
        public string GetPrintableName()
        {
            string temp_string = GetName();
            int found_location;

            found_location = temp_string.LastIndexOf("/");
            if (found_location >= 0)
                temp_string = temp_string.Substring(found_location);

            temp_string.Replace('_', ' ');

            return temp_string;
        }

        /// <summary>
        /// Get the fully qualified name of a node
        /// This function is very slow, so is probably useful for debugging only.
        /// </summary>
        /// <returns></returns>
        public string GetFullyQualifiedName()
        {
            List<string> stack = new List<string>();
            stack.Add(GetDisplayName(true));
            PropertyNode tmpn = GetParent();
            bool atroot = false;
            while (!atroot)
            {
                stack.Add(tmpn.GetDisplayName(true));
                if (tmpn.GetParent() == null)
                    atroot = true;
                else
                    tmpn = tmpn.GetParent();
            }

            string fqname = "";
            for (int i = stack.Count - 1; i > 0; i--)
            {
                fqname += stack[i];
                fqname += "/";
            }
            fqname += stack[0];
            return fqname;
        }

        public string GetDisplayName(bool simplify)
        {
            string display_name = name;
            if (index != 0 || !simplify)
            {
                display_name += "[" + index + "]";
            }
            return display_name;
        }

        /// <summary>
        /// Get the qualified name of a node relative to given base path,
        ///  otherwise the fully qualified name.
        ///  This function is very slow, so is probably useful for debugging only.
        /// </summary>
        /// <param name="path">The path to strip off, if found.</param>
        /// <returns></returns>
        public string GetRelativeName(string path = "/fdm/jsbsim/")
        {
            string temp_string = GetFullyQualifiedName();
            int len = path.Length;
            if ((len > 0) && (temp_string.Substring(0, len) == path))
            {
                temp_string = temp_string.Substring(len, temp_string.Length - len);
            }
            return temp_string;
        }

        ////////////////////////////////////////////////////////////////////////
        // Convenience functions for setting property attributes.
        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Set the state of the archive attribute for a property.
        /// 
        ///  If the archive attribute is true, the property will be written
        ///  when a flight is saved; if it is false, the property will be
        ///  skipped.
        /// 
        ///  A warning message will be printed if the property does not exist.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="state">The state of the archive attribute (defaults to true).</param>
        public void SetArchivable(string name, bool state = true) { throw new NotImplementedException(); }

        /// <summary>
        /// Set the state of the read attribute for a property.
        /// 
        /// If the read attribute is true, the property value will be readable;
        /// if it is false, the property value will always be the default value
        /// for its type.
        /// 
        /// A warning message will be printed if the property does not exist.
        /// 
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="state">The state of the read attribute (defaults to true).</param>
        public void SetReadable(string name, bool state = true) { throw new NotImplementedException(); }

        /// <summary>
        /// Set the state of the write attribute for a property.
        ///
        /// If the write attribute is true, the property value may be modified
        /// (depending on how it is tied); if the write attribute is false, the
        /// property value may not be modified.
        ///
        /// A warning message will be printed if the property does not exist.
        /// 
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="state">The state of the write attribute (defaults to true).</param>
        public void SetWritable(string name, bool state = true) { throw new NotImplementedException(); }


        /// <summary>
        /// Get the delegate foa an Int16 value.
        /// </summary>
        public GetInt16ValueDelegate GetInt16Delegate
        {
            get { return getInt16Delegate; }
        }

        /// <summary>
        /// Get the delegate for an Int32 value.
        /// </summary>
        public GetInt32ValueDelegate GetInt32Delegate
        {
            get { return getInt32Delegate; }
        }

        /// <summary>
        /// Get the delegate for an Int64 value.
        /// </summary>
        public GetInt64ValueDelegate GetInt64Delegate
        {
            get { return getInt64Delegate; }
        }

        /// <summary>
        /// Get the delegate for a double value.
        /// </summary>
        public GetDoubleValueDelegate GetDoubleDelegate
        {
            get { return getDoubleDelegate; }
        }

        /// <summary>
        /// Get the delegate for a bool value.
        /// </summary>
        public GetBoolValueDelegate GetBoolDelegate
        {
            get { return getBoolDelegate; }
        }

        /// <summary>
        /// Get the delegate for a string value.
        /// </summary>
        public GetStringValueDelegate GetStringDelegate
        {
            get { return getStringDelegate; }
        }

        /// <summary>
        /// Get the delegate for getting a double value accessed by index.
        /// </summary>
        public GetIndexedPropertyDelegate GetDoubleIndexedGetDelegate
        {
            get { return getIndexedDelegate; }
        }

        /// <summary>
        /// Get the delegate for setting a double value accessed by index.
        /// </summary>
        public SetIndexedPropertyDelegate GetDoubleIndexedSetDelegate
        {
            get { return setIndexedDelegate; }
        }

        /// <summary>
        /// Event to notify that the property has changed.
        /// </summary>
        /// <value>
        /// Event to notify that the property has changed.
        /// </value>
        public event PropertyHasChangedEventHandler ValueChangedEvent
        {
            add { valueChangedEvent += value; }
            remove { valueChangedEvent -= value; }
        }

        //
        // Path information.
        //

        /// <summary>
        /// Get the path to this node from the root.
        /// </summary>
        /// <param name="simplify"></param>
        /// <returns></returns>
        public string GetPath(bool simplify = false)
        {
            List<PropertyNode> pathList = new List<PropertyNode>();
            for (PropertyNode node = this; node.parent != null; node = node.parent)
                pathList.Add(node);
            string result = "";
            for (int i = pathList.Count - 1; i >= 0; i--)
            {
                result += '/';
                result += pathList[i].GetDisplayName(simplify);
            }
            return result;
        }

        /// <summary>
        /// Get a pointer to the root node.
        /// </summary>
        /// <returns></returns>
        public PropertyNode GetRootNode()
        {
            if (parent == null)
                return this;
            else
                return parent.GetRootNode();
        }


        /// <summary>
        /// Get the node's parent.
        /// </summary>
        /// <returns></returns>
        public PropertyNode GetParent() { return parent; }

        /// <summary>
        /// Get the node's integer index.
        /// </summary>
        /// <returns></returns>
        public int GetIndex() { return index; }

        /// <summary>
        /// Get a child node by name and index.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="index"></param>
        /// <param name="create"></param>
        /// <returns></returns>
        public PropertyNode GetChild(string name, int index = 0, bool create = false)
        {
            int pos = FindChild(name, index, children);
            if (pos >= 0)
                return children[pos];
            else if (create)
            {
                PropertyNode node = new PropertyNode(name, index, this);
                children.Add(node);
                //TODO fireChildAdded(node);
                return node;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Check a single mode attribute for the property node.
        /// </summary>
        /// <param name="attr"></param>
        /// <returns></returns>
        public bool GetAttribute(Attribute attr) { return (this.attr & attr) != 0; }

        /// <summary>
        /// Set a single mode attribute for the property node.
        /// </summary>
        /// <param name="attr"></param>
        /// <param name="state"></param>
        public void SetAttribute(Attribute attr, bool state)
        {
            if (state)
                this.attr |= attr;
            else
                this.attr &= ~attr;
        }

        public override string ToString()
        {
            return GetFullyQualifiedName();
        }

        /// <summary>
        /// Protected constructor for making new nodes on demand.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="index"></param>
        /// <param name="parent"></param>
        protected PropertyNode(string name, int index, PropertyNode parent)
        {
            this.index = index;
            this.name = name;
            this.parent = parent;
            this.attr = Attribute.READ | Attribute.WRITE;
            if (!PathComponentUtis.ValidateName(name))
                throw new Exception("plain name expected instead of '" + name + '\'');
        }

        /// <summary>
        /// Fire a child-removed event to all listeners. 
        /// </summary>
        protected void FireHasChangedEvent()
        {
            if (valueChangedEvent != null)
            {
                valueChangedEvent(this, EventArgs.Empty);
            }
        }

        // Internal function for parsing property paths. last_index provides
        // and index value for the last node name token, if supplied.
        protected static PropertyNode FindNode(PropertyNode current, List<PathComponent> components,
                                               int position, bool create)
        {
            // Run off the end of the list
            if (current == null)
            {
                return null;
            }

            // Success! This is the one we want.
            else if (position >= components.Count)
            {
                return (current.GetAttribute(PropertyNode.Attribute.REMOVED) ? null : current);
            }

            // Empty component means root.
            else if (components[position].name == "")
            {
                return FindNode(current.GetRootNode(), components, position + 1, create);
            }

            // . means current directory
            else if (components[position].name == ".")
            {
                return FindNode(current, components, position + 1, create);
            }

            // .. means parent directory
            else if (components[position].name == "..")
            {
                PropertyNode parent = current.GetParent();
                if (parent == null)
                    throw new Exception("Attempt to move past root with '..'");
                else
                    return FindNode(parent, components, position + 1, create);
            }

            // Otherwise, a child name
            else
            {
                PropertyNode child = current.GetChild(components[position].name,
                                                     components[position].index,
                                                     create);
                return FindNode(child, components, position + 1, create);
            }
        }

        private static int FindChild(string name, int index, List<PropertyNode> nodes)
        {
            int nNodes = nodes.Count;
            for (int i = 0; i < nNodes; i++)
            {
                PropertyNode node = nodes[i];
                if (node.GetIndex() == index && node.GetName() == name)
                    return i;
            }
            return -1;
        }

        protected object objectVal;
        protected PropertyInfo propInfo;
        protected string name;
        protected int index;
        protected PropertyNode parent;
        protected List<PropertyNode> children = new List<PropertyNode>();
        protected bool _tied = false;
        protected Attribute attr = Attribute.NO_ATTR;

        protected GetInt16ValueDelegate getInt16Delegate;
        protected GetInt32ValueDelegate getInt32Delegate;
        protected GetInt64ValueDelegate getInt64Delegate;
        protected GetSingleValueDelegate getSingleDelegate;
        protected GetDoubleValueDelegate getDoubleDelegate;
        protected GetBoolValueDelegate getBoolDelegate;
        protected GetStringValueDelegate getStringDelegate;

        protected SetInt16ValueDelegate setInt16Delegate;
        protected SetInt32ValueDelegate setInt32Delegate;
        protected SetInt64ValueDelegate setInt64Delegate;
        protected SetSingleValueDelegate setSingleDelegate;
        protected SetDoubleValueDelegate setDoubleDelegate;
        protected SetBoolValueDelegate setBoolDelegate;
        protected SetStringValueDelegate setStringDelegate;

        protected SetIndexedPropertyDelegate setIndexedDelegate;
        protected GetIndexedPropertyDelegate getIndexedDelegate;

        private event PropertyHasChangedEventHandler valueChangedEvent;

    }


    //TODO TODO. review this
    public class DoubleWrapper
    {
        double val = 0.0;

        public double GetDoubleValue()
        {
            return val;
        }

        public void SetDoubleValue(double newval)
        {
            val = newval;
        }
    }


}
