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
    using CommonUtils.MathLib;

    // Import log4net classes.
    using log4net;

    using JSBSim.Script;
    using JSBSim.InputOutput;
    using System.Collections.Generic;
    using CommonUtils.Collections;

    /// <summary>
    /// The current vehicle state vector structure contains the translational and
    /// angular position, and the translational and angular velocity.
    /// </summary>
    public class VehicleState
    {
        /// <summary>
        /// Represents the current location of the vehicle in Earth centered Earth
        /// fixed (ECEF) frame.
        /// units ft
        /// </summary>
		public Location vLocation = new Location();

        /// <summary>
        /// The velocity vector of the vehicle with respect to the ECEF frame,
        /// expressed in the body system.
        /// units ft/sec
        /// </summary>
		public Vector3D vUVW = new Vector3D();

        /// <summary>
        /// he angular velocity vector for the vehicle relative to the ECEF frame,
        /// expressed in the body frame.
        /// units rad/sec
        /// </summary>
		public Vector3D vPQR = new Vector3D();

        /// <summary>
        /// The angular velocity vector for the vehicle body frame relative to the
        /// ECI frame, expressed in the body frame.
        /// units rad/sec
        /// </summary>
        public Vector3D vPQRi = new Vector3D();

        /// <summary>
        /// The current orientation of the vehicle, that is, the orientation of the
        /// body frame relative to the local, NED frame.
        /// </summary>
        public Quaternion qAttitudeLocal = new Quaternion();

        /// <summary>
        /// The current orientation of the vehicle, that is, the orientation of the
        /// body frame relative to the inertial(ECI) frame.
        /// </summary>
        public Quaternion qAttitudeECI = new Quaternion();

        public Quaternion vQtrndot = new Quaternion();

        public Vector3D vInertialVelocity = new Vector3D();

        public Vector3D vInertialPosition = new Vector3D();

        public Deque<Vector3D> dqPQRidot = new Deque<Vector3D>(5);
        public Deque<Vector3D> dqUVWidot = new Deque<Vector3D>(5);
        public Deque<Vector3D> dqInertialVelocity = new Deque<Vector3D>(5);
        public Deque<Quaternion> dqQtrndot = new Deque<Quaternion>(5);
    };

    /// <summary>
    /// Models the EOM and integration/propagation of state
    /// The Equations of Motion(EOM) for JSBSim are integrated to propagate the
    /// state of the vehicle given the forces and moments that act on it.The
    /// integration accounts for a rotating Earth.
    /// 
    /// Integration of rotational and translation position and rate can be
    /// customized as needed or frozen by the selection of no integrator.The
    /// selection of which integrator to use is done through the setting of
    /// the associated property.There are four properties which can be set:
    /// 
    /// @code
    /// simulation/integrator/rate/rotational
    /// simulation/integrator/rate/translational
    /// simulation/integrator/position/rotational
    /// simulation/integrator/position/translational
    /// @endcode
    /// 
    /// Each of the integrators listed above can be set to one of the following values:
    /// 
    /// @code
    /// 0: No integrator (Freeze)
    /// 1: Rectangular Euler
    /// 2: Trapezoidal
    /// 3: Adams Bashforth 2
    /// 4: Adams Bashforth 3
    /// 5: Adams Bashforth 4
    /// @endcode
    /// 
    /// @author Jon S. Berndt, Mathias Froehlich, Bertrand Coconnier
    /// </summary>
    public class Propagate : Model
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
        /// The constructor initializes several variables, and sets the initial set
        /// of integrators to use as follows:
        /// - integrator, rotational rate = Adams Bashforth 2
        /// - integrator, translational rate = Adams Bashforth 2
        /// - integrator, rotational position = Trapezoidal
        /// - integrator, translational position = Trapezoidal
        /// </summary>
        /// <param name="exec">the parent executive object</param>
        public Propagate(FDMExecutive exec) : base(exec)
        {
            Name = "Propagate";

            Inertial = FDMExec.Inertial;

            /// These define the indices use to select the various integrators.
            // eNone = 0, eRectEuler, eTrapezoidal, eAdamsBashforth2, eAdamsBashforth3, eAdamsBashforth4};

            integrator_rotational_rate = eIntegrateType.eRectEuler;
            integrator_translational_rate = eIntegrateType.eAdamsBashforth2;
            integrator_rotational_position = eIntegrateType.eRectEuler;
            integrator_translational_position = eIntegrateType.eAdamsBashforth3;

            VState.dqPQRidot.Resize(5, new Vector3D(0.0, 0.0, 0.0));
            VState.dqUVWidot.Resize(5, new Vector3D(0.0, 0.0, 0.0));
            VState.dqInertialVelocity.Resize(5, new Vector3D(0.0, 0.0, 0.0));
            VState.dqQtrndot.Resize(5, new Quaternion(0.0, 0.0, 0.0));

            epa = 0.0;

            Bind();
            Debug(0);
        }

        /// <summary>
        /// These define the indices use to select the various integrators.
        /// </summary>
        public enum eIntegrateType
        {
            eNone = 0, eRectEuler, eTrapezoidal, eAdamsBashforth2,
            eAdamsBashforth3, eAdamsBashforth4, eBuss1, eBuss2, eLocalLinearization, eAdamsBashforth5
        };

        /// <summary>
        /// Initializes the FGPropagate class after instantiation and prior to first execution.
        /// The base class FGModel::InitModel is called first, initializing pointers to the
        /// other FGModel objects(and others).
        /// </summary>
        /// <returns></returns>
        public override bool InitModel()
        {
            if (!base.InitModel()) return false;

            // For initialization ONLY:
            VState.vLocation.SetEllipse(inputs.SemiMajor, inputs.SemiMinor);
            Inertial.SetAltitudeAGL(VState.vLocation, 4.0);

            VState.dqPQRidot.Resize(5, new Vector3D(0.0, 0.0, 0.0));
            VState.dqUVWidot.Resize(5, new Vector3D(0.0, 0.0, 0.0));
            VState.dqInertialVelocity.Resize(5, new Vector3D(0.0, 0.0, 0.0));
            VState.dqQtrndot.Resize(5, new Quaternion(0.0, 0.0, 0.0));

            integrator_rotational_rate = eIntegrateType.eRectEuler;
            integrator_translational_rate = eIntegrateType.eAdamsBashforth2;
            integrator_rotational_position = eIntegrateType.eRectEuler;
            integrator_translational_position = eIntegrateType.eAdamsBashforth3;

            epa = 0.0;

            return true;
        }

        public void InitializeDerivatives()
        {
            VState.dqPQRidot.Assign(5, inputs.vPQRidot);
            VState.dqUVWidot.Assign(5, inputs.vUVWidot);
            VState.dqInertialVelocity.Assign(5, VState.vInertialVelocity);
            VState.dqQtrndot.Assign(5, VState.vQtrndot);
        }

        /// <summary>
        /// Runs the Propagate model; called by the Executive
        /// Can pass in a value indicating if the executive is directing the simulation to Hold.
        /// </summary>
        /// <param name="Holding">if true, the executive has been directed to hold the sim from
        ///  advancing time.Some models may ignore this flag, such as the Input
        ///  model, which may need to be active to listen on a socket for the
        ///  "Resume" command to be given.</param>
        /// <returns>false if no error</returns>
        public override bool Run(bool Holding)
        {
            if (base.Run(Holding)) return true;  // Fast return if we have nothing to do ...
            if (Holding) return false;

            double dt = inputs.DeltaT * rate;  // The 'stepsize'

            // Propagate rotational / translational velocity, angular /translational position, respectively.

            if (!FDMExec.IntegrationSuspended())
            {
                Integrate(VState.qAttitudeECI, VState.vQtrndot, VState.dqQtrndot, dt, integrator_rotational_position);
                Integrate(VState.vPQRi, inputs.vPQRidot, VState.dqPQRidot, dt, integrator_rotational_rate);
                Integrate(VState.vInertialPosition, VState.vInertialVelocity, VState.dqInertialVelocity, dt, integrator_translational_position);
                Integrate(VState.vInertialVelocity, inputs.vUVWidot, VState.dqUVWidot, dt, integrator_translational_rate);
            }

            // CAUTION : the order of the operations below is very important to get
            // transformation matrices that are consistent with the new state of the
            // vehicle

            // 1. Update the Earth position angle (EPA)
            epa += inputs.vOmegaPlanet.Z * dt;

            // 2. Update the Ti2ec and Tec2i transforms from the updated EPA
            double cos_epa = Math.Cos(epa);
            double sin_epa = Math.Sin(epa);
            Ti2ec = new Matrix3D(
                cos_epa, sin_epa, 0.0,
            -sin_epa, cos_epa, 0.0,
            0.0, 0.0, 1.0);
            Tec2i = Ti2ec.GetTranspose();          // ECEF to ECI frame transform

            // 3. Update the location from the updated Ti2ec and inertial position
            VState.vLocation = new Location(Ti2ec * VState.vInertialPosition);

            // 4. Update the other "Location-based" transformation matrices from the
            //    updated vLocation vector.
            UpdateLocationMatrices();

            // 5. Update the "Orientation-based" transformation matrices from the updated
            //    orientation quaternion and vLocation vector.
            UpdateBodyMatrices();

            // Translational position derivative (velocities are integrated in the
            // inertial frame)
            CalculateUVW();

            // Set auxilliary state variables
            RecomputeLocalTerrainVelocity();

            VState.vPQR = VState.vPQRi - Ti2b * inputs.vOmegaPlanet;

            // Angular orientation derivative
            CalculateQuatdot();

            VState.qAttitudeLocal = Tl2b.GetQuaternion();

            // Compute vehicle velocity wrt ECEF frame, expressed in Local horizontal
            // frame.
            vVel = Tb2l * VState.vUVW;

            Debug(2);
            return false;
        }

        /// <summary>
        /// Retrieves the velocity vector.
        /// The vector returned is represented by an FGColumnVector reference.The vector
        /// for the velocity in Local frame is organized(Vnorth, Veast, Vdown). The vector
        /// is 1-based, so that the first element can be retrieved using the "()" operator.
        /// In other words, vVel(1) is Vnorth.Various convenience enumerators are defined
        /// in FGJSBBase.The relevant enumerators for the vector returned by this call are,
        /// eNorth= 1, eEast= 2, eDown= 3.
        /// units ft/sec
        /// </summary>
        /// <returns>The vehicle velocity vector with respect to the Earth centered frame,
        /// expressed in Local horizontal frame.</returns>
        public Vector3D GetVel() { return vVel; }

        /// <summary>
        /// Retrieves the body frame vehicle velocity vector.
        /// The vector returned is represented by an FGColumnVector3 reference. The vector
        /// for the velocity in Body frame is organized (Vx, Vy, Vz). The vector
        /// is 1-based, so that the first element can be retrieved using the "()" operator.
        /// In other words, vUVW(1) is Vx. Various convenience enumerators are defined
        /// in FGJSBBase. The relevant enumerators for the vector returned by this call are,
        /// eX=1, eY=2, eZ=3.
        /// units ft/sec     
        /// </summary>
        /// <returns>The body frame vehicle velocity vector in ft/sec.</returns>
        public Vector3D GetUVW() { return VState.vUVW; }

        /// <summary>
        /// Retrieves the body angular rates vector, relative to the ECEF frame.
        /// Retrieves the body angular rates (p, q, r), which are calculated by integration
        /// of the angular acceleration.
        /// The vector returned is represented by an FGColumnVector3 reference. The vector
        /// for the angular velocity in Body frame is organized (P, Q, R). The vector
        /// is 1-based, so that the first element can be retrieved using the "()" operator.
        /// In other words, vPQR(1) is P. Various convenience enumerators are defined
        /// in FGJSBBase. The relevant enumerators for the vector returned by this call are,
        /// eP=1, eQ=2, eR=3.
        /// units rad/sec
        /// </summary>
        /// <returns>The body frame angular rates in rad/sec.</returns>
        public Vector3D GetPQR() { return VState.vPQR; }

        /// <summary>
        /// Retrieves the body angular rates vector, relative to the ECI (inertial) frame.
        /// Retrieves the body angular rates (p, q, r), which are calculated by integration
        /// of the angular acceleration.
        /// The vector returned is represented by an FGColumnVector reference. The vector
        /// for the angular velocity in Body frame is organized (P, Q, R). The vector
        /// is 1-based, so that the first element can be retrieved using the "()" operator.
        /// In other words, vPQR(1) is P. Various convenience enumerators are defined
        /// in FGJSBBase. The relevant enumerators for the vector returned by this call are,
        /// eP=1, eQ=2, eR=3.
        /// units rad/sec
        /// </summary>
        /// <returns>The body frame inertial angular rates in rad/sec.</returns>
        public Vector3D GetPQRi() { return VState.vPQRi; }

        /// <summary>
        /// Retrieves the time derivative of the body orientation quaternion.
        /// Retrieves the time derivative of the body orientation quaternion based on
        /// the rate of change of the orientation between the body and the ECI frame.
        /// The quaternion returned is represented by an FGQuaternion reference.The
        /// quaternion is 1-based, so that the first element can be retrieved using
        /// the "()" operator.
        /// units rad/sec^2
        /// </summary>
        /// <returns>The time derivative of the body orientation quaternion.</returns>
        public Quaternion GetQuaterniondot() { return VState.vQtrndot; }

        /// <summary>
        /// Retrieves the Euler angles that define the vehicle orientation.
        /// Extracts the Euler angles from the quaternion that stores the orientation
        /// in the Local frame.The order of rotation used is Yaw-Pitch-Roll.The
        /// vector returned is represented by an FGColumnVector reference.The vector
        /// for the Euler angles is organized(Phi, Theta, Psi). The vector
        /// is 1-based, so that the first element can be retrieved using the "()" operator.
        /// In other words, the returned vector item with subscript (1) is Phi.
        /// Various convenience enumerators are defined in FGJSBBase.The relevant
        /// enumerators for the vector returned by this call are, ePhi= 1, eTht= 2, ePsi= 3.
        /// units radians
        /// </summary>
        /// <returns></returns>
        public Vector3D GetEuler() { return VState.qAttitudeLocal.GetEuler(); }

        /// <summary>
        /// Retrieves the Euler angles(in degrees) that define the vehicle orientation.
        /// Extracts the Euler angles from the quaternion that stores the orientation
        /// in the Local frame.The order of rotation used is Yaw-Pitch-Roll.The
        /// vector returned is represented by an FGColumnVector reference.The vector
        /// for the Euler angles is organized(Phi, Theta, Psi). The vector
        /// is 1-based, so that the first element can be retrieved using the "()" operator.
        /// In other words, the returned vector item with subscript (1) is Phi.
        /// Various convenience enumerators are defined in FGJSBBase.The relevant
        /// enumerators for the vector returned by this call are, ePhi= 1, eTht= 2, ePsi= 3.
        /// units degrees
        /// </summary>
        /// <returns>The Euler angle vector, where the first item in the
        /// vector is the angle about the X axis, the second is the
        /// angle about the Y axis, and the third item is the angle
        /// about the Z axis(Phi, Theta, Psi).</returns>
        public Vector3D GetEulerDeg()
        {
            return VState.qAttitudeLocal.GetEuler() * Constants.radtodeg;
        }

        /// <summary>
        /// Retrieves a body frame velocity component.
        /// Retrieves a body frame velocity component. The velocity returned is
        /// extracted from the vUVW vector (an FGColumnVector). The vector for the
        /// velocity in Body frame is organized (Vx, Vy, Vz). The vector is 1-based.
        /// In other words, GetUVW(1) returns Vx. Various convenience enumerators
        /// are defined in FGJSBBase. The relevant enumerators for the velocity
        /// returned by this call are, eX=1, eY=2, eZ=3.
        /// units ft/sec
        /// </summary>
        /// <param name="idx">the index of the velocity component desired (1-based).</param>
        /// <returns>The body frame velocity component.</returns>
        public double GetUVW(int idx) { return VState.vUVW[idx - 1]; }

        /// <summary>
        /// Retrieves a Local frame velocity component.
        /// Retrieves a Local frame velocity component. The velocity returned is
        /// extracted from the vVel vector (an FGColumnVector). The vector for the
        /// velocity in Local frame is organized (Vnorth, Veast, Vdown). The vector
        /// is 1-based. In other words, GetVel(1) returns Vnorth. Various convenience
        /// enumerators are defined in FGJSBBase. The relevant enumerators for the
        /// velocity returned by this call are, eNorth=1, eEast=2, eDown=3.
        /// units ft/sec
        /// </summary>
        /// <param name="idx">the index of the velocity component desired (1-based).</param>
        /// <returns>The body frame velocity component.</returns>
        public double GetVel(int idx) { return vVel[idx - 1]; }

        /// <summary>
        /// Retrieves the total inertial velocity in ft/sec.
        /// </summary>
        /// <returns></returns>
        public double GetInertialVelocityMagnitude() { return VState.vInertialVelocity.Magnitude(); }

        /// <summary>
        /// Retrieves the total local NED velocity in ft/sec.
        /// </summary>
        /// <returns></returns>
        public double GetNEDVelocityMagnitude() { return VState.vUVW.Magnitude(); }

        /// <summary>
        /// Retrieves the inertial velocity vector in ft/sec.
        /// </summary>
        /// <returns></returns>
        public Vector3D GetInertialVelocity() { return VState.vInertialVelocity; }
        public double GetInertialVelocity(int i) { return VState.vInertialVelocity[i - 1]; }

        /// <summary>
        /// Retrieves the inertial position vector.
        /// </summary>
        /// <returns></returns>
        public Vector3D GetInertialPosition() { return VState.vInertialPosition; }
        public double GetInertialPosition(int i) { return VState.vInertialPosition[i - 1]; }

        /// <summary>
        /// Calculates and retrieves the velocity vector relative to the earth centered earth fixed (ECEF) frame
        /// </summary>
        /// <returns></returns>
        public Vector3D GetECEFVelocity() { return Tb2ec * VState.vUVW; }

        /// <summary>
        /// Calculates and retrieves the velocity vector relative to the earth centered earth fixed (ECEF) frame
        ///     for a particular axis.
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public double GetECEFVelocity(int idx) { return (Tb2ec * VState.vUVW)[idx - 1]; }

        /// <summary>
        /// Returns the current altitude above sea level.
        /// This function returns the altitude above sea level.
        /// units ft
        /// </summary>
        /// <returns>The current altitude above sea level in feet.</returns>
        public double GetAltitudeASL()
        {
            return VState.vLocation.GeodAltitude;
        }

        /// <summary>
        /// Returns the current altitude above sea level.
        /// This function returns the altitude above sea level.
        /// units meters
        /// </summary>
        /// <returns>The current altitude above sea level in meters.</returns>
        public double GetAltitudeASLmeters() { return GetAltitudeASL() * Constants.fttom; }

        /// <summary>
        /// Retrieves a body frame angular velocity component relative to the ECEF frame.
        /// Retrieves a body frame angular velocity component.The angular velocity
        /// returned is extracted from the vPQR vector(an FGColumnVector). The vector
        /// for the angular velocity in Body frame is organized(P, Q, R). The vector
        /// is 1-based.In other words, GetPQR(1) returns P(roll rate). Various
        /// convenience enumerators are defined in FGJSBBase.The relevant enumerators
        /// for the angular velocity returned by this call are, eP= 1, eQ= 2, eR= 3.
        /// units rad/sec
        /// </summary>
        /// <param name="axis">the index of the angular velocity component desired (1-based).</param>
        /// <returns>The body frame angular velocity component.</returns>
        public double GetPQR(int axis) { return VState.vPQR[axis - 1]; }

        /// <summary>
        /// Retrieves a body frame angular velocity component relative to the ECI(inertial) frame.
        /// Retrieves a body frame angular velocity component.The angular velocity
        /// returned is extracted from the vPQR vector(an FGColumnVector). The vector
        /// for the angular velocity in Body frame is organized(P, Q, R). The vector
        /// is 1-based.In other words, GetPQR(1) returns P(roll rate). Various
        /// convenience enumerators are defined in FGJSBBase.The relevant enumerators
        /// for the angular velocity returned by this call are, eP= 1, eQ= 2, eR= 3.
        /// units rad/sec
        /// </summary>
        /// <param name="axis">the index of the angular velocity component desired (1-based).</param>
        /// <returns>The body frame angular velocity component.</returns>
        public double GetPQRi(int axis) { return VState.vPQRi[axis - 1]; }

        /// <summary>
        /// Retrieves a vehicle Euler angle component.
        /// Retrieves an Euler angle (Phi, Theta, or Psi) from the quaternion that
        /// stores the vehicle orientation relative to the Local frame. The order of
        /// rotations used is Yaw-Pitch-Roll. The Euler angle with subscript (1) is
        /// Phi. Various convenience enumerators are defined in FGJSBBase. The
        /// relevant enumerators for the Euler angle returned by this call are,
        /// ePhi=1, eTht=2, ePsi=3 (e.g. GetEuler(eTht) returns Theta).
        /// units radians
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        public double GetEuler(int axis) { return VState.qAttitudeLocal.GetEuler()[axis - 1]; }

        /// <summary>
        /// Retrieves a vehicle Euler angle component in degrees.
        /// Retrieves an Euler angle (Phi, Theta, or Psi) from the quaternion that
        /// stores the vehicle orientation relative to the Local frame. The order of
        /// rotations used is Yaw-Pitch-Roll. The Euler angle with subscript (1) is
        /// Phi. Various convenience enumerators are defined in FGJSBBase. The
        /// relevant enumerators for the Euler angle returned by this call are,
        /// ePhi=1, eTht=2, ePsi=3 (e.g. GetEuler(eTht) returns Theta).
        /// units degrees
        /// </summary>
        /// <param name="axis"></param>
        /// <returns>An Euler angle in degrees.</returns>
        public double GetEulerDeg(int axis) { return VState.qAttitudeLocal.GetEuler()[axis - 1] * Constants.radtodeg; }

        /// <summary>
        /// Retrieves the cosine of a vehicle Euler angle component.
        /// Retrieves the cosine of an Euler angle(Phi, Theta, or Psi) from the
        /// quaternion that stores the vehicle orientation relative to the Local frame.
        /// The order of rotations used is Yaw-Pitch-Roll.The Euler angle
        /// with subscript(1) is Phi.Various convenience enumerators are defined in
        /// FGJSBBase.The relevant enumerators for the Euler angle referred to in this
        /// call are, ePhi = 1, eTht = 2, ePsi = 3(e.g.GetCosEuler(eTht) returns cos(theta)).
        /// units none
        /// </summary>
        /// <param name="idx"></param>
        /// <returns>The cosine of an Euler angle.</returns>
        public double GetCosEuler(int idx) { return VState.qAttitudeLocal.GetCosEuler()[idx - 1]; }

        /// <summary>
        /// Retrieves the sine of a vehicle Euler angle component.
        /// Retrieves the sine of an Euler angle(Phi, Theta, or Psi) from the
        /// quaternion that stores the vehicle orientation relative to the Local frame.
        /// The order of rotations used is Yaw-Pitch-Roll.The Euler angle
        /// with subscript(1) is Phi.Various convenience enumerators are defined in
        /// FGJSBBase.The relevant enumerators for the Euler angle referred to in this
        /// call are, ePhi = 1, eTht = 2, ePsi = 3(e.g.GetSinEuler(eTht) returns sin(theta)).
        /// units none
        /// </summary>
        /// <param name="idx">The sine of an Euler angle.</param>
        /// <returns></returns>
        public double GetSinEuler(int idx) { return VState.qAttitudeLocal.GetSinEuler()[idx - 1]; }

        /// <summary>
        /// Returns the current altitude rate.
        /// Returns the current altitude rate (rate of climb).
        /// units ft/sec
        /// </summary>
        /// <returns>The current rate of change in altitude.</returns>
        public double Gethdot()
        {
            return HDot;
        }

        /// <summary>
        /// Returns the "constant" LocalTerrainRadius.
        /// The LocalTerrainRadius parameter is set by the calling application or set to
        /// sea level + terrain elevation if JSBSim is running in standalone mode.
        /// units feet
        /// </summary>
        /// <returns>distance of the local terrain from the center of the earth.</returns>
        public double GetLocalTerrainRadius()
        {
            Location contact;
            Vector3D vDummy;
            Inertial.GetContactPoint(VState.vLocation, out contact, out vDummy, out vDummy, out vDummy);
            return contact.Radius;
        }

        /// <summary>
        /// Returns the Earth position angle.
        /// This is the relative angle around the Z axis of the ECEF frame with
        /// respect to the inertial frame.
        /// </summary>
        /// <returns>Earth position angle in radians.</returns>
        public double GetEarthPositionAngle() { return epa; }

        /// <summary>
        /// Sets the Earth position angle.
        /// This is the relative angle around the Z axis of the ECEF frame with
        /// respect to the inertial frame.
        /// </summary>
        /// <param name="EPA">Earth position angle in radians.</param>
        void SetEarthPositionAngle(double EPA) { epa = EPA; }

        /// <summary>
        /// Returns the Earth position angle in degrees.
        /// </summary>
        /// <returns>Earth position angle in degrees.</returns>
        public double GetEarthPositionAngleDeg() { return epa * Constants.radtodeg; }


        public Vector3D GetTerrainVelocity() { return LocalTerrainVelocity; }
        public Vector3D GetTerrainAngularVelocity() { return LocalTerrainAngularVelocity; }

        public void RecomputeLocalTerrainVelocity()
        {
            Location contact = new Location();
            Vector3D normal = new Vector3D();
            Inertial.GetContactPoint(VState.vLocation, out contact, out normal,
                                     out LocalTerrainVelocity, out LocalTerrainAngularVelocity);
        }

        public double GetTerrainElevation()
        {
            Location contact;
            Vector3D vDummy;
            Inertial.GetContactPoint(VState.vLocation, out contact, out vDummy, out vDummy, out vDummy);
            return contact.GeodAltitude;
        }

        public double GetDistanceAGL()
        {
            return Inertial.GetAltitudeAGL(VState.vLocation);
        }

        public double GetDistanceAGLKm()
        {
            return GetDistanceAGL() * 0.0003048;
        }

        public double GetRadius()
        {
            if (VState.vLocation.Radius == 0) return 1.0;
            else return VState.vLocation.Radius;
        }
        public double GetLongitude() { return VState.vLocation.Longitude; }
        public double GetLatitude() { return VState.vLocation.Latitude; }

        public double GetGeodLatitudeRad() { return VState.vLocation.GeodLatitudeRad; }
        public double GetGeodLatitudeDeg() { return VState.vLocation.GeodLatitudeDeg; }

        public double GetGeodeticAltitude() { return VState.vLocation.GeodAltitude; }
        public double GetGeodeticAltitudeKm() { return VState.vLocation.GeodAltitude * 0.0003048; }

        public double GetLongitudeDeg() { return VState.vLocation.LongitudeDeg; }
        public double GetLatitudeDeg() { return VState.vLocation.LatitudeDeg; }
        Location GetLocation() { return VState.vLocation; }
        public double GetLocation(int i) { return VState.vLocation[i - 1]; }

        /// <summary>
        /// Retrieves the local-to-body transformation matrix.
        /// The quaternion class, being the means by which the orientation of the
        /// vehicle is stored, manages the local-to-body transformation matrix.
        /// </summary>
        /// <returns>the local-to-body transformation matrix.</returns>
        public Matrix3D GetTl2b() { return Tl2b; }

        /// <summary>
        /// Retrieves the body-to-local transformation matrix.
        /// The quaternion class, being the means by which the orientation of the
        /// vehicle is stored, manages the body-to-local transformation matrix.
        /// </summary>
        /// <returns>the body-to-local matrix.</returns>
        public Matrix3D GetTb2l() { return Tb2l; }

        /// <summary>
        /// Retrieves the ECEF-to-body transformation matrix.
        /// </summary>
        /// <returns>a reference to the ECEF-to-body transformation matrix.</returns>
        public Matrix3D GetTec2b() { return Tec2b; }

        /// <summary>
        /// Retrieves the body-to-ECEF transformation matrix.
        /// </summary>
        /// <returns> a reference to the body-to-ECEF matrix.</returns>
        public Matrix3D GetTb2ec() { return Tb2ec; }

        /// <summary>
        /// Retrieves the ECI-to-body transformation matrix.
        /// </summary>
        /// <returns> a reference to the ECI-to-body transformation matrix.</returns>
        public Matrix3D GetTi2b() { return Ti2b; }

        /// <summary>
        /// Retrieves the body-to-ECI transformation matrix.
        /// </summary>
        /// <returns> a reference to the body-to-ECI matrix. </returns>
        public Matrix3D GetTb2i() { return Tb2i; }

        /// <summary>
        /// Retrieves the ECEF-to-ECI transformation matrix.
        /// </summary>
        /// <see cref="SetEarthPositionAngle"/>
        /// <returns>a reference to the ECEF-to-ECI transformation matrix.</returns>
        public Matrix3D GetTec2i() { return Tec2i; }

        /// <summary>
        /// Retrieves the ECI-to-ECEF transformation matrix.
        /// </summary>
        /// <see cref="SetEarthPositionAngle"/>
        /// <returns>a reference to the ECI-to-ECEF matrix.</returns>
        public Matrix3D GetTi2ec() { return Ti2ec; }

        /// <summary>
        /// Retrieves the ECEF-to-local transformation matrix.
        /// Retrieves the ECEF-to-local transformation matrix. Note that the so-called
        /// local from is also know as the NED frame(for North, East, Down).
        /// </summary>
        /// <returns> a reference to the ECEF-to-local matrix.</returns>
        public Matrix3D GetTec2l() { return Tec2l; }

        /// <summary>
        ///  a reference to the ECEF-to-local matrix.
        ///  Retrieves the local-to-ECEF transformation matrix. Note that the so-called
        ///  local from is also know as the NED frame(for North, East, Down).
        /// </summary>
        /// <returns>a reference to the local-to-ECEF matrix. </returns>
        public Matrix3D GetTl2ec() { return Tl2ec; }

        /// <summary>
        /// Retrieves the local-to-inertial transformation matrix.
        /// </summary>
        /// <see cref="SetEarthPositionAngle"/>
        /// <returns> a reference to the local-to-inertial transformation matrix.</returns>
        public Matrix3D GetTl2i() { return Tl2i; }

        /// <summary>
        /// Retrieves the inertial-to-local transformation matrix.
        /// </summary>
        /// <see cref="SetEarthPositionAngle"/>
        /// <returns>a reference to the inertial-to-local matrix.</returns>
        public Matrix3D GetTi2l() { return Ti2l; }

        public VehicleState GetVState() { return VState; }

        public void SetVState(VehicleState vstate)
        {
            //ToDo: Shouldn't all of these be set from the vstate vector passed in?
            VState.vLocation = vstate.vLocation;
            UpdateLocationMatrices();
            SetInertialOrientation(vstate.qAttitudeECI);
            RecomputeLocalTerrainVelocity();
            VState.vUVW = vstate.vUVW;
            vVel = Tb2l * VState.vUVW;
            VState.vPQR = vstate.vPQR;
            VState.vPQRi = VState.vPQR + Ti2b * inputs.vOmegaPlanet;
            VState.vInertialPosition = vstate.vInertialPosition;
            CalculateQuatdot();
        }

        public void SetInertialOrientation(Quaternion Qi)
        {
            VState.qAttitudeECI = Qi;
            VState.qAttitudeECI.Normalize();
            UpdateBodyMatrices();
            VState.qAttitudeLocal = Tl2b.GetQuaternion();
            CalculateQuatdot();
        }

        public void SetInertialVelocity(Vector3D Vi)
        {
            VState.vInertialVelocity = Vi;
            CalculateUVW();
            vVel = Tb2l * VState.vUVW;
        }

        public void SetInertialRates(Vector3D vRates)
        {
            VState.vPQRi = Ti2b * vRates;
            VState.vPQR = VState.vPQRi - Ti2b * inputs.vOmegaPlanet;
            CalculateQuatdot();
        }

        /// <summary>
        /// Returns the quaternion that goes from Local to Body.
        /// </summary>
        /// <returns></returns>
        public Quaternion GetQuaternion() { return VState.qAttitudeLocal; }

        /// <summary>
        /// Returns the quaternion that goes from ECI to Body.
        /// </summary>
        /// <returns></returns>
        public Quaternion GetQuaternionECI() { return VState.qAttitudeECI; }

        /// <summary>
        /// Returns the quaternion that goes from ECEF to Body.
        /// </summary>
        /// <returns></returns>
        public Quaternion GetQuaternionECEF() { return Qec2b; }

        public void SetPQR(int i, double val)
        {
            VState.vPQR[i - 1] = val;
            VState.vPQRi = VState.vPQR + Ti2b * inputs.vOmegaPlanet;
        }

        public void SetUVW(int i, double val)
        {
            VState.vUVW[i - 1] = val;
            CalculateInertialVelocity();
        }

        public void SetLongitude(double lon)
        {
            VState.vLocation.Longitude = lon;
            UpdateVehicleState();
        }
        public void SetLongitudeDeg(double lon) { SetLongitude(lon * Constants.degtorad); }
        public void SetLatitude(double lat)
        {
            VState.vLocation.Latitude = lat;
            UpdateVehicleState();
        }
        public void SetLatitudeDeg(double lat) { SetLatitude(lat * Constants.degtorad); }
        public void SetRadius(double r)
        {
            VState.vLocation.Radius = r;
            VState.vInertialPosition = Tec2i * (Vector3D)VState.vLocation;
        }

        public void SetAltitudeASL(double altASL)
        {
            double geodLat = VState.vLocation.GeodLatitudeRad;
            double longitude = VState.vLocation.Longitude;
            VState.vLocation.SetPositionGeodetic(longitude, geodLat, altASL);
            UpdateVehicleState();
        }

        public void SetAltitudeASLmeters(double altASL) { SetAltitudeASL(altASL / Constants.fttom); }

        public void SetTerrainElevation(double terrainElev)
        {
            Inertial.SetTerrainElevation(terrainElev);
        }

        public void SetDistanceAGL(double tt)
        {
            Inertial.SetAltitudeAGL(VState.vLocation, tt);
            UpdateVehicleState();
        }
        public void SetDistanceAGLKm(double tt)
        {
            SetDistanceAGL(tt * 3280.8399);
        }
        public void SetInitialState(InitialCondition FGIC)
        {
#if TODO
            // Initialize the State Vector elements and the transformation matrices

            // Set the position lat/lon/radius
            VState.vLocation = FGIC.GetPosition();

            epa = FGIC.GetEarthPositionAngleIC();
            Ti2ec = new Matrix3D(
                                Math.Cos(epa), Math.Sin(epa), 0.0,
                                -Math.Sin(epa), Math.Cos(epa), 0.0,
                                0.0, 0.0, 1.0);
            Tec2i = Ti2ec.GetTranspose();          // ECEF to ECI frame transform

            VState.vInertialPosition = Tec2i * VState.vLocation;

            UpdateLocationMatrices();

            // Set the orientation from the euler angles (is normalized within the
            // constructor). The Euler angles represent the orientation of the body
            // frame relative to the local frame.
            VState.qAttitudeLocal = FGIC.GetOrientation();

            VState.qAttitudeECI = Ti2l.GetQuaternion() * VState.qAttitudeLocal;
            UpdateBodyMatrices();

            // Set the velocities in the instantaneus body frame
            VState.vUVW = FGIC.GetUVWFpsIC();

            // Compute the local frame ECEF velocity
            vVel = Tb2l * VState.vUVW;

            // Compute local terrain velocity
            RecomputeLocalTerrainVelocity();

            // Set the angular velocities of the body frame relative to the ECEF frame,
            // expressed in the body frame.
            VState.vPQR = FGIC.GetPQRRadpsIC();

            VState.vPQRi = VState.vPQR + Ti2b * inputs.vOmegaPlanet;

            CalculateInertialVelocity(); // Translational position derivative
            CalculateQuatdot();  // Angular orientation derivative
#endif
            throw new NotImplementedException("Pending upgrade to lastest version of JSBSIM");
        }

        public void SetLocation(Location l)
        {
            VState.vLocation = l;
            UpdateVehicleState();
        }

        public void SetLocation(Vector3D lv)
        {
            Location l = new Location(lv);
            SetLocation(l);
        }

        public void SetPosition(double Lon, double Lat, double Radius)
        {
            Location l = new Location(Lon, Lat, Radius);
            SetLocation(l);
        }


        public void NudgeBodyLocation(Vector3D deltaLoc)
        {
            VState.vInertialPosition -= Tb2i * deltaLoc;
            VState.vLocation -= new Location(Tb2ec * deltaLoc);
        }

        /// <summary>
        /// Sets the property forces/hold-down. This allows to do hard 'hold-down'
        /// such as for rockets on a launch pad with engines ignited.
        /// </summary>
        /// <param name="hd">enables the 'hold-down' function if non-zero</param>
        public void SetHoldDown(bool hd)
        {
            if (hd)
            {
                VState.vUVW = Vector3D.Zero;
                CalculateInertialVelocity();
                VState.vPQR = Vector3D.Zero;
                VState.vPQRi = Ti2b * inputs.vOmegaPlanet;
                CalculateQuatdot();
                InitializeDerivatives();
            }
        }

        // ----------------------->>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>


        /// <summary>
        /// Transform the velocity vector of the inertial frame to be expressed in the
        /// body frame relative to the origin (Earth center), and substract the vehicle
        /// velocity contribution due to the rotation of the planet.
        /// </summary>
        /// <param name=""></param>
        private void CalculateUVW()
        {
            VState.vUVW = Ti2b * (VState.vInertialVelocity - (inputs.vOmegaPlanet * VState.vInertialPosition));
        }

        private void Integrate(Vector3D Integrand,
                             Vector3D Val,
                             Deque<Vector3D> ValDot,
                             double dt,
                             eIntegrateType integration_type)
        {
            ValDot.AddToFront(Val);
            ValDot.RemoveFromBack();

            switch (integration_type)
            {
                case eIntegrateType.eRectEuler:
                    Integrand += dt * ValDot[0];
                    break;
                case eIntegrateType.eTrapezoidal:
                    Integrand += 0.5 * dt * (ValDot[0] + ValDot[1]);
                    break;
                case eIntegrateType.eAdamsBashforth2:
                    Integrand += dt * (1.5 * ValDot[0] - 0.5 * ValDot[1]);
                    break;
                case eIntegrateType.eAdamsBashforth3:
                    Integrand += (1 / 12.0) * dt * (23.0 * ValDot[0] - 16.0 * ValDot[1] + 5.0 * ValDot[2]);
                    break;
                case eIntegrateType.eAdamsBashforth4:
                    Integrand += (1 / 24.0) * dt * (55.0 * ValDot[0] - 59.0 * ValDot[1] + 37.0 * ValDot[2] - 9.0 * ValDot[3]);
                    break;
                case eIntegrateType.eAdamsBashforth5:
                    Integrand += dt * ((1901.0 / 720.0) * ValDot[0] - (1387.0 / 360.0) * ValDot[1] + (109.0 / 30.0) * ValDot[2] - (637.0 / 360.0) * ValDot[3] + (251.0 / 720.0) * ValDot[4]);
                    break;
                case eIntegrateType.eNone: // do nothing, freeze translational rate
                    break;
                case eIntegrateType.eBuss1:
                case eIntegrateType.eBuss2:
                case eIntegrateType.eLocalLinearization:
                    throw new Exception("Can only use Buss (1 & 2) or local linearization integration methods in for rotational position!");
                default:
                    break;
            }
        }

        private void Integrate(Quaternion Integrand,
                               Quaternion Val,
                               Deque<Quaternion> ValDot,
                               double dt,
                               eIntegrateType integration_type)
        {
            ValDot.AddToFront(Val);
            ValDot.RemoveFromBack();

            switch (integration_type)
            {
                case eIntegrateType.eRectEuler:
                    Integrand += dt * ValDot[0];
                    break;
                case eIntegrateType.eTrapezoidal:
                    Integrand += 0.5 * dt * (ValDot[0] + ValDot[1]);
                    break;
                case eIntegrateType.eAdamsBashforth2:
                    Integrand += dt * (1.5 * ValDot[0] - 0.5 * ValDot[1]);
                    break;
                case eIntegrateType.eAdamsBashforth3:
                    Integrand += (1 / 12.0) * dt * (23.0 * ValDot[0] - 16.0 * ValDot[1] + 5.0 * ValDot[2]);
                    break;
                case eIntegrateType.eAdamsBashforth4:
                    Integrand += (1 / 24.0) * dt * (55.0 * ValDot[0] - 59.0 * ValDot[1] + 37.0 * ValDot[2] - 9.0 * ValDot[3]);
                    break;
                case eIntegrateType.eAdamsBashforth5:
                    Integrand += dt * ((1901.0 / 720.0) * ValDot[0] - (1387.0 / 360.0) * ValDot[1] + (109.0 / 30.0) * ValDot[2] - (637.0 / 360.0) * ValDot[3] + (251.0 / 720.0) * ValDot[4]);
                    break;
                case eIntegrateType.eBuss1:
                    {
                        // This is the first order method as described in Samuel R. Buss paper[6].
                        // The formula from Buss' paper is transposed below to quaternions and is
                        // actually the exact solution of the quaternion differential equation
                        // qdot = 1/2*w*q when w is constant.
                        Integrand = Integrand * Quaternion.QExp(0.5 * dt * VState.vPQRi);
                    }
                    return; // No need to normalize since the quaternion exponential is always normal
                case eIntegrateType.eBuss2:
                    {
                        // This is the 'augmented second-order method' from S.R. Buss paper [6].
                        // Unlike Runge-Kutta or Adams-Bashforth, it is a one-pass second-order
                        // method (see reference [6]).
                        Vector3D wi = VState.vPQRi;
                        Vector3D wdoti = inputs.vPQRidot;
                        Vector3D omega = wi + 0.5 * dt * wdoti + dt * dt / 12.0 * wdoti * wi;
                        Integrand = Integrand * Quaternion.QExp(0.5 * dt * omega);
                    }
                    return; // No need to normalize since the quaternion exponential is always normal
                case eIntegrateType.eLocalLinearization:
                    {
                        // This is the local linearization algorithm of Barker et al. (see ref. [7])
                        // It is also a one-pass second-order method. The code below is based on the
                        // more compact formulation issued from equation (107) of ref. [8]. The
                        // constants C1, C2, C3 and C4 have the same value than those in ref. [7] pp. 11
                        Vector3D wi = 0.5 * VState.vPQRi;
                        Vector3D wdoti = 0.5 * inputs.vPQRidot;
                        double omegak2 = Vector3D.Dot(VState.vPQRi, VState.vPQRi);
                        double omegak = omegak2 > 1E-6 ? Math.Sqrt(omegak2) : 1E-6;
                        double rhok = 0.5 * dt * omegak;
                        double C1 = Math.Cos(rhok);
                        double C2 = 2.0 * Math.Sin(rhok) / omegak;
                        double C3 = 4.0 * (1.0 - C1) / (omegak * omegak);
                        double C4 = 4.0 * (dt - C2) / (omegak * omegak);
                        Vector3D Omega = C2 * wi + C3 * wdoti + C4 * wi * wdoti;
                        Quaternion q = new Quaternion();

                        q.W = C1 - C4 * Vector3D.Dot(wi, wdoti);
                        q.X = Omega.P;
                        q.Y = Omega.Q;
                        q.Z = Omega.R;

                        Integrand = Integrand * q;

                        /* Cross check with ref. [7] pp.11-12 formulas and code pp. 20
                        double pk = VState.vPQRi(eP);
                        double qk = VState.vPQRi(eQ);
                        double rk = VState.vPQRi(eR);
                        double pdotk = in.vPQRidot(eP);
                        double qdotk = in.vPQRidot(eQ);
                        double rdotk = in.vPQRidot(eR);
                        double Ap = -0.25 * (pk*pdotk + qk*qdotk + rk*rdotk);
                        double Bp = 0.25 * (pk*qdotk - qk*pdotk);
                        double Cp = 0.25 * (pdotk*rk - pk*rdotk);
                        double Dp = 0.25 * (qk*rdotk - qdotk*rk);
                        double C2p = sin(rhok) / omegak;
                        double C3p = 2.0 * (1.0 - cos(rhok)) / (omegak*omegak);
                        double H = C1 + C4 * Ap;
                        double G = -C2p*rk - C3p*rdotk + C4*Bp;
                        double J = C2p*qk + C3p*qdotk - C4*Cp;
                        double K = C2p*pk + C3p*pdotk - C4*Dp;

                        cout << "q:       " << q << endl;

                        // Warning! In the paper of Barker et al. the quaternion components are not
                        // ordered the same way as in JSBSim (see equations (2) and (3) of ref. [7]
                        // as well as the comment just below equation (3))
                        cout << "FORTRAN: " << H << " , " << K << " , " << J << " , " << -G << endl;*/
                    }
                    break; // The quaternion q is not normal so the normalization needs to be done.
                case eIntegrateType.eNone: // do nothing, freeze rotational rate
                    break;
                default:
                    break;
            }

            Integrand.Normalize();
        }

        private void UpdateLocationMatrices()
        {
            Tl2ec = VState.vLocation.GetTl2ec(); // local to ECEF transform
            Tec2l = Tl2ec.GetTranspose();          // ECEF to local frame transform
            Ti2l = Tec2l * Ti2ec;               // ECI to local frame transform
            Tl2i = Ti2l.GetTranspose();           // local to ECI transform
        }


        private void UpdateBodyMatrices()
        {
            Ti2b = VState.qAttitudeECI.GetTransformationMatrix(); // ECI to body frame transform
            Tb2i = Ti2b.GetTranspose();          // body to ECI frame transform
            Tl2b = Ti2b * Tl2i;                // local to body frame transform
            Tb2l = Tl2b.GetTranspose();          // body to local frame transform
            Tec2b = Ti2b * Tec2i;               // ECEF to body frame transform
            Tb2ec = Tec2b.GetTranspose();         // body to ECEF frame tranform

            Qec2b = Tec2b.GetQuaternion();
        }

#if TODO
          PropertyManager->Tie("velocities/eci-x-fps", this, eX, (PMF)&FGPropagate::GetInertialVelocity);
          PropertyManager->Tie("velocities/eci-y-fps", this, eY, (PMF)&FGPropagate::GetInertialVelocity);
          PropertyManager->Tie("velocities/eci-z-fps", this, eZ, (PMF)&FGPropagate::GetInertialVelocity);

          PropertyManager->Tie("velocities/eci-velocity-mag-fps", this, &FGPropagate::GetInertialVelocityMagnitude);
          PropertyManager->Tie("velocities/ned-velocity-mag-fps", this, &FGPropagate::GetNEDVelocityMagnitude);

          PropertyManager->Tie("position/h-sl-ft", this, &FGPropagate::GetAltitudeASL, &FGPropagate::SetAltitudeASL, true);
          PropertyManager->Tie("position/h-sl-meters", this, &FGPropagate::GetAltitudeASLmeters, &FGPropagate::SetAltitudeASLmeters, true);
          PropertyManager->Tie("position/lat-gc-rad", this, &FGPropagate::GetLatitude, &FGPropagate::SetLatitude, false);
          PropertyManager->Tie("position/long-gc-rad", this, &FGPropagate::GetLongitude, &FGPropagate::SetLongitude, false);
          PropertyManager->Tie("position/lat-gc-deg", this, &FGPropagate::GetLatitudeDeg, &FGPropagate::SetLatitudeDeg, false);
          PropertyManager->Tie("position/long-gc-deg", this, &FGPropagate::GetLongitudeDeg, &FGPropagate::SetLongitudeDeg, false);
          PropertyManager->Tie("position/lat-geod-rad", this, &FGPropagate::GetGeodLatitudeRad);
          PropertyManager->Tie("position/lat-geod-deg", this, &FGPropagate::GetGeodLatitudeDeg);
          PropertyManager->Tie("position/geod-alt-ft", this, &FGPropagate::GetGeodeticAltitude);
          PropertyManager->Tie("position/h-agl-ft", this,  &FGPropagate::GetDistanceAGL, &FGPropagate::SetDistanceAGL);
          PropertyManager->Tie("position/geod-alt-km", this, &FGPropagate::GetGeodeticAltitudeKm);
          PropertyManager->Tie("position/h-agl-km", this,  &FGPropagate::GetDistanceAGLKm, &FGPropagate::SetDistanceAGLKm);
          PropertyManager->Tie("position/radius-to-vehicle-ft", this, &FGPropagate::GetRadius);
          PropertyManager->Tie("position/terrain-elevation-asl-ft", this,
                                  &FGPropagate::GetTerrainElevation,
                                  &FGPropagate::SetTerrainElevation, false);

          PropertyManager->Tie("position/eci-x-ft", this, eX, (PMF)&FGPropagate::GetInertialPosition);
          PropertyManager->Tie("position/eci-y-ft", this, eY, (PMF)&FGPropagate::GetInertialPosition);
          PropertyManager->Tie("position/eci-z-ft", this, eZ, (PMF)&FGPropagate::GetInertialPosition);

          PropertyManager->Tie("position/ecef-x-ft", this, eX, (PMF)&FGPropagate::GetLocation);
          PropertyManager->Tie("position/ecef-y-ft", this, eY, (PMF)&FGPropagate::GetLocation);
          PropertyManager->Tie("position/ecef-z-ft", this, eZ, (PMF)&FGPropagate::GetLocation);

          PropertyManager->Tie("position/epa-rad", this, &FGPropagate::GetEarthPositionAngle);
          PropertyManager->Tie("metrics/terrain-radius", this, &FGPropagate::GetLocalTerrainRadius);

          PropertyManager->Tie("attitude/phi-rad", this, (int)ePhi, (PMF)&FGPropagate::GetEuler);
          PropertyManager->Tie("attitude/theta-rad", this, (int)eTht, (PMF)&FGPropagate::GetEuler);
          PropertyManager->Tie("attitude/psi-rad", this, (int)ePsi, (PMF)&FGPropagate::GetEuler);

          PropertyManager->Tie("attitude/phi-deg", this, (int)ePhi, (PMF)&FGPropagate::GetEulerDeg);
          PropertyManager->Tie("attitude/theta-deg", this, (int)eTht, (PMF)&FGPropagate::GetEulerDeg);
          PropertyManager->Tie("attitude/psi-deg", this, (int)ePsi, (PMF)&FGPropagate::GetEulerDeg);

          PropertyManager->Tie("attitude/roll-rad", this, (int)ePhi, (PMF)&FGPropagate::GetEuler);
          PropertyManager->Tie("attitude/pitch-rad", this, (int)eTht, (PMF)&FGPropagate::GetEuler);
          PropertyManager->Tie("attitude/heading-true-rad", this, (int)ePsi, (PMF)&FGPropagate::GetEuler);

#endif
        /// <summary>
        /// Returns the current altitude rate.
        /// Returns the current altitude rate (rate of climb).
        /// units ft/sec
        /// </summary>
        /// <returns>The current rate of change in altitude.</returns>
        [ScriptAttribute("velocities/h-dot-fps", " JSBSim original Gethdot")]
        public double HDot
        {
            get { return -vVel.Down; }
        }

        [ScriptAttribute("velocities/v-north-fps", " JSBSim original GetVel")]
        public double VelocityNorth { get { return vVel.North; } }

        [ScriptAttribute("velocities/v-east-fps", " JSBSim original GetVel")]
        public double VelocityEast { get { return vVel.East; } }

        [ScriptAttribute("velocities/v-down-fps", " JSBSim original GetVel")]
        public double VelocityDown { get { return vVel.Down; } }

        [ScriptAttribute("velocities/u-fps", " JSBSim original GetUVW")]
        public double VelocityU { get { return VState.vUVW.U; } }

        [ScriptAttribute("velocities/v-fps", " JSBSim original GetUVW")]
        public double VelocityV { get { return VState.vUVW.V; } }

        [ScriptAttribute("velocities/w-fps", " JSBSim original GetUVW")]
        public double VelocityW { get { return VState.vUVW.W; } }

        [ScriptAttribute("velocities/p-rad_sec", " JSBSim original GetPQR")]
        public double VelocityP { get { return VState.vPQR.P; } }

        [ScriptAttribute("velocities/q-rad_sec", " JSBSim original GetPQR")]
        public double VelocityQ { get { return VState.vPQR.Q; } }

        [ScriptAttribute("velocities/r-rad_sec", " JSBSim original GetPQR")]
        public double VelocityR { get { return VState.vPQR.R; } }

        [ScriptAttribute("velocities/pi-rad_sec", " JSBSim original GetPQRi")]
        public double VelocityPi { get { return VState.vPQRi.P; } }

        [ScriptAttribute("velocities/qi-rad_sec", " JSBSim original GetPQRi")]
        public double VelocityQi { get { return VState.vPQRi.Q; } }

        [ScriptAttribute("velocities/ri-rad_sec", " JSBSim original GetPQRi")]
        public double VelocityRi { get { return VState.vPQRi.R; } }


        /// <summary>
        /// Get/sets the altitude
        /// </summary>
        [ScriptAttribute("position/h-sl-ft", " JSBSim original Seth and Geth")]
        public double AltitudeASL
        {
            get { return GetAltitudeASL(); }
            set { SetAltitudeASL(value); }
        }

        [ScriptAttribute("position/h-agl-ft", " JSBSim original GetDistanceAGL/SetDistanceAGL")]
        public double DistanceAGL
        {
            get { return GetDistanceAGL(); }
            set { SetDistanceAGL(value); }
        }

#if DELETEME
        public double GetPQRdot(int idx) { return vPQRdot[idx - 1]; }


        [ScriptAttribute("attitude/phi-rad", " JSBSim original GetEuler")]
        [ScriptAttribute("attitude/roll-rad", " JSBSim original GetEuler")]
        public double AttitudePhi { get { return VState.vQtrn.GetEulerAngles().Phi; } }

        [ScriptAttribute("attitude/theta-rad", " JSBSim original GetEuler")]
        [ScriptAttribute("attitude/pitch-rad", " JSBSim original GetEuler")]
        public double AttitudeTheta { get { return VState.vQtrn.GetEulerAngles().Theta; } }

        [ScriptAttribute("attitude/psi-rad", " JSBSim original GetEuler")]
        [ScriptAttribute("attitude/heading-true-rad", " JSBSim original GetEuler")]
        public double AttitudePsi { get { return VState.vQtrn.GetEulerAngles().Psi; } }

        /// <summary>
        /// Returns the "constant" RunwayRadius.
        /// The RunwayRadius parameter is set by the calling application or set to
        /// zero if JSBSim is running in standalone mode. units feet
        /// </summary>
        /// <returns>distance of the runway from the center of the earth. </returns>
        public double GetRunwayRadius() { return RunwayRadius; }
        public double GetSeaLevelRadius() { return SeaLevelRadius; }
#endif

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

        [ScriptAttribute("position/radius-to-vehicle-ft", " JSBSim original GetRadius")]
        public double Radius
        {
            get { return VState.vLocation.Radius; }
            set { VState.vLocation.Radius = value; }
        }

        [ScriptAttribute("simulation/integrator/rate/rotational", " JSBSim original integrator_rotational_rate")]
        public int IntegratorRotationalRate
        {
            get { return (int)integrator_rotational_rate; }
            set { integrator_rotational_rate = (eIntegrateType)value; }
        }

        [ScriptAttribute("simulation/integrator/rate/translational", " JSBSim original integrator_translational_rate")]
        public int IntegratorTranslationalRate
        {
            get { return (int)integrator_translational_rate; }
            set { integrator_translational_rate = (eIntegrateType)value; }
        }

        [ScriptAttribute("simulation/integrator/position/rotational", " JSBSim original integrator_rotational_position")]
        public int IntegratorRotationalPosition
        {
            get { return (int)integrator_rotational_position; }
            set { integrator_rotational_position = (eIntegrateType)value; }
        }

        [ScriptAttribute("simulation/integrator/position/translational", " JSBSim original integrator_translational_position")]
        public int IntegratorTranslationalPosition
        {
            get { return (int)integrator_translational_position; }
            set { integrator_translational_position = (eIntegrateType)value; }
        }

        public void UpdateVehicleState()
        {
            RecomputeLocalTerrainVelocity();
            VState.vInertialPosition = Tec2i * (Vector3D)VState.vLocation;
            UpdateLocationMatrices();
            UpdateBodyMatrices();
            vVel = Tb2l * VState.vUVW;
            VState.qAttitudeLocal = Tl2b.GetQuaternion();
        }

        /// <summary>
        /// Compute the quaternion orientation derivative
        ///
        /// vQtrndot is the quaternion derivative.
        /// Reference: See Stevens and Lewis, "Aircraft Control and Simulation",
        ///            Second edition (2004), eqn 1.5-16b (page 50)
        /// </summary>
        public void CalculateQuatdot()
        {
            // Compute quaternion orientation derivative on current body rates
            VState.vQtrndot = VState.qAttitudeECI.GetQDot(VState.vPQRi);
        }

        /// <summary>
        /// Transform the velocity vector of the body relative to the origin (Earth
        /// center) to be expressed in the inertial frame, and add the vehicle velocity
        /// contribution due to the rotation of the planet.
        /// Reference: See Stevens and Lewis, "Aircraft Control and Simulation",
        ///            Second edition (2004), eqn 1.5-16c (page 50)
        /// </summary>
        public void CalculateInertialVelocity()
        {
            VState.vInertialVelocity = Tb2i * VState.vUVW + (inputs.vOmegaPlanet * VState.vInertialPosition);
        }

        public struct Inputs
        {
            public Vector3D vPQRidot;
            public Vector3D vUVWidot;
            public Vector3D vOmegaPlanet;
            public double SemiMajor;
            public double SemiMinor;
            public double DeltaT;
        }

        public Inputs inputs = new Inputs();

        // state vector
        private VehicleState VState = new VehicleState();

        private Inertial Inertial = null;
        private Vector3D vVel;
        private Matrix3D Tec2b;
        private Matrix3D Tb2ec;
        private Matrix3D Tl2b;   // local to body frame matrix copy for immediate local use
        private Matrix3D Tb2l;   // body to local frame matrix copy for immediate local use
        private Matrix3D Tl2ec;  // local to ECEF matrix copy for immediate local use
        private Matrix3D Tec2l;  // ECEF to local frame matrix copy for immediate local use
        private Matrix3D Tec2i;  // ECEF to ECI frame matrix copy for immediate local use
        private Matrix3D Ti2ec;  // ECI to ECEF frame matrix copy for immediate local use
        private Matrix3D Ti2b;   // ECI to body frame rotation matrix
        private Matrix3D Tb2i;   // body to ECI frame rotation matrix
        private Matrix3D Ti2l;
        private Matrix3D Tl2i;
        private double epa;        // Earth Position Angle

        private Quaternion Qec2b;

        private Vector3D LocalTerrainVelocity, LocalTerrainAngularVelocity;

        private eIntegrateType integrator_rotational_rate;
        private eIntegrateType integrator_translational_rate;
        private eIntegrateType integrator_rotational_position;
        private eIntegrateType integrator_translational_position;
    }
}
