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

	// Import log4net classes.
	using log4net;
	
	using JSBSim.Script;
	using CommonUtils.MathLib;
	using JSBSim.Format;


	/// <summary>
	/// Models weight and balance information.
	/// </summary>
	public class MassBalance : Model
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

		public MassBalance(FDMExecutive fdmex)
			: base(fdmex)
		{
			Name = "MassBalance";
		}


        public override bool Run(bool Holding)
        {
            double denom, k1, k2, k3, k4, k5, k6;
            double Ixx, Iyy, Izz, Ixy, Ixz, Iyz;

            if (InternalRun()) return true;
            if (FDMExec.Holding()) return false; // if paused don't execute

            weight = emptyWeight + FDMExec.Propulsion.GetTanksWeight() + GetPointMassWeight();

            mass = Constants.lbtoslug * weight;

            // Calculate new CG

            vXYZcg = (FDMExec.Propulsion.GetTanksMoment() + emptyWeight * vbaseXYZcg
                + GetPointMassMoment()) / weight;

            // Calculate new total moments of inertia

            // At first it is the base configuration inertia matrix ...
            mJ = baseJ;
            // ... with the additional term originating from the parallel axis theorem.
            mJ += GetPointmassInertia(Constants.lbtoslug * emptyWeight, vbaseXYZcg);
            // Then add the contributions from the additional pointmasses.
            mJ += CalculatePMInertias();
            mJ += FDMExec.Propulsion.CalculateTankInertias();

            Ixx = mJ.M11;
            Iyy = mJ.M22;
            Izz = mJ.M33;
            Ixy = -mJ.M12;
            Ixz = -mJ.M13;
            Iyz = -mJ.M23;

            // Calculate inertia matrix inverse (ref. Stevens and Lewis, "Flight Control & Simulation")

            k1 = (Iyy * Izz - Iyz * Iyz);
            k2 = (Iyz * Ixz + Ixy * Izz);
            k3 = (Ixy * Iyz + Iyy * Ixz);

            denom = 1.0 / (Ixx * k1 - Ixy * k2 - Ixz * k3);
            k1 = k1 * denom;
            k2 = k2 * denom;
            k3 = k3 * denom;
            k4 = (Izz * Ixx - Ixz * Ixz) * denom;
            k5 = (Ixy * Ixz + Iyz * Ixx) * denom;
            k6 = (Ixx * Iyy - Ixy * Ixy) * denom;

            mJinv = new Matrix3D(k1, k2, k3, k2, k4, k5, k3, k5, k6);

            return false;
        }

		[ScriptAttribute("inertia/mass-slugs", "TODO comments")]
		public double Mass			{ get {return mass;}}
		
		[ScriptAttribute("inertia/weight-lbs", "TODO comments")]
		public double Weight		{ get {return weight;}}

		public double EmptyWeight	
		{
			get {return emptyWeight;}
			set {emptyWeight = value;}
		}

		[ScriptAttribute("inertia/cg-x-ft", "TODO comments")]
		public double CenterGravityX { get {return vXYZcg.X;}}

		[ScriptAttribute("inertia/cg-y-ft", "TODO comments")]
		public double CenterGravityY { get {return vXYZcg.Y;}}

		[ScriptAttribute("inertia/cg-z-ft", "TODO comments")]
		public double CenterGravityZ { get {return vXYZcg.Z;}}

		public Vector3D GetXYZcg() {return vXYZcg;}
		public double GetXYZcg(int axis)  {return vXYZcg[axis];}
		public double GetbaseXYZcg(int axis) {return vbaseXYZcg[axis];}


		/// <summary>
		/// Computes the inertia contribution of a pointmass.
		/// Computes and returns the inertia matrix of a pointmass of mass
		/// slugs at the given vector r in the structural frame. The units
		/// should be for the mass in slug and the vector in the structural
		/// frame as usual in inches.
		/// </summary>
		/// <param name="slugs">the mass of this single pointmass given in slugs</param>
		/// <param name="r">the location of this single pointmass in the structural frame</param>
		/// <returns></returns>
		public Matrix3D GetPointmassInertia(double slugs, Vector3D r)
		{
			Vector3D v = StructuralToBody( r );
			Vector3D sv = slugs*v;
			double xx = sv.X*v.X;
			double yy = sv.Y*v.Y;
			double zz = sv.Z*v.Z;
			double xy = -sv.X*v.Y;
			double xz = -sv.X*v.Z;
			double yz = -sv.Y*v.Z;
			return new Matrix3D( yy+zz, xy, xz,
				xy, xx+zz, yz,
				xz, yz, xx+yy );
		}


		/// <summary>
		/// Conversion from the structural frame to the body frame.
		/// Converts the location given in the structural frame
		/// coordinate system to the body frame. The units of the structural
		/// frame are assumed to be in inches. The unit of the result is in
		/// ft.
		/// </summary>
		/// <param name="r">vector coordinate in the structural reference frame (X positive
		/// aft, measurements in inches).</param>
		/// <returns>coordinate in the body frame, in feet.</returns>
		public Vector3D StructuralToBody(Vector3D r)
		{
			// Under the assumption that in the structural frame the:
			//
			// - X-axis is directed afterwards,
			// - Y-axis is directed towards the right,
			// - Z-axis is directed upwards,
			//
			// (as documented in http://jsbsim.sourceforge.net/JSBSimCoordinates.pdf)
			// we have to subtract first the center of gravity of the plane which
			// is also defined in the structural frame:
			//
			//   Vector3D cgOff = r - vXYZcg;
			//
			// Next, we do a change of units:
			//
			//   cgOff *= inchtoft;
			//
			// And then a 180 degree rotation is done about the Y axis so that the:
			//
			// - X-axis is directed forward,
			// - Y-axis is directed towards the right,
			// - Z-axis is directed downward.
			//
			// This is needed because the structural and body frames are 180 degrees apart.

			return new Vector3D(Constants.inchtoft*(vXYZcg.X-r.X),
				Constants.inchtoft*(r.Y-vXYZcg.Y),
				Constants.inchtoft*(vXYZcg.Z-r.Z));
		}


		public void SetBaseCG(Vector3D CG) {vbaseXYZcg = vXYZcg = CG;}

		public double GetPointMassWeight()
		{
			double PM_total_weight = 0.0;

			for (int i=0; i<pointMasses.Count; i++) 
			{
                PM_total_weight += pointMasses[i].Weight;
			}
			return PM_total_weight;
		}

		public Vector3D GetPointMassMoment()
		{
			pointMassCG = Vector3D.Zero;

            for (int i = 0; i < pointMasses.Count; i++) 
			{
                pointMassCG += pointMasses[i].Weight * pointMasses[i].Location;
			}
			return pointMassCG;
		}

		public Matrix3D GetJ() {return mJ;}
		public Matrix3D GetJinv() {return mJinv;}
		public void SetAircraftBaseInertias(Matrix3D BaseJ) {baseJ = BaseJ;}
		public Matrix3D GetAircraftBaseInertias() {return baseJ;}
/*
		public int GetNumPointMasses() {return pointMassLoc.Count;}
		public Vector3D GetPointMassLoc(int i) {return (Vector3D)pointMassLoc[i];}
		public double GetPointMassWeight(int i) {return (double)pointMassWeight[i];}
 */ 
		public void Load(XmlElement element)
		{
			double bixx, biyy, bizz, bixy, bixz, biyz;

			bixx = biyy = bizz = bixy = bixz = biyz = 0.0;
			foreach (XmlNode currentNode in element.ChildNodes)
			{
                if (currentNode.NodeType == XmlNodeType.Element)
                {
                    XmlElement currentElement = (XmlElement)currentNode;

                    if (currentElement.LocalName.Equals("ixx"))
                    {
                        bixx = FormatHelper.ValueAsNumberConvertTo(currentElement, "SLUG*FT2");
                    }
                    else if (currentElement.LocalName.Equals("iyy"))
                    {
                        biyy = FormatHelper.ValueAsNumberConvertTo(currentElement, "SLUG*FT2");
                    }
                    else if (currentElement.LocalName.Equals("izz"))
                    {
                        bizz = FormatHelper.ValueAsNumberConvertTo(currentElement, "SLUG*FT2");
                    }
                    else if (currentElement.LocalName.Equals("ixy"))
                    {
                        bixy = FormatHelper.ValueAsNumberConvertTo(currentElement, "SLUG*FT2");
                    }
                    else if (currentElement.LocalName.Equals("ixz"))
                    {
                        bixz = FormatHelper.ValueAsNumberConvertTo(currentElement, "SLUG*FT2");
                    }
                    else if (currentElement.LocalName.Equals("iyz"))
                    {
                        biyz = FormatHelper.ValueAsNumberConvertTo(currentElement, "SLUG*FT2");
                    }
                    else if (currentElement.LocalName.Equals("emptywt"))
                    {
                        emptyWeight = FormatHelper.ValueAsNumberConvertTo(currentElement, "LBS");
                    }
                    else if (currentElement.LocalName.Equals("location"))
                    {
                        // Find all LOCATION elements that descend from this METRICS branch of the
                        // config file. This would be CG location, eyepoint, etc.
                        string element_name = currentElement.GetAttribute("name");
                        if (element_name.Equals("CG"))
                            vbaseXYZcg = FormatHelper.TripletConvertTo(currentElement, "IN");

                    }
                    else if (currentElement.LocalName.Equals("pointmass"))
                    {
                        // Find all POINTMASS elements that descend from this METRICS branch of the
                        // config file.
                        AddPointMass(currentElement);
                    }
                }
			}
			SetAircraftBaseInertias(new Matrix3D(	 bixx,  -bixy,  -bixz,
													-bixy,  biyy,  -biyz,
													-bixz,  -biyz,  bizz ));

		}
        public void AddPointMass(XmlElement el)
        {
            XmlElement element = el.GetElementsByTagName("location")[0] as XmlElement;
            string pointmass_name = el.GetAttribute("name");
            if (element == null)
            {
                if (log.IsErrorEnabled)
                    log.Error("Pointmass " + pointmass_name + "has no location.");
                throw new Exception("Pointmass " + pointmass_name + "has no location.");
            }
            string loc_unit = element.GetAttribute("unit");

            double w = 0.0, x = 0.0, y = 0.0, z = 0.0;
            foreach (XmlNode currentNode in element.ChildNodes)
            {
                if (currentNode.NodeType == XmlNodeType.Element)
                {
                    XmlElement currentElement = (XmlElement)currentNode;

                    if (currentElement.LocalName.Equals("x"))
                    {
                        x = FormatHelper.ValueAsNumberConvertTo(currentElement, loc_unit);
                    }
                    else if (currentElement.LocalName.Equals("y"))
                    {
                        y = FormatHelper.ValueAsNumberConvertTo(currentElement, loc_unit);
                    }
                    else if (currentElement.LocalName.Equals("z"))
                    {
                        z = FormatHelper.ValueAsNumberConvertTo(currentElement, loc_unit);
                    }

                }
            }

            element = el.GetElementsByTagName("weight")[0] as XmlElement;
            if (element == null)
            {
                if (log.IsErrorEnabled)
                    log.Error("Pointmass " + pointmass_name + "has no weight.");
                throw new Exception("Pointmass " + pointmass_name + "has no weight.");
            }
            else
                w = FormatHelper.ValueAsNumberConvertTo(element, "LBS");


            pointMasses.Add(new PointMass(w, x, y, z));
        }

		private double weight		= 0.0;
		private double emptyWeight	= 0.0;
		private double mass			= 0.0;
		private Matrix3D mJ			= Matrix3D.Zero;
		private Matrix3D mJinv		= Matrix3D.Zero;
		private Matrix3D pmJ		= Matrix3D.Zero;
		private Matrix3D baseJ		= Matrix3D.Zero;
		private Vector3D vXYZcg		= Vector3D.Zero;
		private Vector3D vXYZtank	= Vector3D.Zero;
		private Vector3D vbaseXYZcg	= Vector3D.Zero;
		private Vector3D vPMxyz		= Vector3D.Zero;
		private Vector3D pointMassCG;

        private class PointMass
        {
            public PointMass(double w, double x, double y, double z)
            {
                Weight = w;
                Location = new Vector3D(x, y, z);
            }
            public Vector3D Location;
            public double Weight;
        }

        private List<PointMass> pointMasses = new List<PointMass>();

		private Matrix3D CalculatePMInertias()
		{
			int size;

			size = pointMasses.Count;
			if (size == 0) return pmJ;

			pmJ = new Matrix3D();

			for (int i=0; i<size; i++)
				pmJ += GetPointmassInertia( Constants.lbtoslug * pointMasses[i].Weight, pointMasses[i].Location );

			return pmJ;
		}

		private const string IdSrc = "$Id:$";
	}
}
