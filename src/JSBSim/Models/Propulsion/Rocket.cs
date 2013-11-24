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
	using System.Text.RegularExpressions;

	// Import log4net classes.
	using log4net;

	using CommonUtils.MathLib;
	using JSBSim.Format;

	/// <summary>
	///  Models a generic rocket engine.
	///  The rocket engine is modeled given the following parameters:
	///  <ul>
	///  <li>Chamber pressure (in psf)</li>
	///  <li>Specific heat ratio (usually about 1.2 for hydrocarbon fuel and LOX)</li>
	///  <li>Propulsive efficiency (in percent, from 0 to 1.0)</li>
	///  <li>Variance (in percent, from 0 to 1.0, nominally 0.05)</li>
	///  </ul>
	///  Additionally, the following control inputs, operating characteristics, and
	///  location are required, as with all other engine types:
	///  <ul>
	///  <li>Throttle setting (in percent, from 0 to 1.0)</li>
	///  <li>Maximum allowable throttle setting</li>
	///  <li>Minimum working throttle setting</li>
	///  <li>Sea level fuel flow at maximum thrust</li>
	///  <li>Sea level oxidizer flow at maximum thrust</li>
	///  <li>X, Y, Z location in structural coordinate frame</li>
	///  <li>Pitch and Yaw</li>
	///  </ul>
	///  The nozzle exit pressure (p2) is returned via a
	///  call to FGNozzle::GetPowerRequired(). This exit pressure is used,
	///  along with chamber pressure and specific heat ratio, to get the
	///  thrust coefficient for the throttle setting. This thrust
	///  coefficient is multiplied by the chamber pressure and then passed
	///  to the nozzle Calculate() routine, where the thrust force is
	///  determined.
	///  
	///  @author Jon S. Berndt
	/// </summary>
	public class Rocket : Engine
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

        public Rocket(FDMExecutive exec, XmlElement parent, XmlElement element, int engine_number)
            : base(exec, parent, element, engine_number)
        {
            string token;
            XmlElement tmpElem;
            foreach (XmlNode currentNode in element.ChildNodes)
            {
                if (currentNode.NodeType == XmlNodeType.Element)
                {
                    tmpElem = currentNode as XmlElement;
                    token = tmpElem.LocalName;

                    if (token.Equals("shr"))
                        SHR = FormatHelper.ValueAsNumber(tmpElem);
                    else if (token.Equals("max_pc"))
                        maxPC = FormatHelper.ValueAsNumberConvertTo(tmpElem, "PSF");
                    else if (token.Equals("prop_eff"))
                        propEff = FormatHelper.ValueAsNumber(tmpElem);
                    else if (token.Equals("maxthrottle"))
                        maxThrottle = FormatHelper.ValueAsNumber(tmpElem);
                    else if (token.Equals("minthrottle"))
                        minThrottle = FormatHelper.ValueAsNumber(tmpElem);
                    else if (token.Equals("slfuelflowmax"))
                        slFuelFlowMax = FormatHelper.ValueAsNumberConvertTo(tmpElem, "LBS/SEC");
                    else if (token.Equals("sloxiflowmax"))
                        slOxiFlowMax = FormatHelper.ValueAsNumberConvertTo(tmpElem, "LBS/SEC");
                    else if (token.Equals("variance"))
                        variance = FormatHelper.ValueAsNumber(tmpElem);
                }
            }

            engineType = EngineType.Rocket;
            Flameout = false;

            PC = 0.0;
            kFactor = (2.0 * SHR * SHR / (SHR - 1.0)) * Math.Pow(2.0 / (SHR + 1), (SHR + 1) / (SHR - 1));
        }

		/// <summary>
		/// Determines the thrust coefficient.
		/// </summary>
		/// <returns>thrust coefficient times chamber pressure</returns>
		public override double Calculate()
		{
			double Cf=0;

			if (!Flameout && !starved) ConsumeFuel();

			throttle = this.FDMExec.FlightControlSystem.GetThrottlePos(engineNumber);

			if (throttle < minThrottle || starved) 
			{
				pctPower = thrust = 0.0; // desired thrust
				Flameout = true;
				PC = 0.0;
			} 
			else 
			{
				pctPower = throttle / maxThrottle;
				PC = maxPC*pctPower * (1.0 + variance * (rand.NextDouble() - 0.5));
				// The Cf (below) is CF from Eqn. 3-30, "Rocket Propulsion Elements", Fifth Edition,
				// George P. Sutton. Note that the thruster function GetPowerRequired() might
				// be better called GetResistance() or something; this function returns the
				// nozzle exit pressure.
				Cf = Math.Sqrt(kFactor*(1 - Math.Pow(thruster.GetPowerRequired()/(PC), (SHR-1)/SHR)));
				Flameout = false;
			}

			return thrust = thruster.Calculate(Cf*maxPC*pctPower*propEff);
		}

		/// <summary>
		/// Gets the chamber pressure.
		/// </summary>
		/// <returns>mber pressure in psf. </returns>
		public double GetChamberPressure() {return PC;}

		/// <summary>
		/// Gets the flame-out status.
		/// The engine will "flame out" if the throttle is set below the minimum
		/// sustainable setting.
		/// </summary>
		/// <returns>true if engine has flamed out. </returns>
		public bool GetFlameout() {return Flameout;}
		public override string GetEngineLabels(string delimeter)
		{
			string buf;

			buf = Name + "_ChamberPress[" + engineNumber + "]" + delimeter
				+ thruster.GetThrusterLabels(engineNumber, delimeter);

			return buf;
		}

        public override string GetEngineValues(string format, IFormatProvider provider, string delimeter)
		{
			string buf;

            buf = PC.ToString(format, provider) + delimeter + thruster.GetThrusterValues(engineNumber, delimeter);

			return buf;
		}


        private double SHR;
		private double maxPC;
		private double propEff;
		private double kFactor;
		private double variance;
		private double PC;
		private bool Flameout;
		private Random rand = new Random();

		private const string IdSrc = "$Id: FGRocket.cpp,v 1.47 2004/12/05 04:06:57 dpculp Exp $";

	}
}
