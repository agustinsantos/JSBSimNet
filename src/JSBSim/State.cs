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

namespace JSBSim
{
	using System;

	// Import log4net classes.
	using log4net;

	using JSBSim.Script;
	using CommonUtils.MathLib;

	/// <summary>
	/// Encapsulates the calculation of aircraft state.
	///  This code is based on FGState written by Jon S. Berndt
	/// </summary>
	public class State
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
		public State(FDMExecutive exec)
		{
			FDMExec = exec;

			sim_time = 0.0;
			dt = 1.0/120.0;

			Bind();
		}

		/// <summary>
		/// Suspends the simulation and sets the delta T to zero.
		/// </summary>
        public void SuspendIntegration() 
		{
			saved_dt = dt;
			dt = 0.0;
		}

		/// <summary>
		/// Resumes the simulation by resetting delta T to the correct value.
		/// </summary>
        public void ResumeIntegration() 
		{
			dt = saved_dt;
		}

        /// <summary>
        /// Returns the simulation suspension state.
        /// return true if suspended, false if executing
        /// </summary>
        public bool IsIntegrationSuspended 
        {
            get {return dt == 0.0;}
        }

		/// <summary>
		/// Gets/Sets the current sim time in seconds..
		/// </summary>
		[ScriptAttribute("sim-time-sec", "The current sim time in seconds")]
		public double SimTime 
		{
			get { return sim_time; }
			set { sim_time = value;	}
		}


		/// <summary>
		/// Gets/Sets the integration time step for the simulation executive.
		/// the time step or delta time is defined in seconds.
		/// </summary>
		[ScriptAttribute("delta-time-sec", "The integration time step for the simulation executive")]
		public double  DeltaTime
		{
			get { return dt;}
			set { dt = value; }
		}

		/// <summary>
		/// Increments the simulation time.
		/// </summary>
		/// <returns>the new simulation time.</returns>
		public double IncrTime() 
		{
			sim_time+=dt;
			return sim_time;
		}

		
		/// <summary>
		/// Initializes the simulation state based on parameters from an Initial Conditions object.
		/// </summary>
		/// <param name="initCond">an initial conditions object.</param>
		public void Initialize(InitialCondition initCond)
		{
#if TODO
			sim_time = 0.0;

			FDMExec.Propagate.SetInitialState( initCond );
            FDMExec.Atmosphere.Run(false);
			FDMExec.Atmosphere.SetWindNED( initCond.WindNFpsIC,
				initCond.WindEFpsIC,
				initCond.WindDFpsIC);

            Vector3D vAeroUVW;
			vAeroUVW = FDMExec.Propagate.GetUVW() + FDMExec.Propagate.GetTl2b()*FDMExec.Atmosphere.GetWindNED();

			double alpha, beta;
			if (vAeroUVW.W != 0.0)
				alpha = vAeroUVW.U*vAeroUVW.U > 0.0 ? Math.Atan2(vAeroUVW.W, vAeroUVW.U) : 0.0;
			else
				alpha = 0.0;
			if (vAeroUVW.V != 0.0)
				beta = vAeroUVW.U*vAeroUVW.U+vAeroUVW.W*vAeroUVW.W > 0.0 ? 
					Math.Atan2(vAeroUVW.V, (Math.Abs(vAeroUVW.U)/vAeroUVW.U)*Math.Sqrt(vAeroUVW.U*vAeroUVW.U + vAeroUVW.W*vAeroUVW.W)) : 0.0;
			else
				beta = 0.0;

			FDMExec.Auxiliary.SetAB(alpha, beta);

			double Vt = vAeroUVW.GetMagnitude();
			FDMExec.Auxiliary.Vt =Vt;

			FDMExec.Auxiliary.Mach = Vt/FDMExec.Atmosphere.SoundSpeed;

			double qbar = 0.5*Vt*Vt*FDMExec.Atmosphere.Density;
			FDMExec.Auxiliary.Qbar = qbar;
#endif 
            throw new NotImplementedException("Pending upgrade to lastest version of JSBSIM");
		} 


		/// <summary>
		/// Calculates and returns the stability-to-body axis transformation matrix.
		/// </summary>
		/// <returns>the stability-to-body transformation matrix.</returns>
		public Matrix3D GetTs2b()
		{
			double ca, cb, sa, sb;

			double alpha = FDMExec.Auxiliary.Getalpha();
			double beta  = FDMExec.Auxiliary.Getbeta();

			ca = Math.Cos(alpha);
			sa = Math.Sin(alpha);
			cb = Math.Cos(beta);
			sb = Math.Sin(beta);

			mTs2b.M11 = ca*cb;
			mTs2b.M12 = -ca*sb;
			mTs2b.M13 = -sa;
			mTs2b.M21 = sb;
			mTs2b.M22 = cb;
			mTs2b.M23 = 0.0;
			mTs2b.M31 = sa*cb;
			mTs2b.M32 = -sa*sb;
			mTs2b.M33 = ca;

			return mTs2b;
		}


		/// <summary>
		/// Calculates and returns the body-to-stability axis transformation matrix.
		/// </summary>
		/// <returns>the stability-to-body transformation matrix.</returns>
		public Matrix3D GetTb2s()
		{
			double alpha,beta;
			double ca, cb, sa, sb;

			alpha = FDMExec.Auxiliary.Getalpha();
			beta  = FDMExec.Auxiliary.Getbeta();

			ca = Math.Cos(alpha);
			sa = Math.Sin(alpha);
			cb = Math.Cos(beta);
			sb = Math.Sin(beta);

			mTb2s.M11 = ca*cb;
			mTb2s.M12 = sb;
			mTb2s.M13 = sa*cb;
			mTb2s.M21 = -ca*sb;
			mTb2s.M22 = cb;
			mTb2s.M23 = -sa*sb;
			mTb2s.M31 = -sa;
			mTb2s.M32 = 0.0;
			mTb2s.M33 = ca;

			return mTb2s;
		}

		/// <summary>
		/// Prints a summary of simulator state (speed, altitude,
		/// configuration, etc.)
		/// </summary>
		public void ReportState()
		{
			//TODO
		} 

		public void Bind()
		{
			FDMExec.PropertyManager.Bind("" ,this);
		}

		public void Unbind()
		{
			FDMExec.PropertyManager.Unbind("" ,this);
		}


		private double sim_time, dt;
		private double saved_dt;

		private FDMExecutive FDMExec;

		private Matrix3D mTs2b;
		private Matrix3D mTb2s;

		private const string IdSrc = "$Id: FGState.cpp,v 1.137 2004/05/21 12:52:54 frohlich Exp $";
	}
}
