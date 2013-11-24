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


    // Import log4net classes.
    using log4net;
    using CommonUtils.MathLib;

    //TODO review this name StateType. The name was orig. State
    public enum StateType { All, Udot, Vdot, Wdot, Qdot, Pdot, Rdot, Hmgt, Nlf };
    public enum ControlType
    {
        Throttle, Beta, Alpha, Elevator, Aileron, Rudder, AltAGL,
        Theta, Phi, Gamma, PitchTrim, RollTrim, YawTrim, Heading
    };

    /// <summary>
    /// Models an aircraft axis for purposes of trimming.
    /// </summary>
    public class TrimAxis
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

        public TrimAxis(FDMExecutive exec,
            InitialCondition ic,
            StateType st,
            ControlType ctrl)
        {

            fdmex = exec;
            fgic = ic;
            state = st;
            control = ctrl;
            max_iterations = 10;
            control_value = 0;
            its_to_stable_value = 0;
            total_iterations = 0;
            total_stability_iterations = 0;
            state_convert = 1.0;
            control_convert = 1.0;
            state_value = 0;
            state_target = 0;
            switch (state)
            {
                case StateType.Udot: tolerance = DEFAULT_TOLERANCE; break;
                case StateType.Vdot: tolerance = DEFAULT_TOLERANCE; break;
                case StateType.Wdot: tolerance = DEFAULT_TOLERANCE; break;
                case StateType.Qdot: tolerance = DEFAULT_TOLERANCE / 10; break;
                case StateType.Pdot: tolerance = DEFAULT_TOLERANCE / 10; break;
                case StateType.Rdot: tolerance = DEFAULT_TOLERANCE / 10; break;
                case StateType.Hmgt: tolerance = 0.01; break;
                case StateType.Nlf: state_target = 1.0; tolerance = 1E-5; break;
                case StateType.All: break;
            }

            solver_eps = tolerance;
            switch (control)
            {
                case ControlType.Throttle:
                    control_min = 0;
                    control_max = 1;
                    control_value = 0.5;
                    break;
                case ControlType.Beta:
                    control_min = -30 * Constants.degtorad;
                    control_max = 30 * Constants.degtorad;
                    control_convert = Constants.radtodeg;
                    break;
                case ControlType.Alpha:
                    control_min = fdmex.Aerodynamics.AlphaCLMin;
                    control_max = fdmex.Aerodynamics.AlphaCLMax;
                    if (control_max <= control_min)
                    {
                        control_max = 20 * Constants.degtorad;
                        control_min = -5 * Constants.degtorad;
                    }
                    control_value = (control_min + control_max) / 2;
                    control_convert = Constants.radtodeg;
                    solver_eps = tolerance / 100;
                    break;
                case ControlType.PitchTrim:
                case ControlType.Elevator:
                case ControlType.RollTrim:
                case ControlType.Aileron:
                case ControlType.YawTrim:
                case ControlType.Rudder:
                    control_min = -1;
                    control_max = 1;
                    state_convert = Constants.radtodeg;
                    solver_eps = tolerance / 100;
                    break;
                case ControlType.AltAGL:
                    control_min = 0;
                    control_max = 30;
                    control_value = fdmex.Propagate.DistanceAGL;
                    solver_eps = tolerance / 100;
                    break;
                case ControlType.Theta:
                    control_min = fdmex.Propagate.GetEuler().Theta - 5 * Constants.degtorad;
                    control_max = fdmex.Propagate.GetEuler().Theta + 5 * Constants.degtorad;
                    state_convert = Constants.radtodeg;
                    break;
                case ControlType.Phi:
                    control_min = fdmex.Propagate.GetEuler().Phi - 30 * Constants.degtorad;
                    control_max = fdmex.Propagate.GetEuler().Phi + 30 * Constants.degtorad;
                    state_convert = Constants.radtodeg;
                    control_convert = Constants.radtodeg;
                    break;
                case ControlType.Gamma:
                    solver_eps = tolerance / 100;
                    control_min = -80 * Constants.degtorad;
                    control_max = 80 * Constants.degtorad;
                    control_convert = Constants.radtodeg;
                    break;
                case ControlType.Heading:
                    control_min = fdmex.Propagate.GetEuler().Psi - 30 * Constants.degtorad;
                    control_max = fdmex.Propagate.GetEuler().Psi + 30 * Constants.degtorad;
                    state_convert = Constants.radtodeg;
                    break;
            }
        }

        public void Run()
        {

            double last_state_value;
            int i;
            setControl();
            //cout + "FGTrimAxis::Run: " + control_value + endl;
            i = 0;
            bool stable = false;
            while (!stable)
            {
                i++;
                last_state_value = state_value;
                fdmex.RunIC();
                getState();
                if (i > 1)
                {
                    if ((Math.Abs(last_state_value - state_value) < tolerance) || (i >= 100))
                        stable = true;
                }
            }

            its_to_stable_value = i;
            total_stability_iterations += its_to_stable_value;
            total_iterations++;
        }


        public double GetState() { getState(); return state_value; }
        //Accels are not settable
        public void SetControl(double val) { control_value = val; }
        public double GetControl() { return control_value; }

        public StateType GetStateType() { return state; }
        public ControlType GetControlType() { return control; }

        public string GetStateName() { return StateNames[(int)state]; }
        public string GetControlName() { return ControlNames[(int)control]; }

        public double GetControlMin() { return control_min; }
        public double GetControlMax() { return control_max; }

        public void SetControlToMin() { control_value = control_min; }
        public void SetControlToMax() { control_value = control_max; }

        public void SetControlLimits(double min, double max)
        {
            control_min = min;
            control_max = max;
        }

        public void SetTolerance(double ff) { tolerance = ff; }
        public double GetTolerance() { return tolerance; }

        public double GetSolverEps() { return solver_eps; }
        public void SetSolverEps(double ff) { solver_eps = ff; }

        public int GetIterationLimit() { return max_iterations; }
        public void SetIterationLimit(int ii) { max_iterations = ii; }

        public int GetStability() { return its_to_stable_value; }
        public int GetRunCount() { return total_stability_iterations; }
        public double GetAvgStability()
        {
            if (total_iterations > 0)
            {
                return (double)(total_stability_iterations) / (double)(total_iterations);
            }
            return 0;
        }

        public void SetThetaOnGround(double ff)
        {
            int center, i, intref;

            // favor an off-center unit so that the same one can be used for both
            // pitch and roll.  An on-center unit is used (for pitch)if that's all
            // that's in contact with the ground.
            i = 0; intref = -1; center = -1;
            while ((intref < 0) && (i < fdmex.GroundReactions.NumGearUnits))
            {
                if (fdmex.GroundReactions.GetGearUnit(i).GetWOW())
                {
                    if (Math.Abs(fdmex.GroundReactions.GetGearUnit(i).GetBodyLocation(2)) > 0.01)
                        intref = i;
                    else
                        center = i;
                }
                i++;
            }
            if ((intref < 0) && (center >= 0))
            {
                intref = center;
            }
            if (log.IsDebugEnabled)
                log.Debug("SetThetaOnGround ref gear: " + intref);
            if (intref >= 0)
            {
                double sp = fdmex.Propagate.GetSinEuler().Phi;
                double cp = fdmex.Propagate.GetCosEuler().Phi;
                double lx = fdmex.GroundReactions.GetGearUnit(intref).GetBodyLocation(1);
                double ly = fdmex.GroundReactions.GetGearUnit(intref).GetBodyLocation(2);
                double lz = fdmex.GroundReactions.GetGearUnit(intref).GetBodyLocation(3);
                double hagl = -1 * lx * Math.Sin(ff) +
                    ly * sp * Math.Cos(ff) +
                    lz * cp * Math.Cos(ff);

                fgic.AltitudeAGLFtIC = hagl;
                if (log.IsDebugEnabled)
                    log.Debug("SetThetaOnGround new alt: " + hagl);
            }
            fgic.ThetaRadIC = ff;
            if (log.IsDebugEnabled)
                log.Debug("SetThetaOnGround new theta: " + ff);
        }

        public void SetPhiOnGround(double ff)
        {
            int i, intref;

            i = 0; intref = -1;
            //must have an off-center unit here
            while ((intref < 0) && (i < fdmex.GroundReactions.NumGearUnits))
            {
                if ((fdmex.GroundReactions.GetGearUnit(i).GetWOW()) &&
                    (Math.Abs(fdmex.GroundReactions.GetGearUnit(i).GetBodyLocation(2)) > 0.01))
                    intref = i;
                i++;
            }
            if (intref >= 0)
            {
                double st = fdmex.Propagate.GetSinEuler().Theta;
                double ct = fdmex.Propagate.GetCosEuler().Theta;
                double lx = fdmex.GroundReactions.GetGearUnit(intref).GetBodyLocation(1);
                double ly = fdmex.GroundReactions.GetGearUnit(intref).GetBodyLocation(2);
                double lz = fdmex.GroundReactions.GetGearUnit(intref).GetBodyLocation(3);
                double hagl = -1 * lx * st +
                    ly * Math.Sin(ff) * ct +
                    lz * Math.Cos(ff) * ct;

                fgic.AltitudeAGLFtIC = hagl;
            }
            fgic.SetRollAngleRadIC(ff);

        }

        public void SetStateTarget(double target) { state_target = target; }
        public double GetStateTarget() { return state_target; }

        public bool initTheta()
        {
            int i, N;
            int iForward = 0;
            int iAft = 1;
            double zAft, zForward, zDiff, theta;
            double xAft, xForward, xDiff;
            bool level;
            double saveAlt;

            saveAlt = fgic.AltitudeAGLFtIC;
            fgic.AltitudeAGLFtIC = 100;


            N = fdmex.GroundReactions.NumGearUnits;

            //find the first wheel unit forward of the cg
            //the list is short so a simple linear search is fine
            for (i = 0; i < N; i++)
            {
                if (fdmex.GroundReactions.GetGearUnit(i).GetBodyLocation(1) > 0)
                {
                    iForward = i;
                    break;
                }
            }
            //now find the first wheel unit aft of the cg
            for (i = 0; i < N; i++)
            {
                if (fdmex.GroundReactions.GetGearUnit(i).GetBodyLocation(1) < 0)
                {
                    iAft = i;
                    break;
                }
            }

            // now adjust theta till the wheels are the same distance from the ground
            xAft = fdmex.GroundReactions.GetGearUnit(iAft).GetBodyLocation(1);
            xForward = fdmex.GroundReactions.GetGearUnit(iForward).GetBodyLocation(1);
            xDiff = xForward - xAft;
            zAft = fdmex.GroundReactions.GetGearUnit(iAft).GetLocalGear(3);
            zForward = fdmex.GroundReactions.GetGearUnit(iForward).GetLocalGear(3);
            zDiff = zForward - zAft;
            level = false;
            theta = fgic.ThetaDegIC;
            while (!level && (i < 100))
            {
                theta += Constants.radtodeg * Math.Atan(zDiff / xDiff);
                fgic.ThetaDegIC = theta;
                fdmex.RunIC();
                zAft = fdmex.GroundReactions.GetGearUnit(iAft).GetLocalGear(3);
                zForward = fdmex.GroundReactions.GetGearUnit(iForward).GetLocalGear(3);
                zDiff = zForward - zAft;
                //cout + endl + theta + "  " + zDiff + endl;
                //cout + "0: " + fdmex.GroundReactions.GetGearUnit(0).GetLocalGear() + endl;
                //cout + "1: " + fdmex.GroundReactions.GetGearUnit(1).GetLocalGear() + endl;
                if (Math.Abs(zDiff) < 0.1)
                    level = true;
                i++;
            }
            //cout + i + endl;
            if (log.IsDebugEnabled)
            {
                log.Debug("    Initial Theta: " + fdmex.Propagate.GetEuler().Theta * Constants.radtodeg);
                log.Debug("    Used gear unit " + iAft + " as aft and " + iForward + " as forward");
            }
            control_min = (theta + 5) * Constants.degtorad;
            control_max = (theta - 5) * Constants.degtorad;
            fgic.AltitudeAGLFtIC = saveAlt;
            if (i < 100)
                return true;
            else
                return false;
        }

        public void AxisReport()
        {


            log.Debug(" " + GetControlName() + " "
                + " " + GetControl() * control_convert + " "
                + GetStateName() + " " + (double)(GetState() + state_target)
                + " " + "Tolerance:" + " " + GetTolerance());

            if (Math.Abs(GetState() + state_target) < Math.Abs(GetTolerance()))
                log.Debug("  Passed");
            else
                log.Debug("  Failed");
        }


        public bool InTolerance() { getState(); return (Math.Abs(state_value) <= tolerance); }

        private FDMExecutive fdmex;
        private InitialCondition fgic;

        private StateType state;
        private ControlType control;

        private double state_target;

        private double state_value;
        private double control_value;

        private double control_min;
        private double control_max;

        private double tolerance;

        private double solver_eps;

        private double state_convert;
        private double control_convert;

        private int max_iterations;

        private int its_to_stable_value;
        private int total_stability_iterations;
        private int total_iterations;

        private void setThrottlesPct()
        {
            double tMin, tMax;
            for (int i = 0; i < fdmex.Propulsion.GetNumEngines(); i++)
            {
                tMin = fdmex.Propulsion.GetEngine(i).GetThrottleMin();
                tMax = fdmex.Propulsion.GetEngine(i).GetThrottleMax();
                //cout + "setThrottlespct: " + i + ", " + control_min + ", " + control_max + ", " + control_value;
                fdmex.FlightControlSystem.SetThrottleCmd(i, tMin + control_value * (tMax - tMin));
                //cout + "setThrottlespct: " + fdmex.GetFCS().GetThrottleCmd(i) + endl;
                fdmex.RunIC(); //apply throttle change
                fdmex.Propulsion.GetSteadyState();
            }
        }

        private void getState()
        {
            switch (state)
            {
                case StateType.Udot: state_value = fdmex.Propagate.GetUVWdot(0) - state_target; break;
                case StateType.Vdot: state_value = fdmex.Propagate.GetUVWdot(1) - state_target; break;
                case StateType.Wdot: state_value = fdmex.Propagate.GetUVWdot(2) - state_target; break;
                case StateType.Qdot: state_value = fdmex.Propagate.GetPQRdot(1) - state_target; break;
                case StateType.Pdot: state_value = fdmex.Propagate.GetPQRdot(0) - state_target; break;
                case StateType.Rdot: state_value = fdmex.Propagate.GetPQRdot(2) - state_target; break;
                case StateType.Hmgt: state_value = computeHmgt() - state_target; break;
                case StateType.Nlf: state_value = fdmex.Aircraft.GetNlf() - state_target; break;
                case StateType.All: break;
            }
        }

        private void getControl()
        {
            switch (control)
            {
                case ControlType.Throttle: control_value = fdmex.FlightControlSystem.GetThrottleCmd(0); break;
                case ControlType.Beta: control_value = fdmex.Auxiliary.Getalpha(); break;
                case ControlType.Alpha: control_value = fdmex.Auxiliary.Getbeta(); break;
                case ControlType.PitchTrim: control_value = fdmex.FlightControlSystem.PitchTrimCmd; break;
                case ControlType.Elevator: control_value = fdmex.FlightControlSystem.ElevatorCmd; break;
                case ControlType.RollTrim:
                case ControlType.Aileron: control_value = fdmex.FlightControlSystem.AileronCmd; break;
                case ControlType.YawTrim:
                case ControlType.Rudder: control_value = fdmex.FlightControlSystem.RudderCmd; break;
                case ControlType.AltAGL: control_value = fdmex.Propagate.DistanceAGL; break;
                case ControlType.Theta: control_value = fdmex.Propagate.GetEuler().Theta; break;
                case ControlType.Phi: control_value = fdmex.Propagate.GetEuler().Phi; break;
                case ControlType.Gamma: control_value = fdmex.Auxiliary.Gamma; break;
                case ControlType.Heading: control_value = fdmex.Propagate.GetEuler().Psi; break;
            }
        }

        private void setControl()
        {
            switch (control)
            {
                case ControlType.Throttle: setThrottlesPct(); break;
                case ControlType.Beta: fgic.BetaRadIC = control_value; break;
                case ControlType.Alpha: fgic.AlphaRadIC = control_value; break;
                case ControlType.PitchTrim: fdmex.FlightControlSystem.PitchTrimCmd = control_value; break;
                case ControlType.Elevator: fdmex.FlightControlSystem.ElevatorCmd = control_value; break;
                case ControlType.RollTrim:
                case ControlType.Aileron: fdmex.FlightControlSystem.AileronCmd = control_value; break;
                case ControlType.YawTrim:
                case ControlType.Rudder: fdmex.FlightControlSystem.RudderCmd = control_value; break;
                case ControlType.AltAGL: fgic.AltitudeAGLFtIC = control_value; break;
                case ControlType.Theta: fgic.ThetaRadIC = control_value; break;
                case ControlType.Phi: fgic.SetRollAngleRadIC(control_value); break;
                case ControlType.Gamma: fgic.SetFlightPathAngleRadIC(control_value); break;
                case ControlType.Heading: fgic.SetTrueHeadingRadIC(control_value); break;
            }
        }


        private double computeHmgt()
        {
            double diff;

            diff = fdmex.Propagate.GetEuler().Psi -
                fdmex.Auxiliary.GroundTrack;

            if (diff < -Math.PI)
            {
                return (diff + 2 * Math.PI);
            }
            else if (diff > Math.PI)
            {
                return (diff - 2 * Math.PI);
            }
            else
            {
                return diff;
            }

        }

        private static readonly string[] StateNames = new string[] 
						{
							"all","udot","vdot","wdot","qdot","pdot","rdot",
							"hmgt","nlf" 
						};
        private static readonly string[] ControlNames = new string[]
						{
							"Throttle","Sideslip","Angle of Attack",
							"Elevator","Ailerons","Rudder",
							"Altitude AGL", "Pitch Angle",
							"Roll Angle", "Flight Path Angle", 
							"Pitch Trim", "Roll Trim", "Yaw Trim",
							"Heading"
						};


        private const double DEFAULT_TOLERANCE = 0.001;
        private const string IdSrc = "$Id: FGTrimAxis.cpp,v 1.53 2004/09/15 12:21:05 ehofman Exp $";
    }
}
