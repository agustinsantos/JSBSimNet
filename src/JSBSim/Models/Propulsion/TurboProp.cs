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
    using JSBSim.MathValues;

    public class TurboProp : Engine
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

        public enum phaseType { tpOff, tpRun, tpSpinUp, tpStart, tpStall, tpSeize, tpTrim };

        public TurboProp(FDMExecutive exec, XmlElement parent, XmlElement el, int engine_number)
            : base(exec, parent, el, engine_number)
        {
            SetDefaults();

            Load(exec, el);
        }


        /// <summary>
        /// The main purpose of Calculate() is to determine what phase the engine should
        /// be in, then call the corresponding function.
        /// </summary>
        /// <returns></returns>
        public override double Calculate()
        {
            TAT = (FDMExec.Auxiliary.TotalTemperature - 491.69) * 0.5555556;
            dt = FDMExec.State.DeltaTime * FDMExec.Propulsion.Rate;

            ThrottleCmd = FDMExec.FlightControlSystem.GetThrottleCmd(engineNumber);

            Prop_RPM = thruster.GetRPM() * thruster.GetGearRatio();
            if (thruster.GetThrusterType() == Thruster.ThrusterType.Propeller)
            {
                ((Propeller)thruster).SetAdvance(FDMExec.FlightControlSystem.GetPropAdvance(engineNumber));
                ((Propeller)thruster).SetFeather(FDMExec.FlightControlSystem.GetPropFeather(engineNumber));
                ((Propeller)thruster).SetReverse(Reversed);
                if (Reversed)
                {
                    ((Propeller)thruster).SetReverseCoef(ThrottleCmd);
                }
                else
                {
                    ((Propeller)thruster).SetReverseCoef(0.0);
                }
            }

            if (Reversed)
            {
                if (ThrottleCmd < BetaRangeThrottleEnd)
                {
                    ThrottleCmd = 0.0;  // idle when in Beta-range
                }
                else
                {
                    // when reversed:
                    ThrottleCmd = (ThrottleCmd - BetaRangeThrottleEnd) / (1 - BetaRangeThrottleEnd) * ReverseMaxPower;
                }
            }

            // When trimming is finished check if user wants engine OFF or RUNNING
            if ((phase == phaseType.tpTrim) && (dt > 0))
            {
                if (running && !starved)
                {
                    phase = phaseType.tpRun;
                    N2 = IdleN2;
                    N1 = IdleN1;
                    OilTemp_degK = 366.0;
                    Cutoff = false;
                }
                else
                {
                    phase = phaseType.tpOff;
                    Cutoff = true;
                    Eng_ITT_degC = TAT;
                    Eng_Temperature = TAT;
                    OilTemp_degK = TAT + 273.15;
                }
            }

            if (!running && Starter)
            {
                if (phase == phaseType.tpOff)
                {
                    phase = phaseType.tpSpinUp;
                    if (StartTime < 0) StartTime = 0;
                }
            }
            if (!running && !Cutoff && (N1 > 15.0))
            {
                phase = phaseType.tpStart;
                StartTime = -1;
            }
            if (Cutoff && (phase != phaseType.tpSpinUp)) phase = phaseType.tpOff;
            if (dt == 0) phase = phaseType.tpTrim;
            if (starved) phase = phaseType.tpOff;
            if (Condition >= 10)
            {
                phase = phaseType.tpOff;
                StartTime = -1;
            }

            if (Condition < 1)
            {
                if (Ielu_max_torque > 0
                  && -Ielu_max_torque > ((Propeller)(thruster)).GetTorque()
                  && ThrottleCmd >= OldThrottle)
                {
                    ThrottleCmd = OldThrottle - 0.1 * dt; //IELU down
                    Ielu_intervent = true;
                }
                else if (Ielu_max_torque > 0 && Ielu_intervent && ThrottleCmd >= OldThrottle)
                {
                    ThrottleCmd = OldThrottle;
                    ThrottleCmd = OldThrottle + 0.05 * dt; //IELU up
                    Ielu_intervent = true;
                }
                else
                {
                    Ielu_intervent = false;
                }
            }
            else
            {
                Ielu_intervent = false;
            }
            OldThrottle = ThrottleCmd;

            switch (phase)
            {
                case phaseType.tpOff: Eng_HP = Off(); break;
                case phaseType.tpRun: Eng_HP = Run(); break;
                case phaseType.tpSpinUp: Eng_HP = SpinUp(); break;
                case phaseType.tpStart: Eng_HP = Start(); break;
                default: Eng_HP = 0; break;
            }

            //printf ("EngHP: %lf / Requi: %lf\n",Eng_HP,Prop_Required_Power);
            return thruster.Calculate((Eng_HP * Constants.hptoftlbssec) - thruster.GetPowerRequired());
        }

        public override double CalcFuelNeed()
        {
            return fuelFlow_pph / 3600 * FDMExec.State.DeltaTime * FDMExec.Propulsion.Rate;
        }


        public override double GetPowerAvailable() { return (Eng_HP * Constants.hptoftlbssec); }
        public double GetPowerAvailable_HP() { return (Eng_HP); }
        public double GetPropRPM() { return (Prop_RPM); }
        public double GetThrottleCmd() { return (ThrottleCmd); }
        public bool GetIeluIntervent() { return Ielu_intervent; }

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

        public double ExpSeek(double var, double target, double accel_tau, double decel_tau)
        {
            // exponential delay instead of the linear delay used in Seek
            double v = var;
            if (v > target)
            {
                v = (v - target) * Math.Exp(-dt / decel_tau) + target;
            }
            else if (v < target)
            {
                v = (target - v) * (1 - Math.Exp(-dt / accel_tau)) + v;
            }
            return v;
        }

        public phaseType GetPhase() { return phase; }

        public bool GetOvertemp() { return Overtemp; }
        public bool GetFire() { return Fire; }
        public bool GetReversed() { return Reversed; }
        public bool GetCutoff() { return Cutoff; }
        public int GetIgnition() { return Ignition; }

        public double GetInlet() { return InletPosition; }
        public double GetNozzle() { return NozzlePosition; }
        public double GetN1() { return N1; }
        public double GetN2() { return N2; }
        public double GetEPR() { return EPR; }
        public double GetITT() { return Eng_ITT_degC; }
        public bool GetEngStarting() { return EngStarting; }

        public double getOilPressure_psi() { return OilPressure_psi; }
        public double getOilTemp_degF() { return Conversion.KelvinToFahrenheit(OilTemp_degK); }

        public bool GetGeneratorPower() { return GeneratorPower; }
        public int GetCondition() { return Condition; }

        public void SetIgnition(int ignition) { Ignition = ignition; }
        public void SetPhase(phaseType p) { phase = p; }
        public void SetEPR(double epr) { EPR = epr; }
        public void SetReverse(bool reversed) { Reversed = reversed; }
        public void SetCutoff(bool cutoff) { Cutoff = cutoff; }

        public void SetGeneratorPower(bool gp) { GeneratorPower = gp; }
        public void SetCondition(int c) { Condition = c; }

        public override string GetEngineLabels(string delimeter)
        {
            string buf;

            buf = Name + "_N1[" + engineNumber + "]" + delimeter
                + Name + "_N2[" + engineNumber + "]" + delimeter
                + Name + "__PwrAvailJVK[" + engineNumber + "]" + delimeter
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


        private phaseType phase;         ///< Operating mode, or "phase"
        private double MilThrust;        ///< Maximum Unaugmented Thrust, static @ S.L. (lbf)
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
        private double ThrottleCmd;      ///< FCS-supplied throttle position
        private double TAT;              ///< total air temperature (deg C)
        private bool Stalled;            ///< true if engine is compressor-stalled
        private bool Seized;             ///< true if inner spool is seized
        private bool Overtemp;           ///< true if EGT exceeds limits
        private bool Fire;               ///< true if engine fire detected
        private bool Reversed;
        private bool Cutoff;
        private int Ignition;

        private double EPR;
        private double OilPressure_psi;
        private double OilTemp_degK;
        private double InletPosition;
        private double NozzlePosition;

        private double Ielu_max_torque;      // max propeller torque (before ielu intervent)
        private bool Ielu_intervent;
        private double OldThrottle;

        private double BetaRangeThrottleEnd; // coef (0-1) where is end of beta-range
        private double ReverseMaxPower;      // coef (0-1) multiplies max throttle on reverse

        private double Idle_Max_Delay;       // time delay for exponencial
        private double MaxPower;             // max engine power [HP]
        private double StarterN1;	           // rotates of generator maked by starter [%]
        private double MaxStartingTime;	     // maximal time for start [s] (-1 means not used)
        private double Prop_RPM;             // propeller RPM
        private double Velocity;
        private double rho;
        private double PSFC;                 // Power specific fuel comsumption [lb/(HP*hr)] at best efficiency

        private double Eng_HP;               // current engine power

        private double StartTime;	           // engine strating time [s] (0 when start button pushed)

        private double ITT_Delay;	         // time delay for exponencial grow of ITT
        private double Eng_ITT_degC;
        private double Eng_Temperature;     // temperature inside engine

        private bool EngStarting;            // logicaly output - TRUE if engine is starting
        private bool GeneratorPower;
        private int Condition;

        private double Off()
        {
            double qbar = FDMExec.Auxiliary.Qbar;
            running = false; EngStarting = false;

            fuelFlow_pph = Seek(fuelFlow_pph, 0, 800.0, 800.0);

            //allow the air turn with generator
            N1 = ExpSeek(N1, qbar / 15.0, Idle_Max_Delay * 2.5, Idle_Max_Delay * 5);

            OilTemp_degK = ExpSeek(OilTemp_degK, 273.15 + TAT, 400, 400);

            Eng_Temperature = ExpSeek(Eng_Temperature, TAT, 300, 400);
            double ITT_goal = ITT_N1.GetValue(N1, 0.1) + ((N1 > 20) ? 0.0 : (20 - N1) / 20.0 * Eng_Temperature);
            Eng_ITT_degC = ExpSeek(Eng_ITT_degC, ITT_goal, ITT_Delay, ITT_Delay * 1.2);

            OilPressure_psi = (N1 / 100.0 * 0.25 + (0.1 - (OilTemp_degK - 273.15) * 0.1 / 80.0) * N1 / 100.0) / 7692.0e-6; //from MPa to psi

            ConsumeFuel(); // for possible setting Starved = false when fuel tank
            // is refilled (fuel crossfeed etc.)

            if (Prop_RPM > 5) return -0.012; // friction in engine when propeller spining (estimate)
            return 0.0;
        }

        private double Run()
        {
            //TODO check it. Not used double idlethrust, milthrust;
            double thrust = 0.0, EngPower_HP, eff_coef;
            running = true; Starter = false; EngStarting = false;

            //---
            double old_N1 = N1;
            N1 = ExpSeek(N1, IdleN1 + ThrottleCmd * N1_factor, Idle_Max_Delay, Idle_Max_Delay * 2.4);

            EngPower_HP = EnginePowerRPM_N1.GetValue(Prop_RPM, N1);
            EngPower_HP *= EnginePowerVC.GetValue();
            if (EngPower_HP > MaxPower) EngPower_HP = MaxPower;

            eff_coef = 9.333 - (N1) / 12; // 430%Fuel at 60%N1
            fuelFlow_pph = PSFC * EngPower_HP * eff_coef;

            Eng_Temperature = ExpSeek(Eng_Temperature, Eng_ITT_degC, 300, 400);
            double ITT_goal = ITT_N1.GetValue((N1 - old_N1) * 300 + N1, 1);
            Eng_ITT_degC = ExpSeek(Eng_ITT_degC, ITT_goal, ITT_Delay, ITT_Delay * 1.2);

            OilPressure_psi = (N1 / 100.0 * 0.25 + (0.1 - (OilTemp_degK - 273.15) * 0.1 / 80.0) * N1 / 100.0) / 7692.0e-6; //from MPa to psi
            //---
            EPR = 1.0 + thrust / MilThrust;

            OilTemp_degK = Seek(OilTemp_degK, 353.15, 0.4 - N1 * 0.001, 0.04);

            ConsumeFuel();

            if (Cutoff) phase = phaseType.tpOff;
            if (starved) phase = phaseType.tpOff;

            return EngPower_HP;
        }

        private double SpinUp()
        {
            double EngPower_HP;
            running = false; 
            EngStarting = true;
            fuelFlow_pph = 0.0;

            if (!GeneratorPower)
            {
                EngStarting = false;
                phase = phaseType.tpOff;
                StartTime = -1;
                return 0.0;
            }

            N1 = ExpSeek(N1, StarterN1, Idle_Max_Delay * 6, Idle_Max_Delay * 2.4);

            Eng_Temperature = ExpSeek(Eng_Temperature, TAT, 300, 400);
            double ITT_goal = ITT_N1.GetValue(N1, 0.1) + ((N1 > 20) ? 0.0 : (20 - N1) / 20.0 * Eng_Temperature);
            Eng_ITT_degC = ExpSeek(Eng_ITT_degC, ITT_goal, ITT_Delay, ITT_Delay * 1.2);

            OilTemp_degK = ExpSeek(OilTemp_degK, 273.15 + TAT, 400, 400);

            OilPressure_psi = (N1 / 100.0 * 0.25 + (0.1 - (OilTemp_degK - 273.15) * 0.1 / 80.0) * N1 / 100.0) / 7692.0e-6; //from MPa to psi
            NozzlePosition = 1.0;

            EngPower_HP = EnginePowerRPM_N1.GetValue(Prop_RPM, N1);
            EngPower_HP *= EnginePowerVC.GetValue();
            if (EngPower_HP > MaxPower) EngPower_HP = MaxPower;

            if (StartTime >= 0) StartTime += dt;
            if (StartTime > MaxStartingTime && MaxStartingTime > 0)
            { //start failed due timeout
                phase = phaseType.tpOff;
                StartTime = -1;
            }

            ConsumeFuel(); // for possible setting Starved = false when fuel tank
            // is refilled (fuel crossfeed etc.)

            return EngPower_HP;
        }

        private double Start()
        {
            double EngPower_HP = 0.0, eff_coef;
            EngStarting = false;
            if ((N1 > 15.0) && !starved)
            {       // minimum 15% N2 needed for start
                double old_N1 = N1;
                cranking = true;                   // provided for sound effects signal
                if (N1 < IdleN1)
                {
                    EngPower_HP = EnginePowerRPM_N1.GetValue(Prop_RPM, N1);
                    EngPower_HP *= EnginePowerVC.GetValue();
                    if (EngPower_HP > MaxPower) EngPower_HP = MaxPower;
                    N1 = ExpSeek(N1, IdleN1 * 1.1, Idle_Max_Delay * 4, Idle_Max_Delay * 2.4);
                    eff_coef = 9.333 - (N1) / 12; // 430%Fuel at 60%N1
                    fuelFlow_pph = PSFC * EngPower_HP * eff_coef;
                    Eng_Temperature = ExpSeek(Eng_Temperature, Eng_ITT_degC, 300, 400);
                    double ITT_goal = ITT_N1.GetValue((N1 - old_N1) * 300 + N1, 1);
                    Eng_ITT_degC = ExpSeek(Eng_ITT_degC, ITT_goal, ITT_Delay, ITT_Delay * 1.2);

                    OilPressure_psi = (N1 / 100.0 * 0.25 + (0.1 - (OilTemp_degK - 273.15) * 0.1 / 80.0) * N1 / 100.0) / 7692.0e-6; //from MPa to psi
                    OilTemp_degK = Seek(OilTemp_degK, 353.15, 0.4 - N1 * 0.001, 0.04);

                }
                else
                {
                    phase = phaseType.tpRun;
                    running = true;
                    Starter = false;
                    cranking = false;
                    fuelFlow_pph = 0;
                    EngPower_HP = 0.0;
                }
            }
            else
            {                 // no start if N2 < 15% or Starved
                phase = phaseType.tpOff;
                Starter = false;
            }

            ConsumeFuel();

            return EngPower_HP;
        }

        private void SetDefaults()
        {
            Name = "Not defined";
            N1 = N2 = 0.0;
            engineType = Engine.EngineType.Turboprop;
            MilThrust = 10000.0;
            IdleN1 = 30.0;
            IdleN2 = 60.0;
            MaxN1 = 100.0;
            MaxN2 = 100.0;
            ThrottleCmd = 0.0;
            InletPosition = 1.0;
            NozzlePosition = 1.0;
            Reversed = false;
            Cutoff = true;
            phase = phaseType.tpOff;
            Stalled = false;
            Seized = false;
            Overtemp = false;
            Fire = false;
            Eng_ITT_degC = 0.0;

            GeneratorPower = true;
            Condition = 0;
            Ielu_intervent = false;

            Idle_Max_Delay = 1.0;
        }



        private void Load(FDMExecutive exec, XmlElement element)
        {
            string property_prefix = "propulsion/engine[" + engineNumber + "]/";

            IdleFF = -1;
            MaxStartingTime = 999999; //very big timeout -> infinite
            Ielu_max_torque = -1;

            // ToDo: Need to make sure units are properly accounted for below.
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
                    else if (token.Equals("idlen1"))
                        IdleN1 = FormatHelper.ValueAsNumber(tmpElem);
                    else if (token.Equals("idlen2"))
                        IdleN2 = FormatHelper.ValueAsNumber(tmpElem);
                    else if (token.Equals("maxn1"))
                        MaxN1 = FormatHelper.ValueAsNumber(tmpElem);
                    else if (token.Equals("maxn2"))
                        MaxN2 = FormatHelper.ValueAsNumber(tmpElem);
                    else if (token.Equals("betarangeend"))
                        BetaRangeThrottleEnd = FormatHelper.ValueAsNumber(tmpElem) / 100.0;
                    else if (token.Equals("reversemaxpower"))
                        ReverseMaxPower = FormatHelper.ValueAsNumber(tmpElem) / 100.0;
                    else if (token.Equals("maxpower"))
                        MaxPower = FormatHelper.ValueAsNumber(tmpElem);
                    else if (token.Equals("idlefuelflow"))
                        IdleFF = FormatHelper.ValueAsNumber(tmpElem);
                    else if (token.Equals("psfc"))
                        PSFC = FormatHelper.ValueAsNumber(tmpElem);
                    else if (token.Equals("n1idle_max_delay"))
                        Idle_Max_Delay = FormatHelper.ValueAsNumber(tmpElem);
                    else if (token.Equals("maxstartingtime"))
                        MaxStartingTime = FormatHelper.ValueAsNumber(tmpElem);
                    else if (token.Equals("startern1"))
                        StarterN1 = FormatHelper.ValueAsNumber(tmpElem);
                    else if (token.Equals("ielumaxtorque"))
                        Ielu_max_torque = FormatHelper.ValueAsNumber(tmpElem);
                    else if (token.Equals("itt_delay"))
                        ITT_Delay = FormatHelper.ValueAsNumber(tmpElem);

                    else if (token.Equals("table"))
                    {
                        string name = tmpElem.GetAttribute("name");
                        if (name.Equals("EnginePowerVC"))
                        {
                            EnginePowerVC = new Table(exec.PropertyManager, tmpElem);
                        }
                        else if (name.Equals("EnginePowerRPM_N1"))
                        {
                            EnginePowerRPM_N1 = new Table(exec.PropertyManager, tmpElem);
                        }
                        else if (name.Equals("ITT_N1"))
                        {
                            ITT_N1 = new Table(exec.PropertyManager, tmpElem);
                        }
                        else
                        {
                            if (log.IsErrorEnabled)
                                log.Error("Unknown table type: " + name + " in turbine definition.");
                            throw new Exception("Unknown table type: " + name + " in turbine definition.");
                        }
                    }
                }
            }

            // Pre-calculations and initializations

            delay = 1;
            N1_factor = MaxN1 - IdleN1;
            N2_factor = MaxN2 - IdleN2;
            OilTemp_degK = (exec.Auxiliary.TotalTemperature - 491.69) * 0.5555556 + 273.0;
            if (IdleFF == -1) IdleFF = Math.Pow(MilThrust, 0.2) * 107.0;  // just an estimate

            if (log.IsDebugEnabled)
                log.Debug("ENG POWER:" + EnginePowerRPM_N1.GetValue(1200, 90));
        }

        private void bindmodel()
        {
            /* I dont like this. Try to change it to properties*/
            FDMExec.PropertyManager.Tie("propulsion/engine[" + engineNumber + "]/n1", this.GetN1, null);
            FDMExec.PropertyManager.Tie("propulsion/engine[" + engineNumber + "]/n2", this.GetN2, null);
            //TODO FDMExec.PropertyManager.Tie("propulsion/engine[" + engineNumber + "]/reverser", this.Reversed, null);
        }

        private void unbind()
        {
            /// TODO
        }


        private Table ITT_N1; // ITT temperature depending on throttle command
        private Table EnginePowerRPM_N1;
        private Table EnginePowerVC;
    }
}
