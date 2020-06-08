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
    using CommonUtils.IO;
    using JSBSim.MathValues;
    using JSBSim.Models;
    // Import log4net classes.
    using log4net;


    /// <summary>
    /// Encapsulates a filter for the flight control system.
    /// The filter component can simulate any first or second order filter. The
    /// Tustin substitution is used to take filter definitions from LaPlace space to the
    /// time domain. The general format for a filter specification is:
    ///
    ///<code> 
    ///<typename name="name">
    ///  <input> property </input>
    ///  <c1> value </c1>
    ///  [<c2> value </c2>]
    ///  [<c3> value </c3>]
    ///  [<c4> value </c4>]
    ///  [<c5> value </c5>]
    ///  [<c6> value </c6>]
    ///  [<clipto>
    ///    <min> {[-]property name | value} </min>
    ///    <max> {[-]property name | value} </max>
    ///  </clipto>]
    ///  [<output> property </output>]
    ///</typename>
    ///</code>
    ///
    /// For a lag filter of the form,
    ///<code>
    ///  C1
    ///------
    ///s + C1
    ///</code>
    ///
    ///the corresponding filter definition is:
    ///
    ///<code>
    ///<lag_filter name="name">
    ///  <input> property </input>
    ///  <c1> value </c1>
    ///  [<clipto>
    ///    <min> {[-]property name | value} </min>
    ///    <max> {[-]property name | value} </max>
    ///  </clipto>]
    ///  [<output> property <output>]
    ///</lag_filter>
    ///</code>
    ///
    //As an example, for the specific filter:
    ///
    ///<code>
    ///  600
    ///------
    ///s + 600
    ///</code>
    ///
    ///the corresponding filter definition could be:
    ///
    ///<code>
    ///<lag_filter name="Heading Roll Error Lag">
    ///  <input> fcs/heading-command </input>
    ///  <c1> 600 </c1>
    ///</lag_filter>
    ///</code>
    ///
    ///For a lead-lag filter of the form:
    ///
    ///<code>
    ///C1*s + C2
    ///---------
    ///C3*s + C4
    ///</code>
    ///
    ///The corresponding filter definition is:
    ///
    ///<code>
    ///<lead_lag_filter name="name">
    ///  <input> property </input>
    ///  <c1> value <c/1>
    ///  <c2> value <c/2>
    ///  <c3> value <c/3>
    ///  <c4> value <c/4>
    ///  [<clipto>
    ///    <min> {[-]property name | value} </min>
    ///    <max> {[-]property name | value} </max>
    ///  </clipto>]
    ///  [<output> property </output>]
    ///</lead_lag_filter>
    ///</code>
    ///
    ///For a washout filter of the form:
    ///
    ///<code>
    ///  s
    ///------
    ///s + C1
    ///</code>
    ///
    ///The corresponding filter definition is:
    ///
    ///<code>
    ///<washout_filter name="name">
    ///  <input> property </input>
    ///  <c1> value </c1>
    ///  [<clipto>
    ///    <min> {[-]property name | value} </min>
    ///    <max> {[-]property name | value} </max>
    ///  </clipto>]
    ///  [<output> property </output>]
    ///</washout_filter>
    ///</code>
    ///
    ///For a second order filter of the form:
    ///
    ///<code>
    ///C1*s^2 + C2*s + C3
    ///------------------
    ///C4*s^2 + C5*s + C6
    ///</code>
    ///
    ///The corresponding filter definition is:
    ///
    ///<code>
    ///<second_order_filter name="name">
    ///  <input> property </input>
    ///  <c1> value </c1>
    ///  <c2> value </c2>
    ///  <c3> value </c3>
    ///  <c4> value </c4>
    ///  <c5> value </c5>
    ///  <c6> value </c6>
    ///  [<clipto>
    ///    <min> {[-]property name | value} </min>
    ///    <max> {[-]property name | value} </max>
    ///  </clipto>]
    ///  [<output> property </output>]
    ///</second_order_filter>
    ///</code>
    ///
    ///For an integrator of the form:
    ///
    ///<code>
    /// C1
    /// ---
    ///  s
    ///</code>
    ///
    ///The corresponding filter definition is:
    ///
    ///<code>
    ///<integrator name="name">
    ///  <input> property </input>
    ///  <c1> value </c1>
    ///  [<trigger> property </trigger>]
    ///  [<clipto>
    ///    <min> {[-]property name | value} </min>
    ///    <max> {[-]property name | value} </max>
    ///  </clipto>]
    ///  [<output> property </output>]
    ///</integrator>
    ///</code>
    ///
    ///For the integrator, the trigger features the following behavior. If the trigger
    ///property value is:
    ///  - 0: no action is taken - the output is calculated normally
    ///  - not 0: (or simply greater than zero), all current and previous inputs will
    ///           be set to 0.0
    ///
    ///In all the filter specifications above, an \<output> element is also seen.  This
    ///is so that the last component in a "string" can copy its value to the appropriate
    ///output, such as the elevator, or speedbrake, etc.
    /// </summary>
    public class Filter : FCSComponent
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

        public Filter(FlightControlSystem fcs, XmlElement element)
            : base(fcs, element)
        {
            DynamicFilter = false; initialize = true;
            C[1] = C[2] = C[3] = C[4] = C[5] = C[6] = null;
            for (int i = 1; i < 7; i++)
                ReadFilterCoefficients(element, i);

            if (compType == "LAG_FILTER") FilterType = FilterTypeEnum.Lag;
            else if (compType == "LEAD_LAG_FILTER") FilterType = FilterTypeEnum.LeadLag;
            else if (compType == "SECOND_ORDER_FILTER") FilterType = FilterTypeEnum.Order2;
            else if (compType == "WASHOUT_FILTER") FilterType = FilterTypeEnum.Washout;
            else FilterType = FilterTypeEnum.Unknown;

            CalculateDynamicFilters();

            Bind(element);

            Debug(0);
        }

        public override bool Run()
        {
            if (initialize)
            {

                PreviousOutput2 = PreviousInput2 = PreviousOutput1 = PreviousInput1 = output = input;
                initialize = false;

            }
            else
            {

                input = inputNodes[0].GetDoubleValue();

                if (DynamicFilter) CalculateDynamicFilters();

                switch (FilterType)
                {
                    case FilterTypeEnum.Lag:
                        output = (input + PreviousInput1) * ca + PreviousOutput1 * cb;
                        break;
                    case FilterTypeEnum.LeadLag:
                        output = input * ca + PreviousInput1 * cb + PreviousOutput1 * cc;
                        break;
                    case FilterTypeEnum.Order2:
                        output = input * ca + PreviousInput1 * cb + PreviousInput2 * cc
                                            - PreviousOutput1 * cd - PreviousOutput2 * ce;
                        break;
                    case FilterTypeEnum.Washout:
                        output = input * ca - PreviousInput1 * ca + PreviousOutput1 * cb;
                        break;
                    case FilterTypeEnum.Unknown:
                        break;
                }

            }

            PreviousOutput2 = PreviousOutput1;
            PreviousOutput1 = output;
            PreviousInput2 = PreviousInput1;
            PreviousInput1 = input;

            Clip();
            SetOutput();

            return true;
        }
        public override void ResetPastStates()
        {
            base.ResetPastStates();

            input = 0.0; initialize = true;
        }

        protected void CalculateDynamicFilters()
        {
            double denom;

            switch (FilterType)
            {
                case FilterTypeEnum.Lag:
                    denom = 2.0 + dt * C[1].GetDoubleValue();
                    ca = dt * C[1].GetDoubleValue() / denom;
                    cb = (2.0 - dt * C[1].GetDoubleValue()) / denom;

                    break;
                case FilterTypeEnum.LeadLag:
                    denom = 2.0 * C[3].GetDoubleValue() + dt * C[4].GetDoubleValue();
                    ca = (2.0 * C[1].GetDoubleValue() + dt * C[2].GetDoubleValue()) / denom;
                    cb = (dt * C[2].GetDoubleValue() - 2.0 * C[1].GetDoubleValue()) / denom;
                    cc = (2.0 * C[3].GetDoubleValue() - dt * C[4].GetDoubleValue()) / denom;
                    break;
                case FilterTypeEnum.Order2:
                    denom = 4.0 * C[4].GetDoubleValue() + 2.0 * C[5].GetDoubleValue() * dt + C[6].GetDoubleValue() * dt * dt;
                    ca = (4.0 * C[1].GetDoubleValue() + 2.0 * C[2].GetDoubleValue() * dt + C[3].GetDoubleValue() * dt * dt) / denom;
                    cb = (2.0 * C[3].GetDoubleValue() * dt * dt - 8.0 * C[1].GetDoubleValue()) / denom;
                    cc = (4.0 * C[1].GetDoubleValue() - 2.0 * C[2].GetDoubleValue() * dt + C[3].GetDoubleValue() * dt * dt) / denom;
                    cd = (2.0 * C[6].GetDoubleValue() * dt * dt - 8.0 * C[4].GetDoubleValue()) / denom;
                    ce = (4.0 * C[4].GetDoubleValue() - 2.0 * C[5].GetDoubleValue() * dt + C[6].GetDoubleValue() * dt * dt) / denom;
                    break;
                case FilterTypeEnum.Washout:
                    denom = 2.0 + dt * C[1].GetDoubleValue();
                    ca = 2.0 / denom;
                    cb = (2.0 - dt * C[1].GetDoubleValue()) / denom;
                    break;
                case FilterTypeEnum.Unknown:
                    log.Error("Unknown filter type");
                    break;
            }

        }
        protected void ReadFilterCoefficients(XmlElement element, int index)
        {
            // index is known to be 1-7. 
            string coefficient = "c" + index;

            if (element.FindElement(coefficient) != null)
            {
                C[index] = new ParameterValue(element.FindElement(coefficient),
                                                propertyManager);
                DynamicFilter |= !C[index].IsConstant();
            }
        }

        protected override void Debug(int from) { }
        protected enum FilterTypeEnum { Lag, LeadLag, Order2, Washout, Integrator, Unknown };
        protected FilterTypeEnum FilterType;
        private bool DynamicFilter;
        /// <summary>
        /// When true, causes previous values to be set to current values. This
        /// is particularly useful for first pass.
        /// </summary>
        private bool initialize;
        private double ca, cb, cc, cd, ce;
        private Parameter[] C = new Parameter[7]; // There are 6 coefficients, indexing is "1" based.
        private double PreviousInput1, PreviousInput2;
        private double PreviousOutput1, PreviousOutput2;
    }
}
