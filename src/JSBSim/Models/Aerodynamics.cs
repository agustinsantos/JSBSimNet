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
	using System.Text;
	using System.Text.RegularExpressions;
	using System.Globalization;
	using System.IO;


	// Import log4net classes.
	using log4net;

	using CommonUtils.MathLib;
	using JSBSim.Script;
	using JSBSim.Format;
	using JSBSim.MathValues;

	/// <summary>
	/// Encapsulates the aerodynamic calculations.
	/// This class owns and contains the list of coefficients that define the
	/// aerodynamic properties of this aircraft. Here also, such unique phenomena
	/// as ground effect and maximum lift curve tailoff are handled.
	/// @config
	/// <pre>
	/// \<AERODYNAMICS>
	///    \<AXIS NAME="{LIFT|DRAG|SIDE|ROLL|PITCH|YAW}">
	///      {Coefficient definitions}
	///    \</AXIS>
	///    {Additional axis definitions}
	/// \</AERODYNAMICS> </pre>
	/// 
	/// This code is based on FGAerodynamics written by Jon S. Berndt, Tony Peden
	/// </summary>
	[Serializable]
	public class Aerodynamics : Model
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
		/// <param name="exec">a reference to the parent executive object</param>
		public Aerodynamics(FDMExecutive exec) : base(exec)
		{
			Name = "Aerodynamics";

			AxisIdx["DRAG"]  = 0;
			AxisIdx["SIDE"]  = 1;
			AxisIdx["LIFT"]  = 2;
			AxisIdx["ROLL"]  = 3;
			AxisIdx["PITCH"] = 4;
			AxisIdx["YAW"]   = 5;

			///TODO review Coeff = new CoeffArray[6];

			impending_stall = stall_hyst = 0.0;
			alphaclmin = alphaclmax = 0.0;
			alphahystmin = alphahystmax = 0.0;
			clsq = lod = 0.0;
			alphaw = 0.0;
			bi2vel = ci2vel = 0.0;

			if (log.IsDebugEnabled)
				log.Debug("Instantiated: Aerodynamics.");
		}

		/// <summary>
		/// Runs the Aerodynamics model; called by the Executive
		/// </summary>
		/// <returns>false if no error</returns>
        public override bool Run()
        {
            double alpha, twovel;

            if (InternalRun()) return true;
            if (FDMExec.Holding()) return false; // if paused don't execute
            
            // calculate some oft-used quantities for speed
            twovel = 2 * FDMExec.Auxiliary.Vt;
            if (twovel != 0)
            {
                bi2vel = FDMExec.Aircraft.WingSpan / twovel;
                ci2vel = FDMExec.Aircraft.WingChord / twovel;
            }

            alphaw = FDMExec.Auxiliary.Getalpha() + FDMExec.Aircraft.WingIncidence;

            alpha = FDMExec.Auxiliary.Getalpha();
            qbar_area = FDMExec.Aircraft.WingArea * FDMExec.Auxiliary.Qbar;

            if (alphaclmax != 0)
            {
                if (alpha > 0.85 * alphaclmax)
                {
                    impending_stall = 10 * (alpha / alphaclmax - 0.85);
                }
                else
                {
                    impending_stall = 0;
                }
            }

            if (alphahystmax != 0.0 && alphahystmin != 0.0)
            {
                if (alpha > alphahystmax)
                {
                    stall_hyst = 1;
                }
                else if (alpha < alphahystmin)
                {
                    stall_hyst = 0;
                }
            }

            vLastFs = vFs;
            vFs = Vector3D.Zero;

            // Tell the variable functions to cache their values, so while the aerodynamic
            // functions are being calculated for each axis, these functions do not get
            // calculated each time, but instead use the values that have already
            // been calculated for this frame.
            for (int i = 0; i < variables.Count; i++) variables[i].CacheValue(true);
            
            for (int axis_ctr = 0; axis_ctr < 3; axis_ctr++)
            {
                if (Coeff[axis_ctr] != null)
                {
                    foreach (Function func in Coeff[axis_ctr])
                    {
                        vFs[axis_ctr] += func.GetValue();
                    }
                }
            }

            // calculate lift coefficient squared
            if (FDMExec.Auxiliary.Qbar > 0)
            {
                clsq = vFs[(int)StabilityAxisForces.eLift] / (FDMExec.Aircraft.WingArea * FDMExec.Auxiliary.Qbar);
                clsq *= clsq;
            }
            if (vFs[(int)StabilityAxisForces.eDrag] > 0)
            {
                lod = vFs[(int)StabilityAxisForces.eLift] / vFs[(int)StabilityAxisForces.eDrag];
            }

            //correct signs of drag and lift to wind axes convention
            //positive forward, right, down
            vFs[(int)StabilityAxisForces.eDrag] *= -1; vFs[(int)StabilityAxisForces.eLift] *= -1;

            vForces = FDMExec.State.GetTs2b() * vFs;

            vDXYZcg = FDMExec.MassBalance.StructuralToBody(FDMExec.Aircraft.AeroRefPointXYZ);

            vMoments = Vector3D.Cross(vDXYZcg, vForces); // M = r X F

            for (int axis_ctr = 0; axis_ctr < 3; axis_ctr++)
            {
                if (Coeff[axis_ctr + 3] != null)
                {
                    foreach (Function func in Coeff[axis_ctr + 3])
                    {
                        vMoments[axis_ctr] += func.GetValue();
                    }
                }
            }

            return false;
        }


		/// <summary>
		/// Gets the total aerodynamic force vector.
		/// </summary>
		/// <returns>a force vector reference.</returns>
		[ScriptAttribute("forces/fb-aero-lbs", "The total aerodynamic force vector")]
		public Vector3D Forces
		{
			get {return vForces;}
		}


		/// <summary>
		/// Gets the aerodynamic force for an axis.
		/// </summary>
		/// <param name="n">Axis index. This could be 0, 1, or 2, or one of the axis enums: eX, eY, eZ.</param>
		/// <returns>the force acting on an axis</returns>
		public double GetForces(int n) {return vForces[n];}

		[ScriptAttribute("forces/fbx-aero-lbs", "Aerodynamic force for the X axis.")]
		public double ForceX {get{return vForces.X;}}

		[ScriptAttribute("forces/fby-aero-lbs", "Aerodynamic force for the Y axis.")]
		public double ForceY {get{return vForces.Y;}}

		[ScriptAttribute("forces/fbz-aero-lbs", "Aerodynamic force for the Z axis.")]
		public double ForceZ {get{return vForces.Z;}}


		/// <summary>
		/// Gets the total aerodynamic moment vector.
		/// </summary>
		/// <returns>a moment vector reference.</returns>
		[ScriptAttribute("moments/aero-lbsft", "Aerodynamic moment vector.")]
		public Vector3D Moments	{get {return vMoments;}}

		[ScriptAttribute("moments/l-aero-lbsft", "Aerodynamic moment for the X axis.")]
		public double MomentX {get{return vMoments.X;}}

		[ScriptAttribute("moments/m-aero-lbsft", "Aerodynamic moment for the Y axis.")]
		public double MomentY {get{return vMoments.Y;}}
		
		[ScriptAttribute("moments/n-aero-lbsft", "Aerodynamic moment for the Z axis.")]
		public double MomentZ {get{return vMoments.Z;}}


		/// <summary>
		/// Gets the aerodynamic moment for an axis.
		/// </summary>
		/// <param name="n">Axis index. This could be 0, 1, or 2, or one of the axis enums: eX, eY, eZ.</param>
		/// <returns>the moment about a single axis (as described also in the
		/// similar call to GetForces(int n).</returns>
		public double GetMoments(int n) {return vMoments[n];}

		public Vector3D GetvLastFs() { return vLastFs; }
		public double GetvLastFs(int axis) { return vLastFs[axis]; }

		[ScriptAttribute("forces/fw-aero-lbs", "Forces Drag Side Lift")]
		public Vector3D ForcesDragSideLift {get { return vFs; }}

		public double GetvFs(int axis) { return vFs[axis]; }

		[ScriptAttribute("forces/fwx-aero-lbs", "Aerodynamic **** for the X axis.")]
		public double vFsX {get{ return vFs.X; }}

		[ScriptAttribute("forces/fwy-aero-lbs", "Aerodynamic **** for the Y axis.")]
		public double vFsY {get{ return vFs.Y; }}

		[ScriptAttribute("forces/fwz-aero-lbs", "Aerodynamic **** for the Z axis.")]
		public double vFsZ {get{ return vFs.Z; }}

		[ScriptAttribute("forces/lod-norm", "TODO: comments.")]
		public double LoD {get { return lod; }}

		[ScriptAttribute("aero/cl-squared", "TODO: comments.")]
		public double ClSquared  { get { return clsq; } }

		[ScriptAttribute("aero/alpha-max-deg", "TODO: comments.")]
		public double AlphaCLMax
		{
			get { return alphaclmax; }
			set { alphaclmax = value; }
		}

		[ScriptAttribute("aero/alpha-min-deg", "TODO: comments.")]
		public double AlphaCLMin
		{
			get { return alphaclmin; }
			set { alphaclmin = value; }
		}
  
		public double GetAlphaHystMax() { return alphahystmax; }
		public double GetAlphaHystMin() { return alphahystmin; }
		
		[ScriptAttribute("aero/stall-hyst-norm", "TODO: comments.")]
		public double HysteresisParm
		{ get { return stall_hyst; }}
		
		[ScriptAttribute("systems/stall-warn-norm", "TODO: comments.")]
		public double StallWarn
		{ get { return impending_stall; }}
		
		[ScriptAttribute("aero/alpha-wing-rad", "TODO: comments.")]
		public double AlphaW
		{ get { return alphaw; }}

		[ScriptAttribute("aero/bi2vel", "TODO: comments.")]
		public double BI2Vel { get { return bi2vel; }}
		
		[ScriptAttribute("aero/ci2vel", "TODO: comments.")]
		public double CI2Vel { get { return ci2vel; }}

        [ScriptAttribute("aero/qbar-area", "TODO: comments.")]
        public double QbarArea { get { return qbar_area; } }

		/// <summary>
		/// Gets the strings for the current set of coefficients.
		/// </summary>
		/// <param name="delimeter">delimeter either a tab or comma string depending on output type</param>
		/// <returns>a string containing the descriptive names for all coefficients</returns>
        public string GetCoefficientStrings(string delimeter)
        {
            string CoeffStrings = "";
            bool firstime = true;

            for (int sd = 0; sd < variables.Count; sd++)
            {
                if (firstime)
                {
                    firstime = false;
                }
                else
                {
                    CoeffStrings += delimeter;
                }
                CoeffStrings += variables[sd].Name;
            }

            for (int axis = 0; axis < 6; axis++)
            {
                if (Coeff[axis] != null)
                {
                    foreach (Function func in Coeff[axis])
                    {
                        CoeffStrings += delimeter;
                        CoeffStrings += func.Name;
                    }
                }
            }
            return CoeffStrings;
        }

		
		/// <summary>
		/// Gets the coefficient values.
		/// </summary>
		/// <param name="delimeter">delimeter either a tab or comma string depending on output type</param>
		/// <returns>a string containing the numeric values for the current set of
		/// coefficients</returns>
        public string GetCoefficientValues(string format, IFormatProvider provider, string delimeter)
		{
			StringBuilder SDValues = new StringBuilder();
			bool firstime = true;

            foreach (Function func in variables)
            {
                if (firstime)
                {
                    firstime = false;
                }
                else
                {
                    SDValues.Append(delimeter);
                }
                SDValues.Append(func.GetValue().ToString(format, provider)); ;
            }

			for (int axis = 0; axis < 6; axis++) 
			{
                if (Coeff[axis] != null)
                {
                    foreach (Function func in Coeff[axis])
                    {
                        SDValues.Append(delimeter);
                        SDValues.Append(func.GetValue().ToString(format, provider));
                    }
                }
			}
			return SDValues.ToString();
		}

				
		public void Load(XmlElement element)
		{
			foreach (XmlNode currentNode in element.ChildNodes)
			{
				if (currentNode.NodeType == XmlNodeType.Element) 
				{
					XmlElement currentElement = (XmlElement)currentNode;
                    
					if (currentElement.LocalName.Equals("axis"))
					{
						LoadAxis(currentElement);
                    }
                    else if (currentElement.LocalName.Equals("alphalimits"))
                    {
                        string supplied_units = currentElement.GetAttribute("unit");
                        XmlNodeList elems = currentElement.GetElementsByTagName("min");
                        alphaclmin = FormatHelper.ValueAsNumberConvertTo(elems[0] as XmlElement, "DEG", supplied_units);
                        elems = currentElement.GetElementsByTagName("max");
                        alphaclmax = FormatHelper.ValueAsNumberConvertTo(elems[0] as XmlElement, "DEG", supplied_units);
                    }
                    else if (currentElement.LocalName.Equals("hysteresis_limits"))
                    {
                        string supplied_units = currentElement.GetAttribute("unit");
                        XmlNodeList elems = currentElement.GetElementsByTagName("min");
                        alphahystmin = FormatHelper.ValueAsNumberConvertTo(elems[0] as XmlElement, "DEG", supplied_units);
                        elems = currentElement.GetElementsByTagName("max");
                        alphahystmax = FormatHelper.ValueAsNumberConvertTo(elems[0] as XmlElement, "DEG", supplied_units);
                    } else if (currentElement.LocalName.Equals("function"))
                    {
                        variables.Add(new Function(FDMExec, currentElement));
                    }
				}
			}
		}

        protected void LoadAxis(XmlElement element)
        {
            string axis = element.GetAttribute("name");
            List<Function> ca = new List<Function>();

            foreach (XmlNode currentNode in element.ChildNodes)
            {
                if (currentNode.NodeType == XmlNodeType.Element)
                {
                    XmlElement currentElement = (XmlElement)currentNode;

                    if (currentElement.LocalName.Equals("function"))
                    {
                        Function func = new Function(FDMExec, currentElement);
                        ca.Add(func);
                    }
                }
            }
            Coeff[(int)AxisIdx[axis]] = ca;
        }


		//TODO Review this when c# 2.0 will be ready 
		// private typedef map<string,int> AxisIndex;
		private  Hashtable AxisIdx = new Hashtable();
		//TODO private typedef vector<FGCoefficient*> CoeffArray;
        private List<Function>[] Coeff = new List<Function>[6]; //TODO it is 6 arrylists
        private List<Function> variables = new List<Function>();
		private Vector3D vFs;
		private Vector3D vForces;
		private Vector3D vMoments;
		private Vector3D vLastFs;
		private Vector3D vDXYZcg;
		private double alphaclmax = 0.0, alphaclmin = 0.0;
		private double alphahystmax= 0.0, alphahystmin= 0.0;
		private double impending_stall = 0.0, stall_hyst = 0.0;
		private double bi2vel = 0.0, ci2vel = 0.0,alphaw = 0.0;
		private double clsq = 0.0,lod = 0.0;
        private double qbar_area = 0.0;
  
		//TODO private typedef double (FGAerodynamics::*PMF)(int) const;

		private const int NAxes =6;
		private static string[] AxisNames = new string[] { "drag", "side", "lift", "roll", "pitch","yaw" };
		private static string[] AxisNamesUpper = new string[] {"DRAG", "SIDE", "LIFT", "ROLL", "PITCH","YAW" };

		private const string IdSrc = "$Id: FGAerodynamics.cpp,v 1.54 2005/01/27 12:23:10 jberndt Exp $";

	}
}
