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
    using JSBSim.MathValues;
    using JSBSim.Models;
    // Import log4net classes.
    using log4net;


    /// <summary>
    /// Encapsulates a PID control component for the flight control system.
    /// 
    /// <h3>Configuration Format:</h3>
    /// 
    /// @code
    /// <pid name="{string}" [type = "standard"]>
    ///   <input> {[-] property } </input>
    ///   <kp> {number|[-] property} </kp>
    ///   <ki type = "rect|trap|ab2|ab3" > {number|[-] property} </ki>
    ///   <kd> {number|[-] property} </kd>
    ///   <trigger> {property} </trigger>
    ///   <pvdot> {property} </pvdot>
    /// </pid>
    /// @endcode
    /// 
    /// For the integration constant element, one can also supply the type attribute for
    /// what kind of integrator to be used, one of:
    /// 
    /// - rect, for a rectangular integrator
    /// - trap, for a trapezoidal integrator
    /// - ab2, for a second order Adams Bashforth integrator
    /// - ab3, for a third order Adams Bashforth integrator
    /// 
    /// For example,
    /// 
    /// @code
    /// <pid name = "fcs/heading-control" >
    ///   < input > fcs / heading - error </ input >
    ///   < kp > 3 </ kp >
    ///   < ki type="ab3"> 1 </ki>
    ///   <kd> 1 </kd>
    /// </pid>
    /// @endcode
    /// 
    /// <h3> Configuration Parameters:</h3>
    /// 
    ///   The values of kp, ki, and kd have slightly different interpretations depending
    ///   on whether the PID controller is a standard one, or an ideal/parallel one -
    ///   with the latter being the default.
    /// 
    ///   By default, the PID controller computes the derivative as being the slope of
    ///   the line joining the value of the previous input to the value of the current
    ///   input.However if a better estimation can be determined for the derivative,
    ///   you can provide its value to the PID controller via the property supplied in
    ///   pvdot.
    /// 
    ///   kp      - Proportional constant, default value 0.
    ///   ki      - Integrative constant, default value 0.
    ///   kd      - Derivative constant, default value 0.
    ///   trigger - Property which is used to sense wind-up, optional.Most often, the
    ///             trigger will be driven by the "saturated" property of a particular
    ///             actuator.When the relevant actuator has reached it's limits (if
    ///             there are any, specified by the<clipto> element) the automatically
    ///             generated saturated property will be greater than zero(true). If
    ///            this property is used as the trigger for the integrator, the
    ///            integrator will not continue to integrate while the property is
    /// 
    ///            still true (> 1), preventing wind-up.
    ///             The integrator can also be reset to 0.0 if the property is set to a
    ///             negative value.
    ///   pvdot - The property to be used as the process variable time derivative.
    /// 
    /// @author Jon S.Berndt
    /// </summary>
    public class PID : FCSComponent
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

        public PID(FlightControlSystem fcs, XmlElement element)
            : base(fcs, element)
        {
            XmlElement el;

            I_out_total = 0.0;
            Input_prev = Input_prev2 = 0.0;
            Trigger = null;
            ProcessVariableDot = null;
            IsStandard = false;
            IntType = eIntegrateType.eNone;       // No integrator initially defined.

            string pid_type = element.GetAttribute("type");

            if (pid_type == "standard") IsStandard = true;

            el = element.FindElement("kp");
            if (el != null)
                Kp = new ParameterValue(el, propertyManager);
            else
                Kp = new RealValue(0.0);

            el = element.FindElement("ki");
            if (el != null)
            {
                string integ_type = el.GetAttribute("type");
                if (integ_type == "rect")
                {            // Use rectangular integration
                    IntType = eIntegrateType.eRectEuler;
                }
                else if (integ_type == "trap")
                {     // Use trapezoidal integration
                    IntType = eIntegrateType.eTrapezoidal;
                }
                else if (integ_type == "ab2")
                {      // Use Adams Bashforth 2nd order integration
                    IntType = eIntegrateType.eAdamsBashforth2;
                }
                else if (integ_type == "ab3")
                {      // Use Adams Bashforth 3rd order integration
                    IntType = eIntegrateType.eAdamsBashforth3;
                }
                else
                {                               // Use default Adams Bashforth 2nd order integration
                    IntType = eIntegrateType.eAdamsBashforth2;
                }

                Ki = new ParameterValue(el, propertyManager);
            }
            else
                Ki = new RealValue(0.0);


            el = element.FindElement("kd");
            if (el != null)
                Kd = new ParameterValue(el, propertyManager);
            else
                Kd = new RealValue(0.0);

            el = element.FindElement("pvdot");
            if (el != null)
                ProcessVariableDot = new PropertyValue(el.InnerText, propertyManager);

            el = element.FindElement("trigger");
            if (el != null)
                Trigger = new PropertyValue(el.InnerText, propertyManager);

            Bind(el);
        }
        // ~FGPID();

        public override bool Run()
        {
            double I_out_delta = 0.0;
            double Dval = 0;

            input = inputNodes[0].GetDoubleValue();

            if (ProcessVariableDot != null)
            {
                Dval = ProcessVariableDot.GetValue();
            }
            else
            {
                Dval = (input - Input_prev) / dt;
            }

            // Do not continue to integrate the input to the integrator if a wind-up
            // condition is sensed - that is, if the property pointed to by the trigger
            // element is non-zero. Reset the integrator to 0.0 if the Trigger value
            // is negative.

            double test = 0.0;
            if (Trigger != null) test = Trigger.GetValue();

            if (Math.Abs(test) < 0.000001)
            {
                switch (IntType)
                {
                    case eIntegrateType.eRectEuler:
                        I_out_delta = input;                         // Normal rectangular integrator
                        break;
                    case eIntegrateType.eTrapezoidal:
                        I_out_delta = 0.5 * (input + Input_prev);    // Trapezoidal integrator
                        break;
                    case eIntegrateType.eAdamsBashforth2:
                        I_out_delta = 1.5 * input - 0.5 * Input_prev;  // 2nd order Adams Bashforth integrator
                        break;
                    case eIntegrateType.eAdamsBashforth3:                                   // 3rd order Adams Bashforth integrator
                        I_out_delta = (23.0 * input - 16.0 * Input_prev + 5.0 * Input_prev2) / 12.0;
                        break;
                    case eIntegrateType.eNone:
                        // No integrator is defined or used.
                        I_out_delta = 0.0;
                        break;
                }
            }

            if (test < 0.0) I_out_total = 0.0;  // Reset integrator to 0.0

            I_out_total += Ki.GetValue() * dt * I_out_delta;

            if (IsStandard)
                output = Kp.GetValue() * (input + I_out_total + Kd.GetValue() * Dval);
            else
                output = Kp.GetValue() * input + I_out_total + Kd.GetValue() * Dval;

            Input_prev2 = test < 0.0 ? 0.0 : Input_prev;
            Input_prev = input;

            Clip();
            SetOutput();

            return true;
        }

        public override void ResetPastStates()
        {
            base.ResetPastStates();

            Input_prev = Input_prev2 = output = I_out_total = 0.0;
        }


        /// These define the indices use to select the various integrators.
        public enum eIntegrateType
        {
            eNone = 0, eRectEuler, eTrapezoidal, eAdamsBashforth2,
            eAdamsBashforth3
        };

        void SetInitialOutput(double val)
        {
            I_out_total = val;
            output = val;
        }


        private double I_out_total;
        private double Input_prev, Input_prev2;

        private bool IsStandard;

        eIntegrateType IntType;

        private IParameter Kp, Ki, Kd, Trigger, ProcessVariableDot;

        protected override void Bind(XmlElement el) { }
        protected override void Debug(int from) { }
    }
}
