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
    using System.Collections.Generic;
    using System.Xml;
    using CommonUtils.IO;
    using CommonUtils.MathLib;
    using JSBSim.Format;
    using JSBSim.Models;
    // Import log4net classes.
    using log4net;

    /// <summary>
    /// Encapsulates a kinematic(mechanical) component for the flight control
    /// system.This component models the action of a moving effector, such as an
    /// aerosurface or other mechanized entity such as a landing gear strut for the
    /// purpose of effecting vehicle control or configuration. The form of the component
    /// specification is:
    /// 
    /// 
    /// @code
    /// <kinematic name="Gear Control">
    ///   <input> [-]property </input>
    ///   [<noscale/>]
    ///      <traverse>
    ///     <setting>
    ///       <position> number</position>
    ///          <time> number</time>
    ///        </setting>
    ///     ...
    ///   </traverse>
    /// 
    ///      [<clipto>
    ///     <min> {[-] property name | value  </min>
    ///     <max> {[-] property name | value} </max>
    ///   </clipto>]
    ///   [<gain> {property name | value} </gain>]
    ///   [<output> {property} </output>]
    /// </kinematic>
    /// @endcode
    /// 
    /// The detent is the position that the component takes, and the lag is the time it
    /// takes to get to that position from an adjacent setting.For example:
    /// 
    /// @code
    /// <kinematic name="Gear Control">
    ///   <input>gear/gear-cmd-norm</input>
    ///   <traverse>
    ///     <setting>
    ///       <position>0</position>
    ///       <time>0</time>
    ///     </setting>
    ///     <setting>
    ///       <position>1</position>
    ///       <time>5</time>
    ///     </setting>
    ///   </traverse>
    ///   <output>gear/gear-pos-norm</output>
    /// </kinematic>
    /// @endcode
    /// 
    /// In this case, it takes 5 seconds to get to a 1 setting.As this is a software
    /// mechanization of a servo-actuator, there should be an output specified.
    /// 
    /// Positions must be given in ascending order.
    /// 
    /// By default, the input is assumed to be in the range [-1;1] and is scaled to the
    /// value specified in the last <position> tag.This behavior can be modified by
    /// adding a <noscale/> tag to the component definition: in that case, the input
    /// value is directly used to determine the current position of the component.
    /// </summary>
    public class Kinemat : FCSComponent
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


        /// <summary>
        /// Initializer.
        /// Initializes the FGKinemat object from the given configuration
        /// file. The Configuration file is expected to be at the stream
        /// position where the KINEMAT object starts. Also it is expected to
        /// be past the end of the current KINEMAT configuration on exit.
        /// </summary>
        /// <param name="fcs">A reference to the ccurrent flightcontrolsystem.</param>
        /// <param name="element">reference to the current aircraft configuration element</param>
        public Kinemat(FlightControlSystem fcs, XmlElement element)
            : base(fcs, element)
        {
            XmlElement traverse_element, setting_element;
            double tmpDetent;
            double tmpTime;

            detents.Clear();
            transitionTimes.Clear();

            output = 0;
            DoScale = true;

            if (element.FindElement("noscale") != null) DoScale = false;

            traverse_element = element.FindElement("traverse");

            var nodeList = traverse_element.GetElementsByTagName("setting");
            foreach (var elem in nodeList)
            {
                if (elem is XmlElement)
                {
                    setting_element = elem as XmlElement;

                    XmlElement tmpel = setting_element.FindElement("position");
                    tmpDetent = FormatHelper.ValueAsNumber(tmpel);
                    tmpel = setting_element.FindElement("time");
                    tmpTime = FormatHelper.ValueAsNumber(tmpel);
                    detents.Add(tmpDetent);
                    transitionTimes.Add(tmpTime);
                }
            }

            if (detents.Count <= 1)
            {
                log.Error("Kinematic component " + name
                     + " must have more than 1 setting element");
                throw new Exception();
            }

            Bind(element);

            Debug(0);
        }

        /// <summary>
        /// Kinemat output value.
        /// </summary>
        /// <returns>the current output of the kinemat object on the range of [0,1].</returns>
		public override double GetOutputPct()
        {
            return (output - detents[0]) / (detents[detents.Count - 1] - detents[0]);
        }

        /// <summary>
        /// Run method, overwrites FCSComponent.Run()
        /// </summary>
        /// <returns>false on success, true on failure. The routine doing the work.</returns>
        public override bool Run()
        {
            double dt0 = dt;

            input = inputNodes[0].GetDoubleValue();

            if (DoScale) input *= detents[detents.Count-1];

            if ( outputNodes.Count > 0)
                output = (double)outputNodes[0].Get();

            input = MathExt.Constrain(detents[0], input, detents[detents.Count - 1]);

            if (fcs.GetTrimStatus())
                // When trimming the output must be reached in one step
                output = input;
            else
            {
                // Process all detent intervals the movement traverses until either the
                // final value is reached or the time interval has finished.
                while (dt0 > 0.0 && !MathExt.EqualToRoundoff(input, output))
                {

                    // Find the area where Output is in
                    int ind;
                    for (ind = 1; (input < output) ? detents[ind] < output : detents[ind] <= output; ++ind)
                        if (ind >= detents.Count)
                            break;

                    // A transition time of 0.0 means an infinite rate.
                    // The output is reached in one step
                    if (transitionTimes[ind] <= 0.0)
                    {
                        output = input;
                        break;
                    }
                    else
                    {
                        // Compute the rate in this area
                        double Rate = (detents[ind] - detents[ind - 1]) / transitionTimes[ind];
                        // Compute the maximum input value inside this area
                        double ThisInput = MathExt.Constrain(detents[ind - 1], input, detents[ind]);
                        // Compute the time to reach the value in ThisInput
                        double ThisDt = Math.Abs((ThisInput - output) / Rate);

                        // and clip to the timestep size
                        if (dt0 < ThisDt)
                        {
                            ThisDt = dt0;
                            if (output < input)
                                output += ThisDt * Rate;
                            else
                                output -= ThisDt * Rate;
                        }
                        else
                            // Handle this case separate to make shure the termination condition
                            // is met even in inexact arithmetics ...
                            output = ThisInput;

                        dt0 -= ThisDt;
                    }
                }
            }

            Clip();
            SetOutput();

            return true;
        }

        private List<double> detents = new List<double>();
        private List<double> transitionTimes = new List<double>();

        private bool DoScale = true;
    }
}
