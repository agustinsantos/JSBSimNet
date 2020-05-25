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
namespace JSBSim.Format
{
    using System;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.Text.RegularExpressions;
    using System.Xml;
    using CommonUtils.MathLib;
    using log4net;

    /// <summary>
    /// Summary description for RegExprFormat.
    /// </summary>
    public class FormatHelper
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

        public static double GetAttributeValueAsNumber(XmlElement element, string attr)
        {
            string attribute = element.GetAttribute(attr);

            if (string.IsNullOrEmpty(attribute))
            {
                log.Error("Expecting numeric attribute value, but got no data");
                throw new Exception("Expecting numeric attribute value, but got no data");
            }
            else
            {
                double number = 0;
                if (!double.TryParse(attribute.Trim(), out number))
                {
                    log.Error("Expecting numeric attribute value, but got: " + attribute);
                    throw new Exception("Expecting numeric attribute value, but got: " + attribute);
                }

                return number;
            }
        }

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
            //string supplied_units = "";

            //if (element != null)
            //{
            //    returnValue = double.Parse(element.InnerText, FormatHelper.numberFormatInfo);
            //    supplied_units = element.GetAttribute("unit");
            //    if (supplied_units != null && supplied_units.Length != 0)
            //    {
            //        returnValue *= convert[supplied_units, target_units];
            //    }
            //}
            //if (element == null)
            //{
            //    log.Error("Attempting to get non-existent element " + el);
            //    throw new Exception("Attempting to get non-existent element " + el);
            //}


            string supplied_units = element.GetAttribute("unit");

            if (!string.IsNullOrEmpty(supplied_units))
            {
                if (!convert.Contains(supplied_units))
                {
                    log.Error("Supplied unit: \"" + supplied_units + "\" does not exist (typo?).");
                    throw new Exception("Supplied unit: \"" + supplied_units + "\" does not exist (typo?).");
                }
                if (!convert.Contains(supplied_units, target_units))
                {
                    log.Error("Supplied unit: \"" + supplied_units + "\" cannot be converted to " + target_units);
                    throw new Exception("Supplied unit: \"" + supplied_units + "\" cannot be converted to " + target_units);
                }
            }

            double value = double.Parse(element.InnerText, FormatHelper.numberFormatInfo);

            // Sanity check for angular values
            if ((supplied_units == "RAD") && (Math.Abs(value) > 2 * Math.PI))
            {
                log.Error(element.Name + " value " + value + " RAD is outside the range [ -2*PI RAD ; +2*PI RAD ]");
            }
            if ((supplied_units == "DEG") && (Math.Abs(value) > 360.0))
            {
                log.Error(element.Name + " value " + value + " DEG is outside the range [ -360 DEG ; +360 DEG ]");
            }


            if (!string.IsNullOrEmpty(supplied_units))
            {
                value *= convert[supplied_units, target_units];
            }

            if ((target_units == "RAD") && (Math.Abs(value) > 2 * Math.PI))
            {
                log.Error(element.Name + " value " + value + " RAD is outside the range [ -2*M_PI RAD ; +2*M_PI RAD ]");
            }
            if ((target_units == "DEG") && (Math.Abs(value) > 360.0))
            {
                log.Error(element.Name + " value " + value + " DEG is outside the range [ -360 DEG ; +360 DEG ]");
            }

            value = DisperseValue(element, value, supplied_units, target_units);

            return value;
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
        public static double DisperseValue(XmlElement e, double val, string supplied_units,
                                string target_units)
        {
            double value = val;

            bool disperse = false;
            string num = Environment.GetEnvironmentVariable("JSBSIM_DISPERSE");
            if (!string.IsNullOrEmpty(num))
            {
                disperse = num.Trim() == "1";  // set dispersions
            }
            else
            {                   // if error set to false
                disperse = false;
                log.Error("Could not process JSBSIM_DISPERSE environment variable: Assumed NO dispersions.");
            }

            if (e.HasAttribute("dispersion") && disperse)
            {
                double disp = GetAttributeValueAsNumber(e, "dispersion");
                if (!String.IsNullOrEmpty(supplied_units)) disp *= convert[supplied_units, target_units];
                string attType = e.GetAttribute("type");
                if (attType == "gaussian" || attType == "gaussiansigned")
                {
                    double grn = MathExt.GaussianRandomNumber();
                    if (attType == "gaussian")
                    {
                        value = val + disp * grn;
                    }
                    else
                    { // Assume gaussiansigned
                        value = (val + disp * grn) * (Math.Abs(grn) / grn);
                    }
                }
                else if (attType == "uniform" || attType == "uniformsigned")
                {
                    double urn = MathExt.Rand();
                    if (attType == "uniform")
                    {
                        value = val + disp * urn;
                    }
                    else
                    { // Assume uniformsigned
                        value = (val + disp * urn) * (Math.Abs(urn) / urn);
                    }
                }
                else
                {
                    log.Error("Unknown dispersion type" + attType);
                    throw new Exception("Unknown dispersion type" + attType);
                }

            }
            return value;
        }

        private static readonly MapConverter convert = new MapConverter();

        // Creates and initializes a NumberFormatInfo associated with the en-US culture.
        public static readonly NumberFormatInfo numberFormatInfo = new CultureInfo("en-US", false).NumberFormat;


        public static readonly Regex tag2valuesRegex = new Regex(tag2valuesStr, RegexOptions.Compiled);
        public static readonly Regex tagvalueRegex = new Regex(tagvalueStr, RegexOptions.Compiled);
        public static readonly Regex vectorRegex = new Regex(vectorStr, RegexOptions.Compiled);

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
            // Length
            map.Add("M", id++);
            map.Add("KM", id++);
            map.Add("FT", id++);
            map.Add("IN", id++);
            map.Add("CM", id++);
            // Area
            map.Add("M2", id++);
            map.Add("CM2", id++);
            map.Add("IN2", id++);
            map.Add("FT2", id++);
            // Volume
            map.Add("IN3", id++);
            map.Add("CC", id++);
            map.Add("M3", id++);
            map.Add("FT3", id++);
            map.Add("LTR", id++);
            map.Add("GAL", id++);
            // Mass & Weight
            map.Add("KG", id++);
            map.Add("LBS", id++);
            map.Add("SLUG", id++);
            // Moments of Inertia
            map.Add("KG*M2", id++);
            map.Add("SLUG*FT2", id++);
            // Angles
            map.Add("RAD", id++);
            map.Add("DEG", id++);
            // Angular rates
            map.Add("DEG/SEC", id++);
            map.Add("RAD/SEC", id++);
            // Spring force
            map.Add("LBS/FT", id++);
            map.Add("N/M", id++);
            // Damping force
            map.Add("LBS/FT/SEC", id++);
            map.Add("N/M/SEC", id++);
            // Damping force (Square law)
            map.Add("LBS/FT2/SEC2", id++);
            map.Add("N/M2/SEC2", id++);
            // Power
            map.Add("HP", id++);
            map.Add("WATTS", id++);
            // Force
            map.Add("N", id++);
            // Velocity
            map.Add("FT/SEC", id++);
            map.Add("FT/S", id++);
            map.Add("KTS", id++);
            map.Add("M/S", id++);
            map.Add("M/SEC", id++);
            map.Add("KM/SEC", id++);
            map.Add("KM/S", id++);
            // Torque
            map.Add("FT*LBS", id++);
            map.Add("N*M", id++);
            // Valve
            map.Add("M4*SEC/KG", id++);
            map.Add("FT4*SEC/SLUG", id++);
            // Pressure
            map.Add("PSI", id++);
            map.Add("PSF", id++);
            map.Add("INHG", id++);
            map.Add("ATM", id++);
            map.Add("PA", id++);
            map.Add("N/M2", id++);
            map.Add("LBS/FT2", id++);
            // Mass flow
            map.Add("LBS/SEC", id++);
            map.Add("KG/MIN", id++);
            map.Add("LBS/MIN", id++);
            map.Add("N/SEC", id++);
            // Fuel Consumption
            map.Add("LBS/HP*HR", id++);
            map.Add("KG/KW*HR", id++);
            // Density
            map.Add("KG/L", id++);
            map.Add("LBS/GAL", id++);


            convert = new double[map.Count, map.Count];
            for (int i = 0; i < map.Count; i++)
                for (int j = 0; j < map.Count; j++)
                    convert[i, j] = double.NaN;

            // this ["from"]["to"] = factor, so: from * factor = to
            // Length
            this["M", "FT"] = Constants.METER_TO_FEET;
            this["FT", "M"] = Constants.FEET_TO_METER;
            this["CM", "FT"] = 0.032808399;
            this["FT", "CM"] = 1.0 / this["CM", "FT"];
            this["KM", "FT"] = 3280.8399;
            this["FT", "KM"] = 1.0 / this["KM", "FT"];
            this["FT", "IN"] = 12.0;
            this["IN", "FT"] = 1.0 / this["FT", "IN"];
            this["IN", "M"] = this["IN", "FT"] * this["FT", "M"];
            this["M", "IN"] = this["M", "FT"] * this["FT", "IN"];
            // Area
            this["M2", "FT2"] = Constants.METER_TO_FEET * Constants.METER_TO_FEET;
            this["FT2", "M2"] = 1.0 / this["M2", "FT2"];
            this["CM2", "FT2"] = this["CM", "FT"] * this["CM", "FT"];
            this["FT2", "CM2"] = 1.0 / this["CM2", "FT2"];
            this["M2", "IN2"] = this["M", "IN"] * this["M", "IN"];
            this["IN2", "M2"] = 1.0 / this["M2", "IN2"];
            this["FT2", "IN2"] = 144.0;
            this["IN2", "FT2"] = 1.0 / this["FT2", "IN2"];
            // Volume
            this["IN3", "CC"] = 16.387064;
            this["CC", "IN3"] = 1.0 / this["IN3", "CC"];
            this["FT3", "IN3"] = 1728.0;
            this["IN3", "FT3"] = 1.0 / this["FT3", "IN3"];
            this["M3", "FT3"] = 35.3146667;
            this["FT3", "M3"] = 1.0 / this["M3", "FT3"];
            this["LTR", "IN3"] = 61.0237441;
            this["IN3", "LTR"] = 1.0 / this["LTR", "IN3"];
            this["GAL", "FT3"] = 0.133681;
            this["FT3", "GAL"] = 1.0 / this["GAL", "FT3"];
            this["IN3", "GAL"] = this["IN3", "FT3"] * this["FT3", "GAL"];
            this["LTR", "GAL"] = this["LTR", "IN3"] * this["IN3", "GAL"];
            this["M3", "GAL"] = 1000.0 * this["LTR", "GAL"];
            this["CC", "GAL"] = this["CC", "IN3"] * this["IN3", "GAL"];
            // Mass & Weight
            this["LBS", "KG"] = 0.45359237;
            this["KG", "LBS"] = 1.0 / this["LBS", "KG"];
            this["SLUG", "KG"] = 14.59390;
            this["KG", "SLUG"] = 1.0 / this["SLUG", "KG"];
            // Moments of Inertia
            this["SLUG*FT2", "KG*M2"] = 1.35694;
            this["KG*M2", "SLUG*FT2"] = 1.0 / this["SLUG*FT2", "KG*M2"];
            // Angles
            this["RAD", "DEG"] = Constants.radtodeg;
            this["DEG", "RAD"] = Constants.degtorad;
            // Angular rates
            this["RAD/SEC", "DEG/SEC"] = this["RAD", "DEG"];
            this["DEG/SEC", "RAD/SEC"] = 1.0 / this["RAD/SEC", "DEG/SEC"];
            // Spring force
            this["LBS/FT", "N/M"] = 14.5939;
            this["N/M", "LBS/FT"] = 1.0 / this["LBS/FT", "N/M"];
            // Damping force
            this["LBS/FT/SEC", "N/M/SEC"] = 14.5939;
            this["N/M/SEC", "LBS/FT/SEC"] = 1.0 / this["LBS/FT/SEC", "N/M/SEC"];
            // Damping force (Square Law)
            this["LBS/FT2/SEC2", "N/M2/SEC2"] = 47.880259;
            this["N/M2/SEC2", "LBS/FT2/SEC2"] = 1.0 / this["LBS/FT2/SEC2", "N/M2/SEC2"];
            // Power
            this["WATTS", "HP"] = 0.001341022;
            this["HP", "WATTS"] = 1.0 / this["WATTS", "HP"];
            // Force
            this["N", "LBS"] = 0.22482;
            this["LBS", "N"] = 1.0 / this["N", "LBS"];
            // Velocity
            this["KTS", "FT/SEC"] = 1.68781;
            this["FT/SEC", "KTS"] = 1.0 / this["KTS", "FT/SEC"];
            this["M/S", "FT/S"] = 3.2808399;
            this["M/S", "KTS"] = this["M/S", "FT/S"] / this["KTS", "FT/SEC"];
            this["M/SEC", "FT/SEC"] = 3.2808399;
            this["FT/S", "M/S"] = 1.0 / this["M/S", "FT/S"];
            this["M/SEC", "FT/SEC"] = 3.2808399;
            this["FT/SEC", "M/SEC"] = 1.0 / this["M/SEC", "FT/SEC"];
            this["KM/SEC", "FT/SEC"] = 3280.8399;
            this["FT/SEC", "KM/SEC"] = 1.0 / this["KM/SEC", "FT/SEC"];
            // Torque
            this["FT*LBS", "N*M"] = 1.35581795;
            this["N*M", "FT*LBS"] = 1 / this["FT*LBS", "N*M"];
            // Valve
            this["M4*SEC/KG", "FT4*SEC/SLUG"] = this["M", "FT"] * this["M", "FT"] *
              this["M", "FT"] * this["M", "FT"] / this["KG", "SLUG"];
            this["FT4*SEC/SLUG", "M4*SEC/KG"] =
              1.0 / this["M4*SEC/KG", "FT4*SEC/SLUG"];
            // Pressure
            this["INHG", "PSF"] = 70.7180803;
            this["PSF", "INHG"] = 1.0 / this["INHG", "PSF"];
            this["ATM", "INHG"] = 29.9246899;
            this["INHG", "ATM"] = 1.0 / this["ATM", "INHG"];
            this["PSI", "INHG"] = 2.03625437;
            this["INHG", "PSI"] = 1.0 / this["PSI", "INHG"];
            this["INHG", "PA"] = 3386.0; // inches Mercury to pascals
            this["PA", "INHG"] = 1.0 / this["INHG", "PA"];
            this["LBS/FT2", "N/M2"] = 14.5939 / this["FT", "M"];
            this["N/M2", "LBS/FT2"] = 1.0 / this["LBS/FT2", "N/M2"];
            this["LBS/FT2", "PA"] = this["LBS/FT2", "N/M2"];
            this["PA", "LBS/FT2"] = 1.0 / this["LBS/FT2", "PA"];
            // Mass flow
            this["KG/MIN", "LBS/MIN"] = this["KG", "LBS"];
            this["N/SEC", "LBS/SEC"] = 0.224808943;
            this["LBS/SEC", "N/SEC"] = 1.0 / this["N/SEC", "LBS/SEC"];
            // Fuel Consumption
            this["LBS/HP*HR", "KG/KW*HR"] = 0.6083;
            this["KG/KW*HR", "LBS/HP*HR"] = 1.0 / this["LBS/HP*HR", "KG/KW*HR"];
            // Density
            this["KG/L", "LBS/GAL"] = 8.3454045;
            this["LBS/GAL", "KG/L"] = 1.0 / this["KG/L", "LBS/GAL"];

            for (int i = 0; i < map.Count; i++)
            {
                convert[i, i] = 1.0;
            }
        }

        public bool Contains(string unit)
        {
            return map.Contains(unit);
        }

        public bool Contains(string from, string to)
        {
            string f = from.Trim().ToUpper();
            string t = to.Trim().ToUpper();

            return map.Contains(f) && map.Contains(t) &&
                convert[(int)map[f], (int)map[t]] != double.NaN;
        }

        public double this[string from, string to]   // Indexer declaration
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
                    throw new Exception("Can't this from " + f + " to " + t + ". Unit unknown");
            }
            set
            {
                string f = from.Trim().ToUpper();
                string t = to.Trim().ToUpper();

                if (map.Contains(f) && map.Contains(t))
                    convert[(int)map[f], (int)map[t]] = value;
                else
                    throw new Exception("Can't this from " + f + " to " + t + ". Unit unknown");
            }
        }
    }
}
