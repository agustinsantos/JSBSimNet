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

namespace JSBSim.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    // Import log4net classes.
    using log4net;

    using JSBSim.Models;
    using CommonUtils.MathLib;

    public class Mars : Atmosphere
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

        public Mars(FDMExecutive exec)
            : base(exec)
        {
            Name = "Mars";
            Reng = 53.5 * 44.01;

            Bind();
            //Debug(0);
        }

        public override double GetPressure(double altitude)
        {
            throw new NotImplementedException("Pending upgrade to lastest version of JSBSIM");
        }

        public override double GetTemperature(double altitude)
        {
            throw new NotImplementedException("Pending upgrade to lastest version of JSBSIM");
        }

        public override void SetTemperature(double t, double h, eTemperature unit = eTemperature.eFahrenheit)
        {
            throw new NotImplementedException("Pending upgrade to lastest version of JSBSIM");
        }

        protected override void Calculate(double altitude)
        {
            //Calculate reftemp, refpress, and density

            // LIMIT the temperatures so they do not descend below absolute zero.

            if (altitude < 22960.0)
            {
                temperature = -25.68 - 0.000548 * altitude; // Deg Fahrenheit
            }
            else
            {
                temperature = -10.34 - 0.001217 * altitude; // Deg Fahrenheit
            }
            pressure = 14.62 * Math.Exp(-0.00003 * altitude); // psf - 14.62 psf =~ 7 millibars
            density = Pressure / (Reng * Temperature); // slugs/ft^3 (needs deg R. as input

            //cout << "Atmosphere:  h=" << altitude << " rho= " << intDensity << endl;
        }

        //private void Debug(int from);
    }
}
