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
    using JSBSim.InputOutput;
    // Import log4net classes.
    using log4net;

    /// <summary>
    /// Encapsulates a Sensor component for the flight control system.
    /// 
    /// Syntax:
    /// 
    /// @code
    /// <sensor name="name">
    ///   <input> property </input>
    ///   <lag> number </lag>
    ///   <noise [variation="PERCENT|ABSOLUTE"] [distribution="UNIFORM|GAUSSIAN"]> number </noise>
    ///   <quantization name = "name" >
    ///     < bits > number </ bits >
    ///     < min > number </ min >
    ///     < max > number </ max >
    ///   </ quantization >
    ///   < drift_rate > number </ drift_rate >
    ///   < gain > number </ gain >
    ///   < bias > number </ bias >
    ///   < delay[type = "time|frames"] > number < / delay >
    /// </ sensor >
    /// @endcode
    /// 
    /// Example:
    /// 
    /// @code
    /// <sensor name="aero/sensor/qbar">
    ///   <input> aero/qbar</input>
    ///   <lag> 0.5 </lag>
    ///   <noise variation = "PERCENT" > 2 </ noise >
    ///   < quantization name="aero/sensor/quantized/qbar">
    ///     <bits> 12 </bits>
    ///     <min> 0 </min>
    ///     <max> 400 </max>
    ///   </quantization>
    ///   <bias> 0.5 </bias>
    /// </sensor>
    /// @endcode
    /// 
    /// The only required element in the sensor definition is the input element.In that
    /// case, no degradation would be modeled, and the output would simply be the input.
    /// 
    /// Noise can be Gaussian or uniform, and the noise can be applied as a factor
    /// (PERCENT) or additively (ABSOLUTE). The noise that can be applied at each frame
    /// of the simulation execution is calculated as a random factor times a noise value
    /// that is specified in the config file.When the noise distribution type is
    /// Gaussian, the random number can be between roughly -3 and +3 for a span of six
    /// sigma.When the distribution type is UNIFORM, the random value can be between
    /// -1.0 and +1.0. This random value is multiplied against the specified noise to
    /// arrive at a random noise value for the frame. If the noise type is PERCENT, then
    /// random noise value is added to one, and that sum is then multiplied against the
    /// input signal for the sensor. In this case, the specified noise value in the
    /// config file would be expected to actually be a percent value, such as 0.05 (for
    /// a 5% variance). If the noise type is ABSOLUTE, then the random noise value
    /// specified in the config file is understood to be an absolute value of noise to
    /// be added to the input signal instead of being added to 1.0 and having that sum
    /// be multiplied against the input signal as in the PERCENT type.For the ABSOLUTE
    /// noise case, the noise number specified in the config file could be any number.
    /// 
    /// If the type is ABSOLUTE, then the noise number times the random number is added
    /// to the input signal instead of being multiplied against it as with the PERCENT
    /// type of noise.
    /// 
    /// The delay element can specify a frame delay.The integer number provided is the
    /// number of frames to delay the output signal.
    /// 
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
            // inputs are read from the base class constructor

            bits = quantized = divisions = 0;
            PreviousInput = PreviousOutput = 0.0;
            min = max = bias = gain = noise_variance = lag = drift_rate = drift = span = 0.0;
            granularity = 0.0;
            noise_type = 0;
            fail_low = fail_high = fail_stuck = false;

            XmlElement quantization_element = element.FindElement("quantization");
            if (quantization_element != null)
            {
                if (quantization_element.FindElement("bits") != null)
                {
                    bits = (int)quantization_element.FindElementValueAsNumber("bits");
                }
                divisions = (1 << bits);
                if (quantization_element.FindElement("min") != null)
                {
                    min = quantization_element.FindElementValueAsNumber("min");
                }
                if (quantization_element.FindElement("max") != null)
                {
                    max = quantization_element.FindElementValueAsNumber("max");
                }
                quant_property = quantization_element.GetAttribute("name");
                span = max - min;
                granularity = span / divisions;
            }
            if (element.FindElement("bias") != null)
            {
                bias = element.FindElementValueAsNumber("bias");
            }
            if (element.FindElement("gain") != null)
            {
                gain = element.FindElementValueAsNumber("gain");
            }
            if (element.FindElement("drift_rate") != null)
            {
                drift_rate = element.FindElementValueAsNumber("drift_rate");
            }
            if (element.FindElement("lag") != null)
            {
                lag = element.FindElementValueAsNumber("lag");
                double denom = 2.00 + dt * lag;
                ca = dt * lag / denom;
                cb = (2.00 - dt * lag) / denom;
            }
            if (element.FindElement("noise") != null)
            {
                noise_variance = element.FindElementValueAsNumber("noise");
                string variation = element.FindElement("noise").GetAttribute("variation");
                if (variation == "PERCENT")
                {
                    NoiseType = eNoiseType.ePercent;
                }
                else if (variation == "ABSOLUTE")
                {
                    NoiseType = eNoiseType.eAbsolute;
                }
                else
                {
                    NoiseType = eNoiseType.ePercent;
                    log.Error("Unknown noise type in sensor: " + name);
                    log.Error("  defaulting to PERCENT.");
                }
                string distribution = element.FindElement("noise").GetAttribute("distribution");
                if (distribution == "UNIFORM")
                {
                    DistributionType = eDistributionType.eUniform;
                }
                else if (distribution == "GAUSSIAN")
                {
                    DistributionType = eDistributionType.eGaussian;
                }
                else
                {
                    DistributionType = eDistributionType.eUniform;
                    log.Error("Unknown random distribution type in sensor: " + name);
                    log.Error("  defaulting to UNIFORM.");
                }
            }

            Bind(element);

            Debug(0);
        }

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
            input = inputNodes[0].GetDoubleValue();

            ProcessSensorSignal();

            SetOutput();

            return true;
        }

        public override void ResetPastStates()
        {
            base.ResetPastStates();

            PreviousOutput = PreviousInput = output = 0.0;
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

        protected enum eNoiseType { ePercent = 0, eAbsolute }
        protected eNoiseType NoiseType;
        protected enum eDistributionType { eUniform = 0, eGaussian }
        protected eDistributionType DistributionType;
        protected double min, max;
        protected double span;
        protected double bias;
        protected double gain;
        protected double drift_rate;
        protected double drift;
        protected double noise_variance;
        protected double lag;
        protected double granularity;
        protected double ca; /// lag filter coefficient "a"
        protected double cb; /// lag filter coefficient "b"
        protected double PreviousOutput;
        protected double PreviousInput;
        protected int noise_type;
        protected int bits;
        protected int quantized;
        protected int divisions;
        protected bool fail_low;
        protected bool fail_high;
        protected bool fail_stuck;
        protected string quant_property;

        protected void ProcessSensorSignal()
        {
            // Degrade signal as specified

            if (!fail_stuck)
            {
                output = input; // perfect sensor

                if (lag != 0.0) Lag();       // models sensor lag and filter
                if (noise_variance != 0.0) Noise();     // models noise
                if (drift_rate != 0.0) Drift();     // models drift over time
                if (gain != 0.0) Gain();      // models a finite gain
                if (bias != 0.0) Bias();      // models a finite bias

                if (delay != 0) Delay();     // models system signal transport latencies

                if (fail_low) output = double.MinValue;
                if (fail_high) output = double.MaxValue;

                if (bits != 0) Quantize();  // models quantization degradation

                Clip();
            }
        }
        protected void Noise()
        {
            double random_value = 0.0;

            if (DistributionType == eDistributionType.eUniform)
            {
                random_value = MathExt.Rand();
            }
            else
            {
                random_value = MathExt.GaussianRandomNumber();
            }

            switch (NoiseType)
            {
                case eNoiseType.ePercent:
                    output *= (1.0 + noise_variance * random_value);
                    break;

                case eNoiseType.eAbsolute:
                    output += noise_variance * random_value;
                    break;
            }
        }

        protected void Bias()
        {
            output += bias;
        }

        protected void Drift()
        {
            drift += drift_rate * dt;
            output += drift;
        }

        protected void Quantize()
        {
            if (output < min) output = min;
            if (output > max) output = max;
            double portion = output - min;
            quantized = (int)(portion / granularity);
            output = quantized * granularity + min;
        }

        protected void Lag()
        {
            // "Output" on the right side of the "=" is the current input
            output = ca * (output + PreviousInput) + PreviousOutput * cb;

            PreviousOutput = output;
            PreviousInput = input;
        }

        protected void Gain()
        {
            output *= gain;
        }

        protected override void Bind(XmlElement el)
        {
            // TODO
        }
    }
}
