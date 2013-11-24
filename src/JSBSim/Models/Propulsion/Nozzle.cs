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

namespace JSBSim.Models.Propulsion
{
	using System;
	using System.Xml;

	// Import log4net classes.
	using log4net;

	using JSBSim.InputOutput;
    using JSBSim.Format;

	/// <summary>
	/// Models a rocket nozzle.
	/// @author Jon S. Berndt
	/// </summary>
	public class Nozzle : Thruster
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

        public Nozzle(FDMExecutive exec, XmlElement parent, XmlElement nozzleElement, int engineNum)
            : base(exec)
		{
            XmlElement tmpElem = nozzleElement.GetElementsByTagName("pe")[0] as XmlElement;
            if (tmpElem != null)
                PE = FormatHelper.ValueAsNumberConvertTo(tmpElem, "PSF");
            else
            {
                if (log.IsErrorEnabled)
                    log.Error("Fatal Error: Nozzle exit pressure must be given in nozzle config file.");
                throw new Exception("Fatal Error: Nozzle exit pressure must be given in nozzle config file.");
            }
            tmpElem = nozzleElement.GetElementsByTagName("expr")[0] as XmlElement;
            if (tmpElem != null)
                ExpR = FormatHelper.ValueAsNumber(tmpElem);
            else
            {
                if (log.IsErrorEnabled)
                    log.Error("Fatal Error: Nozzle expansion ratio must be given in nozzle config file.");
                throw new Exception("Fatal Error: Nozzle expansion ratio must be given in nozzle config file.");
            }
            tmpElem = nozzleElement.GetElementsByTagName("nzl_eff")[0] as XmlElement;
            if (tmpElem != null)
                nzlEff = FormatHelper.ValueAsNumber(tmpElem);
            else
            {
                if (log.IsErrorEnabled)
                    log.Error("Fatal Error: Nozzle efficiency must be given in nozzle config file.");
                throw new Exception("Fatal Error: Nozzle efficiency must be given in nozzle config file.");
            }
            tmpElem = nozzleElement.GetElementsByTagName("diam")[0] as XmlElement;
            if (tmpElem != null)
                Diameter = FormatHelper.ValueAsNumberConvertTo(tmpElem, "FT");
            else
            {
                if (log.IsErrorEnabled)
                    log.Error("Fatal Error: Nozzle diameter must be given in nozzle config file.");
                throw new Exception("Fatal Error: Nozzle diameter must be given in nozzle config file.");
            }

            thrust = 0;
            ReverserAngle = 0.0;
            thrusterType = ThrusterType.Nozzle;
            Area2 = (Diameter * Diameter / 4.0) * Math.PI;
            AreaT = Area2 / ExpR;

        }

		public override double Calculate(double CfPc)
		{
			double pAtm = FDMExec.Atmosphere.Pressure;
			thrust = Math.Max((double)0.0, (CfPc * AreaT + (PE - pAtm)*Area2) * nzlEff);
			vFn.X = thrust;

			thrustCoeff = Math.Max((double)0.0, CfPc / ((pAtm - PE) * Area2));

			return thrust;
		}

		public override double GetPowerRequired() { return PE; }
		public override string GetThrusterLabels(int id, string delimeter)
		{
			return Name + "_Thrust[" + id + "]";
		}

		public override string GetThrusterValues(int id, string delimeter)
		{
			return thrust.ToString();
		}

		private double PE;
		private double ExpR;
		private double nzlEff;
		private double Diameter;
		private double AreaT;
		private double Area2;

		private const string IdSrc = "$Id: FGNozzle.cpp,v 1.39 2004/11/28 15:17:11 dpculp Exp $";
	}
}
