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

namespace JSBSim.Models.Propulsion
{
	using System;
	using System.Xml;
	using System.IO;
	using System.Text;
	using System.Text.RegularExpressions;

	using CommonUtils.MathLib;
	using JSBSim.Format;

	// Import log4net classes.
	using log4net;

	/// <summary>
	/// Models a fuel tank.
	/// @author Jon Berndt
	/// @see Akbar, Raza et al. "A Simple Analysis of Fuel Addition to the CWT of
	/// 747", California Institute of Technology, 1998
	/// <P>
	/// Fuel temperature is calculated using the following assumptions:
	/// <P>
	/// Fuel temperature will only be calculated for tanks which have an initial fuel
	/// temperature specified in the configuration file.
	/// <P>
	/// The surface area of the tank is estimated from the capacity in pounds.  It
	/// is assumed that the tank is a wing tank with dimensions h by 4h by 10h. The
	/// volume of the tank is then 40(h)(h)(h). The area of the upper or lower 
	/// surface is then 40(h)(h).  The volume is also equal to the capacity divided
	/// by 49.368 lbs/cu-ft, for jet fuel.  The surface area of one side can then be
	/// derived from the tank's capacity.  
	/// <P>
	/// The heat capacity of jet fuel is assumed to be 900 Joules/lbm/K, and the 
	/// heat transfer factor of the tank is 1.115 Watts/sq-ft/K.
	/// <P>
	/// Configuration File Format
	/// <pre>
	/// \<AC_TANK TYPE="\<FUEL | OXIDIZER>" NUMBER="\<n>">
	/// XLOC        \<x location>
	/// YLOC        \<y location>
	/// ZLOC        \<z location>
	/// RADIUS      \<radius>
	/// CAPACITY    \<capacity>
	/// CONTENTS    \<contents>
	/// TEMPERATURE \<fuel temperature>
	/// \</AC_TANK>
	/// </pre>
	/// Definition of the tank configuration file parameters:
	/// <pre>
	/// <b>TYPE</b> - One of FUEL or OXIDIZER.
	/// <b>XLOC</b> - Location of tank on aircraft's x-axis, inches.
	/// <b>YLOC</b> - Location of tank on aircraft's y-axis, inches.
	/// <b>ZLOC</b> - Location of tank on aircraft's z-axis, inches.
	/// <b>RADIUS</b> - Equivalent radius of tank, inches, for modeling slosh.
	/// <b>CAPACITY</b> - Capacity in pounds.
	/// <b>CONTENTS</b> - Initial contents in pounds.
	/// <b>TEMPERATURE</b> - Initial temperature in degrees Fahrenheit.
	/// </pre>
	/// 
	/// </summary>
	public class Tank
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

		public enum EnumTankType {Unknown, Fuel, Oxidizer};

		public Tank(FDMExecutive exec, XmlElement element)
		{
            auxiliary = exec.Auxiliary;

			string type = element.GetAttribute("type");
			if      (type == "FUEL")     tankType = EnumTankType.Fuel;
			else if (type == "OXIDIZER") tankType = EnumTankType.Oxidizer;
			else                         tankType = EnumTankType.Unknown;

			foreach (XmlNode currentNode in element.ChildNodes)
			{
				if (currentNode.NodeType == XmlNodeType.Element) 
				{
					XmlElement currentElement = (XmlElement)currentNode;


					if (currentElement.LocalName.Equals("radius"))
						radius = FormatHelper.ValueAsNumberConvertTo(currentElement, "IN");
					else if (currentElement.LocalName.Equals("capacity"))
						capacity = FormatHelper.ValueAsNumberConvertTo(currentElement, "LBS");
					else if (currentElement.LocalName.Equals("contents"))
						contents = FormatHelper.ValueAsNumberConvertTo(currentElement, "LBS");
					else if (currentElement.LocalName.Equals("temperature"))
						temperature = FormatHelper.ValueAsNumberConvertTo(currentElement, "LBS");
					else if (currentElement.LocalName.Equals("location"))
					{
						vXYZ = FormatHelper.TripletConvertTo(currentElement, "IN");
					}
				}
			}

			selected = true;

			if (capacity != 0) 
			{
				pctFull = 100.0*contents/capacity;            // percent full; 0 to 100.0
			} 
			else 
			{
				contents = 0;
				pctFull  = 0;
			}

			if (temperature != -9999.0)  temperature = Conversion.FahrenheitToCelsius(temperature); 
			area = 40.0 * Math.Pow(capacity/1975, 0.666666667);

		}


        public double Drain(double used)
		{
			double shortage = Contents - used;

			if (shortage >= 0) 
			{
				Contents -= used;
				pctFull = 100.0*contents/capacity;
			} 
			else 
			{
				contents = 0.0;
				pctFull = 0.0;
				selected = false;
			}
			return shortage;
		}

		public double Calculate(double dt)
		{
			if (temperature == -9999.0) return 0.0;
			double HeatCapacity = 900.0;        // Joules/lbm/C
			double TempFlowFactor = 1.115;      // Watts/sqft/C
			double TAT = auxiliary.TAT_C;
			double Tdiff = TAT - temperature;
			double dT = 0.0;                    // Temp change due to one surface
			if (Math.Abs(Tdiff) > 0.1) 
			{
				dT = (TempFlowFactor * area * Tdiff * dt) / (Contents * HeatCapacity);
			}
			return temperature += (dT + dT);    // For now, assume upper/lower the same
		}

		public EnumTankType TankType { get {return tankType;}}
		public bool IsSelected { get {return selected;}}
		public double PctFull {get {return pctFull;}}
		
		public Vector3D GetXYZ() {return vXYZ;}
		public double GetXYZ(int idx) {return vXYZ[idx];}

		public double Fill(double amount)
		{
			double overage = 0.0;

			contents += amount;

			if (contents > capacity) 
			{
				overage = contents - capacity;
				contents = capacity;
				pctFull = 100.0;
			} 
			else 
			{
				pctFull = contents/capacity*100.0;
			}
			return overage;
		}

		public double Contents
		{
			get {return contents;}
			set 
			{
				contents = value;
				if (contents > capacity) 
				{
					contents = capacity;
					pctFull = 100.0;
				} 
				else 
				{
					pctFull = contents/capacity*100.0;
				}
			}
		}

		/// <summary>
		/// Gets/sets the temperature in Celsius
		/// </summary>
		public double TemperatureCelsius
		{
			get {return temperature;}
			set { temperature = value; }
		}

		/// <summary>
		/// Gets/sets the temperature in Fahrenheit
		/// </summary>
		public double TemperatureFahrenheit
		{
			get {return Conversion.CelsiusToFahrenheit(temperature);}
			set { temperature = Conversion.FahrenheitToCelsius(value); }
		}



		private EnumTankType tankType;
		private Vector3D vXYZ;
        private double capacity = 0.0;
        private double radius = 0.0;
		private double pctFull;
        private double contents = 0.0;
		private double area = 1.0;
        private double temperature = -9999.0;      
		private bool  selected;
		private Auxiliary auxiliary;

		private const string IdSrc = "$Id: FGTank.cpp,v 1.37 2004/06/07 13:45:08 dpculp Exp $";
	}
}
