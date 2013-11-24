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
	using JSBSim.MathValues;
    using JSBSim.Format;

	/// <summary>
	/// Models Dave Luff's Turbo/Supercharged Piston engine model.
	/// Additional elements are required for a supercharged engine.  These can be
	/// left off a non-supercharged engine, ie. the changes are all backward
	/// compatible at present.
	/// 
	/// - NUMBOOSTSPEEDS - zero (or not present) for a naturally-aspirated engine,
	/// either 1, 2 or 3 for a boosted engine.  This corresponds to the number of
	/// supercharger speeds.  Merlin XII had 1 speed, Merlin 61 had 2, a late
	/// Griffon engine apparently had 3.  No known engine more than 3, although
	/// some German engines apparently had a continuously variable-speed
	/// supercharger.
	/// 
	/// - BOOSTOVERRIDE - whether the boost pressure control system (either a boost
	/// control valve for superchargers or wastegate for turbochargers) can be
	/// overriden by the pilot.  During wartime this was commonly possible, and
	/// known as "War Emergency Power" by the Brits.  1 or 0 in the config file.
	/// This isn't implemented in the model yet though, there would need to be
	/// some way of getting the boost control cutout lever position (on or off)
	/// from FlightGear first.
	/// 
	/// - The next items are all appended with either 1, 2 or 3 depending on which
	/// boost speed they refer to, eg RATEDBOOST1.  The rated values seems to have
	/// been a common convention at the time to express the maximum continuously
	/// available power, and the conditions to attain that power.
	/// 
	/// - RATEDBOOST[123] - the absolute rated boost above sea level ambient for a
	/// given boost speed, in psi.  Eg the Merlin XII had a rated boost of 9psi,
	/// giving approximately 42inHg manifold pressure up to the rated altitude.
	/// 
	/// - RATEDALTITUDE[123] - The altitude up to which rated boost can be
	/// maintained.  Up to this altitude the boost is maintained constant for a
	/// given throttle position by the BCV or wastegate.  Beyond this altitude the
	/// manifold pressure must drop, since the supercharger is now at maximum
	/// unregulated output.  The actual pressure multiplier of the supercharger
	/// system is calculated at initialisation from this value.
	/// 
	/// - RATEDPOWER[123] - The power developed at rated boost at rated altitude at
	/// rated rpm.
	/// 
	/// - RATEDRPM[123] - The rpm at which rated power is developed.
	/// 
	/// - TAKEOFFBOOST - Takeoff boost in psi above ambient.  Many aircraft had an
	/// extra boost setting beyond rated boost, but not totally uncontrolled as in
	/// the already mentioned boost-control-cutout, typically attained by pushing
	/// the throttle past a mechanical 'gate' preventing its inadvertant use. This
	/// was typically used for takeoff, and emergency situations, generally for
	/// not more than five minutes.  This is a change in the boost control
	/// setting, not the actual supercharger speed, and so would only give extra
	/// power below the rated altitude.  When TAKEOFFBOOST is specified in the
	/// config file (and is above RATEDBOOST1), then the throttle position is
	/// interpreted as:
	/// 
	/// - 0 to 0.95 : idle manifold pressure to rated boost (where attainable)
	/// - 0.96, 0.97, 0.98 : rated boost (where attainable).
	/// - 0.99, 1.0 : takeoff boost (where attainable).
	/// 
	/// A typical takeoff boost for an earlyish Merlin was about 12psi, compared
	/// with a rated boost of 9psi.
	/// 
	/// It is quite possible that other boost control settings could have been used
	/// on some aircraft, or that takeoff/extra boost could have activated by other
	/// means than pushing the throttle full forward through a gate, but this will
	/// suffice for now.
	/// 
	/// Note that MAXMP is still the non-boosted max manifold pressure even for
	/// boosted engines - effectively this is simply a measure of the pressure drop
	/// through the fully open throttle.
	/// 
	/// @author Jon S. Berndt (Engine framework code and framework-related mods)
	/// @author Dave Luff (engine operational code)
	/// @author David Megginson (initial porting and additional code)
	/// </summary>
	public class Piston : Engine
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

        public Piston(FDMExecutive exec, XmlElement parent, XmlElement element, int engine_number)
            : base(exec, parent, element, engine_number)
		{  

			engineType = EngineType.Piston;
            // These items are read from the configuration file

            Cycles = 2;
            IdleRPM = 600;
            Displacement = 360;
            MaxHP = 200;
            MinManifoldPressure_inHg = 6.5;
            MaxManifoldPressure_inHg = 28.5;

            // These are internal program variables

			crank_counter = 0;
			OilTemp_degK = 298;
			ManifoldPressure_inHg = FDMExec.Atmosphere.Pressure * Constants.psftoinhg; // psf to in Hg
			minMAP = 21950;
			maxMAP = 96250;
			MAP = FDMExec.Atmosphere.Pressure * 47.88;  // psf to Pa
			CylinderHeadTemp_degK = 0.0;
			Magnetos = 0;
			ExhaustGasTemp_degK = 0.0;
			EGT_degC = 0.0;

			dt = FDMExec.State.DeltaTime;

			// Supercharging
			BoostSpeeds = 0;  // Default to no supercharging
			BoostSpeed = 0;
			Boosted = false;
			BoostOverride = 0;
			bBoostOverride = false;
			bTakeoffBoost = false;
			TakeoffBoost = 0.0;   // Default to no extra takeoff-boost
			int i;
			for (i=0; i<FG_MAX_BOOST_SPEEDS; i++) 
			{
				RatedBoost[i] = 0.0;
				RatedPower[i] = 0.0;
				RatedAltitude[i] = 0.0;
				BoostMul[i] = 1.0;
				RatedMAP[i] = 100000;
				RatedRPM[i] = 2500;
				TakeoffMAP[i] = 100000;
			}
			for (i=0; i<FG_MAX_BOOST_SPEEDS-1; i++) 
			{
				BoostSwitchAltitude[i] = 0.0;
				BoostSwitchPressure[i] = 0.0;
			}

			// Initialisation
			volumetric_efficiency = 0.8;  // Actually f(speed, load) but this will get us running

           // Read inputs from engine data file where present.
            string token;
            XmlElement tmpElem;
            foreach (XmlNode currentNode in element.ChildNodes)
            {
                if (currentNode.NodeType == XmlNodeType.Element)
                {
                    tmpElem = currentNode as XmlElement;
                    token = tmpElem.LocalName;

                    if (token.Equals("minmp")) // Should have ELSE statement telling default value used?
                        MinManifoldPressure_inHg = FormatHelper.ValueAsNumberConvertTo(tmpElem, "INHG");
                    else if (token.Equals("maxmp"))
                        MaxManifoldPressure_inHg = FormatHelper.ValueAsNumberConvertTo(tmpElem, "INHG");
                    else if (token.Equals("displacement"))
                        Displacement = FormatHelper.ValueAsNumberConvertTo(tmpElem, "IN3");
                    else if (token.Equals("maxhp"))
                        MaxHP = FormatHelper.ValueAsNumberConvertTo(tmpElem, "HP");
                    else if (token.Equals("cycles"))
                        Cycles = FormatHelper.ValueAsNumber(tmpElem);
                    else if (token.Equals("idlerpm"))
                        IdleRPM = FormatHelper.ValueAsNumber(tmpElem);
                    else if (token.Equals("maxthrottle"))
                        maxThrottle = FormatHelper.ValueAsNumber(tmpElem);
                    else if (token.Equals("minthrottle"))
                        minThrottle = FormatHelper.ValueAsNumber(tmpElem);
                    else if (token.Equals("numboostspeeds"))
                    { 
                        BoostSpeeds = (int)FormatHelper.ValueAsNumber(tmpElem);
                    }
                    // Turbo- and super-charging parameters
                    else if (token.Equals("boostoverride"))
                        BoostOverride = (int)FormatHelper.ValueAsNumber(tmpElem);
                    else if (token.Equals("takeoffboost"))
                        TakeoffBoost = FormatHelper.ValueAsNumberConvertTo(tmpElem, "PSI");
                    else if (token.Equals("ratedboost1"))
                        RatedBoost[0] = FormatHelper.ValueAsNumberConvertTo(tmpElem, "PSI");
                    else if (token.Equals("ratedboost2"))
                        RatedBoost[1] = FormatHelper.ValueAsNumberConvertTo(tmpElem, "PSI");
                    else if (token.Equals("ratedboost3"))
                        RatedBoost[2] = FormatHelper.ValueAsNumberConvertTo(tmpElem, "PSI");
                    else if (token.Equals("ratedpower1"))
                        RatedPower[0] = FormatHelper.ValueAsNumberConvertTo(tmpElem, "HP");
                    else if (token.Equals("ratedpower2"))
                        RatedPower[1] = FormatHelper.ValueAsNumberConvertTo(tmpElem, "HP");
                    else if (token.Equals("ratedpower3"))
                        RatedPower[2] = FormatHelper.ValueAsNumberConvertTo(tmpElem, "HP");
                    else if (token.Equals("ratedrpm1"))
                        RatedRPM[0] = FormatHelper.ValueAsNumber(tmpElem);
                    else if (token.Equals("ratedrpm2"))
                        RatedRPM[1] = FormatHelper.ValueAsNumber(tmpElem);
                    else if (token.Equals("ratedrpm3"))
                        RatedRPM[2] = FormatHelper.ValueAsNumber(tmpElem);
                    else if (token.Equals("ratedaltitude1"))
                        RatedAltitude[0] = FormatHelper.ValueAsNumberConvertTo(tmpElem, "FT");
                    else if (token.Equals("ratedaltitude2"))
                        RatedAltitude[1] = FormatHelper.ValueAsNumberConvertTo(tmpElem, "FT");
                    else if (token.Equals("ratedaltitude3"))
                        RatedAltitude[2] = FormatHelper.ValueAsNumberConvertTo(tmpElem, "FT");

                    else
                    {
                        if (log.IsErrorEnabled)
                            log.Error("Unhandled token in Engine config file: " + token);
                    }
                }
            }
			
			minMAP = MinManifoldPressure_inHg * 3376.85;  // inHg to Pa
			maxMAP = MaxManifoldPressure_inHg * 3376.85;

			// Set up and sanity-check the turbo/supercharging configuration based on the input values.
			if(TakeoffBoost > RatedBoost[0]) bTakeoffBoost = true;
			for(i=0; i<BoostSpeeds; ++i) 
			{
				bool bad = false;
				if(RatedBoost[i] <= 0.0) bad = true;
				if(RatedPower[i] <= 0.0) bad = true;
				if(RatedAltitude[i] < 0.0) bad = true;  // 0.0 is deliberately allowed - this corresponds to unregulated supercharging.
				if(i > 0 && RatedAltitude[i] < RatedAltitude[i - 1]) bad = true;
				if(bad) 
				{
					// We can't recover from the above - don't use this supercharger speed.
					BoostSpeeds--;
					// TODO - put out a massive error message!
					break;
				}
				// Now sanity-check stuff that is recoverable.
				if(i < BoostSpeeds - 1) 
				{
					if(BoostSwitchAltitude[i] < RatedAltitude[i]) 
					{
						// TODO - put out an error message
						// But we can also make a reasonable estimate, as below.
						BoostSwitchAltitude[i] = RatedAltitude[i] + 1000;
					}
					BoostSwitchPressure[i] = FDMExec.Atmosphere.GetPressure(BoostSwitchAltitude[i]) * 47.88;
					//cout << "BoostSwitchAlt = " << BoostSwitchAltitude[i] << ", pressure = " << BoostSwitchPressure[i] << '\n';
					// Assume there is some hysteresis on the supercharger gear switch, and guess the value for now
					BoostSwitchHysteresis = 1000;
				}
				// Now work out the supercharger pressure multiplier of this speed from the rated boost and altitude.
				RatedMAP[i] = FDMExec.Atmosphere.PressureSeaLevel * 47.88 + RatedBoost[i] * 6895;  // psf*47.88 = Pa, psi*6895 = Pa.
				// Sometimes a separate BCV setting for takeoff or extra power is fitted.
				if(TakeoffBoost > RatedBoost[0]) 
				{
					// Assume that the effect on the BCV is the same whichever speed is in use.
					TakeoffMAP[i] = RatedMAP[i] + ((TakeoffBoost - RatedBoost[0]) * 6895);
					bTakeoffBoost = true;
				} 
				else 
				{
					TakeoffMAP[i] = RatedMAP[i];
					bTakeoffBoost = false;
				}
				BoostMul[i] = RatedMAP[i] / (FDMExec.Atmosphere.GetPressure(RatedAltitude[i]) * 47.88);

			}

			if(BoostSpeeds > 0) 
			{
				Boosted = true;
				BoostSpeed = 0;
			}
			bBoostOverride = (BoostOverride == 1 ? true : false);
		}


		public override string GetEngineLabels(string delimeter)
		{
			string buf;

			buf = Name + "_PwrAvail[" + engineNumber + "]" + delimeter
				+ Name + "_HP[" + engineNumber + "]" + delimeter
				+ Name + "_equiv_ratio[" + engineNumber + "]" + delimeter
				+ Name + "_MAP[" + engineNumber + "]" + delimeter
				+ thruster.GetThrusterLabels(engineNumber, delimeter);

			return buf;
		}

        public override string GetEngineValues(string format, IFormatProvider provider, string delimeter)
		{
			string buf;

            buf = PowerAvailable.ToString(format, provider) + delimeter + HP.ToString(format, provider) + delimeter
                + equivalence_ratio.ToString(format, provider) + delimeter + MAP.ToString(format, provider) + delimeter
				+ thruster.GetThrusterValues(engineNumber, delimeter);

			return buf;
		}

		public override double Calculate()
		{
			if (fuelFlow_gph > 0.0) ConsumeFuel();

			throttle = FDMExec.FlightControlSystem.GetThrottlePos(engineNumber);
			mixture = FDMExec.FlightControlSystem.GetMixturePos(engineNumber);

			//
			// Input values.
			//

			p_amb = FDMExec.Atmosphere.Pressure * 47.88;              // convert from lbs/ft2 to Pa
			p_amb_sea_level = FDMExec.Atmosphere.PressureSeaLevel * 47.88;
			T_amb = FDMExec.Atmosphere.Temperature * (5.0 / 9.0);  // convert from Rankine to Kelvin

			RPM = thruster.GetRPM() * thruster.GetGearRatio();

			IAS = FDMExec.Auxiliary.VcalibratedKTS;

			doEngineStartup();
			if(Boosted) doBoostControl();
			doMAP();
			doAirFlow();
			doFuelFlow();

			//Now that the fuel flow is done check if the mixture is too lean to run the engine
			//Assume lean limit at 22 AFR for now - thats a thi of 0.668
			//This might be a bit generous, but since there's currently no audiable warning of impending
			//cutout in the form of misfiring and/or rough running its probably reasonable for now.
			if (equivalence_ratio < 0.668)
				running = false;

			doEnginePower();
			doEGT();
			doCHT();
			doOilTemperature();
			doOilPressure();

			if (thruster.GetThrusterType() == Thruster.ThrusterType.Propeller) 
			{
				((Propeller)thruster).SetAdvance(FDMExec.FlightControlSystem.GetPropAdvance(engineNumber));
                ((Propeller)thruster).SetFeather(FDMExec.FlightControlSystem.GetPropFeather(engineNumber));

			}

			PowerAvailable = (HP * Constants.hptoftlbssec) - thruster.GetPowerRequired();

			return thruster.Calculate(PowerAvailable);
		}

		public override double GetPowerAvailable() {return PowerAvailable;}

		public override double CalcFuelNeed()
		{
			return fuelFlow_gph / 3600 * 6 * FDMExec.State.DeltaTime * FDMExec.Propulsion.Rate;
		}


		public void SetMagnetos(int magnetos) {Magnetos = magnetos;}

		public double  GetEGT() { return EGT_degC; }
		public int     GetMagnetos() {return Magnetos;}

		public double getExhaustGasTemp_degF() {return Conversion.KelvinToFahrenheit(ExhaustGasTemp_degK);}
		public double getManifoldPressure_inHg() {return ManifoldPressure_inHg;}
		public double getCylinderHeadTemp_degF() {return Conversion.KelvinToFahrenheit(CylinderHeadTemp_degK);}
		public double getOilPressure_psi() {return OilPressure_psi;}
		public double getOilTemp_degF () {return Conversion.KelvinToFahrenheit(OilTemp_degK);}
		public double getRPM() {return RPM;}


		private int crank_counter;

        /* TODO. To check with Jon. These variables are not used ??
		private double BrakeHorsePower;
		private double SpeedSlope;
		private double SpeedIntercept;
		private double AltitudeSlope;
        */
		private double PowerAvailable;

		// timestep
		private double dt;

		/// <summary>
		///  Start or stop the engine.
		/// </summary>
		private void doEngineStartup()
		{
			// Check parameters that may alter the operating state of the engine.
			// (spark, fuel, starter motor etc)
			bool spark;
			bool fuel;

			// Check for spark
			Magneto_Left = false;
			Magneto_Right = false;
			// Magneto positions:
			// 0 . off
			// 1 . left only
			// 2 . right only
			// 3 . both
			if (Magnetos != 0) 
			{
				spark = true;
			} 
			else 
			{
				spark = false;
			}  // neglects battery voltage, master on switch, etc for now.

			if ((Magnetos == 1) || (Magnetos > 2)) Magneto_Left = true;
			if (Magnetos > 1)  Magneto_Right = true;

			// Assume we have fuel for now
			fuel = !starved;

			// Check if we are turning the starter motor
			if (cranking != starter) 
			{
				// This check saves .../cranking from getting updated every loop - they
				// only update when changed.
				cranking = starter;
				crank_counter = 0;
			}

			if (cranking) crank_counter++;  //Check mode of engine operation

			if (!running && spark && fuel) 
			{  // start the engine if revs high enough
				if (cranking) 
				{
					if ((RPM > 450) && (crank_counter > 175)) // Add a little delay to startup
						running = true;                         // on the starter
				} 
				else 
				{
					if (RPM > 450)                            // This allows us to in-air start
						running = true;                         // when windmilling
				}
			}

			// Cut the engine *power* - Note: the engine may continue to
			// spin if the prop is in a moving airstream

			if ( running && (!spark || !fuel) ) running = false;

			// Check for stalling (RPM = 0).
			if (running) 
			{
				if (RPM == 0) 
				{
					running = false;
				} 
				else if ((RPM <= 480) && (cranking)) 
				{
					running = false;
				}
			}
		}

		/// <summary>
		/// Calculate the Current Boost Speed
		/// 
		/// This function calculates the current turbo/supercharger boost speed
		/// based on altitude and the (automatic) boost-speed control valve configuration.
		/// 
		/// Inputs: p_amb, BoostSwitchPressure, BoostSwitchHysteresis
		/// 
		/// Outputs: BoostSpeed
		/// </summary>
		private void doBoostControl()
		{
			if(BoostSpeed < BoostSpeeds - 1) 
			{
				// Check if we need to change to a higher boost speed
				if(p_amb < BoostSwitchPressure[BoostSpeed] - BoostSwitchHysteresis) 
				{
					BoostSpeed++;
				}
			} 
			else if(BoostSpeed > 0) 
			{
				// Check if we need to change to a lower boost speed
				if(p_amb > BoostSwitchPressure[BoostSpeed - 1] + BoostSwitchHysteresis) 
				{
					BoostSpeed--;
				}
			}
		}


		/// <summary>
		/// Calculate the manifold absolute pressure (MAP) in inches hg
		/// 
		/// This function calculates manifold absolute pressure (MAP)
		/// from the throttle position, turbo/supercharger boost control
		/// system, engine speed and local ambient air density.
		/// 
		/// TODO: changes in MP should not be instantaneous -- introduce
		/// a lag between throttle changes and MP changes, to allow pressure
		/// to build up or disperse.
		/// 
		/// Inputs: minMAP, maxMAP, p_amb, Throttle
		/// 
		/// Outputs: MAP, ManifoldPressure_inHg
		/// </summary>
		private void doMAP()
		{
			if(RPM > 10) 
			{
				// Naturally aspirated
				MAP = minMAP + (throttle * (maxMAP - minMAP));
				MAP *= p_amb / p_amb_sea_level;
				if(Boosted) 
				{
					// If takeoff boost is fitted, we currently assume the following throttle map:
					// (In throttle % - actual input is 0 . 1)
					// 99 / 100 - Takeoff boost
					// 96 / 97 / 98 - Rated boost
					// 0 - 95 - Idle to Rated boost (MinManifoldPressure to MaxManifoldPressure)
					// In real life, most planes would be fitted with a mechanical 'gate' between
					// the rated boost and takeoff boost positions.
					double T = throttle; // processed throttle value.
					bool bTakeoffPos = false;
					if(bTakeoffBoost) 
					{
						if(throttle > 0.98) 
						{
							//cout << "Takeoff Boost!!!!\n";
							bTakeoffPos = true;
						} 
						else if(throttle <= 0.95) 
						{
							bTakeoffPos = false;
							T *= 1.0 / 0.95;
						} 
						else 
						{
							bTakeoffPos = false;
							//cout << "Rated Boost!!\n";
							T = 1.0;
						}
					}
					// Boost the manifold pressure.
					MAP *= BoostMul[BoostSpeed];
					// Now clip the manifold pressure to BCV or Wastegate setting.
					if(bTakeoffPos) 
					{
						if(MAP > TakeoffMAP[BoostSpeed]) 
						{
							MAP = TakeoffMAP[BoostSpeed];
						}
					} 
					else 
					{
						if(MAP > RatedMAP[BoostSpeed]) 
						{
							MAP = RatedMAP[BoostSpeed];
						}
					}
				}
			} 
			else 
			{
				// rpm < 10 - effectively stopped.
				// TODO - add a better variation of MAP with engine speed
				MAP = FDMExec.Atmosphere.Pressure * 47.88; // psf to Pa
			}

			// And set the value in American units as well
			ManifoldPressure_inHg = MAP / 3376.85;
		}

		/// <summary>
		///  Calculate the air flow through the engine.
		///  Also calculates ambient air density
		///  (used in CHT calculation for air-cooled engines).
		///  
		///   Inputs: p_amb, R_air, T_amb, MAP, Displacement,
		///    RPM, volumetric_efficiency
		///  
		///  TODO: Model inlet manifold air temperature.
		///  
		///  Outputs: rho_air, m_dot_air
		/// </summary>
		private void doAirFlow()
		{
			rho_air = p_amb / (R_air * T_amb);
			double rho_air_manifold = MAP / (R_air * T_amb);
			double displacement_SI = Displacement * Constants.in3tom3;
			double swept_volume = (displacement_SI * (RPM/60)) / 2;
			double v_dot_air = swept_volume * volumetric_efficiency;
			m_dot_air = v_dot_air * rho_air_manifold;
		}

		/// <summary>
		/// Calculate the fuel flow into the engine.
		/// 
		///  Inputs: Mixture, thi_sea_level, p_amb_sea_level, p_amb, m_dot_air
		/// 
		///  Outputs: equivalence_ratio, m_dot_fuel
		/// </summary>
		private void doFuelFlow()
		{
			double thi_sea_level = 1.3 * mixture;
			equivalence_ratio = thi_sea_level * p_amb_sea_level / p_amb;
			m_dot_fuel = m_dot_air / 14.7 * equivalence_ratio;
			fuelFlow_gph = m_dot_fuel
				* 3600			// seconds to hours
				* 2.2046			// kg to lb
				/ 6.6;			// lb to gal_us of kerosene
		}

		/// <summary>
		/// Calculate the power produced by the engine.
		/// 
		///  Currently, the JSBSim propellor model does not allow the
		///  engine to produce enough RPMs to get up to a high horsepower.
		///  When tested with sufficient RPM, it has no trouble reaching
		///  200HP.
		/// 
		///  Inputs: ManifoldPressure_inHg, p_amb, p_amb_sea_level, RPM, T_amb,
		///    equivalence_ratio, Cycles, MaxHP
		/// 
		///  Outputs: Percentage_Power, HP
		/// </summary>
		private void doEnginePower()
		{
			if (running) 
			{
				double T_amb_degF = Conversion.KelvinToFahrenheit(T_amb);
				double T_amb_sea_lev_degF = Conversion.KelvinToFahrenheit(288);

				// FIXME: this needs to be generalized
				double ManXRPM;  // Convienience term for use in the calculations
				if(Boosted) 
				{
					// Currently a simple linear fit.
					// The zero crossing is moved up the speed-load range to reduce the idling power.
					// This will change!
					double zeroOffset = (minMAP / 2.0) * (IdleRPM / 2.0);
					ManXRPM = MAP * (RPM > RatedRPM[BoostSpeed] ? RatedRPM[BoostSpeed] : RPM);
					// The speed clip in the line above is deliberate.
					Percentage_Power = ((ManXRPM - zeroOffset) / ((RatedMAP[BoostSpeed] * RatedRPM[BoostSpeed]) - zeroOffset)) * 107.0;
					Percentage_Power -= 7.0;  // Another idle power reduction offset - see line above with 107.
					if (Percentage_Power < 0.0) Percentage_Power = 0.0;
					// Note that %power is allowed to go over 100 for boosted powerplants
					// such as for the BCV-override or takeoff power settings.
					// TODO - currently no altitude effect (temperature & exhaust back-pressure) modelled
					// for boosted engines.
				} 
				else 
				{
					ManXRPM = ManifoldPressure_inHg * RPM; // Note that inHg must be used for the following correlation.
					Percentage_Power = (6e-9 * ManXRPM * ManXRPM) + (8e-4 * ManXRPM) - 1.0;
					Percentage_Power += ((T_amb_sea_lev_degF - T_amb_degF) * 7 /120);
					if (Percentage_Power < 0.0) Percentage_Power = 0.0;
					else if (Percentage_Power > 100.0) Percentage_Power = 100.0;
				}

				double Percentage_of_best_power_mixture_power =
					Power_Mixture_Correlation.GetValue(14.7 / equivalence_ratio);

				Percentage_Power *= Percentage_of_best_power_mixture_power / 100.0;

				if(Boosted) 
				{
					HP = Percentage_Power * RatedPower[BoostSpeed] / 100.0;
				} 
				else 
				{
					HP = Percentage_Power * MaxHP / 100.0;
				}

			} 
			else 
			{
				// Power output when the engine is not running
				if (cranking) 
				{
					if (RPM < 10) 
					{
						HP = 3.0;   // This is a hack to prevent overshooting the idle rpm in
						// the first time step. It may possibly need to be changed
						// if the prop model is changed.
					} 
					else if (RPM < 480) 
					{
						HP = 3.0 + ((480 - RPM) / 10.0);
						// This is a guess - would be nice to find a proper starter moter torque curve
					} 
					else 
					{
						HP = 3.0;
					}
				} 
				else 
				{
					// Quick hack until we port the FMEP stuff
					if (RPM > 0.0)
						HP = -1.5;
					else
						HP = 0.0;
				}
			}
			//cout << "Power = " << HP << '\n';
		}

		/// <summary>
		/// Calculate the exhaust gas temperature.
		///
		/// Inputs: equivalence_ratio, m_dot_fuel, calorific_value_fuel,
		///  Cp_air, m_dot_air, Cp_fuel, m_dot_fuel, T_amb, Percentage_Power
		///
		/// Outputs: combustion_efficiency, ExhaustGasTemp_degK
		/// </summary>
		private void doEGT()
		{
			double delta_T_exhaust;
			double enthalpy_exhaust;
			double heat_capacity_exhaust;
			double dEGTdt;

			if ((running) && (m_dot_air > 0.0)) 
			{  // do the energy balance
				combustion_efficiency = Lookup_Combustion_Efficiency.GetValue(equivalence_ratio);
				enthalpy_exhaust = m_dot_fuel * calorific_value_fuel *
					combustion_efficiency * 0.33;
				heat_capacity_exhaust = (Cp_air * m_dot_air) + (Cp_fuel * m_dot_fuel);
				delta_T_exhaust = enthalpy_exhaust / heat_capacity_exhaust;
				ExhaustGasTemp_degK = T_amb + delta_T_exhaust;
				ExhaustGasTemp_degK *= 0.444 + ((0.544 - 0.444) * Percentage_Power / 100.0);
			} 
			else 
			{  // Drop towards ambient - guess an appropriate time constant for now
				dEGTdt = (298.0 - ExhaustGasTemp_degK) / 100.0;
				delta_T_exhaust = dEGTdt * dt;
				ExhaustGasTemp_degK += delta_T_exhaust;
			}
		}

		/// <summary>
		/// Calculate the cylinder head temperature.
		/// 
		/// Inputs: T_amb, IAS, rho_air, m_dot_fuel, calorific_value_fuel,
		///   combustion_efficiency, RPM
		/// 
		///  Outputs: CylinderHeadTemp_degK
		/// </summary>
		private void doCHT()
		{
			double h1 = -95.0;
			double h2 = -3.95;
			double h3 = -0.05;

			double arbitary_area = 1.0;
			double CpCylinderHead = 800.0;
			double MassCylinderHead = 8.0;

			double temperature_difference = CylinderHeadTemp_degK - T_amb;
			double v_apparent = IAS * 0.5144444;
			double v_dot_cooling_air = arbitary_area * v_apparent;
			double m_dot_cooling_air = v_dot_cooling_air * rho_air;
			double dqdt_from_combustion =
				m_dot_fuel * calorific_value_fuel * combustion_efficiency * 0.33;
			double dqdt_forced = (h2 * m_dot_cooling_air * temperature_difference) +
				(h3 * RPM * temperature_difference);
			double dqdt_free = h1 * temperature_difference;
			double dqdt_cylinder_head = dqdt_from_combustion + dqdt_forced + dqdt_free;

			double HeatCapacityCylinderHead = CpCylinderHead * MassCylinderHead;

			CylinderHeadTemp_degK +=
				(dqdt_cylinder_head / HeatCapacityCylinderHead) * dt;
		}

		/// <summary>
		/// Calculate the oil temperature.
		/// 
		///  Inputs: Percentage_Power, running flag.
		/// 
		/// Outputs: OilTemp_degK
		/// </summary>
		private void doOilTemperature()
		{
			double idle_percentage_power = 2.3;        // approximately
			double target_oil_temp;        // Steady state oil temp at the current engine conditions
			double time_constant;          // The time constant for the differential equation

			if (running) 
			{
				target_oil_temp = 363;
				time_constant = 500;        // Time constant for engine-on idling.
				if (Percentage_Power > idle_percentage_power) 
				{
					time_constant /= ((Percentage_Power / idle_percentage_power) / 10.0); // adjust for power
				}
			} 
			else 
			{
				target_oil_temp = 298;
				time_constant = 1000;  // Time constant for engine-off; reflects the fact
				// that oil is no longer getting circulated
			}

			double dOilTempdt = (target_oil_temp - OilTemp_degK) / time_constant;

			OilTemp_degK += (dOilTempdt * dt);
		}

		/// <summary>
		/// Calculate the oil pressure.
		/// 
		/// Inputs: RPM
		/// 
		/// Outputs: OilPressure_psi 
		/// </summary>
		private void doOilPressure()
		{
			double Oil_Press_Relief_Valve = 60; // FIXME: may vary by engine
			double Oil_Press_RPM_Max = 1800;    // FIXME: may vary by engine
			double Design_Oil_Temp = 358;	      // degK; FIXME: may vary by engine
			double Oil_Viscosity_Index = 0.25;

			OilPressure_psi = (Oil_Press_Relief_Valve / Oil_Press_RPM_Max) * RPM;

			if (OilPressure_psi >= Oil_Press_Relief_Valve) 
			{
				OilPressure_psi = Oil_Press_Relief_Valve;
			}

			OilPressure_psi += (Design_Oil_Temp - OilTemp_degK) * Oil_Viscosity_Index;
		}

		//
		// constants
		//

		private const double R_air = 287.3;
		private const double rho_fuel = 800.0;    // kg/m^3
		private const double calorific_value_fuel = 47.3e6;  // W/Kg (approximate)
		private const double Cp_air = 1005;      // J/KgK
		private const double Cp_fuel = 1700;     // J/KgK

		private const int FG_MAX_BOOST_SPEEDS = 3;

        private static readonly double[,] combustionTable = new double[,]{{0.00, 0.980},
                                                        {0.90, 0.980},
                                                        {1.00, 0.970},
                                                        {1.05, 0.950},
                                                        {1.10, 0.900},
                                                        {1.15, 0.850},
                                                        {1.20, 0.790},
                                                        {1.30, 0.700},
                                                        {1.40, 0.630},
                                                        {1.50, 0.570},
                                                        {1.60, 0.525},
                                                        {2.00, 0.345}};
        private Table Lookup_Combustion_Efficiency = new Table(combustionTable);

        private static readonly double[,] mixtureTable = new double[,]{{(14.7/1.6), 78.0},
                                                                    {10, 86.0},
                                                                    {11, 93.5},
                                                                    {12, 98.0},
                                                                    {13, 100.0},
                                                                    {14, 99.0},
                                                                    {15, 96.4},
                                                                    {16, 92.5},
                                                                    {17, 88.0},
                                                                    {18, 83.0},
                                                                    {19, 78.5},
                                                                    {20, 74.0},
                                                                    {(14.7/0.6), 58}};
        private Table Power_Mixture_Correlation = new Table(mixtureTable);

		//
		// Configuration
		//
		private double MinManifoldPressure_inHg; // Inches Hg
		private double MaxManifoldPressure_inHg; // Inches Hg
		private double Displacement;             // cubic inches
		private double MaxHP;                    // horsepower
		private double Cycles;                   // cycles/power stroke
		private double IdleRPM;                  // revolutions per minute
		private int BoostSpeeds;	// Number of super/turbocharger boost speeds - zero implies no turbo/supercharging.
		private int BoostSpeed;	// The current boost-speed (zero-based).
		private bool Boosted;		// Set true for boosted engine.
		private int BoostOverride;	// The raw value read in from the config file - should be 1 or 0 - see description below.
		private bool bBoostOverride;	// Set true if pilot override of the boost regulator was fitted.
		// (Typically called 'war emergency power').
		private bool bTakeoffBoost;	// Set true if extra takeoff / emergency boost above rated boost could be attained.
		// (Typically by extra throttle movement past a mechanical 'gate').
		private double TakeoffBoost;	// Sea-level takeoff boost in psi. (if fitted).
		private double[] RatedBoost = new double[FG_MAX_BOOST_SPEEDS];	// Sea-level rated boost in psi.
		private double[] RatedAltitude = new double[FG_MAX_BOOST_SPEEDS];	// Altitude at which full boost is reached (boost regulation ends)
		// and at which power starts to fall with altitude [ft].
		private double[] RatedRPM = new double[FG_MAX_BOOST_SPEEDS]; // Engine speed at which the rated power for each boost speed is delivered [rpm].
		private double[] RatedPower = new double[FG_MAX_BOOST_SPEEDS];	// Power at rated throttle position at rated altitude [HP].
		private double[] BoostSwitchAltitude = new double[FG_MAX_BOOST_SPEEDS - 1];	// Altitude at which switchover (currently assumed automatic)
		// from one boost speed to next occurs [ft].
		private double[] BoostSwitchPressure = new double[FG_MAX_BOOST_SPEEDS - 1];  // Pressure at which boost speed switchover occurs [Pa]
		private double[] BoostMul = new double[FG_MAX_BOOST_SPEEDS];	// Pressure multipier of unregulated supercharger
		private double[] RatedMAP = new double[FG_MAX_BOOST_SPEEDS];	// Rated manifold absolute pressure [Pa] (BCV clamp)
		private double[] TakeoffMAP = new double[FG_MAX_BOOST_SPEEDS];	// Takeoff setting manifold absolute pressure [Pa] (BCV clamp)
		private double BoostSwitchHysteresis;	// Pa.

		private double minMAP;  // Pa
		private double maxMAP;  // Pa
		private double MAP;     // Pa

		//
		// Inputs (in addition to those in FGEngine).
		//
		private double p_amb;              // Pascals
		private double p_amb_sea_level;    // Pascals
		private double T_amb;              // degrees Kelvin
		private double RPM;                // revolutions per minute
		private double IAS;                // knots
		private bool Magneto_Left;
		private bool Magneto_Right;
		private int Magnetos;

		//
		// Outputs (in addition to those in FGEngine).
		//
		private double rho_air;
		private double volumetric_efficiency;
		private double m_dot_air;
		private double equivalence_ratio;
		private double m_dot_fuel;
		private double Percentage_Power;
		private double HP;
		private double combustion_efficiency;
		private double ExhaustGasTemp_degK;
		private double EGT_degC;
		private double ManifoldPressure_inHg;
		private double CylinderHeadTemp_degK;
		private double OilPressure_psi;
		private double OilTemp_degK;

		private const string IdSrc = "$Id: FGPiston.cpp,v 1.72 2004/12/31 19:19:55 jberndt Exp $";
	}
}
