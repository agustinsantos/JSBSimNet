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
	using System.Collections;
    using System.Collections.Generic;
	using System.Xml;
	using System.IO;
	using System.Text;
	using System.Text.RegularExpressions;

	// Import log4net classes.
	using log4net;

	using CommonUtils.MathLib;
	using JSBSim.Format;
	using JSBSim.Script;
    using JSBSim.InputOutput;


	/// <summary>
	/// Propulsion management class.
	/// The Propulsion class is the container for the entire propulsion system, which is
	/// comprised of engines, and tanks. Once the Propulsion class gets the config file,
	/// it reads in information which is specific to a type of engine. Then:
	/// 
	/// -# The appropriate engine type instance is created
	/// -# At least one tank object is created, and is linked to an engine.
	/// 
	/// At Run time each engines Calculate() method is called.
	/// </summary>
	public class Propulsion : Model
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
		
		/// Constructor
		public Propulsion(FDMExecutive exec) : base(exec)
		{
			Name = "Propulsion";

			activeEngine = -1; // -1: ALL, 0: Engine 1, 1: Engine 2 ...
			tankJ  = Matrix3D.Zero;
		}

		/// <summary>
		/// Executes the propulsion model.
		/// The initial plan for the FGPropulsion class calls for Run() to be executed,
		/// calculating the power available from the engine.
		/// 
		/// [Note: Should we be checking the Starved flag here?]
		/// </summary>
		/// <returns></returns>
		public override bool Run()
		{
            if (InternalRun()) return true;
            if (FDMExec.Holding()) return false; // if paused don't execute
            

			double dt = FDMExec.State.DeltaTime;

			vForces		= Vector3D.Zero;
			vMoments	= Vector3D.Zero;

			for (int i =0; i < engines.Count; i++) 
			{
				engines[i].Calculate();
				vForces  += engines[i].GetBodyForces();  // sum body frame forces
				vMoments += engines[i].GetMoments();     // sum body frame moments
			}

            totalFuelQuantity = 0.0;
            for (int i = 0; i < tanks.Count; i++)
            {
                tanks[i].Calculate(dt * rate);
                if (tanks[i].TankType == Tank.EnumTankType.Fuel)
                {
                    totalFuelQuantity += tanks[i].Contents;
                }

            }

			if (refuel) DoRefuel( dt * rate );
  
			return false;
		}


		/// Retrieves the number of engines defined for the aircraft.
		public int GetNumEngines() {return engines.Count;}


		/// <summary>
		/// Retrieves an engine object pointer from the list of engines.
		/// </summary>
		/// <param name="index">the engine index within the vector container</param>
		/// <returns>the specific engine, or zero if no such engine is
		/// available</returns>
		public Engine GetEngine(int index) 
		{
			if (index <= engines.Count-1) 
				return (Engine)engines[index];
			else
				return null;      
		}

		/// Retrieves the number of tanks defined for the aircraft.
		public int GetNumTanks() {return tanks.Count;}

		/// <summary>
		/// Retrieves a tank object pointer from the list of tanks.
		/// </summary>
		/// <param name="index">the tank index within the vector container</param>
		/// <returns>the specific tank, or zero if no such tank is available</returns>
		public Tank GetTank(int index) 
		{
			if (index <= tanks.Count-1)
				return (Tank)tanks[index];
			else
				return null;
		}

		/// <summary>
		/// Returns the number of fuel tanks currently actively supplying fuel
		/// </summary>
		/// <returns></returns>
		public int GetnumSelectedFuelTanks() {return numSelectedFuelTanks;}

		/// <summary>
		/// Returns the number of oxidizer tanks currently actively supplying oxidizer
		/// </summary>
		/// <returns></returns>
		public int GetnumSelectedOxiTanks() {return numSelectedOxiTanks;}


		/// <summary>
		/// Loops the engines until thrust output steady (used for trimming)
		/// </summary>
		/// <returns></returns>
		public bool GetSteadyState()
		{
			double currentThrust = 0, lastThrust=-1;
			int steady_count,j=0;
			bool steady=false;

			vForces = Vector3D.Zero;
			vMoments = Vector3D.Zero;

			if (!InternalRun()) 
			{
				foreach (Engine engine in engines) 
				{
					engine.SetTrimMode(true);
					steady=false;
					steady_count=0;
					while (!steady && j < 6000) 
					{
						engine.Calculate();
						lastThrust = currentThrust;
						currentThrust = engine.GetThrust();
						if (Math.Abs(lastThrust-currentThrust) < 0.0001) 
						{
							steady_count++;
							if (steady_count > 120) { steady=true; }
						} 
						else 
						{
							steady_count=0;
						}
						j++;
					}
					vForces  += engine.GetBodyForces();  // sum body frame forces
					vMoments += engine.GetMoments();     // sum body frame moments
					engine.SetTrimMode(false);
				}

				return false;
			} 
			else 
			{
				return true;
			}
		}


		/// <summary>
		/// starts the engines in IC mode (dt=0).  All engine-specific setup must
		/// be done before calling this (i.e. magnetos, starter engage, etc.) 
		/// </summary>
		/// <returns></returns>
		public bool ICEngineStart()
		{
			int j;

			vForces		= Vector3D.Zero;
			vMoments	= Vector3D.Zero;

			foreach (Engine engine in engines) 
			{
				engine.SetTrimMode(true);
				j=0;
				while (!engine.GetRunning() && j < 2000) 
				{
					engine.Calculate();
					j++;
				}
				vForces  += engine.GetBodyForces();  // sum body frame forces
				vMoments += engine.GetMoments();     // sum body frame moments
				engine.SetTrimMode(false);
			}
			return true;
		}


		public string GetPropulsionStrings(string delimeter)
		{
			string propulsionStrings = "";
			bool firstime = true;

			foreach (Engine engine in engines) 
			{
				if (firstime)  firstime = false;
				else           propulsionStrings += delimeter;

				propulsionStrings += engine.GetEngineLabels(delimeter);
			}

            for (int i = 0; i < tanks.Count; i++)
            {
                if (tanks[i].TankType == Tank.EnumTankType.Fuel)
					propulsionStrings += delimeter + "Fuel Tank " + i;
                else if (tanks[i].TankType == Tank.EnumTankType.Oxidizer)
					propulsionStrings += delimeter + "Oxidizer Tank " + i;
			}

			return propulsionStrings;
		}

		public string GetPropulsionValues(string format, IFormatProvider provider, string delimeter)
		{
			string propulsionValues = "";
			bool firstime = true;

			foreach (Engine engine in engines) 
			{
				if (firstime)  firstime = false;
				else           propulsionValues += delimeter;

				propulsionValues += engine.GetEngineValues(format, provider, delimeter);
			}
			foreach (Tank tank in tanks) 
			{
                propulsionValues += delimeter + tank.Contents.ToString(format, provider);
			}

			return propulsionValues;
		}


		public Vector3D GetForces()  {return vForces; }
		public double GetForces(int n) { return vForces[n];}
		public Vector3D GetMoments() {return vMoments;}
		public double GetMoments(int n) {return vMoments[n];}

		public bool GetRefuel() {return refuel;}
		public void SetRefuel(bool setting) {refuel = setting;} 

		public double Transfer(int source, int target, double amount)
		{
			double shortage, overage;

			if (source == -1) 
			{
				shortage = 0.0;
			} 
			else 
			{
				shortage = tanks[source].Drain(amount);
			}
			if (target == -1) 
			{
				overage = 0.0;
			} 
			else 
			{
				overage = tanks[target].Fill(amount - shortage);
			}
			return overage;
		}

		public void DoRefuel(double time_slice)
		{
			double fillrate = 100 * time_slice;   // 100 lbs/sec = 6000 lbs/min
			int TanksNotFull = 0;

			foreach (Tank tank in tanks) 
			{
				if (tank.PctFull < 99.99)
					++TanksNotFull;
			}

			if (TanksNotFull != 0) 
			{
				int i=0;

				foreach (Tank tank in tanks) 
				{
					if (tank.PctFull < 99.99)
						Transfer(-1, i, fillrate/TanksNotFull);
					i++;
				}
			}
		}

		public Vector3D GetTanksMoment()
		{
			vXYZtank_arm = Vector3D.Zero;
			foreach (Tank iTank in tanks) 
			{
				vXYZtank_arm.X += iTank.GetXYZ().X*iTank.Contents;
				vXYZtank_arm.Y += iTank.GetXYZ().Y*iTank.Contents;
				vXYZtank_arm.Z += iTank.GetXYZ().Z*iTank.Contents;
			}
			return vXYZtank_arm;
		}

		public double GetTanksWeight()
		{
			double Tw = 0.0;

			foreach (Tank iTank in tanks) 
			{
				Tw += iTank.Contents;
			}
			return Tw;
		}


		public bool GetFuelFreeze() {return fuel_freeze;}

        [ScriptAttribute("propulsion/magneto_cmd", "TODO comments")]
        public int Magnetos
        {
            set { SetMagnetos(value); }
        }

		public void SetMagnetos(int setting)
		{
			if (activeEngine < 0) 
			{
				for (int i=0; i<engines.Count; i++) 
				{
                    // ToDo: first need to make sure the engine Type is really appropriate:
                    //   do a check to see if it is of type Piston. This should be done for
                    //   all of this kind of possibly across-the-board settings.
					((Piston)engines[i]).SetMagnetos(setting);
				}
			} 
			else 
			{
				((Piston)engines[activeEngine]).SetMagnetos(setting);
			}
		}

        [ScriptAttribute("propulsion/starter_cmd", "TODO comments")]
        public int Starter
        {
            set { SetStarter(value); }
        }

		public void SetStarter(int setting)
		{
			if (activeEngine < 0) 
			{
				foreach (Engine engine in engines) 
				{
					if (setting == 0)
						engine.Starter = false;
					else
						engine.Starter = true;
				}
			} 
			else 
			{
				if (setting == 0)
					engines[activeEngine].Starter = false;
				else
					engines[activeEngine].Starter = true;
			}
		}

        [ScriptAttribute("propulsion/cutoff_cmd", "TODO comments")]
        public int Cutoff
        {
            set { SetCutoff(value); }
        }

		public void SetCutoff(int setting)
		{
			if (activeEngine < 0) 
			{
				foreach (Engine engine in engines) 
				{
					if (setting == 0)
						((Turbine)engine).SetCutoff(false);
					else
						((Turbine)engine).SetCutoff(true);
				}
			} 
			else 
			{
				if (setting == 0)
					((Turbine)engines[activeEngine]).SetCutoff(false);
				else
					((Turbine)engines[activeEngine]).SetCutoff(true);
			}
		}

		[ScriptAttribute("propulsion/active_engine", "TODO comments")]
		public int ActiveEngine
		{
			get 
			{
				return activeEngine;
			}
			set 
			{
				if (value >= engines.Count || value < 0)
					activeEngine = -1;
				else
					activeEngine = value;
			}
		}

		public void SetFuelFreeze(bool f)
		{
			fuel_freeze = f;
			foreach (Engine engine in engines)  
			{
				engine.SetFuelFreeze(f);
			}
		}

        [ScriptAttribute("propulsion/total-fuel-lbs", "TODO comments")]
        public double TotalFuelQuantity
        {
            get { return totalFuelQuantity; }
        }

		public Matrix3D CalculateTankInertias()
		{
			if (tanks.Count == 0)
                return tankJ;

            tankJ = Matrix3D.Zero;

			foreach (Tank iTank in tanks)
				tankJ += FDMExec.MassBalance.GetPointmassInertia( Constants.lbtoslug * iTank.Contents, iTank.GetXYZ() );

			return tankJ;
		}

        /// <summary>
		/// Loads the propulsion system (engine[s] and tank[s]).
		/// Characteristics of the propulsion system are read in from the config file.
		/// </summary>
		/// <param name="element">XML element that contains the engine information</param>
		public void  Load(XmlElement element)
		{
            bool throttleAdded = false;

			foreach (XmlNode currentNode in element.ChildNodes)
			{
				if (currentNode.NodeType == XmlNodeType.Element) 
				{
					XmlElement currentElement = (XmlElement)currentNode;

                    if (currentElement.LocalName.Equals("engine"))
                    {
                        string engine_filename = currentElement.GetAttribute("file");
                        if (engine_filename == null || engine_filename.Length == 0)
                        {
                            if (log.IsErrorEnabled)
                                log.Error("Engine definition did not supply an engine file.");
                            throw new Exception("Engine definition did not supply an engine file.");
                        }
                        else
                            throttleAdded = LoadEngine(currentElement, engine_filename);
                    }
                    else if (currentElement.LocalName.Equals("tank"))
                    {
                        //if (log.IsDebugEnabled)
                        //    log.Debug("Reading tank definition");
                        tanks.Add(new Tank(FDMExec, currentElement));
                        switch (tanks[numTanks].TankType)
                        {
                            case Tank.EnumTankType.Fuel:
                                numFuelTanks++;
                                break;
                            case Tank.EnumTankType.Oxidizer:
                                numOxiTanks++;
                                break;
                        }

                    }
				}
			}
            numSelectedFuelTanks = numFuelTanks;
            numSelectedOxiTanks = numOxiTanks;
			CalculateTankInertias();
			if (!throttleAdded) 
				this.FDMExec.FlightControlSystem.AddThrottle(); // need to have at least one throttle

		}

        private bool LoadEngine(XmlElement parent, string engineFileName)
        {
            XmlReader engineReader = FindEngineXmlReader(engineFileName);

            XmlDocument engineDoc  = new XmlDocument();
            // load the data into the document
            engineDoc.Load(engineReader);

            engineReader.Close();

            XmlElement element = engineDoc.DocumentElement;

            string engineType = element.LocalName;
            if (engineType.Equals("piston_engine"))
            {
                havePistonEngine = true;
                if (!isBound) Bind();
                engines.Add(new Piston(FDMExec, parent, element, numEngines));
            }
            else if (engineType.Equals("turbine_engine"))
            {
                haveTurbineEngine = true;
                if (!isBound) Bind();
                engines.Add(new Turbine(FDMExec, parent, element, numEngines));
            }
            else if (engineType.Equals("turboprop_engine"))
            {
                haveTurboPropEngine = true;
                if (!isBound) Bind();
                engines.Add(new TurboProp(FDMExec, parent, element, numEngines));
            }
            else if (engineType.Equals("rocket_engine"))
            {
                haveRocketEngine = true;
                if (!isBound) Bind();
                engines.Add(new Rocket(FDMExec, parent, element, numEngines));
            }
            else if (engineType.Equals("electric_engine"))
            {
                haveElectricEngine = true;
                if (!isBound) Bind();
                engines.Add(new Electric(FDMExec, parent, element, numEngines));
            }
            else
            {
                if (log.IsErrorEnabled)
                    log.Error("Unrecognized engine type: " + engineType + " found in config file.");

                throw new Exception("Unrecognized engine type: " + engineType + " found in config file.");
            }

            this.FDMExec.FlightControlSystem.AddThrottle();
            numEngines++;

            return true;
        }

        public override void Bind()
        {
            if (isBound) return;

            base.Bind();
            PropertyManager propertyManager = FDMExec.PropertyManager;

            isBound = true;

            if (haveTurbineEngine)
            {
                /* TODO TODO TODO
                propertyManager.Tie("propulsion/starter_cmd", null, this.SetStarter);
                propertyManager.Tie("propulsion/cutoff_cmd", null, this.SetCutoff);
                TODO TODO TODO */
            }

            if (havePistonEngine)
            {
                /* TODO TODO TODO
                propertyManager.Tie("propulsion/starter_cmd", null, this.SetStarter);
                propertyManager.Tie("propulsion/magneto_cmd", null, this.SetMagnetos);
                TODO TODO TODO */
            }

            /* TODO TODO TODO
            propertyManager.Tie("propulsion/active_engine", this, (iPMF)&FGPropulsion::GetActiveEngine,
                 &FGPropulsion::SetActiveEngine, true);
            propertyManager.Tie("propulsion/total-fuel-lbs", this, &FGPropulsion::GetTotalFuelQuantity);
            TODO TODO TODO */
        }

        public override void Unbind()
        {
            base.Unbind();
        }


        protected XmlReader FindEngineXmlReader(string engineFileName)
        {
            string fullpath = FDMExec.EnginePath + "/" + engineFileName + ".xml";
            string localpath = FDMExec.AircraftPath + "/Engines/" + engineFileName + ".xml";

            // Look in the Aircraft/Engines directory first
            FileInfo fi1 = new FileInfo(localpath);
            XmlReader engineXmlReader = null;
            if (fi1.Exists)
            {
                try
                {

                    engineXmlReader = new XmlTextReader(localpath);
                    if (log.IsDebugEnabled)
                        log.Debug("Reading engine from file: " + localpath);
                }
                catch (Exception e)
                {
                    if (log.IsDebugEnabled)
                        log.Debug("Could not read engine config file: " + localpath + ", exception:" + e);
                }
            }

            if (engineXmlReader == null)
            {
                fi1 = new FileInfo(fullpath);
                if (fi1.Exists)
                {
                    try
                    {
                        engineXmlReader = new XmlTextReader(fullpath);
                        if (log.IsDebugEnabled)
                            log.Debug("Reading engine from file: " + fullpath);
                    }
                    catch (Exception e)
                    {
                        if (log.IsDebugEnabled)
                            log.Debug("Could not read engine config file: " + fullpath + ", exception:" + e);
                    }

                }
            }

            if (engineXmlReader == null)
            {
                if (log.IsErrorEnabled)
                    log.Error("Could not read engine config file: " + engineFileName + ".xml");

                throw new Exception("Could not read engine config file:" + engineFileName + ".xml");

            }
            else
                return engineXmlReader;
        }

        private List<Engine> engines = new List<Engine>(); //vector <FGEngine*> 
        private List<Tank> tanks = new List<Tank>(); //vector <FGTank*>
		private int numSelectedFuelTanks = 0;
		private int numSelectedOxiTanks = 0;
		private int numFuelTanks = 0;
		private int numOxiTanks = 0;
		private int numEngines = 0;
		private int numTanks = 0;
		private int activeEngine;
		private Vector3D vForces		= Vector3D.Zero;
		private Vector3D vMoments		= Vector3D.Zero;
		private Vector3D vTankXYZ		= Vector3D.Zero;
		private Vector3D vXYZtank_arm	= Vector3D.Zero;
		private Matrix3D tankJ;
		private bool refuel = false;
		private bool fuel_freeze= false;
        private double totalFuelQuantity;
        private static bool isBound = false;
		private bool havePistonEngine = false;
		private bool haveTurbineEngine = false;
		private bool haveRocketEngine = false;
		private bool haveElectricEngine = false;
        private bool haveTurboPropEngine = false;
	}
}
