#region Copyright(C)  Licensed under GNU GPL.
/// Copyright (C) 2005-2020 Agustin Santos Mendez
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
/// 
/// Further information about the GNU Lesser General Public License can also be found on
/// the world wide web at http://www.gnu.org.
#endregion

namespace JSBSim.InputOutput
{
    using System;
    using CommonUtils.MathLib;


    /// <summary>
    /// This class provides callback slots to get ground specific data.
    /// The default implementation returns values for a
    /// ball formed earth with an adjustable terrain elevation.
    /// @author Mathias Froehlich
    /// </summary>
    public abstract class GroundCallback
    {
        public GroundCallback() { time = 0.0; }
        //virtual ~FGGroundCallback() { }

        /// <summary>
        /// Compute the altitude above ground.
        /// The altitude depends on time t and location l.
        /// </summary>
        /// <param name="t">simulation time</param>
        /// <param name="location">location</param>
        /// <param name="contact">Contact point location below the location l</param>
        /// <param name="normal">Normal vector at the contact point</param>
        /// <param name="v">Linear velocity at the contact point</param>
        /// <param name="w">Angular velocity at the contact point</param>
        /// <returns>altitude above ground</returns>
        public abstract double GetAGLevel(double t, Location location,
                            out Location contact,
                             out Vector3D normal, out Vector3D v,
                             out Vector3D w);

        /// <summary>
        /// Compute the altitude above ground.
        /// The altitude depends on location l.
        /// </summary>
        /// <param name="location">location</param>
        /// <param name="contact">Contact point location below the location l</param>
        /// <param name="normal">Normal vector at the contact point</param>
        /// <param name="v">Linear velocity at the contact point</param>
        /// <param name="w">Angular velocity at the contact point</param>
        /// <returns>altitude above ground</returns>
        public virtual double GetAGLevel(Location location, out Location contact,
                            out Vector3D normal, out Vector3D v,
                            out Vector3D w)
        {
            return GetAGLevel(time, location, out contact, out normal, out v, out w);
        }


        /// <summary>
        /// Set the terrain elevation.
        /// Only needs to be implemented if JSBSim should be allowed
        /// to modify the local terrain radius(see the default implementation)
        /// </summary>
        /// <param name="h"></param>
        public virtual void SetTerrainElevation(double h) { }


        /// <summary>
        /// Set the simulation time.
        /// The elapsed time can be used by the ground callbck to assess the planet
        /// rotation or the movement of objects.
        /// </summary>
        /// <param name="_time">elapsed time in seconds since the simulation started.</param>
        public void SetTime(double _time) { time = _time; }

        protected double time;
    }

    //%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
    // The default sphere earth implementation:
    //%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%

    public class DefaultGroundCallback : GroundCallback
    {
        public DefaultGroundCallback(double semiMajor, double semiMinor)
        {
            a = semiMajor; b = semiMinor;
        }

        public override double GetAGLevel(double t, Location loc,
                            out Location contact,
                             out Vector3D normal, out Vector3D vel,
                             out Vector3D angularVel)
        {
            vel = Vector3D.Zero;
            angularVel = Vector3D.Zero;
            Location l = loc.Clone();
            l.SetEllipse(a, b);
            double latitude = l.GeodLatitudeRad;
            double cosLat = Math.Cos(latitude);
            double longitude = l.Longitude;
            normal = new Vector3D(cosLat * Math.Cos(longitude), cosLat * Math.Sin(longitude),
                                     Math.Sin(latitude));
            double loc_radius = loc.Radius;  // Get the radius of the given location
                                             // (e.g. the CG)
            contact = new Location();
            contact.SetEllipse(a, b);
            contact.SetPositionGeodetic(longitude, latitude, mTerrainElevation);
            return l.GeodAltitude - mTerrainElevation;

        }


        public override void SetTerrainElevation(double h)
        {
            mTerrainElevation = h;
        }


        private double a, b;
        private double mTerrainElevation = 0.0;
    }
}
