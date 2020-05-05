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
namespace CommonUtils.MathLib
{
    using System;

    /// <summary>
    /// Moments L, M, N
    /// </summary>
    public enum MomentType { eL, eM, eN };

    /// <summary>
    /// Rates P, Q, R
    /// </summary>
    public enum RateType { eP, eQ, eR };

    /// <summary>
    /// Velocities U, V, W
    /// </summary>
    public enum VelocityType { eU, eV, eW };

    /// <summary>
    /// Positions X, Y, Z
    /// </summary>
    public enum PositionType : int { eX, eY, eZ };

    /// <summary>
    /// Euler angles Phi, Theta, Psi
    /// </summary>
    public enum EulerAngleType { ePhi, eTht, ePsi };

    /// <summary>
    /// Stability axis forces, Drag, Side force, Lift
    /// </summary>
    public enum StabilityAxisForces { eDrag, eSide, eLift };

    /// <summary>
    /// Local frame orientation Roll, Pitch, Yaw
    /// </summary>
    public enum LocalOrientation { eRoll, ePitch, eYaw };

    /// <summary>
    /// Local frame position North, East, Down
    /// </summary>
    public enum LocalPosition { eNorth, eEast, eDown };

    /// <summary>
    /// Locations Radius, Latitude, Longitude
    /// </summary>
    public enum LocationsType { eLat, eLong, eRad };

    /// <summary>
    /// Conversion specifiers
    /// </summary>
    public enum ConversionType { inNone = 0, inDegrees, inRadians, inMeters, inFeet };

    /// <summary>
    /// JSBSim Base class.
    /// This class provides universal constants, utility functions, messaging
    /// functions, and enumerated constants to JSBSim.
    /// </summary>
    public class Conversion
    {
        /// <summary>
        /// Converts from degrees Kelvin to degrees Fahrenheit.
        /// </summary>
        /// <param name="kelvin">The temperature in degrees Kelvin</param>
        /// <returns>The temperature in Fahrenheit</returns>
		public static double KelvinToFahrenheit(double kelvin)
        {
            return 1.8 * kelvin - 459.4;
        }

        /// <summary>
        /// Converts from degrees Celsius to degrees Rankine.
        /// </summary>
        /// <param name="celsius">celsius The temperature in degrees Celsius.</param>
        /// <returns>The temperature in Rankine.</returns>
        public static double CelsiusToRankine(double celsius)
        {
            return celsius * 1.8 + 491.67;
        }

        /// <summary>
        /// Converts from degrees Rankine to degrees Celsius.
        /// </summary>
        /// <param name="rankine">rankine The temperature in degrees Rankine.</param>
        /// <returns>The temperature in Celsius.</returns>
        public static double RankineToCelsius(double rankine)
        {
            return (rankine - 491.67) / 1.8;
        }

        /// <summary>
        /// Converts from degrees Kelvin to degrees Rankine.
        /// </summary>
        /// <param name="kelvin">kelvin The temperature in degrees Kelvin.</param>
        /// <returns>The temperature in Rankine.</returns>
        public static double KelvinToRankine(double kelvin)
        {
            return kelvin * 1.8;
        }

        /// <summary>
        /// Converts from degrees Rankine to degrees Kelvin.
        /// </summary>
        /// <param name="rankine">rankine The temperature in degrees Rankine.</param>
        /// <returns>The temperature in Kelvin.</returns>
        public static double RankineToKelvin(double rankine)
        {
            return rankine / 1.8;
        }

        /// <summary>
        /// Converts from degrees Fahrenheit to degrees Celsius.
        /// </summary>
        /// <param name="fahrenheit">The temperature in degrees Fahrenheit.</param>
        /// <returns>The temperature in Celsius.</returns>
        public static double FahrenheitToCelsius(double fahrenheit)
        {
            return (fahrenheit - 32.0) / 1.8;
        }

        /// <summary>
        /// Converts from degrees Celsius to degrees Fahrenheit.
        /// </summary>
        /// <param name="celsius">The temperature in degrees Celsius.</param>
        /// <returns>The temperature in Fahrenheit.</returns>
        public static double CelsiusToFahrenheit(double celsius)
        {
            return celsius * 1.8 + 32.0;
        }

        /// <summary>
        /// Converts from degrees Celsius to degrees Kelvin
        /// </summary>
        /// <param name="celsius">The temperature in degrees Celsius.</param>
        /// <returns>The temperature in Kelvin.</returns>
        public static double CelsiusToKelvin(double celsius)
        {
            return celsius + 273.15;
        }

        /// <summary>
        /// Converts from degrees Kelvin to degrees Celsius
        /// </summary>
        /// <param name="kelvin">The temperature in degrees Kelvin.</param>
        /// <returns>The temperature in Celsius.</returns>
        public static double KelvinToCelsius(double kelvin)
        {
            return kelvin - 273.15;
        }

        /// <summary>
        /// Converts from feet to meters
        /// </summary>
        /// <param name="measure">The length in feet.</param>
        /// <returns>The length in meters.</returns>
        public static double FeetToMeters(double measure)
        {
            return measure * 0.3048;
        }

        /// <summary>
        /// Compute the total pressure in front of the Pitot tube. It uses the
        /// Rayleigh formula for supersonic speeds(See "Introduction to Aerodynamics
        /// of a Compressible Fluid - H.W.Liepmann, A.E.Puckett - Wiley &#38; sons
        /// (1947)" §5.4 pp 75-80)
        /// </summary>
        /// <param name="mach">The Mach number</param>
        /// <param name="p">Pressure in psf</param>
        /// <returns>The total pressure in front of the Pitot tube in psf</returns>
        public static double PitotTotalPressure(double mach, double p)
        {
            if (mach < 0) return p;
            if (mach < 1)    //calculate total pressure assuming isentropic flow
                return p * Math.Pow((1 + 0.2 * mach * mach), 3.5);
            else
            {
                // shock in front of pitot tube, we'll assume its normal and use
                // the Rayleigh Pitot Tube Formula, i.e. the ratio of total
                // pressure behind the shock to the static pressure in front of
                // the normal shock assumption should not be a bad one -- most supersonic
                // aircraft place the pitot probe out front so that it is the forward
                // most point on the aircraft.  The real shock would, of course, take
                // on something like the shape of a rounded-off cone but, here again,
                // the assumption should be good since the opening of the pitot probe
                // is very small and, therefore, the effects of the shock curvature
                // should be small as well. AFAIK, this approach is fairly well accepted
                // within the aerospace community

                // The denominator below is zero for Mach ~ 0.38, for which
                // we'll never be here, so we're safe

                return p * 166.92158009316827 * Math.Pow(mach, 7.0) / Math.Pow(7 * mach * mach - 1, 2.5);
            }
        }

        /// <summary>
        /// Compute the Mach number from the differential pressure (qc) and the
        /// static pressure. Based on the formulas in the US Air Force Aircraft
        /// Performance Flight Testing Manual (AFFTC-TIH-99-01).
        /// </summary>
        /// <param name="qc">The differential/impact pressure</param>
        /// <param name="p">Pressure in psf</param>
        /// <returns>The Mach number</returns>
        public static double MachFromImpactPressure(double qc, double p)
        {
            double A = qc / p + 1;
            double M = Math.Sqrt(5.0 * (Math.Pow(A, 1.0 / 3.5) - 1));  // Equation (4.12)

            if (M > 1.0)
                for (int i = 0; i < 10; i++)
                    M = 0.8812848543473311 * Math.Sqrt(A * Math.Pow(1 - 1.0 / (7.0 * M * M), 2.5));  // Equation (4.17)

            return M;
        }


        /// <summary>
        /// Calculate the calibrated airspeed from the Mach number. Based on the
        /// formulas in the US Air Force Aircraft Performance Flight Testing 
        /// Manual (AFFTC-TIH-99-01).
        /// </summary>
        /// <param name="mach">The Mach number</param>
        /// <param name="p">Pressure in psf</param>
        /// <returns>The calibrated airspeed (CAS) in ft/s</returns>
        public static double VcalibratedFromMach(double mach, double p)
        {
            double asl = Constants.StdDaySLsoundspeed;
            double psl = Constants.StdDaySLpressure;
            double qc = PitotTotalPressure(mach, p) - p;

            return asl * MachFromImpactPressure(qc, psl);
        }


        /// <summary>
        /// Calculate the Mach number from the calibrated airspeed.Based on the
        /// formulas in the US Air Force Aircraft Performance Flight Testing 
        /// Manual(AFFTC-TIH-99-01).
        /// </summary>
        /// <param name="vcas">The calibrated airspeed (CAS) in ft/s</param>
        /// <param name="p">Pressure in psf</param>
        /// <returns>The Mach number</returns>
        public static double MachFromVcalibrated(double vcas, double p)
        {
            double asl = Constants.StdDaySLsoundspeed;
            double psl = Constants.StdDaySLpressure;
            double qc = PitotTotalPressure(vcas / asl, psl) - psl;

            return MachFromImpactPressure(qc, p);
        }
    }


    /// <summary>
    /// Various constant definitions and math functions.
    /// </summary>
    public class MathExt
    {
        /// <summary>
        /// For divide by zero avoidance, this will be close enough to zero
        /// </summary>
        public const double EPSILON = 0.0000001;

        /// <summary>
        /// smallest such that 1.0+DBL_EPSILON != 1.0
        /// </summary>
        public const double DBL_EPSILON = 2.2204460492503131e-016;

        /// <summary>
        /// smallest such that 1.0+FLT_EPSILON != 1.0
        /// </summary>
        public const float FLT_EPSILON = 1.192092896e-07f;

        /// <summary>
        /// Finite precision comparison.
        /// </summary>
        /// <param name="a">first value to compare</param>
        /// <param name="b">second value to compare</param>
        /// <returns>if the two values can be considered equal up to roundoff</returns>
        public static bool EqualToRoundoff(double a, double b)
        {
            double eps = 2.0 * DBL_EPSILON;
            return Math.Abs(a - b) <= eps * Math.Max(Math.Abs(a), Math.Abs(b));
        }

        /// <summary>
        /// Finite precision comparison.
        /// </summary>
        /// <param name="a">first value to compare</param>
        /// <param name="b">second value to compare</param>
        /// <returns>if the two values can be considered equal up to roundoff</returns>
        public static bool EqualToRoundoff(float a, float b)
        {
            float eps = 2.0f * MathExt.FLT_EPSILON;
            return Math.Abs(a - b) <= eps * Math.Max(Math.Abs(a), Math.Abs(b));
        }

        /// <summary>
        /// Finite precision comparison.
        /// </summary>
        /// <param name="a">first value to compare</param>
        /// <param name="b">second value to compare</param>
        /// <returns>if the two values can be considered equal up to roundoff</returns>
        public static bool EqualToRoundoff(float a, double b)
        {
            return EqualToRoundoff(a, (float)b);
        }

        /// <summary>
        /// Finite precision comparison.
        /// </summary>
        /// <param name="a">first value to compare</param>
        /// <param name="b">second value to compare</param>
        /// <returns>if the two values can be considered equal up to roundoff</returns>
        public static bool EqualToRoundoff(double a, float b)
        {
            return EqualToRoundoff((float)a, b);
        }

        /// <summary>
        /// Constrain a value between a minimum and a maximum value.
        /// </summary>
        /// <param name="min">the min limit</param>
        /// <param name="value">the value to constrain</param>
        /// <param name="max">the max limit</param>
        /// <returns></returns>
        public static double Constrain(double min, double value, double max)
        {
            return value < min ? (min) : (value > max ? (max) : (value));
        }

        public static double Sign(double num) { return num >= 0.0 ? 1.0 : -1.0; }
    }

    /// <summary>
    /// First order, (low pass / lag) filter
    /// </summary>
    public class Filter
    {
        protected double prev_in;
        protected double prev_out;
        protected double ca;
        protected double cb;

        public Filter() { }
        public Filter(double coeff, double dt)
        {
            prev_in = prev_out = 0.0;
            double denom = 2.0 + coeff * dt;
            ca = coeff * dt / denom;
            cb = (2.0 - coeff * dt) / denom;
        }
        public double Execute(double @in)
        {
            double @out = (@in + prev_in) * ca + prev_out * cb;
            prev_in = @in;
            prev_out = @out;
            return @out;
        }
    }
}
