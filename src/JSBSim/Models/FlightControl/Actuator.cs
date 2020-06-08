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

using JSBSim.MathValues;
namespace JSBSim.Models.FlightControl
{
    using System;
    using System.Xml;
    using CommonUtils.IO;
    using JSBSim.Models;
    // Import log4net classes.
    using log4net;


    /// <summary>
    /// Encapsulates an Actuator component for the flight control system.
    /// The actuator can be modeled as a "perfect actuator", with the Output
    /// being set directly to the input.The actuator can be made more "real"
    /// by specifying any/all of the following additional effects that can be
    /// applied to the actuator.In order of application to the input signal,
    /// these are:
    ///     
    ///     - System lag(input lag, really)
    ///     - Rate limiting
    ///     - Deadband
    ///     - Hysteresis(mechanical hysteresis)
    ///     - Bias(mechanical bias)
    ///     - Position limiting("hard stops")
    /// 
    /// 
    /// There are also several malfunctions that can be applied to the actuator
    /// by setting a property to true or false (or 1 or 0).
    /// 
    /// Rate limits can be specified either as a single number or property.If a
    /// single<rate_limit> is supplied(with no "sense" attribute) then the
    /// actuator is rate limited at +/- the specified rate limit.If the
    /// <rate_limit> element is supplied with a "sense" attribute of either
    /// "incr[easing]" or "decr[easing]" then the actuator is limited to the
    /// provided numeric or property value) exactly as provided.
    /// 
    /// Syntax:
    /// 
    /// @code
    /// <actuator name="name">
    ///   <input> {[-] property } </input>
    ///   <lag> number</lag>
    ///   [< rate_limit > {property name | value} </rate_limit>]
    ///   [<rate_limit sense="incr"> {property name | value} </rate_limit>
    ///    <rate_limit sense = "decr" > {property name | value} </rate_limit>]
    ///   <bias> number</bias>
    ///   <deadband_width> number</deadband_width>
    ///   <hysteresis_width> number</hysteresis_width>
    ///   [< clipto >
    ///    < min > {property name | value} </min>
    ///    <max> {property name | value} </max>
    ///    </clipto>]
    ///   [<output> {property} </output>]
    /// </actuator>
    /// @endcode
    /// 
    /// Example:
    /// 
    /// @code
    /// <actuator name="fcs/gimbal_pitch_position_radians">
    ///   <input> fcs/gimbal_pitch_command</input>
    ///   <lag> 60 </lag>
    ///   <rate_limit> 0.085 </rate_limit> <!-- 0.085 radians/sec -.
    ///   <bias> 0.002 </bias>
    ///   <deadband_width> 0.002 </deadband_width>
    ///   <hysteresis_width> 0.05 </hysteresis_width>
    ///   <clipto> <!-- +/- 0.17 radians -.
    ///     <min> -0.17 </min>
    ///     <max>  0.17 </max>
    ///    </clipto>
    /// </actuator>
    /// @endcode
    /// 
    /// @author Jon S.Berndt
    /// </summary>
    public class Actuator : FCSComponent
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

        public Actuator(FlightControlSystem fcs, XmlElement element)
            : base(fcs, element)
        {
            // inputs are read from the base class constructor

            PreviousOutput = 0.0;
            PreviousHystOutput = 0.0;
            PreviousRateLimOutput = 0.0;
            PreviousLagInput = PreviousLagOutput = 0.0;
            bias = lag = hysteresis_width = deadband_width = 0.0;
            rate_limit_incr = rate_limit_decr = null; // no limit
            fail_zero = fail_hardover = fail_stuck = false;
            ca = cb = 0.0;
            initialized = false;
            saturated = false;

            if (element.FindElement("deadband_width") != null)
            {
                deadband_width = element.FindElementValueAsNumber("deadband_width");
            }
            if (element.FindElement("hysteresis_width") != null)
            {
                hysteresis_width = element.FindElementValueAsNumber("hysteresis_width");
            }

            // There can be a single rate limit specified, or increasing and 
            // decreasing rate limits specified, and rate limits can be numeric, or
            // a property.
            XmlNodeList nodeList = element.GetElementsByTagName("rate_limit");
            foreach (var node in nodeList)
            {
                XmlElement ratelim_el = node as XmlElement;
                if (ratelim_el != null)
                {
                    string rate_limit_str = ratelim_el.InnerText;
                    Parameter rate_limit = new ParameterValue(rate_limit_str,
                                                                  propertyManager);

                    if (ratelim_el.HasAttribute("sense"))
                    {
                        string sense = ratelim_el.GetAttribute("sense");
                        if (sense.StartsWith("incr"))
                            rate_limit_incr = rate_limit;
                        else if (sense.StartsWith("decr"))
                            rate_limit_decr = rate_limit;
                    }
                    else
                    {
                        rate_limit_incr = rate_limit;
                        rate_limit_decr = rate_limit;
                    }
                }
            }


            if (element.FindElement("bias") != null)
            {
                bias = element.FindElementValueAsNumber("bias");
            }
            if (element.FindElement("lag") != null)
            {
                lag = element.FindElementValueAsNumber("lag");
                double denom = 2.00 + dt * lag;
                ca = dt * lag / denom;
                cb = (2.00 - dt * lag) / denom;
            }

            Bind(element);

            Debug(0);
        }

        /** This function processes the input.
    It calls private functions if needed to perform the hysteresis, lag,
    limiting, etc. functions. */
        public override bool Run()
        {
            input = inputNodes[0].GetDoubleValue();

            if (fcs.GetTrimStatus()) initialized = false;

            if (fail_zero) input = 0;
            if (fail_hardover) input = input < 0.0 ? ClipMin.GetValue() : ClipMax.GetValue();

            output = input; // Perfect actuator. At this point, if no failures are present
                            // and no subsequent lag, limiting, etc. is done, the output
                            // is simply the input. If any further processing is done
                            // (below) such as lag, rate limiting, hysteresis, etc., then
                            // the Input will be further processed and the eventual Output
                            // will be overwritten from this perfect value.

            if (fail_stuck)
            {
                output = PreviousOutput;
            }
            else
            {
                if (lag != 0.0) Lag();        // models actuator lag
                if (rate_limit_incr != null || rate_limit_decr != null) RateLimit();  // limit the actuator rate
                if (deadband_width != 0.0) Deadband();
                if (hysteresis_width != 0.0) Hysteresis();
                if (bias != 0.0) Bias();       // models a finite bias
                if (delay != 0) Delay();      // Model transport latency
            }

            PreviousOutput = output; // previous value needed for "stuck" malfunction

            initialized = true;

            Clip();

            if (clip)
            {
                double clipmax = ClipMax.GetValue();
                saturated = false;

                if (output >= clipmax && clipmax != 0)
                    saturated = true;
                else
                {
                    double clipmin = ClipMin.GetValue();
                    if (output <= clipmin && clipmin != 0)
                        saturated = true;
                }
            }

            SetOutput();

            return true;
        }

        public override void ResetPastStates()
        {
            base.ResetPastStates();

            PreviousOutput = PreviousHystOutput = PreviousRateLimOutput
              = PreviousLagInput = PreviousLagOutput = output = 0.0;
        }

        // these may need to have the bool argument replaced with a double
        /** This function fails the actuator to zero. The motion to zero
            will flow through the lag, hysteresis, and rate limiting
            functions if those are activated. */
        public void SetFailZero(bool set) { fail_zero = set; }
        public void SetFailHardover(bool set) { fail_hardover = set; }
        public void SetFailStuck(bool set) { fail_stuck = set; }

        public bool GetFailZero() { return fail_zero; }
        public bool GetFailHardover() { return fail_hardover; }
        public bool GetFailStuck() { return fail_stuck; }
        public bool IsSaturated() { return saturated; }


        //double span;
        private double bias;
        private IParameter rate_limit_incr;
        private IParameter rate_limit_decr;
        private double hysteresis_width;
        private double deadband_width;
        private double lag;
        private double ca; // lag filter coefficient "a"
        private double cb; // lag filter coefficient "b"
        private double PreviousOutput;
        private double PreviousHystOutput;
        private double PreviousRateLimOutput;
        private double PreviousLagInput;
        private double PreviousLagOutput;
        private bool fail_zero;
        private bool fail_hardover;
        private bool fail_stuck;
        private bool initialized;
        private bool saturated;

        private void Hysteresis()
        {
            // Note: this function acts cumulatively on the "Output" parameter. So,
            // "Output" is - for the purposes of this Hysteresis method - really the input
            // to the method.
            double input = output;

            if (initialized)
            {
                if (input > PreviousHystOutput)
                    output = Math.Max(PreviousHystOutput, input - 0.5 * hysteresis_width);
                else if (input < PreviousHystOutput)
                    output = Math.Max(PreviousHystOutput, input + 0.5 * hysteresis_width);
            }

            PreviousHystOutput = output;
        }

        private void Lag()
        {
            // "Output" on the right side of the "=" is the current frame input
            // for this Lag filter
            double input = output;

            if (initialized)
                output = ca * (input + PreviousLagInput) + PreviousLagOutput * cb;

            PreviousLagInput = input;
            PreviousLagOutput = output;
        }
        private void RateLimit()
        {
            // Note: this function acts cumulatively on the "Output" parameter. So,
            // "Output" is - for the purposes of this RateLimit method - really the input
            // to the method.
            double input = output;
            if (initialized)
            {
                double delta = input - PreviousRateLimOutput;
                if (rate_limit_incr != null)
                {
                    double rate_limit = rate_limit_incr.GetValue();
                    if (delta > dt * rate_limit)
                        output = PreviousRateLimOutput + rate_limit * dt;
                }
                if (rate_limit_decr != null)
                {
                    double rate_limit = -rate_limit_decr.GetValue();
                    if (delta < dt * rate_limit)
                        output = PreviousRateLimOutput + rate_limit * dt;
                }
            }
            PreviousRateLimOutput = output;
        }

        private void Deadband()
        {
            // Note: this function acts cumulatively on the "Output" parameter. So,
            // "Output" is - for the purposes of this Deadband method - really the input
            // to the method.
            double input = output;

            if (input < -deadband_width / 2.0)
            {
                output = (input + deadband_width / 2.0);
            }
            else if (input > deadband_width / 2.0)
            {
                output = (input - deadband_width / 2.0);
            }
            else
            {
                output = 0.0;
            }
        }

        private void Bias()
        {
            output += bias;
        }
        protected override void Bind(XmlElement el) { }
        protected override void Debug(int from) { }
    }
}
