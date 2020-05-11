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
	using System.Text.RegularExpressions;

	using CommonUtils.MathLib;

	using JSBSim.Format;
	using JSBSim.Script;

	// Import log4net classes.
	using log4net;

	/// <summary>
	/// Summary description for GroundReactions.
	/// </summary>
	public class GroundReactions : Model
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

		public GroundReactions(FDMExecutive exec) : base(exec)
		{
			Name = "FGGroundReactions";

			if (log.IsDebugEnabled)
				log.Debug("Instantiated: GroundReactions.");
		}

        public override bool Run(bool Holding)
        {
            if (InternalRun()) return true;
            if (FDMExec.Holding()) return false; // if paused don't execute

            vForces = Vector3D.Zero;
            vMoments = Vector3D.Zero;

            // Only execute gear force code below 300 feet
            if (FDMExec.Propagate.DistanceAGL < 300.0)
            {
                // Sum forces and moments for all gear, here.
                // Some optimizations may be made here - or rather in the gear code itself.
                // The gear ::Run() method is called several times - once for each gear.
                // Perhaps there is some commonality for things which only need to be
                // calculated once.
                foreach (LGear gear in lGear)
                {
                    vForces += gear.Force();
                    vMoments += gear.Moment();
                }

            }
            else
            {
                // Crash Routine
            }

            return false;
        }


		[ScriptAttribute("forces/fbx-gear-lbs", "TODO comments")]
		public double ForcesX {get {return vForces.X;}}
		
		[ScriptAttribute("forces/fby-gear-lbs", "TODO comments")]
		public double ForcesY {get {return vForces.Y;}}
		
		[ScriptAttribute("forces/fbz-gear-lbs", "TODO comments")]
		public double ForcesZ {get {return vForces.Z;}}

		public Vector3D GetForces() {return vForces;}
		public double GetForces(int idx) {return vForces[idx];}
		
		[ScriptAttribute("moments/l-gear-lbsft", "TODO comments")]
		public double MomentsL { get {return vMoments.X;}}

		[ScriptAttribute("moments/m-gear-lbsft", "TODO comments")]
		public double MomentsM { get {return vMoments.Y;}}

		[ScriptAttribute("moments/n-gear-lbsft", "TODO comments")]
		public double MomentsN { get {return vMoments.Z;}}

		public Vector3D GetMoments() {return vMoments;}
		public double GetMoments(int idx) {return vMoments[idx];}

		public string GetGroundReactionStrings(string delimeter)
		{
            StringBuilder buf = new StringBuilder(1000);

			foreach (LGear gear in lGear) 
			{
				string name = gear.GetName();
                buf.Append(name + "_WOW" + delimeter);
				buf.Append(name + "_stroke" + delimeter);
				buf.Append(name + "_strokeVel" + delimeter);
				buf.Append(name + "_CompressForce" + delimeter);
				buf.Append(name + "_WhlSideForce" + delimeter);
				buf.Append(name + "_WhlVelVecX" + delimeter);
				buf.Append(name + "_WhlVelVecY" + delimeter);
				buf.Append(name + "_WhlRollForce" + delimeter);
				buf.Append(name + "_BodyXForce" + delimeter);
				buf.Append(name + "_BodyYForce" + delimeter);
                buf.Append(name + "_WhlSlipDegrees" + delimeter);
			}

			buf.Append("TotalGearForce_X" + delimeter);
			buf.Append("TotalGearForce_Y" + delimeter);
			buf.Append("TotalGearForce_Z" + delimeter);
			buf.Append("TotalGearMoment_L" + delimeter);
			buf.Append("TotalGearMoment_M" + delimeter);
            buf.Append("TotalGearMoment_N");

			return buf.ToString();
		}

        public string GetGroundReactionValues(string format, IFormatProvider provider, string delimeter)
		{
            StringBuilder buf = new StringBuilder(1000);

			foreach (LGear gear in lGear) 
			{
				buf.Append((gear.GetWOW() ? "1, " : "0, "));
                buf.Append(gear.GetCompLen().ToString(format, provider) + delimeter);
                buf.Append(gear.GetCompVel().ToString(format, provider) + delimeter);
                buf.Append(gear.GetCompForce().ToString(format, provider) + delimeter);
                buf.Append(gear.GetWheelVel().X.ToString(format, provider) + delimeter);
                buf.Append(gear.GetWheelVel().Y.ToString(format, provider) + delimeter);
                buf.Append(gear.GetWheelSideForce().ToString(format, provider) + delimeter);
                buf.Append(gear.GetWheelRollForce().ToString(format, provider) + delimeter);
                buf.Append(gear.GetBodyXForce().ToString(format, provider) + delimeter);
                buf.Append(gear.GetBodyYForce().ToString(format, provider) + delimeter);
                buf.Append(gear.GetWheelSlipAngle().ToString(format, provider) + delimeter);
			}

            buf.Append(vForces.X.ToString(format, provider) + delimeter);
            buf.Append(vForces.Y.ToString(format, provider) + delimeter);
            buf.Append(vForces.Z.ToString(format, provider) + delimeter);
            buf.Append(vMoments.X.ToString(format, provider) + delimeter);
            buf.Append(vMoments.Y.ToString(format, provider) + delimeter);
            buf.Append(vMoments.Z.ToString(format, provider));

			return buf.ToString();
		}
  
		[ScriptAttribute("gear/num-units", "TODO comments")]
		public int NumGearUnits { get { return lGear.Count; }}


		/// <summary>
		/// Gets a gear instance
		/// </summary>
		/// <param name="gear">index of gear instance</param>
		/// <returns>the LGear instance of the gear unit requested</returns>
		public LGear GetGearUnit(int gear) { return (LGear)lGear[gear]; }

		public virtual void Load(XmlReader reader)
		{
			while (reader.Read())
			{
				switch (reader.NodeType)
				{
					case XmlNodeType.Text:
						StringReader sr = new StringReader(reader.Value);
						string str = sr.ReadLine();
						while (str != null)
						{
							str = str.Trim();
							if (str.Length > 1)
							{
								int num = lGear.Count;
								lGear.Add(new LGear(str, FDMExec, num));
								FDMExec.FlightControlSystem.AddGear();
							}
							str = sr.ReadLine();
						}
						break;
					case XmlNodeType.EndElement:
						return;
				}
			}
		}

		public void Load(XmlElement element)
		{
			foreach (XmlNode currentNode in element.ChildNodes)
			{
				if (currentNode.NodeType == XmlNodeType.Element) 
				{
					XmlElement currentElement = (XmlElement)currentNode;

					if (currentElement.LocalName.Equals("contact"))
					{
						int num = lGear.Count;
						lGear.Add(new LGear(currentElement, FDMExec, num)); // make the FCS aware of the landing gear
						FDMExec.FlightControlSystem.AddGear();
					}
				}
			}
		}

        public bool GetWOW()
        {
            bool result = false;
            foreach (LGear gear in lGear)
            {
                if (gear.IsBogey() && gear.GetWOW())
                {
                    result = true;
                    break;
                }
            }
            return result;
        }

        private List<LGear> lGear = new List<LGear>();
		private Vector3D vForces;
		private Vector3D vMoments;
		private Vector3D vMaxStaticGrip;
		private Vector3D vMaxMomentResist;

		private const string IdSrc = "$Id: FGGroundReactions.cpp,v 1.41 2004/11/02 05:19:42 jberndt Exp $";

	}
}
