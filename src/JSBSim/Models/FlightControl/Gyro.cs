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
    using System.Xml;
    using CommonUtils.MathLib;
    using JSBSim.Models;
    // Import log4net classes.
    using log4net;


    /// <summary>
    ///  Encapsulates a Gyro component for the flight control system.
    /// 
    /// Syntax:
    /// 
    /// @code
    /// <gyro name="name">
    ///   <lag> number </lag>
    ///   <noise variation = "PERCENT|ABSOLUTE" > number </ noise >
    ///   < quantization name="name">
    ///     <bits> number</bits>
    ///     <min> number</min>
    ///     <max> number</max>
    ///   </quantization>
    ///   <drift_rate> number</drift_rate>
    ///   <bias> number</bias>
    /// </gyro>
    /// @endcode
    /// 
    /// Example:
    /// 
    /// @code
    /// <gyro name="aero/gyro/roll">
    ///   <axis> X</axis>
    ///   <lag> 0.5 </lag>
    ///   <noise variation = "PERCENT" > 2 </ noise >
    ///   < quantization name="aero/gyro/quantized/qbar">
    ///     <bits> 12 </bits>
    ///     <min> 0 </min>
    ///     <max> 400 </max>
    ///   </quantization>
    ///   <bias> 0.5 </bias>
    /// </gyro>
    /// @endcode
    /// 
    /// For noise, if the type is PERCENT, then the value supplied is understood to be a
    /// percentage variance.That is, if the number given is 0.05, the the variance is
    /// understood to be +/-0.05 percent maximum variance.So, the actual value for the
    /// gyro will be *anywhere* from 0.95 to 1.05 of the actual "perfect" value at any
    /// time - even varying all the way from 0.95 to 1.05 in adjacent frames - whatever
    /// the delta time.
    /// 
    /// @author Jon S.Berndt
    /// </summary>
    public class Gyro : Sensor 
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

        Gyro(FlightControlSystem fcs, XmlElement element)
           : base(fcs, element)
        {
            sensorOrientation=  new SensorOrientation(element);
            Propagate = fcs.GetExec().GetPropagate();

            Debug(0);
        }

        public override bool Run()
        {
            // There is no input assumed. This is a dedicated rotation rate sensor.

            // get aircraft rates
            Rates = Propagate.GetPQRi();

            // transform to the specified orientation
            vRates = sensorOrientation.mT * Rates;

            input =  vRates[sensorOrientation.axis -1];

            ProcessSensorSignal();

            SetOutput();

            return true;
        }



        private Propagate Propagate;
        private Vector3D Rates;
        private Vector3D vRates;
        private SensorOrientation sensorOrientation;

        protected override void Bind(XmlElement el) { }
        protected override void Debug(int from) { }
    }
}
