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

namespace JSBSim.Models
{
	using System;
	using System.Collections;
    using System.Collections.Generic;
	using System.Xml;
	using System.IO;

	// Import log4net classes.
	using log4net;

	using JSBSim.InputOutput;
    using JSBSim.Format;
	using CommonUtils.IO;
	using CommonUtils.MathLib;


	/// <summary>
	/// Handles simulation output.
	/// OUTPUT section definition
	/// 
	/// The following specifies the way that JSBSim writes out data.
	/// 
	/// NAME is the filename you want the output to go to
	/// 
	/// TYPE can be:
	/// CSV       Comma separated data. If a filename is supplied then the data
	/// goes to that file. If COUT or cout is specified, the data goes
	/// to stdout. If the filename is a null filename the data goes to
	/// stdout, as well.
	/// SOCKET    Will eventually send data to a socket output, where NAME
	/// would then be the IP address of the machine the data should be
	/// sent to. DON'T USE THIS YET!
	/// TABULAR   Columnar data. NOT IMPLEMENTED YET!
	/// TERMINAL  Output to terminal. NOT IMPLEMENTED YET!
	/// NONE      Specifies to do nothing. THis setting makes it easy to turn on and
	/// off the data output without having to mess with anything else.
	/// 
	/// The arguments that can be supplied, currently, are
	/// 
	/// RATE_IN_HZ  An integer rate in times-per-second that the data is output. This
	/// value may not be *exactly* what you want, due to the dependence
	/// on dt, the cycle rate for the FDM.
	/// 
	/// The following parameters tell which subsystems of data to output:
	/// 
	/// SIMULATION       ON|OFF
	/// ATMOSPHERE       ON|OFF
	/// MASSPROPS        ON|OFF
	/// AEROSURFACES     ON|OFF
	/// RATES            ON|OFF
	/// VELOCITIES       ON|OFF
	/// FORCES           ON|OFF
	/// MOMENTS          ON|OFF
	/// POSITION         ON|OFF
	/// COEFFICIENTS     ON|OFF
	/// GROUND_REACTIONS ON|OFF
	/// FCS              ON|OFF
	/// PROPULSION       ON|OFF
	/// 
	/// NOTE that Time is always output with the data
	/// </summary>
	public class Output : Model
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

		public Output(FDMExecutive fdmex): base(fdmex)
		{
			Name = "Output";
			sFirstPass = dFirstPass = true;
			//TODO socket = 0;
			outputType = OutputType.None;
			filename = "DefaultOutput.csv";
			enabled = true;
			delimeter = ", ";
		}

        public override bool Run()
        {
            //if (log.IsInfoEnabled)
            //    log.Info("Entering Run() for model " + name + "rate =" + rate);

            if (InternalRun()) return true;
            if (enabled && !FDMExec.State.IsIntegrationSuspended && !FDMExec.Holding())
            {
                if (outputType == OutputType.Socket)
                {
                    // Not done yet
                    //if (log.IsWarnEnabled)
                    //    log.Warn("Socket output is Not Implemented");
                }
                else if (outputType == OutputType.CSV || outputType == OutputType.Tab)
                {
                    DelimitedOutput(filename); //TODO
                }
                else if (outputType == OutputType.Terminal)
                {
                    // Not done yet
                    throw new NotImplementedException("Terminal output");
                }
                else if (outputType == OutputType.Log4Net)
                {
                    Log4NetOutput();

                }
                else if (outputType == OutputType.None)
                {
                    // Do nothing
                }
                else
                {
                    // Not a valid type of output
                }
            }
            return false;
        }

		private StreamWriter outstream;
		private void DelimitedOutput(string fname)
		{
			string scratch = "";

			if (dFirstPass) 
			{
				outstream = new StreamWriter(fname);

				outstream.Write("Time");
				if (SubSystems[(int)eSubSystems.Simulation]) 
				{
					// Nothing here, yet
				}
				if (SubSystems[(int)eSubSystems.Aerosurfaces]) 
				{
					outstream.Write(delimeter);
					outstream.Write("Aileron Cmd" + delimeter);
					outstream.Write("Elevator Cmd" + delimeter);
					outstream.Write("Rudder Cmd" + delimeter);
					outstream.Write("Flap Cmd" + delimeter);
					outstream.Write("Left Aileron Pos" + delimeter);
					outstream.Write("Right Aileron Pos" + delimeter);
					outstream.Write("Elevator Pos" + delimeter);
					outstream.Write("Rudder Pos" + delimeter);
					outstream.Write("Flap Pos");
				}
				if (SubSystems[(int)eSubSystems.Rates]) 
				{
					outstream.Write(delimeter);
					outstream.Write("P" + delimeter + "Q" + delimeter + "R" + delimeter);
					outstream.Write("Pdot" + delimeter + "Qdot" + delimeter + "Rdot");
				}
				if (SubSystems[(int)eSubSystems.Velocities]) 
				{
					outstream.Write(delimeter);
					outstream.Write("QBar" + delimeter);
					outstream.Write("Vtotal" + delimeter);
					outstream.Write("UBody" + delimeter + "VBody" + delimeter + "WBody" + delimeter);
					outstream.Write("UAero" + delimeter + "VAero" + delimeter + "WAero" + delimeter);
					outstream.Write("Vn" + delimeter + "Ve" + delimeter + "Vd");
				}
				if (SubSystems[(int)eSubSystems.Forces]) 
				{
					outstream.Write(delimeter);
					outstream.Write("Drag" + delimeter + "Side" + delimeter + "Lift" + delimeter);
					outstream.Write("L/D" + delimeter);
					outstream.Write("Xforce" + delimeter + "Yforce" + delimeter + "Zforce");
				}
				if (SubSystems[(int)eSubSystems.Moments]) 
				{
					outstream.Write(delimeter);
					outstream.Write("L" + delimeter + "M" + delimeter + "N");
				}
				if (SubSystems[(int)eSubSystems.Atmosphere]) 
				{
					outstream.Write(delimeter);
					outstream.Write("Rho" + delimeter);
					outstream.Write("NWind" + delimeter + "EWind" + delimeter + "DWind");
				}
				if (SubSystems[(int)eSubSystems.MassProps]) 
				{
					outstream.Write(delimeter);
					outstream.Write("Ixx" + delimeter);
					outstream.Write("Ixy" + delimeter);
					outstream.Write("Ixz" + delimeter);
					outstream.Write("Iyx" + delimeter);
					outstream.Write("Iyy" + delimeter);
					outstream.Write("Iyz" + delimeter);
					outstream.Write("Izx" + delimeter);
					outstream.Write("Izy" + delimeter);
					outstream.Write("Izz" + delimeter);
					outstream.Write("Mass" + delimeter);
					outstream.Write("Xcg" + delimeter + "Ycg" + delimeter + "Zcg");
				}
				if (SubSystems[(int)eSubSystems.Propagate]) 
				{
					outstream.Write(delimeter);
					outstream.Write("Altitude" + delimeter);
					outstream.Write("Phi" + delimeter + "Tht" + delimeter + "Psi" + delimeter);
					outstream.Write("Alpha" + delimeter);
					outstream.Write("Beta" + delimeter);
					outstream.Write("Latitude (Deg)" + delimeter);
					outstream.Write("Longitude (Deg)" + delimeter);
					outstream.Write("Distance AGL" + delimeter);
					outstream.Write("Runway Radius");
				}
				if (SubSystems[(int)eSubSystems.Coefficients]) 
				{
					scratch = FDMExec.Aerodynamics.GetCoefficientStrings(delimeter);
					if (scratch.Length != 0) outstream.Write(delimeter + scratch);
				}
				if (SubSystems[(int)eSubSystems.FCS]) 
				{
					scratch = FDMExec.FlightControlSystem.GetComponentStrings(delimeter);
					if (scratch.Length != 0) outstream.Write(delimeter + scratch);
				}
				if (SubSystems[(int)eSubSystems.GroundReactions]) 
				{
					outstream.Write(delimeter);
					outstream.Write(FDMExec.GroundReactions.GetGroundReactionStrings(delimeter));
				}
				if (SubSystems[(int)eSubSystems.Propulsion] && FDMExec.Propulsion.GetNumEngines() > 0) 
				{
					outstream.Write(delimeter);
					outstream.Write(FDMExec.Propulsion.GetPropulsionStrings(delimeter));
				}
				if (outputProperties.Count > 0) 
				{
					foreach (PropertyNode prop in outputProperties) 
					{
						outstream.Write(delimeter + prop.ShortName);
					}
				}

				outstream.WriteLine();
				dFirstPass = false;
			}

			outstream.Write(FDMExec.State.SimTime.ToString(formatDouble, provider));
			if (SubSystems[(int)eSubSystems.Simulation]) 
			{
			}
			if (SubSystems[(int)eSubSystems.Aerosurfaces]) 
			{
				outstream.Write(delimeter);
				outstream.Write(FDMExec.FlightControlSystem.AileronCmd.ToString(formatDouble, provider) + delimeter);
				outstream.Write(FDMExec.FlightControlSystem.ElevatorCmd.ToString(formatDouble, provider) + delimeter);
				outstream.Write(FDMExec.FlightControlSystem.RudderCmd.ToString(formatDouble, provider) + delimeter);
				outstream.Write(FDMExec.FlightControlSystem.FlapsCmd.ToString(formatDouble, provider) + delimeter);
				outstream.Write(FDMExec.FlightControlSystem.LeftAileronPositionRadians.ToString(formatDouble, provider) + delimeter);
				outstream.Write(FDMExec.FlightControlSystem.GetDaRPosRadians().ToString(formatDouble, provider) + delimeter);
				outstream.Write(FDMExec.FlightControlSystem.GetDePosRadians().ToString(formatDouble, provider) + delimeter);
				outstream.Write(FDMExec.FlightControlSystem.GetDrPosRadians().ToString(formatDouble, provider) + delimeter);
				outstream.Write(FDMExec.FlightControlSystem.GetDfPosRadians().ToString(formatDouble, provider));
			}
			if (SubSystems[(int)eSubSystems.Rates]) 
			{
				outstream.Write(delimeter);
				outstream.Write(FDMExec.Propagate.GetPQR().ToString(formatDouble, provider) + delimeter);
				outstream.Write(FDMExec.Propagate.GetPQRdot().ToString(formatDouble, provider));
			}
			if (SubSystems[(int)eSubSystems.Velocities]) 
			{
				outstream.Write(delimeter);
				outstream.Write(FDMExec.Auxiliary.Qbar.ToString(formatDouble, provider) + delimeter);
				outstream.Write(FDMExec.Auxiliary.Vt.ToString(formatDouble, provider) + delimeter);
				outstream.Write(FDMExec.Propagate.GetUVW().ToString(formatDouble, provider) + delimeter);
				outstream.Write(FDMExec.Auxiliary.GetAeroUVW().ToString(formatDouble, provider) + delimeter);
				outstream.Write(FDMExec.Propagate.GetVel().ToString(formatDouble, provider));
			}
			if (SubSystems[(int)eSubSystems.Forces]) 
			{
				outstream.Write(delimeter);
				outstream.Write(FDMExec.Aerodynamics.ForcesDragSideLift.ToString(formatDouble, provider) + delimeter);
				outstream.Write(FDMExec.Aerodynamics.LoD.ToString(formatDouble, provider) + delimeter);
				outstream.Write(FDMExec.Aircraft.Forces.ToString(formatDouble, provider));
			}
			if (SubSystems[(int)eSubSystems.Moments]) 
			{
				outstream.Write(delimeter);
				outstream.Write(FDMExec.Aircraft.Moments.ToString(formatDouble, provider));
			}
			if (SubSystems[(int)eSubSystems.Atmosphere]) 
			{
				outstream.Write(delimeter);
				outstream.Write(FDMExec.Atmosphere.Density.ToString(formatDouble, provider) + delimeter);
				outstream.Write(FDMExec.Atmosphere.GetWindNED().ToString(formatDouble, provider));
			}
			if (SubSystems[(int)eSubSystems.MassProps]) 
			{
				outstream.Write(delimeter);
				outstream.Write(FDMExec.MassBalance.GetJ().ToString(formatDouble, provider) + delimeter);
				outstream.Write(FDMExec.MassBalance.Mass.ToString(formatDouble, provider) + delimeter);
				outstream.Write(FDMExec.MassBalance.GetXYZcg().ToString(formatDouble, provider));
			}
			if (SubSystems[(int)eSubSystems.Propagate]) 
			{
				outstream.Write(delimeter);
				outstream.Write(FDMExec.Propagate.Altitude.ToString(formatDouble, provider) + delimeter);
				outstream.Write(FDMExec.Propagate.GetEuler().ToString(formatDouble, provider) + delimeter);
				outstream.Write(FDMExec.Auxiliary.AlphaDegrees.ToString(formatDouble, provider) + delimeter);
				outstream.Write(FDMExec.Auxiliary.BetaDegrees.ToString(formatDouble, provider) + delimeter);
				outstream.Write(FDMExec.Propagate.GetLocation().LatitudeDeg.ToString(formatDouble, provider) + delimeter);
				outstream.Write(FDMExec.Propagate.GetLocation().LongitudeDeg.ToString(formatDouble, provider) + delimeter);
				outstream.Write(FDMExec.Propagate.DistanceAGL.ToString(formatDouble, provider) + delimeter);
				outstream.Write(FDMExec.Propagate.GetRunwayRadius().ToString(formatDouble, provider));
			}
			if (SubSystems[(int)eSubSystems.Coefficients]) 
			{
                scratch = FDMExec.Aerodynamics.GetCoefficientValues(formatDouble, provider, delimeter);
				if (scratch.Length != 0) outstream.Write(delimeter + scratch);
			}
			if (SubSystems[(int)eSubSystems.FCS]) 
			{
                scratch = FDMExec.FlightControlSystem.GetComponentValues(formatDouble, provider, delimeter);
				if (scratch.Length != 0) outstream.Write(delimeter + scratch);
			}
			if (SubSystems[(int)eSubSystems.GroundReactions]) 
			{
				outstream.Write(delimeter);
                outstream.Write(FDMExec.GroundReactions.GetGroundReactionValues(formatDouble, provider, delimeter));
			}
			if (SubSystems[(int)eSubSystems.Propulsion] && FDMExec.Propulsion.GetNumEngines() > 0) 
			{
				outstream.Write(delimeter);
                outstream.Write(FDMExec.Propulsion.GetPropulsionValues(formatDouble, provider, delimeter));
			}

			foreach (PropertyNode prop in outputProperties) 
			{
                outstream.Write(delimeter + ((double)prop.Get()).ToString(formatDouble, provider));
			}

			outstream.WriteLine();
			outstream.Flush();
		}

		public void Log4NetOutput()
		{
			if (!log.IsInfoEnabled)
				return;

			string scratch = "";

			if (dFirstPass) 
			{
				if (SubSystems[(int)eSubSystems.Coefficients]) 
				{
					scratch = FDMExec.Aerodynamics.GetCoefficientStrings(delimeter);
					if (scratch.Length != 0)
						log.Info(delimeter +scratch);
				}
				if (SubSystems[(int)eSubSystems.FCS]) 
				{
					scratch = FDMExec.FlightControlSystem.GetComponentStrings(delimeter);
					if (scratch.Length != 0)
						log.Info(delimeter + scratch);
				}
				if (SubSystems[(int)eSubSystems.GroundReactions]) 
				{
					log.Info(delimeter);
					log.Info(FDMExec.GroundReactions.GetGroundReactionStrings(delimeter));
				}
				if (SubSystems[(int)eSubSystems.Propulsion] && FDMExec.Propulsion.GetNumEngines() > 0) 
				{
					log.Info(delimeter);
					log.Info(FDMExec.Propulsion.GetPropulsionStrings(delimeter));
				}
				if (outputProperties.Count > 0) 
				{
					foreach (PropertyNode prop in outputProperties) 
					{
						log.Info(delimeter + prop.ShortName);
					}
				}
				dFirstPass = false;
			}


			log.Info("Time :" + FDMExec.State.SimTime);
			if (SubSystems[(int)eSubSystems.Simulation]) 
			{
			}
			if (SubSystems[(int)eSubSystems.Aerosurfaces]) 
			{
				log.Info("Aerosurfaces");
				log.Info("Aileron Cmd :" + FDMExec.FlightControlSystem.AileronCmd);
				log.Info("Elevator Cmd :" + FDMExec.FlightControlSystem.ElevatorCmd);
				log.Info("Rudder Cmd :" + FDMExec.FlightControlSystem.RudderCmd);
				log.Info("Flap Cmd :" + FDMExec.FlightControlSystem.FlapsCmd);
				log.Info("Left Aileron Pos :" + FDMExec.FlightControlSystem.LeftAileronPositionRadians);
				log.Info("Right Aileron Pos :" + FDMExec.FlightControlSystem.GetDaRPosRadians());
				log.Info("Elevator Pos :" + FDMExec.FlightControlSystem.GetDePosRadians());
				log.Info("Rudder Pos :" + FDMExec.FlightControlSystem.GetDrPosRadians());
				log.Info("Flap Pos :" + FDMExec.FlightControlSystem.GetDfPosRadians());
			}
			if (SubSystems[(int)eSubSystems.Rates]) 
			{
				log.Info("Rates");
				log.Info("PQR :" + FDMExec.Propagate.GetPQR());
				log.Info("PQR dot :" + FDMExec.Propagate.GetPQRdot());
			}
			if (SubSystems[(int)eSubSystems.Velocities])
			{
				log.Info("Velocities");
				log.Info("QBar :" + FDMExec.Auxiliary.Qbar);
				log.Info("Vtotal :" + FDMExec.Auxiliary.Vt);
				log.Info("UVW Body :" + FDMExec.Propagate.GetUVW());
				log.Info("UVW Aero :" + FDMExec.Auxiliary.GetAeroUVW());
				log.Info("V ned :" + FDMExec.Propagate.GetVel());
			}
			if (SubSystems[(int)eSubSystems.Forces])
			{
				log.Info("Forces");
				log.Info("Drag, Side, Lift :" + FDMExec.Aerodynamics.ForcesDragSideLift);
				log.Info("L/D :" + FDMExec.Aerodynamics.LoD);
				log.Info("X YZ force :" + FDMExec.Aircraft.Forces);
			}
			if (SubSystems[(int)eSubSystems.Moments]) 
			{
				log.Info("Moments");
				log.Info("LMN :" + FDMExec.Aircraft.Moments);
			}
			if (SubSystems[(int)eSubSystems.Atmosphere])
			{
				log.Info("Atmosphere");
				log.Info("Rho :" + FDMExec.Atmosphere.Density);
				log.Info("NED Wind" + FDMExec.Atmosphere.GetWindNED());
			}
			if (SubSystems[(int)eSubSystems.MassProps])
			{
				log.Info("MassProps");
				log.Info("I :" + FDMExec.MassBalance.GetJ());
				log.Info("Mass :" + FDMExec.MassBalance.Mass);
				log.Info("XYZ cg :" + FDMExec.MassBalance.GetXYZcg());
			}
			if (SubSystems[(int)eSubSystems.Propagate]) 
			{
				log.Info("Propagate");
				log.Info("Altitude:" + FDMExec.Propagate.Altitude);
				log.Info("Phi Tht Psi:" + FDMExec.Propagate.GetEuler());
				log.Info("Alpha:" + FDMExec.Auxiliary.AlphaDegrees);
				log.Info("Beta:" + FDMExec.Auxiliary.BetaDegrees);
				log.Info("Latitude (Deg):" + FDMExec.Propagate.GetLocation().LatitudeDeg);
				log.Info("Longitude (Deg):" + FDMExec.Propagate.GetLocation().LongitudeDeg);
				log.Info("Distance AGL:" + FDMExec.Propagate.DistanceAGL);
				log.Info("Runway Radius:" +FDMExec.Propagate.GetRunwayRadius());
			}
			if (SubSystems[(int)eSubSystems.Coefficients]) 
			{
                scratch = FDMExec.Aerodynamics.GetCoefficientValues(formatDouble, provider, delimeter);
				if (scratch.Length != 0)
					log.Info(delimeter + scratch);
			}
			if (SubSystems[(int)eSubSystems.FCS]) 
			{
                scratch = FDMExec.FlightControlSystem.GetComponentValues(formatDouble, provider, delimeter);
				if (scratch.Length != 0) 
					log.Info(delimeter + scratch);
			}
			if (SubSystems[(int)eSubSystems.GroundReactions]) 
			{
				log.Info(delimeter);
                log.Info(FDMExec.GroundReactions.GetGroundReactionValues(formatDouble, provider, delimeter));
			}
			if (SubSystems[(int)eSubSystems.Propulsion] && FDMExec.Propulsion.GetNumEngines() > 0) 
			{
				log.Info(delimeter);
                log.Info(FDMExec.Propulsion.GetPropulsionValues(formatDouble, provider, delimeter));
			}

			foreach (PropertyNode prop in outputProperties) 
			{
				log.Info(delimeter + (double)prop.Get());
			}
		}

		public void SocketOutput()
		{
            if (log.IsWarnEnabled)
                log.Warn("Socket output is Not Implemented");
		}
		public void SocketStatusOutput(string s)
		{
            if (log.IsWarnEnabled)
                log.Warn("Socket output is Not Implemented");
		}

		public void SetFilename(string fn) {filename = fn;}

		public void SetType(string type)
		{
			if (type == "CSV") 
			{
				outputType = OutputType.CSV;
				delimeter = ", ";
			} 
			else if (type == "TABULAR") 
			{
				outputType = OutputType.Tab;
				delimeter = "\t";
			} 
			else if (type == "SOCKET") 
			{
				outputType = OutputType.Socket;
			} 
			else if (type == "TERMINAL") 
			{
				outputType = OutputType.Terminal;
			} 			
			else if (type == "LOG4NET") 
			{
				outputType = OutputType.Log4Net;
			}
			else if (type != "NONE") 
			{
				outputType = OutputType.Unknown;
				log.Info("Unknown type of output specified in config file");
			}
		}


		public void SetSubsystems(int tt) {SubSystems[tt] = true;}
		public void Enable() { enabled = true; }
		public void Disable() { enabled = false; }
		public bool Toggle() {enabled = !enabled; return enabled;}
		
		public void Load(XmlElement rootElement)
		{
            int outRate = 0;
            XmlElement element;

            string fname = rootElement.GetAttribute("file");
            if (fname.Length != 0)
            {   
                throw new NotImplementedException("Output file configuration not implemented");
                /* TODO
                string output_file_name = FDMExec.AircraftPath + separator
                                    + FDMExec.ModelName + separator + fname + ".xml";

                output_file->open(output_file_name.c_str());
                readXML(*output_file, output_file_parser);
                delete output_file;
                document = output_file_parser.GetDocument();
                 */
            }
            else
            {
                element = rootElement;
            }
            
            string portNumber = element.GetAttribute("port");
            string name = element.GetAttribute("name");
            string type = element.GetAttribute("type");
            string culture = element.GetAttribute("culture");
            SetType(type);

            if (portNumber.Length != 0 && outputType == OutputType.Socket)
            {
                int port = int.Parse(portNumber);
                //socket = new FGfdmSocket(name, port);
            }
            else
            {
                if (name.Length != 0)
                    filename = name;
            }

            if (culture.Length != 0)
            {
                try
                {
                    IFormatProvider newprovider = new System.Globalization.CultureInfo(culture, true);
                    provider = newprovider;
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                        log.Error("Culture provider: " + culture +" raised exception "+e);
                }
            } 

            string strRate = element.GetAttribute("rate");
            if (strRate.Length != 0)
            {
                outRate = int.Parse(strRate, FormatHelper.numberFormatInfo);
            }
            else
            {
                outRate = 1;
            }

			foreach (XmlNode currentNode in element.ChildNodes)
			{
                if (currentNode.NodeType == XmlNodeType.Element)
                {
                    XmlElement currentElement = (XmlElement)currentNode;

                    if (currentElement.LocalName.Equals("simulation") && currentElement.InnerText.Contains("ON"))
                    {
                        SubSystems[(int)eSubSystems.Simulation] = true;
                    }
                    else if (currentElement.LocalName.Equals("aerosurfaces") && currentElement.InnerText.Contains("ON"))
                    {
                        SubSystems[(int)eSubSystems.Aerosurfaces] = true;
                    }
                    else if (currentElement.LocalName.Equals("rates") && currentElement.InnerText.Contains("ON"))
                    {
                        SubSystems[(int)eSubSystems.Rates] = true;
                    }
                    else if (currentElement.LocalName.Equals("velocities") && currentElement.InnerText.Contains("ON"))
                    {
                        SubSystems[(int)eSubSystems.Velocities] = true;
                    }
                    else if (currentElement.LocalName.Equals("forces") && currentElement.InnerText.Contains("ON"))
                    {
                        SubSystems[(int)eSubSystems.Forces] = true;
                    }
                    else if (currentElement.LocalName.Equals("moments") && currentElement.InnerText.Contains("ON"))
                    {
                        SubSystems[(int)eSubSystems.Moments] = true;
                    }
                    else if (currentElement.LocalName.Equals("atmosphere") && currentElement.InnerText.Contains("ON"))
                    {
                        SubSystems[(int)eSubSystems.Atmosphere] = true;
                    }
                    else if (currentElement.LocalName.Equals("massprops") && currentElement.InnerText.Contains("ON"))
                    {
                        SubSystems[(int)eSubSystems.MassProps] = true;
                    }
                    else if (currentElement.LocalName.Equals("position") && currentElement.InnerText.Contains("ON"))
                    {
                        SubSystems[(int)eSubSystems.Propagate] = true;
                    }
                    else if (currentElement.LocalName.Equals("coefficients") && currentElement.InnerText.Contains("ON"))
                    {
                        SubSystems[(int)eSubSystems.Coefficients] = true;
                    }
                    else if (currentElement.LocalName.Equals("ground_reactions") && currentElement.InnerText.Contains("ON"))
                    {
                        SubSystems[(int)eSubSystems.GroundReactions] = true;
                    }
                    else if (currentElement.LocalName.Equals("fcs") && currentElement.InnerText.Contains("ON"))
                    {
                        SubSystems[(int)eSubSystems.FCS] = true;
                    }
                    else if (currentElement.LocalName.Equals("propulsion") && currentElement.InnerText.Contains("ON"))
                    {
                        SubSystems[(int)eSubSystems.Propulsion] = true;
                    }
                    else if (currentElement.LocalName.Equals("property"))
                    {
                        outputProperties.Add(FDMExec.PropertyManager.GetPropertyNode(currentElement.InnerText.Trim()));
                    }
                }
			}
            outRate = outRate > 120 ? 120 : (outRate < 0 ? 0 : outRate);
            rate = (int)(0.5 + 1.0 / (FDMExec.State.DeltaTime * outRate));
        }
		
		/// Subsystem types for specifying which will be output in the FDM data logging
		public enum  eSubSystems : int
		{
			/** Subsystem: Simulation (= 1)          */ Simulation,
			/** Subsystem: Aerosurfaces (= 2)        */ Aerosurfaces,
			/** Subsystem: Body rates (= 4)          */ Rates,
			/** Subsystem: Velocities (= 8)          */ Velocities,
			/** Subsystem: Forces (= 16)             */ Forces,
			/** Subsystem: Moments (= 32)            */ Moments,
			/** Subsystem: Atmosphere (= 64)         */ Atmosphere,
			/** Subsystem: Mass InputOutput (= 128)   */ MassProps,
			/** Subsystem: Coefficients (= 256)      */ Coefficients,
			/** Subsystem: Propagate (= 512)         */ Propagate,
			/** Subsystem: Ground Reactions (= 1024) */ GroundReactions,
			/** Subsystem: FCS (= 2048)              */ FCS,
			/** Subsystem: Propulsion (= 4096)       */ Propulsion      
		};

		public string formatDouble = "F6";
		public string formatDouble2 = "F12";

		public IFormatProvider provider = new System.Globalization.CultureInfo("en-US", true);
		 

		private bool sFirstPass, dFirstPass, enabled;
		private BitArray SubSystems = new BitArray( 13 );


		private string filename, delimeter;

		private enum OutputType {None, CSV, Tab, Socket, Terminal, Log4Net, Unknown} ;
		private OutputType outputType;
		//TODO private ofstream datafile;
		//TODO private FGfdmSocket socket;
        private List<PropertyNode> outputProperties = new List<PropertyNode>();

		private const string IdSrc = "$Id: FGOutput.cpp,v 1.92 2004/11/02 05:19:42 jberndt Exp $";
	}
}
