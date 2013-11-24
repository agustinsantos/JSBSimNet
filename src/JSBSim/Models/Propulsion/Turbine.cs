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
	using System.Collections;
	using System.Xml;
	using System.IO;
	using System.Text.RegularExpressions;

	// Import log4net classes.
	using log4net;

	using CommonUtils.MathLib;
    using JSBSim.MathValues;
	using JSBSim.Format;

	/// <summary>
	/// This class models a turbine engine.  Based on Jon Berndt's FGTurbine module.
	/// Here the term "phase" signifies the engine's mode of operation.  At any given
	/// time the engine is in only one phase.  At simulator startup the engine will be
	/// placed in the Trim phase in order to provide a simplified thrust value without
	/// throttle lag.  When trimming is complete the engine will go to the Off phase,
	/// unless the value FGEngine::Running has been previously set to true, in which
	/// case the engine will go to the Run phase.  Once an engine is in the Off phase
	/// the full starting procedure (or airstart) must be used to get it running.
	/// <P>
	/// - STARTING (on ground):
	/// -# Set the control FGEngine::Starter to true.  The engine will spin up to
	/// a maximum of about %25 N2 (%5.2 N1).  This simulates the action of a
	/// pneumatic starter.
	/// -# After reaching %15 N2 set the control FGEngine::Cutoff to false. If fuel
	/// is available the engine will now accelerate to idle.  The starter will
	/// automatically be set to false after the start cycle.
	/// <P>
	/// - STARTING (in air):
	/// -# Increase speed to obtain a minimum of %15 N2.  If this is not possible,
	/// the starter may be used to assist.
	/// -# Place the control FGEngine::Cutoff to false.
	/// <P>
	/// Ignition is assumed to be on anytime the Cutoff control is set to false,
	/// therefore a seperate ignition system is not modeled.
	/// 
	/// Configuration File Format
	/// <pre>
	/// <FG_TURBINE NAME="<name>">
	/// MILTHRUST   \<thrust>
	/// MAXTHRUST   \<thrust>
	/// BYPASSRATIO \<bypass ratio>
	/// TSFC        \<thrust specific fuel consumption>
	/// ATSFC       \<afterburning thrust specific fuel consumption>
	/// IDLEN1      \<idle N1>
	/// IDLEN2      \<idle N2>
	/// MAXN1       \<max N1>
	/// MAXN2       \<max N2>
	/// AUGMENTED   \<0|1>
	/// AUGMETHOD   \<0|1>
	/// INJECTED    \<0|1>
	/// ...
	/// \</FG_TURBINE>
	/// </pre>
	/// Definition of the turbine engine configuration file parameters:
	/// <pre>
	/// <b>MILTHRUST</b> - Maximum thrust, static, at sea level, lbf.
	/// <b>MAXTHRUST</b> - Afterburning thrust, static, at sea level, lbf
	/// [this value will be ignored when AUGMENTED is zero (false)].
	/// <b>BYPASSRATIO</b> - Ratio of bypass air flow to core air flow.
	/// <b>TSFC</b> - Thrust-specific fuel consumption, lbm/hr/lbf
	/// [i.e. fuel flow divided by thrust].
	/// <b>ATSFC</b> - Afterburning TSFC, lbm/hr/lbf
	/// [this value will be ignored when AUGMENTED is zero (false)]
	/// <b>IDLEN1</b> - Fan rotor rpm (% of max) at idle
	/// <b>IDLEN2</b> - Core rotor rpm (% of max) at idle
	/// <b>MAXN1</b> - Fan rotor rpm (% of max) at full throttle [not always 100!]
	/// <b>MAXN2</b> - Core rotor rpm (% of max) at full throttle [not always 100!]
	/// <b>AUGMENTED</b>
	/// 0 == afterburner not installed
	/// 1 == afterburner installed
	/// <b>AUGMETHOD</b>
	/// 0 == afterburner activated by property /engines/engine[n]/augmentation
	/// 1 == afterburner activated by pushing throttle above 99% position
	/// 2 == throttle range is expanded in the FCS, and values above 1.0 are afterburner range
	/// [this item will be ignored when AUGMENTED == 0]
	/// <b>INJECTED</b>
	/// 0 == Water injection not installed
	/// 1 == Water injection installed
	/// </pre>
	/// @author David P. Culp
	/// </summary>
	public class Turbine : Engine
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
		/// <param name="exec">to executive structure</param>
		/// <param name="element">engine config file instance</param>
		/// <param name="engine_number">engine number</param>
        public Turbine(FDMExecutive exec, XmlElement parent, XmlElement element, int engine_number)
            : base(exec, parent, element, engine_number)
		{
			SetDefaults();

            Load(exec, element);
		}

		public enum PhaseType { Off, Run, SpinUp, Start, Stall, Seize, Trim };

		public override double Calculate()
		{
			TAT = (FDMExec.Auxiliary.TotalTemperature - 491.69) * 0.5555556;
			dt = FDMExec.State.DeltaTime * FDMExec.Propulsion.Rate;
			ThrottlePos = FDMExec.FlightControlSystem.GetThrottlePos(engineNumber);
			if (ThrottlePos > 1.0) 
			{
				AugmentCmd = ThrottlePos - 1.0;
				ThrottlePos -= AugmentCmd;
			} 
			else 
			{
				AugmentCmd = 0.0;
			}

			// When trimming is finished check if user wants engine OFF or RUNNING
			if ((phase == PhaseType.Trim) && (dt > 0)) 
			{
				if (running && !starved) 
				{
					phase = PhaseType.Run;
					N2 = IdleN2 + ThrottlePos * N2_factor;
					N1 = IdleN1 + ThrottlePos * N1_factor;
					OilTemp_degK = 366.0;
					Cutoff = false;
				}
				else 
				{
					phase = PhaseType.Off;
					Cutoff = true;
					EGT_degC = TAT;
				}
			}

			if (!running && Cutoff && starter) 
			{
				if (phase == PhaseType.Off) phase = PhaseType.SpinUp;
			}
			if (!running && !Cutoff && (N2 > 15.0)) phase = PhaseType.Start;
			if (Cutoff && (phase != PhaseType.SpinUp)) phase = PhaseType.Off;
			if (dt == 0) phase = PhaseType.Trim;
			if (starved) phase = PhaseType.Off;
			if (Stalled) phase = PhaseType.Stall;
			if (Seized) phase = PhaseType.Seize;

			switch (phase) 
			{
				case PhaseType.Off:    thrust = Off(); break;
				case PhaseType.Run:    thrust = Run(); break;
				case PhaseType.SpinUp: thrust = SpinUp(); break;
				case PhaseType.Start:  thrust = Start(); break;
				case PhaseType.Stall:  thrust = Stall(); break;
				case PhaseType.Seize:  thrust = Seize(); break;
				case PhaseType.Trim:   thrust = Trim(); break;
				default: thrust = Off(); break;
			}

            // allow thruster to modify thrust (i.e. reversing)
            thrust = thruster.Calculate(thrust);

			return thrust;
		}

		public override double CalcFuelNeed()
		{
			return fuelFlow_pph /3600 * FDMExec.State.DeltaTime * FDMExec.Propulsion.Rate;
		}

		public override double GetPowerAvailable()
		{
			if( ThrottlePos <= 0.77 )
				return 64.94*ThrottlePos;
			else
				return 217.38*ThrottlePos - 117.38;
		}

		public double Seek(double var, double target, double accel, double decel)
		{
			double v = var;
			if (v > target) 
			{
				v -= dt * decel;
				if (v < target) v = target;
			} 
			else if (v < target) 
			{
				v += dt * accel;
				if (v > target) v = target;
			}
			return v;
		}

		public PhaseType GetPhase() { return phase; }

		public bool GetOvertemp()  {return Overtemp; }
		public bool GetInjection() {return Injection;}
		public bool GetFire() { return Fire; }
		public bool GetAugmentation() {return Augmentation;}
		public bool GetReversed() { return Reversed; }
		public bool GetCutoff() { return Cutoff; }
		public int GetIgnition() {return Ignition;}

		public double GetInlet() { return InletPosition; }
		public double GetNozzle() { return NozzlePosition; }
		public double GetBleedDemand() {return BleedDemand;}
		public double GetN1() {return N1;}
		public double GetN2() {return N2;}
		public double GetEPR() {return EPR;}
		public double GetEGT() {return EGT_degC;}

		public double getOilPressure_psi ()  {return OilPressure_psi;}
		public double getOilTemp_degF () {return Conversion.KelvinToFahrenheit(OilTemp_degK);}

		public void SetInjection(bool injection) {Injection = injection;}
		public void SetIgnition(int ignition) {Ignition = ignition;}
		public void SetAugmentation(bool augmentation) {Augmentation = augmentation;}
		public void SetPhase( PhaseType p ) { phase = p; }
		public void SetEPR(double epr) {EPR = epr;}
		public void SetBleedDemand(double bleedDemand) {BleedDemand = bleedDemand;}
		public void SetReverse(bool reversed) { Reversed = reversed; }
		public void SetCutoff(bool cutoff) { Cutoff = cutoff; }

		public override string GetEngineLabels(string delimeter)
		{
			string buf;

			buf = Name + "_N1[" + engineNumber + "]" + delimeter
				+ Name + "_N2[" + engineNumber + "]" + delimeter
				+ thruster.GetThrusterLabels(engineNumber, delimeter);

			return buf;
		}

        public override string GetEngineValues(string format, IFormatProvider provider, string delimeter)
		{
			string buf;

            buf = N1.ToString(format, provider) + delimeter
                + N2.ToString(format, provider) + delimeter
				+ thruster.GetThrusterValues(engineNumber, delimeter);

			return buf;
		}

        private PhaseType phase;         ///< Operating mode, or "phase"
		private double MilThrust;        ///< Maximum Unaugmented Thrust, static @ S.L. (lbf)
		private double MaxThrust;        ///< Maximum Augmented Thrust, static @ S.L. (lbf)
		private double BypassRatio;      ///< Bypass Ratio
		private double TSFC;             ///< Thrust Specific Fuel Consumption (lbm/hr/lbf)
		private double ATSFC;            ///< Augmented TSFC (lbm/hr/lbf)
		private double IdleN1;           ///< Idle N1
		private double IdleN2;           ///< Idle N2
		private double N1;               ///< N1
		private double N2;               ///< N2
		private double MaxN1;            ///< N1 at 100% throttle
		private double MaxN2;            ///< N2 at 100% throttle
		private double IdleFF;           ///< Idle Fuel Flow (lbm/hr)
		private double delay;            ///< Inverse spool-up time from idle to 100% (seconds)
		private double dt;               ///< Simulator time slice
		private double N1_factor;        ///< factor to tie N1 and throttle
		private double N2_factor;        ///< factor to tie N2 and throttle
		private double ThrottlePos;      ///< FCS-supplied throttle position
		private double AugmentCmd;       ///< modulated afterburner command (0.0 to 1.0)
		private double TAT;              ///< total air temperature (deg C)
		private bool Stalled;            ///< true if engine is compressor-stalled
		private bool Seized;             ///< true if inner spool is seized
		private bool Overtemp;           ///< true if EGT exceeds limits
		private bool Fire;               ///< true if engine fire detected
		private bool Injection;
		private bool Augmentation;
		private bool Reversed;
		private bool Cutoff;
		private int Injected;            ///< = 1 if water injection installed
		private int Ignition;
		private int Augmented;           ///< = 1 if augmentation installed
		private int AugMethod;           ///< = 0 if using property /engine[n]/augmentation
		///< = 1 if using last 1% of throttle movement
		///< = 2 if using FCS-defined throttle
		private double EGT_degC;
		private double EPR;
		private double OilPressure_psi;
		private double OilTemp_degK;
		private double BleedDemand;
		private double InletPosition;
		private double NozzlePosition;
        private double correctedTSFC;

        private Function IdleThrustLookup;
        private Function MilThrustLookup;
        private Function MaxThrustLookup;
        private Function InjectionLookup;

		private double Off()
		{
			double qbar = FDMExec.Auxiliary.Qbar;
			running = false;
			fuelFlow_pph = Seek(fuelFlow_pph, 0, 1000.0, 10000.0);
			N1 = Seek(N1, qbar/10.0, N1/2.0, N1/2.0);
			N2 = Seek(N2, qbar/15.0, N2/2.0, N2/2.0);
			EGT_degC = Seek(EGT_degC, TAT, 11.7, 7.3);
			OilTemp_degK = Seek(OilTemp_degK, TAT + 273.0, 0.2, 0.2);
			OilPressure_psi = N2 * 0.62;
			NozzlePosition = Seek(NozzlePosition, 1.0, 0.8, 0.8);
			EPR = Seek(EPR, 1.0, 0.2, 0.2);
			Augmentation = false;
			return 0.0;
		}

		private double Run()
		{
			double idlethrust, milthrust, thrust;
			double N2norm;   // 0.0 = idle N2, 1.0 = maximum N2
            idlethrust = MilThrust * IdleThrustLookup.GetValue();
            milthrust = (MilThrust - idlethrust) * MilThrustLookup.GetValue();

			running = true;
			starter = false;

			N2 = Seek(N2, IdleN2 + ThrottlePos * N2_factor, delay, delay * 3.0);
			N1 = Seek(N1, IdleN1 + ThrottlePos * N1_factor, delay, delay * 2.4);
			N2norm = (N2 - IdleN2) / N2_factor;
			thrust = idlethrust + (milthrust * N2norm * N2norm);
			EGT_degC = TAT + 363.1 + ThrottlePos * 357.1;
			OilPressure_psi = N2 * 0.62;
			OilTemp_degK = Seek(OilTemp_degK, 366.0, 1.2, 0.1);

			if (!Augmentation) 
			{
				double correctedTSFC = TSFC * (0.84 + (1-N2norm)*(1-N2norm));
				fuelFlow_pph = Seek(fuelFlow_pph, thrust * correctedTSFC, 1000.0, 100000);
				if (fuelFlow_pph < IdleFF) fuelFlow_pph = IdleFF;
				NozzlePosition = Seek(NozzlePosition, 1.0 - N2norm, 0.8, 0.8);
				thrust = thrust * (1.0 - BleedDemand);
				EPR = 1.0 + thrust/MilThrust;
			}

			if (AugMethod == 1) 
			{
				if ((ThrottlePos > 0.99) && (N2 > 97.0)) {Augmentation = true;}
				else {Augmentation = false;}
			}

			if ((Augmented == 1) && Augmentation && (AugMethod < 2)) 
			{
				thrust = MaxThrust * MaxThrustLookup.GetValue();
				fuelFlow_pph = Seek(fuelFlow_pph, thrust * ATSFC, 5000.0, 10000.0);
				NozzlePosition = Seek(NozzlePosition, 1.0, 0.8, 0.8);
			}

			if (AugMethod == 2) 
			{
				if (AugmentCmd > 0.0) 
				{
					Augmentation = true;
                    double tdiff = (MaxThrust * MaxThrustLookup.GetValue()) - thrust;
					thrust += (tdiff * AugmentCmd);
					fuelFlow_pph = Seek(fuelFlow_pph, thrust * ATSFC, 5000.0, 10000.0);
					NozzlePosition = Seek(NozzlePosition, 1.0, 0.8, 0.8);
				} 
				else 
				{
					Augmentation = false;
				}
			}

			if ((Injected == 1) && Injection) 
			{
                thrust = thrust * InjectionLookup.GetValue();
			}

			ConsumeFuel();
			if (Cutoff) phase = PhaseType.Off;
			if (starved) phase = PhaseType.Off;

			return thrust;
		}
		private double SpinUp()
		{
			running = false;
			fuelFlow_pph = 0.0;
			N2 = Seek(N2, 25.18, 3.0, N2/2.0);
			N1 = Seek(N1, 5.21, 1.0, N1/2.0);
			EGT_degC = Seek(EGT_degC, TAT, 11.7, 7.3);
			OilPressure_psi = N2 * 0.62;
			OilTemp_degK = Seek(OilTemp_degK, TAT + 273.0, 0.2, 0.2);
			EPR = 1.0;
			NozzlePosition = 1.0;
			return 0.0;
		}

		private double Start()
		{
			if ((N2 > 15.0) && !starved) 
			{       // minimum 15% N2 needed for start
				cranking = true;                   // provided for sound effects signal
				if (N2 < IdleN2) 
				{
					N2 = Seek(N2, IdleN2, 2.0, N2/2.0);
					N1 = Seek(N1, IdleN1, 1.4, N1/2.0);
					EGT_degC = Seek(EGT_degC, TAT + 363.1, 21.3, 7.3);
					fuelFlow_pph = Seek(fuelFlow_pph, IdleFF, 103.7, 103.7);
					OilPressure_psi = N2 * 0.62;
					ConsumeFuel();
				}
				else 
				{
					phase = PhaseType.Run;
					running = true;
					starter = false;
					cranking = false;
				}
			}
			else 
			{                 // no start if N2 < 15%
				phase = PhaseType.Off;
				starter = false;
			}

			return 0.0;
		}

		private double Stall()
		{
			double qbar = FDMExec.Auxiliary.Qbar;
			EGT_degC = TAT + 903.14;
			fuelFlow_pph = IdleFF;
			N1 = Seek(N1, qbar/10.0, 0, N1/10.0);
			N2 = Seek(N2, qbar/15.0, 0, N2/10.0);
			ConsumeFuel();
			if (ThrottlePos < 0.01) phase = PhaseType.Run;        // clear the stall with throttle

			return 0.0;
		}

		private double Seize()
		{
			double qbar = FDMExec.Auxiliary.Qbar;
			N2 = 0.0;
			N1 = Seek(N1, qbar/20.0, 0, N1/15.0);
			fuelFlow_pph = IdleFF;
			ConsumeFuel();
			OilPressure_psi = 0.0;
			OilTemp_degK = Seek(OilTemp_degK, TAT + 273.0, 0, 0.2);
			running = false;
			return 0.0;
		}

		private double Trim()
		{
			double idlethrust, milthrust, thrust, tdiff;
            idlethrust = MilThrust * IdleThrustLookup.GetValue();
            milthrust = (MilThrust - idlethrust) * MilThrustLookup.GetValue();
			thrust = (idlethrust + (milthrust * ThrottlePos * ThrottlePos))
				* (1.0 - BleedDemand);

            if (AugMethod == 1)
            {
                if ((ThrottlePos > 0.99) && (N2 > 97.0)) { Augmentation = true; }
                else { Augmentation = false; }
            }

            if ((Augmented == 1) && Augmentation && (AugMethod < 2))
            {
                thrust = MaxThrust * MaxThrustLookup.GetValue();
            }

            if (AugMethod == 2)
            {
                if (AugmentCmd > 0.0)
                {
                    tdiff = (MaxThrust * MaxThrustLookup.GetValue()) - thrust;
                    thrust += (tdiff * AugmentCmd);
                }
            }

            if ((Injected == 1) && Injection)
            {
                thrust = thrust * InjectionLookup.GetValue();
            }

			return thrust;
		}


		private void SetDefaults()
		{
			N1 = N2 = 0.0;
            engineType = EngineType.Turbine;
            MilThrust = 10000.0;
            MaxThrust = 10000.0;
            BypassRatio = 0.0;
            TSFC = 0.8;
            correctedTSFC = TSFC;
			ATSFC = 1.7;
			IdleN1 = 30.0;
			IdleN2 = 60.0;
			MaxN1 = 100.0;
			MaxN2 = 100.0;
			Augmented = 0;
			AugMethod = 0;
			Injected = 0;
			BleedDemand = 0.0;
			ThrottlePos = 0.0;
			AugmentCmd = 0.0;
			InletPosition = 1.0;
			NozzlePosition = 1.0;
			Augmentation = false;
			Injection = false;
			Reversed = false;
			Cutoff = true;
			phase = PhaseType.Off;
			Stalled = false;
			Seized = false;
			Overtemp = false;
			Fire = false;
			EGT_degC = 0.0;
		}

        private void Load(FDMExecutive exec, XmlElement element)
        {
            string property_prefix = "propulsion/engine[" + engineNumber + "]/";

            string token;
            XmlElement tmpElem;
            foreach (XmlNode currentNode in element.ChildNodes)
            {
                if (currentNode.NodeType == XmlNodeType.Element)
                {
                    tmpElem = currentNode as XmlElement;
                    token = tmpElem.LocalName;

                    if (token.Equals("milthrust"))
                        MilThrust = FormatHelper.ValueAsNumberConvertTo(tmpElem, "LBS");
                    else if (token.Equals("maxthrust"))
                        MaxThrust = FormatHelper.ValueAsNumberConvertTo(tmpElem, "LBS");
                    else if (token.Equals("bypassratio"))
                        BypassRatio = FormatHelper.ValueAsNumber(tmpElem);
                    else if (token.Equals("bleed"))
                        BleedDemand = FormatHelper.ValueAsNumber(tmpElem);
                    else if (token.Equals("tsfc"))
                        TSFC = FormatHelper.ValueAsNumber(tmpElem);
                    else if (token.Equals("atsfc"))
                        ATSFC = FormatHelper.ValueAsNumber(tmpElem);
                    else if (token.Equals("idlen1"))
                        IdleN1 = FormatHelper.ValueAsNumber(tmpElem);
                    else if (token.Equals("idlen2"))
                        IdleN2 = FormatHelper.ValueAsNumber(tmpElem);
                    else if (token.Equals("maxn1"))
                        MaxN1 = FormatHelper.ValueAsNumber(tmpElem);
                    else if (token.Equals("maxn2"))
                        MaxN2 = FormatHelper.ValueAsNumber(tmpElem);
                    else if (token.Equals("augmented"))
                        Augmented = (int)FormatHelper.ValueAsNumber(tmpElem);
                    else if (token.Equals("augmethod"))
                        AugMethod = (int)FormatHelper.ValueAsNumber(tmpElem);
                    else if (token.Equals("injected"))
                        Injected = (int)FormatHelper.ValueAsNumber(tmpElem);


                    else if (token.Equals("function"))
                    {
                        string name = tmpElem.GetAttribute("name");
                        if (name.Equals("IdleThrust"))
                        {
                            IdleThrustLookup = new Function(exec.PropertyManager, tmpElem, property_prefix);
                        }
                        else if (name.Equals("MilThrust"))
                        {
                            MilThrustLookup = new Function(exec.PropertyManager, tmpElem, property_prefix);
                        }
                        else if (name.Equals("AugThrust"))
                        {
                            MaxThrustLookup = new Function(exec.PropertyManager, tmpElem, property_prefix);
                        }
                        else if (name.Equals("Injection"))
                        {
                            InjectionLookup = new Function(exec.PropertyManager, tmpElem, property_prefix);
                        }
                        else
                        {
                            if (log.IsErrorEnabled)
                                log.Error("Unknown function type: " + name + " in turbine definition.");
                            throw new Exception("Unknown function type: " + name + " in turbine definition.");
                        }
                    }
                }
            }

            // Pre-calculations and initializations

            delay = 60.0 / (BypassRatio + 3.0);
            N1_factor = MaxN1 - IdleN1;
            N2_factor = MaxN2 - IdleN2;
            OilTemp_degK = (exec.Auxiliary.TotalTemperature - 491.69) * 0.5555556 + 273.0;
            IdleFF = Math.Pow(MilThrust, 0.2) * 107.0;  // just an estimate

            bindmodel();
        }

        private void bindmodel()
        {
            /* I dont like this. Try to change it to properties*/
            FDMExec.PropertyManager.Tie("propulsion/engine[" + engineNumber + "]/n1", this.GetN1, null);
            FDMExec.PropertyManager.Tie("propulsion/engine[" + engineNumber + "]/n2", this.GetN2, null);
            FDMExec.PropertyManager.Tie("propulsion/engine[" + engineNumber + "]/thrust", this.GetThrust, null);
        }
		private void unbind()
		{
			/// TODO
		}

		private const string IdSrc = "$Id: FGTurbine.cpp,v 1.21 2004/12/05 04:06:57 dpculp Exp $";

	}
}
