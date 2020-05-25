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
	using CommonUtils.MathLib;

	// Import log4net classes.
	using log4net;

	using JSBSim.Script;

	/// <summary>
	/// Encapsulates various uncategorized scheduled functions.
	/// Pilot sensed accelerations are calculated here. This is used
	/// for the coordinated turn ball instrument. Motion base platforms sometimes
	/// use the derivative of pilot sensed accelerations as the driving parameter,
	/// rather than straight accelerations.
	/// 
	/// The theory behind pilot-sensed calculations is presented:
	/// 
	/// For purposes of discussion and calculation, assume for a minute that the
	/// pilot is in space and motionless in inertial space. She will feel
	/// no accelerations. If the aircraft begins to accelerate along any axis or
	/// axes (without rotating), the pilot will sense those accelerations. If
	/// any rotational moment is applied, the pilot will sense an acceleration
	/// due to that motion in the amount:
	/// 
	/// [wdot X R]  +  [w X (w X R)]
	/// Term I          Term II
	/// 
	/// where:
	/// 
	/// wdot = omegadot, the rotational acceleration rate vector
	/// w    = omega, the rotational rate vector
	/// R    = the vector from the aircraft CG to the pilot eyepoint
	/// 
	/// The sum total of these two terms plus the acceleration of the aircraft
	/// body axis gives the acceleration the pilot senses in inertial space.
	/// In the presence of a large body such as a planet, a gravity field also
	/// provides an accelerating attraction. This acceleration can be transformed
	/// from the reference frame of the planet so as to be expressed in the frame
	/// of reference of the aircraft. This gravity field accelerating attraction
	/// is felt by the pilot as a force on her tushie as she sits in her aircraft
	/// on the runway awaiting takeoff clearance.
	/// 
	/// In JSBSim the acceleration of the body frame in inertial space is given
	/// by the F = ma relation. If the vForces vector is divided by the aircraft
	/// mass, the acceleration vector is calculated. The term wdot is equivalent
	/// to the JSBSim vPQRdot vector, and the w parameter is equivalent to vPQR.
	/// The radius R is calculated below in the vector vToEyePt.
	/// 
	/// @author Tony Peden, Jon Berndt
	/// </summary>
	public class Auxiliary : Model 
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
		/// <param name="exec">the parent executive object</param>
		public Auxiliary(FDMExecutive exec) : base(exec)
		{
			Name = "Auxiliary";
			vcas = veas = pt = tat = 0;
			psl = rhosl = 1;
			earthPosAngle = 0.0;
			qbar = 0;
			qbarUW = 0.0;
			qbarUV = 0.0;
			mach = 0.0;
			alpha = beta = 0.0;
			adot = bdot = 0.0;
			gamma = vt = Vground = 0.0;
			psigt = 0.0;
			day_of_year = 1;
			seconds_in_day = 0.0;
			hoverbmac = hoverbcg = 0.0;

			/// TODO vPilotAccel.InitMatrix();
			/// TODO vPilotAccelN.InitMatrix();
			/// TODO vToEyePt.InitMatrix();
			/// TODO vAeroPQR.InitMatrix();
			/// TODO vEulerRates.InitMatrix();

			if (log.IsDebugEnabled)
				log.Debug("Instantiated: Auxiliary.");

		}


		/// <summary>
		/// Runs the Auxiliary routines; called by the Executive
		/// </summary>
		/// <returns>false if no error</returns>
        public override bool Run(bool Holding)
        {
#if TODO
            double A, B, D, hdot_Vt;
            Vector3D vPQR = FDMExec.Propagate.GetPQR();
            Vector3D vUVW = FDMExec.Propagate.GetUVW();
            Vector3D vUVWdot = FDMExec.Propagate.GetUVWdot();
            Vector3D vVel = FDMExec.Propagate.GetVel();

            if (InternalRun()) return true;
            if (FDMExec.Holding()) return false; // if paused don't execute

            p = FDMExec.Atmosphere.Pressure;
            rhosl = FDMExec.Atmosphere.DensitySeaLevel;
            psl = FDMExec.Atmosphere.PressureSeaLevel;
            sat = FDMExec.Atmosphere.Temperature;

            // Rotation

            double cTht = FDMExec.Propagate.GetCosEuler((int)EulerAngleType.eTht);
            double cPhi = FDMExec.Propagate.GetCosEuler((int)EulerAngleType.ePhi);
            double sPhi = FDMExec.Propagate.GetSinEuler((int)EulerAngleType.ePhi);

            vEulerRates.Theta = vPQR.eQ * cPhi - vPQR.eR * sPhi;
            if (cTht != 0.0)
            {
                vEulerRates.Psi = (vPQR.eQ * sPhi + vPQR.eR * cPhi) / cTht;
                vEulerRates.Phi = vPQR.eP + vEulerRates.Psi * sPhi;
            }

          //TODO  vAeroPQR = vPQR + FDMExec.Atmosphere.TurbPQR;

            // Translation

            //vAeroUVW = vUVW + FDMExec.Propagate.GetTl2b() * FDMExec.Atmosphere.GetWindNED();

            vt = vAeroUVW.GetMagnitude();
            if (vt > 0.05)
            {
                if (vAeroUVW.W != 0.0)
                    alpha = vAeroUVW.U * vAeroUVW.U > 0.0 ? Math.Atan2(vAeroUVW.W, vAeroUVW.U) : 0.0;
                if (vAeroUVW.V != 0.0)
                    beta = vAeroUVW.U * vAeroUVW.U + vAeroUVW.W * vAeroUVW.W > 0.0 ? Math.Atan2(vAeroUVW.V,
                        Math.Sqrt(vAeroUVW.U * vAeroUVW.U + vAeroUVW.W * vAeroUVW.W)) : 0.0;

                double mUW = (vAeroUVW.U * vAeroUVW.U + vAeroUVW.W * vAeroUVW.W);
                double signU = 1;
                if (vAeroUVW.U != 0.0)
                    signU = vAeroUVW.U / Math.Abs(vAeroUVW.U);

                if ((mUW == 0.0) || (vt == 0.0))
                {
                    adot = 0.0;
                    bdot = 0.0;
                }
                else
                {
                    adot = (vAeroUVW.U * vUVWdot.W - vAeroUVW.W * vUVWdot.U) / mUW;
                    bdot = (signU * mUW * vUVWdot.V - vAeroUVW.V * (vAeroUVW.U * vUVWdot.U
                        + vAeroUVW.W * vUVWdot.W)) / (vt * vt * Math.Sqrt(mUW));
                }
            }
            else
            {
                alpha = beta = adot = bdot = 0;
            }

            qbar = 0.5 * FDMExec.Atmosphere.Density * vt * vt;
            qbarUW = 0.5 * FDMExec.Atmosphere.Density * (vAeroUVW.U * vAeroUVW.U + vAeroUVW.W * vAeroUVW.W);
            qbarUV = 0.5 * FDMExec.Atmosphere.Density * (vAeroUVW.U * vAeroUVW.U + vAeroUVW.V * vAeroUVW.V);
            mach = vt / FDMExec.Atmosphere.SoundSpeed;
            machU = vMachUVW.U = vAeroUVW.U / FDMExec.Atmosphere.SoundSpeed;
            vMachUVW.V = vAeroUVW.V / FDMExec.Atmosphere.SoundSpeed;
            vMachUVW.W = vAeroUVW.W / FDMExec.Atmosphere.SoundSpeed;

            // Position

            Vground = Math.Sqrt(vVel.North * vVel.North + vVel.East * vVel.East);

            if (vVel.North == 0) psigt = 0;
            else psigt = Math.Atan2(vVel.East, vVel.North);

            if (psigt < 0.0) psigt += 2 * Math.PI;

            if (vt != 0)
            {
                hdot_Vt = -vVel.Down / vt;
                if (Math.Abs(hdot_Vt) <= 1) gamma = Math.Asin(hdot_Vt);
            }
            else
            {
                gamma = 0.0;
            }

            tat = sat * (1 + 0.2 * mach * mach); // Total Temperature, isentropic flow
            tatc = Conversion.RankineToCelsius(tat);

            if (machU < 1)
            {   // Calculate total pressure assuming isentropic flow
                pt = p * Math.Pow((1 + 0.2 * machU * machU), 3.5);
            }
            else
            {
                // Use Rayleigh pitot tube formula for normal shock in front of pitot tube
                B = 5.76 * machU * machU / (5.6 * machU * machU - 0.8);
                D = (2.8 * machU * machU - 0.4) * 0.4167;
                pt = p * Math.Pow(B, 3.5) * D;
            }

            A = Math.Pow(((pt - p) / psl + 1), 0.28571);
            if (machU > 0.0)
            {
                vcas = Math.Sqrt(7 * psl / rhosl * (A - 1));
                veas = Math.Sqrt(2 * qbar / rhosl);
            }
            else
            {
                vcas = veas = 0.0;
            }

            ///TODO vPilotAccel.InitMatrix();
            if (vt > 1.0)
            {
                vPilotAccel = FDMExec.Aerodynamics.Forces
                    + FDMExec.Propulsion.GetForces()
                    + FDMExec.GroundReactions.GetForces();
                vPilotAccel /= FDMExec.MassBalance.Mass;
                vToEyePt = FDMExec.MassBalance.StructuralToBody(FDMExec.Aircraft.EyepointXYZ);
                vPilotAccel += Vector3D.Cross(FDMExec.Propagate.GetPQRdot(), vToEyePt);
                vPilotAccel += Vector3D.Cross(vPQR, Vector3D.Cross(vPQR, vToEyePt));
            }
            else
            {
                Vector3D aux = new Vector3D(0.0, 0.0, FDMExec.Inertial.Gravity);
                vPilotAccel = FDMExec.Propagate.GetTl2b() * aux;
            }

            vPilotAccelN = vPilotAccel / FDMExec.Inertial.Gravity;

            earthPosAngle += FDMExec.State.DeltaTime * FDMExec.Inertial.Omega;

            // VRP computation
            Location vLocation = FDMExec.Propagate.GetLocation();
            Vector3D vrpStructural = FDMExec.Aircraft.VisualRefPointXYZ;
            Vector3D vrpBody = FDMExec.MassBalance.StructuralToBody(vrpStructural);
            Vector3D vrpLocal = FDMExec.Propagate.GetTb2l() * vrpBody;
            vLocationVRP = vLocation.LocalToLocation(vrpLocal);

            // Recompute some derived values now that we know the dependent parameters values ...
            hoverbcg = FDMExec.Propagate.DistanceAGL / FDMExec.Aircraft.WingSpan;

            Vector3D vMac = FDMExec.Propagate.GetTb2l() * FDMExec.MassBalance.StructuralToBody(FDMExec.Aircraft.AeroRefPointXYZ);
            hoverbmac = (FDMExec.Propagate.DistanceAGL + vMac.Z) / FDMExec.Aircraft.WingSpan;

            return false;
#endif
            throw new NotImplementedException("Pending upgrade to lastest version of JSBSIM");
        }

        // GET functions

        // Atmospheric parameters GET functions
        [ScriptAttribute("velocities/vc-fps", "Atmospheric velocities calibrated in FPS.")]
		public double VcalibratedFPS { get { return vcas; }}
		
		[ScriptAttribute("velocities/vc-kts", "Atmospheric velocities calibrated in KTS.")]
		public double VcalibratedKTS { get { return vcas*Constants.fpstokts; }}
		
		[ScriptAttribute("velocities/ve-fps", "Atmospheric velocities equivalent in FPS.")]
		public double VequivalentFPS { get { return veas; }}
		
		[ScriptAttribute("velocities/ve-kts", "Atmospheric velocities equivalent in KTS.")]
		public double VequivalentKTS { get { return veas*Constants.fpstokts; }}


		/// <summary>
		/// Total pressure above is freestream total pressure for subsonic only
		/// for supersonic it is the 1D total pressure behind a normal shock
		/// </summary>
		[ScriptAttribute("velocities/pt-lbs_sqft", "Total pressure.")]
		public double TotalPressure { get { return pt; }}
		
		[ScriptAttribute("velocities/tat-r", "Total Temperature.")]
		public double TotalTemperature { get { return tat; }}
		
		[ScriptAttribute("velocities/tat-c", "TODO: comments.velocities/tat-c.")]
		public double TAT_C { get { return tatc; }}

		[ScriptAttribute("accelerations/a-pilot-x-ft_sec2", "TODO: comments.accelerations/a-pilot-x-ft_sec2.")]
		public double PilotAccelX  { get { return vPilotAccel.X;  }}
		
		[ScriptAttribute("accelerations/a-pilot-y-ft_sec2", "TODO: comments.accelerations/a-pilot-y-ft_sec2.")]
		public double PilotAccelY  { get { return vPilotAccel.Y;  }}
		
		[ScriptAttribute("accelerations/a-pilot-z-ft_sec2", "TODO: comments.accelerations/a-pilot-z-ft_sec2.")]
		public double PilotAccelZ  { get { return vPilotAccel.Z;  }}

		public double GetNpilotVector(int idx)      { return vPilotAccelN[idx]; }
		
		[ScriptAttribute("accelerations/n-pilot-x-norm", "TODO: comments accelerations/n-pilot-x-norm.")]
		public double GetNpilotX      { get { return vPilotAccelN.X; }}
		
		[ScriptAttribute("accelerations/n-pilot-y-norm", "TODO: comments accelerations/n-pilot-y-norm.")]
		public double GetNpilotY      { get { return vPilotAccelN.Y; }}
		
		[ScriptAttribute("accelerations/n-pilot-z-norm", "TODO: comments accelerations/n-pilot-z-norm.")]
		public double GetNpilotZ      { get { return vPilotAccelN.Z; }}
			
		[ScriptAttribute("velocities/p-aero-rad_sec", "TODO: comments velocities/p-aero-rad_sec.")]
		public double GetAeroP	  { get { return vAeroPQR.X;    }}
			
		[ScriptAttribute("velocities/q-aero-rad_sec", "TODO: comments velocities/q-aero-rad_sec.")]
		public double GetAeroQ	  { get { return vAeroPQR.Y;    }}
			
		[ScriptAttribute("velocities/r-aero-rad_sec", "TODO: comments velocities/r-aero-rad_sec.")]
		public double GetAeroR	  { get { return vAeroPQR.Z;    }}

		[ScriptAttribute("velocities/phidot-rad_sec", "TODO: comments.")]
		public double EulerRatesPhi { get { return vEulerRates.Phi; }}
		
		[ScriptAttribute("velocities/thetadot-rad_sec", "TODO: comments.")]
		public double EulerRatesTheta { get { return vEulerRates.Theta; }}
		
		[ScriptAttribute("velocities/psidot-rad_sec", "TODO: comments.")]
		public double EulerRatesPsi { get { return vEulerRates.Psi; }}
		

		public double GetEulerRates(int axis) { return vEulerRates[axis]; }

		public Vector3D GetPilotAccel () { return vPilotAccel;  }
		public Vector3D GetNpilot     () { return vPilotAccelN; }
		public Vector3D GetAeroPQR    () { return vAeroPQR;     }
		

		public Vector3D GetEulerRates () { return vEulerRates;  }
		
		[ScriptAttribute("velocities/u-aero-fps", "TODO: comments.")]
		public double GetAeroU   { get { return vAeroUVW.U;     }}
		
		[ScriptAttribute("velocities/v-aero-fps", "TODO: comments.")]
		public double GetAeroV   { get { return vAeroUVW.V;     }}
		
		[ScriptAttribute("velocities/w-aero-fps", "TODO: comments.")]
		public double GetAeroW   { get { return vAeroUVW.W;     }}
		
		public Vector3D GetAeroUVW    () { return vAeroUVW;     }
		public Location GetLocationVRP() { return vLocationVRP; }

		public double GethVRP() {
#if TODO
            return vLocationVRP.Radius - FDMExec.Propagate.GetSeaLevelRadius();
#endif
            throw new NotImplementedException("Pending upgrade to lastest version of JSBSIM");
        }
        public double GetAeroUVW (int idx) { return vAeroUVW[idx]; }
		public double Getalpha   () { return alpha;      }
		public double Getbeta    () { return beta;       }
		
		[ScriptAttribute("aero/alphadot-rad_sec", "TODO: comments.")]
		public double AlphaDotRadiansSec
		{
			get { return adot;       }
			set { adot = value;		}
		}

		[ScriptAttribute("aero/alphadot-deg_sec", "TODO: comments.")]
		public double AlphaDotDegSec
		{
			get { return adot*Constants.radtodeg;       }
			set { adot = value/Constants.radtodeg;		}
		}

		[ScriptAttribute("aero/betadot-rad_sec", "TODO: comments.")]
		public double BetaDotRadiansSec
		{
			get { return bdot;       }
			set { bdot = value;		}
		}

		[ScriptAttribute("aero/betadot-deg_sec", "TODO: comments.")]
		public double BetaDotDegSec
		{
			get { return bdot*Constants.radtodeg;       }
			set { bdot = value/Constants.radtodeg;		}
		}
	
		[ScriptAttribute("aero/mag-beta-rad", "TODO: comments.")]
		public double MagBetaRandians { get { return Math.Abs(beta); }}
		
		[ScriptAttribute("aero/mag-beta-deg", "TODO: comments.")]
		public double MagBetaDegrees { get { return Math.Abs(beta*Constants.radtodeg); }}


		public double Getalpha (ConversionType unit)
		{
			if (unit == ConversionType.inDegrees)
				return alpha*Constants.radtodeg;
			else if (log.IsErrorEnabled)
				log.Error("Bad units.");
			return 0.0;
		}

		[ScriptAttribute("aero/alpha-deg", "TODO: comments.")]
		public double AlphaDegrees
		{
			get { return  alpha*Constants.radtodeg;  }
			set { alpha = value*Constants.degtorad;  }
		}
		
		[ScriptAttribute("aero/beta-deg", "TODO: comments.")]
		public double BetaDegrees
		{
			get { return  beta*Constants.radtodeg;  }
			set { beta = value*Constants.degtorad;  }
		}

		[ScriptAttribute("aero/alpha-rad", "TODO: comments.")]
		public double AlphaRadians
		{
			get { return  alpha;  }
			set { alpha = value;  }
		}
		
		[ScriptAttribute("aero/beta-rad", "TODO: comments.")]
		public double BetaRadians
		{
			get { return  beta;  }
			set { beta = value;  }
		}

		public double Getbeta (ConversionType unit)
		{
			if (unit == ConversionType.inDegrees)
				return beta*Constants.radtodeg;
			else if (log.IsErrorEnabled)
				log.Error("Bad units.");
			return 0.0;
		}

		public double Getadot (ConversionType unit)  
		{
			if (unit == ConversionType.inDegrees) return adot*Constants.radtodeg;
			else if (log.IsErrorEnabled)
				log.Error("Bad units.");
			return 0.0;
		}

		public double Getbdot (ConversionType unit)
		{
			if (unit == ConversionType.inDegrees)
				return bdot*Constants.radtodeg;
			else if (log.IsErrorEnabled)
				log.Error("Bad units.");
			return 0.0;
		}

		public double GetMagBeta (ConversionType unit) 
		{
			if (unit == ConversionType.inDegrees)
				return Math.Abs(beta)*Constants.radtodeg;
			else if (log.IsErrorEnabled)
				log.Error("Bad units.");
			return 0.0;
		}

		[ScriptAttribute("aero/qbar-psf", "TODO: comments.")]
		public double Qbar    
		{
			get { return qbar;       }
			set { qbar = value;      }
		}

		[ScriptAttribute("aero/qbarUW-psf", "TODO: comments.")]
		public double QbarUW
		{
			get { return qbarUW;     }
			set { qbarUW = value;    }
		}

		[ScriptAttribute("aero/qbarUV-psf", "TODO: comments.")]
		public double QbarUV
		{
			get { return qbarUV;     }
			set { qbarUV = value;    }
		}

		[ScriptAttribute("velocities/vt-fps", "TODO: comments.")]
		public double Vt 
		{
			get { return vt;        }
			set { vt = value;		}
		}
		
		[ScriptAttribute("velocities/vg-fps", "TODO: comments.")]
		public double VGround { get { return Vground;    }}

		[ScriptAttribute("velocities/mach", "TODO: comments.")]
		public double Mach    
		{
			get { return mach; }
			set {mach = value; }
		}

		[ScriptAttribute("velocities/machU", "TODO: comments.")]
		public double MachU   { get { return machU;      }}

		[ScriptAttribute("aero/h_b-cg-ft", "TODO: comments.")]
		public double HOverBCG { get { return hoverbcg; }}
		
		[ScriptAttribute("aero/h_b-mac-ft", "TODO: comments.")]
		public double HOverBMAC	{ get { return hoverbmac; }}

		[ScriptAttribute("flight-path/gamma-rad", "TODO: comments.")]
		public double Gamma
		{ 
			get{ return gamma;  }
			set { gamma = value;}
		}
		
		
		[ScriptAttribute("flight-path/psi-gt-rad", "TODO: comments.")]
		public double GroundTrack	{ get { return psigt;         }}

		
		[ScriptAttribute("position/epa-rad", "TODO: comments.")]
		public double EarthPositionAngle { get { return earthPosAngle; }}

		public double GetHeadWind()
		{
#if TODO
            double psiw,vw;

			psiw = FDMExec.Atmosphere.WindPsi;
			vw = FDMExec.Atmosphere.GetWindNED().GetMagnitude();

			return vw*Math.Cos(psiw - FDMExec.Propagate.GetEuler((int)EulerAngleType.ePsi));
#endif 
            throw new NotImplementedException("Pending upgrade to lastest version of JSBSIM");
		}

		public double GetCrossWind()
		{
#if TODO
            double psiw,vw;

             psiw = FDMExec.Atmosphere.WindPsi;
            vw = FDMExec.Atmosphere.GetWindNED().GetMagnitude();

            return vw *Math.Sin(psiw - FDMExec.Propagate.GetEuler((int)EulerAngleType.ePsi));
#endif
            throw new NotImplementedException("Pending upgrade to lastest version of JSBSIM");
        }

        // SET functions

        public void SetAeroUVW(Vector3D tt) { vAeroUVW = tt; }

		public void SetAB    (double t1, double t2) { alpha=t1; beta=t2; }

		// Time routines, SET and GET functions

		public void SetDayOfYear    (int doy)    { day_of_year = doy;    }
		public void SetSecondsInDay (double sid) { seconds_in_day = sid; }

		public int    GetDayOfYear    () { return day_of_year;    }
		public double GetSecondsInDay () { return seconds_in_day; }

		private double vcas, veas;
		private double rhosl, p, psl, pt, tat, sat, tatc; // Don't add a getter for pt!

		private Vector3D vPilotAccel;
		private Vector3D vPilotAccelN;
		private Vector3D vToEyePt;
		private Vector3D vAeroPQR;
		private Vector3D vAeroUVW;
		private Vector3D vEulerRates;
		private Vector3D vMachUVW;
		private Location vLocationVRP;

		private double vt, Vground, mach, machU;
		private double qbar, qbarUW, qbarUV;
		private double alpha, beta;
		private double adot,bdot;
		private double psigt, gamma;
		private double seconds_in_day;  // seconds since current GMT day began
		private int day_of_year;     // GMT day, 1 .. 366

		private double earthPosAngle;
		private double hoverbcg, hoverbmac;

		private const string IdSrc = "$Id: FGAuxiliary.cpp,v 1.62 2004/08/21 11:51:04 frohlich Exp $";
	}
}
