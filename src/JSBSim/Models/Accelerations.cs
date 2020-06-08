#region Copyright(C)  Licensed under GNU GPL.
#endregion

using System.Collections.Generic;
using CommonUtils.MathLib;
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
namespace JSBSim.Models
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Xml;
    using System.Text;


    // Import log4net classes.
    using log4net;

    using CommonUtils.MathLib;
    using JSBSim.Script;
    using JSBSim.Format;
    using JSBSim.MathValues;

    /// <summary>
    ///     Handles the calculation of accelerations.
    ///
    ///    - Calculate the angular accelerations
    ///    - Calculate the translational accelerations
    ///
    ///    This class is collecting all the forces and the moments acting on the body
    ///    to calculate the corresponding accelerations according to Newton's second
    ///    law. This is also where the friction forces related to the ground reactions
    ///    are evaluated.
    ///
    ///    JSBSim provides several ways to calculate the influence of the gravity on
    ///    the vehicle. The different options can be selected via the following
    ///    properties :
    ///    @property simulation/gravity-model (read/write) Selects the gravity model.
    ///              Two options are available : 0 (Standard gravity assuming the Earth
    ///              is spherical) or 1 (WGS84 gravity taking the Earth oblateness into
    ///              account). WGS84 gravity is the default.
    ///    @property simulation/gravitational-torque (read/write) Enables/disables the
    ///              calculations of the gravitational torque on the vehicle. This is
    ///              mainly relevant for spacecrafts that are orbiting at low altitudes.
    ///              Gravitational torque calculations are disabled by default.
    ///
    ///    Special care is taken in the calculations to obtain maximum fidelity in
    ///    JSBSim results. In FGAccelerations, this is obtained by avoiding as much as
    ///    possible the transformations from one frame to another. As a consequence,
    ///    the frames in which the accelerations are primarily evaluated are dictated
    ///    by the frames in which FGPropagate resolves the equations of movement (the
    ///    ECI frame for the translations and the body frame for the rotations).
    ///
    ///    @see Mark Harris and Robert Lyle, "Spacecraft Gravitational Torques",
    ///         NASA SP-8024, May 1969
    ///
    ///    @author Jon S. Berndt, Mathias Froehlich, Bertrand Coconnier
    /// </summary>
    [Serializable]
    public class Accelerations : Model
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
        /// <param name="exec">a reference to the parent executive object</param>
        public Accelerations(FDMExecutive exec) : base(exec)
        {
            Debug(0);
            Name = "FGAccelerations";
            gravTorque = false;

            vPQRidot = Vector3D.Zero;
            vUVWidot = Vector3D.Zero;
            vUVWdot = Vector3D.Zero;
            vBodyAccel = Vector3D.Zero;

            Bind();
            Debug(0);
        }

        /// <summary>
        ///  Runs the state propagation model; called by the Executive
        /// Can pass in a value indicating if the executive is directing the simulation to Hold.
        /// </summary>
        /// <param name="Holding">if true, the executive has been directed to hold the sim from
        /// advancing time.Some models may ignore this flag, such as the Input
        /// model, which may need to be active to listen on a socket for the
        /// "Resume" command to be given.</param>
        /// <returns>false if no error</returns>
        public override bool Run(bool Holding)
        {
            if (base.Run(Holding)) return true;  // Fast return if we have nothing to do ...
            if (Holding) return false;

            CalculatePQRdot();   // Angular rate derivative
            CalculateUVWdot();   // Translational rate derivative

            if (!FDMExec.GetHoldDown())
                CalculateFrictionForces(inputs.DeltaT * rate);  // Update rate derivatives with friction forces

            Debug(2);
            return false;
        }

        /// <summary>
        /// Initializes the FGAccelerations class after instantiation and prior to first execution.
        /// The base class FGModel::InitModel is called first, initializing pointers to the
        /// other FGModel objects(and others).
        /// </summary>
        /// <returns></returns>
        public override bool InitModel()
        {
            if (!base.InitModel()) return false;

            vPQRidot = Vector3D.Zero;
            vUVWidot = Vector3D.Zero;
            vUVWdot = Vector3D.Zero;
            vBodyAccel = Vector3D.Zero;

            return true;
        }


        /// <summary>
        ///  Retrieves the body axis acceleration.
        ///  Retrieves the computed body axis accelerations based on the
        ///  applied forces and accounting for a rotating body frame.
        ///  The vector returned is represented by an FGColumnVector3 reference. The vector
        ///  for the acceleration in Body frame is organized (Ax, Ay, Az). The vector
        ///  is 1-based, so that the first element can be retrieved using the "()" operator.
        ///  In other words, vUVWdot(1) is Ax. Various convenience enumerators are defined
        ///  in FGJSBBase. The relevant enumerators for the vector returned by this call are,
        ///  eX=1, eY=2, eZ=3.
        ///  units ft/sec^2
        /// </summary>
        /// <returns>Body axis translational acceleration in ft/sec^2.</returns>
        public Vector3D GetUVWdot() { return vUVWdot; }

        /// <summary>
        /// Retrieves the body axis acceleration in the ECI frame.
        /// Retrieves the computed body axis accelerations based on the applied forces.
        /// The ECI frame being an inertial frame this vector does not contain the
        /// Coriolis and centripetal accelerations. The vector is expressed in the
        /// Body frame.
        /// The vector returned is represented by an FGColumnVector3 reference. The
        /// vector for the acceleration in Body frame is organized (Aix, Aiy, Aiz). The
        /// vector is 1-based, so that the first element can be retrieved using the
        /// "()" operator. In other words, vUVWidot(1) is Aix. Various convenience
        /// enumerators are defined in FGJSBBase. The relevant enumerators for the
        /// vector returned by this call are, eX=1, eY=2, eZ=3.
        /// units ft/sec^2
        /// </summary>
        /// <returns>Body axis translational acceleration in ft/sec^2.</returns>
        public Vector3D GetUVWidot() { return vUVWidot; }

        /// <summary>
        /// Retrieves the body axis angular acceleration vector.
        /// Retrieves the body axis angular acceleration vector in rad/sec^2. The
        /// angular acceleration vector is determined from the applied moments and
        /// accounts for a rotating frame.
        /// The vector returned is represented by an FGColumnVector3 reference. The vector
        /// for the angular acceleration in Body frame is organized (Pdot, Qdot, Rdot). The vector
        /// is 1-based, so that the first element can be retrieved using the "()" operator.
        /// In other words, vPQRdot(1) is Pdot. Various convenience enumerators are defined
        /// in FGJSBBase. The relevant enumerators for the vector returned by this call are,
        /// eP=1, eQ=2, eR=3.
        /// units rad/sec^2
        /// </summary>
        /// <returns>The angular acceleration vector.</returns>
        public Vector3D GetPQRdot() { return vPQRdot; }

        /// <summary>
        /// Retrieves the axis angular acceleration vector in the ECI frame.
        /// Retrieves the body axis angular acceleration vector measured in the ECI
        /// frame and expressed in the body frame. The angular acceleration vector is
        /// determined from the applied moments.
        /// The vector returned is represented by an FGColumnVector3 reference. The
        /// vector for the angular acceleration in Body frame is organized (Pidot,
        /// Qidot, Ridot). The vector is 1-based, so that the first element can be
        /// retrieved using the "()" operator. In other words, vPQRidot(1) is Pidot.
        /// Various convenience enumerators are defined in FGJSBBase. The relevant
        /// enumerators for the vector returned by this call are, eP=1, eQ=2, eR=3.
        /// units rad/sec^2
        /// </summary>
        /// <returns>The angular acceleration vector.</returns>
        public Vector3D GetPQRidot() { return vPQRidot; }

        /// <summary>
        /// Retrieves a body frame acceleration component.
        /// Retrieves a body frame acceleration component. The acceleration returned
        /// is extracted from the vUVWdot vector (an FGColumnVector3). The vector for
        /// the acceleration in Body frame is organized (Ax, Ay, Az). The vector is
        /// 1-based. In other words, GetUVWdot(1) returns Ax. Various convenience
        /// enumerators are defined in FGJSBBase. The relevant enumerators for the
        /// acceleration returned by this call are, eX=1, eY=2, eZ=3.
        /// units ft/sec^2
        /// </summary>
        /// <param name="idx">the index of the acceleration component desired (1-based).</param>
        /// <returns>The body frame acceleration component.</returns>
        public double GetUVWdot(int idx) { return vUVWdot[idx - 1]; }

        /// <summary>
        /// Retrieves the acceleration resulting from the applied forces.
        /// Retrieves the ratio of the sum of all forces applied on the craft to its
        /// mass. This does include the friction forces but not the gravity.
        /// The vector returned is represented by an FGColumnVector3 reference. The
        /// vector for the acceleration in Body frame is organized (Ax, Ay, Az). The
        /// vector is 1-based, so that the first element can be retrieved using the
        /// "()" operator. In other words, vBodyAccel(1) is Ax. Various convenience
        /// enumerators are defined in FGJSBBase. The relevant enumerators for the
        /// vector returned by this call are, eX=1, eY=2, eZ=3.
        /// units ft/sec^2
        /// </summary>
        /// <returns>The acceleration resulting from the applied forces.</returns>
        public Vector3D GetBodyAccel() { return vBodyAccel; }

        public double GetGravAccelMagnitude() { return inputs.vGravAccel.Magnitude(); }

        /// <summary>
        /// Retrieves a component of the acceleration resulting from the applied forces.
        /// Retrieves a component of the ratio between the sum of all forces applied
        /// on the craft to its mass. The value returned is extracted from the vBodyAccel
        /// vector (an FGColumnVector3). The vector for the acceleration in Body frame
        /// is organized (Ax, Ay, Az). The vector is 1-based. In other words,
        /// GetBodyAccel(1) returns Ax. Various convenience enumerators are defined
        /// in FGJSBBase. The relevant enumerators for the vector returned by this
        /// call are, eX=1,
        /// </summary>
        /// <param name="idx">the index of the acceleration component desired (1-based).</param>
        /// <returns>The component of the acceleration resulting from the applied forces.</returns>
        public double GetBodyAccel(int idx) { return vBodyAccel[idx - 1]; }

        /** Retrieves a body frame angular acceleration component.
            Retrieves a body frame angular acceleration component. The angular
            acceleration returned is extracted from the vPQRdot vector (an
            FGColumnVector3). The vector for the angular acceleration in Body frame
            is organized (Pdot, Qdot, Rdot). The vector is 1-based. In other words,
            GetPQRdot(1) returns Pdot (roll acceleration). Various convenience
            enumerators are defined in FGJSBBase. The relevant enumerators for the
            angular acceleration returned by this call are, eP=1, eQ=2, eR=3.
            units rad/sec^2
            @param axis the index of the angular acceleration component desired (1-based).
            @return The body frame angular acceleration component.
        */
        public double GetPQRdot(int axis) { return vPQRdot[axis - 1]; }

        /* Retrieves a component of the total moments applied on the body.
            Retrieves a component of the total moments applied on the body. This does
            include the moments generated by friction forces and the gravitational
            torque (if the property \e simulation/gravitational-torque is set to true).
            The vector for the total moments in the body frame is organized (Mx, My
            , Mz). The vector is 1-based. In other words, GetMoments(1) returns Mx.
            Various convenience enumerators are defined in FGJSBBase. The relevant
            enumerators for the moments returned by this call are, eX=1, eY=2, eZ=3.
            units lbs*ft
            @param idx the index of the moments component desired (1-based).
            @return The total moments applied on the body.
         */
        public double GetMoments(int idx) { return inputs.Moment[idx - 1] + vFrictionMoments[idx - 1]; }
        public Vector3D GetMoments() { return inputs.Moment + vFrictionMoments; }

        /// <summary>
        /// Retrieves the total forces applied on the body.
        /// Retrieves the total forces applied on the body. This does include the
        /// friction forces but not the gravity.
        /// The vector for the total forces in the body frame is organized (Fx, Fy
        /// , Fz). The vector is 1-based. In other words, GetForces(1) returns Fx.
        /// Various convenience enumerators are defined in FGJSBBase. The relevant
        /// enumerators for the forces returned by this call are, eX=1, eY=2, eZ=3.
        /// units lbs
        /// </summary>
        /// <param name="idx">the index of the forces component desired (1-based).</param>
        /// <returns> The total forces applied on the body.</returns>
        public double GetForces(int idx) { return inputs.Force[idx - 1] + vFrictionForces[idx - 1]; }
        public Vector3D GetForces() { return inputs.Force + vFrictionForces; }

        /// <summary>
        /// Retrieves the ground moments applied on the body.
        /// Retrieves the ground moments applied on the body. This does include the
        /// ground normal reaction and friction moments.
        /// The vector for the ground moments in the body frame is organized (Mx, My
        /// , Mz). The vector is 1-based. In other words, GetGroundMoments(1) returns
        /// Mx. Various convenience enumerators are defined in FGJSBBase. The relevant
        /// enumerators for the moments returned by this call are, eX=1, eY=2, eZ=3.
        /// units lbs*ft
        /// </summary>
        /// <param name="idx">the index of the moments component desired (1-based).</param>
        /// <returns>The ground moments applied on the body.</returns>
        public double GetGroundMoments(int idx) { return inputs.GroundMoment[idx - 1] + vFrictionMoments[idx - 1]; }
        public Vector3D GetGroundMoments() { return inputs.GroundMoment + vFrictionMoments; }


        /// <summary>
        /// Retrieves the ground forces applied on the body.
        /// Retrieves the ground forces applied on the body. This does include the
        /// ground normal reaction and friction forces.
        /// The vector for the ground forces in the body frame is organized (Fx, Fy
        /// , Fz). The vector is 1-based. In other words, GetGroundForces(1) returns
        /// Fx. Various convenience enumerators are defined in FGJSBBase. The relevant
        /// enumerators for the forces returned by this call are, eX=1, eY=2, eZ=3.
        /// units lbs.
        /// </summary>
        /// <param name="idx">the index of the forces component desired (1-based).</param>
        /// <returns>The ground forces applied on the body.</returns>
        public double GetGroundForces(int idx) { return inputs.GroundForce[idx - 1] + vFrictionForces[idx - 1]; }
        public Vector3D GetGroundForces() { return inputs.GroundForce + vFrictionForces; }

        /// <summary>
        /// Retrieves the weight applied on the body i.e. the force that results from
        /// the gravity applied to the body mass.
        /// The vector for the weight forces in the body frame is organized (Fx, Fy ,
        /// Fz). The vector is 1-based. In other words, GetWeight(1) returns
        /// Fx. Various convenience enumerators are defined in FGJSBBase. The relevant
        /// enumerators for the forces returned by this call are, eX=1, eY=2, eZ=3.
        /// units lbs.
        /// </summary>
        /// <param name="idx">the index of the forces component desired (1-based).</param>
        /// <returns>The ground forces applied on the body.</returns>
        public double GetWeight(int idx) { return inputs.Mass * (inputs.Tec2b * inputs.vGravAccel)[idx - 1]; }
        public Vector3D GetWeight() { return inputs.Mass * (inputs.Tec2b * inputs.vGravAccel); }

        /// <summary>
        /// Initializes the FGAccelerations class prior to a new execution.
        /// Initializes the class prior to a new execution when the input data stored
        /// in the Inputs structure have been set to their initial values.
        /// </summary>
        public void InitializeDerivatives()
        {
            // Make an initial run and set past values
            CalculatePQRdot();           // Angular rate derivative
            CalculateUVWdot();           // Translational rate derivative
            CalculateFrictionForces(0.0);   // Update rate derivatives with friction forces
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
                vUVWidot = inputs.vOmegaPlanet * (inputs.vOmegaPlanet * inputs.vInertialPosition);
                vUVWdot = Vector3D.Zero;
                vPQRidot = inputs.vPQRi * (inputs.Ti2b * inputs.vOmegaPlanet);
                vPQRdot = Vector3D.Zero;
            }
        }

        public class Inputs
        {
            /// The body inertia matrix expressed in the body frame
            public Matrix3D J;
            /// The inverse of the inertia matrix J
            public Matrix3D Jinv;
            /// Transformation matrix from the ECI to the Body frame
            public Matrix3D Ti2b;
            /// Transformation matrix from the Body to the ECI frame
            public Matrix3D Tb2i;
            /// Transformation matrix from the ECEF to the Body frame
            public Matrix3D Tec2b;
            /// Transformation matrix from the ECEF to the ECI frame
            public Matrix3D Tec2i;
            /// Total moments applied to the body except friction and gravity (expressed in the body frame)
            public Vector3D Moment;
            /// Moments generated by the ground normal reactions expressed in the body frame. Does not account for friction.
            public Vector3D GroundMoment;
            /// Total forces applied to the body except friction and gravity (expressed in the body frame)
            public Vector3D Force;
            /// Forces generated by the ground normal reactions expressed in the body frame. Does not account for friction.
            public Vector3D GroundForce;
            /// Gravity intensity vector (expressed in the ECEF frame).
            public Vector3D vGravAccel;
            /// Angular velocities of the body with respect to the ECI frame (expressed in the body frame).
            public Vector3D vPQRi;
            /// Angular velocities of the body with respect to the local frame (expressed in the body frame).
            public Vector3D vPQR;
            /// Velocities of the body with respect to the local frame (expressed in the body frame).
            public Vector3D vUVW;
            /// Body position (X,Y,Z) measured in the ECI frame.
            public Vector3D vInertialPosition;
            /// Earth rotating vector (expressed in the ECI frame).
            public Vector3D vOmegaPlanet;
            /// Terrain velocities with respect to the local frame (expressed in the ECEF frame).
            public Vector3D TerrainVelocity;
            /// Terrain angular velocities with respect to the local frame (expressed in the ECEF frame).
            public Vector3D TerrainAngularVel;
            /// Time step
            public double DeltaT;
            /// Body mass
            public double Mass;
            /// List of Lagrange multipliers set by FGLGear for friction forces calculations.
            public List<LagrangeMultiplier> MultipliersList = new List<LagrangeMultiplier>();
        }
        public Inputs inputs = new Inputs();

        private Vector3D vPQRdot, vPQRidot;
        private Vector3D vUVWdot, vUVWidot;
        private Vector3D vBodyAccel;
        private Vector3D vFrictionForces;
        private Vector3D vFrictionMoments;

        private bool gravTorque;


        /// <summary>
        // Compute body frame rotational accelerations based on the current body moments
        ///
        /// vPQRdot is the derivative of the absolute angular velocity of the vehicle
        /// (body rate with respect to the ECEF frame), expressed in the body frame,
        /// where the derivative is taken in the body frame.
        /// J is the inertia matrix
        /// Jinv is the inverse inertia matrix
        /// vMoments is the moment vector in the body frame
        /// in.vPQRi is the total inertial angular velocity of the vehicle
        /// expressed in the body frame.
        /// Reference: See Stevens and Lewis, "Aircraft Control and Simulation",
        ///            Second edition (2004), eqn 1.5-16e (page 50)
        /// </summary>
        private void CalculatePQRdot()
        {
            if (gravTorque)
            {
                // Compute the gravitational torque
                // Reference: See Harris and Lyle "Spacecraft Gravitational Torques",
                //            NASA SP-8024 (1969) eqn (2) (page 7)
                Vector3D R = inputs.Ti2b * inputs.vInertialPosition;
                double invRadius = 1.0 / R.Magnitude();
                R *= invRadius;
                inputs.Moment += (3.0 * inputs.vGravAccel.Magnitude() * invRadius) * (R * (inputs.J * R));
            }

            // Compute body frame rotational accelerations based on the current body
            // moments and the total inertial angular velocity expressed in the body
            // frame.
            //  if (HoldDown && !FDMExec->GetTrimStatus()) {
            if (FDMExec.GetHoldDown())
            {
                // The rotational acceleration in ECI is calculated so that the rotational
                // acceleration is zero in the body frame.
                vPQRdot = Vector3D.Zero;
                vPQRidot = inputs.vPQRi * (inputs.Ti2b * inputs.vOmegaPlanet);
            }
            else
            {
                vPQRidot = inputs.Jinv * (inputs.Moment - inputs.vPQRi * (inputs.J * inputs.vPQRi));
                vPQRdot = vPQRidot - inputs.vPQRi * (inputs.Ti2b * inputs.vOmegaPlanet);
            }
        }

        /// <summary>
        /// This set of calculations results in the body and inertial frame accelerations
        /// being computed.
        /// Compute body and inertial frames accelerations based on the current body
        /// forces including centripetal and Coriolis accelerations for the former.
        /// in.vOmegaPlanet is the Earth angular rate - expressed in the inertial frame -
        /// so it has to be transformed to the body frame. More completely,
        /// in.vOmegaPlanet is the rate of the ECEF frame relative to the Inertial
        /// frame (ECI), expressed in the Inertial frame.
        /// in.Force is the total force on the vehicle in the body frame.
        /// in.vPQR is the vehicle body rate relative to the ECEF frame, expressed
        /// in the body frame.
        /// in.vUVW is the vehicle velocity relative to the ECEF frame, expressed
        /// in the body frame.
        /// Reference: See Stevens and Lewis, "Aircraft Control and Simulation",
        ///            Second edition (2004), eqns 1.5-13 (pg 48) and 1.5-16d (page 50)
        /// </summary>
        private void CalculateUVWdot()
        {
            if (FDMExec.GetHoldDown() && !FDMExec.GetTrimStatus())
                vBodyAccel = Vector3D.Zero;
            else
                vBodyAccel = inputs.Force / inputs.Mass;

            vUVWdot = vBodyAccel - (inputs.vPQR + 2.0 * (inputs.Ti2b * inputs.vOmegaPlanet)) * inputs.vUVW;

            // Include Centripetal acceleration.
            vUVWdot -= inputs.Ti2b * (inputs.vOmegaPlanet * (inputs.vOmegaPlanet * inputs.vInertialPosition));

            if (FDMExec.GetHoldDown())
            {
                // The acceleration in ECI is calculated so that the acceleration is zero
                // in the body frame.
                vUVWidot = inputs.vOmegaPlanet * (inputs.vOmegaPlanet * inputs.vInertialPosition);
                vUVWdot = Vector3D.Zero;
            }
            else
            {
                vUVWdot += inputs.Tec2b * inputs.vGravAccel;
                vUVWidot = inputs.Tb2i * vBodyAccel + inputs.Tec2i * inputs.vGravAccel;
            }
        }

        /// <summary>
        /// Computes the contact forces just before integrating the EOM.
        /// This routine is using Lagrange multipliers and the projected Gauss-Seidel
        /// (PGS) method.
        /// Reference: See Erin Catto, "Iterative Dynamics with Temporal Coherence",
        ///            February 22, 2005
        /// In JSBSim there is only one rigid body (the aircraft) and there can be
        /// multiple points of contact between the aircraft and the ground. As a
        /// consequence our matrix Jac*M^-1*Jac^T is not sparse and the algorithm
        /// described in Catto's paper has been adapted accordingly.
        /// The friction forces are resolved in the body frame relative to the origin
        /// (Earth center).
        /// </summary>
        /// <param name="dt"></param>
        private void CalculateFrictionForces(double dt)
        {
            List<LagrangeMultiplier> multipliers = inputs.MultipliersList;
            int n = multipliers.Count;

            vFrictionForces = Vector3D.Zero;
            vFrictionMoments = Vector3D.Zero;

            // If no gears are in contact with the ground then return
            if (n == 0) return;

            List<double> a = new List<double>(n * n); // Will contain Jac*M^-1*Jac^T
            List<double> rhs = new List<double>(n);

            // Assemble the linear system of equations
            for (int i = 0; i < n; i++)
            {
                Vector3D U = multipliers[i].ForceJacobian;
                Vector3D r = multipliers[i].LeverArm;
                Vector3D v1 = U / inputs.Mass;
                Vector3D v2 = inputs.Jinv * (r * U); // Should be J^-T but J is symmetric and so is J^-1

                for (int j = 0; j < i; j++)
                    a[i * n + j] = a[j * n + i]; // Takes advantage of the symmetry of Jac^T*M^-1*Jac

                for (int j = i; j < n; j++)
                {
                    U = multipliers[j].ForceJacobian;
                    r = multipliers[j].LeverArm;
                    a[i * n + j] = Vector3D.Dot(U, v1 + v2 * r);
                }
            }

            // Assemble the RHS member

            // Translation
            Vector3D vdot = vUVWdot;
            if (dt > 0.0) // Zeroes out the relative movement between the aircraft and the ground
                vdot += (inputs.vUVW - inputs.Tec2b * inputs.TerrainVelocity) / dt;

            // Rotation
            Vector3D wdot = vPQRdot;
            if (dt > 0.0) // Zeroes out the relative movement between the aircraft and the ground
                wdot += (inputs.vPQR - inputs.Tec2b * inputs.TerrainAngularVel) / dt;

            // Prepare the linear system for the Gauss-Seidel algorithm :
            // 1. Compute the right hand side member 'rhs'
            // 2. Divide every line of 'a' and 'rhs' by a[i,i]. This is in order to save
            //    a division computation at each iteration of Gauss-Seidel.
            for (int i = 0; i < n; i++)
            {
                double d = a[i * n + i];
                Vector3D U = multipliers[i].ForceJacobian;
                Vector3D r = multipliers[i].LeverArm;

                rhs[i] = -Vector3D.Dot(U, vdot + wdot * r) / d;

                for (int j = 0; j < n; j++)
                    a[i * n + j] /= d;
            }

            // Resolve the Lagrange multipliers with the projected Gauss-Seidel method
            for (int iter = 0; iter < 50; iter++)
            {
                double norm = 0.0;

                for (int i = 0; i < n; i++)
                {
                    double lambda0 = multipliers[i].value;
                    double dlambda = rhs[i];

                    for (int j = 0; j < n; j++)
                        dlambda -= a[i * n + j] * multipliers[j].value;

                    var tmp = multipliers[i];
                    tmp.value = MathExt.Constrain(multipliers[i].Min, lambda0 + dlambda, multipliers[i].Max);
                    multipliers[i] = tmp;
                    dlambda = multipliers[i].value - lambda0;

                    norm += Math.Abs(dlambda);
                }

                if (norm < 1E-5) break;
            }

            // Calculate the total friction forces and moments

            for (int i = 0; i < n; i++)
            {
                double lambda = multipliers[i].value;
                Vector3D U = multipliers[i].ForceJacobian;
                Vector3D r = multipliers[i].LeverArm;

                Vector3D F = lambda * U;
                vFrictionForces += F;
                vFrictionMoments += r * F;
            }

            Vector3D accel = vFrictionForces / inputs.Mass;
            Vector3D omegadot = inputs.Jinv * vFrictionMoments;

            vBodyAccel += accel;
            vUVWdot += accel;
            vUVWidot += inputs.Tb2i * accel;
            vPQRdot += omegadot;
            vPQRidot += omegadot;
        }


        public override void Bind() { }
        protected override void Debug(int from) { }
    }
}
