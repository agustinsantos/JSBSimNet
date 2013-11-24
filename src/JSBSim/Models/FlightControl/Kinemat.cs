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
	using System.Collections;
    using System.Collections.Generic;

	// Import log4net classes.
	using log4net;

	using JSBSim.InputOutput;
	using CommonUtils.MathLib;
	using JSBSim.Models;
    using JSBSim.Format;

	/// <summary>
	/// Encapsulates a kinematic component for the flight control system.
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
            XmlElement traverse_element;
            double tmpDetent;
            double tmpTime;

            output = 0.0;
            DoScale = true;

            XmlNodeList childs = element.GetElementsByTagName("noscale");
            if (childs != null && childs.Count > 0)
                DoScale = false;

            traverse_element = element.GetElementsByTagName("traverse")[0] as XmlElement;
            XmlNodeList settingsElements = traverse_element.GetElementsByTagName("setting");
            foreach (XmlElement setting_element in settingsElements)
            {
                childs = setting_element.GetElementsByTagName("position");
                tmpDetent = FormatHelper.ValueAsNumber(childs[0] as XmlElement);
                childs = setting_element.GetElementsByTagName("time");
                tmpTime = FormatHelper.ValueAsNumber(childs[0] as XmlElement);
                detents.Add(tmpDetent);
                transitionTimes.Add(tmpTime);
            }
            NumDetents = detents.Count;

            if (NumDetents <= 1)
            {
                if (log.IsErrorEnabled)
                    log.Error("Kinematic component " + name + " must have more than 1 setting element");

                throw new Exception("Kinematic component must have more than 1 setting element");
            }

            base.Bind();
        }

        /// <summary>
        /// Kinemat output value.
        /// </summary>
        /// <returns>the current output of the kinemat object on the range of [0,1].</returns>
		public override double GetOutputPct() { return OutputPct; }
    
        /// <summary>
        /// Run method, overwrites FCSComponent.Run()
        /// </summary>
        /// <returns>false on success, true on failure. The routine doing the work.</returns>
		public override bool Run ()
		{
			double dt = fcs.GetState().DeltaTime;

            input = inputNodes[0].GetDouble() * inputSigns[0];

			if (DoScale) 
                input *= detents[NumDetents-1];

            if (isOutput) 
                output = outputNode.GetDouble();

			if (input < detents[0])
				input = detents[0];
			else if (detents[NumDetents-1] < input)
				input = detents[NumDetents-1];

			// Process all detent intervals the movement traverses until either the
			// final value is reached or the time interval has finished.
			while ( 0.0 < dt && !MathExt.EqualToRoundoff(input, output) )
			{

				// Find the area where Output is in
				int ind;
				for (ind = 1; (input < output) ? detents[ind] < output : detents[ind] <= output ; ++ind)
					if (NumDetents <= ind)
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
					double Rate = (detents[ind] - (detents[ind-1])/transitionTimes[ind]);
					// Compute the maximum input value inside this area
					double ThisInput = input;
					if (ThisInput < detents[ind-1])   ThisInput = detents[ind-1];
					if (detents[ind] < ThisInput)     ThisInput = detents[ind];
					// Compute the time to reach the value in ThisInput
					double ThisDt = Math.Abs((ThisInput-output)/Rate);

					// and clip to the timestep size
					if (dt < ThisDt) 
					{
						ThisDt = dt;
						if (output < input)
							output += ThisDt*Rate;
						else
							output -= ThisDt*Rate;
					} 
					else
						// Handle this case separate to make shure the termination condition
						// is met even in inexact arithmetics ...
						output = ThisInput;

					dt -= ThisDt;
				}
			}

			OutputPct = (output-detents[0])/(detents[NumDetents-1]-detents[0]);

            Clip();
            if (isOutput) SetOutput();

			return true;
		}

        private List<double> detents = new List<double>();
        private List<double> transitionTimes = new List<double>();
		private int NumDetents;
        private double OutputPct = 0.0;
		private bool  DoScale = true;
	}
}
