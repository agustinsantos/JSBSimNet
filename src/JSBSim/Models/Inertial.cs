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

	using CommonUtils.MathLib;

	/// <summary>
	///  Models inertial forces (e.g. centripetal and coriolis accelerations).
	/// </summary>
	public class Inertial : Model
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

		public Inertial(FDMExecutive fdmex) : base(fdmex)
		{
			Name = "Inertial";

			// Defaults
			gAccelReference = Constants.GM/(radiusReference*radiusReference);
			gAccel          = Constants.GM/(radiusReference*radiusReference);

			if (log.IsDebugEnabled)
				log.Debug("Instantiated: Inertial.");
		}

		public override bool Run(bool Holding)
		{
			// Fast return if we have nothing to do ...
            if (InternalRun()) return true;
            if (FDMExec.Holding()) return false; // if paused don't execute

			// Gravitation accel
			double r = FDMExec.Propagate.Radius;
			gAccel = GetGAccel(r);

			return false;
		}

		public bool LoadInertial(System.Xml.XmlElement element)
		{
			return false;
			//TODO
		}

		public double SLgravity() {return gAccelReference;}
		public double Gravity {get {return gAccel;}}
		public double Omega {get {return rotationRate;}}
		public double GetGAccel(double r) { return Constants.GM/(r*r); }
		public double RefRadius() {return radiusReference;}


		private double gAccel;
		private double gAccelReference;

		//TODO Move these constants to Constants class ??
		private double radiusReference = 20925650.00;
		private double rotationRate = 0.00007272205217;
		
	}
}
