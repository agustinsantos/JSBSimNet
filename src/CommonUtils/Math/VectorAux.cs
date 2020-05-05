#region Copyright(C)  Licensed under GNU GPL.
/// Copyright (C) 2005-2006 Agustin Santos Mendez
/// 
/// JSBSim was developed by Jon S. Berndt, Tony Peden, and
/// David Megginson. 
/// Agustin Santos Mendez implemented and maintains this C# version.
/// 
/// This program is free software; you can redistribute it and/or
///  modify it under the terms of the GNU General Public License
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
namespace CommonUtils.MathLib
{
    using System;

    /// <summary>
    /// Additional vector routines.
    /// </summary>
    public sealed class VectorAux
    {

        /// <summary>
        /// Map a vector onto a plane.
        /// </summary>
        /// <param name="normal">normal vector for the plane</param>
        /// <param name="v0">a point on the plane</param>
        /// <param name="vec">the vector to map onto the plane</param>
        /// <returns>the result vector</returns>
        public static Vector3D MapVectorOntoSurfacePlane(Vector3D normal, Vector3D v0, Vector3D vec)
        {
            Vector3D result;
            Vector3D u1, v;

            // calculate a vector "u1" representing the shortest distance from
            // the plane specified by normal and v0 to a point specified by
            // "vec".  "u1" represents both the direction and magnitude of
            // this desired distance.
            u1 = (Vector3D.Dot(normal, vec) / Vector3D.Dot(normal, normal)) * normal;

            // calculate the vector "v" which is the vector "vec" mapped onto
            // the plane specified by "normal" and "v0".
            v = v0 + vec - u1;

            // Calculate the vector "result" which is "v" - "v0" which is a
            // directional vector pointing from v0 towards v
            result = v - v0;

            return result;
        }

        /// <summary>
        /// Given a point p, and a line through p0 with direction vector d,
        /// find the closest point (p1) on the line.
        /// </summary>
        /// <param name="p">original point</param>
        /// <param name="p0">point on the line</param>
        /// <param name="d">vector defining line direction</param>
        /// <returns>closest point to p on the line</returns>
        public static Vector3D ClosestPointToLine(Vector3D p, Vector3D p0, Vector3D d)
        {
            Vector3D p1;
            Vector3D u, u1;

            u = p - p0;

            // calculate the projection, u1, of u along d.
            u1 = (Vector3D.Dot(u, d) / Vector3D.Dot(d, d)) * d;

            // calculate the point p1 along the line that is closest to p
            p1 = p0 + u1;
            //TODO Review this conversion. Original code: sgAddVec3(p1, p0, u1);

            return p1;
        }


        /// <summary>
        /// Given a point p, and a line through p0 with direction vector d,
        /// find the shortest distance (squared) from the point to the line.
        /// </summary>
        /// <param name="p">original point</param>
        /// <param name="p0">point on the line</param>
        /// <param name="d">vector defining line direction</param>
        /// <returns>shortest distance (squared) from p to line</returns>
        public static double ClosestPointToLineDistSquared(Vector3D p, Vector3D p0, Vector3D d)
        {
            Vector3D u, u1, v;

            u = p - p0;

            // calculate the projection, u1, of u along d.
            u1 = (Vector3D.Dot(u, d) / Vector3D.Dot(d, d)) * d;

            // vector from closest point on line, p1, to the
            // original point, p.
            v = u - u1;

            //return ( sgScalarProductVec3(v * v) );
            return Vector3D.Dot(v, v);
        }
    }
}
