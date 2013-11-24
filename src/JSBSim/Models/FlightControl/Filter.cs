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
	using System.Xml;

	// Import log4net classes.
	using log4net;

	using JSBSim.InputOutput;
	using JSBSim.Models;
    using JSBSim.Format;


	/// <summary>
	/// Encapsulates a filter for the flight control system.
    ///The filter component can simulate any first or second order filter. The
    ///Tustin substitution is used to take filter definitions from LaPlace space to the
    ///time domain. The general format for a filter specification is:
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
	public class Filter  : FCSComponent
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

		
		public enum FilterType {Lag, LeadLag, Order2, Washout, Integrator, Unknown} ;

        public Filter(FlightControlSystem fcs, XmlElement element)
            : base(fcs, element)
        {
            double denom;

            dt = fcs.GetState().DeltaTime;

            if (compType == "LAG_FILTER") filterType = FilterType.Lag;
            else if (compType == "LEAD_LAG_FILTER") filterType = FilterType.LeadLag;
            else if (compType == "SECOND_ORDER_FILTER") filterType = FilterType.Order2;
            else if (compType == "WASHOUT_FILTER") filterType = FilterType.Washout;
            else if (compType == "INTEGRATOR") filterType = FilterType.Integrator;
            else filterType = FilterType.Unknown;

            foreach (XmlNode currentNode in element.ChildNodes)
            {
                if (currentNode.NodeType == XmlNodeType.Element)
                {
                    XmlElement currentElement = (XmlElement)currentNode;

                    if (currentElement.LocalName.Equals("c1"))
                    {
                        C1 = FormatHelper.ValueAsNumber(currentElement);
                    }
                    else if (currentElement.LocalName.Equals("c2"))
                    {
                        C1 = FormatHelper.ValueAsNumber(currentElement);

                    }
                    else if (currentElement.LocalName.Equals("c3"))
                    {
                        C1 = FormatHelper.ValueAsNumber(currentElement);

                    }
                    else if (currentElement.LocalName.Equals("c4"))
                    {
                        C1 = FormatHelper.ValueAsNumber(currentElement);

                    }
                    else if (currentElement.LocalName.Equals("c5"))
                    {
                        C1 = FormatHelper.ValueAsNumber(currentElement);

                    }
                    else if (currentElement.LocalName.Equals("c6"))
                    {
                        C1 = FormatHelper.ValueAsNumber(currentElement);

                    }
                    else if (currentElement.LocalName.Equals("trigger"))
                    {
                        trigger = ResolveSymbol(currentElement.InnerText);
                    }
                    /*
                    else
                    {
                        if (log.IsErrorEnabled)
                            log.Error("Error reading Filter. Tag unknown: " + currentElement.LocalName);
                        throw new Exception("Error reading Filter.");
                    }
                    */
                }
            }

            initialize = true;

            switch (filterType)
            {
                case FilterType.Lag:
                    denom = 2.00 + dt * C1;
                    ca = dt * C1 / denom;
                    cb = (2.00 - dt * C1) / denom;
                    break;
                case FilterType.LeadLag:
                    denom = 2.00 * C3 + dt * C4;
                    ca = (2.00 * C1 + dt * C2) / denom;
                    cb = (dt * C2 - 2.00 * C1) / denom;
                    cc = (2.00 * C3 - dt * C4) / denom;
                    break;
                case FilterType.Order2:
                    denom = 4.0 * C4 + 2.0 * C5 * dt + C6 * dt * dt;
                    ca = (4.0 * C1 + 2.0 * C2 * dt + C3 * dt * dt) / denom;
                    cb = (2.0 * C3 * dt * dt - 8.0 * C1) / denom;
                    cc = (4.0 * C1 - 2.0 * C2 * dt + C3 * dt * dt) / denom;
                    cd = (2.0 * C6 * dt * dt - 8.0 * C4) / denom;
                    ce = (4.0 * C4 - 2.0 * C5 * dt + C6 * dt * dt) / denom;
                    break;
                case FilterType.Washout:
                    denom = 2.00 + dt * C1;
                    ca = 2.00 / denom;
                    cb = (2.00 - dt * C1) / denom;
                    break;
                case FilterType.Integrator:
                    ca = dt * C1 / 2.00;
                    break;
                case FilterType.Unknown:
                    if (log.IsErrorEnabled)
                        log.Error("Error reading Filter. Unknown filter type.");
                    throw new Exception("Unknown filter type.");
                    //break;
            }

            base.Bind();
        }
            



		public override bool Run ()
		{
            double test = 0.0;

            if (initialize) 
			{

				previousOutput1 = previousInput1 = output = input;
				initialize = false;

			} 
			else 
			{
                input = inputNodes[0].GetDouble() * inputSigns[0];
				switch (filterType) 
				{
					case FilterType.Lag:
						output = input * ca + previousInput1 * ca + previousOutput1 * cb;
						break;
					case FilterType.LeadLag:
						output = input * ca + previousInput1 * cb + previousOutput1 * cc;
						break;
					case FilterType.Order2:
						output = input * ca + previousInput1 * cb + previousInput2 * cc
							- previousOutput1 * cd - previousOutput2 * ce;
						break;
					case FilterType.Washout:
						output = input * ca - previousInput1 * ca + previousOutput1 * cb;
						break;
					case FilterType.Integrator:
                        if (trigger != null)
                        {
                            test = trigger.GetDouble();
                            if (Math.Abs(test) > 0.000001)
                            {
                                input = previousInput1 = previousInput2 = 0.0;
                            }
                        }
						output = input * ca + previousInput1 * ca + previousOutput1;
						break;
					case FilterType.Unknown:
						break;
				}

			}

			previousOutput2 = previousOutput1;
			previousOutput1 = output;
			previousInput2  = previousInput1;
			previousInput1  = input;

            Clip();
            if (isOutput) SetOutput();

			return true;
		}

		/// <summary>
		/// When true, causes previous values to be set to current values. This
		/// is particularly useful for first pass.
		/// </summary>
		public bool initialize;

        protected FilterType filterType;
		private double dt;
		private double ca;
		private double cb;
		private double cc;
		private double cd;
		private double ce;
        private double C1 = 0;
        private double C2 = 0;
        private double C3 = 0;
        private double C4 = 0;
        private double C5 = 0;
        private double C6 = 0;
		private double previousInput1;
		private double previousInput2;
		private double previousOutput1;
		private double previousOutput2;
		//private ConfigFile AC_cfg;
		private PropertyNode trigger;
	}
}
