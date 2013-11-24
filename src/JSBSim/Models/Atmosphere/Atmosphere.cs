
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

namespace JSBSim.Models
{
	using System;
	using System.Reflection;


	using CommonUtils.MathLib;
	using JSBSim.Script;

	// Import log4net classes.
	using log4net;

	public class AtmosphereInformation
	{
		private double temperature, density, pressure;

		public double Temperature
		{
			get { return temperature;}
			set { temperature = value;}
		}
		public double Pressure
		{
			get { return pressure;}
			set { pressure = value;}
		}
		public double Density
		{
			get { return density;}
			set { density = value;}
		}
	}

	/// <summary>
	/// Models the standard atmosphere.
	/// This code is based on FGAtmosphere written by Tony Peden, Jon Berndt
	/// see Anderson, John D. "Introduction to Flight, Third Edition", McGraw-Hill,
	/// 1989, ISBN 0-07-001641-0
	/// </summary>
	[Serializable]
	public class Atmosphere : Model
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
		

		public Atmosphere(FDMExecutive exec) : base(exec)
		{
			Name = "Atmosphere";

			lastIndex = 0;
			h = 0.0;
			psiw = 0.0;
            /*
			htab[0]=0;
			htab[1]=36089.239;
			htab[2]=65616.798;
			htab[3]=104986.878;
			htab[4]=154199.475;
			htab[5]=170603.675;
			htab[6]=200131.234;
			htab[7]=259186.352; //ft.
            */
			MagnitudedAccelDt = MagnitudeAccel = Magnitude = 0.0;
			//   turbType = ttNone;
			turbType = TurbType.ttStandard;
			//   turbType = ttBerndt;
			TurbGain = 0.0;
			TurbRate = 1.0;

			T_dev_sl = T_dev = delta_T = 0.0;

			useInfo = internalInfo;
		}

		/// <summary>
		/// Runs the Atmosphere model; called by the Executive
		/// </summary>
		/// <returns>false if no error</returns>
        public override bool Run()
        {
            if (InternalRun()) return true;
            if (FDMExec.Holding()) return false;

            T_dev = 0.0;


            h = FDMExec.Propagate.Altitude;

            if (!useExternal)
            {
                Calculate(h);
                CalculateDerived();
            }
            else
            {
                CalculateDerived();
            }


            // if false then execute this Run()
            //do temp, pressure, and density first
            if (!useExternal)
            {
                h = FDMExec.Propagate.Altitude;
                Calculate(h);
            }

            if (turbType != TurbType.ttNone)
            {
                Turbulence();
                vWindNED += vTurbulence;
            }
            return false;
        }

		public override bool InitModel()
		{
			base.InitModel();

            UseInternal();  // this is the default

			Calculate(h);

            StdSLtemperature = SLtemperature = 518.67;
            StdSLpressure = SLpressure = 2116.22;
            StdSLdensity = SLdensity = 0.00237767;

			SLtemperature = internalInfo.Temperature;
			SLpressure    = internalInfo.Pressure;
			SLdensity     = internalInfo.Density;
			SLsoundspeed  = Math.Sqrt(Constants.SHRatio*Constants.Reng*internalInfo.Temperature);
			rSLtemperature = 1.0/internalInfo.Temperature;
			rSLpressure    = 1.0/internalInfo.Pressure;
			rSLdensity     = 1.0/internalInfo.Density;
			rSLsoundspeed  = 1.0/SLsoundspeed;
			
			useInfo = internalInfo;
			useExternal=false;

			return true;
		}

		/// <summary>
		/// Returns the temperature in degrees Rankine
		/// </summary>
		[ScriptAttribute("atmosphere/T-R", "Temperature in degrees Rankine")]
		public double Temperature {get {return useInfo.Temperature;}}

		/// <summary>
		/// Returns the density in slugs/ft^3
		/// This function may <b>only</b> be used if Run() is called first.
		/// </summary>
		[ScriptAttribute("atmosphere/rho-slugs_ft3", "Density in slugs/ft^3")]
		public double Density {get {return useInfo.Density;}}

		/// <summary>
		/// Returns the pressure in psf
		/// </summary>
		[ScriptAttribute("atmosphere/P-psf", "Pressure in psf")]
		public double Pressure  {get {return useInfo.Pressure;}}


		/// <summary>
		/// Returns the speed of sound in ft/sec.
		/// </summary>
		[ScriptAttribute("atmosphere/a-fps", "Speed of sound in ft/sec")]
		public double SoundSpeed {get {return soundspeed;}}

		/// <summary>
		/// Returns the sea level temperature in degrees Rankine
		/// </summary>
		[ScriptAttribute("atmosphere/T-sl-R", "Sea level temperature in degrees Rankine")]
		public double TemperatureSeaLevel {get {return SLtemperature; }}

		/// <summary>
		/// Returns the sea level density in slugs/ft^3
		/// </summary>
		[ScriptAttribute("atmosphere/rho-sl-slugs_ft3", "Sea level density in slugs/ft^3")]
		public double DensitySeaLevel {get {return SLdensity; }}

		/// <summary>
		/// Returns the sea level pressure in psf.
		/// </summary>
		[ScriptAttribute("atmosphere/P-sl-psf", "Sea level pressure in psf")]
		public double PressureSeaLevel {get {return SLpressure; }}

		/// <summary>
		/// Returns the sea level speed of sound in ft/sec.
		/// </summary>
		[ScriptAttribute("atmosphere/a-sl-fps", "Sea level speed of sound in ft/sec")]
		public double SoundSpeedSeaLevel {get {return SLsoundspeed; }}

		/// <summary>
		/// Returns the ratio of at-altitude temperature over the sea level value.
		/// </summary>
		[ScriptAttribute("atmosphere/theta-norm", "Ratio of at-altitude temperature over the sea level value")]
		public double TemperatureRatio {get {return (useInfo.Temperature)*rSLtemperature; }} 

		/// <summary>
		/// Returns the ratio of at-altitude density over the sea level value.
		/// </summary>
		[ScriptAttribute("atmosphere/sigma-norm", "Ratio of at-altitude density over the sea level value")]
		public double DensityRatio {get {return (useInfo.Density)*rSLdensity; }}

		/// <summary>
		/// Returns the ratio of at-altitude pressure over the sea level value. 
		/// </summary>
		[ScriptAttribute("atmosphere/delta-norm", "Ratio of at-altitude pressure over the sea level value")]
		public double PressureRatio {get {return (useInfo.Pressure)*rSLpressure; }}

		// Returns the ratio of at-altitude sound speed over the sea level value.
		[ScriptAttribute("atmosphere/a-norm", "Ratio of at-altitude sound speed over the sea level value")]
		public double SoundSpeedRatio {get {return soundspeed*rSLsoundspeed; }}

		
		/// <summary>
		/// Tells the simulator to use an externally calculated atmosphere model.
		/// </summary>
		public virtual void UseExternal()
		{
			useInfo = externalInfo;
			useExternal=true;
		}
		
		/// <summary>
		/// Tells the simulator to use the internal atmosphere model.
		/// This is the default
		/// </summary>
		public virtual void UseInternal()
		{
			useInfo = internalInfo;
			useExternal=false;
		}

		/// <sumary>
		/// Gets the boolean that tells if the external atmosphere model is being used.
		/// </sumary>
		public bool External() { return useExternal; }

		///<summary>
		/// Provides the external atmosphere model with an interface to set the temperature.
		///</summary>
		public void SetExTemperature(double t)  { externalInfo.Temperature=t; }
		
		///<summary>
		/// Provides the external atmosphere model with an interface to set the density.
		///</summary>
		public void SetExDensity(double d)      { externalInfo.Density=d; }
		
		///<summary>
		/// Provides the external atmosphere model with an interface to set the pressure.
		///</summary>
		public void SetExPressure(double p)     { externalInfo.Pressure=p; }

		
		///<summary>
		/// Gets/sets the temperature deviation at sea-level in degrees Fahrenheit
		///</summary>
		[ScriptAttribute("atmosphere/T-sl-dev-F", "Temperature deviation at sea-level in degrees Fahrenheit")]
		public double TempDevSeaLevel
		{
			get { return T_dev_sl; }
			set { T_dev_sl = value;}
		}
		
		///<summary>
		/// Sets/Gets the current delta-T in degrees Fahrenheit
		///</summary>
		[ScriptAttribute("atmosphere/delta-T", "Delta-T in degrees Fahrenheit")]
		public double DeltaT
		{
			set {delta_T = value; } 
			get {return delta_T;}
		}

		
		///<summary>
		/// Gets the at-altitude temperature deviation in degrees Fahrenheit
		///</summary>
		public double GetTempDev() { return T_dev; }
		
		///<summary>
		/// Gets the density altitude in feet
		/// </summary>
		[ScriptAttribute("atmosphere/density-altitude", "Delta-T in degrees Fahrenheit")]
		public double DensityAltitude { get { return density_altitude; }}

		///<summary>
		/// Sets the wind components in NED frame.
		///</summary>
		public void SetWindNED(double wN, double wE, double wD) { vWindNED.X=wN; vWindNED.Y=wE; vWindNED.Z=wD;}

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
		public double WindPsi { get { return psiw; }}
  
		public void SetTurbGain(double tt) {TurbGain = tt;}
		public void SetTurbRate(double tt) {TurbRate = tt;}
  
		public double GetTurbPQR(int idx) {return vTurbPQR[idx];}


		[ScriptAttribute("atmosphere/p-turb-rad_sec", "p-turb-rad_sec")]
		public Vector3D TurbPQR {get {return vTurbPQR;}}

		public enum TurbType {ttStandard, ttBerndt, ttNone};

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
		protected double SLtemperature,SLdensity,SLpressure,SLsoundspeed;
		protected double rSLtemperature,rSLdensity,rSLpressure,rSLsoundspeed; //reciprocals

		protected AtmosphereInformation internalInfo = new AtmosphereInformation();
		protected AtmosphereInformation externalInfo;
		protected AtmosphereInformation useInfo;

		protected double soundspeed;
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

		protected void Calculate(double altitude)
		{
			double slope, reftemp, refpress;
			int i = 0;

			i = lastIndex;
			if (altitude < htab[lastIndex]) 
			{
				if (altitude <= 0) 
				{
					i = 0;
					altitude=0;
				} 
				else 
				{
					i = lastIndex-1;
					while (htab[i] > altitude) i--;
				}
			} 
			else if (lastIndex == 7  || altitude > htab[lastIndex+1]) 
			{
				if (altitude >= htab[7]) 
				{
					i = 7;
					altitude = htab[7];
				} 
				else 
				{
					i = lastIndex+1;
					while (htab[i+1] < altitude) i++;
				}
			}

			switch(i) 
			{
				case 1:     // 36089 ft.
					slope     = 0;
					reftemp   = 389.97;
					refpress  = 472.452;
					//refdens   = 0.000706032;
					break;
				case 2:     // 65616 ft.
					slope     = 0.00054864;
					reftemp   = 389.97;
					refpress  = 114.636;
					//refdens   = 0.000171306;
					break;
				case 3:     // 104986 ft.
					slope     = 0.00153619;
					reftemp   = 411.57;
					refpress  = 8.36364;
					//refdens   = 1.18422e-05;
					break;
				case 4:     // 154199 ft.
					slope     = 0;
					reftemp   = 487.17;
					refpress  = 0.334882;
					//refdens   = 4.00585e-7;
					break;
				case 5:     // 170603 ft.
					slope     = -0.00109728;
					reftemp   = 487.17;
					refpress  = 0.683084;
					//refdens   = 8.17102e-7;
					break;
				case 6:     // 200131 ft.
					slope     = -0.00219456;
					reftemp   = 454.17;
					refpress  = 0.00684986;
					//refdens   = 8.77702e-9;
					break;
				case 7:     // 259186 ft.
					slope     = 0;
					reftemp   = 325.17;
					refpress  = 0.000122276;
					//refdens   = 2.19541e-10;
					break;
				case 0:
				default:     // sea level
					slope     = -0.00356616; // R/ft.
					reftemp   = 518.67;    // R
					refpress  = 2116.22;    // psf
					//refdens   = 0.00237767;  // slugs/cubic ft.
					break;

			}

            // If delta_T is set, then that is our temperature deviation at any altitude.
            // If not, then we'll estimate a deviation based on the sea level deviation (if set).
            if (!StandardTempOnly)
            {
                T_dev = 0.0;
                if (delta_T != 0.0)
                {
                    T_dev = delta_T;
                }
                else
                {
                    if ((h < 36089.239) && (T_dev_sl != 0.0))
                    {
                        T_dev = T_dev_sl * (1.0 - (h / 36089.239));
                    }
                }
                reftemp += T_dev;
            }

			if (slope == 0) 
			{
				internalInfo.Temperature = reftemp;
				internalInfo.Pressure = refpress*Math.Exp(-FDMExec.Inertial.SLgravity()/(reftemp*Constants.Reng)*(altitude-htab[i]));
				//intDensity = refdens*exp(-Inertial.SLgravity()/(reftemp*Reng)*(altitude-htab[i]));
				internalInfo.Density = internalInfo.Pressure/(Constants.Reng*internalInfo.Temperature);
			} 
			else 
			{
				internalInfo.Temperature = reftemp+slope*(altitude-htab[i]);
				internalInfo.Pressure = refpress*Math.Pow(internalInfo.Temperature/reftemp,-FDMExec.Inertial.SLgravity()/(slope*Constants.Reng));
				//intDensity = refdens*pow(intTemperature/reftemp,-(Inertial.SLgravity()/(slope*Reng)+1));
				internalInfo.Density = internalInfo.Pressure/(Constants.Reng*internalInfo.Temperature);
			}
			lastIndex=i;
			//cout << "Atmosphere:  h=" << altitude << " rho= " << intDensity << endl;
		}

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
        /// Returns the standard temperature at an arbitrary altitude
        /// </summary>
        /// <param name="alt">the altitude</param>
        /// <returns>the standard temperature at a specified altitude</returns>
        public double GetTemperature(double altitude)
        {
            GetStdAtmosphere(altitude);
            return atmosphere.Temperature;
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
					vDirectiondAccelDt.X = 1 - 2.0*rand.NextDouble();
					vDirectiondAccelDt.Y = 1 - 2.0*rand.NextDouble();
					vDirectiondAccelDt.Z = 1 - 2.0*rand.NextDouble();

					MagnitudedAccelDt = 1 - 2.0*rand.NextDouble() - Magnitude;
					// Scale the magnitude so that it moves
					// away from the peaks
					MagnitudedAccelDt = ((MagnitudedAccelDt - Magnitude) /
						(1 + Math.Abs(Magnitude)));
					MagnitudeAccel    += MagnitudedAccelDt*rate*TurbRate*FDMExec.State.DeltaTime;
					Magnitude         += MagnitudeAccel*rate*FDMExec.State.DeltaTime;

					vDirectiondAccelDt.Normalize();

					// deemphasise non-vertical forces
					vDirectiondAccelDt.X = square_signed(vDirectiondAccelDt.X);
					vDirectiondAccelDt.Y = square_signed(vDirectiondAccelDt.Y);

					vDirectionAccel += vDirectiondAccelDt*rate*TurbRate*FDMExec.State.DeltaTime;
					vDirectionAccel.Normalize();
					vDirection      += vDirectionAccel*rate*FDMExec.State.DeltaTime;

					vDirection.Normalize();

					// Diminish turbulence within three wingspans
					// of the ground
					vTurbulence = TurbGain * Magnitude * vDirection;
					double HOverBMAC = FDMExec.Auxiliary.HOverBMAC;
					if (HOverBMAC < 3.0)
						vTurbulence *= (HOverBMAC / 3.0) * (HOverBMAC / 3.0);

					vTurbulenceGrad = TurbGain*MagnitudeAccel * vDirection;

					vBodyTurbGrad = FDMExec.Propagate.GetTl2b()*vTurbulenceGrad;

					if (FDMExec.Aircraft.WingSpan > 0) 
					{
						vTurbPQR.eP = vBodyTurbGrad.Y/FDMExec.Aircraft.WingSpan;
					} 
					else 
					{
						vTurbPQR.eP = vBodyTurbGrad.Y/30.0;
					}
					//     if (Aircraft.GetHTailArm() != 0.0)
					//       vTurbPQR(eQ) = vBodyTurbGrad(eZ)/Aircraft.GetHTailArm();
					//     else
					//       vTurbPQR(eQ) = vBodyTurbGrad(eZ)/10.0;

					if (FDMExec.Aircraft.VTailArm > 0)
						vTurbPQR.eR = vBodyTurbGrad.X/FDMExec.Aircraft.VTailArm;
					else
						vTurbPQR.eR = vBodyTurbGrad.X/10.0;

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
					vDirectiondAccelDt.X = 1 - 2.0*rand.NextDouble();
					vDirectiondAccelDt.Y = 1 - 2.0*rand.NextDouble();
					vDirectiondAccelDt.Z = 1 - 2.0*rand.NextDouble();


					MagnitudedAccelDt = 1 - 2.0*rand.NextDouble() - Magnitude;
					MagnitudeAccel    += MagnitudedAccelDt*rate*FDMExec.State.DeltaTime;
					Magnitude         += MagnitudeAccel*rate*FDMExec.State.DeltaTime;

					vDirectiondAccelDt.Normalize();
					vDirectionAccel += vDirectiondAccelDt*rate*FDMExec.State.DeltaTime;
					vDirectionAccel.Normalize();
					vDirection      += vDirectionAccel*rate*FDMExec.State.DeltaTime;

					// Diminish z-vector within two wingspans
					// of the ground
					double HOverBMAC = FDMExec.Auxiliary.HOverBMAC;
					if (HOverBMAC < 2.0)
						vDirection.Z *= HOverBMAC / 2.0;

					vDirection.Normalize();

					vTurbulence = TurbGain*Magnitude * vDirection;
					vTurbulenceGrad = TurbGain*MagnitudeAccel * vDirection;

					vBodyTurbGrad = FDMExec.Propagate.GetTl2b()*vTurbulenceGrad;
					vTurbPQR.eP = vBodyTurbGrad.Y/FDMExec.Aircraft.WingSpan;
					if (FDMExec.Aircraft.HTailArm > 0)
						vTurbPQR.eQ = vBodyTurbGrad.Z/FDMExec.Aircraft.HTailArm;
					else
						vTurbPQR.eQ = vBodyTurbGrad.Z/10.0;

					if (FDMExec.Aircraft.VTailArm > 0)
						vTurbPQR.eQ = vBodyTurbGrad.X/FDMExec.Aircraft.VTailArm;
					else
						vTurbPQR.eQ = vBodyTurbGrad.X/10.0;

					break;
				}
				default:
					break;
			}
		}
	}
}
