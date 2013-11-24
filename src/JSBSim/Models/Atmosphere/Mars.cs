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
using System;
using System.Collections.Generic;
using System.Text;

// Import log4net classes.
using log4net;

using JSBSim.Models;
using CommonUtils.MathLib;

namespace JSBSim.Models
{
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
            //TODO Reng = 53.5 * 44.01;
        }

        public override bool InitModel()
        {
            base.InitModel();

            Calculate(h);
            SLtemperature = internalInfo.Temperature;
            SLpressure = internalInfo.Pressure;
            SLdensity = internalInfo.Density;
            SLsoundspeed = Math.Sqrt(Constants.SHRatio * Constants.Reng * internalInfo.Temperature);
            rSLtemperature = 1.0 / internalInfo.Temperature;
            rSLpressure = 1.0 / internalInfo.Pressure;
            rSLdensity = 1.0 / internalInfo.Density;
            rSLsoundspeed = 1.0 / SLsoundspeed;

            useInfo = internalInfo;
            useExternal = false;

            return true;
        }

        public override bool Run()
        {
            if (InternalRun()) return true;
            if (FDMExec.Holding()) return false;

            T_dev = 0.0;

            // if false then execute this Run()
            //do temp, pressure, and density first
            if (!useExternal)
            {
                h = FDMExec.Propagate.Altitude;
                Calculate(h);
            }

            if (turbType != TurbType.ttNone)
            {
                Turbulence();
                vWindNED += vTurbulence;
            }

            if (vWindNED[0] != 0.0)
                psiw = Math.Atan2(vWindNED[1], vWindNED[0]);

            if (psiw < 0) psiw += 2 * Math.PI;

            soundspeed = Math.Sqrt(Constants.SHRatio * Constants.Reng * (useInfo.Temperature));

            return false;
        }

        /* TODO ASM. Review that with Jon. all these vars are redefined??
        double rho;

        private enum tType { Standard, Berndt, None };

        private int lastIndex;
        private double h;
        private double[] htab = new double[8];
        private double SLtemperature, SLdensity, SLpressure, SLsoundspeed;
        private double rSLtemperature, rSLdensity, rSLpressure, rSLsoundspeed; //reciprocals
        //todo double *temperature,*density,*pressure;
        private double soundspeed;
        private bool useExternal;
        private double exTemperature, exDensity, exPressure;
        private double intTemperature, intDensity, intPressure;

        private double MagnitudedAccelDt, MagnitudeAccel, Magnitude;
        private double TurbGain;
        private double TurbRate;
        private Vector3D vDirectiondAccelDt;
        private Vector3D vDirectionAccel;
        private Vector3D vDirection;
        private Vector3D vTurbulence;
        private Vector3D vTurbulenceGrad;
        private Vector3D vBodyTurbGrad;
        private Vector3D vTurbPQR;

        private Vector3D vWindNED;
        private double psiw;
        */
    }
}
