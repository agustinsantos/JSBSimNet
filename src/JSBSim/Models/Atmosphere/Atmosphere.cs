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
namespace JSBSim.Models
{
    using System;
    using CommonUtils.MathLib;
    using JSBSim.InputOutput;
    using JSBSim.Script;

    // Import log4net classes.
    using log4net;


    /// <summary>
    /// Models an empty, abstract base atmosphere class.
    /// 
    /// <h2> Properties </h2>
    /// @property atmosphere/T-R The current modeled temperature in degrees Rankine.
    /// @property atmosphere/rho-slugs_ft3
    /// @property atmosphere/P-psf
    /// @property atmosphere/a-fps
    /// @property atmosphere/T-sl-R
    /// @property atmosphere/rho-sl-slugs_ft3
    /// @property atmosphere/P-sl-psf
    /// @property atmosphere/a-sl-fps
    /// @property atmosphere/theta
    /// @property atmosphere/sigma
    /// @property atmosphere/delta
    /// @property atmosphere/a-ratio
    /// 
    /// This code is based on FGAtmosphere written by Tony Peden, Jon Berndt
    /// see Anderson, John D. "Introduction to Flight, Third Edition", McGraw-Hill,
    /// 1989, ISBN 0-07-001641-0
    /// </summary>
    [Serializable]
    public abstract class Atmosphere : Model
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
        /// Enums for specifying temperature units.
        /// </summary>
        public enum eTemperature { eNoTempUnit = 0, eFahrenheit, eCelsius, eRankine, eKelvin };

        /// <summary>
        /// Enums for specifying pressure units.
        /// </summary>
        public enum ePressure { eNoPressUnit = 0, ePSF, eMillibars, ePascals, eInchesHg };

        public Atmosphere(FDMExecutive exec) : base(exec)
        {
            pressureAltitude = 0.0;      // ft
            densityAltitude = 0.0;       // ft

            Name = "Atmosphere";
            Bind();
            //Debug(0);
        }

        /// <summary>
        /// Runs the Atmosphere model; called by the Executive
        /// Can pass in a value indicating if the executive is directing the simulation to Hold.
        /// </summary>
        /// <param name="Holding">
        /// if true, the executive has been directed to hold the sim from 
        /// advancing time. Some models may ignore this flag, such as the Input
        /// model, which may need to be active to listen on a socket for the
        ///              "Resume" command to be given.
        /// </param>
        /// <returns>false if no error</returns>
        public override bool Run(bool Holding)
        {
            if (base.Run(Holding)) return true;
            if (Holding) return false;

            Calculate(_in.altitudeASL);

            //Debug(2);
            return false;

        }

        public override bool InitModel()
        {
            if (!base.InitModel()) return false;

            Calculate(0.0);
            SLtemperature = temperature = StdDaySLtemperature;
            SLpressure = pressure = StdDaySLpressure;
            SLdensity = density = Pressure / (Constants.Reng * Temperature);
            SLsoundspeed = soundspeed = StdDaySLsoundspeed;

            return true;
        }

        //  *************************************************************************
        /// <summary>
        /// Returns the temperature in degrees Rankine
        /// </summary>
        [ScriptAttribute("atmosphere/T-R", "Temperature in degrees Rankine")]
        public double Temperature { get { return temperature; } }

        /// <summary>
        /// Returns the actual, modeled temperature at the current altitude in degrees Rankine.
        /// </summary>
        /// <returns>Modeled temperature in degrees Rankine.</returns>
        public virtual double GetTemperature() { return temperature; }

        /// <summary>
        /// Returns the actual modeled temperature in degrees Rankine at a specified altitude.
        /// </summary>
        /// <param name="altitude">The altitude above sea level (ASL) in feet.</param>
        /// <returns>temperature in degrees Rankine at the specified altitude.</returns>
        public abstract double GetTemperature(double altitude);

        /// <summary>
        /// Returns the sea level temperature in degrees Rankine
        /// </summary>
        [ScriptAttribute("atmosphere/T-sl-R", "Sea level temperature in degrees Rankine")]
        public double TemperatureSeaLevel { get { return SLtemperature; } }

        /// <summary>
        /// Returns the ratio of at-altitude temperature over the sea level value.
        /// </summary>
        [ScriptAttribute("atmosphere/theta-norm", "Ratio of at-altitude temperature over the sea level value")]
        public double TemperatureRatio { get { return Temperature / SLtemperature; } }

        /// <summary>
        /// Returns the ratio of the temperature as modeled at the supplied altitude
        /// over the sea level value.
        /// </summary>
        /// <param name="h"></param>
        /// <returns></returns>
        public virtual double GetTemperatureRatio(double h) { return GetTemperature(h) / SLtemperature; }

        /// Sets the Sea Level temperature.
        /// @param t the temperature value in the unit provided.
        /// @param unit the unit of the temperature.
        public virtual void SetTemperatureSL(double t, eTemperature unit = eTemperature.eFahrenheit)
        {
            SLtemperature = ConvertToRankine(t, unit);
        }

        /// Sets the temperature at the supplied altitude.
        /// @param t The temperature value in the unit provided.
        /// @param h The altitude in feet above sea level.
        /// @param unit The unit of the temperature.
        public abstract void SetTemperature(double t, double h, eTemperature unit = eTemperature.eFahrenheit);


        //  *************************************************************************
        /// <summary>
        /// Returns the pressure in psf
        /// </summary>
        [ScriptAttribute("atmosphere/P-psf", "Pressure in psf")]
        public double Pressure { get { return pressure; } }

        /// <summary>
        /// Returns the sea level pressure in psf.
        /// </summary>
        [ScriptAttribute("atmosphere/P-sl-psf", "Sea level pressure in psf")]
        public double PressureSeaLevel { get { return SLpressure; } }

        /// <summary>
        /// Returns the ratio of at-altitude pressure over the sea level value. 
        /// </summary>
        [ScriptAttribute("atmosphere/delta-norm", "Ratio of at-altitude pressure over the sea level value")]
        public double PressureRatio { get { return pressure / SLpressure; } }

        /// <summary>
        /// Pressure access functions.
        /// </summary>
        /// <returns>Returns the pressure in psf.</returns>
        public virtual double GetPressure() { return Pressure; }

        /// <summary>
        /// Returns the pressure at a specified altitude in psf.
        /// </summary>
        /// <param name="altitude"></param>
        /// <returns></returns>
        public abstract double GetPressure(double altitude);

        /// <summary>
        /// Returns the sea level pressure in target units, default in psf.
        /// </summary>
        /// <param name="to"></param>
        /// <returns></returns>
        public virtual double GetPressureSL(ePressure to = ePressure.ePSF) { return ConvertFromPSF(SLpressure, to); }

        /// Returns the ratio of at-altitude pressure over the sea level value.
        public virtual double GetPressureRatio() { return Pressure / SLpressure; }

        /// <summary>
        /// Sets the sea level pressure for modeling.
        /// </summary>
        /// <param name="unit">the unit of measure that the specified pressure is supplied in.</param>
        /// <param name="pressure">The pressure in the units specified.</param>
        public virtual void SetPressureSL(ePressure unit, double pressure)
        {
            double press = ConvertToPSF(pressure, unit);

            SLpressure = press;
        }

        //  *************************************************************************
        /// <summary>
        /// Returns the density in slugs/ft^3
        /// This function may <b>only</b> be used if Run() is called first.
        /// </summary>
        [ScriptAttribute("atmosphere/rho-slugs_ft3", "Density in slugs/ft^3")]
        public double Density { get { return density; } }

        /// <summary>
        /// Returns the sea level density in slugs/ft^3
        /// </summary>
        [ScriptAttribute("atmosphere/rho-sl-slugs_ft3", "Sea level density in slugs/ft^3")]
        public double DensitySeaLevel { get { return SLdensity; } }

        /// <summary>
        /// Returns the ratio of at-altitude density over the sea level value.
        /// </summary>
        [ScriptAttribute("atmosphere/sigma-norm", "Ratio of at-altitude density over the sea level value")]
        public double DensityRatio { get { return density / SLdensity; } }

        /// <summary>
        /// Returns the density in slugs/ft^3.
        /// This function may only be used if Run() is called first.
        /// </summary>
        /// <returns></returns>
        public virtual double GetDensity() { return Density; }

        /// <summary>
        /// Returns the density in slugs/ft^3 at a given altitude in ft.
        /// </summary>
        /// <param name="altitude"></param>
        /// <returns></returns>
        public virtual double GetDensity(double altitude)
        {
            return GetPressure(altitude) / (Constants.Reng * GetTemperature(altitude));
        }

        /// Returns the sea level density in slugs/ft^3
        public virtual double GetDensitySL() { return SLdensity; }

        /// Returns the ratio of at-altitude density over the sea level value.
        public virtual double GetDensityRatio() { return Density / SLdensity; }

        //  *************************************************************************

        /// <summary>
        /// Returns the speed of sound in ft/sec.
        /// </summary>
        [ScriptAttribute("atmosphere/a-fps", "Speed of sound in ft/sec")]
        public double SoundSpeed { get { return soundspeed; } }

        // Returns the ratio of at-altitude sound speed over the sea level value.
        [ScriptAttribute("atmosphere/a-norm", "Ratio of at-altitude sound speed over the sea level value")]
        public double SoundSpeedRatio { get { return soundspeed / SLsoundspeed; } }

        /// <summary>
        /// Returns the sea level speed of sound in ft/sec.
        /// </summary>
        [ScriptAttribute("atmosphere/a-sl-fps", "Sea level speed of sound in ft/sec")]
        public double SoundSpeedSeaLevel { get { return SLsoundspeed; } }

        /// <summary>
        /// Returns the speed of sound in ft/sec at a given altitude in ft.
        /// </summary>
        /// <param name="altitude"></param>
        /// <returns></returns>
        public virtual double GetSoundSpeed(double altitude)
        {
            return Math.Sqrt(Constants.SHRatio * Constants.Reng * GetTemperature(altitude));
        }


        /// Returns the sea level speed of sound in ft/sec.
        public virtual double GetSoundSpeedSL() { return SLsoundspeed; }

        /// Returns the ratio of at-altitude sound speed over the sea level value.
        public virtual double GetSoundSpeedRatio() { return soundspeed / SLsoundspeed; }

        //  *************************************************************************

        /// Returns the absolute viscosity.
        public virtual double GetAbsoluteViscosity() { return viscosity; }

        /// Returns the kinematic viscosity.
        public virtual double GetKinematicViscosity() { return kinematicViscosity; }
        //@}

        public virtual double GetDensityAltitude() { return densityAltitude; }

        public virtual double GetPressureAltitude() { return pressureAltitude; }

        //  *************************************************************************
#if DELETEME
        /// <summary>
        /// Tells the simulator to use an externally calculated atmosphere model.
        /// </summary>
        public virtual void UseExternal()
        {
            useInfo = externalInfo;
            useExternal = true;
        }

        /// <summary>
        /// Tells the simulator to use the internal atmosphere model.
        /// This is the default
        /// </summary>
        public virtual void UseInternal()
        {
            useInfo = internalInfo;
            useExternal = false;
        }

        /// <sumary>
        /// Gets the boolean that tells if the external atmosphere model is being used.
        /// </sumary>
        public bool External() { return useExternal; }

        ///<summary>
        /// Provides the external atmosphere model with an interface to set the temperature.
        ///</summary>
        public void SetExTemperature(double t) { externalInfo.Temperature = t; }

        ///<summary>
        /// Provides the external atmosphere model with an interface to set the density.
        ///</summary>
        public void SetExDensity(double d) { externalInfo.Density = d; }

        ///<summary>
        /// Provides the external atmosphere model with an interface to set the pressure.
        ///</summary>
        public void SetExPressure(double p) { externalInfo.Pressure = p; }







        ///<summary>
        /// Gets the at-altitude temperature deviation in degrees Fahrenheit
        ///</summary>
        public double GetTempDev() { return T_dev; }

        ///<summary>
        /// Gets the density altitude in feet
        /// </summary>
        [ScriptAttribute("atmosphere/density-altitude", "Delta-T in degrees Fahrenheit")]
        public double DensityAltitude { get { return density_altitude; } }

        ///<summary>
        /// Sets the wind components in NED frame.
        ///</summary>
        public void SetWindNED(double wN, double wE, double wD) { vWindNED.X = wN; vWindNED.Y = wE; vWindNED.Z = wD; }

        ///<summary>
        /// Retrieves the wind components in NED frame.
        ///</summary>
        public Vector3D GetWindNED() { return vWindNED; }

        /// <summary>
        /// Retrieves the wind direction. The direction is defined as north=0 and
        /// increases counterclockwise. The wind heading is returned in radians
        /// </summary>
        /// <returns></returns>
        [ScriptAttribute("atmosphere/psiw-rad", "Retrieves the wind direction")]
        public double WindPsi { get { return psiw; } }

        public void SetTurbGain(double tt) { TurbGain = tt; }
        public void SetTurbRate(double tt) { TurbRate = tt; }

        public double GetTurbPQR(int idx) { return vTurbPQR[idx]; }


        [ScriptAttribute("atmosphere/p-turb-rad_sec", "p-turb-rad_sec")]
        public Vector3D TurbPQR { get { return vTurbPQR; } }

        public enum TurbType { ttStandard, ttBerndt, ttNone };

        protected double rho;

        protected TurbType turbType;

        protected int lastIndex;
        protected double h;
        protected static double[] htab = new double[8] {0,
                                    36089.239,
                                    65616.798,
                                    104986.878,
                                    154199.475,
                                    170603.675,
                                    200131.234,
                                    259186.352}; //lt
        protected double StdSLtemperature, StdSLdensity, StdSLpressure, StdSLsoundspeed;
        protected double rSLtemperature, rSLdensity, rSLpressure, rSLsoundspeed; //reciprocals

        protected AtmosphereInformation internalInfo = new AtmosphereInformation();
        protected AtmosphereInformation externalInfo;
        protected AtmosphereInformation useInfo;

        protected bool useExternal;

        protected double T_dev_sl, T_dev, delta_T, density_altitude;

        protected AtmosphereInformation atmosphere = new AtmosphereInformation();
        protected bool StandardTempOnly;

        protected double MagnitudedAccelDt, MagnitudeAccel, Magnitude;
        protected double TurbGain;
        protected double TurbRate;
        protected Vector3D vDirectiondAccelDt;
        protected Vector3D vDirectionAccel;
        protected Vector3D vDirection;
        protected Vector3D vTurbulence;
        protected Vector3D vTurbulenceGrad;
        protected Vector3D vBodyTurbGrad;
        protected Vector3D vTurbPQR;

        protected Vector3D vWindNED;
        protected double psiw;

        private Random rand = new Random();



        protected void CalculateDerived()
        {
            T_dev = (useInfo.Temperature) - GetTemperature(h);
            density_altitude = h + T_dev * 66.7;

            if (turbType != TurbType.ttStandard)
            {
                Turbulence();
                vWindNED += vTurbulence;
            }

            if (vWindNED[0] != 0.0)
                psiw = Math.Atan2(vWindNED[1], vWindNED[0]);

            if (psiw < 0) psiw += 2 * Math.PI;

            soundspeed = Math.Sqrt(Constants.SHRatio * Constants.Reng * (useInfo.Temperature));
        }

        //%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
        // Get the standard atmospheric properties at a specified altitude

        protected void GetStdAtmosphere(double altitude)
        {
            StandardTempOnly = true;
            Calculate(altitude);
            StandardTempOnly = false;
            atmosphere.Temperature = internalInfo.Temperature;
            atmosphere.Pressure = internalInfo.Pressure;
            atmosphere.Density = internalInfo.Density;

            // Reset the internal atmospheric state
            Calculate(h);
        }

        //%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
        // 

        /// <summary>
        /// Returns the pressure at an arbitrary altitude in psf
        /// </summary>
        /// <param name="alt">the altitude</param>
        /// <returns>the standard pressure at a specified altitude in psf</returns>
        public double GetPressure(double altitude)
        {
            GetStdAtmosphere(altitude);
            return atmosphere.Pressure;
        }


        /// <summary>
        /// Get the standard density at an arbitrary altitude
        /// </summary>
        /// <param name="alt">the altitude</param>
        /// <returns>the standard density at a specified altitude</returns>
        public double GetDensity(double altitude)
        {
            GetStdAtmosphere(altitude);
            return atmosphere.Density;
        }

        // square a value, but preserve the original sign
        protected static double square_signed(double value)
        {
            if (value < 0)
                return value * value * -1;
            else
                return value * value;
        }

        protected void Turbulence()
        {
            switch (turbType)
            {
                case TurbType.ttStandard:
                    {
                        vDirectiondAccelDt.X = 1 - 2.0 * rand.NextDouble();
                        vDirectiondAccelDt.Y = 1 - 2.0 * rand.NextDouble();
                        vDirectiondAccelDt.Z = 1 - 2.0 * rand.NextDouble();

                        MagnitudedAccelDt = 1 - 2.0 * rand.NextDouble() - Magnitude;
                        // Scale the magnitude so that it moves
                        // away from the peaks
                        MagnitudedAccelDt = ((MagnitudedAccelDt - Magnitude) /
                            (1 + Math.Abs(Magnitude)));
                        MagnitudeAccel += MagnitudedAccelDt * rate * TurbRate * FDMExec.State.DeltaTime;
                        Magnitude += MagnitudeAccel * rate * FDMExec.State.DeltaTime;

                        vDirectiondAccelDt.Normalize();

                        // deemphasise non-vertical forces
                        vDirectiondAccelDt.X = square_signed(vDirectiondAccelDt.X);
                        vDirectiondAccelDt.Y = square_signed(vDirectiondAccelDt.Y);

                        vDirectionAccel += vDirectiondAccelDt * rate * TurbRate * FDMExec.State.DeltaTime;
                        vDirectionAccel.Normalize();
                        vDirection += vDirectionAccel * rate * FDMExec.State.DeltaTime;

                        vDirection.Normalize();

                        // Diminish turbulence within three wingspans
                        // of the ground
                        vTurbulence = TurbGain * Magnitude * vDirection;
                        double HOverBMAC = FDMExec.Auxiliary.HOverBMAC;
                        if (HOverBMAC < 3.0)
                            vTurbulence *= (HOverBMAC / 3.0) * (HOverBMAC / 3.0);

                        vTurbulenceGrad = TurbGain * MagnitudeAccel * vDirection;

                        vBodyTurbGrad = FDMExec.Propagate.GetTl2b() * vTurbulenceGrad;

                        if (FDMExec.Aircraft.WingSpan > 0)
                        {
                            vTurbPQR.eP = vBodyTurbGrad.Y / FDMExec.Aircraft.WingSpan;
                        }
                        else
                        {
                            vTurbPQR.eP = vBodyTurbGrad.Y / 30.0;
                        }
                        //     if (Aircraft.GetHTailArm() != 0.0)
                        //       vTurbPQR(eQ) = vBodyTurbGrad(eZ)/Aircraft.GetHTailArm();
                        //     else
                        //       vTurbPQR(eQ) = vBodyTurbGrad(eZ)/10.0;

                        if (FDMExec.Aircraft.VTailArm > 0)
                            vTurbPQR.eR = vBodyTurbGrad.X / FDMExec.Aircraft.VTailArm;
                        else
                            vTurbPQR.eR = vBodyTurbGrad.X / 10.0;

                        // Clear the horizontal forces
                        // actually felt by the plane, now
                        // that we've used them to calculate
                        // moments.
                        vTurbulence.X = 0.0;
                        vTurbulence.Y = 0.0;

                        break;
                    }
                case TurbType.ttBerndt:
                    {
                        vDirectiondAccelDt.X = 1 - 2.0 * rand.NextDouble();
                        vDirectiondAccelDt.Y = 1 - 2.0 * rand.NextDouble();
                        vDirectiondAccelDt.Z = 1 - 2.0 * rand.NextDouble();


                        MagnitudedAccelDt = 1 - 2.0 * rand.NextDouble() - Magnitude;
                        MagnitudeAccel += MagnitudedAccelDt * rate * FDMExec.State.DeltaTime;
                        Magnitude += MagnitudeAccel * rate * FDMExec.State.DeltaTime;

                        vDirectiondAccelDt.Normalize();
                        vDirectionAccel += vDirectiondAccelDt * rate * FDMExec.State.DeltaTime;
                        vDirectionAccel.Normalize();
                        vDirection += vDirectionAccel * rate * FDMExec.State.DeltaTime;

                        // Diminish z-vector within two wingspans
                        // of the ground
                        double HOverBMAC = FDMExec.Auxiliary.HOverBMAC;
                        if (HOverBMAC < 2.0)
                            vDirection.Z *= HOverBMAC / 2.0;

                        vDirection.Normalize();

                        vTurbulence = TurbGain * Magnitude * vDirection;
                        vTurbulenceGrad = TurbGain * MagnitudeAccel * vDirection;

                        vBodyTurbGrad = FDMExec.Propagate.GetTl2b() * vTurbulenceGrad;
                        vTurbPQR.eP = vBodyTurbGrad.Y / FDMExec.Aircraft.WingSpan;
                        if (FDMExec.Aircraft.HTailArm > 0)
                            vTurbPQR.eQ = vBodyTurbGrad.Z / FDMExec.Aircraft.HTailArm;
                        else
                            vTurbPQR.eQ = vBodyTurbGrad.Z / 10.0;

                        if (FDMExec.Aircraft.VTailArm > 0)
                            vTurbPQR.eQ = vBodyTurbGrad.X / FDMExec.Aircraft.VTailArm;
                        else
                            vTurbPQR.eQ = vBodyTurbGrad.X / 10.0;

                        break;
                    }
                default:
                    break;
            }
        }
#endif

        protected struct Inputs
        {
            public double altitudeASL;
        }

        protected Inputs _in;

        public static readonly double StdDaySLtemperature = Constants.StdDaySLtemperature;
        public static double StdDaySLpressure = Constants.StdDaySLpressure;
        public const double StdDaySLsoundspeed = Constants.StdDaySLpressure;

        protected double SLtemperature, SLdensity, SLpressure, SLsoundspeed; // Sea level conditions
        protected double temperature, density, pressure, soundspeed; // Current actual conditions at altitude

        protected double pressureAltitude;
        protected double densityAltitude;

        protected const double SutherlandConstant = 198.72;  // deg Rankine
        protected const double Beta = 2.269690E-08; // slug/(sec ft R^0.5)
        protected double viscosity, kinematicViscosity;
        protected double Reng = Constants.Reng;

        //  Universal gas constant - ft*lbf/R/mol
        /// <summary>
        /// 
        /// </summary>
        protected static readonly double Rstar = 8.31432 * Constants.kgtoslug / Conversion.KelvinToRankine(Constants.fttom * Constants.fttom);


        protected virtual void Calculate(double altitude)
        {
            PropertyNode node = propertyManager.GetNode();
            if (!propertyManager.HasNode("atmosphere/override/temperature"))
                temperature = GetTemperature(altitude);
            else
                temperature = node.GetDouble("atmosphere/override/temperature");

            if (!propertyManager.HasNode("atmosphere/override/pressure"))
                pressure = GetPressure(altitude);
            else
                pressure = node.GetDouble("atmosphere/override/pressure");

            if (!propertyManager.HasNode("atmosphere/override/density"))
                density = GetDensity(altitude);
            else
                density = node.GetDouble("atmosphere/override/density");

            soundspeed = Math.Sqrt(Constants.SHRatio * Constants.Reng * Temperature);
            pressureAltitude = CalculatePressureAltitude(Pressure, altitude);
            densityAltitude = CalculateDensityAltitude(Density, altitude);

            viscosity = Beta * Math.Pow(Temperature, 1.5) / (SutherlandConstant + Temperature);
            kinematicViscosity = viscosity / Density;
        }

        /// Calculates the density altitude given any temperature or pressure bias.
        /// Calculated density for the specified geometric altitude given any temperature
        /// or pressure biases is passed in.
        /// @param density
        /// @param geometricAlt
        protected virtual double CalculateDensityAltitude(double density, double geometricAlt) { return geometricAlt; }

        /// Calculates the pressure altitude given any temperature or pressure bias.
        /// Calculated pressure for the specified geometric altitude given any temperature
        /// or pressure biases is passed in.
        /// @param pressure
        /// @param geometricAlt
        protected virtual double CalculatePressureAltitude(double pressure, double geometricAlt) { return geometricAlt; }

        /// Converts to Rankine from one of several unit systems.
        protected double ConvertToRankine(double t, eTemperature unit)
        {
            double targetTemp = 0; // in degrees Rankine

            switch (unit)
            {
                case eTemperature.eFahrenheit:
                    targetTemp = t + 459.67;
                    break;
                case eTemperature.eCelsius:
                    targetTemp = (t + 273.15) * 1.8;
                    break;
                case eTemperature.eRankine:
                    targetTemp = t;
                    break;
                case eTemperature.eKelvin:
                    targetTemp = t * 1.8;
                    break;
                default:
                    break;
            }

            return targetTemp;
        }

        /// Converts from Rankine to one of several unit systems.
        protected double ConvertFromRankine(double t, eTemperature unit)
        {
            double targetTemp = 0;

            switch (unit)
            {
                case eTemperature.eFahrenheit:
                    targetTemp = t - 459.67;
                    break;
                case eTemperature.eCelsius:
                    targetTemp = t / 1.8 - 273.15;
                    break;
                case eTemperature.eRankine:
                    targetTemp = t;
                    break;
                case eTemperature.eKelvin:
                    targetTemp = t / 1.8;
                    break;
                default:
                    break;
            }

            return targetTemp;
        }

        /// Converts to PSF (pounds per square foot) from one of several unit systems.
        protected double ConvertToPSF(double p, ePressure unit = ePressure.ePSF)
        {
            double targetPressure = 0; // Pressure in PSF

            switch (unit)
            {
                case ePressure.ePSF:
                    targetPressure = p;
                    break;
                case ePressure.eMillibars:
                    targetPressure = p * 2.08854342;
                    break;
                case ePressure.ePascals:
                    targetPressure = p * 0.0208854342;
                    break;
                case ePressure.eInchesHg:
                    targetPressure = p * 70.7180803;
                    break;
                default:
                    throw new Exception("Undefined pressure unit given");
            }

            return targetPressure;
        }

        /// Converts from PSF (pounds per square foot) to one of several unit systems.
        protected double ConvertFromPSF(double p, ePressure unit = ePressure.ePSF)
        {
            double targetPressure = 0; // Pressure

            switch (unit)
            {
                case ePressure.ePSF:
                    targetPressure = p;
                    break;
                case ePressure.eMillibars:
                    targetPressure = p / 2.08854342;
                    break;
                case ePressure.ePascals:
                    targetPressure = p / 0.0208854342;
                    break;
                case ePressure.eInchesHg:
                    targetPressure = p / 70.7180803;
                    break;
                default:
                    throw new Exception("Undefined pressure unit given");
            }

            return targetPressure;
        }
    }
}
