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
	using System.Collections;
    using System.Collections.Generic;
	using System.Xml;
	using System.IO;
    using System.Text;

	// Import log4net classes.
	using log4net;

	using CommonUtils.MathLib;
	using JSBSim.Models.FlightControl;
	using JSBSim.InputOutput;
	using JSBSim.Script;

	/// <summary>
	/// Encapsulates the Flight Control System (FCS) functionality.
	/// This class owns and contains the list of FCSComponents
	/// that define the control system for this aircraft. The config file for the
	/// aircraft contains a description of the control path that starts at an input
	/// or command and ends at an effector, e.g. an aerosurface. The FCS components
	/// which comprise the control laws for an axis are defined sequentially in
	/// the configuration file. For instance, for the X-15:
	/// 
	/// <pre>
	/// \<FLIGHT_CONTROL NAME="X-15 SAS">
	/// 
	/// \<COMPONENT NAME="Pitch Trim Sum" TYPE="SUMMER">
	///    INPUT        fcs/elevator-cmd-norm
	///    INPUT        fcs/pitch-trim-cmd-norm
	///    CLIPTO       -1 1
	/// \</COMPONENT>
	/// 
	/// \<COMPONENT NAME="Pitch Command Scale" TYPE="AEROSURFACE_SCALE">
	///   INPUT        fcs/pitch-trim-sum
	///   MIN         -50
	///   MAX          50
	/// \</COMPONENT>
	/// 
	/// \<COMPONENT NAME="Pitch Gain 1" TYPE="PURE_GAIN">
	///   INPUT        fcs/pitch-command-scale
	///   GAIN         -0.36
	/// \</COMPONENT>
	/// 
	/// ... etc.
	/// </pre>
	/// 
	/// In the above case we can see the first few components of the pitch channel
	/// defined. The input to the first component, as can be seen in the "Pitch trim
	/// sum" component, is really the sum of two parameters: elevator command (from
	/// the stick - a pilot input), and pitch trim. The type of this component is
	/// "Summer".
	/// The next component created is an aerosurface scale component - a type of
	/// gain (see the LoadFCS() method for insight on how the various types of
	/// components map into the actual component classes).  This continues until the
	/// final component for an axis when the
	/// OUTPUT keyword specifies where the output is supposed to go. See the
	/// individual components for more information on how they are mechanized.
	/// 
	/// Another option for the flight controls portion of the config file is that in
	/// addition to using the "NAME" attribute in,
	/// 
	/// <pre>
	/// \<FLIGHT_CONTROL NAME="X-15 SAS">
	/// </pre>
	/// 
	/// one can also supply a filename:
	/// 
	/// <pre>
	/// \<FLIGHT_CONTROL NAME="X-15 SAS" FILE="X15.xml">
	/// \</FLIGHT_CONTROL>
	/// </pre>
	/// 
	/// In this case, the FCS would be read in from another file.
	/// 
	/// </summary>
    public class FlightControlSystem : Model
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

        public enum FcIdx { De, DaL, DaR, Dr, Dsb, Dsp, Df, NNorm } ;
        public enum OutputForm { Rad, Deg, Norm, Mag, NForms }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="exec">the parent executive object</param>
        public FlightControlSystem(FDMExecutive exec)
            : base(exec)
        {
            int i;
            Name = "FGFCS";

            DaCmd = DeCmd = DrCmd = DfCmd = DsbCmd = DspCmd = 0.0;
            AP_DaCmd = AP_DeCmd = AP_DrCmd = AP_ThrottleCmd = 0.0;
            PTrimCmd = YTrimCmd = RTrimCmd = 0.0;
            gearCmd = gearPos = 1; // default to gear down
            LeftBrake = RightBrake = CenterBrake = 0.0;
            APAttitudeSetPt = APAltitudeSetPt = APHeadingSetPt = APAirspeedSetPt = 0.0;
            DoNormalize = true;

            for (i = 0; i < NForms; i++)
            {
                DePos[i] = DaLPos[i] = DaRPos[i] = DrPos[i] = 0.0;
                DfPos[i] = DsbPos[i] = DspPos[i] = 0.0;
            }

            for (i = 0; i < NNorm; i++) { ToNormalize[i] = -1; }

        }
        /// <summary>
        /// Runs the Flight Controls model; called by the Executive
        /// </summary>
        /// <returns>false if no error</returns>
        public override bool Run(bool Holding)
        {
            int i;

            if (InternalRun()) return true;
            if (FDMExec.Holding()) return false; // if paused don't execute


            // Set the default engine commands
            for (i = 0; i < ThrottlePos.Count; i++) ThrottlePos[i] = ThrottleCmd[i];
            for (i = 0; i < MixturePos.Count; i++) MixturePos[i] = MixtureCmd[i];
            for (i = 0; i < PropAdvance.Count; i++) PropAdvance[i] = PropAdvanceCmd[i];

            // Set the default steering angle
            for (i = 0; i < SteerPosDeg.Count; i++)
            {
                LGear gear = FDMExec.GroundReactions.GetGearUnit(i);
                SteerPosDeg[i] = gear.GetDefaultSteerAngle(this.SteeringCmd);
            }

            for (i = 0; i < APComponents.Count; i++)
                APComponents[i].Run(); // cycle AP components
            for (i = 0; i < FCSComponents.Count; i++)
                FCSComponents[i].Run(); // cycle FCS components

            if (DoNormalize) Normalize();

            return false;
        }


        /// <summary>
        /// Gets the throttle command.
        /// </summary>
        /// <param name="engineNum">engine ID number</param>
        /// <returns>throttle command in percent ( 0 - 100) for the given engine</returns>
        public double GetThrottleCmd(int engineNum)
        {
            if (engineNum < (int)ThrottlePos.Count)
            {
                if (engineNum < 0)
                {
                    if (log.IsErrorEnabled)
                        log.Error("Cannot get throttle value for ALL engines");
                }
                else
                {
                    return (double)ThrottleCmd[engineNum];
                }
            }
            else
            {
                if (log.IsErrorEnabled)
                    log.Error("Throttle " + engineNum + " does not exist! " + ThrottleCmd.Count
                + " engines exist, but throttle setting for engine " + engineNum
                + " is selected");
            }
            return 0.0;
        }

        internal bool GetTrimStatus()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the mixture command
        /// </summary>
        /// <param name="engine">engine ID number</param>
        /// <returns>command in percent ( 0 - 100) for the given engine</returns>
        public double GetMixtureCmd(int engine) { return (double)MixtureCmd[engine]; }


        /// <summary>
        /// Gets the prop pitch command.
        /// </summary>
        /// <param name="engine">engine ID number</param>
        /// <returns>command in percent ( 0.0 - 1.0) for the given engine</returns>
        public double GetPropAdvanceCmd(int engine) { return (double)PropAdvanceCmd[engine]; }



        /// <summary>
        /// Gets the AUTOPilot aileron command.
        /// </summary>
        /// <returns>aileron command in radians</returns>
        public double GetAPDaCmd() { return AP_DaCmd; }


        /// <summary>
        /// Gets the AUTOPilot elevator command.
        /// </summary>
        /// <returns>elevator command in radians</returns>
        public double GetAPDeCmd() { return AP_DeCmd; }


        /// <summary>
        /// Gets the AUTOPilot rudder command.
        /// </summary>
        /// <returns>rudder command in radians</returns>
        public double GetAPDrCmd() { return AP_DrCmd; }


        /// <summary>
        /// Gets the AUTOPilot throttle (all engines) command.
        /// </summary>
        /// <returns>throttle command in percent</returns>
        public double GetAPThrottleCmd() { return AP_ThrottleCmd; }

        /// <summary>
        /// Gets the autopilot pitch attitude setpoint
        /// </summary>
        /// <returns>Pitch attitude setpoint in radians</returns>
        public double GetAPAttitudeSetPt() { return APAttitudeSetPt; }


        /// <summary>
        /// Gets the autopilot altitude setpoint
        /// </summary>
        /// <returns>Altitude setpoint in feet</returns>
        public double GetAPAltitudeSetPt() { return APAltitudeSetPt; }


        /// <summary>
        /// Gets the autopilot heading setpoint
        /// </summary>
        /// <returns>Heading setpoint in radians</returns>
        public double GetAPHeadingSetPt() { return APHeadingSetPt; }

        /// <summary>
        /// Gets the autopilot airspeed setpoint
        /// </summary>
        /// <returns>Airspeed setpoint in fps</returns>
        public double GetAPAirspeedSetPt() { return APAirspeedSetPt; }


        /// <summary>
        /// Sets the autopilot pitch attitude setpoint
        /// </summary>
        /// <param name="set"></param>
        public void SetAPAttitudeSetPt(double set) { APAttitudeSetPt = set; }

        /// <summary>
        /// Sets the autopilot altitude setpoint
        /// </summary>
        public void SetAPAltitudeSetPt(double set) { APAltitudeSetPt = set; }

        /// <summary>
        /// Sets the autopilot heading setpoint
        /// </summary>
        public void SetAPHeadingSetPt(double set) { APHeadingSetPt = set; }

        /// <summary>
        /// Sets the autopilot airspeed setpoint
        /// </summary>
        public void SetAPAirspeedSetPt(double set) { APAirspeedSetPt = set; }


        /// <summary>
        /// Turns on/off the attitude-seeking autopilot.
        /// </summary>
        /// <param name="set">true turns the mode on, false turns it off</param>
        public void SetAPAcquireAttitude(bool set) { APAcquireAttitude = set; }


        /// <summary>
        /// Turns on/off the altitude-seeking autopilot.
        /// </summary>
        /// <param name="set">true turns the mode on, false turns it off</param>
        public void SetAPAcquireAltitude(bool set) { APAcquireAltitude = set; }

        /// <summary>
        /// Turns on/off the heading-seeking autopilot.
        /// </summary>
        /// <param name="set">true turns the mode on, false turns it off</param>
        public void SetAPAcquireHeading(bool set) { APAcquireHeading = set; }


        /// <summary>
        /// Turns on/off the airspeed-seeking autopilot.
        /// </summary>
        /// <param name="set">set true turns the mode on, false turns it off  </param>
        public void SetAPAcquireAirspeed(bool set) { APAcquireAirspeed = set; }

        /// <summary>
        /// Turns on/off the attitude-holding autopilot.
        /// </summary>
        /// <param name="set">true turns the mode on, false turns it off</param>
        public void SetAPAttitudeHold(bool set) { APAttitudeHold = set; }

        /// <summary>
        /// Turns on/off the altitude-holding autopilot.
        /// </summary>
        /// <param name="set">true turns the mode on, false turns it off</param>
        public void SetAPAltitudeHold(bool set) { APAltitudeHold = set; }

        /// <summary>
        /// Turns on/off the heading-holding autopilot.
        /// </summary>
        /// <param name="set">true turns the mode on, false turns it off  </param>
        public void SetAPHeadingHold(bool set) { APHeadingHold = set; }

        /// <summary>
        /// Turns on/off the airspeed-holding autopilot.
        /// </summary>
        /// <param name="set">true turns the mode on, false turns it off</param>
        public void SetAPAirspeedHold(bool set) { APAirspeedHold = set; }

        /// <summary>
        /// Turns on/off the wing-leveler autopilot.
        /// </summary>
        /// <param name="set">true turns the mode on, false turns it off</param>
        public void SetAPWingsLevelHold(bool set) { APWingsLevelHold = set; }

        /// <summary>
        /// Retrieves the on/off mode of the autopilot AcquireAttitude mode
        /// </summary>
        /// <returns>true if on, false if off</returns>
        public bool GetAPAcquireAttitude() { return APAcquireAttitude; }

        /// <summary>
        /// Retrieves the on/off mode of the autopilot AcquireAltitude mode
        /// </summary>
        /// <returns>true if on, false if off</returns>
        public bool GetAPAcquireAltitude() { return APAcquireAltitude; }

        /// <summary>
        /// Retrieves the on/off mode of the autopilot AcquireHeading mode
        /// </summary>
        /// <returns>true if on, false if off</returns>
        public bool GetAPAcquireHeading() { return APAcquireHeading; }

        /// <summary>
        /// Retrieves the on/off mode of the autopilot AcquireAirspeed mode
        /// </summary>
        /// <returns>true if on, false if off </returns>
        public bool GetAPAcquireAirspeed() { return APAcquireAirspeed; }

        /// <summary>
        /// Retrieves the on/off mode of the autopilot AttitudeHold mode
        /// </summary>
        /// <returns>true if on, false if off</returns>
        public bool GetAPAttitudeHold() { return APAttitudeHold; }

        /// <summary>
        /// Retrieves the on/off mode of the autopilot AltitudeHold mode
        /// </summary>
        /// <returns>true if on, false if off</returns>
        public bool GetAPAltitudeHold() { return APAltitudeHold; }

        /// <summary>
        /// Retrieves the on/off mode of the autopilot HeadingHold mode
        /// </summary>
        /// <returns>true if on, false if off</returns>
        public bool GetAPHeadingHold() { return APHeadingHold; }

        /// <summary>
        /// Retrieves the on/off mode of the autopilot AirspeedHold mode
        /// </summary>
        /// <returns>true if on, false if off</returns>
        public bool GetAPAirspeedHold() { return APAirspeedHold; }

        /// <summary>
        /// Retrieves the on/off mode of the autopilot WingsLevelHold mode
        /// </summary>
        /// <returns>true if on, false if off</returns>
        public bool GetAPWingsLevelHold() { return APWingsLevelHold; }


        /// <summary>
        /// Aerosurface position retrieval
        /// Gets the right aileron position.
        /// </summary>
        /// <returns>Right aileron position in radians</returns>
        public double GetDaRPosRadians()
        { return DaRPos[0]; }

        /** Gets the elevator position.
            @return elevator position in radians */
        public double GetDePosRadians() { return DePos[0]; }

        /** Gets the rudder position.
            @return rudder position in radians */
        public double GetDrPosRadians() { return DrPos[0]; }

        /** Gets the speedbrake position.
            @return speedbrake position in radians */
        public double GetDsbPosRadians() { return DsbPos[0]; }

        /** Gets the spoiler position.
            @return spoiler position in radians */
        public double GetDspPosRadians() { return DspPos[0]; }

        /** Gets the flaps position.
            @return flaps position in radians */
        public double GetDfPosRadians() { return DfPos[0]; }

        /// @name Aerosurface position retrieval
        //@{
        /** Gets the left aileron position.
            @return aileron position in radians */
        public double GetDaLPos(OutputForm form)
        { return DaLPos[(int)form]; }

        /// @name Aerosurface position retrieval
        //@{
        /** Gets the right aileron position.
            @return aileron position in radians */
        public double GetDaRPos(OutputForm form) { return DaRPos[(int)form]; }

        /** Gets the elevator position.
            @return elevator position in radians */
        public double GetDePos(OutputForm form) { return DePos[(int)form]; }

        /** Gets the rudder position.
            @return rudder position in radians */
        public double GetDrPos(OutputForm form) { return DrPos[(int)form]; }

        /** Gets the speedbrake position.
            @return speedbrake position in radians */
        public double GetDsbPos(OutputForm form) { return DsbPos[(int)form]; }

        /** Gets the spoiler position.
            @return spoiler position in radians */
        public double GetDspPos(OutputForm form) { return DspPos[(int)form]; }

        /** Gets the flaps position.
            @return flaps position in radians */
        public double GetDfPos(OutputForm form) { return DfPos[(int)form]; }


        /// <summary>
        /// Gets the throttle position.
        /// </summary>
        /// <param name="engine">engine ID number</param>
        /// <returns>throttle position for the given engine in percent ( 0 - 100)</returns>
        public double GetThrottlePos(int engineNum)
        {
            if (engineNum < (int)ThrottlePos.Count)
            {
                if (engineNum < 0)
                {
                    if (log.IsErrorEnabled)
                        log.Error("Cannot get throttle value for ALL engines");
                }
                else
                {
                    return (double)ThrottlePos[engineNum];
                }
            }
            else
            {
                if (log.IsErrorEnabled)
                    log.Error("Throttle " + engineNum + " does not exist! " + ThrottlePos.Count
                        + " engines exist, but attempted throttle position setting is for engine "
                        + engineNum);
            }
            return 0.0;
        }


        /** Gets the mixture position.
            @param engine engine ID number
            @return mixture position for the given engine in percent ( 0 - 100)*/
        public double GetMixturePos(int engine) { return (double)MixturePos[engine]; }

        /** Gets the steering position.
            @return steering position in degrees */
        public double GetSteerPosDeg(int gear) { return (double)SteerPosDeg[gear]; }

        /** Gets the prop pitch position.
            @param engine engine ID number
            @return prop pitch position for the given engine in percent ( 0.0-1.0)*/
        public double GetPropAdvance(int engine) { return (double)PropAdvance[engine]; }

        /** Gets the prop feather position.
            @param engine engine ID number
            @return prop fether for the given engine (on / off)*/
        public bool GetPropFeather(int engine) { return PropFeather[engine]; }

        /// <summary>
        /// Retrieves the state object pointer.
        /// This is used by the FGFCS-owned components.
        /// </summary>
        /// <returns>the State object</returns>
        public State GetState() { return FDMExec.State; }

        /// <summary>
        /// Retrieves all component names for inclusion in output stream
        /// </summary>
        /// <param name="delimeter">either a tab or comma string depending on output type</param>
        /// <returns>a string containing the descriptive names for all components</returns>
        public string GetComponentStrings(string delimeter)
        {
            int comp;
            string CompStrings = "";
            bool firstime = true;

            for (comp = 0; comp < FCSComponents.Count; comp++)
            {
                if (firstime) firstime = false;
                else CompStrings += delimeter;

                CompStrings += FCSComponents[comp].GetName();
            }

            for (comp = 0; comp < APComponents.Count; comp++)
            {
                CompStrings += delimeter;
                CompStrings += APComponents[comp].GetName();
            }

            return CompStrings;
        }

        /// <summary>
        /// Retrieves all component outputs for inclusion in output stream
        /// </summary>
        /// <param name="delimeter">either a tab or comma string depending on output type</param>
        /// <returns>a string containing the numeric values for the current set of
        /// component outputs</returns>
        public string GetComponentValues(string format, IFormatProvider provider, string delimeter)
        {
            StringBuilder CompValues = new StringBuilder();
            bool firstime = true;

            foreach (FCSComponent comp in FCSComponents)
            {
                if (firstime)
                    firstime = false;
                else
                    CompValues.Append(delimeter);
                CompValues.Append(comp.GetOutput().ToString(format, provider));
            }

            foreach (FCSComponent comp in APComponents)
            {
                CompValues.Append(comp.GetOutput().ToString(format, provider));
            }

            return CompValues.ToString();
        }


        /// <summary>
        /// Sets/Gets the aileron command in percent
        /// </summary>
        /// <param name="cmd">cmd </param>
        [ScriptAttribute("fcs/aileron-cmd-norm", "TODO comments")]
        public double AileronCmd
        {
            get { return DaCmd; }
            set { DaCmd = value; }
        }


        /// <summary>
        /// Sets/Gets the elevator command in percent
        /// </summary>
        /// <param name="cmd"></param>
        [ScriptAttribute("fcs/elevator-cmd-norm", "TODO comments")]
        public double ElevatorCmd
        {
            get { return DeCmd; }
            set { DeCmd = value; }
        }

        /// <summary>
        /// Sets/Gets the rudder command in percent
        /// </summary>
        /// <param name="cmd">rudder command in percent</param>
        [ScriptAttribute("fcs/rudder-cmd-norm", "TODO comments")]
        public double RudderCmd
        {
            get { return DrCmd; }
            set { DrCmd = value; }
        }


        /// <summary>
        /// Sets/Gets the steering command in percent
        /// </summary>
        /// <param name="cmd">steering command in percent</param>
        [ScriptAttribute("fcs/steer-cmd-norm", "TODO comments")]
        public double SteeringCmd
        {
            get { return DsCmd; }
            set { DsCmd = value; }
        }

        /// <summary>
        /// Sets/Gets the flaps command in percent
        /// </summary>
        /// <param name="cmd">flaps command in percent</param>
        [ScriptAttribute("fcs/flap-cmd-norm", "TODO comments")]
        public double FlapsCmd
        {
            get { return DfCmd; }
            set { DfCmd = value; }
        }

        /// <summary>
        /// Sets/Gets the speedbrake command in percent
        /// </summary>
        /// <param name="cmd">speedbrake command in percent</param>
        [ScriptAttribute("fcs/speedbrake-cmd-norm", "TODO comments")]
        public double SpeedbrakeCmd
        {
            get { return DsbCmd; }
            set { DsbCmd = value; }
        }

        /// <summary>
        /// Sets/Gets the spoilers command in percent
        /// </summary>
        /// <param name="cmd">spoilers command in percent</param>
        [ScriptAttribute("fcs/spoiler-cmd-norm", "TODO comments")]
        public double SpoilersCmd
        {
            get { return DspCmd; }
            set { DspCmd = value; }
        }

        /// <summary>
        /// Sets/Gets the pitch trim command in percent
        /// </summary>
        /// <param name="cmd">pitch trim command in percent</param>
        [ScriptAttribute("fcs/pitch-trim-cmd-norm", "TODO comments")]
        public double PitchTrimCmd
        {
            get { return PTrimCmd; }
            set { PTrimCmd = value; }
        }


        /// <summary>
        /// Sets/Gets the rudder trim command in percent
        /// </summary>
        /// <param name="cmd">rudder trim command in percent</param>
        [ScriptAttribute("fcs/yaw-trim-cmd-norm", "TODO comments")]
        public double YawTrimCmd
        {
            get { return YTrimCmd; }
            set { YTrimCmd = value; }
        }

        /// <summary>
        /// Sets/Gets the aileron trim command in percent
        /// </summary>
        /// <param name="cmd">aileron trim command in percent</param>
        [ScriptAttribute("fcs/roll-trim-cmd-norm", "TODO comments")]
        public double RollTrimCmd
        {
            get { return RTrimCmd; }
            set { RTrimCmd = value; }
        }

        /// <summary>
        /// Sets/Gets the gear extend/retract command, defaults to down
        /// </summary>
        /// <param name="cmd">command 0 for up, 1 for down</param>
        [ScriptAttribute("gear/gear-cmd-norm", "TODO comments")]
        public double GearCmd
        {
            get { return gearCmd; }
            set { gearCmd = value; }
        }

        /// <summary>
        /// Sets/Gets the left aileron position in radians.
        /// </summary>
        [ScriptAttribute("fcs/left-aileron-pos-rad", "TODO comments")]
        public double LeftAileronPositionRadians
        {
            get { return DaLPos[(int)OutputForm.Rad]; }
            set { SetDaLPos(OutputForm.Rad, value); }
        }

        /// <summary>
        /// Sets/Gets the left aileron position in deg.
        /// </summary>
        [ScriptAttribute("fcs/left-aileron-pos-deg", "TODO comments")]
        public double LeftAileronPositionDeg
        {
            get { return DaLPos[(int)OutputForm.Deg]; }
            set { SetDaLPos(OutputForm.Deg, value); }
        }


        /// <summary>
        /// Sets/Gets the left aileron position Norm form.
        /// </summary>
        [ScriptAttribute("fcs/left-aileron-pos-norm", "TODO comments")]
        public double LeftAileronPositionNorm
        {
            get { return DaLPos[(int)OutputForm.Norm]; }
            set { SetDaLPos(OutputForm.Norm, value); }
        }


        /// <summary>
        /// Sets/Gets the left aileron position Norm form.
        /// </summary>
        [ScriptAttribute("fcs/mag-left-aileron-pos-rad", "TODO comments")]
        public double LeftAileronPositionMag
        {
            get { return DaLPos[(int)OutputForm.Mag]; }
            set { SetDaLPos(OutputForm.Mag, value); }
        }

        /// <summary>
        /// Sets/Gets the right aileron position in radians.
        /// </summary>
        [ScriptAttribute("fcs/right-aileron-pos-rad", "TODO comments")]
        public double RightAileronPositionRadians
        {
            get { return DaRPos[(int)OutputForm.Rad]; }
            set { SetDaRPos(OutputForm.Rad, value); }
        }

        /// <summary>
        /// Sets/Gets the right aileron position Deg form.
        /// </summary>
        [ScriptAttribute("fcs/right-aileron-pos-deg", "TODO comments")]
        public double RightAileronPositionDeg
        {
            get { return DaRPos[(int)OutputForm.Deg]; }
            set { SetDaRPos(OutputForm.Deg, value); }
        }

        /// <summary>
        /// Sets/Gets the right aileron position Norm form.
        /// </summary>
        [ScriptAttribute("fcs/right-aileron-pos-norm", "TODO comments")]
        public double RightAileronPositionNorm
        {
            get { return DaRPos[(int)OutputForm.Norm]; }
            set { SetDaRPos(OutputForm.Norm, value); }
        }

        /// <summary>
        /// Sets/Gets the right aileron position Norm form.
        /// </summary>
        [ScriptAttribute("fcs/mag-right-aileron-pos-rad", "TODO comments")]
        public double RightAileronPositionMag
        {
            get { return DaRPos[(int)OutputForm.Mag]; }
            set { SetDaRPos(OutputForm.Mag, value); }
        }


        /// <summary>
        /// Sets/Gets the Elevator position in radians.
        /// </summary>
        [ScriptAttribute("fcs/elevator-pos-rad", "TODO comments")]
        public double ElevatorPositionRadians
        {
            get { return DePos[(int)OutputForm.Rad]; }
            set { SetDePos(OutputForm.Rad,  value); }
        }

        /// <summary>
        /// Sets/Gets the Elevator position in Deg.
        /// </summary>
        [ScriptAttribute("fcs/elevator-pos-deg", "TODO comments")]
        public double ElevatorPositionDeg
        {
            get { return DePos[(int)OutputForm.Deg]; }
            set { SetDePos(OutputForm.Deg, value); }
        }

        /// <summary>
        /// Sets/Gets the Elevator position Norm form.
        /// </summary>
        [ScriptAttribute("fcs/elevator-pos-norm", "TODO comments")]
        public double ElevatorPositionNorm
        {
            get { return DePos[(int)OutputForm.Norm]; }
            set { SetDePos(OutputForm.Norm, value); }
        }


        /// <summary>
        /// Sets/Gets the elevator position Norm form.
        /// </summary>
        [ScriptAttribute("fcs/mag-elevator-pos-rad", "TODO comments")]
        public double ElevatorPositionMag
        {
            get { return DePos[(int)OutputForm.Mag]; }
            set { SetDePos(OutputForm.Mag, value); }
        }


        /// <summary>
        /// Sets/Gets the Rudder position in radians.
        /// </summary>
        [ScriptAttribute("fcs/rudder-pos-rad", "TODO comments")]
        public double RudderPositionRadians
        {
            get { return DrPos[(int)OutputForm.Rad]; }
            set { SetDrPos(OutputForm.Rad, value); }
        }

        /// <summary>
        /// Sets/Gets the Rudder position in deg
        /// </summary>
        [ScriptAttribute("fcs/rudder-pos-deg", "TODO comments")]
        public double RudderPositionDeg
        {
            get { return DrPos[(int)OutputForm.Deg]; }
            set { SetDrPos(OutputForm.Deg, value); }
        }

        /// <summary>
        /// Sets/Gets the Rudder position Norm form.
        /// </summary>
        [ScriptAttribute("fcs/rudder-pos-norm", "TODO comments")]
        public double RudderPositionNorm
        {
            get { return DrPos[(int)OutputForm.Norm]; }
            set { SetDrPos(OutputForm.Norm, value); }
        }


        /// <summary>
        /// Sets/Gets the Rudder position Mag form.
        /// </summary>
        [ScriptAttribute("fcs/mag-rudder-pos-rad", "TODO comments")]
        public double RudderPositionMag
        {
            get { return DrPos[(int)OutputForm.Mag]; }
            set { SetDrPos(OutputForm.Mag, value); }
        }


        /// <summary>
        /// Sets/Gets the flaps position in radians.
        /// </summary>
        [ScriptAttribute("fcs/flap-pos-rad", "TODO comments")]
        public double FlapsPositionRadians
        {
            get { return DfPos[(int)OutputForm.Rad]; }
            set { SetDfPos(OutputForm.Rad, value); }
        }

        /// <summary>
        /// Sets/Gets the flaps position in radians.
        /// </summary>
        [ScriptAttribute("fcs/flap-pos-deg", "TODO comments")]
        public double FlapsPositionDeg
        {
            get { return DfPos[(int)OutputForm.Deg]; }
            set { SetDfPos(OutputForm.Deg, value); }
        }

        /// <summary>
        /// Sets/Gets the flaps position Norm form.
        /// </summary>
        [ScriptAttribute("fcs/flap-pos-norm", "TODO comments")]
        public double FlapsPositionNorm
        {
            get { return DfPos[(int)OutputForm.Norm]; }
            set { SetDfPos(OutputForm.Norm, value); }
        }


        /// <summary>
        /// Sets/Gets the Speedbrake position in radians.
        /// </summary>
        [ScriptAttribute("fcs/speedbrake-pos-rad", "TODO comments")]
        public double SpeedbrakePositionRadians
        {
            get { return DsbPos[(int)OutputForm.Rad]; }
            set { SetDsbPos(OutputForm.Rad, value); }
        }

        /// <summary>
        /// Sets/Gets the Speedbrake position in deg.
        /// </summary>
        [ScriptAttribute("fcs/speedbrake-pos-deg", "TODO comments")]
        public double SpeedbrakePositionDeg
        {
            get { return DsbPos[(int)OutputForm.Deg]; }
            set { SetDsbPos(OutputForm.Deg, value); }
        }
        /// <summary>
        /// Sets/Gets the Speedbrake position Norm form.
        /// </summary>
        [ScriptAttribute("fcs/speedbrake-pos-norm", "TODO comments")]
        public double SpeedbrakePositionNorm
        {
            get { return DsbPos[(int)OutputForm.Norm]; }
            set { SetDsbPos(OutputForm.Norm, value); }
        }


        /// <summary>
        /// Sets/Gets the Speedbrake position Norm form.
        /// </summary>
        [ScriptAttribute("fcs/mag-speedbrake-pos-rad", "TODO comments")]
        public double SpeedbrakePositionMag
        {
            get { return DsbPos[(int)OutputForm.Mag]; }
            set { SetDsbPos(OutputForm.Mag, value); }
        }


        /// <summary>
        /// Sets/Gets the Spoiler position in radians.
        /// </summary>
        [ScriptAttribute("fcs/spoiler-pos-rad", "TODO comments")]
        public double SpoilerPositionRadians
        {
            get { return DspPos[(int)OutputForm.Rad]; }
            set { SetDspPos(OutputForm.Rad, value); }
        }

        /// <summary>
        /// Sets/Gets the Spoiler position in Deg.
        /// </summary>
        [ScriptAttribute("fcs/spoiler-pos-deg", "TODO comments")]
        public double SpoilerPositionDeg
        {
            get { return DspPos[(int)OutputForm.Deg]; }
            set { SetDspPos(OutputForm.Deg, value); }
        }

        /// <summary>
        /// Sets/Gets the Spoiler position Norm form.
        /// </summary>
        [ScriptAttribute("fcs/spoiler-pos-norm", "TODO comments")]
        public double SpoilerPositionNorm
        {
            get { return DspPos[(int)OutputForm.Norm]; }
            set { SetDspPos(OutputForm.Norm, value); }
        }


        /// <summary>
        /// Sets/Gets the Spoiler position Norm form.
        /// </summary>
        [ScriptAttribute("fcs/mag-spoiler-pos-rad", "TODO comments")]
        public double SpoilerPositionMag
        {
            get { return DspPos[(int)OutputForm.Mag]; }
            set { SetDspPos(OutputForm.Mag, value); }
        }

        /// <summary>
        /// Sets/Gets the gear extend/retract position, (0 up, 1 down), defaults to down.
        /// </summary>
        [ScriptAttribute("gear/gear-pos-norm", "TODO comments")]
        public double GearPosition
        {
            get { return gearPos; }
            set { gearPos = value; }
        }


        /// <summary>
        /// Sets the throttle command for the specified engine
        /// </summary>
        /// <param name="engineNum">engine engine ID number</param>
        /// <param name="cmd">throttle command in percent (0 - 100)</param>
        public void SetThrottleCmd(int engineNum, double setting)
        {
            int ctr;

            if (engineNum < (int)ThrottlePos.Count)
            {
                if (engineNum < 0)
                {
                    for (ctr = 0; ctr < ThrottleCmd.Count; ctr++) ThrottleCmd[ctr] = setting;
                }
                else
                {
                    ThrottleCmd[engineNum] = setting;
                }
            }
            else
            {
                if (log.IsErrorEnabled) log.Error("Throttle " + engineNum
                    + " does not exist! " + ThrottleCmd.Count
                    + " engines exist, but attempted throttle command is for engine "
                    + engineNum);
            }
        }


        /// <summary>
        /// Sets the mixture command for the specified engine
        /// </summary>
        /// <param name="engineNum">engine ID number</param>
        /// <param name="setting">mixture command in percent (0 - 100)</param>
        void SetMixtureCmd(int engineNum, double setting)
        {
            int ctr;

            if (engineNum < (int)ThrottlePos.Count)
            {
                if (engineNum < 0)
                {
                    for (ctr = 0; ctr < MixtureCmd.Count; ctr++) MixtureCmd[ctr] = setting;
                }
                else
                {
                    MixtureCmd[engineNum] = setting;
                }
            }
        }


        /// <summary>
        /// Sets the propeller pitch command for the specified engine
        /// </summary>
        /// <param name="engineNum">engine ID number</param>
        /// <param name="setting">mixture command in percent (0.0 - 1.0)</param>
        void SetPropAdvanceCmd(int engineNum, double setting)
        {
            int ctr;

            if (engineNum < (int)ThrottlePos.Count)
            {
                if (engineNum < 0)
                {
                    for (ctr = 0; ctr < PropAdvanceCmd.Count; ctr++) PropAdvanceCmd[ctr] = setting;
                }
                else
                {
                    PropAdvanceCmd[engineNum] = setting;
                }
            }
        }
        //@}

        /// @name AUTOPilot . FCS effector command setting
        //@{
        /** Sets the AUTOPilot aileron command
            @param cmd AUTOPilot aileron command in radians*/
        public void SetAPDaCmd(double cmd) { AP_DaCmd = cmd; }

        /** Sets the AUTOPilot elevator command
            @param cmd AUTOPilot elevator command in radians*/
        public void SetAPDeCmd(double cmd) { AP_DeCmd = cmd; }

        /** Sets the AUTOPilot rudder command
            @param cmd AUTOPilot rudder command in radians*/
        public void SetAPDrCmd(double cmd) { AP_DrCmd = cmd; }

        /** Sets the AUTOPilot throttle command
            @param cmd AUTOPilot throttle command in percent*/
        public void SetAPThrottleCmd(double cmd) { AP_ThrottleCmd = cmd; }
        //@}

        /// @name Aerosurface position setting
        //@{
        /** Sets the left aileron position
            @param cmd left aileron position in radians*/
        public void SetDaLPos(OutputForm form, double pos)
        {
            switch (form)
            {
                case OutputForm.Rad:
                    DaLPos[(int)OutputForm.Rad] = pos;
                    DaLPos[(int)OutputForm.Deg] = pos * Constants.radtodeg;
                    break;
                case OutputForm.Deg:
                    DaLPos[(int)OutputForm.Rad] = pos * Constants.degtorad;
                    DaLPos[(int)OutputForm.Deg] = pos;
                    break;
                case OutputForm.Norm:
                    DaLPos[(int)OutputForm.Norm] = pos;
                    break;
            }
            DaLPos[(int)OutputForm.Mag] = Math.Abs(DaLPos[(int)OutputForm.Rad]);
        }

        /** Sets the right aileron position
            @param cmd right aileron position in radians*/
        public void SetDaRPos(OutputForm form, double pos)
        {
            switch (form)
            {
                case OutputForm.Rad:
                    DaRPos[(int)OutputForm.Rad] = pos;
                    DaRPos[(int)OutputForm.Deg] = pos * Constants.radtodeg;
                    break;
                case OutputForm.Deg:
                    DaRPos[(int)OutputForm.Rad] = pos * Constants.degtorad;
                    DaRPos[(int)OutputForm.Deg] = pos;
                    break;
                case OutputForm.Norm:
                    DaRPos[(int)OutputForm.Norm] = pos;
                    break;
            }
            DaRPos[(int)OutputForm.Mag] = Math.Abs(DaRPos[(int)OutputForm.Rad]);
        }

        /** Sets the elevator position
            @param cmd elevator position in radians*/
        public void SetDePos(OutputForm form, double pos)
        {
            switch (form)
            {
                case OutputForm.Rad:
                    DePos[(int)OutputForm.Rad] = pos;
                    DePos[(int)OutputForm.Deg] = pos * Constants.radtodeg;
                    break;
                case OutputForm.Deg:
                    DePos[(int)OutputForm.Rad] = pos * Constants.degtorad;
                    DePos[(int)OutputForm.Deg] = pos;
                    break;
                case OutputForm.Norm:
                    DePos[(int)OutputForm.Norm] = pos;
                    break;
            }
            DePos[(int)OutputForm.Mag] = Math.Abs(DePos[(int)OutputForm.Rad]);
        }

        /** Sets the rudder position
            @param cmd rudder position in radians*/
        public void SetDrPos(OutputForm form, double pos)
        {
            switch (form)
            {
                case OutputForm.Rad:
                    DrPos[(int)OutputForm.Rad] = pos;
                    DrPos[(int)OutputForm.Deg] = pos * Constants.radtodeg;
                    break;
                case OutputForm.Deg:
                    DrPos[(int)OutputForm.Rad] = pos * Constants.degtorad;
                    DrPos[(int)OutputForm.Deg] = pos;
                    break;
                case OutputForm.Norm:
                    DrPos[(int)OutputForm.Norm] = pos;
                    break;
            }
            DrPos[(int)OutputForm.Mag] = Math.Abs(DrPos[(int)OutputForm.Rad]);
        }

        /** Sets the flaps position
           @param cmd flaps position in radians*/
        public void SetDfPos(OutputForm form, double pos)
        {
            switch (form)
            {
                case OutputForm.Rad:
                    DfPos[(int)OutputForm.Rad] = pos;
                    DfPos[(int)OutputForm.Deg] = pos * Constants.radtodeg;
                    break;
                case OutputForm.Deg:
                    DfPos[(int)OutputForm.Rad] = pos * Constants.degtorad;
                    DfPos[(int)OutputForm.Deg] = pos;
                    break;
                case OutputForm.Norm:
                    DfPos[(int)OutputForm.Norm] = pos;
                    break;
            }
            DfPos[(int)OutputForm.Mag] = Math.Abs(DfPos[(int)OutputForm.Rad]);
        }

        /** Sets the speedbrake position
            @param cmd speedbrake position in radians*/
        public void SetDsbPos(OutputForm form, double pos)
        {
            switch (form)
            {
                case OutputForm.Rad:
                    DsbPos[(int)OutputForm.Rad] = pos;
                    DsbPos[(int)OutputForm.Deg] = pos * Constants.radtodeg;
                    break;
                case OutputForm.Deg:
                    DsbPos[(int)OutputForm.Rad] = pos * Constants.degtorad;
                    DsbPos[(int)OutputForm.Deg] = pos;
                    break;
                case OutputForm.Norm:
                    DsbPos[(int)OutputForm.Norm] = pos;
                    break;
            }
            DsbPos[(int)OutputForm.Mag] = Math.Abs(DsbPos[(int)OutputForm.Rad]);
        }

        /** Sets the spoiler position
            @param cmd spoiler position in radians*/
        public void SetDspPos(OutputForm form, double pos)
        {
            switch (form)
            {
                case OutputForm.Rad:
                    DspPos[(int)OutputForm.Rad] = pos;
                    DspPos[(int)OutputForm.Deg] = pos * Constants.radtodeg;
                    break;
                case OutputForm.Deg:
                    DspPos[(int)OutputForm.Rad] = pos * Constants.degtorad;
                    DspPos[(int)OutputForm.Deg] = pos;
                    break;
                case OutputForm.Norm:
                    DspPos[(int)OutputForm.Norm] = pos;
                    break;
            }
            DspPos[(int)OutputForm.Mag] = Math.Abs(DspPos[(int)OutputForm.Rad]);
        }

        /// <summary>
        /// Sets the actual throttle setting for the specified engine
        /// </summary>
        /// <param name="engine">engine ID number</param>
        /// <param name="cmd">throttle setting in percent (0 - 100)</param>
        public void SetThrottlePos(int engineNum, double setting)
        {
            int ctr;

            if (engineNum < (int)ThrottlePos.Count)
            {
                if (engineNum < 0)
                {
                    for (ctr = 0; ctr < ThrottlePos.Count; ctr++) ThrottlePos[ctr] = setting;
                }
                else
                {
                    ThrottlePos[engineNum] = setting;
                }
            }
            else
            {
                if (log.IsErrorEnabled)
                    log.Error("Throttle " + engineNum + " does not exist! " + ThrottlePos.Count
                        + " engines exist, but attempted throttle position setting is for engine "
                        + engineNum);
            }
        }


        /** Sets the actual mixture setting for the specified engine
            @param engine engine ID number
            @param cmd mixture setting in percent (0 - 100)*/
        public void SetMixturePos(int engineNum, double setting)
        {
            int ctr;

            if (engineNum < (int)ThrottlePos.Count)
            {
                if (engineNum < 0)
                {
                    for (ctr = 0; ctr <= MixtureCmd.Count; ctr++) MixturePos[ctr] = MixtureCmd[ctr];
                }
                else
                {
                    MixturePos[engineNum] = setting;
                }
            }
        }


        /** Sets the steering position
            @param cmd steering position in degrees*/
        public void SetSteerPosDeg(int gear, double pos) { SteerPosDeg[gear] = pos; }


        /// <summary>
        /// Sets the actual prop pitch setting for the specified engine
        /// </summary>
        /// <param name="engineNum">engine ID number</param>
        /// <param name="setting">prop pitch setting in percent (0.0 - 1.0)</param>
        public void SetPropAdvance(int engineNum, double setting)
        {
            int ctr;

            if (engineNum < (int)ThrottlePos.Count)
            {
                if (engineNum < 0)
                {
                    for (ctr = 0; ctr <= PropAdvanceCmd.Count; ctr++) PropAdvance[ctr] = PropAdvanceCmd[ctr];
                }
                else
                {
                    PropAdvance[engineNum] = setting;
                }
            }
        }

        public void SetFeatherCmd(int engineNum, bool setting)
        {
            if (engineNum < ThrottlePos.Count)
            {
                if (engineNum < 0)
                {
                    for (int ctr = 0; ctr < PropFeatherCmd.Count; ctr++) PropFeatherCmd[ctr] = setting;
                }
                else
                {
                    PropFeatherCmd[engineNum] = setting;
                }
            }
        }

        public void SetPropFeather(int engineNum, bool setting)
        {
            if (engineNum < ThrottlePos.Count)
            {
                if (engineNum < 0)
                {
                    for (int ctr = 0; ctr <= PropFeatherCmd.Count; ctr++) PropFeather[ctr] = PropFeatherCmd[ctr];
                }
                else
                {
                    PropFeather[engineNum] = setting;
                }
            }
        }

        /// @name Landing Gear brakes
        //@{
        /** Sets the left brake group
            @param cmd brake setting in percent (0.0 - 1.0) */
        public void SetLBrake(double cmd) { LeftBrake = cmd; }

        /** Sets the right brake group
            @param cmd brake setting in percent (0.0 - 1.0) */
        public void SetRBrake(double cmd) { RightBrake = cmd; }

        /** Sets the center brake group
            @param cmd brake setting in percent (0.0 - 1.0) */
        public void SetCBrake(double cmd) { CenterBrake = cmd; }


        /// <summary>
        /// Gets the brake for a specified group.
        /// </summary>
        /// <param name="bg">which brakegroup to retrieve the command for</param>
        /// <returns>the brake setting for the supplied brake group argument</returns>
        public double GetBrake(LGear.BrakeGroup bg)
        {
            switch (bg)
            {
                case LGear.BrakeGroup.Left:
                    return LeftBrake;
                case LGear.BrakeGroup.Right:
                    return RightBrake;
                case LGear.BrakeGroup.Center:
                    return CenterBrake;
                default:
                    if (log.IsErrorEnabled)
                        log.Error("GetBrake asked to return a bogus brake value");
                    break;
            }
            return 0.0;
        }


        public void AddThrottle()
        {
            ThrottleCmd.Add(0.0);
            ThrottlePos.Add(0.0);
            MixtureCmd.Add(0.0);     // assume throttle and mixture are coupled
            MixturePos.Add(0.0);
            PropAdvanceCmd.Add(0.0); // assume throttle and prop pitch are coupled
            PropAdvance.Add(0.0);

            PropFeatherCmd.Add(false);
            PropFeather.Add(false);

            BindThrottle(ThrottleCmd.Count - 1);

        }

        public void AddGear()
        {
            SteerPosDeg.Add(0.0);
        }

        public PropertyManager GetPropertyManager() { return FDMExec.PropertyManager; }

        public void convert()
        {
            //TODO
        }

        public void BindThrottle(int num)
        {
            PropertyManager propertyManager = FDMExec.PropertyManager;

            propertyManager.Tie("fcs/throttle-cmd-norm[" + num + "]", num, this.GetThrottleCmd, this.SetThrottleCmd);
            propertyManager.Tie("fcs/throttle-pos-norm[" + num + "]", num, this.GetThrottlePos, this.SetThrottlePos);

            propertyManager.Tie("fcs/mixture-cmd-norm[" + num + "]", num, this.GetMixtureCmd, this.SetMixtureCmd);
            propertyManager.Tie("fcs/mixture-pos-norm[" + num + "]", num, this.GetMixturePos, this.SetMixturePos);

            propertyManager.Tie("fcs/advance-cmd-norm[" + num + "]", num, this.GetPropAdvanceCmd, this.SetPropAdvanceCmd);
            propertyManager.Tie("fcs/advance-pos-norm[" + num + "]", num, this.GetPropAdvance, this.SetPropAdvance);


            //TODO TODO ??
            //propertyManager.Tie("fcs/feather-cmd-norm[" + num + "]", num, this.GetFeatherCmd, this.SetFeatherCmd);
            //propertyManager.Tie("fcs/feather-pos-norm[" + num + "]", num, this.GetPropFeather, this.SetPropFeather);
        }

        public void BindModel()
        {
            PropertyManager propertyManager = FDMExec.PropertyManager;

            for (int i = 0; i < SteerPosDeg.Count; i++)
            {
                if (FDMExec.GroundReactions.GetGearUnit(i).GetSteerable())
                {
                    propertyManager.Tie("fcs/steer-pos-deg[" + i + "]", i, this.GetSteerPosDeg, this.SetSteerPosDeg);
                }
            }
        }

        public void Unbind(PropertyManager node)
        {
            //TODO
        }

        public void Load(XmlElement element)
        {
            string name = element.GetAttribute("name");
            List<FCSComponent> components = new List<FCSComponent>();

            if (name == null || name.Length == 0)
            {
                string fname = element.GetAttribute("file");


                if (fname.Length == 0)
                {
                    if (log.IsErrorEnabled)
                        log.Error("FCS/Autopilot does not appear to be defined inline nor in a file");
                }
                else
                {
                    string file = FDMExec.AircraftPath + "/" + FDMExec.ModelName + "/" + fname + ".xml";
                    FileInfo fi1 = new FileInfo(file);
                    if (!fi1.Exists)
                    {
                        if (log.IsErrorEnabled)
                            log.Error("Could not open " + FDMExec.ModelName + " file: " + file);

                        throw new Exception("Could not open " + FDMExec.ModelName + " file: " + file); ;
                    }
                    else
                    {
                        // set local config file object pointer to FCS config
                        XmlReaderSettings settings = new XmlReaderSettings();

                        settings.ValidationType = ValidationType.Schema;

                        XmlReader xmlReader = XmlReader.Create(new XmlTextReader(file), settings);

                        XmlDocument doc = new XmlDocument();
                        // load the data into the dom
                        doc.Load(xmlReader);
                        element = doc.DocumentElement;

                    }
                }
            }

            string fcstype = element.LocalName;

            if (fcstype == "autopilot")
            {
                components = APComponents;
                Name = "Autopilot: " + element.GetAttribute("name");
            }
            else if (fcstype == "flight_control")
            {
                components = FCSComponents;
                Name = "FCS: " + element.GetAttribute("name");
            }
            else
            {
                if (log.IsErrorEnabled)
                    log.Error("Unknown FCS delimiter : " + fcstype);
            }

            // ToDo: How do these get untied?
            // ToDo: Consider having INPUT and OUTPUT interface properties. Would then
            //       have to duplicate this block of code after channel read code.
            //       Input properties could be write only (nah), and output could be read
            //       only.

            if (fcstype.Equals("flight_control"))
                BindModel();

            if (log.IsDebugEnabled)
                log.Debug("Control System Name: " + Name);


            foreach (XmlNode currentNode in element.ChildNodes)
            {
                if (currentNode.NodeType == XmlNodeType.Element)
                {
                    XmlElement currentElement = (XmlElement)currentNode;

                    if (currentElement.LocalName.Equals("property"))
                    {
                        DoubleWrapper propNode = new DoubleWrapper();
                        interface_properties.Add(propNode);
                        string interface_property_string = currentElement.InnerText;

                        // ToDo: How do these get untied?
                        // ToDo: Consider having INPUT and OUTPUT interface properties. Would then
                        //       have to duplicate this block of code after channel read code.
                        //       Input properties could be write only (nah), and output could be read
                        //       only.
                        FDMExec.PropertyManager.Tie(interface_property_string, propNode.GetDoubleValue, propNode.SetDoubleValue);
                    }
                    else if (currentElement.LocalName.Equals("sensor"))
                    {
                        try
                        {
                            sensors.Add(new Sensor(this, currentElement));
                        }
                        catch (Exception e)
                        {
                            if (log.IsErrorEnabled)
                                log.Error("Error reading Sensor information:" + e);
                            throw new Exception("Error reading Sensor information.");
                        }
                    }
                    else if (currentElement.LocalName.Equals("channel"))
                    {
                        LoadChannel(currentElement, ref components);
                    }
                    else
                    {
                        if (log.IsErrorEnabled)
                            log.Error("Error reading FCS information. Unkown tag:" + currentElement.LocalName);
                        throw new Exception("Error reading FCS information. Unkown tag:" + currentElement.LocalName);
                    }
                }
            }

            /*
            string nodeName;
            for (int i = 0; i < components.Count; i++)
            {

                if ((components[i].GetComponentType() == "AEROSURFACE_SCALE")
                    || (components[i].GetComponentType() == "KINEMAT")
                    && (components[i].GetOutputNode() != null))
                {
                    nodeName = components[i].GetOutputNode().ShortName;
                    if (nodeName.Equals("elevator-pos-rad"))
                    {
                        ToNormalize[(int)FcIdx.De] = i;
                    }
                    else if (nodeName.Equals("left-aileron-pos-rad")
                        || nodeName.Equals("aileron-pos-rad"))
                    {
                        ToNormalize[(int)FcIdx.DaL] = i;
                    }
                    else if (nodeName.Equals("right-aileron-pos-rad"))
                    {
                        ToNormalize[(int)FcIdx.DaR] = i;
                    }
                    else if (nodeName.Equals("rudder-pos-rad"))
                    {
                        ToNormalize[(int)FcIdx.Dr] = i;
                    }
                    else if (nodeName.Equals("speedbrake-pos-rad"))
                    {
                        ToNormalize[(int)FcIdx.Dsb] = i;
                    }
                    else if (nodeName.Equals("spoiler-pos-rad"))
                    {
                        ToNormalize[(int)FcIdx.Dsp] = i;
                    }
                    else if (nodeName.Equals("flap-pos-deg"))
                    {
                        ToNormalize[(int)FcIdx.Df] = i;
                    }
                }
            }
            */
        }

        public void LoadChannel(XmlElement element, ref List<FCSComponent> Comp)
        {
            foreach (XmlNode currentNode in element.ChildNodes)
            {
                if (currentNode.NodeType == XmlNodeType.Element)
                {
                    XmlElement currentElement = (XmlElement)currentNode;

                    string comp_name = currentElement.GetAttribute("type");


                    if (log.IsDebugEnabled)
                    {
                        if (comp_name.Length != 0)

                            log.Debug("    Loading Component :" + comp_name);
                        else
                            log.Debug("    Loading Component :" + currentElement.LocalName);
                    }

                    if ((comp_name.Equals("LAG_FILTER")) ||
                        (comp_name.Equals("LEAD_LAG_FILTER")) ||
                        (comp_name.Equals("SECOND_ORDER_FILTER")) ||
                        (comp_name.Equals("WASHOUT_FILTER")) ||
                        (comp_name.Equals("INTEGRATOR")) ||
                        (currentElement.LocalName.Equals("lag_filter")) ||
                    (currentElement.LocalName.Equals("lead_lag_filter")) ||
                    (currentElement.LocalName.Equals("washout_filter")) ||
                    (currentElement.LocalName.Equals("second_order_filter")) ||
                    (currentElement.LocalName.Equals("integrator")))
                    {
                        Comp.Add(new FlightControl.Filter(this, currentElement));

                    }
                    else if ((comp_name.Equals("PURE_GAIN")) ||
                           (comp_name.Equals("SCHEDULED_GAIN")) ||
                           (comp_name.Equals("AEROSURFACE_SCALE")) ||
                           (currentElement.LocalName.Equals("pure_gain")) ||
                           (currentElement.LocalName.Equals("scheduled_gain")) ||
                           (currentElement.LocalName.Equals("aerosurface_scale")))
                    {
                        Comp.Add(new Gain(this, currentElement));
                    }
                    else if ((comp_name.Equals("SUMMER")) ||
                          (currentElement.LocalName.Equals("summer")))
                    {
                        Comp.Add(new Summer(this, currentElement));
                    }
                    else if ((comp_name.Equals("DEADBAND")) || (currentElement.LocalName.Equals("deadband")))
                    {
                        Comp.Add(new DeadBand(this, currentElement));
                    }
                    else if (comp_name.Equals("GRADIENT"))
                    {
                        Comp.Add(new Gradient(this, currentElement));
                    }
                    else if ((comp_name.Equals("SWITCH")) || (currentElement.LocalName.Equals("switch")))
                    {
                        Comp.Add(new Switch(this, currentElement));
                    }
                    else if ((comp_name.Equals("KINEMAT")) || (currentElement.LocalName.Equals("kinematic")))
                    {
                        Comp.Add(new Kinemat(this, currentElement));
                    }
                    else if ((comp_name.Equals("FUNCTION")) || (currentElement.LocalName.Equals("fcs_function")))
                    {
                        Comp.Add(new FCSFunction(this, currentElement));
                    }
                    else
                    {
                        if (log.IsErrorEnabled)
                            log.Error("Unknown FCS component: " + comp_name);
                        throw new Exception("Unknown FCS component: " + comp_name);
                    }
                }
            }
        }


        private bool DoNormalize;
        private void Normalize()
        {
            //not all of these are guaranteed to be defined for every model
            //those that are have an index >=0 in the ToNormalize array
            //ToNormalize is filled in Load()

            if (ToNormalize[(int)FcIdx.De] > -1)
            {
                DePos[(int)OutputForm.Norm] = FCSComponents[ToNormalize[(int)FcIdx.De]].GetOutputPct();
            }

            if (ToNormalize[(int)FcIdx.DaL] > -1)
            {
                DaLPos[(int)OutputForm.Norm] = FCSComponents[ToNormalize[(int)FcIdx.DaL]].GetOutputPct();
            }

            if (ToNormalize[(int)FcIdx.DaR] > -1)
            {
                DaRPos[(int)OutputForm.Norm] = FCSComponents[ToNormalize[(int)FcIdx.DaR]].GetOutputPct();
            }

            if (ToNormalize[(int)FcIdx.Dr] > -1)
            {
                DrPos[(int)OutputForm.Norm] = FCSComponents[ToNormalize[(int)FcIdx.Dr]].GetOutputPct();
            }

            if (ToNormalize[(int)FcIdx.Dsb] > -1)
            {
                DsbPos[(int)OutputForm.Norm] = FCSComponents[ToNormalize[(int)FcIdx.Dsb]].GetOutputPct();
            }

            if (ToNormalize[(int)FcIdx.Dsp] > -1)
            {
                DspPos[(int)OutputForm.Norm] = FCSComponents[ToNormalize[(int)FcIdx.Dsp]].GetOutputPct();
            }

            if (ToNormalize[(int)FcIdx.Df] > -1)
            {
                DfPos[(int)OutputForm.Norm] = FCSComponents[ToNormalize[(int)FcIdx.Df]].GetOutputPct();
            }

            DePos[(int)OutputForm.Mag] = Math.Abs(DePos[(int)OutputForm.Rad]);
            DaLPos[(int)OutputForm.Mag] = Math.Abs(DaLPos[(int)OutputForm.Rad]);
            DaRPos[(int)OutputForm.Mag] = Math.Abs(DaRPos[(int)OutputForm.Rad]);
            DrPos[(int)OutputForm.Mag] = Math.Abs(DrPos[(int)OutputForm.Rad]);
            DsbPos[(int)OutputForm.Mag] = Math.Abs(DsbPos[(int)OutputForm.Rad]);
            DspPos[(int)OutputForm.Mag] = Math.Abs(DspPos[(int)OutputForm.Rad]);
            DfPos[(int)OutputForm.Mag] = Math.Abs(DfPos[(int)OutputForm.Rad]);

        }

        internal double GetChannelDeltaT()
        {
            throw new NotImplementedException();
        }

        private const int NForms = (int)OutputForm.NForms;
        private const int NNorm = (int)FcIdx.NNorm;

        private double DaCmd, DeCmd, DrCmd, DsCmd, DfCmd, DsbCmd, DspCmd;
        private double AP_DaCmd, AP_DeCmd, AP_DrCmd, AP_ThrottleCmd;
        private double[] DePos = new double[NForms];
        private double[] DaLPos = new double[NForms];
        private double[] DaRPos = new double[NForms];
        private double[] DrPos = new double[NForms];
        private double[] DfPos = new double[NForms];
        private double[] DsbPos = new double[NForms];
        private double[] DspPos = new double[NForms];
        private double PTrimCmd, YTrimCmd, RTrimCmd;
        private List<double> ThrottleCmd = new List<double>(); //vector <double>
        private List<double> ThrottlePos = new List<double>();
        private List<double> MixtureCmd = new List<double>();
        private List<double> MixturePos = new List<double>();
        private List<double> PropAdvanceCmd = new List<double>();
        private List<double> PropAdvance = new List<double>();
        private List<bool> PropFeatherCmd = new List<bool>();
        private List<bool> PropFeather = new List<bool>();
        private List<double> SteerPosDeg = new List<double>();
        private double LeftBrake, RightBrake, CenterBrake; // Brake settings
        private double gearCmd, gearPos;

        private List<FCSComponent> FCSComponents = new List<FCSComponent>();
        private List<FCSComponent> APComponents = new List<FCSComponent>();
        private List<DoubleWrapper> interface_properties = new List<DoubleWrapper>();
        private List<FCSComponent> sensors = new List<FCSComponent>();

        private double APAttitudeSetPt, APAltitudeSetPt, APHeadingSetPt, APAirspeedSetPt;
        private bool APAcquireAttitude, APAcquireAltitude, APAcquireHeading, APAcquireAirspeed;
        private bool APAttitudeHold, APAltitudeHold, APHeadingHold, APAirspeedHold, APWingsLevelHold;

        private int[] ToNormalize = new int[NNorm];

        private const string IdSrc = "$Id: FGFCS.cpp,v 1.102 2005/01/20 07:27:35 jberndt Exp $";

        private const string NAME = "NAME";
        private const string TYPE = "TYPE";
        private const string FILE = "FILE";
        private const string COMPONENT = "COMPONENT";
        private const string AUTOPILOT = "AUTOPILOT";
        private const string FLIGHT_CONTROL = "FLIGHT_CONTROL";


    }
}
