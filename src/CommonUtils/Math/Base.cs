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
namespace CommonUtils.MathLib
{
	using System;

	/// <summary>
	/// Moments L, M, N
	/// </summary>
	public enum MomentType {eL,  eM,     eN    };

	/// <summary>
	/// Rates P, Q, R
	/// </summary>
	public enum RateType {eP, eQ,     eR    };

	/// <summary>
	/// Velocities U, V, W
	/// </summary>
	public enum VelocityType {eU, eV,     eW    };

	/// <summary>
	/// Positions X, Y, Z
	/// </summary>
	public enum PositionType : int {eX, eY, eZ};

	/// <summary>
	/// Euler angles Phi, Theta, Psi
	/// </summary>
	public enum EulerAngleType {ePhi , eTht,   ePsi  };

	/// <summary>
	/// Stability axis forces, Drag, Side force, Lift
	/// </summary>
	public enum StabilityAxisForces {eDrag, eSide,  eLift };

	/// <summary>
	/// Local frame orientation Roll, Pitch, Yaw
	/// </summary>
	public enum LocalOrientation {eRoll, ePitch,  eYaw  };

	/// <summary>
	/// Local frame position North, East, Down
	/// </summary>
	public enum LocalPosition {eNorth, eEast,  eDown };

	/// <summary>
	/// Locations Radius, Latitude, Longitude
	/// </summary>
	public enum LocationsType {eLat, eLong, eRad     };

	/// <summary>
	/// Conversion specifiers
	/// </summary>
	public enum ConversionType {inNone = 0, inDegrees, inRadians, inMeters, inFeet };


	public class Conversion
	{
		public static double KelvinToFahrenheit (double kelvin) 
		{
			return 1.8*kelvin - 459.4;
		}

		public static double RankineToCelsius (double rankine) 
		{
			return (rankine - 491.67)/1.8;
		}

		public static double FahrenheitToCelsius (double fahrenheit) 
		{
			return (fahrenheit - 32.0)/1.8;
		}

		public static double CelsiusToFahrenheit (double celsius) 
		{
			return celsius * 1.8 + 32.0;
		}
	}


	/// <summary>
	/// Various constant definitions and math functions.
	/// </summary>
	public class MathExt
	{
		/** For divide by zero avoidance, this will be close enough to zero */
		public const double EPSILON			= 0.0000001;

		public const double DBL_EPSILON	 =  1.0e-16; //TODO
		public const float FLT_EPSILON	 =  1.0e-8f; //TODO

		/// <summary>
		/// Finite precision comparison.
		/// </summary>
		/// <param name="a">first value to compare</param>
		/// <param name="b">second value to compare</param>
		/// <returns>if the two values can be considered equal up to roundoff</returns>
		static public bool EqualToRoundoff(double a, double b) 
		{
			double eps = 2.0*DBL_EPSILON;
			return Math.Abs(a - b) <= eps*Math.Max(Math.Abs(a), Math.Abs(b));
		}

		/// <summary>
		/// Finite precision comparison.
		/// </summary>
		/// <param name="a">first value to compare</param>
		/// <param name="b">second value to compare</param>
		/// <returns>if the two values can be considered equal up to roundoff</returns>
		static bool EqualToRoundoff(float a, float b) 
		{
			float eps = 2.0f*MathExt.FLT_EPSILON;
			return Math.Abs(a - b) <= eps*Math.Max(Math.Abs(a), Math.Abs(b));
		}
		
		/// <summary>
		/// Finite precision comparison.
		/// </summary>
		/// <param name="a">first value to compare</param>
		/// <param name="b">second value to compare</param>
		/// <returns>if the two values can be considered equal up to roundoff</returns>
		static bool EqualToRoundoff(float a, double b) 
		{
			return EqualToRoundoff(a, (float)b);
		}
		
		/// <summary>
		/// Finite precision comparison.
		/// </summary>
		/// <param name="a">first value to compare</param>
		/// <param name="b">second value to compare</param>
		/// <returns>if the two values can be considered equal up to roundoff</returns>
		static bool EqualToRoundoff(double a, float b) 
		{
			return EqualToRoundoff((float)a, b);
		}

	}
}
