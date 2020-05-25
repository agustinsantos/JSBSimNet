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
    using System.IO;
    using System.Text;
    using System.Xml;

    // Import log4net classes.
    using log4net;
    using CommonUtils.MathLib;
    using CommonUtils.IO;
    using JSBSim.Format;
    using JSBSim.MathValues;

    /// <summary>
    /// Landing gear model.
    /// Calculates forces and moments due to landing gear reactions. This is done in
    /// several steps, and is dependent on what kind of gear is being modeled. Here
    /// are the parameters that can be specified in the config file for modeling
    /// landing gear:
    /// <p>
    /// <b><u>Physical Characteristics</u></b><br>
    /// <ol>
    /// <li>X, Y, Z location, in inches in structural coordinate frame</li>
    /// <li>Spring constant, in lbs/ft</li>
    /// <li>Damping coefficient, in lbs/ft/sec</li>
    /// <li>Dynamic Friction Coefficient</li>
    /// <li>Static Friction Coefficient</li>
    /// </ol></p><p>
    /// <b><u>Operational InputOutput</b></u><br>
    /// <ol>
    /// <li>Name</li>
    /// <li>Steerability attribute {one of STEERABLE | FIXED | CASTERED}</li>
    /// <li>Brake Group Membership {one of LEFT | CENTER | RIGHT | NOSE | TAIL | NONE}</li>
    /// <li>Max Steer Angle, in degrees</li>
    /// </ol></p>
    /// <p>
    /// <b><u>Algorithm and Approach to Modeling</u></b><br>
    /// <ol>
    /// <li>Find the location of the uncompressed landing gear relative to the CG of
    /// the aircraft. Remember, the structural coordinate frame that the aircraft is
    /// defined in is: X positive towards the tail, Y positive out the right side, Z
    /// positive upwards. The locations of the various parts are given in inches in
    /// the config file.</li>
    /// <li>The vector giving the location of the gear (relative to the cg) is
    /// rotated 180 degrees about the Y axis to put the coordinates in body frame (X
    /// positive forwards, Y positive out the right side, Z positive downwards, with
    /// the origin at the cg). The lengths are also now given in feet.</li>
    /// <li>The new gear location is now transformed to the local coordinate frame
    /// using the body-to-local matrix. (Mb2l).</li>
    /// <li>Knowing the location of the center of gravity relative to the ground
    /// (height above ground level or AGL) now enables gear deflection to be
    /// calculated. The gear compression value is the local frame gear Z location
    /// value minus the height AGL. [Currently, we make the assumption that the gear
    /// is oriented - and the deflection occurs in - the Z axis only. Additionally,
    /// the vector to the landing gear is currently not modified - which would
    /// (correctly) move the point of contact to the actual compressed-gear point of
    /// contact. Eventually, articulated gear may be modeled, but initially an
    /// effort must be made to model a generic system.] As an example, say the
    /// aircraft left main gear location (in local coordinates) is Z = 3 feet
    /// (positive) and the height AGL is 2 feet. This tells us that the gear is
    /// compressed 1 foot.</li>
    /// <li>If the gear is compressed, a Weight-On-Wheels (WOW) flag is set.</li>
    /// <li>With the compression length calculated, the compression velocity may now
    /// be calculated. This will be used to determine the damping force in the
    /// strut. The aircraft rotational rate is multiplied by the vector to the wheel
    /// to get a wheel velocity in body frame. That velocity vector is then
    /// transformed into the local coordinate frame.</li>
    /// <li>The aircraft cg velocity in the local frame is added to the
    /// just-calculated wheel velocity (due to rotation) to get a total wheel
    /// velocity in the local frame.</li>
    /// <li>The compression speed is the Z-component of the vector.</li>
    /// <li>With the wheel velocity vector no longer needed, it is normalized and
    /// multiplied by a -1 to reverse it. This will be used in the friction force
    /// calculation.</li>
    /// <li>Since the friction force takes place solely in the runway plane, the Z
    /// coordinate of the normalized wheel velocity vector is set to zero.</li>
    /// <li>The gear deflection force (the force on the aircraft acting along the
    /// local frame Z axis) is now calculated given the spring and damper
    /// coefficients, and the gear deflection speed and stroke length. Keep in mind
    /// that gear forces always act in the negative direction (in both local and
    /// body frames), and are not capable of generating a force in the positive
    /// sense (one that would attract the aircraft to the ground). So, the gear
    /// forces are always negative - they are limited to values of zero or less. The
    /// gear force is simply the negative of the sum of the spring compression
    /// length times the spring coefficient and the gear velocity times the damping
    /// coefficient.</li>
    /// <li>The lateral/directional force acting on the aircraft through the landing
    /// 
    /// gear (along the local frame X and Y axes) is calculated next. First, the
    /// friction coefficient is multiplied by the recently calculated Z-force. This
    /// is the friction force. It must be given direction in addition to magnitude.
    /// We want the components in the local frame X and Y axes. From step 9, above,
    /// the conditioned wheel velocity vector is taken and the X and Y parts are
    /// multiplied by the friction force to get the X and Y components of friction.
    /// </li>
    /// <li>The wheel force in local frame is next converted to body frame.</li>
    /// <li>The moment due to the gear force is calculated by multiplying r x F
    /// (radius to wheel crossed into the wheel force). Both of these operands are
    /// in body frame.</li>
    /// </ol>
    /// @author Jon S. Berndt
    /// @see Richard E. McFarland, "A Standard Kinematic Model for Flight Simulation at
    /// NASA-Ames", NASA CR-2497, January 1975
    /// @see Barnes W. McCormick, "Aerodynamics, Aeronautics, and Flight Mechanics",
    /// Wiley & Sons, 1979 ISBN 0-471-03032-5
    /// @see W. A. Ragsdale, "A Generic Landing Gear Dynamics Model for LASRS++",
    /// AIAA-2000-4303
    /// </summary>
    public class LGear
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

        /// Brake grouping enumerators
        public enum BrakeGroup { None = 0, Left, Right, Center, Nose, Tail };

        /// Steering group membership enumerators
        public enum SteerType { Steer, Fixed, Caster };

        /// Report type enumerators
        public enum ReportType { None, Takeoff, Land };

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="File">the config file instance</param>
        /// <param name="exec">the parent executive object</param>
        /// <param name="number"></param>
        public LGear(XmlElement element, FDMExecutive exec, int number)
        {

            FDMExec = exec;
            bool dampReboundFound = false;
            bool vXYZFound = false;

            GearNumber = number;

            name = element.GetAttribute("name").Trim();
            sContactType = element.GetAttribute("type").Trim();
            foreach (XmlNode currentNode in element.ChildNodes)
            {
                if (currentNode.NodeType == XmlNodeType.Element)
                {
                    try
                    {
                        XmlElement currentElement = (XmlElement)currentNode;

                        if (currentElement.LocalName.Equals("spring_coeff"))
                        {
                            kSpring = FormatHelper.ValueAsNumberConvertTo(currentElement, "LBS/FT");
                        }
                        else if (currentElement.LocalName.Equals("damping_coeff"))
                        {
                            bDamp = FormatHelper.ValueAsNumberConvertTo(currentElement, "LBS/FT/SEC");
                        }
                        else if (currentElement.LocalName.Equals("damping_coeff_rebound"))
                        {
                            bDampRebound = FormatHelper.ValueAsNumberConvertTo(currentElement, "LBS/FT/SEC");
                            dampReboundFound = true;
                        }
                        else if (currentElement.LocalName.Equals("dynamic_friction"))
                            dynamicFCoeff = double.Parse(currentElement.InnerText, FormatHelper.numberFormatInfo);
                        else if (currentElement.LocalName.Equals("static_friction"))
                            staticFCoeff = double.Parse(currentElement.InnerText, FormatHelper.numberFormatInfo);
                        else if (currentElement.LocalName.Equals("rolling_friction"))
                            rollingFCoeff = double.Parse(currentElement.InnerText, FormatHelper.numberFormatInfo);
                        else if (currentElement.LocalName.Equals("max_steer"))
                        {
                            maxSteerAngle = FormatHelper.ValueAsNumberConvertTo(currentElement, "DEG");
                        }
                        else if (currentElement.LocalName.Equals("retractable"))
                            isRetractable = (int.Parse(currentElement.InnerText, FormatHelper.numberFormatInfo) == 1);
                        else if (currentElement.LocalName.Equals("steer_type"))
                            sSteerType = currentElement.InnerText.Trim();
                        else if (currentElement.LocalName.Equals("brake_group"))
                            sBrakeGroup = currentElement.InnerText.Trim();
                        else if (currentElement.LocalName.Equals("location"))
                        {
                            vXYZ = FormatHelper.TripletConvertTo(currentElement, "IN");
                            vXYZFound = true;
                        }
                        else if (currentElement.LocalName.Equals("table"))
                        {
                            string force_type = currentElement.GetAttribute("type");
                            if (force_type.Equals("CORNERING_COEFF"))
                            {
                                ForceY_Table = new Table(exec.PropertyManager, currentElement);
                            }
                            else
                            {
                                if (log.IsErrorEnabled)
                                    log.Error("Undefined force table for " + name + " contact point");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (log.IsErrorEnabled)
                            log.Error("Exception " + e + " reading Gear Xml element" + ((XmlElement)currentNode).LocalName);

                    }
                }
            }

            if (!vXYZFound)
            {
                if (log.IsErrorEnabled)
                    log.Error("No location given for contact " + name);
                throw new Exception("No location given for contact " + name);
            }
            if (sBrakeGroup.Equals("LEFT")) eBrakeGrp = BrakeGroup.Left;
            else if (sBrakeGroup.Equals("RIGHT")) eBrakeGrp = BrakeGroup.Right;
            else if (sBrakeGroup.Equals("CENTER")) eBrakeGrp = BrakeGroup.Center;
            else if (sBrakeGroup.Equals("NOSE")) eBrakeGrp = BrakeGroup.Nose;
            else if (sBrakeGroup.Equals("TAIL")) eBrakeGrp = BrakeGroup.Tail;
            else if (sBrakeGroup.Equals("NONE")) eBrakeGrp = BrakeGroup.None;
            else if (sBrakeGroup.Length == 0)
            {
                eBrakeGrp = BrakeGroup.None;
                sBrakeGroup = "NONE (defaulted)";
            }
            else
            {
                if (log.IsErrorEnabled)
                    log.Error("Improper braking group specification in config file: "
                        + sBrakeGroup + " is undefined.");
            }

            if (maxSteerAngle == 360)
                sSteerType = "CASTERED";
            else if (maxSteerAngle == 0.0)
                sSteerType = "FIXED";
            else
                sSteerType = "STEERABLE";
            if (sSteerType.Equals("STEERABLE")) eSteerType = SteerType.Steer;
            else if (sSteerType.Equals("FIXED")) eSteerType = SteerType.Fixed;
            else if (sSteerType.Equals("CASTERED")) eSteerType = SteerType.Caster;
            else
            {
                if (log.IsErrorEnabled)
                    log.Error("Improper steering type specification in config file: "
                        + sSteerType + " is undefined.");
            }

            if (!dampReboundFound)
                bDampRebound = bDamp;

            vWhlBodyVec = FDMExec.MassBalance.StructuralToBody(vXYZ);

            vLocalGear = FDMExec.Propagate.GetTb2l() * vWhlBodyVec;

        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="File">the config file instance</param>
        /// <param name="exec">the parent executive object</param>
        /// <param name="number"></param>
        public LGear(string str, FDMExecutive exec, int number)
        {

            FDMExec = exec;

            GearNumber = number;

            StringReader sr = new StringReader(str);
            ReaderText reader = new ReaderText(sr);

            reader.ReadWord();
            name = reader.ReadWord();

            vXYZ = new Vector3D(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble());

            kSpring = reader.ReadDouble();
            bDamp = reader.ReadDouble();
            dynamicFCoeff = reader.ReadDouble();
            staticFCoeff = reader.ReadDouble();
            rollingFCoeff = reader.ReadDouble();

            sSteerType = reader.ReadWord();
            sBrakeGroup = reader.ReadWord();
            maxSteerAngle = reader.ReadInt();
            string sRetractable = reader.ReadWord();

            if (log.IsDebugEnabled)
            {
                log.Debug("name " + name);

                log.Debug("vXYZ(1) " + vXYZ.X);
                log.Debug("vXYZ(2) " + vXYZ.Y);
                log.Debug("vXYZ(3) " + vXYZ.Z);

                log.Debug("kSpring " + kSpring);
                log.Debug("bDamp " + bDamp);
                log.Debug("dynamicFCoeff " + dynamicFCoeff);
                log.Debug("staticFCoeff " + staticFCoeff);
                log.Debug("rollingFCoeff " + rollingFCoeff);

                log.Debug("sSteerType " + sSteerType);
                log.Debug("sBrakeGroup " + sBrakeGroup);
                log.Debug("maxSteerAngle " + maxSteerAngle);
                log.Debug("sRetractable " + sRetractable);
            }

            if (sBrakeGroup == "LEFT") eBrakeGrp = BrakeGroup.Left;
            else if (sBrakeGroup == "RIGHT") eBrakeGrp = BrakeGroup.Right;
            else if (sBrakeGroup == "CENTER") eBrakeGrp = BrakeGroup.Center;
            else if (sBrakeGroup == "NOSE") eBrakeGrp = BrakeGroup.Nose;
            else if (sBrakeGroup == "TAIL") eBrakeGrp = BrakeGroup.Tail;
            else if (sBrakeGroup == "NONE") eBrakeGrp = BrakeGroup.None;
            else
            {
                if (log.IsErrorEnabled)
                    log.Error("Improper braking group specification in config file: "
                        + sBrakeGroup + " is undefined.");
            }

            if (sSteerType == "STEERABLE") eSteerType = SteerType.Steer;
            else if (sSteerType == "FIXED") eSteerType = SteerType.Fixed;
            else if (sSteerType == "CASTERED") eSteerType = SteerType.Caster;
            else
            {
                if (log.IsErrorEnabled)
                    log.Error("Improper steering type specification in config file: "
                        + sSteerType + " is undefined.");
            }

            if (sRetractable == "RETRACT")
            {
                isRetractable = true;
            }
            else
            {
                isRetractable = false;
            }
            vWhlBodyVec = FDMExec.MassBalance.StructuralToBody(vXYZ);

            vLocalGear = FDMExec.Propagate.GetTb2l() * vWhlBodyVec;

        }


        /** Constructor
                @param lgear a reference to an existing FGLGear object     */

        public LGear(LGear lgear)
        {
            //TODO clon
        }

        /// The Force vector for this gear
        public Vector3D Force()
        {
#if TODO
            Vector3D normal, cvel;
            Location contact, gearLoc;
            double t = FDMExec.State.SimTime;

            vForce = Vector3D.Zero;
            vMoment = Vector3D.Zero;

            if (isRetractable)
                ComputeRetractionState();

            if (GearUp)
                return vForce;

            vWhlBodyVec = FDMExec.MassBalance.StructuralToBody(vXYZ);
            vLocalGear = FDMExec.Propagate.GetTb2l() * vWhlBodyVec;

            gearLoc = FDMExec.Propagate.GetLocation().LocalToLocation(vLocalGear);
            compressLength = -FDMExec.GroundCallback.GetAGLevel(t, gearLoc, out contact, out normal, out cvel);

            if (compressLength > 0.00)
            {

                WOW = true; // Weight-On-Wheels is true

                // [The next equation should really use the vector to the contact patch of
                // the tire including the strut compression and not the original vWhlBodyVec.]

                vWhlVelVec = FDMExec.Propagate.GetTb2l() * (Vector3D.Cross(FDMExec.Propagate.GetPQR(), vWhlBodyVec));
                vWhlVelVec += FDMExec.Propagate.GetVel() - cvel;
                compressSpeed = vWhlVelVec.Z;

                InitializeReporting();
                ComputeBrakeForceCoefficient();
                ComputeSteeringAngle();
                ComputeSlipAngle();
                ComputeSideForceCoefficient();
                ComputeVerticalStrutForce();

                // Compute the forces in the wheel ground plane.
                RollingForce = (1.0 - TirePressureNorm) * 30
                               + vLocalForce.eZ * BrakeFCoeff * (RollingWhlVel >= 0 ? 1.0 : -1.0);
                SideForce = vLocalForce.eZ * FCoeff;

                // Transform these forces back to the local reference frame.

                vLocalForce.eX = RollingForce * CosWheel - SideForce * SinWheel;
                vLocalForce.eY = SideForce * CosWheel + RollingForce * SinWheel;

                // Transform the forces back to the body frame and compute the moment.

                vForce = FDMExec.Propagate.GetTl2b() * vLocalForce;

                // Lag and attenuate the XY-plane forces dependent on velocity

                double RFRV = 0.015; // Rolling force relaxation velocity
                double SFRV = 0.25;  // Side force relaxation velocity
                double dT = FDMExec.State.DeltaTime * FDMExec.GroundReactions.Rate;

                In = vForce;
                vForce.eX = (0.25) * (In.eX + prevIn.eX) + (0.50) * prevOut.eX;
                vForce.eY = (0.15) * (In.eY + prevIn.eY) + (0.70) * prevOut.eY;
                prevOut = vForce;
                prevIn = In;

                if (Math.Abs(RollingWhlVel) <= RFRV) vForce.eX *= Math.Abs(RollingWhlVel) / RFRV;
                if (Math.Abs(SideWhlVel) <= SFRV) vForce.eY *= Math.Abs(SideWhlVel) / SFRV;

                vMoment = Vector3D.Cross(vWhlBodyVec, vForce);
            }
            else
            { // Gear is NOT compressed
                WOW = false;
                compressLength = 0.0;

                // Return to neutral position between 1.0 and 0.8 gear pos.
                SteerAngle *= Math.Max(FDMExec.FlightControlSystem.GearPosition - 0.8, 0.0) / 0.2;

                ResetReporting();
            }

            ReportTakeoffOrLanding();
            CrashDetect();

            return vForce;
#endif
            throw new NotImplementedException("Pending upgrade to lastest version of JSBSIM");
        }

        public bool IsBogey() {return (sContactType.Equals("BOGEY"));}

        /// The Moment vector for this gear
        public Vector3D Moment() { return vMoment; }

        /// Gets the location of the gear in Body axes
        public Vector3D GetBodyLocation() { return vWhlBodyVec; }
        public double GetBodyLocation(int idx) { return vWhlBodyVec[idx]; }

        public Vector3D GetLocalGear() { return vLocalGear; }
        public double GetLocalGear(int idx) { return vLocalGear[idx]; }

        /// Gets the name of the gear
        public string GetName() { return name; }
        /// Gets the Weight On Wheels flag value
        public bool GetWOW() { return WOW; }
        /// Gets the current compressed length of the gear in feet
        public double GetCompLen() { return compressLength; }
        /// Gets the current gear compression velocity in ft/sec
        public double GetCompVel() { return compressSpeed; }
        /// Gets the gear compression force in pounds
        public double GetCompForce() { return Force().Z; }
        public double GetBrakeFCoeff() { return BrakeFCoeff; }
        public double GetXYZ(int i) { return vXYZ[i]; }

        /// Gets the current normalized tire pressure
        public double GetTirePressure() { return TirePressureNorm; }
        /// Sets the new normalized tire pressure
        public void SetTirePressure(double p) { TirePressureNorm = p; }

        /// Sets the brake value in percent (0 - 100)
        public void SetBrake(double bp) { brakePct = bp; }

        /** Set the console touchdown reporting feature
                @param flag true turns on touchdown reporting, false turns it off */
        public void SetReport(bool flag) { ReportEnable = flag; }
        /** Get the console touchdown reporting feature
                @return true if reporting is turned on */
        public bool GetReport() { return ReportEnable; }
        public double GetSteerNorm() { return Constants.radtodeg / maxSteerAngle * SteerAngle; }
        public double GetDefaultSteerAngle(double cmd) { return cmd * maxSteerAngle; }
        public double GetstaticFCoeff() { return staticFCoeff; }
        public double GetdynamicFCoeff() { return dynamicFCoeff; }
        public double GetrollingFCoeff() { return rollingFCoeff; }

        public int GetBrakeGroup() { return (int)eBrakeGrp; }
        public int GetSteerType() { return (int)eSteerType; }

        public bool GetSteerable() { return eSteerType != SteerType.Fixed; }
        public bool GetRetractable() { return isRetractable; }
        public bool GetGearUnitUp() { return GearUp; }
        public bool GetGearUnitDown() { return GearDown; }
        public double GetWheelSideForce() { return SideForce; }
        public double GetWheelRollForce() { return RollingForce; }
        public double GetBodyXForce() { return vLocalForce.X; }
        public double GetBodyYForce() { return vLocalForce.Y; }
        public double GetWheelSlipAngle() { return WheelSlip; }
        public Vector3D GetWheelVel() { return vWhlVelVec; }
        public double GetWheelVel(int axis) { return vWhlVelVec[axis]; }
        public double GetkSpring() { return kSpring; }
        public double GetbDamp() { return bDamp; }
        public double GetmaxSteerAngle() { return maxSteerAngle; }
        public string GetsBrakeGroup() { return sBrakeGroup; }
        public bool IsRetractable { get { return isRetractable; } }
        public string GetsSteerType() { return sSteerType; }

        private void ComputeRetractionState()
        {
            if (FDMExec.FlightControlSystem.GearPosition < 0.01)
            {
                GearUp = true;
                GearDown = false;
            }
            else if (FDMExec.FlightControlSystem.GearPosition > 0.99)
            {
                GearDown = true;
                GearUp = false;
            }
            else
            {
                GearUp = false;
                GearDown = false;
            }
        }

        // Takeoff and landing reporting functionality
        private void ReportTakeoffOrLanding()
        {
            double deltaT = FDMExec.State.DeltaTime * FDMExec.GroundReactions.Rate;

            if (FirstContact) LandingDistanceTraveled += FDMExec.Auxiliary.VGround * deltaT;

            if (StartedGroundRun)
            {
                TakeoffDistanceTraveled50ft += FDMExec.Auxiliary.VGround * deltaT;
                if (WOW) TakeoffDistanceTraveled += FDMExec.Auxiliary.VGround * deltaT;
            }

            if (ReportEnable && FDMExec.Auxiliary.VGround <= 0.05 && !LandingReported)
            {
                if (log.IsDebugEnabled)
                    Report(ReportType.Land);
            }

            if (ReportEnable && !TakeoffReported &&
                (vLocalGear.Z - FDMExec.Propagate.DistanceAGL) < -50.0)
            {
                if (log.IsDebugEnabled)
                    Report(ReportType.Takeoff);
            }

            if (lastWOW != WOW)
            {
                if (log.IsDebugEnabled)
                    log.Debug("GEAR_CONTACT: " + name + " " + WOW);
            }

            lastWOW = WOW;
        }

        //%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
        // Crash detection logic (really out-of-bounds detection)
        private void CrashDetect()
        {
            if (compressLength > 500.0 ||
            vForce.Magnitude() > 100000000.0 ||
            vMoment.Magnitude() > 5000000000.0 ||
            SinkRate > 1.4666 * 30)
            {
                if (log.IsDebugEnabled)
                    log.Debug("Crash Detected: Simulation FREEZE.");
                FDMExec.State.SuspendIntegration();
            }
        }

        // Reset reporting functionality after takeoff
        private void ResetReporting()
        {
            if (FDMExec.Propagate.DistanceAGL > 200.0)
            {
                FirstContact = false;
                StartedGroundRun = false;
                LandingReported = false;
                LandingDistanceTraveled = 0.0;
                MaximumStrutForce = MaximumStrutTravel = 0.0;
            }
        }

        private void InitializeReporting()
        {
            // If this is the first time the wheel has made contact, remember some values
            // for later printout.

            if (!FirstContact)
            {
                FirstContact = true;
                SinkRate = compressSpeed;
                GroundSpeed = FDMExec.Propagate.GetVel().Magnitude();
                TakeoffReported = false;
            }

            // If the takeoff run is starting, initialize.

            if ((FDMExec.Propagate.GetVel().Magnitude() > 0.1) &&
                (FDMExec.FlightControlSystem.GetBrake(BrakeGroup.Left) == 0) &&
                (FDMExec.FlightControlSystem.GetBrake(BrakeGroup.Right) == 0) &&
                (FDMExec.FlightControlSystem.GetThrottlePos(0) == 1) && !StartedGroundRun)
            {
                TakeoffDistanceTraveled = 0;
                TakeoffDistanceTraveled50ft = 0;
                StartedGroundRun = true;
            }
        }

        // The following needs work regarding friction coefficients and braking and
        // steering The BrakeFCoeff formula assumes that an anti-skid system is used.
        // It also assumes that we won't be turning and braking at the same time.
        // Will fix this later.
        // [JSB] The braking force coefficients include normal rolling coefficient +
        // a percentage of the static friction coefficient based on braking applied.
        private void ComputeBrakeForceCoefficient()
        {
            switch (eBrakeGrp)
            {
                case BrakeGroup.Left:
                    BrakeFCoeff = (rollingFCoeff * (1.0 - FDMExec.FlightControlSystem.GetBrake(BrakeGroup.Left)) +
                        staticFCoeff * FDMExec.FlightControlSystem.GetBrake(BrakeGroup.Left));
                    break;
                case BrakeGroup.Right:
                    BrakeFCoeff = (rollingFCoeff * (1.0 - FDMExec.FlightControlSystem.GetBrake(BrakeGroup.Right)) +
                        staticFCoeff * FDMExec.FlightControlSystem.GetBrake(BrakeGroup.Right));
                    break;
                case BrakeGroup.Center:
                    BrakeFCoeff = (rollingFCoeff * (1.0 - FDMExec.FlightControlSystem.GetBrake(BrakeGroup.Center)) +
                        staticFCoeff * FDMExec.FlightControlSystem.GetBrake(BrakeGroup.Center));
                    break;
                case BrakeGroup.Nose:
                    BrakeFCoeff = (rollingFCoeff * (1.0 - FDMExec.FlightControlSystem.GetBrake(BrakeGroup.Center)) +
                        staticFCoeff * FDMExec.FlightControlSystem.GetBrake(BrakeGroup.Center));
                    break;
                case BrakeGroup.Tail:
                    BrakeFCoeff = (rollingFCoeff * (1.0 - FDMExec.FlightControlSystem.GetBrake(BrakeGroup.Center)) +
                        staticFCoeff * FDMExec.FlightControlSystem.GetBrake(BrakeGroup.Center));
                    break;
                case BrakeGroup.None:
                    BrakeFCoeff = rollingFCoeff;
                    break;
                default:
                    if (log.IsErrorEnabled)
                        log.Error("Improper brake group membership detected for this gear.");
                    break;
            }
        }

        private void ComputeSlipAngle()
        {
            double deltaT = FDMExec.State.DeltaTime * FDMExec.Aircraft.Rate;

            // Transform the wheel velocities from the local axis system to the wheel axis system.

            RollingWhlVel = vWhlVelVec.X * CosWheel + vWhlVelVec.Y * SinWheel;
            SideWhlVel = vWhlVelVec.Y * CosWheel - vWhlVelVec.X * SinWheel;

            // Calculate tire slip angle.

            if (Math.Abs(RollingWhlVel) < 0.1 && Math.Abs(SideWhlVel) < 0.01)
            {
                WheelSlip = -SteerAngle * Constants.radtodeg;
            }
            else
            {
                WheelSlip = Math.Atan2(SideWhlVel, Math.Abs(RollingWhlVel)) * Constants.radtodeg;
            }
            slipIn = WheelSlip;
            WheelSlip = (0.46) * (slipIn + last_SlipIn) + (0.08) * lastWheelSlip;
            lastWheelSlip = WheelSlip;
            last_SlipIn = slipIn;
        }

        // Compute the steering angle in any case.
        // This will also make sure that animations will look right.
        private void ComputeSteeringAngle()
        {
            switch (eSteerType)
            {
                case SteerType.Steer:
                    SteerAngle = Constants.degtorad * FDMExec.FlightControlSystem.GetSteerPosDeg(GearNumber);
                    break;
                case SteerType.Fixed:
                    SteerAngle = 0.0;
                    break;
                case SteerType.Caster:
                    // Note to Jon: This is not correct for castering gear.  I'll fix it later.
                    SteerAngle = 0.0;
                    break;
                default:
                    if (log.IsErrorEnabled)
                        log.Error("Improper steering type membership detected for this gear.");
                    break;
            }
            SinWheel = Math.Sin(FDMExec.Propagate.GetEuler().Psi + SteerAngle);
            CosWheel = Math.Cos(FDMExec.Propagate.GetEuler().Psi + SteerAngle);
        }

        // Compute the sideforce coefficients using similar assumptions to LaRCSim for now.
        // Allow a maximum of 10 degrees tire slip angle before wheel slides.  At that point,
        // transition from static to dynamic friction.  There are more complicated formulations
        // of this that avoid the discrete jump (similar to Pacejka).  Will fix this later.
        private void ComputeSideForceCoefficient()
        {
            if (ForceY_Table != null)
            {

                FCoeff = ForceY_Table.GetValue(WheelSlip);

            }
            else
            {

                if (Math.Abs(WheelSlip) <= 10.0)
                {
                    FCoeff = staticFCoeff * WheelSlip / 10.0;
                }
                else if (Math.Abs(WheelSlip) <= 40.0)
                {
                    FCoeff = (dynamicFCoeff * (Math.Abs(WheelSlip) - 10.0) / 10.0
                              + staticFCoeff * (40.0 - Math.Abs(WheelSlip)) / 10.0) * (WheelSlip >= 0 ? 1.0 : -1.0);
                }
                else
                {
                    FCoeff = dynamicFCoeff * (WheelSlip >= 0 ? 1.0 : -1.0);
                }
            }
        }

        // Compute the vertical force on the wheel using square-law damping (per comment
        // in paper AIAA-2000-4303 - see header prologue comments). We might consider
        // allowing for both square and linear damping force calculation. Also need to
        // possibly give a "rebound damping factor" that differs from the compression
        // case.
        private void ComputeVerticalStrutForce()
        {
            double springForce = 0;
            double dampForce = 0;

            springForce = -compressLength * kSpring;

            if (compressSpeed >= 0.0)
            {
                dampForce = -compressSpeed * bDamp;
            }
            else
            {
                dampForce = -compressSpeed * bDampRebound;
            }
            vLocalForce.eZ = Math.Min(springForce + dampForce, (double)0.0);

            // Remember these values for reporting
            MaximumStrutForce = Math.Max(MaximumStrutForce, Math.Abs(vLocalForce.eZ));
            MaximumStrutTravel = Math.Max(MaximumStrutTravel, Math.Abs(compressLength));
        }

        private int GearNumber;
        private Vector3D vXYZ;
        private Vector3D vMoment;
        private Vector3D vWhlBodyVec;
        private Vector3D vLocalGear;
        private Vector3D vForce;
        private Vector3D vLocalForce;
        private Vector3D vWhlVelVec;     // Velocity of this wheel (Local)
        private Vector3D In;
        private Vector3D prevIn;
        private Vector3D prevOut;
        private Table ForceY_Table;
        private double SteerAngle;
        private double kSpring;
        private double bDamp;
        private double bDampRebound;
        private double compressLength = 0.0;
        private double compressSpeed = 0.0;
        private double staticFCoeff, dynamicFCoeff, rollingFCoeff;
        private double brakePct = 0.0;
        private double BrakeFCoeff;
        //TODO Not used private double maxCompLen = 0.0;
        private double SinkRate = 0.0;
        private double GroundSpeed = 0.0;
        private double TakeoffDistanceTraveled = 0.0;
        private double TakeoffDistanceTraveled50ft = 0.0;
        private double LandingDistanceTraveled = 0.0;
        private double MaximumStrutForce = 0.0;
        private double MaximumStrutTravel = 0.0;
        private double SideWhlVel, RollingWhlVel;
        private double RollingForce = 0.0, SideForce = 0.0, FCoeff;
        private double WheelSlip = 0.0;
        private double lastWheelSlip = 0.0;
        private double slipIn;
        private double last_SlipIn;
        private double TirePressureNorm = 1.0;
        private double SinWheel, CosWheel;
        private bool WOW = true;
        private bool lastWOW = true;
        private bool FirstContact = false;
        private bool StartedGroundRun = false;
        private bool LandingReported = false;
        private bool TakeoffReported = false;
        private bool ReportEnable;
        private bool isRetractable;
        private bool GearUp = false, GearDown = true;
        // private bool Servicable = true;  TODO remove this??
        private string name;
        private string sSteerType;
        private string sBrakeGroup = "";
        private string sContactType;

        private BrakeGroup eBrakeGrp;
        private SteerType eSteerType;
        private double maxSteerAngle;

        private FDMExecutive FDMExec;

        private void Report(ReportType rt)
        {
            //TODO
        }
        private const string IdSrc = "$Id: FGLGear.cpp,v 1.119 2005/01/13 07:15:32 frohlich Exp $";
    }
}
