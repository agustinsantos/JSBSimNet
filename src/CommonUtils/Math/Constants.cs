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

/// If you define USEJSBSIM, the simulator will use the same constants as the
/// original JSBSIM. If you comments out this line or define other name
/// (i.e. NOTUSEJSBSIM), the simulator will use constants derived from true
/// expressions.
#define USEJSBSIM

#region Identification
/// $Id:$
#endregion
namespace CommonUtils.MathLib
{
	using System;

	/// <summary>
	/// Summary description for Constants.
	/// </summary>
	public class Constants
	{
#if USEJSBSIM
        public const double radtodeg = 57.29578;
        public const double degtorad = 0.01745329;
#else
        public const double degtorad = (2.0 * Math.PI) / 360.0;
        public const double radtodeg = 360.0 / (2.0 * Math.PI);
#endif
        public const double hptoftlbssec = 550.0;
		public const double psftoinhg = 0.014138; // psf to in Hg
		public const double fpstokts = 0.592484;
		public const double ktstofps = 1.68781;
		public const double inchtoft = 0.08333333;
		public const double in3tom3 = 1.638706E-5;
		public const double Reng = 1716.0; // Specific Gas Constant,ft^2/(sec^2*R)
		public const double SHRatio = 1.40;
		public const double GM = 14.06252720E15;

		public const double PI_MUL2 = 6.28318530717958647692;
		public const double PI_DIV2 = 1.57079632679489661923;
		public const double PI_DIV4 = 0.78539816339744830961;

		// Note that definition of lbtoslug by the inverse of slugtolb and not
		// to a different constant you can also get from some tables will make
		// lbtoslug*slugtolb == 1 up to the magnitude of roundoff. So converting from
		// slug to lb and back will yield to the original value you started with up
		// to the magnitude of roundoff.
		// Taken from units gnu commandline tool
		public const double slugtolb	= 32.174049;
		public const double lbtoslug	= 1.0/slugtolb;

		/** pi/180/60/60, or about 100 feet at earths' equator */
		public const double ONE_SECOND = 4.848136811E-6;


		/** Radius of Earth in kilometers at the equator.  Another source had
		 *  6378.165 but this is probably close enough */
		public const double EARTH_RAD = 6378.155;


		// Earth parameters for WGS 84, taken from LaRCsim/ls_constants.h

		/** Value of earth radius from LaRCsim (ft) */
		public const double EQUATORIAL_RADIUS_FT  = 20925650.0;

		/** Value of earth radius from LaRCsim (meter) */
		public const double EQUATORIAL_RADIUS_M    = 6378138.12;

		/** Radius squared (ft) */
		public const double EQ_RAD_SQUARE_FT = 437882827922500.0;

		/** Radius squared (meter) */
		public const double EQ_RAD_SQUARE_M   = 40680645877797.1344;

		// Conversions

		/** Arc seconds to radians.  (arcsec*pi)/(3600*180) = rad */
		public const double ARCSEC_TO_RAD    = 4.84813681109535993589e-06;

		/** Radians to arc seconds.  (rad*3600*180)/pi = arcsec */
		public const double RAD_TO_ARCSEC    = 206264.806247096355156;

		/** Feet to Meters */
		public const double FEET_TO_METER    = 0.3048;

		/** Meters to Feet */
		public const double METER_TO_FEET    = 3.28083989501312335958;

		/** Meters to Nautical Miles.  1 nm = 6076.11549 feet */
		public const double METER_TO_NM      = 0.0005399568034557235;

		/** Nautical Miles to Meters */
		public const double NM_TO_METER      = 1852.0000;

		/** Meters to Statute Miles. */
		public const double METER_TO_SM      = 0.0006213699494949496;

		/** Statute Miles to Meters. */
		public const double SM_TO_METER      = 1609.3412196;

		/** Radians to Nautical Miles.  1 nm = 1/60 of a degree */
		public const double NM_TO_RAD        = 0.00029088820866572159;

		/** Nautical Miles to Radians */
		public const double RAD_TO_NM        = 3437.7467707849392526;

		/** Miles per second to Knots */
		public const double MPS_TO_KT       = 1.9438444924406046432;

		/** Knots to Miles per second */
		public const double KT_TO_MPS       = 0.5144444444444444444;

		/** Miles per second to Miles per hour */
		public const double MPS_TO_MPH       = 2.2369362920544020312;

		/** Miles per hour to Miles per second */
		public const double MPH_TO_MPS       = 0.44704;

		/** Meters per second to Kilometers per hour */
		public const double MPS_TO_KMH       = 3.6;

		/** Kilometers per hour to Miles per second */
		public const double KMH_TO_MPS       = 0.2777777777777777778;

		/** Pascal to Inch Mercury */
		public const double PASCAL_TO_INHG   = 0.0002952998330101010;

		/** Inch Mercury to Pascal */
		public const double INHG_TO_PA       = 3386.388640341;


	}
}
