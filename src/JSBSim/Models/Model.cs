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

	// Import log4net classes.
	using log4net;
	using JSBSim.InputOutput;


	/// <summary>
	/// Base class for all scheduled JSBSim models
	/// This code is based on FGFDMExec written by Jon S. Berndt
	/// </summary>
	[Serializable]
	public class Model
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

		public Model (FDMExecutive fdmex)
		{
			FDMExec = fdmex;

			//in order for Model derived classes to self-Bind (that is, call
			//their Bind function in the constructor, the PropertyManager pointer
			//must be brought up now.
			//TODO ?? propertyManager = FDMExec.PropertyManager;

			exe_ctr     = 1;
			rate        = 1;
			
			Bind();
		}

		/// <summary>
		/// Runs the model; called by the Executive
		/// </summary>
		/// <returns>false if no error</returns>
		public virtual bool Run()
		{
            return InternalRun();
		}

        protected bool InternalRun()
        {
            //if (log.IsInfoEnabled)
            //    log.Info("Entering Run() for model " + name);

            if (exe_ctr == 1)
            {
                if (exe_ctr++ >= rate) exe_ctr = 1;
                return false;
            }
            else
            {
                if (exe_ctr++ >= rate) exe_ctr = 1;
                return true;
            }
        }

		public virtual bool InitModel()
		{
			/*
			State           = FDMExec->GetState();
			Atmosphere      = FDMExec->GetAtmosphere();
			FCS             = FDMExec->GetFCS();
			Propulsion      = FDMExec->GetPropulsion();
			MassBalance     = FDMExec->GetMassBalance();
			Aerodynamics    = FDMExec->GetAerodynamics();
			Inertial        = FDMExec->GetInertial();
			GroundReactions = FDMExec->GetGroundReactions();
			Aircraft        = FDMExec->GetAircraft();
			Propagate       = FDMExec->GetPropagate();
			Auxiliary       = FDMExec->GetAuxiliary();
			Output          = FDMExec->GetOutput();

			if (!State ||
				!Atmosphere ||
				!FCS ||
				!Propulsion ||
				!MassBalance ||
				!Aerodynamics ||
				!Inertial ||
				!GroundReactions ||
				!Aircraft ||
				!Propagate ||
				!Auxiliary ||
				!Output) return(false);
			else return(true);
			*/
			return(true);
		}
		
		public virtual int Rate
		{
			get {return rate;}
			set {rate = value;}
		}

		public string Name 
		{ 
			get { return name;}
			set  { name = value;}
		}

		public virtual void Bind()
		{
			FDMExec.PropertyManager.Bind("" ,this);
		}

		public virtual void Unbind()
		{
			FDMExec.PropertyManager.Unbind("" ,this);
		}



		protected int exe_ctr = 1;
		protected int rate = 1;

		protected FDMExecutive		FDMExec;
		//protected PropertyManager	propertyManager;

		protected string name;
		
		/* TODO
		protected FGState            state;
		protected FGAtmosphere       atmosphere;
		protected FGFCS              FCS;
		protected FGPropulsion       propulsion;
		protected FGMassBalance      massBalance;
		protected FGAerodynamics     aerodynamics;
		protected FGInertial         inertial;
		protected FGGroundReactions  groundReactions;
		protected FGAircraft         aircraft;
		protected FGPropagate        propagate;
		protected FGAuxiliary        auxiliary;
		protected FGOutput           output;
		*/
		private const string IdSrc = "$Id: FGModel.cpp,v 1.30 2004/04/24 17:12:57 jberndt Exp $";
	}
}
