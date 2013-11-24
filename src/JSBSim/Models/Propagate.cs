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
    using JSBSim.InputOutput;

	public struct VehicleState 
	{
		public Location vLocation;
		public Vector3D vUVW;
		public Vector3D vPQR;
		public Quaternion vQtrn;
	};

	/// <summary>
	/// Models the EOM and integration/propagation of state
	/// @author Jon S. Berndt, Mathias Froehlich
	/// </summary>
	public class Propagate :  Model 
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
		public Propagate(FDMExecutive exec) : base(exec)
		{
			Name = "Propagate";
		}

		public override bool InitModel()
		{
			base.InitModel();

			SeaLevelRadius = FDMExec.Inertial.RefRadius();          // For initialization ONLY
			RunwayRadius   = SeaLevelRadius;

			VState.vLocation = new Location();
			VState.vQtrn = new Quaternion();
			VState.vLocation.Radius = SeaLevelRadius + 4.0;

			return true;
		}

		/// <summary>
		/// Runs the Propagate model; called by the Executive
		/// </summary>
		/// <returns>false if no error</returns>
		public override bool Run()
		{
            if (InternalRun()) return true;
            if (FDMExec.Holding()) return false; // if paused don't execute

            RecomputeRunwayRadius();

			double dt = FDMExec.State.DeltaTime*rate;  // The 'stepsize'
			Vector3D omega = new Vector3D( 0.0, 0.0, FDMExec.Inertial.Omega); // earth rotation
			Vector3D vForces = FDMExec.Aircraft.Forces;     // current forces
			Vector3D vMoments = FDMExec.Aircraft.Moments;   // current moments

			double mass = FDMExec.MassBalance.Mass;         // mass
			Matrix3D J = FDMExec.MassBalance.GetJ();        // inertia matrix
			Matrix3D Jinv = FDMExec.MassBalance.GetJinv();  // inertia matrix inverse
			double r = this.Radius;                         // radius
			if (r == 0.0) 
			{
				if(log.IsErrorEnabled)
					log.Error("radius = 0 !");
				r = 1e-16;
			} // radius check
			double rInv = 1.0/r;
			Vector3D gAccel= new Vector3D( 0.0, 0.0, FDMExec.Inertial.GetGAccel(r) );

			// The rotation matrices:
			Matrix3D Tl2b = GetTl2b();  // local to body frame
			Matrix3D Tb2l = GetTb2l();  // body to local frame
			Matrix3D Tec2l = VState.vLocation.GetTec2l();  // earth centered to local frame
			Matrix3D Tl2ec = VState.vLocation.GetTl2ec();  // local to earth centered frame

			// Inertial angular velocity measured in the body frame.
			Vector3D pqri = VState.vPQR + Tl2b*(Tec2l*omega);

			// Compute vehicle velocity wrt EC frame, expressed in Local horizontal frame.
			vVel = Tb2l * VState.vUVW;

			// First compute the time derivatives of the vehicle state values:

			// Compute body frame rotational accelerations based on the current body moments
			vPQRdot = Jinv*(vMoments - Vector3D.Cross(pqri, (J*pqri)));

			// Compute body frame accelerations based on the current body forces
			vUVWdot = Vector3D.Cross(VState.vUVW, VState.vPQR) + vForces/mass;

			// Coriolis acceleration.
			Vector3D ecVel = Tl2ec*vVel;
			Vector3D ace = 2.0*Vector3D.Cross(omega, ecVel);
			vUVWdot -= Tl2b*(Tec2l*ace);

			// Centrifugal acceleration.
            if (!FDMExec.GroundReactions.GetWOW())
            {
                Vector3D aeec = Vector3D.Cross(omega, Vector3D.Cross(omega, (Vector3D)VState.vLocation));
                vUVWdot -= Tl2b * (Tec2l * aeec);
            }

			// Gravitation accel
			vUVWdot += Tl2b*gAccel;

			// Compute vehicle velocity wrt EC frame, expressed in EC frame
			Vector3D vLocationDot = Tl2ec * vVel;

			Vector3D omegaLocal = new Vector3D( rInv*vVel.East,
			-rInv*vVel.North,
			-rInv*vVel.East*VState.vLocation.TanLatitude );

			// Compute quaternion orientation derivative on current body rates
			Quaternion vQtrndot = VState.vQtrn.GetQDot( VState.vPQR - Tl2b*omegaLocal );

			// Propagate velocities
			VState.vPQR += dt*vPQRdot;
			VState.vUVW += dt*vUVWdot;

			// Propagate positions
			VState.vQtrn += dt*vQtrndot;
			VState.vLocation += (Location)(dt*vLocationDot);

			return false;
		}

        public void RecomputeRunwayRadius()
        {
            // Get the runway radius.
            Location contactloc;
            Vector3D dvNormal, dvVel;
            GroundCallback gcb = FDMExec.GroundCallback;
            double t = FDMExec.State.SimTime;
            gcb.GetAGLevel(t, VState.vLocation, out contactloc, out dvNormal, out dvVel);
            RunwayRadius = contactloc.Radius;
        }

		public Vector3D GetVel() { return vVel; }

        [ScriptAttribute("velocities/v-north-fps", " JSBSim original GetVel")]
        public double VelocityNorth { get { return vVel.North; } }

        [ScriptAttribute("velocities/v-east-fps", " JSBSim original GetVel")]
        public double VelocityEast { get { return vVel.East; } }

        [ScriptAttribute("velocities/v-down-fps", " JSBSim original GetVel")]
        public double VelocityDown { get { return vVel.Down; } }

        public Vector3D GetUVW() { return VState.vUVW; }

        [ScriptAttribute("velocities/u-fps", " JSBSim original GetUVW")]
        public double VelocityU { get { return VState.vUVW.U; } }

        [ScriptAttribute("velocities/v-fps", " JSBSim original GetUVW")]
        public double VelocityV { get { return VState.vUVW.V; } }

        [ScriptAttribute("velocities/w-fps", " JSBSim original GetUVW")]
        public double VelocityW { get { return VState.vUVW.W; } }
        
        public Vector3D GetUVWdot() { return vUVWdot; }
		public Vector3D GetPQR() {return VState.vPQR;}

        [ScriptAttribute("velocities/p-rad_sec", " JSBSim original GetPQR")]
        public double VelocityP { get { return VState.vPQR.eP; } }

        [ScriptAttribute("velocities/q-rad_sec", " JSBSim original GetPQR")]
        public double VelocityQ { get { return VState.vPQR.eQ; } }

        [ScriptAttribute("velocities/r-rad_sec", " JSBSim original GetPQR")]
        public double VelocityR { get { return VState.vPQR.eR; } }

		public Vector3D GetPQRdot() {return vPQRdot;}

        [ScriptAttribute("accelerations/pdot-rad_sec", " JSBSim original GetPQRdot")]
        public double AccelerationPdot { get { return vPQRdot.eP; } }

        [ScriptAttribute("accelerations/qdot-rad_sec", " JSBSim original GetPQRdot")]
        public double AccelerationQdot { get { return vPQRdot.eQ; } }

        [ScriptAttribute("accelerations/rdot-rad_sec", " JSBSim original GetPQRdot")]
        public double AccelerationRdot { get {return vPQRdot.eR; }}

		public Vector3D GetEuler() { return VState.vQtrn.GetEulerAngles(); }
		public Vector3D GetCosEuler() { return VState.vQtrn.GetCosEuler(); }
		public Vector3D GetSinEuler() { return VState.vQtrn.GetSinEuler(); }

		public double GetUVW   (int idx) { return VState.vUVW[idx]; }
        public double GetUVWdot(int idx) { return vUVWdot[idx]; }


        [ScriptAttribute("accelerations/udot-fps", " JSBSim original GetUVWdot")]
        public double AccelerationUdot { get { return vUVWdot.U; } }

        [ScriptAttribute("accelerations/vdot-fps", " JSBSim original GetUVWdot")]
        public double AccelerationVdot { get { return vUVWdot.V; } }

        [ScriptAttribute("accelerations/wdot-fps", " JSBSim original GetUVWdot")]
        public double AccelerationWdot { get { return vUVWdot.W; } }
        
        public double GetVel(int idx) { return vVel[idx]; }

		/// <summary>
		/// Get/sets the altitude
		/// </summary>
        [ScriptAttribute("position/h-sl-ft", " JSBSim original Seth and Geth")]
        public double Altitude
        {
            get { return VState.vLocation.Radius - SeaLevelRadius; }
            set { VState.vLocation.Radius = value + SeaLevelRadius; }
        }
		

		public double GetPQR(int axis) {return VState.vPQR[axis];}
		public double GetPQRdot(int idx) {return vPQRdot[idx];}
		public double GetEuler(int axis) { return VState.vQtrn.GetEulerAngles()[axis]; }

        [ScriptAttribute("attitude/phi-rad", " JSBSim original GetEuler")]
        [ScriptAttribute("attitude/roll-rad", " JSBSim original GetEuler")]
        public double AttitudePhi { get { return VState.vQtrn.GetEulerAngles().Phi; } }

        [ScriptAttribute("attitude/theta-rad", " JSBSim original GetEuler")]
        [ScriptAttribute("attitude/pitch-rad", " JSBSim original GetEuler")]
        public double AttitudeTheta { get { return VState.vQtrn.GetEulerAngles().Theta; } }

        [ScriptAttribute("attitude/psi-rad", " JSBSim original GetEuler")]
        [ScriptAttribute("attitude/heading-true-rad", " JSBSim original GetEuler")]
        public double AttitudePsi { get { return VState.vQtrn.GetEulerAngles().Psi; } }
       
        public double GetCosEuler(int idx) { return VState.vQtrn.GetCosEuler()[idx]; }
		public double GetSinEuler(int idx) { return VState.vQtrn.GetSinEuler()[idx]; }


        [ScriptAttribute("velocities/h-dot-fps", " JSBSim original Gethdot")]
        public double HDot
        {
            get { return -vVel.Down; }
        }


		/// <summary>
		/// Returns the "constant" RunwayRadius.
		/// The RunwayRadius parameter is set by the calling application or set to
		/// zero if JSBSim is running in standalone mode. units feet
		/// </summary>
		/// <returns>distance of the runway from the center of the earth. </returns>
		public double GetRunwayRadius() { return RunwayRadius; }
		public double GetSeaLevelRadius() { return SeaLevelRadius; }

        [ScriptAttribute("position/h-agl-ft", " JSBSim original GetDistanceAGL/SetDistanceAGL")]
        public double DistanceAGL
        {
            get { return VState.vLocation.Radius - RunwayRadius; }
            set { VState.vLocation.Radius = value + RunwayRadius; }
        }

        [ScriptAttribute("position/long-gc-rad", " JSBSim original GetLongitude/SetLongitude")]
        public double Longitude
        { 
            get { return VState.vLocation.Longitude; }
            set { VState.vLocation.Longitude = value; }
        }

		[ScriptAttribute("position/lat-gc-rad", " JSBSim original GetLatitude/SetLatitude")]
        public double Latitude
        {
            get { return VState.vLocation.Latitude; }
            set { VState.vLocation.Latitude = value; }
        }

		public Location GetLocation() { return VState.vLocation; }
        public void SetLocation(Location l) { VState.vLocation = l; }

        [ScriptAttribute("position/radius-to-vehicle-ft", " JSBSim original GetRadius")]
        public double Radius
        {
            get { return VState.vLocation.Radius; }
            set { VState.vLocation.Radius = value; }
        }

		/// <summary>
		/// Retrieves the local-to-body transformation matrix.
		/// </summary>
		/// <returns>the local-to-body transformation matrix.</returns>
		public Matrix3D GetTl2b() { return VState.vQtrn.GetTransformationMatrix(); }


		/// <summary>
		/// Retrieves the body-to-local transformation matrix.
		/// </summary>
		/// <returns>the body-to-local matrix.</returns>
		public Matrix3D GetTb2l() { return VState.vQtrn.GetInverseTransformationMatrix(); }

		// SET functions

		public void SetRunwayRadius(double tt) { RunwayRadius = tt; }
		public void SetSeaLevelRadius(double tt) { SeaLevelRadius = tt; }


		public void SetInitialState(InitialCondition ic)
		{
			SeaLevelRadius = ic.SeaLevelRadiusFtIC;
            RunwayRadius = SeaLevelRadius;

			// Set the position lat/lon/radius
			VState.vLocation = new Location( ic.LongitudeRadIC,
				ic.LatitudeRadIC,
				ic.AltitudeFtIC + ic.SeaLevelRadiusFtIC );

			// Set the Orientation from the euler angles
			VState.vQtrn = new Quaternion( ic.GetPhiRadIC(),
				ic.GetThetaRadIC(),
				ic.GetPsiRadIC() );

			// Set the velocities in the instantaneus body frame
			VState.vUVW = new Vector3D( ic.UBodyFpsIC,
				ic.VBodyFpsIC,
				ic.WBodyFpsIC);

			// Set the angular velocities in the instantaneus body frame.
			VState.vPQR = new Vector3D( ic.PRadpsIC,
				ic.QRadpsIC,
				ic.RRadpsIC);

			// Compute some derived values.
			vVel = VState.vQtrn.GetInverseTransformationMatrix()*VState.vUVW;

			// Finaly make shure that the quaternion stays normalized.
			VState.vQtrn.Normalize();

            // Recompute the RunwayRadius level.
            RecomputeRunwayRadius();
		}


		// state vector
		private VehicleState VState = new VehicleState();

		private Vector3D vVel;
		private Vector3D vPQRdot;
		private Vector3D vUVWdot;

		private double RunwayRadius, SeaLevelRadius;

		private const string IdSrc = "$Id: FGPropagate.cpp,v 1.19 2004/12/08 01:28:42 jberndt Exp $";
	}
}
