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

	// Import log4net classes.
	using log4net;

	using CommonUtils.MathLib;
    using JSBSim.Format;

	/// <summary>
	/// Summary description for Electric.
	/// Models and electric motor.
	/// FGElectric models an electric motor based on the configuration file
	/// POWER_WATTS parameter.  The throttle controls motor output linearly from
	/// zero to POWER_WATTS.  This power value (converted internally to horsepower)
	/// is then used by FGPropeller to apply torque to the propeller.
	/// @author David Culp
	/// </summary>

	class Electric : Engine
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

		/// Constructor
        public Electric(FDMExecutive exec, XmlElement parent, XmlElement element, int engine_number)
            : base(exec, parent, element, engine_number)
		{
			engineType= EngineType.Electric;
			dt = exec.State.DeltaTime;

            XmlElement powerElem = element.GetElementsByTagName("power")[0] as XmlElement;
            if (powerElem != null)
                PowerWatts = FormatHelper.ValueAsNumberConvertTo(powerElem, "WATTS");
		}

		public override double Calculate()
		{
			throttle = FDMExec.FlightControlSystem.GetThrottlePos(engineNumber);

			RPM = thruster.GetRPM() * thruster.GetGearRatio();

			HP = PowerWatts * throttle / hptowatts;

			PowerAvailable = (HP * Constants.hptoftlbssec) - thruster.GetPowerRequired();

			return thrust = thruster.Calculate(PowerAvailable);
		}

		public override double GetPowerAvailable() {return PowerAvailable;}
		public override double CalcFuelNeed()	{ return 0;	}

		public double GetRPM() {return RPM;}
		public override string GetEngineLabels(string delimeter)
		{
			return ""; // currently no labels are returned for this engine
		}

        public override string GetEngineValues(string format, IFormatProvider provider, string delimeter)
		{
			return ""; // currently no labels are returned for this engine
		}

		private double BrakeHorsePower;
		private double PowerAvailable;

		// timestep
		private double dt;

		// constants
        private double hptowatts = 745.7;

        private double PowerWatts = 745.7;  // maximum engine power
		private double RPM;                 // revolutions per minute
		private double HP;
	}
}
