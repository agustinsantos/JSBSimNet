#region Copyright(C)  Licensed under GNU GPL.
/// Copyright (C) 2005-2006 Agustin Santos Mendez
/// 
/// JSBSim was developed by Jon S. Berndt, Tony Peden, and
/// David Megginson. 
/// Agustin Santos Mendez implemented and maintains this C# version.
/// 
/// This program is free software; you can redistribute it and/or
///  modify it under the terms of the GNU General Public License
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
namespace JSBSim.Format
{
	using System;
	using System.Text.RegularExpressions;
	using System.Xml;
	using System.Globalization;
	using System.Collections;
	using System.Collections.Specialized;

	using CommonUtils.MathLib;

	/// <summary>
	/// Summary description for RegExprFormat.
	/// </summary>
	public class FormatHelper
	{

        public static double ValueAsNumberConvertTo(XmlElement element, string target_units, string supplied_units)
        {
            double returnValue = double.PositiveInfinity;
            if (element != null)
            {
                returnValue = double.Parse(element.InnerText, FormatHelper.numberFormatInfo);
                if (supplied_units != null && supplied_units.Length != 0)
                {
                    returnValue *= convert[supplied_units, target_units];
                }
            }
            return returnValue;
        }

		public static double ValueAsNumberConvertTo(XmlElement element, string target_units)
		{
			double returnValue = double.PositiveInfinity;
			string supplied_units="";

			if (element != null) 
			{
				returnValue = double.Parse(element.InnerText, FormatHelper.numberFormatInfo);
				supplied_units = element.GetAttribute("unit");
				if (supplied_units != null && supplied_units.Length != 0) 
				{
					returnValue *= convert[supplied_units, target_units];
				}
			} 
			return returnValue;
		}

		public static double ValueAsNumber(XmlElement element)
		{
			double returnValue = double.PositiveInfinity;

			if (element != null) 
			{
				returnValue = double.Parse(element.InnerText, FormatHelper.numberFormatInfo);
			} 
			return returnValue;
		}

		public static Vector3D TripletConvertTo(XmlElement element, string target_units)
		{
			Vector3D triplet = new Vector3D(0.0, 0.0, 0.0);
			string supplied_units = element.GetAttribute("unit");

			foreach (XmlNode currentNode in element.ChildNodes)
			{
				if (currentNode.NodeType == XmlNodeType.Element) 
				{
					XmlElement currentElement = (XmlElement)currentNode;

					if (currentElement.LocalName.Equals("x") || currentElement.LocalName.Equals("roll"))
					{
						triplet.X = double.Parse(currentElement.InnerText, FormatHelper.numberFormatInfo);
						if (supplied_units != null && supplied_units.Length != 0) 
						{
							triplet.X *= convert[supplied_units, target_units];
						}
					}
					 else if (currentElement.LocalName.Equals("y") || currentElement.LocalName.Equals("pitch"))
					{
						triplet.Y = double.Parse(currentElement.InnerText, FormatHelper.numberFormatInfo);
						if (supplied_units != null && supplied_units.Length != 0) 
						{
							triplet.Y *= convert[supplied_units, target_units];
						}
					}
					else if (currentElement.LocalName.Equals("z") || currentElement.LocalName.Equals("yaw"))
					{
						triplet.Z = double.Parse(currentElement.InnerText, FormatHelper.numberFormatInfo);
						if (supplied_units != null && supplied_units.Length != 0) 
						{
							triplet.Z *= convert[supplied_units, target_units];
						}
					}

				}
			}
			return triplet;
		}
		
		private static MapConverter convert = new MapConverter();

		// Creates and initializes a NumberFormatInfo associated with the en-US culture.
		public static readonly NumberFormatInfo numberFormatInfo = new CultureInfo( "en-US", false ).NumberFormat;
		
		
		public static readonly Regex tag2valuesRegex = new Regex(tag2valuesStr, RegexOptions.Compiled);
		public static readonly Regex tagvalueRegex   = new Regex(tagvalueStr, RegexOptions.Compiled);
		public static readonly Regex vectorRegex     = new Regex(vectorStr, RegexOptions.Compiled);
		
		//AC_GEAR RIGHT_MLG 648.0  100.0  -84.0  120000.0 10000.0  0.5  0.8  0.02 FIXED     RIGHT 0 RETRACT

		private const string tag2valuesStr = @"([\s ])*(?<tag>[A-Z_]+)([\s ]+)(?<min>[0-9+-.]*)([\s ]+)(?<max>[0-9+-.]*)([\s ]+)";
		private const string tagvalueStr = "(?<tag>[A-Z_]+)( +)(?<value>[0-9+-. ]*)";
		private const string vectorStr = "( *)(?<x>[0-9+-.]*)( +)(?<y>[0-9+-.]*)( +)(?<z>[0-9+-.]*)( *)";

        internal const string propertyStr = @"([-]?[a-zA-Z/_][a-zA-Z0-9/_-]+)";
        internal const string conditionStr = @"(EQ|NE|GT|GE|LT|LE|eq|ne|gt|ge|lt|le|==|!=|>|>=|<|<=)";
        internal const string valueStr = @"(([+]|[-]|[.]|[-.]|[0-9])[0-9]*([.][0-9]*)?([e|E][-|+][0-9]*)?)";
        internal const string subsequentSpaces = "( +)";

	}

	public class MapConverter
	{
		private static double[,] convert;
		private static ListDictionary map = new ListDictionary();

		public MapConverter()
		{
			int id = 0;
			map.Add("M",   id++);
			map.Add("M2",  id++);
			map.Add("FT",  id++);
			map.Add("FT2", id++);
			map.Add("IN",  id++);
			map.Add("LBS", id++);
			map.Add("KG",  id++);
			map.Add("SLUG*FT2", id++);
			map.Add("KG*M2", id++);
			map.Add("RAD",  id++);
			map.Add("DEG",  id++);
			map.Add("LBS/FT",  id++);
			map.Add("LBS/FT/SEC",  id++);

			convert = new double[map.Count, map.Count];
			// convert ["from"]["to"] = factor, so: from * factor = to
			this["M","FT"] = Constants.METER_TO_FEET;
			this["FT","M"] = Constants.FEET_TO_METER;
			
			this["M2","FT2"] = Constants.METER_TO_FEET*Constants.METER_TO_FEET;
			this["FT2","M2"] = 1.0/this["M2","FT2"];
			
			this["FT","IN"] = 12.0;
			this["IN","FT"] = 1.0/this["FT","IN"];
			
			this["LBS","KG"] = 0.45359237;
			this["KG","LBS"] = 1.0/this["LBS","KG"];
			
			this["SLUG*FT2","KG*M2"] = 1.35694;
			this["KG*M2","SLUG*FT2"] = 1.0/this["SLUG*FT2","KG*M2"];
			
			this["RAD","DEG"] = Constants.radtodeg;
			this["DEG","RAD"] = Constants.degtorad;

			for (int i = 0; i < map.Count; i++)
			{
				convert[i,i] = 1.0;
			}
		}

		public double this [string from, string to]   // Indexer declaration
		{
			get
			{
				string f = from.Trim().ToUpper();
				string t = to.Trim().ToUpper();

				if (f.Equals(t))
					return 1.0;
				else if (map.Contains(f) && map.Contains(t))
					return convert[(int)map[f], (int)map[t]];
				else 
					throw new Exception("Can't convert from "+ f + " to " + t + ". Unit unknown");
			}
			set
			{
				string f = from.Trim().ToUpper();
				string t = to.Trim().ToUpper();

				if (map.Contains(f) && map.Contains(t))
					convert[(int)map[f], (int)map[t]] = value;
				else 
					throw new Exception("Can't convert from "+ f + " to " + t + ". Unit unknown");
			}
		}
	}
}
