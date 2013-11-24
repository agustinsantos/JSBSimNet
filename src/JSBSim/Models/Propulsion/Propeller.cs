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

namespace JSBSim.Models.Propulsion
{
    using System;
    using System.Xml;

    // Import log4net classes.
    using log4net;

    using CommonUtils.MathLib;
    using JSBSim.InputOutput;
    using JSBSim.MathValues;
    using JSBSim.Format;

    /// <summary>
    /// Propeller modeling class.
    /// Propeller models a propeller given the tabular data for Ct and Cp
    /// indexed by advance ratio "J". The data for the propeller is
    /// stored in a config file named "prop_name.xml". The propeller config file
    /// is referenced from the main aircraft config file in the "Propulsion" section.
    /// See the constructor for FGPropeller to see what is read in and what should
    /// be stored in the config file.<br>
    /// Several references were helpful, here:<ul>
    /// <li>Barnes W. McCormick, "Aerodynamics, Aeronautics, and Flight Mechanics",
    /// Wiley & Sons, 1979 ISBN 0-471-03032-5</li>
    /// <li>Edwin Hartman, David Biermann, "The Aerodynamic Characteristics of
    /// Full Scale Propellers Having 2, 3, and 4 Blades of Clark Y and R.A.F. 6
    /// Airfoil Sections", NACA Report TN-640, 1938 (?)</li>
    /// <li>Various NACA Technical Notes and Reports</li>
    /// </ul>
    /// @author Jon S. Berndt
    /// </summary>
    public class Propeller : Thruster
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


        public Propeller(FDMExecutive exec, XmlElement parent, XmlElement prop_element, int number)
            : base(exec, parent, prop_element, number)
        {
            string name = "";

            MaxPitch = MinPitch = P_Factor = Pitch = Advance = MinRPM = MaxRPM = 0.0;
            rotationSense = 1; // default clockwise rotation
            ReversePitch = 0.0;
            Reversed = false;
            Feathered = false;
            Reverse_coef = 0.0;
            gearRatio = 1.0;

            string token;
            XmlElement tmpElem;
            foreach (XmlNode currentNode in prop_element.ChildNodes)
            {
                if (currentNode.NodeType == XmlNodeType.Element)
                {
                    tmpElem = currentNode as XmlElement;
                    token = tmpElem.LocalName;

                    if (token.Equals("ixx"))
                        Ixx = FormatHelper.ValueAsNumberConvertTo(tmpElem, "SLUG*FT2");
                    else if (token.Equals("diameter"))
                        Diameter = FormatHelper.ValueAsNumberConvertTo(tmpElem, "FT");
                    else if (token.Equals("numblades"))
                        numBlades = (int)FormatHelper.ValueAsNumber(tmpElem);
                    else if (token.Equals("gearratio"))
                        gearRatio = FormatHelper.ValueAsNumber(tmpElem);
                    else if (token.Equals("minpitch"))
                        MinPitch = FormatHelper.ValueAsNumber(tmpElem);
                    else if (token.Equals("maxpitch"))
                        MaxPitch = FormatHelper.ValueAsNumber(tmpElem);
                    else if (token.Equals("minrpm"))
                        MinRPM = FormatHelper.ValueAsNumber(tmpElem);
                    else if (token.Equals("maxrpm"))
                        MaxRPM = FormatHelper.ValueAsNumber(tmpElem);
                    else if (token.Equals("reversepitch"))
                        ReversePitch = FormatHelper.ValueAsNumber(tmpElem);
                    else if (token.Equals("table"))
                    {
                        name = tmpElem.GetAttribute("name");
                        if (name.Equals("C_THRUST"))
                        {
                            cThrust = new Table(exec.PropertyManager, tmpElem);
                        }
                        else if (name.Equals("C_POWER"))
                        {
                            cPower = new Table(exec.PropertyManager, tmpElem);
                        }
                        else
                        {
                            if (log.IsErrorEnabled)
                                log.Error("Unknown table type: " + name + " in propeller definition.");
                        }

                    }
                    else if (token.Equals("sense"))
                    {
                        double senseTmp = FormatHelper.ValueAsNumber(tmpElem);
                        RotationSense = Math.Abs(senseTmp) / senseTmp;
                    }
                    else if (token.Equals("p_factor"))
                        P_Factor = FormatHelper.ValueAsNumber(tmpElem);
                }
            }
        
            if (P_Factor < 0)
            {
                if (log.IsErrorEnabled)
                        log.Error("P-Factor value in config file must be greater than zero");
            }

            thrusterType = ThrusterType.Propeller;
            RPM = 0;
            vTorque = Vector3D.Zero;
            D4 = Diameter * Diameter * Diameter * Diameter;
            D5 = D4 * Diameter;

            string property_name = "propulsion/engine[" + engineNum + "]/advance-ratio";
            FDMExec.PropertyManager.Tie(property_name, this.GetAdvanceRatio, null);
            property_name = "propulsion/engine[" + engineNum + "]/blade-angle";
            FDMExec.PropertyManager.Tie(property_name, this.GetPitch, this.SetPitch);

        }



        /// <summary>
        /// Sets the Revolutions Per Minute for the propeller. Normally the propeller
        /// instance will calculate its own rotational velocity, given the Torque
        /// produced by the engine and integrating over time using the standard
        /// equation for rotational acceleration "a": a = Q/I , where Q is Torque and
        /// I is moment of inertia for the propeller.
        /// </summary>
        /// <param name="rpm">the rotational velocity of the propeller</param>
        public override void SetRPM(double rpm) { RPM = rpm; }

        /// Returns true of this propeller is variable pitch
        public bool IsVPitch() { return MaxPitch != MinPitch; }

        /// <summary>
        /// This commands the pitch of the blade to change to the value supplied.
        /// This call is meant to be issued either from the cockpit or by the flight
        /// control system (perhaps to maintain constant RPM for a constant-speed
        /// propeller). This value will be limited to be within whatever is specified
        /// in the config file for Max and Min pitch. It is also one of the lookup
        /// indices to the power and thrust tables for variable-pitch propellers.
        /// 
        /// </summary>
        /// <param name="pitch">the pitch of the blade in degrees</param>
        public void SetPitch(double pitch) { Pitch = pitch; }

        public void SetAdvance(double advance) { Advance = advance; }

        /// Sets the P-Factor constant
        public void SetPFactor(double pf) { P_Factor = pf; }

        /// <summary>
        /// Gets/Sets the rotation sense of the propeller.
        /// </summary>
        /// <param name="s">this value should be +/- 1 ONLY. +1 indicates clockwise rotation as
        ///		 viewed by someone standing behind the engine looking forward into
        ///		 the direction of flight</param>
        public double RotationSense
        {
            get { return rotationSense; }
            set { rotationSense = value; }
        }

        public double GetPFactorValue() { return P_Factor; }

        /// Retrieves the pitch of the propeller in degrees.
        public double GetPitch() { return Pitch; }

        /// Retrieves the RPMs of the propeller
        public override double GetRPM() { return RPM; }

        /// Retrieves the propeller moment of inertia
        public double GetIxx() { return Ixx; }

        /// Retrieves the Torque in foot-pounds (Don't you love the English system?)
        public double GetTorque() { return vTorque.X; }

        public double GetAdvanceRatio()
        {
            return J;
        }

        /// <summary>
        /// Retrieves the power required (or "absorbed") by the propeller -
        /// i.e. the power required to keep spinning the propeller at the current
        /// velocity, air density,  and rotational rate. */
        /// </summary>
        /// <returns></returns>
        public override double GetPowerRequired()
        {
            double cPReq, J;
            double rho = FDMExec.Atmosphere.Density;
            double RPS = RPM / 60.0;

            if (RPS != 0) J = FDMExec.Auxiliary.GetAeroUVW().U / (Diameter * RPS);
            else J = 1000.0; // Set J to a high number

            if (MaxPitch == MinPitch)
            { // Fixed pitch prop
                Pitch = MinPitch;
                cPReq = cPower.GetValue(J);
            }
            else
            {                    // Variable pitch prop

                if (MaxRPM != MinRPM)
                {   // fixed-speed prop

                    // do normal calculation when propeller is neither feathered nor reversed
                    if (!Feathered)
                    {
                        if (!Reversed)
                        {

                            double rpmReq = MinRPM + (MaxRPM - MinRPM) * Advance;
                            double dRPM = rpmReq - RPM;
                            // The pitch of a variable propeller cannot be changed when the RPMs are
                            // too low - the oil pump does not work.
                            if (RPM > 200) Pitch -= dRPM / 10;

                            if (Pitch < MinPitch) Pitch = MinPitch;
                            else if (Pitch > MaxPitch) Pitch = MaxPitch;

                        }
                        else
                        { // Reversed propeller

                            // when reversed calculate propeller pitch depending on throttle lever position
                            // (beta range for taxing full reverse for braking)
                            double PitchReq = MinPitch - (MinPitch - ReversePitch) * Reverse_coef;
                            // The pitch of a variable propeller cannot be changed when the RPMs are
                            // too low - the oil pump does not work.
                            if (RPM > 200) Pitch += (PitchReq - Pitch) / 200;
                            if (RPM > MaxRPM)
                            {
                                Pitch += (MaxRPM - RPM) / 50;
                                if (Pitch < ReversePitch) Pitch = ReversePitch;
                                else if (Pitch > MaxPitch) Pitch = MaxPitch;
                            }
                        }

                    }
                    else
                    { // Feathered propeller
                        // ToDo: Make feathered and reverse settings done via FGKinemat
                        Pitch += (MaxPitch - Pitch) / 300; // just a guess (about 5 sec to fully feathered)
                    }
                }
                else // Reversed propeller
                {
                    Pitch = MinPitch + (MaxPitch - MinPitch) * Advance;
                }
                cPReq = cPower.GetValue(J, Pitch);
            }


            if (RPS > 0)
            {
                powerRequired = cPReq * RPS * RPS * RPS * D5 * rho;
                vTorque.X = -rotationSense * powerRequired / (RPS * 2.0 * Math.PI);
            }
            else
            {
                powerRequired = 0.0;
                vTorque.X = 0.0;
            }

            return powerRequired;
        }


        /// <summary>
        /// Calculates and returns the thrust produced by this propeller.
        /// Given the excess power available from the engine (in foot-pounds), the thrust is
        /// calculated, as well as the current RPM. The RPM is calculated by integrating
        /// the torque provided by the engine over what the propeller "absorbs"
        /// (essentially the "drag" of the propeller).
        /// </summary>
        /// <param name="PowerAvailable">this is the excess power provided by the engine to
        /// accelerate the prop. It could be negative, dictating that the propeller
        /// would be slowed.</param>
        /// <returns>the thrust in pounds</returns>
        public override double Calculate(double PowerAvailable)
        {
            double omega, alpha, beta;
            double Vel = FDMExec.Auxiliary.GetAeroUVW().U;
            double rho = FDMExec.Atmosphere.Density;
            double RPS = RPM / 60.0;

            if (RPM > 0.10)
            {
                J = Vel / (Diameter * RPS);
            }
            else
            {
                J = 0.0;
            }

            if (MaxPitch == MinPitch)
            { // Fixed pitch prop
                thrustCoeff = cThrust.GetValue(J);
            }
            else
            {                    // Variable pitch prop
                thrustCoeff = cThrust.GetValue(J, Pitch);
            }

            if (P_Factor > 0.0001)
            {
                alpha = FDMExec.Auxiliary.Getalpha();
                beta = FDMExec.Auxiliary.Getbeta();
                SetActingLocationY(GetLocationY() + P_Factor * alpha * rotationSense);
                SetActingLocationZ(GetLocationZ() + P_Factor * beta * rotationSense);
            }

            thrust = thrustCoeff * RPS * RPS * Diameter * Diameter * Diameter * Diameter * rho;
            omega = RPS * 2.0 * Math.PI;

            vFn.X = thrust;

            // The Ixx value and rotation speed given below are for rotation about the
            // natural axis of the engine. The transform takes place in the base class
            // FGForce::GetBodyForces() function.

            vH.X = Ixx * omega * rotationSense;
            vH.Y = 0.0;
            vH.Z = 0.0;

            if (omega > 0.0) ExcessTorque = gearRatio * PowerAvailable / omega;
            else ExcessTorque = gearRatio * PowerAvailable / 1.0;

            RPM = (RPS + ((ExcessTorque / Ixx) / (2.0 * Math.PI)) * deltaT) * 60.0;

            if (RPM < 1.0)
                RPM = 0; // Engine friction stops rotation arbitrarily at 1 RPM.

            vMn = Vector3D.Cross(FDMExec.Propagate.GetPQR(), vH) + vTorque * rotationSense;

            return thrust; // return thrust in pounds

        }

        public Vector3D GetPFactor()
        {
            double px = 0.0, py, pz;

            py = thrust * rotationSense * (GetActingLocationY() - GetLocationY()) / 12.0;
            pz = thrust * rotationSense * (GetActingLocationZ() - GetLocationZ()) / 12.0;

            return new Vector3D(px, py, pz);
        }

        public override string GetThrusterLabels(int id, string delimeter)
        {
            string buf;

            buf = Name + "_Torque[" + id + "]" + delimeter
                + Name + "_PFactor_Pitch[" + id + "]" + delimeter
                + Name + "_PFactor_Yaw[" + id + "]" + delimeter
                + Name + "_Thrust[" + id + "]" + delimeter;
            if (IsVPitch())
                buf += Name + "_Pitch[" + id + "]" + delimeter;
            buf += Name + "_RPM[" + id + "]";

            return buf;
        }

        public override string GetThrusterValues(int id, string delimeter)
        {
            string buf;

            Vector3D vPFactor = GetPFactor();
            buf = vTorque.X + delimeter
                + vPFactor.Pitch + delimeter
                + vPFactor.Yaw + delimeter
                + thrust + delimeter;
            if (IsVPitch())
                buf += Pitch + delimeter;
            buf += RPM;

            return buf;
        }

        public void SetReverseCoef(double c) { Reverse_coef = c; }
        public double GetReverseCoef() { return Reverse_coef; }
        public void SetReverse(bool r) { Reversed = r; }
        public bool GetReverse() { return Reversed; }
        public void SetFeather(bool f) { Feathered = f; }
        public bool GetFeather() { return Feathered; }

        private int numBlades;
        private double J;
        private double RPM;
        private double Ixx;
        private double Diameter;
        private double MaxPitch = 0.0;
        private double MinPitch = 0.0;
        private double MinRPM = 0.0;
        private double MaxRPM = 0.0;
        private double Pitch;
        private double P_Factor = 0.0;
        private double rotationSense; //Sense in the JSBSim Original
        private double Advance;
        private double ExcessTorque;
        private double D4;
        private double D5;
        private Vector3D vTorque;
        private Table cThrust;
        private Table cPower;

        private double ReversePitch; // Pitch, when fully reversed
        private bool Reversed;		 // true, when propeller is reversed
        private double Reverse_coef; // 0 - 1 defines AdvancePitch (0=MIN_PITCH 1=REVERSE_PITCH)
        private bool Feathered;    // true, if feather command

        private const string IdSrc = "$Id: FGPropagate.cpp,v 1.19 2004/12/08 01:28:42 jberndt Exp $";
    }
}
