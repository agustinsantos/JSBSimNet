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
#region Identification
/// $Id:$
#endregion

using System;
using System.Collections.Generic;
using System.Text;

using CommonUtils.MathLib;

namespace JSBSim.InputOutput
{
    public class GroundCallback
    {   
        ///<summary>
        /// Compute the altitude above sealevel.
        ///</summary>
        public virtual double GetAltitude(Location loc)
        {
            return loc.Radius - mReferenceRadius;
        }

        ///<summary>
        /// Compute the altitude above ground. Defaults to sealevel altitude.
        ///</summary>
        public double GetAGLevel(double t, Location loc, out Location contact, out Vector3D normal, out Vector3D vel)
        {

            //TODO TODO 
            // This function is defined as is at the original JSBSim.
            // vel and normal are not used ????
            vel = new Vector3D(0.0, 0.0, 0.0);
            normal = (-1 / ((Vector3D)loc).GetMagnitude()) * (Vector3D)loc;
            double radius = loc.Radius;
            double agl = GetAltitude(loc);
            contact = (Location)(((radius - agl) / radius) * (Vector3D)loc);
            return agl;
        }
        /// Reference radius.
        private double mReferenceRadius = 20925650.0;
    }
}
