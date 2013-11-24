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
	using CommonUtils.MathLib;
	using System.Xml;

	// Import log4net classes.
	using log4net;

	using JSBSim.InputOutput;
    using JSBSim.Format;

	/// <summary>
	/// Base class for specific thrusting devices such as propellers, nozzles, etc.
	/// @author Jon Berndt
	/// </summary>
	public class Thruster : Force
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
        
        public enum ThrusterType { Nozzle, Rotor, Propeller, Direct };

		public Thruster(FDMExecutive fdmex): base(fdmex)
		{
			thrusterType = ThrusterType.Direct;
			SetTransformType(Force.TransformType.Custom);

			engineNum = 0;
			propertyManager = FDMExec.PropertyManager;
		}

        public Thruster(FDMExecutive exec, XmlElement elementParent, XmlElement element, int num)
            : base(exec)
        {
            thrusterType = ThrusterType.Direct;
            SetTransformType(Force.TransformType.Custom);
            gearRatio = 1.0;

            engineNum = num;
            thrustCoeff = 0.0;
            reverserAngle = 0.0;
            propertyManager = FDMExec.PropertyManager;

            name = element.Name;

            // Determine the initial location and orientation of this thruster and load the
            // thruster with this information.

            XmlElement elementTmp = elementParent.GetElementsByTagName("location")[0] as XmlElement;
            Vector3D location = Vector3D.Zero;
            if (elementTmp != null)
                location = FormatHelper.TripletConvertTo(elementTmp, "IN");
            else if (log.IsErrorEnabled)
                log.Error("No thruster location found.");

            Vector3D orientation = Vector3D.Zero;
            elementTmp = elementParent.GetElementsByTagName("orient")[0] as XmlElement;
            if (elementTmp != null)
                orientation = FormatHelper.TripletConvertTo(elementTmp, "IN");
            else if (log.IsErrorEnabled)
                log.Error("No thruster orientation found.");

            SetLocation(location);
            SetAnglesToBody(orientation);
        }
	

		public virtual double Calculate(double tt) 
		{
			thrust = tt;
			vFn.X = thrust * Math.Cos(reverserAngle);
			return vFn.X;
		}

		public string Name
		{
			get { return name;}
			set {name = value;}
		}


		public virtual void SetRPM(double rpm) {}
		public virtual double GetRPM() { return 0.0; }

		public virtual double GetPowerRequired() {return 0.0;}
		public virtual void SetdeltaT(double dt) {deltaT = dt;}
		public double GetThrust() {return thrust;}
		public ThrusterType GetThrusterType() {return thrusterType;}
		public double GetGearRatio() {return gearRatio; }
		public virtual string GetThrusterLabels(int id, string delimeter)
		{
			string buf;

			buf = Name + "_Thrust[" + id + "]";

			return buf;
		}

		public virtual string GetThrusterValues(int id, string delimeter)
		{
			string buf;

			buf = thrust.ToString();

			return buf;
		}

		public double ReverserAngle
		{
			get {return reverserAngle;}
			set {reverserAngle = value;}
		}


		public  double ThrustCoefficient
		{
			get { return thrustCoeff; }
			set { thrustCoeff = value;}
		}

		protected ThrusterType thrusterType;
		protected string name;
		protected double thrust;
		protected double powerRequired;
		protected double deltaT;
		protected double gearRatio;
		protected double thrustCoeff;
		protected double reverserAngle;
		protected int engineNum;
		protected PropertyManager propertyManager;


		private const string IdSrc = "$Id: FGThruster.cpp,v 1.29 2004/11/28 15:17:11 dpculp Exp $";
	}
}
