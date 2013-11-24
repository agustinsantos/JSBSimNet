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
	using CommonUtils.MathLib;

	// Import log4net classes.
	using log4net;

	/// <summary>
	/// Utility class that aids in the conversion of forces between coordinate systems
	/// and calculation of moments.
	/// <br><h3>Resolution of Applied Forces into Moments and Body Axes Components</h3>
	/// <br><p>
	/// All forces acting on the aircraft that cannot be considered a change in weight
	/// need to be resolved into body axis components so that the aircraft acceleration
	/// vectors, both translational and rotational, can be computed. Furthermore, the
	/// moments produced by each force that does not act at a location corresponding to
	/// the center of gravity also need to be computed. Unfortunately, the math required
	/// to do this can be a bit messy and errors are easily introduced so the class
	/// FGForce was created to provide these services in a consistent and reusable
	/// manner.<br><br></p>
	/// 
	/// <h4>Basic usage</h4>
	/// 
	/// <p>Force requires that its users supply it with the location of the applied
	/// force vector in JSBSim structural coordinates, the sense of each axis in that
	/// coordinate system relative to the body system, the orientation of the vector
	/// also relative to body coordinates and, of course, the force vector itself. With
	/// this information it will compute both the body axis force components and the
	/// resulting moments. Any moments inherently produced by the native system can be
	/// supplied as well and they will be summed with those computed.</p>
	/// 
	/// <p>A good example for demonstrating the use of this class are the aerodynamic
	/// forces: lift, drag, and side force and the aerodynamic moments about the pitch,
	/// roll and yaw axes. These "native" forces and moments are computed and stored
	/// in the FGColumnVector objects vFs and vMoments. Their native coordinate system
	/// is often referred to as the wind system and is defined as a right-handed system
	/// having its x-axis aligned with the relative velocity vector and pointing towards
	/// the rear of the aircraft , the y-axis extending out the right wing, and the
	/// z-axis directed upwards. This is different than body axes; they are defined such
	/// that the x-axis is lies on the aircraft's roll axis and positive forward, the
	/// y-axis is positive out the right wing, and the z-axis is positive downwards. In
	/// this instance, JSBSim already provides the needed transform and FGForce can make
	/// use of it by calling SetTransformType() once an object is created:</p>
	/// 
	/// <p><tt>Force fgf(FDMExec);</tt><br>
	/// <tt>fgf.SetTransformType(tWindBody);</tt><br><br>
	/// 
	/// This call need only be made once for each object. The available transforms are
	/// defined in the enumerated type TransformType and are tWindBody, tLocalBody,
	/// tCustom, and tNone. The local-to-body transform, like the wind-to-body, also
	/// makes use of that already available in JSBSim. tNone sets FGForce to do no
	/// angular transform at all, and tCustom allows for modeling force vectors at
	/// arbitrary angles relative to the body system such as that produced by propulsion
	/// systems. Setting up and using a custom transform is covered in more detail below.
	/// Continuing with the example, the point of application of the aerodynamic forces,
	/// the aerodynamic reference point in JSBSim, also needs to be set:</p>
	/// <p><tt>
	/// fgf.SetLocation(x, y, z)</tt></p>
	/// 
	/// <p>where x, y, and z are in JSBSim structural coordinates.</p>
	/// 
	/// <p>Initialization is complete and the FGForce object is ready to do its job. As
	/// stated above, the lift, drag, and side force are computed and stored in the
	/// vector vFs and need to be passed to FGForce:</p>
	/// 
	/// <p><tt>fgf.SetNativeForces(vFs);</tt> </p>
	/// 
	/// <p>The same applies to the aerodynamic pitching, rolling and yawing moments:</p>
	/// 
	/// <p><tt>fgf.SetNativeMoments(vMoments);</tt></p>
	/// 
	/// <p>Note that storing the native forces and moments outside of this class is not
	/// strictly necessary, overloaded SetNativeForces() and SetNativeMoments() methods
	/// which each accept three doubles (rather than a vector) are provided and can be
	/// repeatedly called without incurring undue overhead. The body axes force vector
	/// can now be retrieved by calling:</p>
	/// 
	/// <p><tt>vFb=fgf.GetBodyForces();</tt></p>
	/// 
	/// <p>This method is where the bulk of the work gets done so calling it more than
	/// once for the same set of native forces and moments should probably be avoided.
	/// Note that the moment calculations are done here as well so they should not be
	/// retrieved after calling the GetBodyForces() method:</p>
	/// 
	/// <p><tt>vM=fgf.GetMoments();</tt> </p>
	/// 
	/// <p>As an aside, the native moments are not needed to perform the computations
	/// correctly so, if the FGForce object is not being used to store them then an
	/// alternate approach is to avoid the SetNativeMoments call and perform the sum</p>
	/// 
	/// <p><tt>vMoments+=fgf.GetMoments();</tt> <br><br>
	/// 
	/// after the forces have been retrieved. </p>
	/// 
	/// <h4>Use of the Custom Transform Type</h4>
	/// 
	/// <p>In cases where the native force vector is not aligned with the body, wind, or
	/// local coordinate systems a custom transform type is provided. A vectorable engine
	/// nozzle will be used to demonstrate its usage. Initialization is much the same:</p>
	/// 
	/// <p><tt>Force fgf(FDMExec);</tt> <br>
	/// <tt>fgf.SetTransformType(tCustom);</tt> <br>
	/// <tt>fgf.SetLocation(x,y,z);</tt> </p>
	/// 
	/// <p>Except that here the tCustom transform type is specified and the location of
	/// the thrust vector is used rather than the aerodynamic reference point. Thrust is
	/// typically considered to be positive when directed aft while the body x-axis is
	/// positive forward and, if the native system is right handed, the z-axis will be
	/// reversed as well. These differences in sense need to be specified using by the
	/// call: </p>
	/// 
	/// <p><tt>fgf.SetSense(-1,1,-1);</tt></p>
	/// 
	/// <p>The angles are specified by calling the method: </p>
	/// 
	/// <p><tt>fgf.SetAnglesToBody(pitch, roll, yaw);</tt> </p>
	/// 
	/// <p>in which the transform matrix is computed. Note that these angles should be
	/// taken relative to the body system and not the local as the names might suggest.
	/// For an aircraft with vectorable thrust, this method will need to be called
	/// every time the nozzle angle changes, a fixed engine/nozzle installation, on the
	/// other hand, will require it to be be called only once.</p>
	/// 
	/// <p>Retrieval of the computed forces and moments is done as detailed above.</p>
	/// <br>
	/// <blockquote>
	/// <p><i>CAVEAT: If the custom system is used to compute
	/// the wind-to-body transform, then the sign of the sideslip
	/// angle must be reversed when calling SetAnglesToBody().
	/// This is true because sideslip angle does not follow the right
	/// hand rule. Using the custom transform type this way
	/// should not be necessary, as it is already provided as a built
	/// in type (and the sign differences are correctly accounted for).</i>
	/// <br></p>
	/// </blockquote>
	/// 
	/// <h4>Use as a Base Type</h4>
	/// 
	/// <p>For use as a base type, the native force and moment vector data members are
	/// defined as protected. In this case the SetNativeForces() and SetNativeMoments()
	/// methods need not be used and, instead, the assignments to vFn, the force vector,
	/// and vMn, the moments, can be made directly. Otherwise, the usage is similar.<br>
	/// <br><br></p>
	/// 
	/// 	@author Tony Peden
	/// </summary>
	public class Force
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
		
		public enum TransformType { None, WindBody, LocalBody, Custom };

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="exec"></param>
		public Force(FDMExecutive exec)
		{
			FDMExec = exec;
			mT = Matrix3D.Identity;
			vSense = Vector3D.Zero;
			if (log.IsDebugEnabled)
				log.Debug("Instantiated: Force.");
		}



		public void SetNativeForces(double Fnx, double Fny, double Fnz) 
		{
			vFn.X = Fnx;
			vFn.Y = Fny;
			vFn.Z = Fnz;
		}
		public void SetNativeForces(Vector3D vv) { vFn = vv; }

		public void SetNativeMoments(double Ln,double Mn, double Nn) 
		{
			vMn.X = Ln;
			vMn.Y = Mn;
			vMn.Z = Nn;
		}
		public void SetNativeMoments(Vector3D vv) { vMn = vv; }

		public Vector3D GetNativeForces() { return vFn; }
		public Vector3D GetNativeMoments() { return vMn; }

		public Vector3D GetBodyForces()
		{
			vFb = Transform()*(Vector3D.MultiplyElements(vFn, vSense));

			// Find the distance from this vector's acting location to the cg; this
			// needs to be done like this to convert from structural to body coords.
			// CG and RP values are in inches

			vDXYZ = FDMExec.MassBalance.StructuralToBody(vActingXYZn);

			vM = vMn + Vector3D.Cross(vDXYZ, vFb);

			return vFb;
		}


		public Vector3D GetMoments() { return vM; }

		/// <summary>
		/// Normal point of application, JSBsim structural coords
		/// </summary>
		/// <param name="x">x +back inches</param>
		/// <param name="y">y +right inches</param>
		/// <param name="z">z +upinches</param>
		public void SetLocation(double x, double y, double z) 
		{
			vXYZn.X = x;
			vXYZn.Y = y;
			vXYZn.Z = z;
			SetActingLocation(x, y, z);
		}

		/// <summary>
		/// Acting point of application.
		/// JSBsim structural coords used (inches, x +back, y +right, z +up).
		/// This function sets the point at which the force acts - this may
		/// not be the same as where the object resides. One area where this
		/// is true is P-Factor modeling.
		/// </summary>
		/// <param name="x">acting location of force</param>
		/// <param name="y">acting location of force</param>
		/// <param name="z">acting location of force</param>
		public void SetActingLocation(double x, double y, double z) 
		{
			vActingXYZn.X = x;
			vActingXYZn.Y = y;
			vActingXYZn.Z = z;
		}
		public void SetLocationX(double x) {vXYZn.X = x; vActingXYZn[(int)PositionType.eX] = x;}
		public void SetLocationY(double y) {vXYZn.Y = y; vActingXYZn[(int)PositionType.eY] = y;}
		public void SetLocationZ(double z) {vXYZn.Z = z; vActingXYZn[(int)PositionType.eZ] = z;}
		public double SetActingLocationX(double x) {vActingXYZn.X = x; return x;}
		public double SetActingLocationY(double y) {vActingXYZn.Y = y; return y;}
		public double SetActingLocationZ(double z) {vActingXYZn.Z = z; return z;}
		public void SetLocation(Vector3D vv) { vXYZn = vv; SetActingLocation(vv);}
		public void SetActingLocation(Vector3D vv) { vActingXYZn = vv; }

		public double GetLocationX() { return vXYZn.X;}
		public double GetLocationY() { return vXYZn.Y;}
		public double GetLocationZ() { return vXYZn.Z;}
		public double GetActingLocationX() { return vActingXYZn.X;}
		public double GetActingLocationY() { return vActingXYZn.Y;}
		public double GetActingLocationZ() { return vActingXYZn.Z;}
		public Vector3D GetLocation() { return vXYZn; }
		public Vector3D GetActingLocation() { return vActingXYZn; }

		//these angles are relative to body axes, not earth!!!!!
		//I'm using these because pitch, roll, and yaw are easy to visualize,
		//there's no equivalent to roll in wind axes i.e. alpha, ? , beta
		//making up new names or using these is a toss-up: either way people
		//are going to get confused.
		//They are in radians.

		public void SetAnglesToBody(double broll, double bpitch, double byaw)
		{
			if (transformType == TransformType.Custom) 
			{
				double cp,sp,cr,sr,cy,sy;

				cp = Math.Cos(bpitch); sp = Math.Sin(bpitch);
				cr = Math.Cos(broll);  sr = Math.Sin(broll);
				cy = Math.Cos(byaw);   sy = Math.Sin(byaw);

				mT.M11 = cp*cy;
				mT.M12 = cp*sy;
				mT.M13 = -1*sp;

				mT.M21 = sr*sp*cy-cr*sy;
				mT.M22 = sr*sp*sy+cr*cy;
				mT.M23 = sr*cp;

				mT.M31 = cr*sp*cy+sr*sy;
				mT.M32 = cr*sp*sy-sr*cy;
				mT.M33 = cr*cp;
			}
		}
		public void  SetAnglesToBody(Vector3D vv) 
		{
			SetAnglesToBody(vv.Roll, vv.Pitch, vv.Yaw);
		}

		public void SetSense(double x, double y, double z) { vSense.X=x; vSense.Y=y; vSense.Z=z; }

		public Vector3D Sense
		{
			get{ return vSense; }
			set { vSense = value;}
		}

		public void SetTransformType(TransformType ii) { transformType=ii; }
		public TransformType GetTransformType() { return transformType; }

		public Matrix3D Transform()
		{
			switch(transformType) 
			{
				case TransformType.WindBody:
					return FDMExec.State.GetTs2b();
				case TransformType.LocalBody:
					return FDMExec.Propagate.GetTl2b();
				case TransformType.Custom:
				case TransformType.None:
					return mT;
				default:
					//cout << "Unrecognized tranform requested from Transform()" << endl;
					return Matrix3D.Zero;
			}
		}


		protected FDMExecutive FDMExec;
		protected Vector3D vFn;
		protected Vector3D vMn;
		protected Vector3D vH;

		private Vector3D vFb;
		private Vector3D vM;
		private Vector3D vXYZn;
		private Vector3D vActingXYZn;
		private Vector3D vDXYZ;
		private Vector3D vSense;

		private Matrix3D mT;

		protected TransformType transformType;
	}
}
