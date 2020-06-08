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

namespace JSBSim.Models.FlightControl
{
    using System;
    using System.Xml;
    using CommonUtils.IO;
    using CommonUtils.MathLib;
    using JSBSim.Format;
    using JSBSim.Models;
    // Import log4net classes.
    using log4net;


    /// <summary>
    /// Encapsulates a SensorOrientation capability for a sensor.
    /// @author Jon S.Berndt
    /// </summary>
    public class SensorOrientation
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

        public SensorOrientation(XmlElement element)
        {
            XmlElement orient_element = element.FindElement("orientation");
            if (orient_element != null)
                vOrient = FormatHelper.TripletConvertTo(orient_element, "RAD");

            axis = 0;

            XmlElement axis_element = element.FindElement("axis");
            if (axis_element != null)
            {
                string sAxis = element.FindElementValue("axis");
                if (sAxis == "X" || sAxis == "x")
                {
                    axis = 1;
                }
                else if (sAxis == "Y" || sAxis == "y")
                {
                    axis = 2;
                }
                else if (sAxis == "Z" || sAxis == "z")
                {
                    axis = 3;
                }
            }

            if (axis == 0)
            {
                log.Error("  Incorrect/no axis specified for this sensor; assuming X axis");
                axis = 1;
            }

            CalculateTransformMatrix();
        }

        protected internal Vector3D vOrient = new Vector3D();
        protected internal Matrix3D mT;
        protected internal int axis;

        protected internal void CalculateTransformMatrix()
        {
            double cp, sp, cr, sr, cy, sy;

            cp = Math.Cos(vOrient.Pitch); sp = Math.Sin(vOrient.Pitch);
            cr = Math.Cos(vOrient.Roll); sr = Math.Sin(vOrient.Roll);
            cy = Math.Cos(vOrient.Yaw); sy = Math.Sin(vOrient.Yaw);

            mT.M11 = cp * cy;
            mT.M12 = cp * sy;
            mT.M13 = -sp;

            mT.M21 = sr * sp * cy - cr * sy;
            mT.M22 = sr * sp * sy + cr * cy;
            mT.M23 = sr * cp;

            mT.M31 = cr * sp * cy + sr * sy;
            mT.M32 = cr * sp * sy - sr * cy;
            mT.M33 = cr * cp;

            // This transform is different than for FGForce, where we want a native nozzle
            // force in body frame. Here we calculate the body frame accel and want it in
            // the transformed accelerometer frame. So, the next line is commented out.
            // mT = mT.Inverse();
        }
        protected void Debug(int from) { }
    }
}
