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

namespace JSBSim.Models.FlightControl
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Xml;

    // Import log4net classes.
    using log4net;

    using JSBSim.Script;
    using JSBSim.InputOutput;
    using JSBSim.Format;

    /// <summary>
    /// Encapsulates a Sensor component for the flight control system.
    /// 
    /// Syntax:
    /// <code>
    /// <sensor name=”name” rate_group=”name”>
    ///   <input> property </input>
    ///   <lag> number </lag>
    ///   <noise variation=”PERCENT|ABSOLUTE”> number </noise>
    ///   <quantization name="name">
    ///     <bits> number </bits>
    ///     <min> number </min>
    ///    <max> number </max>
    ///   </quantization>
    ///   <drift_rate> number </drift_rate>
    ///   <bias> number </bias>
    /// </sensor>
    /// </code>
    /// 
    /// Example:
    /// <code>
    /// <sensor name=”aero/sensor/qbar” rate_group=”HFCS”>
    ///   <input> aero/qbar </input>
    ///   <lag> 0.5 </lag>
    ///   <noise variation=”PERCENT”> 2 </noise>
    ///   <quantization name="aero/sensor/quantized/qbar">
    ///     <bits> 12 </bits>
    ///     <min> 0 </min>
    ///     <max> 400 </max>
    ///   </quantization>
    ///   <bias> 0.5 </bias>
    /// </sensor>
    /// </code>
    /// 
    /// The only required element in the sensor definition is the input element. In that
    /// case, no degradation would be modeled, and the output would simply be the input.
    /// For noise, if the type is PERCENT, then the value supplied is understood to be a
    /// percentage variance. That is, if the number given is 0.05, the the variance is
    /// understood to be +/-0.05 percent maximum variance. So, the actual value for the sensor
    /// will be *anywhere* from 0.95 to 1.05 of the actual "perfect" value at any time -
    /// even varying all the way from 0.95 to 1.05 in adjacent frames - whatever the delta
    /// time.
    /// </summary>
    public class Sensor : FCSComponent
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

        public Sensor(FlightControlSystem fcs, XmlElement element)
            : base(fcs, element)
        {
            double denom;
            dt = fcs.GetState().DeltaTime;
            XmlElement tmpElem;

            // inputs are read from the base class constructor
            XmlElement quantization_element = element.GetElementsByTagName("quantization")[0] as XmlElement; 
            if (quantization_element != null)
            {
                tmpElem = quantization_element.GetElementsByTagName("bits")[0] as XmlElement;
                if (tmpElem != null)
                {
                    bits = (int)FormatHelper.ValueAsNumber(tmpElem);
                }
                divisions = (1 << bits);
                tmpElem = quantization_element.GetElementsByTagName("min")[0] as XmlElement;
                if (tmpElem != null)
                {
                    min = FormatHelper.ValueAsNumber(tmpElem);
                }
                tmpElem = quantization_element.GetElementsByTagName("max")[0] as XmlElement;
                if (tmpElem != null)
                {
                    max = FormatHelper.ValueAsNumber(tmpElem);
                }
                span = max - min;
                granularity = span / divisions;
            }

            tmpElem = quantization_element.GetElementsByTagName("bias")[0] as XmlElement;
            if (tmpElem != null)
            {
                bias = FormatHelper.ValueAsNumber(tmpElem);
            }

            tmpElem = quantization_element.GetElementsByTagName("drift_rate")[0] as XmlElement;
            if (tmpElem != null)
            {
                drift_rate = FormatHelper.ValueAsNumber(tmpElem);
            }

            tmpElem = quantization_element.GetElementsByTagName("lag")[0] as XmlElement;
            if (tmpElem != null)
            {
                lag = FormatHelper.ValueAsNumber(tmpElem);
                denom = 2.00 + dt * lag;
                ca = dt * lag / denom;
                cb = (2.00 - dt * lag) / denom;
            }

            tmpElem = quantization_element.GetElementsByTagName("noise")[0] as XmlElement;
            if (tmpElem != null)
            {
                noise_variance = FormatHelper.ValueAsNumber(tmpElem);
                string variation = tmpElem.GetAttribute("variation");
                if (variation.Equals("PERCENT"))
                {
                    noiseType = NoiseType.ePercent;
                }
                else if (variation.Equals("ABSOLUTE"))
                {
                    noiseType = NoiseType.eAbsolute;
                }
                else
                {
                    noiseType = NoiseType.ePercent;
                    if (log.IsErrorEnabled)
                    log.Error("Unknown noise type in sensor: " + name + ". Defaulting to PERCENT.");
                }
            }

            base.Bind();
            Bind();
        }

        /* TODO remove Obsolete */
        public void SetFailLow(double val) { FailLow = val; }
        public void SetFailHigh(double val) { FailHigh = val; }
        public void SetFailStuck(double val) { FailStuck = val; }

        public double GetFailLow() { return FailLow; }
        public double GetFailHigh() { return FailHigh; }
        public double GetFailStuck() { return FailStuck; }

        public double FailLow
        {
            get { if (fail_low) return 1.0; else return 0.0; }
            set { if (value > 0.0) fail_low = true; else fail_low = false; }
        }

        public double FailHigh
        {
            get { if (fail_high) return 1.0; else return 0.0; }
            set { if (value > 0.0) fail_high = true; else fail_high = false; }
        }

        public double FailStuck
        {
            get { if (fail_stuck) return 1.0; else return 0.0; }
            set { if (value > 0.0) fail_stuck = true; else fail_stuck = false; }
        }

        public override bool Run()
        {
            input = inputNodes[0].GetDouble() * inputSigns[0];

            output = input; // perfect sensor

            // Degrade signal as specified

            if (fail_stuck)
            {
                output = PreviousOutput;
                return true;
            }

            if (lag != 0.0) 
                Lag();       // models sensor lag
            if (noise_variance != 0.0)
                Noise();     // models noise
            if (drift_rate != 0.0) 
                Drift();     // models drift over time
            if (bias != 0.0) 
                Bias();      // models a finite bias

            if (fail_low)
                output = double.NegativeInfinity;
            if (fail_high) 
                output = double.PositiveInfinity;

            if (bits != 0) 
                Quantize();  // models quantization degradation
            //  if (delay != 0.0)          Delay();     // models system signal transport latencies

            return true;
        }

        private Random randGenerator = new Random();
        private void Noise()
        {
            double random_value = randGenerator.NextDouble() - 0.5; //TODO. Tests it

            switch (noiseType)
            {
                case NoiseType.ePercent:
                    output *= (1.0 + noise_variance * random_value);
                    break;

                case NoiseType.eAbsolute:
                    output += noise_variance * random_value;
                    break;
            }
        }

        private void Bias()
        {
            output += bias;
        }

        private void Drift()
        {
            drift += drift_rate * dt;
            output += drift;
        }

        private void Quantize()
        {
            if (output < min) output = min;
            if (output > max) output = max;
            double portion = output - min;
            quantized = (int)(portion / granularity);
            output = quantized * granularity + min;
        }

        private void Lag()
        {
            // "Output" on the right side of the "=" is the current frame input
            output = ca * (output + PreviousInput) + PreviousOutput * cb;

            PreviousOutput = output;
            PreviousInput = input;
        }

        public override void Bind()
        {
            string tmp = "fcs/" + PropertyManager.MakePropertyName(name, true);
            string tmp_low = tmp + "/malfunction/fail_low";
            string tmp_high = tmp + "/malfunction/fail_high";
            string tmp_stuck = tmp + "/malfunction/fail_stuck";

            fcs.GetPropertyManager().Tie(tmp_low, this.GetFailLow, this.SetFailLow);
            fcs.GetPropertyManager().Tie(tmp_high, this.GetFailHigh, this.SetFailHigh);
            fcs.GetPropertyManager().Tie(tmp_stuck, this.GetFailStuck, this.SetFailStuck);

        }

        private enum NoiseType { ePercent = 0, eAbsolute };

        private NoiseType noiseType = NoiseType.ePercent;
        private double dt;
        private double min = 0.0, max = 0.0;
        private double span = 0.0;
        private double bias = 0.0;
        private double drift_rate = 0.0;
        private double drift = 0.0;
        private double noise_variance = 0.0;
        private double lag = 0.0;
        private double granularity = 0.0;
        private double ca; /// lag filter coefficient "a"
        private double cb; /// lag filter coefficient "b"
        private double PreviousOutput = 0.0;
        private double PreviousInput = 0.0;
        private int noise_type;
        private int bits = 0;
        private int quantized = 0;
        private int divisions = 0;
        private bool fail_low = false;
        private bool fail_high = false;
        private bool fail_stuck = false;


    }
}
