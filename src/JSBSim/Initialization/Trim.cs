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
	using System.Collections;
    using System.Collections.Generic;
	using CommonUtils.MathLib;

	// Import log4net classes.
	using log4net;


	public enum TrimMode
	{
		Longitudinal,
		Full,
		Ground,
		Pullup,
		Custom,
		None,
		Turn
	} ;


	/// <summary>
	/// Trim -- the trimming routine for JSBSim.
	/// Trim finds the aircraft attitude and control settings needed to maintain
	/// the steady state described by the InitialCondition object .  It does this
	/// iteratively by assigning a control to each state and adjusting that control
	/// until the state is within a specified tolerance of zero. States include the
	/// recti-linear accelerations udot, vdot, and wdot, the angular accelerations
	/// qdot, pdot, and rdot, and the difference between heading and ground track.
	/// Controls include the usual flight deck controls available to the pilot plus
	/// angle of attack (alpha), sideslip angle(beta), flight path angle (gamma),
	/// pitch attitude(theta), roll attitude(phi), and altitude above ground.  The
	/// last three are used for on-ground trimming. The state-control pairs used in
	/// a given trim are completely user configurable and several pre-defined modes
	/// are provided as well. They are:
	/// - tLongitudinal: Trim wdot with alpha, udot with thrust, qdot with elevator
	/// - tFull: tLongitudinal + vdot with phi, pdot with aileron, rdot with rudder
	/// and heading minus ground track (hmgt) with beta
	/// - tPullup: tLongitudinal but adjust alpha to achieve load factor input
	/// with SetTargetNlf()
	/// - tGround: wdot with altitude, qdot with theta, and pdot with phi
	/// 
	/// The remaining modes include <b>tCustom</b>, which is completely user defined and
	/// <b>tNone</b>.
	/// 
	/// Note that trims can (and do) fail for reasons that are completely outside
	/// the control of the trimming routine itself. The most common problem is the
	/// initial conditions: is the model capable of steady state flight
	/// at those conditions?  Check the speed, altitude, configuration (flaps,
	/// gear, etc.), weight, cg, and anything else that may be relevant.
	/// 
	/// Example usage:<pre>
	/// FGFDMExec* FDMExec = new FGFDMExec();
	/// 
	/// FGInitialCondition* fgic = new FGInitialCondition(FDMExec);
	/// FGTrim fgt(FDMExec, fgic, tFull);
	/// fgic.SetVcaibratedKtsIC(100);
	/// fgic.SetAltitudeFtIC(1000);
	/// fgic.SetClimbRate(500);
	/// if( !fgt.DoTrim() ) 
	/// {
	/// cout + "Trim Failed" + endl;
	/// }
	/// fgt.Report(); </pre>
	/// @author Tony Peden
	/// </summary>
	public class Trim
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
		/// Initializes the trimming class
		/// </summary>
		/// <param name="fdmexec">a JSBSim executive object</param>
		/// <param name="tm">trim mode</param>
		public Trim(FDMExecutive exec, TrimMode tm)
		{

			N=Nsub=0;
			max_iterations=60;
			max_sub_iterations=100;
			Tolerance=1E-3;
			A_Tolerance = Tolerance / 10;
  
			Debug=0;DebugLevel=0;
			fdmex=exec;
			fgic=fdmex.GetIC;
			total_its=0;
			trimudot=true;
			gamma_fallback=true;
			axis_count=0;
			mode=tm;
			xlo=xhi=alo=ahi=0.0;
			targetNlf=1.0;
			debug_axis=StateType.All;
			SetMode(tm);
			if (log.IsDebugEnabled)
				log.Debug("Instantiated: FGTrim");
		}


		/// <summary>
		/// Execute the trim
		/// </summary>
		/// <returns></returns>
        public bool DoTrim()
        {

            trim_failed = false;
            int i;

            for (i = 0; i < fdmex.GroundReactions.NumGearUnits; i++)
            {
                fdmex.GroundReactions.GetGearUnit(i).SetReport(false);
            }

            fdmex.DisableOutput();

            fgic.PRadpsIC = 0.0;
            fgic.QRadpsIC = 0.0;
            fgic.RRadpsIC = 0.0;

            //clear the sub iterations counts & zero out the controls
            for (current_axis = 0; current_axis < TrimAxes.Count; current_axis++)
            {
                //cout + current_axis + "  " + TrimAxes[current_axis].GetStateName()
                //+ "  " + TrimAxes[current_axis].GetControlName()+ endl;
                if (TrimAxes[current_axis].GetStateType() == StateType.Qdot)
                {
                    if (mode == TrimMode.Ground)
                    {
                        TrimAxes[current_axis].initTheta();
                    }
                }
                xlo = TrimAxes[current_axis].GetControlMin();
                xhi = TrimAxes[current_axis].GetControlMax();
                TrimAxes[current_axis].SetControl((xlo + xhi) / 2);
                TrimAxes[current_axis].Run();
                //TrimAxes[current_axis].AxisReport();
                sub_iterations[current_axis] = 0;
                successful[current_axis] = 0;
                solution[current_axis] = false;
            }


            if (mode == TrimMode.Pullup)
            {
                log.Debug("Setting pitch rate and nlf... ");
                setupPullup();
                log.Debug("pitch rate done ... ");
                TrimAxes[0].SetStateTarget(targetNlf);
                log.Debug("nlf done");
            }
            else if (mode == TrimMode.Turn)
            {
                setupTurn();
                //TrimAxes[0].SetStateTarget(targetNlf);
            }

            do
            {
                axis_count = 0;
                for (current_axis = 0; current_axis < TrimAxes.Count; current_axis++)
                {
                    SetDebug();
                    UpdateRates();
                    Nsub = 0;
                    if (!solution[current_axis])
                    {
                        if (CheckLimits())
                        {
                            solution[current_axis] = true;
                            Solve();
                        }
                    }
                    else if (FindInterval())
                    {
                        Solve();
                    }
                    else
                    {
                        solution[current_axis] = false;
                    }
                    sub_iterations[current_axis] += Nsub;
                }
                for (current_axis = 0; current_axis < TrimAxes.Count; current_axis++)
                {
                    //these checks need to be done after all the axes have run
                    if (Debug > 0)
                        TrimAxes[current_axis].AxisReport();
                    if (TrimAxes[current_axis].InTolerance())
                    {
                        axis_count++;
                        successful[current_axis]++;
                    }
                }


                if ((axis_count == TrimAxes.Count - 1) && (TrimAxes.Count > 1))
                {
                    //cout + TrimAxes.size()-1 + " out of " + TrimAxes.size() + "!" + endl;
                    //At this point we can check the input limits of the failed axis
                    //and declare the trim failed if there is no sign change. If there
                    //is, keep going until success or max iteration count

                    //Oh, well: two out of three ain't bad
                    for (current_axis = 0; current_axis < TrimAxes.Count; current_axis++)
                    {
                        //these checks need to be done after all the axes have run
                        if (!TrimAxes[current_axis].InTolerance())
                        {
                            if (!CheckLimits())
                            {
                                // special case this for now -- if other cases arise proper
                                // support can be added to TrimAxis
                                if ((gamma_fallback) &&
                                    (TrimAxes[current_axis].GetStateType() == StateType.Udot) &&
                                    (TrimAxes[current_axis].GetControlType() == ControlType.Throttle))
                                {
                                    if (log.IsErrorEnabled)
                                        log.Error("  Can't trim udot with throttle, trying flight"
                                            + " path angle. (" + N + ")");
                                    if (TrimAxes[current_axis].GetState() > 0)
                                        TrimAxes[current_axis].SetControlToMin();
                                    else
                                        TrimAxes[current_axis].SetControlToMax();
                                    TrimAxes[current_axis].Run();
                                    //TODO delete TrimAxes[current_axis];
                                    TrimAxes[current_axis] = new TrimAxis(fdmex, fgic, StateType.Udot,
                                        ControlType.Gamma);
                                }
                                else
                                {
                                    if (log.IsErrorEnabled)
                                        log.Error("  Sorry, " + TrimAxes[current_axis].GetStateName()
                                        + " doesn't appear to be trimmable");
                                    //total_its=k;
                                    trim_failed = true; //force the trim to fail
                                } //gamma_fallback
                            }
                        } //solution check
                    } //for loop
                } //all-but-one check
                N++;
                if (N > max_iterations)
                    trim_failed = true;
            } while ((axis_count < TrimAxes.Count) && (!trim_failed));
            if ((!trim_failed) && (axis_count >= TrimAxes.Count))
            {
                total_its = N;
                if (log.IsDebugEnabled)
                    log.Debug("  Trim successful");
            }
            else
            {
                total_its = N;
                if (log.IsDebugEnabled)
                    log.Debug("  Trim failed");
            }
            for (i = 0; i < fdmex.GroundReactions.NumGearUnits; i++)
            {
                fdmex.GroundReactions.GetGearUnit(i).SetReport(true);
            }
            fdmex.EnableOutput();
            return !trim_failed;
        }

		/** Print the results of the trim. For each axis trimmed, this
			includes the final state value, control value, and tolerance
			used.
			@return true if trim succeeds
		*/
		public void Report()
		{

			log.Info("  Trim Results: ");
			for(current_axis=0; current_axis<TrimAxes.Count; current_axis++)
				TrimAxes[current_axis].AxisReport();

		}

		/** Iteration statistics
		*/
		public void TrimStats()
		{
			int run_sum=0;
			log.Info("  Trim Statistics: ");
			log.Info("    Total Iterations: " + total_its);
			if(total_its > 0) 
			{
				log.Info("    Sub-iterations:");
				int axis = 0;
				foreach (TrimAxis currentAxis in TrimAxes) 
				{
					run_sum+= currentAxis.GetRunCount();
					log.Info(currentAxis.GetStateName() +": "+
						sub_iterations[axis]+" average: "+
						sub_iterations[axis]/(double)(total_its) + "  successful: " +
						successful[axis] + "  stability: "+
						currentAxis.GetAvgStability() );
					axis ++;
				}
				log.Info("    Run Count: " + run_sum);
			}
		}

		/// <summary>
		/// Clear all state-control pairs and set a predefined trim mode
		/// </summary>
		/// <param name="tm">the set of axes to trim. Can be:
		/// tLongitudinal, tFull, tGround, tCustom, or tNone</param>
		public void SetMode(TrimMode tm)
		{
			ClearStates();
			mode=tm;
			switch(tm) 
			{
				case TrimMode.Full:
					if (log.IsDebugEnabled)          
						log.Debug("  Full Trim");
					TrimAxes.Add(new TrimAxis(fdmex,fgic,StateType.Wdot,ControlType.Alpha ));
					TrimAxes.Add(new TrimAxis(fdmex,fgic,StateType.Udot,ControlType.Throttle ));
					TrimAxes.Add(new TrimAxis(fdmex,fgic,StateType.Qdot,ControlType.PitchTrim ));
					TrimAxes.Add(new TrimAxis(fdmex,fgic,StateType.Hmgt,ControlType.Beta ));
					TrimAxes.Add(new TrimAxis(fdmex,fgic,StateType.Vdot,ControlType.Phi ));
					TrimAxes.Add(new TrimAxis(fdmex,fgic,StateType.Pdot,ControlType.Aileron ));
					TrimAxes.Add(new TrimAxis(fdmex,fgic,StateType.Rdot,ControlType.Rudder ));
					break;
				case TrimMode.Longitudinal:
					if (log.IsDebugEnabled)          
						log.Debug("  Longitudinal Trim");
					TrimAxes.Add(new TrimAxis(fdmex,fgic,StateType.Wdot,ControlType.Alpha ));
					TrimAxes.Add(new TrimAxis(fdmex,fgic,StateType.Udot,ControlType.Throttle ));
					TrimAxes.Add(new TrimAxis(fdmex,fgic,StateType.Qdot,ControlType.PitchTrim ));
					break;
				case TrimMode.Ground:
					if (log.IsDebugEnabled)          
						log.Debug("  Ground Trim");
					TrimAxes.Add(new TrimAxis(fdmex,fgic,StateType.Wdot,ControlType.AltAGL ));
					TrimAxes.Add(new TrimAxis(fdmex,fgic,StateType.Qdot,ControlType.Theta ));
					//TrimAxes.push_back(new TrimAxis(fdmex,fgic,tPdot,tPhi ));
					break;
				case TrimMode.Pullup:
					TrimAxes.Add(new TrimAxis(fdmex,fgic,StateType.Nlf,ControlType.Alpha ));
					TrimAxes.Add(new TrimAxis(fdmex,fgic,StateType.Udot,ControlType.Throttle ));
					TrimAxes.Add(new TrimAxis(fdmex,fgic,StateType.Qdot,ControlType.PitchTrim ));
					TrimAxes.Add(new TrimAxis(fdmex,fgic,StateType.Hmgt,ControlType.Beta ));
					TrimAxes.Add(new TrimAxis(fdmex,fgic,StateType.Vdot,ControlType.Phi ));
					TrimAxes.Add(new TrimAxis(fdmex,fgic,StateType.Pdot,ControlType.Aileron ));
					TrimAxes.Add(new TrimAxis(fdmex,fgic,StateType.Rdot,ControlType.Rudder ));
					break;
				case TrimMode.Turn:
					TrimAxes.Add(new TrimAxis(fdmex,fgic,StateType.Wdot,ControlType.Alpha ));
					TrimAxes.Add(new TrimAxis(fdmex,fgic,StateType.Udot,ControlType.Throttle ));
					TrimAxes.Add(new TrimAxis(fdmex,fgic,StateType.Qdot,ControlType.PitchTrim ));
					TrimAxes.Add(new TrimAxis(fdmex,fgic,StateType.Vdot,ControlType.Beta ));
					TrimAxes.Add(new TrimAxis(fdmex,fgic,StateType.Pdot,ControlType.Aileron ));
					TrimAxes.Add(new TrimAxis(fdmex,fgic,StateType.Rdot,ControlType.Rudder ));
					break;
				case TrimMode.Custom:
				case TrimMode.None:
					break;
			}
			//cout + "TrimAxes.size(): " + TrimAxes.size() + endl;
			sub_iterations = new double[TrimAxes.Count];
			successful = new double[TrimAxes.Count];
			solution = new bool[TrimAxes.Count];
			current_axis=0;
		}


		/// <summary>
		/// Clear all state-control pairs from the current configuration.
		/// The trimming routine must have at least one state-control pair
		/// configured to be useful
		/// </summary>
		public void ClearStates()
		{
			mode=TrimMode.Custom;
			TrimAxes.Clear();
		}

		/// <summary>
		/// Add a state-control pair to the current configuration. See the enums
		/// State and Control in TrimAxis.h for the available options.
		/// Will fail if the given state is already configured.
		/// </summary>
		/// <param name="state">the accel or other condition to zero</param>
		/// <param name="control">the control used to zero the state</param>
		/// <returns>true if add is successful</returns>
		public bool AddState( StateType state, ControlType control )
		{
			bool result=true;
  
			mode = TrimMode.Custom;
			//vector <TrimAxis*>::iterator iAxes = TrimAxes.begin();
			foreach (TrimAxis iAxes in TrimAxes) 
			{
				if( iAxes.GetStateType() == state )
					result=false;
			}
			if(result) 
			{
				TrimAxes.Add(new TrimAxis(fdmex,fgic,state,control));

				sub_iterations=new double[TrimAxes.Count];
				successful=new double[TrimAxes.Count];
				solution=new bool[TrimAxes.Count];
			}
			return result;
		}  


		/// <summary>
		/// Remove a specific state-control pair from the current configuration
		/// </summary>
		/// <param name="state">the state to remove</param>
		/// <returns>true if removal is successful</returns>
		public bool RemoveState( StateType state )
		{
			bool result=false;
  
			mode = TrimMode.Custom;
			foreach (TrimAxis iAxes in TrimAxes) 
			{
				if( iAxes.GetStateType() == state ) 
				{
					TrimAxes.Remove(iAxes);
					result=true;
					continue;
				}
			}
			if(result) 
			{
				sub_iterations=new double[TrimAxes.Count];
				successful=new double[TrimAxes.Count];
				solution=new bool[TrimAxes.Count];
			}  
			return result;
		}  

		/// <summary>
		/// Change the control used to zero a state previously configured
		/// </summary>
		/// <param name="state">the accel or other condition to zero</param>
		/// <param name="new_control">the control used to zero the state</param>
		/// <returns></returns>
		public bool EditState( StateType state, ControlType new_control )
		{       
			bool result=false;
  
			mode = TrimMode.Custom;
			int pos = 0;
			foreach (TrimAxis iAxes in TrimAxes) 
			{
				if( iAxes.GetStateType() == state ) 
				{
					TrimAxes[pos] = new TrimAxis(fdmex,fgic,state,new_control);
					result=true;
					break;
				}
				pos ++;

			}
			return result;
		}

		/** automatically switch to trimming longitudinal acceleration with
			flight path angle (gamma) once it becomes apparent that there
			is not enough/too much thrust.
			@param bb true to enable fallback
		*/
		public  void SetGammaFallback(bool bb) { gamma_fallback=bb; }

		/** query the fallback state
			@return true if fallback is enabled.
		*/
		public  bool GetGammaFallback() { return gamma_fallback; }

		/** Set the iteration limit. DoTrim() will return false if limit
			iterations are reached before trim is achieved.  The default
			is 60.  This does not ordinarily need to be changed.
			@param ii integer iteration limit
		*/
		public  void SetMaxCycles(int ii) { max_iterations = ii; }

		/** Set the per-axis iteration limit.  Attempt to zero each state
			by iterating limit times before moving on to the next. The
			default limit is 100 and also does not ordinarily need to
			be changed.
			@param ii integer iteration limit
		*/
		public  void SetMaxCyclesPerAxis(int ii) { max_sub_iterations = ii; }

		/** Set the tolerance for declaring a state trimmed. Angular accels are
			held to a tolerance of 1/10th of the given.  The default is
			0.001 for the recti-linear accelerations and 0.0001 for the angular.
		*/
		public  void SetTolerance(double tt) 
		{
			Tolerance = tt;
			A_Tolerance = tt / 10;
		}

		/**
		  Debug level 1 shows results of each top-level iteration
		  Debug level 2 shows level 1 & results of each per-axis iteration
		*/
		public  void SetDebug(int level) { DebugLevel = level; }
		public  void ClearDebug() { DebugLevel = 0; }

		/**
		  Output debug data for one of the axes
		  The State enum is defined in TrimAxis.h
		*/
		public  void DebugState(StateType state) { debug_axis=state; }

		public  void SetTargetNlf(float nlf) { targetNlf=nlf; }
		public  double GetTargetNlf() { return targetNlf; }


		//TODO private vector<TrimAxis*> TrimAxes;
        private List<TrimAxis> TrimAxes = new List<TrimAxis>();
		private int current_axis;
		private int N, Nsub;
		private TrimMode mode;
		private int DebugLevel, Debug;
		private double Tolerance, A_Tolerance;
		private double wdot,udot,qdot;
		private double dth;
		private double[] sub_iterations;
		private double[] successful;
		private bool[] solution;
		private int max_sub_iterations;
		private int max_iterations;
		private int total_its;
		private bool trimudot;
		private bool gamma_fallback;
		private bool trim_failed;
		private uint axis_count;
		private int solutionDomain;
		private double xlo,xhi,alo,ahi;
		private double targetNlf;
		private StateType debug_axis;

		private double psidot,thetadot;

		private FDMExecutive fdmex;
		private InitialCondition fgic;

		private bool Solve()
		{

			double x1,x2,x3,f1,f2,f3,d,d0;
			const double relax =0.9;
			double eps = TrimAxes[current_axis].GetSolverEps();

			x1=x2=x3=0;
			d=1;
			bool success=false;
			//initializations
			if( solutionDomain != 0) 
			{
				/* if(ahi > alo) { */
				x1=xlo;f1=alo;
				x3=xhi;f3=ahi;
				/* } else {
				   x1=xhi;f1=ahi;
				   x3=xlo;f3=alo;
				 }   */
				d0=Math.Abs(x3-x1);
				//iterations
				//max_sub_iterations=TrimAxes[current_axis].GetIterationLimit();
				while ( (TrimAxes[current_axis].InTolerance() == false )
					&& (Math.Abs(d) > eps) && (Nsub < max_sub_iterations)) 
				{
					Nsub++;
					d=(x3-x1)/d0;
					x2=x1-d*d0*f1/(f3-f1);
					TrimAxes[current_axis].SetControl(x2);
					TrimAxes[current_axis].Run();
					f2=TrimAxes[current_axis].GetState();
					if(log.IsDebugEnabled && Debug > 1) 
					{
						log.Debug("Trim.Solve Nsub,x1,x2,x3: " + Nsub + ", " + x1
							+ ", " + x2 + ", " + x3);
						log.Debug("                          " + f1 + ", " + f2 + ", " + f3);
					}
					if(f1*f2 <= 0.0) 
					{
						x3=x2;
						f3=f2;
						f1=relax*f1;
						//cout + "Solution is between x1 and x2" + endl;
					}
					else if(f2*f3 <= 0.0) 
					{
						x1=x2;
						f1=f2;
						f3=relax*f3;
						//cout + "Solution is between x2 and x3" + endl;

					}
					//cout + i + endl;

      
				}//end while
				if(Nsub < max_sub_iterations) success=true;
			}  
			return success;
		}

		/** @return false if there is no change in the current axis accel
			between accel(control_min) and accel(control_max). If there is a
			change, sets solutionDomain to:
			0 for no sign change,
		   -1 if sign change between accel(control_min) and accel(0)
			1 if sign between accel(0) and accel(control_max)
		*/

		/// <summary>
		/// produces an interval (xlo..xhi) on one side or the other of the current 
		/// control value in which a solution exists.  This domain is, hopefully, 
		/// smaller than xmin..0 or 0..xmax and the solver will require fewer iterations 
		/// to find the solution. This is, hopefully, more efficient than having the 
		/// solver start from scratch every time. Maybe it isn't though...
		/// This tries to take advantage of the idea that the changes from iteration to
		/// iteration will be small after the first one or two top-level iterations.
		/// 
		/// assumes that changing the control will a produce significant change in the
		/// accel i.e. CheckLimits() has already been called.
		/// 
		/// if a solution is found above the current control, the function returns true 
		/// and xlo is set to the current control, xhi to the interval max it found, and 
		/// solutionDomain is set to 1.
		/// if the solution lies below the current control, then the function returns 
		/// true and xlo is set to the interval min it found and xmax to the current 
		/// control. if no solution is found, then the function returns false.
		///  
		/// in all cases, alo=accel(xlo) and ahi=accel(xhi) after the function exits.
		/// no assumptions about the state of the sim after this function has run 
		/// can be made.
		/// </summary>
		/// <returns></returns>
		private bool FindInterval()
		{
			bool found=false;
			double step;
			double current_control=TrimAxes[current_axis].GetControl();
			double current_accel=TrimAxes[current_axis].GetState();;
			double xmin=TrimAxes[current_axis].GetControlMin();
			double xmax=TrimAxes[current_axis].GetControlMax();
			double lastxlo,lastxhi,lastalo,lastahi;
  
			step=0.025*Math.Abs(xmax);
			xlo=xhi=current_control;
			alo=ahi=current_accel;
			lastxlo=xlo;lastxhi=xhi;
			lastalo=alo;lastahi=ahi;
			do 
			{
    
				Nsub++;
				step*=2;
				xlo-=step;
				if(xlo < xmin) xlo=xmin;
				xhi+=step;
				if(xhi > xmax) xhi=xmax;
				TrimAxes[current_axis].SetControl(xlo);
				TrimAxes[current_axis].Run();
				alo=TrimAxes[current_axis].GetState();
				TrimAxes[current_axis].SetControl(xhi);
				TrimAxes[current_axis].Run();
				ahi=TrimAxes[current_axis].GetState();
				if(Math.Abs(ahi-alo) <= TrimAxes[current_axis].GetTolerance()) continue;
				if(alo*ahi <=0) 
				{  //found interval with root
					found=true;
					if(alo*current_accel <= 0) 
					{ //narrow interval down a bit
						solutionDomain=-1;
						xhi=lastxlo;
						ahi=lastalo;
						//xhi=current_control;
						//ahi=current_accel;
					} 
					else 
					{
						solutionDomain=1;
						xlo=lastxhi;
						alo=lastahi;
						//xlo=current_control;
						//alo=current_accel;
					}     
				}
				lastxlo=xlo;lastxhi=xhi;
				lastalo=alo;lastahi=ahi;
				if( !found && xlo==xmin && xhi==xmax ) continue;
				if(log.IsDebugEnabled && Debug >1)
					log.Debug("Trim::FindInterval: Nsub=" + Nsub + " Lo= " + xlo
						+ " Hi= " + xhi + " alo*ahi: " + alo*ahi );
			} while(!found && (Nsub <= max_sub_iterations) );
			return found;
		}

		private bool CheckLimits()
		{
			bool solutionExists;
			double current_control=TrimAxes[current_axis].GetControl();
			double current_accel=TrimAxes[current_axis].GetState();
			xlo=TrimAxes[current_axis].GetControlMin();
			xhi=TrimAxes[current_axis].GetControlMax();

			TrimAxes[current_axis].SetControl(xlo);
			TrimAxes[current_axis].Run();
			alo=TrimAxes[current_axis].GetState();
			TrimAxes[current_axis].SetControl(xhi);
			TrimAxes[current_axis].Run();
			ahi=TrimAxes[current_axis].GetState();
			if(log.IsDebugEnabled && Debug > 1)
				log.Debug("CheckLimits() xlo,xhi,alo,ahi: " + xlo + ", " + xhi + ", "
					+ alo + ", " + ahi);
			solutionDomain=0;
			solutionExists=false;
			if(Math.Abs(ahi-alo) > TrimAxes[current_axis].GetTolerance()) 
			{
				if(alo*current_accel <= 0) 
				{
					solutionExists=true;
					solutionDomain=-1;
					xhi=current_control;
					ahi=current_accel;
				} 
				else if(current_accel*ahi < 0)
				{
					solutionExists=true;
					solutionDomain=1;
					xlo=current_control;
					alo=current_accel;  
				}
			} 
			TrimAxes[current_axis].SetControl(current_control);
			TrimAxes[current_axis].Run();
			return solutionExists;
		}


		private void setupPullup()
		{
			double g,q,cgamma;
			g=fdmex.Inertial.Gravity;
			cgamma=Math.Cos(fgic.GetFlightPathAngleRadIC());

			if (log.IsDebugEnabled)
				log.Debug("setPitchRateInPullup():  " + g + ", " + cgamma + ", "
					+ fgic.VtrueFpsIC);
			q=g*(targetNlf-cgamma)/fgic.VtrueFpsIC;
			if (log.IsDebugEnabled)
				log.Debug(targetNlf + ", " + q);
			fgic.QRadpsIC = q;
			if (log.IsDebugEnabled)
				log.Debug("setPitchRateInPullup() complete");
  
		}
		private void setupTurn()
		{
			double g,phi;
			phi = fgic.GetRollAngleRadIC();
			if( Math.Abs(phi) > 0.001 && Math.Abs(phi) < 1.56 ) 
			{
				targetNlf = 1 / Math.Cos(phi);
				g = fdmex.Inertial.Gravity; 
				psidot = g*Math.Tan(phi) / fgic.UBodyFpsIC;
				if (log.IsDebugEnabled)
					log.Debug(targetNlf + ", " + psidot);
			}
   
		}

		private void UpdateRates()
		{
			if( mode == TrimMode.Turn ) 
			{
				double phi = fgic.GetRollAngleRadIC();
				double g = fdmex.Inertial.Gravity; 
				double p,q,r,theta;
				if(Math.Abs(phi) > 0.001 && Math.Abs(phi) < 1.56 ) 
				{
                    theta = fgic.ThetaRadIC;
					phi=fgic.GetRollAngleRadIC();
					psidot = g*Math.Tan(phi) / fgic.UBodyFpsIC;
					p=-psidot*Math.Sin(theta);
					q=psidot*Math.Cos(theta)*Math.Sin(phi);
					r=psidot*Math.Cos(theta)*Math.Cos(phi);
				} 
				else 
				{
					p=q=r=0;
				}      
				fgic.PRadpsIC = p;
				fgic.QRadpsIC = q;
				fgic.RRadpsIC = r;
			} 
			else if( mode == TrimMode.Pullup && Math.Abs(targetNlf-1) > 0.01) 
			{
				double g,q,cgamma;
				g = fdmex.Inertial.Gravity;
				cgamma=Math.Cos(fgic.GetFlightPathAngleRadIC());
				q=g*(targetNlf-cgamma)/fgic.VtrueFpsIC;
				fgic.QRadpsIC = q;
			}  
		}

		private void SetDebug()
		{
			if(debug_axis == StateType.All ||
				TrimAxes[current_axis].GetStateType() == debug_axis ) 
			{
				Debug=DebugLevel; 
				return;
			} 
			else 
			{
				Debug=0;
				return;
			}
		}

		private const string IdSrc = "$Id: FGTrim.cpp,v 1.46 2004/04/12 04:07:36 apeden Exp $";

	}
}
