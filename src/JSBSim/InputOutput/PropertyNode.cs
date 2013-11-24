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
	using System.Collections;
	using System.Reflection;


	/// <summary>
	/// Summary description for IProperty.
	/// </summary>
	public class PropertyNode
	{
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
		public delegate double GetDoubleValueDelegate();

        public delegate void SetInt16ValueDelegate(short val);
        public delegate void SetInt32ValueDelegate(int val);
        public delegate void SetInt64ValueDelegate(long val);
        public delegate void SetDoubleValueDelegate(double val);

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
            setInt32Delegate = setDel;
            getInt32Delegate = getDel;
        }

        public PropertyNode(string nodeName,
                GetDoubleValueDelegate getDel,
                SetDoubleValueDelegate setDel)
        {
            name = nodeName;
            setDoubleDelegate = setDel;
            getDoubleDelegate = getDel;
        }

		public PropertyNode(string nodeName, Type type, PropertyInfo prop)
		{
			name = nodeName;
			objectVal = null;
			propInfo = prop;

			if (propInfo.PropertyType == typeof(System.Int16))
				getInt16Delegate = (GetInt16ValueDelegate)Delegate.CreateDelegate(typeof(GetInt16ValueDelegate), type, propInfo.GetGetMethod().Name);
			else if (propInfo.PropertyType == typeof(System.Int32))
				getInt32Delegate = (GetInt32ValueDelegate)Delegate.CreateDelegate(typeof(GetInt32ValueDelegate), type, propInfo.GetGetMethod().Name);
			else if (propInfo.PropertyType == typeof(System.Int64))
				getInt64Delegate = (GetInt64ValueDelegate)Delegate.CreateDelegate(typeof(GetInt64ValueDelegate), type, propInfo.GetGetMethod().Name);
			else if (propInfo.PropertyType == typeof(System.Double))
				getDoubleDelegate = (GetDoubleValueDelegate)Delegate.CreateDelegate(typeof(GetDoubleValueDelegate), type, propInfo.GetGetMethod().Name);

		}
		
		public PropertyNode(string nodeName,  object val, PropertyInfo prop)
		{
			name = nodeName;
			objectVal = val;
			propInfo = prop;

            if (propInfo.PropertyType == typeof(System.Int16))
            {
                if (propInfo.GetSetMethod() != null)
                    setInt16Delegate = (SetInt16ValueDelegate)Delegate.CreateDelegate(typeof(SetInt16ValueDelegate), objectVal, propInfo.GetSetMethod().Name);
                if (propInfo.GetGetMethod() != null)
                    getInt16Delegate = (GetInt16ValueDelegate)Delegate.CreateDelegate(typeof(GetInt16ValueDelegate), objectVal, propInfo.GetGetMethod().Name);
            }
            else if (propInfo.PropertyType == typeof(System.Int32))
            {
                if (propInfo.GetSetMethod() != null)
                    setInt32Delegate = (SetInt32ValueDelegate)Delegate.CreateDelegate(typeof(SetInt32ValueDelegate), objectVal, propInfo.GetSetMethod().Name);
                if (propInfo.GetGetMethod() != null)
                    getInt32Delegate = (GetInt32ValueDelegate)Delegate.CreateDelegate(typeof(GetInt32ValueDelegate), objectVal, propInfo.GetGetMethod().Name);
            }
            else if (propInfo.PropertyType == typeof(System.Int64))
            {
                if (propInfo.GetSetMethod() != null)
                    setInt64Delegate = (SetInt64ValueDelegate)Delegate.CreateDelegate(typeof(SetInt64ValueDelegate), objectVal, propInfo.GetSetMethod().Name);
                if (propInfo.GetGetMethod() != null)
                    getInt64Delegate = (GetInt64ValueDelegate)Delegate.CreateDelegate(typeof(GetInt64ValueDelegate), objectVal, propInfo.GetGetMethod().Name);
            }
            else if (propInfo.PropertyType == typeof(System.Double))
            {
                if (propInfo.GetSetMethod() != null)
                    setDoubleDelegate = (SetDoubleValueDelegate)Delegate.CreateDelegate(typeof(SetDoubleValueDelegate), objectVal, propInfo.GetSetMethod().Name);
                if (propInfo.GetGetMethod() != null)
                    getDoubleDelegate = (GetDoubleValueDelegate)Delegate.CreateDelegate(typeof(GetDoubleValueDelegate), objectVal, propInfo.GetGetMethod().Name);
            }
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
            else if (getIndexedDelegate != null)
                return getIndexedDelegate(index);
            else if (getInt32Delegate != null)
                return (double)getInt32Delegate();
            else if (getInt16Delegate != null)
                return (double)getInt16Delegate();
            else if (getInt64Delegate != null)
                return (double)getInt64Delegate();
            else
                throw new Exception("This property doesn't return Double.");
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
		/// Gets the Double value for object.
		/// </summary>
		public Double GetDouble() 
		{
            if (getDoubleDelegate != null)
                return getDoubleDelegate();
            else if (getIndexedDelegate != null)
                return getIndexedDelegate(index);
            else if (getInt32Delegate != null)
                return (double)getInt32Delegate();
            else if (getInt16Delegate != null)
                return (double)getInt16Delegate();
            else if (getInt64Delegate != null)
                return (double)getInt64Delegate();
            else
				throw new Exception("This property doesn't return Double.");
		}


		/// <summary>
		/// Sets the value for this object.
		/// </summary>
		public void Set(object param) 
		{
            if (propInfo != null)
                propInfo.SetValue(objectVal, param, null);
            else if (setIndexedDelegate != null)
                setIndexedDelegate(index, (double) param);
		}

		public System.Type PropertyType 
		{
			get { return propInfo.PropertyType;}
		}

		/// <summary>
		/// Get the node's simple name.
		/// </summary>
		public string ShortName 
		{
			get { return name;}
			set { name = value;}
		}

		/// <summary>
		/// Get the delegate foa an Int16 value.
		/// </summary>
		public GetInt16ValueDelegate GetInt16Delegate 
		{
			get { return getInt16Delegate;}
		}

		/// <summary>
		/// Get the delegate for an Int32 value.
		/// </summary>
		public GetInt32ValueDelegate GetInt32Delegate 
		{
			get { return getInt32Delegate;}
		}

		/// <summary>
		/// Get the delegate for an Int64 value.
		/// </summary>
		public GetInt64ValueDelegate GetInt64Delegate 
		{
			get { return getInt64Delegate;}
		}

		/// <summary>
		/// Get the delegate for a double value.
		/// </summary>
		public GetDoubleValueDelegate GetDoubleDelegate 
		{
			get { return getDoubleDelegate;}
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

		protected object objectVal;
		protected PropertyInfo propInfo;
		protected string name;
        protected int index;
		
		protected readonly GetInt16ValueDelegate getInt16Delegate;
		protected readonly GetInt32ValueDelegate getInt32Delegate;
		protected readonly GetInt64ValueDelegate getInt64Delegate;
		protected readonly GetDoubleValueDelegate getDoubleDelegate;

        protected readonly SetInt16ValueDelegate setInt16Delegate;
        protected readonly SetInt32ValueDelegate setInt32Delegate;
        protected readonly SetInt64ValueDelegate setInt64Delegate;
        protected readonly SetDoubleValueDelegate setDoubleDelegate;

        protected readonly SetIndexedPropertyDelegate setIndexedDelegate;
        protected readonly GetIndexedPropertyDelegate getIndexedDelegate;

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
