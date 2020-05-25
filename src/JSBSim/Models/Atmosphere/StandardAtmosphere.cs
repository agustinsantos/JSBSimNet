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
    using System.Collections.Generic;
    using CommonUtils.MathLib;
    using JSBSim.InputOutput;
    using JSBSim.MathValues;
    using JSBSim.Script;

    // Import log4net classes.
    using log4net;

    /// <summary>
    /// Models the 1976 U.S. Standard Atmosphere, with the ability to modify the 
    /// temperature and pressure.A base feature of the model is the temperature
    /// profile that is stored as an FGTable object with this data:
    /// 
    /// @code
    /// GeoMet Alt Temp      GeoPot Alt  GeoMet Alt
    ///    (ft)      (deg R)      (km)        (km)
    ///  ---------  --------    ----------  ----------
    ///        0.0    518.67 //    0.000       0.000
    ///    36151.6    390.0  //   11.000      11.019
    ///    65823.5    390.0  //   20.000      20.063
    ///   105518.4    411.6  //   32.000      32.162
    ///   155347.8    487.2  //   47.000      47.350
    ///   168677.8    487.2  //   51.000      51.413
    ///   235570.9    386.4  //   71.000      71.802
    ///   282152.2    336.5; //   84.852      86.000
    /// @endcode
    /// 
    /// The pressure is calculated at lower altitudes through the use of two equations
    /// that are presented in the U.S.Standard Atmosphere document (see references).
    /// Density, kinematic viscosity, speed of sound, etc., are all calculated based
    /// on various constants and temperature and pressure.At higher altitudes (above
    /// 86 km (282152 ft) a different and more complicated method of calculating
    /// pressure is used.
    /// 
    /// The temperature may be modified through the use of several methods. Ultimately,
    /// these access methods allow the user to modify the sea level standard
    /// temperature, and/or the sea level standard pressure, so that the entire profile
    /// will be consistently and accurately calculated.
    /// 
    ///   <h2> Properties </h2>
    ///   @property atmosphere/delta-T
    ///   @property atmosphere/T-sl-dev-F
    /// 
    ///   @author Jon Berndt
    ///   @see "U.S. Standard Atmosphere, 1976", NASA TM-X-74335
    /// </summary>
    public class StandardAtmosphere : Atmosphere
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fdmex"></param>
        public StandardAtmosphere(FDMExecutive fdmex) : base(fdmex)
        {
            StdSLpressure = StdDaySLpressure; temperatureBias = 0.0;
            temperatureDeltaGradient = 0.0; VaporMassFraction = 0.0;
            saturatedVaporPressure = 0.0;

            Name = "StandardAtmosphere";

            // This is the U.S. Standard Atmosphere table for temperature in degrees
            // Rankine, based on geometric altitude. The table values are often given
            // in literature relative to geopotential altitude. 
            //
            double[,] stdAtmos = new double[,] {
            //  GeoPot Alt    Temp       GeoPot Alt  GeoMet Alt
            //  (ft)      (deg R)        (km)        (km)
            //  -----------   --------     ----------  ----------
                { 0.0000 , 518.67 },    //    0.000       0.000
                { 36089.2388 , 389.97}, //   11.000      11.019
                { 65616.7979 , 389.97}, //   20.000      20.063
                { 104986.8766 , 411.57},//   32.000      32.162
                { 154199.4751 , 487.17},//   47.000      47.350
                { 167322.8346 , 487.17},//   51.000      51.413
                { 232939.6325 , 386.37},//   71.000      71.802
                { 278385.8268 , 336.5028},// 84.852      86.000
                { 298556.4304 , 336.5028}}; //           91.000 - First layer in high altitude regime
            StdAtmosTemperatureTable = new Table(stdAtmos);

            // This is the maximum water vapor mass fraction in ppm (parts per million) of
            // dry air measured in the atmosphere according to the ISA 1976 document.
            // Values at altitude below 8 km are record high. All other values are 1%
            // high.
            double[,] maxVapor = new double[,] {
            //  Geopot Alt    Water     Geopot Alt
            //  (ft)       (ppm)        (km)
            //  ----------    -----     ----------
                { 0.0000 , 35000.0 },    //  0.0000 - Record high
                { 3280.8399 , 31000.0 }, //  1.0000
                { 6561.6798 , 28000.0 }, //  2.0000
                { 13123.3596 , 22000.0 },//  4.0000
                { 19685.0394 , 8900.0 }, //  6.0000
                { 26246.7192 , 4700.0 }, //  8.0000 - Record high
                { 32808.3990 , 1300.0 }, // 10.0000 - 1% high
                { 39370.0787 , 230.0 },  // 12.0000
                { 45931.7585 , 48.0 },   // 14.0000
                { 52493.4383 , 38.0} };  // 16.0000 - 1% high
            MaxVaporMassFraction = new Table(maxVapor);
            int numRows = StdAtmosTemperatureTable.GetNumRows();

            // Initialize the standard atmosphere lapse rates.
            CalculateLapseRates();
            StdLapseRates = LapseRates;

            // Assume the altitude to fade out the gradient at is at the highest
            // altitude in the table. Above that, other functions are used to
            // calculate temperature.
            gradientFadeoutAltitude = StdAtmosTemperatureTable.GetElement(numRows, 0);

            // Initialize the standard atmosphere pressure break points.
            PressureBreakpoints = new List<double>(new double[numRows]);
            CalculatePressureBreakpoints(StdSLpressure);
            StdPressureBreakpoints = PressureBreakpoints;

            StdSLtemperature = StdAtmosTemperatureTable.GetElement(1, 1);
            StdSLdensity = StdSLpressure / (Rdry * StdSLtemperature);

            CalculateStdDensityBreakpoints();
            StdSLsoundspeed = Math.Sqrt(Constants.SHRatio * Rdry * StdSLtemperature);

            Bind();
            //Debug(0);
        }

        /// Destructor
        //virtual ~FGStandardAtmosphere();

        public override bool InitModel()
        {

            // Assume the altitude to fade out the gradient at is at the highest
            // altitude in the table. Above that, other functions are used to
            // calculate temperature.
            gradientFadeoutAltitude = StdAtmosTemperatureTable.GetElement(StdAtmosTemperatureTable.GetNumRows(), 0);

            temperatureDeltaGradient = 0.0;
            temperatureBias = 0.0;
            LapseRates = StdLapseRates;

            PressureBreakpoints = StdPressureBreakpoints;

            SLpressure = StdSLpressure;
            SLtemperature = StdSLtemperature;
            SLdensity = StdSLdensity;
            SLsoundspeed = StdSLsoundspeed;

            Calculate(0.0);

            //  PrintStandardAtmosphereTable();

            return true;
        }

        //  *************************************************************************
        /// @name Temperature access functions.
        /// There are several ways to get the temperature, and several modeled
        /// temperature values that can be retrieved. The U.S. Standard Atmosphere
        /// temperature either at a specified altitude, or at sea level can be
        /// retrieved. These two temperatures do NOT include the effects of any bias
        /// or delta gradient that may have been supplied by the user. The modeled
        /// temperature and the modeled temperature at sea level can also be
        /// retrieved. These two temperatures DO include the effects of an optionally
        /// user-supplied bias or delta gradient.
        // @{
        /// Returns the actual modeled temperature in degrees Rankine at a specified
        /// altitude.
        /// @param altitude The altitude above sea level (ASL) in feet.
        /// @return Modeled temperature in degrees Rankine at the specified altitude.
        public override double GetTemperature(double altitude)
        {
            double GeoPotAlt = GeopotentialAltitude(altitude);

            double T;

            if (GeoPotAlt >= 0.0)
            {
                T = StdAtmosTemperatureTable.GetValue(GeoPotAlt);

                if (GeoPotAlt <= gradientFadeoutAltitude)
                    T -= temperatureDeltaGradient * GeoPotAlt;
            }
            else
            {
                // We don't need to add TemperatureDeltaGradient*GeoPotAlt here because
                // the lapse rate vector already accounts for the temperature gradient.
                T = StdAtmosTemperatureTable.GetValue(0.0) + GeoPotAlt * LapseRates[0];
            }

            T += temperatureBias;

            if (GeoPotAlt <= gradientFadeoutAltitude)
                T += temperatureDeltaGradient * gradientFadeoutAltitude;

            return T;
        }

        /// Returns the standard temperature in degrees Rankine at a specified
        /// altitude.
        /// @param altitude The altitude in feet above sea level (ASL) to get the
        ///                 temperature at.
        /// @return The STANDARD temperature in degrees Rankine at the specified
        ///         altitude.
        public virtual double GetStdTemperature(double altitude)
        {
            double GeoPotAlt = GeopotentialAltitude(altitude);

            if (GeoPotAlt >= 0.0)
                return StdAtmosTemperatureTable.GetValue(GeoPotAlt);
            else
                return StdAtmosTemperatureTable.GetValue(0.0) + GeoPotAlt * LapseRates[0];
        }


        /// Returns the standard sea level temperature in degrees Rankine.
        /// @return The STANDARD temperature at sea level in degrees Rankine.
        public virtual double GetStdTemperatureSL() { return StdSLtemperature; }

        /// Returns the ratio of the standard temperature at the supplied altitude 
        /// over the standard sea level temperature.
        public virtual double GetStdTemperatureRatio(double h) { return GetStdTemperature(h) / StdSLtemperature; }

        /// Returns the temperature bias over the sea level value in degrees Rankine.
        public virtual double GetTemperatureBias(eTemperature to)
        {
            if (to == eTemperature.eCelsius || to == eTemperature.eKelvin)
                return temperatureBias / 1.80;
            else return temperatureBias;
        }

        /// Returns the temperature gradient to be applied on top of the standard
        /// temperature gradient.
        public virtual double GetTemperatureDeltaGradient(eTemperature to)
        {
            if (to == eTemperature.eCelsius || to == eTemperature.eKelvin)
                return temperatureDeltaGradient / 1.80;
            else return temperatureDeltaGradient;
        }

        /// Sets the Sea Level temperature, if it is to be different than the
        /// standard.
        /// This function will calculate a bias - a difference - from the standard
        /// atmosphere temperature and will apply that bias to the entire
        /// temperature profile. This is one way to set the temperature bias. Using
        /// the SetTemperatureBias function will replace the value calculated by
        /// this function.
        /// @param t the temperature value in the unit provided.
        /// @param unit the unit of the temperature.
        public override void SetTemperatureSL(double t, eTemperature unit = eTemperature.eFahrenheit)
        {
            SetTemperature(t, 0.0, unit);
        }

        /// Sets the temperature at the supplied altitude, if it is to be different
        /// than the standard temperature.
        /// This function will calculate a bias - a difference - from the standard
        /// atmosphere temperature at the supplied altitude and will apply that
        /// calculated bias to the entire temperature profile. If a graded delta is
        /// present, that will be included in the calculation - that is, regardless
        /// of any graded delta present, a temperature bias will be determined so that
        /// the temperature requested in this function call will be reached.
        /// @param t The temperature value in the unit provided.
        /// @param h The altitude in feet above sea level.
        /// @param unit The unit of the temperature.
        public override void SetTemperature(double t, double h, eTemperature unit = eTemperature.eFahrenheit)
        {
            double targetTemp = ConvertToRankine(t, unit);
            double GeoPotAlt = GeopotentialAltitude(h);

            temperatureBias = targetTemp - GetStdTemperature(h);

            if (GeoPotAlt <= gradientFadeoutAltitude)
                temperatureBias -= temperatureDeltaGradient * (gradientFadeoutAltitude - GeoPotAlt);

            CalculatePressureBreakpoints(SLpressure);

            SLtemperature = GetTemperature(0.0);
            CalculateSLSoundSpeedAndDensity();
        }

        /// Sets the temperature bias to be added to the standard temperature at all
        /// altitudes.
        /// This function sets the bias - the difference - from the standard
        /// atmosphere temperature. This bias applies to the entire
        /// temperature profile. Another way to set the temperature bias is to use the
        /// SetSLTemperature function, which replaces the value calculated by
        /// this function with a calculated bias.
        /// @param t the temperature value in the unit provided.
        /// @param unit the unit of the temperature.
        public virtual void SetTemperatureBias(eTemperature unit, double t)
        {
            if (unit == eTemperature.eCelsius || unit == eTemperature.eKelvin)
                t *= 1.80; // If temp delta "t" is given in metric, scale up to English

            temperatureBias = t;
            CalculatePressureBreakpoints(SLpressure);

            SLtemperature = GetTemperature(0.0);
            CalculateSLSoundSpeedAndDensity();
        }


        /// Sets a Sea Level temperature delta that is ramped out by 86 km.
        /// The value of the delta is used to calculate a delta gradient that is
        /// applied to the temperature at all altitudes below 86 km (282152 ft). 
        /// For instance, if a temperature of 20 degrees F is supplied, the delta
        /// gradient would be 20/282152 - or, about 7.09E-5 degrees/ft. At sea level,
        /// the full 20 degrees would be added to the standard temperature,
        /// but that 20 degree delta would be reduced by 7.09E-5 degrees for every
        /// foot of altitude above sea level, so that by 86 km, there would be no
        /// further delta added to the standard temperature.
        /// The graded delta can be used along with the a bias to tailor the
        /// temperature profile as desired.
        /// @param t the sea level temperature delta value in the unit provided.
        /// @param unit the unit of the temperature.
        public virtual void SetSLTemperatureGradedDelta(eTemperature unit, double deltemp)
        {
            SetTemperatureGradedDelta(deltemp, 0.0, unit);
        }

        /// Sets the temperature delta value at the supplied altitude/elevation above
        /// sea level, to be added to the standard temperature and ramped out by
        /// 86 km.
        /// This function computes the sea level delta from the standard atmosphere
        /// temperature at sea level.
        /// @param t the temperature skew value in the unit provided.
        /// @param unit the unit of the temperature.
        public virtual void SetTemperatureGradedDelta(double deltemp, double h, eTemperature unit = eTemperature.eFahrenheit)
        {
            if (unit == eTemperature.eCelsius || unit == eTemperature.eKelvin)
                deltemp *= 1.80; // If temp delta "t" is given in metric, scale up to English

            temperatureDeltaGradient = deltemp / (gradientFadeoutAltitude - GeopotentialAltitude(h));
            CalculateLapseRates();
            CalculatePressureBreakpoints(SLpressure);

            SLtemperature = GetTemperature(0.0);
            CalculateSLSoundSpeedAndDensity();
        }

        /// This function resets the model to apply no bias or delta gradient to the
        /// temperature.
        /// The delta gradient and bias values are reset to 0.0, and the standard
        /// temperature is used for the entire temperature profile at all altitudes.
        public virtual void ResetSLTemperature()
        {
            temperatureBias = temperatureDeltaGradient = 0.0;
            CalculateLapseRates();
            CalculatePressureBreakpoints(SLpressure);

            SLtemperature = StdSLtemperature;
            CalculateSLSoundSpeedAndDensity();
        }
        //@}

        //  *************************************************************************
        /// @name Pressure access functions.
        //@{
        /// Returns the pressure at a specified altitude in psf.
        public override double GetPressure(double altitude)
        {
            double GeoPotAlt = GeopotentialAltitude(altitude);

            // Iterate through the altitudes to find the current Base Altitude
            // in the table. That is, if the current altitude (the argument passed in)
            // is 20000 ft, then the base altitude from the table is 0.0. If the
            // passed-in altitude is 40000 ft, the base altitude is 36089.2388 ft (and
            // the index "b" is 2 - the second entry in the table).
            double BaseAlt = StdAtmosTemperatureTable.GetElement(1, 0);
            int numRows = StdAtmosTemperatureTable.GetNumRows();
            int b;

            for (b = 0; b < numRows - 2; ++b)
            {
                double testAlt = StdAtmosTemperatureTable.GetElement(b + 2, 0);
                if (GeoPotAlt < testAlt)
                    break;
                BaseAlt = testAlt;
            }

            double Tmb = GetTemperature(GeometricAltitude(BaseAlt));
            double deltaH = GeoPotAlt - BaseAlt;
            double Lmb = LapseRates[b];

            if (Lmb != 0.0)
            {
                double Exp = Constants.g0 / (Rdry * Lmb);
                double factor = Tmb / (Tmb + Lmb * deltaH);
                return PressureBreakpoints[b] * Math.Pow(factor, Exp);
            }
            else
                return PressureBreakpoints[b] * Math.Exp(-Constants.g0 * deltaH / (Rdry * Tmb));
        }


        /// Returns the standard pressure at the specified altitude.
        public virtual double GetStdPressure(double altitude)
        {
            double GeoPotAlt = GeopotentialAltitude(altitude);

            // Iterate through the altitudes to find the current Base Altitude
            // in the table. That is, if the current altitude (the argument passed in)
            // is 20000 ft, then the base altitude from the table is 0.0. If the
            // passed-in altitude is 40000 ft, the base altitude is 36089.2388 ft (and
            // the index "b" is 2 - the second entry in the table).
            double BaseAlt = StdAtmosTemperatureTable.GetElement(1, 0);
            int numRows = StdAtmosTemperatureTable.GetNumRows();
            int b;

            for (b = 0; b < numRows - 2; ++b)
            {
                double testAlt = StdAtmosTemperatureTable.GetElement(b + 2, 0);
                if (GeoPotAlt < testAlt)
                    break;
                BaseAlt = testAlt;
            }

            double Tmb = GetStdTemperature(GeometricAltitude(BaseAlt));
            double deltaH = GeoPotAlt - BaseAlt;
            double Lmb = LapseRates[b];

            if (Lmb != 0.0)
            {
                double Exp = Constants.g0 / (Rdry * Lmb);
                double factor = Tmb / (Tmb + Lmb * deltaH);
                return StdPressureBreakpoints[b] * Math.Pow(factor, Exp);
            }
            else
                return StdPressureBreakpoints[b] * Math.Exp(-Constants.g0 * deltaH / (Rdry * Tmb));
        }

        /** Sets the sea level pressure for modeling an off-standard pressure
            profile. This could be useful in the case where the pressure at an
            airfield is known or set for a particular simulation run.
            @param pressure The pressure in the units specified.
            @param unit the unit of measure that the specified pressure is
                             supplied in.*/
        public override void SetPressureSL(ePressure unit, double pressure)
        {
            SLpressure = ConvertToPSF(pressure, unit);
            CalculateSLDensity();
            CalculatePressureBreakpoints(SLpressure);
        }

        /// <summary>
        /// Resets the sea level to the Standard sea level pressure, and recalculates
        /// dependent parameters so that the pressure calculations are standard.
        /// </summary>
        public virtual void ResetSLPressure()
        {
            SLpressure = StdSLpressure;
            CalculateSLDensity();
            CalculatePressureBreakpoints(StdSLpressure);
        }
        //@}

        //  *************************************************************************
        /// @name Density access functions.
        //@{
        /// Returns the standard density at a specified altitude
        public virtual double GetStdDensity(double altitude)
        {
            return GetStdPressure(altitude) / (Rdry * GetStdTemperature(altitude));
        }

        //@}

        //  *************************************************************************
        ///@name Humidity access functions
        //@{
        /** Sets the dew point.
            @param dewpoint The dew point in the units specified
            @param unit The unit of measure that the specified dew point is supplied
                        in. */
        public void SetDewPoint(eTemperature unit, double dewpoint)
        {
            double altitude = CalculatePressureAltitude(Pressure, 0.0);
            double VaporPressure = CalculateVaporPressure(ConvertToRankine(dewpoint, unit));
            VaporMassFraction = Rdry * VaporPressure / (Rwater * (Pressure - VaporPressure));
            ValidateVaporMassFraction(altitude);
        }

        /** Returns the dew point.
            @param to The unit of measure that the dew point should be supplied in. */
        public double GetDewPoint(eTemperature to)
        {
            double dewpoint_degC;
            double VaporPressure = Pressure * VaporMassFraction / (VaporMassFraction + Rdry / Rwater);

            if (VaporPressure <= 0.0)
                dewpoint_degC = -c;
            else
            {
                double x = Math.Log(VaporPressure / a);
                dewpoint_degC = c * x / (b - x);
            }

            return ConvertFromRankine(1.8 * (dewpoint_degC + 273.15), to);
        }

        /** Sets the partial pressure of water vapor.
            @param Pv The vapor pressure in the units specified
            @param unit The unit of measure that the specified vapor pressure is
                        supplied in. */
        public void SetVaporPressure(ePressure unit, double Pa)
        {
            double altitude = CalculatePressureAltitude(Pressure, 0.0);
            double VaporPressure = ConvertToPSF(Pa, unit);
            VaporMassFraction = Rdry * VaporPressure / (Rwater * (Pressure - VaporPressure));
            ValidateVaporMassFraction(altitude);
        }

        /** Returns the partial pressure of water vapor.
            @param to The unit of measure that the water vapor should be supplied in.
*/
        public double GetVaporPressure(ePressure to)
        {
            double VaporPressure = Pressure * VaporMassFraction / (VaporMassFraction + Rdry / Rwater);
            return ConvertFromPSF(VaporPressure, to);
        }

        /** Returns the saturated pressure of water vapor.
            @param to The unit of measure that the water vapor should be supplied in.
*/
        public double GetSaturatedVaporPressure(ePressure to)
        {
            return ConvertFromPSF(saturatedVaporPressure, to);
        }

        /** Sets the relative humidity.
            @param RH The relative humidity in percent. */
        public void SetRelativeHumidity(double RH)
        {
            double altitude = CalculatePressureAltitude(Pressure, 0.0);
            double VaporPressure = 0.01 * RH * saturatedVaporPressure;
            VaporMassFraction = Rdry * VaporPressure / (Rwater * (Pressure - VaporPressure));
            ValidateVaporMassFraction(altitude);
        }

        /// Returns the relative humidity in percent.
        public double GetRelativeHumidity()
        {
            double VaporPressure = Pressure * VaporMassFraction / (VaporMassFraction + Rdry / Rwater);
            return 100.0 * VaporPressure / saturatedVaporPressure;
        }

        /** Sets the vapor mass per million of dry air mass units.
            @param frac The fraction of water in ppm of dry air. */
        public void SetVaporMassFractionPPM(double frac)
        {
            double altitude = CalculatePressureAltitude(Pressure, 0.0);
            VaporMassFraction = frac * 1E-6;
            ValidateVaporMassFraction(altitude);
        }

        /// Returns the vapor mass per million of dry air mass units (ppm).
        public double GetVaporMassFractionPPM()
        {
            return VaporMassFraction * 1E6;
        }

        /// Prints the U.S. Standard Atmosphere table.
        public virtual void PrintStandardAtmosphereTable() { throw new NotImplementedException("Pending upgrade to lastest version of JSBSIM"); }


        /// Standard sea level conditions
        protected double StdSLtemperature, StdSLdensity, StdSLpressure, StdSLsoundspeed;

        protected double temperatureBias;
        protected double temperatureDeltaGradient;
        protected double gradientFadeoutAltitude;
        protected double VaporMassFraction;
        protected double saturatedVaporPressure;

        protected Table StdAtmosTemperatureTable;
        protected Table MaxVaporMassFraction;
        protected List<double> LapseRates = new List<double>();
        protected List<double> PressureBreakpoints = new List<double>();
        protected List<double> StdPressureBreakpoints = new List<double>();
        protected List<double> StdDensityBreakpoints = new List<double>();
        protected List<double> StdLapseRates = new List<double>();

        protected override void Calculate(double altitude)
        {
            base.Calculate(altitude);
            saturatedVaporPressure = CalculateVaporPressure(Temperature);
            ValidateVaporMassFraction(altitude);
        }

        /// Recalculate the lapse rate vectors when the temperature profile is altered
        /// in a way that would change the lapse rates, such as when a gradient is
        /// applied.
        /// This function is also called to initialize the lapse rate vector.
        protected void CalculateLapseRates()
        {
            int numRows = StdAtmosTemperatureTable.GetNumRows();
            LapseRates.Clear();

            for (int bh = 0; bh < numRows - 1; bh++)
            {
                double t0 = StdAtmosTemperatureTable.GetElement(bh + 1, 1);
                double t1 = StdAtmosTemperatureTable.GetElement(bh + 2, 1);
                double h0 = StdAtmosTemperatureTable.GetElement(bh + 1, 0);
                double h1 = StdAtmosTemperatureTable.GetElement(bh + 2, 0);
                LapseRates.Add((t1 - t0) / (h1 - h0) - temperatureDeltaGradient);
            }
        }

        /// Calculate (or recalculate) the atmospheric pressure breakpoints at the 
        /// altitudes in the standard temperature table.
        protected void CalculatePressureBreakpoints(double SLpress)
        {
            PressureBreakpoints[0] = SLpress;

            for (int b = 0; b < PressureBreakpoints.Count - 1; b++)
            {
                double BaseTemp = StdAtmosTemperatureTable.GetElement(b + 1, 1);
                double BaseAlt = StdAtmosTemperatureTable.GetElement(b + 1, 0);
                double UpperAlt = StdAtmosTemperatureTable.GetElement(b + 2, 0);
                double deltaH = UpperAlt - BaseAlt;
                double Tmb = BaseTemp
                             + temperatureBias
                             + (gradientFadeoutAltitude - BaseAlt) * temperatureDeltaGradient;
                if (LapseRates[b] != 0.00)
                {
                    double Lmb = LapseRates[b];
                    double Exp = Constants.g0 / (Rdry * Lmb);
                    double factor = Tmb / (Tmb + Lmb * deltaH);
                    PressureBreakpoints[b + 1] = PressureBreakpoints[b] * Math.Pow(factor, Exp);
                }
                else
                {
                    PressureBreakpoints[b + 1] = PressureBreakpoints[b] * Math.Exp(-Constants.g0 * deltaH / (Rdry * Tmb));
                }
            }
        }

        /// Calculate the atmospheric density breakpoints at the 
        /// altitudes in the standard temperature table.
        protected void CalculateStdDensityBreakpoints()
        {
            StdDensityBreakpoints.Clear();
            for (int i = 0; i < StdPressureBreakpoints.Count; i++)
                StdDensityBreakpoints.Add(StdPressureBreakpoints[i] / (Rdry * StdAtmosTemperatureTable.GetElement(i + 1, 1)));
        }

        /// Convert a geometric altitude to a geopotential altitude
        protected double GeopotentialAltitude(double geometalt) { return (geometalt * EarthRadius) / (EarthRadius + geometalt); }

        /// Convert a geopotential altitude to a geometric altitude
        protected double GeometricAltitude(double geopotalt) { return (geopotalt * EarthRadius) / (EarthRadius - geopotalt); }

        /// <summary>
        /// Calculates the density altitude given any temperature or pressure bias.
        /// Calculated density for the specified geometric altitude given any temperature
        /// or pressure biases is passed in.
        /// see
        /// https://en.wikipedia.org/wiki/Density_altitude
        ///  https://wahiduddin.net/calc/density_altitude.htm
        /// </summary>
        /// <param name="density"></param>
        /// <param name="geometricAlt"></param>
        /// <returns></returns>
        protected override double CalculateDensityAltitude(double density, double geometricAlt)
        {
            // Work out which layer we're dealing with
            int b = 0;
            for (; b < StdDensityBreakpoints.Count - 2; b++)
            {
                if (density >= StdDensityBreakpoints[b + 1])
                    break;
            }

            // Get layer properties
            double Tmb = StdAtmosTemperatureTable.GetElement(b + 1, 1);
            double Hb = StdAtmosTemperatureTable.GetElement(b + 1, 0);
            double Lmb = StdLapseRates[b];
            double pb = StdDensityBreakpoints[b];

            double density_altitude = 0.0;

            // https://en.wikipedia.org/wiki/Barometric_formula for density solved for H
            if (Lmb != 0.0)
            {
                double Exp = -1.0 / (1.0 + Constants.g0 / (Rdry * Lmb));
                density_altitude = Hb + (Tmb / Lmb) * (Math.Pow(density / pb, Exp) - 1);
            }
            else
            {
                double Factor = -Rdry * Tmb / Constants.g0;
                density_altitude = Hb + Factor * Math.Log(density / pb);
            }

            return GeometricAltitude(density_altitude);
        }

        /** Calculates the pressure altitude given any temperature or pressure bias.
        Calculated density for the specified geometric altitude given any temperature
        or pressure biases is passed in.
        @param pressure
        @param geometricAlt
        @see
        https://en.wikipedia.org/wiki/Pressure_altitude
        */
        protected override double CalculatePressureAltitude(double pressure, double geometricAlt)
        {
            // Work out which layer we're dealing with
            int b = 0;
            for (; b < StdPressureBreakpoints.Count - 2; b++)
            {
                if (pressure >= StdPressureBreakpoints[b + 1])
                    break;
            }

            // Get layer properties
            double Tmb = StdAtmosTemperatureTable.GetElement(b + 1, 1);
            double Hb = StdAtmosTemperatureTable.GetElement(b + 1, 0);
            double Lmb = StdLapseRates[b];
            double Pb = StdPressureBreakpoints[b];

            double pressure_altitude = 0.0;

            if (Lmb != 0.00)
            {
                // Equation 33(a) from ISA document solved for H
                double Exp = -Rdry * Lmb / Constants.g0;
                pressure_altitude = Hb + (Tmb / Lmb) * (Math.Pow(pressure / Pb, Exp) - 1);
            }
            else
            {
                // Equation 33(b) from ISA document solved for H
                double Factor = -Rdry * Tmb / Constants.g0;
                pressure_altitude = Hb + Factor * Math.Log(pressure / Pb);
            }

            return GeometricAltitude(pressure_altitude);
        }

        /// Calculate the pressure of water vapor with the Magnus formula.
        protected double CalculateVaporPressure(double temperature)
        {
            double temperature_degC = Conversion.RankineToCelsius(temperature);
            return a * Math.Exp(b * temperature_degC / (c + temperature_degC));
        }

        /// Validate the value of the vapor mass fraction
        protected void ValidateVaporMassFraction(double geometricAlt)
        {
            if (saturatedVaporPressure < Pressure)
            {
                double VaporPressure = Pressure * VaporMassFraction / (VaporMassFraction + Rdry / Rwater);
                if (VaporPressure > saturatedVaporPressure)
                    VaporMassFraction = Rdry * saturatedVaporPressure / (Rwater * (Pressure - saturatedVaporPressure));
            }

            double GeoPotAlt = GeopotentialAltitude(geometricAlt);
            double maxFraction = 1E-6 * MaxVaporMassFraction.GetValue(GeoPotAlt);

            if ((VaporMassFraction > maxFraction) || (VaporMassFraction < 0.0))
                VaporMassFraction = maxFraction;

            // Update the gas constant factor
            Reng = (VaporMassFraction * Rwater + Rdry) / (1.0 + VaporMassFraction);
        }

        /// Calculate the SL density
        protected void CalculateSLDensity() { SLdensity = SLpressure / (Constants.Reng * SLtemperature); }

        /// Calculate the SL density and sound speed
        protected void CalculateSLSoundSpeedAndDensity()
        {
            SLsoundspeed = Math.Sqrt(Constants.SHRatio * Constants.Reng * SLtemperature);
            CalculateSLDensity();
        }

        ///<summary>
        /// Sets/Gets the current delta-T in degrees Fahrenheit
        ///</summary>
        [ScriptAttribute("atmosphere/delta-T", "Delta-T in degrees Fahrenheit")]
        public double TemperatureBias
        {
            set { SetTemperatureBias(eTemperature.eRankine, value); }
            get { return GetTemperatureBias(eTemperature.eRankine); }
        }

        ///<summary>
        /// Gets/sets the temperature gradient to be applied on top of the standard
        /// temperature gradient
        ///</summary>
        [ScriptAttribute("atmosphere/SL-graded-delta-T", "The temperature gradient to be applied on top of the standard temperature gradient")]
        public double TemperatureDeltaGradient
        {
            set { SetSLTemperatureGradedDelta(eTemperature.eRankine, value); }
            get { return GetTemperatureDeltaGradient(eTemperature.eRankine); }
        }

        ///<summary>
        /// Gets/sets the sea level pressure in target units, default in psf.
        ///</summary>
        [ScriptAttribute("atmosphere/P-sl-psf", "The sea level pressure in target units, default in psf.")]
        public double PressureSL
        {
            set { SetPressureSL(ePressure.ePSF, value); }
            get { return GetPressureSL(ePressure.ePSF); }
        }

        ///<summary>
        /// Gets/sets the dew point.
        ///</summary>
        [ScriptAttribute("atmosphere/dew-point-R", "The dew point.")]
        public double DewPoint
        {
            set { SetDewPoint(eTemperature.eRankine, value); }
            get { return GetDewPoint(eTemperature.eRankine); }
        }

        ///<summary>
        /// Gets/sets the partial pressure of water vapor.
        ///</summary>
        [ScriptAttribute("atmosphere/vapor-pressure-psf", "The partial pressure of water vapor.")]
        public double VaporPressure
        {
            set { SetVaporPressure(ePressure.ePSF, value); }
            get { return GetVaporPressure(ePressure.ePSF); }
        }

        ///<summary>
        /// Gets/sets the partial pressure of water vapor.
        ///</summary>
        [ScriptAttribute("atmosphere/saturated-vapor-pressure-psf", "The partial pressure of water vapor.")]
        public double SaturatedVaporPressure
        {
            get { return GetSaturatedVaporPressure(ePressure.ePSF); }
        }

        ///<summary>
        /// Gets/sets the relative humidity.
        ///</summary>
        [ScriptAttribute("atmosphere/RH", "The relative humidity.")]
        public double RelativeHumidity
        {
            set { SetRelativeHumidity(value); }
            get { return GetRelativeHumidity(); }
        }

        ///<summary>
        /// Gets/sets the vapor mass per million of dry air mass units (ppm).
        ///</summary>
        [ScriptAttribute("atmosphere/RH", "The vapor mass per million of dry air mass units (ppm).")]
        public double VaporMassFractionPPM
        {
            set { SetVaporMassFractionPPM(value); }
            get { return GetVaporMassFractionPPM(); }
        }

        public override void Bind()
        {
            FDMExec.PropertyManager.Bind("atmosphere", this);
        }

        //protected override void Debug(int from) { throw new NotImplementedException(); }

        /// Earth radius in ft as defined for ISA 1976
        protected const double EarthRadius = 6356766.0 / Constants.fttom;
        /** Sonntag constants based on ref [2]. They are valid for temperatures
            between -45 degC (-49 degF) and 60 degC (140 degF) with a precision of
            +/-0.35 degC (+/-0.63 degF) */
        protected const double a = 611.2 / Constants.psftopa; // psf
        protected const double b = 17.62; // 1/degC
        protected const double c = 243.12; // degC
                                           /// Mean molecular weight for water - slug/mol
        protected const double Mwater = 18.016 * Constants.kgtoslug / 1000.0;
        protected static readonly double Rdry = Rstar / Constants.Mair;
        protected static readonly double Rwater = Rstar / Mwater;
    }
}
