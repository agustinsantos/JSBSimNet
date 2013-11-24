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
	using System.Text.RegularExpressions;
	using System.Globalization;

	// Import log4net classes.
	using log4net;

	using CommonUtils.MathLib;
	using JSBSim.InputOutput;
	using JSBSim.Format;


	/// <summary>
	/// Base class for all engines.
	/// This base class contains methods and members common to all engines, such as
	/// logic to drain fuel from the appropriate tank, etc.
	/// This code is based on FGEngine written by Jon S. Berndt
	/// </summary>
	public class Engine
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

        public enum EngineType { Unknown, Rocket, Piston, Turbine, Turboprop, Electric };

        public Engine(FDMExecutive exec, XmlElement parent, XmlElement element, int engine_number)
		{
            this.engineNumber = engine_number;
            Vector3D location, orientation;

            FDMExec = exec;

            Name = element.GetAttribute("name");

            // Find and set engine location
            XmlElement tmpElem = parent.GetElementsByTagName("location")[0] as XmlElement;
            if (tmpElem != null)
            {
                location = FormatHelper.TripletConvertTo(tmpElem, "IN");
            }
            else
            {
                if (log.IsErrorEnabled)
                    log.Error("No engine location found for this engine.");
                throw new Exception("No engine location found for this engine.");
            }

            tmpElem = parent.GetElementsByTagName("orient")[0] as XmlElement;
            if (tmpElem != null)
            {
                orientation = FormatHelper.TripletConvertTo(tmpElem, "IN");
            }
            else
            {
                if (log.IsErrorEnabled)
                    log.Error("No engine orientation found for this engine.");
                throw new Exception("No engine orientation found for this engine.");
            }

            SetPlacement(location, orientation);

            // Load thruster
            tmpElem = parent.GetElementsByTagName("thruster")[0] as XmlElement;
            if (tmpElem != null)
            {
                LoadThruster(tmpElem);
            }
            else
            {
                if (log.IsErrorEnabled)
                    log.Error("No thruster definition supplied with engine definition.");
                throw new Exception("No thruster definition supplied with engine definition.");
            }

            // Load feed tank[s] references
            XmlNodeList feedNodes = parent.GetElementsByTagName("feed");
            if (feedNodes.Count > 0 )
            {
                foreach (XmlElement elem in feedNodes)
                {
                    AddFeedTank((int)FormatHelper.ValueAsNumber(elem));
                }
            }
            else
            {
                if (log.IsErrorEnabled)
                    log.Error("No feed tank specified in engine definition.");
                throw new Exception("No feed tank specified in engine definition.");
            }

			if (log.IsDebugEnabled)
				log.Debug("Instantiated: Engine.");

		}


		public EngineType      GetEngineType() { return engineType; }

		public virtual string Name
		{
			get {return name; }
			set { name = value;}
		}

        /* OBSOLETE ??
		public string GetThrusterFileName() {return thrusterFileName;}

		public string EngineFileName
		{
			get {return engineFileName;}
			set {engineFileName = value;}
		}
        */

		// Engine controls
		public virtual double  GetThrottleMin() { return minThrottle; }
		public virtual double  GetThrottleMax() { return maxThrottle; }
		public virtual double  GetThrottle() { return throttle; }
		public virtual double  GetMixture() { return mixture; }

		public virtual bool	Starter
		{
			get { return starter; }
			set { starter = value; }
		}

		public virtual double getFuelFlow_gph () {return fuelFlow_gph;}
		public virtual double getFuelFlow_pph () {return fuelFlow_pph;}
		public virtual double GetThrust() { return thrust; }

		/// <summary>
		/// TODO make IsStarved, isRunning, isCranking ???
		/// </summary>
		/// <returns></returns>
		public virtual bool   GetStarved() { return starved; }
		public virtual void SetStarved()    { starved = true; }

		public virtual bool   GetRunning() { return running; }
		public virtual void SetRunning(bool bb) { running=bb; }
		public virtual bool   GetCranking() { return cranking; }

		public virtual void SetStarved(bool tt) { starved = tt; }

		public virtual void AddFeedTank(int tkID) { sourceTanks.Add(tkID); }
		public virtual void SetFuelFreeze(bool f) { fuelFreeze = f; }


		/// <summary>
		/// Calculates the thrust of the engine, and other engine functions.
		/// </summary>
		/// <returns>Thrust in pounds</returns>
		public virtual double Calculate() {return 0.0;}

		/// <summary>
		/// Reduces the fuel in the active tanks by the amount required.
		///	This function should be called from within the
		///	derived class' Calculate() function before any other calculations are
		///	done. This base class method removes fuel from the fuel tanks as
		///	appropriate, and sets the starved flag if necessary.
		/// </summary>
		public virtual void ConsumeFuel()
		{
			if (fuelFreeze) return;
			double fshortage, oshortage, tanksWithFuel;
			Tank tank;

			if (trimMode) return;
			fshortage = oshortage = tanksWithFuel = 0.0;

			// count how many assigned tanks have fuel
			for (int i=0; i<sourceTanks.Count; i++) 
			{
				tank = FDMExec.Propulsion.GetTank((int)sourceTanks[i]);
				if (tank.Contents > 0.0) 
				{
					++tanksWithFuel;
				}
			}
			if (tanksWithFuel == 0) return;

			for (int i=0; i<sourceTanks.Count; i++) 
			{
				tank = FDMExec.Propulsion.GetTank((int)sourceTanks[i]);
				if (tank.TankType == Tank.EnumTankType.Fuel) 
				{
					fshortage += tank.Drain(CalcFuelNeed()/tanksWithFuel);
				} 
				else 
				{
					oshortage += tank.Drain(CalcOxidizerNeed()/tanksWithFuel);
				}
			}

			if (fshortage < 0.00 || oshortage < 0.00) starved = true;
			else starved = false;
		}

		/// <summary>
		/// The fuel need is calculated based on power levels and flow rate for that
		/// power level. It is also turned from a rate into an actual amount (pounds)
		/// by multiplying it by the delta T and the rate.
		/// </summary>
		/// <returns>Total fuel requirement for this engine in pounds. </returns>
		public virtual double CalcFuelNeed()
		{
			fuelNeed = slFuelFlowMax*pctPower*FDMExec.State.DeltaTime*FDMExec.Propulsion.Rate;
			return fuelNeed;
		}

		/// <summary>
		/// The oxidizer need is calculated based on power levels and flow rate for that
		///	power level. It is also turned from a rate into an actual amount (pounds)
		///	by multiplying it by the delta T and the rate.
		/// </summary>
		/// <returns>Total oxidizer requirement for this engine in pounds.</returns>
		public virtual double CalcOxidizerNeed()
		{
			oxidizerNeed = slOxiFlowMax*pctPower*FDMExec.State.DeltaTime*FDMExec.Propulsion.Rate;
			return oxidizerNeed;
		}


		/// Sets engine placement information
		public virtual void SetPlacement(Vector3D location, Vector3D orientation)
		{
			X = location.X;
            Y = location.Y;
            Z = location.Z;
			enginePitch = orientation.Pitch;
			engineYaw = orientation.Yaw;
		}

		public double GetPlacementX() {return X;}
		public double GetPlacementY() {return Y;}
		public double GetPlacementZ() {return Z;}
		public double GetPitch() {return enginePitch;}
		public double GetYaw() {return engineYaw;}

		public virtual double GetPowerAvailable() {return 0.0;}

		public virtual bool GetTrimMode() {return trimMode;}
		public virtual void SetTrimMode(bool state) {trimMode = state;}

		public virtual Vector3D GetBodyForces() { return thruster.GetBodyForces();	}

		public virtual Vector3D GetMoments() { return thruster.GetMoments(); }

        public void LoadThruster(XmlElement element)
        {
            XmlElement thruster_element;
            string thrusterFilename = element.GetAttribute("file");
            if (thrusterFilename.Length == 0)
            {
                if (log.IsErrorEnabled)
                    log.Error("Thruster file does not appear to be defined");
                throw new Exception("Thruster file does not appear to be defined");
            }
            else
            {
                string file = FDMExec.EnginePath + "/" + thrusterFilename + ".xml";
                FileInfo fi1 = new FileInfo(file);
                if (!fi1.Exists)
                {
                    file = FDMExec.AircraftPath + "/" + "Engines" + "/" + thrusterFilename + ".xml";
                    fi1 = new FileInfo(file);
                }
                if (!fi1.Exists)
                {
                    if (log.IsErrorEnabled)
                        log.Error("Could not open " + FDMExec.ModelName + " file: " + file);

                    throw new Exception("Could not open thruster file" + thrusterFilename); ;
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
                    thruster_element = doc.DocumentElement;

                }
            }

            string thrType = thruster_element.Name;

            if (thrType.Equals("propeller"))
            {
                thruster = new Propeller(FDMExec, element, thruster_element, engineNumber);
            }
            else if (thrType.Equals("nozzle"))
            {
                thruster = new Nozzle(FDMExec, element, thruster_element, engineNumber);
            }
            else if (thrType.Equals("direct"))
            {
                thruster = new Thruster(FDMExec, element, thruster_element, engineNumber);
            }

            thruster.SetdeltaT(FDMExec.State.DeltaTime * FDMExec.Propulsion.Rate);

        }


		public Thruster GetThruster() {return thruster;}


		public virtual string GetEngineLabels(string delimeter)
		{
			return null;
		}

        public virtual string GetEngineValues(string format, IFormatProvider provider, string delimeter)		
		{
			return null;
		}

		public int GetNumSourceTanks() {return sourceTanks.Count;}
		public int GetSourceTank(int t) {return (int)sourceTanks[t];}

		protected PropertyManager PropertyManager;
		protected string name;
		protected int   engineNumber;
		protected EngineType engineType = EngineType.Unknown;
		protected double X = 0.0, Y = 0.0, Z= 0.0;
		protected double enginePitch = 0.0;
		protected double engineYaw = 0.0;
		protected double slFuelFlowMax = 0.0;
		protected double slOxiFlowMax = 0.0;
		protected double maxThrottle = 1.0;
		protected double minThrottle = 0.0;

		protected double thrust = 0.0;
		protected double throttle = 0.0;
		protected double mixture = 1.0;
		protected double fuelNeed = 0.0;
		protected double oxidizerNeed = 0.0;
		protected double pctPower = 0.0;
		protected bool  starter = false;
		protected bool  starved = false;
		protected bool  running = false;
		protected bool  cranking = false;
		protected bool  trimMode = false;
		protected bool  fuelFreeze = false;

		protected double fuelFlow_gph = 0.0;
		protected double fuelFlow_pph = 0.0;

		protected FDMExecutive      FDMExec;
		protected Thruster			thruster;

        private List<int> sourceTanks = new List<int>(); //vector <int>
		
		private const string IdSrc = "$Id: FGEngine.cpp,v 1.76 2005/01/27 12:23:10 jberndt Exp $";
	}
}
