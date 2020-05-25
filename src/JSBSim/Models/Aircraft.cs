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
	using System.IO;
	using System.Data;
	using System.Xml;
	using System.Text;
	using System.Text.RegularExpressions;
	using System.Globalization;

	// Import log4net classes.
	using log4net;

	using JSBSim.Script;
	using CommonUtils.MathLib;
	using JSBSim.Format;

	/// <summary>
	/// Encapsulates an Aircraft and its systems.
	/// Owns all the parts (other classes) which make up this aircraft. This includes
	/// the Engines, Tanks, Propellers, Nozzles, Aerodynamic and Mass properties,
	/// landing gear, etc. These constituent parts may actually run as separate
	/// JSBSim models themselves, but the responsibility for initializing them and
	/// for retrieving their force and moment contributions falls to FGAircraft.
	/// 
	/// This code is based on FGAircraft written by  Jon S. Berndt
	/// 
	/// @see Cooke, Zyda, Pratt, and McGhee, "NPSNET: Flight Simulation Dynamic Modeling
	///    Using Quaternions", Presence, Vol. 1, No. 4, pp. 404-420  Naval Postgraduate
	///    School, January 1994
	/// @see D. M. Henderson, "Euler Angles, Quaternions, and Transformation Matrices",
	///  JSC 12960, July 1977
	/// @see Richard E. McFarland, "A Standard Kinematic Model for Flight Simulation at
	///  NASA-Ames", NASA CR-2497, January 1975
	/// @see Barnes W. McCormick, "Aerodynamics, Aeronautics, and Flight Mechanics",
	///  Wiley & Sons, 1979 ISBN 0-471-03032-5
	/// @see Bernard Etkin, "Dynamics of Flight, Stability and Control", Wiley & Sons,
	///  1982 ISBN 0-471-08936-2
	/// </summary>
	[Serializable]
	public class Aircraft : Model
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
		/// Constructor
		/// </summary>
		/// <param name="exec">Executive a reference to the parent executive object</param>
		public Aircraft(FDMExecutive exec) : base(exec)
		{
			Name = "Aircraft";

			if (log.IsDebugEnabled)
				log.Debug("Instantiated: Aircraft.");
		}

		/// <summary>
		/// Runs the Aircraft model; called by the Executive
		/// </summary>
		/// <returns>false if no error</returns>
        public override bool Run(bool Holding)
        {
#if TODO
            if (InternalRun()) return true;
            if (FDMExec.Holding()) return false; // if paused don't execute
            
            // if false then execute this Run()
            vForces = Vector3D.Zero; 
            vForces = FDMExec.Aerodynamics.Forces;
            vForces += FDMExec.Propulsion.GetForces();
            vForces += FDMExec.GroundReactions.GetForces();

            vMoments = Vector3D.Zero;
            vMoments = FDMExec.Aerodynamics.Moments;
            vMoments += FDMExec.Propulsion.GetMoments();
            vMoments += FDMExec.GroundReactions.GetMoments();

            vBodyAccel = vForces / FDMExec.MassBalance.Mass;

            vNcg = vBodyAccel / FDMExec.Inertial.Gravity;

            vNwcg = FDMExec.State.GetTb2s() * vNcg;
            vNwcg.Z = -1 * vNwcg.Z + 1;

            return false;
#endif
            throw new NotImplementedException("Pending upgrade to lastest version of JSBSIM");

        }

        /// <summary>
        /// Gets/Sets the aircraft name
        /// </summary>
        public string AircraftName
		{
			get { return aircraftName; }
			set { aircraftName = value;}
		}
  
		/// Gets the wing area
		[ScriptAttribute("metrics/Sw-sqft", "The wing area")]
		public double WingArea
		{
			get { return wingArea; }
		}

		/// Gets the wing span
		[ScriptAttribute("metrics/bw-ft", "The wing span")]
		public double WingSpan
		{
			get{ return wingSpan; }
		}

		[ScriptAttribute("metrics/iw-deg", "The wing incidence")]
		public double WingIncidence
		{
			get { return wingIncidence; }
		}

		/// Gets the average wing chord
		[ScriptAttribute("metrics/cbarw-ft", "The wing chord")]
		public double WingChord
		{
			get { return cbar; }
		}

		[ScriptAttribute("metrics/Sh-sqft", "The horizontal tail area")]
		public double HTailArea
		{
			get { return hTailArea; }
		}

		[ScriptAttribute("metrics/lh-ft", "The horizontaltail arm")]
		public double HTailArm
		{
			get { return hTailArm; }
		}

		[ScriptAttribute("metrics/Sv-sqft", "The vertical tail area")]
		public double VTailArea
		{
			get { return vTailArea; }
		}

		[ScriptAttribute("metrics/lv-ft", "The vertical tail arm")]
		public double VTailArm
		{
			get { return vTailArm; }
		}

		/// <summary>
		/// HTailArm / cbar
		/// </summary>
		[ScriptAttribute("metrics/lh-norm", "HTailArm / cbar")]
		public double Lbarh
		{
			get { return lbarh; }
		}

		/// <summary>
		///  VTailArm / cbar
		/// </summary>
		[ScriptAttribute("metrics/lv-norm", "VTailArm / cbar")]
		public double Lbarv
		{
			get { return lbarv; }
		}

		/// <summary>
		///  H. Tail Volume
		/// </summary>
		[ScriptAttribute("metrics/vbarh-norm", "H. Tail Volume")]
		public double Vbarh
		{
			get{ return vbarh; }
		}

		/// <summary>
		/// V. Tail Volume
		/// </summary>
		[ScriptAttribute("metrics/vbarv-norm", "V. Tail Volume")]
		public double Vbarv
		{
			get { return vbarv; }
		}

		[ScriptAttribute("moments/total-lbsft", "Moments")]
		public Vector3D Moments
		{
			get { return vMoments; }
		}

		public double GetMoments(int idx) { return vMoments[idx]; }

		[ScriptAttribute("moments/total-lbs", "Forces")]
		public Vector3D Forces
		{
			get{ return vForces; }
		}

		public double GetForces(int idx) { return vForces[idx]; }

		public Vector3D GetBodyAccel() { return vBodyAccel; }
		public double GetBodyAccel(int idx) { return vBodyAccel[idx]; }
		public Vector3D GetNcg   ()  { return vNcg; }
		public double GetNcg(int idx)  { return vNcg[idx]; }

		[ScriptAttribute("metrics/aero-rp-in", "TODO comments")]
		public Vector3D AeroRefPointXYZ
		{
				get { return vXYZrp; }
		}

		[ScriptAttribute("metrics/visualrefpoint-in", "TODO comments")]
		public Vector3D VisualRefPointXYZ
		{
			get { return vXYZvrp; }
		}

		[ScriptAttribute("metrics/eyepoint-in", "TODO comments")]
		public Vector3D EyepointXYZ
		{
			get{ return vXYZep; }
		}

		public double GetXYZrp(int idx) { return vXYZrp[idx]; }
		public double GetXYZvrp(int idx) { return vXYZvrp[idx]; }
		public double GetXYZep(int idx) { return vXYZep[idx]; }

		public double GetNlf() 
		{
			return -1*FDMExec.Aerodynamics.ForcesDragSideLift.Z/FDMExec.MassBalance.Weight;
		}

		public Vector3D GetNwcg() { return vNwcg; }

		private Vector3D vMoments;
		private Vector3D vForces;
		private Vector3D vXYZrp;
		private Vector3D vXYZvrp;
		private Vector3D vXYZep;
		private Vector3D vDXYZcg;
		private Vector3D vBodyAccel;
		private Vector3D vNcg;
		private Vector3D vNwcg;

		private double wingArea  = 0.0, wingSpan = 0.0, cbar, wingIncidence = 0.0;
		private double hTailArea = 0.0, vTailArea = 0.0, hTailArm = 0.0, vTailArm = 0.0;
		private double lbarh = 0.0,lbarv = 0.0,vbarh = 0.0,vbarv = 0.0;
		private string aircraftName;

		public void Load(XmlElement element)
		{
			foreach (XmlNode currentNode in element.ChildNodes)
			{
				if (currentNode.NodeType == XmlNodeType.Element) 
				{
					XmlElement currentElement = (XmlElement)currentNode;

                    if (currentElement.LocalName.Equals("wingarea"))
                    {
                        wingArea = FormatHelper.ValueAsNumberConvertTo(currentElement, "FT2");
                    }
                    else if (currentElement.LocalName.Equals("wingspan"))
                    {
                        wingSpan = FormatHelper.ValueAsNumberConvertTo(currentElement, "FT");
                    }
                    else if (currentElement.LocalName.Equals("chord"))
                    {
                        cbar = FormatHelper.ValueAsNumberConvertTo(currentElement, "FT");
                    }
                    else if (currentElement.LocalName.Equals("wing_incidence"))
                    {
                        wingIncidence = FormatHelper.ValueAsNumberConvertTo(currentElement, "DEG");
                    }
                    else if (currentElement.LocalName.Equals("htailarea"))
                    {
                        hTailArea = FormatHelper.ValueAsNumberConvertTo(currentElement, "FT2");
                    }
                    else if (currentElement.LocalName.Equals("htailarm"))
                    {
                        hTailArm = FormatHelper.ValueAsNumberConvertTo(currentElement, "FT");
                    }
                    else if (currentElement.LocalName.Equals("vtailarea"))
                    {
                        vTailArea = FormatHelper.ValueAsNumberConvertTo(currentElement, "FT2");
                    }
                    else if (currentElement.LocalName.Equals("vtailarm"))
                    {
                        vTailArm = FormatHelper.ValueAsNumberConvertTo(currentElement, "FT");
                    }
                    else if (currentElement.LocalName.Equals("location"))
                    {
                        // Find all LOCATION elements that descend from this METRICS branch of the
                        // config file. This would be CG location, eyepoint, etc.
                        string element_name = currentElement.GetAttribute("name");
                        if (element_name.Equals("AERORP"))
                            vXYZrp = FormatHelper.TripletConvertTo(currentElement, "IN");
                        else if (element_name.Equals("EYEPOINT"))
                            vXYZep = FormatHelper.TripletConvertTo(currentElement, "IN");
                        else if (element_name.Equals("VRP"))
                            vXYZvrp = FormatHelper.TripletConvertTo(currentElement, "IN");
                    }
				}
			}

			// calculate some derived parameters
			if (cbar != 0.0) 
			{
				lbarh = HTailArm/cbar;
				lbarv = VTailArm/cbar;
				if (wingArea != 0.0) 
				{
					vbarh = HTailArm*hTailArea / (cbar*wingArea);
					vbarv = VTailArm*vTailArea / (cbar*wingArea);
				}
			}
		}

		private void ParseVector (string str, out double x, out double y, out double z)
		{
			Match match = FormatHelper.vectorRegex.Match(str);
			if (match.Success) 
			{
				x = double.Parse(match.Groups["x"].Value, FormatHelper.numberFormatInfo);
				y = double.Parse(match.Groups["y"].Value, FormatHelper.numberFormatInfo);
				z = double.Parse(match.Groups["z"].Value, FormatHelper.numberFormatInfo);
			} 
			else
				throw new Exception("Can't parse a Vector from " + str);
		}

		private const string AC_WINGAREA = "AC_WINGAREA";
		private const string AC_WINGSPAN = "AC_WINGSPAN";
		private const string AC_WINGINCIDENCE = "AC_WINGINCIDENCE";
		private const string AC_CHORD = "AC_CHORD";
		private const string AC_HTAILAREA = "AC_HTAILAREA";
		private const string AC_HTAILARM = "AC_HTAILARM";
		private const string AC_VTAILAREA = "AC_VTAILAREA";
		private const string AC_VTAILARM = "AC_VTAILARM";
		private const string AC_IXX = "AC_IXX";
		private const string AC_IYY = "AC_IYY";
		private const string AC_IZZ = "AC_IZZ";
		private const string AC_IXY = "AC_IXY";
		private const string AC_IXZ = "AC_IXZ";
		private const string AC_IYZ = "AC_IYZ";
		private const string AC_EMPTYWT = "AC_EMPTYWT";
		private const string AC_CGLOC = "AC_CGLOC";
		private const string AC_EYEPTLOC = "AC_EYEPTLOC";
		private const string AC_AERORP = "AC_AERORP";
		private const string AC_VRP = "AC_VRP";
		private const string AC_POINTMASS = "AC_POINTMASS";




	}
}
