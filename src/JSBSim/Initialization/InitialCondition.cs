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
namespace JSBSim
{
	using System;
	using System.IO;
	using System.Xml;

	// Import log4net classes.
	using log4net;

	using CommonUtils.MathLib;
	using CommonUtils.IO;
	using JSBSim.InputOutput;
    using JSBSim.Format;
    using JSBSim.Script;

	public enum SpeedSet{ setvt, setvc, setve, setmach, setuvw, setned, setvg } ;
	public enum WindSet { setwned, setwmd, setwhc } ;

    /// <summary>
    /// Takes a set of initial conditions and provide a kinematically consistent set
    /// of body axis velocity components, euler angles, and altitude.  This class
    /// does not attempt to trim the model i.e. the sim will most likely start in a
    /// very dynamic state (unless, of course, you have chosen your IC's wisely)
    /// even after setting it up with this class.
    /// 
    /// USAGE NOTES
    /// 
    /// With a valid object of FDMExecutive and an aircraft model loaded
    /// InitialCondition fgic=new InitialCondition(FDMExec);
    /// fgic.SetVcalibratedKtsIC()
    /// fgic.SetAltitudeFtIC();
    /// 
    /// to directly into Run
    /// FDMExec.GetState().Initialize(fgic)
    /// delete fgic;
    /// FDMExec.Run()
    /// 
    /// or to loop the sim w/o integrating
    /// FDMExec.RunIC(fgic)
    /// 
    /// Speed:
    /// 
    /// Since vc, ve, vt, and mach all represent speed, the remaining
    /// three are recalculated each time one of them is set (using the
    /// current altitude).  The most recent speed set is remembered so 
    /// that if and when altitude is reset, the last set speed is used 
    /// to recalculate the remaining three. Setting any of the body 
    /// components forces a recalculation of vt and vt then becomes the
    /// most recent speed set.
    ///  
    /// Alpha,Gamma, and Theta:
    /// 
    /// This class assumes that it will be used to set up the sim for a
    /// steady, zero pitch rate condition. Since any two of those angles 
    /// specifies the third gamma (flight path angle) is favored when setting
    /// alpha and theta and alpha is favored when setting gamma. i.e.
    ///  
    /// - set alpha : recalculate theta using gamma as currently set
    /// - set theta : recalculate alpha using gamma as currently set
    /// - set gamma : recalculate theta using alpha as currently set
    /// 
    /// The idea being that gamma is most interesting to pilots (since it 
    /// is indicative of climb rate). 
    /// Setting climb rate is, for the purpose of this discussion,
    /// considered equivalent to setting gamma.
    /// 
    /// These are the items that can be set in an initialization file:
    /// 
    /// - ubody (velocity, ft/sec)
    /// - vbody (velocity, ft/sec)
    /// - wbody (velocity, ft/sec)
    /// - latitude (position, degrees)
    /// - longitude (position, degrees)
    /// - phi (orientation, degrees)
    /// - theta (orientation, degrees)
    /// - psi (orientation, degrees)
    /// - alpha (angle, degrees)
    /// - beta (angle, degrees)
    /// - gamma (angle, degrees)
    /// - roc (vertical velocity, ft/sec)
    /// - altitude (altitude, ft)
    /// - winddir (wind from-angle, degrees)
    /// - vwind (magnitude wind speed, ft/sec)
    /// - hwind (headwind speed, knots)
    /// - xwind (crosswind speed, knots)
    /// - vc (calibrated airspeed, ft/sec)
    /// - mach (mach)
    /// - vground (ground speed, ft/sec)
    /// - running (0 or 1)
    /// Setting climb rate is, for the purpose of this discussion, 
    /// considered equivalent to setting gamma.
    /// @author Tony Peden
    /// 
    /// </summary>
	public class InitialCondition
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


		public InitialCondition(FDMExecutive fdmex)
		{
			if(fdmex != null ) 
			{

				vt=vc=ve=vg=0;
				mach=0;
				alpha=beta=gamma=0;
				theta=phi=psi=0;
				altitude=hdot=0;
				latitude=longitude=0;
				u=v=w=0;
				p=q=r=0;
				uw=vw=ww=0;
				vnorth=veast=vdown=0;
				wnorth=weast=wdown=0;
				whead=wcross=0;
				wdir=wmag=0;
				lastSpeedSet = SpeedSet.setvt;
				lastWindSet = WindSet.setwned;
				sea_level_radius = fdmex.Inertial.RefRadius();
				radius_to_vehicle = fdmex.Inertial.RefRadius();
				terrain_altitude = 0;

				salpha=sbeta=stheta=sphi=spsi=sgamma=0;
				calpha=cbeta=ctheta=cphi=cpsi=cgamma=1;

				FDMExec=fdmex;
                FDMExec.Propagate.Altitude = altitude;
				FDMExec.Atmosphere.Run();
				PropertyManager=FDMExec.PropertyManager;
				Bind();
			}
			else 
			{
				if (log.IsErrorEnabled)
					log.Error("InitialCondition: This class requires a pointer to a valid FDMExecutive object");
			}
		}


        /// <summary>
        /// sets/gets calibrated airspeed initial condition in knots.
        /// </summary>
        [ScriptAttribute("ic/vc-kts", "(read/write) Calibrated airspeed initial condition in knots")]
        public double VcalibratedKtsIC
        {
            get { return vc * Constants.fpstokts; }
            set
            {

                if (getMachFromVcas(ref mach, value * Constants.ktstofps))
                {
                    //cout << "Mach: " << mach << endl;
                    lastSpeedSet = SpeedSet.setvc;
                    vc = value * Constants.ktstofps;
                    vt = mach * FDMExec.Atmosphere.SoundSpeed;
                    ve = vt * Math.Sqrt(FDMExec.Atmosphere.DensityRatio);
                    //cout << "Vt: " << vt*fpstokts << " Vc: " << vc*fpstokts << endl;
                }
                else
                {
                    if (log.IsErrorEnabled)
                    {
                        log.Error("Failed to get Mach number for given Vc and altitude, Vc unchanged.");
                        log.Error("Please mail the set of initial conditions used to apeden@earthlink.net");
                    }
                }
            }
        }

        /// <summary>
        /// sets/gets equivalent airspeed initial condition in knots.
        /// </summary>
        [ScriptAttribute("ic/ve-kts", "(read/write) Knots equivalent airspeed initial condition")]
        public double VequivalentKtsIC
        {
            get { return ve * Constants.fpstokts; }
            set
            {
                ve = value * Constants.ktstofps;
                lastSpeedSet = SpeedSet.setve;
                vt = ve * 1 / Math.Sqrt(FDMExec.Atmosphere.DensityRatio);
                mach = vt / FDMExec.Atmosphere.SoundSpeed;
                vc = calcVcas(mach);
            }
        }

        /// <summary>
        /// sets/gets ground speed initial condition in knots.
        /// </summary>
        [ScriptAttribute("ic/vg-kts", "(read/write) Ground speed initial condition in knots")]
        public double VgroundKtsIC
        {
            get { return vg * Constants.fpstokts; }
            set { VgroundFpsIC = value * Constants.ktstofps; }
        }

        /// <summary>
        /// sets/gets true airspeed initial condition in knots.
        /// </summary>
        [ScriptAttribute("ic/vt-kts", "(read/write) True airspeed initial condition in knots")]
        public double VtrueKtsIC
        {
            get { return vt * Constants.fpstokts; }
            set { VtrueFpsIC = value * Constants.ktstofps; }
        }

        /// <summary>
        /// sets/gets mach initial condition.
        /// </summary>
        [ScriptAttribute("ic/mach", "(read/write) Mach initial condition")]
        public double MachIC
        {
            get { return mach; }
            set
            {
                mach = value;
                lastSpeedSet = SpeedSet.setmach;
                vt = mach * FDMExec.Atmosphere.SoundSpeed;
                vc = calcVcas(mach);
                ve = vt * Math.Sqrt(FDMExec.Atmosphere.DensityRatio);
            }
        }

        /// <summary>
        /// sets/gets angle of attack initial condition in degrees.
        /// </summary>
        [ScriptAttribute("ic/alpha-deg", "(read/write) Angle of attack initial condition in degrees")]
        public double AlphaDegIC
        {
            get { return alpha * Constants.radtodeg; }
            set { AlphaRadIC = value * Constants.degtorad; }
        }

        /// <summary>
        /// sets/gets angle of sideslip initial condition in degrees.
        /// </summary>
        [ScriptAttribute("ic/beta-deg", "(read/write) Angle of sideslip initial condition in degrees")]
        public double BetaDegIC
        {
            get { return beta * Constants.radtodeg; }
            set { BetaRadIC = value * Constants.degtorad; }
        }

        /// <summary>
        /// sets/gets pitch angle initial condition in degrees.
        /// </summary>
        [ScriptAttribute("ic/theta-deg", "(read/write) Pitch angle initial condition in degrees")]
        public double ThetaDegIC
        {
            get { return theta * Constants.radtodeg; }
            set { ThetaRadIC = value * Constants.degtorad; }
        }

        /// <summary>
        /// sets/gets the roll angle initial condition in degrees.
        /// </summary>
        [ScriptAttribute("ic/phi-deg", "(read/write) Roll angle initial condition in degree")]
        public double PhiDegIC
        {
            get { return phi * Constants.radtodeg; }
            set { SetRollAngleRadIC(value * Constants.degtorad); }
        }

        /// <summary>
        /// sets/gets the heading angle initial condition in degrees.
        /// </summary>
        [ScriptAttribute("ic/psi-true-deg", "(read/write) Heading angle initial condition in degrees")]
        public double PsiDegIC
        {
            get { return psi * Constants.radtodeg; }
            set { SetTrueHeadingRadIC(value * Constants.degtorad); }
        }
        
        /// <summary>
        /// sets/gets the climb rate initial condition in feet/minute.
        /// </summary>
        [ScriptAttribute("ic/roc-fpm", "(read/write) Rate of climb initial condition in feet/minute")]
        public double ClimbRateFpmIC
        {
            get { return hdot * 60; }
            set { SetClimbRateFpsIC(value / 60.0); }
        }

        /// <summary>
        /// sets/gets the flight path angle initial condition in degrees.
        /// </summary>
        [ScriptAttribute("ic/gamma-deg", "(read/write) Flightpath angle initial condition in degrees")]
        public double FlightPathAngleDegIC
        {
            get { return gamma * Constants.radtodeg; }
            set { SetFlightPathAngleRadIC(value * Constants.degtorad); }
        }

        /// <summary>
        /// sets/gets the altitude initial condition in feet.
        /// </summary>
        [ScriptAttribute("ic/h-sl-ft", "(read/write) Height above sea level initial condition in feet")]
        public double AltitudeFtIC
        {
            get { return altitude; }
            set
            {
                altitude = value;
                FDMExec.Propagate.Altitude = altitude;
                FDMExec.Atmosphere.Run();
                //lets try to make sure the user gets what they intended

                switch (lastSpeedSet)
                {
                    case SpeedSet.setned:
                    case SpeedSet.setuvw:
                    case SpeedSet.setvt:
                        VtrueKtsIC = vt * Constants.fpstokts;
                        break;
                    case SpeedSet.setvc:
                        VcalibratedKtsIC = vc * Constants.fpstokts;
                        break;
                    case SpeedSet.setve:
                        VequivalentKtsIC = ve * Constants.fpstokts;
                        break;
                    case SpeedSet.setmach:
                        MachIC = mach;
                        break;
                    case SpeedSet.setvg:
                        VgroundFpsIC = vg;
                        break;
                }
            }
        }

        /// <summary>
        /// sets/gets the initial Altitude above ground level.
        /// </summary>
        [ScriptAttribute("ic/h-agl-ft", "(read/write) Height above ground level initial condition in feet")]
        public double AltitudeAGLFtIC
        {
            get { return altitude - terrain_altitude; }
            set
            {
                FDMExec.Propagate.DistanceAGL = value;
                altitude = FDMExec.Propagate.Altitude;
                AltitudeFtIC = altitude;
            }

        }

        /// <summary>
        /// sets/gets the initial sea level radius from planet center
        /// </summary>
        [ScriptAttribute("ic/sea-level-radius-ft", "(read/write) Height above ground level initial condition in feet")]
        public double SeaLevelRadiusFtIC
        {
            get { return sea_level_radius; }
            set { sea_level_radius = value; }
        }

        /// <summary>
        /// sets/gets the initial terrain elevation.
        /// </summary>
        [ScriptAttribute("ic/terrain-altitude-ft", "(read/write) Terrain elevation above sea level in feet")]
        public double TerrainAltitudeFtIC
        {
            get { return terrain_altitude; }
            set { terrain_altitude = value; }
        }

        /// <summary>
        /// sets/gets  the initial latitude in degrees.
        /// </summary>
        [ScriptAttribute("ic/lat-gc-deg", "(read/write) Latitude initial condition in degrees")]
        public double LatitudeDegIC
        {
            get { return latitude * Constants.radtodeg; }
            set { latitude = value * Constants.degtorad; }
        }

        /// <summary>
        /// sets/gets the initial longitude in degrees.
        /// </summary>
        [ScriptAttribute("ic/long-gc-deg", "(read/write) Longitude initial condition in degrees")]
        public double LongitudeDegIC
        {
            get { return longitude * Constants.radtodeg; }
            set { longitude = value * Constants.degtorad; }
        }

        /// <summary>
        /// sets/gets the initial ground speed in feet/second.
        /// </summary>
        [ScriptAttribute("ic/vg-fps", "(read/write) Ground speed initial condition in feet/second")]
        public double VgroundFpsIC
        {
            get { return vg; }
            set
            {
                double ua, va, wa;
                double vxz;

                vg = value;
                lastSpeedSet = SpeedSet.setvg;
                vnorth = vg * Math.Cos(psi); veast = vg * Math.Sin(psi); vdown = 0;
                calcUVWfromNED();
                ua = u + uw; va = v + vw; wa = w + ww;
                vt = Math.Sqrt(ua * ua + va * va + wa * wa);
                alpha = beta = 0;
                vxz = Math.Sqrt(u * u + w * w);
                if (w != 0) alpha = Math.Atan2(w, u);
                if (vxz != 0) beta = Math.Atan2(v, vxz);
                mach = vt / FDMExec.Atmosphere.SoundSpeed;
                vc = calcVcas(mach);
                ve = vt * Math.Sqrt(FDMExec.Atmosphere.DensityRatio);
            }
        }

        /// <summary>
        /// sets/gets the initial true airspeed in feet/second.
        /// </summary>
        [ScriptAttribute("ic/vt-fps", "(read/write) True airspeed initial condition in feet/second")]
        public double VtrueFpsIC
        {
            get { return vt; }
            set
            {
                vt = value;
                lastSpeedSet = SpeedSet.setvt;
                mach = vt / FDMExec.Atmosphere.SoundSpeed;
                vc = calcVcas(mach);
                ve = vt * Math.Sqrt(FDMExec.Atmosphere.DensityRatio);
            }
        }

        /// <summary>
        /// sets/gets the initial body axis X velocity in feet/second.
        /// </summary>
        [ScriptAttribute("ic/u-fps", "Body frame x-axis velocity initial condition in feet/second")]
        public double UBodyFpsIC
        {
            get
            {
                if (lastSpeedSet == SpeedSet.setvg)
                    return u;
                else
                    return vt * calpha * cbeta - uw;
            }
            set
            {
                u = value;
                vt = Math.Sqrt(u * u + v * v + w * w);
                lastSpeedSet = SpeedSet.setuvw;
            }
        }

        /// <summary>
        /// sets/gets the initial body axis Y velocityin feet/second.
        /// </summary>
        [ScriptAttribute("ic/v-fps", "Body frame y-axis velocity initial condition in feet/second")]
        public double VBodyFpsIC
        {
            get
            {
                if (lastSpeedSet == SpeedSet.setvg)
                    return v;
                else
                {
                    return vt * sbeta - vw;
                }
            }
            set
            {
                v = value;
                vt = Math.Sqrt(u * u + v * v + w * w);
                lastSpeedSet = SpeedSet.setuvw;
            }
        }

        /// <summary>
        /// sets/gets the initial body axis Z velocity in feet/second.
        /// </summary>
        [ScriptAttribute("ic/w-fps", "Body frame z-axis velocity initial condition in feet/second")]
        public double WBodyFpsIC
        {
            get
            {
                if (lastSpeedSet == SpeedSet.setvg)
                    return w;
                else
                    return vt * salpha * cbeta - ww;
            }
            set
            {
                w = value;
                vt = Math.Sqrt(u * u + v * v + w * w);
                lastSpeedSet = SpeedSet.setuvw;
            }
        }


        public void SetVnorthFpsIC(double tt)
        {
            vnorth = tt;
            calcUVWfromNED();
            vt = Math.Sqrt(u * u + v * v + w * w);
            lastSpeedSet = SpeedSet.setned;
        }

        public void SetVeastFpsIC(double tt)
        {
            veast = tt;
            calcUVWfromNED();
            vt = Math.Sqrt(u * u + v * v + w * w);
            lastSpeedSet = SpeedSet.setned;
        }

        public void SetVdownFpsIC(double tt)
        {
            vdown = tt;
            calcUVWfromNED();
            vt = Math.Sqrt(u * u + v * v + w * w);
            SetClimbRateFpsIC(-1 * vdown);
            lastSpeedSet = SpeedSet.setned;
        }

        /// <summary>
        /// Gets the initial body axis X wind velocity in feet/second.
        /// </summary>
        [ScriptAttribute("ic/vw-bx-fps", "(read) Wind velocity initial condition in Body X frame in feet/second")]
        public double WindUFpsIC { get { return uw; } }
        
        /// <summary>
        /// Gets the initial body axis Y wind velocity in feet/second.
        /// </summary>
        [ScriptAttribute("ic/vw-by-fps", "(read) Wind velocity initial condition in Body Y frame in feet/second")]
        public double WindVFpsIC { get { return vw; } }
        
        /// <summary>
        /// Gets the initial body axis Z wind velocity in feet/second.
        /// </summary>
        [ScriptAttribute("ic/vw-bz-fps", "(read) Wind velocity initial condition in Body Z frame in feet/second")]
        public double WindWFpsIC { get { return ww; } }
        
        /// <summary>
        /// sets/gets the initial body axis Z velocity in feet/second.
        /// </summary>
        [ScriptAttribute("ic/vw-north-fps", "(read) Wind northward velocity initial condition in feet/second")]
        public double WindNFpsIC { get { return wnorth; } }
        
        /// <summary>
        /// sets/gets the initial body axis Z velocity in feet/second.
        /// </summary>
        [ScriptAttribute("ic/vw-east-fps", "(read) Wind eastward velocity initial condition in feet/second")]
        public double WindEFpsIC { get { return weast; } }
        
        /// <summary>
        /// sets/gets the initial body axis Z velocity in feet/second.
        /// </summary>
        [ScriptAttribute("ic/vw-down-fps", "(read) Wind downward velocity initial condition in feet/second")]
        public double WindDFpsIC { get { return wdown; } }
        
        /// <summary>
        /// gets the initial total wind velocity in feet/sec.
        /// </summary>
        [ScriptAttribute("ic/vw-mag-fps", "(read) Wind velocity magnitude initial condition in feet/sec")]
        public double WindFpsIC { get { return Math.Sqrt(wnorth * wnorth + weast * weast); } }

        /// <summary>
        /// gets/sets the initial body axis roll rate in radians/second.
        /// </summary>
        [ScriptAttribute("ic/p-rad_sec", "(read/write) Roll rate initial condition in radians/second")]
        public double PRadpsIC
        {
            get { return p; }
            set { p = value; }
        }

        /// <summary>
        /// gets/sets the initial body axis pitch rate in radians/second.
        /// </summary>
        [ScriptAttribute("ic/q-rad_sec", "(read/write) Pitch rate initial condition in radians/second")]
        public double QRadpsIC
        {
            get { return q; }
            set { q = value; }
        }

        /// <summary>
        /// gets/sets the initial body axis yaw rate in radians/second.
        /// </summary>
        [ScriptAttribute("ic/r-rad_sec", "(read/write) Yaw rate initial condition in radians/second")]
        public double RRadpsIC
        {
            get { return r; }
            set { r = value; }
        }

        public void SetClimbRateFpsIC(double tt)
        {

            if (vt > 0.1)
            {
                hdot = tt;
                gamma = Math.Asin(hdot / vt);
                sgamma = Math.Sin(gamma); cgamma = Math.Cos(gamma);
            }
        }
        public double GetWindDirDegIC()
        {
            if (weast != 0.0)
                return Math.Atan2(weast, wnorth) * Constants.radtodeg;
            else if (wnorth > 0)
                return 0.0;
            else
                return 180.0;
        }

        public double GetClimbRateFpsIC() { return hdot; }


        public void SetWindDirDegIC(double dir)
        {
            wdir = dir * Constants.degtorad;
            lastWindSet = WindSet.setwmd;
            calcWindUVW();
            if (lastSpeedSet == SpeedSet.setvg)
                VgroundFpsIC = vg;
        }



  
		public void SetWindNEDFpsIC(double wN, double wE, double wD)
		{
			wnorth = wN; weast = wE; wdown = wD;
			lastWindSet = WindSet.setwned;
			calcWindUVW();
			if(lastSpeedSet == SpeedSet.setvg)
				VgroundFpsIC = vg;
		}
 
		public void SetWindMagKtsIC(double mag)
		{
			wmag=mag*Constants.ktstofps;
			lastWindSet = WindSet.setwmd;
			calcWindUVW();
			if(lastSpeedSet == SpeedSet.setvg)
				VgroundFpsIC = vg;
		}
 
		public void SetHeadWindKtsIC(double head)
		{
			whead=head*Constants.ktstofps;
			lastWindSet = WindSet.setwhc;
			calcWindUVW();
			if(lastSpeedSet == SpeedSet.setvg)
                VgroundFpsIC = vg;

		}

		public void SetCrossWindKtsIC(double cross) // positive from left
		{
			wcross=cross*Constants.ktstofps;
			lastWindSet = WindSet.setwhc;
			calcWindUVW();
			if(lastSpeedSet == SpeedSet.setvg)
				VgroundFpsIC = vg;

		}
 
		public void SetWindDownKtsIC(double wD)
		{
			wdown=wD;
			calcWindUVW();
			if(lastSpeedSet == SpeedSet.setvg)
				VgroundFpsIC = vg;
		}


		public void SetFlightPathAngleRadIC(double tt)
		{
			gamma=tt;
			sgamma=Math.Sin(gamma); cgamma=Math.Cos(gamma);
			getTheta();
			hdot=vt*sgamma;
		}



		public void SetRollAngleRadIC(double tt)
		{
			phi=tt;
			sphi=Math.Sin(phi); cphi=Math.Cos(phi);
			getTheta();
		}

		public void SetTrueHeadingRadIC(double tt)
		{
			psi=tt;
			spsi=Math.Sin(psi); cpsi=Math.Cos(psi);
			calcWindUVW();
		}

		public double GetFlightPathAngleRadIC() { return gamma; }

        [ScriptAttribute("ic/alpha-rad", "The initial alpha in radians.")]
        public double AlphaRadIC
        {
            get { return alpha; }
            set
            {
                alpha = value;
                salpha = Math.Sin(alpha); calpha = Math.Cos(alpha);
                getTheta();
            }
        }

        [ScriptAttribute("ic/beta-rad", "The initial beta in radians.")]
        public double BetaRadIC
        {
            get { return beta; }
            set
            {
                beta = value;
                sbeta = Math.Sin(beta); cbeta = Math.Cos(beta);
                getTheta();
            }
        }

        [ScriptAttribute("ic/theta-rad", "The initial pitch angle in radians.")]
        public double ThetaRadIC
        {
            get { return theta; }
            set
            {
                theta = value;
                stheta = Math.Sin(theta); ctheta = Math.Cos(theta);
                getAlpha();
            }
        }

		//public double GetAlphaRadIC() { return alpha; }
		//public double GetPitchAngleRadIC() { return theta; }
		//public double GetBetaRadIC() { return beta; }
        /*public void SetAlphaRadIC(double tt)
        {
            alpha = tt;
            salpha = Math.Sin(alpha); calpha = Math.Cos(alpha);
            getTheta();
        }
        public void SetBetaRadIC(double tt)
        {
            beta = tt;
            sbeta = Math.Sin(beta); cbeta = Math.Cos(beta);
            getTheta();
        }
         

        public void SetPitchAngleRadIC(double tt)
        {
            theta = tt;
            stheta = Math.Sin(theta); ctheta = Math.Cos(theta);
            getAlpha();
        }
*/

		public double GetRollAngleRadIC() { return phi; }
		public double GetHeadingRadIC()  { return psi; }

		//public double GetLatitudeRadIC() { return latitude; }
		//public double GetLongitudeRadIC() { return longitude; }
        //public void SetLatitudeRadIC(double tt) { latitude = tt; }
        //public void SetLongitudeRadIC(double tt) { longitude = tt; }

        [ScriptAttribute("ic/lat-gc-rad", "The initial latitude in radians.")]
        public double LatitudeRadIC
        {
            get { return latitude; }
            set { latitude = value; }
        }

        [ScriptAttribute("ic/long-gc-rad", "The initial longitude in radians.")]
        public double LongitudeRadIC
        {
            get { return longitude; }
            set { longitude = value; }
        }

		public double GetThetaRadIC() { return theta; }
		public double GetPhiRadIC() { return phi; }
		public double GetPsiRadIC() { return psi; }

		public SpeedSet GetSpeedSet() { return lastSpeedSet; }
		public WindSet GetWindSet() { return lastWindSet; }
  

        public void Load(string rstfile, bool useStoredPath)
        {
            string resetDef, acpath;
            string sep = "/";

            if (useStoredPath)
            {
                acpath = FDMExec.AircraftPath + sep + FDMExec.ModelName;
                resetDef = acpath + sep + rstfile + ".xml";
            }
            else
            {
                resetDef = rstfile;
            }


            try
            {
                XmlTextReader reader = new XmlTextReader(resetDef);
                XmlDocument doc = new XmlDocument();
                // load the data into the dom
                doc.Load(reader);
                XmlNodeList childNodes = doc.GetElementsByTagName("initialize");
                Load(childNodes[0] as XmlElement);
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                {
                    log.Error("Exception reading IC reset file: " + e);
                }
            }
        }
        public void Load(XmlElement root)
        {
            Load(root, true);
        }

        public void Load(XmlElement root, bool mustRun)
        {
            double tmp;
            try
            {
                foreach (XmlNode currentNode in root.ChildNodes)
                {
                    if (currentNode.NodeType == XmlNodeType.Element)
                    {
                        XmlElement currentElement = (XmlElement)currentNode;

                        if (currentElement.LocalName.Equals("ubody"))
                        {
                            tmp = FormatHelper.ValueAsNumberConvertTo(currentElement, "FT/SEC");
                            UBodyFpsIC = tmp;
                        }
                        else if (currentElement.LocalName.Equals("vbody"))
                        {
                            tmp = FormatHelper.ValueAsNumberConvertTo(currentElement, "FT/SEC");
                            VBodyFpsIC = tmp;
                        }
                        else if (currentElement.LocalName.Equals("wbody"))
                        {
                            tmp = FormatHelper.ValueAsNumberConvertTo(currentElement, "FT/SEC");
                            WBodyFpsIC = tmp;
                        }
                        else if (currentElement.LocalName.Equals("latitude"))
                        {
                            tmp = FormatHelper.ValueAsNumberConvertTo(currentElement, "DEG");
                            LatitudeDegIC = tmp;
                        }
                        else if (currentElement.LocalName.Equals("longitude"))
                        {
                            tmp = FormatHelper.ValueAsNumberConvertTo(currentElement, "DEG");
                            LongitudeDegIC = tmp;
                        }
                        else if (currentElement.LocalName.Equals("phi"))
                        {
                            tmp = FormatHelper.ValueAsNumberConvertTo(currentElement, "DEG");
                            PhiDegIC = tmp;
                        }
                        else if (currentElement.LocalName.Equals("theta"))
                        {
                            tmp = FormatHelper.ValueAsNumberConvertTo(currentElement, "DEG");
                            ThetaDegIC = tmp;
                        }
                        else if (currentElement.LocalName.Equals("psi"))
                        {
                            tmp = FormatHelper.ValueAsNumberConvertTo(currentElement, "DEG");
                            PsiDegIC = tmp;
                        }
                        else if (currentElement.LocalName.Equals("alpha"))
                        {
                            tmp = FormatHelper.ValueAsNumberConvertTo(currentElement, "DEG");
                            AlphaDegIC = tmp;
                        }
                        else if (currentElement.LocalName.Equals("beta"))
                        {
                            tmp = FormatHelper.ValueAsNumberConvertTo(currentElement, "DEG");
                            BetaDegIC = tmp;
                        }
                        else if (currentElement.LocalName.Equals("gamma"))
                        {
                            tmp = FormatHelper.ValueAsNumberConvertTo(currentElement, "DEG");
                            FlightPathAngleDegIC = tmp;
                        }
                        else if (currentElement.LocalName.Equals("roc"))
                        {
                            tmp = FormatHelper.ValueAsNumberConvertTo(currentElement, "FT/SEC");
                            ClimbRateFpmIC = tmp;
                        }
                        else if (currentElement.LocalName.Equals("altitude"))
                        {
                            tmp = FormatHelper.ValueAsNumberConvertTo(currentElement, "FT");
                            AltitudeFtIC = tmp;
                        }
                        else if (currentElement.LocalName.Equals("winddir"))
                        {
                            tmp = FormatHelper.ValueAsNumberConvertTo(currentElement, "DEG");
                            SetWindDirDegIC(tmp);
                        }
                        else if (currentElement.LocalName.Equals("vwind"))
                        {
                            tmp = FormatHelper.ValueAsNumberConvertTo(currentElement, "FT/SEC");
                            SetWindMagKtsIC(tmp);
                        }
                        else if (currentElement.LocalName.Equals("hwind"))
                        {
                            tmp = FormatHelper.ValueAsNumberConvertTo(currentElement, "KTS");
                            SetHeadWindKtsIC(tmp);
                        }
                        else if (currentElement.LocalName.Equals("xwind"))
                        {
                            tmp = FormatHelper.ValueAsNumberConvertTo(currentElement, "KTS");
                            SetCrossWindKtsIC(tmp);
                        }
                        else if (currentElement.LocalName.Equals("vc"))
                        {
                            tmp = FormatHelper.ValueAsNumberConvertTo(currentElement, "FT/SEC");
                            VcalibratedKtsIC = tmp;
                        }
                        else if (currentElement.LocalName.Equals("mach"))
                        {
                            tmp = FormatHelper.ValueAsNumber(currentElement);
                            MachIC = tmp;
                        }
                        else if (currentElement.LocalName.Equals("vground"))
                        {
                            tmp = FormatHelper.ValueAsNumberConvertTo(currentElement, "FT/SEC");
                            VgroundKtsIC = tmp;
                        }
                        else if (currentElement.LocalName.Equals("running"))
                        {
                            throw new NotImplementedException("TODO. Not implemented running");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                {
                    log.Error("Exception reading IC reset file: " + e);
                }
            }

             if (mustRun) FDMExec.RunIC();
        }

        public virtual void Bind()
        {
            FDMExec.PropertyManager.Bind("", this);
        }

        public virtual void Unbind()
        {
            FDMExec.PropertyManager.Unbind("", this);
        }


  
		private double vt,vc,ve,vg;
		private double mach;
		private double altitude,hdot;
		private double latitude,longitude;
		private double u,v,w;
		private double p,q,r;
		private double uw,vw,ww;
		private double vnorth,veast,vdown;
		private double wnorth,weast,wdown;
		private double whead, wcross, wdir, wmag;
		private double sea_level_radius;
		private double terrain_altitude;
		private double radius_to_vehicle;

		private double  alpha, beta, theta, phi, psi, gamma;
		private double salpha,sbeta,stheta,sphi,spsi,sgamma;
		private double calpha,cbeta,ctheta,cphi,cpsi,cgamma;

		private double xlo, xhi,xmin,xmax;

		private delegate double fp(double x);
		private fp sfunc;

		private SpeedSet lastSpeedSet;
		private WindSet lastWindSet;

		private FDMExecutive FDMExec;
		private PropertyManager PropertyManager;

		private bool getAlpha()
		{
			bool result=false;
			double guess=theta-gamma;

			if(vt < 0.01) return false;

			xlo=xhi=0;
			xmin=FDMExec.Aerodynamics.AlphaCLMin;
			xmax=FDMExec.Aerodynamics.AlphaCLMax;
			sfunc= new fp(GammaEqOfAlpha);
			if(findInterval(0,guess))
			{
				if(solve(ref alpha,0))
				{
					result=true;
					salpha=Math.Sin(alpha);
					calpha=Math.Cos(alpha);
				}
			}
			calcWindUVW();
			return result;
		}

		private bool getTheta() 
		{
			bool result=false;
			double guess=alpha+gamma;

			if(vt < 0.01) return false;

			xlo=xhi=0;
			xmin=-89;xmax=89;
			sfunc= new fp(GammaEqOfTheta);
			if(findInterval(0,guess))
			{
				if(solve(ref theta,0))
				{
					result=true;
					stheta=Math.Sin(theta);
					ctheta=Math.Cos(theta);
				}
			}
			calcWindUVW();
			return result;
		}

		private bool getMachFromVcas(ref double Mach,double vcas)
		{

			bool result=false;
			double guess=1.5;
			xlo=xhi=0;
			xmin=0;xmax=50;
			sfunc= new fp(calcVcas);
			if(findInterval(vcas,guess)) 
			{
				if(solve(ref mach, vcas))
					result=true;
			}
			return result;
		}

		private double GammaEqOfTheta(double Theta)
		{
			double a,b,c;
			double sTheta,cTheta;

			//theta=Theta; stheta=Math.Math.Sin(theta); ctheta=Math.Cos(theta);
			sTheta=Math.Sin(Theta); cTheta=Math.Cos(Theta);
			calcWindUVW();
			a=wdown + vt*calpha*cbeta + uw;
			b=vt*sphi*sbeta + vw*sphi;
			c=vt*cphi*salpha*cbeta + ww*cphi;
			return vt*sgamma - ( a*sTheta - (b+c)*cTheta);
		}

		private double GammaEqOfAlpha(double Alpha)
		{
			double a,b,c;
			double sAlpha,cAlpha;
			sAlpha=Math.Sin(Alpha); cAlpha=Math.Cos(Alpha);
			a=wdown + vt*cAlpha*cbeta + uw;
			b=vt*sphi*sbeta + vw*sphi;
			c=vt*cphi*sAlpha*cbeta + ww*cphi;

			return vt*sgamma - ( a*stheta - (b+c)*ctheta );
		}

		private double calcVcas(double Mach)
		{

			double p     = FDMExec.Atmosphere.Pressure;
			double psl   = FDMExec.Atmosphere.PressureSeaLevel;
			double rhosl = FDMExec.Atmosphere.DensitySeaLevel;
			double pt,A,B,D,vcas;
			if(Mach < 0) Mach=0;
			if(Mach < 1)    //calculate total pressure assuming isentropic flow
				pt=p*Math.Pow((1 + 0.2*Mach*Mach),3.5);
			else 
			{
				// shock in front of pitot tube, we'll assume its normal and use
				// the Rayleigh Pitot Tube Formula, i.e. the ratio of total
				// pressure behind the shock to the static pressure in front


				//the normal shock assumption should not be a bad one -- most supersonic
				//aircraft place the pitot probe out front so that it is the forward
				//most point on the aircraft.  The real shock would, of course, take
				//on something like the shape of a rounded-off cone but, here again,
				//the assumption should be good since the opening of the pitot probe
				//is very small and, therefore, the effects of the shock curvature
				//should be small as well. AFAIK, this approach is fairly well accepted
				//within the aerospace community

				B = 5.76*Mach*Mach/(5.6*Mach*Mach - 0.8);

				// The denominator above is zero for Mach ~ 0.38, for which
				// we'll never be here, so we're safe

				D = (2.8*Mach*Mach-0.4)*0.4167;
				pt = p*Math.Pow(B,3.5)*D;
			}

			A = Math.Pow(((pt-p)/psl+1),0.28571);
			vcas = Math.Sqrt(7*psl/rhosl*(A-1));
			//cout << "calcVcas: vcas= " << vcas*fpstokts << " mach= " << Mach << " pressure: " << pt << endl;
			return vcas;
		}

		private void calcUVWfromNED()
		{
			u=vnorth*ctheta*cpsi +
				veast*ctheta*spsi -
				vdown*stheta;
			v=vnorth*( sphi*stheta*cpsi - cphi*spsi ) +
				veast*( sphi*stheta*spsi + cphi*cpsi ) +
				vdown*sphi*ctheta;
			w=vnorth*( cphi*stheta*cpsi + sphi*spsi ) +
				veast*( cphi*stheta*spsi - sphi*cpsi ) +
				vdown*cphi*ctheta;
		}

		private void calcWindUVW()
		{

			switch(lastWindSet) 
			{
				case WindSet.setwmd:
					wnorth=wmag*Math.Cos(wdir);
					weast=wmag*Math.Sin(wdir);
					break;
				case WindSet.setwhc:
					wnorth=whead*Math.Cos(psi) + wcross*Math.Cos(psi+Math.PI/2.0);
					weast=whead*Math.Sin(psi) + wcross*Math.Sin(psi+Math.PI/2.0);
					break;
				case WindSet.setwned:
					break;
			}
			uw=wnorth*ctheta*cpsi +
				weast*ctheta*spsi -
				wdown*stheta;
			vw=wnorth*( sphi*stheta*cpsi - cphi*spsi ) +
				weast*( sphi*stheta*spsi + cphi*cpsi ) +
				wdown*sphi*ctheta;
			ww=wnorth*(cphi*stheta*cpsi + sphi*spsi) +
				weast*(cphi*stheta*spsi - sphi*cpsi) +
				wdown*cphi*ctheta;
		}

		private bool findInterval(double x,double guess)
		{
			//void find_interval(inter_params &ip,eqfunc f,double y,double constant, int &flag){

			int i=0;
			bool found = false;
			double flo,fhi,fguess;
			double lo,hi,step;
			step=0.1;
			fguess = sfunc(guess)-x;
			lo=hi=guess;
			do 
			{
				step=2*step;
				lo-=step;
				hi+=step;
				if(lo < xmin) lo=xmin;
				if(hi > xmax) hi=xmax;
				i++;
				flo = sfunc(lo)-x;
				fhi = sfunc(hi)-x;
				if(flo*fhi <=0) 
				{  //found interval with root
					found=true;
					if(flo*fguess <= 0) 
					{  //narrow interval down a bit
						hi=lo+step;    //to pass solver interval that is as
						//small as possible
					}
					else if(fhi*fguess <= 0) 
					{
						lo=hi-step;
					}
				}
				//cout << "FindInterval: i=" << i << " Lo= " << lo << " Hi= " << hi << endl;
			}
			while((!found) && (i <= 100));
			xlo=lo;
			xhi=hi;
			return found;
		}

		private const double relax = 0.9;
		private bool solve(ref double y, double x)
		{
			double x1,x2,x3,f1,f2,f3,d,d0;
			double eps=1E-5;
			int i;
			bool success=false;

			//initializations
			d=1;
			x2 = 0;
			x1=xlo;x3=xhi;
			f1= sfunc(x1)-x;
			f3= sfunc(x3)-x;
			d0= Math.Abs(x3-x1);

			//iterations
			i=0;
			while ((Math.Abs(d) > eps) && (i < 100)) 
			{
				d=(x3-x1)/d0;
				x2 = x1-d*d0*f1/(f3-f1);

				f2 = sfunc(x2)-x;
				//cout << "Solve x1,x2,x3: " << x1 << "," << x2 << "," << x3 << endl;
				//cout << "                " << f1 << "," << f2 << "," << f3 << endl;

				if(Math.Abs(f2) <= 0.001) 
				{
					x1=x3=x2;
				} 
				else if(f1*f2 <= 0.0) 
				{
					x3=x2;
					f3=f2;
					f1=relax*f1;
				} 
				else if(f2*f3 <= 0) 
				{
					x1=x2;
					f1=f2;
					f3=relax*f3;
				}
				//cout << i << endl;
				i++;
			}//end while
			if(i < 100) 
			{
				success=true;
				y=x2;
			}

			//cout << "Success= " << success << " Vcas: " << vcas*fpstokts << " Mach: " << x2 << endl;
			return success;
		}

        /*
        public void SetVgroundFpsIC(double tt)
        {
            double ua, va, wa;
            double vxz;

            vg = tt;
            lastSpeedSet = SpeedSet.setvg;
            vnorth = vg * Math.Cos(psi); veast = vg * Math.Sin(psi); vdown = 0;
            calcUVWfromNED();
            ua = u + uw; va = v + vw; wa = w + ww;
            vt = Math.Sqrt(ua * ua + va * va + wa * wa);
            alpha = beta = 0;
            vxz = Math.Sqrt(u * u + w * w);
            if (w != 0) alpha = Math.Atan2(w, u);
            if (vxz != 0) beta = Math.Atan2(v, vxz);
            mach = vt / FDMExec.Atmosphere.SoundSpeed;
            vc = calcVcas(mach);
            ve = vt * Math.Sqrt(FDMExec.Atmosphere.DensityRatio);
        }
        */
        // DELETE DELETE DELETE
        //public void SetPitchAngleDegIC(double tt) { ThetaRadIC = tt * Constants.degtorad; }
        //public  double GetPitchAngleDegIC() { return theta*Constants.radtodeg; }

        //public void SetClimbRateFpmIC(double tt) { SetClimbRateFpsIC(tt / 60.0); }
        //public void SetFlightPathAngleDegIC(double tt) { SetFlightPathAngleRadIC(tt * Constants.degtorad); }


        //public  double GetLatitudeDegIC() { return latitude*Constants.radtodeg; }
        //public  double GetLongitudeDegIC() { return longitude*Constants.radtodeg; }
        //public void SetLatitudeDegIC(double tt) { latitude = tt * Constants.degtorad; }
        //public void SetLongitudeDegIC(double tt) { longitude = tt * Constants.degtorad; }
        /*
            public void SetAltitudeFtIC(double tt)
            {
                altitude=tt;
                FDMExec.Propagate.Seth(altitude);
                FDMExec.Atmosphere.Run();
                //lets try to make sure the user gets what they intended

                switch(lastSpeedSet) 
                {
                    case SpeedSet.setned:
                    case SpeedSet.setuvw:
                    case SpeedSet.setvt:
                        VtrueKtsIC = vt*Constants.fpstokts;
                        break;
                    case SpeedSet.setvc:
                        VcalibratedKtsIC = vc*Constants.fpstokts;
                        break;
                    case SpeedSet.setve:
                        VequivalentKtsIC = ve*Constants.fpstokts;
                        break;
                    case SpeedSet.setmach:
                        MachIC = mach;
                        break;
                    case SpeedSet.setvg:
                        SetVgroundFpsIC(vg);
                        break;
                }
            }
            public void SetAltitudeAGLFtIC(double tt)
            {
                FDMExec.Propagate.SetDistanceAGL(tt);
                altitude=FDMExec.Propagate.Altitude;
                SetAltitudeFtIC(altitude);
            }
            public void SetSeaLevelRadiusFtIC(double tt)
            {
                sea_level_radius = tt;
            }

            public void SetTerrainAltitudeFtIC(double tt)
            {
                terrain_altitude=tt;
            } 
            */

        //public double GetSeaLevelRadiusFtIC() { return sea_level_radius; }
        //public double GetTerrainAltitudeFtIC() { return terrain_altitude; }
        //public  double GetClimbRateFpmIC() { return hdot*60; }
        //public  double GetFlightPathAngleDegIC() { return gamma*Constants.radtodeg; }
  

        //public  double GetAltitudeFtIC() { return altitude; }
        //public  double GetAltitudeAGLFtIC() { return altitude - terrain_altitude; }
        //public  void SetAlphaDegIC(double tt)      { SetAlphaRadIC(tt*Constants.degtorad); }
        //public  void SetBetaDegIC(double tt)       { SetBetaRadIC(tt*Constants.degtorad);}
        //public double GetAlphaDegIC() { return alpha * Constants.radtodeg; }
        //public double GetBetaDegIC() { return beta * Constants.radtodeg; }

        //public double GetVcalibratedKtsIC() { return vc*Constants.fpstokts; }
        //public  double GetVequivalentKtsIC() { return ve*Constants.fpstokts; }
        //public  double GetVgroundKtsIC() { return vg*Constants.fpstokts; }
        //public  double GetVtrueKtsIC() { return vt*Constants.fpstokts; }
        //public  double GetMachIC() { return mach; }
        //public double GetRollAngleDegIC() { return phi * Constants.radtodeg; }
        //public double GetHeadingDegIC() { return psi * Constants.radtodeg; }

        //public  void SetRollAngleDegIC(double tt)  { SetRollAngleRadIC(tt*Constants.degtorad);}
        //public  void SetTrueHeadingDegIC(double tt){ SetTrueHeadingRadIC(tt*Constants.degtorad); }

        /*
                public void SetVcalibratedKtsIC(double tt)
                {

                    if (getMachFromVcas(ref mach, tt * Constants.ktstofps))
                    {
                        //cout << "Mach: " << mach << endl;
                        lastSpeedSet = SpeedSet.setvc;
                        vc = tt * Constants.ktstofps;
                        vt = mach * FDMExec.Atmosphere.SoundSpeed;
                        ve = vt * Math.Sqrt(FDMExec.Atmosphere.DensityRatio);
                        //cout << "Vt: " << vt*fpstokts << " Vc: " << vc*fpstokts << endl;
                    }
                    else
                    {
                        if (log.IsErrorEnabled)
                        {
                            log.Error("Failed to get Mach number for given Vc and altitude, Vc unchanged.");
                            log.Error("Please mail the set of initial conditions used to apeden@earthlink.net");
                        }
                    }
                }

                public void SetVequivalentKtsIC(double tt)
                {
                    ve = tt * Constants.ktstofps;
                    lastSpeedSet = SpeedSet.setve;
                    vt = ve * 1 / Math.Sqrt(FDMExec.Atmosphere.DensityRatio);
                    mach = vt / FDMExec.Atmosphere.SoundSpeed;
                    vc = calcVcas(mach);
                }
                public void SetVgroundKtsIC(double tt) { SetVgroundFpsIC(tt * Constants.ktstofps); }
                public void SetVtrueKtsIC(double tt) { SetVtrueFpsIC(tt * Constants.ktstofps); }

                public void SetMachIC(double tt)
                {
                    mach = tt;
                    lastSpeedSet = SpeedSet.setmach;
                    vt = mach * FDMExec.Atmosphere.SoundSpeed;
                    vc = calcVcas(mach);
                    ve = vt * Math.Sqrt(FDMExec.Atmosphere.DensityRatio);
                }
                public double GetUBodyFpsIC()
                {
                    if (lastSpeedSet == SpeedSet.setvg)
                        return u;
                    else
                        return vt * calpha * cbeta - uw;
                }
                public void SetUBodyFpsIC(double tt)
                {
                    u = tt;
                    vt = Math.Sqrt(u * u + v * v + w * w);
                    lastSpeedSet = SpeedSet.setuvw;
                }
                
                public double GetVBodyFpsIC()
                {
                    if (lastSpeedSet == SpeedSet.setvg)
                        return v;
                    else
                    {
                        return vt * sbeta - vw;
                    }
                }
                
                public void SetVBodyFpsIC(double tt)
                {
                    v = tt;
                    vt = Math.Sqrt(u * u + v * v + w * w);
                    lastSpeedSet = SpeedSet.setuvw;
                }
                

                public double GetWBodyFpsIC()
                {
                    if (lastSpeedSet == SpeedSet.setvg)
                        return w;
                    else
                        return vt * salpha * cbeta - ww;
                }
                public void SetWBodyFpsIC(double tt)
                {
                    w = tt;
                    vt = Math.Sqrt(u * u + v * v + w * w);
                    lastSpeedSet = SpeedSet.setuvw;
                }
                public double GetWindUFpsIC() { return uw; }
                public double GetWindVFpsIC() { return vw; }
                public double GetWindWFpsIC() { return ww; }
                public double GetWindNFpsIC() { return wnorth; }
                public double GetWindEFpsIC() { return weast; }
                public double GetWindDFpsIC() { return wdown; }
                public double GetWindFpsIC() { return Math.Sqrt(wnorth * wnorth + weast * weast); }
                */

                //public double GetVgroundFpsIC() { return vg; }
                //public double GetVtrueFpsIC() { return vt; }

        /*
        public void SetVgroundFpsIC(double tt)
        {
            double ua,va,wa;
            double vxz;

            vg=tt;
            lastSpeedSet=SpeedSet.setvg;
            vnorth = vg*Math.Cos(psi); veast = vg*Math.Sin(psi); vdown = 0;
            calcUVWfromNED();
            ua = u + uw; va = v + vw; wa = w + ww;
            vt = Math.Sqrt( ua*ua + va*va + wa*wa );
            alpha = beta = 0;
            vxz = Math.Sqrt( u*u + w*w );
            if( w != 0 ) alpha = Math.Atan2( w, u );
            if( vxz != 0 ) beta = Math.Atan2( v, vxz );
            mach=vt/FDMExec.Atmosphere.SoundSpeed;
            vc=calcVcas(mach);
            ve=vt*Math.Sqrt(FDMExec.Atmosphere.DensityRatio);
        }
        public void SetVtrueFpsIC(double tt)
        {
            vt=tt;
            lastSpeedSet=SpeedSet.setvt;
            mach=vt/FDMExec.Atmosphere.SoundSpeed;
            vc=calcVcas(mach);
            ve=vt*Math.Sqrt(FDMExec.Atmosphere.DensityRatio);
        }

        public double GetPRadpsIC() { return p; }
        public double GetQRadpsIC() { return q; }
        public double GetRRadpsIC() { return r; }
        public void SetPRadpsIC(double tt) { p = tt; }
        public void SetQRadpsIC(double tt) { q = tt; }
        public void SetRRadpsIC(double tt) { r = tt; }
        */



        private const string IdSrc = "$Id: FGInitialCondition.cpp,v 1.63 2004/04/27 11:37:48 jberndt Exp $";
	}
}
